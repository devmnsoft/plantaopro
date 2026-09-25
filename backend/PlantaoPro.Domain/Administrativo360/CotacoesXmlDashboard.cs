using System.Text.RegularExpressions;

namespace PlantaoPro.Domain.Administrativo360;

public static class CotacaoRegras
{
    private static readonly HashSet<string> StatusValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "RECEBIDA", "EM_RELACIONAMENTO", "EM_ORCAMENTO", "AGUARDANDO_APROVACAO",
        "PRONTA_PARA_ENVIO", "RESPONDIDA", "CANCELADA", "EXPIRADA"
    };

    public static void ValidarTransicaoStatus(string statusAtual, string novoStatus)
    {
        if (!StatusValidos.Contains(statusAtual))
            throw new ArgumentException($"Situação atual da cotação '{statusAtual}' é inválida.");

        if (!StatusValidos.Contains(novoStatus))
            throw new ArgumentException($"Nova situação da cotação '{novoStatus}' é inválida.");

        var sAtual = statusAtual.ToUpperInvariant();
        var sNovo = novoStatus.ToUpperInvariant();

        if (sAtual == sNovo) return;

        if (sAtual is "RESPONDIDA" or "CANCELADA" or "EXPIRADA")
            throw new InvalidOperationException($"Cotação na situação '{sAtual}' não permite alteração de status.");

        if (sNovo is "CANCELADA" or "EXPIRADA") return;

        bool transicaoPermitida = (sAtual, sNovo) switch
        {
            ("RECEBIDA", "EM_RELACIONAMENTO") => true,
            ("RECEBIDA", "EM_ORCAMENTO") => true,
            ("EM_RELACIONAMENTO", "EM_ORCAMENTO") => true,
            ("EM_RELACIONAMENTO", "AGUARDANDO_APROVACAO") => true,
            ("EM_ORCAMENTO", "AGUARDANDO_APROVACAO") => true,
            ("AGUARDANDO_APROVACAO", "PRONTA_PARA_ENVIO") => true,
            ("AGUARDANDO_APROVACAO", "EM_ORCAMENTO") => true, // devolução para ajuste
            ("PRONTA_PARA_ENVIO", "RESPONDIDA") => true,
            _ => false
        };

        if (!transicaoPermitida)
            throw new InvalidOperationException($"Transição de status inválida para cotação: '{sAtual}' -> '{sNovo}'.");
    }

    public static void ValidarPrazoResposta(DateTime prazoUtc, DateTime agoraUtc)
    {
        if (agoraUtc > prazoUtc)
            throw new InvalidOperationException($"O prazo de resposta da cotação expirou em {prazoUtc:dd/MM/yyyy HH:mm} UTC.");
    }

    public static decimal CalcularQuantidadeConvertida(decimal quantidadeSolicitada, decimal fatorConversao)
    {
        if (quantidadeSolicitada <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantidadeSolicitada), "Quantidade solicitada deve ser maior que zero.");

        if (fatorConversao <= 0)
            throw new ArgumentOutOfRangeException(nameof(fatorConversao), "Fator de conversão deve ser maior que zero.");

        return Math.Round(quantidadeSolicitada * fatorConversao, 4);
    }

    public static void ValidarPendenciasParaEnvio(
        IReadOnlyCollection<(int NumeroItem, string StatusRelacionamento, string? MotivoNaoAtendimento, Guid? ProdutoId)> itens,
        Guid? orcamentoId)
    {
        if (itens is null || itens.Count == 0)
            throw new InvalidOperationException("A cotação não possui itens para envio de resposta.");

        var pendentes = itens.Where(i => i.StatusRelacionamento == "PENDENTE").ToList();
        if (pendentes.Count > 0)
        {
            var nums = string.Join(", ", pendentes.Select(p => $"Item {p.NumeroItem}"));
            throw new InvalidOperationException($"Não é possível aprovar ou enviar resposta: existem itens sem relacionamento de produto ({nums}).");
        }

        var naoAtendidosSemMotivo = itens.Where(i => i.StatusRelacionamento == "NAO_ATENDIDO" && string.IsNullOrWhiteSpace(i.MotivoNaoAtendimento)).ToList();
        if (naoAtendidosSemMotivo.Count > 0)
        {
            var nums = string.Join(", ", naoAtendidosSemMotivo.Select(p => $"Item {p.NumeroItem}"));
            throw new InvalidOperationException($"Itens marcados como não atendidos exigem justificativa obrigatória ({nums}).");
        }

        var atendidos = itens.Where(i => i.StatusRelacionamento == "RELACIONADO").ToList();
        if (atendidos.Count > 0 && (!orcamentoId.HasValue || orcamentoId.Value == Guid.Empty))
            throw new InvalidOperationException("Cotação com itens relacionados exige orçamento cirúrgico vinculado antes do envio.");
    }
}

