using System;
using System.Linq;
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
/// R83 (bloco B4 - legislacao brasileira): Simples Nacional. Contrato de vetores oficiais
/// travado em exemplos reais: faixas cumulativas dos anexos I a V (LC 123/2006 redacao LC
/// 155/2016), classe de receita ME/EPP pelos tetos da fonte unica, alíquota efetiva =
/// (RBT12 x aliquota - parcela) / RBT12 (art. 18 paragrafos 1o, 1o-A e 3o) e vencimento do
/// DAS no dia oficial do mes seguinte a competencia. Todos os numeros legais leem da fonte
/// unica parametros_legais.
/// </summary>
public sealed class R83SimplesContratoTests
{
    private const string MigrationV2363 = "database/migrations/2026_10_v2363_r80_parametros_legais.sql";
    private const string MigrationV2366 = "database/migrations/2026_10_v2366_r83_simples_nacional_2026.sql";
    private const string MigrationVersion = "2026_10_v2366_r83_simples_nacional_2026";

    // Tabela oficial travada (anexo I a V x faixa 1 a 6): "limite|aliquota_pct|parcela_a_deduzir".
    private static readonly string[][] EsperadoFaixas =
    {
        new[] { "180000.00|4.00|0.00", "360000.00|7.30|5940.00", "720000.00|9.50|13860.00", "1800000.00|10.70|22500.00", "3600000.00|14.30|87300.00", "4800000.00|19.00|378000.00" },
        new[] { "180000.00|4.50|0.00", "360000.00|7.80|5940.00", "720000.00|10.00|13860.00", "1800000.00|11.20|22500.00", "3600000.00|14.70|85500.00", "4800000.00|30.00|720000.00" },
        new[] { "180000.00|6.00|0.00", "360000.00|11.20|9360.00", "720000.00|13.50|17640.00", "1800000.00|16.00|35640.00", "3600000.00|21.00|125640.00", "4800000.00|33.00|648000.00" },
        new[] { "180000.00|4.50|0.00", "360000.00|9.00|8100.00", "720000.00|10.20|12420.00", "1800000.00|14.00|39780.00", "3600000.00|22.00|183780.00", "4800000.00|33.00|828000.00" },
        new[] { "180000.00|15.50|0.00", "360000.00|18.00|4500.00", "720000.00|19.50|9900.00", "1800000.00|20.50|17100.00", "3600000.00|23.00|62100.00", "4800000.00|30.50|540000.00" }
    };

