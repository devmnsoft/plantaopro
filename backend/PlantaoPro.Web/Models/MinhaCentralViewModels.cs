using System.Text.Json.Serialization;

namespace PlantaoPro.Web.Models;
public sealed class CentralSummaryViewModel { public int Open { get; set; } public int Overdue { get; set; } public int Critical { get; set; } public int Waiting { get; set; } public int CompletedToday { get; set; } }
public sealed class WorkItemViewModel
{
    public Guid Id { get; set; }
    [JsonPropertyName("tipo")] public string Type { get; set; } = string.Empty;
    [JsonPropertyName("titulo")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("descricao")] public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    [JsonPropertyName("prioridade")] public string Priority { get; set; } = string.Empty;
    [JsonPropertyName("responsavelId")] public Guid? ResponsibleId { get; set; }
    [JsonPropertyName("posicao")] public int Position { get; set; }
    [JsonPropertyName("versao")] public int Version { get; set; }
    [JsonPropertyName("venceEm")] public DateTimeOffset? DueAt { get; set; }
    [JsonPropertyName("criadoEm")] public DateTimeOffset CreatedAt { get; set; }
    [JsonPropertyName("atualizadoEm")] public DateTimeOffset UpdatedAt { get; set; }
}
public sealed class MinhaCentralViewModel { public CentralSummaryViewModel Summary { get; set; }=new(); public IReadOnlyList<WorkItemViewModel> Items { get; set; }=Array.Empty<WorkItemViewModel>(); public string Error { get; set; }=string.Empty; }
public sealed record BreadcrumbViewModel(string Label,string? Url);
public sealed record WorkspaceActionViewModel(string Label,string Url);
public sealed class WorkspaceHeaderViewModel { public IReadOnlyList<BreadcrumbViewModel> Breadcrumbs { get; init; }=Array.Empty<BreadcrumbViewModel>(); public string Title { get; init; }=string.Empty; public string Description { get; init; }=string.Empty; public string? Status { get; init; } public string? Context { get; init; } public WorkspaceActionViewModel? PrimaryAction { get; init; } public IReadOnlyList<WorkspaceActionViewModel> SecondaryActions { get; init; }=Array.Empty<WorkspaceActionViewModel>(); public string? HelpContext { get; init; } }
