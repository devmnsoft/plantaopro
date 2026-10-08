using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R5-B6 (cobranca SaaS com provider SANDBOX), contra o PostgreSQL local
/// (plantaopro_test), no template B5: GUIDs fixos com prefixo b6, semente
/// idempotente em uma unica transcacao (purga + insert) para reruns previsiveis.
/// Cada fato usa sua propria fatura para nao depender de ordem de execucao;
/// chamadas bloqueadas nunca mutam estado, entao podem compartilhar faturas.
///
/// C1  Cobrar cria cobranca INICIADA com auditoria de evento e e idempotente
///     (segunda chamada devolve a cobranca ativa existente, sem duplicar).
/// C2  Bloqueios honestos: provider inexistente 404, inativo 409, sem
///     implementacao 409, sem credencial 503 (nada persistido antes), fatura
///     sem valor/ja paga/cancelada/em contestacao 409, fatura inexistente 404.
/// C3  HMAC: assinatura invalida da 401 sem persistir nada e NAO consome o
///     dedupe do unico (provider,evento); o evento legitimo subsequente aplica.
/// C4  PAGAMENTO_APROVADO valido liquida a fatura com os efeitos canonicos
///     (fatura PAGA + pagamento + evento + auditoria) e o replay do mesmo
///     eventoId devolve DUPLICADO sem segundo pagamento.
/// C5  Maquina de estados: recusa apos falha ignora, aprovado fora de ordem
///     ignora, valor divergente recusa sem tocar a fatura, estorno de cobranca
///     PAGA aplica e mantem a fatura paga ate decisao do financeiro.
/// C6  Simulador sandbox: GET publica so le (abrir nao muta), POST aprovado
///     percorre o MESMO pipeline e liquida a fatura; nova simulacao em
///     cobranca terminal e 409 honesto.
/// C7  Providers/lista de eventos e o contrato de TRIAL no gerar-mensal
///     (so assinaturas ATIVA geram fatura mensal).
/// </summary>
[Collection("saas-operacao-serial")]
public sealed class CobrancaB6Tests : IDisposable
{
    private const string Segredo = "b6-segredo-de-teste";

    private static readonly Guid B6Cliente = Guid.Parse("b6000000-0000-4000-9000-0000000000c1");
    // Uma fatura por cenario; nomes FA..FJ espelham os fatos abaixo.
    private static readonly Guid FA = Guid.Parse("b6000002-0000-4000-9000-0000000000a1"); // idempotencia
    private static readonly Guid FB = Guid.Parse("b6000002-0000-4000-9000-0000000000a2"); // valor zero
    private static readonly Guid FC = Guid.Parse("b6000002-0000-4000-9000-0000000000a3"); // ja paga
    private static readonly Guid FD = Guid.Parse("b6000002-0000-4000-9000-0000000000a4"); // cancelada
    private static readonly Guid FE = Guid.Parse("b6000002-0000-4000-9000-0000000000a5"); // em contestacao
    private static readonly Guid FF = Guid.Parse("b6000002-0000-4000-9000-0000000000a6"); // aprovado + replay
    private static readonly Guid FG = Guid.Parse("b6000002-0000-4000-9000-0000000000a7"); // 401 + dedupe vivo
    private static readonly Guid FH = Guid.Parse("b6000002-0000-4000-9000-0000000000a8"); // valor divergente
    private static readonly Guid FI = Guid.Parse("b6000002-0000-4000-9000-0000000000a9"); // estorno
    private static readonly Guid FJ = Guid.Parse("b6000002-0000-4000-9000-0000000000aa"); // simulacao sandbox
    private static readonly Guid FK = Guid.Parse("b6000002-0000-4000-9000-0000000000ab"); // maquina de estados (fora de ordem)
    private static readonly Guid FL = Guid.Parse("b6000002-0000-4000-9000-0000000000ac"); // linha do tempo (C7)

    private readonly Task SementePronta = SementarAsync();

    static CobrancaB6Tests() => DapperTypeHandlerRegistrar.RegistrarTodos();

    public void Dispose()
    {
        SementePronta.GetAwaiter().GetResult();
        PurgarAsync().GetAwaiter().GetResult();
    }

