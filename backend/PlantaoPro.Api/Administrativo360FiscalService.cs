using System.Text.RegularExpressions;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;

namespace PlantaoPro.Api;

// ============================================================================
// Fiscal ADM360 no lado da API (R5-A2): unico caminho de escrita das pre-notas
// e dos parametros (o Web virou BFF fino e nao toca mais no banco fiscal).
// Sem sucesso ficticio:
//   - motivo de bloqueio derivado dos parametros -> 200 {Avancou=false, Mensagem};
//   - conector ausente no ambiente -> 400 honesto (BusinessException), sem mudar estado;
//   - ENVIANDO so existe quando um transmissor real executou (conector = P1).
// ============================================================================
public sealed class Administrativo360FiscalService
{
    private const string PrefixoCredenciais = "Fiscal:Credenciais";

    // Mesma restricao de segredo da regra de dominio (A33): o campo guarda NOME de referencia.
    private static readonly Regex ReferenciaSegura = new("^[A-Za-z0-9._\\-]{1,120}$", RegexOptions.Compiled);

    private readonly IConfiguration configuration;
    private readonly ICurrentUserService currentUser;
    private readonly FiscalTransmissorCatalogo catalogo;
    private readonly ILogger<Administrativo360FiscalService> logger;

    public Administrativo360FiscalService(
        IConfiguration configuration,
        ICurrentUserService currentUser,
        FiscalTransmissorCatalogo catalogo,
        ILogger<Administrativo360FiscalService> logger)
    {
        this.configuration = configuration;
        this.currentUser = currentUser;
        this.catalogo = catalogo;
        this.logger = logger;
    }

    private string ConnectionString() =>
        configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string 'Default' ausente na configuração da API.");

    private Guid Tenant() => currentUser.TenantId ?? throw new UnauthorizedAccessException("Selecione uma organização.");
    private Guid Usuario() => currentUser.UserId ?? throw new UnauthorizedAccessException("Usuário não autenticado.");

    /// <summary>
    /// Disponibilidade da credencial NO AMBIENTE (A33): o banco guarda so o nome da
    /// referencia; o segredo em si vem da configuracao (appsettings/user-secrets do
    /// pool da API) sob "Fiscal:Credenciais:{nome}".
    /// </summary>
    private bool CredencialDisponivel(string? nomeReferencia)
    {
        if (string.IsNullOrWhiteSpace(nomeReferencia) || !ReferenciaSegura.IsMatch(nomeReferencia))
            return false;
        return !string.IsNullOrWhiteSpace(configuration.GetSection(PrefixoCredenciais)[nomeReferencia]);
    }

    private ParametrosFiscaisAmbienteDto AmbienteDe(ParametrosFiscaisDto? dto) =>
        new(dto,
            dto is not null && CredencialDisponivel(dto.CertificadoReferencia),
            catalogo.TemPara(dto?.Provedor));

    public async Task<ParametrosFiscaisAmbienteDto> ObterParametrosAsync(CancellationToken ct)
    {
        var repo = new ParametrosFiscaisRepository(ConnectionString());
        return AmbienteDe(await repo.ObterAsync(Tenant(), ct));
    }

    public async Task<ParametrosFiscaisAmbienteDto> SalvarParametrosAsync(SalvarParametrosFiscaisCommand comando, CancellationToken ct)
    {
        var repo = new ParametrosFiscaisRepository(ConnectionString());
        var dto = await repo.SalvarAsync(Tenant(), Usuario(), comando, ct);
        logger.LogInformation("Parâmetros fiscais salvos. Tenant:{TenantId} Status:{Status}", Tenant(), dto.Status);
        return AmbienteDe(dto);
    }

    public async Task<IReadOnlyList<NotaPreEmitidaResumoDto>> ListarNotasAsync(string? situacao, CancellationToken ct)
    {
        var repo = new NotasPreEmitidasRepository(ConnectionString());
        return await repo.ListarAsync(Tenant(), situacao, ct);
    }

    public async Task<NotaPreEmitidaDetalhesDto> ObterNotaAsync(Guid id, CancellationToken ct)
    {
        var repo = new NotasPreEmitidasRepository(ConnectionString());
        return await repo.ObterAsync(Tenant(), id, ct)
            ?? throw new KeyNotFoundException("Pré-nota não encontrada.");
    }

