using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed class ModuleContractingService
{
    private readonly IConfiguration configuration;
    private readonly ICurrentUserService currentUser;
    public ModuleContractingService(IConfiguration configuration, ICurrentUserService currentUser) { this.configuration = configuration; this.currentUser = currentUser; }

    public async Task<IReadOnlyList<CommercialModuleDto>> CatalogAsync(CancellationToken ct)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<ModuleRow>(new CommandDefinition(@"select m.id,m.codigo,m.nome,m.descricao,m.funcionalidades::text as funcionalidades,
 m.preco_base as preco, case when m.preco_base is null then null else 'MENSAL' end as periodicidade,
 case when upper(m.status)='ATIVO' and m.disponivel_comercialmente then 'DISPONIVEL' else 'INDISPONIVEL' end as disponibilidade,
 coalesce(tm.status,case when tm.habilitado then 'ATIVO' else 'SUSPENSO' end,'NAO_CONTRATADO') as estado_contratual,
 coalesce(array_agg(dep.codigo) filter(where dep.codigo is not null),array[]::text[]) as ""DependenciasArray""
from plantaopro.modulos_sistema m
left join plantaopro.tenant_modulos tm on tm.modulo_id=m.id and tm.tenant_id=@tenant and tm.reg_status='A'
left join plantaopro.modulo_catalogo_dependencias md on md.modulo_id=m.id and md.reg_status='A'
left join plantaopro.modulos_sistema dep on dep.id=md.modulo_dependencia_id
where m.reg_status='A' group by m.id,tm.status,tm.habilitado order by m.nome", new { tenant = currentUser.TenantId }, cancellationToken: ct));
        return rows.Select(Map).ToArray();
    }

    public async Task<ContractReviewDto> ReviewAsync(ContractReviewRequest request, CancellationToken ct)
    {
        RequireTenantAdmin();
        var ids = request.ModuloIds.Where(x => x != Guid.Empty).Distinct().OrderBy(x => x).ToArray();
        if (ids.Length == 0) throw new ArgumentException("Selecione ao menos um módulo.");
        var catalog = (await CatalogAsync(ct)).Where(x => ids.Contains(x.Id)).ToArray();
        if (catalog.Length != ids.Length || catalog.Any(x => x.Disponibilidade != "DISPONIVEL" || x.EstadoContratual is "ATIVO" or "SUSPENSO")) throw new InvalidOperationException("Um módulo não está disponível para nova contratação.");
        var selectedCodes = catalog.Select(x => x.Codigo).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = catalog.SelectMany(x => x.Dependencias).Where(x => !selectedCodes.Contains(x)).Distinct().ToArray();
        if (missing.Length > 0) throw new InvalidOperationException("Inclua as dependências: " + string.Join(", ", missing));
        var start = (request.InicioPrevisto ?? DateTimeOffset.UtcNow).ToUniversalTime();
        if (start < DateTimeOffset.UtcNow.AddMinutes(-1)) throw new ArgumentException("O início previsto não pode estar no passado.");
        var noPrice = catalog.Any(x => !x.Preco.HasValue);
        return new ContractReviewDto { ConditionsVersion = Version(currentUser.TenantId!.Value, start, catalog), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15), InicioPrevisto = start, Periodicidade = noPrice ? "PROPOSTA" : "MENSAL", Total = noPrice ? null : catalog.Sum(x => x.Preco), RequerProposta = noPrice, Itens = catalog };
    }

    public async Task<ContractRequestDto> ConfirmAsync(ConfirmContractRequest request, CancellationToken ct)
    {
        var review = await ReviewAsync(request, ct);
        if (!FixedEquals(review.ConditionsVersion, request.ConditionsVersion)) throw new ConditionsChangedException();
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey)) throw new ArgumentException("Chave de idempotência obrigatória.");
        await using var cn = Connection(); await cn.OpenAsync(ct); await using var tx = await cn.BeginTransactionAsync(ct);
        var id = Guid.NewGuid(); var tenant = currentUser.TenantId!.Value;
        var existing = await cn.ExecuteScalarAsync<Guid?>(new CommandDefinition("select id from plantaopro.solicitacoes_modulos where tenant_id=@tenant and chave_idempotencia=@key and reg_status='A'", new { tenant, key=request.IdempotencyKey.Trim() }, tx, cancellationToken:ct));
        if (existing.HasValue) { await tx.CommitAsync(ct); return (await GetAsync(existing.Value, cn, ct))!; }
        var inserted = await cn.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.solicitacoes_modulos(id,tenant_id,cliente_id,protocolo,status,versao_condicoes,total,periodicidade,inicio_previsto,chave_idempotencia,solicitado_por,reg_date,reg_status)
