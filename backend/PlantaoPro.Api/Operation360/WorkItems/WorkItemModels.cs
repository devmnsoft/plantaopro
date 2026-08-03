namespace PlantaoPro.Api.Operation360.WorkItems;

public static class WorkItemStatus
{
    public const string Entrada = "ENTRADA";
    public const string Prioridade = "PRIORIDADE";
    public const string EmAndamento = "EM_ANDAMENTO";
    public const string Aguardando = "AGUARDANDO";
    public const string Concluido = "CONCLUIDO";
    public const string Cancelado = "CANCELADO";
    public static readonly IReadOnlySet<string> All = new HashSet<string>(new[] { Entrada, Prioridade, EmAndamento, Aguardando, Concluido, Cancelado }, StringComparer.Ordinal);
}

public sealed record WorkItemDto(Guid Id, string Tipo, string Titulo, string Descricao, string Status,
    string Prioridade, Guid? ResponsavelId, Guid? UnidadeId, int Posicao, DateTimeOffset? VenceEm,
    int Versao, DateTimeOffset CriadoEm, DateTimeOffset AtualizadoEm);
public sealed record WorkItemHistoryDto(Guid Id, string Acao, string? Origem, string? Destino, Guid UsuarioId, DateTimeOffset CriadoEm);
public sealed record WorkItemMoveRequest(Guid ItemId, string Source, string Destination, int Position, int Version, Guid IdempotencyKey);
public sealed record WorkItemVersionRequest(int Version, Guid IdempotencyKey);
public sealed record WorkItemCommentRequest(string Comment, int Version, Guid IdempotencyKey);
public sealed record WorkItemForwardRequest(Guid ResponsibleId, int Version, Guid IdempotencyKey);
public sealed record WorkItemPostponeRequest(DateTimeOffset DueAt, int Version, Guid IdempotencyKey);
public sealed record CentralSummaryDto(int Open, int Overdue, int Critical, int Waiting, int CompletedToday);
public sealed record MinhaCentralDto(CentralSummaryDto Summary, IReadOnlyList<WorkItemDto> Items);
public sealed record WorkItemMutationResult(bool Found, bool Conflict, bool Duplicate, WorkItemDto? Item);
