namespace PlantaoPro.Web.Models;

public sealed class Saude360RegistroViewModel
{
    public Guid Id { get; set; }
    public Guid? PacienteId { get; set; }
    public Guid? MedicoId { get; set; }
    public Guid? AgendamentoId { get; set; }
    public Guid? ConsultaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RegDate { get; set; }
}

public sealed class Saude360PageViewModel
{
    public string Titulo { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Permissao { get; set; } = string.Empty;
    public string Plano { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public IEnumerable<Saude360RegistroViewModel> Registros { get; set; } = Array.Empty<Saude360RegistroViewModel>();
    public IEnumerable<Saude360ActionLinkViewModel> Acoes { get; set; } = Array.Empty<Saude360ActionLinkViewModel>();
}

public sealed class Saude360ActionLinkViewModel
{
    public string Titulo { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Icone { get; set; } = string.Empty;
}

public sealed class Saude360FormViewModel
{
    public string Titulo { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ApiEndpoint { get; set; } = string.Empty;
    public Guid? Id { get; set; }
    public Guid? PacienteId { get; set; }
    public Guid? MedicoId { get; set; }
    public Guid? AgendamentoId { get; set; }
    public Guid? ConsultaId { get; set; }
    public Guid? PlanoSaudeId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string Justificativa { get; set; } = string.Empty;
    public string FormaPagamento { get; set; } = string.Empty;
    public string NumeroCarteirinha { get; set; } = string.Empty;
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public decimal? Valor { get; set; }
    public bool Principal { get; set; }
}
