using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Controllers;
using PlantaoPro.Web.Services;

namespace PlantaoPro.Tests;

public sealed class V1230OperationalPilotBehaviorTests
{
    private static readonly string[] RequiredProfiles =
    {
        "Administrador Global", "Administrador Cliente", "Administrador Clínica", "Coordenação",
        "Operador", "Recepção", "Triagem", "Enfermagem", "Médico", "Financeiro",
        "Financeiro Clínica", "Faturamento Convênio", "Hospital", "Parceiro", "Suporte",
        "Auditor", "Auditor Clínico", "Comercial", "Customer Success"
    };

    [Fact]
    public void EveryAuthenticatedProfileHasCanonicalNavigation()
    {
        var catalog = new FeatureCatalogService();

        foreach (var profile in RequiredProfiles)
        {
            var navigation = catalog.Navigation.Where(item => string.Equals(item.Profile, profile, StringComparison.OrdinalIgnoreCase)).ToList();
            Assert.NotEmpty(navigation);
            Assert.InRange(navigation.Count, 1, 12);
            Assert.All(navigation, item => Assert.Contains(catalog.Features,
                feature => feature.Code == item.FeatureCode && feature.Status == "CANONICAL" && feature.IsAvailable));
        }
    }

    [Fact]
    public void CoverageInvitationIsAnAntiforgeryProtectedAsyncOperation()
    {
        var action = typeof(CentralEscalaController).GetMethod(nameof(CentralEscalaController.Convidar));

        Assert.NotNull(action);
        Assert.NotNull(action!.GetCustomAttributes(typeof(HttpPostAttribute), true).SingleOrDefault());
        Assert.NotNull(action.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true).SingleOrDefault());
        Assert.Equal(typeof(Task<IActionResult>), action.ReturnType);
    }
}
