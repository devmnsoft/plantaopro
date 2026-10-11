using System;
using System.IO;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R7-D-F3: jornada Central de Escala (cobertura/convite/substituição) em recursos
/// localizados. Contrato de fonte (mesmo padrão F1/F2/R7-E1): o controller e as views
/// não podem voltar a carregar os literais pt-BR de mensagens, hero, filtros, cards,
/// kanban e estados vazios; as chaves Coverage.*/Shift.* existem nos 4 idiomas.
/// </summary>
public sealed class R7DF3CentralEscalaI18nContratoTests
{
    private static string Root => RepositoryPathResolver.ResolveRoot();

    private static string Read(params string[] partes) => File.ReadAllText(Path.Combine(new[] { Root }.Concat(partes).ToArray()));

    [Fact]
    public void CentralEscalaController_Mensagens_UseRecursosENaoCarregaLiteraisPtBr()
    {
        var cs = Read("backend", "PlantaoPro.Web", "Controllers", "CentralEscalaController.cs");

        foreach (var chave in new[]
                 {
                     "Coverage.LoadFailed", "Coverage.LoadUnexpected", "Coverage.FiltersInvalid",
                     "Coverage.DecisionValidation", "Coverage.DecisionRecorded", "Coverage.DecisionConflict",
                     "Coverage.DecisionForbidden", "Coverage.DecisionNotFound", "Coverage.DecisionUnknown",
                     "Coverage.InviteValidation", "Coverage.InviteMessage", "Coverage.InviteFailed",
                     "Coverage.InviteSent", "Coverage.InviteUnexpected", "Shift.NotFound", "Shift.LoadUnexpected"
                 })
        {
            Assert.Contains($"UiText.L(\"{chave}\")", cs);
        }

        foreach (var literal in new[]
                 {
                     "Não foi possível carregar a Central de Escala.",
                     "Falha inesperada ao carregar a Central de Escala.",
                     "Revise os filtros informados.",
                     "Selecione uma decisão explícita e informe justificativa entre 3 e 1.000 caracteres.",
                     "Decisão registrada. O profissional já pode acompanhar a situação no portal.",
                     "A solicitação mudou ou já foi decidida. Revise o estado atual antes de tentar novamente.",
                     "Sua permissão ou vínculo com a unidade não permite esta decisão.",
                     "Solicitação indisponível no contexto atual.",
                     "Não foi possível confirmar o resultado. Consulte o estado antes de repetir.",
                     "Selecione um plantão e um médico válidos para enviar o convite.",
                     "Convite enviado pela Central de Cobertura.",
                     "O convite não foi enviado. Verifique a disponibilidade e se já existe um convite pendente.",
                     "Convite enviado e registrado com sucesso.",
                     "Não foi possível enviar o convite agora. Tente novamente.",
                     "Plantão não encontrado.",
                     "Falha inesperada ao carregar o plantão."
                 })
        {
            Assert.DoesNotContain(literal, cs);
        }
    }

