using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using PlantaoPro.Api.Controllers;
using System.Reflection;

namespace PlantaoPro.Tests;

public sealed class SwaggerRouteUniquenessContractTests
{
    private static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException("Raiz do repositório não encontrada para testes de contrato.");
        }

        return dir.FullName;
    }

    private static string Read(string path)
    {
        return File.ReadAllText(Path.Combine(Root(), path));
    }

    [Fact]
    public void ApiDashboardDeveEstarApenasNoControllerPrincipal()
    {
        var rotas = ObterRotasApi(typeof(DashboardController), typeof(V112HomologationController));
        var dashboardPrincipal = rotas
            .Where(x => x.Method == "GET" && x.Route == "api/dashboard")
            .ToList();

        Assert.Single(dashboardPrincipal);
        Assert.Equal(nameof(DashboardController), dashboardPrincipal[0].Controller);
    }

    [Fact]
    public void V112DashboardDeveUsarRotaVersionada()
    {
        var v112 = Read("backend/PlantaoPro.Api/Controllers/V112HomologationController.cs");

        Assert.DoesNotContain("\"api/dashboard\"", v112, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("api/v112/dashboard", v112, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RotasDashboardVersionadasNaoDevemConflitarComDashboardPrincipal()
    {
        var rotas = ObterRotasApi(
            typeof(DashboardController),
            typeof(V112HomologationController),
            typeof(V114ProdutoController),
            typeof(V115FaturamentoController));

        Assert.Contains(rotas, x => x.Method == "GET" && x.Route == "api/v112/dashboard");
        Assert.DoesNotContain(rotas, x => x.Controller == nameof(V112HomologationController) && x.Route == "api/dashboard");

        var duplicadas = rotas
            .GroupBy(x => x.Method + " " + x.Route)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.DoesNotContain("GET api/dashboard", duplicadas);
    }

    private static List<ApiRoute> ObterRotasApi(params Type[] controllers)
    {
        var rotas = new List<ApiRoute>();

        foreach (var controller in controllers)
        {
            var prefixos = controller.GetCustomAttributes<RouteAttribute>()
                .Select(x => x.Template ?? string.Empty)
                .DefaultIfEmpty(string.Empty)
                .ToArray();

            foreach (var method in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                foreach (var attr in method.GetCustomAttributes().OfType<HttpMethodAttribute>())
                {
                    foreach (var prefixo in prefixos)
                    {
                        var rota = Combinar(prefixo, attr.Template ?? string.Empty);
                        foreach (var metodo in attr.HttpMethods)
                        {
                            rotas.Add(new ApiRoute(controller.Name, metodo.ToUpperInvariant(), rota));
                        }
                    }
                }
            }
        }

        return rotas;
    }

    private static string Combinar(string prefixo, string template)
    {
        if (string.IsNullOrWhiteSpace(prefixo))
        {
            return Normalizar(template);
        }

        if (string.IsNullOrWhiteSpace(template))
        {
            return Normalizar(prefixo);
        }

        return Normalizar(prefixo.Trim('/') + "/" + template.Trim('/'));
    }

    private static string Normalizar(string rota)
    {
        return rota.Trim('/').ToLowerInvariant();
    }

    private sealed record ApiRoute(string Controller, string Method, string Route);
}
