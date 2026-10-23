using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Http;
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
/// R99 (jornadas guiadas para tenants novos): o provisionamento (self-service e B2B mapped)
/// agora contrata por padrão os módulos que o plano inclui: cada linha plano_modulos com
/// incluido=true (módulo ATIVO no catálogo) vira tenant_modulos efetivo na MESMA transação
/// do cadastro, antes da materialização da jornada — origem PLANO, histórico
/// CONTRATAR_PLANO. Sem isso o tenant novo nascia com modulos_efetivos() vazio e a jornada
/// "por módulo" nunca aparecia (verificado ao vivo no tenant trial da R98). Camada web:
/// /Onboarding ganha CTA do próximo passo (espelhando a reavaliação canônica, nunca mixando
/// o seed com o estado reavaliado) + progresso por grupo; MinhaAssinatura expõe a faixa de
/// implantação; /PrimeirosPassos sai do guard orfao (AJUDA, comum) com handoff para a
/// jornada; /Cadastro/Sucesso orienta o que acontece depois. Contrato travado: (1) o drill
/// E2E API+Web com RESULTADO=PASSEI; (2) o dump t1 do banco (contrato + materialização);
/// (3) os fragmentos de código (kernel, guard e telas); (4) o kernel em nível de serviço
/// com dois mundos determinísticos (plano com módulo vs plano sem módulo).
/// </summary>
[Collection("saas-operacao-serial")]
public sealed class R99JornadaGuiadaTenantNovoContratoTests
{
    private const string PastaR99 = "docs/evidencias/2026-10-23-rodada-99";
    private const string ArquivoSaida = "r99a_drill_saida.txt";
    private const string ArquivoT1 = "r99a_t1_contrato_banco.txt";

    private static readonly string[] CodigosMundo1 =
    {
        "ONB_EMPRESA_DADOS", "ONB_EQUIPE_CONVIDAR", "ONB_EQUIPE_ACESSO", "ONB_IDENTIDADE",
        "ONB_PREFERENCIAS", "ONB_PL_CRIAR", "ONB_PL_PUBLICAR", "ONB_PL_ESCALA", "ONB_REVISAO"
    };

    private static readonly string[] CodigosMundo2 =
    {
        "ONB_EMPRESA_DADOS", "ONB_EQUIPE_CONVIDAR", "ONB_EQUIPE_ACESSO", "ONB_IDENTIDADE",
        "ONB_PREFERENCIAS", "ONB_REVISAO"
    };

    // =========================================================================
    // Fatos do drill (execucao canonica contra o banco vivo, API + Web no ar)
    // =========================================================================

