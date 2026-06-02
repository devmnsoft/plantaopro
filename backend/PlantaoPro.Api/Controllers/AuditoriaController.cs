using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using System.Text;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/auditoria")]
[Authorize]
public class AuditoriaController : ControllerBase
{
    private readonly IConfiguration _cfg;
    private readonly UsuarioContextService _usuarioContext;
    private readonly PermissionGuardService _permissionGuard;
    private readonly IAuditService _audit;
    private readonly ILogger<AuditoriaController> _logger;

    public AuditoriaController(IConfiguration cfg, UsuarioContextService usuarioContext, PermissionGuardService permissionGuard, IAuditService audit, ILogger<AuditoriaController> logger)
    {
        _cfg = cfg;
        _usuarioContext = usuarioContext;
        _permissionGuard = permissionGuard;
        _audit = audit;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] AuditoriaFiltroRequest filtro)
    {
        var permissao = await _permissionGuard.ValidarPermissaoAsync(PermissionConstants.AuditoriaVer);
        if (!permissao.Success) return StatusCode(permissao.StatusCode, permissao);

        try
        {
            var page = Math.Max(filtro.Page, 1);
            var pageSize = Math.Clamp(filtro.PageSize, 1, 100);
            using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var where = BuildWhere(filtro, out var args);
            var clienteId = _usuarioContext.GetClienteId();
            if (!_usuarioContext.IsAdminGlobal() && clienteId.HasValue)
            {
                where.Add("a.cliente_id = @ctxClienteId");
                args.Add("ctxClienteId", clienteId.Value);
            }

            args.Add("limit", pageSize);
            args.Add("offset", (page - 1) * pageSize);
            var whereSql = where.Count == 0 ? string.Empty : " where " + string.Join(" and ", where);
            var items = await cn.QueryAsync<AuditoriaItemDto>(@"select
    a.id as ""Id"",
    a.usuario_id as ""UsuarioId"",
    a.cliente_id as ""ClienteId"",
    coalesce(u.nome, '') as ""UsuarioNome"",
    coalesce(c.nome_fantasia, '') as ""ClienteNome"",
    coalesce(a.perfil, '') as ""Perfil"",
    coalesce(a.entidade, '') as ""Entidade"",
    a.entidade_id as ""EntidadeId"",
    coalesce(a.acao, '') as ""Acao"",
    coalesce(a.sucesso, true) as ""Sucesso"",
    coalesce(a.ip_origem, '') as ""IpOrigem"",
    a.reg_date as ""RegDate""
from plantaopro.auditoria_acoes_criticas a
left join plantaopro.usuarios u on u.id = a.usuario_id
left join plantaopro.clientes c on c.id = a.cliente_id" + whereSql + " order by a.reg_date desc limit @limit offset @offset", args);
            var total = await cn.ExecuteScalarAsync<long>("select count(*) from plantaopro.auditoria_acoes_criticas a" + whereSql, args);
            return Ok(ApiResponse<PagedResult<AuditoriaItemDto>>.Ok(new PagedResult<AuditoriaItemDto>(items, page, pageSize, total), "Auditoria carregada."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar auditoria");
            return StatusCode(500, ApiResponse<string>.Fail("Falha ao carregar auditoria.", 500));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var permissao = await _permissionGuard.ValidarPermissaoAsync(PermissionConstants.AuditoriaVer);
        if (!permissao.Success) return StatusCode(permissao.StatusCode, permissao);

        try
        {
            using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var item = await cn.QueryFirstOrDefaultAsync<AuditoriaDetalheDto>(@"select
    a.id as ""Id"",
    a.usuario_id as ""UsuarioId"",
    a.cliente_id as ""ClienteId"",
    coalesce(u.nome, '') as ""UsuarioNome"",
    coalesce(c.nome_fantasia, '') as ""ClienteNome"",
    coalesce(a.perfil, '') as ""Perfil"",
    coalesce(a.entidade, '') as ""Entidade"",
    a.entidade_id as ""EntidadeId"",
    coalesce(a.acao, '') as ""Acao"",
    coalesce(a.sucesso, true) as ""Sucesso"",
    coalesce(a.ip_origem, '') as ""IpOrigem"",
    coalesce(a.user_agent, a.detalhes->>'userAgent', '') as ""UserAgent"",
    coalesce(a.detalhes::text, '{}') as ""DetalhesJson"",
    a.reg_date as ""RegDate""
from plantaopro.auditoria_acoes_criticas a
left join plantaopro.usuarios u on u.id = a.usuario_id
left join plantaopro.clientes c on c.id = a.cliente_id
where a.id=@id", new { id });
            if (item is null) return NotFound(ApiResponse<string>.Fail("Registro de auditoria não encontrado.", 404));
            if (!_usuarioContext.IsAdminGlobal() && _usuarioContext.GetClienteId().HasValue && item.ClienteId != _usuarioContext.GetClienteId())
            {
                await _audit.RegistrarAsync(_usuarioContext.GetUsuarioId(), _usuarioContext.GetClienteId(), AuditoriaConstants.Entidades.Auditoria, id, AuditoriaConstants.Acoes.AcessoNegado, new { motivo = "auditoria_de_outro_cliente" }, false, HttpContext.Connection.RemoteIpAddress?.ToString(), string.Join(',', _usuarioContext.GetRoles()));
                return StatusCode(403, ApiResponse<string>.Fail("Você não possui permissão para visualizar este registro.", 403));
            }
            return Ok(ApiResponse<AuditoriaDetalheDto>.Ok(item, "Registro de auditoria carregado."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar detalhe de auditoria {Id}", id);
            return StatusCode(500, ApiResponse<string>.Fail("Falha ao carregar detalhe da auditoria.", 500));
        }
    }

    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo()
    {
        var permissao = await _permissionGuard.ValidarPermissaoAsync(PermissionConstants.AuditoriaVer);
        if (!permissao.Success) return StatusCode(permissao.StatusCode, permissao);

        try
        {
            using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var clienteFiltro = !_usuarioContext.IsAdminGlobal() && _usuarioContext.GetClienteId().HasValue ? " and cliente_id = @clienteId" : string.Empty;
            var data = await cn.QueryFirstAsync<AuditoriaResumoDto>(@"select
(select count(*) from plantaopro.auditoria_acoes_criticas where reg_date::date=current_date" + clienteFiltro + @") as AcoesHoje,
(select count(*) from plantaopro.auditoria_acoes_criticas where reg_date::date=current_date and sucesso=false" + clienteFiltro + @") as FalhasHoje,
(select count(*) from plantaopro.auditoria_acoes_criticas where acao in ('ACESSO_NEGADO','BLOQUEIO_TENANT','BLOQUEIO_PERMISSAO') and reg_date::date=current_date" + clienteFiltro + @") as AcessosNegados,
(select count(*) from plantaopro.auditoria_acoes_criticas where acao='EXPORTAR_RELATORIO' and reg_date::date=current_date" + clienteFiltro + @") as Exportacoes", new { clienteId = _usuarioContext.GetClienteId() });
            return Ok(ApiResponse<AuditoriaResumoDto>.Ok(data, "Resumo de auditoria carregado."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar resumo de auditoria");
            return StatusCode(500, ApiResponse<string>.Fail("Falha ao carregar resumo de auditoria.", 500));
        }
    }

    [HttpGet("exportar-csv")]
    public async Task<IActionResult> ExportarCsv([FromQuery] AuditoriaFiltroRequest filtro)
    {
        var result = await Get(filtro) as ObjectResult;
        if (result?.Value is not ApiResponse<PagedResult<AuditoriaItemDto>> response || response.Data is null) return StatusCode(result?.StatusCode ?? 500, result?.Value);
        await _audit.RegistrarAsync(_usuarioContext.GetUsuarioId(), _usuarioContext.GetClienteId(), AuditoriaConstants.Entidades.Relatorio, null, AuditoriaConstants.Acoes.ExportarRelatorio, new { tipo = "auditoria_csv" }, true, HttpContext.Connection.RemoteIpAddress?.ToString(), string.Join(',', _usuarioContext.GetRoles()));
        var sb = new StringBuilder("DataHora;UsuarioId;ClienteId;Perfil;Entidade;EntidadeId;Acao;Sucesso;Ip\n");
        foreach (var item in response.Data.Items) sb.AppendLine($"{item.RegDate:O};{item.UsuarioId};{item.ClienteId};{item.Perfil};{item.Entidade};{item.EntidadeId};{item.Acao};{item.Sucesso};{item.IpOrigem}");
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "auditoria.csv");
    }

    private static List<string> BuildWhere(AuditoriaFiltroRequest filtro, out DynamicParameters args)
    {
        args = new DynamicParameters();
        var where = new List<string>();
        if (filtro.DataInicio.HasValue) { where.Add("a.reg_date >= @dataInicio"); args.Add("dataInicio", filtro.DataInicio.Value); }
        if (filtro.DataFim.HasValue) { where.Add("a.reg_date <= @dataFim"); args.Add("dataFim", filtro.DataFim.Value); }
        if (filtro.UsuarioId.HasValue) { where.Add("a.usuario_id = @usuarioId"); args.Add("usuarioId", filtro.UsuarioId.Value); }
        if (filtro.ClienteId.HasValue) { where.Add("a.cliente_id = @clienteId"); args.Add("clienteId", filtro.ClienteId.Value); }
        if (!string.IsNullOrWhiteSpace(filtro.Perfil)) { where.Add("a.perfil ilike @perfil"); args.Add("perfil", "%" + filtro.Perfil + "%"); }
        if (!string.IsNullOrWhiteSpace(filtro.Entidade)) { where.Add("a.entidade = @entidade"); args.Add("entidade", filtro.Entidade); }
        if (!string.IsNullOrWhiteSpace(filtro.Acao)) { where.Add("a.acao = @acao"); args.Add("acao", filtro.Acao); }
        if (filtro.Sucesso.HasValue) { where.Add("a.sucesso = @sucesso"); args.Add("sucesso", filtro.Sucesso.Value); }
        if (!string.IsNullOrWhiteSpace(filtro.Texto)) { where.Add("(a.acao ilike @texto or a.entidade ilike @texto or a.detalhes::text ilike @texto)"); args.Add("texto", "%" + filtro.Texto + "%"); }
        return where;
    }
}

public sealed class AuditoriaFiltroRequest
{
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public Guid? UsuarioId { get; set; }
    public Guid? ClienteId { get; set; }
    public string? Perfil { get; set; }
    public string? Entidade { get; set; }
    public string? Acao { get; set; }
    public bool? Sucesso { get; set; }
    public string? Texto { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class AuditoriaItemDto
{
    public Guid Id { get; set; }
    public Guid? UsuarioId { get; set; }
    public Guid? ClienteId { get; set; }
    public string UsuarioNome { get; set; } = string.Empty;
    public string ClienteNome { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public string Entidade { get; set; } = string.Empty;
    public Guid? EntidadeId { get; set; }
    public string Acao { get; set; } = string.Empty;
    public bool Sucesso { get; set; }
    public string IpOrigem { get; set; } = string.Empty;
    public DateTime RegDate { get; set; }

    public DateTime DataHora
    {
        get => RegDate;
        set => RegDate = value;
    }
}

public sealed class AuditoriaDetalheDto
{
    public Guid Id { get; set; }
    public Guid? UsuarioId { get; set; }
    public Guid? ClienteId { get; set; }
    public string UsuarioNome { get; set; } = string.Empty;
    public string ClienteNome { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public string Entidade { get; set; } = string.Empty;
    public Guid? EntidadeId { get; set; }
    public string Acao { get; set; } = string.Empty;
    public bool Sucesso { get; set; }
    public string IpOrigem { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string DetalhesJson { get; set; } = "{}";
    public DateTime RegDate { get; set; }

    public string Detalhes
    {
        get => DetalhesJson;
        set => DetalhesJson = string.IsNullOrWhiteSpace(value) ? "{}" : value;
    }

    public DateTime DataHora
    {
        get => RegDate;
        set => RegDate = value;
    }
}

public record AuditoriaResumoDto(long AcoesHoje, long FalhasHoje, long AcessosNegados, long Exportacoes);
