using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R92 (B5): planos de execução EXPLAIN nos caminhos mais quentes do bloco ADM360 + índices faltantes.
/// Medido no banco de teste com o tenant mais denso (10.172 orçamentos / 17.886 movimentos /
/// 2.623 reservas): o detalhe do orçamento busca reservas por item (N+1) e o join de cirurgias filtra
/// por (tenant_id, origem_tipo, origem_id) — sem índice líder em origem cada acesso fazia seq scan da
/// tabela de reservas; e a listagem padrão de orçamentos (ORDER BY created_at DESC LIMIT/OFFSET)
/// refazia seq scan + top-N sort das linhas do tenant a cada página. A view adm360_saldos foi
/// probeada (LEFT JOIN pré-agregado vs subqueries correlacionadas): equivalência exata, porém o plano
/// ficou pior (2,95 s vs 68 ms) por propagação do predicado do tenant, então a view segue inalterada
/// com o índice parcial ix_adm360_reservas_saldo da R55. Contrato travado: migration v2373 com a DDL
/// exata, manifest com hash real e os dois índices presentes no banco de teste (auto-cura pela
/// reaplicação idempotente sob advisory lock + retry de impasse 40P01, padrão R55/R89).
/// </summary>
public sealed class R92IndizesCaminhosQuentesContratoTests
{
    private const string MigrationFile = "database/migrations/2026_10_v2373_r92_indizes_caminhos_quentes.sql";
    private const string MigrationVersion = "2026_10_v2373_r92_indizes_caminhos_quentes";

    private const string DdlReservasOrigem =
        "CREATE INDEX IF NOT EXISTS ix_adm360_reservas_origem ON plantaopro.adm360_reservas(tenant_id, origem_tipo, origem_id) INCLUDE (produto_id, quantidade) WHERE situacao = 'ATIVA';";

    private const string DdlOrcamentosCriado =
        "CREATE INDEX IF NOT EXISTS ix_adm360_orcamentos_tenant_criado ON plantaopro.adm360_orcamentos(tenant_id, created_at DESC);";

    // Fato 1 - migration com a DDL exata dos dois índices.
    [Fact]
    public void Migration_v2373_tem_a_DDL_exata_dos_dois_indices()
    {
        var root = RepositoryPathResolver.ResolveRoot();
        var sql = File.ReadAllText(Path.Combine(root, MigrationFile));
        Assert.Contains(DdlReservasOrigem, Normalizar(sql));
        Assert.Contains(DdlOrcamentosCriado, Normalizar(sql));
        Assert.StartsWith("-- R92", sql.TrimStart());
    }

    // Fato 2 - manifesto com a entrada v2373 real (hash LF, dependência em v2372, status ativo).
    [Fact]
    public void Manifesto_tem_entrada_v2373_com_hash_real_dependencia_v2372_e_status()
    {
        var root = RepositoryPathResolver.ResolveRoot();
        var sql = File.ReadAllText(Path.Combine(root, MigrationFile));
        var manifest = File.ReadAllText(Path.Combine(root, "database", "migration-manifest.json"));

        using var doc = JsonDocument.Parse(manifest);
        var entrada = doc.RootElement.GetProperty("migrations").EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("version").GetString() == MigrationVersion);
        Assert.NotEqual(JsonValueKind.Undefined, entrada.ValueKind);

        Assert.Equal(MigrationFile.Replace('\\', '/'), entrada.GetProperty("source").GetString());
        Assert.Equal(Sha256Lf(sql), entrada.GetProperty("checksum").GetString());
        Assert.Equal("active", entrada.GetProperty("status").GetString());
        Assert.True(entrada.GetProperty("transactional").GetBoolean());
        var deps = entrada.GetProperty("dependsOn").EnumerateArray().Select(d => d.GetString()).ToArray();
        Assert.Equal(new[] { "2026_10_v2372_r89_vendas_internacionais" }, deps);
    }

    // Fato 3 - índices presentes no banco de teste com a definição exata (auto-cura idempotente).
    [Fact]
    public async Task Indices_existemNoBancoDeTestesComDefinicaoExata()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();

        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r92_indizes_caminhos_quentes'));");
        try
        {
            // v2373 é idempotente (create index if not exists): reaplicar é a auto-cura.
            var sql = File.ReadAllText(Path.Combine(RepositoryPathResolver.ResolveRoot(), MigrationFile));
            await ExecutarMigrationComRetryAsync(cn, sql);

            foreach (var nome in new[] { "ix_adm360_reservas_origem", "ix_adm360_orcamentos_tenant_criado" })
            {
                var ocorrencias = await cn.ExecuteScalarAsync<int>(
                    "SELECT count(*) FROM pg_indexes WHERE schemaname='plantaopro' AND indexname=@nome;",
                    new { nome });
                Assert.Equal(1, ocorrencias);
            }

            var defReservas = await cn.ExecuteScalarAsync<string>(
                "SELECT pg_get_indexdef('plantaopro.ix_adm360_reservas_origem'::regclass);");
            Assert.Contains("USING btree (tenant_id, origem_tipo, origem_id)", defReservas);
            Assert.Contains("INCLUDE (produto_id, quantidade)", defReservas);
            Assert.Contains("situacao", defReservas);
            Assert.Contains("ATIVA", defReservas);

            var defOrcamentos = await cn.ExecuteScalarAsync<string>(
                "SELECT pg_get_indexdef('plantaopro.ix_adm360_orcamentos_tenant_criado'::regclass);");
            Assert.Contains("USING btree (tenant_id, created_at DESC)", defOrcamentos);
        }
        finally
        {
            await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r92_indizes_caminhos_quentes'));");
        }
    }

    private static string Sha256Lf(string conteudo) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(conteudo.Replace("\r\n", "\n")))).ToLowerInvariant();

    /// <summary>
    /// Executa a migration tolerando impasse (40P01): DDL de catálogo pode conflitar com sessões de
    /// teste que escrevem nas tabelas adm360 em paralelo; quando o Postgres escolhe esta sessão como
    /// vítima, basta repetir (padrão R55).
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