    // Fato 1 - a execucao canonica do drill ponta a ponta: dois planos (um com PLANTOES
    // incluido=true via governanca), dois mundos TRIAL pelo funil, seed de jornada pura,
    // contrato de modulo no banco, reavaliacao avancando por dados persistidos, proxima
    // acao com link coerente e as quatro superficies Web do dono.
    [Fact]
    public void Drill_saida_marcadores_completos_e_resultado_passei()
    {
        var linhas = File.ReadAllLines(Caminho(PastaR99 + "/" + ArquivoSaida));
        Assert.StartsWith("R99A_DRILL data=", linhas[0]);
        Assert.DoesNotContain(linhas, l => l.StartsWith("FALHA ", StringComparison.Ordinal));

        Assert.Equal("PORTAS_LIVRES_INICIO=SIM", ObterLinha(linhas, "PORTAS_LIVRES_INICIO"));
        var api = Parsear(ObterLinha(linhas, "ESTAGEM_1_API_NO_AR "));
        Assert.Equal("SIM", api["ok"]);
        Assert.True(int.TryParse(api.GetOrThrow("pid"), out var pidApi) && pidApi > 0, $"pid da API ausente: {api.GetOrThrow("pid")}.");
        var web = Parsear(ObterLinha(linhas, "ESTAGEM_1_WEB_NO_AR "));
        Assert.Equal("SIM", web["ok"]);
        Assert.True(int.TryParse(web.GetOrThrow("pid"), out var pidWeb) && pidWeb > 0, $"pid da Web ausente: {web.GetOrThrow("pid")}.");
        Assert.Equal("PRE_LIMPEZA_RERUN ok=SIM", ObterLinha(linhas, "PRE_LIMPEZA_RERUN"));

        var planos = Parsear(ObterLinha(linhas, "PA_PLANO_OK "));
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", planos["plano1"]);
        Assert.Equal("1", planos["vinculos_plano1"]);
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", planos["plano2"]);
        Assert.Equal("0", planos["vinculos_plano2"]);

        var mundos = Parsear(ObterLinha(linhas, "PA_MUNDOS_OK "));
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", mundos["trial_tenant"]);
        Assert.Equal("TRIAL", mundos["trial_status"]);
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", mundos["semmod_tenant"]);
        Assert.Equal("TRIAL", mundos["semmod_status"]);

        var seed = Parsear(ObterLinha(linhas, "FUNIL_STATUS_SEED "));
        Assert.Equal("200", seed["http"]);
        Assert.Equal("EM_ANDAMENTO", seed["status"]);
        Assert.Equal("0", seed["progresso"]);
        Assert.Equal("SIM", seed["proxima_seed"]);

        Assert.StartsWith("FUNIL_CONTRATO_OK ", ObterLinha(linhas, "FUNIL_CONTRATO_OK "));
        var t1 = Parsear(ObterLinha(linhas, "T1_DUMP_OK "));
        Assert.Equal("r99a_t1_contrato_banco.txt", t1["arquivo"]);

        var reav = Parsear(ObterLinha(linhas, "FUNIL_REAVALIACAO_OK "));
        Assert.Equal("200", reav["http"]);
        Assert.Equal("9", reav["etapas"]);
        Assert.Equal("14", reav["progresso"]);
        Assert.Equal("SIM", reav["proxima_convite"]);

        var controle = Parsear(ObterLinha(linhas, "FUNIL_CONTROLE_OK "));
        Assert.Equal("6", controle["etapas"]);
        Assert.Equal("0", controle["progresso"]);
        Assert.Equal("SIM", controle["proxima_empresa"]);

        var proxima = Parsear(ObterLinha(linhas, "PROXIMA_ACAO_OK "));
        Assert.Equal("200", proxima["http"]);
        Assert.Equal("SIM", proxima["titulo_convite"]);
        Assert.Equal("/ConvitesEquipe/Index", proxima["link"]);
        Assert.Equal("PENDENTE", proxima["status"]);

        Assert.StartsWith("WEB_LOGIN_OK http=200", ObterLinha(linhas, "WEB_LOGIN_OK "));

        var onb = Parsear(ObterLinha(linhas, "WEB_ONBOARDING_OK "));
        Assert.Equal("200", onb["http"]);
        Assert.Equal("9", onb["itens"]);
        Assert.Equal("SIM", onb["cta"]);
        Assert.Equal("SIM", onb["cta_link"]);
        Assert.Equal("SIM", onb["proxima_convite"]);
        Assert.Equal("14", onb["progresso"]);
        Assert.Equal("1de4", onb["grupo_geral"]);
        Assert.Equal("0de3", onb["grupo_plantoes"]);
        Assert.Equal("SIM", onb["sem_saudes360"]);
        Assert.Equal("SIM", onb["sem_adm360"]);

        Assert.StartsWith("WEB_LINK_PROXIMA_OK http=200", ObterLinha(linhas, "WEB_LINK_PROXIMA_OK "));

        var asin = Parsear(ObterLinha(linhas, "WEB_ASSINATURA_OK "));
        Assert.Equal("200", asin["http"]);
        Assert.Equal("SIM", asin["faixa"]);
        Assert.Equal("14", asin["percentual"]);
        Assert.Equal("SIM", asin["botao"]);

        var pp = Parsear(ObterLinha(linhas, "WEB_PRIMEIROS_PASSOS_OK "));
        Assert.Equal("200", pp["http"]);
        Assert.Equal("SIM", pp["handoff"]);

        var su = Parsear(ObterLinha(linhas, "WEB_SUCESSO_OK "));
        Assert.Equal("200", su["http"]);
        Assert.Equal("SIM", su["orientacao"]);

        Assert.Equal("PORTAS_LIVRES=SIM", ObterLinha(linhas, "PORTAS_LIVRES="));
        Assert.Equal("RESULTADO=PASSEI", ObterLinha(linhas, "RESULTADO"));
    }