values(@id,@tenant,@cliente,'MOD-'||upper(substr(replace(@id::text,'-',''),1,12)),'PENDENTE',@version,@total,@period,@start,@key,@user,now(),'A')
on conflict (tenant_id,chave_idempotencia) where reg_status='A' do nothing", new {id,tenant,cliente=currentUser.ClienteId,version=review.ConditionsVersion,total=review.Total,period=review.Periodicidade,start=review.InicioPrevisto,key=request.IdempotencyKey.Trim(),user=currentUser.UserId},tx,cancellationToken:ct));
        if (inserted == 0) { var concurrent = await cn.ExecuteScalarAsync<Guid>(new CommandDefinition("select id from plantaopro.solicitacoes_modulos where tenant_id=@tenant and chave_idempotencia=@key and reg_status='A'",new{tenant,key=request.IdempotencyKey.Trim()},tx,cancellationToken:ct)); await tx.CommitAsync(ct); return (await GetAsync(concurrent,cn,ct))!; }
        foreach(var item in review.Itens) await cn.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.solicitacao_modulo_itens(id,solicitacao_id,modulo_id,codigo,nome,descricao,funcionalidades,dependencias,preco,periodicidade,reg_date,reg_status)
values(gen_random_uuid(),@id,@module,@code,@name,@description,@features::jsonb,@dependencies::jsonb,@price,@period,now(),'A')",new{id,module=item.Id,code=item.Codigo,name=item.Nome,description=item.Descricao,features=JsonSerializer.Serialize(item.Funcionalidades),dependencies=JsonSerializer.Serialize(item.Dependencias),price=item.Preco,period=item.Periodicidade},tx,cancellationToken:ct));
        await tx.CommitAsync(ct); return (await GetAsync(id, cn, ct))!;
    }

    public async Task<IReadOnlyList<ContractRequestDto>> ListAsync(string? status, Guid? tenant, CancellationToken ct)
    {
        var effectiveTenant=currentUser.IsGlobalAdmin()?tenant:currentUser.TenantId;
        if(!currentUser.IsGlobalAdmin()&&!currentUser.IsTenantAdmin()) throw new UnauthorizedAccessException();
        await using var cn=Connection();
        var ids=await cn.QueryAsync<Guid>(new CommandDefinition("select id from plantaopro.solicitacoes_modulos where reg_status='A' and (@tenant is null or tenant_id=@tenant) and (@status='' or status=@status) order by reg_date desc limit 200",new{tenant=effectiveTenant,status=(status??"").Trim().ToUpperInvariant()},cancellationToken:ct));
        var list=new List<ContractRequestDto>(); foreach(var id in ids){var dto=await GetAsync(id,cn,ct);if(dto is not null)list.Add(dto);} return list;
    }

    public async Task<ContractRequestDto> DecideAsync(Guid id,bool approve,ContractDecisionRequest request,CancellationToken ct)
    {
        if(!currentUser.IsGlobalAdmin()) throw new UnauthorizedAccessException();
        if(!approve&&string.IsNullOrWhiteSpace(request.Justificativa)) throw new ArgumentException("Justificativa obrigatória para recusa.");
        await using var cn=Connection();await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);
        var row=await cn.QueryFirstOrDefaultAsync<RequestRow>(new CommandDefinition("select id,tenant_id as TenantId,status,versao_condicoes as ConditionsVersion,inicio_previsto as InicioPrevisto from plantaopro.solicitacoes_modulos where id=@id and reg_status='A' for update",new{id},tx,cancellationToken:ct))??throw new KeyNotFoundException();
        if(row.Status is "APROVADA" or "RECUSADA") { await tx.CommitAsync(ct); return (await GetAsync(id,cn,ct))!; }
        if(row.Status!="PENDENTE"||!FixedEquals(row.ConditionsVersion,request.ConditionsVersion)) throw new ConditionsChangedException();
        var status=approve?"APROVADA":"RECUSADA";
        await cn.ExecuteAsync(new CommandDefinition("update plantaopro.solicitacoes_modulos set status=@status,justificativa=@reason,decidido_por=@user,decidido_em=now(),reg_update=now() where id=@id",new{id,status,reason=request.Justificativa?.Trim(),user=currentUser.UserId},tx,cancellationToken:ct));
        if(approve) await cn.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.tenant_modulos(id,tenant_id,modulo_id,codigo,codigo_modulo,habilitado,status,origem,preco_contratado,ativado_em,created_by,reg_date,reg_status)
