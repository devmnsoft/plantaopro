namespace PlantaoPro.Tests;

public sealed class ConflictTenantIsolationContractTests
{
    private static string Api(string relativePath) => File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, relativePath));

    [Fact]
    public void ConflictEndpoints_MustValidateDoctorTenantBeforeQueryingSchedules()
    {
        var controller = Api(Path.Combine("Controllers", "ConflitosController.cs"));

        Assert.Contains("PodeAcessarMedicoAsync(usuarioId.Value, medicoId)", controller, StringComparison.Ordinal);
        Assert.Contains("RegistrarAcessoNegadoAsync", controller, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status403Forbidden", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Recommendations_MustNeverMixDoctorsAndShiftsFromDifferentTenants()
    {
        var service = Api("BusinessRulesServices.cs");

        Assert.Contains("medico.cliente_id=p.cliente_id", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("m.cliente_id=@clienteId", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cliente_id as ClienteId", service, StringComparison.OrdinalIgnoreCase);
    }
}
