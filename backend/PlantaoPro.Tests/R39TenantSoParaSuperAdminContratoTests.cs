using System.Text;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R39 (diretriz r24a1, epic E2 parte 2 — "tenant visível apenas para super-admin"; fechamento do E2).
/// Fato 1 trava que o backoffice AdminSaas continua restrito a papéis de plataforma (o atributo
/// [Authorize] logo acima da classe não pode conter AdministradorCliente). Fato 2 trava que NENHUM
/// .cshtml fora de AdminSaas/ e fora da whitelist de keeps classificados na R38 contenha
/// "tenant"/"tenants" em qualquer linha — uma tela institucional nova ou futura com o termo falha.
/// Revisita de D-R24-3: docs/evidencias/2026-10-18-rodada-39/r39a0-*.md.
/// </summary>
public class R39TenantSoParaSuperAdminContratoTests
{
    // Keeps classificados na R38: código não renderizado (claims/variáveis/Model.Tenant*/CSS),
    // zonas isGlobal/super-admin, nota técnica de analista e marketing. AdminSaas/ fica fora do scan inteiro.
    private static readonly string[] WhitelistDeKeeps =
    {
        "Account\\Login.cshtml",
        "Administrativo360\\Gestao\\Dashboard.cshtml",
        "AssistenteIa\\Index.cshtml",
        "Clientes\\Details.cshtml",
        "Clientes\\Index.cshtml",
        "CommandCenter\\Index.cshtml",
        "CommercialDemoWeb\\Landing.cshtml",
        "Convite\\Aceitar.cshtml",
        "DashboardsPremium\\Perfil.cshtml",
        "Fase2Operational\\Dashboard.cshtml",
        "Home\\Dashboard.cshtml",
        "MeuDia\\Index.cshtml",
        "Modulos\\Tenant.cshtml",
        "Saude360\\Modulo.cshtml",
        "Shared\\_AppSidebar.cshtml",
        "Shared\\_AppTopbar.cshtml",
        "Shared\\_ContextBar.cshtml",
        "Shared\\_GuidedJourney.cshtml",
        "Shared\\_PageContextHeader.cshtml",
        "Shared\\_ScreenGuide.cshtml",
        "Shared\\_TenantContextBadge.cshtml",
        "Shared\\_UserMenu.cshtml",
        "Shared\\SaasComercialPage.cshtml",
        "Usuarios\\Form.cshtml",
        "Usuarios\\Index.cshtml",
    };

    [Fact]
    public void BackofficeAdminSaas_NaoAceitaAdminDaInstituicao()
    {
        var p = Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Controllers", "CommercialDemoWebController.cs");
        var texto = File.ReadAllText(p, Encoding.UTF8);
        var idx = texto.IndexOf("class AdminSaasController", StringComparison.Ordinal);
        Assert.True(idx > 400, "marcador da classe não encontrado");
        var cabeca = texto.Substring(idx - 400, 400);
        Assert.Contains("[Authorize", cabeca, StringComparison.Ordinal);
        Assert.Contains("AdministradorGlobal", cabeca, StringComparison.Ordinal);
        Assert.DoesNotContain("AdministradorCliente", cabeca, StringComparison.Ordinal);
    }

    [Fact]
    public void TelasForaDoBackoffice_NaoContemTenantForaDaWhitelist()
    {
        var views = Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Views");
        var ofensores = new List<string>();
        foreach (var arquivo in Directory.GetFiles(views, "*.cshtml", SearchOption.AllDirectories))
        {
            var rel = arquivo.Substring(views.Length + 1).Replace('/', '\\');
            if (rel.StartsWith("AdminSaas\\", StringComparison.Ordinal)) continue;
            if (WhitelistDeKeeps.Contains(rel)) continue;
            var texto = File.ReadAllText(arquivo, Encoding.UTF8);
            if (texto.IndexOf("tenant", StringComparison.OrdinalIgnoreCase) >= 0) ofensores.Add(rel);
        }
        Assert.Empty(ofensores);
    }
}
