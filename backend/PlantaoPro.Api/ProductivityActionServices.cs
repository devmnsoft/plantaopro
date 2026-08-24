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
        select concat('OPERACAO:PLANTAO:',p.id,':COBERTURA') as Key,'OPERACAO' as Module,'PLANTAO' as EntityType,
          p.id as EntityId,'COBERTURA' as ActionCode,'Plantão sem cobertura' as Title,
          coalesce(nullif(p.nome,''),'Existem vagas abertas para este plantão.') as Description,
          case when x.due_at <= now()+interval '12 hours' then 'CRITICA' when x.due_at <= now()+interval '24 hours' then 'ALTA'
               when x.due_at <= now()+interval '72 hours' then 'NORMAL' else 'BAIXA' end as Priority,
          'ATIVA' as Status,x.due_at as DueAt,p.criado_em as CreatedAt,'EQUIPE' as OwnerType,null::uuid as OwnerId,
          'bi-calendar2-x' as Icon,coalesce(nullif(p.nome,''),'Operação') as ContextLabel,
          '/Plantoes/Detalhes/'||p.id as PrimaryAction,true as CanSnooze,false as CanDismiss,
          coalesce(p.atualizado_em,p.criado_em) as SourceUpdatedAt
        from plantaopro.plantoes p
        cross join lateral (select coalesce(nullif(p.dados->>'dataInicio','')::timestamptz,nullif(p.dados->>'data_inicio','')::timestamptz) due_at,
          coalesce(nullif(p.dados->>'vagasDisponiveis','')::int,nullif(p.dados->>'vagas_disponiveis','')::int,0) vagas) x
        where @operation and p.tenant_id=@tenantId and p.status not in ('CANCELADO','CONCLUIDO','INATIVO') and x.vagas>0 and x.due_at is not null and x.due_at>=now()
        union all
        select concat('OPERACAO:CONVITE:',c.id,':RESPONDER'),'OPERACAO','CONVITE',c.id,'RESPONDER','Convite aguardando resposta',
          'Um convite de plantão aguarda sua resposta.','NORMAL','ATIVA',null,c.criado_em,'USUARIO',c.medico_id,
          'bi-envelope-check','Plantão','/Convites',true,false,coalesce(c.respondido_em,c.reenviado_em,c.criado_em)
        from plantaopro.cobertura_convites c where @operation and c.tenant_id=@tenantId and c.status='PENDENTE'
          and (not @doctorOnly or c.medico_id=@userId)
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
        ";

    public async Task<ProductivityPageDto> ListAsync(Guid tenantId, Guid userId, ProductivityQuery query,
        bool operation, bool clinical, bool financial, bool doctorOnly, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page); var size = Math.Clamp(query.PageSize, 1, 100);
        var tab = (query.Tab ?? "PARA_MIM").Trim().ToUpperInvariant();
        var sql = $@"
            with derived as ({DerivedSql}), visible as (
              select d.*,s.snoozed_until is not null and s.snoozed_until>now() as IsSnoozed
              from derived d left join plantaopro.productivity_item_user_state s
                on s.tenant_id=@tenantId and s.user_id=@userId and s.item_key=d.Key
              where s.dismissed_at is null
            ), filtered as (
              select * from visible where (@priority is null or Priority=@priority) and (@module is null or Module=@module)
                and (@status is null or Status=@status) and (@ownerId is null or OwnerId=@ownerId)
                and (@dueFrom is null or DueAt>=@dueFrom) and (@dueTo is null or DueAt<=@dueTo)
                and (case @tab when 'CRITICAS' then Priority='CRITICA' when 'HOJE' then DueAt>=date_trunc('day',now()) and DueAt<date_trunc('day',now())+interval '1 day'
                     when 'ATRASADAS' then DueAt<now() when 'ADIADAS' then IsSnoozed else not IsSnoozed end)
            )
            select *,count(*) over()::int as TotalRows from filtered
            order by case Priority when 'CRITICA' then 1 when 'ALTA' then 2 when 'NORMAL' then 3 else 4 end,DueAt nulls last,CreatedAt
            offset @offset limit @size
            ";
        await using var cn = Open();
        var args = new { tenantId,userId,operation,clinical,financial,doctorOnly,
            priority=Normalize(query.Priority),module=Normalize(query.Module),status=Normalize(query.Status),query.OwnerId,query.DueFrom,query.DueTo,
            tab,offset=(page-1)*size,size };
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
    IReadOnlyList<QuickActionDto> QuickActions();
}

