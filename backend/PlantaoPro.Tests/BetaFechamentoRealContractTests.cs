namespace PlantaoPro.Tests;

public class BetaFechamentoRealContractTests
{
    [Fact]
    public void BaseWebController_DeveRegistrarAmostraEmFalhasDeDesserializacaoPaginada()
    {
        var raiz = EncontrarRaizRepositorio();
        var arquivo = Path.Combine(raiz, "backend", "PlantaoPro.Web", "Controllers", "BaseWebController.cs");
        var conteudo = File.ReadAllText(arquivo);

        Assert.Contains("CreateResponseSample", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Erro de desserialização paginada. Endpoint:{Endpoint} Status:{Status} ResponseSample:{ResponseSample}", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("API paginada retornou envelope sem sucesso", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TryGetProperty(root, out _, \"success\", \"Success\")", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TryGetProperty(root, out _, \"data\", \"Data\")", conteudo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OperacaoAssistida_DeveUsarAjaxSeguroEmAcoesCriticasDaUx()
    {
        var raiz = EncontrarRaizRepositorio();
        var arquivos = new[]
        {
            Path.Combine(raiz, "backend", "PlantaoPro.Web", "Views", "OperacaoAssistida", "Checklist.cshtml"),
            Path.Combine(raiz, "backend", "PlantaoPro.Web", "Views", "OperacaoAssistida", "Ocorrencias.cshtml"),
            Path.Combine(raiz, "backend", "PlantaoPro.Web", "Views", "OperacaoAssistida", "Treinamentos.cshtml")
        };

        foreach (var arquivo in arquivos)
        {
            var conteudo = File.ReadAllText(arquivo);
            Assert.Contains("data-ajax-form=\"true\"", conteudo, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("data-confirm=\"true\"", conteudo, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("data-confirm-title", conteudo, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("data-confirm-message", conteudo, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("data-confirm-type", conteudo, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void RelatorioFechamentoReal_DeveRegistrarValidacoesPendenciasEPrLimpo()
    {
        var raiz = EncontrarRaizRepositorio();
        var arquivo = Path.Combine(raiz, "docs", "homologacao", "relatorio-fechamento-real-beta-2026-06-06.md");
        var conteudo = File.ReadAllText(arquivo);

        Assert.Contains("codex/plantaopro-beta-fechamento-real", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("backup/antes-plantaopro-beta-fechamento-real", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Build API", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Build Web", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet não disponível", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Pendências reais", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PR limpo", conteudo, StringComparison.OrdinalIgnoreCase);
    }

    private static string EncontrarRaizRepositorio()
    {
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (diretorio is not null && !Directory.Exists(Path.Combine(diretorio.FullName, ".git")))
        {
            diretorio = diretorio.Parent;
        }

        if (diretorio is null)
        {
            throw new InvalidOperationException("Raiz do repositório não encontrada para testes de contrato.");
        }

        return diretorio.FullName;
    }
}
