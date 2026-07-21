using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace PlantaoPro.Tests;

public sealed class SwaggerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;
    public SwaggerIntegrationTests(WebApplicationFactory<Program> factory) { this.factory = factory.WithWebHostBuilder(b => b.UseSetting("environment", "Development")); }

    [Fact]
    public async Task SwaggerJsonDeveResponder200ComBearerPathsEOperacoesUnicas()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.TryGetProperty("paths", out var paths));
        Assert.True(paths.EnumerateObject().Any(x => x.Name.Contains("/api/relatorios/executivos/exportar-csv") || x.Name.Contains("api/relatorios/executivos/exportar-csv")));
        Assert.True(paths.EnumerateObject().Any(x => x.Name.Contains("/api/relatorios/valor/exportar-csv") || x.Name.Contains("api/relatorios/valor/exportar-csv")));
        Assert.True(doc.RootElement.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("Bearer", out _));
        var operationIds = paths.EnumerateObject().SelectMany(p => p.Value.EnumerateObject()).Where(m => m.Value.TryGetProperty("operationId", out _)).Select(m => m.Value.GetProperty("operationId").GetString()).ToArray();
        Assert.Equal(operationIds.Length, operationIds.Distinct(StringComparer.Ordinal).Count());
    }
}
