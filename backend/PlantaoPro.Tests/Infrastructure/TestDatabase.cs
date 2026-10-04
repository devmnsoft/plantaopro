namespace PlantaoPro.Tests.Infrastructure;

/// <summary>
/// Fonte única da connection string dos testes de integração contra PostgreSQL
/// (substitui as resoluções duplicadas que existiam em cada suíte de testes).
///
/// Ordem de resolução:
///   1. PLANTAOPRO_TEST_CONNECTION   - banco descartável de CI (mantém testes separados do banco dev/demo);
///   2. Padrão local (plantaopro_test) para rodar sem nenhuma configuração.
///
/// P0 (homologação): NÃO existe fallback para a conexão operacional da aplicação
/// (PLANTAOPRO_CONNECTION_STRING / ConnectionStrings__Default são ignoradas) — os testes
/// nunca podem escrever no banco que a aplicação está usando em execução.
///
/// Não existe senha alternativa silenciosa: se o banco configurado estiver fora do ar,
/// os testes falham com erro de conexão explícito.
/// </summary>
public static class TestDatabase
{
    public static string ConnectionString => ResolverConnectionString().Value;

    /// <summary>
    /// Nome da fonte que definiu o <see cref="ConnectionString"/> atual
    /// (auditoria: permite evidenciar em execuções de CI quais variáveis estiveram definidas).
    /// </summary>
    public static string Origem => ResolverConnectionString().Origem;

    private static (string Value, string Origem) ResolverConnectionString()
    {
        var testConnection = Environment.GetEnvironmentVariable("PLANTAOPRO_TEST_CONNECTION");
        if (!string.IsNullOrWhiteSpace(testConnection))
            return (testConnection, "PLANTAOPRO_TEST_CONNECTION");

        // P0 (homologação): PLANTAOPRO_CONNECTION_STRING e ConnectionStrings__Default são
        // deliberadamente ignoradas aqui (ver cabeçalho desta classe).
        const string padraoLocal = "Host=127.0.0.1;Port=5432;Database=plantaopro_test;Username=postgres;Password=123456;Pooling=true;Maximum Pool Size=50;Minimum Pool Size=0;Timeout=30;Command Timeout=60;Search Path=PlantaoPro,public;Application Name=PlantaoPro.tests";
        return (padraoLocal, "padrao local (plantaopro_test)");
    }
}
