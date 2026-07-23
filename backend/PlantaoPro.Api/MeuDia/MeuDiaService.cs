using System.Security.Claims;
using PlantaoPro.Api.Models;
namespace PlantaoPro.Api;
public sealed class MeuDiaService : IMeuDiaService
{
    private readonly IMeuDiaRepository _repo; public MeuDiaService(IMeuDiaRepository repo)=>_repo=repo; private MeuDiaDto Resumo(ClaimsPrincipal u)=>_repo.ObterResumoAsync(u).GetAwaiter().GetResult();
    public MeuDiaDto ObterResumo(ClaimsPrincipal user)=>Resumo(user); public IEnumerable<MeuDiaIndicadorDto> Indicadores(ClaimsPrincipal user)=>Resumo(user).Indicadores; public IEnumerable<MeuDiaItemDto> Pendencias(ClaimsPrincipal user)=>Resumo(user).Pendencias; public IEnumerable<MeuDiaItemDto> Agenda(ClaimsPrincipal user)=>Resumo(user).Agenda; public IEnumerable<MeuDiaItemDto> Alertas(ClaimsPrincipal user)=>Resumo(user).Alertas; public IEnumerable<MeuDiaItemDto> AcoesRapidas(ClaimsPrincipal user)=>Resumo(user).AcoesRapidas; public MeuDiaItemDto AlterarEstado(Guid id,string status,MeuDiaEstadoRequest request)=>_repo.AlterarEstadoAsync(id,status,request,new ClaimsPrincipal()).GetAwaiter().GetResult();
}