    // ---------------------------------------------------------------------
    // C1 - cobrar e idempotente.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task C1_Cobrar_CriaCobrancaEE_Idempotente()
    {
        await SementePronta;
        var svc = NovoServico();

        var primeira = await svc.CobrarAsync(FA, null, "127.0.0.1", default);
        Assert.True(primeira.Success, primeira.Message);
        Assert.False(primeira.Data!.JaExistia);
        Assert.StartsWith("SBX-", primeira.Data.Referencia);
        Assert.Equal("INICIADA", primeira.Data.Status);
        Assert.Contains("/api/cobranca/sandbox/", primeira.Data.CheckoutPath);
        Assert.Equal(1, await ContarAsync("select count(*) from plantaopro.cobranca_cobrancas where fatura_id=@f and status='INICIADA' and reg_status='A'", new { f = FA }));
        Assert.Equal(1, await ContarAsync("select count(*) from plantaopro.cobranca_eventos where fatura_id=@f and tipo='COBRANCA_CRIADA'", new { f = FA }));

        var segunda = await svc.CobrarAsync(FA, null, "127.0.0.1", default);
        Assert.True(segunda.Success, segunda.Message);
        Assert.True(segunda.Data!.JaExistia);
        Assert.Equal(primeira.Data.Referencia, segunda.Data.Referencia);
        var ativas = await ContarAsync("select count(*) from plantaopro.cobranca_cobrancas where fatura_id=@f and reg_status='A' and status in ('PENDENTE','INICIADA')", new { f = FA });
        Assert.True(ativas == 1, "idempotencia nao pode duplicar cobranca ativa");
    }

    // ---------------------------------------------------------------------
    // C2 - bloqueios honestos, sempre sem resíduo.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task C2_Bloqueios_NaoPersistemENemInventamSucesso()
    {
        await SementePronta;
        var svc = NovoServico();

        var inexistente = await svc.CobrarAsync(FA, "ZZNAOEX", null, default);
        Assert.False(inexistente.Success);
        Assert.Equal(404, inexistente.StatusCode);

        var inativo = await svc.CobrarAsync(FA, "B6OFF", null, default);
        Assert.False(inativo.Success);
        Assert.Equal(409, inativo.StatusCode);

        var semImpl = await svc.CobrarAsync(FA, "B6SOFTP", null, default);
        Assert.False(semImpl.Success);
        Assert.Equal(409, semImpl.StatusCode);

        var semSegredo = await NovoServico(BuildCfg(segredo: null)).CobrarAsync(FA, null, null, default);
        Assert.False(semSegredo.Success);
        Assert.Equal(503, semSegredo.StatusCode);
        Assert.Contains("Cobranca:Credenciais:SANDBOX", semSegredo.Message);

        var semValor = await svc.CobrarAsync(FB, null, null, default);
        Assert.False(semValor.Success);
        Assert.Equal(409, semValor.StatusCode);

        var jaPaga = await svc.CobrarAsync(FC, null, null, default);
        Assert.False(jaPaga.Success);
        Assert.Equal(409, jaPaga.StatusCode);

        var cancelada = await svc.CobrarAsync(FD, null, null, default);
        Assert.False(cancelada.Success);
        Assert.Equal(409, cancelada.StatusCode);

        var contestando = await svc.CobrarAsync(FE, null, null, default);
        Assert.False(contestando.Success);
        Assert.Equal(409, contestando.StatusCode);

        var Fantasma = await svc.CobrarAsync(Guid.Parse("b6000002-0000-4000-9000-00000000ffff"), null, null, default);
        Assert.False(Fantasma.Success);
        Assert.Equal(404, Fantasma.StatusCode);

        // Escopo: os bloqueios acima jamais criaram cobranca para as faturas tentadas
        // (faturas de outros cenarios legitimaemente tem cobrancas suas).
        var residuo = await ContarAsync(
            "select count(*) from plantaopro.cobranca_cobrancas where reg_status='A' and fatura_id in (@fb,@fc,@fd,@fe)",
            new { fb = FB, fc = FC, fd = FD, fe = FE });
        Assert.True(residuo == 0, "nenhum bloqueio pode deixar cobranca no banco");
    }

