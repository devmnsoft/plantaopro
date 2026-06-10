namespace PlantaoPro.Api.Models;

public record MedicoDisponibilidadeRequest(DateTime DataInicio, DateTime DataFim, string Turno, Guid? HospitalId, Guid? EspecialidadeId, string? Observacoes);
public record MedicoIndisponibilidadeRequest(DateTime DataInicio, DateTime DataFim, string Motivo);
public record MedicoPreferenciasRequest(Guid[]? HospitaisPreferidos, Guid[]? EspecialidadesPreferidas, string[]? TurnosPreferidos, int LimitePlantoesSemana, int LimitePlantoesMes, string? Observacoes);
public record SugestaoFeedbackRequest(Guid? MedicoId, string Feedback, string? Observacao);
public record SolicitarSubstituicaoRequest(Guid PlantaoId, Guid? EscalaId, string Motivo);
public record DecisaoSubstituicaoRequest(string Justificativa);
public record ConvidarSubstitutoRequest(Guid MedicoId, string? Mensagem);
public record ConfirmarSubstitutoRequest(Guid MedicoId, string? Observacao);
public record CriarPendenciaRequest(string Tipo, string Titulo, string Descricao, string Prioridade, DateTime? Prazo, Guid? ResponsavelUsuarioId, string? Entidade, Guid? EntidadeId);
public record AtribuirPendenciaRequest(Guid ResponsavelUsuarioId, string? Observacao);
public record ResolverPendenciaRequest(string Observacao);
public record AdiarPendenciaRequest(DateTime NovoPrazo, string Observacao);
public record PreferenciaNotificacaoRequest(string TipoEvento, bool InApp, bool Email, bool Push, bool Whatsapp);
public record SalvarFiltroRelatorioRequest(string Nome, string Tipo, IDictionary<string, string>? Filtros);

public sealed class MedicoDisponibilidadeDto
{
    public Guid Id { get; set; }
    public Guid MedicoId { get; set; }
    public Guid? HospitalId { get; set; }
    public string HospitalNome { get; set; } = string.Empty;
    public Guid? EspecialidadeId { get; set; }
    public string EspecialidadeNome { get; set; } = string.Empty;
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public string Turno { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Observacoes { get; set; } = string.Empty;
}

public sealed class MedicoPreferenciasDto
{
    public Guid Id { get; set; }
    public Guid MedicoId { get; set; }
    public Guid[] HospitaisPreferidos { get; set; } = Array.Empty<Guid>();
    public Guid[] EspecialidadesPreferidas { get; set; } = Array.Empty<Guid>();
    public string[] TurnosPreferidos { get; set; } = Array.Empty<string>();
    public int LimitePlantoesSemana { get; set; }
    public int LimitePlantoesMes { get; set; }
    public string Observacoes { get; set; } = string.Empty;
}

public sealed class MedicoDisponivelDto
{
    public Guid MedicoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Crm { get; set; } = string.Empty;
    public string Especialidade { get; set; } = string.Empty;
    public bool Disponivel { get; set; }
    public bool Indisponivel { get; set; }
    public bool PossuiConflito { get; set; }
    public decimal Score { get; set; }
    public string Motivos { get; set; } = string.Empty;
    public string Alertas { get; set; } = string.Empty;
}

public sealed class EscalaSugestaoDto
{
    public Guid Id { get; set; }
    public Guid PlantaoId { get; set; }
    public string HospitalNome { get; set; } = string.Empty;
    public string EspecialidadeNome { get; set; } = string.Empty;
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public string Status { get; set; } = string.Empty;
    public IEnumerable<MedicoDisponivelDto> Medicos { get; set; } = Array.Empty<MedicoDisponivelDto>();
}

public sealed class SubstituicaoDto
{
    public Guid Id { get; set; }
    public Guid PlantaoId { get; set; }
    public Guid? EscalaId { get; set; }
    public Guid MedicoSolicitanteId { get; set; }
    public Guid? MedicoSubstitutoId { get; set; }
    public string MedicoSolicitanteNome { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RegDate { get; set; }
}

public sealed class PendenciaOperacionalDto
{
    public Guid Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? Prazo { get; set; }
    public Guid? ResponsavelUsuarioId { get; set; }
    public DateTime RegDate { get; set; }
}

public sealed class PendenciasResumoDto
{
    public long Abertas { get; set; }
    public long Criticas { get; set; }
    public long Vencidas { get; set; }
    public long Minhas { get; set; }
}

public sealed class RelatorioResumoDto
{
    public string Tipo { get; set; } = string.Empty;
    public long TotalPlantoes { get; set; }
    public long TotalEscalas { get; set; }
    public long TotalConvites { get; set; }
    public long TotalSubstituicoes { get; set; }
    public long TotalPagamentos { get; set; }
    public decimal ValorTotal { get; set; }
    public IEnumerable<IDictionary<string, object>> Linhas { get; set; } = Array.Empty<IDictionary<string, object>>();
}
