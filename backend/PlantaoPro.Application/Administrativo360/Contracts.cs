namespace PlantaoPro.Application.Administrativo360;

public sealed record PedidoItemCommand(Guid ProdutoId, decimal Quantidade, decimal PrecoUnitario, decimal Desconto);
public sealed record CriarPedidoCommand(Guid FornecedorId, DateOnly? Previsao, decimal Frete, IReadOnlyList<PedidoItemCommand> Itens);
public sealed record ReceberItemCommand(Guid PedidoItemId, decimal Quantidade, string? Lote, DateOnly? Validade, Guid LocalId);
public sealed record ConfirmarRecebimentoCommand(Guid PedidoId, string Documento, string IdempotencyKey, IReadOnlyList<ReceberItemCommand> Itens, Guid? DocumentoXmlId = null);
public sealed record DecidirInspecaoCommand(Guid RecebimentoItemId, decimal Aprovada, decimal Reprovada, string Justificativa, string Destino, string IdempotencyKey);
// A12 — Ocorrências/Não conformidades (writer de adm360_ocorrencias, rodada 10 r10b1)
public sealed record CriarOcorrenciaCommand(Guid ProdutoId, Guid LoteId, Guid LocalId, string Tipo, string Descricao, decimal Quantidade, Guid? ResponsavelId = null, DateOnly? Prazo = null, Guid? RecebimentoItemId = null);
public sealed record VersaoOcorrenciaCommand(long Versao);
public sealed record EncerrarOcorrenciaCommand(long Versao, string? Destino = null, string? JustificativaEncerramento = null, string? EfeitoFisico = null);
public sealed record OcorrenciaResumo(Guid Id, string Produto, string Lote, string Local, string Tipo, string Descricao, decimal Quantidade, string Situacao, Guid? ResponsavelId, DateOnly? Prazo, string? Destino, string? JustificativaEncerramento, long Versao, DateTimeOffset CriadoEm);
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
public sealed record ReservaAtivaPlanejamento(Guid Id, string Produto, string Lote, decimal Quantidade, DateTimeOffset CriadoEm);

public sealed record PlanejamentoReservaOrcamento(Guid OrcamentoId, string Numero, string Situacao, DateOnly DataPrevista, string Hospital, IReadOnlyList<ItemReservaPlanejamento> Itens, IReadOnlyList<ReservaAtivaPlanejamento> ReservasAtivas);

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
    Task CancelarAsync(Guid tenantId, Guid usuarioId, Guid pedidoId, string motivo, CancellationToken ct);
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

