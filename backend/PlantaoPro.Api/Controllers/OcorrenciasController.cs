using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Api.Controllers;

[ApiController, Authorize, Route("api/ocorrencias")]
[Tags("Ocorrências operacionais")]
public sealed class OcorrenciasController : ControllerBase
{
    private readonly OcorrenciaService service;
    public OcorrenciasController(OcorrenciaService service) => this.service = service;
    [HttpGet] public async Task<IActionResult> Listar([FromQuery] OcorrenciaFiltro filtro,CancellationToken ct)=>Ok(await service.ListarAsync(filtro,ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Obter(Guid id,CancellationToken ct){var item=await service.ObterAsync(id,ct);return item is null?NotFound():Ok(item);}
    [HttpGet("{id:guid}/historico")] public async Task<IActionResult> Historico(Guid id,CancellationToken ct)=>await Result(()=>service.HistoricoAsync(id,ct));
    [HttpPost] public async Task<IActionResult> Criar([FromBody] CriarOcorrenciaRequest request,CancellationToken ct)
    { if(!ModelState.IsValid)return ValidationProblem(ModelState); var item=await service.CriarAsync(request,ct);return CreatedAtAction(nameof(Obter),new{id=item.Id},item); }
    [HttpPost("{id:guid}/atribuir")] public async Task<IActionResult> Atribuir(Guid id,[FromBody] AtribuirOcorrenciaRequest request,CancellationToken ct)=>await Result(async()=>{await service.AtribuirAsync(id,request,ct);return await service.ObterAsync(id,ct);});
    [HttpPost("{id:guid}/situacao")] public async Task<IActionResult> Transicionar(Guid id,[FromBody] TransicionarOcorrenciaRequest request,CancellationToken ct)=>await Result(async()=>{await service.TransicionarAsync(id,request,ct);return await service.ObterAsync(id,ct);});
    private async Task<IActionResult> Result<T>(Func<Task<T>> action){try{return Ok(await action());}catch(OcorrenciaConcurrencyException ex){return Conflict(new{code="STALE_VERSION",message=ex.Message});}catch(ArgumentException ex){return BadRequest(new{code="VALIDATION_ERROR",message=ex.Message});}catch(InvalidOperationException ex){return Conflict(new{code="INVALID_STATE",message=ex.Message});}catch(UnauthorizedAccessException){return Forbid();}catch(KeyNotFoundException){return NotFound();}}
}
