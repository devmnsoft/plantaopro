namespace PlantaoPro.Tests;

public class ConsolidacaoFormsLookupsQaTests
{
    [Fact]
    public void FormularioGenerico_NaoDeveExporIdsManuais_EmCamposPrincipais()
    {
        var raiz = RepositoryPathResolver.RepoRoot;
        var formulario = Path.Combine(RepositoryPathResolver.WebRoot, "Views", "Saude360", "Formulario.cshtml");
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
        var raiz = RepositoryPathResolver.RepoRoot;
        var api = File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "Controllers", "Saude360SupportControllers.cs"));

        foreach (var rota in new[] { "pacientes", "medicos", "agendamentos", "consultas", "convenios", "planos-saude", "hospitais", "especialidades", "cid", "formas-pagamento", "classificacoes-risco" })
        {
            Assert.Contains("\"" + rota + "\"", api, StringComparison.OrdinalIgnoreCase);
        }

        Assert.True(File.Exists(Path.Combine(RepositoryPathResolver.WebRoot, "Views", "Shared", "_LookupSelect.cshtml")));
        Assert.True(File.Exists(Path.Combine(RepositoryPathResolver.WebRoot, "Views", "Shared", "_AutocompleteField.cshtml")));
        Assert.True(File.Exists(Path.Combine(RepositoryPathResolver.WebRoot, "wwwroot", "js", "lookup.js")));
    }

    [Fact]
    public void DocumentacaoFinal_DeveExistir()
    {
        var raiz = RepositoryPathResolver.RepoRoot;
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
}
