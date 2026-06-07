using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using PlantaoPro.Api;

namespace PlantaoPro.Api.Controllers
{
    [ApiController]
    [Route("api/hospitais")]
    public class HospitaisController : ControllerBase
    {
        private readonly HospitalService service;
        private readonly AssinaturaGuardService assinaturaGuard;
        private readonly UsuarioContextService usuarioContext;

        public HospitaisController(HospitalService service, AssinaturaGuardService assinaturaGuard, UsuarioContextService usuarioContext)
        {
            this.service = service;
            this.assinaturaGuard = assinaturaGuard;
            this.usuarioContext = usuarioContext;
        }
        [Authorize(Roles = RolesConstants.CadastrosCoordenacao)]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var r = await service.GetAllAsync();
            return StatusCode(r.StatusCode, r);
        }
        [Authorize(Roles = RolesConstants.CadastrosCoordenacao)]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var r = await service.GetByIdAsync(id);
            return StatusCode(r.StatusCode, r);
        }
        [Authorize(Roles = RolesConstants.CadastrosCoordenacao)]
        [HttpPost]
        public async Task<IActionResult> Create(CreateHospitalRequest req)
        {
            var clienteId = usuarioContext.GetClienteId();
            if (clienteId.HasValue)
            {
                var permissao = await assinaturaGuard.PodeCadastrarHospitalAsync(clienteId.Value);
                if (!permissao.Success) return StatusCode(permissao.StatusCode, permissao);
            }

            var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
            var r = await service.CreateAsync(req, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
            if (r.Success && clienteId.HasValue) await assinaturaGuard.RegistrarUsoAsync(clienteId.Value, "HOSPITAIS", 1);
            return StatusCode(r.StatusCode, r);
        }
        [Authorize(Roles = RolesConstants.CadastrosCoordenacao)]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpdateHospitalRequest req)
        {
            var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
            var r = await service.UpdateAsync(id, req, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
            return StatusCode(r.StatusCode, r);
        }
        [Authorize(Roles = RolesConstants.CadastrosCoordenacao)]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
            var r = await service.DeleteAsync(id, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
            return StatusCode(r.StatusCode, r);
        }
    }
}
