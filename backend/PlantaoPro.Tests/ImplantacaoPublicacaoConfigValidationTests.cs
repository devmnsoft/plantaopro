using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using PlantaoPro.Web.Services.Security;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// A2 (rodada 4): validação de configuração na inicialização do Web (publicação/IIS).
/// Testes puros de unidade: sem banco, sem HTTP — o contrato é "mensagem clara, sem segredo,
/// fail-fast na porta certa e na hora certa".
/// </summary>
public sealed class ImplantacaoPublicacaoConfigValidationTests
{
    private sealed class EnvironmentStub : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "PlantaoPro.Web";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRoot { get; set; } = new PhysicalFileProvider(AppContext.BaseDirectory);
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(AppContext.BaseDirectory);
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = new PhysicalFileProvider(AppContext.BaseDirectory);
    }

    private static EnvironmentStub Env(string name) => new() { EnvironmentName = name };

    private static IConfiguration Config(params (string Key, string? Value)[] pairs)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(pairs.ToDictionary(p => p.Key, p => p.Value))
            .Build();

    // ---------- Precedência da BaseUrl ----------

    [Fact]
    public void ResolveBaseUrl_PreferenciaApiSettingsSobrePlantaoProApi()
    {
        var cfg = Config(("ApiSettings:BaseUrl", "http://127.0.0.1:9001/"), ("PlantaoProApi:BaseUrl", "http://127.0.0.1:8197/"));
        Assert.Equal("http://127.0.0.1:9001/", PlantaoProApiStartupValidator.ResolveBaseUrl(cfg));
    }

    [Fact]
    public void ResolveBaseUrl_UsaPlantaoProApiQuandoApiSettingsAusente()
    {
        var cfg = Config(("PlantaoProApi:BaseUrl", "http://127.0.0.1:8197/"));
        Assert.Equal("http://127.0.0.1:8197/", PlantaoProApiStartupValidator.ResolveBaseUrl(cfg));
    }

    [Fact]
    public void ResolveBaseUrl_AmbasAusentes_RetornaNulo()
    {
        Assert.Null(PlantaoProApiStartupValidator.ResolveBaseUrl(Config()));
    }

    // ---------- BaseUrl: presença e formato ----------

    [Fact]
    public void ValidateBaseUrl_Ausente_LancaMensagemClaraSemSegredo()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => PlantaoProApiStartupValidator.ValidateBaseUrl(Config(), Env("Production")));
        Assert.Contains("PlantaoProApi:BaseUrl", ex.Message);
        Assert.Contains("não encontrada", ex.Message);
        Assert.Contains("PlantaoProApi__BaseUrl", ex.Message);
    }

    [Fact]
    public void ValidateBaseUrl_UrlRelativaOuInvalida_Lanca()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => PlantaoProApiStartupValidator.ValidateBaseUrl(Config(("PlantaoProApi:BaseUrl", "api/interna")), Env("Production")));
        Assert.Contains("inválida", ex.Message);
    }

    [Fact]
    public void ValidateBaseUrl_EsquemaNaoHttp_Lanca()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => PlantaoProApiStartupValidator.ValidateBaseUrl(Config(("PlantaoProApi:BaseUrl", "ftp://interno/api/")), Env("Production")));
        Assert.Contains("http ou https", ex.Message);
    }

    [Fact]
    public void ValidateBaseUrl_ProducaoComUrlInternaValida_Aceita()
    {
        PlantaoProApiStartupValidator.ValidateBaseUrl(Config(("PlantaoProApi:BaseUrl", "http://127.0.0.1:8197/")), Env("Production"));
    }

    // ---------- BaseUrl: portas de desenvolvimento ----------

    [Theory]
    [InlineData(51976)]
    [InlineData(51977)]
    [InlineData(52976)]
    [InlineData(52977)]
    public void ValidateBaseUrl_ProducaoComPortaDeDesenvolvimento_Lanca(int porta)
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => PlantaoProApiStartupValidator.ValidateBaseUrl(Config(("PlantaoProApi:BaseUrl", $"https://localhost:{porta}/")), Env("Production")));
        Assert.Contains("porta de desenvolvimento", ex.Message);
        Assert.Contains(porta.ToString(), ex.Message);
    }

    [Fact]
    public void ValidateBaseUrl_DevComPortaDeDesenvolvimento_Aceita()
    {
        PlantaoProApiStartupValidator.ValidateBaseUrl(Config(("PlantaoProApi:BaseUrl", "https://localhost:51977/")), Env("Development"));
    }

    [Fact]
    public void ValidateBaseUrl_TestingComPortaDeDesenvolvimento_Aceita()
    {
        // A fábrica Web (ambiente Testing) herda a BaseUrl versionada antes do override.
        PlantaoProApiStartupValidator.ValidateBaseUrl(Config(("PlantaoProApi:BaseUrl", "https://localhost:51977/")), Env("Testing"));
    }

    // ---------- Data Protection: persistência de chaves ----------

    [Fact]
    public void ValidateDataProtection_ProducaoSemDiretorio_LancaMensagemClara()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => PlantaoProApiStartupValidator.ValidateDataProtectionKeysDirectory(Config(), Env("Production")));
        Assert.Contains("DataProtection:KeysDirectory", ex.Message);
        Assert.Contains("reciclagem", ex.Message);
    }

    [Fact]
    public void ValidateDataProtection_ProducaoDiretorioInexistente_Lanca()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ppr4a2_" + Guid.NewGuid().ToString("N"));
        try
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => PlantaoProApiStartupValidator.ValidateDataProtectionKeysDirectory(Config(("DataProtection:KeysDirectory", dir)), Env("Production")));
            Assert.Contains("não existe", ex.Message);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ValidateDataProtection_ProducaoDiretorioExistente_Aceita()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ppr4a2_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            PlantaoProApiStartupValidator.ValidateDataProtectionKeysDirectory(Config(("DataProtection:KeysDirectory", dir)), Env("Production"));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ValidateDataProtection_DevSemDiretorio_NaoLanca()
    {
        PlantaoProApiStartupValidator.ValidateDataProtectionKeysDirectory(Config(), Env("Development"));
    }

    [Fact]
    public void ValidateDataProtection_DevComDiretorioInexistente_CriaDiretorio()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ppr4a2_" + Guid.NewGuid().ToString("N"));
        try
        {
            PlantaoProApiStartupValidator.ValidateDataProtectionKeysDirectory(Config(("DataProtection:KeysDirectory", dir)), Env("Development"));
            Assert.True(Directory.Exists(dir));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    // ---------- Contrato do Validate completo ----------

    [Fact]
    public void Validate_ProducaoCompleta_Valido_NaoLanca()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ppr4a2_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            PlantaoProApiStartupValidator.Validate(
                Config(("PlantaoProApi:BaseUrl", "http://127.0.0.1:8197/"), ("DataProtection:KeysDirectory", dir)),
                Env("Production"));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Validate_ProducaoComDefeito_FalhaRapidamente()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ppr4a2_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => PlantaoProApiStartupValidator.Validate(
                    Config(("PlantaoProApi:BaseUrl", "https://localhost:51977/"), ("DataProtection:KeysDirectory", dir)),
                    Env("Production")));
            Assert.Contains("porta de desenvolvimento", ex.Message);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
