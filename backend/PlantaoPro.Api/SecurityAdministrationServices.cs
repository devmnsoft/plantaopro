using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed record EffectivePermissionDiagnostic(bool Permitido, string Codigo, string Motivo, string Origem);

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
    public EffectivePermissionService(IConfiguration cfg) { this.cfg = cfg; }
    public async Task<IEnumerable<string>> ObterPermissoesAsync(Guid usuarioId, Guid? tenantId, CancellationToken ct = default)
    {
        const string sql = @"select distinct coalesce(p.codigo, ms.codigo || ':' || ac.codigo) codigo
from plantaopro.usuarios u
join plantaopro.usuarios_perfis up on up.usuario_id=u.id and up.reg_status='A'
join plantaopro.perfis pf on pf.id=up.perfil_id and pf.reg_status='A'
join plantaopro.perfil_permissoes pp on pp.perfil_id=pf.id and pp.reg_status='A' and coalesce(pp.permitido,true)=true
join plantaopro.permissoes p on p.id=pp.permissao_id and p.reg_status='A'
left join plantaopro.modulos_sistema ms on ms.id=p.modulo_id
left join plantaopro.acoes_sistema ac on ac.id=p.acao_id
where u.id=@usuarioId and u.reg_status='A' and (@tenantId is null or up.tenant_id is null or up.tenant_id=@tenantId)
union
select distinct p.codigo
from plantaopro.usuario_permissoes_especiais upe
join plantaopro.permissoes p on p.id=upe.permissao_id and p.reg_status='A'
where upe.usuario_id=@usuarioId and upe.reg_status='A' and upe.permitido=true and (@tenantId is null or upe.tenant_id is null or upe.tenant_id=@tenantId)";
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        return await cn.QueryAsync<string>(new CommandDefinition(sql, new { usuarioId, tenantId }, cancellationToken: ct));
    }
    public async Task<EffectivePermissionDiagnostic> TestarAsync(Guid usuarioId, Guid? tenantId, string modulo, string acao, CancellationToken ct = default)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var usuario = await cn.QuerySingleOrDefaultAsync<(string RegStatus, Guid? TenantId)>(new CommandDefinition("select reg_status RegStatus, tenant_id TenantId from plantaopro.usuarios where id=@usuarioId", new { usuarioId }, cancellationToken: ct));
        if (string.IsNullOrWhiteSpace(usuario.RegStatus)) return new(false, "USER_NOT_FOUND", "Usuário não encontrado.", "USUARIO");
        if (!string.Equals(usuario.RegStatus, "A", StringComparison.OrdinalIgnoreCase)) return new(false, "USER_INACTIVE", "Usuário inativo ou bloqueado.", "USUARIO");
        if (tenantId.HasValue && usuario.TenantId.HasValue && usuario.TenantId.Value != tenantId.Value) return new(false, "CROSS_TENANT_DENIED", "Usuário pertence a outro tenant.", "TENANT");
        var permissoes = (await ObterPermissoesAsync(usuarioId, tenantId, ct)).ToArray();
        var chave = (modulo + ":" + acao).ToUpperInvariant();
        if (permissoes.Any(p => p == "*" || string.Equals(p, chave, StringComparison.OrdinalIgnoreCase))) return new(true, "ALLOWED", "Permitido por perfil ou permissão especial ativa.", "PERMISSAO");
        return new(false, "PERMISSION_NOT_GRANTED", "Permissão efetiva não concedida para este tenant, plano ou assinatura.", "PERFIL");
    }
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
    private readonly IConfiguration cfg; private readonly ICurrentUserService currentUser; private readonly IEffectivePermissionService permissions;
    public SecurityAdministrationService(IConfiguration cfg, ICurrentUserService currentUser, IEffectivePermissionService permissions) { this.cfg = cfg; this.currentUser = currentUser; this.permissions = permissions; }
    private Guid? TenantScope(Guid? requested) => currentUser.IsGlobalAdmin() ? requested : currentUser.TenantId;
    public async Task<object> DashboardAsync(CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default")); var tenantId = TenantScope(null);
        var row = await cn.QuerySingleAsync(new CommandDefinition(@"select
(select count(*) from plantaopro.usuarios where reg_status='A' and (@tenantId is null or tenant_id=@tenantId)) usuarios_ativos,
(select count(*) from plantaopro.usuarios where reg_status<>'A' and (@tenantId is null or tenant_id=@tenantId)) usuarios_inativos,
(select count(*) from plantaopro.usuarios u where not exists (select 1 from plantaopro.usuarios_perfis up where up.usuario_id=u.id and up.reg_status='A') and (@tenantId is null or u.tenant_id=@tenantId)) usuarios_sem_perfil,
(select count(*) from plantaopro.login_tentativas where sucesso=false) logins_falha,
(select count(*) from plantaopro.auth_sessoes where revogada_em is null and reg_status='A') sessoes_ativas,
(select count(*) from plantaopro.auth_sessoes where revogada_em is not null) sessoes_revogadas", new { tenantId }, cancellationToken: ct));
        return row;
    }
    public async Task<IEnumerable<object>> UsuariosAsync(string? busca, int page, int pageSize, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default")); var tenantId = TenantScope(null);
        return await cn.QueryAsync<object>(new CommandDefinition(@"select id,nome,email,tenant_id,cliente_id,reg_status,reg_date from plantaopro.usuarios where (@tenantId is null or tenant_id=@tenantId) and (@busca is null or nome ilike '%'||@busca||'%' or email ilike '%'||@busca||'%') order by nome limit @take offset @skip", new { tenantId, busca, take=Math.Clamp(pageSize,1,100), skip=(Math.Max(page,1)-1)*Math.Clamp(pageSize,1,100) }, cancellationToken: ct));
    }
    public async Task<object?> UsuarioAsync(Guid id, CancellationToken ct) { await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default")); return await cn.QuerySingleOrDefaultAsync<object>(new CommandDefinition("select id,nome,email,tenant_id,cliente_id,reg_status,reg_date from plantaopro.usuarios where id=@id and (@tenantId is null or tenant_id=@tenantId)", new { id, tenantId=TenantScope(null) }, cancellationToken: ct)); }
    public async Task<IEnumerable<object>> PerfisAsync(CancellationToken ct) { await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default")); return await cn.QueryAsync<object>(new CommandDefinition("select id,nome,descricao,reg_status from plantaopro.perfis order by nome", cancellationToken: ct)); }
    public async Task RevogarSessoesAsync(Guid usuarioId, string motivo, CancellationToken ct) { await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default")); await cn.ExecuteAsync(new CommandDefinition("update plantaopro.auth_sessoes set revogada_em=now(), motivo_revogacao=@motivo, reg_update=now() where usuario_id=@usuarioId and revogada_em is null", new { usuarioId, motivo }, cancellationToken: ct)); }
    public Task<IEnumerable<string>> PermissoesEfetivasAsync(Guid usuarioId, Guid? tenantId, CancellationToken ct) => permissions.ObterPermissoesAsync(usuarioId, TenantScope(tenantId), ct);
}
