namespace PlantaoPro.Api.Operation360.WorkItems;
public interface IWorkItemService
{
    Task<MinhaCentralDto> CentralAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkItemDto>> ListAsync(CancellationToken cancellationToken);
    Task<WorkItemDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkItemHistoryDto>> HistoryAsync(Guid id, CancellationToken cancellationToken);
    Task<WorkItemMutationResult> MoveAsync(WorkItemMoveRequest request, CancellationToken cancellationToken);
    Task<WorkItemMutationResult> AssignAsync(Guid id, Guid responsibleId, WorkItemVersionRequest request, CancellationToken cancellationToken);
    Task<WorkItemMutationResult> CommentAsync(Guid id, WorkItemCommentRequest request, CancellationToken cancellationToken);
    Task<WorkItemMutationResult> PostponeAsync(Guid id, WorkItemPostponeRequest request, CancellationToken cancellationToken);
}
