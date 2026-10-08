using System.Net;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Route("MinhaAssinatura")]
public sealed class MinhaAssinaturaController : BaseWebController
{
    public MinhaAssinaturaController(IHttpClientFactory factory, ILogger<MinhaAssinaturaController> logger)
        : base(factory, logger) { }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var result = await ReadApiResponseAsync<AssinaturaDto>(client, "api/minha-assinatura");
        if (result.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();

        var model = new MinhaAssinaturaViewModel();
        if (result.StatusCode == HttpStatusCode.Forbidden)
            model.ErrorMessage = "Você não tem permissão para consultar os dados da assinatura.";
        else if (result.StatusCode == HttpStatusCode.NotFound)
            model.ErrorMessage = null;
        else if (result.Data is not null)
        {
            model.Plano = result.Data.PlanoNome;
            model.Status = result.Data.Status;
            model.Vencimento = result.Data.DataFim.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(result.Data.DataFim.Value, DateTimeKind.Utc)) : null;
        }
        else if (!string.IsNullOrWhiteSpace(result.Error))
            model.ErrorMessage = result.Error;

        var solicitacoes = await ReadApiListResponseAsync<MinhaSolicitacaoPlanoWebViewModel>(client, "api/minha-assinatura/solicitacoes");
        model.Solicitacoes = solicitacoes.Data.ToArray();

