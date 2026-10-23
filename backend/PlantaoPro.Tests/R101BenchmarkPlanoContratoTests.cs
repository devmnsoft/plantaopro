using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R101 (relatórios com benchmark — "seus indicadores vs o segmento" por plano): compara os
/// indicadores mensais do tenant autenticado com os pares que mantêm assinatura ativa no MESMO
/// plano — expõe somente estatísticas agregadas (mediana, média, posição e tamanho da amostra),
/// nunca valor individual de vizinho; com menos pares que a amostra mínima (3) cada indicador
/// permanece nulo e o resultado sinaliza amostra insuficiente. Período = mês calendário UTC
/// (fronteira semiaberta), escopo canônico por cliente_id, porta HTTP coberta pela permissão
/// RELATORIOS_VER do catálogo. Contrato travado: (1) o drill E2E com RESULTADO=PASSEI + o dump
/// t1 com os números fechados; (2) os fragmentos de código + a função pura de parse de mês;
/// (3) o serviço em nível de banco reproduzindo a fixture do drill, com mundo extremo em PLANO
/// diferente provando isolamento por plano e mês vazio tudo nulo; (4) sem assinatura ativa → 404.
/// </summary>
[Collection("saas-operacao-serial")]
public sealed class R101BenchmarkPlanoContratoTests
{
    private const string PastaR101 = "docs/evidencias/2026-10-23-rodada-101";
    private const string ArquivoSaida = "r101a_drill_saida.txt";
    private const string ArquivoT1 = "r101a_t1_benchmark_api.json";
    private const string PlanoCodigo = "r101_teste_plano";
    private const string PlanoXCodigo = "r101_teste_plano_x";

    // CNPJs determinísticos da rodada (fixture direta em SQL, fora da validação de cadastro).
    private const string CnpjA = "99998101000101";
    private const string CnpjC = "99998101000205";
    private const string CnpjP1 = "99998101000309";
    private const string CnpjP2 = "99998101000403";
    private const string CnpjX = "99998101000507";
    private const string CnpjM = "99998101000611";

    private static readonly string[] TodosCnpjs = { CnpjA, CnpjC, CnpjP1, CnpjP2, CnpjX, CnpjM };
    private static readonly string[] TodosPlanos = { PlanoCodigo, PlanoXCodigo };

    // =========================================================================
    // Fatos do drill (execucao canonica contra o banco vivo, API + Web no ar)
    // =========================================================================

