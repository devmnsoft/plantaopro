using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Services;

/// <summary>
/// P2 IA — BFF (Web -> API) das tarefas e da configuração do assistente.
/// Espelha o padrão canônico de <see cref="ProductivityWebService"/>:
/// correlação em toda chamada (X-Correlation-ID), prazo total HTTP que cobre
/// envio E leitura integral do corpo, classificação honesta da falha
/// (cancelado/tempo limite/transporte/401/403/5xx/resposta inválida) e nenhum
/// segredo ou token em mensagens ou logs. O contexto e o tenant são montados
/// no servidor (API) após autorização — a UI nunca monta o prompt.
/// </summary>
public sealed class AiWebService
{
    public const string StatusNaoAutenticado = "NAO_AUTENTICADO";
    public const string MensagemSessaoExpirada = "Sua sessão expirou. Entre novamente para continuar.";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<AiWebService> _logger;

    public AiWebService(IHttpClientFactory clients, ILogger<AiWebService> logger)
        => (_clients, _logger) = (clients, logger);

    /// <summary>Jornada 1 da entrega inicial: resumo das pendências do Meu Dia.</summary>
    public Task<AiOutcomeViewModel> ResumoMeuDiaAsync(string token, CancellationToken ct)
        => PostOutcomeAsync(token, "api/ai/tarefas/meu-dia-resumo", null, ct);

    /// <summary>Jornada 2 da entrega inicial: análise de cotação (Administrativo 360).</summary>
    public Task<AiOutcomeViewModel> AnaliseCotacaoAsync(string token, Guid cotacaoId, CancellationToken ct)
        => PostOutcomeAsync(token, "api/ai/tarefas/analise-cotacao", new { cotacaoId }, ct);

    /// <summary>Teste de conexão autorizado (não consome cota da tarefa).</summary>
    public Task<AiOutcomeViewModel> TestarConexaoAsync(string token, string provider, CancellationToken ct)
        => PostOutcomeAsync(token, "api/ai/test-conexao", new { provedor = provider }, ct);

    /// <summary>Configuração por tarefa (admin): chaves sempre mascaradas na leitura.</summary>
    public async Task<AiConfigPageViewModel> ObterConfiguracoesAsync(string token, CancellationToken ct)
    {
        const string uri = "api/ai/config";
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var client = CreateClient(token);
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.TryAddWithoutValidation("X-Correlation-ID", correlationId);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!response.IsSuccessStatusCode)
            {
                stopwatch.Stop();
                var status = response.StatusCode;
                _logger.LogWarning("IA(config): API respondeu {StatusCode} para {Uri} em {ElapsedMs} ms (correlacao={CorrelationId}).", (int)status, uri, stopwatch.ElapsedMilliseconds, correlationId);
                return new AiConfigPageViewModel { Erro = ErroPorStatus(status) };
            }

