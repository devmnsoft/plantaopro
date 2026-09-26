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
        var departamentos = await ReadApiResponse<IReadOnlyList<Departamento360ViewModel>>(client, "api/administrativo360/departamentos");
        var cargos = await ReadApiResponse<IReadOnlyList<Cargo360ViewModel>>(client, "api/administrativo360/cargos");
        var colaboradores = await ReadApiResponse<IReadOnlyList<Colaborador360ViewModel>>(client, "api/administrativo360/colaboradores");
        var contratos = await ReadApiResponse<IReadOnlyList<Contrato360ViewModel>>(client, "api/administrativo360/contratos");

        return View(new Administrativo360PageViewModel
        {
            Resumo = resumo.Data ?? new(0, 0, 0, 0),
            Departamentos = departamentos.Data ?? Array.Empty<Departamento360ViewModel>(),
            Cargos = cargos.Data ?? Array.Empty<Cargo360ViewModel>(),
            Colaboradores = colaboradores.Data ?? Array.Empty<Colaborador360ViewModel>(),
            Contratos = contratos.Data ?? Array.Empty<Contrato360ViewModel>(),
            Erro = resumo.Error ?? departamentos.Error ?? cargos.Error ?? colaboradores.Error ?? contratos.Error
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
    // CADASTROS MESTRES (PARCEIROS, PRODUTOS, LOCAIS)
    // ==========================================

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

    [HttpGet]
    public async Task<IActionResult> PedidosCompra(string? fornecedor, string? situacao, DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var q = new List<string>();
        if (!string.IsNullOrWhiteSpace(fornecedor)) q.Add($"fornecedor={Uri.EscapeDataString(fornecedor)}");
        if (!string.IsNullOrWhiteSpace(situacao) && situacao != "Todas") q.Add($"situacao={Uri.EscapeDataString(situacao)}");
        if (inicio.HasValue) q.Add($"inicio={inicio:yyyy-MM-dd}");
        if (fim.HasValue) q.Add($"fim={fim:yyyy-MM-dd}");

        var query = "api/administrativo360/compras" + (q.Count > 0 ? "?" + string.Join("&", q) : "");
        var resp = await ReadApiResponse<IReadOnlyList<PedidoCompraResumoViewModel>>(client, query);
        var lookups = await CarregarLookupsAsync(client);

        return View(new PedidosCompraIndexViewModel
        {
            Pedidos = resp.Data ?? Array.Empty<PedidoCompraResumoViewModel>(),
            Fornecedores = lookups.Parceiros.Where(p => p.Fornecedor).ToArray(),
            Produtos = lookups.Produtos,
            Fornecedor = fornecedor,
            Situacao = situacao,
            Inicio = inicio,
            Fim = fim,
            Erro = resp.Error
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarPedidoCompra(Guid fornecedorId, DateOnly? previsao, decimal frete, Guid[] produtoId, decimal[] quantidade, decimal[] precoUnitario, decimal[] desconto)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        if (fornecedorId == Guid.Empty || produtoId == null || produtoId.Length == 0)
        {
            TempData["ErrorMessage"] = "Fornecedor e ao menos um produto são obrigatórios.";
            return RedirectToAction(nameof(PedidosCompra));
        }

        var itens = new List<object>();
        for (int i = 0; i < produtoId.Length; i++)
        {
            var pId = produtoId[i];
            var qtd = quantidade != null && i < quantidade.Length ? quantidade[i] : 0;
            var preco = precoUnitario != null && i < precoUnitario.Length ? precoUnitario[i] : 0;
            var desc = desconto != null && i < desconto.Length ? desconto[i] : 0;
            if (pId != Guid.Empty && qtd > 0)
            {
                itens.Add(new { ProdutoId = pId, Quantidade = qtd, PrecoUnitario = preco, Desconto = desc });
            }
        }

        if (itens.Count == 0)
        {
            TempData["ErrorMessage"] = "Informe ao menos um produto com quantidade maior que zero.";
            return RedirectToAction(nameof(PedidosCompra));
        }

        var payload = new
        {
            FornecedorId = fornecedorId,
            Previsao = previsao,
            Frete = frete,
            Itens = itens
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/compras", payload);
        if (resp.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created)
            TempData["SuccessMessage"] = "Pedido de compra cadastrado com sucesso em rascunho.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao criar pedido de compra.";

        return RedirectToAction(nameof(PedidosCompra));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AprovarPedidoCompra(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", $"APROV-{id:N}");

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/compras/{id}/aprovar", new { });
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["SuccessMessage"] = "Pedido de compra aprovado com sucesso. Valores congelados.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao aprovar pedido de compra.";

        return RedirectToAction(nameof(PedidosCompra));
    }

    // ==========================================
    // RECEBIMENTOS E CONFERÊNCIA
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Recebimentos()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var respAprov = await ReadApiResponse<IReadOnlyList<PedidoCompraResumoViewModel>>(client, "api/administrativo360/compras?situacao=APROVADO");
        var respParc = await ReadApiResponse<IReadOnlyList<PedidoCompraResumoViewModel>>(client, "api/administrativo360/compras?situacao=PARCIAL");
        var lookups = await CarregarLookupsAsync(client);

        var list = new List<PedidoCompraResumoViewModel>();
        if (respAprov.Data != null) list.AddRange(respAprov.Data);
        if (respParc.Data != null) list.AddRange(respParc.Data);

        return View(new RecebimentosIndexViewModel
        {
            PedidosPendentes = list,
            Locais = lookups.Locais.Where(l => l.Tipo == "INTERNO").ToArray(),
            Erro = respAprov.Error ?? respParc.Error
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarRecebimento(Guid pedidoId, string documento, Guid pedidoItemId, decimal quantidade, string? lote, DateOnly? validade, Guid localId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        if (pedidoId == Guid.Empty || string.IsNullOrWhiteSpace(documento) || quantidade <= 0 || localId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "Pedido, documento da nota fiscal, quantidade e local de destino são obrigatórios.";
            return RedirectToAction(nameof(Recebimentos));
        }

        var key = $"REC-{Guid.NewGuid():N}";
        var payload = new
        {
            PedidoId = pedidoId,
            Documento = documento.Trim(),
            IdempotencyKey = key,
            Itens = new[]
            {
                new
                {
                    PedidoItemId = pedidoItemId,
                    Quantidade = quantidade,
                    Lote = string.IsNullOrWhiteSpace(lote) ? "LOTE-PADRAO" : lote.Trim(),
                    Validade = validade,
                    LocalId = localId
                }
            }
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/compras/recebimentos", payload);
        if (resp.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created)
            TempData["SuccessMessage"] = "Recebimento confirmado com sucesso. Material direcionado para quarentena/inspeção e obrigação gerada no financeiro.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao registrar recebimento.";

        return RedirectToAction(nameof(Recebimentos));
    }

    // ==========================================
    // QUALIDADE E INSPEÇÕES
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Inspecoes()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<IReadOnlyList<InspecaoPendenteViewModel>>(client, "api/administrativo360/qualidade/pendentes");

        return View(new InspecoesIndexViewModel
        {
            Pendentes = resp.Data ?? Array.Empty<InspecaoPendenteViewModel>(),
            Erro = resp.Error
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DecidirInspecao(Guid recebimentoItemId, decimal aprovada, decimal reprovada, string? justificativa, string? destino)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        if (aprovada + reprovada <= 0)
        {
            TempData["ErrorMessage"] = "Informe uma quantidade aprovada ou reprovada.";
            return RedirectToAction(nameof(Inspecoes));
        }

        if (reprovada > 0 && string.IsNullOrWhiteSpace(justificativa))
        {
            TempData["ErrorMessage"] = "A reprovação exige justificativa técnica obrigatória.";
            return RedirectToAction(nameof(Inspecoes));
        }

        var payload = new
        {
            RecebimentoItemId = recebimentoItemId,
            Aprovada = aprovada,
            Reprovada = reprovada,
            Justificativa = string.IsNullOrWhiteSpace(justificativa) ? "Inspeção visual e documental conforme normas da Qualidade" : justificativa.Trim(),
            Destino = string.IsNullOrWhiteSpace(destino) ? "ALMOXARIFADO_LIBERADO" : destino.Trim(),
            IdempotencyKey = $"INSP-{Guid.NewGuid():N}"
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/qualidade/decisoes", payload);
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["SuccessMessage"] = "Decisão de inspeção registrada com sucesso. Material liberado para estoque conforme quantidade aprovada.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao registrar decisão da inspeção.";

        return RedirectToAction(nameof(Inspecoes));
    }

    [HttpGet]
    public async Task<IActionResult> Ocorrencias()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<IReadOnlyList<InspecaoPendenteViewModel>>(client, "api/administrativo360/qualidade/pendentes");
        var lista = (resp.Data ?? Array.Empty<InspecaoPendenteViewModel>()).Select(p => new OcorrenciaViewModel(
            p.RecebimentoItemId, "QUARENTENA_RECEBIMENTO",
            $"Item em quarentena aguardando laudo de qualidade: {p.Produto} (Lote: {p.Lote})",
            p.Pendente, "ABERTA", DateOnly.FromDateTime(DateTime.Today.AddDays(2)), p.Local, DateTimeOffset.UtcNow
        )).ToList();

        return View(new OcorrenciasIndexViewModel
        {
            Ocorrencias = lista,
            Erro = resp.Error
        });
    }

    // ==========================================
    // ESTOQUE, LOTES E MOVIMENTAÇÕES
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Estoque(string? busca, string? condicao, Guid? localId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var q = new List<string>();
        if (!string.IsNullOrWhiteSpace(busca)) q.Add($"busca={Uri.EscapeDataString(busca)}");
        if (!string.IsNullOrWhiteSpace(condicao)) q.Add($"condicao={Uri.EscapeDataString(condicao)}");
        if (localId.HasValue && localId.Value != Guid.Empty) q.Add($"localId={localId.Value}");

        var query = "api/administrativo360/estoque" + (q.Count > 0 ? "?" + string.Join("&", q) : "");
        var resp = await ReadApiResponse<IReadOnlyList<SaldoEstoqueViewModel>>(client, query);
        var lookups = await CarregarLookupsAsync(client);

        return View(new EstoqueIndexViewModel
        {
            Saldos = resp.Data ?? Array.Empty<SaldoEstoqueViewModel>(),
            Locais = lookups.Locais,
            Busca = busca,
            Condicao = condicao,
            LocalId = localId,
            Erro = resp.Error
        });
    }

    [HttpGet]
    public async Task<IActionResult> Movimentacoes()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<IReadOnlyList<SaldoEstoqueViewModel>>(client, "api/administrativo360/estoque");
        var lookups = await CarregarLookupsAsync(client);

        return View(new MovimentacoesIndexViewModel
        {
            Saldos = resp.Data ?? Array.Empty<SaldoEstoqueViewModel>(),
            Locais = lookups.Locais,
            Erro = resp.Error
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferirEstoque(Guid produtoId, Guid loteId, Guid origemId, Guid destinoId, decimal quantidade, string motivo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        if (origemId == destinoId)
        {
            TempData["ErrorMessage"] = "Local de origem e destino devem ser diferentes.";
            return RedirectToAction(nameof(Movimentacoes));
        }

        if (quantidade <= 0)
        {
            TempData["ErrorMessage"] = "Quantidade a transferir deve ser positiva.";
            return RedirectToAction(nameof(Movimentacoes));
        }

        var payload = new
        {
            ProdutoId = produtoId,
            LoteId = loteId,
            OrigemId = origemId,
            DestinoId = destinoId,
            Quantidade = quantidade,
            Motivo = string.IsNullOrWhiteSpace(motivo) ? "Transferência interna entre locais de armazenagem" : motivo.Trim(),
            IdempotencyKey = $"TRANSF-{Guid.NewGuid():N}"
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/estoque/transferencias", payload);
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["SuccessMessage"] = "Transferência de estoque concluída com sucesso.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao transferir estoque.";

        return RedirectToAction(nameof(Movimentacoes));
    }

    // ==========================================
    // INVENTÁRIOS E AJUSTES DE ESTOQUE
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Inventarios()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<IReadOnlyList<InventarioResumoViewModel>>(client, "api/administrativo360/inventarios");
        var lookups = await CarregarLookupsAsync(client);

        return View(new InventariosIndexViewModel
        {
            Inventarios = resp.Data ?? Array.Empty<InventarioResumoViewModel>(),
            Locais = lookups.Locais,
            Produtos = lookups.Produtos,
            Lotes = lookups.Lotes,
            Erro = resp.Error
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AbrirInventario(Guid localId, string escopo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new { LocalId = localId, Escopo = string.IsNullOrWhiteSpace(escopo) ? "Contagem geral de estoque" : escopo.Trim() };
        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/inventarios", payload);

        if (resp.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created)
            TempData["SuccessMessage"] = "Inventário aberto com sucesso. O local está em contagem de estoque.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao abrir inventário.";

        return RedirectToAction(nameof(Inventarios));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ContarInventario(Guid inventarioId, Guid produtoId, Guid loteId, decimal quantidade, string? condicao)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new
        {
            ProdutoId = produtoId,
            LoteId = loteId,
            Quantidade = quantidade,
            Condicao = string.IsNullOrWhiteSpace(condicao) ? "LIBERADO" : condicao
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Put, $"api/administrativo360/inventarios/{inventarioId}/contagens", payload);
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["SuccessMessage"] = "Contagem registrada no inventário.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao registrar contagem.";

        return RedirectToAction(nameof(Inventarios));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AprovarInventario(Guid inventarioId, string? justificativa)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", $"INV-APROV-{inventarioId:N}");

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("justificativa", string.IsNullOrWhiteSpace(justificativa) ? "Ajuste de inventário aprovado pela gerência" : justificativa.Trim())
        });

        var message = new HttpRequestMessage(HttpMethod.Post, $"api/administrativo360/inventarios/{inventarioId}/aprovar") { Content = formContent };
        var response = await client.SendAsync(message);

        if (response.IsSuccessStatusCode)
            TempData["SuccessMessage"] = "Inventário aprovado e ajustes de saldo consolidados com sucesso.";
        else
            TempData["ErrorMessage"] = "Falha ao aprovar inventário.";

        return RedirectToAction(nameof(Inventarios));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarInventario(Guid inventarioId, string? motivo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("motivo", string.IsNullOrWhiteSpace(motivo) ? "Cancelado pelo usuário" : motivo.Trim())
        });

        var message = new HttpRequestMessage(HttpMethod.Post, $"api/administrativo360/inventarios/{inventarioId}/cancelar") { Content = formContent };
        var response = await client.SendAsync(message);

        if (response.IsSuccessStatusCode)
            TempData["SuccessMessage"] = "Inventário cancelado com sucesso.";
        else
            TempData["ErrorMessage"] = "Falha ao cancelar inventário.";

        return RedirectToAction(nameof(Inventarios));
    }

    // ==========================================
    // COLETA MÓVEL
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Coleta()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<IReadOnlyList<TarefaColetaViewModel>>(client, "api/administrativo360/coleta/tarefas");

        return View(new ColetaIndexViewModel
        {
            Tarefas = resp.Data ?? Array.Empty<TarefaColetaViewModel>(),
            Erro = resp.Error
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarLeitura(Guid tarefaId, string codigo, string? lote, decimal quantidade)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        if (string.IsNullOrWhiteSpace(codigo) || quantidade <= 0)
        {
            TempData["ErrorMessage"] = "Código de barras e quantidade positiva são obrigatórios.";
            return RedirectToAction(nameof(Coleta));
        }

        var payload = new
        {
            TarefaId = tarefaId,
            ScanId = Guid.NewGuid(),
            Codigo = codigo.Trim(),
            Lote = lote?.Trim(),
            Quantidade = quantidade
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/coleta/leituras", payload);
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["SuccessMessage"] = "Leitura de código de barras registrada com sucesso na tarefa.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao registrar leitura.";

        return RedirectToAction(nameof(Coleta));
    }

    // ==========================================
    // ORÇAMENTOS CIRÚRGICOS E RESERVAS DE MATERIAIS
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Orcamentos(string? busca, string? situacao, DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = $"api/administrativo360/orcamentos?busca={Uri.EscapeDataString(busca ?? "")}&situacao={Uri.EscapeDataString(situacao ?? "")}";
        if (inicio.HasValue) query += $"&inicio={inicio.Value:yyyy-MM-dd}";
        if (fim.HasValue) query += $"&fim={fim.Value:yyyy-MM-dd}";

        var resp = await ReadApiResponse<IReadOnlyList<OrcamentoResumoViewModel>>(client, query);
        ViewBag.Busca = busca;
        ViewBag.Situacao = situacao;
        ViewBag.Inicio = inicio;
        ViewBag.Fim = fim;
        ViewBag.Erro = resp.Error;

        return View(resp.Data ?? Array.Empty<OrcamentoResumoViewModel>());
    }

    [HttpGet]
    public async Task<IActionResult> OrcamentoNovo()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var model = new OrcamentoFormViewModel();
        await PreencherLookupsOrcamentoAsync(client, model);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OrcamentoNovo(OrcamentoFormViewModel form)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        if (form.HospitalId == Guid.Empty || form.ResponsavelFinanceiroId == Guid.Empty || string.IsNullOrWhiteSpace(form.Procedimento))
        {
            TempData["ErrorMessage"] = "Hospital, Responsável Financeiro e Procedimento são obrigatórios.";
            await PreencherLookupsOrcamentoAsync(client, form);
            return View(form);
        }

        if (form.Itens.Count == 0 || form.Itens.All(x => x.Quantidade <= 0))
        {
            TempData["ErrorMessage"] = "Informe ao menos um produto com quantidade positiva.";
            await PreencherLookupsOrcamentoAsync(client, form);
            return View(form);
        }

        var payload = new
        {
            form.HospitalId,
            form.MedicoId,
            Procedimento = form.Procedimento.Trim(),
            form.ResponsavelFinanceiroId,
            form.VendedorId,
            form.DataPrevista,
            form.Validade,
            Observacoes = form.Observacoes?.Trim(),
            Itens = form.Itens.Where(i => i.Quantidade > 0).Select(i => new
            {
                i.ProdutoId,
                i.Quantidade,
                i.PrecoUnitario,
                i.Desconto
            }).ToArray()
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/orcamentos", payload);
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
        {
            TempData["SuccessMessage"] = "Orçamento cirúrgico cadastrado em rascunho com sucesso.";
            return RedirectToAction(nameof(Orcamentos));
        }

        TempData["ErrorMessage"] = resp.Error ?? "Falha ao cadastrar orçamento.";
        await PreencherLookupsOrcamentoAsync(client, form);
        return View(form);
    }

    [HttpGet]
    public async Task<IActionResult> OrcamentoEditar(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<OrcamentoDetalhesViewModel>(client, $"api/administrativo360/orcamentos/{id}");
        if (resp.Data is null)
        {
            TempData["ErrorMessage"] = resp.Error ?? "Orçamento não encontrado.";
            return RedirectToAction(nameof(Orcamentos));
        }

        var d = resp.Data;
        var form = new OrcamentoFormViewModel
        {
            Id = d.Id,
            HospitalId = d.HospitalId,
            MedicoId = d.MedicoId,
            Procedimento = d.Procedimento,
            ResponsavelFinanceiroId = d.ResponsavelFinanceiroId,
            VendedorId = d.VendedorId,
            DataPrevista = d.DataPrevista,
            Validade = d.Validade,
            Observacoes = d.Observacoes,
            Itens = d.Itens.Select(i => new OrcamentoItemInputModel
            {
                ProdutoId = i.ProdutoId,
                Quantidade = i.Quantidade,
                PrecoUnitario = i.PrecoUnitario,
                Desconto = i.Desconto
            }).ToList()
        };

        ViewBag.Situacao = d.Situacao;
        ViewBag.Numero = d.Numero;
        ViewBag.Revisao = d.Revisao;
        return View(form);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OrcamentoEditar(Guid id, OrcamentoFormViewModel form)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new
        {
            OrcamentoId = id,
            form.HospitalId,
            form.MedicoId,
            Procedimento = form.Procedimento.Trim(),
            form.ResponsavelFinanceiroId,
            form.VendedorId,
            form.DataPrevista,
            form.Validade,
            Observacoes = form.Observacoes?.Trim(),
            MotivoRevisao = form.MotivoRevisao?.Trim(),
            Itens = form.Itens.Where(i => i.Quantidade > 0).Select(i => new
            {
                i.ProdutoId,
                i.Quantidade,
                i.PrecoUnitario,
                i.Desconto
            }).ToArray()
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Put, $"api/administrativo360/orcamentos/{id}", payload);
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
        {
            TempData["SuccessMessage"] = "Orçamento cirúrgico atualizado com sucesso.";
            return RedirectToAction(nameof(OrcamentoDetalhes), new { id });
        }

        TempData["ErrorMessage"] = resp.Error ?? "Falha ao atualizar orçamento.";
        return RedirectToAction(nameof(OrcamentoEditar), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> OrcamentoDetalhes(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<OrcamentoDetalhesViewModel>(client, $"api/administrativo360/orcamentos/{id}");
        if (resp.Data is null)
        {
            TempData["ErrorMessage"] = resp.Error ?? "Orçamento não encontrado.";
            return RedirectToAction(nameof(Orcamentos));
        }

        var revisoes = await ReadApiResponse<IReadOnlyList<OrcamentoRevisaoHistoricoViewModel>>(client, $"api/administrativo360/orcamentos/{id}/revisoes");
        ViewBag.Revisoes = revisoes.Data ?? Array.Empty<OrcamentoRevisaoHistoricoViewModel>();

        return View(resp.Data);
    }

    [HttpGet]
    public async Task<IActionResult> OrcamentoImprimir(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<OrcamentoDetalhesViewModel>(client, $"api/administrativo360/orcamentos/{id}");
        if (resp.Data is null)
        {
            TempData["ErrorMessage"] = resp.Error ?? "Orçamento não encontrado.";
            return RedirectToAction(nameof(Orcamentos));
        }

        return View(resp.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OrcamentoAprovar(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/orcamentos/{id}/aprovar", new { });
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
        {
            TempData["SuccessMessage"] = "Orçamento aprovado com sucesso! A versão comercial está congelada e apta para reserva de materiais.";
        }
        else
        {
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao aprovar orçamento.";
        }

        return RedirectToAction(nameof(OrcamentoDetalhes), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OrcamentoRejeitar(Guid id, string motivo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/orcamentos/{id}/rejeitar", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("motivo", motivo)
        }));

        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
        {
            TempData["SuccessMessage"] = "Orçamento rejeitado.";
        }
        else
        {
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao rejeitar orçamento.";
        }

        return RedirectToAction(nameof(OrcamentoDetalhes), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OrcamentoCancelar(Guid id, string motivo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/orcamentos/{id}/cancelar", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("motivo", motivo)
        }));

        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
        {
            TempData["SuccessMessage"] = "Orçamento cancelado e reservas ativas liberadas.";
        }
        else
        {
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao cancelar orçamento.";
        }

        return RedirectToAction(nameof(OrcamentoDetalhes), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> OrcamentoReserva(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<PlanejamentoReservaOrcamentoViewModel>(client, $"api/administrativo360/orcamentos/{id}/reserva-planejamento");
        if (resp.Data is null)
        {
            TempData["ErrorMessage"] = resp.Error ?? "Orçamento não encontrado ou ainda não aprovado para reserva.";
            return RedirectToAction(nameof(OrcamentoDetalhes), new { id });
        }

        return View(resp.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OrcamentoReservarItem(Guid id, Guid produtoId, Guid loteId, Guid localId, decimal quantidade)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new
        {
            ProdutoId = produtoId,
            LoteId = loteId,
            LocalId = localId,
            Quantidade = quantidade,
            Origem = "ORCAMENTO_CIRURGICO",
            OrigemId = id,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/orcamentos/{id}/reservas", payload);
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
        {
            TempData["SuccessMessage"] = "Material reservado com sucesso para a cirurgia.";
        }
        else
        {
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao realizar reserva de material.";
        }

        return RedirectToAction(nameof(OrcamentoReserva), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OrcamentoCancelarReserva(Guid id, Guid reservaId, string motivo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = $"api/administrativo360/orcamentos/{id}/reservas/{reservaId}?motivo={Uri.EscapeDataString(motivo ?? "")}";
        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Delete, query, new { });

        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
        {
            TempData["SuccessMessage"] = "Reserva de material cancelada com sucesso.";
        }
        else
        {
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao cancelar reserva.";
        }

        return RedirectToAction(nameof(OrcamentoReserva), new { id });
    }

    // ==========================================
    // CADASTROS BÁSICOS
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

    [HttpGet]
    public async Task<IActionResult> Vales(string? busca, string? situacao, DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(busca)) query.Add($"busca={Uri.EscapeDataString(busca)}");
        if (!string.IsNullOrWhiteSpace(situacao)) query.Add($"situacao={Uri.EscapeDataString(situacao)}");
        if (inicio.HasValue) query.Add($"inicio={inicio:yyyy-MM-dd}");
        if (fim.HasValue) query.Add($"fim={fim:yyyy-MM-dd}");

        var path = "api/administrativo360/vales" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var res = await ReadApiResponse<List<ValeResumoViewModel>>(client, path);

        ViewBag.Busca = busca;
        ViewBag.Situacao = situacao;
        ViewBag.Inicio = inicio;
        ViewBag.Fim = fim;

        return View(res.Data ?? new List<ValeResumoViewModel>());
    }

    [HttpGet]
    public async Task<IActionResult> ValeNovo(Guid? orcamentoId, Guid? cirurgiaId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var model = new ValeFormViewModel
        {
            OrcamentoId = orcamentoId,
            CirurgiaId = cirurgiaId
        };

        if (cirurgiaId.HasValue)
        {
            var res = await ReadApiResponse<CirurgiaDetalhesViewModel>(client, $"api/administrativo360/cirurgias/{cirurgiaId.Value}");
            var cirurgia = res.Data;
            if (cirurgia is not null)
            {
                model.HospitalId = cirurgia.HospitalId;
                model.LocalDestinoId = cirurgia.LocalDestinoId;
                model.OrcamentoId ??= cirurgia.OrcamentoId;
                model.OrcamentoRevisao = cirurgia.OrcamentoRevisao;
                model.DataSaidaPrevista = cirurgia.DataPrevista;
                model.DataRetornoPrevista = cirurgia.DataPrevista.AddDays(7);
            }
        }
        else if (orcamentoId.HasValue)
        {
            var res = await ReadApiResponse<OrcamentoDetalhesViewModel>(client, $"api/administrativo360/orcamentos/{orcamentoId.Value}");
            var orc = res.Data;
            if (orc is not null)
            {
                model.HospitalId = orc.HospitalId;
                model.OrcamentoRevisao = orc.Revisao;
                model.DataSaidaPrevista = orc.DataPrevista;
                model.DataRetornoPrevista = orc.DataPrevista.AddDays(7);
            }
        }

        await PreencherLookupsValeAsync(client, model);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ValeNovo(ValeFormViewModel model)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        if (model.Itens.Count == 0)
        {
            TempData["ErrorMessage"] = "Adicione ao menos um item ao vale de consignação.";
            await PreencherLookupsValeAsync(client, model);
            return View(model);
        }

        var payload = new
        {
            model.CirurgiaId,
            model.OrcamentoId,
            model.OrcamentoRevisao,
            model.HospitalId,
            model.CustodianteId,
            model.LocalOrigemId,
            model.LocalDestinoId,
            model.DataSaidaPrevista,
            model.DataRetornoPrevista,
            model.Observacoes,
            Itens = model.Itens.Select(i => new
            {
                i.ProdutoId,
                i.LoteId,
                i.ReservaId,
                i.QuantidadeSolicitada,
                i.PrecoUnitario
            }).ToList(),
            IdempotencyKey = Guid.NewGuid().ToString("N")
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/vales", payload);
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
        {
            TempData["SuccessMessage"] = "Vale de consignação criado com sucesso.";
            return RedirectToAction(nameof(Vales));
        }

        TempData["ErrorMessage"] = resp.Error ?? "Falha ao criar vale de consignação.";
        await PreencherLookupsValeAsync(client, model);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ValeDetalhes(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<ValeDetalhesViewModel>(client, $"api/administrativo360/vales/{id}");
        if (resp.Data is null) return NotFound();

        return View(resp.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ValeSepararItem(Guid id, Guid itemId, decimal quantidade)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new { ValeItemId = itemId, QuantidadeSeparada = quantidade };
        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/vales/{id}/separar-item", payload);

        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["SuccessMessage"] = "Conferência do item registrada.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao registrar conferência do item.";

        return RedirectToAction(nameof(ValeDetalhes), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ValeConcluirSeparacao(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/vales/{id}/concluir-separacao", new { });

        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["SuccessMessage"] = "Separação de materiais concluída. Vale pronto para expedição.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao concluir separação de materiais.";

        return RedirectToAction(nameof(ValeDetalhes), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ValeExpedir(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new { ValeId = id, IdempotencyKey = Guid.NewGuid().ToString("N") };
        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/vales/{id}/expedir", payload);

        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["SuccessMessage"] = "Vale expedido com sucesso! Materiais transferidos para custódia externa no hospital.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao expedir vale.";

        return RedirectToAction(nameof(ValeDetalhes), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ValeEvento(Guid id, Guid itemId, string tipo, decimal quantidade, string? motivo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var endpoint = tipo switch
        {
            "CONSUMO" => $"api/administrativo360/vales/{id}/consumo",
            "RETORNO" => $"api/administrativo360/vales/{id}/retorno",
            "PERDA" => $"api/administrativo360/vales/{id}/perda",
            _ => throw new ArgumentException("Tipo de evento inválido.")
        };

        var payload = new
        {
            ValeId = id,
            ValeItemId = itemId,
            Quantidade = quantidade,
            Motivo = motivo,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, endpoint, payload);

        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["SuccessMessage"] = $"Evento de {tipo} registrado com sucesso.";
        else
            TempData["ErrorMessage"] = resp.Error ?? $"Falha ao registrar evento de {tipo}.";

        return RedirectToAction(nameof(ValeDetalhes), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ValeReconciliar(Guid id, string? observacoes)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new
        {
            ValeId = id,
            Observacoes = observacoes,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/vales/{id}/reconciliar", payload);

        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["SuccessMessage"] = "Vale reconciliado com sucesso! Pronto para valorização no próximo incremento.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao reconciliar vale.";

        return RedirectToAction(nameof(ValeDetalhes), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ValeCancelar(Guid id, string motivo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new { ValeId = id, Motivo = motivo };
        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/vales/{id}/cancelar", payload);

        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["SuccessMessage"] = "Vale cancelado com sucesso.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao cancelar vale.";

        return RedirectToAction(nameof(ValeDetalhes), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> ValeImprimir(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<ValeDetalhesViewModel>(client, $"api/administrativo360/vales/{id}");
        if (resp.Data is null) return NotFound();

        return View(resp.Data);
    }

    // ==========================================
    // RELATÓRIOS FUNCIONAIS E EXPORTAÇÃO CSV
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Relatorios(string? aba, string? busca)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        aba = string.IsNullOrWhiteSpace(aba) ? "pendentes" : aba.ToLowerInvariant();

        var respPendentes = await ReadApiResponse<List<RelatorioValesPendentesViewModel>>(client, "api/administrativo360/relatorios/vales-pendentes");
        var pendentes = respPendentes.Data ?? new();

        var respCustodia = await ReadApiResponse<List<RelatorioCustodiaExternaViewModel>>(client, "api/administrativo360/relatorios/custodia-externa");
        var custodia = respCustodia.Data ?? new();

        var respReconciliacao = await ReadApiResponse<List<RelatorioReconciliacaoViewModel>>(client, "api/administrativo360/relatorios/reconciliacao");
        var reconciliacao = respReconciliacao.Data ?? new();

        var rastreioPath = "api/administrativo360/relatorios/rastreabilidade" + (!string.IsNullOrWhiteSpace(busca) ? $"?busca={Uri.EscapeDataString(busca)}" : "");
        var respRastreio = await ReadApiResponse<List<RelatorioRastreabilidadeViewModel>>(client, rastreioPath);
        var rastreabilidade = respRastreio.Data ?? new();

        var model = new Adm360RelatoriosIndexViewModel
        {
            AbaAtiva = aba,
            Busca = busca,
            ValesPendentes = pendentes,
            CustodiaExterna = custodia,
            Reconciliacao = reconciliacao,
            Rastreabilidade = rastreabilidade
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportarValesPendentesCsv()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<List<RelatorioValesPendentesViewModel>>(client, "api/administrativo360/relatorios/vales-pendentes");
        var itens = resp.Data ?? new();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Vale;Hospital;Cirurgia;DataSaida;RetornoPrevisto;QuantidadePendente;Responsavel;DiasAtraso");

        foreach (var it in itens)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.Numero),
                SanitizarCsv(it.Hospital),
                SanitizarCsv(it.CirurgiaNumero ?? ""),
                it.DataSaida?.ToString("yyyy-MM-dd HH:mm") ?? "",
                it.DataRetornoPrevista?.ToString("yyyy-MM-dd") ?? "",
                it.QuantidadePendente.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                SanitizarCsv(it.Responsavel),
                it.DiasAtraso.ToString()));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"vales_pendentes_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarCustodiaExternaCsv()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<List<RelatorioCustodiaExternaViewModel>>(client, "api/administrativo360/relatorios/custodia-externa");
        var itens = resp.Data ?? new();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Local;Produto;Sku;Lote;Validade;Hospital;Vale;Quantidade");

        foreach (var it in itens)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.Local),
                SanitizarCsv(it.Produto),
                SanitizarCsv(it.Sku),
                SanitizarCsv(it.Lote),
                it.Validade?.ToString("yyyy-MM-dd") ?? "",
                SanitizarCsv(it.Hospital),
                SanitizarCsv(it.ValeNumero),
                it.Quantidade.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"custodia_externa_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarReconciliacaoCsv()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<List<RelatorioReconciliacaoViewModel>>(client, "api/administrativo360/relatorios/reconciliacao");
        var itens = resp.Data ?? new();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Vale;Hospital;Cirurgia;TotalExpedido;TotalConsumido;TotalDevolvido;TotalPerda;PendenteCustodia;Situacao");

        foreach (var it in itens)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.Numero),
                SanitizarCsv(it.Hospital),
                SanitizarCsv(it.Cirurgia ?? ""),
                it.TotalExpedido.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.TotalConsumido.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.TotalDevolvido.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.TotalPerda.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.PendenteCustodia.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                SanitizarCsv(it.Situacao)));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"reconciliacao_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarRastreabilidadeCsv(string? busca)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var path = "api/administrativo360/relatorios/rastreabilidade" + (!string.IsNullOrWhiteSpace(busca) ? $"?busca={Uri.EscapeDataString(busca)}" : "");
        var resp = await ReadApiResponse<List<RelatorioRastreabilidadeViewModel>>(client, path);
        var itens = resp.Data ?? new();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Produto;Lote;Validade;OrigemTipo;Documento;Vale;Hospital;LocalAtual;Condicao;Quantidade;DataMovimento");

        foreach (var it in itens)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.Produto),
                SanitizarCsv(it.Lote),
                it.Validade?.ToString("yyyy-MM-dd") ?? "",
                SanitizarCsv(it.OrigemTipo),
                SanitizarCsv(it.DocumentoOrigem ?? ""),
                SanitizarCsv(it.ValeNumero ?? ""),
                SanitizarCsv(it.Hospital ?? ""),
                SanitizarCsv(it.LocalAtual),
                SanitizarCsv(it.Condicao),
                it.Quantidade.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.DataMovimento.ToString("yyyy-MM-dd HH:mm")));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"rastreabilidade_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    // ==========================================
    // BLOCO B - VALORIZAÇÃO E VENDA INTERNA
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Valorizacao(string? busca)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var path = "api/administrativo360/valorizacoes/pendentes";
        var resp = await ReadApiResponse<List<ValeResumoViewModel>>(client, path);
        var pendentes = resp.Data ?? new List<ValeResumoViewModel>();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            pendentes = pendentes.Where(v =>
                v.Numero.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                (v.Hospital != null && v.Hospital.Contains(busca, StringComparison.OrdinalIgnoreCase)) ||
                (v.CirurgiaNumero != null && v.CirurgiaNumero.Contains(busca, StringComparison.OrdinalIgnoreCase)) ||
                (v.OrcamentoNumero != null && v.OrcamentoNumero.Contains(busca, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        ViewBag.Busca = busca;
        ViewBag.Erro = resp.Error;

        return View(new ValorizacaoIndexViewModel
        {
            ValesPendentes = pendentes,
            Busca = busca
        });
    }

    [HttpGet]
    public async Task<IActionResult> ValorizacaoPrevia(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<PreviaValorizacaoViewModel>(client, $"api/administrativo360/valorizacoes/previa/{id}");
        if (resp.Data is null)
        {
            TempData["ErrorMessage"] = resp.Error ?? "Vale não encontrado para valorização prévia.";
            return RedirectToAction(nameof(Valorizacao));
        }

        return View(new ValorizacaoPreviaViewModel
        {
            Previa = resp.Data
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarValorizacao(
        Guid valeId,
        Guid pagadorId,
        Guid? vendedorId,
        decimal descontoGeral,
        decimal comissaoPercentual,
        string condicaoPagamento,
        int quantidadeParcelas,
        string? observacoes,
        string? idempotencyKey)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString("N") : idempotencyKey;

        // 1. Executa a valorização
        var cmdVal = new
        {
            ValeId = valeId,
            PagadorId = pagadorId,
            VendedorId = vendedorId,
            DescontoGeral = descontoGeral,
            ComissaoPercentual = comissaoPercentual,
            IdempotencyKey = $"VAL-{key}",
            Observacoes = observacoes
        };

        var respVal = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, "api/administrativo360/valorizacoes", cmdVal);

        if (respVal.StatusCode is not (System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created))
        {
            TempData["ErrorMessage"] = respVal.Error ?? "Erro ao valorizar o vale.";
            return RedirectToAction(nameof(ValorizacaoPrevia), new { id = valeId });
        }

        Guid valorizacaoId = Guid.Empty;
        if (respVal.Data.TryGetProperty("id", out var idProp) && idProp.TryGetGuid(out var valId))
        {
            valorizacaoId = valId;
        }

        // 2. Confirma a venda correspondente gerando títulos de cobrança
        var cmdVenda = new
        {
            ValorizacaoId = valorizacaoId,
            CondicaoPagamento = string.IsNullOrWhiteSpace(condicaoPagamento) ? "A_VISTA" : condicaoPagamento,
            QuantidadeParcelas = quantidadeParcelas <= 0 ? 1 : quantidadeParcelas,
            IdempotencyKey = $"VEN-{key}",
            Observacoes = observacoes
        };

        var respVenda = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, "api/administrativo360/vendas/confirmar", cmdVenda);

        if (respVenda.StatusCode is not (System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created))
        {
            TempData["ErrorMessage"] = respVenda.Error ?? "Vale valorizado, mas houve erro ao gerar a venda e títulos.";
            return RedirectToAction(nameof(Vendas));
        }

        Guid vendaId = Guid.Empty;
        if (respVenda.Data.TryGetProperty("id", out var vProp) && vProp.TryGetGuid(out var vId))
        {
            vendaId = vId;
        }

        TempData["SuccessMessage"] = "Vale valorizado com sucesso e venda interna gerada!";
        return RedirectToAction(nameof(VendaDetalhes), new { id = vendaId != Guid.Empty ? vendaId : valorizacaoId });
    }

    [HttpGet]
    public async Task<IActionResult> Vendas(string? busca, string? situacao, DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(busca)) query.Add($"busca={Uri.EscapeDataString(busca)}");
        if (!string.IsNullOrWhiteSpace(situacao)) query.Add($"situacao={Uri.EscapeDataString(situacao)}");
        if (inicio.HasValue) query.Add($"inicio={inicio:yyyy-MM-dd}");
        if (fim.HasValue) query.Add($"fim={fim:yyyy-MM-dd}");

        var path = "api/administrativo360/vendas" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var resp = await ReadApiResponse<List<VendaResumoViewModel>>(client, path);

        return View(new VendasIndexViewModel
        {
            Vendas = (IReadOnlyList<VendaResumoViewModel>?)resp.Data ?? Array.Empty<VendaResumoViewModel>(),
            Busca = busca,
            Situacao = situacao,
            Inicio = inicio,
            Fim = fim
        });
    }

    [HttpGet]
    public async Task<IActionResult> VendaDetalhes(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<VendaDetalhesViewModel>(client, $"api/administrativo360/vendas/{id}");
        if (resp.Data is null)
        {
            TempData["ErrorMessage"] = resp.Error ?? "Venda não encontrada.";
            return RedirectToAction(nameof(Vendas));
        }

        return View(new VendaDetalhesPageViewModel { Venda = resp.Data });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarVenda(Guid id, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            TempData["ErrorMessage"] = "O motivo do cancelamento é obrigatório.";
            return RedirectToAction(nameof(VendaDetalhes), new { id });
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await SendApiAsync<string, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, $"api/administrativo360/vendas/{id}/cancelar", motivo);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Venda cancelada com sucesso." : resp.Error;
        return RedirectToAction(nameof(VendaDetalhes), new { id });
    }

    // ==========================================
    // BLOCO C - CONTAS A RECEBER E TÍTULOS
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> TitulosReceber(string? busca, string? situacao, Guid? pagadorId, DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(busca)) query.Add($"busca={Uri.EscapeDataString(busca)}");
        if (!string.IsNullOrWhiteSpace(situacao)) query.Add($"situacao={Uri.EscapeDataString(situacao)}");
        if (pagadorId.HasValue) query.Add($"pagadorId={pagadorId.Value}");
        if (inicio.HasValue) query.Add($"inicio={inicio:yyyy-MM-dd}");
        if (fim.HasValue) query.Add($"fim={fim:yyyy-MM-dd}");

        var path = "api/administrativo360/titulos" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var resp = await ReadApiResponse<List<TituloReceberResumoViewModel>>(client, path);

        return View(new TitulosIndexViewModel
        {
            Titulos = (IReadOnlyList<TituloReceberResumoViewModel>?)resp.Data ?? Array.Empty<TituloReceberResumoViewModel>(),
            Busca = busca,
            Situacao = situacao,
            PagadorId = pagadorId,
            Inicio = inicio,
            Fim = fim
        });
    }

    [HttpGet]
    public async Task<IActionResult> TituloDetalhes(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<TituloReceberDetalhesViewModel>(client, $"api/administrativo360/titulos/{id}");
        if (resp.Data is null)
        {
            TempData["ErrorMessage"] = resp.Error ?? "Título não encontrado.";
            return RedirectToAction(nameof(TitulosReceber));
        }

        var contasResp = await ReadApiResponse<List<ContaFinanceiraViewModel>>(client, "api/administrativo360/caixa/contas");

        return View(new TituloDetalhesPageViewModel
        {
            Titulo = resp.Data,
            Contas = (IReadOnlyList<ContaFinanceiraViewModel>?)contasResp.Data ?? Array.Empty<ContaFinanceiraViewModel>()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReceberTitulo(
        Guid id,
        Guid contaFinanceiraId,
        DateOnly dataRecebimento,
        decimal valorRecebido,
        string meioPagamento,
        string? referencia,
        string? observacoes,
        string? idempotencyKey)
    {
        if (valorRecebido <= 0m)
        {
            TempData["ErrorMessage"] = "O valor recebido deve ser positivo.";
            return RedirectToAction(nameof(TituloDetalhes), new { id });
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString("N") : idempotencyKey;

        var cmd = new
        {
            TituloId = id,
            ContaFinanceiraId = contaFinanceiraId,
            DataRecebimento = dataRecebimento,
            ValorRecebido = valorRecebido,
            MeioPagamento = meioPagamento,
            Referencia = referencia,
            IdempotencyKey = key,
            Observacoes = observacoes
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, "api/administrativo360/titulos/receber", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Recebimento registrado manualmente com sucesso." : resp.Error;
        return RedirectToAction(nameof(TituloDetalhes), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EstornarRecebimento(Guid tituloId, Guid baixaId, string motivo, string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            TempData["ErrorMessage"] = "O motivo do estorno é obrigatório.";
            return RedirectToAction(nameof(TituloDetalhes), new { id = tituloId });
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString("N") : idempotencyKey;

        var cmd = new
        {
            BaixaId = baixaId,
            Motivo = motivo,
            IdempotencyKey = key
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, "api/administrativo360/titulos/estornar", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Baixa estornada com sucesso. Saldo e caixa revertidos." : resp.Error;
        return RedirectToAction(nameof(TituloDetalhes), new { id = tituloId });
    }

    // ==========================================
    // BLOCO C/D - FLUXO DE CAIXA E CONTAS
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> FluxoCaixa(DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var dtInicio = inicio ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var dtFim = fim ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

        var contasResp = await ReadApiResponse<List<ContaFinanceiraViewModel>>(client, "api/administrativo360/caixa/contas");
        var fluxoResp = await ReadApiResponse<FluxoCaixaViewModel>(client, $"api/administrativo360/caixa/fluxo?inicio={dtInicio:yyyy-MM-dd}&fim={dtFim:yyyy-MM-dd}");

        return View(new FluxoCaixaPageViewModel
        {
            Contas = (IReadOnlyList<ContaFinanceiraViewModel>?)contasResp.Data ?? Array.Empty<ContaFinanceiraViewModel>(),
            Fluxo = fluxoResp.Data ?? new FluxoCaixaViewModel(DateOnly.FromDateTime(DateTime.UtcNow), 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, Array.Empty<FluxoCaixaItemViewModel>()),
            Inicio = dtInicio,
            Fim = dtFim
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarContaFinanceira(
        string nome,
        string tipo,
        string? banco,
        string? agencia,
        string? conta,
        decimal saldoInicial,
        DateOnly? dataSaldoInicial)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            TempData["ErrorMessage"] = "O nome da conta financeira é obrigatório.";
            return RedirectToAction(nameof(FluxoCaixa));
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var cmd = new
        {
            Nome = nome,
            Tipo = tipo,
            Banco = banco,
            Agencia = agencia,
            Conta = conta,
            SaldoInicial = saldoInicial,
            DataSaldoInicial = dataSaldoInicial ?? DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, "api/administrativo360/caixa/contas", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Conta financeira cadastrada com sucesso." : resp.Error;
        return RedirectToAction(nameof(FluxoCaixa));
    }

    // ==========================================
    // BLOCO B - CONTAS A PAGAR & OBRIGAÇÕES
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> TitulosPagar(string? busca, string? situacao, Guid? fornecedorId, string? centroCusto, DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(busca)) query.Add($"busca={Uri.EscapeDataString(busca)}");
        if (!string.IsNullOrWhiteSpace(situacao)) query.Add($"situacao={Uri.EscapeDataString(situacao)}");
        if (fornecedorId.HasValue) query.Add($"fornecedorId={fornecedorId.Value}");
        if (!string.IsNullOrWhiteSpace(centroCusto)) query.Add($"centroCusto={Uri.EscapeDataString(centroCusto)}");
        if (inicio.HasValue) query.Add($"inicio={inicio.Value:yyyy-MM-dd}");
        if (fim.HasValue) query.Add($"fim={fim.Value:yyyy-MM-dd}");

        var path = "api/administrativo360/titulos-pagar" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var resp = await ReadApiResponse<List<TituloPagarResumoViewModel>>(client, path);

        return View(new TitulosPagarIndexViewModel
        {
            Titulos = (IReadOnlyList<TituloPagarResumoViewModel>?)resp.Data ?? Array.Empty<TituloPagarResumoViewModel>(),
            Busca = busca,
            Situacao = situacao,
            FornecedorId = fornecedorId,
            CentroCusto = centroCusto,
            Inicio = inicio,
            Fim = fim
        });
    }

    [HttpGet]
    public async Task<IActionResult> TituloPagarDetalhes(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<TituloPagarDetalhesViewModel>(client, $"api/administrativo360/titulos-pagar/{id}");
        if (resp.Data is null)
        {
            TempData["ErrorMessage"] = resp.Error ?? "Título a pagar não encontrado.";
            return RedirectToAction(nameof(TitulosPagar));
        }

        var contasResp = await ReadApiResponse<List<ContaFinanceiraViewModel>>(client, "api/administrativo360/caixa/contas");

        return View(new TituloPagarDetalhesPageViewModel
        {
            Titulo = resp.Data,
            Contas = (IReadOnlyList<ContaFinanceiraViewModel>?)contasResp.Data ?? Array.Empty<ContaFinanceiraViewModel>()
        });
    }

    [HttpGet]
    public IActionResult NovaDespesa()
    {
        return View(new DespesaManualFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NovaDespesa(DespesaManualFormViewModel model, string? idempotencyKey)
    {
        if (model.FornecedorId == Guid.Empty || model.ValorPrincipal <= 0)
        {
            TempData["ErrorMessage"] = "Fornecedor e valor positivo são obrigatórios.";
            return View(model);
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString("N") : idempotencyKey;

        var cmd = new
        {
            model.FornecedorId,
            Documento = model.Documento?.Trim(),
            model.Competencia,
            model.DataVencimento,
            model.ValorPrincipal,
            CentroCusto = model.CentroCusto?.Trim(),
            Observacoes = model.Observacoes?.Trim(),
            IdempotencyKey = key
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, "api/administrativo360/titulos-pagar/despesa-manual", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Despesa manual registrada com sucesso." : resp.Error;
        return RedirectToAction(nameof(TitulosPagar));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AprovarTituloPagar(Guid id, string? idempotencyKey)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString("N") : idempotencyKey;

        var cmd = new
        {
            TituloId = id,
            IdempotencyKey = key
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, $"api/administrativo360/titulos-pagar/{id}/aprovar", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Obrigação aprovada com sucesso." : resp.Error;
        return RedirectToAction(nameof(TituloPagarDetalhes), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PagarTitulo(
        Guid id,
        Guid contaFinanceiraId,
        DateOnly dataPagamento,
        decimal valorPago,
        string meioPagamento,
        string? referencia,
        string? idempotencyKey)
    {
        if (valorPago <= 0m)
        {
            TempData["ErrorMessage"] = "O valor do pagamento deve ser positivo.";
            return RedirectToAction(nameof(TituloPagarDetalhes), new { id });
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString("N") : idempotencyKey;

        var cmd = new
        {
            TituloId = id,
            ContaId = contaFinanceiraId,
            DataPagamento = dataPagamento,
            Valor = valorPago,
            MeioPagamento = meioPagamento,
            Referencia = referencia,
            IdempotencyKey = key
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, "api/administrativo360/titulos-pagar/pagar", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Pagamento registrado manualmente com sucesso." : resp.Error;
        return RedirectToAction(nameof(TituloPagarDetalhes), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EstornarPagamento(Guid tituloId, Guid pagamentoId, string motivo, string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            TempData["ErrorMessage"] = "O motivo do estorno é obrigatório.";
            return RedirectToAction(nameof(TituloPagarDetalhes), new { id = tituloId });
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString("N") : idempotencyKey;

        var cmd = new
        {
            PagamentoId = pagamentoId,
            Motivo = motivo,
            IdempotencyKey = key
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, "api/administrativo360/titulos-pagar/estornar", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Pagamento estornado com sucesso. Saldo e obrigação recompostos." : resp.Error;
        return RedirectToAction(nameof(TituloPagarDetalhes), new { id = tituloId });
    }

    [HttpGet]
    public async Task<IActionResult> ComissoesPendentes(Guid? vendedorId, DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = new List<string>();
        if (vendedorId.HasValue) query.Add($"vendedorId={vendedorId.Value}");
        if (inicio.HasValue) query.Add($"inicio={inicio.Value:yyyy-MM-dd}");
        if (fim.HasValue) query.Add($"fim={fim.Value:yyyy-MM-dd}");

        var path = "api/administrativo360/titulos-pagar/comissoes-pendentes" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var resp = await ReadApiResponse<List<ComissaoPendenteViewModel>>(client, path);

        return View(new ComissoesPendentesIndexViewModel
        {
            Comissoes = (IReadOnlyList<ComissaoPendenteViewModel>?)resp.Data ?? Array.Empty<ComissaoPendenteViewModel>(),
            VendedorId = vendedorId,
            Inicio = inicio,
            Fim = fim
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GerarTituloComissao(Guid vendedorId, DateOnly dataVencimento, string? idempotencyKey)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString("N") : idempotencyKey;

        var cmd = new
        {
            VendedorId = vendedorId,
            DataVencimento = dataVencimento,
            IdempotencyKey = key
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, "api/administrativo360/titulos-pagar/gerar-de-comissao", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Título de comissão gerado em Contas a Pagar com sucesso." : resp.Error;
        return RedirectToAction(nameof(TitulosPagar));
    }

    // ==========================================
    // CONTAS FINANCEIRAS (CADASTRO, EDIÇÃO, INATIVAÇÃO)
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> ContasFinanceiras()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<List<ContaFinanceiraViewModel>>(client, "api/administrativo360/caixa/contas");

        return View(new ContasFinanceirasIndexViewModel
        {
            Contas = (IReadOnlyList<ContaFinanceiraViewModel>?)resp.Data ?? Array.Empty<ContaFinanceiraViewModel>()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AtualizarContaFinanceira(ContaFinanceiraFormViewModel form)
    {
        if (!form.Id.HasValue || string.IsNullOrWhiteSpace(form.Nome))
        {
            TempData["ErrorMessage"] = "Conta e nome são obrigatórios.";
            return RedirectToAction(nameof(ContasFinanceiras));
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var cmd = new
        {
            ContaId = form.Id.Value,
            form.Nome,
            form.Tipo,
            form.Banco,
            form.Agencia,
            form.Conta,
            form.DataSaldoInicial,
            form.Ativo
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Put, $"api/administrativo360/caixa/contas/{form.Id.Value}", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Conta financeira atualizada com sucesso." : resp.Error;
        return RedirectToAction(nameof(ContasFinanceiras));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> InativarContaFinanceira(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Delete, $"api/administrativo360/caixa/contas/{id}", new { });

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Conta financeira inativada com sucesso." : resp.Error;
        return RedirectToAction(nameof(ContasFinanceiras));
    }

    // ==========================================
    // BLOCO C - FECHAMENTO DE CAIXA
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> FechamentoCaixa(Guid? contaId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var contasResp = await ReadApiResponse<List<ContaFinanceiraViewModel>>(client, "api/administrativo360/caixa/contas");
        var query = contaId.HasValue ? $"?contaId={contaId.Value}" : "";
        var fechamentosResp = await ReadApiResponse<List<CaixaFechamentoViewModel>>(client, $"api/administrativo360/caixa/fechamentos{query}");

        return View(new FechamentoCaixaIndexViewModel
        {
            Fechamentos = (IReadOnlyList<CaixaFechamentoViewModel>?)fechamentosResp.Data ?? Array.Empty<CaixaFechamentoViewModel>(),
            Contas = (IReadOnlyList<ContaFinanceiraViewModel>?)contasResp.Data ?? Array.Empty<ContaFinanceiraViewModel>(),
            ContaId = contaId
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> FecharCaixa(
        Guid contaFinanceiraId,
        DateOnly dataInicio,
        DateOnly dataFim,
        decimal saldoConferido,
        string? justificativa,
        string? idempotencyKey)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString("N") : idempotencyKey;

        var cmd = new
        {
            ContaId = contaFinanceiraId,
            DataInicio = dataInicio,
            DataFim = dataFim,
            SaldoConferido = saldoConferido,
            Justificativa = justificativa,
            IdempotencyKey = key
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, "api/administrativo360/caixa/fechar", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Caixa fechado com sucesso." : resp.Error;
        return RedirectToAction(nameof(FechamentoCaixa), new { contaId = contaFinanceiraId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReabrirCaixa(Guid fechamentoId, Guid? contaId, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            TempData["ErrorMessage"] = "O motivo da reabertura é obrigatório.";
            return RedirectToAction(nameof(FechamentoCaixa), new { contaId });
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var cmd = new
        {
            FechamentoId = fechamentoId,
            Motivo = motivo
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, $"api/administrativo360/caixa/fechamentos/{fechamentoId}/reabrir", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Fechamento de caixa reaberto com sucesso." : resp.Error;
        return RedirectToAction(nameof(FechamentoCaixa), new { contaId });
    }

    [HttpGet]
    public async Task<IActionResult> ExportarTitulosPagarCsv(
        string? busca, string? situacao, Guid? fornecedorId, string? centroCusto, DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(busca)) query.Add($"busca={Uri.EscapeDataString(busca)}");
        if (!string.IsNullOrWhiteSpace(situacao)) query.Add($"situacao={Uri.EscapeDataString(situacao)}");
        if (fornecedorId.HasValue) query.Add($"fornecedorId={fornecedorId.Value}");
        if (!string.IsNullOrWhiteSpace(centroCusto)) query.Add($"centroCusto={Uri.EscapeDataString(centroCusto)}");
        if (inicio.HasValue) query.Add($"inicio={inicio.Value:yyyy-MM-dd}");
        if (fim.HasValue) query.Add($"fim={fim.Value:yyyy-MM-dd}");

        var path = "api/administrativo360/titulos-pagar" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var resp = await ReadApiResponse<List<TituloPagarResumoViewModel>>(client, path);
        var itens = resp.Data ?? new();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Numero;Fornecedor;Origem;Documento;Competencia;Emissao;Vencimento;Parcela;ValorPrincipal;ValorPago;SaldoAberto;Situacao;CentroCusto");

        foreach (var it in itens)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.Numero),
                SanitizarCsv(it.Fornecedor),
                SanitizarCsv(it.OrigemTipo),
                SanitizarCsv(it.Documento ?? ""),
                it.Competencia.ToString("yyyy-MM-dd"),
                it.DataEmissao.ToString("yyyy-MM-dd"),
                it.DataVencimento.ToString("yyyy-MM-dd"),
                $"{it.Parcela}/{it.TotalParcelas}",
                it.ValorPrincipal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.ValorPago.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.SaldoAberto.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                SanitizarCsv(it.Situacao),
                SanitizarCsv(it.CentroCusto ?? "")));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"titulos_pagar_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarTitulosReceberCsv(
        string? busca, string? situacao, Guid? pagadorId, DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(busca)) query.Add($"busca={Uri.EscapeDataString(busca)}");
        if (!string.IsNullOrWhiteSpace(situacao)) query.Add($"situacao={Uri.EscapeDataString(situacao)}");
        if (pagadorId.HasValue) query.Add($"pagadorId={pagadorId.Value}");
        if (inicio.HasValue) query.Add($"inicio={inicio.Value:yyyy-MM-dd}");
        if (fim.HasValue) query.Add($"fim={fim.Value:yyyy-MM-dd}");

        var path = "api/administrativo360/titulos" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var resp = await ReadApiResponse<List<TituloReceberResumoViewModel>>(client, path);
        var itens = resp.Data ?? new();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Numero;Venda;Pagador;Parcela;Emissao;Vencimento;ValorPrincipal;ValorRecebido;SaldoAberto;Situacao");

        foreach (var it in itens)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.Numero),
                SanitizarCsv(it.VendaNumero),
                SanitizarCsv(it.Pagador),
                $"{it.Parcela}/{it.TotalParcelas}",
                it.DataEmissao.ToString("yyyy-MM-dd"),
                it.DataVencimento.ToString("yyyy-MM-dd"),
                it.ValorPrincipal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.ValorRecebido.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.SaldoAberto.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                SanitizarCsv(it.Situacao)));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"titulos_receber_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    // ==========================================
    // BLOCO D/E - RELATÓRIOS FINANCEIROS & CSV
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> RelatoriosFinanceiros(string? aba, DateOnly? inicio, DateOnly? fim, Guid? vendedorId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var dtInicio = inicio ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var dtFim = fim ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var query = $"?inicio={dtInicio:yyyy-MM-dd}&fim={dtFim:yyyy-MM-dd}";
        if (vendedorId.HasValue) query += $"&vendedorId={vendedorId.Value}";

        var respVendas = await ReadApiResponse<List<RelatorioVendasItemViewModel>>(client, $"api/administrativo360/relatorios-financeiros/vendas{query}");
        var respComissoes = await ReadApiResponse<List<RelatorioComissaoItemViewModel>>(client, $"api/administrativo360/relatorios-financeiros/comissoes{query}");
        var respMargem = await ReadApiResponse<List<RelatorioMargemItemViewModel>>(client, $"api/administrativo360/relatorios-financeiros/margem{query}");

        return View(new RelatoriosFinanceirosPageViewModel
        {
            Vendas = (IReadOnlyList<RelatorioVendasItemViewModel>?)respVendas.Data ?? Array.Empty<RelatorioVendasItemViewModel>(),
            Comissoes = (IReadOnlyList<RelatorioComissaoItemViewModel>?)respComissoes.Data ?? Array.Empty<RelatorioComissaoItemViewModel>(),
            Margens = (IReadOnlyList<RelatorioMargemItemViewModel>?)respMargem.Data ?? Array.Empty<RelatorioMargemItemViewModel>(),
            AbaAtiva = string.IsNullOrWhiteSpace(aba) ? "vendas" : aba,
            Inicio = dtInicio,
            Fim = dtFim,
            VendedorId = vendedorId
        });
    }

    [HttpGet]
    public async Task<IActionResult> ExportarVendasCsv(DateOnly? inicio, DateOnly? fim, Guid? vendedorId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = $"?inicio={inicio:yyyy-MM-dd}&fim={fim:yyyy-MM-dd}";
        if (vendedorId.HasValue) query += $"&vendedorId={vendedorId.Value}";

        var resp = await ReadApiResponse<List<RelatorioVendasItemViewModel>>(client, $"api/administrativo360/relatorios-financeiros/vendas{query}");
        var itens = resp.Data ?? new();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Numero;Data;Hospital;Pagador;Vendedor;TotalBruto;Desconto;TotalLiquido;TotalCusto;ComissaoPrevista;Situacao");

        foreach (var it in itens)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.Numero),
                it.Data.ToString("yyyy-MM-dd"),
                SanitizarCsv(it.Hospital),
                SanitizarCsv(it.Pagador),
                SanitizarCsv(it.Vendedor ?? ""),
                it.TotalBruto.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.Desconto.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.TotalLiquido.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.TotalCusto.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.ComissaoPrevista.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                SanitizarCsv(it.Situacao)));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"vendas_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarComissoesCsv(DateOnly? inicio, DateOnly? fim, Guid? vendedorId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = $"?inicio={inicio:yyyy-MM-dd}&fim={fim:yyyy-MM-dd}";
        if (vendedorId.HasValue) query += $"&vendedorId={vendedorId.Value}";

        var resp = await ReadApiResponse<List<RelatorioComissaoItemViewModel>>(client, $"api/administrativo360/relatorios-financeiros/comissoes{query}");
        var itens = resp.Data ?? new();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Vendedor;VendaNumero;DataBaixa;BaseCalculo;Percentual;ComissaoApropriada;Situacao");

        foreach (var it in itens)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.Vendedor),
                SanitizarCsv(it.VendaNumero),
                it.DataBaixa.ToString("yyyy-MM-dd"),
                it.BaseCalculo.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.Percentual.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.ComissaoApropriada.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                SanitizarCsv(it.Situacao)));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"comissoes_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarMargemCsv(DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = $"?inicio={inicio:yyyy-MM-dd}&fim={fim:yyyy-MM-dd}";
        var resp = await ReadApiResponse<List<RelatorioMargemItemViewModel>>(client, $"api/administrativo360/relatorios-financeiros/margem{query}");
        var itens = resp.Data ?? new();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("VendaNumero;ValeNumero;Hospital;ReceitaLiquida;CustoConsumido;ComissaoPrevista;ComissaoApropriada;MargemContribuicao;MargemPercentual");

        foreach (var it in itens)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.VendaNumero),
                SanitizarCsv(it.ValeNumero),
                SanitizarCsv(it.Hospital),
                it.ReceitaLiquida.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.CustoConsumido.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.ComissaoPrevista.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.ComissaoApropriada.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.MargemContribuicao.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.MargemPercentual.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"margem_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    private static string SanitizarCsv(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return string.Empty;
        var limpo = valor.Replace("\"", "\"\"");
        // Prevenção contra Formula Injection no Excel (=, +, -, @)
        if (limpo.StartsWith('=') || limpo.StartsWith('+') || limpo.StartsWith('-') || limpo.StartsWith('@'))
            limpo = "'" + limpo;
        return $"\"{limpo}\"";
    }

    private async Task<IActionResult> Send<T>(string endpoint, T payload, string success)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var response = await SendApiAsync<T, System.Text.Json.JsonElement>(client, HttpMethod.Post, endpoint, payload);
        var ok = response.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? success : response.Error;
        return RedirectToAction(nameof(Index));
    }
}


