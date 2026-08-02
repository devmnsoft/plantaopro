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
        if (Guid.TryParse(unit, out var unitId)) await Groups.AddToGroupAsync(Context.ConnectionId, "tenant:" + tenantId.ToString("N") + ":unit:" + unitId.ToString("N"));
        await base.OnConnectedAsync();
    }
}
public sealed class OperacaoHub : TenantIsolatedHub { }
public sealed class FilaHub : TenantIsolatedHub { }
public sealed class NotificacoesHub : TenantIsolatedHub { }
public sealed class EscalasHub : TenantIsolatedHub { }
