using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R91 (bloco B5 — escala/performance/confiabilidade): contrato de rotas das páginas web.
/// A varredura viva (apps Release contra plantaopro_test) capturou o defeito real do bloco B4:
/// os oito controllers *Web declaram [HttpGet("")]/[HttpGet("Index")] SEM rota de controller —
/// os templates viram rotas ABSOLUTAS ("/" e "/index"), colidindo entre si desde a R82
/// (GET / e GET /index em 500 AmbiguousMatch) e 404 nas URLs do menu lateral e dos fetch JS;
/// seis deles também caíam em CATALOGO_NAO_CONFIGURADO no guard SaaS (R88/R89 já registravam).
/// Corrigido na R91 com [Route("{Controller}")] (padrão CentralEscala/Billing/Adm360*Web) +
/// entradas no catálogo do guard. Trava por scanner de fonte (comentários removidos; arquivos
/// com vários controllers, como V116WebControllers, processados por classe):
/// (1) unicidade do template absoluto de rota de atributo por verbo em todo o Web;
/// (2) todo controller de página autenticada consta do catálogo do guard, do conjunto público
/// ou da allow-list legacy documentada no fato correspondente.
/// </summary>
public sealed class R91ConfiabilidadeRotasWebContratoTests
{
    private static string P(params string[] partes)
    {
        var caminho = RepositoryPathResolver.RepoRoot;
        foreach (var parte in partes) caminho = Path.Combine(caminho, parte);
        return caminho;
    }

    private static readonly string DirControllers = P("backend", "PlantaoPro.Web", "Controllers");
    private static readonly Regex DeclaracaoClasse = new(@"^\s*(?:public\s+|internal\s+)?(?:abstract\s+|sealed\s+|static\s+|partial\s+)*class\s+(\w+)\s*:");
    private static readonly Regex RotaAcao = new(@"\[Http(Get|Post|Put|Delete|Patch)\(\s*""([^""]*)""\s*\)\]", RegexOptions.Compiled);
    private static readonly Regex RotaClasse = new(@"\bRoute\(\s*""([^""]*)""\s*\)", RegexOptions.Compiled);

    // Remove comentários // linha-a-linha respeitando literais de string (uma linha de comentário
    // citando um atributo de rota, como a do OnboardingController, não pode contar como rota).
    private static string SemComentarios(string texto)
    {
        var sb = new StringBuilder(texto.Length);
        foreach (var linhaBruta in texto.Split('\n'))
        {
            var linha = linhaBruta.TrimEnd('\r');
            var emStringa = false;
            var corte = -1;
            for (var i = 0; i < linha.Length - 1; i++)
            {
                var c = linha[i];
                if (emStringa)
                {
                    if (c == '\\') i++;
                    else if (c == '"') emStringa = false;
                }
                else if (c == '"') emStringa = true;
                else if (c == '/' && linha[i + 1] == '/') { corte = i; break; }
            }
            sb.AppendLine(corte >= 0 ? linha[..corte] : linha);
        }
        return sb.ToString();
    }

    private record InfoClasse(string Nome, int IndiceLinha, bool Abstract, string? PrefixoRota, bool AnonOuApi);

    // Varre o arquivo (já sem comentários) e lista cada classe declarada com o prefixo de rota
    // vindo das linhas de atributo imediatamente acima (inclusive compostas: [X,Route("...")]).
    private static List<InfoClasse> ClassesDoArquivo(string[] linhas)
    {
        var resultado = new List<InfoClasse>();
        var atributos = new List<string>();
        for (var i = 0; i < linhas.Length; i++)
        {
            var t = linhas[i].Trim();
            var m = DeclaracaoClasse.Match(t);
            if (m.Success)
            {
                string? prefixo = null;
                var anonOuApi = t.Contains("[AllowAnonymous]") || t.Contains("[ApiController]");
                for (var j = atributos.Count - 1; j >= 0; j--)
                {
                    anonOuApi |= atributos[j].Contains("[AllowAnonymous]") || atributos[j].Contains("[ApiController]");
                    var mr = RotaClasse.Match(atributos[j]);
                    if (mr.Success)
                    {
                        var nomeCurto = m.Groups[1].Value.Replace("Controller", "");
                        prefixo = mr.Groups[1].Value.Replace("[controller]", nomeCurto);
                        break;
                    }
                }
                resultado.Add(new InfoClasse(m.Groups[1].Value, i, t.Contains("abstract"), prefixo, anonOuApi));
                atributos.Clear();
                continue;
            }
            if (t.StartsWith('[')) atributos.Add(t);
        }
        return resultado;
    }

