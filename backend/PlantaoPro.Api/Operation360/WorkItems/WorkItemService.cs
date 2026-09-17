using PlantaoPro.Api.Operation360.Realtime;
using PlantaoPro.Api.Data;

namespace PlantaoPro.Api.Operation360.WorkItems;

public sealed class WorkItemService : IWorkItemService
{
    private readonly IWorkItemRepository repository; private readonly ICurrentUserService current; private readonly IHttpContextAccessor accessor;
    private readonly IOperationRealtimePublisher realtime; private readonly IAuditService audit; private readonly IModuleAccessService modules;
    public WorkItemService(IWorkItemRepository repository, ICurrentUserService current, IHttpContextAccessor accessor, IOperationRealtimePublisher realtime, IAuditService audit, IModuleAccessService modules)
    { this.repository = repository; this.current = current; this.accessor = accessor; this.realtime = realtime; this.audit = audit; this.modules = modules; }
    private Guid Tenant => current.TenantId ?? throw new UnauthorizedAccessException("Contexto do tenant não encontrado.");
    private Guid UserId => current.UserId ?? throw new UnauthorizedAccessException("Usuário não identificado.");
    private Guid? Unit { get { Guid value; return Guid.TryParse(accessor.HttpContext?.User.FindFirst("unidade_id")?.Value, out value) ? value : null; } }
    public Task<IReadOnlyList<WorkItemDto>> ListAsync(CancellationToken ct) => repository.ListAsync(Tenant, Unit, ct);
    public Task<WorkItemDto?> GetAsync(Guid id, CancellationToken ct) => repository.GetAsync(Tenant, Unit, id, ct);
    public Task<IReadOnlyList<WorkItemHistoryDto>> HistoryAsync(Guid id, CancellationToken ct) => repository.HistoryAsync(Tenant, Unit, id, ct);
    public async Task<MinhaCentralDto> CentralAsync(CancellationToken ct)
    {
        if (current.IsGlobalAdmin() && !current.TenantId.HasValue)
        {
            var globalContext = new CentralContextDto(Guid.Empty, "Visão global MNSOFT", null, null,
                "Administrador global", true, DateTimeOffset.UtcNow);
            return new MinhaCentralDto(globalContext, new CentralSummaryDto(0, 0, 0, 0, 0),
                Array.Empty<CentralWorkItemDto>(), new[]
                {
                    new CentralShortcutDto("Central Global", "Filtre clientes por módulo e situação.", "/SaasDashboard", "ADMIN_SAAS"),
                    new CentralShortcutDto("Clientes", "Selecione explicitamente o cliente antes de entrar na operação.", "/Clientes", "ADMIN_SAAS")
                });
        }
        var persisted = await ListAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var items = persisted
            .Where(x => x.Status is not (WorkItemStatus.Concluido or WorkItemStatus.Cancelado))
            .GroupBy(x => $"{x.Tipo.Trim().ToUpperInvariant()}:{x.Id}", StringComparer.Ordinal)
            .Select(group => ToCentralItem(group.OrderByDescending(x => x.AtualizadoEm).First()))
            .OrderBy(x => x.DueAt.HasValue ? 0 : 1).ThenBy(x => x.DueAt).ThenByDescending(x => x.UpdatedAt).ThenBy(x => x.StableKey)
            .Take(100).ToArray();
        var context = new CentralContextDto(Tenant, Claim("tenant") ?? Claim("cliente") ?? "Organização selecionada", Unit,
            Claim("unidade"), ResolveProfile(), false, now);
        var summary = new CentralSummaryDto(items.Length, items.Count(x => x.DueAt < now),
            items.Count(x => x.Priority == "CRITICA"), items.Count(x => x.Status == WorkItemStatus.Aguardando),
            persisted.Count(x => x.Status == WorkItemStatus.Concluido && x.AtualizadoEm.UtcDateTime.Date == now.UtcDateTime.Date));
        return new MinhaCentralDto(context, summary, items, BuildShortcuts());
    }

    private CentralWorkItemDto ToCentralItem(WorkItemDto item)
    {
        var route = ResolveRoute(item.Tipo, item.Id);
        return new CentralWorkItemDto(item.Id, $"{item.Tipo.Trim().ToUpperInvariant()}:{item.Id}", item.Tipo,
            item.Titulo, item.Descricao, item.Status, item.Prioridade, item.ResponsavelId, item.UnidadeId,
            item.VenceEm, item.AtualizadoEm, route.Label, route.DetailUrl, route.Action, route.ActionUrl);
    }

