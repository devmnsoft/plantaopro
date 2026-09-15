using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using PlantaoPro.Api.Controllers;

namespace PlantaoPro.Tests;

public sealed class PerfilCrudAuditTests
{
    [Theory]
    [InlineData(nameof(PerfisController.Criar))]
    [InlineData(nameof(PerfisController.Editar))]
    [InlineData(nameof(PerfisController.Inativar))]
    [InlineData(nameof(PerfisController.Permissoes))]
    public void MutacoesDePerfil_ExigemPapelAdministrativo(string action)
    {
        var method = typeof(PerfisController).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(candidate => candidate.Name == action);

        var roles = method.GetCustomAttribute<AuthorizeAttribute>()?.Roles ?? string.Empty;

        Assert.Contains("ADMINISTRADOR_CLIENTE", roles, StringComparison.Ordinal);
        Assert.DoesNotContain("AUDITOR", roles, StringComparison.Ordinal);
        Assert.DoesNotContain("FINANCEIRO", roles, StringComparison.Ordinal);
    }

    [Fact]
    public void FormularioDePerfil_UsaPostAcessivelEProtecaoContraReenvio()
    {
        var view = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Web", "Views", "Perfis", "Form.cshtml");

        Assert.Contains("method=\"post\"", view);
        Assert.Contains("AntiForgeryToken", view);
        Assert.Contains("data-focus-invalid", view);
        Assert.Contains("data-unsaved-form", view);
        Assert.Contains("data-submit-feedback", view);
        Assert.Contains("Como usar esta página", view);
    }

    [Fact]
    public void PersistenciaDePerfil_DistingueAusenciaDuplicidadeEConcorrencia()
    {
        var service = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Api", "SelfServiceServices.cs");

        Assert.Contains("pg_advisory_xact_lock", service);
        Assert.Contains("affected == 0", service);
        Assert.Contains("protegido pelo sistema", service);
        Assert.Contains("Já existe um perfil ativo", service);
        Assert.Contains("Fail(\"Já existe um perfil ativo com este nome ou código no cliente.\", 409)", service);
    }
}