    private static string Normalizar(string absoluto)
    {
        var t = absoluto.Trim().TrimEnd('/').ToLowerInvariant();
        return t.Length == 0 ? "/" : "/" + t;
    }

    [Fact]
    public void RotasAbsolutas_SaoUnicasPorVerboEmTodoOWeb()
    {
        // Dois endpoints com o mesmo verbo + template absoluto produzem AmbiguousMatch (500)
        // no request que os casa — foi exatamente o que quebrou "/" e "/index" na R82.
        var raiz = RepositoryPathResolver.RepoRoot;
        var vistos = new Dictionary<string, List<string>>();
        foreach (var arquivo in Directory.GetFiles(DirControllers, "*.cs"))
        {
            var linhas = SemComentarios(File.ReadAllText(arquivo, Encoding.UTF8)).Split('\n');
            var classes = ClassesDoArquivo(linhas);
            if (classes.Count == 0) continue;
            var relativo = arquivo[(raiz.Length + 1)..];
            for (var i = 0; i < linhas.Length; i++)
            {
                var t = linhas[i];
                var classe = classes.LastOrDefault(c => c.IndiceLinha <= i);
                if (classe is null) continue;
                foreach (Match m in RotaAcao.Matches(t))
                {
                    var verbo = m.Groups[1].Value.ToLowerInvariant();
                    var tpl = m.Groups[2].Value;
                    string absoluto;
                    if (tpl.StartsWith('/')) absoluto = tpl;                                  // absoluto explicito
                    else if (classe.PrefixoRota != null) absoluto = tpl.Length == 0 ? classe.PrefixoRota : classe.PrefixoRota.TrimEnd('/') + "/" + tpl;
                    else absoluto = tpl;                                                      // sem prefixo => absoluto raiz
                    var chave = verbo + " " + Normalizar(absoluto);
                    if (!vistos.TryGetValue(chave, out var l)) { l = new List<string>(); vistos[chave] = l; }
                    l.Add($"{relativo}::{classe.Nome} {m.Groups[1].Value}(\"{tpl}\")");
                }
            }
        }
        Assert.True(vistos.Count > 50, $"Scanner de rotas sem alcance: apenas {vistos.Count} templates capturados");
        var colisoes = vistos.Where(kv => kv.Value.Count > 1).Select(kv => kv.Key + " => " + string.Join(" | ", kv.Value));
        Assert.True(!colisoes.Any(), "Templates absolutos colidindo (AmbiguousMatch/500): " + string.Join(" || ", colisoes));
    }

