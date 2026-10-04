using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// WP8 (J11): TestDatabase é a fonte única da connection string dos testes e a ORIGEM do valor
/// resolvido precisa ser auditável (propriedade Origem), de modo que uma execução de CI possa
/// evidenciar de onde veio o banco efetivamente usado.
///
/// P0 (homologação): não existe fallback para a conexão operacional da aplicação —
/// PLANTAOPRO_CONNECTION_STRING e ConnectionStrings__Default são ignoradas, para que os
/// testes nunca escrevam no banco que a aplicação está usando em execução.
/// </summary>
public sealed class TestDatabaseResolutionTests
{
    private const string CsPadrao = "Host=127.0.0.1;Port=5432;Database=plantaopro_test;Username=postgres;Password=123456;Pooling=true;Maximum Pool Size=50;Minimum Pool Size=0;Timeout=30;Command Timeout=60;Search Path=PlantaoPro,public;Application Name=PlantaoPro.tests";

    private sealed class EnvScope : IDisposable
    {
        private readonly (string Name, string? Old)[] _prev;
        public EnvScope(params (string Name, string? Value)[] settings)
        {
            _prev = settings.Select(s => (s.Name, Environment.GetEnvironmentVariable(s.Name))).ToArray();
            foreach (var s in settings) Environment.SetEnvironmentVariable(s.Name, s.Value);
        }
        public void Dispose()
        {
            foreach (var p in _prev) Environment.SetEnvironmentVariable(p.Name, p.Old);
        }
    }

    [Fact]
    public void PLANTAOPRO_TEST_CONNECTION_vence_toda_a_ordem_e_origem_registra_a_fonte()
    {
        using var _ = new EnvScope(("PLANTAOPRO_TEST_CONNECTION", CsPadrao), ("PLANTAOPRO_CONNECTION_STRING", "Host=127.0.0.1;Port=5432;Database=operacional_1;Username=postgres;Password=123456"), ("ConnectionStrings__Default", "Host=127.0.0.1;Port=5432;Database=operacional_2;Username=postgres;Password=123456"));
        Assert.Equal(CsPadrao, TestDatabase.ConnectionString);
        Assert.Equal("PLANTAOPRO_TEST_CONNECTION", TestDatabase.Origem);
    }

    [Fact]
    public void variaveis_operacionais_sao_ignoradas_sem_banco_de_teste_explicito()
    {
        using var _ = new EnvScope(("PLANTAOPRO_TEST_CONNECTION", null), ("PLANTAOPRO_CONNECTION_STRING", "Host=127.0.0.1;Port=5432;Database=operacional_1;Username=postgres;Password=123456"), ("ConnectionStrings__Default", "Host=127.0.0.1;Port=5432;Database=operacional_2;Username=postgres;Password=123456"));
        Assert.Equal(CsPadrao, TestDatabase.ConnectionString);
        Assert.Equal("padrao local (plantaopro_test)", TestDatabase.Origem);
    }

    [Fact]
    public void nenhuma_variavel_definida_usa_padrao_local_plantaopro_test()
    {
        using var _ = new EnvScope(("PLANTAOPRO_TEST_CONNECTION", null), ("PLANTAOPRO_CONNECTION_STRING", null), ("ConnectionStrings__Default", null));
        Assert.Equal(CsPadrao, TestDatabase.ConnectionString);
        Assert.Equal("padrao local (plantaopro_test)", TestDatabase.Origem);
    }
}
