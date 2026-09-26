namespace PlantaoPro.Web.Models;

public sealed record SelectListItemViewModel(string Value, string Text);

public sealed record Administrativo360ResumoViewModel(int Departamentos, int Cargos, int ColaboradoresAtivos, int ContratosVigentes);
public sealed record Departamento360ViewModel(Guid Id, string Codigo, string Nome, bool Ativo);
public sealed record Cargo360ViewModel(Guid Id, string Codigo, string Nome, Guid? DepartamentoId, string? Departamento, bool Ativo);
public sealed record Colaborador360ViewModel(Guid Id, string Matricula, string Nome, string Cpf, string Email, Guid CargoId, string Cargo, Guid? DepartamentoId, string? Departamento, string Status);
public sealed record Contrato360ViewModel(Guid Id, Guid ColaboradorId, string Colaborador, string Tipo, DateOnly Inicio, DateOnly? Fim, decimal Salario, int CargaHorariaSemanal, string Status);

public sealed class Administrativo360PageViewModel
{
    public Administrativo360ResumoViewModel Resumo { get; init; } = new(0, 0, 0, 0);
    public IReadOnlyList<Departamento360ViewModel> Departamentos { get; init; } = Array.Empty<Departamento360ViewModel>();
    public IReadOnlyList<Cargo360ViewModel> Cargos { get; init; } = Array.Empty<Cargo360ViewModel>();
    public IReadOnlyList<Colaborador360ViewModel> Colaboradores { get; init; } = Array.Empty<Colaborador360ViewModel>();
    public IReadOnlyList<Contrato360ViewModel> Contratos { get; init; } = Array.Empty<Contrato360ViewModel>();
    public string? Erro { get; init; }
}

public sealed record OrcamentoItemViewModel(
    Guid? Id,
    Guid ProdutoId,
    string? Sku,
    string? Produto,
    string? Unidade,
    decimal Quantidade,
    decimal PrecoUnitario,
    decimal Desconto,
    decimal Total,
    decimal QuantidadeReservada,
    decimal FaltaAtender);

public sealed record OrcamentoResumoViewModel(
    Guid Id,
    string Numero,
    int Revisao,
    string Hospital,
    string? Medico,
    string Procedimento,
    string ResponsavelFinanceiro,
    DateOnly DataPrevista,
    DateOnly Validade,
    string Situacao,
    decimal TotalGeral,
    DateTimeOffset CriadoEm);

public sealed record OrcamentoDetalhesViewModel(
    Guid Id,
    string Numero,
    int Revisao,
    Guid HospitalId,
    string Hospital,
    Guid? MedicoId,
    string? Medico,
    string Procedimento,
    Guid ResponsavelFinanceiroId,
    string ResponsavelFinanceiro,
    Guid? VendedorId,
    DateOnly DataPrevista,
    DateOnly Validade,
    string Situacao,
    decimal TotalProdutos,
    decimal DescontoGeral,
    decimal TotalGeral,
    string? Observacoes,
    DateTimeOffset? AprovadoEm,
    int Versao,
    IReadOnlyList<OrcamentoItemViewModel> Itens);

public sealed record LoteElegivelReservaViewModel(
    Guid LoteId,
    string Lote,
    DateOnly? Validade,
    Guid LocalId,
    string Local,
    decimal Fisico,
    decimal Reservado,
    decimal Disponivel,
    bool ElegivelParaDataCirurgia);

public sealed record ItemReservaPlanejamentoViewModel(
    Guid ProdutoId,
    string Produto,
    string Sku,
    decimal QuantidadeSolicitada,
    decimal QuantidadeReservada,
    decimal FaltaAtender,
    IReadOnlyList<LoteElegivelReservaViewModel> LotesElegiveis);

public sealed record PlanejamentoReservaOrcamentoViewModel(
    Guid OrcamentoId,
    string Numero,
    string Situacao,
    DateOnly DataPrevista,
    string Hospital,
    IReadOnlyList<ItemReservaPlanejamentoViewModel> Itens);

public sealed record OrcamentoRevisaoHistoricoViewModel(
    Guid Id,
    int Revisao,
    string Motivo,
    string SnapshotJson,
    DateTimeOffset CriadoEm);

