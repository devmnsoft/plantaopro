using Npgsql;
namespace PlantaoPro.Tests.Infrastructure;
public sealed class PostgreSqlTestFixture : IAsyncLifetime
{
    // Fonte única de conexão de testes (TestDatabase): banco plantaopro_test por padrão,
    // sobrescrevível via PLANTAOPRO_TEST_CONNECTION para apontar um banco descartável de CI.
    public string ConnectionString { get; } = TestDatabase.ConnectionString;
    public async Task InitializeAsync() { await using var connection = new NpgsqlConnection(ConnectionString); await connection.OpenAsync(); }
    public Task DisposeAsync() => Task.CompletedTask;
}
