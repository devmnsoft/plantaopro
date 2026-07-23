using System.Security.Claims;
using PlantaoPro.Api.Controllers;
namespace PlantaoPro.Api;
public interface IImpersonationService{object Iniciar(ClaimsPrincipal user, IniciarImpersonacaoRequest request); object Encerrar(ClaimsPrincipal user, EncerrarImpersonacaoRequest request); object Atual(ClaimsPrincipal user); IEnumerable<object> Historico(ClaimsPrincipal user);}
