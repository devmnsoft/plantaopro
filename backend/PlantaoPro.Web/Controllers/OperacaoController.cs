using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public class OperacaoController : Controller
{
    public IActionResult Index()
    {
        return View("~/Views/V114/Produto.cshtml", new V114ProdutoWebPage("Operação Inteligente 3.0", "Central O que fazer agora com Saúde 360, Plantões, Escalas, Faturamento e Outbox.", "api/v114/operacao/central"));
    }
}
