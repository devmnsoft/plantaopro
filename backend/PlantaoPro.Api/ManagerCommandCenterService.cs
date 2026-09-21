using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed record PlatformHealthSummary(
    long ActiveClients,
    long ActiveTenants,
    long ContractedModules);

public sealed record CommandCenterSummary(
    long Today,
    long Uncovered,
    long PendingConfirmation,
    long Critical,
    long AvailableProfessionals,
    long PendingCheckIns,
    long OpenIncidents,
    long PendingReplacements,
    long FinancialPending,
    long CriticalNotifications,
    PlatformHealthSummary? PlatformHealth = null);

public sealed record CoverageItem(
    Guid Id,
    string Unit,
    string Specialty,
    DateTime StartsAt,
    DateTime EndsAt,
    string Status,
    int OpenSlots,
    int Risk,
    string RiskLabel);

public sealed record ManagerCommandCenterDto(
    CommandCenterSummary Summary,
    IReadOnlyList<CoverageItem> Coverage,
    DateTime GeneratedAt);

public sealed class ManagerCommandCenterService
{
    private const string SqlCommandCenter = @"
        select
            count(*) filter (where p.data_inicio::date = current_date) as ""Today"",
            count(*) filter (
                where p.vagas_disponiveis > 0
                  and lower(p.status) not in ('cancelado', 'realizado')) as ""Uncovered"",
            count(*) filter (
                where lower(p.status) in ('aberto', 'pendente')) as ""PendingConfirmation"",
            count(*) filter (
                where p.vagas_disponiveis > 0
                  and p.data_inicio <= now() + interval '6 hours') as ""Critical"",
            (
                select count(*)
                from plantaopro.medicos m
                where (m.tenant_id = @TenantId or m.cliente_id = @TenantId)
                  and m.reg_status = 'A'
            ) as ""AvailableProfessionals"",
            (
                select count(*)
                from plantaopro.medico_checkins mc
                where mc.tenant_id = @TenantId
                  and mc.checkout_em is null
                  and mc.status_conferencia = 'REGISTRO_INCOMPLETO'
            ) as ""PendingCheckIns"",
            (
                select count(*)
                from plantaopro.ocorrencias_operacionais o
                where o.tenant_id = @TenantId
                  and o.situacao in ('ABERTA', 'EM_ATENDIMENTO')
                  and o.reg_status = 'A'
            ) as ""OpenIncidents"",
            (
                select count(*)
                from plantaopro.cobertura_convites cc
                where (cc.tenant_id = @TenantId or cc.cliente_id = @TenantId)
                  and cc.status = 'PENDENTE'
            ) as ""PendingReplacements"",
            (
                select count(*)
                from plantaopro.fechamento_plantao fp
                where fp.tenant_id = @TenantId
                  and fp.status in ('EM_CONFERENCIA', 'AGUARDANDO_APROVACAO', 'COM_DIVERGENCIA')
            ) as ""FinancialPending"",
            (
                select count(*)
                from plantaopro.notificacoes n
                where (n.cliente_id = @TenantId or n.tenant_id = @TenantId)
                  and n.reg_status = 'A'
                  and not coalesce(n.lida, false)
                  and lower(n.tipo) in ('critico', 'urgente', 'erro')
            ) as ""CriticalNotifications""
        from plantaopro.plantoes p
        where (p.cliente_id = @TenantId or p.tenant_id = @TenantId)
          and p.reg_status = 'A'
          and p.data_inicio::date between @From and @To;

        select
            p.id as ""Id"",
            coalesce(h.nome_fantasia, 'Unidade') as ""Unit"",
            coalesce(e.nome, 'Especialidade') as ""Specialty"",
            p.data_inicio as ""StartsAt"",
            p.data_fim as ""EndsAt"",
            p.status as ""Status"",
            p.vagas_disponiveis as ""OpenSlots""
        from plantaopro.plantoes p
        join plantaopro.hospitais h on h.id = p.hospital_id
        join plantaopro.especialidades e on e.id = p.especialidade_id
        where (p.cliente_id = @TenantId or p.tenant_id = @TenantId)
          and p.reg_status = 'A'
          and p.data_inicio::date between @From and @To
          and (@Status is null or lower(p.status) = lower(@Status))
        order by p.data_inicio
        limit 200
    ";

