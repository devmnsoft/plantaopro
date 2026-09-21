using Dapper;
using Npgsql;
using PlantaoPro.Domain.Ocorrencias;

namespace PlantaoPro.Api;

public sealed class OcorrenciaService
{
    private readonly IConfiguration configuration;
    private readonly ICurrentUserService currentUser;
    public OcorrenciaService(IConfiguration configuration, ICurrentUserService currentUser)
    { this.configuration = configuration; this.currentUser = currentUser; }
    private Guid Tenant => currentUser.TenantId ?? throw new UnauthorizedAccessException("Contexto da organização obrigatório.");
    private Guid UserId => currentUser.UserId ?? throw new UnauthorizedAccessException("Identidade inválida.");
    private bool Gestor => currentUser.IsTenantAdmin() || currentUser.IsGlobalAdmin() || currentUser.HasRole("COORDENADOR");
    private NpgsqlConnection Connection() => new(configuration.GetConnectionString("Default"));

    public async Task<OcorrenciaDto> CriarAsync(CriarOcorrenciaRequest request, CancellationToken ct)
    {
        var titulo = request.Titulo?.Trim() ?? string.Empty;
        var descricao = request.Descricao?.Trim() ?? string.Empty;
        if (request.UnidadeId == Guid.Empty) throw new ArgumentException("Selecione uma unidade.");
        if (titulo.Length is < 4 or > 160) throw new ArgumentException("O título deve ter entre 4 e 160 caracteres.");
        if (descricao.Length is < 10 or > 4000) throw new ArgumentException("A descrição deve ter entre 10 e 4000 caracteres.");
        var categoria = Normalize(request.Categoria); var prioridade = Normalize(request.Prioridade);
        if (!OcorrenciaWorkflow.Categorias.Contains(categoria)) throw new ArgumentException("Categoria operacional inválida.");
        if (!OcorrenciaWorkflow.Prioridades.Contains(prioridade)) throw new ArgumentException("Prioridade inválida.");
        await using var cn = Connection(); await cn.OpenAsync(ct); await using var tx = await cn.BeginTransactionAsync(ct);
        var contextoValido = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"select exists(
select 1 from plantaopro.hospitais h where h.id=@unidade and h.cliente_id=@tenant and h.reg_status='A'
and (@plantao is null or exists(select 1 from plantaopro.plantoes p where p.id=@plantao and p.hospital_id=h.id and p.cliente_id=@tenant and p.reg_status='A')))", new { unidade=request.UnidadeId, plantao=request.PlantaoId, tenant=Tenant }, tx, cancellationToken:ct));
        if (!contextoValido) throw new UnauthorizedAccessException("Unidade ou plantão fora do escopo autorizado.");
        var id = Guid.NewGuid();
        await cn.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.ocorrencias_operacionais(id,tenant_id,unidade_id,plantao_id,titulo,descricao,categoria,prioridade,solicitante_id)
values(@id,@tenant,@unidade,@plantao,@titulo,@descricao,@categoria,@prioridade,@user)", new { id,tenant=Tenant,unidade=request.UnidadeId,plantao=request.PlantaoId,titulo,descricao,categoria,prioridade,user=UserId },tx,cancellationToken:ct));
        await Evento(cn,tx,id,"CRIADA","Ocorrência registrada e classificada.",true,ct);
        await tx.CommitAsync(ct); return (await ObterAsync(id,ct))!;
    }

    public async Task<OcorrenciaPagina> ListarAsync(OcorrenciaFiltro f, CancellationToken ct)
    {
        f.Pagina=Math.Max(1,f.Pagina); f.Tamanho=Math.Clamp(f.Tamanho,1,100); var offset=(f.Pagina-1)*f.Tamanho;
        var p=new {tenant=Tenant,user=UserId,gestor=Gestor,q=string.IsNullOrWhiteSpace(f.Pesquisa)?null:$"%{f.Pesquisa.Trim()}%",f.UnidadeId,f.ResponsavelId,categoria=NormalizeNullable(f.Categoria),situacao=NormalizeNullable(f.Situacao),f.Minhas,f.De,f.Ate,offset,f.Tamanho};
        const string where=@"o.tenant_id=@tenant and o.reg_status='A'
and (@gestor or o.solicitante_id=@user or o.responsavel_id=@user)
and (@q is null or o.titulo ilike @q or o.descricao ilike @q)
and (@UnidadeId is null or o.unidade_id=@UnidadeId)
and (@ResponsavelId is null or o.responsavel_id=@ResponsavelId)
and (@categoria is null or o.categoria=@categoria)
and (@situacao is null or o.situacao=@situacao)
and (not @Minhas or o.solicitante_id=@user or o.responsavel_id=@user)
and (@De is null or o.criado_em>=@De) and (@Ate is null or o.criado_em<@Ate)";
        await using var cn=Connection();
	        var itens=(await cn.QueryAsync<OcorrenciaDto>(new CommandDefinition($@"select id as ""Id"",tenant_id as ""TenantId"",unidade_id as ""UnidadeId"",plantao_id as ""PlantaoId"",titulo as ""Titulo"",descricao as ""Descricao"",categoria as ""Categoria"",prioridade as ""Prioridade"",situacao as ""Situacao"",solicitante_id as ""SolicitanteId"",responsavel_id as ""ResponsavelId"",prazo_resolucao as ""PrazoResolucao"",resolucao as ""Resolucao"",versao as ""Versao"",criado_em as ""CriadoEm"",atualizado_em as ""AtualizadoEm"" from plantaopro.ocorrencias_operacionais o where {where} order by o.atualizado_em desc,o.id limit @Tamanho offset @offset",p,cancellationToken:ct))).AsList();
	        var counts=await cn.QuerySingleAsync<Counts>(new CommandDefinition($@"select count(*) as ""Total"",count(*) filter(where situacao not in ('RESOLVIDA','CANCELADA')) as ""Abertas"",count(*) filter(where responsavel_id is null and situacao not in ('RESOLVIDA','CANCELADA')) as ""SemResponsavel"",count(*) filter(where situacao='EM_ATENDIMENTO') as ""EmAtendimento"",count(*) filter(where situacao='RESOLVIDA' and (@De is null or atualizado_em>=@De) and (@Ate is null or atualizado_em<@Ate)) as ""ResolvidasPeriodo"",count(*) filter(where prazo_resolucao is not null and prazo_resolucao<now() and situacao not in ('RESOLVIDA','CANCELADA')) as ""Vencidas"" from plantaopro.ocorrencias_operacionais o where {where}",p,cancellationToken:ct));
        return new(itens,counts.Total,counts.Abertas,counts.SemResponsavel,counts.EmAtendimento,counts.ResolvidasPeriodo,counts.Vencidas,f.Pagina,f.Tamanho);
    }

    public async Task<OcorrenciaDto?> ObterAsync(Guid id,CancellationToken ct)
	    { await using var cn=Connection(); return await cn.QueryFirstOrDefaultAsync<OcorrenciaDto>(new CommandDefinition(@"select id as ""Id"",tenant_id as ""TenantId"",unidade_id as ""UnidadeId"",plantao_id as ""PlantaoId"",titulo as ""Titulo"",descricao as ""Descricao"",categoria as ""Categoria"",prioridade as ""Prioridade"",situacao as ""Situacao"",solicitante_id as ""SolicitanteId"",responsavel_id as ""ResponsavelId"",prazo_resolucao as ""PrazoResolucao"",resolucao as ""Resolucao"",versao as ""Versao"",criado_em as ""CriadoEm"",atualizado_em as ""AtualizadoEm"" from plantaopro.ocorrencias_operacionais where id=@id and tenant_id=@tenant and reg_status='A' and (@gestor or solicitante_id=@user or responsavel_id=@user)",new{id,tenant=Tenant,user=UserId,gestor=Gestor},cancellationToken:ct)); }

    public async Task AtribuirAsync(Guid id,AtribuirOcorrenciaRequest r,CancellationToken ct)
    {
        if(!Gestor)throw new UnauthorizedAccessException(); await using var cn=Connection(); await cn.OpenAsync(ct); await using var tx=await cn.BeginTransactionAsync(ct);
        if(r.ResponsavelId==Guid.Empty || r.Versao<=0)throw new ArgumentException("Selecione um responsável e informe a versão atual da ocorrência.");
        var habilitado=await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"select exists(
select 1 from plantaopro.usuarios u
where u.id=@id and u.reg_status='A' and coalesce(u.status,'ATIVO')='ATIVO'
and (coalesce(u.tenant_id,u.cliente_id)=@tenant or exists(
 select 1 from plantaopro.usuario_tenant_acessos uta where uta.usuario_id=u.id and uta.tenant_id=@tenant
 and uta.reg_status='A' and uta.status='ATIVO' and (uta.acesso_inicio is null or uta.acesso_inicio<=now())
 and (uta.acesso_fim is null or uta.acesso_fim>now())))",new{id=r.ResponsavelId,tenant=Tenant},tx,cancellationToken:ct));
        if(!habilitado)throw new ArgumentException("Responsável inativo ou fora da organização.");
        var changed=await cn.ExecuteAsync(new CommandDefinition("update plantaopro.ocorrencias_operacionais set responsavel_id=@responsavel,situacao=case when situacao='ABERTA' then 'EM_ATENDIMENTO' else situacao end,versao=versao+1,atualizado_em=now() where id=@id and tenant_id=@tenant and versao=@versao and situacao not in ('RESOLVIDA','CANCELADA') and reg_status='A'",new{id,tenant=Tenant,responsavel=r.ResponsavelId,versao=r.Versao},tx,cancellationToken:ct));
        if(changed==0)throw new OcorrenciaConcurrencyException(); await Evento(cn,tx,id,"ATRIBUIDA","Responsável atribuído.",true,ct); await tx.CommitAsync(ct);
    }

    public async Task TransicionarAsync(Guid id,TransicionarOcorrenciaRequest r,CancellationToken ct)
    {
        var destino=Normalize(r.Situacao);
        if(r.Versao<=0)throw new ArgumentException("Informe a versão atual da ocorrência.");
        await using var cn=Connection(); await cn.OpenAsync(ct); await using var tx=await cn.BeginTransactionAsync(ct);
        var atual=await cn.QueryFirstOrDefaultAsync<StateRow>(new CommandDefinition("select situacao as \"Situacao\",responsavel_id as \"ResponsavelId\" from plantaopro.ocorrencias_operacionais where id=@id and tenant_id=@tenant and versao=@versao and reg_status='A' for update",new{id,tenant=Tenant,versao=r.Versao},tx,cancellationToken:ct))??throw new OcorrenciaConcurrencyException();
        if(!Gestor&&atual.ResponsavelId!=UserId)throw new UnauthorizedAccessException();
        if(!OcorrenciaWorkflow.PodeTransicionar(atual.Situacao,destino,Gestor))throw new InvalidOperationException("Transição não permitida.");
        if(destino is ("RESOLVIDA" or "CANCELADA") && string.IsNullOrWhiteSpace(r.Descricao))throw new ArgumentException(destino=="RESOLVIDA"?"Descreva a providência adotada.":"Informe o motivo do cancelamento.");
        var changed=await cn.ExecuteAsync(new CommandDefinition("update plantaopro.ocorrencias_operacionais set situacao=@destino,resolucao=case when @destino='RESOLVIDA' then @descricao else resolucao end,cancelamento_motivo=case when @destino='CANCELADA' then @descricao else cancelamento_motivo end,versao=versao+1,atualizado_em=now() where id=@id and tenant_id=@tenant and versao=@versao and reg_status='A'",new{id,tenant=Tenant,versao=r.Versao,destino,descricao=r.Descricao?.Trim()},tx,cancellationToken:ct));
        if(changed!=1)throw new OcorrenciaConcurrencyException();
        await Evento(cn,tx,id,destino,destino=="RESOLVIDA"?$"Providência: {r.Descricao!.Trim()}":r.Descricao?.Trim()??$"Situação alterada para {destino}.",true,ct); await tx.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<OcorrenciaEventoDto>> HistoricoAsync(Guid id,CancellationToken ct)
    { if(await ObterAsync(id,ct) is null)throw new KeyNotFoundException(); await using var cn=Connection(); return (await cn.QueryAsync<OcorrenciaEventoDto>(new CommandDefinition(@"select id as ""Id"",tipo as ""Tipo"",autor_id as ""AutorId"",descricao as ""Descricao"",visivel_solicitante as ""VisivelSolicitante"",criado_em as ""CriadoEm"" from plantaopro.ocorrencia_eventos where tenant_id=@tenant and ocorrencia_id=@id and reg_status='A' and (@gestor or visivel_solicitante) order by criado_em,id",new{id,tenant=Tenant,gestor=Gestor},cancellationToken:ct))).AsList(); }
    private async Task Evento(NpgsqlConnection cn,NpgsqlTransaction tx,Guid id,string tipo,string descricao,bool visivel,CancellationToken ct)=>await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.ocorrencia_eventos(tenant_id,ocorrencia_id,tipo,autor_id,descricao,visivel_solicitante) values(@tenant,@id,@tipo,@user,@descricao,@visivel)",new{tenant=Tenant,id,tipo,user=UserId,descricao,visivel},tx,cancellationToken:ct));
    private static string Normalize(string? value)=>(value??"").Trim().ToUpperInvariant(); private static string? NormalizeNullable(string? value)=>string.IsNullOrWhiteSpace(value)?null:Normalize(value);
    private sealed class Counts { public long Total{get;set;} public long Abertas{get;set;} public long SemResponsavel{get;set;} public long EmAtendimento{get;set;} public long ResolvidasPeriodo{get;set;} public long Vencidas{get;set;} }
    private sealed class StateRow { public string Situacao{get;set;}=""; public Guid? ResponsavelId{get;set;} }
}
public sealed class OcorrenciaConcurrencyException : Exception
{
    public OcorrenciaConcurrencyException() : base("A ocorrência foi alterada por outro usuário. Atualize antes de repetir.") { }
}
