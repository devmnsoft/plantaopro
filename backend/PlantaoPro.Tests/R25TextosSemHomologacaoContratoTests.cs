using System.Text;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R25-B1 (diretriz r24a1, epic E8 — "tire todos os textos que sugerem homologação"): os literais
/// de homologação/demo removidos ou reescritos na r25b1 não podem reaparecer nos textos de UI.
/// Inventário classificado por file:line e whitelist de domínio em
/// docs/evidencias/2026-10-18-rodada-25/r25a0-inventario-mestre-diretriz-epics.md.
/// Whitelist preservada (conceito de domínio, NÃO cai no epic): ambiente fiscal NF-e da SEFAZ
/// (HOMOLOGACAO/PRODUCAO nas telas Fiscal/Cotações), status "Não Homologada" do monitor fiscal,
/// módulo Operação Assistida (implantação por cliente), vocabulário de demo comercial do módulo
/// de vendas (CommercialDemoWeb/B2BLaunch) e arquivos css/chaves de config (não-texto de UI).
/// </summary>
public class R25TextosSemHomologacaoContratoTests
{
    private static string ReadRelative(params string[] partes) => File.ReadAllText(
        Path.Combine(new[] { RepositoryPathResolver.RepoRoot }.Concat(partes).ToArray()), Encoding.UTF8);

    [Fact]
    public void BannerDoAmbienteDeExemplo_UsoTextoSimplesNasQuatroCulturas()
    {
        // R7DF6 trava as CHAVES dos resx; o VALOR é livre e agora segue a linguagem simples da diretriz.
        var pt = ReadRelative("backend", "PlantaoPro.Web", "Localization", "UiTexts.resx");
        var en = ReadRelative("backend", "PlantaoPro.Web", "Localization", "UiTexts.en.resx");
        var es = ReadRelative("backend", "PlantaoPro.Web", "Localization", "UiTexts.es.resx");
        var fr = ReadRelative("backend", "PlantaoPro.Web", "Localization", "UiTexts.fr.resx");
        foreach (var resx in new[] { pt, en, es, fr })
        {
            Assert.Contains("Auth.DemoNotice", resx, StringComparison.Ordinal);
            Assert.DoesNotContain("Ambiente de demonstração ativo", resx, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Demo environment active", resx, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void RotasDeCompatibilidade_112_NaoAnunciamHomologacaoAoUsuario()
    {
        var ctrl = ReadRelative("backend", "PlantaoPro.Web", "Controllers", "V112WebController.cs");
        Assert.DoesNotContain("Demo funcional", ctrl, StringComparison.Ordinal);
        Assert.DoesNotContain("Homologação visual", ctrl, StringComparison.Ordinal);
        Assert.DoesNotContain("Validar homologação", ctrl, StringComparison.Ordinal);
        Assert.DoesNotContain("status da demo", ctrl, StringComparison.Ordinal);
        Assert.DoesNotContain("boleto demonstrativo", ctrl, StringComparison.Ordinal);

        var view = ReadRelative("backend", "PlantaoPro.Web", "Views", "V112", "OperationalPage.cshtml");
        Assert.DoesNotContain("Homologação v1.12", view, StringComparison.Ordinal);
        Assert.DoesNotContain("Demo explícito quando indicado", view, StringComparison.Ordinal);

        var breadcrumb = ReadRelative("backend", "PlantaoPro.Web", "Services", "BreadcrumbService.cs");
        var linha = breadcrumb.Split('\n').First(l => l.Contains("V112Web", StringComparison.Ordinal));
        Assert.Contains("Compatibilidade", linha, StringComparison.Ordinal);
        Assert.DoesNotContain("Homologação", linha, StringComparison.Ordinal);
    }

    [Fact]
    public void LiteraisDeDemoRemovidos_NaoAparemEmServicosENenhumaView()
    {
        var assistente = ReadRelative("backend", "PlantaoPro.Web", "Services", "AssistenteContextualService.cs");
        Assert.DoesNotContain("massa demo", assistente, StringComparison.OrdinalIgnoreCase);

        var fluxo = ReadRelative("backend", "PlantaoPro.Web", "Services", "Fase2OperationalFlowService.cs");
        Assert.DoesNotContain("demonstrativo", fluxo, StringComparison.OrdinalIgnoreCase);

        var inteligencia = ReadRelative("backend", "PlantaoPro.Web", "Controllers", "OperacaoInteligenteController.cs");
        Assert.DoesNotContain("modo demonstração", inteligencia, StringComparison.OrdinalIgnoreCase);

        // Varredura global das VIEWS pelos marcadores mais fortes de ambiente de teste:
        var raizViews = Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Views");
        var proibidos = new[] { "LOTE-DEMO", "santacasa-homologacao", "Tenant demonstração", "Hospital demonstração" };
        foreach (var arquivo in Directory.EnumerateFiles(raizViews, "*.cshtml", SearchOption.AllDirectories))
        {
            var conteudo = File.ReadAllText(arquivo, Encoding.UTF8);
            foreach (var marcador in proibidos)
            {
                Assert.DoesNotContain(marcador, conteudo, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void BoletosEEstadosVazios_LinguagemSimplesSemDemo()
    {
        var clinico = ReadRelative("backend", "PlantaoPro.Web", "Controllers", "FaturamentoClinicoController.cs");
        Assert.DoesNotContain("somente demonstrativo", clinico, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Demo Boleto", clinico, StringComparison.Ordinal);

        var v115 = ReadRelative("backend", "PlantaoPro.Web", "Controllers", "V115FaturamentoWebControllers.cs");
        Assert.DoesNotContain("demo-boleto", v115, StringComparison.OrdinalIgnoreCase);

        var agenda = ReadRelative("backend", "PlantaoPro.Web", "Views", "Agendamentos", "AgendaPremium.cshtml");
        Assert.DoesNotContain("pacientes demonstrativos", agenda, StringComparison.OrdinalIgnoreCase);

        var perfis = ReadRelative("backend", "PlantaoPro.Web", "Views", "DashboardsPremium", "Perfil.cshtml");
        Assert.DoesNotContain("número demonstrativo", perfis, StringComparison.OrdinalIgnoreCase);

        var faturaView = ReadRelative("backend", "PlantaoPro.Web", "Views", "FaturamentoClinico", "Index.cshtml");
        Assert.DoesNotContain("histórico demonstrativo", faturaView, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("API do tenant", faturaView, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AmbienteFiscalNF_e_WhitelistDeDominio_Permanecem()
    {
        // O termo SEFAZ é conceito de domínio fiscal — a reescrita E8 não pode apagá-lo acidentalmente.
        var fiscal = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Fiscal", "Configurar.cshtml");
        Assert.Contains("\"HOMOLOGACAO\"", fiscal, StringComparison.Ordinal);

        var estabelecimentos = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Cotacoes", "Estabelecimentos.cshtml");
        Assert.Contains("HOMOLOGACAO", estabelecimentos, StringComparison.Ordinal);
    }
}
