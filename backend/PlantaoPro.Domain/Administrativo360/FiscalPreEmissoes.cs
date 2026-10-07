using System.Text.RegularExpressions;

namespace PlantaoPro.Domain.Administrativo360;

/// <summary>
/// Situações da pré-nota fiscal interna (MVP A29: pré-documento + conferência de referências).
/// Estados EXTERNOS (AUTORIZADA/REJEITADA) somente com evidência válida gravada —
/// invariante espelhada nos CHECKs do banco (migração v2321). Emissão autorizada real é P1.
/// </summary>
public static class NotaPreEmitidaSituacoes
{
    public const string Rascunho = "RASCUNHO";
    public const string ProntaParaEmissao = "PRONTA_PARA_EMISSAO";
    public const string Enviando = "ENVIANDO";
    public const string Autorizada = "AUTORIZADA";
    public const string Rejeitada = "REJEITADA";
    public const string PendenteConfirmacao = "PENDENTE_CONFIRMACAO";
    public const string Cancelada = "CANCELADA";
}

/// <summary>
/// Regras de transição e evidência da pré-nota fiscal (H19: nunca status externo sem
/// evidência válida; A29: conferência de referências antes de qualquer envio).
/// </summary>
public static class NotaPreEmitidaRegras
{
    private static readonly HashSet<string> SituacoesValidas = new(StringComparer.OrdinalIgnoreCase)
    {
        NotaPreEmitidaSituacoes.Rascunho,
        NotaPreEmitidaSituacoes.ProntaParaEmissao,
        NotaPreEmitidaSituacoes.Enviando,
        NotaPreEmitidaSituacoes.Autorizada,
        NotaPreEmitidaSituacoes.Rejeitada,
        NotaPreEmitidaSituacoes.PendenteConfirmacao,
        NotaPreEmitidaSituacoes.Cancelada
    };

