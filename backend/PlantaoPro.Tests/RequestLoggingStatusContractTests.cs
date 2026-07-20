namespace PlantaoPro.Tests;

public sealed class RequestLoggingStatusContractTests
{
    private static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, ".git"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Raiz não encontrada.");
    }
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root(), path));

    [Fact]
    public void MiddlewareRegistraStatusAposNextComSucessoPorStatusFinal()
    {
        var middleware = Read("backend/PlantaoPro.Api/RequestLoggingMiddleware.cs");
        Assert.Contains("await _next(context)", middleware);
        Assert.Contains("context.Response?.StatusCode", middleware);
        Assert.Contains("statusCode < 400", middleware);
        Assert.Contains("StatusCode", middleware);
        Assert.Contains("Sucesso", middleware);
    }

    [Fact]
    public void ActionFilterNaoUsaStatusPrematuroNoLogFinal()
    {
        var filter = Read("backend/PlantaoPro.Api/RequestLogContext.cs");
        Assert.DoesNotContain("http.Response.StatusCode", filter, StringComparison.Ordinal);
        Assert.DoesNotContain("API request finalizado", filter, StringComparison.Ordinal);
        Assert.Contains("RequestLoggingMiddleware", filter, StringComparison.Ordinal);
    }
}
