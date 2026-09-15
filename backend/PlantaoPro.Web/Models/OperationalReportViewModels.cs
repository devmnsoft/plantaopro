namespace PlantaoPro.Web.Models;
public sealed record OperationalReportIndicator(string Code,string Label,decimal Value,string Unit,string? DetailStatus,string Definition);
public sealed record OperationalReportRow(Guid Id,string Reference,string Unit,string Professional,DateTime PeriodDate,string Status,decimal? Value,string ActionUrl);
public sealed record OperationalReportResult(string Code,string Title,string TimeCriterion,DateTime GeneratedAtUtc,IReadOnlyList<OperationalReportIndicator> Indicators,IReadOnlyList<OperationalReportRow> Items,long Total,int Page,int PageSize,bool CurrentState,string? DetailStatus);
public sealed record OperationalReportPageViewModel(string Kind,DateOnly Inicio,DateOnly Fim,Guid? UnidadeId,Guid? EspecialidadeId,Guid? ProfissionalId,string? Situacao,OperationalReportResult? Report,string? Error);
