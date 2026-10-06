using System.Globalization;
using PlantaoPro.CrossCutting.Localization;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// WS-A2 (item 2 - gate financeiro): matriz de regras do contrato central de leitura de
/// valores humanos (ValorHumano). Cobrem inclusao, edicao e qualquer ponto que parseia
/// texto digitado: "12,50" e 12.50 (NUNCA 1250), "1.234,56" e 1234.56, zero, limites,
/// casas decimais, invalidos e separadores ambiguos (rejeitados, nunca chutados).
/// Observacao: esperados em texto canonico invariante ("12.50") porque literal decimal com
/// sufixo m nao e aceito como argumento de atributo InlineData (limitacao do compilador C#).
/// </summary>
public sealed class ValorHumanoTests
{
    private static decimal Esperado(string canonico)
        => decimal.Parse(canonico, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData("12,50", "12.50")]   // reprodutor M2.5: antes virava 1250
    [InlineData("12,5", "12.5")]
    [InlineData(",50", "0.5")]
    [InlineData("1.234,56", "1234.56")]
    [InlineData("1.234,5", "1234.5")]
    [InlineData("1.234.567,89", "1234567.89")]
    [InlineData("1,234.56", "1234.56")]
    [InlineData("12.5", "12.5")]     // formato invariante vindo de JS/browsers
    [InlineData("12.50", "12.5")]
    [InlineData("1234.56", "1234.56")]
    [InlineData("1250", "1250")]
    [InlineData("0", "0")]
    [InlineData("0,00", "0")]
    [InlineData("-3,14", "-3.14")]
    [InlineData("-3.14", "-3.14")]
    [InlineData("+5", "5")]
    [InlineData("R$ 1.234,56", "1234.56")]
    [InlineData("$ 12.50", "12.5")]
    [InlineData("1 234,56", "1234.56")]
    [InlineData("12,5000", "12.5")]      // ate 4 casas (precisao numeric(18,4))
    [InlineData("1250.0000", "1250")]
    [InlineData("999999999999.9999", "999999999999.9999")]   // teto do schema
    [InlineData("-999999999999.9999", "-999999999999.9999")]
    [InlineData("1.234.567", "1234567")] // milhares sem centavos
    public void ValoresHumanos_Validos_ConvertemSemSilenciar(string entrada, string esperadoCanonico)
    {
        bool ok = ValorHumano.TentarConverter(entrada, out decimal valor, out string erro);

        Assert.True(ok, $"'{entrada}' deveria ser valido. Erro: {erro}");
        Assert.Equal(Esperado(esperadoCanonico), valor);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("12,")]           // incompleto: nao pode virar 12 silencioso
    [InlineData("12.")]
    [InlineData(".")]
    [InlineData(",")]
    [InlineData("-")]
    [InlineData("+")]
    [InlineData("--5")]
    [InlineData("5-")]
    [InlineData("$")]
    [InlineData("1e5")]
    [InlineData("12a,50")]
    [InlineData("1.234")]         // ambiguo: milhar? centavos?
    [InlineData("1,234")]
    [InlineData("12,345")]
    [InlineData("12.345")]
    [InlineData("1,23,4")]
    [InlineData("1.2.3")]
    [InlineData("1.234.56")]
    [InlineData("12,50,78")]
    [InlineData("12,50000")]      // mais de 4 casas
    [InlineData("1000000000000")] // excede teto numeric(18,4)
    [InlineData("-1000000000000")]
    public void ValoresHumanos_Invalidos_NaoPassamSilenciosos(string? entrada)
    {
        // entrada pode ser null de proposito (caso coberto); o contrato aceita texto nulo.
        bool ok = ValorHumano.TentarConverter(entrada!, out decimal _, out string erro);

        Assert.False(ok, $"'{entrada}' deveria ser rejeitado.");
        Assert.False(string.IsNullOrWhiteSpace(erro));
        // mensagem humana, sem vazar termos tecnicos de binding
        Assert.DoesNotContain("Parameter", erro, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Ambiguo_ApresentaMotivoComExemplo()
    {
        bool ok = ValorHumano.TentarConverter("1.234", out _, out string erro);

        Assert.False(ok);
        Assert.Contains("ambiguo", erro, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1.234,00", erro, StringComparison.Ordinal);
    }

    [Fact]
    public void Limite_ExcedeTetoDoSchema_RejeitaComMensagem()
    {
        bool ok = ValorHumano.TentarConverter("1.000.000.000.000", out _, out string erro);

        Assert.False(ok);
        Assert.Contains("limite", erro, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("1234.56", "1.234,56")]
    [InlineData("12.5", "12,50")]
    [InlineData("0", "0,00")]
    [InlineData("-3.14", "-3,14")]
    public void Formato_UnicoPtBr_SoParaExibicao(string valorCanonico, string esperado)
        => Assert.Equal(esperado, ValorHumano.Format(Esperado(valorCanonico)));
}
