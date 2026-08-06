using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = RolesConstants.PlantoesGestao)]
[Route("api/agenda")]
[Tags("Agenda operacional")]
public sealed class AgendaOperacionalController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly UsuarioContextService _context;
    private readonly PlantaoService _plantoes;

    public AgendaOperacionalController(IConfiguration configuration, UsuarioContextService context, PlantaoService plantoes)
    {
        _configuration = configuration;
        _context = context;
        _plantoes = plantoes;
    }

    [HttpGet]
    public async Task<IActionResult> Resumo([FromQuery] DateTime? inicio, [FromQuery] DateTime? fim)
    {
        await using var cn = Connection();
        var filtro = TenantFilter("p");
        var result = await cn.QuerySingleAsync<AgendaResumoDto>(@"select
 count(distinct p.id) as ""TotalEventos"",
 count(distinct p.id) filter (where upper(p.status) in ('ABERTO','PENDENTE')) as ""Criticos"",
 count(distinct c.id) filter (where c.status='ABERTO') as ""Conflitos"",
 count(distinct p.id) filter (where upper(p.status)='CONFIRMADO') as ""Confirmados""
from plantaopro.plantoes p
left join plantaopro.agenda_evento_conflitos c on c.evento_id=p.id and c.status='ABERTO'
where p.reg_status='A' and p.data_inicio >= @inicio and p.data_inicio < @fim" + filtro,
            Params(inicio ?? DateTime.UtcNow.Date, fim ?? DateTime.UtcNow.Date.AddDays(30)));
        return Ok(ApiResponse<AgendaResumoDto>.Ok(result));
    }

    [HttpGet("eventos")]
    public async Task<IActionResult> Eventos([FromQuery] DateTime? inicio, [FromQuery] DateTime? fim, [FromQuery] Guid? hospitalId, [FromQuery] Guid? medicoId, [FromQuery] Guid? especialidadeId, [FromQuery] string? status)
    {
        await using var cn = Connection();
        var eventos = await cn.QueryAsync<AgendaEventoDto>(@"select distinct p.id as ""Id"", p.data_inicio as ""Inicio"", p.data_fim as ""Fim"",
 h.nome_fantasia as ""Hospital"", e.nome as ""Especialidade"", p.status as ""Status"",
 coalesce(m.nome,'Sem médico confirmado') as ""Medico"", exists(select 1 from plantaopro.agenda_evento_conflitos c where c.evento_id=p.id and c.status='ABERTO') as ""TemConflito""
from plantaopro.plantoes p join plantaopro.hospitais h on h.id=p.hospital_id
join plantaopro.especialidades e on e.id=p.especialidade_id
left join plantaopro.escalas s on s.plantao_id=p.id and s.reg_status='A'
left join plantaopro.medicos m on m.id=s.medico_id
where p.reg_status='A' and p.data_inicio >= @inicio and p.data_inicio < @fim
and (@hospitalId is null or p.hospital_id=@hospitalId) and (@medicoId is null or m.id=@medicoId)
and (@especialidadeId is null or p.especialidade_id=@especialidadeId) and (@status is null or upper(p.status)=upper(@status))" + TenantFilter("p") + " order by p.data_inicio",
            MergeParams(inicio ?? DateTime.UtcNow.Date, fim ?? DateTime.UtcNow.Date.AddDays(30), hospitalId, medicoId, especialidadeId, status));
        return Ok(ApiResponse<IEnumerable<AgendaEventoDto>>.Ok(eventos));
    }

    [HttpGet("conflitos")]
    public async Task<IActionResult> Conflitos()
    {
        await using var cn = Connection();
        var itens = await cn.QueryAsync<AgendaConflitoDto>(@"select c.id as ""Id"", c.evento_id as ""EventoId"", c.tipo as ""Tipo"", c.severidade as ""Severidade"", c.descricao as ""Descricao"", c.criado_em as ""CriadoEm""
from plantaopro.agenda_evento_conflitos c join plantaopro.plantoes p on p.id=c.evento_id
where c.status='ABERTO'" + TenantFilter("p") + " order by c.criado_em desc", new { clienteId = _context.GetClienteId() });
        return Ok(ApiResponse<IEnumerable<AgendaConflitoDto>>.Ok(itens));
    }

    [HttpGet("medicos")]
    public Task<IActionResult> Medicos() => Lookup("medicos", "nome");

    [HttpGet("hospitais")]
    public Task<IActionResult> Hospitais() => Lookup("hospitais", "nome_fantasia");

    [HttpPost("eventos")]
    public async Task<IActionResult> Criar([FromBody] CreatePlantaoRequest request)
    {
        var result = await _plantoes.CreateAsync(request, _context.GetUsuarioId() ?? Guid.Empty, _context.GetIpOrigem(), Request.Headers.UserAgent.ToString());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("eventos/{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] UpdatePlantaoRequest request)
    {
        var result = await _plantoes.UpdateAsync(id, request, _context.GetUsuarioId() ?? Guid.Empty, _context.GetIpOrigem(), Request.Headers.UserAgent.ToString());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("eventos/{id:guid}/resolver-conflito")]
    public async Task<IActionResult> ResolverConflito(Guid id, [FromBody] ResolverConflitoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Resolucao)) return BadRequest(ApiResponse<object>.Fail("Informe a resolução adotada.", 400));
        await using var cn = Connection();
        var alterados = await cn.ExecuteAsync(@"update plantaopro.agenda_evento_conflitos c set status='RESOLVIDO', resolucao=@resolucao, resolvido_por=@usuarioId, resolvido_em=now()
from plantaopro.plantoes p where c.evento_id=p.id and c.id=@id and c.status='ABERTO'" + TenantFilter("p"), new { id, resolucao = request.Resolucao.Trim(), usuarioId = _context.GetUsuarioId(), clienteId = _context.GetClienteId() });
        return alterados == 0 ? NotFound(ApiResponse<object>.Fail("Conflito não encontrado neste tenant.", 404)) : Ok(ApiResponse<object>.Ok(new { id }, "Conflito resolvido."));
    }

    private async Task<IActionResult> Lookup(string table, string label)
    {
        await using var cn = Connection();
        var sql = "select id as \"Id\", " + label + " as \"Nome\" from plantaopro." + table + " x where reg_status='A'" + TenantFilter("x") + " order by " + label;
        var result = await cn.QueryAsync<AgendaLookupDto>(sql, new { clienteId = _context.GetClienteId() });
        return Ok(ApiResponse<IEnumerable<AgendaLookupDto>>.Ok(result));
    }

    private NpgsqlConnection Connection() => new(_configuration.GetConnectionString("Default"));
    private string TenantFilter(string alias) => _context.IsAdminGlobal() ? string.Empty : " and " + alias + ".cliente_id=@clienteId";
    private object Params(DateTime inicio, DateTime fim) => new { inicio, fim, clienteId = _context.GetClienteId() };
    private object MergeParams(DateTime inicio, DateTime fim, Guid? hospitalId, Guid? medicoId, Guid? especialidadeId, string? status) => new { inicio, fim, hospitalId, medicoId, especialidadeId, status, clienteId = _context.GetClienteId() };
}

public sealed record AgendaResumoDto(long TotalEventos, long Criticos, long Conflitos, long Confirmados);
public sealed record AgendaEventoDto(Guid Id, DateTime Inicio, DateTime Fim, string Hospital, string Especialidade, string Status, string Medico, bool TemConflito);
public sealed record AgendaConflitoDto(Guid Id, Guid EventoId, string Tipo, string Severidade, string Descricao, DateTime CriadoEm);
public sealed record AgendaLookupDto(Guid Id, string Nome);
public sealed record ResolverConflitoRequest(string Resolucao);
