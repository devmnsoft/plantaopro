using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Controllers;

namespace PlantaoPro.Tests;

public class SaasComercialLgpdJornadaAjudaContractTests
{
    [Fact]
    public void SqlComercialLgpdJornadaAjuda_DeveConterTabelasObrigatoriasEIndices()
    {
        var raiz = EncontrarRaizRepositorio();
        var sql = File.ReadAllText(Path.Combine(raiz, "database", "migrations", "2026_plantao_pro_saas_comercial_lgpd_jornada.sql"));

        var tabelas = new[]
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
            "plantaopro.ajuda_feedbacks",
            "plantaopro.eventos_sistema",
            "plantaopro.logs_operacionais",
            "plantaopro.auditoria_eventos",
            "plantaopro.auditoria_lgpd_eventos"
        };

        foreach (var tabela in tabelas)
        {
            Assert.Contains("create table if not exists " + tabela, sql, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("create index if not exists ix_faturas_saas_vencimento", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("create unique index if not exists ux_jornada_cliente_cliente", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DO $$", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ADD CONSTRAINT IF NOT EXISTS", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EndpointsJornadaComercialLgpdAjuda_DeveEstarExpostos()
    {
        var rotas = ObterRotasApi(
            typeof(JornadaClientesController),
            typeof(ComercialController),
            typeof(LgpdController),
            typeof(AjudaApiController),
            typeof(InteligenciaSaasController),
            typeof(SaasDashboardController));

        foreach (var rota in new[]
        {
            "api/jornada-clientes/{clienteId:guid}/avancar",
            "api/jornada-clientes/{clienteId:guid}/retroceder",
            "api/jornada-clientes/funil",
            "api/comercial/leads",
            "api/comercial/oportunidades/{id:guid}/ganhar",
            "api/comercial/propostas/{id:guid}/aprovar",
            "api/comercial/sugerir-plano",
            "api/lgpd/consentimentos/registrar",
            "api/lgpd/exportar-meus-dados",
            "api/ajuda/buscar",
            "api/ajuda/checklists/perfil/{perfil}",
            "api/saas-dashboard/resumo"
        })
        {
            Assert.Contains(rota, rotas);
        }
    }

    [Fact]
    public void ManualInterativoWeb_DeveTerViewsComBuscaChecklistFeedbackELinks()
    {
        var raiz = EncontrarRaizRepositorio();
        var index = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Web", "Views", "Ajuda", "Index.cshtml"));
        var artigo = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Web", "Views", "Ajuda", "Artigo.cshtml"));
        var controller = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Web", "Controllers", "AjudaController.cs"));

        Assert.Contains("asp-action=\"Index\"", index, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Checklist", index, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("asp-action=\"Feedback\"", artigo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Abrir tela relacionada", artigo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ADMINISTRADOR_GLOBAL", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("COORDENACAO", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MEDICO", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FINANCEIRO", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HOSPITAL", controller, StringComparison.OrdinalIgnoreCase);
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
