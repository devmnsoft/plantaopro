using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using PlantaoPro.Web.Services.Security;

namespace PlantaoPro.Web.Services.Mvc;

/// <summary>
/// Gate financeiro + destino seguro (R4-A3): se qualquer campo falhou no binding
/// (separador ambiguo, fora dos limites ou ausente), a acao NAO executa e NAO chama a
/// API — o estado de modelo invalido em MVC classico, sem este filtro, simplesmente liga
/// o decimal como default(0) e o fluxo prossegue em silencio (bug M2.5). A mensagem humana
/// vai para TempData["Error"] (mesmo storage que as views leem).
///
/// Destino de retorno NAO confiado ao header Referer cru (open redirect):
/// - BFF (/bff/*): JSON 400 com envelope canonico — consumidores de API nunca recebem redirect HTML.
/// - GET: redirect para a propria URL da requisicao (path+query preservados): a acao nao
///   executou, nao ha efeito no banco, e os filtros/seletores do usuario permanecem na URL.
/// - POST (form MVC): redirect para o Referer apenas se local (mesmo scheme/host/porta da
///   requisicao). Sem Referer, URL invalida ou origem externa → 400 direto (contrato legado).
/// </summary>
public sealed class ModelStateInvalidoFiltro : IActionFilter
{
    private readonly ITempDataDictionaryFactory _tempData;

    public ModelStateInvalidoFiltro(ITempDataDictionaryFactory tempData)
    {
        this._tempData = tempData;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Outro filtro ja decidiu (ex.: guarda SaaS redirecionando para acesso negado).
        if (context.Result is not null) return;
        if (context.ModelState.IsValid) return;

        var mensagem = context.ModelState
            .SelectMany(p => p.Value.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
            ?? "Dados invalidos. Verifique os campos destacados.";

        // Mesmo storage que o controller.TempData das views (padrao casa: TempData["Error"]).
        _tempData.GetTempData(context.HttpContext)["Error"] = mensagem;

        var http = context.HttpContext.Request;

        // BFF/API: JSON com status adequado, nunca redirect para HTML.
        if (BffContracts.IsBffPath(http.Path))
        {
            context.Result = new BffDadosInvalidosResult(mensagem);
            return;
        }

        // GET idempotente: reenvia a mesma URL (filtros/seletores preservados), sem efeito no banco.
        if (HttpMethods.IsGet(http.Method))
        {
            context.Result = new RedirectResult((http.Path + http.QueryString).ToString());
            return;
        }

        // Form MVC: somente redirect para origem local validada; caso contrario 400 direto.
        var destinoLocal = DestinoLocalValidado(http);
        context.Result = destinoLocal is null
            ? new StatusCodeResult(StatusCodes.Status400BadRequest)
            : new RedirectResult(destinoLocal);
    }

    public void OnActionExecuted(ActionExecutedContext context) { }

    /// <summary>
    /// Retorna o path+query do Referer somente quando ele é uma URL absoluta http(s) LOCAL:
    /// mesmo scheme, host (case-insensitive) e porta que a requisição atual. Sem header,
    /// URL relativa/inválida ou origem externa → null. Impede open redirect via Referer
    /// (ex.: header falsificado apontando para site externo ou protocolo não-http).
    /// </summary>
    public static string? DestinoLocalValidado(HttpRequest request)
    {
        var referer = request.Headers["Referer"].ToString();
        if (string.IsNullOrWhiteSpace(referer)) return null;
        if (!Uri.TryCreate(referer, UriKind.Absolute, out var uri)) return null;

        // Em .NET 10 HostString.Value inclui a porta ("localhost:443"); a comparacao de host
        // precisa do nome puro (Host.Host) para nao falsar origem local legitima.
        var hostRequisicao = request.Host.Host ?? request.Host.Value;
        if (string.IsNullOrEmpty(hostRequisicao)) return null;

        if (uri.Scheme != request.Scheme) return null;
        if (!string.Equals(uri.Host, hostRequisicao, StringComparison.OrdinalIgnoreCase)) return null;

        // Em .NET 10 HttpRequest.Port nao existe mais: a porta vem de Host.Port (explicita)
        // ou do default do esquema. Uri.Port aplica a mesma normalizacao (443/80).
        var portaRequisicao = request.Host.Port ?? (string.Equals(request.Scheme, "https", StringComparison.OrdinalIgnoreCase) ? 443 : 80);
        if (uri.Port != portaRequisicao) return null;
        return uri.PathAndQuery;
    }

    /// <summary>Envelope JSON canonico do BFF para dados invalidos (400 + DADOS_INVALIDOS).</summary>
    private sealed class BffDadosInvalidosResult : IActionResult
    {
        private readonly string _mensagem;

        public BffDadosInvalidosResult(string mensagem) => _mensagem = mensagem;

        public Task ExecuteResultAsync(ActionContext actionContext) =>
            BffContracts.RespondAsync(
                actionContext.HttpContext,
                StatusCodes.Status400BadRequest,
                BffContracts.RazaoDadosInvalidos,
                _mensagem);
    }
}
