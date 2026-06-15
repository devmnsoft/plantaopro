using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Services;

public sealed class Saude360WebService
{
    private readonly IHttpClientFactory factory;
    private readonly ILogger<Saude360WebService> logger;
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };

    public Saude360WebService(IHttpClientFactory factory, ILogger<Saude360WebService> logger)
    {
        this.factory = factory;
        this.logger = logger;
    }

    public async Task<(IEnumerable<Saude360RegistroViewModel> Registros, string Error)> ListarAsync(string token, string endpoint)
    {
        try
        {
            var client = CreateClient(token);
            var response = await client.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) return (Array.Empty<Saude360RegistroViewModel>(), ApiErrorPresenter.ToFriendlyMessage(content));
            var parsed = ParseRegistros(content);
            return (parsed.Registros, parsed.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao carregar endpoint Saúde 360 {Endpoint}", endpoint);
            return (Array.Empty<Saude360RegistroViewModel>(), ApiErrorPresenter.ToFriendlyMessage(ex.Message));
        }
    }

    public async Task<(bool Success, string Message)> EnviarAsync(string token, string endpoint, Saude360FormViewModel form)
    {
        try
        {
            var client = CreateClient(token);
            var payload = JsonSerializer.Serialize(new
            {
                form.PacienteId,
                form.MedicoId,
                form.AgendamentoId,
                form.ConsultaId,
                form.PlanoSaudeId,
                form.Nome,
                form.Descricao,
                form.Codigo,
                form.Motivo,
                form.Justificativa,
                form.FormaPagamento,
                form.NumeroCarteirinha,
                form.Cpf,
                form.Cns,
                form.DocumentoAlternativo,
                form.NomeSocial,
                form.SexoGenero,
                form.Telefone,
                form.Email,
                form.Endereco,
                form.ResponsavelNome,
                form.Especialidade,
                form.ClassificacaoRisco,
                form.QueixaPrincipal,
                form.DataInicio,
                form.DataFim,
                form.DataNascimento,
                form.PressaoSistolica,
                form.PressaoDiastolica,
                form.FrequenciaCardiaca,
                form.FrequenciaRespiratoria,
                form.Temperatura,
                form.Saturacao,
                form.Peso,
                form.Altura,
                form.Valor,
                form.Principal
            }, JsonOptions);
            var contentPayload = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = form.Id.HasValue ? await client.PutAsync(endpoint, contentPayload) : await client.PostAsync(endpoint, contentPayload);
            var content = await response.Content.ReadAsStringAsync();
            var envelope = JsonSerializer.Deserialize<Saude360ApiEnvelope<Saude360RegistroViewModel>>(content, JsonOptions);
            return (response.IsSuccessStatusCode && (envelope == null || envelope.Success), ApiErrorPresenter.ToFriendlyMessage(envelope?.Message ?? (response.IsSuccessStatusCode ? "Operação concluída." : "Operação não concluída.")));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar endpoint Saúde 360 {Endpoint}", endpoint);
            return (false, ApiErrorPresenter.ToFriendlyMessage(ex.Message));
        }
    }

    private static (IEnumerable<Saude360RegistroViewModel> Registros, string Message) ParseRegistros(string content)
    {
        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;
        var message = root.TryGetProperty("message", out var msg) ? msg.GetString() ?? string.Empty : string.Empty;
        if (!root.TryGetProperty("data", out var data) || data.ValueKind == JsonValueKind.Null) return (Array.Empty<Saude360RegistroViewModel>(), message);
        if (data.ValueKind == JsonValueKind.Array)
        {
            var registros = JsonSerializer.Deserialize<IEnumerable<Saude360RegistroViewModel>>(data.GetRawText(), JsonOptions) ?? Array.Empty<Saude360RegistroViewModel>();
            return (registros, message);
        }
        if (data.ValueKind == JsonValueKind.Object)
        {
            if (data.TryGetProperty("id", out _))
            {
                var registro = JsonSerializer.Deserialize<Saude360RegistroViewModel>(data.GetRawText(), JsonOptions);
                return (registro is null ? Array.Empty<Saude360RegistroViewModel>() : new[] { registro }, message);
            }

            var registrosResumo = new List<Saude360RegistroViewModel>();
            foreach (var property in data.EnumerateObject())
            {
                registrosResumo.Add(new Saude360RegistroViewModel
                {
                    Id = Guid.Empty,
                    Nome = FormatLabel(property.Name),
                    Codigo = "KPI",
                    Descricao = FormatValue(property.Value),
                    Status = "ATUAL",
                    RegDate = DateTime.UtcNow
                });
            }
            return (registrosResumo, message);
        }
        return (Array.Empty<Saude360RegistroViewModel>(), message);
    }

    private static string FormatLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return value.Replace("_", " ").Trim();
    }

    private static string FormatValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)) return number.ToString("N0");
        if (value.ValueKind == JsonValueKind.String) return value.GetString() ?? string.Empty;
        if (value.ValueKind == JsonValueKind.True) return "Sim";
        if (value.ValueKind == JsonValueKind.False) return "Não";
        if (value.ValueKind == JsonValueKind.Null) return "0";
        return value.ToString();
    }

    private HttpClient CreateClient(string token)
    {
        var client = factory.CreateClient("PlantaoProApi");
        if (!string.IsNullOrWhiteSpace(token)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private sealed class Saude360ApiEnvelope<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }
}
