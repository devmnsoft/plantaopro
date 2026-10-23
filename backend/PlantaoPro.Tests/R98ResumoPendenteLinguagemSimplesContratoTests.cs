using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Ai;
using PlantaoPro.Api.Controllers;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Productivity;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R98 — assistente Drael em linguagem simples: resumo do pendente + orientação
/// ao usuário final (dono da organização, não um perfil técnico).
///
/// Evidências executáveis deste contrato:
///   F1 — drill E2E (API sozinha + stub local do provedor Drael): NAO_HABILITADO
///        antes da configuração, salvar tarefa RESUMO_PENDENTE, teste de conexão,
///        geração com auditoria em ai_usos, cota mensal (1) esgotando na segunda
///        chamada e VAZIO em tenant limpo ATIVA (sem gastar provedor);
///   F2 — migração v2377 amarrada ao manifest (checksum LF) e aos checks vivos
///        de task_code em ai_config E ai_usos (padrão do v2308);
///   F3 — tarefa nova registrada nos contratos de código (TryParse, lista de
///        configuração, DI, rota) — a UI de admin passa a ver o cartão;
///   F4 — jornada: contexto TRIAL completo (dias restantes, etapas pendentes do
///        onboarding reavaliado, operação com atraso e teto de linhas + estouro);
///   F5 — regra de VAZIO: nada pendente e contrato ATIVA → null (sem chamada ao
///        provedor); TRIAL/situação contratual diferente → sempre gera;
///   F6 — minimização: descrição livre/evidência do onboarding nunca vazam para
///        as linhas (somente rótulos de catálogo);
///   F7 — gateway ponta a ponta com stub do Drael: sanitização HTML, auditoria
///        com o novo código de tarefa e cota 1 bloqueando a segunda geração;
///   F8 — rota: sem tenant → NAO_HABILITADO; jornada vazia → VAZIO com sucesso;
///        sucesso passa o taskCode/resposta do gateway intactos;
///   F9 — camada Web: BFF, ação /MinhaAssinatura/ResumoIa, painel na tela do
///        dono (data-ai-url + script) e etiqueta na tela de configuração.
/// </summary>
[Collection("ia-camada")]
public sealed class R98ResumoPendenteLinguagemSimplesContratoTests : IAsyncLifetime
{
    private static readonly string Cs = TestDatabase.ConnectionString;
    private readonly HashSet<Guid> _tenants = new();
    private readonly HashSet<Guid> _clientes = new();
    private readonly HashSet<Guid> _planos = new();

    public async Task InitializeAsync()
    {
        try
        {
            await using var cn = new NpgsqlConnection(Cs);
            await cn.OpenAsync();
        }
        catch (Exception ex)
        {
            Assert.Fail($"PostgreSQL obrigatório para o contrato R98 não está acessível " +
                        $"(fonte: {TestDatabase.Origem}): {ex.Message}");
        }
    }

