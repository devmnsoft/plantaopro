using PlantaoPro.Api.Productivity;

namespace PlantaoPro.Api;

// Compatibility facade: Meu Dia and Central de Ações share one real, derived source.
public interface IMeuDiaRepository
{
    Task<ProductivityPageDto> ReadAsync(ProductivityQuery query, CancellationToken ct);
}

public sealed class MeuDiaRepository : IMeuDiaRepository
{
    private readonly IProductivityActionService productivity; public MeuDiaRepository(IProductivityActionService productivity)=>this.productivity=productivity;
    public Task<ProductivityPageDto> ReadAsync(ProductivityQuery query, CancellationToken ct) => productivity.ListAsync(query, ct);
}

public interface IMeuDiaService
{
    Task<ProductivityPageDto> ObterResumoAsync(CancellationToken ct);
    Task<ProductivitySummaryDto> IndicadoresAsync(CancellationToken ct);
    Task<IReadOnlyList<ProductivityActionDto>> PendenciasAsync(CancellationToken ct);
    Task<IReadOnlyList<ProductivityActionDto>> AgendaAsync(CancellationToken ct);
    IReadOnlyList<QuickActionDto> AcoesRapidas();
}

public sealed class MeuDiaService : IMeuDiaService
{
    private readonly IMeuDiaRepository repository; private readonly IProductivityActionService productivity; public MeuDiaService(IMeuDiaRepository repository,IProductivityActionService productivity){this.repository=repository;this.productivity=productivity;}
    private static ProductivityQuery Today => new(DueTo: DateTimeOffset.UtcNow.AddDays(1), PageSize: 25);
    public Task<ProductivityPageDto> ObterResumoAsync(CancellationToken ct) => repository.ReadAsync(Today, ct);
    public Task<ProductivitySummaryDto> IndicadoresAsync(CancellationToken ct) => productivity.SummaryAsync(ct);
    public async Task<IReadOnlyList<ProductivityActionDto>> PendenciasAsync(CancellationToken ct) => (await repository.ReadAsync(Today, ct)).Items;
    public async Task<IReadOnlyList<ProductivityActionDto>> AgendaAsync(CancellationToken ct) => (await repository.ReadAsync(new ProductivityQuery(Module:"CLINICO",DueTo:DateTimeOffset.UtcNow.AddDays(1)),ct)).Items;
    public IReadOnlyList<QuickActionDto> AcoesRapidas() => productivity.QuickActions();
}
