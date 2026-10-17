namespace PlantaoPro.Domain.Administrativo360;

// A12 — Não conformidades/ocorrências (spec Administrativo 360):
// "Aberta → em tratamento → resolvida → encerrada. Encerrar exige destino dos itens e
// justificativa. Registro de ocorrência não apaga estoque."
public static class OcorrenciasA12
{
    public const string Aberta = "ABERTA";
    public const string EmTratamento = "EM_TRATAMENTO";
    public const string Resolvida = "RESOLVIDA";
    public const string Encerrada = "ENCERRADA";

    // Efeito físico decidido no ENCERRAR (rodada 15 r15b1): o destino escolhe o que o saldo
    // bloqueado vira. DEVOLUCAO devolve à circulação (comportamento histórico); BAIXA consome
    // fisicamente (descarte/consumo — única saída em que a ocorrência apaga estoque, e só por
    // decisão explícita da operadora: o registro em si nunca apaga).
    public const string EfeitoDevolucao = "DEVOLUCAO";
    public const string EfeitoBaixa = "BAIXA";

    // Progressão estrita: cada comando avança exatamente um passo; sem saltos nem retorno.
    public static bool PodeTransicionar(string atual, string destino) =>
        (atual == Aberta && destino == EmTratamento) ||
        (atual == EmTratamento && destino == Resolvida) ||
        (atual == Resolvida && destino == Encerrada);

    public static void ValidarCriacao(string tipo, string descricao, decimal quantidade)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            throw new Administrativo360BusinessException("Informe o tipo do desvio.");
        if (tipo.Length > 40)
            throw new Administrativo360BusinessException("Tipo do desvio excede 40 caracteres.");
        if (string.IsNullOrWhiteSpace(descricao))
            throw new Administrativo360BusinessException("Informe a descrição da ocorrência.");
        if (quantidade <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantidade));
    }

    // A12: "Encerrar exige destino dos itens e justificativa." Rodada 15: encerrar também
    // decide o efeito físico do saldo bloqueado — campo canônico, nunca inferência de texto.
    public static void ValidarEncerramento(string? destino, string? justificativa, string? efeitoFisico = null)
    {
        if (string.IsNullOrWhiteSpace(destino))
            throw new Administrativo360BusinessException("Encerrar exige o destino dos itens.");
        if (string.IsNullOrWhiteSpace(justificativa))
            throw new Administrativo360BusinessException("Encerrar exige justificativa.");
        NormalizarEfeitoFisico(efeitoFisico);
    }

    // Campo canônico do efeito físico (rodada 15 D2): vazio/nulo ⇒ DEVOLUCAO — encerramentos
    // anteriores à rodada (e payloads sem o campo) mantêm o comportamento histórico de
    // devolução. Qualquer outro valor fora do par canônico rejeita antes de qualquer efeito.
    public static string NormalizarEfeitoFisico(string? efeitoFisico)
    {
        var e = (efeitoFisico ?? string.Empty).Trim().ToUpperInvariant();
        if (e.Length == 0)
            return EfeitoDevolucao;
        if (e == EfeitoDevolucao || e == EfeitoBaixa)
            return e;
        throw new Administrativo360BusinessException("Efeito físico inválido ao encerrar: use DEVOLUCAO ou BAIXA.");
    }
}
