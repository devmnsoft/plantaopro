namespace PlantaoPro.Web.Models;

public sealed class FechamentoOperacionalPageViewModel
{
    public IReadOnlyList<FechamentoWebDto> Fechamentos { get; set; }=Array.Empty<FechamentoWebDto>();
    public FechamentoWebDto? Selecionado { get; set; }
    public IReadOnlyList<FechamentoTimelineWebDto> Timeline { get; set; }=Array.Empty<FechamentoTimelineWebDto>();
    public string? Error { get; set; }
    public DateOnly? Inicio { get; set; } public DateOnly? Fim { get; set; } public Guid? UnidadeId { get; set; }
    public Guid? EspecialidadeId { get; set; } public string Profissional { get; set; }=""; public string Status { get; set; }="";
}
public sealed class FechamentoWebDto
{
    public Guid Id{get;set;} public Guid PlantaoId{get;set;} public string Hospital{get;set;}=""; public string Especialidade{get;set;}="";
    public DateTime Inicio{get;set;} public DateTime Fim{get;set;} public string Status{get;set;}=""; public decimal ValorPrevisto{get;set;}
    public decimal ValorApurado{get;set;} public decimal HorasPrevistas{get;set;} public decimal HorasRealizadas{get;set;} public int QuantidadeEscalas{get;set;}
    public int DivergenciasAbertas{get;set;} public DateTime CriadoEm{get;set;} public IReadOnlyList<FechamentoItemWebDto> Itens{get;set;}=Array.Empty<FechamentoItemWebDto>(); public IReadOnlyList<FechamentoDivergenciaWebDto> Divergencias{get;set;}=Array.Empty<FechamentoDivergenciaWebDto>();
    public Guid HospitalId{get;set;} public Guid EspecialidadeId{get;set;} public string Profissionais{get;set;}="";
}
public sealed class FechamentoItemWebDto{public Guid Id{get;set;}public Guid EscalaId{get;set;}public string Medico{get;set;}="";public string Crm{get;set;}="";public string StatusEscala{get;set;}="";public DateTime InicioPrevisto{get;set;}public DateTime FimPrevisto{get;set;}public decimal HorasPrevistas{get;set;}public decimal HorasRealizadas{get;set;}public decimal ValorPrevisto{get;set;}public decimal ValorApurado{get;set;}public bool PossuiDivergencia{get;set;}public Guid? PagamentoId{get;set;}public string? PagamentoStatus{get;set;}}
public sealed class FechamentoDivergenciaWebDto{public Guid Id{get;set;}public string Tipo{get;set;}="";public string Descricao{get;set;}="";public string Status{get;set;}="";public string? Resolucao{get;set;}public DateTime CriadoEm{get;set;}public DateTime? ResolvidoEm{get;set;}}
public sealed class FechamentoTimelineWebDto{public Guid Id{get;set;}public string Evento{get;set;}="";public string? StatusAnterior{get;set;}public string? StatusNovo{get;set;}public string? Descricao{get;set;}public DateTime ExecutadoEm{get;set;}}
