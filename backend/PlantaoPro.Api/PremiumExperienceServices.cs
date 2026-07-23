using System.Security.Claims;
using PlantaoPro.CrossCutting.Security;
using PlantaoPro.Api.Controllers;

namespace PlantaoPro.Api;

public interface IContextoRepository { }
public sealed class ContextoRepository : IContextoRepository { }
public sealed record ContextoAtualDto(Guid? UsuarioId, Guid? TenantId, Guid? ClienteId, string ContextMode, string AccessScope, string? PrimaryRole, string? TenantContextId);
public sealed record TenantDisponivelDto(Guid TenantId, Guid ClienteId, string Cliente, string Tenant, string Plano, bool Ativo);
public sealed record ContextoTrocaDto(Guid Id, Guid? TenantId, string Evento, DateTime TimestampUtc);
public interface IContextoService
{
    ContextoAtualDto Atual(ClaimsPrincipal user);
    IEnumerable<TenantDisponivelDto> TenantsDisponiveis(ClaimsPrincipal user);
    IEnumerable<ContextoTrocaDto> Recentes(ClaimsPrincipal user);
    object Selecionar(ClaimsPrincipal user, SelecionarContextoRequest request);
    object RetornarGlobal(ClaimsPrincipal user);
    IEnumerable<ContextoTrocaDto> Historico(ClaimsPrincipal user);
}
public sealed class ContextTokenService { public string ModeTenant => "TENANT"; public string ModeGlobal => "GLOBAL"; }
public sealed class ContextAuthorizationService { public bool CanSwitch(ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true; }
public sealed class ContextoService : IContextoService
{
    public ContextoAtualDto Atual(ClaimsPrincipal user) => new(ParseGuid(user.FindFirstValue("uid") ?? user.FindFirstValue(ClaimTypes.NameIdentifier)), ParseGuid(user.FindFirstValue("tenant_id")), ParseGuid(user.FindFirstValue("cliente_id")), user.FindFirstValue("context_mode") ?? "GLOBAL", user.FindFirstValue("access_scope") ?? AccessScopes.Global, user.FindFirstValue("primary_role") ?? user.FindFirstValue(ClaimTypes.Role), user.FindFirstValue("tenant_context_id"));
    public IEnumerable<TenantDisponivelDto> TenantsDisponiveis(ClaimsPrincipal user) => Array.Empty<TenantDisponivelDto>();
    public IEnumerable<ContextoTrocaDto> Recentes(ClaimsPrincipal user) => Array.Empty<ContextoTrocaDto>();
    public object Selecionar(ClaimsPrincipal user, SelecionarContextoRequest request) => new { request.TenantId, contextMode = "TENANT", accessScope = AccessScopes.Hybrid, tenantContextId = Guid.NewGuid(), sessionId = Guid.NewGuid(), originalUserId = user.FindFirstValue("uid") ?? user.FindFirstValue(ClaimTypes.NameIdentifier) };
    public object RetornarGlobal(ClaimsPrincipal user) => new { contextMode = "GLOBAL", accessScope = AccessScopes.Global, tenantId = (Guid?)null, clienteId = (Guid?)null };
    public IEnumerable<ContextoTrocaDto> Historico(ClaimsPrincipal user) => Array.Empty<ContextoTrocaDto>();
    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var id) ? id : null;
}

public interface IImpersonationRepository { }
public sealed class ImpersonationRepository : IImpersonationRepository { }
public sealed class ImpersonationTokenService { public DateTime ExpiresAt(int? minutes) => DateTime.UtcNow.AddMinutes(Math.Clamp(minutes ?? 30, 1, 30)); }
public sealed class ImpersonationAuthorizationService { public bool CanStart(ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true && user.FindFirstValue("impersonation") != "true"; }
public interface IImpersonationService
{
    object Iniciar(ClaimsPrincipal user, IniciarImpersonacaoRequest request);
    object Encerrar(ClaimsPrincipal user, EncerrarImpersonacaoRequest request);
    object Atual(ClaimsPrincipal user);
    IEnumerable<object> Historico(ClaimsPrincipal user);
}
public sealed class ImpersonationService : IImpersonationService
{
    private readonly ImpersonationTokenService _tokens = new();
    public object Iniciar(ClaimsPrincipal user, IniciarImpersonacaoRequest request) => new { impersonation = true, originalUserId = user.FindFirstValue("uid") ?? user.FindFirstValue(ClaimTypes.NameIdentifier), impersonatedUserId = request.UsuarioAlvoId, request.TenantId, impersonationSessionId = Guid.NewGuid(), impersonationExpiresAt = _tokens.ExpiresAt(request.DuracaoMinutos), ticket = request.TicketReferencia };
    public object Encerrar(ClaimsPrincipal user, EncerrarImpersonacaoRequest request) => new { request.ImpersonacaoSessaoId, status = "ENCERRADA", encerradaEm = DateTime.UtcNow };
    public object Atual(ClaimsPrincipal user) => new { impersonation = user.FindFirstValue("impersonation") == "true", originalUserId = user.FindFirstValue("original_user_id"), impersonatedUserId = user.FindFirstValue("impersonated_user_id"), impersonationSessionId = user.FindFirstValue("impersonation_session_id"), impersonationExpiresAt = user.FindFirstValue("impersonation_expires_at") };
    public IEnumerable<object> Historico(ClaimsPrincipal user) => Array.Empty<object>();
}
