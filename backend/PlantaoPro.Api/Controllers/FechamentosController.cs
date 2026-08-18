using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
namespace PlantaoPro.Api.Controllers;
[ApiController, Route("api/fechamentos"), Authorize(Roles = RolesConstants.PlantoesGestao)]
public sealed class FechamentosController(FechamentoOperacionalService service) : ControllerBase
{
 private IActionResult R<T>(ApiResponse<T> x)=>StatusCode(x.StatusCode,x);
 [HttpGet] public async Task<IActionResult> List()=>R(await service.ListAsync());
 [HttpGet("pendentes")] public async Task<IActionResult> Pending()=>R(await service.ListAsync(true));
 [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id)=>R(await service.GetAsync(id));
 [HttpGet("{id:guid}/timeline")] public async Task<IActionResult> Timeline(Guid id)=>R(await service.TimelineAsync(id));
 [HttpPost("plantao/{plantaoId:guid}/gerar")] public async Task<IActionResult> Generate(Guid plantaoId)=>R(await service.GenerateAsync(plantaoId));
 [HttpPost("{id:guid}/iniciar-conferencia")] public async Task<IActionResult> Start(Guid id)=>R(await service.StartAsync(id));
 [HttpPost("{id:guid}/concluir-conferencia")] public async Task<IActionResult> Finish(Guid id)=>R(await service.FinishAsync(id));
 [HttpPost("{id:guid}/divergencias")] public async Task<IActionResult> Divergence(Guid id,CriarDivergenciaRequest x)=>R(await service.AddDivergenceAsync(id,x));
 [HttpPost("{id:guid}/divergencias/{divergenciaId:guid}/resolver")] public async Task<IActionResult> Resolve(Guid id,Guid divergenciaId,ResolverDivergenciaRequest x)=>R(await service.ResolveDivergenceAsync(id,divergenciaId,x.Resolucao));
 [HttpPost("{id:guid}/aprovar"),Authorize(Roles=RolesConstants.FinanceiroGestao)] public async Task<IActionResult> Approve(Guid id)=>R(await service.ApproveAsync(id));
 [HttpPost("{id:guid}/devolver"),Authorize(Roles=RolesConstants.FinanceiroGestao)] public async Task<IActionResult> Return(Guid id,DevolverFechamentoRequest x)=>R(await service.ReturnAsync(id,x.Motivo));
 [HttpPost("{id:guid}/gerar-financeiro"),Authorize(Roles=RolesConstants.FinanceiroGestao)] public async Task<IActionResult> Financial(Guid id)=>R(await service.GenerateFinancialAsync(id));
}
