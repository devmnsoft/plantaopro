using System.Security.Claims;
namespace PlantaoPro.Api;
public sealed class ImpersonationAuthorizationService { public bool CanStart(ClaimsPrincipal user)=>user.Identity?.IsAuthenticated==true && user.FindFirstValue("impersonation")!="true"; }
