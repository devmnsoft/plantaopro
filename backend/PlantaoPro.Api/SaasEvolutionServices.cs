using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public interface ILogOperacionalService : IAuditService { }
public interface ILgpdAuditService : IAuditService { }
public interface IEventoSistemaService : IAuditService { }

public sealed class LogOperacionalService : ILogOperacionalService
{
    private readonly IAuditService inner;
    public LogOperacionalService(IAuditService inner) { this.inner = inner; }
    public Task RegistrarAsync(Guid? usuarioId, Guid? clienteId, string entidade, Guid? entidadeId, string acao, object? detalhes, bool sucesso, string? ipOrigem, string? perfil, CancellationToken ct = default) => inner.RegistrarAsync(usuarioId, clienteId, entidade, entidadeId, acao, detalhes, sucesso, ipOrigem, perfil, ct);
    public Task LogAsync(Guid? userId, string acao, string entidade, Guid? registroId, string descricao, string? valorAnterior = null, string? valorNovo = null, string? ip = null, string? userAgent = null) => inner.LogAsync(userId, acao, entidade, registroId, descricao, valorAnterior, valorNovo, ip, userAgent);
}

public sealed class LgpdAuditService : ILgpdAuditService
{
    private readonly IAuditService inner;
    public LgpdAuditService(IAuditService inner) { this.inner = inner; }
    public Task RegistrarAsync(Guid? usuarioId, Guid? clienteId, string entidade, Guid? entidadeId, string acao, object? detalhes, bool sucesso, string? ipOrigem, string? perfil, CancellationToken ct = default) => inner.RegistrarAsync(usuarioId, clienteId, entidade, entidadeId, acao, detalhes, sucesso, ipOrigem, perfil, ct);
    public Task LogAsync(Guid? userId, string acao, string entidade, Guid? registroId, string descricao, string? valorAnterior = null, string? valorNovo = null, string? ip = null, string? userAgent = null) => inner.LogAsync(userId, acao, entidade, registroId, descricao, valorAnterior, valorNovo, ip, userAgent);
}

public sealed class EventoSistemaService : IEventoSistemaService
{
    private readonly IAuditService inner;
    public EventoSistemaService(IAuditService inner) { this.inner = inner; }
    public Task RegistrarAsync(Guid? usuarioId, Guid? clienteId, string entidade, Guid? entidadeId, string acao, object? detalhes, bool sucesso, string? ipOrigem, string? perfil, CancellationToken ct = default) => inner.RegistrarAsync(usuarioId, clienteId, entidade, entidadeId, acao, detalhes, sucesso, ipOrigem, perfil, ct);
    public Task LogAsync(Guid? userId, string acao, string entidade, Guid? registroId, string descricao, string? valorAnterior = null, string? valorNovo = null, string? ip = null, string? userAgent = null) => inner.LogAsync(userId, acao, entidade, registroId, descricao, valorAnterior, valorNovo, ip, userAgent);
}

public sealed class LgpdService
{
    private readonly IConfiguration cfg;
    private readonly ILgpdAuditService audit;
    private readonly ILogger<LgpdService> logger;
    public LgpdService(IConfiguration cfg, ILgpdAuditService audit, ILogger<LgpdService> logger) { this.cfg = cfg; this.audit = audit; this.logger = logger; }

