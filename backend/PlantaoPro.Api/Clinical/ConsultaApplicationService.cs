using System.Data;
using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Clinical;

public interface IConsultaRepository
{
    Task<Consulta?> ObterAsync(Guid id, Guid clienteId, CancellationToken ct, IDbTransaction? tx = null);
    Task<ConsultaWorkspaceResponse?> WorkspaceAsync(Guid id, Guid clienteId, CancellationToken ct);
    Task<IReadOnlyList<ConsultaResumoResponse>> FilaAsync(Guid clienteId, Guid? unidadeId, Guid? medicoId, int pagina, int tamanho, CancellationToken ct);
    Task<bool> AlterarStatusAsync(Guid id, Guid clienteId, ConsultaStatus atual, ConsultaStatus destino, int versao, Guid? usuarioId, CancellationToken ct, IDbTransaction? tx = null);
    Task<bool> SalvarRascunhoAsync(Guid id, Guid clienteId, SalvarConsultaRascunhoRequest request, Guid? usuarioId, CancellationToken ct);
    Task<IReadOnlyList<ConsultaCid>> ListarCidsAsync(Guid consultaId, Guid clienteId, CancellationToken ct);
    Task<ConsultaCid?> AdicionarCidAsync(Guid consultaId, Guid clienteId, AdicionarConsultaCidRequest request, Guid? usuarioId, CancellationToken ct);
    Task<bool> RemoverCidAsync(Guid consultaId, Guid consultaCidId, Guid clienteId, Guid? usuarioId, CancellationToken ct);
    NpgsqlConnection AbrirConexao();
}

public sealed class ConsultaRepository : IConsultaRepository
{
    private readonly string connectionString;
    public ConsultaRepository(IConfiguration cfg) => connectionString = cfg.GetConnectionString("Default") ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada.");
    public NpgsqlConnection AbrirConexao() => new(connectionString);
    private static CommandDefinition Cmd(string sql, object? args, IDbTransaction? tx, CancellationToken ct) => new(sql, args, tx, cancellationToken: ct);

    public async Task<Consulta?> ObterAsync(Guid id, Guid clienteId, CancellationToken ct, IDbTransaction? tx = null)
    {
        const string sql = """select id,cliente_id ClienteId,unidade_id UnidadeId,atendimento_id AtendimentoId,agendamento_id AgendamentoId,paciente_id PacienteId,medico_id MedicoId,triagem_id TriagemId,status,coalesce(anamnese,'') Anamnese,coalesce(exame_fisico,'') ExameFisico,coalesce(hipotese_diagnostica,'') HipoteseDiagnostica,coalesce(diagnostico,'') Diagnostico,coalesce(conduta,'') Conduta,coalesce(orientacoes,'') Orientacoes,coalesce(observacoes,'') Observacoes,inicio_em InicioEm,finalizada_em FinalizadaEm,cancelada_em CanceladaEm,motivo_cancelamento MotivoCancelamento,versao,created_by CreatedBy,updated_by UpdatedBy,reg_date RegDate,reg_update RegUpdate,reg_status RegStatus from plantaopro.consultas where id=@id and cliente_id=@clienteId and reg_status='A'""";
        var cn = tx?.Connection ?? AbrirConexao(); if (tx is null) await ((NpgsqlConnection)cn).OpenAsync(ct);
        try { return await cn.QuerySingleOrDefaultAsync<Consulta>(Cmd(sql, new { id, clienteId }, tx, ct)); } finally { if (tx is null) await ((NpgsqlConnection)cn).DisposeAsync(); }
    }

