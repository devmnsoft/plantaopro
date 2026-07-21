using System.Globalization;
using System.Text;
using System.Text.Json;
using Dapper;
using Npgsql;
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
    private readonly IConfiguration cfg;
    public ReportPermissionService(IConfiguration cfg) { this.cfg = cfg; }
    public ApiResponse<bool> Validar(ReportDefinition report, Guid? tenantId, Guid? usuarioId, bool exportacao)
    {
        if (!usuarioId.HasValue) return ApiResponse<bool>.Fail("Usuário autenticado é obrigatório.", 401);
        if (!tenantId.HasValue) return ApiResponse<bool>.Fail("Contexto explícito de tenant é obrigatório para relatórios.", 403);
        var required = new List<string> { "Relatorios.Ver", report.RequiredPermission };
        if (exportacao) required.Add("Relatorios.Exportar");
        if (report.ContainsSensitiveData) required.Add("Relatorios.DadosSensiveis");
        using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var allowed = cn.ExecuteScalar<bool>(@"
select exists(
    select 1
    from plantaopro.usuario_perfis up
    join plantaopro.perfis pf on pf.id=up.perfil_id and pf.reg_status='A'
    join plantaopro.perfil_permissoes pp on pp.perfil_id=pf.id and pp.permitido=true and pp.reg_status='A'
    join plantaopro.permissoes pm on pm.id=pp.permissao_id and pm.reg_status='A'
    where up.usuario_id=@usuarioId and up.tenant_id=@tenantId and up.reg_status='A' and pm.codigo = any(@required)
) or exists(
    select 1 from plantaopro.usuario_permissoes_especiais upe
    join plantaopro.permissoes pm on pm.id=upe.permissao_id and pm.reg_status='A'
    where upe.usuario_id=@usuarioId and upe.tenant_id=@tenantId and upe.reg_status='A' and coalesce(upe.permitido,true)=true and pm.codigo = any(@required)
)", new { usuarioId, tenantId, required = required.Distinct().ToArray() });
        if (!allowed) return ApiResponse<bool>.Fail("Usuário não possui permissão, role, módulo/plano ou autorização suficiente para este relatório.", 403);
        return ApiResponse<bool>.Ok(true, "Permissão validada.");
    }
}

