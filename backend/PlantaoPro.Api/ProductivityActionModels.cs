namespace PlantaoPro.Api.Productivity;

public static class ProductivityPriority
{
    public const string Critica = "CRITICA";
    public const string Alta = "ALTA";
    public const string Normal = "NORMAL";
    public const string Baixa = "BAIXA";
}

public sealed record ProductivityActionDto(
    string Key, string Module, string EntityType, Guid EntityId, string ActionCode,
    string Title, string Description, string Priority, string Status,
    DateTimeOffset? DueAt, DateTimeOffset CreatedAt, string OwnerType, Guid? OwnerId,
    string Icon, string ContextLabel, string PrimaryAction, bool CanSnooze,
    bool CanDismiss, DateTimeOffset SourceUpdatedAt, bool IsSnoozed);

public sealed record ProductivityQuery(
    string? Tab = null, string? Priority = null, string? Module = null,
    string? Status = null, Guid? OwnerId = null, Guid? UnitId = null,
    DateTimeOffset? DueFrom = null, DateTimeOffset? DueTo = null,
    int Page = 1, int PageSize = 25);

public sealed record ProductivityPageDto(
    IReadOnlyList<ProductivityActionDto> Items, int Page, int PageSize, int Total, int TotalPages);

public sealed record ProductivitySummaryDto(int Active, int Critical, int Today, int Overdue, int Snoozed);
public sealed record SnoozeProductivityRequest(DateTimeOffset SnoozedUntil);
public sealed record QuickActionDto(string Code, string Label, string Icon, string Controller, string Action,
    IReadOnlyDictionary<string, string>? RouteValues = null);

