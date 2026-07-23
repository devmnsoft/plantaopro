using System.Security.Claims;
using PlantaoPro.Api.Models;
namespace PlantaoPro.Api;
public interface IMeuDiaRepository{Task<MeuDiaDto> ObterResumoAsync(ClaimsPrincipal user, CancellationToken ct=default); Task<MeuDiaItemDto> AlterarEstadoAsync(Guid id,string status,MeuDiaEstadoRequest request,ClaimsPrincipal user,CancellationToken ct=default);}
