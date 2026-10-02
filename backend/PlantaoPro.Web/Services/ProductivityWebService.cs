using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Services;

public sealed class ProductivityWebService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<ProductivityWebService> _logger;

    public ProductivityWebService(IHttpClientFactory clients, ILogger<ProductivityWebService> logger)
        => (_clients, _logger) = (clients, logger);

    public Task<ProductivityPageViewModel> GetActionsAsync(string token, ProductivityQueryViewModel query, CancellationToken ct)
        => GetAsync(token, BuildQuery("api/produtividade", query), ct);

    public Task<ProductivityPageViewModel> GetMyDayAsync(string token, CancellationToken ct)
        => GetAsync(token, "api/produtividade/meu-dia", ct);

    public async Task<HttpResponseMessage> SnoozeAsync(string token, string key, DateTimeOffset until, CancellationToken ct)
    {
        var client = CreateClient(token);
        return await client.PostAsJsonAsync($"api/produtividade/{Uri.EscapeDataString(key)}/adiar", new { snoozedUntil = until }, Json, ct);
    }

    /// <summary>
    /// Busca a visão da produtividade classificando a falha de forma honesta:
    /// timeout (tempo limite do HttpClient), cancelamento legítimo do navegador/chamador,
    /// falha de transporte, 401, 403, 5xx e resposta inválida. O identificador de
    /// correlação é registrado nos logs para casar Web/API/banco; nenhum valor sensível
    /// (token, cabeçalhos) entra na mensagem ou no log.
    /// </summary>
    private async Task<ProductivityPageViewModel> GetAsync(string token, string uri, CancellationToken ct)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();
        HttpResponseMessage? response = null;
        try
        {
            response = await CreateClient(token).GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogDebug("Productivity: consulta cancelada pelo chamador para {Uri} em {ElapsedMs} ms (correlacao={CorrelationId}).", uri, stopwatch.ElapsedMilliseconds, correlationId);
            return new() { ErrorKind = "CANCELADO", Error = "A consulta foi cancelada antes da conclusão." };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("Productivity: tempo limite excedido para {Uri} após {ElapsedMs} ms (correlacao={CorrelationId}).", uri, stopwatch.ElapsedMilliseconds, correlationId);
            return new() { ErrorKind = "TIMEOUT", Error = "A resposta da Central de Ações demorou mais do que o limite permitido. Tente novamente." };
        }
        catch (HttpRequestException exception)
        {
            stopwatch.Stop();
            _logger.LogWarning(exception, "Productivity: falha de transporte para {Uri} em {ElapsedMs} ms (correlacao={CorrelationId}).", uri, stopwatch.ElapsedMilliseconds, correlationId);
            return new() { ErrorKind = "TRANSPORTE", Error = "A Central de Ações está temporariamente indisponível. Tente novamente." };
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                stopwatch.Stop();
                var status = response.StatusCode;
                _logger.LogWarning("Productivity: API respondeu {StatusCode} para {Uri} em {ElapsedMs} ms (correlacao={CorrelationId}).", (int)status, uri, stopwatch.ElapsedMilliseconds, correlationId);
                return status switch
                {
                    HttpStatusCode.Unauthorized => new ProductivityPageViewModel { ErrorKind = "NAO_AUTENTICADO", Error = "Sua sessão expirou. Entre novamente para continuar." },
                    HttpStatusCode.Forbidden => new ProductivityPageViewModel { ErrorKind = "SEM_PERMISSAO", Error = "Seu perfil não possui acesso a esta visão ou o acesso foi revogado." },
                    _ when (int)status >= 500 => new ProductivityPageViewModel { ErrorKind = "FALHA_SERVIDOR", Error = "O serviço que carrega o seu dia apresentou uma falha interna. Tente novamente." },
                    _ => new ProductivityPageViewModel { ErrorKind = "ERRO_INESPERADO", Error = "Não foi possível carregar os dados reais agora. Tente novamente." }
                };
            }

            stopwatch.Stop();
            try
            {
                return await response.Content.ReadFromJsonAsync<ProductivityPageViewModel>(Json, ct)
                       ?? new ProductivityPageViewModel { ErrorKind = "RESPOSTA_INVALIDA", Error = "A resposta da Central de Ações veio vazia. Tente novamente." };
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogDebug("Productivity: leitura da resposta cancelada pelo chamador para {Uri} (correlacao={CorrelationId}).", uri, correlationId);
                return new() { ErrorKind = "CANCELADO", Error = "A consulta foi cancelada antes da conclusão." };
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(exception, "Productivity: resposta inválida da API para {Uri} (correlacao={CorrelationId}).", uri, correlationId);
                return new() { ErrorKind = "RESPOSTA_INVALIDA", Error = "A resposta da Central de Ações veio em um formato inesperado. Tente novamente." };
            }
        }
    }

    private HttpClient CreateClient(string token)
    {
        var client = _clients.CreateClient("PlantaoProApi");
        if (!string.IsNullOrWhiteSpace(token)) client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    private static string BuildQuery(string path, ProductivityQueryViewModel query)
    {
        var values = new Dictionary<string, string?>
        {
            ["tab"] = query.Tab, ["priority"] = query.Priority, ["module"] = query.Module,
            ["status"] = query.Status, ["unitId"] = query.UnitId, ["mine"] = query.Mine ? "true" : null,
            ["page"] = Math.Max(1, query.Page).ToString(), ["pageSize"] = Math.Clamp(query.PageSize, 1, 100).ToString()
        };
        var now = DateTimeOffset.UtcNow;
        if (query.PeriodFrom.HasValue) values["dueFrom"] = query.PeriodFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).ToString("O");
        if (query.PeriodTo.HasValue) values["dueTo"] = query.PeriodTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).ToString("O");
        if (query.Due == "hoje") { values["dueFrom"] = now.Date.ToString("O"); values["dueTo"] = now.Date.AddDays(1).ToString("O"); }
        else if (query.Due == "atrasado") values["dueTo"] = now.ToString("O");
        return Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(path, values.Where(x => !string.IsNullOrWhiteSpace(x.Value)).ToDictionary(x => x.Key, x => x.Value!));
    }
}
