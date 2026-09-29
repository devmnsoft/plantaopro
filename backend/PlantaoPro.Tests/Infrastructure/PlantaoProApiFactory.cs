using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace PlantaoPro.Tests.Infrastructure;

public sealed class PlantaoProApiFactory : WebApplicationFactory<PlantaoPro.Api.ApiAssemblyMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = TestDatabase.ConnectionString,
            ["Jwt:Key"] = "__SET_VIA_USER_SECRETS_OR_CI_SECRET_32_CHARS__",
            ["Jwt:Issuer"] = "PlantaoPro.Testing",
            ["Jwt:Audience"] = "PlantaoPro.Testing",
            ["DatabaseStartup:Validate"] = "false"
        }));
    }
}
