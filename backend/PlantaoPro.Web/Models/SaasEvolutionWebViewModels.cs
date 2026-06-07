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
