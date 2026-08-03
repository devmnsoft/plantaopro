using Microsoft.AspNetCore.SignalR;
using PlantaoPro.Api.Realtime;

namespace PlantaoPro.Api.Operation360.Realtime;

public interface IOperationRealtimePublisher
{
    Task PublishWorkItemAsync(Guid tenantId, Guid? unitId, string eventName, object payload, CancellationToken cancellationToken);
    Task PublishNotificationAsync(Guid tenantId, Guid userId, string eventName, object payload, CancellationToken cancellationToken);
}

public sealed class OperationRealtimePublisher : IOperationRealtimePublisher
{
    private readonly IHubContext<OperacaoHub> operation;
    private readonly IHubContext<NotificacoesHub> notifications;
    public OperationRealtimePublisher(IHubContext<OperacaoHub> operation, IHubContext<NotificacoesHub> notifications) { this.operation = operation; this.notifications = notifications; }
    public async Task PublishWorkItemAsync(Guid tenantId, Guid? unitId, string eventName, object payload, CancellationToken ct)
    {
        await operation.Clients.Group($"tenant:{tenantId:N}").SendAsync(eventName, payload, ct);
        if (unitId.HasValue) await operation.Clients.Group($"unidade:{unitId:N}").SendAsync(eventName, payload, ct);
    }
    public Task PublishNotificationAsync(Guid tenantId, Guid userId, string eventName, object payload, CancellationToken ct) =>
        notifications.Clients.Groups($"tenant:{tenantId:N}", $"usuario:{userId:N}").SendAsync(eventName, payload, ct);
}
