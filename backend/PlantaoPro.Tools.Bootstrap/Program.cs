using Dapper;
using Npgsql;

const string LocalEmail = "admin.global@plantaopro.local";
var localPassword = string.Concat("PlantaoPro.Admin@", "2026!", "Trocar");

if (args.Length == 0 || (args[0] != "create-admin" && args[0] != "hash-password"))
{
    Console.Error.WriteLine("Uso: create-admin [--email email] [--name nome] | hash-password");
    return 2;
}

var environment = Environment.GetEnvironmentVariable("PLANTAOPRO_BOOTSTRAP_ENVIRONMENT");
if (string.IsNullOrWhiteSpace(environment))
{
    Console.Error.WriteLine("PLANTAOPRO_BOOTSTRAP_ENVIRONMENT deve ser informado explicitamente.");
    return 2;
}

var password = Environment.GetEnvironmentVariable("PLANTAOPRO_BOOTSTRAP_PASSWORD");
if (string.IsNullOrWhiteSpace(password))
{
    Console.Error.WriteLine("PLANTAOPRO_BOOTSTRAP_PASSWORD deve ser fornecida por secret ou variável de ambiente.");
    return 2;
}

ValidatePassword(password);
var email = Value("--email") ?? Environment.GetEnvironmentVariable("PLANTAOPRO_BOOTSTRAP_ADMIN_EMAIL") ?? LocalEmail;
var name = Value("--name") ?? Environment.GetEnvironmentVariable("PLANTAOPRO_BOOTSTRAP_ADMIN_NAME") ?? "Super Administrador PlantãoPro";
var forceRotationValue = Environment.GetEnvironmentVariable("PLANTAOPRO_BOOTSTRAP_FORCE_ROTATION");
var forceRotation = bool.TryParse(forceRotationValue, out var parsedRotation) && parsedRotation;
var production = string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);

if (production && (email.EndsWith("@plantaopro.local", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(password, localPassword, StringComparison.Ordinal) || !forceRotation))
{
    Console.Error.WriteLine("Bootstrap recusado: Production exige e-mail próprio, senha segura exclusiva e rotação obrigatória.");
    return 2;
}

var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
password = string.Empty;
Environment.SetEnvironmentVariable("PLANTAOPRO_BOOTSTRAP_PASSWORD", null);

if (args[0] == "hash-password")
{
    Console.Out.Write(hash);
    return 0;
}

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default") ??
                       Environment.GetEnvironmentVariable("PLANTAOPRO_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Configure ConnectionStrings__Default ou PLANTAOPRO_CONNECTION_STRING sem expor segredo em argumentos.");
    return 2;
}

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();
var missing = await connection.QueryAsync<string>("select t from unnest(array['usuarios','perfis','usuarios_perfis','perfil_permissoes','permissoes','auditoria']) t where to_regclass('plantaopro.'||t) is null");
if (missing.Any())
    throw new InvalidOperationException("Schema inválido. Tabelas ausentes: " + string.Join(',', missing));

var normalizedEmail = email.Trim().ToLowerInvariant();
await using var transaction = await connection.BeginTransactionAsync();
try
{
    await connection.ExecuteAsync("select pg_advisory_xact_lock(hashtext('plantaopro.bootstrap.superadmin'))", transaction: transaction);
    var profileId = await connection.ExecuteScalarAsync<Guid?>("select id from plantaopro.perfis where lower(btrim(codigo))='administrador_global' and tenant_id is null and cliente_id is null and reg_status='A' order by reg_date,id limit 1", transaction: transaction);
    if (!profileId.HasValue)
    {
        profileId = Guid.NewGuid();
        await connection.ExecuteAsync("insert into plantaopro.perfis(id,codigo,nome,descricao,base_sistema,customizado,status,reg_status,reg_date) values(@profileId,'ADMINISTRADOR_GLOBAL','Super Administrador','Administração SaaS global',true,false,'ATIVO','A',now())", new { profileId }, transaction);
    }

    var userId = await connection.ExecuteScalarAsync<Guid?>("select id from plantaopro.usuarios where lower(coalesce(email_normalizado,email))=@email and reg_status='A' order by reg_date,id limit 1", new { email = normalizedEmail }, transaction);
    var created = !userId.HasValue;
    if (created)
    {
        userId = Guid.NewGuid();
        await connection.ExecuteAsync(@"insert into plantaopro.usuarios(id,tenant_id,cliente_id,nome,email,email_normalizado,senha_hash,status,reg_status,senha_alteracao_obrigatoria,reg_date)
values(@userId,null,null,@name,@email,@email,@hash,'ATIVO','A',@forceRotation,now())", new { userId, name, email = normalizedEmail, hash, forceRotation }, transaction);
    }

    await connection.ExecuteAsync(@"insert into plantaopro.usuarios_perfis(id,tenant_id,cliente_id,usuario_id,perfil_id,reg_status,reg_date)
select gen_random_uuid(),null,null,@userId,@profileId,'A',now()
where not exists(select 1 from plantaopro.usuarios_perfis where usuario_id=@userId and perfil_id=@profileId and reg_status='A')", new { userId, profileId }, transaction);
    await connection.ExecuteAsync(@"insert into plantaopro.perfil_permissoes(id,perfil_id,permissao_id,permitido,bloqueado_por_plano,reg_status,reg_date)
select gen_random_uuid(),@profileId,p.id,true,false,'A',now() from plantaopro.permissoes p
where p.reg_status='A' and not exists(select 1 from plantaopro.perfil_permissoes pp where pp.perfil_id=@profileId and pp.permissao_id=p.id and pp.reg_status='A')", new { profileId }, transaction);
    await connection.ExecuteAsync("insert into plantaopro.auditoria(id,codigo,nome,status,dados,criado_em) values(gen_random_uuid(),@action,@description,'ATIVO',jsonb_build_object('usuario_id',@userId,'senha_alterada',false),now())", new { userId, action = created ? "BOOTSTRAP_ADMIN" : "BOOTSTRAP_ADMIN_RECONCILIADO", description = created ? "Administrador global inicial criado" : "Vínculos do administrador global reconciliados; senha preservada" }, transaction);
    await transaction.CommitAsync();
    Console.WriteLine(created ? "Administrador global criado; troca de senha obrigatória configurada." : "Administrador global preservado; vínculos reconciliados sem alterar a senha.");
    return 0;
}
catch
{
    await transaction.RollbackAsync();
    throw;
}

string? Value(string key)
{
    var index = Array.IndexOf(args, key);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static void ValidatePassword(string value)
{
    if (value.Length < 12 || !value.Any(char.IsUpper) || !value.Any(char.IsLower) || !value.Any(char.IsDigit) || !value.Any(ch => !char.IsLetterOrDigit(ch)))
        throw new InvalidOperationException("Senha deve ter 12+ caracteres, maiúscula, minúscula, número e símbolo.");
}
