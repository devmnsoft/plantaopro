using System.Net;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public static class WebListControllerExtensions
{
    public static async Task<IActionResult> RenderList<T>(this BaseWebController c, string endpoint)
    {
        var client = c.CreateApiClient();
        if (!c.AddBearerToken(client)) return c.HandleUnauthorized();
        var (data, error, status) = await c.ReadApiResponse<IEnumerable<T>>(client, endpoint);
        if (status == HttpStatusCode.Unauthorized) return c.HandleUnauthorized();
        if (status == HttpStatusCode.Forbidden) return c.View(new ListPageViewModel<T>(Array.Empty<T>(), "Acesso negado."));
        if (status == HttpStatusCode.NotFound) return c.View(new ListPageViewModel<T>(Array.Empty<T>(), "Endpoint não encontrado."));
        if ((int)status >= 500) return c.View(new ListPageViewModel<T>(Array.Empty<T>(), "Falha interna da API."));
        return c.View(new ListPageViewModel<T>(data ?? Array.Empty<T>(), null, error));
    }

    public static async Task<IActionResult> RenderPaged<T>(this BaseWebController c, string endpoint)
    {
        var client = c.CreateApiClient();
        if (!c.AddBearerToken(client)) return c.HandleUnauthorized();
        var (data, error, status) = await c.ReadApiResponse<PagedResult<T>>(client, endpoint);
        if (status == HttpStatusCode.Unauthorized) return c.HandleUnauthorized();
        if (status == HttpStatusCode.Forbidden) return c.View(new ListPageViewModel<T>(Array.Empty<T>(), "Acesso negado."));
        if (status == HttpStatusCode.NotFound) return c.View(new ListPageViewModel<T>(Array.Empty<T>(), "Endpoint não encontrado."));
        if ((int)status >= 500) return c.View(new ListPageViewModel<T>(Array.Empty<T>(), "Falha interna da API."));
        return c.View(new ListPageViewModel<T>(data?.Items ?? Array.Empty<T>(), null, error));
    }
}