    // ---------------------------------------------------------------------
    // C3 - HMAC invalido: 401 honesto que nao consome o dedupe.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task C3_HmacInvalido_401_SemResiduo_E_EventoLegitimoDepoisAplica()
    {
        await SementePronta;
        var svc = NovoServico();
        var cobrar = await svc.CobrarAsync(FG, null, null, default);
        Assert.True(cobrar.Success, cobrar.Message);

        var corpo = PayloadAprovado("b6evt-fg-1", cobrar.Data!.Referencia, 250m);
        var svcOutraPraia = NovoServico(BuildCfg("segredo-diferente"));
        var recusado = await svcOutraPraia.ProcessarWebhookAsync("SANDBOX", corpo, "sha256=" + CobrancaService.HexAssinatura(Segredo, corpo), default);
        Assert.False(recusado.Success);
        Assert.Equal(401, recusado.StatusCode);
        var eventosApos401 = await ContarAsync("select count(*) from plantaopro.cobranca_webhook_eventos where evento_id='b6evt-fg-1'", new { });
        Assert.True(eventosApos401 == 0, "replay nao assinado jamais consume o dedupe de um evento legitimo futuro");

        var aplicado = await svc.ProcessarWebhookAsync("SANDBOX", corpo, "sha256=" + CobrancaService.HexAssinatura(Segredo, corpo), default);
        Assert.True(aplicado.Success, aplicado.Message);
        Assert.Equal("APLICADO", aplicado.Data!.Resultado);
        Assert.Equal("PAGA", await ConsultarAsync<string>("select status from plantaopro.faturas_saas where id=@f", new { f = FG }));
    }

    // ---------------------------------------------------------------------
    // C4 - pagamento aprovado: efeitos canonicos + replay DUPLICADO.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task C4_WebhookAprovado_LiquidaComEfeitosCanonicos_E_ReplayNaoPagaDeNovo()
    {
        await SementePronta;
        var svc = NovoServico();
        var cobrar = await svc.CobrarAsync(FF, null, null, default);
        Assert.True(cobrar.Success, cobrar.Message);
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        var corpo = PayloadAprovado("b6evt-ff-1", cobrar.Data!.Referencia, 250m, hoje);
        var primeiro = await svc.ProcessarWebhookAsync("SANDBOX", corpo, "sha256=" + CobrancaService.HexAssinatura(Segredo, corpo), default);
        Assert.True(primeiro.Success, primeiro.Message);
        Assert.Equal("APLICADO", primeiro.Data!.Resultado);

        Assert.Equal("PAGA", await ConsultarAsync<string>("select status from plantaopro.faturas_saas where id=@f", new { f = FF }));
        Assert.Equal(250m, await ConsultarAsync<decimal>("select coalesce(valor_pago,0) from plantaopro.faturas_saas where id=@f", new { f = FF }));
        Assert.Equal(hoje, await ConsultarAsync<DateOnly?>("select data_pagamento from plantaopro.faturas_saas where id=@f", new { f = FF }));
        Assert.Equal("PIX_B6", await ConsultarAsync<string>("select coalesce(forma_pagamento,'') from plantaopro.faturas_saas where id=@f", new { f = FF }));
        Assert.Equal(1, await ContarAsync("select count(*) from plantaopro.pagamentos_saas where fatura_id=@f and reg_status='A'", new { f = FF }));
        Assert.Equal(1, await ContarAsync("select count(*) from plantaopro.cobranca_eventos where fatura_id=@f and tipo='FATURA_PAGA'", new { f = FF }));
        Assert.Equal("PAGA", await ConsultarAsync<string>("select status from plantaopro.cobranca_cobrancas where fatura_id=@f and reg_status='A'", new { f = FF }));
        Assert.Equal("APLICADO", await ConsultarAsync<string>("select resultado from plantaopro.cobranca_webhook_eventos where evento_id='b6evt-ff-1'", new { }));

        var replay = await svc.ProcessarWebhookAsync("SANDBOX", corpo, "sha256=" + CobrancaService.HexAssinatura(Segredo, corpo), default);
        Assert.True(replay.Success, replay.Message);
        Assert.Equal("DUPLICADO", replay.Data!.Resultado);
        var pagamentosReplay = await ContarAsync("select count(*) from plantaopro.pagamentos_saas where fatura_id=@f and reg_status='A'", new { f = FF });
        Assert.True(pagamentosReplay == 1, "replay nao pode gerar segundo pagamento");
        Assert.Equal(1, await ContarAsync("select count(*) from plantaopro.cobranca_webhook_eventos where evento_id='b6evt-ff-1'", new { }));
    }

