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

[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE")]
public sealed class WhiteLabelController : Controller
{
    [HttpGet("WhiteLabel")]
    public IActionResult Index() => View("Index", new WhiteLabelWebViewModel());
    public IActionResult Edit() => View("Index", new WhiteLabelWebViewModel());
    public IActionResult Preview() => View("Preview", new WhiteLabelWebViewModel());
    public IActionResult Assets() => View("Assets", new WhiteLabelWebViewModel());
    public IActionResult Emails() => View("Emails", new WhiteLabelWebViewModel());
}

[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE")]
public sealed class PerfisController : BaseWebController
{
    public PerfisController(IHttpClientFactory httpClientFactory, ILogger<PerfisController> logger) : base(httpClientFactory, logger) { }

    [HttpGet("Perfis")]
    public async Task<IActionResult> Index()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (profiles, error, _) = await ReadApiListResponseAsync<PerfilWebViewModel>(client, "api/perfis");
        ViewBag.ErrorMessage = error;
        return View("Index", profiles);
    }

    [HttpGet("Perfis/Create")]
    public IActionResult Create() => View("Form", new PerfilWebViewModel { Status = "ATIVO" });

    [HttpPost("Perfis/Create"), ValidateAntiForgeryToken]
    public Task<IActionResult> Create(PerfilWebViewModel model) => SaveAsync(model, HttpMethod.Post, "api/perfis");

    [HttpGet("Perfis/Edit/{id?}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var profile = await GetProfileAsync(id);
        return profile is null ? RedirectToAction(nameof(Index)) : View("Form", profile);
    }

    [HttpPost("Perfis/Edit/{id:guid}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(Guid id, PerfilWebViewModel model)
    {
        model.Id = id;
        return SaveAsync(model, HttpMethod.Put, "api/perfis/" + id);
    }

    [HttpGet("Perfis/Details/{id?}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var profile = await GetProfileAsync(id);
        return profile is null ? RedirectToAction(nameof(Index)) : View("Details", profile);
    }

    [HttpGet("Perfis/Permissoes/{id?}")]
    public async Task<IActionResult> Permissoes(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var profile = await GetProfileAsync(id, client);
        if (profile is null) return RedirectToAction(nameof(Index));
        var (permissions, error, _) = await ReadApiListResponseAsync<PermissaoWebViewModel>(client, "api/permissoes");
        var (selected, selectedError, _) = await ReadApiListResponseAsync<Guid>(client, $"api/perfis/{id}/permissoes");
        ViewBag.ErrorMessage = error ?? selectedError;
        return View("Permissoes", new PerfilPermissoesWebViewModel { Perfil = profile, Permissoes = permissions, PermissoesSelecionadas = selected.ToArray() });
    }

    [HttpPost("Perfis/Permissoes/{id:guid}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Permissoes(Guid id, Guid[] permissoesSelecionadas)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (_, error, status) = await SendApiAsync<object, string>(client, HttpMethod.Post, $"api/perfis/{id}/permissoes", new { permissoesPermitidas = permissoesSelecionadas ?? Array.Empty<Guid>() });
        TempData[(int)status is >= 200 and <= 299 ? "SuccessMessage" : "ErrorMessage"] = (int)status is >= 200 and <= 299 ? "Permissões atualizadas com sucesso." : error ?? "Não foi possível atualizar as permissões.";
        return RedirectToAction(nameof(Permissoes), new { id });
    }

    [HttpPost("Perfis/Inativar/{id:guid}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Inativar(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (_, error, status) = await SendApiAsync<object, string>(client, HttpMethod.Post, $"api/perfis/{id}/inativar", new { });
        TempData[(int)status is >= 200 and <= 299 ? "SuccessMessage" : "ErrorMessage"] = (int)status is >= 200 and <= 299 ? "Perfil inativado." : error ?? "Não foi possível inativar o perfil.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SaveAsync(PerfilWebViewModel model, HttpMethod method, string endpoint)
    {
        if (!ModelState.IsValid) return View("Form", model);
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (id, error, status) = await SendApiAsync<PerfilWebViewModel, Guid>(client, method, endpoint, model);
        if ((int)status is < 200 or > 299)
        {
            ModelState.AddModelError(string.Empty, error ?? "Não foi possível salvar o perfil.");
            return View("Form", model);
        }
        TempData["SuccessMessage"] = "Perfil salvo com sucesso.";
        return RedirectToAction(nameof(Permissoes), new { id = id == Guid.Empty ? model.Id : id });
    }

    private async Task<PerfilWebViewModel?> GetProfileAsync(Guid id, HttpClient? suppliedClient = null)
    {
        var ownsClient = suppliedClient is null;
        var client = suppliedClient ?? CreateApiClient();
        try
        {
            if (!AddBearerToken(client)) return null;
            var (profile, error, _) = await ReadApiResponseAsync<PerfilWebViewModel>(client, "api/perfis/" + id);
            if (profile is null) TempData["ErrorMessage"] = error ?? "Perfil não encontrado.";
            return profile;
        }
        finally
        {
            if (ownsClient) client.Dispose();
        }
    }
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
