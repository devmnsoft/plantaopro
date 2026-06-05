using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using PlantaoPro.Api;
namespace PlantaoPro.Api.Controllers
{
    [ApiController]
    [Route("api/financeiro")]
    public class FinanceiroController : ControllerBase
    {
        private readonly FinanceiroService service;
        private readonly ILogger<FinanceiroController> logger;
        public FinanceiroController(FinanceiroService service, ILogger<FinanceiroController> logger)
        {
            this.service = service;
            this.logger = logger;
        }
        [Authorize(Roles = RolesConstants.FinanceiroGestao)]
        [HttpGet("pagamentos")]
        public async Task<IActionResult> Listar([FromQuery] PagamentoFilterRequest f)
        {
            try
            {
                var r = await service.ListarAsync(f);
                return StatusCode(r.StatusCode, r);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao listar pagamentos");
                return StatusCode(500, ApiResponse<string>.Fail("Erro ao listar pagamentos.", 500));
            }
        }
        [Authorize(Roles = RolesConstants.FinanceiroGestao)]
        [HttpGet("pagamentos/{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var r = await service.GetByIdAsync(id);
            return StatusCode(r.StatusCode, r);
        }
        [Authorize(Roles = RolesConstants.FinanceiroGestao)]
        [HttpPost("pagamentos/gerar")]
        public Task<IActionResult> Gerar([FromBody] GerarPagamentoRequest req)
        {
            return ExecutarAcaoCriticaAsync(
                "gerar pagamento",
                async uid => await service.GerarAsync(req, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()));
        }
        [Authorize(Roles = RolesConstants.FinanceiroGestao)]
        [HttpPost("pagamentos/{id:guid}/confirmar")]
        public Task<IActionResult> Confirmar(Guid id, [FromBody] ConfirmarPagamentoRequest req)
        {
            return ExecutarAcaoCriticaAsync(
                "confirmar pagamento",
                async uid => await service.ConfirmarAsync(id, req, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()),
                id);
        }
        [Authorize(Roles = RolesConstants.FinanceiroGestao)]
        [HttpPost("pagamentos/{id:guid}/cancelar")]
        public Task<IActionResult> Cancelar(Guid id, [FromBody] CancelarPagamentoRequest req)
        {
            return ExecutarAcaoCriticaAsync(
                "cancelar pagamento",
                async uid => await service.CancelarAsync(id, req.Justificativa, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()),
                id);
        }

        [Authorize(Roles = RolesConstants.FinanceiroGestao)]
        [HttpGet("resumo")]
        public async Task<IActionResult> Resumo([FromQuery] PagamentoFilterRequest f)
        {
            var r = await service.ListarAsync(f);
            return StatusCode(r.StatusCode, r);
        }

        [Authorize(Roles = RolesConstants.Medico)]
        [HttpGet("meus-pagamentos")]
        public async Task<IActionResult> Meus([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
            var r = await service.MeusAsync(uid, page, pageSize);
            return StatusCode(r.StatusCode, r);
        }

        [Authorize(Roles = RolesConstants.Medico)]
        [HttpGet("meus-pagamentos/{id:guid}")]
        public async Task<IActionResult> MeuPorId(Guid id)
        {
            var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
            var r = await service.MeuByIdAsync(uid, id);
            return StatusCode(r.StatusCode, r);
        }

        private async Task<IActionResult> ExecutarAcaoCriticaAsync<T>(string acao, Func<Guid, Task<ApiResponse<T>>> operacao, Guid? id = null)
        {
            try
            {
                var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
                var r = await operacao(uid);
                return StatusCode(r.StatusCode, r);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao {Acao} financeiro {Id}", acao, id);
                return StatusCode(500, ApiResponse<object>.Fail("Não foi possível processar a ação financeira no momento.", 500));
            }
        }
    }
}