    public async Task<ConsultaWorkspaceResponse?> WorkspaceAsync(Guid id, Guid clienteId, CancellationToken ct)
    {
        await using var cn = AbrirConexao(); await cn.OpenAsync(ct);
        const string sql = """select c.id,c.cliente_id ClienteId,c.unidade_id UnidadeId,c.atendimento_id AtendimentoId,c.agendamento_id AgendamentoId,c.paciente_id PacienteId,c.medico_id MedicoId,c.triagem_id TriagemId,c.status,coalesce(c.anamnese,'') Anamnese,coalesce(c.exame_fisico,'') ExameFisico,coalesce(c.hipotese_diagnostica,'') HipoteseDiagnostica,coalesce(c.diagnostico,'') Diagnostico,coalesce(c.conduta,'') Conduta,coalesce(c.orientacoes,'') Orientacoes,coalesce(c.observacoes,'') Observacoes,c.inicio_em InicioEm,c.finalizada_em FinalizadaEm,c.cancelada_em CanceladaEm,c.motivo_cancelamento MotivoCancelamento,c.versao,c.created_by CreatedBy,c.updated_by UpdatedBy,c.reg_date RegDate,c.reg_update RegUpdate,c.reg_status RegStatus,p.nome PacienteNome,p.nome_social NomeSocial,p.data_nascimento DataNascimento,p.sexo_genero SexoGenero,p.alergias,u.nome Unidade,t.classificacao_risco ClassificacaoRisco,a.checkin_em ChegadaEm from plantaopro.consultas c join plantaopro.pacientes p on p.id=c.paciente_id left join plantaopro.unidades u on u.id=c.unidade_id left join plantaopro.triagens t on t.id=c.triagem_id left join plantaopro.agendamentos a on a.id=c.agendamento_id where c.id=@id and c.cliente_id=@clienteId and c.reg_status='A'""";
        ConsultaWorkspaceResponse? result = null;
        await cn.QueryAsync<Consulta, ConsultaWorkspaceResponse, ConsultaWorkspaceResponse>(new CommandDefinition(sql, new { id, clienteId }, cancellationToken: ct), (consulta, workspace) => { workspace.Consulta = consulta; result = workspace; return workspace; }, splitOn: "PacienteNome");
        if (result is not null) result.Cids = await ListarCidsAsync(id, clienteId, ct);
        return result;
    }

    public async Task<IReadOnlyList<ConsultaResumoResponse>> FilaAsync(Guid clienteId, Guid? unidadeId, Guid? medicoId, int pagina, int tamanho, CancellationToken ct)
    {
        await using var cn = AbrirConexao(); var offset = (Math.Max(1, pagina) - 1) * Math.Clamp(tamanho, 1, 100);
        const string sql = """select c.id,c.paciente_id PacienteId,p.nome PacienteNome,c.status,coalesce(t.classificacao_risco,'SEM_CLASSIFICACAO') ClassificacaoRisco,coalesce(a.checkin_em,c.reg_date) ChegadaEm,greatest(0,extract(epoch from(now()-coalesce(a.checkin_em,c.reg_date)))/60)::int TempoEsperaMinutos from plantaopro.consultas c join plantaopro.pacientes p on p.id=c.paciente_id left join plantaopro.triagens t on t.id=c.triagem_id left join plantaopro.agendamentos a on a.id=c.agendamento_id where c.cliente_id=@clienteId and c.reg_status='A' and c.status in ('AGUARDANDO','EM_ATENDIMENTO','RASCUNHO') and (@unidadeId is null or c.unidade_id=@unidadeId) and (@medicoId is null or c.medico_id=@medicoId) order by case coalesce(t.classificacao_risco,'') when 'VERMELHO' then 1 when 'LARANJA' then 2 when 'AMARELO' then 3 when 'VERDE' then 4 else 5 end,coalesce(a.checkin_em,c.reg_date) limit @tamanho offset @offset""";
        return (await cn.QueryAsync<ConsultaResumoResponse>(new CommandDefinition(sql, new { clienteId, unidadeId, medicoId, tamanho = Math.Clamp(tamanho, 1, 100), offset }, cancellationToken: ct))).AsList();
    }

    public async Task<bool> AlterarStatusAsync(Guid id, Guid clienteId, ConsultaStatus atual, ConsultaStatus destino, int versao, Guid? usuarioId, CancellationToken ct, IDbTransaction? tx = null)
    {
        const string sql = """update plantaopro.consultas set status=@destino,inicio_em=case when @destino='EM_ATENDIMENTO' then coalesce(inicio_em,now()) else inicio_em end,finalizada_em=case when @destino='FINALIZADA' then now() else finalizada_em end,cancelada_em=case when @destino='CANCELADA' then now() else cancelada_em end,versao=versao+1,updated_by=@usuarioId,reg_update=now() where id=@id and cliente_id=@clienteId and status=@atual and versao=@versao and reg_status='A'""";
        var cn = tx?.Connection ?? AbrirConexao(); if (tx is null) await ((NpgsqlConnection)cn).OpenAsync(ct);
        try { return await cn.ExecuteAsync(Cmd(sql, new { id, clienteId, atual = atual.ToString(), destino = destino.ToString(), versao, usuarioId }, tx, ct)) == 1; } finally { if (tx is null) await ((NpgsqlConnection)cn).DisposeAsync(); }
    }

