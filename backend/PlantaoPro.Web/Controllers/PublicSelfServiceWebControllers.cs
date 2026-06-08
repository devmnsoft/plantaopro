using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[AllowAnonymous]
[Route("planos")]
public sealed class PlanosPublicosController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View(Planos());

    [HttpGet("comparar")]
    public IActionResult Comparar() => View(Planos());

    [HttpGet("duvidas")]
    public IActionResult Duvidas() => View(Faq());

    internal static IEnumerable<PlanoFaqWebViewModel> Faq()
    {
        return new List<PlanoFaqWebViewModel>
        {
            new PlanoFaqWebViewModel { Pergunta = "Posso começar sem implantação manual?", Resposta = "Sim. O cadastro self-service provisiona tenant, cliente, assinatura, administrador, LGPD, white label padrão e onboarding." },
            new PlanoFaqWebViewModel { Pergunta = "White label está disponível em todos os planos?", Resposta = "White label depende do plano contratado e tem fallback visual seguro." },
            new PlanoFaqWebViewModel { Pergunta = "Como funcionam upgrade e downgrade?", Resposta = "Upgrade registra solicitação comercial; downgrade valida limites atuais antes de prosseguir." }
        };
    }

    internal static IEnumerable<PlanoPublicoWebViewModel> Planos()
    {
        return new List<PlanoPublicoWebViewModel>
        {
            new PlanoPublicoWebViewModel { Nome = "Essencial", Slug = "essencial", Descricao = "Para equipes iniciando a gestão digital de plantões.", ValorMensal = 399, LimiteMedicos = 20, LimiteHospitais = 2, LimitePlantoesMes = 100, LimiteUsuarios = 5, Recursos = new [] { "Área do médico Web", "Notificações internas", "Relatórios básicos", "Suporte padrão" } },
            new PlanoPublicoWebViewModel { Nome = "Profissional", Slug = "profissional", Descricao = "Para operações em crescimento com mobile e relatórios avançados.", ValorMensal = 899, LimiteMedicos = 100, LimiteHospitais = 10, LimitePlantoesMes = 500, LimiteUsuarios = 20, PermiteMobile = true, Destaque = true, Recursos = new [] { "API Mobile", "Relatórios avançados", "Operação Assistida", "Suporte prioritário" } },
            new PlanoPublicoWebViewModel { Nome = "Enterprise", Slug = "enterprise", Descricao = "Para redes com white label, BI, integrações e SLA customizado.", ValorMensal = 1999, LimiteMedicos = 0, LimiteHospitais = 0, LimitePlantoesMes = 0, LimiteUsuarios = 0, PermiteMobile = true, PermiteBi = true, PermiteWhiteLabel = true, Recursos = new [] { "White label", "BI", "Integrações/API", "Customer Success avançado" } },
            new PlanoPublicoWebViewModel { Nome = "Custom", Slug = "custom", Descricao = "Projeto sob medida com implantação assistida completa.", ValorMensal = 0, LimiteMedicos = 0, LimiteHospitais = 0, LimitePlantoesMes = 0, LimiteUsuarios = 0, PermiteMobile = true, PermiteBi = true, PermiteWhiteLabel = true, Recursos = new [] { "Precificação sob proposta", "Integrações específicas", "Contrato personalizado" } }
        };
    }
}

[AllowAnonymous]
[Route("cadastro")]
public sealed class CadastroController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => RedirectToAction(nameof(Empresa));

    [HttpGet("empresa")]
    public IActionResult Empresa() => View("Cadastro", CriarModelo());

    [HttpGet("plano")]
    public IActionResult Plano() => View("Cadastro", CriarModelo());

    [HttpGet("usuario")]
    public IActionResult Usuario() => View("Cadastro", CriarModelo());

    [HttpGet("confirmacao")]
    public IActionResult Confirmacao() => View("Cadastro", CriarModelo());

    [HttpPost("confirmacao")]
    [ValidateAntiForgeryToken]
    public IActionResult Confirmar(CadastroSelfServiceWebViewModel model)
    {
        if (!model.AceiteTermos || !model.AceitePrivacidade)
        {
            ModelState.AddModelError(string.Empty, "Aceite os termos e a política de privacidade para continuar.");
        }
        if (!ModelState.IsValid)
        {
            model.Planos = PlanosPublicosController.Planos();
            TempData["ErrorMessage"] = "Revise os campos obrigatórios.";
            return View("Cadastro", model);
        }
        TempData["SuccessMessage"] = "Cadastro recebido. A API self-service finalizará tenant, cliente, assinatura e usuário administrador.";
        return RedirectToAction(nameof(Sucesso));
    }

    [HttpGet("sucesso")]
    public IActionResult Sucesso() => View();

    private static CadastroSelfServiceWebViewModel CriarModelo()
    {
        return new CadastroSelfServiceWebViewModel { Planos = PlanosPublicosController.Planos(), Periodicidade = "MENSAL", ConsentimentoLgpd = true };
    }
}

