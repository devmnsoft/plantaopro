namespace PlantaoPro.Domain.Financeiro;

/// <summary>
/// Regras determinísticas compartilhadas pela API e pelos futuros conectores de
/// faturamento. Não contém dados clínicos nem depende de um provedor externo.
/// </summary>
public static class ClinicalCommercialRules
{
    public static bool PodeReceber(decimal valor, DateTime? data, string? formaPagamento, bool jaRecebida, bool caixaFechado)
    {
        return valor > 0 && data.HasValue && !string.IsNullOrWhiteSpace(formaPagamento) && !jaRecebida && !caixaFechado;
    }

    public static bool DescontoValido(decimal valorBruto, decimal desconto, decimal limitePermitido)
    {
        return valorBruto >= 0 && desconto >= 0 && limitePermitido >= 0 && desconto <= valorBruto && desconto <= limitePermitido;
    }

    public static bool JustificativaValida(string? justificativa)
    {
        return !string.IsNullOrWhiteSpace(justificativa) && justificativa.Trim().Length >= 5;
    }

    public static bool PodeAutorizarConvenio(string? statusConvenio, DateOnly fimContrato, DateOnly hoje)
    {
        return string.Equals(statusConvenio, "ATIVO", StringComparison.OrdinalIgnoreCase) && fimContrato >= hoje;
    }

    public static bool PodeFaturarConvenio(string? statusAutorizacao)
    {
        // Fail closed: uma autorização ausente, desconhecida ou ainda pendente
        // jamais deve liberar faturamento. Novos estados precisam ser incluídos
        // deliberadamente nesta regra, em vez de serem aceitos por exclusão.
        return string.Equals(statusAutorizacao?.Trim(), "APROVADA", StringComparison.OrdinalIgnoreCase);
    }

    public static bool PlanoPodeSerUsado(string? statusPlano, DateOnly? validade, DateOnly hoje)
    {
        return string.Equals(statusPlano, "ATIVO", StringComparison.OrdinalIgnoreCase)
            && (!validade.HasValue || validade.Value >= hoje);
    }

    public static bool PodeGerarRepasse(string? statusAtendimento, bool origemConvenio, bool recebido, bool faturado, bool jaExiste)
    {
        if (jaExiste || !string.Equals(statusAtendimento, "FINALIZADA", StringComparison.OrdinalIgnoreCase)) return false;
        return origemConvenio ? recebido || faturado : recebido;
    }

    public static bool PodeVisualizarRepasse(bool administradorGlobal, bool financeiroTenant, Guid usuarioMedicoId, Guid medicoId)
    {
        return administradorGlobal || financeiroTenant || (usuarioMedicoId != Guid.Empty && usuarioMedicoId == medicoId);
    }
}
