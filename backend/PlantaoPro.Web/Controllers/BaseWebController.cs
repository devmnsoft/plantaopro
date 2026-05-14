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

    [NonAction]
    public HttpClient CreateApiClient()
    {
        return HttpClientFactory.CreateClient("PlantaoProApi");
    }

    [NonAction]
    public string? GetJwtToken()
    {
        return User.FindFirst("jwt")?.Value;
    }

    [NonAction]
    public bool AddBearerToken(HttpClient client)
    {
        var token = GetJwtToken();

        if (string.IsNullOrWhiteSpace(token))
            return false;

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return true;
    }

    [NonAction]
    public IActionResult HandleUnauthorized(string? message = null)
    {
        TempData["Error"] = message ?? "Sessão expirada. Faça login novamente.";
        return RedirectToAction("Login", "Account");
    }

    [NonAction]
    public async Task<(T Data, string? Error, HttpStatusCode StatusCode)> ReadApiResponse<T>(
        HttpClient client,
        string endpoint)
    {
        var response = await client.GetAsync(endpoint);
        var content = await response.Content.ReadAsStringAsync();

        var user = User.Identity?.Name ?? "anônimo";

        Logger.LogInformation(
            "API call BaseUrl:{BaseUrl} Endpoint:{Endpoint} Status:{Status} Usuario:{Usuario}",
            client.BaseAddress,
            endpoint,
            (int)response.StatusCode,
            user
        );

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Logger.LogWarning(
                    "Rota da API não encontrada. Endpoint:{Endpoint} Status:{Status} Response:{Response}",
                    endpoint,
                    (int)response.StatusCode,
                    content
                );
            }
            else
            {
                Logger.LogWarning(
                    "Falha ao consultar API. Endpoint:{Endpoint} Status:{Status} Response:{Response}",
                    endpoint,
                    (int)response.StatusCode,
                    content
                );
            }

            var apiError = TryParseMessage(content);

            return (
                default!,
                apiError ?? $"Falha ao consultar API ({(int)response.StatusCode}).",
                response.StatusCode
            );
        }

        try
        {
            var apiResult = JsonSerializer.Deserialize<ApiResponse<T>>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );

            return (
                apiResult is not null ? apiResult.Data : default!,
                apiResult?.Message,
                response.StatusCode
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Erro ao desserializar resposta da API. Endpoint:{Endpoint} Response:{Response}",
                endpoint,
                content
            );

            return (
                default!,
                "Erro ao interpretar resposta da API.",
                HttpStatusCode.InternalServerError
            );
        }
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
            var parsed = JsonSerializer.Deserialize<ApiResponse<JsonElement>>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );

            return parsed?.Message;
        }
        catch
        {
            return null;
        }
    }
}