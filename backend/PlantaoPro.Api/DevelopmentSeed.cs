using Dapper;
using Npgsql;
using System.Text.Json;

namespace PlantaoPro.Api.Data;

/// <summary>Provisionamento deliberado das identidades de demonstração local.</summary>
/// <remarks>Chamado por --provision-demo/--reset-demo-passwords ou, em Development, por DemoSeed:AutoProvisionIfEmpty.</remarks>
public static class DevelopmentSeed
{
    internal const string SuperEmail = "superadmin@mnsoft.example";
    internal const string ManagerEmail = "gestor@santacasa-demo.example";
    internal const string PhysicianEmail = "medico@santacasa-demo.example";
    internal const string DemoTenantCode = "santa-casa-demonstracao";
    private static readonly Guid DemoClientId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647501");
    private static readonly Guid DemoTenantId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
    private static readonly Guid DemoPlanId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647503");
    private static readonly Guid DemoUnitId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647504");
    private static readonly Guid SuperUserId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647510");
    private static readonly Guid ManagerUserId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647511");
    private static readonly Guid PhysicianUserId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647512");
    private static readonly Guid DemoSubscriptionId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647505");

    private const string DemoStructureSql = @"
insert into plantaopro.planos(id,codigo,nome,status,dados)
values(@planId,@planCode,@planName,'ATIVO',cast(@planData as jsonb))
on conflict(id) do nothing;

insert into plantaopro.clientes(id,tenant_id,codigo,nome,status,dados)
values(@clientId,@tenantId,@clientCode,@clientName,'ATIVO',cast(@clientData as jsonb))
on conflict(id) do nothing;

insert into plantaopro.tenants(id,tenant_id,codigo,nome,status,dados)
values(@tenantId,@tenantId,@tenantCode,@clientName,'ATIVO',cast(@tenantData as jsonb))
on conflict(id) do nothing;

insert into plantaopro.assinaturas(id,tenant_id,codigo,nome,status,dados)
values(@subscriptionId,@tenantId,@subscriptionCode,@subscriptionName,'ATIVO',cast(@subscriptionData as jsonb))
on conflict(id) do nothing;

insert into plantaopro.unidades(id,tenant_id,codigo,nome,status,dados)
values(@unitId,@tenantId,@unitCode,@unitName,'ATIVO',cast(@unitData as jsonb))
on conflict(id) do nothing;

insert into plantaopro.hospitais(id,tenant_id,cliente_id,nome,nome_fantasia,status,dados)
values(@unitId,@tenantId,@clientId,@unitName,@unitName,'ATIVO',cast(@unitData as jsonb))
on conflict(id) do nothing;

insert into plantaopro.tenant_modulos(id,tenant_id,codigo,codigo_modulo,nome,habilitado,status,dados)
select module.id,@tenantId,module.code,module.code,module.name,true,'ATIVO',cast(@moduleData as jsonb)
from (values
    (cast('d3f6584c-2c64-4e5a-9ea9-4e1428647521' as uuid),'ESCALAS','Escalas'),
    (cast('d3f6584c-2c64-4e5a-9ea9-4e1428647522' as uuid),'EXECUCAO','Execução'),
    (cast('d3f6584c-2c64-4e5a-9ea9-4e1428647523' as uuid),'CONFERENCIA','Conferência')
) module(id,code,name)
on conflict(id) do nothing;";

    private const string ValidateDemoStructureSql = @"
select count(*) from (
    select id from plantaopro.planos where id=@planId and codigo=@planCode
    union all select id from plantaopro.clientes where id=@clientId and tenant_id=@tenantId and codigo=@clientCode
    union all select id from plantaopro.tenants where id=@tenantId and tenant_id=@tenantId and codigo=@tenantCode
    union all select id from plantaopro.assinaturas where id=@subscriptionId and tenant_id=@tenantId and codigo=@subscriptionCode
    union all select id from plantaopro.unidades where id=@unitId and tenant_id=@tenantId and codigo=@unitCode
    union all select id from plantaopro.tenant_modulos where id=cast('d3f6584c-2c64-4e5a-9ea9-4e1428647521' as uuid) and tenant_id=@tenantId and codigo='ESCALAS'
    union all select id from plantaopro.tenant_modulos where id=cast('d3f6584c-2c64-4e5a-9ea9-4e1428647522' as uuid) and tenant_id=@tenantId and codigo='EXECUCAO'
    union all select id from plantaopro.tenant_modulos where id=cast('d3f6584c-2c64-4e5a-9ea9-4e1428647523' as uuid) and tenant_id=@tenantId and codigo='CONFERENCIA'
) expected;";

