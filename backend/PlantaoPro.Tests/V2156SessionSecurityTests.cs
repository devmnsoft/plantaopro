using PlantaoPro.Api;

namespace PlantaoPro.Tests;

public sealed class V2156SessionSecurityTests
{
    private static readonly string RepoRoot = RepositoryPathResolver.RepoRoot;

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

    [Fact]
    public void LogoutWeb_RevogaSessaoApiAntesDeDescartarCookieEToken()
    {
        var web = File.ReadAllText(Path.Combine(RepoRoot, "backend/PlantaoPro.Web/Controllers/AccountController.cs"));
        var api = File.ReadAllText(Path.Combine(RepoRoot, "backend/PlantaoPro.Api/Controllers/AuthController.cs"));
        var sessions = File.ReadAllText(Path.Combine(RepoRoot, "backend/PlantaoPro.Api/AuthenticationSessionServices.cs"));

        var logout = web.IndexOf("public async Task<IActionResult> Logout()", StringComparison.Ordinal);
        var apiRevocation = web.IndexOf("api/auth/logout", logout, StringComparison.Ordinal);
        var cookieSignOut = web.IndexOf("SignOutAsync", logout, StringComparison.Ordinal);
        Assert.True(logout >= 0 && apiRevocation > logout && cookieSignOut > apiRevocation);
        Assert.Contains("[HttpPost(\"logout\")]", api);
        Assert.Contains("sessions.RevokeAsync(User, \"LOGOUT\", ct)", api);
        Assert.Contains("revogada_em=coalesce(revogada_em,now())", sessions);
    }

    [Fact]
    public void RedefinicaoDeSenha_RevogaSessoesENaoRegistraIdentificadorEmClaro()
    {
        var api = File.ReadAllText(Path.Combine(RepoRoot, "backend/PlantaoPro.Api/Controllers/AuthController.cs"));
        var web = File.ReadAllText(Path.Combine(RepoRoot, "backend/PlantaoPro.Web/Controllers/AccountController.cs"));

        Assert.Contains("motivo_revogacao='PASSWORD_RESET'", api);
        Assert.Contains("LoginIdentifierNormalizer.AuditValue", api);
        Assert.DoesNotContain("Token inválido/expirado para reset de senha {Email}", api);
        Assert.DoesNotContain("Senha redefinida Email:{Email}", web);
        Assert.DoesNotContain("Falha redefinir senha Email:{Email}", web);
    }

    [Fact]
    public void RefreshDeContexto_PreservaSessaoJwtEOuEncerraSessaoLocal()
    {
        var web = File.ReadAllText(Path.Combine(RepoRoot, "backend/PlantaoPro.Web/Controllers/AccountController.cs"));
        var refresh = web[web.IndexOf("public async Task<IActionResult> RefreshContext", StringComparison.Ordinal)..];

        Assert.Contains("new Claim(\"session_id\", login.SessionId)", refresh);
        Assert.Contains("new Claim(\"jwt\", login.Token)", refresh);
        Assert.Contains("HttpContext.SignOutAsync", refresh);
        Assert.Contains("HttpContext.Session.Clear()", refresh);
        Assert.Contains("Sua sessão expirou ou foi revogada", refresh);
    }

    private static AuthenticationSessionRow ValidSession()=>new()
    {
        Id=Guid.NewGuid(),UsuarioId=Guid.NewGuid(),RegStatus="A",UsuarioRegStatus="A",UsuarioStatus="ATIVO",TenantStatus="ATIVO",ExpiraEm=DateTime.UtcNow.AddMinutes(5)
    };
}
