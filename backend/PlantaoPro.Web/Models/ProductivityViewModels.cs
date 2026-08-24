using System.Text.Json.Serialization;

namespace PlantaoPro.Web.Models;

public sealed class ProductivityPageViewModel
{
    public IReadOnlyList<ProductivityItemViewModel> Items { get; set; } = Array.Empty<ProductivityItemViewModel>();
    public ProductivitySummaryViewModel Summary { get; set; } = new();
    public IReadOnlyList<ProductivityQuickActionViewModel> QuickActions { get; set; } = Array.Empty<ProductivityQuickActionViewModel>();
    public IReadOnlyList<ProductivityAgendaItemViewModel> Agenda { get; set; } = Array.Empty<ProductivityAgendaItemViewModel>();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int Total { get; set; }
    public bool CanViewTeam { get; set; }
    public string? Error { get; set; }
}

public sealed class ProductivityItemViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ActionCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "NORMAL";
    public string Status { get; set; } = "ATIVA";
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Icon { get; set; } = "bi-list-check";
    public string ContextLabel { get; set; } = string.Empty;
    public ProductivityActionViewModel? PrimaryAction { get; set; }
    public bool CanSnooze { get; set; }
    public bool CanDismiss { get; set; }
}

public sealed class ProductivityActionViewModel
{
    public string Label { get; set; } = "Abrir";
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = "Index";
    public IReadOnlyDictionary<string, string> RouteValues { get; set; } = new Dictionary<string, string>();
}

public sealed class ProductivitySummaryViewModel
{
    public int Active { get; set; }
    public int Critical { get; set; }
    public int Today { get; set; }
    public int Overdue { get; set; }
    public int Snoozed { get; set; }
    public int CompletedToday { get; set; }
}

public sealed class ProductivityQuickActionViewModel
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-arrow-right";
    public string Controller { get; set; } = "Home";
    public string Action { get; set; } = "Index";
    public IReadOnlyDictionary<string, string> RouteValues { get; set; } = new Dictionary<string, string>();
}

public sealed class ProductivityAgendaItemViewModel
{
    public string Title { get; set; } = string.Empty;
    public string ContextLabel { get; set; } = string.Empty;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public string Section { get; set; } = "PROXIMOS";
}

public sealed class ProductivityQueryViewModel
{
    public string Tab { get; set; } = "para-mim";
    public string? Priority { get; set; }
    public string? Module { get; set; }
    public string? Status { get; set; }
    public string? Due { get; set; }
    public string? UnitId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public sealed record SnoozeProductivityRequest(DateTimeOffset SnoozedUntil);
