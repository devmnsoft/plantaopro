using Npgsql;

namespace PlantaoPro.Api;

public static class ConnectionStringStartupValidator
{
    public static void Validate(string? connectionString, IWebHostEnvironment environment, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:Default não configurada.");

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        Console.WriteLine($"Database startup configuration Host:{builder.Host} Port:{builder.Port} Database:{builder.Database} Environment:{environment.EnvironmentName}");

        var allowLegacy = configuration.GetValue<bool>("Database:AllowLegacyPostgresDatabase");
        if (environment.IsDevelopment() && string.Equals(builder.Database, "postgres", StringComparison.OrdinalIgnoreCase) && !allowLegacy)
            throw new InvalidOperationException("Development deve usar Database=plantaopro. Configure Database:AllowLegacyPostgresDatabase=true apenas para compatibilidade temporária.");
    }
}
