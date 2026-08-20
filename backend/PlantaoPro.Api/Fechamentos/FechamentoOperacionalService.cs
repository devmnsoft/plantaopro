using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using PlantaoPro.Domain.Financeiro;

namespace PlantaoPro.Api.Fechamentos;

public sealed class FechamentoOperacionalService
{
    private static readonly HashSet<string> TiposDivergencia = new(StringComparer.OrdinalIgnoreCase) { "HORAS", "VALOR", "PRESENCA", "AUSENCIA", "SUBSTITUICAO", "ESCALA", "OUTRO" };
    private readonly IConfiguration configuration; private readonly ICurrentUserService current; private readonly IAuditService audit;
    private readonly NotificacaoService notificacoes; private readonly ILogger<FechamentoOperacionalService> logger;
    public FechamentoOperacionalService(IConfiguration configuration, ICurrentUserService current, IAuditService audit, NotificacaoService notificacoes, ILogger<FechamentoOperacionalService> logger)
    { this.configuration = configuration; this.current = current; this.audit = audit; this.notificacoes = notificacoes; this.logger = logger; }

    private NpgsqlConnection Connection() => new(configuration.GetConnectionString("Default"));
    private (Guid Tenant, Guid Cliente, Guid Usuario) Contexto() => (current.TenantId ?? throw new UnauthorizedAccessException(), current.ClienteId ?? throw new UnauthorizedAccessException(), current.UserId ?? throw new UnauthorizedAccessException());

    private const string ResumoSql = @"
        select f.id as ""Id"",f.plantao_id as ""PlantaoId"",coalesce(h.nome_fantasia,'') as ""Hospital"",
        coalesce(es.nome,'') as ""Especialidade"",p.data_inicio as ""Inicio"",p.data_fim as ""Fim"",f.status as ""Status"",
        f.valor_previsto as ""ValorPrevisto"",f.valor_apurado as ""ValorApurado"",f.horas_previstas as ""HorasPrevistas"",
        f.horas_realizadas as ""HorasRealizadas"",(select count(*)::int from plantaopro.fechamento_plantao_escalas i where i.tenant_id=f.tenant_id and i.fechamento_id=f.id) as ""QuantidadeEscalas"",
        (select count(*)::int from plantaopro.fechamento_divergencias d where d.tenant_id=f.tenant_id and d.fechamento_id=f.id and d.status='ABERTA') as ""DivergenciasAbertas"",
        f.iniciado_em as ""CriadoEm"" from plantaopro.fechamento_plantao f join plantaopro.plantoes p on p.id=f.plantao_id
        join plantaopro.hospitais h on h.id=p.hospital_id join plantaopro.especialidades es on es.id=p.especialidade_id
        ";

    public async Task<ApiResponse<IReadOnlyList<FechamentoResumoDto>>> ListarAsync(bool pendentes, CancellationToken ct)
    {
        var c = Contexto(); await using var cn = Connection();
        var sql = ResumoSql + " where f.tenant_id=@Tenant and f.cliente_id=@Cliente" + (pendentes ? " and f.status not in ('CONCLUIDO','CANCELADO')" : "") + " order by f.iniciado_em desc limit 200";
        var rows = (await cn.QueryAsync<FechamentoResumoDto>(new CommandDefinition(sql, c, cancellationToken: ct))).AsList();
        return ApiResponse<IReadOnlyList<FechamentoResumoDto>>.Ok(rows);
    }

