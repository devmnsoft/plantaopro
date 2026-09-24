namespace PlantaoPro.Domain.Administrativo360;

public static class ConsignacaoCirurgicaRegras
{
    // Invariantes de Cirurgia
    public static bool PodeAlterarCirurgia(string situacao) =>
        string.Equals(situacao, "AGENDADA", StringComparison.OrdinalIgnoreCase);

    public static bool PodeCancelarCirurgia(string situacao, bool temExpedicao) =>
        !temExpedicao && !string.Equals(situacao, "REALIZADA", StringComparison.OrdinalIgnoreCase)
                      && !string.Equals(situacao, "CANCELADA", StringComparison.OrdinalIgnoreCase);

    // Invariantes de Vale de Consignação
    public static bool PodeEditarVale(string situacao) =>
        string.Equals(situacao, "RASCUNHO", StringComparison.OrdinalIgnoreCase);

    public static bool PodeSepararVale(string situacao) =>
        string.Equals(situacao, "RASCUNHO", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(situacao, "EM_SEPARACAO", StringComparison.OrdinalIgnoreCase);

    public static bool PodeExpedirVale(string situacao) =>
        string.Equals(situacao, "PRONTO_PARA_EXPEDICAO", StringComparison.OrdinalIgnoreCase);

    public static bool PodeRegistrarEventos(string situacao) =>
        string.Equals(situacao, "EXPEDIDO", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(situacao, "RETORNO_PARCIAL", StringComparison.OrdinalIgnoreCase);

    public static bool PodeCancelarVale(string situacao) =>
        string.Equals(situacao, "RASCUNHO", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(situacao, "EM_SEPARACAO", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(situacao, "PRONTO_PARA_EXPEDICAO", StringComparison.OrdinalIgnoreCase);

    public static void ValidarQuantidadeSeparacao(decimal reservada, decimal separada)
    {
        if (separada < 0)
            throw new ArgumentOutOfRangeException(nameof(separada), "Quantidade separada não pode ser negativa.");
        if (separada > reservada)
            throw new InvalidOperationException($"Quantidade separada ({separada}) não pode exceder a quantidade reservada ({reservada}).");
    }

    public static decimal CalcularPendenteCustodia(decimal expedida, decimal consumida, decimal devolvida, decimal perda)
    {
        if (expedida < 0) throw new ArgumentOutOfRangeException(nameof(expedida), "Quantidade expedida não pode ser negativa.");
        if (consumida < 0) throw new ArgumentOutOfRangeException(nameof(consumida), "Quantidade consumida não pode ser negativa.");
        if (devolvida < 0) throw new ArgumentOutOfRangeException(nameof(devolvida), "Quantidade devolvida não pode ser negativa.");
        if (perda < 0) throw new ArgumentOutOfRangeException(nameof(perda), "Quantidade de perda não pode ser negativa.");

        var totalAtendido = consumida + devolvida + perda;
        if (totalAtendido > expedida)
            throw new InvalidOperationException($"Soma dos eventos ({totalAtendido}) excede a quantidade expedida ({expedida}).");

        return expedida - totalAtendido;
    }

    public static void ValidarEvento(decimal pendenteCustodia, decimal quantidadeEvento, string tipoEvento)
    {
        if (quantidadeEvento <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantidadeEvento), $"Quantidade do evento {tipoEvento} deve ser positiva.");
        if (quantidadeEvento > pendenteCustodia)
            throw new InvalidOperationException($"Quantidade do evento {tipoEvento} ({quantidadeEvento}) excede o saldo pendente em custódia ({pendenteCustodia}).");
    }

    public static bool ValidarReconciliacao(IEnumerable<(decimal Expedida, decimal Consumida, decimal Devolvida, decimal Perda)> itens)
    {
        foreach (var item in itens)
        {
            var pendente = CalcularPendenteCustodia(item.Expedida, item.Consumida, item.Devolvida, item.Perda);
            if (pendente > 0) return false;
        }
        return true;
    }
}

public static class CirurgiaRegras
{
    public static void ValidarCriacao(string procedimento, DateOnly dataPrevista)
    {
        if (string.IsNullOrWhiteSpace(procedimento))
            throw new ArgumentException("Descrição do procedimento cirúrgico é obrigatória.");
    }

    public static bool PodeAlterar(string situacao) =>
        ConsignacaoCirurgicaRegras.PodeAlterarCirurgia(situacao);

    public static bool PodeCancelar(string situacao, bool temExpedicao) =>
        ConsignacaoCirurgicaRegras.PodeCancelarCirurgia(situacao, temExpedicao);
}

public static class ValeConsignacaoRegras
{
    public static bool PodeExpedir(string situacao) =>
        ConsignacaoCirurgicaRegras.PodeExpedirVale(situacao);

    public static decimal CalcularPendenteCustodia(decimal expedida, decimal consumida, decimal devolvida, decimal perda) =>
        ConsignacaoCirurgicaRegras.CalcularPendenteCustodia(expedida, consumida, devolvida, perda);

    public static void ValidarEvento(decimal pendenteCustodia, decimal quantidadeEvento, string tipoEvento) =>
        ConsignacaoCirurgicaRegras.ValidarEvento(pendenteCustodia, quantidadeEvento, tipoEvento);

    public static bool ValidarReconciliacao(IEnumerable<(decimal Expedida, decimal Consumida, decimal Devolvida, decimal Perda)> itens) =>
        ConsignacaoCirurgicaRegras.ValidarReconciliacao(itens);
}

