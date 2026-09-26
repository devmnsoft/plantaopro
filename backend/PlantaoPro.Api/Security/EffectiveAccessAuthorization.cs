using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace PlantaoPro.Api.Security;

public sealed class EffectiveAccessRequirement : IAuthorizationRequirement
{
    public EffectiveAccessRequirement(string policy, string? permission = null)
    {
        Policy = policy;
        Permission = permission;
    }
    public string Policy { get; }
    public string? Permission { get; }
}

public sealed class EffectiveAccessAuthorizationHandler : AuthorizationHandler<EffectiveAccessRequirement>
{
    private readonly IEffectivePermissionService permissions;
    private readonly IHttpContextAccessor httpContextAccessor;

    public EffectiveAccessAuthorizationHandler(IEffectivePermissionService permissions, IHttpContextAccessor httpContextAccessor)
    {
        this.permissions = permissions;
        this.httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, EffectiveAccessRequirement requirement)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (context.User.Identity?.IsAuthenticated != true)
        {
            if (httpContext != null)
                httpContext.Items["EffectiveAccessDiagnostic"] = new EffectivePermissionDiagnostic(false, "UNAUTHENTICATED", "Usuário não autenticado.", "AUTENTICACAO");
            return;
        }

        var userId = GuidClaim(context.User, "uid") ?? GuidClaim(context.User, ClaimTypes.NameIdentifier);
        if (!userId.HasValue)
        {
            if (httpContext != null)
                httpContext.Items["EffectiveAccessDiagnostic"] = new EffectivePermissionDiagnostic(false, "USER_ID_MISSING", "Identificador do usuário ausente.", "TOKEN");
            return;
        }

        var tenantId = GuidClaim(context.User, "tenant_id");
        var global = context.User.IsInRole(RolesConstants.AdministradorGlobal);

        if (requirement.Policy == "GlobalAccess" && !global)
        {
            if (httpContext != null)
                httpContext.Items["EffectiveAccessDiagnostic"] = new EffectivePermissionDiagnostic(false, "GLOBAL_REQUIRED", "Recurso restrito a administradores globais MNSOFT.", "PERFIL_GLOBAL");
            return;
        }

        if ((requirement.Policy == "TenantAccess" || requirement.Policy == "TenantContextRequired") && !tenantId.HasValue)
        {
            if (httpContext != null)
                httpContext.Items["EffectiveAccessDiagnostic"] = new EffectivePermissionDiagnostic(false, "TENANT_CONTEXT_REQUIRED", "Selecione o tenant correto para continuar.", "TENANT");
            return;
        }

        if (requirement.Policy == "HybridAccess" && !global && !tenantId.HasValue)
        {
            if (httpContext != null)
                httpContext.Items["EffectiveAccessDiagnostic"] = new EffectivePermissionDiagnostic(false, "HYBRID_ACCESS_DENIED", "Acesso híbrido requer perfil global ou contexto de tenant selecionado.", "TENANT");
            return;
        }

        if (requirement.Policy == "TenantContextOptional")
        {
            context.Succeed(requirement);
            return;
        }

        var permission = requirement.Permission;
        if (permission is null)
        {
            context.Succeed(requirement);
            return;
        }

        var parts = permission.Split(':', 2);
        var result = await permissions.TestarAsync(userId.Value, tenantId, parts[0], parts.Length == 2 ? parts[1] : "ACESSAR");
        if (httpContext != null)
            httpContext.Items["EffectiveAccessDiagnostic"] = result;

        if (result.Permitido)
            context.Succeed(requirement);
    }

    private static Guid? GuidClaim(ClaimsPrincipal user, string type) => Guid.TryParse(user.FindFirstValue(type), out var id) ? id : null;
}