    private (string Label, string DetailUrl, string? Action, string? ActionUrl) ResolveRoute(string type, Guid id)
    {
        var normalized = type.Trim().ToUpperInvariant();
        if (normalized.Contains("OCORRENCIA") && modules.CanAccessModule("OCORRENCIAS"))
            return ("Ocorrência operacional", $"/Ocorrencias?id={id}", "Analisar ocorrência", $"/Ocorrencias?id={id}");
        if ((normalized.Contains("PLANTAO") || normalized.Contains("COBERTURA")) && modules.CanAccessModule("PLANTOES"))
            return ("Plantão", $"/Plantoes/Details/{id}", "Abrir plantão", $"/Plantoes/Details/{id}");
        if ((normalized.Contains("ESCALA") || normalized.Contains("CONFIRMACAO")) && modules.CanAccessModule("ESCALAS"))
            return ("Escala", "/Escalas", "Abrir escala", "/Escalas");
        if (normalized.Contains("EXECUCAO") && modules.CanAccessModule("EXECUCAO"))
            return ("Execução de plantão", "/ConferenciaExecucao", "Conferir execução", "/ConferenciaExecucao");
        if ((normalized.Contains("PAGAMENTO") || normalized.Contains("APURACAO")) && modules.CanAccessModule("FINANCEIRO"))
            return ("Apuração financeira", "/Financeiro", "Consultar apuração", "/Financeiro");
        return ("Pendência operacional", "/Pendencias", null, null);
    }

    private IReadOnlyList<CentralShortcutDto> BuildShortcuts()
    {
        var result = new List<CentralShortcutDto>();
        AddShortcut(result, "PLANTOES", "Plantões", "Consulte cobertura e confirmações.", "/Plantoes");
        AddShortcut(result, "ESCALAS", "Escalas", "Acompanhe a escala da operação.", "/Escalas");
        AddShortcut(result, "OCORRENCIAS", "Ocorrências", "Analise ocorrências abertas.", "/Ocorrencias");
        AddShortcut(result, "FINANCEIRO", "Financeiro", "Consulte apuração e demonstrativos.", "/Financeiro");
        if (current.IsTenantAdmin()) AddShortcut(result, "SEGURANCA", "Equipe e acessos", "Administre vínculos autorizados.", "/Seguranca/Usuarios");
        return result;
    }

    private void AddShortcut(ICollection<CentralShortcutDto> target, string module, string label, string description, string url)
    { if (modules.CanAccessModule(module)) target.Add(new CentralShortcutDto(label, description, url, module)); }
    private string? Claim(string type) => accessor.HttpContext?.User.FindFirst(type)?.Value;
    private string ResolveProfile() => current.IsGlobalAdmin() ? "Administrador global" : current.IsTenantAdmin() ? "Administrador do cliente" : current.IsDoctor() ? "Profissional" : "Operação";
    public Task<WorkItemMutationResult> MoveAsync(WorkItemMoveRequest r, CancellationToken ct) { if (!WorkItemStatus.All.Contains(r.Destination) || !WorkItemStatus.All.Contains(r.Source) || r.Position < 0) throw new ArgumentException("Destino, origem ou posição inválidos."); return ApplyAsync("MOVER", repository.MoveAsync(Tenant, Unit, UserId, r, ct), ct); }
    public Task<WorkItemMutationResult> AssignAsync(Guid id, Guid responsibleId, WorkItemVersionRequest r, CancellationToken ct) => ApplyAsync(responsibleId == Guid.Empty ? "ASSUMIR" : "ENCAMINHAR", repository.AssignAsync(Tenant, Unit, UserId, id, responsibleId == Guid.Empty ? UserId : responsibleId, r, ct), ct);
    public Task<WorkItemMutationResult> CommentAsync(Guid id, WorkItemCommentRequest r, CancellationToken ct) { if (string.IsNullOrWhiteSpace(r.Comment) || r.Comment.Trim().Length > 2000) throw new ArgumentException("Informe um comentário de até 2.000 caracteres."); return ApplyAsync("COMENTAR", repository.CommentAsync(Tenant, Unit, UserId, id, r, ct), ct); }
    public Task<WorkItemMutationResult> PostponeAsync(Guid id, WorkItemPostponeRequest r, CancellationToken ct) { if (r.DueAt <= DateTimeOffset.UtcNow) throw new ArgumentException("O novo prazo deve estar no futuro."); return ApplyAsync("ADIAR", repository.PostponeAsync(Tenant, Unit, UserId, id, r, ct), ct); }
    private async Task<WorkItemMutationResult> ApplyAsync(string action, Task<WorkItemMutationResult> operation, CancellationToken ct)
    {
        var result = await operation; if (result.Item != null && !result.Conflict) { await audit.RegistrarAsync(UserId, Tenant, "WORK_ITEM", result.Item.Id, action, new { result.Item.Status, result.Item.Versao }, true, null, current.Roles.FirstOrDefault(), ct); await realtime.PublishWorkItemAsync(Tenant, Unit, result.Item.Status == WorkItemStatus.Concluido ? "WorkItemConcluido" : "WorkItemAtualizado", result.Item, ct); } return result;
    }
}
