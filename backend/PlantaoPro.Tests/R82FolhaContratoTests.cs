using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Dapper;
using Npgsql;
using PlantaoPro.Application.Folha;
using PlantaoPro.Application.Legislacao;
using PlantaoPro.Infrastructure.Folha;
using PlantaoPro.Infrastructure.Legislacao;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R82 (bloque B4 - legislacao brasileira): folha de pagamento PF em linguagem leiga.
/// Contrato de vetores oficiais travado em exemplo concreto para os quatro simuladores
/// (mes com HE/dependentes/simplificada, 13o proporcional, ferias + terco, rescisao sem
/// justa causa). Regras travadas: meses contam cheios a partir de 15 dias; HE +50% sobre
/// (base / 220h); 13o isento ate 2x minimo na base do IRRF; rescisao tributa evento unico
/// consolidado + FGTS 8% + multa 40%. Todos os parametros legais leem da fonte unica.
/// </summary>
public sealed class R82FolhaContratoTests
{
    private const string MigrationV2363 = "database/migrations/2026_10_v2363_r80_parametros_legais.sql";
    private const string MigrationV2364 = "database/migrations/2026_10_v2364_r81_tabelas_pf_2026.sql";
    private const string MigrationV2365 = "database/migrations/2026_10_v2365_r82_parametros_folha_2026.sql";
    private const string MigrationVersion = "2026_10_v2365_r82_parametros_folha_2026";

