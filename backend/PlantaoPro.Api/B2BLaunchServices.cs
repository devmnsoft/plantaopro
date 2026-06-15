using System.Security.Cryptography;
using System.Text;
using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Data;

public sealed class TenantIsolationValidatorService
{
    private readonly TenantContextService _tenantContext;
    private readonly IAuditService _audit;
    private readonly ILogger<TenantIsolationValidatorService> _logger;

    public TenantIsolationValidatorService(TenantContextService tenantContext, IAuditService audit, ILogger<TenantIsolationValidatorService> logger)
    {
        _tenantContext = tenantContext;
        _audit = audit;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> ValidarAcessoTenantAsync(Guid tenantId, string entidade, Guid? entidadeId, string? ip)
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        if (!ctx.Success || ctx.Data is null || ctx.Data.TenantId is null)
        {
            await RegistrarBloqueioAsync(null, entidade, entidadeId, "tenant_nao_identificado", ip);
            return ApiResponse<string>.Fail("Tenant não identificado para esta operação.", 401);
        }

        var adminGlobal = _tenantContext.UsuarioEhAdminGlobal();
        if (adminGlobal)
        {
            await _audit.RegistrarAsync(_tenantContext.ObterUsuarioId(), ctx.Data.ClienteId, "TENANT_CONTEXT", tenantId, "ADMIN_GLOBAL_ALTERNOU_CONTEXTO", new { entidade }, true, ip, "ADMINISTRADOR_GLOBAL");
            return ApiResponse<string>.Ok("ok");
        }

        if (ctx.Data.TenantId.Value != tenantId)
        {
            await RegistrarBloqueioAsync(ctx.Data.ClienteId, entidade, entidadeId, "acesso_cruzado_tenant", ip);
            return ApiResponse<string>.Fail("Acesso negado para dados de outro cliente.", 403);
        }

        if (string.Equals(ctx.Data.Status, "SUSPENSO", StringComparison.OrdinalIgnoreCase))
        {
            await RegistrarBloqueioAsync(ctx.Data.ClienteId, entidade, entidadeId, "tenant_suspenso", ip);
            return ApiResponse<string>.Fail("Tenant suspenso. Regularize a assinatura para continuar.", 403);
        }

        return ApiResponse<string>.Ok("ok");
    }

    public async Task<ApiResponse<string>> ValidarEntidadeDoTenantAsync(Guid tenantId, Guid? entidadeTenantId, string entidade, Guid entidadeId, string? ip)
    {
        if (entidadeTenantId is null || entidadeTenantId.Value != tenantId)
        {
            await RegistrarBloqueioAsync(null, entidade, entidadeId, "entidade_de_outro_tenant", ip);
            return ApiResponse<string>.Fail("Registro não pertence ao tenant atual.", 403);
        }
        return await ValidarAcessoTenantAsync(tenantId, entidade, entidadeId, ip);
    }

    private async Task RegistrarBloqueioAsync(Guid? clienteId, string entidade, Guid? entidadeId, string motivo, string? ip)
    {
        _logger.LogWarning("Bloqueio multi-tenant em {Entidade} {EntidadeId}: {Motivo}", entidade, entidadeId, motivo);
        await _audit.RegistrarAsync(_tenantContext.ObterUsuarioId(), clienteId, entidade, entidadeId, "ACESSO_CROSS_TENANT_BLOQUEADO", new { motivo }, false, ip, "TENANT_ISOLATION");
    }
}

public sealed class B2BLaunchService
{
    private static readonly string[] EscoposPermitidos =
    {
        "plantoes:read",
        "plantoes:write",
        "medicos:read",
        "escalas:read",
        "webhooks:write",
        "pacientes:read",
        "agendamentos:read",
        "consultas:read",
        "financeiro:read"
    };

    private readonly IConfiguration _cfg;
    private readonly TenantContextService _tenantContext;
    private readonly IAuditService _audit;
    private readonly ILogger<B2BLaunchService> _logger;

