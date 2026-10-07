using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Services.Security;

namespace PlantaoPro.Web.Controllers;

/// <summary>
/// Fiscal — pré-emissão (MVP A29, d1 §9). Páginas MVC que constroem os repositórios do F1
/// por request (string de conexão da IConfiguration + usuário da sessão); não há endpoints
/// API novos neste escopo. As ações têm nomes próprios para que o guard de rota resolva ADM360.VER.
/// Emissão autorizada real é P1: o botão "Emitir" só avança quando os parâmetros não trazem
/// motivo real de bloqueio (MotivoBloqueioEmissao) — L33/H19: nenhum botão que sempre retorna sucesso.
/// </summary>
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE,ADMIN_CLIENTE,GESTOR_OPERACIONAL,DIRETOR,COORDENACAO,COORDENADOR,CONSULTA_CLIENTE,AUDITOR")]
[Route("Administrativo360/Fiscal")]
public sealed class Adm360FiscalWebController : BaseWebController
{
    private const string PrefixoCredenciais = "Fiscal:Credenciais";
    private const string ErroSemSessao = "Sessão sem cliente (tenant) ou usuário identificado; entre com uma conta do cliente.";
    private const string ErroSemConexao = "Connection string 'Default' ausente na configuração da Web; verifique o appsettings.";

    // Mesma restrição de segredo da regra de domínio (A33): o campo guarda NOME de referência.
    private static readonly Regex ReferenciaSegura = new("^[A-Za-z0-9._\\-]{1,120}$", RegexOptions.Compiled);

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

    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUser;

    public Adm360FiscalWebController(
        IHttpClientFactory factory,
        ILogger<Adm360FiscalWebController> logger,
        IConfiguration configuration,
        ICurrentUserService currentUser)
        : base(factory, logger)
    {
        _configuration = configuration;
        _currentUser = currentUser;
    }

    // ------------------------------------------------------------------
    // 1. Configuração fiscal (parâmetros de emissão — pendência P2 visível)
    // ------------------------------------------------------------------

