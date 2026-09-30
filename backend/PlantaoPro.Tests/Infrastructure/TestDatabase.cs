namespace PlantaoPro.Tests.Infrastructure;

/// <summary>
/// Fonte única da connection string dos testes de integração contra PostgreSQL
/// (substitui as resoluções duplicadas que existiam em cada suíte de testes).
///
/// Ordem de resolução:
///   1. PLANTAOPRO_TEST_CONNECTION   - banco descartável de CI (mantém testes separados do banco dev/demo);
///   2. PLANTAOPRO_CONNECTION_STRING - banco dev/demo definido explicitamente;
///   3. ConnectionStrings__Default   - mesmo valor usado pela aplicação em execução;
///   4. Padrão local (plantaopro_test) para rodar sem nenhuma configuração.
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

        var connectionOverride = Environment.GetEnvironmentVariable("PLANTAOPRO_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(connectionOverride))
            return (connectionOverride, "PLANTAOPRO_CONNECTION_STRING");

        var appDefault = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
        if (!string.IsNullOrWhiteSpace(appDefault))
            return (appDefault, "ConnectionStrings__Default");

        const string padraoLocal = "Host=127.0.0.1;Port=5432;Database=plantaopro_test;Username=postgres;Password=123456;Pooling=true;Maximum Pool Size=50;Minimum Pool Size=0;Timeout=30;Command Timeout=60;Search Path=PlantaoPro,public;Application Name=PlantaoPro.tests";
        return (padraoLocal, "padrao local (nenhuma variavel de ambiente definida)");
    }
}