    // ---------------------------------------------------------------------
    // C5 - maquina de estados: fora de ordem, valor divergente, estorno.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task C5_MaquinaDeEstados_NuncaRebaixaNemInventa()
    {
        await SementePronta;
        var svc = NovoServico();

        // Recusa registrada, depois aprovado fora de ordem: nada rebaixa/liquida.
        var cobrarG = await svc.CobrarAsync(FK, null, null, default);
        Assert.True(cobrarG.Success, cobrarG.Message);
        var negado = await ExecutarWebhookAsync(svc, Payload("{\"eventoId\":\"b6evt-c5-g1\",\"tipo\":\"PAGAMENTO_NEGADO\",\"referencia\":\"" + cobrarG.Data!.Referencia + "\"}"));
        Assert.Equal("APLICADO", negado.Resultado);
        Assert.Equal("FALHA", await ConsultarAsync<string>("select status from plantaopro.cobranca_cobrancas where fatura_id=@f and reg_status='A'", new { f = FK }));
        var faturaAposRecusa = await ConsultarAsync<string>("select status from plantaopro.faturas_saas where id=@f", new { f = FK });
        Assert.True(faturaAposRecusa == "ABERTA", "recusa jamais altera a fatura");
        var aprovadoTardio = await ExecutarWebhookAsync(svc, PayloadAprovado("b6evt-c5-g2", cobrarG.Data.Referencia, 250m));
        Assert.Equal("IGNORADO", aprovadoTardio.Resultado);
        Assert.Equal("FALHA", await ConsultarAsync<string>("select status from plantaopro.cobranca_cobrancas where fatura_id=@f and reg_status='A'", new { f = FK }));

        // Valor divergente: RECUSADO sem alterar fatura nem cobranca.
        var cobrarH = await svc.CobrarAsync(FH, null, null, default);
        Assert.True(cobrarH.Success, cobrarH.Message);
        var divergente = await ExecutarWebhookAsync(svc, PayloadAprovado("b6evt-c5-h1", cobrarH.Data!.Referencia, 100m));
        Assert.Equal("RECUSADO", divergente.Resultado);
        Assert.Equal("ABERTA", await ConsultarAsync<string>("select status from plantaopro.faturas_saas where id=@f", new { f = FH }));
        Assert.Equal("INICIADA", await ConsultarAsync<string>("select status from plantaopro.cobranca_cobrancas where fatura_id=@f and reg_status='A'", new { f = FH }));
        Assert.Equal(0, await ContarAsync("select count(*) from plantaopro.pagamentos_saas where fatura_id=@f and reg_status='A'", new { f = FH }));

        // Estorno de cobranca PAGA aplica na cobranca e mantem a fatura paga (decisao do financeiro).
        var cobrarI = await svc.CobrarAsync(FI, null, null, default);
        Assert.True(cobrarI.Success, cobrarI.Message);
        await ExecutarWebhookAsync(svc, PayloadAprovado("b6evt-c5-i1", cobrarI.Data!.Referencia, 80m));
        var estorno = await ExecutarWebhookAsync(svc, Payload("{\"eventoId\":\"b6evt-c5-i2\",\"tipo\":\"ESTORNO\",\"referencia\":\"" + cobrarI.Data.Referencia + "\"}"));
        Assert.Equal("APLICADO", estorno.Resultado);
        Assert.Equal("ESTORNADA", await ConsultarAsync<string>("select status from plantaopro.cobranca_cobrancas where fatura_id=@f and reg_status='A'", new { f = FI }));
        Assert.Equal("PAGA", await ConsultarAsync<string>("select status from plantaopro.faturas_saas where id=@f", new { f = FI }));
        var segundoEstorno = await ExecutarWebhookAsync(svc, Payload("{\"eventoId\":\"b6evt-c5-i3\",\"tipo\":\"ESTORNO\",\"referencia\":\"" + cobrarI.Data.Referencia + "\"}"));
        Assert.Equal("IGNORADO", segundoEstorno.Resultado);

        // Referencia desconhecida para o provider: IGNORADO honesto (nada e inventado).
        var fantasma = await ExecutarWebhookAsync(svc, Payload("{\"eventoId\":\"b6evt-c5-x1\",\"tipo\":\"PAGAMENTO_APROVADO\",\"referencia\":\"SBX-inexistente\",\"valorPago\":10}"));
        Assert.Equal("IGNORADO", fantasma.Resultado);
    }

