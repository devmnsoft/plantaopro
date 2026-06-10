using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using System.Data;
using System.Text.Json;

namespace PlantaoPro.Api;

public sealed class Saude360ClinicalService
{
    private readonly IConfiguration cfg;
    private readonly ICurrentUserService currentUser;
    private readonly IAuditService audit;
    private readonly ILogger<Saude360ClinicalService> logger;

    private static readonly Dictionary<string, string> Tables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "pacientes", "pacientes" },
        { "painel", "painel_chamada_fila" },
        { "painelHistorico", "painel_chamada_historico" },
        { "agendamentos", "agendamentos" },
        { "triagens", "triagens" },
        { "consultas", "consultas" },
        { "cid", "cid_tabela" },
        { "prescricoes", "prescricoes" },
        { "prescricaoModelos", "prescricao_modelos" },
        { "contasReceber", "clinica_contas_receber" },
        { "recebimentos", "clinica_recebimentos" },
        { "caixa", "clinica_caixa" },
        { "convenios", "convenios" },
        { "convenioPlanos", "convenio_planos" },
        { "convenioAutorizacoes", "convenio_autorizacoes" },
        { "planosSaude", "planos_saude" },
        { "planoSaudePacientes", "plano_saude_pacientes" }
    };

    public Saude360ClinicalService(IConfiguration cfg, ICurrentUserService currentUser, IAuditService audit, ILogger<Saude360ClinicalService> logger)
    {
        this.cfg = cfg;
        this.currentUser = currentUser;
        this.audit = audit;
        this.logger = logger;
    }

    private NpgsqlConnection Cn() => new NpgsqlConnection(cfg.GetConnectionString("Default"));
    private Guid? TenantId => currentUser.ClienteId ?? currentUser.TenantId;
    private bool IsGlobal => currentUser.IsGlobalAdmin();

    public async Task<ApiResponse<IEnumerable<Saude360RegistroDto>>> ListarAsync(string tableKey, string? status = null, Guid? pacienteId = null, Guid? medicoId = null, Guid? agendamentoId = null, Guid? consultaId = null, string? termo = null)
    {
        var table = ResolveTable(tableKey);
        await using var cn = Cn();
        var sql = BuildListSql(table, tableKey, pacienteId, medicoId, agendamentoId, consultaId, termo);
        var rows = await cn.QueryAsync(sql, new { tenantId = TenantId, isGlobal = IsGlobal, status, pacienteId, medicoId, agendamentoId, consultaId, termo, likeTermo = string.IsNullOrWhiteSpace(termo) ? null : "%" + termo.Trim() + "%" });
        return ApiResponse<IEnumerable<Saude360RegistroDto>>.Ok(rows.Select(ToDto).ToArray(), "Registros carregados.");
    }

    public async Task<ApiResponse<Saude360RegistroDto>> ObterAsync(string tableKey, Guid id)
    {
        var table = ResolveTable(tableKey);
        await using var cn = Cn();
        var row = await cn.QueryFirstOrDefaultAsync($"select * from plantaopro.{table} where id=@id and reg_status='A' and (@tenantId is null or cliente_id is null or cliente_id=@tenantId or @isGlobal)", new { id, tenantId = TenantId, isGlobal = IsGlobal });
        if (row is null) return ApiResponse<Saude360RegistroDto>.Fail("Registro não encontrado.", 404);
        await AuditAsync(table, id, "VISUALIZAR", new { table });
        return ApiResponse<Saude360RegistroDto>.Ok(ToDto(row), "Registro encontrado.");
    }

    public async Task<ApiResponse<Saude360RegistroDto>> CriarAsync(string tableKey, Saude360CreateRequest request)
    {
        var table = ResolveTable(tableKey);
        var validation = ValidateCreate(tableKey, request);
        if (!string.IsNullOrWhiteSpace(validation)) return ApiResponse<Saude360RegistroDto>.Fail(validation, 400);

        await using var cn = Cn();
        if (string.Equals(tableKey, "agendamentos", StringComparison.OrdinalIgnoreCase))
        {
            var conflict = await cn.ExecuteScalarAsync<int>(@"select count(1) from plantaopro.agendamentos where reg_status='A' and status not in ('CANCELADO','FINALIZADO') and medico_id=@medicoId and cliente_id=@tenantId and data_inicio < @fim and data_fim > @inicio", new { medicoId = request.MedicoId, tenantId = TenantId, inicio = request.DataInicio, fim = request.DataFim });
            if (conflict > 0) return ApiResponse<Saude360RegistroDto>.Fail("Já existe agendamento para o médico no horário informado.", 409);
        }
        if (string.Equals(tableKey, "cid", StringComparison.OrdinalIgnoreCase))
        {
            var exists = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.cid_tabela where codigo=@codigo and reg_status='A'", new { codigo = request.Codigo });
            if (exists > 0) return ApiResponse<Saude360RegistroDto>.Fail("CID já cadastrado para o código informado.", 409);
        }
        if (string.Equals(tableKey, "planoSaudePacientes", StringComparison.OrdinalIgnoreCase) && request.Principal && request.PacienteId.HasValue)
        {
            await cn.ExecuteAsync("update plantaopro.plano_saude_pacientes set principal=false, updated_at=now(), updated_by=@uid where paciente_id=@pacienteId and cliente_id=@tenantId and reg_status='A'", new { uid = currentUser.UserId, pacienteId = request.PacienteId, tenantId = TenantId });
        }

        var id = Guid.NewGuid();
        var data = BuildInsert(tableKey, request, id);
        await cn.ExecuteAsync(data.Sql, data.Args);
        await AuditAsync(table, id, "CRIAR", new { table, request = SafeRequest(request) });
        return await ObterAsync(tableKey, id);
    }

    public async Task<ApiResponse<Saude360RegistroDto>> AtualizarAsync(string tableKey, Guid id, Saude360CreateRequest request)
    {
        var table = ResolveTable(tableKey);
        var validation = ValidateUpdate(tableKey, request);
        if (!string.IsNullOrWhiteSpace(validation)) return ApiResponse<Saude360RegistroDto>.Fail(validation, 400);
        await using var cn = Cn();
        var affected = await cn.ExecuteAsync($@"update plantaopro.{table}
set status = coalesce(nullif(@status,''), status), updated_by=@uid, updated_at=now()
where id=@id and reg_status='A' and (@tenantId is null or cliente_id is null or cliente_id=@tenantId or @isGlobal)", new { id, status = request.Status, uid = currentUser.UserId, tenantId = TenantId, isGlobal = IsGlobal });
        if (affected == 0) return ApiResponse<Saude360RegistroDto>.Fail("Registro não encontrado para atualização.", 404);
        await AuditAsync(table, id, "ATUALIZAR", new { table });
        return await ObterAsync(tableKey, id);
    }

    public async Task<ApiResponse<Saude360RegistroDto>> AcaoAsync(string tableKey, Guid id, string acao, Saude360ActionRequest request)
    {
        var table = ResolveTable(tableKey);
        var status = StatusForAction(acao);
        var validation = ValidateAction(tableKey, acao, request);
        if (!string.IsNullOrWhiteSpace(validation)) return ApiResponse<Saude360RegistroDto>.Fail(validation, 400);
        await using var cn = Cn();
        var affected = await cn.ExecuteAsync($"update plantaopro.{table} set status=@status, updated_by=@uid, updated_at=now() where id=@id and reg_status='A' and (@tenantId is null or cliente_id is null or cliente_id=@tenantId or @isGlobal)", new { id, status, uid = currentUser.UserId, tenantId = TenantId, isGlobal = IsGlobal });
        if (affected == 0) return ApiResponse<Saude360RegistroDto>.Fail("Registro não encontrado para ação.", 404);
        await InsertHistoricoAsync(cn, tableKey, id, acao, request);
        await AuditAsync(table, id, acao.ToUpperInvariant(), new { table, motivo = request.Motivo, justificativa = request.Justificativa });
        return await ObterAsync(tableKey, id);
    }

    public async Task<ApiResponse<object>> ResumoFinanceiroAsync()
    {
        await using var cn = Cn();
        var resumo = await cn.QueryFirstAsync(@"select
coalesce(sum(case when status='ABERTA' then valor else 0 end),0) as aberto,
coalesce(sum(case when status='RECEBIDO' then valor else 0 end),0) as recebido,
count(1) as total
from plantaopro.clinica_contas_receber where reg_status='A' and (@tenantId is null or cliente_id is null or cliente_id=@tenantId or @isGlobal)", new { tenantId = TenantId, isGlobal = IsGlobal });
        return ApiResponse<object>.Ok(resumo, "Resumo financeiro carregado.");
    }

    private static string BuildListSql(string table, string key, Guid? pacienteId, Guid? medicoId, Guid? agendamentoId, Guid? consultaId, string? termo)
    {
        var where = new List<string>();
        where.Add("reg_status = 'A'");
        where.Add("(@tenantId is null or cliente_id is null or cliente_id = @tenantId or @isGlobal)");
        where.Add("(@status is null or status = @status)");
        if (HasColumn(key, "paciente_id")) where.Add("(@pacienteId is null or paciente_id = @pacienteId)");
        if (HasColumn(key, "medico_id")) where.Add("(@medicoId is null or medico_id = @medicoId)");
        if (HasColumn(key, "agendamento_id")) where.Add("(@agendamentoId is null or agendamento_id = @agendamentoId)");
        if (HasColumn(key, "consulta_id")) where.Add("(@consultaId is null or consulta_id = @consultaId)");
        if (HasSearchColumns(key)) where.Add("(@termo is null or coalesce(nome,'') ilike @likeTermo or coalesce(descricao,'') ilike @likeTermo or coalesce(codigo,'') ilike @likeTermo)");
        return "select * from plantaopro." + table + " where " + string.Join(" and ", where) + " order by reg_date desc limit 200";
    }

    private static bool HasColumn(string key, string column)
    {
        var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "painel", new[] { "paciente_id", "agendamento_id" } },
            { "agendamentos", new[] { "paciente_id", "medico_id" } },
            { "triagens", new[] { "paciente_id", "agendamento_id" } },
            { "consultas", new[] { "paciente_id", "medico_id", "agendamento_id" } },
            { "prescricoes", new[] { "paciente_id", "medico_id", "consulta_id" } },
            { "contasReceber", new[] { "paciente_id", "agendamento_id", "consulta_id" } },
            { "convenioAutorizacoes", new[] { "paciente_id", "agendamento_id", "consulta_id" } },
            { "planoSaudePacientes", new[] { "paciente_id" } }
        };
        string[] cols;
        return map.TryGetValue(key, out cols) && cols.Any(c => string.Equals(c, column, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasSearchColumns(string key) => key == "cid" || key == "prescricaoModelos";

    private (string Sql, object Args) BuildInsert(string key, Saude360CreateRequest r, Guid id)
    {
        var tenantId = TenantId;
        var uid = currentUser.UserId;
        if (key == "pacientes") return ("insert into plantaopro.pacientes(id,cliente_id,nome,email,telefone,status,created_by) values(@id,@tenantId,@nome,@email,@telefone,@status,@uid)", new { id, tenantId, nome = r.Nome, email = r.Descricao, telefone = r.Observacoes, status = Default(r.Status, "ATIVO"), uid });
        if (key == "painel") return ("insert into plantaopro.painel_chamada_fila(id,cliente_id,painel_id,paciente_id,agendamento_id,setor_id,sala_id,guiche_id,senha,paciente_nome,status,created_by) values(@id,@tenantId,@painelId,@pacienteId,@agendamentoId,@setorId,@salaId,@guicheId,@senha,@pacienteNome,'AGUARDANDO',@uid)", new { id, tenantId, r.PainelId, r.PacienteId, r.AgendamentoId, r.SetorId, r.SalaId, r.GuicheId, r.Senha, pacienteNome = r.PacienteNome, uid });
        if (key == "agendamentos") return ("insert into plantaopro.agendamentos(id,cliente_id,paciente_id,medico_id,unidade_id,data_inicio,data_fim,tipo,status,observacoes,valor,created_by) values(@id,@tenantId,@pacienteId,@medicoId,@unidadeId,@inicio,@fim,@tipo,'AGENDADO',@obs,@valor,@uid)", new { id, tenantId, r.PacienteId, r.MedicoId, r.UnidadeId, inicio = r.DataInicio, fim = r.DataFim, tipo = Default(r.Tipo, "CONSULTA"), obs = r.Observacoes, valor = r.Valor ?? 0, uid });
        if (key == "triagens") return ("insert into plantaopro.triagens(id,cliente_id,paciente_id,agendamento_id,enfermeiro_id,classificacao_risco,queixa_principal,status,created_by) values(@id,@tenantId,@pacienteId,@agendamentoId,@uid,@classificacao,@queixa,'ABERTA',@uid)", new { id, tenantId, r.PacienteId, r.AgendamentoId, uid, classificacao = r.Tipo, queixa = r.Descricao });
        if (key == "consultas") return ("insert into plantaopro.consultas(id,cliente_id,paciente_id,agendamento_id,triagem_id,medico_id,status,created_by) values(@id,@tenantId,@pacienteId,@agendamentoId,@triagemId,@medicoId,'ABERTA',@uid)", new { id, tenantId, r.PacienteId, r.AgendamentoId, r.TriagemId, r.MedicoId, uid });
        if (key == "cid") return ("insert into plantaopro.cid_tabela(id,cliente_id,codigo,descricao,status,created_by) values(@id,@tenantId,@codigo,@descricao,'ATIVO',@uid)", new { id, tenantId, codigo = r.Codigo, descricao = r.Descricao, uid });
        if (key == "prescricoes") return ("insert into plantaopro.prescricoes(id,cliente_id,paciente_id,consulta_id,medico_id,modelo_id,status,created_by) values(@id,@tenantId,@pacienteId,@consultaId,@medicoId,@modeloId,'RASCUNHO',@uid)", new { id, tenantId, r.PacienteId, r.ConsultaId, r.MedicoId, r.ModeloId, uid });
        if (key == "prescricaoModelos") return ("insert into plantaopro.prescricao_modelos(id,cliente_id,nome,medico_id,descricao,status,created_by) values(@id,@tenantId,@nome,@medicoId,@descricao,'ATIVO',@uid)", new { id, tenantId, nome = r.Nome, r.MedicoId, descricao = r.Descricao, uid });
        if (key == "contasReceber") return ("insert into plantaopro.clinica_contas_receber(id,cliente_id,paciente_id,agendamento_id,consulta_id,descricao,valor,vencimento,status,created_by) values(@id,@tenantId,@pacienteId,@agendamentoId,@consultaId,@descricao,@valor,@vencimento,'ABERTA',@uid)", new { id, tenantId, r.PacienteId, r.AgendamentoId, r.ConsultaId, descricao = r.Descricao, valor = r.Valor ?? 0, vencimento = r.Vencimento, uid });
        if (key == "recebimentos") return ("insert into plantaopro.clinica_recebimentos(id,cliente_id,conta_receber_id,valor,forma_pagamento,status,created_by) values(@id,@tenantId,@contaId,@valor,@forma,'RECEBIDO',@uid)", new { id, tenantId, contaId = r.AgendamentoId, valor = r.Valor ?? 0, forma = r.FormaPagamento, uid });
        if (key == "caixa") return ("insert into plantaopro.clinica_caixa(id,cliente_id,saldo_inicial,status,created_by) values(@id,@tenantId,@valor,'ABERTO',@uid)", new { id, tenantId, valor = r.Valor ?? 0, uid });
        if (key == "convenios") return ("insert into plantaopro.convenios(id,cliente_id,nome,codigo_ans,status,created_by) values(@id,@tenantId,@nome,@codigo,'ATIVO',@uid)", new { id, tenantId, nome = r.Nome, codigo = r.Codigo, uid });
        if (key == "convenioPlanos") return ("insert into plantaopro.convenio_planos(id,cliente_id,convenio_id,nome,registro_ans,status,created_by) values(@id,@tenantId,@convenioId,@nome,@codigo,'ATIVO',@uid)", new { id, tenantId, r.ConvenioId, nome = r.Nome, codigo = r.Codigo, uid });
        if (key == "convenioAutorizacoes") return ("insert into plantaopro.convenio_autorizacoes(id,cliente_id,convenio_id,paciente_id,agendamento_id,consulta_id,procedimento_id,motivo,status,created_by) values(@id,@tenantId,@convenioId,@pacienteId,@agendamentoId,@consultaId,@procedimentoId,@motivo,'SOLICITADA',@uid)", new { id, tenantId, r.ConvenioId, r.PacienteId, r.AgendamentoId, r.ConsultaId, r.ProcedimentoId, motivo = r.Motivo, uid });
        if (key == "planosSaude") return ("insert into plantaopro.planos_saude(id,cliente_id,nome,operadora,registro_ans,status,created_by) values(@id,@tenantId,@nome,@operadora,@codigo,'ATIVO',@uid)", new { id, tenantId, nome = r.Nome, operadora = r.Descricao, codigo = r.Codigo, uid });
        return ("insert into plantaopro.plano_saude_pacientes(id,cliente_id,plano_saude_id,paciente_id,numero_carteirinha,principal,validade,status,created_by) values(@id,@tenantId,@planoSaudeId,@pacienteId,@carteira,@principal,@validade,'ATIVO',@uid)", new { id, tenantId, r.PlanoSaudeId, r.PacienteId, carteira = r.NumeroCarteirinha, r.Principal, r.Validade, uid });
    }

    private async Task InsertHistoricoAsync(IDbConnection cn, string key, Guid id, string acao, Saude360ActionRequest request)
    {
        var detalhes = JsonSerializer.Serialize(new { acao, request.Motivo, request.Justificativa, request.Valor, request.FormaPagamento });
        var tenantId = TenantId;
        var uid = currentUser.UserId;
        if (key == "painel") await cn.ExecuteAsync("insert into plantaopro.painel_chamada_historico(id,cliente_id,fila_id,acao,detalhes,usuario_id) values(gen_random_uuid(),@tenantId,@id,@acao,cast(@detalhes as jsonb),@uid)", new { tenantId, id, acao, detalhes, uid });
        if (key == "agendamentos") await cn.ExecuteAsync("insert into plantaopro.agendamento_historico(id,cliente_id,agendamento_id,acao,detalhes,usuario_id) values(gen_random_uuid(),@tenantId,@id,@acao,cast(@detalhes as jsonb),@uid)", new { tenantId, id, acao, detalhes, uid });
        if (key == "triagens") await cn.ExecuteAsync("insert into plantaopro.triagem_historico(id,cliente_id,triagem_id,acao,detalhes,usuario_id) values(gen_random_uuid(),@tenantId,@id,@acao,cast(@detalhes as jsonb),@uid)", new { tenantId, id, acao, detalhes, uid });
        if (key == "consultas") await cn.ExecuteAsync("insert into plantaopro.consulta_historico(id,cliente_id,consulta_id,acao,detalhes,usuario_id) values(gen_random_uuid(),@tenantId,@id,@acao,cast(@detalhes as jsonb),@uid)", new { tenantId, id, acao, detalhes, uid });
        if (key == "prescricoes") await cn.ExecuteAsync("insert into plantaopro.prescricao_historico(id,cliente_id,prescricao_id,acao,detalhes,usuario_id) values(gen_random_uuid(),@tenantId,@id,@acao,cast(@detalhes as jsonb),@uid)", new { tenantId, id, acao, detalhes, uid });
    }

    private static string ResolveTable(string key)
    {
        string table;
        if (!Tables.TryGetValue(key, out table)) throw new InvalidOperationException("Tabela Saúde 360 inválida.");
        return table;
    }

    private static string? ValidateCreate(string key, Saude360CreateRequest r)
    {
        if (key == "pacientes" && string.IsNullOrWhiteSpace(r.Nome)) return "Paciente exige nome.";
        if (key == "agendamentos" && (!r.PacienteId.HasValue || !r.MedicoId.HasValue || !r.DataInicio.HasValue || !r.DataFim.HasValue || r.DataFim <= r.DataInicio)) return "Agendamento exige paciente, médico, início e fim válidos.";
        if (key == "triagens" && !r.PacienteId.HasValue) return "Triagem exige paciente vinculado.";
        if (key == "consultas" && (!r.PacienteId.HasValue || !r.MedicoId.HasValue)) return "Consulta exige paciente e médico.";
        if (key == "cid" && (string.IsNullOrWhiteSpace(r.Codigo) || string.IsNullOrWhiteSpace(r.Descricao))) return "CID exige código e descrição.";
        if (key == "prescricoes" && (!r.PacienteId.HasValue || !r.MedicoId.HasValue)) return "Prescrição exige paciente e médico.";
        if (key == "contasReceber" && (r.Valor.GetValueOrDefault() <= 0 || string.IsNullOrWhiteSpace(r.Descricao))) return "Conta a receber exige valor e descrição.";
        if (key == "recebimentos" && (r.Valor.GetValueOrDefault() <= 0 || string.IsNullOrWhiteSpace(r.FormaPagamento))) return "Recebimento exige valor e forma de pagamento.";
        if ((key == "convenios" || key == "planosSaude" || key == "prescricaoModelos") && string.IsNullOrWhiteSpace(r.Nome)) return "Nome é obrigatório.";
        if (key == "planoSaudePacientes" && (!r.PacienteId.HasValue || !r.PlanoSaudeId.HasValue)) return "Vínculo de plano exige paciente e plano.";
        return null;
    }

    private static string? ValidateUpdate(string key, Saude360CreateRequest r) => null;

    private static string? ValidateAction(string key, string acao, Saude360ActionRequest r)
    {
        var normalized = acao.ToLowerInvariant();
        if ((normalized == "cancelar" || normalized == "ausente" || normalized == "negar" || normalized == "estornar") && string.IsNullOrWhiteSpace(r.Motivo) && string.IsNullOrWhiteSpace(r.Justificativa)) return "A ação exige motivo ou justificativa.";
        if (normalized == "receber" && (r.Valor.GetValueOrDefault() <= 0 || string.IsNullOrWhiteSpace(r.FormaPagamento))) return "Recebimento exige valor e forma de pagamento.";
        return null;
    }

    private static string StatusForAction(string acao)
    {
        var a = acao.ToLowerInvariant();
        if (a == "chamar" || a == "rechamar") return "CHAMADO";
        if (a == "finalizar") return "FINALIZADO";
        if (a == "ausente") return "AUSENTE";
        if (a == "confirmar") return "CONFIRMADO";
        if (a == "checkin") return "CHECKIN";
        if (a == "cancelar") return "CANCELADO";
        if (a == "reagendar") return "REAGENDADO";
        if (a == "iniciar") return "EM_ATENDIMENTO";
        if (a == "inativar") return "INATIVO";
        if (a == "aprovar") return "APROVADA";
        if (a == "negar") return "NEGADA";
        if (a == "receber") return "RECEBIDO";
        if (a == "estornar") return "ESTORNADO";
        if (a == "fechar-caixa") return "FECHADO";
        return acao.ToUpperInvariant();
    }

    private async Task AuditAsync(string table, Guid id, string action, object detalhes)
    {
        try
        {
            await audit.RegistrarAsync(currentUser.UserId, TenantId, table, id, "SAUDE360_" + action, detalhes, true, null, string.Join(',', currentUser.Roles));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha de auditoria Saúde 360 para {Table}/{Id}", table, id);
        }
    }

    private static object SafeRequest(Saude360CreateRequest r) => new { r.PacienteId, r.MedicoId, r.AgendamentoId, r.ConsultaId, r.Nome, r.Codigo, r.Status, r.Tipo, r.Valor };
    private static string Default(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static Saude360RegistroDto ToDto(dynamic row)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in (IDictionary<string, object?>)row) dict[item.Key] = item.Value;
        return new Saude360RegistroDto
        {
            Id = Get<Guid>(dict, "id"),
            ClienteId = GetNullable<Guid>(dict, "cliente_id"),
            PacienteId = GetNullable<Guid>(dict, "paciente_id"),
            MedicoId = GetNullable<Guid>(dict, "medico_id"),
            AgendamentoId = GetNullable<Guid>(dict, "agendamento_id"),
            ConsultaId = GetNullable<Guid>(dict, "consulta_id"),
            Nome = GetString(dict, "nome", GetString(dict, "paciente_nome", string.Empty)),
            Descricao = GetString(dict, "descricao", GetString(dict, "queixa_principal", string.Empty)),
            Codigo = GetString(dict, "codigo", GetString(dict, "codigo_cid", string.Empty)),
            Status = GetString(dict, "status", string.Empty),
            RegDate = Get<DateTime>(dict, "reg_date"),
            Dados = dict
        };
    }

    private static T Get<T>(Dictionary<string, object?> dict, string key)
    {
        object? value;
        if (!dict.TryGetValue(key, out value) || value is null) return default(T)!;
        if (value is T t) return t;
        return (T)Convert.ChangeType(value, typeof(T));
    }

    private static T? GetNullable<T>(Dictionary<string, object?> dict, string key) where T : struct
    {
        object? value;
        if (!dict.TryGetValue(key, out value) || value is null) return null;
        if (value is T t) return t;
        return (T)Convert.ChangeType(value, typeof(T));
    }

    private static string GetString(Dictionary<string, object?> dict, string key, string fallback)
    {
        object? value;
        return dict.TryGetValue(key, out value) && value is not null ? value.ToString() ?? fallback : fallback;
    }
}