    // Fato 2 - o dump t1 do banco ANTES de qualquer reavaliacao: mundo 1 com o modulo do
    // plano contratado (origem PLANO, historico CONTRATAR_PLANO, efetivo na funcao canonica)
    // e a jornada materializada por modulo (9 etapas, sem SAUDE360/ADM360); mundo 2 (plano
    // sem modulos) segue com a jornada geral de 6 etapas — sem regressao para planos antigos.
    [Fact]
    public void Drill_dump_t1_contrato_do_plano_materia_a_jornada_por_modulo()
    {
        var linhas = File.ReadAllLines(Caminho(PastaR99 + "/" + ArquivoT1));
        Assert.True(linhas.Length == 2, $"esperadas duas linhas de dump (mundo1/mundo2): {linhas.Length}.");

        var m1 = Parsear(linhas[0]);
        Assert.StartsWith("T1_MUNDO1", linhas[0]);
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", m1["tenant"]);
        Assert.Equal("[PLANTOES]", m1["modulos"]);
        Assert.Equal("[PLANO]", m1["origens"]);
        Assert.Equal("[PLANTOES]", m1["efetivos"]);
        Assert.Equal("1", m1["historico"]);
        Assert.Equal("9", m1["checklist_total"]);
        Assert.Equal(string.Join(",", CodigosMundo1), m1["codigos"]);
        Assert.Equal("EM_ANDAMENTO_0", m1["onb"]);
        Assert.Equal("SIM", m1["seed_empresa"]);

        var m2 = Parsear(linhas[1]);
        Assert.StartsWith("T1_MUNDO2", linhas[1]);
        Assert.Equal("[]", m2["modulos"]);
        Assert.Equal("[]", m2["origens"]);
        Assert.Equal("[vazio]", m2["efetivos"]);
        Assert.Equal("0", m2["historico"]);
        Assert.Equal("6", m2["checklist_total"]);
        Assert.Equal(string.Join(",", CodigosMundo2), m2["codigos"]);
        Assert.Equal("EM_ANDAMENTO_0", m2["onb"]);
        Assert.Equal("SIM", m2["seed_empresa"]);
    }

    // =========================================================================
    // Fragmentos de codigo (contrato estatico)
    // =========================================================================

