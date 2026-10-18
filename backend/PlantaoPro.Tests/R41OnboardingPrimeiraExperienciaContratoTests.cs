using System.Text;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R41 (diretriz r24a1, epic E9 — onboarding de novo cliente + primeira experiência): duas lacunas
/// reais foram fechadas: (A) as descrições canônicas dos critérios da jornada de implantação
/// exibiam "tenant" em 4 pontos visíveis ao admin da instituição — agora dizem "sua instituição";
/// (B) a primeira tela do admin da conta (MeuDia) não apontava para o checklist de implantação —
/// agora o controller carrega o status com best-effort (falha oculta o aviso, nunca derruba a
/// página) e a view exibe um card com progresso/proxima ação e atalho para /Onboarding.
/// Plano: docs/evidencias/2026-10-18-rodada-41/r41a0-e9-onboarding-primeira-experiencia.md.
/// </summary>
public class R41OnboardingPrimeiraExperienciaContratoTests
{
    [Fact]
    public void CriteriosCanonicosDaJornada_SemTenantNoTextoRenderizado()
    {
        var p = Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Api", "OnboardingJornadaService.cs");
        var texto = File.ReadAllText(p, Encoding.UTF8);
        var inicio = texto.IndexOf("CriterioDescricoes", StringComparison.Ordinal);
        Assert.True(inicio >= 0, "dicionário CriterioDescricoes não encontrado");
        var fim = texto.IndexOf("};", inicio, StringComparison.Ordinal);
        Assert.True(fim > inicio, "fim do dicionário não encontrado");
        var dicionario = texto.Substring(inicio, fim - inicio + 2);
        Assert.DoesNotContain("tenant", dicionario, StringComparison.OrdinalIgnoreCase);
        // Amostra de positivos: as reescritas existem, não foram apagadas.
        Assert.Contains("grade da sua instituição", dicionario, StringComparison.Ordinal);
        Assert.Contains("plantao da sua instituição", dicionario, StringComparison.Ordinal);
        Assert.Contains("Paciente cadastrado na sua instituição", dicionario, StringComparison.Ordinal);
        Assert.Contains("fluxo assistido da sua instituição", dicionario, StringComparison.Ordinal);
    }

    [Fact]
    public void MeuDia_CarregaEstadoDaImplantacaoComBestEffort()
    {
        var p = Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Controllers", "MeuDiaController.cs");
        var texto = File.ReadAllText(p, Encoding.UTF8);
        Assert.Contains("GetOnboardingStatusAsync", texto, StringComparison.Ordinal);
        Assert.Contains("OnboardingCarregado = true", texto, StringComparison.Ordinal);
        // Só o admin da conta recebe o aviso; médico e visão global não.
        Assert.Contains("RolesConstants.AdministradorCliente", texto, StringComparison.Ordinal);

        var svc = Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Services", "ProductivityWebService.cs");
        var svcTexto = File.ReadAllText(svc, Encoding.UTF8);
        Assert.Contains("api/onboarding/status", svcTexto, StringComparison.Ordinal);
        Assert.Contains("HttpStatusCode.NotFound", svcTexto, StringComparison.Ordinal);
    }

    [Fact]
    public void MeuDia_ExibeAvisoComProgressoEAtalhoParaOChecklist()
    {
        var p = Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Views", "MeuDia", "Index.cshtml");
        var v = File.ReadAllText(p, Encoding.UTF8);
        Assert.Contains("Sua implantação está", v, StringComparison.Ordinal);
        Assert.Contains("Prepare sua conta para o primeiro uso", v, StringComparison.Ordinal);
        Assert.Contains("asp-controller=\"Onboarding\"", v, StringComparison.Ordinal);
        // O aviso nunca aparece finalizado.
        Assert.Contains("\"FINALIZADO\"", v, StringComparison.Ordinal);
    }
}