    private static string Root => RepositoryPathResolver.ResolveRoot();
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    private static string Sha256Lf(string conteudo) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(conteudo.Replace("\r\n", "\n")))).ToLowerInvariant();

    private static async Task GarantirSchemaAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r82_folha_schema'));");
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
            // v2364/v2365 sao idempotentes: reexecucao nao duplica (on conflict do nothing).
            await cn.ExecuteAsync(Read(MigrationV2364));
            await cn.ExecuteAsync(Read(MigrationV2365));
        }
        finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r82_folha_schema'));"); }
    }

    private static IFolhaPfService Servico()
    {
        var repo = new ParametrosLegaisRepository(TestDatabase.ConnectionString);
        return new FolhaPfService(repo, new TributacaoPfService(repo));
    }

    [Fact]
    public void Manifesto_tem_entrada_v2365_com_hash_real_dependencia_e_status()
    {
        var sql = Read(MigrationV2365);
        var manifest = Read("database/migration-manifest.json");

        using var doc = JsonDocument.Parse(manifest);
        var entrada = doc.RootElement.GetProperty("migrations").EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("version").GetString() == MigrationVersion);
        Assert.NotEqual(JsonValueKind.Undefined, entrada.ValueKind);

        Assert.Equal(MigrationV2365.Replace('\\', '/'), entrada.GetProperty("source").GetString());
        Assert.Equal(Sha256Lf(sql), entrada.GetProperty("checksum").GetString());
        Assert.Equal("active", entrada.GetProperty("status").GetString());
        Assert.True(entrada.GetProperty("transactional").GetBoolean());
        var deps = entrada.GetProperty("dependsOn").EnumerateArray().Select(d => d.GetString()).ToArray();
        Assert.Equal(new[] { "2026_10_v2364_r81_tabelas_pf_2026" }, deps);

        foreach (var codigo in new[] { "HORA_EXTRA_ACRESCIMO_PCT", "DIAS_MES_TRABALHADO_HORAS", "DEPENDENTE_DEDUCAO_MENSAL",
            "DEDUCAO_SIMPLIFICADA_MENSAL", "PISO_ISENCAO_IRRF_DECIMO_TERCEIRO", "FGTS_MULTA_RESCISAO_PCT", "DIAS_CONTAGEM_MES_CREDITADO" })
            Assert.Contains(codigo, sql);
    }

    [Fact]
    public async Task Parametros_de_folha_semeados_na_fonte_unica_com_fonte_citavel()
    {
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();

        async Task<string> Valor(string codigo) =>
            (await cn.QuerySingleAsync<string>("select valor from plantaopro.parametros_legais where codigo=@c and ano=2026 and reg_status='A'", new { c = codigo }));

        Assert.Equal("50.00", await Valor("HORA_EXTRA_ACRESCIMO_PCT"));
        Assert.Equal("220", await Valor("DIAS_MES_TRABALHADO_HORAS"));
        Assert.Equal("189.59", await Valor("DEPENDENTE_DEDUCAO_MENSAL"));
        Assert.Equal("564.80", await Valor("DEDUCAO_SIMPLIFICADA_MENSAL"));
        Assert.Equal("3036.00", await Valor("PISO_ISENCAO_IRRF_DECIMO_TERCEIRO"));
        Assert.Equal("40.00", await Valor("FGTS_MULTA_RESCISAO_PCT"));
        Assert.Equal("15", await Valor("DIAS_CONTAGEM_MES_CREDITADO"));

        var semFonte = await cn.ExecuteScalarAsync<int>(
            "select count(*) from plantaopro.parametros_legais where ano=2026 and reg_status='A' and codigo in ('HORA_EXTRA_ACRESCIMO_PCT','DIAS_MES_TRABALHADO_HORAS','DEPENDENTE_DEDUCAO_MENSAL','DEDUCAO_SIMPLIFICADA_MENSAL','PISO_ISENCAO_IRRF_DECIMO_TERCEIRO','FGTS_MULTA_RESCISAO_PCT','DIAS_CONTAGEM_MES_CREDITADO') and (fonte_oficial is null or btrim(fonte_oficial)='')");
        Assert.Equal(0, semFonte);

        // Regras estaveis da CLT ficam fixas (sem vigencia); derivadas do minimo, vigencia anual.
        var fixos = await cn.ExecuteScalarAsync<int>(
            "select count(*) from plantaopro.parametros_legais where codigo in ('HORA_EXTRA_ACRESCIMO_PCT','DIAS_MES_TRABALHADO_HORAS','FGTS_MULTA_RESCISAO_PCT','DIAS_CONTAGEM_MES_CREDITADO') and ano=2026 and vigencia_inicio is null and vigencia_fim is null");
        Assert.Equal(4, fixos);
        var vigentes = await cn.ExecuteScalarAsync<int>(
            "select count(*) from plantaopro.parametros_legais where codigo in ('DEPENDENTE_DEDUCAO_MENSAL','DEDUCAO_SIMPLIFICADA_MENSAL','PISO_ISENCAO_IRRF_DECIMO_TERCEIRO') and ano=2026 and vigencia_inicio='2026-01-01'::date and vigencia_fim='2026-12-31'::date");
        Assert.Equal(3, vigentes);
    }

    [Fact]
    public async Task Mes_com_horas_extras_e_dependente_bate_com_o_oficial_linha_a_linha()
    {
        await GarantirSchemaAsync();
        var servico = Servico();

        // 5.000 base + 10h extras (50% sobre hora normal 220h) + 1 dependente.
        var mes = await servico.CalcularMesAsync(new CalcularMesCommand(5000.00m, 10m, 1, false, null));
        Assert.Equal(2026, mes.Ano);
        Assert.Equal(22.73m, mes.ValorHoraReferencia);
        Assert.Equal(340.91m, mes.ValorHorasExtras);
        Assert.Equal(5340.91m, mes.SalarioBruto);
        Assert.Equal(189.59m, mes.DescontosDependentes);
        Assert.Equal(0.00m, mes.DeducaoSimplificada);
        Assert.Equal(5340.91m, mes.BaseInss);
        Assert.Equal(557.33m, mes.Inss);
        Assert.Equal(4593.99m, mes.BaseIrrf);
        Assert.Equal(4554.00m, mes.PisoIrrfMensal);
        Assert.Equal(370.88m, mes.Irrf);
        Assert.Equal(4412.70m, mes.SalarioLiquido);
        Assert.Equal(8.00m, mes.AliquotaFgtsPct);
        Assert.Equal(427.27m, mes.FgtsDepositado);

        // 6.000 base sem extras com deducao simplificada: IRRF sai do zero porque a base passa do piso.
        var simplificada = await servico.CalcularMesAsync(new CalcularMesCommand(6000.00m, 0m, 0, true, null));
        Assert.Equal(649.60m, simplificada.Inss);
        Assert.Equal(564.80m, simplificada.DeducaoSimplificada);
        Assert.Equal(4785.60m, simplificada.BaseIrrf);
        Assert.Equal(420.04m, simplificada.Irrf);
        Assert.Equal(4930.36m, simplificada.SalarioLiquido);
        Assert.Equal(480.00m, simplificada.FgtsDepositado);
    }

    [Fact]
    public async Task Decimo_terceiro_prorrateia_meses_e_isenta_ate_dois_minimos()
    {
        await GarantirSchemaAsync();
        var servico = Servico();

        // Admitido em 2025: ano cheio em 2026, 12 meses; primeira parcela = metade do proporcional.
        var anoCheio = await servico.CalcularDecimoTerceiroAsync(new CalcularDecimoTerceiroCommand(5000.00m, new DateTime(2025, 6, 10), 1, null));
        Assert.Equal(12, anoCheio.MesesCreditados);
        Assert.Equal(5000.00m, anoCheio.ProRataAnual);
        Assert.Equal(2500.00m, anoCheio.ValorParcela);
        Assert.Equal(202.23m, anoCheio.Inss);
        Assert.Equal(0.00m, anoCheio.ExcedentePisoDoisMinimos);
        Assert.Equal(0.00m, anoCheio.Irrf);
        Assert.Equal(2297.77m, anoCheio.LiquidoParcela);

        // Admissão em 2026-03-15: março conta (15+ dias) e soma 10 meses no ano.
        var parcial = await servico.CalcularDecimoTerceiroAsync(new CalcularDecimoTerceiroCommand(4500.00m, new DateTime(2026, 3, 15), 1, null));
        Assert.Equal(10, parcial.MesesCreditados);
        Assert.Equal(3750.00m, parcial.ProRataAnual);
        Assert.Equal(1875.00m, parcial.ValorParcela);
        Assert.Equal(145.98m, parcial.Inss);
        Assert.Equal(0.00m, parcial.Irrf);
        Assert.Equal(1729.02m, parcial.LiquidoParcela);

        // Salário alto: excedente sobre 2x minimo entra na primeira faixa e ainda zera pela parcela a deduzir.
        var alto = await servico.CalcularDecimoTerceiroAsync(new CalcularDecimoTerceiroCommand(8000.00m, new DateTime(2024, 1, 5), 1, null));
        Assert.Equal(8000.00m, alto.ProRataAnual);
        Assert.Equal(4000.00m, alto.ValorParcela);
        Assert.Equal(373.41m, alto.Inss);
        Assert.Equal(964.00m, alto.ExcedentePisoDoisMinimos);
        Assert.Equal(0.00m, alto.Irrf);
        Assert.Equal(3626.59m, alto.LiquidoParcela);
    }

    [Fact]
    public async Task Ferias_com_terco_constitucional_batem_com_o_oficial()
    {
        await GarantirSchemaAsync();
        var servico = Servico();

        var periodoCheio = await servico.CalcularFeriasAsync(new CalcularFeriasCommand(3000.00m, 30, null));
        Assert.Equal(3000.00m, periodoCheio.ValorFerias);
        Assert.Equal(1000.00m, periodoCheio.TercoFerias);
        Assert.Equal(4000.00m, periodoCheio.TotalFerias);
        Assert.Equal(373.41m, periodoCheio.Inss);
        Assert.Equal(3626.59m, periodoCheio.BaseIrrf);
        Assert.Equal(0.00m, periodoCheio.Irrf);
        Assert.Equal(3626.59m, periodoCheio.Liquido);

        var salarioAlto = await servico.CalcularFeriasAsync(new CalcularFeriasCommand(8000.00m, 30, null));
        Assert.Equal(2666.67m, salarioAlto.TercoFerias);
        Assert.Equal(10666.67m, salarioAlto.TotalFerias);
        Assert.Equal(951.64m, salarioAlto.Inss);
        Assert.Equal(9715.03m, salarioAlto.BaseIrrf);
        Assert.Equal(1775.63m, salarioAlto.Irrf);
        Assert.Equal(7939.40m, salarioAlto.Liquido);
    }

    [Fact]
    public async Task Rescisao_sem_justa_causa_monta_a_previa_verba_por_verba()
    {
        await GarantirSchemaAsync();
        var servico = Servico();

        // Admitido 2025-02-10, demitido 2026-10-15, sem aviso prévio dado: saldo do mês, 13o
        // proporcional (10 meses), férias vencidas + proporcionais (cada com terço), aviso indenizado,
        // FGTS de 22 meses e multa de 40%.
        var rescisao = await servico.CalcularRescisaoAsync(new CalcularRescisaoCommand(4000.00m, new DateTime(2025, 2, 10), new DateTime(2026, 10, 15), false, null));
        Assert.False(rescisao.AvisoPrevio);
        Assert.Equal(2000.00m, rescisao.Verbas.Single(v => v.Codigo == "SALDO").Valor);
        Assert.Equal(3333.33m, rescisao.Verbas.Single(v => v.Codigo == "DECIMO_PROPORCIONAL").Valor);
        Assert.Equal(4000.00m, rescisao.Verbas.Single(v => v.Codigo == "FERIAS_VENCIDAS").Valor);
        Assert.Equal(1333.33m, rescisao.Verbas.Single(v => v.Codigo == "TERCO_VENCIDAS").Valor);
        Assert.Equal(4000.00m, rescisao.Verbas.Single(v => v.Codigo == "FERIAS_PROPORCIONAL").Valor);
        Assert.Equal(1333.33m, rescisao.Verbas.Single(v => v.Codigo == "TERCO_PROPORCIONAL").Valor);
        Assert.Equal(4000.00m, rescisao.Verbas.Single(v => v.Codigo == "AVISO_PREVIO").Valor);

        Assert.Equal(19999.99m, rescisao.TotalVerbasTributaveis);
        Assert.Equal(951.64m, rescisao.InssRetido);
        Assert.Equal(19048.35m, rescisao.BaseIrrf);
        Assert.Equal(4342.30m, rescisao.IrrfRetido);
        Assert.Equal(14706.05m, rescisao.LiquidoVerbas);

        Assert.Equal(8.00m, rescisao.AliquotaFgtsPct);
        Assert.Equal(22, rescisao.MesesFgtsDepositados);
        Assert.Equal(7040.00m, rescisao.FgtsTotal);
        Assert.Equal(40.00m, rescisao.MultaPct);
        Assert.Equal(2816.00m, rescisao.Multa40);
        Assert.Equal(24562.05m, rescisao.TotalParaPerceber);
    }

    [Fact]
    public async Task Validacoes_da_folha_sao_erros_explicitos()
    {
        await GarantirSchemaAsync();
        var servico = Servico();

        // Dependentes e deducao simplificada nao se misturam (regra oficial do contribuinte).
        await Assert.ThrowsAsync<ArgumentException>(() => servico.CalcularMesAsync(new CalcularMesCommand(6000.00m, 0m, 2, true, null)));
        await Assert.ThrowsAsync<ArgumentException>(() => servico.CalcularFeriasAsync(new CalcularFeriasCommand(3000.00m, 31, null)));
        await Assert.ThrowsAsync<ArgumentException>(() => servico.CalcularDecimoTerceiroAsync(new CalcularDecimoTerceiroCommand(3000.00m, new DateTime(2025, 1, 1), 3, null)));
        await Assert.ThrowsAsync<ArgumentException>(() => servico.CalcularRescisaoAsync(new CalcularRescisaoCommand(3000.00m, new DateTime(2026, 5, 1), new DateTime(2026, 2, 1), false, null)));
    }
}
