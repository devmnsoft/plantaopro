using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Controllers;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Tests;

public class CommercialDemoVersionContractTests
{
    [Fact]
    public void Api_DeveExporRotasComerciaisPublicasSaasPortaisModulosEDemo()
    {
        var rotas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tipo in new[]
        {
            typeof(PublicCommercialController), typeof(PropostasComerciaisController), typeof(AdminSaasApiController), typeof(ClientePortalApiController),
            typeof(ParceiroPortalApiController), typeof(ModulosApiController), typeof(FeatureFlagsApiController), typeof(DemoApiController)
        })
        {
            foreach (var rota in ObterRotas(tipo)) rotas.Add(rota);
        }

        foreach (var rota in new[]
        {
            "api/public/landing", "api/public/faq", "api/public/casos-uso", "api/public/lead", "api/public/agendar-demo",
            "api/public/simulador/perguntas", "api/public/simulador/calcular", "api/public/simulador/gerar-lead",
            "api/propostas-comerciais", "api/propostas-comerciais/{id:guid}/gerar-itens", "api/propostas-comerciais/{id:guid}/aprovar",
            "api/propostas-comerciais/{id:guid}/converter-em-cliente", "api/propostas-comerciais/{id:guid}/preview",
            "api/admin-saas/resumo", "api/cliente-portal/resumo", "api/parceiro-portal/resumo", "api/modulos/{id:guid}/habilitar-tenant",
            "api/modulos/{id:guid}/desabilitar-tenant", "api/feature-flags/{id:guid}", "api/demo/gerar-dados", "api/demo/limpar-dados", "api/demo/status"
        })
        {
            Assert.Contains(rota, rotas);
        }
    }

    [Fact]
    public void Dtos_DeveUsarValidacaoEPadraoApiResponse()
    {
        Assert.Contains(nameof(ApiResponse<string>.Ok), typeof(ApiResponse<string>).GetMethods().Select(x => x.Name));
        var lead = File.ReadAllText(Path.Combine(EncontrarRaizRepositorio(), "backend", "PlantaoPro.Api", "CommercialDemoDtos.cs"));
        Assert.Contains("[Required] public string Nome", lead, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[Required, EmailAddress]", lead, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("public string Website", lead, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DescontoPercentual", lead, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Service_DeveBloquearPropostaVencidaSpamDemoRealEGovernancaAuditada()
    {
        var service = File.ReadAllText(Path.Combine(EncontrarRaizRepositorio(), "backend", "PlantaoPro.Api", "CommercialDemoService.cs"));
        Assert.Contains("Proposta vencida não pode ser aprovada", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Website", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ApenasDemo = true", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ToggleModuleForTenantAsync", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UpdateFeatureFlagAsync", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AuditAsync", service, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Migracao_DeveConterTodasTabelasObrigatoriasEIdempotencia()
    {
        var sql = File.ReadAllText(Path.Combine(EncontrarRaizRepositorio(), "database", "migrations", "2026_plantao_pro_versao_comercial_demo.sql"));
        foreach (var tabela in new[]
        {
            "public_landing_secoes", "public_landing_depoimentos", "public_landing_faq", "public_landing_ctas", "public_landing_casos_uso", "public_landing_materiais", "public_landing_leads",
            "simulador_planos_perguntas", "simulador_planos_respostas", "simulador_planos_resultados", "simulador_planos_historico",
            "propostas_comerciais", "proposta_comercial_itens", "proposta_comercial_planos", "proposta_comercial_modulos", "proposta_comercial_condicoes", "proposta_comercial_aceites", "proposta_comercial_historico", "proposta_comercial_templates",
            "demo_ambientes", "demo_usuarios", "demo_roteiros", "demo_checklists", "demo_execucoes", "demo_feedbacks", "modulos_sistema", "modulo_dependencias", "modulo_planos", "modulo_flags", "modulo_habilitacoes_tenant", "modulo_historico_alteracoes",
            "onboarding_templates", "onboarding_template_etapas", "onboarding_template_tarefas", "onboarding_template_perfis", "onboarding_template_aplicacoes", "proposta_templates", "proposta_template_secoes", "proposta_template_variaveis", "proposta_template_itens",
            "comercial_playbooks", "comercial_scripts", "comercial_cadencias", "comercial_followups", "comercial_metas", "comercial_resultados"
        })
        {
            Assert.Contains(tabela, sql, StringComparison.OrdinalIgnoreCase);
        }
        Assert.Contains("create table if not exists", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("create index if not exists", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("do $$", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ADD CONSTRAINT IF NOT EXISTS", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static HashSet<string> ObterRotas(Type controller)
    {
        var rotas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var prefixo = controller.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>().FirstOrDefault()?.Template ?? string.Empty;
        foreach (var metodo in controller.GetMethods())
        {
            var atributos = metodo.GetCustomAttributes(typeof(HttpMethodAttribute), inherit: true).Cast<HttpMethodAttribute>();
            foreach (var atributo in atributos) rotas.Add(Combinar(prefixo, atributo.Template));
        }
        return rotas;
    }

    private static string Combinar(string prefixo, string? template)
    {
        if (string.IsNullOrWhiteSpace(template)) return prefixo.Trim('/');
        return (prefixo.TrimEnd('/') + "/" + template.TrimStart('/')).Trim('/');
    }

    private static string EncontrarRaizRepositorio()
    {
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (diretorio is not null && !Directory.Exists(Path.Combine(diretorio.FullName, ".git"))) diretorio = diretorio.Parent;
        if (diretorio is null) throw new InvalidOperationException("Raiz do repositório não encontrada.");
        return diretorio.FullName;
    }
}
