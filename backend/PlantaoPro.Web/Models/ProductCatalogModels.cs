namespace PlantaoPro.Web.Models;

public sealed record FeatureDefinition(
    string Code,
    string Name,
    string Description,
    string Domain,
    string Controller,
    string Action,
    string Endpoint,
    string Profile,
    string Permission,
    string Plan,
    string Status,
    string Journey,
    string CanonicalVersion,
    string LegacyAlias,
    bool IsAvailable);

public sealed record NavigationDefinition(
    string Profile,
    string Group,
    string Label,
    string Icon,
    string FeatureCode,
    int Order);

public sealed record PageDefinition(
    string FeatureCode,
    string Title,
    string Description,
    IReadOnlyList<string> Breadcrumb,
    string JourneyStep,
    string PrimaryActionLabel,
    string SecondaryActionLabel);

public sealed record PageContextViewModel(
    string Title,
    string Description,
    IReadOnlyList<string> Breadcrumb,
    string JourneyStep,
    string PrimaryActionLabel,
    string SecondaryActionLabel,
    string? TenantName,
    string? CurrentRecord);
