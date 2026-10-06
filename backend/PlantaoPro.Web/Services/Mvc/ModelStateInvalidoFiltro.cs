using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace PlantaoPro.Web.Services.Mvc;

/// <summary>
/// Gate financeiro (item 2): no MVC classico o estado de modelo invalido NAO interrompe a
/// acao automaticamente — o campo decimal simplesmente liga como default(0) e o fluxo
/// prossegue em silencio (metade do bug M2.5: alem do parse, a acao ignorava a falha).
/// Este filtro completa o contrato iniciado pelo binder ValorHumano: se qualquer campo
/// falhou no binding (separador ambiguo, fora dos limites ou ausente), a acao NAO executa
/// e NAO chama a API; o usuario volta para a pagina de origem (padrao PRG deste projeto)
/// e ve a mensagem humana em TempData["Error"]. Sem header Referer responde 400 direto.
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

        var referer = context.HttpContext.Request.Headers["Referer"].ToString();
        context.Result = string.IsNullOrWhiteSpace(referer)
            ? new StatusCodeResult(StatusCodes.Status400BadRequest)
            : new RedirectResult(referer);
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
