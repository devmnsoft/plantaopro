using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public class FaturamentoRegrasController : Controller { public IActionResult Index() => View("~/Views/V114/Produto.cshtml", Page("Regras de Faturamento", "Motor v1.15 com filtros, cards, tabela, detalhes, modal, toast, loading, empty state, badges e auditoria/LGPD.", "api/v115/faturamento/regras")); private static V114ProdutoWebPage Page(string t, string s, string e) => new V114ProdutoWebPage(t, s, e); }
[Authorize]
public class RepassesMedicosController : Controller { public IActionResult Index() => View("~/Views/V114/Produto.cshtml", new V114ProdutoWebPage("Repasses Médicos", "Percentual/valor fixo por regra, contestação, resolução e pagamento confirmado sem conteúdo clínico sensível.", "api/v115/repasses-medicos")); }
[Authorize]
public class GlosasController : Controller { public IActionResult Index() => View("~/Views/V114/Produto.cshtml", new V114ProdutoWebPage("Glosas de Convênio", "Glosas por convênio com prazo de recurso, alertas e resolução auditada.", "api/v115/glosas")); }
[Authorize]
public class RecebimentosController : Controller { public IActionResult Index() => View("~/Views/V114/Produto.cshtml", new V114ProdutoWebPage("Recebimentos", "Recebimentos manuais auditados; integrações externas dependem de provedor.", "api/v115/faturamento/contas-receber")); }
[Authorize]
public class ConfiguracoesFinanceirasController : Controller { public IActionResult Index() => View("~/Views/V114/Produto.cshtml", new V114ProdutoWebPage("Configurações Financeiras", "Configuração de itens faturáveis, regras, demo-boleto explícito e dependências de provedor.", "api/v115/faturamento/regras")); }

public partial class FaturamentoClinicoController
{
    public IActionResult Regras() => View("~/Views/V114/Produto.cshtml", new V114ProdutoWebPage("Regras", "Criar e editar regras reais de faturamento v1.15.", "api/v115/faturamento/regras"));
    public IActionResult Recebimentos() => View("~/Views/V114/Produto.cshtml", new V114ProdutoWebPage("Recebimentos", "Recebimentos auditados por tenant e perfil.", "api/v115/faturamento/contas-receber"));
    public IActionResult Configuracoes() => View("~/Views/V114/Produto.cshtml", new V114ProdutoWebPage("Configurações", "Parâmetros financeiros, demo-boleto e provedores externos.", "api/v115/faturamento/regras"));
}
