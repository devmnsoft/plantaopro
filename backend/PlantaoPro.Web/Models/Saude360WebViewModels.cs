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
    public string TipoAtendimento { get; set; } = string.Empty;
    public string Convenio { get; set; } = string.Empty;
    public string Sala { get; set; } = string.Empty;
    public string ProfissionalNome { get; set; } = string.Empty;
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

    public IEnumerable<string> ValidarTriagem()
    {
        if (string.IsNullOrWhiteSpace(ClassificacaoRisco)) yield return "Selecione a classificação de risco.";
        if (PressaoSistolica is < 50 or > 260 || PressaoDiastolica is < 30 or > 160)
            yield return "Informe uma pressão arterial plausível (PAS 50–260 e PAD 30–160 mmHg).";
        if (Temperatura is < 30 or > 45) yield return "Informe uma temperatura entre 30 e 45 °C.";
        if (Saturacao is < 50 or > 100) yield return "Informe uma saturação entre 50% e 100%.";
        if (FrequenciaCardiaca is < 20 or > 250) yield return "Informe uma frequência cardíaca entre 20 e 250 bpm.";
        if ((ClassificacaoRisco == "EMERGENCIA" || ClassificacaoRisco == "MUITO_URGENTE") && string.IsNullOrWhiteSpace(Descricao))
            yield return "Registre uma observação clínica para classificações de alto risco.";
    }
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
    public string HelpText { get; set; } = "Busque registros reais do tenant pela descrição; identificadores técnicos não são solicitados.";
}

public sealed class AutocompleteFieldViewModel
{
    public string FieldName { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Placeholder { get; set; } = string.Empty;
}
