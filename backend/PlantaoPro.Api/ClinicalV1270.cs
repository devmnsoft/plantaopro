using System.Data;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public enum ConsultaStatus { AGUARDANDO, EM_ATENDIMENTO, RASCUNHO, FINALIZADA, CANCELADA, RETORNO_SOLICITADO }

public sealed class Consulta
{
    public Guid Id { get; init; }
    public Guid ClienteId { get; init; }
    public Guid UnidadeId { get; init; }
    public Guid AtendimentoId { get; init; }
    public Guid? AgendamentoId { get; init; }
    public Guid PacienteId { get; init; }
    public Guid MedicoId { get; init; }
    public Guid? TriagemId { get; init; }
    public ConsultaStatus Status { get; init; }
    public string Anamnese { get; init; } = "";
    public string ExameFisico { get; init; } = "";
    public string HipoteseDiagnostica { get; init; } = "";
    public string Diagnostico { get; init; } = "";
    public string Conduta { get; init; } = "";
    public string Orientacoes { get; init; } = "";
    public string Observacoes { get; init; } = "";
    public DateTimeOffset? InicioEm { get; init; }
    public DateTimeOffset? FinalizadaEm { get; init; }
    public DateTimeOffset? CanceladaEm { get; init; }
    public string? MotivoCancelamento { get; init; }
    public int Versao { get; init; }
    public Guid? CreatedBy { get; init; }
    public Guid? UpdatedBy { get; init; }
    public DateTimeOffset RegDate { get; init; }
    public DateTimeOffset? RegUpdate { get; init; }
    public char RegStatus { get; init; }
}

public sealed record ConsultaCid(Guid Id, Guid ClienteId, Guid ConsultaId, Guid CidId, string Codigo, string Descricao, string Tipo, bool Principal, int Ordem);
public sealed record IniciarConsultaRequest(int Versao);
public sealed record SalvarConsultaRascunhoRequest(string Anamnese, string ExameFisico, string HipoteseDiagnostica, string Diagnostico, string Conduta, string Orientacoes, string Observacoes, int Versao);
public sealed record FinalizarConsultaRequest(int Versao);
public sealed record CancelarConsultaRequest(string Motivo, int Versao);
public sealed record ReabrirConsultaRequest(string Justificativa, int Versao);
public sealed record AdicionarConsultaCidRequest(Guid CidId, string Tipo, bool Principal);
public sealed record CriarSolicitacaoExameRequest(string Exame, string Justificativa, string Prioridade);
public sealed record CriarEncaminhamentoRequest(string Especialidade, string Motivo, string Prioridade);
public sealed record SolicitarRetornoRequest(DateOnly? DataSugerida, string Motivo);
public sealed record ConsultaResumoResponse(Guid Id, Guid PacienteId, Guid MedicoId, string Status, int Versao, DateTimeOffset? InicioEm);
public sealed record ConsultaWorkspaceResponse(Consulta Consulta, IReadOnlyList<ConsultaCid> Cids, IReadOnlyList<string> Pendencias);
public sealed record ConsultaPendenciasFinalizacaoResponse(IReadOnlyList<string> Impeditivas, IReadOnlyList<string> Alertas, bool PodeFinalizar);

public static class ConsultaStateMachine
{
    private static readonly IReadOnlyDictionary<ConsultaStatus, IReadOnlySet<ConsultaStatus>> Transicoes = new Dictionary<ConsultaStatus, IReadOnlySet<ConsultaStatus>>
    {
        [ConsultaStatus.AGUARDANDO] = new HashSet<ConsultaStatus> { ConsultaStatus.EM_ATENDIMENTO, ConsultaStatus.CANCELADA },
        [ConsultaStatus.EM_ATENDIMENTO] = new HashSet<ConsultaStatus> { ConsultaStatus.RASCUNHO, ConsultaStatus.FINALIZADA, ConsultaStatus.CANCELADA },
        [ConsultaStatus.RASCUNHO] = new HashSet<ConsultaStatus> { ConsultaStatus.RASCUNHO, ConsultaStatus.FINALIZADA, ConsultaStatus.CANCELADA },
        [ConsultaStatus.FINALIZADA] = new HashSet<ConsultaStatus> { ConsultaStatus.RETORNO_SOLICITADO }
    };