    public B2BLaunchService(IConfiguration cfg, TenantContextService tenantContext, IAuditService audit, ILogger<B2BLaunchService> logger)
    {
        _cfg = cfg;
        _tenantContext = tenantContext;
        _audit = audit;
        _logger = logger;
    }

    public Task<ApiResponse<DeveloperOverviewDto>> DeveloperOverviewAsync()
    {
        var dto = new DeveloperOverviewDto
        {
            Titulo = "Developer Portal PlantãoPro",
            Autenticacao = "Envie Authorization: Bearer {token} ou X-Api-Key para integrações servidor-servidor.",
            EscoposDisponiveis = EscoposPermitidos,
            EndpointsLiberados = new[] { "/api/plantoes", "/api/medicos", "/api/escalas", "/api/developer/webhooks" },
            LimiteRequisicoesMinuto = 120
        };
        return Task.FromResult(ApiResponse<DeveloperOverviewDto>.Ok(dto));
    }

    public async Task<ApiResponse<ApiKeyCreateResultDto>> CriarApiKeyAsync(ApiKeyCreateRequest request, string? ip)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Nome)) return ApiResponse<ApiKeyCreateResultDto>.Fail("Nome da chave é obrigatório.", 400);

            var escopos = request.Escopos?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? Array.Empty<string>();

            if (escopos.Length == 0) return ApiResponse<ApiKeyCreateResultDto>.Fail("Informe ao menos um escopo.", 400);

            var invalidos = escopos
                .Where(x => !EscoposPermitidos.Contains(x, StringComparer.OrdinalIgnoreCase))
                .ToArray();

            if (invalidos.Length > 0)
            {
                return ApiResponse<ApiKeyCreateResultDto>.Fail("Existem escopos inválidos para a chave de API.", 400, invalidos);
            }

            var ctx = await _tenantContext.ObterAtualAsync();
            if (!ctx.Success || ctx.Data?.TenantId is null) return ApiResponse<ApiKeyCreateResultDto>.Fail(ctx.Message, ctx.StatusCode);

            var chave = "pp_live_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace("+", "").Replace("/", "").Replace("=", "");
            var hash = HashApiKey(chave);
            var id = Guid.NewGuid();

            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();
            await cn.ExecuteAsync(@"insert into plantaopro.api_chaves(id,tenant_id,cliente_id,nome,api_key_hash,escopos,status,reg_date,reg_status)
values(@id,@tenantId,@clienteId,@nome,@hash,@escopos,'ATIVA',now(),'A')", new { id, tenantId = ctx.Data.TenantId.Value, clienteId = ctx.Data.ClienteId, nome = request.Nome.Trim(), hash, escopos = string.Join(',', escopos) }, tx);
            await cn.ExecuteAsync(@"insert into plantaopro.api_rate_limits(id,tenant_id,cliente_id,api_chave_id,limite_minuto,limite_dia,status,reg_date,reg_status)
values(gen_random_uuid(),@tenantId,@clienteId,@id,120,10000,'ATIVO',now(),'A')", new { id, tenantId = ctx.Data.TenantId.Value, clienteId = ctx.Data.ClienteId }, tx);
            await tx.CommitAsync();

            await _audit.RegistrarAsync(_tenantContext.ObterUsuarioId(), ctx.Data.ClienteId, "API_KEY", id, "CRIAR", new { Nome = request.Nome.Trim(), escopos }, true, ip, "ADMINISTRADOR_CLIENTE");
            return ApiResponse<ApiKeyCreateResultDto>.Ok(new ApiKeyCreateResultDto { Id = id, Nome = request.Nome.Trim(), ChaveExibicaoUnica = chave, Aviso = "Copie agora. A chave não será exibida novamente." }, "API key criada com sucesso.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar API key");
            return ApiResponse<ApiKeyCreateResultDto>.Fail("Não foi possível criar a API key.", 500);
        }
    }

    public Task<ApiResponse<IEnumerable<string>>> ListarEscoposAsync()
    {
        return Task.FromResult(ApiResponse<IEnumerable<string>>.Ok(EscoposPermitidos));
    }

    public async Task<ApiResponse<IEnumerable<object>>> ListarApiKeysAsync()
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        if (!ctx.Success) return ApiResponse<IEnumerable<object>>.Fail(ctx.Message, ctx.StatusCode);

        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync(@"select id, nome, escopos, status, reg_date as CriadoEm, ultimo_uso_em as UltimoUsoEm
from plantaopro.api_chaves
where (@tenantId is null or tenant_id = @tenantId) and reg_status = 'A'
order by reg_date desc", new { tenantId = ctx.Data?.TenantId });
        return ApiResponse<IEnumerable<object>>.Ok(rows.Cast<object>());
    }

    public Task<ApiResponse<IEnumerable<object>>> ListarApiKeyLogsAsync(Guid id)
    {
        var logs = new List<object>();
        return Task.FromResult(ApiResponse<IEnumerable<object>>.Ok(logs));
    }

    public async Task<ApiResponse<ApiKeyCreateResultDto>> RotacionarApiKeyAsync(Guid id, string? ip)
    {
        var revogar = await RevogarApiKeyAsync(id, ip);
        if (!revogar.Success) return ApiResponse<ApiKeyCreateResultDto>.Fail(revogar.Message, revogar.StatusCode, revogar.Errors);
        return ApiResponse<ApiKeyCreateResultDto>.Ok(new ApiKeyCreateResultDto { Id = id, Aviso = "Chave revogada. Crie uma nova chave para concluir a rotação." }, "Rotação iniciada com segurança.");
    }

    public async Task<ApiResponse<string>> RevogarApiKeyAsync(Guid id, string? ip)
    {
        try
        {
            var ctx = await _tenantContext.ObterAtualAsync();
            if (!ctx.Success) return ApiResponse<string>.Fail(ctx.Message, ctx.StatusCode);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var linhas = await cn.ExecuteAsync("update plantaopro.api_chaves set status='REVOGADA', revogada_em=now(), reg_update=now() where id=@id and (@tenantId is null or tenant_id=@tenantId) and reg_status='A'", new { id, tenantId = ctx.Data?.TenantId });
            if (linhas == 0) return ApiResponse<string>.Fail("API key não encontrada para este tenant.", 404);
            await _audit.RegistrarAsync(_tenantContext.ObterUsuarioId(), ctx.Data?.ClienteId, "API_KEY", id, "REVOGAR", new { id }, true, ip, "ADMINISTRADOR_CLIENTE");
            return ApiResponse<string>.Ok("ok", "API key revogada.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao revogar API key {ApiKeyId}", id);
            return ApiResponse<string>.Fail("Não foi possível revogar a API key.", 500);
        }
    }

    public Task<ApiResponse<IEnumerable<B2BResumoItemDto>>> ListarAsync(string area)
    {
        var itens = Dados(area);
        return Task.FromResult(ApiResponse<IEnumerable<B2BResumoItemDto>>.Ok(itens));
    }

    public async Task<ApiResponse<Guid>> CriarItemAuditavelAsync(string entidade, string nome, string descricao, string prioridade, string? ip)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(nome)) return ApiResponse<Guid>.Fail("Nome/título é obrigatório.", 400);
            var ctx = await _tenantContext.ObterAtualAsync();
            var id = Guid.NewGuid();
            await _audit.RegistrarAsync(_tenantContext.ObterUsuarioId(), ctx.Data?.ClienteId, entidade, id, "CRIAR", new { nome, descricao, prioridade }, true, ip, "B2B");
            return ApiResponse<Guid>.Ok(id, "Registro criado e auditado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar item auditável {Entidade}", entidade);
            return ApiResponse<Guid>.Fail("Não foi possível criar o registro.", 500);
        }
    }

    public async Task<ApiResponse<string>> AlterarStatusAuditavelAsync(string entidade, Guid id, string acao, string? ip)
    {
        try
        {
            var ctx = await _tenantContext.ObterAtualAsync();
            await _audit.RegistrarAsync(_tenantContext.ObterUsuarioId(), ctx.Data?.ClienteId, entidade, id, acao, new { id }, true, ip, "B2B");
            return ApiResponse<string>.Ok("ok", "Status atualizado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao alterar status {Entidade} {Id}", entidade, id);
            return ApiResponse<string>.Fail("Não foi possível alterar o status.", 500);
        }
    }

    public Task<ApiResponse<OperacionalLaunchDto>> OperacionalAsync()
    {
        var dto = new OperacionalLaunchDto
        {
            CentralEscala = Dados("central-escala"),
            AgendaMedico = Dados("agenda-medico"),
            Disponibilidades = Dados("disponibilidade"),
            Substituicoes = Dados("substituicao"),
            Relatorios = Dados("relatorios")
        };
        return Task.FromResult(ApiResponse<OperacionalLaunchDto>.Ok(dto));
    }

    private static string HashApiKey(string chave)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(chave))).ToLowerInvariant();
    }

    private static IEnumerable<B2BResumoItemDto> Dados(string area)
    {
        var hoje = DateTime.UtcNow.Date;
        if (string.Equals(area, "contratos", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { new B2BResumoItemDto { Id = Guid.Empty, Codigo = "SLA-ENTERPRISE", Nome = "Contrato Enterprise White Label", Descricao = "SLA, propriedade dos dados, taxa de setup e aceite digital.", Status = "ATIVO", Valor = 3990, DataReferencia = hoje } };
        }
        if (string.Equals(area, "suporte", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { new B2BResumoItemDto { Id = Guid.Empty, Codigo = "SUP-B2B", Nome = "Suporte B2B prioritário", Descricao = "Fluxo empresa contratante -> MNSOFT com SLA por prioridade.", Status = "ABERTO", Prioridade = "ALTA", DataReferencia = hoje } };
        }
        if (string.Equals(area, "monitoramento", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { new B2BResumoItemDto { Id = Guid.Empty, Codigo = "HEALTH", Nome = "Healthchecks ativos", Descricao = "Endpoints lentos, erros críticos, uso por tenant e alertas.", Status = "ONLINE", Prioridade = "CRITICA", DataReferencia = hoje } };
        }
        if (string.Equals(area, "gotomarket", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { new B2BResumoItemDto { Id = Guid.Empty, Codigo = "ONE-PAGE", Nome = "One page comercial", Descricao = "Material para hospitais, empresas de gestão e parceiros.", Status = "INTERNO", DataReferencia = hoje } };
        }
        if (string.Equals(area, "central-escala", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { new B2BResumoItemDto { Id = Guid.Empty, Codigo = "RISCO_ESCALA", Nome = "Plantões descobertos", Descricao = "Visão por hospital, especialidade e status com convite rápido.", Status = "ATENCAO", Prioridade = "ALTA", DataReferencia = hoje } };
        }
        if (string.Equals(area, "agenda-medico", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { new B2BResumoItemDto { Id = Guid.Empty, Codigo = "AGENDA", Nome = "Agenda mensal do médico", Descricao = "Próximos plantões, histórico, pagamentos previstos e convites.", Status = "ATIVO", DataReferencia = hoje } };
        }
        if (string.Equals(area, "disponibilidade", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { new B2BResumoItemDto { Id = Guid.Empty, Codigo = "DISPONIBILIDADE", Nome = "Disponibilidade médica", Descricao = "Dias disponíveis, indisponibilidades e sugestão para coordenação.", Status = "ATIVO", DataReferencia = hoje } };
        }
        if (string.Equals(area, "substituicao", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { new B2BResumoItemDto { Id = Guid.Empty, Codigo = "SUBSTITUICAO", Nome = "Substituição de plantão", Descricao = "Solicitação, aprovação/recusa, aceite de outro médico e auditoria.", Status = "ATIVO", DataReferencia = hoje } };
        }
        return new[] { new B2BResumoItemDto { Id = Guid.Empty, Codigo = "RELATORIOS", Nome = "Relatórios operacionais", Descricao = "Plantões, escalas, cancelamentos, substituições, pagamentos e exportações auditadas.", Status = "ATIVO", DataReferencia = hoje } };
    }
}
