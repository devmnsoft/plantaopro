using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public record V116OperacaoRequest(string? Nome, Guid? EntidadeId, decimal? Valor, string? Justificativa, string? Observacao);
public record V116ResumoDto(Guid Id, string Tipo, string Status, string Descricao, decimal Valor, DateTime CriadoEm);

public abstract class V116BaseService
{
    protected static readonly Guid DemoTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    protected static List<V116ResumoDto> Seed(string tipo)
    {
        var itens = new List<V116ResumoDto>();
        itens.Add(new V116ResumoDto(Guid.NewGuid(), tipo, "PENDENTE", tipo + " pendente demo", 120, DateTime.UtcNow.AddDays(-1)));
        itens.Add(new V116ResumoDto(Guid.NewGuid(), tipo, "APROVADO", tipo + " aprovado demo", 240, DateTime.UtcNow));
        return itens;
    }
    protected Task<ApiResponse<IEnumerable<object>>> ListarAsync(string tipo)
    {
        IEnumerable<object> dados = Seed(tipo).Select(x => new { x.Id, TenantId = DemoTenantId, x.Tipo, x.Status, x.Descricao, x.Valor, x.CriadoEm });
        return Task.FromResult(ApiResponse<IEnumerable<object>>.Ok(dados, tipo + " listados com tenant e LGPD."));
    }
    protected Task<ApiResponse<object>> AcaoAsync(string tipo, string status, Guid? id = null, V116OperacaoRequest? request = null)
    {
        object dto = new { Id = id ?? Guid.NewGuid(), TenantId = DemoTenantId, Tipo = tipo, Status = status, request?.Valor, request?.Justificativa, Auditado = true, Timeline = true, UpdatedAt = DateTime.UtcNow };
        return Task.FromResult(ApiResponse<object>.Ok(dto, tipo + " atualizado."));
    }
}

