using Microsoft.AspNetCore.Mvc;

namespace BarberSync.AdminWeb.Controllers;

[ApiController]
[Route("AdminApi")]
public class AdminApiController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AdminApiController> _logger;
    public AdminApiController(IHttpClientFactory httpClientFactory, ILogger<AdminApiController> logger)
    { _httpClientFactory = httpClientFactory; _logger = logger; }

    private static readonly object[] Services =
    {
        new { id = "svc-corte", name = "Corte Masculino", duration = 35, price = 45.00m, active = true, site = true, kiosk = true, mobile = true },
        new { id = "svc-barba", name = "Barba Tradicional", duration = 25, price = 35.00m, active = true, site = true, kiosk = true, mobile = true },
        new { id = "svc-combo", name = "Corte + Barba", duration = 60, price = 75.00m, active = true, site = true, kiosk = true, mobile = true },
        new { id = "svc-sobrancelha", name = "Sobrancelha", duration = 15, price = 25.00m, active = true, site = true, kiosk = true, mobile = false },
        new { id = "svc-hidratacao", name = "Hidratação Capilar", duration = 45, price = 90.00m, active = true, site = true, kiosk = true, mobile = true },
        new { id = "svc-manicure", name = "Manicure", duration = 50, price = 55.00m, active = true, site = true, kiosk = true, mobile = true }
    };
    private static readonly object[] Professionals =
    {
        new { id = "pro-rafael", name = "Rafael Barber", specialty = "Cortes clássicos", rating = 4.9, active = true, revenue = 12800 },
        new { id = "pro-lucas", name = "Lucas Navalha", specialty = "Barba e degradê", rating = 4.8, active = true, revenue = 11350 },
        new { id = "pro-bruno", name = "Bruno Estilo", specialty = "Coloração e combos", rating = 4.7, active = true, revenue = 9800 },
        new { id = "pro-camila", name = "Camila Beauty", specialty = "Sobrancelha e hidratação", rating = 4.9, active = true, revenue = 10420 },
        new { id = "pro-amanda", name = "Amanda Nails", specialty = "Manicure", rating = 4.8, active = true, revenue = 7600 }
    };
    private static object Envelope(object data, string message = "Operação realizada com sucesso.") => new { success = true, message, data };

    [HttpGet("dashboard")] public IActionResult Dashboard() => Ok(Envelope(new { revenueToday = 4850, appointmentsToday = 38, occupancy = 87, ticketAverage = 72.4, eventBus = 124 }));
    [HttpGet("clients")] public IActionResult Clients() => Ok(Envelope(new[] { new { id="cli-ana", name="Ana Souza", phone="(11) 98888-1001", visits=12, loyalty=840 }, new { id="cli-joao", name="João Lima", phone="(11) 97777-2020", visits=7, loyalty=460 }}));
    [HttpGet("professionals")] public IActionResult GetProfessionals() => Ok(Envelope(Professionals));
    [HttpGet("services")] public IActionResult GetServices() => Ok(Envelope(Services));
    [HttpGet("appointments")] public IActionResult Appointments() => Ok(Envelope(new[] { new { id="ag-001", client="Ana Souza", service="Corte + Barba", professional="Rafael Barber", time="10:30", status="Confirmado" }, new { id="ag-002", client="João Lima", service="Barba Tradicional", professional="Lucas Navalha", time="11:00", status="Check-in" }}));
    [HttpGet("service-orders")] public IActionResult Orders() => Ok(Envelope(new[] { new { id="cmd-1001", client="Ana Souza", total=110.0, status="Aberta" }}));
    [HttpGet("products")] public IActionResult Products() => Ok(Envelope(new[] { new { id="prd-01", name="Pomada Matte", stock=8, critical=10 }, new { id="prd-02", name="Shampoo Hidratante", stock=22, critical=6 }}));
    [HttpGet("stock-critical")] public IActionResult StockCritical() => Ok(Envelope(new[] { new { sku="POM-001", name="Pomada Matte", stock=8, minimum=10 }}));
    [HttpGet("campaigns")] public IActionResult Campaigns() => Ok(Envelope(new[] { new { id="camp-01", name="Volte em 30 dias", status="Ativa", conversions=18 }}));
    [HttpGet("coupons")] public IActionResult Coupons() => Ok(Envelope(new[] { new { code="BARBA10", discount=10, status="Ativo" }}));
    [HttpGet("reviews")] public IActionResult Reviews() => Ok(Envelope(new[] { new { client="Marcos", rating=5, comment="Atendimento excelente" }}));
    [HttpGet("loyalty")] public IActionResult Loyalty() => Ok(Envelope(new { members=420, pointsIssued=18400, redemptions=72 }));
    [HttpGet("copilot-suggestions")] public IActionResult Suggestions() => Ok(Envelope(new[] { "Abrir campanha para clientes inativos", "Repor pomadas abaixo do mínimo", "Oferecer combo corte + barba às 17h" }));
    [HttpGet("kiosk-status")] public IActionResult KioskStatus() => Ok(Envelope(new { deviceCode="KIOSK-DEMO-001", status="Online", flowCompletion=96 }));
    [HttpGet("financial-summary")] public IActionResult Financial() => Ok(Envelope(new { cash=4850, pix=2380, card=1870, pending=600 }));
    [HttpGet("reports-summary")] public IActionResult Reports() => Ok(Envelope(new { mrr=38900, churn=1.8, nps=82, branches=6 }));

    [HttpPost("clients")] public IActionResult PostClient([FromBody] object body) => Ok(Envelope(new { id = Guid.NewGuid(), body }));
    [HttpPut("clients/{id}")] public IActionResult PutClient(string id, [FromBody] object body) => Ok(Envelope(new { id, body }));
    [HttpDelete("clients/{id}")] public IActionResult DeleteClient(string id) => Ok(Envelope(new { id }));
    [HttpPost("professionals")] public IActionResult PostProfessional([FromBody] object body) => Ok(Envelope(new { id = Guid.NewGuid(), body }));
    [HttpPut("professionals/{id}")] public IActionResult PutProfessional(string id, [FromBody] object body) => Ok(Envelope(new { id, body }));
    [HttpDelete("professionals/{id}")] public IActionResult DeleteProfessional(string id) => Ok(Envelope(new { id }));
    [HttpPost("services")] public IActionResult PostService([FromBody] object body) => Ok(Envelope(new { id = Guid.NewGuid(), body }));
    [HttpPut("services/{id}")] public IActionResult PutService(string id, [FromBody] object body) => Ok(Envelope(new { id, body }));
    [HttpDelete("services/{id}")] public IActionResult DeleteService(string id) => Ok(Envelope(new { id }));
    [HttpPost("appointments")] public IActionResult PostAppointment([FromBody] object body) => Ok(Envelope(new { id = Guid.NewGuid(), protocol = $"BS-{DateTime.UtcNow:yyyyMMddHHmmss}", body }));
    [HttpPost("appointments/{id}/{actionName:regex(^(confirm|check-in|start|finish|cancel)$)}")] public IActionResult AppointmentAction(string id, string actionName) => Ok(Envelope(new { id, status = actionName }));
    [HttpPost("service-orders/open")] public IActionResult OpenOrder([FromBody] object body) => Ok(Envelope(new { id = $"cmd-{Random.Shared.Next(1000,9999)}", body }));
    [HttpPost("service-orders/{id}/{actionName:regex(^(pay|close)$)}")] public IActionResult OrderAction(string id, string actionName) => Ok(Envelope(new { id, status = actionName }));
    [HttpPost("stock/{actionName:regex(^(entry|exit)$)}")] public IActionResult StockAction(string actionName, [FromBody] object body) => Ok(Envelope(new { actionName, body }));
    [HttpPost("campaigns")] public IActionResult PostCampaign([FromBody] object body) => Ok(Envelope(new { id = Guid.NewGuid(), body }));
    [HttpPost("coupons")] public IActionResult PostCoupon([FromBody] object body) => Ok(Envelope(new { id = Guid.NewGuid(), body }));
    [HttpPost("copilot/ask")] public IActionResult Ask([FromBody] object body) => Ok(Envelope(new { answer = "Sugestão demo executada: campanha segmentada criada e evento registrado.", body }));
}
