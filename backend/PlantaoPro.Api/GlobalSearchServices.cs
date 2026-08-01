using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed record GlobalSearchItem(
    Guid Id,
    string Type,
    string Title,
    string? Subtitle,
    string Route,
    string Icon);

public sealed record GlobalSearchResponse(string Query, IReadOnlyList<GlobalSearchItem> Items);

public interface IGlobalSearchRepository
{
    Task<IReadOnlyList<GlobalSearchItem>> SearchAsync(
        Guid clienteId,
        string query,
        int limit,
        bool includePatients,
        CancellationToken cancellationToken);
}

public sealed class GlobalSearchRepository : IGlobalSearchRepository
{
    private readonly string connectionString;

    public GlobalSearchRepository(IConfiguration configuration)
    {
        connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada.");
    }

    public async Task<IReadOnlyList<GlobalSearchItem>> SearchAsync(
        Guid clienteId,
        string query,
        int limit,
        bool includePatients,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            select id as Id, tipo as Type, titulo as Title, subtitulo as Subtitle,
                   rota as Route, icone as Icon
            from (
                select m.id, 'MEDICO' as tipo, m.nome as titulo,
                       concat_ws(' · ', m.crm, m.uf_crm) as subtitulo,
                       '/Medicos/Details/' || m.id as rota, 'bi-person-badge' as icone, 20 as ordem
                  from plantaopro.medicos m
                 where m.cliente_id = @clienteId and m.reg_status = 'A'
                   and (m.nome ilike @pattern or coalesce(m.crm, '') ilike @pattern)
                union all
                select u.id, 'UNIDADE', u.nome, null,
                       '/HospitalArea' , 'bi-building', 30
                  from plantaopro.unidades u
                 where u.cliente_id = @clienteId and u.reg_status = 'A' and u.nome ilike @pattern
                union all
                select p.id, 'PLANTAO', coalesce(e.nome, 'Plantão'),
                       to_char(p.data_inicio, 'DD/MM/YYYY HH24:MI'),
                       '/Plantoes/Details/' || p.id, 'bi-calendar2-pulse', 40
                  from plantaopro.plantoes p
                  left join plantaopro.especialidades e on e.id = p.especialidade_id
                 where p.cliente_id = @clienteId and p.reg_status = 'A'
                   and (coalesce(e.nome, '') ilike @pattern or to_char(p.data_inicio, 'DD/MM/YYYY') ilike @pattern)
                union all
                select p.id, 'PACIENTE', p.nome, null,
                       '/Pacientes/Details/' || p.id, 'bi-person-heart', 10
                  from plantaopro.pacientes p
                 where @includePatients and p.cliente_id = @clienteId and p.reg_status = 'A'
                   and p.nome ilike @pattern
            ) results
            order by ordem, titulo
            limit @limit;
        ";

        await using var connection = new NpgsqlConnection(connectionString);
        var command = new CommandDefinition(
            sql,
            new { clienteId, pattern = $"%{query}%", limit, includePatients },
            cancellationToken: cancellationToken);
        return (await connection.QueryAsync<GlobalSearchItem>(command)).AsList();
    }
}

public interface IGlobalSearchService
{
    Task<ApiResponse<GlobalSearchResponse>> SearchAsync(string? query, int limit, CancellationToken cancellationToken);
}

public sealed class GlobalSearchService : IGlobalSearchService
{
    private readonly IGlobalSearchRepository repository;
    private readonly ICurrentUserService currentUser;
    private readonly IPermissionService permissions;

    public GlobalSearchService(
        IGlobalSearchRepository repository,
        ICurrentUserService currentUser,
        IPermissionService permissions)
    {
        this.repository = repository;
        this.currentUser = currentUser;
        this.permissions = permissions;
    }

    public async Task<ApiResponse<GlobalSearchResponse>> SearchAsync(
        string? query,
        int limit,
        CancellationToken cancellationToken)
    {
        var normalized = (query ?? string.Empty).Trim();
        if (normalized.Length < 2)
            return ApiResponse<GlobalSearchResponse>.Fail("Informe ao menos dois caracteres para pesquisar.", 400);

        var tenant = currentUser.ClienteId ?? currentUser.TenantId;
        if (tenant is not Guid clienteId)
            return ApiResponse<GlobalSearchResponse>.Fail("Selecione uma organização para pesquisar.", 403);

        var safeLimit = Math.Clamp(limit, 1, 20);
        var items = await repository.SearchAsync(
            clienteId,
            normalized,
            safeLimit,
            permissions.CanViewSensitiveData(),
            cancellationToken);
        return ApiResponse<GlobalSearchResponse>.Ok(new GlobalSearchResponse(normalized, items));
    }
}
