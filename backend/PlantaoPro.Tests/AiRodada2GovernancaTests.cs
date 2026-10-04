using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Ai;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// Rodada 2 IA (Brief A.2) — governança dos provedores de assistentes.
///
/// Cobre, com evidência executável sobre o banco real de teste:
///   - concorrência distribuída   -> slots em ai_chamadas_ativas (multi-instância),
///     esgotados = CONCORRENCIA sem gastar cota; slots órfãos &gt;6 min autoexpiram;
///   - cota mensal atômica        -> reserva com rollover preguiçoso e backfill de
///     sucessos do mês (upgrade/legado); 2 concorrentes com cota 1 => só 1 reserva;
///   - orçamento mensal           -> atingido OU zerado bloqueia ANTES da chamada;
///     moeda/versão de preço persistidas na auditoria;
///   - custo estimado/confirmado  -> TIMEOUT e SUCESSO sem usage viram incerto com
///     estimativa creditada ao mês; tokens completos + preço vigente viram confirmado exato;
///     TRANSPORTE sem fallback não custa nada e libera a cota;
///   - fallback                   -> usa o MODELO PRÓPRIO configurado no body da chamada;
///   - compatibilidade de modelo  -> catálogo do concorrente / aposentados bloqueiam;
///     prefixo certo mas desconhecido apenas avisa (não bloqueia);
///   - teste de conexão           -> limite de 10 testes/tenant/provedor em 24h auditados,
///     bloqueio sem chamar o provedor;
///   - prompts no servidor        -> limite de entrada aplicado com aviso contável e
///     escopo explícito (período/fuso/quantidade) visível ao usuário;
///   - reconciliação              -> só mês corrente, escopo do admin (ou global),
///     ajusta o crédito mensal do TENANT DO USO;
///   - resiliência                -> falha de auditoria não derruba a resposta.
/// </summary>
[Collection("ia-camada")]
public sealed class AiRodada2GovernancaTests : IAsyncLifetime
{
    private static readonly string Cs = TestDatabase.ConnectionString;
    private readonly HashSet<Guid> _tenants = new();

    public async Task InitializeAsync()
    {
        try
        {
            await using var cn = new NpgsqlConnection(Cs);
            await cn.OpenAsync();
        }
        catch (Exception ex)
        {
            Assert.Fail($"PostgreSQL obrigatório para os testes de governança IA não está acessível " +
                        $"(fonte: {TestDatabase.Origem}): {ex.Message}");
        }
    }

