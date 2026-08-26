using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed record UnitDashboardDto(long Today, long Future, long Uncovered, long AwaitingConfirmation,
    long OpenIncidents, long PendingRequests, decimal CoveragePercent, decimal ContractedValue,
    IReadOnlyList<dynamic> RecentNotifications);
internal sealed record UnitDashboardRow(long Today,long Future,long Uncovered,long AwaitingConfirmation,long OpenIncidents,long PendingRequests,decimal CoveragePercent,decimal ContractedValue);
public sealed record CreateShiftRequestDto(Guid UnitId, Guid SpecialtyId, string Sector, DateOnly Date,
    TimeOnly StartsAt, TimeOnly EndsAt, int Professionals, string? Notes, string Priority, string Justification, bool Draft);
public sealed record ShiftRequestDecisionDto(string? Reason);

public sealed class UnitDashboardService
{
    private readonly IConfiguration configuration; private readonly ILogger<UnitDashboardService> logger;
    public UnitDashboardService(IConfiguration configuration, ILogger<UnitDashboardService> logger) { this.configuration=configuration; this.logger=logger; }
    public async Task<ApiResponse<UnitDashboardDto>> GetAsync(Guid tenantId, Guid unitId, CancellationToken ct)
    {
        if (tenantId==Guid.Empty || unitId==Guid.Empty) return ApiResponse<UnitDashboardDto>.Fail("Contexto de unidade inválido.",403);
        try { await using var cn=new NpgsqlConnection(configuration.GetConnectionString("Default"));
            const string sql=@"select count(*) filter(where p.data_inicio::date=current_date) as \"Today\", count(*) filter(where p.data_inicio>now()) as \"Future\", count(*) filter(where p.vagas_disponiveis>0 and lower(p.status) not in ('cancelado','realizado')) as \"Uncovered\", count(*) filter(where lower(p.status) in ('aberto','pendente')) as \"AwaitingConfirmation\", 0::bigint as \"OpenIncidents\", (select count(*) from plantaopro.solicitacoes_plantao s where s.tenant_id=@tenantId and s.unidade_id=@unitId and s.status in ('enviada','em_analise')) as \"PendingRequests\", coalesce(round(100.0*sum(p.quantidade_vagas-p.vagas_disponiveis)/nullif(sum(p.quantidade_vagas),0),2),0) as \"CoveragePercent\", coalesce((select sum(c.valor_base) from plantaopro.contratos_operacionais c where c.tenant_id=@tenantId and c.unidade_id=@unitId and c.status='ativo' and current_date between c.vigencia_inicio and c.vigencia_fim),0) as \"ContractedValue\" from plantaopro.plantoes p where p.cliente_id=@tenantId and p.hospital_id=@unitId and p.reg_status='A'; select n.id,n.titulo,n.mensagem,n.data_criacao from plantaopro.notificacoes n where n.cliente_id=@tenantId and (n.hospital_id is null or n.hospital_id=@unitId) and n.reg_status='A' order by n.data_criacao desc limit 8";
            using var grid=await cn.QueryMultipleAsync(new CommandDefinition(sql,new{tenantId,unitId},cancellationToken:ct)); var row=await grid.ReadSingleAsync<UnitDashboardRow>(); var notifications=(await grid.ReadAsync()).ToArray(); return ApiResponse<UnitDashboardDto>.Ok(new(row.Today,row.Future,row.Uncovered,row.AwaitingConfirmation,row.OpenIncidents,row.PendingRequests,row.CoveragePercent,row.ContractedValue,notifications)); }
        catch(Exception ex){logger.LogError(ex,"Falha no portal da unidade {UnitId} do tenant {TenantId}",unitId,tenantId);throw;}
    }
}

