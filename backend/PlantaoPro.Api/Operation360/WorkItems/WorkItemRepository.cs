using Dapper;
using Npgsql;

namespace PlantaoPro.Api.Operation360.WorkItems;

public sealed class WorkItemRepository : IWorkItemRepository
{
    private readonly string connectionString;
    private const string Scope = "tenant_id=@tenantId and (@unitId is null or unidade_id=@unitId) and reg_status='A'";
    private const string Columns = "id, tipo, titulo, descricao, status, prioridade, responsavel_id as ResponsavelId, unidade_id as UnidadeId, posicao, vence_em as VenceEm, versao, criado_em as CriadoEm, atualizado_em as AtualizadoEm";

    public WorkItemRepository(IConfiguration configuration) => connectionString = configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada.");
    private NpgsqlConnection Open() => new(connectionString);

    public async Task<IReadOnlyList<WorkItemDto>> ListAsync(Guid tenantId, Guid? unitId, CancellationToken ct)
    {
        await using var cn = Open();
        var rows = await cn.QueryAsync<WorkItemDto>(new CommandDefinition($"select {Columns} from plantaopro.work_items where {Scope} order by status,posicao,vence_em nulls last", new { tenantId, unitId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<WorkItemDto?> GetAsync(Guid tenantId, Guid? unitId, Guid id, CancellationToken ct)
    {
        await using var cn = Open();
        return await GetAsync(cn, null, tenantId, unitId, id, ct);
    }

    public async Task<IReadOnlyList<WorkItemHistoryDto>> HistoryAsync(Guid tenantId, Guid? unitId, Guid id, CancellationToken ct)
    {
        await using var cn = Open();
        var sql = $"select h.id,h.acao,h.origem,h.destino,h.usuario_id as UsuarioId,h.criado_em as CriadoEm from plantaopro.work_item_history h join plantaopro.work_items w on w.id=h.work_item_id where w.{Scope} and w.id=@id order by h.criado_em desc";
        return (await cn.QueryAsync<WorkItemHistoryDto>(new CommandDefinition(sql, new { tenantId, unitId, id }, cancellationToken: ct))).AsList();
    }

    public Task<WorkItemMutationResult> MoveAsync(Guid tenantId, Guid? unitId, Guid userId, WorkItemMoveRequest r, CancellationToken ct) =>
        MutateAsync(tenantId, unitId, userId, r.ItemId, r.Version, r.IdempotencyKey, "MOVER", r.Source, r.Destination,
            "status=@destination,posicao=@position", new { destination = r.Destination, position = r.Position }, ct);

    public Task<WorkItemMutationResult> AssignAsync(Guid tenantId, Guid? unitId, Guid userId, Guid id, Guid responsibleId, WorkItemVersionRequest r, CancellationToken ct) =>
        MutateAsync(tenantId, unitId, userId, id, r.Version, r.IdempotencyKey, "ENCAMINHAR", null, WorkItemStatus.EmAndamento,
            "responsavel_id=@responsibleId,status='EM_ANDAMENTO'", new { responsibleId }, ct);

    public async Task<WorkItemMutationResult> CommentAsync(Guid tenantId, Guid? unitId, Guid userId, Guid id, WorkItemCommentRequest r, CancellationToken ct)
    {
        return await MutateAsync(tenantId, unitId, userId, id, r.Version, r.IdempotencyKey, "COMENTAR", null, null,
            "atualizado_em=now()", new { }, ct, async (cn, tx) => await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.work_item_comments(id,work_item_id,autor_id,comentario,criado_em) values(gen_random_uuid(),@id,@userId,@comment,now())", new { id, userId, comment = r.Comment.Trim() }, tx, cancellationToken: ct)));
    }

    public Task<WorkItemMutationResult> PostponeAsync(Guid tenantId, Guid? unitId, Guid userId, Guid id, WorkItemPostponeRequest r, CancellationToken ct) =>
        MutateAsync(tenantId, unitId, userId, id, r.Version, r.IdempotencyKey, "ADIAR", null, WorkItemStatus.Aguardando,
            "vence_em=@dueAt,status='AGUARDANDO'", new { dueAt = r.DueAt }, ct);

    private async Task<WorkItemMutationResult> MutateAsync(Guid tenantId, Guid? unitId, Guid userId, Guid id, int version, Guid key,
        string action, string? source, string? destination, string setters, object values, CancellationToken ct, Func<NpgsqlConnection, NpgsqlTransaction, Task>? extra = null)
    {
        await using var cn = Open(); await cn.OpenAsync(ct); await using var tx = await cn.BeginTransactionAsync(ct);
        var duplicate = await cn.ExecuteScalarAsync<bool>(new CommandDefinition("select exists(select 1 from plantaopro.operational_transition_history where tenant_id=@tenantId and idempotency_key=@key)", new { tenantId, key }, tx, cancellationToken: ct));
        if (duplicate) { await tx.RollbackAsync(ct); return new(true, false, true, await GetAsync(tenantId, unitId, id, ct)); }
        var parameters = new DynamicParameters(values); parameters.Add("tenantId", tenantId); parameters.Add("unitId", unitId); parameters.Add("id", id); parameters.Add("version", version);
        var affected = await cn.ExecuteAsync(new CommandDefinition($"update plantaopro.work_items set {setters},versao=versao+1,atualizado_em=now() where {Scope} and id=@id and versao=@version", parameters, tx, cancellationToken: ct));
        if (affected == 0) { var exists = await GetAsync(cn, tx, tenantId, unitId, id, ct); await tx.RollbackAsync(ct); return new(exists != null, exists != null, false, exists); }
        if (extra != null) await extra(cn, tx);
        await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.work_item_history(id,work_item_id,usuario_id,acao,origem,destino,criado_em) values(gen_random_uuid(),@id,@userId,@action,@source,@destination,now())", new { id, userId, action, source, destination }, tx, cancellationToken: ct));
        await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.operational_transition_history(id,tenant_id,unidade_id,entidade,entidade_id,origem,destino,usuario_id,versao,idempotency_key) values(gen_random_uuid(),@tenantId,@unitId,'WORK_ITEM',@id,@source,@destination,@userId,@version,@key)", new { tenantId, unitId, id, source = source ?? action, destination = destination ?? action, userId, version, key }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct); return new(true, false, false, await GetAsync(tenantId, unitId, id, ct));
    }

    private static Task<WorkItemDto?> GetAsync(NpgsqlConnection cn, NpgsqlTransaction? tx, Guid tenantId, Guid? unitId, Guid id, CancellationToken ct) =>
        cn.QuerySingleOrDefaultAsync<WorkItemDto>(new CommandDefinition($"select {Columns} from plantaopro.work_items where {Scope} and id=@id", new { tenantId, unitId, id }, tx, cancellationToken: ct));
}
