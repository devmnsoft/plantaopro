using System.Security.Claims;
using PlantaoPro.Api.Models;
namespace PlantaoPro.Api;
public interface IMeuDiaService{MeuDiaDto ObterResumo(ClaimsPrincipal user); IEnumerable<MeuDiaIndicadorDto> Indicadores(ClaimsPrincipal user); IEnumerable<MeuDiaItemDto> Pendencias(ClaimsPrincipal user); IEnumerable<MeuDiaItemDto> Agenda(ClaimsPrincipal user); IEnumerable<MeuDiaItemDto> Alertas(ClaimsPrincipal user); IEnumerable<MeuDiaItemDto> AcoesRapidas(ClaimsPrincipal user); MeuDiaItemDto AlterarEstado(Guid id,string status,MeuDiaEstadoRequest request);}
