using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed class SaasModuleDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal PrecoBase { get; set; }
    public bool Essencial { get; set; }
    public string Status { get; set; } = string.Empty;
    public string FuncionalidadesJson { get; set; } = "[]";
    public int? LimitePadrao { get; set; }
    public long ClientesAtivos { get; set; }
    public bool Contratado { get; set; }
    public bool Habilitado { get; set; }
    public decimal? PrecoContratado { get; set; }
    public int? LimiteContratado { get; set; }
}

public sealed class SaasModuleUpsertRequest
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Categoria { get; set; } = "OPERACAO";
    public decimal PrecoBase { get; set; }
    public bool Essencial { get; set; }
    public string Status { get; set; } = "ATIVO";
    public string[] Funcionalidades { get; set; } = Array.Empty<string>();
    public int? LimitePadrao { get; set; }
}

public sealed class TenantModuleContractRequest
{
    public Guid TenantId { get; set; }
    public decimal? PrecoContratado { get; set; }
    public int? LimiteContratado { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

public sealed class PlanModuleContractRequest
{
    public Guid PlanoId { get; set; }
    public bool Incluido { get; set; } = true;
    public decimal? PrecoAdicional { get; set; }
    public int? Limite { get; set; }
}

public sealed class SaasModuleCatalogService
{
    private static readonly Regex CodePattern = new("^[A-Z][A-Z0-9_]{1,79}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly IConfiguration configuration;
    private readonly ICurrentUserService currentUser;
    private readonly IAuditService audit;

    public SaasModuleCatalogService(IConfiguration configuration, ICurrentUserService currentUser, IAuditService audit)
    {
        this.configuration = configuration;
        this.currentUser = currentUser;
        this.audit = audit;
    }

    public async Task<ApiResponse<IEnumerable<SaasModuleDto>>> ListAsync(Guid? requestedTenantId, CancellationToken ct)
    {
        var tenantId = TenantScope(requestedTenantId);
        if (!currentUser.IsGlobalAdmin() && !tenantId.HasValue)
            return ApiResponse<IEnumerable<SaasModuleDto>>.Fail("Tenant não identificado para consultar os módulos contratados.", 403);
        const string sql = @"select m.id as ""Id"", m.codigo as ""Codigo"", m.nome as ""Nome"",
coalesce(m.descricao,'') as ""Descricao"", coalesce(m.categoria,'OPERACAO') as ""Categoria"",
coalesce(m.preco_base,0) as ""PrecoBase"", coalesce(m.essencial,false) as ""Essencial"",
coalesce(m.status,'ATIVO') as ""Status"", coalesce(m.funcionalidades,'[]'::jsonb)::text as ""FuncionalidadesJson"",
m.limite_padrao as ""LimitePadrao"",
(select count(distinct tm.tenant_id) from plantaopro.tenant_modulos tm where tm.modulo_id=m.id and tm.reg_status='A' and tm.habilitado=true and upper(coalesce(tm.status,'ATIVO'))='ATIVO') as ""ClientesAtivos"",
coalesce(tm.id is not null,false) as ""Contratado"", coalesce(tm.habilitado,false) as ""Habilitado"",
tm.preco_contratado as ""PrecoContratado"", tm.limite_contratado as ""LimiteContratado""
from plantaopro.modulos_sistema m
left join plantaopro.tenant_modulos tm on tm.modulo_id=m.id and tm.tenant_id=@tenantId and tm.reg_status='A'
where m.reg_status='A'
order by m.essencial desc, m.ordem, m.nome";
        await using var connection = Connection();
        var rows = await connection.QueryAsync<SaasModuleDto>(new CommandDefinition(sql, new { tenantId }, cancellationToken: ct));
        return ApiResponse<IEnumerable<SaasModuleDto>>.Ok(rows);
    }

    public async Task<ApiResponse<SaasModuleDto>> GetAsync(Guid id, Guid? requestedTenantId, CancellationToken ct)
    {
        var list = await ListAsync(requestedTenantId, ct);
        var item = list.Data?.FirstOrDefault(module => module.Id == id);
        return item is null ? ApiResponse<SaasModuleDto>.Fail("Módulo não encontrado.", 404) : ApiResponse<SaasModuleDto>.Ok(item);
    }

    public async Task<ApiResponse<Guid>> SaveAsync(Guid? id, SaasModuleUpsertRequest request, string? ip, CancellationToken ct)
    {
        if (!currentUser.IsGlobalAdmin()) return ApiResponse<Guid>.Fail("Somente o Super Administrador MNSOFT pode alterar o catálogo e os preços.", 403);
        var errors = Validate(request);
        if (errors.Count > 0) return ApiResponse<Guid>.Fail("Revise os dados do módulo.", 400, errors);

        var moduleId = id ?? Guid.NewGuid();
        var code = NormalizeCode(request.Codigo);
        var status = NormalizeStatus(request.Status);
        await using var connection = Connection();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var before = id.HasValue
            ? await connection.QuerySingleOrDefaultAsync<SaasModuleDto>(new CommandDefinition(@"select id as ""Id"", codigo as ""Codigo"", nome as ""Nome"", coalesce(descricao,'') as ""Descricao"", coalesce(categoria,'OPERACAO') as ""Categoria"", coalesce(preco_base,0) as ""PrecoBase"", coalesce(essencial,false) as ""Essencial"", coalesce(status,'ATIVO') as ""Status"", coalesce(funcionalidades,'[]'::jsonb)::text as ""FuncionalidadesJson"", limite_padrao as ""LimitePadrao"" from plantaopro.modulos_sistema where id=@moduleId and reg_status='A'", new { moduleId }, transaction, cancellationToken: ct))
            : null;
        if (id.HasValue && before is null)
        {
            await transaction.RollbackAsync(ct);
            return ApiResponse<Guid>.Fail("Módulo não encontrado.", 404);
        }

        const string sql = @"insert into plantaopro.modulos_sistema(id,codigo,nome,descricao,categoria,preco_base,essencial,status,funcionalidades,limite_padrao,reg_status,reg_date,created_by)
values(@moduleId,@code,@nome,@descricao,@categoria,@precoBase,@essencial,@status,cast(@funcionalidades as jsonb),@limitePadrao,'A',now(),@userId)
on conflict (id) do update set codigo=excluded.codigo,nome=excluded.nome,descricao=excluded.descricao,categoria=excluded.categoria,
preco_base=excluded.preco_base,essencial=excluded.essencial,status=excluded.status,funcionalidades=excluded.funcionalidades,
limite_padrao=excluded.limite_padrao,reg_update=now(),updated_by=@userId";
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            moduleId,
            code,
            nome = request.Nome.Trim(),
            descricao = request.Descricao.Trim(),
            categoria = NormalizeCode(request.Categoria),
            request.PrecoBase,
            request.Essencial,
            status,
            funcionalidades = JsonSerializer.Serialize(request.Funcionalidades.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct()),
            request.LimitePadrao,
            userId = currentUser.UserId
        }, transaction, cancellationToken: ct));
        await transaction.CommitAsync(ct);
        await audit.RegistrarAsync(currentUser.UserId, null, "MODULO_SAAS", moduleId, id.HasValue ? "EDITAR" : "CRIAR", new { antes = before, depois = new { code, request.Nome, request.PrecoBase, request.Essencial, status } }, true, ip, "ADMINISTRADOR_GLOBAL", ct);
        return ApiResponse<Guid>.Ok(moduleId, id.HasValue ? "Módulo atualizado." : "Módulo criado.");
    }

