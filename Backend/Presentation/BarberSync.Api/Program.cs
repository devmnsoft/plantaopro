var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();

object OkEnvelope(object data, string message = "Operação realizada com sucesso.") => new { success = true, message, data };

var services = new[]
{
    new { id = "svc-corte", name = "Corte Masculino", duration = 35, price = 45.00m, channel = "site/totem/mobile", active = true },
    new { id = "svc-barba", name = "Barba Tradicional", duration = 25, price = 35.00m, channel = "site/totem/mobile", active = true },
    new { id = "svc-combo", name = "Corte + Barba", duration = 60, price = 75.00m, channel = "site/totem/mobile", active = true },
    new { id = "svc-sobrancelha", name = "Sobrancelha", duration = 15, price = 25.00m, channel = "site/totem", active = true },
    new { id = "svc-hidratacao", name = "Hidratação Capilar", duration = 45, price = 90.00m, channel = "site/totem/mobile", active = true },
    new { id = "svc-manicure", name = "Manicure", duration = 50, price = 55.00m, channel = "site/totem/mobile", active = true }
};

var professionals = new[]
{
    new { id = "pro-rafael", name = "Rafael Barber", specialty = "Cortes clássicos", rating = 4.9, available = true },
    new { id = "pro-lucas", name = "Lucas Navalha", specialty = "Barba e degradê", rating = 4.8, available = true },
    new { id = "pro-bruno", name = "Bruno Estilo", specialty = "Coloração e combos", rating = 4.7, available = true },
    new { id = "pro-camila", name = "Camila Beauty", specialty = "Sobrancelha e hidratação", rating = 4.9, available = true },
    new { id = "pro-amanda", name = "Amanda Nails", specialty = "Manicure", rating = 4.8, available = true }
};

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/api/health", () => OkEnvelope(new { status = "online", product = "BarberSync SaaS Platform Demo 10.0" }));
app.MapGet("/api/services", () => OkEnvelope(services));
app.MapGet("/api/professionals", () => OkEnvelope(professionals));
app.MapGet("/api/dashboard/summary", () => OkEnvelope(new { revenueToday = 4850, appointmentsToday = 38, occupancy = 87, nps = 82, cashOpen = true }));
app.MapGet("/api/kiosk/services", (string? deviceCode) => OkEnvelope(services, $"Serviços carregados para {deviceCode ?? "KIOSK-DEMO-001"}."));
app.MapGet("/api/kiosk/professionals", (string? serviceId, string? deviceCode) => OkEnvelope(professionals, $"Profissionais disponíveis para {serviceId ?? "demo"}."));
app.MapPost("/api/appointments", (object request) => OkEnvelope(new { protocol = $"BS-{DateTime.UtcNow:yyyyMMddHHmmss}", request }));
app.MapPost("/api/kiosk/payment/mock", (object request) => OkEnvelope(new { transactionId = Guid.NewGuid(), authorized = true, request }));
app.MapPost("/api/kiosk/review", (object request) => OkEnvelope(new { received = true, request }));

app.Run();
