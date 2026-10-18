using Dapper;
using Npgsql;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R55 — Latência do Estoque (view adm360_saldos).
/// Causa raiz medida na R54: a view executa duas vezes por grupo de saldo a subquery correlacionada
/// sobre adm360_reservas (reservado e disponivel) e a tabela semia nenhum índice cobrindo o padrão
/// (tenant_id, produto_id, lote_id, local_id) com situacao='ATIVA' — com 7.831 grupos do tenant demo,
/// cada query pesou 5,3–5,9 s. Contrato travado aqui: migration v2354 com a DDL exata, os dois
/// scripts canônicos sincronizados e o índice presente no banco de teste (com auto-cura pela
/// reaplicação idempotente da migration sob advisory lock + retry de impasse 40P01, padrão Bloco7).
/// </summary>
public sealed class R55EstoqueLatenciaContratoTests
{
    private const string MigrationFile = "2026_10_v2354_r55_indice_reservas_saldo.sql";

    private const string DdlEsperada = "CREATE INDEX IF NOT EXISTS ix_adm360_reservas_saldo ON plantaopro.adm360_reservas(tenant_id,produto_id,lote_id,local_id) INCLUDE(quantidade) WHERE situacao='ATIVA';";

    [Fact]
    public void Migration_v2354_ExisteComADdlExataDoIndice()
    {
        var path = RepositoryPathResolver.DatabaseFile("migrations", MigrationFile);
        Assert.True(File.Exists(path), $"Migration v2354 não encontrada em {path}.");
        Assert.Contains(DdlEsperada, Normalizar(File.ReadAllText(path)));
    }

    [Fact]
    public void ScriptsCanonicos_MantemADdlDoIndiceSincronizada()
    {
        var completo = File.ReadAllText(RepositoryPathResolver.DatabaseFile("scrpt_completo.sql"));
        var instalador = File.ReadAllText(RepositoryPathResolver.DatabaseFile("pgadmin", "instalar_no_banco_atual.sql"));
        Assert.Contains(DdlEsperada, Normalizar(completo));
        Assert.Contains(DdlEsperada, Normalizar(instalador));
    }

    [Fact]
    public async Task Indice_ReservasSaldo_ExisteNoBancoDeTestesComColunasExatas()
    {
        var cs = TestDatabase.ConnectionString;
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();

        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r55_ix_adm360_reservas_saldo'));");
        try
        {
            var sql = File.ReadAllText(RepositoryPathResolver.DatabaseFile("migrations", MigrationFile));
            await ExecutarMigrationComRetryAsync(cn, sql);

            var ocorrencias = await cn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM pg_indexes " +
                "WHERE schemaname='plantaopro' AND tablename='adm360_reservas' AND indexname='ix_adm360_reservas_saldo';");
            Assert.Equal(1, ocorrencias);

            // Definição canônica do índice (pg_get_indexdef): colunas-chave em ordem, INCLUDE e
            // condição parcial — sem depender de introspecção de pg_index.indkey (int2vector).
            var definicao = await cn.ExecuteScalarAsync<string>(
                "SELECT pg_get_indexdef('plantaopro.ix_adm360_reservas_saldo'::regclass);");
            Assert.Contains("USING btree (tenant_id, produto_id, lote_id, local_id)", definicao);
            Assert.Contains("INCLUDE (quantidade)", definicao);
            Assert.Contains("WHERE", definicao);
            Assert.Contains("situacao", definicao);
            Assert.Contains("ATIVA", definicao);
        }
        finally
        {
            await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r55_ix_adm360_reservas_saldo'));");
        }
    }

    /// <summary>
    /// Executa a migration tolerando impasse (40P01): DDL de catálogo pode conflitar com sessões de
    /// teste que escrevem nas tabelas adm360 em paralelo; quando o Postgres escolhe esta sessão como
    /// vítima, basta repetir (padrão do Bloco7).
    /// </summary>
    private static async Task ExecutarMigrationComRetryAsync(NpgsqlConnection cn, string sql)
    {
        for (var tentativa = 1; ; tentativa++)
        {
            try
            {
                await cn.ExecuteAsync(sql);
                return;
            }
            catch (PostgresException ex) when (ex.SqlState == "40P01" && tentativa < 5)
            {
                await Task.Delay(150 * tentativa);
            }
        }
    }

    private static string Normalizar(string sql) =>
        string.Join(" ", sql.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
}