    // ---------------------------------------------------------------------
    // C6 - simulador sandbox: GET so le, POST aprovado liquida, terminal e 409.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task C6_SimulacaoSandbox_GetSoLe_PostAprovadoLiquida_TerminalRecusa()
    {
        await SementePronta;
        var svc = NovoServico();
        var cobrar = await svc.CobrarAsync(FJ, null, null, default);
        Assert.True(cobrar.Success, cobrar.Message);
        var referencia = cobrar.Data!.Referencia;

        var leitura = await svc.ObterPublicaAsync(referencia, default);
        Assert.True(leitura.Success, leitura.Message);
        Assert.Equal("INICIADA", leitura.Data!.Status);
        Assert.Equal(60.50m, leitura.Data.Valor);
        var leituraDeNovo = await svc.ObterPublicaAsync(referencia, default);
        Assert.Equal(leitura.Data.Status, leituraDeNovo.Data!.Status);
        Assert.Equal(60.50m, await ConsultarAsync<decimal>("select valor from plantaopro.cobranca_cobrancas where referencia=@r and reg_status='A'", new { r = referencia }));

        var simulado = await svc.SimularPagamentoAsync(referencia, true, default);
        Assert.True(simulado.Success, simulado.Message);
        Assert.Equal("APLICADO", simulado.Data!.Resultado);
        Assert.Equal("PAGA", await ConsultarAsync<string>("select status from plantaopro.faturas_saas where id=@f", new { f = FJ }));
        Assert.Equal("PIX_SANDBOX", await ConsultarAsync<string>("select forma_pagamento from plantaopro.faturas_saas where id=@f", new { f = FJ }));
        var publicaFinal = await svc.ObterPublicaAsync(referencia, default);
        Assert.Equal("PAGA", publicaFinal.Data!.Status);
        Assert.Equal("PAGA", publicaFinal.Data.FaturaStatus);

        var deNovo = await svc.SimularPagamentoAsync(referencia, true, default);
        Assert.False(deNovo.Success);
        Assert.Equal(409, deNovo.StatusCode);

        var inexistente = await svc.ObterPublicaAsync("SBX-que-nao-existe", default);
        Assert.False(inexistente.Success);
        Assert.Equal(404, inexistente.StatusCode);
    }