    public static bool PodeTransicionar(ConsultaStatus atual, ConsultaStatus destino) => Transicoes.TryGetValue(atual, out var destinos) && destinos.Contains(destino);
}

public interface IConsultaRepository
{
    Task<Consulta?> ObterAsync(Guid id, Guid clienteId, Guid? medicoId, CancellationToken ct, IDbTransaction? tx = null);
    Task<IReadOnlyList<Consulta>> FilaAsync(Guid clienteId, Guid? unidadeId, Guid? medicoId, int pagina, int tamanho, CancellationToken ct);
    Task<IReadOnlyList<ConsultaCid>> CidsAsync(Guid consultaId, Guid clienteId, CancellationToken ct, IDbTransaction? tx = null);
    Task<bool> AlterarStatusAsync(Guid id, Guid clienteId, int versao, ConsultaStatus status, Guid usuarioId, CancellationToken ct, IDbTransaction? tx = null);
    Task<bool> SalvarRascunhoAsync(Guid id, Guid clienteId, Guid usuarioId, SalvarConsultaRascunhoRequest request, CancellationToken ct);
    Task<ConsultaCid?> AdicionarCidAsync(Guid consultaId, Guid clienteId, Guid usuarioId, AdicionarConsultaCidRequest request, CancellationToken ct);
    Task<bool> RemoverCidAsync(Guid consultaId, Guid consultaCidId, Guid clienteId, Guid usuarioId, CancellationToken ct);
}

public sealed class ConsultaRepository : IConsultaRepository
{
    private readonly string connectionString;
    public ConsultaRepository(IConfiguration configuration) => connectionString = configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada");
    private NpgsqlConnection NovaConexao() => new(connectionString);
    private const string Colunas = "id,cliente_id,unidade_id,atendimento_id,agendamento_id,paciente_id,medico_id,triagem_id,status,coalesce(anamnese,'') anamnese,coalesce(exame_fisico,'') exame_fisico,coalesce(hipotese_diagnostica,'') hipotese_diagnostica,coalesce(diagnostico,'') diagnostico,coalesce(conduta,'') conduta,coalesce(orientacoes,'') orientacoes,coalesce(observacoes,'') observacoes,inicio_em,finalizada_em,cancelada_em,motivo_cancelamento,versao,created_by,updated_by,reg_date,reg_update,reg_status";

    public async Task<Consulta?> ObterAsync(Guid id, Guid clienteId, Guid? medicoId, CancellationToken ct, IDbTransaction? tx = null)
    {
        var cn = tx?.Connection ?? NovaConexao();
        try { return await cn.QuerySingleOrDefaultAsync<Consulta>(new CommandDefinition($"select {Colunas} from plantaopro.consultas where id=@id and cliente_id=@clienteId and reg_status='A' and (@medicoId is null or medico_id=@medicoId)", new { id, clienteId, medicoId }, tx, cancellationToken: ct)); }
        finally { if (tx is null) cn.Dispose(); }
    }