    public async Task DisposeAsync()
    {
        if (_tenants.Count == 0) return;
        try
        {
            await using var cn = new NpgsqlConnection(Cs);
            await cn.OpenAsync();
            await using var cmd = new NpgsqlCommand(@"
                DELETE FROM plantaopro.ai_chamadas_ativas WHERE tenant_id = ANY(@ids);
                DELETE FROM plantaopro.ai_usos WHERE tenant_id = ANY(@ids);
                DELETE FROM plantaopro.ai_config WHERE tenant_id = ANY(@ids);", cn);
            cmd.Parameters.AddWithValue("@ids", _tenants.ToArray());
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Best-effort: o banco já foi verificado no InitializeAsync.
        }
    }

    // ------------------------------------------------------------------
    // Concorrência distribuída (ai_chamadas_ativas)
    // ------------------------------------------------------------------

    [Fact]
    public async Task SlotsEsgotados_Concorrencia_SemCotaESemChamadaAoProvedor()
    {
        var tenant = NovoTenant();
        var current = new FakeCurrent { TenantId = tenant };
        var amb = Montar(current, groqKey: "gk-slot-1");
        await using var prov = amb.Prov;

        await amb.Gateway.SalvarConfiguracaoAsync(AiTaskCodes.MeuDiaResumo, UpdateBase(), CancellationToken.None);

        try
        {
            // Preenche TODOS os slots globais (padrão do servidor: 4).
            for (var i = 0; i < 4; i++)
                await InsertSlot(tenant, stale: false);

            var outcome = await amb.Gateway.ExecutarTarefaAsync(AiTaskCodes.MeuDiaResumo, ExecBase(tenant), CancellationToken.None);

            Assert.False(outcome.Success);
            Assert.Equal(AiErrorKinds.Concorrencia, outcome.StatusKind);
            Assert.Equal(0, amb.Groq.Chamadas);
            // A reserva de cota feita antes do slot é devolvida — nada fica consumido.
            var row = await LerConfig(amb.Repo, tenant);
            Assert.Equal(0, row.UsosMesAtual);
            Assert.Equal(0m, row.OrcamentoUsadoMes);
        }
        finally
        {
            await ExecSql("DELETE FROM plantaopro.ai_chamadas_ativas WHERE tenant_id=@T", ("@T", tenant));
        }
    }

    [Fact]
    public async Task SlotsOrfaosVelhos_Autoexpirados_NovaChamadaConsegueAbrir()
    {
        var tenant = NovoTenant();
        var current = new FakeCurrent { TenantId = tenant };
        var amb = Montar(current, groqKey: "gk-slot-2");
        await using var prov = amb.Prov;

        await amb.Gateway.SalvarConfiguracaoAsync(AiTaskCodes.MeuDiaResumo, UpdateBase(), CancellationToken.None);

        // Instância caiu sem fechar: 4 slots com mais de 6 minutos.
        for (var i = 0; i < 4; i++)
            await InsertSlot(tenant, stale: true);

        var outcome = await amb.Gateway.ExecutarTarefaAsync(AiTaskCodes.MeuDiaResumo, ExecBase(tenant), CancellationToken.None);

        Assert.True(outcome.Success);
        Assert.Equal(1, amb.Groq.Chamadas);
        // Os órfãos foram removidos por idade e o slot desta chamada foi fechado no finally.
        var restantes = await ExecSqlCount("SELECT count(*) FROM plantaopro.ai_chamadas_ativas WHERE tenant_id=@T", ("@T", tenant));
        Assert.Equal(0, restantes);
    }

    // ------------------------------------------------------------------
    // Cota mensal atômica (contadores em ai_config)
    // ------------------------------------------------------------------

    [Fact]
    public async Task RolloverPreguicoso_LinhaDeMesAntigo_ResetaParaContagemRealDoMesAtual()
    {
        var tenant = NovoTenant();
        var current = new FakeCurrent { TenantId = tenant };
        var amb = Montar(current, groqKey: "gk-roll-1");
        await using var prov = amb.Prov;

        await amb.Gateway.SalvarConfiguracaoAsync(AiTaskCodes.MeuDiaResumo, UpdateBase(), CancellationToken.None);

        // Linha travada num mês antigo com contadores "fantasmas".
        await ExecSql(@"UPDATE plantaopro.ai_config
                        SET mes_referencia = 202001, usos_mes_atual = 99, orcamento_usado_mes = 12.3456
                        WHERE tenant_id=@T AND task_code=@K",
            ("@T", tenant), ("@K", AiTaskCodes.MeuDiaResumo));
        // Sucessos REAIS do mês corrente (upgrade/legado): o rollover não pode perdê-los.
        await InsertUsoSucesso(tenant);
        await InsertUsoSucesso(tenant);

        var outcome = await amb.Gateway.ExecutarTarefaAsync(AiTaskCodes.MeuDiaResumo, ExecBase(tenant), CancellationToken.None);
        Assert.True(outcome.Success);

        var row = await LerConfig(amb.Repo, tenant);
        Assert.Equal(AiGateway.MesReferenciaAgora(), row.MesReferencia);
        // 2 sucessos do mês + 1 reserva desta chamada — e NÃO o 99 fantasma.
        Assert.Equal(3, row.UsosMesAtual);
        // O crédito do mês antigo foi zerado (modelo sem preço: nenhum crédito novo).
        Assert.Equal(0m, row.OrcamentoUsadoMes);
    }

    [Fact]
    public async Task ReservaAtomica_ComCotaUm_DoisConcorrentes_SomenteUmReserva()
    {
        var tenant = NovoTenant();
        var current = new FakeCurrent { TenantId = tenant };
        var amb = Montar(current);
        await using var prov = amb.Prov;

        await amb.Gateway.SalvarConfiguracaoAsync(AiTaskCodes.MeuDiaResumo, UpdateBase(cota: 1), CancellationToken.None);
        var mes = AiGateway.MesReferenciaAgora();

        var reservaA = amb.Repo.ReservarUsoAsync(tenant, AiTaskCodes.MeuDiaResumo, mes, CancellationToken.None);
        var reservaB = amb.Repo.ReservarUsoAsync(tenant, AiTaskCodes.MeuDiaResumo, mes, CancellationToken.None);
        var resultados = await Task.WhenAll(reservaA, reservaB);

        Assert.True(resultados[0] != resultados[1], "Com cota 1, apenas UMA das duas reservas concorrentes pode passar.");

        // Liberadas, a cota volta a 0 e uma terceira reserva passa normalmente.
        await amb.Repo.LiberarUsoAsync(tenant, AiTaskCodes.MeuDiaResumo, mes, CancellationToken.None);
        await amb.Repo.LiberarUsoAsync(tenant, AiTaskCodes.MeuDiaResumo, mes, CancellationToken.None);
        var row = await LerConfig(amb.Repo, tenant);
        Assert.Equal(0, row.UsosMesAtual);
        Assert.True(await amb.Repo.ReservarUsoAsync(tenant, AiTaskCodes.MeuDiaResumo, mes, CancellationToken.None));
        await amb.Repo.LiberarUsoAsync(tenant, AiTaskCodes.MeuDiaResumo, mes, CancellationToken.None);
    }

    [Fact]
    public async Task OrcamentoMensalAtingido_OuZerado_BloqueiaAntesDeChamarProvedor()
    {
        var tenant = NovoTenant();
        var current = new FakeCurrent { TenantId = tenant };
        var amb = Montar(current, groqKey: "gk-orca-1");
        await using var prov = amb.Prov;

        await amb.Gateway.SalvarConfiguracaoAsync(AiTaskCodes.MeuDiaResumo,
            UpdateBase(orcamento: 1.00m), CancellationToken.None);
        var mes = AiGateway.MesReferenciaAgora();

        // Crédito do mês já consumiu todo o orçamento.
        await ExecSql(@"UPDATE plantaopro.ai_config
                        SET mes_referencia=@M, orcamento_usado_mes=1.0
                        WHERE tenant_id=@T AND task_code=@K",
            ("@M", mes), ("@T", tenant), ("@K", AiTaskCodes.MeuDiaResumo));

        var excedido = await amb.Gateway.ExecutarTarefaAsync(AiTaskCodes.MeuDiaResumo, ExecBase(tenant), CancellationToken.None);
        Assert.False(excedido.Success);
        Assert.Equal(AiErrorKinds.OrcamentoExcedido, excedido.StatusKind);
        Assert.Contains("Orçamento", excedido.Mensagem);
        Assert.Equal(0, amb.Groq.Chamadas);

        // Orçamento zerado também bloqueia tudo (comportamento estrito documentado).
        await ExecSql(@"UPDATE plantaopro.ai_config
                        SET orcamento_mensal=0, orcamento_usado_mes=0
                        WHERE tenant_id=@T AND task_code=@K",
            ("@T", tenant), ("@K", AiTaskCodes.MeuDiaResumo));
        var zerado = await amb.Gateway.ExecutarTarefaAsync(AiTaskCodes.MeuDiaResumo, ExecBase(tenant), CancellationToken.None);
        Assert.Equal(AiErrorKinds.OrcamentoExcedido, zerado.StatusKind);
        Assert.Equal(0, amb.Groq.Chamadas);

        var row = await LerConfig(amb.Repo, tenant);
        Assert.Equal(0, row.UsosMesAtual); // nem reserva nem slot foram abertos
    }

    // ------------------------------------------------------------------
    // Custo estimado/confirmado (preços versionados em ai_precos_modelos)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Timeout_CustoIncertoEstimado_CreditadoAoMesComVersaoDoPreco()
    {
        var tenant = NovoTenant();
        var current = new FakeCurrent { TenantId = tenant };
        var lento = new StubHttp(async (req, corpo, ct) =>
        {
            await Task.Delay(150);
            throw new TaskCanceledException("tempo limite excedido");
        });
        var amb = Montar(current, groq: lento, groqKey: "gk-to-1");
        await using var prov = amb.Prov;

        await amb.Gateway.SalvarConfiguracaoAsync(AiTaskCodes.MeuDiaResumo,
            UpdateBase(modelo: "gpt-oss-20b"), CancellationToken.None);

        var preco = await amb.Repo.ObterPrecoVigenteAsync("groq", "gpt-oss-20b", CancellationToken.None);
        Assert.NotNull(preco); // seed v2308 presente no banco de teste

        var outcome = await amb.Gateway.ExecutarTarefaAsync(AiTaskCodes.MeuDiaResumo, ExecBase(tenant), CancellationToken.None);
        Assert.False(outcome.Success);
        Assert.Equal(AiErrorKinds.Timeout, outcome.StatusKind);
        Assert.True(outcome.FallbackUsado == false);

        // Estimativa documentada: ~4 chars/token na entrada, metade do teto na saída.
        var chars = LenPromptUsuario(amb.Groq);
        var esperado = AiGateway.CalcularCusto(preco!, AiGateway.EstimarTokens(chars), Math.Max(1, 1200 / 2));

        var custo = await LerCustoUso(tenant);
        Assert.Equal(esperado, custo.Est);
        Assert.Null(custo.Conf);
        Assert.True(custo.Incerto);
        Assert.Equal(preco!.Moeda, custo.Moeda);
        Assert.NotNull(custo.Versao); // a versão do preço usada fica auditada

        // O crédito incerto alimenta o orçamento do mês (a reconciliação corrige depois).
        var row = await LerConfig(amb.Repo, tenant);
        Assert.Equal(esperado, row.OrcamentoUsadoMes);
    }

    [Fact]
    public async Task Sucesso_TokensCompletosEPrecoVigente_CustoConfirmadoExato()
    {
        var tenant = NovoTenant();
        var current = new FakeCurrent { TenantId = tenant };
        var corpo = OkOpenAiCom(modelo: "gpt-oss-20b", tokensIn: 1_000_000, tokensOut: 2_000_000);
        var amb = Montar(current, groq: Status(200, corpo), groqKey: "gk-ok-1");
        await using var prov = amb.Prov;

        await amb.Gateway.SalvarConfiguracaoAsync(AiTaskCodes.MeuDiaResumo,
            UpdateBase(modelo: "gpt-oss-20b"), CancellationToken.None);
        var preco = await amb.Repo.ObterPrecoVigenteAsync("groq", "gpt-oss-20b", CancellationToken.None);
        Assert.NotNull(preco);

        var outcome = await amb.Gateway.ExecutarTarefaAsync(AiTaskCodes.MeuDiaResumo, ExecBase(tenant), CancellationToken.None);
        Assert.True(outcome.Success);
        Assert.Equal(1_000_000, outcome.TokensIn);

        var esperado = AiGateway.CalcularCusto(preco!, 1_000_000, 2_000_000);
        var custo = await LerCustoUso(tenant);
        Assert.Equal(esperado, custo.Conf);
        Assert.Null(custo.Est); // confirmado: não há estimativa pendente
        Assert.False(custo.Incerto);
        Assert.NotNull(custo.Versao);

        var row = await LerConfig(amb.Repo, tenant);
        Assert.Equal(esperado, row.OrcamentoUsadoMes);
    }

    [Fact]
    public async Task Sucesso_ProvedorNaoinformouUsage_CustoEstimadoIncertoECreditado()
    {
        var tenant = NovoTenant();
        var current = new FakeCurrent { TenantId = tenant };
        var amb = Montar(current, groq: Status(200, CorpoSemUsage), groqKey: "gk-ok-2");
        await using var prov = amb.Prov;

        await amb.Gateway.SalvarConfiguracaoAsync(AiTaskCodes.MeuDiaResumo,
            UpdateBase(modelo: "gpt-oss-20b"), CancellationToken.None);
        var preco = await amb.Repo.ObterPrecoVigenteAsync("groq", "gpt-oss-20b", CancellationToken.None);
        Assert.NotNull(preco);

        var outcome = await amb.Gateway.ExecutarTarefaAsync(AiTaskCodes.MeuDiaResumo, ExecBase(tenant), CancellationToken.None);
        Assert.True(outcome.Success);

        var chars = LenPromptUsuario(amb.Groq);
        var esperado = AiGateway.CalcularCusto(preco!, AiGateway.EstimarTokens(chars), Math.Max(1, 1200 / 2));
        var custo = await LerCustoUso(tenant);
        Assert.Equal(esperado, custo.Est);
        Assert.Null(custo.Conf);
        Assert.True(custo.Incerto); // processou, mas o provedor não reportou tokens: honestamente incerto

        var row = await LerConfig(amb.Repo, tenant);
        Assert.Equal(esperado, row.OrcamentoUsadoMes);
    }

    [Fact]
    public async Task FalhaDeTransporte_SemFallback_LiberaCotaESemCredito()
    {
        var tenant = NovoTenant();
        var current = new FakeCurrent { TenantId = tenant };
        var amb = Montar(current, groq: Status(500, "falha interna"), groqKey: "gk-tr-1");
        await using var prov = amb.Prov;

        await amb.Gateway.SalvarConfiguracaoAsync(AiTaskCodes.MeuDiaResumo,
            UpdateBase(modelo: "gpt-oss-20b"), CancellationToken.None);
        var outcome = await amb.Gateway.ExecutarTarefaAsync(AiTaskCodes.MeuDiaResumo, ExecBase(tenant), CancellationToken.None);

        Assert.False(outcome.Success);
        Assert.Equal(AiErrorKinds.Transporte, outcome.StatusKind);
        Assert.Equal(1, amb.Groq.Chamadas);

        // Transporte: a chamada não chegou a ser processada — não fatura e libera tudo.
        var custo = await LerCustoUso(tenant);
        Assert.Null(custo.Est);
        Assert.Null(custo.Conf);
        Assert.False(custo.Incerto);
        var row = await LerConfig(amb.Repo, tenant);
        Assert.Equal(0, row.UsosMesAtual);
        Assert.Equal(0m, row.OrcamentoUsadoMes);
    }

    // ------------------------------------------------------------------
    // Fallback com modelo próprio + compatibilidade de modelos
    // ------------------------------------------------------------------

    [Fact]
    public async Task Fallback_ModeloProprioConfigurado_ViaNoBodyDaChamada()
    {
        var tenant = NovoTenant();
        var current = new FakeCurrent { TenantId = tenant };
        var amb = Montar(current,
            groq: Status(500, "falha interna"),
            groqKey: "gk-fb-1", deepseekKey: "dk-fb-1");
        await using var prov = amb.Prov;

        await amb.Gateway.SalvarConfiguracaoAsync(AiTaskCodes.MeuDiaResumo,
            UpdateBase(fallback: "deepseek", fallbackModelo: "deepseek-flash"), CancellationToken.None);
        var outcome = await amb.Gateway.ExecutarTarefaAsync(AiTaskCodes.MeuDiaResumo, ExecBase(tenant), CancellationToken.None);

        Assert.True(outcome.Success);
        Assert.Equal("deepseek", outcome.Provedor);
        Assert.True(outcome.FallbackUsado);
        Assert.Equal(1, amb.Groq.Chamadas);
        Assert.Equal(1, amb.DeepSeek.Chamadas);
        // O fallback NÃO reusa o modelo do principal: vai o modelo próprio configurado.
        Assert.Contains("\"model\":\"deepseek-flash\"", amb.DeepSeek.UltimoCorpo!, StringComparison.OrdinalIgnoreCase);

        var usos = await UsosDoTenant(tenant, AiTaskCodes.MeuDiaResumo);
        Assert.Contains(usos, u => u.Status == "SUCESSO" && u.Provedor == "deepseek");
    }

    [Fact]
    public async Task CompatibilidadeDeModelo_IncompativelBloqueia_EEstranhoDoCatalogoAvisa()
    {
        var tenant = NovoTenant();
        var current = new FakeCurrent { TenantId = tenant };
        var amb = Montar(current, encryptionKey: KeyMestraHex());
        await using var prov = amb.Prov;

        // Catálogo do concorrente nunca chega à configuração.
        var exGroq = await Assert.ThrowsAsync<AiConfigException>(() => amb.Gateway.SalvarConfiguracaoAsync(
            AiTaskCodes.MeuDiaResumo, UpdateBase(provedor: "groq", modelo: "gemini-2.5-flash"), CancellationToken.None));
        Assert.Contains("Gemini", exGroq.Message);

        var exGemini = await Assert.ThrowsAsync<AiConfigException>(() => amb.Gateway.SalvarConfiguracaoAsync(
            AiTaskCodes.MeuDiaResumo, UpdateBase(provedor: "gemini", modelo: "gpt-oss-20b"), CancellationToken.None));
        Assert.Contains("prefixo", exGemini.Message);

        // Aposentado pelo provedor (deepseek-chat/-reasoner, 2026-07-24).
        var exDeep = await Assert.ThrowsAsync<AiConfigException>(() => amb.Gateway.SalvarConfiguracaoAsync(
            AiTaskCodes.MeuDiaResumo, UpdateBase(provedor: "deepseek", modelo: "deepseek-chat"), CancellationToken.None));
        Assert.Contains("aposentado", exDeep.Message);

        // Modelo próprio do fallback incompatível com o provedor de destino.
        var exFb = await Assert.ThrowsAsync<AiConfigException>(() => amb.Gateway.SalvarConfiguracaoAsync(
            AiTaskCodes.MeuDiaResumo,
            UpdateBase(fallback: "deepseek", fallbackModelo: "deepseek-reasoner"), CancellationToken.None));
        Assert.Contains("Fallback", exFb.Message);

        // Prefixo certo mas fora do catálogo documentado: SALVA e avisa (não bloqueia).
        var view = await amb.Gateway.SalvarConfiguracaoAsync(AiTaskCodes.MeuDiaResumo,
            UpdateBase(provedor: "groq", modelo: "nova-familia-x"), CancellationToken.None);
        Assert.NotNull(view.AvisoModelo);
        Assert.Contains("catálogo", view.AvisoModelo);
    }

    // ------------------------------------------------------------------
    // Teste de conexão: limite auditado por tenant/provedor em 24h
    // ------------------------------------------------------------------

    [Fact]
    public async Task TestesDeConexao_NoveEm24hPassam_DezeBloqueiamSemHTTP()
    {
        // 9 tentativas registradas: a décima ainda passa (e fica auditada).
        var tenantA = NovoTenant();
        var ambA = Montar(new FakeCurrent { TenantId = tenantA }, groqKey: "gk-tc-a");
        await using (ambA.Prov)
        {
            for (var i = 0; i < 9; i++)
                await InsertTesteConexao(tenantA, "groq");
            var ok = await ambA.Gateway.TestarConexaoAsync("groq", CancellationToken.None);
            Assert.True(ok.Success);
            Assert.Equal(1, ambA.Groq.Chamadas);
            Assert.Equal(10, await ambA.Repo.ContarTestesConexao24hAsync(tenantA, "groq", CancellationToken.None));
        }

        // 10 tentativas registradas: bloqueio SEM chamar o provedor.
        var tenantB = NovoTenant();
        var ambB = Montar(new FakeCurrent { TenantId = tenantB }, groqKey: "gk-tc-b");
        await using (ambB.Prov)
        {
            for (var i = 0; i < 10; i++)
                await InsertTesteConexao(tenantB, "groq");
            var bloqueado = await ambB.Gateway.TestarConexaoAsync("groq", CancellationToken.None);
            Assert.False(bloqueado.Success);
            Assert.Equal(AiErrorKinds.TesteLimite, bloqueado.StatusKind);
            Assert.Equal(0, ambB.Groq.Chamadas);
            // O bloqueio em si não consome tentativa adicional.
            Assert.Equal(10, await ambB.Repo.ContarTestesConexao24hAsync(tenantB, "groq", CancellationToken.None));
        }
    }

    // ------------------------------------------------------------------
    // Aritmética pura (deadline total, mês de referência, estimativa de tokens)
    // ------------------------------------------------------------------

    [Fact]
    public void DeadlineTotal_MesReferenciaEEstimativa_PurosEDeterministicos()
    {
        // Deadline TOTAL = timeout × tentativas (principal + fallback), teto 120 s.
        Assert.Equal(10, AiGateway.DeadlineTotalS(5));      // mínimo clampado em 5 s
        Assert.Equal(10, AiGateway.DeadlineTotalS(0));      // 0 → clampado para 5 s
        Assert.Equal(60, AiGateway.DeadlineTotalS(30));     // 30 s × 2 tentativas
        Assert.Equal(120, AiGateway.DeadlineTotalS(60));    // teto de 120 s
        Assert.Equal(120, AiGateway.DeadlineTotalS(90));
        Assert.Equal(30, AiGateway.DeadlineTotalS(30, 1));  // sem fallback: só 1 tentativa

        // Mês de referência em America/Sao_Paulo (UTC−3 fixo): o mês só muda ao
        // cruzar a primeira DIÁRIA do mês — 03:00 UTC quando caem no dia 1.
        Assert.Equal(202610, AiGateway.MesReferenciaAgora(new DateTimeOffset(2026, 10, 4, 02, 59, 0, TimeSpan.Zero)));
        Assert.Equal(202610, AiGateway.MesReferenciaAgora(new DateTimeOffset(2026, 11, 1, 02, 59, 0, TimeSpan.Zero))); // 31/10 23:59 SP
        Assert.Equal(202611, AiGateway.MesReferenciaAgora(new DateTimeOffset(2026, 11, 1, 03, 00, 0, TimeSpan.Zero))); // 01/11 00:00 SP
        Assert.Equal(202612, AiGateway.MesReferenciaAgora(new DateTimeOffset(2026, 12, 31, 02, 30, 0, TimeSpan.Zero)));

        // Heurística documentada: ~4 caracteres por token (mínimo 1).
        Assert.Equal(1, AiGateway.EstimarTokens(0));
        Assert.Equal(1, AiGateway.EstimarTokens(7));
        Assert.Equal(2, AiGateway.EstimarTokens(8));
    }

    // ------------------------------------------------------------------
    // Prompts no servidor: limite de entrada aplicado + escopo explícito
    // ------------------------------------------------------------------

    [Fact]
    public async Task MontarPrompts_LimiteDeEntradaTruncaComAvisoContavel_EEscopoExposto()
    {
        var tenant = Guid.NewGuid();
        var agoraSp = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-3)).ToString("dd/MM/yyyy HH:mm");

