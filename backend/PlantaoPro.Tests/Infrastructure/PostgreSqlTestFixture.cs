using Npgsql;
namespace PlantaoPro.Tests.Infrastructure;
public sealed class PostgreSqlTestFixture : IAsyncLifetime
{
    // Default unico de testes: banco plantaopro_test (mesma convencao de PlantaoProApiFactory).
    public string ConnectionString { get; } = Environment.GetEnvironmentVariable("PLANTAOPRO_TEST_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=plantaopro_test;Username=postgres;"
        + "Password=123456";
    public async Task InitializeAsync() { await using var connection = new NpgsqlConnection(ConnectionString); await connection.OpenAsync(); }
    public Task DisposeAsync() => Task.CompletedTask;
}
