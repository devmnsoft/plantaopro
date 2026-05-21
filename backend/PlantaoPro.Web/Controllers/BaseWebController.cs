using System.Net;
using System.Net.Http.Headers;
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
            var apiResult = JsonSerializer.Deserialize<ApiResponse<T>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return (apiResult is not null ? apiResult.Data : default, apiResult?.Message, response.StatusCode);
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Erro de desserialização. Endpoint:{Endpoint} Status:{Status} ResponseSample:{ResponseSample}", endpoint, (int)response.StatusCode, sample);
            return (default, "A API retornou dados em formato inesperado. Tente novamente em instantes.", HttpStatusCode.InternalServerError);
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
            var parsed = JsonSerializer.Deserialize<ApiResponse<JsonElement>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return parsed?.Message;
        }
        catch { return null; }
    }
}