            try
            {
                var dto = await response.Content.ReadFromJsonAsync<AiConfigResponseDto>(Json, ct);
                stopwatch.Stop();
                _logger.LogInformation("BFF->IA GET {Uri} Status={StatusCode} DuracaoTotalMs={DuracaoTotalMs} CorrelationId={CorrelationId}", uri, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, correlationId);
                return new AiConfigPageViewModel
                {
                    Configuracoes = dto?.Configuracoes ?? Array.Empty<AiConfiguracaoViewModel>(),
                    ChaveMestraDoServidorConfigurada = dto?.ChaveMestraDoServidorConfigurada ?? false
                };
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(exception, "IA(config): resposta inválida da API para {Uri} (correlacao={CorrelationId}).", uri, correlationId);
                return new AiConfigPageViewModel { Erro = "A configuração do assistente veio em um formato inesperado. Tente novamente." };
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogDebug("IA(config): consulta cancelada pelo chamador para {Uri} em {ElapsedMs} ms (correlacao={CorrelationId}).", uri, stopwatch.ElapsedMilliseconds, correlationId);
            return new AiConfigPageViewModel { Erro = "A consulta foi cancelada antes da conclusão." };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("IA(config): tempo limite excedido (envio ou leitura do corpo) para {Uri} após {ElapsedMs} ms (correlacao={CorrelationId}).", uri, stopwatch.ElapsedMilliseconds, correlationId);
            return new AiConfigPageViewModel { Erro = "A resposta do assistente demorou mais do que o limite permitido. Tente novamente." };
        }
        catch (HttpRequestException exception)
        {
            stopwatch.Stop();
            _logger.LogWarning(exception, "IA(config): falha de transporte para {Uri} em {ElapsedMs} ms (correlacao={CorrelationId}).", uri, stopwatch.ElapsedMilliseconds, correlationId);
            return new AiConfigPageViewModel { Erro = "A configuração do assistente está temporariamente indisponível. Tente novamente." };
        }
    }

    /// <summary>Salvamento por tarefa; 400 devolve a mensagem de validação do gateway.</summary>
    public async Task<(bool Ok, string? Mensagem)> SalvarConfiguracaoAsync(string token, string task, AiConfigFormModel form, CancellationToken ct)
    {
        var uri = $"api/ai/config/{Uri.EscapeDataString(task)}";
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var client = CreateClient(token);
            using var request = new HttpRequestMessage(HttpMethod.Put, uri) { Content = JsonContent.Create(form, options: Json) };
            request.Headers.TryAddWithoutValidation("X-Correlation-ID", correlationId);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!response.IsSuccessStatusCode)
            {
                stopwatch.Stop();
                var status = response.StatusCode;
                string? apiMessage = null;
                if (status == HttpStatusCode.BadRequest)
                {
                    try
                    {
                        var raw = await response.Content.ReadAsStringAsync(ct);
                        using var doc = JsonDocument.Parse(raw);
                        if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                            doc.RootElement.TryGetProperty("mensagem", out var m) && m.ValueKind == JsonValueKind.String)
                        {
                            apiMessage = m.GetString();
                        }
                    }
                    catch (JsonException)
                    {
                        // Corpo não-JSON: mantém a mensagem genérica abaixo.
                    }
                }
                _logger.LogWarning("IA(config): API respondeu {StatusCode} para {Uri} em {ElapsedMs} ms (correlacao={CorrelationId}).", (int)status, uri, stopwatch.ElapsedMilliseconds, correlationId);
                return status switch
                {
                    HttpStatusCode.Unauthorized => (false, MensagemSessaoExpirada),
                    HttpStatusCode.Forbidden => (false, "Seu perfil não possui acesso à configuração do assistente ou o acesso foi revogado."),
                    HttpStatusCode.BadRequest => (false, string.IsNullOrWhiteSpace(apiMessage) ? "Revise os campos e tente novamente." : apiMessage),
                    _ when (int)status >= 500 => (false, "O serviço do assistente apresentou uma falha interna. Tente novamente."),
                    _ => (false, "Não foi possível salvar agora. Tente novamente.")
                };
            }

            stopwatch.Stop();
            _logger.LogInformation("BFF->IA PUT {Uri} Status={StatusCode} DuracaoTotalMs={DuracaoTotalMs} CorrelationId={CorrelationId}", uri, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, correlationId);
            return (true, null);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            stopwatch.Stop();
            return (false, "A operação foi cancelada antes da conclusão.");
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("IA(config): tempo limite excedido (envio ou leitura do corpo) para {Uri} após {ElapsedMs} ms (correlacao={CorrelationId}).", uri, stopwatch.ElapsedMilliseconds, correlationId);
            return (false, "A operação demorou mais do que o limite permitido. Tente novamente.");
        }
        catch (HttpRequestException exception)
        {
            stopwatch.Stop();
            _logger.LogWarning(exception, "IA(config): falha de transporte para {Uri} em {ElapsedMs} ms (correlacao={CorrelationId}).", uri, stopwatch.ElapsedMilliseconds, correlationId);
            return (false, "O serviço está temporariamente indisponível. Tente novamente.");
        }
    }

    private static string ErroPorStatus(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized => MensagemSessaoExpirada,
        HttpStatusCode.Forbidden => "Seu perfil não possui acesso à configuração do assistente ou o acesso foi revogado.",
        _ when (int)status >= 500 => "O serviço do assistente apresentou uma falha interna. Tente novamente.",
        _ => "Não foi possível carregar os dados agora. Tente novamente."
    };

    private async Task<AiOutcomeViewModel> PostOutcomeAsync(string token, string uri, object? payload, CancellationToken ct)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var client = CreateClient(token);
            using var request = payload is null
                ? new HttpRequestMessage(HttpMethod.Post, uri)
                : new HttpRequestMessage(HttpMethod.Post, uri) { Content = JsonContent.Create(payload, options: Json) };
            // Correlação Web <-> API: o mesmo identificador chega ao header X-Correlation-ID.
            request.Headers.TryAddWithoutValidation("X-Correlation-ID", correlationId);
            // Prazo total HTTP (WP-S2): o Timeout do cliente cobre envio E leitura integral do corpo.
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!response.IsSuccessStatusCode)
            {
                stopwatch.Stop();
                var status = response.StatusCode;
                _logger.LogWarning("IA: API respondeu {StatusCode} para {Uri} em {ElapsedMs} ms (correlacao={CorrelationId}).", (int)status, uri, stopwatch.ElapsedMilliseconds, correlationId);
                return status switch
                {
                    HttpStatusCode.NotFound => new AiOutcomeViewModel { Success = false, StatusKind = "NAO_ENCONTRADO", Mensagem = "Registro não encontrado neste cliente." },
                    HttpStatusCode.Unauthorized => new AiOutcomeViewModel { Success = false, StatusKind = StatusNaoAutenticado, Mensagem = MensagemSessaoExpirada },
                    HttpStatusCode.Forbidden => new AiOutcomeViewModel { Success = false, StatusKind = "SEM_PERMISSAO", Mensagem = "Seu perfil não possui acesso ao assistente ou o acesso foi revogado." },
                    _ when (int)status >= 500 => new AiOutcomeViewModel { Success = false, StatusKind = "FALHA_SERVIDOR", Mensagem = "O serviço do assistente apresentou uma falha interna. Tente novamente." },
                    _ => new AiOutcomeViewModel { Success = false, StatusKind = "ERRO_INESPERADO", Mensagem = "Não foi possível concluir agora. Tente novamente." }
                };
            }

            try
            {
                var outcome = await response.Content.ReadFromJsonAsync<AiOutcomeViewModel>(Json, ct)
                             ?? new AiOutcomeViewModel { Success = false, StatusKind = "RESPOSTA_INVALIDA", Mensagem = "A resposta do assistente veio vazia. Tente novamente." };
                stopwatch.Stop();
                _logger.LogInformation("BFF->IA POST {Uri} Status={StatusCode} DuracaoTotalMs={DuracaoTotalMs} CorrelationId={CorrelationId}", uri, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, correlationId);
                return outcome;
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(exception, "IA: resposta inválida da API para {Uri} (correlacao={CorrelationId}).", uri, correlationId);
                return new() { Success = false, StatusKind = "RESPOSTA_INVALIDA", Mensagem = "A resposta do assistente veio em um formato inesperado. Tente novamente." };
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogDebug("IA: consulta cancelada pelo chamador para {Uri} em {ElapsedMs} ms (correlacao={CorrelationId}).", uri, stopwatch.ElapsedMilliseconds, correlationId);
            return new AiOutcomeViewModel { Success = false, StatusKind = "CANCELADO", Mensagem = "A consulta foi cancelada antes da conclusão." };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("IA: tempo limite excedido (envio ou leitura do corpo) para {Uri} após {ElapsedMs} ms (correlacao={CorrelationId}).", uri, stopwatch.ElapsedMilliseconds, correlationId);
            return new AiOutcomeViewModel { Success = false, StatusKind = "TIMEOUT", Mensagem = "A resposta do assistente demorou mais do que o limite permitido. Tente novamente." };
        }
        catch (HttpRequestException exception)
        {
            stopwatch.Stop();
            _logger.LogWarning(exception, "IA: falha de transporte para {Uri} em {ElapsedMs} ms (correlacao={CorrelationId}).", uri, stopwatch.ElapsedMilliseconds, correlationId);
            return new AiOutcomeViewModel { Success = false, StatusKind = "TRANSPORTE", Mensagem = "O assistente está temporariamente indisponível. Tente novamente." };
        }
    }

    private HttpClient CreateClient(string token)
    {
        var client = _clients.CreateClient("PlantaoProApi");
        if (!string.IsNullOrWhiteSpace(token)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private sealed record AiConfigResponseDto(IReadOnlyList<AiConfiguracaoViewModel>? Configuracoes, bool ChaveMestraDoServidorConfigurada);
}
