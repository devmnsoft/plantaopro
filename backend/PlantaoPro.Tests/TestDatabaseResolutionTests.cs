using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// WP8 (J11): TestDatabase é a fonte única da connection string dos testes e a ORIGEM do valor
/// resolvido precisa ser auditável (propriedade Origem), de modo que uma execução de CI possa
/// evidenciar se o banco veio de PLANTAOPRO_TEST_CONNECTION (explícito) ou de um fallback.
///
/// Todas as ramificações da resolução apontam para o MESMO banco efetivo nesta homologação,
/// então a troca de variáveis aqui é segura mesmo com coleções do xunit em paralelo: um teste
/// concorrente que resolver a string no meio da troca obtém o mesmo valor funcional.
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
        using var _ = new EnvScope(("PLANTAOPRO_TEST_CONNECTION", CsPadrao), ("PLANTAOPRO_CONNECTION_STRING", null), ("ConnectionStrings__Default", null));
        Assert.Equal(CsPadrao, TestDatabase.ConnectionString);
        Assert.Equal("PLANTAOPRO_TEST_CONNECTION", TestDatabase.Origem);
    }

    [Fact]
    public void sem_PLANTAOPRO_TEST_CONNECTION_cai_para_PLANTAOPRO_CONNECTION_STRING()
    {
        using var _ = new EnvScope(("PLANTAOPRO_TEST_CONNECTION", null), ("PLANTAOPRO_CONNECTION_STRING", CsPadrao), ("ConnectionStrings__Default", null));
        Assert.Equal(CsPadrao, TestDatabase.ConnectionString);
        Assert.Equal("PLANTAOPRO_CONNECTION_STRING", TestDatabase.Origem);
    }

    [Fact]
    public void sem_as_duas_primeiras_cai_para_ConnectionStrings__Default()
    {
        using var _ = new EnvScope(("PLANTAOPRO_TEST_CONNECTION", null), ("PLANTAOPRO_CONNECTION_STRING", null), ("ConnectionStrings__Default", CsPadrao));
        Assert.Equal(CsPadrao, TestDatabase.ConnectionString);
        Assert.Equal("ConnectionStrings__Default", TestDatabase.Origem);
    }

    [Fact]
    public void nenhuma_variavel_definida_usa_padrao_local_e_origem_registra_o_fallback()
    {
        using var _ = new EnvScope(("PLANTAOPRO_TEST_CONNECTION", null), ("PLANTAOPRO_CONNECTION_STRING", null), ("ConnectionStrings__Default", null));
        Assert.Equal(CsPadrao, TestDatabase.ConnectionString);
        Assert.Equal("padrao local (nenhuma variavel de ambiente definida)", TestDatabase.Origem);
    }
}
