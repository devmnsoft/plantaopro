using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public interface IMeuDiaRepository { }
public sealed class MeuDiaRepository : IMeuDiaRepository { }
public interface IMeuDiaService
{
    MeuDiaDto ObterResumo(System.Security.Claims.ClaimsPrincipal user);
    IEnumerable<MeuDiaIndicadorDto> Indicadores(System.Security.Claims.ClaimsPrincipal user);
    IEnumerable<MeuDiaItemDto> Pendencias(System.Security.Claims.ClaimsPrincipal user);
    IEnumerable<MeuDiaItemDto> Agenda(System.Security.Claims.ClaimsPrincipal user);
    IEnumerable<MeuDiaItemDto> Alertas(System.Security.Claims.ClaimsPrincipal user);
    IEnumerable<MeuDiaItemDto> AcoesRapidas(System.Security.Claims.ClaimsPrincipal user);
    MeuDiaItemDto AlterarEstado(Guid id, string status, MeuDiaEstadoRequest request);
}
public sealed class MeuDiaService : IMeuDiaService
{
    public MeuDiaDto ObterResumo(System.Security.Claims.ClaimsPrincipal user) => new(Indicadores(user), Pendencias(user), Agenda(user), Alertas(user), AcoesRapidas(user));
    public IEnumerable<MeuDiaIndicadorDto> Indicadores(System.Security.Claims.ClaimsPrincipal user) => new[] { new MeuDiaIndicadorDto("pendencias", "Pendências", 0, "bi-check2-circle", "success") };
    public IEnumerable<MeuDiaItemDto> Pendencias(System.Security.Claims.ClaimsPrincipal user) => Array.Empty<MeuDiaItemDto>();
    public IEnumerable<MeuDiaItemDto> Agenda(System.Security.Claims.ClaimsPrincipal user) => Array.Empty<MeuDiaItemDto>();
    public IEnumerable<MeuDiaItemDto> Alertas(System.Security.Claims.ClaimsPrincipal user) => Array.Empty<MeuDiaItemDto>();
    public IEnumerable<MeuDiaItemDto> AcoesRapidas(System.Security.Claims.ClaimsPrincipal user) => new[] { new MeuDiaItemDto(Guid.NewGuid(), "acao", "Revisar agenda", "Abra sua agenda operacional do dia.", "media", DateTime.UtcNow.Date, "Você", "aberto", "bi-calendar2-week", "Agenda", "Index", false, false) };
    public MeuDiaItemDto AlterarEstado(Guid id, string status, MeuDiaEstadoRequest request) => new(id, "estado", "Item atualizado", request.Motivo ?? "Estado alterado com auditoria", "media", request.NovaData, "Você", status, "bi-check2", "MeuDia", "Index", status != "concluido", status != "adiado");
}
