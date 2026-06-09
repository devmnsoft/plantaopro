namespace PlantaoPro.Api.Models;

public sealed class Fase2KpiDto
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Hint { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class Fase2FlowDto
{
    public string Area { get; set; } = string.Empty;
    public string TenantScope { get; set; } = string.Empty;
    public IEnumerable<Fase2KpiDto> Kpis { get; set; } = Array.Empty<Fase2KpiDto>();
    public IEnumerable<string> RequiredValidations { get; set; } = Array.Empty<string>();
    public IEnumerable<string> AvailableActions { get; set; } = Array.Empty<string>();
}

public sealed class Fase2ActionRequest
{
    public Guid? TenantId { get; set; }
    public Guid? EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
}
