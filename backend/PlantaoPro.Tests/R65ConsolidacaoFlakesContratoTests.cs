using System.IO;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R65 (fila r53b1 §7 item 10): consolidação do catálogo de flakes. As duas
/// famílias com assinatura observada em múltiplas rodadas recebem endurecimento
/// de infraestrutura (não do produto) e ganham contrato contra regressão:
///   ① 40P01/40001 em PurgarAsync/Dispose com coleções concorrentes sob carga
///      (catálogo desde r8b10: ProvisionamentoB5 P1/P3, R7OB O1-O5, SaasGovernancaB6
///      G3/G4b/G5, B5 P2) → todas as purgas rodam pelo retry único compartilhado
///      TestInfraRetry.ExecutarComRetryImpasseAsync (origem: helper privado r9m-P2).
///   ② corrida de snapshot em R19 Db_CentralClientes (itens e totais são duas
///      statements separadas; um purge concorrente entre elas podia deixar o
///      somatório menor que a página lida) → as duas rodam no mesmo snapshot
///      REPEATABLE READ.
/// Flakes restantes do catálogo (família anônima de suíte sob execuções
/// consecutivas) seguem a regra de casa: capturar o nome no resumo, isolar 5×.
/// </summary>
public sealed class R65ConsolidacaoFlakesContratoTests
{
    private static readonly string[] Purgadoras =
    {
        "CobrancaB6Tests.cs",
        "OnboardingC7Tests.cs",
        "ProvisionamentoB5Tests.cs",
        "R23CaixaJustificativaEItemPedidoTests.cs",
        "R7OBOnboardingPreferenciasIdiomaTests.cs",
        "R8B4CancelamentoCompraRecebidaTests.cs",
        "SaasComercialB4MatrizTests.cs",
        "SaasGovernancaB6Rodada4Tests.cs"
    };

    [Fact]
    public void Familia_01_TodasAsPurgasRodemPeloRetryUnicoCompartilhado()
    {
        var dir = Path.Combine(RepositoryPathResolver.BackendRoot, "PlantaoPro.Tests");

        // O helper compartilhado existe com a assinatura correta: só 40P01/40001,
        // um único retry, e propaga na segunda falha (nunca mascara).
        var helperPath = Path.Combine(dir, "TestInfraRetryImpasse.cs");
        Assert.True(File.Exists(helperPath), "TestInfraRetryImpasse.cs ausente");
        var helper = File.ReadAllText(helperPath);
        Assert.Contains("catch (PostgresException ex) when (ex.SqlState is \"40P01\" or \"40001\")", helper);
        Assert.Contains("public static async Task ExecutarComRetryImpasseAsync(Func<Task> acao)", helper);

        // Uma única definição no projeto inteiro (o privado da SaasComercialB4 foi
        // promovido para o compartilhado — duas definições = receita dividida).
        var definicoes = Directory.EnumerateFiles(dir, "*.cs")
            .Where(f => !f.EndsWith("R65ConsolidacaoFlakesContratoTests.cs")) // este contrato cita o literal
            .Count(f => File.ReadAllText(f).Contains("ExecutarComRetryImpasseAsync(Func<Task> acao)"));
        Assert.Equal(1, definicoes);

        // Toda classe que purga roteia o PurgarAsync por ele.
        foreach (var classe in Purgadoras)
        {
            var conteudo = File.ReadAllText(Path.Combine(dir, classe));
            Assert.True(conteudo.Contains("TestInfraRetry.ExecutarComRetryImpasseAsync"),
                classe + " ainda purga fora do retry compartilhado");
        }
    }

    [Fact]
    public void Familia_02_CentralClientesExecutaItensETotaisNoMesmoSnapshot()
    {
        var path = Path.Combine(RepositoryPathResolver.BackendRoot, "PlantaoPro.Tests", "R19DapperBindingGuardTests.cs");
        var src = File.ReadAllText(path);
        Assert.True(src.Contains("IsolationLevel.RepeatableRead"),
            "itens/totais sem snapshot único volta a correr com purgas concorrentes");
        // A transaction precisa de fato alimentar o QueryMultiple (não existir
        // declarada e descartada): o parâmetro tx vai na mesma chamada.
        Assert.Contains(", tx);", src);
    }
}
