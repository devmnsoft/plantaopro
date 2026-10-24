using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public class NotificacoesController : BaseWebController
{
    public NotificacoesController(IHttpClientFactory f, ILogger<NotificacoesController> l) : base(f, l) { }

    public IActionResult Index() => View();

    // R103 (integracoes - WhatsApp/e-mail com opt-in): a tela de preferencias renderiza o
    // outbox dos canais externos no servidor. Degradacao graciosa: em falha de comunicacao o
    // outbox aparece vazio em vez de bloquear a tela.
    public async Task<IActionResult> Preferencias()
    {
        var modelo = new MensageriaPreferenciasViewModel();
        using var client = CreateApiClient();
        if (!AddBearerToken(client))
            return View(modelo);
        try
        {
            using var docOutbox = JsonDocument.Parse(await client.GetStringAsync("api/mensageria/outbox?limite=30"));
            if (docOutbox.RootElement.TryGetProperty("data", out var dados) && dados.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in dados.EnumerateArray())
                    modelo.Itens.Add(new MensagemOutboxItemViewModel
                    {
                        Canal = LerTexto(item, "canal"),
                        Destino = LerTexto(item, "destino"),
                        Evento = LerTexto(item, "tipoEvento"),
                        Status = LerTexto(item, "status"),
                        CriadoEm = LerTexto(item, "criadoEm")
                    });
            }

            using var docResumo = JsonDocument.Parse(await client.GetStringAsync("api/mensageria/resumo"));
            if (docResumo.RootElement.TryGetProperty("data", out var resumo))
            {
                modelo.Resumo.Total = LerNumero(resumo, "total");
                modelo.Resumo.Enviadas = LerNumero(resumo, "enviadas");
                modelo.Resumo.BloqueadasSemConsentimento = LerNumero(resumo, "bloqueadasSemConsentimento");
                modelo.Resumo.SemDestino = LerNumero(resumo, "semDestino");
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Falha ao carregar o outbox de mensageria na tela de preferencias");
        }
        return View(modelo);
    }

    private static string LerTexto(JsonElement item, string propriedade) =>
        item.TryGetProperty(propriedade, out var valor) ? valor.GetString() ?? string.Empty : string.Empty;

    private static long LerNumero(JsonElement item, string propriedade) =>
        item.TryGetProperty(propriedade, out var valor) && valor.ValueKind == JsonValueKind.Number ? valor.GetInt64() : 0;
}