public interface IOcorrenciasRepository
{
    Task<IReadOnlyList<OcorrenciaResumo>> ListarAsync(Guid tenantId, string? situacao, CancellationToken ct);
    Task<Guid> AbrirAsync(Guid tenantId, Guid usuarioId, CriarOcorrenciaCommand command, CancellationToken ct);
    Task TratarAsync(Guid tenantId, Guid usuarioId, Guid ocorrenciaId, long versao, CancellationToken ct);
    Task ResolverAsync(Guid tenantId, Guid usuarioId, Guid ocorrenciaId, long versao, CancellationToken ct);
    Task EncerrarAsync(Guid tenantId, Guid usuarioId, Guid ocorrenciaId, EncerrarOcorrenciaCommand command, CancellationToken ct);
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

// ---------------------------------------------------------
// BLOCOS B, C, D: VALORIZAÇÃO, VENDAS, CONTAS A RECEBER, CAIXA E COMISSÕES
// ---------------------------------------------------------

public sealed record PreviaValorizacaoItemDto(
    Guid ValeItemId,
    Guid ProdutoId,
    string Sku,
    string Produto,
    Guid LoteId,
    string Lote,
    decimal QuantidadeConsumida,
    decimal PrecoUnitario,
    decimal Desconto,
    decimal Subtotal,
    decimal CustoUnitario,
    decimal CustoTotal,
    bool CustoAusente);

public sealed record PreviaValorizacaoDto(
    Guid ValeId,
    string ValeNumero,
    Guid? CirurgiaId,
    string? CirurgiaNumero,
    Guid? OrcamentoId,
    int? OrcamentoRevisao,
    Guid HospitalId,
    string Hospital,
    Guid PagadorId,
    string Pagador,
    Guid? VendedorId,
    string? Vendedor,
    decimal TotalBruto,
    decimal DescontoTotal,
    decimal TotalLiquido,
    decimal TotalCusto,
    decimal ComissaoPercentual,
    decimal ComissaoPrevista,
    IReadOnlyList<PreviaValorizacaoItemDto> Itens,
    IReadOnlyList<string> Pendencias);

public sealed record ValorizarValeCommand(
    Guid ValeId,
    Guid PagadorId,
    Guid? VendedorId,
    decimal ComissaoPercentual,
    decimal DescontoGeral,
    string IdempotencyKey,
    string? Observacoes);

public sealed record VendaResumoDto(
    Guid Id,
    string Numero,
    Guid? ValorizacaoId,
    string OrigemTipo,
    string Hospital,
    string Pagador,
    string? Vendedor,
    DateOnly Competencia,
    decimal TotalLiquido,
    decimal TotalCusto,
    decimal ComissaoPrevista,
    string CondicaoPagamento,
    string Situacao);

public sealed record VendaItemDto(
    Guid Id,
    Guid ProdutoId,
    string Sku,
    string Produto,
    Guid LoteId,
    string Lote,
    decimal Quantidade,
    decimal PrecoUnitario,
    decimal Desconto,
    decimal Subtotal,
    decimal CustoUnitario,
    decimal CustoTotal);

public sealed record VendaDetalhesDto(
    Guid Id,
    string Numero,
    Guid? ValorizacaoId,
    Guid? ValeId,
    string? ValeNumero,
    Guid ClienteId,
    string Cliente,
    Guid PagadorId,
    string Pagador,
    Guid? VendedorId,
    string? Vendedor,
    DateOnly Competencia,
    decimal TotalBruto,
    decimal Desconto,
    decimal TotalLiquido,
    decimal TotalCusto,
    decimal ComissaoPercentual,
    decimal ComissaoPrevista,
    string CondicaoPagamento,
    int QuantidadeParcelas,
    string Situacao,
    string? Observacoes,
    DateTime CriadoEm,
    IReadOnlyList<VendaItemDto> Itens,
    IReadOnlyList<TituloReceberResumoDto> Titulos);

public sealed record ConfirmarVendaCommand(
    Guid ValorizacaoId,
    string CondicaoPagamento,
    int QuantidadeParcelas,
    string IdempotencyKey,
    string? Observacoes);

public sealed record TituloReceberResumoDto(
    Guid Id,
    Guid VendaId,
    string VendaNumero,
    string Numero,
    string Pagador,
    int Parcela,
    int TotalParcelas,
    DateOnly DataEmissao,
    DateOnly DataVencimento,
    decimal ValorPrincipal,
    decimal ValorRecebido,
    decimal SaldoAberto,
    string Situacao,
    bool Vencido);

public sealed record TituloBaixaDto(
    Guid Id,
    Guid TituloId,
    Guid ContaId,
    string ContaNome,
    DateOnly DataRecebimento,
    decimal ValorRecebido,
    string MeioPagamento,
    string? Referencia,
    bool Estornado,
    string? RecebidoPor,
    DateTime CriadoEm);

public sealed record TituloEstornoDto(
    Guid Id,
    Guid BaixaId,
    decimal ValorEstornado,
    string Motivo,
    string? EstornadoPor,
    DateTime CriadoEm);

public sealed record TituloReceberDetalhesDto(
    Guid Id,
    Guid VendaId,
    string VendaNumero,
    string Numero,
    Guid PagadorId,
    string Pagador,
    int Parcela,
    int TotalParcelas,
    DateOnly DataEmissao,
    DateOnly DataVencimento,
    decimal ValorPrincipal,
    decimal ValorDesconto,
    decimal ValorJuros,
    decimal ValorRecebido,
    decimal SaldoAberto,
    string Situacao,
    bool Vencido,
    IReadOnlyList<TituloBaixaDto> Baixas,
    IReadOnlyList<TituloEstornoDto> Estornos);

public sealed record ReceberTituloCommand(
    Guid TituloId,
    Guid ContaId,
    DateOnly DataRecebimento,
    decimal Valor,
    string MeioPagamento,
    string? Referencia,
    string IdempotencyKey);

public sealed record EstornarBaixaCommand(
    Guid BaixaId,
    string Motivo,
    string IdempotencyKey);

public sealed record ContaFinanceiraDto(
    Guid Id,
    string Nome,
    string Tipo,
    string? Banco,
    string? Agencia,
    string? Conta,
    decimal SaldoInicial,
    DateOnly? DataSaldoInicial,
    decimal SaldoAtual,
    bool Ativo);

public sealed record CriarContaFinanceiraCommand(
    string Nome,
    string Tipo,
    string? Banco,
    string? Agencia,
    string? Conta,
    decimal SaldoInicial,
    DateOnly? DataSaldoInicial = null);

public sealed record AtualizarContaFinanceiraCommand(
    Guid ContaId,
    string Nome,
    string Tipo,
    string? Banco,
    string? Agencia,
    string? Conta,
    DateOnly? DataSaldoInicial,
    bool Ativo);

public sealed record MovimentoFinanceiroDto(
    Guid Id,
    Guid ContaId,
    string ContaNome,
    string Tipo,
    decimal Valor,
    DateOnly DataMovimento,
    string Descricao,
    string OrigemTipo,
    DateTime CriadoEm);

public sealed record ExtratoContaDto(
    ContaFinanceiraDto Conta,
    decimal SaldoAbertura,
    decimal TotalEntradas,
    decimal TotalSaidas,
    decimal SaldoFechamento,
    decimal SaldoAtual,
    IReadOnlyList<MovimentoFinanceiroDto> Movimentos);

public sealed record FluxoCaixaItemDto(
    DateOnly Data,
    string Descricao,
    string Origem,
    decimal PrevistoEntrada,
    decimal PrevistoSaida,
    decimal RealizadoEntrada,
    decimal RealizadoSaida,
    decimal SaldoAcumulado);

public sealed record FluxoCaixaDto(
    DateOnly DataBase,
    decimal SaldoAtualContas,
    decimal SaldoAberturaPeriodo,
    decimal SaldoFechamentoPeriodo,
    decimal TotalEntradasPrevistas,
    decimal TotalSaidasPrevistas,
    decimal TotalEntradasRealizadas,
    decimal TotalSaidasRealizadas,
    decimal TotalVencidosReceber,
    decimal TotalVencidosPagar,
    IReadOnlyList<FluxoCaixaItemDto> Itens);

public sealed record CaixaFechamentoDto(
    Guid Id,
    Guid ContaId,
    string ContaNome,
    DateOnly DataInicio,
    DateOnly DataFim,
    decimal SaldoAbertura,
    decimal TotalEntradas,
    decimal TotalSaidas,
    decimal SaldoCalculado,
    decimal SaldoConferido,
    decimal Diferenca,
    string? Justificativa,
    string Situacao,
    string? MotivoReabertura,
    DateTime? ReabertoEm,
    string? ReabertoPor,
    string FechadoPor,
    DateTime CriadoEm);

public sealed record FecharCaixaCommand(
    Guid ContaId,
    DateOnly DataInicio,
    DateOnly DataFim,
    decimal SaldoConferido,
    string? Justificativa,
    string IdempotencyKey);

public sealed record ReabrirCaixaCommand(
    Guid FechamentoId,
    string Motivo);

// Contas a Pagar
public sealed record TituloPagarResumoDto(
    Guid Id,
    string Numero,
    Guid FornecedorId,
    string Fornecedor,
    string OrigemTipo,
    Guid? OrigemId,
    string? Documento,
    DateOnly Competencia,
    DateOnly DataEmissao,
    DateOnly DataVencimento,
    int Parcela,
    int TotalParcelas,
    decimal ValorPrincipal,
    decimal ValorPago,
    decimal SaldoAberto,
    string Situacao,
    string? CentroCusto,
    bool Vencido);

public sealed record TituloPagamentoDto(
    Guid Id,
    Guid TituloId,
    Guid ContaId,
    string ContaNome,
    DateOnly DataPagamento,
    decimal ValorPago,
    string MeioPagamento,
    string? Referencia,
    bool Estornado,
    string? PagoPor,
    DateTime CriadoEm);

public sealed record PagamentoEstornoDto(
    Guid Id,
    Guid PagamentoId,
    decimal ValorEstornado,
    string Motivo,
    string? EstornadoPor,
    DateTime CriadoEm);

public sealed record TituloPagarDetalhesDto(
    Guid Id,
    string Numero,
    Guid FornecedorId,
    string Fornecedor,
    string OrigemTipo,
    Guid? OrigemId,
    string? Documento,
    DateOnly Competencia,
    DateOnly DataEmissao,
    DateOnly DataVencimento,
    int Parcela,
    int TotalParcelas,
    decimal ValorPrincipal,
    decimal ValorDesconto,
    decimal ValorJuros,
    decimal ValorPago,
    decimal SaldoAberto,
    string Situacao,
    string? CentroCusto,
    string? Observacoes,
    DateTime? AprovadoEm,
    string? AprovadoPor,
    string? CriadoPor,
    DateTime CriadoEm,
    bool Vencido,
    IReadOnlyList<TituloPagamentoDto> Pagamentos,
    IReadOnlyList<PagamentoEstornoDto> Estornos);


public sealed record CriarDespesaManualCommand(
    Guid FornecedorId,
    string? Documento,
    DateOnly Competencia,
    DateOnly DataVencimento,
    decimal ValorPrincipal,
    string? CentroCusto,
    string? Observacoes,
    string IdempotencyKey);

public sealed record AprovarTituloPagarCommand(
    Guid TituloId,
    string IdempotencyKey);

public sealed record PagarTituloCommand(
    Guid TituloId,
    Guid ContaId,
    DateOnly DataPagamento,
    decimal Valor,
    string MeioPagamento,
    string? Referencia,
    string IdempotencyKey);

public sealed record EstornarPagamentoCommand(
    Guid PagamentoId,
    string Motivo,
    string IdempotencyKey);

public sealed record GerarTituloPagarDeRecebimentoCommand(
    Guid RecebimentoId,
    DateOnly DataVencimento,
    string IdempotencyKey);

public sealed record GerarTituloPagarDeComissaoCommand(
    Guid VendedorId,
    IReadOnlyList<Guid> ComissaoIds,
    DateOnly DataVencimento,
    string IdempotencyKey);

// s9/s10 (rodada 9 r9f): a dimensão comercial é o CLIENTE (adm360_parceiros.eh_cliente),
// não o hospital. Registros históricos podem apontar v.cliente_id para a tabela clínica
// hospitais — o nome resolve via COALESCE(parceiro, hospital) e o fallback é
// "Cliente não identificado". Filtro por clienteId é validado contra o tenant (cross-tenant
// é rejeitado com 400 — ver ValidarClienteFiltroAsync no repositório).
public sealed record RelatorioVendasItem(
    string VendaNumero,
    DateOnly Competencia,
    string Cliente,
    string Pagador,
    string Vendedor,
    decimal TotalLiquido,
    decimal TotalCusto,
    decimal ComissaoPrevista,
    string Situacao,
    // s10 (rodada 9 r9f): bruto/desconto gravados na venda (antes ausentes do DTO e zerados
    // no espelho Web — o binding JSON era por nome e as propriedades antigas nunca casavam).
    decimal TotalBruto = 0m,
    decimal Desconto = 0m);

public sealed record RelatorioComissaoItem(
    string Vendedor,
    string VendaNumero,
    DateOnly DataRecebimento,
    decimal BaseCalculo,
    decimal Percentual,
    decimal ComissaoApropriada,
    string Situacao);

// s9 (rodada 9 r9f): dimensão comercial Cliente (não hospital) — ver comentário em RelatorioVendasItem.
public sealed record RelatorioMargemItem(
    string VendaNumero,
    string ValeNumero,
    string Cliente,
    decimal ReceitaLiquida,
    decimal CustoConsumido,
    decimal ComissaoPrevista,
    decimal ComissaoApropriada,
    decimal MargemContribuicao,
    decimal MargemPercentual);

// s10 (rodada 9 r9f): vencimentos por PARCELA — cada título/parcela é exatamente uma linha
// (sem multiplicação por JOINs); "vencido" é avaliado na data do relatório (referência =
// data de geração), mesma regra das contas a receber: vencimento < hoje && saldo aberto > 0.
public sealed record RelatorioVencimentoItem(
    string TituloNumero,
    string VendaNumero,
    DateOnly Competencia,
    string Cliente,
    string Pagador,
    int Parcela,
    int TotalParcelas,
    DateOnly DataEmissao,
    DateOnly DataVencimento,
    decimal ValorPrincipal,
    decimal ValorRecebido,
    decimal SaldoAberto,
    string Situacao,
    bool Vencido);

public interface IValorizacaoRepository
{
    Task<IReadOnlyList<ValeResumo>> ListarValesPendentesAsync(Guid tenantId, CancellationToken ct);
    Task<PreviaValorizacaoDto?> ObterPreviaAsync(Guid tenantId, Guid valeId, CancellationToken ct);
    Task<Guid> ValorizarAsync(Guid tenantId, Guid usuarioId, ValorizarValeCommand command, CancellationToken ct);
}

public interface IVendaRepository
{
    Task<IReadOnlyList<VendaResumoDto>> ListarAsync(Guid tenantId, string? busca, string? situacao, DateOnly? inicio, DateOnly? fim, CancellationToken ct);
    Task<VendaDetalhesDto?> ObterPorIdAsync(Guid tenantId, Guid id, CancellationToken ct);
    Task<Guid> ConfirmarVendaAsync(Guid tenantId, Guid usuarioId, ConfirmarVendaCommand command, CancellationToken ct);
    Task CancelarVendaAsync(Guid tenantId, Guid usuarioId, Guid vendaId, string motivo, CancellationToken ct);
}

public interface IContasReceberRepository
{
    Task<IReadOnlyList<TituloReceberResumoDto>> ListarAsync(Guid tenantId, string? busca, string? situacao, Guid? pagadorId, DateOnly? inicio, DateOnly? fim, CancellationToken ct);
    Task<TituloReceberDetalhesDto?> ObterPorIdAsync(Guid tenantId, Guid id, CancellationToken ct);
    Task<Guid> ReceberAsync(Guid tenantId, Guid usuarioId, ReceberTituloCommand command, CancellationToken ct);
    Task<Guid> EstornarAsync(Guid tenantId, Guid usuarioId, EstornarBaixaCommand command, CancellationToken ct);
}

public sealed record ComissaoPendenteDto(
    Guid Id,
    Guid VendaId,
    string VendaNumero,
    Guid BaixaId,
    Guid VendedorId,
    string VendedorNome,
    decimal BaseCalculo,
    decimal Percentual,
    decimal ValorComissao,
    string Situacao,
    DateTime CriadoEm);

public interface IContasPagarRepository
{
    Task<IReadOnlyList<TituloPagarResumoDto>> ListarAsync(Guid tenantId, string? busca, string? situacao, Guid? fornecedorId, string? centroCusto, DateOnly? inicio, DateOnly? fim, CancellationToken ct);
    Task<TituloPagarDetalhesDto?> ObterPorIdAsync(Guid tenantId, Guid id, CancellationToken ct);
    Task<Guid> CriarDespesaManualAsync(Guid tenantId, Guid usuarioId, CriarDespesaManualCommand command, CancellationToken ct);
    Task AprovarAsync(Guid tenantId, Guid usuarioId, AprovarTituloPagarCommand command, CancellationToken ct);
    Task<Guid> PagarAsync(Guid tenantId, Guid usuarioId, PagarTituloCommand command, CancellationToken ct);
    Task<Guid> EstornarPagamentoAsync(Guid tenantId, Guid usuarioId, EstornarPagamentoCommand command, CancellationToken ct);
    Task<Guid> GerarDeRecebimentoAsync(Guid tenantId, Guid usuarioId, GerarTituloPagarDeRecebimentoCommand command, CancellationToken ct);
    Task<Guid> GerarDeComissaoAsync(Guid tenantId, Guid usuarioId, GerarTituloPagarDeComissaoCommand command, CancellationToken ct);
    Task<IReadOnlyList<ComissaoPendenteDto>> ListarComissoesPendentesAsync(Guid tenantId, Guid? vendedorId, DateOnly? inicio, DateOnly? fim, CancellationToken ct);
}


// s11 (rodada 9 r9g): DRE gerencial POR CLIENTE em competência (vendas e despesas reconhecidas na
// competência do documento — NÃO fluxo de caixa, que é por data de recebimento/pagamento).
// ClienteId nulo = bucket "Não atribuída" (venda sem nome resolvido + despesas, que nesta base não
// têm vínculo com cliente comercial — limitação declarada, não atribuição inventada).
public sealed record DreGerencialLinha(
    Guid? ClienteId,
    string Cliente,
    int QtdVendas,
    decimal ReceitaBruta,
    decimal Deducoes,
    decimal ReceitaLiquida,
    decimal CustoRealizado,
    decimal ResultadoBruto,
    decimal ComissoesPrevistas,
    decimal DespesasAtribuidas,
    decimal ResultadoGerencial);

// Conciliação: consolidado = clientes + "Não atribuída", verificado re-agregando as mesmas fontes
// no período (deltas != 0 sinalizam divergência em vez de esconder).
public sealed record DreGerencialConciliacao(
    int VendasNoPeriodo,
    int DespesasNoPeriodo,
    bool ReceitaLiquidaBate,
    decimal DeltaReceitaLiquida,
    bool CustoBate,
    decimal DeltaCusto,
    bool DespesasBatem,
    decimal DeltaDespesas);

public sealed record DreGerencialDto(
    DateOnly? Inicio,
    DateOnly? Fim,
    IReadOnlyList<DreGerencialLinha> Linhas,
    DreGerencialConciliacao Conciliacao);

public interface ICaixaRepository
{
    Task<IReadOnlyList<ContaFinanceiraDto>> ListarContasAsync(Guid tenantId, CancellationToken ct);
    Task<ContaFinanceiraDto?> ObterContaPorIdAsync(Guid tenantId, Guid id, CancellationToken ct);
    Task<Guid> CriarContaAsync(Guid tenantId, Guid usuarioId, CriarContaFinanceiraCommand command, CancellationToken ct);
    Task AtualizarContaAsync(Guid tenantId, Guid usuarioId, AtualizarContaFinanceiraCommand command, CancellationToken ct);
    Task InativarContaAsync(Guid tenantId, Guid usuarioId, Guid contaId, CancellationToken ct);
    Task<ExtratoContaDto> ExtratoContaAsync(Guid tenantId, Guid contaId, DateOnly? inicio, DateOnly? fim, CancellationToken ct);
    Task<FluxoCaixaDto> FluxoCaixaAsync(Guid tenantId, DateOnly inicio, DateOnly fim, CancellationToken ct);
    Task<IReadOnlyList<CaixaFechamentoDto>> ListarFechamentosAsync(Guid tenantId, Guid? contaId, CancellationToken ct);
    Task<Guid> FecharCaixaAsync(Guid tenantId, Guid usuarioId, FecharCaixaCommand command, CancellationToken ct);
    Task ReabrirCaixaAsync(Guid tenantId, Guid usuarioId, ReabrirCaixaCommand command, CancellationToken ct);
}

public interface IAdm360FinanceiroRelatoriosRepository
{
    // s9 (rodada 9 r9f): filtro por CLIENTE comercial (renomeado de hospitalId); cross-tenant é rejeitado com 400.
    Task<IReadOnlyList<RelatorioVendasItem>> RelatorioVendasAsync(Guid tenantId, DateOnly? inicio, DateOnly? fim, Guid? clienteId, Guid? vendedorId, CancellationToken ct);
    Task<IReadOnlyList<RelatorioComissaoItem>> RelatorioComissoesAsync(Guid tenantId, Guid? vendedorId, DateOnly? inicio, DateOnly? fim, CancellationToken ct);
    // s9 (rodada 9 r9f): clienteId opcional final (compatibilidade de fonte com chamadas existentes).
    Task<IReadOnlyList<RelatorioMargemItem>> RelatorioMargemAsync(Guid tenantId, DateOnly? inicio, DateOnly? fim, CancellationToken ct, Guid? clienteId = null);
    // s10 (rodada 9 r9f): vencimentos por parcela, um título = uma linha; vencido na data do relatório.
    Task<IReadOnlyList<RelatorioVencimentoItem>> RelatorioVencimentosAsync(Guid tenantId, DateOnly? inicio, DateOnly? fim, Guid? clienteId, CancellationToken ct);
    // s11 (rodada 9 r9g): DRE gerencial por cliente em competência; cross-tenant → 400.
    Task<DreGerencialDto> RelatorioDreAsync(Guid tenantId, DateOnly? inicio, DateOnly? fim, Guid? clienteId, CancellationToken ct);
}

// Cadastros e Lookups Administrativo 360
public sealed record Parceiro360(
    Guid Id,
    string Nome,
    string? Documento,
    bool Fornecedor,
    bool Ativo,
    DateTime CriadoEm,
    bool EhHospital = false,
    bool EhPagador = false,
    bool EhCliente = false);
public sealed record Medico360(Guid Id, string Nome, string? Documento, bool Ativo);
public sealed record Produto360(Guid Id, string Sku, string Nome, string Unidade, string? CodigoBarras, bool ControlaLote, bool ExigeInspecao, decimal? PrecoCusto, bool Ativo, string? AnvisaNumero = null, string AnvisaAplicabilidade = "PENDENTE", bool AnvisaEntradaManual = true, DateOnly? AnvisaVigencia = null, string? AnvisaJustificacao = null, decimal? PrecoVenda = null)
{
    // s7 (rodada 9): campos derivados de precificação recalculados NO SERVIDOR (definição única
    // para listagem/detalhe/lookups — não vêm do banco e nunca são persistidos).
    // Custo ausente ≠ ZERO: se PrecoCusto ou PrecoVenda é nulo, o campo derivado fica nulo
    // (não entra na conta como zero). Denominador zero (venda=0 na margem; custo=0 no markup)
    // => "não calculável" (nulo no JSON). Margem/markup negativos passam sem clamp (evidência
    // de margem negativa). Política de arredondamento: 2 casas (padrão do módulo).
    public decimal? LucroBrutoUnitario => PrecoVenda.HasValue && PrecoCusto.HasValue
        ? Math.Round(PrecoVenda.Value - PrecoCusto.Value, 2)
        : null;
    public decimal? MargemPercentual => LucroBrutoUnitario.HasValue && PrecoVenda!.Value > 0m
        ? Math.Round(LucroBrutoUnitario!.Value / PrecoVenda!.Value * 100m, 2)
        : null;
    public decimal? MarkupPercentual => LucroBrutoUnitario.HasValue && PrecoCusto!.Value > 0m
        ? Math.Round(LucroBrutoUnitario!.Value / PrecoCusto!.Value * 100m, 2)
        : null;
}
public sealed record Local360(Guid Id, string Codigo, string Nome, string Tipo, bool Ativo);
public sealed record Lote360(Guid Id, Guid ProdutoId, string ProdutoNome, string Codigo, DateOnly? Validade, DateOnly? Fabricacao, decimal? CustoUnitario = null);
public sealed record Lookups360Bundle(
    IReadOnlyList<Parceiro360> Parceiros,
    IReadOnlyList<Produto360> Produtos,
    IReadOnlyList<Local360> Locais,
    IReadOnlyList<Lote360> Lotes,
    IReadOnlyList<Medico360>? Medicos = null,
    IReadOnlyList<Parceiro360>? Hospitais = null,
    IReadOnlyList<Parceiro360>? Pagadores = null,
    IReadOnlyList<Parceiro360>? Fornecedores = null)
{
    public IReadOnlyList<Medico360> Medicos { get; init; } = Medicos ?? Array.Empty<Medico360>();
    public IReadOnlyList<Parceiro360> Hospitais { get; init; } = Hospitais ?? Array.Empty<Parceiro360>();
    public IReadOnlyList<Parceiro360> Pagadores { get; init; } = Pagadores ?? Array.Empty<Parceiro360>();
    public IReadOnlyList<Parceiro360> Fornecedores { get; init; } = Fornecedores ?? Array.Empty<Parceiro360>();
}

public interface ICadastrosRepository
{
    Task<IReadOnlyList<Parceiro360>> ListarParceirosAsync(Guid tenantId, string? busca, bool? fornecedor, CancellationToken ct, int pagina = 1, int limite = 100, bool? ehHospital = null, bool? ehPagador = null, bool? ehCliente = null, bool apenasAtivos = false, bool apenasInativos = false);
    Task<Guid> SalvarParceiroAsync(Guid tenantId, Guid? id, string nome, string? documento, bool fornecedor, bool ativo, CancellationToken ct, bool ehHospital = false, bool ehPagador = false, bool ehCliente = false);
    Task AlternarStatusParceiroAsync(Guid tenantId, Guid id, bool ativo, CancellationToken ct);

