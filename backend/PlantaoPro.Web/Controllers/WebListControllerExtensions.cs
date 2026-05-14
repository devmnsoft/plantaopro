using System.Net;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public static class WebListControllerExtensions
{
    public static async Task<IActionResult> RenderList<T>(this BaseWebController controller, string endpoint)
    {
        var client = controller.CreateApiClient();
        if (!controller.AddBearerToken(client)) return controller.HandleUnauthorized();

        var result = await controller.ReadApiResponse<IEnumerable<T>>(client, endpoint);
        if (result.StatusCode == HttpStatusCode.Unauthorized) return controller.HandleUnauthorized();

        var items = result.Data ?? Array.Empty<T>();
        if (result.StatusCode == HttpStatusCode.Forbidden) return controller.View(new ListPageViewModel<T>(Array.Empty<T>(), "Acesso negado."));
        if (result.StatusCode == HttpStatusCode.NotFound) return controller.View(new ListPageViewModel<T>(Array.Empty<T>(), "Endpoint não encontrado."));
        if ((int)result.StatusCode >= 500) return controller.View(new ListPageViewModel<T>(Array.Empty<T>(), "Falha interna da API."));

        return controller.View(new ListPageViewModel<T>(items, null, result.Error, items.LongCount(), 1, items.Any() ? items.Count() : 20));
    }

    public static async Task<IActionResult> RenderPaged<T>(this BaseWebController controller, string endpoint)
    {
        var client = controller.CreateApiClient();
        if (!controller.AddBearerToken(client)) return controller.HandleUnauthorized();

        var result = await controller.ReadApiResponse<PagedResult<T>>(client, endpoint);
        if (result.StatusCode == HttpStatusCode.Unauthorized) return controller.HandleUnauthorized();
        if (result.StatusCode == HttpStatusCode.Forbidden) return controller.View(new ListPageViewModel<T>(Array.Empty<T>(), "Acesso negado."));
        if (result.StatusCode == HttpStatusCode.NotFound) return controller.View(new ListPageViewModel<T>(Array.Empty<T>(), "Endpoint não encontrado."));
        if ((int)result.StatusCode >= 500) return controller.View(new ListPageViewModel<T>(Array.Empty<T>(), "Falha interna da API."));

        var data = result.Data;
        var items = data?.Items ?? Array.Empty<T>();
        return controller.View(new ListPageViewModel<T>(items, null, result.Error, data?.Total ?? 0, data?.Page ?? 1, data?.PageSize ?? 20));
    }
}
