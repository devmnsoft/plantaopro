using System.Security.Claims;
using System.Text.Json;
using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public interface ILogOperacionalService
{
    Task RegistrarAsync(Guid? usuarioId, Guid? clienteId, string? perfil, string acao, string entidade, Guid? entidadeId, string? ip, string? userAgent, bool sucesso, string mensagem, object? dadosAntes = null, object? dadosDepois = null, string? correlationId = null);
}

public interface ILgpdAuditService
{
    Task RegistrarEventoAsync(Guid? usuarioId, Guid? clienteId, string evento, string? entidade, Guid? entidadeId, object? detalhes, string? ip, string? userAgent);
}

public interface IEventoSistemaService
{
    Task RegistrarAsync(Guid? usuarioId, Guid? clienteId, string? perfil, string acao, string entidade, Guid? entidadeId, bool sucesso, string mensagem, string? ip, string? userAgent, string? correlationId);
}

public sealed class EventoSistemaService : IEventoSistemaService, ILogOperacionalService
{
    private readonly IConfiguration cfg;
    private readonly ILogger<EventoSistemaService> logger;

    public EventoSistemaService(IConfiguration cfg, ILogger<EventoSistemaService> logger)
    {
        this.cfg = cfg;
        this.logger = logger;
    }

    public Task RegistrarAsync(Guid? usuarioId, Guid? clienteId, string? perfil, string acao, string entidade, Guid? entidadeId, bool sucesso, string mensagem, string? ip, string? userAgent, string? correlationId)
        => RegistrarAsync(usuarioId, clienteId, perfil, acao, entidade, entidadeId, ip, userAgent, sucesso, mensagem, null, null, correlationId);

    public async Task RegistrarAsync(Guid? usuarioId, Guid? clienteId, string? perfil, string acao, string entidade, Guid? entidadeId, string? ip, string? userAgent, bool sucesso, string mensagem, object? dadosAntes = null, object? dadosDepois = null, string? correlationId = null)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync(@"insert into plantaopro.eventos_sistema(id, usuario_id, cliente_id, perfil, acao, entidade, entidade_id, ip, user_agent, sucesso, mensagem, dados_antes, dados_depois, correlation_id, reg_date)
values(gen_random_uuid(), @usuarioId, @clienteId, @perfil, @acao, @entidade, @entidadeId, @ip, @userAgent, @sucesso, @mensagem, cast(@dadosAntes as jsonb), cast(@dadosDepois as jsonb), @correlationId, now())", new
            {
                usuarioId,
                clienteId,
                perfil = Sanitizar(perfil, 120),
                acao = Sanitizar(acao, 120),
                entidade = Sanitizar(entidade, 120),
                entidadeId,
                ip = Sanitizar(ip, 80),
                userAgent = Sanitizar(userAgent, 500),
                sucesso,
                mensagem = Sanitizar(mensagem, 1000),
                dadosAntes = SerializarSeguro(dadosAntes),
                dadosDepois = SerializarSeguro(dadosDepois),
                correlationId = Sanitizar(correlationId, 120)
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao registrar evento operacional {Acao} {Entidade}", acao, entidade);
        }
    }

    private static string? SerializarSeguro(object? valor)
    {
        if (valor is null) return null;
        var json = JsonSerializer.Serialize(valor);
        return json.Replace("senha", "***", StringComparison.OrdinalIgnoreCase)
            .Replace("token", "***", StringComparison.OrdinalIgnoreCase)
            .Replace("secret", "***", StringComparison.OrdinalIgnoreCase);
    }

    private static string? Sanitizar(string? valor, int tamanho)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var seguro = valor.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);
        return seguro.Length <= tamanho ? seguro : seguro.Substring(0, tamanho);
    }
}

public sealed class LgpdAuditService : ILgpdAuditService
{
    private readonly IConfiguration cfg;
    private readonly ILogger<LgpdAuditService> logger;

    public LgpdAuditService(IConfiguration cfg, ILogger<LgpdAuditService> logger)
    {
        this.cfg = cfg;
        this.logger = logger;
    }

    public async Task RegistrarEventoAsync(Guid? usuarioId, Guid? clienteId, string evento, string? entidade, Guid? entidadeId, object? detalhes, string? ip, string? userAgent)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync(@"insert into plantaopro.lgpd_eventos_privacidade(id, usuario_id, cliente_id, evento, entidade, entidade_id, detalhes, ip, user_agent, reg_date)
values(gen_random_uuid(), @usuarioId, @clienteId, @evento, @entidade, @entidadeId, cast(@detalhes as jsonb), @ip, @userAgent, now())", new
            {
                usuarioId,
                clienteId,
                evento,
                entidade,
                entidadeId,
                detalhes = detalhes is null ? null : JsonSerializer.Serialize(detalhes),
                ip,
                userAgent
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao registrar evento LGPD {Evento}", evento);
        }
    }
}

public abstract class SaasEvolutionServiceBase
{
    protected readonly IConfiguration Cfg;
    protected readonly IAuditService Audit;
    protected readonly ILogOperacionalService LogOperacional;
    protected readonly ILogger Logger;

    protected SaasEvolutionServiceBase(IConfiguration cfg, IAuditService audit, ILogOperacionalService logOperacional, ILogger logger)
    {
        Cfg = cfg;
        Audit = audit;
        LogOperacional = logOperacional;
        Logger = logger;
    }

    protected static Guid? GetUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub") ?? user.FindFirstValue("usuario_id") ?? user.FindFirstValue("uid");
        if (Guid.TryParse(raw, out var id)) return id;
        return null;
    }

    protected static Guid? GetClienteId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue("cliente_id") ?? user.FindFirstValue("clienteId") ?? user.FindFirstValue("cliente");
        if (Guid.TryParse(raw, out var id)) return id;
        return null;
    }

    protected static string GetPerfil(ClaimsPrincipal user)
    {
        var roles = user.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray();
        if (roles.Length > 0) return string.Join(',', roles);
        return user.FindFirstValue("perfil") ?? "sem-perfil";
    }

    protected static bool IsAdminGlobal(ClaimsPrincipal user)
        => user.IsInRole(RolesConstants.AdministradorGlobal) || string.Equals(user.FindFirstValue("perfil"), RolesConstants.AdministradorGlobal, StringComparison.OrdinalIgnoreCase);

    protected static bool PodeVerCliente(ClaimsPrincipal user, Guid? clienteId)
    {
        if (IsAdminGlobal(user)) return true;
        var contexto = GetClienteId(user);
        return contexto.HasValue && clienteId.HasValue && contexto.Value == clienteId.Value;
    }

    protected async Task AuditarAsync(ClaimsPrincipal user, string entidade, Guid? entidadeId, string acao, object? detalhes, bool sucesso, HttpContext httpContext, Guid? clienteId = null)
    {
        var usuarioId = GetUserId(user);
        var cliente = clienteId ?? GetClienteId(user);
        var perfil = GetPerfil(user);
        var ip = httpContext.Connection.RemoteIpAddress?.ToString();
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        await Audit.RegistrarAsync(usuarioId, cliente, entidade, entidadeId, acao, detalhes, sucesso, ip, perfil);
        await LogOperacional.RegistrarAsync(usuarioId, cliente, perfil, acao, entidade, entidadeId, ip, ua, sucesso, "Ação registrada", null, detalhes, httpContext.TraceIdentifier);
    }
}

public sealed class LgpdService : SaasEvolutionServiceBase
{
    private readonly ILgpdAuditService lgpdAudit;

