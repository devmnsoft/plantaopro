using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R59 (fila r53b1 §7 item 5 / D-R49-4): acessibilidade avançada — th scope em tabelas densas,
/// trap de foco em modais/overlays, landmarks completos, aria-live em toasts e teclado em
/// widgets custom. Recon desta rodada mostrou que a infraestrutura (BS 5.3.7, overlay-manager,
/// live regions, landmarks) já existia construída pelas rodadas premium; a correção material foi
/// a varredura de &lt;th scope=&gt; (D-R59-1, script versionado r59_scope_sweep.ps1) e este contrato
/// trava as cinco frentes contra regressão. Scanner de fonte (padrão de casa).
/// Plano: docs/evidencias/2026-10-19-rodada-59/r59a0-acessibilidade-avancada-plano.md.
/// </summary>
public sealed class R59AcessibilidadeAvancadaContratoTests
{
    private static string WebRoot => System.IO.Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web");

    private static string Ler(params string[] partes) => System.IO.File.ReadAllText(System.IO.Path.Combine(partes.Prepend(WebRoot).ToArray()), Encoding.UTF8);

    [Fact]
    public void Tabelas_TodosCabecalhosComScope()
    {
        // D-R59-1: zero <th sem scope em todo Views. O \b ignora <thead>/<tfoot>.
        var views = System.IO.Path.Combine(WebRoot, "Views");
        var total = 0;
        var violacoes = new List<string>();
        foreach (var arquivo in Directory.EnumerateFiles(views, "*.cshtml", SearchOption.AllDirectories))
        {
            var texto = System.IO.File.ReadAllText(arquivo, Encoding.UTF8);
            foreach (Match m in Regex.Matches(texto, @"<th\b[^>]*>"))
            {
                total++;
                if (!m.Value.Contains("scope="))
                    violacoes.Add(Path.GetFileName(arquivo) + " | " + m.Value.Substring(0, Math.Min(70, m.Value.Length)));
            }
        }
        Assert.True(total > 700, "inventário de cabeçalhos inesperadamente pequeno: " + total);
        Assert.True(violacoes.Count == 0, "cabeçalhos de tabela sem scope:\n" + string.Join("\n", violacoes.Take(15)));
    }

    [Fact]
    public void ModaisEOverlays_TrapDeFocoTravado()
    {
        // Modais bootstrap: o bundle v5.3.x do layout dá trap de foco, role=dialog, aria-modal
        // e devolução de foco automaticamente ao abrir/fechar.
        Assert.Contains("~/js/vendor/bootstrap.bundle.min.js", Ler("Views", "Shared", "_Layout.cshtml"));
        var bundle = Ler("wwwroot", "js", "vendor", "bootstrap.bundle.min.js").Substring(0, 200);
        Assert.Contains("Bootstrap v5.3", bundle);

        // Overlays custom ( drawers, paleta): stack própria com trap + Escape + foco de volta ao gatilho.
        var overlay = Ler("wwwroot", "js", "core", "overlay-manager.js");
        Assert.Contains("import { trapFocus } from './focus-manager.js';", overlay);
        Assert.Contains("if (event.key === 'Escape') this.close(current.element); else trapFocus(current.element, event);", overlay);
        Assert.Contains("entry?.trigger?.focus();", overlay);

        var focus = Ler("wwwroot", "js", "core", "focus-manager.js");
        Assert.Contains("export function trapFocus(", focus);
        Assert.Contains("event.shiftKey && document.activeElement === first", focus);
        Assert.Contains("export function focusFirstInvalid(", focus);
    }

    [Fact]
    public void Landmarks_CompletosNosLayouts()
    {
        var layout = Ler("Views", "Shared", "_Layout.cshtml");
        Assert.Contains("<a class=\"skip-link\" href=\"#mainContent\">", layout);
        Assert.Contains("<main class=\"pp-content app-content\" id=\"mainContent\" tabindex=\"-1\">", layout);
        Assert.Contains("<main class=\"auth-main\" id=\"mainContent\">", layout);

        var auth = Ler("Views", "Shared", "_AuthLayout.cshtml");
        Assert.Contains("<a class=\"skip-link\" href=\"#mainContent\">", auth);
        Assert.Contains("<main id=\"mainContent\"", auth);

        Assert.Contains("<aside class=\"pp-sidebar app-sidebar pp-calm-navigation\" id=\"appSidebar\" aria-label=", Ler("Views", "Shared", "_AppSidebar.cshtml"));
        var topbar = Ler("Views", "Shared", "_AppTopbar.cshtml");
        Assert.Contains("<header class=\"pp-topbar app-topbar\">", topbar);
        Assert.Contains("<nav class=\"pp-breadcrumb\" aria-label=", topbar);
        Assert.Contains("<footer class=\"pp-footer app-footer\">", Ler("Views", "Shared", "_AppFooter.cshtml"));
        Assert.Contains("<nav class=\"mobile-navigation d-lg-none\" aria-label=", Ler("Views", "Shared", "_MobileNavigation.cshtml"));
        Assert.Contains("<dialog class=\"pp-dialog\" id=\"commandPalette\" aria-labelledby=\"commandPaletteTitle\">", Ler("Views", "Shared", "_CommandPalette.cshtml"));
    }

    [Fact]
    public void Toasts_RegioesVivasERoles()
    {
        const string liveRegion = "<div class=\"pp-live-region\" id=\"appLiveRegion\" role=\"status\" aria-live=\"polite\" aria-atomic=\"true\"></div>";
        Assert.Contains(liveRegion, Ler("Views", "Shared", "_Layout.cshtml"));
        Assert.Contains(liveRegion, Ler("Views", "Shared", "_AuthLayout.cshtml"));

        Assert.Contains(
            "<section class=\"toast-region\" aria-label=\"Mensagens\" aria-live=\"polite\" aria-relevant=\"additions\" data-toast-region>",
            Ler("Views", "Shared", "_ToastRegion.cshtml"));

        // Toasts server-driven (TempData -> PlantaoProToast): itens com role status/alert dentro da região viva.
        var toastServer = Ler("wwwroot", "js", "plantaopro-toast.js");
        Assert.Contains("element.setAttribute(\"aria-live\", \"polite\")", toastServer);
        Assert.Contains("? \"alert\" : \"status\"", toastServer);
        Assert.Contains("close.setAttribute(\"aria-label\", \"Fechar notificação\")", toastServer);

        // Toasts de componente: itens focáveis dentro da mesma região [data-toast-region].
        var toastComp = Ler("wwwroot", "js", "components", "toast.js");
        Assert.Contains("document.querySelector('[data-toast-region]')", toastComp);
        Assert.Contains("item.tabIndex = 0", toastComp);
    }

    [Fact]
    public void WidgetsCustom_CaminhoDeTecladoTravado()
    {
        // Kanban: drag opcional + select nativo (teclado completo) por card e live node para anúncio.
        var kanban = Ler("wwwroot", "js", "components", "kanban-board.js");
        Assert.Contains("[data-move-select]", kanban);
        Assert.Contains("[data-kanban-live]", kanban);

        // Listas ordenáveis: API dedicada com botão + select para mover sem arrastar.
        Assert.Contains("enableAccessibleMove", Ler("wwwroot", "js", "components", "sortable-list.js"));
    }
}
