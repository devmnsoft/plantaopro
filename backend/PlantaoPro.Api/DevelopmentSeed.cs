using Dapper;
using Npgsql;

namespace PlantaoPro.Api.Data;

/// <summary>Provisionamento deliberado das duas identidades de demonstração.</summary>
/// <remarks>Este código só é chamado pelo comando --provision-demo/--reset-demo-passwords.</remarks>
public static class DevelopmentSeed
{
    internal const string SuperEmail = "superadmin@mnsoft.example";
    internal const string ManagerEmail = "gestor@santacasa-demo.example";
    internal const string DemoTenantCode = "santa-casa-demonstracao";
    private static readonly Guid DemoClientId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647501");
    private static readonly Guid DemoTenantId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
    private static readonly Guid DemoPlanId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647503");
    private static readonly Guid DemoUnitId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647504");
    private static readonly Guid SuperUserId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647510");
    private static readonly Guid ManagerUserId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647511");

    public static async Task RunAsync(IServiceProvider services, bool resetPasswords, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DevelopmentSeed");
        if (!env.IsDevelopment()) throw new InvalidOperationException("O provisionamento demonstrativo é permitido somente em Development.");
        if (!cfg.GetValue<bool>("DemoSeed:Enabled")) throw new InvalidOperationException("Habilite explicitamente DemoSeed:Enabled para executar o comando.");

        var expectedDatabase = cfg["DemoSeed:DevelopmentDatabase"]?.Trim();
        if (string.IsNullOrWhiteSpace(expectedDatabase))
            throw new InvalidOperationException("Defina DemoSeed:DevelopmentDatabase com o nome exato do banco local descartável.");

        var superPassword = cfg["DemoSeed:SuperAdminPassword"] ?? "MnSoft!Demo2026#Admin";
        var managerPassword = cfg["DemoSeed:ManagerPassword"] ?? "SantaCasa!Demo2026#Gestor";
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.OpenAsync(ct);
        var actualDatabase = await cn.ExecuteScalarAsync<string>(new CommandDefinition("select current_database()", cancellationToken: ct));
        if (!string.Equals(actualDatabase, expectedDatabase, StringComparison.Ordinal))
            throw new InvalidOperationException("O banco conectado não corresponde a DemoSeed:DevelopmentDatabase; nenhuma alteração foi realizada.");

        await using var tx = await cn.BeginTransactionAsync(ct);
        try
        {
            await cn.ExecuteAsync(new CommandDefinition("select pg_advisory_xact_lock(7065262026)", transaction: tx, cancellationToken: ct));

            var existingGlobal = await cn.QueryFirstOrDefaultAsync<(Guid Id, string Email)>(new CommandDefinition(@"
select u.id,u.email from plantaopro.usuarios u join plantaopro.usuarios_perfis up on up.usuario_id=u.id
join plantaopro.perfis p on p.id=up.perfil_id where p.codigo='ADMINISTRADOR_GLOBAL' and u.reg_status='A' and up.reg_status='A' limit 1", transaction: tx, cancellationToken: ct));
            if (existingGlobal.Id != Guid.Empty && !string.Equals(existingGlobal.Email, SuperEmail, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Já existe um administrador global ({existingGlobal.Email}). O registro foi preservado; nenhum segundo administrador foi criado.");

            await cn.ExecuteAsync(new CommandDefinition(@"
insert into plantaopro.planos(id,codigo,nome,status,dados) values(@plan,'DEMO_LOCAL','Plano demonstração local','ATIVO','{\"demonstracao\":true}'::jsonb) on conflict(id) do nothing;
insert into plantaopro.clientes(id,tenant_id,codigo,nome,status,dados) values(@client,@tenant,'SANTA_CASA_DEMONSTRACAO','Santa Casa Demonstração','ATIVO','{\"demonstracao\":true,\"ficticio\":true}'::jsonb) on conflict(id) do nothing;
insert into plantaopro.tenants(id,tenant_id,codigo,nome,status,dados) values(@tenant,@tenant,@code,'Santa Casa Demonstração','ATIVO','{\"demonstracao\":true}'::jsonb) on conflict(id) do nothing;
insert into plantaopro.assinaturas(id,tenant_id,codigo,nome,status,dados) values('d3f6584c-2c64-4e5a-9ea9-4e1428647505',@tenant,'CONTRATO_DEMO','Contrato demonstrativo','ATIVO','{\"externo\":false,\"demonstracao\":true}'::jsonb) on conflict(id) do nothing;
insert into plantaopro.unidades(id,tenant_id,codigo,nome,status,dados) values(@unit,@tenant,'UNIDADE_DEMO','Unidade Central — Demonstração','ATIVO','{\"demonstracao\":true}'::jsonb) on conflict(id) do nothing;
insert into plantaopro.tenant_modulos(id,tenant_id,codigo,nome,status,dados)
select ('d3f6584c-2c64-4e5a-9ea9-' || suffix)::uuid,@tenant,code,name,'ATIVO','{\"demonstracao\":true,\"contratado\":true}'::jsonb
from (values ('4e1428647521','ESCALAS','Escalas'),('4e1428647522','EXECUCAO','Execução'),('4e1428647523','CONFERENCIA','Conferência')) m(suffix,code,name) on conflict(id) do nothing;",
                new { plan = DemoPlanId, client = DemoClientId, tenant = DemoTenantId, unit = DemoUnitId, code = DemoTenantCode }, tx, cancellationToken: ct));

            var globalProfile = await EnsureProfile(cn, tx, "ADMINISTRADOR_GLOBAL", "Administrador global", null, null, ct);
            var managerProfile = await EnsureProfile(cn, tx, "ADMINISTRADOR_CLIENTE", "Administrador do cliente", DemoTenantId, DemoClientId, ct);
            await EnsureUser(cn, tx, SuperUserId, "Administrador MNSOFT — Demonstração", SuperEmail, superPassword, null, null, globalProfile, resetPasswords, ct);
            await EnsureUser(cn, tx, ManagerUserId, "Gestor Santa Casa — Demonstração", ManagerEmail, managerPassword, DemoTenantId, DemoClientId, managerProfile, resetPasswords, ct);
            await tx.CommitAsync(ct);
            logger.LogInformation("Contas demonstrativas provisionadas em {Database}; senhas existentes foram {PasswordAction}.", actualDatabase, resetPasswords ? "redefinidas explicitamente" : "preservadas");
        }
        catch { await tx.RollbackAsync(ct); throw; }
    }

    private static async Task<Guid> EnsureProfile(NpgsqlConnection cn, NpgsqlTransaction tx, string code, string name, Guid? tenant, Guid? client, CancellationToken ct)
    {
        var id = await cn.ExecuteScalarAsync<Guid?>(new CommandDefinition("select id from plantaopro.perfis where codigo=@code and tenant_id is not distinct from @tenant and reg_status='A' limit 1", new { code, tenant }, tx, cancellationToken: ct));
        if (id.HasValue) return id.Value;
        return await cn.ExecuteScalarAsync<Guid>(new CommandDefinition(@"insert into plantaopro.perfis(tenant_id,cliente_id,codigo,nome,descricao,base_sistema,customizado,status,reg_status) values(@tenant,@client,@code,@name,'Perfil canônico para demonstração local',true,false,'ATIVO','A') returning id", new { tenant, client, code, name }, tx, cancellationToken: ct));
    }

    private static async Task EnsureUser(NpgsqlConnection cn, NpgsqlTransaction tx, Guid stableId, string name, string email, string password, Guid? tenant, Guid? client, Guid profile, bool reset, CancellationToken ct)
    {
        var id = await cn.ExecuteScalarAsync<Guid?>(new CommandDefinition("select id from plantaopro.usuarios where lower(email_normalizado)=lower(@email) and reg_status='A' limit 1", new { email }, tx, cancellationToken: ct));
        if (id.HasValue && id.Value != stableId)
            throw new InvalidOperationException($"O login demonstrativo {email} já pertence a outro registro. Nenhuma identidade foi alterada.");
        if (!id.HasValue)
        {
            id = stableId;
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            await cn.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.usuarios(id,tenant_id,cliente_id,nome,email,email_normalizado,senha_hash,status,reg_status,senha_alteracao_obrigatoria) values(@id,@tenant,@client,@name,@email,lower(@email),@hash,'ATIVO','A',false)", new { id, tenant, client, name, email, hash }, tx, cancellationToken: ct));
        }
        else if (reset)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            await cn.ExecuteAsync(new CommandDefinition("update plantaopro.usuarios set senha_hash=@hash,reg_update=now() where id=@id", new { hash, id }, tx, cancellationToken: ct));
            await cn.ExecuteAsync(new CommandDefinition("update plantaopro.auth_sessoes set revogada_em=now(),motivo_revogacao='DEMO_PASSWORD_RESET',reg_update=now() where usuario_id=@id and revogada_em is null", new { id }, tx, cancellationToken: ct));
        }
        await cn.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.usuarios_perfis(tenant_id,cliente_id,usuario_id,perfil_id,reg_status) select @tenant,@client,@id,@profile,'A' where not exists(select 1 from plantaopro.usuarios_perfis where usuario_id=@id and perfil_id=@profile and reg_status='A')", new { tenant, client, id, profile }, tx, cancellationToken: ct));
    }
}
