using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Services.Security;

namespace PlantaoPro.Web.Controllers;

/// <summary>
/// B5: convites de equipe do tenant (criar/listar/revogar via API).
/// O token do convite sai UMA vez no TempData após criar; o aceite é público.
/// </summary>
[Route("ConvitesEquipe")]
public sealed class ConvitesEquipeController : BaseWebController
{
    private readonly ICurrentUserService _currentUser;

    public ConvitesEquipeController(IHttpClientFactory factory, ILogger<ConvitesEquipeController> logger,
        ICurrentUserService currentUser)
        : base(factory, logger)
    {
        _currentUser = currentUser;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        // R5-A3: convites pertencem ao cliente (tenant). A área global sem cliente ativo
        // não tem equipe própria; consultar a API escopada só gerava 403 confuso misturado
        // com "Nenhum convite registrado". Entrega leitura honesta: explica e não chama.
        if (_currentUser.IsGlobalAdmin() && !_currentUser.TenantId.HasValue)
        {
            return View("~/Views/ConvitesEquipe/Index.cshtml", new ConvitesEquipeIndexViewModel
            {
                SemContextoCliente = true,
                InfoMessage = "Convites de equipe pertencem a um cliente. Selecione um cliente em AdminSaaS para criar ou revisar convites da equipe.",
                TokenCriado = TempData["ConviteToken"] as string,
                EmailConvidado = TempData["ConviteEmail"] as string,
            });
        }

        var convites = await ReadApiListResponseAsync<ConviteEquipeWebViewModel>(client, "api/equipe/convites");
        if (convites.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        var perfis = await ReadApiListResponseAsync<PerfilOpcaoUsuarioViewModel>(client, "api/seguranca/perfis-atribuiveis");

        return View("~/Views/ConvitesEquipe/Index.cshtml", new ConvitesEquipeIndexViewModel
        {
            Convites = convites.Data.ToArray(),
            Perfis = perfis.Data,
            TokenCriado = TempData["ConviteToken"] as string,
            EmailConvidado = TempData["ConviteEmail"] as string,
            ErrorMessage = convites.Error ?? perfis.Error
        });
    }

    [HttpPost("Criar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar([FromForm] string? email, [FromForm] Guid[]? perfilIds, [FromForm] int? diasValidade)
    {
        if (string.IsNullOrWhiteSpace(email) || perfilIds is null || perfilIds.Length == 0)
        {
            TempData["Error"] = "Informe o e-mail e ao menos um perfil.";
            return RedirectToAction(nameof(Index));
        }

        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var (criado, _, status) = await SendApiAsync<CriarConviteRequest, ConviteCriadoResposta>(
            client, HttpMethod.Post, "api/equipe/convites",
            new CriarConviteRequest(email.Trim(), perfilIds, diasValidade));
        if (status == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (criado?.Token is null)
        {
            // Falha HTTP: o helper já gravou TempData["Error"] com a mensagem real.
            TempData["Error"] ??= "Não foi possível criar o convite.";
            return RedirectToAction(nameof(Index));
        }

        TempData["ConviteToken"] = criado.Token;
        TempData["ConviteEmail"] = email.Trim();
        TempData["Success"] = "Convite criado. Compartilhe o link uma única vez — o token não será exibido de novo.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Revogar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revogar(Guid id)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var (_, _, status) = await SendApiAsync<object, string>(
            client, HttpMethod.Post, $"api/equipe/convites/{id}/revogar", new { });
        if (status == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        // Falha HTTP: o helper já gravou TempData["Error"] com a mensagem real.
        if (status is >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous)
            TempData["Success"] = "Convite revogado.";
        return RedirectToAction(nameof(Index));
    }

    private sealed record CriarConviteRequest(string Email, Guid[] PerfilIds, int? DiasValidade);
    private sealed record ConviteCriadoResposta(Guid Id, string Email, Guid[] PerfilIds, string[] PerfilNomes, DateTime ExpiraEm, DateTime? UsadoEm, string Estado, DateTime CriadoEm, string? Token);
}

/// <summary>B5: aceite público do convite (link com token, anônimo).</summary>
[AllowAnonymous]
[Route("convite")]
public sealed class ConvitePublicoWebController : Controller
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<ConvitePublicoWebController> _logger;

    public ConvitePublicoWebController(IHttpClientFactory factory, ILogger<ConvitePublicoWebController> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    [HttpGet("aceitar/{token}")]
    public async Task<IActionResult> Aceitar(string token)
    {
        var model = await ValidarAsync(token);
        if (model is null) return View("~/Views/Convite/Aceitar.cshtml", new ConviteAceiteWebViewModel { Token = token, ErrorMessage = "Não foi possível validar o convite agora." });
        return View("~/Views/Convite/Aceitar.cshtml", model);
    }

    [HttpPost("aceitar/{token}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarAceite(string token, [FromForm] string? nome, [FromForm] string? senha, [FromForm] string? telefone)
    {
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(senha))
        {
            var atual = await ValidarAsync(token);
            atual ??= new ConviteAceiteWebViewModel { Token = token };
            atual.ErrorMessage = "Informe nome e senha para concluir.";
            return View("~/Views/Convite/Aceitar.cshtml", atual);
        }

        try
        {
            var client = _factory.CreateClient("PlantaoProApi");
            using var response = await client.PostAsJsonAsync($"api/public/convites/{token}/aceitar", new { nome, senha, telefone });
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Conta criada. Faça login para continuar.";
                return RedirectToAction("Login", "Account");
            }
            var atual = await ValidarAsync(token);
            atual ??= new ConviteAceiteWebViewModel { Token = token };
            atual.ErrorMessage = "Não foi possível concluir o aceite. O convite pode ter expirado ou sido utilizado.";
            return View("~/Views/Convite/Aceitar.cshtml", atual);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha de comunicação no aceite do convite.");
            return View("~/Views/Convite/Aceitar.cshtml", new ConviteAceiteWebViewModel { Token = token, ErrorMessage = "Não foi possível concluir agora. Tente novamente." });
        }
    }

    private async Task<ConviteAceiteWebViewModel?> ValidarAsync(string token)
    {
        try
        {
            var client = _factory.CreateClient("PlantaoProApi");
            using var response = await client.GetAsync($"api/public/convites/{token}");
            if (!response.IsSuccessStatusCode) return new ConviteAceiteWebViewModel { Token = token, Motivo = "Convite inválido." };
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            var payload = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var data) && data.ValueKind != JsonValueKind.Null
                ? data.GetRawText()
                : root.GetRawText();
            var model = JsonSerializer.Deserialize<ConviteAceiteWebViewModel>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (model is null) return null;
            model.Token = token;
            return model;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Convite público indisponível.");
            return null;
        }
    }
}
