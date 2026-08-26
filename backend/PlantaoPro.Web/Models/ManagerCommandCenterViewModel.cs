namespace PlantaoPro.Web.Models;
public sealed record ManagerSummary(long Today,long Uncovered,long PendingConfirmation,long Critical,long AvailableProfessionals,long PendingCheckIns,long OpenIncidents,long PendingReplacements,long FinancialPending,long CriticalNotifications);
public sealed record ManagerCoverage(Guid Id,string Unit,string Specialty,DateTime StartsAt,DateTime EndsAt,string Status,int OpenSlots,int Risk,string RiskLabel);
public sealed record ManagerCommandCenterViewModel(ManagerSummary Summary,IReadOnlyList<ManagerCoverage> Coverage,DateTime GeneratedAt,string? Error=null)
{ public static ManagerCommandCenterViewModel Empty(string? error=null)=>new(new(0,0,0,0,0,0,0,0,0,0),Array.Empty<ManagerCoverage>(),DateTime.UtcNow,error); }
