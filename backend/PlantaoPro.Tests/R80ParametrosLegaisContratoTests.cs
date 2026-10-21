using System.Security.Cryptography;
using System.Text.Json;
using Dapper;
using Npgsql;
using PlantaoPro.Application.Legislacao;
using PlantaoPro.Infrastructure.Legislacao;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R80 (bloque B4 - legislacao brasileira): parametros_legais como fonte unica de
/// parametros legais (valor, fonte oficial citavel, vigencia, ano). Plano r66a0 R80.
/// 1) Manifesto: entrada v2363 presente com checksum real (SHA256 do SQL com CRLF-&gt;LF),
///    dependencia em v2362 e status ativo.
/// 2) Banco: seed idempotente reexecutada e valores oficiais 2026 conferidos linha a
///    linha (salario minimo 1518.00; piso IRRF derivado = 3 x minimo; alíquota FGTS
///    8.00; dias DAS 20 / DCTFWeb 15 / FGTS 20) sem duplicata em (codigo, ano).
/// 3) Repositorio: filtro por ano, ultima linha por codigo e fonte oficial obrigatoria.
/// </summary>
public sealed class R80ParametrosLegaisContratoTests
{
    private const string MigrationFile = "database/migrations/2026_10_v2363_r80_parametros_legais.sql";
    private const string MigrationVersion = "2026_10_v2363_r80_parametros_legais";

    private static readonly string[] CodigosSemeados =
    {
        "SALARIO_MINIMO", "PISO_ISENCAO_IRRF_MENSAL", "FGTS_ALIQUOTA_MENSAL",
        "FGTS_VENCIMENTO_DIA", "DAS_SIMPLES_VENCIMENTO_DIA", "DCTFWEB_VENCIMENTO_DIA"
    };

