using Npgsql;

namespace PlantaoPro.Tests;

/// <summary>
/// R65 (fila r53b1 §7 item 10): ponto único de infraestrutura contra impasse
/// transitório (família ① do catálogo de flakes). Origem: helper privado de
/// SaasComercialB4MatrizTests (r9m-P2), promovido aqui quando a mesma assinatura
/// (Npgsql 40P01 deadlock detectado / 40001 falha de serialização em purgas
/// concorrentes sob carga) continuou surfando em outras classes — ProvisionamentoB5,
/// R7OB, SaasGovernancaB6Rodada4 e os demais purlistas. As declarações de purge
/// são idempotentes por construção (IDs fixos/tenants próprios do teste), então o
/// retry único é seguro; se a segunda tentativa também falhar, a exceção propaga
/// (o retry nunca mascara falha real nem assert). Toda classe que purga deve
/// rotear o corpo do seu PurgarAsync por aqui.
/// </summary>
public static class TestInfraRetry
{
    public static async Task ExecutarComRetryImpasseAsync(Func<Task> acao)
    {
        try
        {
            await acao();
        }
        catch (PostgresException ex) when (ex.SqlState is "40P01" or "40001")
        {
            await Task.Delay(250);
            await acao();
        }
    }
}
