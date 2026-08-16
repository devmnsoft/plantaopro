namespace PlantaoPro.Web.Models;

public sealed record FaturamentoClinicoViewModel(
    IReadOnlyList<FaturamentoClinicoItemViewModel> Items,
    string? Error,
    string? Status = null,
    string? CompetenciaFiltro = null,
    string? Convenio = null,
    bool HasActiveFilters = false);

public sealed class FaturamentoClinicoItemViewModel
{
    public Guid Id { get; init; }
    public Guid? ReferenciaId { get; init; }
    public Guid? AtendimentoId { get; init; }
    public decimal? Valor { get; init; }
    public string? Status { get; init; }
    public string? Convenio { get; init; }
    public DateTime? EmitidaEm { get; init; }

    public Guid? OrigemId => AtendimentoId ?? ReferenciaId;

    public string Competencia => EmitidaEm?.ToString("MM/yyyy") ?? "Não informada pela API";
}
