namespace PlantaoPro.Application.Administrativo360;

public sealed record PedidoItemCommand(Guid ProdutoId, decimal Quantidade, decimal PrecoUnitario, decimal Desconto);
public sealed record CriarPedidoCommand(Guid FornecedorId, DateOnly? Previsao, decimal Frete, IReadOnlyList<PedidoItemCommand> Itens);
public sealed record ReceberItemCommand(Guid PedidoItemId, decimal Quantidade, string? Lote, DateOnly? Validade, Guid LocalId);
public sealed record ConfirmarRecebimentoCommand(Guid PedidoId, string Documento, string IdempotencyKey, IReadOnlyList<ReceberItemCommand> Itens);
public sealed record DecidirInspecaoCommand(Guid RecebimentoItemId, decimal Aprovada, decimal Reprovada, string Justificativa, string Destino, string IdempotencyKey);
public sealed record TransferirCommand(Guid ProdutoId, Guid LoteId, Guid OrigemId, Guid DestinoId, decimal Quantidade, string Motivo, string IdempotencyKey);
public sealed record ReservarCommand(Guid ProdutoId, Guid LoteId, Guid LocalId, decimal Quantidade, string Origem, Guid OrigemId, string IdempotencyKey);
public sealed record RegistrarLeituraCommand(Guid TarefaId, Guid ScanId, string Codigo, string? Lote, decimal Quantidade);
public sealed record AbrirInventarioCommand(Guid LocalId, string Escopo);
public sealed record ContarInventarioCommand(Guid ProdutoId, Guid LoteId, decimal Quantidade);
public sealed record InventarioResumo(Guid Id, string Local, string Escopo, string Situacao, DateTimeOffset CriadoEm);

public sealed record PedidoResumo(Guid Id, string Numero, string Fornecedor, string Situacao, decimal Total, DateTimeOffset CriadoEm);
public sealed record SaldoEstoque(Guid ProdutoId, string Produto, Guid LoteId, string Lote, DateOnly? Validade, Guid LocalId, string Local, string Condicao, decimal Fisico, decimal Reservado, decimal Disponivel);
public sealed record InspecaoPendente(Guid RecebimentoItemId, string Produto, string Lote, decimal Recebida, decimal Pendente, string Local);
public sealed record TarefaColeta(Guid Id, string Tipo, string Descricao, string Situacao);

public interface IComprasRepository
{
    Task<IReadOnlyList<PedidoResumo>> ListarAsync(Guid tenantId, string? fornecedor, string? situacao, DateOnly? inicio, DateOnly? fim, CancellationToken ct);
    Task<Guid> CriarAsync(Guid tenantId, Guid usuarioId, CriarPedidoCommand command, CancellationToken ct);
    Task AprovarAsync(Guid tenantId, Guid usuarioId, Guid pedidoId, string idempotencyKey, CancellationToken ct);
    Task<Guid> ReceberAsync(Guid tenantId, Guid usuarioId, ConfirmarRecebimentoCommand command, CancellationToken ct);
}
public interface IEstoqueRepository
{
    Task<IReadOnlyList<SaldoEstoque>> ConsultarAsync(Guid tenantId, string? busca, string? condicao, Guid? localId, CancellationToken ct);
    Task TransferirAsync(Guid tenantId, Guid usuarioId, TransferirCommand command, CancellationToken ct);
    Task ReservarAsync(Guid tenantId, Guid usuarioId, ReservarCommand command, CancellationToken ct);
}
public interface IQualidadeRepository
{
    Task<IReadOnlyList<InspecaoPendente>> PendentesAsync(Guid tenantId, CancellationToken ct);
    Task DecidirAsync(Guid tenantId, Guid usuarioId, DecidirInspecaoCommand command, CancellationToken ct);
}
public interface IColetaRepository
{
    Task<IReadOnlyList<TarefaColeta>> TarefasAsync(Guid tenantId, Guid usuarioId, CancellationToken ct);
    Task RegistrarAsync(Guid tenantId, Guid usuarioId, RegistrarLeituraCommand command, CancellationToken ct);
}
public interface IInventarioRepository
{
    Task<IReadOnlyList<InventarioResumo>> ListarAsync(Guid tenantId, CancellationToken ct);
    Task<Guid> AbrirAsync(Guid tenantId, Guid usuarioId, AbrirInventarioCommand command, CancellationToken ct);
    Task ContarAsync(Guid tenantId, Guid usuarioId, Guid inventarioId, ContarInventarioCommand command, CancellationToken ct);
    Task AprovarAsync(Guid tenantId, Guid usuarioId, Guid inventarioId, string justificativa, string idempotencyKey, CancellationToken ct);
    Task CancelarAsync(Guid tenantId, Guid usuarioId, Guid inventarioId, string motivo, CancellationToken ct);
}
