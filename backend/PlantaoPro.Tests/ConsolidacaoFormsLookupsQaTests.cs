namespace PlantaoPro.Tests;

public class ConsolidacaoFormsLookupsQaTests
{
    [Fact]
    public void FormularioGenerico_NaoDeveExporIdsManuais_EmCamposPrincipais()
    {
        var raiz = EncontrarRaizRepositorio();
        var formulario = Path.Combine(raiz, "backend", "PlantaoPro.Web", "Views", "Saude360", "Formulario.cshtml");
        var conteudo = File.ReadAllText(formulario);

        Assert.DoesNotContain("Paciente ID", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Médico/profissional ID", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Agendamento ID", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Consulta ID", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Plano de saúde ID", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("_LookupSelect", conteudo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Lookups_DeveTerEndpointsPrincipais_E_ComponentesWeb()
    {
        var raiz = EncontrarRaizRepositorio();
        var api = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Api", "Controllers", "Saude360SupportControllers.cs"));

        foreach (var rota in new[] { "pacientes", "medicos", "agendamentos", "consultas", "convenios", "planos-saude", "hospitais", "especialidades", "cid", "formas-pagamento", "classificacoes-risco" })
        {
            Assert.Contains("\"" + rota + "\"", api, StringComparison.OrdinalIgnoreCase);
        }

        Assert.True(File.Exists(Path.Combine(raiz, "backend", "PlantaoPro.Web", "Views", "Shared", "_LookupSelect.cshtml")));
        Assert.True(File.Exists(Path.Combine(raiz, "backend", "PlantaoPro.Web", "Views", "Shared", "_AutocompleteField.cshtml")));
        Assert.True(File.Exists(Path.Combine(raiz, "backend", "PlantaoPro.Web", "wwwroot", "js", "lookup.js")));
    }

    [Fact]
    public void DocumentacaoFinal_DeveExistir()
    {
        var raiz = EncontrarRaizRepositorio();
        foreach (var documento in new[]
        {
            "docs/release/consolidacao-funcional-forms-ux-premium.md",
            "docs/produto/matriz-status-funcional-plantao-pro.md",
            "docs/homologacao/auditoria-funcional-forms-ux.md",
            "docs/homologacao/qa-menu-global.md",
            "docs/homologacao/qa-database-migrations-seeds.md",
            "docs/homologacao/qa-final-funcional.md",
            "docs/demo/roteiro-demo-funcional-plantao-pro.md",
            "docs/ux/jornada-cliente-leigo.md",
            "docs/ux/design-system-premium.md",
            "docs/operacao/ordem-migrations-seeds.md",
            "docs/seguranca/checklist-lgpd-auditoria.md"
        })
        {
            var caminho = Path.Combine(raiz, documento);
            Assert.True(File.Exists(caminho), "Documento obrigatório ausente: " + documento);
            Assert.Contains("PlantãoPro", File.ReadAllText(caminho), StringComparison.OrdinalIgnoreCase);
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
