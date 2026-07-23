using Dapper;
using Npgsql;

namespace PlantaoPro.Api;

public static class DatabaseStartupReadinessValidator
{
    private static readonly string[] EssentialTables = new[] { "usuarios", "perfis", "usuarios_perfis", "api_request_logs", "api_error_logs" };
    private static readonly Dictionary<string, string[]> EssentialColumns = new()
    {
        ["usuarios"] = new[] { "id", "nome", "email", "senha_hash", "reg_status", "cliente_id", "tenant_id", "email_normalizado", "status", "senha_alteracao_obrigatoria", "bloqueado_ate", "ultimo_login" },
        ["perfis"] = new[] { "id", "tenant_id", "cliente_id", "codigo", "base_sistema", "customizado", "status" },
        ["usuarios_perfis"] = new[] { "id", "tenant_id", "cliente_id", "usuario_id", "perfil_id", "reg_status" }
    };

    public static void Validate(string? connectionString, IWebHostEnvironment environment, IConfiguration configuration)
    {
        ConnectionStringStartupValidator.Validate(connectionString, environment, configuration);
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var csb = new NpgsqlConnectionStringBuilder(connectionString);
        try
        {
            using var cn = new NpgsqlConnection(connectionString);
            cn.Open();
            var schemaExists = cn.ExecuteScalar<int>("select 1 from information_schema.schemata where schema_name = @schema", new { schema = "plantaopro" }) == 1;
            if (!schemaExists) throw new InvalidOperationException("Schema obrigatório \"plantaopro\" não existe. Execute: dotnet run --project backend/PlantaoPro.Tools.Database -- install");

            foreach (var table in EssentialTables)
            {
                var exists = cn.ExecuteScalar<int>("select count(*) from information_schema.tables where table_schema = 'plantaopro' and table_name = @table", new { table }) == 1;
                if (!exists) throw new InvalidOperationException($"Tabela essencial plantaopro.{table} não existe. Execute: dotnet run --project backend/PlantaoPro.Tools.Database -- install");
            }

            foreach (var requirement in EssentialColumns)
            {
                foreach (var column in requirement.Value)
                {
                    var exists = cn.ExecuteScalar<int>("select count(*) from information_schema.columns where table_schema = 'plantaopro' and table_name = @table and column_name = @column", new { table = requirement.Key, column }) == 1;
                    if (!exists) throw new InvalidOperationException($"Schema de identidade incompatível: coluna obrigatória plantaopro.{requirement.Key}.{column} ausente. Execute: dotnet run --project backend/PlantaoPro.Tools.Database -- repair-identity-schema");
                }
            }
        }
        catch (PostgresException ex) when (ex.SqlState == "3D000")
        {
            throw new InvalidOperationException(MissingDatabaseMessage(csb.Database ?? "(não informado)", environment), ex);
        }
        catch (PostgresException ex) when (ex.SqlState == "28P01")
        {
            throw new InvalidOperationException("Usuário ou senha inválidos para PostgreSQL. Revise ConnectionStrings:Default sem expor a senha.", ex);
        }
        catch (PostgresException ex) when (ex.SqlState == "3F000")
        {
            throw new InvalidOperationException("Schema obrigatório plantaopro inexistente. Execute o comando install da ferramenta oficial.", ex);
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            throw new InvalidOperationException("Tabela essencial inexistente. Execute o comando install da ferramenta oficial.", ex);
        }
        catch (NpgsqlException ex) when (ex.SqlState == "08001")
        {
            throw new InvalidOperationException("Servidor PostgreSQL inacessível. Verifique host, porta e rede.", ex);
        }
        catch (PostgresException ex) when (ex.SqlState == "57P03")
        {
            throw new InvalidOperationException("PostgreSQL ainda está inicializando. Aguarde e tente novamente.", ex);
        }
    }

    private static string MissingDatabaseMessage(string database, IWebHostEnvironment environment)
    {
        var msg = $"O banco \"{database}\" não existe.";
        if (environment.IsDevelopment())
        {
            msg += "\n\nExecute:\n\ndotnet run --project backend/PlantaoPro.Tools.Database -- create-database\n\nDepois:\n\ndotnet run --project backend/PlantaoPro.Tools.Database -- install";
        }
        return msg;
    }
}
