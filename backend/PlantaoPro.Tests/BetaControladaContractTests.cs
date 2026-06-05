using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Controllers;

namespace PlantaoPro.Tests;

public class BetaControladaContractTests
{
    [Fact]
    public void OperacaoAssistida_DeveListarEndpointsObrigatoriosDaBetaControlada()
    {
        var rotas = typeof(OperacaoAssistidaController)
            .GetMethods()
            .SelectMany(m => m.GetCustomAttributes(typeof(HttpMethodAttribute), inherit: true).Cast<HttpMethodAttribute>())
            .SelectMany(a => a.Template is null ? Array.Empty<string>() : new[] { a.Template })
            .ToArray();

        var obrigatorias = new[]
        {
            "clientes",
            "clientes/{clienteId:guid}",
            "clientes/{clienteId:guid}/checklist",
            "checklist/{id:guid}/concluir",
            "checklist/{id:guid}/reabrir",
            "clientes/{clienteId:guid}/ocorrencias",
            "ocorrencias/{id:guid}/resolver",
            "clientes/{clienteId:guid}/treinamentos"
        };

        foreach (var rota in obrigatorias)
        {
            Assert.Contains(rota, rotas);
        }
    }

    [Fact]
    public void MobileApi_DeveListarEndpointsMvpSprintZero()
    {
        var rotas = typeof(MobileController)
            .GetMethods()
            .SelectMany(m => m.GetCustomAttributes(typeof(HttpMethodAttribute), inherit: true).Cast<HttpMethodAttribute>())
            .SelectMany(a => a.Template is null ? Array.Empty<string>() : new[] { a.Template })
            .ToArray();

        var obrigatorias = new[]
        {
            "auth/login",
            "me",
            "dashboard",
            "plantoes-disponiveis",
            "plantoes/{id:guid}",
            "plantoes/{id:guid}/solicitar",
            "convites",
            "convites/{id:guid}/aceitar",
            "convites/{id:guid}/recusar",
            "minhas-escalas",
            "meus-pagamentos",
            "notificacoes",
            "notificacoes/{id:guid}/lida",
            "perfil",
            "disponibilidade",
            "preferencias",
            "suporte/chamados"
        };

        foreach (var rota in obrigatorias)
        {
            Assert.Contains(rota, rotas);
        }
    }

    [Fact]
    public void ChecklistBeta_DeveConterRoteiroManualFinalComPassosCriticos()
    {
        var raiz = EncontrarRaizRepositorio();
        var arquivo = Path.Combine(raiz, "docs", "homologacao", "checklist-beta-comercial.md");
        var conteudo = File.ReadAllText(arquivo);

        Assert.Contains("Roteiro manual final da Beta Controlada", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Confirmar ausência de resíduos", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Operação Assistida", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("API Mobile", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Confirmar build verde", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PR sem arquivos binários", conteudo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RelatorioEstabilizacao_DeveRegistrarBranchBackupBuildEPendencias()
    {
        var raiz = EncontrarRaizRepositorio();
        var arquivo = Path.Combine(raiz, "docs", "homologacao", "relatorio-estabilizacao-plantao-pro-2026-06-04.md");
        var conteudo = File.ReadAllText(arquivo);

        Assert.Contains("backup local de segurança", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Nenhum merge adicional", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet: command not found", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/api/health", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Swagger", conteudo, StringComparison.OrdinalIgnoreCase);
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
