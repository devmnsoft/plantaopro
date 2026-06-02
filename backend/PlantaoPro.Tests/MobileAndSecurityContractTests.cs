using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api;
using PlantaoPro.Api.Controllers;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Tests;

public class MobileAndSecurityContractTests
{
    [Fact]
    public void MobileController_DeveExigirJwtPorPadrao_EPermitirLoginAnonimo()
    {
        var controllerAuthorize = typeof(MobileController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true);
        var loginMethod = typeof(MobileController).GetMethod(nameof(MobileController.Login));

        Assert.NotEmpty(controllerAuthorize);
        Assert.NotNull(loginMethod);
        Assert.NotEmpty(loginMethod!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
    }

    [Fact]
    public void MobileController_DeveEstarDocumentadoComTagMobileERotaPadrao()
    {
        var route = Assert.Single(typeof(MobileController).GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>());
        var tagAttribute = Assert.Single(typeof(MobileController).GetCustomAttributes(inherit: true).Where(a => a.GetType().Name == "TagsAttribute"));
        var tagsProperty = tagAttribute.GetType().GetProperty("Tags");
        var tags = Assert.IsAssignableFrom<IEnumerable<string>>(tagsProperty!.GetValue(tagAttribute));

        Assert.Equal("api/mobile", route.Template);
        Assert.Contains("Mobile", tags);
    }

    [Fact]
    public void MobileLoginResponse_NaoDeveRetornarSenhaHashOuSegredo()
    {
        var propriedades = typeof(MobileLoginResponseDto).GetProperties().Select(p => p.Name).ToArray();

        Assert.Contains(nameof(MobileLoginResponseDto.Token), propriedades);
        Assert.DoesNotContain("Senha", propriedades);
        Assert.DoesNotContain("SenhaHash", propriedades);
        Assert.DoesNotContain("Password", propriedades);
        Assert.DoesNotContain("Secret", propriedades);
    }

    [Fact]
    public void Permissoes_DeveManterIsolamentoPorPerfilEssencial()
    {
        Assert.Equal("ADMINISTRADOR", RolesConstants.Administrador);
        Assert.Equal("MEDICO", RolesConstants.Medico);
        Assert.Contains(RolesConstants.Financeiro, RolesConstants.FinanceiroGestao);
        Assert.DoesNotContain(RolesConstants.Medico, RolesConstants.FinanceiroGestao);
        Assert.DoesNotContain(RolesConstants.Financeiro, RolesConstants.CadastrosOperacao);
    }

    [Fact]
    public void ApiResponse_FalhaSemToken_DeveRepresentarUnauthorizedAmigavel()
    {
        var response = ApiResponse<object>.Fail("Token ausente ou inválido.", 401);

        Assert.False(response.Success);
        Assert.Equal(401, response.StatusCode);
        Assert.DoesNotContain("Exception", response.Message);
        Assert.DoesNotContain("stack", response.Message, StringComparison.OrdinalIgnoreCase);
    }
}