[Authorize]
public sealed class WhiteLabelController : Controller
{
    [HttpGet("WhiteLabel")]
    public IActionResult Index() => View("Index", new WhiteLabelWebViewModel());
    public IActionResult Edit() => View("Index", new WhiteLabelWebViewModel());
    public IActionResult Preview() => View("Preview", new WhiteLabelWebViewModel());
    public IActionResult Assets() => View("Assets", new WhiteLabelWebViewModel());
    public IActionResult Emails() => View("Emails", new WhiteLabelWebViewModel());
}

[Authorize]
public sealed class PerfisController : Controller
{
    [HttpGet("Perfis")]
    public IActionResult Index() => View("Index", Perfis());
    [HttpGet("Perfis/Create")]
    public IActionResult Create() => View("Create", new PerfilWebViewModel());
    [HttpGet("Perfis/Edit/{id?}")]
    public IActionResult Edit(Guid? id) => View("Edit", new PerfilWebViewModel { Id = id ?? Guid.Empty });
    [HttpGet("Perfis/Details/{id?}")]
    public IActionResult Details(Guid? id) => View("Details", new PerfilWebViewModel { Id = id ?? Guid.Empty, Nome = "Perfil customizado" });
    [HttpGet("Perfis/Permissoes/{id?}")]
    public IActionResult Permissoes(Guid? id) => View("Permissoes", new PerfilWebViewModel { Id = id ?? Guid.Empty, Nome = "Perfil customizado" });

    private static IEnumerable<PerfilWebViewModel> Perfis() => new []
    {
        new PerfilWebViewModel { Codigo = "ADMINISTRADOR_CLIENTE", Nome = "Administrador cliente", BaseSistema = true, Status = "ATIVO" },
        new PerfilWebViewModel { Codigo = "COORDENADOR", Nome = "Coordenador", BaseSistema = true, Status = "ATIVO" },
        new PerfilWebViewModel { Codigo = "MEDICO", Nome = "Médico", BaseSistema = true, Status = "ATIVO" }
    };
}

[Authorize]
public sealed class ParametrizacoesController : Controller
{
    [HttpGet("Parametrizacoes")]
    public IActionResult Index() => View("Index", Modelo());
    [HttpGet("Parametrizacoes/Operacional")]
    public IActionResult Operacional() => View("Operacional", Modelo());
    [HttpGet("Parametrizacoes/Financeiro")]
    public IActionResult Financeiro() => View("Financeiro", Modelo());
    [HttpGet("Parametrizacoes/Notificacoes")]
    public IActionResult Notificacoes() => View("Notificacoes", Modelo());
    [HttpGet("Parametrizacoes/Lgpd")]
    public IActionResult Lgpd() => View("Lgpd", Modelo());
    [HttpGet("Parametrizacoes/WhiteLabel")]
    public IActionResult WhiteLabel() => View("WhiteLabel", Modelo());

    private static ParametrizacoesWebViewModel Modelo() => new ParametrizacoesWebViewModel
    {
        Operacionais = new Dictionary<string, string> { ["Autoaceite médico"] = "Não", ["Aprovação coordenação"] = "Sim", ["Convite automático"] = "Sim" },
        Financeiras = new Dictionary<string, string> { ["Moeda"] = "BRL", ["Prazo pagamento"] = "30 dias" },
        Notificacoes = new Dictionary<string, string> { ["E-mail"] = "Ativo", ["Sistema"] = "Ativo" },
        Lgpd = new Dictionary<string, string> { ["Versão política"] = "1.0", ["Retenção"] = "5 anos" }
    };
}

[Authorize]
public sealed class MinhaAssinaturaController : Controller
{
    [HttpGet("MinhaAssinatura")]
    public IActionResult Index() => View("Index");
    [HttpGet("MinhaAssinatura/Uso")]
    public IActionResult Uso() => View("Uso");
    [HttpGet("MinhaAssinatura/Upgrade")]
    public IActionResult Upgrade() => View("Upgrade", PlanosPublicosController.Planos());
    [HttpGet("MinhaAssinatura/Downgrade")]
    public IActionResult Downgrade() => View("Downgrade", PlanosPublicosController.Planos());
    [HttpGet("MinhaAssinatura/Faturas")]
    public IActionResult Faturas() => View("Faturas");
    [HttpGet("MinhaAssinatura/Cancelamento")]
    public IActionResult Cancelamento() => View("Cancelamento");
}
