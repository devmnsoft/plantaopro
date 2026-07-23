namespace PlantaoPro.Api;
public sealed record ImpersonationSessionDto(Guid ImpersonationSessionId, Guid OriginalUserId, Guid ImpersonatedUserId, Guid TenantId, Guid? ClienteId, DateTime ImpersonationExpiresAt, string Status, string Token);
public sealed record ImpersonationAtualDto(bool Impersonation, string? OriginalUserId, string? ImpersonatedUserId, string? ImpersonationSessionId, string? ImpersonationExpiresAt);
