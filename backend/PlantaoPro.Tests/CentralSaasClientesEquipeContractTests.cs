using Xunit;

namespace PlantaoPro.Tests;

public sealed class CentralSaasClientesEquipeContractTests
{
    private static string Service => File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot, "backend/PlantaoPro.Api/SecurityAdministrationServices.cs"));

    [Fact]
    public void CriacaoDeUsuario_SerializaClienteERevalidaLimiteContratual()
    {
        Assert.Contains("select id from plantaopro.clientes", Service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("for update", Service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("p.limite_usuarios", Service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Limite contratual de", Service);
        Assert.Contains("antes de reativar este acesso", Service);
    }

    [Fact]
    public void Bloqueio_ProtegeUltimoAdministradorLocalERevogaSessaoNaTransacao()
    {
        Assert.Contains("O último administrador habilitado do cliente", Service);
        Assert.Contains("ADMINISTRADOR_CLIENTE", Service);
        Assert.Contains("update plantaopro.auth_sessoes", Service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("await tx.CommitAsync(ct)", Service);
    }

    [Fact]
    public void AlteracaoLocal_SeparaVinculoDeIdentidadeEProtegeRevogacaoDoUltimoAdministrador()
    {
        Assert.Contains("update plantaopro.usuario_tenant_acessos", Service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("VINCULO_USUARIO", Service);
        Assert.Contains("a identidade permanece inalterada", Service);
        Assert.Contains("perfil do último administrador habilitado", Service);
        Assert.Contains("PERMISSOES_PERFIL_ALTERADAS", Service);
    }

    [Fact]
    public void ConsultasDeCapacidade_UsamAliasValidoEmStringVerbatim()
    {
        Assert.DoesNotContain("as \\\"Limit\\\"", Service);
        Assert.DoesNotContain("as \\\"Used\\\"", Service);
        Assert.Contains("as \"\"Limit\"\"", Service);
        Assert.Contains("as \"\"Used\"\"", Service);
    }
}
