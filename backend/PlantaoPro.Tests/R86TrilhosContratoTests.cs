using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R86 (trilhos de pagamento): PIX EMV (QR Code BR Code, BACEN/EMVCo, com CRC16-CCITT-FALSE),
/// boleto (BACEN Carta-Circular 2926: código de barras de 44 posições, linha digitável de 47
/// e fator de vencimento com rollover) e arquivo CNAB 400 de remessa, todos derivados das
/// faturas abertas do motor de cobrança SaaS (faturas_saas).
/// Vetores travados por fontes públicas: exemplos oficiais da CC 2926 (módulo 11, módulo 10
/// e linha digitável do exemplo oficial), tabela oficial de fator de vencimento (até
/// 21/02/2025, fator 9999), valor de checagem publicado do CRC-16/CCITT-FALSE
/// ("123456789" -> 29B1) e vetores fim-a-fim gerados pelo próprio produto (PIX estático
/// e dinâmico com txid determinístico, vetores A/B do boleto e arquivo de remessa).
/// A configuração dos trilhos é GLOBAL (plano SaaS, padrão cobranca_providers) e as
/// constantes institucionais (GUI, MCC, moeda, data base do fator, layout, documentos
/// FEBEN) leem da fonte única parametros_legais.
/// </summary>
public sealed class R86TrilhosContratoTests : IDisposable
{
    private const string MigrationV2363 = "database/migrations/2026_10_v2363_r80_parametros_legais.sql";
    private const string MigrationV2369 = "database/migrations/2026_10_v2369_r86_trilhos_pagamento.sql";
    private const string MigrationVersion = "2026_10_v2369_r86_trilhos_pagamento";

    private const string GuiPix = "br.gov.bcb.pix";
    private const string ChavePix = "plantaopro@exemplo.com.br";
    private const string NomeBeneficiario = "PLANTAOPRO LTDA";
    private const string CidadeBeneficiario = "CURITIBA";

    // Vetores PIX travados: payload estático e dinâmico (valor 150,00) gerados pelo produto.
    private const string VetorPixEstatico = "00022626470014br.gov.bcb.pix0125plantaopro@exemplo.com.br5204000053039865802BR5915PLANTAOPRO LTDA6008CURITIBA6304FCB5";
    private const string VetorPixDinamico = "00022626470014br.gov.bcb.pix0125plantaopro@exemplo.com.br5204000053039865406150.005802BR5915PLANTAOPRO LTDA6008CURITIBA622905251C8B7A2000014A56B7C800000630401D0";
    private const string TxidFixo = "1C8B7A2000014A56B7C800000";

    private static readonly Guid FaturaFixa = Guid.Parse("1c8b7a20-0001-4a56-b7c8-000000000001"); // id cujo ToString("N") deriva o TxidFixo
    private static readonly Guid ClienteR86 = Guid.Parse("b6000002-0000-4000-9000-8600000000c1");
    private static readonly Guid F1 = Guid.Parse("b6000002-0000-4000-9000-8600000000f1"); // ABERTA 150,00 venc 30/10/2026
    private static readonly Guid F2 = Guid.Parse("b6000002-0000-4000-9000-8600000000f2"); // ABERTA 234,56 venc 15/11/2026
    private static readonly Guid F3 = Guid.Parse("b6000002-0000-4000-9000-8600000000f3"); // PAGA 999,99 (fora da remessa)
    private static readonly Guid F4 = Guid.Parse("b6000002-0000-4000-9000-8600000000f4"); // CANCELADA (fora da remessa)
    private static readonly Guid F5 = Guid.Parse("b6000002-0000-4000-9000-8600000000f5"); // EM_CONTESTACAO (fora da remessa)
    private static readonly Guid F6 = Guid.Parse("b6000002-0000-4000-9000-8600000000f6"); // ABERTA valor zero (fora da remessa)
    private static readonly Guid Fpaga = Guid.Parse("b6000002-0000-4000-9000-8600000000fa"); // PAGA (sem trilho de pagamento)

    private readonly Task SementePronta = SementarAsync();

    static R86TrilhosContratoTests() => DapperTypeHandlerRegistrar.RegistrarTodos();