    public async Task DisposeAsync()
    {
        try
        {
            await using var cn = new NpgsqlConnection(Cs);
            await cn.OpenAsync();
            if (_tenants.Count > 0)
            {
                await using var cmd = new NpgsqlCommand(@"
                    DELETE FROM plantaopro.tenant_onboarding_checklist WHERE tenant_id = ANY(@ids);
                    DELETE FROM plantaopro.tenant_onboarding WHERE tenant_id = ANY(@ids);
                    DELETE FROM plantaopro.assinaturas WHERE tenant_id = ANY(@ids);
                    DELETE FROM plantaopro.ai_chamadas_ativas WHERE tenant_id = ANY(@ids);
                    DELETE FROM plantaopro.ai_usos WHERE tenant_id = ANY(@ids);
                    DELETE FROM plantaopro.ai_config WHERE tenant_id = ANY(@ids);
                    DELETE FROM plantaopro.tenants WHERE id = ANY(@ids);", cn);
                cmd.Parameters.AddWithValue("@ids", _tenants.ToArray());
                await cmd.ExecuteNonQueryAsync();
            }
            if (_clientes.Count > 0)
            {
                await using var cmd = new NpgsqlCommand(
                    "DELETE FROM plantaopro.clientes WHERE id = ANY(@ids)", cn);
                cmd.Parameters.AddWithValue("@ids", _clientes.ToArray());
                await cmd.ExecuteNonQueryAsync();
            }
            if (_planos.Count > 0)
            {
                await using var cmd = new NpgsqlCommand(
                    "DELETE FROM plantaopro.planos WHERE id = ANY(@ids)", cn);
                cmd.Parameters.AddWithValue("@ids", _planos.ToArray());
                await cmd.ExecuteNonQueryAsync();
            }
        }
        catch
        {
            // Best-effort: o banco já foi verificado no InitializeAsync.
        }
    }

    // ------------------------------------------------------------------
    // F1 — drill E2E
    // ------------------------------------------------------------------

    [Fact]
    public void F1_DrillE2E_MarkersEResultado()
    {
        var arquivo = Path.Combine(RepositoryPathResolver.RepoRoot,
            "docs", "evidencias", "2026-10-23-rodada-98", "r98a_drill_saida.txt");
        Assert.True(File.Exists(arquivo), "Saída do drill R98 não encontrada: rode r98a_drill_assistente.ps1 antes.");
        var texto = File.ReadAllText(arquivo, Encoding.UTF8);

        Assert.Contains("R98A_DRILL data=", texto);
        Assert.Contains("PORTAS_LIVRES_INICIO=SIM", texto);
        Assert.Contains("ESTAGEM_1_STUB_DRAEL ok=SIM", texto);
        Assert.Contains("ESTAGEM_1_API_NO_AR ok=SIM", texto);
        Assert.Contains("PRE_LIMPEZA_RERUN ok=SIM", texto);
        Assert.Contains("AI_TAREFA_NAO_HABILITADA_OK http=200 status=NAO_HABILITADO", texto);
        Assert.Contains("AI_CONFIG_OK http=200 task=RESUMO_PENDENTE", texto);
        Assert.Contains("AI_TESTE_CONEXAO_OK http=200 status=OK", texto);
        Assert.Contains("AI_GERACAO_OK http=200 sucesso=SIM provedor=drael", texto);
        Assert.Contains("AI_USO_REGISTRADO_OK task=RESUMO_PENDENTE", texto);
        Assert.Contains("AI_COTA_EXCEDIDA_OK http=200 status=COTA_EXCEDIDA", texto);
        Assert.Contains("AI_VAZIO_OK http=200 status=VAZIO", texto);
        Assert.Contains("PORTAS_LIVRES=SIM", texto);
        Assert.Contains("RESULTADO=PASSEI", texto);
        Assert.DoesNotContain("FALHA=", texto.Replace("RESULTADO=PASSEI", ""));
    }

    // ------------------------------------------------------------------
    // F2 — migração v2377
    // ------------------------------------------------------------------

    [Fact]
    public async Task F2_MigracaoV2377_ManifestChecksumEChecksVivos()
    {
        const string versao = "2026_10_v2377_r98_ai_resumo_pendente";
        var raiz = RepositoryPathResolver.RepoRoot;
        var manifest = File.ReadAllText(Path.Combine(raiz, "database", "migration-manifest.json"), Encoding.UTF8);
        using var doc = JsonDocument.Parse(manifest);
        var entrada = doc.RootElement.GetProperty("migrations").EnumerateArray()
            .FirstOrDefault(m => m.TryGetProperty("version", out var v) && v.GetString() == versao);
        Assert.NotEqual(JsonValueKind.Undefined, entrada.ValueKind);
        Assert.Equal("database/migrations/2026_10_v2377_r98_ai_resumo_pendente.sql",
            entrada.GetProperty("source").GetString());
        Assert.Equal("2026_10_v2376_r97_planos_configuraveis_entitlements",
            entrada.GetProperty("dependsOn")[0].GetString());
        Assert.Equal("active", entrada.GetProperty("status").GetString());

        // Checksum do manifest = SHA256 do conteúdo do arquivo normalizado p/ LF (lógica do runner).
        var caminhoSql = Path.Combine(raiz, "database", "migrations", versao + ".sql");
        var bruto = File.ReadAllBytes(caminhoSql);
        var normalizado = Encoding.UTF8.GetString(bruto).Replace("\r\n", "\n");
        var sha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizado))).ToLowerInvariant();
        Assert.Equal(sha, entrada.GetProperty("checksum").GetString());

        var sql = File.ReadAllText(caminhoSql, Encoding.UTF8);
        Assert.Contains("ai_config_task_code_check", sql);
        Assert.Contains("ai_usos_task_code_check", sql);
        Assert.Contains("'MEU_DIA_RESUMO', 'COTACAO_ANALISE', 'RESUMO_PENDENTE'", sql);
        Assert.Contains("'TESTAR_CONEXAO', 'RESUMO_PENDENTE'", sql);

        // Checks VIVOS no banco de teste: ambas as tabelas aceitam a tarefa nova.
        await using var cn = new NpgsqlConnection(Cs);
        await cn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            @"select pg_get_constraintdef(oid) from pg_constraint
              where conrelid in ('plantaopro.ai_config'::regclass,'plantaopro.ai_usos'::regclass)
                and contype = 'c' and pg_get_constraintdef(oid) ilike '%task_code%'
              order by conname", cn);
        var defs = new List<string>();
        await using (var rd = await cmd.ExecuteReaderAsync())
        {
            while (await rd.ReadAsync()) defs.Add(rd.GetString(0));
        }
        Assert.Equal(2, defs.Count);
        foreach (var d in defs)
            Assert.Contains("'RESUMO_PENDENTE'", d);
        // TESTAR_CONEXAO segue só na auditoria (sem linha de configuração por tenant).
        Assert.Equal(1, defs.Count(d => d.Contains("'TESTAR_CONEXAO'", StringComparison.OrdinalIgnoreCase)));
    }

    // ------------------------------------------------------------------
    // F3 — tarefa nos contratos de código + lista de configuração
    // ------------------------------------------------------------------

    [Fact]
    public async Task F3_TarefaRegistradaNosContratosENaListagemDeConfiguracao()
    {
        Assert.True(AiTaskCodes.TryParse("resumo_pendente", out var codigo));
        Assert.Equal("RESUMO_PENDENTE", codigo);
        Assert.False(AiTaskCodes.TryParse("RESUMO_PENDENTE_X", out _));
        Assert.False(AiTaskCodes.TryParse("RESUMO-PENDENTE", out _));
        Assert.False(AiTaskCodes.TryParse(null, out _));

        // O admin passa a ver o cartão da tarefa nova na listagem de configuração.
        var cfg = CfgBase();
        var current = new FakeCurrent { TenantId = Guid.NewGuid() };
        var gw = new AiGateway(Array.Empty<IAiProviderAdapter>(),
            new AiConfigRepository(cfg), new AiSecretProtector(cfg), current, cfg, NullLogger<AiGateway>.Instance);
        var views = await gw.ObterConfiguracoesAsync(CancellationToken.None);
        Assert.Contains(AiTaskCodes.ResumoPendente, views.Select(v => v.TaskCode));

        var raiz = RepositoryPathResolver.RepoRoot;
        var program = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Api", "Program.cs"), Encoding.UTF8);
        Assert.Contains("AddScoped<AiJornadaResumoPendente>", program);
        var controller = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Api", "Controllers", "AiController.cs"), Encoding.UTF8);
        Assert.Contains("[HttpPost(\"tarefas/resumo-pendente\")]", controller);
        var gateway = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Api", "Ai", "AiGateway.cs"), Encoding.UTF8);
        Assert.Contains("AiTaskCodes.MeuDiaResumo, AiTaskCodes.CotacaoAnalise, AiTaskCodes.ResumoPendente", gateway);
    }

    // ------------------------------------------------------------------
    // F4 — jornada: mundo TRIAL com pendências nas três áreas
    // ------------------------------------------------------------------

    [Fact]
    public async Task F4_Jornada_MundoTrial_ContextoDasTresAreasEtetoDeLinhas()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var (tenant, _, _, _) = await CriarMundoAsync(
            statusAssinatura: "TRIAL", trialFim: hoje.AddDays(4), dataFim: hoje.AddDays(34),
            etapas: new[] { ("Configurar dados da equipe", true), ("Personalizar identidade visual", false) });

        var itens = Enumerable.Range(1, 20).Select(n => n == 1
            ? ItemR98(n, priority: "CRITICA", module: "Financeiro", title: "Cobranca pendente",
                ctx: "fatura 12", due: DateTimeOffset.UtcNow.AddDays(-1))
            : ItemR98(n, due: DateTimeOffset.UtcNow.AddDays(2))).ToList();
        var prod = new FakeProductivity(itens);
        var exec = await NovaJornada(tenant, prod).ConstruirAsync(CancellationToken.None);

        Assert.NotNull(exec);
        var e = exec!;
        Assert.Equal("RESUMO_PENDENTE", e.ContextoTipo);
        Assert.Equal(AiJornadaResumoPendente.InstrucaoLinguagemSimples, e.InstrucaoTarefa);
        Assert.Equal(AiJornadaResumoPendente.JanelaDiasOperacao, 7);
        Assert.Equal(AiJornadaResumoPendente.MaxLinhasOnboarding, 12);
        Assert.Equal(AiJornadaResumoPendente.MaxLinhasOperacao, 15);

        // Área contrato: teste grátis com dias restantes reais.
        var linhaContrato = e.ContextoLinhas.First(l => l.StartsWith("[contrato]"));
        Assert.Contains("Teste gratuito em andamento", linhaContrato);
        Assert.Contains(hoje.AddDays(4).ToString("dd/MM/yyyy"), linhaContrato);
        Assert.Contains("faltam 4 dia(s)", linhaContrato);
        Assert.Contains("cobrança mensal", linhaContrato);

        // Área configuração inicial: etapas reavaliadas, rótulo de módulo e natureza.
        Assert.Contains(e.ContextoLinhas, l => l.StartsWith("[configuracao]")
            && l.Contains("Configurar dados da equipe") && l.Contains("obrigatória"));
        Assert.Contains(e.ContextoLinhas, l => l.Contains("Personalizar identidade visual")
            && l.Contains("opcional"));

        // Área operação: teto de 15 linhas + linha de estouro; atraso marcado.
        var linhasOp = e.ContextoLinhas.Where(l => l.StartsWith("[operacao]")).ToList();
        Assert.Equal(16, linhasOp.Count);
        Assert.Contains(linhasOp, l => l.Contains("[CRITICA] Financeiro/Cobranca pendente")
            && l.Contains("fatura 12") && l.Contains(", ATARDA"));
        Assert.Contains(linhasOp, l => l.Contains("e mais 5 pendência(s) operacional(is) não listada(s)"));

        // Janela da consulta derivada (7 dias à frente) e teto de página.
        Assert.NotNull(prod.UltimaConsulta);
        var q = prod.UltimaConsulta!;
        Assert.Equal(25, q.PageSize);
        Assert.NotNull(q.DueTo);
        Assert.True(q.DueTo!.Value > DateTimeOffset.UtcNow.AddDays(5)
            && q.DueTo.Value < DateTimeOffset.UtcNow.AddDays(9),
            $"janela DueTo fora do esperado: {q.DueTo:o}");

        // Escopo explícito (período/fuso/quantidade) exibido ao usuário.
        Assert.NotNull(e.Escopo);
        Assert.Contains("America/Sao_Paulo", e.Escopo);
        Assert.Contains("contrato: TRIAL", e.Escopo);
        Assert.Contains("2 etapa(s) pendente(s)", e.Escopo);
        Assert.Contains("operação: 20 item(ns) carregado(s) de 20 no total", e.Escopo);

        // Contrato de linguagem simples travado no prompt.
        Assert.Contains("sem jargão técnico e sem siglas", e.InstrucaoTarefa);
        Assert.Contains("Máximo de 160 palavras", e.InstrucaoTarefa);
        Assert.Contains("não aplica nada sozinho", e.InstrucaoTarefa);
    }

    // ------------------------------------------------------------------
    // F5 — regra de VAZIO (sem gastar chamada/cota de provedor)
    // ------------------------------------------------------------------

    [Fact]
    public async Task F5_Jornada_RegraDeVazio_AtivaLimpaNula_TrialSempreGera()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);

        // (a) ATIVA, nada de onboarding pendente e nada operacional → null (rota responde VAZIO).
        var (tAtiva, _, _, _) = await CriarMundoAsync("ATIVA", null, hoje.AddDays(30));
        var vazio = await NovaJornada(tAtiva, new FakeProductivity()).ConstruirAsync(CancellationToken.None);
        Assert.Null(vazio);

        // (b) TRIAL limpo → ainda gera: a orientação sobre o teste é o propósito da tarefa.
        var (tTrial, _, _, _) = await CriarMundoAsync("TRIAL", hoje.AddDays(2), hoje.AddDays(32));
        var trial = await NovaJornada(tTrial, new FakeProductivity()).ConstruirAsync(CancellationToken.None);
        Assert.NotNull(trial);
        var linha = trial!.ContextoLinhas.First(l => l.StartsWith("[contrato]"));
        Assert.Contains("Teste gratuito em andamento", linha);
        Assert.Contains("faltam 2 dia(s)", linha);

        // (c) ATIVA com uma única pendência operacional → gera.
        var (tOp, _, _, _) = await CriarMundoAsync("ATIVA", null, hoje.AddDays(30));
        var op = await NovaJornada(tOp, new FakeProductivity(new[] { ItemR98(1) }))
            .ConstruirAsync(CancellationToken.None);
        Assert.NotNull(op);
        Assert.Contains(op!.ContextoLinhas, l => l.StartsWith("[operacao]"));
    }

    // ------------------------------------------------------------------
    // F6 — minimização de dados (rótulos de catálogo; nada de texto livre)
    // ------------------------------------------------------------------

    [Fact]
    public async Task F6_Jornada_Minimizacao_DescricaoLivreNaomFoge()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var etapas = Enumerable.Range(1, 30)
            .Select(n => ($"Etapa R98 numero {n}", false))
            .ToArray();
        var (tenant, _, _, _) = await CriarMundoAsync("ATIVA", null, hoje.AddDays(30), etapas);

        var exec = await NovaJornada(tenant, new FakeProductivity()).ConstruirAsync(CancellationToken.None);
        Assert.NotNull(exec);
        var linhasCfg = exec!.ContextoLinhas.Where(l => l.StartsWith("[configuracao]")).ToList();

        // Teto de 12 etapas listadas + 1 linha de estouro (30 pendentes).
        Assert.Equal(AiJornadaResumoPendente.MaxLinhasOnboarding + 1, linhasCfg.Count);
        Assert.Contains(linhasCfg, l => l.Contains("e mais 18 etapa(s) de configuração pendente(s)"));

        // Texto livre (descrição/evidência) jamais entra nas linhas enviadas ao modelo.
        foreach (var l in exec.ContextoLinhas)
        {
            Assert.DoesNotContain("ZZZ_DESCRICAO_LIVRE_ZZZ", l);
            Assert.DoesNotContain("conclusao humana", l);
        }
    }

    // ------------------------------------------------------------------
    // F7 — gateway ponta a ponta com stub do provedor Drael
    // ------------------------------------------------------------------

    [Fact]
    public async Task F7_GatewayPontaAPonta_Drael_SanitizacaoAuditoriaECotaUm()
    {
        var tenant = Guid.NewGuid();
        _tenants.Add(tenant);

        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = Cs,
            ["Ai:EncryptionKey"] = KeyHex()
        }).Build();
        var stub = new StubHttp((req, corpo, ct) => Task.FromResult((200, CorpoDrael("<b>Olá</b> & bem-vindo", 12, 8))));
        var sc = new ServiceCollection();
        sc.AddLogging();
        sc.AddHttpClient("AiDrael")
            .ConfigurePrimaryHttpMessageHandler(() => stub)
            .ConfigureHttpClient(c => { c.BaseAddress = new Uri("http://drael.test/v1/"); c.Timeout = TimeSpan.FromSeconds(5); });
        using var prov = sc.BuildServiceProvider();
        var current = new FakeCurrent { TenantId = tenant };
        var gw = new AiGateway(
            new IAiProviderAdapter[] { new DraelAdapter(prov.GetRequiredService<IHttpClientFactory>(), cfg) },
            new AiConfigRepository(cfg), new AiSecretProtector(cfg), current, cfg, NullLogger<AiGateway>.Instance);

        // Tarefa nova habilitada pelo tenant, chave do tenant (cifrada) e cota MENSAL de 1 uso.
        await gw.SalvarConfiguracaoAsync(AiTaskCodes.ResumoPendente,
            new AiConfigUpdate(true, "drael", "drael-v1", null, "dk-do-tenant", 4000, 1200, 30, 1, null, null, null),
            CancellationToken.None);

        var exec = new AiTaskExecution(tenant, Guid.NewGuid(), "RESUMO_PENDENTE", null,
            "Instrução fixa do teste.", new[] { "[contrato] Teste gratuito em andamento (falta 1 dia)." }, "escopo de teste");

        var outcome = await gw.ExecutarTarefaAsync(AiTaskCodes.ResumoPendente, exec, CancellationToken.None);
        Assert.True(outcome.Success);
        Assert.Equal("drael", outcome.Provedor);
        Assert.Equal(8, outcome.TokensOut);
        // Saída sanitizada: HTML escapado, nada de marcação crua.
        Assert.NotNull(outcome.Texto);
        Assert.Contains("&lt;b&gt;", outcome.Texto);
        Assert.Contains("&amp;", outcome.Texto);
        Assert.DoesNotContain("<b>", outcome.Texto);

        // Auditoria imutável com o NOVO código de tarefa.
        var sucessos1 = (await UsosDoTenant(tenant)).Where(u => u.Status == "SUCESSO").ToList();
        Assert.Single(sucessos1);
        Assert.Equal("drael", sucessos1[0].Provedor);

        // Segunda geração no mesmo mês: cota de 1 esgotada, sem nova chamada ao provedor.
        var chamadasAntes = stub.Chamadas;
        var segunda = await gw.ExecutarTarefaAsync(AiTaskCodes.ResumoPendente, exec, CancellationToken.None);
        Assert.False(segunda.Success);
        Assert.Equal(AiErrorKinds.CotaExcedida, segunda.StatusKind);
        Assert.Equal(chamadasAntes, stub.Chamadas);
        var sucessos2 = (await UsosDoTenant(tenant)).Where(u => u.Status == "SUCESSO").ToList();
        Assert.Single(sucessos2);
    }

    // ------------------------------------------------------------------
    // F8 — rota: classificação e passagem do resultado
    // ------------------------------------------------------------------

    [Fact]
    public async Task F8_Rota_SemTenantNaoHabilitado_VazioComSucesso_SucessoPassaIntegro()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);

        // (a) sem tenant na sessão → NAO_HABILITADO com mensagem amigável (antes de qualquer IO).
        var currentSemTenant = new FakeCurrent();
        var ctrlSem = NovaController(currentSemTenant, new FakeProductivity(), new FakeGateway());
        var oa = AwaitOutcome(await ctrlSem.ResumoPendente(CancellationToken.None));
        Assert.False(oa.Success);
        Assert.Equal(AiErrorKinds.NaoHabilitado, oa.StatusKind);
        Assert.Contains("Selecione um cliente/instituição", oa.Mensagem);

        // (b) tenant limpo ATIVA → jornada devolve null → VAZIO com SUCESSO (não gastou provedor).
        var (tAtiva, _, _, _) = await CriarMundoAsync("ATIVA", null, hoje.AddDays(30));
        var currentAtiva = new FakeCurrent { TenantId = tAtiva };
        var gwB = new FakeGateway();
        var ctrlB = NovaController(currentAtiva, new FakeProductivity(), gwB);
        var ob = AwaitOutcome(await ctrlB.ResumoPendente(CancellationToken.None));
        Assert.True(ob.Success);
        Assert.Equal(AiErrorKinds.Vazio, ob.StatusKind);
        Assert.Contains("Tudo em dia", ob.Mensagem);
        Assert.Null(gwB.UltimoTaskCode); // nenhuma chamada ao gateway/provedor

        // (c) situação com pendências → gateway recebe a tarefa certa e o outcome passa íntegro.
        var (tTrial, _, _, _) = await CriarMundoAsync("TRIAL", hoje.AddDays(3), hoje.AddDays(33));
        var currentTrial = new FakeCurrent { TenantId = tTrial };
        var resposta = new AiOutcome(true, AiErrorKinds.Ok, "ok", Texto: "TEXT");
        var gwC = new FakeGateway { Resposta = resposta };
        var ctrlC = NovaController(currentTrial, new FakeProductivity(), gwC);
        var oc = AwaitOutcome(await ctrlC.ResumoPendente(CancellationToken.None));
        Assert.Equal(resposta, oc);
        Assert.Equal(AiTaskCodes.ResumoPendente, gwC.UltimoTaskCode);
        Assert.NotNull(gwC.UltimoExec);
        Assert.Equal("RESUMO_PENDENTE", gwC.UltimoExec!.ContextoTipo);
        Assert.Equal(tTrial, gwC.UltimoExec.TenantId);
    }

    // ------------------------------------------------------------------
    // F9 — camada Web (BFF + painel do dono + etiqueta de configuração)
    // ------------------------------------------------------------------

    [Fact]
    public void F9_CamadaWeb_BFFPainelEdoUsuarioFinal()
    {
        var raiz = RepositoryPathResolver.RepoRoot;

        var bff = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Web", "Services", "AiWebService.cs"), Encoding.UTF8);
        Assert.Contains("ResumoPendenteAsync", bff);
        Assert.Contains("api/ai/tarefas/resumo-pendente", bff);

        var ctrl = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Web", "Controllers", "MinhaAssinaturaController.cs"), Encoding.UTF8);
        Assert.Contains("[HttpGet(\"ResumoIa\")]", ctrl);
        Assert.Contains("_ai.ResumoPendenteAsync(", ctrl);
        Assert.Contains("StatusNaoAutenticado", ctrl);

        var view = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Web", "Views", "MinhaAssinatura", "Index.cshtml"), Encoding.UTF8);
        Assert.Contains("data-ai-url=\"/MinhaAssinatura/ResumoIa\"", view);
        Assert.Contains("data-ai-method=\"GET\"", view);
        Assert.Contains("data-ai-run", view);
        Assert.Contains("O que você precisa saber agora", view);
        Assert.Contains("~/js/ai-assistant-panel.js", view);
        Assert.Contains("~/css/design-system/assistente-ia.css", view);

        var assistente = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Web", "Views", "AssistenteIa", "Index.cshtml"), Encoding.UTF8);
        Assert.Contains("\"RESUMO_PENDENTE\" => \"Resumo do pendente — linguagem simples (usuário final)\"", assistente);
    }

    // ------------------------------------------------------------------
    // Fixtures e helpers
    // ------------------------------------------------------------------

    /// <summary>Mundo R98 mínimo e determinístico: cliente + tenant + plano + assinatura
    /// (+ master de onboarding e etapas pendentes sem critério automático, que permanecem
    /// PENDENTES na reavaliação contra dados).</summary>
    private async Task<(Guid Tenant, Guid Cliente, Guid Plano, Guid Assinatura)> CriarMundoAsync(
        string statusAssinatura, DateOnly? trialFim, DateOnly? dataFim,
        IEnumerable<(string Titulo, bool Obrigatorio)>? etapas = null)
    {
        var tenant = Guid.NewGuid();
        var cliente = Guid.NewGuid();
        var plano = Guid.NewGuid();
        var assinatura = Guid.NewGuid();
        var onboarding = Guid.NewGuid();
        _tenants.Add(tenant); _clientes.Add(cliente); _planos.Add(plano);

        var dataFimEfetiva = dataFim ?? DateOnly.FromDateTime(DateTime.Today.AddMonths(1));
        await ExecSql(@"
            insert into plantaopro.clientes(id) values(@c);
            insert into plantaopro.tenants(id, cliente_id) values(@t, @c);
            insert into plantaopro.planos(id, codigo, nome, valor_mensal, valor_anual, dias_trial, permite_trial, status)
            values(@p, @codigo, 'Plano Teste R98', 100.00, 1200.00, 7, true, 'ATIVO');
            insert into plantaopro.assinaturas(id, tenant_id, cliente_id, plano_id, status, periodicidade, valor_contratado, data_inicio, data_fim, data_trial_fim)
            values(@a, @t, @c, @p, @st, 'MENSAL', 100.00, now(), @df, @tf);
            insert into plantaopro.tenant_onboarding(id, tenant_id, cliente_id) values(@o, @t, @c);",
            ("@t", tenant), ("@c", cliente), ("@p", plano), ("@a", assinatura), ("@o", onboarding),
            ("@codigo", "R98_" + tenant.ToString("N")),
            ("@st", statusAssinatura), ("@df", dataFimEfetiva), ("@tf", (object?)trialFim ?? DBNull.Value));

        if (etapas is not null)
        {
            var n = 0;
            foreach (var (titulo, obrigatorio) in etapas)
            {
                n++;
                await ExecSql(@"
                    insert into plantaopro.tenant_onboarding_checklist(
                        id, onboarding_id, tenant_id, cliente_id, codigo, titulo, descricao, ordem, obrigatorio, modulo_codigo, status)
                    values(@i, @o, @t, @c, @cod, @titulo, 'Descricao livre ZZZ_DESCRICAO_LIVRE_ZZZ - nao deve aparecer nas linhas do assistente.', @n, @obrig, 'FINANCEIRO', 'PENDENTE')",
                    ("@i", Guid.NewGuid()), ("@o", onboarding), ("@t", tenant), ("@c", cliente),
                    ("@cod", $"R98_ETAPA_{n:D2}"), ("@titulo", titulo), ("@n", n), ("@obrig", obrigatorio));
            }
        }
        return (tenant, cliente, plano, assinatura);
    }

    private AiJornadaResumoPendente NovaJornada(Guid tenant, FakeProductivity prod)
        => new(prod,
            new OnboardingJornadaService(CfgBase(), NullLogger<OnboardingJornadaService>.Instance),
            CfgBase(), new FakeCurrent { TenantId = tenant, UserId = Guid.NewGuid() });

    private static AiController NovaController(ICurrentUserService current, FakeProductivity prod, FakeGateway gateway)
    {
        var cfg = CfgBase();
        var resumo = new AiJornadaResumoPendente(
            prod, new OnboardingJornadaService(cfg, NullLogger<OnboardingJornadaService>.Instance), cfg, current);
        return new AiController(
            gateway,
            new AiJornadaMeuDia(prod, current),
            new AiJornadaCotacao(null!, current),
            resumo,
            current,
            NullLogger<AiController>.Instance);
    }

    private static AiOutcome AwaitOutcome(IActionResult result)
    {
        var or = Assert.IsAssignableFrom<ObjectResult>(result);
        return Assert.IsType<AiOutcome>(or.Value);
    }

    private static IConfiguration CfgBase()
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = Cs
        }).Build();

    private static ProductivityActionDto ItemR98(int n, string priority = "ALTA", string module = "Operacao",
        string title = "Tarefa operacional de rotina", string ctx = "contexto operacional padrao",
        DateTimeOffset? due = null)
        => new($"r98-{n}", module, "TAREFA", Guid.NewGuid(), "ABRIR", title,
            $"Descricao livre do item {n} (nao deve aparecer nas linhas).", priority, "PENDENTE",
            due ?? DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow, "TENANT", null,
            "bi-list", ctx, "ABRIR", true, false, DateTimeOffset.UtcNow, false);

    private static string KeyHex()
        => Convert.ToHexString(Enumerable.Range(0, 32).Select(i => (byte)i).ToArray());

    private static string CorpoDrael(string conteudo, int tokensIn, int tokensOut)
        => JsonSerializer.Serialize(new
        {
            id = "gen-r98",
            @object = "chat.completion",
            created = 1,
            model = "drael-v1",
            choices = new[] { new { index = 0, message = new { role = "assistant", content = conteudo }, finish_reason = "stop" } },
            usage = new { prompt_tokens = tokensIn, completion_tokens = tokensOut, total_tokens = tokensIn + tokensOut }
        });

    // -- banco -----------------------------------------------------------

    private static async Task ExecSql(string sql, params (string Nome, object Valor)[] pars)
    {
        await using var cn = new NpgsqlConnection(Cs);
        await cn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, cn);
        foreach (var (nome, valor) in pars) cmd.Parameters.AddWithValue(nome, valor);
        await cmd.ExecuteNonQueryAsync();
    }

    private sealed record UsoLinha(string Status, string? Provedor);

    private static async Task<List<UsoLinha>> UsosDoTenant(Guid tenant)
    {
        var lista = new List<UsoLinha>();
        await using var cn = new NpgsqlConnection(Cs);
        await cn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            @"select status, provedor from plantaopro.ai_usos
              where tenant_id = @t and task_code = 'RESUMO_PENDENTE' order by created_at", cn);
        cmd.Parameters.AddWithValue("@t", tenant);
        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
            lista.Add(new UsoLinha(rd.GetString(0), rd.IsDBNull(1) ? null : rd.GetString(1)));
        return lista;
    }

    // -- fakes -----------------------------------------------------------

    private sealed class FakeProductivity : IProductivityActionService
    {
        private readonly IReadOnlyList<ProductivityActionDto> _itens;
        public ProductivityQuery? UltimaConsulta { get; private set; }

        public FakeProductivity(IReadOnlyList<ProductivityActionDto>? itens = null)
            => _itens = itens ?? Array.Empty<ProductivityActionDto>();

        public Task<ProductivityPageDto> ListAsync(ProductivityQuery query, CancellationToken ct)
        {
            UltimaConsulta = query;
            var total = _itens.Count;
            var pageSize = Math.Max(1, query.PageSize);
            return Task.FromResult(new ProductivityPageDto(_itens, query.Page, pageSize, total,
                Math.Max(1, (int)Math.Ceiling(total / (double)pageSize))));
        }

        public Task<ProductivitySummaryDto> SummaryAsync(CancellationToken ct)
            => Task.FromResult(new ProductivitySummaryDto(0, 0, 0, 0, 0));
        public Task SnoozeAsync(string key, DateTimeOffset until, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<ProductivityAgendaItemDto>> GetAgendaAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<ProductivityAgendaItemDto>>(Array.Empty<ProductivityAgendaItemDto>());
        public IReadOnlyList<QuickActionDto> QuickActions() => Array.Empty<QuickActionDto>();
    }

    private sealed class FakeGateway : IAiGateway
    {
        public AiOutcome Resposta { get; init; } = new(true, AiErrorKinds.Ok, "ok", Texto: "TEXT");
        public string? UltimoTaskCode { get; private set; }
        public AiTaskExecution? UltimoExec { get; private set; }

        public bool MasterKeyConfigurada => false;
        public Task<AiOutcome> ExecutarTarefaAsync(string taskCode, AiTaskExecution execution, CancellationToken ct)
        {
            UltimoTaskCode = taskCode;
            UltimoExec = execution;
            return Task.FromResult(Resposta);
        }

        public Task<IReadOnlyList<AiConfigView>> ObterConfiguracoesAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<AiConfigView>>(Array.Empty<AiConfigView>());
        public Task<AiConfigView> SalvarConfiguracaoAsync(string taskCode, AiConfigUpdate update, CancellationToken ct)
            => throw new NotSupportedException();
        public Task<AiOutcome> TestarConexaoAsync(string provider, CancellationToken ct)
            => throw new NotSupportedException();
        public Task<IReadOnlyList<AiUsoIncerto>> ListarUsosIncertosAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<AiUsoIncerto>>(Array.Empty<AiUsoIncerto>());
        public Task<bool> ReconciliarUsoAsync(Guid usoId, decimal? valorConfirmado, CancellationToken ct)
            => Task.FromResult(false);
    }

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
}
