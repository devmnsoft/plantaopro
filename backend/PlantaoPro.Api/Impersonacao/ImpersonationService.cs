using System.Security.Claims;
using PlantaoPro.Api.Controllers;
namespace PlantaoPro.Api;
public sealed class ImpersonationService : IImpersonationService
{
    private readonly IImpersonationRepository _repo; public ImpersonationService(IImpersonationRepository repo)=>_repo=repo;
    public object Iniciar(ClaimsPrincipal user, IniciarImpersonacaoRequest request)=>_repo.IniciarAsync(user,request).GetAwaiter().GetResult();
    public object Encerrar(ClaimsPrincipal user, EncerrarImpersonacaoRequest request)=>_repo.EncerrarAsync(user,request).GetAwaiter().GetResult();
    public object Atual(ClaimsPrincipal user)=>new ImpersonationAtualDto(user.FindFirstValue("impersonation")=="true",user.FindFirstValue("original_user_id"),user.FindFirstValue("impersonated_user_id"),user.FindFirstValue("impersonation_session_id"),user.FindFirstValue("impersonation_expires_at"));
    public IEnumerable<object> Historico(ClaimsPrincipal user)=>_repo.HistoricoAsync(user).GetAwaiter().GetResult();
}
