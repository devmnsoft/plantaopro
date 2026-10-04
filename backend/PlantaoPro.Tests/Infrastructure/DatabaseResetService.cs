using Dapper;
using Npgsql;
namespace PlantaoPro.Tests.Infrastructure;

/// <summary>
/// Reset destrutivo das tabelas work_items (truncate + restart identity cascade).
///
/// WP-A4 — validação pré-destrutiva: o truncate só é executado contra um banco
/// EXPLICITAMENTE AUTORIZADO como descartável:
///   1. PLANTAOPRO_TEST_CONNECTION definida (banco descartável de CI); ou
///   2. nome do banco contendo "test" (ex.: plantaopro_test); ou
///   3. PLANTAOPRO_ALLOW_DESTRUCTIVE_DB=on (autorização explícita por fora do padrão).
/// Em qualquer outro caso (ex.: connection string apontando para o banco operacional)
/// a operação falha com mensagem explícita ANTES de tocar em qualquer tabela.
/// </summary>
public sealed class DatabaseResetService
{
    private readonly string connectionString;
    public DatabaseResetService(string connectionString) { this.connectionString = connectionString; }

    public async Task ResetAsync()
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        ValidarBancoAutorizado(
            builder.Database ?? "",
            Environment.GetEnvironmentVariable("PLANTAOPRO_TEST_CONNECTION") is { Length: > 0 },
            AutorizacaoDestrutivaExplicita());
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.ExecuteAsync("truncate plantaopro.work_item_history, plantaopro.work_item_comments, plantaopro.work_item_assignments, plantaopro.work_items restart identity cascade");
    }

    /// <summary>
    /// Regra pura de autorização pré-destrutiva (isolada dos environment variables para
    /// permitir testes unitários determinísticos em qualquer ambiente): um banco só pode ser
    /// destruído se foi explicitamente autorizado de alguma das três formas documentadas.
    /// </summary>
    internal static void ValidarBancoAutorizado(string nomeBanco, bool ciAutorizada, bool autorizacaoExplicita)
    {
        if (ciAutorizada || nomeBanco.Contains("test", StringComparison.OrdinalIgnoreCase) || autorizacaoExplicita)
            return;
        throw new InvalidOperationException(
            $"Validação pré-destrutiva do DatabaseResetService: o banco '{nomeBanco}' não está " +
            "explicitamente autorizado como descartável. Defina PLANTAOPRO_TEST_CONNECTION " +
            "(banco descartável de CI), use um nome de banco contendo 'test' ou defina " +
            "PLANTAOPRO_ALLOW_DESTRUCTIVE_DB=on para autorizar o TRUNCATE explicitamente.");
    }

    private static bool AutorizacaoDestrutivaExplicita() =>
        string.Equals(Environment.GetEnvironmentVariable("PLANTAOPRO_ALLOW_DESTRUCTIVE_DB"), "on", StringComparison.OrdinalIgnoreCase);
}
