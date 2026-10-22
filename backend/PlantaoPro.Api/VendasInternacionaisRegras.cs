using System;
using System.Linq;

namespace PlantaoPro.Api;

/// <summary>
/// R89 (venda para fora do país): regras puras do faturamento internacional
/// simplificado — conversão de BRL (moeda base) para a moeda de destino pela
/// cotação institucional, VAT do país de destino aplicado ACRESCIDO sobre a base
/// convertida e validação das moedas aceitas. Sem IO. Constantes espelham a fonte
/// única parametros_legais (COTACAO_MOEDAS, COTACAO_MOEDA_BASE,
/// NOTA_INTERNACIONAL_FORMA); o serviço lê os parâmetros e passa os valores reais
/// aqui como argumentos. Fontes: BRL é a moeda base do faturamento SaaS
/// (faturas_saas); alíquotas padrão de VAT do destino vêm do catálogo institucional
/// (paises_vat, com fonte citável por linha); no lado brasileiro a venda ao
/// exterior enquadra-se nas dispensas clássicas de exportação — ICMS
/// (Lei 8.080/1990, art. 39, II) e IPI (Lei 4.502/1964, art. 15, III, c/c Decreto
/// 4.404/2002, arts. 19-23).
/// </summary>
public static class VendasInternacionaisRegras
{
    public const string MoedaBase = "BRL";
    public const string MoedasFallback = "USD|EUR|GBP";
    public const string FormaDocumento = "NAO_TRADICIONAL";
    public static readonly DateOnly DataMinimaCotacao = new(2020, 1, 1);
    public const decimal TaxaMaxSanidade = 1000000m;

    /// <summary>Conversão de BRL para moeda externa: valor/taxa, arredondado monetário em 2 casas (metade afasta de zero).</summary>
    public static decimal ConvertParaMoeda(decimal valorBrl, decimal taxaBrlPorUnidade) =>
        Math.Round(valorBrl / taxaBrlPorUnidade, 2, MidpointRounding.AwayFromZero);

    /// <summary>VAT do destino acrescido sobre a base convertida: round(valor * aliquota / 100, 2) (metade afasta de zero).</summary>
    public static decimal ValorVat(decimal valorMoeda, decimal aliquotaPct) =>
        Math.Round(valorMoeda * aliquotaPct / 100m, 2, MidpointRounding.AwayFromZero);

    public sealed record NotaInternacionalCalculo(
        decimal ValorBrl, decimal TaxaAplicada, decimal ValorMoeda, decimal AliquotaPct, decimal ValorVat, decimal TotalMoeda);

    /// <summary>
    /// Nota fiscal não-tradicional simplificada: base em BRL (moeda base),
    /// conversão pela cotação (BRL por unidade), VAT padrão do país de destino
    /// acrescido sobre a base convertida; total_moeda = valor_moeda + valor_vat.
    /// </summary>
    public static NotaInternacionalCalculo CalcularNota(decimal valorBrl, decimal taxaBrlPorUnidade, decimal aliquotaPct)
    {
        if (valorBrl < 0m) throw new ArgumentException("Valor em BRL nao pode ser negativo.", nameof(valorBrl));
        if (taxaBrlPorUnidade <= 0m) throw new ArgumentException("Taxa de cambio deve ser positiva.", nameof(taxaBrlPorUnidade));
        if (aliquotaPct < 0m || aliquotaPct > 100m) throw new ArgumentException("Aliquota de VAT deve estar entre 0 e 100.", nameof(aliquotaPct));
        var valorMoeda = ConvertParaMoeda(valorBrl, taxaBrlPorUnidade);
        var valorVat = ValorVat(valorMoeda, aliquotaPct);
        return new NotaInternacionalCalculo(valorBrl, taxaBrlPorUnidade, valorMoeda, aliquotaPct, valorVat,
            Math.Round(valorMoeda + valorVat, 2, MidpointRounding.AwayFromZero));
    }

    /// <summary>Moeda aceita no faturamento internacional (ISO 4217, sem distinção de caixa, contra o catálogo parametrizado).</summary>
    public static bool MoedaAceita(string? moeda, string catalogoMoedas)
    {
        var m = (moeda ?? string.Empty).Trim().ToUpperInvariant();
        if (m.Length != 3 || !m.All(char.IsLetter)) return false;
        return catalogoMoedas.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(x => x.Equals(m, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Código ISO válido com o tamanho esperado (letras maiúsculas, ex.: ISO 3166 alpha-2 ou 4217 alpha-3).</summary>
    public static bool FormatoIso(string? codigo, int tamanho)
    {
        var c = (codigo ?? string.Empty).Trim().ToUpperInvariant();
        return c.Length == tamanho && c.All(x => x >= 'A' && x <= 'Z');
    }
}