public sealed class V116ConvenioService : V116BaseService
{
    public Task<ApiResponse<IEnumerable<object>>> ListarAutorizacoesAsync() => ListarAsync("AUTORIZACAO_CONVENIO");
    public Task<ApiResponse<object>> CriarAutorizacaoAsync(V116OperacaoRequest r) => AcaoAsync("AUTORIZACAO_CONVENIO", "PENDENTE", null, r);
    public Task<ApiResponse<object>> AprovarAsync(Guid id, V116OperacaoRequest? r) => AcaoAsync("AUTORIZACAO_CONVENIO", "APROVADA", id, r);
    public Task<ApiResponse<object>> NegarAsync(Guid id, V116OperacaoRequest? r) => AcaoAsync("AUTORIZACAO_CONVENIO", "NEGADA", id, r);
    public Task<ApiResponse<object>> CancelarAsync(Guid id, V116OperacaoRequest? r) => AcaoAsync("AUTORIZACAO_CONVENIO", "CANCELADA", id, r);
    public Task<ApiResponse<IEnumerable<object>>> ListarGuiasAsync() => ListarAsync("GUIA_CONVENIO");
    public Task<ApiResponse<object>> GerarGuiaAsync(V116OperacaoRequest r) => AcaoAsync("GUIA_CONVENIO", "GERADA", null, r);
    public Task<ApiResponse<object>> VincularContaAsync(Guid id, V116OperacaoRequest r) => AcaoAsync("GUIA_CONVENIO", "VINCULADA_CONTA", id, r);
}
public sealed class V116LoteFaturamentoService : V116BaseService
{
    public Task<ApiResponse<IEnumerable<object>>> ListarAsync() => ListarAsync("LOTE_FATURAMENTO"); public Task<ApiResponse<object>> CriarAsync(V116OperacaoRequest r) => AcaoAsync("LOTE_FATURAMENTO", "ABERTO", null, r); public Task<ApiResponse<object>> ObterAsync(Guid id) => AcaoAsync("LOTE_FATURAMENTO", "DETALHE", id); public Task<ApiResponse<object>> AdicionarItemAsync(Guid id, V116OperacaoRequest r) => AcaoAsync("LOTE_ITEM", "ADICIONADO", id, r); public Task<ApiResponse<object>> RemoverItemAsync(Guid id, Guid itemId) => AcaoAsync("LOTE_ITEM", "REMOVIDO", itemId); public Task<ApiResponse<object>> FecharAsync(Guid id, V116OperacaoRequest? r) => AcaoAsync("LOTE_FATURAMENTO", "FECHADO", id, r); public Task<ApiResponse<object>> ReabrirAsync(Guid id, V116OperacaoRequest? r) => AcaoAsync("LOTE_FATURAMENTO", "ABERTO", id, r); public Task<ApiResponse<object>> EnviadoDemoAsync(Guid id) => AcaoAsync("LOTE_FATURAMENTO", "ENVIADO_DEMO", id); public Task<ApiResponse<object>> RetornoDemoAsync(Guid id, V116OperacaoRequest? r) => AcaoAsync("LOTE_FATURAMENTO", "RETORNO_DEMO", id, r);
}
public sealed class V116CaixaService : V116BaseService
{
    public Task<ApiResponse<object>> StatusAsync() => AcaoAsync("CAIXA", "ABERTO"); public Task<ApiResponse<object>> AbrirAsync(V116OperacaoRequest r) => AcaoAsync("CAIXA", "ABERTO", null, r); public Task<ApiResponse<object>> FecharAsync(V116OperacaoRequest r) { if (r.Valor != 0 && string.IsNullOrWhiteSpace(r.Justificativa)) return Task.FromResult(ApiResponse<object>.Fail("Divergência exige justificativa.", 400)); return AcaoAsync("CAIXA", "FECHADO", null, r); } public Task<ApiResponse<IEnumerable<object>>> MovimentosAsync() => ListarAsync("CAIXA_MOVIMENTO"); public Task<ApiResponse<object>> ReceberAsync(Guid contaId, V116OperacaoRequest r) => AcaoAsync("RECEBIMENTO", r.Valor.GetValueOrDefault() > 0 ? "PARCIAL_OU_TOTAL" : "PENDENTE", contaId, r); public Task<ApiResponse<object>> EstornarAsync(Guid id, V116OperacaoRequest r) { if (string.IsNullOrWhiteSpace(r.Justificativa)) return Task.FromResult(ApiResponse<object>.Fail("Estorno exige justificativa.", 400)); return AcaoAsync("ESTORNO", "REGISTRADO", id, r); } public Task<ApiResponse<object>> ConciliarAsync(Guid id, V116OperacaoRequest r) => AcaoAsync("CONCILIACAO", "CONCILIADO", id, r);
}
public sealed class V116TimelineService : V116BaseService { public Task<ApiResponse<IEnumerable<object>>> ListarAsync(string e, Guid id) => ListarAsync("TIMELINE_" + e.ToUpperInvariant()); public Task<ApiResponse<object>> ComentarAsync(string e, Guid id, V116OperacaoRequest r) => AcaoAsync("TIMELINE_COMENTARIO_" + e.ToUpperInvariant(), "REGISTRADO", id, r); public Task<ApiResponse<IEnumerable<object>>> HistoricoAsync(string e, Guid id) => ListarAsync("HISTORICO_" + e.ToUpperInvariant()); }
public sealed class V116NotificacaoOperacionalService : V116BaseService { public Task<ApiResponse<IEnumerable<object>>> ListarAsync() => ListarAsync("NOTIFICACAO_OPERACIONAL"); public Task<ApiResponse<object>> LidaAsync(Guid id) => AcaoAsync("NOTIFICACAO_OPERACIONAL", "LIDA", id); public Task<ApiResponse<object>> TodasLidasAsync() => AcaoAsync("NOTIFICACAO_OPERACIONAL", "LIDAS"); public Task<ApiResponse<object>> ReprocessarAsync() => AcaoAsync("OUTBOX_OPERACIONAL", "REPROCESSADO"); }
public sealed class V116RelatorioExecutivoService : V116BaseService { public Task<ApiResponse<IEnumerable<object>>> GerarAsync(string nome) => ListarAsync("RELATORIO_" + nome.ToUpperInvariant()); }
