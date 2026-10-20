using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R73 (plano r66a0, epic B2): linguagem simples v2 — varredura de jargão em TODOS os valores dos
/// UiTexts.*.resx (4 culturas) e no texto visível das views, com substituição por linguagem de usuário
/// e lista KEEP documentada por decisão de público:
///  - Adm360.RelFin.Ind* = definições de indicadores fiéis ao SQL (KEEP herdado da R34/R8-B27);
///  - Nav.LogsESaude = link da seção MNSOFT administracao para ADMINISTRADOR_GLOBAL (Observabilidade);
///  - Modulo Integracoes/B2B = publico de consumidores de API (webhook, API Key);
///  - Paginas comerciais = nome de recurso do produto (API Enterprise, API Mobile).
/// Fato 1 = anti-regressao por vocabulario sobre valores de resx fora da KEEP; fato 2 = frases-jargao
/// proibidas em qualquer view (texto bruto, frase escolhida por nao bater em nome de codigo);
/// fato 3 = strings exatas da reescrita (views e resx pt-BR) travadas para ninguem reverter.
/// Plano: docs/evidencias/2026-10-20-rodada-73/r73a0-varredura-jargao-v2-plano.md.
/// </summary>
public sealed class R73LinguagemSimplesV2ContratoTests
{
    private static string WebDir => System.IO.Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web");

    // Vocabulario da varredura v2 (mesma lista do r73_scan.ps1): palavras soltas (ci, \b) e
    // acronimos em caixa alta (sensivel a maiuscula, \b). Frases nao palavras, quando ha colisao
    // com nome de codigo — mesmo criterio da R50.
    private const string Palavras =
        "tenant|tenants|inquilino|inquilinos|outbox|payload|payloads|json|http|antiforgery|server|servers"
        + "|log|logs|fallback|cache|webhook|webhooks|hash|checksum|checksums|seed|seeds|bootstrap"
        + "|exceção|excecao|backend|frontend|migração|migracao|migration|schema|stack|blob|multipart"
        + "|cookie|cookies|debug|uuid|guid|api|bff|endpoint|endpoints";

    private const string Acronimos = "API|BFF|UUID|GUID|BLOB|CRUD|CSRF|JWT|OAuth|SQL|JSON|CHECKSUM|WEBHOOK|ANTIFORGERY";

    // KEEP documentado: chave que legítimamente mantém vocabulario tecnico pelo seu publico/uso.
    private static readonly string[] ChavesKeep =
    {
        "Nav.LogsESaude", // sidebar, seccao MNSOFT administracao (apenas ADMINISTRADOR_GLOBAL) -> Observabilidade
        "Adm360.RelFin.IndDreFiltros", // definicao fiel ao SQL do indicador (familia KEEP R34/R8-B27)
        "Adm360.RelFin.IndDreFonte", // idem
        "Adm360.RelFin.IndMargemFiltros", // idem
        "Adm360.RelFin.IndVencFiltros", // idem
        "Adm360.RelFin.IndVendasFiltros", // idem
        "Adm360.RelFin.IndVendasFonte", // idem
    };

    [Fact]
    public void ValoresResxQuatroCulturas_SemJargaoForaDaListaKeep()
    {
        var falhas = new List<string>();
        foreach (var nome in new[] { "UiTexts.resx", "UiTexts.en.resx", "UiTexts.es.resx", "UiTexts.fr.resx" })
        {
            var caminho = System.IO.Path.Combine(WebDir, "Localization", nome);
            var texto = System.IO.File.ReadAllText(caminho);
            var datas = Regex.Matches(texto, "<data name=\"([^\"]+)\"[^>]*>\\s*<value>(.*?)</value>", RegexOptions.Singleline);
            foreach (Match d in datas)
            {
                var chave = d.Groups[1].Value;
                if (ChavesKeep.Contains(chave)) { continue; }
                var valor = d.Groups[2].Value;
                if (string.IsNullOrWhiteSpace(valor)) { continue; }
                var achados = Regex.Matches(valor, "\\b(" + Palavras + ")\\b", RegexOptions.IgnoreCase)
                    .Concat(Regex.Matches(valor, "\\b(" + Acronimos + ")\\b"))
                    .Select(m => m.Value.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase);
                foreach (var h in achados)
                {
                    var snip = Regex.Replace(valor.Trim(), @"\s+", " ");
                    if (snip.Length > 140) { snip = snip.Substring(0, 140) + "..."; }
                    falhas.Add($"{nome} :: {chave} :: {h} => {snip}");
                }
            }
        }
        Assert.True(falhas.Count == 0, "Jargo em valor de resx fora da KEEP:\n" + string.Join("\n", falhas));
    }

