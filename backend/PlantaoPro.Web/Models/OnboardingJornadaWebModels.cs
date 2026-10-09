namespace PlantaoPro.Web.Models;

/// <summary>
/// R5-C7/C8: jornada de onboarding materializada do catalogo canonicos (adaptada ao contrato
/// do cliente) e avaliada contra dados persistidos. A view nunca inventa etapas: se o onboarding
/// ainda nao existe, mostra o ponto de partida honesto ("Iniciar").
/// </summary>
public sealed class OnboardingJornadaWebViewModel
{
    public bool SemContextoCliente { get; set; }
    public string? InfoMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>Tenant autenticado ainda nao tem onboarding materializado (nunca mostrar lista fake).</summary>
    public bool SemOnboarding { get; set; }

    public string Status { get; set; } = string.Empty;
    public int Progresso { get; set; }
    public string ProximaAcao { get; set; } = string.Empty;

    /// <summary>Grupo 1 = "Implantacao geral"; depois um grupo por modulo contratado. Ja ordenado.</summary>
    public List<OnboardingGrupoWebModel> Grupos { get; set; } = new();
}

public sealed class OnboardingGrupoWebModel
{
    public bool Geral { get; set; }
    public string ModuloCodigo { get; set; } = string.Empty;
    public string ModuloNome { get; set; } = string.Empty;
    public List<OnboardingEtapaWebModel> Itens { get; set; } = new();
}

public sealed class OnboardingEtapaWebModel
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Objetivo { get; set; } = string.Empty;
    public string Responsavel { get; set; } = "CLIENTE";
    public string? PreRequisitoCodigo { get; set; }
    public bool PreRequisitoAtendido { get; set; } = true;
    public string ModuloCodigo { get; set; } = string.Empty;
    public string ModuloNome { get; set; } = string.Empty;
    public string CriterioCodigo { get; set; } = string.Empty;
    public string CriterioDescricao { get; set; } = string.Empty;
    public string Evidencia { get; set; } = string.Empty;
    public string LinkAcao { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public bool Obrigatorio { get; set; }
    public bool Atendida { get; set; }
    public bool Pulada { get; set; }
}

/// <summary>Espelho do payload de api/onboarding/status.</summary>
public sealed class OnboardingStatusWebModel
{
    public Guid TenantId { get; set; }
    public Guid ClienteId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Progresso { get; set; }
    public string ProximaAcao { get; set; } = string.Empty;
}
