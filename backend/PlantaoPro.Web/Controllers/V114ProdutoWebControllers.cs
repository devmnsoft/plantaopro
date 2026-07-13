using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public class FaturamentoClinicoController : Controller
{
    public IActionResult Index() => View("~/Views/V114/Produto.cshtml", V114Page("Faturamento Clínico", "Contas, títulos, repasses, glosas e demo-boleto explícito.", "api/v114/faturamento/contas-receber"));
    public IActionResult ContasReceber() => View("~/Views/V114/Produto.cshtml", V114Page("Contas a Receber", "Cobrança mínima sem evolução clínica sensível.", "api/v114/faturamento/contas-receber"));
    public IActionResult Titulos() => View("~/Views/V114/Produto.cshtml", V114Page("Títulos", "Títulos financeiros com demo-boleto demonstrativo.", "api/v114/faturamento/titulos"));
    public IActionResult RepassesMedicos() => View("~/Views/V114/Produto.cshtml", V114Page("Repasses Médicos", "Repasses por plantão realizado e atendimento faturado.", "api/v114/faturamento/repasses-medicos"));
    public IActionResult Glosas() => View("~/Views/V114/Produto.cshtml", V114Page("Glosas", "Registro e acompanhamento de glosas por convênio.", "api/v114/faturamento/glosas"));
    public IActionResult DemoBoleto() => View("~/Views/V114/Produto.cshtml", V114Page("Demo Boleto", "Boleto demonstrativo: não emite cobrança real.", "api/v114/faturamento/titulos"));
    private static V114ProdutoWebPage V114Page(string title, string subtitle, string endpoint) => new V114ProdutoWebPage(title, subtitle, endpoint);
}

[Authorize]
public class ItensFaturaveisController : Controller
{
    public IActionResult Index() => View("~/Views/V114/Produto.cshtml", Page("Itens Faturáveis", "Serviços, procedimentos, plantões, taxas, repasses, convênios e pacotes.", "api/v114/itens-faturaveis"));
    public IActionResult Create() => View("~/Views/V114/Form.cshtml", Page("Novo Item Faturável", "Cadastro auditado por tenant/perfil.", "api/v114/itens-faturaveis"));
    public IActionResult Edit(Guid id) => View("~/Views/V114/Form.cshtml", Page("Editar Item Faturável", "Atualização auditada sem boleto real.", "api/v114/itens-faturaveis/" + id));
    public IActionResult Details(Guid id) => View("~/Views/V114/Produto.cshtml", Page("Detalhes do Item Faturável", "Uso em faturamento clínico e repasses.", "api/v114/itens-faturaveis?id=" + id));
    private static V114ProdutoWebPage Page(string title, string subtitle, string endpoint) => new V114ProdutoWebPage(title, subtitle, endpoint);
}

[Authorize]
public class JornadaController : Controller
{
    public IActionResult Index() => View("~/Views/V114/Produto.cshtml", new V114ProdutoWebPage("Jornada por Perfil", "Recepção, triagem, médico, financeiro, coordenação e admin cliente.", "api/v114/jornada/proximas-acoes"));
}

[Authorize]
public class TemplatesOperacionaisController : Controller
{
    public IActionResult Index() => View("~/Views/V114/Produto.cshtml", new V114ProdutoWebPage("Templates Operacionais", "Modelos de operação, prescrição, faturamento e jornada.", "api/v114/templates-operacionais"));
}

[Authorize]
public class FavoritosController : Controller
{
    public IActionResult Index() => View("~/Views/V114/Produto.cshtml", new V114ProdutoWebPage("Favoritos e Atalhos", "Favoritos por usuário, últimos acessados e atalhos configuráveis.", "api/v114/jornada/proximas-acoes"));
}

[Authorize]
public class HistoricoAcoesController : Controller
{
    public IActionResult Index(string? entidade, Guid? id) => View("~/Views/V114/Produto.cshtml", new V114ProdutoWebPage("Histórico de Ações", "Timeline de paciente, atendimento, plantão e financeiro com auditoria.", "api/v114/operacao/atividades"));
}

public sealed record V114ProdutoWebPage(string Title, string Subtitle, string Endpoint);
