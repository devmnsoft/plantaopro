using System;
using System.IO;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R7-D-F5: onboarding/implantação em recursos localizados + acentuação corrigida.
/// Contrato de fonte (mesmo padrão F1–F4): controller e views não podem voltar a
/// carregar literais pt-BR; os acentos corrigidos (módulos, concluída) ficam nos
/// valores pt-BR dos recursos; o JS da Central de Implantação consome data-attrs
/// localizados (padrão F1) mantendo valores de máquina intactos.
/// </summary>
public sealed class R7DF5OnboardingImplantacaoI18nContratoTests
{
    private static string Root => RepositoryPathResolver.ResolveRoot();

    private static string Read(params string[] partes) => File.ReadAllText(Path.Combine(new[] { Root }.Concat(partes).ToArray()));

    [Fact]
    public void OnboardingController_MensagensEmRecursosEAcentosCorretosNoPtBr()
    {
        var controller = Read("backend", "PlantaoPro.Web", "Controllers", "OnboardingController.cs");
        foreach (var chave in new[]
                 {
                     "Onboarding.NoClientContext", "Onboarding.StartedSuccess", "Onboarding.ReevaluatedSuccess",
                     "Onboarding.StepCompletedSuccess", "Onboarding.StepSkippedSuccess", "Onboarding.StepRestoredSuccess",
                     "Onboarding.ClientCreateFailed", "Onboarding.ClientCreateUnexpectedError", "Onboarding.SummaryUnavailable",
                     "Onboarding.SummaryLoadError", "Onboarding.GeneralGroup"
                 })
        {
            Assert.Contains($"UiText.L(\"{chave}\")", controller);
        }
        // acentos corrigidos: literais antigos não voltam
        foreach (var literal in new[] { "modulos contratados", "Etapa concluida.", "as etapas refletem os modulos", "Onboarding pertence a um cliente." })
            Assert.DoesNotContain(literal, controller);

        // correção confirmada nos valores pt-BR dos recursos
        var resx = Read("backend", "PlantaoPro.Web", "Localization", "UiTexts.resx");
        Assert.Contains("<value>Onboarding iniciado: as etapas refletem os módulos contratados deste cliente.</value>", resx);
        Assert.Contains("<value>Etapa concluída.</value>", resx);
    }

