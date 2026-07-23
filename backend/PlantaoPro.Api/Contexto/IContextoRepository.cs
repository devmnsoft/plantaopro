using System.Security.Claims;

namespace PlantaoPro.Api;

public interface IContextoRepository
{
    Task<ContextoAtualDto> AtualAsync(ClaimsPrincipal user, CancellationToken ct = default);
    Task<IReadOnlyCollection<TenantDisponivelDto>> TenantsDisponiveisAsync(Guid usuarioId, CancellationToken ct = default);
    Task<IReadOnlyCollection<ContextoTrocaDto>> TrocasAsync(Guid usuarioId, CancellationToken ct = default);
    Task<SelecionarContextoResponse> SelecionarAsync(ClaimsPrincipal user, Guid tenantId, string? motivo, CancellationToken ct = default);
    Task<RetornarContextoGlobalResponse> RetornarGlobalAsync(ClaimsPrincipal user, CancellationToken ct = default);
}
