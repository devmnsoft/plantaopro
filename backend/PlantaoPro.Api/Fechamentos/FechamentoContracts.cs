namespace PlantaoPro.Api.Fechamentos;

public class FechamentoResumoDto
{
    public Guid Id { get; set; }
    public Guid PlantaoId { get; set; }
    public string Hospital { get; set; } = string.Empty;
    public string Especialidade { get; set; } = string.Empty;
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal ValorPrevisto { get; set; }
    public decimal ValorApurado { get; set; }
    public decimal HorasPrevistas { get; set; }
    public decimal HorasRealizadas { get; set; }
    public int QuantidadeEscalas { get; set; }
    public int DivergenciasAbertas { get; set; }
    public DateTime CriadoEm { get; set; }
    public Guid HospitalId { get; set; }
    public Guid EspecialidadeId { get; set; }
    public string Profissionais { get; set; } = string.Empty;
}
public sealed record FechamentoFiltroRequest(DateOnly? Inicio, DateOnly? Fim, Guid? UnidadeId, string? Profissional, Guid? EspecialidadeId, string? Status, bool Pendentes = false);
public sealed class FechamentoItemDto
{
    public Guid Id { get; set; } public Guid EscalaId { get; set; } public Guid MedicoId { get; set; }
    public string Medico { get; set; } = string.Empty; public string Crm { get; set; } = string.Empty;
    public string StatusEscala { get; set; } = string.Empty; public DateTime InicioPrevisto { get; set; }
    public DateTime FimPrevisto { get; set; } public decimal HorasPrevistas { get; set; }
    public decimal HorasRealizadas { get; set; } public decimal ValorPrevisto { get; set; }
    public decimal ValorApurado { get; set; } public bool PossuiDivergencia { get; set; }
    public Guid? PagamentoId { get; set; } public string? PagamentoStatus { get; set; }
}
public sealed class FechamentoDivergenciaDto
{
    public Guid Id { get; set; } public Guid? FechamentoItemId { get; set; } public string Tipo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty; public decimal? ValorAnterior { get; set; }
    public decimal? ValorProposto { get; set; } public string Status { get; set; } = string.Empty;
    public string? Resolucao { get; set; } public DateTime CriadoEm { get; set; } public DateTime? ResolvidoEm { get; set; }
}
public sealed class FechamentoDetalheDto : FechamentoResumoDto
{
    public IReadOnlyList<FechamentoItemDto> Itens { get; set; } = Array.Empty<FechamentoItemDto>();
    public IReadOnlyList<FechamentoDivergenciaDto> Divergencias { get; set; } = Array.Empty<FechamentoDivergenciaDto>();
}
public sealed class FechamentoTimelineDto
{
    public Guid Id { get; set; } public string Evento { get; set; } = string.Empty; public string? StatusAnterior { get; set; }
    public string? StatusNovo { get; set; } public string? Descricao { get; set; } public Guid ExecutadoPor { get; set; }
    public DateTime ExecutadoEm { get; set; }
}
public sealed record CriarDivergenciaRequest(Guid? FechamentoItemId, string Tipo, string Descricao, decimal? ValorAnterior, decimal? ValorProposto, string? Motivo);
public sealed record ResolverDivergenciaRequest(string Resolucao);
public sealed record DevolverFechamentoRequest(string Motivo);
public sealed record RejeitarFechamentoRequest(string Motivo, string? Observacao);
