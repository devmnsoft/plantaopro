namespace PlantaoPro.Web.Models;

public class Saude360SpecificFormViewModel
{
    public Guid? Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string ApiEndpoint { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
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

public sealed class PacienteFormViewModel : Saude360SpecificFormViewModel { }
public sealed class AgendamentoFormViewModel : Saude360SpecificFormViewModel { }
public sealed class CheckInFormViewModel : Saude360SpecificFormViewModel { }
public sealed class TriagemFormViewModel : Saude360SpecificFormViewModel { }
public sealed class ConsultaAtendimentoViewModel : Saude360SpecificFormViewModel { }
public sealed class CidFormViewModel : Saude360SpecificFormViewModel { }
public sealed class CidImportacaoViewModel : Saude360SpecificFormViewModel { }
public sealed class PrescricaoFormViewModel : Saude360SpecificFormViewModel { }
public sealed class ContaReceberFormViewModel : Saude360SpecificFormViewModel { }
public sealed class RecebimentoFormViewModel : Saude360SpecificFormViewModel { }
public sealed class ConvenioFormViewModel : Saude360SpecificFormViewModel { }
public sealed class AutorizacaoConvenioFormViewModel : Saude360SpecificFormViewModel { }
public sealed class PlanoSaudeFormViewModel : Saude360SpecificFormViewModel { }
public sealed class PlanoSaudePacienteFormViewModel : Saude360SpecificFormViewModel { }
public sealed class PlantaoFormViewModel : Saude360SpecificFormViewModel { }
public sealed class EscalaFormViewModel : Saude360SpecificFormViewModel { }
public sealed class MedicoFormViewModel : Saude360SpecificFormViewModel { }
public sealed class HospitalFormViewModel : Saude360SpecificFormViewModel { }
public sealed class EspecialidadeFormViewModel : Saude360SpecificFormViewModel { }