    public async Task<ApiResponse<Guid>> ToggleTenantAsync(Guid moduleId, TenantModuleContractRequest request, bool enabled, string? ip, CancellationToken ct)
    {
        if (!currentUser.IsGlobalAdmin()) return ApiResponse<Guid>.Fail("Somente o Super Administrador MNSOFT pode alterar a contratação.", 403);
        if (request.TenantId == Guid.Empty || string.IsNullOrWhiteSpace(request.Motivo)) return ApiResponse<Guid>.Fail("Cliente e motivo são obrigatórios.", 400);
        if (request.PrecoContratado < 0 || request.LimiteContratado < 0) return ApiResponse<Guid>.Fail("Preço e limite não podem ser negativos.", 400);

        await using var connection = Connection();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var module = await connection.QuerySingleOrDefaultAsync<SaasModuleDto>(new CommandDefinition("select id as \"Id\", codigo as \"Codigo\", nome as \"Nome\" from plantaopro.modulos_sistema where id=@moduleId and reg_status='A'", new { moduleId }, transaction, cancellationToken: ct));
        var tenantExists = await connection.QuerySingleAsync<bool>(new CommandDefinition("select exists(select 1 from plantaopro.clientes where id=@tenantId and reg_status='A')", new { tenantId = request.TenantId }, transaction, cancellationToken: ct));
        if (module is null || !tenantExists)
        {
            await transaction.RollbackAsync(ct);
            return ApiResponse<Guid>.Fail("Cliente ou módulo não encontrado.", 404);
        }

        var before = await connection.QuerySingleOrDefaultAsync<object>(new CommandDefinition("select id,habilitado,status,preco_contratado,limite_contratado from plantaopro.tenant_modulos where tenant_id=@tenantId and modulo_id=@moduleId and reg_status='A'", new { tenantId = request.TenantId, moduleId }, transaction, cancellationToken: ct));
        var contractId = await connection.QuerySingleAsync<Guid>(new CommandDefinition(@"insert into plantaopro.tenant_modulos(id,tenant_id,modulo_id,codigo_modulo,codigo,nome,habilitado,origem,status,preco_contratado,limite_contratado,ativado_em,desativado_em,reg_status,reg_date,created_by)
values(gen_random_uuid(),@tenantId,@moduleId,@code,@code,@name,@enabled,'CONTRATO',@status,@price,@limit,case when @enabled then now() else null end,case when @enabled then null else now() end,'A',now(),@userId)
on conflict (tenant_id,modulo_id) where reg_status='A' do update set habilitado=@enabled,status=@status,preco_contratado=@price,limite_contratado=@limit,
codigo_modulo=@code,codigo=@code,nome=@name,ativado_em=case when @enabled then now() else plantaopro.tenant_modulos.ativado_em end,
desativado_em=case when @enabled then null else now() end,reg_update=now(),updated_by=@userId returning id", new
        {
            tenantId = request.TenantId,
            moduleId,
            code = module.Codigo,
            name = module.Nome,
            enabled,
            status = enabled ? "ATIVO" : "BLOQUEADO",
            price = request.PrecoContratado,
            limit = request.LimiteContratado,
            userId = currentUser.UserId
        }, transaction, cancellationToken: ct));
        var after = new { habilitado = enabled, status = enabled ? "ATIVO" : "BLOQUEADO", request.PrecoContratado, request.LimiteContratado, request.Motivo };
        await connection.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.tenant_modulos_historico(tenant_modulo_id,tenant_id,modulo_id,acao,antes,depois,usuario_id,ip_origem)
values(@contractId,@tenantId,@moduleId,@action,cast(@before as jsonb),cast(@after as jsonb),@userId,@ip)", new
        {
            contractId,
            tenantId = request.TenantId,
            moduleId,
            action = enabled ? "HABILITAR" : "DESABILITAR",
            before = before is null ? null : JsonSerializer.Serialize(before),
            after = JsonSerializer.Serialize(after),
            userId = currentUser.UserId,
            ip
        }, transaction, cancellationToken: ct));
        await transaction.CommitAsync(ct);
        await audit.RegistrarAsync(currentUser.UserId, request.TenantId, "TENANT_MODULO", contractId, enabled ? "HABILITAR" : "DESABILITAR", new { moduleId, module.Codigo, antes = before, depois = after }, true, ip, "ADMINISTRADOR_GLOBAL", ct);
        return ApiResponse<Guid>.Ok(contractId, enabled ? "Módulo habilitado para o cliente." : "Módulo bloqueado para o cliente.");
    }

    public async Task<ApiResponse<Guid>> LinkPlanAsync(Guid moduleId, PlanModuleContractRequest request, string? ip, CancellationToken ct)
    {
        if (!currentUser.IsGlobalAdmin()) return ApiResponse<Guid>.Fail("Somente o Super Administrador MNSOFT pode alterar planos.", 403);
        if (request.PlanoId == Guid.Empty || request.PrecoAdicional < 0 || request.Limite < 0) return ApiResponse<Guid>.Fail("Plano, preço e limite inválidos.", 400);
        await using var connection = Connection();
        var id = await connection.QuerySingleOrDefaultAsync<Guid>(new CommandDefinition(@"insert into plantaopro.plano_modulos(id,plano_id,modulo_id,incluido,limite,preco_adicional,reg_status,reg_date,created_by)
select gen_random_uuid(),@planoId,@moduleId,@incluido,@limite,@preco,'A',now(),@userId
where exists(select 1 from plantaopro.planos where id=@planoId and reg_status='A') and exists(select 1 from plantaopro.modulos_sistema where id=@moduleId and reg_status='A')
on conflict (plano_id,modulo_id) where reg_status='A' do update set incluido=@incluido,limite=@limite,preco_adicional=@preco,reg_update=now(),updated_by=@userId returning id", new { planoId = request.PlanoId, moduleId, request.Incluido, request.Limite, preco = request.PrecoAdicional, userId = currentUser.UserId }, cancellationToken: ct));
        if (id == Guid.Empty) return ApiResponse<Guid>.Fail("Plano ou módulo não encontrado.", 404);
        await audit.RegistrarAsync(currentUser.UserId, null, "PLANO_MODULO", id, "VINCULAR", new { request.PlanoId, moduleId, request.Incluido, request.PrecoAdicional, request.Limite }, true, ip, "ADMINISTRADOR_GLOBAL", ct);
        return ApiResponse<Guid>.Ok(id, "Módulo vinculado ao plano.");
    }

    private Guid? TenantScope(Guid? requestedTenantId) => currentUser.IsGlobalAdmin() ? requestedTenantId : currentUser.TenantId;
    private NpgsqlConnection Connection() => new(configuration.GetConnectionString("Default"));
    private static string NormalizeCode(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant().Replace(' ', '_');
    private static string NormalizeStatus(string? value) => string.Equals(value, "INATIVO", StringComparison.OrdinalIgnoreCase) ? "INATIVO" : "ATIVO";
    private static List<string> Validate(SaasModuleUpsertRequest request)
    {
        var errors = new List<string>();
        if (!CodePattern.IsMatch(NormalizeCode(request.Codigo))) errors.Add("Código deve usar letras maiúsculas, números e sublinhado.");
        if (string.IsNullOrWhiteSpace(request.Nome) || request.Nome.Trim().Length > 160) errors.Add("Nome é obrigatório e deve ter até 160 caracteres.");
        if (request.Descricao?.Length > 1200) errors.Add("Descrição deve ter até 1200 caracteres.");
        if (request.PrecoBase < 0) errors.Add("Preço base não pode ser negativo.");
        if (request.LimitePadrao < 0) errors.Add("Limite padrão não pode ser negativo.");
        return errors;
    }
}
