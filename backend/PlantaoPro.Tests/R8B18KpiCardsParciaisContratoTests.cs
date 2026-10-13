using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B18 — §29 item 10: "Cards KPI manuais × partials `_KpiCard`/`_CoverageCard` subutilizados."
///
/// RESULTADO DA VALIDAÇÃO (fonte canônica: o sistema de design de views + os 4 locais apontados):
///
///   O snapshot da varredura está DESATUALIZADO: o parcial canônico `_KpiCard` NÃO é "usado só em
///   Clientes/Inteligencia" — ele é o componente ESTABELECIDO de KPI do design system, consumido por
///   MÚLTIPLAS views (Coordenacao, Marketplace, WhiteLabel×2, Permissoes×2, Clientes/Inteligencia); e
///   existe ao lado de `_CoverageCard` (métrica de cobertura c/ link "Ver detalhes"), usado em
///   CentralEscala. Os dois modelos vivem em <c>DesignSystemViewModels.cs</c>.
///
///   OS 4 LOCAIS apontados pela varredura NÃO são duplicatas byte-idênticas do parcial — são 4
///   RENDERIZAÇÕES DIFERENTES de um "tile de métrica", nenhuma roteada pelo `_KpiCard`:
///     • <c>Bi/Index</c> e <c>Operacoes/Index</c>: KPIs **hidratadas por JS** (shell `<div id=…>` +
///       `fetch('/api/bi/resumo-executivo')` / `fetch('/api/operacoes/dashboard')` montando cards no cliente).
///     • <c>MeuDia/Index</c>: strip próprio `class="day-kpis"` (<c>&lt;article&gt;&lt;span&gt;…&lt;/span&gt;&lt;strong&gt;…&lt;/strong&gt;</c>).
///     • <c>CommandCenter/Index</c>: strip próprio `class="cc-kpis"` (<c>&lt;article&gt;&lt;span&gt;/&lt;strong&gt;/&lt;small&gt;</c>).
///
///   CONCLUSÃO: a "redundância" é CONCEPTUAL (várias formas de exibir um número de métrica), não uma cópia
///   exata removível por swap. Unificar os 4 no `_KpiCard` seria um REFATOR DE DESIGN (trocar markup/classe/
///   DOM em 4 páginas vivas + converter as JS-hidratadas para server-side) — decisão de produto/direção
///   visual, FORA do rito desta rodada (sem regra inventada / sem deleção antecipada; a consolidação
///   byte-equivalente à la B12 não se aplica por não serem idênticos). Canônico estabelecido = o par de
///   parciais do design system. Esta fatia ENTREGUE a validação + CONTRATO anti-drift que fixa:
///   (1) os parciais canônicos existem e o `_KpiCard` é efetivamente estabelecido (múltiplas views);
///   (2) Bi e Operacoes hidratam KPI por JS (não pelo parcial); (3) Meu Dia e CommandCenter usam strips
///   próprios distintos. Zero mudança de produção.
/// </summary>
public sealed class R8B18KpiCardsParciaisContratoTests
{
    private static string Web(params string[] segs) =>
        Path.Combine(new[] { RepositoryPathResolver.BackendRoot, "PlantaoPro.Web" }.Concat(segs).ToArray());

    private const string ParcialKpi = "partial name=\"_KpiCard\"";

    [Fact]
    public void ParciaisCanonicosDoDesignSystem_ExistemEEstaBelecidosNoCodigo()
    {
        Assert.True(File.Exists(Web("Views", "Shared", "_KpiCard.cshtml")), "O parcial canônico _KpiCard deveria existir em Views/Shared.");
        Assert.True(File.Exists(Web("Views", "Shared", "_CoverageCard.cshtml")), "O parcial canônico _CoverageCard deveria existir em Views/Shared.");

        var dsVm = File.ReadAllText(Web("Models", "DesignSystemViewModels.cs"));
        Assert.Contains("KpiCardViewModel", dsVm);
        Assert.Contains("CoverageCardViewModel", dsVm);

        // Refuta o snapshot desatualizado "usado só em Clientes/Inteligencia": o _KpiCard é estabelecido em várias views.
        var usandoKpi = RepositoryPathResolver
            .EnumerateRepositoryFiles(new[] { RepositoryPathResolver.BackendRoot }, "*.cshtml")
            .Where(p => p.Replace('\\', '/').Contains("/PlantaoPro.Web/Views/") && File.ReadAllText(p).Contains(ParcialKpi))
            .ToArray();
        Assert.True(usandoKpi.Length >= 5,
            $"O parcial canônico _KpiCard deveria ser estabelecido em múltiplas views (encontrado em {usandoKpi.Length} arquivo(s)).");
    }

    [Fact]
    public void BiEOperacoes_HidratamKpisPorJS_NaoPeloParcial()
    {
        var bi = File.ReadAllText(Web("Views", "Bi", "Index.cshtml"));
        var ops = File.ReadAllText(Web("Views", "Operacoes", "Index.cshtml"));
        // Renderização client-side: o valor vem de fetch em runtime, não de model server-side no parcial.
        Assert.Contains("fetch('/api/bi/resumo-executivo')", bi);
        Assert.DoesNotContain(ParcialKpi, bi);
        Assert.Contains("fetch('/api/operacoes/dashboard')", ops);
        Assert.DoesNotContain(ParcialKpi, ops);
    }

    [Fact]
    public void MeuDiaeCommandCenter_UsoStripsProprios_DistintosDoParcial()
    {
        var meuDia = File.ReadAllText(Web("Views", "MeuDia", "Index.cshtml"));
        var cc = File.ReadAllText(Web("Views", "CommandCenter", "Index.cshtml"));
        // Cada página tem seu próprio design de KPI (markup/classe distintos do kpi-card), não o parcial.
        Assert.Contains("day-kpis", meuDia);
        Assert.DoesNotContain(ParcialKpi, meuDia);
        Assert.Contains("cc-kpis", cc);
        Assert.DoesNotContain(ParcialKpi, cc);
    }
}