    private static readonly HashSet<string> OrigensValidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "MANUAL", "VENDA", "ORCAMENTO", "COTACAO"
    };

    public static void ValidarSituacao(string? situacao)
    {
        if (string.IsNullOrWhiteSpace(situacao) || !SituacoesValidas.Contains(situacao))
            throw new ArgumentException($"Situação de pré-nota inválida: '{situacao}'.");
    }

    /// <summary>
    /// Matriz oficial de transições internas:
    ///   RASCUNHO -> PRONTA_PARA_EMISSAO | CANCELADA
    ///   PRONTA_PARA_EMISSAO -> RASCUNHO | ENVIANDO | CANCELADA
    ///   ENVIANDO -> AUTORIZADA | REJEITADA | PENDENTE_CONFIRMACAO | PRONTA_PARA_EMISSAO
    ///     (retorno a PRONTA cobre falha interna de transmissão, ainda sem tentativa externa registrada)
    ///   PENDENTE_CONFIRMACAO -> AUTORIZADA | REJEITADA | CANCELADA
    ///     (consulta externa confere ou nega a autorização duvidosa)
    ///   REJEITADA -> PRONTA_PARA_EMISSAO | CANCELADA  (correção + reenvio)
    ///   AUTORIZADA / CANCELADA -> terminais
    ///     (o cancelamento OFICIAL de autorizada depende da consulta/cancelamento externo — P1)
    /// </summary>
    public static void ValidarTransicao(string situacaoAtual, string novaSituacao)
    {
        ValidarSituacao(situacaoAtual);
        ValidarSituacao(novaSituacao);

        var atual = situacaoAtual.ToUpperInvariant();
        var novo = novaSituacao.ToUpperInvariant();
        if (atual == novo) return;

        bool permitida = (atual, novo) switch
        {
            (NotaPreEmitidaSituacoes.Rascunho, NotaPreEmitidaSituacoes.ProntaParaEmissao) => true,
            (NotaPreEmitidaSituacoes.ProntaParaEmissao, NotaPreEmitidaSituacoes.Rascunho) => true,
            (NotaPreEmitidaSituacoes.ProntaParaEmissao, NotaPreEmitidaSituacoes.Enviando) => true,
            (NotaPreEmitidaSituacoes.Enviando, NotaPreEmitidaSituacoes.Autorizada) => true,
            (NotaPreEmitidaSituacoes.Enviando, NotaPreEmitidaSituacoes.Rejeitada) => true,
            (NotaPreEmitidaSituacoes.Enviando, NotaPreEmitidaSituacoes.PendenteConfirmacao) => true,
            (NotaPreEmitidaSituacoes.Enviando, NotaPreEmitidaSituacoes.ProntaParaEmissao) => true,
            (NotaPreEmitidaSituacoes.PendenteConfirmacao, NotaPreEmitidaSituacoes.Autorizada) => true,
            (NotaPreEmitidaSituacoes.PendenteConfirmacao, NotaPreEmitidaSituacoes.Rejeitada) => true,
            (NotaPreEmitidaSituacoes.Rejeitada, NotaPreEmitidaSituacoes.ProntaParaEmissao) => true,
            (_, NotaPreEmitidaSituacoes.Cancelada) when atual is not NotaPreEmitidaSituacoes.Autorizada => true,
            _ => false
        };

        if (!permitida)
            throw new Administrativo360BusinessException($"Transição inválida para pré-nota: '{atual}' -> '{novo}'.");
    }

    /// <summary>
    /// Evidência externa mínima exigida pela situação-alvo (espelha os CHECKs de v2321):
    /// AUTORIZADA exige chave de acesso válida (44 dígitos) + momento; REJEITADA exige momento.
    /// Estados internos não exigem evidência externa.
    /// </summary>
    public static void ValidarEvidenciaParaSituacao(
        string novaSituacao,
        string? chaveAcessoExterna,
        DateTime? emitidaEm,
        string? rejeicaoMensagem)
    {
        var alvo = novaSituacao.ToUpperInvariant();
        switch (alvo)
        {
            case NotaPreEmitidaSituacoes.Autorizada:
                if (!ChaveAcessoValida(chaveAcessoExterna))
                    throw new Administrativo360BusinessException(
                        "Não é possível registrar AUTORIZADA sem chave de acesso externa válida (44 dígitos).");
                if (emitidaEm is null)
                    throw new Administrativo360BusinessException(
                        "Não é possível registrar AUTORIZADA sem o momento da autorização (emitida_em).");
                break;
            case NotaPreEmitidaSituacoes.Rejeitada:
                if (emitidaEm is null)
                    throw new Administrativo360BusinessException(
                        "Não é possível registrar REJEITADA sem o momento do retorno externo (emitida_em).");
                if (string.IsNullOrWhiteSpace(rejeicaoMensagem))
                    throw new Administrativo360BusinessException(
                        "A rejeição externa exige a mensagem devolvida pelo emissor.");
                break;
        }
    }

    /// <summary>Chave de acesso NF-e: exatamente 44 dígitos numéricos.</summary>
    public static bool ChaveAcessoValida(string? chave) =>
        chave != null && Regex.IsMatch(chave, @"^\d{44}$");

    /// <summary>A29: conferência de referências — origem não-manual exige identificador do documento.</summary>
    public static void ValidarOrigem(string origemTipo, Guid? origemId)
    {
        if (string.IsNullOrWhiteSpace(origemTipo) || !OrigensValidas.Contains(origemTipo))
            throw new ArgumentException($"Tipo de origem inválido para pré-nota: '{origemTipo}'.");
        if (!origemTipo.Equals("MANUAL", StringComparison.OrdinalIgnoreCase) && origemId is null)
            throw new Administrativo360BusinessException(
                $"Pré-nota com origem '{origemTipo.ToUpperInvariant()}' exige o identificador do documento de origem.");
    }

    /// <summary>CANCELADA exige momento + motivo (invariante de v2321).</summary>
    public static void ValidarCancelamento(DateTime? canceladaEm, string? motivoCancelamento)
    {
        if (canceladaEm is null)
            throw new Administrativo360BusinessException("O cancelamento da pré-nota exige o momento registrado.");
        if (string.IsNullOrWhiteSpace(motivoCancelamento))
            throw new Administrativo360BusinessException("O motivo do cancelamento da pré-nota é obrigatório.");
    }

    /// <summary>Bloqueio explícito de "Emitir" derivado dos parâmetros (P2 formal). Nunca sucesso fictício.</summary>
    public static string? MotivoBloqueioEmissao(ParametrosFiscaisSnapshot parametros)
    {
        if (parametros is null)
            return "Parâmetros fiscais ainda não cadastrados para este cliente.";

        if (parametros.Status.Equals(ParametrosFiscaisRegras.PendenteDeConfiguracao, StringComparison.OrdinalIgnoreCase))
            return "Pendência de configuração fiscal (P2): definam operação, UF/regime e provedor antes de emitir.";

        if (parametros.Status.Equals(ParametrosFiscaisRegras.Bloqueado, StringComparison.OrdinalIgnoreCase))
            return $"Emissão externa bloqueada: {parametros.Observacao ?? "ver os parâmetros fiscais do cliente."}";

        if (string.IsNullOrWhiteSpace(parametros.CertificadoReferencia))
            return "Credencial de emissão não referenciada: informe o nome do segredo (certificado/token) no parâmetro fiscal.";

        if (!parametros.CredencialDisponivelNoAmbiente)
            return $"Credencial '{parametros.CertificadoReferencia}' indisponível neste ambiente; forneça o segredo antes de emitir.";

        return null; // configurado + credencial presente: emissão liberada (conector real = P1)
    }
}

/// <summary>Visão somente-leitura dos parâmetros usada pelas regras de bloqueio (sem acoplar o DTO do app).</summary>
public sealed record ParametrosFiscaisSnapshot(
    string Status,
    string? CertificadoReferencia,
    bool CredencialDisponivelNoAmbiente,
    string? Observacao
);

