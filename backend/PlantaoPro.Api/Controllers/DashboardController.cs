using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService service;
        private readonly ILogger<DashboardController> logger;
        public DashboardController(DashboardService service, ILogger<DashboardController> logger)
        {
            this.service = service;
            this.logger = logger;
        }
        [Authorize(Roles = RolesConstants.Dashboard)]
        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
                var r = await service.GetAsync(uid);
                return StatusCode(r.StatusCode, r);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao carregar dashboard");
                return StatusCode(500, ApiResponse<string>.Fail("Erro ao carregar dashboard.", 500));
            }
        }
        [Authorize(Roles = RolesConstants.Dashboard)]
        [HttpGet("mobile/home")]
        public async Task<IActionResult> MobileHome()
        {
            var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
            var r = await service.GetAsync(uid);
            return StatusCode(r.StatusCode, r);
        }
    }
}
