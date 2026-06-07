using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed class PermissionService
{
    private readonly IAuditService audit;

    public PermissionService(IAuditService audit)
    {
        this.audit = audit;
    }

    public async Task<ApiResponse<IEnumerable<RolePermissionDto>>> ListRolePermissionsAsync(Guid userId, string? ip, string? ua)
    {
        var rolePermissions = new[]
        {
            new RolePermissionDto(RolesConstants.Administrador, new[]{"dashboard:read","usuarios:manage","auditoria:read","plantoes:manage","escalas:manage","financeiro:manage"}),
            new RolePermissionDto(RolesConstants.Coordenacao, new[]{"dashboard:read","plantoes:manage","escalas:manage","medicos:read"}),
            new RolePermissionDto(RolesConstants.Operador, new[]{"dashboard:read","plantoes:manage","escalas:manage"}),
            new RolePermissionDto(RolesConstants.Financeiro, new[]{"dashboard:financeiro","pagamentos:manage","relatorios:financeiro"}),
            new RolePermissionDto(RolesConstants.Medico, new[]{"agenda:read","plantoes:request","pagamentos:read"}),
            new RolePermissionDto(RolesConstants.Hospital, new[]{"hospital:read","plantoes:publish","escalas:read"})
        };

        await audit.LogAsync(userId, "READ_PERMISSION_MATRIX", "perfis", null, "Consulta da matriz de permissões", ip: ip, userAgent: ua);
        return ApiResponse<IEnumerable<RolePermissionDto>>.Ok(rolePermissions);
    }
}

public sealed class NotificationPreferenceService
{
    private readonly IConfiguration cfg;
    private readonly IAuditService audit;

    public NotificationPreferenceService(IConfiguration cfg, IAuditService audit)
    {
        this.cfg = cfg;
        this.audit = audit;
    }

    public async Task<ApiResponse<IEnumerable<NotificationPreferenceDto>>> GetAsync(Guid userId)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<NotificationPreferenceDto>(@"select tipo as Tipo, in_app as InApp, email as Email
from plantaopro.usuario_notificacao_preferencias where usuario_id=@userId order by tipo", new { userId });
        return ApiResponse<IEnumerable<NotificationPreferenceDto>>.Ok(rows);
    }

    public async Task<ApiResponse<string>> UpsertAsync(Guid userId, UpsertNotificationPreferenceRequest request, string? ip, string? ua)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.ExecuteAsync(@"insert into plantaopro.usuario_notificacao_preferencias(usuario_id,tipo,in_app,email,reg_date,reg_status)
values(@userId,@tipo,@inApp,@email,now(),'A')
on conflict(usuario_id,tipo) do update set in_app=excluded.in_app,email=excluded.email,reg_update=now()", new
        {
            userId,
            tipo = request.Tipo.Trim().ToUpperInvariant(),
            request.InApp,
            request.Email
        });
        await audit.LogAsync(userId, "UPSERT_NOTIFICATION_PREF", "usuario_notificacao_preferencias", userId, $"Preferência atualizada: {request.Tipo}", valorNovo: System.Text.Json.JsonSerializer.Serialize(request), ip: ip, userAgent: ua);
        return ApiResponse<string>.Ok("Preferência atualizada.");
    }
}

public sealed class PremiumOperacoesService
{
    private readonly IConfiguration cfg;
    private readonly IAuditService audit;

    public PremiumOperacoesService(IConfiguration cfg, IAuditService audit)
    {
        this.cfg = cfg;
        this.audit = audit;
    }