/// <summary>
/// Regras dos parâmetros de emissão fiscal: a decisão comercial ausente é registrada como
/// pendência (P2) — nenhum campo obrigatório vazio significa "configurado" (sem sucesso falso).
/// </summary>
public static class ParametrosFiscaisRegras
{
    public const string PendenteDeConfiguracao = "PENDENTE_DE_CONFIGURACAO";
    public const string Configurado = "CONFIGURADO";
    public const string Bloqueado = "BLOQUEADO";

    private static readonly HashSet<string> RegimesValidos = new(StringComparer.OrdinalIgnoreCase)
    { "SIMPLES_NACIONAL", "LUCRO_PRESUMIDO", "LUCRO_REAL" };

    private static readonly HashSet<string> OperacoesValidas = new(StringComparer.OrdinalIgnoreCase)
    { "VENDA", "REMESSA", "RETORNO" };

    private static readonly HashSet<string> AmbientesValidos = new(StringComparer.OrdinalIgnoreCase)
    { "PENDENTE", "HOMOLOGACAO", "PRODUCAO" };

    private static readonly HashSet<string> ProvedoresValidos = new(StringComparer.OrdinalIgnoreCase)
    { "SEFAZ_DIRETO", "OPMENEXO", "INPART", "OUTRO" };

    /// <summary>Identificador simples: garante que certificado_referencia guarda NOME de segredo, nunca o valor (A33).</summary>
    private static readonly Regex ReferenciaSegredo = new("^[A-Za-z0-9._\\-]{1,120}$", RegexOptions.Compiled);

    public static void ValidarCampos(
        string? uf,
        string? municipio,
        string? regimeFiscal,
        string? operacaoFiscal,
        IDictionary<string, string>? cfops,
        string ambiente,
        string? provedor,
        string? certificadoReferencia)
    {
        if (uf is not null && uf.Trim().Length != 2)
            throw new Administrativo360BusinessException("A UF deve ter exatamente 2 letras (ex.: SP).");

        if (regimeFiscal is not null && !RegimesValidos.Contains(regimeFiscal))
            throw new Administrativo360BusinessException($"Regime fiscal inválido: '{regimeFiscal}'.");

        if (operacaoFiscal is not null && !OperacoesValidas.Contains(operacaoFiscal))
            throw new Administrativo360BusinessException($"Operação fiscal inválida: '{operacaoFiscal}'.");

        if (!AmbientesValidos.Contains(ambiente ?? string.Empty))
            throw new Administrativo360BusinessException($"Ambiente inválido: '{ambiente}'.");

        if (provedor is not null && !ProvedoresValidos.Contains(provedor))
            throw new Administrativo360BusinessException($"Provedor de emissão inválido: '{provedor}'.");

        if (!string.IsNullOrWhiteSpace(certificadoReferencia) && !ReferenciaSegredo.IsMatch(certificadoReferencia.Trim()))
            throw new Administrativo360BusinessException(
                "O campo de credencial aceita apenas o NOME da referência do segredo (letras, números, ponto, hífen, sublinhado — máx. 120). Nunca grave o valor do certificado aqui.");

        // CFOP por operação (jsonb): sem CFOP universal; chaves restritas às operações canônicas
        // e valores numéricos de 4 dígitos. Ausente/vazio = pendência de parametrização (A30/L376).
        if (cfops is not null)
        {
            foreach (var (operacao, cfop) in cfops)
            {
                if (!OperacoesValidas.Contains(operacao))
                    throw new Administrativo360BusinessException($"CFOP informado para operação desconhecida: '{operacao}'.");
                if (string.IsNullOrWhiteSpace(cfop) || !Regex.IsMatch(cfop, @"^\d{4}$"))
                    throw new Administrativo360BusinessException($"CFOP inválido para '{operacao}': deve ser numérico de 4 dígitos.");
            }
        }
    }

    /// <summary>Conjunto comercial completo: só então a pendência P2 deixa de existir.</summary>
    public static bool ConjuntoCompleto(
        string? uf,
        string? municipio,
        string? regimeFiscal,
        string? operacaoFiscal,
        string? provedor,
        string ambiente)
    {
        return uf is not null
            && !string.IsNullOrWhiteSpace(municipio)
            && regimeFiscal is not null
            && operacaoFiscal is not null
            && provedor is not null
            && (ambiente.Equals("HOMOLOGACAO", StringComparison.OrdinalIgnoreCase)
                || ambiente.Equals("PRODUCAO", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Status derivado (determinístico e espelhado no CHECK do banco): incompleto =
    /// PENDENTE_DE_CONFIGURACAO (P2 visível); completo = BLOQUEADO se houver bloqueio
    /// externo declarado, senão CONFIGURADO.
    /// </summary>
    public static string DerivarStatus(bool conjuntoCompleto, bool bloqueioExterno)
    {
        if (!conjuntoCompleto) return PendenteDeConfiguracao;
        return bloqueioExterno ? Bloqueado : Configurado;
    }
}
