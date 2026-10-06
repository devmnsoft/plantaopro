using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R4-A5: /__test/signin (e /__test/dump) somente com flag explícita e apenas em Testing.
/// Prova por execução: (1) matrix do predicado canônico TestSigninController.Habilitado;
/// (2) hosts reais da aplicação em Production e Development SEM a flag respondendo 404 —
/// e também COM a flag, já que a flag só vale em Testing (proteção contra pool com
/// ambiente errado, finding D1); (3) host Testing da suíte com a flag emitindo a sessão.
/// </summary>
[Collection("web-bff")]
public sealed class TestSigninFlagGateTests
{
    private readonly PlantaoProWebFactory _factory;

    public TestSigninFlagGateTests(PlantaoProWebFactory factory) => _factory = factory;

    // ------------------------------------------------------------------
    // Matrix do predicado canônico (fonte única da guarda)
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("Production", null, false)]
    [InlineData("Production", "false", false)]
    [InlineData("Production", "true", false)]
    [InlineData("Development", null, false)]
    [InlineData("Development", "true", false)]
    [InlineData("Staging", "true", false)]
    [InlineData("Testing", null, false)]
    [InlineData("Testing", "", false)]
    [InlineData("Testing", "false", false)]
    [InlineData("Testing", "banana", false)]
    [InlineData("Testing", "true", true)]
    [InlineData("Testing", "TRUE", true)]
    [InlineData("testing", "true", true)]
    public void MatrixHabilitacao_SegueAPoliticaDeAmbienteeFlag(string ambiente, string? flag, bool esperado) =>
        Assert.Equal(esperado, PlantaoPro.Web.Controllers.TestSigninController.Habilitado(ambiente, flag));

    // ------------------------------------------------------------------
    // Prova por execução sobre hosts reais
    // ------------------------------------------------------------------

    [Fact]
    public async Task TestingComFlag_EmiteSessaoComAntiforgery()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("__test/signin");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"ok\":true", body);
        Assert.Contains("\"antiforgery\"", body);
    }

    [Fact]
    public async Task DevelopmentSemFlag_Responde404() =>
        await AssertHostAmbienteAsync("Development", flagLigada: false);

    [Fact]
    public async Task DevelopmentComFlag_Responde404MesmoAssim() =>
        await AssertHostAmbienteAsync("Development", flagLigada: true);

    [Fact]
    public async Task ProductionSemFlag_Responde404() =>
        await AssertHostAmbienteAsync("Production", flagLigada: false);

    [Fact]
    public async Task ProductionComFlag_Responde404MesmoAssim() =>
        await AssertHostAmbienteAsync("Production", flagLigada: true);

    private static async Task AssertHostAmbienteAsync(string ambiente, bool flagLigada)
    {
        using var factory = new FactoryAmbiente(ambiente, flagLigada);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var respSignin = await client.GetAsync("__test/signin");
        Assert.Equal(HttpStatusCode.NotFound, respSignin.StatusCode);
        var respDump = await client.GetAsync("__test/dump");
        Assert.Equal(HttpStatusCode.NotFound, respDump.StatusCode);
    }

    /// <summary>
    /// Host da aplicação Web em outro ambiente (mesmo Programa, mesma stub de API
    /// determinística). Em Production fornece DataProtection:KeysDirectory em diretório
    /// temporário — requisito fail-fast do validador de startup (A2).
    /// </summary>
    private sealed class FactoryAmbiente : WebApplicationFactory<PlantaoPro.Web.WebAssemblyMarker>
    {
        private readonly string _ambiente;
        private readonly bool _flagLigada;
        private readonly StubApiHandler _stub = new();

        public FactoryAmbiente(string ambiente, bool flagLigada)
        {
            _ambiente = ambiente;
            _flagLigada = flagLigada;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(_ambiente);
            builder.UseSetting("ApiSettings:BaseUrl", "http://api.test.local");
            if (_flagLigada)
                builder.UseSetting("TestAuth:Enabled", "true");
            if (_ambiente == "Production")
            {
                var dir = Path.Combine(Path.GetTempPath(), "plantao-pro-testkeys-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(dir);
                builder.UseSetting("DataProtection:KeysDirectory", dir);
            }
            builder.ConfigureTestServices(services => services.AddHttpClient("PlantaoProApi")
                .ConfigurePrimaryHttpMessageHandler(() => _stub));
        }
    }
}
