using System.Text.RegularExpressions;
using Dapper;
using Npgsql;

var command = args.Length == 0 ? "doctor" : args[0].Trim().ToLowerInvariant();
var options = ParseOptions(args.Skip(1).ToArray());
var root = FindRepoRoot();
var cs = Environment.GetEnvironmentVariable("PLANTAOPRO_CONNECTION_STRING")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
    ?? "Host=localhost;Port=5432;Database=plantaopro;Username=postgres;Password=__SET_VIA_USER_SECRETS__;Pooling=true;Search Path=plantaopro,public";
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

try
{
    switch (command)
    {
        case "doctor": await Doctor(cs); break;
        case "create-database": await CreateDatabase(cs, env); break;
        case "install": await ExecuteScript(cs, root / "database" / "scrpt_completo.sql", "install"); break;
        case "upgrade": await ExecuteManifest(cs, root / "database" / "migration-manifest.json", "upgrade"); break;
        case "verify": await Verify(cs); break;
        case "status": await Status(cs); break;
        case "bootstrap-admin": await BootstrapAdmin(cs, options); break;
        case "seed-demo": Console.WriteLine("Seed demo seguro: nenhum dado sensível gerado por padrão."); break;
        case "reset-development": if (!env.Equals("Development", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("reset-development permitido somente em Development."); Console.WriteLine("Reset development exige confirmação externa; operação destrutiva não executada automaticamente."); break;
        default: throw new InvalidOperationException("Comando desconhecido. Use doctor, create-database, install, upgrade, verify, status, bootstrap-admin, seed-demo ou reset-development.");
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

static async Task ExecuteScript(string cs, Path script, string label)
{
    if (!File.Exists(script)) throw new FileNotFoundException("Script não encontrado", script.ToString());
    await using var cn = new NpgsqlConnection(cs);
    await cn.OpenAsync();
    await using var tx = await cn.BeginTransactionAsync();
    await cn.ExecuteAsync(await File.ReadAllTextAsync(script), transaction: tx, commandTimeout: 0);
    await tx.CommitAsync();
    Console.WriteLine($"{label} concluído com sucesso.");
}

static async Task ExecuteManifest(string cs, Path manifest, string label)
{
    if (!File.Exists(manifest)) throw new FileNotFoundException("Manifesto não encontrado", manifest.ToString());
    Console.WriteLine($"{label}: use database/scrpt_completo.sql para bases limpas e migrations versionadas do manifesto para legado.");
    await Verify(cs);
}

static async Task Verify(string cs)
{
    await using var cn = new NpgsqlConnection(cs);
    await cn.OpenAsync();
    foreach (var t in new[] { "perfis", "usuarios", "api_request_logs", "implantacao_status" })
    {
        var ok = await cn.ExecuteScalarAsync<int>("select count(*) from information_schema.tables where table_schema='plantaopro' and table_name=@t", new { t });
        Console.WriteLine($"plantaopro.{t}: {(ok == 1 ? "OK" : "AUSENTE")}");
    }
}
static Task Status(string cs) => Verify(cs);

static async Task BootstrapAdmin(string cs, Dictionary<string,string> opt)
{
    var email = opt.ContainsKey("email") ? opt["email"] : throw new InvalidOperationException("Informe --email.");
    var name = opt.ContainsKey("name") ? opt["name"] : "Administrador Geral";
    var password = Environment.GetEnvironmentVariable("PLANTAOPRO_BOOTSTRAP_ADMIN_PASSWORD") ?? throw new InvalidOperationException("Defina PLANTAOPRO_BOOTSTRAP_ADMIN_PASSWORD fora do Git.");
    var hash = BCrypt.Net.BCrypt.HashPassword(password);
    await using var cn = new NpgsqlConnection(cs); await cn.OpenAsync();
    await cn.ExecuteAsync(@"insert into plantaopro.usuarios(nome,email,email_normalizado,senha_hash,status,reg_status) select @name,@email,upper(@email),@hash,'ATIVO','A' where not exists (select 1 from plantaopro.usuarios where email_normalizado=upper(@email))", new { name, email, hash });
    Console.WriteLine($"Administrador global garantido para {email}; senha não exibida.");
}
static Dictionary<string,string> ParseOptions(string[] a){ var d=new Dictionary<string,string>(); for(var i=0;i<a.Length-1;i+=2) if(a[i].StartsWith("--")) d[a[i].Substring(2)]=a[i+1]; return d; }
static Path FindRepoRoot(){ var d=new DirectoryInfo(Directory.GetCurrentDirectory()); while(d!=null && !File.Exists(Path.Combine(d.FullName,"backend","PlantaoPro.sln"))) d=d.Parent; return Path.GetFullPath(d?.FullName ?? Directory.GetCurrentDirectory()); }
static void ValidateDbName(string name){ if(!Regex.IsMatch(name ?? "", "^[a-zA-Z_][a-zA-Z0-9_]{0,62}$")) throw new InvalidOperationException("Nome do banco inválido para criação segura."); }
static string QuoteIdentifier(string v)=>"\""+v.Replace("\"","\"\"")+"\"";
static string Sanitize(string s)=>Regex.Replace(s, "(?i)(password|senha)=[^;\\s]+", "$1=[omitida]");