public sealed class ReportQueryService : IReportQueryService
{
    private static readonly HashSet<string> Sortable = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "indicador", "valor", "periodo" };
    private readonly IConfiguration cfg;
    public ReportQueryService(IConfiguration cfg) { this.cfg = cfg; }
    public async Task<IReadOnlyCollection<IDictionary<string, object?>>> ConsultarAsync(ReportDefinition report, ReportFilterRequest filtros, Guid? tenantId, CancellationToken cancellationToken)
    {
        if (!tenantId.HasValue) throw new InvalidOperationException("Tenant obrigatório.");
        var inicio = filtros.Inicio ?? DateTime.UtcNow.Date.AddDays(-30);
        var fim = filtros.Fim ?? DateTime.UtcNow.Date;
        if (fim < inicio) throw new InvalidOperationException("Data final deve ser maior ou igual à data inicial.");
        if ((fim - inicio).TotalDays > 366) throw new InvalidOperationException("Período máximo permitido para relatório é 366 dias.");
        if (!string.IsNullOrWhiteSpace(filtros.OrdenarPor) && !Sortable.Contains(filtros.OrdenarPor)) throw new InvalidOperationException("Coluna de ordenação não permitida.");
        var pagina = Math.Max(1, filtros.Pagina);
        var tamanho = Math.Min(Math.Max(1, filtros.TamanhoPagina), 200);
        var sql = Sql(report.Code);
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync(sql, new { tenantId, inicio, fim = fim.Date.AddDays(1), filtros.UnidadeId, filtros.HospitalId, filtros.MedicoId, filtros.EspecialidadeId, filtros.ConvenioId, filtros.Status, filtros.FormaPagamento, limit = tamanho, offset = (pagina - 1) * tamanho });
        return rows.Select(r => (IDictionary<string, object?>)new Dictionary<string, object?>((IDictionary<string, object?>)r, StringComparer.OrdinalIgnoreCase)).ToArray();
    }
    private static string Sql(string code)
    {
        var table = code switch
        {
            "PLANTOES_OPERACIONAL" => "plantoes", "ESCALAS_CONSOLIDADO" => "escalas", "CONVITES_OPERACIONAL" => "cadastro_cliente_convites", "PAGAMENTOS_MEDICOS" => "pagamentos",
            "ATENDIMENTOS_CLINICOS" => "consultas", "PRODUTIVIDADE_MEDICA" => "consultas", "FINANCEIRO_CLINICA" => "v116_recebimentos_parciais", "CONVENIOS_FATURAMENTO" => "v116_convenio_guias",
            "AUTORIZACOES_OPERACIONAL" => "v116_convenio_autorizacoes", "GLOSAS_CONSOLIDADO" => "v116_faturamento_lote_itens", "CAIXA_MOVIMENTACOES" => "v116_caixa_movimentos", "REPASSES_MEDICOS" => "pagamentos",
            "AUDITORIA_OPERACIONAL" => "auditoria_eventos", "EXECUTIVO_GERAL" => "plantoes", _ => "plantoes"
        };
        return $@"select @code::text as indicador, count(1)::bigint as valor, to_char(date_trunc('day', coalesce(t.reg_date, now())), 'YYYY-MM-DD') as periodo
from plantaopro.{table} t
where coalesce(t.reg_status,'A')='A'
  and (coalesce(to_jsonb(t)->>'tenant_id', to_jsonb(t)->>'cliente_id') is null or coalesce(to_jsonb(t)->>'tenant_id', to_jsonb(t)->>'cliente_id')=@tenantId::text)
  and coalesce(t.reg_date, now()) >= @inicio and coalesce(t.reg_date, now()) < @fim
  and (@status is null or upper(coalesce(to_jsonb(t)->>'status',''))=upper(@status))
group by periodo
order by periodo
limit @limit offset @offset".Replace("@code", "'" + code.Replace("'", "") + "'");
    }
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
    private readonly ILogger<ReportAuditService> logger; private readonly IConfiguration cfg;
    public ReportAuditService(ILogger<ReportAuditService> logger, IConfiguration cfg) { this.logger = logger; this.cfg = cfg; }
    public async Task RegistrarAsync(Guid? clienteId, Guid? tenantId, Guid? usuarioId, string codigoRelatorio, string formato, object filtrosSanitizados, int quantidadeRegistros, bool contemDadosSensiveis, string status, long duracaoMs, string nomeArquivo, string? ipOrigem)
    {
        if (!tenantId.HasValue) throw new InvalidOperationException("Tenant obrigatório para auditoria de relatório.");
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.ExecuteAsync(@"insert into plantaopro.relatorio_exportacoes(id,cliente_id,tenant_id,usuario_id,codigo_relatorio,formato,filtros_sanitizados,quantidade_registros,contem_dados_sensiveis,status,duracao_ms,nome_arquivo,ip_origem,reg_date,reg_status)
values(gen_random_uuid(),@clienteId,@tenantId,@usuarioId,@codigoRelatorio,@formato,cast(@filtros as jsonb),@quantidadeRegistros,@contemDadosSensiveis,@status,@duracaoMs,@nomeArquivo,@ipOrigem,now(),'A')",
            new { clienteId, tenantId, usuarioId, codigoRelatorio, formato, filtros = JsonSerializer.Serialize(filtrosSanitizados), quantidadeRegistros, contemDadosSensiveis, status, duracaoMs, nomeArquivo, ipOrigem });
        logger.LogInformation("Exportação de relatório persistida: codigo={CodigoRelatorio} formato={Formato} quantidade={QuantidadeRegistros} status={Status}", codigoRelatorio, formato, quantidadeRegistros, status);
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
