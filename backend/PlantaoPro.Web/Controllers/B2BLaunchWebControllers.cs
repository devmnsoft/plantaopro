using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class DeveloperController : Controller
{
    public IActionResult Index() => View("~/Views/B2BLaunch/Index.cshtml", Pagina("Developer Portal", "Documentação, API keys, webhooks, limites e exemplos para integrações B2B.", "ApiKeys"));
    public IActionResult Autenticacao() => View("~/Views/B2BLaunch/Index.cshtml", Pagina("Autenticação da API", "JWT, X-Api-Key, escopos e erros amigáveis.", "ApiKeys"));
    public IActionResult Endpoints() => View("~/Views/B2BLaunch/Index.cshtml", Pagina("Endpoints liberados", "Contratos de API disponíveis por plano e módulo.", "ApiKeys"));
    public IActionResult Webhooks() => View("~/Views/B2BLaunch/Index.cshtml", Pagina("Webhooks", "Eventos configuráveis para plantões, escalas, pagamentos e suporte.", "ApiKeys"));
    public IActionResult ApiKeys() => View("~/Views/B2BLaunch/Form.cshtml", Pagina("API keys", "Criação e revogação auditadas; chave exibida apenas uma vez.", "ApiKeys"));
    public IActionResult RateLimits() => View("~/Views/B2BLaunch/Index.cshtml", Pagina("Rate limits", "Limites por tenant, plano, chave e escopo.", "ApiKeys"));
    public IActionResult Exemplos() => View("~/Views/B2BLaunch/Index.cshtml", Pagina("Exemplos", "Curl, respostas, erros comuns e guia de integração.", "ApiKeys"));
    public IActionResult Uso() => View("~/Views/B2BLaunch/Index.cshtml", Pagina("Uso da API", "Consumo mensal, chamadas, erros e bloqueios por limite.", "ApiKeys"));
    private static B2BLaunchPageViewModel Pagina(string titulo, string subtitulo, string acao) => B2BLaunchPages.Pagina(titulo, subtitulo, "Developer", acao);
}

[Authorize]
public sealed class ContratosController : Controller
{
    public IActionResult Index() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Contratos", "Plano, vigência, valores, setup, SLA e propriedade dos dados.", "Contratos", "Create"));
    public IActionResult Create() => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Novo contrato", "Contrato auditável com aceite digital e SLA contratado.", "Contratos", "Index"));
    public IActionResult Edit(Guid id) => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Editar contrato", "Alteração com histórico e auditoria.", "Contratos", "Index"));
    public IActionResult Details(Guid id) => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Detalhes do contrato", "Itens, aceites, anexos e renovações.", "Contratos", "Sla"));
    public IActionResult Sla(Guid? id) => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("SLA contratado", "Uptime, suporte, resolução crítica, manutenção, backup e exportação.", "Sla", "Indicadores"));
    public IActionResult Aceites(Guid? id) => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Aceites digitais", "Termos versionados, LGPD e histórico de aceite.", "Contratos", "Index"));
}

[Authorize]
public sealed class SlaController : Controller
{
    public IActionResult Index() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("SLA", "Indicadores, incidentes, resposta e resolução por severidade.", "Sla", "Incidentes"));
    public IActionResult Incidentes() => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Incidentes de SLA", "Abertura/resolução auditada e alerta em severidade crítica.", "Sla", "Indicadores"));
    public IActionResult Indicadores() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Indicadores SLA", "Tempo médio de resposta, resolução, uptime e violações.", "Sla", "Incidentes"));
}

[Authorize]
public sealed class BetaController : Controller
{
    public IActionResult Index() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Soft launch beta", "Programas, clientes piloto, feedback, bugs e indicadores.", "Beta", "Feedbacks"));
    public IActionResult Programas() => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Programas beta", "Seleção de empresas parceiras e desconto vitalício configurável.", "Beta", "Clientes"));
    public IActionResult Clientes() => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Clientes beta", "Acompanhamento de implantação e satisfação.", "Beta", "Feedbacks"));
    public IActionResult Feedbacks() => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Feedbacks beta", "Severidade, classificação, resolução e conversão em tarefa interna.", "Beta", "Indicadores"));
    public IActionResult Indicadores() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Indicadores beta", "Abertos, críticos, resolvidos, resposta média e satisfação.", "Beta", "Feedbacks"));
}