        return View(model);
    }

    [HttpGet("Uso")]
    public async Task<IActionResult> Uso()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var result = await ReadApiResponseAsync<UsoPlanoWebViewModel>(client, "api/minha-assinatura/uso");
        if (result.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();

        return View("Uso", result.Data ?? new UsoPlanoWebViewModel { ErrorMessage = result.Error ?? "Não foi possível carregar o uso do plano." });
    }

    [HttpGet("Modulos")]
    public async Task<IActionResult> Modulos()
    {
        var model = new ClientModulesPageViewModel();
        await PopulateModulesAsync(model);
        return View("Modulos", model);
    }

    [HttpPost("Modulos/Revisar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevisarModulos(ClientModuleReviewInput input)
    {
        var model = new ClientModulesPageViewModel();
        if (!ModelState.IsValid)
        {
            model.ErrorMessage = "Selecione ao menos um módulo disponível para revisar.";
            await PopulateModulesAsync(model);
            return View("Modulos", model);
        }

        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await SendApiAsync<ClientModuleReviewInput, ClientModuleReviewViewModel>(client, HttpMethod.Post, "api/portal-cliente/modulos/revisao", input);
        if (result.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        model.Revisao = result.Data;
        if (model.Revisao is not null) model.Revisao.IdempotencyKey = Guid.NewGuid().ToString("N");
        else model.ErrorMessage = result.Error ?? "Não foi possível revisar as condições comerciais.";
        await PopulateModulesAsync(model);
        return View("Modulos", model);
    }

    [HttpPost("Modulos/Confirmar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarModulos(ClientModuleConfirmInput input)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "A revisão expirou ou está incompleta. Revise a oferta novamente."; return RedirectToAction(nameof(Modulos)); }
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await SendApiAsync<ClientModuleConfirmInput, ClientModuleRequestViewModel>(client, HttpMethod.Post, "api/portal-cliente/modulos/solicitacoes", input);
        if (result.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (result.Data is null)
        {
            TempData["Error"] = result.StatusCode == HttpStatusCode.Conflict
                ? "As condições mudaram ou a solicitação já foi tratada. Revise a oferta atual antes de confirmar."
                : result.Error ?? "Não foi possível registrar a solicitação.";
        }
        else TempData["Success"] = $"Solicitação {result.Data.Protocolo} registrada para análise. O módulo ainda não foi ativado.";
        return RedirectToAction(nameof(Modulos));
    }

    [HttpPost("Modulos/Solicitacoes/{id:guid}/Cancelar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarSolicitacaoModulo(Guid id, string justificativa)
    {
        if (string.IsNullOrWhiteSpace(justificativa)) { TempData["Error"] = "Informe o motivo do cancelamento."; return RedirectToAction(nameof(Modulos)); }
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await SendApiAsync<object, object>(client, HttpMethod.Post, $"api/portal-cliente/modulos/solicitacoes/{id}/cancelar", new { Justificativa = justificativa, ConditionsVersion = string.Empty });
        TempData[result.Data is null ? "Error" : "Success"] = result.Data is null ? result.Error ?? "Não foi possível cancelar a solicitação." : "Solicitação cancelada. Nenhum dado operacional foi removido.";
        return RedirectToAction(nameof(Modulos));
    }

    private async Task PopulateModulesAsync(ClientModulesPageViewModel model)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) { model.ErrorMessage ??= "Sessão expirada."; return; }
        var catalog = await ReadApiListResponseAsync<ClientModuleViewModel>(client, "api/portal-cliente/modulos/catalogo");
        var requests = await ReadApiListResponseAsync<ClientModuleRequestViewModel>(client, "api/portal-cliente/modulos/solicitacoes");
        model.Catalogo = catalog.Data.ToArray();
        model.Solicitacoes = requests.Data.ToArray();
        model.ErrorMessage ??= catalog.Error ?? requests.Error;
    }

    [HttpGet("Limites")]
    public async Task<IActionResult> Limites()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var result = await ReadApiResponseAsync<UsoPlanoWebViewModel>(client, "api/minha-assinatura/uso");
        if (result.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();

        return View("Limites", result.Data ?? new UsoPlanoWebViewModel { ErrorMessage = result.Error ?? "Não foi possível carregar os limites do plano." });
    }

    [HttpGet("Upgrade")]
    public async Task<IActionResult> Upgrade() => View("Upgrade", await UpgradeDowngradeAsync("UPGRADE"));

    [HttpGet("Downgrade")]
    public async Task<IActionResult> Downgrade() => View("Downgrade", await UpgradeDowngradeAsync("DOWNGRADE"));

    [HttpPost("SolicitarUpgrade")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SolicitarUpgrade(Guid planoDestinoId, [FromForm] string? motivo) =>
        await SolicitarMudancaAsync("upgrade", planoDestinoId, motivo);

    [HttpPost("SolicitarDowngrade")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SolicitarDowngrade(Guid planoDestinoId, [FromForm] string? motivo) =>
        await SolicitarMudancaAsync("downgrade", planoDestinoId, motivo);

    [HttpGet("Faturas")]
    public async Task<IActionResult> Faturas()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var result = await ReadApiListResponseAsync<FaturaDto>(client, "api/minha-assinatura/faturas");
        if (result.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();

        var model = new FaturasAssinaturaWebViewModel
        {
            Faturas = result.Data.Select(f => new AssinaturaCobrancaViewModel
            {
                Data = new DateTimeOffset(DateTime.SpecifyKind(f.Vencimento, DateTimeKind.Utc)),
                Status = f.Status,
                Valor = f.Valor.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"))
            }).ToArray(),
            ErrorMessage = result.Error
        };
        return View("Faturas", model);
    }

    [HttpGet("Cancelamento")]
    public IActionResult Cancelamento() => View("Cancelamento");

    [HttpPost("SolicitarCancelamento")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SolicitarCancelamento([FromForm] string? motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length < 10)
        {
            TempData["Error"] = "Informe um motivo com pelo menos 10 caracteres.";
            return RedirectToAction(nameof(Cancelamento));
        }

        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var result = await SendApiAsync<SolicitarCancelamentoRequest, string>(
            client, HttpMethod.Post, "api/minha-assinatura/solicitar-cancelamento",
            new SolicitarCancelamentoRequest(motivo.Trim()));
        if (result.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (result.Data is null)
        {
            TempData["Error"] = result.Error ?? "Não foi possível registrar a solicitação.";
            return RedirectToAction(nameof(Cancelamento));
        }

        TempData["Success"] = "Solicitação registrada para avaliação comercial. Seu acesso continua ativo — nada foi encerrado automaticamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<UpgradeDowngradeWebViewModel> UpgradeDowngradeAsync(string acao)
    {
        var model = new UpgradeDowngradeWebViewModel { Acao = acao };
        var client = CreateApiClient();
        if (!AddBearerToken(client))
        {
            model.ErrorMessage = "Sessão expirada. Faça login novamente.";
            return model;
        }

        var assinatura = await ReadApiResponseAsync<AssinaturaDto>(client, "api/minha-assinatura");
        if (assinatura.StatusCode == HttpStatusCode.Unauthorized)
        {
            model.ErrorMessage = "Sessão expirada. Faça login novamente.";
            return model;
        }
        if (assinatura.Data is not null)
        {
            model.AssinaturaPlanoId = assinatura.Data.PlanoId;
            model.AssinaturaPlanoNome = assinatura.Data.PlanoNome;
        }

        var planos = await ReadApiListResponseAsync<PlanoPublicoWebViewModel>(client, "api/planos/publicos");
        model.Planos = planos.Data.ToArray();
        var solicitacoes = await ReadApiListResponseAsync<MinhaSolicitacaoPlanoWebViewModel>(client, "api/minha-assinatura/solicitacoes");
        model.Solicitacoes = solicitacoes.Data
            .Where(s => string.Equals(s.Status, "SOLICITADO", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s.Status, "CANCELAMENTO_SOLICITADO", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        model.ErrorMessage = planos.Error ?? solicitacoes.Error
            ?? (model.Planos.Count == 0 ? "Catálogo de planos indisponível no momento." : null);
        return model;
    }

    private async Task<IActionResult> SolicitarMudancaAsync(string tipo, Guid planoDestinoId, string? motivo)
    {
        var destino = string.Equals(tipo, "downgrade", StringComparison.OrdinalIgnoreCase) ? nameof(Downgrade) : nameof(Upgrade);
        if (planoDestinoId == Guid.Empty)
        {
            TempData["Error"] = "Selecione o plano destino.";
            return RedirectToAction(destino);
        }

        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var result = await SendApiAsync<SolicitarMudancaPlanoRequest, string>(
            client, HttpMethod.Post, $"api/minha-assinatura/solicitar-{tipo}",
            new SolicitarMudancaPlanoRequest(planoDestinoId, motivo?.Trim() ?? string.Empty));
        if (result.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (result.Data is null)
        {
            TempData["Error"] = result.Error ?? "Não foi possível registrar a solicitação.";
            return RedirectToAction(destino);
        }

        TempData["Success"] = "Solicitação registrada para avaliação comercial. Nada mudou no seu plano.";
        return RedirectToAction(nameof(Index));
    }

    private sealed record AssinaturaDto(Guid AssinaturaId, Guid ClienteId, Guid PlanoId, string PlanoNome, string Status, decimal ValorContratado, DateTime DataInicio, DateTime? DataFim);
    private sealed record FaturaDto(Guid Id, Guid ClienteId, Guid AssinaturaId, DateTime Vencimento, decimal Valor, string Status, string Descricao);
    private sealed record SolicitarMudancaPlanoRequest(Guid PlanoDestinoId, string Motivo);
    private sealed record SolicitarCancelamentoRequest(string Motivo);
}