    [Fact]
    public void OnboardingViewsUsemRecursosEMaquinasIntegras()
    {
        var index = Read("backend", "PlantaoPro.Web", "Views", "Onboarding", "Index.cshtml");
        foreach (var chave in new[]
                 {
                     "Onboarding.PageTitle", "Onboarding.HeroTitle", "Onboarding.HeroBody", "Onboarding.ReassessNow",
                     "Onboarding.NewClientWithPlan", "Onboarding.NoClientTitle", "Onboarding.RegisterClient",
                     "Onboarding.StartEyebrow", "Onboarding.StartButton", "Onboarding.DoneTitle", "Onboarding.InProgressTitle",
                     "Onboarding.NextStepLabel", "Onboarding.AllVerified", "Onboarding.ProgressAria", "Onboarding.LegendDone",
                     "Onboarding.StatusPending", "Onboarding.StepsAria", "Onboarding.ResponsibleMnssoft", "Onboarding.OptionalSuffix",
                     "Onboarding.DependenceNote", "Onboarding.CriterionLabel", "Onboarding.OpenScreen", "Onboarding.CompleteStep",
                     "Onboarding.SkipJustificationAria", "Onboarding.RestoreStep", "Onboarding.PillLocked"
                 })
        {
            Assert.Contains($"UiText.L(\"{chave}\")", index);
        }
        // valores de máquina de status/perfil permanecem íntegros (comparações e dados)
        Assert.Contains("Model.Status == \"FINALIZADO\"", index);
        Assert.Contains("e.Status == \"CONCLUIDO\"", index);
        Assert.Contains("e.Responsavel == \"MNSOFT\"", index);
        foreach (var literal in new[]
                 {
                     "Implantação orientada ao contrato", "Onboarding de clientes</h2>", "Reavaliar agora</button>",
                     "Sem cliente selecionado", "Onboarding ainda não iniciado", "Iniciar onboarding",
                     "Checklist do cliente", "Jornada em andamento", "Todas as etapas obrigatórias foram verificadas",
                     "Legenda de status", "Depende da etapa anterior deste módulo.", "Abrir tela", "Justificativa para pular"
                 })
        {
            Assert.DoesNotContain(literal, index);
        }

        var wizard = Read("backend", "PlantaoPro.Web", "Views", "Onboarding", "NovoCliente.cshtml");
        foreach (var chave in new[]
                 {
                     "Onboarding.WizardTitle", "Onboarding.WizardHeroTitle", "Onboarding.CancelWizard", "Onboarding.StepperAria",
                     "Onboarding.StepCompany", "Onboarding.StepReview", "Onboarding.CompanySectionBody", "Onboarding.LabelCompanyName",
                     "Onboarding.HelpCnpj", "Onboarding.LabelCorporateEmail", "Profile.Phone", "Onboarding.LabelUf",
                     "Onboarding.PlanSectionTitle", "Onboarding.LabelPlanId", "Onboarding.HelpPlanId", "Onboarding.StatusTrial",
                     "Onboarding.LabelUnitManager", "Onboarding.HelpUnitManager", "Onboarding.AdminSectionTitle",
                     "Clinical.LabelFullName", "Onboarding.LabelTempPassword", "Onboarding.HelpTempPassword",
                     "Shift.Back", "Onboarding.ReviewAndCreate", "Onboarding.SummaryBeforeTitle", "Onboarding.SummaryCheck4",
                     "Onboarding.SummaryNoSample"
                 })
        {
            Assert.Contains($"UiText.L(\"{chave}\")", wizard);
        }
        // máquina: valores dos selects e passos do stepper intactos
        Assert.Contains("<option value=\"ATIVO\">", wizard);
        Assert.Contains("<option value=\"TESTE\">", wizard);
        Assert.Contains("data-step=\"1\" aria-current=\"step\"", wizard);
        foreach (var literal in new[]
                 {
                     "Configure empresa, plano, unidade e administrador inicial", "Prepare uma operação segura desde o primeiro acesso",
                     "Cancelar onboarding", "Empresa</li>", "Dados fiscais e canais de contato do tenant", "Razão social",
                     "Use a denominação registrada no CNPJ", "Nome fantasia", "Plano e unidade inicial",
                     "Identificador do plano", "Período de teste</option>", "Nome da unidade", "Tipo de unidade",
                     "Administrador inicial</h2>", "Senha temporária", "Revisar e criar cliente", "Antes de concluir",
                     "Nenhum dado de exemplo é preenchido automaticamente"
                 })
        {
            Assert.DoesNotContain(literal, wizard);
        }

        var sucesso = Read("backend", "PlantaoPro.Web", "Views", "Onboarding", "Sucesso.cshtml");
        foreach (var chave in new[] { "Onboarding.SuccessPageTitle", "Onboarding.SuccessAlert", "Onboarding.SuccessPlanLabel", "Onboarding.SuccessUnitLabel", "Onboarding.SuccessAdminLabel", "Onboarding.SuccessSubscriptionLabel", "Onboarding.SuccessCreatedLabel", "Onboarding.GoToClients" })
            Assert.Contains($"UiText.L(\"{chave}\")", sucesso);
        foreach (var literal in new[] { "Cliente criado com sucesso.", "Usuário administrador:", "Criado em:", ">Ir para clientes</a>" })
            Assert.DoesNotContain(literal, sucesso);
    }

