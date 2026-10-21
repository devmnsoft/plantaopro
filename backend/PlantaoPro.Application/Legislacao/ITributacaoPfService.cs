using System.Threading;
using System.Threading.Tasks;

namespace PlantaoPro.Application.Legislacao;

/// <summary>
/// R81 (bloco B4): faixa de tributo na fonte unica. LimiteSuperior vazio = faixa
/// final sem teto. Calculo por "parcela a deduzir": aliquota x base - parcela.
/// </summary>
public sealed record FaixaTributoLegalDto(string Faixa, string LimiteSuperior, decimal AliquotaPercentual, decimal ParcelaADeduzir);

/// <summary>Tabela vigente de tributacao PF de um ano (tudo lido da fonte unica).</summary>
public sealed record TabelaTributacaoPfDto(
    int Ano,
    decimal SalarioMinimo,
    decimal PisoIrrfMensal,
    decimal TetoInss,
    IReadOnlyList<FaixaTributoLegalDto> FaixasInss,
    IReadOnlyList<FaixaTributoLegalDto> FaixasIrrf);

/// <summary>
/// Resultado do calculo PF mensal bruto -&gt; descontos -&gt; liquido. INSS progressivo
/// (base limitada ao teto) sobre o bruto; IRRF progressivo mensal (parcela a deduzir)
/// sobre (bruto - INSS - deducoes), isento ate o piso da fonte unica. Arredondamento 2 casas.
/// </summary>
public sealed record CalculoTributacaoPfDto(
    int Ano,
    decimal SalarioBruto,
    decimal SalarioMinimo,
    decimal TetoInss,
    decimal BaseInss,
    decimal Inss,
    decimal DescontosDependentes,
    decimal DeducaoSimplificada,
    decimal BaseIrrf,
    decimal PisoIrrfMensal,
    decimal Irrf,
    decimal TotalDescontos,
    decimal SalarioLiquido);

public sealed record CalcularTributacaoPfCommand(decimal SalarioBruto, int? Ano);

/// <summary>
/// R81: nucleo de tributacao pessoa fisica com vetores oficiais vindos exclusivamente
/// da fonte unica parametros_legais (migracoes versionadas com fonte citavel).
/// R82: deducoes do contribuinte (dependentes ou simplificada) entram na base do IRRF.
/// </summary>
public interface ITributacaoPfService
{
    Task<TabelaTributacaoPfDto> ObterTabelaAsync(int? ano, CancellationToken ct = default);

    Task<CalculoTributacaoPfDto> CalcularAsync(int? ano, decimal salarioBruto, decimal descontosDependentes = 0m, decimal deducaoSimplificada = 0m, CancellationToken ct = default);
}