    public async Task<Guid> CriarNotaAsync(CriarNotaPreEmitidaCommand comando, CancellationToken ct)
    {
        var repo = new NotasPreEmitidasRepository(ConnectionString());
        var tenant = Tenant();
        var id = await repo.CriarAsync(tenant, Usuario(), comando, ct);
        logger.LogInformation("Pré-nota criada. Nota:{NotaId} Tenant:{TenantId}", id, tenant);
        return id;
    }

    /// <summary>RASCUNHO -> PRONTA_PARA_EMISSAO (validacao server-side; auditor nao passa).</summary>
    public async Task<NotaPreEmitidaDetalhesDto> MarcarProntaAsync(Guid id, CancellationToken ct)
    {
        var tenant = Tenant();
        var usuario = Usuario();
        var repo = new NotasPreEmitidasRepository(ConnectionString());
        var nota = await repo.ObterAsync(tenant, id, ct)
            ?? throw new KeyNotFoundException("Pré-nota não encontrada.");
        if (!nota.Resumo.Situacao.Equals(NotaPreEmitidaSituacoes.Rascunho, StringComparison.OrdinalIgnoreCase))
            throw new Administrativo360BusinessException("Apenas uma pré-nota em rascunho pode ser marcada como pronta.");
        await repo.TransicionarAsync(tenant, usuario,
            new TransicionarNotaPreEmitidaCommand(id, NotaPreEmitidaSituacoes.ProntaParaEmissao), ct);
        return await repo.ObterAsync(tenant, id, ct)
            ?? throw new KeyNotFoundException("Pré-nota não encontrada.");
    }

    /// <summary>
    /// Volta para um estado menos avancado (matriz do Domain, alvo server-side):
    /// PRONTA -> RASCUNHO (editar itens); ENVIANDO -> PRONTA (falha interna de
    /// transmissao); REJEITADA -> PRONTA (correcao + reenvio).
    /// </summary>
    public async Task<NotaPreEmitidaDetalhesDto> ReabrirAsync(Guid id, CancellationToken ct)
    {
        var tenant = Tenant();
        var usuario = Usuario();
        var repo = new NotasPreEmitidasRepository(ConnectionString());
        var nota = await repo.ObterAsync(tenant, id, ct)
            ?? throw new KeyNotFoundException("Pré-nota não encontrada.");
        var atual = nota.Resumo.Situacao.ToUpperInvariant();
        var alvo = atual switch
        {
            NotaPreEmitidaSituacoes.ProntaParaEmissao => NotaPreEmitidaSituacoes.Rascunho,
            NotaPreEmitidaSituacoes.Enviando => NotaPreEmitidaSituacoes.ProntaParaEmissao,
            NotaPreEmitidaSituacoes.Rejeitada => NotaPreEmitidaSituacoes.ProntaParaEmissao,
            _ => throw new Administrativo360BusinessException($"A situação {atual} não pode ser reaberta agora.")
        };
        await repo.TransicionarAsync(tenant, usuario, new TransicionarNotaPreEmitidaCommand(id, alvo), ct);
        return await repo.ObterAsync(tenant, id, ct)
            ?? throw new KeyNotFoundException("Pré-nota não encontrada.");
    }

    /// <summary>Cancelamento interno (motivo obrigatorio). AUTORIZADA/CANCELADA bloqueadas.</summary>
    public async Task<NotaPreEmitidaDetalhesDto> CancelarAsync(Guid id, string? motivo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new Administrativo360BusinessException("Informe o motivo do cancelamento (obrigatório).");
        var tenant = Tenant();
        var usuario = Usuario();
        var repo = new NotasPreEmitidasRepository(ConnectionString());
        var nota = await repo.ObterAsync(tenant, id, ct)
            ?? throw new KeyNotFoundException("Pré-nota não encontrada.");
        var atual = nota.Resumo.Situacao.ToUpperInvariant();
        if (atual == NotaPreEmitidaSituacoes.Autorizada)
            throw new Administrativo360BusinessException("Pré-nota autorizada não pode ser cancelada por aqui: o cancelamento oficial depende da consulta externa (P1).");
        if (atual == NotaPreEmitidaSituacoes.Cancelada)
            throw new Administrativo360BusinessException("A pré-nota já está cancelada.");
        await repo.TransicionarAsync(tenant, usuario,
            new TransicionarNotaPreEmitidaCommand(id, NotaPreEmitidaSituacoes.Cancelada, MotivoCancelamento: motivo.Trim()), ct);
        return await repo.ObterAsync(tenant, id, ct)
            ?? throw new KeyNotFoundException("Pré-nota não encontrada.");
    }

