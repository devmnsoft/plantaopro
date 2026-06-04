var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient("BarberSyncApi", (sp, client) =>
{
    var baseUrl = sp.GetRequiredService<IConfiguration>()["ApiSettings:BaseUrl"] ?? "http://localhost:8080";
    client.BaseAddress = new Uri(baseUrl);
});
var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Admin/Error");
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute("default", "{controller=Admin}/{action=Index}/{id?}");
app.Run();
