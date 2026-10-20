using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R75 - Inventario maquina paginas x artigos do manual (bloco B3). O inventario completo
/// versionado em docs/evidencias/&lt;data&gt;-rodada-75/r75_inventario.txt e o script de geracao
/// r75_gap.ps1 (mesma pasta) enumeram cada acao de controller que renderiza view (inclusive
/// parciais, heranca de bases abstratas e delegacoes via helpers) e cruzam com os artigos
/// declarados em AjudaController. Reexecutar o script apos criar/remover paginas ou artigos e
/// atualizar o arquivo; os fatos abaixo falham se isso nao acontecer. R79 trava o gap em zero.
/// </summary>
public sealed class R75InventarioManualContratoTests
{
    private static string Root => RepositoryPathResolver.ResolveRepositoryRoot();

    private const string InventarioRelativo = "docs/evidencias/2026-10-20-rodada-75/r75_inventario.txt";
    private const string AjudaRelativo = "backend/PlantaoPro.Web/Controllers/AjudaController.cs";

    // Gap medido na R75 (pontos de partida dos lotes de artigos das R76-R78). Crescer exige
    // reexecutar r75_gap.ps1 e revisar se nova pagina ficou sem artigo.
    private const int GapMaximoTravado = 573;

    private static string Read(string relativo) => File.ReadAllText(Path.Combine(Root, relativo.Replace('/', Path.DirectorySeparatorChar)));

    private static HashSet<string> LerPaginasDoInventario(string inventario)
    {
        var pares = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var linha in inventario.Split('\n'))
        {
            if (!linha.StartsWith("PAGINA|")) continue;
            var campos = linha.TrimEnd('\r').Split('|');
            if (campos.Length < 3) continue;
            pares.Add(campos[1] + "/" + campos[2]);
        }
        return pares;
    }

    private static List<(string Controller, string Acao)> LerArtigosDaFonte()
    {
        var fonte = Read(AjudaRelativo);
        var re = new Regex("Artigo\\(\"[^\"]+\",\\s*\"[^\"]+\",\\s*\"[^\"]+\",\\s*\"[^\"]+\",\\s*\"[^\"]*\"?,\\s*\"([^\"]+)\",\\s*\"([^\"]+)\"");
        return re.Matches(fonte)
            .Select(m => (m.Groups[1].Value, m.Groups[2].Value))
            .ToList();
    }

    private static int LerResumoCampo(string inventario, string campo)
    {
        foreach (var linha in inventario.Split('\n'))
        {
            if (!linha.StartsWith("RESUMO|")) continue;
            var m = Regex.Match(linha, campo + @"=(\d+)");
            return m.Success ? int.Parse(m.Groups[1].Value) : -1;
        }
        return -1;
    }

    [Fact]
    public void Artigos_sempre_apontam_para_pagina_do_inventario()
    {
        var inventario = Read(InventarioRelativo);
        var paginas = LerPaginasDoInventario(inventario);
        var artigos = LerArtigosDaFonte();
        Assert.True(artigos.Count > 0, "nenhum artigo reconhecido em AjudaController - padrao de parser defasado");

        var orfaos = artigos
            .Where(a => !paginas.Contains(a.Controller + "/" + a.Acao))
            .Select(a => a.Controller + "/" + a.Acao)
            .ToList();
        Assert.True(orfaos.Count == 0, "artigo(s) apontando para pagina fora do inventario: " + string.Join(", ", orfaos));

        // frescor: a quantidade de artigos no inventario acompanha a fonte
        var qtdInventario = LerResumoCampo(inventario, "ARTIGOS");
        Assert.Equal(artigos.Count, qtdInventario);
    }

    [Fact]
    public void Gap_sem_artigo_nao_cresce_sem_rerun_do_inventario()
    {
        var inventario = Read(InventarioRelativo);
        var gap = LerResumoCampo(inventario, "GAP_SEM_ARTIGO");
        Assert.True(gap >= 0, "linha RESUMO ausente ou malformada no inventario");
        Assert.True(gap <= GapMaximoTravado, "gap subiu para " + gap + ": reexecutar r75_gap.ps1 e registrar a mudanca na evidencia");
    }

    [Fact]
    public void Quebras_potenciais_travadas_por_revisao()
    {
        // Rotas GET cuja view resolvida nao existe em disco (potencial falha de renderizacao).
        // Grupo travado da R75 - corrigir uma rota exige atualizar esta lista com evidencia.
        var esperadas = new[]
        {
            "agenda/calendario", "agenda/conflitos", "agenda/dia", "agenda/hospitais",
            "agenda/medicos", "agenda/mes", "agenda/semana",
            "demoadmin/gerardados", "demoadmin/index", "demoadmin/roteiros", "demoadmin/status",
            "faturamentoclinico/contasreceber", "faturamentoclinico/recebimentos",
            "gestordashboard/index",
            "v112web/activities", "v112web/billing", "v112web/customers", "v112web/dashboard",
            "v112web/demo", "v112web/homologation", "v112web/inventory", "v112web/journey",
            "v112web/orders", "v112web/outbox", "v112web/products", "v112web/tasks",
            "v112web/templates",
            "workflowsaude360/index", "workflowsaude360/proximaacao"
        };
        var esperados = new HashSet<string>(esperadas, StringComparer.OrdinalIgnoreCase);

        var inventario = Read(InventarioRelativo);
        var atuais = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var linha in inventario.Split('\n'))
        {
            if (!linha.StartsWith("POTE_QUEBRA|")) continue;
            var campos = linha.TrimEnd('\r').Split('|');
            if (campos.Length < 3) continue;
            atuais.Add(campos[1] + "/" + campos[2]);
        }
        Assert.Equal(esperados, atuais);
    }
}