    public async Task<ApiResponse<LgpdPoliticaDto>> PoliticaAtualAsync()
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var item = await cn.QueryFirstOrDefaultAsync<LgpdPoliticaDto>(@"select id as ""Id"", coalesce(versao,'') as ""Versao"", coalesce(titulo,'') as ""Titulo"", coalesce(conteudo,'') as ""Conteudo"", vigente_desde as ""VigenteDesde"" from plantaopro.lgpd_politicas where publicada=true and reg_status='A' order by vigente_desde desc limit 1");
            item ??= new LgpdPoliticaDto { Id = Guid.Empty, Versao = "2026.06", Titulo = "Política de Privacidade PlantãoPro", Conteudo = "Tratamos dados para gestão de plantões, finanças, comunicação operacional, auditoria, suporte, faturamento SaaS, Customer Success e obrigações legais.", VigenteDesde = DateTime.UtcNow.Date };
            return ApiResponse<LgpdPoliticaDto>.Ok(item);
        }
        catch (Exception ex) { logger.LogError(ex, "Erro LGPD política"); return ApiResponse<LgpdPoliticaDto>.Fail("Não foi possível carregar a política LGPD.", 500); }
    }

    public async Task<ApiResponse<IEnumerable<LgpdConsentimentoDto>>> ConsentimentosAsync(Guid? usuarioId)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var rows = await cn.QueryAsync<LgpdConsentimentoDto>(@"select id as ""Id"", usuario_id as ""UsuarioId"", coalesce(finalidade,'') as ""Finalidade"", coalesce(base_legal,'') as ""BaseLegal"", coalesce(versao_politica,'') as ""VersaoPolitica"", consentido as ""Consentido"", coalesce(ip,'') as ""Ip"", reg_date as ""RegDate"" from plantaopro.lgpd_consentimentos where reg_status='A' and (@usuarioId is null or usuario_id=@usuarioId) order by reg_date desc limit 100", new { usuarioId });
            return ApiResponse<IEnumerable<LgpdConsentimentoDto>>.Ok(rows);
        }
        catch (Exception ex) { logger.LogError(ex, "Erro LGPD consentimentos"); return ApiResponse<IEnumerable<LgpdConsentimentoDto>>.Fail("Não foi possível listar consentimentos.", 500); }
    }

    public async Task<ApiResponse<Guid>> RegistrarConsentimentoAsync(Guid? usuarioId, RegistrarConsentimentoRequest request, string? ip, string? perfil)
    {
        try
        {
            var erros = new List<string>();
            if (string.IsNullOrWhiteSpace(request.Finalidade)) erros.Add("Finalidade é obrigatória.");
            if (string.IsNullOrWhiteSpace(request.BaseLegal)) erros.Add("Base legal é obrigatória.");
            if (erros.Count > 0) return ApiResponse<Guid>.Fail("Verifique os dados do consentimento.", 400, erros);
            var id = Guid.NewGuid();
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync(@"insert into plantaopro.lgpd_consentimentos(id, usuario_id, finalidade, base_legal, versao_politica, consentido, ip, user_agent, reg_status, reg_date) values(@id,@usuarioId,@Finalidade,@BaseLegal,@VersaoPolitica,@Consentido,@ip,'','A',now())", new { id, usuarioId, request.Finalidade, request.BaseLegal, request.VersaoPolitica, request.Consentido, ip });
            await RegistrarEventoAsync(usuarioId, "CONSENTIMENTO_REGISTRADO", request.Finalidade, ip);
            await audit.RegistrarAsync(usuarioId, null, "lgpd_consentimentos", id, "REGISTRAR", new { request.Finalidade, request.BaseLegal, request.Consentido }, true, ip, perfil);
            return ApiResponse<Guid>.Ok(id, "Consentimento registrado com sucesso.");
        }
        catch (Exception ex) { logger.LogError(ex, "Erro registrar consentimento LGPD"); return ApiResponse<Guid>.Fail("Não foi possível registrar consentimento.", 500); }
    }

    public async Task<ApiResponse<IEnumerable<LgpdSolicitacaoDto>>> SolicitacoesAsync(Guid? usuarioId, Guid? clienteId, bool admin)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var rows = await cn.QueryAsync<LgpdSolicitacaoDto>(@"select id as ""Id"", usuario_id as ""UsuarioId"", cliente_id as ""ClienteId"", coalesce(tipo,'') as ""Tipo"", coalesce(status,'') as ""Status"", coalesce(descricao,'') as ""Descricao"", coalesce(resposta,'') as ""Resposta"", reg_date as ""RegDate"", respondida_em as ""RespondidaEm"" from plantaopro.lgpd_solicitacoes_titular where reg_status='A' and (@admin or usuario_id=@usuarioId or cliente_id=@clienteId) order by reg_date desc limit 100", new { admin, usuarioId, clienteId });
            return ApiResponse<IEnumerable<LgpdSolicitacaoDto>>.Ok(rows);
        }
        catch (Exception ex) { logger.LogError(ex, "Erro listar solicitações LGPD"); return ApiResponse<IEnumerable<LgpdSolicitacaoDto>>.Fail("Não foi possível listar solicitações LGPD.", 500); }
    }

    public async Task<ApiResponse<Guid>> CriarSolicitacaoAsync(Guid? usuarioId, Guid? clienteId, CriarSolicitacaoLgpdRequest request, string? ip, string? perfil)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Tipo) || string.IsNullOrWhiteSpace(request.Descricao)) return ApiResponse<Guid>.Fail("Tipo e descrição são obrigatórios.", 400);
            var id = Guid.NewGuid();
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync(@"insert into plantaopro.lgpd_solicitacoes_titular(id, usuario_id, cliente_id, tipo, status, descricao, reg_status, reg_date) values(@id,@usuarioId,@clienteId,upper(@Tipo),'ABERTA',@Descricao,'A',now())", new { id, usuarioId, clienteId, request.Tipo, request.Descricao });
            await RegistrarEventoAsync(usuarioId, "SOLICITACAO_CRIADA", request.Tipo, ip);
            await audit.RegistrarAsync(usuarioId, clienteId, "lgpd_solicitacoes_titular", id, "CRIAR", new { request.Tipo }, true, ip, perfil);
            return ApiResponse<Guid>.Ok(id, "Solicitação LGPD criada com sucesso.");
        }
        catch (Exception ex) { logger.LogError(ex, "Erro criar solicitação LGPD"); return ApiResponse<Guid>.Fail("Não foi possível criar solicitação LGPD.", 500); }
    }

    public async Task<ApiResponse<string>> ResponderSolicitacaoAsync(Guid id, ResponderSolicitacaoLgpdRequest request, Guid? usuarioId, string? ip, string? perfil)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Resposta)) return ApiResponse<string>.Fail("Resposta é obrigatória.", 400);
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var rows = await cn.ExecuteAsync(@"update plantaopro.lgpd_solicitacoes_titular set resposta=@Resposta, status=upper(@Status), respondida_por=@usuarioId, respondida_em=now(), reg_update=now() where id=@id and reg_status='A'", new { id, request.Resposta, request.Status, usuarioId });
            if (rows == 0) return ApiResponse<string>.Fail("Solicitação não encontrada.", 404);
            await RegistrarEventoAsync(usuarioId, "SOLICITACAO_RESPONDIDA", id.ToString(), ip);
            await audit.RegistrarAsync(usuarioId, null, "lgpd_solicitacoes_titular", id, "RESPONDER", new { request.Status }, true, ip, perfil);
            return ApiResponse<string>.Ok("ok", "Solicitação respondida com sucesso.");
        }
        catch (Exception ex) { logger.LogError(ex, "Erro responder LGPD"); return ApiResponse<string>.Fail("Não foi possível responder solicitação LGPD.", 500); }
    }

    public async Task<ApiResponse<object>> ExportarMeusDadosAsync(Guid? usuarioId, Guid? clienteId, string? ip, string? perfil)
    {
        try
        {
            var exportacaoId = Guid.NewGuid();
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync(@"insert into plantaopro.lgpd_exportacoes_dados(id, usuario_id, cliente_id, status, ip, reg_status, reg_date) values(@exportacaoId,@usuarioId,@clienteId,'GERADA',@ip,'A',now())", new { exportacaoId, usuarioId, clienteId, ip });
            var dados = new { ExportacaoId = exportacaoId, UsuarioId = usuarioId, ClienteId = clienteId, GeradoEm = DateTime.UtcNow, Aviso = "Exportação inclui dados cadastrais e solicitações LGPD permitidas. Dados financeiros/auditoria seguem retenção legal." };
            await RegistrarEventoAsync(usuarioId, "EXPORTACAO_DADOS", exportacaoId.ToString(), ip);
            await audit.RegistrarAsync(usuarioId, clienteId, "lgpd_exportacoes_dados", exportacaoId, "EXPORTAR", new { exportacaoId }, true, ip, perfil);
            return ApiResponse<object>.Ok(dados, "Exportação registrada com sucesso.");
        }
        catch (Exception ex) { logger.LogError(ex, "Erro exportar dados LGPD"); return ApiResponse<object>.Fail("Não foi possível exportar os dados.", 500); }
    }

    public async Task<ApiResponse<string>> AnonimizarAsync(Guid usuarioId, Guid? operadorId, string? ip, string? perfil)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();
            await cn.ExecuteAsync(@"insert into plantaopro.lgpd_anonimizacoes(id, usuario_id, motivo, status, reg_status, reg_date) values(gen_random_uuid(),@usuarioId,'Solicitação do titular respeitando retenções legais','REGISTRADA','A',now())", new { usuarioId }, tx);
            await cn.ExecuteAsync(@"update plantaopro.usuarios set nome='Usuário anonimizado', email=concat('anonimizado+', id, '@plantaopro.local'), telefone=null, reg_update=now() where id=@usuarioId", new { usuarioId }, tx);
            await tx.CommitAsync();
            await RegistrarEventoAsync(operadorId, "ANONIMIZACAO_REGISTRADA", usuarioId.ToString(), ip);
            await audit.RegistrarAsync(operadorId, null, "usuarios", usuarioId, "ANONIMIZAR", new { usuarioId }, true, ip, perfil);
            return ApiResponse<string>.Ok("ok", "Anonimização registrada respeitando obrigações legais.");
        }
        catch (Exception ex) { logger.LogError(ex, "Erro anonimizar usuário LGPD"); return ApiResponse<string>.Fail("Não foi possível anonimizar o usuário.", 500); }
    }

    public async Task<ApiResponse<IEnumerable<LgpdEventoDto>>> EventosAsync(Guid? usuarioId, bool admin)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var rows = await cn.QueryAsync<LgpdEventoDto>(@"select id as ""Id"", usuario_id as ""UsuarioId"", coalesce(acao,'') as ""Acao"", coalesce(detalhes,'') as ""Detalhes"", coalesce(ip,'') as ""Ip"", reg_date as ""RegDate"" from plantaopro.lgpd_eventos_privacidade where reg_status='A' and (@admin or usuario_id=@usuarioId) order by reg_date desc limit 100", new { admin, usuarioId });
            return ApiResponse<IEnumerable<LgpdEventoDto>>.Ok(rows);
        }
        catch (Exception ex) { logger.LogError(ex, "Erro eventos LGPD"); return ApiResponse<IEnumerable<LgpdEventoDto>>.Fail("Não foi possível listar eventos LGPD.", 500); }
    }

    private async Task RegistrarEventoAsync(Guid? usuarioId, string acao, string detalhes, string? ip)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync(@"insert into plantaopro.lgpd_eventos_privacidade(id, usuario_id, acao, detalhes, ip, reg_status, reg_date) values(gen_random_uuid(),@usuarioId,@acao,@detalhes,@ip,'A',now())", new { usuarioId, acao, detalhes, ip });
        }
        catch (Exception ex) { logger.LogWarning(ex, "Falha não bloqueante ao registrar evento LGPD"); }
    }
}