    /// <summary>
    /// PRONTA -> ENVIANDO honesto: bloqueio de parametros devolve 200 sem avancar;
    /// sem conector registrado, 400 honesto SEM mudar estado; com conector real (P1),
    /// transiciona para ENVIANDO, delega e grava o resultado com evidencia.
    /// </summary>
    public async Task<ResultadoEmissaoFiscal> EmitirAsync(Guid id, CancellationToken ct)
    {
        var tenant = Tenant();
        var usuario = Usuario();
        var notas = new NotasPreEmitidasRepository(ConnectionString());
        var parametrosRepo = new ParametrosFiscaisRepository(ConnectionString());

        var nota = await notas.ObterAsync(tenant, id, ct)
            ?? throw new KeyNotFoundException("Pré-nota não encontrada.");
        var situacao = nota.Resumo.Situacao.ToUpperInvariant();
        if (situacao == NotaPreEmitidaSituacoes.Rascunho)
            throw new Administrativo360BusinessException("A pré-nota está em rascunho: marque como pronta antes de emitir.");
        if (situacao != NotaPreEmitidaSituacoes.ProntaParaEmissao)
            throw new Administrativo360BusinessException($"Emitir está disponível apenas na situação PRONTA_PARA_EMISSAO (atual: {nota.Resumo.Situacao}).");

        var parametros = await parametrosRepo.ObterAsync(tenant, ct);
        var snapshot = parametros is null
            ? null
            : new ParametrosFiscaisSnapshot(parametros.Status, parametros.CertificadoReferencia,
                CredencialDisponivel(parametros.CertificadoReferencia), parametros.Observacao);

        // A regra trata null como "ainda não cadastrados"; a anotação só não cobre esse caso.
        var motivo = NotaPreEmitidaRegras.MotivoBloqueioEmissao(snapshot!);
        if (motivo is not null)
            return new ResultadoEmissaoFiscal(false, nota.Resumo.Situacao, $"Emissão bloqueada — {motivo}");

        // Sem bloqueio de parametros: exige conector real. Ausente = 400 honesto, sem transicao.
        NotaPreEmitidaRegras.ValidarTransmissorDisponivel(parametros!.Provedor, catalogo.TemPara(parametros.Provedor));
        var transmissor = catalogo.Obter(parametros.Provedor);

        await notas.TransicionarAsync(tenant, usuario,
            new TransicionarNotaPreEmitidaCommand(id, NotaPreEmitidaSituacoes.Enviando), ct);
        try
        {
            var resultado = await transmissor.TransmitirAsync(nota, parametros, ct);
            var final = (resultado.SituacaoAtual ?? NotaPreEmitidaSituacoes.Enviando).ToUpperInvariant();
            if (resultado.Avancou && final != NotaPreEmitidaSituacoes.Enviando)
            {
                await notas.TransicionarAsync(tenant, usuario,
                    new TransicionarNotaPreEmitidaCommand(id, final,
                        ChaveAcessoExterna: resultado.ChaveAcessoExterna,
                        EmitidaEm: resultado.EmitidaEm,
                        RejeicaoMensagem: final == NotaPreEmitidaSituacoes.Rejeitada ? resultado.Mensagem : null), ct);
            }
            var atualizada = await notas.ObterAsync(tenant, id, ct);
            return resultado with { SituacaoAtual = atualizada?.Resumo.Situacao ?? final };
        }
        catch (Exception ex)
        {
            // Falha tecnica do conector: desfaz o ENVIANDO (matriz permite ENVIANDO -> PRONTA
            // como "falha interna de transmissao") para nao deixar estado orfao; o erro
            // original propaga (500 + log com correlacao, nunca sucesso).
            logger.LogError(ex, "Falha técnica do transmissor fiscal. Nota:{NotaId} Tenant:{TenantId}", id, tenant);
            await notas.TransicionarAsync(tenant, usuario,
                new TransicionarNotaPreEmitidaCommand(id, NotaPreEmitidaSituacoes.ProntaParaEmissao), ct);
            throw;
        }
    }
}