    // Fato 3 - o contrato da rodada esta no codigo: kernel contrata os incluidos do plano
    // antes de materializar a jornada (mesma transacao, origem PLANO), guard casa
    // /PrimeirosPassos com AJEDA comum, controller relee o status pos-reavaliacao para a UI
    // nunca mixar seed com estado real, e as quatro superficies expem a orientacao guiada.
    [Fact]
    public void Codigo_carrega_o_contrato_da_jornada_por_modulo()
    {
        var kernel = File.ReadAllText(Caminho("backend/PlantaoPro.Api/SelfServiceServices.cs"));
        Assert.Contains("await ContratarModulosIncluidosDoPlanoAsync(cn, tx, tenantId, usuarioId, request.Plano.PlanoId);", kernel);
        Assert.Contains("private static async Task ContratarModulosIncluidosDoPlanoAsync(", kernel);
        Assert.Contains("'PLANO','ATIVO',0,", kernel);
        // A fonte usa string verbatim C#: o jsonb aparece com aspas DOBRADAS no .cs (renderiza {"origem":...});
        // o fragmento mira o trecho exato — incluindo a virgula entre os campos que ja quebrou a rota uma vez.
        Assert.Contains("\"\"origem\"\":\"\"PLANO\"\",\"\"incluidoNoPlano\"\":true", kernel);
        // o hook roda ANTES da materializacao (ordem no texto-fonte do provisionamento).
        var idxHook = kernel.IndexOf("await ContratarModulosIncluidosDoPlanoAsync(cn, tx", StringComparison.Ordinal);
        var idxOnb = kernel.IndexOf("await CriarOnboardingAsync(cn, tx", StringComparison.Ordinal);
        Assert.True(idxHook > 0 && idxOnb > 0 && idxHook < idxOnb,
            "o contrato dos modulos deve executar antes da materializacao da jornada.");

        var guard = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Services/Security/SaasRouteGuardFilter.cs"));
        Assert.Contains("[\"PrimeirosPassos\"] = \"AJUDA\",", guard);

        var ctrlWeb = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Controllers/OnboardingController.cs"));
        Assert.Contains("vm.ProximaAcaoLink = proxima.LinkAcao;", ctrlWeb);
        Assert.Contains("var statusRelido = await ReadApiResponse<OnboardingStatusWebModel>(client, \"api/onboarding/status\");", ctrlWeb);
        Assert.Contains("grupo.Obrigatorias = itens.Count(i => i.Obrigatorio && i.Status != \"PULADA\");", ctrlWeb);

        var ctrlAssinatura = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Controllers/MinhaAssinaturaController.cs"));
        Assert.Contains("\"api/onboarding/status\"", ctrlAssinatura);
        Assert.Contains("FINALIZADO", ctrlAssinatura);

        var vmAssinatura = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Models/MinhaAssinaturaViewModel.cs"));
        Assert.Contains("public OnboardingStatusWebModel? JornadaImplantacao { get; set; }", vmAssinatura);

        var viewOnb = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Views/Onboarding/Index.cshtml"));
        Assert.Contains("UiText.L(\"Onboarding.NextStepCta\")", viewOnb);
        Assert.Contains("UiText.L(\"Onboarding.GroupProgress\")", viewOnb);

        var viewAssinatura = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Views/MinhaAssinatura/Index.cshtml"));
        Assert.Contains("Model.JornadaImplantacao is { } jImplantacao", viewAssinatura);
        Assert.Contains("Continuar implanta", viewAssinatura);

        var viewSucesso = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Views/Cadastro/Sucesso.cshtml"));
        Assert.Contains("jornada guiada", viewSucesso);
        Assert.Contains("menu <strong>Implantação</strong>", viewSucesso);

        var viewPp = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Views/PrimeirosPassos/Index.cshtml"));
        Assert.Contains("Ver minha implanta", viewPp);

        var resx = File.ReadAllText(Caminho("backend/PlantaoPro.Web/Localization/UiTexts.resx"));
        Assert.Contains("<data name=\"Onboarding.NextStepCta\"", resx);
        Assert.Contains("<data name=\"Onboarding.NextStepBlockedNote\"", resx);
        Assert.Contains("<data name=\"Onboarding.GroupProgress\"", resx);
    }

    // =========================================================================
    // Kernel em nivel de servico (dois mundos deterministicos, crash-idempotentes)
    // =========================================================================

