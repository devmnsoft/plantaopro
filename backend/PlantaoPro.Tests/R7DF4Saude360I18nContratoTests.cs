using System;
using System.IO;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R7-D-F4: jornada Saúde 360 (renderizadores compartilhados) em recursos localizados.
/// Contrato de fonte (mesmo padrão F1/F2/F3): os parciais Modulo/Formulario/_PageHelp/
/// _LookupSelect não podem voltar a carregar literais pt-BR; o vazio é específico por
/// módulo (com nome do módulo + CTA real da ViewModel); Consultas/Index é o wrapper
/// padrão da família (estado vazio + CTAs reais) e não o stub de badges estático.
/// </summary>
public sealed class R7DF4Saude360I18nContratoTests
{
    private static string Root => RepositoryPathResolver.ResolveRoot();

    private static string Read(params string[] partes) => File.ReadAllText(Path.Combine(new[] { Root }.Concat(partes).ToArray()));

    [Fact]
    public void Saude360Modulo_UseRecursosEvazioEspecificoPorModulo()
    {
        var view = Read("backend", "PlantaoPro.Web", "Views", "Saude360", "Modulo.cshtml");

        foreach (var chave in new[]
                 {
                     "Clinical.PhWhenToUse", "Clinical.PhStep1", "Clinical.PhStep4", "Clinical.PhTipAudit",
                     "Clinical.PhTipSensitive", "Clinical.PhProfileNotes", "Nav.SectionClinical", "Clinical.JourneyTagline",
                     "Clinical.JourneyAria", "Clinical.JourneyPatient", "Clinical.JourneyAppointment",
                     "Clinical.JourneyCheckIn", "Clinical.JourneyCallBoard", "Clinical.JourneyTriage",
                     "Clinical.JourneyConsult", "Clinical.JourneyPrescription", "Clinical.JourneyFinance",
                     "Clinical.JourneyCountAppointments", "Clinical.JourneyCountWaiting", "Clinical.JourneyCountRxPending",
                     "Clinical.NextKicker", "Clinical.NextTitle", "Clinical.NextBody", "Clinical.NextRulesButton",
                     "Clinical.PatientContextAria", "Clinical.PatientBlockData", "Clinical.PatientBlockBilling", "Clinical.PatientBlockBody",
                     "Clinical.EmptyTitle", "Clinical.EmptyBody", "Clinical.EmptyTableBody",
                     "Clinical.KpiStripAria", "Clinical.KpiPlanMin", "Clinical.KpiUnavailable", "Clinical.KpiProtectedHint",
                     "Clinical.RecordsKicker", "Clinical.RecordsTableAria", "Clinical.ThIdentity", "Clinical.ThRecordDate",
                     "Clinical.LgpdLabel", "Clinical.LgpdBody"
                 })
        {
            Assert.Contains($"UiText.L(\"{chave}\")", view);
        }

        // vazio des-genericizado: título formatado com o módulo + CTA real da ViewModel
        Assert.Contains("string.Format(System.Globalization.CultureInfo.InvariantCulture, UiText.L(\"Clinical.EmptyTitle\"), vm.Modulo)", view);
        Assert.Contains("primaria != null ? primaria.Titulo : null", view);

        foreach (var literal in new[]
                 {
                     "Nenhum registro encontrado", "A API não retornou registros reais para este módulo.\")",
                     "jornada assistencial</span>", "Etapas da jornada Saúde 360", "Agendamentos de hoje",
                     "Continue somente com vínculo e status reais", "Ver regras da jornada",
                     "Contexto longitudinal do paciente", "Selecione um paciente retornado pela API",
                     "Plano mínimo", "resultado real carregado", "a API não confirmou o total",
                     "tenant, perfil e auditoria", "Registros assistenciais", "Identificação</th>",
                     "LGPD e auditoria:", "Use esta tela para conduzir a jornada assistencial",
                     "Acesso clínico é auditado.", "A tela adapta a operação"
                 })
        {
            Assert.DoesNotContain(literal, view);
        }
    }

