using System.Text.Json;

namespace PlantaoPro.Api.SavedViews;

public sealed record SavedViewDto(Guid Id, string Module, string Name, JsonElement Filters, JsonElement? Sort, bool IsDefault, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record SaveSavedViewRequest(string Module, string Name, JsonElement Filters, JsonElement? Sort, bool IsDefault = false);
public sealed record UpdateSavedViewRequest(string Name, JsonElement Filters, JsonElement? Sort, bool IsDefault = false);

public sealed class SavedViewValidationException : Exception { public SavedViewValidationException(string message):base(message){} }
public sealed class SavedViewConflictException : Exception { public SavedViewConflictException(string message):base(message){} }

public interface ISavedViewRepository
{
    Task<IReadOnlyList<SavedViewDto>> ListAsync(Guid tenantId, Guid userId, string module, CancellationToken ct);
    Task<SavedViewDto?> GetAsync(Guid tenantId, Guid userId, Guid id, CancellationToken ct);
    Task<SavedViewDto> CreateAsync(Guid tenantId, Guid userId, string module, string name, string normalizedName, string filtersJson, string? sortJson, bool isDefault, CancellationToken ct);
    Task<SavedViewDto?> UpdateAsync(Guid tenantId, Guid userId, Guid id, string name, string normalizedName, string filtersJson, string? sortJson, bool isDefault, CancellationToken ct);
    Task<bool> DeleteAsync(Guid tenantId, Guid userId, Guid id, CancellationToken ct);
    Task<SavedViewDto?> SetDefaultAsync(Guid tenantId, Guid userId, Guid id, CancellationToken ct);
}

public interface ISavedViewService
{
    Task<IReadOnlyList<SavedViewDto>> ListAsync(string module, CancellationToken ct);
    Task<SavedViewDto> CreateAsync(SaveSavedViewRequest request, CancellationToken ct);
    Task<SavedViewDto?> UpdateAsync(Guid id, UpdateSavedViewRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    Task<SavedViewDto?> SetDefaultAsync(Guid id, CancellationToken ct);
}
