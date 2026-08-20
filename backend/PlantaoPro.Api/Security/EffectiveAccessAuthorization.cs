using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace PlantaoPro.Api.Security;

public sealed class EffectiveAccessRequirement : IAuthorizationRequirement
{
    public EffectiveAccessRequirement(string policy,string? permission=null){Policy=policy;Permission=permission;}
    public string Policy { get; }
    public string? Permission { get; }
}

public sealed class EffectiveAccessAuthorizationHandler : AuthorizationHandler<EffectiveAccessRequirement>
{
    private readonly IEffectivePermissionService permissions;
    public EffectiveAccessAuthorizationHandler(IEffectivePermissionService permissions)=>this.permissions=permissions;
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,EffectiveAccessRequirement requirement)
    {
        if(context.User.Identity?.IsAuthenticated!=true)return;
        var userId=GuidClaim(context.User,"uid")??GuidClaim(context.User,ClaimTypes.NameIdentifier);if(!userId.HasValue)return;
        var tenantId=GuidClaim(context.User,"tenant_id");
        var global=context.User.IsInRole(RolesConstants.AdministradorGlobal);
        if(requirement.Policy=="GlobalAccess"&&!global)return;
        if((requirement.Policy=="TenantAccess"||requirement.Policy=="TenantContextRequired")&&!tenantId.HasValue)return;
        if(requirement.Policy=="HybridAccess"&&!global&&!tenantId.HasValue)return;
        if(requirement.Policy=="TenantContextOptional"){context.Succeed(requirement);return;}
        var permission=requirement.Permission;
        if(permission is null){context.Succeed(requirement);return;}
        var parts=permission.Split(':',2);var result=await permissions.TestarAsync(userId.Value,tenantId,parts[0],parts.Length==2?parts[1]:"ACESSAR");
        if(result.Permitido)context.Succeed(requirement);
    }
    private static Guid? GuidClaim(ClaimsPrincipal user,string type)=>Guid.TryParse(user.FindFirstValue(type),out var id)?id:null;
}