    [HttpGet("Configurar")]
    public async Task<IActionResult> Configurar()
    {
        if (_currentUser.TenantId is not Guid tenant || _currentUser.UserId is not Guid usuario)
            return View("~/Views/Administrativo360/Fiscal/Configurar.cshtml", new ParametrosFiscaisConfiguracaoViewModel { Erro = ErroSemSessao });
        if (string.IsNullOrWhiteSpace(_configuration.GetConnectionString("Default")))
            return View("~/Views/Administrativo360/Fiscal/Configurar.cshtml", new ParametrosFiscaisConfiguracaoViewModel { Erro = ErroSemConexao });

        try
        {
            var repo = new ParametrosFiscaisRepository(ConnectionStringObrigatoria());
            var dto = await repo.ObterAsync(tenant);

            if (dto is null)
                return View("~/Views/Administrativo360/Fiscal/Configurar.cshtml", new ParametrosFiscaisConfiguracaoViewModel
                {
                    JaCadastrado = false,
                    PendenciasP2 = TodasAsPendencias()
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
                PendenciasP2 = PendenciasDoDto(dto)
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Falha ao carregar parâmetros fiscais. Tenant:{TenantId}", tenant);
            return View("~/Views/Administrativo360/Fiscal/Configurar.cshtml", new ParametrosFiscaisConfiguracaoViewModel { Erro = "Falha ao carregar os parâmetros fiscais deste cliente." });
        }
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

        try
        {
            var repo = new ParametrosFiscaisRepository(ConnectionStringObrigatoria());
            var salvo = await repo.SalvarAsync(tenant, usuario, comando);

            TempData["Success"] = $"Parâmetros fiscais salvos. Status atual: {salvo.Status.ToUpperInvariant()}.";
        }
        catch (Exception ex)
        {
            RegistrarErro(ex, "salvar os parâmetros fiscais", tenant);
        }

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
        if (string.IsNullOrWhiteSpace(_configuration.GetConnectionString("Default")))
            return View("~/Views/Administrativo360/Fiscal/Notas.cshtml", new NotasPreEmissoesIndexViewModel { Erro = ErroSemConexao });

        var filtro = SituacoesConhecidas.Contains(situacao ?? "") ? situacao : null;

        try
        {
            var repo = new NotasPreEmitidasRepository(ConnectionStringObrigatoria());
            var notas = await repo.ListarAsync(tenant, filtro);

            return View("~/Views/Administrativo360/Fiscal/Notas.cshtml", new NotasPreEmissoesIndexViewModel
            {
                Notas = notas,
                SituacaoFiltro = filtro
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Falha ao listar pré-notas. Tenant:{TenantId}", tenant);
            return View("~/Views/Administrativo360/Fiscal/Notas.cshtml", new NotasPreEmissoesIndexViewModel { Erro = "Falha ao listar as pré-notas deste cliente." });
        }
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
        else if (string.IsNullOrWhiteSpace(_configuration.GetConnectionString("Default"))) vm.Erro = ErroSemConexao;

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

        try
        {
            var repo = new NotasPreEmitidasRepository(ConnectionStringObrigatoria());
            var id = await repo.CriarAsync(tenant, usuario, comando);

            TempData["Success"] = "Pré-nota criada em rascunho com conferência de referências.";
            return RedirectToAction(nameof(Detalhes), new { id });
        }
        catch (Exception ex)
        {
            RegistrarErro(ex, "criar a pré-nota", tenant);
            return RedirectToAction(nameof(Nova));
        }
    }

    [HttpGet("Detalhes/{id}")]
    public async Task<IActionResult> Detalhes(Guid id)
    {
        if (_currentUser.TenantId is not Guid tenant)
            return View("~/Views/Administrativo360/Fiscal/Detalhes.cshtml", new NotaPreEmissoesDetalhesViewModel { Erro = ErroSemSessao });
        if (string.IsNullOrWhiteSpace(_configuration.GetConnectionString("Default")))
            return View("~/Views/Administrativo360/Fiscal/Detalhes.cshtml", new NotaPreEmissoesDetalhesViewModel { Erro = ErroSemConexao });

        try
        {
            var notas = new NotasPreEmitidasRepository(ConnectionStringObrigatoria());
            var parametrosRepo = new ParametrosFiscaisRepository(ConnectionStringObrigatoria());

            var nota = await notas.ObterAsync(tenant, id);
            if (nota is null)
            {
                TempData["Error"] = "Pré-nota não encontrada para este cliente.";
                return RedirectToAction(nameof(Notas));
            }

            var parametros = await parametrosRepo.ObterAsync(tenant);
            var bloqueio = BloqueioDeEmissao(parametros);

            return View("~/Views/Administrativo360/Fiscal/Detalhes.cshtml", new NotaPreEmissoesDetalhesViewModel
            {
                Nota = nota,
                Transicoes = TransicoesPara(nota.Resumo.Situacao),
                StatusParametros = parametros?.Status,
                BloqueioEmissao = bloqueio
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Falha ao abrir detalhes da pré-nota. Nota:{NotaId} Tenant:{TenantId}", id, tenant);
            return View("~/Views/Administrativo360/Fiscal/Detalhes.cshtml", new NotaPreEmissoesDetalhesViewModel { Erro = "Falha ao abrir a pré-nota." });
        }
    }

    /// <summary>RASCUNHO -> PRONTA_PARA_EMISSAO.</summary>
    [HttpPost("MarcarPronta")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarPronta(Guid id) =>
        await TransicionarAsync(id, nota =>
        {
            if (!nota.Resumo.Situacao.Equals(NotaPreEmitidaSituacoes.Rascunho, StringComparison.OrdinalIgnoreCase))
                throw new Administrativo360BusinessException("Apenas uma pré-nota em rascunho pode ser marcada como pronta.");
            return (NotaPreEmitidaSituacoes.ProntaParaEmissao, "Pré-nota marcada como pronta para emissão.");
        });

    /// <summary>
    /// PRONTA -> ENVIANDO. Só avança quando os parâmetros não trazem motivo real de bloqueio
    /// (pendência P2, bloqueio declarado, credencial ausente/indisponível) — o botão existe,
    /// mas nunca devolve sucesso sem o conjunto configurado (L33/H19). Conector real = P1.
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

        try
        {
            var cs = ConnectionStringObrigatoria();
            var notas = new NotasPreEmitidasRepository(cs);
            var nota = await notas.ObterAsync(tenant, id);
            if (nota is null)
            {
                TempData["Error"] = "Pré-nota não encontrada para este cliente.";
                return RedirectToAction(nameof(Notas));
            }

            var situacao = nota.Resumo.Situacao.ToUpperInvariant();
            if (situacao == NotaPreEmitidaSituacoes.Rascunho)
                TempData["Error"] = "A pré-nota está em rascunho: use 'Marcar como pronta' antes de emitir.";
            else if (situacao != NotaPreEmitidaSituacoes.ProntaParaEmissao)
                TempData["Error"] = $"Emitir está disponível apenas na situação PRONTA_PARA_EMISSAO (atual: {nota.Resumo.Situacao}).";
            else
            {
                var parametros = await new ParametrosFiscaisRepository(cs).ObterAsync(tenant);
                var motivo = BloqueioDeEmissao(parametros);
                if (motivo is not null)
                    TempData["Error"] = $"Emissão bloqueada — {motivo}";
                else
                {
                    await notas.TransicionarAsync(tenant, usuario,
                        new TransicionarNotaPreEmitidaCommand(id, NotaPreEmitidaSituacoes.Enviando));
                    TempData["Success"] = "Pré-nota transmitida (ENVIANDO). A autorização/rejeição só é registrada com o retorno externo (conector = P1).";
                }
            }
        }
        catch (Exception ex)
        {
            RegistrarErro(ex, "transmitir a pré-nota", tenant);
        }

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    /// <summary>
    /// Volta para um estado menos avançado (matriz do Domain):
    /// PRONTA -> RASCUNHO (editar itens); ENVIANDO -> PRONTA (falha interna de transmissão);
    /// REJEITADA -> PRONTA (correção + reenvio).
    /// </summary>
    [HttpPost("Reabrir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reabrir(Guid id) =>
        await TransicionarAsync(id, nota =>
        {
            var atual = nota.Resumo.Situacao.ToUpperInvariant();
            return atual switch
            {
                NotaPreEmitidaSituacoes.ProntaParaEmissao => (NotaPreEmitidaSituacoes.Rascunho, "Pré-nota reaberta em rascunho (itens voltaram a ser editáveis)."),
                NotaPreEmitidaSituacoes.Enviando => (NotaPreEmitidaSituacoes.ProntaParaEmissao, "Transmissão retornada: a pré-nota voltou a PRONTA_PARA_EMISSAO."),
                NotaPreEmitidaSituacoes.Rejeitada => (NotaPreEmitidaSituacoes.ProntaParaEmissao, "Rejeitada corrigida: a pré-nota está pronta para novo envio."),
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

        try
        {
            var notas = new NotasPreEmitidasRepository(ConnectionStringObrigatoria());
            await notas.TransicionarAsync(tenant, usuario,
                new TransicionarNotaPreEmitidaCommand(id, NotaPreEmitidaSituacoes.Cancelada, MotivoCancelamento: motivoCancelamento.Trim()));
            TempData["Success"] = "Pré-nota cancelada.";
        }
        catch (Exception ex)
        {
            RegistrarErro(ex, "cancelar a pré-nota", tenant);
        }

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    // ------------------------------------------------------------------
    // Infraestrutura da ação (contexto, repositórios, erros, matriz p/ view)
    // ------------------------------------------------------------------

    private async Task<IActionResult> TransicionarAsync(
        Guid id,
        Func<NotaPreEmitidaDetalhesDto, (string Alvo, string MensagemSucesso)> decidir)
    {
        if (_currentUser.TenantId is not Guid tenant || _currentUser.UserId is not Guid usuario)
        {
            TempData["Error"] = ErroSemSessao;
            return RedirectToAction(nameof(Detalhes), new { id });
        }

        try
        {
            var notas = new NotasPreEmitidasRepository(ConnectionStringObrigatoria());
            var nota = await notas.ObterAsync(tenant, id);
            if (nota is null)
            {
                TempData["Error"] = "Pré-nota não encontrada para este cliente.";
                return RedirectToAction(nameof(Notas));
            }

            var (alvo, mensagem) = decidir(nota);
            await notas.TransicionarAsync(tenant, usuario,
                new TransicionarNotaPreEmitidaCommand(id, alvo));
            TempData["Success"] = mensagem;
        }
        catch (Exception ex)
        {
            RegistrarErro(ex, "alterar a situação da pré-nota", tenant);
        }

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    private string ConnectionStringObrigatoria() =>
        _configuration.GetConnectionString("Default") ?? throw new InvalidOperationException(ErroSemConexao);

    private void RegistrarErro(Exception ex, string operacao, Guid tenantId)
    {
        if (ex is not (Administrativo360BusinessException or KeyNotFoundException or ArgumentException or InvalidOperationException))
            Logger.LogError(ex, "Falha inesperada ao {Operacao}. Tenant:{TenantId}", operacao, tenantId);

        TempData["Error"] = ex switch
        {
            Administrativo360BusinessException b => b.Message,
            KeyNotFoundException k => k.Message,
            ArgumentException a => a.Message,
            InvalidOperationException i => i.Message,
            _ => $"Falha inesperada ao {operacao}. Tente novamente."
        };
    }

    /// <summary>
    /// Disponibilidade da credencial NO AMBIENTE (A33): o banco guarda só o nome da
    /// referência; o segredo em si vem da configuração (appsettings/user-secrets) sob
    /// "Fiscal:Credenciais:{nome}". Sem a chave, a emissão continua bloqueada com motivo real.
    /// </summary>
    private bool CredencialDisponivel(string? nomeReferencia)
    {
        if (string.IsNullOrWhiteSpace(nomeReferencia) || !ReferenciaSegura.IsMatch(nomeReferencia))
            return false;
        return !string.IsNullOrWhiteSpace(_configuration.GetSection(PrefixoCredenciais)[nomeReferencia]);
    }

    /// <summary>Motivo real (ou null) impedindo a emissão externa — aceita parâmetros inexistentes.</summary>
    private string? BloqueioDeEmissao(ParametrosFiscaisDto? parametros)
    {
        var snapshot = parametros is null
            ? null
            : new ParametrosFiscaisSnapshot(parametros.Status, parametros.CertificadoReferencia,
                CredencialDisponivel(parametros.CertificadoReferencia), parametros.Observacao);

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
}