    // ---------------------------------------------------------------------
    // C7 - providers, linha do tempo e TRIAL no gerar-mensal.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task C7_ProvidersEventosETrial()
    {
        await SementePronta;
        var svc = NovoServico();
        var providers = await svc.ListarProvidersAsync(default);
        Assert.True(providers.Success, providers.Message);
        var sandbox = Assert.Single(providers.Data!, p => p.Codigo == "SANDBOX");
        Assert.Equal("ATIVO", sandbox.Status);
        Assert.True(sandbox.CredencialConfigurada);
        var semCredencial = (await NovoServico(BuildCfg(segredo: null)).ListarProvidersAsync(default)).Data!
            .Single(p => p.Codigo == "SANDBOX");
        Assert.False(semCredencial.CredencialConfigurada, "sem segredo no pool da API a listagem deve dizer a verdade");

        var cobrar = await svc.CobrarAsync(FL, null, null, default);
        Assert.True(cobrar.Success, cobrar.Message);
        await ExecutarWebhookAsync(svc, PayloadAprovado("b6evt-c7-f1", cobrar.Data!.Referencia, 250m));
        var eventos = await svc.EventosFaturaAsync(FL, default);
        Assert.True(eventos.Success, eventos.Message);
        Assert.Contains(eventos.Data!.Cobrancas, c => c.Status == "PAGA");
        Assert.Contains(eventos.Data.Eventos, e => e.Fonte == "financeiro" && e.Tipo == "COBRANCA_CRIADA");
        Assert.Contains(eventos.Data.Eventos, e => e.Fonte == "financeiro" && e.Tipo == "FATURA_PAGA");
        Assert.Contains(eventos.Data.Eventos, e => e.Fonte == "webhook" && e.Tipo == "PAGAMENTO_APROVADO" && e.Resultado == "APLICADO");

        var fantasma = await svc.EventosFaturaAsync(Guid.Parse("b6000002-0000-4000-9000-00000000fffe"), default);
        Assert.False(fantasma.Success);
        Assert.Equal(404, fantasma.StatusCode);

        // Contrato B6/triagem comercial: TRIAL nao gera fatura mensal (so ATIVA).
        var fonte = File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Api", "Controllers", "SaasCommercialController.cs"));
        var gerar = fonte.Substring(fonte.IndexOf("gerar-mensal", StringComparison.Ordinal));
        Assert.Contains("upper(a.status)='ATIVA'", gerar, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------
    // Infraestrutura local (semente, payloads, helpers).
    // ---------------------------------------------------------------------
    private const string SegredoCfg = CobrancaService.PrefixoCredenciais + ":SANDBOX";

    private static IConfiguration BuildCfg(string? segredo = Segredo) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = TestDatabase.ConnectionString,
            [SegredoCfg] = segredo
        })
        .Build();

    private static CobrancaService NovoServico(IConfiguration? cfg = null) =>
        new(cfg ?? BuildCfg(), new B6FakeAuditService(), new[] { new SandboxCobrancaProvider() }, NullLogger<CobrancaService>.Instance);

    private static byte[] Payload(string json) => Encoding.UTF8.GetBytes(json);

    private static string PayloadAprovadoJson(string eventoId, string referencia, decimal valor, DateOnly? data = null)
        => "{\"eventoId\":\"" + eventoId + "\",\"tipo\":\"PAGAMENTO_APROVADO\",\"referencia\":\"" + referencia +
           "\",\"valorPago\":" + valor.ToString("0.00", CultureInfo.InvariantCulture) +
           ",\"dataPagamento\":\"" + (data ?? DateOnly.FromDateTime(DateTime.UtcNow)).ToString("yyyy-MM-dd") + "\",\"formaPagamento\":\"PIX_B6\"}";

    private static byte[] PayloadAprovado(string eventoId, string referencia, decimal valor, DateOnly? data = null)
        => Payload(PayloadAprovadoJson(eventoId, referencia, valor, data));

    /// <summary>Executa o webhook como o provider faria: HMAC do corpo bruto no header.</summary>
    private static async Task<WebhookRetornoDto> ExecutarWebhookAsync(CobrancaService svc, byte[] corpo)
    {
        var r = await svc.ProcessarWebhookAsync("SANDBOX", corpo, "sha256=" + CobrancaService.HexAssinatura(Segredo, corpo), default);
        Assert.True(r.Success, r.Message);
        return r.Data!;
    }

    private static async Task<T> ConsultarAsync<T>(string sql, object parametros)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        return await cn.QuerySingleAsync<T>(sql, parametros);
    }

    private static Task<int> ContarAsync(string sql, object parametros) => ConsultarAsync<int>(sql, parametros);

    private const string PurgeSql = @"
delete from plantaopro.cobranca_webhook_eventos where provider_codigo in ('SANDBOX','B6OFF','B6SOFTP') and (referencia like 'SBX-%' or provider_codigo <> 'SANDBOX');
delete from plantaopro.cobranca_cobrancas where cliente_id=@cliente;
delete from plantaopro.cobranca_eventos where cliente_id=@cliente;
delete from plantaopro.pagamentos_saas where cliente_id=@cliente;
delete from plantaopro.faturas_saas where cliente_id=@cliente;
delete from plantaopro.cobranca_providers where codigo in ('B6OFF','B6SOFTP');
delete from plantaopro.clientes where id=@cliente;";

    private const string InsertSql = @"
insert into plantaopro.clientes (id, razao_social, cnpj, status, reg_status) values
  (@cliente, 'B6 Clinica de Cobranca', '90011122000133', 'ATIVO', 'A');
