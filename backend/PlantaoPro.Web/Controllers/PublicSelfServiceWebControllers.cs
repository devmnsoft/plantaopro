using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

/// <summary>
/// B4: catálogo público lido da API (api/public/planos + faq) — preços, limites
/// e recursos sempre do banco, nunca inventados. Em falha, lista vazia honesta
/// (as views mostram "indisponível") em vez de tabela estática com valores.
/// </summary>
[AllowAnonymous]
[Route("planos")]
public sealed class PlanosPublicosController : Controller
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<PlanosPublicosController> _logger;

    public PlanosPublicosController(IHttpClientFactory factory, ILogger<PlanosPublicosController> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await PlanosAsync());

    [HttpGet("comparar")]
    public async Task<IActionResult> Comparar() => View(await PlanosAsync());

    [HttpGet("duvidas")]
    public async Task<IActionResult> Duvidas() => View(await FaqAsync());

    internal async Task<IEnumerable<PlanoPublicoWebViewModel>> PlanosAsync() =>
        await GetAsync<List<PlanoPublicoWebViewModel>>("api/public/planos") ?? new List<PlanoPublicoWebViewModel>();

    internal async Task<IEnumerable<PlanoFaqWebViewModel>> FaqAsync() =>
        await GetAsync<List<PlanoFaqWebViewModel>>("api/public/planos/faq") ?? new List<PlanoFaqWebViewModel>();

    internal static IEnumerable<PlanoPublicoWebViewModel> Planos() => Enumerable.Empty<PlanoPublicoWebViewModel>();

    internal static IEnumerable<PlanoFaqWebViewModel> Faq() => Enumerable.Empty<PlanoFaqWebViewModel>();

    internal static async Task<List<PlanoPublicoWebViewModel>> CatalogoAsync(IHttpClientFactory factory, ILogger logger)
    {
        try
        {
            var client = factory.CreateClient("PlantaoProApi");
            using var response = await client.GetAsync("api/public/planos");
            if (!response.IsSuccessStatusCode) return new List<PlanoPublicoWebViewModel>();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("data", out var data)
                && data.ValueKind != JsonValueKind.Null)
                return JsonSerializer.Deserialize<List<PlanoPublicoWebViewModel>>(data.GetRawText(), JsonOptions) ?? new List<PlanoPublicoWebViewModel>();
            return JsonSerializer.Deserialize<List<PlanoPublicoWebViewModel>>(root.GetRawText(), JsonOptions) ?? new List<PlanoPublicoWebViewModel>();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Catálogo público de planos indisponível.");
            return new List<PlanoPublicoWebViewModel>();
        }
    }

    private async Task<T?> GetAsync<T>(string endpoint) where T : class
    {
        try
        {
            var client = _factory.CreateClient("PlantaoProApi");
            using var response = await client.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("data", out var data)
                && data.ValueKind != JsonValueKind.Null)
                return JsonSerializer.Deserialize<T>(data.GetRawText(), JsonOptions);
            return JsonSerializer.Deserialize<T>(root.GetRawText(), JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Catálogo público indisponível. Endpoint:{Endpoint}", endpoint);
            return null;
        }
    }
}

[AllowAnonymous]
[Route("cadastro")]
public sealed class CadastroController : Controller
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<CadastroController> _logger;

    public CadastroController(IHttpClientFactory factory, ILogger<CadastroController> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    [HttpGet("")]
    public IActionResult Index() => RedirectToAction(nameof(Empresa));

    [HttpGet("empresa")]
    public async Task<IActionResult> Empresa() => View("Cadastro", await CriarModeloAsync());

    [HttpGet("plano")]
    public async Task<IActionResult> Plano() => View("Cadastro", await CriarModeloAsync());

    [HttpGet("usuario")]
    public async Task<IActionResult> Usuario() => View("Cadastro", await CriarModeloAsync());

    [HttpGet("confirmacao")]
    public async Task<IActionResult> Confirmacao() => View("Cadastro", await CriarModeloAsync());

    [HttpPost("confirmacao")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar(CadastroSelfServiceWebViewModel model)
    {
        if (!model.AceiteTermos || !model.AceitePrivacidade)
        {
            ModelState.AddModelError(string.Empty, "Aceite os termos e a política de privacidade para continuar.");
        }
        if (!ModelState.IsValid)
        {
            model.Planos = await PlanosPublicosController.CatalogoAsync(_factory, _logger);
            TempData["Error"] = "Revise os campos obrigatórios.";
            return View("Cadastro", model);
        }

        // B5: o POST antes terminava aqui com TempData de sucesso sem persistir
        // nada. Agora chama o finalizar real (self-service provisiona de verdade).
        try
        {
            var client = _factory.CreateClient("PlantaoProApi");
            var payload = new
            {
                empresa = new
                {
                    nomeFantasia = model.NomeFantasia,
                    razaoSocial = model.RazaoSocial,
                    cnpj = model.Cnpj,
                    segmento = model.Segmento,
                    quantidadeMedicos = model.QuantidadeMedicos,
                    quantidadeHospitais = model.QuantidadeHospitais,
                    volumePlantoesMes = model.VolumePlantoesMes,
                    cidade = model.Cidade,
                    uf = model.Uf,
                    telefone = model.Telefone,
                    emailCorporativo = model.EmailCorporativo
                },
                plano = new
                {
                    planoId = model.PlanoId,
                    periodicidade = string.IsNullOrWhiteSpace(model.Periodicidade) ? "MENSAL" : model.Periodicidade,
                    aceiteTermos = model.AceiteTermos,
                    aceitePrivacidade = model.AceitePrivacidade,
                    consentimentoLgpd = model.ConsentimentoLgpd
                },
                usuarioAdmin = new
                {
                    nome = model.ResponsavelNome,
                    email = model.ResponsavelEmail,
                    telefone = model.ResponsavelTelefone,
                    cargo = model.ResponsavelCargo,
                    senha = model.Senha
                }
            };
            using var response = await client.PostAsJsonAsync("api/public/cadastro/finalizar", payload);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                model.Planos = await PlanosPublicosController.CatalogoAsync(_factory, _logger);
                TempData["Error"] = ExtrairMensagemApi(body) ?? "Não foi possível concluir o cadastro. Revise os dados.";
                return View("Cadastro", model);
            }
            TempData["Success"] = "Cadastro finalizado com sucesso. Faça login para começar.";
            return RedirectToAction(nameof(Sucesso));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha de comunicação no finalizar self-service.");
            model.Planos = await PlanosPublicosController.CatalogoAsync(_factory, _logger);
            TempData["Error"] = "Não foi possível concluir o cadastro agora. Tente novamente.";
            return View("Cadastro", model);
        }
    }

    [HttpGet("sucesso")]
    public IActionResult Sucesso() => View();

    private static string? ExtrairMensagemApi(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("message", out var message)
                && message.ValueKind == JsonValueKind.String)
                return message.GetString();
        }
        catch (JsonException)
        {
        }
        return null;
    }

    private async Task<CadastroSelfServiceWebViewModel> CriarModeloAsync()
    {
        return new CadastroSelfServiceWebViewModel { Planos = await PlanosPublicosController.CatalogoAsync(_factory, _logger), Periodicidade = "MENSAL", ConsentimentoLgpd = true };
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
        TempData["Success"] = "Perfil salvo com sucesso.";
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
            if (profile is null) TempData["Error"] = error ?? "Perfil não encontrado.";
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
