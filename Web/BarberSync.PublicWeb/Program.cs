var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();builder.Services.AddHttpClient("BarberSyncApi", (sp,c)=>c.BaseAddress=new Uri(sp.GetRequiredService<IConfiguration>()["ApiSettings:BaseUrl"]??"http://localhost:8080"));
var app=builder.Build();app.UseStaticFiles();app.UseRouting();app.MapControllerRoute("default","{controller=Home}/{action=Index}/{id?}");app.Run();
