using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api;
using PlantaoPro.Api.Controllers;

namespace PlantaoPro.Tests;

public class SaasInteligenteContractTests
{
    [Fact]
    public void EndpointsSaasInteligente_DevemEstarExpostos()
    {
        var rotas = ObterRotasApi(
            typeof(ClientesController),
            typeof(PlanosController),
            typeof(AssinaturasController),
            typeof(FaturamentoSaasController),
            typeof(SaasInteligenciaController),
            typeof(SaasDashboardController),
            typeof(RelatoriosSaasController));

        var esperadas = new[]
        {
            "api/clientes/lookup",
            "api/clientes/{id:guid}/resumo-saas",
            "api/clientes/{id:guid}/saude",
            "api/clientes/{id:guid}/uso-plano",
            "api/clientes/{id:guid}/bloqueios",
            "api/clientes/{id:guid}/alertas",
            "api/planos",
            "api/assinaturas",
            "api/faturamento-saas/resumo",
            "api/faturamento-saas/gerar-mensal",
            "api/faturamento-saas/inadimplencia",
            "api/saas-inteligencia/clientes/{clienteId:guid}/saude",
            "api/saas-inteligencia/clientes/{clienteId:guid}/alertas",
            "api/saas-inteligencia/clientes/{clienteId:guid}/recomendacoes",
            "api/saas-inteligencia/resumo",
            "api/saas-inteligencia/clientes/{clienteId:guid}/recalcular",
            "api/saas-dashboard/resumo",
            "api/planos/{id:guid}/recursos",
            "api/assinaturas/{id:guid}/alterar-plano",
            "api/assinaturas/cliente/{clienteId:guid}/atual",
            "api/faturamento-saas/faturas/{id:guid}/resolver-contestacao",
            "api/relatorios/saas/clientes",
            "api/relatorios/saas/assinaturas",
            "api/relatorios/saas/faturamento",
            "api/relatorios/saas/inadimplencia",
            "api/relatorios/saas/uso-planos",
            "api/relatorios/saas/clientes-risco",
            "api/relatorios/saas/upgrade"
        };

        foreach (var rota in esperadas)
        {
            Assert.Contains(rota, rotas);
        }
    }

    [Fact]
    public void AssinaturaGuard_DeveExporContratoDeLimitesBloqueiosEUso()
    {
        var tipo = typeof(AssinaturaGuardService);
        var metodos = tipo.GetMethods().Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("ObterAssinaturaAtualAsync", metodos);
        Assert.Contains("ObterUsoPlanoAsync", metodos);
        Assert.Contains("PodeCadastrarMedicoAsync", metodos);
        Assert.Contains("PodeCadastrarHospitalAsync", metodos);
        Assert.Contains("PodeCadastrarUsuarioAsync", metodos);
        Assert.Contains("PodePublicarPlantaoAsync", metodos);
        Assert.Contains("PodeEnviarConviteAsync", metodos);
        Assert.Contains("PodeUsarMobileAsync", metodos);
        Assert.Contains("PodeUsarBIAsync", metodos);
        Assert.Contains("PodeUsarAPIAsync", metodos);
        Assert.Contains("PodeUsarIntegracoesAsync", metodos);
        Assert.Contains("PodeUsarRelatoriosAvancadosAsync", metodos);
        Assert.Contains("RegistrarBloqueioAsync", metodos);
        Assert.Contains("RegistrarUsoAsync", metodos);
    }

    [Fact]
    public void SqlSaasInteligente_DeveSerIncrementalIdempotenteESemConstraintIfNotExists()
    {
        var raiz = EncontrarRaizRepositorio();
        var sql = File.ReadAllText(Path.Combine(raiz, "database", "migrations", "2026_plantao_pro_saas_inteligente.sql"));

        foreach (var tabela in new[]
        {
            "plantaopro.plano_recursos",
            "plantaopro.assinatura_historico",
            "plantaopro.assinatura_uso",
            "plantaopro.faturas_saas",
            "plantaopro.fatura_itens",
            "plantaopro.pagamentos_saas",
            "plantaopro.cobranca_eventos",
            "plantaopro.customer_success_interacoes",
            "plantaopro.customer_success_planos_acao",
            "plantaopro.cliente_saude_historico",
            "plantaopro.cliente_alertas",
            "plantaopro.cliente_bloqueios",
            "plantaopro.cliente_limites_uso"
        })
        {
            Assert.Contains("CREATE TABLE IF NOT EXISTS " + tabela, sql, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("CREATE UNIQUE INDEX IF NOT EXISTS ux_clientes_cnpj_ativo", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DO $$", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ADD CONSTRAINT IF NOT EXISTS", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DocumentacaoSaas_DeveCobrirHomologacaoDemoEOperacao()
    {
        var raiz = EncontrarRaizRepositorio();
        var arquivos = new[]
        {
            Path.Combine(raiz, "docs", "saas", "visao-geral-saas.md"),
            Path.Combine(raiz, "docs", "saas", "clientes.md"),
            Path.Combine(raiz, "docs", "saas", "planos.md"),
            Path.Combine(raiz, "docs", "saas", "assinaturas.md"),
            Path.Combine(raiz, "docs", "saas", "limites-e-bloqueios.md"),
            Path.Combine(raiz, "docs", "saas", "faturamento-saas.md"),
            Path.Combine(raiz, "docs", "saas", "inteligencia-saas.md"),
            Path.Combine(raiz, "docs", "saas", "customer-success.md"),
            Path.Combine(raiz, "docs", "saas", "relatorios-saas.md"),
            Path.Combine(raiz, "docs", "homologacao", "roteiro-saas-completo.md"),
            Path.Combine(raiz, "docs", "demo", "roteiro-demo-saas.md")
        };

        foreach (var arquivo in arquivos)
        {
            Assert.True(File.Exists(arquivo), "Documento obrigatório ausente: " + arquivo);
            var conteudo = File.ReadAllText(arquivo);
            Assert.Contains("SaaS", conteudo, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static HashSet<string> ObterRotasApi(params Type[] controllers)
    {
        var rotas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var controller in controllers)
        {
            var prefixo = controller.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>().FirstOrDefault()?.Template ?? string.Empty;
            var metodos = controller.GetMethods();
            foreach (var metodo in metodos)
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
