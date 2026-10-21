using System.Globalization;
using Dapper;
using Npgsql;
using PlantaoPro.Application.Legislacao;

namespace PlantaoPro.Infrastructure.Legislacao;

/// <summary>
/// R81 (bloco B4): nucleo de tributacao pessoa fisica. Todos os vetores oficiais
/// (minimo, teto, faixas INSS/IRRF, piso) vem exclusivamente da fonte unica
/// parametros_legais (migracoes versionadas com fonte citavel) - nenhum numero legal
/// vive no codigo. Metodos oficiais: INSS progressivo por "parcela a deduzir" sobre
/// a base limitada ao teto; IRRF mensal progressivo sobre (bruto - INSS), isento ate
/// o piso da fonte unica. Arredondamento comercial (2 casas, meio para fora).
/// </summary>
public sealed class TributacaoPfService : ITributacaoPfService
{
    private static readonly NumberFormatInfo Decimais = CultureInfo.InvariantCulture.NumberFormat;

    private readonly IParametrosLegaisRepository parametros;

    public TributacaoPfService(IParametrosLegaisRepository parametros) => this.parametros = parametros;

    public async Task<TabelaTributacaoPfDto> ObterTabelaAsync(int? ano, CancellationToken ct = default)
    {
        var linhas = await parametros.ListarAtivosAsync(ano, ct);
        var anoEfetivo = ResolveAno(linhas, ano);

        decimal Minimo() => ValorObrigatorio(linhas, "SALARIO_MINIMO");
        decimal Piso() => ValorObrigatorio(linhas, "PISO_ISENCAO_IRRF_MENSAL");
        decimal Teto() => ValorObrigatorio(linhas, "INSS_TETO_CONTRIBUICAO");

        var faixasInss = Faixas(linhas, "INSS_FAIXA_");
        var faixasIrrf = Faixas(linhas, "IRRF_FAIXA_");
        if (faixasInss.Count == 0) throw new InvalidOperationException($"Vetores INSS do ano {anoEfetivo} ausentes da fonte unica.");
        if (faixasIrrf.Count == 0) throw new InvalidOperationException($"Vetores IRRF do ano {anoEfetivo} ausentes da fonte unica.");

        return new TabelaTributacaoPfDto(anoEfetivo, Minimo(), Piso(), Teto(), faixasInss, faixasIrrf);
    }

    public async Task<CalculoTributacaoPfDto> CalcularAsync(int? ano, decimal salarioBruto, CancellationToken ct = default)
    {
        // O desserializador canonico nao produz NaN; a validacao numerica basta aqui.
        if (salarioBruto <= 0m)
            throw new ArgumentException("Informe um salario bruto maior que zero.", nameof(salarioBruto));

        var tabela = await ObterTabelaAsync(ano, ct);

        // INSS progressivo: base limitada ao teto; faixa pela base; aliquota x base - parcela.
        var baseInss = decimal.Min(salarioBruto, tabela.TetoInss);
        var inss = AplicarFaixa(tabela.FaixasInss, baseInss, clampNoLimiteFinal: true);

        // IRRF mensal: base = bruto - INSS; isento ate o piso da fonte unica.
        var baseIrrf = salarioBruto - inss;
        var irrf = baseIrrf <= tabela.PisoIrrfMensal ? 0m : AplicarFaixa(tabela.FaixasIrrf, baseIrrf, clampNoLimiteFinal: false);

        var total = Redondo(inss + irrf);
        return new CalculoTributacaoPfDto(
            tabela.Ano, salarioBruto, tabela.SalarioMinimo, tabela.TetoInss,
            Redondo(baseInss), inss, Redondo(baseIrrf), tabela.PisoIrrfMensal, irrf, total,
            Redondo(salarioBruto - total));
    }

    private static int ResolveAno(List<ParametroLegalDto> linhas, int? pedido)
    {
        if (pedido is not null)
        {
            if (!linhas.Exists(l => l.Ano == pedido))
                throw new InvalidOperationException($"Nenhum parametro legal ativo para o ano {pedido} na fonte unica.");
            return pedido.Value;
        }
        // Sem ano pedido: usa o maior ano disponivel (fonte unica decide o vigente).
        return linhas.Select(l => l.Ano).Max();
    }

    private static decimal ValorObrigatorio(List<ParametroLegalDto> linhas, string codigo)
    {
        var linha = linhas.FirstOrDefault(l => string.Equals(l.Codigo, codigo, StringComparison.OrdinalIgnoreCase));
        if (linha is null) throw new InvalidOperationException($"Parametro legal {codigo} ausente da fonte unica.");
        return decimal.Parse(linha.Valor, Decimais);
    }

    private static List<FaixaTributoLegalDto> Faixas(List<ParametroLegalDto> linhas, string prefixoCodigo)
    {
        return linhas
            .Where(l => l.Codigo.StartsWith(prefixoCodigo, StringComparison.OrdinalIgnoreCase))
            .OrderBy(l => l.Codigo, StringComparer.OrdinalIgnoreCase)
            .Select(l =>
            {
                var partes = l.Valor.Split('|');
                if (partes.Length != 3)
                    throw new InvalidOperationException($"Faixa {l.Codigo} malformada na fonte unica: esperado \"limite|aliquota|parcela\".");
                return new FaixaTributoLegalDto(
                    l.Codigo,
                    partes[0].Trim(),
                    decimal.Parse(partes[1], Decimais),
                    decimal.Parse(partes[2], Decimais));
            })
            .ToList();
    }

    /// <summary>
    /// Alíquota x base - parcela, primeira faixa cujo limite cubra a base; limite vazio
    /// = faixa final sem teto. No INSS (clamp) a base ja esta limitada ao teto, portanto
    /// a faixa final cobre; no IRRF a faixa final e ilimitada por definicao.
    /// </summary>
    private static decimal AplicarFaixa(IReadOnlyList<FaixaTributoLegalDto> faixas, decimal baseCalculo, bool clampNoLimiteFinal)
    {
        foreach (var faixa in faixas)
        {
            var temTeto = !string.IsNullOrEmpty(faixa.LimiteSuperior);
            if (!temTeto || baseCalculo <= decimal.Parse(faixa.LimiteSuperior, Decimais))
            {
                var valor = Redondo(faixa.AliquotaPercentual / 100m * baseCalculo - faixa.ParcelaADeduzir);
                return valor > 0m ? valor : 0m;
            }
        }
        if (clampNoLimiteFinal)
        {
            var ultima = faixas[^1];
            var limite = decimal.Parse(ultima.LimiteSuperior, Decimais);
            var valor = Redondo(ultima.AliquotaPercentual / 100m * limite - ultima.ParcelaADeduzir);
            return valor > 0m ? valor : 0m;
        }
        throw new InvalidOperationException("Base acima de todas as faixas com teto definido - revisao da fonte unica.");
    }

    private static decimal Redondo(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
