namespace PlantaoPro.Web.Models;

public sealed class KpiCardViewModel
{
    public string? Title { get; set; }
    public string? Value { get; set; }
    public string? Subtitle { get; set; }
    public string? Icon { get; set; }
    public string? Variation { get; set; }
    public string? SemanticColor { get; set; }
}

public sealed class ModuleBannerViewModel
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? ActionText { get; set; }
    public string? ActionController { get; set; }
    public string? ActionName { get; set; }
    public bool ActionDisabled { get; set; }
}

public sealed class ActionToolbarViewModel
{
    public string? NewText { get; set; }
    public string? NewController { get; set; }
    public string? NewAction { get; set; }
    public bool NewDisabled { get; set; }
    public string? ExportFormId { get; set; }
}

public sealed class ConfirmModalViewModel
{
    public string Id { get; set; } = "pp-confirm-modal";
    public string Title { get; set; } = "Confirmar ação";
    public string Message { get; set; } = "Deseja realmente continuar?";
    public string ConfirmText { get; set; } = "Confirmar";
    public string CancelText { get; set; } = "Cancelar";
    public string Type { get; set; } = "warning";
}

public sealed class FilterPanelViewModel
{
    public string? Description { get; set; }
    public string? FormId { get; set; }
}
