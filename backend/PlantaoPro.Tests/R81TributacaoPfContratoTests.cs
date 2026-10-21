using Dapper;
using Npgsql;
using PlantaoPro.Application.Legislacao;
using PlantaoPro.Infrastructure.Legislacao;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R81 (bloque B4 - legislacao brasileira): nucleo de tributacao PF 2026 com vetores
/// oficiais na fonte unica. Contrato de vetores travado em exemplo concreto:
/// INSS progressivo por "parcela a deduzir" (Portaria Int. MPS/MF n. 6/2025, ancorada
/// ao minimo 1.518,00 do plano r66a0) + IRRF mensal (tabela RFB vigente com esse minimo)
/// + piso de isencao lido da fonte unica (PISO_ISENCAO_IRRF_MENSAL, 3x minimo).
/// Os vetores do calculo NUNCA vivem no codigo: o servico carrega tudo de parametros_legais.
/// </summary>
public sealed class R81TributacaoPfContratoTests
{
    private const string MigrationV2363 = "database/migrations/2026_10_v2363_r80_parametros_legais.sql";
    private const string MigrationV2364 = "database/migrations/2026_10_v2364_r81_tabelas_pf_2026.sql";

    private static string Root => RepositoryPathResolver.ResolveRoot();
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    /// <summary>Advisory lock proprio; garante tabela (v2363) e semeadura das tabelas PF (v2364).</summary>
    private static async Task GarantirSchemaAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r81_tabelas_pf_schema'));");
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
            // v2364 e idempotente: reexecucao nao duplica (on conflict do nothing).
            await cn.ExecuteAsync(Read(MigrationV2364));
        }
        finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r81_tabelas_pf_schema'));"); }
    }

    private static ITributacaoPfService Servico() =>
        new TributacaoPfService(new ParametrosLegaisRepository(TestDatabase.ConnectionString));

    [Fact]
    public async Task Vetores_oficiais_2026_semeados_na_fonte_unica_com_fonte_citavel()
    {
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();

        async Task<string> Valor(string codigo) =>
            (await cn.QuerySingleAsync<string>("select valor from plantaopro.parametros_legais where codigo=@c and ano=2026 and reg_status='A'", new { c = codigo }));

        Assert.Equal("8157.41", await Valor("INSS_TETO_CONTRIBUICAO"));
        Assert.Equal("1518.00|7.5|0.00", await Valor("INSS_FAIXA_1"));
        Assert.Equal("2793.88|9.0|22.77", await Valor("INSS_FAIXA_2"));
        Assert.Equal("4190.83|12.0|106.59", await Valor("INSS_FAIXA_3"));
        Assert.Equal("8157.41|14.0|190.40", await Valor("INSS_FAIXA_4"));
        Assert.Equal("2826.65|7.5|169.44", await Valor("IRRF_FAIXA_1"));
        Assert.Equal("3751.05|15.0|381.44", await Valor("IRRF_FAIXA_2"));
        Assert.Equal("4664.68|22.5|662.77", await Valor("IRRF_FAIXA_3"));
        Assert.Equal("|27.5|896.00", await Valor("IRRF_FAIXA_4"));
        // Piso (R80) continua a unica fonte da isencao.
        Assert.Equal("4554.00", await Valor("PISO_ISENCAO_IRRF_MENSAL"));

        var semFonte = await cn.ExecuteScalarAsync<int>(
            "select count(*) from plantaopro.parametros_legais where ano=2026 and reg_status='A' and (codigo like '%FAIXA_%' or codigo = 'INSS_TETO_CONTRIBUICAO') and (fonte_oficial is null or btrim(fonte_oficial)='')");
        Assert.Equal(0, semFonte);
    }

    [Fact]
    public async Task Contrato_de_vetores_oficiais_exemplos_travados_linha_a_linha()
    {
        await GarantirSchemaAsync();
        var servico = Servico();

        async Task<CalculoTributacaoPfDto> Calcular(decimal bruto) => await servico.CalcularAsync(null, bruto);

        // Salario minimo: INSS 7,5% puro; IRRF isento (base 1.404,15 abaixo do piso).
        var minimo = await Calcular(1518.00m);
        Assert.Equal(2026, minimo.Ano);
        Assert.Equal(1518.00m, minimo.SalarioMinimo);
        Assert.Equal(8157.41m, minimo.TetoInss);
        Assert.Equal(1518.00m, minimo.BaseInss);
        Assert.Equal(113.85m, minimo.Inss);
        Assert.Equal(1404.15m, minimo.BaseIrrf);
        Assert.Equal(4554.00m, minimo.PisoIrrfMensal);
        Assert.Equal(0.00m, minimo.Irrf);
        Assert.Equal(113.85m, minimo.TotalDescontos);
        Assert.Equal(1404.15m, minimo.SalarioLiquido);

        // Limite exato da faixa 2 do INSS: 9% x 2.793,88 - 22,77 = 228,68.
        var limiteFaixa2 = await Calcular(2793.88m);
        Assert.Equal(228.68m, limiteFaixa2.Inss);
        Assert.Equal(0.00m, limiteFaixa2.Irrf);
        Assert.Equal(2565.20m, limiteFaixa2.SalarioLiquido);

        // 5.000: INSS faixa final 509,60; base IRRF 4.490,40 ainda abaixo do piso.
        var meio = await Calcular(5000.00m);
        Assert.Equal(509.60m, meio.Inss);
        Assert.Equal(4490.40m, meio.BaseIrrf);
        Assert.Equal(0.00m, meio.Irrf);
        Assert.Equal(4490.40m, meio.SalarioLiquido);

        // Teto exato: INSS 951,64; IRRF 27,5% sobre 7.205,77 - 896,00 = 1.085,59.
        var teto = await Calcular(8157.41m);
        Assert.Equal(8157.41m, teto.BaseInss);
        Assert.Equal(951.64m, teto.Inss);
        Assert.Equal(7205.77m, teto.BaseIrrf);
        Assert.Equal(1085.59m, teto.Irrf);
        Assert.Equal(2037.23m, teto.TotalDescontos);
        Assert.Equal(6120.18m, teto.SalarioLiquido);

        // Acima do teto: INSS travado em 951,64; IRRF cresce 27,5% - 896,00.
        var alto = await Calcular(12000.00m);
        Assert.Equal(8157.41m, alto.BaseInss);
        Assert.Equal(951.64m, alto.Inss);
        Assert.Equal(11048.36m, alto.BaseIrrf);
        Assert.Equal(2142.30m, alto.Irrf);
        Assert.Equal(3093.94m, alto.TotalDescontos);
        Assert.Equal(8906.06m, alto.SalarioLiquido);

        // A tabela exposta bate com os vetores oficiais (fonte unica).
        var tabela = await servico.ObterTabelaAsync(null);
        Assert.Equal(1518.00m, tabela.SalarioMinimo);
        Assert.Equal(4554.00m, tabela.PisoIrrfMensal);
        Assert.Equal(8157.41m, tabela.TetoInss);
        Assert.Equal(4, tabela.FaixasInss.Count);
        Assert.Equal(4, tabela.FaixasIrrf.Count);
        Assert.Equal(14.0m, tabela.FaixasInss[^1].AliquotaPercentual);
        Assert.Equal(896.00m, tabela.FaixasIrrf[^1].ParcelaADeduzir);
        Assert.Equal("", tabela.FaixasIrrf[^1].LimiteSuperior);
    }

    [Fact]
    public async Task Validacao_de_salario_e_ano_sem_vetores_sao_erro_explicito()
    {
        await GarantirSchemaAsync();
        var servico = Servico();

        await Assert.ThrowsAsync<ArgumentException>(() => servico.CalcularAsync(null, 0m));
        await Assert.ThrowsAsync<ArgumentException>(() => servico.CalcularAsync(null, -5.00m));
        await Assert.ThrowsAsync<InvalidOperationException>(() => servico.CalcularAsync(1999, 1000m));
        await Assert.ThrowsAsync<InvalidOperationException>(() => servico.ObterTabelaAsync(1999));
    }
}
