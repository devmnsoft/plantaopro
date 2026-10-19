using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R60 (fila r53b1 §7 item 6; pendência da diretriz E6 L116): inventário completo de
/// GUIDs/fragmentos remanescentes em UI. Recon desta rodada (plano r60a0): 3 fragmentos de
/// GUID renderizados (AssistenteIa, Notas fiscais e FechamentoId em MeusPagamentos — corrigidos
/// para o identificador inteiro), 1 digest SHA256 sem inteiro em lugar nenhum (ganhou title),
/// chaves longas com abreviação exigindo o inteiro em title na linha, 1 placeholder com o
/// tenant demo como exemplo (Cotacoes/Detalhes — padrão neutro de zeros) e 4 campos/filtros
/// operacionais etiquetados em whitelist travada. Scanner de fonte (padrão de casa).
/// Plano: docs/evidencias/2026-10-19-rodada-60/r60a0-guids-fragmentos-plano.md.
/// </summary>
public sealed class R60GuidsEFragmentosContratoTests
{
    private static string WebRoot => System.IO.Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web");

    private static string Ler(params string[] partes) =>
        System.IO.File.ReadAllText(System.IO.Path.Combine(partes.Prepend(WebRoot).ToArray()), Encoding.UTF8);

    [Fact]
    public void Views_SemFragmentosTruncadosDeIdentificador()
    {
        // D-R60-1: fragmento de GUID não é conteúdo — o valor inteiro aparece (Notas fiscais,
        // custos do Assistente IA, fechamento em MeusPagamentos, corrigidos nesta rodada).
        // Regra A: nenhum Substring(0, 1..19) em Views.
        // Regra B: fatia [..1..19] só é permitida quando a mesma linha expõe o valor inteiro
        // em title="..." (padrão já usado em Chave de Acesso NFe e SHA256 de exportação) ou na
        // linha de máscara de documento (formato CPF em Colaboradores.cshtml — não é identificador).
        var views = System.IO.Path.Combine(WebRoot, "Views");
        var violacoes = new List<string>();
        foreach (var arquivo in Directory.EnumerateFiles(views, "*.cshtml", SearchOption.AllDirectories))
        {
            var texto = System.IO.File.ReadAllText(arquivo, Encoding.UTF8);
            var linhas = texto.Split('\n');

            foreach (Match m in Regex.Matches(texto, @"Substring\(0,\s*(?:[1-9]|1[0-9])\)"))
            {
                violacoes.Add(Path.GetFileName(arquivo) + " | Substring: " + m.Value);
            }

            foreach (Match m in Regex.Matches(texto, @"\[\.\.\s*(?:[1-9]|1[0-9])\]"))
            {
                var linha = "";
                var pos = m.Index;
                for (var i = 0; i < linhas.Length; i++)
                {
                    if (pos < linhas[i].Length) { linha = linhas[i]; break; }
                    pos -= linhas[i].Length + 1;
                }
                if (linha.Contains("title=\"") || linha.Contains("d[..3]")) continue;
                violacoes.Add(Path.GetFileName(arquivo) + " | [" + m.Value + "] sem valor inteiro na linha");
            }
        }

        Assert.True(violacoes.Count == 0, "fragmentos truncados de identificador em Views:\n" + string.Join("\n", violacoes.Take(15)));
    }

    [Fact]
    public void Views_SemLiteralGuidComDadosDeExemplo()
    {
        // D-R60-2: todo literal em formato GUID dentro de um view deve ser o placeholder
        // neutro de zeros (formato sem dado real). O tenant demo só pode existir nos seeds.
        const string neutro = "00000000-0000-0000-0000-000000000000";
        var views = System.IO.Path.Combine(WebRoot, "Views");
        var violacoes = new List<string>();
        foreach (var arquivo in Directory.EnumerateFiles(views, "*.cshtml", SearchOption.AllDirectories))
        {
            var texto = System.IO.File.ReadAllText(arquivo, Encoding.UTF8);
            foreach (Match m in Regex.Matches(texto, @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}"))
            {
                if (!string.Equals(m.Value, neutro, StringComparison.OrdinalIgnoreCase))
                {
                    violacoes.Add(Path.GetFileName(arquivo) + " | " + m.Value);
                }
            }
        }

        Assert.True(violacoes.Count == 0, "literais GUID com dados reais em Views:\n" + string.Join("\n", violacoes.Take(15)));
    }

    [Fact]
    public void CamposOperacionaisDeIdentificador_RotulosTravados()
    {
        // D-R60-3 (whitelist do inventário): os campos que pedem/exibem identificadores por
        // necessidade operacional ficam travados COM seu rótulo — rótulo sumiu ou placeholder
        // com dado real voltou, contrato falha.
        var fiscalNova = Ler("Views", "Administrativo360", "Fiscal", "Nova.cshtml");
        Assert.Contains("Código do documento de origem", fiscalNova);
        Assert.Contains("name=\"OrigemId\"", fiscalNova);
        Assert.Contains("placeholder=\"00000000-0000-0000-0000-000000000000\"", fiscalNova);

        var cotDet = Ler("Views", "Administrativo360", "Cotacoes", "Detalhes.cshtml");
        Assert.Contains("@UiText.L(\"Adm360.CotDet.LblIdentificadorManual\")", cotDet);
        Assert.Contains("placeholder=\"00000000-0000-0000-0000-000000000000\"", cotDet);

        Assert.Contains("Unidade (identificador)", Ler("Views", "CentralEscala", "Substituicoes.cshtml"));

        var conferencia = Ler("Views", "ConferenciaExecucao", "Index.cshtml");
        Assert.Contains("Unidade (ID)", conferencia);
        Assert.Contains("Profissional (ID)", conferencia);
    }
}
