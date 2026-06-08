using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api;
using PlantaoPro.Api.Controllers;

namespace PlantaoPro.Tests;

public class SaasLgpdJornadaInteligenteContractTests
{
    [Fact]
    public void EndpointsDaRodadaSaasLgpdJornadaManual_DevemEstarExpostos()
    {
        var rotas = ObterRotasApi(
            typeof(JornadaClientesController),
            typeof(ComercialController),
            typeof(LgpdController),
            typeof(AjudaApiController),
            typeof(InteligenciaSaasController),
            typeof(SaasDashboardController));

        var esperadas = new[]
        {
            "api/jornada-clientes",
            "api/jornada-clientes/{clienteId:guid}/avancar",
            "api/jornada-clientes/{clienteId:guid}/retroceder",
            "api/jornada-clientes/{clienteId:guid}/eventos",
            "api/jornada-clientes/{clienteId:guid}/tarefas",
            "api/jornada-clientes/tarefas/{id:guid}/concluir",
            "api/jornada-clientes/funil",
            "api/comercial/leads",
            "api/comercial/oportunidades/{id:guid}/ganhar",
            "api/comercial/oportunidades/{id:guid}/perder",
            "api/comercial/propostas/{id:guid}/aprovar",
            "api/comercial/funil",
            "api/comercial/previsao-receita",
            "api/comercial/sugerir-plano",
            "api/lgpd/politica-atual",
            "api/lgpd/consentimentos/registrar",
            "api/lgpd/exportar-meus-dados",
            "api/lgpd/anonimizar/{usuarioId:guid}",
            "api/ajuda/topicos",
            "api/ajuda/buscar",
            "api/ajuda/checklists/perfil/{perfil}",
            "api/inteligencia/saas/resumo",
            "api/inteligencia/clientes/{clienteId:guid}/saude",
            "api/inteligencia/recalcular",
            "api/saas-dashboard/clientes-risco",
            "api/saas-dashboard/faturas-vencidas",
            "api/saas-dashboard/oportunidades-upgrade",
            "api/saas-dashboard/funil-comercial"
        };

        foreach (var rota in esperadas)
        {
            Assert.Contains(rota, rotas);
        }
    }

