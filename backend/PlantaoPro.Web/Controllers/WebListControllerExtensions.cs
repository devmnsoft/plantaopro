using System.Net;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public static class WebListControllerExtensions
{
    public static async Task<IActionResult> RenderList<T>(
        this BaseWebController controller,
        string endpoint,
        int page = 1,
        int pageSize = 20)
    {
        var client = controller.CreateApiClient();

        if (!controller.AddBearerToken(client))
            return controller.HandleUnauthorized();

        var result = await controller.ReadApiResponse<IEnumerable<T>>(client, endpoint);

        if (result.StatusCode == HttpStatusCode.Unauthorized)
            return controller.HandleUnauthorized();

        var items = result.Data ?? Array.Empty<T>();
        var total = items.LongCount();

        var errorMessage = result.StatusCode == HttpStatusCode.OK ? null : result.Error;

        return controller.View(new ListPageViewModel<T>(
            Items: items,
            ErrorMessage: errorMessage,
            InfoMessage: result.StatusCode == HttpStatusCode.OK ? result.Error : null,
            Total: total,
            Page: page,
            PageSize: pageSize
        ));
    }

    public static async Task<IActionResult> RenderPaged<T>(
        this BaseWebController controller,
        string endpoint)
    {
        var client = controller.CreateApiClient();

        if (!controller.AddBearerToken(client))
            return controller.HandleUnauthorized();

        var result = await controller.ReadApiPagedResponseAsync<T>(client, endpoint);

        if (result.StatusCode == HttpStatusCode.Unauthorized)
            return controller.HandleUnauthorized();

        var data = result.Data ?? PagedResult<T>.Empty();

        var errorMessage = result.StatusCode == HttpStatusCode.OK ? null : result.Error;

        return controller.View(new ListPageViewModel<T>(
            Items: data?.Items ?? Array.Empty<T>(),
            ErrorMessage: errorMessage,
            InfoMessage: result.StatusCode == HttpStatusCode.OK ? result.Error : null,
            Total: data?.Total ?? 0,
            Page: data?.Page ?? 1,
            PageSize: data?.PageSize ?? 20
        ));
    }

    public static async Task<DetailsPageViewModel<T>> RenderDetails<T>(
        this BaseWebController controller,
        string endpoint)
    {
        var client = controller.CreateApiClient();

        if (!controller.AddBearerToken(client))
        {
            return new DetailsPageViewModel<T>(
                Data: default,
                ErrorMessage: "Sessão expirada.",
                IsPlaceholder: true
            );
        }

        var result = await controller.ReadApiResponse<T>(client, endpoint);

        if (result.StatusCode == HttpStatusCode.NotFound)
        {
            return new DetailsPageViewModel<T>(
                Data: default,
                ErrorMessage: "Detalhamento em preparação.",
                IsPlaceholder: true
            );
        }

        if ((int)result.StatusCode >= 400)
        {
            return new DetailsPageViewModel<T>(
                Data: default,
                ErrorMessage: result.Error ?? "Falha ao carregar detalhes.",
                IsPlaceholder: true
            );
        }

        return new DetailsPageViewModel<T>(
            Data: result.Data,
            ErrorMessage: null,
            IsPlaceholder: false
        );
    }
}
