using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Web.Models;

// ============================================================================
// Fiscal — pré-emissão (R4-F2). ViewModels das páginas MVC; os DTOs vêm do
// Application (FiscalPreEmissoesContracts) e as regras de exibição do Domain.
// ============================================================================

/// <summary>Formulário + estado dos parâmetros fiscais (Configurar).</summary>
public sealed class ParametrosFiscaisConfiguracaoViewModel
{
    public bool JaCadastrado { get; init; }

    public string? Uf { get; init; }
    public string? Municipio { get; init; }
    public string? RegimeFiscal { get; init; }
    public string? OperacaoFiscal { get; init; }
    public string? CfopVenda { get; init; }
    public string? CfopRemessa { get; init; }
    public string? CfopRetorno { get; init; }
    public string? ResponsavelNome { get; init; }
    public string Ambiente { get; init; } = "PENDENTE";
    public string? Provedor { get; init; }
    public string? CertificadoReferencia { get; init; }
    public string? Observacao { get; init; }
    public bool BloqueioExterno { get; init; }

    /// <summary>Status atual derivado (null quando ainda não cadastrado — P2 sempre visível).</summary>
    public string? StatusAtual { get; init; }

    /// <summary>Decisões comerciais ausentes (pendência P2) — lista explícita, nunca omissão.</summary>
    public IReadOnlyList<string> PendenciasP2 { get; init; } = Array.Empty<string>();

    /// <summary>Disponibilidade do segredo NO AMBIENTE da API (A33) — null quando desconhecido.</summary>
    public bool? CredencialDisponivelNoAmbiente { get; init; }

    /// <summary>Existe conector registrado para o provedor no ambiente (P1) — null quando desconhecido.</summary>
    public bool? TransmissorRegistradoNoAmbiente { get; init; }

    public string? Erro { get; init; }
}

/// <summary>Lista de pré-notas com filtro por situação.</summary>
public sealed class NotasPreEmissoesIndexViewModel
{
    public IReadOnlyList<NotaPreEmitidaResumoDto> Notas { get; init; } = Array.Empty<NotaPreEmitidaResumoDto>();
    public string? SituacaoFiltro { get; init; }
    public string? Erro { get; init; }
}

/// <summary>Linha editável do formulário de criação (itens em linhas fixas, sem JS).</summary>
public sealed class NovaNotaItemLinha
{
    // string? deliberada: com Nullable enable, string não-nula vira [Required] implícito
    // no model binding e quebraria as linhas vazias ("linhas sem descrição são ignoradas").
    public string? Descricao { get; set; }
    public decimal Quantidade { get; set; } = 1;
    public decimal PrecoUnitario { get; set; }
}

/// <summary>Formulário "Nova pré-nota" (criação em RASCUNHO com conferência de origem — A29).</summary>
public sealed class NovaNotaPreEmissoesViewModel
{
    public string OrigemTipo { get; set; } = "MANUAL";
    public Guid? OrigemId { get; set; }
    public string DestinatarioNome { get; set; } = "";
    public string? DestinatarioDocumento { get; set; }

    /// <summary>Linhas fixas (até 8); apenas linhas com descrição entram na pré-nota.</summary>
    public List<NovaNotaItemLinha> Itens { get; set; } = new() { new() };

    public string? Erro { get; set; }
}

/// <summary>Estatos possíveis da pré-nota, precomputados no controller (matriz do Domain).</summary>
public sealed record TransicoesPossiveis(
    bool PodeMarcarPronta,
    bool PodeEmitir,
    bool PodeReabrir,
    bool PodeCancelar);

/// <summary>Página de detalhes: pré-nota + conferência de origem + bloqueio real de emissão (H19/L33).</summary>
public sealed class NotaPreEmissoesDetalhesViewModel
{
    public NotaPreEmitidaDetalhesDto? Nota { get; init; }
    public TransicoesPossiveis? Transicoes { get; init; }

    /// <summary>Status derivado dos parâmetros fiscais do tenant (null = ainda não cadastrados).</summary>
    public string? StatusParametros { get; init; }

    /// <summary>Motivo real (ou nulo) impedindo a emissão externa — nunca sucesso fictício (L33/H19).</summary>
    public string? BloqueioEmissao { get; init; }

    /// <summary>Parametros ok mas sem conector no ambiente: Emissao autorizada INDISPONIVEL (P1).</summary>
    public bool? TransmissaoIndisponivelNoAmbiente { get; init; }

    /// <summary>Provedor configurado nos parametros (para nomear o conector ausente).</summary>
    public string? ProvedorConfigurado { get; init; }

    public string? Erro { get; init; }
}
