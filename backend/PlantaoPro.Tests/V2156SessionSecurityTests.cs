using PlantaoPro.Api;

namespace PlantaoPro.Tests;

public sealed class V2156SessionSecurityTests
{
    [Fact]
    public void SessaoRevogada_NaoPodeAutenticarChamadaSeguinte()
    {
        var row=ValidSession(); row.RevogadaEm=DateTime.UtcNow;
        Assert.False(AuthenticationSessionState.IsUsable(row,row.UsuarioId,DateTime.UtcNow));
    }

    [Fact]
    public void ContextoDeFormularioAntigo_NaoPodeCruzarTenant()
    {
        var row=ValidSession(); row.TenantId=Guid.NewGuid(); row.ClienteId=row.TenantId;
        Assert.False(AuthenticationSessionState.ContextMatches(row,Guid.NewGuid(),row.ClienteId));
        Assert.True(AuthenticationSessionState.ContextMatches(row,row.TenantId,row.ClienteId));
    }

    [Fact]
    public void ClienteSuspenso_MantemSessaoParaFluxoDeRegularizacao()
    {
        var row=ValidSession(); row.TenantStatus="SUSPENSO";
        Assert.True(AuthenticationSessionState.IsUsable(row,row.UsuarioId,DateTime.UtcNow));
    }

    private static AuthenticationSessionRow ValidSession()=>new()
    {
        Id=Guid.NewGuid(),UsuarioId=Guid.NewGuid(),RegStatus="A",UsuarioRegStatus="A",UsuarioStatus="ATIVO",TenantStatus="ATIVO",ExpiraEm=DateTime.UtcNow.AddMinutes(5)
    };
}
