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
    private Guid Uid()
    {
        var uidClaim = User.FindFirst("uid")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        return Guid.TryParse(uidClaim, out var uid) ? uid : Guid.Empty;
    }

    [HttpGet("resumo")] public async Task<IActionResult> Resumo(){ var uid = Uid(); if(uid==Guid.Empty) return Unauthorized(ApiResponse<object>.Fail("Usuário não autenticado.",401)); var r = await service.ResumoAsync(uid); return StatusCode(r.StatusCode, r); }
    [HttpGet("plantoes-disponiveis")] public async Task<IActionResult> PlantoesDisponiveis([FromQuery]int page=1,[FromQuery]int pageSize=20){ var uid=Uid(); if(uid==Guid.Empty) return Unauthorized(ApiResponse<object>.Fail("Usuário não autenticado.",401)); var r = await service.PlantoesDisponiveisAsync(uid,page,pageSize); return StatusCode(r.StatusCode, r); }
    [HttpGet("plantoes-recomendados")] public async Task<IActionResult> PlantoesRecomendados([FromQuery]int top=5){ var uid=Uid(); if(uid==Guid.Empty) return Unauthorized(ApiResponse<object>.Fail("Usuário não autenticado.",401)); var r = await service.PlantoesRecomendadosAsync(uid,top); return StatusCode(r.StatusCode, r); }
    [HttpGet("minhas-escalas")] public async Task<IActionResult> MinhasEscalas([FromQuery]int page=1,[FromQuery]int pageSize=20){ var uid=Uid(); if(uid==Guid.Empty) return Unauthorized(ApiResponse<object>.Fail("Usuário não autenticado.",401)); var r = await service.MinhasEscalasAsync(uid,page,pageSize); return StatusCode(r.StatusCode, r); }
    [HttpGet("meus-pagamentos")] public async Task<IActionResult> MeusPagamentos([FromQuery]int page=1,[FromQuery]int pageSize=20){ var uid=Uid(); if(uid==Guid.Empty) return Unauthorized(ApiResponse<object>.Fail("Usuário não autenticado.",401)); var r = await service.MeusPagamentosAsync(uid,page,pageSize); return StatusCode(r.StatusCode, r); }
}
