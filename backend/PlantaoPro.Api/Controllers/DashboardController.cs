using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api;

namespace PlantaoPro.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService service; public DashboardController(DashboardService service)
        {
            this.service = service;
        }
        [Authorize(Roles = RolesConstants.Dashboard)]
        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
            var r = await service.GetAsync(uid);
            return StatusCode(r.StatusCode, r);
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
