namespace PlantaoPro.Web.Models;

public sealed class LgpdPoliticaViewModel
{
    public Guid Id { get; set; }
    public string Versao { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Conteudo { get; set; } = string.Empty;
    public DateTime VigenteDesde { get; set; }
}

public sealed class LgpdConsentimentoViewModel
{
    public Guid Id { get; set; }
    public string VersaoPolitica { get; set; } = string.Empty;
    public string Finalidade { get; set; } = string.Empty;
    public string BaseLegal { get; set; } = string.Empty;
    public bool Consentido { get; set; }
    public DateTime DataConsentimento { get; set; }
}

public sealed class LgpdSolicitacaoViewModel
{
    public Guid Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Resumo { get; set; } = string.Empty;
    public string Resposta { get; set; } = string.Empty;
    public DateTime RegDate { get; set; }
}

public sealed class LgpdEventoViewModel
{
    public Guid Id { get; set; }
    public string Evento { get; set; } = string.Empty;
    public string Entidade { get; set; } = string.Empty;
    public DateTime RegDate { get; set; }
}

public sealed class LgpdRetencaoViewModel
{
    public string Categoria { get; set; } = string.Empty;
    public string BaseLegal { get; set; } = string.Empty;
    public int PrazoMeses { get; set; }
    public string Observacoes { get; set; } = string.Empty;
}

public sealed class CriarSolicitacaoLgpdWebRequest
{
    public string Tipo { get; set; } = string.Empty;
    public string Resumo { get; set; } = string.Empty;
}

public sealed class JornadaClienteViewModel
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string Etapa { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Responsavel { get; set; } = string.Empty;
    public string ProximaAcao { get; set; } = string.Empty;
    public DateTime RegDate { get; set; }
}

public sealed class JornadaTarefaViewModel
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public DateTime? Prazo { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class FunilItemViewModel
{
    public string Etapa { get; set; } = string.Empty;
    public long Quantidade { get; set; }
}

public sealed class MudarEtapaJornadaWebRequest
{
    public string Motivo { get; set; } = string.Empty;
    public string? ProximaAcao { get; set; }
    public string? Responsavel { get; set; }
}

public sealed class CriarTarefaJornadaWebRequest
{
    public Guid ClienteId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Responsavel { get; set; }
    public DateTime? Prazo { get; set; }
}

public sealed class ComercialLeadViewModel
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Origem { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int MedicosDesejados { get; set; }
    public int Hospitais { get; set; }
    public int PlantoesMes { get; set; }
}

public sealed class ComercialLeadWebRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Origem { get; set; }
    public int MedicosDesejados { get; set; }
    public int Hospitais { get; set; }
    public int PlantoesMes { get; set; }
    public bool PrecisaMobile { get; set; }
    public bool PrecisaBi { get; set; }
    public bool SuportePrioritario { get; set; }
    public bool OperacaoAssistida { get; set; }
}

public sealed class ComercialOportunidadeViewModel
{
    public Guid Id { get; set; }
    public Guid? LeadId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Etapa { get; set; } = string.Empty;
    public decimal ValorEstimado { get; set; }
    public string PlanoRecomendado { get; set; } = string.Empty;
    public int Probabilidade { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class ComercialOportunidadeWebRequest
{
    public Guid? LeadId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public decimal ValorEstimado { get; set; }
    public string? PlanoRecomendado { get; set; }
    public int Probabilidade { get; set; } = 10;
}

public sealed class ComercialPropostaViewModel
{
    public Guid Id { get; set; }
    public Guid OportunidadeId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string PlanoNome { get; set; } = string.Empty;
    public decimal ValorMensal { get; set; }
    public decimal DescontoPercentual { get; set; }
    public DateTime Validade { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class ComercialPropostaWebRequest
{
    public Guid OportunidadeId { get; set; }
    public string PlanoNome { get; set; } = string.Empty;
    public decimal ValorMensal { get; set; }
    public decimal DescontoPercentual { get; set; }
    public DateTime Validade { get; set; }
    public string? Observacoes { get; set; }
}

public sealed class AjudaTopicoViewModel
{
    public Guid Id { get; set; }
    public string Perfil { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
}

public sealed class AjudaArtigoViewModel
{
    public Guid Id { get; set; }
    public string Perfil { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Resumo { get; set; } = string.Empty;
    public string Conteudo { get; set; } = string.Empty;
    public string LinkAcao { get; set; } = string.Empty;
}

public sealed class AjudaChecklistViewModel
{
    public string Perfil { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string LinkAcao { get; set; } = string.Empty;
}
