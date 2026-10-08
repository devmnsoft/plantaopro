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
    /// <summary>Solicitações de plano pendentes/decididas (estado honesto no Index).</summary>
    public IReadOnlyList<MinhaSolicitacaoPlanoWebViewModel> Solicitacoes { get; set; } = Array.Empty<MinhaSolicitacaoPlanoWebViewModel>();
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

// ============================================================================
// B4 - Minha assinatura real (uso/limites/faturas/solicitações lidos da API;
// preços e limites sempre do banco, nunca inventados).
// ============================================================================

/// <summary>Uso + limites do plano (espelha o DTO da API uso).</summary>
public sealed class UsoPlanoWebViewModel
{
    public string PlanoNome { get; set; } = string.Empty;
    public string AssinaturaStatus { get; set; } = string.Empty;
    public DateTime? DataFim { get; set; }
    public DateTime? DataTrialFim { get; set; }
    public int MedicosUsados { get; set; }
    public int MedicosLimite { get; set; }
    public int HospitaisUsados { get; set; }
    public int HospitaisLimite { get; set; }
    public int PlantoesMesUsados { get; set; }
    public int PlantoesMesLimite { get; set; }
    public int UsuariosUsados { get; set; }
    public int UsuariosLimite { get; set; }
    public int ConvitesMesUsados { get; set; }
    public int ConvitesMesLimite { get; set; }
    public bool PermiteMobile { get; set; }
    public bool PermiteBi { get; set; }
    public bool PermiteRelatoriosAvancados { get; set; }
    public bool PermiteIntegracoes { get; set; }
    public bool PermiteOperacaoAssistida { get; set; }
    public bool PermiteSuportePrioritario { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>Solicitação de plano do próprio tenant (estado honesto, nunca "concluído").</summary>
public sealed class MinhaSolicitacaoPlanoWebViewModel
{
    public Guid Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string PlanoDestinoNome { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SolicitadoEm { get; set; }
    public string MensagemEstado { get; set; } = string.Empty;
}

/// <summary>Upgrade/downgrade: catálogo real + solicitações pendentes.</summary>
public sealed class UpgradeDowngradeWebViewModel
{
    public string Acao { get; set; } = "UPGRADE";
    public Guid AssinaturaPlanoId { get; set; }
    public string AssinaturaPlanoNome { get; set; } = string.Empty;
    public IReadOnlyList<PlanoPublicoWebViewModel> Planos { get; set; } = Array.Empty<PlanoPublicoWebViewModel>();
    public IReadOnlyList<MinhaSolicitacaoPlanoWebViewModel> Solicitacoes { get; set; } = Array.Empty<MinhaSolicitacaoPlanoWebViewModel>();
    public string? ErrorMessage { get; set; }
}

/// <summary>Faturas da assinatura (lidas da API; vazio honesto quando ausentes).</summary>
public sealed class FaturasAssinaturaWebViewModel
{
    public IReadOnlyList<AssinaturaCobrancaViewModel> Faturas { get; set; } = Array.Empty<AssinaturaCobrancaViewModel>();
    public string? ErrorMessage { get; set; }
}
