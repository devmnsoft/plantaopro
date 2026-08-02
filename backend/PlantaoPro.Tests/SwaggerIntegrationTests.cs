using System.Text.Json;
using PlantaoPro.Api;
using Xunit;
using PlantaoPro.Tests.Infrastructure;

namespace PlantaoPro.Tests;

public sealed class SwaggerIntegrationTests : IClassFixture<PlantaoProApiFactory>
{
    private readonly PlantaoProApiFactory factory;
    public SwaggerIntegrationTests(PlantaoProApiFactory factory) { this.factory = factory; }

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
