using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class EscalasController : ControllerBase
    {
        private readonly EscalaService service;
        private readonly ILogger<EscalasController> logger;

        public EscalasController(EscalaService service, ILogger<EscalasController> logger)
        {
            this.service = service;
            this.logger = logger;
        }

        [Authorize(Roles = RolesConstants.EscalasGestao)]
        [HttpGet("escalas")]
        public async Task<IActionResult> Listar([FromQuery] EscalaFilterRequest f)
        {
            var r = await service.ListarAsync(f);
            return StatusCode(r.StatusCode, r);
        }

        [Authorize(Roles = RolesConstants.EscalasGestao)]
        [HttpGet("escalas/{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var r = await service.GetByIdAsync(id);
            return StatusCode(r.StatusCode, r);
        }

        [Authorize(Roles = RolesConstants.Medico)]
        [HttpGet("medicos/me/plantoes")]
        public async Task<IActionResult> Meus([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var uid = GetUserId();
            var r = await service.ListarPorMedicoUsuarioAsync(uid, page, pageSize);
            return StatusCode(r.StatusCode, r);
        }

        [Authorize(Roles = RolesConstants.Medico)]
        [HttpPost("plantoes/{id:guid}/aceitar")]
        public Task<IActionResult> Aceitar(Guid id, [FromBody] AcceptPlantaoRequest req)
        {
            return SolicitarPlantao(id, req.MedicoId, "aceitar plantão");
        }

        [Authorize(Roles = RolesConstants.Medico)]
        [HttpPost("plantoes/{id:guid}/solicitar")]
        public Task<IActionResult> Solicitar(Guid id, [FromBody] SolicitarPlantaoRequest req)
        {
            return SolicitarPlantao(id, req.MedicoId, "solicitar plantão");
        }

        [Authorize(Roles = RolesConstants.EscalasGestao)]
        [HttpPost("escalas/{id:guid}/confirmar")]
        public Task<IActionResult> Confirmar(Guid id, [FromBody] ConfirmEscalaRequest req)
        {
            return AlterarStatus(id, "confirmado", req.Justificativa, null, "confirmar escala");
        }

        [Authorize]
        [HttpPost("escalas/{id:guid}/recusar")]
        public Task<IActionResult> Recusar(Guid id, [FromBody] RecusarEscalaRequest req)
        {
            return AlterarStatus(id, "recusado", req.Justificativa, null, "recusar escala");
        }

        [Authorize]
        [HttpPost("escalas/{id:guid}/cancelar")]
        public Task<IActionResult> Cancelar(Guid id, [FromBody] CancelarEscalaRequest req)
        {
            return AlterarStatus(id, "cancelado", req.Justificativa, null, "cancelar escala");
        }

        [Authorize]
        [HttpPost("escalas/{id:guid}/substituir")]
        public Task<IActionResult> Substituir(Guid id, [FromBody] SubstituirEscalaRequest req)
        {
            return AlterarStatus(id, "substituido", req.Justificativa, req.NovoMedicoId, "substituir escala");
        }

        [Authorize]
        [HttpPost("escalas/{id:guid}/realizar")]
        [HttpPost("escalas/{id:guid}/marcar-realizado")]
        public Task<IActionResult> Realizado(Guid id, [FromBody] CompleteEscalaRequest req)
        {
            return AlterarStatus(id, "realizado", req.Justificativa, null, "marcar escala realizada");
        }

        [Authorize(Roles = RolesConstants.EscalasGestao)]
        [HttpPost("escalas/{id:guid}/nao-compareceu")]
        public Task<IActionResult> NaoCompareceu(Guid id, [FromBody] CompleteEscalaRequest req)
        {
            return AlterarStatus(id, "nao_compareceu", req.Justificativa, null, "marcar não comparecimento");
        }

        private async Task<IActionResult> SolicitarPlantao(Guid plantaoId, Guid medicoId, string acao)
        {
            try
            {
                var uid = GetUserId();
                var r = await service.AceitarAsync(plantaoId, medicoId, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
                return StatusCode(r.StatusCode, r);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao {Acao} {PlantaoId}", acao, plantaoId);
                return StatusCode(500, ApiResponse<string>.Fail("Erro ao processar solicitação de plantão.", 500));
            }
        }

        private async Task<IActionResult> AlterarStatus(Guid escalaId, string status, string? justificativa, Guid? novoMedicoId, string acao)
        {
            try
            {
                var uid = GetUserId();
                var r = await service.AlterarStatusAsync(escalaId, status, justificativa, uid, novoMedicoId, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
                return StatusCode(r.StatusCode, r);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao {Acao} {EscalaId}", acao, escalaId);
                return StatusCode(500, ApiResponse<string>.Fail("Erro ao alterar status da escala.", 500));
            }
        }

        private Guid GetUserId()
        {
            return Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
        }
    }
}
