namespace PlantaoPro.Api;

/// <summary>
/// R8-B9 (§27 — estender padrão Operacional / contrato de definição). Unidade canônica de
/// definição de um indicador de dashboard/KPI. Estende o padrão já usado por
/// <see cref="OperationalReportIndicator"/> (campo <c>Definition</c> por indicador +
/// <c>TimeCriterion</c> no resultado) aos demais calculadores sobrepostos listados em
/// r8a0 §29 item 1 (<see cref="BiService"/>, <see cref="DashboardPremiumService"/>,
/// <see cref="Data.DashboardService"/>, <see cref="Fase6BiIntegracoesService"/>, v114/operacional).
/// </summary>
/// <param name="Codigo">Chave estável do indicador (nome da propriedade / alias do SELECT).</param>
/// <param name="Label">Rótulo legível (pt-BR) para exibição.</param>
/// <param name="Definicao">Fórmula/fonte/período/fuso/filtros efetivamente aplicados pelo serviço
/// (texto fiel ao SQL real; nada inventado). Indicadores de período marcam "(período)";
/// de estado marcam "(estado atual)".</param>
public sealed record KpiDefinicao(string Codigo, string Label, string Definicao);
