namespace PlantaoPro.Web.Models;

/// <summary>
/// R100 (notificações pautadas no calendário fiscal): visão web de um alerta de obrigação do
/// período exibido (BFF da rota api/calendario-fiscal/alertas). Níveis: CRITICO/ALTA/MEDIA;
/// Escalonada marca que o atraso passou do limiar e o alerta subiu para o dono.
/// </summary>
public sealed record AlertaObrigacaoFiscalWebDto(string Codigo, string Nome, string DataVencimento, int DiasRestantes, string Estado, string SlaNivel, bool Escalonada);

/// <summary>Modelo do cartão "SLA e escalonamento" do calendário fiscal (melhor esforço: vazio quando a leitura falha).</summary>
public sealed class CalendarioFiscalWebViewModel
{
    public int MesExibicao { get; init; }

    public int AnoExibicao { get; init; }

    public IReadOnlyList<AlertaObrigacaoFiscalWebDto> Alertas { get; set; } = Array.Empty<AlertaObrigacaoFiscalWebDto>();
}
