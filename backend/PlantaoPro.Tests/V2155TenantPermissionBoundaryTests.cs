using PlantaoPro.Api.Controllers;

namespace PlantaoPro.Tests;

public sealed class V2155TenantPermissionBoundaryTests
{
    private static string Api(string relativePath) => File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, relativePath));

    [Fact]
    public void DecisaoEfetiva_DeveAceitarSomenteVinculoAtivoEVigente()
    {
        var source = Api("SecurityAdministrationServices.cs");

        Assert.Contains("usuario_tenant_acessos", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("uta.reg_status='A' and uta.status='ATIVO'", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("uta.acesso_inicio is null or uta.acesso_inicio<=now()", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("uta.acesso_fim is null or uta.acesso_fim>now()", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CROSS_TENANT_DENIED", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ConsultasDePermissaoPorUsuario_DevemValidarEscopoAntesDoCalculo()
    {
        var permissionsController = Api(Path.Combine("Controllers", "PermissoesController.cs"));
        var securityController = Api(Path.Combine("Controllers", "SegurancaController.cs"));

        Assert.Contains("UsuarioPertenceAoEscopoAsync(usuarioId, effectiveTenantId", permissionsController, StringComparison.Ordinal);
        Assert.Contains("UsuarioPertenceAoEscopoAsync(request.UsuarioId.Value, effectiveTenantId", permissionsController, StringComparison.Ordinal);
        Assert.Contains("TestarPermissaoNoEscopoAsync", securityController, StringComparison.Ordinal);
        Assert.Contains("Usuário não encontrado no tenant permitido", permissionsController, StringComparison.Ordinal);
        Assert.Contains("Usuário não encontrado no tenant permitido", securityController, StringComparison.Ordinal);
    }

    [Fact]
    public void AdministradorLocal_NaoDeveConfiarNoTenantEnviadoPelaRequisicao()
    {
        var controller = Api(Path.Combine("Controllers", "PermissoesController.cs"));
        var service = Api("SecurityAdministrationServices.cs");

        Assert.Contains("current.IsGlobalAdmin() ? tenantId : current.TenantId", controller, StringComparison.Ordinal);
        Assert.Contains("current.IsGlobalAdmin() ? request.TenantId : current.TenantId", controller, StringComparison.Ordinal);
        Assert.Contains("if (currentUser.IsGlobalAdmin()) return requested", service, StringComparison.Ordinal);
        Assert.Contains("return currentUser.TenantId ?? throw", service, StringComparison.Ordinal);
    }
}
