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
        { "pacienteHistorico", "paciente_historico" },
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
        try
        {
            await GarantirBaseClinicaAsync();
            await using var cn = Cn();
            var sql = BuildListSql(table, tableKey, pacienteId, medicoId, agendamentoId, consultaId, termo);
            var rows = await cn.QueryAsync(sql, new { tenantId = TenantId, isGlobal = IsGlobal, status, pacienteId, medicoId, agendamentoId, consultaId, termo, likeTermo = string.IsNullOrWhiteSpace(termo) ? null : "%" + termo.Trim() + "%" });
            if (string.Equals(tableKey, "consultas", StringComparison.OrdinalIgnoreCase))
            {
                await AuditAsync(table, Guid.Empty, "LISTAR", new { tableKey });
            }
            return ApiResponse<IEnumerable<Saude360RegistroDto>>.Ok(rows.Select(ToDto).ToArray(), "Registros carregados.");
        }
        catch (PostgresException ex) when (IsUndefinedTable(ex))
        {
            logger.LogError(ex, "Tabela clínica não encontrada para {TableKey}", tableKey);
            if (string.Equals(tableKey, "consultas", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<IEnumerable<Saude360RegistroDto>>.Fail("A tabela clínica de consultas ainda não foi inicializada. Execute as migrations do Saúde 360.", 500);
            }
            return ApiResponse<IEnumerable<Saude360RegistroDto>>.Fail("A base clínica ainda não foi inicializada. Execute as migrations do Saúde 360.", 500);
        }
    }

    public async Task<ApiResponse<Saude360RegistroDto>> ObterAsync(string tableKey, Guid id)
    {
        var table = ResolveTable(tableKey);
        await GarantirBaseClinicaAsync();
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

        await GarantirBaseClinicaAsync();
        await using var cn = Cn();
        if (string.Equals(tableKey, "agendamentos", StringComparison.OrdinalIgnoreCase))
        {
            var conflict = await cn.ExecuteScalarAsync<int>(@"select count(1) from plantaopro.agendamentos where reg_status='A' and status not in ('CANCELADO','FINALIZADO') and medico_id=@medicoId and cliente_id=@tenantId and data_inicio < @fim and data_fim > @inicio", new { medicoId = request.MedicoId, tenantId = TenantId, inicio = request.DataInicio, fim = request.DataFim });
            if (conflict > 0) return ApiResponse<Saude360RegistroDto>.Fail("Já existe agendamento para o médico no horário informado.", 409);
        }
        if (string.Equals(tableKey, "pacientes", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(request.Cpf))
        {
            var cpfExists = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.pacientes where reg_status='A' and cliente_id=@tenantId and regexp_replace(coalesce(cpf,''), '[^0-9]', '', 'g')=regexp_replace(@cpf, '[^0-9]', '', 'g')", new { tenantId = TenantId, cpf = request.Cpf });
            if (cpfExists > 0) return ApiResponse<Saude360RegistroDto>.Fail("CPF já cadastrado para este tenant.", 409);
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
        await GarantirBaseClinicaAsync();
        await using var cn = Cn();
        if (string.Equals(tableKey, "pacientes", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(request.Cpf))
        {
            var cpfExists = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.pacientes where id<>@id and reg_status='A' and cliente_id=@tenantId and regexp_replace(coalesce(cpf,''), '[^0-9]', '', 'g')=regexp_replace(@cpf, '[^0-9]', '', 'g')", new { id, tenantId = TenantId, cpf = request.Cpf });
            if (cpfExists > 0) return ApiResponse<Saude360RegistroDto>.Fail("CPF já cadastrado para este tenant.", 409);
        }
        var update = BuildUpdate(tableKey, table);
        var affected = await cn.ExecuteAsync(update, new { id, tenantId = TenantId, isGlobal = IsGlobal, uid = currentUser.UserId, request.Nome, request.Descricao, request.Codigo, request.Status, request.Tipo, request.Observacoes, request.Cpf, request.Cns, request.DocumentoAlternativo, request.NomeSocial, request.SexoGenero, request.Telefone, request.Email, request.Endereco, request.ResponsavelNome, request.DataNascimento, request.PacienteId, request.MedicoId, request.UnidadeId, request.SalaId, request.DataInicio, request.DataFim, request.Especialidade, request.Valor, request.ClassificacaoRisco, request.QueixaPrincipal, request.Alergias, request.MedicamentosUso });
        if (affected == 0) return ApiResponse<Saude360RegistroDto>.Fail("Registro não encontrado para atualização.", 404);
        await AuditAsync(table, id, "ATUALIZAR", new { table });
        return await ObterAsync(tableKey, id);
    }

    public async Task<ApiResponse<Saude360RegistroDto>> AcaoAsync(string tableKey, Guid id, string acao, Saude360ActionRequest request)
    {
        var table = ResolveTable(tableKey);
        var status = StatusForAction(acao);
        if (string.Equals(tableKey, "painel", StringComparison.OrdinalIgnoreCase) && string.Equals(acao, "finalizar", StringComparison.OrdinalIgnoreCase)) status = "FINALIZADO";
        if (string.Equals(tableKey, "triagens", StringComparison.OrdinalIgnoreCase) && string.Equals(acao, "finalizar", StringComparison.OrdinalIgnoreCase)) status = "FINALIZADA";
        if (string.Equals(tableKey, "consultas", StringComparison.OrdinalIgnoreCase) && string.Equals(acao, "iniciar", StringComparison.OrdinalIgnoreCase)) status = "EM_ATENDIMENTO";
        if (string.Equals(tableKey, "consultas", StringComparison.OrdinalIgnoreCase) && string.Equals(acao, "finalizar", StringComparison.OrdinalIgnoreCase)) status = "FINALIZADA";
        if (string.Equals(tableKey, "consultas", StringComparison.OrdinalIgnoreCase) && string.Equals(acao, "cancelar", StringComparison.OrdinalIgnoreCase)) status = "CANCELADA";
        var validation = ValidateAction(tableKey, acao, request);
        if (!string.IsNullOrWhiteSpace(validation)) return ApiResponse<Saude360RegistroDto>.Fail(validation, 400);
        await GarantirBaseClinicaAsync();
        await using var cn = Cn();
        if (string.Equals(tableKey, "agendamentos", StringComparison.OrdinalIgnoreCase) && string.Equals(acao, "checkin", StringComparison.OrdinalIgnoreCase))
        {
            var currentStatus = await cn.ExecuteScalarAsync<string>("select status from plantaopro.agendamentos where id=@id and reg_status='A' and (@tenantId is null or cliente_id=@tenantId or @isGlobal)", new { id, tenantId = TenantId, isGlobal = IsGlobal });
            if (currentStatus != "AGENDADO" && currentStatus != "CONFIRMADO") return ApiResponse<Saude360RegistroDto>.Fail("Check-in permitido apenas para AGENDADO ou CONFIRMADO.", 409);
        }
        if (string.Equals(tableKey, "triagens", StringComparison.OrdinalIgnoreCase) && string.Equals(acao, "iniciar", StringComparison.OrdinalIgnoreCase))
        {
            var currentStatus = await cn.ExecuteScalarAsync<string>("select status from plantaopro.triagens where id=@id and reg_status='A' and (@tenantId is null or cliente_id=@tenantId or @isGlobal)", new { id, tenantId = TenantId, isGlobal = IsGlobal });
            if (currentStatus == "FINALIZADA") return ApiResponse<Saude360RegistroDto>.Fail("Triagem finalizada não pode ser reiniciada sem permissão especial.", 409);
        }
        var actionUpdateSql = string.Equals(tableKey, "consultas", StringComparison.OrdinalIgnoreCase)
            ? $"update plantaopro.{table} set status=@status, updated_by=@uid, reg_update=now() where id=@id and reg_status='A' and (@tenantId is null or cliente_id is null or cliente_id=@tenantId or @isGlobal)"
            : $"update plantaopro.{table} set status=@status, updated_by=@uid, updated_at=now() where id=@id and reg_status='A' and (@tenantId is null or cliente_id is null or cliente_id=@tenantId or @isGlobal)";
        var affected = await cn.ExecuteAsync(actionUpdateSql, new { id, status, uid = currentUser.UserId, tenantId = TenantId, isGlobal = IsGlobal });
        if (affected == 0) return ApiResponse<Saude360RegistroDto>.Fail("Registro não encontrado para ação.", 404);
        await InsertHistoricoAsync(cn, tableKey, id, acao, request);
        await AplicarEfeitosClinicosAsync(cn, tableKey, id, acao, request);
        object auditDetalhes = string.Equals(tableKey, "consultas", StringComparison.OrdinalIgnoreCase)
            ? (object)new { table, acao }
            : new { table, motivo = request.Motivo, justificativa = request.Justificativa };
        await AuditAsync(table, id, acao.ToUpperInvariant(), auditDetalhes);
        return await ObterAsync(tableKey, id);
    }

    public async Task<ApiResponse<object>> DashboardResumoAsync()
    {
        try
        {
            await GarantirBaseClinicaAsync();
            await using var cn = Cn();
            var resumo = await cn.QueryFirstAsync(@"select
(select count(1) from plantaopro.agendamentos where reg_status='A' and (@tenantId is null or cliente_id=@tenantId or @isGlobal) and data_inicio::date=current_date) as agendamentos_hoje,
(select count(1) from plantaopro.agendamentos where reg_status='A' and (@tenantId is null or cliente_id=@tenantId or @isGlobal) and status='CHECKIN_REALIZADO' and data_inicio::date=current_date) as checkins_realizados,
(select count(1) from plantaopro.painel_chamada_fila where reg_status='A' and (@tenantId is null or cliente_id=@tenantId or @isGlobal) and status='AGUARDANDO') as pacientes_aguardando,
(select count(1) from plantaopro.painel_chamada_fila where reg_status='A' and (@tenantId is null or cliente_id=@tenantId or @isGlobal) and status in ('CHAMADO','RECHAMADO')) as pacientes_chamados,
(select count(1) from plantaopro.triagens where reg_status='A' and (@tenantId is null or cliente_id=@tenantId or @isGlobal) and status in ('AGUARDANDO','EM_TRIAGEM')) as triagens_pendentes,
(select count(1) from plantaopro.triagens where reg_status='A' and (@tenantId is null or cliente_id=@tenantId or @isGlobal) and status in ('FINALIZADA','ENCAMINHADA_CONSULTA')) as triagens_finalizadas,
(select count(1) from plantaopro.agendamentos where reg_status='A' and (@tenantId is null or cliente_id=@tenantId or @isGlobal) and status='FALTOU' and data_inicio::date=current_date) as faltas_dia,
(select count(1) from plantaopro.agendamentos where reg_status='A' and (@tenantId is null or cliente_id=@tenantId or @isGlobal) and status='CANCELADO' and data_inicio::date=current_date) as cancelamentos_dia", new { tenantId = TenantId, isGlobal = IsGlobal });
            await AuditAsync("clinica_dashboard", Guid.Empty, "RESUMO", new { modulo = "DASHBOARD_CLINICO" });
            return ApiResponse<object>.Ok(resumo, "Dashboard clínico carregado.");
        }
        catch (PostgresException ex) when (IsUndefinedTable(ex))
        {
            logger.LogError(ex, "Tabela clínica não encontrada para dashboard clínico");
            return ApiResponse<object>.Fail("A base clínica ainda não foi inicializada. Execute as migrations do Saúde 360.", 500);
        }
    }

    public async Task<ApiResponse<object>> ResumoFinanceiroAsync()
    {
        await GarantirBaseClinicaAsync();
        await using var cn = Cn();
        var resumo = await cn.QueryFirstAsync(@"select
coalesce(sum(case when status='ABERTA' then valor else 0 end),0) as aberto,
coalesce(sum(case when status='RECEBIDO' then valor else 0 end),0) as recebido,
count(1) as total
from plantaopro.clinica_contas_receber where reg_status='A' and (@tenantId is null or cliente_id is null or cliente_id=@tenantId or @isGlobal)", new { tenantId = TenantId, isGlobal = IsGlobal });
        return ApiResponse<object>.Ok(resumo, "Resumo financeiro carregado.");
    }


    private async Task GarantirBaseClinicaAsync()
    {
        await using var cn = Cn();
        await Saude360ClinicalSchema.GarantirBaseClinicaAsync(cn, logger);
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
        if (string.Equals(key, "pacientes", StringComparison.OrdinalIgnoreCase)) where.Add("(@termo is null or coalesce(nome,'') ilike @likeTermo or coalesce(cpf,'') ilike @likeTermo or coalesce(telefone,'') ilike @likeTermo or coalesce(email,'') ilike @likeTermo)");
        else if (HasSearchColumns(key)) where.Add("(@termo is null or coalesce(nome,'') ilike @likeTermo or coalesce(descricao,'') ilike @likeTermo or coalesce(codigo,'') ilike @likeTermo)");
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
            { "planoSaudePacientes", new[] { "paciente_id" } },
            { "pacienteHistorico", new[] { "paciente_id" } }
        };
        string[] cols;
        return map.TryGetValue(key, out cols) && cols.Any(c => string.Equals(c, column, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasSearchColumns(string key) => key == "pacientes" || key == "cid" || key == "prescricaoModelos";

    private (string Sql, object Args) BuildInsert(string key, Saude360CreateRequest r, Guid id)
    {
        var tenantId = TenantId;
        var uid = currentUser.UserId;
        if (key == "pacientes") return ("insert into plantaopro.pacientes(id,cliente_id,nome,nome_social,data_nascimento,sexo_genero,cpf,cns,documento_alternativo,email,telefone,endereco,responsavel_nome,observacoes,consentimento_lgpd,consentimento_lgpd_em,consentimento_lgpd_canal,status,created_by) values(@id,@tenantId,@nome,@nomeSocial,@nascimento,@sexo,@cpf,@cns,@doc,@email,@telefone,@endereco,@responsavel,@observacoes,@consentimento,case when @consentimento then now() else null end,@canalConsentimento,@status,@uid)", new { id, tenantId, nome = r.Nome, nomeSocial = r.NomeSocial, nascimento = r.DataNascimento, sexo = r.SexoGenero, cpf = r.Cpf, cns = r.Cns, doc = r.DocumentoAlternativo, email = r.Email, telefone = r.Telefone, endereco = r.Endereco, responsavel = r.ResponsavelNome, observacoes = r.Observacoes, consentimento = r.ConsentimentoLgpd, canalConsentimento = r.ConsentimentoLgpdCanal, status = Default(r.Status, "ATIVO"), uid });
        if (key == "painel") return ("insert into plantaopro.painel_chamada_fila(id,cliente_id,painel_id,paciente_id,agendamento_id,setor_id,sala_id,guiche_id,senha,paciente_nome,status,created_by) values(@id,@tenantId,@painelId,@pacienteId,@agendamentoId,@setorId,@salaId,@guicheId,@senha,@pacienteNome,'AGUARDANDO',@uid)", new { id, tenantId, r.PainelId, r.PacienteId, r.AgendamentoId, r.SetorId, r.SalaId, r.GuicheId, r.Senha, pacienteNome = r.PacienteNome, uid });
        if (key == "agendamentos") return ("insert into plantaopro.agendamentos(id,cliente_id,paciente_id,medico_id,unidade_id,sala_id,data_inicio,data_fim,tipo,especialidade,status,observacoes,valor,created_by) values(@id,@tenantId,@pacienteId,@medicoId,@unidadeId,@salaId,@inicio,@fim,@tipo,@especialidade,'AGENDADO',@obs,@valor,@uid)", new { id, tenantId, r.PacienteId, r.MedicoId, r.UnidadeId, r.SalaId, inicio = r.DataInicio, fim = r.DataFim, tipo = Default(r.Tipo, "CONSULTA"), especialidade = r.Especialidade, obs = r.Observacoes, valor = r.Valor ?? 0, uid });
        if (key == "triagens") return ("insert into plantaopro.triagens(id,cliente_id,paciente_id,agendamento_id,enfermeiro_id,classificacao_risco,queixa_principal,pressao_sistolica,pressao_diastolica,frequencia_cardiaca,frequencia_respiratoria,temperatura,saturacao,peso,altura,imc,glicemia,alergias_relatadas,medicamentos_uso,observacoes,status,created_by) values(@id,@tenantId,@pacienteId,@agendamentoId,@uid,@classificacao,@queixa,@pas,@pad,@fc,@fr,@temp,@sat,@peso,@altura,case when @peso > 0 and @altura > 0 then round((@peso / (@altura * @altura))::numeric,2) else null end,@glicemia,@alergias,@medicamentos,@obs,'AGUARDANDO',@uid)", new { id, tenantId, r.PacienteId, r.AgendamentoId, uid, classificacao = Default(r.ClassificacaoRisco, r.Tipo), queixa = Default(r.QueixaPrincipal, r.Descricao), pas = r.PressaoSistolica, pad = r.PressaoDiastolica, fc = r.FrequenciaCardiaca, fr = r.FrequenciaRespiratoria, temp = r.Temperatura, sat = r.Saturacao, peso = r.Peso, altura = r.Altura, glicemia = r.Glicemia, alergias = r.Alergias, medicamentos = r.MedicamentosUso, obs = r.Observacoes });
        if (key == "consultas") return ("insert into plantaopro.consultas(id,cliente_id,paciente_id,agendamento_id,triagem_id,medico_id,status,created_by) values(@id,@tenantId,@pacienteId,@agendamentoId,@triagemId,@medicoId,'AGUARDANDO',@uid)", new { id, tenantId, r.PacienteId, r.AgendamentoId, r.TriagemId, r.MedicoId, uid });
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

    private static string BuildUpdate(string key, string table)
    {
        if (key == "pacientes") return $@"update plantaopro.{table} set
nome=coalesce(nullif(@Nome,''),nome), nome_social=coalesce(nullif(@NomeSocial,''),nome_social), data_nascimento=coalesce(@DataNascimento,data_nascimento), sexo_genero=coalesce(nullif(@SexoGenero,''),sexo_genero), cpf=coalesce(nullif(@Cpf,''),cpf), cns=coalesce(nullif(@Cns,''),cns), documento_alternativo=coalesce(nullif(@DocumentoAlternativo,''),documento_alternativo), email=coalesce(nullif(@Email,''),email), telefone=coalesce(nullif(@Telefone,''),telefone), endereco=coalesce(nullif(@Endereco,''),endereco), responsavel_nome=coalesce(nullif(@ResponsavelNome,''),responsavel_nome), observacoes=coalesce(nullif(@Observacoes,''),observacoes), status=coalesce(nullif(@Status,''),status), updated_by=@uid, updated_at=now()
where id=@id and reg_status='A' and (@tenantId is null or cliente_id is null or cliente_id=@tenantId or @isGlobal)";
        if (key == "agendamentos") return $@"update plantaopro.{table} set
paciente_id=coalesce(@PacienteId,paciente_id), medico_id=coalesce(@MedicoId,medico_id), unidade_id=coalesce(@UnidadeId,unidade_id), sala_id=coalesce(@SalaId,sala_id), data_inicio=coalesce(@DataInicio,data_inicio), data_fim=coalesce(@DataFim,data_fim), tipo=coalesce(nullif(@Tipo,''),tipo), especialidade=coalesce(nullif(@Especialidade,''),especialidade), observacoes=coalesce(nullif(@Observacoes,''),observacoes), status=coalesce(nullif(@Status,''),status), valor=coalesce(@Valor,valor), updated_by=@uid, updated_at=now()
where id=@id and reg_status='A' and (@tenantId is null or cliente_id is null or cliente_id=@tenantId or @isGlobal)";
        if (key == "triagens") return $@"update plantaopro.{table} set
classificacao_risco=coalesce(nullif(@ClassificacaoRisco,''),nullif(@Tipo,''),classificacao_risco), queixa_principal=coalesce(nullif(@QueixaPrincipal,''),nullif(@Descricao,''),queixa_principal), alergias_relatadas=coalesce(nullif(@Alergias,''),alergias_relatadas), medicamentos_uso=coalesce(nullif(@MedicamentosUso,''),medicamentos_uso), observacoes=coalesce(nullif(@Observacoes,''),observacoes), status=coalesce(nullif(@Status,''),status), updated_by=@uid, updated_at=now()
where id=@id and reg_status='A' and status <> 'FINALIZADA' and (@tenantId is null or cliente_id is null or cliente_id=@tenantId or @isGlobal)";
        if (key == "consultas") return $@"update plantaopro.{table} set status=coalesce(nullif(@Status,''),status), tipo=coalesce(nullif(@Tipo,''),tipo), observacoes=coalesce(nullif(@Observacoes,''),observacoes), data_inicio=coalesce(@DataInicio,data_inicio), data_fim=coalesce(@DataFim,data_fim), updated_by=@uid, reg_update=now() where id=@id and reg_status='A' and (@tenantId is null or cliente_id is null or cliente_id=@tenantId or @isGlobal)";
        return $@"update plantaopro.{table} set status=coalesce(nullif(@Status,''),status), updated_by=@uid, updated_at=now() where id=@id and reg_status='A' and (@tenantId is null or cliente_id is null or cliente_id=@tenantId or @isGlobal)";
    }

    private async Task AplicarEfeitosClinicosAsync(IDbConnection cn, string key, Guid id, string acao, Saude360ActionRequest request)
    {
        var tenantId = TenantId;
        var uid = currentUser.UserId;
        if (key == "agendamentos" && acao.Equals("checkin", StringComparison.OrdinalIgnoreCase))
        {
            await cn.ExecuteAsync(@"insert into plantaopro.agendamento_checkins(id,cliente_id,agendamento_id,paciente_id,usuario_id,observacoes)
select gen_random_uuid(), cliente_id, id, paciente_id, @uid, @obs from plantaopro.agendamentos where id=@id and not exists (select 1 from plantaopro.agendamento_checkins c where c.agendamento_id=@id and c.reg_status='A')", new { id, uid, obs = request.Observacoes });
            await cn.ExecuteAsync(@"insert into plantaopro.painel_chamada_fila(id,cliente_id,paciente_id,agendamento_id,senha,paciente_nome,status,created_by)
select gen_random_uuid(), a.cliente_id, a.paciente_id, a.id, 'P' || lpad((nextval('plantaopro.seq_painel_senhas') % 10000)::text,4,'0'), coalesce(p.nome,'Paciente'), 'AGUARDANDO', @uid
from plantaopro.agendamentos a left join plantaopro.pacientes p on p.id=a.paciente_id where a.id=@id and not exists (select 1 from plantaopro.painel_chamada_fila f where f.agendamento_id=@id and f.reg_status='A')", new { id, uid });
            await cn.ExecuteAsync(@"insert into plantaopro.triagem_fila(id,cliente_id,paciente_id,agendamento_id,status,created_by)
select gen_random_uuid(), cliente_id, paciente_id, id, 'AGUARDANDO', @uid from plantaopro.agendamentos where id=@id and not exists (select 1 from plantaopro.triagem_fila f where f.agendamento_id=@id and f.reg_status='A')", new { id, uid });
        }
        if (key == "triagens" && acao.Equals("finalizar", StringComparison.OrdinalIgnoreCase))
        {
            await cn.ExecuteAsync("update plantaopro.agendamentos set status='AGUARDANDO_CONSULTA', updated_by=@uid, updated_at=now() where id=(select agendamento_id from plantaopro.triagens where id=@id) and reg_status='A'", new { id, uid });
            await cn.ExecuteAsync(@"insert into plantaopro.triagem_encaminhamentos(id,cliente_id,triagem_id,paciente_id,agendamento_id,destino,status,created_by)
select gen_random_uuid(), cliente_id, id, paciente_id, agendamento_id, 'CONSULTA', 'ENCAMINHADA', @uid from plantaopro.triagens where id=@id", new { id, uid });
        }
    }

    private async Task InsertHistoricoAsync(IDbConnection cn, string key, Guid id, string acao, Saude360ActionRequest request)
    {
        var detalhes = JsonSerializer.Serialize(new { acao, request.Motivo, request.Justificativa, request.Valor, request.FormaPagamento });
        var tenantId = TenantId;
        var uid = currentUser.UserId;
        if (key == "pacientes") await cn.ExecuteAsync("insert into plantaopro.paciente_historico(id,cliente_id,paciente_id,acao,detalhes,usuario_id) values(gen_random_uuid(),@tenantId,@id,@acao,cast(@detalhes as jsonb),@uid)", new { tenantId, id, acao, detalhes, uid });
        if (key == "painel") await cn.ExecuteAsync("insert into plantaopro.painel_chamada_historico(id,cliente_id,fila_id,acao,detalhes,usuario_id) values(gen_random_uuid(),@tenantId,@id,@acao,cast(@detalhes as jsonb),@uid)", new { tenantId, id, acao, detalhes, uid });
        if (key == "agendamentos") await cn.ExecuteAsync("insert into plantaopro.agendamento_historico(id,cliente_id,agendamento_id,acao,detalhes,usuario_id) values(gen_random_uuid(),@tenantId,@id,@acao,cast(@detalhes as jsonb),@uid)", new { tenantId, id, acao, detalhes, uid });
        if (key == "triagens") await cn.ExecuteAsync("insert into plantaopro.triagem_historico(id,cliente_id,triagem_id,acao,detalhes,usuario_id) values(gen_random_uuid(),@tenantId,@id,@acao,cast(@detalhes as jsonb),@uid)", new { tenantId, id, acao, detalhes, uid });
        if (key == "consultas") await cn.ExecuteAsync(@"insert into plantaopro.consulta_historico(id,cliente_id,consulta_id,paciente_id,acao,detalhe,usuario_id)
select gen_random_uuid(), cliente_id, id, paciente_id, @acao, @detalhe, @uid
from plantaopro.consultas
where id=@id", new { id, acao, detalhe = detalhes, uid });
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

    private static bool IsUndefinedTable(PostgresException ex)
    {
        return ex.SqlState == "42P01";
    }

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
        if (a == "checkin") return "CHECKIN_REALIZADO";
        if (a == "cancelar") return "CANCELADO";
        if (a == "reagendar") return "REAGENDADO";
        if (a == "iniciar") return "EM_TRIAGEM";
        if (a == "inativar") return "INATIVO";
        if (a == "reativar") return "ATIVO";
        if (a == "marcar-falta") return "FALTOU";
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

    private static object SafeRequest(Saude360CreateRequest r) => new { r.PacienteId, r.MedicoId, r.AgendamentoId, r.ConsultaId, r.Codigo, r.Status, r.Tipo, r.Valor, r.ConsentimentoLgpd };
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
