using PlantaoPro.Api.Operation360.Realtime;
using PlantaoPro.Api.Data;

namespace PlantaoPro.Api.Operation360.WorkItems;

public sealed class WorkItemService : IWorkItemService
{
    private readonly IWorkItemRepository repository; private readonly ICurrentUserService current; private readonly IHttpContextAccessor accessor;
    private readonly IOperationRealtimePublisher realtime; private readonly IAuditService audit;
    public WorkItemService(IWorkItemRepository repository, ICurrentUserService current, IHttpContextAccessor accessor, IOperationRealtimePublisher realtime, IAuditService audit)
    { this.repository = repository; this.current = current; this.accessor = accessor; this.realtime = realtime; this.audit = audit; }
    private Guid Tenant => current.TenantId ?? throw new UnauthorizedAccessException("Contexto do tenant não encontrado.");
    private Guid UserId => current.UserId ?? throw new UnauthorizedAccessException("Usuário não identificado.");
    private Guid? Unit { get { Guid value; return Guid.TryParse(accessor.HttpContext?.User.FindFirst("unidade_id")?.Value, out value) ? value : null; } }
    public Task<IReadOnlyList<WorkItemDto>> ListAsync(CancellationToken ct) => repository.ListAsync(Tenant, Unit, ct);
    public Task<WorkItemDto?> GetAsync(Guid id, CancellationToken ct) => repository.GetAsync(Tenant, Unit, id, ct);
    public Task<IReadOnlyList<WorkItemHistoryDto>> HistoryAsync(Guid id, CancellationToken ct) => repository.HistoryAsync(Tenant, Unit, id, ct);
    public async Task<MinhaCentralDto> CentralAsync(CancellationToken ct) { var items = await ListAsync(ct); var now = DateTimeOffset.UtcNow; return new(new(items.Count(x => x.Status != WorkItemStatus.Concluido), items.Count(x => x.VenceEm < now && x.Status != WorkItemStatus.Concluido), items.Count(x => x.Prioridade == "CRITICA"), items.Count(x => x.Status == WorkItemStatus.Aguardando), items.Count(x => x.Status == WorkItemStatus.Concluido && x.AtualizadoEm.UtcDateTime.Date == now.UtcDateTime.Date)), items); }
    public Task<WorkItemMutationResult> MoveAsync(WorkItemMoveRequest r, CancellationToken ct) { if (!WorkItemStatus.All.Contains(r.Destination) || !WorkItemStatus.All.Contains(r.Source) || r.Position < 0) throw new ArgumentException("Destino, origem ou posição inválidos."); return ApplyAsync("MOVER", repository.MoveAsync(Tenant, Unit, UserId, r, ct), ct); }
    public Task<WorkItemMutationResult> AssignAsync(Guid id, Guid responsibleId, WorkItemVersionRequest r, CancellationToken ct) => ApplyAsync(responsibleId == Guid.Empty ? "ASSUMIR" : "ENCAMINHAR", repository.AssignAsync(Tenant, Unit, UserId, id, responsibleId == Guid.Empty ? UserId : responsibleId, r, ct), ct);
    public Task<WorkItemMutationResult> CommentAsync(Guid id, WorkItemCommentRequest r, CancellationToken ct) { if (string.IsNullOrWhiteSpace(r.Comment) || r.Comment.Trim().Length > 2000) throw new ArgumentException("Informe um comentário de até 2.000 caracteres."); return ApplyAsync("COMENTAR", repository.CommentAsync(Tenant, Unit, UserId, id, r, ct), ct); }
    public Task<WorkItemMutationResult> PostponeAsync(Guid id, WorkItemPostponeRequest r, CancellationToken ct) { if (r.DueAt <= DateTimeOffset.UtcNow) throw new ArgumentException("O novo prazo deve estar no futuro."); return ApplyAsync("ADIAR", repository.PostponeAsync(Tenant, Unit, UserId, id, r, ct), ct); }
    private async Task<WorkItemMutationResult> ApplyAsync(string action, Task<WorkItemMutationResult> operation, CancellationToken ct)
    {
        var result = await operation; if (result.Item != null && !result.Conflict) { await audit.RegistrarAsync(UserId, Tenant, "WORK_ITEM", result.Item.Id, action, new { result.Item.Status, result.Item.Versao }, true, null, current.Roles.FirstOrDefault(), ct); await realtime.PublishWorkItemAsync(Tenant, Unit, result.Item.Status == WorkItemStatus.Concluido ? "WorkItemConcluido" : "WorkItemAtualizado", result.Item, ct); } return result;
    }
}
