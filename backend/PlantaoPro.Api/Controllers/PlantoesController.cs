using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using PlantaoPro.Api;

namespace PlantaoPro.Api.Controllers
{
[ApiController]
[Route("api/plantoes")]
public class PlantoesController : ControllerBase
{
    private readonly PlantaoService service;
    private readonly MedicoRecomendacaoService recomendacaoService;
    private readonly ILogger<PlantoesController> logger;

    public PlantoesController(PlantaoService service, MedicoRecomendacaoService recomendacaoService, ILogger<PlantoesController> logger)
    {
        this.service = service;
        this.recomendacaoService = recomendacaoService;
        this.logger = logger;
    }

    [Authorize(Roles = RolesConstants.PlantoesGestao)]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PlantaoFilterRequest filter)
    {
        try
        {
            var r = await service.GetAllAsync(filter);
            return StatusCode(r.StatusCode, r);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao listar plantões");
            return StatusCode(500, ApiResponse<string>.Fail("Erro ao listar plantões.", 500));
        }
    }

 [Authorize(Roles = RolesConstants.Medico + "," + RolesConstants.PlantoesGestao)][HttpGet("disponiveis")] public async Task<IActionResult> Disponiveis([FromQuery]PlantaoFilterRequest filter){var r=await service.GetAllAsync(filter with{Status="aberto"}); return StatusCode(r.StatusCode,r);} 
 [Authorize(Roles = RolesConstants.PlantoesGestao)][HttpGet("{id:guid}")] public async Task<IActionResult> GetById(Guid id){var r=await service.GetByIdAsync(id); return StatusCode(r.StatusCode,r);} 
 [Authorize(Roles = RolesConstants.PlantoesGestao)][HttpPost] public async Task<IActionResult> Create(CreatePlantaoRequest req){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.CreateAsync(req,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} 
 [Authorize(Roles = RolesConstants.PlantoesGestao)][HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id,UpdatePlantaoRequest req){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.UpdateAsync(id,req,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} 
 [Authorize(Roles = RolesConstants.PlantoesGestao)][HttpPost("{id:guid}/publicar")] public async Task<IActionResult> Publicar(Guid id,[FromBody]StatusRequest req){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.ChangeStatusAsync(id,"aberto",req.Justificativa,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} 
 [Authorize(Roles = RolesConstants.PlantoesGestao)][HttpPost("{id:guid}/cancelar")] public async Task<IActionResult> Cancelar(Guid id,[FromBody]StatusRequest req){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.ChangeStatusAsync(id,"cancelado",req.Justificativa,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} 
 [Authorize(Roles = RolesConstants.PlantoesGestao)][HttpPost("{id:guid}/encerrar")] public async Task<IActionResult> Encerrar(Guid id,[FromBody]StatusRequest req){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.ChangeStatusAsync(id,"realizado",req.Justificativa,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} 

 [Authorize(Roles = RolesConstants.PlantoesGestao)]
 [HttpGet("{id:guid}/medicos-recomendados")]
 public async Task<IActionResult> MedicosRecomendados(Guid id, [FromQuery] int limite = 20)
 {
     try
     {
         var recomendados = await recomendacaoService.RecomendarMedicosParaPlantaoAsync(id, limite);
         return Ok(ApiResponse<IEnumerable<MedicoRecomendadoDto>>.Ok(recomendados));
     }
     catch (Exception ex)
     {
         logger.LogError(ex, "Erro ao recomendar médicos para plantão {PlantaoId}", id);
         return StatusCode(500, ApiResponse<string>.Fail("Erro ao recomendar médicos para o plantão.", 500));
     }
 }

 [Authorize(Roles = RolesConstants.PlantoesGestao)]
 [HttpPost("{id:guid}/convidar-recomendados")]
 public async Task<IActionResult> ConvidarRecomendados(Guid id, [FromBody] ConvidarRecomendadosRequest request)
 {
     try
     {
         var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
         var response = await recomendacaoService.ConvidarRecomendadosAsync(id, request.MedicoIds, request.Mensagem, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
         return StatusCode(response.StatusCode, response);
     }
     catch (Exception ex)
     {
         logger.LogError(ex, "Erro ao convidar médicos recomendados para plantão {PlantaoId}", id);
         return StatusCode(500, ApiResponse<string>.Fail("Erro ao convidar médicos recomendados.", 500));
     }
 }

}
}