public sealed class OrcamentoFormViewModel
{
    public Guid? Id { get; set; }
    public Guid HospitalId { get; set; }
    public Guid? MedicoId { get; set; }
    public string Procedimento { get; set; } = string.Empty;
    public Guid ResponsavelFinanceiroId { get; set; }
    public Guid? VendedorId { get; set; }
    public DateOnly DataPrevista { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
    public DateOnly Validade { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
    public string? Observacoes { get; set; }
    public string? MotivoRevisao { get; set; }
    public List<OrcamentoItemInputModel> Itens { get; set; } = new();
    public IReadOnlyList<Parceiro360ViewModel> Hospitais { get; set; } = Array.Empty<Parceiro360ViewModel>();
    public IReadOnlyList<Medico360ViewModel> Medicos { get; set; } = Array.Empty<Medico360ViewModel>();
    public IReadOnlyList<Parceiro360ViewModel> Pagadores { get; set; } = Array.Empty<Parceiro360ViewModel>();
    public IReadOnlyList<Produto360ViewModel> ProdutosDisponiveis { get; set; } = Array.Empty<Produto360ViewModel>();
}

public sealed class OrcamentoItemInputModel
{
    public Guid ProdutoId { get; set; }
    public decimal Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Desconto { get; set; }
}

// Cirurgias Operacionais
public sealed record CirurgiaResumoViewModel(
    Guid Id,
    string Numero,
    string Hospital,
    string? Medico,
    string Procedimento,
    DateOnly DataPrevista,
    TimeOnly? HoraPrevista,
    string? OrcamentoNumero,
    string LocalDestino,
    string Situacao,
    DateTimeOffset CriadoEm);

public sealed record CirurgiaDetalhesViewModel(
    Guid Id,
    string Numero,
    Guid HospitalId,
    string Hospital,
    Guid? MedicoId,
    string? Medico,
    string Procedimento,
    DateOnly DataPrevista,
    TimeOnly? HoraPrevista,
    Guid? OrcamentoId,
    string? OrcamentoNumero,
    int? OrcamentoRevisao,
    Guid? ResponsavelId,
    string? Responsavel,
    Guid LocalDestinoId,
    string LocalDestino,
    string Situacao,
    string? Observacoes,
    DateTimeOffset CriadoEm);

public sealed class CirurgiaFormViewModel
{
    public Guid? Id { get; set; }
    public Guid HospitalId { get; set; }
    public Guid? MedicoId { get; set; }
    public string Procedimento { get; set; } = string.Empty;
    public DateOnly DataPrevista { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
    public TimeOnly? HoraPrevista { get; set; } = new TimeOnly(8, 0);
    public Guid? OrcamentoId { get; set; }
    public int? OrcamentoRevisao { get; set; }
    public Guid? ResponsavelId { get; set; }
    public Guid LocalDestinoId { get; set; }
    public string? Observacoes { get; set; }
    public IReadOnlyList<Parceiro360ViewModel> Hospitais { get; set; } = Array.Empty<Parceiro360ViewModel>();
    public IReadOnlyList<Medico360ViewModel> Medicos { get; set; } = Array.Empty<Medico360ViewModel>();
    public IReadOnlyList<Local360ViewModel> Locais { get; set; } = Array.Empty<Local360ViewModel>();
    public IReadOnlyList<OrcamentoResumoViewModel> Orcamentos { get; set; } = Array.Empty<OrcamentoResumoViewModel>();
}

// Vales de Consignação
public sealed record ValeResumoViewModel(
    Guid Id,
    string Numero,
    string? CirurgiaNumero,
    string? OrcamentoNumero,
    string Hospital,
    string LocalOrigem,
    string LocalDestino,
    DateOnly DataSaidaPrevista,
    DateTimeOffset? DataSaidaEfetiva,
    string Situacao,
    string SituacaoFinanceira,
    decimal TotalItens,
    DateTimeOffset CriadoEm);

public sealed record ValeItemDetalhesViewModel(
    Guid Id,
    Guid ProdutoId,
    string Sku,
    string Produto,
    string Unidade,
    Guid LoteId,
    string Lote,
    DateOnly? Validade,
    Guid? ReservaId,
    decimal QuantidadeSolicitada,
    decimal QuantidadeSeparada,
    decimal QuantidadeExpedida,
    decimal QuantidadeConsumida,
    decimal QuantidadeDevolvida,
    decimal QuantidadePerda,
    decimal PendenteCustodia,
    decimal PrecoUnitario);

public sealed record ValeEventoDetalhesViewModel(
    Guid Id,
    Guid ValeItemId,
    string Produto,
    string Lote,
    string Tipo,
    decimal Quantidade,
    DateTimeOffset DataEvento,
    string? Motivo,
    string? RegistradoPor);

public sealed record ValeDetalhesViewModel(
    Guid Id,
    string Numero,
    Guid? CirurgiaId,
    string? CirurgiaNumero,
    Guid? OrcamentoId,
    string? OrcamentoNumero,
    int? OrcamentoRevisao,
    Guid HospitalId,
    string Hospital,
    Guid? CustodianteId,
    string? Custodiante,
    Guid LocalOrigemId,
    string LocalOrigem,
    Guid LocalDestinoId,
    string LocalDestino,
    DateOnly DataSaidaPrevista,
    DateTimeOffset? DataSaidaEfetiva,
    DateOnly? DataRetornoPrevista,
    DateTimeOffset? DataReconciliacao,
    string Situacao,
    string SituacaoFinanceira,
    string? Observacoes,
    DateTimeOffset CriadoEm,
    IReadOnlyList<ValeItemDetalhesViewModel> Itens,
    IReadOnlyList<ValeEventoDetalhesViewModel> Eventos);

public sealed class ValeFormViewModel
{
    public Guid? CirurgiaId { get; set; }
    public Guid? OrcamentoId { get; set; }
    public int? OrcamentoRevisao { get; set; }
    public Guid HospitalId { get; set; }
    public Guid? CustodianteId { get; set; }
    public Guid LocalOrigemId { get; set; }
    public Guid LocalDestinoId { get; set; }
    public DateOnly DataSaidaPrevista { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
    public DateOnly? DataRetornoPrevista { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
    public string? Observacoes { get; set; }
    public List<ValeItemInputModel> Itens { get; set; } = new();
    public IReadOnlyList<Parceiro360ViewModel> Hospitais { get; set; } = Array.Empty<Parceiro360ViewModel>();
    public IReadOnlyList<Local360ViewModel> LocaisOrigem { get; set; } = Array.Empty<Local360ViewModel>();
    public IReadOnlyList<Local360ViewModel> LocaisDestino { get; set; } = Array.Empty<Local360ViewModel>();
    public IReadOnlyList<CirurgiaResumoViewModel> Cirurgias { get; set; } = Array.Empty<CirurgiaResumoViewModel>();
    public IReadOnlyList<OrcamentoResumoViewModel> Orcamentos { get; set; } = Array.Empty<OrcamentoResumoViewModel>();
    public IReadOnlyList<Produto360ViewModel> ProdutosDisponiveis { get; set; } = Array.Empty<Produto360ViewModel>();
    public IReadOnlyList<Lote360ViewModel> LotesDisponiveis { get; set; } = Array.Empty<Lote360ViewModel>();
}

public sealed class ValeItemInputModel
{
    public Guid ProdutoId { get; set; }
    public Guid LoteId { get; set; }
    public Guid? ReservaId { get; set; }
    public decimal QuantidadeSolicitada { get; set; }
    public decimal PrecoUnitario { get; set; }
}

public sealed class EventoValeInputModel
{
    public Guid ValeId { get; set; }
    public Guid ValeItemId { get; set; }
    public string Tipo { get; set; } = "CONSUMO";
    public decimal Quantidade { get; set; }
    public string? Motivo { get; set; }
}

// Relatórios Operacionais
public sealed record RelatorioValesPendentesViewModel(
    Guid ValeId,
    string Numero,
    string Hospital,
    string? CirurgiaNumero,
    DateTimeOffset? DataSaida,
    DateOnly? DataRetornoPrevista,
    decimal QuantidadePendente,
    string Responsavel,
    int DiasAtraso);

public sealed record RelatorioCustodiaExternaViewModel(
    Guid LocalId,
    string Local,
    Guid ProdutoId,
    string Sku,
    string Produto,
    Guid LoteId,
    string Lote,
    DateOnly? Validade,
    string Hospital,
    Guid ValeId,
    string ValeNumero,
    decimal Quantidade);

public sealed record RelatorioReconciliacaoViewModel(
    Guid ValeId,
    string Numero,
    string Hospital,
    string? Cirurgia,
    decimal TotalExpedido,
    decimal TotalConsumido,
    decimal TotalDevolvido,
    decimal TotalPerda,
    decimal PendenteCustodia,
    string Situacao);

public sealed record RelatorioRastreabilidadeViewModel(
    string Produto,
    string Lote,
    DateOnly? Validade,
    string OrigemTipo,
    string? DocumentoOrigem,
    string? ValeNumero,
    string? Hospital,
    string LocalAtual,
    string Condicao,
    decimal Quantidade,
    DateTimeOffset DataMovimento);

public sealed class Adm360RelatoriosIndexViewModel
{
    public IReadOnlyList<RelatorioValesPendentesViewModel> ValesPendentes { get; init; } = Array.Empty<RelatorioValesPendentesViewModel>();
    public IReadOnlyList<RelatorioCustodiaExternaViewModel> CustodiaExterna { get; init; } = Array.Empty<RelatorioCustodiaExternaViewModel>();
    public IReadOnlyList<RelatorioReconciliacaoViewModel> Reconciliacao { get; init; } = Array.Empty<RelatorioReconciliacaoViewModel>();
    public IReadOnlyList<RelatorioRastreabilidadeViewModel> Rastreabilidade { get; init; } = Array.Empty<RelatorioRastreabilidadeViewModel>();
    public string AbaAtiva { get; init; } = "pendentes";
    public string? Busca { get; init; }
}

public sealed class ValorizacaoIndexViewModel
{
    public IReadOnlyList<ValeResumoViewModel> ValesPendentes { get; init; } = Array.Empty<ValeResumoViewModel>();
    public string? Busca { get; init; }
}

public sealed record PreviaValorizacaoItemViewModel(
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

public sealed record PreviaValorizacaoViewModel(
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
    IReadOnlyList<PreviaValorizacaoItemViewModel> Itens,
    IReadOnlyList<string> Pendencias);

public sealed record VendaResumoViewModel(
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

public sealed record VendaItemViewModel(
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

public sealed record TituloReceberResumoViewModel(
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

public sealed record VendaDetalhesViewModel(
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
    IReadOnlyList<VendaItemViewModel> Itens,
    IReadOnlyList<TituloReceberResumoViewModel> Titulos);

public sealed record TituloBaixaViewModel(
    Guid Id,
    Guid TituloId,
    Guid ContaId,
    string ContaNome,
    DateOnly DataRecebimento,
    decimal ValorRecebido,
    string MeioPagamento,
    string? Referencia,
    bool Estornado,
    string? MotivoEstorno,
    DateTime CriadoEm);

public sealed record TituloReceberDetalhesViewModel(
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
    decimal ValorRecebido,
    decimal SaldoAberto,
    string Situacao,
    bool Vencido,
    string? Observacoes,
    DateTime CriadoEm,
    IReadOnlyList<TituloBaixaViewModel> Baixas);

public sealed record ContaFinanceiraViewModel(
    Guid Id,
    string Nome,
    string Tipo,
    string? Banco,
    string? Agencia,
    string? Conta,
    decimal SaldoInicial,
    DateOnly DataSaldoInicial,
    decimal SaldoAtual,
    bool Ativa,
    DateTime CriadoEm);

public sealed record ExtratoLancamentoViewModel(
    Guid Id,
    DateOnly Data,
    string Tipo,
    string OrigemTipo,
    Guid? OrigemId,
    string? Documento,
    string? Historico,
    decimal Valor,
    decimal SaldoApos);

public sealed record ExtratoContaViewModel(
    Guid ContaId,
    string ContaNome,
    decimal SaldoInicial,
    decimal TotalEntradas,
    decimal TotalSaidas,
    decimal SaldoFinal,
    IReadOnlyList<ExtratoLancamentoViewModel> Lancamentos);

public sealed record FluxoCaixaItemViewModel(
    DateOnly Data,
    string Tipo,
    string Descricao,
    string Origem,
    decimal PrevistoEntrada,
    decimal PrevistoSaida,
    decimal RealizadoEntrada,
    decimal RealizadoSaida,
    decimal SaldoAcumulado);

public sealed record FluxoCaixaViewModel(
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
    IReadOnlyList<FluxoCaixaItemViewModel> Itens);

public sealed record RelatorioVendasItemViewModel(
    string Numero,
    DateOnly Data,
    string Hospital,
    string Pagador,
    string? Vendedor,
    decimal TotalBruto,
    decimal Desconto,
    decimal TotalLiquido,
    decimal TotalCusto,
    decimal ComissaoPrevista,
    string Situacao);

public sealed record RelatorioComissaoItemViewModel(
    string Vendedor,
    string VendaNumero,
    DateOnly DataBaixa,
    decimal BaseCalculo,
    decimal Percentual,
    decimal ComissaoApropriada,
    string Situacao);

public sealed record RelatorioMargemItemViewModel(
    string VendaNumero,
    string ValeNumero,
    string Hospital,
    decimal ReceitaLiquida,
    decimal CustoConsumido,
    decimal ComissaoPrevista,
    decimal ComissaoApropriada,
    decimal MargemContribuicao,
    decimal MargemPercentual);

public sealed class ValorizacaoPreviaViewModel
{
    public PreviaValorizacaoViewModel Previa { get; init; } = null!;
    public IReadOnlyList<SelectListItemViewModel> Parceiros { get; init; } = Array.Empty<SelectListItemViewModel>();
    public IReadOnlyList<SelectListItemViewModel> Vendedores { get; init; } = Array.Empty<SelectListItemViewModel>();
}

public sealed class VendasIndexViewModel
{
    public IReadOnlyList<VendaResumoViewModel> Vendas { get; init; } = Array.Empty<VendaResumoViewModel>();
    public string? Busca { get; init; }
    public string? Situacao { get; init; }
    public DateOnly? Inicio { get; init; }
    public DateOnly? Fim { get; init; }
}

public sealed class VendaDetalhesPageViewModel
{
    public VendaDetalhesViewModel Venda { get; init; } = null!;
}

public sealed class TitulosIndexViewModel
{
    public IReadOnlyList<TituloReceberResumoViewModel> Titulos { get; init; } = Array.Empty<TituloReceberResumoViewModel>();
    public IReadOnlyList<SelectListItemViewModel> Pagadores { get; init; } = Array.Empty<SelectListItemViewModel>();
    public string? Busca { get; init; }
    public string? Situacao { get; init; }
    public Guid? PagadorId { get; init; }
    public DateOnly? Inicio { get; init; }
    public DateOnly? Fim { get; init; }
}

public sealed class TituloDetalhesPageViewModel
{
    public TituloReceberDetalhesViewModel Titulo { get; init; } = null!;
    public IReadOnlyList<ContaFinanceiraViewModel> Contas { get; init; } = Array.Empty<ContaFinanceiraViewModel>();
}

public sealed class FluxoCaixaPageViewModel
{
    public FluxoCaixaViewModel Fluxo { get; init; } = null!;
    public IReadOnlyList<ContaFinanceiraViewModel> Contas { get; init; } = Array.Empty<ContaFinanceiraViewModel>();
    public DateOnly Inicio { get; init; }
    public DateOnly Fim { get; init; }
}

public sealed class RelatoriosFinanceirosPageViewModel
{
    public IReadOnlyList<RelatorioVendasItemViewModel> Vendas { get; init; } = Array.Empty<RelatorioVendasItemViewModel>();
    public IReadOnlyList<RelatorioComissaoItemViewModel> Comissoes { get; init; } = Array.Empty<RelatorioComissaoItemViewModel>();
    public IReadOnlyList<RelatorioMargemItemViewModel> Margens { get; init; } = Array.Empty<RelatorioMargemItemViewModel>();
    public string AbaAtiva { get; init; } = "vendas";
    public DateOnly? Inicio { get; init; }
    public DateOnly? Fim { get; init; }
    public Guid? VendedorId { get; init; }
    public IReadOnlyList<SelectListItemViewModel> Vendedores { get; init; } = Array.Empty<SelectListItemViewModel>();
}

// ==========================================
// CONTAS A PAGAR & FECHAMENTO DE CAIXA VIEWMODELS
// ==========================================

public sealed record TituloPagarResumoViewModel(
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

public sealed record TituloPagamentoViewModel(
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

public sealed record PagamentoEstornoViewModel(
    Guid Id,
    Guid PagamentoId,
    decimal ValorEstornado,
    string Motivo,
    string? EstornadoPor,
    DateTime CriadoEm);

public sealed record TituloPagarDetalhesViewModel(
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
    IReadOnlyList<TituloPagamentoViewModel> Pagamentos,
    IReadOnlyList<PagamentoEstornoViewModel> Estornos);

public sealed class TitulosPagarIndexViewModel
{
    public IReadOnlyList<TituloPagarResumoViewModel> Titulos { get; init; } = Array.Empty<TituloPagarResumoViewModel>();
    public IReadOnlyList<SelectListItemViewModel> Fornecedores { get; init; } = Array.Empty<SelectListItemViewModel>();
    public string? Busca { get; init; }
    public string? Situacao { get; init; }
    public Guid? FornecedorId { get; init; }
    public string? CentroCusto { get; init; }
    public DateOnly? Inicio { get; init; }
    public DateOnly? Fim { get; init; }
}

public sealed class TituloPagarDetalhesPageViewModel
{
    public TituloPagarDetalhesViewModel Titulo { get; init; } = null!;
    public IReadOnlyList<ContaFinanceiraViewModel> Contas { get; init; } = Array.Empty<ContaFinanceiraViewModel>();
}

public sealed class DespesaManualFormViewModel
{
    public Guid FornecedorId { get; set; }
    public string? Documento { get; set; }
    public DateOnly Competencia { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly DataVencimento { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
    public decimal ValorPrincipal { get; set; }
    public string? CentroCusto { get; set; } = "ADMINISTRATIVO";
    public string? Observacoes { get; set; }
    public IReadOnlyList<SelectListItemViewModel> Fornecedores { get; set; } = Array.Empty<SelectListItemViewModel>();
}

public sealed record ComissaoPendenteViewModel(
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

public sealed class ComissoesPendentesIndexViewModel
{
    public IReadOnlyList<ComissaoPendenteViewModel> Comissoes { get; init; } = Array.Empty<ComissaoPendenteViewModel>();
    public IReadOnlyList<SelectListItemViewModel> Vendedores { get; init; } = Array.Empty<SelectListItemViewModel>();
    public Guid? VendedorId { get; init; }
    public DateOnly? Inicio { get; init; }
    public DateOnly? Fim { get; init; }
}

public sealed record CaixaFechamentoViewModel(
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

public sealed class FechamentoCaixaIndexViewModel
{
    public IReadOnlyList<CaixaFechamentoViewModel> Fechamentos { get; init; } = Array.Empty<CaixaFechamentoViewModel>();
    public IReadOnlyList<ContaFinanceiraViewModel> Contas { get; init; } = Array.Empty<ContaFinanceiraViewModel>();
    public Guid? ContaId { get; init; }
}

public sealed class ContasFinanceirasIndexViewModel
{
    public IReadOnlyList<ContaFinanceiraViewModel> Contas { get; init; } = Array.Empty<ContaFinanceiraViewModel>();
}

public sealed class ContaFinanceiraFormViewModel
{
    public Guid? Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = "BANCO";
    public string? Banco { get; set; }
    public string? Agencia { get; set; }
    public string? Conta { get; set; }
    public decimal SaldoInicial { get; set; }
    public DateOnly? DataSaldoInicial { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public bool Ativo { get; set; } = true;
}

// Cadastros e Lookups Administrativo 360
public sealed record Parceiro360ViewModel(Guid Id, string Nome, string? Documento, bool Fornecedor, bool Ativo, DateTime CriadoEm);
public sealed record Medico360ViewModel(Guid Id, string Nome, string? Documento, bool Ativo);
public sealed record Produto360ViewModel(Guid Id, string Sku, string Nome, string Unidade, string? CodigoBarras, bool ControlaLote, bool ExigeInspecao, decimal PrecoCusto, bool Ativo);
public sealed record Local360ViewModel(Guid Id, string Codigo, string Nome, string Tipo, bool Ativo);
public sealed record Lote360ViewModel(Guid Id, Guid ProdutoId, string ProdutoNome, string Codigo, DateOnly? Validade, DateOnly? Fabricacao);

public sealed class CadastrosIndexViewModel
{
    public IReadOnlyList<Parceiro360ViewModel> Parceiros { get; init; } = Array.Empty<Parceiro360ViewModel>();
    public IReadOnlyList<Produto360ViewModel> Produtos { get; init; } = Array.Empty<Produto360ViewModel>();
    public IReadOnlyList<Local360ViewModel> Locais { get; init; } = Array.Empty<Local360ViewModel>();
    public string AbaAtiva { get; init; } = "parceiros";
    public string? Erro { get; init; }
}

public sealed class ParceirosIndexViewModel
{
    public IReadOnlyList<Parceiro360ViewModel> Parceiros { get; init; } = Array.Empty<Parceiro360ViewModel>();
    public string? Busca { get; init; }
    public string? Papel { get; init; }
    public bool? Fornecedor { get; init; }
    public bool? Status { get; init; }
    public string? Erro { get; init; }
    public string? Sucesso { get; init; }
}

public sealed class ProdutosIndexViewModel
{
    public IReadOnlyList<Produto360ViewModel> Produtos { get; init; } = Array.Empty<Produto360ViewModel>();
    public string? Busca { get; init; }
    public bool? Status { get; init; }
    public bool ApenasAtivos { get; init; }
    public string? Erro { get; init; }
    public string? Sucesso { get; init; }
}

public sealed class LocaisIndexViewModel
{
    public IReadOnlyList<Local360ViewModel> Locais { get; init; } = Array.Empty<Local360ViewModel>();
    public string? Busca { get; init; }
    public string? Tipo { get; init; }
    public bool? Status { get; init; }
    public bool ApenasAtivos { get; init; }
    public string? Erro { get; init; }
    public string? Sucesso { get; init; }
}

public sealed class LotesIndexViewModel
{
    public IReadOnlyList<Lote360ViewModel> Lotes { get; init; } = Array.Empty<Lote360ViewModel>();
    public IReadOnlyList<Produto360ViewModel> Produtos { get; init; } = Array.Empty<Produto360ViewModel>();
    public string? Busca { get; init; }
    public Guid? ProdutoId { get; init; }
    public string? Erro { get; init; }
    public string? Sucesso { get; init; }
}

public sealed class Lookups360ViewModel
{
    public IReadOnlyList<Parceiro360ViewModel> Parceiros { get; init; } = Array.Empty<Parceiro360ViewModel>();
    public IReadOnlyList<Produto360ViewModel> Produtos { get; init; } = Array.Empty<Produto360ViewModel>();
    public IReadOnlyList<Local360ViewModel> Locais { get; init; } = Array.Empty<Local360ViewModel>();
    public IReadOnlyList<Lote360ViewModel> Lotes { get; init; } = Array.Empty<Lote360ViewModel>();
    public IReadOnlyList<Medico360ViewModel> Medicos { get; init; } = Array.Empty<Medico360ViewModel>();
    public IReadOnlyList<Parceiro360ViewModel> Hospitais { get; init; } = Array.Empty<Parceiro360ViewModel>();
    public IReadOnlyList<Parceiro360ViewModel> Pagadores { get; init; } = Array.Empty<Parceiro360ViewModel>();
    public IReadOnlyList<Parceiro360ViewModel> Fornecedores { get; init; } = Array.Empty<Parceiro360ViewModel>();
}

// Suprimentos e Estoque ViewModels
public sealed record PedidoCompraResumoViewModel(Guid Id, string Numero, string Fornecedor, string Situacao, decimal Total, DateTimeOffset CriadoEm);

public sealed class PedidosCompraIndexViewModel
{
    public IReadOnlyList<PedidoCompraResumoViewModel> Pedidos { get; init; } = Array.Empty<PedidoCompraResumoViewModel>();
    public IReadOnlyList<Parceiro360ViewModel> Fornecedores { get; init; } = Array.Empty<Parceiro360ViewModel>();
    public IReadOnlyList<Produto360ViewModel> Produtos { get; init; } = Array.Empty<Produto360ViewModel>();
    public string? Fornecedor { get; init; }
    public string? Situacao { get; init; }
    public DateOnly? Inicio { get; init; }
    public DateOnly? Fim { get; init; }
    public string? Erro { get; init; }
}

public sealed class RecebimentosIndexViewModel
{
    public IReadOnlyList<PedidoCompraResumoViewModel> PedidosPendentes { get; init; } = Array.Empty<PedidoCompraResumoViewModel>();
    public IReadOnlyList<Local360ViewModel> Locais { get; init; } = Array.Empty<Local360ViewModel>();
    public string? Erro { get; init; }
}

public sealed record InspecaoPendenteViewModel(Guid RecebimentoItemId, string Produto, string Lote, decimal Recebida, decimal Pendente, string Local);

public sealed class InspecoesIndexViewModel
{
    public IReadOnlyList<InspecaoPendenteViewModel> Pendentes { get; init; } = Array.Empty<InspecaoPendenteViewModel>();
    public string? Erro { get; init; }
}

public sealed record OcorrenciaViewModel(Guid Id, string Tipo, string Descricao, decimal Quantidade, string Situacao, DateOnly? Prazo, string? Destino, DateTimeOffset CriadoEm);

public sealed class OcorrenciasIndexViewModel
{
    public IReadOnlyList<OcorrenciaViewModel> Ocorrencias { get; init; } = Array.Empty<OcorrenciaViewModel>();
    public string? Erro { get; init; }
}

public sealed record SaldoEstoqueViewModel(Guid ProdutoId, string Produto, Guid LoteId, string Lote, DateOnly? Validade, Guid LocalId, string Local, string Condicao, decimal Fisico, decimal Reservado, decimal Disponivel);

public sealed class EstoqueIndexViewModel
{
    public IReadOnlyList<SaldoEstoqueViewModel> Saldos { get; init; } = Array.Empty<SaldoEstoqueViewModel>();
    public IReadOnlyList<Local360ViewModel> Locais { get; init; } = Array.Empty<Local360ViewModel>();
    public string? Busca { get; init; }
    public string? Condicao { get; init; }
    public Guid? LocalId { get; init; }
    public string? Erro { get; init; }
}

public sealed class MovimentacoesIndexViewModel
{
    public IReadOnlyList<SaldoEstoqueViewModel> Saldos { get; init; } = Array.Empty<SaldoEstoqueViewModel>();
    public IReadOnlyList<Local360ViewModel> Locais { get; init; } = Array.Empty<Local360ViewModel>();
    public string? Erro { get; init; }
}

public sealed record InventarioResumoViewModel(Guid Id, string Local, string Escopo, string Situacao, DateTimeOffset CriadoEm);

public sealed class InventariosIndexViewModel
{
    public IReadOnlyList<InventarioResumoViewModel> Inventarios { get; init; } = Array.Empty<InventarioResumoViewModel>();
    public IReadOnlyList<Local360ViewModel> Locais { get; init; } = Array.Empty<Local360ViewModel>();
    public IReadOnlyList<Produto360ViewModel> Produtos { get; init; } = Array.Empty<Produto360ViewModel>();
    public IReadOnlyList<Lote360ViewModel> Lotes { get; init; } = Array.Empty<Lote360ViewModel>();
    public string? Erro { get; init; }
}

public sealed record TarefaColetaViewModel(Guid Id, string Tipo, string Descricao, string Situacao);

public sealed class ColetaIndexViewModel
{
    public IReadOnlyList<TarefaColetaViewModel> Tarefas { get; init; } = Array.Empty<TarefaColetaViewModel>();
    public string? Erro { get; init; }
}



