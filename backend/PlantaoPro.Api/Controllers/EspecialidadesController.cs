using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using PlantaoPro.Api;

namespace PlantaoPro.Api.Controllers
{
[ApiController]
[Route("api/especialidades")]
public class EspecialidadesController : ControllerBase
{
    private readonly EspecialidadeService service;
    private readonly ILogger<EspecialidadesController> logger;

    public EspecialidadesController(EspecialidadeService service, ILogger<EspecialidadesController> logger)
    {
        this.service = service;
        this.logger = logger;
    }

    [Authorize(Roles = RolesConstants.CadastrosCoordenacao)]
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var r = await service.GetAllAsync();
            return StatusCode(r.StatusCode, r);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao listar especialidades");
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível listar especialidades.", 500));
        }
    }

    [Authorize(Roles = RolesConstants.CadastrosCoordenacao)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var r = await service.GetByIdAsync(id);
            return StatusCode(r.StatusCode, r);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao carregar especialidade {Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível carregar a especialidade.", 500));
        }
    }

    [Authorize(Roles = RolesConstants.CadastrosCoordenacao)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateEspecialidadeRequest req)
    {
        try
        {
            var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
            var r = await service.CreateAsync(req, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
            return StatusCode(r.StatusCode, r);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao criar especialidade");
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível criar a especialidade.", 500));
        }
    }

    [Authorize(Roles = RolesConstants.CadastrosCoordenacao)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateEspecialidadeRequest req)
    {
        try
        {
            var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
            var r = await service.UpdateAsync(id, req, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
            return StatusCode(r.StatusCode, r);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao atualizar especialidade {Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível atualizar a especialidade.", 500));
        }
    }

    [Authorize(Roles = RolesConstants.CadastrosCoordenacao)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
            var r = await service.DeleteAsync(id, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
            return StatusCode(r.StatusCode, r);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao inativar especialidade {Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível inativar a especialidade.", 500));
        }
    }
}
}