    public static async Task RunIfEmptyAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DevelopmentSeed");
        if (!env.IsDevelopment()) return;
        if (!cfg.GetValue<bool>("DemoSeed:Enabled") || !cfg.GetValue<bool>("DemoSeed:AutoProvisionIfEmpty")) return;

        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.OpenAsync(ct);
        var hasIdentity = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(
            "select to_regclass('plantaopro.usuarios') is not null", cancellationToken: ct));
        if (!hasIdentity)
        {
            logger.LogWarning("Schema de identidade ausente; o auto-provisionamento de demonstração foi ignorado.");
            return;
        }

        var existing = await cn.ExecuteScalarAsync<int>(new CommandDefinition(@"
select count(*) from plantaopro.usuarios
where reg_status='A' and lower(coalesce(email_normalizado,email)) in (lower(@super),lower(@manager),lower(@physician))",
            new { super = SuperEmail, manager = ManagerEmail, physician = PhysicianEmail }, cancellationToken: ct));

        var reset = cfg.GetValue("DemoSeed:ResetPasswordsOnStartup", true);
        if (existing >= 3 && !reset)
        {
            logger.LogInformation("Contas de demonstração já existem; nenhuma alteração automática.");
            return;
        }

        await RunAsync(services, reset || existing < 3, ct);
    }

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

        var superPassword = cfg["DemoSeed:SuperAdminPassword"];
        var managerPassword = cfg["DemoSeed:ManagerPassword"];
        var physicianPassword = cfg["DemoSeed:PhysicianPassword"];
        if (string.IsNullOrWhiteSpace(superPassword) || string.IsNullOrWhiteSpace(managerPassword) || string.IsNullOrWhiteSpace(physicianPassword))
            throw new InvalidOperationException("Forneça DemoSeed:SuperAdminPassword, DemoSeed:ManagerPassword e DemoSeed:PhysicianPassword por configuração local.");

        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.OpenAsync(ct);
        var actualDatabase = await cn.ExecuteScalarAsync<string>(new CommandDefinition("select current_database()", cancellationToken: ct));
        var allowLegacyPostgres = cfg.GetValue("Database:AllowLegacyPostgresDatabase", false);
        var databaseMatches = string.Equals(actualDatabase, expectedDatabase, StringComparison.Ordinal)
            || (allowLegacyPostgres && string.Equals(actualDatabase, "postgres", StringComparison.OrdinalIgnoreCase));
        if (!databaseMatches)
            throw new InvalidOperationException("O banco conectado não corresponde a DemoSeed:DevelopmentDatabase; nenhuma alteração foi realizada.");

        await using var tx = await cn.BeginTransactionAsync(ct);
        try
        {
            await cn.ExecuteAsync(new CommandDefinition("select pg_advisory_xact_lock(7065262026)", transaction: tx, cancellationToken: ct));
            await EnsureCompatibleSchema(cn, tx, ct);

            var existingGlobal = await cn.QueryFirstOrDefaultAsync<(Guid Id, string Email)>(new CommandDefinition(@"
select u.id,u.email from plantaopro.usuarios u join plantaopro.usuarios_perfis up on up.usuario_id=u.id
join plantaopro.perfis p on p.id=up.perfil_id where p.codigo='ADMINISTRADOR_GLOBAL' and u.reg_status='A' and up.reg_status='A' limit 1", transaction: tx, cancellationToken: ct));
            var skipSuper = existingGlobal.Id != Guid.Empty && !string.Equals(existingGlobal.Email, SuperEmail, StringComparison.OrdinalIgnoreCase);
            if (skipSuper)
                logger.LogWarning("Já existe um administrador global ({Email}). O superadmin de demonstração não será criado; gestor e médico demo seguem.", existingGlobal.Email);

            var demoParameters = new
            {
                planId = DemoPlanId,
                planCode = "DEMO_LOCAL",
                planName = "Plano demonstração local",
                planData = JsonSerializer.Serialize(new { demonstracao = true }),
                clientId = DemoClientId,
                clientCode = "SANTA_CASA_DEMONSTRACAO",
                clientName = "Cliente Modelo — Demonstração",
                clientData = JsonSerializer.Serialize(new { demonstracao = true, ficticio = true }),
                tenantId = DemoTenantId,
                tenantCode = DemoTenantCode,
                tenantData = JsonSerializer.Serialize(new { demonstracao = true }),
                subscriptionId = DemoSubscriptionId,
                subscriptionCode = "CONTRATO_DEMO",
                subscriptionName = "Contrato demonstrativo",
                subscriptionData = JsonSerializer.Serialize(new { externo = false, demonstracao = true }),
                unitId = DemoUnitId,
                unitCode = "UNIDADE_DEMO",
                unitName = "Unidade Central — Demonstração",
                unitData = JsonSerializer.Serialize(new { demonstracao = true }),
                moduleData = JsonSerializer.Serialize(new { demonstracao = true, contratado = true })
            };
            await cn.ExecuteAsync(new CommandDefinition(DemoStructureSql, demoParameters, tx, cancellationToken: ct));
            await cn.ExecuteAsync(new CommandDefinition(@"
update plantaopro.clientes set
    razao_social = coalesce(nullif(razao_social,''), nome, 'Cliente Modelo — Demonstração'),
    nome_fantasia = coalesce(nullif(nome_fantasia,''), nome, razao_social, 'Cliente Modelo — Demonstração'),
    status = coalesce(nullif(status,''), 'ATIVO'),
    reg_status = coalesce(nullif(reg_status,''), 'A')
where id=@clientId", new { clientId = DemoClientId }, tx, cancellationToken: ct));

            var validatedRecords = await cn.ExecuteScalarAsync<int>(new CommandDefinition(ValidateDemoStructureSql, demoParameters, tx, cancellationToken: ct));
            if (validatedRecords != 8)
                throw new InvalidOperationException("Há colisão entre os IDs demonstrativos e registros existentes. A transação foi cancelada sem alterar o banco.");

            var globalProfile = await EnsureProfile(cn, tx, "ADMINISTRADOR_GLOBAL", "Administrador global", null, null, ct);
            var managerProfile = await EnsureProfile(cn, tx, "ADMINISTRADOR_CLIENTE", "Administrador do cliente", DemoTenantId, DemoClientId, ct);
            var physicianProfile = await EnsureProfile(cn, tx, "MEDICO", "Médico", DemoTenantId, DemoClientId, ct);
            if (!skipSuper)
                await EnsureUser(cn, tx, SuperUserId, "Administrador MNSOFT — Demonstração", SuperEmail, superPassword, null, null, globalProfile, resetPasswords, ct);
            await EnsureUser(cn, tx, ManagerUserId, "Gestor Operacional — Demonstração", ManagerEmail, managerPassword, DemoTenantId, DemoClientId, managerProfile, resetPasswords, ct);
            await EnsureUser(cn, tx, PhysicianUserId, "Dra. Ana Souza — Demonstração", PhysicianEmail, physicianPassword, DemoTenantId, DemoClientId, physicianProfile, resetPasswords, ct);
            await EnsurePhysicianRow(cn, tx, ct);
            await tx.CommitAsync(ct);
            logger.LogInformation("Contas demonstrativas provisionadas em {Database}; senhas existentes foram {PasswordAction}.", actualDatabase, resetPasswords ? "redefinidas explicitamente" : "preservadas");
        }
        catch { await tx.RollbackAsync(ct); throw; }
    }

    private static async Task EnsureCompatibleSchema(NpgsqlConnection cn, NpgsqlTransaction tx, CancellationToken ct)
    {
        await cn.ExecuteAsync(new CommandDefinition(@"
do $compat$
begin
    if to_regclass('plantaopro.clientes') is not null then
        alter table plantaopro.clientes add column if not exists tenant_id uuid;
        alter table plantaopro.clientes add column if not exists codigo text;
        alter table plantaopro.clientes add column if not exists nome text;
        alter table plantaopro.clientes add column if not exists razao_social varchar(200);
        alter table plantaopro.clientes add column if not exists nome_fantasia varchar(200);
        alter table plantaopro.clientes add column if not exists cnpj varchar(30);
        alter table plantaopro.clientes add column if not exists status text default 'ATIVO';
        alter table plantaopro.clientes add column if not exists dados jsonb default '{}'::jsonb;
        alter table plantaopro.clientes add column if not exists reg_status char(1) default 'A';
    end if;
    if to_regclass('plantaopro.hospitais') is not null then
        alter table plantaopro.hospitais add column if not exists tenant_id uuid;
        alter table plantaopro.hospitais add column if not exists cliente_id uuid;
        alter table plantaopro.hospitais add column if not exists dados jsonb default '{}'::jsonb;
    end if;
    if to_regclass('plantaopro.medicos') is null then
        create table plantaopro.medicos (
            id uuid primary key default gen_random_uuid(),
            usuario_id uuid null,
            cpf text null,
            status text not null default 'ATIVO',
            reg_status char(1) not null default 'A',
            reg_date timestamptz not null default now()
        );
    else
        alter table plantaopro.medicos add column if not exists usuario_id uuid;
        alter table plantaopro.medicos add column if not exists cpf text;
        alter table plantaopro.medicos add column if not exists reg_status char(1) default 'A';
    end if;
end
$compat$;", transaction: tx, cancellationToken: ct));
    }

    private static async Task EnsurePhysicianRow(NpgsqlConnection cn, NpgsqlTransaction tx, CancellationToken ct)
    {
        await cn.ExecuteAsync(new CommandDefinition(@"
insert into plantaopro.medicos(id, usuario_id, cpf, status, reg_status)
select 'd3f6584c-2c64-4e5a-9ea9-4e1428647530', @userId, '52998224725', 'ATIVO', 'A'
where not exists (select 1 from plantaopro.medicos where usuario_id=@userId and coalesce(reg_status,'A')='A')",
            new { userId = PhysicianUserId }, tx, cancellationToken: ct));
    }

    private static async Task<Guid> EnsureProfile(NpgsqlConnection cn, NpgsqlTransaction tx, string code, string name, Guid? tenant, Guid? client, CancellationToken ct)
    {
        var existing = await cn.QueryFirstOrDefaultAsync<(Guid Id, Guid? Client)>(new CommandDefinition(
            commandText: "select id,cliente_id from plantaopro.perfis where codigo=@code and tenant_id is not distinct from @tenant and reg_status='A' limit 1",
            parameters: new { code, tenant },
            transaction: tx,
            cancellationToken: ct));
        if (existing.Id != Guid.Empty)
        {
            if (existing.Client != client)
                throw new InvalidOperationException($"O perfil {code} já existe em outro contexto de cliente. Nenhum vínculo foi alterado.");
            return existing.Id;
        }
        return await cn.ExecuteScalarAsync<Guid>(new CommandDefinition(@"insert into plantaopro.perfis(tenant_id,cliente_id,codigo,nome,descricao,base_sistema,customizado,status,reg_status) values(@tenant,@client,@code,@name,'Perfil canônico para demonstração local',true,false,'ATIVO','A') returning id", new { tenant, client, code, name }, tx, cancellationToken: ct));
    }

    private static async Task EnsureUser(NpgsqlConnection cn, NpgsqlTransaction tx, Guid stableId, string name, string email, string password, Guid? tenant, Guid? client, Guid profile, bool reset, CancellationToken ct)
    {
        var existing = await cn.QueryFirstOrDefaultAsync<(Guid Id, Guid? Tenant, Guid? Client)>(new CommandDefinition(
            commandText: "select id,tenant_id,cliente_id from plantaopro.usuarios where lower(email_normalizado)=lower(@email) and reg_status='A' limit 1",
            parameters: new { email },
            transaction: tx,
            cancellationToken: ct));
        Guid? id = existing.Id == Guid.Empty ? null : existing.Id;
        if (id.HasValue && id.Value != stableId)
            throw new InvalidOperationException($"O login demonstrativo {email} já pertence a outro registro. Nenhuma identidade foi alterada.");
        if (id.HasValue && (existing.Tenant != tenant || existing.Client != client))
            throw new InvalidOperationException($"O login demonstrativo {email} já existe em outro contexto. Nenhuma identidade foi alterada.");
        if (!id.HasValue)
        {
            id = stableId;
            var hash = Security.PasswordHashService.Hash(password);
            await cn.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.usuarios(id,tenant_id,cliente_id,nome,email,email_normalizado,senha_hash,status,reg_status,senha_alteracao_obrigatoria) values(@id,@tenant,@client,@name,@email,lower(@email),@hash,'ATIVO','A',false)", new { id, tenant, client, name, email, hash }, tx, cancellationToken: ct));
        }
        else if (reset)
        {
            var hash = Security.PasswordHashService.Hash(password);
            await cn.ExecuteAsync(new CommandDefinition("update plantaopro.usuarios set senha_hash=@hash,status='ATIVO',reg_status='A',senha_alteracao_obrigatoria=false,bloqueado_ate=null,reg_update=now() where id=@id", new { hash, id }, tx, cancellationToken: ct));
            await cn.ExecuteAsync(new CommandDefinition("update plantaopro.auth_sessoes set revogada_em=now(),motivo_revogacao='DEMO_PASSWORD_RESET',reg_update=now() where usuario_id=@id and revogada_em is null", new { id }, tx, cancellationToken: ct));
        }
        await cn.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.usuarios_perfis(tenant_id,cliente_id,usuario_id,perfil_id,reg_status) select @tenant,@client,@id,@profile,'A' where not exists(select 1 from plantaopro.usuarios_perfis where usuario_id=@id and perfil_id=@profile and reg_status='A')", new { tenant, client, id, profile }, tx, cancellationToken: ct));
    }
}
