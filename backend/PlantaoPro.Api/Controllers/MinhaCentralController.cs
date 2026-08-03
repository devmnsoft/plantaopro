using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using PlantaoPro.Api.Operation360.WorkItems;
namespace PlantaoPro.Api.Controllers;
[ApiController, Authorize, Route("api/minha-central")]
public sealed class MinhaCentralController : ControllerBase { [HttpGet] public async Task<IActionResult> Get([FromServices] IWorkItemService service, CancellationToken ct) => Ok(await service.CentralAsync(ct)); }
