using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using System.Net;

namespace PlantaoPro.Web.Controllers;

[Authorize]
[Route("MeuDia")]
public sealed class MeuDiaController : BaseWebController
{
    public MeuDiaController(IHttpClientFactory httpClientFactory, ILogger<MeuDiaController> logger) : base(httpClientFactory, logger) { }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Meu Dia";
        var fallback = MeuDiaViewModel.Empty(Saudacao(), Contexto());
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (data, error, statusCode) = await ReadApiResponseAsync<MeuDiaViewModel>(client, "api/meu-dia");
        if (statusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (statusCode == HttpStatusCode.Forbidden) return View(fallback with { ErrorMessage = "Você não possui permissão para visualizar o Meu Dia." });
        return View(data is null ? fallback with { ErrorMessage = error } : data with { Saudacao = Saudacao(), Contexto = Contexto(), ErrorMessage = error });
    }

    [HttpPost("{id:guid}/concluir")]
    public async Task<IActionResult> Concluir(Guid id) => await AlterarEstado(id, "concluir");
    [HttpPost("{id:guid}/adiar")]
    public async Task<IActionResult> Adiar(Guid id) => await AlterarEstado(id, "adiar");
    [HttpPost("{id:guid}/reabrir")]
    public async Task<IActionResult> Reabrir(Guid id) => await AlterarEstado(id, "reabrir");

    private async Task<IActionResult> AlterarEstado(Guid id, string action)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        await SendApiWithoutResponseAsync(client, HttpMethod.Post, $"api/meu-dia/{id}/{action}", new { motivo = "Alterado pela página Meu Dia", novaData = (DateTime?)null });
        return RedirectToAction(nameof(Index));
    }

    private string Saudacao(){var h=DateTime.Now.Hour; var periodo=h<12?"Bom dia":h<18?"Boa tarde":"Boa noite"; var nome=(User.Identity?.Name??"usuário").Split(' ',StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()??"usuário"; return $"{periodo}, {nome}";}
    private string Contexto()=>User.FindFirst("tenant")?.Value ?? User.FindFirst("cliente")?.Value ?? "Contexto global";
}
