using System.Security.Claims;
namespace PlantaoPro.Api;
public sealed class ContextAuthorizationService { public bool CanSwitch(ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true && user.FindFirstValue("impersonation") != "true"; }