    public async Task<bool> SalvarRascunhoAsync(Guid id, Guid clienteId, SalvarConsultaRascunhoRequest r, Guid? usuarioId, CancellationToken ct)
    {
        await using var cn = AbrirConexao(); const string sql = """update plantaopro.consultas set anamnese=@Anamnese,exame_fisico=@ExameFisico,hipotese_diagnostica=@HipoteseDiagnostica,diagnostico=@Diagnostico,conduta=@Conduta,orientacoes=@Orientacoes,observacoes=@Observacoes,status='RASCUNHO',versao=versao+1,updated_by=@usuarioId,reg_update=now() where id=@id and cliente_id=@clienteId and versao=@Versao and status in ('EM_ATENDIMENTO','RASCUNHO') and reg_status='A'""";
        return await cn.ExecuteAsync(new CommandDefinition(sql, new { id, clienteId, r.Versao, r.Anamnese, r.ExameFisico, r.HipoteseDiagnostica, r.Diagnostico, r.Conduta, r.Orientacoes, r.Observacoes, usuarioId }, cancellationToken: ct)) == 1;
    }

    public async Task<IReadOnlyList<ConsultaCid>> ListarCidsAsync(Guid consultaId, Guid clienteId, CancellationToken ct)
    {
        await using var cn = AbrirConexao(); const string sql = """select cc.id,cc.cliente_id ClienteId,cc.consulta_id ConsultaId,cc.cid_id CidId,c.codigo,c.descricao,cc.tipo,cc.principal,cc.ordem from plantaopro.consulta_cids cc join plantaopro.cid_tabela c on c.id=cc.cid_id where cc.consulta_id=@consultaId and cc.cliente_id=@clienteId and cc.reg_status='A' order by cc.principal desc,cc.ordem""";
        return (await cn.QueryAsync<ConsultaCid>(new CommandDefinition(sql, new { consultaId, clienteId }, cancellationToken: ct))).AsList();
    }

    public async Task<ConsultaCid?> AdicionarCidAsync(Guid consultaId, Guid clienteId, AdicionarConsultaCidRequest r, Guid? usuarioId, CancellationToken ct)
    {
        await using var cn = AbrirConexao(); await cn.OpenAsync(ct); await using var tx = await cn.BeginTransactionAsync(ct);
        if (r.Principal) await cn.ExecuteAsync(Cmd("update plantaopro.consulta_cids set principal=false where consulta_id=@consultaId and cliente_id=@clienteId and reg_status='A'", new { consultaId, clienteId }, tx, ct));
        var id = Guid.NewGuid(); const string sql = """insert into plantaopro.consulta_cids(id,cliente_id,consulta_id,cid_id,tipo,principal,ordem,created_by,reg_date,reg_status) select @id,@clienteId,@consultaId,c.id,@tipo,@principal,coalesce((select max(ordem)+1 from plantaopro.consulta_cids where consulta_id=@consultaId),1),@usuarioId,now(),'A' from plantaopro.cid_tabela c where c.id=@cidId and c.reg_status='A' on conflict (cliente_id,consulta_id,cid_id) where reg_status='A' do nothing""";
        if (await cn.ExecuteAsync(Cmd(sql, new { id, clienteId, consultaId, cidId = r.CidId, tipo = r.Principal ? "PRINCIPAL" : r.Tipo, principal = r.Principal, usuarioId }, tx, ct)) != 1) { await tx.RollbackAsync(ct); return null; }
        await tx.CommitAsync(ct); return (await ListarCidsAsync(consultaId, clienteId, ct)).Single(x => x.Id == id);
    }

    public async Task<bool> RemoverCidAsync(Guid consultaId, Guid consultaCidId, Guid clienteId, Guid? usuarioId, CancellationToken ct)
    { await using var cn = AbrirConexao(); return await cn.ExecuteAsync(new CommandDefinition("update plantaopro.consulta_cids set reg_status='I',removed_by=@usuarioId,removed_at=now() where id=@consultaCidId and consulta_id=@consultaId and cliente_id=@clienteId and reg_status='A'", new { consultaId, consultaCidId, clienteId, usuarioId }, cancellationToken: ct)) == 1; }
}

public interface IConsultaApplicationService
{
    Task<ApiResponse<IReadOnlyList<ConsultaResumoResponse>>> FilaAsync(Guid? unidadeId, Guid? medicoId, int pagina, int tamanho, CancellationToken ct);
    Task<ApiResponse<ConsultaWorkspaceResponse>> WorkspaceAsync(Guid id, CancellationToken ct);
    Task<ApiResponse<Consulta>> IniciarAsync(Guid id, IniciarConsultaRequest request, CancellationToken ct);
    Task<ApiResponse<Consulta>> SalvarRascunhoAsync(Guid id, SalvarConsultaRascunhoRequest request, CancellationToken ct);
    Task<ApiResponse<ConsultaPendenciasFinalizacaoResponse>> PendenciasAsync(Guid id, CancellationToken ct);
    Task<ApiResponse<Consulta>> FinalizarAsync(Guid id, FinalizarConsultaRequest request, CancellationToken ct);
}