public sealed class JornadaClienteService
{
    private static readonly string[] Etapas = new[] { "LEAD_CADASTRADO", "DEMONSTRACAO_AGENDADA", "DEMONSTRACAO_REALIZADA", "PROPOSTA_ENVIADA", "NEGOCIACAO", "CLIENTE_CONVERTIDO", "IMPLANTACAO", "TREINAMENTO", "OPERACAO_ASSISTIDA", "ATIVO", "EXPANSAO", "RISCO", "CANCELADO" };
    private readonly IConfiguration cfg;
    private readonly IEventoSistemaService audit;
    private readonly ILogger<JornadaClienteService> logger;
    public JornadaClienteService(IConfiguration cfg, IEventoSistemaService audit, ILogger<JornadaClienteService> logger) { this.cfg = cfg; this.audit = audit; this.logger = logger; }

    public async Task<ApiResponse<IEnumerable<JornadaClienteDto>>> ListarAsync()
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var rows = await cn.QueryAsync<JornadaClienteDto>(@"select j.cliente_id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"", coalesce(j.etapa,'LEAD_CADASTRADO') as ""Etapa"", coalesce(j.responsavel,'') as ""Responsavel"", coalesce(j.proxima_acao,'') as ""ProximaAcao"", j.reg_date as ""RegDate"" from plantaopro.jornada_cliente j left join plantaopro.clientes c on c.id=j.cliente_id where j.reg_status='A' order by j.reg_date desc limit 100");
            return ApiResponse<IEnumerable<JornadaClienteDto>>.Ok(rows);
        }
        catch (Exception ex) { logger.LogError(ex, "Erro jornada clientes"); return ApiResponse<IEnumerable<JornadaClienteDto>>.Fail("Não foi possível listar jornadas.", 500); }
    }

    public async Task<ApiResponse<JornadaClienteDetalheDto>> DetalharAsync(Guid clienteId)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var row = await cn.QueryFirstOrDefaultAsync<JornadaClienteDto>(@"select j.cliente_id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"", coalesce(j.etapa,'LEAD_CADASTRADO') as ""Etapa"", coalesce(j.responsavel,'') as ""Responsavel"", coalesce(j.proxima_acao,'') as ""ProximaAcao"", j.reg_date as ""RegDate"" from plantaopro.jornada_cliente j left join plantaopro.clientes c on c.id=j.cliente_id where j.cliente_id=@clienteId and j.reg_status='A'", new { clienteId });
            if (row is null) return ApiResponse<JornadaClienteDetalheDto>.Fail("Jornada não encontrada.", 404);

            var eventos = await ListarEventosJornadaAsync(cn, clienteId);
            var tarefas = await ListarTarefasJornadaAsync(cn, clienteId);
            return ApiResponse<JornadaClienteDetalheDto>.Ok(new JornadaClienteDetalheDto { Jornada = row, Eventos = eventos, Tarefas = tarefas });
        }
        catch (Exception ex) { logger.LogError(ex, "Erro detalhar jornada"); return ApiResponse<JornadaClienteDetalheDto>.Fail("Não foi possível carregar jornada.", 500); }
    }

    public async Task<ApiResponse<IEnumerable<JornadaEventoDto>>> ListarEventosAsync(Guid clienteId)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var rows = await ListarEventosJornadaAsync(cn, clienteId);
            return ApiResponse<IEnumerable<JornadaEventoDto>>.Ok(rows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao listar eventos da jornada {ClienteId}", clienteId);
            return ApiResponse<IEnumerable<JornadaEventoDto>>.Fail("Não foi possível listar eventos da jornada.", 500);
        }
    }

    public async Task<ApiResponse<IEnumerable<JornadaTarefaDto>>> ListarTarefasAsync(Guid clienteId)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var rows = await ListarTarefasJornadaAsync(cn, clienteId);
            return ApiResponse<IEnumerable<JornadaTarefaDto>>.Ok(rows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao listar tarefas da jornada {ClienteId}", clienteId);
            return ApiResponse<IEnumerable<JornadaTarefaDto>>.Fail("Não foi possível listar tarefas da jornada.", 500);
        }
    }

    private static async Task<IEnumerable<JornadaEventoDto>> ListarEventosJornadaAsync(NpgsqlConnection cn, Guid clienteId)
    {
        return await cn.QueryAsync<JornadaEventoDto>(@"select id as ""Id"", cliente_id as ""ClienteId"", coalesce(tipo,'') as ""Tipo"", coalesce(resumo,'') as ""Resumo"", reg_date as ""RegDate""
from plantaopro.jornada_cliente_eventos
where cliente_id=@clienteId and reg_status='A'
order by reg_date desc
limit 100", new { clienteId });
    }

    private static async Task<IEnumerable<JornadaTarefaDto>> ListarTarefasJornadaAsync(NpgsqlConnection cn, Guid clienteId)
    {
        return await cn.QueryAsync<JornadaTarefaDto>(@"select id as ""Id"", cliente_id as ""ClienteId"", coalesce(titulo,'') as ""Titulo"", coalesce(responsavel,'') as ""Responsavel"", coalesce(status,'PENDENTE') as ""Status"", vencimento as ""Vencimento""
from plantaopro.jornada_cliente_tarefas
where cliente_id=@clienteId and reg_status='A'
order by case when status='PENDENTE' then 0 else 1 end, vencimento nulls last, reg_date desc
limit 100", new { clienteId });
    }

    public async Task<ApiResponse<string>> MudarEtapaAsync(Guid clienteId, MudarEtapaJornadaRequest request, bool avancar, Guid? usuarioId, string? ip, string? perfil)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Motivo)) return ApiResponse<string>.Fail(avancar ? "Avançar etapa exige motivo/resumo." : "Retroceder etapa exige justificativa.", 400);
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();
            var atual = await cn.ExecuteScalarAsync<string>("select etapa from plantaopro.jornada_cliente where cliente_id=@clienteId and reg_status='A'", new { clienteId }, tx) ?? Etapas[0];
            var idx = Array.IndexOf(Etapas, atual);
            if (idx < 0) idx = 0;
            var novoIdx = avancar ? Math.Min(idx + 1, Etapas.Length - 1) : Math.Max(idx - 1, 0);
            var novaEtapa = Etapas[novoIdx];
            if (novaEtapa == "CANCELADO" && string.IsNullOrWhiteSpace(request.Motivo)) return ApiResponse<string>.Fail("Cliente cancelado exige motivo.", 400);
            await cn.ExecuteAsync(@"insert into plantaopro.jornada_cliente(id, cliente_id, etapa, responsavel, proxima_acao, reg_status, reg_date) values(gen_random_uuid(),@clienteId,@novaEtapa,'Customer Success','Definir próxima ação','A',now()) on conflict (cliente_id) do update set etapa=@novaEtapa, proxima_acao='Definir próxima ação', reg_update=now()", new { clienteId, novaEtapa }, tx);
            await cn.ExecuteAsync(@"insert into plantaopro.jornada_cliente_eventos(id, cliente_id, tipo, resumo, usuario_id, reg_status, reg_date) values(gen_random_uuid(),@clienteId,@tipo,@resumo,@usuarioId,'A',now())", new { clienteId, tipo = avancar ? "AVANCOU_ETAPA" : "RETROCEDEU_ETAPA", resumo = request.Motivo, usuarioId }, tx);
            if (novaEtapa == "IMPLANTACAO")
            {
                await CriarTarefaJornadaSeNaoExistirAsync(cn, tx, clienteId, "Abrir checklist de operação assistida", "Customer Success", "OPERACAO_ASSISTIDA", "2 days");
            }
            if (novaEtapa == "OPERACAO_ASSISTIDA")
            {
                await CriarTarefaJornadaSeNaoExistirAsync(cn, tx, clienteId, "Validar primeiros plantões em operação assistida", "Customer Success", "OPERACAO_ASSISTIDA", "3 days");
            }
            if (novaEtapa == "RISCO")
            {
                await CriarTarefaJornadaSeNaoExistirAsync(cn, tx, clienteId, "Ação de Customer Success para cliente em risco", "Customer Success", "CUSTOMER_SUCCESS", "2 days");
            }
            await RegistrarAlertaJornadaAsync(cn, tx, clienteId, novaEtapa, request.Motivo);
            await tx.CommitAsync();
            await audit.RegistrarAsync(usuarioId, clienteId, "jornada_cliente", clienteId, avancar ? "AVANCAR" : "RETROCEDER", new { De = atual, Para = novaEtapa, request.Motivo }, true, ip, perfil);
            return ApiResponse<string>.Ok(novaEtapa, "Etapa atualizada com sucesso.");
        }
        catch (Exception ex) { logger.LogError(ex, "Erro mudar etapa jornada"); return ApiResponse<string>.Fail("Não foi possível atualizar etapa da jornada.", 500); }
    }

    private static Task CriarTarefaJornadaSeNaoExistirAsync(NpgsqlConnection cn, NpgsqlTransaction tx, Guid clienteId, string titulo, string responsavel, string tipo, string prazo)
    {
        return cn.ExecuteAsync(@"insert into plantaopro.jornada_cliente_tarefas(id, cliente_id, titulo, responsavel, status, vencimento, tipo, reg_status, reg_date)
select gen_random_uuid(), @clienteId, @titulo, @responsavel, 'PENDENTE', now() + cast(@prazo as interval), @tipo, 'A', now()
where not exists (
    select 1 from plantaopro.jornada_cliente_tarefas
    where cliente_id=@clienteId and titulo=@titulo and status='PENDENTE' and reg_status='A'
)", new { clienteId, titulo, responsavel, tipo, prazo }, tx);
    }

    private static Task RegistrarAlertaJornadaAsync(NpgsqlConnection cn, NpgsqlTransaction tx, Guid clienteId, string etapa, string motivo)
    {
        var severidade = etapa == "RISCO" || etapa == "CANCELADO" ? "ALTA" : "MEDIA";
        var titulo = "Jornada do cliente atualizada";
        var mensagem = "Cliente movido para " + etapa + ": " + motivo;
        return cn.ExecuteAsync(@"insert into plantaopro.cliente_alertas(id, cliente_id, tipo, severidade, titulo, mensagem, resolvido, reg_status, reg_date)
values(gen_random_uuid(), @clienteId, @tipo, @severidade, @titulo, @mensagem, false, 'A', now())",
            new { clienteId, tipo = "JORNADA_" + etapa, severidade, titulo, mensagem }, tx);
    }

    public async Task<ApiResponse<Guid>> CriarEventoAsync(Guid clienteId, CriarJornadaEventoRequest request, Guid? usuarioId)
    {
        if (string.IsNullOrWhiteSpace(request.Resumo)) return ApiResponse<Guid>.Fail("Resumo do evento é obrigatório.", 400);
        var id = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.ExecuteAsync(@"insert into plantaopro.jornada_cliente_eventos(id, cliente_id, tipo, resumo, usuario_id, reg_status, reg_date) values(@id,@clienteId,@Tipo,@Resumo,@usuarioId,'A',now())", new { id, clienteId, request.Tipo, request.Resumo, usuarioId });
        return ApiResponse<Guid>.Ok(id, "Evento registrado.");
    }

    public async Task<ApiResponse<Guid>> CriarTarefaAsync(Guid clienteId, CriarJornadaTarefaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Titulo)) return ApiResponse<Guid>.Fail("Título da tarefa é obrigatório.", 400);
        var id = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.ExecuteAsync(@"insert into plantaopro.jornada_cliente_tarefas(id, cliente_id, titulo, responsavel, status, vencimento, reg_status, reg_date) values(@id,@clienteId,@Titulo,@Responsavel,'PENDENTE',@Vencimento,'A',now())", new { id, clienteId, request.Titulo, request.Responsavel, request.Vencimento });
        return ApiResponse<Guid>.Ok(id, "Tarefa criada.");
    }

    public async Task<ApiResponse<string>> ConcluirTarefaAsync(Guid id)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var rows = await cn.ExecuteAsync("update plantaopro.jornada_cliente_tarefas set status='CONCLUIDA', concluida_em=now(), reg_update=now() where id=@id and reg_status='A'", new { id });
        return rows == 0 ? ApiResponse<string>.Fail("Tarefa não encontrada.", 404) : ApiResponse<string>.Ok("ok", "Tarefa concluída.");
    }

    public async Task<ApiResponse<IEnumerable<FunilEtapaDto>>> FunilAsync()
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<FunilEtapaDto>(@"select coalesce(etapa,'LEAD_CADASTRADO') as ""Etapa"", count(1)::bigint as ""Total"" from plantaopro.jornada_cliente where reg_status='A' group by etapa order by etapa");
        return ApiResponse<IEnumerable<FunilEtapaDto>>.Ok(rows);
    }
}

