namespace PlantaoPro.Api.Operation360.WorkItems;

public interface IWorkItemRepository
{
    Task<IReadOnlyList<WorkItemDto>> ListAsync(Guid tenantId, Guid? unitId, CancellationToken cancellationToken);
    Task<WorkItemDto?> GetAsync(Guid tenantId, Guid? unitId, Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkItemHistoryDto>> HistoryAsync(Guid tenantId, Guid? unitId, Guid id, CancellationToken cancellationToken);
    Task<WorkItemMutationResult> MoveAsync(Guid tenantId, Guid? unitId, Guid userId, WorkItemMoveRequest request, CancellationToken cancellationToken);
    Task<WorkItemMutationResult> AssignAsync(Guid tenantId, Guid? unitId, Guid userId, Guid id, Guid responsibleId, WorkItemVersionRequest request, CancellationToken cancellationToken);
    Task<WorkItemMutationResult> CommentAsync(Guid tenantId, Guid? unitId, Guid userId, Guid id, WorkItemCommentRequest request, CancellationToken cancellationToken);
    Task<WorkItemMutationResult> PostponeAsync(Guid tenantId, Guid? unitId, Guid userId, Guid id, WorkItemPostponeRequest request, CancellationToken cancellationToken);
}
