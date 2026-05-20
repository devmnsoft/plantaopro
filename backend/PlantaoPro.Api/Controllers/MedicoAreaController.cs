using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/medico-area")]
public class MedicoAreaController : ControllerBase
{
    private readonly MedicoAreaService service;
    public MedicoAreaController(MedicoAreaService service){this.service=service;}
    private Guid Uid()=>Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);

    [HttpGet("resumo")] public async Task<IActionResult> Resumo()=>StatusCode((await service.ResumoAsync(Uid())).StatusCode, await service.ResumoAsync(Uid()));
    [HttpGet("plantoes-disponiveis")] public async Task<IActionResult> PlantoesDisponiveis([FromQuery]int page=1,[FromQuery]int pageSize=20)=>StatusCode((await service.PlantoesDisponiveisAsync(Uid(),page,pageSize)).StatusCode, await service.PlantoesDisponiveisAsync(Uid(),page,pageSize));
}