    private readonly IConfiguration configuration;
    private readonly ILogger<ManagerCommandCenterService> logger;

    public ManagerCommandCenterService(
        IConfiguration configuration,
        ILogger<ManagerCommandCenterService> logger)
    {
        this.configuration = configuration;
        this.logger = logger;
    }

    public async Task<ApiResponse<ManagerCommandCenterDto>> GetAsync(
        Guid tenantId,
        DateOnly from,
        DateOnly to,
        string? status,
        CancellationToken ct)
    {
        if (tenantId == Guid.Empty)
        {
            return ApiResponse<ManagerCommandCenterDto>.Fail(
                "Selecione um contexto de organização válido.",
                403);
        }

        if (to < from || to.DayNumber - from.DayNumber > 92)
        {
            return ApiResponse<ManagerCommandCenterDto>.Fail(
                "O período deve ser válido e ter no máximo 93 dias.",
                400);
        }

        try
        {
            await using var connection = new NpgsqlConnection(
                configuration.GetConnectionString("Default"));
            await connection.OpenAsync(ct);

            var parameters = new
            {
                TenantId = tenantId,
                From = from,
                To = to,
                Status = string.IsNullOrWhiteSpace(status) ? null : status
            };

            var command = new CommandDefinition(
                SqlCommandCenter,
                parameters,
                cancellationToken: ct);

            using var grid = await connection.QueryMultipleAsync(command);
            var summary = await grid.ReadSingleAsync<CommandCenterSummary>();
            var coverage = (await grid.ReadAsync<CoverageRow>())
                .Select(row => row.ToItem())
                .ToArray();

            var result = new ManagerCommandCenterDto(summary, coverage, DateTime.UtcNow);
            return ApiResponse<ManagerCommandCenterDto>.Ok(result);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Falha no Command Center do tenant {TenantId}",
                tenantId);
            throw;
        }
    }

    public async Task<ApiResponse<ManagerCommandCenterDto>> GetGlobalAsync(CancellationToken ct)
    {
        try
        {
            await using var connection = new NpgsqlConnection(
                configuration.GetConnectionString("Default"));
            await connection.OpenAsync(ct);

            const string sql = @"
                select
                    (select count(*) from plantaopro.clientes where reg_status = 'A' and status = 'ATIVO') as ""ActiveClients"",
                    (select count(*) from plantaopro.tenants where status = 'ATIVO') as ""ActiveTenants"",
                    (select count(*) from plantaopro.tenant_modulos where status = 'ATIVO') as ""ContractedModules"";
            ";

            var health = await connection.QuerySingleAsync<PlatformHealthSummary>(new CommandDefinition(sql, cancellationToken: ct));
            var summary = new CommandCenterSummary(
                Today: 0,
                Uncovered: 0,
                PendingConfirmation: 0,
                Critical: 0,
                AvailableProfessionals: 0,
                PendingCheckIns: 0,
                OpenIncidents: 0,
                PendingReplacements: 0,
                FinancialPending: 0,
                CriticalNotifications: 0,
                PlatformHealth: health);

            var result = new ManagerCommandCenterDto(summary, Array.Empty<CoverageItem>(), DateTime.UtcNow);
            return ApiResponse<ManagerCommandCenterDto>.Ok(result);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha no Command Center global da plataforma");
            throw;
        }
    }

    private sealed record CoverageRow(
        Guid Id,
        string Unit,
        string Specialty,
        DateTime StartsAt,
        DateTime EndsAt,
        string Status,
        int OpenSlots)
    {
        public CoverageItem ToItem()
        {
            var risk = PlantaoPro.Domain.Escalas.ShiftRiskCalculator.Calculate(
                StartsAt,
                OpenSlots,
                Status.Equals("pendente", StringComparison.OrdinalIgnoreCase),
                false,
                0,
                DateTimeOffset.UtcNow);

            var riskLabel = risk >= 70
                ? "Crítico"
                : risk >= 35
                    ? "Atenção"
                    : "Estável";

            return new CoverageItem(
                Id,
                Unit,
                Specialty,
                StartsAt,
                EndsAt,
                Status,
                OpenSlots,
                risk,
                riskLabel);
        }
    }
}
