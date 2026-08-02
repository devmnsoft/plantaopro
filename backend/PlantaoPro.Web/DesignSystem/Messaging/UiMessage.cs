namespace PlantaoPro.Web.DesignSystem.Messaging;
public enum UiMessageSeverity { Information, Success, Warning, Error }
public static class UiMessageCode
{
    public const string ShiftPublished = "SHIFT_PUBLISHED";
    public const string CheckInCompleted = "CHECKIN_COMPLETED";
    public const string ScheduleConflict = "SCHEDULE_CONFLICT";
    public const string DraftProtected = "DRAFT_PROTECTED";
}
public sealed record UiMessageAction(string Label, string Url, bool Primary = false);
public sealed record UiMessage(string Code, string Title, string Description, UiMessageSeverity Severity, bool Persistent, string? ReferenceCode, IReadOnlyList<UiMessageAction> Actions);
public static class UiMessageCatalog
{
    public static UiMessage ShiftPublished(int vacancies) => new(UiMessageCode.ShiftPublished, "Plantão publicado", $"As {vacancies} vagas já estão disponíveis para convite.", UiMessageSeverity.Success, false, null, Array.Empty<UiMessageAction>());
    public static UiMessage ScheduleConflict(string interval) => new(UiMessageCode.ScheduleConflict, "Horário indisponível", $"O profissional já possui outro atendimento entre {interval}. Escolha um novo horário.", UiMessageSeverity.Warning, true, null, Array.Empty<UiMessageAction>());
}