    private static string Root => RepositoryPathResolver.ResolveRoot();
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    private static string Sha256Lf(string conteudo) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(conteudo.Replace("\r\n", "\n")))).ToLowerInvariant();

    private static async Task GarantirSchemaAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r83_simples_schema'));");
        try
        {
            var temTabela = await cn.ExecuteScalarAsync<bool>(
                "select exists(select 1 from information_schema.tables where table_schema='plantaopro' and table_name='parametros_legais')");
            if (!temTabela)
            {
                for (var tentativa = 1; ; tentativa++)
                {
                    try { await cn.ExecuteAsync(Read(MigrationV2363)); break; }
                    catch (PostgresException ex) when (ex.SqlState == "40P01" && tentativa < 5) { await Task.Delay(150 * tentativa); }
                }
            }
            // v2366 é idempotente: reexecução não duplica (on conflict do nothing).
            await cn.ExecuteAsync(Read(MigrationV2366));
        }
        finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r83_simples_schema'));"); }
    }

    private static ISimplesNacionalService Servico()
    {
        var repo = new ParametrosLegaisRepository(TestDatabase.ConnectionString);
        return new SimplesNacionalService(repo);
    }

    private static decimal Lim(FaixaTributoLegalDto faixa) =>
        decimal.Parse(faixa.LimiteSuperior, System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public void Manifesto_tem_entrada_v2366_com_hash_real_dependencia_e_status()
    {
        var sql = Read(MigrationV2366);
        var manifest = Read("database/migration-manifest.json");

        using var doc = JsonDocument.Parse(manifest);
        var entrada = doc.RootElement.GetProperty("migrations").EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("version").GetString() == MigrationVersion);
        Assert.NotEqual(JsonValueKind.Undefined, entrada.ValueKind);

        Assert.Equal(MigrationV2366.Replace('\\', '/'), entrada.GetProperty("source").GetString());
        Assert.Equal(Sha256Lf(sql), entrada.GetProperty("checksum").GetString());
        Assert.Equal("active", entrada.GetProperty("status").GetString());
        Assert.True(entrada.GetProperty("transactional").GetBoolean());
        var deps = entrada.GetProperty("dependsOn").EnumerateArray().Select(d => d.GetString()).ToArray();
        Assert.Equal(new[] { "2026_10_v2365_r82_parametros_folha_2026" }, deps);

        for (var anexo = 1; anexo <= 5; anexo++)
            for (var faixa = 1; faixa <= 6; faixa++)
                Assert.Contains($"SIMPLES_ANEXO_{anexo}_FAIXA_{faixa}", sql);
        foreach (var codigo in new[] { "SIMPLES_LIMITE_ME_RECEITA_ANUAL", "SIMPLES_LIMITE_EPP_RECEITA_ANUAL", "SIMPLES_DAS_DIA_VENCIMENTO" })
            Assert.Contains(codigo, sql);
    }

    [Fact]
    public async Task Tabelas_oficiais_do_simples_semeadas_na_fonte_unica_com_fonte_citavel()
    {
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();

        async Task<string> Valor(string codigo) =>
            (await cn.QuerySingleAsync<string>("select valor from plantaopro.parametros_legais where codigo=@c and ano=2026 and reg_status='A'", new { c = codigo }));

        for (var anexo = 0; anexo < EsperadoFaixas.Length; anexo++)
            for (var faixa = 0; faixa < EsperadoFaixas[anexo].Length; faixa++)
                Assert.Equal(EsperadoFaixas[anexo][faixa], await Valor($"SIMPLES_ANEXO_{anexo + 1}_FAIXA_{faixa + 1}"));

        Assert.Equal("360000.00", await Valor("SIMPLES_LIMITE_ME_RECEITA_ANUAL"));
        Assert.Equal("4800000.00", await Valor("SIMPLES_LIMITE_EPP_RECEITA_ANUAL"));
        Assert.Equal("20", await Valor("SIMPLES_DAS_DIA_VENCIMENTO"));

        var semFonte = await cn.ExecuteScalarAsync<int>(
            "select count(*) from plantaopro.parametros_legais where ano=2026 and reg_status='A' and codigo like 'SIMPLES\\_%' escape '\\' and (fonte_oficial is null or btrim(fonte_oficial)='')");
        Assert.Equal(0, semFonte);

        // Regras fixas da lei (tabelas e tetos): todas com vigencia nula na fonte unica.
        var fixos = await cn.ExecuteScalarAsync<int>(
            "select count(*) from plantaopro.parametros_legais where ano=2026 and reg_status='A' and codigo like 'SIMPLES\\_%' escape '\\' and vigencia_inicio is null and vigencia_fim is null");
        Assert.Equal(33, fixos);
    }

    [Fact]
    public async Task Tabela_do_servico_exibe_cinco_anexos_com_faixas_cumulativas_em_ordem()
    {
        await GarantirSchemaAsync();
        var servico = Servico();

        var tabela = await servico.ObterTabelaAsync(null);
        Assert.Equal(2026, tabela.Ano);
        Assert.Equal(360000.00m, tabela.LimiteMeReceitaAnual);
        Assert.Equal(4800000.00m, tabela.LimiteEppReceitaAnual);
        Assert.Equal(20, tabela.DasDiaVencimento);
        Assert.Equal(5, tabela.Anexos.Count);

        var decimais = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;
        foreach (var anexo in tabela.Anexos)
        {
            Assert.Equal(6, anexo.Faixas.Count);
            var limiteAnterior = 0m;
            for (var i = 0; i < anexo.Faixas.Count; i++)
            {
                var esperado = EsperadoFaixas[anexo.Numero - 1][i].Split('|');
                Assert.Equal(decimal.Parse(esperado[0], decimais), Lim(anexo.Faixas[i]));
                Assert.Equal(decimal.Parse(esperado[1], decimais), anexo.Faixas[i].AliquotaPercentual);
                Assert.Equal(decimal.Parse(esperado[2], decimais), anexo.Faixas[i].ParcelaADeduzir);
                Assert.True(Lim(anexo.Faixas[i]) > limiteAnterior);
                limiteAnterior = Lim(anexo.Faixas[i]);
            }
            Assert.Equal(4800000.00m, Lim(anexo.Faixas[^1]));
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => servico.ObterTabelaAsync(1999));
    }

    [Fact]
    public async Task Faixa_cumulativa_e_aliquota_efetiva_batem_com_o_oficial_em_exemplos_reais()
    {
        await GarantirSchemaAsync();
        var servico = Servico();

        // Clínica de serviços: RBT12 720.000 (teto da faixa 3 inclusivo), mês 60.000.
        var servicos = await servico.GerarPreviaDasAsync(new PreviaDasCommand(3, 7, 2026, 720000.00m, 60000.00m, null));
        Assert.Equal("EPP", servicos.ClasseReceita);
        Assert.Equal(3, servicos.FaixaNumero);
        Assert.Equal(720000.00m, servicos.FaixaLimiteSuperior);
        Assert.Equal(13.50m, servicos.AliquotaFaixaPct);
        Assert.Equal(17640.00m, servicos.ParcelaADeduzir);
        Assert.Equal(11.05m, servicos.AliquotaEfetivaPct);
        Assert.False(servicos.ReceitaAcumuladaEstimada);
        Assert.Equal(6630.00m, servicos.ValorDas);

        // Comércio por revenda: RBT12 200.000 dentro da faixa 2, mês 18.750.
        var comercio = await servico.GerarPreviaDasAsync(new PreviaDasCommand(1, 7, 2026, 200000.00m, 18750.00m, null));
        Assert.Equal("ME", comercio.ClasseReceita);
        Assert.Equal(2, comercio.FaixaNumero);
        Assert.Equal(360000.00m, comercio.FaixaLimiteSuperior);
        Assert.Equal(7.30m, comercio.AliquotaFaixaPct);
        Assert.Equal(5940.00m, comercio.ParcelaADeduzir);
        Assert.Equal(4.33m, comercio.AliquotaEfetivaPct);
        Assert.Equal(811.88m, comercio.ValorDas);

        // Limite inclusivo: RBT12 exatamente 180.000 fica na faixa 1 (alíquota 4%).
        var borda = await servico.GerarPreviaDasAsync(new PreviaDasCommand(1, 7, 2026, 180000.00m, 15000.00m, null));
        Assert.Equal(1, borda.FaixaNumero);
        Assert.Equal(4.00m, borda.AliquotaFaixaPct);
        Assert.Equal(0.00m, borda.ParcelaADeduzir);
        Assert.Equal(4.00m, borda.AliquotaEfetivaPct);
        Assert.Equal(600.00m, borda.ValorDas);
    }

    [Fact]
    public async Task Empresa_nova_estima_acumulado_e_faixa_alta_mantem_a_formula_efetiva()
    {
        await GarantirSchemaAsync();
        var servico = Servico();

        // Sem acumulado informado: estima 12x o mês (flag no resultado) e aplica a faixa certa.
        var nova = await servico.GerarPreviaDasAsync(new PreviaDasCommand(3, 7, 2026, null, 10000.00m, null));
        Assert.True(nova.ReceitaAcumuladaEstimada);
        Assert.Equal(120000.00m, nova.ReceitaBrutaAcumulada12Meses);
        Assert.Equal(1, nova.FaixaNumero);
        Assert.Equal(6.00m, nova.AliquotaEfetivaPct);
        Assert.Equal(600.00m, nova.ValorDas);

        // Indústria na faixa 5: efetiva = (2.400.000 x 14,7% - 85.500) / 2.400.000 = 11,1375%.
        var industria = await servico.GerarPreviaDasAsync(new PreviaDasCommand(2, 7, 2026, 2400000.00m, 200000.00m, null));
        Assert.Equal(5, industria.FaixaNumero);
        Assert.Equal(3600000.00m, industria.FaixaLimiteSuperior);
        Assert.Equal(14.70m, industria.AliquotaFaixaPct);
        Assert.Equal(85500.00m, industria.ParcelaADeduzir);
        Assert.Equal(11.1375m, industria.AliquotaEfetivaPct);
        Assert.Equal(22275.00m, industria.ValorDas);
    }

    [Fact]
    public async Task Vencimento_do_das_sai_no_dia_oficial_do_mes_seguinte_a_competencia()
    {
        await GarantirSchemaAsync();
        var servico = Servico();

        // Competência dezembro/2026: o vencimento cai em janeiro/2027.
        var dezembro = await servico.GerarPreviaDasAsync(new PreviaDasCommand(3, 12, 2026, 720000.00m, 60000.00m, null));
        Assert.Equal(20, dezembro.DasDiaVencimento);
        Assert.Equal(1, dezembro.VencimentoMes);
        Assert.Equal(2027, dezembro.VencimentoAno);
        Assert.Equal(6630.00m, dezembro.ValorDas);

        // Consultoria na faixa 1 (RBT12 90.000): alíquota plana 15,5%; competência junho/2026.
        var consultoria = await servico.GerarPreviaDasAsync(new PreviaDasCommand(5, 6, 2026, 90000.00m, 7500.00m, null));
        Assert.Equal(1, consultoria.FaixaNumero);
        Assert.Equal(15.50m, consultoria.AliquotaEfetivaPct);
        Assert.Equal(1162.50m, consultoria.ValorDas);
        Assert.Equal(20, consultoria.DasDiaVencimento);
        Assert.Equal(7, consultoria.VencimentoMes);
        Assert.Equal(2026, consultoria.VencimentoAno);
    }

    [Fact]
    public async Task Validacoes_do_simples_sao_erros_explicitos()
    {
        await GarantirSchemaAsync();
        var servico = Servico();

        await Assert.ThrowsAsync<ArgumentException>(() => servico.GerarPreviaDasAsync(new PreviaDasCommand(0, 7, 2026, 1000.00m, 1000.00m, null)));
        await Assert.ThrowsAsync<ArgumentException>(() => servico.GerarPreviaDasAsync(new PreviaDasCommand(6, 7, 2026, 1000.00m, 1000.00m, null)));
        await Assert.ThrowsAsync<ArgumentException>(() => servico.GerarPreviaDasAsync(new PreviaDasCommand(3, 0, 2026, 1000.00m, 1000.00m, null)));
        await Assert.ThrowsAsync<ArgumentException>(() => servico.GerarPreviaDasAsync(new PreviaDasCommand(3, 13, 2026, 1000.00m, 1000.00m, null)));
        await Assert.ThrowsAsync<ArgumentException>(() => servico.GerarPreviaDasAsync(new PreviaDasCommand(3, 7, 2026, -1.00m, 1000.00m, null)));
        await Assert.ThrowsAsync<ArgumentException>(() => servico.GerarPreviaDasAsync(new PreviaDasCommand(3, 7, 2026, 1000.00m, 0.00m, null)));
        // Acima do teto EPP a empresa ultrapassa o regime.
        await Assert.ThrowsAsync<ArgumentException>(() => servico.GerarPreviaDasAsync(new PreviaDasCommand(3, 7, 2026, 4800000.01m, 1000.00m, null)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => servico.GerarPreviaDasAsync(new PreviaDasCommand(3, 7, 2026, 1000.00m, 1000.00m, 1999)));
    }
}
