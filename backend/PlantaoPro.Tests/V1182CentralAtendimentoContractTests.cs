namespace PlantaoPro.Tests;

public sealed class V1182CentralAtendimentoContractTests
{
    private static readonly string Root = FindRepoRoot();

    [Fact]
    public void WebNaoDeveConfigurarSwaggerEApiDeveManterSwagger()
    {
        var webProgram = File.ReadAllText(Path.Combine(Root, "backend/PlantaoPro.Web/Program.cs"));
        Assert.DoesNotContain("AddSwaggerGen", webProgram, StringComparison.Ordinal);
        Assert.DoesNotContain("UseSwagger", webProgram, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.OpenApi.Models", webProgram, StringComparison.Ordinal);

        var apiProgram = File.ReadAllText(Path.Combine(Root, "backend/PlantaoPro.Api/Program.cs"));
        Assert.Contains("AddSwaggerGen", apiProgram, StringComparison.Ordinal);
        Assert.Contains("UseSwagger", apiProgram, StringComparison.Ordinal);
    }

    [Fact]
    public void CentralAtendimentoDeveTerControllerServiceViewModelEView()
    {
        Assert.True(File.Exists(Path.Combine(Root, "backend/PlantaoPro.Api/Controllers/CentralAtendimentoController.cs")));
        var service = File.ReadAllText(Path.Combine(Root, "backend/PlantaoPro.Api/CentralAtendimentoService.cs"));
        Assert.Contains("interface ICentralAtendimentoService", service, StringComparison.Ordinal);
        Assert.Contains("class CentralAtendimentoService", service, StringComparison.Ordinal);
        Assert.Contains("cliente_id=@tenantId or a.tenant_id=@tenantId", service, StringComparison.Ordinal);
        Assert.Contains("ApiResponse<CentralAtendimentoResumoDto>", service, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(Root, "backend/PlantaoPro.Web/Controllers/CentralAtendimentoController.cs")));
        Assert.True(File.Exists(Path.Combine(Root, "backend/PlantaoPro.Web/Models/CentralAtendimentoViewModels.cs")));
        Assert.True(File.Exists(Path.Combine(Root, "backend/PlantaoPro.Web/Views/CentralAtendimento/Index.cshtml")));
    }

    [Fact]
    public void PoliciesClinicasDevemExistirNaApiENoWeb()
    {
        var apiProgram = File.ReadAllText(Path.Combine(Root, "backend/PlantaoPro.Api/Program.cs"));
        var webProgram = File.ReadAllText(Path.Combine(Root, "backend/PlantaoPro.Web/Program.cs"));
        foreach (var policy in new[] { "CentralAtendimento.Ver", "Agendamento.Criar", "Agendamento.Confirmar", "Agendamento.CheckIn", "PainelChamada.Operar", "Triagem.Iniciar", "Triagem.Finalizar", "Consulta.Iniciar", "Consulta.Editar", "Consulta.Finalizar", "Consulta.VerDadosSensiveis" })
        {
            Assert.Contains(policy, apiProgram, StringComparison.Ordinal);
            Assert.Contains(policy, webProgram, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void MigratorDeveExporInstallBaselineUpgradeComChecksumReal()
    {
        var script = File.ReadAllText(Path.Combine(Root, "scripts/apply-canonical-migrations.sh"));
        Assert.Contains("install|baseline|upgrade", script, StringComparison.Ordinal);
        Assert.Contains("sha256sum", script, StringComparison.Ordinal);
        Assert.Contains("Checksum mismatch", script, StringComparison.Ordinal);
        Assert.DoesNotContain("000000000000", script, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "backend/PlantaoPro.sln"))) dir = dir.Parent;
        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }
}
