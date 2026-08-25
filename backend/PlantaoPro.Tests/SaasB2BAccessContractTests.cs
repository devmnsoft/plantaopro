using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using PlantaoPro.Api;
using PlantaoPro.Api.Controllers;
using PlantaoPro.Api.Models;
using PlantaoPro.Api.Security;

namespace PlantaoPro.Tests;

public sealed class SaasB2BAccessContractTests
{
    [Fact]
    public void Catalog_uses_predictable_permissions_and_required_modules()
    {
        Assert.True(SaasPermissions.IsKnown("tenants.read"));
        Assert.True(SaasPermissions.IsKnown("white_label.manage"));
        Assert.Contains("FINANCE", SaasModules.All);
        Assert.DoesNotContain(SaasPermissions.All, value => value.Contains(':'));
    }

    [Fact]
    public void Tenant_admin_cannot_manage_global_tenants_or_plans()
    {
        Assert.False(SaasProfilePermissions.Allows(RolesConstants.TenantAdmin, "tenants.manage"));
        Assert.False(SaasProfilePermissions.Allows(RolesConstants.TenantAdmin, "plans.manage"));
        Assert.True(SaasProfilePermissions.Allows(RolesConstants.TenantAdmin, "users.manage"));
    }

    [Fact]
    public void Auditor_and_support_are_read_only()
    {
        Assert.True(SaasProfilePermissions.Allows(RolesConstants.AuditorRole, "audit.read"));
        Assert.False(SaasProfilePermissions.Allows(RolesConstants.AuditorRole, "finance.manage"));
        Assert.True(SaasProfilePermissions.Allows(RolesConstants.Support, "tenants.read"));
        Assert.False(SaasProfilePermissions.Allows(RolesConstants.Support, "users.manage"));
    }

    [Fact]
    public void White_label_rejects_low_contrast_and_markup_and_keeps_default_fallback()
    {
        var fallback = new WhiteLabelConfiguracaoDto();
        Assert.Null(WhiteLabelSecurityValidator.Validate(fallback));
        fallback.CorPrimaria = "#ffffff";
        fallback.CorFundo = "#ffffff";
        Assert.Contains("contraste", WhiteLabelSecurityValidator.Validate(fallback), StringComparison.OrdinalIgnoreCase);
        fallback = new WhiteLabelConfiguracaoDto { NomePlataforma = "<script>alert(1)</script>" };
        Assert.Contains("HTML", WhiteLabelSecurityValidator.Validate(fallback));
    }

    [Theory]
    [InlineData("text/html", 100, "https://cdn.example/logo.png")]
    [InlineData("image/png", 3_000_000, "https://cdn.example/logo.png")]
    [InlineData("image/png", 100, "javascript:alert(1)")]
    public void White_label_rejects_unsafe_assets(string type, long size, string url) =>
        Assert.NotNull(WhiteLabelSecurityValidator.ValidateAsset(type, size, url));

    [Fact]
    public void White_label_mutations_require_administrator_role()
    {
        var method = typeof(WhiteLabelController).GetMethod("Salvar")!;
        var roles = method.GetCustomAttribute<AuthorizeAttribute>()?.Roles ?? string.Empty;
        Assert.Contains(RolesConstants.PlatformAdmin, roles);
        Assert.Contains(RolesConstants.TenantAdmin, roles);
        Assert.DoesNotContain(RolesConstants.Professional, roles);
    }
}
