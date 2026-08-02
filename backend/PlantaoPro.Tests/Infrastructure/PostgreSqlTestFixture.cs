using Npgsql;
namespace PlantaoPro.Tests.Infrastructure;
public sealed class PostgreSqlTestFixture : IAsyncLifetime
{
    public string ConnectionString { get; } = Environment.GetEnvironmentVariable("PLANTAOPRO_TEST_CONNECTION") ?? "Host=localhost;Port=5432;Database=plantaopro_test;Username=postgres;Password=postgres";
    public async Task InitializeAsync() { await using var connection = new NpgsqlConnection(ConnectionString); await connection.OpenAsync(); }
    public Task DisposeAsync() => Task.CompletedTask;
}
