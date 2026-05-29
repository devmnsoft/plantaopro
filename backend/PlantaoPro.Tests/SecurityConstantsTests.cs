using PlantaoPro.Api;

namespace PlantaoPro.Tests;

public class SecurityConstantsTests
{
    [Fact]
    public void AuditoriaConstants_DeveExporAcoesCriticas()
    {
        Assert.Equal("LOGIN_SUCESSO", AuditoriaConstants.Acoes.LoginSucesso);
        Assert.Equal("ACESSO_NEGADO", AuditoriaConstants.Acoes.AcessoNegado);
        Assert.Equal("BLOQUEIO_TENANT", AuditoriaConstants.Acoes.BloqueioTenant);
        Assert.Equal("API_MOBILE", AuditoriaConstants.Entidades.ApiMobile);
    }

    [Fact]
    public void Permissoes_DeveIncluirAuditoriaEObservabilidadeNoAdminGlobal()
    {
        Assert.Contains(Permissoes.AuditoriaVer, Permissoes.Todas);
        Assert.Contains(Permissoes.ObservabilidadeVer, Permissoes.Todas);
        Assert.Contains(Permissoes.MedicosVer, Permissoes.AdminCliente);
    }
}
