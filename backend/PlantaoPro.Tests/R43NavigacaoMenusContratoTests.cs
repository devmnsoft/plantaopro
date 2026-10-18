using System.Text;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R43 (diretriz r24a1, epic E0 design — navegação e menus): três correções de clareza/descobrimento.
/// (A) O menu lateral exibia o literal inglês "Command Center" fora do i18n — agora usa a chave
/// Nav.CommandCenter (4 culturas) e a própria tela adotou o mesmo rótulo (h1 + title);
/// (B) duas áreas do admin da conta não tinham item no menu — Implantação (Onboarding) e Minha
/// Assinatura só eram alcançáveis por atalho; entram na seção de gestão quando o módulo core
/// (ONBOARDING/ASSINATURAS, TenantAdministrationModules) concede acesso, nunca para o admin global
/// (que já tem "Assinaturas" na seção de plataforma);
/// (C) o título da página de implantação era "Onboarding SaaS" em 4 culturas — jargão para usuário;
/// agora "Implantação" (pt) e equivalentes nas demais culturas. Plano:
/// docs/evidencias/2026-10-18-rodada-43/r43a0-e0-navegacao-menus-plano.md.
/// </summary>
public class R43NavigacaoMenusContratoTests
{
    private static string ReadRelative(params string[] partes) => File.ReadAllText(
        Path.Combine(new[] { RepositoryPathResolver.RepoRoot }.Concat(partes).ToArray()), Encoding.UTF8);

    [Fact]
    public void MenuSemLiteralInglis_ComandoCentralizadoNoI18n()
    {
        var sidebar = ReadRelative("backend", "PlantaoPro.Web", "Views", "Shared", "_AppSidebar.cshtml");
        Assert.DoesNotContain("<span>Command Center</span>", sidebar, StringComparison.Ordinal);
        Assert.Contains("@UiText.L(\"Nav.CommandCenter\")", sidebar, StringComparison.Ordinal);

        var cc = ReadRelative("backend", "PlantaoPro.Web", "Views", "CommandCenter", "Index.cshtml");
        Assert.DoesNotContain("<h1>Command Center</h1>", cc, StringComparison.Ordinal);
        Assert.Contains("@UiText.L(\"Nav.CommandCenter\")", cc, StringComparison.Ordinal);
    }

    [Fact]
    public void AreasDoAdminDaConta_TemItemDeMenuQuandoOTituloExiste()
    {
        var sidebar = ReadRelative("backend", "PlantaoPro.Web", "Views", "Shared", "_AppSidebar.cshtml");
        // Implantação: guarda de módulo core + nunca para admin global.
        Assert.Contains("Can(\"ONBOARDING\") && !isGlobalAdmin", sidebar, StringComparison.Ordinal);
        Assert.Contains("asp-controller=\"Onboarding\"", sidebar, StringComparison.Ordinal);
        Assert.Contains("@UiText.L(\"Onboarding.PageTitle\")", sidebar, StringComparison.Ordinal);
        // Minha Assinatura: idem.
        Assert.Contains("Can(\"ASSINATURAS\") && !isGlobalAdmin", sidebar, StringComparison.Ordinal);
        Assert.Contains("asp-controller=\"MinhaAssinatura\"", sidebar, StringComparison.Ordinal);
        Assert.Contains("@UiText.L(\"Nav.MinhaAssinatura\")", sidebar, StringComparison.Ordinal);
    }

    [Fact]
    public void ChavesNovasEsemJargaoExistemNasQuatroCulturas()
    {
        foreach (var f in new[] { "UiTexts.resx", "UiTexts.en.resx", "UiTexts.es.resx", "UiTexts.fr.resx" })
        {
            var resx = ReadRelative("backend", "PlantaoPro.Web", "Localization", f);
            Assert.Contains("<data name=\"Nav.CommandCenter\"", resx, StringComparison.Ordinal);
            Assert.Contains("<data name=\"Nav.MinhaAssinatura\"", resx, StringComparison.Ordinal);
            Assert.DoesNotContain("Onboarding SaaS", resx, StringComparison.Ordinal);
            Assert.DoesNotContain("SaaS onboarding", resx, StringComparison.Ordinal);
        }

        var pt = ReadRelative("backend", "PlantaoPro.Web", "Localization", "UiTexts.resx");
        Assert.Contains("<value>Implantação</value>", pt, StringComparison.Ordinal);
        Assert.Contains("<value>Central de operações</value>", pt, StringComparison.Ordinal);
    }
}
