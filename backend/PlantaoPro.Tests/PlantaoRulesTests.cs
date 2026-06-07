using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Tests;

public class PlantaoRulesTests
{
    private readonly PlantaoRegraService _service = new();

    [Fact]
    public void ValidarCriacao_DeveFalhar_QuandoDataFimMenorOuIgualInicio()
    {
        var req = new CreatePlantaoRequest(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddHours(-1), 100, 1, "Presencial", null);
        var resp = _service.ValidarCriacao(req);
        Assert.False(resp.Success);
    }

    [Fact]
    public void ValidarCriacao_DeveFalhar_QuandoVagasZero()
    {
        var req = new CreatePlantaoRequest(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddHours(12), 100, 0, "Presencial", null);
        var resp = _service.ValidarCriacao(req);
        Assert.False(resp.Success);
    }

    [Fact]
    public void ValidarCriacao_DeveFalhar_QuandoValorNegativo()
    {
        var req = new CreatePlantaoRequest(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddHours(12), -1, 1, "Presencial", null);
        var resp = _service.ValidarCriacao(req);
        Assert.False(resp.Success);
    }

    [Fact]
    public void Transicao_DevePermitirFluxoEsperado()
    {
        var transicao = new PlantaoTransicaoService();
        Assert.True(transicao.PodeTransicionar(PlantaoStatus.Rascunho, PlantaoStatus.Aberto));
        Assert.False(transicao.PodeTransicionar(PlantaoStatus.Realizado, PlantaoStatus.Aberto));
    }
}


public class EnterpriseSecurityConstantsTests
{
    [Fact]
    public void Auditoria_DeveListarEventosCriticosPadronizados()
    {
        Assert.Equal("LOGIN_SUCESSO", PlantaoPro.Api.AuditoriaConstants.Acoes.LoginSucesso);
        Assert.Equal("ACESSO_NEGADO", PlantaoPro.Api.AuditoriaConstants.Acoes.AcessoNegado);
        Assert.Equal("BLOQUEIO_TENANT", PlantaoPro.Api.AuditoriaConstants.Acoes.BloqueioTenant);
    }

    [Fact]
    public void Permissoes_DeveListarPermissoesMinimas()
    {
        Assert.Equal("AUDITORIA_VER", PlantaoPro.Api.PermissionConstants.AuditoriaVer);
        Assert.Equal("OBSERVABILIDADE_VER", PlantaoPro.Api.PermissionConstants.ObservabilidadeVer);
        Assert.Equal("FINANCEIRO_CONFIRMAR", PlantaoPro.Api.PermissionConstants.FinanceiroConfirmar);
    }
}

public class AuthContractTests
{
    [Fact]
    public void LoginResponse_DeveCarregarClienteIdParaIsolamentoMultiempresa()
    {
        var clienteId = Guid.NewGuid();
        var response = new LoginResponse("token", DateTime.UtcNow.AddHours(1), Guid.NewGuid(), "Admin", new[] { "ADMINISTRADOR" }, clienteId);

        Assert.Equal(clienteId, response.ClienteId);
    }

    [Fact]
    public void Auditoria_DeveListarEntidadesSaasCriticas()
    {
        Assert.Equal("FATURA_SAAS", PlantaoPro.Api.AuditoriaConstants.Entidades.FaturaSaas);
        Assert.Equal("ASSINATURA", PlantaoPro.Api.AuditoriaConstants.Entidades.Assinatura);
        Assert.Equal("API_MOBILE", PlantaoPro.Api.AuditoriaConstants.Entidades.ApiMobile);
    }
}
