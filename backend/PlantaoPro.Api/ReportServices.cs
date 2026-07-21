using System.Globalization;
using System.Text;
using System.Text.Json;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Data;

public sealed class ReportDefinition
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Module { get; init; } = string.Empty;
    public string RequiredPermission { get; init; } = string.Empty;
    public string[] SupportedFormats { get; init; } = Array.Empty<string>();
    public bool ContainsSensitiveData { get; init; }
}

public sealed class ReportFilterRequest
{
    public DateTime? Inicio { get; init; }
    public DateTime? Fim { get; init; }
    public Guid? UnidadeId { get; init; }
    public Guid? HospitalId { get; init; }
    public Guid? MedicoId { get; init; }
    public Guid? EspecialidadeId { get; init; }
    public Guid? ConvenioId { get; init; }
    public string? Status { get; init; }
    public string? FormaPagamento { get; init; }
    public string? Agrupamento { get; init; }
    public string? OrdenarPor { get; init; }
    public int Pagina { get; init; } = 1;
    public int TamanhoPagina { get; init; } = 50;
}

public sealed class ReportExportResult
{
    public byte[] Content { get; init; } = Array.Empty<byte>();
    public string ContentType { get; init; } = "text/csv; charset=utf-8";
    public string FileName { get; init; } = string.Empty;
    public int RowCount { get; init; }
}

public interface IReportCatalogService { IReadOnlyCollection<ReportDefinition> Listar(); ReportDefinition? Obter(string codigo); }
public interface IReportPermissionService { ApiResponse<bool> Validar(ReportDefinition report, Guid? tenantId, Guid? usuarioId, bool exportacao); }
public interface IReportQueryService { Task<IReadOnlyCollection<IDictionary<string, object?>>> ConsultarAsync(ReportDefinition report, ReportFilterRequest filtros, Guid? tenantId, CancellationToken cancellationToken); }
public interface IReportAuditService { Task RegistrarAsync(Guid? clienteId, Guid? tenantId, Guid? usuarioId, string codigoRelatorio, string formato, object filtrosSanitizados, int quantidadeRegistros, bool contemDadosSensiveis, string status, long duracaoMs, string nomeArquivo, string? ipOrigem); }
public interface IReportExportService { Task<ReportExportResult> ExportarCsvAsync(string codigo, ReportFilterRequest filtros, Guid? tenantId, Guid? usuarioId, string? ipOrigem, CancellationToken cancellationToken); }

public sealed class ReportCatalogService : IReportCatalogService
{
    private static readonly ReportDefinition[] Reports = new[]
    {
        Def("PLANTOES_OPERACIONAL", "Plantões operacionais", "Plantões por período, hospital e especialidade.", "Plantões", "Relatorios.Ver", false),
        Def("ESCALAS_CONSOLIDADO", "Escalas consolidadas", "Cobertura de escalas e convites.", "Escalas", "Relatorios.Ver", false),
        Def("CONVITES_OPERACIONAL", "Convites", "Convites enviados, aceitos e pendentes.", "Convites", "Relatorios.Ver", false),
        Def("PAGAMENTOS_MEDICOS", "Pagamentos médicos", "Valores previstos e realizados por médico.", "Pagamentos", "Relatorios.Financeiros", false),
        Def("ATENDIMENTOS_CLINICOS", "Atendimentos clínicos", "Indicadores clínicos agregados sem conteúdo assistencial.", "Atendimentos", "Relatorios.Clinicos", true),
        Def("PRODUTIVIDADE_MEDICA", "Produtividade médica", "Volume de atendimentos por médico.", "Produtividade", "Relatorios.Clinicos", false),
        Def("FINANCEIRO_CLINICA", "Financeiro da clínica", "Receitas, inadimplência e contas vencidas.", "Financeiro", "Relatorios.Financeiros", false),
        Def("CONVENIOS_FATURAMENTO", "Convênios", "Faturamento e volume por convênio.", "Convênios", "Relatorios.Financeiros", false),
        Def("AUTORIZACOES_OPERACIONAL", "Autorizações", "Autorizações por status e convênio.", "Autorizações", "Relatorios.Financeiros", false),
        Def("GLOSAS_CONSOLIDADO", "Glosas", "Glosas consolidadas por período.", "Glosas", "Relatorios.Financeiros", false),
        Def("CAIXA_MOVIMENTACOES", "Caixa", "Movimentações financeiras do caixa.", "Caixa", "Relatorios.Financeiros", false),
        Def("REPASSES_MEDICOS", "Repasses médicos", "Repasses devidos e realizados.", "Repasses", "Relatorios.Financeiros", false),
        Def("AUDITORIA_OPERACIONAL", "Auditoria operacional", "Eventos operacionais auditáveis.", "Auditoria", "Relatorios.Executivos", false),
        Def("EXECUTIVO_GERAL", "Indicadores executivos", "Visão executiva de operação e valor.", "Executivo", "Relatorios.Executivos", false)
    };
    public IReadOnlyCollection<ReportDefinition> Listar() { return Reports; }
    public ReportDefinition? Obter(string codigo) { return Reports.FirstOrDefault(x => string.Equals(x.Code, codigo, StringComparison.OrdinalIgnoreCase)); }
    private static ReportDefinition Def(string code, string name, string desc, string module, string perm, bool sensivel) { return new ReportDefinition { Code = code, Name = name, Description = desc, Module = module, RequiredPermission = perm, SupportedFormats = new[] { "csv" }, ContainsSensitiveData = sensivel }; }
}

