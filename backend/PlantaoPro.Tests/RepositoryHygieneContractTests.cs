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
            "bar" + "bearia",
            "sala" + "o",
            "est" + "etica",
            "P" + "DV",
            "com" + "anda",
            "fideli" + "dade",
            "admin-demo" + "-store",
            "admin-event" + "-bus",
            "kio" + "sk-flow"
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
