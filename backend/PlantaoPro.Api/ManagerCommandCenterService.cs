using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed record CommandCenterSummary(long Today, long Uncovered, long PendingConfirmation, long Critical,
    long AvailableProfessionals, long PendingCheckIns, long OpenIncidents, long PendingReplacements,
    long FinancialPending, long CriticalNotifications);
public sealed record CoverageItem(Guid Id, string Unit, string Specialty, DateTime StartsAt, DateTime EndsAt,
    string Status, int OpenSlots, int Risk, string RiskLabel);
public sealed record ManagerCommandCenterDto(CommandCenterSummary Summary, IReadOnlyList<CoverageItem> Coverage, DateTime GeneratedAt);

public sealed class ManagerCommandCenterService
{
    private readonly IConfiguration configuration; private readonly ILogger<ManagerCommandCenterService> logger;
    public ManagerCommandCenterService(IConfiguration configuration, ILogger<ManagerCommandCenterService> logger) { this.configuration=configuration; this.logger=logger; }

    public async Task<ApiResponse<ManagerCommandCenterDto>> GetAsync(Guid tenantId, DateOnly from, DateOnly to, string? status, CancellationToken ct)
    {
        if (tenantId == Guid.Empty) return ApiResponse<ManagerCommandCenterDto>.Fail("Selecione um contexto de organização válido.", 403);
        if (to < from || to.DayNumber-from.DayNumber > 92) return ApiResponse<ManagerCommandCenterDto>.Fail("O período deve ser válido e ter no máximo 93 dias.", 400);
        try
        {
            await using var cn = new NpgsqlConnection(configuration.GetConnectionString("Default")); await cn.OpenAsync(ct);
            const string sql = @"select count(*) filter(where p.data_inicio::date=current_date) as ""Today"",
count(*) filter(where p.vagas_disponiveis>0 and lower(p.status) not in ('cancelado','realizado')) as ""Uncovered"",
count(*) filter(where lower(p.status) in ('aberto','pendente')) as ""PendingConfirmation"",
count(*) filter(where p.vagas_disponiveis>0 and p.data_inicio<=now()+interval '6 hours') as ""Critical"",
(select count(*) from plantaopro.medicos m where coalesce(m.tenant_id,m.cliente_id)=@tenantId and m.reg_status='A') as ""AvailableProfessionals"",
0 as ""PendingCheckIns"",0 as ""OpenIncidents"",0 as ""PendingReplacements"",
(select count(*) from plantaopro.pagamentos pg join plantaopro.plantoes px on px.id=pg.plantao_id where px.cliente_id=@tenantId and pg.reg_status='A' and lower(pg.status)='pendente') as ""FinancialPending"",
(select count(*) from plantaopro.notificacoes n where n.cliente_id=@tenantId and n.reg_status='A' and not coalesce(n.lida,false) and lower(n.tipo) in ('critico','urgente','erro')) as ""CriticalNotifications"
from plantaopro.plantoes p where p.cliente_id=@tenantId and p.reg_status='A' and p.data_inicio::date between @from and @to;
select p.id as ""Id"",coalesce(h.nome_fantasia,'Unidade') as ""Unit"",coalesce(e.nome,'Especialidade') as ""Specialty"",p.data_inicio as ""StartsAt"",p.data_fim as ""EndsAt"",p.status as ""Status"",p.vagas_disponiveis as ""OpenSlots""
from plantaopro.plantoes p join plantaopro.hospitais h on h.id=p.hospital_id join plantaopro.especialidades e on e.id=p.especialidade_id
where p.cliente_id=@tenantId and p.reg_status='A' and p.data_inicio::date between @from and @to and (@status is null or lower(p.status)=lower(@status)) order by p.data_inicio limit 200";
            using var grid=await cn.QueryMultipleAsync(new CommandDefinition(sql,new {tenantId,from,to,status=string.IsNullOrWhiteSpace(status)?null:status},cancellationToken:ct));
            var summary=await grid.ReadSingleAsync<CommandCenterSummary>(); var rows=(await grid.ReadAsync<CoverageRow>()).Select(x=>x.ToItem()).ToArray();
            return ApiResponse<ManagerCommandCenterDto>.Ok(new(summary,rows,DateTime.UtcNow));
        }
        catch(Exception ex) { logger.LogError(ex,"Falha no Command Center do tenant {TenantId}",tenantId); throw; }
    }
    private sealed record CoverageRow(Guid Id,string Unit,string Specialty,DateTime StartsAt,DateTime EndsAt,string Status,int OpenSlots)
    { public CoverageItem ToItem(){var risk=PlantaoPro.Domain.Escalas.ShiftRiskCalculator.Calculate(StartsAt,OpenSlots,Status.Equals("pendente",StringComparison.OrdinalIgnoreCase),false,0,DateTimeOffset.UtcNow);return new(Id,Unit,Specialty,StartsAt,EndsAt,Status,OpenSlots,risk,risk>=70?"Crítico":risk>=35?"Atenção":"Estável");} }
}
