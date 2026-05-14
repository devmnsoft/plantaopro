using System.Net;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public static class WebListControllerExtensions
{
    public static async Task<IActionResult> RenderList<T>(
        this BaseWebController controller,
        string endpoint)
    {
        var client = controller.CreateApiClient();

        if (!controller.AddBearerToken(client))
            return controller.HandleUnauthorized();

        var result = await controller.ReadApiResponse<IEnumerable<T>>(client, endpoint);

        if (result.StatusCode == HttpStatusCode.Unauthorized)
            return controller.HandleUnauthorized();

        if (result.StatusCode == HttpStatusCode.Forbidden)
            return controller.View(new ListPageViewModel<T>(
                Array.Empty<T>(),
                "Acesso negado."
            ));

        if (result.StatusCode == HttpStatusCode.NotFound)
            return controller.View(new ListPageViewModel<T>(
                Array.Empty<T>(),
                "Endpoint não encontrado."
            ));

        if ((int)result.StatusCode >= 500)
            return controller.View(new ListPageViewModel<T>(
                Array.Empty<T>(),
                "Falha interna da API."
            ));

        return controller.View(new ListPageViewModel<T>(
            result.Data ?? Array.Empty<T>(),
            null,
            result.Error
        ));
    }

    public static async Task<IActionResult> RenderPaged<T>(
        this BaseWebController controller,
        string endpoint)
    {
        var client = controller.CreateApiClient();

        if (!controller.AddBearerToken(client))
            return controller.HandleUnauthorized();

        var result = await controller.ReadApiResponse<PagedResult<T>>(client, endpoint);

        if (result.StatusCode == HttpStatusCode.Unauthorized)
            return controller.HandleUnauthorized();

        if (result.StatusCode == HttpStatusCode.Forbidden)
            return controller.View(new ListPageViewModel<T>(
                Array.Empty<T>(),
                "Acesso negado."
            ));

        if (result.StatusCode == HttpStatusCode.NotFound)
            return controller.View(new ListPageViewModel<T>(
                Array.Empty<T>(),
                "Endpoint não encontrado."
            ));

        if ((int)result.StatusCode >= 500)
            return controller.View(new ListPageViewModel<T>(
                Array.Empty<T>(),
                "Falha interna da API."
            ));

        return controller.View(new ListPageViewModel<T>(
            result.Data?.Items ?? Array.Empty<T>(),
            null,
            result.Error
        ));
    }
}