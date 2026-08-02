using System.Collections.ObjectModel;

namespace PlantaoPro.Web.DesignSystem;

public static class AppIconRegistry
{
    private static readonly IReadOnlyDictionary<AppIconKey, AppIcon> Icons =
        new ReadOnlyDictionary<AppIconKey, AppIcon>(new Dictionary<AppIconKey, AppIcon>
        {
            [AppIconKey.Unknown] = new(AppIconKey.Unknown, "unknown"),
            [AppIconKey.Dashboard] = new(AppIconKey.Dashboard, "layout-dashboard"), [AppIconKey.Home] = new(AppIconKey.Home, "home"),
            [AppIconKey.Search] = new(AppIconKey.Search, "search"), [AppIconKey.Notification] = new(AppIconKey.Notification, "bell"),
            [AppIconKey.Calendar] = new(AppIconKey.Calendar, "calendar"), [AppIconKey.Shift] = new(AppIconKey.Shift, "calendar-clock"),
            [AppIconKey.Schedule] = new(AppIconKey.Schedule, "calendar-event"), [AppIconKey.Doctor] = new(AppIconKey.Doctor, "stethoscope"),
            [AppIconKey.Patient] = new(AppIconKey.Patient, "user-heart"), [AppIconKey.Hospital] = new(AppIconKey.Hospital, "building-hospital"),
            [AppIconKey.Unit] = new(AppIconKey.Unit, "building"), [AppIconKey.Reception] = new(AppIconKey.Reception, "desk"),
            [AppIconKey.Triage] = new(AppIconKey.Triage, "activity-heartbeat"), [AppIconKey.Consultation] = new(AppIconKey.Consultation, "medical-cross"),
            [AppIconKey.Prescription] = new(AppIconKey.Prescription, "prescription"), [AppIconKey.Finance] = new(AppIconKey.Finance, "wallet"),
            [AppIconKey.Payment] = new(AppIconKey.Payment, "credit-card"), [AppIconKey.Report] = new(AppIconKey.Report, "chart-bar"),
            [AppIconKey.Settings] = new(AppIconKey.Settings, "settings"), [AppIconKey.Security] = new(AppIconKey.Security, "shield-lock"),
            [AppIconKey.Audit] = new(AppIconKey.Audit, "clipboard-check"), [AppIconKey.User] = new(AppIconKey.User, "user"),
            [AppIconKey.Help] = new(AppIconKey.Help, "help-circle"), [AppIconKey.Add] = new(AppIconKey.Add, "plus"),
            [AppIconKey.Edit] = new(AppIconKey.Edit, "pencil"), [AppIconKey.Delete] = new(AppIconKey.Delete, "trash"),
            [AppIconKey.View] = new(AppIconKey.View, "eye"), [AppIconKey.History] = new(AppIconKey.History, "history"),
            [AppIconKey.Filter] = new(AppIconKey.Filter, "filter"), [AppIconKey.Download] = new(AppIconKey.Download, "download"),
            [AppIconKey.Upload] = new(AppIconKey.Upload, "upload"), [AppIconKey.Print] = new(AppIconKey.Print, "printer"),
            [AppIconKey.Check] = new(AppIconKey.Check, "check"), [AppIconKey.Warning] = new(AppIconKey.Warning, "alert-triangle"),
            [AppIconKey.Error] = new(AppIconKey.Error, "circle-x"), [AppIconKey.Information] = new(AppIconKey.Information, "info-circle"),
            [AppIconKey.More] = new(AppIconKey.More, "dots"), [AppIconKey.ChevronDown] = new(AppIconKey.ChevronDown, "chevron-down"),
            [AppIconKey.ChevronRight] = new(AppIconKey.ChevronRight, "chevron-right"), [AppIconKey.Menu] = new(AppIconKey.Menu, "menu"),
            [AppIconKey.Close] = new(AppIconKey.Close, "x"), [AppIconKey.Logout] = new(AppIconKey.Logout, "logout"),
            [AppIconKey.DragHandle] = new(AppIconKey.DragHandle, "grip-vertical"), [AppIconKey.Move] = new(AppIconKey.Move, "arrows-move"),
            [AppIconKey.Waiting] = new(AppIconKey.Waiting, "clock-hour-4"), [AppIconKey.Called] = new(AppIconKey.Called, "speakerphone"),
            [AppIconKey.Emergency] = new(AppIconKey.Emergency, "ambulance"), [AppIconKey.Document] = new(AppIconKey.Document, "file-text"),
            [AppIconKey.Attachment] = new(AppIconKey.Attachment, "paperclip"), [AppIconKey.Comment] = new(AppIconKey.Comment, "message"),
            [AppIconKey.Favorite] = new(AppIconKey.Favorite, "star"), [AppIconKey.Recent] = new(AppIconKey.Recent, "clock"),
            [AppIconKey.Refresh] = new(AppIconKey.Refresh, "refresh"), [AppIconKey.Retry] = new(AppIconKey.Retry, "reload"),
            [AppIconKey.Lock] = new(AppIconKey.Lock, "lock"), [AppIconKey.Unlock] = new(AppIconKey.Unlock, "lock-open"),
            [AppIconKey.Coverage] = new(AppIconKey.Coverage, "users-group"), [AppIconKey.WorkItem] = new(AppIconKey.WorkItem, "list-check")
        });

    public static AppIcon Resolve(AppIconKey key) =>
        Icons.TryGetValue(key, out var icon) && key != AppIconKey.Unknown
            ? icon
            : throw new ArgumentOutOfRangeException(nameof(key), key, "Ícone não registrado para uso em runtime.");

    public static bool IsRegistered(AppIconKey key) => Icons.ContainsKey(key);

}
