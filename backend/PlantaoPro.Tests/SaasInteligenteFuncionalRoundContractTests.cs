using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api;
using PlantaoPro.Api.Controllers;

namespace PlantaoPro.Tests;

public class SaasInteligenteFuncionalRoundContractTests
{
    [Fact]
    public void SqlFuncional_DeveConsolidarTabelasERegrasIdempotentes()
    {
        var raiz = EncontrarRaizRepositorio();
        var caminho = Path.Combine(raiz, "database", "migrations", "2026_plantao_pro_saas_inteligente_funcional.sql");
        Assert.True(File.Exists(caminho), "Migração funcional obrigatória ausente.");
        var sql = File.ReadAllText(caminho);

        foreach (var tabela in new[]
        {
            "plantaopro.clientes",
            "plantaopro.planos",
            "plantaopro.assinaturas",
            "plantaopro.faturas_saas",
            "plantaopro.comercial_leads",
            "plantaopro.comercial_oportunidades",
            "plantaopro.comercial_propostas",
            "plantaopro.jornada_cliente",
            "plantaopro.customer_success_planos_acao",
            "plantaopro.lgpd_consentimentos",
            "plantaopro.ajuda_artigos",
            "plantaopro.auditoria_eventos"
        })
        {
            Assert.Contains("CREATE TABLE IF NOT EXISTS " + tabela, sql, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("DO $$", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE INDEX IF NOT EXISTS", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE UNIQUE INDEX IF NOT EXISTS ux_assinaturas_cliente_ativa", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ADD CONSTRAINT IF NOT EXISTS", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DashboardSaas_DevePermitirAlertasGeraisParaCustomerSuccess()
    {
        var rotas = ObterRotasApi(typeof(SaasDashboardController));
        Assert.Contains("api/saas-dashboard/alertas", rotas);

        var service = File.ReadAllText(Path.Combine(EncontrarRaizRepositorio(), "backend", "PlantaoPro.Api", "SaasIntelligenceService.cs"));
        var controller = File.ReadAllText(Path.Combine(EncontrarRaizRepositorio(), "backend", "PlantaoPro.Api", "Controllers", "SaasDashboardController.cs"));
        var web = File.ReadAllText(Path.Combine(EncontrarRaizRepositorio(), "backend", "PlantaoPro.Web", "Controllers", "CustomerSuccessController.cs"));

        Assert.Contains("ListarAlertasAbertosAsync", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ListarAlertasAbertosAsync", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("api/saas-dashboard/alertas", web, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Informe o cliente para listar alertas", controller, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public void SqlAuditavel_DeveExistirComNomeDaRodadaEConterTodasAsAreas()
    {
        var raiz = EncontrarRaizRepositorio();
        var caminho = Path.Combine(raiz, "database", "migrations", "2026_plantao_pro_saas_inteligente_auditavel.sql");
        Assert.True(File.Exists(caminho), "Migração da rodada SaaS inteligente auditável ausente.");
        var sql = File.ReadAllText(caminho);

        foreach (var tabela in new[]
        {
            "plantaopro.clientes",
            "plantaopro.planos",
            "plantaopro.plano_recursos",
            "plantaopro.assinaturas",
            "plantaopro.faturas_saas",
            "plantaopro.comercial_leads",
            "plantaopro.comercial_oportunidades",
            "plantaopro.comercial_propostas",
            "plantaopro.jornada_cliente",
            "plantaopro.customer_success_riscos",
            "plantaopro.lgpd_solicitacoes_titular",
            "plantaopro.ajuda_feedbacks",
            "plantaopro.logs_operacionais",
            "plantaopro.auditoria_lgpd_eventos"
        })
        {
            Assert.Contains("create table if not exists " + tabela, sql, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("DO $$", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE INDEX IF NOT EXISTS", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ADD CONSTRAINT IF NOT EXISTS", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FluxosOperacionais_DeveBloquearBiERelatoriosAvancadosPorPlano()
    {
        var raiz = EncontrarRaizRepositorio();
        var biController = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Api", "Controllers", "BiController.cs"));
        var relatoriosController = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Api", "Controllers", "RelatoriosSaasController.cs"));

        Assert.Contains("PodeUsarBIAsync", biController, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ValidarPlanoBiAsync", biController, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AcessoNegado", biController, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PodeUsarRelatoriosAvancadosAsync", relatoriosController, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ValidarRelatorioAvancadoAsync", relatoriosController, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AcessoNegado", relatoriosController, StringComparison.OrdinalIgnoreCase);
    }

    private static HashSet<string> ObterRotasApi(params Type[] controllers)
    {
        var rotas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var controller in controllers)
        {
            var prefixo = controller.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>().FirstOrDefault()?.Template ?? string.Empty;
            foreach (var metodo in controller.GetMethods())
            {
                var atributos = metodo.GetCustomAttributes(typeof(HttpMethodAttribute), inherit: true).Cast<HttpMethodAttribute>().ToArray();
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
        if (string.IsNullOrWhiteSpace(template)) return prefixo.Trim('/');
        return (prefixo.Trim('/') + "/" + template.Trim('/')).Trim('/');
    }

    private static string EncontrarRaizRepositorio()
    {
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (diretorio is not null && !Directory.Exists(Path.Combine(diretorio.FullName, ".git"))) diretorio = diretorio.Parent;
        if (diretorio is null) throw new InvalidOperationException("Raiz do repositório não encontrada para testes de contrato.");
        return diretorio.FullName;
    }
}
