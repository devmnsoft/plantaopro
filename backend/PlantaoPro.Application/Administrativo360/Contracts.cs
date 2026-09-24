namespace PlantaoPro.Application.Administrativo360;

public sealed record PedidoItemCommand(Guid ProdutoId, decimal Quantidade, decimal PrecoUnitario, decimal Desconto);
public sealed record CriarPedidoCommand(Guid FornecedorId, DateOnly? Previsao, decimal Frete, IReadOnlyList<PedidoItemCommand> Itens);
public sealed record ReceberItemCommand(Guid PedidoItemId, decimal Quantidade, string? Lote, DateOnly? Validade, Guid LocalId);
public sealed record ConfirmarRecebimentoCommand(Guid PedidoId, string Documento, string IdempotencyKey, IReadOnlyList<ReceberItemCommand> Itens);
public sealed record DecidirInspecaoCommand(Guid RecebimentoItemId, decimal Aprovada, decimal Reprovada, string Justificativa, string Destino, string IdempotencyKey);
public sealed record TransferirCommand(Guid ProdutoId, Guid LoteId, Guid OrigemId, Guid DestinoId, decimal Quantidade, string Motivo, string IdempotencyKey);
public sealed record ReservarCommand(Guid ProdutoId, Guid LoteId, Guid LocalId, decimal Quantidade, string Origem, Guid OrigemId, string IdempotencyKey, DateOnly? DataPrevistaUso = null);
public sealed record RegistrarLeituraCommand(Guid TarefaId, Guid ScanId, string Codigo, string? Lote, decimal Quantidade);
public sealed record AbrirInventarioCommand(Guid LocalId, string Escopo);
public sealed record ContarInventarioCommand(Guid ProdutoId, Guid LoteId, decimal Quantidade, string? Condicao = "LIBERADO");
public sealed record InventarioResumo(Guid Id, string Local, string Escopo, string Situacao, DateTimeOffset CriadoEm);

public sealed record PedidoResumo(Guid Id, string Numero, string Fornecedor, string Situacao, decimal Total, DateTimeOffset CriadoEm);
public sealed record SaldoEstoque(Guid ProdutoId, string Produto, Guid LoteId, string Lote, DateOnly? Validade, Guid LocalId, string Local, string Condicao, decimal Fisico, decimal Reservado, decimal Disponivel);
public sealed record InspecaoPendente(Guid RecebimentoItemId, string Produto, string Lote, decimal Recebida, decimal Pendente, string Local);
public sealed record TarefaColeta(Guid Id, string Tipo, string Descricao, string Situacao);

// Orçamentos Cirúrgicos e Reservas
public sealed record OrcamentoItemCommand(Guid ProdutoId, decimal Quantidade, decimal PrecoUnitario, decimal Desconto);
public sealed record CriarOrcamentoCommand(Guid HospitalId, Guid? MedicoId, string Procedimento, Guid ResponsavelFinanceiroId, Guid? VendedorId, DateOnly DataPrevista, DateOnly Validade, string? Observacoes, IReadOnlyList<OrcamentoItemCommand> Itens, string? IdempotencyKey = null);
public sealed record AtualizarOrcamentoCommand(Guid OrcamentoId, Guid HospitalId, Guid? MedicoId, string Procedimento, Guid ResponsavelFinanceiroId, Guid? VendedorId, DateOnly DataPrevista, DateOnly Validade, string? Observacoes, string? MotivoRevisao, IReadOnlyList<OrcamentoItemCommand> Itens);

public sealed record OrcamentoResumo(Guid Id, string Numero, int Revisao, string Hospital, string? Medico, string Procedimento, string ResponsavelFinanceiro, DateOnly DataPrevista, DateOnly Validade, string Situacao, decimal TotalGeral, DateTimeOffset CriadoEm);
public sealed record OrcamentoItemDetalhe(Guid Id, Guid ProdutoId, string Sku, string Produto, string Unidade, decimal Quantidade, decimal PrecoUnitario, decimal Desconto, decimal Total, decimal QuantidadeReservada, decimal FaltaAtender);
public sealed record OrcamentoDetalhes(Guid Id, string Numero, int Revisao, Guid HospitalId, string Hospital, Guid? MedicoId, string? Medico, string Procedimento, Guid ResponsavelFinanceiroId, string ResponsavelFinanceiro, Guid? VendedorId, DateOnly DataPrevista, DateOnly Validade, string Situacao, decimal TotalProdutos, decimal DescontoGeral, decimal TotalGeral, string? Observacoes, DateTimeOffset? AprovadoEm, int Versao, IReadOnlyList<OrcamentoItemDetalhe> Itens);
public sealed record OrcamentoRevisaoHistorico(Guid Id, int Revisao, string Motivo, string SnapshotJson, DateTimeOffset CriadoEm);

public sealed record LoteElegivelReserva(Guid LoteId, string Lote, DateOnly? Validade, Guid LocalId, string Local, decimal Fisico, decimal Reservado, decimal Disponivel, bool ElegivelParaDataCirurgia);
public sealed record ItemReservaPlanejamento(Guid ProdutoId, string Produto, string Sku, decimal QuantidadeSolicitada, decimal QuantidadeReservada, decimal FaltaAtender, IReadOnlyList<LoteElegivelReserva> LotesElegiveis);
public sealed record PlanejamentoReservaOrcamento(Guid OrcamentoId, string Numero, string Situacao, DateOnly DataPrevista, string Hospital, IReadOnlyList<ItemReservaPlanejamento> Itens);

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

public interface IOrcamentoCirurgicoRepository
{
    Task<IReadOnlyList<OrcamentoResumo>> ListarAsync(Guid tenantId, string? busca, string? situacao, DateOnly? inicio, DateOnly? fim, CancellationToken ct);
    Task<OrcamentoDetalhes?> ObterPorIdAsync(Guid tenantId, Guid id, CancellationToken ct);
    Task<Guid> CriarAsync(Guid tenantId, Guid usuarioId, CriarOrcamentoCommand command, CancellationToken ct);
    Task AtualizarAsync(Guid tenantId, Guid usuarioId, AtualizarOrcamentoCommand command, CancellationToken ct);
    Task AprovarAsync(Guid tenantId, Guid usuarioId, Guid orcamentoId, string idempotencyKey, CancellationToken ct);
    Task RejeitarAsync(Guid tenantId, Guid usuarioId, Guid orcamentoId, string motivo, CancellationToken ct);
    Task CancelarAsync(Guid tenantId, Guid usuarioId, Guid orcamentoId, string motivo, CancellationToken ct);
    Task<PlanejamentoReservaOrcamento?> ObterPlanejamentoReservaAsync(Guid tenantId, Guid orcamentoId, CancellationToken ct);
    Task ReservarItemAsync(Guid tenantId, Guid usuarioId, Guid orcamentoId, ReservarCommand command, CancellationToken ct);
    Task CancelarReservaAsync(Guid tenantId, Guid usuarioId, Guid orcamentoId, Guid reservaId, string motivo, CancellationToken ct);
    Task<IReadOnlyList<OrcamentoRevisaoHistorico>> ObterRevisoesAsync(Guid tenantId, Guid orcamentoId, CancellationToken ct);
}
