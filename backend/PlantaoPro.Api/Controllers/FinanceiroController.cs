using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
namespace PlantaoPro.Api.Controllers
{
    [ApiController]
    [Route("api/financeiro")]
    public class FinanceiroController : ControllerBase
    {
        private readonly FinanceiroService service; public FinanceiroController(FinanceiroService service)
        {
            this.service = service;
        }
        [Authorize]
        [HttpGet("pagamentos")]
        public async Task<IActionResult> Listar([FromQuery] PagamentoFilterRequest f)
        {
            var r = await service.ListarAsync(f);
            return StatusCode(r.StatusCode, r);
        }
        [Authorize]
        [HttpGet("pagamentos/{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var r = await service.GetByIdAsync(id);
            return StatusCode(r.StatusCode, r);
        }
        [Authorize]
        [HttpPost("pagamentos/gerar")]
        public async Task<IActionResult> Gerar([FromBody] GerarPagamentoRequest req)
        {
            var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
            var r = await service.GerarAsync(req, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
            return StatusCode(r.StatusCode, r);
        }
        [Authorize]
        [HttpPost("pagamentos/{id:guid}/confirmar")]
        public async Task<IActionResult> Confirmar(Guid id, [FromBody] ConfirmarPagamentoRequest req)
        {
            var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
            var r = await service.ConfirmarAsync(id, req, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
            return StatusCode(r.StatusCode, r);
        }
        [Authorize]
        [HttpPost("pagamentos/{id:guid}/cancelar")]
        public async Task<IActionResult> Cancelar(Guid id, [FromBody] CancelPaymentRequest req)
        {
            var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
            var r = await service.CancelarAsync(id, req.Justificativa, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
            return StatusCode(r.StatusCode, r);
        }
        [Authorize]
        [HttpGet("meus-pagamentos")]
        public async Task<IActionResult> Meus([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
            var r = await service.MeusAsync(uid, page, pageSize);
            return StatusCode(r.StatusCode, r);
        }
        [Authorize]
        [HttpGet("meus-pagamentos/{id:guid}")]
        public async Task<IActionResult> MeuPorId(Guid id)
        {
            var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
            var r = await service.MeuByIdAsync(uid, id);
            return StatusCode(r.StatusCode, r);
        }
    }
}
