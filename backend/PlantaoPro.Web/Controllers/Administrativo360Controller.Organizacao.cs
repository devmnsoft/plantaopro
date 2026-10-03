using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

// ============================================================
// Gestão Organizacional — uma tela por área: visão geral (Index),
// departamentos, cargos, colaboradores e contratos de trabalho.
// Se a API rejeita a operação (regra de negócio/validação), a MESMA
// tela é reaberta com os valores digitados e a mensagem junto ao
// formulário — o usuário nunca perde o que preencheu nem vê erro
// técnico de materialização/banco.
// ============================================================
public partial class Administrativo360Controller
{
    private const int OrgPageSize = 50;
    private const int OrgSelectorSize = 100;

    // ==================== DEPARTAMENTOS ====================
    public async Task<IActionResult> Departamentos(string? busca, int page = 1)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        return View(await CarregarDepartamentosAsync(client, busca, page));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Departamento(string? busca, string codigo, string nome)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var codigoLimpo = (codigo ?? string.Empty).Trim();
        var nomeLimpo = (nome ?? string.Empty).Trim();
        if (codigoLimpo.Length == 0 || nomeLimpo.Length == 0)
            return ViewDepartamentoComErro(await CarregarDepartamentosAsync(client, busca, 1), codigoLimpo, nomeLimpo, "Informe o código e o nome do departamento.");
        var (ok, erro) = await EnviarAsync(client, HttpMethod.Post, "api/administrativo360/departamentos", new { codigo = codigoLimpo, nome = nomeLimpo });
        if (ok)
        {
            TempData["Success"] = "Departamento cadastrado.";
            return RedirectToAction(nameof(Departamentos), new { busca });
        }
        return ViewDepartamentoComErro(await CarregarDepartamentosAsync(client, busca, 1), codigoLimpo, nomeLimpo, erro);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AtualizarDepartamento(Guid id, string? busca, int page, string codigo, string nome)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var codigoLimpo = (codigo ?? string.Empty).Trim();
        var nomeLimpo = (nome ?? string.Empty).Trim();
        if (codigoLimpo.Length == 0 || nomeLimpo.Length == 0)
            return ViewDepartamentoComErro(await CarregarDepartamentosAsync(client, busca, page), codigoLimpo, nomeLimpo, "Informe o código e o nome do departamento.", id, new DepartamentoEdicaoModel(id, codigoLimpo, nomeLimpo));
        var (ok, erro) = await EnviarAsync(client, HttpMethod.Put, $"api/administrativo360/departamentos/{id}", new { codigo = codigoLimpo, nome = nomeLimpo });
        if (ok)
        {
            TempData["Success"] = "Departamento atualizado.";
            return RedirectToAction(nameof(Departamentos), new { busca, page });
        }
        return ViewDepartamentoComErro(await CarregarDepartamentosAsync(client, busca, page), codigoLimpo, nomeLimpo, erro, id, new DepartamentoEdicaoModel(id, codigoLimpo, nomeLimpo));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> InativarDepartamento(Guid id, string? busca, int page) =>
        await TransicaoAsync(HttpMethod.Post, $"api/administrativo360/departamentos/{id}/inativar", new { },
            "Departamento inativado. Cargos e colaboradores vinculados preservam o histórico.",
            () => RedirectToAction(nameof(Departamentos), new { busca, page }));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AtivarDepartamento(Guid id, string? busca, int page) =>
        await TransicaoAsync(HttpMethod.Post, $"api/administrativo360/departamentos/{id}/ativar", new { },
            "Departamento reativado.",
            () => RedirectToAction(nameof(Departamentos), new { busca, page }));