    // Fato 1 - a execucao canonica ponta a ponta: um plano sem vinculos + dois mundos TRIAL pelo
    // funil (A ator / C controle), RELATORIOS habilitado na rota real de governanca, permissao
    // RELATORIOS.VER ao dono por excecao por usuario (guard web decide por grants do login),
    // fixture deterministica em 09/2026 fechando a amostra em 3 pares com dado, T1 com os 5
    // indicadores vs o plano, mes vazio tudo nulo, hub com o cartao novo, pagina server-side com
    // os valores reais e o controle sem o modulo negado como MODULO_NAO_CONTRATADO.
    [Fact]
    public void Drill_saida_marcadores_completos_e_resultado_passei()
    {
        var linhas = File.ReadAllLines(Caminho(PastaR101 + "/" + ArquivoSaida));
        Assert.StartsWith("R101A_DRILL data=", linhas[0]);
        Assert.DoesNotContain(linhas, l => l.StartsWith("FALHA ", StringComparison.Ordinal));

        Assert.Equal("PORTAS_LIVRES_INICIO=SIM", ObterLinha(linhas, "PORTAS_LIVRES_INICIO"));
        var api = Parsear(ObterLinha(linhas, "ESTAGEM_1_API_NO_AR "));
        Assert.Equal("SIM", api.GetOrThrow("ok"));
        Assert.True(int.TryParse(api.GetOrThrow("pid"), out var pidApi) && pidApi > 0, $"pid da API ausente: {api.GetOrThrow("pid")}.");
        var web = Parsear(ObterLinha(linhas, "ESTAGEM_1_WEB_NO_AR "));
        Assert.Equal("SIM", web.GetOrThrow("ok"));
        Assert.True(int.TryParse(web.GetOrThrow("pid"), out var pidWeb) && pidWeb > 0, $"pid da Web ausente: {web.GetOrThrow("pid")}.");

        Assert.Equal("PRE_LIMPEZA_RERUN ok=SIM", ObterLinha(linhas, "PRE_LIMPEZA_RERUN"));

        var plano = Parsear(ObterLinha(linhas, "PA_PLANO_OK "));
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", plano.GetOrThrow("plano"));
        Assert.Equal("0", plano.GetOrThrow("vinculos"));

        var mundos = Parsear(ObterLinha(linhas, "PA_MUNDOS_OK "));
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", mundos.GetOrThrow("adm_tenant"));
        Assert.Equal("TRIAL", mundos.GetOrThrow("adm_status"));
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", mundos.GetOrThrow("ctrl_tenant"));
        Assert.Equal("TRIAL", mundos.GetOrThrow("ctrl_status"));

        Assert.StartsWith("PA_RELATORIOS_OK ", ObterLinha(linhas, "PA_RELATORIOS_OK "));
        Assert.Contains("origem=CONTRATO", ObterLinha(linhas, "PA_RELATORIOS_OK "));
        Assert.Contains("historico=1", ObterLinha(linhas, "PA_RELATORIOS_OK "));
        Assert.Contains("efetivos=[RELATORIOS]", ObterLinha(linhas, "PA_RELATORIOS_OK "));

        Assert.StartsWith("PA_PERMISSAO_OK usuario=donoA", ObterLinha(linhas, "PA_PERMISSAO_OK "));
        Assert.Contains("permissao=RELATORIOS.VER", ObterLinha(linhas, "PA_PERMISSAO_OK "));

        var fix = Parsear(ObterLinha(linhas, "PEERS_FIXTURE_OK "));
        Assert.Equal("22", fix.GetOrThrow("adm_eventos"));
        Assert.Equal("6", fix.GetOrThrow("ctrl_eventos"));
        Assert.Equal("11", fix.GetOrThrow("p1_eventos"));
        Assert.Equal("30", fix.GetOrThrow("p2_eventos"));
        Assert.Equal("69", fix.GetOrThrow("total"));

        Assert.StartsWith("T1_DUMP_OK ", ObterLinha(linhas, "T1_DUMP_OK "));
        Assert.Contains("plano=R101_PLANO_DRILL", ObterLinha(linhas, "T1_DUMP_OK "));
        Assert.Contains("mes=2026-09", ObterLinha(linhas, "T1_DUMP_OK "));
        Assert.Contains("kpis=5", ObterLinha(linhas, "T1_DUMP_OK "));

        Assert.Equal("BENCHMARK_VAZIO_OK http=200 mes=2025-01 insuficiente=SIM valores_nulos=5", ObterLinha(linhas, "BENCHMARK_VAZIO_OK "));
        Assert.StartsWith("WEB_LOGIN_OK http=200", ObterLinha(linhas, "WEB_LOGIN_OK "));
        Assert.StartsWith("WEB_HUB_OK http=200 cartao=SIM", ObterLinha(linhas, "WEB_HUB_OK "));
        Assert.StartsWith("WEB_BENCHMARK_OK http=200 kpi=SIM plano=SIM valor=SIM percentil=SIM", ObterLinha(linhas, "WEB_BENCHMARK_OK "));
        Assert.StartsWith("CONTROLE_WEB_OK http=200 motivo=MODULO_NAO_CONTRATADO", ObterLinha(linhas, "CONTROLE_WEB_OK "));
        Assert.Equal("PORTAS_LIVRES=SIM", ObterLinha(linhas, "PORTAS_LIVRES="));
        Assert.Equal("RESULTADO=PASSEI", ObterLinha(linhas, "RESULTADO"));
    }

