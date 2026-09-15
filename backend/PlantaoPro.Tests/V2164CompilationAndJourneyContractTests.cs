namespace PlantaoPro.Tests;

public sealed class V2164CompilationAndJourneyContractTests
{
    private static string Read(string relative) => File.ReadAllText(Path.Combine(FindRoot(), relative));

    private static string FindRoot()
    {
        var path = AppContext.BaseDirectory;
        while (path is not null && !File.Exists(Path.Combine(path, "database", "install-manifest.json")))
            path = Directory.GetParent(path)?.FullName;
        return path ?? throw new DirectoryNotFoundException();
    }

    [Fact]
    public void CatalogSql_UsesACSharp10CompatibleVerbatimLiteralWithoutChangingTheQuery()
    {
        var service = Read("backend/PlantaoPro.Api/ModuleContractingService.cs");

        Assert.Contains("new CommandDefinition(@\"select m.id,m.codigo", service);
        Assert.Contains("as \"\"DependenciasArray\"\"", service);
        Assert.Contains("left join plantaopro.tenant_modulos tm on tm.modulo_id=m.id and tm.tenant_id=@tenant", service);
        Assert.Contains("left join plantaopro.modulo_catalogo_dependencias md on md.modulo_id=m.id", service);
        Assert.Contains("where m.reg_status='A' group by m.id,tm.status,tm.habilitado order by m.nome", service);
        Assert.DoesNotContain("$@\"select m.id,m.codigo", service);
    }

    [Fact]
    public void ContractReview_AcceptsAnActiveDependencyButNotASuspendedOrScheduledOne()
    {
        var service = Read("backend/PlantaoPro.Api/ModuleContractingService.cs");

        Assert.Contains("x.EstadoContratual == \"ATIVO\"", service);
        Assert.Contains("!selectedCodes.Contains(x) && !contractedCodes.Contains(x)", service);
        Assert.Contains("Distinct(StringComparer.OrdinalIgnoreCase)", service);
        Assert.Contains("x.EstadoContratual is \"ATIVO\" or \"SUSPENSO\"", service);
    }

    [Fact]
    public void Tests_KeepTheApiProjectReferenceInsteadOfABinaryReference()
    {
        var project = Read("backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj");

        Assert.Contains("<ProjectReference Include=\"../PlantaoPro.Api/PlantaoPro.Api.csproj\" />", project);
        Assert.DoesNotContain("PlantaoPro.Api.dll", project);
        Assert.DoesNotContain("<HintPath>", project);
    }

    [Fact]
    public void Login_PostsWithAntiforgeryAndAlwaysRecoversItsLoadingState()
    {
        var view = Read("backend/PlantaoPro.Web/Views/Account/Login.cshtml");
        var script = Read("backend/PlantaoPro.Web/wwwroot/js/auth-login.js");

        Assert.Contains("@Html.AntiForgeryToken()", view);
        Assert.Contains("method=\"post\"", view);
        Assert.Contains("name=\"returnUrl\"", view);
        Assert.Contains("data-focus-invalid", view);
        Assert.Contains("resetSubmission", script);
        Assert.DoesNotContain("window.setTimeout", script);
        Assert.Contains("client.Timeout", Read("backend/PlantaoPro.Web/Program.cs"));
        Assert.Contains("window.addEventListener(\"pageshow\"", script);
        Assert.Contains("requestInFlight", script);
        Assert.DoesNotContain("resetSubmission();\n        if (errorSummary)", script);
        Assert.DoesNotContain("alert(", script);
        Assert.DoesNotContain("confirm(", script);
    }

    [Fact]
    public void PasswordRecovery_KeepsThePublicResponseGenericAndDoesNotExposeTokensOrEmail()
    {
        var api = Read("backend/PlantaoPro.Api/Controllers/AuthController.cs");
        var web = Read("backend/PlantaoPro.Web/Controllers/AccountController.cs");

        Assert.DoesNotContain("TokenDev", api);
        Assert.DoesNotContain("Solicitação de recuperação de senha para {Email}", api);
        Assert.Contains("Entrega:PENDENTE_SEM_PROVEDOR", api);
        Assert.DoesNotContain("ResponseSample", web);
        Assert.DoesNotContain("Token de desenvolvimento", web);
    }
}
