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

    protected BaseWebController(IHttpClientFactory httpClientFactory, ILogger logger)
    {
        HttpClientFactory = httpClientFactory;
        Logger = logger;
    }

    protected HttpClient CreateApiClient() => HttpClientFactory.CreateClient("PlantaoProApi");

    protected string? GetJwtToken() => User.FindFirst("jwt")?.Value;

    protected bool AddBearerToken(HttpClient client)
    {
        var token = GetJwtToken();
        if (string.IsNullOrWhiteSpace(token)) return false;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return true;
    }

    protected IActionResult HandleUnauthorized(string? message = null)
    {
        TempData["Error"] = message ?? "Sessão expirada. Faça login novamente.";
        return RedirectToAction("Login", "Account");
    }

    protected async Task<(T? Data, string? Error, HttpStatusCode StatusCode)> ReadApiResponse<T>(HttpClient client, string endpoint)
    {
        var response = await client.GetAsync(endpoint);
        var content = await response.Content.ReadAsStringAsync();
        var user = User.Identity?.Name ?? "anônimo";
        Logger.LogInformation("API call BaseUrl:{BaseUrl} Endpoint:{Endpoint} Status:{Status} Usuario:{Usuario} Response:{Response}",
            client.BaseAddress, endpoint, (int)response.StatusCode, user, content);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
                Logger.LogWarning("Rota da API não encontrada. Endpoint:{Endpoint}", endpoint);

            var apiError = TryParseMessage(content);
            return (default, apiError ?? $"Falha ao consultar API ({(int)response.StatusCode}).", response.StatusCode);
        }

        var apiResult = JsonSerializer.Deserialize<ApiResponse<T>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return (apiResult?.Data, apiResult?.Message, response.StatusCode);
    }

    protected ViewResult EmptyViewWithError<TModel>(TModel model, string message)
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
        catch
        {
            return null;
        }
    }
}