    [Fact]
    public void SqlDaRodadaSaasLgpdJornadaManual_DeveConterTabelasObrigatoriasEConstraintsIdempotentes()
    {
        var raiz = EncontrarRaizRepositorio();
        var sql = File.ReadAllText(Path.Combine(raiz, "database", "migrations", "2026_plantao_pro_saas_lgpd_jornada_inteligente.sql"));

        foreach (var tabela in new[]
        {
            "plantaopro.clientes",
            "plantaopro.planos",
            "plantaopro.assinaturas",
            "plantaopro.cliente_bloqueios",
            "plantaopro.customer_success_riscos",
            "plantaopro.jornada_cliente_eventos",
            "plantaopro.comercial_propostas",
            "plantaopro.lgpd_consentimentos",
            "plantaopro.ajuda_artigos",
            "plantaopro.ajuda_feedbacks"
        })
        {
            Assert.Contains("CREATE TABLE IF NOT EXISTS " + tabela, sql, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("CREATE INDEX IF NOT EXISTS", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DO $$", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ADD CONSTRAINT IF NOT EXISTS", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SaasIntelligenceService_DeveExporMotorDeRegrasDaRodada()
    {
        var metodos = typeof(SaasIntelligenceService).GetMethods().Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("CalcularSaudeClienteAsync", metodos);
        Assert.Contains("DetectarRiscoCancelamentoAsync", metodos);
        Assert.Contains("DetectarOportunidadeUpgradeAsync", metodos);
        Assert.Contains("SugerirPlanoParaLeadAsync", metodos);
        Assert.Contains("SugerirAcoesCustomerSuccessAsync", metodos);
        Assert.Contains("DetectarUsoAnormalAsync", metodos);
        Assert.Contains("DetectarQuedaDeUsoAsync", metodos);
        Assert.Contains("GerarResumoExecutivoClienteAsync", metodos);
        Assert.Contains("GerarAlertasInteligentesAsync", metodos);
        Assert.Contains("GerarProximasAcoesComerciaisAsync", metodos);
        Assert.Contains("ListarFaturasVencidasAsync", metodos);
        Assert.Contains("GerarFunilComercialAsync", metodos);
    }

    [Fact]
    public void SqlExatoDaRodadaSolicitada_DeveConterTodasAsFamiliasEColunaTipoDaTarefa()
    {
        var raiz = EncontrarRaizRepositorio();
        var sql = File.ReadAllText(Path.Combine(raiz, "database", "migrations", "2026_plantao_pro_saas_jornada_lgpd_inteligencia.sql"));

        foreach (var tabela in new[]
        {
            "plantaopro.clientes",
            "plantaopro.planos",
            "plantaopro.plano_recursos",
            "plantaopro.assinaturas",
            "plantaopro.assinatura_historico",
            "plantaopro.assinatura_uso",
            "plantaopro.faturas_saas",
            "plantaopro.fatura_itens",
            "plantaopro.pagamentos_saas",
            "plantaopro.cobranca_eventos",
            "plantaopro.cliente_bloqueios",
            "plantaopro.cliente_alertas",
            "plantaopro.cliente_limites_uso",
            "plantaopro.cliente_saude_historico",
            "plantaopro.jornada_cliente",
            "plantaopro.jornada_cliente_eventos",
            "plantaopro.jornada_cliente_tarefas",
            "plantaopro.jornada_cliente_observacoes",
            "plantaopro.jornada_cliente_responsaveis",
            "plantaopro.comercial_leads",
            "plantaopro.comercial_oportunidades",
            "plantaopro.comercial_propostas",
            "plantaopro.comercial_proposta_itens",
            "plantaopro.comercial_interacoes",
            "plantaopro.comercial_motivos_perda",
            "plantaopro.comercial_regras_desconto",
            "plantaopro.customer_success_interacoes",
            "plantaopro.customer_success_planos_acao",
            "plantaopro.customer_success_riscos",
            "plantaopro.customer_success_tarefas",
            "plantaopro.lgpd_consentimentos",
            "plantaopro.lgpd_solicitacoes_titular",
            "plantaopro.lgpd_eventos_privacidade",
            "plantaopro.lgpd_politicas",
            "plantaopro.lgpd_bases_legais",
            "plantaopro.lgpd_retencao_dados",
            "plantaopro.lgpd_exportacoes_dados",
            "plantaopro.lgpd_anonimizacoes",
            "plantaopro.ajuda_topicos",
            "plantaopro.ajuda_artigos",
            "plantaopro.ajuda_passos",
            "plantaopro.ajuda_checklists",
            "plantaopro.ajuda_feedbacks"
        })
        {
            Assert.Contains("CREATE TABLE IF NOT EXISTS " + tabela, sql, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("ALTER TABLE plantaopro.jornada_cliente_tarefas ADD COLUMN IF NOT EXISTS tipo", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE INDEX IF NOT EXISTS", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DO $$", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ADD CONSTRAINT IF NOT EXISTS", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void JornadaEGuardSaas_DevemRegistrarChecklistAlertasEBloqueiosCriticos()
    {
        var raiz = EncontrarRaizRepositorio();
        var jornada = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Api", "SaasEvolutionServices.cs"));
        var guard = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Api", "TenantServices.cs"));

        Assert.Contains("Abrir checklist de operação assistida", jornada, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Validar primeiros plantões em operação assistida", jornada, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JORNADA_", jornada, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CLIENTE_SUSPENSO", guard, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CLIENTE_CANCELADO", guard, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ASSINATURA_CANCELADA", guard, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ASSINATURA_VENCIDA", guard, StringComparison.OrdinalIgnoreCase);
    }

    private static HashSet<string> ObterRotasApi(params Type[] controllers)
    {
        var rotas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var controller in controllers)
        {
            var prefixo = controller.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty;
            foreach (var metodo in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                foreach (var attr in metodo.GetCustomAttributes().OfType<HttpMethodAttribute>())
                {
                    var template = attr.Template ?? string.Empty;
                    var rota = string.IsNullOrWhiteSpace(template) ? prefixo : prefixo.TrimEnd('/') + "/" + template.TrimStart('/');
                    rotas.Add(rota);
                }
            }
        }
        return rotas;
    }

    private static string EncontrarRaizRepositorio()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
