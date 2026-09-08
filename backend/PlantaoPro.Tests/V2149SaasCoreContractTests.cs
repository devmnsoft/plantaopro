using Microsoft.AspNetCore.Authorization;
using PlantaoPro.Api;
using PlantaoPro.Api.Controllers;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using System.ComponentModel.DataAnnotations;
using UsuarioEditorViewModel = PlantaoPro.Web.Models.UsuarioEditorViewModel;

namespace PlantaoPro.Tests;

public sealed class V2149SaasCoreContractTests
{
    [Theory]
    [InlineData("gestor@hospital.com.br", LoginIdentifierKind.Email, "gestor@hospital.com.br")]
    [InlineData("123.456.789-00", LoginIdentifierKind.Cpf, "12345678900")]
    [InlineData("12.345.678/0001-90", LoginIdentifierKind.Cnpj, "12345678000190")]
    public void Login_DeveClassificarENormalizarIdentificador(string value, LoginIdentifierKind expectedKind, string expectedValue)
    {
        var kind = LoginIdentifierNormalizer.Classify(value);

        Assert.Equal(expectedKind, kind);
        Assert.Equal(expectedValue, LoginIdentifierNormalizer.Normalize(value, kind));
    }

    [Fact]
    public void Login_NaoDeveAceitarDocumentoComTamanhoInvalido()
    {
        Assert.Equal(LoginIdentifierKind.Invalid, LoginIdentifierNormalizer.Classify("1234"));
        Assert.DoesNotContain("12345678900", LoginIdentifierNormalizer.AuditValue("123.456.789-00", LoginIdentifierKind.Cpf));
    }

    [Fact]
    public void LoginResponse_DeveTransportarCatalogoEfetivoDeAcesso()
    {
        var properties = typeof(LoginResponse).GetProperties().Select(property => property.Name).ToArray();

        Assert.Contains(nameof(LoginResponse.Permissions), properties);
        Assert.Contains(nameof(LoginResponse.Modules), properties);
    }

    [Fact]
    public void DtoDeModulo_DeveSerMaterializavelPeloDapper()
    {
        var constructor = typeof(SaasModuleDto).GetConstructor(Type.EmptyTypes);

        Assert.NotNull(constructor);
        Assert.All(typeof(SaasModuleDto).GetProperties(), property => Assert.True(property.SetMethod?.IsPublic));
    }

    [Fact]
    public void AlteracoesComerciaisDeModulo_DevemExigirAdministradorGlobal()
    {
        var controller = typeof(ModulosApiController);
        var mutations = new[] { "Criar", "Atualizar", "Habilitar", "Desabilitar", "VincularPlano" };

        foreach (var methodName in mutations)
        {
            var method = controller.GetMethod(methodName);
            Assert.NotNull(method);
            var authorize = Assert.Single(method!.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
            Assert.Contains("ADMINISTRADOR_GLOBAL", authorize.Roles ?? string.Empty);
        }
    }

    [Fact]
    public void CatalogoDeModulo_DeveOferecerFluxosPersistentesDeContratoEPlano()
    {
        var methods = typeof(SaasModuleCatalogService).GetMethods().Select(method => method.Name).ToArray();

        Assert.Contains(nameof(SaasModuleCatalogService.ListAsync), methods);
        Assert.Contains(nameof(SaasModuleCatalogService.SaveAsync), methods);
        Assert.Contains(nameof(SaasModuleCatalogService.ToggleTenantAsync), methods);
        Assert.Contains(nameof(SaasModuleCatalogService.LinkPlanAsync), methods);
    }

    [Theory]
    [InlineData(nameof(SegurancaController.CriarUsuario))]
    [InlineData(nameof(SegurancaController.EditarUsuario))]
    [InlineData(nameof(SegurancaController.Bloquear))]
    [InlineData(nameof(SegurancaController.Desbloquear))]
    public void Usuarios_MutacoesDevemExigirPerfilAdministrador(string methodName)
    {
        var method = typeof(SegurancaController).GetMethods().Single(candidate => candidate.Name == methodName);
        var roles = Assert.Single(method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>()).Roles ?? string.Empty;

        Assert.Contains(RolesConstants.AdministradorGlobal, roles);
        Assert.DoesNotContain(RolesConstants.Auditor, roles);
    }

    [Fact]
    public void UsuarioNovo_DeveExigirSenhaForteEPerfil()
    {
        var model = new UsuarioEditorViewModel
        {
            Nome = "Gestor da unidade",
            Email = "gestor@hospital.test",
            SenhaTemporaria = "fraca",
            PerfilIds = Array.Empty<Guid>()
        };
        var errors = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(model, new ValidationContext(model), errors, true);

        Assert.False(valid);
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(UsuarioEditorViewModel.SenhaTemporaria)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(UsuarioEditorViewModel.PerfilIds)));
    }
}