    // ==================== CARGOS ====================
    public async Task<IActionResult> Cargos(string? busca, int page = 1)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        return View(await CarregarCargosAsync(client, busca, page));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cargo(string? busca, string codigo, string nome, Guid? departamentoId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var codigoLimpo = (codigo ?? string.Empty).Trim();
        var nomeLimpo = (nome ?? string.Empty).Trim();
        if (codigoLimpo.Length == 0 || nomeLimpo.Length == 0)
            return ViewCargoComErro(await CarregarCargosAsync(client, busca, 1), codigoLimpo, nomeLimpo, departamentoId, "Informe o código e o nome do cargo.");
        var (ok, erro) = await EnviarAsync(client, HttpMethod.Post, "api/administrativo360/cargos", new { codigo = codigoLimpo, nome = nomeLimpo, departamentoId });
        if (ok)
        {
            TempData["Success"] = "Cargo cadastrado.";
            return RedirectToAction(nameof(Cargos), new { busca });
        }
        return ViewCargoComErro(await CarregarCargosAsync(client, busca, 1), codigoLimpo, nomeLimpo, departamentoId, erro);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AtualizarCargo(Guid id, string? busca, int page, string codigo, string nome, Guid? departamentoId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var codigoLimpo = (codigo ?? string.Empty).Trim();
        var nomeLimpo = (nome ?? string.Empty).Trim();
        var edicao = new CargoEdicaoModel(id, codigoLimpo, nomeLimpo, departamentoId);
        if (codigoLimpo.Length == 0 || nomeLimpo.Length == 0)
            return ViewCargoComErro(await CarregarCargosAsync(client, busca, page), codigoLimpo, nomeLimpo, departamentoId, "Informe o código e o nome do cargo.", id, edicao);
        var (ok, erro) = await EnviarAsync(client, HttpMethod.Put, $"api/administrativo360/cargos/{id}", new { codigo = codigoLimpo, nome = nomeLimpo, departamentoId });
        if (ok)
        {
            TempData["Success"] = "Cargo atualizado.";
            return RedirectToAction(nameof(Cargos), new { busca, page });
        }
        return ViewCargoComErro(await CarregarCargosAsync(client, busca, page), codigoLimpo, nomeLimpo, departamentoId, erro, id, edicao);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> InativarCargo(Guid id, string? busca, int page) =>
        await TransicaoAsync(HttpMethod.Post, $"api/administrativo360/cargos/{id}/inativar", new { },
            "Cargo inativado. Colaboradores vinculados preservam o histórico.",
            () => RedirectToAction(nameof(Cargos), new { busca, page }));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AtivarCargo(Guid id, string? busca, int page) =>
        await TransicaoAsync(HttpMethod.Post, $"api/administrativo360/cargos/{id}/ativar", new { },
            "Cargo reativado.",
            () => RedirectToAction(nameof(Cargos), new { busca, page }));

    // ==================== COLABORADORES ====================
    public async Task<IActionResult> Colaboradores(string? busca, string? status, int page = 1)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        return View(await CarregarColaboradoresAsync(client, busca, status, page));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Colaborador(string? busca, string? status, string matricula, string nome, string cpf, string email, Guid cargoId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var modelo = await CarregarColaboradoresAsync(client, busca, status, 1);
        var matriculaLimpa = (matricula ?? string.Empty).Trim();
        var nomeLimpo = (nome ?? string.Empty).Trim();
        var cpfDigitos = (cpf ?? string.Empty).Replace(".", "").Replace("-", "");
        var emailLimpo = (email ?? string.Empty).Trim();
        if (matriculaLimpa.Length == 0 || nomeLimpo.Length == 0)
            return ViewColaboradorComErro(modelo, matriculaLimpa, nomeLimpo, cpf, emailLimpo, cargoId, "Informe a matrícula e o nome do colaborador.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(cpfDigitos, @"^\d{11}$"))
            return ViewColaboradorComErro(modelo, matriculaLimpa, nomeLimpo, cpf, emailLimpo, cargoId, "Informe um CPF válido com 11 dígitos.");
        if (emailLimpo.Length == 0 || !emailLimpo.Contains('@'))
            return ViewColaboradorComErro(modelo, matriculaLimpa, nomeLimpo, cpf, emailLimpo, cargoId, "Informe um e-mail válido.");
        if (cargoId == Guid.Empty)
            return ViewColaboradorComErro(modelo, matriculaLimpa, nomeLimpo, cpf, emailLimpo, cargoId, "Selecione o cargo do colaborador.");
        var (ok, erro) = await EnviarAsync(client, HttpMethod.Post, "api/administrativo360/colaboradores",
            new { matricula = matriculaLimpa, nome = nomeLimpo, cpf = cpfDigitos, email = emailLimpo, cargoId });
        if (ok)
        {
            TempData["Success"] = "Colaborador cadastrado.";
            return RedirectToAction(nameof(Colaboradores), new { busca, status });
        }
        return ViewColaboradorComErro(await CarregarColaboradoresAsync(client, busca, status, 1), matriculaLimpa, nomeLimpo, cpf, emailLimpo, cargoId, erro);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AtualizarColaborador(Guid id, string? busca, string? status, int page, string nome, string email)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var nomeLimpo = (nome ?? string.Empty).Trim();
        var emailLimpo = (email ?? string.Empty).Trim();
        if (nomeLimpo.Length == 0 || emailLimpo.Length == 0 || !emailLimpo.Contains('@'))
            return ViewColaboradorComErro(await CarregarColaboradoresAsync(client, busca, status, page), string.Empty, nomeLimpo, string.Empty, emailLimpo, Guid.Empty,
                "Informe nome e e-mail válidos.", id, new ColaboradorEdicaoModel(id, nomeLimpo, emailLimpo));
        var (ok, erro) = await EnviarAsync(client, HttpMethod.Put, $"api/administrativo360/colaboradores/{id}", new { nome = nomeLimpo, email = emailLimpo });
        if (ok)
        {
            TempData["Success"] = "Dados do colaborador atualizados.";
            return RedirectToAction(nameof(Colaboradores), new { busca, status, page });
        }
        return ViewColaboradorComErro(await CarregarColaboradoresAsync(client, busca, status, page), string.Empty, nomeLimpo, string.Empty, emailLimpo, Guid.Empty,
            erro, id, new ColaboradorEdicaoModel(id, nomeLimpo, emailLimpo));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> StatusColaborador(Guid id, string? busca, string? status, int page, string novoStatus)
    {
        var alvo = (novoStatus ?? string.Empty).Trim().ToUpperInvariant();
        var mensagem = alvo switch
        {
            "ATIVO" => "Colaborador reativado.",
            "AFASTADO" => "Colaborador marcado como afastado.",
            "DESLIGADO" => "Colaborador desligado. Encerre os contratos vigentes quando houver.",
            _ => null
        };
        if (mensagem is null)
        {
            TempData["Error"] = "Status inválido para o colaborador.";
            return RedirectToAction(nameof(Colaboradores), new { busca, status, page });
        }
        return await TransicaoAsync(HttpMethod.Post, $"api/administrativo360/colaboradores/{id}/status", new { status = alvo }, mensagem,
            () => RedirectToAction(nameof(Colaboradores), new { busca, status, page }));
    }

    // ==================== CONTRATOS DE TRABALHO ====================
    public async Task<IActionResult> Contratos(string? busca, string? status, int page = 1)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        return View(await CarregarContratosAsync(client, busca, status, page));
    }

    public async Task<IActionResult> ContratoDetalhes(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        return View(await CarregarContratoDetalhesAsync(client, id));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Contrato(string? busca, string? status, Guid colaboradorId, string tipo, DateOnly? inicio, DateOnly? fim, decimal salario, int cargaHorariaSemanal)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var modelo = await CarregarContratosAsync(client, busca, status, 1);
        modelo.ColaboradorId = colaboradorId;
        modelo.Tipo = tipo;
        modelo.Inicio = inicio;
        modelo.Fim = fim;
        modelo.Salario = salario;
        modelo.CargaHorariaSemanal = cargaHorariaSemanal;
        if (colaboradorId == Guid.Empty) return View("Contratos", ComFormError(modelo, "Selecione o colaborador a contratar."));
        if (inicio is null) return View("Contratos", ComFormError(modelo, "Informe a data de início do contrato."));
        if (fim is not null && fim < inicio) return View("Contratos", ComFormError(modelo, "A data final não pode ser anterior à data de início."));
        if (salario <= 0) return View("Contratos", ComFormError(modelo, "Informe um salário maior que zero."));
        if (cargaHorariaSemanal is < 1 or > 60) return View("Contratos", ComFormError(modelo, "A carga horária semanal deve estar entre 1 e 60 horas."));
        var (ok, erro) = await EnviarAsync(client, HttpMethod.Post, "api/administrativo360/contratos",
            new { colaboradorId, tipo, inicio = inicio.Value.ToString("yyyy-MM-dd"), fim = fim?.ToString("yyyy-MM-dd"), salario, cargaHorariaSemanal });
        if (ok)
        {
            TempData["Success"] = "Contratação registrada.";
            return RedirectToAction(nameof(Contratos), new { busca, status });
        }
        var modeloRejeitado = await CarregarContratosAsync(client, busca, status, 1);
        modeloRejeitado.ColaboradorId = colaboradorId;
        modeloRejeitado.Tipo = tipo;
        modeloRejeitado.Inicio = inicio;
        modeloRejeitado.Fim = fim;
        modeloRejeitado.Salario = salario;
        modeloRejeitado.CargaHorariaSemanal = cargaHorariaSemanal;
        return View("Contratos", ComFormError(modeloRejeitado, erro));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EncerrarContrato(Guid id, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (ok, erro) = await EnviarAsync(client, HttpMethod.Post, $"api/administrativo360/contratos/{id}/encerrar", new { fim = fim?.ToString("yyyy-MM-dd") });
        if (ok)
        {
            TempData["Success"] = "Contrato encerrado. O histórico permanece disponível.";
            return RedirectToAction(nameof(ContratoDetalhes), new { id });
        }
        var baseDetalhe = await CarregarContratoDetalhesAsync(client, id);
        LimparAlertaGlobal();
        return View("ContratoDetalhes", new ContratoDetalhesViewModel
        {
            Contrato = baseDetalhe.Contrato,
            // Se o carregamento do detalhe falhou, a mensagem de carga tem prioridade.
            Erro = baseDetalhe.Contrato is null ? baseDetalhe.Erro : erro,
            Fim = fim
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarContrato(Guid id) =>
        await TransicaoAsync(HttpMethod.Post, $"api/administrativo360/contratos/{id}/cancelar", new { },
            "Contrato cancelado. O registro foi preservado para auditoria.",
            () => RedirectToAction(nameof(ContratoDetalhes), new { id }));

    // ==================== RE-RENDER COM VALORES PRESERVADOS ====================
    private IActionResult ViewDepartamentoComErro(DepartamentosIndexViewModel baseModelo, string codigo, string nome, string? erro, Guid? id = null, DepartamentoEdicaoModel? editar = null)
    {
        baseModelo.Codigo = codigo;
        baseModelo.Nome = nome;
        baseModelo.FormError = erro;
        baseModelo.Editar = editar;
        LimparAlertaGlobal();
        return View("Departamentos", baseModelo);
    }

    private IActionResult ViewCargoComErro(CargosIndexViewModel baseModelo, string codigo, string nome, Guid? departamentoId, string? erro, Guid? id = null, CargoEdicaoModel? editar = null)
    {
        baseModelo.Codigo = codigo;
        baseModelo.Nome = nome;
        baseModelo.DepartamentoId = departamentoId;
        baseModelo.FormError = erro;
        baseModelo.Editar = editar;
        LimparAlertaGlobal();
        return View("Cargos", baseModelo);
    }

    private IActionResult ViewColaboradorComErro(ColaboradoresIndexViewModel baseModelo, string matricula, string nome, string cpf, string email, Guid cargoId, string? erro, Guid? id = null, ColaboradorEdicaoModel? editar = null)
    {
        baseModelo.Matricula = matricula;
        baseModelo.Nome = nome;
        baseModelo.Cpf = cpf;
        baseModelo.Email = email;
        baseModelo.CargoId = cargoId;
        baseModelo.FormError = erro;
        baseModelo.Editar = editar;
        LimparAlertaGlobal();
        return View("Colaboradores", baseModelo);
    }

    private static ContratosIndexViewModel ComFormError(ContratosIndexViewModel modelo, string? erro)
    {
        modelo.FormError = erro;
        return modelo;
    }

    // ==================== CARREGAMENTO DAS TELAS ====================
    private async Task<DepartamentosIndexViewModel> CarregarDepartamentosAsync(HttpClient client, string? busca, int page)
    {
        var (pagina, erro, _) = await ReadApiPagedResponseAsync<Departamento360ViewModel>(client, UrlLista("departamentos", page, busca), page, OrgPageSize);
        return new DepartamentosIndexViewModel { Pagina = pagina, Page = pagina.Page, Busca = busca, ErroLista = erro };
    }

    private async Task<CargosIndexViewModel> CarregarCargosAsync(HttpClient client, string? busca, int page)
    {
        var (pagina, erro, _) = await ReadApiPagedResponseAsync<Cargo360ViewModel>(client, UrlLista("cargos", page, busca), page, OrgPageSize);
        var deps = await ReadApiPagedResponseAsync<Departamento360ViewModel>(client, $"api/administrativo360/departamentos?pagina=1&tamanho={OrgSelectorSize}&somenteAtivos=true", 1, OrgSelectorSize);
        return new CargosIndexViewModel
        {
            Pagina = pagina,
            Page = pagina.Page,
            Busca = busca,
            ErroLista = erro,
            Departamentos = deps.Data.Items.ToList()
        };
    }

    private async Task<ColaboradoresIndexViewModel> CarregarColaboradoresAsync(HttpClient client, string? busca, string? status, int page)
    {
        var (pagina, erro, _) = await ReadApiPagedResponseAsync<Colaborador360ViewModel>(client, UrlLista("colaboradores", page, busca, status), page, OrgPageSize);
        var cargos = await ReadApiPagedResponseAsync<Cargo360ViewModel>(client, $"api/administrativo360/cargos?pagina=1&tamanho={OrgSelectorSize}&somenteAtivos=true", 1, OrgSelectorSize);
        return new ColaboradoresIndexViewModel
        {
            Pagina = pagina,
            Page = pagina.Page,
            Busca = busca,
            Status = status,
            ErroLista = erro,
            Cargos = cargos.Data.Items.ToList()
        };
    }

    private async Task<ContratosIndexViewModel> CarregarContratosAsync(HttpClient client, string? busca, string? status, int page)
    {
        var (pagina, erro, _) = await ReadApiPagedResponseAsync<Contrato360ViewModel>(client, UrlLista("contratos", page, busca, status), page, OrgPageSize);
        // Seletor com apenas colaboradores ativos (regra de contratação).
        var colaboradores = await ReadApiPagedResponseAsync<Colaborador360ViewModel>(client, $"api/administrativo360/colaboradores?pagina=1&tamanho={OrgSelectorSize}&status=ATIVO", 1, OrgSelectorSize);
        return new ContratosIndexViewModel
        {
            Pagina = pagina,
            Page = pagina.Page,
            Busca = busca,
            Status = status,
            ErroLista = erro,
            Colaboradores = colaboradores.Data.Items.ToList()
        };
    }

    private async Task<ContratoDetalhesViewModel> CarregarContratoDetalhesAsync(HttpClient client, Guid id)
    {
        var (data, erro, _) = await ReadApiResponse<Contrato360DetalheViewModel>(client, $"api/administrativo360/contratos/{id}");
        return new ContratoDetalhesViewModel { Contrato = data, Erro = erro };
    }

    // ==================== SUPORTE ====================
    private static string UrlLista(string recurso, int page, string? busca, string? status = null)
    {
        var url = $"api/administrativo360/{recurso}?pagina={page}&tamanho={OrgPageSize}";
        if (!string.IsNullOrWhiteSpace(busca)) url += $"&busca={Uri.EscapeDataString(busca.Trim())}";
        if (!string.IsNullOrWhiteSpace(status)) url += $"&status={Uri.EscapeDataString(status.Trim().ToUpperInvariant())}";
        return url;
    }

    private async Task<(bool Ok, string? Erro)> EnviarAsync(HttpClient client, HttpMethod metodo, string endpoint, object payload)
    {
        var (_, erro, status) = await SendApiAsync<object, JsonElement>(client, metodo, endpoint, payload);
        if (status is >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous) return (true, null);
        return (false, erro ?? "Não foi possível concluir a operação. Tente novamente.");
    }

    // Ação de impacto simples (inativar/ativar/status/encerrar/cancelar): sempre redireciona
    // de volta à tela de origem preservando filtros; o resultado vai no alerta global.
    private async Task<IActionResult> TransicaoAsync(HttpMethod metodo, string endpoint, object payload, string sucesso, Func<IActionResult> destino)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (ok, erro) = await EnviarAsync(client, metodo, endpoint, payload);
        // Chaves unificadas do toast global (bloco C): o helper da BaseWebController
        // pode ter gravado "Error" bruto na falha da API; limpa antes de gravar a
        // mensagem amigável para que o feedback apareça UMA única vez.
        LimparAlertaGlobal();
        if (ok)
        {
            TempData["Success"] = sucesso;
        }
        else
        {
            TempData["Error"] = erro;
        }
        return destino();
    }

    // Remove o efeito colateral de erros dos helpers da BaseWebController para que a
    // mensagem apareça UMA única vez, no local certo (formulário ou alerta da tela).
    private void LimparAlertaGlobal()
    {
        TempData["Error"] = null;
        TempData["Success"] = null;
    }
}
