namespace PlantaoPro.Domain.Ocorrencias;

public static class OcorrenciaWorkflow
{
    public static readonly string[] Categorias = { "ATRASO", "AUSENCIA", "ACESSO", "INDISPONIBILIDADE", "COBERTURA", "DIVERGENCIA_REGISTRO", "OUTRA" };
    public static readonly string[] Prioridades = { "BAIXA", "MEDIA", "ALTA", "CRITICA" };
    public static readonly string[] Situacoes = { "ABERTA", "EM_ATENDIMENTO", "AGUARDANDO_INFORMACAO", "RESOLVIDA", "CANCELADA" };

    public static bool PodeTransicionar(string atual, string destino, bool gestor) => (atual, destino, gestor) switch
    {
        ("ABERTA", "EM_ATENDIMENTO", true) => true,
        ("EM_ATENDIMENTO", "AGUARDANDO_INFORMACAO", _) => true,
        ("AGUARDANDO_INFORMACAO", "EM_ATENDIMENTO", _) => true,
        ("EM_ATENDIMENTO", "RESOLVIDA", _) => true,
        ("ABERTA" or "EM_ATENDIMENTO" or "AGUARDANDO_INFORMACAO", "CANCELADA", true) => true,
        ("RESOLVIDA" or "CANCELADA", "ABERTA", true) => true,
        _ => false
    };
}
