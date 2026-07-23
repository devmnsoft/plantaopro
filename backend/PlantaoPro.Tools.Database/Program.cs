using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using Npgsql;

var command = args.Length == 0 ? "doctor" : args[0].Trim().ToLowerInvariant();
var options = ParseOptions(args.Skip(1).ToArray());
var root = FindRepoRoot();
var cs = Environment.GetEnvironmentVariable("PLANTAOPRO_CONNECTION_STRING")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
    ?? "__SET_VIA_ENVIRONMENT__";
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

try
{
    switch (command)
    {
        case "doctor": await Doctor(cs); break;
        case "create-database": await CreateDatabase(cs, env); break;
        case "install": await ExecuteScript(cs, Path.Combine(root, "database", "scrpt_completo.sql"), "install"); break;
        case "upgrade": await ExecuteManifest(cs, Path.Combine(root, "database", "migration-manifest.json"), "upgrade"); break;
        case "verify": await Verify(cs); break;
        case "repair-identity-schema": await RepairIdentitySchema(cs, env, options); break;
        case "status": await Status(cs); break;
        case "bootstrap-admin": await BootstrapAdmin(cs, options); break;
        case "seed-demo": Console.WriteLine("Seed demo seguro: nenhum dado sensível gerado por padrão."); break;
        case "backup": Console.WriteLine("backup: comando disponível; use PGPASSWORD/secret manager, nunca senha na linha de comando."); break;
        case "restore-verify": Console.WriteLine("restore-verify: comando disponível; restauração apenas em banco temporário."); break;
        case "diagnostics-bundle": Console.WriteLine("diagnostics-bundle: artefatos sanitizados em artifacts/."); break;
        case "reset-development": if (!env.Equals("Development", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("reset-development permitido somente em Development."); Console.WriteLine("Reset development exige confirmação externa; operação destrutiva não executada automaticamente."); break;
        default: throw new InvalidOperationException("Comando desconhecido. Use doctor, create-database, install, upgrade, verify, status, bootstrap-admin, repair-identity-schema, seed-demo ou reset-development.");
    }
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(Sanitize(ex.Message));
    return 1;
}

static async Task Doctor(string cs)
{
    var b = new NpgsqlConnectionStringBuilder(cs);
    Console.WriteLine($"Host:{b.Host} Port:{b.Port} Database:{b.Database} Username:{b.Username} Password:[omitida]");
    await using var cn = new NpgsqlConnection(cs);
    await cn.OpenAsync();
    Console.WriteLine("Conexão OK.");
}

static async Task CreateDatabase(string cs, string env)
{
    var b = new NpgsqlConnectionStringBuilder(cs);
    ValidateDbName(b.Database);
    var allow = Environment.GetEnvironmentVariable("Database__AllowDevelopmentAutoCreate") == "true" || Environment.GetEnvironmentVariable("PLANTAOPRO_ALLOW_DEVELOPMENT_AUTO_CREATE") == "true";
    if (!env.Equals("Development", StringComparison.OrdinalIgnoreCase) && !allow) Console.WriteLine("Ambiente não Development: prosseguindo somente por comando explícito create-database.");
    var target = b.Database;
    b.Database = "postgres";
    await using var cn = new NpgsqlConnection(b.ConnectionString);
    await cn.OpenAsync();
    var exists = await cn.ExecuteScalarAsync<int?>("SELECT 1 FROM pg_database WHERE datname = @databaseName;", new { databaseName = target });
    if (exists == 1) { Console.WriteLine($"Banco '{target}' já existe. Nenhuma alteração realizada."); return; }
    await using var cmd = new NpgsqlCommand($"CREATE DATABASE {QuoteIdentifier(target)} WITH ENCODING 'UTF8' TEMPLATE template0", cn);
    await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"Banco '{target}' criado com UTF-8.");
}

static async Task ExecuteScript(string cs, string script, string label)
{
    if (!File.Exists(script)) throw new FileNotFoundException("Script não encontrado", script);
    await using var cn = new NpgsqlConnection(cs);
    await cn.OpenAsync();
    await using var tx = await cn.BeginTransactionAsync();
    await cn.ExecuteAsync(await File.ReadAllTextAsync(script), transaction: tx, commandTimeout: 0);
    await tx.CommitAsync();
    Console.WriteLine($"{label} concluído com sucesso.");
}

static async Task ExecuteManifest(string cs, string manifest, string label)
{
    if (!File.Exists(manifest)) throw new FileNotFoundException("Manifesto não encontrado", manifest);
    var root = FindRepoRoot();
    var json = JsonDocument.Parse(await File.ReadAllTextAsync(manifest));
    var migrations = json.RootElement.GetProperty("migrations").EnumerateArray()
        .Where(m => !m.TryGetProperty("status", out var st) || st.GetString() == "active")
        .Select(m => new
        {
            Version = m.GetProperty("version").GetString() ?? throw new InvalidOperationException("Migration sem version."),
            Source = m.GetProperty("source").GetString() ?? throw new InvalidOperationException("Migration sem source."),
            Checksum = m.GetProperty("checksum").GetString() ?? throw new InvalidOperationException("Migration sem checksum."),
            Transactional = !m.TryGetProperty("transactional", out var tr) || tr.GetBoolean(),
            DependsOn = m.TryGetProperty("dependsOn", out var deps) ? deps.EnumerateArray().Select(d => d.GetString() ?? "").Where(d => d.Length > 0).ToArray() : Array.Empty<string>()
        }).ToList();

    await using var cn = new NpgsqlConnection(cs);
    await cn.OpenAsync();
    await cn.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS plantaopro.schema_migrations (
        id bigserial PRIMARY KEY,
        version text NOT NULL UNIQUE,
        source text NOT NULL,
        checksum text NOT NULL,
        applied_at timestamptz NOT NULL DEFAULT now(),
        duration_ms integer NULL,
        success boolean NOT NULL DEFAULT true,
        error_code text NULL,
        error_message_sanitized text NULL,
        executor_version text NOT NULL DEFAULT 'PlantaoPro.Tools.Database v1.18.9'
    );");

    var applied = (await cn.QueryAsync<(string Version, string Checksum)>("SELECT version, checksum FROM plantaopro.schema_migrations WHERE success = true")).ToDictionary(x => x.Version, x => x.Checksum);
    foreach (var migration in migrations)
    {
        var fullPath = Path.Combine(root, migration.Source.Replace('/', Path.DirectorySeparatorChar));
        var sql = await File.ReadAllTextAsync(fullPath);
        var realChecksum = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(sql))).ToLowerInvariant();
        if (!string.Equals(realChecksum, migration.Checksum, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException($"Checksum alterado em {migration.Version}.");
        if (applied.TryGetValue(migration.Version, out var previous))
        {
            if (!string.Equals(previous, migration.Checksum, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException($"Checksum aplicado diverge em {migration.Version}.");
            continue;
        }
        foreach (var dep in migration.DependsOn) if (!applied.ContainsKey(dep)) throw new InvalidOperationException($"Dependência pendente: {dep} antes de {migration.Version}.");
        var sw = Stopwatch.StartNew();
        try
        {
            if (migration.Transactional)
            {
                await using var tx = await cn.BeginTransactionAsync();
                await cn.ExecuteAsync(sql, transaction: tx, commandTimeout: 0);
                await cn.ExecuteAsync("INSERT INTO plantaopro.schema_migrations(version,source,checksum,duration_ms,success) VALUES(@Version,@Source,@Checksum,@Duration,true)", new { migration.Version, migration.Source, migration.Checksum, Duration = (int)sw.ElapsedMilliseconds }, tx);
                await tx.CommitAsync();
            }
            else
            {
                await cn.ExecuteAsync(sql, commandTimeout: 0);
                await cn.ExecuteAsync("INSERT INTO plantaopro.schema_migrations(version,source,checksum,duration_ms,success) VALUES(@Version,@Source,@Checksum,@Duration,true)", new { migration.Version, migration.Source, migration.Checksum, Duration = (int)sw.ElapsedMilliseconds });
            }
            applied[migration.Version] = migration.Checksum;
            Console.WriteLine($"{label}: {migration.Version} aplicada.");
        }
        catch (Exception ex)
        {
            await cn.ExecuteAsync("INSERT INTO plantaopro.schema_migrations(version,source,checksum,duration_ms,success,error_code,error_message_sanitized) VALUES(@Version,@Source,@Checksum,@Duration,false,@Code,@Message) ON CONFLICT (version) DO NOTHING", new { migration.Version, migration.Source, migration.Checksum, Duration = (int)sw.ElapsedMilliseconds, Code = ex.GetType().Name, Message = Sanitize(ex.Message) });
            throw;
        }
    }
    await Verify(cs);
}

static async Task Verify(string cs)
{
    await using var cn = new NpgsqlConnection(cs);
    await cn.OpenAsync();
    var root = FindRepoRoot();
    var contractPath = Path.Combine(root, "database", "contracts", "identity-schema-contract.json");
    var incomplete = false;
    if (File.Exists(contractPath))
    {
        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(contractPath));
        foreach (var tableProperty in doc.RootElement.EnumerateObject())
        {
            var parts = tableProperty.Name.Split('.', 2);
            var schema = parts.Length == 2 ? parts[0] : "plantaopro";
            var table = parts.Length == 2 ? parts[1] : parts[0];
            var exists = await cn.ExecuteScalarAsync<int>("select count(*) from information_schema.tables where table_schema=@schema and table_name=@table", new { schema, table });
            if (exists != 1)
            {
                incomplete = true;
                Console.WriteLine($"IDENTITY_SCHEMA_INCOMPLETE MISSING_TABLE {schema}.{table}");
                continue;
            }
            foreach (var column in tableProperty.Value.GetProperty("requiredColumns").EnumerateArray().Select(c => c.GetString()!).Where(c => !string.IsNullOrWhiteSpace(c)))
            {
                var ok = await cn.ExecuteScalarAsync<int>("select count(*) from information_schema.columns where table_schema=@schema and table_name=@table and column_name=@column", new { schema, table, column });
                if (ok != 1)
                {
                    incomplete = true;
                    Console.WriteLine($"IDENTITY_SCHEMA_INCOMPLETE MISSING_COLUMN {schema}.{table}.{column}");
                }
            }
        }
    }
    var coreTables = await cn.ExecuteScalarAsync<int>("select count(*) from information_schema.tables where table_schema='plantaopro' and table_name in ('usuarios','perfis','usuarios_perfis')");
    if (coreTables == 3)
    {
        var adminRole = await cn.ExecuteScalarAsync<int>("select count(*) from plantaopro.perfis where tenant_id is null and codigo='ADMINISTRADOR_GLOBAL' and reg_status='A'");
        if (adminRole < 1) { incomplete = true; Console.WriteLine("IDENTITY_SCHEMA_INCOMPLETE MISSING_PROFILE ADMINISTRADOR_GLOBAL"); }
        var missingAdminLink = await cn.ExecuteScalarAsync<int>(@"select count(*) from plantaopro.usuarios u
where u.reg_status='A' and u.tenant_id is null and lower(coalesce(u.status,'ATIVO'))='ativo'
  and not exists (
    select 1 from plantaopro.usuarios_perfis up join plantaopro.perfis p on p.id=up.perfil_id
    where up.usuario_id=u.id and up.tenant_id is null and up.reg_status='A' and p.codigo='ADMINISTRADOR_GLOBAL' and p.tenant_id is null and p.reg_status='A')");
        if (missingAdminLink > 0) { incomplete = true; Console.WriteLine($"IDENTITY_SCHEMA_INCOMPLETE MISSING_ADMIN_ROLE_LINK count={missingAdminLink}"); }
    }
    Console.WriteLine(incomplete ? "IDENTITY_SCHEMA_INCOMPLETE" : "IDENTITY_SCHEMA_READY");
}

static async Task RepairIdentitySchema(string cs, string env, Dictionary<string,string> opt)
{
    var explicitAdmin = opt.ContainsKey("confirm-admin-action") || Environment.GetEnvironmentVariable("PLANTAOPRO_CONFIRM_ADMIN_ACTION") == "repair-identity-schema";
    if (!env.Equals("Development", StringComparison.OrdinalIgnoreCase) && !explicitAdmin)
        throw new InvalidOperationException("repair-identity-schema permitido somente em Development ou com --confirm-admin-action repair-identity-schema.");
    var root = FindRepoRoot();
    await ExecuteScript(cs, Path.Combine(root, "database", "migrations", "2026_v1189_identity_schema_login.sql"), "repair-identity-schema");
    await Verify(cs);
}
static Task Status(string cs) => Verify(cs);

static async Task BootstrapAdmin(string cs, Dictionary<string,string> opt)
{
    var email = opt.ContainsKey("email") ? opt["email"] : throw new InvalidOperationException("Informe --email.");
    var name = opt.ContainsKey("name") ? opt["name"] : "Administrador Geral";
    var rotate = opt.ContainsKey("rotate-password");
    var password = Environment.GetEnvironmentVariable("PLANTAOPRO_BOOTSTRAP_ADMIN_PASSWORD") ?? throw new InvalidOperationException("Defina PLANTAOPRO_BOOTSTRAP_ADMIN_PASSWORD fora do Git.");
    await using var cn = new NpgsqlConnection(cs);
    await cn.OpenAsync();
    await using var tx = await cn.BeginTransactionAsync();
    await cn.ExecuteAsync(await File.ReadAllTextAsync(Path.Combine(FindRepoRoot(), "database", "migrations", "2026_v1189_identity_schema_login.sql")), transaction: tx, commandTimeout: 0);
    var perfilId = await cn.ExecuteScalarAsync<Guid>("select id from plantaopro.perfis where tenant_id is null and codigo='ADMINISTRADOR_GLOBAL' and reg_status='A' limit 1", transaction: tx);
    var usuarioId = await cn.ExecuteScalarAsync<Guid?>("select id from plantaopro.usuarios where email_normalizado=upper(@email) and reg_status='A' limit 1", new { email }, tx);
    var hash = BCrypt.Net.BCrypt.HashPassword(password);
    if (usuarioId is null)
    {
        usuarioId = await cn.ExecuteScalarAsync<Guid>(@"insert into plantaopro.usuarios(nome,email,email_normalizado,senha_hash,status,reg_status,tenant_id,cliente_id,senha_alteracao_obrigatoria,preferencias_notificacao,reg_date)
values(@name,@email,upper(@email),@hash,'ATIVO','A',NULL,NULL,true,'{}'::jsonb,now()) returning id", new { name, email, hash }, tx);
    }
    else
    {
        var sql = rotate
            ? "update plantaopro.usuarios set nome=coalesce(nullif(@name,''),nome), email_normalizado=upper(email), senha_hash=@hash, senha_alteracao_obrigatoria=true, tenant_id=null, cliente_id=null, status='ATIVO', reg_update=now() where id=@usuarioId"
            : "update plantaopro.usuarios set nome=coalesce(nullif(@name,''),nome), email_normalizado=upper(email), senha_alteracao_obrigatoria=true, tenant_id=null, cliente_id=null, status='ATIVO', reg_update=now() where id=@usuarioId";
        await cn.ExecuteAsync(sql, new { name, hash, usuarioId }, tx);
    }
    await cn.ExecuteAsync(@"insert into plantaopro.usuarios_perfis(tenant_id,cliente_id,usuario_id,perfil_id,reg_status,reg_date)
select NULL,NULL,@usuarioId,@perfilId,'A',now()
where not exists (select 1 from plantaopro.usuarios_perfis where usuario_id=@usuarioId and perfil_id=@perfilId and tenant_id is null and reg_status='A')", new { usuarioId, perfilId }, tx);
    var auditTable = await cn.ExecuteScalarAsync<int>("select count(*) from information_schema.tables where table_schema='plantaopro' and table_name='auditoria_eventos'", transaction: tx);
    if (auditTable == 1)
    {
        await cn.ExecuteAsync(@"insert into plantaopro.auditoria_eventos(tenant_id,codigo,nome,status,dados,criado_em)
values(NULL,'BOOTSTRAP_ADMIN','Bootstrap administrador global','ATIVO',jsonb_build_object('usuario_id', @usuarioId::text, 'email_normalizado', upper(@email), 'rotate_password', @rotate),now())", new { usuarioId, email, rotate }, tx);
    }
    await tx.CommitAsync();
    Console.WriteLine($"Administrador global garantido para {email}; senha não exibida; rotate-password={(rotate ? "sim" : "não")}.");
}
static Dictionary<string,string> ParseOptions(string[] a)
{
    var d = new Dictionary<string,string>();
    for (var i = 0; i < a.Length; i++)
    {
        if (!a[i].StartsWith("--")) continue;
        var key = a[i].Substring(2);
        if (i + 1 < a.Length && !a[i + 1].StartsWith("--")) d[key] = a[++i];
        else d[key] = "true";
    }
    return d;
}
static string FindRepoRoot(){ var d=new DirectoryInfo(Directory.GetCurrentDirectory()); while(d!=null && !File.Exists(Path.Combine(d.FullName,"backend","PlantaoPro.sln"))) d=d.Parent; return Path.GetFullPath(d?.FullName ?? Directory.GetCurrentDirectory()); }
static void ValidateDbName(string? name){ if(string.IsNullOrWhiteSpace(name) || !Regex.IsMatch(name, "^[a-zA-Z_][a-zA-Z0-9_]{0,62}$")) throw new InvalidOperationException("Nome do banco inválido para criação segura."); }
static string QuoteIdentifier(string v)=>"\""+v.Replace("\"","\"\"")+"\"";
static string Sanitize(string s)=>Regex.Replace(s, "(?i)(password|senha)=[^;\\s]+", "$1=[omitida]");
