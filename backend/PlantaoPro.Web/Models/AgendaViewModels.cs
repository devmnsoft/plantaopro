namespace PlantaoPro.Web.Models;

public class AgendaOperacionalViewModel
{
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public string? Status { get; set; }
    public IEnumerable<PlantaoResumoDto> Itens { get; set; } = Array.Empty<PlantaoResumoDto>();
    public long Total { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class AgendaClinicaViewModel
{
    public string Titulo { get; set; } = "Agenda clínica premium";
    public string Endpoint { get; set; } = "api/agendamentos";
    public string Fonte { get; set; } = "Real";
    public string ErrorMessage { get; set; } = string.Empty;
    public AgendaClinicaFiltroViewModel Filtro { get; set; } = new AgendaClinicaFiltroViewModel();
    public IEnumerable<AgendaClinicaItemViewModel> Itens { get; set; } = Array.Empty<AgendaClinicaItemViewModel>();
    public IEnumerable<AgendaStatusBadgeViewModel> StatusCards { get; set; } = Array.Empty<AgendaStatusBadgeViewModel>();
}

public sealed class AgendaClinicaItemViewModel
{
    public Guid Id { get; set; }
    public DateTime Horario { get; set; }
    public string Paciente { get; set; } = string.Empty;
    public string Medico { get; set; } = string.Empty;
    public string Especialidade { get; set; } = string.Empty;
    public string Unidade { get; set; } = string.Empty;
    public string TipoAtendimento { get; set; } = string.Empty;
    public string Convenio { get; set; } = string.Empty;
    public string Sala { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    private string StatusNormalizado => Status.Trim().ToUpperInvariant();
    public bool PodeConfirmar => StatusNormalizado.Contains("AGEND");
    public bool PodeCheckIn => StatusNormalizado.Contains("AGEND") || StatusNormalizado.Contains("CONFIRM");
    public bool PodeAbrirTriagem => StatusNormalizado.Contains("CHECK") || StatusNormalizado.Contains("AGUARD");
    public bool PodeAlterarAgenda => !string.IsNullOrWhiteSpace(Status)
        && !StatusNormalizado.Contains("CANCEL")
        && !StatusNormalizado.Contains("FINAL");

    public string TempoEspera
    {
        get
        {
            if (Horario == default || !StatusIndicaEspera()) return "Não aplicável ao status atual";
            var elapsed = DateTime.Now - Horario;
            if (elapsed <= TimeSpan.Zero) return "Atendimento ainda não previsto";
            return elapsed.TotalHours >= 1
                ? $"{(int)elapsed.TotalHours}h {elapsed.Minutes}min desde o horário"
                : $"{Math.Max(1, elapsed.Minutes)} min desde o horário";
        }
    }

    public string Atraso
    {
        get
        {
            if (Horario == default || !StatusIndicaEspera()) return "Não identificado";
            var elapsed = DateTime.Now - Horario;
            return elapsed > TimeSpan.Zero ? $"{Math.Max(1, (int)elapsed.TotalMinutes)} min" : "Sem atraso";
        }
    }

    public string ProximaAcao
    {
        get
        {
            var normalized = StatusNormalizado;
            if (normalized.Contains("AGEND")) return "Confirmar ou realizar check-in";
            if (normalized.Contains("CONFIRM")) return "Realizar check-in";
            if (normalized.Contains("CHECK") || normalized.Contains("AGUARD")) return "Abrir triagem";
            return "Revise as ações permitidas para o status";
        }
    }

    private bool StatusIndicaEspera()
    {
        var normalized = Status.Trim().ToUpperInvariant();
        return normalized.Contains("CHECK") || normalized.Contains("AGUARD");
    }
}

public sealed class AgendaClinicaFiltroViewModel
{
    public DateTime? Data { get; set; }
    public string Medico { get; set; } = string.Empty;
    public string Unidade { get; set; } = string.Empty;
    public string Especialidade { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class AgendaStatusBadgeViewModel
{
    public string Status { get; set; } = string.Empty;
    public int Total { get; set; }
    public string CssClass { get; set; } = "bg-secondary";
}
