using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;
using System.Text.Json;

namespace PlantaoPro.Api.Data;

public sealed class Saude360ClinicalService
{
    private readonly IConfiguration cfg;
    private readonly IAuditService audit;
    private readonly ICurrentUserService currentUser;

    public Saude360ClinicalService(IConfiguration cfg, IAuditService audit, ICurrentUserService currentUser)
    {
        this.cfg = cfg;
        this.audit = audit;
        this.currentUser = currentUser;
    }

    private NpgsqlConnection Cn() => new(cfg.GetConnectionString("Default"));
    private Guid? ClienteId => currentUser.ClienteId ?? currentUser.TenantId;
    private Guid? UserId => currentUser.UserId;
    private static string N(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    private static string Json(object value) => JsonSerializer.Serialize(value);

    private ApiResponse<T> TenantObrigatorio<T>() => ApiResponse<T>.Fail("Tenant/cliente obrigatório para executar operação clínica.", 403);

    public async Task<ApiResponse<IEnumerable<Saude360ItemDto>>> ListarAsync(string tabela, string status = "", Guid? filtroId = null, string filtroColuna = "")
    {
        if (!TabelasPermitidas.Contains(tabela)) return ApiResponse<IEnumerable<Saude360ItemDto>>.Fail("Tabela clínica não permitida.", 400);
        await using var cn = Cn();
        var filtro = string.Empty;
        if (filtroId.HasValue && ColunasFiltroPermitidas.Contains(filtroColuna)) filtro = " and " + filtroColuna + "=@filtroId";
        var rows = await cn.QueryAsync<Saude360ItemDto>(@"select id as ""Id"", tenant_id as ""TenantId"", cliente_id as ""ClienteId"", paciente_id as ""PacienteId"", medico_id as ""MedicoId"", agendamento_id as ""AgendamentoId"", consulta_id as ""ConsultaId"", coalesce(codigo,'') as ""Codigo"", coalesce(nome,'') as ""Nome"", coalesce(descricao,'') as ""Descricao"", coalesce(status,'') as ""Status"", reg_date as ""RegDate"" from plantaopro." + tabela + @" where reg_status='A' and (@clienteId is null or cliente_id=@clienteId) and (@status='' or status=@status)" + filtro + " order by reg_date desc limit 300", new { clienteId = ClienteId, status = status ?? string.Empty, filtroId });
        return ApiResponse<IEnumerable<Saude360ItemDto>>.Ok(rows, "Registros listados.");
    }

    public async Task<ApiResponse<Saude360ItemDto>> ObterAsync(string tabela, Guid id)
    {
        if (!TabelasPermitidas.Contains(tabela)) return ApiResponse<Saude360ItemDto>.Fail("Tabela clínica não permitida.", 400);
        await using var cn = Cn();
        var row = await cn.QueryFirstOrDefaultAsync<Saude360ItemDto>(@"select id as ""Id"", tenant_id as ""TenantId"", cliente_id as ""ClienteId"", paciente_id as ""PacienteId"", medico_id as ""MedicoId"", agendamento_id as ""AgendamentoId"", consulta_id as ""ConsultaId"", coalesce(codigo,'') as ""Codigo"", coalesce(nome,'') as ""Nome"", coalesce(descricao,'') as ""Descricao"", coalesce(status,'') as ""Status"", reg_date as ""RegDate"" from plantaopro." + tabela + @" where id=@id and reg_status='A' and (@clienteId is null or cliente_id=@clienteId)", new { id, clienteId = ClienteId });
        if (row is null) return ApiResponse<Saude360ItemDto>.Fail("Registro não encontrado no tenant atual.", 404);
        await AuditarAsync("READ", tabela, id, "Acesso a registro clínico");
        return ApiResponse<Saude360ItemDto>.Ok(row, "Registro carregado.");
    }

    public async Task<ApiResponse<Guid>> CriarPacienteAsync(PacienteRequest request)
    {
        if (!ClienteId.HasValue) return TenantObrigatorio<Guid>();
        if (string.IsNullOrWhiteSpace(request.Nome)) return ApiResponse<Guid>.Fail("Informe o nome do paciente.");
        var id = Guid.NewGuid();
        await using var cn = Cn();
        await cn.ExecuteAsync(@"insert into plantaopro.pacientes(id,tenant_id,cliente_id,nome,codigo,descricao,status,metadata,created_by,reg_date,reg_status)
values(@id,@tenantId,@clienteId,@nome,@documento,@descricao,'ATIVO',cast(@metadata as jsonb),@userId,now(),'A')", new { id, tenantId = ClienteId, clienteId = ClienteId, nome = request.Nome.Trim(), documento = request.Documento.Trim(), descricao = "Cadastro de paciente", metadata = Json(new { request.DataNascimento, request.Telefone, request.Email }), userId = UserId });
        await AuditarAsync("CREATE", "pacientes", id, "Paciente criado");
        return ApiResponse<Guid>.Ok(id, "Paciente criado.");
    }

    public async Task<ApiResponse<Guid>> CriarAgendamentoAsync(AgendamentoRequest request)
    {
        if (!ClienteId.HasValue) return TenantObrigatorio<Guid>();
        var erro = ValidarAgendamento(request.PacienteId, request.DataInicio, request.DataFim);
        if (erro is not null) return ApiResponse<Guid>.Fail(erro);
        await using var cn = Cn();
        if (request.MedicoId.HasValue)
        {
            var conflito = await cn.ExecuteScalarAsync<int>(@"select count(1) from plantaopro.agendamentos where reg_status='A' and cliente_id=@clienteId and medico_id=@medicoId and status not in ('CANCELADO','FALTANTE') and data_inicio < @fim and data_fim > @inicio", new { clienteId = ClienteId, medicoId = request.MedicoId, inicio = request.DataInicio, fim = request.DataFim });
            if (conflito > 0) return ApiResponse<Guid>.Fail("Já existe agendamento para o médico no horário informado.");
        }
        var id = Guid.NewGuid();
        await cn.ExecuteAsync(@"insert into plantaopro.agendamentos(id,tenant_id,cliente_id,paciente_id,medico_id,data_inicio,data_fim,nome,descricao,status,valor_total,metadata,created_by,reg_date,reg_status)
values(@id,@tenantId,@clienteId,@pacienteId,@medicoId,@inicio,@fim,@nome,@descricao,'AGENDADO',@valor,cast(@metadata as jsonb),@userId,now(),'A')", new { id, tenantId = ClienteId, clienteId = ClienteId, pacienteId = request.PacienteId, medicoId = request.MedicoId, inicio = request.DataInicio, fim = request.DataFim, nome = N(request.Tipo, "Consulta"), descricao = request.Observacoes, valor = request.ValorPrevisto, metadata = Json(request), userId = UserId });
        await HistoricoAsync(cn, "agendamento_historico", id, "CRIAR", "AGENDADO", request.Observacoes);
        await AuditarAsync("CREATE", "agendamentos", id, "Agendamento clínico criado");
        return ApiResponse<Guid>.Ok(id, "Agendamento criado.");
    }

    public async Task<ApiResponse<Guid>> AtualizarAgendamentoAsync(Guid id, AgendamentoRequest request)
    {
        var erro = ValidarAgendamento(request.PacienteId, request.DataInicio, request.DataFim);
        if (erro is not null) return ApiResponse<Guid>.Fail(erro);
        await using var cn = Cn();
        var count = await cn.ExecuteAsync(@"update plantaopro.agendamentos set paciente_id=@pacienteId, medico_id=@medicoId, data_inicio=@inicio, data_fim=@fim, nome=@nome, descricao=@descricao, valor_total=@valor, metadata=cast(@metadata as jsonb), updated_by=@userId, updated_at=now() where id=@id and reg_status='A' and cliente_id=@clienteId", new { id, clienteId = ClienteId, pacienteId = request.PacienteId, medicoId = request.MedicoId, inicio = request.DataInicio, fim = request.DataFim, nome = N(request.Tipo, "Consulta"), descricao = request.Observacoes, valor = request.ValorPrevisto, metadata = Json(request), userId = UserId });
        if (count == 0) return ApiResponse<Guid>.Fail("Agendamento não encontrado no tenant atual.", 404);
        await HistoricoAsync(cn, "agendamento_historico", id, "ATUALIZAR", "AGENDADO", request.Observacoes);
        await AuditarAsync("UPDATE", "agendamentos", id, "Agendamento atualizado");
        return ApiResponse<Guid>.Ok(id, "Agendamento atualizado.");
    }

    public Task<ApiResponse<Guid>> ConfirmarAgendamentoAsync(Guid id) => AtualizarStatusAsync("agendamentos", id, "CONFIRMADO", "Agendamento confirmado", "agendamento_historico");
    public Task<ApiResponse<Guid>> CheckinAgendamentoAsync(Guid id) => AtualizarStatusAsync("agendamentos", id, "CHECKIN", "Check-in realizado", "agendamento_checkins");

    public async Task<ApiResponse<Guid>> CancelarAgendamentoAsync(Guid id, AgendamentoCancelamentoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Motivo)) return ApiResponse<Guid>.Fail("Cancelamento exige motivo.");
        return await AtualizarStatusAsync("agendamentos", id, "CANCELADO", request.Motivo, "agendamento_cancelamentos");
    }

    public async Task<ApiResponse<Guid>> ReagendarAsync(Guid id, AgendamentoReagendamentoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Motivo)) return ApiResponse<Guid>.Fail("Reagendamento exige motivo.");
        if (request.DataFim <= request.DataInicio) return ApiResponse<Guid>.Fail("Data final deve ser maior que a inicial.");
        await using var cn = Cn();
        var count = await cn.ExecuteAsync("update plantaopro.agendamentos set data_inicio=@inicio,data_fim=@fim,status='REAGENDADO',updated_by=@userId,updated_at=now() where id=@id and cliente_id=@clienteId and reg_status='A'", new { id, clienteId = ClienteId, inicio = request.DataInicio, fim = request.DataFim, userId = UserId });
        if (count == 0) return ApiResponse<Guid>.Fail("Agendamento não encontrado no tenant atual.", 404);
        await HistoricoAsync(cn, "agendamento_historico", id, "REAGENDAR", "REAGENDADO", request.Motivo);
        await AuditarAsync("UPDATE", "agendamentos", id, "Agendamento reagendado");
        return ApiResponse<Guid>.Ok(id, "Agendamento reagendado.");
    }

    public async Task<ApiResponse<Guid>> AcaoPainelAsync(string acao, PainelChamadaAcaoRequest request)
    {
        if (!ClienteId.HasValue) return TenantObrigatorio<Guid>();
        var status = acao == "FINALIZAR" ? "FINALIZADO" : acao == "AUSENTE" ? "AUSENTE" : "CHAMADO";
        await using var cn = Cn();
        var id = request.FilaId ?? Guid.NewGuid();
        if (!request.FilaId.HasValue)
        {
            await cn.ExecuteAsync(@"insert into plantaopro.painel_chamada_fila(id,tenant_id,cliente_id,painel_id,agendamento_id,paciente_id,setor_id,sala_id,guiche_id,codigo,nome,descricao,status,metadata,created_by,reg_date,reg_status)
values(@id,@tenantId,@clienteId,@painelId,@agendamentoId,@pacienteId,@setorId,@salaId,@guicheId,@senha,@nome,@descricao,@status,cast(@metadata as jsonb),@userId,now(),'A')", new { id, tenantId = ClienteId, clienteId = ClienteId, request.PainelId, request.AgendamentoId, request.PacienteId, request.SetorId, request.SalaId, request.GuicheId, senha = N(request.Senha, id.ToString().Substring(0, 8).ToUpperInvariant()), nome = "Senha " + N(request.Senha, id.ToString().Substring(0, 8).ToUpperInvariant()), descricao = N(request.Motivo, acao), status, metadata = Json(request), userId = UserId });
        }
        else
        {
            await cn.ExecuteAsync("update plantaopro.painel_chamada_fila set status=@status, updated_by=@userId, updated_at=now(), chamado_em=case when @status='CHAMADO' then now() else chamado_em end, finalizado_em=case when @status in ('FINALIZADO','AUSENTE') then now() else finalizado_em end where id=@id and cliente_id=@clienteId and reg_status='A'", new { id, clienteId = ClienteId, status, userId = UserId });
        }
        await HistoricoAsync(cn, "painel_chamada_historico", id, acao, status, request.Motivo);
        await AuditarAsync("UPDATE", "painel_chamada_fila", id, "Painel de chamada: " + acao);
        return ApiResponse<Guid>.Ok(id, "Ação do painel registrada.");
    }

    public async Task<ApiResponse<IEnumerable<Saude360ItemDto>>> TvPainelAsync(Guid painelId, string token)
    {
        await using var cn = Cn();
        var autorizado = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.paineis_chamada where id=@painelId and reg_status='A' and coalesce(token_seguro,'')=@token", new { painelId, token = token ?? string.Empty });
        if (autorizado == 0) return ApiResponse<IEnumerable<Saude360ItemDto>>.Fail("Token seguro do painel inválido.", 401);
        var rows = await cn.QueryAsync<Saude360ItemDto>(@"select id as ""Id"", tenant_id as ""TenantId"", cliente_id as ""ClienteId"", paciente_id as ""PacienteId"", medico_id as ""MedicoId"", agendamento_id as ""AgendamentoId"", consulta_id as ""ConsultaId"", coalesce(codigo,'') as ""Codigo"", coalesce(nome,'') as ""Nome"", coalesce(descricao,'') as ""Descricao"", coalesce(status,'') as ""Status"", reg_date as ""RegDate"" from plantaopro.painel_chamada_fila where painel_id=@painelId and reg_status='A' and status in ('CHAMADO','RECHAMADO') order by updated_at desc nulls last, reg_date desc limit 20", new { painelId });
        return ApiResponse<IEnumerable<Saude360ItemDto>>.Ok(rows, "Chamadas do painel carregadas.");
    }

    public async Task<ApiResponse<Guid>> CriarTriagemAsync(TriagemRequest request)
    {
        if (!ClienteId.HasValue) return TenantObrigatorio<Guid>();
        if (request.PacienteId == Guid.Empty) return ApiResponse<Guid>.Fail("Triagem deve estar vinculada a paciente.");
        var id = Guid.NewGuid();
        await using var cn = Cn();
        await cn.ExecuteAsync(@"insert into plantaopro.triagens(id,tenant_id,cliente_id,paciente_id,agendamento_id,nome,descricao,status,metadata,created_by,reg_date,reg_status)
values(@id,@tenantId,@clienteId,@pacienteId,@agendamentoId,@nome,@descricao,'EM_TRIAGEM',cast(@metadata as jsonb),@userId,now(),'A')", new { id, tenantId = ClienteId, clienteId = ClienteId, request.PacienteId, request.AgendamentoId, nome = N(request.ClassificacaoRisco, "Classificação pendente"), descricao = request.QueixaPrincipal, metadata = Json(request), userId = UserId });
        await HistoricoAsync(cn, "triagem_historico", id, "CRIAR", "EM_TRIAGEM", request.QueixaPrincipal);
        await AuditarAsync("CREATE", "triagens", id, "Triagem criada");
        return ApiResponse<Guid>.Ok(id, "Triagem criada.");
    }

    public Task<ApiResponse<Guid>> AtualizarTriagemAsync(Guid id, TriagemRequest request) => AtualizarRegistroClinicoAsync("triagens", id, request.PacienteId, null, request.AgendamentoId, null, N(request.ClassificacaoRisco, "Classificação pendente"), request.QueixaPrincipal, "EM_TRIAGEM", request);
    public Task<ApiResponse<Guid>> FinalizarTriagemAsync(Guid id) => AtualizarStatusAsync("triagens", id, "FINALIZADA", "Triagem finalizada e encaminhada para consulta.", "triagem_historico");

    public async Task<ApiResponse<Guid>> CriarConsultaAsync(ConsultaRequest request)
    {
        if (!ClienteId.HasValue) return TenantObrigatorio<Guid>();
        if (request.PacienteId == Guid.Empty) return ApiResponse<Guid>.Fail("Consulta deve estar vinculada a paciente.");
        var id = Guid.NewGuid();
        await using var cn = Cn();
        await cn.ExecuteAsync(@"insert into plantaopro.consultas(id,tenant_id,cliente_id,paciente_id,medico_id,agendamento_id,nome,descricao,status,metadata,created_by,reg_date,reg_status)
values(@id,@tenantId,@clienteId,@pacienteId,@medicoId,@agendamentoId,'Consulta clínica',@descricao,'ABERTA',cast(@metadata as jsonb),@userId,now(),'A')", new { id, tenantId = ClienteId, clienteId = ClienteId, request.PacienteId, request.MedicoId, request.AgendamentoId, descricao = request.Anamnese, metadata = Json(request), userId = UserId });
        await HistoricoAsync(cn, "consulta_historico", id, "CRIAR", "ABERTA", "Consulta criada");
        await AuditarAsync("CREATE", "consultas", id, "Consulta criada");
        return ApiResponse<Guid>.Ok(id, "Consulta criada.");
    }

    public Task<ApiResponse<Guid>> AtualizarConsultaAsync(Guid id, ConsultaRequest request) => AtualizarRegistroClinicoAsync("consultas", id, request.PacienteId, request.MedicoId, request.AgendamentoId, null, "Consulta clínica", request.Anamnese, "EM_ATENDIMENTO", request);
    public Task<ApiResponse<Guid>> IniciarConsultaAsync(Guid id) => AtualizarStatusAsync("consultas", id, "EM_ATENDIMENTO", "Consulta iniciada", "consulta_historico");
    public Task<ApiResponse<Guid>> FinalizarConsultaAsync(Guid id) => AtualizarStatusAsync("consultas", id, "FINALIZADA", "Consulta finalizada", "consulta_historico");
    public async Task<ApiResponse<Guid>> CancelarConsultaAsync(Guid id, ConsultaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Motivo)) return ApiResponse<Guid>.Fail("Cancelamento da consulta exige motivo.");
        return await AtualizarStatusAsync("consultas", id, "CANCELADA", request.Motivo, "consulta_historico");
    }

    public async Task<ApiResponse<Guid>> CriarCidAsync(CidRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Codigo) || string.IsNullOrWhiteSpace(request.Descricao)) return ApiResponse<Guid>.Fail("Informe código e descrição do CID.");
        await using var cn = Cn();
        var duplicado = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.cid_tabela where upper(codigo)=upper(@codigo) and reg_status='A'", new { request.Codigo });
        if (duplicado > 0) return ApiResponse<Guid>.Fail("CID já cadastrado com o código informado.");
        var id = Guid.NewGuid();
        await cn.ExecuteAsync("insert into plantaopro.cid_tabela(id,codigo,nome,descricao,status,metadata,created_by,reg_date,reg_status) values(@id,@codigo,@nome,@descricao,'ATIVO',cast(@metadata as jsonb),@userId,now(),'A')", new { id, codigo = request.Codigo.Trim().ToUpperInvariant(), nome = request.Codigo.Trim().ToUpperInvariant(), descricao = request.Descricao.Trim(), metadata = Json(request), userId = UserId });
        await AuditarAsync("CREATE", "cid_tabela", id, "CID criado");
        return ApiResponse<Guid>.Ok(id, "CID criado.");
    }

    public Task<ApiResponse<Guid>> AtualizarCidAsync(Guid id, CidRequest request) => AtualizarRegistroClinicoAsync("cid_tabela", id, null, null, null, null, request.Codigo, request.Descricao, "ATIVO", request);
    public Task<ApiResponse<Guid>> InativarCidAsync(Guid id) => AtualizarStatusAsync("cid_tabela", id, "INATIVO", "CID inativado", "cid_uso_historico");
    public Task<ApiResponse<IEnumerable<Saude360ItemDto>>> BuscarCidAsync(string termo) => ListarBuscaAsync("cid_tabela", termo);
    public Task<ApiResponse<Guid>> FavoritarCidAsync(Guid id) => CriarFavoritoCidAsync(id);

    public async Task<ApiResponse<Guid>> ImportarCidAsync(IEnumerable<CidRequest> requests)
    {
        var total = 0;
        foreach (var request in requests ?? Array.Empty<CidRequest>())
        {
            var result = await CriarCidAsync(request);
            if (result.Success) total++;
        }
        return ApiResponse<Guid>.Ok(Guid.Empty, total + " CID(s) importados sem duplicidade.");
    }

    public Task<ApiResponse<Guid>> CriarPrescricaoAsync(PrescricaoRequest request) => CriarRegistroClinicoAsync("prescricoes", request.PacienteId, request.MedicoId, null, request.ConsultaId, "Prescrição médica", request.Orientacoes, "RASCUNHO", request, "Prescrição criada");
    public Task<ApiResponse<Guid>> AtualizarPrescricaoAsync(Guid id, PrescricaoRequest request) => AtualizarRegistroClinicoAsync("prescricoes", id, request.PacienteId, request.MedicoId, null, request.ConsultaId, "Prescrição médica", request.Orientacoes, "RASCUNHO", request);
    public Task<ApiResponse<Guid>> FinalizarPrescricaoAsync(Guid id) => AtualizarStatusAsync("prescricoes", id, "FINALIZADA", "Prescrição finalizada", "prescricao_historico");
    public async Task<ApiResponse<Guid>> CancelarPrescricaoAsync(Guid id, PrescricaoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Justificativa)) return ApiResponse<Guid>.Fail("Cancelamento de prescrição exige justificativa.");
        return await AtualizarStatusAsync("prescricoes", id, "CANCELADA", request.Justificativa, "prescricao_cancelamentos");
    }

    public async Task<ApiResponse<Guid>> CriarModeloPrescricaoAsync(PrescricaoModeloRequest request)
    {
        if (!ClienteId.HasValue) return TenantObrigatorio<Guid>();
        if (string.IsNullOrWhiteSpace(request.Nome)) return ApiResponse<Guid>.Fail("Informe o nome do modelo.");
        var id = Guid.NewGuid();
        await using var cn = Cn();
        await cn.ExecuteAsync("insert into plantaopro.prescricao_modelos(id,tenant_id,cliente_id,nome,descricao,status,metadata,created_by,reg_date,reg_status) values(@id,@tenantId,@clienteId,@nome,@descricao,'ATIVO',cast(@metadata as jsonb),@userId,now(),'A')", new { id, tenantId = ClienteId, clienteId = ClienteId, nome = request.Nome.Trim(), descricao = request.ItensResumo, metadata = Json(request), userId = UserId });
        await AuditarAsync("CREATE", "prescricao_modelos", id, "Modelo de prescrição criado");
        return ApiResponse<Guid>.Ok(id, "Modelo criado.");
    }

    public async Task<ApiResponse<Guid>> ReceberAsync(ClinicaRecebimentoRequest request)
    {
        if (!ClienteId.HasValue) return TenantObrigatorio<Guid>();
        if (request.Valor <= 0) return ApiResponse<Guid>.Fail("Recebimento exige valor maior que zero.");
        if (string.IsNullOrWhiteSpace(request.FormaPagamento)) return ApiResponse<Guid>.Fail("Recebimento exige forma de pagamento.");
        var id = Guid.NewGuid();
        await using var cn = Cn();
        await cn.ExecuteAsync("insert into plantaopro.clinica_recebimentos(id,tenant_id,cliente_id,conta_receber_id,nome,descricao,status,valor_pago,metadata,created_by,reg_date,reg_status) values(@id,@tenantId,@clienteId,@contaId,@nome,@descricao,'RECEBIDO',@valor,cast(@metadata as jsonb),@userId,now(),'A')", new { id, tenantId = ClienteId, clienteId = ClienteId, contaId = request.ContaReceberId, nome = request.FormaPagamento.Trim(), descricao = "Recebimento clínico", valor = request.Valor, metadata = Json(request), userId = UserId });
        await AuditarAsync("CREATE", "clinica_recebimentos", id, "Recebimento clínico registrado");
        return ApiResponse<Guid>.Ok(id, "Recebimento registrado.");
    }

    public async Task<ApiResponse<Guid>> EstornarFinanceiroAsync(Guid? id, ClinicaRecebimentoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Justificativa)) return ApiResponse<Guid>.Fail("Estorno/cancelamento exige justificativa.");
        return await AtualizarStatusAsync("clinica_recebimentos", id ?? Guid.Empty, "ESTORNADO", request.Justificativa, "clinica_lancamentos");
    }

    public Task<ApiResponse<Guid>> FecharCaixaAsync(ClinicaRecebimentoRequest request) => CriarRegistroClinicoAsync("clinica_fechamentos_caixa", null, null, null, null, "Fechamento de caixa", request.Justificativa, "FECHADO", request, "Fechamento de caixa auditado");

    public async Task<ApiResponse<Guid>> CriarConvenioAsync(ConvenioRequest request)
    {
        if (!ClienteId.HasValue) return TenantObrigatorio<Guid>();
        if (string.IsNullOrWhiteSpace(request.Nome)) return ApiResponse<Guid>.Fail("Informe o nome do convênio.");
        return await CriarRegistroClinicoAsync("convenios", null, null, null, null, request.Nome, request.RegistroAns, "ATIVO", request, "Convênio criado");
    }

    public Task<ApiResponse<Guid>> AtualizarConvenioAsync(Guid id, ConvenioRequest request) => AtualizarRegistroClinicoAsync("convenios", id, null, null, null, null, request.Nome, request.RegistroAns, "ATIVO", request);
    public async Task<ApiResponse<Guid>> InativarConvenioAsync(Guid id, ConvenioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Motivo)) return ApiResponse<Guid>.Fail("Inativação de convênio exige motivo.");
        return await AtualizarStatusAsync("convenios", id, "INATIVO", request.Motivo, "convenio_glosas");
    }
    public Task<ApiResponse<Guid>> CriarPlanoConvenioAsync(Guid convenioId, PlanoSaudeRequest request) => CriarRegistroClinicoAsync("convenio_planos", null, null, null, null, request.Nome, request.CoberturaResumo, "ATIVO", new { convenioId, request }, "Plano de convênio criado");
    public Task<ApiResponse<Guid>> CriarAutorizacaoConvenioAsync(ConvenioAutorizacaoRequest request) => CriarRegistroClinicoAsync("convenio_autorizacoes", request.PacienteId, null, null, null, N(request.Codigo, "Autorização"), request.Motivo, "PENDENTE", request, "Autorização criada");
    public async Task<ApiResponse<Guid>> DecidirAutorizacaoAsync(Guid id, string status, ConvenioAutorizacaoRequest request)
    {
        if (status == "NEGADA" && string.IsNullOrWhiteSpace(request.Motivo)) return ApiResponse<Guid>.Fail("Negar autorização exige motivo.");
        return await AtualizarStatusAsync("convenio_autorizacoes", id, status, request.Motivo, "convenio_glosas");
    }

    public async Task<ApiResponse<Guid>> CriarPlanoSaudeAsync(PlanoSaudeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome)) return ApiResponse<Guid>.Fail("Informe o nome do plano de saúde.");
        return await CriarRegistroClinicoAsync("planos_saude", null, null, null, null, request.Nome, request.CoberturaResumo, "ATIVO", request, "Plano de saúde criado");
    }
    public Task<ApiResponse<Guid>> AtualizarPlanoSaudeAsync(Guid id, PlanoSaudeRequest request) => AtualizarRegistroClinicoAsync("planos_saude", id, null, null, null, null, request.Nome, request.CoberturaResumo, "ATIVO", request);
    public async Task<ApiResponse<Guid>> InativarPlanoSaudeAsync(Guid id, PlanoSaudeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Motivo)) return ApiResponse<Guid>.Fail("Inativação de plano exige motivo.");
        return await AtualizarStatusAsync("planos_saude", id, "INATIVO", request.Motivo, "plano_saude_historico");
    }

    public async Task<ApiResponse<Guid>> VincularPlanoPacienteAsync(Guid pacienteId, PlanoSaudePacienteRequest request)
    {
        if (!ClienteId.HasValue) return TenantObrigatorio<Guid>();
        if (pacienteId == Guid.Empty || request.PlanoSaudeId == Guid.Empty) return ApiResponse<Guid>.Fail("Informe paciente e plano de saúde.");
        await using var cn = Cn();
        if (request.Principal)
        {
            await cn.ExecuteAsync("update plantaopro.plano_saude_pacientes set metadata=jsonb_set(coalesce(metadata,'{}'::jsonb),'{principal}','false'::jsonb), updated_at=now(), updated_by=@userId where paciente_id=@pacienteId and cliente_id=@clienteId", new { pacienteId, clienteId = ClienteId, userId = UserId });
        }
        var id = Guid.NewGuid();
        await cn.ExecuteAsync("insert into plantaopro.plano_saude_pacientes(id,tenant_id,cliente_id,paciente_id,nome,descricao,status,metadata,created_by,reg_date,reg_status) values(@id,@tenantId,@clienteId,@pacienteId,@nome,@descricao,'ATIVO',cast(@metadata as jsonb),@userId,now(),'A')", new { id, tenantId = ClienteId, clienteId = ClienteId, pacienteId, nome = request.PlanoSaudeId.ToString(), descricao = request.NumeroCarteirinha, metadata = Json(request), userId = UserId });
        await AuditarAsync("CREATE", "plano_saude_pacientes", id, "Plano vinculado ao paciente");
        return ApiResponse<Guid>.Ok(id, "Plano vinculado ao paciente.");
    }

    private async Task<ApiResponse<IEnumerable<Saude360ItemDto>>> ListarBuscaAsync(string tabela, string termo)
    {
        await using var cn = Cn();
        var rows = await cn.QueryAsync<Saude360ItemDto>(@"select id as ""Id"", tenant_id as ""TenantId"", cliente_id as ""ClienteId"", paciente_id as ""PacienteId"", medico_id as ""MedicoId"", agendamento_id as ""AgendamentoId"", consulta_id as ""ConsultaId"", coalesce(codigo,'') as ""Codigo"", coalesce(nome,'') as ""Nome"", coalesce(descricao,'') as ""Descricao"", coalesce(status,'') as ""Status"", reg_date as ""RegDate"" from plantaopro." + tabela + @" where reg_status='A' and (unaccent(coalesce(codigo,'') || ' ' || coalesce(nome,'') || ' ' || coalesce(descricao,'')) ilike unaccent(@termo)) order by codigo limit 100", new { termo = "%" + (termo ?? string.Empty).Trim() + "%" });
        return ApiResponse<IEnumerable<Saude360ItemDto>>.Ok(rows, "Busca realizada.");
    }

    private async Task<ApiResponse<Guid>> CriarRegistroClinicoAsync(string tabela, Guid? pacienteId, Guid? medicoId, Guid? agendamentoId, Guid? consultaId, string nome, string descricao, string status, object payload, string mensagem)
    {
        if (!ClienteId.HasValue) return TenantObrigatorio<Guid>();
        var id = Guid.NewGuid();
        await using var cn = Cn();
        await cn.ExecuteAsync("insert into plantaopro." + tabela + "(id,tenant_id,cliente_id,paciente_id,medico_id,agendamento_id,consulta_id,nome,descricao,status,metadata,created_by,reg_date,reg_status) values(@id,@tenantId,@clienteId,@pacienteId,@medicoId,@agendamentoId,@consultaId,@nome,@descricao,@status,cast(@metadata as jsonb),@userId,now(),'A')", new { id, tenantId = ClienteId, clienteId = ClienteId, pacienteId, medicoId, agendamentoId, consultaId, nome, descricao, status, metadata = Json(payload), userId = UserId });
        await AuditarAsync("CREATE", tabela, id, mensagem);
        return ApiResponse<Guid>.Ok(id, mensagem + ".");
    }

    private async Task<ApiResponse<Guid>> AtualizarRegistroClinicoAsync(string tabela, Guid id, Guid? pacienteId, Guid? medicoId, Guid? agendamentoId, Guid? consultaId, string nome, string descricao, string status, object payload)
    {
        await using var cn = Cn();
        var count = await cn.ExecuteAsync("update plantaopro." + tabela + " set paciente_id=@pacienteId, medico_id=@medicoId, agendamento_id=@agendamentoId, consulta_id=@consultaId, nome=@nome, descricao=@descricao, status=@status, metadata=cast(@metadata as jsonb), updated_by=@userId, updated_at=now() where id=@id and reg_status='A' and (@clienteId is null or cliente_id=@clienteId)", new { id, clienteId = ClienteId, pacienteId, medicoId, agendamentoId, consultaId, nome, descricao, status, metadata = Json(payload), userId = UserId });
        if (count == 0) return ApiResponse<Guid>.Fail("Registro não encontrado no tenant atual.", 404);
        await AuditarAsync("UPDATE", tabela, id, "Registro clínico atualizado");
        return ApiResponse<Guid>.Ok(id, "Registro atualizado.");
    }

    private async Task<ApiResponse<Guid>> AtualizarStatusAsync(string tabela, Guid id, string status, string motivo, string historico)
    {
        if (id == Guid.Empty) return ApiResponse<Guid>.Fail("Informe o identificador do registro.");
        await using var cn = Cn();
        var count = await cn.ExecuteAsync("update plantaopro." + tabela + " set status=@status, updated_by=@userId, updated_at=now() where id=@id and reg_status='A' and (@clienteId is null or cliente_id=@clienteId)", new { id, clienteId = ClienteId, status, userId = UserId });
        if (count == 0) return ApiResponse<Guid>.Fail("Registro não encontrado no tenant atual.", 404);
        await HistoricoAsync(cn, historico, id, status, status, motivo);
        await AuditarAsync("UPDATE", tabela, id, motivo);
        return ApiResponse<Guid>.Ok(id, "Status atualizado.");
    }

    private async Task CriarFavoritoCidAsyncInternal(NpgsqlConnection cn, Guid cidId)
    {
        await cn.ExecuteAsync("insert into plantaopro.cid_favoritos(id,tenant_id,cliente_id,nome,descricao,status,metadata,created_by,reg_date,reg_status) values(gen_random_uuid(),@tenantId,@clienteId,'Favorito CID',@descricao,'ATIVO',cast(@metadata as jsonb),@userId,now(),'A')", new { tenantId = ClienteId, clienteId = ClienteId, descricao = cidId.ToString(), metadata = Json(new { cidId }), userId = UserId });
    }

    private async Task<ApiResponse<Guid>> CriarFavoritoCidAsync(Guid cidId)
    {
        if (!ClienteId.HasValue) return TenantObrigatorio<Guid>();
        await using var cn = Cn();
        await CriarFavoritoCidAsyncInternal(cn, cidId);
        await AuditarAsync("CREATE", "cid_favoritos", cidId, "CID favoritado por médico/tenant");
        return ApiResponse<Guid>.Ok(cidId, "CID favoritado.");
    }

    private async Task HistoricoAsync(NpgsqlConnection cn, string tabela, Guid referenciaId, string acao, string status, string? descricao)
    {
        if (!TabelasPermitidas.Contains(tabela)) return;
        await cn.ExecuteAsync("insert into plantaopro." + tabela + "(id,tenant_id,cliente_id,nome,descricao,status,metadata,created_by,reg_date,reg_status) values(gen_random_uuid(),@tenantId,@clienteId,@nome,@descricao,@status,cast(@metadata as jsonb),@userId,now(),'A')", new { tenantId = ClienteId, clienteId = ClienteId, nome = acao, descricao = descricao ?? string.Empty, status, metadata = Json(new { referenciaId, acao }), userId = UserId });
    }

    private async Task AuditarAsync(string acao, string tabela, Guid id, string descricao)
    {
        await audit.LogAsync(UserId, acao, tabela, id, descricao);
    }

    private static string? ValidarAgendamento(Guid pacienteId, DateTime inicio, DateTime fim)
    {
        if (pacienteId == Guid.Empty) return "Agendamento deve estar vinculado a paciente.";
        if (fim <= inicio) return "Data final deve ser maior que a inicial.";
        return null;
    }

    private static readonly HashSet<string> ColunasFiltroPermitidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "paciente_id", "medico_id", "agendamento_id", "consulta_id", "status"
    };

    private static readonly HashSet<string> TabelasPermitidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "pacientes", "paciente_contatos", "paciente_enderecos", "paciente_documentos", "paciente_convenios", "paciente_historico",
        "paineis_chamada", "painel_chamada_configuracoes", "painel_chamada_setores", "painel_chamada_salas", "painel_chamada_guiches", "painel_chamada_fila", "painel_chamada_historico",
        "agendamentos", "agendamento_historico", "agendamento_bloqueios", "agendamento_cancelamentos", "agendamento_checkins",
        "triagens", "triagem_sinais_vitais", "triagem_classificacoes_risco", "triagem_historico",
        "consultas", "consulta_anamnese", "consulta_exame_fisico", "consulta_diagnosticos", "consulta_condutas", "consulta_encaminhamentos", "consulta_historico",
        "cid_tabela", "cid_favoritos", "cid_uso_historico",
        "prescricoes", "prescricao_itens", "prescricao_modelos", "prescricao_historico", "prescricao_cancelamentos",
        "clinica_contas_receber", "clinica_recebimentos", "clinica_caixa", "clinica_fechamentos_caixa", "clinica_repasses", "clinica_lancamentos", "clinica_glosas",
        "convenios", "convenio_contratos", "convenio_planos", "convenio_tabelas", "convenio_procedimentos", "convenio_autorizacoes", "convenio_glosas", "convenio_faturamentos",
        "planos_saude", "plano_saude_coberturas", "plano_saude_pacientes", "plano_saude_autorizacoes", "plano_saude_historico"
    };
}
