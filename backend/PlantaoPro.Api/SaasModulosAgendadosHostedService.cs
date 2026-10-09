using PlantaoPro.Api;

namespace PlantaoPro.Api;

/// <summary>
/// R5-E13/P2: scheduler da ativacao de contratos AGENDADO vencidos (kernel B4
/// <see cref="SaasModuleCatalogService.AtivarAgendadosProgramadoAsync"/>). Antes a unica porta era o
/// POST manual api/admin-saas/modulos/ativar-agendados (ADMINISTRADOR_GLOBAL); um upgrade com inicio
/// futuro só começava a operar se alguém lembrasse de chamar. O scheduler roda em qualquer ambiente
/// menos Testing (a suite xUnit compartilha o banco e nao pode ver estados mudando por baixo), com o
/// mesmo kernel transacional e idempotente da rota: so AGENDADO com ativado_em &lt;= now() transiciona,
/// FOR UPDATE serializa instancias concorrentes e cada tick sem vencidos e no-op.
/// O ator do tick e o sistema (trilha tenant_modulos_historico com usuario_id NULL e
/// ip_origem SCHEDULER) — nunca finge ser usuario logado.
/// Config: Saas:AtivarAgendados:IntervaloSegundos (default 300; &lt;= 0 desliga).
/// </summary>
public sealed class SaasModulosAgendadosHostedService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IConfiguration configuration;
    private readonly IHostEnvironment environment;
    private readonly ILogger<SaasModulosAgendadosHostedService> logger;

    public SaasModulosAgendadosHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<SaasModulosAgendadosHostedService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.configuration = configuration;
        this.environment = environment;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (environment.IsEnvironment("Testing")) return;

        var intervaloSegundos = 300; // default: 5 minutos
        if (configuration["Saas:AtivarAgendados:IntervaloSegundos"] is { } raw && int.TryParse(raw, out var parseado))
            intervaloSegundos = parseado;
        if (intervaloSegundos <= 0 || string.IsNullOrWhiteSpace(configuration.GetConnectionString("Default"))) return;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervaloSegundos));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    // Escopo por tick: SaasModuleCatalogService e scoped e so depende de configuracao
                    // quando nao ha contexto HTTP (nucleo do scheduler nao le usuario).
                    using var scope = scopeFactory.CreateScope();
                    var catalog = scope.ServiceProvider.GetRequiredService<SaasModuleCatalogService>();
                    var result = await catalog.AtivarAgendadosProgramadoAsync(stoppingToken);
                    if (result.StatusCode == 200 && result.Data > 0)
                        logger.LogWarning("SaaS: {Quantidade} contrato(s) AGENDADO(s) ativado(s) pelo scheduler (ip_origem SCHEDULER).", result.Data);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Tick falho nao derruba o host; o proximo tick retenta (kernel idempotente).
                    logger.LogWarning(ex, "SaaS: tick do scheduler de contratos AGENDADO nao concluiu; sera retomado no proximo tick.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // encerramento normal da host
        }
    }
}