    [Fact]
    public void FormularioEParticularesCompartilhadosUsemRecursos()
    {
        var form = Read("backend", "PlantaoPro.Web", "Views", "Saude360", "Formulario.cshtml");
        foreach (var chave in new[]
                 {
                     "Clinical.FormHelpDescription", "Clinical.FormHelpStep2", "Clinical.FormHelpTip1", "Clinical.FormHelpProfileNotes",
                     "Clinical.FormSubline", "Clinical.FormHiddenHint", "Shift.Back", "Clinical.SectionPersonalData",
                     "Clinical.SectionContactAddress", "Clinical.SectionScheduleLinks", "Clinical.SectionTriageVitals",
                     "Clinical.SectionObservationsBilling", "Clinical.LabelFullName", "Clinical.LabelGender", "Clinical.LabelCns",
                     "Profile.Phone", "Clinical.LookupDoctor", "Clinical.LookupHealthPlan", "Clinical.LookupPatientPlaceholder",
                     "Shift.ThSpecialty", "Shift.Start", "Clinical.ThCode", "Shift.Value", "Clinical.LabelChiefComplaint",
                     "Clinical.LabelRiskClassification", "Clinical.LabelSelectOption", "Clinical.TriageRiskHelp",
                     "Clinical.VitalSystolic", "Clinical.VitalSpO2", "Clinical.LabelPaymentMethod", "Clinical.LabelMarkPrimary",
                     "Clinical.ButtonCancel", "Clinical.LoadingSaving", "Clinical.ButtonSave", "Clinical.ButtonFinishTriage",
                     "Clinical.TriageFinishDisabledTitle", "Clinical.ButtonReferConsult", "Clinical.TriageReferDisabledTitle"
                 })
        {
            Assert.Contains($"UiText.L(\"{chave}\")", form);
        }
        // valores de máquina do select de risco permanecem íntegros
        Assert.Contains("<option>EMERGENCIA</option><option>MUITO_URGENTE</option><option>URGENTE</option><option>POUCO_URGENTE</option><option>NAO_URGENTE</option>", form);

        foreach (var literal in new[]
                 {
                     "Dados pessoais", "Contato e endereço", "Agendamento e vínculos", "Triagem e sinais vitais", "Observações e cobrança",
                     "Nome completo", "Nome social", "CPF opcional", "Documento alternativo",
                     "Busque o paciente pelo nome", "Selecione pelo nome do profissional", "Selecione convênio ou plano",
                     "Queixa principal", "Decisão do profissional autorizado", "PAS (mmHg)", "Saturação (%)",
                     "Forma de pagamento", ">Carteirinha<", "Marcar como principal",
                     "Salvando...", "data-loading-text=\"Salvar\"", "Finalizando...", "Finalizar triagem",
                     "Encaminhar para consulta", "Salve a triagem para obter um identificador real",
                     "Formulário auditável com escopo por tenant", "Preencha os campos com orientação contextual"
                 })
        {
            Assert.DoesNotContain(literal, form);
        }

        var pageHelp = Read("backend", "PlantaoPro.Web", "Views", "Shared", "_PageHelp.cshtml");
        foreach (var chave in new[] { "Clinical.PageHelpAria", "Clinical.PageHelpYouAreHere", "Clinical.PageHelpWhenToUseLabel", "Clinical.PageHelpNextSteps", "Clinical.PageHelpTips", "Clinical.PageHelpManual" })
            Assert.Contains($"UiText.L(\"{chave}\")", pageHelp);
        foreach (var literal in new[] { "Ajuda contextual da tela", "Você está aqui", "Quando usar:", "Próximos passos", "Dicas rápidas", ">Manual</a>" })
            Assert.DoesNotContain(literal, pageHelp);

        var lookup = Read("backend", "PlantaoPro.Web", "Views", "Shared", "_LookupSelect.cshtml");
        Assert.Contains("UiText.L(\"Clinical.LookupSelected\")", lookup);
        Assert.Contains("UiText.L(\"Clinical.LookupNotFoundHelp\")", lookup);
        Assert.DoesNotContain("Registro selecionado", lookup);
        Assert.DoesNotContain("Não encontrou? Veja como cadastrar novo.", lookup);
    }

    [Fact]
    public void ConsultasIndex_EWrapperPadraoDaFamiliaSemStubEstatico()
    {
        var view = Read("backend", "PlantaoPro.Web", "Views", "Consultas", "Index.cshtml");

        Assert.Contains("@model PlantaoPro.Web.Models.Saude360PageViewModel", view);
        Assert.Contains(@"Html.PartialAsync(""~/Views/Saude360/Modulo.cshtml"", Model)", view);
        foreach (var literal in new[] { "Jornada clínica com tenant", "Consulta médica</span>", "Auditoria clínica", "<span class=\"badge bg-primary-subtle text-primary\">PlantãoPro" })
            Assert.DoesNotContain(literal, view);
    }

    [Fact]
    public void RecursosContemAsChavesClinicasEmTodosOsQuatroIdiomas()
    {
        foreach (var arquivo in new[] { "UiTexts.resx", "UiTexts.en.resx", "UiTexts.es.resx", "UiTexts.fr.resx" })
        {
            var resx = Read("backend", "PlantaoPro.Web", "Localization", arquivo);
            foreach (var chave in new[]
                     {
                         "Clinical.JourneyAria", "Clinical.JourneyCountAppointments", "Clinical.NextTitle",
                         "Clinical.EmptyTitle", "Clinical.EmptyTableBody", "Clinical.KpiProtected",
                         "Clinical.RecordsTableAria", "Clinical.LgpdLabel", "Clinical.PageHelpYouAreHere",
                         "Clinical.FormHelpStep2", "Clinical.SectionTriageVitals", "Clinical.LabelChiefComplaint",
                         "Clinical.LabelRiskClassification", "Clinical.VitalSpO2", "Clinical.ButtonFinishTriage",
                         "Clinical.TriageReferDisabledTitle", "Clinical.LookupSelected", "Clinical.LookupNotFoundHelp"
                     })
            {
                Assert.Contains($"<data name=\"{chave}\"", resx, StringComparison.Ordinal);
            }
        }
    }
}
