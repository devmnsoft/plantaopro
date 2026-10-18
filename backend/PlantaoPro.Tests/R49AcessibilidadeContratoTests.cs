using System.Globalization;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R49 (roadmap r24a1): acessibilidade básica — aria/labels/foco + contraste revisitado.
/// Scanner de fonte permanente (padrão de casa) que trava o resultado da passada R49:
/// todo campo de formulário tem nome acessível, todo botão só-ícone tem aria-label/title,
/// os layouts carregam a linha canônica com azul AA por último, os pares críticos de tinta
/// medem >= 4,5:1 (calculado a partir dos hex reais dos tokens) e a fundação de foco/movimento
/// do accessibility.css permanece. Plano:
/// docs/evidencias/2026-10-18-rodada-49/r49a0-acessibilidade-basica-plano.md.
/// </summary>
public sealed class R49AcessibilidadeContratoTests
{
    private static string ViewsDir => System.IO.Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Views");
    private static string CssDir => System.IO.Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "wwwroot", "css");

    private static string Ler(string relativo) => System.IO.File.ReadAllText(System.IO.Path.Combine(RepositoryPathResolver.RepoRoot, relativo), System.Text.Encoding.UTF8);

    private static IReadOnlyList<string> TodosCshtml()
    {
        var lista = new List<string>();
        foreach (var arquivo in System.IO.Directory.EnumerateFiles(ViewsDir, "*.cshtml", System.IO.SearchOption.AllDirectories)) lista.Add(arquivo);
        return lista;
    }

    // Retorna o texto da tag a partir da posição do '<' inicial (lida em múltiplas linhas).
    private static string TextoDaTag(string texto, int inicio)
    {
        var profundidade = 0;
        for (var i = inicio; i < texto.Length; i++)
        {
            if (texto[i] == '<') profundidade++;
            else if (texto[i] == '>') { profundidade--; if (profundidade == 0) return texto.Substring(inicio, i - inicio + 1); }
        }
        return texto.Substring(inicio);
    }

    // O controle está dentro de um <label ...> imediatamente anterior (associação implícita válida).
    private static bool DentroDeLabel(string texto, int indice)
    {
        var w0 = Math.Max(0, indice - 90);
        var janela = texto.Substring(w0, indice - w0);
        var lt = janela.LastIndexOf('<');
        return lt >= 0 && janela.Substring(lt).StartsWith("<label", StringComparison.Ordinal);
    }

    [Fact]
    public void Formularios_TodoCampoTemNomeAcessivel()
    {
        var violacoes = new List<string>();
        foreach (var arquivo in TodosCshtml())
        {
            var t = System.IO.File.ReadAllText(arquivo, System.Text.Encoding.UTF8);
            var relativo = arquivo.Replace(ViewsDir, "").Replace('\\', '/');
            foreach (var tipo in new[] { "input", "select", "textarea" })
            {
                var idx = 0;
                while ((idx = t.IndexOf("<" + tipo, idx, StringComparison.Ordinal)) >= 0)
                {
                    var resto = t.Substring(idx + tipo.Length + 1);
                    if (!resto.StartsWith(' ') && !resto.StartsWith('>') && !resto.StartsWith('/')) { idx++; continue; }
                    var tag = TextoDaTag(t, idx);
                    idx += tag.Length;
                    var menor = tag.ToLowerInvariant();
                    if (tipo == "input" && (menor.Contains("type=\"hidden\"") || menor.Contains("type='hidden'"))) continue;
                    var temNome = menor.Contains("asp-for") || menor.Contains("id=") || menor.Contains("aria-label")
                        || menor.Contains("aria-labelledby") || menor.Contains("placeholder=") || menor.Contains("title=");
                    if (!temNome && !DentroDeLabel(t, idx - tag.Length))
                    {
                        var curta = tag.Trim();
                        if (curta.Length > 110) curta = curta.Substring(0, 110);
                        violacoes.Add($"{relativo}: <{tipo}> sem nome acessível: {curta}");
                    }
                }
            }
        }
        Assert.True(violacoes.Count == 0, "Campos sem nome acessível (id/asp-for/aria-label/aria-labelledby/placeholder/title ou <label> envolvente):\n" + string.Join("\n", violacoes.Take(10)));
    }

    [Fact]
    public void BotoesSoIcone_TemAriaLabelOuTitle()
    {
        var violacoes = new List<string>();
        foreach (var arquivo in TodosCshtml())
        {
            var t = System.IO.File.ReadAllText(arquivo, System.Text.Encoding.UTF8);
            var relativo = arquivo.Replace(ViewsDir, "").Replace('\\', '/');
            var idx = 0;
            while ((idx = t.IndexOf("<button", idx, StringComparison.Ordinal)) >= 0)
            {
                var abertura = TextoDaTag(t, idx);
                var fim = t.IndexOf("</button>", idx, StringComparison.Ordinal);
                if (fim < 0) fim = t.Length - 1;
                var interno = t.Substring(idx + abertura.Length, Math.Max(0, fim - idx - abertura.Length));
                idx += 7;
                if (abertura.ToLowerInvariant().Contains("aria-label") || abertura.ToLowerInvariant().Contains("title=")) continue;
                var visivel = System.Text.RegularExpressions.Regex.Replace(interno, "<[^>]+>", "").Trim();
                var ehIcone = visivel.Length == 0 && (interno.Contains("<i ") || interno.Contains("<i>") || interno.Contains("<svg"))
                    && !interno.Contains("@UiText") && !interno.Contains("Model.");
                if (ehIcone)
                {
                    var curta = abertura.Trim();
                    if (curta.Length > 110) curta = curta.Substring(0, 110);
                    violacoes.Add($"{relativo}: <button> só-ícone sem aria-label/title: {curta}");
                }
            }
        }
        Assert.True(violacoes.Count == 0, "Botões só-ícone sem nome acessível:\n" + string.Join("\n", violacoes.Take(10)));
    }

    [Fact]
    public void Layouts_TemLangESkipLinkEMainEEAzulAAPorUltimo()
    {
        foreach (var layout in new[] { "backend/PlantaoPro.Web/Views/Shared/_Layout.cshtml", "backend/PlantaoPro.Web/Views/Shared/_AuthLayout.cshtml" })
        {
            var t = Ler(layout.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(t.Contains("<html") && t.Contains("lang="), layout + ": <html> sem lang");
            Assert.Contains("class=\"skip-link\"", t);
            Assert.Contains("id=\"mainContent\"", t);
            // Azul AA (v2151) precisa vencer a cascata: carregado depois do último bloco global anterior (v2148).
            var iV2151 = t.IndexOf("v2151-wp-c-homologacao.css", StringComparison.Ordinal);
            var iV2148 = t.IndexOf("v2148-operational-premium.css", StringComparison.Ordinal);
            Assert.True(iV2151 > 0 && iV2148 > 0 && iV2151 > iV2148, layout + ": v2151 (azul AA) ausente ou não carrega por último");
            // R49: a fundação de acessibilidade (skip-link css, :focus-visible, reduced-motion) precisa carregar em ambos os layouts.
            var iAcc = t.IndexOf("accessibility.css", StringComparison.Ordinal);
            Assert.True(iAcc > 0 && iAcc > iV2148, layout + ": accessibility.css ausente ou não carrega após o bloco v2148");
        }
        var tLayout = Ler("backend/PlantaoPro.Web/Views/Shared/_Layout.cshtml".Replace('/', Path.DirectorySeparatorChar));
        var iV2150 = tLayout.IndexOf("v2150-bloco-c-canonical.css", StringComparison.Ordinal);
        Assert.True(iV2150 > 0 && iV2150 < tLayout.IndexOf("v2151-wp-c-homologacao.css", StringComparison.Ordinal), "_Layout: v2150 deve carregar antes do v2151");
    }

    [Fact]
    public void Contraste_ParesCriticosMedemQuatroCincoOuMais()
    {
        var tokens = Ler("backend/PlantaoPro.Web/wwwroot/css/design-system/tokens.css".Replace('/', Path.DirectorySeparatorChar));
        var homolog = Ler("backend/PlantaoPro.Web/wwwroot/css/design-system/v2151-wp-c-homologacao.css".Replace('/', Path.DirectorySeparatorChar));

        string Token(string css, string nome)
        {
            var m = System.Text.RegularExpressions.Regex.Match(css, @"--pp-" + System.Text.RegularExpressions.Regex.Escape(nome) + @"\s*:\s*#([0-9a-fA-F]{6})");
            Assert.True(m.Success, "token --pp-" + nome + " não encontrado com hex");
            return "#" + m.Groups[1].Value;
        }

        var branc = "#ffffff";
        var pares = new Dictionary<string, string>
        {
            ["text-primary"] = Token(tokens, "text-primary"),
            ["text-secondary"] = Token(tokens, "text-secondary"),
            ["text-muted"] = Token(tokens, "text-muted"),
            ["text-soft"] = Token(tokens, "text-soft"),
            ["success"] = Token(tokens, "success"),
            ["warning"] = Token(tokens, "warning"),
            ["danger"] = Token(tokens, "danger"),
            ["info"] = Token(tokens, "info"),
            // Primário efetivo: o v2151 reescreve o token canônico para o azul AA (vence a cascata por ordem de carga).
            ["brand-primary(v2151)"] = Token(homolog, "brand-primary"),
        };
        foreach (var p in pares)
        {
            var razao = Proporcao(p.Value, branc);
            Assert.True(razao >= 4.5, $"par {p.Key} {p.Value} sobre branco mede {razao.ToString("0.00", CultureInfo.InvariantCulture)}:1 (mínimo 4,5:1)");
        }
    }

    private static double Proporcao(string fgHex, string bgHex)
    {
        double Lum(string hex)
        {
            int c(int i) => Convert.ToInt32(hex.Substring(i, 2), 16);
            double Lin(double v) { v /= 255.0; return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4); }
            return 0.2126 * Lin(c(1)) + 0.7152 * Lin(c(3)) + 0.0722 * Lin(c(5));
        }
        var la = Lum(fgHex); var lb = Lum(bgHex);
        if (la < lb) { var tmp = la; la = lb; lb = tmp; }
        return (la + 0.05) / (lb + 0.05);
    }

    [Fact]
    public void Fundacao_FocoVisivelMovimentoReduzidoESkipLinkCss()
    {
        var fundacao = Ler("backend/PlantaoPro.Web/wwwroot/css/design-system/accessibility.css".Replace('/', Path.DirectorySeparatorChar));
        Assert.Contains(":focus-visible", fundacao);
        Assert.Contains("outline", fundacao);
        Assert.Contains("prefers-reduced-motion:reduce", fundacao);
        Assert.Contains(".skip-link", fundacao);
    }
}