    [Fact]
    public void CentralEscalaIndex_UseRecursosENaoCarregaLiteraisPtBr()
    {
        var view = Read("backend", "PlantaoPro.Web", "Views", "CentralEscala", "Index.cshtml");

        foreach (var chave in new[]
                 {
                     "Coverage.Title", "Coverage.HeatmapCovered", "Coverage.HeatmapWaiting",
                     "Coverage.HeatmapOpen", "Coverage.HeatmapConflicts", "Shift.VacanciesShort",
                     "Coverage.HeroKicker", "Coverage.HeroTitle", "Coverage.HeroIntro", "Coverage.LiveView",
                     "Shift.New", "Coverage.OpenCalendar", "Coverage.HowToTitle", "Coverage.HowToIntro",
                     "Coverage.HowToSolicitedBody", "Coverage.HowToConfirmedBody", "Coverage.FilterFormAria",
                     "Coverage.From", "Coverage.Until", "Coverage.Situation", "Coverage.AllOptions",
                     "Coverage.SituationDiscovered", "Coverage.SituationCancelled", "Coverage.SearchLabel",
                     "Coverage.ApplyFilters", "Coverage.IndicatorsAria", "Coverage.ShiftsToday",
                     "Coverage.ConfirmedCard", "Coverage.WaitingCard", "Coverage.AtRiskCard",
                     "Coverage.ConflictsCard", "Coverage.ViewModesAria", "Coverage.ExploreBy",
                     "Coverage.ModeCalendar", "Coverage.ModeList", "Coverage.ModeUnit",
                     "Coverage.ModeProfessional", "Coverage.ModeRisk", "Coverage.MapKicker",
                     "Coverage.MapTitle", "Coverage.ViewFullGrid", "Coverage.MapNote", "Coverage.QueueKicker",
                     "Coverage.QueueTitle", "Coverage.OpenShiftsCount", "Coverage.OpenShiftsHint",
                     "Coverage.PendingConfirmationsCount", "Coverage.PendingConfirmationsHint",
                     "Coverage.ConflictsCount", "Coverage.ConflictsHint", "Coverage.TimelineKicker",
                     "Coverage.TimelineTitle", "Coverage.TimelineEmptyTitle", "Coverage.TimelineEmptyBody",
                     "Coverage.QuickKicker", "Coverage.QuickTitle", "Coverage.QuickAvailableProfessionals",
                     "Coverage.QuickAvailableProfessionalsHint", "Coverage.QuickSubstitutions",
                     "Coverage.QuickSubstitutionsHint", "Coverage.QuickRecentAlerts", "Coverage.UnreadCount",
                     "Coverage.QuickFinancialPending", "Coverage.PendingRecordsCount"
                 })
        {
            Assert.Contains($"UiText.L(\"{chave}\")", view);
        }

        foreach (var literal in new[]
                 {
                     "Operação em tempo real", "Central de cobertura", "Visão operacional atual",
                     "Novo plantão</a>", "Abrir calendário</a>", "Como usar esta página",
                     "Filtros da cobertura", "Vaga descoberta", "Atribuída sem confirmação",
                     "Solicitação aberta</option>", "Cobertura concluída</option>", "Cancelada ou expirada",
                     "Unidade, especialidade ou responsável", "Aplicar filtros",
                     "Indicadores de cobertura", "Plantões hoje", "Modos de visualização",
                     "Explorar por", "Mapa de cobertura", "Leitura rápida da operação",
                     "Ver grade completa", "Fila prioritária", "Atenção necessária",
                    "plantão(ões) em aberto</strong>", "Agenda operacional tranquila",
                    "Ações rápidas", "não lido(s)", "registro(s)"
                 })
        {
            Assert.DoesNotContain(literal, view);
        }
    }

