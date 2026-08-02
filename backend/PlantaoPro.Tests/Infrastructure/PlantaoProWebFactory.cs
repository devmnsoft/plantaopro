using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
namespace PlantaoPro.Tests.Infrastructure;
public sealed class PlantaoProWebFactory : WebApplicationFactory<PlantaoPro.Web.WebAssemblyMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");
}
