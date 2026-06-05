using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers
{
    [ApiController]
    [Route("api/notificacoes")]
    public class NotificacoesController : ControllerBase
    {
        private readonly NotificacaoService service;
        private readonly ILogger<NotificacoesController> logger;

        public NotificacoesController(NotificacaoService service, ILogger<NotificacoesController> logger)
        {
            this.service = service;
            this.logger = logger;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] NotificationFilterRequest f)
        {
            try
            {
                var uid = GetUserId();
                var r = await service.ListarAsync(uid, f);
                return StatusCode(r.StatusCode, r);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao listar notificações do usuário autenticado");
                return StatusCode(500, ApiResponse<string>.Fail("Não foi possível listar notificações no momento.", 500));
            }
        }

        [Authorize]
        [HttpGet("nao-lidas")]
        public async Task<IActionResult> NaoLidas([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var uid = GetUserId();
                var filtro = new NotificationFilterRequest(null, false, null, null, page, pageSize);
                var r = await service.ListarAsync(uid, filtro);
                return StatusCode(r.StatusCode, r);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao listar notificações não lidas do usuário autenticado");
                return StatusCode(500, ApiResponse<string>.Fail("Não foi possível listar notificações não lidas no momento.", 500));
            }
        }

        [Authorize]
        [HttpPut("{id:guid}/lida")]
        public async Task<IActionResult> Lida(Guid id)
        {
            try
            {
                var uid = GetUserId();
                var r = await service.MarcarLidaAsync(uid, id, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
                return StatusCode(r.StatusCode, r);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao marcar notificação {NotificacaoId} como lida", id);
                return StatusCode(500, ApiResponse<string>.Fail("Não foi possível marcar a notificação como lida no momento.", 500));
            }
        }

        [Authorize]
        [HttpPut("lidas")]
        public async Task<IActionResult> Lidas()
        {
            try
            {
                var uid = GetUserId();
                var r = await service.MarcarTodasLidasAsync(uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
                return StatusCode(r.StatusCode, r);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao marcar todas as notificações como lidas");
                return StatusCode(500, ApiResponse<string>.Fail("Não foi possível marcar as notificações como lidas no momento.", 500));
            }
        }

        private Guid GetUserId()
        {
            return Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
        }
    }
}
