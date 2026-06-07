using System.Text;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.Financeiro)]
[Route("api/relatorios/saas")]
[Tags("SaaS - Relatórios")]
public sealed class RelatoriosSaasController : ControllerBase
{
    private readonly IConfiguration cfg;
    private readonly IAuditService audit;
    private readonly ILogger<RelatoriosSaasController> logger;

    public RelatoriosSaasController(IConfiguration cfg, IAuditService audit, ILogger<RelatoriosSaasController> logger)
    {
        this.cfg = cfg;
        this.audit = audit;
        this.logger = logger;
    }

    [HttpGet("clientes")]
    public Task<IActionResult> Clientes([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => ExecutarRelatorioAsync("clientes", @"select c.id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"",
       coalesce(c.status,'') as ""Status"", coalesce(c.saude_status,'') as ""Classificacao"", 1::bigint as ""Quantidade"", 0::numeric as ""Valor""
from plantaopro.clientes c
where c.reg_status='A' and (@status is null or upper(c.status)=upper(@status))
order by c.reg_date desc", new { status }, page, pageSize);

    [HttpGet("assinaturas")]
    public Task<IActionResult> Assinaturas([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => ExecutarRelatorioAsync("assinaturas", @"select a.cliente_id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"",
       coalesce(a.status,'') as ""Status"", coalesce(p.nome,'') as ""Classificacao"", 1::bigint as ""Quantidade"", a.valor_contratado as ""Valor""
from plantaopro.assinaturas a
join plantaopro.clientes c on c.id=a.cliente_id
join plantaopro.planos p on p.id=a.plano_id
where a.reg_status='A' and (@status is null or upper(a.status)=upper(@status))
order by a.reg_date desc", new { status }, page, pageSize);

    [HttpGet("faturamento")]
    public Task<IActionResult> Faturamento([FromQuery] DateOnly? competencia, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => ExecutarRelatorioAsync("faturamento", @"select f.cliente_id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"",
       coalesce(f.status,'') as ""Status"", 'FATURA' as ""Classificacao"", 1::bigint as ""Quantidade"", f.valor as ""Valor"", f.competencia as ""Competencia""
from plantaopro.faturas_saas f
join plantaopro.clientes c on c.id=f.cliente_id
where f.reg_status='A' and (@status is null or upper(f.status)=upper(@status)) and (@competencia is null or f.competencia=@competencia)
order by f.competencia desc, f.vencimento desc", new { competencia, status }, page, pageSize);

    [HttpGet("inadimplencia")]
    public Task<IActionResult> Inadimplencia([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => ExecutarRelatorioAsync("inadimplencia", @"select f.cliente_id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"",
       'VENCIDA' as ""Status"", 'INADIMPLENTE' as ""Classificacao"", count(*)::bigint as ""Quantidade"", coalesce(sum(f.valor),0) as ""Valor""
from plantaopro.faturas_saas f
join plantaopro.clientes c on c.id=f.cliente_id
where f.reg_status='A' and f.status='VENCIDA'
group by f.cliente_id, c.nome_fantasia, c.razao_social
order by coalesce(sum(f.valor),0) desc", new { }, page, pageSize);

    [HttpGet("uso-planos")]
    public Task<IActionResult> UsoPlanos([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => ExecutarRelatorioAsync("uso-planos", @"select u.cliente_id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"",
       coalesce(u.recurso,'') as ""Status"", coalesce(p.nome,'') as ""Classificacao"", coalesce(sum(u.usado),0)::bigint as ""Quantidade"", coalesce(avg(u.percentual),0) as ""Valor"", max(u.competencia) as ""Competencia""
from plantaopro.cliente_limites_uso u
join plantaopro.clientes c on c.id=u.cliente_id
left join plantaopro.assinaturas a on a.id=u.assinatura_id
left join plantaopro.planos p on p.id=a.plano_id
where u.reg_status='A'
group by u.cliente_id, c.nome_fantasia, c.razao_social, u.recurso, p.nome
order by coalesce(avg(u.percentual),0) desc", new { }, page, pageSize);

    [HttpGet("clientes-risco")]
    public Task<IActionResult> ClientesRisco([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => ExecutarRelatorioAsync("clientes-risco", @"select c.id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"",
       coalesce(c.status,'') as ""Status"", coalesce(c.saude_status,'') as ""Classificacao"", 1::bigint as ""Quantidade"", c.saude_score::numeric as ""Valor""
from plantaopro.clientes c
where c.reg_status='A' and coalesce(c.saude_status,'') in ('RISCO','CRITICO')
order by c.saude_score asc, c.reg_date desc", new { }, page, pageSize);

    [HttpGet("upgrade")]
    public Task<IActionResult> Upgrade([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => ExecutarRelatorioAsync("upgrade", @"select a.cliente_id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"",
       coalesce(a.tipo,'') as ""Status"", coalesce(a.severidade,'') as ""Classificacao"", 1::bigint as ""Quantidade"", 0::numeric as ""Valor""
from plantaopro.cliente_alertas a
join plantaopro.clientes c on c.id=a.cliente_id
where a.reg_status='A' and a.resolvido=false and a.tipo in ('UPGRADE','USO_ALTO','LIMITE')
order by a.reg_date desc", new { }, page, pageSize);

    [HttpGet("{tipo}/exportar")]
    public async Task<IActionResult> Exportar(string tipo)
    {
        var response = await ObterDadosAsync(tipo, new { }, 1, 500);
        if (!response.Success || response.Data is null) return StatusCode(response.StatusCode, response);
        await audit.RegistrarAsync(null, null, "RELATORIO_SAAS", Guid.Empty, "EXPORTAR", new { tipo }, true, HttpContext.Connection.RemoteIpAddress?.ToString(), "ADMINISTRADOR_GLOBAL");
        var csv = new StringBuilder();
        csv.AppendLine("Cliente;Status;Classificacao;Quantidade;Valor;Competencia");
        foreach (var item in response.Data.Items)
        {
            csv.AppendLine($"{SanitizarCsv(item.ClienteNome)};{SanitizarCsv(item.Status)};{SanitizarCsv(item.Classificacao)};{item.Quantidade};{item.Valor};{item.Competencia}");
        }
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "relatorio-saas-" + tipo + ".csv");
    }

    private async Task<IActionResult> ExecutarRelatorioAsync(string tipo, string sql, object parametros, int page, int pageSize)
    {
        try
        {
            var p = Math.Max(1, page);
            var s = Math.Clamp(pageSize, 1, 100);
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var items = await cn.QueryAsync<RelatorioSaasLinhaDto>(sql + "\nlimit @s offset @offset", UnirParametros(parametros, new { s, offset = (p - 1) * s }));
            var total = items.LongCount();
            return Ok(ApiResponse<PagedResult<RelatorioSaasLinhaDto>>.Ok(new PagedResult<RelatorioSaasLinhaDto>(items, p, s, total), "Relatório SaaS carregado."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao carregar relatório SaaS {Tipo}", tipo);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar relatório SaaS.", 500));
        }
    }

    private async Task<ApiResponse<PagedResult<RelatorioSaasLinhaDto>>> ObterDadosAsync(string tipo, object parametros, int page, int pageSize)
    {
        var sql = tipo.ToLowerInvariant() == "clientes" ? @"select c.id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"", coalesce(c.status,'') as ""Status"", coalesce(c.saude_status,'') as ""Classificacao"", 1::bigint as ""Quantidade"", 0::numeric as ""Valor"" from plantaopro.clientes c where c.reg_status='A' order by c.reg_date desc" : @"select f.cliente_id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"", coalesce(f.status,'') as ""Status"", 'FATURA' as ""Classificacao"", 1::bigint as ""Quantidade"", f.valor as ""Valor"", f.competencia as ""Competencia"" from plantaopro.faturas_saas f join plantaopro.clientes c on c.id=f.cliente_id where f.reg_status='A' order by f.competencia desc";
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var p = Math.Max(1, page);
        var s = Math.Clamp(pageSize, 1, 500);
        var items = await cn.QueryAsync<RelatorioSaasLinhaDto>(sql + " limit @s offset @offset", new { s, offset = (p - 1) * s });
        return ApiResponse<PagedResult<RelatorioSaasLinhaDto>>.Ok(new PagedResult<RelatorioSaasLinhaDto>(items, p, s, items.LongCount()));
    }

    private static DynamicParameters UnirParametros(object a, object b)
    {
        var p = new DynamicParameters(a);
        p.AddDynamicParams(b);
        return p;
    }

    private static string SanitizarCsv(string value)
    {
        return (value ?? string.Empty).Replace(";", ",", StringComparison.OrdinalIgnoreCase).Replace("\r", " ", StringComparison.OrdinalIgnoreCase).Replace("\n", " ", StringComparison.OrdinalIgnoreCase);
    }
}