    // Fato 4 - o núcleo do contrato em nível de serviço (mesma transação do cadastro):
    // plano com PLANTOES incluido=true -> tenant_modulos (origem PLANO) efetivo via
    // modulos_efetivos(), jornada materializada com as 3 etapas de plantões, historico
    // CONTRATAR_PLANO e, após a reavaliação contra dados persistidos (telefone presente),
    // progresso 14 (1 de 7 obrigatórias) com próxima = convite de equipe. Plano sem modulos
    // -> contrato vazio, jornada geral de 6 etapas, progresso 0 (telefone ausente).
    [Fact]
    public async Task Servico_kernel_contrata_os_incluidos_do_plano_na_materiacao()
    {
        await SementePronta;
        var svc = NovoSelfService();
        var jornada = new OnboardingJornadaService(BuildCfg(), NullLogger<OnboardingJornadaService>.Instance);

        var cnpjMod = "99991010000177";
        var cnpjSem = "99991010000188";
        try
        {
            var planoMod = await PlanoDrillAsync("r99_teste_plano_mod", withVinculo: true);
            var planoSem = await PlanoDrillAsync("r99_teste_plano_sem", withVinculo: false);

            var rMod = await svc.FinalizarCadastroAsync(Pedido(cnpjMod, "r99-kernel-mod@exemplo.test", planoMod, comTelefone: true), null, null);
            Assert.True(rMod.Success, rMod.Message);
            var idsMod = rMod.Data!;

            var rSem = await svc.FinalizarCadastroAsync(Pedido(cnpjSem, "r99-kernel-sem@exemplo.test", planoSem, comTelefone: false), null, null);
            Assert.True(rSem.Success, rSem.Message);
            var idsSem = rSem.Data!;

            // ---- mundo com modulo: contrato + materializacao
            Assert.Equal("PLANTOES|PLANO", await ConsultarAsync<string>(@"select upper(ms.codigo)||'|'||tm.origem
                from plantaopro.tenant_modulos tm join plantaopro.modulos_sistema ms on ms.id=tm.modulo_id
                where tm.tenant_id=@t and tm.reg_status='A'", new { t = idsMod.TenantId }));
            Assert.Equal(1, await ConsultarAsync<int>(
                "select count(*) from plantaopro.tenant_modulos where tenant_id=@t and reg_status='A'", new { t = idsMod.TenantId }));
            Assert.Equal(1, await ConsultarAsync<int>(
                "select count(*) from plantaopro.modulos_efetivos(@t) f where upper(f.codigo)='PLANTOES'", new { t = idsMod.TenantId }));
            Assert.Equal(1, await ConsultarAsync<int>(
                "select count(*) from plantaopro.modulos_efetivos(@t)", new { t = idsMod.TenantId }));
            Assert.Equal(1, await ConsultarAsync<int>(@"select count(*) from plantaopro.tenant_modulos_historico h
                join plantaopro.tenant_modulos tm on tm.id=h.tenant_modulo_id
                where tm.tenant_id=@t and h.acao='CONTRATAR_PLANO'", new { t = idsMod.TenantId }));
            Assert.Equal(9, await ConsultarAsync<int>(
                "select count(*) from plantaopro.tenant_onboarding_checklist where tenant_id=@t", new { t = idsMod.TenantId }));
            Assert.Equal(0, await ConsultarAsync<int>(
                "select count(*) from plantaopro.tenant_onboarding_checklist where tenant_id=@t and (codigo like 'ONB_SD_%' or codigo like 'ONB_AD_%')", new { t = idsMod.TenantId }));
            var onbSeed = await ConsultarAsync<(string Status, int Progresso)>(
                "select status, progresso from plantaopro.tenant_onboarding where tenant_id=@t", new { t = idsMod.TenantId });
            Assert.Equal("EM_ANDAMENTO", onbSeed.Status);
            Assert.Equal(0, onbSeed.Progresso);

            // reavaliacao contra os dados persistidos: telefone presente -> a etapa da
            // empresa se conclui sozinha (AUTOMATICO) e a jornada avanca para o convite.
            var reav = await jornada.ReavaliarAsync(idsMod.TenantId, idsMod.ClienteId);
            Assert.True(reav.Success, reav.Message);
            var onbApos = await ConsultarAsync<(int Progresso, string Proxima)>(
                "select progresso, coalesce(proxima_acao,'') from plantaopro.tenant_onboarding where tenant_id=@t", new { t = idsMod.TenantId });
            Assert.Equal(14, onbApos.Progresso);
            Assert.Contains("Convidar o primeiro integrante", onbApos.Proxima);
            var etapaEmpresa = await ConsultarAsync<(string Status, string Origem)>(@"select status, coalesce(origem_conclusao,'')
                from plantaopro.tenant_onboarding_checklist where tenant_id=@t and codigo='ONB_EMPRESA_DADOS'", new { t = idsMod.TenantId });
            Assert.Equal("CONCLUIDO", etapaEmpresa.Status);
            Assert.Equal("AUTOMATICO", etapaEmpresa.Origem);

            // ---- mundo sem modulos: mesmo funil, jornada geral imovel (sem regressao)
            Assert.Equal(0, await ConsultarAsync<int>(
                "select count(*) from plantaopro.tenant_modulos where tenant_id=@t and reg_status='A'", new { t = idsSem.TenantId }));
            Assert.Equal(6, await ConsultarAsync<int>(
                "select count(*) from plantaopro.tenant_onboarding_checklist where tenant_id=@t", new { t = idsSem.TenantId }));
            var reav2 = await jornada.ReavaliarAsync(idsSem.TenantId, idsSem.ClienteId);
            Assert.True(reav2.Success, reav2.Message);
            var onb2 = await ConsultarAsync<(int Progresso, string Proxima)>(
                "select progresso, coalesce(proxima_acao,'') from plantaopro.tenant_onboarding where tenant_id=@t", new { t = idsSem.TenantId });
            Assert.Equal(0, onb2.Progresso);
            Assert.Contains("Completar os dados da empresa", onb2.Proxima);
        }
        finally
        {
            await LimparPorCnpj(cnpjMod);
            await LimparPorCnpj(cnpjSem);
            await RemoverPlanoPorCodigo("r99_teste_plano_mod");
            await RemoverPlanoPorCodigo("r99_teste_plano_sem");
        }
    }

    // =========================================================================
    // Infraestrutura local
    // =========================================================================

    private readonly Task SementePronta = SementarAsync();

    private static async Task SementarAsync()
    {
        // crash-idempotencia (padrao R96/r98): remove tudo ligado aos CNPJs/planos propios antes.
        foreach (var cnpj in new[] { "99991010000177", "99991010000188" })
            await LimparPorCnpj(cnpj);
        await RemoverPlanoPorCodigo("r99_teste_plano_mod");
        await RemoverPlanoPorCodigo("r99_teste_plano_sem");
    }

    private static IConfiguration BuildCfg() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
        .Build();

    private static SelfServiceSaasService NovoSelfService()
    {
        var cfg = BuildCfg();
        var audit = new R99FakeAuditService();
        return new SelfServiceSaasService(cfg, new TenantContextService(new HttpContextAccessor(), cfg, NullLogger<TenantContextService>.Instance),
            audit, new AssinaturaGuardService(cfg, audit, NullLogger<AssinaturaGuardService>.Instance),
            NullLogger<SelfServiceSaasService>.Instance,
            new CobrancaService(cfg, audit, Array.Empty<ICobrancaProvider>(), NullLogger<CobrancaService>.Instance));
    }

    private static CadastroSelfServiceRequest Pedido(string cnpj, string email, Guid plano, bool comTelefone) => new()
    {
        Empresa = new CadastroEmpresaRequest
        {
            NomeFantasia = "R99 Teste " + cnpj[..4],
            RazaoSocial = "R99 Teste LTDA",
            Cnpj = cnpj,
            Cidade = "Olinda",
            Uf = "PE",
            Telefone = comTelefone ? "(81) 99999-0007" : string.Empty,
            EmailCorporativo = "r99-emp@exemplo.test"
        },
        Plano = new CadastroPlanoRequest { PlanoId = plano, Periodicidade = "MENSAL", AceiteTermos = true, AceitePrivacidade = true, ConsentimentoLgpd = true },
        UsuarioAdmin = new CadastroUsuarioAdminRequest { Nome = "R99 Admin", Email = email, Senha = "R99senha123!" },
        StatusAssinatura = "TRIAL",
        DiasTrial = 5
    };

    /// <summary>Plano fixture deterministico (codigo unico parcial entre ativos) com, ou sem,
    /// vinculo PLANTOES incluido=true — o mesmo shape da governanca /api/modulos/{id}/vincular-plano.</summary>
    private static async Task<Guid> PlanoDrillAsync(string codigo, bool withVinculo)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.ExecuteAsync($@"
delete from plantaopro.plano_modulos where plano_id in (select id from plantaopro.planos where upper(codigo)=upper('{codigo}'));
delete from plantaopro.planos where upper(codigo)=upper('{codigo}');
insert into plantaopro.planos(id,nome,codigo,valor_mensal,valor_anual,status,reg_status,criado_em,reg_date,dados,
 limite_medicos,limite_hospitais,limite_plantoes_mes,limite_usuarios,limite_convites_mes,
 permite_mobile,permite_bi,permite_relatorios_avancados,permite_integracoes,permite_operacao_assistida,permite_suporte_prioritario,
 permite_api,ordem_exibicao,possui_mobile,possui_bi,possui_relatorios_avancados,permite_trial,dias_trial,metadata,permite_relatorios)
values(gen_random_uuid(),'R99 Teste {codigo}','{codigo}',100.00,1250.00,'ATIVO','A',now(),now(),'{{}}'::jsonb,
 0,0,0,0,0,false,false,false,false,false,false,false,955,false,false,false,true,5,'{{}}'::jsonb,false);");
        if (withVinculo)
        {
            await cn.ExecuteAsync($@"insert into plantaopro.plano_modulos(id,plano_id,modulo_id,codigo_modulo,habilitado,status,reg_date,reg_status,metadata,incluido,limite,preco_adicional)
select gen_random_uuid(),p.id,m.id,m.codigo,true,'ATIVO',now(),'A','{{}}'::jsonb,true,0,0
from plantaopro.planos p join plantaopro.modulos_sistema m on upper(m.codigo)='PLANTOES' and m.reg_status='A' and upper(coalesce(m.status,'ATIVO'))='ATIVO'
where upper(p.codigo)=upper('{codigo}')");
        }
        // interpolacao direta (Dapper nao trata '{0}' sem '@' como parametro — ficaria literal).
        return await cn.ExecuteScalarAsync<Guid>($@"select id from plantaopro.planos where upper(codigo)=upper('{codigo}')");
    }

    private static async Task RemoverPlanoPorCodigo(string codigo)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.ExecuteAsync($@"
delete from plantaopro.plano_recursos where plano_id in (select id from plantaopro.planos where upper(codigo)=upper('{codigo}'));
delete from plantaopro.plano_faq where plano_id in (select id from plantaopro.planos where upper(codigo)=upper('{codigo}'));
delete from plantaopro.plano_modulos where plano_id in (select id from plantaopro.planos where upper(codigo)=upper('{codigo}'));
delete from plantaopro.planos where upper(codigo)=upper('{codigo}');");
    }

    private static async Task<T> ConsultarAsync<T>(string sql, object parametros)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        return await cn.QuerySingleAsync<T>(sql, parametros);
    }

    private static async Task LimparPorCnpj(string cnpj)
    {
        await TestInfraRetry.ExecutarComRetryImpasseAsync(async () =>
        {
            await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
            await cn.ExecuteAsync(@"
delete from plantaopro.cadastro_cliente_pagamentos_iniciais where assinatura_id in (select a.id from plantaopro.assinaturas a join plantaopro.clientes c on c.id=a.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.lgpd_consentimentos where tenant_id in (select t.id from plantaopro.tenants t join plantaopro.clientes c on c.id=t.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.tenant_onboarding_checklist where tenant_id in (select t.id from plantaopro.tenants t join plantaopro.clientes c on c.id=t.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.tenant_onboarding where tenant_id in (select t.id from plantaopro.tenants t join plantaopro.clientes c on c.id=t.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.tenant_modulos_historico where tenant_id in (select t.id from plantaopro.tenants t join plantaopro.clientes c on c.id=t.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.tenant_modulos where tenant_id in (select t.id from plantaopro.tenants t join plantaopro.clientes c on c.id=t.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.tenant_white_label where tenant_id in (select t.id from plantaopro.tenants t join plantaopro.clientes c on c.id=t.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.usuarios_perfis where usuario_id in (select u.id from plantaopro.usuarios u join plantaopro.clientes c on c.id=u.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.perfis where tenant_id in (select t.id from plantaopro.tenants t join plantaopro.clientes c on c.id=t.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.usuarios where id in (select u.id from plantaopro.usuarios u join plantaopro.clientes c on c.id=u.cliente_id where regexp_replace(c.cnpj,'\D','','g')=@cnpj);
delete from plantaopro.assinaturas where cliente_id in (select id from plantaopro.clientes where regexp_replace(cnpj,'\D','','g')=@cnpj);
delete from plantaopro.clientes where regexp_replace(cnpj,'\D','','g')=@cnpj;
delete from plantaopro.cadastro_cliente_solicitacoes where regexp_replace(cnpj,'\D','','g')=@cnpj;",
                new { cnpj });
        });
    }

    private sealed class R99FakeAuditService : IAuditService
    {
        public Task RegistrarAsync(Guid? usuarioId, Guid? clienteId, string entidade, Guid? entidadeId, string acao,
            object? detalhes, bool sucesso, string? ipOrigem, string? perfil, CancellationToken ct = default) => Task.CompletedTask;

        public Task LogAsync(Guid? userId, string acao, string entidade, Guid? registroId, string descricao,
            string? valorAnterior = null, string? valorNovo = null, string? ip = null, string? userAgent = null) => Task.CompletedTask;
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
}
// GetOrThrow: reutiliza DicionarioExtensaoR96 (mesmo namespace; duas extensoes iguais em
// arquivos distintos do mesmo namespace viram CS0121 em toda a suíte).
