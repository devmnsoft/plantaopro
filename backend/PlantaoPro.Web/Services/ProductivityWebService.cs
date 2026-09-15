using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Services;

public sealed class ProductivityWebService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<ProductivityWebService> _logger;

    public ProductivityWebService(IHttpClientFactory clients, ILogger<ProductivityWebService> logger)
        => (_clients, _logger) = (clients, logger);

    public Task<ProductivityPageViewModel> GetActionsAsync(string token, ProductivityQueryViewModel query, CancellationToken ct)
        => GetAsync(token, BuildQuery("api/produtividade", query), ct);

    public Task<ProductivityPageViewModel> GetMyDayAsync(string token, CancellationToken ct)
        => GetAsync(token, "api/produtividade/meu-dia", ct);

    public async Task<HttpResponseMessage> SnoozeAsync(string token, string key, DateTimeOffset until, CancellationToken ct)
    {
        var client = CreateClient(token);
        return await client.PostAsJsonAsync($"api/produtividade/{Uri.EscapeDataString(key)}/adiar", new { snoozedUntil = until }, Json, ct);
    }

    private async Task<ProductivityPageViewModel> GetAsync(string token, string uri, CancellationToken ct)
    {
        try
        {
            using var response = await CreateClient(token).GetAsync(uri, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Productivity API returned {StatusCode} for {Uri}", response.StatusCode, uri);
                return new() { Error = response.StatusCode == System.Net.HttpStatusCode.Forbidden
                    ? "Seu perfil não possui acesso a esta visão ou o acesso foi revogado."
                    : "Não foi possível carregar os dados reais agora. Tente novamente." };
            }

            return await response.Content.ReadFromJsonAsync<ProductivityPageViewModel>(Json, ct) ?? new();
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Productivity API unavailable for {Uri}", uri);
            return new() { Error = "A Central de Ações está temporariamente indisponível." };
        }
    }

    private HttpClient CreateClient(string token)
    {
        var client = _clients.CreateClient("PlantaoProApi");
        if (!string.IsNullOrWhiteSpace(token)) client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    private static string BuildQuery(string path, ProductivityQueryViewModel query)
    {
        var values = new Dictionary<string, string?>
        {
            ["tab"] = query.Tab, ["priority"] = query.Priority, ["module"] = query.Module,
            ["status"] = query.Status, ["unitId"] = query.UnitId, ["mine"] = query.Mine ? "true" : null,
            ["page"] = Math.Max(1, query.Page).ToString(), ["pageSize"] = Math.Clamp(query.PageSize, 1, 100).ToString()
        };
        var now = DateTimeOffset.UtcNow;
        if (query.PeriodFrom.HasValue) values["dueFrom"] = query.PeriodFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).ToString("O");
        if (query.PeriodTo.HasValue) values["dueTo"] = query.PeriodTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).ToString("O");
        if (query.Due == "hoje") { values["dueFrom"] = now.Date.ToString("O"); values["dueTo"] = now.Date.AddDays(1).ToString("O"); }
        else if (query.Due == "atrasado") values["dueTo"] = now.ToString("O");
        return Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(path, values.Where(x => !string.IsNullOrWhiteSpace(x.Value)).ToDictionary(x => x.Key, x => x.Value!));
    }
}
