using Microsoft.AspNetCore.Mvc;

namespace BarberSync.AdminWeb.Controllers;

public class AdminController : Controller
{
    private static readonly HashSet<string> Pages = new(StringComparer.OrdinalIgnoreCase)
    {
        "Dashboard","Clients","Professionals","Services","Appointments","ServiceOrders","Stock","Campaigns","Coupons","Reviews","Loyalty","Copilot","Kiosk","PublicSite","Reports","Help","DemoExperience","DemoCenter","Operations","CustomerJourney","SaasControlCenter","LeadToCash","ChannelManager","PlatformSettings","Users","Branches","Audit","Notifications"
    };

    public IActionResult Index() => RedirectToAction(nameof(Dashboard));
    public IActionResult Dashboard() => View("DemoPage", "Dashboard");
    public IActionResult Clients() => View("DemoPage", "Clients");
    public IActionResult Professionals() => View("DemoPage", "Professionals");
    public IActionResult Services() => View("DemoPage", "Services");
    public IActionResult Appointments() => View("DemoPage", "Appointments");
    public IActionResult ServiceOrders() => View("DemoPage", "ServiceOrders");
    public IActionResult Stock() => View("DemoPage", "Stock");
    public IActionResult Campaigns() => View("DemoPage", "Campaigns");
    public IActionResult Coupons() => View("DemoPage", "Coupons");
    public IActionResult Reviews() => View("DemoPage", "Reviews");
    public IActionResult Loyalty() => View("DemoPage", "Loyalty");
    public IActionResult Copilot() => View("DemoPage", "Copilot");
    public IActionResult Kiosk() => View("DemoPage", "Kiosk");
    public IActionResult PublicSite() => View("DemoPage", "PublicSite");
    public IActionResult Reports() => View("DemoPage", "Reports");
    public IActionResult Help() => View("DemoPage", "Help");
    public IActionResult DemoExperience() => View("DemoPage", "DemoExperience");
    public IActionResult DemoCenter() => View("DemoPage", "DemoCenter");
    public IActionResult Operations() => View("DemoPage", "Operations");
    public IActionResult CustomerJourney() => View("DemoPage", "CustomerJourney");
    public IActionResult SaasControlCenter() => View("DemoPage", "SaasControlCenter");
    public IActionResult LeadToCash() => View("DemoPage", "LeadToCash");
    public IActionResult ChannelManager() => View("DemoPage", "ChannelManager");
    public IActionResult PlatformSettings() => View("DemoPage", "PlatformSettings");
    public IActionResult Users() => View("DemoPage", "Users");
    public IActionResult Branches() => View("DemoPage", "Branches");
    public IActionResult Audit() => View("DemoPage", "Audit");
    public IActionResult Notifications() => View("DemoPage", "Notifications");
    public IActionResult Error() => View("DemoPage", "Erro controlado");
}
