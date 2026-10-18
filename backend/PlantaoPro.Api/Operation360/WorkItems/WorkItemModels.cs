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

// R18 (r18b3 F7b): datas como DateTime — o reader do Npgsql devolve DateTime para timestamptz e o
// Dapper nao faz a conversao para DateTimeOffset. Com record (constructor unico) a binding falha na
// materializacao e /api/minha-central caia em 500 para todo papel nao-global, mesmo com zero linhas
// (o deserializer e gerado antes da primeira linha). Padrao da casa: todos os DTOs ligados ao Dapper
// usam DateTime (ver CobrancaPublicaDto, TimelineClinicaDto etc.).
public sealed record WorkItemDto(Guid Id, string Tipo, string Titulo, string Descricao, string Status,
    string Prioridade, Guid? ResponsavelId, Guid? UnidadeId, int Posicao, DateTime? VenceEm,
    int Versao, DateTime CriadoEm, DateTime AtualizadoEm);
// R19 (r19b1 F7e): mesmo contrato do WorkItemDto — history e materializado pelo Dapper direto da
// tabela work_item_history (criado_em timestamptz); DTOFF no parametro quebraria o match.
public sealed record WorkItemHistoryDto(Guid Id, string Acao, string? Origem, string? Destino, Guid UsuarioId, DateTime CriadoEm);
public sealed record WorkItemMoveRequest(Guid ItemId, string Source, string Destination, int Position, int Version, Guid IdempotencyKey);
public sealed record WorkItemVersionRequest(int Version, Guid IdempotencyKey);
public sealed record WorkItemCommentRequest(string Comment, int Version, Guid IdempotencyKey);
public sealed record WorkItemForwardRequest(Guid ResponsibleId, int Version, Guid IdempotencyKey);
public sealed record WorkItemPostponeRequest(DateTimeOffset DueAt, int Version, Guid IdempotencyKey);
public sealed record CentralSummaryDto(int Open, int Overdue, int Critical, int Waiting, int CompletedToday);
public sealed record CentralContextDto(Guid TenantId, string Organization, Guid? UnitId, string? Unit, string Profile, bool GlobalView, DateTimeOffset UpdatedAt);
public sealed record CentralWorkItemDto(Guid Id, string StableKey, string Type, string Title, string Description,
    string Status, string Priority, Guid? ResponsibleId, Guid? UnitId, DateTimeOffset? DueAt,
    DateTimeOffset UpdatedAt, string OriginLabel, string OriginUrl, string? NextAction, string? ActionUrl);
public sealed record CentralShortcutDto(string Label, string Description, string Url, string Module);
public sealed record MinhaCentralDto(CentralContextDto Context, CentralSummaryDto Summary,
    IReadOnlyList<CentralWorkItemDto> Items, IReadOnlyList<CentralShortcutDto> Shortcuts);
public sealed record WorkItemMutationResult(bool Found, bool Conflict, bool Duplicate, WorkItemDto? Item);