    public async Task<ApiResponse<PremiumOperacoesResumoDto>> ResumoAsync(Guid userId, DateTime? inicio, DateTime? fim, string? perfil, string? ip, string? ua)
    {
        var dtInicio = inicio?.Date ?? DateTime.UtcNow.Date.AddDays(-30);
        var dtFim = fim?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));

        var totais = await cn.QueryFirstAsync<(long PendenciasEscalas, long PendenciasPagamentos, long NaoRealizados, decimal ValorEmRisco, decimal HorasConfirmadas)>(@"
select
  (select count(1) from plantaopro.escalas e where e.reg_status='A' and e.status in ('solicitado','confirmado')) as PendenciasEscalas,
  (select count(1) from plantaopro.pagamentos pg where pg.reg_status='A' and pg.status in ('pendente','atrasado')) as PendenciasPagamentos,
  (select count(1) from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id where e.reg_status='A' and e.status='confirmado' and p.data_fim < now()) as NaoRealizados,
  (select coalesce(sum(pg.valor_previsto),0) from plantaopro.pagamentos pg where pg.reg_status='A' and pg.status in ('pendente','atrasado')) as ValorEmRisco,
  (select coalesce(sum(extract(epoch from (p.data_fim-p.data_inicio))/3600.0),0) from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id where e.reg_status='A' and e.status in ('confirmado','realizado') and p.data_inicio between @dtInicio and @dtFim) as HorasConfirmadas
", new { dtInicio, dtFim });

        var seriePlantoes = await cn.QueryAsync<DashboardChartItem>(@"
select to_char(date_trunc('week', p.data_inicio), 'DD/MM') as Label, count(1)::numeric as Valor
from plantaopro.plantoes p
where p.reg_status='A' and p.data_inicio between @dtInicio and @dtFim
group by 1 order by min(p.data_inicio)", new { dtInicio, dtFim });

        var seriePagamentos = await cn.QueryAsync<DashboardChartItem>(@"
select coalesce(pg.status,'desconhecido') as Label, coalesce(sum(pg.valor_previsto),0) as Valor
from plantaopro.pagamentos pg
where pg.reg_status='A' and coalesce(pg.data_prevista::timestamp, now()) between @dtInicio and @dtFim
group by coalesce(pg.status,'desconhecido')", new { dtInicio, dtFim });

        var alertas = new List<AlertaOperacionalDto>();
        if (totais.NaoRealizados > 0) alertas.Add(new("plantao", "Plantões pendentes de conclusão", $"{totais.NaoRealizados} plantões confirmados já ultrapassaram o horário final.", "alta"));
        if (totais.PendenciasPagamentos > 0) alertas.Add(new("financeiro", "Pagamentos pendentes/atrasados", $"{totais.PendenciasPagamentos} pagamentos aguardam baixa financeira.", "media"));
        if (totais.PendenciasEscalas > 0) alertas.Add(new("escala", "Escalas aguardando decisão", $"{totais.PendenciasEscalas} escalas ainda estão em fila de confirmação.", "baixa"));

        var papel = string.IsNullOrWhiteSpace(perfil) ? "geral" : perfil.Trim().ToLowerInvariant();
        var cards = new List<PremiumKpiCardDto>
        {
            new("horas_confirmadas", "Horas confirmadas", totais.HorasConfirmadas, "primary", "bi-clock-history", "h", $"Período: {dtInicio:dd/MM/yyyy} até {dtFim:dd/MM/yyyy}"),
            new("valor_risco", "Valor em risco", totais.ValorEmRisco, "danger", "bi-exclamation-octagon", "R$", "Pagamentos pendentes e atrasados"),
            new("pendencias_escala", "Pendências de escala", totais.PendenciasEscalas, "warning", "bi-calendar2-week", "itens", "Escalas aguardando ação"),
            new("pendencias_pagamento", "Pendências financeiras", totais.PendenciasPagamentos, "success", "bi-cash-stack", "itens", "Fluxo financeiro pendente")
        };

        if (papel == "medico")
            cards = cards.Where(c => c.Chave is "horas_confirmadas" or "pendencias_pagamento").ToList();

        await audit.LogAsync(userId, "PREMIUM_OVERVIEW", "dashboards", null, $"Resumo premium consultado para perfil {papel}", ip: ip, userAgent: ua);
        var payload = new PremiumOperacoesResumoDto(cards, alertas, seriePlantoes, seriePagamentos);
        return ApiResponse<PremiumOperacoesResumoDto>.Ok(payload);
    }
}
