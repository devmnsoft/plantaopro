using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Services.Security;

namespace PlantaoPro.Web.Controllers;

/// <summary>
/// Fiscal — pré-emissão (MVP A29, d1 §9). BFF FINO (R5-A2): nenhuma tela toca mais
/// no banco fiscal; todas as operações passam pela API
/// (api/administrativo360/fiscal) via Bearer da sessão. As ações mantêm nomes,
/// rotas e roles para que o guard de rota resolva ADM360 por ação.
/// Emissão autorizada real é P1: sem conector registrado no ambiente, o POST
/// "Emitir" devolve 400 honesto da API ("conector não integrado") — L33/H19:
/// nenhum botão que sempre retorna sucesso.
/// </summary>
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE,ADMIN_CLIENTE,GESTOR_OPERACIONAL,DIRETOR,COORDENACAO,COORDENADOR,CONSULTA_CLIENTE,AUDITOR")]
[Route("Administrativo360/Fiscal")]
public sealed class Adm360FiscalWebController : BaseWebController
{
    private const string ErroSemSessao = "Sessão sem cliente (tenant) ou usuário identificado; entre com uma conta do cliente.";
    private const string RotaApi = "api/administrativo360/fiscal";

    private static readonly HashSet<string> SituacoesConhecidas = new(StringComparer.OrdinalIgnoreCase)
    {
        NotaPreEmitidaSituacoes.Rascunho,
        NotaPreEmitidaSituacoes.ProntaParaEmissao,
        NotaPreEmitidaSituacoes.Enviando,
        NotaPreEmitidaSituacoes.Autorizada,
        NotaPreEmitidaSituacoes.Rejeitada,
        NotaPreEmitidaSituacoes.PendenteConfirmacao,
        NotaPreEmitidaSituacoes.Cancelada
    };

    private readonly ICurrentUserService _currentUser;

    public Adm360FiscalWebController(
        IHttpClientFactory factory,
        ILogger<Adm360FiscalWebController> logger,
        ICurrentUserService currentUser)
        : base(factory, logger)
    {
        _currentUser = currentUser;
    }

    // ------------------------------------------------------------------
    // 1. Configuração fiscal (parâmetros de emissão — pendência P2 visível)
    // ------------------------------------------------------------------

