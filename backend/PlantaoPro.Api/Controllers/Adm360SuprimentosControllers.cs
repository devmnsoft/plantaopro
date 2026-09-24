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
 [HttpGet] public async Task<IActionResult> Listar([FromQuery]string? fornecedor,[FromQuery]string? situacao,[FromQuery]DateOnly? inicio,[FromQuery]DateOnly? fim,CancellationToken ct){var x=Context();return Ok(await repository.ListarAsync(x.Tenant,fornecedor,situacao,inicio,fim,ct));}
 [HttpPost] public async Task<IActionResult> Criar(CriarPedidoCommand command,CancellationToken ct){var x=Context();var id=await repository.CriarAsync(x.Tenant,x.User,command,ct);return CreatedAtAction(nameof(Listar),new{id});}
 [HttpPost("{id:guid}/aprovar")] public async Task<IActionResult> Aprovar(Guid id,[FromHeader(Name="Idempotency-Key")]string key,CancellationToken ct){var x=Context();await repository.AprovarAsync(x.Tenant,x.User,id,key,ct);return NoContent();}
 [HttpPost("recebimentos")] public async Task<IActionResult> Receber(ConfirmarRecebimentoCommand command,CancellationToken ct){var x=Context();return Ok(new{id=await repository.ReceberAsync(x.Tenant,x.User,command,ct)});}
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
