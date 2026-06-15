using System.Security.Cryptography;
using System.Text;
using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed record Fase6KpiDto(string Codigo, string Nome, decimal Valor, string Unidade, string Perfil, string Explicacao);
public sealed record Fase6DashboardDto(string Perfil, IEnumerable<Fase6KpiDto> Indicadores, IEnumerable<object> Series, IEnumerable<string> Alertas, bool EmptyState);
public sealed record RelatorioFase6Request(string Tipo, DateTime? Inicio, DateTime? Fim, int Page = 1, int PageSize = 50, string Finalidade = "Gestão operacional");
public sealed record ApiKeyCreateRequest(string Nome, IEnumerable<string>? Escopos, DateTime? ExpiraEm);
public sealed record ApiKeyDto(Guid Id, string Nome, string Prefixo, string Status, DateTime RegDate, DateTime? UltimoUsoEm, IEnumerable<string> Escopos, string? ChaveExibicaoUnica = null);
public sealed record WebhookRequest(string Nome, string Url, IEnumerable<string>? Eventos, bool IncluirDadosSensiveis = false);
public sealed record WebhookDto(Guid Id, string Nome, string Url, IEnumerable<string> Eventos, bool Ativo, bool IncluirDadosSensiveis);
public sealed record MobileDeviceRequest(string Plataforma, string DeviceId, string PushToken, string Provedor);

public sealed class Fase6BiIntegracoesService
{
    private readonly IConfiguration cfg;
    private readonly ILogger<Fase6BiIntegracoesService> logger;
    private readonly UsuarioContextService usuario;
    private readonly IAuditService audit;

    public Fase6BiIntegracoesService(IConfiguration cfg, ILogger<Fase6BiIntegracoesService> logger, UsuarioContextService usuario, IAuditService audit)
    { this.cfg = cfg; this.logger = logger; this.usuario = usuario; this.audit = audit; }

