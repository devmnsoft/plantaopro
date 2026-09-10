using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace PlantaoPro.Api;

public sealed record EffectivePermissionDiagnostic(bool Permitido, string Codigo, string Motivo, string Origem);

public sealed class SaasUserUpsertRequest
{
    public Guid? TenantId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? SenhaTemporaria { get; set; }
    public Guid[] PerfilIds { get; set; } = Array.Empty<Guid>();
}

public sealed class SaasAssignableProfileDto
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public bool BaseSistema { get; set; }
}

public sealed record SecurityProfileRequest(string Nome, string? Descricao);
public sealed record SecurityProfilePermissionRequest(Guid PermissaoId, bool Permitido);
public sealed record SecurityProfilePermissionsRequest(IReadOnlyList<SecurityProfilePermissionRequest> Permissoes);
public sealed record SecurityPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total);

public interface IEffectivePermissionService
{
    Task<EffectivePermissionDiagnostic> TestarAsync(Guid usuarioId, Guid? tenantId, string modulo, string acao, CancellationToken ct = default);
    Task<IEnumerable<string>> ObterPermissoesAsync(Guid usuarioId, Guid? tenantId, CancellationToken ct = default);
}

public interface IPasswordPolicyService
{
    Task<object> ObterAsync(Guid? tenantId, CancellationToken ct = default);
}

