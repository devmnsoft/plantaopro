namespace PlantaoPro.Tests;

public class RepositoryHygieneContractTests
{
    [Fact]
    public void Repositorio_NaoDeveVersionarDiretoriosRaizDoProdutoIncorreto()
    {
        var raiz = EncontrarRaizRepositorio();
        var diretoriosProibidos = new[]
        {
            "Admin" + "Web",
            "Public" + "Web",
            "Kio" + "sk",
            "Kio" + "skWeb",
            "Admin" + "Api",
            "Public" + "Api",
            "Kio" + "skApi",
            "Mobile" + "App"
        };

        foreach (var diretorio in diretoriosProibidos)
        {
            Assert.False(Directory.Exists(Path.Combine(raiz, diretorio)), $"Diretório externo não permitido na raiz: {diretorio}");
        }

        Assert.True(Directory.Exists(Path.Combine(raiz, "mobile", "PlantaoPro.App")));
    }

    [Fact]
    public void Repositorio_NaoDeveConterTermosDoProdutoIncorretoEmArquivosVersionaveis()
    {
        var raiz = EncontrarRaizRepositorio();
        var termosProibidos = new[]
        {
            "Barber" + "Sync",
            "barber" + "sync",
            "Admin" + "Api",
            "Public" + "Api",
            "Kio" + "skApi",
            "KIO" + "SK",
            "Public" + "Web",
            "Admin" + "Web",
            "Mobile" + "App",
            "bar" + "bearia",
            "sala" + "o",
            "sal" + "ao",
            "est" + "etica",
            "P" + "DV",
            "com" + "anda",
            "fideli" + "dade",
            "api:" + "8080",
            "80" + "81",
            "80" + "82",
            "80" + "83",
            "admin-demo" + "-store",
            "admin-event" + "-bus",
            "kio" + "sk-flow",
            "kio" + "sk.js",
            "public" + ".js"
        };

        var arquivos = Directory.EnumerateFiles(raiz, "*", SearchOption.AllDirectories)
            .Where(DeveInspecionar)
            .ToArray();

        foreach (var arquivo in arquivos)
        {
            var conteudo = File.ReadAllText(arquivo);
            foreach (var termo in termosProibidos)
            {
                Assert.DoesNotContain(termo, conteudo, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Gitignore_DeveBloquearBinariosArtefatosEDependenciasLocais()
    {
        var raiz = EncontrarRaizRepositorio();
        var gitignore = File.ReadAllText(Path.Combine(raiz, ".gitignore"));

        Assert.Contains("*.dll", gitignore, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("*.pdb", gitignore, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("*.zip", gitignore, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("*.apk", gitignore, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("*.aab", gitignore, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("node_modules/", gitignore, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DocumentacaoBetaHomologavel_DeveConterArtefatosObrigatoriosDeAceite()
    {
        var raiz = EncontrarRaizRepositorio();
        var documentosObrigatorios = new[]
        {
            Path.Combine("docs", "homologacao", "checklist-beta-comercial.md"),
            Path.Combine("docs", "homologacao", "checklist-beta-homologavel.md"),
            Path.Combine("docs", "homologacao", "roteiro-operacao-ponta-a-ponta.md"),
            Path.Combine("docs", "homologacao", "roteiro-saas-basico.md"),
            Path.Combine("docs", "homologacao", "roteiro-operacao-assistida.md"),
            Path.Combine("docs", "demo", "roteiro-demo-comercial.md"),
            Path.Combine("docs", "demo", "usuarios-demonstracao.md"),
            Path.Combine("docs", "deploy", "checklist-go-live-beta.md"),
            Path.Combine("docs", "deploy", "deploy-producao-controlada.md"),
            Path.Combine("docs", "mobile", "mobile-api-endpoints.md"),
            Path.Combine("docs", "mobile", "mobile-fluxos.md"),
            Path.Combine("docs", "mobile", "sprint-zero-app.md"),
            Path.Combine("docs", "saas", "faturamento-saas.md"),
            Path.Combine("docs", "suporte", "chamados.md"),
            Path.Combine("docs", "customer-success", "customer-success.md"),
            Path.Combine("docs", "operacao-assistida", "operacao-assistida.md"),
            Path.Combine("docs", "homologacao", "relatorio-beta-homologavel-2026-06-05.md")
        };

        foreach (var documento in documentosObrigatorios)
        {
            var caminho = Path.Combine(raiz, documento);
            Assert.True(File.Exists(caminho), $"Documento obrigatório ausente: {documento}");
            var conteudo = File.ReadAllText(caminho);
            Assert.Contains("PlantãoPro", conteudo, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ChecklistBetaHomologavel_DeveConsolidarGatesDaHomologacaoFinal()
    {
        var raiz = EncontrarRaizRepositorio();
        var arquivo = Path.Combine(raiz, "docs", "homologacao", "checklist-beta-homologavel.md");
        var conteudo = File.ReadAllText(arquivo);

        Assert.Contains("Beta Homologável Final", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Gate 0", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Gate 1", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Operação médica ponta a ponta", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SaaS, faturamento e operação assistida", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("API Mobile MVP", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Roteiro manual final obrigatório", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Pendências reais", conteudo, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public void SanitizacaoBeta_DeveDocumentarValidacoesObrigatoriasSemResiduosExternos()
    {
        var raiz = EncontrarRaizRepositorio();
        var arquivo = Path.Combine(raiz, "docs", "homologacao", "relatorio-sanitizacao-beta-2026-06-05.md");
        var conteudo = File.ReadAllText(arquivo);

        Assert.Contains("PlantãoPro", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("branch work", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("produto incorreto", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("aplicativo móvel multiplataforma permitido", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/api/health", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pendências", conteudo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HealthController_DeveRetornarApiResponseTipado()
    {
        var raiz = EncontrarRaizRepositorio();
        var arquivo = Path.Combine(raiz, "backend", "PlantaoPro.Api", "Controllers", "HealthController.cs");
        var conteudo = File.ReadAllText(arquivo);

        Assert.Contains("ApiResponse<HealthDto>", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ProducesResponseType", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PlantaoPro.Api online", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("new {", conteudo, StringComparison.OrdinalIgnoreCase);
    }

    private static bool DeveInspecionar(string arquivo)
    {
        var normalizado = arquivo.Replace(Path.DirectorySeparatorChar, '/');
        if (normalizado.Contains("/.git/", StringComparison.OrdinalIgnoreCase)) return false;
        if (normalizado.Contains("/bin/", StringComparison.OrdinalIgnoreCase)) return false;
        if (normalizado.Contains("/obj/", StringComparison.OrdinalIgnoreCase)) return false;
        if (normalizado.Contains("/node_modules/", StringComparison.OrdinalIgnoreCase)) return false;
        if (normalizado.Contains("/.vs/", StringComparison.OrdinalIgnoreCase)) return false;

        var extensao = Path.GetExtension(arquivo).ToLowerInvariant();
        return extensao is ".cs" or ".cshtml" or ".md" or ".json" or ".sql" or ".js" or ".ts" or ".tsx" or ".css";
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
