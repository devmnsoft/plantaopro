using System.Security.Claims;
using PlantaoPro.Api.Controllers;

namespace PlantaoPro.Api;
public interface IContextoService
{
    ContextoAtualDto Atual(ClaimsPrincipal user);
    IEnumerable<TenantDisponivelDto> TenantsDisponiveis(ClaimsPrincipal user);
    IEnumerable<ContextoTrocaDto> Recentes(ClaimsPrincipal user);
    object Selecionar(ClaimsPrincipal user, SelecionarContextoRequest request);
    object RetornarGlobal(ClaimsPrincipal user);
    IEnumerable<ContextoTrocaDto> Historico(ClaimsPrincipal user);
}
