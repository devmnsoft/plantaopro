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
            ["ConnectionStrings:Default"] = Environment.GetEnvironmentVariable("PLANTAOPRO_TEST_CONNECTION")
                ?? Environment.GetEnvironmentVariable("PLANTAOPRO_CONNECTION_STRING")
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                ?? "Host=127.0.0.1;Port=5432;Database=plantaopro_test;Username=postgres;Password=123456;Search Path=PlantaoPro,public",
            ["Jwt:Key"] = "__SET_VIA_USER_SECRETS_OR_CI_SECRET_32_CHARS__",
            ["Jwt:Issuer"] = "PlantaoPro.Testing",
            ["Jwt:Audience"] = "PlantaoPro.Testing",
            ["DatabaseStartup:Validate"] = "false"
        }));
    }
}
