using System.Security.Claims;
using PlantaoPro.Api.Controllers;

namespace PlantaoPro.Api;
public sealed class ContextoService : IContextoService
{
    private readonly IContextoRepository _repo; public ContextoService(IContextoRepository repo)=>_repo=repo;
    private static Guid UserId(ClaimsPrincipal u)=>Guid.Parse(u.FindFirstValue("uid")??u.FindFirstValue(ClaimTypes.NameIdentifier)??Guid.Empty.ToString());
    public ContextoAtualDto Atual(ClaimsPrincipal user)=>_repo.AtualAsync(user).GetAwaiter().GetResult();
    public IEnumerable<TenantDisponivelDto> TenantsDisponiveis(ClaimsPrincipal user)=>_repo.TenantsDisponiveisAsync(UserId(user)).GetAwaiter().GetResult();
    public IEnumerable<ContextoTrocaDto> Recentes(ClaimsPrincipal user)=>Historico(user);
    public object Selecionar(ClaimsPrincipal user, SelecionarContextoRequest request)=>_repo.SelecionarAsync(user,request.TenantId,request.Motivo).GetAwaiter().GetResult();
    public object RetornarGlobal(ClaimsPrincipal user)=>_repo.RetornarGlobalAsync(user).GetAwaiter().GetResult();
    public IEnumerable<ContextoTrocaDto> Historico(ClaimsPrincipal user)=>_repo.TrocasAsync(UserId(user)).GetAwaiter().GetResult();
}
