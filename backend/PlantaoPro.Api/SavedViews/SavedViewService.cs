using PlantaoPro.Api.Data;

namespace PlantaoPro.Api.SavedViews;

public sealed class SavedViewService : ISavedViewService
{
    private readonly ISavedViewRepository repository; private readonly ICurrentUserService currentUser; private readonly IAuditService audit;
    public SavedViewService(ISavedViewRepository repository,ICurrentUserService currentUser,IAuditService audit){this.repository=repository;this.currentUser=currentUser;this.audit=audit;}
    public Task<IReadOnlyList<SavedViewDto>> ListAsync(string module, CancellationToken ct)
    {
        var context = Context();
        return repository.ListAsync(context.Tenant, context.User, SavedViewValidation.Module(module), ct);
    }

    public async Task<SavedViewDto> CreateAsync(SaveSavedViewRequest request, CancellationToken ct)
    {
        var context=Context(); var module=SavedViewValidation.Module(request.Module); var name=SavedViewValidation.Name(request.Name);
        var filters=SavedViewValidation.Json(request.Filters,module,false); var sort=Sort(request.Sort,module);
        var result=await repository.CreateAsync(context.Tenant,context.User,module,name,SavedViewValidation.NormalizedName(name),filters,sort,request.IsDefault,ct);
        await Audit(context,result.Id,"SAVED_VIEW_CREATE",new{module,result.Name},ct); return result;
    }

    public async Task<SavedViewDto?> UpdateAsync(Guid id, UpdateSavedViewRequest request, CancellationToken ct)
    {
        var context=Context(); var current=await repository.GetAsync(context.Tenant,context.User,id,ct); if(current is null)return null;
        var name=SavedViewValidation.Name(request.Name); var filters=SavedViewValidation.Json(request.Filters,current.Module,false); var sort=Sort(request.Sort,current.Module);
        var result=await repository.UpdateAsync(context.Tenant,context.User,id,name,SavedViewValidation.NormalizedName(name),filters,sort,request.IsDefault,ct);
        if(result is not null)await Audit(context,id,"SAVED_VIEW_UPDATE",new{result.Module,result.Name},ct); return result;
    }

    public async Task<bool> DeleteAsync(Guid id,CancellationToken ct){var c=Context();var deleted=await repository.DeleteAsync(c.Tenant,c.User,id,ct);if(deleted)await Audit(c,id,"SAVED_VIEW_DELETE",null,ct);return deleted;}
    public async Task<SavedViewDto?> SetDefaultAsync(Guid id,CancellationToken ct){var c=Context();var result=await repository.SetDefaultAsync(c.Tenant,c.User,id,ct);if(result is not null)await Audit(c,id,"SAVED_VIEW_UPDATE",new{result.Module,isDefault=true},ct);return result;}

    private static string? Sort(System.Text.Json.JsonElement? value,string module)=>!value.HasValue || value.Value.ValueKind == System.Text.Json.JsonValueKind.Null ? null : SavedViewValidation.Json(value.Value,module,true);
    private (Guid Tenant,Guid User) Context()=>currentUser.IsAuthenticated() && currentUser.UserId is Guid user && (currentUser.TenantId??currentUser.ClienteId) is Guid tenant && tenant!=Guid.Empty ? (tenant,user) : throw new UnauthorizedAccessException("Contexto de usuário e tenant é obrigatório.");
    private Task Audit((Guid Tenant,Guid User)c,Guid id,string action,object? details,CancellationToken ct)=>audit.RegistrarAsync(c.User,c.Tenant,"SAVED_VIEW",id,action,details,true,null,string.Join(',',currentUser.Roles),ct);
}
