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
