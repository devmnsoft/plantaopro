namespace PlantaoPro.Web.Models;

public sealed class PlanoSaasViewModel
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorMensal { get; set; }
    public int LimiteMedicos { get; set; }
    public int LimiteHospitais { get; set; }
    public int LimitePlantoesMes { get; set; }
    public bool PermiteMobile { get; set; }
    public bool PermiteBi { get; set; }
    public bool PermiteRelatoriosAvancados { get; set; }
    public bool PermiteIntegracoes { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class PlanoRecursoSaasViewModel
{
    public Guid Id { get; set; }
    public Guid PlanoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public bool Habilitado { get; set; }
    public int? Limite { get; set; }
}

public sealed class PlanosSaasIndexViewModel
{
    public PagedResult<PlanoSaasViewModel> Planos { get; set; } = PagedResult<PlanoSaasViewModel>.Empty();
    public string? Status { get; set; }
}

public sealed class AssinaturaSaasViewModel
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public Guid PlanoId { get; set; }
    public string PlanoNome { get; set; } = string.Empty;
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal ValorContratado { get; set; }
    public int DiaVencimento { get; set; }
    public string Observacoes { get; set; } = string.Empty;
}

public sealed class AssinaturasSaasIndexViewModel
{
    public PagedResult<AssinaturaSaasViewModel> Assinaturas { get; set; } = PagedResult<AssinaturaSaasViewModel>.Empty();
    public string? Status { get; set; }
}

public sealed class SaasResumoExecutivoViewModel
{
    public long ClientesAtivos { get; set; }
    public long ClientesTrial { get; set; }
    public long ClientesSuspensos { get; set; }
    public long ClientesCancelados { get; set; }
    public long ClientesRisco { get; set; }
    public long ClientesCriticos { get; set; }
    public decimal ReceitaPrevistaMes { get; set; }
    public decimal ReceitaRecebidaMes { get; set; }
    public long FaturasAbertas { get; set; }
    public long FaturasVencidas { get; set; }
    public decimal MrrEstimado { get; set; }
    public decimal ChurnEstimado { get; set; }
    public long ClientesProximosLimite { get; set; }
    public long OportunidadesUpgrade { get; set; }
    public long AlertasAbertos { get; set; }
}

public sealed class ClienteSaudeSaasViewModel
{
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string Classificacao { get; set; } = string.Empty;
    public int Score { get; set; }
    public bool Inadimplente { get; set; }
    public bool UsoAlto { get; set; }
    public bool Inativo { get; set; }
    public bool ElegivelUpgrade { get; set; }
    public IEnumerable<string> Riscos { get; set; } = Array.Empty<string>();
    public IEnumerable<string> Oportunidades { get; set; } = Array.Empty<string>();
    public IEnumerable<string> AcoesRecomendadas { get; set; } = Array.Empty<string>();
}

public sealed class ClienteAlertaSaasViewModel
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Severidade { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public bool Resolvido { get; set; }
    public DateTime RegDate { get; set; }
}

public sealed class CustomerSuccessIndexViewModel
{
    public SaasResumoExecutivoViewModel Resumo { get; set; } = new SaasResumoExecutivoViewModel();
    public IEnumerable<ClienteAlertaSaasViewModel> Alertas { get; set; } = Array.Empty<ClienteAlertaSaasViewModel>();
}

public sealed class RelatorioSaasLinhaViewModel
{
    public Guid? ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Classificacao { get; set; } = string.Empty;
    public long Quantidade { get; set; }
    public decimal Valor { get; set; }
    public DateOnly? Competencia { get; set; }
}

public sealed class UsoPlanoViewModel
{
    public Guid ClienteId { get; set; }
    public Guid AssinaturaId { get; set; }
    public Guid PlanoId { get; set; }
    public string PlanoNome { get; set; } = string.Empty;
    public string AssinaturaStatus { get; set; } = string.Empty;
    public int MedicosUsados { get; set; }
    public int MedicosLimite { get; set; }
    public int HospitaisUsados { get; set; }
    public int HospitaisLimite { get; set; }
    public int PlantoesMesUsados { get; set; }
    public int PlantoesMesLimite { get; set; }
    public bool PermiteMobile { get; set; }
    public bool PermiteBi { get; set; }
    public bool PermiteRelatoriosAvancados { get; set; }
    public bool PermiteIntegracoes { get; set; }
}

public sealed class SaasRecomendacaoViewModel
{
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
}