    public LgpdService(IConfiguration cfg, IAuditService audit, ILogOperacionalService logOperacional, ILgpdAuditService lgpdAudit, ILogger<LgpdService> logger)
        : base(cfg, audit, logOperacional, logger)
    {
        this.lgpdAudit = lgpdAudit;
    }

    public async Task<ApiResponse<LgpdPoliticaDto>> PoliticaAtualAsync()
    {
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var row = await cn.QueryFirstOrDefaultAsync<LgpdPoliticaDto>(@"select id as ""Id"", coalesce(versao,'') as ""Versao"", coalesce(titulo,'') as ""Titulo"", coalesce(conteudo,'') as ""Conteudo"", vigente_desde as ""VigenteDesde""
from plantaopro.lgpd_politicas where ativo=true and reg_status='A' order by vigente_desde desc limit 1");
        return row is null ? ApiResponse<LgpdPoliticaDto>.Fail("Política de privacidade não cadastrada.", 404) : ApiResponse<LgpdPoliticaDto>.Ok(row);
    }

    public async Task<ApiResponse<IEnumerable<LgpdConsentimentoDto>>> ConsentimentosAsync(ClaimsPrincipal user)
    {
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var usuarioId = GetUserId(user);
        var clienteId = GetClienteId(user);
        var sql = @"select id as ""Id"", usuario_id as ""UsuarioId"", cliente_id as ""ClienteId"", coalesce(versao_politica,'') as ""VersaoPolitica"", coalesce(finalidade,'') as ""Finalidade"", coalesce(base_legal,'') as ""BaseLegal"", consentido as ""Consentido"", data_consentimento as ""DataConsentimento""
from plantaopro.lgpd_consentimentos where reg_status='A'";
        object args;
        if (IsAdminGlobal(user))
        {
            sql += " order by data_consentimento desc limit 200";
            args = new { };
        }
        else
        {
            sql += " and (usuario_id=@usuarioId or cliente_id=@clienteId) order by data_consentimento desc limit 100";
            args = new { usuarioId, clienteId };
        }

        var rows = await cn.QueryAsync<LgpdConsentimentoDto>(sql, args);
        return ApiResponse<IEnumerable<LgpdConsentimentoDto>>.Ok(rows);
    }

    public async Task<ApiResponse<LgpdConsentimentoDto>> RegistrarConsentimentoAsync(ClaimsPrincipal user, RegistrarConsentimentoRequest request, HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(request.Finalidade) || string.IsNullOrWhiteSpace(request.BaseLegal)) return ApiResponse<LgpdConsentimentoDto>.Fail("Finalidade e base legal são obrigatórias.", 400);
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();
        try
        {
            var politica = await cn.QueryFirstOrDefaultAsync<LgpdPoliticaDto>(@"select id as ""Id"", coalesce(versao,'') as ""Versao"", coalesce(titulo,'') as ""Titulo"", coalesce(conteudo,'') as ""Conteudo"", vigente_desde as ""VigenteDesde"" from plantaopro.lgpd_politicas where ativo=true and reg_status='A' order by vigente_desde desc limit 1", transaction: tx);
            if (politica is null)
            {
                await tx.RollbackAsync();
                return ApiResponse<LgpdConsentimentoDto>.Fail("Política vigente não encontrada.", 404);
            }
            var id = Guid.NewGuid();
            var usuarioId = GetUserId(user);
            var clienteId = GetClienteId(user);
            var ip = httpContext.Connection.RemoteIpAddress?.ToString();
            var ua = httpContext.Request.Headers.UserAgent.ToString();
            await cn.ExecuteAsync(@"insert into plantaopro.lgpd_consentimentos(id, usuario_id, cliente_id, politica_id, versao_politica, finalidade, base_legal, consentido, ip, user_agent, data_consentimento, reg_status, reg_date)
values(@id, @usuarioId, @clienteId, @politicaId, @versao, @finalidade, @baseLegal, @consentido, @ip, @ua, now(), 'A', now())", new { id, usuarioId, clienteId, politicaId = politica.Id, versao = politica.Versao, finalidade = request.Finalidade, baseLegal = request.BaseLegal, consentido = request.Consentido, ip, ua }, tx);
            await tx.CommitAsync();
            await lgpdAudit.RegistrarEventoAsync(usuarioId, clienteId, "CONSENTIMENTO_REGISTRADO", "LGPD_CONSENTIMENTO", id, new { request.Finalidade, request.BaseLegal, request.Consentido }, ip, ua);
            await AuditarAsync(user, "LGPD_CONSENTIMENTO", id, "REGISTRAR", new { request.Finalidade, request.BaseLegal, request.Consentido }, true, httpContext, clienteId);
            return ApiResponse<LgpdConsentimentoDto>.Ok(new LgpdConsentimentoDto { Id = id, UsuarioId = usuarioId, ClienteId = clienteId, VersaoPolitica = politica.Versao, Finalidade = request.Finalidade, BaseLegal = request.BaseLegal, Consentido = request.Consentido, DataConsentimento = DateTime.UtcNow }, "Consentimento registrado.");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ApiResponse<IEnumerable<LgpdSolicitacaoDto>>> SolicitacoesAsync(ClaimsPrincipal user)
    {
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var usuarioId = GetUserId(user);
        var clienteId = GetClienteId(user);
        var sql = @"select id as ""Id"", usuario_id as ""UsuarioId"", cliente_id as ""ClienteId"", coalesce(tipo,'') as ""Tipo"", coalesce(status,'') as ""Status"", coalesce(resumo,'') as ""Resumo"", coalesce(resposta,'') as ""Resposta"", reg_date as ""RegDate"" from plantaopro.lgpd_solicitacoes_titular where reg_status='A'";
        object args;
        if (IsAdminGlobal(user))
        {
            sql += " order by reg_date desc limit 200";
            args = new { };
        }
        else
        {
            sql += " and (usuario_id=@usuarioId or cliente_id=@clienteId) order by reg_date desc limit 100";
            args = new { usuarioId, clienteId };
        }
        var rows = await cn.QueryAsync<LgpdSolicitacaoDto>(sql, args);
        return ApiResponse<IEnumerable<LgpdSolicitacaoDto>>.Ok(rows);
    }

    public async Task<ApiResponse<LgpdSolicitacaoDto>> CriarSolicitacaoAsync(ClaimsPrincipal user, CriarSolicitacaoLgpdRequest request, HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(request.Tipo) || string.IsNullOrWhiteSpace(request.Resumo)) return ApiResponse<LgpdSolicitacaoDto>.Fail("Tipo e resumo da solicitação são obrigatórios.", 400);
        var tipo = request.Tipo.Trim().ToUpperInvariant();
        var tipos = new[] { "EXPORTACAO", "CORRECAO", "ANONIMIZACAO", "EXCLUSAO", "INFORMACAO" };
        if (!tipos.Contains(tipo)) return ApiResponse<LgpdSolicitacaoDto>.Fail("Tipo LGPD inválido.", 400);
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();
        try
        {
            var id = Guid.NewGuid();
            var usuarioId = GetUserId(user);
            var clienteId = GetClienteId(user);
            var ip = httpContext.Connection.RemoteIpAddress?.ToString();
            var ua = httpContext.Request.Headers.UserAgent.ToString();
            await cn.ExecuteAsync(@"insert into plantaopro.lgpd_solicitacoes_titular(id, usuario_id, cliente_id, tipo, status, resumo, ip, user_agent, reg_status, reg_date)
values(@id, @usuarioId, @clienteId, @tipo, 'ABERTA', @resumo, @ip, @ua, 'A', now())", new { id, usuarioId, clienteId, tipo, resumo = request.Resumo, ip, ua }, tx);
            await tx.CommitAsync();
            await lgpdAudit.RegistrarEventoAsync(usuarioId, clienteId, "SOLICITACAO_TITULAR_CRIADA", "LGPD_SOLICITACAO", id, new { tipo }, ip, ua);
            await AuditarAsync(user, "LGPD_SOLICITACAO", id, "CRIAR", new { tipo }, true, httpContext, clienteId);
            return ApiResponse<LgpdSolicitacaoDto>.Ok(new LgpdSolicitacaoDto { Id = id, UsuarioId = usuarioId, ClienteId = clienteId, Tipo = tipo, Status = "ABERTA", Resumo = request.Resumo, RegDate = DateTime.UtcNow }, "Solicitação LGPD registrada.");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ApiResponse<string>> ResponderSolicitacaoAsync(ClaimsPrincipal user, Guid id, ResponderSolicitacaoLgpdRequest request, HttpContext httpContext)
    {
        if (!IsAdminGlobal(user)) return ApiResponse<string>.Fail("Apenas administradores autorizados podem responder solicitações LGPD.", 403);
        if (string.IsNullOrWhiteSpace(request.Status) || string.IsNullOrWhiteSpace(request.Resposta)) return ApiResponse<string>.Fail("Status e resposta são obrigatórios.", 400);
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var rows = await cn.ExecuteAsync(@"update plantaopro.lgpd_solicitacoes_titular set status=@status, resposta=@resposta, respondido_por=@usuarioId, respondido_em=now(), reg_update=now() where id=@id and reg_status='A'", new { id, status = request.Status.ToUpperInvariant(), resposta = request.Resposta, usuarioId = GetUserId(user) });
        if (rows == 0) return ApiResponse<string>.Fail("Solicitação não encontrada.", 404);
        await AuditarAsync(user, "LGPD_SOLICITACAO", id, "RESPONDER", new { request.Status }, true, httpContext);
        return ApiResponse<string>.Ok("ok", "Solicitação respondida.");
    }

    public async Task<ApiResponse<object>> ExportarMeusDadosAsync(ClaimsPrincipal user, HttpContext httpContext)
    {
        var usuarioId = GetUserId(user);
        var clienteId = GetClienteId(user);
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var perfil = GetPerfil(user);
        var consentimentos = await cn.QueryAsync<LgpdConsentimentoDto>(@"select id as ""Id"", usuario_id as ""UsuarioId"", cliente_id as ""ClienteId"", coalesce(versao_politica,'') as ""VersaoPolitica"", coalesce(finalidade,'') as ""Finalidade"", coalesce(base_legal,'') as ""BaseLegal"", consentido as ""Consentido"", data_consentimento as ""DataConsentimento"" from plantaopro.lgpd_consentimentos where (usuario_id=@usuarioId or cliente_id=@clienteId) and reg_status='A' order by data_consentimento desc limit 100", new { usuarioId, clienteId });
        var solicitacoes = await cn.QueryAsync<LgpdSolicitacaoDto>(@"select id as ""Id"", usuario_id as ""UsuarioId"", cliente_id as ""ClienteId"", coalesce(tipo,'') as ""Tipo"", coalesce(status,'') as ""Status"", coalesce(resumo,'') as ""Resumo"", coalesce(resposta,'') as ""Resposta"", reg_date as ""RegDate"" from plantaopro.lgpd_solicitacoes_titular where (usuario_id=@usuarioId or cliente_id=@clienteId) and reg_status='A' order by reg_date desc limit 100", new { usuarioId, clienteId });
        var exportacao = new { usuarioId, clienteId, perfil, geradoEmUtc = DateTime.UtcNow, consentimentos, solicitacoes, aviso = "Dados financeiros, auditoria e obrigações legais podem estar sujeitos a retenção legal e não são removidos automaticamente." };
        await cn.ExecuteAsync(@"insert into plantaopro.lgpd_exportacoes_dados(id, usuario_id, cliente_id, formato, resumo, ip, user_agent, reg_date)
values(gen_random_uuid(), @usuarioId, @clienteId, 'JSON', cast(@resumo as jsonb), @ip, @ua, now())", new { usuarioId, clienteId, resumo = JsonSerializer.Serialize(exportacao), ip = httpContext.Connection.RemoteIpAddress?.ToString(), ua = httpContext.Request.Headers.UserAgent.ToString() });
        await AuditarAsync(user, "LGPD_EXPORTACAO", null, "EXPORTAR_DADOS", new { escopo = "meus_dados" }, true, httpContext, clienteId);
        return ApiResponse<object>.Ok(exportacao, "Exportação gerada e auditada.");
    }

    public async Task<ApiResponse<string>> AnonimizarAsync(ClaimsPrincipal user, Guid usuarioId, HttpContext httpContext)
    {
        if (!IsAdminGlobal(user) && GetUserId(user) != usuarioId) return ApiResponse<string>.Fail("Você só pode solicitar anonimização dos próprios dados.", 403);
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var possuiFinanceiro = await cn.ExecuteScalarAsync<bool>(@"select exists(select 1 from plantaopro.pagamentos p where p.medico_id=@usuarioId) or exists(select 1 from plantaopro.auditoria_acoes_criticas a where a.usuario_id=@usuarioId)", new { usuarioId });
        var permitido = !possuiFinanceiro;
        var motivo = permitido ? "Anonimização permitida para dados cadastrais sem obrigação legal bloqueante." : "Anonimização total bloqueada por retenção legal de financeiro/auditoria; executar mascaramento parcial quando houver rotina específica.";
        await cn.ExecuteAsync(@"insert into plantaopro.lgpd_anonimizacoes(id, usuario_id, cliente_id, permitido, motivo, campos_anonimizados, executado_por, ip, reg_date)
values(gen_random_uuid(), @usuarioId, @clienteId, @permitido, @motivo, @campos, @executor, @ip, now())", new { usuarioId, clienteId = GetClienteId(user), permitido, motivo, campos = permitido ? "email, telefone, nome" : null, executor = GetUserId(user), ip = httpContext.Connection.RemoteIpAddress?.ToString() });
        await AuditarAsync(user, "LGPD_ANONIMIZACAO", usuarioId, permitido ? "ANONIMIZAR" : "BLOQUEAR_ANONIMIZACAO", new { motivo }, permitido, httpContext);
        return permitido ? ApiResponse<string>.Ok("ok", motivo) : ApiResponse<string>.Fail(motivo, 409);
    }

    public async Task<ApiResponse<IEnumerable<LgpdEventoDto>>> EventosAsync(ClaimsPrincipal user)
    {
        if (!IsAdminGlobal(user)) return ApiResponse<IEnumerable<LgpdEventoDto>>.Fail("Acesso restrito à administração LGPD.", 403);
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<LgpdEventoDto>(@"select id as ""Id"", usuario_id as ""UsuarioId"", cliente_id as ""ClienteId"", coalesce(evento,'') as ""Evento"", coalesce(entidade,'') as ""Entidade"", entidade_id as ""EntidadeId"", reg_date as ""RegDate"" from plantaopro.lgpd_eventos_privacidade order by reg_date desc limit 200");
        return ApiResponse<IEnumerable<LgpdEventoDto>>.Ok(rows);
    }

    public async Task<ApiResponse<IEnumerable<LgpdRetencaoDto>>> RetencaoAsync()
    {
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<LgpdRetencaoDto>(@"select id as ""Id"", coalesce(categoria,'') as ""Categoria"", coalesce(base_legal,'') as ""BaseLegal"", prazo_meses as ""PrazoMeses"", coalesce(observacoes,'') as ""Observacoes"" from plantaopro.lgpd_retencao_dados where reg_status='A' order by categoria limit 100");
        return ApiResponse<IEnumerable<LgpdRetencaoDto>>.Ok(rows);
    }
}

public sealed class JornadaClienteService : SaasEvolutionServiceBase
{
    private static readonly string[] Etapas = new[] { "LEAD_CADASTRADO", "DEMONSTRACAO_AGENDADA", "DEMONSTRACAO_REALIZADA", "PROPOSTA_ENVIADA", "CLIENTE_EM_NEGOCIACAO", "CLIENTE_CONVERTIDO", "CLIENTE_EM_IMPLANTACAO", "CLIENTE_EM_TREINAMENTO", "CLIENTE_EM_OPERACAO_ASSISTIDA", "CLIENTE_ATIVO", "CLIENTE_EM_EXPANSAO", "CLIENTE_EM_RISCO", "CLIENTE_CANCELADO" };

    public JornadaClienteService(IConfiguration cfg, IAuditService audit, ILogOperacionalService logOperacional, ILogger<JornadaClienteService> logger) : base(cfg, audit, logOperacional, logger) { }

    public async Task<ApiResponse<IEnumerable<JornadaClienteDto>>> ListarAsync(ClaimsPrincipal user)
    {
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var sql = @"select j.id as ""Id"", j.cliente_id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"", coalesce(j.etapa,'') as ""Etapa"", j.score as ""Score"", coalesce(j.responsavel,'') as ""Responsavel"", coalesce(j.proxima_acao,'') as ""ProximaAcao"", j.reg_date as ""RegDate"" from plantaopro.jornada_cliente j left join plantaopro.clientes c on c.id=j.cliente_id where j.reg_status='A'";
        object args;
        if (IsAdminGlobal(user)) { sql += " order by j.reg_update desc nulls last, j.reg_date desc limit 100"; args = new { }; }
        else { sql += " and j.cliente_id=@clienteId order by j.reg_date desc limit 100"; args = new { clienteId = GetClienteId(user) }; }
        return ApiResponse<IEnumerable<JornadaClienteDto>>.Ok(await cn.QueryAsync<JornadaClienteDto>(sql, args));
    }

    public async Task<ApiResponse<JornadaClienteDto>> ObterAsync(ClaimsPrincipal user, Guid clienteId)
    {
        if (!PodeVerCliente(user, clienteId)) return ApiResponse<JornadaClienteDto>.Fail("Acesso negado ao cliente informado.", 403);
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var row = await ObterOuCriarAsync(cn, clienteId);
        return ApiResponse<JornadaClienteDto>.Ok(row);
    }

    public async Task<ApiResponse<JornadaClienteDto>> AvancarAsync(ClaimsPrincipal user, Guid clienteId, MudarEtapaJornadaRequest request, HttpContext httpContext)
        => await MudarEtapaAsync(user, clienteId, request, true, httpContext);

    public async Task<ApiResponse<JornadaClienteDto>> RetrocederAsync(ClaimsPrincipal user, Guid clienteId, MudarEtapaJornadaRequest request, HttpContext httpContext)
        => await MudarEtapaAsync(user, clienteId, request, false, httpContext);

    private async Task<ApiResponse<JornadaClienteDto>> MudarEtapaAsync(ClaimsPrincipal user, Guid clienteId, MudarEtapaJornadaRequest request, bool avancar, HttpContext httpContext)
    {
        if (!PodeVerCliente(user, clienteId)) return ApiResponse<JornadaClienteDto>.Fail("Acesso negado ao cliente informado.", 403);
        if (string.IsNullOrWhiteSpace(request.Motivo)) return ApiResponse<JornadaClienteDto>.Fail(avancar ? "Avançar etapa exige motivo/resumo." : "Retroceder etapa exige justificativa.", 400);
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();
        try
        {
            var jornada = await ObterOuCriarAsync(cn, clienteId, tx);
            var indice = Array.IndexOf(Etapas, jornada.Etapa);
            if (indice < 0) indice = 0;
            var novoIndice = avancar ? Math.Min(indice + 1, Etapas.Length - 1) : Math.Max(indice - 1, 0);
            var novaEtapa = Etapas[novoIndice];
            if (novaEtapa == "CLIENTE_ATIVO")
            {
                var possuiAssinatura = await cn.ExecuteScalarAsync<bool>("select exists(select 1 from plantaopro.assinaturas_saas where cliente_id=@clienteId and status in ('ATIVA','TRIAL') and reg_status='A')", new { clienteId }, tx);
                if (!possuiAssinatura)
                {
                    await tx.RollbackAsync();
                    return ApiResponse<JornadaClienteDto>.Fail("Conversão para cliente ativo exige plano e assinatura SaaS ativa.", 409);
                }
            }
            if (novaEtapa == "CLIENTE_CANCELADO" && request.Motivo.Trim().Length < 10)
            {
                await tx.RollbackAsync();
                return ApiResponse<JornadaClienteDto>.Fail("Cliente cancelado exige motivo detalhado.", 400);
            }
            await cn.ExecuteAsync(@"update plantaopro.jornada_cliente set etapa=@novaEtapa, score=@score, responsavel=@responsavel, proxima_acao=@proximaAcao, reg_update=now() where id=@id", new { novaEtapa, score = novoIndice * 10, responsavel = request.Responsavel, proximaAcao = request.ProximaAcao, id = jornada.Id }, tx);
            await cn.ExecuteAsync(@"insert into plantaopro.jornada_cliente_eventos(id, cliente_id, jornada_id, etapa_anterior, etapa_nova, tipo, resumo, usuario_id, reg_date)
values(gen_random_uuid(), @clienteId, @jornadaId, @anterior, @nova, @tipo, @resumo, @usuarioId, now())", new { clienteId, jornadaId = jornada.Id, anterior = jornada.Etapa, nova = novaEtapa, tipo = avancar ? "AVANCO" : "RETROCESSO", resumo = request.Motivo, usuarioId = GetUserId(user) }, tx);
            if (novaEtapa == "CLIENTE_EM_IMPLANTACAO" || novaEtapa == "CLIENTE_EM_RISCO")
            {
                await cn.ExecuteAsync(@"insert into plantaopro.jornada_cliente_tarefas(id, cliente_id, titulo, descricao, responsavel, prazo, status, reg_status, reg_date)
values(gen_random_uuid(), @clienteId, @titulo, @descricao, @responsavel, now() + interval '3 days', 'PENDENTE', 'A', now())", new { clienteId, titulo = novaEtapa == "CLIENTE_EM_IMPLANTACAO" ? "Abrir checklist de operação assistida" : "Ação de Customer Success para cliente em risco", descricao = request.Motivo, responsavel = request.Responsavel }, tx);
            }
            await tx.CommitAsync();
            await AuditarAsync(user, "JORNADA_CLIENTE", jornada.Id, avancar ? "AVANCAR" : "RETROCEDER", new { anterior = jornada.Etapa, nova = novaEtapa, request.Motivo }, true, httpContext, clienteId);
            jornada.Etapa = novaEtapa;
            jornada.Score = novoIndice * 10;
            jornada.Responsavel = request.Responsavel ?? string.Empty;
            jornada.ProximaAcao = request.ProximaAcao ?? string.Empty;
            return ApiResponse<JornadaClienteDto>.Ok(jornada, "Etapa da jornada atualizada.");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ApiResponse<JornadaEventoDto>> CriarEventoAsync(ClaimsPrincipal user, Guid clienteId, CriarEventoJornadaRequest request, HttpContext httpContext)
    {
        if (!PodeVerCliente(user, clienteId)) return ApiResponse<JornadaEventoDto>.Fail("Acesso negado ao cliente informado.", 403);
        if (string.IsNullOrWhiteSpace(request.Tipo) || string.IsNullOrWhiteSpace(request.Resumo)) return ApiResponse<JornadaEventoDto>.Fail("Tipo e resumo são obrigatórios.", 400);
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var jornada = await ObterOuCriarAsync(cn, clienteId);
        var id = Guid.NewGuid();
        await cn.ExecuteAsync(@"insert into plantaopro.jornada_cliente_eventos(id, cliente_id, jornada_id, etapa_nova, tipo, resumo, usuario_id, reg_date) values(@id, @clienteId, @jornadaId, @etapa, @tipo, @resumo, @usuarioId, now())", new { id, clienteId, jornadaId = jornada.Id, etapa = jornada.Etapa, tipo = request.Tipo, resumo = request.Resumo, usuarioId = GetUserId(user) });
        await AuditarAsync(user, "JORNADA_EVENTO", id, "CRIAR", new { request.Tipo }, true, httpContext, clienteId);
        return ApiResponse<JornadaEventoDto>.Ok(new JornadaEventoDto { Id = id, ClienteId = clienteId, EtapaNova = jornada.Etapa, Tipo = request.Tipo, Resumo = request.Resumo, RegDate = DateTime.UtcNow }, "Evento registrado.");
    }

    public async Task<ApiResponse<JornadaTarefaDto>> CriarTarefaAsync(ClaimsPrincipal user, Guid clienteId, CriarTarefaJornadaRequest request, HttpContext httpContext)
    {
        if (!PodeVerCliente(user, clienteId)) return ApiResponse<JornadaTarefaDto>.Fail("Acesso negado ao cliente informado.", 403);
        if (string.IsNullOrWhiteSpace(request.Titulo)) return ApiResponse<JornadaTarefaDto>.Fail("Título da tarefa é obrigatório.", 400);
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var id = Guid.NewGuid();
        await cn.ExecuteAsync(@"insert into plantaopro.jornada_cliente_tarefas(id, cliente_id, titulo, descricao, responsavel, prazo, status, reg_status, reg_date) values(@id, @clienteId, @titulo, @descricao, @responsavel, @prazo, 'PENDENTE', 'A', now())", new { id, clienteId, titulo = request.Titulo, descricao = request.Descricao, responsavel = request.Responsavel, prazo = request.Prazo });
        await AuditarAsync(user, "JORNADA_TAREFA", id, "CRIAR", new { request.Titulo }, true, httpContext, clienteId);
        return ApiResponse<JornadaTarefaDto>.Ok(new JornadaTarefaDto { Id = id, ClienteId = clienteId, Titulo = request.Titulo, Descricao = request.Descricao ?? string.Empty, Responsavel = request.Responsavel ?? string.Empty, Prazo = request.Prazo, Status = "PENDENTE" }, "Tarefa criada.");
    }

    public async Task<ApiResponse<string>> ConcluirTarefaAsync(ClaimsPrincipal user, Guid id, HttpContext httpContext)
    {
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var clienteId = await cn.ExecuteScalarAsync<Guid?>("select cliente_id from plantaopro.jornada_cliente_tarefas where id=@id and reg_status='A'", new { id });
        if (!clienteId.HasValue) return ApiResponse<string>.Fail("Tarefa não encontrada.", 404);
        if (!PodeVerCliente(user, clienteId.Value)) return ApiResponse<string>.Fail("Acesso negado ao cliente da tarefa.", 403);
        await cn.ExecuteAsync("update plantaopro.jornada_cliente_tarefas set status='CONCLUIDA', concluida_em=now(), reg_update=now() where id=@id", new { id });
        await AuditarAsync(user, "JORNADA_TAREFA", id, "CONCLUIR", null, true, httpContext, clienteId.Value);
        return ApiResponse<string>.Ok("ok", "Tarefa concluída.");
    }

    public async Task<ApiResponse<IEnumerable<FunilItemDto>>> FunilAsync(ClaimsPrincipal user)
    {
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var sql = "select etapa as \"Etapa\", count(1)::bigint as \"Quantidade\" from plantaopro.jornada_cliente where reg_status='A'";
        object args;
        if (IsAdminGlobal(user)) { args = new { }; }
        else { sql += " and cliente_id=@clienteId"; args = new { clienteId = GetClienteId(user) }; }
        sql += " group by etapa order by min(reg_date)";
        return ApiResponse<IEnumerable<FunilItemDto>>.Ok(await cn.QueryAsync<FunilItemDto>(sql, args));
    }

    public async Task<ApiResponse<IEnumerable<JornadaTarefaDto>>> TarefasAsync(ClaimsPrincipal user)
    {
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var sql = @"select t.id as ""Id"", t.cliente_id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"", coalesce(t.titulo,'') as ""Titulo"", coalesce(t.descricao,'') as ""Descricao"", coalesce(t.responsavel,'') as ""Responsavel"", t.prazo as ""Prazo"", coalesce(t.status,'') as ""Status"" from plantaopro.jornada_cliente_tarefas t left join plantaopro.clientes c on c.id=t.cliente_id where t.reg_status='A'";
        object args;
        if (IsAdminGlobal(user)) { sql += " order by t.prazo nulls last, t.reg_date desc limit 100"; args = new { }; }
        else { sql += " and t.cliente_id=@clienteId order by t.prazo nulls last limit 100"; args = new { clienteId = GetClienteId(user) }; }
        return ApiResponse<IEnumerable<JornadaTarefaDto>>.Ok(await cn.QueryAsync<JornadaTarefaDto>(sql, args));
    }

    private static async Task<JornadaClienteDto> ObterOuCriarAsync(NpgsqlConnection cn, Guid clienteId, NpgsqlTransaction? tx = null)
    {
        var row = await cn.QueryFirstOrDefaultAsync<JornadaClienteDto>(@"select j.id as ""Id"", j.cliente_id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"", coalesce(j.etapa,'') as ""Etapa"", j.score as ""Score"", coalesce(j.responsavel,'') as ""Responsavel"", coalesce(j.proxima_acao,'') as ""ProximaAcao"", j.reg_date as ""RegDate"" from plantaopro.jornada_cliente j left join plantaopro.clientes c on c.id=j.cliente_id where j.cliente_id=@clienteId and j.reg_status='A' order by j.reg_date desc limit 1", new { clienteId }, tx);
        if (row is not null) return row;
        var id = Guid.NewGuid();
        await cn.ExecuteAsync("insert into plantaopro.jornada_cliente(id, cliente_id, etapa, score, reg_status, reg_date) values(@id, @clienteId, 'LEAD_CADASTRADO', 0, 'A', now())", new { id, clienteId }, tx);
        return new JornadaClienteDto { Id = id, ClienteId = clienteId, Etapa = "LEAD_CADASTRADO", Score = 0, RegDate = DateTime.UtcNow };
    }
}

public sealed class ComercialService : SaasEvolutionServiceBase
{
    public ComercialService(IConfiguration cfg, IAuditService audit, ILogOperacionalService logOperacional, ILogger<ComercialService> logger) : base(cfg, audit, logOperacional, logger) { }

    public async Task<ApiResponse<IEnumerable<ComercialLeadDto>>> LeadsAsync()
    {
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<ComercialLeadDto>(@"select id as ""Id"", coalesce(nome,'') as ""Nome"", coalesce(empresa,'') as ""Empresa"", coalesce(email,'') as ""Email"", coalesce(telefone,'') as ""Telefone"", coalesce(origem,'') as ""Origem"", coalesce(status,'') as ""Status"", medicos_desejados as ""MedicosDesejados"", hospitais as ""Hospitais"", plantoes_mes as ""PlantoesMes"", precisa_mobile as ""PrecisaMobile"", precisa_bi as ""PrecisaBi"", suporte_prioritario as ""SuportePrioritario"", operacao_assistida as ""OperacaoAssistida"", reg_date as ""RegDate"" from plantaopro.comercial_leads where reg_status='A' order by reg_date desc limit 100");
        return ApiResponse<IEnumerable<ComercialLeadDto>>.Ok(rows);
    }

    public async Task<ApiResponse<ComercialLeadDto>> CriarLeadAsync(ClaimsPrincipal user, ComercialLeadRequest request, HttpContext httpContext)
    {
        var erro = ValidarLead(request);
        if (erro is not null) return ApiResponse<ComercialLeadDto>.Fail(erro, 400);
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var id = Guid.NewGuid();
        await cn.ExecuteAsync(@"insert into plantaopro.comercial_leads(id,nome,empresa,email,telefone,origem,status,medicos_desejados,hospitais,plantoes_mes,precisa_mobile,precisa_bi,suporte_prioritario,operacao_assistida,reg_status,reg_date)
values(@id,@Nome,@Empresa,@Email,@Telefone,@Origem,'NOVO',@MedicosDesejados,@Hospitais,@PlantoesMes,@PrecisaMobile,@PrecisaBi,@SuportePrioritario,@OperacaoAssistida,'A',now())", new { id, request.Nome, request.Empresa, request.Email, request.Telefone, request.Origem, request.MedicosDesejados, request.Hospitais, request.PlantoesMes, request.PrecisaMobile, request.PrecisaBi, request.SuportePrioritario, request.OperacaoAssistida });
        await AuditarAsync(user, "COMERCIAL_LEAD", id, "CRIAR", new { request.Empresa, request.Email }, true, httpContext);
        var dto = new ComercialLeadDto { Id = id, Nome = request.Nome, Empresa = request.Empresa, Email = request.Email, Telefone = request.Telefone ?? string.Empty, Origem = request.Origem ?? string.Empty, Status = "NOVO", MedicosDesejados = request.MedicosDesejados, Hospitais = request.Hospitais, PlantoesMes = request.PlantoesMes, PrecisaMobile = request.PrecisaMobile, PrecisaBi = request.PrecisaBi, SuportePrioritario = request.SuportePrioritario, OperacaoAssistida = request.OperacaoAssistida, RegDate = DateTime.UtcNow };
        return ApiResponse<ComercialLeadDto>.Ok(dto, "Lead criado.");
    }

    public async Task<ApiResponse<ComercialLeadDto>> AtualizarLeadAsync(ClaimsPrincipal user, Guid id, ComercialLeadRequest request, HttpContext httpContext)
    {
        var erro = ValidarLead(request);
        if (erro is not null) return ApiResponse<ComercialLeadDto>.Fail(erro, 400);
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var rows = await cn.ExecuteAsync(@"update plantaopro.comercial_leads set nome=@Nome, empresa=@Empresa, email=@Email, telefone=@Telefone, origem=@Origem, medicos_desejados=@MedicosDesejados, hospitais=@Hospitais, plantoes_mes=@PlantoesMes, precisa_mobile=@PrecisaMobile, precisa_bi=@PrecisaBi, suporte_prioritario=@SuportePrioritario, operacao_assistida=@OperacaoAssistida, reg_update=now() where id=@id and reg_status='A'", new { id, request.Nome, request.Empresa, request.Email, request.Telefone, request.Origem, request.MedicosDesejados, request.Hospitais, request.PlantoesMes, request.PrecisaMobile, request.PrecisaBi, request.SuportePrioritario, request.OperacaoAssistida });
        if (rows == 0) return ApiResponse<ComercialLeadDto>.Fail("Lead não encontrado.", 404);
        await AuditarAsync(user, "COMERCIAL_LEAD", id, "EDITAR", new { request.Empresa }, true, httpContext);
        return ApiResponse<ComercialLeadDto>.Ok(new ComercialLeadDto { Id = id, Nome = request.Nome, Empresa = request.Empresa, Email = request.Email, Telefone = request.Telefone ?? string.Empty, Origem = request.Origem ?? string.Empty, Status = "ATUALIZADO", MedicosDesejados = request.MedicosDesejados, Hospitais = request.Hospitais, PlantoesMes = request.PlantoesMes, PrecisaMobile = request.PrecisaMobile, PrecisaBi = request.PrecisaBi, SuportePrioritario = request.SuportePrioritario, OperacaoAssistida = request.OperacaoAssistida }, "Lead atualizado.");
    }

    public async Task<ApiResponse<IEnumerable<ComercialOportunidadeDto>>> OportunidadesAsync()
    {
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<ComercialOportunidadeDto>(@"select id as ""Id"", lead_id as ""LeadId"", cliente_id as ""ClienteId"", coalesce(titulo,'') as ""Titulo"", coalesce(etapa,'') as ""Etapa"", valor_estimado as ""ValorEstimado"", coalesce(plano_recomendado,'') as ""PlanoRecomendado"", probabilidade as ""Probabilidade"", coalesce(status,'') as ""Status"", coalesce(motivo_perda,'') as ""MotivoPerda"", reg_date as ""RegDate"" from plantaopro.comercial_oportunidades where reg_status='A' order by reg_date desc limit 100");
        return ApiResponse<IEnumerable<ComercialOportunidadeDto>>.Ok(rows);
    }

    public async Task<ApiResponse<ComercialOportunidadeDto>> CriarOportunidadeAsync(ClaimsPrincipal user, ComercialOportunidadeRequest request, HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(request.Titulo)) return ApiResponse<ComercialOportunidadeDto>.Fail("Título da oportunidade é obrigatório.", 400);
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var id = Guid.NewGuid();
        await cn.ExecuteAsync(@"insert into plantaopro.comercial_oportunidades(id,lead_id,cliente_id,titulo,etapa,valor_estimado,plano_recomendado,probabilidade,status,reg_status,reg_date)
values(@id,@LeadId,@ClienteId,@Titulo,'QUALIFICACAO',@ValorEstimado,@PlanoRecomendado,@Probabilidade,'ABERTA','A',now())", new { id, request.LeadId, request.ClienteId, request.Titulo, request.ValorEstimado, request.PlanoRecomendado, request.Probabilidade });
        if (request.LeadId.HasValue) await cn.ExecuteAsync("update plantaopro.comercial_leads set status='OPORTUNIDADE', reg_update=now() where id=@id", new { id = request.LeadId.Value });
        await AuditarAsync(user, "COMERCIAL_OPORTUNIDADE", id, "CRIAR", new { request.Titulo }, true, httpContext, request.ClienteId);
        return ApiResponse<ComercialOportunidadeDto>.Ok(new ComercialOportunidadeDto { Id = id, LeadId = request.LeadId, ClienteId = request.ClienteId, Titulo = request.Titulo, Etapa = "QUALIFICACAO", ValorEstimado = request.ValorEstimado, PlanoRecomendado = request.PlanoRecomendado ?? string.Empty, Probabilidade = request.Probabilidade, Status = "ABERTA" }, "Oportunidade criada.");
    }

    public async Task<ApiResponse<ComercialOportunidadeDto>> AtualizarOportunidadeAsync(ClaimsPrincipal user, Guid id, ComercialOportunidadeRequest request, HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(request.Titulo)) return ApiResponse<ComercialOportunidadeDto>.Fail("Título da oportunidade é obrigatório.", 400);
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var rows = await cn.ExecuteAsync("update plantaopro.comercial_oportunidades set titulo=@Titulo, valor_estimado=@ValorEstimado, plano_recomendado=@PlanoRecomendado, probabilidade=@Probabilidade, reg_update=now() where id=@id and reg_status='A'", new { id, request.Titulo, request.ValorEstimado, request.PlanoRecomendado, request.Probabilidade });
        if (rows == 0) return ApiResponse<ComercialOportunidadeDto>.Fail("Oportunidade não encontrada.", 404);
        await AuditarAsync(user, "COMERCIAL_OPORTUNIDADE", id, "EDITAR", new { request.Titulo }, true, httpContext, request.ClienteId);
        return ApiResponse<ComercialOportunidadeDto>.Ok(new ComercialOportunidadeDto { Id = id, LeadId = request.LeadId, ClienteId = request.ClienteId, Titulo = request.Titulo, ValorEstimado = request.ValorEstimado, PlanoRecomendado = request.PlanoRecomendado ?? string.Empty, Probabilidade = request.Probabilidade, Status = "ABERTA" }, "Oportunidade atualizada.");
    }

    public async Task<ApiResponse<string>> GanharOportunidadeAsync(ClaimsPrincipal user, Guid id, HttpContext httpContext)
    {
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var rows = await cn.ExecuteAsync("update plantaopro.comercial_oportunidades set status='GANHA', etapa='CLIENTE_CONVERTIDO', reg_update=now() where id=@id and reg_status='A'", new { id });
        if (rows == 0) return ApiResponse<string>.Fail("Oportunidade não encontrada.", 404);
        await AuditarAsync(user, "COMERCIAL_OPORTUNIDADE", id, "GANHAR", new { regra = "oportunidade ganha deve criar cliente quando lead ainda não possui cadastro" }, true, httpContext);
        return ApiResponse<string>.Ok("ok", "Oportunidade marcada como ganha. Conclua plano e assinatura no onboarding SaaS.");
    }

    public async Task<ApiResponse<string>> PerderOportunidadeAsync(ClaimsPrincipal user, Guid id, PerderOportunidadeRequest request, HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(request.Motivo)) return ApiResponse<string>.Fail("Oportunidade perdida exige motivo.", 400);
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var rows = await cn.ExecuteAsync("update plantaopro.comercial_oportunidades set status='PERDIDA', etapa='PERDIDA', motivo_perda=@motivo, reg_update=now() where id=@id and reg_status='A'", new { id, motivo = request.Motivo });
        if (rows == 0) return ApiResponse<string>.Fail("Oportunidade não encontrada.", 404);
        await AuditarAsync(user, "COMERCIAL_OPORTUNIDADE", id, "PERDER", new { motivoInformado = true }, true, httpContext);
        return ApiResponse<string>.Ok("ok", "Oportunidade marcada como perdida.");
    }

    public async Task<ApiResponse<IEnumerable<ComercialPropostaDto>>> PropostasAsync()
    {
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<ComercialPropostaDto>(@"select id as ""Id"", oportunidade_id as ""OportunidadeId"", coalesce(numero,'') as ""Numero"", coalesce(plano_nome,'') as ""PlanoNome"", valor_mensal as ""ValorMensal"", desconto_percentual as ""DescontoPercentual"", validade as ""Validade"", coalesce(status,'') as ""Status"", coalesce(observacoes,'') as ""Observacoes"" from plantaopro.comercial_propostas where reg_status='A' order by reg_date desc limit 100");
        return ApiResponse<IEnumerable<ComercialPropostaDto>>.Ok(rows);
    }

    public async Task<ApiResponse<ComercialPropostaDto>> CriarPropostaAsync(ClaimsPrincipal user, ComercialPropostaRequest request, HttpContext httpContext)
    {
        if (request.OportunidadeId == Guid.Empty || string.IsNullOrWhiteSpace(request.PlanoNome) || request.ValorMensal <= 0) return ApiResponse<ComercialPropostaDto>.Fail("Oportunidade, plano e valor são obrigatórios.", 400);
        if (request.Validade.Date < DateTime.UtcNow.Date) return ApiResponse<ComercialPropostaDto>.Fail("Proposta deve ter validade futura.", 400);
        if (request.DescontoPercentual > 20 && !IsAdminGlobal(user)) return ApiResponse<ComercialPropostaDto>.Fail("Desconto acima de 20% exige ADMINISTRADOR_GLOBAL.", 403);
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var id = Guid.NewGuid();
        var numero = "PROP-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        await cn.ExecuteAsync(@"insert into plantaopro.comercial_propostas(id,oportunidade_id,numero,plano_nome,valor_mensal,desconto_percentual,validade,status,observacoes,reg_status,reg_date)
values(@id,@OportunidadeId,@numero,@PlanoNome,@ValorMensal,@DescontoPercentual,@Validade,'RASCUNHO',@Observacoes,'A',now())", new { id, request.OportunidadeId, numero, request.PlanoNome, request.ValorMensal, request.DescontoPercentual, validade = request.Validade.Date, request.Observacoes });
        await AuditarAsync(user, "COMERCIAL_PROPOSTA", id, "CRIAR", new { request.PlanoNome, request.DescontoPercentual }, true, httpContext);
        return ApiResponse<ComercialPropostaDto>.Ok(new ComercialPropostaDto { Id = id, OportunidadeId = request.OportunidadeId, Numero = numero, PlanoNome = request.PlanoNome, ValorMensal = request.ValorMensal, DescontoPercentual = request.DescontoPercentual, Validade = request.Validade.Date, Status = "RASCUNHO", Observacoes = request.Observacoes ?? string.Empty }, "Proposta criada.");
    }

    public Task<ApiResponse<string>> EnviarPropostaAsync(ClaimsPrincipal user, Guid id, HttpContext httpContext) => AlterarStatusPropostaAsync(user, id, "ENVIADA", "ENVIAR", httpContext);
    public Task<ApiResponse<string>> RecusarPropostaAsync(ClaimsPrincipal user, Guid id, HttpContext httpContext) => AlterarStatusPropostaAsync(user, id, "RECUSADA", "RECUSAR", httpContext);

    public async Task<ApiResponse<string>> AprovarPropostaAsync(ClaimsPrincipal user, Guid id, HttpContext httpContext)
    {
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var validade = await cn.ExecuteScalarAsync<DateTime?>("select validade from plantaopro.comercial_propostas where id=@id and reg_status='A'", new { id });
        if (!validade.HasValue) return ApiResponse<string>.Fail("Proposta não encontrada.", 404);
        if (validade.Value.Date < DateTime.UtcNow.Date) return ApiResponse<string>.Fail("Proposta vencida não pode ser aprovada.", 409);
        return await AlterarStatusPropostaAsync(user, id, "APROVADA", "APROVAR", httpContext);
    }

    public async Task<ApiResponse<IEnumerable<FunilItemDto>>> FunilAsync()
    {
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<FunilItemDto>("select etapa as \"Etapa\", count(1)::bigint as \"Quantidade\" from plantaopro.comercial_oportunidades where reg_status='A' group by etapa order by etapa");
        return ApiResponse<IEnumerable<FunilItemDto>>.Ok(rows);
    }

    public async Task<ApiResponse<object>> PrevisaoReceitaAsync()
    {
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var row = await cn.QueryFirstAsync(@"select coalesce(sum(valor_estimado * probabilidade / 100.0),0)::numeric(12,2) as previsao, count(1)::bigint as oportunidades from plantaopro.comercial_oportunidades where status='ABERTA' and reg_status='A'");
        return ApiResponse<object>.Ok(new { previsao = (decimal)row.previsao, oportunidades = (long)row.oportunidades });
    }

    public ApiResponse<PlanoSugeridoDto> SugerirPlano(SugerirPlanoRequest request)
    {
        var recursos = new List<string>();
        if (request.PrecisaMobile) recursos.Add("Aplicativo mobile");
        if (request.PrecisaBi) recursos.Add("BI e relatórios avançados");
        if (request.SuportePrioritario) recursos.Add("Suporte prioritário");
        if (request.OperacaoAssistida) recursos.Add("Operação assistida");
        string plano;
        if (request.MedicosDesejados > 80 || request.Hospitais > 8 || request.PlantoesMes > 500 || request.SuportePrioritario || request.OperacaoAssistida) plano = "Enterprise";
        else if (request.MedicosDesejados > 25 || request.Hospitais > 3 || request.PlantoesMes > 150 || request.PrecisaBi) plano = "Profissional";
        else plano = "Essencial";
        return ApiResponse<PlanoSugeridoDto>.Ok(new PlanoSugeridoDto { Plano = plano, Justificativa = "Recomendação determinística baseada em médicos, hospitais, volume mensal, mobile, BI, suporte e operação assistida.", RecursosRecomendados = recursos });
    }

    private async Task<ApiResponse<string>> AlterarStatusPropostaAsync(ClaimsPrincipal user, Guid id, string status, string acao, HttpContext httpContext)
    {
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var rows = await cn.ExecuteAsync("update plantaopro.comercial_propostas set status=@status, reg_update=now() where id=@id and reg_status='A'", new { id, status });
        if (rows == 0) return ApiResponse<string>.Fail("Proposta não encontrada.", 404);
        await cn.ExecuteAsync("insert into plantaopro.comercial_interacoes(id, oportunidade_id, tipo, resumo, usuario_id, reg_date) select gen_random_uuid(), oportunidade_id, @acao, @resumo, @usuarioId, now() from plantaopro.comercial_propostas where id=@id", new { id, acao, resumo = "Proposta " + status.ToLowerInvariant(), usuarioId = GetUserId(user) });
        await AuditarAsync(user, "COMERCIAL_PROPOSTA", id, acao, new { status }, true, httpContext);
        return ApiResponse<string>.Ok("ok", "Proposta atualizada.");
    }

    private static string? ValidarLead(ComercialLeadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome)) return "Nome do lead é obrigatório.";
        if (string.IsNullOrWhiteSpace(request.Empresa)) return "Empresa do lead é obrigatória.";
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@')) return "E-mail válido é obrigatório.";
        if (request.MedicosDesejados < 0 || request.Hospitais < 0 || request.PlantoesMes < 0) return "Volumes comerciais não podem ser negativos.";
        return null;
    }
}

public sealed class AjudaInterativaService : SaasEvolutionServiceBase
{
    public AjudaInterativaService(IConfiguration cfg, IAuditService audit, ILogOperacionalService logOperacional, ILogger<AjudaInterativaService> logger) : base(cfg, audit, logOperacional, logger) { }

    public async Task<ApiResponse<IEnumerable<AjudaTopicoDto>>> TopicosAsync(string? perfil)
    {
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<AjudaTopicoDto>(@"select id as ""Id"", coalesce(perfil,'') as ""Perfil"", coalesce(titulo,'') as ""Titulo"", coalesce(descricao,'') as ""Descricao"", ordem as ""Ordem"" from plantaopro.ajuda_topicos where reg_status='A' and (@perfil is null or perfil=@perfil) order by ordem, titulo limit 100", new { perfil = string.IsNullOrWhiteSpace(perfil) ? null : perfil });
        return ApiResponse<IEnumerable<AjudaTopicoDto>>.Ok(rows);
    }

    public async Task<ApiResponse<IEnumerable<AjudaArtigoDto>>> ArtigosAsync(string? perfil)
    {
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<AjudaArtigoDto>(@"select id as ""Id"", topico_id as ""TopicoId"", coalesce(perfil,'') as ""Perfil"", coalesce(titulo,'') as ""Titulo"", coalesce(resumo,'') as ""Resumo"", coalesce(conteudo,'') as ""Conteudo"", coalesce(link_acao,'') as ""LinkAcao"" from plantaopro.ajuda_artigos where reg_status='A' and (@perfil is null or perfil=@perfil) order by titulo limit 100", new { perfil = string.IsNullOrWhiteSpace(perfil) ? null : perfil });
        return ApiResponse<IEnumerable<AjudaArtigoDto>>.Ok(rows);
    }

    public async Task<ApiResponse<AjudaArtigoDto>> ArtigoAsync(Guid id)
    {
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var row = await cn.QueryFirstOrDefaultAsync<AjudaArtigoDto>(@"select id as ""Id"", topico_id as ""TopicoId"", coalesce(perfil,'') as ""Perfil"", coalesce(titulo,'') as ""Titulo"", coalesce(resumo,'') as ""Resumo"", coalesce(conteudo,'') as ""Conteudo"", coalesce(link_acao,'') as ""LinkAcao"" from plantaopro.ajuda_artigos where id=@id and reg_status='A'", new { id });
        return row is null ? ApiResponse<AjudaArtigoDto>.Fail("Artigo não encontrado.", 404) : ApiResponse<AjudaArtigoDto>.Ok(row);
    }

    public async Task<ApiResponse<IEnumerable<AjudaArtigoDto>>> BuscarAsync(string termo)
    {
        if (string.IsNullOrWhiteSpace(termo)) return ApiResponse<IEnumerable<AjudaArtigoDto>>.Ok(Array.Empty<AjudaArtigoDto>());
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<AjudaArtigoDto>(@"select id as ""Id"", topico_id as ""TopicoId"", coalesce(perfil,'') as ""Perfil"", coalesce(titulo,'') as ""Titulo"", coalesce(resumo,'') as ""Resumo"", coalesce(conteudo,'') as ""Conteudo"", coalesce(link_acao,'') as ""LinkAcao"" from plantaopro.ajuda_artigos where reg_status='A' and (titulo ilike @busca or resumo ilike @busca or conteudo ilike @busca) order by titulo limit 50", new { busca = "%" + termo.Trim() + "%" });
        return ApiResponse<IEnumerable<AjudaArtigoDto>>.Ok(rows);
    }

    public async Task<ApiResponse<string>> FeedbackAsync(ClaimsPrincipal user, Guid id, AjudaFeedbackRequest request, HttpContext httpContext)
    {
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var existe = await cn.ExecuteScalarAsync<bool>("select exists(select 1 from plantaopro.ajuda_artigos where id=@id and reg_status='A')", new { id });
        if (!existe) return ApiResponse<string>.Fail("Artigo não encontrado.", 404);
        await cn.ExecuteAsync("insert into plantaopro.ajuda_feedbacks(id, artigo_id, usuario_id, util, comentario, reg_date) values(gen_random_uuid(), @id, @usuarioId, @util, @comentario, now())", new { id, usuarioId = GetUserId(user), util = request.Util, comentario = request.Comentario });
        await AuditarAsync(user, "AJUDA_ARTIGO", id, "FEEDBACK", new { request.Util }, true, httpContext, GetClienteId(user));
        return ApiResponse<string>.Ok("ok", "Obrigado pelo feedback.");
    }

    public async Task<ApiResponse<IEnumerable<AjudaChecklistDto>>> ChecklistsAsync(string perfil)
    {
        await using var cn = new NpgsqlConnection(Cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<AjudaChecklistDto>(@"select id as ""Id"", coalesce(perfil,'') as ""Perfil"", coalesce(titulo,'') as ""Titulo"", coalesce(descricao,'') as ""Descricao"", coalesce(link_acao,'') as ""LinkAcao"", ordem as ""Ordem"" from plantaopro.ajuda_checklists where reg_status='A' and perfil=@perfil order by ordem, titulo limit 50", new { perfil });
        return ApiResponse<IEnumerable<AjudaChecklistDto>>.Ok(rows);
    }
}
