using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// WP-A4 — validação pré-destrutiva do <see cref="DatabaseResetService"/> (TRUNCATE ... cascade).
/// Testa a regra pura de autorização (isolada dos environment variables) em qualquer ambiente:
/// o banco só pode ser destruído se estiver explicitamente autorizado como descartável.
/// </summary>
public sealed class DatabaseResetServiceValidacaoPreDestrutivaTests
{
    [Fact(DisplayName = "Reset: banco sem autorização explícita é bloqueado antes do TRUNCATE")]
    public void BancoNaoAutorizado_BloqueiaComMensagemExplicita()
    {
        var erro = Assert.Throws<InvalidOperationException>(() =>
            DatabaseResetService.ValidarBancoAutorizado("plantaopro_dev", ciAutorizada: false, autorizacaoExplicita: false));
        Assert.Contains("não está", erro.Message);
        Assert.Contains("explicitamente autorizado", erro.Message);
    }

    [Theory(DisplayName = "Reset: banco com 'test' no nome é autorizado (padrão local e CI)")]
    [InlineData("plantaopro_test")]
    [InlineData("PLANTAOPRO_TEST_CI")]
    public void NomeComTest_Autoriza(string nomeBanco)
    {
        // Sem exceção = autorizado.
        DatabaseResetService.ValidarBancoAutorizado(nomeBanco, ciAutorizada: false, autorizacaoExplicita: false);
    }

    [Fact(DisplayName = "Reset: PLANTAOPRO_TEST_CONNECTION (CI) autoriza qualquer nome de banco")]
    public void CiAutorizada_AutorizaIndependenteDoNome()
    {
        DatabaseResetService.ValidarBancoAutorizado("gate_disposable", ciAutorizada: true, autorizacaoExplicita: false);
    }

    [Fact(DisplayName = "Reset: PLANTAOPRO_ALLOW_DESTRUCTIVE_DB=on autoriza qualquer nome de banco")]
    public void AutorizacaoExplicita_AutorizaIndependenteDoNome()
    {
        DatabaseResetService.ValidarBancoAutorizado("plantaopro_dev", ciAutorizada: false, autorizacaoExplicita: true);
    }
}
