using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public partial class Administrativo360Controller
{
    // Agenda de cirurgias vinculada a orcamentos
    [HttpGet]
    public async Task<IActionResult> Cirurgias(string? busca, string? situacao, DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(busca)) query.Add($"busca={Uri.EscapeDataString(busca)}");
        if (!string.IsNullOrWhiteSpace(situacao)) query.Add($"situacao={Uri.EscapeDataString(situacao)}");
        if (inicio.HasValue) query.Add($"inicio={inicio:yyyy-MM-dd}");
        if (fim.HasValue) query.Add($"fim={fim:yyyy-MM-dd}");

        var path = "api/administrativo360/cirurgias" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var res = await ReadApiResponse<List<CirurgiaResumoViewModel>>(client, path);

        ViewBag.Busca = busca;
        ViewBag.Situacao = situacao;
        ViewBag.Inicio = inicio;
        ViewBag.Fim = fim;
        ViewBag.Erro = res.Error;

        return View(res.Data ?? new List<CirurgiaResumoViewModel>());
    }

    [HttpGet]
    public async Task<IActionResult> CirurgiaNova(Guid? orcamentoId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var model = new CirurgiaFormViewModel();
        if (orcamentoId.HasValue)
        {
            var res = await ReadApiResponse<OrcamentoDetalhesViewModel>(client, $"api/administrativo360/orcamentos/{orcamentoId.Value}");
            var orc = res.Data;
            if (orc is not null)
            {
                model.OrcamentoId = orc.Id;
                model.OrcamentoRevisao = orc.Revisao;
                model.HospitalId = orc.HospitalId;
                model.MedicoId = orc.MedicoId;
                model.Procedimento = orc.Procedimento;
                model.DataPrevista = orc.DataPrevista;
            }
        }

        await PreencherLookupsCirurgiaAsync(client, model);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CirurgiaNova(CirurgiaFormViewModel model)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        if (string.IsNullOrWhiteSpace(model.Procedimento))
        {
            ModelState.AddModelError(nameof(model.Procedimento), "Informe o procedimento cirúrgico.");
            await PreencherLookupsCirurgiaAsync(client, model);
            return View(model);
        }

        var payload = new
        {
            model.HospitalId,
            model.MedicoId,
            model.Procedimento,
            model.DataPrevista,
            model.HoraPrevista,
            model.OrcamentoId,
            model.OrcamentoRevisao,
            model.ResponsavelId,
            model.LocalDestinoId,
            model.Observacoes
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/cirurgias", payload);
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
        {
            TempData["SuccessMessage"] = "Cirurgia operacional agendada com sucesso.";
            return RedirectToAction(nameof(Cirurgias));
        }

        TempData["ErrorMessage"] = resp.Error ?? "Falha ao agendar cirurgia.";
        await PreencherLookupsCirurgiaAsync(client, model);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CirurgiaDetalhes(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<CirurgiaDetalhesViewModel>(client, $"api/administrativo360/cirurgias/{id}");
        if (resp.Data is null) return NotFound();

        return View(resp.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CirurgiaCancelar(Guid id, string motivo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new { CirurgiaId = id, Motivo = motivo };
        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/cirurgias/{id}/cancelar", payload);

        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["SuccessMessage"] = "Cirurgia cancelada com sucesso.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao cancelar cirurgia.";

        return RedirectToAction(nameof(CirurgiaDetalhes), new { id });
    }

    // ==========================================
    // VALES DE CONSIGNAÇÃO
    // ==========================================
}
