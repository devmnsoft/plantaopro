using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;
using PlantaoPro.Api.Operation360.WorkItems;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/lookups")]
public sealed class LookupsController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public LookupsController(Saude360ClinicalService service) { this.service = service; }

    [HttpGet("pacientes")] public async Task<IActionResult> Pacientes([FromQuery] string? termo, [FromQuery] string? term) { return await Lookup("pacientes", NormalizeTerm(termo, term)); }
    [HttpGet("medicos")] public Task<IActionResult> Medicos([FromQuery] string? termo, [FromQuery] string? term, [FromQuery] int limite = 50) => LookupEntidade("medicos", NormalizeTerm(termo, term), limite);
    [HttpGet("hospitais")] public Task<IActionResult> Hospitais([FromQuery] string? termo, [FromQuery] string? term, [FromQuery] int limite = 50) => LookupEntidade("hospitais", NormalizeTerm(termo, term), limite);
    [HttpGet("unidades")] public Task<IActionResult> Unidades([FromQuery] string? termo, [FromQuery] int limite = 50) => LookupEntidade("unidades", NormalizeTerm(termo, null), limite);
    [HttpGet("especialidades")] public Task<IActionResult> Especialidades([FromQuery] string? termo, [FromQuery] string? term, [FromQuery] int limite = 50) => LookupEntidade("especialidades", NormalizeTerm(termo, term), limite);
    [HttpGet("salas")] public Task<IActionResult> Salas([FromQuery] string? termo, [FromQuery] int limite = 50) => LookupEntidade("salas", NormalizeTerm(termo, null), limite);
    [HttpGet("convenios")] public async Task<IActionResult> Convenios([FromQuery] string? termo, [FromQuery] string? term) { return await Lookup("convenios", NormalizeTerm(termo, term)); }
    [HttpGet("planos-saude")] public async Task<IActionResult> PlanosSaude([FromQuery] string? termo, [FromQuery] string? term) { return await Lookup("planosSaude", NormalizeTerm(termo, term)); }
    [HttpGet("agendamentos")] public async Task<IActionResult> Agendamentos([FromQuery] string? termo, [FromQuery] string? term) { return await Lookup("agendamentos", NormalizeTerm(termo, term)); }
    [HttpGet("consultas")] public async Task<IActionResult> Consultas([FromQuery] string? termo, [FromQuery] string? term) { return await Lookup("consultas", NormalizeTerm(termo, term)); }
    [HttpGet("cid")] public async Task<IActionResult> Cid([FromQuery] string? termo, [FromQuery] string? term) { return await Lookup("cid", NormalizeTerm(termo, term)); }
    [HttpGet("classificacoes-risco")] public IActionResult ClassificacoesRisco() { return Ok(ApiResponse<IEnumerable<LookupItemDto>>.Ok(ToItems(new List<string> { "EMERGENCIA", "MUITO_URGENTE", "URGENTE", "POUCO_URGENTE", "NAO_URGENTE" }), "Lookup carregado.")); }
    [HttpGet("formas-pagamento")] public IActionResult FormasPagamento() { return Ok(ApiResponse<IEnumerable<LookupItemDto>>.Ok(ToItems(new List<string> { "DINHEIRO", "PIX", "CARTAO_CREDITO", "CARTAO_DEBITO", "CONVENIO" }), "Lookup carregado.")); }
    [HttpGet("status-agendamento")] public IActionResult StatusAgendamento() { return Ok(ApiResponse<IEnumerable<LookupItemDto>>.Ok(ToItems(new List<string> { "AGENDADO", "CONFIRMADO", "CHECKIN_REALIZADO", "EM_TRIAGEM", "AGUARDANDO_CONSULTA", "ATENDIDO", "CANCELADO", "FALTOU" }), "Lookup carregado.")); }
    [HttpGet("status-triagem")] public IActionResult StatusTriagem() { return Ok(ApiResponse<IEnumerable<LookupItemDto>>.Ok(ToItems(new List<string> { "AGUARDANDO", "EM_TRIAGEM", "FINALIZADA", "CANCELADA" }), "Lookup carregado.")); }
    [HttpGet("status-consulta")] public IActionResult StatusConsulta() { return Ok(ApiResponse<IEnumerable<LookupItemDto>>.Ok(ToItems(new List<string> { "AGUARDANDO", "EM_ATENDIMENTO", "FINALIZADA", "CANCELADA" }), "Lookup carregado.")); }
    [HttpGet("status-financeiro")] public IActionResult StatusFinanceiro() { return Ok(ApiResponse<IEnumerable<LookupItemDto>>.Ok(ToItems(new List<string> { "ABERTA", "VENCIDA", "RECEBIDO", "CANCELADA", "ESTORNADO", "ABERTO", "FECHADO" }), "Lookup carregado.")); }

    private static string? NormalizeTerm(string? termo, string? term)
    {
        var value = !string.IsNullOrWhiteSpace(termo)
            ? termo
            : term;

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();

        return value.Length <= 120
            ? value
            : value.Substring(0, 120);
    }

    private async Task<IActionResult> Lookup(string key, string? termo)
    {
        var result = await service.ListarAsync(key, termo: termo);
        var itens = (result.Data ?? Array.Empty<Saude360RegistroDto>()).Take(50).Select(x => new LookupItemDto { Id = x.Id, Text = string.IsNullOrWhiteSpace(x.Nome) ? x.Descricao : x.Nome, Description = x.Descricao, Extra = x.Codigo, Status = x.Status }).ToList();
        return StatusCode(result.StatusCode, ApiResponse<IEnumerable<LookupItemDto>>.Ok(itens, result.Message));
    }

    private async Task<IActionResult> LookupEntidade(string entidade, string? termo, int limite)
    {
        var result = await service.ListarLookupEntidadesAsync(entidade, termo, limite);
        return StatusCode(result.StatusCode, result);
    }

    private static IEnumerable<LookupItemDto> ToItems(IEnumerable<string> values)
    {
        return values.Select(v => new LookupItemDto { Id = Guid.Empty, Text = v, Description = "Opção padrão do Saúde 360", Extra = string.Empty, Status = "ATIVO" }).ToList();
    }
}