        // Direto no montador: 30 linhas longas com orçamento de 256 tokens (~1024 chars).
        var linhas = Enumerable.Range(1, 30).Select(i => $"pendência {i}: " + new string('x', 380)).ToList();
        var exec = new AiTaskExecution(tenant, Guid.NewGuid(), "PENDENCIAS_MEU_DIA", null,
            "Resuma as pendências.", linhas,
            Escopo: $"Pendências com prazo entre {agoraSp} e {agoraSp} (America/Sao_Paulo); 25 de 25 itens");

        var (_, usuario, truncou) = AiGateway.MontarPrompts(exec, 256);
        Assert.True(truncou);
        Assert.Contains("(truncados por limite de entrada)", usuario);
        Assert.Contains("e mais", usuario);
        Assert.Contains($"Escopo da análise: Pendências com prazo entre {agoraSp}", usuario);

        var (_, usuarioSemEscopo, _) = AiGateway.MontarPrompts(
            new AiTaskExecution(tenant, Guid.NewGuid(), "X", null, "Instrução.", linhas), 256);
        Assert.DoesNotContain("Escopo da análise:", usuarioSemEscopo);

        // De ponta a ponta: a truncagem chega ao provider exatamente como exibida à UI.
        var current = new FakeCurrent { TenantId = tenant };
        var amb = Montar(current, groqKey: "gk-trunc-1");
        await using var prov = amb.Prov;
        try
        {
            await amb.Gateway.SalvarConfiguracaoAsync(AiTaskCodes.MeuDiaResumo,
                UpdateBase(limiteEntrada: 256), CancellationToken.None);
            var outcome = await amb.Gateway.ExecutarTarefaAsync(AiTaskCodes.MeuDiaResumo,
                new AiTaskExecution(tenant, Guid.NewGuid(), "TESTE", null, "Instrução.", linhas, Escopo: "janela 24h (America/Sao_Paulo)"),
                CancellationToken.None);
            Assert.True(outcome.Success);
            Assert.Equal("janela 24h (America/Sao_Paulo)", outcome.Escopo);
            var enviado = LenConteudoUsuario(amb.Groq);
            Assert.Contains("(truncados por limite de entrada)", enviado);
            Assert.Contains("Escopo da análise: janela 24h", enviado);
        }
        finally
        {
            _tenants.Add(tenant); // rastreado p/ limpeza no DisposeAsync
        }
    }

    // ------------------------------------------------------------------
    // Reconciliação de custos incertos
    // ------------------------------------------------------------------

    [Fact]
    public async Task Reconciliacao_UsoIncertoDoMes_ConfirmaCustoEAjustaCreditoDoTenantDoUso()
    {
        var t1 = NovoTenant();
        var t2 = NovoTenant();
        var mes = AiGateway.MesReferenciaAgora();
        var current = new FakeCurrent { TenantId = t1 };
        var amb = Montar(current);
        await using var prov = amb.Prov;

        // Linhas de config + créditos mensais pré-existentes nos dois tenants.
        await amb.Gateway.SalvarConfiguracaoAsync(AiTaskCodes.MeuDiaResumo, UpdateBase(), CancellationToken.None);
        current.TenantId = t2;
        await amb.Gateway.SalvarConfiguracaoAsync(AiTaskCodes.MeuDiaResumo, UpdateBase(), CancellationToken.None);
        await ExecSql(@"UPDATE plantaopro.ai_config SET mes_referencia=@M, orcamento_usado_mes=0.50
                        WHERE tenant_id=@T AND task_code=@K", ("@M", mes), ("@T", t1), ("@K", AiTaskCodes.MeuDiaResumo));
        await ExecSql(@"UPDATE plantaopro.ai_config SET mes_referencia=@M, orcamento_usado_mes=0.10
                        WHERE tenant_id=@T AND task_code=@K", ("@M", mes), ("@T", t2), ("@K", AiTaskCodes.MeuDiaResumo));

        var usoA = Guid.NewGuid(); // T1: estimado 0.50
        var usoB = Guid.NewGuid(); // T2: estimado 0.10
        var usoC = Guid.NewGuid(); // T1: estimado 0.05
        await InsertUso(usoA, t1, "groq", AiErrorKinds.Timeout, 0.50m);
        await InsertUso(usoB, t2, "groq", AiErrorKinds.Timeout, 0.10m);
        await InsertUso(usoC, t1, "groq", AiErrorKinds.Timeout, 0.05m);

        // Admin vê a lista global de pendências.
        var lista = await amb.Gateway.ListarUsosIncertosAsync(CancellationToken.None);
        Assert.Contains(lista, u => u.UsoId == usoA);
        Assert.Contains(lista, u => u.UsoId == usoB);

        // Admin do tenant T1 confirma parte do custo: o crédito do TENANT DO USO é ajustado.
        current.TenantId = t1; // volta p/ o dono do usoA (o último SalvarConfiguracaoAsync deixou t2)
        Assert.True(await amb.Gateway.ReconciliarUsoAsync(usoA, 0.20m, CancellationToken.None));
        var custoA = await LerCustoUsoPorId(usoA);
        Assert.Equal(0.20m, custoA.Conf);
        Assert.NotNull(custoA.Carimbo);
        var row1 = await LerConfig(amb.Repo, t1);
        Assert.Equal(0.20m, row1.OrcamentoUsadoMes); // 0.50 − (0.50−0.20)

        // Segunda vez (já reconciliado) e outro tenant (fora do escopo): negados.
        Assert.False(await amb.Gateway.ReconciliarUsoAsync(usoA, 0.20m, CancellationToken.None));
        current.TenantId = t2;
        Assert.False(await amb.Gateway.ReconciliarUsoAsync(usoA, 0.20m, CancellationToken.None));

        // Zero custo (nulo) também reconcilia: delta −0.10 deixa o crédito de T2 em 0.
        Assert.True(await amb.Gateway.ReconciliarUsoAsync(usoB, null, CancellationToken.None));
        var row2 = await LerConfig(amb.Repo, t2);
        Assert.Equal(0m, row2.OrcamentoUsadoMes);

        // Admin global alcança uso de qualquer tenant (dentro do mês corrente).
        current.TenantId = null;
        current.Global = true;
        Assert.True(await amb.Gateway.ReconciliarUsoAsync(usoC, null, CancellationToken.None));
        var row1b = await LerConfig(amb.Repo, t1);
        Assert.Equal(0.15m, row1b.OrcamentoUsadoMes); // 0.20 − 0.05

        // Valor negativo é rejeitado antes de tocar o banco.
        await Assert.ThrowsAsync<AiConfigException>(() =>
            amb.Gateway.ReconciliarUsoAsync(usoId: Guid.NewGuid(), valorConfirmado: -1m, ct: CancellationToken.None));
    }

    // ------------------------------------------------------------------
    // Resiliência: auditoria fora do ar não derruba a resposta
    // ------------------------------------------------------------------

    [Fact]
    public async Task FalhaDaAuditoria_RespostaDoUsuarioNaoEca()
    {
        var tenant = NovoTenant();
        var current = new FakeCurrent { TenantId = tenant };
        var cfgAuditoria = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = Cs })
            .Build();
        var amb = Montar(current, groqKey: "gk-aud-1",
            repoOverride: new AuditoriaCega(new AiConfigRepository(cfgAuditoria)));
        await using var prov = amb.Prov;

        await amb.Gateway.SalvarConfiguracaoAsync(AiTaskCodes.MeuDiaResumo, UpdateBase(), CancellationToken.None);
        var outcome = await amb.Gateway.ExecutarTarefaAsync(AiTaskCodes.MeuDiaResumo, ExecBase(tenant), CancellationToken.None);

        Assert.True(outcome.Success);
        Assert.Equal(1, amb.Groq.Chamadas);
    }

    // ------------------------------------------------------------------
    // Infraestrutura dos testes
    // ------------------------------------------------------------------

    private Guid NovoTenant()
    {
        var t = Guid.NewGuid();
        _tenants.Add(t);
        return t;
    }

    private static string KeyMestraHex()
        => Convert.ToHexString(Enumerable.Range(0, 32).Select(i => (byte)i).ToArray());

    private static AiConfigUpdate UpdateBase(
        string provedor = "groq", string modelo = "llama-teste", string? fallback = null,
        string? fallbackModelo = null, string? apiKey = null, int cota = 100,
        decimal? orcamento = null, string? orcamentoMoeda = null, int limiteEntrada = 4000)
        => new(true, provedor, modelo, fallback, apiKey, limiteEntrada, 1200, 30, cota, orcamento, fallbackModelo, orcamentoMoeda);

    private static AiTaskExecution ExecBase(Guid tenant, IReadOnlyList<string>? linhas = null, string? escopo = null)
        => new(tenant, Guid.NewGuid(), "TESTE", null, "Instrução fixa da tarefa de teste.",
            linhas ?? new[] { "linha um", "linha dois" }, escopo);

    private delegate Task<(int Status, string Body)> StubResponder(HttpRequestMessage request, string corpo, CancellationToken ct);

    private class StubHttp : HttpMessageHandler
    {
        private readonly StubResponder _responder;
        public StubHttp(StubResponder responder) { _responder = responder; }
        public int Chamadas { get; private set; }
        public HttpRequestMessage? Ultimo { get; private set; }
        public string? UltimoCorpo { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Chamadas++;
            Ultimo = request;
            UltimoCorpo = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
            var (status, body) = await _responder(request, UltimoCorpo, ct);
            return new HttpResponseMessage((HttpStatusCode)status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed record Ambiente(
        AiGateway Gateway,
        IAiConfigRepository Repo,
        AiSecretProtector Protector,
        StubHttp Groq,
        StubHttp Gemini,
        StubHttp DeepSeek,
        IConfiguration Cfg,
        ServiceProvider Prov);

    private const string CorpoSemUsage =
        @"{""id"":""gen-teste"",""object"":""chat.completion"",""created"":1,""model"":""gpt-oss-20b"",""choices"":[{""index"":0,""message"":{""role"":""assistant"",""content"":""RESPOSTA_TESTE""},""finish_reason"":""stop""}]}";

    private static string OkOpenAiCom(string modelo, int tokensIn, int tokensOut)
        => JsonSerializer.Serialize(new
        {
            id = "gen-teste",
            @object = "chat.completion",
            created = 1,
            model = modelo,
            choices = new[] { new { index = 0, message = new { role = "assistant", content = "RESPOSTA_TESTE" }, finish_reason = "stop" } },
            usage = new { prompt_tokens = tokensIn, completion_tokens = tokensOut, total_tokens = tokensIn + tokensOut }
        });

    private static StubHttp Status(int status, string body)
        => new((req, corpo, ct) => Task.FromResult((status, body)));

    private static Ambiente Montar(
        FakeCurrent current,
        StubHttp? groq = null, StubHttp? gemini = null, StubHttp? deepseek = null,
        string? encryptionKey = null, string? groqKey = null, string? geminiKey = null, string? deepseekKey = null,
        int? maxSlots = null, IAiConfigRepository? repoOverride = null)
    {
        var dados = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = Cs
        };
        if (encryptionKey is not null) dados["Ai:EncryptionKey"] = encryptionKey;
        if (groqKey is not null) dados["Ai:Providers:Groq:ApiKey"] = groqKey;
        if (geminiKey is not null) dados["Ai:Providers:Gemini:ApiKey"] = geminiKey;
        if (deepseekKey is not null) dados["Ai:Providers:DeepSeek:ApiKey"] = deepseekKey;
        if (maxSlots is not null) dados["Ai:MaxChamadasSimultaneas"] = maxSlots.ToString();
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(dados).Build();

        var g = groq ?? new StubHttp((req, corpo, ct) => Task.FromResult((200, OkOpenAiCom("llama-teste", 11, 7))));
        var ge = gemini ?? new StubHttp((req, corpo, ct) => Task.FromResult((200,
            @"{""candidates"":[{""content"":{""parts"":[{""text"":""RESPOSTA_GEMINI""}]}}],""usageMetadata"":{""promptTokenCount"":5,""candidatesTokenCount"":3}}")));
        var d = deepseek ?? new StubHttp((req, corpo, ct) => Task.FromResult((200, OkOpenAiCom("deepseek-flash", 11, 7))));

        var sc = new ServiceCollection();
        sc.AddLogging();
        sc.AddHttpClient("AiGroq")
            .ConfigurePrimaryHttpMessageHandler(() => g)
            .ConfigureHttpClient(c => { c.BaseAddress = new Uri("http://groq.test/"); c.Timeout = TimeSpan.FromSeconds(5); });
        sc.AddHttpClient("AiGemini")
            .ConfigurePrimaryHttpMessageHandler(() => ge)
            .ConfigureHttpClient(c => { c.BaseAddress = new Uri("http://gemini.test/"); c.Timeout = TimeSpan.FromSeconds(5); });
        sc.AddHttpClient("AiDeepSeek")
            .ConfigurePrimaryHttpMessageHandler(() => d)
            .ConfigureHttpClient(c => { c.BaseAddress = new Uri("http://deepseek.test/"); c.Timeout = TimeSpan.FromSeconds(5); });

        var prov = sc.BuildServiceProvider();
        var factory = prov.GetRequiredService<IHttpClientFactory>();
        var repo = repoOverride ?? new AiConfigRepository(cfg);
        var gateway = new AiGateway(
            new IAiProviderAdapter[]
            {
                new GroqAdapter(factory, cfg),
                new GeminiAdapter(factory, cfg),
                new DeepSeekAdapter(factory, cfg)
            },
            repo, new AiSecretProtector(cfg), current, cfg, NullLogger<AiGateway>.Instance);

        return new Ambiente(gateway, repo, new AiSecretProtector(cfg), g, ge, d, cfg, (ServiceProvider)prov);
    }

    // -- banco -----------------------------------------------------------

    private static async Task ExecSql(string sql, params (string Nome, object Valor)[] pars)
    {
        await using var cn = new NpgsqlConnection(Cs);
        await cn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, cn);
        foreach (var (nome, valor) in pars) cmd.Parameters.AddWithValue(nome, valor);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<int> ExecSqlCount(string sql, params (string Nome, object Valor)[] pars)
    {
        await using var cn = new NpgsqlConnection(Cs);
        await cn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, cn);
        foreach (var (nome, valor) in pars) cmd.Parameters.AddWithValue(nome, valor);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static Task InsertSlot(Guid tenant, bool stale)
        => ExecSql(stale
            ? "INSERT INTO plantaopro.ai_chamadas_ativas (tenant_id, task_code, criado_em) VALUES (@T, @K, now() - interval '7 minutes')"
            : "INSERT INTO plantaopro.ai_chamadas_ativas (tenant_id, task_code) VALUES (@T, @K)",
            ("@T", tenant), ("@K", AiTaskCodes.MeuDiaResumo));

    private static Task InsertUsoSucesso(Guid tenant)
        => ExecSql("INSERT INTO plantaopro.ai_usos (tenant_id, task_code, status) VALUES (@T, 'MEU_DIA_RESUMO', 'SUCESSO')",
            ("@T", tenant));

    private static Task InsertTesteConexao(Guid tenant, string provedor)
        => ExecSql("INSERT INTO plantaopro.ai_usos (tenant_id, task_code, provedor, status) VALUES (@T, 'TESTAR_CONEXAO', @P, 'SUCESSO')",
            ("@T", tenant), ("@P", provedor));

    private static Task InsertUso(Guid id, Guid tenant, string provedor, string erroClasse, decimal custoEstimado)
        => ExecSql(@"INSERT INTO plantaopro.ai_usos
                     (id, tenant_id, task_code, provedor, status, erro_classe, custo_estimado, custo_incerto)
                     VALUES (@Id, @T, 'MEU_DIA_RESUMO', @P, 'FALHA', @E, @C, TRUE)",
            ("@Id", id), ("@T", tenant), ("@P", provedor), ("@E", erroClasse), ("@C", custoEstimado));

    private static async Task<AiConfigRow> LerConfig(IAiConfigRepository repo, Guid tenant)
    {
        var row = await repo.ObterAsync(tenant, AiTaskCodes.MeuDiaResumo, CancellationToken.None);
        Assert.NotNull(row);
        return row!;
    }

    private static async Task<(decimal? Est, decimal? Conf, bool Incerto, string Moeda, DateTime? Versao, DateTime? Carimbo)> LerCustoUsoPorId(Guid usoId)
    {
        await using var cn = new NpgsqlConnection(Cs);
        await cn.OpenAsync();
        await using var cmd = new NpgsqlCommand(@"
            SELECT custo_estimado, custo_confirmado, custo_incerto, moeda, preco_versao, reconciliado_em
            FROM plantaopro.ai_usos WHERE id = @id", cn);
        cmd.Parameters.AddWithValue("@id", usoId);
        await using var rd = await cmd.ExecuteReaderAsync();
        Assert.True(await rd.ReadAsync());
        return (rd.IsDBNull(0) ? null : rd.GetDecimal(0),
                rd.IsDBNull(1) ? null : rd.GetDecimal(1),
                rd.GetBoolean(2), rd.GetString(3),
                rd.IsDBNull(4) ? null : rd.GetDateTime(4),
                rd.IsDBNull(5) ? null : rd.GetDateTime(5));
    }

    private static Task<(decimal? Est, decimal? Conf, bool Incerto, string Moeda, DateTime? Versao, DateTime? Carimbo)> LerCustoUso(Guid tenant)
        => LerCustoUsoPorIdAsync(tenant);

    private static async Task<(decimal? Est, decimal? Conf, bool Incerto, string Moeda, DateTime? Versao, DateTime? Carimbo)> LerCustoUsoPorIdAsync(Guid tenant)
    {
        await using var cn = new NpgsqlConnection(Cs);
        await cn.OpenAsync();
        await using var cmd = new NpgsqlCommand(@"
            SELECT custo_estimado, custo_confirmado, custo_incerto, moeda, preco_versao, reconciliado_em
            FROM plantaopro.ai_usos
            WHERE tenant_id = @t AND task_code = 'MEU_DIA_RESUMO'
            ORDER BY created_at DESC LIMIT 1", cn);
        cmd.Parameters.AddWithValue("@t", tenant);
        await using var rd = await cmd.ExecuteReaderAsync();
        Assert.True(await rd.ReadAsync());
        return (rd.IsDBNull(0) ? null : rd.GetDecimal(0),
                rd.IsDBNull(1) ? null : rd.GetDecimal(1),
                rd.GetBoolean(2), rd.GetString(3),
                rd.IsDBNull(4) ? null : rd.GetDateTime(4),
                rd.IsDBNull(5) ? null : rd.GetDateTime(5));
    }

    private static int LenPromptUsuario(StubHttp http)
    {
        using var doc = JsonDocument.Parse(http.UltimoCorpo!);
        return doc.RootElement.GetProperty("messages")[1].GetProperty("content").GetString()!.Length;
    }

    private static string LenConteudoUsuario(StubHttp http)
    {
        using var doc = JsonDocument.Parse(http.UltimoCorpo!);
        return doc.RootElement.GetProperty("messages")[1].GetProperty("content").GetString()!;
    }

    private sealed record UsoLinha(string Status, string? ErroClasse, string? Provedor);

    private static async Task<List<UsoLinha>> UsosDoTenant(Guid tenant, string task)
    {
        var lista = new List<UsoLinha>();
        await using var cn = new NpgsqlConnection(Cs);
        await cn.OpenAsync();
        await using var cmd = new NpgsqlCommand(@"
            SELECT status, erro_classe, provedor
            FROM plantaopro.ai_usos
            WHERE tenant_id = @t AND task_code = @k
            ORDER BY created_at", cn);
        cmd.Parameters.AddWithValue("@t", tenant);
        cmd.Parameters.AddWithValue("@k", task);
        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
            lista.Add(new UsoLinha(
                rd.GetString(0),
                rd.IsDBNull(1) ? null : rd.GetString(1),
                rd.IsDBNull(2) ? null : rd.GetString(2)));
        return lista;
    }

    private sealed class FakeCurrent : ICurrentUserService
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public Guid? TenantId { get; set; }
        public bool Global { get; set; }
        public Guid? ClienteId => TenantId;
        public Guid? SessionId => null;
        public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
        public bool IsAuthenticated() => true;
        public bool IsGlobalAdmin() => Global;
        public bool IsTenantAdmin() => false;
        public bool IsPartner() => false;
        public bool IsDoctor() => false;
        public bool HasRole(string role) => Roles.Contains(role);
    }

    /// <summary>Decorador: tudo funciona, EXCETO a gravação de auditoria (simula o banco fora do ar
    /// nesse caminho específico). A resposta do usuário continua saudável.</summary>
    private sealed class AuditoriaCega : IAiConfigRepository
    {
        private readonly IAiConfigRepository _inner;
        public AuditoriaCega(IAiConfigRepository inner) { _inner = inner; }

        public Task<AiConfigRow?> ObterAsync(Guid tenantId, string taskCode, CancellationToken ct) => _inner.ObterAsync(tenantId, taskCode, ct);
        public Task<IReadOnlyList<AiConfigRow>> ObterTodasAsync(Guid tenantId, CancellationToken ct) => _inner.ObterTodasAsync(tenantId, ct);
        public Task UpsertAsync(AiConfigRow row, CancellationToken ct) => _inner.UpsertAsync(row, ct);
        public Task<int> ContarUsosMesAtualAsync(Guid tenantId, string taskCode, CancellationToken ct) => _inner.ContarUsosMesAtualAsync(tenantId, taskCode, ct);
        public Task RegistrarUsoAsync(Guid tenantId, Guid? userId, string taskCode, string? contextoTipo, Guid? contextoId,
            string? provedor, string? modelo, bool sucesso, string? erroClasse,
            int? tokensEntrada, int? tokensSaida, int? duracaoMs, CancellationToken ct) => _inner.RegistrarUsoAsync(tenantId, userId, taskCode, contextoTipo, contextoId, provedor, modelo, sucesso, erroClasse, tokensEntrada, tokensSaida, duracaoMs, ct);
        public Task<bool> ReservarUsoAsync(Guid tenantId, string taskCode, int mesReferencia, CancellationToken ct) => _inner.ReservarUsoAsync(tenantId, taskCode, mesReferencia, ct);
        public Task LiberarUsoAsync(Guid tenantId, string taskCode, int mesReferencia, CancellationToken ct) => _inner.LiberarUsoAsync(tenantId, taskCode, mesReferencia, ct);
        public Task RegistrarCustoMensalAsync(Guid tenantId, string taskCode, int mesReferencia, decimal delta, CancellationToken ct) => _inner.RegistrarCustoMensalAsync(tenantId, taskCode, mesReferencia, delta, ct);
        public Task<Guid?> AbrirSlotAsync(Guid tenantId, string taskCode, int maxSlots, CancellationToken ct) => _inner.AbrirSlotAsync(tenantId, taskCode, maxSlots, ct);
        public Task FecharSlotAsync(Guid slotId, CancellationToken ct) => _inner.FecharSlotAsync(slotId, ct);
        public Task<AiPrecoVigente?> ObterPrecoVigenteAsync(string provedor, string modelo, CancellationToken ct) => _inner.ObterPrecoVigenteAsync(provedor, modelo, ct);
        public Task<int> ContarTestesConexao24hAsync(Guid tenantId, string provedor, CancellationToken ct) => _inner.ContarTestesConexao24hAsync(tenantId, provedor, ct);
        public Task<IReadOnlyList<AiUsoIncerto>> ListarUsosIncertosAsync(CancellationToken ct) => _inner.ListarUsosIncertosAsync(ct);
        public Task<bool> ReconciliarUsoAsync(Guid tenantEscopo, Guid usoId, decimal? valorConfirmado, int mesReferenciaAtual, CancellationToken ct) => _inner.ReconciliarUsoAsync(tenantEscopo, usoId, valorConfirmado, mesReferenciaAtual, ct);

        public Task RegistrarUsoComCustoAsync(Guid tenantId, Guid? userId, string taskCode, string? contextoTipo, Guid? contextoId,
            string? provedor, string? modelo, bool sucesso, string? erroClasse,
            int? tokensEntrada, int? tokensSaida, int? duracaoMs,
            decimal? custoEstimado, decimal? custoConfirmado, bool custoIncerto,
            string moeda, DateTime? precoVersao, CancellationToken ct)
            => throw new InvalidOperationException("auditoria indisponível (simulação)");
    }
}
