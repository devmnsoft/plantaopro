namespace PlantaoPro.Api.Contracts.Notifications;

/// <summary>Preferência canônica de entrega para uma categoria e evento.</summary>
public sealed record NotificationPreferenceDto(
    string Categoria,
    string TipoEvento,
    bool InApp,
    bool Email,
    bool Push,
    bool Whatsapp,
    bool Ativo);

public sealed record NotificationPreferencesRequest(
    IReadOnlyList<NotificationPreferenceDto> Preferences);
