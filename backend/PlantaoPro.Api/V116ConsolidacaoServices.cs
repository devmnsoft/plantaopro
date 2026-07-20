using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using System.Text.Json;

namespace PlantaoPro.Api;

public record V116OperacaoRequest(string? Nome, Guid? EntidadeId, decimal? Valor, string? Justificativa, string? Observacao);
public record V116ResumoDto(Guid Id, Guid TenantId, Guid ClienteId, string Tipo, string Status, string Descricao, decimal? Valor, DateTime CriadoEm, DateTime? AtualizadoEm, bool Auditado, string Fonte);

public abstract class V116BaseService
{
    private readonly IConfiguration configuration;
    private readonly ICurrentUserService currentUser;
    private readonly IAuditService audit;
    private readonly ILogger logger;
    private static readonly HashSet<string> TabelasPermitidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "v116_convenio_autorizacoes", "v116_convenio_guias", "v116_faturamento_lotes", "v116_faturamento_lote_itens",
        "v116_caixas", "v116_caixa_movimentos", "v116_recebimentos_parciais", "v116_estornos", "v116_timelines",
        "v116_notificacoes_operacionais", "v116_relatorios_execucoes", "v116_auditoria_consultas", "v116_integracao_provedores"
    };

    protected V116BaseService(IConfiguration configuration, ICurrentUserService currentUser, IAuditService audit, ILogger logger)
    {
        this.configuration = configuration;
        this.currentUser = currentUser;
        this.audit = audit;
        this.logger = logger;
    }

    protected async Task<ApiResponse<IEnumerable<object>>> ListarAsync(string tabela, string tipo, string? entidade = null, Guid? entidadeId = null)
    {
        try
        {
            var ctx = ResolverContexto();
            if (!ctx.Success) return ApiResponse<IEnumerable<object>>.Fail(ctx.Message, ctx.StatusCode);
            await using var cn = CriarConexao();
            var filtroPayload = entidadeId.HasValue ? " and payload_demo @> cast(@FiltroEntidade as jsonb)" : string.Empty;
            var sql = @"select id, tenant_id as TenantId, cliente_id as ClienteId, @Tipo as Tipo, coalesce(status_operacional, reg_status) as Status,
    coalesce(descricao, '') as Descricao, valor as Valor, created_at as CriadoEm, updated_at as AtualizadoEm,
    true as Auditado, 'PostgreSQL/Dapper' as Fonte
from plantaopro." + TabelaSegura(tabela) + @"
where tenant_id = @TenantId and cliente_id = @ClienteId and coalesce(reg_status, 'ATIVO') <> 'EXCLUIDO'" + filtroPayload + @"
order by created_at desc
limit 100";
            var rows = await cn.QueryAsync<V116ResumoDto>(sql, new { ctx.TenantId, ctx.ClienteId, Tipo = tipo, FiltroEntidade = JsonSerializer.Serialize(new { entidadeId }) });
            return ApiResponse<IEnumerable<object>>.Ok(rows.Cast<object>().ToList(), tipo + " listados com persistência, tenant e LGPD.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao listar v1.16 {Tipo}", tipo);
            return ApiResponse<IEnumerable<object>>.Fail("Não foi possível consultar dados v1.16 persistidos.", 500);
        }
    }

    protected async Task<ApiResponse<object>> CriarAsync(string tabela, string tipo, string status, V116OperacaoRequest? request = null, Guid? id = null)
    {
        try
        {
            var ctx = ResolverContexto();
            if (!ctx.Success) return ApiResponse<object>.Fail(ctx.Message, ctx.StatusCode);
            var novoId = id ?? Guid.NewGuid();
            var descricao = Texto(request?.Nome) ?? Texto(request?.Observacao) ?? tipo;
            var payload = JsonSerializer.Serialize(new { request?.EntidadeId, request?.Justificativa, request?.Observacao, origem = "v1.17-runtime" });
            await using var cn = CriarConexao();
            var row = await cn.QuerySingleAsync<V116ResumoDto>(@"insert into plantaopro." + TabelaSegura(tabela) + @"
(id, cliente_id, tenant_id, descricao, status_operacional, valor, data_referencia, payload_demo, created_by, created_at, reg_status)
values (@Id, @ClienteId, @TenantId, @Descricao, @Status, @Valor, now(), cast(@Payload as jsonb), @UserId, now(), 'ATIVO')
returning id, tenant_id as TenantId, cliente_id as ClienteId, @Tipo as Tipo, coalesce(status_operacional, reg_status) as Status,
    coalesce(descricao, '') as Descricao, valor as Valor, created_at as CriadoEm, updated_at as AtualizadoEm,
    true as Auditado, 'PostgreSQL/Dapper' as Fonte", new { Id = novoId, ctx.ClienteId, ctx.TenantId, Descricao = descricao, Status = status, request?.Valor, Payload = payload, UserId = ctx.UserId?.ToString(), Tipo = tipo });
            await RegistrarEventoAsync(ctx, tabela, novoId, tipo + "_" + status, true, request);
            return ApiResponse<object>.Ok(row, tipo + " persistido.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao persistir v1.16 {Tipo}", tipo);
            return ApiResponse<object>.Fail("Não foi possível persistir a operação v1.16.", 500);
        }
    }

    protected async Task<ApiResponse<object>> AtualizarStatusAsync(string tabela, string tipo, string status, Guid id, V116OperacaoRequest? request = null)
    {
        try
        {
            var ctx = ResolverContexto();
            if (!ctx.Success) return ApiResponse<object>.Fail(ctx.Message, ctx.StatusCode);
            var payload = JsonSerializer.Serialize(new { request?.EntidadeId, request?.Justificativa, request?.Observacao, status, origem = "v1.17-runtime" });
            await using var cn = CriarConexao();
            var row = await cn.QuerySingleOrDefaultAsync<V116ResumoDto>(@"update plantaopro." + TabelaSegura(tabela) + @"
set status_operacional=@Status, valor=coalesce(@Valor, valor), descricao=coalesce(@Descricao, descricao), payload_demo=cast(@Payload as jsonb), updated_at=now(), updated_by=@UserId
where id=@Id and tenant_id=@TenantId and cliente_id=@ClienteId and coalesce(reg_status, 'ATIVO') <> 'EXCLUIDO'
returning id, tenant_id as TenantId, cliente_id as ClienteId, @Tipo as Tipo, coalesce(status_operacional, reg_status) as Status,
    coalesce(descricao, '') as Descricao, valor as Valor, created_at as CriadoEm, updated_at as AtualizadoEm,
    true as Auditado, 'PostgreSQL/Dapper' as Fonte", new { Id = id, ctx.TenantId, ctx.ClienteId, Status = status, request?.Valor, Descricao = Texto(request?.Nome) ?? Texto(request?.Observacao), Payload = payload, UserId = ctx.UserId?.ToString(), Tipo = tipo });
            if (row is null) return ApiResponse<object>.Fail(tipo + " não encontrado para o tenant atual.", 404);
            await RegistrarEventoAsync(ctx, tabela, id, tipo + "_" + status, true, request);
            return ApiResponse<object>.Ok(row, tipo + " atualizado.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao atualizar v1.16 {Tipo} {Id}", tipo, id);
            return ApiResponse<object>.Fail("Não foi possível atualizar a operação v1.16.", 500);
        }
    }

    private NpgsqlConnection CriarConexao() => new NpgsqlConnection(configuration.GetConnectionString("Default"));
    private async Task RegistrarEventoAsync(V116Contexto ctx, string entidade, Guid id, string acao, bool sucesso, object? detalhes) => await audit.RegistrarAsync(ctx.UserId, ctx.ClienteId, entidade, id, acao, detalhes, sucesso, null, string.Join(',', currentUser.Roles));
    private V116Contexto ResolverContexto()
    {
        if (!currentUser.IsAuthenticated()) return V116Contexto.Fail("Sessão inválida ou expirada.", 401);
        var tenantId = currentUser.TenantId;
        var clienteId = currentUser.ClienteId ?? tenantId;
        if (!tenantId.HasValue || !clienteId.HasValue) return V116Contexto.Fail("Contexto de tenant/cliente obrigatório para v1.16.", 403);
        return V116Contexto.Ok(currentUser.UserId, tenantId.Value, clienteId.Value);
    }
    private static string TabelaSegura(string tabela) { if (!TabelasPermitidas.Contains(tabela)) throw new InvalidOperationException("Tabela v1.16 não permitida."); return tabela; }
    private static string? Texto(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private sealed class V116Contexto
    {
        public bool Success { get; private set; } public string Message { get; private set; } = string.Empty; public int StatusCode { get; private set; } public Guid? UserId { get; private set; } public Guid TenantId { get; private set; } public Guid ClienteId { get; private set; }
        public static V116Contexto Ok(Guid? userId, Guid tenantId, Guid clienteId) => new V116Contexto { Success = true, StatusCode = 200, UserId = userId, TenantId = tenantId, ClienteId = clienteId };
        public static V116Contexto Fail(string message, int statusCode) => new V116Contexto { Success = false, Message = message, StatusCode = statusCode };
    }
}

public sealed class V116ConvenioService : V116BaseService
{
    public V116ConvenioService(IConfiguration c, ICurrentUserService u, IAuditService a, ILogger<V116ConvenioService> l) : base(c, u, a, l) { }
    public Task<ApiResponse<IEnumerable<object>>> ListarAutorizacoesAsync() => ListarAsync("v116_convenio_autorizacoes", "AUTORIZACAO_CONVENIO");
    public Task<ApiResponse<object>> CriarAutorizacaoAsync(V116OperacaoRequest r) => CriarAsync("v116_convenio_autorizacoes", "AUTORIZACAO_CONVENIO", "PENDENTE", r);
    public Task<ApiResponse<object>> AprovarAsync(Guid id, V116OperacaoRequest? r) => AtualizarStatusAsync("v116_convenio_autorizacoes", "AUTORIZACAO_CONVENIO", "APROVADA", id, r);
    public Task<ApiResponse<object>> NegarAsync(Guid id, V116OperacaoRequest? r) => AtualizarStatusAsync("v116_convenio_autorizacoes", "AUTORIZACAO_CONVENIO", "NEGADA", id, r);
    public Task<ApiResponse<object>> CancelarAsync(Guid id, V116OperacaoRequest? r) => AtualizarStatusAsync("v116_convenio_autorizacoes", "AUTORIZACAO_CONVENIO", "CANCELADA", id, r);
    public Task<ApiResponse<IEnumerable<object>>> ListarGuiasAsync() => ListarAsync("v116_convenio_guias", "GUIA_CONVENIO");
    public Task<ApiResponse<object>> GerarGuiaAsync(V116OperacaoRequest r) => CriarAsync("v116_convenio_guias", "GUIA_CONVENIO", "GERADA", r);
    public Task<ApiResponse<object>> VincularContaAsync(Guid id, V116OperacaoRequest r) => AtualizarStatusAsync("v116_convenio_guias", "GUIA_CONVENIO", "VINCULADA_CONTA", id, r);
}
public sealed class V116LoteFaturamentoService : V116BaseService
{
    public V116LoteFaturamentoService(IConfiguration c, ICurrentUserService u, IAuditService a, ILogger<V116LoteFaturamentoService> l) : base(c, u, a, l) { }
    public Task<ApiResponse<IEnumerable<object>>> ListarAsync() => ListarAsync("v116_faturamento_lotes", "LOTE_FATURAMENTO"); public Task<ApiResponse<object>> CriarAsync(V116OperacaoRequest r) => CriarAsync("v116_faturamento_lotes", "LOTE_FATURAMENTO", "ABERTO", r); public Task<ApiResponse<object>> ObterAsync(Guid id) => AtualizarStatusAsync("v116_faturamento_lotes", "LOTE_FATURAMENTO", "DETALHE", id); public Task<ApiResponse<object>> AdicionarItemAsync(Guid id, V116OperacaoRequest r) => CriarAsync("v116_faturamento_lote_itens", "LOTE_ITEM", "ADICIONADO", r); public Task<ApiResponse<object>> RemoverItemAsync(Guid id, Guid itemId) => AtualizarStatusAsync("v116_faturamento_lote_itens", "LOTE_ITEM", "REMOVIDO", itemId); public Task<ApiResponse<object>> FecharAsync(Guid id, V116OperacaoRequest? r) => AtualizarStatusAsync("v116_faturamento_lotes", "LOTE_FATURAMENTO", "FECHADO", id, r); public Task<ApiResponse<object>> ReabrirAsync(Guid id, V116OperacaoRequest? r) => AtualizarStatusAsync("v116_faturamento_lotes", "LOTE_FATURAMENTO", "ABERTO", id, r); public Task<ApiResponse<object>> EnviadoDemoAsync(Guid id) => AtualizarStatusAsync("v116_faturamento_lotes", "LOTE_FATURAMENTO", "ENVIADO_DEMO", id); public Task<ApiResponse<object>> RetornoDemoAsync(Guid id, V116OperacaoRequest? r) => AtualizarStatusAsync("v116_faturamento_lotes", "LOTE_FATURAMENTO", "RETORNO_DEMO", id, r);
}
public sealed class V116CaixaService : V116BaseService
{
    public V116CaixaService(IConfiguration c, ICurrentUserService u, IAuditService a, ILogger<V116CaixaService> l) : base(c, u, a, l) { }
    public Task<ApiResponse<object>> StatusAsync() => CriarAsync("v116_caixas", "CAIXA", "ABERTO"); public Task<ApiResponse<object>> AbrirAsync(V116OperacaoRequest r) => CriarAsync("v116_caixas", "CAIXA", "ABERTO", r); public async Task<ApiResponse<object>> FecharAsync(V116OperacaoRequest r) { if (r.Valor != 0 && string.IsNullOrWhiteSpace(r.Justificativa)) return ApiResponse<object>.Fail("Divergência exige justificativa.", 400); return await CriarAsync("v116_caixas", "CAIXA", "FECHADO", r); } public Task<ApiResponse<IEnumerable<object>>> MovimentosAsync() => ListarAsync("v116_caixa_movimentos", "CAIXA_MOVIMENTO"); public Task<ApiResponse<object>> ReceberAsync(Guid contaId, V116OperacaoRequest r) => CriarAsync("v116_recebimentos_parciais", "RECEBIMENTO", r.Valor.GetValueOrDefault() > 0 ? "PARCIAL_OU_TOTAL" : "PENDENTE", r, contaId); public async Task<ApiResponse<object>> EstornarAsync(Guid id, V116OperacaoRequest r) { if (string.IsNullOrWhiteSpace(r.Justificativa)) return ApiResponse<object>.Fail("Estorno exige justificativa.", 400); return await CriarAsync("v116_estornos", "ESTORNO", "REGISTRADO", r, id); } public Task<ApiResponse<object>> ConciliarAsync(Guid id, V116OperacaoRequest r) => AtualizarStatusAsync("v116_caixa_movimentos", "CONCILIACAO", "CONCILIADO", id, r);
}
public sealed class V116TimelineService : V116BaseService { public V116TimelineService(IConfiguration c, ICurrentUserService u, IAuditService a, ILogger<V116TimelineService> l) : base(c, u, a, l) { } public Task<ApiResponse<IEnumerable<object>>> ListarAsync(string e, Guid id) => ListarAsync("v116_timelines", "TIMELINE_" + e.ToUpperInvariant(), e, id); public Task<ApiResponse<object>> ComentarAsync(string e, Guid id, V116OperacaoRequest r) => CriarAsync("v116_timelines", "TIMELINE_COMENTARIO_" + e.ToUpperInvariant(), "REGISTRADO", r, id); public Task<ApiResponse<IEnumerable<object>>> HistoricoAsync(string e, Guid id) => ListarAsync("v116_auditoria_consultas", "HISTORICO_" + e.ToUpperInvariant(), e, id); }
public sealed class V116NotificacaoOperacionalService : V116BaseService { public V116NotificacaoOperacionalService(IConfiguration c, ICurrentUserService u, IAuditService a, ILogger<V116NotificacaoOperacionalService> l) : base(c, u, a, l) { } public Task<ApiResponse<IEnumerable<object>>> ListarAsync() => ListarAsync("v116_notificacoes_operacionais", "NOTIFICACAO_OPERACIONAL"); public Task<ApiResponse<object>> LidaAsync(Guid id) => AtualizarStatusAsync("v116_notificacoes_operacionais", "NOTIFICACAO_OPERACIONAL", "LIDA", id); public Task<ApiResponse<object>> TodasLidasAsync() => CriarAsync("v116_notificacoes_operacionais", "NOTIFICACAO_OPERACIONAL", "LIDAS"); public Task<ApiResponse<object>> ReprocessarAsync() => CriarAsync("v116_integracao_provedores", "OUTBOX_OPERACIONAL", "REPROCESSADO"); }
public sealed class V116RelatorioExecutivoService : V116BaseService { public V116RelatorioExecutivoService(IConfiguration c, ICurrentUserService u, IAuditService a, ILogger<V116RelatorioExecutivoService> l) : base(c, u, a, l) { } public Task<ApiResponse<IEnumerable<object>>> GerarAsync(string nome) => ListarAsync("v116_relatorios_execucoes", "RELATORIO_" + nome.ToUpperInvariant()); }
