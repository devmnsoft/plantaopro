using Dapper;
using Npgsql;
namespace PlantaoPro.Tests.Infrastructure;
public sealed class DatabaseResetService
{
    private readonly string connectionString;
    public DatabaseResetService(string connectionString) { this.connectionString = connectionString; }
    public async Task ResetAsync() { await using var connection = new NpgsqlConnection(connectionString); await connection.ExecuteAsync("truncate plantaopro.work_item_history, plantaopro.work_item_comments, plantaopro.work_item_assignments, plantaopro.work_items restart identity cascade"); }
}
