using System.Security.Claims;
using Dapper;
using Npgsql;
using PlantaoPro.CrossCutting.Security;
using PlantaoPro.Api.Controllers;

namespace PlantaoPro.Api;

public sealed record ContextoAtualDto(Guid? UsuarioId, Guid? TenantId, Guid? ClienteId, string ContextMode, string AccessScope, string? PrimaryRole, string? TenantContextId);
public sealed record TenantDisponivelDto(Guid TenantId, Guid ClienteId, string Cliente, string Tenant, string Plano, bool Ativo);
public sealed record ContextoTrocaDto(Guid Id, Guid? TenantId, string Evento, DateTime TimestampUtc);
public sealed record ContextSelectionDto(Guid SessionId, Guid TenantId, Guid? ClienteId, string ContextMode);

public interface IContextoRepository
{
    Task<IReadOnlyList<TenantDisponivelDto>> TenantsAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<ContextoTrocaDto>> HistoryAsync(Guid userId, int take, CancellationToken ct);
    Task<ContextSelectionDto?> SelectAsync(Guid userId, Guid tenantId, string? reason, CancellationToken ct);
    Task<bool> ReturnGlobalAsync(Guid userId, string? reason, CancellationToken ct);
}

public sealed class ContextoRepository : IContextoRepository
{
    private readonly string connectionString;
    public ContextoRepository(IConfiguration configuration) => connectionString = configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada.");
    private NpgsqlConnection Open() => new NpgsqlConnection(connectionString);
    public async Task<IReadOnlyList<TenantDisponivelDto>> TenantsAsync(Guid userId, CancellationToken ct)
    {
        const string sql = @"select a.tenant_id TenantId,a.cliente_id ClienteId,coalesce(c.nome,'') Cliente,coalesce(t.nome,'') Tenant,coalesce(s.nome,s.codigo,'') Plano,true Ativo
from plantaopro.usuario_tenant_acessos a join plantaopro.usuarios u on u.id=a.usuario_id and u.reg_status='A' and u.status='ATIVO'
join plantaopro.tenants t on t.id=a.tenant_id and t.status='ATIVO' left join plantaopro.clientes c on c.id=a.cliente_id and c.status='ATIVO'
left join lateral(select nome,codigo from plantaopro.assinaturas s where s.tenant_id=a.tenant_id and s.status='ATIVO' order by s.criado_em desc limit 1)s on true
where a.usuario_id=@userId and a.reg_status='A' and a.status='ATIVO' and (a.acesso_fim is null or a.acesso_fim>now()) order by c.nome,t.nome";
        await using var cn=Open(); return (await cn.QueryAsync<TenantDisponivelDto>(new CommandDefinition(sql,new{userId},cancellationToken:ct))).AsList();
    }
    public async Task<IReadOnlyList<ContextoTrocaDto>> HistoryAsync(Guid userId,int take,CancellationToken ct)
    { await using var cn=Open(); return (await cn.QueryAsync<ContextoTrocaDto>(new CommandDefinition("select id,tenant_destino_id TenantId,modo_destino Evento,reg_date TimestampUtc from plantaopro.contexto_trocas where usuario_id=@userId and reg_status='A' order by reg_date desc limit @take",new{userId,take=Math.Clamp(take,1,100)},cancellationToken:ct))).AsList(); }
    public async Task<ContextSelectionDto?> SelectAsync(Guid userId,Guid tenantId,string? reason,CancellationToken ct)
    {
        await using var cn=Open(); await cn.OpenAsync(ct); await using var tx=await cn.BeginTransactionAsync(ct);
        var access=await cn.QuerySingleOrDefaultAsync<(Guid TenantId,Guid? ClienteId,string Perfil)>(new CommandDefinition(@"select a.tenant_id TenantId,a.cliente_id ClienteId,coalesce(p.codigo,p.nome,'USUARIO') Perfil from plantaopro.usuario_tenant_acessos a
join plantaopro.usuarios u on u.id=a.usuario_id and u.reg_status='A' and u.status='ATIVO' join plantaopro.tenants t on t.id=a.tenant_id and t.status='ATIVO'
left join plantaopro.clientes c on c.id=a.cliente_id left join plantaopro.perfis p on p.id=a.perfil_id and p.reg_status='A' and p.status='ATIVO'
where a.usuario_id=@userId and a.tenant_id=@tenantId and a.reg_status='A' and a.status='ATIVO' and (a.acesso_fim is null or a.acesso_fim>now()) and (c.id is null or c.status='ATIVO')",new{userId,tenantId},tx,cancellationToken:ct));
        if(access.TenantId==Guid.Empty){await tx.RollbackAsync(ct);return null;}
        await cn.ExecuteAsync(new CommandDefinition("update plantaopro.contexto_sessoes set encerrado_em=now(),reg_status='I' where usuario_id=@userId and encerrado_em is null and reg_status='A'",new{userId},tx,cancellationToken:ct));
        var id=Guid.NewGuid();
        await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.contexto_sessoes(id,sessao_id,usuario_id,tenant_id,cliente_id,modo,perfil_efetivo) values(@id,@session,@userId,@tenantId,@ClienteId,'TENANT',@Perfil)",new{id,session=id.ToString("N"),userId,tenantId,access.ClienteId,access.Perfil},tx,cancellationToken:ct));
        await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.contexto_trocas(contexto_sessao_id,usuario_id,tenant_destino_id,modo_destino,motivo) values(@id,@userId,@tenantId,'TENANT',@reason)",new{id,userId,tenantId,reason=Sanitize(reason)},tx,cancellationToken:ct));
        await cn.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.usuario_contextos_recentes(usuario_id,tenant_id,cliente_id) values(@userId,@tenantId,@ClienteId)
on conflict(usuario_id,tenant_id) do update set cliente_id=excluded.cliente_id,ultimo_acesso_em=now(),total_acessos=plantaopro.usuario_contextos_recentes.total_acessos+1,reg_status='A'",new{userId,tenantId,access.ClienteId},tx,cancellationToken:ct));
        await tx.CommitAsync(ct); return new ContextSelectionDto(id,tenantId,access.ClienteId,"TENANT");
    }
    public async Task<bool> ReturnGlobalAsync(Guid userId,string? reason,CancellationToken ct)
    { await using var cn=Open(); await cn.OpenAsync(ct); await using var tx=await cn.BeginTransactionAsync(ct); var n=await cn.ExecuteAsync(new CommandDefinition("update plantaopro.contexto_sessoes set encerrado_em=now(),reg_status='I' where usuario_id=@userId and encerrado_em is null and reg_status='A'",new{userId},tx,cancellationToken:ct)); await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.contexto_trocas(usuario_id,modo_destino,motivo) values(@userId,'GLOBAL',@reason)",new{userId,reason=Sanitize(reason)},tx,cancellationToken:ct)); await tx.CommitAsync(ct); return n>=0; }
    private static string? Sanitize(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim().Substring(0,Math.Min(value.Trim().Length,500));
}

public interface IContextoService
{
    ContextoAtualDto Atual(ClaimsPrincipal user);
    Task<IReadOnlyList<TenantDisponivelDto>> TenantsDisponiveisAsync(ClaimsPrincipal user,CancellationToken ct);
    Task<IReadOnlyList<ContextoTrocaDto>> RecentesAsync(ClaimsPrincipal user,CancellationToken ct);
    Task<ContextSelectionDto> SelecionarAsync(ClaimsPrincipal user,SelecionarContextoRequest request,CancellationToken ct);
    Task RetornarGlobalAsync(ClaimsPrincipal user,CancellationToken ct);
    Task<IReadOnlyList<ContextoTrocaDto>> HistoricoAsync(ClaimsPrincipal user,CancellationToken ct);
}
public sealed class ContextTokenService { public string ModeTenant => "TENANT"; public string ModeGlobal => "GLOBAL"; }
public sealed class ContextAuthorizationService { public bool CanSwitch(ClaimsPrincipal user)=>user.Identity?.IsAuthenticated==true; }
public sealed class ContextoService : IContextoService
{
    private readonly IContextoRepository repository;
    public ContextoService(IContextoRepository repository)=>this.repository=repository;
    public ContextoAtualDto Atual(ClaimsPrincipal user)=>new(UserId(user),Parse(user.FindFirstValue("tenant_id")),Parse(user.FindFirstValue("cliente_id")),user.FindFirstValue("context_mode")??"GLOBAL",user.FindFirstValue("access_scope")??AccessScopes.Global,user.FindFirstValue("primary_role")??user.FindFirstValue(ClaimTypes.Role),user.FindFirstValue("tenant_context_id"));
    public Task<IReadOnlyList<TenantDisponivelDto>> TenantsDisponiveisAsync(ClaimsPrincipal u,CancellationToken ct)=>repository.TenantsAsync(Required(u),ct);
    public Task<IReadOnlyList<ContextoTrocaDto>> RecentesAsync(ClaimsPrincipal u,CancellationToken ct)=>repository.HistoryAsync(Required(u),10,ct);
    public async Task<ContextSelectionDto> SelecionarAsync(ClaimsPrincipal u,SelecionarContextoRequest r,CancellationToken ct)=>await repository.SelectAsync(Required(u),r.TenantId,r.Motivo,ct)??throw new UnauthorizedAccessException("Tenant indisponível para este usuário.");
    public async Task RetornarGlobalAsync(ClaimsPrincipal u,CancellationToken ct)=>await repository.ReturnGlobalAsync(Required(u),"Retorno solicitado pelo usuário",ct);
    public Task<IReadOnlyList<ContextoTrocaDto>> HistoricoAsync(ClaimsPrincipal u,CancellationToken ct)=>repository.HistoryAsync(Required(u),100,ct);
    private static Guid Required(ClaimsPrincipal u)=>UserId(u)??throw new UnauthorizedAccessException("Usuário não identificado.");
    private static Guid? UserId(ClaimsPrincipal u)=>Parse(u.FindFirstValue("uid")??u.FindFirstValue(ClaimTypes.NameIdentifier)); private static Guid? Parse(string? s)=>Guid.TryParse(s,out var id)?id:null;
}

public sealed record ImpersonationSessionDto(Guid Id,Guid OriginalUserId,Guid ImpersonatedUserId,Guid TenantId,DateTime ExpiresAt,string Status);
public interface IImpersonationRepository
{
    Task<IReadOnlyList<object>> UsersAsync(Guid tenantId,CancellationToken ct); Task<ImpersonationSessionDto?> StartAsync(Guid original,Guid target,Guid tenant,string reason,string ticket,int minutes,CancellationToken ct); Task<bool> EndAsync(Guid original,Guid session,CancellationToken ct); Task<IReadOnlyList<object>> HistoryAsync(Guid original,CancellationToken ct);
}
public sealed class ImpersonationRepository : IImpersonationRepository
{
    private readonly string cs; public ImpersonationRepository(IConfiguration c)=>cs=c.GetConnectionString("Default")??throw new InvalidOperationException("ConnectionStrings:Default não configurada."); private NpgsqlConnection Open()=>new NpgsqlConnection(cs);
    public async Task<IReadOnlyList<object>> UsersAsync(Guid tenantId,CancellationToken ct){await using var cn=Open();return (await cn.QueryAsync<object>(new CommandDefinition("select distinct u.id,u.nome,u.email from plantaopro.usuarios u join plantaopro.usuario_tenant_acessos a on a.usuario_id=u.id and a.tenant_id=@tenantId and a.status='ATIVO' and a.reg_status='A' where u.reg_status='A' and u.status='ATIVO' order by u.nome",new{tenantId},cancellationToken:ct))).AsList();}
    public async Task<ImpersonationSessionDto?> StartAsync(Guid original,Guid target,Guid tenant,string reason,string ticket,int minutes,CancellationToken ct){await using var cn=Open();await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct); if(original==target)return null; var valid=await cn.ExecuteScalarAsync<bool>(new CommandDefinition("select exists(select 1 from plantaopro.usuario_tenant_acessos a join plantaopro.usuarios u on u.id=a.usuario_id and u.reg_status='A' and u.status='ATIVO' where a.usuario_id=@target and a.tenant_id=@tenant and a.status='ATIVO' and a.reg_status='A')",new{target,tenant},tx,cancellationToken:ct)); if(!valid)return null; var active=await cn.ExecuteScalarAsync<bool>(new CommandDefinition("select exists(select 1 from plantaopro.impersonacao_sessoes where usuario_origem_id=@original and status='ATIVA' and encerrado_em is null and expira_em>now())",new{original},tx,cancellationToken:ct));if(active)return null;var id=Guid.NewGuid();var expires=DateTime.UtcNow.AddMinutes(minutes);await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.impersonacao_sessoes(id,usuario_origem_id,usuario_alvo_id,tenant_id,motivo,ticket_referencia,expira_em) values(@id,@original,@target,@tenant,@reason,@ticket,@expires)",new{id,original,target,tenant,reason=reason.Trim(),ticket=ticket.Trim(),expires},tx,cancellationToken:ct));await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.impersonacao_eventos(impersonacao_sessao_id,usuario_origem_id,usuario_alvo_id,evento) values(@id,@original,@target,'INICIO')",new{id,original,target},tx,cancellationToken:ct));await tx.CommitAsync(ct);return new(id,original,target,tenant,expires,"ATIVA");}
    public async Task<bool> EndAsync(Guid original,Guid session,CancellationToken ct){await using var cn=Open();await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);var target=await cn.ExecuteScalarAsync<Guid?>(new CommandDefinition("update plantaopro.impersonacao_sessoes set status='ENCERRADA',encerrado_em=now(),encerrado_por=@original where id=@session and usuario_origem_id=@original and status='ATIVA' and encerrado_em is null returning usuario_alvo_id",new{session,original},tx,cancellationToken:ct));if(!target.HasValue){await tx.RollbackAsync(ct);return false;}await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.impersonacao_eventos(impersonacao_sessao_id,usuario_origem_id,usuario_alvo_id,evento) values(@session,@original,@target,'ENCERRAMENTO')",new{session,original,target},tx,cancellationToken:ct));await tx.CommitAsync(ct);return true;}
    public async Task<IReadOnlyList<object>> HistoryAsync(Guid original,CancellationToken ct){await using var cn=Open();return(await cn.QueryAsync<object>(new CommandDefinition("select id,usuario_alvo_id,tenant_id,motivo,ticket_referencia,iniciado_em,expira_em,encerrado_em,case when status='ATIVA' and expira_em<=now() then 'EXPIRADA' else status end status from plantaopro.impersonacao_sessoes where usuario_origem_id=@original order by iniciado_em desc limit 100",new{original},cancellationToken:ct))).AsList();}
}
public sealed class ImpersonationTokenService{public DateTime ExpiresAt(int? m)=>DateTime.UtcNow.AddMinutes(Math.Clamp(m??30,1,30));}
public sealed class ImpersonationAuthorizationService{public bool CanStart(ClaimsPrincipal u)=>u.Identity?.IsAuthenticated==true&&u.FindFirstValue("impersonation")!="true";}
public interface IImpersonationService{Task<IReadOnlyList<object>> UsuariosAsync(Guid tenant,CancellationToken ct);Task<ImpersonationSessionDto> IniciarAsync(ClaimsPrincipal u,IniciarImpersonacaoRequest r,CancellationToken ct);Task EncerrarAsync(ClaimsPrincipal u,EncerrarImpersonacaoRequest r,CancellationToken ct);object Atual(ClaimsPrincipal u);Task<IReadOnlyList<object>> HistoricoAsync(ClaimsPrincipal u,CancellationToken ct);}
public sealed class ImpersonationService:IImpersonationService
{
    private readonly IImpersonationRepository repo;public ImpersonationService(IImpersonationRepository repo)=>this.repo=repo;
    public Task<IReadOnlyList<object>> UsuariosAsync(Guid t,CancellationToken ct)=>repo.UsersAsync(t,ct);
    public async Task<ImpersonationSessionDto> IniciarAsync(ClaimsPrincipal u,IniciarImpersonacaoRequest r,CancellationToken ct){if(u.FindFirstValue("impersonation")=="true")throw new InvalidOperationException("Impersonação encadeada não é permitida.");var minutes=r.DuracaoMinutos??30;if(minutes<1||minutes>30)throw new ArgumentOutOfRangeException(nameof(r.DuracaoMinutos),"Duração máxima de 30 minutos.");return await repo.StartAsync(Id(u),r.UsuarioAlvoId,r.TenantId,r.Motivo,r.TicketReferencia,minutes,ct)??throw new UnauthorizedAccessException("Usuário alvo ou sessão de impersonação inválida.");}
    public async Task EncerrarAsync(ClaimsPrincipal u,EncerrarImpersonacaoRequest r,CancellationToken ct){var id=r.ImpersonacaoSessaoId??Parse(u.FindFirstValue("impersonation_session_id"))??throw new ArgumentException("Sessão obrigatória.");if(!await repo.EndAsync(Id(u),id,ct))throw new KeyNotFoundException("Sessão ativa não encontrada.");}
    public object Atual(ClaimsPrincipal u)=>new{impersonation=u.FindFirstValue("impersonation")=="true",originalUserId=u.FindFirstValue("original_user_id"),impersonatedUserId=u.FindFirstValue("impersonated_user_id"),impersonationSessionId=u.FindFirstValue("impersonation_session_id"),impersonationExpiresAt=u.FindFirstValue("impersonation_expires_at")};
    public Task<IReadOnlyList<object>> HistoricoAsync(ClaimsPrincipal u,CancellationToken ct)=>repo.HistoryAsync(Id(u),ct);
    private static Guid Id(ClaimsPrincipal u)=>Parse(u.FindFirstValue("uid")??u.FindFirstValue(ClaimTypes.NameIdentifier))??throw new UnauthorizedAccessException();private static Guid? Parse(string? s)=>Guid.TryParse(s,out var id)?id:null;
}
