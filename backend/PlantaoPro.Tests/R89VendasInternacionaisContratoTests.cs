using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R89 (venda para fora do país): moeda base BRL + cotações institucionais
/// (USD/EUR/GBP, upsert por moeda+data preservando histórico), catálogo de
/// alíquotas padrão de VAT por país de destino com fonte citável por linha e
/// faturamento internacional simplificado como nota fiscal não-tradicional
/// amarrada à fatura SaaS: conversão pela cotação mais recente registrada
/// (data &lt;= hoje) e VAT do destino acrescido sobre a base convertida
/// (subtotal + imposto). Exatamente uma nota EMITIDA por fatura; anular libera
/// para reemissão. Vetores travados: conversões exatas com os seeds (5027,70 BRL
/// = 1.000,00 USD a 5.0277; 563,53 BRL = 100,00 EUR a 5.6353; 673,00 BRL =
/// 100,00 GBP a 6,73), arredondamento metade-afasta-de-zero e ciclo completo de
/// simulação/emissão/anulação com validações.
/// </summary>
public sealed class R89VendasInternacionaisContratoTests : IDisposable
{
    private const string MigrationV2363 = "database/migrations/2026_10_v2363_r80_parametros_legais.sql";
    private const string MigrationV2372 = "database/migrations/2026_10_v2372_r89_vendas_internacionais.sql";
    private const string MigrationVersion = "2026_10_v2372_r89_vendas_internacionais";

    private static readonly Guid ClienteA = Guid.Parse("b8000010-0000-4000-9000-890000000001");
    private static readonly Guid ClienteB = Guid.Parse("b8000010-0000-4000-9000-890000000002");
    private static readonly Guid UsuarioR89 = Guid.Parse("b8000010-0000-4000-9000-8900000000a1");
    private static readonly Guid FaturaA = Guid.Parse("b8000010-0000-4000-9000-890000000011"); // ABERTA 5027.70 (USD 1000)
    private static readonly Guid FaturaB = Guid.Parse("b8000010-0000-4000-9000-890000000012"); // ABERTA 100.50 (USD 19.99)
    private static readonly Guid FaturaC = Guid.Parse("b8000010-0000-4000-9000-890000000013"); // VENCIDA 563.53 (EUR 100)
    private static readonly Guid FaturaPagua = Guid.Parse("b8000010-0000-4000-9000-890000000014"); // PAGA 100.00 (fora da lista)
    private static readonly Guid FaturaCancelada = Guid.Parse("b8000010-0000-4000-9000-890000000015"); // CANCELADA (fora da lista)
    private static readonly Guid FaturaZerada = Guid.Parse("b8000010-0000-4000-9000-890000000016"); // ABERTA 0.00 (fora da lista)
    private static readonly Guid FaturaAusente = Guid.Parse("b8000010-0000-4000-9000-8900000000f9");

    private readonly Task SementePronta = SementarAsync();

