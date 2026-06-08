namespace PlantaoPro.Web.Models;

public sealed class SimpleCardViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
}

public sealed class LgpdSolicitacaoFormViewModel
{
    public string Tipo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
}

public sealed class JornadaClienteFormViewModel
{
    public Guid ClienteId { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

public sealed class ComercialLeadFormViewModel
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public int MedicosDesejados { get; set; }
    public int HospitaisDesejados { get; set; }
    public int PlantoesMes { get; set; }
    public bool PrecisaMobile { get; set; }
    public bool PrecisaBi { get; set; }
    public bool SuportePrioritario { get; set; }
    public bool OperacaoAssistida { get; set; }
}

public sealed class JornadaClienteResumoViewModel
{
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string Etapa { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public string ProximaAcao { get; set; } = string.Empty;
    public DateTime RegDate { get; set; }
}

public sealed class JornadaClienteEventoViewModel
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Resumo { get; set; } = string.Empty;
    public DateTime RegDate { get; set; }
}

public sealed class JornadaClienteTarefaViewModel
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? Vencimento { get; set; }
}

public sealed class JornadaClienteDetalheViewModel
{
    public JornadaClienteResumoViewModel Jornada { get; set; } = new JornadaClienteResumoViewModel();
    public IEnumerable<JornadaClienteEventoViewModel> Eventos { get; set; } = Array.Empty<JornadaClienteEventoViewModel>();
    public IEnumerable<JornadaClienteTarefaViewModel> Tarefas { get; set; } = Array.Empty<JornadaClienteTarefaViewModel>();
}

public sealed class JornadaClientesIndexViewModel
{
    public IEnumerable<JornadaClienteResumoViewModel> Jornadas { get; set; } = Array.Empty<JornadaClienteResumoViewModel>();
    public string? ErrorMessage { get; set; }
}

public sealed class JornadaClientesFunilViewModel
{
    public IEnumerable<FunilEtapaViewModel> Etapas { get; set; } = Array.Empty<FunilEtapaViewModel>();
    public string? ErrorMessage { get; set; }
}

public sealed class FunilEtapaViewModel
{
    public string Etapa { get; set; } = string.Empty;
    public long Total { get; set; }
}

public sealed class CriarJornadaEventoFormViewModel
{
    public Guid ClienteId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Resumo { get; set; } = string.Empty;
}

public sealed class CriarJornadaTarefaFormViewModel
{
    public Guid ClienteId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public DateTime? Vencimento { get; set; }
}

public sealed class ClienteInteligenciaPageViewModel
{
    public Guid ClienteId { get; set; }
    public ClienteSaudeSaasViewModel Saude { get; set; } = new ClienteSaudeSaasViewModel();
    public UsoPlanoViewModel Uso { get; set; } = new UsoPlanoViewModel();
    public IEnumerable<ClienteAlertaSaasViewModel> Alertas { get; set; } = Array.Empty<ClienteAlertaSaasViewModel>();
    public string? ErrorMessage { get; set; }
}
