namespace PlantaoPro.Web.Models;
public sealed record PlatformHealthSummary(long ActiveClients, long ActiveTenants, long ContractedModules);
public sealed record ManagerSummary(long Today,long Uncovered,long PendingConfirmation,long Critical,long AvailableProfessionals,long PendingCheckIns,long OpenIncidents,long PendingReplacements,long FinancialPending,long CriticalNotifications,PlatformHealthSummary? PlatformHealth=null)
{
    /// <summary>R8-B23 (§27): definições dos indicadores operacionais (espelhadas da API; a Web não referencia o projeto Api).</summary>
    public IReadOnlyList<KpiDefinicaoDto>? Definicoes { get; init; }
    /// <summary>R8-B23 (§27): critério de tempo/escopo aplicado aos indicadores operacionais (fuso do servidor).</summary>
    public string TimeCriterion { get; init; } = "";
}
public sealed record ManagerCoverage(Guid Id,string Unit,string Specialty,DateTime StartsAt,DateTime EndsAt,string Status,int OpenSlots,int Risk,string RiskLabel);
public sealed record ManagerCommandCenterViewModel(ManagerSummary Summary,IReadOnlyList<ManagerCoverage> Coverage,DateTime GeneratedAt,string? Error=null)
{ public static ManagerCommandCenterViewModel Empty(string? error=null)=>new(new(0,0,0,0,0,0,0,0,0,0),Array.Empty<ManagerCoverage>(),DateTime.UtcNow,error); }
