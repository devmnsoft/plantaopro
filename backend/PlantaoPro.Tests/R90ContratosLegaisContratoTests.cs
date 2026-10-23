using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
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
/// R90 (bloco B4 - fechamento da bateria de contratos legais): bateria completa
/// sobre o conjunto de parametros oficiais 2026 da fonte unica parametros_legais
/// (rodadas 80 a 89). Contrato travado em tres camadas: (1) valores exatos com
/// fonte oficial citavel e vigencia em cada linha; (2) derivacoes cruzadas entre
/// parametros (piso 3x minimo, 13o 2x minimo, teto = ultima faixa INSS,
/// continuidade das parcelas a deduzir nas fronteiras de faixa, limites ME/EPP);
/// (3) exemplos reais de calculo passando pelos servicos oficiais (tributacao PF
/// e folha) com os vetores travados das rodadas 81/82. Nenhum numero legal vive
/// neste arquivo alem dos vetores esperados que sao o proprio contrato.
/// </summary>
public sealed class R90ContratosLegaisContratoTests
{
    private const string MigrationV2363 = "database/migrations/2026_10_v2363_r80_parametros_legais.sql";
    private const string MigrationV2364 = "database/migrations/2026_10_v2364_r81_tabelas_pf_2026.sql";
    private const string MigrationV2365 = "database/migrations/2026_10_v2365_r82_parametros_folha_2026.sql";
    private const string MigrationV2366 = "database/migrations/2026_10_v2366_r83_simples_nacional_2026.sql";
    private const string MigrationV2367 = "database/migrations/2026_10_v2367_r84_calendario_fiscal_2026.sql";
    private const string MigrationV2368 = "database/migrations/2026_10_v2368_r85_dfe_nacional.sql";
    private const string MigrationV2369 = "database/migrations/2026_10_v2369_r86_trilhos_pagamento.sql";
    private const string MigrationV2370 = "database/migrations/2026_10_v2370_r87_lgpd_saude.sql";
    private const string MigrationV2371 = "database/migrations/2026_10_v2371_r88_esocial_trabalhista.sql";
    private const string MigrationV2372 = "database/migrations/2026_10_v2372_r89_vendas_internacionais.sql";

    private static string Root => RepositoryPathResolver.ResolveRoot();
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    /// <summary>Linha da fonte unica para as asserts de valor/fonte/vigencia.</summary>
    private sealed record LinhaParametro(string Codigo, string Valor, string Fonte, string VigIni, string VigFim);

