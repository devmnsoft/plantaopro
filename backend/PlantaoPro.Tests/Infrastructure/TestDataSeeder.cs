using Dapper;
using Npgsql;
namespace PlantaoPro.Tests.Infrastructure;
public static class TestDataSeeder
{
    public static async Task SeedAsync(string connectionString) { await using var connection = new NpgsqlConnection(connectionString); await connection.ExecuteAsync("select 1"); }
}