public static class XmlDocumentoRegras
{
    private static readonly Regex ChaveNfeRegex = new(@"^\d{44}$", RegexOptions.Compiled);

    public static void ValidarChaveAcesso(string chaveAcesso)
    {
        if (string.IsNullOrWhiteSpace(chaveAcesso))
            throw new ArgumentException("Chave de acesso é obrigatória.", nameof(chaveAcesso));

        var limpa = chaveAcesso.Trim();
        if (!ChaveNfeRegex.IsMatch(limpa))
            throw new ArgumentException($"Chave de acesso inválida. Deve conter exatamente 44 dígitos numéricos. Recebido: '{limpa}'.", nameof(chaveAcesso));

        // Modelo 55 da NF-e está nas posições 21 e 22 (índice 20 e 21 em 0-based)
        var modelo = limpa.Substring(20, 2);
        if (modelo != "55")
            throw new InvalidOperationException($"Modelo de documento fiscal '{modelo}' não suportado neste módulo. O escopo é estritamente NF-e Modelo 55.");
    }

    public static void ValidarDestinatarioAutorizado(string cnpjDestinatario, IEnumerable<string> cnpjsAutorizados)
    {
        if (string.IsNullOrWhiteSpace(cnpjDestinatario))
            throw new ArgumentException("CNPJ do destinatário não informado no documento fiscal.");

        var limpoDest = Regex.Replace(cnpjDestinatario, @"\D", "");
        var autorizadosLimpos = cnpjsAutorizados.Select(c => Regex.Replace(c, @"\D", "")).ToHashSet();

        if (!autorizadosLimpos.Contains(limpoDest))
            throw new InvalidOperationException($"O CNPJ do destinatário '{limpoDest}' não pertence a nenhum estabelecimento autorizado deste tenant.");
    }

    public static void ValidarIntervaloDfe(DateTime? ultimaConsultaUtc, DateTime agoraUtc, int intervaloMinutosMinimo = 60)
    {
        if (!ultimaConsultaUtc.HasValue) return;

        var proximaPermitida = ultimaConsultaUtc.Value.AddMinutes(intervaloMinutosMinimo);
        if (agoraUtc < proximaPermitida)
        {
            var restante = proximaPermitida - agoraUtc;
            throw new InvalidOperationException($"Respeito ao intervalo da SEFAZ: próxima consulta DF-e permitida apenas após {proximaPermitida:dd/MM/yyyy HH:mm:ss} UTC (restam {Math.Ceiling(restante.TotalMinutes)} minutos).");
        }
    }
}

public static class CapacidadeContratadaRegras
{
    public static void ValidarCapacidadeAtiva(string capacidade, IEnumerable<string> capacidadesAtivas)
    {
        var ativas = new HashSet<string>(capacidadesAtivas, StringComparer.OrdinalIgnoreCase);
        if (!ativas.Contains(capacidade))
            throw new InvalidOperationException($"A capacidade '{capacidade}' não está contratada ou habilitada para este tenant no módulo Administrativo 360.");
    }
}
