using Dapper;
using Npgsql;

namespace PlantaoPro.Api.SavedViews;

public sealed class SavedViewRepository : ISavedViewRepository
{
    private readonly IConfiguration configuration; public SavedViewRepository(IConfiguration configuration)=>this.configuration=configuration;
    private const string Select = @"
        select id, module, name, filters_json::text FiltersJson, sort_json::text SortJson,
               is_default IsDefault, created_at CreatedAt, updated_at UpdatedAt
        from plantaopro.saved_views
        ";
    private NpgsqlConnection Connection() => new(configuration.GetConnectionString("Default"));

    public async Task<IReadOnlyList<SavedViewDto>> ListAsync(Guid tenantId, Guid userId, string module, CancellationToken ct)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<Row>(new CommandDefinition(Select + " where tenant_id=@tenantId and user_id=@userId and module=@module order by is_default desc, name", new { tenantId, userId, module }, cancellationToken: ct));
        return rows.Select(Map).ToArray();
    }

    public async Task<SavedViewDto?> GetAsync(Guid tenantId, Guid userId, Guid id, CancellationToken ct)
    {
        await using var cn = Connection();
        return Map(await cn.QuerySingleOrDefaultAsync<Row>(new CommandDefinition(Select + " where tenant_id=@tenantId and user_id=@userId and id=@id", new { tenantId, userId, id }, cancellationToken: ct)));
    }

    public async Task<SavedViewDto> CreateAsync(Guid tenantId, Guid userId, string module, string name, string normalizedName, string filtersJson, string? sortJson, bool isDefault, CancellationToken ct)
    {
        await using var cn = Connection(); await cn.OpenAsync(ct); await using var tx = await cn.BeginTransactionAsync(ct);
        try {
            if (isDefault) await ClearDefault(cn, tx, tenantId, userId, module, ct);
            var row = await cn.QuerySingleAsync<Row>(new CommandDefinition(@"
                insert into plantaopro.saved_views(tenant_id,user_id,module,name,normalized_name,filters_json,sort_json,is_default)
                values(@tenantId,@userId,@module,@name,@normalizedName,cast(@filtersJson as jsonb),cast(@sortJson as jsonb),@isDefault)
                returning id,module,name,filters_json::text FiltersJson,sort_json::text SortJson,is_default IsDefault,created_at CreatedAt,updated_at UpdatedAt
                ", new { tenantId, userId, module, name, normalizedName, filtersJson, sortJson, isDefault }, tx, cancellationToken: ct));
            await tx.CommitAsync(ct); return Map(row)!;
        } catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation) { await tx.RollbackAsync(ct); throw new SavedViewConflictException("Já existe uma visão com esse nome no módulo."); }
    }

    public async Task<SavedViewDto?> UpdateAsync(Guid tenantId, Guid userId, Guid id, string name, string normalizedName, string filtersJson, string? sortJson, bool isDefault, CancellationToken ct)
    {
        await using var cn = Connection(); await cn.OpenAsync(ct); await using var tx = await cn.BeginTransactionAsync(ct);
        try {
            var module = await cn.QuerySingleOrDefaultAsync<string>(new CommandDefinition("select module from plantaopro.saved_views where id=@id and tenant_id=@tenantId and user_id=@userId for update", new { id, tenantId, userId }, tx, cancellationToken: ct));
            if (module is null) { await tx.RollbackAsync(ct); return null; }
            if (isDefault) await ClearDefault(cn, tx, tenantId, userId, module, ct);
            var row = await cn.QuerySingleAsync<Row>(new CommandDefinition(@"
                update plantaopro.saved_views set name=@name,normalized_name=@normalizedName,filters_json=cast(@filtersJson as jsonb),sort_json=cast(@sortJson as jsonb),is_default=@isDefault,updated_at=now()
                where id=@id and tenant_id=@tenantId and user_id=@userId
                returning id,module,name,filters_json::text FiltersJson,sort_json::text SortJson,is_default IsDefault,created_at CreatedAt,updated_at UpdatedAt
                ", new { id, tenantId, userId, name, normalizedName, filtersJson, sortJson, isDefault }, tx, cancellationToken: ct));
            await tx.CommitAsync(ct); return Map(row);
        } catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation) { await tx.RollbackAsync(ct); throw new SavedViewConflictException("Já existe uma visão com esse nome no módulo."); }
    }

    public async Task<bool> DeleteAsync(Guid tenantId, Guid userId, Guid id, CancellationToken ct) { await using var cn=Connection(); return await cn.ExecuteAsync(new CommandDefinition("delete from plantaopro.saved_views where id=@id and tenant_id=@tenantId and user_id=@userId",new{id,tenantId,userId},cancellationToken:ct)) == 1; }

    public async Task<SavedViewDto?> SetDefaultAsync(Guid tenantId, Guid userId, Guid id, CancellationToken ct)
    {
        await using var cn=Connection(); await cn.OpenAsync(ct); await using var tx=await cn.BeginTransactionAsync(ct);
        try {
            var module=await cn.QuerySingleOrDefaultAsync<string>(new CommandDefinition("select module from plantaopro.saved_views where id=@id and tenant_id=@tenantId and user_id=@userId for update",new{id,tenantId,userId},tx,cancellationToken:ct));
            if(module is null){await tx.RollbackAsync(ct);return null;} await ClearDefault(cn,tx,tenantId,userId,module,ct);
            await cn.ExecuteAsync(new CommandDefinition("update plantaopro.saved_views set is_default=true,updated_at=now() where id=@id",new{id},tx,cancellationToken:ct));
            var row=await cn.QuerySingleAsync<Row>(new CommandDefinition(Select + " where id=@id",new{id},tx,cancellationToken:ct));
            await tx.CommitAsync(ct); return Map(row);
        } catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation) { await tx.RollbackAsync(ct); throw new SavedViewConflictException("Outra visão já foi definida como padrão."); }
    }

    private static Task<int> ClearDefault(NpgsqlConnection cn,NpgsqlTransaction tx,Guid tenantId,Guid userId,string module,CancellationToken ct)=>cn.ExecuteAsync(new CommandDefinition("update plantaopro.saved_views set is_default=false,updated_at=now() where tenant_id=@tenantId and user_id=@userId and module=@module and is_default",new{tenantId,userId,module},tx,cancellationToken:ct));
    private static SavedViewDto? Map(Row? row) { if(row is null)return null; using var f=System.Text.Json.JsonDocument.Parse(row.FiltersJson); System.Text.Json.JsonElement? sort=null; if(row.SortJson is not null){using var s=System.Text.Json.JsonDocument.Parse(row.SortJson);sort=s.RootElement.Clone();} return new(row.Id,row.Module,row.Name,f.RootElement.Clone(),sort,row.IsDefault,row.CreatedAt,row.UpdatedAt); }
    private sealed class Row { public Guid Id{get;set;} public string Module{get;set;}=""; public string Name{get;set;}=""; public string FiltersJson{get;set;}="{}"; public string? SortJson{get;set;} public bool IsDefault{get;set;} public DateTime CreatedAt{get;set;} public DateTime UpdatedAt{get;set;} }
}