insert into plantaopro.cobranca_providers (codigo, nome, modo, status, reg_status) values
  ('B6OFF', 'B6 Provedor Inativo', 'SANDBOX', 'INATIVO', 'A'),
  ('B6SOFTP', 'B6 Provedor Sem Implementacao', 'SANDBOX', 'ATIVO', 'A');
insert into plantaopro.faturas_saas (id, cliente_id, competencia, vencimento, valor, status, reg_status) values
  (@fa, @cliente, @c1, @v1, 150, 'ABERTA', 'A'),
  (@fb, @cliente, @c2, @v2, 0, 'ABERTA', 'A'),
  (@fc, @cliente, @c3, @v3, 150, 'PAGA', 'A'),
  (@fd, @cliente, @c4, @v4, 150, 'CANCELADA', 'A'),
  (@fe, @cliente, @c5, @v5, 150, 'EM_CONTESTACAO', 'A'),
  (@ff, @cliente, @c6, @v6, 250, 'ABERTA', 'A'),
  (@fg, @cliente, @c7, @v7, 250, 'ABERTA', 'A'),
  (@fh, @cliente, @c8, @v8, 99.90, 'ABERTA', 'A'),
  (@fi, @cliente, @c9, @v9, 80, 'ABERTA', 'A'),
  (@fj, @cliente, @c10, @v10, 60.50, 'ABERTA', 'A'),
  (@fk, @cliente, @c11, @v11, 250, 'ABERTA', 'A'),
  (@fl, @cliente, @c12, @v12, 250, 'ABERTA', 'A');";

    private static object SeedParams() => new
    {
        cliente = B6Cliente,
        fa = FA,
        fb = FB,
        fc = FC,
        fd = FD,
        fe = FE,
        ff = FF,
        fg = FG,
        fh = FH,
        fi = FI,
        fj = FJ,
        fk = FK,
        fl = FL,
        // Competencias futuras para 'AtualizarVencidasAsync' de leituras canonicas nunca
        // reclassificar as faturas semeadas (o teste afirma estados exatos).
        c1 = new DateOnly(2026, 11, 1), v1 = new DateOnly(2026, 11, 20),
        c2 = new DateOnly(2026, 12, 1), v2 = new DateOnly(2026, 12, 20),
        c3 = new DateOnly(2027, 1, 1), v3 = new DateOnly(2027, 1, 20),
        c4 = new DateOnly(2027, 2, 1), v4 = new DateOnly(2027, 2, 20),
        c5 = new DateOnly(2027, 3, 1), v5 = new DateOnly(2027, 3, 20),
        c6 = new DateOnly(2027, 4, 1), v6 = new DateOnly(2027, 4, 20),
        c7 = new DateOnly(2027, 5, 1), v7 = new DateOnly(2027, 5, 20),
        c8 = new DateOnly(2027, 6, 1), v8 = new DateOnly(2027, 6, 20),
        c9 = new DateOnly(2027, 7, 1), v9 = new DateOnly(2027, 7, 20),
        c10 = new DateOnly(2027, 8, 1), v10 = new DateOnly(2027, 8, 20),
        c11 = new DateOnly(2027, 9, 1), v11 = new DateOnly(2027, 9, 20),
        c12 = new DateOnly(2027, 10, 1), v12 = new DateOnly(2027, 10, 20)
    };

    private static async Task SementarAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();
        await cn.ExecuteAsync(PurgeSql, SeedParams(), tx);
        await cn.ExecuteAsync(InsertSql, SeedParams(), tx);
        await tx.CommitAsync();
    }

    private static async Task PurgarAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync(PurgeSql, SeedParams());
    }

    private sealed class B6FakeAuditService : IAuditService
    {
        public Task RegistrarAsync(Guid? usuarioId, Guid? clienteId, string entidade, Guid? entidadeId, string acao,
            object? detalhes, bool sucesso, string? ipOrigem, string? perfil, CancellationToken ct = default) => Task.CompletedTask;

        public Task LogAsync(Guid? userId, string acao, string entidade, Guid? registroId, string descricao,
            string? valorAnterior = null, string? valorNovo = null, string? ip = null, string? userAgent = null) => Task.CompletedTask;
    }
}
