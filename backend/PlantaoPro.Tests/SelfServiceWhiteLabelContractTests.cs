using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Controllers;

namespace PlantaoPro.Tests;

public class SelfServiceWhiteLabelContractTests
{
    [Fact]
    public void SelfService_DeveExporEndpointsPublicosObrigatorios()
    {
        var rotas = ObterRotas(typeof(PublicSelfServiceController));
        Assert.Contains("api/public/planos", rotas);
        Assert.Contains("api/public/planos/comparativo", rotas);
        Assert.Contains("api/public/cadastro/iniciar", rotas);
        Assert.Contains("api/public/cadastro/empresa", rotas);
        Assert.Contains("api/public/cadastro/plano", rotas);
        Assert.Contains("api/public/cadastro/usuario-admin", rotas);
        Assert.Contains("api/public/cadastro/finalizar", rotas);
    }

    [Fact]
    public void WhiteLabelPerfisParametrizacoesEMinhaAssinatura_DevemExporContratos()
    {
        var rotas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rota in ObterRotas(typeof(WhiteLabelController))) rotas.Add(rota);
        foreach (var rota in ObterRotas(typeof(PerfisController))) rotas.Add(rota);
        foreach (var rota in ObterRotas(typeof(PermissoesSistemaController))) rotas.Add(rota);
        foreach (var rota in ObterRotas(typeof(ParametrizacoesController))) rotas.Add(rota);
        foreach (var rota in ObterRotas(typeof(MinhaAssinaturaController))) rotas.Add(rota);

        Assert.Contains("api/white-label/configuracao", rotas);
        Assert.Contains("api/white-label/tenant/{tenantId:guid}", rotas);
        Assert.Contains("api/white-label/tenant/{tenantId:guid}/logo", rotas);
        Assert.Contains("api/perfis/{id:guid}/permissoes", rotas);
        Assert.Contains("api/permissoes", rotas);
        Assert.Contains("api/modulos-sistema", rotas);
        Assert.Contains("api/parametrizacoes/operacionais", rotas);
        Assert.Contains("api/minha-assinatura/uso", rotas);
        Assert.Contains("api/minha-assinatura/solicitar-upgrade", rotas);
        Assert.Contains("api/minha-assinatura/solicitar-downgrade", rotas);
    }

    [Fact]
    public void MigracaoWhiteLabelSelfService_DeveCriarTabelasCriticasEUsarConstraintsSeguras()
    {
        var raiz = EncontrarRaizRepositorio();
        var sql = File.ReadAllText(Path.Combine(raiz, "database", "migrations", "2026_plantao_pro_white_label_self_service.sql"));

        foreach (var tabela in new[] { "tenants", "tenant_white_label", "tenant_onboarding", "planos", "assinatura_uso", "cadastro_cliente_solicitacoes", "perfis", "permissoes", "white_label_assets", "lgpd_consentimentos" })
        {
            Assert.Contains(tabela, sql, StringComparison.OrdinalIgnoreCase);
        }
        Assert.Contains("DO $$", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ADD CONSTRAINT IF NOT EXISTS", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CadastroSelfService_DeveRegistrarTenantClienteAssinaturaAdminLgpdEOnboarding()
    {
        var raiz = EncontrarRaizRepositorio();
        var service = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Api", "SelfServiceServices.cs"));

        Assert.Contains("insert into plantaopro.tenants", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into plantaopro.clientes", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into plantaopro.assinaturas", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into plantaopro.usuarios", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("lgpd_consentimentos", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tenant_onboarding_checklist", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BCrypt.Net.BCrypt.HashPassword", service, StringComparison.OrdinalIgnoreCase);
    }

    private static HashSet<string> ObterRotas(Type controller)
    {
        var rotas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var prefixo = controller.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>().FirstOrDefault()?.Template ?? string.Empty;
        foreach (var metodo in controller.GetMethods())
        {
            var atributos = metodo.GetCustomAttributes(typeof(HttpMethodAttribute), inherit: true).Cast<HttpMethodAttribute>();
            foreach (var atributo in atributos)
            {
                foreach (var template in atributo.HttpMethods)
                {
                    _ = template;
                }
                rotas.Add(Combinar(prefixo, atributo.Template));
            }
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
