using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Services.Security;

namespace PlantaoPro.Web.Controllers;

/// <summary>
/// R5-C7/C8: o Index do onboarding nao e mais uma lista estatica de 11 etapas genericas.
/// Ele le o checklist materializado do catalogo canonicos (adaptado ao contrato do cliente)
/// e reavaliado contra dados persistidos; acoes comerciais viram operacoes reais na API.
/// NovoCliente/Sucesso continuam sendo o wizard MNSOFT (admin cria cliente + plano + admin).
/// </summary>
[Authorize]
public sealed class OnboardingController : BaseWebController
{
    private readonly ICurrentUserService _currentUser;

    public OnboardingController(IHttpClientFactory httpClientFactory, ILogger<OnboardingController> logger,
        ICurrentUserService currentUser) : base(httpClientFactory, logger)
    {
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var vm = new OnboardingJornadaWebViewModel
        {
            SuccessMessage = TempData["Success"] as string,
            ErrorMessage = TempData["Error"] as string,
        };

        // A area global sem cliente ativo nao tem onboarding proprio (mesma honestidade de convites/notificacoes em A3).
        if (_currentUser.IsGlobalAdmin() && !_currentUser.TenantId.HasValue)
        {
            vm.SemContextoCliente = true;
            vm.InfoMessage = "Onboarding pertence a um cliente. Cadastre um novo cliente com plano em \"Iniciar onboarding\" ou selecione um cliente em AdminSaaS para acompanhar a jornada dele.";
            return View(vm);
        }

        var status = await ReadApiResponse<OnboardingStatusWebModel>(client, "api/onboarding/status");
        if (status.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (status.Data is null)
        {
            //tenant autenticado sem onboarding materializado: ponto de partida real, nunca lista fake.
            vm.SemOnboarding = true;
            return View(vm);
        }

        vm.Status = status.Data.Status;
        vm.Progresso = status.Data.Progresso;
        vm.ProximaAcao = status.Data.ProximaAcao;

        // O GET da API ja reavalia contra os dados persistidos e persiste as transicoes (mesma governanca dos contratos).
        var checklist = await ReadApiListResponseAsync<OnboardingEtapaWebModel>(client, "api/onboarding/checklist");
        if (checklist.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        vm.ErrorMessage ??= checklist.Error;

        var etapas = checklist.Data.OrderBy(e => e.Ordem).ToList();
        if (etapas.Any(e => string.IsNullOrEmpty(e.ModuloCodigo)))
            vm.Grupos.Add(new OnboardingGrupoWebModel
            {
                Geral = true,
                ModuloNome = "Implantação geral",
                Itens = etapas.Where(e => string.IsNullOrEmpty(e.ModuloCodigo)).ToList(),
            });
        vm.Grupos.AddRange(etapas
            .Where(e => !string.IsNullOrEmpty(e.ModuloCodigo))
            .GroupBy(e => e.ModuloCodigo)
            .Select(g => new OnboardingGrupoWebModel
            {
                ModuloCodigo = g.Key,
                ModuloNome = g.First().ModuloNome,
                Itens = g.ToList(),
            })
            .OrderBy(grupo => grupo.Itens.Min(e => e.Ordem)));

        return View(vm);
    }

    [HttpPost("Iniciar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Iniciar()
    {
        var (ok, _) = await PostJornadaAsync("api/onboarding/iniciar");
        if (ok) TempData["Success"] = "Onboarding iniciado: as etapas refletem os modulos contratados deste cliente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Reavaliar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reavaliar()
    {
        var (ok, _) = await PostJornadaAsync("api/onboarding/reavaliar");
        if (ok) TempData["Success"] = "Checklist reavaliado contra os dados atuais do ambiente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ConcluirEtapa")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConcluirEtapa(Guid id)
    {
        // Sem criterio atendido a API responde 409 com a evidencia do que falta (nada de sucesso por clique).
        var (_, erro) = await PostJornadaAsync($"api/onboarding/checklist/{id}/concluir");
        if (erro is null) TempData["Success"] = "Etapa concluida.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("PularEtapa")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PularEtapa(Guid id, [FromForm] string? justificativa)
    {
        var (_, erro) = await PostJornadaAsync($"api/onboarding/checklist/{id}/pular", new { justificativa });
        if (erro is null) TempData["Success"] = "Etapa opcional marcada como pulada (persistida).";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("RestaurarEtapa")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestaurarEtapa(Guid id)
    {
        var (_, erro) = await PostJornadaAsync($"api/onboarding/checklist/{id}/restaurar");
        if (erro is null) TempData["Success"] = "Etapa restaurada para o checklist.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>POST fino na API; devolve (sucesso, erro). helper ja propaga TempData["Error"] com a mensagem real.</summary>
    private async Task<(bool Ok, string? Erro)> PostJornadaAsync(string rota, object? body = null)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return (false, null);
        var (_, erro, status) = await SendApiAsync<object, string>(client, HttpMethod.Post, rota, body ?? new { });
        if (status == HttpStatusCode.Unauthorized) return (false, null);
        return (status is >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous, erro);
    }

    [HttpGet]
    public IActionResult NovoCliente() => View(new OnboardingClienteViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NovoCliente(OnboardingClienteViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            using var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();

            var req = model.ToRequest();
            var (payload, error, statusCode) = await SendApiAsync<CreateClienteOnboardingRequest, OnboardingResumoDto>(client, HttpMethod.Post, "api/onboarding/cliente", req);

            if (statusCode is < System.Net.HttpStatusCode.OK or >= System.Net.HttpStatusCode.Ambiguous || payload is null)
            {
                ModelState.AddModelError(string.Empty, error ?? "Não foi possível concluir o onboarding.");
                return View(model);
            }

            return RedirectToAction(nameof(Sucesso), new { clienteId = payload.ClienteId });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao executar onboarding web");
            ModelState.AddModelError(string.Empty, "Erro inesperado ao criar cliente.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Sucesso(Guid clienteId)
    {
        try
        {
            using var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();

            var (data, error, _) = await ReadApiResponse<OnboardingResumoDto>(client, $"api/onboarding/resumo?clienteId={clienteId}");
            if (data is null) return EmptyViewWithError(new OnboardingResumoDto(), error ?? "Resumo não disponível.");
            return View(data);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao carregar página de sucesso do onboarding.");
            return EmptyViewWithError(new OnboardingResumoDto(), "Erro ao carregar resumo.");
        }
    }
}
