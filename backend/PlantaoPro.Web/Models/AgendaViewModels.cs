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
    public string Status { get; set; } = string.Empty;
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