public sealed class ShiftRequestService
{
    private readonly IConfiguration configuration; private readonly IAuditService audit; private readonly ILogger<ShiftRequestService> logger;
    public ShiftRequestService(IConfiguration configuration,IAuditService audit,ILogger<ShiftRequestService> logger){this.configuration=configuration;this.audit=audit;this.logger=logger;}
    public async Task<ApiResponse<Guid>> CreateAsync(Guid tenantId,Guid userId,CreateShiftRequestDto input,CancellationToken ct)
    {
        if(tenantId==Guid.Empty||input.UnitId==Guid.Empty||input.SpecialtyId==Guid.Empty) return ApiResponse<Guid>.Fail("Unidade e especialidade são obrigatórias.",400);
        if(input.StartsAt>=input.EndsAt) return ApiResponse<Guid>.Fail("O horário final deve ser posterior ao inicial.",400);
        if(input.Professionals<1) return ApiResponse<Guid>.Fail("Informe ao menos um profissional.",400);
        var id=Guid.NewGuid();
        try{await using var cn=new NpgsqlConnection(configuration.GetConnectionString("Default")); await cn.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.solicitacoes_plantao(id,tenant_id,unidade_id,especialidade_id,setor,data,horario_inicio,horario_fim,quantidade_profissionais,observacoes,prioridade,justificativa,status,criado_por) select @id,@tenantId,@UnitId,@SpecialtyId,@Sector,@Date,@StartsAt,@EndsAt,@Professionals,@Notes,@Priority,@Justification,@status,@userId where exists(select 1 from plantaopro.hospitais h where h.id=@UnitId and h.cliente_id=@tenantId and h.reg_status='A') and exists(select 1 from plantaopro.especialidades e where e.id=@SpecialtyId and e.cliente_id=@tenantId and e.reg_status='A')",new{id,tenantId,userId,input.UnitId,input.SpecialtyId,input.Sector,input.Date,input.StartsAt,input.EndsAt,input.Professionals,input.Notes,input.Priority,input.Justification,status=input.Draft?"rascunho":"enviada"},cancellationToken:ct)); await audit.RegistrarAsync(userId,tenantId,"SOLICITACAO_PLANTAO",id,"CRIAR",new{input.UnitId,input.SpecialtyId},true,null,"PORTAL_UNIDADE",ct); return ApiResponse<Guid>.Ok(id);}
        catch(Exception ex){logger.LogError(ex,"Falha ao criar solicitação no tenant {TenantId}",tenantId);throw;}
    }
}

public sealed class ShiftRequestApprovalService
{
    private readonly IConfiguration configuration; private readonly IAuditService audit; private readonly ILogger<ShiftRequestApprovalService> logger;
    public ShiftRequestApprovalService(IConfiguration configuration,IAuditService audit,ILogger<ShiftRequestApprovalService> logger){this.configuration=configuration;this.audit=audit;this.logger=logger;}
    public async Task<ApiResponse<Guid>> DecideAsync(Guid tenantId,Guid userId,Guid requestId,bool approved,string? reason,bool convert,CancellationToken ct)
    {
        if(!approved&&string.IsNullOrWhiteSpace(reason)) return ApiResponse<Guid>.Fail("O motivo da recusa é obrigatório.",400);
        try{await using var cn=new NpgsqlConnection(configuration.GetConnectionString("Default"));await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);
            var request=await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition("select * from plantaopro.solicitacoes_plantao where id=@requestId and tenant_id=@tenantId and status in ('enviada','em_analise','aprovada') for update",new{requestId,tenantId},tx,cancellationToken:ct));
            if(request is null)return ApiResponse<Guid>.Fail("Solicitação não encontrada no contexto autorizado.",404);
            Guid? shiftId=null;
            if(approved&&convert){var conflict=await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"select exists(select 1 from plantaopro.plantoes p where p.cliente_id=@tenantId and p.hospital_id=@unit and p.especialidade_id=@specialty and p.reg_status='A' and tstzrange(p.data_inicio,p.data_fim,'[)') && tstzrange(@start,@finish,'[)'))",new{tenantId,unit=(Guid)request.unidade_id,specialty=(Guid)request.especialidade_id,start=((DateOnly)request.data).ToDateTime((TimeOnly)request.horario_inicio),finish=((DateOnly)request.data).ToDateTime((TimeOnly)request.horario_fim)},tx,cancellationToken:ct));if(conflict)return ApiResponse<Guid>.Fail("A conversão conflita com um plantão existente.",409);shiftId=Guid.NewGuid();await cn.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.plantoes(id,cliente_id,hospital_id,especialidade_id,data_inicio,data_fim,quantidade_vagas,vagas_disponiveis,status,reg_status) values(@shiftId,@tenantId,@unit,@specialty,@start,@finish,@quantity,@quantity,'aberto','A')",new{shiftId,tenantId,unit=(Guid)request.unidade_id,specialty=(Guid)request.especialidade_id,start=((DateOnly)request.data).ToDateTime((TimeOnly)request.horario_inicio),finish=((DateOnly)request.data).ToDateTime((TimeOnly)request.horario_fim),quantity=(int)request.quantidade_profissionais},tx,cancellationToken:ct));}
            var status=approved?(convert?"convertida":"aprovada"):"recusada";await cn.ExecuteAsync(new CommandDefinition("update plantaopro.solicitacoes_plantao set status=@status,motivo_recusa=@reason,plantao_id=@shiftId,atualizado_em=now() where id=@requestId and tenant_id=@tenantId",new{status,reason,shiftId,requestId,tenantId},tx,cancellationToken:ct));await tx.CommitAsync(ct);await audit.RegistrarAsync(userId,tenantId,"SOLICITACAO_PLANTAO",requestId,status.ToUpperInvariant(),new{reason,shiftId},true,null,"GESTOR",ct);return ApiResponse<Guid>.Ok(shiftId??requestId);}
        catch(Exception ex){logger.LogError(ex,"Falha ao decidir solicitação {RequestId} no tenant {TenantId}",requestId,tenantId);throw;}
    }
}
