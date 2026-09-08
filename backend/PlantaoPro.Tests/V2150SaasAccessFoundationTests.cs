using Microsoft.AspNetCore.Authorization;
using PlantaoPro.Api;
using PlantaoPro.Api.Controllers;
using PlantaoPro.Api.Data;
using System.Reflection;

namespace PlantaoPro.Tests;

public sealed class V2150SaasAccessFoundationTests
{
    private static string Api(string relativePath) => File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, relativePath));
    private static string Web(string relativePath) => File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, relativePath));

    [Fact]
    public void SessaoAtiva_DeveSerUtilizavel()
    {
        var userId = Guid.NewGuid();
        var session = Session(userId, DateTime.UtcNow.AddMinutes(5));

        Assert.True(AuthenticationSessionState.IsUsable(session, userId, DateTime.UtcNow));
    }

    [Theory]
    [InlineData(true, false, "ATIVO", "ATIVO")]
    [InlineData(false, true, "ATIVO", "ATIVO")]
    [InlineData(false, false, "BLOQUEADO", "ATIVO")]
    [InlineData(false, false, "ATIVO", "SUSPENSO")]
    public void SessaoExpiradaRevogadaOuBloqueada_DeveSerRecusada(bool expired, bool revoked, string userStatus, string tenantStatus)
    {
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var session = Session(userId, expired ? now.AddSeconds(-1) : now.AddMinutes(5));
        session.RevogadaEm = revoked ? now : null;
        session.UsuarioStatus = userStatus;
        session.TenantStatus = tenantStatus;

        Assert.False(AuthenticationSessionState.IsUsable(session, userId, now));
    }

    [Fact]
    public void DtoDeSessao_DeveSerMaterializavelPeloDapper()
    {
        Assert.NotNull(typeof(AuthenticationSessionRow).GetConstructor(Type.EmptyTypes));
        Assert.All(typeof(AuthenticationSessionRow).GetProperties(), property => Assert.True(property.SetMethod?.IsPublic));
    }

    [Fact]
    public void Bearer_DeveValidarSessaoPersistidaDepoisDoJwt()
    {
        var program = Api("Program.cs");
        var sessions = Api("AuthenticationSessionServices.cs");

        Assert.Contains("OnTokenValidated", program);
        Assert.Contains("ValidateAsync(context.Principal", program);
        Assert.Contains("revogada_em", sessions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expira_em", sessions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("u.status", sessions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("c.status", sessions, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Login_DeveCriarSessaoEManterSuperAdminSemTenant()
    {
        var source = Api("Data.cs");

        Assert.Contains("sessions.CreateAsync(sessionGuid", source);
        Assert.Contains("var clienteId = isGlobal ? null : user.ClienteId", source);
        Assert.Contains("var tenantId = isGlobal ? null", source);
        Assert.DoesNotContain("Use seu e-mail ou CPF para acessar esta instituição", source);
    }

    [Fact]
    public void Clientes_DeveSerExclusivoDoAdministradorGlobal()
    {
        var authorize = Assert.Single(typeof(ClientesController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>());

        Assert.Equal(RolesConstants.AdministradorGlobal, authorize.Roles);
    }

    [Theory]
    [InlineData(nameof(UsuariosController.ListUsers))]
    [InlineData(nameof(UsuariosController.Unlock))]
    public void UsuariosLegados_DevemExigirAdministradorReconhecido(string methodName)
    {
        var method = typeof(UsuariosController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        var authorize = Assert.Single(method!.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());

        Assert.Contains(RolesConstants.AdministradorGlobal, authorize.Roles ?? string.Empty);
        Assert.Contains(RolesConstants.AdministradorCliente, authorize.Roles ?? string.Empty);
        Assert.DoesNotContain("ADMINISTRATOR", authorize.Roles ?? string.Empty);
    }

    [Fact]
    public void UsuarioLegado_DeveFiltrarListaEDesbloqueioPeloTenant()
    {
        var source = Api("Data.cs");

        Assert.Contains("coalesce(u.tenant_id,u.cliente_id) = @tenantId", source);
        Assert.Contains("coalesce(tenant_id,cliente_id)=@tenantId", source);
        Assert.Contains("if (!isGlobal && !tenantId.HasValue) return", source);
    }

    [Fact]
    public void DashboardSeguranca_DeveFiltrarUsuariosTentativasESessoesPeloTenant()
    {
        var source = Api("SecurityAdministrationServices.cs");

        Assert.Contains("left join plantaopro.usuarios u on u.id=lt.usuario_id", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("coalesce(u.tenant_id,u.cliente_id)=@tenantId", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("coalesce(tenant_id,cliente_id)=@tenantId", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Contexto de tenant obrigatório", source);
    }

    [Fact]
    public void SuperAdmin_DeveListarESelecionarQualquerTenantAtivo()
    {
        var source = Api("PremiumExperienceServices.cs");

        Assert.Contains("@globalAccess or exists", source);
        Assert.Contains("case when @globalAccess then 'ADMINISTRADOR_GLOBAL'", source);
        Assert.Contains("repository.SelectAsync(Required(u),r.TenantId,IsGlobal(u)", source);
        Assert.Contains("insert into plantaopro.contexto_trocas", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Menu_DeveContinuarFiltradoPorModuloEPermissaoEfetiva()
    {
        var sidebar = Web(Path.Combine("Views", "Shared", "_AppSidebar.cshtml"));
        var access = Web(Path.Combine("Services", "Security", "AccessServices.cs"));

        Assert.Contains("ModuleAccessService.CanAccessModule", sidebar);
        Assert.Contains("PermissionService.HasPermission", sidebar);
        Assert.Contains("access_catalog_version", access);
        Assert.Contains("HasAccessClaim(\"module\"", access);
        Assert.Contains("HasAccessClaim(\"permission\"", access);
    }

    [Fact]
    public void LoginPremium_DeveTerEstadosCriticosSubmitRealEGuia()
    {
        var view = Web(Path.Combine("Views", "Account", "Login.cshtml"));

        Assert.Contains("method=\"post\"", view);
        Assert.Contains("type=\"submit\"", view);
        Assert.Contains("session-expired", view);
        Assert.Contains("user-blocked", view);
        Assert.Contains("tenant-blocked", view);
        Assert.Contains("Como usar esta tela", view);
    }

    private static AuthenticationSessionRow Session(Guid userId, DateTime expiresAt) => new()
    {
        Id = Guid.NewGuid(),
        UsuarioId = userId,
        ExpiraEm = expiresAt,
        RegStatus = "A",
        UsuarioRegStatus = "A",
        UsuarioStatus = "ATIVO",
        TenantStatus = "ATIVO"
    };
}