[ApiController]
[Authorize]
[Route("api/pendencias-clinicas")]
public sealed class PendenciasClinicasApiController : ControllerBase
{
    private readonly IWorkItemService service;
    private readonly ICurrentUserService current;

    public PendenciasClinicasApiController(IWorkItemService service, ICurrentUserService current) { this.service = service; this.current = current; }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(ApiResponse<IReadOnlyList<WorkItemDto>>.Ok(await service.ListAsync(ct), "Pendências clínicas carregadas."));

    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo(CancellationToken ct) { var central = await service.CentralAsync(ct); return Ok(ApiResponse<CentralSummaryDto>.Ok(central.Summary, "Resumo de pendências carregado.")); }

    [HttpGet("minhas")]
    public async Task<IActionResult> Minhas(CancellationToken ct) { var central = await service.CentralAsync(ct); return Ok(ApiResponse<IReadOnlyList<WorkItemDto>>.Ok(central.Items.Where(x => x.ResponsavelId == current.UserId).ToList(), "Pendências atribuídas carregadas.")); }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) { var item = await service.GetAsync(id, ct); return item is null ? NotFound(ApiResponse<string>.Fail("Pendência não encontrada.", 404)) : Ok(ApiResponse<WorkItemDto>.Ok(item)); }

    [HttpPost("{id:guid}/assumir")]
    public async Task<IActionResult> Assumir(Guid id, [FromBody] WorkItemVersionRequest request, CancellationToken ct) => Mutation(await service.AssignAsync(id, Guid.Empty, request, ct));

    [HttpPost("{id:guid}/resolver")]
    public async Task<IActionResult> Resolver(Guid id, [FromBody] WorkItemVersionRequest request, CancellationToken ct)
    {
        var item = await service.GetAsync(id, ct);
        if (item is null) return NotFound(ApiResponse<string>.Fail("Pendência não encontrada.", 404));
        return Mutation(await service.MoveAsync(new WorkItemMoveRequest(id, item.Status, WorkItemStatus.Concluido, item.Posicao, request.Version, request.IdempotencyKey), ct));
    }

    [HttpPost("{id:guid}/adiar")]
    public async Task<IActionResult> Adiar(Guid id, [FromBody] WorkItemPostponeRequest request, CancellationToken ct) => Mutation(await service.PostponeAsync(id, request, ct));

    private IActionResult Mutation(WorkItemMutationResult result)
    {
        if (!result.Found) return NotFound(ApiResponse<string>.Fail("Pendência não encontrada.", 404));
        if (result.Conflict) return Conflict(ApiResponse<string>.Fail("A pendência foi alterada por outro usuário. Atualize os dados.", 409));
        return Ok(ApiResponse<WorkItemDto>.Ok(result.Item!, result.Duplicate ? "A operação já havia sido processada." : "Pendência atualizada com sucesso."));
    }
}

public sealed class LookupItemDto { public Guid Id { get; set; } public string Text { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public string Extra { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; }
