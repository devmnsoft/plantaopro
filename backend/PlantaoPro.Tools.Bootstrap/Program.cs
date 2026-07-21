using Dapper;
using Npgsql;

if (args.Length == 0 || args[0] != "create-admin")
{
    Console.Error.WriteLine("Uso: create-admin --email admin@empresa.com --name \"Administrador Geral\"");
    return 2;
}
string? email = Value("--email");
string? name = Value("--name");
if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(name))
{
    Console.Error.WriteLine("Informe --email e --name.");
    return 2;
}
var password = Environment.GetEnvironmentVariable("PLANTAOPRO_BOOTSTRAP_PASSWORD");
if (string.IsNullOrWhiteSpace(password))
{
    Console.Error.Write("Senha inicial (não será exibida): ");
    password = ReadPassword();
    Console.Error.WriteLine();
}
ValidatePassword(password);
var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? Environment.GetEnvironmentVariable("PLANTAOPRO_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(cs))
{
    Console.Error.WriteLine("Configure ConnectionStrings__Default ou PLANTAOPRO_CONNECTION_STRING sem expor senha em argumentos.");
    return 2;
}
await using var cn = new NpgsqlConnection(cs);
await cn.OpenAsync();
var missing = await cn.QueryAsync<string>("select t from unnest(array['usuarios','perfis','usuarios_perfis','login_tentativas']) t where to_regclass('plantaopro.'||t) is null");
if (missing.Any()) throw new InvalidOperationException("Schema inválido. Tabelas ausentes: " + string.Join(',', missing));
var normalized = email.Trim().ToLowerInvariant();
var existing = await cn.ExecuteScalarAsync<Guid?>("select id from plantaopro.usuarios where email_normalizado=@email or lower(email)=@email limit 1", new { email = normalized });
if (existing.HasValue)
{
    Console.WriteLine("Administrador já existe. Nenhuma senha foi sobrescrita.");
    return 0;
}
var tx = await cn.BeginTransactionAsync();
try
{
    var perfilId = await cn.ExecuteScalarAsync<Guid?>("select id from plantaopro.perfis where codigo='ADMINISTRADOR_GLOBAL' and tenant_id is null and reg_status='A' limit 1", transaction: tx);
    if (!perfilId.HasValue)
    {
        perfilId = Guid.NewGuid();
        await cn.ExecuteAsync("insert into plantaopro.perfis(id,codigo,nome,descricao,base_sistema,customizado,status,reg_status,reg_date) values(@perfilId,'ADMINISTRADOR_GLOBAL','Administrador Global','Administração SaaS global',true,false,'ATIVO','A',now())", new { perfilId }, tx);
    }
    var userId = Guid.NewGuid();
    var hash = BCrypt.Net.BCrypt.HashPassword(password);
    await cn.ExecuteAsync("insert into plantaopro.usuarios(id,nome,email,email_normalizado,senha_hash,status,reg_status,senha_alteracao_obrigatoria,reg_date) values(@userId,@name,@email,@normalized,@hash,'ATIVO','A',true,now())", new { userId, name, email = email.Trim(), normalized, hash }, tx);
    await cn.ExecuteAsync("insert into plantaopro.usuarios_perfis(id,usuario_id,perfil_id,reg_status,reg_date) values(gen_random_uuid(),@userId,@perfilId,'A',now())", new { userId, perfilId }, tx);
    await cn.ExecuteAsync("insert into plantaopro.auditoria(id,usuario_id,acao,entidade,registro_id,descricao,reg_status,reg_date) values(gen_random_uuid(),@userId,'BOOTSTRAP_ADMIN','usuarios',@userId,'Administrador global inicial criado via CLI','A',now())", new { userId }, tx);
    await tx.CommitAsync();
    Console.WriteLine("Administrador Global criado com sucesso. Primeiro login exigirá troca de senha.");
    return 0;
}
catch { await tx.RollbackAsync(); throw; }

string? Value(string key) { var i = Array.IndexOf(args, key); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
static void ValidatePassword(string password)
{
    if (password.Length < 12 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) || !password.Any(ch => !char.IsLetterOrDigit(ch)))
        throw new InvalidOperationException("Senha deve ter 12+ caracteres, maiúscula, minúscula, número e símbolo.");
}
static string ReadPassword()
{
    var chars = new List<char>();
    ConsoleKeyInfo key;
    while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
    {
        if (key.Key == ConsoleKey.Backspace && chars.Count > 0) chars.RemoveAt(chars.Count - 1);
        else if (!char.IsControl(key.KeyChar)) chars.Add(key.KeyChar);
    }
    return new string(chars.ToArray());
}
