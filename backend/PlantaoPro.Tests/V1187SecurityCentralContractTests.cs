namespace PlantaoPro.Tests;

public sealed class V1187SecurityCentralContractTests
{
    private static string Root(){ var d=new DirectoryInfo(AppContext.BaseDirectory); while(d is not null && !Directory.Exists(Path.Combine(d.FullName,".git"))) d=d.Parent; return d?.FullName ?? throw new InvalidOperationException("Raiz não encontrada."); }
    private static string Read(string path)=>File.ReadAllText(Path.Combine(Root(), path));

    [Fact]
    public void GeradorNormalizaFuncoesESearchPath()
    {
        var generator = Read("scripts/generate-scrpt-completo.py");
        Assert.Contains("SET search_path TO plantaopro, public;", generator);
        Assert.Contains("gen_random_uuid(", generator);
        Assert.Contains("public.unaccent(", generator);
    }

    [Fact]
    public void ScriptCompletoNaoMantemFuncoesNaoQualificadas()
    {
        var sql = Read("database/scrpt_completo.sql");
        Assert.DoesNotContain("SET search_path TO plantaopro;", sql);
        Assert.DoesNotContain("uuid_generate_v4()", sql);
        Assert.DoesNotContain(" unaccent(", sql);
        Assert.Contains("auth_sessoes", sql);
        Assert.Contains("politicas_senha", sql);
    }

    [Fact]
    public void CentralSegurancaPossuiApiViewsEServicosPersistidos()
    {
        Assert.True(File.Exists(Path.Combine(Root(), "backend/PlantaoPro.Api/Controllers/SegurancaController.cs")));
        Assert.True(File.Exists(Path.Combine(Root(), "backend/PlantaoPro.Api/SecurityAdministrationServices.cs")));
        Assert.True(File.Exists(Path.Combine(Root(), "backend/PlantaoPro.Web/Controllers/SegurancaController.cs")));
        foreach (var view in new[] { "Index", "Usuarios", "UsuarioDetalhes", "Perfis", "PerfilDetalhes", "Permissoes", "Matriz", "Sessoes", "TentativasLogin", "AcessosNegados", "Auditoria", "PoliticasSenha" })
        {
            Assert.True(File.Exists(Path.Combine(Root(), $"backend/PlantaoPro.Web/Views/Seguranca/{view}.cshtml")), view);
        }
    }

    [Fact]
    public void WorkflowPublicaJobsObrigatoriosETrxReal()
    {
        var ci = Read(".github/workflows/dotnet-ci.yml");
        foreach (var job in new[] { "build-test", "database-complete-script-clean-install", "database-complete-script-idempotency", "database-legacy-compatibility", "database-upgrade", "database-schema-equivalence", "runtime-from-complete-script", "auth-e2e", "security-access-e2e", "swagger-contract", "mobile-check", "repository-security" })
        {
            Assert.Contains(job + ":", ci);
        }
        Assert.Contains("--results-directory artifacts/TestResults", ci);
        Assert.Contains("artifacts/TestResults/tests.trx", ci);
        Assert.DoesNotContain("continue-on-error: true", ci);
    }
}
