using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Controllers;

namespace PlantaoPro.Tests;

public class BetaPrReadyContractTests
{
    [Fact]
    public void EndpointsPrincipaisDoReadme_DevemEstarExpostosNosControllersApi()
    {
        var rotas = ObterRotasApi(
            typeof(EscalasController),
            typeof(FinanceiroController),
            typeof(NotificacoesController),
            typeof(DashboardController),
            typeof(MobileController));

        var endpointsObrigatorios = new[]
        {
            "api/escalas",
            "api/escalas/{id:guid}",
            "api/medicos/me/plantoes",
            "api/plantoes/{id:guid}/aceitar",
            "api/escalas/{id:guid}/confirmar",
            "api/escalas/{id:guid}/recusar",
            "api/escalas/{id:guid}/cancelar",
            "api/escalas/{id:guid}/substituir",
            "api/escalas/{id:guid}/marcar-realizado",
            "api/financeiro/pagamentos",
            "api/financeiro/pagamentos/{id:guid}",
            "api/financeiro/pagamentos/gerar",
            "api/financeiro/pagamentos/{id:guid}/confirmar",
            "api/financeiro/pagamentos/{id:guid}/cancelar",
            "api/financeiro/meus-pagamentos",
            "api/notificacoes",
            "api/notificacoes/nao-lidas",
            "api/notificacoes/{id:guid}/lida",
            "api/notificacoes/lidas",
            "api/dashboard",
            "api/mobile/home"
        };

        foreach (var endpoint in endpointsObrigatorios)
        {
            Assert.Contains(endpoint, rotas);
        }
    }

    [Fact]
    public void AcoesCriticasFinanceirasENotificacoes_DevemTerLoggerTryCatchERespostaPadronizada()
    {
        var raiz = EncontrarRaizRepositorio();
        var financeiro = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Api", "Controllers", "FinanceiroController.cs"));
        var notificacoes = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Api", "Controllers", "NotificacoesController.cs"));

        Assert.Contains("ILogger<FinanceiroController>", financeiro, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ExecutarAcaoCriticaAsync", financeiro, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Não foi possível processar a ação financeira", financeiro, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ApiResponse<object>.Fail", financeiro, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("ILogger<NotificacoesController>", notificacoes, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("try", notificacoes, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("catch (Exception ex)", notificacoes, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ApiResponse<string>.Fail", notificacoes, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Request.Headers.Authorization", notificacoes, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RelatorioPrReady_DeveRegistrarEvidenciasDaRodadaHomologavel()
    {
        var raiz = EncontrarRaizRepositorio();
        var arquivo = Path.Combine(raiz, "docs", "homologacao", "relatorio-beta-homologavel-pr-ready-final-2026-06-05.md");
        var conteudo = File.ReadAllText(arquivo);

        Assert.Contains("PlantãoPro Beta Homologável PR-Ready", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sem resíduos", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Build", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("endpoints principais", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pendências reais", conteudo, StringComparison.OrdinalIgnoreCase);
    }

    private static HashSet<string> ObterRotasApi(params Type[] controllers)
    {
        var rotas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var controller in controllers)
        {
            var prefixo = controller
                .GetCustomAttributes(typeof(RouteAttribute), inherit: true)
                .Cast<RouteAttribute>()
                .FirstOrDefault()
                ?.Template ?? string.Empty;

            var metodos = controller.GetMethods();
            foreach (var metodo in metodos)
            {
                var atributos = metodo
                    .GetCustomAttributes(typeof(HttpMethodAttribute), inherit: true)
                    .Cast<HttpMethodAttribute>()
                    .ToArray();

                foreach (var atributo in atributos)
                {
                    rotas.Add(Combinar(prefixo, atributo.Template));
                }
            }
        }

        return rotas;
    }

    private static string Combinar(string prefixo, string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return prefixo.Trim('/');
        }

        return (prefixo.Trim('/') + "/" + template.Trim('/')).Trim('/');
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