    public async Task<IReadOnlyList<Consulta>> FilaAsync(Guid clienteId, Guid? unidadeId, Guid? medicoId, int pagina, int tamanho, CancellationToken ct)
    {
        await using var cn = NovaConexao();
        var rows = await cn.QueryAsync<Consulta>(new CommandDefinition($"select {Colunas} from plantaopro.consultas where cliente_id=@clienteId and reg_status='A' and status in ('AGUARDANDO','EM_ATENDIMENTO','RASCUNHO') and (@unidadeId is null or unidade_id=@unidadeId) and (@medicoId is null or medico_id=@medicoId) order by case status when 'EM_ATENDIMENTO' then 0 else 1 end, reg_date offset @offset limit @tamanho", new { clienteId, unidadeId, medicoId, offset = (pagina - 1) * tamanho, tamanho }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<ConsultaCid>> CidsAsync(Guid consultaId, Guid clienteId, CancellationToken ct, IDbTransaction? tx = null)
    {
        var cn = tx?.Connection ?? NovaConexao();
        try { return (await cn.QueryAsync<ConsultaCid>(new CommandDefinition("select cc.id,cc.cliente_id,cc.consulta_id,cc.cid_id,c.codigo,c.descricao,cc.tipo,cc.principal,cc.ordem from plantaopro.consulta_cids cc join plantaopro.cid_tabela c on c.id=cc.cid_id and c.reg_status='A' where cc.consulta_id=@consultaId and cc.cliente_id=@clienteId and cc.reg_status='A' order by cc.principal desc,cc.ordem", new { consultaId, clienteId }, tx, cancellationToken: ct))).AsList(); }
        finally { if (tx is null) cn.Dispose(); }
    }

    public async Task<bool> AlterarStatusAsync(Guid id, Guid clienteId, int versao, ConsultaStatus status, Guid usuarioId, CancellationToken ct, IDbTransaction? tx = null)
    {
        var cn = tx?.Connection ?? NovaConexao();
        var campoData = status == ConsultaStatus.FINALIZADA ? ",finalizada_em=now()" : status == ConsultaStatus.CANCELADA ? ",cancelada_em=now()" : status == ConsultaStatus.EM_ATENDIMENTO ? ",inicio_em=coalesce(inicio_em,now())" : "";
        try { return await cn.ExecuteAsync(new CommandDefinition($"update plantaopro.consultas set status=@status{campoData},versao=versao+1,updated_by=@usuarioId,reg_update=now() where id=@id and cliente_id=@clienteId and versao=@versao and reg_status='A'", new { id, clienteId, versao, status = status.ToString(), usuarioId }, tx, cancellationToken: ct)) == 1; }
        finally { if (tx is null) cn.Dispose(); }
    }

    public async Task<bool> SalvarRascunhoAsync(Guid id, Guid clienteId, Guid usuarioId, SalvarConsultaRascunhoRequest r, CancellationToken ct)
    {
        await using var cn = NovaConexao();
        return await cn.ExecuteAsync(new CommandDefinition("update plantaopro.consultas set anamnese=@Anamnese,exame_fisico=@ExameFisico,hipotese_diagnostica=@HipoteseDiagnostica,diagnostico=@Diagnostico,conduta=@Conduta,orientacoes=@Orientacoes,observacoes=@Observacoes,status='RASCUNHO',versao=versao+1,updated_by=@usuarioId,reg_update=now() where id=@id and cliente_id=@clienteId and versao=@Versao and status in ('EM_ATENDIMENTO','RASCUNHO') and reg_status='A'", new { id, clienteId, usuarioId, r.Anamnese, r.ExameFisico, r.HipoteseDiagnostica, r.Diagnostico, r.Conduta, r.Orientacoes, r.Observacoes, r.Versao }, cancellationToken: ct)) == 1;
    }

    public async Task<ConsultaCid?> AdicionarCidAsync(Guid consultaId, Guid clienteId, Guid usuarioId, AdicionarConsultaCidRequest r, CancellationToken ct)
    {
        await using var cn = NovaConexao(); await cn.OpenAsync(ct); await using var tx = await cn.BeginTransactionAsync(ct);
        if (r.Principal) await cn.ExecuteAsync(new CommandDefinition("update plantaopro.consulta_cids set principal=false where consulta_id=@consultaId and cliente_id=@clienteId and principal and reg_status='A'", new { consultaId, clienteId }, tx, cancellationToken: ct));
        var id = await cn.QuerySingleAsync<Guid>(new CommandDefinition("insert into plantaopro.consulta_cids(id,cliente_id,consulta_id,cid_id,tipo,principal,ordem,created_by) select gen_random_uuid(),@clienteId,@consultaId,@CidId,@Tipo,@Principal,coalesce(max(ordem),0)+1,@usuarioId from plantaopro.consulta_cids where consulta_id=@consultaId returning id", new { consultaId, clienteId, usuarioId, r.CidId, Tipo = string.IsNullOrWhiteSpace(r.Tipo) ? "SECUNDARIO" : r.Tipo, r.Principal }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct);
        return (await CidsAsync(consultaId, clienteId, ct)).Single(x => x.Id == id);
    }

    public async Task<bool> RemoverCidAsync(Guid consultaId, Guid consultaCidId, Guid clienteId, Guid usuarioId, CancellationToken ct)
    {
        await using var cn = NovaConexao();
        return await cn.ExecuteAsync(new CommandDefinition("update plantaopro.consulta_cids set reg_status='I',updated_by=@usuarioId,reg_update=now() where id=@consultaCidId and consulta_id=@consultaId and cliente_id=@clienteId and reg_status='A'", new { consultaCidId, consultaId, clienteId, usuarioId }, cancellationToken: ct)) == 1;
    }
}

public interface IConsultaApplicationService
{
    Task<ApiResponse<IReadOnlyList<ConsultaResumoResponse>>> FilaAsync(Guid? unidadeId, int pagina, int tamanho, CancellationToken ct);
    Task<ApiResponse<ConsultaWorkspaceResponse>> WorkspaceAsync(Guid id, CancellationToken ct);
    Task<ApiResponse<ConsultaResumoResponse>> IniciarAsync(Guid id, IniciarConsultaRequest request, CancellationToken ct);
    Task<ApiResponse<ConsultaResumoResponse>> SalvarAsync(Guid id, SalvarConsultaRascunhoRequest request, CancellationToken ct);
    Task<ApiResponse<ConsultaPendenciasFinalizacaoResponse>> PendenciasAsync(Guid id, CancellationToken ct);
    Task<ApiResponse<ConsultaResumoResponse>> FinalizarAsync(Guid id, FinalizarConsultaRequest request, CancellationToken ct);
}

public sealed class ConsultaApplicationService : IConsultaApplicationService
{
    private const string Conflito = "Este atendimento foi alterado em outra sessão. Recarregue os dados antes de salvar novamente.";
    private readonly IConsultaRepository repository; private readonly ICurrentUserService user; private readonly IAuditService audit;
    public ConsultaApplicationService(IConsultaRepository repository, ICurrentUserService user, IAuditService audit) { this.repository = repository; this.user = user; this.audit = audit; }
    private Guid ClienteId => user.ClienteId ?? user.TenantId ?? throw new UnauthorizedAccessException("Contexto da clínica não informado.");
    private Guid UsuarioId => user.UserId ?? throw new UnauthorizedAccessException("Usuário não identificado.");
    private Guid? EscopoMedico => user.IsDoctor() ? user.UserId : null;
    private static ConsultaResumoResponse Resumo(Consulta c) => new(c.Id, c.PacienteId, c.MedicoId, c.Status.ToString(), c.Versao, c.InicioEm);

    public async Task<ApiResponse<IReadOnlyList<ConsultaResumoResponse>>> FilaAsync(Guid? unidadeId, int pagina, int tamanho, CancellationToken ct)
    { var r = await repository.FilaAsync(ClienteId, unidadeId, EscopoMedico, Math.Max(1, pagina), Math.Clamp(tamanho, 1, 100), ct); return ApiResponse<IReadOnlyList<ConsultaResumoResponse>>.Ok(r.Select(Resumo).ToList()); }
    public async Task<ApiResponse<ConsultaWorkspaceResponse>> WorkspaceAsync(Guid id, CancellationToken ct)
    { var c = await repository.ObterAsync(id, ClienteId, EscopoMedico, ct); if (c is null) return ApiResponse<ConsultaWorkspaceResponse>.Fail("Consulta não encontrada ou sem acesso.", 404); var cids = await repository.CidsAsync(id, ClienteId, ct); await Auditar(id, "PRONTUARIO_VISUALIZADO", ct); return ApiResponse<ConsultaWorkspaceResponse>.Ok(new(c, cids, CalcularPendencias(c, cids))); }
    public async Task<ApiResponse<ConsultaResumoResponse>> IniciarAsync(Guid id, IniciarConsultaRequest r, CancellationToken ct)
    { var c = await repository.ObterAsync(id, ClienteId, EscopoMedico, ct); if (c is null) return ApiResponse<ConsultaResumoResponse>.Fail("Consulta não encontrada.", 404); if (!ConsultaStateMachine.PodeTransicionar(c.Status, ConsultaStatus.EM_ATENDIMENTO)) return ApiResponse<ConsultaResumoResponse>.Fail("A consulta não pode ser iniciada no estado atual.", 422); if (!await repository.AlterarStatusAsync(id, ClienteId, r.Versao, ConsultaStatus.EM_ATENDIMENTO, UsuarioId, ct)) return ApiResponse<ConsultaResumoResponse>.Fail(Conflito, 409); await Auditar(id, "CONSULTA_INICIADA", ct); return ApiResponse<ConsultaResumoResponse>.Ok(Resumo((await repository.ObterAsync(id, ClienteId, EscopoMedico, ct))!)); }
    public async Task<ApiResponse<ConsultaResumoResponse>> SalvarAsync(Guid id, SalvarConsultaRascunhoRequest r, CancellationToken ct)
    { if (!await repository.SalvarRascunhoAsync(id, ClienteId, UsuarioId, r, ct)) return ApiResponse<ConsultaResumoResponse>.Fail(Conflito, 409); await Auditar(id, "CONSULTA_RASCUNHO_SALVO", ct); return ApiResponse<ConsultaResumoResponse>.Ok(Resumo((await repository.ObterAsync(id, ClienteId, EscopoMedico, ct))!)); }
    public async Task<ApiResponse<ConsultaPendenciasFinalizacaoResponse>> PendenciasAsync(Guid id, CancellationToken ct)
    { var c = await repository.ObterAsync(id, ClienteId, EscopoMedico, ct); if (c is null) return ApiResponse<ConsultaPendenciasFinalizacaoResponse>.Fail("Consulta não encontrada.", 404); var p = CalcularPendencias(c, await repository.CidsAsync(id, ClienteId, ct)); return ApiResponse<ConsultaPendenciasFinalizacaoResponse>.Ok(new(p, Array.Empty<string>(), p.Count == 0)); }
    public async Task<ApiResponse<ConsultaResumoResponse>> FinalizarAsync(Guid id, FinalizarConsultaRequest r, CancellationToken ct)
    { var c = await repository.ObterAsync(id, ClienteId, EscopoMedico, ct); if (c is null) return ApiResponse<ConsultaResumoResponse>.Fail("Consulta não encontrada.", 404); var p = CalcularPendencias(c, await repository.CidsAsync(id, ClienteId, ct)); if (p.Count > 0) return ApiResponse<ConsultaResumoResponse>.Fail(string.Join(" ", p), 422, p); if (!ConsultaStateMachine.PodeTransicionar(c.Status, ConsultaStatus.FINALIZADA)) return ApiResponse<ConsultaResumoResponse>.Fail("A consulta não pode ser finalizada no estado atual.", 422); if (!await repository.AlterarStatusAsync(id, ClienteId, r.Versao, ConsultaStatus.FINALIZADA, UsuarioId, ct)) return ApiResponse<ConsultaResumoResponse>.Fail(Conflito, 409); await Auditar(id, "CONSULTA_FINALIZADA", ct); return ApiResponse<ConsultaResumoResponse>.Ok(Resumo((await repository.ObterAsync(id, ClienteId, EscopoMedico, ct))!)); }
    private static IReadOnlyList<string> CalcularPendencias(Consulta c, IReadOnlyList<ConsultaCid> cids) { var p = new List<string>(); if (c.PacienteId == Guid.Empty) p.Add("Paciente ausente."); if (c.MedicoId == Guid.Empty) p.Add("Médico ausente."); if (c.AtendimentoId == Guid.Empty) p.Add("Atendimento ausente."); if (string.IsNullOrWhiteSpace(c.Anamnese)) p.Add("Anamnese obrigatória."); if (string.IsNullOrWhiteSpace(c.Diagnostico)) p.Add("Diagnóstico obrigatório."); if (!cids.Any(x => x.Principal)) p.Add("CID principal obrigatório."); return p; }
    private Task Auditar(Guid id, string acao, CancellationToken ct) => audit.RegistrarAsync(UsuarioId, ClienteId, "CONSULTA", id, acao, new { consultaId = id }, true, null, string.Join(',', user.Roles), ct);
}

