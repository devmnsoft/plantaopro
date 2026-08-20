using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;
using PlantaoPro.Api.SavedViews;

namespace PlantaoPro.Api.Controllers;

[ApiController, Authorize, Route("api/saved-views")]
public sealed class SavedViewsController : ControllerBase
{
    private readonly ISavedViewService service; public SavedViewsController(ISavedViewService service)=>this.service=service;
    [HttpGet] public async Task<IActionResult> List([FromQuery]string module,CancellationToken ct)=>await Execute(async()=>Ok(ApiResponse<IReadOnlyList<SavedViewDto>>.Ok(await service.ListAsync(module,ct))));
    [HttpPost] public async Task<IActionResult> Create([FromBody]SaveSavedViewRequest request,CancellationToken ct)=>await Execute(async()=>{var item=await service.CreateAsync(request,ct);return StatusCode(201,ApiResponse<SavedViewDto>.Ok(item,"Visão salva criada."));});
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id,[FromBody]UpdateSavedViewRequest request,CancellationToken ct)=>await Execute(async()=>{var item=await service.UpdateAsync(id,request,ct);return item is null?NotFound(ApiResponse<SavedViewDto>.Fail("Visão não encontrada.",404)):Ok(ApiResponse<SavedViewDto>.Ok(item));});
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id,CancellationToken ct)=>await Execute(async()=>await service.DeleteAsync(id,ct)?NoContent():NotFound(ApiResponse<object>.Fail("Visão não encontrada.",404)));
    [HttpPost("{id:guid}/default")] public async Task<IActionResult> SetDefault(Guid id,CancellationToken ct)=>await Execute(async()=>{var item=await service.SetDefaultAsync(id,ct);return item is null?NotFound(ApiResponse<SavedViewDto>.Fail("Visão não encontrada.",404)):Ok(ApiResponse<SavedViewDto>.Ok(item));});

    private static async Task<IActionResult> Execute(Func<Task<IActionResult>> action){try{return await action();}catch(SavedViewValidationException ex){return new UnprocessableEntityObjectResult(ApiResponse<object>.Fail(ex.Message,422));}catch(SavedViewConflictException ex){return new ConflictObjectResult(ApiResponse<object>.Fail(ex.Message,409));}catch(UnauthorizedAccessException ex){return new ObjectResult(ApiResponse<object>.Fail(ex.Message,403)){StatusCode=403};}}
}
