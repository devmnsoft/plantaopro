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
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("PLANTAOPRO_TEST_CONNECTION")
        ?? Environment.GetEnvironmentVariable("PLANTAOPRO_CONNECTION_STRING")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
        ?? "Host=127.0.0.1;Port=5432;Database=plantaopro_test;Username=postgres;Password=123456;Pooling=true;Maximum Pool Size=50;Minimum Pool Size=0;Timeout=30;Command Timeout=60;Search Path=PlantaoPro,public;Application Name=PlantaoPro.tests";
}
