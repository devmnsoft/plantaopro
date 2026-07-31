using System.ComponentModel.DataAnnotations;

namespace PlantaoPro.Api.Clinical;

public enum ConsultaStatus { AGUARDANDO, EM_ATENDIMENTO, RASCUNHO, FINALIZADA, CANCELADA, RETORNO_SOLICITADO }
public enum PrescricaoStatus { RASCUNHO, FINALIZADA, CANCELADA, SUBSTITUIDA }
public enum TipoFaturamentoAssistencial { PARTICULAR, CONVENIO, PLANO_SAUDE, CORTESIA, ISENTO, FATURAMENTO_POSTERIOR }

public sealed class Consulta
{
    public Guid Id { get; set; } public Guid ClienteId { get; set; } public Guid UnidadeId { get; set; }
    public Guid AtendimentoId { get; set; } public Guid? AgendamentoId { get; set; } public Guid PacienteId { get; set; }
    public Guid MedicoId { get; set; } public Guid? TriagemId { get; set; } public ConsultaStatus Status { get; set; }
    public string Anamnese { get; set; } = ""; public string ExameFisico { get; set; } = "";
    public string HipoteseDiagnostica { get; set; } = ""; public string Diagnostico { get; set; } = "";
    public string Conduta { get; set; } = ""; public string Orientacoes { get; set; } = ""; public string Observacoes { get; set; } = "";
    public DateTime? InicioEm { get; set; } public DateTime? FinalizadaEm { get; set; } public DateTime? CanceladaEm { get; set; }
    public string? MotivoCancelamento { get; set; } public int Versao { get; set; }
    public Guid? CreatedBy { get; set; } public Guid? UpdatedBy { get; set; } public DateTime RegDate { get; set; }
    public DateTime? RegUpdate { get; set; } public string RegStatus { get; set; } = "A";
}

public sealed class ConsultaCid { public Guid Id { get; set; } public Guid ClienteId { get; set; } public Guid ConsultaId { get; set; } public Guid CidId { get; set; } public string Codigo { get; set; } = ""; public string Descricao { get; set; } = ""; public string Tipo { get; set; } = "SECUNDARIO"; public bool Principal { get; set; } public int Ordem { get; set; } }
public sealed class ConsultaSolicitacaoExame { public Guid Id { get; set; } public Guid ConsultaId { get; set; } public string Exame { get; set; } = ""; public string? IndicacaoClinica { get; set; } public string Prioridade { get; set; } = "ROTINA"; public DateTime RegDate { get; set; } }
public sealed class ConsultaEncaminhamento { public Guid Id { get; set; } public Guid ConsultaId { get; set; } public string Especialidade { get; set; } = ""; public string Motivo { get; set; } = ""; public DateTime RegDate { get; set; } }
public sealed class ConsultaHistorico { public Guid Id { get; set; } public Guid ConsultaId { get; set; } public string Evento { get; set; } = ""; public int Versao { get; set; } public DateTime RegDate { get; set; } }

public sealed class Prescricao { public Guid Id { get; set; } public Guid ClienteId { get; set; } public Guid UnidadeId { get; set; } public Guid ConsultaId { get; set; } public Guid PacienteId { get; set; } public Guid MedicoId { get; set; } public PrescricaoStatus Status { get; set; } public string OrientacoesGerais { get; set; } = ""; public int Versao { get; set; } public DateTime? FinalizadaEm { get; set; } public IReadOnlyList<PrescricaoItem> Itens { get; set; } = Array.Empty<PrescricaoItem>(); }
public sealed class PrescricaoItem { public Guid Id { get; set; } public Guid PrescricaoId { get; set; } public string MedicamentoNome { get; set; } = ""; public string? PrincipioAtivo { get; set; } public string? Apresentacao { get; set; } public string? Concentracao { get; set; } public string Dose { get; set; } = ""; public string UnidadeDose { get; set; } = ""; public string ViaAdministracao { get; set; } = ""; public string Frequencia { get; set; } = ""; public string Duracao { get; set; } = ""; public decimal Quantidade { get; set; } public string? Instrucoes { get; set; } public bool UsoContinuo { get; set; } public int Ordem { get; set; } }

