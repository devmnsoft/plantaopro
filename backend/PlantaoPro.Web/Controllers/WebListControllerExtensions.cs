using System.Net;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public static class WebListControllerExtensions
{
    public static async Task<IActionResult> RenderList<T>(this BaseWebController controller, string endpoint, int page = 1, int pageSize = 20)
    {
        var client = controller.CreateApiClient();
        if (!controller.AddBearerToken(client)) return controller.HandleUnauthorized();
        var result = await controller.ReadApiResponse<IEnumerable<T>>(client, endpoint);
        if (result.StatusCode == HttpStatusCode.Unauthorized) return controller.HandleUnauthorized();
        var items = result.Data ?? Array.Empty<T>();
        var total = items.LongCount();
        return controller.View(new ListPageViewModel<T>(items, page, pageSize, total, result.StatusCode == HttpStatusCode.OK ? null : result.Error, result.Error));
    }

    public static async Task<IActionResult> RenderPaged<T>(this BaseWebController controller, string endpoint)
    {
        var client = controller.CreateApiClient();
        if (!controller.AddBearerToken(client)) return controller.HandleUnauthorized();
        var result = await controller.ReadApiResponse<PagedResult<T>>(client, endpoint);
        if (result.StatusCode == HttpStatusCode.Unauthorized) return controller.HandleUnauthorized();
        return controller.View(new ListPageViewModel<T>(result.Data?.Items ?? Array.Empty<T>(), result.Data?.Page ?? 1, result.Data?.PageSize ?? 20, result.Data?.Total ?? 0, result.StatusCode == HttpStatusCode.OK ? null : result.Error, result.Error));
    }

    public static async Task<DetailsPageViewModel<T>> RenderDetails<T>(this BaseWebController controller, string endpoint)
    {
        var client = controller.CreateApiClient();
        if (!controller.AddBearerToken(client)) return new DetailsPageViewModel<T>(default, "Sessão expirada.", true);
        var result = await controller.ReadApiResponse<T>(client, endpoint);
        if (result.StatusCode == HttpStatusCode.NotFound) return new DetailsPageViewModel<T>(default, "Detalhamento em preparação.", true);
        if ((int)result.StatusCode >= 400) return new DetailsPageViewModel<T>(default, result.Error ?? "Falha ao carregar detalhes.", true);
        return new DetailsPageViewModel<T>(result.Data);
    }
}
