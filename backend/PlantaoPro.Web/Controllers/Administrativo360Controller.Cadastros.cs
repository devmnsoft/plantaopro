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
        else if (papel == "CLIENTE")
            itens = itens.Where(p => !p.Fornecedor);

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
}
