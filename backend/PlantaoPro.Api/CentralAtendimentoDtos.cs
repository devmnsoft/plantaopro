namespace PlantaoPro.Api;

public sealed class CentralAtendimentoFiltro
{
    public DateOnly? Data { get; set; }
    public Guid? UnidadeId { get; set; }
    public Guid? MedicoId { get; set; }
    public string? Especialidade { get; set; }
    public string? Status { get; set; }
    public string? Prioridade { get; set; }
    public string? Paciente { get; set; }
}

public sealed class CentralAtendimentoResumoDto
{
    public CentralAtendimentoKpiDto Kpis { get; set; } = new CentralAtendimentoKpiDto();
    public IEnumerable<CentralAtendimentoGrupoDto> Grupos { get; set; } = Array.Empty<CentralAtendimentoGrupoDto>();
}

public sealed class CentralAtendimentoKpiDto
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

public sealed class CentralAtendimentoGrupoDto
{
    public string Status { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public IEnumerable<CentralAtendimentoItemDto> Itens { get; set; } = Array.Empty<CentralAtendimentoItemDto>();
}

public sealed class CentralAtendimentoItemDto
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