    Task<IReadOnlyList<Produto360>> ListarProdutosAsync(Guid tenantId, string? busca, bool apenasAtivos, CancellationToken ct, int pagina = 1, int limite = 100, bool apenasInativos = false);
    Task<Guid> SalvarProdutoAsync(Guid tenantId, Guid? id, string sku, string nome, string unidade, string? codigoBarras, bool controlaLote, bool exigeInspecao, decimal? precoCusto, bool ativo, CancellationToken ct, string? anvisaNumero = null, string anvisaAplicabilidade = "PENDENTE", bool anvisaEntradaManual = true, DateOnly? anvisaVigencia = null, string? anvisaJustificacao = null, decimal? precoVenda = null);
    Task AlternarStatusProdutoAsync(Guid tenantId, Guid id, bool ativo, CancellationToken ct);

    Task<IReadOnlyList<Local360>> ListarLocaisAsync(Guid tenantId, string? tipo, bool apenasAtivos, CancellationToken ct, int pagina = 1, int limite = 100, bool apenasInativos = false, string? busca = null);
    Task<Guid> SalvarLocalAsync(Guid tenantId, Guid? id, string codigo, string nome, string tipo, bool ativo, CancellationToken ct);

    Task<IReadOnlyList<Lote360>> ListarLotesAsync(Guid tenantId, Guid? produtoId, CancellationToken ct);
    // s5 (rodada 9): registro de lote a partir da tela de produto. Não cria movimentação:
    // o cadastro do lote não ajusta estoque automaticamente (quantidade só por movimento c/ origem).
    Task<Guid> SalvarLoteAsync(Guid tenantId, Guid produtoId, string codigo, DateOnly? validade, decimal? custoUnitario, CancellationToken ct);
    Task<Lookups360Bundle> ObterLookupsAsync(Guid tenantId, CancellationToken ct);
}