    [Fact]
    public void ImplantacaoCentralERespectivosJsUsemRecursosEDataAttrs()
    {
        var view = Read("backend", "PlantaoPro.Web", "Views", "Implantacao", "Index.cshtml");
        foreach (var chave in new[]
                 {
                     "Implantacao.PageTitle", "Implantacao.HeroTitle", "Implantacao.HeroBody", "Implantacao.ValidateAll",
                     "Implantacao.Refresh", "Implantacao.Loading", "Implantacao.KpisAria", "Implantacao.KpiReadiness",
                     "Implantacao.KpiSteps", "Implantacao.KpiPending", "Implantacao.KpiAlerts", "Implantacao.FilterStatus",
                     "Implantacao.FilterAll", "Implantacao.ProgressAria", "Implantacao.EmptyFiltered", "Implantacao.TimelineTitle",
                     "Implantacao.EvidenceTitle", "Implantacao.WaitingLoad", "Implantacao.ConfirmTitle", "Implantacao.CloseAria",
                     "Implantacao.ConfirmBody", "Clinical.ButtonCancel", "Implantacao.JsDueDateLabel", "Implantacao.JsValidateStep",
                     "Implantacao.JsLoadError", "Implantacao.JsValidateError", "Onboarding.ResponsiblePrefix"
                 })
        {
            Assert.Contains($"UiText.L(\"{chave}\")", view);
        }
        // data-attrs localizados expostos ao JS (padrão F1)
        foreach (var attr in new[] { "data-responsavel-label=", "data-prazo-label=", "data-validar-etapa-label=", "data-load-error=", "data-validate-error=" })
            Assert.Contains(attr, view);
        // máquina: rotas da API, ids e opções de status intactos
        Assert.Contains("data-prontidao-url=\"/api/implantacao/prontidao\"", view);
        Assert.Contains("<option>PENDENTE</option><option>OK</option>", view);
        foreach (var literal in new[] { "Central de Implantação, Diagnóstico e Go-Live", "Painel responsivo para acompanhar KPIs", "Validar tudo</button>", "Carregando informações reais da API...", "Prontidão</span>", "Filtro por status", "Nenhuma etapa encontrada para o filtro selecionado.", "Timeline e histórico", "Aguardando carregamento.", "Executar validação completa de implantação agora?" })
            Assert.DoesNotContain(literal, view);

        var js = Read("backend", "PlantaoPro.Web", "wwwroot", "js", "implantacao.js");
        foreach (var consumo in new[] { "app.dataset.responsavelLabel", "app.dataset.prazoLabel", "app.dataset.validarEtapaLabel", "app.dataset.loadError", "app.dataset.validateError" })
            Assert.Contains(consumo, js);
        Assert.DoesNotContain("Responsável: ${etapa.responsavel}", js);
    }

    [Fact]
    public void RecursosContemAsChavesOnboardingEImplantacaoEmTodosOsQuatroIdiomas()
    {
        foreach (var arquivo in new[] { "UiTexts.resx", "UiTexts.en.resx", "UiTexts.es.resx", "UiTexts.fr.resx" })
        {
            var resx = Read("backend", "PlantaoPro.Web", "Localization", arquivo);
            foreach (var chave in new[]
                     {
                         "Onboarding.NoClientContext", "Onboarding.StartedSuccess", "Onboarding.StepCompletedSuccess", "Onboarding.GeneralGroup",
                         "Onboarding.PageTitle", "Onboarding.ProgressAria", "Onboarding.StatusPending", "Onboarding.OptionalSuffix",
                         "Onboarding.ResponsiblePrefix", "Onboarding.WizardTitle", "Onboarding.LabelCompanyName", "Onboarding.HelpTempPassword",
                         "Onboarding.ReviewAndCreate", "Onboarding.SummaryCheck4", "Onboarding.SuccessPageTitle", "Onboarding.GoToClients",
                         "Implantacao.PageTitle", "Implantacao.HeroTitle", "Implantacao.KpiReadiness", "Implantacao.EmptyFiltered",
                         "Implantacao.ConfirmBody", "Implantacao.JsDueDateLabel", "Implantacao.JsValidateStep", "Implantacao.JsLoadError", "Implantacao.JsValidateError"
                     })
            {
                Assert.Contains($"<data name=\"{chave}\"", resx, StringComparison.Ordinal);
            }
        }
    }
}
