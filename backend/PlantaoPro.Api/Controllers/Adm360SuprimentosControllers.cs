using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Api.Controllers;

[ApiController,Route("api/administrativo360/compras"),Authorize(Policy="Adm360.Compras")]
public sealed class Compras360Controller : ControllerBase
{
 private readonly IComprasRepository repository;
 private readonly ICurrentUserService current;
 public Compras360Controller(IComprasRepository repository, ICurrentUserService current) { this.repository=repository; this.current=current; }
 private (Guid Tenant,Guid User) Context()=> (current.TenantId??throw new UnauthorizedAccessException(),current.UserId??throw new UnauthorizedAccessException());
 [HttpGet] public async Task<IActionResult> Listar([FromQuery]string? fornecedor,[FromQuery]string? situacao,[FromQuery]DateOnly? inicio,[FromQuery]DateOnly? fim,CancellationToken ct){var x=Context();try{return Ok(await repository.ListarAsync(x.Tenant,fornecedor,situacao,inicio,fim,ct));}catch(ArgumentException ex){return BadRequest(new { error = ex.Message });}}
 [HttpPost] public async Task<IActionResult> Criar(CriarPedidoCommand command,CancellationToken ct){var x=Context();var id=await repository.CriarAsync(x.Tenant,x.User,command,ct);return CreatedAtAction(nameof(Listar),new{id});}
 [HttpPost("{id:guid}/aprovar")] public async Task<IActionResult> Aprovar(Guid id,[FromHeader(Name="Idempotency-Key")]string key,CancellationToken ct){var x=Context();await repository.AprovarAsync(x.Tenant,x.User,id,key,ct);return NoContent();}
 [HttpPost("recebimentos")] public async Task<IActionResult> Receber(ConfirmarRecebimentoCommand command,CancellationToken ct){var x=Context();return Ok(new{id=await repository.ReceberAsync(x.Tenant,x.User,command,ct)});}
 // R8-B4: cancelamento com compensação — 409 fundamentado quando há efeito que exige estorno/ajuste prévio.
 [HttpPost("{id:guid}/cancelar")] public async Task<IActionResult> Cancelar(Guid id,[FromBody]string motivo,CancellationToken ct){var x=Context();try{await repository.CancelarAsync(x.Tenant,x.User,id,motivo,ct);return NoContent();}catch(ArgumentException ex){return BadRequest(new { error = ex.Message });}catch(PlantaoPro.Domain.Administrativo360.Administrativo360BusinessException ex){return Conflict(new { message = ex.Message });}}
}
[ApiController,Route("api/administrativo360/estoque"),Authorize(Policy="Adm360.Estoque")]
public sealed class Estoque360Controller : ControllerBase
{
 private readonly IEstoqueRepository repository;
 private readonly ICurrentUserService current;
 public Estoque360Controller(IEstoqueRepository repository, ICurrentUserService current) { this.repository=repository; this.current=current; }
 private (Guid Tenant,Guid User) Context()=>(current.TenantId??throw new UnauthorizedAccessException(),current.UserId??throw new UnauthorizedAccessException());
 [HttpGet] public async Task<IActionResult> Consultar(string? busca,string? condicao,Guid? localId,CancellationToken ct){var x=Context();return Ok(await repository.ConsultarAsync(x.Tenant,busca,condicao,localId,ct));}
 [HttpPost("transferencias")] public async Task<IActionResult> Transferir(TransferirCommand command,CancellationToken ct){var x=Context();await repository.TransferirAsync(x.Tenant,x.User,command,ct);return NoContent();}
 [HttpPost("reservas")] public async Task<IActionResult> Reservar(ReservarCommand command,CancellationToken ct){var x=Context();await repository.ReservarAsync(x.Tenant,x.User,command,ct);return NoContent();}
}
[ApiController,Route("api/administrativo360/qualidade"),Authorize(Policy="Adm360.Qualidade")]
public sealed class Qualidade360Controller : ControllerBase
{
 private readonly IQualidadeRepository repository;
 private readonly ICurrentUserService current;
 public Qualidade360Controller(IQualidadeRepository repository, ICurrentUserService current) { this.repository=repository; this.current=current; }
 private (Guid Tenant,Guid User) Context()=>(current.TenantId??throw new UnauthorizedAccessException(),current.UserId??throw new UnauthorizedAccessException());
 [HttpGet("pendentes")] public async Task<IActionResult> Pendentes(CancellationToken ct){var x=Context();return Ok(await repository.PendentesAsync(x.Tenant,ct));}
 [HttpPost("decisoes")] public async Task<IActionResult> Decidir(DecidirInspecaoCommand command,CancellationToken ct){var x=Context();await repository.DecidirAsync(x.Tenant,x.User,command,ct);return NoContent();}
}
// A12 — Ocorrências/Não conformidades (writer de adm360_ocorrencias, rodada 10 r10b1).
// Mesma política de qualidade (ADM360:LIBERAR_QUALIDADE): o estoquista puro não trata ocorrências.
[ApiController,Route("api/administrativo360/ocorrencias"),Authorize(Policy="Adm360.Qualidade")]
public sealed class Ocorrencias360Controller : ControllerBase
{
 private readonly IOcorrenciasRepository repository;
 private readonly ICurrentUserService current;
 public Ocorrencias360Controller(IOcorrenciasRepository repository, ICurrentUserService current) { this.repository=repository; this.current=current; }
 private (Guid Tenant,Guid User) Context()=>(current.TenantId??throw new UnauthorizedAccessException(),current.UserId??throw new UnauthorizedAccessException());
 [HttpGet] public async Task<IActionResult> Listar([FromQuery]string? situacao,CancellationToken ct){var x=Context();return Ok(await repository.ListarAsync(x.Tenant,situacao,ct));}
 [HttpPost] public async Task<IActionResult> Abrir(CriarOcorrenciaCommand command,CancellationToken ct){var x=Context();try{return Ok(new{id=await repository.AbrirAsync(x.Tenant,x.User,command,ct)});}catch(ArgumentException ex){return BadRequest(new { error = ex.Message });}}
 [HttpPost("{id:guid}/tratar")] public async Task<IActionResult> Tratar(Guid id,[FromBody]VersaoOcorrenciaCommand command,CancellationToken ct){var x=Context();try{await repository.TratarAsync(x.Tenant,x.User,id,command.Versao,ct);return NoContent();}catch(ArgumentException ex){return BadRequest(new { error = ex.Message });}catch(PlantaoPro.Domain.Administrativo360.Administrativo360BusinessException ex){return Conflict(new { message = ex.Message });}}
 [HttpPost("{id:guid}/resolver")] public async Task<IActionResult> Resolver(Guid id,[FromBody]VersaoOcorrenciaCommand command,CancellationToken ct){var x=Context();try{await repository.ResolverAsync(x.Tenant,x.User,id,command.Versao,ct);return NoContent();}catch(ArgumentException ex){return BadRequest(new { error = ex.Message });}catch(PlantaoPro.Domain.Administrativo360.Administrativo360BusinessException ex){return Conflict(new { message = ex.Message });}}
 [HttpPost("{id:guid}/encerrar")] public async Task<IActionResult> Encerrar(Guid id,[FromBody]EncerrarOcorrenciaCommand command,CancellationToken ct){var x=Context();try{await repository.EncerrarAsync(x.Tenant,x.User,id,command,ct);return NoContent();}catch(ArgumentException ex){return BadRequest(new { error = ex.Message });}catch(PlantaoPro.Domain.Administrativo360.Administrativo360BusinessException ex){return Conflict(new { message = ex.Message });}}
}
[ApiController,Route("api/administrativo360/coleta"),Authorize(Policy="Adm360.Estoque")]
public sealed class Coleta360Controller : ControllerBase
{
 private readonly IColetaRepository repository;
 private readonly ICurrentUserService current;
 public Coleta360Controller(IColetaRepository repository, ICurrentUserService current) { this.repository=repository; this.current=current; }
 private (Guid Tenant,Guid User) Context()=>(current.TenantId??throw new UnauthorizedAccessException(),current.UserId??throw new UnauthorizedAccessException());
 [HttpGet("tarefas")] public async Task<IActionResult> Tarefas(CancellationToken ct){var x=Context();return Ok(await repository.TarefasAsync(x.Tenant,x.User,ct));}
 [HttpPost("leituras")] public async Task<IActionResult> Registrar(RegistrarLeituraCommand command,CancellationToken ct){var x=Context();await repository.RegistrarAsync(x.Tenant,x.User,command,ct);return NoContent();}
}
[ApiController,Route("api/administrativo360/inventarios"),Authorize(Policy="Adm360.Estoque")]
public sealed class Inventarios360Controller : ControllerBase
{
 private readonly IInventarioRepository repository; private readonly ICurrentUserService current;
 public Inventarios360Controller(IInventarioRepository repository,ICurrentUserService current){this.repository=repository;this.current=current;}
 private (Guid Tenant,Guid User) Context()=>(current.TenantId??throw new UnauthorizedAccessException(),current.UserId??throw new UnauthorizedAccessException());
 [HttpGet] public async Task<IActionResult> Listar(CancellationToken ct){var x=Context();return Ok(await repository.ListarAsync(x.Tenant,ct));}
 [HttpPost] public async Task<IActionResult> Abrir(AbrirInventarioCommand command,CancellationToken ct){var x=Context();return Ok(new{id=await repository.AbrirAsync(x.Tenant,x.User,command,ct)});}
 [HttpPut("{id:guid}/contagens")] public async Task<IActionResult> Contar(Guid id,ContarInventarioCommand command,CancellationToken ct){var x=Context();await repository.ContarAsync(x.Tenant,x.User,id,command,ct);return NoContent();}
 [HttpPost("{id:guid}/aprovar"),Authorize(Policy="Adm360.InventarioAprovar")] public async Task<IActionResult> Aprovar(Guid id,[FromForm]string justificativa,[FromHeader(Name="Idempotency-Key")]string key,CancellationToken ct){var x=Context();await repository.AprovarAsync(x.Tenant,x.User,id,justificativa,key,ct);return NoContent();}
 [HttpPost("{id:guid}/cancelar")] public async Task<IActionResult> Cancelar(Guid id,[FromForm]string motivo,CancellationToken ct){var x=Context();await repository.CancelarAsync(x.Tenant,x.User,id,motivo,ct);return NoContent();}
}
