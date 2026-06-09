namespace PlantaoPro.Web.Models;

public sealed class KpiCardViewModel
{
    public KpiCardViewModel() { }

    public KpiCardViewModel(string? title, string? value, string? subtitle = null, string? icon = null, string? variation = null, string? semanticColor = null)
    {
        Title = title;
        Value = value;
        Subtitle = subtitle;
        Icon = icon;
        Variation = variation;
        SemanticColor = semanticColor;
    }

    public string? Title { get; set; }
    public string? Value { get; set; }
    public string? Subtitle { get; set; }
    public string? Icon { get; set; }
    public string? Variation { get; set; }
    public string? SemanticColor { get; set; }
}

public sealed class ModuleBannerViewModel
{
    public ModuleBannerViewModel() { }

    public ModuleBannerViewModel(string? title, string? description, string? icon = null, string? actionText = null, string? actionController = null, string? actionName = null, bool actionDisabled = false)
    {
        Title = title;
        Description = description;
        Icon = icon;
        ActionText = actionText;
        ActionController = actionController;
        ActionName = actionName;
        ActionDisabled = actionDisabled;
    }

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
    public ActionToolbarViewModel() { }

    public ActionToolbarViewModel(string? newText, string? newController, string? newAction, bool newDisabled = false, string? downloadFormId = null)
    {
        NewText = newText;
        NewController = newController;
        NewAction = newAction;
        NewDisabled = newDisabled;
        DownloadFormId = downloadFormId;
    }

    public string? NewText { get; set; }
    public string? NewController { get; set; }
    public string? NewAction { get; set; }
    public bool NewDisabled { get; set; }
    public string? DownloadFormId { get; set; }
}

public sealed class ConfirmModalViewModel
{
    public ConfirmModalViewModel() { }

    public ConfirmModalViewModel(string? id = null, string? title = null, string? message = null, string? confirmText = null, string? cancelText = null, string? type = null)
    {
        Id = string.IsNullOrWhiteSpace(id) ? Id : id;
        Title = string.IsNullOrWhiteSpace(title) ? Title : title;
        Message = string.IsNullOrWhiteSpace(message) ? Message : message;
        ConfirmText = string.IsNullOrWhiteSpace(confirmText) ? ConfirmText : confirmText;
        CancelText = string.IsNullOrWhiteSpace(cancelText) ? CancelText : cancelText;
        Type = string.IsNullOrWhiteSpace(type) ? Type : type;
    }

    public string Id { get; set; } = "pp-confirm-modal";
    public string Title { get; set; } = "Confirmar ação";
    public string Message { get; set; } = "Deseja realmente continuar?";
    public string ConfirmText { get; set; } = "Confirmar";
    public string CancelText { get; set; } = "Cancelar";
    public string Type { get; set; } = "warning";
}

public sealed class FilterPanelViewModel
{
    public FilterPanelViewModel() { }

    public FilterPanelViewModel(string? description, string? formId = null)
    {
        Description = description;
        FormId = formId;
    }

    public string? Description { get; set; }
    public string? FormId { get; set; }
}

public sealed class PermissionMatrixViewModel
{
    public PermissionMatrixViewModel()
    {
        Perfis = new List<string>();
        Modulos = new List<PermissionModuleViewModel>();
    }

    public IList<string> Perfis { get; set; }
    public IList<PermissionModuleViewModel> Modulos { get; set; }
    public string PerfilTeste { get; set; } = string.Empty;
    public string ModuloTeste { get; set; } = string.Empty;
    public string AcaoTeste { get; set; } = string.Empty;
    public string ResultadoTeste { get; set; } = string.Empty;
}

public sealed class PermissionModuleViewModel
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public IList<string> Acoes { get; set; } = new List<string>();
    public Dictionary<string, IList<string>> PermissoesPorPerfil { get; set; } = new Dictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase);
}
