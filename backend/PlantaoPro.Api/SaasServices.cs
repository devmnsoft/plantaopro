using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed class ClienteService
{
    private readonly IConfiguration cfg;
    private readonly IAuditService audit;
    private readonly ILogger<ClienteService> logger;

    public ClienteService(IConfiguration cfg, IAuditService audit, ILogger<ClienteService> logger)
    {
        this.cfg = cfg;
        this.audit = audit;
        this.logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<ClienteDto>>> ListAsync()
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var rows = await cn.QueryAsync<ClienteDto>(@"select id,razao_social as RazaoSocial,nome_fantasia as NomeFantasia,cnpj,email,telefone,cidade,estado,plano_id as PlanoId,status,reg_status as RegStatus,reg_date as RegDate,reg_update as RegUpdate from plantaopro.clientes where reg_status='A' order by nome_fantasia");
            return ApiResponse<IEnumerable<ClienteDto>>.Ok(rows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao listar clientes");
            return ApiResponse<IEnumerable<ClienteDto>>.Fail("Não foi possível listar clientes no momento.", 500);
        }
    }

    public async Task<ApiResponse<ClienteCentralPageDto>> ListCentralAsync(string? search, string? status, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 10, 100);
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        status = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToUpperInvariant();
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            const string filtered = @"from plantaopro.clientes c
where c.reg_status='A'
  and (@search is null or c.nome_fantasia ilike '%'||@search||'%' or c.razao_social ilike '%'||@search||'%' or c.cnpj ilike '%'||@search||'%')
  and (@status is null or upper(c.status)=@status)";
            var args = new { search, status, take = pageSize, skip = (page - 1) * pageSize };
            var sql = @"select c.id as ""Id"",coalesce(c.razao_social,'') as ""RazaoSocial"",coalesce(c.nome_fantasia,c.razao_social,'') as ""NomeFantasia"",
coalesce(c.cnpj,'') as ""Cnpj"",coalesce(c.cidade,'') as ""Cidade"",coalesce(c.estado,'') as ""Estado"",coalesce(c.status,'') as ""Status"",
(select count(distinct u.id) from plantaopro.usuarios u left join plantaopro.tenants tu on tu.id=u.tenant_id where u.reg_status='A' and coalesce(u.status,'ATIVO')='ATIVO' and coalesce(u.cliente_id,tu.cliente_id,u.tenant_id)=c.id) as ""UsuariosAtivos"",
(select count(*) from plantaopro.tenant_onboarding_checklist oc left join plantaopro.tenants ot on ot.id=oc.tenant_id where oc.reg_status='A' and oc.status='PENDENTE' and coalesce(oc.cliente_id,ot.cliente_id)=c.id) as ""Pendencias"",
coalesce((select string_agg(distinct coalesce(m.nome,tm.codigo_modulo,tm.codigo),' · ' order by coalesce(m.nome,tm.codigo_modulo,tm.codigo)) from plantaopro.tenants t join plantaopro.tenant_modulos tm on tm.tenant_id=t.id and tm.reg_status='A' and tm.habilitado=true and tm.status='ATIVO' left join plantaopro.modulos_sistema m on m.id=tm.modulo_id where t.cliente_id=c.id and tm.ativado_em is not null and tm.ativado_em<=now() and tm.desativado_em is null),'') as ""ModulosVigentes""
" + filtered + @" order by coalesce(c.nome_fantasia,c.razao_social),c.id limit @take offset @skip;
select count(*) as ""Clientes"",count(*) filter(where upper(c.status)='ATIVO') as ""Ativos"",count(*) filter(where upper(c.status)='SUSPENSO') as ""Suspensos"",
coalesce(sum((select count(distinct u.id) from plantaopro.usuarios u left join plantaopro.tenants tu on tu.id=u.tenant_id where u.reg_status='A' and coalesce(u.status,'ATIVO')='ATIVO' and coalesce(u.cliente_id,tu.cliente_id,u.tenant_id)=c.id)),0) as ""UsuariosAtivos"",
coalesce(sum((select count(*) from plantaopro.tenant_onboarding_checklist oc left join plantaopro.tenants ot on ot.id=oc.tenant_id where oc.reg_status='A' and oc.status='PENDENTE' and coalesce(oc.cliente_id,ot.cliente_id)=c.id)),0) as ""Pendencias"" " + filtered + ";";
            using var multi = await cn.QueryMultipleAsync(sql, args);
            var items = (await multi.ReadAsync<ClienteCentralDto>()).AsList();
            var totals = await multi.ReadSingleAsync<ClienteCentralTotais>();
            return ApiResponse<ClienteCentralPageDto>.Ok(new(items, page, pageSize, totals.Clientes, totals));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao listar central global de clientes");
            return ApiResponse<ClienteCentralPageDto>.Fail("Não foi possível carregar a Central de Clientes.", 500);
        }
    }

    public async Task<ApiResponse<string>> AlterarStatusAsync(Guid id, AlterarStatusClienteRequest request, Guid? actorId, string? ip, string perfil)
    {
        var action = request.Acao?.Trim().ToUpperInvariant();
        var target = action switch { "SUSPENDER" => "SUSPENSO", "REATIVAR" => "ATIVO", "CANCELAR" => "CANCELADO", _ => null };
        if (target is null) return ApiResponse<string>.Fail("Ação administrativa inválida.", 422);
        if (string.IsNullOrWhiteSpace(request.Motivo)) return ApiResponse<string>.Fail("Informe o motivo e o alcance da alteração.", 422);
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();
        var before = await cn.QuerySingleOrDefaultAsync<string>("select status from plantaopro.clientes where id=@id and reg_status='A' for update", new { id }, tx);
        if (before is null) return ApiResponse<string>.Fail("Cliente não encontrado.", 404);
        if (!string.Equals(before, target, StringComparison.OrdinalIgnoreCase))
            await cn.ExecuteAsync("update plantaopro.clientes set status=@target,reg_update=now() where id=@id and reg_status='A'", new { id, target }, tx);
        await tx.CommitAsync();
        await audit.RegistrarAsync(actorId, id, "CLIENTE", id, action!, new { antes = before, depois = target, motivo = request.Motivo.Trim(), resultado = "SUCESSO" }, true, ip, perfil);
        return ApiResponse<string>.Ok(target, before == target ? "Cliente já estava nesse estado; nenhuma transição foi duplicada." : "Situação administrativa atualizada.");
    }

    public async Task<ApiResponse<ClienteDto>> GetAsync(Guid id)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var row = await cn.QueryFirstOrDefaultAsync<ClienteDto>(@"select id,razao_social as RazaoSocial,nome_fantasia as NomeFantasia,cnpj,email,telefone,cidade,estado,plano_id as PlanoId,status,reg_status as RegStatus,reg_date as RegDate,reg_update as RegUpdate from plantaopro.clientes where id=@id", new { id });
            return row is null ? ApiResponse<ClienteDto>.Fail("Cliente não encontrado.",404) : ApiResponse<ClienteDto>.Ok(row);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao detalhar cliente");
            return ApiResponse<ClienteDto>.Fail("Não foi possível detalhar o cliente.", 500);
        }
    }
}
