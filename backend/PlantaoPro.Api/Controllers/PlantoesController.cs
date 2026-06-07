using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers
{
    [ApiController]
    [Route("api/plantoes")]
    public class PlantoesController : ControllerBase
    {
        private readonly PlantaoService service;
        private readonly MedicoRecomendacaoService recomendacaoService;
        private readonly ILogger<PlantoesController> logger;
        private readonly AssinaturaGuardService assinaturaGuard;
        private readonly UsuarioContextService usuarioContext;

        public PlantoesController(PlantaoService service, MedicoRecomendacaoService recomendacaoService, AssinaturaGuardService assinaturaGuard, UsuarioContextService usuarioContext, ILogger<PlantoesController> logger)
        {
            this.service = service;
            this.recomendacaoService = recomendacaoService;
            this.assinaturaGuard = assinaturaGuard;
            this.usuarioContext = usuarioContext;
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

        [Authorize(Roles = RolesConstants.Medico + "," + RolesConstants.PlantoesGestao)]
        [HttpGet("disponiveis")]
        public async Task<IActionResult> Disponiveis([FromQuery] PlantaoFilterRequest filter)
        {
            try
            {
                var r = await service.GetAllAsync(filter with { Status = "aberto" });
                return StatusCode(r.StatusCode, r);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao listar plantões disponíveis");
                return StatusCode(500, ApiResponse<string>.Fail("Erro ao listar plantões disponíveis.", 500));
            }
        }

        [Authorize(Roles = RolesConstants.PlantoesGestao)]
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
                logger.LogError(ex, "Erro ao obter plantão {PlantaoId}", id);
                return StatusCode(500, ApiResponse<string>.Fail("Erro ao obter plantão.", 500));
            }
        }

        [Authorize(Roles = RolesConstants.PlantoesGestao)]
        [HttpPost]
        public async Task<IActionResult> Create(CreatePlantaoRequest req)
        {
            try
            {
                var uid = GetUserId();
                var r = await service.CreateAsync(req, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
                return StatusCode(r.StatusCode, r);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao criar plantão");
                return StatusCode(500, ApiResponse<string>.Fail("Erro ao criar plantão.", 500));
            }
        }

        [Authorize(Roles = RolesConstants.PlantoesGestao)]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpdatePlantaoRequest req)
        {
            try
            {
                var uid = GetUserId();
                var r = await service.UpdateAsync(id, req, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
                return StatusCode(r.StatusCode, r);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao atualizar plantão {PlantaoId}", id);
                return StatusCode(500, ApiResponse<string>.Fail("Erro ao atualizar plantão.", 500));
            }
        }

        [Authorize(Roles = RolesConstants.PlantoesGestao)]
        [HttpPost("{id:guid}/publicar")]
        public Task<IActionResult> Publicar(Guid id, [FromBody] StatusRequest req)
        {
            return ChangeStatus(id, "aberto", req.Justificativa, "publicar plantão");
        }

        [Authorize(Roles = RolesConstants.PlantoesGestao)]
        [HttpPost("{id:guid}/cancelar")]
        public Task<IActionResult> Cancelar(Guid id, [FromBody] StatusRequest req)
        {
            return ChangeStatus(id, "cancelado", req.Justificativa, "cancelar plantão");
        }

        [Authorize(Roles = RolesConstants.PlantoesGestao)]
        [HttpPost("{id:guid}/realizar")]
        [HttpPost("{id:guid}/encerrar")]
        public Task<IActionResult> Realizar(Guid id, [FromBody] StatusRequest req)
        {
            return ChangeStatus(id, "realizado", req.Justificativa, "realizar plantão");
        }

        [Authorize(Roles = RolesConstants.PlantoesGestao)]
        [HttpGet("{id:guid}/historico")]
        public async Task<IActionResult> Historico(Guid id)
        {
            try
            {
                var r = await service.ListarHistoricoAsync(id);
                return StatusCode(r.StatusCode, r);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao listar histórico do plantão {PlantaoId}", id);
                return StatusCode(500, ApiResponse<string>.Fail("Erro ao listar histórico do plantão.", 500));
            }
        }

        [Authorize(Roles = RolesConstants.PlantoesGestao)]
        [HttpGet("{id:guid}/escalas")]
        public async Task<IActionResult> Escalas(Guid id)
        {
            try
            {
                var r = await service.ListarEscalasAsync(id);
                return StatusCode(r.StatusCode, r);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao listar escalas do plantão {PlantaoId}", id);
                return StatusCode(500, ApiResponse<string>.Fail("Erro ao listar escalas do plantão.", 500));
            }
        }

        [Authorize(Roles = RolesConstants.PlantoesGestao)]
        [HttpGet("{id:guid}/convites")]
        public async Task<IActionResult> Convites(Guid id)
        {
            try
            {
                var r = await service.ListarConvitesAsync(id);
                return StatusCode(r.StatusCode, r);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao listar convites do plantão {PlantaoId}", id);
                return StatusCode(500, ApiResponse<string>.Fail("Erro ao listar convites do plantão.", 500));
            }
        }

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
                var clienteId = usuarioContext.GetClienteId();
                if (clienteId.HasValue)
                {
                    var permissaoPlano = await assinaturaGuard.PodeEnviarConviteAsync(clienteId.Value);
                    if (!permissaoPlano.Success) return StatusCode(permissaoPlano.StatusCode, permissaoPlano);
                }

                var uid = GetUserId();
                var response = await recomendacaoService.ConvidarRecomendadosAsync(id, request.MedicoIds, request.Mensagem, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
                if (response.Success && clienteId.HasValue) await assinaturaGuard.RegistrarUsoAsync(clienteId.Value, "CONVITES", request.MedicoIds?.Count() ?? 0);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao convidar médicos recomendados para plantão {PlantaoId}", id);
                return StatusCode(500, ApiResponse<string>.Fail("Erro ao convidar médicos recomendados.", 500));
            }
        }

        private async Task<IActionResult> ChangeStatus(Guid id, string status, string justificativa, string acao)
        {
            try
            {
                var uid = GetUserId();
                if (string.Equals(status, "aberto", StringComparison.OrdinalIgnoreCase))
                {
                    var clienteId = usuarioContext.GetClienteId();
                    if (clienteId.HasValue)
                    {
                        var permissaoPlano = await assinaturaGuard.PodePublicarPlantao(clienteId.Value);
                        if (!permissaoPlano.Success) return StatusCode(permissaoPlano.StatusCode, permissaoPlano);
                    }
                }

                var r = await service.ChangeStatusAsync(id, status, justificativa, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
                return StatusCode(r.StatusCode, r);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao {Acao} {PlantaoId}", acao, id);
                return StatusCode(500, ApiResponse<string>.Fail("Erro ao alterar status do plantão.", 500));
            }
        }

        private Guid GetUserId()
        {
            return Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
        }
    }
}
