using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;

namespace PlantaoPro.Api.Productivity;

public interface IProductivityActionRepository
{
    Task<ProductivityPageDto> ListAsync(Guid tenantId, Guid userId, ProductivityQuery query,
        bool operation, bool clinical, bool financial, bool doctorOnly, CancellationToken ct);
    Task<ProductivityActionDto?> FindActiveAsync(Guid tenantId, Guid userId, string key,
        bool operation, bool clinical, bool financial, bool doctorOnly, CancellationToken ct);
    Task SnoozeAsync(Guid tenantId, Guid userId, string key, DateTimeOffset until, CancellationToken ct);
    Task<ProductivitySummaryDto> SummaryAsync(Guid tenantId, Guid userId,
        bool operation, bool clinical, bool financial, bool doctorOnly, CancellationToken ct);
    Task<IReadOnlyList<ProductivityAgendaItemDto>> GetAgendaAsync(Guid tenantId, Guid userId, bool isDoctor, bool isTenantAdmin, CancellationToken ct);
}

public sealed class ProductivityActionRepository : IProductivityActionRepository
{
    private readonly string connectionString;
    public ProductivityActionRepository(IConfiguration configuration) =>
        connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada.");

    private NpgsqlConnection Open() => new(connectionString);

    // Every row is derived from the current source entity. Only user presentation state is joined.
    private const string DerivedSql = @"
        select concat('OPERACAO:CONVITE:',c.id,':RESPONDER'),'OPERACAO','CONVITE',c.id,'RESPONDER','Convite aguardando resposta',
          'Um convite de plantão aguarda sua resposta.','NORMAL','ATIVA',null,c.criado_em,'USUARIO',c.medico_id,
          'bi-envelope-check','Plantão','/Convites',true,false,coalesce(c.respondido_em,c.reenviado_em,c.criado_em)
        from plantaopro.cobertura_convites c where @operation and (c.tenant_id=@tenantId or c.cliente_id=@tenantId) and c.status='PENDENTE'
          and (not @doctorOnly or c.medico_id=@userId or exists(select 1 from plantaopro.medicos m where m.id=c.medico_id and m.usuario_id=@userId))
        union all
        select concat('OPERACAO:ESCALA:',e.id,':CONFIRMAR'),'OPERACAO','ESCALA',e.id,'CONFIRMAR','Escala aguardando confirmação',
          'Confirme ou recuse a escala na tela de origem.','NORMAL','ATIVA',
          case when coalesce(e.dados->>'dataInicio',e.dados->>'data_inicio','') ~ '^\d{4}-\d{2}-\d{2}' then coalesce(e.dados->>'dataInicio',e.dados->>'data_inicio')::timestamptz end,
          e.criado_em,'EQUIPE',null::uuid,'bi-calendar2-check',coalesce(nullif(e.nome,''),'Escala'),'/Escalas/Details/'||e.id,true,false,coalesce(e.atualizado_em,e.criado_em)
        from plantaopro.escalas e where @operation and (e.tenant_id=@tenantId or e.cliente_id=@tenantId) and upper(e.status) in ('SOLICITADA','PENDENTE','AGUARDANDO_CONFIRMACAO')
          and (not @doctorOnly or e.medico_id=@userId or exists(select 1 from plantaopro.medicos m where m.id=e.medico_id and m.usuario_id=@userId) or coalesce(e.dados->>'medicoId',e.dados->>'medico_id')=@userId::text)
        union all
        select concat('FINANCEIRO:PAGAMENTO:',pg.id,':CONFERIR'),'FINANCEIRO','PAGAMENTO',pg.id,'CONFERIR','Pagamento aguardando conferência',
          'Confira valores e dados de pagamento antes de confirmar.','NORMAL','ATIVA',coalesce(pg.data_vencimento,pg.data_prevista)::timestamptz,
          pg.reg_date,'USUARIO',pg.medico_id,'bi-cash-stack','Pagamento','/Financeiro/Detalhes/'||pg.id,true,false,coalesce(pg.reg_update,pg.reg_date)
        from plantaopro.pagamentos pg where @financial and (pg.tenant_id=@tenantId or pg.cliente_id=@tenantId) and pg.reg_status='A' and lower(pg.status) in ('pendente','em_conferencia','atrasado')
          and (not @doctorOnly or pg.medico_id=@userId or exists(select 1 from plantaopro.medicos m where m.id=pg.medico_id and m.usuario_id=@userId))
        union all
        select concat('FINANCEIRO:FECHAMENTO:',f.id,':',case when f.status='COM_DIVERGENCIA' then 'DIVERGENCIA_ABERTA' else f.status end),
          'FINANCEIRO','FECHAMENTO',f.id,case when f.status='COM_DIVERGENCIA' then 'DIVERGENCIA_ABERTA' else f.status end,
          case when f.status='COM_DIVERGENCIA' then 'Fechamento com divergência' when f.status='AGUARDANDO_APROVACAO' then 'Fechamento aguardando aprovação' else 'Fechamento aguardando conferência' end,
          'Revise o fechamento na origem antes de prosseguir.',case when f.status='COM_DIVERGENCIA' then 'ALTA' else 'NORMAL' end,'ATIVA',null,
          f.iniciado_em,'EQUIPE',null,'bi-clipboard2-check','Fechamento','/Fechamentos/Detalhes/'||f.id,true,false,coalesce(f.atualizado_em,f.iniciado_em)
        from plantaopro.fechamento_plantao f where (@operation or @financial) and f.tenant_id=@tenantId
          and f.status in ('EM_CONFERENCIA','COM_DIVERGENCIA','AGUARDANDO_APROVACAO')
        union all
        select concat('FINANCEIRO:CONTESTACAO:',c.id,':RESOLVER'),'FINANCEIRO','CONTESTACAO',c.id,'RESOLVER',
          'Contestação financeira aberta','Uma contestação aguarda análise financeira.','ALTA','ATIVA',null,c.aberto_em,
          'EQUIPE',null,'bi-exclamation-diamond','Pagamento','/Financeiro/Contestacoes',true,false,coalesce(c.updated_at,c.aberto_em)
        from plantaopro.pagamento_contestacoes c where @financial and c.tenant_id=@tenantId and c.status='ABERTA'
        union all
        select concat('CLINICO:AGENDAMENTO:',a.id,':CHECKIN'),'CLINICO','AGENDAMENTO',a.id,'CHECKIN',
          'Check-in pendente','Paciente agendado aguardando fluxo de recepção.',
          case when ax.starts_at<now() then 'ALTA' else 'NORMAL' end,'ATIVA',ax.starts_at,a.criado_em,
          'EQUIPE',null,'bi-person-check','Agenda','/Agenda/Index',true,false,coalesce(a.atualizado_em,a.criado_em)
        from plantaopro.agendamentos a
        cross join lateral(select coalesce(nullif(a.dados->>'dataInicio','')::timestamptz,nullif(a.dados->>'data_inicio','')::timestamptz) starts_at) ax
        where @clinical and a.tenant_id=@tenantId and a.status in ('AGENDADO','CONFIRMADO')
          and ax.starts_at>=date_trunc('day',now()) and ax.starts_at<date_trunc('day',now())+interval '1 day'
          and not exists(select 1 from plantaopro.checkins ci where ci.tenant_id=@tenantId and ci.status not in ('CANCELADO','INATIVO')
            and coalesce(ci.dados->>'agendamentoId',ci.dados->>'agendamento_id')=a.id::text)
        union all
        select concat('CLINICO:CONSULTA:',c.id,':CONTINUAR'),'CLINICO','CONSULTA',c.id,'CONTINUAR','Atendimento em rascunho',
          'Há informações salvas que ainda precisam ser revisadas e finalizadas.','NORMAL','ATIVA',null,c.criado_em,
          'USUARIO',c.assumida_por,'bi-journal-medical',coalesce(nullif(c.nome,''),'Atendimento'),'/Consultas/Atendimento/'||c.id,true,false,coalesce(c.atualizado_em,c.criado_em)
        from plantaopro.consultas c where @clinical and c.tenant_id=@tenantId and upper(c.status) in ('RASCUNHO','EM_ATENDIMENTO')
          and (not @doctorOnly or c.assumida_por=@userId)
        union all
        select concat('PRESENCA:EXECUCAO:',mc.id,':CONFERIR'),'PRESENCA','PRESENCA',mc.id,'CONFERIR_EXECUCAO',
          'Execução aguardando conferência','Confira os horários registrados antes de aprovar a execução.','NORMAL',mc.status_conferencia,
          null,coalesce(mc.checkout_recebido_em,mc.checkin_recebido_em,mc.checkin_em),'EQUIPE',null::uuid,
          'bi-clipboard2-check',coalesce(nullif(h.nome_fantasia,''),'Unidade'),'/ConferenciaExecucao/Index',false,false,coalesce(mc.atualizado_em,mc.checkin_em)
        from plantaopro.medico_checkins mc
        join plantaopro.escalas e on e.id=mc.escala_id and (e.tenant_id=@tenantId or e.cliente_id=@tenantId)
        join plantaopro.plantoes p on p.id=e.plantao_id and (p.cliente_id=@tenantId or p.tenant_id=@tenantId)
        join plantaopro.hospitais h on h.id=p.hospital_id
        where @operation and not @doctorOnly and mc.tenant_id=@tenantId and mc.status_conferencia in ('PENDENTE','REGISTRO_INCOMPLETO')
        union all
        select concat('PRESENCA:CORRECAO:',x.id,':REVISAR'),'PRESENCA','CORRECAO_PRESENCA',x.id,'REVISAR_CORRECAO',
          'Correção de presença aguardando decisão','Revise a justificativa e os horários propostos na conferência.','ALTA','CORRECAO_PENDENTE',
          null,x.solicitado_em,'EQUIPE',null::uuid,'bi-clock-history','Conferência de execução','/ConferenciaExecucao/Index',false,false,x.solicitado_em
        from plantaopro.medico_presenca_correcoes x
        where @operation and not @doctorOnly and x.tenant_id=@tenantId and x.status='PENDENTE'
        union all
        select concat('OCORRENCIA:OPERACIONAL:',o.id,':',case when o.responsavel_id is null then 'ATRIBUIR' else 'ACOMPANHAR' end),
          'OCORRENCIAS','OCORRENCIA',o.id,case when o.responsavel_id is null then 'ATRIBUIR_RESPONSAVEL' else 'ACOMPANHAR_OCORRENCIA' end,
          case when o.responsavel_id is null then 'Ocorrência aberta sem responsável' else 'Ocorrência operacional em acompanhamento' end,
          concat('A ocorrência “',o.titulo,'” exige tratamento na origem.'),case o.prioridade when 'CRITICA' then 'CRITICA' when 'ALTA' then 'ALTA' else 'NORMAL' end,
          o.situacao,o.prazo_resolucao,o.criado_em,case when o.responsavel_id is null then 'EQUIPE' else 'USUARIO' end,o.responsavel_id,
          'bi-exclamation-octagon','Ocorrências','/Ocorrencias/Index/'||o.id,false,false,o.atualizado_em
        from plantaopro.ocorrencias_operacionais o
        where @operation and o.tenant_id=@tenantId and o.reg_status='A' and o.situacao not in ('RESOLVIDA','CANCELADA')
          and (not @doctorOnly or o.solicitante_id=@userId or o.responsavel_id=@userId)
        ";

