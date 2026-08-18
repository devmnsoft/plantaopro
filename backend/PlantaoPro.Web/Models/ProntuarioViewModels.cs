namespace PlantaoPro.Web.Models;

public sealed class ProntuarioViewModel
{
    public PacienteProntuarioViewModel Paciente { get; set; } = new();
    public ResumoProntuarioViewModel Resumo { get; set; } = new();
    public IReadOnlyList<TimelineProntuarioViewModel> Timeline { get; set; } = Array.Empty<TimelineProntuarioViewModel>();
    public string? Erro { get; set; }
}
public sealed class PacienteProntuarioViewModel { public Guid Id { get; set; } public string Nome { get; set; }=""; public string? NomeSocial { get; set; } public DateOnly? DataNascimento { get; set; } public string? Unidade { get; set; } public string? Convenio { get; set; } }
public sealed class ResumoProntuarioViewModel { public IReadOnlyList<ProblemaProntuarioViewModel> ProblemasAtivos { get; set; }=Array.Empty<ProblemaProntuarioViewModel>(); public IReadOnlyList<AlergiaProntuarioViewModel> Alergias { get; set; }=Array.Empty<AlergiaProntuarioViewModel>(); public IReadOnlyList<MedicamentoProntuarioViewModel> MedicamentosEmUso { get; set; }=Array.Empty<MedicamentoProntuarioViewModel>(); }
public sealed class ProblemaProntuarioViewModel { public string Descricao { get; set; }=""; public string Status { get; set; }=""; }
public sealed class AlergiaProntuarioViewModel { public string Substancia { get; set; }=""; public string Gravidade { get; set; }=""; public bool Confirmada { get; set; } }
public sealed class MedicamentoProntuarioViewModel { public string MedicamentoDescricao { get; set; }=""; public string? Dose { get; set; } public string? Frequencia { get; set; } }
public sealed class TimelineProntuarioViewModel { public Guid Id { get; set; } public string Tipo { get; set; }=""; public DateTime Data { get; set; } public string Titulo { get; set; }=""; public string ResumoSeguro { get; set; }=""; public string? Profissional { get; set; } public string? Unidade { get; set; } }
