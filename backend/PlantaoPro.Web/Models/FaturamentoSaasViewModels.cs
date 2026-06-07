namespace PlantaoPro.Web.Models;

public sealed class FaturamentoSaasResumoViewModel
{
    public decimal ReceitaPrevista { get; set; }
    public decimal ReceitaRecebida { get; set; }
    public long FaturasAbertas { get; set; }
    public long FaturasVencidas { get; set; }
    public long FaturasEmContestacao { get; set; }
}

public sealed class FaturaSaasViewModel
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public Guid AssinaturaId { get; set; }
    public DateOnly Competencia { get; set; }
    public decimal Valor { get; set; }
    public DateOnly Vencimento { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? ValorPago { get; set; }
    public DateOnly? DataPagamento { get; set; }
    public string FormaPagamento { get; set; } = string.Empty;
    public string MotivoCancelamento { get; set; } = string.Empty;
    public string MotivoContestacao { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
}

public sealed class InadimplenciaSaasViewModel
{
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public long FaturasVencidas { get; set; }
    public decimal ValorVencido { get; set; }
    public DateOnly VencimentoMaisAntigo { get; set; }
}

public sealed class FaturamentoSaasIndexViewModel
{
    public FaturamentoSaasResumoViewModel Resumo { get; set; } = new FaturamentoSaasResumoViewModel();
    public PagedResult<FaturaSaasViewModel> Faturas { get; set; } = PagedResult<FaturaSaasViewModel>.Empty();
    public string? Status { get; set; }
    public string? Competencia { get; set; }
}