    [Fact]
    public void PaginasAutenticadas_ConstamDoCatalogoDoGuard()
    {
        // Controller fora do catalogo cai em CATALOGO_NAO_CONFIGURADO no guard (redirect para
        // AccessDenied em toda acao, GET e POST) quando nao ha [AllowAnonymous] — mesmo sem
        // [Authorize], o filtro aplica o catalogo as paginas renderizadas.
        var guard = File.ReadAllText(P("backend", "PlantaoPro.Web", "Services", "Security", "SaasRouteGuardFilter.cs"), Encoding.UTF8);
        var catalogo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inicioCat = guard.IndexOf("ControllerModules =", StringComparison.Ordinal);
        var fimCat = guard.IndexOf("};", inicioCat, StringComparison.Ordinal);
        foreach (Match m in Regex.Matches(guard[inicioCat..fimCat], @"""(\w+)""\]\s*=")) catalogo.Add(m.Groups[1].Value);
        var inicioPub = guard.IndexOf("PublicControllers", StringComparison.Ordinal);
        var fimPub = guard.IndexOf("};", inicioPub, StringComparison.Ordinal);
        var publicos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in Regex.Matches(guard[inicioPub..fimPub], @"""(\w+)""")) publicos.Add(m.Groups[1].Value);

        // Allow-list legacy: paginas de fase antiga fora da navegacao ativa (a unica nav que as
        // referencia, _NavigationRail.cshtml, e marca-morta — zero referencias no Views). Famílias:
        // (a) orfas de fase 1 sem rota no menu; (b) ilha V116 (Views/V116); (c) ilha de launch B2B
        // (controllers B2BCommercialOps/B2BLaunch que se autoligam nas views B2BLaunch, sem entrada
        // no menu ativo); (d) auxiliar de convites publicos. Quando uma for reativada no menu,
        // registre-a no catalogo e remova daqui.
        var legacy = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // (a) orfas de fase 1
            "CentralAtendimento", "ConferenciaExecucao", "FaturamentoClinico", "Implantacao",
            "Integracoes", "Inteligencia", "Jornada", "Manual", "Ocorrencias",
            "Operacao", "OperacaoAssistida", "OperacaoInteligente", "Operacoes",
            "Piloto", "PrimeirosPassos", "Seguranca", "V112Web",
            // (b) ilha V116
            "ConvenioAutorizacoes", "GuiasConvenio", "LotesFaturamento", "Timelines",
            "NotificacoesOperacionais", "RelatoriosExecutivos",
            // (c) ilha de launch B2B
            "Executivo", "WhiteLabelTemplates", "Renovacoes", "Expansoes", "Developer",
            "Contratos", "Sla", "Beta", "Monitoramento", "GoToMarket", "OperacionalLaunch",
            "FeatureFlags", "DemoAdmin", "ItensFaturaveis", "TemplatesOperacionais",
            "Favoritos", "HistoricoAcoes", "FaturamentoRegras", "RepassesMedicos",
            "Glosas", "Recebimentos", "ConfiguracoesFinanceiras",
            // (d) convites publicos
            "ConvitePublicoWeb"
        };

        var fora = new List<string>();
        foreach (var arquivo in Directory.GetFiles(DirControllers, "*.cs"))
        {
            var linhas = SemComentarios(File.ReadAllText(arquivo, Encoding.UTF8)).Split('\n');
            var classes = ClassesDoArquivo(linhas);
            for (var k = 0; k < classes.Count; k++)
            {
                var c = classes[k];
                if (c.Abstract) continue;
                // O guard compara por descriptor.ControllerName (sem o sufixo "Controller").
                var nomeCatalogo = c.Nome.EndsWith("Controller") ? c.Nome[..^10] : c.Nome;
                var fim = k + 1 < classes.Count ? classes[k + 1].IndiceLinha : linhas.Length;
                var segmento = string.Join("\n", linhas[c.IndiceLinha..fim]);
                if (!segmento.Contains("View(")) continue;
                if (c.AnonOuApi || segmento.Contains("[AllowAnonymous]") || segmento.Contains("[ApiController]")) continue;
                if (catalogo.Contains(nomeCatalogo) || publicos.Contains(nomeCatalogo) || legacy.Contains(nomeCatalogo)) continue;
                fora.Add(nomeCatalogo);
            }
        }
        Assert.True(fora.Count == 0, "Controllers de pagina autenticada fora do catalogo do guard: " + string.Join(", ", fora));

        // Trava da correcao R91: os oito controllers do bloco B4 carregam o prefixo explicito
        // e constam do catalogo (sem o prefixo, o fato anterior só passa se outro controller
        // não ocupar a mesma rota absoluta — aqui o par é afirmado direto).
        foreach (var c in new[] { "FolhaWeb", "SimplesWeb", "CalendarioFiscalWeb", "DfeNacionalWeb", "TrilhosPagamentoWeb", "EsocialTrabalhoWeb", "VendasInternacionaisWeb", "LegislacaoWeb" })
        {
            var ctl = File.ReadAllText(Path.Combine(DirControllers, c + "Controller.cs"), Encoding.UTF8);
            Assert.Contains($"[Route(\"{c}\")]", ctl);
            Assert.True(catalogo.Contains(c), $"Controller {c} ausente do catalogo do guard");
        }
    }
}