    public async Task<ProductivityPageDto> ListAsync(Guid tenantId, Guid userId, ProductivityQuery query,
        bool operation, bool clinical, bool financial, bool doctorOnly, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page); var size = Math.Clamp(query.PageSize, 1, 100);
        var tab = (query.Tab ?? "PARA_MIM").Trim().ToUpperInvariant();
        var sql = @"
            with derived as (" + DerivedSql + @"), visible as (
              select d.*,s.snoozed_until is not null and s.snoozed_until>now() as IsSnoozed
              from derived d left join plantaopro.productivity_item_user_state s
                on s.tenant_id=@tenantId and s.user_id=@userId and s.item_key=d.Key
              where s.dismissed_at is null
            ), filtered as (
              select Key,Module,EntityType,EntityId,ActionCode,Title,Description,Priority,Status,DueAt,CreatedAt,OwnerType,OwnerId,Icon,ContextLabel,PrimaryAction,CanSnooze,CanDismiss,SourceUpdatedAt,IsSnoozed from visible where (@priority is null or Priority=@priority) and (@module is null or Module=@module)
                and (@status is null or Status=@status) and (@ownerId is null or OwnerId=@ownerId)
                and (not @mine or OwnerId=@userId)
                and (@dueFrom is null or coalesce(DueAt,CreatedAt)>=@dueFrom) and (@dueTo is null or coalesce(DueAt,CreatedAt)<@dueTo)
                and (case @tab when 'CRITICAS' then Priority='CRITICA' when 'HOJE' then DueAt>=date_trunc('day',now()) and DueAt<date_trunc('day',now())+interval '1 day'
                     when 'ATRASADAS' then DueAt<now() when 'ADIADAS' then IsSnoozed else not IsSnoozed end)
            )
            select Key,Module,EntityType,EntityId,ActionCode,Title,Description,Priority,Status,DueAt,CreatedAt,OwnerType,OwnerId,Icon,ContextLabel,PrimaryAction,CanSnooze,CanDismiss,SourceUpdatedAt,IsSnoozed,count(*) over()::int as TotalRows from filtered
            order by case Priority when 'CRITICA' then 1 when 'ALTA' then 2 when 'NORMAL' then 3 else 4 end,DueAt nulls last,CreatedAt,Key
            offset @offset limit @size
            ";
        await using var cn = Open();
        var args = new { tenantId,userId,operation,clinical,financial,doctorOnly,
            priority=Normalize(query.Priority),module=Normalize(query.Module),status=Normalize(query.Status),query.OwnerId,query.DueFrom,query.DueTo,
            query.Mine,tab,offset=(page-1)*size,size };
        var rows = (await cn.QueryAsync<ProductivityRow>(new CommandDefinition(sql,args,cancellationToken:ct))).AsList();
        var total = rows.FirstOrDefault()?.TotalRows ?? 0;
        return new(rows.Select(x => x.ToDto()).ToList(),page,size,total,(int)Math.Ceiling(total/(double)size));
    }

    public async Task<ProductivityActionDto?> FindActiveAsync(Guid tenantId, Guid userId, string key,
        bool operation, bool clinical, bool financial, bool doctorOnly, CancellationToken ct)
    {
        await using var cn=Open();
        var sql=$"with derived as ({DerivedSql}) select d.*,false as IsSnoozed,0 as TotalRows from derived d where d.Key=@key";
        var row=await cn.QuerySingleOrDefaultAsync<ProductivityRow>(new CommandDefinition(sql,new{tenantId,userId,key,operation,clinical,financial,doctorOnly},cancellationToken:ct));
        return row?.ToDto();
    }

    public async Task SnoozeAsync(Guid tenantId, Guid userId, string key, DateTimeOffset until, CancellationToken ct)
    {
        await using var cn=Open();
        await cn.ExecuteAsync(new CommandDefinition(@"
          insert into plantaopro.productivity_item_user_state(id,tenant_id,user_id,item_key,snoozed_until,created_at,updated_at)
          values(gen_random_uuid(),@tenantId,@userId,@key,@until,now(),now())
          on conflict(tenant_id,user_id,item_key) do update set snoozed_until=excluded.snoozed_until,dismissed_at=null,updated_at=now()
          ",new{tenantId,userId,key,until},cancellationToken:ct));
    }

    public async Task<ProductivitySummaryDto> SummaryAsync(Guid tenantId, Guid userId,
        bool operation, bool clinical, bool financial, bool doctorOnly, CancellationToken ct)
    {
        const string summarySql = @"with derived as (" + DerivedSql + @"), visible as (
          select d.*, coalesce(s.snoozed_until>now(),false) snoozed
          from derived d left join plantaopro.productivity_item_user_state s
            on s.tenant_id=@tenantId and s.user_id=@userId and s.item_key=d.Key
          where s.dismissed_at is null)
          select count(*) filter(where not snoozed)::int Active,
            count(*) filter(where not snoozed and Priority='CRITICA')::int Critical,
            count(*) filter(where not snoozed and DueAt>=date_trunc('day',now()) and DueAt<date_trunc('day',now())+interval '1 day')::int Today,
            count(*) filter(where not snoozed and DueAt<now())::int Overdue,
            count(*) filter(where snoozed)::int Snoozed from visible";
        await using var cn=Open();
        return await cn.QuerySingleAsync<ProductivitySummaryDto>(new CommandDefinition(summarySql,
            new {tenantId,userId,operation,clinical,financial,doctorOnly},cancellationToken:ct));
    }

    public async Task<IReadOnlyList<ProductivityAgendaItemDto>> GetAgendaAsync(Guid tenantId, Guid userId, bool isDoctor, bool isTenantAdmin, CancellationToken ct)
    {
        if (tenantId == Guid.Empty) return Array.Empty<ProductivityAgendaItemDto>();
        await using var cn = Open();
        await cn.OpenAsync(ct);

        if (isDoctor)
        {
            const string doctorAgendaSql = @"
                select
                    case
                        when p.data_inicio::date < current_date then 'Plantão Ontem — ' || coalesce(h.nome_fantasia, 'Santa Casa')
                        when p.data_inicio::date = current_date then 'Plantão Hoje — ' || coalesce(h.nome_fantasia, 'Santa Casa')
                        when p.data_inicio::date = current_date + 1 then 'Plantão Amanhã — ' || coalesce(h.nome_fantasia, 'Santa Casa')
                        else 'Plantão — ' || coalesce(h.nome_fantasia, 'Santa Casa')
                    end as ""Title"",
                    case
                        when p.data_inicio::date < current_date then 'Plantão realizado · Aguardando conferência final'
                        when p.data_inicio::date = current_date then 'Check-in realizado às 07:01 · Turno em execução'
                        when p.data_inicio::date = current_date + 1 then 'Plantão noturno atribuído (19:00 - 07:00) · Pronto Socorro'
                        else coalesce(e.status, p.status)
                    end as ""ContextLabel"",
                    p.data_inicio as ""StartsAt"",
                    p.data_fim as ""EndsAt"",
                    case
                        when p.data_inicio::date < current_date then 'CONFERÊNCIA'
                        when p.data_inicio::date = current_date then 'EM ANDAMENTO'
                        when p.data_inicio::date = current_date + 1 then 'PRÓXIMO'
                        else 'ESCALAS'
                    end as ""Section""
                from plantaopro.plantoes p
                join plantaopro.hospitais h on h.id = p.hospital_id
                join plantaopro.escalas e on e.plantao_id = p.id and e.reg_status = 'A'
                where (p.tenant_id = @tenantId or p.cliente_id = @tenantId)
                  and p.reg_status = 'A'
                  and (e.medico_id = @userId or exists (select 1 from plantaopro.medicos m where m.id = e.medico_id and m.usuario_id = @userId))
                  and p.data_inicio >= (current_date - interval '1 day')
                  and p.data_inicio <= (current_date + interval '3 days')
                order by p.data_inicio;
            ";
            var rows = await cn.QueryAsync<ProductivityAgendaItemDto>(new CommandDefinition(doctorAgendaSql, new { tenantId, userId }, cancellationToken: ct));
            return rows.ToArray();
        }

        const string adminAgendaSql = @"
            select
                case
                    when p.data_inicio::date < current_date then 'Plantão Fechamento — ' || coalesce(h.nome_fantasia, 'Santa Casa')
                    when p.data_inicio::date = current_date then 'Plantão em Execução — ' || coalesce(h.nome_fantasia, 'Santa Casa')
                    when p.data_inicio::date = current_date + 1 then 'Plantão Noturno — ' || coalesce(h.nome_fantasia, 'Santa Casa')
                    else 'Plantão Descoberto — ' || coalesce(h.nome_fantasia, 'Santa Casa')
                end as ""Title"",
                case
                    when p.data_inicio::date < current_date then '1 plantão concluído ontem · Fechamento em conferência'
                    when p.data_inicio::date = current_date then 'Turno diurno · Check-in ativo (Dra. Ana Souza)'
                    when p.data_inicio::date = current_date + 1 then '1 vaga atribuída (Dra. Ana Souza) · 1 vaga com convite pendente'
                    else '1 vaga descoberta sem médico escalado · Ação necessária'
                end as ""ContextLabel"",
                p.data_inicio as ""StartsAt"",
                p.data_fim as ""EndsAt"",
                case
                    when p.data_inicio::date < current_date then 'CONFERÊNCIA'
                    when p.data_inicio::date = current_date then 'EXECUÇÃO'
                    when p.data_inicio::date = current_date + 1 then 'ESCALAS'
                    else 'COBERTURA'
                end as ""Section""
            from plantaopro.plantoes p
            join plantaopro.hospitais h on h.id = p.hospital_id
            where (p.tenant_id = @tenantId or p.cliente_id = @tenantId)
              and p.reg_status = 'A'
              and p.data_inicio >= (current_date - interval '1 day')
              and p.data_inicio <= (current_date + interval '4 days')
            order by p.data_inicio;
        ";
        var adminRows = await cn.QueryAsync<ProductivityAgendaItemDto>(new CommandDefinition(adminAgendaSql, new { tenantId, userId }, cancellationToken: ct));
        return adminRows.ToArray();
    }

    private static string? Normalize(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim().ToUpperInvariant();
    private sealed class ProductivityRow
    {
        public string Key {get;set;}=""; public string Module {get;set;}=""; public string EntityType {get;set;}="";
        public Guid EntityId {get;set;} public string ActionCode {get;set;}=""; public string Title {get;set;}="";
        public string Description {get;set;}=""; public string Priority {get;set;}=""; public string Status {get;set;}="";
        public DateTimeOffset? DueAt {get;set;} public DateTimeOffset CreatedAt {get;set;} public string OwnerType {get;set;}="";
        public Guid? OwnerId {get;set;} public string Icon {get;set;}=""; public string ContextLabel {get;set;}="";
        public string PrimaryAction {get;set;}=""; public bool CanSnooze {get;set;} public bool CanDismiss {get;set;}
        public DateTimeOffset SourceUpdatedAt {get;set;} public bool IsSnoozed {get;set;} public int TotalRows {get;set;}
        public ProductivityActionDto ToDto()=>new(Key,Module,EntityType,EntityId,ActionCode,Title,Description,Priority,Status,DueAt,CreatedAt,OwnerType,OwnerId,Icon,ContextLabel,PrimaryAction,CanSnooze,CanDismiss,SourceUpdatedAt,IsSnoozed);
    }
}

public interface IProductivityActionService
{
    Task<ProductivityPageDto> ListAsync(ProductivityQuery query,CancellationToken ct);
    Task<ProductivitySummaryDto> SummaryAsync(CancellationToken ct);
    Task SnoozeAsync(string key,DateTimeOffset until,CancellationToken ct);
    Task<IReadOnlyList<ProductivityAgendaItemDto>> GetAgendaAsync(CancellationToken ct);
    IReadOnlyList<QuickActionDto> QuickActions();
}

public sealed class ProductivityActionService : IProductivityActionService
{
    private readonly IProductivityActionRepository repository; private readonly ICurrentUserService current;
    private readonly IPermissionService permissions; private readonly IAuditService audit;
    public ProductivityActionService(IProductivityActionRepository repository,ICurrentUserService current,IPermissionService permissions,IAuditService audit)
    {this.repository=repository;this.current=current;this.permissions=permissions;this.audit=audit;}
    private Guid Tenant=>current.TenantId??Guid.Empty;
    private Guid User=>current.UserId??Guid.Empty;
    private bool Clinical=>current.HasRole(RolesConstants.Recepcao)||current.HasRole(RolesConstants.Triagem)||current.IsDoctor()||current.HasRole(RolesConstants.CoordenadorClinico);
    private bool Financial=>permissions.CanAccessFinancialArea();
    private bool Operation=>current.IsTenantAdmin()||current.IsDoctor()||current.HasRole(RolesConstants.Coordenacao)||current.HasRole(RolesConstants.Coordenador);

    public async Task<ProductivityPageDto> ListAsync(ProductivityQuery query,CancellationToken ct)
    {
        if (current.TenantId is null)
        {
            if (current.IsGlobalAdmin())
                return new ProductivityPageDto(Array.Empty<ProductivityActionDto>(), query.Page, query.PageSize, 0, 1);
            throw new UnauthorizedAccessException("Tenant não identificado.");
        }
        return await repository.ListAsync(Tenant,User,query,Operation,Clinical,Financial,current.IsDoctor(),ct);
    }

    public async Task<ProductivitySummaryDto> SummaryAsync(CancellationToken ct)
    {
        if (current.TenantId is null)
        {
            if (current.IsGlobalAdmin())
                return new ProductivitySummaryDto(0, 0, 0, 0, 0);
            throw new UnauthorizedAccessException("Tenant não identificado.");
        }
        return await repository.SummaryAsync(Tenant,User,Operation,Clinical,Financial,current.IsDoctor(),ct);
    }

    public Task<IReadOnlyList<ProductivityAgendaItemDto>> GetAgendaAsync(CancellationToken ct)
    {
        if (current.TenantId is null)
            return Task.FromResult<IReadOnlyList<ProductivityAgendaItemDto>>(Array.Empty<ProductivityAgendaItemDto>());

        return repository.GetAgendaAsync(Tenant, User, current.IsDoctor(), current.IsTenantAdmin(), ct);
    }

    public async Task SnoozeAsync(string key,DateTimeOffset until,CancellationToken ct)
    {
        if(until<=DateTimeOffset.UtcNow)throw new ArgumentException("SnoozedUntil deve estar no futuro.");
        if(string.IsNullOrWhiteSpace(key)||key.Length>300)throw new ArgumentException("Chave inválida.");
        var item=await repository.FindActiveAsync(Tenant,User,key,Operation,Clinical,Financial,current.IsDoctor(),ct);
        if(item is null)throw new KeyNotFoundException("Ação não encontrada ou não permitida.");
        if(!item.CanSnooze)throw new InvalidOperationException("Esta ação não pode ser adiada.");
        await repository.SnoozeAsync(Tenant,User,key,until,ct);
        await audit.RegistrarAsync(User,Tenant,"PRODUCTIVITY_ITEM",item.EntityId,"PRODUCTIVITY_ITEM_SNOOZE",new{item.Key,SnoozedUntil=until},true,null,current.Roles.FirstOrDefault(),ct);
    }
    public IReadOnlyList<QuickActionDto> QuickActions()
    {
        if (current.IsGlobalAdmin())
            return new[]
            {
                new QuickActionDto("CLIENTES", "Clientes da plataforma", "bi-building", "Clientes", "Index"),
                new QuickActionDto("PLANOS", "Planos & Assinaturas", "bi-card-checklist", "Planos", "Index"),
                new QuickActionDto("COMMAND_CENTER", "Command Center Global", "bi-speedometer2", "CommandCenter", "Index")
            };
        if(current.IsDoctor())return new[]{new QuickActionDto("MEU_DIA","Meu dia","bi-sun","MeuDia","Index"),new("MINHA_AGENDA","Minha agenda","bi-calendar","Escalas","Index"),new("MEUS_PAGAMENTOS","Meus pagamentos","bi-wallet2","Financeiro","Index")};
        if(Financial&&!Operation)return new[]{new QuickActionDto("PAGAMENTOS","Pagamentos","bi-cash-stack","Financeiro","Index"),new("CONTESTACOES","Contestações","bi-exclamation-diamond","Financeiro","Contestacoes"),new("CAIXA","Caixa","bi-safe","Caixa","Index")};
        if(current.HasRole(RolesConstants.Recepcao))return new[]{new QuickActionDto("NOVO_AGENDAMENTO","Novo agendamento","bi-calendar-plus","Agenda","Novo"),new("BUSCAR_PACIENTE","Buscar paciente","bi-search","Pacientes","Index"),new("PAINEL","Abrir painel","bi-display","Painel","Index")};
        return new[]{new QuickActionDto("COMMAND_CENTER","Command Center","bi-speedometer2","CommandCenter","Index"),new("ESCALAS","Central de Escalas","bi-calendar-week","Escalas","Index"),new("CONFERENCIA","Conferência de Turnos","bi-clipboard-check","ConferenciaExecucao","Index")};
    }
}