public sealed class ReportPermissionService : IReportPermissionService
{
    public ApiResponse<bool> Validar(ReportDefinition report, Guid? tenantId, Guid? usuarioId, bool exportacao)
    {
        if (!usuarioId.HasValue) return ApiResponse<bool>.Fail("Usuário autenticado é obrigatório.", 401);
        if (!tenantId.HasValue) return ApiResponse<bool>.Fail("Contexto explícito de tenant é obrigatório para relatórios.", 403);
        if (exportacao && report.ContainsSensitiveData && report.RequiredPermission != "Relatorios.DadosSensiveis") return ApiResponse<bool>.Fail("Exportação de dados sensíveis exige permissão explícita.", 403);
        return ApiResponse<bool>.Ok(true, "Permissão validada.");
    }
}

public sealed class ReportQueryService : IReportQueryService
{
    private static readonly HashSet<string> Sortable = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "indicador", "valor", "periodo" };
    public Task<IReadOnlyCollection<IDictionary<string, object?>>> ConsultarAsync(ReportDefinition report, ReportFilterRequest filtros, Guid? tenantId, CancellationToken cancellationToken)
    {
        var inicio = filtros.Inicio ?? DateTime.UtcNow.Date.AddDays(-30); var fim = filtros.Fim ?? DateTime.UtcNow.Date;
        if (fim < inicio) throw new InvalidOperationException("Data final deve ser maior ou igual à data inicial.");
        if ((fim - inicio).TotalDays > 366) throw new InvalidOperationException("Período máximo permitido para relatório é 366 dias.");
        if (!string.IsNullOrWhiteSpace(filtros.OrdenarPor) && !Sortable.Contains(filtros.OrdenarPor)) throw new InvalidOperationException("Coluna de ordenação não permitida.");
        IReadOnlyCollection<IDictionary<string, object?>> rows = new[] { Row("Periodo", inicio.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " a " + fim.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)), Row("Relatorio", report.Name), Row("Tenant", tenantId.ToString() ?? "") };
        return Task.FromResult(rows);
    }
    private static IDictionary<string, object?> Row(string indicador, object? valor) { return new Dictionary<string, object?> { { "indicador", indicador }, { "valor", valor } }; }
}

public sealed class CsvExportService
{
    public byte[] ToCsv(IEnumerable<IDictionary<string, object?>> rows)
    {
        var materialized = rows.ToArray();
        var headers = materialized.SelectMany(x => x.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine(string.Join(";", headers.Select(Escape)));
        foreach (var row in materialized)
        {
            sb.AppendLine(string.Join(";", headers.Select(h => Escape(row.TryGetValue(h, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : string.Empty))));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
    private static string Escape(string? value)
    {
        var text = (value ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");
        if (text.Length > 0 && (text[0] == '=' || text[0] == '+' || text[0] == '-' || text[0] == '@')) text = "'" + text;
        var quote = text.Contains(';') || text.Contains('"') || text.Contains('\n');
        text = text.Replace("\"", "\"\"");
        return quote ? "\"" + text + "\"" : text;
    }
}

public sealed class ReportAuditService : IReportAuditService
{
    private readonly ILogger<ReportAuditService> logger;
    public ReportAuditService(ILogger<ReportAuditService> logger) { this.logger = logger; }
    public Task RegistrarAsync(Guid? clienteId, Guid? tenantId, Guid? usuarioId, string codigoRelatorio, string formato, object filtrosSanitizados, int quantidadeRegistros, bool contemDadosSensiveis, string status, long duracaoMs, string nomeArquivo, string? ipOrigem)
    {
        logger.LogInformation("Exportação de relatório auditada: codigo={CodigoRelatorio} formato={Formato} quantidade={QuantidadeRegistros} status={Status} duracaoMs={DuracaoMs} arquivo={NomeArquivo}", codigoRelatorio, formato, quantidadeRegistros, status, duracaoMs, nomeArquivo);
        return Task.CompletedTask;
    }
}

public sealed class ReportExportService : IReportExportService
{
    private readonly IReportCatalogService catalog; private readonly IReportPermissionService permissions; private readonly IReportQueryService query; private readonly CsvExportService csv; private readonly IReportAuditService audit;
    public ReportExportService(IReportCatalogService catalog, IReportPermissionService permissions, IReportQueryService query, CsvExportService csv, IReportAuditService audit) { this.catalog = catalog; this.permissions = permissions; this.query = query; this.csv = csv; this.audit = audit; }
    public async Task<ReportExportResult> ExportarCsvAsync(string codigo, ReportFilterRequest filtros, Guid? tenantId, Guid? usuarioId, string? ipOrigem, CancellationToken cancellationToken)
    {
        var started = DateTime.UtcNow; var report = catalog.Obter(codigo) ?? throw new InvalidOperationException("Relatório não encontrado.");
        var permission = permissions.Validar(report, tenantId, usuarioId, true); if (!permission.Success) throw new UnauthorizedAccessException(permission.Message);
        var rows = await query.ConsultarAsync(report, filtros, tenantId, cancellationToken);
        var fileName = NormalizeFileName(report.Code.ToLowerInvariant()) + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".csv";
        var content = csv.ToCsv(rows);
        await audit.RegistrarAsync(tenantId, tenantId, usuarioId, report.Code, "CSV", new { filtros.Inicio, filtros.Fim, filtros.HospitalId, filtros.MedicoId, filtros.Status }, rows.Count, report.ContainsSensitiveData, "SUCESSO", (long)(DateTime.UtcNow - started).TotalMilliseconds, fileName, ipOrigem);
        return new ReportExportResult { Content = content, FileName = fileName, RowCount = rows.Count };
    }
    private static string NormalizeFileName(string value) { return new string(value.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray()).Trim('-'); }
}
