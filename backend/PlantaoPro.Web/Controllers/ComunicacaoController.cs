using System.Net;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public class ComunicacaoController : BaseWebController
{
    public ComunicacaoController(IHttpClientFactory httpClientFactory, ILogger<ComunicacaoController> logger) : base(httpClientFactory, logger) { }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? search = null, string? tipo = null, string? status = null)
    {
        var model = new ConversaListViewModel { Page = Math.Max(1, page), PageSize = Math.Max(1, pageSize), Search = search, Tipo = tipo, Status = status };

        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();

            var endpoint = $"api/comunicacao/conversas?page={model.Page}&pageSize={model.PageSize}&search={Uri.EscapeDataString(search ?? string.Empty)}&tipo={Uri.EscapeDataString(tipo ?? string.Empty)}&status={Uri.EscapeDataString(status ?? string.Empty)}";
            var (data, error, statusCode) = await ReadApiResponse<IEnumerable<ConversaResumoDto>>(client, endpoint);

            model.Conversas = data ?? Array.Empty<ConversaResumoDto>();
            model.Total = model.Conversas.Count();
            model.TotalPages = model.PageSize > 0 ? Math.Max(1, (int)Math.Ceiling(model.Total / (double)model.PageSize)) : 1;

            if (!string.IsNullOrWhiteSpace(error) && statusCode != HttpStatusCode.OK)
            {
                model.ErrorMessage = error;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro inesperado ao listar conversas.");
            model.ErrorMessage = "Não foi possível carregar as conversas neste momento.";
            model.Conversas = Array.Empty<ConversaResumoDto>();
        }

        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var model = new ConversaDetalhesViewModel { Id = id, NovaMensagem = new NovaMensagemViewModel { ConversaId = id } };

        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();

            var (data, error, statusCode) = await ReadApiResponse<ConversaDetalhesViewModel>(client, $"api/comunicacao/conversas/{id}");

            if (data is not null)
            {
                model = data;
                model.NovaMensagem ??= new NovaMensagemViewModel();
                model.NovaMensagem.ConversaId = model.Id == Guid.Empty ? id : model.Id;
                model.Mensagens ??= Array.Empty<MensagemConversaDto>();
                model.Participantes ??= Array.Empty<ParticipanteConversaDto>();
            }

            if (!string.IsNullOrWhiteSpace(error) && statusCode != HttpStatusCode.OK)
            {
                model.ErrorMessage = error;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro inesperado ao carregar conversa {ConversaId}.", id);
            model.ErrorMessage = "Não foi possível carregar os detalhes da conversa.";
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> NovaConversa()
    {
        var model = new NovaConversaViewModel();

        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();

            var (users, error, statusCode) = await ReadApiListResponseAsync<UsuarioConversaOpcaoDto>(client, "api/usuarios/opcoes-conversa");
            model.UsuariosDisponiveis = users.ToList();

            if (!string.IsNullOrWhiteSpace(error) && statusCode != HttpStatusCode.OK)
            {
                model.ErrorMessage = statusCode == HttpStatusCode.NotFound
                    ? "Lista de participantes indisponível no momento."
                    : error;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao preparar tela de nova conversa.");
            model.ErrorMessage = "Não foi possível carregar os participantes neste momento.";
            model.UsuariosDisponiveis = Array.Empty<UsuarioConversaOpcaoDto>();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NovaConversa(NovaConversaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.UsuariosDisponiveis = await ObterUsuariosDisponiveisFallbackSeguro();
            return View(model);
        }

        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();

            var payload = new
            {
                model.Titulo,
                model.Tipo,
                model.Entidade,
                model.EntidadeId,
                Participantes = model.ParticipantesSelecionados.ToArray(),
                MensagemInicial = model.MensagemInicial
            };

            var (id, error, statusCode) = await SendApiAsync<object, Guid>(client, HttpMethod.Post, "api/comunicacao/conversas", payload);
            if (statusCode != HttpStatusCode.OK || id == Guid.Empty)
            {
                TempData["Error"] = string.IsNullOrWhiteSpace(error) ? "Não foi possível criar a conversa." : error;
                model.UsuariosDisponiveis = await ObterUsuariosDisponiveisFallbackSeguro();
                return View(model);
            }

            TempData["Success"] = "Conversa criada com sucesso.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao criar conversa.");
            TempData["Error"] = "Erro inesperado ao criar conversa.";
            model.UsuariosDisponiveis = await ObterUsuariosDisponiveisFallbackSeguro();
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarMensagem(NovaMensagemViewModel model)
    {
        if (!ModelState.IsValid || model.ConversaId == Guid.Empty)
        {
            TempData["Error"] = "Informe uma mensagem válida.";
            return RedirectToAction(nameof(Details), new { id = model.ConversaId });
        }

        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();

            var payload = new { model.Mensagem, Tipo = model.Tipo ?? "TEXTO", model.AnexoUrl };
            var (success, error, _) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, $"api/comunicacao/conversas/{model.ConversaId}/mensagens", payload);

            TempData[success ? "Success" : "Error"] = success
                ? "Mensagem enviada com sucesso."
                : string.IsNullOrWhiteSpace(error) ? "Não foi possível enviar a mensagem." : error;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao enviar mensagem da conversa {ConversaId}", model.ConversaId);
            TempData["Error"] = "Erro inesperado ao enviar mensagem.";
        }

        return RedirectToAction(nameof(Details), new { id = model.ConversaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarLida(Guid id, Guid conversaId)
    {
        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();

            var (success, error, _) = await SendApiWithoutResponseAsync<object?>(client, HttpMethod.Put, $"api/comunicacao/mensagens/{id}/lida", null);
            TempData[success ? "Success" : "Error"] = success
                ? "Mensagem marcada como lida."
                : string.IsNullOrWhiteSpace(error) ? "Não foi possível marcar a mensagem como lida." : error;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao marcar mensagem {MensagemId} como lida", id);
            TempData["Error"] = "Erro inesperado ao atualizar leitura da mensagem.";
        }

        return RedirectToAction(nameof(Details), new { id = conversaId });
    }

    private async Task<IEnumerable<UsuarioConversaOpcaoDto>> ObterUsuariosDisponiveisFallbackSeguro()
    {
        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return Array.Empty<UsuarioConversaOpcaoDto>();
            var (usuarios, _, _) = await ReadApiListResponseAsync<UsuarioConversaOpcaoDto>(client, "api/usuarios/opcoes-conversa");
            return usuarios.ToList();
        }
        catch
        {
            return Array.Empty<UsuarioConversaOpcaoDto>();
        }
    }

    public IActionResult Templates() => View();
}
