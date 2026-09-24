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