    // Fato 2 - o dump t1 da chamada canonica: os 5 indicadores do dono A em 2026-09 com os
    // numeros fechados da fixture (pares C/P1/P2 no mesmo plano; estatisticas agregadas somam
    // todos os donos com dado no mes e a contagem de pares exclui o proprio dono).
    [Fact]
    public void Drill_dump_t1_numeros_fechados_do_benchmark_por_plano()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Caminho(PastaR101 + "/" + ArquivoT1), System.Text.Encoding.UTF8));
        var raiz = doc.RootElement;
        Assert.True(raiz.GetProperty("success").GetBoolean());
        var d = raiz.GetProperty("data");
        Assert.Equal("2026-09", d.GetProperty("mesReferencia").GetString());
        Assert.Equal("R101_PLANO_DRILL", d.GetProperty("planoCodigo").GetString());
        Assert.Equal(3, d.GetProperty("amostraMinimaPeers").GetInt32());
        Assert.False(d.GetProperty("amostraInsuficiente").GetBoolean());
        Assert.True(d.GetProperty("temDadosProprios").GetBoolean());

        var inds = d.GetProperty("indicadores").EnumerateArray().ToList();
        Assert.Equal(5, inds.Count);
        AssertKpiJson(inds, "BENCH_PLANTOES_POR_MEDICO", 4.0, 3.5, 3.25, 67, 3);
        AssertKpiJson(inds, "BENCH_COBERTURA_VAGAS_PCT", 75.0, 81.25, 78.125, 33, 3);
        AssertKpiJson(inds, "BENCH_CANCELAMENTO_ESCALAS_PCT", 20.0, 10.0, 10.0, 67, 3);
        AssertKpiJson(inds, "BENCH_TEMPO_MEDIO_PREENCHIMENTO_H", 10.0, 9.0, 9.5, 67, 3);
        AssertKpiJson(inds, "BENCH_VALOR_MEDIO_PLANTAO_PAGO", 200.0, 250.0, 250.0, 33, 3);
    }

    // =========================================================================
    // Fragmentos de codigo + funcoes puras (contrato estatico)
    // =========================================================================

    // Fato 3 - o contrato da rodada esta no codigo: amostra minima fixa, agregados com
    // percentile_cont(0.5), pares = assinaturas ativas/trial do mesmo plano excluindo o proprio
    // dono, saida normalizada para float8 (determinismo do materializer), rota GET benchmark,
    // registro de dependencia, BFF server-side web e a pagina com marcador por indicador.
    [Fact]
    public void Codigo_carrega_o_contrato_do_benchmark_por_plano()
    {
        var svc = File.ReadAllText(Caminho("backend/PlantaoPro.Api/RelatorioBenchmarkService.cs"));
        Assert.Contains("public const int AmostraMinimaPeers = 3;", svc);
        Assert.Contains("percentile_cont(0.5) within group (order by v)", svc);
        Assert.Contains("pa.status in ('ATIVA','TRIAL')", svc);
        Assert.Contains("pa.cliente_id<>@cid", svc);
        Assert.Contains("count(*) filter (where dono<>@cid)", svc);
        Assert.Contains("a.status in ('ATIVA','TRIAL')", svc);
        Assert.Contains("order by (a.status<>'ATIVA') asc, a.data_inicio desc nulls last", svc);
        Assert.Contains("'BENCH_PLANTOES_POR_MEDICO'", svc);
        Assert.Contains("'BENCH_COBERTURA_VAGAS_PCT'", svc);
        Assert.Contains("'BENCH_CANCELAMENTO_ESCALAS_PCT'", svc);
        Assert.Contains("'BENCH_TEMPO_MEDIO_PREENCHIMENTO_H'", svc);
        Assert.Contains("'BENCH_VALOR_MEDIO_PLANTAO_PAGO'", svc);
        Assert.Contains("PermissionConstants.RelatoriosVer", svc);
        Assert.Contains("pf.v::float8 MeuValor", svc);
        Assert.Contains("rows.Length == 0 ? 0 : rows.Max(r => (long)r.NPeers)", svc);
        Assert.Contains("Assinatura ativa não encontrada para calcular o benchmark.", svc);

        var ctrl = File.ReadAllText(Caminho("backend/PlantaoPro.Api/Controllers/RelatoriosCentralController.cs"));
        Assert.Contains("[HttpGet(\"benchmark\")]", ctrl);
        Assert.Contains("benchmark.ObterAsync(mes, cancellationToken)", ctrl);

        var prog = File.ReadAllText(Caminho("backend/PlantaoPro.Api/Program.cs"));
        Assert.Contains("AddScoped<RelatorioBenchmarkService>();", prog);

        var bff = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Controllers/RelatoriosController.cs"));
        Assert.Contains("\"api/relatorios/benchmark\"", bff);
        Assert.Contains("View(\"Benchmark\", new PlantaoPro.Web.Models.BenchmarkPageViewModel(mes, result.Data, result.Error))", bff);

        var view = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Views/Relatorios/Benchmark.cshtml"));
        Assert.Contains("data-kpi=\"@i.Codigo\"", view);
        Assert.Contains("Amostra insuficiente neste mês.", view);
        Assert.Contains("% da amostra", view);
        Assert.Contains("Pares na amostra", view);

        var hub = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Views/Relatorios/Index.cshtml"));
        Assert.Contains("Title = \"Benchmark do plano\"", hub);
        Assert.Contains("Action = \"Benchmark\"", hub);
        Assert.Contains("Plantões por período", hub);

        // Funcao pura de parse: "yyyy-MM" exato (deterministica, sem cultura) e mes atual UTC.
        Assert.True(RelatorioBenchmarkService.TentarParsearMes("2026-09", out var p1));
        Assert.Equal(new DateOnly(2026, 9, 1), p1);
        Assert.True(RelatorioBenchmarkService.TentarParsearMes(" 2026-09 ", out var p2));
        Assert.Equal(new DateOnly(2026, 9, 1), p2);
        Assert.True(RelatorioBenchmarkService.TentarParsearMes("0026-01", out var p3));
        Assert.Equal(new DateOnly(26, 1, 1), p3);
        Assert.True(RelatorioBenchmarkService.TentarParsearMes("9999-12", out var p4));
        Assert.Equal(new DateOnly(9999, 12, 1), p4);

        var antes = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        Assert.True(RelatorioBenchmarkService.TentarParsearMes(null, out var atual));
        var depois = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        Assert.True(atual == antes || atual == depois, $"mês atual inesperado: {atual}.");
        Assert.True(RelatorioBenchmarkService.TentarParsearMes("", out var vazio));
        Assert.True(vazio == antes || vazio == depois, $"mês vazio inesperado: {vazio}.");
        Assert.True(RelatorioBenchmarkService.TentarParsearMes("   ", out var branco));
        Assert.True(branco == antes || branco == depois, $"mês em branco inesperado: {branco}.");

        foreach (var inv in new[] { "2026-13", "2026-00", "2026/09", "2026-9", "2026-091", "abcdefg", "2026-0a", "0000-01", "2026-09-05" })
        {
            Assert.False(RelatorioBenchmarkService.TentarParsearMes(inv, out _), $"parse deveria rejeitar: {inv}.");
        }
    }

    // =========================================================================
    // Servico em nivel de banco (dois planos, seis mundos deterministicos)
    // =========================================================================

    // Fato 4 - o núcleo em nível de serviço reproduzindo a fixture fechada do drill no plano de
    // teste (A ator + C/P1/P2 pares; valores do A: 4.0 por médico / cobertura 75% / cancelamento
    // 20% / preenchimento 10h / valor médio 200): os 5 indicadores chegam com mediana/média/
    // posição/pares exatos e amostra suficiente. O mundo X, em PLANO diferente, carrega valores
    // extremos (100 plantões, pagamentos de 1.000.000) e deixa os números do A intactos
    // (isolamento por plano), enquanto no próprio plano tem dados próprios mas zero pares.
    // Mês vazio (2025-01): tudo nulo e amostra insuficiente.
    [Fact]
    public async Task Servico_benchmark_pares_do_mesmo_plano_com_isolamento_e_mes_vazio()
    {
        await SementarAsync();
        var svc = NovoServico();
        try
        {
            var planoA = await PlanoDrillAsync(PlanoCodigo, "R101 Teste Plano");
            var planoX = await PlanoDrillAsync(PlanoXCodigo, "R101 Teste Plano X");
            await CriarEstruturaAsync("R101 Serv Adm", CnpjA, planoA, 2);
            await CriarEstruturaAsync("R101 Serv Ctrl", CnpjC, planoA, 1);
            await CriarEstruturaAsync("R101 Serv P1", CnpjP1, planoA, 1);
            await CriarEstruturaAsync("R101 Serv P2", CnpjP2, planoA, 4);

            // A: 2 medicos, 8 plantoes 4v/1disp (75%), 8 confirmadas + 2 canceladas (20%),
            //    preenchimento +10h, 2 pagamentos de 200 em 2026-09 -> 8/2 = 4.0 por medico.
            await InsertPlantoesAsync(CnpjA, 4, 1, "confirmado", "2026-09-01 14:00:00+00", "2026-08-25 00:00:00+00", 7);
            await InsertEscalasAsync(CnpjA, "confirmada", "2026-08-25 10:00:00+00", 10, "g.n < 8");
            await InsertEscalasAsync(CnpjA, "cancelado", "2026-08-25 10:00:00+00", 10, "g.n >= 8");
            await InsertPagamentosAsync(CnpjA, 200, "2026-09-15", 2);

            // C: 1 medico, 2 plantoes 4v/0 (100%), 2 confirmadas (0%), +8h, 1 pagamento de 300.
            await InsertPlantoesAsync(CnpjC, 4, 0, "confirmado", "2026-09-10 10:00:00+00", "2026-08-25 00:00:00+00", 1);
            await InsertEscalasAsync(CnpjC, "confirmada", "2026-08-25 08:00:00+00", 2, "true");
            await InsertPagamentosAsync(CnpjC, 300, "2026-09-12", 1);

            // P1: 1 medico, 4 plantoes (tres 4v/0 + um 4v/2 = 87.5%), 4 confirmadas + 1 cancelada
            //     (20%), +5h, 1 pagamento de 100 -> 4/1 = 4.0 por medico.
            await InsertPlantoesAsync(CnpjP1, 4, 0, "confirmado", "2026-09-05 10:00:00+00", "2026-08-20 00:00:00+00", 2);
            await PlantaoExtraP1Async(CnpjP1);
            await InsertEscalasAsync(CnpjP1, "confirmada", "2026-08-20 05:00:00+00", 5, "g.n < 4");
            await InsertEscalasAsync(CnpjP1, "cancelado", "2026-08-20 05:00:00+00", 5, "g.n >= 4");
            await InsertPagamentosAsync(CnpjP1, 100, "2026-09-20", 1);

            // P2: 4 medicos, 12 plantoes 4v/2 (50%), 12 confirmadas (0%), +15h, 2 pagamentos de
            //     400 -> 12/4 = 3.0 por medico.
            await InsertPlantoesAsync(CnpjP2, 4, 2, "confirmado", "2026-09-01 08:00:00+00", "2026-08-18 00:00:00+00", 11);
            await InsertEscalasAsync(CnpjP2, "confirmada", "2026-08-18 15:00:00+00", 12, "true");
            await InsertPagamentosAsync(CnpjP2, 400, "2026-09-25", 2);

            // X (outro plano): 5 medicos, 30 plantoes 40v/0 em todo o mes (6 por medico),
            //    30 confirmadas (+3h), 10 pagamentos de 1.000.000 - extremos que contaminariam
            //    qualquer media se o escopo escapasse do plano.
            await CriarEstruturaAsync("R101 Serv X", CnpjX, planoX, 5);
            await InsertPlantoesAsync(CnpjX, 40, 0, "confirmado", "2026-09-01 09:00:00+00", "2026-08-21 00:00:00+00", 29);
            await InsertEscalasAsync(CnpjX, "confirmada", "2026-08-21 03:00:00+00", 30, "true");
            await InsertPagamentosAsync(CnpjX, 1000000, "2026-09-15", 10);

            var cliA = await ClienteIdPorCnpjAsync(CnpjA);
            var res = await svc.CalcularAsync(cliA, new DateOnly(2026, 9, 1), CancellationToken.None);
            Assert.True(res.Success, res.Message);
            var d = res.Data!;
            Assert.Equal("2026-09", d.MesReferencia);
            Assert.Equal(PlanoCodigo, d.PlanoCodigo);
            Assert.Equal(3, d.AmostraMinimaPeers);
            Assert.False(d.AmostraInsuficiente);
            Assert.True(d.TemDadosProprios);
            AssertKpi(d, "BENCH_PLANTOES_POR_MEDICO", 4.0m, 3.5, 3.25, 67, 3);
            AssertKpi(d, "BENCH_COBERTURA_VAGAS_PCT", 75.0m, 81.25, 78.125, 33, 3);
            AssertKpi(d, "BENCH_CANCELAMENTO_ESCALAS_PCT", 20.0m, 10.0, 10.0, 67, 3);
            AssertKpi(d, "BENCH_TEMPO_MEDIO_PREENCHIMENTO_H", 10.0m, 9.0, 9.5, 67, 3);
            AssertKpi(d, "BENCH_VALOR_MEDIO_PLANTAO_PAGO", 200.0m, 250.0, 250.0, 33, 3);

            // X no proprio plano: tem dados proprios e zero pares -> amostra insuficiente;
            // como os agregados incluem o dono quando ele tem dado, mediana/media degeneram
            // para o proprio valor e a posicao na amostra fica nula (n-1 = 0).
            var cliX = await ClienteIdPorCnpjAsync(CnpjX);
            var rx = await svc.CalcularAsync(cliX, new DateOnly(2026, 9, 1), CancellationToken.None);
            Assert.True(rx.Success, rx.Message);
            var dx = rx.Data!;
            Assert.Equal(PlanoXCodigo, dx.PlanoCodigo);
            Assert.True(dx.AmostraInsuficiente);
            Assert.True(dx.TemDadosProprios);
            Assert.Equal(5, dx.Indicadores.Count);
            AssertKpiUnico(dx, "BENCH_PLANTOES_POR_MEDICO", 6.0m, 6.0);
            AssertKpiUnico(dx, "BENCH_COBERTURA_VAGAS_PCT", 100.0m, 100.0);
            AssertKpiUnico(dx, "BENCH_CANCELAMENTO_ESCALAS_PCT", 0.0m, 0.0);
            AssertKpiUnico(dx, "BENCH_TEMPO_MEDIO_PREENCHIMENTO_H", 3.0m, 3.0);
            AssertKpiUnico(dx, "BENCH_VALOR_MEDIO_PLANTAO_PAGO", 1000000.0m, 1000000.0);

            // Mês vazio: ninguem registrou nada em 2025-01 -> tudo nulo, zero pares, insuficiente.
            var rv = await svc.CalcularAsync(cliA, new DateOnly(2025, 1, 1), CancellationToken.None);
            Assert.True(rv.Success, rv.Message);
            var dv = rv.Data!;
            Assert.Equal("2025-01", dv.MesReferencia);
            Assert.Equal(PlanoCodigo, dv.PlanoCodigo);
            Assert.True(dv.AmostraInsuficiente);
            Assert.False(dv.TemDadosProprios);
            Assert.Equal(5, dv.Indicadores.Count);
            foreach (var i in dv.Indicadores)
            {
                Assert.Null(i.MeuValor);
                Assert.Null(i.MedianaPlano);
                Assert.Null(i.MediaPlano);
                Assert.Null(i.PercentilMeu);
                Assert.Equal(0, i.PeersNaAmostra);
            }
        }
        finally
        {
            await SementarAsync();
        }
    }

    // Fato 5 - sem assinatura ativa/trial vigente no plano nao ha segmento para comparar: o
    // núcleo responde 404 determinístico (mesmo com dados de negócio no mês).
    [Fact]
    public async Task Servico_sem_assinatura_ativa_retorna_404()
    {
        await SementarAsync();
        var svc = NovoServico();
        try
        {
            var plano = await PlanoDrillAsync(PlanoCodigo, "R101 Teste Plano");
            await CriarEstruturaAsync("R101 Serv Morta", CnpjM, plano, 1);
            await InsertPlantoesAsync(CnpjM, 4, 0, "confirmado", "2026-09-03 09:00:00+00", "2026-08-22 00:00:00+00", 1);
            await using (var cn = new NpgsqlConnection(TestDatabase.ConnectionString))
            {
                await cn.ExecuteAsync(@"
update plantaopro.assinaturas set status='CANCELADA'
where cliente_id=(select id from plantaopro.clientes where regexp_replace(cnpj,'\D','','g')=@cnpj)", new { cnpj = CnpjM });
            }
            var cliM = await ClienteIdPorCnpjAsync(CnpjM);
            var res = await svc.CalcularAsync(cliM, new DateOnly(2026, 9, 1), CancellationToken.None);
            Assert.False(res.Success);
            Assert.Equal(404, res.StatusCode);
            Assert.Contains("Assinatura ativa", res.Message);
        }
        finally
        {
            await SementarAsync();
        }
    }

    // =========================================================================
    // Infraestrutura local (crash-idempotente, padrao R96/R99/R100)
    // =========================================================================

    private static RelatorioBenchmarkService NovoServico() =>
        new(BuildCfg(), null!, null!);

    private static IConfiguration BuildCfg() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
        .Build();

    /// <summary>Clean completo antes de cada fato de banco (também cobre crash do fato anterior).</summary>
    private static async Task SementarAsync()
    {
        foreach (var cnpj in TodosCnpjs)
            await LimparPorCnpjAsync(cnpj);
        foreach (var codigo in TodosPlanos)
            await RemoverPlanoPorCodigoAsync(codigo);
    }

    private const string TplEstrutura = @"
insert into plantaopro.clientes(id,nome,status,cnpj,reg_status,criado_em,reg_date)
values(gen_random_uuid(),'{NOME}','ATIVO','{CNPJ}','A',now(),now());
insert into plantaopro.assinaturas(cliente_id,plano_id,status,data_inicio,valor_mensal,periodicidade,reg_status,reg_date)
select id,'{PLANO}','ATIVA',date '2026-09-01',100.00,'MENSAL','A',now() from plantaopro.clientes where cnpj='{CNPJ}';
insert into plantaopro.medicos(nome,cliente_id,status,reg_status)
select '{NOME} Medico '||n, c.id, 'ATIVO','A' from generate_series(1,{QMED}) n
cross join (select id from plantaopro.clientes where cnpj='{CNPJ}') c;";

    private const string TplPlantoes = @"
insert into plantaopro.plantoes(cliente_id,vagas,vagas_disponiveis,status,data_inicio,reg_date,reg_status)
select c.id,{VAGAS},{DISP},'{STATUS}',('{INICIO}'::timestamptz) + ((g.n||' day')::interval),'{REGDATE}'::timestamptz,'A'
from generate_series(0,{N}) as g(n) cross join (select id from plantaopro.clientes where cnpj='{CNPJ}') c;";

    private const string TplEscalas = @"
with pcl as (select array_agg(id order by data_inicio) ids from plantaopro.plantoes where cliente_id=(select id from plantaopro.clientes where cnpj='{CNPJ}')),
mcl as (select id medico_id from plantaopro.medicos where cliente_id=(select id from plantaopro.clientes where cnpj='{CNPJ}') order by id limit 1)insert into plantaopro.escalas(cliente_id,medico_id,plantao_id,status,reg_date,reg_status)
select (select id from plantaopro.clientes where regexp_replace(cnpj,'\D', '', 'g')='{CNPJ}'), m.medico_id, pc.ids[(g.n % array_length(pc.ids,1))+1],'{STATUS}','{REGESCALA}'::timestamptz,'A'
from generate_series(0,{LIMITE}-1) as g(n) cross join pcl pc cross join mcl m
where {COND};";

    private const string TplPagamentos = @"
insert into plantaopro.pagamentos(cliente_id,escala_id,medico_id,plantao_id,valor_pago,valor_previsto,status,data_pagamento,reg_status)
select e.cid, e.eid, e.mid, e.pid, {VALOR}, {VALOR}, 'pago', date '{DATA}', 'A'
from (select e.id eid, e.medico_id mid, e.plantao_id pid, p.cliente_id cid, row_number() over (order by e.reg_date, e.id) rn
      from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id
      where p.cliente_id=(select id from plantaopro.clientes where cnpj='{CNPJ}')) e
where e.rn <= {N};";

    private static async Task CriarEstruturaAsync(string nome, string cnpj, Guid plano, int qmed)
    {
        await ExecutarSql(TplEstrutura
            .Replace("{NOME}", nome)
            .Replace("{CNPJ}", cnpj)
            .Replace("{PLANO}", plano.ToString())
            .Replace("{QMED}", qmed.ToString(CultureInfo.InvariantCulture)));
    }

    private static async Task InsertPlantoesAsync(string cnpj, int vagas, int disp, string status, string inicio, string regdate, int n)
    {
        await ExecutarSql(TplPlantoes
            .Replace("{CNPJ}", cnpj)
            .Replace("{VAGAS}", vagas.ToString(CultureInfo.InvariantCulture))
            .Replace("{DISP}", disp.ToString(CultureInfo.InvariantCulture))
            .Replace("{STATUS}", status)
            .Replace("{INICIO}", inicio)
            .Replace("{REGDATE}", regdate)
            .Replace("{N}", n.ToString(CultureInfo.InvariantCulture)));
    }

    private static async Task InsertEscalasAsync(string cnpj, string status, string regescala, int limite, string cond)
    {
        await ExecutarSql(TplEscalas
            .Replace("{CNPJ}", cnpj)
            .Replace("{STATUS}", status)
            .Replace("{REGESCALA}", regescala)
            .Replace("{LIMITE}", limite.ToString(CultureInfo.InvariantCulture))
            .Replace("{COND}", cond));
    }

    private static async Task InsertPagamentosAsync(string cnpj, int valor, string data, int n)
    {
        await ExecutarSql(TplPagamentos
            .Replace("{CNPJ}", cnpj)
            .Replace("{VALOR}", valor.ToString(CultureInfo.InvariantCulture))
            .Replace("{DATA}", data)
            .Replace("{N}", n.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>O quarto plantão do P1 (4v/2) que fecha a cobertura média em 87.5%.</summary>
    private static async Task PlantaoExtraP1Async(string cnpj)
    {
        await ExecutarSql($@"
insert into plantaopro.plantoes(cliente_id,vagas,vagas_disponiveis,status,data_inicio,reg_date,reg_status)
values((select id from plantaopro.clientes where cnpj='{cnpj}'),4,2,'confirmado','2026-09-08 10:00:00+00'::timestamptz,'2026-08-20 00:00:00+00'::timestamptz,'A');");
    }

    /// <summary>Plano fixture determinístico sem vínculos (shape do catálogo vigente).</summary>
    private static async Task<Guid> PlanoDrillAsync(string codigo, string nome)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.ExecuteAsync(@"
delete from plantaopro.plano_recursos where plano_id in (select id from plantaopro.planos where upper(codigo)=upper(@codigo));
delete from plantaopro.plano_modulos where plano_id in (select id from plantaopro.planos where upper(codigo)=upper(@codigo));
delete from plantaopro.plano_faq where plano_id in (select id from plantaopro.planos where upper(codigo)=upper(@codigo));
insert into plantaopro.planos(id,nome,codigo,valor_mensal,valor_anual,status,reg_status,criado_em,reg_date,dados,
 limite_medicos,limite_hospitais,limite_plantoes_mes,limite_usuarios,limite_convites_mes,
 permite_mobile,permite_bi,permite_relatorios_avancados,permite_integracoes,permite_operacao_assistida,permite_suporte_prioritario,
 permite_api,ordem_exibicao,possui_mobile,possui_bi,possui_relatorios_avancados,permite_trial,dias_trial,metadata,permite_relatorios)
values(gen_random_uuid(),@nome,@codigo,100.00,1250.00,'ATIVO','A',now(),now(),'{}'::jsonb,
 0,0,0,0,0,false,false,false,false,false,false,false,957,false,false,false,true,5,'{}'::jsonb,false);", new { codigo, nome });
        return await cn.ExecuteScalarAsync<Guid>($"select id from plantaopro.planos where upper(codigo)=upper('{codigo}')");
    }

    private static async Task RemoverPlanoPorCodigoAsync(string codigo)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.ExecuteAsync(@"
delete from plantaopro.plano_recursos where plano_id in (select id from plantaopro.planos where upper(codigo)=upper(@codigo));
delete from plantaopro.plano_modulos where plano_id in (select id from plantaopro.planos where upper(codigo)=upper(@codigo));
delete from plantaopro.plano_faq where plano_id in (select id from plantaopro.planos where upper(codigo)=upper(@codigo));
delete from plantaopro.assinatura_historico where assinatura_id in (select id from plantaopro.assinaturas where plano_id in (select id from plantaopro.planos where upper(codigo)=upper(@codigo)));
delete from plantaopro.assinaturas where plano_id in (select id from plantaopro.planos where upper(codigo)=upper(@codigo));
delete from plantaopro.planos where upper(codigo)=upper(@codigo);", new { codigo });
    }

    private static async Task<Guid> ClienteIdPorCnpjAsync(string cnpj)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        return await cn.QuerySingleAsync<Guid>(
            "select id from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')=@cnpj", new { cnpj });
    }

    private static async Task LimparPorCnpjAsync(string cnpj)
    {
        await TestInfraRetry.ExecutarComRetryImpasseAsync(async () =>
        {
            await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
            const string cid = "(select id from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')=@cnpj)";
            const string tid = "(select id from plantaopro.tenants where cliente_id=" + cid + ")";
            await cn.ExecuteAsync($@"
delete from plantaopro.pagamentos g where g.cliente_id in {cid} or g.plantao_id in (select id from plantaopro.plantoes p where p.cliente_id in {cid});
delete from plantaopro.escalas e where e.cliente_id in {cid} or e.plantao_id in (select id from plantaopro.plantoes p where p.cliente_id in {cid});
delete from plantaopro.plantoes where cliente_id in {cid};
delete from plantaopro.medicos where cliente_id in {cid};
delete from plantaopro.cadastro_cliente_pagamentos_iniciais where assinatura_id in (select id from plantaopro.assinaturas where cliente_id in {cid});
delete from plantaopro.assinatura_historico where assinatura_id in (select id from plantaopro.assinaturas where cliente_id in {cid});
delete from plantaopro.cliente_alertas where cliente_id in {cid};
delete from plantaopro.cliente_bloqueios where cliente_id in {cid};
delete from plantaopro.assinatura_uso where cliente_id in {cid};
delete from plantaopro.faturas_saas where cliente_id in {cid};
delete from plantaopro.paineis_publicos where cliente_id in {cid};
delete from plantaopro.painel_publico_tokens where cliente_id in {cid};
delete from plantaopro.lgpd_consentimentos where cliente_id in {cid};
delete from plantaopro.tenant_onboarding_checklist where cliente_id in {cid};
delete from plantaopro.tenant_onboarding where cliente_id in {cid};
delete from plantaopro.ai_chamadas_ativas where tenant_id in {tid};
delete from plantaopro.ai_usos where tenant_id in {tid};
delete from plantaopro.ai_config where tenant_id in {tid};
delete from plantaopro.adm_obrigacoes_fiscais where tenant_id in {tid};
delete from plantaopro.tenant_modulos_historico where tenant_id in {tid};
delete from plantaopro.tenant_modulos where tenant_id in {tid};
delete from plantaopro.tenant_white_label where tenant_id in {tid};
delete from plantaopro.perfil_permissoes where perfil_id in (select id from plantaopro.perfis where tenant_id in {tid});
delete from plantaopro.perfis where tenant_id in {tid};
delete from plantaopro.notificacoes where usuario_id in (select id from plantaopro.usuarios where cliente_id in {cid});
delete from plantaopro.usuarios_perfis where usuario_id in (select id from plantaopro.usuarios where cliente_id in {cid});
delete from plantaopro.usuarios where cliente_id in {cid};
delete from plantaopro.tenants where cliente_id in {cid};
delete from plantaopro.assinaturas where cliente_id in {cid};
delete from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')=@cnpj;
delete from plantaopro.cadastro_cliente_solicitacoes where regexp_replace(cnpj,'\\D','','g')=@cnpj;", new { cnpj });
        });
    }

    private static async Task ExecutarSql(string sql)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.ExecuteAsync(sql);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static string Caminho(string relativo) =>
        Path.Combine(RepositoryPathResolver.ResolveRoot(), relativo.Replace('/', Path.DirectorySeparatorChar));

    private static string ObterLinha(string[] linhas, string prefixo)
    {
        var linha = linhas.FirstOrDefault(l => l.StartsWith(prefixo, StringComparison.Ordinal));
        Assert.NotNull(linha);
        return linha!;
    }

    /// <summary>Parsa linha chave=valor separada por espacos (formato maquina-legivel do harness).</summary>
    private static Dictionary<string, string> Parsear(string linha)
    {
        var d = new Dictionary<string, string>();
        foreach (var token in linha.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var i = token.IndexOf('=');
            if (i > 0)
            {
                d[token.Substring(0, i)] = token.Substring(i + 1);
            }
        }
        return d;
    }

    private static JsonElement ObterKpiJson(IReadOnlyList<JsonElement> inds, string codigo)
    {
        var el = inds.FirstOrDefault(e => e.GetProperty("codigo").GetString() == codigo);
        Assert.True(el.ValueKind != JsonValueKind.Undefined, $"kpi {codigo} ausente no dump.");
        return el;
    }

    private static void AssertKpiJson(IReadOnlyList<JsonElement> inds, string codigo, double meu, double mediana, double media, int percentil, int pares)
    {
        var el = ObterKpiJson(inds, codigo);
        Assert.Equal(meu, el.GetProperty("meuValor").GetDouble(), 2);
        Assert.Equal(mediana, el.GetProperty("medianaPlano").GetDouble(), 2);
        Assert.Equal(media, el.GetProperty("mediaPlano").GetDouble(), 2);
        Assert.Equal(percentil, el.GetProperty("percentilMeu").GetInt32());
        Assert.Equal(pares, el.GetProperty("peersNaAmostra").GetInt32());
    }

    private static void AssertKpi(BenchmarkResultadoDto d, string codigo, decimal meu, double mediana, double media, int percentil, int pares)
    {
        var i = d.Indicadores.FirstOrDefault(x => x.Codigo == codigo);
        Assert.NotNull(i);
        Assert.True(i!.MeuValor.HasValue, $"{codigo}: meuValor nulo.");
        Assert.True(i.MedianaPlano.HasValue, $"{codigo}: mediana nula.");
        Assert.True(i.MediaPlano.HasValue, $"{codigo}: media nula.");
        Assert.True(i.PercentilMeu.HasValue, $"{codigo}: percentil nulo.");
        Assert.Equal(meu, i.MeuValor.Value);
        Assert.Equal(mediana, i.MedianaPlano.Value);
        Assert.Equal(media, i.MediaPlano.Value);
        Assert.Equal(percentil, i.PercentilMeu.Value);
        Assert.Equal(pares, i.PeersNaAmostra);
    }

    /// <summary>Dono unico na amostra (zero pares): agregados = proprio valor, posicao nula.</summary>
    private static void AssertKpiUnico(BenchmarkResultadoDto d, string codigo, decimal meu, double estatistica)
    {
        var i = d.Indicadores.FirstOrDefault(x => x.Codigo == codigo);
        Assert.NotNull(i);
        Assert.True(i!.MeuValor.HasValue, $"{codigo}: meuValor nulo.");
        Assert.True(i.MedianaPlano.HasValue, $"{codigo}: mediana nula.");
        Assert.True(i.MediaPlano.HasValue, $"{codigo}: media nula.");
        Assert.Null(i.PercentilMeu);
        Assert.Equal(meu, i.MeuValor.Value);
        Assert.Equal(estatistica, i.MedianaPlano.Value);
        Assert.Equal(estatistica, i.MediaPlano.Value);
        Assert.Equal(0, i.PeersNaAmostra);
    }
}
// GetOrThrow: reutiliza DicionarioExtensaoR96 (mesmo namespace; duas extensoes iguais em
// arquivos distintos do mesmo namespace viram CS0121 em toda a suite).
