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
            ["ConnectionStrings:Default"] = Environment.GetEnvironmentVariable("PLANTAOPRO_TEST_CONNECTION") ?? "Host=localhost;Port=5432;Database=plantaopro_test;Username=postgres;Password=postgres",
            ["Jwt:Key"] = "testing-only-key-with-at-least-thirty-two-characters",
            ["Jwt:Issuer"] = "PlantaoPro",
            ["Jwt:Audience"] = "PlantaoPro",
            ["DatabaseStartup:Validate"] = "true"
        }));
    }
}
