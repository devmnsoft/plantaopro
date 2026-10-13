using System.Text.RegularExpressions;
using PlantaoPro.Domain.Administrativo360;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B14 — §29 item 6: "Comissão calculada/apropriada em 4 locais (baixa, geração AP,
/// relatórios, UI) — validar canonicidade da fórmula."
///
/// RESULTADO DA VALIDAÇÃO (fonte canônica, sem regra inventada):
///   • A fórmula é JÁ canônica e única: <c>ComissaoRegras.CalcularComissaoApropriada</c>
///     (Domain <c>FinanceiroVendas.cs</c>) — técnica de "total acumulado − já apropriado, arredondado
///     a 2 casas e piso em 0", que evita drift de centavos entre parcelas (coberto por
///     <c>BlocoD_ComissaoRegras_CalculoProporcionalEvitaDiferencaCentavos</c>). Há também
///     <c>CalcularEstornoComissao</c>.
///   • OS 4 LOCAIS NÃO DIVERGEM ENTRE SI:
///       (a) baixa <c>ContasReceberRepository.cs</c> = ÚNICO ESCRITOR de <c>adm360_comissoes_apropriadas</c>,
///           e DELEGA a <c>ComissaoRegras.CalcularComissaoApropriada</c>;
///       (b) geração AP <c>GerarDeComissaoAsync</c> (<c>ContasPagarRepository.cs</c>) = SOMENTE agrega
///           <c>SUM(valor_comissao)</c> já escrito por (a) — não re-aplica taxa × base;
///       (c) relatórios <c>Adm360FinanceiroRelatoriosRepository.cs</c> = LÊ coluna gravada
///           (<c>v.comissao_prevista</c>) ou agrega (<c>SUM(com.valor_comissao)</c>) — não recalcula;
///       (d) UI <c>TituloDetalhes.cshtml</c> = texto explicativo ("apropria proporcionalmente a comissão").
///   • Dois CONCEITOS distintos (não duplicados): <c>comissao_prevista</c> (planejada — derivada na
///     valorização e COPIADA para a venda) × <c>valor_comissao</c> (realizada proporcional — escrita na baixa).
///   • ESTORNO consistente: flip <c>situacao='ESTORNADA'</c> + acúmulo filtra <c>situacao='APROPRIADA'</c>.
///
/// CONCLUSÃO: nada a migrar (a fórmula já está single-source no Domain + testada). Esta fatia ENTREGA a
/// validação + um CONTRATO anti-drift que fixa o invariante "um único escritor + consumidores só
/// agregam/lêem", impedindo que no futuro alguém re-introduza uma segunda fórmula de taxa × base.
/// </summary>
public sealed class R8B14ComissaoCanonicaContratoTests
{
    private static readonly Regex InsertRx = new(
        @"INSERT\s+INTO\s+(?:plantaopro\.)?adm360_comissoes_apropriadas", RegexOptions.IgnoreCase);

    private static string Infra(string file) =>
        Path.Combine(RepositoryPathResolver.BackendRoot, "PlantaoPro.Infrastructure", "Administrativo360", file);

    [Fact]
    public void EscritaUnica_ComissoesApropriadas_SoAPassaBaixaDelegandoARegraCanonical()
    {
        var candidatos = RepositoryPathResolver
            .EnumerateRepositoryFiles(new[] { RepositoryPathResolver.BackendRoot }, "*.cs")
            .Concat(RepositoryPathResolver.EnumerateRepositoryFiles(new[] { RepositoryPathResolver.DatabaseRoot }, "*.sql"))
            .Where(p => p.Replace('\\', '/').IndexOf("/PlantaoPro.Tests/", StringComparison.OrdinalIgnoreCase) < 0)
            .Where(path => InsertRx.IsMatch(File.ReadAllText(path)))
            .ToArray();

        // Exatamente UM ponto escreve adm360_comissoes_apropriadas (produto + SQL) — possível 2ª fórmula se > 1.
        Assert.True(candidatos.Length == 1,
            $"Há {candidatos.Length} ponto(s) escrevendo adm360_comissoes_apropriadas — esperado exatamente 1 (possível 2ª fórmula de comissão).");
        Assert.EndsWith("ContasReceberRepository.cs", candidatos[0].Replace('\\', '/'));

        // E o escritor delega à função canônica do Domain (não aplica taxa × base inline).
        Assert.Contains("ComissaoRegras.CalcularComissaoApropriada(", File.ReadAllText(candidatos[0]));
    }

    [Fact]
    public void Consumidores_GeracaoAPERelatorios_AgregamELeemSemRecalcularTaxa()
    {
        // (b) Geração AP: extrai o método e prova que SOMENTE soma valor_comissao (nunca toca em taxa).
        var pagar = File.ReadAllText(Infra("ContasPagarRepository.cs"));
        var metodo = Regex.Match(pagar, @"GerarDeComissaoAsync\b[\s\S]*?\r?\n    \}");
        Assert.True(metodo.Success, "Método GerarDeComissaoAsync não encontrado.");
        Assert.Contains("comissoes.Sum(c => (decimal)c.valor_comissao)", metodo.Value);
        Assert.DoesNotContain("percentual", metodo.Value, StringComparison.OrdinalIgnoreCase);

        // (c) Relatórios: lê a coluna planejada e agrega a realizada (sem dividir por 100 / sem taxa × base).
        var rel = File.ReadAllText(Infra("Adm360FinanceiroRelatoriosRepository.cs"));
        Assert.Contains("v.comissao_prevista AS ComissaoPrevista", rel);
        Assert.Contains("SUM(com.valor_comissao)", rel);
    }

    [Fact]
    public void Estorno_FlipDaSituacao_SaiDoAcumuloDaComissaoRealizada()
    {
        var receber = File.ReadAllText(Infra("ContasReceberRepository.cs"));
        // O estorno marca a linha de apropriação como ESTORNADA…
        Assert.Contains("SET situacao = 'ESTORNADA'", receber);
        // …e o acúmulo usado pela próxima apropriação filtra apenas APROPRIADA (estornadas não contam).
        Assert.Contains("situacao = 'APROPRIADA'", receber);
    }

    [Fact]
    public void FormulaCanonical_PontosConhecidosIdentidadeTotalEPisoEEstorno()
    {
        // Pagamento único do total → comissão exata (identidade total × taxa).
        Assert.Equal(5.00m, ComissaoRegras.CalcularComissaoApropriada(0m, 100.00m, 5.0m, 0m));

        // Piso em 0: base já totalmente apropriada → centavo extra não gera comissão negativa.
        Assert.Equal(0.00m, ComissaoRegras.CalcularComissaoApropriada(100.00m, 0.01m, 5.0m, 5.00m));

        // Estorno: mesmo arredondamento da regra (taxa × valor estornado, 2 casas).
        Assert.Equal(1.67m, ComissaoRegras.CalcularEstornoComissao(33.34m, 5.0m));
        Assert.Equal(5.00m, ComissaoRegras.CalcularEstornoComissao(100.00m, 5.0m));

        // Guardas de domínio.
        Assert.Throws<ArgumentOutOfRangeException>(() => ComissaoRegras.CalcularComissaoApropriada(0m, 10m, 150m, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => ComissaoRegras.CalcularComissaoApropriada(0m, 0m, 5m, 0m));
    }
}
