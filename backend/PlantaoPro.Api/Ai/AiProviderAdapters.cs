using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace PlantaoPro.Api.Ai;

/// <summary>
/// Adapter de um provedor de IA concreto (Groq / Gemini / DeepSeek).
/// Cada adapter:
///   - conhece a chave global opcional do servidor (fallback do tenant);
///   - recebe a chave a usar do gateway (chave do tenant tem prioridade);
///   - classifica falhas nas classes canônicas (<see cref="AiErrorKinds"/>);
///   - nunca propaga segredos para fora (logs/mensagens sem chave, corpo ou URL com token).
/// </summary>
public interface IAiProviderAdapter
{
    string Provider { get; }
    bool HasGlobalKey { get; }
    string GlobalKey { get; }
    Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, string apiKey, CancellationToken ct);
}

/// <summary>
/// Helper de transporte compartilhado pelos adaptadores.
/// Padrão da homologação P0 (WP-S2): envio e leitura integral do corpo num único try —
/// com handler primário customizado o HttpClient.Timeout NÃO cobre a leitura do corpo
/// isolada; aqui o cancelamento do timeout chega como OperationCanceledException dentro
/// do mesmo escopo e vira TIMEOUT (nunca hang, nunca 500).
/// </summary>
internal static class AiHttp
{
    internal static async Task<(int Status, string Body)> SendAndReadAsync(
        HttpClient client, HttpRequestMessage request, string provider, CancellationToken ct)
    {
        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return ((int)response.StatusCode, body);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new AiProviderException(AiErrorKinds.Timeout, $"{provider}: tempo limite excedido ao aguardar a resposta.");
        }
        catch (HttpRequestException ex)
        {
            throw new AiProviderException(AiErrorKinds.Transporte, $"{provider}: falha de transporte ao contatar o provedor.", ex);
        }
    }

    internal static void ClassifyStatus(int status, string provider)
    {
        if (status == 429)
            // HTTP 429 = limitação de requisições do lado do provedor (rate limit).
            // Não é possível afirmar aqui que a chave foi aceita nem descartada.
            throw new AiProviderException(AiErrorKinds.ProvedorLimitado,
                $"{provider}: o provedor reportou limitação de requisições neste momento (HTTP 429).");
        if (status < 200 || status >= 300)
            throw new AiProviderException(AiErrorKinds.Transporte,
                $"{provider}: o provedor respondeu HTTP {status}.");
    }

    /// <summary>Interpreta o wire format compatível com OpenAI (Groq e DeepSeek).</summary>
    internal static AiCompletionResult ParseOpenAi(string provider, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("choices", out var choices)
                || choices.ValueKind != JsonValueKind.Array
                || choices.GetArrayLength() == 0)
                throw new FormatException("resposta sem choices");

            var message = choices[0].GetProperty("message").GetProperty("content");
            var text = message.ValueKind == JsonValueKind.String ? message.GetString() : null;
            if (string.IsNullOrWhiteSpace(text)) throw new FormatException("conteúdo vazio");

            int tokensIn = 0, tokensOut = 0;
            if (doc.RootElement.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
            {
                if (usage.TryGetProperty("prompt_tokens", out var pt) && pt.TryGetInt32(out var a)) tokensIn = a;
                if (usage.TryGetProperty("completion_tokens", out var cto) && cto.TryGetInt32(out var b)) tokensOut = b;
            }
            string model = doc.RootElement.TryGetProperty("model", out var m) && m.ValueKind == JsonValueKind.String
                ? m.GetString() ?? string.Empty
                : string.Empty;
            return new AiCompletionResult(text.Trim(), model, tokensIn, tokensOut);
        }
        catch (Exception ex) when (ex is JsonException or FormatException or KeyNotFoundException or ArgumentOutOfRangeException)
        {
            throw new AiProviderException(AiErrorKinds.RespostaInvalida,
                $"{provider}: a resposta do provedor não pôde ser interpretada.");
        }
    }
}

/// <summary>
/// Base para provedores com wire format compatível com OpenAI
/// (POST chat/completions, Bearer no Authorization).
/// </summary>
public abstract class OpenAiCompatibleAdapter : IAiProviderAdapter
{
    private readonly IHttpClientFactory _clients;
    private readonly string _clientName;

    protected OpenAiCompatibleAdapter(IHttpClientFactory clients, IConfiguration configuration, string clientName, string keyConfigPath)
    {
        _clients = clients;
        _clientName = clientName;
        GlobalKey = (configuration[keyConfigPath] ?? string.Empty).Trim();
    }

    public abstract string Provider { get; }
    public bool HasGlobalKey => !string.IsNullOrEmpty(GlobalKey);
    public string GlobalKey { get; }

