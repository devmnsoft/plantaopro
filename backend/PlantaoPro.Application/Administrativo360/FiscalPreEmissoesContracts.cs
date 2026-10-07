namespace PlantaoPro.Application.Administrativo360;

// ============================================================================
// Fiscal — pré-emissão (MVP A29: pré-documento interno + conferência de referências)
// Sem emissão autorizada no MVP; status externo somente com evidência válida (H19).
// ============================================================================

// 1. Parâmetros fiscais de emissão (uma linha por tenant) --------------------

public sealed record ParametrosFiscaisDto(
    Guid Id,
    Guid TenantId,
    string? Uf,
    string? Municipio,
    string? RegimeFiscal,
    string? OperacaoFiscal,
    IReadOnlyDictionary<string, string> Cfops,
    Guid? ResponsavelId,
    string? ResponsavelNome,
    string Ambiente,
    string? Provedor,
    string? CertificadoReferencia,
    string Status,
    string? Observacao,
    DateTime CriadoEm,
    DateTime? AtualizadoEm
);

public sealed record SalvarParametrosFiscaisCommand(
    string? Uf,
    string? Municipio,
    string? RegimeFiscal,
    string? OperacaoFiscal,
    IDictionary<string, string>? Cfops,
    Guid? ResponsavelId,
    string? ResponsavelNome,
    string Ambiente,
    string? Provedor,
    /// <summary>NOME da referência do segredo (user-secrets/ambiente) — nunca o valor (A33).</summary>
    string? CertificadoReferencia,
    string? Observacao,
    bool BloqueioExterno = false
);

public interface IParametrosFiscaisRepository
{
    Task<ParametrosFiscaisDto?> ObterAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Upsert único por tenant; status derivado deterministicamente dos campos.</summary>
    Task<ParametrosFiscaisDto> SalvarAsync(Guid tenantId, Guid usuarioId, SalvarParametrosFiscaisCommand comando, CancellationToken ct = default);
}

// 2. Pré-nota de emissão (documento interno) ----------------------------------

public sealed record NotaPreEmitidaResumoDto(
    Guid Id,
    string Numero,
    string OrigemTipo,
    Guid? OrigemId,
    string Situacao,
    string DestinatarioNome,
    string? DestinatarioDocumento,
    decimal ValorTotal,
    string? ChaveAcessoExterna,
    DateTime? EmitidaEm,
    DateTime CriadoEm
);

public sealed record NotaPreEmitidaItemDto(
    Guid Id,
    string Descricao,
    decimal Quantidade,
    decimal PrecoUnitario,
    decimal Total
);

public sealed record NotaPreEmitidaDetalhesDto(
    NotaPreEmitidaResumoDto Resumo,
    IReadOnlyList<NotaPreEmitidaItemDto> Itens,
    string? RejeicaoMensagem,
    DateTime? CanceladaEm,
    string? MotivoCancelamento
);

public sealed record NotaPreEmitidaItemCommand(
    string Descricao,
    decimal Quantidade,
    decimal PrecoUnitario
);

/// <summary>
/// Criação da pré-nota (situação inicial RASCUNHO). Conferência de referência (A29):
/// origem não-manual exige documento de origem existente no tenant.
/// </summary>
public sealed record CriarNotaPreEmitidaCommand(
    string OrigemTipo,
    Guid? OrigemId,
    string DestinatarioNome,
    string? DestinatarioDocumento,
    IReadOnlyList<NotaPreEmitidaItemCommand> Itens
);

/// <summary>Substituição integral dos itens — permitida apenas em RASCUNHO.</summary>
public sealed record AtualizarNotaPreEmitidaItensCommand(
    Guid NotaId,
    IReadOnlyList<NotaPreEmitidaItemCommand> Itens
);

/// <summary>
/// Transição de situação com evidência quando a situação-alvo é externa
/// (AUTORIZADA/REJEITADA). IdempotencyKey opcional: repetição do mesmo comando
/// não duplica a transição/evento.
/// </summary>
public sealed record TransicionarNotaPreEmitidaCommand(
    Guid NotaId,
    string NovaSituacao,
    string? ChaveAcessoExterna = null,
    string? ProtocoloExterno = null,
    DateTime? EmitidaEm = null,
    string? RejeicaoMensagem = null,
    DateTime? CanceladaEm = null,
    string? MotivoCancelamento = null,
    string? IdempotencyKey = null
);

public interface INotasPreEmitidasRepository
{
    Task<IReadOnlyList<NotaPreEmitidaResumoDto>> ListarAsync(Guid tenantId, string? situacao = null, CancellationToken ct = default);

    Task<NotaPreEmitidaDetalhesDto?> ObterAsync(Guid tenantId, Guid notaId, CancellationToken ct = default);

    Task<Guid> CriarAsync(Guid tenantId, Guid usuarioId, CriarNotaPreEmitidaCommand comando, CancellationToken ct = default);

    Task AtualizarItensAsync(Guid tenantId, Guid usuarioId, AtualizarNotaPreEmitidaItensCommand comando, CancellationToken ct = default);

    Task TransicionarAsync(Guid tenantId, Guid usuarioId, TransicionarNotaPreEmitidaCommand comando, CancellationToken ct = default);
}
