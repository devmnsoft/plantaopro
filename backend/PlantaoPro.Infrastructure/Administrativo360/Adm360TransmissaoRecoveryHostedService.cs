using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PlantaoPro.Infrastructure.Administrativo360;

/// <summary>
/// B7: reconciliação PERÍODICA de respostas presas em ENVIANDO (complemento à recovery de boot).
/// Um crash durante a chamada externa já é coberto no boot; este serviço cobre quedas que
/// ocorrem com o processo vivo (ex.: exceção após o commit da marcação e antes da finalização
/// registrada, ou finalização perdida por falha de infraestrutura). Executa apenas fora do
/// ambiente Testing, com intervalo maior que zero e connection string presente.
/// Risco documentado: não há lock entre ticks — uma execução pode sobrepor-se à anterior se o
/// tick anterior ainda estiver em curso. A sobreposição é inofensiva porque a conciliação é
/// idempotente (WHERE status_transmissao = 'ENVIANDO' com limiar de idade) e as tentativas de
/// envio são append-only com UNIQUE(tenant_id, resposta_id, tentativa).
/// Configuração: Adm360:TransmissaoRecovery:IntervaloSegundos (default 300; &lt;= 0 desliga).
/// </summary>
public sealed class Adm360TransmissaoRecoveryHostedService : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;
    private readonly ILogger<Adm360TransmissaoRecoveryHostedService> _logger;

    public Adm360TransmissaoRecoveryHostedService(
        IConfiguration config,
        IHostEnvironment env,
        ILogger<Adm360TransmissaoRecoveryHostedService> logger)
    {
        _config = config;
        _env = env;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_env.IsEnvironment("Testing")) return;

        var intervaloSegundos = 300; // default: 5 minutos
        if (_config["Adm360:TransmissaoRecovery:IntervaloSegundos"] is { } rawIntervalo && int.TryParse(rawIntervalo, out var intervaloParseado))
            intervaloSegundos = intervaloParseado;
        var connectionString = _config.GetConnectionString("Default");
        if (intervaloSegundos <= 0 || string.IsNullOrWhiteSpace(connectionString)) return;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervaloSegundos));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    // Sem token de vida longa por tick individual: o stoppingToken cobre o shutdown
                    // do loop; falhas pontuais são logadas e o próximo tick retenta (idempotente).
                    var recuperadas = await Adm360TransmissaoRecovery.ReconciliarEnviandoAncoradosAsync(connectionString!, 10, CancellationToken.None);
                    if (recuperadas > 0)
                        _logger.LogWarning("Administrativo 360: {Quantidade} resposta(s) recuperada(s) de ENVIANDO para RESULTADO_DESCONHECIDO pela reconciliação periódica.", recuperadas);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Administrativo 360: reconciliação periódica de ENVIANDO não concluída neste tick; será retomada no próximo.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // encerramento normal da host
        }
    }
}