public sealed class ComercialSaasService
{
    private readonly IConfiguration cfg;
    private readonly IAuditService audit;
    private readonly ILogger<ComercialSaasService> logger;
    public ComercialSaasService(IConfiguration cfg, IAuditService audit, ILogger<ComercialSaasService> logger) { this.cfg = cfg; this.audit = audit; this.logger = logger; }

    public PlanoSugeridoDto SugerirPlano(SugerirPlanoRequest input)
    {
        var pontos = input.MedicosDesejados + input.HospitaisDesejados * 5 + input.PlantoesMes / 10 + (input.PrecisaBi ? 20 : 0) + (input.PrecisaMobile ? 10 : 0) + (input.SuportePrioritario ? 15 : 0) + (input.OperacaoAssistida ? 15 : 0);
        var plano = pontos >= 80 ? "Enterprise" : pontos >= 35 ? "Profissional" : "Essencial";
        return new PlanoSugeridoDto { Plano = plano, Score = pontos, Justificativa = "Recomendação determinística baseada em médicos, hospitais, volume mensal de plantões, mobile, BI, suporte e operação assistida." };
    }

    public async Task<ApiResponse<IEnumerable<ComercialLeadDto>>> LeadsAsync()
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<ComercialLeadDto>(@"select id as ""Id"", coalesce(nome,'') as ""Nome"", coalesce(email,'') as ""Email"", coalesce(telefone,'') as ""Telefone"", coalesce(empresa,'') as ""Empresa"", coalesce(status,'') as ""Status"", reg_date as ""RegDate"" from plantaopro.comercial_leads where reg_status='A' order by reg_date desc limit 100");
        return ApiResponse<IEnumerable<ComercialLeadDto>>.Ok(rows);
    }

    public async Task<ApiResponse<Guid>> SalvarLeadAsync(Guid? id, ComercialLeadRequest request, Guid? usuarioId, string? ip, string? perfil)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Nome) || string.IsNullOrWhiteSpace(request.Email)) return ApiResponse<Guid>.Fail("Nome e e-mail são obrigatórios.", 400);
            var leadId = id ?? Guid.NewGuid();
            var sugestao = SugerirPlano(new SugerirPlanoRequest { MedicosDesejados = request.MedicosDesejados, HospitaisDesejados = request.HospitaisDesejados, PlantoesMes = request.PlantoesMes, PrecisaMobile = request.PrecisaMobile, PrecisaBi = request.PrecisaBi, SuportePrioritario = request.SuportePrioritario, OperacaoAssistida = request.OperacaoAssistida });
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync(@"insert into plantaopro.comercial_leads(id,nome,email,telefone,empresa,status,plano_recomendado,reg_status,reg_date) values(@leadId,@Nome,@Email,@Telefone,@Empresa,'NOVO',@Plano,'A',now()) on conflict (id) do update set nome=@Nome,email=@Email,telefone=@Telefone,empresa=@Empresa,plano_recomendado=@Plano,reg_update=now()", new { leadId, request.Nome, request.Email, request.Telefone, request.Empresa, Plano = sugestao.Plano });
            await audit.RegistrarAsync(usuarioId, null, "comercial_leads", leadId, id.HasValue ? "EDITAR" : "CRIAR", new { request.Nome, request.Empresa, sugestao.Plano }, true, ip, perfil);
            return ApiResponse<Guid>.Ok(leadId, id.HasValue ? "Lead atualizado." : "Lead criado.");
        }
        catch (Exception ex) { logger.LogError(ex, "Erro salvar lead"); return ApiResponse<Guid>.Fail("Não foi possível salvar lead.", 500); }
    }

    public async Task<ApiResponse<IEnumerable<ComercialOportunidadeDto>>> OportunidadesAsync()
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<ComercialOportunidadeDto>(@"select id as ""Id"", lead_id as ""LeadId"", coalesce(nome,'') as ""Nome"", coalesce(etapa,'') as ""Etapa"", valor_estimado as ""ValorEstimado"", coalesce(plano_recomendado,'') as ""PlanoRecomendado"", reg_date as ""RegDate"" from plantaopro.comercial_oportunidades where reg_status='A' order by reg_date desc limit 100");
        return ApiResponse<IEnumerable<ComercialOportunidadeDto>>.Ok(rows);
    }

    public async Task<ApiResponse<Guid>> SalvarOportunidadeAsync(Guid? id, ComercialOportunidadeRequest request, Guid? usuarioId, string? ip, string? perfil)
    {
        if (string.IsNullOrWhiteSpace(request.Nome)) return ApiResponse<Guid>.Fail("Nome da oportunidade é obrigatório.", 400);
        var oppId = id ?? Guid.NewGuid();
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();
        await cn.ExecuteAsync(@"insert into plantaopro.comercial_oportunidades(id,lead_id,nome,etapa,valor_estimado,plano_recomendado,reg_status,reg_date) values(@oppId,@LeadId,@Nome,'NEGOCIACAO',@ValorEstimado,@PlanoRecomendado,'A',now()) on conflict (id) do update set nome=@Nome,valor_estimado=@ValorEstimado,plano_recomendado=@PlanoRecomendado,reg_update=now()", new { oppId, request.LeadId, request.Nome, request.ValorEstimado, request.PlanoRecomendado }, tx);
        await cn.ExecuteAsync(@"insert into plantaopro.comercial_interacoes(id,oportunidade_id,lead_id,tipo,resumo,reg_status,reg_date) values(gen_random_uuid(),@oppId,@LeadId,@tipo,@resumo,'A',now())", new { oppId, request.LeadId, tipo = id.HasValue ? "OPORTUNIDADE_EDITADA" : "OPORTUNIDADE_CRIADA", resumo = request.Nome }, tx);
        await tx.CommitAsync();
        await audit.RegistrarAsync(usuarioId, null, "comercial_oportunidades", oppId, id.HasValue ? "EDITAR" : "CRIAR", new { request.Nome, request.ValorEstimado, request.PlanoRecomendado }, true, ip, perfil);
        return ApiResponse<Guid>.Ok(oppId, id.HasValue ? "Oportunidade atualizada." : "Oportunidade criada.");
    }

    public async Task<ApiResponse<string>> GanharOportunidadeAsync(Guid id, Guid? usuarioId, string? ip, string? perfil)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();
        var rows = await cn.ExecuteAsync("update plantaopro.comercial_oportunidades set etapa='GANHA', reg_update=now() where id=@id and reg_status='A'", new { id }, tx);
        await cn.ExecuteAsync(@"insert into plantaopro.comercial_interacoes(id,oportunidade_id,tipo,resumo,reg_status,reg_date) values(gen_random_uuid(),@id,'OPORTUNIDADE_GANHA','Oportunidade ganha; conversão exige cliente, plano e assinatura.','A',now())", new { id }, tx);
        await tx.CommitAsync();
        if (rows == 0) return ApiResponse<string>.Fail("Oportunidade não encontrada.", 404);
        await audit.RegistrarAsync(usuarioId, null, "comercial_oportunidades", id, "GANHAR", new { Status = "GANHA" }, true, ip, perfil);
        return ApiResponse<string>.Ok("ok", "Oportunidade marcada como ganha. Conversão exige criação de cliente, plano e assinatura no fluxo SaaS.");
    }

    public async Task<ApiResponse<string>> PerderOportunidadeAsync(Guid id, StatusComMotivoRequest request, Guid? usuarioId, string? ip, string? perfil)
    {
        if (string.IsNullOrWhiteSpace(request.Motivo)) return ApiResponse<string>.Fail("Oportunidade perdida exige motivo.", 400);
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();
        var rows = await cn.ExecuteAsync("update plantaopro.comercial_oportunidades set etapa='PERDIDA', motivo_perda=@Motivo, reg_update=now() where id=@id and reg_status='A'", new { id, request.Motivo }, tx);
        await cn.ExecuteAsync(@"insert into plantaopro.comercial_interacoes(id,oportunidade_id,tipo,resumo,reg_status,reg_date) values(gen_random_uuid(),@id,'OPORTUNIDADE_PERDIDA',@Motivo,'A',now())", new { id, request.Motivo }, tx);
        await tx.CommitAsync();
        if (rows == 0) return ApiResponse<string>.Fail("Oportunidade não encontrada.", 404);
        await audit.RegistrarAsync(usuarioId, null, "comercial_oportunidades", id, "PERDER", new { request.Motivo }, true, ip, perfil);
        return ApiResponse<string>.Ok("ok", "Oportunidade marcada como perdida.");
    }

    public async Task<ApiResponse<IEnumerable<ComercialPropostaDto>>> PropostasAsync()
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<ComercialPropostaDto>(@"select id as ""Id"", oportunidade_id as ""OportunidadeId"", coalesce(numero,'') as ""Numero"", valor_total as ""ValorTotal"", desconto_percentual as ""DescontoPercentual"", validade as ""Validade"", coalesce(status,'') as ""Status"" from plantaopro.comercial_propostas where reg_status='A' order by reg_date desc limit 100");
        return ApiResponse<IEnumerable<ComercialPropostaDto>>.Ok(rows);
    }

    public async Task<ApiResponse<Guid>> CriarPropostaAsync(ComercialPropostaRequest request, bool adminGlobal, Guid? usuarioId, string? ip, string? perfil)
    {
        if (request.OportunidadeId == Guid.Empty || request.ValorTotal <= 0 || request.Validade.Date < DateTime.UtcNow.Date) return ApiResponse<Guid>.Fail("Proposta exige oportunidade, valor positivo e validade futura.", 400);
        if (request.DescontoPercentual > 15m && !adminGlobal)
        {
            await audit.RegistrarAsync(usuarioId, null, "comercial_propostas", request.OportunidadeId, "DESCONTO_BLOQUEADO", new { request.DescontoPercentual }, false, ip, perfil);
            return ApiResponse<Guid>.Fail("Desconto acima de 15% exige ADMINISTRADOR_GLOBAL.", 403);
        }
        var id = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();
        await cn.ExecuteAsync(@"insert into plantaopro.comercial_propostas(id,oportunidade_id,numero,valor_total,desconto_percentual,validade,status,reg_status,reg_date) values(@id,@OportunidadeId,@numero,@ValorTotal,@DescontoPercentual,@Validade,'RASCUNHO','A',now())", new { id, request.OportunidadeId, numero = "PROP-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"), request.ValorTotal, request.DescontoPercentual, request.Validade }, tx);
        await cn.ExecuteAsync(@"insert into plantaopro.comercial_interacoes(id,oportunidade_id,tipo,resumo,reg_status,reg_date) values(gen_random_uuid(),@OportunidadeId,'PROPOSTA_CRIADA',@resumo,'A',now())", new { request.OportunidadeId, resumo = "Proposta criada com valor " + request.ValorTotal.ToString("0.00") }, tx);
        await tx.CommitAsync();
        await audit.RegistrarAsync(usuarioId, null, "comercial_propostas", id, "CRIAR", new { request.ValorTotal, request.DescontoPercentual, request.Validade }, true, ip, perfil);
        return ApiResponse<Guid>.Ok(id, "Proposta criada.");
    }

    public async Task<ApiResponse<string>> AlterarStatusPropostaAsync(Guid id, string status, Guid? usuarioId, string? ip, string? perfil)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.OpenAsync();
        if (status == "APROVADA")
        {
            var vencida = await cn.ExecuteScalarAsync<bool>("select exists(select 1 from plantaopro.comercial_propostas where id=@id and validade < current_date)", new { id });
            if (vencida) return ApiResponse<string>.Fail("Proposta vencida não pode ser aprovada.", 400);
        }
        await using var tx = await cn.BeginTransactionAsync();
        var oportunidadeId = await cn.ExecuteScalarAsync<Guid?>("select oportunidade_id from plantaopro.comercial_propostas where id=@id and reg_status='A'", new { id }, tx);
        var rows = await cn.ExecuteAsync("update plantaopro.comercial_propostas set status=@status, reg_update=now() where id=@id and reg_status='A'", new { id, status }, tx);
        if (oportunidadeId.HasValue)
        {
            await cn.ExecuteAsync(@"insert into plantaopro.comercial_interacoes(id,oportunidade_id,tipo,resumo,reg_status,reg_date) values(gen_random_uuid(),@oportunidadeId,@tipo,@resumo,'A',now())", new { oportunidadeId, tipo = "PROPOSTA_" + status, resumo = "Proposta alterada para " + status }, tx);
        }
        await tx.CommitAsync();
        if (rows == 0) return ApiResponse<string>.Fail("Proposta não encontrada.", 404);
        await audit.RegistrarAsync(usuarioId, null, "comercial_propostas", id, status, new { Status = status }, true, ip, perfil);
        return ApiResponse<string>.Ok("ok", "Status da proposta atualizado.");
    }

    public async Task<ApiResponse<IEnumerable<ComercialFunilEtapaDto>>> FunilAsync()
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<ComercialFunilEtapaDto>(@"select coalesce(etapa,'SEM_ETAPA') as ""Etapa"", count(1)::bigint as ""Total"", coalesce(sum(valor_estimado),0) as ""ValorEstimado"" from plantaopro.comercial_oportunidades where reg_status='A' group by coalesce(etapa,'SEM_ETAPA') order by ""Total"" desc limit 50");
        return ApiResponse<IEnumerable<ComercialFunilEtapaDto>>.Ok(rows);
    }

    public async Task<ApiResponse<ComercialPrevisaoReceitaDto>> PrevisaoReceitaAsync()
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var row = await cn.QuerySingleAsync<ComercialPrevisaoReceitaDto>(@"select
    coalesce(sum(valor_total) filter (where status in ('RASCUNHO','ENVIADA')),0) as ""ReceitaAberta"",
    coalesce(sum(valor_total) filter (where status='ENVIADA'),0) as ""ReceitaEnviada"",
    coalesce(sum(valor_total) filter (where status='APROVADA'),0) as ""ReceitaAprovada"",
    count(1) filter (where validade < current_date and status not in ('APROVADA','RECUSADA'))::bigint as ""PropostasVencidas""
from plantaopro.comercial_propostas where reg_status='A'");
        return ApiResponse<ComercialPrevisaoReceitaDto>.Ok(row);
    }
}

