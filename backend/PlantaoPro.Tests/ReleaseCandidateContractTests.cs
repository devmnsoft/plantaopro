namespace PlantaoPro.Tests;

public class ReleaseCandidateContractTests
{
    [Fact]
    public void DocumentacaoHomologacao_DeveConterChecklistComercialObrigatorio()
    {
        var raiz = EncontrarRaizRepositorio();
        var arquivo = Path.Combine(raiz, "docs", "homologacao", "checklist-mvp-comercial.md");
        var conteudo = File.ReadAllText(arquivo);

        Assert.Contains("MVP comercial homologável", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Fluxo operacional médico", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Fluxo SaaS básico", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("API Mobile MVP", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Segurança e multiempresa", conteudo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SqlSuporteMobile_DeveManterContratoTipoAutorUsadoPelaApi()
    {
        var raiz = EncontrarRaizRepositorio();
        var arquivo = Path.Combine(raiz, "backend", "sql", "20260602_suporte_mobile_rc.sql");
        var conteudo = File.ReadAllText(arquivo);

        Assert.Contains("CREATE TABLE IF NOT EXISTS plantaopro.chamado_mensagens", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tipo_autor varchar(30) NOT NULL DEFAULT 'USUARIO'", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ALTER TABLE plantaopro.chamado_mensagens ADD COLUMN IF NOT EXISTS tipo_autor", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ADD CONSTRAINT IF NOT EXISTS", conteudo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DocumentacaoMobile_DeveListarEndpointsMvpObrigatorios()
    {
        var raiz = EncontrarRaizRepositorio();
        var arquivo = Path.Combine(raiz, "docs", "mobile", "mobile-api-endpoints.md");
        var conteudo = File.ReadAllText(arquivo);

        var endpoints = new[]
        {
            "/auth/login",
            "/me",
            "/dashboard",
            "/plantoes-disponiveis",
            "/plantoes/{id}",
            "/plantoes/{id}/solicitar",
            "/convites",
            "/convites/{id}/aceitar",
            "/convites/{id}/recusar",
            "/minhas-escalas",
            "/meus-pagamentos",
            "/notificacoes",
            "/notificacoes/{id}/lida",
            "/perfil",
            "/disponibilidade",
            "/preferencias",
            "/suporte/chamados"
        };

        foreach (var endpoint in endpoints)
        {
            Assert.Contains(endpoint, conteudo, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string EncontrarRaizRepositorio()
    {
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (diretorio is not null && !Directory.Exists(Path.Combine(diretorio.FullName, ".git")))
        {
            diretorio = diretorio.Parent;
        }

        if (diretorio is null)
        {
            throw new InvalidOperationException("Raiz do repositório não encontrada para testes de contrato.");
        }

        return diretorio.FullName;
    }
}

public class SaasCommercialContractTests
{
    [Fact]
    public void AssinaturaGuard_DeveExporValidacoesComerciaisDoPlano()
    {
        var metodos = typeof(PlantaoPro.Api.AssinaturaGuardService).GetMethods().Select(m => m.Name).ToArray();

        Assert.Contains("PodeCadastrarMedico", metodos);
        Assert.Contains("PodeCadastrarHospital", metodos);
        Assert.Contains("PodePublicarPlantao", metodos);
        Assert.Contains("PodeUsarMobile", metodos);
        Assert.Contains("PodeUsarBi", metodos);
        Assert.Contains("PodeUsarRelatoriosAvancados", metodos);
        Assert.Contains("PodeUsarIntegracoes", metodos);
        Assert.Contains("ObterUsoPlano", metodos);
    }

    [Fact]
    public void FaturamentoSaasController_DeveExporEndpointsMvpComercial()
    {
        var rotas = typeof(PlantaoPro.Api.Controllers.FaturamentoSaasController)
            .GetMethods()
            .SelectMany(m => m.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpMethodAttribute), inherit: true).Cast<Microsoft.AspNetCore.Mvc.HttpMethodAttribute>())
            .SelectMany(a => a.Template is null ? Array.Empty<string>() : new[] { a.Template })
            .ToArray();

        Assert.Contains("resumo", rotas);
        Assert.Contains("faturas", rotas);
        Assert.Contains("faturas/{id:guid}", rotas);
        Assert.Contains("gerar-mensal", rotas);
        Assert.Contains("faturas/{id:guid}/marcar-paga", rotas);
        Assert.Contains("faturas/{id:guid}/cancelar", rotas);
        Assert.Contains("faturas/{id:guid}/notificar", rotas);
        Assert.Contains("faturas/{id:guid}/contestar", rotas);
        Assert.Contains("inadimplencia", rotas);
    }

    [Fact]
    public void SqlMvpComercial_DeveUsarConstraintsSegurasEStatusDeFatura()
    {
        var raiz = EncontrarRaizRepositorio();
        var arquivo = Path.Combine(raiz, "backend", "sql", "20260603_mvp_comercial_avancado.sql");
        var conteudo = File.ReadAllText(arquivo);

        Assert.Contains("CREATE TABLE IF NOT EXISTS plantaopro.faturas_saas", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE INDEX IF NOT EXISTS ix_faturas_saas_cliente_status", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EM_CONTESTACAO", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ADD CONSTRAINT IF NOT EXISTS", conteudo, StringComparison.OrdinalIgnoreCase);
    }

    private static string EncontrarRaizRepositorio()
    {
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (diretorio is not null && !Directory.Exists(Path.Combine(diretorio.FullName, ".git")))
        {
            diretorio = diretorio.Parent;
        }

        if (diretorio is null)
        {
            throw new InvalidOperationException("Raiz do repositório não encontrada para testes de contrato.");
        }

        return diretorio.FullName;
    }
}

public class BetaComercialDocumentationContractTests
{
    [Fact]
    public void ChecklistBetaComercial_DeveCobrirOperacaoSaasMobileESeguranca()
    {
        var raiz = EncontrarRaizRepositorio();
        var arquivo = Path.Combine(raiz, "docs", "homologacao", "checklist-beta-comercial.md");
        var conteudo = File.ReadAllText(arquivo);

        Assert.Contains("Beta Comercial Controlada", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Checklist operacional médico", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Checklist SaaS básico", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("API Mobile MVP", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("segurança", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("observabilidade", conteudo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SprintZeroMobile_DeveOrientarReactNativeExpoSecureStoreETelasMvp()
    {
        var raiz = EncontrarRaizRepositorio();
        var arquivo = Path.Combine(raiz, "docs", "mobile", "sprint-zero-app.md");
        var conteudo = File.ReadAllText(arquivo);

        Assert.Contains("React Native", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Expo", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SecureStore", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Plantões disponíveis", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Meus pagamentos", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Suporte", conteudo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeployGoLiveBeta_DeveConterSmokeTestRollbackEOperacaoAssistida()
    {
        var raiz = EncontrarRaizRepositorio();
        var arquivo = Path.Combine(raiz, "docs", "deploy", "checklist-go-live-beta.md");
        var conteudo = File.ReadAllText(arquivo);

        Assert.Contains("Smoke test obrigatório", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Rollback", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/api/health", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Fluxo operacional médico", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("operação assistida", conteudo, StringComparison.OrdinalIgnoreCase);
    }

    private static string EncontrarRaizRepositorio()
    {
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (diretorio is not null && !Directory.Exists(Path.Combine(diretorio.FullName, ".git")))
        {
            diretorio = diretorio.Parent;
        }

        if (diretorio is null)
        {
            throw new InvalidOperationException("Raiz do repositório não encontrada para testes de contrato.");
        }

        return diretorio.FullName;
    }
}
