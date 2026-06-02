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
    [Fact]
    public void MobileController_DeveCentralizarSolicitacaoNoServicoDeEscala()
    {
        var campo = typeof(MobileController).GetField("_escala", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var metodo = typeof(MobileController).GetMethod(nameof(MobileController.SolicitarPlantao));

        Assert.NotNull(campo);
        Assert.Equal(typeof(PlantaoPro.Api.Data.EscalaService), campo!.FieldType);
        Assert.NotNull(metodo);
    }

    [Fact]
    public void MobileController_DeveExporEndpointsMvpObrigatorios()
    {
        var rotas = typeof(MobileController)
            .GetMethods()
            .SelectMany(m => m.GetCustomAttributes(typeof(HttpMethodAttribute), inherit: true).Cast<HttpMethodAttribute>())
            .SelectMany(a => a.Template is null ? Array.Empty<string>() : new[] { a.Template })
            .ToArray();

        Assert.Contains("auth/login", rotas);
        Assert.Contains("me", rotas);
        Assert.Contains("dashboard", rotas);
        Assert.Contains("plantoes-disponiveis", rotas);
        Assert.Contains("plantoes/{id:guid}", rotas);
        Assert.Contains("plantoes/{id:guid}/solicitar", rotas);
        Assert.Contains("convites", rotas);
        Assert.Contains("convites/{id:guid}/aceitar", rotas);
        Assert.Contains("convites/{id:guid}/recusar", rotas);
        Assert.Contains("minhas-escalas", rotas);
        Assert.Contains("meus-pagamentos", rotas);
        Assert.Contains("notificacoes", rotas);
        Assert.Contains("notificacoes/{id:guid}/lida", rotas);
        Assert.Contains("perfil", rotas);
        Assert.Contains("disponibilidade", rotas);
        Assert.Contains("preferencias", rotas);
        Assert.Contains("suporte/chamados", rotas);
        Assert.Contains("suporte/chamados/{id:guid}", rotas);
    }


    [Fact]
    public void MobileConvites_DeveUsarContratoDeConviteERecusaComMotivo()
    {
        var recusar = typeof(MobileController).GetMethod(nameof(MobileController.RecusarConvite));
        var aceitar = typeof(MobileController).GetMethod(nameof(MobileController.AceitarConvite));
        var dto = typeof(MobileController).GetNestedType("MobileConviteDto");
        var request = typeof(MobileController).GetNestedType("MobileRecusarConviteRequest");

        Assert.NotNull(aceitar);
        Assert.NotNull(recusar);
        Assert.NotNull(dto);
        Assert.NotNull(request);
        Assert.Contains("PlantaoId", dto!.GetProperties().Select(p => p.Name));
        Assert.Contains("Status", dto.GetProperties().Select(p => p.Name));
        Assert.Contains("Motivo", request!.GetProperties().Select(p => p.Name));
    }

    [Fact]
    public void MobileConvites_NaoDeveExporCamposSensiveisNoDto()
    {
        var dto = typeof(MobileController).GetNestedType("MobileConviteDto");
        var propriedades = dto!.GetProperties().Select(p => p.Name).ToArray();

        Assert.DoesNotContain("Senha", propriedades);
        Assert.DoesNotContain("SenhaHash", propriedades);
        Assert.DoesNotContain("Token", propriedades);
        Assert.DoesNotContain("UsuarioId", propriedades);
    }

    [Fact]
    public void MobileSuporte_DeveUsarContratoLeveESemDadosSensiveis()
    {
        var dto = typeof(MobileController).GetNestedType("MobileChamadoSuporteDto");
        var detalhe = typeof(MobileController).GetNestedType("MobileChamadoSuporteDetalheDto");
        var mensagem = typeof(MobileController).GetNestedType("MobileChamadoMensagemDto");
        var response = typeof(MobileController).GetNestedType("MobileChamadoSuporteDetalheResponseDto");
        var request = typeof(MobileController).GetNestedType("MobileCriarChamadoSuporteRequest");

        Assert.NotNull(dto);
        Assert.NotNull(detalhe);
        Assert.NotNull(mensagem);
        Assert.NotNull(response);
        Assert.NotNull(request);
        Assert.Contains("Protocolo", dto!.GetProperties().Select(p => p.Name));
        Assert.Contains("Titulo", request!.GetProperties().Select(p => p.Name));
        Assert.Contains("Descricao", detalhe!.GetProperties().Select(p => p.Name));
        Assert.Contains("Mensagem", mensagem!.GetProperties().Select(p => p.Name));
        Assert.Contains("Mensagens", response!.GetProperties().Select(p => p.Name));
        Assert.DoesNotContain("Senha", dto.GetProperties().Select(p => p.Name));
        Assert.DoesNotContain("Token", dto.GetProperties().Select(p => p.Name));
        Assert.DoesNotContain("UsuarioId", detalhe.GetProperties().Select(p => p.Name));
    }

    [Fact]
    public void OperacaoService_DeveReceberContextoDoUsuarioParaIsolamentoMultiempresa()
    {
        var construtor = Assert.Single(typeof(OperacaoService).GetConstructors());
        var tipos = construtor.GetParameters().Select(p => p.ParameterType).ToArray();

        Assert.Contains(typeof(UsuarioContextService), tipos);
        Assert.Contains(AuditoriaConstants.Entidades.Operacao, new[] { AuditoriaConstants.Entidades.Operacao });
    }

}
