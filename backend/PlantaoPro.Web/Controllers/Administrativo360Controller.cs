using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE,ADMIN_CLIENTE,GESTOR_OPERACIONAL,DIRETOR,COORDENACAO,COORDENADOR,CONSULTA_CLIENTE,AUDITOR")]
public partial class Administrativo360Controller : BaseWebController
{
    public Administrativo360Controller(IHttpClientFactory factory, ILogger<Administrativo360Controller> logger)
        : base(factory, logger) { }

    public async Task<IActionResult> Index()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resumo = await ReadApiResponse<Administrativo360ResumoViewModel>(client, "api/administrativo360/resumo");

        return View(new OrganizacaoIndexViewModel
        {
            Resumo = resumo.Data ?? new(0, 0, 0, 0),
            Erro = resumo.Error
        });
    }

    private async Task<Lookups360ViewModel> CarregarLookupsAsync(HttpClient client)
    {
        var resp = await ReadApiResponse<Lookups360ViewModel>(client, "api/administrativo360/cadastros/lookups");
        if (!string.IsNullOrEmpty(resp.Error))
        {
            Logger.LogWarning("Falha ao carregar lookups do Administrativo 360: {Error}", resp.Error);
        }
        return resp.Data ?? new Lookups360ViewModel();
    }

    private async Task PreencherLookupsOrcamentoAsync(HttpClient client, OrcamentoFormViewModel model)
    {
        var lookups = await CarregarLookupsAsync(client);
        model.Hospitais = (lookups.Hospitais?.Count > 0 ? lookups.Hospitais : lookups.Parceiros.Where(p => !p.Fornecedor).ToList()).ToArray();
        model.Medicos = lookups.Medicos ?? Array.Empty<Medico360ViewModel>();
        model.Pagadores = (lookups.Pagadores?.Count > 0 ? lookups.Pagadores : lookups.Parceiros.Where(p => !p.Fornecedor).ToList()).ToArray();
        model.ProdutosDisponiveis = lookups.Produtos;
    }

    private async Task PreencherLookupsCirurgiaAsync(HttpClient client, CirurgiaFormViewModel model)
    {
        var lookups = await CarregarLookupsAsync(client);
        var respOrc = await ReadApiResponse<IReadOnlyList<OrcamentoResumoViewModel>>(client, "api/administrativo360/orcamentos");
        model.Hospitais = (lookups.Hospitais?.Count > 0 ? lookups.Hospitais : lookups.Parceiros.Where(p => !p.Fornecedor).ToList()).ToArray();
        model.Medicos = lookups.Medicos ?? Array.Empty<Medico360ViewModel>();
        model.Locais = lookups.Locais;
        model.Orcamentos = respOrc.Data ?? Array.Empty<OrcamentoResumoViewModel>();
    }

    private async Task PreencherLookupsValeAsync(HttpClient client, ValeFormViewModel model)
    {
        var lookups = await CarregarLookupsAsync(client);
        var respCir = await ReadApiResponse<IReadOnlyList<CirurgiaResumoViewModel>>(client, "api/administrativo360/cirurgias");
        var respOrc = await ReadApiResponse<IReadOnlyList<OrcamentoResumoViewModel>>(client, "api/administrativo360/orcamentos");
        model.Hospitais = (lookups.Hospitais?.Count > 0 ? lookups.Hospitais : lookups.Parceiros.Where(p => !p.Fornecedor).ToList()).ToArray();
        model.LocaisOrigem = lookups.Locais.Where(l => l.Tipo == "INTERNO").ToArray();
        model.LocaisDestino = lookups.Locais.ToArray();
        model.Cirurgias = respCir.Data ?? Array.Empty<CirurgiaResumoViewModel>();
        model.Orcamentos = respOrc.Data ?? Array.Empty<OrcamentoResumoViewModel>();
        model.ProdutosDisponiveis = lookups.Produtos;
        model.LotesDisponiveis = lookups.Lotes;
    }

    // ==========================================
    // HELPERS COMPARTILHADOS (CSV e envio generico a API)
    // As areas de dominio estao em parciais: .Cadastros .Organizacao .ComprasRecebimentos
    // .EstoqueValorizacao .Orcamentos .Cirurgias .Vales .Relatorios
    // .Vendas .Financeiro .RelatoriosFinanceiros
    // ==========================================
    private static string SanitizarCsv(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return string.Empty;
        var limpo = valor.Replace("\"", "\"\"");
        // Prevenção contra Formula Injection no Excel (=, +, -, @)
        if (limpo.StartsWith('=') || limpo.StartsWith('+') || limpo.StartsWith('-') || limpo.StartsWith('@'))
            limpo = "'" + limpo;
        return $"\"{limpo}\"";
    }
}
