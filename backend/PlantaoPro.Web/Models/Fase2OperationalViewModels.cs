namespace PlantaoPro.Web.Models;

public sealed class Fase2KpiViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Hint { get; set; } = string.Empty;
    public string Status { get; set; } = "OK";
    public string Icon { get; set; } = "bi-activity";
}

public sealed class Fase2ActionViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public bool RequiresConfirmation { get; set; }
    public bool Blocked { get; set; }
    public string BlockReason { get; set; } = string.Empty;
}

public sealed class Fase2WorkItemViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Due { get; set; } = string.Empty;
    public IList<Fase2ActionViewModel> Actions { get; set; } = new List<Fase2ActionViewModel>();
}

public sealed class Fase2FlowStepViewModel
{
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class Fase2OperationalPageViewModel
{
    public string Area { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string CurrentSection { get; set; } = string.Empty;
    public string TenantScope { get; set; } = string.Empty;
    public string Persona { get; set; } = string.Empty;
    public bool HasPlanBlock { get; set; }
    public string PlanBlockMessage { get; set; } = string.Empty;
    public IList<Fase2KpiViewModel> Kpis { get; set; } = new List<Fase2KpiViewModel>();
    public IList<Fase2FlowStepViewModel> Steps { get; set; } = new List<Fase2FlowStepViewModel>();
    public IList<Fase2WorkItemViewModel> WorkItems { get; set; } = new List<Fase2WorkItemViewModel>();
    public IList<Fase2ActionViewModel> PrimaryActions { get; set; } = new List<Fase2ActionViewModel>();
    public IList<string> Alerts { get; set; } = new List<string>();
}