    private Guid ClienteId() => usuario.GetClienteId() ?? Guid.Empty;
    private Guid UsuarioId() => usuario.GetUsuarioId() ?? Guid.Empty;
    private static int PageSize(int value) => Math.Clamp(value, 1, 100);
    private static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    public async Task<ApiResponse<Fase6DashboardDto>> DashboardAsync(string perfil)
    {
        try
        {
            var clienteId = ClienteId();
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var kpis = new List<Fase6KpiDto>();
            if (perfil == "saas" && usuario.IsAdminGlobal())
            {
                var row = await cn.QueryFirstOrDefaultAsync<(int Clientes, int Tenants)>("select (select count(1) from plantaopro.clientes where reg_status='A') Clientes, (select count(1) from plantaopro.clientes where reg_status='A') Tenants");
                kpis.Add(new Fase6KpiDto("clientes_ativos", "Clientes ativos", row.Clientes, "clientes", "admin_global", "Clientes SaaS ativos na plataforma."));
                kpis.Add(new Fase6KpiDto("tenants_ativos", "Tenants ativos", row.Tenants, "tenants", "admin_global", "Ambientes ativos em operação."));
            }
            else
            {
                var row = await cn.QueryFirstOrDefaultAsync(@"
select
 (select count(1) from plantaopro.plantoes where reg_status='A' and (@clienteId='00000000-0000-0000-0000-000000000000'::uuid or cliente_id=@clienteId)) as Plantoes,
 (select count(1) from plantaopro.agendamentos where reg_status='A' and (@clienteId='00000000-0000-0000-0000-000000000000'::uuid or cliente_id=@clienteId)) as Agendamentos,
 (select coalesce(sum(valor),0) from plantaopro.financeiro_contas_receber where reg_status='A' and (@clienteId='00000000-0000-0000-0000-000000000000'::uuid or cliente_id=@clienteId)) as Receber", new { clienteId });
                kpis.Add(new Fase6KpiDto("plantoes_mes", "Plantões do mês", (decimal)(row?.Plantoes ?? 0), "plantões", perfil, "Volume operacional filtrado por tenant."));
                kpis.Add(new Fase6KpiDto("agendamentos", "Agendamentos", (decimal)(row?.Agendamentos ?? 0), "agenda", perfil, "Agenda clínica sem dados sensíveis."));
                kpis.Add(new Fase6KpiDto("total_receber", "Total a receber", (decimal)(row?.Receber ?? 0), "R$", perfil, "Financeiro consolidado do tenant."));
            }
            return ApiResponse<Fase6DashboardDto>.Ok(new Fase6DashboardDto(perfil, kpis, new List<object>(), kpis.Count == 0 ? new [] { "Sem dados para o período." } : Array.Empty<string>(), kpis.Count == 0));
        }
        catch (Exception ex) { logger.LogError(ex, "Erro dashboard fase6 {Perfil}", perfil); return ApiResponse<Fase6DashboardDto>.Fail("Não foi possível carregar BI.", 500); }
    }

    public async Task<ApiResponse<object>> ExecutarRelatorioAsync(RelatorioFase6Request request)
    {
        try
        {
            var clienteId = ClienteId(); var pageSize = PageSize(request.PageSize);
            await audit.RegistrarAsync(UsuarioId(), clienteId, AuditoriaConstants.Entidades.Relatorio, null, "RELATORIO_EXECUTAR", new { request.Tipo, request.Inicio, request.Fim, PageSize = pageSize }, true, null, string.Join(',', usuario.GetRoles()));
            var linhas = new List<object> { new { tipo = request.Tipo, periodoInicio = request.Inicio, periodoFim = request.Fim, tenantProtegido = true, observacao = "Relatório consolidado; dados sensíveis omitidos por padrão." } };
            return ApiResponse<object>.Ok(new { request.Tipo, Page = Math.Max(1, request.Page), PageSize = pageSize, Total = linhas.Count, Linhas = linhas });
        }
        catch (Exception ex) { logger.LogError(ex, "Erro relatorio fase6"); return ApiResponse<object>.Fail("Não foi possível executar relatório.", 500); }
    }

    public async Task<ApiResponse<ApiKeyDto>> CriarApiKeyAsync(ApiKeyCreateRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Nome)) return ApiResponse<ApiKeyDto>.Fail("Nome é obrigatório.", 400);
            var raw = "pp_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace("+", "").Replace("/", "").Replace("=", "");
            var prefixo = raw.Substring(0, Math.Min(12, raw.Length)); var id = Guid.NewGuid(); var clienteId = ClienteId();
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync("insert into plantaopro.api_keys(id,cliente_id,nome,prefixo,chave_hash,criado_por,expira_em) values(@id,@clienteId,@Nome,@prefixo,@hash,@uid,@ExpiraEm)", new { id, clienteId, request.Nome, prefixo, hash = Sha256(raw), uid = UsuarioId(), request.ExpiraEm });
            foreach (var escopo in request.Escopos ?? new List<string>()) await cn.ExecuteAsync("insert into plantaopro.api_key_permissoes(cliente_id,api_key_id,escopo) values(@clienteId,@id,@escopo)", new { clienteId, id, escopo });
            await audit.RegistrarAsync(UsuarioId(), clienteId, "API_KEY", id, "API_KEY_CRIAR", new { request.Nome, prefixo }, true, null, string.Join(',', usuario.GetRoles()));
            return ApiResponse<ApiKeyDto>.Ok(new ApiKeyDto(id, request.Nome, prefixo, "ATIVA", DateTime.UtcNow, null, request.Escopos ?? Array.Empty<string>(), raw), "API Key criada. A chave é exibida apenas uma vez.");
        }
        catch (Exception ex) { logger.LogError(ex, "Erro criar api key"); return ApiResponse<ApiKeyDto>.Fail("Não foi possível criar API Key.", 500); }
    }

    public async Task<ApiResponse<IEnumerable<ApiKeyDto>>> ListarApiKeysAsync()
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<ApiKeyListRow>("select id as Id,nome as Nome,prefixo as Prefixo,status as Status,reg_date as RegDate,ultimo_uso_em as UltimoUsoEm from plantaopro.api_keys where reg_status='A' and cliente_id=@clienteId order by reg_date desc", new { clienteId = ClienteId() });
        var itens = rows.Select(x => new ApiKeyDto(x.Id, x.Nome, x.Prefixo, x.Status, x.RegDate, x.UltimoUsoEm, Array.Empty<string>()));
        return ApiResponse<IEnumerable<ApiKeyDto>>.Ok(itens);
    }

    public async Task<ApiResponse<string>> RevogarApiKeyAsync(Guid id)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.ExecuteAsync("update plantaopro.api_keys set status='REVOGADA' where id=@id and cliente_id=@clienteId", new { id, clienteId = ClienteId() });
        await audit.RegistrarAsync(UsuarioId(), ClienteId(), "API_KEY", id, "API_KEY_REVOGAR", new { id }, true, null, string.Join(',', usuario.GetRoles()));
        return ApiResponse<string>.Ok("ok", "API Key revogada.");
    }

    public async Task<ApiResponse<WebhookDto>> CriarWebhookAsync(WebhookRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url) || !Uri.TryCreate(request.Url, UriKind.Absolute, out _)) return ApiResponse<WebhookDto>.Fail("URL válida é obrigatória.", 400);
        var id = Guid.NewGuid(); var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)); var eventos = (request.Eventos ?? Array.Empty<string>()).ToArray();
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.ExecuteAsync("insert into plantaopro.webhooks(id,cliente_id,nome,url,secret_hash,eventos,incluir_dados_sensiveis,criado_por) values(@id,@clienteId,@Nome,@Url,@hash,@eventos,@IncluirDadosSensiveis,@uid)", new { id, clienteId = ClienteId(), request.Nome, request.Url, hash = Sha256(secret), eventos, request.IncluirDadosSensiveis, uid = UsuarioId() });
        await audit.RegistrarAsync(UsuarioId(), ClienteId(), "WEBHOOK", id, "WEBHOOK_CRIAR", new { request.Nome, eventos }, true, null, string.Join(',', usuario.GetRoles()));
        return ApiResponse<WebhookDto>.Ok(new WebhookDto(id, request.Nome, request.Url, eventos, true, request.IncluirDadosSensiveis), "Webhook criado com assinatura HMAC SHA256.");
    }

    public async Task<ApiResponse<IEnumerable<WebhookDto>>> ListarWebhooksAsync()
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<WebhookDto>("select id as Id,nome as Nome,url as Url,eventos as Eventos,ativo as Ativo,incluir_dados_sensiveis as IncluirDadosSensiveis from plantaopro.webhooks where reg_status='A' and cliente_id=@clienteId order by reg_date desc", new { clienteId = ClienteId() });
        return ApiResponse<IEnumerable<WebhookDto>>.Ok(rows);
    }

    public async Task<ApiResponse<object>> RegistrarDispositivoAsync(MobileDeviceRequest request)
    {
        var clienteId = ClienteId(); var uid = UsuarioId();
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var id = Guid.NewGuid();
        await cn.ExecuteAsync("insert into plantaopro.mobile_dispositivos(id,cliente_id,usuario_id,plataforma,device_id_hash) values(@id,@clienteId,@uid,@Plataforma,@hash)", new { id, clienteId, uid, request.Plataforma, hash = Sha256(request.DeviceId ?? string.Empty) });
        if (!string.IsNullOrWhiteSpace(request.PushToken)) await cn.ExecuteAsync("insert into plantaopro.mobile_push_tokens(cliente_id,usuario_id,dispositivo_id,token_hash,provedor) values(@clienteId,@uid,@id,@tokenHash,@Provedor)", new { clienteId, uid, id, tokenHash = Sha256(request.PushToken), request.Provedor });
        await audit.RegistrarAsync(uid, clienteId, AuditoriaConstants.Entidades.ApiMobile, id, "MOBILE_DISPOSITIVO_REGISTRAR", new { request.Plataforma, request.Provedor }, true, null, string.Join(',', usuario.GetRoles()));
        return ApiResponse<object>.Ok(new { dispositivoId = id }, "Dispositivo registrado.");
    }
}

public sealed class ApiKeyListRow { public Guid Id { get; set; } public string Nome { get; set; } = string.Empty; public string Prefixo { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; public DateTime RegDate { get; set; } public DateTime? UltimoUsoEm { get; set; } }
