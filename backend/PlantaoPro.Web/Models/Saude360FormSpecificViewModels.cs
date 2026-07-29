using System.ComponentModel.DataAnnotations;

namespace PlantaoPro.Web.Models;

public sealed class PacienteFormViewModel
{
    public Guid? Id { get; set; }
    [Required(ErrorMessage = "Informe o nome."), StringLength(200)] public string Nome { get; set; } = string.Empty;
    [StringLength(200)] public string NomeSocial { get; set; } = string.Empty;
    [Required(ErrorMessage = "Informe a data de nascimento.")] public DateOnly? DataNascimento { get; set; }
    public string SexoGenero { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Cns { get; set; } = string.Empty;
    public string DocumentoAlternativo { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")] public string Email { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string ResponsavelNome { get; set; } = string.Empty;
    [Range(typeof(bool), "true", "true", ErrorMessage = "Registre o consentimento LGPD.")] public bool ConsentimentoLgpd { get; set; }
}

public sealed class AgendamentoFormViewModel
{
    public Guid? Id { get; set; }
    [Required] public Guid? PacienteId { get; set; }
    [Required] public Guid? MedicoId { get; set; }
    [Required] public Guid? UnidadeId { get; set; }
    public Guid? SalaId { get; set; }
    [Required] public DateTime? DataInicio { get; set; }
    [Required] public DateTime? DataFim { get; set; }
    public string Tipo { get; set; } = "CONSULTA";
    public string Especialidade { get; set; } = string.Empty;
    public string Observacoes { get; set; } = string.Empty;
}

public sealed class CheckInFormViewModel { [Required] public Guid? AgendamentoId { get; set; } }
public sealed class TriagemFormViewModel : Saude360SpecificFormViewModel { }
public sealed class ConsultaAtendimentoViewModel : Saude360SpecificFormViewModel { }
public sealed class CidFormViewModel : Saude360SpecificFormViewModel { }
public sealed class CidImportacaoViewModel : Saude360SpecificFormViewModel { }
public sealed class PrescricaoFormViewModel : Saude360SpecificFormViewModel { }
public sealed class ContaReceberFormViewModel : Saude360SpecificFormViewModel { }
public sealed class RecebimentoFormViewModel : Saude360SpecificFormViewModel { }
public sealed class ConvenioFormViewModel : Saude360SpecificFormViewModel { }
public sealed class AutorizacaoConvenioViewModel : Saude360SpecificFormViewModel { }
public sealed class PlanoSaudeFormViewModel : Saude360SpecificFormViewModel { }
public sealed class PlanoSaudePacienteFormViewModel : Saude360SpecificFormViewModel { }
public sealed class EscalaFormViewModel : Saude360SpecificFormViewModel { }

public class Saude360SpecificFormViewModel
{
    public Guid? Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public Guid? PacienteId { get; set; }
    public Guid? MedicoId { get; set; }
    public Guid? AgendamentoId { get; set; }
    public Guid? ConsultaId { get; set; }
    public Guid? PlanoSaudeId { get; set; }
    public IEnumerable<LookupSelectViewModel> Lookups { get; set; } = Array.Empty<LookupSelectViewModel>();
    public string PagePurpose { get; set; } = string.Empty;
    public string NextStep { get; set; } = string.Empty;
}
