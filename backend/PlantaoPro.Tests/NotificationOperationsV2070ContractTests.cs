using PlantaoPro.Api.Operation360.Notifications;

namespace PlantaoPro.Tests;

public sealed class NotificationOperationsV2070ContractTests
{
    [Fact]
    public void Central_filter_supports_operational_dimensions_and_empty_result()
    {
        var filter = new NotificationFilter("CHECKIN_PENDENTE", "OPERACAO", "ALTA", "NAO_LIDA", null, null, 25);
        Assert.Equal("OPERACAO", filter.Modulo);
        Assert.Equal("NAO_LIDA", filter.Status);
        Assert.Equal(25, filter.Limit);
    }

    [Fact]
    public void Dispatch_contract_requires_tenant_user_and_real_origin()
    {
        var tenant=Guid.NewGuid(); var user=Guid.NewGuid(); var origin=Guid.NewGuid();
        var notification = new DispatchNotification(tenant,user,"ESCALA","RISCO_COBERTURA","Cobertura em risco","Revise a escala.","CRITICA","WORK_ITEM",origin,"/MinhaCentral");
        Assert.Equal(tenant,notification.TenantId);
        Assert.Equal(user,notification.UsuarioId);
        Assert.Equal(origin,notification.OrigemId);
    }

    [Fact]
    public void Preference_contract_keeps_in_app_enabled()
    {
        var preference = new PlantaoPro.Api.Contracts.Notifications.NotificationPreferenceDto("FINANCEIRO","APROVACAO",true,false,false,false,true);
        Assert.True(preference.InApp);
        Assert.False(preference.Email);
        Assert.True(preference.Ativo);
    }

    [Fact]
    public void V2070_assets_enforce_tenant_filters_and_semantic_actions()
    {
        var root=RepositoryPathResolver.RepoRoot;
        var service=File.ReadAllText(Path.Combine(root,"backend/PlantaoPro.Api/Operation360/Notifications/NotificationService.cs"));
        var view=File.ReadAllText(Path.Combine(root,"backend/PlantaoPro.Web/Views/Notificacoes/Index.cshtml"));
        Assert.Contains("n.tenant_id=@tenantId",service);
        Assert.Contains("r.usuario_id=@userId",service);
        Assert.Contains("data-read-all",view);
        Assert.DoesNotContain("href=\"#\"",view);
    }
}
