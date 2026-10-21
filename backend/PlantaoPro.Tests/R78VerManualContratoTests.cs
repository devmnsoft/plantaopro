using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R78 - Botao "Ver manual" padronizado presente em todo header (bloco B3). Scanner de
/// fonte que trava: (1) o header autenticado (_AppTopbar) exibe botao rotulado apontando
/// para a central de ajuda; (2) os ramos publicos/deslogados do _Layout e o _AuthLayout
/// renderizam o atalho padronizado via _PublicHelpLink; e (3) o rotulo existe localizado
/// nas 4 culturas (pt-BR, en, es, fr). Cor nunca e o unico canal: o atalho tem rotulo de
/// texto visivel alem do icone.
/// Plano: docs/evidencias/2026-10-19-rodada-66/r66a0-plano-50-sprints.md (R78).
/// </summary>
public sealed class R78VerManualContratoTests
{
    private static string Root => RepositoryPathResolver.ResolveRepositoryRoot();

    private static string Read(string relativo) => File.ReadAllText(Path.Combine(Root, relativo.Replace('/', Path.DirectorySeparatorChar)));

    private static int CountOccurrences(string texto, string alvo)
    {
        var qtd = 0;
        var i = 0;
        while ((i = texto.IndexOf(alvo, i, StringComparison.Ordinal)) >= 0)
        {
            qtd++;
            i += alvo.Length;
        }
        return qtd;
    }

    [Fact]
    public void Topbar_autenticada_exibe_botao_rotulado_para_a_central_de_ajuda()
    {
        var topbar = Read("backend/PlantaoPro.Web/Views/Shared/_AppTopbar.cshtml");
        Assert.Contains("asp-controller=\"Ajuda\"", topbar);
        // O mesmo elemento carrega a chave duas vezes (title + rotulo visivel): se cair
        // para 1, o botao virou icone sem texto e cor/icone passaram a ser o unico canal.
        Assert.True(CountOccurrences(topbar, "Nav.TopbarManual") >= 2, "botao Ver manual sem rotulo visivel na topbar");
    }

    [Fact]
    public void Ramos_deslogados_renderizam_o_atalho_padronizado()
    {
        var layout = Read("backend/PlantaoPro.Web/Views/Shared/_Layout.cshtml");
        Assert.Contains("<partial name=\"_PublicHelpLink\" />", layout);

        var auth = Read("backend/PlantaoPro.Web/Views/Shared/_AuthLayout.cshtml");
        Assert.Contains("<partial name=\"_PublicHelpLink\" />", auth);

        var parcial = Read("backend/PlantaoPro.Web/Views/Shared/_PublicHelpLink.cshtml");
        Assert.Contains("asp-controller=\"Ajuda\"", parcial);
        Assert.Contains("Nav.TopbarManual", parcial);
    }

    [Fact]
    public void Rotulo_do_manual_esta_localizado_nas_4_culturas()
    {
        foreach (var arquivo in new[]
        {
            "backend/PlantaoPro.Web/Localization/UiTexts.resx",
            "backend/PlantaoPro.Web/Localization/UiTexts.en.resx",
            "backend/PlantaoPro.Web/Localization/UiTexts.es.resx",
            "backend/PlantaoPro.Web/Localization/UiTexts.fr.resx"
        })
        {
            var resx = Read(arquivo);
            var m = Regex.Match(resx, "<data name=\"Nav\\.TopbarManual\"[^>]*>\\s*<value>([^<]*)</value>");
            Assert.True(m.Success, "chave Nav.TopbarManual ausente em " + Path.GetFileName(arquivo));
            Assert.False(string.IsNullOrWhiteSpace(m.Groups[1].Value), "valor vazio em " + Path.GetFileName(arquivo));
        }
    }
}