    [Fact]
    public void Views_SemFrasesDeJargaoEmTextoVisivel()
    {
        // Frases (nao palavras soltas) para nao bater em nome de codigo: bootstrap.Modal, data-endpoint,
        // JSON.parse, enctype multipart/form-data e comentarios @* *@ / <!-- --> permanecem como codigo.
        var frases = new[]
        {
            "banco de dados", "base de dados", "payload clínico", "consultar a api",
            "disponíveis na api", "via api", "endpoint transacional", "a api não", "a api ainda"
        };
        var falhas = new List<string>();
        var viewsDir = System.IO.Path.Combine(WebDir, "Views");
        foreach (var arq in System.IO.Directory.GetFiles(viewsDir, "*.cshtml", SearchOption.AllDirectories))
        {
            var relativo = arq.Substring(viewsDir.Length + 1).Replace('\\', '/');
            var linhas = System.IO.File.ReadAllLines(arq);
            for (var i = 0; i < linhas.Length; i++)
            {
                var ln = linhas[i];
                if (string.IsNullOrWhiteSpace(ln)) { continue; }
                var minuscula = ln.ToLowerInvariant();
                foreach (var f in frases)
                {
                    if (minuscula.Contains(f, StringComparison.Ordinal))
                    {
                        falhas.Add($"{relativo}:L{i + 1} :: {f}");
                    }
                }
            }
        }
        Assert.True(falhas.Count == 0, "Frase de jargao em view:\n" + string.Join("\n", falhas));
    }

    [Fact]
    public void ReescritaR73_StringsExatasExistem_NasViewsENosResxPtBr()
    {
        // Strings exatas da varredura v2 — trava a redacao escolhida (linguagem de usuario) contra reversao.
        VerificaView("Views/Financeiro/Index.cshtml", "Não encontramos lançamentos para os filtros atuais.");
        VerificaView("Views/Financeiro/Index.cshtml", "a confirmação exige status pendente e valor informado.");
        VerificaView("Views/Administrativo360/FechamentoCaixa.cshtml", "são bloqueados pelo sistema.");
        VerificaView("Views/Administrativo360/Fiscal/Configurar.cshtml", "nunca nos registros do sistema nem nas telas.");
        VerificaView("Views/MinhaAssinatura/Index.cshtml", "Não encontramos um vínculo contratual para esta organização.");
        VerificaView("Views/MinhaAssinatura/Index.cshtml", "Limites e uso ainda não foram informados para este contrato.");
        VerificaView("Views/Comercial/Funil.cshtml", ">Dados por etapa</p>");
        VerificaView("Views/Auditoria/Index.cshtml", "A busca respeita o seu perfil de acesso e os dados disponíveis.");
        VerificaView("Views/Seguranca/Index.cshtml", "Dados carregados do sistema");
        VerificaView("Views/V112/OperationalPage.cshtml", "para carregar os dados.");
        VerificaView("Views/Integracoes/Logs.cshtml", "ou dados clínicos sensíveis.");
        VerificaView("Views/Lgpd/Consentimentos.cshtml", "fica registrado no sistema e pode ser consultado pela área técnica.");

        // resx pt-BR: valor exato das chaves reescritas.
        var resx = System.IO.File.ReadAllText(System.IO.Path.Combine(WebDir, "Localization", "UiTexts.resx"));
        var esperados = new Dictionary<string, string>
        {
            ["Shift.TipFilter"] = "O filtro é aplicado a todos os registros, não apenas à página atual.",
            ["Clinical.PhTipSensitive"] = "Dados sensíveis não aparecem nos registros do sistema.",
            ["Clinical.FormHiddenHint"] = "Formulário Saúde 360 com validação, proteção contra envio duplicado e preservação dos dados em caso de erro.",
            ["Implantacao.Loading"] = "Carregando informações reais do sistema...",
            ["Adm360.Dash.CapDashboardDesc"] = "Métricas e estatísticas apuradas com os dados reais do sistema.",
        };
        foreach (var par in esperados)
        {
            var m = Regex.Match(resx, "<data name=\"" + Regex.Escape(par.Key) + "\"[^>]*>\\s*<value>(.*?)</value>", RegexOptions.Singleline);
            Assert.True(m.Success, $"chave ausente: {par.Key}");
            Assert.Equal(par.Value, m.Groups[1].Value);
        }
    }

    private static void VerificaView(string relativo, string trecho)
    {
        var caminho = System.IO.Path.Combine(WebDir, relativo.Replace('/', System.IO.Path.DirectorySeparatorChar));
        var texto = System.IO.File.ReadAllText(caminho);
        Assert.Contains(trecho, texto, StringComparison.Ordinal);
    }
}
