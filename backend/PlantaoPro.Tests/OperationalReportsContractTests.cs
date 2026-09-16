using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api;
using PlantaoPro.Api.Controllers;

namespace PlantaoPro.Tests;

public sealed class OperationalReportsContractTests
{
    [Fact]
    public void Controller_has_a_traditional_dependency_injection_constructor()
    {
        var constructors = typeof(OperationalReportsController).GetConstructors();

        var constructor = Assert.Single(constructors);
        var parameter = Assert.Single(constructor.GetParameters());
        Assert.Equal(typeof(OperationalReportService), parameter.ParameterType);
    }

    [Fact]
    public void Controller_preserves_authorization_route_and_endpoint_contracts()
    {
        var controller = typeof(OperationalReportsController);
        Assert.NotNull(controller.GetCustomAttributes(typeof(AuthorizeAttribute), true).SingleOrDefault());
        Assert.Equal("api/relatorios-operacionais", controller.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().Single().Template);

        var get = controller.GetMethod(nameof(OperationalReportsController.Get));
        var csv = controller.GetMethod(nameof(OperationalReportsController.Csv));
        Assert.Equal("{kind}", Assert.Single(get!.GetCustomAttributes(typeof(HttpGetAttribute), true).Cast<HttpGetAttribute>()).Template);
        Assert.Equal("{kind}/csv", Assert.Single(csv!.GetCustomAttributes(typeof(HttpGetAttribute), true).Cast<HttpGetAttribute>()).Template);
        Assert.Contains(get.GetParameters(), parameter => parameter.ParameterType == typeof(CancellationToken));
        Assert.Contains(csv.GetParameters(), parameter => parameter.ParameterType == typeof(CancellationToken));
    }

    [Theory]
    [InlineData("=SUM(A1:A2)", "'=SUM(A1:A2)")]
    [InlineData("+cmd", "'+cmd")]
    [InlineData("texto;separado", "\"texto;separado\"")]
    [InlineData("linha\r\nnova", "linha  nova")]
    public void Csv_fields_are_safe_for_spreadsheets(string value, string expected)
    {
        Assert.Equal(expected, OperationalReportService.EscapeCsvField(value));
    }

    [Fact]
    public void Export_failure_preserves_the_http_status_from_report_validation()
    {
        var exception = new OperationalReportExportException("Período inválido.", 422);

        Assert.Equal(422, exception.StatusCode);
        Assert.Equal("Período inválido.", exception.Message);
    }
}