public record IniciarConsultaRequest(int Versao);
public record SalvarConsultaRascunhoRequest([Required] int Versao, string? Anamnese, string? ExameFisico, string? HipoteseDiagnostica, string? Diagnostico, string? Conduta, string? Orientacoes, string? Observacoes);
public record FinalizarConsultaRequest([Required] int Versao, TipoFaturamentoAssistencial TipoFaturamento, decimal ValorBruto = 0, decimal Desconto = 0, decimal Coparticipacao = 0, string? Justificativa = null);
public record CancelarConsultaRequest([Required, MinLength(10)] string Motivo, int Versao);
public record ReabrirConsultaRequest([Required, MinLength(10)] string Justificativa, int Versao);
public record AdicionarConsultaCidRequest(Guid CidId, bool Principal, string Tipo = "SECUNDARIO");
public record CriarSolicitacaoExameRequest([Required] string Exame, string? IndicacaoClinica, string Prioridade = "ROTINA");
public record CriarEncaminhamentoRequest([Required] string Especialidade, [Required] string Motivo);
public record SolicitarRetornoRequest(DateOnly DataSugerida, [Required] string Motivo);
public record CriarPrescricaoRequest(string? OrientacoesGerais);
public record SalvarPrescricaoRequest(int Versao, string? OrientacoesGerais);
public record CriarPrescricaoItemRequest([Required] string MedicamentoNome, string? PrincipioAtivo, string? Apresentacao, string? Concentracao, [Required] string Dose, [Required] string UnidadeDose, [Required] string ViaAdministracao, [Required] string Frequencia, [Required] string Duracao, decimal Quantidade, string? Instrucoes, bool UsoContinuo, bool ConfirmacaoAlertaAlergia = false);

public sealed class ConsultaWorkspaceResponse { public Consulta Consulta { get; set; } = new(); public string PacienteNome { get; set; } = ""; public string? NomeSocial { get; set; } public DateOnly? DataNascimento { get; set; } public string? SexoGenero { get; set; } public string? Alergias { get; set; } public string? Plano { get; set; } public string Unidade { get; set; } = ""; public string? ClassificacaoRisco { get; set; } public DateTime? ChegadaEm { get; set; } public IReadOnlyList<ConsultaCid> Cids { get; set; } = Array.Empty<ConsultaCid>(); public Prescricao? Prescricao { get; set; } }
public record ConsultaResumoResponse(Guid Id, Guid PacienteId, string PacienteNome, string Status, string? ClassificacaoRisco, DateTime ChegadaEm, int TempoEsperaMinutos);
public record ConsultaPendenciasFinalizacaoResponse(IReadOnlyList<string> Impeditivas, IReadOnlyList<string> Alertas, decimal CobrancaPrevista) { public bool PodeFinalizar => Impeditivas.Count == 0; }

public static class ConsultaStateMachine
{
    private static readonly HashSet<(ConsultaStatus, ConsultaStatus)> Transicoes = new() { (ConsultaStatus.AGUARDANDO, ConsultaStatus.EM_ATENDIMENTO), (ConsultaStatus.EM_ATENDIMENTO, ConsultaStatus.RASCUNHO), (ConsultaStatus.RASCUNHO, ConsultaStatus.RASCUNHO), (ConsultaStatus.RASCUNHO, ConsultaStatus.FINALIZADA), (ConsultaStatus.EM_ATENDIMENTO, ConsultaStatus.FINALIZADA), (ConsultaStatus.AGUARDANDO, ConsultaStatus.CANCELADA), (ConsultaStatus.EM_ATENDIMENTO, ConsultaStatus.CANCELADA), (ConsultaStatus.RASCUNHO, ConsultaStatus.CANCELADA), (ConsultaStatus.FINALIZADA, ConsultaStatus.RETORNO_SOLICITADO) };
    public static bool PodeTransicionar(ConsultaStatus atual, ConsultaStatus destino) => Transicoes.Contains((atual, destino));
    public static void Validar(ConsultaStatus atual, ConsultaStatus destino) { if (!PodeTransicionar(atual, destino)) throw new InvalidOperationException($"Transição de {atual} para {destino} não é permitida."); }
}

public static class AtendimentoBillingService
{
    public static decimal CalcularValorLiquido(decimal bruto, decimal desconto, decimal coparticipacao)
    {
        if (bruto < 0 || desconto < 0 || coparticipacao < 0) throw new ArgumentOutOfRangeException(nameof(bruto), "Valores de cobrança não podem ser negativos.");
        var liquido = bruto - desconto + coparticipacao;
        if (liquido < 0) throw new InvalidOperationException("O valor líquido da cobrança não pode ser negativo.");
        return liquido;
    }
}
