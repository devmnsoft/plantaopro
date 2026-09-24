namespace PlantaoPro.Web.Models;

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

