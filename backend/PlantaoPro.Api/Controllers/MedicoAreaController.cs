using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = RolesConstants.Medico)]
[Route("api/medico-area")]
public class MedicoAreaController : ControllerBase
{
    private readonly MedicoAreaService service;
    public MedicoAreaController(MedicoAreaService service){this.service=service;}
    private Guid Uid()=>Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);

    [HttpGet("resumo")] public async Task<IActionResult> Resumo(){ var r = await service.ResumoAsync(Uid()); return StatusCode(r.StatusCode, r); }
    [HttpGet("plantoes-disponiveis")] public async Task<IActionResult> PlantoesDisponiveis([FromQuery]int page=1,[FromQuery]int pageSize=20){ var r = await service.PlantoesDisponiveisAsync(Uid(),page,pageSize); return StatusCode(r.StatusCode, r); }
    [HttpGet("plantoes-recomendados")] public async Task<IActionResult> PlantoesRecomendados([FromQuery]int top=5){ var r = await service.PlantoesRecomendadosAsync(Uid(),top); return StatusCode(r.StatusCode, r); }
    [HttpGet("minhas-escalas")] public async Task<IActionResult> MinhasEscalas([FromQuery]int page=1,[FromQuery]int pageSize=20){ var r = await service.MinhasEscalasAsync(Uid(),page,pageSize); return StatusCode(r.StatusCode, r); }
    [HttpGet("meus-pagamentos")] public async Task<IActionResult> MeusPagamentos([FromQuery]int page=1,[FromQuery]int pageSize=20){ var r = await service.MeusPagamentosAsync(Uid(),page,pageSize); return StatusCode(r.StatusCode, r); }
}
