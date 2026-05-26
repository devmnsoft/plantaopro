using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public abstract class BaseWebController : Controller
{
    protected readonly IHttpClientFactory HttpClientFactory;
    protected readonly ILogger Logger;
    private const int MaxLogContentLength = 400;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    protected BaseWebController(IHttpClientFactory httpClientFactory, ILogger logger)
    {
        HttpClientFactory = httpClientFactory;
        Logger = logger;
    }

    [NonAction] public HttpClient CreateApiClient() => HttpClientFactory.CreateClient("PlantaoProApi");
    [NonAction] public string? GetJwtToken() => User.FindFirst("jwt")?.Value;

    [NonAction]
    public bool AddBearerToken(HttpClient client)
    {
        var token = GetJwtToken();
        if (string.IsNullOrWhiteSpace(token)) return false;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return true;
    }

    [NonAction]
    public IActionResult HandleUnauthorized(string? message = null)
    {
        TempData["Error"] = message ?? "Sessão expirada. Faça login novamente.";
        return RedirectToAction("Login", "Account");
    }

    [NonAction]
    public async Task<(T? Data, string? Error, HttpStatusCode StatusCode)> ReadApiResponse<T>(HttpClient client, string endpoint)
        => await ReadApiResponseAsync<T>(client, endpoint);

    [NonAction]
    public async Task<(T? Data, string? Error, HttpStatusCode StatusCode)> ReadApiResponseAsync<T>(HttpClient client, string endpoint)
    {
        var response = await client.GetAsync(endpoint);
        var content = await response.Content.ReadAsStringAsync();
        var user = User.Identity?.Name ?? "anônimo";
        var sample = content.Length > MaxLogContentLength ? content[..MaxLogContentLength] + "..." : content;

        Logger.LogInformation("API call BaseUrl:{BaseUrl} Endpoint:{Endpoint} Status:{Status} Usuario:{Usuario}", client.BaseAddress, endpoint, (int)response.StatusCode, user);

        if (!response.IsSuccessStatusCode)
        {
            Logger.LogWarning("Falha ao consultar API. Endpoint:{Endpoint} Status:{Status} ResponseSample:{ResponseSample}", endpoint, (int)response.StatusCode, sample);
            var apiError = TryParseMessage(content);
            return (default, apiError ?? $"Falha ao consultar API ({(int)response.StatusCode}).", response.StatusCode);
        }

        try
        {
            var apiResult = JsonSerializer.Deserialize<ApiResponse<T>>(content, JsonOptions);
            return (apiResult is not null ? apiResult.Data : default, apiResult?.Message, response.StatusCode);
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Erro de desserialização. Endpoint:{Endpoint} Status:{Status} ResponseSample:{ResponseSample}", endpoint, (int)response.StatusCode, sample);
            return (default, "A API retornou dados em formato inesperado. Tente novamente em instantes.", HttpStatusCode.InternalServerError);
        }
    }

    [NonAction]
    public async Task<(IEnumerable<T> Data, string? Error, HttpStatusCode StatusCode)> ReadApiListResponseAsync<T>(HttpClient client, string endpoint)
    {
        var result = await ReadApiResponseAsync<IEnumerable<T>>(client, endpoint);
        return (result.Data ?? Array.Empty<T>(), result.Error, result.StatusCode);
    }

    [NonAction]
    public async Task<(TResponse? Data, string? Error, HttpStatusCode StatusCode)> SendApiAsync<TRequest, TResponse>(HttpClient client, HttpMethod method, string endpoint, TRequest request)
    {
        try
        {
            var payload = new StringContent(JsonSerializer.Serialize(request, JsonOptions), Encoding.UTF8, "application/json");
            var message = new HttpRequestMessage(method, endpoint) { Content = payload };
            var response = await client.SendAsync(message);
            return await ReadApiResponsePayloadAsync<TResponse>(endpoint, response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao enviar requisição para API. Endpoint:{Endpoint}", endpoint);
            TempData["Error"] = "Não foi possível concluir a operação agora. Tente novamente.";
            return (default, "Falha de comunicação com a API.", HttpStatusCode.InternalServerError);
        }
    }

    [NonAction]
    public async Task<(bool Success, string? Error, HttpStatusCode StatusCode)> DeleteApiAsync(HttpClient client, string endpoint, bool useDelete = false)
    {
        try
        {
            var response = useDelete ? await client.DeleteAsync(endpoint) : await client.PostAsync(endpoint, null);
            var handled = await ReadApiResponsePayloadAsync<JsonElement>(endpoint, response);
            return (response.IsSuccessStatusCode, handled.Error, handled.StatusCode);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao excluir/inativar recurso na API. Endpoint:{Endpoint}", endpoint);
            TempData["Error"] = "Não foi possível concluir a operação agora. Tente novamente.";
            return (false, "Falha de comunicação com a API.", HttpStatusCode.InternalServerError);
        }
    }

    [NonAction]
    public async Task<(PagedResult<T> Data, string? Error, HttpStatusCode StatusCode)> ReadApiPagedResponseAsync<T>(HttpClient client, string endpoint, int page = 1, int pageSize = 20)
    {
        var response = await client.GetAsync(endpoint);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            var apiError = TryParseMessage(content);
            return (PagedResult<T>.Empty(page, pageSize), apiError ?? $"Falha ao consultar API ({(int)response.StatusCode}).", response.StatusCode);
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            if (!doc.RootElement.TryGetProperty("data", out var data))
                return (PagedResult<T>.Empty(page, pageSize), null, response.StatusCode);

            if (data.ValueKind == JsonValueKind.Array)
            {
                var items = JsonSerializer.Deserialize<List<T>>(data.GetRawText(), JsonOptions) ?? new List<T>();
                return (new PagedResult<T> { Items = items, Page = page, PageSize = pageSize, TotalItems = items.Count, TotalPages = items.Count == 0 ? 0 : 1 }, null, response.StatusCode);
            }

            var paged = JsonSerializer.Deserialize<PagedResult<T>>(data.GetRawText(), JsonOptions) ?? PagedResult<T>.Empty(page, pageSize);
            if (paged.TotalPages <= 0 && paged.PageSize > 0)
                paged.TotalPages = (int)Math.Ceiling((double)paged.TotalItems / paged.PageSize);
            return (paged, null, response.StatusCode);
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Erro de desserialização paginada. Endpoint:{Endpoint}", endpoint);
            return (PagedResult<T>.Empty(page, pageSize), "A API retornou dados em formato inesperado. Tente novamente em instantes.", HttpStatusCode.InternalServerError);
        }
    }

    [NonAction]
    protected void LogRequestContext(string acao, string endpoint, int? statusCode = null)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "ip-desconhecido";
        var usuario = User.Identity?.Name ?? "anônimo";
        var perfil = string.Join(',', User.Claims.Where(c => c.Type.Contains("role", StringComparison.OrdinalIgnoreCase)).Select(c => c.Value).Distinct());
        Logger.LogInformation("{Acao} | Endpoint:{Endpoint} | Status:{Status} | Usuario:{Usuario} | Perfil:{Perfil} | IP:{IP} | TimestampUtc:{TimestampUtc}",
            acao,
            endpoint,
            statusCode,
            usuario,
            string.IsNullOrWhiteSpace(perfil) ? "sem-perfil" : perfil,
            ip,
            DateTime.UtcNow);
    }

    [NonAction]
    public ViewResult EmptyViewWithError<TModel>(TModel model, string message)
    {
        ViewBag.ErrorMessage = message;
        return View(model);
    }

    private static string? TryParseMessage(string content)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<ApiResponse<JsonElement>>(content, JsonOptions);
            return parsed?.Message;
        }
        catch { return null; }
    }

    [NonAction]
    public string HandleApiError(HttpStatusCode statusCode, string? apiMessage = null)
    {
        var message = statusCode switch
        {
            HttpStatusCode.BadRequest => apiMessage ?? "Não foi possível processar a solicitação. Revise os dados e tente novamente.",
            HttpStatusCode.Unauthorized => "Sessão expirada. Faça login novamente.",
            HttpStatusCode.Forbidden => "Você não possui permissão para realizar esta ação.",
            HttpStatusCode.NotFound => apiMessage ?? "Registro não encontrado ou não está mais disponível.",
            HttpStatusCode.InternalServerError => "Ocorreu uma instabilidade interna. Tente novamente em instantes.",
            _ => apiMessage ?? "Não foi possível concluir a operação agora."
        };

        TempData["Error"] = message;
        return message;
    }

    private async Task<(T? Data, string? Error, HttpStatusCode StatusCode)> ReadApiResponsePayloadAsync<T>(string endpoint, HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var user = User.Identity?.Name ?? "anônimo";
        var sample = content.Length > MaxLogContentLength ? content[..MaxLogContentLength] + "..." : content;

        Logger.LogInformation("API call BaseUrl:{BaseUrl} Endpoint:{Endpoint} Status:{Status} Usuario:{Usuario}", response.RequestMessage?.RequestUri?.GetLeftPart(UriPartial.Authority), endpoint, (int)response.StatusCode, user);

        if (!response.IsSuccessStatusCode)
        {
            Logger.LogWarning("Falha ao consultar API. Endpoint:{Endpoint} Status:{Status} Usuario:{Usuario} ResponseSample:{ResponseSample}", endpoint, (int)response.StatusCode, user, sample);
            var parsedMessage = TryParseMessage(content);
            var message = HandleApiError(response.StatusCode, parsedMessage);
            return (default, message, response.StatusCode);
        }

        try
        {
            var apiResult = JsonSerializer.Deserialize<ApiResponse<T>>(content, JsonOptions);
            return (apiResult is not null ? apiResult.Data : default, apiResult?.Message, response.StatusCode);
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Erro de desserialização. Endpoint:{Endpoint} Status:{Status} ResponseSample:{ResponseSample}", endpoint, (int)response.StatusCode, sample);
            var message = "A API retornou dados em formato inesperado. Tente novamente em instantes.";
            TempData["Error"] = message;
            return (default, message, HttpStatusCode.InternalServerError);
        }
    }
}
