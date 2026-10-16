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

    // A12: "Encerrar exige destino dos itens e justificativa."
    public static void ValidarEncerramento(string? destino, string? justificativa)
    {
        if (string.IsNullOrWhiteSpace(destino))
            throw new Administrativo360BusinessException("Encerrar exige o destino dos itens.");
        if (string.IsNullOrWhiteSpace(justificativa))
            throw new Administrativo360BusinessException("Encerrar exige justificativa.");
    }
}