    public abstract Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, string apiKey, CancellationToken ct);

    protected async Task<AiCompletionResult> CompleteOpenAiAsync(AiCompletionRequest request, string apiKey, CancellationToken ct)
    {
        var client = _clients.CreateClient(_clientName);
        using var http = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(new
            {
                model = request.Model,
                temperature = request.Temperature,
                max_tokens = request.MaxTokens,
                messages = new[]
                {
                    new { role = "system", content = request.SystemPrompt },
                    new { role = "user", content = request.UserPrompt }
                }
            })
        };
        http.Headers.TryAddWithoutValidation("Authorization", "Bearer " + apiKey);

        // A chave viaja SOMENTE no cabeçalho Authorization — nunca no corpo.
        var (status, body) = await AiHttp.SendAndReadAsync(client, http, Provider, ct);
        AiHttp.ClassifyStatus(status, Provider);
        return AiHttp.ParseOpenAi(Provider, body);
    }
}

public sealed class GroqAdapter : OpenAiCompatibleAdapter
{
    public GroqAdapter(IHttpClientFactory clients, IConfiguration configuration)
        : base(clients, configuration, "AiGroq", "Ai:Providers:Groq:ApiKey") { }

    public override string Provider => AiProviderCodes.Groq;

    public override Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, string apiKey, CancellationToken ct)
        => CompleteOpenAiAsync(request, apiKey, ct);
}

public sealed class DeepSeekAdapter : OpenAiCompatibleAdapter
{
    public DeepSeekAdapter(IHttpClientFactory clients, IConfiguration configuration)
        : base(clients, configuration, "AiDeepSeek", "Ai:Providers:DeepSeek:ApiKey") { }

    public override string Provider => AiProviderCodes.DeepSeek;

    public override Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, string apiKey, CancellationToken ct)
        => CompleteOpenAiAsync(request, apiKey, ct);
}

/// <summary>
/// Gemini: wire format próprio (v1beta models/{model}:generateContent,
/// chave no header x-goog-api-key, uso em usageMetadata).
/// </summary>
public sealed class GeminiAdapter : IAiProviderAdapter
{
    private readonly IHttpClientFactory _clients;

    public GeminiAdapter(IHttpClientFactory clients, IConfiguration configuration)
    {
        _clients = clients;
        GlobalKey = (configuration["Ai:Providers:Gemini:ApiKey"] ?? string.Empty).Trim();
    }

    public string Provider => AiProviderCodes.Gemini;
    public bool HasGlobalKey => !string.IsNullOrEmpty(GlobalKey);
    public string GlobalKey { get; }

    public async Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, string apiKey, CancellationToken ct)
    {
        var client = _clients.CreateClient("AiGemini");
        using var http = new HttpRequestMessage(
            HttpMethod.Post, "v1beta/models/" + Uri.EscapeDataString(request.Model) + ":generateContent")
        {
            Content = JsonContent.Create(new
            {
                systemInstruction = new { parts = new[] { new { text = request.SystemPrompt } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = request.UserPrompt } } } },
                generationConfig = new { maxOutputTokens = request.MaxTokens, temperature = request.Temperature }
            })
        };
        http.Headers.TryAddWithoutValidation("x-goog-api-key", apiKey);

        var (status, body) = await AiHttp.SendAndReadAsync(client, http, Provider, ct);
        AiHttp.ClassifyStatus(status, Provider);

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("candidates", out var candidates)
                || candidates.ValueKind != JsonValueKind.Array
                || candidates.GetArrayLength() == 0)
                throw new FormatException("resposta sem candidates");

            var candidate = candidates[0];
            string text = string.Empty;
            if (candidate.TryGetProperty("content", out var content)
                && content.TryGetProperty("parts", out var parts)
                && parts.ValueKind == JsonValueKind.Array
                && parts.GetArrayLength() > 0
                && parts[0].TryGetProperty("text", out var textEl))
                text = textEl.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text)) throw new FormatException("texto vazio");

            int tokensIn = 0, tokensOut = 0;
            if (doc.RootElement.TryGetProperty("usageMetadata", out var um) && um.ValueKind == JsonValueKind.Object)
            {
                if (um.TryGetProperty("promptTokenCount", out var ptc) && ptc.TryGetInt32(out var a)) tokensIn = a;
                if (um.TryGetProperty("candidatesTokenCount", out var ctc) && ctc.TryGetInt32(out var b)) tokensOut = b;
            }
            string model = doc.RootElement.TryGetProperty("modelVersion", out var mv) && mv.ValueKind == JsonValueKind.String
                ? mv.GetString() ?? request.Model
                : request.Model;
            return new AiCompletionResult(text.Trim(), model, tokensIn, tokensOut);
        }
        catch (Exception ex) when (ex is JsonException or FormatException or KeyNotFoundException or ArgumentOutOfRangeException)
        {
            throw new AiProviderException(AiErrorKinds.RespostaInvalida,
                $"{Provider}: a resposta do provedor não pôde ser interpretada.");
        }
    }
}
