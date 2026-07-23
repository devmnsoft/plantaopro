namespace PlantaoPro.Api;

public sealed record ContextoAtualDto(Guid? UsuarioId, Guid? TenantId, Guid? ClienteId, string ContextMode, string AccessScope, string? PrimaryRole, string? TenantContextId);
public sealed record TenantDisponivelDto(Guid TenantId, string TenantNome, Guid ClienteId, string ClienteNome, string? CnpjMascarado, string Status, string Plano, string AssinaturaStatus, string[] ModulosAtivos, DateTime? UltimoAcesso, bool PodeSelecionar, string? MotivoBloqueio)
{
    public string Cliente => ClienteNome;
    public string Tenant => TenantNome;
    public bool Ativo => PodeSelecionar;
}
public sealed record ContextoTrocaDto(Guid Id, Guid? TenantId, string Evento, DateTime TimestampUtc);
public sealed record SelecionarContextoResponse(Guid TenantId, Guid ClienteId, Guid TenantContextId, string ContextMode, string AccessScope, string SessionId, string Token);
public sealed record RetornarContextoGlobalResponse(string ContextMode, string AccessScope, Guid? TenantId, Guid? ClienteId, string Token);
