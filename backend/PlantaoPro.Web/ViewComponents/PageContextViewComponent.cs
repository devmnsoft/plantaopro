using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Services;

namespace PlantaoPro.Web.ViewComponents;

public sealed class PageContextViewComponent : ViewComponent
{
    private readonly IPageContextService pageContext;

    public PageContextViewComponent(IPageContextService pageContext) => this.pageContext = pageContext;

    public IViewComponentResult Invoke()
    {
        var controller = RouteData.Values["controller"]?.ToString() ?? string.Empty;
        var action = RouteData.Values["action"]?.ToString() ?? "Index";
        var tenant = UserClaimsPrincipal.FindFirst("tenant_name")?.Value;
        return View(pageContext.Resolve(controller, action, tenant));
    }
}
