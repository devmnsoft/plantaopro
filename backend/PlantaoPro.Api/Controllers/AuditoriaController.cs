using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/auditoria")]
[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador)]
public class AuditoriaController : ControllerBase
{
    private readonly IConfiguration _cfg;
    private readonly UsuarioContextService _usuarioContext;
    private readonly PermissionGuardService _permissionGuard;
    private readonly ILogger<AuditoriaController> _logger;

    public AuditoriaController(IConfiguration cfg, UsuarioContextService usuarioContext, PermissionGuardService permissionGuard, ILogger<AuditoriaController> logger)
    {
        _cfg = cfg;
        _usuarioContext = usuarioContext;
        _permissionGuard = permissionGuard;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] AuditoriaFiltro filtro)
    {
        if (!_permissionGuard.HasPermission(Permissoes.AuditoriaVer)) return Forbid();

        try
        {
            var page = filtro.Page <= 0 ? 1 : filtro.Page;
            var pageSize = filtro.PageSize <= 0 || filtro.PageSize > 100 ? 20 : filtro.PageSize;
            var where = new List<string> { "1=1" };
            var p = new DynamicParameters();
            p.Add("pOffset", (page - 1) * pageSize);
            p.Add("pLimit", pageSize);
            AplicarFiltros(filtro, where, p);
            if (!_usuarioContext.IsAdminGlobal())
            {
                var clienteId = _usuarioContext.GetClienteId();
                if (!clienteId.HasValue) return StatusCode(403, ApiResponse<string>.Fail("Acesso negado à auditoria do cliente.", 403));
                where.Add("cliente_id=@clienteId");
                p.Add("clienteId", clienteId.Value);
            }

            var whereSql = string.Join(" and ", where);
            using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var items = await cn.QueryAsync($@"select id as Id, usuario_id as UsuarioId, cliente_id as ClienteId, entidade as Entidade,
                    entidade_id as EntidadeId, acao as Acao, detalhes::text as Detalhes, sucesso as Sucesso,
                    ip_origem as Ip, perfil as Perfil, reg_date as DataHora
                from plantaopro.auditoria_acoes_criticas
                where {whereSql}
                order by reg_date desc
                offset @pOffset limit @pLimit", p);
            var total = await cn.ExecuteScalarAsync<long>($"select count(*) from plantaopro.auditoria_acoes_criticas where {whereSql}", p);
            return Ok(ApiResponse<object>.Ok(new { items, page, pageSize, total }, "Auditoria carregada."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar auditoria");
            return StatusCode(500, ApiResponse<string>.Fail("Falha ao consultar auditoria.", 500));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        if (!_permissionGuard.HasPermission(Permissoes.AuditoriaVer)) return Forbid();

        try
        {
            using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var item = await cn.QueryFirstOrDefaultAsync(@"select id as Id, usuario_id as UsuarioId, cliente_id as ClienteId, entidade as Entidade,
                    entidade_id as EntidadeId, acao as Acao, detalhes::text as Detalhes, sucesso as Sucesso,
                    ip_origem as Ip, perfil as Perfil, reg_date as DataHora
                from plantaopro.auditoria_acoes_criticas where id=@id", new { id });
            if (item is null) return NotFound(ApiResponse<string>.Fail("Registro de auditoria não encontrado.", 404));
            if (!_usuarioContext.IsAdminGlobal())
            {
                Guid? clienteId = item.ClienteId;
                var atual = _usuarioContext.GetClienteId();
                if (!clienteId.HasValue || !atual.HasValue || clienteId.Value != atual.Value) return StatusCode(403, ApiResponse<string>.Fail("Acesso negado à auditoria do cliente.", 403));
            }

            return Ok(ApiResponse<object>.Ok(item, "Auditoria carregada."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar detalhe de auditoria {AuditoriaId}", id);
            return StatusCode(500, ApiResponse<string>.Fail("Falha ao consultar auditoria.", 500));
        }
    }

    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo()
    {
        if (!_permissionGuard.HasPermission(Permissoes.AuditoriaVer)) return Forbid();

        try
        {
            var where = new List<string> { "reg_date::date = current_date" };
            var p = new DynamicParameters();
            if (!_usuarioContext.IsAdminGlobal())
            {
                var clienteId = _usuarioContext.GetClienteId();
                if (!clienteId.HasValue) return StatusCode(403, ApiResponse<string>.Fail("Acesso negado à auditoria do cliente.", 403));
                where.Add("cliente_id=@clienteId");
                p.Add("clienteId", clienteId.Value);
            }

            using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var whereSql = string.Join(" and ", where);
            var data = await cn.QueryFirstAsync($@"select
                count(*) as AcoesHoje,
                count(*) filter (where sucesso=false) as FalhasHoje,
                count(*) filter (where acao in ('{AuditoriaConstants.Acoes.AcessoNegado}','{AuditoriaConstants.Acoes.BloqueioTenant}','{AuditoriaConstants.Acoes.BloqueioPermissao}')) as AcessosNegados,
                count(*) filter (where acao='{AuditoriaConstants.Acoes.ExportarRelatorio}') as Exportacoes
                from plantaopro.auditoria_acoes_criticas where {whereSql}", p);
            return Ok(ApiResponse<object>.Ok(data, "Resumo de auditoria carregado."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar resumo de auditoria");
            return StatusCode(500, ApiResponse<string>.Fail("Falha ao consultar resumo de auditoria.", 500));
        }
    }

    private static void AplicarFiltros(AuditoriaFiltro filtro, List<string> where, DynamicParameters p)
    {
        if (filtro.DataInicio.HasValue) { where.Add("reg_date >= @dataInicio"); p.Add("dataInicio", filtro.DataInicio.Value); }
        if (filtro.DataFim.HasValue) { where.Add("reg_date < @dataFim"); p.Add("dataFim", filtro.DataFim.Value.Date.AddDays(1)); }
        if (filtro.UsuarioId.HasValue) { where.Add("usuario_id=@usuarioId"); p.Add("usuarioId", filtro.UsuarioId.Value); }
        if (filtro.ClienteId.HasValue) { where.Add("cliente_id=@filtroClienteId"); p.Add("filtroClienteId", filtro.ClienteId.Value); }
        if (!string.IsNullOrWhiteSpace(filtro.Perfil)) { where.Add("perfil ilike @perfil"); p.Add("perfil", "%" + filtro.Perfil.Trim() + "%"); }
        if (!string.IsNullOrWhiteSpace(filtro.Entidade)) { where.Add("entidade=@entidade"); p.Add("entidade", filtro.Entidade.Trim().ToUpperInvariant()); }
        if (!string.IsNullOrWhiteSpace(filtro.Acao)) { where.Add("acao=@acao"); p.Add("acao", filtro.Acao.Trim().ToUpperInvariant()); }
        if (filtro.Sucesso.HasValue) { where.Add("sucesso=@sucesso"); p.Add("sucesso", filtro.Sucesso.Value); }
        if (!string.IsNullOrWhiteSpace(filtro.Texto)) { where.Add("(acao ilike @texto or entidade ilike @texto or detalhes::text ilike @texto)"); p.Add("texto", "%" + filtro.Texto.Trim() + "%"); }
    }
}

public sealed class AuditoriaFiltro
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
