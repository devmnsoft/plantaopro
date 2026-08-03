using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace PlantaoPro.Api.Realtime;

[Authorize]
public abstract class TenantIsolatedHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenant = Context.User?.FindFirstValue("tenant_id") ?? Context.User?.FindFirstValue("cliente_id");
        if (!Guid.TryParse(tenant, out var tenantId)) { Context.Abort(); return; }
        await Groups.AddToGroupAsync(Context.ConnectionId, "tenant:" + tenantId.ToString("N"));
        var unit = Context.User?.FindFirstValue("unidade_id");
        if (Guid.TryParse(unit, out var unitId)) await Groups.AddToGroupAsync(Context.ConnectionId, "unidade:" + unitId.ToString("N"));
        var user = Context.User?.FindFirstValue("uid") ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(user, out var userId)) await Groups.AddToGroupAsync(Context.ConnectionId, "usuario:" + userId.ToString("N"));
        foreach (var role in Context.User?.FindAll(ClaimTypes.Role) ?? Array.Empty<Claim>())
            await Groups.AddToGroupAsync(Context.ConnectionId, "perfil:" + role.Value.Trim().ToUpperInvariant());
        await base.OnConnectedAsync();
    }
}
public sealed class OperacaoHub : TenantIsolatedHub { }
public sealed class FilaHub : TenantIsolatedHub { }
public sealed class NotificacoesHub : TenantIsolatedHub { }
public sealed class EscalasHub : TenantIsolatedHub { }
