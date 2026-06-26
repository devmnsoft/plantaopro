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
    public string Cpf { get; set; } = string.Empty;
    public string Cns { get; set; } = string.Empty;
    public string DocumentoAlternativo { get; set; } = string.Empty;
    public string NomeSocial { get; set; } = string.Empty;
    public string SexoGenero { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string ResponsavelNome { get; set; } = string.Empty;
    public string Especialidade { get; set; } = string.Empty;
    public string ClassificacaoRisco { get; set; } = string.Empty;
    public string QueixaPrincipal { get; set; } = string.Empty;
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public DateOnly? DataNascimento { get; set; }
    public decimal? PressaoSistolica { get; set; }
    public decimal? PressaoDiastolica { get; set; }
    public decimal? FrequenciaCardiaca { get; set; }
    public decimal? FrequenciaRespiratoria { get; set; }
    public decimal? Temperatura { get; set; }
    public decimal? Saturacao { get; set; }
    public decimal? Peso { get; set; }
    public decimal? Altura { get; set; }
    public decimal? Valor { get; set; }
    public bool Principal { get; set; }
}

public sealed class FriendlyErrorViewModel
{
    public string Title { get; set; } = "Não foi possível carregar esta tela";
    public string Message { get; set; } = string.Empty;
    public string ActionText { get; set; } = "Voltar ao fluxo";
    public string Controller { get; set; } = "ClinicaDashboard";
    public string Action { get; set; } = "FluxoAtendimento";
}

public sealed class PageHelpViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string WhenToUse { get; set; } = string.Empty;
    public IEnumerable<string> StepByStep { get; set; } = Array.Empty<string>();
    public IEnumerable<string> Tips { get; set; } = Array.Empty<string>();
    public IEnumerable<Saude360ActionLinkViewModel> RelatedActions { get; set; } = Array.Empty<Saude360ActionLinkViewModel>();
    public string ProfileNotes { get; set; } = string.Empty;
    public string DocumentationUrl { get; set; } = string.Empty;
}

public sealed class LookupSelectViewModel
{
    public string FieldName { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Placeholder { get; set; } = string.Empty;
    public string HelpText { get; set; } = "Digite para buscar registros reais do tenant; nenhum GUID precisa ser digitado.";
}

public sealed class AutocompleteFieldViewModel
{
    public string FieldName { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Placeholder { get; set; } = string.Empty;
}
