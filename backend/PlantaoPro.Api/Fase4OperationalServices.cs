using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;
using Fase4MedicoDisponivelDto = PlantaoPro.Api.Models.MedicoDisponivelDto;
using Fase4EscalaSugestaoDto = PlantaoPro.Api.Models.EscalaSugestaoDto;
using System.Text;
using System.Text.Json;

namespace PlantaoPro.Api.Data;

public sealed class OperationalAutomationService
{
    private readonly IConfiguration cfg;
    private readonly IAuditService audit;
    private readonly ILogger<OperationalAutomationService> logger;

    public OperationalAutomationService(IConfiguration cfg, IAuditService audit, ILogger<OperationalAutomationService> logger)
    {
        this.cfg = cfg;
        this.audit = audit;
        this.logger = logger;
    }

    private NpgsqlConnection Cn() => new(cfg.GetConnectionString("Default"));

    public async Task<(Guid? UsuarioId, Guid? ClienteId)> ObterContextoUsuarioAsync(System.Security.Claims.ClaimsPrincipal user)
    {
        Guid? uid = TryGuid(user.FindFirst("uid")?.Value ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        Guid? clienteId = TryGuid(user.FindFirst("cliente_id")?.Value);
        if (uid.HasValue && !clienteId.HasValue)
        {
            await using var cn = Cn();
            clienteId = await cn.ExecuteScalarAsync<Guid?>("select cliente_id from plantaopro.usuarios where id=@uid union all select cliente_id from plantaopro.app_user where id=@uid limit 1", new { uid });
        }
        return (uid, clienteId);
    }

    private static Guid? TryGuid(string? value) => Guid.TryParse(value, out var id) ? id : null;

    private async Task<(Guid Id, Guid? ClienteId)> ObterMedicoDoUsuarioAsync(NpgsqlConnection cn, Guid uid, System.Data.IDbTransaction? tx = null)
    {
        var medico = await cn.QueryFirstOrDefaultAsync<(Guid Id, Guid? ClienteId)>("select id, cliente_id from plantaopro.medicos where usuario_id=@uid and reg_status='A' limit 1", new { uid }, tx);
        return medico.Id == Guid.Empty ? (Guid.Empty, null) : medico;
    }

    public async Task<ApiResponse<IEnumerable<MedicoDisponibilidadeDto>>> ListarDisponibilidadesAsync(Guid uid, DateTime? inicio = null, DateTime? fim = null)
    {
        if (inicio.HasValue != fim.HasValue) return ApiResponse<IEnumerable<MedicoDisponibilidadeDto>>.Fail("Informe início e fim para filtrar o período.");
        if (inicio.HasValue)
        {
            var erro = ValidarPeriodo(inicio.Value, fim!.Value, 366);
            if (erro is not null) return ApiResponse<IEnumerable<MedicoDisponibilidadeDto>>.Fail(erro);
        }
        await using var cn = Cn();
        var medico = await ObterMedicoDoUsuarioAsync(cn, uid);
        if (medico.Id == Guid.Empty) return ApiResponse<IEnumerable<MedicoDisponibilidadeDto>>.Fail("Médico não encontrado para o usuário autenticado.", 404);
        var rows = await cn.QueryAsync<MedicoDisponibilidadeDto>(@"select d.id as ""Id"", d.medico_id as ""MedicoId"", d.hospital_id as ""HospitalId"", coalesce(h.nome_fantasia,h.razao_social,'') as ""HospitalNome"", d.especialidade_id as ""EspecialidadeId"", coalesce(e.nome,'') as ""EspecialidadeNome"", d.data_inicio as ""DataInicio"", d.data_fim as ""DataFim"", coalesce(d.turno,'') as ""Turno"", coalesce(d.status,'') as ""Status"", coalesce(d.observacoes,'') as ""Observacoes"", coalesce(d.reg_update,d.reg_date) as ""Versao""
from plantaopro.medico_disponibilidades d
left join plantaopro.hospitais h on h.id=d.hospital_id
left join plantaopro.especialidades e on e.id=d.especialidade_id
where d.medico_id=@medicoId and d.reg_status='A' and (@inicio is null or (d.data_inicio < @fim and d.data_fim > @inicio)) order by d.data_inicio limit 200", new { medicoId = medico.Id, inicio, fim });
        return ApiResponse<IEnumerable<MedicoDisponibilidadeDto>>.Ok(rows, "Disponibilidades listadas.");
    }

    public async Task<ApiResponse<Guid>> CriarDisponibilidadeAsync(Guid uid, PlantaoPro.Api.Models.MedicoDisponibilidadeRequest request, string? ip, string? ua)
    {
        var erro = ValidarPeriodo(request.DataInicio, request.DataFim);
        if (erro is not null) return ApiResponse<Guid>.Fail(erro);
        erro = ValidarTexto(request.Observacoes, 500, "A observação");
        if (erro is not null) return ApiResponse<Guid>.Fail(erro);
        await using var cn = Cn();
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var medico = await ObterMedicoDoUsuarioAsync(cn, uid, tx);
        if (medico.Id == Guid.Empty) return ApiResponse<Guid>.Fail("Médico não encontrado para o usuário autenticado.", 404);
        await cn.ExecuteAsync("select pg_advisory_xact_lock(hashtextextended(@key, 0))", new { key = $"disponibilidade:{medico.Id}" }, tx);
        if (!await ContextoPermitidoAsync(cn, tx, medico.ClienteId, request.HospitalId, request.EspecialidadeId)) return ApiResponse<Guid>.Fail("Unidade ou especialidade fora do seu contexto autorizado.", 403);
        var existente = await cn.ExecuteScalarAsync<Guid?>(@"select id from plantaopro.medico_disponibilidades where medico_id=@medicoId and reg_status='A' and status='ATIVA' and data_inicio=@inicio and data_fim=@fim and hospital_id is not distinct from @hospitalId and especialidade_id is not distinct from @especialidadeId limit 1", new { medicoId = medico.Id, inicio = request.DataInicio, fim = request.DataFim, request.HospitalId, request.EspecialidadeId }, tx);
        if (existente.HasValue) { await tx.CommitAsync(); return ApiResponse<Guid>.Ok(existente.Value, "Esta disponibilidade já estava cadastrada; nenhum registro foi duplicado."); }
        var conflita = await cn.ExecuteScalarAsync<int>(@"select count(1) from (select data_inicio,data_fim from plantaopro.medico_indisponibilidades where medico_id=@medicoId and reg_status='A' and status='ATIVA' union all select data_inicio,data_fim from plantaopro.medico_disponibilidades where medico_id=@medicoId and reg_status='A' and status='ATIVA') x where x.data_inicio < @fim and x.data_fim > @inicio", new { medicoId = medico.Id, inicio = request.DataInicio, fim = request.DataFim }, tx);
        if (conflita > 0) return ApiResponse<Guid>.Fail("O período se sobrepõe a uma informação existente. Ajuste ou remova o intervalo anterior; contradições não são resolvidas automaticamente.", 409);
        var id = Guid.NewGuid();
        await cn.ExecuteAsync(@"insert into plantaopro.medico_disponibilidades(id,cliente_id,medico_id,hospital_id,especialidade_id,data_inicio,data_fim,turno,status,observacoes,created_by,reg_date,reg_status)
values(@id,@clienteId,@medicoId,@hospitalId,@especialidadeId,@inicio,@fim,@turno,'ATIVA',@obs,@uid,now(),'A')", new { id, clienteId = medico.ClienteId, medicoId = medico.Id, hospitalId = request.HospitalId, especialidadeId = request.EspecialidadeId, inicio = request.DataInicio, fim = request.DataFim, turno = Normalizar(request.Turno, "GERAL"), obs = request.Observacoes?.Trim(), uid }, tx);
        await RegistrarHistoricoDisponibilidadeAsync(cn, medico.ClienteId, medico.Id, "medico_disponibilidades", id, "CRIAR", request, uid, tx);
        await tx.CommitAsync();
        await audit.LogAsync(uid, "CREATE", "medico_disponibilidades", id, "Médico cadastrou disponibilidade", ip: ip, userAgent: ua);
        return ApiResponse<Guid>.Ok(id, "Disponibilidade cadastrada.");
    }

    public async Task<ApiResponse<string>> AtualizarDisponibilidadeAsync(Guid uid, Guid id, PlantaoPro.Api.Models.MedicoDisponibilidadeRequest request, string? ip, string? ua)
    {
        var erro = ValidarPeriodo(request.DataInicio, request.DataFim);
        if (erro is not null) return ApiResponse<string>.Fail(erro);
        erro = ValidarTexto(request.Observacoes, 500, "A observação");
        if (erro is not null) return ApiResponse<string>.Fail(erro);
        await using var cn = Cn();
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var medico = await ObterMedicoDoUsuarioAsync(cn, uid, tx);
        if (medico.Id == Guid.Empty) return ApiResponse<string>.Fail("Médico não encontrado para o usuário autenticado.", 404);
        await cn.ExecuteAsync("select pg_advisory_xact_lock(hashtextextended(@key, 0))", new { key = $"disponibilidade:{medico.Id}" }, tx);
        if (!await ContextoPermitidoAsync(cn, tx, medico.ClienteId, request.HospitalId, request.EspecialidadeId)) return ApiResponse<string>.Fail("Unidade ou especialidade fora do seu contexto autorizado.", 403);
        var conflita = await cn.ExecuteScalarAsync<int>(@"select count(1) from (select id,data_inicio,data_fim from plantaopro.medico_disponibilidades where medico_id=@medicoId and reg_status='A' and status='ATIVA' union all select id,data_inicio,data_fim from plantaopro.medico_indisponibilidades where medico_id=@medicoId and reg_status='A' and status='ATIVA') x where x.id<>@id and x.data_inicio < @fim and x.data_fim > @inicio", new { id, medicoId = medico.Id, inicio = request.DataInicio, fim = request.DataFim }, tx);
        if (conflita > 0) return ApiResponse<string>.Fail("O período se sobrepõe a uma informação existente. Ajuste o intervalo antes de salvar.", 409);
        var updated = await cn.ExecuteAsync(@"update plantaopro.medico_disponibilidades set hospital_id=@hospitalId, especialidade_id=@especialidadeId, data_inicio=@inicio, data_fim=@fim, turno=@turno, observacoes=@obs, updated_by=@uid, reg_update=now() where id=@id and medico_id=@medicoId and reg_status='A' and (@versao is null or coalesce(reg_update,reg_date)=@versao)", new { id, medicoId = medico.Id, hospitalId = request.HospitalId, especialidadeId = request.EspecialidadeId, inicio = request.DataInicio, fim = request.DataFim, turno = Normalizar(request.Turno, "GERAL"), obs = request.Observacoes, uid, versao = request.VersaoEsperada }, tx);
        if (updated == 0)
        {
            var pertence = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.medico_disponibilidades where id=@id and medico_id=@medicoId and reg_status='A'", new { id, medicoId = medico.Id }, tx) > 0;
            return ApiResponse<string>.Fail(pertence ? "O registro mudou desde a última consulta. Atualize a tela antes de editar novamente." : "Disponibilidade não encontrada.", pertence ? 409 : 404);
        }
        await RegistrarHistoricoDisponibilidadeAsync(cn, medico.ClienteId, medico.Id, "medico_disponibilidades", id, "ATUALIZAR", request, uid, tx);
        await tx.CommitAsync();
        await audit.LogAsync(uid, "UPDATE", "medico_disponibilidades", id, "Médico atualizou disponibilidade", ip: ip, userAgent: ua);
        return ApiResponse<string>.Ok("ok", "Disponibilidade atualizada.");
    }

    public async Task<ApiResponse<string>> RemoverDisponibilidadeAsync(Guid uid, Guid id, string? ip, string? ua)
    {
        await using var cn = Cn();
        var medico = await ObterMedicoDoUsuarioAsync(cn, uid);
        if (medico.Id == Guid.Empty) return ApiResponse<string>.Fail("Médico não encontrado para o usuário autenticado.", 404);
        var updated = await cn.ExecuteAsync("update plantaopro.medico_disponibilidades set reg_status='I', updated_by=@uid, reg_update=now() where id=@id and medico_id=@medicoId and reg_status='A'", new { id, medicoId = medico.Id, uid });
        if (updated == 0) return ApiResponse<string>.Fail("Disponibilidade não encontrada.", 404);
        await RegistrarHistoricoDisponibilidadeAsync(cn, medico.ClienteId, medico.Id, "medico_disponibilidades", id, "REMOVER", new { id }, uid);
        await audit.LogAsync(uid, "DELETE", "medico_disponibilidades", id, "Médico removeu disponibilidade", ip: ip, userAgent: ua);
        return ApiResponse<string>.Ok("ok", "Disponibilidade removida.");
    }

    public async Task<ApiResponse<IEnumerable<MedicoDisponibilidadeDto>>> ListarIndisponibilidadesAsync(Guid uid)
    {
        await using var cn = Cn();
        var medico = await ObterMedicoDoUsuarioAsync(cn, uid);
        if (medico.Id == Guid.Empty) return ApiResponse<IEnumerable<MedicoDisponibilidadeDto>>.Fail("Médico não encontrado para o usuário autenticado.", 404);
        var rows = await cn.QueryAsync<MedicoDisponibilidadeDto>(@"select id as ""Id"", medico_id as ""MedicoId"", data_inicio as ""DataInicio"", data_fim as ""DataFim"", 'INDISPONIVEL' as ""Turno"", coalesce(status,'') as ""Status"", coalesce(motivo,'') as ""Observacoes"" from plantaopro.medico_indisponibilidades where medico_id=@medicoId and reg_status='A' order by data_inicio desc limit 200", new { medicoId = medico.Id });
        return ApiResponse<IEnumerable<MedicoDisponibilidadeDto>>.Ok(rows, "Indisponibilidades listadas.");
    }

    public async Task<ApiResponse<Guid>> CriarIndisponibilidadeAsync(Guid uid, MedicoIndisponibilidadeRequest request, string? ip, string? ua)
    {
        var erro = ValidarPeriodo(request.DataInicio, request.DataFim);
        if (erro is not null) return ApiResponse<Guid>.Fail(erro);
        if (string.IsNullOrWhiteSpace(request.Motivo)) return ApiResponse<Guid>.Fail("Informe o motivo da indisponibilidade.");
        erro = ValidarTexto(request.Motivo, 500, "O motivo");
        if (erro is not null) return ApiResponse<Guid>.Fail(erro);
        await using var cn = Cn();
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var medico = await ObterMedicoDoUsuarioAsync(cn, uid, tx);
        if (medico.Id == Guid.Empty) return ApiResponse<Guid>.Fail("Médico não encontrado para o usuário autenticado.", 404);
        await cn.ExecuteAsync("select pg_advisory_xact_lock(hashtextextended(@key, 0))", new { key = $"disponibilidade:{medico.Id}" }, tx);
        var existente = await cn.ExecuteScalarAsync<Guid?>(@"select id from plantaopro.medico_indisponibilidades where medico_id=@medicoId and reg_status='A' and status='ATIVA' and data_inicio=@inicio and data_fim=@fim limit 1", new { medicoId = medico.Id, inicio = request.DataInicio, fim = request.DataFim }, tx);
        if (existente.HasValue) { await tx.CommitAsync(); return ApiResponse<Guid>.Ok(existente.Value, "Esta indisponibilidade já estava cadastrada; nenhum registro foi duplicado."); }
        var conflita = await cn.ExecuteScalarAsync<int>(@"select count(1) from (select data_inicio,data_fim from plantaopro.medico_disponibilidades where medico_id=@medicoId and reg_status='A' and status='ATIVA' union all select data_inicio,data_fim from plantaopro.medico_indisponibilidades where medico_id=@medicoId and reg_status='A' and status='ATIVA') x where x.data_inicio < @fim and x.data_fim > @inicio", new { medicoId = medico.Id, inicio = request.DataInicio, fim = request.DataFim }, tx);
        if (conflita > 0) return ApiResponse<Guid>.Fail("O período se sobrepõe a uma informação existente. Ajuste ou remova o intervalo anterior; contradições não são resolvidas automaticamente.", 409);
        var compromissos = await cn.ExecuteScalarAsync<int>(@"select count(1) from plantaopro.escalas es join plantaopro.plantoes p on p.id=es.plantao_id where es.medico_id=@medicoId and es.reg_status='A' and es.status in ('confirmado','solicitado') and p.data_inicio < @fim and p.data_fim > @inicio", new { medicoId = medico.Id, inicio = request.DataInicio, fim = request.DataFim }, tx);
        var id = Guid.NewGuid();
        await cn.ExecuteAsync(@"insert into plantaopro.medico_indisponibilidades(id,cliente_id,medico_id,data_inicio,data_fim,motivo,status,created_by,reg_date,reg_status) values(@id,@clienteId,@medicoId,@inicio,@fim,@motivo,'ATIVA',@uid,now(),'A')", new { id, clienteId = medico.ClienteId, medicoId = medico.Id, inicio = request.DataInicio, fim = request.DataFim, motivo = request.Motivo.Trim(), uid }, tx);
        await RegistrarHistoricoDisponibilidadeAsync(cn, medico.ClienteId, medico.Id, "medico_indisponibilidades", id, "CRIAR", request, uid, tx);
        await tx.CommitAsync();
        await audit.LogAsync(uid, "CREATE", "medico_indisponibilidades", id, "Médico cadastrou indisponibilidade", ip: ip, userAgent: ua);
        return ApiResponse<Guid>.Ok(id, compromissos > 0 ? "Indisponibilidade cadastrada. Há compromisso no período: a atribuição foi preservada; solicite substituição se necessário." : "Indisponibilidade cadastrada.");
    }

    public async Task<ApiResponse<string>> RemoverIndisponibilidadeAsync(Guid uid, Guid id, string? ip, string? ua)
    {
        await using var cn = Cn();
        var medico = await ObterMedicoDoUsuarioAsync(cn, uid);
        if (medico.Id == Guid.Empty) return ApiResponse<string>.Fail("Médico não encontrado para o usuário autenticado.", 404);
        var updated = await cn.ExecuteAsync("update plantaopro.medico_indisponibilidades set reg_status='I', updated_by=@uid, reg_update=now() where id=@id and medico_id=@medicoId and reg_status='A'", new { id, medicoId = medico.Id, uid });
        if (updated == 0) return ApiResponse<string>.Fail("Indisponibilidade não encontrada.", 404);
        await audit.LogAsync(uid, "DELETE", "medico_indisponibilidades", id, "Médico removeu indisponibilidade", ip: ip, userAgent: ua);
        return ApiResponse<string>.Ok("ok", "Indisponibilidade removida.");
    }

    public async Task<ApiResponse<MedicoPreferenciasDto>> ObterPreferenciasAsync(Guid uid)
    {
        await using var cn = Cn();
        var medico = await ObterMedicoDoUsuarioAsync(cn, uid);
        if (medico.Id == Guid.Empty) return ApiResponse<MedicoPreferenciasDto>.Fail("Médico não encontrado para o usuário autenticado.", 404);
        var dto = await cn.QueryFirstOrDefaultAsync<MedicoPreferenciasDto>(@"select id as ""Id"", medico_id as ""MedicoId"", coalesce(hospitais_preferidos, array[]::uuid[]) as ""HospitaisPreferidos"", coalesce(especialidades_preferidas, array[]::uuid[]) as ""EspecialidadesPreferidas"", coalesce(turnos_preferidos, array[]::text[]) as ""TurnosPreferidos"", coalesce(limite_plantoes_semana,5) as ""LimitePlantoesSemana"", coalesce(limite_plantoes_mes,20) as ""LimitePlantoesMes"", coalesce(observacoes,'') as ""Observacoes"" from plantaopro.medico_preferencias_plantao where medico_id=@medicoId and reg_status='A' limit 1", new { medicoId = medico.Id });
        return ApiResponse<MedicoPreferenciasDto>.Ok(dto ?? new MedicoPreferenciasDto { MedicoId = medico.Id, LimitePlantoesSemana = 5, LimitePlantoesMes = 20 }, "Preferências carregadas.");
    }

    public async Task<ApiResponse<MedicoPreferenciasDto>> AtualizarPreferenciasAsync(Guid uid, MedicoPreferenciasRequest request, string? ip, string? ua)
    {
        if (request.LimitePlantoesSemana < 0 || request.LimitePlantoesMes < 0) return ApiResponse<MedicoPreferenciasDto>.Fail("Limites de plantões não podem ser negativos.");
        await using var cn = Cn();
        var medico = await ObterMedicoDoUsuarioAsync(cn, uid);
        if (medico.Id == Guid.Empty) return ApiResponse<MedicoPreferenciasDto>.Fail("Médico não encontrado para o usuário autenticado.", 404);
        var id = await cn.ExecuteScalarAsync<Guid?>("select id from plantaopro.medico_preferencias_plantao where medico_id=@medicoId and reg_status='A' limit 1", new { medicoId = medico.Id }) ?? Guid.NewGuid();
        await cn.ExecuteAsync(@"insert into plantaopro.medico_preferencias_plantao(id,cliente_id,medico_id,hospitais_preferidos,especialidades_preferidas,turnos_preferidos,limite_plantoes_semana,limite_plantoes_mes,observacoes,created_by,reg_date,reg_status)
values(@id,@clienteId,@medicoId,@hospitais,@especialidades,@turnos,@semana,@mes,@obs,@uid,now(),'A')
on conflict (medico_id) do update set hospitais_preferidos=@hospitais, especialidades_preferidas=@especialidades, turnos_preferidos=@turnos, limite_plantoes_semana=@semana, limite_plantoes_mes=@mes, observacoes=@obs, updated_by=@uid, reg_update=now()", new { id, clienteId = medico.ClienteId, medicoId = medico.Id, hospitais = request.HospitaisPreferidos ?? Array.Empty<Guid>(), especialidades = request.EspecialidadesPreferidas ?? Array.Empty<Guid>(), turnos = request.TurnosPreferidos ?? Array.Empty<string>(), semana = request.LimitePlantoesSemana == 0 ? 5 : request.LimitePlantoesSemana, mes = request.LimitePlantoesMes == 0 ? 20 : request.LimitePlantoesMes, obs = request.Observacoes, uid });
        await RegistrarHistoricoDisponibilidadeAsync(cn, medico.ClienteId, medico.Id, "medico_preferencias_plantao", id, "ATUALIZAR", request, uid);
        await audit.LogAsync(uid, "UPDATE", "medico_preferencias_plantao", id, "Médico atualizou preferências", ip: ip, userAgent: ua);
        return await ObterPreferenciasAsync(uid);
    }

    public async Task<ApiResponse<IEnumerable<Fase4MedicoDisponivelDto>>> ListarMedicosDisponiveisAsync(Guid? clienteId, DateTime dataInicio, DateTime dataFim, Guid? hospitalId, Guid? especialidadeId)
    {
        var erro = ValidarPeriodo(dataInicio, dataFim);
        if (erro is not null) return ApiResponse<IEnumerable<Fase4MedicoDisponivelDto>>.Fail(erro);
        await using var cn = Cn();
        var rows = await cn.QueryAsync<Fase4MedicoDisponivelDto>(@"select m.id as ""MedicoId"", coalesce(m.nome,'') as ""Nome"", coalesce(m.crm,'') as ""Crm"", coalesce(e.nome,'') as ""Especialidade"",
exists(select 1 from plantaopro.medico_disponibilidades d where d.medico_id=m.id and d.reg_status='A' and d.status='ATIVA' and d.data_inicio <= @inicio and d.data_fim >= @fim and (@hospitalId is null or d.hospital_id is null or d.hospital_id=@hospitalId) and (@especialidadeId is null or d.especialidade_id is null or d.especialidade_id=@especialidadeId)) as ""Disponivel"",
exists(select 1 from plantaopro.medico_indisponibilidades i where i.medico_id=m.id and i.reg_status='A' and i.status='ATIVA' and i.data_inicio < @fim and i.data_fim > @inicio) as ""Indisponivel"",
exists(select 1 from plantaopro.escalas es join plantaopro.plantoes p on p.id=es.plantao_id where es.medico_id=m.id and es.reg_status='A' and es.status in ('confirmado','solicitado') and p.data_inicio < @fim and p.data_fim > @inicio) as ""PossuiConflito"",
0::numeric as ""Score"", '' as ""Motivos"", '' as ""Alertas""
from plantaopro.medicos m
left join plantaopro.especialidades e on e.id=m.especialidade_id
where m.reg_status='A' and (@clienteId is null or m.cliente_id=@clienteId) and (@especialidadeId is null or m.especialidade_id=@especialidadeId)
order by m.nome limit 200", new { clienteId, inicio = dataInicio, fim = dataFim, hospitalId, especialidadeId });
        var result = rows.Select(r =>
        {
            var atendidos = new List<string>();
            var impedimentos = new List<string>();
            if (r.Disponivel) atendidos.Add("Disponibilidade confirmada para todo o período"); else impedimentos.Add("Sem disponibilidade confirmada para todo o período");
            if (!r.Indisponivel) atendidos.Add("Sem indisponibilidade cadastrada"); else impedimentos.Add("Indisponível no período");
            if (!r.PossuiConflito) atendidos.Add("Sem conflito de escala"); else impedimentos.Add("Indisponível por conflito de horário");
            r.Score = 0; // elegibilidade explicável; não há pontuação contratual configurada
            r.Motivos = string.Join("; ", atendidos);
            r.Alertas = string.Join("; ", impedimentos);
            return r;
        }).ToArray();
        return ApiResponse<IEnumerable<Fase4MedicoDisponivelDto>>.Ok(result, "Candidatos e critérios de elegibilidade listados.");
    }

    public async Task<ApiResponse<EscalaSugestaoDto>> GerarSugestaoAsync(Guid plantaoId, Guid? uid, Guid? clienteId, string? ip, string? ua)
    {
        await using var cn = Cn();
        var plantao = await cn.QueryFirstOrDefaultAsync<dynamic>(@"select p.id, p.cliente_id, p.hospital_id, p.especialidade_id, p.data_inicio, p.data_fim, coalesce(h.nome_fantasia,h.razao_social,'') hospital_nome, coalesce(e.nome,'') especialidade_nome from plantaopro.plantoes p left join plantaopro.hospitais h on h.id=p.hospital_id left join plantaopro.especialidades e on e.id=p.especialidade_id where p.id=@plantaoId and p.reg_status='A' and (@clienteId is null or p.cliente_id=@clienteId)", new { plantaoId, clienteId });
        if (plantao is null) return ApiResponse<EscalaSugestaoDto>.Fail("Plantão não encontrado.", 404);
        Guid pClienteId = (Guid)(plantao.cliente_id ?? Guid.Empty);
        var medicosResponse = await ListarMedicosDisponiveisAsync(clienteId ?? (pClienteId == Guid.Empty ? null : pClienteId), (DateTime)plantao.data_inicio, (DateTime)plantao.data_fim, (Guid?)plantao.hospital_id, (Guid?)plantao.especialidade_id);
        var medicos = (medicosResponse.Data ?? Array.Empty<Fase4MedicoDisponivelDto>()).OrderByDescending(m => m.Score).Take(20).ToArray();
        var sugestaoId = Guid.NewGuid();
        await cn.ExecuteAsync(@"insert into plantaopro.escala_sugestoes(id,cliente_id,plantao_id,hospital_id,especialidade_id,status,score_medio,gerada_por,criterios,reg_date,reg_status) values(@id,@clienteId,@plantaoId,@hospitalId,@especialidadeId,'GERADA',@score,@uid,cast(@criterios as jsonb),now(),'A')", new { id = sugestaoId, clienteId = pClienteId == Guid.Empty ? clienteId : pClienteId, plantaoId, hospitalId = (Guid?)plantao.hospital_id, especialidadeId = (Guid?)plantao.especialidade_id, score = medicos.Length == 0 ? 0 : medicos.Average(x => x.Score), uid, criterios = JsonSerializer.Serialize(new[] { "ativo", "tenant", "especialidade", "disponibilidade", "indisponibilidade", "conflito", "limite", "preferencia" }) });
        foreach (var m in medicos)
        {
            await cn.ExecuteAsync(@"insert into plantaopro.escala_sugestao_medicos(id,cliente_id,sugestao_id,plantao_id,medico_id,score,motivos,alertas,status,reg_date,reg_status) values(gen_random_uuid(),@clienteId,@sugestaoId,@plantaoId,@medicoId,@score,@motivos,@alertas,'SUGERIDO',now(),'A')", new { clienteId = pClienteId == Guid.Empty ? clienteId : pClienteId, sugestaoId, plantaoId, medicoId = m.MedicoId, score = m.Score, motivos = m.Motivos, alertas = m.Alertas });
        }
        await cn.ExecuteAsync("insert into plantaopro.escala_sugestao_historico(id,cliente_id,sugestao_id,plantao_id,acao,detalhes,usuario_id,reg_date,reg_status) values(gen_random_uuid(),@clienteId,@sugestaoId,@plantaoId,'GERAR',cast(@detalhes as jsonb),@uid,now(),'A')", new { clienteId = pClienteId == Guid.Empty ? clienteId : pClienteId, sugestaoId, plantaoId, detalhes = JsonSerializer.Serialize(new { total = medicos.Length }), uid });
        if (uid.HasValue) await audit.LogAsync(uid, "CREATE", "escala_sugestoes", sugestaoId, "Sugestão inteligente gerada", ip: ip, userAgent: ua);
        return ApiResponse<EscalaSugestaoDto>.Ok(new EscalaSugestaoDto { Id = sugestaoId, PlantaoId = plantaoId, HospitalNome = (string)plantao.hospital_nome, EspecialidadeNome = (string)plantao.especialidade_nome, DataInicio = (DateTime)plantao.data_inicio, DataFim = (DateTime)plantao.data_fim, Status = "GERADA", Medicos = medicos }, "Sugestão gerada.");
    }

    public async Task<ApiResponse<EscalaSugestaoDto>> ObterSugestoesAsync(Guid plantaoId, Guid? clienteId)
    {
        await using var cn = Cn();
        var s = await cn.QueryFirstOrDefaultAsync<dynamic>(@"select s.id, s.plantao_id, s.status, p.data_inicio, p.data_fim, coalesce(h.nome_fantasia,h.razao_social,'') hospital_nome, coalesce(e.nome,'') especialidade_nome from plantaopro.escala_sugestoes s join plantaopro.plantoes p on p.id=s.plantao_id left join plantaopro.hospitais h on h.id=p.hospital_id left join plantaopro.especialidades e on e.id=p.especialidade_id where s.plantao_id=@plantaoId and s.reg_status='A' and (@clienteId is null or s.cliente_id=@clienteId) order by s.reg_date desc limit 1", new { plantaoId, clienteId });
        if (s is null) return await GerarSugestaoAsync(plantaoId, null, clienteId, null, null);
        var medicos = await cn.QueryAsync<Fase4MedicoDisponivelDto>(@"select m.id as ""MedicoId"", coalesce(m.nome,'') as ""Nome"", coalesce(m.crm,'') as ""Crm"", coalesce(e.nome,'') as ""Especialidade"", true as ""Disponivel"", false as ""Indisponivel"", false as ""PossuiConflito"", sm.score as ""Score"", coalesce(sm.motivos,'') as ""Motivos"", coalesce(sm.alertas,'') as ""Alertas"" from plantaopro.escala_sugestao_medicos sm join plantaopro.medicos m on m.id=sm.medico_id left join plantaopro.especialidades e on e.id=m.especialidade_id where sm.sugestao_id=@id and sm.reg_status='A' order by sm.score desc", new { id = (Guid)s.id });
        return ApiResponse<EscalaSugestaoDto>.Ok(new EscalaSugestaoDto { Id = (Guid)s.id, PlantaoId = plantaoId, HospitalNome = (string)s.hospital_nome, EspecialidadeNome = (string)s.especialidade_nome, DataInicio = (DateTime)s.data_inicio, DataFim = (DateTime)s.data_fim, Status = (string)s.status, Medicos = medicos }, "Sugestões carregadas.");
    }

    public async Task<ApiResponse<string>> ConvidarSugeridosAsync(Guid plantaoId, Guid uid, Guid? clienteId, string? ip, string? ua)
    {
        var sugestao = await ObterSugestoesAsync(plantaoId, clienteId);
        if (!sugestao.Success || sugestao.Data is null) return ApiResponse<string>.Fail(sugestao.Message, sugestao.StatusCode);
        await using var cn = Cn();
        var total = 0;
        foreach (var medico in sugestao.Data.Medicos.Take(10))
        {
            var exists = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.convites where plantao_id=@plantaoId and medico_id=@medicoId and reg_status='A'", new { plantaoId, medicoId = medico.MedicoId });
            if (exists > 0) continue;
            await cn.ExecuteAsync("insert into plantaopro.convites(id,plantao_id,medico_id,status,mensagem,created_by,reg_date,reg_status) values(gen_random_uuid(),@plantaoId,@medicoId,'ENVIADO','Convite gerado por sugestão inteligente.',@uid,now(),'A')", new { plantaoId, medicoId = medico.MedicoId, uid });
            total++;
        }
        await audit.LogAsync(uid, "CREATE", "convites", plantaoId, "Convites enviados para médicos sugeridos", valorNovo: total.ToString(), ip: ip, userAgent: ua);
        return ApiResponse<string>.Ok("ok", total + " convite(s) enviados.");
    }

    public async Task<ApiResponse<string>> RegistrarFeedbackSugestaoAsync(Guid id, Guid uid, SugestaoFeedbackRequest request, string? ip, string? ua)
    {
        if (string.IsNullOrWhiteSpace(request.Feedback)) return ApiResponse<string>.Fail("Informe o feedback da sugestão.");
        await using var cn = Cn();
        var clienteId = await cn.ExecuteScalarAsync<Guid?>("select cliente_id from plantaopro.escala_sugestoes where id=@id and reg_status='A'", new { id });
        await cn.ExecuteAsync("insert into plantaopro.escala_sugestao_feedback(id,cliente_id,sugestao_id,medico_id,feedback,observacao,usuario_id,reg_date,reg_status) values(gen_random_uuid(),@clienteId,@id,@medicoId,@feedback,@observacao,@uid,now(),'A')", new { clienteId, id, medicoId = request.MedicoId, feedback = request.Feedback.Trim().ToUpperInvariant(), observacao = request.Observacao, uid });
        await audit.LogAsync(uid, "UPDATE", "escala_sugestoes", id, "Feedback registrado", ip: ip, userAgent: ua);
        return ApiResponse<string>.Ok("ok", "Feedback registrado.");
    }

    public async Task<ApiResponse<Guid>> SolicitarSubstituicaoAsync(Guid uid, SolicitarSubstituicaoRequest request, string? ip, string? ua)
    {
        if (request.PlantaoId == Guid.Empty || !request.EscalaId.HasValue || request.EscalaId == Guid.Empty)
            return ApiResponse<Guid>.Fail("Selecione uma atribuição válida.");
        if (string.IsNullOrWhiteSpace(request.Motivo) || request.Motivo.Trim().Length < 10)
            return ApiResponse<Guid>.Fail("Informe uma justificativa com pelo menos 10 caracteres.");

        await using var cn = Cn();
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var medico = await ObterMedicoDoUsuarioAsync(cn, uid);
            if (medico.Id == Guid.Empty) return ApiResponse<Guid>.Fail("Vínculo profissional ativo não encontrado.", 403);
            var origem = await cn.QueryFirstOrDefaultAsync<(Guid EscalaId, Guid PlantaoId, Guid? ClienteId, string Status, DateTime Inicio)>(@"select e.id as EscalaId,p.id as PlantaoId,p.cliente_id as ClienteId,e.status,p.data_inicio as Inicio
from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id and p.reg_status='A'
where e.id=@escalaId and e.plantao_id=@plantaoId and e.medico_id=@medicoId and e.reg_status='A'
  and lower(e.status) in ('confirmado','confirmada') and p.cliente_id=@clienteId for update of e,p",
                new { escalaId=request.EscalaId, plantaoId=request.PlantaoId, medicoId=medico.Id, clienteId=medico.ClienteId }, tx);
            if (origem.EscalaId == Guid.Empty) return ApiResponse<Guid>.Fail("A atribuição não pertence ao profissional ou não está confirmada.", 403);
            if (origem.Inicio <= DateTime.UtcNow) return ApiResponse<Guid>.Fail("Plantão iniciado deve seguir o fluxo de ocorrência/passagem de responsabilidade.", 409);
            var bloqueada = await cn.ExecuteScalarAsync<bool>(@"select exists(select 1 from plantaopro.medico_checkins where escala_id=@escalaId)
 or exists(select 1 from plantaopro.pagamentos where escala_id=@escalaId and reg_status='A')", new { escalaId=origem.EscalaId }, tx);
            if (bloqueada) return ApiResponse<Guid>.Fail("A atribuição possui presença ou financeiro consolidado; solicite ajuste operacional.", 409);
            var ativa = await cn.ExecuteScalarAsync<bool>(@"select exists(select 1 from plantaopro.substituicoes_plantao where escala_id=@escalaId and reg_status='A' and status in ('SOLICITADA','APROVADA','SUBSTITUTO_CONVIDADO','AGUARDANDO_APROVACAO'))", new { escalaId=origem.EscalaId }, tx);
            if (ativa) return ApiResponse<Guid>.Fail("Já existe uma solicitação ativa para esta atribuição.", 409);

            var id = Guid.NewGuid();
            await cn.ExecuteAsync(@"insert into plantaopro.substituicoes_plantao(id,cliente_id,plantao_id,escala_id,medico_solicitante_id,motivo,status,versao,created_by,reg_date,reg_status)
values(@id,@clienteId,@plantaoId,@escalaId,@medicoId,@motivo,'SOLICITADA',1,@uid,now(),'A')", new { id, clienteId=origem.ClienteId, plantaoId=origem.PlantaoId, escalaId=origem.EscalaId, medicoId=medico.Id, motivo=request.Motivo.Trim(), uid }, tx);
            await InserirHistoricoSubstituicaoAsync(cn, origem.ClienteId, id, "SOLICITAR", string.Empty, "SOLICITADA", "Responsabilidade permanece com o profissional original até a efetivação. " + request.Motivo.Trim(), uid, tx);
            await tx.CommitAsync();
            await audit.LogAsync(uid, "CREATE", "substituicoes_plantao", id, "Profissional solicitou substituição; atribuição original preservada", ip: ip, userAgent: ua);
            return ApiResponse<Guid>.Ok(id, "Solicitação aberta. Você permanece responsável até a cobertura ser efetivada.");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation || ex.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            await tx.RollbackAsync();
            return ApiResponse<Guid>.Fail("A solicitação foi alterada em outra sessão. Atualize antes de tentar novamente.", 409);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            logger.LogError(ex, "Erro ao solicitar substituição da escala {EscalaId}", request.EscalaId);
            return ApiResponse<Guid>.Fail("Não foi possível registrar a solicitação.", 500);
        }
    }

    public async Task<ApiResponse<IEnumerable<SubstituicaoDto>>> ListarSubstituicoesDoMedicoAsync(Guid uid)
    {
        await using var cn = Cn();
        var medico = await ObterMedicoDoUsuarioAsync(cn, uid);
        if (medico.Id == Guid.Empty) return ApiResponse<IEnumerable<SubstituicaoDto>>.Fail("Médico não encontrado para o usuário autenticado.", 404);
        var rows = await cn.QueryAsync<SubstituicaoDto>(@"select s.id as ""Id"", s.plantao_id as ""PlantaoId"", s.escala_id as ""EscalaId"", s.medico_solicitante_id as ""MedicoSolicitanteId"", s.motivo as ""Motivo"", s.status as ""Status"", s.reg_date as ""RegDate""
from plantaopro.substituicoes_plantao s
where s.medico_solicitante_id=@medicoId and s.cliente_id=@clienteId and s.reg_status='A'
order by s.reg_date desc", new { medicoId = medico.Id, clienteId = medico.ClienteId });
        return ApiResponse<IEnumerable<SubstituicaoDto>>.Ok(rows, "Substituições carregadas.");
    }

    public async Task<ApiResponse<IEnumerable<SubstituicaoDto>>> ListarSubstituicoesAsync(Guid? clienteId)
    {
        await using var cn = Cn();
        var rows = await cn.QueryAsync<SubstituicaoDto>(@"select s.id as ""Id"", s.plantao_id as ""PlantaoId"", s.escala_id as ""EscalaId"", s.medico_solicitante_id as ""MedicoSolicitanteId"", s.medico_substituto_id as ""MedicoSubstitutoId"", coalesce(m.nome,'') as ""MedicoSolicitanteNome"",p.hospital_id as ""HospitalId"",coalesce(h.nome_fantasia,h.razao_social,'') as ""HospitalNome"",coalesce(es.nome,'') as ""EspecialidadeNome"",p.data_inicio as ""DataInicio"",p.data_fim as ""DataFim"", coalesce(s.motivo,'') as ""Motivo"", coalesce(s.status,'') as ""Status"", s.reg_date as ""RegDate"",s.versao as ""Versao"" from plantaopro.substituicoes_plantao s join plantaopro.medicos m on m.id=s.medico_solicitante_id join plantaopro.plantoes p on p.id=s.plantao_id and p.reg_status='A' join plantaopro.hospitais h on h.id=p.hospital_id and h.reg_status='A' join plantaopro.especialidades es on es.id=p.especialidade_id and es.reg_status='A' where s.reg_status='A' and (@clienteId is null or s.cliente_id=@clienteId) order by s.reg_date desc,s.id desc limit 200", new { clienteId });
        return ApiResponse<IEnumerable<SubstituicaoDto>>.Ok(rows, "Substituições listadas.");
    }

    public async Task<ApiResponse<SubstituicaoDto>> ObterSubstituicaoAsync(Guid id, Guid? clienteId)
    {
        await using var cn = Cn();
        var row = await cn.QueryFirstOrDefaultAsync<SubstituicaoDto>(@"select s.id as ""Id"",s.plantao_id as ""PlantaoId"",s.escala_id as ""EscalaId"",s.medico_solicitante_id as ""MedicoSolicitanteId"",s.medico_substituto_id as ""MedicoSubstitutoId"",coalesce(m.nome,'') as ""MedicoSolicitanteNome"",p.hospital_id as ""HospitalId"",coalesce(h.nome_fantasia,h.razao_social,'') as ""HospitalNome"",coalesce(es.nome,'') as ""EspecialidadeNome"",p.data_inicio as ""DataInicio"",p.data_fim as ""DataFim"",coalesce(s.motivo,'') as ""Motivo"",coalesce(s.status,'') as ""Status"",s.reg_date as ""RegDate"",s.versao as ""Versao""
from plantaopro.substituicoes_plantao s
join plantaopro.medicos m on m.id=s.medico_solicitante_id
join plantaopro.plantoes p on p.id=s.plantao_id and p.reg_status='A'
join plantaopro.hospitais h on h.id=p.hospital_id and h.reg_status='A'
join plantaopro.especialidades es on es.id=p.especialidade_id and es.reg_status='A'
where s.id=@id and s.reg_status='A' and (@clienteId is null or s.cliente_id=@clienteId)", new { id, clienteId });
        return row is null ? ApiResponse<SubstituicaoDto>.Fail("Substituição não encontrada.", 404) : ApiResponse<SubstituicaoDto>.Ok(row, "Substituição carregada.");
    }

    public async Task<ApiResponse<string>> MudarStatusSubstituicaoAsync(Guid id, Guid uid, Guid? clienteId, string acao, string novoStatus, string justificativa, long versaoEsperada, string? ip, string? ua)
    {
        if (versaoEsperada <= 0) return ApiResponse<string>.Fail("A versão atual da solicitação é obrigatória.", 422);
        justificativa = justificativa?.Trim() ?? string.Empty;
        if (justificativa.Length is < 3 or > 1000) return ApiResponse<string>.Fail("A justificativa deve conter entre 3 e 1000 caracteres.", 422);
        await using var cn = Cn();
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var atual = await cn.QueryFirstOrDefaultAsync<(Guid? ClienteId, string Status, long Versao)>("select cliente_id, status, versao from plantaopro.substituicoes_plantao where id=@id and reg_status='A' and (@clienteId is null or cliente_id=@clienteId) for update", new { id, clienteId }, tx);
        if (string.IsNullOrWhiteSpace(atual.Status)) return ApiResponse<string>.Fail("Substituição não encontrada.", 404);
        if (atual.Status is not "SOLICITADA") return ApiResponse<string>.Fail("A solicitação já foi decidida ou avançou para outra etapa. Consulte o resultado atual.", 409);
        if (atual.Versao != versaoEsperada) return ApiResponse<string>.Fail("A solicitação foi alterada durante a análise. Revise antes de decidir.", 409);
        var changed = await cn.ExecuteAsync("update plantaopro.substituicoes_plantao set status=@novoStatus, responsavel_usuario_id=@uid, updated_by=@uid, reg_update=now(),versao=versao+1 where id=@id and status='SOLICITADA' and versao=@versaoEsperada", new { id, novoStatus, uid, versaoEsperada }, tx);
        if (changed != 1) return ApiResponse<string>.Fail("A solicitação foi alterada durante a análise. Revise antes de decidir.", 409);
        await cn.ExecuteAsync("insert into plantaopro.substituicao_aprovacoes(id,cliente_id,substituicao_id,aprovador_usuario_id,decisao,justificativa,reg_date,reg_status) values(gen_random_uuid(),@clienteId,@id,@uid,@novoStatus,@justificativa,now(),'A')", new { clienteId = atual.ClienteId, id, uid, novoStatus, justificativa }, tx);
        await InserirHistoricoSubstituicaoAsync(cn, atual.ClienteId, id, acao, atual.Status, novoStatus, justificativa, uid, tx);
        await tx.CommitAsync();
        await audit.LogAsync(uid, "UPDATE", "substituicoes_plantao", id, acao, ip: ip, userAgent: ua);
        return ApiResponse<string>.Ok("ok", "Substituição atualizada.");
    }

    public async Task<ApiResponse<string>> ConvidarSubstitutoAsync(Guid id, Guid uid, Guid? clienteId, ConvidarSubstitutoRequest request, string? ip, string? ua)
    {
        if (request.MedicoId == Guid.Empty) return ApiResponse<string>.Fail("Informe o médico substituto.");
        await using var cn = Cn();
        var s = await cn.QueryFirstOrDefaultAsync<(Guid PlantaoId, Guid? ClienteId, string Status)>("select plantao_id, cliente_id, status from plantaopro.substituicoes_plantao where id=@id and reg_status='A' and (@clienteId is null or cliente_id=@clienteId)", new { id, clienteId });
        if (s.PlantaoId == Guid.Empty) return ApiResponse<string>.Fail("Substituição não encontrada.", 404);
        await cn.ExecuteAsync("insert into plantaopro.substituicao_candidatos(id,cliente_id,substituicao_id,medico_id,status,motivos,reg_date,reg_status) values(gen_random_uuid(),@clienteId,@id,@medicoId,'CONVIDADO',@msg,now(),'A')", new { clienteId = s.ClienteId, id, medicoId = request.MedicoId, msg = request.Mensagem });
        await cn.ExecuteAsync("update plantaopro.substituicoes_plantao set medico_substituto_id=@medicoId,status='SUBSTITUTO_CONVIDADO',updated_by=@uid,reg_update=now() where id=@id", new { id, request.MedicoId, uid });
        await InserirHistoricoSubstituicaoAsync(cn, s.ClienteId, id, "CONVIDAR_SUBSTITUTO", s.Status, "SUBSTITUTO_CONVIDADO", request.Mensagem, uid);
        await audit.LogAsync(uid, "CREATE", "substituicao_candidatos", id, "Substituto convidado", ip: ip, userAgent: ua);
        return ApiResponse<string>.Ok("ok", "Substituto convidado.");
    }

    public async Task<ApiResponse<string>> ConfirmarSubstitutoAsync(Guid id, Guid uid, Guid? clienteId, ConfirmarSubstitutoRequest request, string? ip, string? ua)
    {
        if (request.MedicoId == Guid.Empty || request.VersaoEsperada <= 0) return ApiResponse<string>.Fail("Candidato e versão atual são obrigatórios.");
        await using var cn = Cn();
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            await cn.ExecuteAsync("select pg_advisory_xact_lock(hashtextextended(@key, 2170))", new { key=request.MedicoId.ToString("N") }, tx);
            var s = await cn.QueryFirstOrDefaultAsync<(Guid PlantaoId, Guid? EscalaId, Guid? ClienteId, Guid SolicitanteId, Guid? SubstitutoId, string Status, long Versao)>(@"select plantao_id as PlantaoId,escala_id as EscalaId,cliente_id as ClienteId,medico_solicitante_id as SolicitanteId,medico_substituto_id as SubstitutoId,status,versao
from plantaopro.substituicoes_plantao where id=@id and reg_status='A' and cliente_id=@clienteId for update", new { id, clienteId }, tx);
            if (s.PlantaoId == Guid.Empty) return ApiResponse<string>.Fail("Solicitação não encontrada no cliente ativo.", 404);
            if (s.Status == "CONFIRMADA" && s.SubstitutoId == request.MedicoId) { await tx.CommitAsync(); return ApiResponse<string>.Ok("ok", "Cobertura já efetivada anteriormente."); }
            if (s.Versao != request.VersaoEsperada) return ApiResponse<string>.Fail("A solicitação mudou. Recarregue antes de aprovar.", 409);
            if (s.Status is not ("SUBSTITUTO_CONVIDADO" or "AGUARDANDO_APROVACAO" or "APROVADA")) return ApiResponse<string>.Fail("A solicitação não está pronta para efetivação.", 409);
            if (!s.EscalaId.HasValue) return ApiResponse<string>.Fail("A atribuição original não foi identificada.", 409);

            var origem = await cn.QueryFirstOrDefaultAsync<(string Status, DateTime Inicio, DateTime Fim)>(@"select e.status,p.data_inicio as Inicio,p.data_fim as Fim from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id where e.id=@escalaId and e.plantao_id=@plantaoId and e.medico_id=@solicitante and e.reg_status='A' for update of e,p", new { escalaId=s.EscalaId, plantaoId=s.PlantaoId, solicitante=s.SolicitanteId }, tx);
            if (string.IsNullOrWhiteSpace(origem.Status) || origem.Inicio <= DateTime.UtcNow) return ApiResponse<string>.Fail("A atribuição original mudou ou o plantão já iniciou.", 409);
            var elegivel = await cn.ExecuteScalarAsync<bool>(@"select exists(select 1 from plantaopro.medicos m where m.id=@medicoId and m.cliente_id=@clienteId and m.reg_status='A' and coalesce(m.bloqueado,false)=false)
 and exists(select 1 from plantaopro.medico_disponibilidades d where d.medico_id=@medicoId and d.reg_status='A' and d.status='ATIVA' and d.data_inicio<=@inicio and d.data_fim>=@fim)
 and not exists(select 1 from plantaopro.medico_indisponibilidades d where d.medico_id=@medicoId and d.reg_status='A' and d.status='ATIVA' and d.data_inicio<@fim and d.data_fim>@inicio)
 and not exists(select 1 from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id where e.medico_id=@medicoId and e.reg_status='A' and lower(e.status) in ('solicitado','solicitada','confirmado','confirmada','em_andamento') and p.data_inicio<@fim and p.data_fim>@inicio)", new { request.MedicoId, clienteId=s.ClienteId, origem.Inicio, origem.Fim }, tx);
            if (!elegivel) return ApiResponse<string>.Fail("O candidato não está mais elegível ou não possui disponibilidade confirmada.", 409);
            var bloqueada = await cn.ExecuteScalarAsync<bool>(@"select exists(select 1 from plantaopro.medico_checkins where escala_id=@escalaId) or exists(select 1 from plantaopro.pagamentos where escala_id=@escalaId and reg_status='A')", new { escalaId=s.EscalaId }, tx);
            if (bloqueada) return ApiResponse<string>.Fail("Presença ou financeiro já registrado impede substituição direta.", 409);

            var novaEscalaId = Guid.NewGuid();
            await cn.ExecuteAsync(@"insert into plantaopro.escalas(id,plantao_id,medico_id,status,justificativa,created_by,reg_status,reg_date)
values(@id,@plantaoId,@medicoId,'confirmado',@justificativa,@uid,'A',now())", new { id=novaEscalaId, plantaoId=s.PlantaoId, medicoId=request.MedicoId, justificativa="Cobertura da solicitação " + id, uid }, tx);
            await cn.ExecuteAsync("update plantaopro.escalas set status='substituido',justificativa=@justificativa,updated_by=@uid,reg_update=now() where id=@escalaId", new { escalaId=s.EscalaId, justificativa="Substituída pela escala " + novaEscalaId, uid }, tx);
            var changed = await cn.ExecuteAsync(@"update plantaopro.substituicoes_plantao set medico_substituto_id=@medicoId,nova_escala_id=@novaEscalaId,status='CONFIRMADA',versao=versao+1,updated_by=@uid,reg_update=now() where id=@id and versao=@versao", new { request.MedicoId, novaEscalaId, uid, id, versao=request.VersaoEsperada }, tx);
            if (changed != 1) return ApiResponse<string>.Fail("A solicitação mudou em outra sessão.", 409);
            await cn.ExecuteAsync("update plantaopro.substituicao_candidatos set status=case when medico_id=@medicoId then 'EFETIVADO' else 'REVOGADO' end,updated_by=@uid,reg_update=now() where substituicao_id=@id and reg_status='A' and status in ('CONVIDADO','ACEITO')", new { id, request.MedicoId, uid }, tx);
            await InserirHistoricoSubstituicaoAsync(cn, s.ClienteId, id, "EFETIVAR", s.Status, "CONFIRMADA", request.Observacao ?? "Cobertura efetivada; presença e financeiro não foram transferidos.", uid, tx);
            await tx.CommitAsync();
            await audit.LogAsync(uid, "EFETIVAR", "substituicoes_plantao", id, "Nova atribuição criada e original encerrada", ip: ip, userAgent: ua);
            return ApiResponse<string>.Ok(novaEscalaId.ToString(), "Cobertura efetivada sem transferir presença ou financeiro.");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation || ex.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            await tx.RollbackAsync();
            return ApiResponse<string>.Fail("A cobertura foi atualizada simultaneamente. Consulte o resultado antes de reenviar.", 409);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(); logger.LogError(ex, "Erro ao efetivar substituição {Id}", id);
            return ApiResponse<string>.Fail("Não foi possível efetivar. Consulte o histórico antes de tentar novamente.", 500);
        }
    }

    public async Task<ApiResponse<IEnumerable<object>>> HistoricoSubstituicaoAsync(Guid id, Guid? clienteId)
    {
        await using var cn = Cn();
        var rows = await cn.QueryAsync<object>("select id, acao, status_anterior, status_novo, observacao, usuario_id, reg_date from plantaopro.substituicao_historico where substituicao_id=@id and reg_status='A' and (@clienteId is null or cliente_id=@clienteId) order by reg_date", new { id, clienteId });
        return ApiResponse<IEnumerable<object>>.Ok(rows, "Histórico carregado.");
    }

    public async Task<ApiResponse<IEnumerable<PendenciaOperacionalDto>>> ListarPendenciasAsync(Guid? clienteId, Guid? responsavelId = null)
    {
        await using var cn = Cn();
        var rows = await cn.QueryAsync<PendenciaOperacionalDto>(@"select id as ""Id"", coalesce(tipo,'') as ""Tipo"", coalesce(titulo,'') as ""Titulo"", coalesce(descricao,'') as ""Descricao"", coalesce(prioridade,'') as ""Prioridade"", coalesce(status,'') as ""Status"", prazo as ""Prazo"", responsavel_usuario_id as ""ResponsavelUsuarioId"", reg_date as ""RegDate"" from plantaopro.pendencias_operacionais where reg_status='A' and (@clienteId is null or cliente_id=@clienteId) and (@responsavelId is null or responsavel_usuario_id=@responsavelId) order by case prioridade when 'CRITICA' then 0 when 'ALTA' then 1 else 2 end, prazo nulls last, reg_date desc limit 300", new { clienteId, responsavelId });
        return ApiResponse<IEnumerable<PendenciaOperacionalDto>>.Ok(rows, "Pendências listadas.");
    }

    public async Task<ApiResponse<PendenciasResumoDto>> ResumoPendenciasAsync(Guid? clienteId, Guid? uid)
    {
        await using var cn = Cn();
        var dto = await cn.QueryFirstAsync<PendenciasResumoDto>(@"select count(1) filter(where status in ('ABERTA','ADIADA')) as ""Abertas"", count(1) filter(where prioridade='CRITICA' and status in ('ABERTA','ADIADA')) as ""Criticas"", count(1) filter(where prazo < now() and status in ('ABERTA','ADIADA')) as ""Vencidas"", count(1) filter(where responsavel_usuario_id=@uid and status in ('ABERTA','ADIADA')) as ""Minhas"" from plantaopro.pendencias_operacionais where reg_status='A' and (@clienteId is null or cliente_id=@clienteId)", new { clienteId, uid });
        return ApiResponse<PendenciasResumoDto>.Ok(dto, "Resumo de pendências carregado.");
    }

    public async Task<ApiResponse<Guid>> CriarPendenciaAsync(Guid uid, Guid? clienteId, CriarPendenciaRequest request, string? ip, string? ua)
    {
        if (string.IsNullOrWhiteSpace(request.Titulo) || string.IsNullOrWhiteSpace(request.Tipo)) return ApiResponse<Guid>.Fail("Informe tipo e título da pendência.");
        await using var cn = Cn();
        var id = await CriarPendenciaInternaAsync(cn, clienteId, request.Tipo, request.Titulo, request.Descricao, request.Prioridade, request.ResponsavelUsuarioId, request.Entidade, request.EntidadeId, uid, request.Prazo);
        await audit.LogAsync(uid, "CREATE", "pendencias_operacionais", id, "Pendência operacional criada", ip: ip, userAgent: ua);
        return ApiResponse<Guid>.Ok(id, "Pendência criada.");
    }

    public async Task<ApiResponse<string>> AtualizarPendenciaAsync(Guid id, Guid uid, Guid? clienteId, string acao, string status, string observacao, Guid? responsavel, DateTime? prazo, string? ip, string? ua)
    {
        if (status == "RESOLVIDA" && string.IsNullOrWhiteSpace(observacao)) return ApiResponse<string>.Fail("Resolver pendência exige observação.");
        await using var cn = Cn();
        var atual = await cn.QueryFirstOrDefaultAsync<(Guid? ClienteId, string Status)>("select cliente_id, status from plantaopro.pendencias_operacionais where id=@id and reg_status='A' and (@clienteId is null or cliente_id=@clienteId)", new { id, clienteId });
        if (string.IsNullOrWhiteSpace(atual.Status)) return ApiResponse<string>.Fail("Pendência não encontrada.", 404);
        await cn.ExecuteAsync("update plantaopro.pendencias_operacionais set status=@status, responsavel_usuario_id=coalesce(@responsavel,responsavel_usuario_id), prazo=coalesce(@prazo,prazo), updated_by=@uid, reg_update=now() where id=@id", new { id, status, responsavel, prazo, uid });
        await cn.ExecuteAsync("insert into plantaopro.pendencia_historico(id,cliente_id,pendencia_id,acao,status_anterior,status_novo,observacao,usuario_id,reg_date,reg_status) values(gen_random_uuid(),@clienteId,@id,@acao,@anterior,@status,@observacao,@uid,now(),'A')", new { clienteId = atual.ClienteId, id, acao, anterior = atual.Status, status, observacao, uid });
        await audit.LogAsync(uid, "UPDATE", "pendencias_operacionais", id, acao, ip: ip, userAgent: ua);
        return ApiResponse<string>.Ok("ok", "Pendência atualizada.");
    }

    public async Task<ApiResponse<string>> RecalcularPendenciasAsync(Guid uid, Guid? clienteId, string? ip, string? ua)
    {
        await using var cn = Cn();
        var plantoes = await cn.QueryAsync<(Guid Id, Guid? ClienteId, string Hospital)>(@"select p.id, p.cliente_id, coalesce(h.nome_fantasia,h.razao_social,'') from plantaopro.plantoes p left join plantaopro.hospitais h on h.id=p.hospital_id where p.reg_status='A' and p.status in ('aberto','publicado') and p.vagas_disponiveis>0 and p.data_inicio between now() and now()+ interval '14 days' and (@clienteId is null or p.cliente_id=@clienteId) limit 100", new { clienteId });
        var total = 0;
        foreach (var p in plantoes)
        {
            var exists = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.pendencias_operacionais where entidade='plantoes' and entidade_id=@id and status in ('ABERTA','ADIADA') and reg_status='A'", new { id = p.Id });
            if (exists == 0)
            {
                await CriarPendenciaInternaAsync(cn, p.ClienteId, "PLANTAO_SEM_MEDICO", "Plantão sem médico", p.Hospital, "ALTA", null, "plantoes", p.Id, uid);
                total++;
            }
        }
        await audit.LogAsync(uid, "UPDATE", "pendencias_operacionais", null, "Pendências recalculadas", valorNovo: total.ToString(), ip: ip, userAgent: ua);
        return ApiResponse<string>.Ok("ok", total + " pendência(s) recalculadas.");
    }

    public async Task<ApiResponse<RelatorioResumoDto>> RelatorioAsync(string tipo, Guid? clienteId, DateTime? inicio, DateTime? fim)
    {
        await using var cn = Cn();
        var di = inicio ?? DateTime.UtcNow.Date.AddDays(-30);
        var df = fim ?? DateTime.UtcNow.Date.AddDays(1);
        var dto = await cn.QueryFirstAsync<RelatorioResumoDto>(@"select @tipo as ""Tipo"", (select count(1) from plantaopro.plantoes where reg_status='A' and data_inicio between @di and @df and (@clienteId is null or cliente_id=@clienteId)) as ""TotalPlantoes"", (select count(1) from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id where e.reg_status='A' and p.data_inicio between @di and @df and (@clienteId is null or p.cliente_id=@clienteId)) as ""TotalEscalas"", (select count(1) from plantaopro.convites c join plantaopro.plantoes p on p.id=c.plantao_id where c.reg_status='A' and p.data_inicio between @di and @df and (@clienteId is null or p.cliente_id=@clienteId)) as ""TotalConvites"", (select count(1) from plantaopro.substituicoes_plantao where reg_status='A' and reg_date between @di and @df and (@clienteId is null or cliente_id=@clienteId)) as ""TotalSubstituicoes"", (select count(1) from plantaopro.pagamentos where reg_status='A' and reg_date between @di and @df and (@clienteId is null or cliente_id=@clienteId)) as ""TotalPagamentos"", (select coalesce(sum(valor_previsto),0) from plantaopro.pagamentos where reg_status='A' and reg_date between @di and @df and (@clienteId is null or cliente_id=@clienteId)) as ""ValorTotal"" ", new { tipo, clienteId, di, df });
        var linhas = await cn.QueryAsync(@"select coalesce(h.nome_fantasia,h.razao_social,'Sem hospital') as hospital, coalesce(e.nome,'Sem especialidade') as especialidade, count(1) as total from plantaopro.plantoes p left join plantaopro.hospitais h on h.id=p.hospital_id left join plantaopro.especialidades e on e.id=p.especialidade_id where p.reg_status='A' and p.data_inicio between @di and @df and (@clienteId is null or p.cliente_id=@clienteId) group by hospital, especialidade order by total desc limit 100", new { clienteId, di, df });
        dto.Linhas = linhas.Select(r => (IDictionary<string, object>)r).ToArray();
        return ApiResponse<RelatorioResumoDto>.Ok(dto, "Relatório carregado.");
    }

    public async Task<byte[]> ExportarCsvAsync(string tipo, Guid? clienteId, Guid? uid, DateTime? inicio, DateTime? fim)
    {
        var rel = await RelatorioAsync(tipo, clienteId, inicio, fim);
        var csv = new StringBuilder();
        csv.AppendLine("Tipo;TotalPlantoes;TotalEscalas;TotalConvites;TotalSubstituicoes;TotalPagamentos;ValorTotal");
        var d = rel.Data ?? new RelatorioResumoDto { Tipo = tipo };
        csv.AppendLine($"{SanitizarCsv(d.Tipo)};{d.TotalPlantoes};{d.TotalEscalas};{d.TotalConvites};{d.TotalSubstituicoes};{d.TotalPagamentos};{d.ValorTotal}");
        await using var cn = Cn();
        await cn.ExecuteAsync("insert into plantaopro.relatorios_exportacoes(id,cliente_id,tipo,formato,arquivo_nome,exportado_por,total_linhas,reg_date,reg_status) values(gen_random_uuid(),@clienteId,@tipo,'CSV',@arquivo,@uid,1,now(),'A')", new { clienteId, tipo, arquivo = "relatorio-" + tipo + ".csv", uid });
        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<ApiResponse<Guid>> SalvarFiltroAsync(Guid uid, Guid? clienteId, SalvarFiltroRelatorioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome) || string.IsNullOrWhiteSpace(request.Tipo)) return ApiResponse<Guid>.Fail("Informe nome e tipo do filtro.");
        await using var cn = Cn();
        var id = Guid.NewGuid();
        await cn.ExecuteAsync("insert into plantaopro.relatorios_filtros_salvos(id,cliente_id,usuario_id,nome,tipo,filtros,status,reg_date,reg_status) values(@id,@clienteId,@uid,@nome,@tipo,cast(@filtros as jsonb),'ATIVO',now(),'A')", new { id, clienteId, uid, nome = request.Nome.Trim(), tipo = request.Tipo.Trim().ToUpperInvariant(), filtros = JsonSerializer.Serialize(request.Filtros ?? new Dictionary<string, string>()) });
        return ApiResponse<Guid>.Ok(id, "Filtro salvo.");
    }

    public async Task<ApiResponse<IEnumerable<object>>> ListarFiltrosAsync(Guid uid, Guid? clienteId)
    {
        await using var cn = Cn();
        var rows = await cn.QueryAsync<object>("select id, nome, tipo, filtros, reg_date from plantaopro.relatorios_filtros_salvos where usuario_id=@uid and reg_status='A' and (@clienteId is null or cliente_id=@clienteId) order by reg_date desc limit 100", new { uid, clienteId });
        return ApiResponse<IEnumerable<object>>.Ok(rows, "Filtros salvos listados.");
    }

    private static string? ValidarPeriodo(DateTime inicio, DateTime fim, int? maximoDias = null)
    {
        if (inicio == default || fim == default) return "Informe data inicial e final.";
        if (fim <= inicio) return "Data final deve ser maior que a data inicial.";
        if (maximoDias.HasValue && fim - inicio > TimeSpan.FromDays(maximoDias.Value)) return $"O período de consulta deve ter no máximo {maximoDias.Value} dias.";
        return null;
    }

    private static string? ValidarTexto(string? value, int limite, string campo) => value?.Trim().Length > limite ? $"{campo} deve ter no máximo {limite} caracteres." : null;

    private static async Task<bool> ContextoPermitidoAsync(NpgsqlConnection cn, System.Data.IDbTransaction? tx, Guid? clienteId, Guid? hospitalId, Guid? especialidadeId)
    {
        if (!clienteId.HasValue) return !hospitalId.HasValue && !especialidadeId.HasValue;
        var hospitais = !hospitalId.HasValue || await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.hospitais where id=@hospitalId and cliente_id=@clienteId and reg_status='A'", new { hospitalId, clienteId }, tx) > 0;
        var especialidades = !especialidadeId.HasValue || await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.especialidades where id=@especialidadeId and reg_status='A'", new { especialidadeId }, tx) > 0;
        return hospitais && especialidades;
    }

    private static string Normalizar(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToUpperInvariant();

    private static string SanitizarCsv(string value) => (value ?? string.Empty).Replace(";", ",", StringComparison.OrdinalIgnoreCase).Replace("\r", " ", StringComparison.OrdinalIgnoreCase).Replace("\n", " ", StringComparison.OrdinalIgnoreCase);

    private static Fase4MedicoDisponivelDto AplicarScore(Fase4MedicoDisponivelDto dto)
    {
        decimal score = 50;
        var motivos = new List<string>();
        var alertas = new List<string>();
        if (dto.Disponivel) { score += 25; motivos.Add("Disponível no período"); }
        if (!dto.Indisponivel) { score += 15; motivos.Add("Sem indisponibilidade registrada"); } else alertas.Add("Indisponível no período");
        if (!dto.PossuiConflito) { score += 10; motivos.Add("Sem conflito de escala"); } else alertas.Add("Conflito de agenda");
        dto.Score = Math.Min(100, score);
        dto.Motivos = string.Join("; ", motivos);
        dto.Alertas = string.Join("; ", alertas);
        return dto;
    }

    private static async Task RegistrarHistoricoDisponibilidadeAsync(NpgsqlConnection cn, Guid? clienteId, Guid medicoId, string entidade, Guid entidadeId, string acao, object detalhes, Guid uid, System.Data.IDbTransaction? tx = null)
    {
        await cn.ExecuteAsync("insert into plantaopro.medico_historico_disponibilidade(id,cliente_id,medico_id,entidade,entidade_id,acao,detalhes,usuario_id,reg_date,reg_status) values(gen_random_uuid(),@clienteId,@medicoId,@entidade,@entidadeId,@acao,cast(@detalhes as jsonb),@uid,now(),'A')", new { clienteId, medicoId, entidade, entidadeId, acao, detalhes = JsonSerializer.Serialize(detalhes), uid }, tx);
    }

    private static async Task InserirHistoricoSubstituicaoAsync(NpgsqlConnection cn, Guid? clienteId, Guid substituicaoId, string acao, string? anterior, string novo, string? observacao, Guid uid, System.Data.IDbTransaction? tx = null)
    {
        await cn.ExecuteAsync("insert into plantaopro.substituicao_historico(id,cliente_id,substituicao_id,acao,status_anterior,status_novo,observacao,usuario_id,reg_date,reg_status) values(gen_random_uuid(),@clienteId,@id,@acao,@anterior,@novo,@observacao,@uid,now(),'A')", new { clienteId, id = substituicaoId, acao, anterior, novo, observacao, uid }, tx);
    }

    private static async Task<Guid> CriarPendenciaInternaAsync(NpgsqlConnection cn, Guid? clienteId, string tipo, string titulo, string descricao, string prioridade, Guid? responsavel, string? entidade, Guid? entidadeId, Guid uid, DateTime? prazo = null)
    {
        var id = Guid.NewGuid();
        await cn.ExecuteAsync("insert into plantaopro.pendencias_operacionais(id,cliente_id,tipo,titulo,descricao,prioridade,status,prazo,responsavel_usuario_id,entidade,entidade_id,created_by,reg_date,reg_status) values(@id,@clienteId,@tipo,@titulo,@descricao,@prioridade,'ABERTA',@prazo,@responsavel,@entidade,@entidadeId,@uid,now(),'A')", new { id, clienteId, tipo = Normalizar(tipo, "OPERACIONAL"), titulo, descricao, prioridade = Normalizar(prioridade, "MEDIA"), prazo, responsavel, entidade, entidadeId, uid });
        if (responsavel.HasValue)
            await cn.ExecuteAsync("insert into plantaopro.pendencia_responsaveis(id,cliente_id,pendencia_id,usuario_id,papel,reg_date,reg_status) values(gen_random_uuid(),@clienteId,@id,@responsavel,'RESPONSAVEL',now(),'A')", new { clienteId, id, responsavel });
        return id;
    }
}
