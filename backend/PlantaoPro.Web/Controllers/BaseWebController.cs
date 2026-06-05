using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new JsonStringEnumConverter(), new DateOnlyJsonConverter(), new NullableDateOnlyJsonConverter() }
    };

    protected BaseWebController(IHttpClientFactory httpClientFactory, ILogger logger)
    {
        HttpClientFactory = httpClientFactory;
        Logger = logger;
    }

    [NonAction] public HttpClient CreateApiClient() => HttpClientFactory.CreateClient("PlantaoProApi");
    [NonAction] public string? GetJwtToken() => GetTokenFromSessionOrCookie() ?? User.FindFirst("jwt")?.Value ?? User.FindFirst("Token")?.Value;

    [NonAction]
    public string? GetTokenFromSessionOrCookie()
    {
        var sessionToken = HttpContext?.Session?.GetString("jwt");
        if (!string.IsNullOrWhiteSpace(sessionToken))
        {
            return sessionToken;
        }

        var legacySessionToken = HttpContext?.Session?.GetString("JwtToken");
        if (!string.IsNullOrWhiteSpace(legacySessionToken))
        {
            return legacySessionToken;
        }

        if (Request?.Cookies?.TryGetValue("jwt", out var cookieToken) == true && !string.IsNullOrWhiteSpace(cookieToken))
        {
            return cookieToken;
        }

        return null;
    }

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
        try
        {
            var response = await client.GetAsync(endpoint);
            return await ReadApiResponsePayloadAsync<T>(endpoint, response, setTempDataOnError: false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao consultar API. Endpoint:{Endpoint}", endpoint);
            return (default, "Falha de comunicação com a API.", HttpStatusCode.InternalServerError);
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
    public async Task<(bool Success, string? Error, HttpStatusCode StatusCode)> SendApiWithoutResponseAsync<TRequest>(HttpClient client, HttpMethod method, string endpoint, TRequest request)
    {
        var response = await SendApiAsync<TRequest, JsonElement>(client, method, endpoint, request);
        return (response.StatusCode is >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous, response.Error, response.StatusCode);
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
        try
        {
            var response = await client.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();
            var sample = content.Length > MaxLogContentLength ? content[..MaxLogContentLength] + "..." : content;

            Logger.LogInformation("API paged call BaseUrl:{BaseUrl} Endpoint:{Endpoint} Status:{Status} Usuario:{Usuario}", client.BaseAddress, endpoint, (int)response.StatusCode, User.Identity?.Name ?? "anônimo");

            if (!response.IsSuccessStatusCode)
            {
                var apiError = TryParseMessage(content);
                Logger.LogWarning("Falha ao consultar API paginada. Endpoint:{Endpoint} Status:{Status} ResponseSample:{ResponseSample}", endpoint, (int)response.StatusCode, sample);
                return (PagedResult<T>.Empty(page, pageSize), apiError ?? $"Falha ao consultar API ({(int)response.StatusCode}).", response.StatusCode);
            }

            var paged = DeserializePagedPayload<T>(content, page, pageSize);
            return (NormalizePagedResult(paged, page, pageSize), null, response.StatusCode);
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Erro de desserialização paginada. Endpoint:{Endpoint}", endpoint);
            return (PagedResult<T>.Empty(page, pageSize), "A API retornou dados em formato inesperado. Tente novamente em instantes.", HttpStatusCode.InternalServerError);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao consultar API paginada. Endpoint:{Endpoint}", endpoint);
            return (PagedResult<T>.Empty(page, pageSize), "Falha de comunicação com a API.", HttpStatusCode.InternalServerError);
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
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (TryGetStringProperty(root, "message", out var message) ||
                TryGetStringProperty(root, "mensagem", out message) ||
                TryGetStringProperty(root, "error", out message) ||
                TryGetStringProperty(root, "detail", out message) ||
                TryGetStringProperty(root, "title", out message))
            {
                return message;
            }

            if (root.ValueKind == JsonValueKind.Object && TryGetProperty(root, out var errors, "errors", "Errors"))
            {
                if (errors.ValueKind == JsonValueKind.Array)
                {
                    var messages = errors.EnumerateArray()
                        .Select(error => error.ValueKind == JsonValueKind.String ? error.GetString() : error.ToString())
                        .Where(error => !string.IsNullOrWhiteSpace(error));
                    return string.Join(" ", messages);
                }

                if (errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = errors.EnumerateObject()
                        .SelectMany(error => error.Value.ValueKind == JsonValueKind.Array
                            ? error.Value.EnumerateArray().Select(item => item.GetString())
                            : new[] { error.Value.ToString() })
                        .Where(error => !string.IsNullOrWhiteSpace(error));
                    return string.Join(" ", messages);
                }
            }
        }
        catch (JsonException)
        {
            return content.Length > MaxLogContentLength ? content[..MaxLogContentLength] : content;
        }

        return null;
    }

    [NonAction]
    public string HandleApiError(HttpStatusCode statusCode, string? apiMessage = null)
    {
        var message = BuildApiErrorMessage(statusCode, apiMessage);

        TempData["Error"] = message;
        return message;
    }

    private async Task<(T? Data, string? Error, HttpStatusCode StatusCode)> ReadApiResponsePayloadAsync<T>(string endpoint, HttpResponseMessage response, bool setTempDataOnError = true)
    {
        var content = await response.Content.ReadAsStringAsync();
        var user = User.Identity?.Name ?? "anônimo";
        var sample = content.Length > MaxLogContentLength ? content[..MaxLogContentLength] + "..." : content;

        Logger.LogInformation("API call BaseUrl:{BaseUrl} Endpoint:{Endpoint} Status:{Status} Usuario:{Usuario}", response.RequestMessage?.RequestUri?.GetLeftPart(UriPartial.Authority), endpoint, (int)response.StatusCode, user);

        if (!response.IsSuccessStatusCode)
        {
            Logger.LogWarning("Falha ao consultar API. Endpoint:{Endpoint} Status:{Status} Usuario:{Usuario} ResponseSample:{ResponseSample}", endpoint, (int)response.StatusCode, user, sample);
            var parsedMessage = TryParseMessage(content);
            var message = setTempDataOnError
                ? HandleApiError(response.StatusCode, parsedMessage)
                : BuildApiErrorMessage(response.StatusCode, parsedMessage);
            return (default, message, response.StatusCode);
        }

        try
        {
            return (DeserializeApiPayload<T>(content), TryParseMessage(content), response.StatusCode);
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Erro de desserialização. Endpoint:{Endpoint} Status:{Status} ResponseSample:{ResponseSample}", endpoint, (int)response.StatusCode, sample);
            var message = "A API retornou dados em formato inesperado. Tente novamente em instantes.";
            if (setTempDataOnError)
            {
                TempData["Error"] = message;
            }
            return (default, message, HttpStatusCode.InternalServerError);
        }
    }

    private static T? DeserializeApiPayload<T>(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return default;
        }

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Object)
        {
            if (TryGetProperty(root, out var data, "data", "Data", "payload", "Payload", "result", "Result", "value", "Value"))
            {
                return DeserializeElement<T>(data);
            }

            if (IsEnumerableTarget<T>() && TryGetProperty(root, out var rootItems, "items", "Items", "results", "Results", "records", "Records"))
            {
                return JsonSerializer.Deserialize<T>(rootItems.GetRawText(), JsonOptions);
            }
        }

        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }


    private static T? DeserializeElement<T>(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return default;
        }

        if (IsEnumerableTarget<T>() && element.ValueKind == JsonValueKind.Object && TryGetProperty(element, out var items, "items", "Items", "results", "Results", "records", "Records"))
        {
            return JsonSerializer.Deserialize<T>(items.GetRawText(), JsonOptions);
        }

        return JsonSerializer.Deserialize<T>(element.GetRawText(), JsonOptions);
    }

    private static PagedResult<T> DeserializePagedPayload<T>(string content, int page, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return PagedResult<T>.Empty(page, pageSize);
        }

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        var data = root.ValueKind == JsonValueKind.Object && TryGetProperty(root, out var wrappedData, "data", "Data", "payload", "Payload", "result", "Result", "value", "Value")
            ? wrappedData
            : root;

        if (data.ValueKind == JsonValueKind.Object && TryGetProperty(data, out var nestedItems, "items", "Items", "results", "Results", "records", "Records"))
        {
            var nestedPage = TryGetIntProperty(data, page, "page", "Page", "pagina", "Pagina");
            var nestedPageSize = TryGetIntProperty(data, pageSize, "pageSize", "PageSize", "limit", "Limit");
            var nestedTotal = TryGetLongProperty(data, "total", "Total", "totalItems", "TotalItems", "count", "Count") ?? 0;
            var items = JsonSerializer.Deserialize<List<T>>(nestedItems.GetRawText(), JsonOptions) ?? new List<T>();
            return NormalizePagedResult(new PagedResult<T> { Items = items, Page = nestedPage, PageSize = nestedPageSize, TotalItems = nestedTotal }, page, pageSize);
        }

        if (data.ValueKind == JsonValueKind.Array)
        {
            var items = JsonSerializer.Deserialize<List<T>>(data.GetRawText(), JsonOptions) ?? new List<T>();
            return new PagedResult<T> { Items = items, Page = page, PageSize = pageSize, TotalItems = items.Count };
        }

        if (data.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return PagedResult<T>.Empty(page, pageSize);
        }

        return JsonSerializer.Deserialize<PagedResult<T>>(data.GetRawText(), JsonOptions) ?? PagedResult<T>.Empty(page, pageSize);
    }

    private static PagedResult<T> NormalizePagedResult<T>(PagedResult<T>? paged, int fallbackPage, int fallbackPageSize)
    {
        paged ??= PagedResult<T>.Empty(fallbackPage, fallbackPageSize);
        paged.Items ??= Array.Empty<T>();
        paged.Page = paged.Page <= 0 ? fallbackPage : paged.Page;
        paged.PageSize = paged.PageSize <= 0 ? fallbackPageSize : paged.PageSize;

        if (paged.TotalItems <= 0)
        {
            paged.TotalItems = paged.Items.LongCount();
        }

        paged.TotalPages = paged.PageSize <= 0
            ? 0
            : (int)Math.Ceiling((double)paged.TotalItems / paged.PageSize);

        return paged;
    }


    private static int TryGetIntProperty(JsonElement root, int fallback, params string[] names)
    {
        if (!TryGetProperty(root, out var property, names)) return fallback;
        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number)) return number;
        if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out number)) return number;
        return fallback;
    }

    private static long? TryGetLongProperty(JsonElement root, params string[] names)
    {
        if (!TryGetProperty(root, out var property, names)) return null;
        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var number)) return number;
        if (property.ValueKind == JsonValueKind.String && long.TryParse(property.GetString(), out number)) return number;
        return null;
    }

    private static bool TryGetStringProperty(JsonElement root, string name, out string? value)
    {
        value = null;
        if (!TryGetProperty(root, out var property, name))
        {
            return false;
        }

        value = property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryGetProperty(JsonElement root, out JsonElement value, params string[] names)
    {
        value = default;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var name in names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (root.TryGetProperty(name, out value))
            {
                return true;
            }
        }

        foreach (var property in root.EnumerateObject())
        {
            if (names.Any(name => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                value = property.Value;
                return true;
            }
        }

        return false;
    }

    private static bool IsEnumerableTarget<T>()
    {
        var target = typeof(T);
        return target != typeof(string) && target != typeof(JsonElement) && typeof(System.Collections.IEnumerable).IsAssignableFrom(target);
    }

    private static string BuildApiErrorMessage(HttpStatusCode statusCode, string? apiMessage = null)
        => statusCode switch
        {
            HttpStatusCode.BadRequest => apiMessage ?? "Não foi possível processar a solicitação. Revise os dados e tente novamente.",
            HttpStatusCode.Unauthorized => "Sessão expirada. Faça login novamente.",
            HttpStatusCode.Forbidden => "Você não possui permissão para realizar esta ação.",
            HttpStatusCode.NotFound => apiMessage ?? "Registro não encontrado ou não está mais disponível.",
            HttpStatusCode.Conflict => apiMessage ?? "Conflito de dados detectado. Atualize a tela e tente novamente.",
            HttpStatusCode.InternalServerError => "Ocorreu uma instabilidade interna. Tente novamente em instantes.",
            _ => apiMessage ?? "Não foi possível concluir a operação agora."
        };

    private sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
    {
        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType is JsonTokenType.Null)
            {
                return DateOnly.MinValue;
            }

            if (reader.TokenType is not JsonTokenType.String)
            {
                throw new JsonException($"Token inválido para DateOnly: {reader.TokenType}.");
            }

            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return DateOnly.MinValue;
            }

            if (DateOnly.TryParse(value, out var parsed))
            {
                return parsed;
            }

            if (DateTime.TryParse(value, out var dateTime))
            {
                return DateOnly.FromDateTime(dateTime);
            }

            throw new JsonException($"Data inválida para DateOnly: {value}.");
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
    }

    private sealed class NullableDateOnlyJsonConverter : JsonConverter<DateOnly?>
    {
        public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType is JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType is not JsonTokenType.String)
            {
                throw new JsonException($"Token inválido para DateOnly?: {reader.TokenType}.");
            }

            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (DateOnly.TryParse(value, out var parsed))
            {
                return parsed;
            }

            if (DateTime.TryParse(value, out var dateTime))
            {
                return DateOnly.FromDateTime(dateTime);
            }

            throw new JsonException($"Data inválida para DateOnly?: {value}.");
        }

        public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
                return;
            }

            writer.WriteNullValue();
        }
    }
}
