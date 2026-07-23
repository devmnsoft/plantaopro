using System.Security.Claims;
using PlantaoPro.Api.Controllers;
namespace PlantaoPro.Api;
public interface IImpersonationRepository
{
    Task<ImpersonationSessionDto> IniciarAsync(ClaimsPrincipal user, IniciarImpersonacaoRequest request, CancellationToken ct=default);
    Task<object> EncerrarAsync(ClaimsPrincipal user, EncerrarImpersonacaoRequest request, CancellationToken ct=default);
    Task<IReadOnlyCollection<object>> HistoricoAsync(ClaimsPrincipal user, CancellationToken ct=default);
}
