namespace PlantaoPro.Application.Administrativo360;

public sealed record PedidoItemCommand(Guid ProdutoId, decimal Quantidade, decimal PrecoUnitario, decimal Desconto);
public sealed record CriarPedidoCommand(Guid FornecedorId, DateOnly? Previsao, decimal Frete, IReadOnlyList<PedidoItemCommand> Itens);
public sealed record ReceberItemCommand(Guid PedidoItemId, decimal Quantidade, string? Lote, DateOnly? Validade, Guid LocalId);
public sealed record ConfirmarRecebimentoCommand(Guid PedidoId, string Documento, string IdempotencyKey, IReadOnlyList<ReceberItemCommand> Itens);
public sealed record DecidirInspecaoCommand(Guid RecebimentoItemId, decimal Aprovada, decimal Reprovada, string Justificativa, string Destino, string IdempotencyKey);
public sealed record TransferirCommand(Guid ProdutoId, Guid LoteId, Guid OrigemId, Guid DestinoId, decimal Quantidade, string Motivo, string IdempotencyKey);
public sealed record ReservarCommand(Guid ProdutoId, Guid LoteId, Guid LocalId, decimal Quantidade, string Motivo, Guid? OrigemId, string IdempotencyKey, DateOnly? DataPrevistaUso = null, Guid? OrcamentoItemId = null);
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
public sealed record ItemReservaPlanejamento(Guid ProdutoId, string Produto, string Sku, decimal QuantidadeSolicitada, decimal QuantidadeReservada, decimal FaltaAtender, IReadOnlyList<LoteElegivelReserva> LotesElegiveis, Guid? OrcamentoItemId = null);
public sealed record PlanejamentoReservaOrcamento(Guid OrcamentoId, string Numero, string Situacao, DateOnly DataPrevista, string Hospital, IReadOnlyList<ItemReservaPlanejamento> Itens);

// Cirurgias Operacionais
public sealed record CriarCirurgiaCommand(Guid HospitalId, Guid? MedicoId, string Procedimento, DateOnly DataPrevista, TimeOnly? HoraPrevista, Guid? OrcamentoId, int? OrcamentoRevisao, Guid? ResponsavelId, Guid LocalDestinoId, string? Observacoes);
public sealed record AtualizarCirurgiaCommand(Guid CirurgiaId, Guid HospitalId, Guid? MedicoId, string Procedimento, DateOnly DataPrevista, TimeOnly? HoraPrevista, Guid? ResponsavelId, Guid LocalDestinoId, string? Observacoes);
public sealed record CancelarCirurgiaCommand(Guid CirurgiaId, string Motivo);
public sealed record CirurgiaResumo(Guid Id, string Numero, string Hospital, string? Medico, string Procedimento, DateOnly DataPrevista, TimeOnly? HoraPrevista, string? OrcamentoNumero, string LocalDestino, string Situacao, DateTimeOffset CriadoEm);
public sealed record CirurgiaDetalhes(Guid Id, string Numero, Guid HospitalId, string Hospital, Guid? MedicoId, string? Medico, string Procedimento, DateOnly DataPrevista, TimeOnly? HoraPrevista, Guid? OrcamentoId, string? OrcamentoNumero, int? OrcamentoRevisao, Guid? ResponsavelId, string? Responsavel, Guid LocalDestinoId, string LocalDestino, string Situacao, string? Observacoes, DateTimeOffset CriadoEm);

// Vales de Consignação
public sealed record ValeItemCommand(Guid ProdutoId, Guid LoteId, Guid? ReservaId, decimal QuantidadeSolicitada, decimal PrecoUnitario);
public sealed record CriarValeCommand(Guid? CirurgiaId, Guid? OrcamentoId, int? OrcamentoRevisao, Guid HospitalId, Guid? CustodianteId, Guid LocalOrigemId, Guid LocalDestinoId, DateOnly DataSaidaPrevista, DateOnly? DataRetornoPrevista, string? Observacoes, IReadOnlyList<ValeItemCommand> Itens, string? IdempotencyKey = null);
public sealed record SepararItemValeCommand(Guid ValeItemId, decimal QuantidadeSeparada);
public sealed record ConcluirSeparacaoValeCommand(Guid ValeId);
public sealed record ExpedirValeCommand(Guid ValeId, string IdempotencyKey);
public sealed record RegistrarConsumoValeCommand(Guid ValeId, Guid ValeItemId, decimal Quantidade, string? Motivo, string IdempotencyKey);
public sealed record RegistrarRetornoValeCommand(Guid ValeId, Guid ValeItemId, decimal Quantidade, string? Motivo, string IdempotencyKey);
public sealed record RegistrarPerdaValeCommand(Guid ValeId, Guid ValeItemId, decimal Quantidade, string Motivo, string IdempotencyKey);
public sealed record ReconciliarValeCommand(Guid ValeId, string? Observacoes, string IdempotencyKey);
public sealed record CancelarValeCommand(Guid ValeId, string Motivo);

