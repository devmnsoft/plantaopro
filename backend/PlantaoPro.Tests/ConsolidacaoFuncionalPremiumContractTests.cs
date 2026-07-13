namespace PlantaoPro.Tests;

public class ConsolidacaoFuncionalPremiumContractTests
{
    [Fact]
    public void Saude360_ActionsPrioritarias_NaoDevemRetornarIndexGenerico()
    {
        var raiz = EncontrarRaizRepositorio();
        var arquivo = Path.Combine(raiz, "backend", "PlantaoPro.Web", "Controllers", "Saude360WebControllers.cs");
        var conteudo = File.ReadAllText(arquivo);

        var actionsPrioritarias = new[]
        {
            "Atendimento", "HistoricoPaciente", "Favoritos", "MaisUsados", "ContasReceber",
            "Repasses", "Glosas", "Contratos", "Planos", "Faturamento", "Coberturas", "Autorizacoes"
        };

        foreach (var action in actionsPrioritarias)
        {
            Assert.DoesNotContain("public Task<IActionResult> " + action + "() { return Index(); }", conteudo, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("api/consultas/atendimento", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("api/cid/favoritos", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("api/clinica-financeiro/contas-receber", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("api/convenios/faturamento", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("api/planos-saude/coberturas", conteudo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AuditoriaConsolidacao_DeveDocumentarMatrizQaPendenciasESeguranca()
    {
        var raiz = EncontrarRaizRepositorio();
        var documentos = new[]
        {
            Path.Combine("docs", "homologacao", "auditoria-repositorio-plantao-pro.md"),
            Path.Combine("docs", "produto", "matriz-funcionalidades-plantao-pro.md"),
            Path.Combine("docs", "homologacao", "qa-menu-global.md"),
            Path.Combine("docs", "homologacao", "pendencias-reais-pos-auditoria.md"),
            Path.Combine("docs", "release", "auditoria-consolidacao-geral.md"),
            Path.Combine("docs", "ux", "jornada-cliente-leigo.md"),
            Path.Combine("docs", "ux", "design-system-premium.md"),
            Path.Combine("docs", "seguranca", "checklist-lgpd-auditoria.md"),
            Path.Combine("docs", "homologacao", "qa-final-consolidacao.md")
        };

        foreach (var documento in documentos)
        {
            var caminho = Path.Combine(raiz, documento);
            Assert.True(File.Exists(caminho), "Documento obrigatório ausente: " + documento);
            var conteudo = File.ReadAllText(caminho);
            Assert.Contains("PlantãoPro", conteudo, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string EncontrarRaizRepositorio()
    {
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (diretorio != null && !File.Exists(Path.Combine(diretorio.FullName, ".gitignore")))
        {
            diretorio = diretorio.Parent;
        }

        Assert.NotNull(diretorio);
        return diretorio!.FullName;
    }
}
