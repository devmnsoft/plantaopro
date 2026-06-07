namespace PlantaoPro.Api.Models;

public sealed class LgpdPoliticaDto
{
    public Guid Id { get; set; }
    public string Versao { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Conteudo { get; set; } = string.Empty;
    public DateTime VigenteDesde { get; set; }
}

public sealed class LgpdConsentimentoDto
{
    public Guid Id { get; set; }
    public Guid? UsuarioId { get; set; }
    public string Finalidade { get; set; } = string.Empty;
    public string BaseLegal { get; set; } = string.Empty;
    public string VersaoPolitica { get; set; } = string.Empty;
    public bool Consentido { get; set; }
    public string Ip { get; set; } = string.Empty;
    public DateTime RegDate { get; set; }
}

public sealed class RegistrarConsentimentoRequest
{
    public string Finalidade { get; set; } = string.Empty;
    public string BaseLegal { get; set; } = string.Empty;
    public string VersaoPolitica { get; set; } = string.Empty;
    public bool Consentido { get; set; } = true;
}

public sealed class LgpdSolicitacaoDto
{
    public Guid Id { get; set; }
    public Guid? UsuarioId { get; set; }
    public Guid? ClienteId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Resposta { get; set; } = string.Empty;
    public DateTime RegDate { get; set; }
    public DateTime? RespondidaEm { get; set; }
}

public sealed class CriarSolicitacaoLgpdRequest
{
    public string Tipo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
}

public sealed class ResponderSolicitacaoLgpdRequest
{
    public string Resposta { get; set; } = string.Empty;
    public string Status { get; set; } = "RESPONDIDA";
}

public sealed class LgpdEventoDto
{
    public Guid Id { get; set; }
    public Guid? UsuarioId { get; set; }
    public string Acao { get; set; } = string.Empty;
    public string Detalhes { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public DateTime RegDate { get; set; }
}

public sealed class JornadaClienteDto
{
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string Etapa { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public string ProximaAcao { get; set; } = string.Empty;
    public DateTime RegDate { get; set; }
}

public sealed class JornadaEventoDto
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Resumo { get; set; } = string.Empty;
    public DateTime RegDate { get; set; }
}

public sealed class JornadaTarefaDto
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? Vencimento { get; set; }
}

public sealed class MudarEtapaJornadaRequest
{
    public string Motivo { get; set; } = string.Empty;
}

public sealed class CriarJornadaEventoRequest
{
    public string Tipo { get; set; } = string.Empty;
    public string Resumo { get; set; } = string.Empty;
}

public sealed class CriarJornadaTarefaRequest
{
    public string Titulo { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public DateTime? Vencimento { get; set; }
}

public sealed class FunilEtapaDto
{
    public string Etapa { get; set; } = string.Empty;
    public long Total { get; set; }
}

public sealed class ComercialLeadDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RegDate { get; set; }
}

public sealed class ComercialLeadRequest
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

public sealed class ComercialOportunidadeDto
{
    public Guid Id { get; set; }
    public Guid? LeadId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Etapa { get; set; } = string.Empty;
    public decimal ValorEstimado { get; set; }
    public string PlanoRecomendado { get; set; } = string.Empty;
    public DateTime RegDate { get; set; }
}

public sealed class ComercialOportunidadeRequest
{
    public Guid? LeadId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal ValorEstimado { get; set; }
    public string PlanoRecomendado { get; set; } = string.Empty;
}

public sealed class ComercialPropostaDto
{
    public Guid Id { get; set; }
    public Guid OportunidadeId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public decimal ValorTotal { get; set; }
    public decimal DescontoPercentual { get; set; }
    public DateTime Validade { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class ComercialPropostaRequest
{
    public Guid OportunidadeId { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal DescontoPercentual { get; set; }
    public DateTime Validade { get; set; }
}

public sealed class StatusComMotivoRequest
{
    public string Motivo { get; set; } = string.Empty;
}

public sealed class SugerirPlanoRequest
{
    public int MedicosDesejados { get; set; }
    public int HospitaisDesejados { get; set; }
    public int PlantoesMes { get; set; }
    public bool PrecisaMobile { get; set; }
    public bool PrecisaBi { get; set; }
    public bool SuportePrioritario { get; set; }
    public bool OperacaoAssistida { get; set; }
}

public sealed class PlanoSugeridoDto
{
    public string Plano { get; set; } = string.Empty;
    public string Justificativa { get; set; } = string.Empty;
    public decimal Score { get; set; }
}

public sealed class AjudaTopicoDto
{
    public Guid Id { get; set; }
    public string Perfil { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
}

public sealed class AjudaArtigoDto
{
    public Guid Id { get; set; }
    public Guid TopicoId { get; set; }
    public string Perfil { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Conteudo { get; set; } = string.Empty;
    public string LinkAcao { get; set; } = string.Empty;
}

public sealed class AjudaFeedbackRequest
{
    public bool Util { get; set; }
    public string Comentario { get; set; } = string.Empty;
}