    [Fact]
    public void PlantaoEDetalheUsemRecursosENaoCarregamLiteraisPtBr()
    {
        var plantao = Read("backend", "PlantaoPro.Web", "Views", "CentralEscala", "Plantao.cshtml");
        foreach (var chave in new[]
                 {
                     "Shift.DetailsTitle", "Shift.Back", "Shift.Start", "Shift.End",
                     "Shift.Vacancies", "Shift.Value", "Shift.FullDetails", "Shift.ShiftsLink", "Shift.AgendaLink"
                 })
        {
            Assert.Contains($"UiText.L(\"{chave}\")", plantao);
        }
        foreach (var literal in new[] { "Plantão - Central de Escala", ">Voltar</a>", "<small>Início</small>", "<small>Fim</small>", "<small>Vagas</small>", "<small>Valor</small>", "Detalhes completos</a>", "Escalas</a>", "Agenda</a>" })
            Assert.DoesNotContain(literal, plantao);

        var lista = Read("backend", "PlantaoPro.Web", "Views", "Plantoes", "Index.cshtml");
        foreach (var chave in new[]
                 {
                     "Shift.ListTitle", "Shift.ListIntroSubtitle", "Shift.ListIntroNote",
                     "Shift.TipFilter", "Shift.TipRisk", "Shift.TipDetail", "Shift.RefreshFailedTitle",
                     "Shift.TryAgain", "Shift.JourneyAria", "Shift.JourneyStepShift", "Shift.JourneyStepInvite",
                     "Shift.JourneyStepSchedule", "Shift.JourneyStepConfirmation", "Shift.JourneyStepPresence",
                     "Shift.JourneyStepClose", "Shift.JourneyStepFinancial", "Shift.KpiStripAria",
                     "Shift.KpiResults", "Shift.KpiResultsHint", "Shift.KpiNext24h", "Shift.KpiEstimatedValue",
                     "Shift.ThisPage", "Shift.FilterFormAria", "Shift.FilterKicker", "Shift.FilterHeading",
                     "Shift.ClearAll", "Shift.City", "Shift.CityPlaceholder", "Shift.StatusFilter",
                     "Shift.AllOptions", "Shift.StatusDraft", "Shift.StatusPublished", "Shift.StatusDone",
                     "Shift.StatusCancelled", "Shift.FromLabel", "Shift.UntilLabel", "Shift.AllCoverage",
                     "Shift.WithVacancies", "Shift.MinValue", "Shift.MaxValue", "Shift.SortBy",
                     "Shift.SortDate", "Shift.SortValue", "Shift.SortHospital", "Shift.SortSpecialty",
                     "Shift.SortVacancies", "Shift.SortDirectionAria", "Shift.SortDesc", "Shift.SortAsc",
                     "Shift.WorkspaceKicker", "Shift.ViewbarTitle", "Shift.ViewbarAria", "Shift.ViewKanban",
                     "Shift.ThHospital", "Shift.ThSpecialty", "Shift.ThPeriod", "Shift.ThCoverage",
                     "Shift.ThActions", "Shift.Hours", "Shift.FilledSlots", "Shift.CoverageComplete",
                     "Shift.CoveragePending", "Shift.NoCoverageRisk", "Shift.NoCoverage", "Shift.NextAction",
                     "Shift.SeekCoverage", "Shift.ReviewOperation", "Shift.NoObservations",
                     "Shift.ViewDetails", "Shift.ViewInvites", "Shift.OpenOperation", "Shift.MobileCardsAria",
                     "Shift.Risk", "Shift.NoDerivableRisk", "Shift.ViewInvitesAria", "Shift.KanbanAria",
                     "Shift.LaneCritical", "Shift.LaneOpen", "Shift.LaneCovered", "Shift.LaneEmpty",
                     "Shift.CoverageProgressAria", "Shift.EmptyTitle", "Shift.EmptyBody"
                 })
        {
            Assert.Contains($"UiText.L(\"{chave}\")", lista);
        }

        foreach (var literal in new[]
                 {
                     "Central de Plantões", "Planeje, publique e recupere coberturas",
                     "Jornada operacional MVP", "Presença ou substituição", "Fechamento</span>",
                     "Resumo dos resultados exibidos", "em todos os resultados filtrados",
                     "Valor previsto", "nesta página", "Filtros operacionais de plantões",
                     "Filtros reais", "Recorte operacional", "Limpar tudo", "Ex.: Recife",
                     "Rascunho", "Publicado</option>", "Realizado</option>", "A partir de",
                     "Com vagas", "Valor mínimo", "Valor máximo", "Decrescente", "Crescente",
                     "Workspace operacional", "Escolha como acompanhar a cobertura",
                     "Visualização dos plantões", "Plantões em cartões", "Sem risco derivável",
                     "Cobertura crítica", "Em cobertura", "Cobertos", "Nenhum plantão nesta etapa.",
                     "Cobertura do plantão", "Nenhum plantão neste recorte",
                     "Não foi possível atualizar a operação", "Tentar novamente"
                 })
        {
            Assert.DoesNotContain(literal, lista);
        }
    }

    [Fact]
    public void RecursosContemAsChavesDaJornadaEmTodosOsQuatroIdiomas()
    {
        foreach (var arquivo in new[] { "UiTexts.resx", "UiTexts.en.resx", "UiTexts.es.resx", "UiTexts.fr.resx" })
        {
            var resx = Read("backend", "PlantaoPro.Web", "Localization", arquivo);
            foreach (var chave in new[]
                     {
                         "Coverage.LoadFailed", "Coverage.DecisionRecorded", "Coverage.InviteMessage",
                         "Coverage.HeroTitle", "Coverage.SituationDiscovered", "Coverage.OpenShiftsCount",
                         "Coverage.QuickRecentAlerts", "Shift.NotFound", "Shift.DetailsTitle",
                         "Shift.FullDetails", "Shift.ListTitle", "Shift.JourneyStepPresence",
                         "Shift.KpiNext24h", "Shift.FilterHeading", "Shift.LaneCritical",
                         "Shift.LaneEmpty", "Shift.EmptyBody", "Shift.NoDerivableRisk", "Shift.ViewInvitesAria"
                     })
            {
                Assert.Contains($"<data name=\"{chave}\"", resx, StringComparison.Ordinal);
            }
        }
    }
}
