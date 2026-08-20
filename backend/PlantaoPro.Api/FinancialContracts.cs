namespace PlantaoPro.Api;

public sealed class FinancialTenantContext
{
    private readonly ICurrentUserService currentUser; public FinancialTenantContext(ICurrentUserService currentUser)=>this.currentUser=currentUser;
    public Guid? TenantId => currentUser.ClienteId ?? currentUser.TenantId;
    public Guid? UserId => currentUser.UserId;
    public IReadOnlyCollection<string> Roles => currentUser.Roles;
    public bool HasTenant => TenantId.HasValue;
    public ApiResponse<T> MissingTenant<T>() => ApiResponse<T>.Fail("Contexto de tenant financeiro obrigatorio.", 403);
}

public sealed record RegraFaturamentoDto(Guid Id, string Codigo, string Nome, string Tipo, decimal ValorBase, decimal DescontoPercentual, decimal AcrescimoPercentual, Guid? ItemFaturavelId, Guid? ConvenioId, string Status);
public sealed record ContaReceberDto(Guid Id, Guid ReferenciaId, string OrigemTipo, decimal ValorOriginal, decimal Descontos, decimal Acrescimos, decimal GlosaReconhecida, decimal Recebido, decimal Estornado, decimal Saldo, string Status, DateTime EmitidaEm);
public sealed record RecebimentoDto(Guid Id, Guid ContaId, decimal Valor, string Forma, DateTime RecebidoEm, string Status);
public sealed record RecebimentoResumoDto(Guid ContaId, decimal Recebido, decimal Estornado, decimal Saldo, string Status);
public sealed record GlosaDto(Guid Id, Guid ContaId, Guid? TituloId, Guid ConvenioId, string Motivo, decimal ValorGlosado, decimal ValorRecuperado, DateTime? PrazoRecurso, string Status);
public sealed record GlosaResumoDto(Guid Id, Guid ContaId, Guid ConvenioId, decimal ValorGlosado, DateTime? PrazoRecurso, string Status);
public sealed record GlosaHistoricoDto(Guid Id, Guid GlosaId, string Evento, string? Resultado, decimal? ValorAnterior, decimal? ValorNovo, DateTime CriadoEm);
public sealed record RepasseRegraDto(Guid Id, string Tipo, decimal Percentual, decimal ValorFixo, string EventoGerador, Guid? ConvenioId);
public sealed record RepasseMedicoDto(Guid Id, Guid ReferenciaId, Guid MedicoId, Guid RegraId, decimal ValorBase, decimal ValorRepasse, string EventoGerador, string Status);
public sealed record CaixaDto(Guid Id, Guid? UnidadeId, Guid OperadorId, decimal ValorAbertura, decimal SaldoEsperado, string Status, DateTime AbertoEm);
public sealed record MovimentoCaixaDto(Guid Id, Guid CaixaId, string Tipo, string Direcao, decimal Valor, string? Motivo, DateTime CriadoEm);
public sealed record FinanceiroDashboardDto(decimal AReceber, decimal Recebido, decimal Vencido, decimal Glosado, decimal EmRecurso, decimal Recuperado, decimal RepassesPendentes, decimal PagamentosPlantaoPendentes, decimal SaldoCaixa);
public sealed record OperacaoFinanceiraDto(Guid Id, string Status, decimal? Valor = null, decimal? Saldo = null);
public sealed record AlertaFinanceiroDto(Guid Id, string Tipo, string Prioridade, Guid EntidadeId, string Cta, string Rota, DateTime? Prazo, string Status);

internal sealed record RegraFaturamentoRow(Guid Id, string Codigo, string Nome, string Tipo, decimal ValorBase, decimal DescontoPercentual, decimal AcrescimoPercentual, Guid? ItemFaturavelId, Guid? ConvenioId, string Status);
internal sealed record ContaLockRow(Guid Id, decimal Valor, string Status);
internal sealed record RegraRepasseRow(Guid Id, string Tipo, decimal Percentual, decimal ValorFixo, string EventoGerador);
