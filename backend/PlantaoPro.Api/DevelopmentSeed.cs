using Dapper;
using Npgsql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace PlantaoPro.Api.Data;

public static class DevelopmentSeed
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DevelopmentSeed");
        if (!env.IsDevelopment()) return;

        var demoPassword = cfg["Demo:Password"] ?? cfg["PLANTAOPRO_DEMO_PASSWORD"] ?? Environment.GetEnvironmentVariable("PLANTAOPRO_DEMO_PASSWORD");
        if (string.IsNullOrWhiteSpace(demoPassword) || demoPassword.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("DevelopmentSeed habilitado, mas Demo:Password/PLANTAOPRO_DEMO_PASSWORD não foi configurada fora do Git.");

        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.OpenAsync();

        var planoId = await UpsertId(cn, "plantaopro.planos", "slug", "demo", "insert into plantaopro.planos(id,nome,slug,descricao,valor_mensal,status,reg_status,reg_date) values(@id,'Demo Local','demo','Plano demo local',0,'ATIVO','A',now())");
        var clienteId = await UpsertId(cn, "plantaopro.clientes", "cnpj", "00000000000000", "insert into plantaopro.clientes(id,razao_social,nome_fantasia,cnpj,email,plano_id,status,reg_status,reg_date) values(@id,'PlantãoPro Demo','PlantãoPro Demo','00000000000000','demo@plantaopro.local',@planoId,'ATIVO','A',now())", new { planoId });
        var tenantId = await UpsertId(cn, "plantaopro.tenants", "slug", "demo", "insert into plantaopro.tenants(id,cliente_id,plano_id,nome,slug,status,reg_status,reg_date) values(@id,@clienteId,@planoId,'Tenant Demo','demo','ATIVO','A',now())", new { clienteId, planoId });
        await cn.ExecuteAsync("insert into plantaopro.assinaturas(id,tenant_id,cliente_id,plano_id,status,valor_contratado,valor_mensal,reg_status,reg_date) select gen_random_uuid(),@tenantId,@clienteId,@planoId,'ATIVA',0,0,'A',now() where not exists(select 1 from plantaopro.assinaturas where tenant_id=@tenantId and reg_status='A')", new { tenantId, clienteId, planoId });

        var roles = new[] { "ADMINISTRADOR_GLOBAL", "ADMINISTRADOR_CLIENTE", "ADMINISTRADOR", "COORDENACAO", "OPERADOR", "FINANCEIRO", "MEDICO", "HOSPITAL", "RECEPCAO", "TRIAGEM" };
        foreach (var role in roles)
            await cn.ExecuteAsync(@"insert into plantaopro.perfis(id,tenant_id,cliente_id,codigo,nome,descricao,base_sistema,customizado,status,reg_status,reg_date)
select gen_random_uuid(),case when @role='ADMINISTRADOR_GLOBAL' then null else @tenantId end,case when @role='ADMINISTRADOR_GLOBAL' then null else @clienteId end,@role,@role,'Perfil demo '||@role,true,false,'ATIVO','A',now()
where not exists(select 1 from plantaopro.perfis where codigo=@role and coalesce(tenant_id,'00000000-0000-0000-0000-000000000000'::uuid)=coalesce(case when @role='ADMINISTRADOR_GLOBAL' then null else @tenantId end,'00000000-0000-0000-0000-000000000000'::uuid) and reg_status='A')", new { role, tenantId, clienteId });

        var users = new[]
        {
            ("admin.global@plantaopro.local", "Administrador Global", "ADMINISTRADOR_GLOBAL", true),
            ("admin@plantaopro.local", "Administrador Cliente", "ADMINISTRADOR_CLIENTE", false),
            ("coordenacao@plantaopro.local", "Coordenação", "COORDENACAO", false),
            ("operador@plantaopro.local", "Operador", "OPERADOR", false),
            ("financeiro@plantaopro.local", "Financeiro", "FINANCEIRO", false),
            ("medico@plantaopro.local", "Médico", "MEDICO", false),
            ("hospital@plantaopro.local", "Hospital", "HOSPITAL", false),
            ("recepcao@plantaopro.local", "Recepção", "RECEPCAO", false),
            ("triagem@plantaopro.local", "Triagem", "TRIAGEM", false)
        };
        foreach (var u in users)
        {
            var userId = await cn.ExecuteScalarAsync<Guid?>("select id from plantaopro.usuarios where email_normalizado=lower(@email) or lower(email)=lower(@email) limit 1", new { email = u.Item1 }) ?? Guid.NewGuid();
            await cn.ExecuteAsync(@"insert into plantaopro.usuarios(id,tenant_id,cliente_id,nome,email,email_normalizado,senha_hash,status,reg_status,senha_alteracao_obrigatoria,reg_date)
values(@userId,@tenantIdValue,@clienteIdValue,@nome,@email,lower(@email),@hash,'ATIVO','A',false,now())
on conflict (id) do update set nome=@nome,email_normalizado=lower(@email),senha_hash=@hash,status='ATIVO',reg_status='A',tenant_id=@tenantIdValue,cliente_id=@clienteIdValue,reg_update=now()", new { userId, tenantIdValue = u.Item4 ? (Guid?)null : tenantId, clienteIdValue = u.Item4 ? (Guid?)null : clienteId, nome = u.Item2, email = u.Item1, hash = BCrypt.Net.BCrypt.HashPassword(demoPassword) });
            var perfilId = await cn.ExecuteScalarAsync<Guid>("select id from plantaopro.perfis where codigo=@role and reg_status='A' order by tenant_id nulls first limit 1", new { role = u.Item3 });
            await cn.ExecuteAsync("insert into plantaopro.usuarios_perfis(id,tenant_id,cliente_id,usuario_id,perfil_id,reg_status,reg_date) select gen_random_uuid(),@tenantIdValue,@clienteIdValue,@userId,@perfilId,'A',now() where not exists(select 1 from plantaopro.usuarios_perfis where usuario_id=@userId and perfil_id=@perfilId and reg_status='A')", new { tenantIdValue = u.Item4 ? (Guid?)null : tenantId, clienteIdValue = u.Item4 ? (Guid?)null : clienteId, userId, perfilId });
        }
        logger.LogInformation("DevelopmentSeed executado com contas demo sem expor senha.");
    }

    private static async Task<Guid> UpsertId(NpgsqlConnection cn, string table, string key, string value, string insertSql, object? extra = null)
    {
        var id = await cn.ExecuteScalarAsync<Guid?>($"select id from {table} where {key}=@value limit 1", new { value });
        if (id.HasValue) return id.Value;
        var newId = Guid.NewGuid();
        var args = Merge(new { id = newId, value }, extra);
        await cn.ExecuteAsync(insertSql, args);
        return newId;
    }
    private static object Merge(object first, object? second)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var p in first.GetType().GetProperties()) dict[p.Name] = p.GetValue(first);
        if (second != null) foreach (var p in second.GetType().GetProperties()) dict[p.Name] = p.GetValue(second);
        return dict;
    }
}
