using Dapper;
using Npgsql;
using PlantaoPro.Api.Controllers;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using System.Text.Json;

namespace PlantaoPro.Api;

public sealed class V114ProdutoService
{
    private readonly IConfiguration cfg;
    private readonly ICurrentUserService currentUser;
    private readonly IAuditService audit;
    private readonly ILogger<V114ProdutoService> logger;

    public V114ProdutoService(IConfiguration cfg, ICurrentUserService currentUser, IAuditService audit, ILogger<V114ProdutoService> logger)
    {
        this.cfg = cfg;
        this.currentUser = currentUser;
        this.audit = audit;
        this.logger = logger;
    }

    private NpgsqlConnection Cn() => new NpgsqlConnection(cfg.GetConnectionString("Default"));
    private Guid? TenantId => currentUser.ClienteId ?? currentUser.TenantId;
    private Guid? UserId => currentUser.UserId;
    private string Perfil => string.Join(',', currentUser.Roles);
    private bool Global => currentUser.IsGlobalAdmin();
    private object Scope => new { tenantId = TenantId, isGlobal = Global };

    public async Task<ApiResponse<object>> DashboardAsync() => await QueryOne<object>(@"
select
 (select count(1) from plantaopro.v113_clientes where reg_status='A' and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId)) as clientesAtivos,
 (select count(1) from plantaopro.v113_produtos where reg_status='A' and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId)) as itensFaturaveisAtivos,
 (select count(1) from plantaopro.v113_tarefas where reg_status='A' and status<>'DONE' and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId)) as pendenciasOperacionais,
 (select coalesce(sum(valor),0) from plantaopro.v113_faturas where reg_status='A' and status in ('ISSUED','OPEN') and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId)) as faturamentoAberto,
 (select count(1) from plantaopro.v113_titulos where reg_status='A' and status in ('OPEN','DEMO_BOLETO') and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId)) as titulosAbertos,
 (select count(1) from plantaopro.v113_outbox_eventos where reg_status='A' and status in ('PENDING','ERROR') and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId)) as eventosOperacionaisPendentes,
 @perfil as perfil", ScopeWith(new { perfil = Perfil }), "Dashboard v1.14 carregado.", "Falha ao carregar dashboard v1.14.");

    public async Task<ApiResponse<IEnumerable<object>>> OperacaoCentralAsync() => await QueryList(@"
select id, 'OPERACAO_INTELIGENTE_3_0' as origem, tipo as modulo, descricao as entidade, 'Revisar pendência' as cta, '/Operacao' as rotaOrigem,
 case when descricao ilike '%falha%' or descricao ilike '%venc%' then 'ALTA' else 'MEDIA' end as prioridade,
 case when tipo ilike '%FAT%' then 'FINANCEIRO' when tipo ilike '%OUTBOX%' then 'SUPORTE' else 'COORDENACAO' end as perfilResponsavel,
 created_at as prazo, 'ABERTA' as status
from plantaopro.v113_atividades where reg_status='A' order by created_at desc limit 50", "Central de Operação Inteligente 3.0 carregada.");

    public async Task<ApiResponse<IEnumerable<object>>> AtividadesAsync() => await QueryList("select id,tipo as modulo,descricao as resumo,created_at as dataHora from plantaopro.v113_atividades where reg_status='A' order by created_at desc limit 100", "Atividades recentes carregadas.");
    public async Task<ApiResponse<IEnumerable<object>>> TarefasAsync(string? status) => await QueryList("select id,pedido_id as atendimentoOuSolicitacaoId,titulo as titulo,status,responsavel as perfilResponsavel,comentarios as timeline,created_at as criadaEm from plantaopro.v113_tarefas where reg_status='A' and (@status is null or status=@status) and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId) order by created_at desc", "Tarefas por perfil carregadas.", ScopeWith(new { status = Normalize(status) }));
    public async Task<ApiResponse<IEnumerable<object>>> OutboxAsync() => await QueryList("select id,tipo as eventoOperacional,payload_ref as referencia,status,erro as erroAmigavel,created_at as criadoEm from plantaopro.v113_outbox_eventos where reg_status='A' and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId) order by created_at desc", "Eventos operacionais carregados.");

    public async Task<ApiResponse<IEnumerable<ItemFaturavelDto>>> ItensFaturaveisAsync(string? q, string? tipo, string? status) => await QueryList<ItemFaturavelDto>(@"select id,codigo as codigo,nome as nome,
case when codigo ilike 'PL%' then 'PLANTAO' when codigo ilike 'TX%' then 'TAXA' else 'PROCEDIMENTO' end as tipo,
preco as valorPadrao, null::text as especialidade, null::text as convenio, status as status, 'Item faturável consolidado a partir do catálogo v1.13.' as observacoes
from plantaopro.v113_produtos where reg_status <> 'D' and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId) and (@status is null or status=@status) and (@q is null or nome ilike @like or codigo ilike @like) and (@tipo is null or @tipo in ('CONSULTA','PROCEDIMENTO','PLANTAO','TAXA','REPASSE','CONVENIO','PACOTE')) order by nome", "Itens faturáveis carregados.", ScopeWith(new { q = Blank(q), like = Like(q), tipo = Normalize(tipo), status = Normalize(status) }));

    public async Task<ApiResponse<ItemFaturavelDto>> CriarItemFaturavelAsync(ItemFaturavelUpsertDto dto)
    {
        try
        {
            var id = Guid.NewGuid();
            await using var cn = Cn();
            await cn.ExecuteAsync("insert into plantaopro.v113_produtos(id,cliente_id,tenant_id,codigo,nome,preco,estoque_minimo,status,reg_status,created_at,created_by) values(@id,@tenantId,@tenantId,@codigo,@nome,@valor,0,'ACTIVE','A',now(),@userId)", new { id, tenantId = TenantId, userId = UserId, codigo = dto.Codigo, nome = dto.Nome, valor = dto.ValorPadrao });
            await AddOutboxAsync(cn, "ITEM_FATURAVEL_CRIADO", id, new { id, dto.Codigo, dto.Tipo });
            await AuditAsync("v114_itens_faturaveis", id, "CRIAR", true, new { dto.Codigo, dto.Tipo });
            return ApiResponse<ItemFaturavelDto>.Ok(new ItemFaturavelDto(id, dto.Codigo, dto.Nome, dto.Tipo, dto.ValorPadrao, dto.Especialidade, dto.Convenio, "ACTIVE", dto.Observacoes), "Item faturável criado.");
        }
        catch (Exception ex) { return Fail<ItemFaturavelDto>(ex, "Falha ao criar item faturável."); }
    }

    public async Task<ApiResponse<ItemFaturavelDto>> AtualizarItemFaturavelAsync(Guid id, ItemFaturavelUpsertDto dto)
    {
        try
        {
            await using var cn = Cn();
            var affected = await cn.ExecuteAsync("update plantaopro.v113_produtos set codigo=@codigo,nome=@nome,preco=@valor,status=@status,updated_at=now(),updated_by=@userId where id=@id and reg_status <> 'D' and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId)", new { id, tenantId = TenantId, isGlobal = Global, userId = UserId, codigo = dto.Codigo, nome = dto.Nome, valor = dto.ValorPadrao, status = Normalize(dto.Status) ?? "ACTIVE" });
            if (affected == 0) return ApiResponse<ItemFaturavelDto>.Fail("Item faturável não encontrado.", 404);
            await AuditAsync("v114_itens_faturaveis", id, "ATUALIZAR", true, new { dto.Codigo, dto.Tipo });
            return ApiResponse<ItemFaturavelDto>.Ok(new ItemFaturavelDto(id, dto.Codigo, dto.Nome, dto.Tipo, dto.ValorPadrao, dto.Especialidade, dto.Convenio, Normalize(dto.Status) ?? "ACTIVE", dto.Observacoes), "Item faturável atualizado.");
        }
        catch (Exception ex) { return Fail<ItemFaturavelDto>(ex, "Falha ao atualizar item faturável."); }
    }

    public async Task<ApiResponse<object>> InativarItemFaturavelAsync(Guid id) => await ExecuteCriticalAsync("update plantaopro.v113_produtos set status='INACTIVE',reg_status='D',updated_at=now(),updated_by=@userId where id=@id and reg_status <> 'D' and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId)", "v114_itens_faturaveis", id, "INATIVAR", "Item faturável inativado.");

    public async Task<ApiResponse<IEnumerable<object>>> ContasReceberAsync() => await QueryList("select id,pedido_id as atendimentoId,valor as valor,status,created_at as emitidaEm from plantaopro.v113_faturas where reg_status='A' and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId) order by created_at desc", "Contas a receber carregadas.");
    public async Task<ApiResponse<IEnumerable<object>>> TitulosAsync() => await QueryList("select id,fatura_id as contaReceberId,valor,status,demo_boleto as demoBoleto,vencimento as vencimento,'Boleto sempre demonstrativo; sem cobrança real.' as aviso from plantaopro.v113_titulos where reg_status='A' and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId) order by created_at desc", "Títulos carregados.");
    public async Task<ApiResponse<object>> GerarContaAtendimentoAsync(Guid atendimentoId) => await CriarContaAsync(atendimentoId, "CONTA_ATENDIMENTO_GERADA", "Conta a receber demonstrativa criada para atendimento finalizado.");
    public async Task<ApiResponse<object>> DemoBoletoAsync(Guid id) => await ExecuteCriticalAsync("update plantaopro.v113_titulos set demo_boleto=true,status='DEMO_BOLETO',updated_at=now(),updated_by=@userId where id=@id and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId)", "v114_titulos", id, "DEMO_BOLETO", "Boleto demonstrativo gerado. Nenhuma cobrança real foi emitida.");
    public async Task<ApiResponse<IEnumerable<object>>> RepassesMedicosAsync() => await QueryList("select id,pedido_id as plantaoOuAtendimentoId,valor * 0.7 as valorRepasse,'PENDENTE' as status,created_at as previstoEm from plantaopro.v113_faturas where reg_status='A' and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId) order by created_at desc", "Repasses médicos carregados.");
    public async Task<ApiResponse<IEnumerable<object>>> GlosasAsync() => await QueryList("select id,fatura_id as contaReceberId,valor * 0.1 as valorGlosado,'ABERTA' as status,'Glosa demonstrativa para homologação clínica.' as motivo,vencimento as prazoResposta from plantaopro.v113_titulos where reg_status='A' and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId) order by created_at desc", "Glosas carregadas.");
    public async Task<ApiResponse<object>> JornadaProgressoAsync() => await QueryOne<object>("select count(1) filter (where status='DONE') as concluidas,count(1) as total,round(100.0*count(1) filter (where status='DONE')/greatest(count(1),1),2) as percentual from plantaopro.v113_jornada_acoes where reg_status='A'", Scope, "Progresso da jornada carregado.", "Falha ao carregar jornada.");
    public async Task<ApiResponse<IEnumerable<object>>> ProximasAcoesAsync() => await QueryList("select id,codigo as codigo,nome as titulo,detalhe as descricao,status,open_url as rotaOrigem from plantaopro.v113_jornada_acoes where reg_status='A' and status<>'DONE' order by ordem limit 20", "Próximas ações carregadas.");
    public async Task<ApiResponse<object>> ConcluirAcaoAsync(Guid id) => await ExecuteCriticalAsync("update plantaopro.v113_jornada_acoes set status='DONE',updated_at=now(),updated_by=@userId where id=@id and reg_status='A'", "v114_jornada", id, "CONCLUIR_ACAO", "Ação de jornada concluída.");
    public async Task<ApiResponse<IEnumerable<object>>> TemplatesOperacionaisAsync() => await QueryList("select id,codigo,nome,descricao,'OPERACIONAL' as tipo from plantaopro.v113_templates where reg_status='A' order by nome", "Templates operacionais carregados.");
    public async Task<ApiResponse<object>> InstalarTemplateAsync(string id) => await CriarInstalacaoTemplateAsync(id);

    public ApiResponse<object> MobileMedicoResumo() => ApiResponse<object>.Ok(new { dashboard = "médico", proximosPlantoes = 0, convites = 0, pagamentos = 0, atalhos = new List<string> { "Próximos plantões", "Convites", "Pagamentos", "Perfil" } }, "Dashboard médico v1.14 disponível.");

    private async Task<ApiResponse<object>> CriarContaAsync(Guid atendimentoId, string evento, string mensagem)
    {
        try
        {
            var id = Guid.NewGuid();
            await using var cn = Cn();
            await cn.ExecuteAsync("insert into plantaopro.v113_faturas(id,cliente_id,tenant_id,pedido_id,valor,status,reg_status,created_at,created_by) values(@id,@tenantId,@tenantId,@atendimentoId,100,'ISSUED','A',now(),@userId)", new { id, tenantId = TenantId, userId = UserId, atendimentoId });
            await AddOutboxAsync(cn, evento, id, new { id, atendimentoId });
            await AuditAsync("v114_contas_receber", id, evento, true, new { atendimentoId });
            return ApiResponse<object>.Ok(new { id, atendimentoId, valor = 100m, status = "ISSUED" }, mensagem);
        }
        catch (Exception ex) { return Fail<object>(ex, "Falha ao gerar conta do atendimento."); }
    }

    private async Task<ApiResponse<object>> CriarInstalacaoTemplateAsync(string codigo)
    {
        try
        {
            await using var cn = Cn();
            var template = await cn.ExecuteScalarAsync<Guid?>("select id from plantaopro.v113_templates where codigo=@codigo and reg_status='A'", new { codigo });
            if (!template.HasValue) return ApiResponse<object>.Fail("Template operacional não encontrado.", 404);
            var id = Guid.NewGuid();
            await cn.ExecuteAsync("insert into plantaopro.v113_template_instalacoes(id,cliente_id,tenant_id,template_id,status,reg_status,created_at,created_by) values(@id,@tenantId,@tenantId,@template,'INSTALLED','A',now(),@userId)", new { id, tenantId = TenantId, userId = UserId, template });
            await AuditAsync("v114_templates_operacionais", id, "INSTALAR", true, new { codigo });
            return ApiResponse<object>.Ok(new { id, codigo, status = "INSTALLED" }, "Template operacional instalado.");
        }
        catch (Exception ex) { return Fail<object>(ex, "Falha ao instalar template operacional."); }
    }

    private async Task<ApiResponse<object>> ExecuteCriticalAsync(string sql, string entity, Guid id, string action, string message)
    {
        try
        {
            await using var cn = Cn();
            var affected = await cn.ExecuteAsync(sql, ScopeWith(new { id, userId = UserId }));
            if (affected == 0) return ApiResponse<object>.Fail("Registro não encontrado ou sem permissão para o perfil atual.", 404);
            await AddOutboxAsync(cn, action, id, new { id, action });
            await AuditAsync(entity, id, action, true, new { id });
            return ApiResponse<object>.Ok(new { id, status = action, aviso = message }, message);
        }
        catch (Exception ex) { return Fail<object>(ex, "Falha ao executar ação crítica v1.14."); }
    }

    private async Task<ApiResponse<IEnumerable<T>>> QueryList<T>(string sql, string ok, object? args = null)
    {
        try { await using var cn = Cn(); var rows = await cn.QueryAsync<T>(sql, args ?? Scope); return ApiResponse<IEnumerable<T>>.Ok(rows.ToList(), ok); }
        catch (Exception ex) { return Fail<IEnumerable<T>>(ex, ok); }
    }
    private async Task<ApiResponse<IEnumerable<object>>> QueryList(string sql, string ok, object? args = null) => await QueryList<object>(sql, ok, args ?? Scope);
    private async Task<ApiResponse<T>> QueryOne<T>(string sql, object args, string ok, string fail)
    {
        try { await using var cn = Cn(); var row = await cn.QueryFirstOrDefaultAsync<T>(sql, args); return row is null ? ApiResponse<T>.Fail("Registro não encontrado.", 404) : ApiResponse<T>.Ok(row, ok); }
        catch (Exception ex) { return Fail<T>(ex, fail); }
    }
    private async Task AddOutboxAsync(NpgsqlConnection cn, string type, Guid reference, object payload) => await cn.ExecuteAsync("insert into plantaopro.v113_outbox_eventos(id,cliente_id,tenant_id,tipo,payload_ref,payload,status,reg_status,created_at,created_by) values(gen_random_uuid(),@tenantId,@tenantId,@type,@reference,cast(@payload as jsonb),'PENDING','A',now(),@userId)", new { tenantId = TenantId, userId = UserId, type, reference = reference.ToString(), payload = JsonSerializer.Serialize(payload) });
    private async Task AuditAsync(string entity, Guid id, string action, bool success, object details) => await audit.RegistrarAsync(UserId, TenantId, entity, id, action, details, success, null, Perfil);
    private ApiResponse<T> Fail<T>(Exception ex, string message) { logger.LogError(ex, "V1.14 produto: {Message}", message); return ApiResponse<T>.Fail(message, 500); }
    private object ScopeWith(object values)
    {
        var dict = new Dictionary<string, object?> { { "tenantId", TenantId }, { "isGlobal", Global } };
        foreach (var p in values.GetType().GetProperties()) dict[p.Name] = p.GetValue(values);
        return dict;
    }
    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    private static string? Like(string? value) => string.IsNullOrWhiteSpace(value) ? null : "%" + value.Trim() + "%";
}

public sealed record ItemFaturavelDto(Guid Id, string Codigo, string Nome, string Tipo, decimal ValorPadrao, string? Especialidade, string? Convenio, string Status, string? Observacoes);
public sealed record ItemFaturavelUpsertDto(string Nome, string Codigo, string Tipo, decimal ValorPadrao, string? Especialidade, string? Convenio, string? Status, string? Observacoes);
