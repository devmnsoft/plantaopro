using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR")]
public sealed class PilotoController : BaseWebController
{
    public PilotoController(IHttpClientFactory httpClientFactory, ILogger<PilotoController> logger) : base(httpClientFactory, logger) { }

    public async Task<IActionResult> Index()
    {
        var resumo = await ApiGetAsync<Dictionary<string, object>>("api/piloto/resumo");
        return View(resumo.Data);
    }

    public async Task<IActionResult> Checklist()
    {
        var checklist = await ApiGetAsync<List<ChecklistViewModel>>("api/piloto/checklist");
        return View(checklist.Data ?? new List<ChecklistViewModel>());
    }

    public async Task<IActionResult> Ocorrencias()
    {
        var ocorrencias = await ApiGetAsync<List<OcorrenciaViewModel>>("api/piloto/ocorrencias");
        return View(ocorrencias.Data ?? new List<OcorrenciaViewModel>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarOcorrencia(NovaOcorrenciaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ToastError"] = "Verifique os campos obrigatórios.";
            return RedirectToAction(nameof(Ocorrencias));
        }

        var response = await ApiPostAsync<object>("api/piloto/ocorrencias", model);
        TempData[response.Success ? "ToastSuccess" : "ToastError"] = response.Message;
        return RedirectToAction(nameof(Ocorrencias));
    }

    public sealed class ChecklistViewModel { public int Id { get; set; } public string Titulo { get; set; } = string.Empty; public bool Concluido { get; set; } }
    public sealed class OcorrenciaViewModel { public Guid Id { get; set; } public string Tipo { get; set; } = string.Empty; public string Prioridade { get; set; } = string.Empty; public string Descricao { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; public string Responsavel { get; set; } = string.Empty; public DateTime DataAbertura { get; set; } }
    public sealed class NovaOcorrenciaViewModel { public string Tipo { get; set; } = string.Empty; public string Prioridade { get; set; } = string.Empty; public string Descricao { get; set; } = string.Empty; public string? Responsavel { get; set; } }
}
