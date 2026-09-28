using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public partial class Administrativo360Controller
{
    [HttpGet]
    public async Task<IActionResult> Parceiros(string? busca, string? papel, bool? status)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var lookups = await CarregarLookupsAsync(client);
        var itens = lookups.Parceiros.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLowerInvariant();
            itens = itens.Where(p => p.Nome.ToLowerInvariant().Contains(termo) || (p.Documento != null && p.Documento.Contains(termo)));
        }

        if (papel == "FORNECEDOR")
            itens = itens.Where(p => p.Fornecedor);
        else if (papel == "HOSPITAL")
            itens = itens.Where(p => p.EhHospital);
        else if (papel == "PAGADOR")
            itens = itens.Where(p => p.EhPagador);
        else if (papel == "CLIENTE")
            itens = itens.Where(p => p.EhCliente);

        if (status.HasValue)
            itens = itens.Where(p => p.Ativo == status.Value);

        return View(new ParceirosIndexViewModel
        {
            Parceiros = itens.ToList(),
            Busca = busca,
            Papel = papel,
            Status = status
        });
    }

    [HttpGet]
    public async Task<IActionResult> Produtos(string? busca, bool? status)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var lookups = await CarregarLookupsAsync(client);
        var itens = lookups.Produtos.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLowerInvariant();
            itens = itens.Where(p => p.Nome.ToLowerInvariant().Contains(termo) || p.Sku.ToLowerInvariant().Contains(termo) || (p.CodigoBarras != null && p.CodigoBarras.Contains(termo)));
        }

        if (status.HasValue)
            itens = itens.Where(p => p.Ativo == status.Value);

        return View(new ProdutosIndexViewModel
        {
            Produtos = itens.ToList(),
            Busca = busca,
            Status = status
        });
    }

    [HttpGet]
    public async Task<IActionResult> Locais(string? busca, string? tipo, bool? status)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var lookups = await CarregarLookupsAsync(client);
        var itens = lookups.Locais.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLowerInvariant();
            itens = itens.Where(l => l.Nome.ToLowerInvariant().Contains(termo) || l.Codigo.ToLowerInvariant().Contains(termo));
        }

        if (!string.IsNullOrWhiteSpace(tipo))
            itens = itens.Where(l => l.Tipo.Equals(tipo, StringComparison.OrdinalIgnoreCase));

        if (status.HasValue)
            itens = itens.Where(l => l.Ativo == status.Value);

        return View(new LocaisIndexViewModel
        {
            Locais = itens.ToList(),
            Busca = busca,
            Tipo = tipo,
            Status = status
        });
    }

    [HttpGet]
    public async Task<IActionResult> Lotes(string? busca, Guid? produtoId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var lookups = await CarregarLookupsAsync(client);
        var itens = lookups.Lotes.AsEnumerable();

        if (produtoId.HasValue && produtoId.Value != Guid.Empty)
            itens = itens.Where(l => l.ProdutoId == produtoId.Value);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLowerInvariant();
            itens = itens.Where(l => l.Codigo.ToLowerInvariant().Contains(termo) || l.ProdutoNome.ToLowerInvariant().Contains(termo));
        }

        return View(new LotesIndexViewModel
        {
            Lotes = itens.ToList(),
            Produtos = lookups.Produtos,
            Busca = busca,
            ProdutoId = produtoId
        });
    }
    [HttpGet]
    public async Task<IActionResult> Cadastros(string? aba)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var lookups = await CarregarLookupsAsync(client);
        return View(new CadastrosIndexViewModel
        {
            Parceiros = lookups.Parceiros,
            Produtos = lookups.Produtos,
            Locais = lookups.Locais,
            AbaAtiva = string.IsNullOrWhiteSpace(aba) ? "parceiros" : aba
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarParceiro(Guid? id, string nome, string? documento, bool fornecedor, bool ativo = true, bool ehHospital = false, bool ehPagador = false, bool ehCliente = false)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new { Id = id, Nome = nome, Documento = documento, Fornecedor = fornecedor, Ativo = ativo, EhHospital = ehHospital, EhPagador = ehPagador, EhCliente = ehCliente };
        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/cadastros/parceiros", payload);
        if (resp.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created)
            TempData["SuccessMessage"] = "Parceiro cadastrado/atualizado com sucesso.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao salvar parceiro.";

        var referer = Request.Headers["Referer"].ToString();
        if (referer.Contains("/Parceiros", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Parceiros));

        return RedirectToAction(nameof(Cadastros), new { aba = "parceiros" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AlternarStatusParceiro(Guid id, bool ativo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Patch, $"api/administrativo360/cadastros/parceiros/{id}/status?ativo={ativo}", new { });
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["SuccessMessage"] = $"Parceiro {(ativo ? "ativado" : "inativado")} com sucesso.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao alterar status do parceiro.";

        var referer = Request.Headers["Referer"].ToString();
        if (referer.Contains("/Parceiros", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Parceiros));

        return RedirectToAction(nameof(Cadastros), new { aba = "parceiros" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarProduto(Guid? id, string sku, string nome, string unidade, string? codigoBarras, bool controlaLote, bool exigeInspecao, decimal precoCusto, bool ativo = true)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new { Id = id, Sku = sku, Nome = nome, Unidade = unidade, CodigoBarras = codigoBarras, ControlaLote = controlaLote, ExigeInspecao = exigeInspecao, PrecoCusto = precoCusto, Ativo = ativo };
        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/cadastros/produtos", payload);
        if (resp.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created)
            TempData["SuccessMessage"] = "Produto cadastrado/atualizado com sucesso.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao salvar produto.";

        var referer = Request.Headers["Referer"].ToString();
        if (referer.Contains("/Produtos", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Produtos));

        return RedirectToAction(nameof(Cadastros), new { aba = "produtos" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AlternarStatusProduto(Guid id, bool ativo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Patch, $"api/administrativo360/cadastros/produtos/{id}/status?ativo={ativo}", new { });
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["SuccessMessage"] = $"Produto {(ativo ? "ativado" : "inativado")} com sucesso.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao alterar status do produto.";

        var referer = Request.Headers["Referer"].ToString();
        if (referer.Contains("/Produtos", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Produtos));

        return RedirectToAction(nameof(Cadastros), new { aba = "produtos" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarLocal(Guid? id, string codigo, string nome, string tipo, bool ativo = true)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new { Id = id, Codigo = codigo, Nome = nome, Tipo = tipo, Ativo = ativo };
        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/cadastros/locais", payload);
        if (resp.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created)
            TempData["SuccessMessage"] = "Local de armazenamento cadastrado/atualizado com sucesso.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao salvar local.";

        var referer = Request.Headers["Referer"].ToString();
        if (referer.Contains("/Locais", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Locais));

        return RedirectToAction(nameof(Cadastros), new { aba = "locais" });
    }

    // ==========================================
    // COMPRAS E SUPRIMENTOS
    // ==========================================
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Departamento(string codigo, string nome) =>
        await Send("api/administrativo360/departamentos", new { codigo, nome }, "Departamento cadastrado.");

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cargo(string codigo, string nome, Guid? departamentoId) =>
        await Send("api/administrativo360/cargos", new { codigo, nome, departamentoId }, "Cargo cadastrado.");

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Colaborador(string matricula, string nome, string cpf, string email, Guid cargoId) =>
        await Send("api/administrativo360/colaboradores", new { matricula, nome, cpf, email, cargoId }, "Colaborador cadastrado.");

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Contrato(Guid colaboradorId, string tipo, DateOnly inicio, DateOnly? fim, decimal salario, int cargaHorariaSemanal) =>
        await Send("api/administrativo360/contratos", new { colaboradorId, tipo, inicio, fim, salario, cargaHorariaSemanal }, "Contratação registrada.");

    // ==========================================
    // CIRURGIAS OPERACIONAIS
    // ==========================================
}