    private static string Root => RepositoryPathResolver.ResolveRoot();
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    private static string Sha256Lf(string conteudo) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(conteudo.Replace("\r\n", "\n")))).ToLowerInvariant();

    [Fact]
    public void Manifesto_tem_entrada_v2363_com_hash_real_dependencia_e_status()
    {
        var sql = Read(MigrationFile);
        var manifest = Read("database/migration-manifest.json");

        using var doc = JsonDocument.Parse(manifest);
        var entrada = doc.RootElement.GetProperty("migrations").EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("version").GetString() == MigrationVersion);
        Assert.NotEqual(JsonValueKind.Undefined, entrada.ValueKind);

        Assert.Equal(MigrationFile.Replace('\\', '/'), entrada.GetProperty("source").GetString());
        Assert.Equal(Sha256Lf(sql), entrada.GetProperty("checksum").GetString());
        Assert.Equal("active", entrada.GetProperty("status").GetString());
        Assert.True(entrada.GetProperty("transactional").GetBoolean());
        var deps = entrada.GetProperty("dependsOn").EnumerateArray().Select(d => d.GetString()).ToArray();
        Assert.Equal(new[] { "2026_10_v2362_r72_anexos_arquivos" }, deps);

        // A tabela e cada codigo oficial da seed estao presentes no SQL versionado.
        Assert.Contains("create table if not exists plantaopro.parametros_legais", sql);
        foreach (var codigo in CodigosSemeados)
            Assert.Contains(codigo, sql);
    }

    /// <summary>Advisory lock serializa a aplicacao; probe idempotente na tabela (padrao R72).</summary>
    private static async Task GarantirSchemaAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r80_parametros_legais_schema'));");
        try
        {
            var tem = await cn.ExecuteScalarAsync<bool>(
                "select exists(select 1 from information_schema.tables where table_schema='plantaopro' and table_name='parametros_legais')");
            if (!tem)
            {
                var sql = Read(MigrationFile);
                for (var tentativa = 1; ; tentativa++)
                {
                    try { await cn.ExecuteAsync(sql); break; }
                    catch (PostgresException ex) when (ex.SqlState == "40P01" && tentativa < 5) { await Task.Delay(150 * tentativa); }
                }
            }
        }
        finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r80_parametros_legais_schema'));"); }
    }

    [Fact]
    public async Task Seed_idempotente_e_valores_oficiais_2026_conferidos_no_banco()
    {
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();

        // Reexecutar a migration nao duplica (idempotencia exigida pelo padrao da casa).
        await cn.ExecuteAsync(Read(MigrationFile));
        var duplicadas = await cn.ExecuteScalarAsync<int>(
            "select count(*) from (select codigo, ano from plantaopro.parametros_legais where reg_status='A' group by codigo, ano having count(*) > 1) d");
        Assert.Equal(0, duplicadas);

        async Task<string> Valor(string codigo) =>
            (await cn.QuerySingleAsync<string>("select valor from plantaopro.parametros_legais where codigo=@c and ano=2026 and reg_status='A'", new { c = codigo }));

        Assert.Equal("1518.00", await Valor("SALARIO_MINIMO"));
        Assert.Equal("4554.00", await Valor("PISO_ISENCAO_IRRF_MENSAL")); // 3 x 1518.00
        Assert.Equal("8.00", await Valor("FGTS_ALIQUOTA_MENSAL"));
        Assert.Equal("20", await Valor("FGTS_VENCIMENTO_DIA"));
        Assert.Equal("20", await Valor("DAS_SIMPLES_VENCIMENTO_DIA"));
        Assert.Equal("15", await Valor("DCTFWEB_VENCIMENTO_DIA"));

        // Fonte oficial citavel obrigatoria em toda linha (requisito do plano r66a0 R80).
        var semFonte = await cn.ExecuteScalarAsync<int>(
            "select count(*) from plantaopro.parametros_legais where reg_status='A' and (fonte_oficial is null or btrim(fonte_oficial) = '')");
        Assert.Equal(0, semFonte);

        // Vigencia anual apenas nos parametros de exercicio; fixos ficam nulos.
        var vigentes = await cn.ExecuteScalarAsync<int>(
            "select count(*) from plantaopro.parametros_legais where codigo='SALARIO_MINIMO' and ano=2026 and vigencia_inicio='2026-01-01'::date and vigencia_fim='2026-12-31'::date");
        Assert.Equal(1, vigentes);
        var fixosSemVigencia = await cn.ExecuteScalarAsync<int>(
            "select count(*) from plantaopro.parametros_legais where codigo in ('FGTS_ALIQUOTA_MENSAL','DAS_SIMPLES_VENCIMENTO_DIA','DCTFWEB_VENCIMENTO_DIA') and ano=2026 and vigencia_inicio is null and vigencia_fim is null");
        Assert.Equal(3, fixosSemVigencia);
    }

    [Fact]
    public async Task Repositorio_filtra_por_ano_localiza_por_codigo_com_fonte_e_vigencia_iso()
    {
        await GarantirSchemaAsync();
        var repo = new ParametrosLegaisRepository(TestDatabase.ConnectionString);

        var lista2026 = await repo.ListarAtivosAsync(2026);
        Assert.Contains(lista2026, p => p.Codigo == "SALARIO_MINIMO" && p.Ano == 2026 && p.Valor == "1518.00");
        Assert.All(lista2026, p => Assert.False(string.IsNullOrWhiteSpace(p.FonteOficial)));
        Assert.All(lista2026, p => Assert.Equal(2026, p.Ano));

        // Vigencia em ISO YYYY-MM-DD (ou "" quando o parametro nao tem vigencia).
        var minimo = lista2026.Single(p => p.Codigo == "SALARIO_MINIMO");
        Assert.Equal("2026-01-01", minimo.VigenciaInicio);
        Assert.Equal("2026-12-31", minimo.VigenciaFim);
        var das = lista2026.Single(p => p.Codigo == "DAS_SIMPLES_VENCIMENTO_DIA");
        Assert.Equal("", das.VigenciaInicio);

        var item = await repo.ObterPorCodigoAsync("DAS_SIMPLES_VENCIMENTO_DIA", null);
        Assert.NotNull(item);
        Assert.Equal("20", item!.Valor);
        Assert.NotNull(item.FonteOficial);

        Assert.Null(await repo.ObterPorCodigoAsync("CODIGO_INEXISTENTE_R80", 2026));
        Assert.Null(await repo.ObterPorCodigoAsync("SALARIO_MINIMO", 1999));
    }
}