    /// <summary>Advisory lock proprio; garante a tabela (v2363) e a semeadura de todas
    /// as migracoes B4 ate a v2372 (todas idempotentes, on conflict do nothing).</summary>
    private static async Task GarantirSchemaAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r90_contratos_legais_schema'));");
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
            await cn.ExecuteAsync(Read(MigrationV2364));
            await cn.ExecuteAsync(Read(MigrationV2365));
            await cn.ExecuteAsync(Read(MigrationV2366));
            await cn.ExecuteAsync(Read(MigrationV2367));
            await cn.ExecuteAsync(Read(MigrationV2368));
            await cn.ExecuteAsync(Read(MigrationV2369));
            await cn.ExecuteAsync(Read(MigrationV2370));
            await cn.ExecuteAsync(Read(MigrationV2371));
            await cn.ExecuteAsync(Read(MigrationV2372));
        }
        finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r90_contratos_legais_schema'));") ; }
    }

    private static async Task<Dictionary<string, LinhaParametro>> Parametros()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var linhas = (await cn.QueryAsync<LinhaParametro>(
            @"select codigo as Codigo, valor as Valor,
                     coalesce(fonte_oficial, '') as Fonte,
                     coalesce(vigencia_inicio::text, '') as VigIni,
                     coalesce(vigencia_fim::text, '') as VigFim
                from plantaopro.parametros_legais
                 where ano = 2026 and reg_status = 'A'"))
            .ToList();
        return linhas.ToDictionary(l => l.Codigo);
    }

    private static decimal Num(string valor) =>
        decimal.Parse(valor, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

    private static (decimal Limite, decimal Aliq, decimal Parc) ParseFaixa(string valor)
    {
        var p = valor.Split('|');
        Assert.True(p.Length == 3, $"formato limite|aliquota|parcela violado: {valor}");
        var limite = p[0] == "" ? decimal.MaxValue : Num(p[0]);
        return (limite, Num(p[1]), Num(p[2]));
    }

    /// <summary>Invariante das tabelas oficiais: a faixa N avaliada pelo seu
    /// "aliquota x base - parcela" no limite da faixa anterior deve bater com a
    /// faixa anterior no mesmo ponto (tolerancia 0,01 = arredondamento da parcela).</summary>
    private static void ConferirContinuidade((decimal Limite, decimal Aliq, decimal Parc)[] faixas, string contexto, int ateExclusive)
    {
        for (var n = 1; n < ateExclusive; n++)
        {
            var baseFronteira = faixas[n - 1].Limite;
            var porFormulaNova = Math.Round(faixas[n].Aliq / 100m * baseFronteira - faixas[n].Parc, 2, MidpointRounding.AwayFromZero);
            var porFormulaAnterior = Math.Round(faixas[n - 1].Aliq / 100m * baseFronteira - faixas[n - 1].Parc, 2, MidpointRounding.AwayFromZero);
            Assert.True(Math.Abs(porFormulaNova - porFormulaAnterior) <= 0.01m,
                $"{contexto}: a faixa {n + 1} deve ser continua no limite anterior ({baseFronteira}); nova={porFormulaNova} anterior={porFormulaAnterior}");
        }
    }

    [Fact]
    public async Task Bateria_particiona_os_117_parametros_ativos_de_2026_sem_perder_codigo()
    {
        await GarantirSchemaAsync();
        var servico = new BateriaLegalService(new ParametrosLegaisRepository(TestDatabase.ConnectionString));
        var bateria = await servico.ObterBateriaAsync(2026);

        Assert.Equal(2026, bateria.Ano);
        Assert.Equal(117, bateria.TotalParametros);
        var planos = bateria.Grupos.SelectMany(g => g.Parametros).ToList();
        Assert.Equal(117, planos.Count);
        Assert.Equal(117, planos.Select(p => p.Codigo).Distinct().Count());
        // Os 10 temas fixos na ordem do catalogo; sem grupo "DIVERSOS" (tudo classificado).
        Assert.Equal(
            new[] { "PF_TRIBUTACAO", "FOLHA_RESCISAO", "FGTS", "SIMPLES_NACIONAL", "CALENDARIO_FISCAL",
                    "ESOCIAL_TRABALHO", "DFE_SEFAZ", "TRILHOS_PAGAMENTO", "LGPD_SA", "VENDA_INTERNACIONAL" },
            bateria.Grupos.Select(g => g.Tema).ToArray());
        Assert.DoesNotContain(bateria.Grupos, g => g.Tema == "DIVERSOS");
        // Cada linha ativa tem ano correto e fonte oficial citavel (contrato da fonte unica).
        Assert.DoesNotContain(planos, p => p.Ano != 2026);
        Assert.DoesNotContain(planos, p => string.IsNullOrWhiteSpace(p.FonteOficial));

        // Nada ativo fora da bateria: o total bate com a contagem ativa direta no banco.
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var dbTotal = await cn.ExecuteScalarAsync<int>(
            "select count(*) from plantaopro.parametros_legais where ano=2026 and reg_status='A'");
        Assert.Equal(dbTotal, planos.Count);
    }

    [Fact]
    public async Task Bateria_pf_tributacao_oficial_travada_linha_a_linha_com_fonte_citavel()
    {
        await GarantirSchemaAsync();
        var bat = await Parametros();

        Assert.Equal("1518.00", bat["SALARIO_MINIMO"].Valor);
        Assert.Contains("diario oficial", bat["SALARIO_MINIMO"].Fonte);
        Assert.Equal("4554.00", bat["PISO_ISENCAO_IRRF_MENSAL"].Valor);
        Assert.Contains("Decreto-Lei 3.240/1941", bat["PISO_ISENCAO_IRRF_MENSAL"].Fonte);
        Assert.Equal("8157.41", bat["INSS_TETO_CONTRIBUICAO"].Valor);
        Assert.Contains("Portaria Interministerial MPS/MF n. 6/2025", bat["INSS_TETO_CONTRIBUICAO"].Fonte);

        Assert.Equal("1518.00|7.5|0.00", bat["INSS_FAIXA_1"].Valor);
        Assert.Equal("2793.88|9.0|22.77", bat["INSS_FAIXA_2"].Valor);
        Assert.Equal("4190.83|12.0|106.59", bat["INSS_FAIXA_3"].Valor);
        Assert.Equal("8157.41|14.0|190.40", bat["INSS_FAIXA_4"].Valor);
        for (var i = 1; i <= 4; i++)
            Assert.Contains("Portaria Interministerial MPS/MF n. 6/2025", bat[$"INSS_FAIXA_{i}"].Fonte);

        Assert.Equal("2826.65|7.5|169.44", bat["IRRF_FAIXA_1"].Valor);
        Assert.Equal("3751.05|15.0|381.44", bat["IRRF_FAIXA_2"].Valor);
        Assert.Equal("4664.68|22.5|662.77", bat["IRRF_FAIXA_3"].Valor);
        Assert.Equal("|27.5|896.00", bat["IRRF_FAIXA_4"].Valor);
        for (var i = 1; i <= 4; i++)
            Assert.Contains("Lei 14.848/2024", bat[$"IRRF_FAIXA_{i}"].Fonte);

        // Anuais travados na janela completa de 2026.
        foreach (var codigo in new[]
        {
            "SALARIO_MINIMO", "PISO_ISENCAO_IRRF_MENSAL", "INSS_TETO_CONTRIBUICAO",
            "INSS_FAIXA_1", "INSS_FAIXA_2", "INSS_FAIXA_3", "INSS_FAIXA_4",
            "IRRF_FAIXA_1", "IRRF_FAIXA_2", "IRRF_FAIXA_3", "IRRF_FAIXA_4"
        })
        {
            Assert.True(bat[codigo].VigIni == "2026-01-01", $"{codigo}: vigencia inicio deve ser 2026-01-01");
            Assert.True(bat[codigo].VigFim == "2026-12-31", $"{codigo}: vigencia fim deve ser 2026-12-31");
        }
    }

    [Fact]
    public async Task Derivacoes_cruzadas_da_bateria_pf_sao_mateiramente_consistentes()
    {
        await GarantirSchemaAsync();
        var bat = await Parametros();

        var minimo = Num(bat["SALARIO_MINIMO"].Valor);
        var piso = Num(bat["PISO_ISENCAO_IRRF_MENSAL"].Valor);
        var piso13 = Num(bat["PISO_ISENCAO_IRRF_DECIMO_TERCEIRO"].Valor);
        var teto = Num(bat["INSS_TETO_CONTRIBUICAO"].Valor);

        Assert.True(piso == 3 * minimo, $"piso mensal de isencao deve ser 3 x minimo ({piso} vs {3 * minimo})");
        Assert.True(piso13 == 2 * minimo, $"piso do 13o deve ser 2 x minimo ({piso13} vs {2 * minimo})");

        var inss = Enumerable.Range(1, 4).Select(i => ParseFaixa(bat[$"INSS_FAIXA_{i}"].Valor)).ToArray();
        var irrf = Enumerable.Range(1, 4).Select(i => ParseFaixa(bat[$"IRRF_FAIXA_{i}"].Valor)).ToArray();

        // Teto = limite da ultima faixa INSS; a primeira faixa INSS parte do proprio minimo.
        Assert.True(teto == inss[3].Limite, $"teto ({teto}) deve ser o limite da INSS_FAIXA_4 ({inss[3].Limite})");
        Assert.True(inss[0].Limite == minimo, $"primeira faixa INSS parte do minimo ({inss[0].Limite} vs {minimo})");
        for (var i = 0; i < 3; i++)
        {
            Assert.True(inss[i].Limite < inss[i + 1].Limite, $"limites INSS crescentes na faixa {i + 1}");
            Assert.True(inss[i].Aliq < inss[i + 1].Aliq, $"aliquotas INSS crescentes na faixa {i + 1}");
            Assert.True(irrf[i].Limite < irrf[i + 1].Limite, $"limites IRRF crescentes na faixa {i + 1}");
            Assert.True(irrf[i].Aliq < irrf[i + 1].Aliq, $"aliquotas IRRF crescentes na faixa {i + 1}");
        }
        Assert.Equal(decimal.MaxValue, irrf[3].Limite);
        // O piso de isencao fica acima da primeira faixa IRRF: ele prevalece sobre a tabela.
        Assert.True(irrf[0].Limite < piso, $"piso de isencao ({piso}) prevalece sobre a 1a faixa IRRF ({irrf[0].Limite})");

        // Continuidade das parcelas a deduzir nas fronteiras de faixa.
        ConferirContinuidade(inss, "INSS", 4);
        ConferirContinuidade(irrf, "IRRF", 4);

        // Deducoes do contribuinte positivas e abaixo do piso.
        var dependente = Num(bat["DEPENDENTE_DEDUCAO_MENSAL"].Valor);
        var simplificada = Num(bat["DEDUCAO_SIMPLIFICADA_MENSAL"].Valor);
        Assert.True(dependente > 0m && dependente < piso, $"deducao por dependente ({dependente}) entre zero e o piso");
        Assert.True(simplificada > 0m && simplificada < piso, $"deducao simplificada ({simplificada}) entre zero e o piso");
    }

    [Fact]
    public async Task Bateria_folha_fgts_e_rescisao_travada_com_duas_fontes_para_a_hora_extra()
    {
        await GarantirSchemaAsync();
        var bat = await Parametros();

        // Hora extra travada com dois codigos-fonte (R82 consome HORA_EXTRA_ACRESCIMO_PCT;
        // R88 HORA_EXTRA_ACRESIMO_MIN_PCT) - duplicidade registrada, valores identicos.
        Assert.Equal("50.00", bat["HORA_EXTRA_ACRESCIMO_PCT"].Valor);
        Assert.Equal("50", bat["HORA_EXTRA_ACRESIMO_MIN_PCT"].Valor);
        Assert.True(Num(bat["HORA_EXTRA_ACRESCIMO_PCT"].Valor) == Num(bat["HORA_EXTRA_ACRESIMO_MIN_PCT"].Valor),
            "os dois codigos de hora extra devem bater");
        Assert.Contains("CLT art. 7", bat["HORA_EXTRA_ACRESCIMO_PCT"].Fonte);
        Assert.Contains("art. 59", bat["HORA_EXTRA_ACRESIMO_MIN_PCT"].Fonte);

        // Jornada mensal canonica (220h) travada nos dois codigos consumidores.
        Assert.Equal("220", bat["DIAS_MES_TRABALHADO_HORAS"].Valor);
        Assert.Equal("220", bat["SALARIO_DIVISOR_MENSAL_HORAS"].Valor);
        Assert.Equal("15", bat["DIAS_CONTAGEM_MES_CREDITADO"].Valor);
        Assert.Equal("189.59", bat["DEPENDENTE_DEDUCAO_MENSAL"].Valor);
        Assert.Equal("564.80", bat["DEDUCAO_SIMPLIFICADA_MENSAL"].Valor);
        Assert.Equal("3036.00", bat["PISO_ISENCAO_IRRF_DECIMO_TERCEIRO"].Valor);
        Assert.Contains("RIR art. 788", bat["PISO_ISENCAO_IRRF_DECIMO_TERCEIRO"].Fonte);

        Assert.Equal("8.00", bat["FGTS_ALIQUOTA_MENSAL"].Valor);
        Assert.Contains("Lei 5.107/1966", bat["FGTS_ALIQUOTA_MENSAL"].Fonte);
        Assert.Equal("20", bat["FGTS_VENCIMENTO_DIA"].Valor);
        Assert.Equal("40.00", bat["FGTS_MULTA_RESCISAO_PCT"].Valor);
        Assert.Contains("art. 18", bat["FGTS_MULTA_RESCISAO_PCT"].Fonte);

        // Regras estaveis da CLT ficam fixas (sem vigencia); deducoes anuais na janela 2026.
        foreach (var codigo in new[]
        {
            "HORA_EXTRA_ACRESCIMO_PCT", "HORA_EXTRA_ACRESIMO_MIN_PCT", "DIAS_MES_TRABALHADO_HORAS",
            "DIAS_CONTAGEM_MES_CREDITADO", "FGTS_ALIQUOTA_MENSAL", "FGTS_VENCIMENTO_DIA",
            "FGTS_MULTA_RESCISAO_PCT", "SALARIO_DIVISOR_MENSAL_HORAS"
        })
        {
            Assert.True(bat[codigo].VigIni == "" && bat[codigo].VigFim == "", $"{codigo} deve ser fixo (sem vigencia)");
        }
        foreach (var codigo in new[] { "DEPENDENTE_DEDUCAO_MENSAL", "DEDUCAO_SIMPLIFICADA_MENSAL", "PISO_ISENCAO_IRRF_DECIMO_TERCEIRO" })
        {
            Assert.True(bat[codigo].VigIni == "2026-01-01" && bat[codigo].VigFim == "2026-12-31", $"{codigo} deve ter vigencia anual 2026");
        }
    }

    [Fact]
    public async Task Bateria_simples_nacional_travada_com_faixas_cumulativas_consistentes()
    {
        await GarantirSchemaAsync();
        var bat = await Parametros();

        var esperadas = new Dictionary<string, string>
        {
            ["SIMPLES_ANEXO_1_FAIXA_1"] = "180000.00|4.00|0.00",
            ["SIMPLES_ANEXO_1_FAIXA_2"] = "360000.00|7.30|5940.00",
            ["SIMPLES_ANEXO_1_FAIXA_3"] = "720000.00|9.50|13860.00",
            ["SIMPLES_ANEXO_1_FAIXA_4"] = "1800000.00|10.70|22500.00",
            ["SIMPLES_ANEXO_1_FAIXA_5"] = "3600000.00|14.30|87300.00",
            ["SIMPLES_ANEXO_1_FAIXA_6"] = "4800000.00|19.00|378000.00",
            ["SIMPLES_ANEXO_2_FAIXA_1"] = "180000.00|4.50|0.00",
            ["SIMPLES_ANEXO_2_FAIXA_2"] = "360000.00|7.80|5940.00",
            ["SIMPLES_ANEXO_2_FAIXA_3"] = "720000.00|10.00|13860.00",
            ["SIMPLES_ANEXO_2_FAIXA_4"] = "1800000.00|11.20|22500.00",
            ["SIMPLES_ANEXO_2_FAIXA_5"] = "3600000.00|14.70|85500.00",
            ["SIMPLES_ANEXO_2_FAIXA_6"] = "4800000.00|30.00|720000.00",
            ["SIMPLES_ANEXO_3_FAIXA_1"] = "180000.00|6.00|0.00",
            ["SIMPLES_ANEXO_3_FAIXA_2"] = "360000.00|11.20|9360.00",
            ["SIMPLES_ANEXO_3_FAIXA_3"] = "720000.00|13.50|17640.00",
            ["SIMPLES_ANEXO_3_FAIXA_4"] = "1800000.00|16.00|35640.00",
            ["SIMPLES_ANEXO_3_FAIXA_5"] = "3600000.00|21.00|125640.00",
            ["SIMPLES_ANEXO_3_FAIXA_6"] = "4800000.00|33.00|648000.00",
            ["SIMPLES_ANEXO_4_FAIXA_1"] = "180000.00|4.50|0.00",
            ["SIMPLES_ANEXO_4_FAIXA_2"] = "360000.00|9.00|8100.00",
            ["SIMPLES_ANEXO_4_FAIXA_3"] = "720000.00|10.20|12420.00",
            ["SIMPLES_ANEXO_4_FAIXA_4"] = "1800000.00|14.00|39780.00",
            ["SIMPLES_ANEXO_4_FAIXA_5"] = "3600000.00|22.00|183780.00",
            ["SIMPLES_ANEXO_4_FAIXA_6"] = "4800000.00|33.00|828000.00",
            ["SIMPLES_ANEXO_5_FAIXA_1"] = "180000.00|15.50|0.00",
            ["SIMPLES_ANEXO_5_FAIXA_2"] = "360000.00|18.00|4500.00",
            ["SIMPLES_ANEXO_5_FAIXA_3"] = "720000.00|19.50|9900.00",
            ["SIMPLES_ANEXO_5_FAIXA_4"] = "1800000.00|20.50|17100.00",
            ["SIMPLES_ANEXO_5_FAIXA_5"] = "3600000.00|23.00|62100.00",
            ["SIMPLES_ANEXO_5_FAIXA_6"] = "4800000.00|30.50|540000.00"
        };
        foreach (var (codigo, esperado) in esperadas)
            Assert.Equal(esperado, bat[codigo].Valor);
        foreach (var codigo in esperadas.Keys)
            Assert.Contains("LC 123/2006", bat[codigo].Fonte);

        var me = Num(bat["SIMPLES_LIMITE_ME_RECEITA_ANUAL"].Valor);
        var epp = Num(bat["SIMPLES_LIMITE_EPP_RECEITA_ANUAL"].Valor);
        Assert.Equal("360000.00", bat["SIMPLES_LIMITE_ME_RECEITA_ANUAL"].Valor);
        Assert.Equal("4800000.00", bat["SIMPLES_LIMITE_EPP_RECEITA_ANUAL"].Valor);
        Assert.Contains("LC 155/2016", bat["SIMPLES_LIMITE_EPP_RECEITA_ANUAL"].Fonte);

        for (var anexo = 1; anexo <= 5; anexo++)
        {
            var faixas = Enumerable.Range(1, 6)
                .Select(i => ParseFaixa(bat[$"SIMPLES_ANEXO_{anexo}_FAIXA_{i}"].Valor))
                .ToArray();
            // Classe ME = duas primeiras faixas; EPP fecha na ultima.
            Assert.True(faixas[1].Limite == me, $"anexo {anexo}: limite ME = fim da 2a faixa");
            Assert.True(faixas[5].Limite == epp, $"anexo {anexo}: limite EPP = fim da 6a faixa");
            for (var i = 0; i < 5; i++)
            {
                Assert.True(faixas[i].Limite < faixas[i + 1].Limite, $"anexo {anexo}: limites crescentes na faixa {i + 1}");
                Assert.True(faixas[i].Aliq < faixas[i + 1].Aliq, $"anexo {anexo}: aliquotas crescentes na faixa {i + 1}");
            }
            // Faixas 2 a 5 contínuas na fronteira; a 6a (trecho exclusivo de EPP) é intencionalmente
            // descontinua nesse mundo do plano (faixa punitiva acima dos 3,6M) e nao entra na conferencia.
            ConferirContinuidade(faixas, $"SIMPLES ANEXO {anexo}", 5);
        }

        // Dia do DAS travado nos tres codigos consumidores.
        Assert.Equal("20", bat["SIMPLES_DAS_DIA_VENCIMENTO"].Valor);
        Assert.Equal("20", bat["DAS_SIMPLES_VENCIMENTO_DIA"].Valor);
        Assert.Equal("20", bat["OBRIGACAO_FISCAL_DAS_SIMPLES"].Valor.Split('|')[2]);
    }

    [Fact]
    public async Task Bateria_calendario_fiscal_travada_no_formato_periodicidade_offset_dia_regime()
    {
        await GarantirSchemaAsync();
        var bat = await Parametros();

        Assert.Equal("MENSAL|1|20|SIMPLES_NACIONAL", bat["OBRIGACAO_FISCAL_DAS_SIMPLES"].Valor);
        Assert.Equal("MENSAL|1|15|GERAL", bat["OBRIGACAO_FISCAL_DCTFWEB_MENSAL"].Valor);
        Assert.Equal("MENSAL|1|15|GERAL", bat["OBRIGACAO_FISCAL_EFD_ICMS_MENSAL"].Valor);
        Assert.Equal("MENSAL|1|7|CLT", bat["OBRIGACAO_FISCAL_ESOCIAL_S1200"].Valor);
        Assert.Equal("MENSAL|1|20|CLT", bat["OBRIGACAO_FISCAL_FGTS_MENSAL"].Valor);
        Assert.Equal("ANUAL|0|31|GERAL", bat["OBRIGACAO_FISCAL_LIVROS_CONTABEIS_ANUAIS"].Valor);

        foreach (var (codigo, linha) in bat.Where(kvp => kvp.Key.StartsWith("OBRIGACAO_FISCAL_")).ToList())
        {
            var p = linha.Valor.Split('|');
            Assert.True(p.Length == 4, $"{codigo}: formato periodicidade|offset|dia|regime violado ({linha.Valor})");
            Assert.True(p[0] == "MENSAL" || p[0] == "ANUAL", $"{codigo}: periodicidade deve ser MENSAL ou ANUAL");
            Assert.True(int.TryParse(p[1], out var offset) && offset >= 0, $"{codigo}: offset de meses invalido");
            Assert.True(int.TryParse(p[2], out var dia) && dia >= 1 && dia <= 31, $"{codigo}: dia invalido");
        }

        // Janela de alerta e parametro operacional (documentado como nao legal).
        Assert.Equal("7", bat["OBRIGACOES_FISCAIS_JANELA_ALERTA_DIAS"].Valor);
        Assert.Contains("plano r66a0", bat["OBRIGACOES_FISCAIS_JANELA_ALERTA_DIAS"].Fonte);

        // Dias isolados (R80) batem com o dia dentro das obrigacoes (R84).
        Assert.Equal("15", bat["DCTFWEB_VENCIMENTO_DIA"].Valor);
        Assert.Equal("15", bat["OBRIGACAO_FISCAL_DCTFWEB_MENSAL"].Valor.Split('|')[2]);
        Assert.Equal("20", bat["FGTS_VENCIMENTO_DIA"].Valor);
        Assert.Equal("20", bat["OBRIGACAO_FISCAL_FGTS_MENSAL"].Valor.Split('|')[2]);
    }

    [Fact]
    public async Task Bateria_esocial_e_trabalhista_travada_com_teto_de_aviso_atinivel()
    {
        await GarantirSchemaAsync();
        var bat = await Parametros();

        var baseDias = Num(bat["AVISO_PREVIO_BASE_DIAS"].Valor);
        var acrescimo = Num(bat["AVISO_PREVIO_ACRESCIMO_POR_ANO_DIAS"].Valor);
        var teto = Num(bat["AVISO_PREVIO_TETO_TOTAL_DIAS"].Valor);
        Assert.Equal("30", bat["AVISO_PREVIO_BASE_DIAS"].Valor);
        Assert.Equal("3", bat["AVISO_PREVIO_ACRESCIMO_POR_ANO_DIAS"].Valor);
        Assert.Equal("90", bat["AVISO_PREVIO_TETO_TOTAL_DIAS"].Valor);
        Assert.Contains("Lei 12.506/2011", bat["AVISO_PREVIO_TETO_TOTAL_DIAS"].Fonte);
        Assert.True((teto - baseDias) % acrescimo == 0, "o teto de aviso deve ser atingivel pelos acréscimos");
        Assert.True((teto - baseDias) / acrescimo == 20, "o teto e alcancado apos 20 anos completos");

        Assert.Equal("44", bat["JORNADA_SEMANAL_MAX_HORAS"].Valor);
        Assert.Contains("CF/88", bat["JORNADA_SEMANAL_MAX_HORAS"].Fonte);

        // Catalogo oficial eSocial S-1.3: 14 eventos (serie S-1000 a S-1299 do empregador).
        var catalogo = bat["ESOCIAL_EVENTOS_CATALOGO"].Valor.Split('|').ToList();
        Assert.Equal(
            new[] { "S-1000", "S-1005", "S-1010", "S-1020", "S-1070", "S-1200", "S-1202", "S-1207",
                    "S-1210", "S-1260", "S-1270", "S-1280", "S-1298", "S-1299" },
            catalogo);

        // Divergencia registrada (sem igualdade assertada): o calendario fiscal R84 trava o
        // S-1200 especificamente no dia 7 e a MOS R88 trava eventos periodicos genericos no dia 15.
        Assert.Equal("7", bat["OBRIGACAO_FISCAL_ESOCIAL_S1200"].Valor.Split('|')[2]);
        Assert.Equal("15", bat["ESOCIAL_VENCIMENTO_PERIODICO_DIA"].Valor);
        Assert.Contains("MOS S-1.3", bat["ESOCIAL_VENCIMENTO_PERIODICO_DIA"].Fonte);
    }

    [Fact]
    public async Task Bateria_dfe_sefaz_travada_com_27_unidades_e_codigos_sinqef_distintos()
    {
        await GarantirSchemaAsync();
        var bat = await Parametros();

        Assert.Equal("4.0", bat["DFE_LAYOUT_NFE"].Valor);
        Assert.Equal("4.0", bat["DFE_LAYOUT_NFCE"].Valor);
        Assert.Equal("1.00", bat["DFE_LAYOUT_CFE_SAT"].Valor);
        Assert.Equal("3.00", bat["DFE_LAYOUT_CFE_SERVICOS"].Valor);

        // Tabela oficial de codigos de UF do DF-e (Ajuste SINIEF 07/05): 26 UFs + DF.
        var esperadoCodigo = new Dictionary<string, string>
        {
            ["SEFAZ_UF_AC"] = "12", ["SEFAZ_UF_AL"] = "27", ["SEFAZ_UF_AM"] = "13", ["SEFAZ_UF_AP"] = "16",
            ["SEFAZ_UF_BA"] = "29", ["SEFAZ_UF_CE"] = "23", ["SEFAZ_UF_DF"] = "53", ["SEFAZ_UF_ES"] = "32",
            ["SEFAZ_UF_GO"] = "52", ["SEFAZ_UF_MA"] = "21", ["SEFAZ_UF_MG"] = "31", ["SEFAZ_UF_MS"] = "50",
            ["SEFAZ_UF_MT"] = "51", ["SEFAZ_UF_PA"] = "15", ["SEFAZ_UF_PB"] = "25", ["SEFAZ_UF_PE"] = "26",
            ["SEFAZ_UF_PI"] = "22", ["SEFAZ_UF_PR"] = "41", ["SEFAZ_UF_RJ"] = "33", ["SEFAZ_UF_RN"] = "24",
            ["SEFAZ_UF_RO"] = "11", ["SEFAZ_UF_RR"] = "14", ["SEFAZ_UF_RS"] = "43", ["SEFAZ_UF_SC"] = "42",
            ["SEFAZ_UF_SE"] = "28", ["SEFAZ_UF_SP"] = "35", ["SEFAZ_UF_TO"] = "17"
        };
        var linhas = bat.Where(kvp => kvp.Key.StartsWith("SEFAZ_UF_")).ToList();
        Assert.Equal(27, linhas.Count);
        Assert.Equal(27, linhas.Select(l => l.Value.Valor.Split('|')[0]).Distinct().Count());

        var ambientesSat = new HashSet<string> { "SEFAZ_UF_AC", "SEFAZ_UF_AP", "SEFAZ_UF_CE", "SEFAZ_UF_PA", "SEFAZ_UF_PB", "SEFAZ_UF_PE", "SEFAZ_UF_RR" };
        foreach (var (codigo, linha) in linhas)
        {
            var p = linha.Valor.Split('|');
            Assert.True(p.Length == 2, $"{codigo}: formato codigoUF|documentos violado ({linha.Valor})");
            Assert.Equal(esperadoCodigo[codigo], p[0]);
            var docs = p[1].Split(',').ToList();
            Assert.Contains("NFE", docs);
            Assert.Contains("CFE_SERVICOS", docs);
            foreach (var doc in docs)
                Assert.True(doc is "NFE" or "NFC-E" or "CFE_SAT" or "CFE_SERVICOS", $"{codigo}: documento desconhecido ({doc})");
            var temSat = ambientesSat.Contains(codigo);
            Assert.True(docs.Contains("CFE_SAT") == temSat, $"{codigo}: ambiente SAT nao confere");
            Assert.True(docs.Contains("NFC-E") == !temSat, $"{codigo}: NFC-E so fora dos ambientes SAT");
            Assert.Contains("SINIEF", linha.Fonte);
        }
    }

    [Fact]
    public async Task Bateria_trilhos_pagamento_e_lgpd_travadas_com_fonte_citavel()
    {
        await GarantirSchemaAsync();
        var bat = await Parametros();

        Assert.Equal("1997-10-07", bat["TRILHO_BOLETO_BASE_FATOR_DIA"].Valor);
        Assert.Contains("Carta-Circular 2926", bat["TRILHO_BOLETO_BASE_FATOR_DIA"].Fonte);
        Assert.Equal("9", bat["TRILHO_BOLETO_MOEDA"].Valor);
        Assert.Equal("400", bat["TRILHO_CNAB_REMESSA_LAYOUT"].Valor);
        Assert.Equal("camt.239.00|camt.240.00", bat["TRILHO_FEBEN_DOCUMENTOS"].Valor);
        Assert.Equal("br.gov.bcb.pix", bat["TRILHO_PIX_GUI"].Valor);
        Assert.Equal("0000", bat["TRILHO_PIX_MCC_PADRAO"].Valor);

        // Retencoes: 5 anos fiscais (CTN art. 194) e 20 anos de prontuario (Lei 13.787/2018 art. 6).
        var fiscal = Num(bat["LGPD_SA_RETENCAO_FISCAL_MESES"].Valor);
        var prontuario = Num(bat["LGPD_SA_RETENCAO_PRONTUARIO_MESES"].Valor);
        Assert.Equal("60", bat["LGPD_SA_RETENCAO_FISCAL_MESES"].Valor);
        Assert.Equal("240", bat["LGPD_SA_RETENCAO_PRONTUARIO_MESES"].Valor);
        Assert.True(fiscal == 5 * 12, $"retencao fiscal ({fiscal} meses) deve ser 5 anos");
        Assert.True(prontuario == 20 * 12, $"retencao de prontuario ({prontuario} meses) deve ser 20 anos");
        Assert.Contains("13.787/2018", bat["LGPD_SA_RETENCAO_PRONTUARIO_MESES"].Fonte);
        Assert.Contains("13.709/2018", bat["LGPD_SA_BASE_SENSIVEL"].Fonte);
        Assert.Equal("CONSENTIMENTO_EXPLICITO", bat["LGPD_SA_BASE_SENSIVEL"].Valor);

        Assert.Equal(4, bat["LGPD_SA_CANAIS"].Valor.Split('|').Length);
        Assert.Equal(3, bat["LGPD_SA_DIREITOS"].Valor.Split('|').Length);
        Assert.Equal(2, bat["LGPD_SA_ESCOPOS_CONSENTIMENTO"].Valor.Split('|').Length);
    }

    [Fact]
    public async Task Bateria_venda_internacional_travada_com_dispensas_de_exportacao()
    {
        await GarantirSchemaAsync();
        var bat = await Parametros();

        Assert.Equal(
            new[] { "USD", "EUR", "GBP" },
            bat["COTACAO_MOEDAS"].Valor.Split('|').ToList());
        Assert.Equal("BRL", bat["COTACAO_MOEDA_BASE"].Valor);
        Assert.Equal("NAO_TRADICIONAL", bat["NOTA_INTERNACIONAL_FORMA"].Valor);
        Assert.Contains("Lei 8.080/1990", bat["NOTA_INTERNACIONAL_FORMA"].Fonte);
        Assert.Contains("4.502/1964", bat["NOTA_INTERNACIONAL_FORMA"].Fonte);
    }

    [Fact]
    public async Task Exemplos_reais_de_calculo_oficiais_batem_pelos_servicos_travados()
    {
        await GarantirSchemaAsync();
        var repo = new ParametrosLegaisRepository(TestDatabase.ConnectionString);
        var tributacao = new TributacaoPfService(repo);

        // Salario minimo: INSS 7,5% puro; IRRF isento (base abaixo do piso).
        var minimo = await tributacao.CalcularAsync(2026, 1518.00m);
        Assert.Equal(113.85m, minimo.Inss);
        Assert.Equal(1404.15m, minimo.BaseIrrf);
        Assert.Equal(0.00m, minimo.Irrf);
        Assert.Equal(1404.15m, minimo.SalarioLiquido);

        // Salario medio de 5.000: INSS da faixa final (509,60); base ainda abaixo do piso.
        var medio = await tributacao.CalcularAsync(2026, 5000.00m);
        Assert.Equal(509.60m, medio.Inss);
        Assert.Equal(4490.40m, medio.BaseIrrf);
        Assert.Equal(0.00m, medio.Irrf);
        Assert.Equal(4490.40m, medio.SalarioLiquido);

        // Teto de contribuicao: INSS 951,64; IRRF 27,5% sobre 7.205,77 - 896,00.
        var teto = await tributacao.CalcularAsync(2026, 8157.41m);
        Assert.Equal(8157.41m, teto.BaseInss);
        Assert.Equal(951.64m, teto.Inss);
        Assert.Equal(7205.77m, teto.BaseIrrf);
        Assert.Equal(1085.59m, teto.Irrf);
        Assert.Equal(6120.18m, teto.SalarioLiquido);

        // Folha real (trava R82 re-travada pela bateria): 5.000 + 10h extras + 1 dependente.
        var folha = new FolhaPfService(repo, tributacao);
        var mes = await folha.CalcularMesAsync(new CalcularMesCommand(5000.00m, 10m, 1, false, 2026));
        Assert.Equal(5340.91m, mes.SalarioBruto);
        Assert.Equal(557.33m, mes.Inss);
        Assert.Equal(4593.99m, mes.BaseIrrf);
        Assert.Equal(370.88m, mes.Irrf);
        Assert.Equal(4412.70m, mes.SalarioLiquido);
        Assert.Equal(8.00m, mes.AliquotaFgtsPct);
        Assert.Equal(427.27m, mes.FgtsDepositado);
    }
}