[Authorize]
public sealed class SuporteController : Controller
{
    public IActionResult Index() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Suporte B2B", "Empresa contratante aciona MNSOFT; usuários finais acionam admin cliente quando habilitado.", "Suporte", "Create"));
    public IActionResult Create() => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Abrir chamado", "Prioridade vinculada ao plano/SLA e auditoria de cada interação.", "Suporte", "MeusChamados"));
    public IActionResult Details(Guid id) => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Detalhes do chamado", "Respostas, escalonamento, resolução e cálculo de SLA.", "Suporte", "Sla"));
    public IActionResult BaseConhecimento() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Base de conhecimento", "Artigos por perfil, plano e liberação para parceiro.", "Suporte", "Index"));
    public IActionResult Sla() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("SLA do suporte", "Prazos por prioridade e canal de atendimento.", "Suporte", "Create"));
    public IActionResult MeusChamados() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Meus chamados", "Chamados do tenant atual sem expor outros clientes.", "Suporte", "Create"));
}

[Authorize]
public sealed class MonitoramentoController : Controller
{
    public IActionResult Index() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Monitoramento ativo", "Healthchecks, tenants, performance, erros e alertas.", "Monitoramento", "Alertas"));
    public IActionResult Tenants() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Saúde dos tenants", "Uso, queda de login, risco e faturas vencidas.", "Monitoramento", "HealthChecks"));
    public IActionResult Performance() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Performance", "Tempo médio, endpoints lentos e telemetria por rota.", "Monitoramento", "Erros"));
    public IActionResult Erros() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Erros críticos", "Erros 500, 401/403, login e integrações sem payload sensível.", "Monitoramento", "Alertas"));
    public IActionResult Alertas() => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Alertas", "Reconhecimento e resolução de alertas críticos pelo admin global.", "Monitoramento", "Index"));
    public IActionResult HealthChecks() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Healthchecks", "Monitoramento ativo que não derruba a operação em caso de falha.", "Monitoramento", "Performance"));
}

[Authorize]
public sealed class GoToMarketController : Controller
{
    public IActionResult Index() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Go-to-market B2B", "Casos de uso, materiais, campanhas, parceiros e decisores.", "GoToMarket", "Materiais"));
    public IActionResult CasosUso() => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Casos de uso", "Hospitais, empresas de gestão, revenda e operação assistida.", "GoToMarket", "Materiais"));
    public IActionResult Materiais() => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Materiais comerciais", "One page, roteiro de demo, proposta, FAQ e argumentários.", "GoToMarket", "Campanhas"));
    public IActionResult Campanhas() => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Campanhas", "E-mail, LinkedIn e cadências para lançamento B2B.", "GoToMarket", "Decisores"));
    public IActionResult Decisores() => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Decisores", "CTO, diretor de negócios, coordenação médica e parceiros.", "GoToMarket", "Index"));
}

[Authorize]
public sealed class OperacionalLaunchController : Controller
{
    public IActionResult Index() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Evolução operacional", "Central de escala, agenda médica, disponibilidade, substituição e relatórios.", "OperacionalLaunch", "Index"));
}

internal static class B2BLaunchPages
{
    public static B2BLaunchPageViewModel Pagina(string titulo, string subtitulo, string controller, string acao)
    {
        return new B2BLaunchPageViewModel
        {
            Titulo = titulo,
            Subtitulo = subtitulo,
            Cards = new[]
            {
                new B2BLaunchCardViewModel { Codigo = "TENANT", Titulo = "Isolamento multi-tenant", Descricao = "Dados filtrados por tenant/cliente, com auditoria de acesso cruzado.", Status = "Ativo", Controller = controller, Acao = acao },
                new B2BLaunchCardViewModel { Codigo = "PLANO", Titulo = "Limites por plano", Descricao = "Módulos, API, suporte, armazenamento e SLA seguem o plano contratado.", Status = "Parametrizado", Controller = controller, Acao = acao },
                new B2BLaunchCardViewModel { Codigo = "AUDIT", Titulo = "Auditoria e LGPD", Descricao = "Ações sensíveis registram histórico e evitam exposição de dados sensíveis.", Status = "Auditável", Controller = controller, Acao = acao }
            }
        };
    }
}
