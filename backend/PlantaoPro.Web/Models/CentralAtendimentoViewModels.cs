namespace PlantaoPro.Web.Models;

public sealed class CentralAtendimentoViewModel
{
    public DateOnly Data { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string Status { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
    public string Paciente { get; set; } = string.Empty;
    public CentralAtendimentoKpiViewModel Kpis { get; set; } = new CentralAtendimentoKpiViewModel();
    public IEnumerable<CentralAtendimentoGrupoViewModel> Grupos { get; set; } = Array.Empty<CentralAtendimentoGrupoViewModel>();
    public string ErrorMessage { get; set; } = string.Empty;
}

public sealed class CentralAtendimentoKpiViewModel
{
    public int TotalAgendado { get; set; }
    public int Presentes { get; set; }
    public int Faltas { get; set; }
    public int PacientesAguardando { get; set; }
    public decimal TempoMedioEsperaMinutos { get; set; }
    public int TriagensPendentes { get; set; }
    public int ConsultasEmAndamento { get; set; }
    public int AtendimentosFinalizados { get; set; }
    public int AtrasosAcimaLimite { get; set; }
    public IDictionary<string, int> DistribuicaoPorStatus { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
}

public sealed class CentralAtendimentoGrupoViewModel
{
    public string Status { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public IEnumerable<CentralAtendimentoItemViewModel> Itens { get; set; } = Array.Empty<CentralAtendimentoItemViewModel>();
}

public sealed class CentralAtendimentoItemViewModel
{
    public Guid AgendamentoId { get; set; }
    public string Paciente { get; set; } = string.Empty;
    public string NomeSocial { get; set; } = string.Empty;
    public DateTime Horario { get; set; }
    public int TempoEsperaMinutos { get; set; }
    public string Medico { get; set; } = string.Empty;
    public string Especialidade { get; set; } = string.Empty;
    public string Unidade { get; set; } = string.Empty;
    public string Sala { get; set; } = string.Empty;
    public string Convenio { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
    public string ProximaAcaoPermitida { get; set; } = string.Empty;
}