public sealed record ValeResumo(Guid Id, string Numero, string? CirurgiaNumero, string? OrcamentoNumero, string Hospital, string LocalOrigem, string LocalDestino, DateOnly DataSaidaPrevista, DateTimeOffset? DataSaidaEfetiva, string Situacao, string SituacaoFinanceira, decimal TotalItens, DateTimeOffset CriadoEm);
public sealed record ValeItemDetalhes(Guid Id, Guid ProdutoId, string Sku, string Produto, string Unidade, Guid LoteId, string Lote, DateOnly? Validade, Guid? ReservaId, decimal QuantidadeSolicitada, decimal QuantidadeSeparada, decimal QuantidadeExpedida, decimal QuantidadeConsumida, decimal QuantidadeDevolvida, decimal QuantidadePerda, decimal PendenteCustodia, decimal PrecoUnitario);
public sealed record ValeEventoDetalhes(Guid Id, Guid ValeItemId, string Produto, string Lote, string Tipo, decimal Quantidade, DateTimeOffset DataEvento, string? Motivo, string? RegistradoPor);
public sealed record ValeDetalhes(Guid Id, string Numero, Guid? CirurgiaId, string? CirurgiaNumero, Guid? OrcamentoId, string? OrcamentoNumero, int? OrcamentoRevisao, Guid HospitalId, string Hospital, Guid? CustodianteId, string? Custodiante, Guid LocalOrigemId, string LocalOrigem, Guid LocalDestinoId, string LocalDestino, DateOnly DataSaidaPrevista, DateTimeOffset? DataSaidaEfetiva, DateOnly? DataRetornoPrevista, DateTimeOffset? DataReconciliacao, string Situacao, string SituacaoFinanceira, string? Observacoes, DateTimeOffset CriadoEm, IReadOnlyList<ValeItemDetalhes> Itens, IReadOnlyList<ValeEventoDetalhes> Eventos);

// Relatórios Operacionais e Rastreabilidade
public sealed record RelatorioValesPendentesItem(Guid ValeId, string Numero, string Hospital, string? CirurgiaNumero, DateTimeOffset? DataSaida, DateOnly? DataRetornoPrevista, decimal QuantidadePendente, string Responsavel, int DiasAtraso);
public sealed record RelatorioCustodiaExternaItem(Guid LocalId, string Local, Guid ProdutoId, string Sku, string Produto, Guid LoteId, string Lote, DateOnly? Validade, string Hospital, Guid ValeId, string ValeNumero, decimal Quantidade);
public sealed record RelatorioReconciliacaoItem(Guid ValeId, string Numero, string Hospital, string? Cirurgia, decimal TotalExpedido, decimal TotalConsumido, decimal TotalDevolvido, decimal TotalPerda, decimal PendenteCustodia, string Situacao);
public sealed record RelatorioRastreabilidadeItem(string Produto, string Lote, DateOnly? Validade, string OrigemTipo, string? DocumentoOrigem, string? ValeNumero, string? Hospital, string LocalAtual, string Condicao, decimal Quantidade, DateTimeOffset DataMovimento);

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

public interface ICirurgiaRepository
{
    Task<IReadOnlyList<CirurgiaResumo>> ListarAsync(Guid tenantId, string? busca, string? situacao, DateOnly? inicio, DateOnly? fim, CancellationToken ct);
    Task<CirurgiaDetalhes?> ObterPorIdAsync(Guid tenantId, Guid id, CancellationToken ct);
    Task<Guid> CriarAsync(Guid tenantId, Guid usuarioId, CriarCirurgiaCommand command, CancellationToken ct);
    Task AtualizarAsync(Guid tenantId, Guid usuarioId, AtualizarCirurgiaCommand command, CancellationToken ct);
    Task CancelarAsync(Guid tenantId, Guid usuarioId, Guid cirurgiaId, string motivo, CancellationToken ct);
}

public interface IValeConsignacaoRepository
{
    Task<IReadOnlyList<ValeResumo>> ListarAsync(Guid tenantId, string? busca, string? situacao, DateOnly? inicio, DateOnly? fim, CancellationToken ct);
    Task<ValeDetalhes?> ObterPorIdAsync(Guid tenantId, Guid id, CancellationToken ct);
    Task<Guid> CriarAsync(Guid tenantId, Guid usuarioId, CriarValeCommand command, CancellationToken ct);
    Task SepararItemAsync(Guid tenantId, Guid usuarioId, Guid valeId, SepararItemValeCommand command, CancellationToken ct);
    Task ConcluirSeparacaoAsync(Guid tenantId, Guid usuarioId, Guid valeId, CancellationToken ct);
    Task ExpedirAsync(Guid tenantId, Guid usuarioId, ExpedirValeCommand command, CancellationToken ct);
    Task RegistrarConsumoAsync(Guid tenantId, Guid usuarioId, RegistrarConsumoValeCommand command, CancellationToken ct);
    Task RegistrarRetornoAsync(Guid tenantId, Guid usuarioId, RegistrarRetornoValeCommand command, CancellationToken ct);
    Task RegistrarPerdaAsync(Guid tenantId, Guid usuarioId, RegistrarPerdaValeCommand command, CancellationToken ct);
    Task ReconciliarAsync(Guid tenantId, Guid usuarioId, ReconciliarValeCommand command, CancellationToken ct);
    Task CancelarAsync(Guid tenantId, Guid usuarioId, Guid valeId, string motivo, CancellationToken ct);
}

public interface IAdm360RelatoriosRepository
{
    Task<IReadOnlyList<RelatorioValesPendentesItem>> ValesPendentesAsync(Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<RelatorioCustodiaExternaItem>> CustodiaExternaAsync(Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<RelatorioReconciliacaoItem>> ReconciliacaoAsync(Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<RelatorioRastreabilidadeItem>> RastreabilidadeAsync(Guid tenantId, string? produtoOuLote, CancellationToken ct);
}