    [HttpGet("Configurar")]
    public async Task<IActionResult> Configurar()
    {
        if (_currentUser.TenantId is not Guid tenant)
            return View("~/Views/Administrativo360/Fiscal/Configurar.cshtml", new ParametrosFiscaisConfiguracaoViewModel { Erro = ErroSemSessao });

        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var (ambiente, erro, status) = await ReadApiResponseAsync<ParametrosFiscaisAmbienteDto>(client, RotaApi + "/parametros");
        if (status == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (ambiente is null)
        {
            Logger.LogWarning("Falha ao carregar parâmetros fiscais via API. Tenant:{TenantId} Status:{Status}", tenant, (int)status);
            return View("~/Views/Administrativo360/Fiscal/Configurar.cshtml", new ParametrosFiscaisConfiguracaoViewModel { Erro = erro ?? "Falha ao carregar os parâmetros fiscais deste cliente." });
        }

        var dto = ambiente.Parametros;
        if (dto is null)
            return View("~/Views/Administrativo360/Fiscal/Configurar.cshtml", new ParametrosFiscaisConfiguracaoViewModel
            {
                JaCadastrado = false,
                PendenciasP2 = TodasAsPendencias(),
                CredencialDisponivelNoAmbiente = false,
                TransmissorRegistradoNoAmbiente = false
            });

        return View("~/Views/Administrativo360/Fiscal/Configurar.cshtml", new ParametrosFiscaisConfiguracaoViewModel
        {
            JaCadastrado = true,
            Uf = dto.Uf,
            Municipio = dto.Municipio,
            RegimeFiscal = dto.RegimeFiscal,
            OperacaoFiscal = dto.OperacaoFiscal,
            CfopVenda = dto.Cfops.GetValueOrDefault("VENDA"),
            CfopRemessa = dto.Cfops.GetValueOrDefault("REMESSA"),
            CfopRetorno = dto.Cfops.GetValueOrDefault("RETORNO"),
            ResponsavelNome = dto.ResponsavelNome,
            Ambiente = dto.Ambiente,
            Provedor = dto.Provedor,
            CertificadoReferencia = dto.CertificadoReferencia,
            Observacao = dto.Observacao,
            BloqueioExterno = dto.Status.Equals(ParametrosFiscaisRegras.Bloqueado, StringComparison.OrdinalIgnoreCase),
            StatusAtual = dto.Status,
            PendenciasP2 = PendenciasDoDto(dto),
            CredencialDisponivelNoAmbiente = ambiente.CredencialDisponivelNoAmbiente,
            TransmissorRegistradoNoAmbiente = ambiente.TransmissorRegistradoNoAmbiente
        });
    }

    [HttpPost("SalvarConfiguracao")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarConfiguracao(
        [FromForm] string? uf,
        [FromForm] string? municipio,
        [FromForm] string? regimeFiscal,
        [FromForm] string? operacaoFiscal,
        [FromForm] string? cfopVenda,
        [FromForm] string? cfopRemessa,
        [FromForm] string? cfopRetorno,
        [FromForm] string? responsavelNome,
        [FromForm] string ambiente,
        [FromForm] string? provedor,
        [FromForm] string? certificadoReferencia,
        [FromForm] string? observacao,
        [FromForm] bool bloqueioExterno)
    {
        if (_currentUser.TenantId is not Guid tenant || _currentUser.UserId is not Guid usuario)
        {
            TempData["Error"] = ErroSemSessao;
            return RedirectToAction(nameof(Configurar));
        }

        var cfops = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(cfopVenda)) cfops["VENDA"] = cfopVenda.Trim();
        if (!string.IsNullOrWhiteSpace(cfopRemessa)) cfops["REMESSA"] = cfopRemessa.Trim();
        if (!string.IsNullOrWhiteSpace(cfopRetorno)) cfops["RETORNO"] = cfopRetorno.Trim();

        var comando = new SalvarParametrosFiscaisCommand(
            NormalizarUf(uf), municipio?.Trim(), NormalizarCadeia(regimeFiscal), NormalizarCadeia(operacaoFiscal),
            cfops.Count > 0 ? cfops : null,
            null, // responsável fica como nome livre no MVP (sem vínculo com cadastro de pessoas)
            responsavelNome?.Trim(),
            NormalizarCadeia(ambiente) ?? "PENDENTE",
            NormalizarCadeia(provedor),
            certificadoReferencia?.Trim(),
            observacao?.Trim(),
            bloqueioExterno);

        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var (salvo, _, status) = await SendApiAsync<SalvarParametrosFiscaisCommand, ParametrosFiscaisAmbienteDto>(
            client, HttpMethod.Post, RotaApi + "/parametros", comando);
        if (status == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (salvo?.Parametros is not null)
            TempData["Success"] = $"Parâmetros fiscais salvos. Status atual: {salvo.Parametros.Status.ToUpperInvariant()}.";
        // Falha HTTP: o helper já gravou TempData["Error"] com a mensagem real da API.

        return RedirectToAction(nameof(Configurar));
    }

    // ------------------------------------------------------------------
    // 2. Pré-notas (lista / criação / detalhes / transições internas)
    // ------------------------------------------------------------------

    [HttpGet("Notas")]
    public async Task<IActionResult> Notas([FromQuery] string? situacao)
    {
        if (_currentUser.TenantId is not Guid tenant)
            return View("~/Views/Administrativo360/Fiscal/Notas.cshtml", new NotasPreEmissoesIndexViewModel { Erro = ErroSemSessao });

        var filtro = SituacoesConhecidas.Contains(situacao ?? "") ? situacao : null;

        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var endpoint = RotaApi + "/notas" + (filtro is null ? "" : "?situacao=" + Uri.EscapeDataString(filtro));
        var (notas, erro, status) = await ReadApiListResponseAsync<NotaPreEmitidaResumoDto>(client, endpoint);
        if (status == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (erro is not null)
        {
            Logger.LogWarning("Falha ao listar pré-notas via API. Tenant:{TenantId} Status:{Status}", tenant, (int)status);
            return View("~/Views/Administrativo360/Fiscal/Notas.cshtml", new NotasPreEmissoesIndexViewModel { Erro = erro });
        }

        return View("~/Views/Administrativo360/Fiscal/Notas.cshtml", new NotasPreEmissoesIndexViewModel
        {
            Notas = notas.ToList(),
            SituacaoFiltro = filtro
        });
    }

    [HttpGet("Nova")]
    public IActionResult Nova([FromQuery] string? origemTipo, [FromQuery] Guid? origemId)
    {
        var vm = new NovaNotaPreEmissoesViewModel
        {
            OrigemTipo = OrigemConhecida(origemTipo),
            OrigemId = origemId,
            Itens = Enumerable.Repeat(new NovaNotaItemLinha(), 8).ToList()
        };

        if (_currentUser.TenantId is null || _currentUser.UserId is null) vm.Erro = ErroSemSessao;

        return View("~/Views/Administrativo360/Fiscal/Nova.cshtml", vm);
    }

    [HttpPost("SalvarNota")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarNota([FromForm] NovaNotaPreEmissoesViewModel form)
    {
        if (_currentUser.TenantId is not Guid tenant || _currentUser.UserId is not Guid usuario)
        {
            TempData["Error"] = ErroSemSessao;
            return RedirectToAction(nameof(Nova));
        }

        var origemTipo = OrigemConhecida(form.OrigemTipo);
        var itens = form.Itens
            .Where(i => !string.IsNullOrWhiteSpace(i.Descricao))
            .Select(i => new NotaPreEmitidaItemCommand(i.Descricao!.Trim(), i.Quantidade, i.PrecoUnitario))
            .ToList();

        var comando = new CriarNotaPreEmitidaCommand(
            origemTipo,
            origemTipo.Equals("MANUAL", StringComparison.OrdinalIgnoreCase) ? null : form.OrigemId,
            form.DestinatarioNome,
            form.DestinatarioDocumento,
            itens);

        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var (resposta, _, status) = await SendApiAsync<CriarNotaPreEmitidaCommand, IdResposta>(
            client, HttpMethod.Post, RotaApi + "/notas", comando);
        if (status == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (resposta is null)
        {
            // Falha HTTP: o helper já gravou TempData["Error"] com a mensagem real da API.
            return RedirectToAction(nameof(Nova));
        }

        TempData["Success"] = "Pré-nota criada em rascunho com conferência de referências.";
        return RedirectToAction(nameof(Detalhes), new { id = resposta.Id });
    }

    [HttpGet("Detalhes/{id}")]
    public async Task<IActionResult> Detalhes(Guid id)
    {
        if (_currentUser.TenantId is not Guid tenant)
            return View("~/Views/Administrativo360/Fiscal/Detalhes.cshtml", new NotaPreEmissoesDetalhesViewModel { Erro = ErroSemSessao });

        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var (nota, erroNota, statusNota) = await ReadApiResponseAsync<NotaPreEmitidaDetalhesDto>(client, $"{RotaApi}/notas/{id}");
        if (statusNota == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (nota is null)
        {
            TempData["Error"] = erroNota ?? "Pré-nota não encontrada para este cliente.";
            return RedirectToAction(nameof(Notas));
        }

        var (ambiente, _, _) = await ReadApiResponseAsync<ParametrosFiscaisAmbienteDto>(client, RotaApi + "/parametros");
        var parametros = ambiente?.Parametros;
        var bloqueio = BloqueioDeEmissao(parametros, ambiente?.CredencialDisponivelNoAmbiente ?? false);

        return View("~/Views/Administrativo360/Fiscal/Detalhes.cshtml", new NotaPreEmissoesDetalhesViewModel
        {
            Nota = nota,
            Transicoes = TransicoesPara(nota.Resumo.Situacao),
            StatusParametros = parametros?.Status,
            BloqueioEmissao = bloqueio,
            TransmissaoIndisponivelNoAmbiente = bloqueio is null && ambiente is not null && !ambiente.TransmissorRegistradoNoAmbiente,
            ProvedorConfigurado = parametros?.Provedor
        });
    }

    /// <summary>RASCUNHO -> PRONTA_PARA_EMISSAO.</summary>
    [HttpPost("MarcarPronta")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarPronta(Guid id) =>
        await TransicionarNaApiAsync(id, "marcar-pronta", nota =>
        {
            if (!nota.Resumo.Situacao.Equals(NotaPreEmitidaSituacoes.Rascunho, StringComparison.OrdinalIgnoreCase))
                throw new Administrativo360BusinessException("Apenas uma pré-nota em rascunho pode ser marcada como pronta.");
            return "Pré-nota marcada como pronta para emissão.";
        });

    /// <summary>
    /// PRONTA -> ENVIANDO honesto: a API devolve 200 sem avancar quando ha motivo
    /// real de bloqueio, e 400 honesto ("conector não integrado", P1) quando os
    /// parametros estao ok mas nao ha transmissor no ambiente — nunca ENVIANDO falso.
    /// </summary>
    [HttpPost("Emitir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Emitir(Guid id)
    {
        if (_currentUser.TenantId is not Guid tenant || _currentUser.UserId is not Guid usuario)
        {
            TempData["Error"] = ErroSemSessao;
            return RedirectToAction(nameof(Detalhes), new { id });
        }

        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var (resultado, _, status) = await SendApiAsync<object, ResultadoEmissaoFiscal>(
            client, HttpMethod.Post, $"{RotaApi}/notas/{id}/emitir", new { });
        if (status == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (resultado is not null)
        {
            if (resultado.Avancou)
                TempData["Success"] = string.IsNullOrWhiteSpace(resultado.ChaveAcessoExterna)
                    ? "Pré-nota transmitida (ENVIANDO). A autorização/rejeição só é registrada com o retorno externo (conector = P1)."
                    : $"Pré-nota transmitida (ENVIANDO). Chave externa: {resultado.ChaveAcessoExterna}.";
            else
                TempData["Error"] = resultado.Mensagem;
        }
        // Resultado nulo = falha HTTP: o helper já gravou TempData["Error"] com a mensagem real da API.

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    /// <summary>
    /// Volta para um estado menos avançado (matriz do Domain, alvo server-side):
    /// PRONTA -> RASCUNHO (editar itens); ENVIANDO -> PRONTA (falha interna de transmissão);
    /// REJEITADA -> PRONTA (correção + reenvio).
    /// </summary>
    [HttpPost("Reabrir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reabrir(Guid id) =>
        await TransicionarNaApiAsync(id, "reabrir", nota =>
        {
            var atual = nota.Resumo.Situacao.ToUpperInvariant();
            return atual switch
            {
                NotaPreEmitidaSituacoes.ProntaParaEmissao => "Pré-nota reaberta em rascunho (itens voltaram a ser editáveis).",
                NotaPreEmitidaSituacoes.Enviando => "Transmissão retornada: a pré-nota voltou a PRONTA_PARA_EMISSAO.",
                NotaPreEmitidaSituacoes.Rejeitada => "Rejeitada corrigida: a pré-nota está pronta para novo envio.",
                _ => throw new Administrativo360BusinessException($"A situação {atual} não pode ser reaberta agora.")
            };
        });

    /// <summary>Cancelamento interno (motivo obrigatório). Cancelamento OFICIAL de autorizada = P1 externo.</summary>
    [HttpPost("CancelarNota")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarNota(Guid id, [FromForm] string? motivoCancelamento)
    {
        if (_currentUser.TenantId is not Guid tenant || _currentUser.UserId is not Guid usuario)
        {
            TempData["Error"] = ErroSemSessao;
            return RedirectToAction(nameof(Detalhes), new { id });
        }

        if (string.IsNullOrWhiteSpace(motivoCancelamento))
        {
            TempData["Error"] = "Informe o motivo do cancelamento (obrigatório).";
            return RedirectToAction(nameof(Detalhes), new { id });
        }

        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var (cancelada, _, status) = await SendApiAsync<CancelarNotaPreEmitidaCommand, NotaPreEmitidaDetalhesDto>(
            client, HttpMethod.Post, $"{RotaApi}/notas/{id}/cancelar",
            new CancelarNotaPreEmitidaCommand(motivoCancelamento.Trim()));
        if (status == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (cancelada is not null)
            TempData["Success"] = "Pré-nota cancelada.";
        // Falha HTTP: o helper já gravou TempData["Error"] com a mensagem real da API.

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    // ------------------------------------------------------------------
    // Infraestrutura da ação (BFF: sessão + API; matriz p/ view)
    // ------------------------------------------------------------------

    /// <summary>
    /// Lê a nota na API (mensagem local + redirect coerente), valida a intenção e
    /// executa o segmento (marcar-pronta/reabrir). A validacao definitiva e
    /// server-side; a API responde 400 honesto quando a transicao nao cabe.
    /// </summary>
    private async Task<IActionResult> TransicionarNaApiAsync(
        Guid id,
        string segmento,
        Func<NotaPreEmitidaDetalhesDto, string> mensagemSucesso)
    {
        if (_currentUser.TenantId is not Guid tenant || _currentUser.UserId is not Guid usuario)
        {
            TempData["Error"] = ErroSemSessao;
            return RedirectToAction(nameof(Detalhes), new { id });
        }

        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        string mensagem;
        try
        {
            var (nota, erroNota, statusNota) = await ReadApiResponseAsync<NotaPreEmitidaDetalhesDto>(client, $"{RotaApi}/notas/{id}");
            if (statusNota == HttpStatusCode.Unauthorized) return HandleUnauthorized();
            if (nota is null)
            {
                TempData["Error"] = erroNota ?? "Pré-nota não encontrada para este cliente.";
                return RedirectToAction(nameof(Notas));
            }

            mensagem = mensagemSucesso(nota);
        }
        catch (Administrativo360BusinessException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Detalhes), new { id });
        }

        var (_, _, status) = await SendApiAsync<object, NotaPreEmitidaDetalhesDto>(
            client, HttpMethod.Post, $"{RotaApi}/notas/{id}/{segmento}", new { });
        if (status == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (status is >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous)
            TempData["Success"] = mensagem;
        // Falha HTTP: o helper já gravou TempData["Error"] com a mensagem real da API.

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    /// <summary>Motivo real (ou null) impedindo a emissão externa — aceita parâmetros inexistentes.</summary>
    private static string? BloqueioDeEmissao(ParametrosFiscaisDto? parametros, bool credencialDisponivel)
    {
        var snapshot = parametros is null
            ? null
            : new ParametrosFiscaisSnapshot(parametros.Status, parametros.CertificadoReferencia,
                credencialDisponivel, parametros.Observacao);

        // A regra trata null como "ainda não cadastrados"; a anotação só não cobre esse caso.
        return NotaPreEmitidaRegras.MotivoBloqueioEmissao(snapshot!);
    }

    private static readonly string[] OrigensConhecidas = { "MANUAL", "VENDA", "ORCAMENTO", "COTACAO" };

    private static string OrigemConhecida(string? origem) =>
        OrigensConhecidas.Contains((origem ?? "").Trim().ToUpperInvariant())
            ? origem!.Trim().ToUpperInvariant()
            : "MANUAL";

    private static string? NormalizarUf(string? uf)
    {
        uf = uf?.Trim();
        return string.IsNullOrEmpty(uf) ? null : uf.ToUpperInvariant();
    }

    private static string? NormalizarCadeia(string? valor)
    {
        valor = valor?.Trim();
        return string.IsNullOrEmpty(valor) ? null : valor.ToUpperInvariant();
    }

    private static List<string> TodasAsPendencias() => new()
    {
        "UF de emissão", "Município", "Regime fiscal", "Operação fiscal",
        "Provedor de emissão", "Ambiente (homologação/produção)",
        "CFOP por operação", "Credencial de emissão (nome do segredo)"
    };

    private static List<string> PendenciasDoDto(ParametrosFiscaisDto dto)
    {
        var pendencias = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.Uf)) pendencias.Add("UF de emissão");
        if (string.IsNullOrWhiteSpace(dto.Municipio)) pendencias.Add("Município");
        if (string.IsNullOrWhiteSpace(dto.RegimeFiscal)) pendencias.Add("Regime fiscal");
        if (string.IsNullOrWhiteSpace(dto.OperacaoFiscal)) pendencias.Add("Operação fiscal");
        if (string.IsNullOrWhiteSpace(dto.Provedor)) pendencias.Add("Provedor de emissão");
        if (!dto.Ambiente.Equals("HOMOLOGACAO", StringComparison.OrdinalIgnoreCase) &&
            !dto.Ambiente.Equals("PRODUCAO", StringComparison.OrdinalIgnoreCase))
            pendencias.Add("Ambiente (homologação/produção)");
        if (dto.Cfops.Count == 0) pendencias.Add("CFOP por operação");
        if (string.IsNullOrWhiteSpace(dto.CertificadoReferencia)) pendencias.Add("Credencial de emissão (nome do segredo)");
        return pendencias;
    }

    private static TransicoesPossiveis TransicoesPara(string situacao) => situacao.ToUpperInvariant() switch
    {
        NotaPreEmitidaSituacoes.Rascunho => new(true, false, false, true),
        NotaPreEmitidaSituacoes.ProntaParaEmissao => new(false, true, true, true),
        NotaPreEmitidaSituacoes.Enviando => new(false, false, true, true),
        NotaPreEmitidaSituacoes.PendenteConfirmacao => new(false, false, false, true),
        NotaPreEmitidaSituacoes.Rejeitada => new(false, false, true, true),
        _ => new(false, false, false, false) // AUTORIZADA e CANCELADA são terminais no MVP
    };

    private sealed record IdResposta(Guid Id);
}
