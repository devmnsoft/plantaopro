using System.Reflection;
using System.Security.Claims;
using PlantaoPro.CrossCutting.Security;
using PlantaoPro.Web.Security;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// Paridade entre as constantes de role (Web e API) e o catálogo canônico de roles.
/// Garante que nenhum [Authorize(Roles=...)] aponte para role fora do catálogo e que o
/// catálogo não tenha role sem constante nos dois projetos. Aliases legacy são tolerados
/// apenas quando documentados explicitamente abaixo.
/// </summary>
public sealed class RoleCatalogConstantsParityTests
{
    private static readonly RoleCatalog Catalog = new();

    // Aliases legacy aceitos intencionalmente (persistidos em banco ou usados inline em
    // [Authorize]). Não são roles do catálogo; a lista faz o papel de documentação viva.
    private static readonly HashSet<string> DocumentedAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        "ADMIN_CLIENTE",        // normalizado por IRoleCatalog.Normalize -> ADMINISTRADOR_CLIENTE
        "GESTOR_OPERACIONAL",   // aceito em [Authorize] do Administrativo360 (Web), legado
        "CONSULTA_CLIENTE"      // aceito em [Authorize] do Administrativo360 (Web), legado
    };

    private static IEnumerable<string> RoleCodeConstants(Type type) =>
        type
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .Where(value => !value.Contains(',')) // constantes de grupo (lista de roles) ficam de fora
            .Distinct(StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void ConstantesDeRolDoWebExistemNoCatalogoOuSaoAliasDocumentados()
    {
        var faltando = RoleCodeConstants(typeof(PlantaoPro.Web.Security.RolesConstants))
            .Where(code => Catalog.Find(code) is null && !DocumentedAliases.Contains(code))
            .ToArray();
        Assert.Empty(faltando);
    }

    [Fact]
    public void ConstantesDeRolDaApiExistemNoCatalogoOuSaoAliasDocumentados()
    {
        var faltando = RoleCodeConstants(typeof(PlantaoPro.Api.RolesConstants))
            .Where(code => Catalog.Find(code) is null && !DocumentedAliases.Contains(code))
            .ToArray();
        Assert.Empty(faltando);
    }

    [Fact]
    public void TodosOsCodigosDoCatalogoTemConstanteNoWebENaApi()
    {
        var web = RoleCodeConstants(typeof(PlantaoPro.Web.Security.RolesConstants)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var api = RoleCodeConstants(typeof(PlantaoPro.Api.RolesConstants)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var role in Catalog.Roles)
        {
            Assert.True(web.Contains(role.Code), $"PlantaoPro.Web.Security.RolesConstants sem constante para {role.Code}.");
            Assert.True(api.Contains(role.Code), $"PlantaoPro.Api.RolesConstants sem constante para {role.Code}.");
        }
    }

    [Fact]
    public void RolesClinicasTmPrioridadeEstritamenteAbaixoDoParBaseCorrespondente()
    {
        void Abaixo(string clinica, string parBase)
        {
            var c = Catalog.Find(clinica) ?? throw new InvalidOperationException($"Role {clinica} ausente do catálogo.");
            var b = Catalog.Find(parBase) ?? throw new InvalidOperationException($"Role {parBase} ausente do catálogo.");
            Assert.True(c.Priority < b.Priority, $"{clinica} ({c.Priority}) deve ter prioridade estritamente menor que {parBase} ({b.Priority}).");
            Assert.Equal(AccessScopes.Tenant, c.Scope);
        }
        Abaixo("ADMINISTRADOR_CLINICA", "ADMINISTRADOR");
        Abaixo("COORDENADOR_CLINICO", "COORDENACAO");
        Abaixo("FINANCEIRO_CLINICA", "FINANCEIRO");
        Abaixo("FATURAMENTO_CONVENIO", "FINANCEIRO_CLINICA");
        Abaixo("ENFERMAGEM", "MEDICO");
        Abaixo("AUDITOR_CLINICO", "RECEPCAO");
    }

    [Fact]
    public void ResolverPreservaParBaseEmUsuariosComMultiplosPerfis()
    {
        var resolver = new PrimaryRoleResolver(Catalog);
        Assert.Equal("ADMINISTRADOR", resolver.Resolve(new[] { "ADMINISTRADOR", "ADMINISTRADOR_CLINICA" }));
        Assert.Equal("COORDENACAO", resolver.Resolve(new[] { "COORDENACAO", "COORDENADOR_CLINICO" }));
        Assert.Equal("MEDICO", resolver.Resolve(new[] { "MEDICO", "ENFERMAGEM" }));
        Assert.Equal("FINANCEIRO", resolver.Resolve(new[] { "FINANCEIRO", "FATURAMENTO_CONVENIO" }));
        // Perfil clínico isolado continua resolvendo para si próprio (comportamento anterior preservado).
        Assert.Equal("ADMINISTRADOR_CLINICA", resolver.Resolve(new[] { "ADMINISTRADOR_CLINICA" }));
        Assert.Equal("ENFERMAGEM", resolver.Resolve(new[] { "ENFERMAGEM" }));
    }
}

/// <summary>
/// O Login e o RefreshContext consomem o mesmo construtor de claims (SessionClaimsBuilder).
/// Estes testes fixam o contrato do conjunto de claims da sessão v2149: completeness
/// (incluindo access_catalog_version), normalização ':' -> '.' e status do cliente.
/// </summary>
public sealed class SessionClaimsBuilderTests
{
    private static readonly IRoleCatalog Catalog = new RoleCatalog();
    private static readonly IPrimaryRoleResolver Primary = new PrimaryRoleResolver(Catalog);
    private static readonly IAccessScopeResolver Scopes = new AccessScopeResolver(Catalog);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private static SessionLoginContext Ctx(string? sessionId = null) => new(
        UsuarioId,
        "Gestor Demo",
        "gestor@santacasa-demo.example",
        new[] { "ADMINISTRADOR_CLIENTE" },
        new[] { "adm360:estoque", "ADM360:VER" },
        new[] { "adm360" },
        null, null, false, null,
        Guid.NewGuid(),
        "Santa Casa Demonstração",
        "ativo",
        TenantId,
        "Santa Casa Demonstração",
        sessionId,
        "token-teste");

    [Fact]
    public void SessaoContemClaimsObrigatoriasDoCatalogoV2149()
    {
        var (principal, primaryRole, accessScope, contextMode, _) = SessionClaimsBuilder.Build(Ctx("sess-1"), Catalog, Primary, Scopes, "fallback");
        Assert.Equal("ADMINISTRADOR_CLIENTE", primaryRole);
        Assert.Equal(AccessScopes.Tenant, accessScope);
        Assert.Equal(AccessScopes.Tenant, contextMode);

        var claims = principal.Claims;
        Assert.Contains(claims, c => c.Type == "access_catalog_version" && c.Value == "v2149");
        Assert.Contains(claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == UsuarioId.ToString());
        Assert.Contains(claims, c => c.Type == "sub" && c.Value == UsuarioId.ToString());
        Assert.Contains(claims, c => c.Type == "email" && c.Value == "gestor@santacasa-demo.example");
        Assert.Contains(claims, c => c.Type == ClaimTypes.Role && c.Value == "ADMINISTRADOR_CLIENTE");
        Assert.Contains(claims, c => c.Type == "role" && c.Value == "ADMINISTRADOR_CLIENTE");
        Assert.Contains(claims, c => c.Type == "is_global_admin" && c.Value == "false");
        Assert.Contains(claims, c => c.Type == "cliente_status" && c.Value == "ATIVO");
        Assert.Contains(claims, c => c.Type == "cliente" && c.Value == "Santa Casa Demonstração");
        Assert.Contains(claims, c => c.Type == "tenant_id" && c.Value == TenantId.ToString());
        Assert.Contains(claims, c => c.Type == "session_id" && c.Value == "sess-1");
        // P8 (cookie ~14 KB): o JWT da API saiu do principal/cookie para caber no guideline de
        // ~4 KB por cookie (evita o chunking PlantaoPro.AuthC1..C3 do CookieManager do
        // framework). Ele segue disponível via sessão local (BaseWebController.GetJwtToken()).
        Assert.DoesNotContain(claims, c => c.Type == "jwt");
    }

    [Fact]
    public void PermissoesEModulosSaoNormalizadosComPontoComoSeparador()
    {
        var (principal, _, _, _, _) = SessionClaimsBuilder.Build(Ctx(), Catalog, Primary, Scopes, "fallback");
        // P8: permissões em uma única claim combinada (vírgula), normalizadas ':' -> '.'
        var combined = principal.Claims.FirstOrDefault(c => c.Type == "permissions")?.Value;
        var permissoes = (combined ?? string.Empty).Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries).OrderBy(v => v).ToArray();
        Assert.Equal(new[] { "ADM360.ESTOQUE", "ADM360.VER" }, permissoes);
        Assert.DoesNotContain(principal.Claims, c => c.Type == "permission");
        var modulos = principal.Claims.Where(c => c.Type == "module").Select(c => c.Value).ToArray();
        Assert.Equal(new[] { "ADM360" }, modulos);
    }

    [Fact]
    public void SemSessionIdDaApiUsaFallbackDaSessaoLocal()
    {
        var (principal, _, _, _, _) = SessionClaimsBuilder.Build(Ctx(sessionId: null), Catalog, Primary, Scopes, "sessao-local-42");
        Assert.Contains(principal.Claims, c => c.Type == "session_id" && c.Value == "sessao-local-42");
    }

    [Fact]
    public void GlobalAdminGeradoSemTenantEPrioridadeGlobal()
    {
        var ctx = Ctx() with { Roles = new[] { "ADMINISTRADOR_GLOBAL" }, TenantId = null, ContextMode = null };
        var (principal, primaryRole, accessScope, contextMode, _) = SessionClaimsBuilder.Build(ctx, Catalog, Primary, Scopes, "f");
        Assert.Equal("ADMINISTRADOR_GLOBAL", primaryRole);
        Assert.Equal(AccessScopes.Global, accessScope);
        Assert.Equal(AccessScopes.Global, contextMode);
        Assert.Contains(principal.Claims, c => c.Type == "is_global_admin" && c.Value == "true");
        Assert.DoesNotContain(principal.Claims, c => c.Type == "tenant_id");
    }
}
