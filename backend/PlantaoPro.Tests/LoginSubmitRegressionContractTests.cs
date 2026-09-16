namespace PlantaoPro.Tests;

public sealed class LoginSubmitRegressionContractTests
{
    private static string Read(string relative) => File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot, relative));

    [Fact]
    public void GenericSubmitFeedback_DoesNotOwnLoginLoadingState()
    {
        var script = Read("backend/PlantaoPro.Web/wwwroot/js/form-experience.js");

        Assert.Contains("form.matches('[data-submit-loading=\"manual\"], [data-login-form]')", script);
        Assert.Contains("return;", script);
    }

    [Fact]
    public void LoginHandler_RespectsEarlierValidationCancellation()
    {
        var script = Read("backend/PlantaoPro.Web/wwwroot/js/auth-login.js");
        var handler = script.IndexOf("form?.addEventListener(\"submit\"", StringComparison.Ordinal);
        var preventedGuard = script.IndexOf("if (event.defaultPrevented) return;", handler, StringComparison.Ordinal);
        var loadingState = script.IndexOf("form.dataset.requestStarted = \"true\";", handler, StringComparison.Ordinal);

        Assert.True(handler >= 0);
        Assert.True(preventedGuard > handler);
        Assert.True(loadingState > preventedGuard);
        Assert.DoesNotContain("form.submit()", script);
    }

    [Fact]
    public void LoginPage_LoadsGenericFeedbackBeforeItsSingleSpecificHandler()
    {
        var layout = Read("backend/PlantaoPro.Web/Views/Shared/_AuthLayout.cshtml");
        var view = Read("backend/PlantaoPro.Web/Views/Account/Login.cshtml");

        Assert.Equal(1, Count(layout, "form-experience.js"));
        Assert.Equal(1, Count(view, "auth-login.js"));
        Assert.Contains("data-submit-loading=\"manual\"", view);
        Assert.Contains("data-login-form", view);
    }

    [Fact]
    public void WebAndApiLogin_PropagateANonSensitiveCorrelationIdentifier()
    {
        var web = Read("backend/PlantaoPro.Web/Controllers/AccountController.cs");
        var api = Read("backend/PlantaoPro.Api/Controllers/AuthController.cs");

        Assert.Contains("HttpContext.TraceIdentifier", web);
        Assert.Contains("X-Correlation-ID", web);
        Assert.Contains("BeginScope", web);
        Assert.Contains("Request.Headers[\"X-Correlation-ID\"]", api);
        Assert.Contains("Response.Headers[\"X-Correlation-ID\"]", api);
        Assert.Contains("BeginScope", api);
    }

    private static int Count(string value, string fragment) =>
        value.Split(fragment, StringSplitOptions.None).Length - 1;
}
