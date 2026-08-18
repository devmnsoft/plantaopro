namespace PlantaoPro.Api.Models;

public static class FechamentoStatus
{
    public const string Aberto = "ABERTO"; public const string EmConferencia = "EM_CONFERENCIA";
    public const string ComDivergencia = "COM_DIVERGENCIA"; public const string AguardandoAprovacao = "AGUARDANDO_APROVACAO";
    public const string Aprovado = "APROVADO"; public const string Devolvido = "DEVOLVIDO";
    public const string FinanceiroGerado = "FINANCEIRO_GERADO"; public const string Concluido = "CONCLUIDO";
}
public class FechamentoResumoDto { public Guid Id { get; set; } public Guid PlantaoId { get; set; } public string Status { get; set; } = ""; public DateTime DataReferencia { get; set; } public string Hospital { get; set; } = ""; public string Especialidade { get; set; } = ""; public decimal ValorPrevisto { get; set; } public decimal ValorApurado { get; set; } public decimal HorasPrevistas { get; set; } public decimal HorasRealizadas { get; set; } public int QuantidadeEscalas { get; set; } public int DivergenciasAbertas { get; set; } }
public sealed class FechamentoDetalheDto : FechamentoResumoDto { public IReadOnlyList<FechamentoItemDto> Itens { get; set; } = []; public IReadOnlyList<FechamentoDivergenciaDto> Divergencias { get; set; } = []; }
public sealed class FechamentoItemDto { public Guid Id { get; set; } public Guid EscalaId { get; set; } public Guid MedicoId { get; set; } public string Medico { get; set; } = ""; public string Crm { get; set; } = ""; public string StatusEscala { get; set; } = ""; public decimal HorasPrevistas { get; set; } public decimal HorasRealizadas { get; set; } public decimal ValorPrevisto { get; set; } public decimal ValorApurado { get; set; } public Guid? PagamentoId { get; set; } }
public sealed class FechamentoDivergenciaDto { public Guid Id { get; set; } public Guid? FechamentoItemId { get; set; } public string Tipo { get; set; } = ""; public string Descricao { get; set; } = ""; public string Status { get; set; } = ""; public string? Resolucao { get; set; } public DateTime CriadoEm { get; set; } }
public sealed class FechamentoTimelineDto { public Guid Id { get; set; } public string StatusAnterior { get; set; } = ""; public string StatusNovo { get; set; } = ""; public string Acao { get; set; } = ""; public string? Motivo { get; set; } public DateTime CriadoEm { get; set; } }
public sealed record CriarDivergenciaRequest(Guid? FechamentoItemId, string Tipo, string Descricao, decimal? ValorAnterior, decimal? ValorProposto, string? Motivo);
public sealed record ResolverDivergenciaRequest(string Resolucao);
public sealed record DevolverFechamentoRequest(string Motivo);
public sealed record ResolverContestacaoPagamentoRequest(string Decisao, string Justificativa, decimal? NovoValor);

