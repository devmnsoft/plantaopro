using PlantaoPro.Api;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Tests;

public class SaasLimitsAndPremiumFeaturesContractTests
{
    [Fact]
    public void PlanoRequest_DeveExporLimitesOperacionaisEPremium()
    {
        var request = new PlanoComercialRequest(
            "Enterprise",
            "Plano com controle operacional completo",
            1990m,
            100,
            20,
            500,
            true,
            true,
            true,
            true,
            LimiteUsuarios: 50,
            LimiteConvitesMes: 1000,
            PermiteOperacaoAssistida: true,
            PermiteSuportePrioritario: true);

        Assert.Equal(50, request.LimiteUsuarios);
        Assert.Equal(1000, request.LimiteConvitesMes);
        Assert.True(request.PermiteOperacaoAssistida);
        Assert.True(request.PermiteSuportePrioritario);
    }

    [Fact]
    public void AssinaturaGuard_DeveConterRegrasParaUsuariosConvitesOperacaoAssistidaESuportePrioritario()
    {
        var raiz = EncontrarRaizRepositorio();
        var guard = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Api", "TenantServices.cs"));
        var mobile = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Api", "Controllers", "MobileController.cs"));
        var operacaoAssistida = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Api", "Controllers", "OperacaoAssistidaController.cs"));

        Assert.Contains("Limite de usuários do plano atingido", guard, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Limite mensal de convites do plano atingido", guard, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PodeUsarOperacaoAssistidaAsync", guard, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PodeUsarSuportePrioritarioAsync", guard, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PodeUsarSuportePrioritarioAsync", mobile, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PodeUsarOperacaoAssistidaAsync", operacaoAssistida, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MigracaoAuditavel_DeveMaterializarLimitesEFlagsPremiumDoPlano()
    {
        var raiz = EncontrarRaizRepositorio();
        var sql = File.ReadAllText(Path.Combine(raiz, "database", "migrations", "2026_plantao_pro_saas_inteligente_auditavel.sql"));

        foreach (var coluna in new[]
        {
            "limite_usuarios",
            "limite_convites_mes",
            "permite_operacao_assistida",
            "permite_suporte_prioritario"
        })
        {
            Assert.Contains(coluna, sql, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("ADD CONSTRAINT IF NOT EXISTS", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static string EncontrarRaizRepositorio()
    {
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (diretorio is not null && !Directory.Exists(Path.Combine(diretorio.FullName, ".git"))) diretorio = diretorio.Parent;
        if (diretorio is null) throw new InvalidOperationException("Raiz do repositório não encontrada para testes de contrato.");
        return diretorio.FullName;
    }
}