    public async Task<ApiResponse<FechamentoDetalheDto>> ObterAsync(Guid id, CancellationToken ct)
    {
        var c = Contexto(); await using var cn = Connection();
        var header = await cn.QueryFirstOrDefaultAsync<FechamentoDetalheDto>(new CommandDefinition(ResumoSql + " where f.id=@id and f.tenant_id=@Tenant and f.cliente_id=@Cliente", new { id, c.Tenant, c.Cliente }, cancellationToken: ct));
        if (header is null) return ApiResponse<FechamentoDetalheDto>.Fail("Fechamento não encontrado.", 404);
        header.Itens = (await cn.QueryAsync<FechamentoItemDto>(new CommandDefinition(@"
            select i.id as ""Id"",i.escala_id as ""EscalaId"",i.medico_id as ""MedicoId"",coalesce(m.nome,'') as ""Medico"",coalesce(m.crm,'') as ""Crm"",
            i.status_escala as ""StatusEscala"",i.inicio_previsto as ""InicioPrevisto"",i.fim_previsto as ""FimPrevisto"",i.horas_previstas as ""HorasPrevistas"",
            i.horas_realizadas as ""HorasRealizadas"",i.valor_previsto as ""ValorPrevisto"",i.valor_calculado as ""ValorApurado"",i.possui_divergencia as ""PossuiDivergencia"",
            o.pagamento_id as ""PagamentoId"",pg.status as ""PagamentoStatus"" from plantaopro.fechamento_plantao_escalas i join plantaopro.medicos m on m.id=i.medico_id
            left join plantaopro.financeiro_pagamento_origem o on o.tenant_id=i.tenant_id and o.fechamento_id=i.fechamento_id and o.escala_id=i.escala_id
            left join plantaopro.pagamentos pg on pg.id=o.pagamento_id where i.tenant_id=@Tenant and i.fechamento_id=@id order by i.inicio_previsto,m.nome
            ", new { id, c.Tenant }, cancellationToken: ct))).AsList();
        header.Divergencias = (await cn.QueryAsync<FechamentoDivergenciaDto>(new CommandDefinition(@"
            select id as ""Id"",fechamento_item_id as ""FechamentoItemId"",tipo as ""Tipo"",descricao as ""Descricao"",valor_anterior as ""ValorAnterior"",
            valor_proposto as ""ValorProposto"",status as ""Status"",resolucao as ""Resolucao"",criada_em as ""CriadoEm"",resolvida_em as ""ResolvidoEm""
            from plantaopro.fechamento_divergencias where tenant_id=@Tenant and fechamento_id=@id order by criada_em desc
            ", new { id, c.Tenant }, cancellationToken: ct))).AsList();
        return ApiResponse<FechamentoDetalheDto>.Ok(header);
    }

    public async Task<ApiResponse<IReadOnlyList<FechamentoTimelineDto>>> TimelineAsync(Guid id, CancellationToken ct)
    {
        var c = Contexto(); await using var cn = Connection();
        if (!await cn.ExecuteScalarAsync<bool>(new CommandDefinition("select exists(select 1 from plantaopro.fechamento_plantao where id=@id and tenant_id=@Tenant and cliente_id=@Cliente)", new { id, c.Tenant, c.Cliente }, cancellationToken: ct))) return ApiResponse<IReadOnlyList<FechamentoTimelineDto>>.Fail("Fechamento não encontrado.", 404);
        var rows = (await cn.QueryAsync<FechamentoTimelineDto>(new CommandDefinition("select id as \"Id\",evento as \"Evento\",status_anterior as \"StatusAnterior\",status_novo as \"StatusNovo\",descricao as \"Descricao\",executado_por as \"ExecutadoPor\",executado_em as \"ExecutadoEm\" from plantaopro.fechamento_historico where tenant_id=@Tenant and cliente_id=@Cliente and fechamento_id=@id order by executado_em desc", new { id, c.Tenant, c.Cliente }, cancellationToken: ct))).AsList();
        return ApiResponse<IReadOnlyList<FechamentoTimelineDto>>.Ok(rows);
    }

    public async Task<ApiResponse<FechamentoDetalheDto>> GerarAsync(Guid plantaoId, string? ip, string? ua, CancellationToken ct)
    {
        var c = Contexto(); await using var cn = Connection(); await cn.OpenAsync(ct); await using var tx = await cn.BeginTransactionAsync(ct);
        try
        {
            var plantao = await cn.QueryFirstOrDefaultAsync<PlantaoSnapshot>(new CommandDefinition("select p.id as \"Id\",p.hospital_id as \"HospitalId\",p.data_inicio as \"Inicio\",p.data_fim as \"Fim\",p.valor as \"Valor\",p.status as \"Status\" from plantaopro.plantoes p where p.id=@plantaoId and p.reg_status='A' for update", new { plantaoId }, tx, cancellationToken: ct));
            if (plantao is null) return ApiResponse<FechamentoDetalheDto>.Fail("Plantão não encontrado.", 404);
            if (!string.Equals(plantao.Status, "realizado", StringComparison.OrdinalIgnoreCase)) return ApiResponse<FechamentoDetalheDto>.Fail("Somente plantão realizado pode ser fechado.", 422);
            var existente = await cn.ExecuteScalarAsync<Guid?>(new CommandDefinition("select id from plantaopro.fechamento_plantao where tenant_id=@Tenant and plantao_id=@plantaoId and status<>'CANCELADO' limit 1", new { c.Tenant, plantaoId }, tx, cancellationToken: ct));
            if (existente.HasValue) return ApiResponse<FechamentoDetalheDto>.Fail("Já existe fechamento para este plantão.", 409);
            var escalas = (await cn.QueryAsync<EscalaSnapshot>(new CommandDefinition("select e.id as \"Id\",e.medico_id as \"MedicoId\",e.status as \"Status\",p.data_inicio as \"Inicio\",p.data_fim as \"Fim\",p.valor as \"Valor\" from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id where e.plantao_id=@plantaoId and e.reg_status='A' and e.status in ('realizado','nao_compareceu') for update", new { plantaoId }, tx, cancellationToken: ct))).AsList();
            if (escalas.Count == 0) return ApiResponse<FechamentoDetalheDto>.Fail("O plantão não possui escalas realizadas ou ausências registradas.", 422);
            var id = Guid.NewGuid(); decimal hp = 0, hr = 0, vp = 0, va = 0;
            foreach (var e in escalas) { var horas = Math.Round((decimal)(e.Fim-e.Inicio).TotalHours,2); var realizado = e.Status=="realizado"; var valor = PlantaoPaymentCalculator.Calcular(e.Valor,e.Inicio,e.Fim); hp+=horas; vp+=valor; if(realizado){hr+=horas;va+=valor;} }
            await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.fechamento_plantao(id,tenant_id,cliente_id,plantao_id,hospital_id,status,data_referencia,valor_previsto,valor_apurado,horas_previstas,horas_realizadas,iniciado_por,iniciado_em,atualizado_por) values(@id,@Tenant,@Cliente,@plantaoId,@HospitalId,'ABERTO',@referencia,@vp,@va,@hp,@hr,@Usuario,now(),@Usuario)", new { id, c.Tenant, c.Cliente, plantaoId, plantao.HospitalId, referencia=DateOnly.FromDateTime(plantao.Inicio), vp, va, hp, hr, c.Usuario }, tx, cancellationToken: ct));
            foreach (var e in escalas) { var horas=Math.Round((decimal)(e.Fim-e.Inicio).TotalHours,2); var valor=PlantaoPaymentCalculator.Calcular(e.Valor,e.Inicio,e.Fim); var realizado=e.Status=="realizado"; await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.fechamento_plantao_escalas(id,tenant_id,fechamento_id,escala_id,medico_id,plantao_id,status_escala,inicio_previsto,fim_previsto,inicio_realizado,fim_realizado,presenca,horas_previstas,horas_realizadas,valor_previsto,valor_calculado) values(gen_random_uuid(),@Tenant,@id,@Id,@MedicoId,@plantaoId,@Status,@Inicio,@Fim,@ir,@fr,@realizado,@horas,@hreal,@valor,@apurado)", new { c.Tenant,id,e.Id,e.MedicoId,plantaoId,e.Status,e.Inicio,e.Fim,ir=realizado?e.Inicio:(DateTime?)null,fr=realizado?e.Fim:(DateTime?)null,realizado,horas,hreal=realizado?horas:0,valor,apurado=realizado?valor:0 }, tx, cancellationToken:ct)); }
            await Historico(cn,tx,c,id,"GERADO",null,FechamentoStatus.Aberto,"Fechamento gerado a partir das escalas reais.",ct);
            await notificacoes.CriarNotificacaoAsync(c.Usuario,"Fechamento criado","Fechamento operacional disponível para conferência.","financeiro",tx);
            await tx.CommitAsync(ct); await audit.LogAsync(c.Usuario,"CREATE","fechamento_plantao",id,"Fechamento operacional gerado",ip:ip,userAgent:ua);
            return await ObterAsync(id,ct);
        }
        catch(PostgresException ex) when(ex.SqlState==PostgresErrorCodes.UniqueViolation) { await tx.RollbackAsync(ct); return ApiResponse<FechamentoDetalheDto>.Fail("Já existe fechamento para este plantão.",409); }
        catch(Exception ex){ await tx.RollbackAsync(ct); logger.LogError(ex,"Erro ao gerar fechamento do plantão {PlantaoId}",plantaoId); return ApiResponse<FechamentoDetalheDto>.Fail("Não foi possível gerar o fechamento.",500); }
    }

    public Task<ApiResponse<FechamentoDetalheDto>> IniciarConferenciaAsync(Guid id, CancellationToken ct) => TransicionarAsync(id, new[] { FechamentoStatus.Aberto,FechamentoStatus.Devolvido }, FechamentoStatus.EmConferencia,"CONFERENCIA_INICIADA",null,ct);
    public async Task<ApiResponse<FechamentoDetalheDto>> ConcluirConferenciaAsync(Guid id, CancellationToken ct)
    {
        var c=Contexto(); await using var cn=Connection(); var abertas=await cn.ExecuteScalarAsync<int>(new CommandDefinition("select count(*) from plantaopro.fechamento_divergencias where tenant_id=@Tenant and fechamento_id=@id and status='ABERTA'",new{id,c.Tenant},cancellationToken:ct));
        if(abertas>0) return ApiResponse<FechamentoDetalheDto>.Fail("Resolva as divergências abertas antes de concluir a conferência.",422);
        return await TransicionarAsync(id,new[] { FechamentoStatus.EmConferencia },FechamentoStatus.AguardandoAprovacao,"CONFERENCIA_CONCLUIDA",null,ct);
    }
    public Task<ApiResponse<FechamentoDetalheDto>> AprovarAsync(Guid id,CancellationToken ct)=>TransicionarAsync(id,new[] { FechamentoStatus.AguardandoAprovacao },FechamentoStatus.Aprovado,"APROVADO",null,ct,"aprovado_por","aprovado_em");
    public async Task<ApiResponse<FechamentoDetalheDto>> DevolverAsync(Guid id,string motivo,CancellationToken ct)
    { motivo=(motivo??"").Trim(); if(motivo.Length<10||motivo.Length>500)return ApiResponse<FechamentoDetalheDto>.Fail("Motivo deve possuir entre 10 e 500 caracteres.",422); return await TransicionarAsync(id,new[] { FechamentoStatus.AguardandoAprovacao },FechamentoStatus.Devolvido,"DEVOLVIDO",motivo,ct,"devolvido_por","devolvido_em"); }

    public async Task<ApiResponse<FechamentoDetalheDto>> CriarDivergenciaAsync(Guid id,CriarDivergenciaRequest request,CancellationToken ct)
    {
        var c=Contexto(); var tipo=(request.Tipo??"").Trim().ToUpperInvariant(); var descricao=(request.Descricao??"").Trim();
        if(!TiposDivergencia.Contains(tipo)||descricao.Length<3||descricao.Length>500)return ApiResponse<FechamentoDetalheDto>.Fail("Tipo ou descrição de divergência inválidos.",422);
        await using var cn=Connection(); await cn.OpenAsync(ct); await using var tx=await cn.BeginTransactionAsync(ct);
        var f=await Lock(cn,tx,id,c,ct); if(f is null)return ApiResponse<FechamentoDetalheDto>.Fail("Fechamento não encontrado.",404); if(f.Status is not (FechamentoStatus.EmConferencia or FechamentoStatus.ComDivergencia))return ApiResponse<FechamentoDetalheDto>.Fail("Status não permite registrar divergência.",409);
        if(request.FechamentoItemId.HasValue && !await cn.ExecuteScalarAsync<bool>(new CommandDefinition("select exists(select 1 from plantaopro.fechamento_plantao_escalas where id=@item and fechamento_id=@id and tenant_id=@Tenant)",new{item=request.FechamentoItemId,id,c.Tenant},tx,cancellationToken:ct)))return ApiResponse<FechamentoDetalheDto>.Fail("Item do fechamento não encontrado.",404);
        await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.fechamento_divergencias(id,tenant_id,fechamento_id,fechamento_item_id,tipo,descricao,valor_anterior,valor_proposto,motivo,status,criada_por) values(gen_random_uuid(),@Tenant,@id,@item,@tipo,@descricao,@anterior,@proposto,@motivo,'ABERTA',@Usuario)",new{c.Tenant,id,item=request.FechamentoItemId,tipo,descricao,anterior=request.ValorAnterior,proposto=request.ValorProposto,motivo=request.Motivo,c.Usuario},tx,cancellationToken:ct));
        if(request.FechamentoItemId.HasValue)await cn.ExecuteAsync(new CommandDefinition("update plantaopro.fechamento_plantao_escalas set possui_divergencia=true,atualizado_em=now() where id=@item and tenant_id=@Tenant",new{item=request.FechamentoItemId,c.Tenant},tx,cancellationToken:ct));
        if(f.Status==FechamentoStatus.EmConferencia)await AtualizarStatus(cn,tx,id,c,FechamentoStatus.EmConferencia,FechamentoStatus.ComDivergencia,ct);
        await Historico(cn,tx,c,id,"DIVERGENCIA_ABERTA",f.Status,FechamentoStatus.ComDivergencia,descricao,ct); await notificacoes.CriarNotificacaoAsync(c.Usuario,"Divergência aberta",descricao,"financeiro",tx); await tx.CommitAsync(ct); return await ObterAsync(id,ct);
    }

    public async Task<ApiResponse<FechamentoDetalheDto>> ResolverDivergenciaAsync(Guid id,Guid divergenciaId,string resolucao,CancellationToken ct)
    {
        resolucao=(resolucao??"").Trim(); if(resolucao.Length<3||resolucao.Length>1000)return ApiResponse<FechamentoDetalheDto>.Fail("Resolução deve possuir entre 3 e 1000 caracteres.",422); var c=Contexto(); await using var cn=Connection(); await cn.OpenAsync(ct); await using var tx=await cn.BeginTransactionAsync(ct);
        var d=await cn.QueryFirstOrDefaultAsync<(Guid Id,Guid? ItemId)>(new CommandDefinition("select id as \"Id\",fechamento_item_id as \"ItemId\" from plantaopro.fechamento_divergencias where id=@divergenciaId and fechamento_id=@id and tenant_id=@Tenant and status='ABERTA' for update",new{divergenciaId,id,c.Tenant},tx,cancellationToken:ct)); if(d.Id==Guid.Empty)return ApiResponse<FechamentoDetalheDto>.Fail("Divergência aberta não encontrada.",404);
        var changed=await cn.ExecuteAsync(new CommandDefinition("update plantaopro.fechamento_divergencias set status='RESOLVIDA',resolucao=@resolucao,resolvida_por=@Usuario,resolvida_em=now() where id=@divergenciaId and tenant_id=@Tenant and status='ABERTA'",new{divergenciaId,c.Tenant,resolucao,c.Usuario},tx,cancellationToken:ct)); if(changed!=1)return ApiResponse<FechamentoDetalheDto>.Fail("Divergência já foi resolvida.",409);
        if(d.ItemId.HasValue)await cn.ExecuteAsync(new CommandDefinition("update plantaopro.fechamento_plantao_escalas i set possui_divergencia=exists(select 1 from plantaopro.fechamento_divergencias d where d.tenant_id=i.tenant_id and d.fechamento_item_id=i.id and d.status='ABERTA'),atualizado_em=now() where i.id=@ItemId and i.tenant_id=@Tenant",new{d.ItemId,c.Tenant},tx,cancellationToken:ct));
        var abertas=await cn.ExecuteScalarAsync<int>(new CommandDefinition("select count(*) from plantaopro.fechamento_divergencias where tenant_id=@Tenant and fechamento_id=@id and status='ABERTA'",new{id,c.Tenant},tx,cancellationToken:ct)); if(abertas==0)await AtualizarStatus(cn,tx,id,c,FechamentoStatus.ComDivergencia,FechamentoStatus.EmConferencia,ct);
        await Historico(cn,tx,c,id,"DIVERGENCIA_RESOLVIDA",FechamentoStatus.ComDivergencia,abertas==0?FechamentoStatus.EmConferencia:FechamentoStatus.ComDivergencia,resolucao,ct); await notificacoes.CriarNotificacaoAsync(c.Usuario,"Divergência resolvida",resolucao,"financeiro",tx); await tx.CommitAsync(ct); return await ObterAsync(id,ct);
    }

    public async Task<ApiResponse<FechamentoDetalheDto>> GerarFinanceiroAsync(Guid id,CancellationToken ct)
    {
        var c=Contexto(); await using var cn=Connection(); await cn.OpenAsync(ct); await using var tx=await cn.BeginTransactionAsync(ct); var f=await Lock(cn,tx,id,c,ct); if(f is null)return ApiResponse<FechamentoDetalheDto>.Fail("Fechamento não encontrado.",404); if(f.Status==FechamentoStatus.FinanceiroGerado){await tx.RollbackAsync(ct);return await ObterAsync(id,ct);} if(f.Status!=FechamentoStatus.Aprovado)return ApiResponse<FechamentoDetalheDto>.Fail("Somente fechamento aprovado pode gerar financeiro.",409);
        var items=(await cn.QueryAsync<FinanceItem>(new CommandDefinition("select i.id as \"ItemId\",i.escala_id as \"EscalaId\",i.medico_id as \"MedicoId\",i.plantao_id as \"PlantaoId\",i.valor_calculado as \"Valor\",i.horas_realizadas as \"Horas\",i.status_escala as \"Status\" from plantaopro.fechamento_plantao_escalas i where i.tenant_id=@Tenant and i.fechamento_id=@id for update",new{id,c.Tenant},tx,cancellationToken:ct))).AsList();
        foreach(var item in items.Where(x=>x.Status=="realizado"&&x.Valor>0)){var pagamentoId=await cn.ExecuteScalarAsync<Guid?>(new CommandDefinition("select id from plantaopro.pagamentos where escala_id=@EscalaId and reg_status='A' limit 1",item,tx,cancellationToken:ct)); if(!pagamentoId.HasValue){pagamentoId=Guid.NewGuid();await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.pagamentos(id,escala_id,medico_id,plantao_id,valor_previsto,status,data_prevista,observacoes,reg_date,reg_status,created_by,horas_referencia,valor_hora,processado_automaticamente) values(@pagamentoId,@EscalaId,@MedicoId,@PlantaoId,@Valor,'pendente',current_date+7,'Gerado pelo fechamento operacional',now(),'A',@Usuario,@Horas,case when @Horas>0 then @Valor/@Horas else 0 end,true)",new{pagamentoId,item.EscalaId,item.MedicoId,item.PlantaoId,item.Valor,item.Horas,c.Usuario},tx,cancellationToken:ct));await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.historico_pagamento(id,pagamento_id,status_novo,justificativa,usuario_id,reg_date) values(gen_random_uuid(),@pagamentoId,'pendente','Gerado pelo fechamento operacional',@Usuario,now())",new{pagamentoId,c.Usuario},tx,cancellationToken:ct));} await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.financeiro_pagamento_origem(id,tenant_id,pagamento_id,fechamento_id,escala_id) values(gen_random_uuid(),@Tenant,@pagamentoId,@id,@EscalaId) on conflict do nothing",new{c.Tenant,pagamentoId,id,item.EscalaId},tx,cancellationToken:ct));}
        var changed=await AtualizarStatus(cn,tx,id,c,FechamentoStatus.Aprovado,FechamentoStatus.FinanceiroGerado,ct,"financeiro_gerado_por","financeiro_gerado_em"); if(changed!=1)return ApiResponse<FechamentoDetalheDto>.Fail("O fechamento foi alterado por outro usuário.",409); await Historico(cn,tx,c,id,"FINANCEIRO_GERADO",FechamentoStatus.Aprovado,FechamentoStatus.FinanceiroGerado,"Pagamentos médicos gerados ou vinculados.",ct); await notificacoes.CriarNotificacaoAsync(c.Usuario,"Financeiro gerado","Pagamentos médicos vinculados ao fechamento.","financeiro",tx); await tx.CommitAsync(ct); return await ObterAsync(id,ct);
    }

    private async Task<ApiResponse<FechamentoDetalheDto>> TransicionarAsync(Guid id,string[] esperados,string destino,string evento,string? descricao,CancellationToken ct,string? userColumn=null,string? dateColumn=null)
    { var c=Contexto(); await using var cn=Connection(); await cn.OpenAsync(ct); await using var tx=await cn.BeginTransactionAsync(ct); var f=await Lock(cn,tx,id,c,ct); if(f is null)return ApiResponse<FechamentoDetalheDto>.Fail("Fechamento não encontrado.",404); if(!esperados.Contains(f.Status)||!FechamentoStatus.PodeTransicionar(f.Status,destino))return ApiResponse<FechamentoDetalheDto>.Fail($"Transição de {f.Status} para {destino} não permitida.",409); var changed=await AtualizarStatus(cn,tx,id,c,f.Status,destino,ct,userColumn,dateColumn,descricao); if(changed!=1)return ApiResponse<FechamentoDetalheDto>.Fail("O fechamento foi alterado por outro usuário.",409); await Historico(cn,tx,c,id,evento,f.Status,destino,descricao,ct); await notificacoes.CriarNotificacaoAsync(c.Usuario,$"Fechamento: {destino}",descricao??$"Status alterado para {destino}.","financeiro",tx); await tx.CommitAsync(ct); return await ObterAsync(id,ct); }
    private static Task<LockedFechamento?> Lock(NpgsqlConnection cn,NpgsqlTransaction tx,Guid id,(Guid Tenant,Guid Cliente,Guid Usuario)c,CancellationToken ct)=>cn.QueryFirstOrDefaultAsync<LockedFechamento>(new CommandDefinition("select id as \"Id\",status as \"Status\" from plantaopro.fechamento_plantao where id=@id and tenant_id=@Tenant and cliente_id=@Cliente for update",new{id,c.Tenant,c.Cliente},tx,cancellationToken:ct));
    private static Task<int> AtualizarStatus(NpgsqlConnection cn,NpgsqlTransaction tx,Guid id,(Guid Tenant,Guid Cliente,Guid Usuario)c,string esperado,string destino,CancellationToken ct,string? userColumn=null,string? dateColumn=null,string? motivo=null){var extra=userColumn is null?"":$",{userColumn}=@Usuario,{dateColumn}=now()"; if(destino==FechamentoStatus.EmConferencia)extra+=",conferido_por=@Usuario,conferido_em=now()"; if(destino==FechamentoStatus.Devolvido)extra+=",motivo_devolucao=@motivo"; return cn.ExecuteAsync(new CommandDefinition($"update plantaopro.fechamento_plantao set status=@destino,atualizado_por=@Usuario,atualizado_em=now(){extra} where id=@id and tenant_id=@Tenant and cliente_id=@Cliente and status=@esperado",new{id,c.Tenant,c.Cliente,c.Usuario,esperado,destino,motivo},tx,cancellationToken:ct));}
    private static Task Historico(NpgsqlConnection cn,NpgsqlTransaction tx,(Guid Tenant,Guid Cliente,Guid Usuario)c,Guid id,string evento,string? anterior,string novo,string? descricao,CancellationToken ct)=>cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.fechamento_historico(id,tenant_id,cliente_id,fechamento_id,evento,status_anterior,status_novo,descricao,executado_por) values(gen_random_uuid(),@Tenant,@Cliente,@id,@evento,@anterior,@novo,@descricao,@Usuario)",new{c.Tenant,c.Cliente,c.Usuario,id,evento,anterior,novo,descricao},tx,cancellationToken:ct));
    private sealed class PlantaoSnapshot{public Guid Id{get;set;}public Guid HospitalId{get;set;}public DateTime Inicio{get;set;}public DateTime Fim{get;set;}public decimal Valor{get;set;}public string Status{get;set;}="";}
    private sealed class EscalaSnapshot{public Guid Id{get;set;}public Guid MedicoId{get;set;}public string Status{get;set;}="";public DateTime Inicio{get;set;}public DateTime Fim{get;set;}public decimal Valor{get;set;}}
    private sealed class LockedFechamento{public Guid Id{get;set;}public string Status{get;set;}="";}
    private sealed class FinanceItem{public Guid ItemId{get;set;}public Guid EscalaId{get;set;}public Guid MedicoId{get;set;}public Guid PlantaoId{get;set;}public decimal Valor{get;set;}public decimal Horas{get;set;}public string Status{get;set;}="";}
}