public sealed class AjudaInterativaService
{
    private readonly IConfiguration cfg;
    public AjudaInterativaService(IConfiguration cfg) { this.cfg = cfg; }

    public async Task<ApiResponse<IEnumerable<AjudaTopicoDto>>> TopicosAsync(string? perfil)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<AjudaTopicoDto>(@"select id as ""Id"", coalesce(perfil,'') as ""Perfil"", coalesce(titulo,'') as ""Titulo"", coalesce(descricao,'') as ""Descricao"" from plantaopro.ajuda_topicos where reg_status='A' and (@perfil is null or perfil=@perfil or perfil='TODOS') order by perfil,titulo limit 100", new { perfil });
        return ApiResponse<IEnumerable<AjudaTopicoDto>>.Ok(rows);
    }

    public async Task<ApiResponse<IEnumerable<AjudaArtigoDto>>> ArtigosAsync(string? perfil)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<AjudaArtigoDto>(@"select id as ""Id"", topico_id as ""TopicoId"", coalesce(perfil,'') as ""Perfil"", coalesce(titulo,'') as ""Titulo"", coalesce(conteudo,'') as ""Conteudo"", coalesce(link_acao,'') as ""LinkAcao"" from plantaopro.ajuda_artigos where reg_status='A' and (@perfil is null or perfil=@perfil or perfil='TODOS') order by titulo limit 100", new { perfil });
        return ApiResponse<IEnumerable<AjudaArtigoDto>>.Ok(rows);
    }

    public async Task<ApiResponse<AjudaArtigoDto>> ArtigoAsync(Guid id)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var row = await cn.QueryFirstOrDefaultAsync<AjudaArtigoDto>(@"select id as ""Id"", topico_id as ""TopicoId"", coalesce(perfil,'') as ""Perfil"", coalesce(titulo,'') as ""Titulo"", coalesce(conteudo,'') as ""Conteudo"", coalesce(link_acao,'') as ""LinkAcao"" from plantaopro.ajuda_artigos where id=@id and reg_status='A'", new { id });
        return row is null ? ApiResponse<AjudaArtigoDto>.Fail("Artigo não encontrado.", 404) : ApiResponse<AjudaArtigoDto>.Ok(row);
    }

    public async Task<ApiResponse<IEnumerable<AjudaArtigoDto>>> BuscarAsync(string? termo)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<AjudaArtigoDto>(@"select id as ""Id"", topico_id as ""TopicoId"", coalesce(perfil,'') as ""Perfil"", coalesce(titulo,'') as ""Titulo"", coalesce(conteudo,'') as ""Conteudo"", coalesce(link_acao,'') as ""LinkAcao"" from plantaopro.ajuda_artigos where reg_status='A' and (@termo is null or titulo ilike '%' || @termo || '%' or conteudo ilike '%' || @termo || '%') order by titulo limit 50", new { termo });
        return ApiResponse<IEnumerable<AjudaArtigoDto>>.Ok(rows);
    }

    public async Task<ApiResponse<string>> FeedbackAsync(Guid artigoId, AjudaFeedbackRequest request, Guid? usuarioId)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.ExecuteAsync(@"insert into plantaopro.ajuda_feedbacks(id, artigo_id, usuario_id, util, comentario, reg_status, reg_date) values(gen_random_uuid(),@artigoId,@usuarioId,@Util,@Comentario,'A',now())", new { artigoId, usuarioId, request.Util, request.Comentario });
        return ApiResponse<string>.Ok("ok", "Obrigado pelo feedback.");
    }

    public async Task<ApiResponse<IEnumerable<AjudaArtigoDto>>> ChecklistPerfilAsync(string perfil)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<AjudaArtigoDto>(@"select a.id as ""Id"", a.topico_id as ""TopicoId"", coalesce(a.perfil,'') as ""Perfil"", coalesce(a.titulo,'') as ""Titulo"", coalesce(a.conteudo,'') as ""Conteudo"", coalesce(a.link_acao,'') as ""LinkAcao"" from plantaopro.ajuda_artigos a where a.reg_status='A' and (a.perfil=@perfil or a.perfil='TODOS') order by a.ordem,a.titulo limit 30", new { perfil });
        return ApiResponse<IEnumerable<AjudaArtigoDto>>.Ok(rows);
    }
}
