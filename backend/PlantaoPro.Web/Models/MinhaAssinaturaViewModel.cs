namespace PlantaoPro.Web.Models;

public sealed class MinhaAssinaturaViewModel
{
    public string? Plano { get; set; }
    public string? Status { get; set; }
    public string? Ciclo { get; set; }
    public DateTimeOffset? Vencimento { get; set; }
    public string? ResponsavelFinanceiro { get; set; }
    public IReadOnlyList<AssinaturaMetricaViewModel> Limites { get; set; } = Array.Empty<AssinaturaMetricaViewModel>();
    public IReadOnlyList<AssinaturaMetricaViewModel> Uso { get; set; } = Array.Empty<AssinaturaMetricaViewModel>();
    public IReadOnlyList<string> Modulos { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Alertas { get; set; } = Array.Empty<string>();
    public IReadOnlyList<AssinaturaCobrancaViewModel> HistoricoCobranca { get; set; } = Array.Empty<AssinaturaCobrancaViewModel>();
    public string? ErrorMessage { get; set; }
    public bool HasSubscription => !string.IsNullOrWhiteSpace(Plano) || !string.IsNullOrWhiteSpace(Status);
}

public sealed class AssinaturaMetricaViewModel
{
    public string? Nome { get; set; }
    public string? Valor { get; set; }
}

public sealed class AssinaturaCobrancaViewModel
{
    public DateTimeOffset? Data { get; set; }
    public string? Status { get; set; }
    public string? Valor { get; set; }
}
