using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class OperacaoInteligenteController : BaseWebController
{
    private readonly IConfiguration configuration;

    public OperacaoInteligenteController(IHttpClientFactory factory, ILogger<OperacaoInteligenteController> logger, IConfiguration configuration) : base(factory, logger)
    {
        this.configuration = configuration;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();

            var result = await ReadApiResponseAsync<OperacaoInteligenteResumoApiDto>(client, "api/operacao-inteligente/resumo");
            if (result.Data is not null)
            {
                return View(Mapear(result.Data));
            }

            if (configuration.GetValue<bool>("DemoMode"))
            {
                TempData["Warning"] = "API indisponível; exibindo modo demonstração explicitamente habilitado.";
                return View(OperacaoInteligenteViewModel.Demo());
            }

            TempData["Error"] = result.Error ?? "Não foi possível carregar a operação inteligente agora.";
            return View(OperacaoInteligenteViewModel.Empty());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao carregar cockpit de operação inteligente.");
            TempData["Error"] = "Não foi possível carregar a operação inteligente agora.";
            return View(OperacaoInteligenteViewModel.Empty());
        }
    }

    private static OperacaoInteligenteViewModel Mapear(OperacaoInteligenteResumoApiDto dto)
    {
        var model = OperacaoInteligenteViewModel.Empty();
        model.Subtitulo = string.IsNullOrWhiteSpace(dto.MensagemGuia) ? model.Subtitulo : dto.MensagemGuia;
        foreach (var item in dto.Pendencias ?? Array.Empty<OperacaoPendenciaApiDto>())
        {
            model.Pendencias.Add(new OperacaoPendenciaViewModel(item.Titulo, item.Motivo, item.Prioridade, item.PerfilResponsavel, item.Cta, item.UrlOrigem, item.Status));
        }
        foreach (var item in dto.Recomendacoes ?? Array.Empty<OperacaoRecomendacaoApiDto>())
        {
            model.Recomendacoes.Add(new OperacaoRecomendacaoViewModel(item.Severidade, item.PerfilAlvo, item.Modulo, item.Mensagem, item.AcaoSugerida, item.UrlAcao));
        }
        return model;
    }
}