    static R89VendasInternacionaisContratoTests() => DapperTypeHandlerRegistrar.RegistrarTodos();

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
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r89_vendas_internacionais_schema'));");
        try
        {
            var temParametros = await cn.ExecuteScalarAsync<bool>(
                "select exists(select 1 from information_schema.tables where table_schema='plantaopro' and table_name='parametros_legais')");
            if (!temParametros)
            {
                for (var tentativa = 1; ; tentativa++)
                {
                    try { await cn.ExecuteAsync(Read(MigrationV2363)); break; }
                    catch (PostgresException ex) when (ex.SqlState == "40P01" && tentativa < 5) { await Task.Delay(150 * tentativa); }
                }
            }
            // v2372 é idempotente: reexecução não duplica (create if not exists + on conflict / where not exists).
            await cn.ExecuteAsync(Read(MigrationV2372));
        }
        finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r89_vendas_internacionais_schema'));"); }
    }

    private static VendasInternacionaisService Svc() => new(TestDatabase.ConnectionString);

    private sealed record ParamTeste(string Codigo, string? Valor);

    // ---------------------------------------------------------------------
    // Fato 1 - manifesto com a entrada v2372 real.
    // ---------------------------------------------------------------------
    [Fact]
    public void Manifesto_tem_entrada_v2372_com_hash_real_dependencia_v2371_e_status()
    {
        var sql = Read(MigrationV2372);
        var manifest = Read("database/migration-manifest.json");

        using var doc = JsonDocument.Parse(manifest);
        var entrada = doc.RootElement.GetProperty("migrations").EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("version").GetString() == MigrationVersion);
        Assert.NotEqual(JsonValueKind.Undefined, entrada.ValueKind);

        Assert.Equal(MigrationV2372.Replace('\\', '/'), entrada.GetProperty("source").GetString());
        Assert.Equal(Sha256Lf(sql), entrada.GetProperty("checksum").GetString());
        Assert.Equal("active", entrada.GetProperty("status").GetString());
        Assert.True(entrada.GetProperty("transactional").GetBoolean());
        var deps = entrada.GetProperty("dependsOn").EnumerateArray().Select(d => d.GetString()).ToArray();
        Assert.Equal(new[] { "2026_10_v2371_r88_esocial_trabalhista" }, deps);

        foreach (var tabela in new[]
        {
            "create table if not exists plantaopro.cotacoes_moedas",
            "create table if not exists plantaopro.paises_vat",
            "create table if not exists plantaopro.notas_internacionais",
            "create unique index if not exists ux_v2372_nota_emitida_por_fatura"
        })
            Assert.Contains(tabela, sql);
        foreach (var codigo in new[] { "COTACAO_MOEDAS", "COTACAO_MOEDA_BASE", "NOTA_INTERNACIONAL_FORMA" })
            Assert.Contains(codigo, sql);
        Assert.Contains("'USD|EUR|GBP'", sql);
        Assert.Contains("'BRL'", sql);
        Assert.Contains("Lei 8.080/1990", sql);
        Assert.Contains("on conflict (codigo, ano)", sql);
    }

    // ---------------------------------------------------------------------
    // Fato 2 - schema e seeds idempotentes na fonte única, com fonte citável.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Schema_cotacoes_destinos_e_parametros_idempotentes_na_fonte_unica_com_fonte_citavel()
    {
        await SementePronta;
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();

        var usd = await cn.ExecuteScalarAsync<decimal>(
            "select taxa from plantaopro.cotacoes_moedas where moeda='USD' and data='2026-10-22'::date");
        Assert.Equal(5.027700m, usd);
        var eur = await cn.ExecuteScalarAsync<decimal>(
            "select taxa from plantaopro.cotacoes_moedas where moeda='EUR' and data='2026-10-22'::date");
        Assert.Equal(5.635300m, eur);
        var gbp = await cn.ExecuteScalarAsync<bool>(
            "select exists(select 1 from plantaopro.cotacoes_moedas where moeda='GBP' and data='2026-10-22'::date and char_length(fonte) >= 5)");
        Assert.True(gbp);

        var destinos = (await cn.QueryAsync<(string Pais, decimal Aliquota)>(@"
select pais, aliquota from plantaopro.paises_vat where reg_status='A' order by pais")).ToList();
        Assert.Equal(9, destinos.Count);
        var mapa = destinos.ToDictionary(x => x.Pais, x => x.Aliquota, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(19.00m, mapa["DE"]);
        Assert.Equal(21.00m, mapa["ES"]);
        Assert.Equal(20.00m, mapa["FR"]);
        Assert.Equal(20.00m, mapa["GB"]);
        Assert.Equal(22.00m, mapa["IT"]);
        Assert.Equal(10.00m, mapa["JP"]);
        Assert.Equal(21.00m, mapa["NL"]);
        Assert.Equal(23.00m, mapa["PT"]);
        Assert.Equal(0.00m, mapa["US"]);

        var semFonte = await cn.ExecuteScalarAsync<int>(
            "select count(*) from plantaopro.paises_vat where reg_status='A' and char_length(coalesce(fonte,'')) < 5");
        Assert.Equal(0, semFonte);

        // idempotência: reexecutar a migração não duplica cotações, destinos nem parâmetros.
        await cn.ExecuteAsync(Read(MigrationV2372));
        Assert.Equal(3, await cn.ExecuteScalarAsync<int>("select count(*) from plantaopro.cotacoes_moedas where reg_status='A'"));
        Assert.Equal(9, await cn.ExecuteScalarAsync<int>("select count(*) from plantaopro.paises_vat where reg_status='A'"));

        var parametros = (await cn.QueryAsync<ParamTeste>(@"
select codigo Codigo, valor Valor from plantaopro.parametros_legais
where codigo in ('COTACAO_MOEDAS','COTACAO_MOEDA_BASE','NOTA_INTERNACIONAL_FORMA') and reg_status='A'
order by ano desc")).ToList();
        Assert.Equal(3, parametros.Count);
        var valores = parametros.ToDictionary(p => p.Codigo, p => p.Valor ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("USD|EUR|GBP", valores["COTACAO_MOEDAS"]);
        Assert.Equal("BRL", valores["COTACAO_MOEDA_BASE"]);
        Assert.Equal("NAO_TRADICIONAL", valores["NOTA_INTERNACIONAL_FORMA"]);

        var temIndice = await cn.ExecuteScalarAsync<bool>(
            "select exists(select 1 from pg_indexes where schemaname='plantaopro' and indexname='ux_v2372_nota_emitida_por_fatura')");
        Assert.True(temIndice);
    }

    // ---------------------------------------------------------------------
    // Fato 3 - conversão BRL e VAT: vetores travados com arredondamento
    //         metade-afasta-de-zero (distingue de banker's rounding).
    // ---------------------------------------------------------------------
    [Fact]
    public void Conversao_brl_e_vat_vetores_travados_com_round_metade_afasta_de_zero()
    {
        // seeds institucionais: 5027,70 BRL a 5,0277 = exatamente 1.000,00 USD.
        Assert.Equal(1000.00m, VendasInternacionaisRegras.ConvertParaMoeda(5027.70m, 5.0277m));
        // 100,50 BRL a 5,0277 = 19,9892... -> 19,99 USD.
        Assert.Equal(19.99m, VendasInternacionaisRegras.ConvertParaMoeda(100.50m, 5.0277m));
        // 563,53 BRL a 5,6353 = exatamente 100,00 EUR; 673,00 BRL a 6,73 = 100,00 GBP.
        Assert.Equal(100.00m, VendasInternacionaisRegras.ConvertParaMoeda(563.53m, 5.6353m));
        Assert.Equal(100.00m, VendasInternacionaisRegras.ConvertParaMoeda(673.00m, 6.73m));
        // ponto médio exato: 503,3984625 / 5,0277 = 100,125 -> 100,13 (banker's daria 100,12).
        Assert.Equal(100.13m, VendasInternacionaisRegras.ConvertParaMoeda(503.3984625m, 5.0277m));

        // VAT DE 19% sobre 19,99 = 3,7981 -> 3,80; ponto médio 2,50 * 1% = 0,025 -> 0,03.
        Assert.Equal(3.80m, VendasInternacionaisRegras.ValorVat(19.99m, 19m));
        Assert.Equal(0.03m, VendasInternacionaisRegras.ValorVat(2.50m, 1m));
        Assert.Equal(20.00m, VendasInternacionaisRegras.ValorVat(100.00m, 20m));
        Assert.Equal(0.00m, VendasInternacionaisRegras.ValorVat(1000.00m, 0m));

        var notaDe = VendasInternacionaisRegras.CalcularNota(5027.70m, 5.0277m, 19m);
        Assert.Equal(1000.00m, notaDe.ValorMoeda);
        Assert.Equal(190.00m, notaDe.ValorVat);
        Assert.Equal(1190.00m, notaDe.TotalMoeda);
        var notaUs = VendasInternacionaisRegras.CalcularNota(100.50m, 5.0277m, 19m);
        Assert.Equal(19.99m, notaUs.ValorMoeda);
        Assert.Equal(3.80m, notaUs.ValorVat);
        Assert.Equal(23.79m, notaUs.TotalMoeda);

        Assert.Throws<ArgumentException>(() => VendasInternacionaisRegras.CalcularNota(-1m, 5.0277m, 19m));
        Assert.Throws<ArgumentException>(() => VendasInternacionaisRegras.CalcularNota(100m, 0m, 19m));
        Assert.Throws<ArgumentException>(() => VendasInternacionaisRegras.CalcularNota(100m, 5.0277m, 101m));

        Assert.True(VendasInternacionaisRegras.MoedaAceita("usd", "USD|EUR|GBP"));
        Assert.True(VendasInternacionaisRegras.MoedaAceita("Eur", "USD|EUR|GBP"));
        Assert.False(VendasInternacionaisRegras.MoedaAceita("BRL", "USD|EUR|GBP"));
        Assert.False(VendasInternacionaisRegras.MoedaAceita("XX", "USD|EUR|GBP"));
        Assert.False(VendasInternacionaisRegras.MoedaAceita(null, "USD|EUR|GBP"));
        Assert.True(VendasInternacionaisRegras.FormatoIso("de", 2));
        Assert.False(VendasInternacionaisRegras.FormatoIso("DEU", 2));
        Assert.False(VendasInternacionaisRegras.FormatoIso(null, 2));
    }

    // ---------------------------------------------------------------------
    // Fato 4 - registro de cotação: upsert por (moeda, data) e validações.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Registrar_cotacao_faz_upsert_por_moeda_data_e_valida_campos()
    {
        await SementePronta;
        await GarantirSchemaAsync();
        var s = Svc();
        var ct = CancellationToken.None;

        var nova = await s.RegistrarCotacaoAsync(new ViCotacaoCommand("usd", new DateOnly(2026, 10, 20), 4.9m, "taxa de teste R89"), ct);
        Assert.True(nova.Success);
        var novaLinha = nova.Data!;
        Assert.Equal("USD", novaLinha.Moeda);
        Assert.Equal(new DateOnly(2026, 10, 20), novaLinha.Data);
        Assert.Equal(4.9m, novaLinha.Taxa);

        // mesma (moeda, data) atualiza em vez de duplicar (upsert).
        var atualiza = await s.RegistrarCotacaoAsync(new ViCotacaoCommand("USD", new DateOnly(2026, 10, 20), 4.95m, null), ct);
        Assert.True(atualiza.Success);
        Assert.Equal(4.95m, atualiza.Data!.Taxa);

        var doDia = await s.ListarCotacoesAsync("USD", new DateOnly(2026, 10, 20), new DateOnly(2026, 10, 20), ct);
        Assert.True(doDia.Success);
        Assert.Single(doDia.Data!);
        Assert.Equal(4.95m, doDia.Data![0].Taxa);
        var historico = await s.ListarCotacoesAsync("USD", null, null, ct);
        Assert.True(historico.Success);
        var linhasUsd = historico.Data!;
        Assert.Contains(linhasUsd, c => c.Data == new DateOnly(2026, 10, 22) && c.Taxa == 5.027700m);
        Assert.Equal(2, linhasUsd.Count(c => c.Moeda == "USD"));
        Assert.Equal(new DateOnly(2026, 10, 22), linhasUsd[0].Data); // mais recente primeiro

        var intervaloInverso = await s.ListarCotacoesAsync("USD", new DateOnly(2026, 10, 22), new DateOnly(2026, 10, 20), ct);
        Assert.Equal(400, intervaloInverso.StatusCode);

        var moedaFora = await s.RegistrarCotacaoAsync(new ViCotacaoCommand("JPY", new DateOnly(2026, 10, 20), 4.9m, null), ct);
        Assert.Equal(400, moedaFora.StatusCode);
        var moedaBase = await s.RegistrarCotacaoAsync(new ViCotacaoCommand("BRL", new DateOnly(2026, 10, 20), 1m, null), ct);
        Assert.Equal(400, moedaBase.StatusCode);
        var taxaNula = await s.RegistrarCotacaoAsync(new ViCotacaoCommand("USD", new DateOnly(2026, 10, 20), 0m, null), ct);
        Assert.Equal(400, taxaNula.StatusCode);
        var taxaNegativa = await s.RegistrarCotacaoAsync(new ViCotacaoCommand("USD", new DateOnly(2026, 10, 20), -2m, null), ct);
        Assert.Equal(400, taxaNegativa.StatusCode);
        var taxaEnorme = await s.RegistrarCotacaoAsync(new ViCotacaoCommand("USD", new DateOnly(2026, 10, 20), 2000000m, null), ct);
        Assert.Equal(400, taxaEnorme.StatusCode);
        var dataFutura = await s.RegistrarCotacaoAsync(new ViCotacaoCommand("USD", new DateOnly(2027, 1, 1), 4.9m, null), ct);
        Assert.Equal(400, dataFutura.StatusCode);
        var dataAntiga = await s.RegistrarCotacaoAsync(new ViCotacaoCommand("USD", new DateOnly(1999, 12, 31), 4.9m, null), ct);
        Assert.Equal(400, dataAntiga.StatusCode);
    }

    // ---------------------------------------------------------------------
    // Fato 5 - faturas candidatas: somente abertas/vencidas com valor positivo,
    //         com o nome do cliente da fatura.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Faturas_candidatas_exibem_somente_abertas_ou_vencidas_com_valor_positivo()
    {
        await SementePronta;
        var s = Svc();
        var ct = CancellationToken.None;

        var lista = await s.FaturasDisponiveisAsync(100, ct);
        Assert.True(lista.Success);
        var itens = lista.Data!;
        var ids = itens.Select(f => f.Id).ToList();
        Assert.Contains(ids, f => f == FaturaA);
        Assert.Contains(ids, f => f == FaturaB);
        Assert.Contains(ids, f => f == FaturaC);
        Assert.DoesNotContain(ids, f => f == FaturaPagua);
        Assert.DoesNotContain(ids, f => f == FaturaCancelada);
        Assert.DoesNotContain(ids, f => f == FaturaZerada);

        var faturaC = itens.First(f => f.Id == FaturaC);
        Assert.Equal("VENCIDA", faturaC.Status);
        Assert.Equal(563.53m, faturaC.ValorBrl);
        Assert.Contains("Cliente Global B", faturaC.ClienteNome ?? string.Empty);
        var faturaA = itens.First(f => f.Id == FaturaA);
        Assert.Contains("Cliente Global A", faturaA.ClienteNome ?? string.Empty);
    }

    // ---------------------------------------------------------------------
    // Fato 6 - simulação da nota: conversão + VAT sem persistir, com validações.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Simular_nota_converte_pela_cotacao_aplica_vat_do_destino_sem_persistir()
    {
        await SementePronta;
        await GarantirSchemaAsync();
        var s = Svc();
        var ct = CancellationToken.None;

        var paraDe = await s.SimularNotaAsync(new ViNotaComando(FaturaA, "DE", "USD"), ct);
        Assert.True(paraDe.Success);
        var de = paraDe.Data!;
        Assert.Equal("Alemanha", de.PaisNome);
        Assert.Equal(5027.70m, de.ValorBrl);
        Assert.Equal(5.027700m, de.TaxaAplicada);
        Assert.Equal(new DateOnly(2026, 10, 22), de.DataCotacao);
        Assert.Equal(1000.00m, de.ValorMoeda);
        Assert.Equal(19.00m, de.Aliquota);
        Assert.Equal(190.00m, de.ValorVat);
        Assert.Equal(1190.00m, de.TotalMoeda);
        Assert.Equal("NAO_TRADICIONAL", de.FormaDocumento);
        Assert.NotNull(de.Fontes);
        Assert.Contains(de.Fontes, f => f.Contains("Lei 8.080/1990"));
        Assert.Contains(de.Fontes, f => f.Contains("Decreto 4.404/2002"));

        var paraGb = await s.SimularNotaAsync(new ViNotaComando(FaturaC, "GB", "EUR"), ct);
        Assert.True(paraGb.Success);
        var gb = paraGb.Data!;
        Assert.Equal(100.00m, gb.ValorMoeda);
        Assert.Equal(20.00m, gb.ValorVat);
        Assert.Equal(120.00m, gb.TotalMoeda);

        var paraUs = await s.SimularNotaAsync(new ViNotaComando(FaturaB, "US", "GBP"), ct);
        Assert.True(paraUs.Success);
        var us = paraUs.Data!;
        Assert.Equal(14.93m, us.ValorMoeda);
        Assert.Equal(0.00m, us.ValorVat);
        Assert.Equal(14.93m, us.TotalMoeda);

        var faturaAusente = await s.SimularNotaAsync(new ViNotaComando(FaturaAusente, "DE", "USD"), ct);
        Assert.Equal(404, faturaAusente.StatusCode);
        var faturaPaga = await s.SimularNotaAsync(new ViNotaComando(FaturaPagua, "DE", "USD"), ct);
        Assert.Equal(400, faturaPaga.StatusCode);
        var paisDesconhecido = await s.SimularNotaAsync(new ViNotaComando(FaturaA, "ZZ", "USD"), ct);
        Assert.Equal(400, paisDesconhecido.StatusCode);
        var paisInvalido = await s.SimularNotaAsync(new ViNotaComando(FaturaA, "ALEMANHA", "USD"), ct);
        Assert.Equal(400, paisInvalido.StatusCode);
        var moedaBrl = await s.SimularNotaAsync(new ViNotaComando(FaturaA, "DE", "BRL"), ct);
        Assert.Equal(400, moedaBrl.StatusCode);
        var moedaJpy = await s.SimularNotaAsync(new ViNotaComando(FaturaA, "DE", "JPY"), ct);
        Assert.Equal(400, moedaJpy.StatusCode);
        var semFatura = await s.SimularNotaAsync(new ViNotaComando(Guid.Empty, "DE", "USD"), ct);
        Assert.Equal(400, semFatura.StatusCode);
    }

    // ---------------------------------------------------------------------
    // Fato 7 - emissão: snapshot de cotação/VAT persistido, uma EMITIDA por
    //          fatura (409) e listagem com filtros.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Emitir_nota_persiste_snapshot_da_cotacao_e_vat_liberando_apenas_uma_por_fatura()
    {
        await SementePronta;
        await GarantirSchemaAsync();
        var s = Svc();
        var ct = CancellationToken.None;

        var ok1 = await s.EmitirNotaAsync(UsuarioR89, new ViNotaComando(FaturaB, "DE", "USD"), ct);
        Assert.True(ok1.Success);
        var emitida = ok1.Data!;
        Assert.Equal("EMITIDA", emitida.Status);
        Assert.Equal("NAO_TRADICIONAL", emitida.FormaDocumento);
        Assert.Equal("R89 Cliente Global A", emitida.ClienteNome);
        Assert.Equal(100.50m, emitida.ValorBrl);
        Assert.Equal(5.027700m, emitida.TaxaAplicada);
        Assert.Equal(new DateOnly(2026, 10, 22), emitida.DataCotacao);
        Assert.Equal(19.99m, emitida.ValorMoeda);
        Assert.Equal(3.80m, emitida.ValorVat);
        Assert.Equal(23.79m, emitida.TotalMoeda);
        Assert.Null(emitida.MotivoAnulacao);

        var duplicada = await s.EmitirNotaAsync(UsuarioR89, new ViNotaComando(FaturaB, "ES", "USD"), ct);
        Assert.Equal(409, duplicada.StatusCode);

        var ok2 = await s.EmitirNotaAsync(UsuarioR89, new ViNotaComando(FaturaC, "GB", "EUR"), ct);
        Assert.True(ok2.Success);
        var emitida2 = ok2.Data!;
        Assert.Equal(100.00m, emitida2.ValorMoeda);
        Assert.Equal(20.00m, emitida2.ValorVat);
        Assert.Equal(120.00m, emitida2.TotalMoeda);
        Assert.Contains("Cliente Global B", emitida2.ClienteNome ?? string.Empty);

        var faturaFechada = await s.EmitirNotaAsync(UsuarioR89, new ViNotaComando(FaturaCancelada, "DE", "USD"), ct);
        Assert.Equal(400, faturaFechada.StatusCode);

        var porFatura = await s.ListarNotasAsync(FaturaB, ct);
        Assert.True(porFatura.Success);
        Assert.Single(porFatura.Data!);
        Assert.Equal(emitida.Id, porFatura.Data![0].Id);

        var geral = await s.ListarNotasAsync(null, ct);
        Assert.True(geral.Success);
        Assert.Contains(geral.Data!, n => n.FaturaId == FaturaB && n.Status == "EMITIDA");
        Assert.Contains(geral.Data!, n => n.FaturaId == FaturaC && n.Status == "EMITIDA");
    }

    // ---------------------------------------------------------------------
    // Fato 8 - anulação: motivo obrigatório, 404 na segunda vez e reemissão
    //          liberada após anular.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Anular_nota_exige_motivo_libera_a_fatura_para_reemissao_e_nao_repete()
    {
        await SementePronta;
        await GarantirSchemaAsync();
        var s = Svc();
        var ct = CancellationToken.None;

        var ok = await s.EmitirNotaAsync(UsuarioR89, new ViNotaComando(FaturaA, "GB", "GBP"), ct);
        Assert.True(ok.Success);
        var emitidaA = ok.Data!;
        Assert.Equal(747.06m, emitidaA.ValorMoeda);
        Assert.Equal(149.41m, emitidaA.ValorVat);
        Assert.Equal(896.47m, emitidaA.TotalMoeda);
        var id = emitidaA.Id;

        var semMotivo = await s.AnularNotaAsync(new ViAnularNotaCommand(id, null), ct);
        Assert.Equal(400, semMotivo.StatusCode);
        var motivoLargo = await s.AnularNotaAsync(new ViAnularNotaCommand(id, new string('x', 401)), ct);
        Assert.Equal(400, motivoLargo.StatusCode);

        var anulada = await s.AnularNotaAsync(new ViAnularNotaCommand(id, "fatura duplicada de competência"), ct);
        Assert.True(anulada.Success);
        var deNovo = await s.AnularNotaAsync(new ViAnularNotaCommand(id, "segunda tentativa"), ct);
        Assert.Equal(404, deNovo.StatusCode);

        var lista = await s.ListarNotasAsync(FaturaA, ct);
        Assert.Single(lista.Data!);
        var anuladaLinha = lista.Data![0];
        Assert.Equal("ANULADA", anuladaLinha.Status);
        Assert.Equal("fatura duplicada de competência", anuladaLinha.MotivoAnulacao);

        var reemite = await s.EmitirNotaAsync(UsuarioR89, new ViNotaComando(FaturaA, "DE", "USD"), ct);
        Assert.True(reemite.Success);
        var reemitida = reemite.Data!;
        Assert.Equal("EMITIDA", reemitida.Status);
        Assert.Equal(1190.00m, reemitida.TotalMoeda);
    }

    // ---------------------------------------------------------------------
    // Fato 9 - a cotação mais recente registrada até o dia é que vence.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Cotacao_mais_recente_registrada_e_que_vence_na_emissao()
    {
        await SementePronta;
        await GarantirSchemaAsync();
        var s = Svc();
        var ct = CancellationToken.None;
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();

        try
        {
            // afasta o seed GBP (2026-10-22) para um dia anterior e registra uma
            // cotação mais recente: a emissão deve usar a mais recente (6.55).
            await cn.ExecuteAsync("update plantaopro.cotacoes_moedas set data='2026-10-15'::date, taxa=6.400000 where moeda='GBP' and reg_status='A'");
            var registrada = await s.RegistrarCotacaoAsync(new ViCotacaoCommand("GBP", new DateOnly(2026, 10, 20), 6.55m, "teste R89 taxa mais recente"), ct);
            Assert.True(registrada.Success);

            var sim = await s.SimularNotaAsync(new ViNotaComando(FaturaC, "GB", "GBP"), ct);
            Assert.True(sim.Success);
            var simulada = sim.Data!;
            Assert.Equal(6.55m, simulada.TaxaAplicada);
            Assert.Equal(new DateOnly(2026, 10, 20), simulada.DataCotacao);
            // 563,53 / 6,55 = 86,0351... -> 86.04; VAT 20% -> 17.21; total 103.25.
            Assert.Equal(86.04m, simulada.ValorMoeda);
            Assert.Equal(17.21m, simulada.ValorVat);
            Assert.Equal(103.25m, simulada.TotalMoeda);

            var historico = await s.ListarCotacoesAsync("GBP", null, null, ct);
            Assert.True(historico.Success);
            var linhas = historico.Data!;
            Assert.Equal(2, linhas.Count(c => c.Moeda == "GBP"));
            Assert.Equal(new DateOnly(2026, 10, 20), linhas[0].Data);
        }
        finally
        {
            await cn.ExecuteAsync("delete from plantaopro.cotacoes_moedas where moeda='GBP' and data='2026-10-20'::date");
            await cn.ExecuteAsync("update plantaopro.cotacoes_moedas set data='2026-10-22'::date, taxa=6.730000 where moeda='GBP' and reg_status='A'");
        }
    }

    // ---------------------------------------------------------------------
    // Sementes transacionais com locks consultivos (padrão da casa).
    // ---------------------------------------------------------------------
    private const string PurgeSql = @"
delete from plantaopro.notas_internacionais where fatura_id in (@f1, @f2, @f3, @f4, @f5, @f6);
delete from plantaopro.faturas_saas where id in (@f1, @f2, @f3, @f4, @f5, @f6);
delete from plantaopro.clientes where id in (@c1, @c2);
delete from plantaopro.cotacoes_moedas where (moeda, data) in (('USD','2026-10-20'::date),('EUR','2026-10-20'::date),('GBP','2026-10-20'::date));
update plantaopro.cotacoes_moedas set data='2026-10-22'::date, taxa=6.730000 where moeda='GBP' and data<>'2026-10-22'::date;";

    private const string InsertSql = @"
insert into plantaopro.clientes (id, razao_social, cnpj) values
  (@c1, 'R89 Cliente Global A', '11.111.111/0001-99'),
  (@c2, 'R89 Cliente Global B', '22.222.222/0001-99');
insert into plantaopro.faturas_saas (id, cliente_id, status, competencia, valor, valor_total, vencimento) values
  (@f1, @c1, 'ABERTA', @comp10, 5027.70, 5027.70, @venc1110),
  (@f2, @c1, 'ABERTA', @comp08, 100.50, 100.50, @venc1105),
  (@f3, @c2, 'VENCIDA', @comp09, 563.53, 563.53, @venc1001),
  (@f4, @c1, 'PAGA', @comp07, 100.00, 100.00, @venc1110),
  (@f5, @c1, 'CANCELADA', @comp06, 80.00, 80.00, @venc1110),
  (@f6, @c1, 'ABERTA', @comp05, 0.00, 0.00, @venc1110);";

    private static object SeedParams() => new
    {
        c1 = ClienteA,
        c2 = ClienteB,
        f1 = FaturaA,
        f2 = FaturaB,
        f3 = FaturaC,
        f4 = FaturaPagua,
        f5 = FaturaCancelada,
        f6 = FaturaZerada,
        comp10 = new DateTime(2026, 10, 1),
        comp09 = new DateTime(2026, 9, 1),
        comp08 = new DateTime(2026, 8, 1),
        comp07 = new DateTime(2026, 7, 1),
        comp06 = new DateTime(2026, 6, 1),
        comp05 = new DateTime(2026, 5, 1),
        venc1001 = new DateTime(2026, 10, 1),
        venc1105 = new DateTime(2026, 11, 5),
        venc1110 = new DateTime(2026, 11, 10)
    };

    private static async Task SementarAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r89_vendas_internacionais_seed'));");
        try
        {
            await using var tx = await cn.BeginTransactionAsync();
            await cn.ExecuteAsync(PurgeSql, SeedParams(), tx);
            await cn.ExecuteAsync(InsertSql, SeedParams(), tx);
            await tx.CommitAsync();
        }
        finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r89_vendas_internacionais_seed'));"); }
    }

    private static async Task PurgarAsync()
    {
        await TestInfraRetry.ExecutarComRetryImpasseAsync(async () =>
        {
            await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
            await cn.OpenAsync();
            await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r89_vendas_internacionais_seed'));");
            try
            {
                await cn.ExecuteAsync(PurgeSql, SeedParams());
            }
            finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r89_vendas_internacionais_seed'));"); }
        });
    }
}