    public void Dispose()
    {
        SementePronta.GetAwaiter().GetResult();
        PurgarAsync().GetAwaiter().GetResult();
    }

    private static string Root => RepositoryPathResolver.ResolveRoot();
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    private static string Sha256Lf(string conteudo) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(conteudo.Replace("\r\n", "\n")))).ToLowerInvariant();

    private static async Task GarantirSchemaAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r86_trilhos_schema'));");
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
            // v2369 é idempotente: reexecução não duplica (create if not exists + on conflict do nothing).
            await cn.ExecuteAsync(Read(MigrationV2369));
        }
        finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r86_trilhos_schema'));"); }
    }

    private static async Task PurgarConfigAsync()
    {
        await TestInfraRetry.ExecutarComRetryImpasseAsync(async () =>
        {
            await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
            await cn.OpenAsync();
            await cn.ExecuteAsync("delete from plantaopro.cobranca_trilhos_config");
        });
    }

    private static TrilhosConfigCommand ComandoConfig(long nossoNumeroBase) =>
        new("237", "Banco Teste CC 2926", "0001", "01", "1234567", nossoNumeroBase, ChavePix, NomeBeneficiario, CidadeBeneficiario);

    // ---------------------------------------------------------------------
    // Fato 1 - manifesto com a entrada v2369 real.
    // ---------------------------------------------------------------------
    [Fact]
    public void Manifesto_tem_entrada_v2369_com_hash_real_dependencia_v2368_e_status()
    {
        var sql = Read(MigrationV2369);
        var manifest = Read("database/migration-manifest.json");

        using var doc = JsonDocument.Parse(manifest);
        var entrada = doc.RootElement.GetProperty("migrations").EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("version").GetString() == MigrationVersion);
        Assert.NotEqual(JsonValueKind.Undefined, entrada.ValueKind);

        Assert.Equal(MigrationV2369.Replace('\\', '/'), entrada.GetProperty("source").GetString());
        Assert.Equal(Sha256Lf(sql), entrada.GetProperty("checksum").GetString());
        Assert.Equal("active", entrada.GetProperty("status").GetString());
        Assert.True(entrada.GetProperty("transactional").GetBoolean());
        var deps = entrada.GetProperty("dependsOn").EnumerateArray().Select(d => d.GetString()).ToArray();
        Assert.Equal(new[] { "2026_10_v2368_r85_dfe_nacional" }, deps);

        foreach (var codigo in new[] { "TRILHO_PIX_GUI", "TRILHO_PIX_MCC_PADRAO", "TRILHO_BOLETO_MOEDA", "TRILHO_BOLETO_BASE_FATOR_DIA", "TRILHO_CNAB_REMESSA_LAYOUT", "TRILHO_FEBEN_DOCUMENTOS" })
            Assert.Contains(codigo, sql);
        Assert.Contains("create table if not exists plantaopro.cobranca_trilhos_config", sql);
        Assert.Contains("br.gov.bcb.pix", sql);
        Assert.Contains("1997-10-07", sql);
        Assert.Contains("camt.239.00|camt.240.00", sql);
        Assert.Contains("on conflict (codigo, ano)", sql);
    }

    // ---------------------------------------------------------------------
    // Fato 2 - schema e seeds idempotentes na fonte única, com fonte citável.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Schema_e_seeds_trilhos_idempotentes_na_fonte_unica_com_fonte_citavel()
    {
        await SementePronta;
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();

        var colunas = (await cn.QueryAsync<string>("select column_name from information_schema.columns where table_schema='plantaopro' and table_name='cobranca_trilhos_config'")).ToList();
        foreach (var coluna in new[] { "banco_codigo", "banco_nome", "agencia", "carteira", "conta_cedente", "nosso_numero_base", "chave_pix", "beneficiario_nome", "beneficiario_cidade", "ativo" })
            Assert.Contains(coluna, colunas);

        var esperado = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["TRILHO_PIX_GUI"] = "br.gov.bcb.pix",
            ["TRILHO_PIX_MCC_PADRAO"] = "0000",
            ["TRILHO_BOLETO_MOEDA"] = "9",
            ["TRILHO_BOLETO_BASE_FATOR_DIA"] = "1997-10-07",
            ["TRILHO_CNAB_REMESSA_LAYOUT"] = "400",
            ["TRILHO_FEBEN_DOCUMENTOS"] = "camt.239.00|camt.240.00"
        };
        foreach (var par in esperado)
        {
            var linha = await cn.QuerySingleAsync<dynamic>(
                "select valor, fonte_oficial from plantaopro.parametros_legais where codigo=@c and ano=2026 and reg_status='A'",
                new { c = par.Key });
            Assert.Equal(par.Value, (string)linha.valor);
            Assert.True(((string)linha.fonte_oficial).Length >= 10, "fonte oficial citavel exigida para " + par.Key);
        }

        // Idempotência: rodar a migração de novo não duplica nenhuma linha.
        await cn.ExecuteAsync(Read(MigrationV2369));
        var total = await cn.ExecuteScalarAsync<int>(
            @"select count(*) from plantaopro.parametros_legais where codigo like 'TRILHO\_%' escape '\' and ano=2026 and reg_status='A'");
        Assert.Equal(6, total);
    }

    // ---------------------------------------------------------------------
    // Fato 3 - PIX EMV estático e dinâmico travados + CRC16 com valor público.
    // ---------------------------------------------------------------------
    [Fact]
    public void Pix_emv_estatico_e_dinamico_travados_e_crc16_confere_valor_publico()
    {
        Assert.Equal(VetorPixEstatico, TrilhosPagamentoRegras.GerarPixEmv(GuiPix, ChavePix, "0000", NomeBeneficiario, CidadeBeneficiario, null, null));

        // O txid vem do id da fatura em formato N maiúsculo (determinístico, até 25 caracteres).
        var txid = FaturaFixa.ToString("N").ToUpperInvariant()[..25];
        Assert.Equal(TxidFixo, txid);
        Assert.Equal(VetorPixDinamico, TrilhosPagamentoRegras.GerarPixEmv(GuiPix, ChavePix, "0000", NomeBeneficiario, CidadeBeneficiario, "150.00", txid));

        // Valor de checagem publicado do CRC-16/CCITT-FALSE prova o polinômio/inicial de forma independente.
        Assert.Equal("29B1", TrilhosPagamentoRegras.Crc16Ccitt("123456789"));
        Assert.Equal(VetorPixEstatico[^4..], TrilhosPagamentoRegras.Crc16Ccitt(VetorPixEstatico[..^4]));
        Assert.Equal(VetorPixDinamico[^4..], TrilhosPagamentoRegras.Crc16Ccitt(VetorPixDinamico[..^4]));
    }

    // ---------------------------------------------------------------------
    // Fato 4 - validação PIX: casos válidos extraem os campos; inválidos caem.
    // ---------------------------------------------------------------------
    [Fact]
    public void Validacao_pix_emv_recebe_validos_e_rejeita_casos_invalidos()
    {
        var ok = TrilhosPagamentoRegras.ValidarPixEmv(VetorPixDinamico);
        Assert.True(ok.Valida, string.Join("; ", ok.Erros));
        Assert.Equal(GuiPix, ok.Gui);
        Assert.Equal(ChavePix, ok.Chave);
        Assert.Equal("0000", ok.Mcc);
        Assert.Equal("986", ok.Moeda);
        Assert.Equal("BR", ok.Pais);
        Assert.Equal(NomeBeneficiario, ok.Nome);
        Assert.Equal(CidadeBeneficiario, ok.Cidade);
        Assert.Equal("150.00", ok.Valor);
        Assert.Equal(TxidFixo, ok.Txid);

        Assert.False(TrilhosPagamentoRegras.ValidarPixEmv("").Valida);

        var truncado = TrilhosPagamentoRegras.ValidarPixEmv(VetorPixEstatico[..^6]);
        Assert.False(truncado.Valida);

        var trocado = VetorPixEstatico[..10] + '1' + VetorPixEstatico[11..];
        Assert.False(TrilhosPagamentoRegras.ValidarPixEmv(trocado).Valida);

        var semPais = TrilhosPagamentoRegras.ValidarPixEmv(VetorPixEstatico.Replace("5802BR", string.Empty));
        Assert.False(semPais.Valida);
        Assert.Contains(semPais.Erros, e => e.Contains("58"));

        var naoAscii = TrilhosPagamentoRegras.ValidarPixEmv(TrilhosPagamentoRegras.GerarPixEmv(GuiPix, ChavePix, "0000", "PLANTAOPRÕ LTDA", CidadeBeneficiario, null, null));
        Assert.False(naoAscii.Valida);
        Assert.Contains(naoAscii.Erros, e => e.Contains("ASCII"));

        var virgula = TrilhosPagamentoRegras.ValidarPixEmv(TrilhosPagamentoRegras.GerarPixEmv(GuiPix, ChavePix, "0000", NomeBeneficiario, CidadeBeneficiario, "150,00", TxidFixo));
        Assert.False(virgula.Valida);
        Assert.Contains(virgula.Erros, e => e.Contains("valor"));
    }

    // ---------------------------------------------------------------------
    // Fato 5 - boleto: DVs oficiais, fator com rollover e linha digitável.
    // ---------------------------------------------------------------------
    [Fact]
    public void Boleto_mod11_mod10_fator_e_linha_digitavel_conferem_vetores_oficiais_e_fixos()
    {
        var baseFator = new DateOnly(1997, 10, 7);

        // Exemplos oficiais da Carta-Circular 2926 (25/07/2000).
        Assert.Equal((720, 5, 6), TrilhosPagamentoRegras.Modulo11Boleto("7999123200000066007878130875503042541200000"));
        Assert.Equal((57, 7, 3), TrilhosPagamentoRegras.Modulo10Campo("999977721"));

        // Linha digitável oficial reconstrói o código de barras (DV geral 1) e round-tripa.
        const string LnOficial = "99991100100000350007772130530150081897500000";
        var ldOficial = TrilhosPagamentoRegras.LinhaDigitavelBoleto(LnOficial);
        Assert.Equal("99997772133053015008218975000003110010000035000", ldOficial.Raw47);
        Assert.Equal("99997.77213 30530.150082 18975.000003 1 10010000035000", ldOficial.Exibicao);
        Assert.Equal(1, TrilhosPagamentoRegras.Modulo11Boleto("9999100100000350007772130530150081897500000").DV);
        var oficialOk = TrilhosPagamentoRegras.ValidarLinhaDigitavel(ldOficial.Exibicao, baseFator, new DateOnly(2026, 10, 22));
        Assert.True(oficialOk.Valida, string.Join("; ", oficialOk.Erros));
        Assert.Equal("999", oficialOk.Banco);
        Assert.True(oficialOk.Fator == 1001, "fator do exemplo oficial");
        Assert.True(oficialOk.ValorCentavos == 35000L, "valor do exemplo oficial");
        Assert.Equal("7772130530150081897500000", oficialOk.CampoLivre);

        // Tabela oficial de fator de vencimento (marco zero 07/10/1997, encerra em 21/02/2025 com 9999).
        var datas = new[] { new DateOnly(2000, 7, 3), new DateOnly(2000, 7, 5), new DateOnly(2002, 12, 1), new DateOnly(2007, 3, 29), new DateOnly(2011, 11, 11), new DateOnly(2025, 2, 21) };
        var fatores = new[] { 1000, 1002, 1881, 3460, 5148, 9999 };
        for (var i = 0; i < datas.Length; i++)
            Assert.Equal(fatores[i], TrilhosPagamentoRegras.FatorVencimento(datas[i], baseFator).FatorExibido);

        // Rollover: dias totais fora da tabela oficial, fator exibido = dias mod 10000.
        Assert.Equal((10615L, 615, 1L), TrilhosPagamentoRegras.FatorVencimento(new DateOnly(2026, 10, 30), baseFator));
        Assert.Equal((10631L, 631, 1L), TrilhosPagamentoRegras.FatorVencimento(new DateOnly(2026, 11, 15), baseFator));

        // Vetor A (nosso número 7001, venc 30/10/2026, 150,00) - configuração bancária fixa da rodada.
        var clA = TrilhosPagamentoRegras.CampoLivreBoleto("0001", "01", 7001, "1234567");
        Assert.Equal("0001010000000700112345670", clA);
        var lnA = TrilhosPagamentoRegras.LinhaNumeroBoleto("237", '9', 615, 15000, clA);
        Assert.Equal("23797061500000150000001010000000700112345670", lnA);
        var ldA = TrilhosPagamentoRegras.LinhaDigitavelBoleto(lnA);
        Assert.Equal("23790001081000000070201123456707706150000015000", ldA.Raw47);
        Assert.Equal("23790.00108 10000.000702 01123.456707 7 06150000015000", ldA.Exibicao);

        // Vetor B (nosso número 7002, venc 15/11/2026, 234,56).
        var clB = TrilhosPagamentoRegras.CampoLivreBoleto("0001", "01", 7002, "1234567");
        var lnB = TrilhosPagamentoRegras.LinhaNumeroBoleto("237", '9', 631, 23456, clB);
        Assert.Equal("23794063100000234560001010000000700212345670", lnB);
        var ldB = TrilhosPagamentoRegras.LinhaDigitavelBoleto(lnB);
        Assert.Equal("23790001081000000070202123456705406310000023456", ldB.Raw47);
        Assert.Equal("23790.00108 10000.000702 02123.456705 4 06310000023456", ldB.Exibicao);

        // Validação da linha digitável: confere tudo e estima o vencimento pela janela de rollover.
        var hoje = new DateOnly(2026, 10, 22);
        var va = TrilhosPagamentoRegras.ValidarLinhaDigitavel(ldA.Exibicao, baseFator, hoje);
        Assert.True(va.Valida, string.Join("; ", va.Erros));
        Assert.Equal("237", va.Banco);
        Assert.Equal("9", va.Moeda);
        Assert.True(va.Fator == 615, "fator exibido do vetor A");
        Assert.True(va.VencimentoProvavel == new DateOnly(2026, 10, 30), "vencimento provavel do vetor A");
        Assert.True(va.ValorCentavos == 15000L, "valor do vetor A");
        Assert.Equal("0001010000000700112345670", va.CampoLivre);

        // Alteração de 1 dígito do valor quebra o DV geral (módulo 11).
        var tamperValor = ldA.Raw47[..^1] + '1';
        var vt = TrilhosPagamentoRegras.ValidarLinhaDigitavel(tamperValor, baseFator, hoje);
        Assert.False(vt.Valida);
        Assert.Contains(vt.Erros, e => e.Contains("módulo 11"));

        // Moeda diferente do real cai na posição 4.
        var moedaErrada = ldA.Raw47[..3] + '5' + ldA.Raw47[4..];
        var vm = TrilhosPagamentoRegras.ValidarLinhaDigitavel(moedaErrada, baseFator, hoje);
        Assert.False(vm.Valida);
        Assert.Contains(vm.Erros, e => e.Contains("moeda"));
    }

    // ---------------------------------------------------------------------
    // Fato 6 - remessa CNAB 400: inclusão/exclusão de faturas, cabeçalho, registros e trailer.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Remessa_cnab400_compoe_cabecalho_registros_e_trailer_de_400_posicoes()
    {
        await SementePronta;
        await GarantirSchemaAsync();
        await PurgarConfigAsync();
        try
        {
            var svc = new TrilhosPagamentoService(TestDatabase.ConnectionString);
            var salvar = await svc.SalvarConfiguracaoAsync(ComandoConfig(7000), default);
            Assert.True(salvar.Success, salvar.Message);

            // Candidatas globais exatamente como o serviço filtra (fonte da contagem esperada).
            await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
            await cn.OpenAsync();
            var linhaQtd = await cn.QuerySingleAsync<dynamic>(
                @"select count(*)::bigint as qtd, coalesce(sum((valor*100)::bigint),0)::bigint as centavos
                    from plantaopro.faturas_saas
                   where reg_status='A' and status in ('ABERTA','VENCIDA') and valor > 0");
            var qtd = (long)linhaQtd.qtd;
            var centavos = (long)linhaQtd.centavos;
            Assert.True(qtd >= 3, "as tres faturas abertas da rodada devem ser candidatas (vieram " + qtd + ")");
            Assert.True(qtd <= 500, "limite maximo de faturas por remessa");

            var resp = await svc.GerarRemessaBoletosAsync(new GerarRemessaCommand(null, 500), default);
            Assert.True(resp.Success, resp.Message);
            var d = resp.Data!;
            Assert.Equal(qtd, d.TotalItens);
            Assert.Equal(centavos / 100m, d.TotalValor);
            Assert.Equal(400, d.Layout);
            Assert.Equal("237", d.BancoCodigo);
            Assert.Equal("0001", d.Agencia);
            Assert.Equal("1234567", d.ContaCedente);

            // Arquivo: linhas de exatamente 400 posições separadas por CRLF, com CRLF final.
            Assert.EndsWith("\r\n", d.ConteudoArquivo);
            var partes = d.ConteudoArquivo.Split(new[] { "\r\n" }, StringSplitOptions.None);
            Assert.Equal(qtd + 3, partes.Length);
            for (var i = 0; i < partes.Length - 1; i++)
                Assert.Equal(400, partes[i].Length);

            var cab = partes[0];
            Assert.StartsWith("01RECEREMISSAO", cab);
            Assert.Equal("237", cab.Substring(14, 3));
            Assert.Equal("0001", cab.Substring(17, 4));
            Assert.Equal("1234567", cab.Substring(22, 7));
            Assert.Equal(DateOnly.FromDateTime(DateTime.Now.Date).ToString("ddMMyyyy", CultureInfo.InvariantCulture), cab.Substring(49, 8));

            var tra = partes[qtd + 1];
            Assert.StartsWith("9 *", tra);
            Assert.Equal(((int)qtd).ToString("D3"), tra.Substring(77, 3));
            Assert.Equal(centavos.ToString().PadLeft(15, '0'), tra.Substring(132, 15));
            Assert.Equal(new string(' ', 253), tra.Substring(147));

            // As faturas da rodada: nosso número sequencial, código de barras e linha digitável recalculados de forma independente.
            VerificarItemRemessa(d, F1, 615, "30102026", 15000L);
            VerificarItemRemessa(d, F2, 631, "15112026", 23456L);
        }
        finally { await PurgarConfigAsync(); }
    }

    private static void VerificarItemRemessa(RemessaBoletosDto d, Guid faturaId, int fatorEsperado, string vencDt, long centavosEsperados)
    {
        var idx = d.Itens.ToList().FindIndex(i => i.FaturaId == faturaId);
        Assert.True(idx >= 0, "fatura presente na remessa");
        var item = d.Itens[idx];
        var nosso = 7000L + idx + 1; // nossa sequência: base + posição no arquivo
        Assert.Equal(nosso, item.NossoNumero);
        Assert.Equal(fatorEsperado, item.FatorVencimento);
        Assert.True(item.RolloverFator == 1L, "rollover do fator em 2026");
        Assert.True(item.Valor * 100m == centavosEsperados, "valor do item");

        var cl = TrilhosPagamentoRegras.CampoLivreBoleto("0001", "01", nosso, "1234567");
        var ln = TrilhosPagamentoRegras.LinhaNumeroBoleto("237", '9', fatorEsperado, centavosEsperados, cl);
        Assert.Equal(ln, item.LinhaNumero);
        var ld = TrilhosPagamentoRegras.LinhaDigitavelBoleto(ln);
        Assert.Equal(ld.Raw47, item.LinhaDigitavel);
        Assert.Equal(ld.Exibicao, item.LinhaDigitavelExibicao);

        // Registro do CNAB: nosso número D17, linha número 44, vencimento ddMMyyyy, valor D15.
        var partes = d.ConteudoArquivo.Split(new[] { "\r\n" }, StringSplitOptions.None);
        var reg = partes.FirstOrDefault(l => l.StartsWith("10*") && l.Contains(ln));
        Assert.False(string.IsNullOrEmpty(reg), "registro do CNAB com o código de barras da fatura");
        Assert.Equal(nosso.ToString().PadLeft(17, '0'), reg!.Substring(52, 17));
        Assert.Equal(ln, reg.Substring(75, 44));
        Assert.Equal(vencDt, reg.Substring(124, 8));
        Assert.Equal(centavosEsperados.ToString().PadLeft(15, '0'), reg.Substring(132, 15));
    }

    // ---------------------------------------------------------------------
    // Fato 7 - configuração upsert, PIX por fatura com txid fixo e 404 honesto sem config.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Config_upsert_pix_por_fatura_com_txid_deterministico_e_sem_config_404_honesto()
    {
        await SementePronta;
        await GarantirSchemaAsync();
        await PurgarConfigAsync();
        try
        {
            var svc = new TrilhosPagamentoService(TestDatabase.ConnectionString);

            // Sem configuração: 404 honesto em todas as operações de geração.
            var sem = await svc.ObterConfiguracaoAsync(default);
            Assert.False(sem.Success);
            Assert.Equal(404, sem.StatusCode);
            Assert.Contains("Configure", sem.Message);
            var pixSem = await svc.GerarPixEstaticoAsync(default);
            Assert.False(pixSem.Success);
            Assert.Equal(404, pixSem.StatusCode);
            var remSem = await svc.GerarRemessaBoletosAsync(new GerarRemessaCommand(null, 10), default);
            Assert.False(remSem.Success);
            Assert.Equal(404, remSem.StatusCode);

            // Validação antes de tocar no banco: erros apontam cada campo.
            var camposRuins = await svc.SalvarConfiguracaoAsync(
                new TrilhosConfigCommand("23", "Banco Teste CC 2926", "0001", "01", "123456", 7000, ChavePix, NomeBeneficiario, CidadeBeneficiario), default);
            Assert.False(camposRuins.Success);
            Assert.Equal(400, camposRuins.StatusCode);
            Assert.Contains(camposRuins.Errors!, e => e.Contains("código do banco"));
            Assert.Contains(camposRuins.Errors!, e => e.Contains("conta do cedente"));

            var comAcento = await svc.SalvarConfiguracaoAsync(
                new TrilhosConfigCommand("237", "Banco Teste CC 2926", "0001", "01", "1234567", 7000, ChavePix, "PLANTAOPRÕ LTDA", CidadeBeneficiario), default);
            Assert.False(comAcento.Success);
            Assert.Contains(comAcento.Errors!, e => e.Contains("sem acento"));
            Assert.Equal(0, await ContarConfigAsync());

            // Upsert: segunda chamada atualiza a mesma linha, não cria outra.
            var primeira = await svc.SalvarConfiguracaoAsync(ComandoConfig(7000), default);
            Assert.True(primeira.Success, primeira.Message);
            Assert.Equal("237", primeira.Data!.BancoCodigo);
            Assert.Equal(7000L, primeira.Data.NossoNumeroBase);
            Assert.True(primeira.Data.Ativo, "config salva vem ativa");
            Assert.Equal(1, await ContarConfigAsync());

            var segunda = await svc.SalvarConfiguracaoAsync(ComandoConfig(7001), default);
            Assert.True(segunda.Success, segunda.Message);
            Assert.Equal(7001L, segunda.Data!.NossoNumeroBase);
            Assert.Equal(1, await ContarConfigAsync());

            // PIX estático com a configuração salva: vetor travado.
            var est = await svc.GerarPixEstaticoAsync(default);
            Assert.True(est.Success, est.Message);
            Assert.Equal(VetorPixEstatico, est.Data!.CopiaECola);
            Assert.Null(est.Data.Txid);

            // PIX dinâmico da fatura fixa: mesmo payload do vetor, txid derivado do id dela.
            var din = await svc.GerarPixFaturaAsync(FaturaFixa, default);
            Assert.True(din.Success, din.Message);
            Assert.Equal(TxidFixo, din.Data!.Txid);
            Assert.Equal(VetorPixDinamico, din.Data.CopiaECola);
            Assert.True(din.Data.Valor == 150.00m, "valor do payload PIX da fatura");

            // Bloqueios honestos: fatura paga não aceita trilho; inexistente dá 404.
            var paga = await svc.GerarPixFaturaAsync(Fpaga, default);
            Assert.False(paga.Success);
            Assert.Equal(409, paga.StatusCode);
            var inexistente = await svc.GerarPixFaturaAsync(Guid.NewGuid(), default);
            Assert.False(inexistente.Success);
            Assert.Equal(404, inexistente.StatusCode);
        }
        finally { await PurgarConfigAsync(); }
    }

    // ---------------------------------------------------------------------
    // Semente idempotente (purga + insert em uma transação), padrão B6.
    // ---------------------------------------------------------------------
    private const string PurgeFaturasSql = "delete from plantaopro.faturas_saas where cliente_id=@cliente";
    private const string PurgeClienteSql = "delete from plantaopro.clientes where id=@cliente";

    private const string InsertSql = @"
insert into plantaopro.clientes (id, razao_social, cnpj, status, reg_status) values
  (@cliente, 'R86 Operacao de Cobranca', '90861122000133', 'ATIVO', 'A');
insert into plantaopro.faturas_saas (id, cliente_id, competencia, vencimento, valor, status, reg_status) values
  (@f1, @cliente, @c1, @v1, 150.00, 'ABERTA', 'A'),
  (@f2, @cliente, @c2, @v2, 234.56, 'ABERTA', 'A'),
  (@f3, @cliente, @c3, @v3, 999.99, 'PAGA', 'A'),
  (@f4, @cliente, @c4, @v4, 100.00, 'CANCELADA', 'A'),
  (@f5, @cliente, @c5, @v5, 100.00, 'EM_CONTESTACAO', 'A'),
  (@f6, @cliente, @c6, @v6, 0, 'ABERTA', 'A'),
  (@ffixa, @cliente, @c7, @v7, 150.00, 'ABERTA', 'A'),
  (@fpaga, @cliente, @c8, @v8, 50.00, 'PAGA', 'A');";

    private static object SeedParams() => new
    {
        cliente = ClienteR86,
        f1 = F1,
        f2 = F2,
        f3 = F3,
        f4 = F4,
        f5 = F5,
        f6 = F6,
        ffixa = FaturaFixa,
        fpaga = Fpaga,
        c1 = new DateOnly(2027, 1, 1), v1 = new DateOnly(2026, 10, 30),
        c2 = new DateOnly(2027, 3, 1), v2 = new DateOnly(2026, 11, 15),
        c3 = new DateOnly(2027, 4, 1), v3 = new DateOnly(2026, 11, 20),
        c4 = new DateOnly(2027, 5, 1), v4 = new DateOnly(2026, 12, 20),
        c5 = new DateOnly(2027, 6, 1), v5 = new DateOnly(2027, 1, 20),
        c6 = new DateOnly(2027, 7, 1), v6 = new DateOnly(2027, 2, 20),
        c7 = new DateOnly(2027, 8, 1), v7 = new DateOnly(2026, 10, 30),
        c8 = new DateOnly(2027, 9, 1), v8 = new DateOnly(2027, 3, 20)
    };

    private static async Task SementarAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r86_trilhos_seed'));");
        try
        {
            await using var tx = await cn.BeginTransactionAsync();
            await cn.ExecuteAsync(PurgeFaturasSql, SeedParams(), tx);
            await cn.ExecuteAsync(PurgeClienteSql, SeedParams(), tx);
            await cn.ExecuteAsync(InsertSql, SeedParams(), tx);
            await tx.CommitAsync();
        }
        finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r86_trilhos_seed'));"); }
    }

    private static async Task PurgarAsync()
    {
        await TestInfraRetry.ExecutarComRetryImpasseAsync(async () =>
        {
            await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
            await cn.OpenAsync();
            await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r86_trilhos_seed'));");
            try
            {
                await cn.ExecuteAsync(PurgeFaturasSql, SeedParams());
                await cn.ExecuteAsync(PurgeClienteSql, SeedParams());
            }
            finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r86_trilhos_seed'));"); }
        });
    }

    private static async Task<int> ContarConfigAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        return await cn.ExecuteScalarAsync<int>("select count(*) from plantaopro.cobranca_trilhos_config");
    }
}
