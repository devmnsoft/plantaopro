using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using PlantaoPro.Api.Controllers;

namespace PlantaoPro.Tests;

public sealed class M5SaasBoundaryContractTests
{
    private static string Read(string relative) =>
        File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot, relative));

    [Theory]
    [InlineData(nameof(PlanosController.Criar))]
    [InlineData(nameof(PlanosController.Editar))]
    [InlineData(nameof(PlanosController.Inativar))]
    [InlineData(nameof(PlanosController.Reativar))]
    [InlineData(nameof(PlanosController.AtualizarRecursos))]
    public void EscritasNoCatalogoDePlanos_SaoExclusivasDoAdministradorGlobal(string action)
    {
        var method = typeof(PlanosController).GetMethod(action, BindingFlags.Instance | BindingFlags.Public);
        var authorization = Assert.Single(method!.GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal(RolesConstants.AdministradorGlobal, authorization.Roles);
    }

    [Fact]
    public void AlteracoesDePlanos_AuditamAIdentidadeGlobalReal()
    {
        var controller = Read("backend/PlantaoPro.Api/Controllers/SaasCommercialController.cs");

        Assert.Contains("ICurrentUserService _currentUser", controller);
        Assert.Contains("RegistrarAsync(_currentUser.UserId, null", controller);
        Assert.DoesNotContain("RegistrarAsync(null, null, entidade", controller);
    }

    [Fact]
    public void AprovacaoDeModulo_RevalidaDependenciasAtivasNaTransacao()
    {
        var service = Read("backend/PlantaoPro.Api/ModuleContractingService.cs");

        Assert.Contains("modulo_catalogo_dependencias dependency", service);
        Assert.Contains("upper(coalesce(active_contract.status,'ATIVO'))='ATIVO'", service);
        Assert.Contains("if(missingDependency)throw new ConditionsChangedException()", service);
        Assert.Contains("EnsureAcyclicDependencies(fullCatalog, selectedCodes)", service);
        Assert.Contains("catálogo possui ciclo de dependências", service);
    }
}
