namespace PlantaoPro.Web.Models;

public sealed record DashboardViewModel(string Perfil, IReadOnlyList<KpiItemViewModel> Indicadores, IReadOnlyList<OperationalWorkItemViewModel> Pendencias, IReadOnlyList<TimelineEventViewModel> Timeline);
public sealed record CoverageDashboardViewModel(IReadOnlyList<KpiItemViewModel> Indicadores, IReadOnlyList<OperationalWorkItemViewModel> PlantoesCriticos, IReadOnlyList<TimelineEventViewModel> Timeline);
public sealed record EscalaBoardViewModel(IReadOnlyList<KpiItemViewModel> Indicadores, IReadOnlyList<OperationalWorkItemViewModel> Escalas, IReadOnlyList<TimelineEventViewModel> Timeline);
public sealed record FechamentoViewModel(IReadOnlyList<KpiItemViewModel> Indicadores, IReadOnlyList<OperationalWorkItemViewModel> Pendentes, IReadOnlyList<TimelineEventViewModel> Timeline);
public sealed record FinanceiroDashboardViewModel(IReadOnlyList<KpiItemViewModel> Indicadores, IReadOnlyList<OperationalWorkItemViewModel> Pagamentos, IReadOnlyList<TimelineEventViewModel> Timeline);
public sealed record MinhaCentralCockpitViewModel(IReadOnlyList<KpiItemViewModel> Indicadores, IReadOnlyList<OperationalWorkItemViewModel> Itens, IReadOnlyList<TimelineEventViewModel> Timeline);

public sealed record KpiItemViewModel(string Label, string Value, string Hint, string Tone = "neutral");
public sealed record OperationalWorkItemViewModel(string Id, string Title, string Description, string Status, string Priority, string Origin, string NextAction, string Url);
public sealed record TimelineEventViewModel(string Title, string Description, DateTimeOffset OccurredAt, string Tone = "neutral");

public sealed record BffDashboardResponse(string Perfil, IEnumerable<KpiItemViewModel> Indicadores, IEnumerable<OperationalWorkItemViewModel> Pendencias, IEnumerable<TimelineEventViewModel> Timeline, string EmptyStateMessage);
