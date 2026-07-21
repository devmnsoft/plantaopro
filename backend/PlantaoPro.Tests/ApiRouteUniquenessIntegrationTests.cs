using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using PlantaoPro.Api;
using Xunit;

namespace PlantaoPro.Tests;

public sealed class ApiRouteUniquenessIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;
    public ApiRouteUniquenessIntegrationTests(WebApplicationFactory<Program> factory) { this.factory = factory.WithWebHostBuilder(b => b.UseSetting("environment", "Testing")); }

    [Fact]
    public void TodasAsRotasDaApiDevemSerUnicasPorMetodoECaminhoNormalizado()
    {
        using var scope = factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        var duplicates = provider.ApiDescriptionGroups.Items.SelectMany(x => x.Items)
            .Where(x => !string.IsNullOrWhiteSpace(x.HttpMethod))
            .GroupBy(x => new { Method = x.HttpMethod!.ToUpperInvariant(), Path = ApiRouteStartupValidator.NormalizePath(x.RelativePath) })
            .Where(x => x.Count() > 1).ToArray();
        var message = string.Join(Environment.NewLine, duplicates.Select(g => g.Key.Method + " " + g.Key.Path + Environment.NewLine + string.Join(Environment.NewLine, g.Select(d => d.ActionDescriptor.DisplayName))));
        Assert.True(duplicates.Length == 0, message);
    }

    [Theory]
    [InlineData("api/relatorios/exportar-csv?tipo=x", "/api/relatorios/exportar-csv")]
    [InlineData("/API/RELATORIOS/{id:guid}/", "/api/relatorios/{id}")]
    public void NormalizacaoDeRotasRemoveQueryConstraintsBarrasECaixa(string input, string expected)
    {
        Assert.Equal(expected, ApiRouteStartupValidator.NormalizePath(input));
    }
}