select gen_random_uuid(),r.tenant_id,i.modulo_id,i.codigo,i.codigo,(r.inicio_previsto<=now()),case when r.inicio_previsto<=now() then 'ATIVO' else 'AGENDADO' end,'SOLICITACAO',i.preco,r.inicio_previsto,@user,now(),'A' from plantaopro.solicitacoes_modulos r join plantaopro.solicitacao_modulo_itens i on i.solicitacao_id=r.id and i.reg_status='A' where r.id=@id
on conflict (tenant_id,modulo_id) where reg_status='A' and modulo_id is not null do update set habilitado=excluded.habilitado,status=excluded.status,preco_contratado=excluded.preco_contratado,ativado_em=excluded.ativado_em,updated_by=@user,reg_update=now()",new{id,user=currentUser.UserId},tx,cancellationToken:ct));
        await tx.CommitAsync(ct);return (await GetAsync(id,cn,ct))!;
    }

    public async Task CancelAsync(Guid id,string reason,CancellationToken ct){RequireTenantAdmin();if(string.IsNullOrWhiteSpace(reason))throw new ArgumentException("Informe o motivo do cancelamento.");await using var cn=Connection();var changed=await cn.ExecuteAsync(new CommandDefinition("update plantaopro.solicitacoes_modulos set status='CANCELADA',justificativa=@reason,decidido_por=@user,decidido_em=now(),reg_update=now() where id=@id and tenant_id=@tenant and status='PENDENTE' and reg_status='A'",new{id,tenant=currentUser.TenantId,user=currentUser.UserId,reason=reason.Trim()},cancellationToken:ct));if(changed==0)throw new InvalidOperationException("Somente solicitação pendente do cliente pode ser cancelada.");}
    private async Task<ContractRequestDto?> GetAsync(Guid id,NpgsqlConnection cn,CancellationToken ct){var dto=await cn.QueryFirstOrDefaultAsync<ContractRequestDto>(new CommandDefinition(@"select r.id,r.protocolo,r.tenant_id as TenantId,coalesce(c.nome_fantasia,c.razao_social,'') as ClienteNome,r.status,r.versao_condicoes as ConditionsVersion,r.total,r.periodicidade,r.inicio_previsto as InicioPrevisto,r.reg_date as CriadoEm,r.decidido_em as DecididoEm,r.justificativa from plantaopro.solicitacoes_modulos r left join plantaopro.tenants t on t.id=r.tenant_id left join plantaopro.clientes c on c.id=t.cliente_id where r.id=@id and r.reg_status='A' and (@global or r.tenant_id=@tenant)",new{id,global=currentUser.IsGlobalAdmin(),tenant=currentUser.TenantId},cancellationToken:ct));if(dto is null)return null;var items=await cn.QueryAsync<ModuleRow>(new CommandDefinition(@"select modulo_id as id,codigo,nome,descricao,funcionalidades::text as funcionalidades,dependencias::text as dependencias,preco,periodicidade,'DISPONIVEL' as disponibilidade,'SOLICITADO' as estado_contratual from plantaopro.solicitacao_modulo_itens where solicitacao_id=@id and reg_status='A' order by nome",new{id},cancellationToken:ct));dto.Itens=items.Select(Map).ToArray();return dto;}
    private void RequireTenantAdmin(){if(!currentUser.IsTenantAdmin()||!currentUser.TenantId.HasValue)throw new UnauthorizedAccessException();}
    private NpgsqlConnection Connection()=>new(configuration.GetConnectionString("Default"));
    private static CommercialModuleDto Map(ModuleRow x)=>new(){Id=x.Id,Codigo=x.Codigo,Nome=x.Nome,Descricao=x.Descricao,Funcionalidades=Parse(x.Funcionalidades),Dependencias=x.DependenciasArray??Parse(x.Dependencias),Preco=x.Preco,Periodicidade=x.Periodicidade,Disponibilidade=x.Disponibilidade,EstadoContratual=x.EstadoContratual};
    private static string[] Parse(string? json){try{return JsonSerializer.Deserialize<string[]>(json??"[]")??Array.Empty<string>();}catch{return Array.Empty<string>();}}
    private static string Version(Guid tenant,DateTimeOffset start,IEnumerable<CommercialModuleDto> items){var canonical=$"{tenant:N}|{start:O}|"+string.Join("|",items.OrderBy(x=>x.Id).Select(x=>$"{x.Id:N}:{x.Preco?.ToString(System.Globalization.CultureInfo.InvariantCulture)??"PROPOSTA"}:{x.Periodicidade}"));return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();}
    private static bool FixedEquals(string a,string b){var x=Encoding.UTF8.GetBytes(a??"");var y=Encoding.UTF8.GetBytes(b??"");return x.Length==y.Length&&CryptographicOperations.FixedTimeEquals(x,y);}
    private sealed class ModuleRow{public Guid Id{get;set;}public string Codigo{get;set;}="";public string Nome{get;set;}="";public string Descricao{get;set;}="";public string? Funcionalidades{get;set;}public string? Dependencias{get;set;}public string[]? DependenciasArray{get;set;}public decimal? Preco{get;set;}public string? Periodicidade{get;set;}public string Disponibilidade{get;set;}="";public string EstadoContratual{get;set;}="";}
    private sealed class RequestRow{public Guid Id{get;set;}public Guid TenantId{get;set;}public string Status{get;set;}="";public string ConditionsVersion{get;set;}="";public DateTimeOffset InicioPrevisto{get;set;}}
}
public sealed class ConditionsChangedException:Exception{public ConditionsChangedException():base("As condições comerciais mudaram. Faça uma nova revisão antes de confirmar."){} }