public sealed class ProductivityActionService : IProductivityActionService
{
    private readonly IProductivityActionRepository repository; private readonly ICurrentUserService current;
    private readonly IPermissionService permissions; private readonly IAuditService audit;
    public ProductivityActionService(IProductivityActionRepository repository,ICurrentUserService current,IPermissionService permissions,IAuditService audit)
    {this.repository=repository;this.current=current;this.permissions=permissions;this.audit=audit;}
    private Guid Tenant=>current.TenantId??throw new UnauthorizedAccessException("Tenant não identificado.");
    private Guid User=>current.UserId??throw new UnauthorizedAccessException("Usuário não identificado.");
    private bool Clinical=>current.HasRole(RolesConstants.Recepcao)||current.HasRole(RolesConstants.Triagem)||current.IsDoctor()||current.HasRole(RolesConstants.CoordenadorClinico);
    private bool Financial=>permissions.CanAccessFinancialArea();
    private bool Operation=>current.IsTenantAdmin()||current.IsDoctor()||current.HasRole(RolesConstants.Coordenacao)||current.HasRole(RolesConstants.Coordenador);
    public Task<ProductivityPageDto> ListAsync(ProductivityQuery query,CancellationToken ct)=>repository.ListAsync(Tenant,User,query,Operation,Clinical,Financial,current.IsDoctor(),ct);
    public async Task<ProductivitySummaryDto> SummaryAsync(CancellationToken ct)
    {
        var all=await ListAsync(new ProductivityQuery(PageSize:100),ct); var now=DateTimeOffset.UtcNow;
        return new(all.Total,all.Items.Count(x=>x.Priority==ProductivityPriority.Critica),all.Items.Count(x=>x.DueAt?.UtcDateTime.Date==now.UtcDateTime.Date),all.Items.Count(x=>x.DueAt<now),
            (await ListAsync(new ProductivityQuery(Tab:"ADIADAS",PageSize:1),ct)).Total);
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
        if(current.IsDoctor())return new[]{new QuickActionDto("MEU_DIA","Meu dia","bi-sun","MeuDia","Index"),new("MINHA_AGENDA","Minha agenda","bi-calendar","Agenda","Index"),new("MEUS_PAGAMENTOS","Meus pagamentos","bi-wallet2","Financeiro","Index")};
        if(Financial&&!Operation)return new[]{new QuickActionDto("PAGAMENTOS","Pagamentos","bi-cash-stack","Financeiro","Index"),new("CONTESTACOES","Contestações","bi-exclamation-diamond","Financeiro","Contestacoes"),new("CAIXA","Caixa","bi-safe","Caixa","Index")};
        if(current.HasRole(RolesConstants.Recepcao))return new[]{new QuickActionDto("NOVO_AGENDAMENTO","Novo agendamento","bi-calendar-plus","Agenda","Novo"),new("BUSCAR_PACIENTE","Buscar paciente","bi-search","Pacientes","Index"),new("PAINEL","Abrir painel","bi-display","Painel","Index")};
        return new[]{new QuickActionDto("NOVO_PLANTAO","Novo plantão","bi-calendar-plus","Plantoes","Novo"),new("COBERTURA","Abrir cobertura","bi-people","CentralEscala","Index"),new("FECHAMENTOS","Ver fechamentos","bi-clipboard-check","Fechamentos","Index")};
    }
}
