using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public class ComunicacaoController : BaseWebController
{
    public ComunicacaoController(IHttpClientFactory httpClientFactory, ILogger<ComunicacaoController> logger) : base(httpClientFactory, logger) {}

    public async Task<IActionResult> Index()
    {
        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var (conversas, error, _) = await ReadApiResponse<IEnumerable<ConversaListViewModel>>(client, "api/comunicacao/conversas");
            if (!string.IsNullOrWhiteSpace(error)) TempData["Error"] = error;
            return View(conversas ?? Array.Empty<ConversaListViewModel>());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro inesperado na central de comunicação");
            TempData["Error"] = "Não foi possível carregar a central de comunicação.";
            return View(Array.Empty<ConversaListViewModel>());
        }
    }

    public IActionResult NovaConversa() => View(new NovaConversaViewModel());
    public IActionResult Details(Guid id) => View(new ConversaDetalhesViewModel { ConversaId = id });
}