public sealed class ConsultaApplicationService : IConsultaApplicationService
{
    private const string Conflito = "Este atendimento foi alterado em outra sessão. Recarregue os dados antes de salvar novamente.";
    private readonly IConsultaRepository repository; private readonly ICurrentUserService user; private readonly IAuditService audit;
    public ConsultaApplicationService(IConsultaRepository repository, ICurrentUserService user, IAuditService audit) { this.repository = repository; this.user = user; this.audit = audit; }
    private Guid? Tenant => user.ClienteId ?? user.TenantId;
    private ApiResponse<T> SemTenant<T>() => ApiResponse<T>.Fail("Selecione uma organização para acessar o prontuário.", 403);
    private Task Auditar(Guid id, string acao, object detalhes) => audit.RegistrarAsync(user.UserId, Tenant, "consultas", id, acao, detalhes, true, null, string.Join(',', user.Roles));

    public async Task<ApiResponse<IReadOnlyList<ConsultaResumoResponse>>> FilaAsync(Guid? unidadeId, Guid? medicoId, int pagina, int tamanho, CancellationToken ct) => Tenant is not Guid tenant ? SemTenant<IReadOnlyList<ConsultaResumoResponse>>() : ApiResponse<IReadOnlyList<ConsultaResumoResponse>>.Ok(await repository.FilaAsync(tenant, unidadeId, medicoId, pagina, tamanho, ct));
    public async Task<ApiResponse<ConsultaWorkspaceResponse>> WorkspaceAsync(Guid id, CancellationToken ct) { if (Tenant is not Guid tenant) return SemTenant<ConsultaWorkspaceResponse>(); var value = await repository.WorkspaceAsync(id, tenant, ct); if (value is null) return ApiResponse<ConsultaWorkspaceResponse>.Fail("Consulta não encontrada.", 404); await Auditar(id, "PRONTUARIO_VISUALIZAR", new { consultaId = id }); return ApiResponse<ConsultaWorkspaceResponse>.Ok(value); }
    public async Task<ApiResponse<Consulta>> IniciarAsync(Guid id, IniciarConsultaRequest request, CancellationToken ct) { if (Tenant is not Guid tenant) return SemTenant<Consulta>(); var atual = await repository.ObterAsync(id, tenant, ct); if (atual is null) return ApiResponse<Consulta>.Fail("Consulta não encontrada.", 404); ConsultaStateMachine.Validar(atual.Status, ConsultaStatus.EM_ATENDIMENTO); if (!await repository.AlterarStatusAsync(id, tenant, atual.Status, ConsultaStatus.EM_ATENDIMENTO, request.Versao, user.UserId, ct)) return ApiResponse<Consulta>.Fail(Conflito, 409); await Auditar(id, "CONSULTA_INICIAR", new { request.Versao }); return ApiResponse<Consulta>.Ok((await repository.ObterAsync(id, tenant, ct))!); }
    public async Task<ApiResponse<Consulta>> SalvarRascunhoAsync(Guid id, SalvarConsultaRascunhoRequest request, CancellationToken ct) { if (Tenant is not Guid tenant) return SemTenant<Consulta>(); if (!await repository.SalvarRascunhoAsync(id, tenant, request, user.UserId, ct)) return ApiResponse<Consulta>.Fail(Conflito, 409); await Auditar(id, "CONSULTA_SALVAR", new { request.Versao }); return ApiResponse<Consulta>.Ok((await repository.ObterAsync(id, tenant, ct))!, "Rascunho salvo."); }
    public async Task<ApiResponse<ConsultaPendenciasFinalizacaoResponse>> PendenciasAsync(Guid id, CancellationToken ct) { if (Tenant is not Guid tenant) return SemTenant<ConsultaPendenciasFinalizacaoResponse>(); var c = await repository.ObterAsync(id, tenant, ct); if (c is null) return ApiResponse<ConsultaPendenciasFinalizacaoResponse>.Fail("Consulta não encontrada.", 404); var p = new List<string>(); if (c.PacienteId == Guid.Empty) p.Add("Paciente não vinculado."); if (c.MedicoId == Guid.Empty) p.Add("Médico não vinculado."); if (c.AtendimentoId == Guid.Empty) p.Add("Atendimento não vinculado."); if (string.IsNullOrWhiteSpace(c.Anamnese)) p.Add("Preencha a anamnese."); if (string.IsNullOrWhiteSpace(c.Diagnostico)) p.Add("Preencha o diagnóstico."); if ((await repository.ListarCidsAsync(id, tenant, ct)).All(x => !x.Principal)) p.Add("Informe o CID principal."); var alertas = string.IsNullOrWhiteSpace(c.ExameFisico) ? new[] { "Exame físico não preenchido." } : Array.Empty<string>(); return ApiResponse<ConsultaPendenciasFinalizacaoResponse>.Ok(new(p, alertas, 0)); }
    public async Task<ApiResponse<Consulta>> FinalizarAsync(Guid id, FinalizarConsultaRequest request, CancellationToken ct)
    {
        if (Tenant is not Guid tenant) return SemTenant<Consulta>(); var pendencias = await PendenciasAsync(id, ct); if (pendencias.Data is { PodeFinalizar: false }) return ApiResponse<Consulta>.Fail("Existem pendências impeditivas.", 422, pendencias.Data.Impeditivas);
        if ((request.TipoFaturamento is TipoFaturamentoAssistencial.CORTESIA or TipoFaturamentoAssistencial.ISENTO) && string.IsNullOrWhiteSpace(request.Justificativa)) return ApiResponse<Consulta>.Fail("Cortesia ou isenção exige justificativa.", 400);
        var liquido = AtendimentoBillingService.CalcularValorLiquido(request.ValorBruto, request.Desconto, request.Coparticipacao);
        await using var cn = repository.AbrirConexao(); await cn.OpenAsync(ct); await using var tx = await cn.BeginTransactionAsync(ct);
        var atual = await repository.ObterAsync(id, tenant, ct, tx); if (atual is null) return ApiResponse<Consulta>.Fail("Consulta não encontrada.", 404); ConsultaStateMachine.Validar(atual.Status, ConsultaStatus.FINALIZADA);
        if (!await repository.AlterarStatusAsync(id, tenant, atual.Status, ConsultaStatus.FINALIZADA, request.Versao, user.UserId, ct, tx)) { await tx.RollbackAsync(ct); return ApiResponse<Consulta>.Fail(Conflito, 409); }
        await cn.ExecuteAsync(new CommandDefinition("update plantaopro.atendimentos set status='FINALIZADO',finalizado_em=now(),updated_by=@uid where id=@atendimentoId and cliente_id=@tenant; update plantaopro.agendamentos set status='ATENDIDO',updated_by=@uid,updated_at=now() where id=@agendamentoId and cliente_id=@tenant; update plantaopro.fila_atendimento set status='FINALIZADO',reg_update=now() where atendimento_id=@atendimentoId and cliente_id=@tenant and reg_status='A'", new { atual.AtendimentoId, atual.AgendamentoId, tenant, uid = user.UserId }, tx, cancellationToken: ct));
        await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.clinica_contas_receber(id,cliente_id,unidade_id,paciente_id,atendimento_id,consulta_id,medico_id,valor_bruto,desconto,coparticipacao,valor_liquido,valor_pago,vencimento,status,origem,justificativa,created_by,reg_date,reg_status) values(gen_random_uuid(),@tenant,@UnidadeId,@PacienteId,@AtendimentoId,@id,@MedicoId,@ValorBruto,@Desconto,@Coparticipacao,@liquido,0,current_date,case when @tipo in ('CONVENIO','PLANO_SAUDE') then 'EM_ANALISE' else 'ABERTA' end,'CONSULTA',@Justificativa,@uid,now(),'A') on conflict (cliente_id,consulta_id) where reg_status='A' do nothing", new { tenant, atual.UnidadeId, atual.PacienteId, atual.AtendimentoId, id, atual.MedicoId, request.ValorBruto, request.Desconto, request.Coparticipacao, liquido, tipo = request.TipoFaturamento.ToString(), request.Justificativa, uid = user.UserId }, tx, cancellationToken: ct));
        await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.consulta_historico(id,cliente_id,consulta_id,evento,versao,created_by,reg_date,reg_status) values(gen_random_uuid(),@tenant,@id,'FINALIZADA',@versao,@uid,now(),'A')", new { tenant, id, versao = request.Versao + 1, uid = user.UserId }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct); await Auditar(id, "CONSULTA_FINALIZAR", new { request.Versao, request.TipoFaturamento, ValorLiquido = liquido }); return ApiResponse<Consulta>.Ok((await repository.ObterAsync(id, tenant, ct))!, "Consulta finalizada e cobrança processada.");
    }
}