public sealed class EffectivePermissionService : IEffectivePermissionService
{
    private readonly IConfiguration cfg;
    private static readonly HashSet<string> CoreModules = new(StringComparer.OrdinalIgnoreCase)
    {
        "MEU_DIA", "AJUDA", "LGPD", "CONTA", "TREINAMENTO", "USUARIOS", "PERFIS",
        "PERMISSOES", "CONFIGURACOES", "SEGURANCA", "ASSINATURAS", "CLIENTE_PORTAL"
    };
    public EffectivePermissionService(IConfiguration cfg) { this.cfg = cfg; }
    public async Task<IEnumerable<string>> ObterPermissoesAsync(Guid usuarioId, Guid? tenantId, CancellationToken ct = default)
    {
        const string sql = @"with granted as (
select distinct upper(replace(coalesce(p.codigo, ms.codigo || '.' || ac.codigo),':','.')) codigo
from plantaopro.usuarios u
join plantaopro.usuarios_perfis up on up.usuario_id=u.id and up.reg_status='A'
join plantaopro.perfis pf on pf.id=up.perfil_id and pf.reg_status='A' and coalesce(pf.status,'ATIVO')='ATIVO'
join plantaopro.perfil_permissoes pp on pp.perfil_id=pf.id and pp.reg_status='A' and coalesce(pp.permitido,true)=true and coalesce(pp.bloqueado_por_plano,false)=false
join plantaopro.permissoes p on p.id=pp.permissao_id and p.reg_status='A'
left join plantaopro.modulos_sistema ms on ms.id=p.modulo_id
left join plantaopro.acoes_sistema ac on ac.id=p.acao_id
where u.id=@usuarioId and u.reg_status='A'
  and (@tenantId is null or up.tenant_id is null or up.tenant_id=@tenantId)
  and (@tenantId is null or pf.tenant_id is null or pf.tenant_id=@tenantId)
union
select distinct upper(replace(p.codigo,':','.'))
from plantaopro.usuario_permissoes_especiais upe
join plantaopro.permissoes p on p.id=upe.permissao_id and p.reg_status='A'
where upe.usuario_id=@usuarioId and upe.reg_status='A' and upe.permitido=true and (@tenantId is null or upe.tenant_id is null or upe.tenant_id=@tenantId)
), denied as (
select distinct upper(replace(p.codigo,':','.')) codigo
from plantaopro.usuario_permissoes_especiais upe
join plantaopro.permissoes p on p.id=upe.permissao_id and p.reg_status='A'
where upe.usuario_id=@usuarioId and upe.reg_status='A' and upe.permitido=false and (@tenantId is null or upe.tenant_id is null or upe.tenant_id=@tenantId)
)
select g.codigo from granted g where not exists(select 1 from denied d where d.codigo=g.codigo) order by g.codigo";
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        return await cn.QueryAsync<string>(new CommandDefinition(sql, new { usuarioId, tenantId }, cancellationToken: ct));
    }
    public async Task<EffectivePermissionDiagnostic> TestarAsync(Guid usuarioId, Guid? tenantId, string modulo, string acao, CancellationToken ct = default)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var usuario = await cn.QuerySingleOrDefaultAsync<(string RegStatus, string Status, Guid? TenantId)>(new CommandDefinition(@"select u.reg_status RegStatus, coalesce(u.status,'ATIVO') Status,
coalesce(u.tenant_id,u.cliente_id) TenantId
from plantaopro.usuarios u
where u.id=@usuarioId", new { usuarioId }, cancellationToken: ct));
        if (string.IsNullOrWhiteSpace(usuario.RegStatus)) return new(false, "USER_NOT_FOUND", "Usuário não encontrado.", "USUARIO");
        if (!string.Equals(usuario.RegStatus, "A", StringComparison.OrdinalIgnoreCase) || !string.Equals(usuario.Status, "ATIVO", StringComparison.OrdinalIgnoreCase)) return new(false, "USER_INACTIVE", "Usuário inativo ou bloqueado.", "USUARIO");
        var globalAdmin = await cn.QuerySingleAsync<bool>(new CommandDefinition(@"select exists(
select 1 from plantaopro.usuarios_perfis up join plantaopro.perfis p on p.id=up.perfil_id
where up.usuario_id=@usuarioId and up.reg_status='A' and p.reg_status='A'
and upper(coalesce(p.codigo,p.nome)) in ('ADMIN_GLOBAL','ADMINISTRADOR_GLOBAL','SUPER_ADMIN','SUPER_ADMINISTRADOR'))", new { usuarioId }, cancellationToken: ct));
        if (globalAdmin) return new(true, "GLOBAL_ADMIN", "Permitido pelo escopo global MNSOFT.", "PERFIL_GLOBAL");
        if (!tenantId.HasValue)
            return new(false, "TENANT_CONTEXT_REQUIRED", "Selecione o tenant correto para continuar.", "TENANT");

        var tenantAccess = await cn.QuerySingleOrDefaultAsync<(bool Authorized, string TenantStatus)>(new CommandDefinition(@"select
  (coalesce(u.tenant_id,u.cliente_id)=t.id or exists(
    select 1 from plantaopro.usuario_tenant_acessos uta
    where uta.usuario_id=u.id and uta.tenant_id=t.id
      and uta.reg_status='A' and uta.status='ATIVO'
      and (uta.acesso_inicio is null or uta.acesso_inicio<=now())
      and (uta.acesso_fim is null or uta.acesso_fim>now())
  )) Authorized,
  case when t.status='ATIVO' and (c.id is null or c.status='ATIVO') then 'ATIVO' else coalesce(c.status,t.status,'INATIVO') end TenantStatus
from plantaopro.usuarios u
join plantaopro.tenants t on t.id=@tenantId and t.reg_status='A'
left join plantaopro.clientes c on c.id=t.cliente_id and c.reg_status='A'
where u.id=@usuarioId", new { usuarioId, tenantId }, cancellationToken: ct));
        if (!tenantAccess.Authorized)
            return new(false, "CROSS_TENANT_DENIED", "Usuário não possui vínculo ativo com este tenant.", "TENANT");
        if (!string.Equals(tenantAccess.TenantStatus, "ATIVO", StringComparison.OrdinalIgnoreCase))
            return new(false, "TENANT_INACTIVE", "A instituição está bloqueada ou inativa.", "TENANT");

        var moduleCode = Normalize(modulo);
        if (!CoreModules.Contains(moduleCode))
        {
            var contracted = await cn.QuerySingleAsync<bool>(new CommandDefinition(@"select exists(
select 1 from plantaopro.tenant_modulos tm
left join plantaopro.modulos_sistema ms on ms.id=tm.modulo_id and ms.reg_status='A'
where tm.tenant_id=@tenantId and tm.reg_status='A' and tm.habilitado=true
and upper(coalesce(tm.status,'ATIVO'))='ATIVO'
and upper(coalesce(nullif(tm.codigo_modulo,''),ms.codigo))=@moduleCode)", new { tenantId, moduleCode }, cancellationToken: ct));
            if (!contracted) return new(false, "MODULE_NOT_CONTRACTED", "Este módulo não faz parte da contratação ativa do cliente.", "CONTRATO");
        }
        var permissoes = (await ObterPermissoesAsync(usuarioId, tenantId, ct)).ToArray();
        var chave = $"{moduleCode}.{Normalize(acao)}";
        if (permissoes.Any(p => p == "*" || string.Equals(Normalize(p), chave, StringComparison.OrdinalIgnoreCase) || string.Equals(Normalize(p), $"{moduleCode}.*", StringComparison.OrdinalIgnoreCase))) return new(true, "ALLOWED", "Permitido por perfil ou permissão especial ativa.", "PERMISSAO");
        return new(false, "PERMISSION_NOT_GRANTED", "Permissão efetiva não concedida para este tenant, plano ou assinatura.", "PERFIL");
    }

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant().Replace(':', '.');
}

public sealed class PasswordPolicyService : IPasswordPolicyService
{
    private readonly IConfiguration cfg;
    public PasswordPolicyService(IConfiguration cfg) { this.cfg = cfg; }
    public async Task<object> ObterAsync(Guid? tenantId, CancellationToken ct = default)
    {
        const string sql = @"select tamanho_minimo, exige_maiuscula, exige_minuscula, exige_numero, exige_especial, historico_quantidade, expiracao_dias, tentativas_permitidas, bloqueio_minutos, troca_obrigatoria, proibir_senhas_comuns from plantaopro.politicas_senha where reg_status='A' and (tenant_id=@tenantId or tenant_id is null) order by tenant_id nulls last limit 1";
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        return await cn.QuerySingleOrDefaultAsync<object>(new CommandDefinition(sql, new { tenantId }, cancellationToken: ct)) ?? new { tamanho_minimo = 10, exige_maiuscula = true };
    }
}

public sealed class SecurityAdministrationService
{
    private readonly IConfiguration cfg; private readonly ICurrentUserService currentUser; private readonly IEffectivePermissionService permissions; private readonly PlantaoPro.Api.Data.IAuditService audit;
    public SecurityAdministrationService(IConfiguration cfg, ICurrentUserService currentUser, IEffectivePermissionService permissions, PlantaoPro.Api.Data.IAuditService audit) { this.cfg = cfg; this.currentUser = currentUser; this.permissions = permissions; this.audit = audit; }
    private Guid? TenantScope(Guid? requested)
    {
        if (currentUser.IsGlobalAdmin()) return requested;
        return currentUser.TenantId ?? throw new UnauthorizedAccessException("Contexto de tenant obrigatório para administrar segurança.");
    }
    public async Task<bool> UsuarioPertenceAoEscopoAsync(Guid usuarioId, Guid? requestedTenantId, CancellationToken ct)
    {
        var tenantId = TenantScope(requestedTenantId);
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        if (!tenantId.HasValue)
            return currentUser.IsGlobalAdmin() && await cn.QuerySingleAsync<bool>(new CommandDefinition(
                "select exists(select 1 from plantaopro.usuarios where id=@usuarioId and reg_status='A')",
                new { usuarioId }, cancellationToken: ct));

        return await cn.QuerySingleAsync<bool>(new CommandDefinition(@"select exists(
select 1
from plantaopro.usuarios u
where u.id=@usuarioId and u.reg_status='A'
  and (
    coalesce(u.tenant_id,u.cliente_id)=@tenantId
    or exists(
      select 1 from plantaopro.usuario_tenant_acessos uta
      where uta.usuario_id=u.id and uta.tenant_id=@tenantId
        and uta.reg_status='A' and uta.status='ATIVO'
        and (uta.acesso_inicio is null or uta.acesso_inicio<=now())
        and (uta.acesso_fim is null or uta.acesso_fim>now())
    )
  )", new { usuarioId, tenantId }, cancellationToken: ct));
    }
    public async Task<EffectivePermissionDiagnostic?> TestarPermissaoNoEscopoAsync(Guid usuarioId, Guid? requestedTenantId, string modulo, string acao, CancellationToken ct)
    {
        var tenantId = TenantScope(requestedTenantId);
        if (!await UsuarioPertenceAoEscopoAsync(usuarioId, tenantId, ct)) return null;
        return await permissions.TestarAsync(usuarioId, tenantId, modulo, acao, ct);
    }
    public async Task<object> DashboardAsync(CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default")); var tenantId = TenantScope(null);
        var row = await cn.QuerySingleAsync(new CommandDefinition(@"select
(select count(*) from plantaopro.usuarios where reg_status='A' and (@tenantId is null or coalesce(tenant_id,cliente_id)=@tenantId)) usuarios_ativos,
(select count(*) from plantaopro.usuarios where reg_status<>'A' and (@tenantId is null or coalesce(tenant_id,cliente_id)=@tenantId)) usuarios_inativos,
(select count(*) from plantaopro.usuarios u where not exists (select 1 from plantaopro.usuarios_perfis up where up.usuario_id=u.id and up.reg_status='A') and (@tenantId is null or coalesce(u.tenant_id,u.cliente_id)=@tenantId)) usuarios_sem_perfil,
(select count(*) from plantaopro.login_tentativas lt left join plantaopro.usuarios u on u.id=lt.usuario_id where lt.sucesso=false and (@tenantId is null or coalesce(u.tenant_id,u.cliente_id)=@tenantId)) logins_falha,
(select count(*) from plantaopro.auth_sessoes s where s.revogada_em is null and s.reg_status='A' and (@tenantId is null or coalesce(s.tenant_id,s.cliente_id)=@tenantId)) sessoes_ativas,
(select count(*) from plantaopro.auth_sessoes s where s.revogada_em is not null and (@tenantId is null or coalesce(s.tenant_id,s.cliente_id)=@tenantId)) sessoes_revogadas", new { tenantId }, cancellationToken: ct));
        return row;
    }
    public async Task<IEnumerable<object>> UsuariosAsync(string? busca, int page, int pageSize, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default")); var tenantId = TenantScope(null);
        return await cn.QueryAsync<object>(new CommandDefinition(@"select u.id,u.nome,u.email,u.telefone,coalesce(u.tenant_id,u.cliente_id) tenant_id,u.cliente_id,coalesce(c.nome_fantasia,c.razao_social,'Contexto global MNSOFT') tenant_nome,coalesce(u.status,'ATIVO') status,u.reg_status,u.ultimo_login,u.bloqueado_ate,u.reg_date,
coalesce(string_agg(distinct p.nome,', ') filter(where p.id is not null),'Sem perfil') perfis
from plantaopro.usuarios u
left join plantaopro.clientes c on c.id=coalesce(u.tenant_id,u.cliente_id) and c.reg_status='A'
left join plantaopro.usuarios_perfis up on up.usuario_id=u.id and up.reg_status='A'
left join plantaopro.perfis p on p.id=up.perfil_id and p.reg_status='A'
where (@tenantId is null or coalesce(u.tenant_id,u.cliente_id)=@tenantId) and (@busca is null or u.nome ilike '%'||@busca||'%' or u.email ilike '%'||@busca||'%')
group by u.id,u.nome,u.email,u.telefone,u.tenant_id,u.cliente_id,c.nome_fantasia,c.razao_social,u.status,u.reg_status,u.ultimo_login,u.bloqueado_ate,u.reg_date order by u.nome limit @take offset @skip", new { tenantId, busca, take=Math.Clamp(pageSize,1,100), skip=(Math.Max(page,1)-1)*Math.Clamp(pageSize,1,100) }, cancellationToken: ct));
    }
    public async Task<object?> UsuarioAsync(Guid id, CancellationToken ct) { await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default")); return await cn.QuerySingleOrDefaultAsync<object>(new CommandDefinition("select id,nome,email,telefone,coalesce(tenant_id,cliente_id) tenant_id,cliente_id,coalesce(status,'ATIVO') status,reg_status,ultimo_login,bloqueado_ate,reg_date from plantaopro.usuarios where id=@id and (@tenantId is null or coalesce(tenant_id,cliente_id)=@tenantId)", new { id, tenantId=TenantScope(null) }, cancellationToken: ct)); }
    public async Task<IEnumerable<object>> PerfisAsync(CancellationToken ct) { await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default")); return await cn.QueryAsync<object>(new CommandDefinition("select id,nome,descricao,reg_status from plantaopro.perfis where (@tenantId is null or tenant_id is null or tenant_id=@tenantId) order by nome", new { tenantId = TenantScope(null) }, cancellationToken: ct)); }
    public async Task<object?> PerfilAsync(Guid id, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        return await cn.QuerySingleOrDefaultAsync<object>(new CommandDefinition(@"select id,tenant_id,codigo,nome,descricao,base_sistema,customizado,status,reg_status
from plantaopro.perfis where id=@id and reg_status='A' and (@tenantId is null or tenant_id is null or tenant_id=@tenantId)", new { id, tenantId=TenantScope(null) }, cancellationToken: ct));
    }
    public async Task<ApiResponse<Guid>> SalvarPerfilAsync(Guid? id, SecurityProfileRequest request, CancellationToken ct)
    {
        var nome=(request.Nome??string.Empty).Trim();
        if(nome.Length is < 3 or > 120) return ApiResponse<Guid>.Fail("Nome deve ter entre 3 e 120 caracteres.",400);
        var tenantId=TenantScope(null); if(!tenantId.HasValue) return ApiResponse<Guid>.Fail("Selecione um cliente antes de administrar perfis locais.",409);
        await using var cn=new NpgsqlConnection(cfg.GetConnectionString("Default")); await cn.OpenAsync(ct); await using var tx=await cn.BeginTransactionAsync(ct);
        var profileId=id??Guid.NewGuid();
        if(id.HasValue)
        {
            var editable=await cn.ExecuteScalarAsync<bool>(new CommandDefinition("select exists(select 1 from plantaopro.perfis where id=@profileId and tenant_id=@tenantId and reg_status='A' and coalesce(base_sistema,false)=false)",new{profileId,tenantId},tx,cancellationToken:ct));
            if(!editable) return ApiResponse<Guid>.Fail("Perfil não encontrado ou protegido pelo sistema.",404);
            await cn.ExecuteAsync(new CommandDefinition("update plantaopro.perfis set nome=@nome,descricao=@descricao,reg_update=now(),updated_by=@actor where id=@profileId",new{profileId,nome,descricao=request.Descricao?.Trim(),actor=currentUser.UserId},tx,cancellationToken:ct));
        }
        else
        {
            var codigo="CUSTOM_"+profileId.ToString("N").Substring(0,12).ToUpperInvariant();
            await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.perfis(id,tenant_id,cliente_id,codigo,nome,descricao,customizado,status,reg_status,created_by) values(@profileId,@tenantId,@tenantId,@codigo,@nome,@descricao,true,'ATIVO','A',@actor)",new{profileId,tenantId,codigo,nome,descricao=request.Descricao?.Trim(),actor=currentUser.UserId},tx,cancellationToken:ct));
        }
        await tx.CommitAsync(ct); return ApiResponse<Guid>.Ok(profileId,id.HasValue?"Perfil atualizado.":"Perfil criado.");
    }
    public async Task<ApiResponse<Guid>> CopiarPerfilAsync(Guid sourceId, CancellationToken ct)
    {
        var source=await PerfilAsync(sourceId,ct); if(source is null) return ApiResponse<Guid>.Fail("Perfil de origem não encontrado.",404);
        var tenantId=TenantScope(null); if(!tenantId.HasValue) return ApiResponse<Guid>.Fail("Selecione um cliente antes de copiar o perfil.",409);
        await using var cn=new NpgsqlConnection(cfg.GetConnectionString("Default")); await cn.OpenAsync(ct); await using var tx=await cn.BeginTransactionAsync(ct); var id=Guid.NewGuid();
        var inserted=await cn.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.perfis(id,tenant_id,cliente_id,codigo,nome,descricao,customizado,status,reg_status,created_by)
select @id,@tenantId,@tenantId,@codigo,left(nome||' (cópia)',120),descricao,true,'ATIVO','A',@actor from plantaopro.perfis where id=@sourceId and reg_status='A' and (@global or tenant_id is null or tenant_id=@tenantId)",new{id,tenantId,codigo="CUSTOM_"+id.ToString("N").Substring(0,12).ToUpperInvariant(),actor=currentUser.UserId,sourceId,global=currentUser.IsGlobalAdmin()},tx,cancellationToken:ct));
        if(inserted==0) return ApiResponse<Guid>.Fail("Perfil de origem não encontrado.",404);
        await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.perfil_permissoes(id,perfil_id,permissao_id,permitido,bloqueado_por_plano,reg_status,created_by) select gen_random_uuid(),@id,permissao_id,permitido,bloqueado_por_plano,'A',@actor from plantaopro.perfil_permissoes where perfil_id=@sourceId and reg_status='A'",new{id,sourceId,actor=currentUser.UserId},tx,cancellationToken:ct));
        await tx.CommitAsync(ct); return ApiResponse<Guid>.Ok(id,"Perfil copiado.");
    }
    public async Task<IEnumerable<object>> PermissoesPerfilAsync(Guid id,CancellationToken ct)
    {
        if(await PerfilAsync(id,ct) is null) return Array.Empty<object>(); await using var cn=new NpgsqlConnection(cfg.GetConnectionString("Default"));
        return await cn.QueryAsync<object>(new CommandDefinition(@"select p.id,p.codigo,p.nome,coalesce(pp.permitido,false) permitido,coalesce(pp.bloqueado_por_plano,false) bloqueado_por_plano
from plantaopro.permissoes p left join plantaopro.perfil_permissoes pp on pp.permissao_id=p.id and pp.perfil_id=@id and pp.reg_status='A' where p.reg_status='A' order by p.codigo",new{id},cancellationToken:ct));
    }
    public async Task<ApiResponse<Guid>> SalvarPermissoesPerfilAsync(Guid id,SecurityProfilePermissionsRequest request,CancellationToken ct)
    {
        var tenantId=TenantScope(null); await using var cn=new NpgsqlConnection(cfg.GetConnectionString("Default")); await cn.OpenAsync(ct); await using var tx=await cn.BeginTransactionAsync(ct);
        var editable=tenantId.HasValue&&await cn.ExecuteScalarAsync<bool>(new CommandDefinition("select exists(select 1 from plantaopro.perfis where id=@id and tenant_id=@tenantId and reg_status='A' and coalesce(base_sistema,false)=false)",new{id,tenantId},tx,cancellationToken:ct));
        if(!editable) return ApiResponse<Guid>.Fail("Perfil não encontrado ou protegido pelo sistema.",404);
        var entries=(request.Permissoes??Array.Empty<SecurityProfilePermissionRequest>()).Where(x=>x.PermissaoId!=Guid.Empty).GroupBy(x=>x.PermissaoId).Select(x=>x.Last()).ToArray();
        var valid=await cn.ExecuteScalarAsync<int>(new CommandDefinition("select count(*) from plantaopro.permissoes where id=any(@ids) and reg_status='A'",new{ids=entries.Select(x=>x.PermissaoId).ToArray()},tx,cancellationToken:ct));
        if(valid!=entries.Length) return ApiResponse<Guid>.Fail("Há permissões inválidas na solicitação.",400);
        await cn.ExecuteAsync(new CommandDefinition("update plantaopro.perfil_permissoes set reg_status='I',reg_update=now(),updated_by=@actor where perfil_id=@id and reg_status='A'",new{id,actor=currentUser.UserId},tx,cancellationToken:ct));
        foreach(var entry in entries) await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.perfil_permissoes(id,perfil_id,permissao_id,permitido,bloqueado_por_plano,reg_status,created_by) values(gen_random_uuid(),@id,@permissionId,@allowed,false,'A',@actor)",new{id,permissionId=entry.PermissaoId,allowed=entry.Permitido,actor=currentUser.UserId},tx,cancellationToken:ct));
        await cn.ExecuteAsync(new CommandDefinition("update plantaopro.auth_sessoes set revogada_em=now(),motivo_revogacao='PERMISSOES_PERFIL_ALTERADAS',reg_update=now() where usuario_id in(select usuario_id from plantaopro.usuarios_perfis where perfil_id=@id and reg_status='A') and revogada_em is null",new{id},tx,cancellationToken:ct));
        await tx.CommitAsync(ct); return ApiResponse<Guid>.Ok(id,"Permissões persistidas e sessões afetadas revogadas.");
    }
    public async Task<IEnumerable<SaasAssignableProfileDto>> PerfisAtribuiveisAsync(Guid? requestedTenantId, CancellationToken ct)
    {
        var tenantId = TenantScope(requestedTenantId);
        if (!currentUser.IsGlobalAdmin() && !tenantId.HasValue) return Array.Empty<SaasAssignableProfileDto>();
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        return await cn.QueryAsync<SaasAssignableProfileDto>(new CommandDefinition(@"select id as ""Id"", tenant_id as ""TenantId"", coalesce(codigo,'') as ""Codigo"", nome as ""Nome"", coalesce(descricao,'') as ""Descricao"", coalesce(base_sistema,false) as ""BaseSistema""
from plantaopro.perfis
where reg_status='A' and coalesce(status,'ATIVO')='ATIVO'
  and upper(coalesce(codigo,nome)) not in ('ADMIN_GLOBAL','ADMINISTRADOR_GLOBAL','SUPER_ADMIN','SUPER_ADMINISTRADOR')
  and (@tenantId is null or tenant_id is null or tenant_id=@tenantId)
order by base_sistema desc,nome", new { tenantId }, cancellationToken: ct));
    }
    public async Task<IEnumerable<Guid>> PerfisDoUsuarioAsync(Guid id, CancellationToken ct)
    {
        var tenantId = TenantScope(null);
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        return await cn.QueryAsync<Guid>(new CommandDefinition(@"select up.perfil_id
from plantaopro.usuarios_perfis up
join plantaopro.usuarios u on u.id=up.usuario_id
where up.usuario_id=@id and up.reg_status='A' and (@tenantId is null or coalesce(u.tenant_id,u.cliente_id)=@tenantId)
order by up.reg_date", new { id, tenantId }, cancellationToken: ct));
    }
    public async Task<ApiResponse<Guid>> SalvarUsuarioAsync(Guid? id, SaasUserUpsertRequest request, string? ip, string? userAgent, CancellationToken ct)
    {
        var tenantId = TenantScope(request.TenantId);
        var errors = ValidarUsuario(request, id.HasValue);
        if (!tenantId.HasValue) errors.Add("Selecione o cliente do usuário.");
        var profileIds = (request.PerfilIds ?? Array.Empty<Guid>()).Where(value => value != Guid.Empty).Distinct().ToArray();
        if (profileIds.Length == 0) errors.Add("Selecione ao menos um perfil.");
        if (errors.Count > 0) return ApiResponse<Guid>.Fail("Revise os dados do usuário.", 400, errors);

        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);
        var tenantExists = await cn.QuerySingleAsync<bool>(new CommandDefinition("select exists(select 1 from plantaopro.clientes where id=@tenantId and reg_status='A' and coalesce(status,'ATIVO') not in ('CANCELADO','INATIVO'))", new { tenantId }, tx, cancellationToken: ct));
        if (!tenantExists) return ApiResponse<Guid>.Fail("Cliente não encontrado ou indisponível.", 404);

        var validProfiles = (await cn.QueryAsync<Guid>(new CommandDefinition(@"select id from plantaopro.perfis
where id=any(@profileIds) and reg_status='A' and coalesce(status,'ATIVO')='ATIVO'
  and (tenant_id is null or tenant_id=@tenantId)
  and upper(coalesce(codigo,nome)) not in ('ADMIN_GLOBAL','ADMINISTRADOR_GLOBAL','SUPER_ADMIN','SUPER_ADMINISTRADOR')", new { profileIds, tenantId }, tx, cancellationToken: ct))).ToArray();
        if (validProfiles.Length != profileIds.Length) return ApiResponse<Guid>.Fail("Um ou mais perfis não pertencem ao cliente ou não podem ser atribuídos.", 400);

        var userId = id ?? Guid.NewGuid();
        var before = id.HasValue
            ? await cn.QuerySingleOrDefaultAsync<(Guid Id, Guid? TenantId, string Nome, string Email, string? Telefone, string Status)>(new CommandDefinition(@"select id,coalesce(tenant_id,cliente_id) tenant_id,nome,email,telefone,coalesce(status,'ATIVO') status
from plantaopro.usuarios where id=@userId and (@scopeTenantId is null or coalesce(tenant_id,cliente_id)=@scopeTenantId)", new { userId, scopeTenantId = TenantScope(null) }, tx, cancellationToken: ct))
            : default;
        if (id.HasValue && before.Id == Guid.Empty) return ApiResponse<Guid>.Fail("Usuário não encontrado no tenant permitido.", 404);
        if (id.HasValue && before.TenantId != tenantId) return ApiResponse<Guid>.Fail("Não é permitido mover usuário entre clientes.", 409);

        var email = request.Email.Trim().ToLowerInvariant();
        var duplicate = await cn.QuerySingleAsync<bool>(new CommandDefinition("select exists(select 1 from plantaopro.usuarios where lower(coalesce(email_normalizado,email))=lower(@email) and reg_status='A' and id<>@userId)", new { email, userId }, tx, cancellationToken: ct));
        if (duplicate) return ApiResponse<Guid>.Fail("Já existe um usuário ativo com este e-mail.", 409);

        if (!id.HasValue)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(request.SenhaTemporaria!);
            await cn.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.usuarios(id,tenant_id,cliente_id,nome,email,email_normalizado,senha_hash,telefone,status,reg_status,senha_alteracao_obrigatoria,reg_date,created_by)
values(@userId,@tenantId,@tenantId,@nome,@email,upper(@email),@hash,@telefone,'ATIVO','A',true,now(),@actorId)", new { userId, tenantId, nome = request.Nome.Trim(), email, hash, telefone = CleanPhone(request.Telefone), actorId = currentUser.UserId }, tx, cancellationToken: ct));
        }
        else
        {
            await cn.ExecuteAsync(new CommandDefinition(@"update plantaopro.usuarios set nome=@nome,email=@email,email_normalizado=upper(@email),telefone=@telefone,
senha_hash=case when @hash is null then senha_hash else @hash end,
senha_alteracao_obrigatoria=case when @hash is null then senha_alteracao_obrigatoria else true end,
reg_update=now(),updated_by=@actorId where id=@userId", new
            {
                userId,
                nome = request.Nome.Trim(),
                email,
                telefone = CleanPhone(request.Telefone),
                hash = string.IsNullOrWhiteSpace(request.SenhaTemporaria) ? null : BCrypt.Net.BCrypt.HashPassword(request.SenhaTemporaria),
                actorId = currentUser.UserId
            }, tx, cancellationToken: ct));
        }

        await cn.ExecuteAsync(new CommandDefinition("update plantaopro.usuarios_perfis set reg_status='I',reg_update=now(),updated_by=@actorId where usuario_id=@userId and reg_status='A' and not(perfil_id=any(@profileIds))", new { userId, profileIds, actorId = currentUser.UserId }, tx, cancellationToken: ct));
        foreach (var profileId in profileIds)
        {
            var restored = await cn.ExecuteAsync(new CommandDefinition("update plantaopro.usuarios_perfis set tenant_id=@tenantId,cliente_id=@tenantId,reg_status='A',reg_update=now(),updated_by=@actorId where usuario_id=@userId and perfil_id=@profileId and reg_status<>'A'", new { tenantId, userId, profileId, actorId = currentUser.UserId }, tx, cancellationToken: ct));
            if (restored == 0)
                await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.usuarios_perfis(id,tenant_id,cliente_id,usuario_id,perfil_id,reg_status,reg_date,created_by) select gen_random_uuid(),@tenantId,@tenantId,@userId,@profileId,'A',now(),@actorId where not exists(select 1 from plantaopro.usuarios_perfis where usuario_id=@userId and perfil_id=@profileId and reg_status='A')", new { tenantId, userId, profileId, actorId = currentUser.UserId }, tx, cancellationToken: ct));
        }
        await tx.CommitAsync(ct);
        if (id.HasValue) await RevogarSessoesAsync(userId, "ALTERACAO_CADASTRAL_OU_PERFIL", ct);
        await audit.RegistrarAsync(currentUser.UserId, tenantId, "USUARIO", userId, id.HasValue ? "EDITAR" : "CRIAR", new { antes = id.HasValue ? new { before.Nome, before.Email, before.Telefone, before.Status } : null, depois = new { request.Nome, email, telefone = CleanPhone(request.Telefone), perfis = profileIds, senhaAlterada = !string.IsNullOrWhiteSpace(request.SenhaTemporaria) } }, true, ip, string.Join(',', currentUser.Roles), ct);
        return ApiResponse<Guid>.Ok(userId, id.HasValue ? "Usuário e perfis atualizados." : "Usuário criado; a troca da senha temporária será exigida no primeiro acesso.");
    }
    public async Task<ApiResponse<Guid>> AlterarStatusUsuarioAsync(Guid id, string status, string? ip, CancellationToken ct)
    {
        var normalized = (status ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized is not ("ATIVO" or "BLOQUEADO" or "INATIVO")) return ApiResponse<Guid>.Fail("Status de usuário inválido.", 400);
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var tenantId = TenantScope(null);
        var before = await cn.QuerySingleOrDefaultAsync<(Guid Id, Guid? TenantId, string Status)>(new CommandDefinition("select id,coalesce(tenant_id,cliente_id) tenant_id,coalesce(status,'ATIVO') status from plantaopro.usuarios where id=@id and (@tenantId is null or coalesce(tenant_id,cliente_id)=@tenantId)", new { id, tenantId }, cancellationToken: ct));
        if (before.Id == Guid.Empty) return ApiResponse<Guid>.Fail("Usuário não encontrado no tenant permitido.", 404);
        var isGlobalTarget = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"select exists(select 1 from plantaopro.usuarios_perfis up join plantaopro.perfis p on p.id=up.perfil_id where up.usuario_id=@id and up.reg_status='A' and p.reg_status='A' and upper(coalesce(p.codigo,p.nome)) in ('ADMIN_GLOBAL','ADMINISTRADOR_GLOBAL','SUPER_ADMIN','SUPER_ADMINISTRADOR'))", new { id }, cancellationToken: ct));
        if (isGlobalTarget && !currentUser.IsGlobalAdmin()) return ApiResponse<Guid>.Fail("Perfis globais só podem ser administrados pela MNSOFT.", 403);
        if (isGlobalTarget && normalized != "ATIVO")
        {
            var activeGlobalAdmins = await cn.ExecuteScalarAsync<int>(new CommandDefinition(@"select count(distinct u.id) from plantaopro.usuarios u join plantaopro.usuarios_perfis up on up.usuario_id=u.id and up.reg_status='A' join plantaopro.perfis p on p.id=up.perfil_id and p.reg_status='A' where u.reg_status='A' and coalesce(u.status,'ATIVO')='ATIVO' and upper(coalesce(p.codigo,p.nome)) in ('ADMIN_GLOBAL','ADMINISTRADOR_GLOBAL','SUPER_ADMIN','SUPER_ADMINISTRADOR')", cancellationToken: ct));
            if (activeGlobalAdmins <= 1) return ApiResponse<Guid>.Fail("O Super Administrador MNSOFT principal não pode ser bloqueado ou inativado.", 409);
        }
        var regStatus = normalized == "INATIVO" ? "I" : "A";
        await cn.ExecuteAsync(new CommandDefinition("update plantaopro.usuarios set status=@normalized,reg_status=@regStatus,bloqueado_ate=case when @normalized='BLOQUEADO' then 'infinity'::timestamptz else null end,reg_update=now() where id=@id", new { id, normalized, regStatus }, cancellationToken: ct));
        await RevogarSessoesAsync(id, "ALTERACAO_STATUS_" + normalized, ct);
        await audit.RegistrarAsync(currentUser.UserId, before.TenantId, "USUARIO", id, "ALTERAR_STATUS", new { antes = before.Status, depois = normalized }, true, ip, string.Join(',', currentUser.Roles), ct);
        return ApiResponse<Guid>.Ok(id, normalized == "ATIVO" ? "Usuário desbloqueado." : "Acesso do usuário interrompido.");
    }
    public async Task RevogarSessoesAsync(Guid usuarioId, string motivo, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.ExecuteAsync(new CommandDefinition(@"update plantaopro.auth_sessoes s set revogada_em=now(),motivo_revogacao=@motivo,reg_update=now()
from plantaopro.usuarios u where s.usuario_id=u.id and s.usuario_id=@usuarioId and s.revogada_em is null
and (@tenantId is null or coalesce(u.tenant_id,u.cliente_id)=@tenantId)", new { usuarioId, motivo, tenantId = TenantScope(null) }, cancellationToken: ct));
    }
    public async Task<ApiResponse<Guid>> RevogarSessoesAdministrativamenteAsync(Guid usuarioId, string? ip, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var target = await cn.QuerySingleOrDefaultAsync<(Guid Id, Guid? TenantId)>(new CommandDefinition("select id,coalesce(tenant_id,cliente_id) tenant_id from plantaopro.usuarios where id=@usuarioId and (@tenantId is null or coalesce(tenant_id,cliente_id)=@tenantId)", new { usuarioId, tenantId = TenantScope(null) }, cancellationToken: ct));
        if (target.Id == Guid.Empty) return ApiResponse<Guid>.Fail("Usuário não encontrado no tenant permitido.", 404);
        await RevogarSessoesAsync(usuarioId, "REVOGACAO_ADMINISTRATIVA", ct);
        await audit.RegistrarAsync(currentUser.UserId, target.TenantId, "USUARIO", usuarioId, "REVOGAR_SESSOES", new { motivo = "REVOGACAO_ADMINISTRATIVA" }, true, ip, string.Join(',', currentUser.Roles), ct);
        return ApiResponse<Guid>.Ok(usuarioId, "Sessões revogadas.");
    }
    public async Task<ApiResponse<Guid>> ExigirTrocaSenhaAsync(Guid usuarioId,string? ip,CancellationToken ct)
    {
        if(!await UsuarioPertenceAoEscopoAsync(usuarioId,null,ct)) return ApiResponse<Guid>.Fail("Usuário não encontrado no tenant permitido.",404);
        await using var cn=new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.ExecuteAsync(new CommandDefinition("update plantaopro.usuarios set senha_alteracao_obrigatoria=true,reg_update=now(),updated_by=@actor where id=@usuarioId",new{usuarioId,actor=currentUser.UserId},cancellationToken:ct));
        await RevogarSessoesAsync(usuarioId,"TROCA_SENHA_OBRIGATORIA",ct);
        await audit.RegistrarAsync(currentUser.UserId,TenantScope(null),"USUARIO",usuarioId,"EXIGIR_TROCA_SENHA",new{obrigatoria=true},true,ip,string.Join(',',currentUser.Roles),ct);
        return ApiResponse<Guid>.Ok(usuarioId,"Troca de senha exigida; sessões anteriores foram revogadas.");
    }
    public async Task<SecurityPage<object>> SessoesAsync(int page,int pageSize,CancellationToken ct)
    {
        var tenantId=TenantScope(null); var take=Math.Clamp(pageSize,1,100); var currentPage=Math.Max(page,1); await using var cn=new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var total=await cn.ExecuteScalarAsync<long>(new CommandDefinition("select count(*) from plantaopro.auth_sessoes where (@tenantId is null or coalesce(tenant_id,cliente_id)=@tenantId)",new{tenantId},cancellationToken:ct));
        var items=(await cn.QueryAsync<object>(new CommandDefinition(@"select s.id,s.usuario_id,u.nome usuario,s.dispositivo_nome,s.ip_mascarado,s.iniciado_em,s.ultimo_uso_em,s.expira_em,s.revogada_em,s.motivo_revogacao,
case when s.id=@currentSession then true else false end sessao_atual from plantaopro.auth_sessoes s join plantaopro.usuarios u on u.id=s.usuario_id where (@tenantId is null or coalesce(s.tenant_id,s.cliente_id)=@tenantId) order by coalesce(s.ultimo_uso_em,s.iniciado_em) desc limit @take offset @skip",new{tenantId,currentSession=currentUser.SessionId,take,skip=(currentPage-1)*take},cancellationToken:ct))).AsList();
        return new(items,currentPage,take,total);
    }
    public async Task<ApiResponse<Guid>> RevogarSessaoAsync(Guid id,string? ip,CancellationToken ct)
    {
        var tenantId=TenantScope(null); await using var cn=new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var changed=await cn.ExecuteAsync(new CommandDefinition("update plantaopro.auth_sessoes set revogada_em=now(),motivo_revogacao='REVOGACAO_ADMINISTRATIVA',reg_update=now() where id=@id and revogada_em is null and (@tenantId is null or coalesce(tenant_id,cliente_id)=@tenantId)",new{id,tenantId},cancellationToken:ct));
        if(changed==0)return ApiResponse<Guid>.Fail("Sessão não encontrada ou já revogada.",404);
        await audit.RegistrarAsync(currentUser.UserId,tenantId,"SESSAO",id,"REVOGAR",new{motivo="REVOGACAO_ADMINISTRATIVA"},true,ip,string.Join(',',currentUser.Roles),ct); return ApiResponse<Guid>.Ok(id,"Sessão revogada.");
    }
    public async Task<SecurityPage<object>> TentativasLoginAsync(int page,int pageSize,CancellationToken ct)
    {
        var tenantId=TenantScope(null); return await PageAsync("plantaopro.login_tentativas lt left join plantaopro.usuarios u on u.id=lt.usuario_id","(@tenantId is null or coalesce(u.tenant_id,u.cliente_id)=@tenantId)","lt.id,lt.usuario_id,u.nome usuario,lt.email identificador,lt.ip,lt.sucesso,lt.motivo,lt.bloqueado_ate,lt.reg_date",page,pageSize,tenantId,ct);
    }
    public async Task<SecurityPage<object>> AuditoriaAsync(int page,int pageSize,CancellationToken ct)
    {
        var tenantId=TenantScope(null); return await PageAsync("plantaopro.auditoria_acoes_criticas a","(@tenantId is null or coalesce(a.tenant_id,a.cliente_id)=@tenantId)","a.id,a.usuario_id,coalesce(a.tenant_id,a.cliente_id) tenant_id,a.entidade,a.entidade_id,a.acao,a.sucesso,a.ip_origem,a.perfil,a.reg_date",page,pageSize,tenantId,ct);
    }
    private async Task<SecurityPage<object>> PageAsync(string source,string where,string columns,int page,int pageSize,Guid? tenantId,CancellationToken ct)
    {
        var take=Math.Clamp(pageSize,1,100);var currentPage=Math.Max(page,1);await using var cn=new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var total=await cn.ExecuteScalarAsync<long>(new CommandDefinition("select count(*) from "+source+" where "+where,new{tenantId},cancellationToken:ct));
        var items=(await cn.QueryAsync<object>(new CommandDefinition("select "+columns+" from "+source+" where "+where+" order by reg_date desc limit @take offset @skip",new{tenantId,take,skip=(currentPage-1)*take},cancellationToken:ct))).AsList();return new(items,currentPage,take,total);
    }
    public Task<IEnumerable<string>> PermissoesEfetivasAsync(Guid usuarioId, Guid? tenantId, CancellationToken ct) => permissions.ObterPermissoesAsync(usuarioId, TenantScope(tenantId), ct);

    private static List<string> ValidarUsuario(SaasUserUpsertRequest request, bool editing)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Nome) || request.Nome.Trim().Length is < 3 or > 160) errors.Add("Nome deve ter entre 3 e 160 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Email) || !new EmailAddressAttribute().IsValid(request.Email.Trim())) errors.Add("Informe um e-mail válido.");
        if (!editing && string.IsNullOrWhiteSpace(request.SenhaTemporaria)) errors.Add("Informe uma senha temporária.");
        if (!string.IsNullOrWhiteSpace(request.SenhaTemporaria) && !StrongPassword(request.SenhaTemporaria)) errors.Add("A senha temporária deve ter ao menos 10 caracteres, maiúscula, minúscula, número e símbolo.");
        if (CleanPhone(request.Telefone)?.Length > 20) errors.Add("Telefone deve ter no máximo 20 dígitos.");
        return errors;
    }
    private static bool StrongPassword(string value) => value.Length >= 10 && value.Any(char.IsUpper) && value.Any(char.IsLower) && value.Any(char.IsDigit) && value.Any(ch => !char.IsLetterOrDigit(ch));
    private static string? CleanPhone(string? value) => string.IsNullOrWhiteSpace(value) ? null : new string(value.Where(char.IsDigit).ToArray());
}
