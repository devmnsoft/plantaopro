using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Localization;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public partial class Administrativo360Controller
{
    [HttpGet]
    public async Task<IActionResult> Parceiros(string? busca, string? papel, bool? status, int? pagina)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        // Listagem dedicada com filtros e paginação no servidor (teto de 100 itens por página);
        // inativos continuam visíveis para preservar o histórico.
        var paginaAtual = Math.Max(1, pagina ?? 1);
        var qs = new List<string> { "pagina=" + paginaAtual };
        if (!string.IsNullOrWhiteSpace(busca))
            qs.Add("busca=" + Uri.EscapeDataString(busca.Trim()));

        switch ((papel ?? "").Trim().ToUpperInvariant())
        {
            case "FORNECEDOR":
                qs.Add("fornecedor=true");
                break;
            case "HOSPITAL":
                qs.Add("ehHospital=true");
                break;
            case "PAGADOR":
                qs.Add("ehPagador=true");
                break;
            case "CLIENTE":
                qs.Add("ehCliente=true");
                break;
        }

        if (status == true)
            qs.Add("apenasAtivos=true");
        else if (status == false)
            qs.Add("apenasInativos=true");

        var resp = await ReadApiListResponseAsync<Parceiro360ViewModel>(client, "api/administrativo360/cadastros/parceiros?" + string.Join("&", qs));
        var itens = resp.Data.ToList();

        return View(new ParceirosIndexViewModel
        {
            Parceiros = itens,
            Busca = busca,
            Papel = papel,
            Status = status,
            Pagina = paginaAtual,
            TemProximaPagina = itens.Count >= 100,
            Erro = resp.Error
        });
    }

    [HttpGet]
    public async Task<IActionResult> Produtos(string? busca, bool? status, int? pagina, Guid? produtoId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        // Listagem dedicada com filtros e paginação no servidor (teto de 100 itens por página).
        var paginaAtual = Math.Max(1, pagina ?? 1);
        var qs = new List<string> { "pagina=" + paginaAtual };
        if (!string.IsNullOrWhiteSpace(busca))
            qs.Add("busca=" + Uri.EscapeDataString(busca.Trim()));
        if (status == true)
            qs.Add("apenasAtivos=true");
        else if (status == false)
            qs.Add("apenasInativos=true");

        var resp = await ReadApiListResponseAsync<Produto360ViewModel>(client, "api/administrativo360/cadastros/produtos?" + string.Join("&", qs));
        var itens = resp.Data.ToList();

        var vm = new ProdutosIndexViewModel
        {
            Produtos = itens,
            Busca = busca,
            Status = status,
            Pagina = paginaAtual,
            TemProximaPagina = itens.Count >= 100,
            Erro = resp.Error
        };

        // s5 (rodada 9): seção de lotes vinculados quando um produto está em contexto
        // (registro de lote não ajusta estoque; quantidade só por movimento com origem).
        if (produtoId.HasValue && produtoId.Value != Guid.Empty)
        {
            vm.ProdutoId = produtoId.Value;
            var contexto = itens.FirstOrDefault(x => x.Id == produtoId.Value);
            var respLotes = await ReadApiListResponseAsync<Lote360ViewModel>(client, "api/administrativo360/cadastros/lotes?produtoId=" + produtoId.Value);
            vm.Lotes = respLotes.Data.ToList();
            if (contexto != null)
            {
                vm.ProdutoSku = contexto.Sku;
                vm.ProdutoNome = contexto.Nome;
                vm.ProdutoControlaLote = contexto.ControlaLote;
            }
            else if (vm.Lotes.Count > 0)
            {
                vm.ProdutoNome = vm.Lotes[0].ProdutoNome;
            }
            else
            {
                vm.ProdutoNome = produtoId.Value.ToString();
            }
            if (string.IsNullOrWhiteSpace(vm.Erro) && !string.IsNullOrWhiteSpace(respLotes.Error))
                vm.Erro = respLotes.Error;
        }

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Locais(string? busca, string? tipo, bool? status, int? pagina)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        // Listagem dedicada com filtros e paginação no servidor (teto de 100 itens por página).
        var paginaAtual = Math.Max(1, pagina ?? 1);
        var qs = new List<string> { "pagina=" + paginaAtual };
        if (!string.IsNullOrWhiteSpace(busca))
            qs.Add("busca=" + Uri.EscapeDataString(busca.Trim()));
        if (!string.IsNullOrWhiteSpace(tipo))
            qs.Add("tipo=" + Uri.EscapeDataString(tipo.Trim().ToUpperInvariant()));
        if (status == true)
            qs.Add("apenasAtivos=true");
        else if (status == false)
            qs.Add("apenasInativos=true");

        var resp = await ReadApiListResponseAsync<Local360ViewModel>(client, "api/administrativo360/cadastros/locais?" + string.Join("&", qs));
        var itens = resp.Data.ToList();

        return View(new LocaisIndexViewModel
        {
            Locais = itens,
            Busca = busca,
            Tipo = tipo,
            Status = status,
            Pagina = paginaAtual,
            TemProximaPagina = itens.Count >= 100,
            Erro = resp.Error
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
            TempData["Success"] = UiText.L("Adm360.Cadastros.OkPartnerSaved");
        else
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Cadastros.FallbackSavePartner");

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
            TempData["Success"] = ativo ? UiText.L("Adm360.Cadastros.OkPartnerActivated") : UiText.L("Adm360.Cadastros.OkPartnerDeactivated");
        else
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Cadastros.FallbackPartnerStatus");

        var referer = Request.Headers["Referer"].ToString();
        if (referer.Contains("/Parceiros", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Parceiros));

        return RedirectToAction(nameof(Cadastros), new { aba = "parceiros" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarProduto(Guid? id, string sku, string nome, string unidade, string? codigoBarras, bool controlaLote, bool exigeInspecao, decimal precoCusto, bool ativo = true, string? anvisaNumero = null, string anvisaAplicabilidade = "PENDENTE", bool anvisaEntradaManual = true, DateOnly? anvisaVigencia = null, string? anvisaJustificacao = null)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        // s6 (rodada 9): campos ANVISA encaminhados para validação no servidor (API).
        var payload = new { Id = id, Sku = sku, Nome = nome, Unidade = unidade, CodigoBarras = codigoBarras, ControlaLote = controlaLote, ExigeInspecao = exigeInspecao, PrecoCusto = precoCusto, Ativo = ativo, AnvisaNumero = anvisaNumero, AnvisaAplicabilidade = anvisaAplicabilidade, AnvisaEntradaManual = anvisaEntradaManual, AnvisaVigencia = anvisaVigencia, AnvisaJustificacao = anvisaJustificacao };
        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/cadastros/produtos", payload);
        if (resp.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created)
            TempData["Success"] = UiText.L("Adm360.Cadastros.OkProductSaved");
        else
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Cadastros.FallbackSaveProduct");

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
            TempData["Success"] = ativo ? UiText.L("Adm360.Cadastros.OkProductActivated") : UiText.L("Adm360.Cadastros.OkProductDeactivated");
        else
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Cadastros.FallbackProductStatus");

        var referer = Request.Headers["Referer"].ToString();
        if (referer.Contains("/Produtos", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Produtos));

        return RedirectToAction(nameof(Cadastros), new { aba = "produtos" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarLote(Guid produtoId, string codigo, DateOnly? validade, decimal? custoUnitario)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new { ProdutoId = produtoId, Codigo = codigo, Validade = validade, CustoUnitario = custoUnitario };
        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/cadastros/lotes", payload);
        if (resp.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created)
            TempData["Success"] = UiText.L("Adm360.Cadastros.OkLoteSaved");
        else
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Cadastros.FallbackSaveLote");

        // Volta mantendo o contexto do produto (seção de lotes atualizada).
        return RedirectToAction(nameof(Produtos), new { produtoId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarLocal(Guid? id, string codigo, string nome, string tipo, bool ativo = true)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new { Id = id, Codigo = codigo, Nome = nome, Tipo = tipo, Ativo = ativo };
        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/cadastros/locais", payload);
        if (resp.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created)
            TempData["Success"] = UiText.L("Adm360.Cadastros.OkLocationSaved");
        else
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Cadastros.FallbackSaveLocation");

        var referer = Request.Headers["Referer"].ToString();
        if (referer.Contains("/Locais", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Locais));

        return RedirectToAction(nameof(Cadastros), new { aba = "locais" });
    }

    // ==========================================
    // CIRURGIAS OPERACIONAIS
    // ==========================================
}
