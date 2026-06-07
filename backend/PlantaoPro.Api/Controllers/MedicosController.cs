using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using PlantaoPro.Api;

namespace PlantaoPro.Api.Controllers
{
[ApiController]
[Route("api/medicos")]
public class MedicosController : ControllerBase
{
    private readonly MedicoService service;
    private readonly AssinaturaGuardService assinaturaGuard;
    private readonly UsuarioContextService usuarioContext;
    private readonly ILogger<MedicosController> logger;

    public MedicosController(MedicoService service, AssinaturaGuardService assinaturaGuard, UsuarioContextService usuarioContext, ILogger<MedicosController> logger)
    {
        this.service = service;
        this.assinaturaGuard = assinaturaGuard;
        this.usuarioContext = usuarioContext;
        this.logger = logger;
    }

    [Authorize(Roles = RolesConstants.CadastrosOperacao)]
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var r = await service.ListarAsync();
            return StatusCode(r.StatusCode, r);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao listar médicos");
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível listar médicos.", 500));
        }
    }

    [Authorize(Roles = RolesConstants.CadastrosOperacao)]
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
            logger.LogError(ex, "Erro ao carregar médico {Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível carregar o médico.", 500));
        }
    }

    [Authorize(Roles = RolesConstants.CadastrosOperacao)]
    [HttpPost("cadastro")]
    public async Task<IActionResult> Cadastro([FromBody] CreateMedicoRequest req)
    {
        try
        {
            var clienteId = usuarioContext.GetClienteId();
            if (clienteId.HasValue)
            {
                var permissao = await assinaturaGuard.PodeCadastrarMedicoAsync(clienteId.Value);
                if (!permissao.Success) return StatusCode(permissao.StatusCode, permissao);
            }

            var uid = Guid.Parse(User.Claims.First(x => x.Type == "uid").Value);
            var r = await service.CriarAsync(req, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
            if (r.Success && clienteId.HasValue) await assinaturaGuard.RegistrarUsoAsync(clienteId.Value, "MEDICOS", 1);
            return StatusCode(r.StatusCode, r);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao criar médico");
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível criar o médico.", 500));
        }
    }

    [Authorize(Roles = RolesConstants.CadastrosOperacao)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMedicoRequest req)
    {
        try
        {
            var uid = Guid.Parse(User.Claims.First(x => x.Type == "uid").Value);
            var r = await service.AtualizarAsync(id, req, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
            return StatusCode(r.StatusCode, r);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao atualizar médico {Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível atualizar o médico.", 500));
        }
    }

    [Authorize(Roles = RolesConstants.CadastrosOperacao)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var uid = Guid.Parse(User.Claims.First(x => x.Type == "uid").Value);
            var r = await service.InativarAsync(id, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
            return StatusCode(r.StatusCode, r);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao inativar médico {Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível inativar o médico.", 500));
        }
    }
}
}
