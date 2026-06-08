using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Controllers;

namespace PlantaoPro.Tests;

public class WhiteLabelB2BLaunchContractTests
{
    [Fact]
    public void B2BLaunch_DeveExporEndpointsObrigatorios()
    {
        var rotas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rota in ObterRotas(typeof(DeveloperController))) rotas.Add(rota);
        foreach (var rota in ObterRotas(typeof(ContratosController))) rotas.Add(rota);
        foreach (var rota in ObterRotas(typeof(SlaController))) rotas.Add(rota);
        foreach (var rota in ObterRotas(typeof(SuporteController))) rotas.Add(rota);
        foreach (var rota in ObterRotas(typeof(BetaController))) rotas.Add(rota);
        foreach (var rota in ObterRotas(typeof(MonitoramentoController))) rotas.Add(rota);
        foreach (var rota in ObterRotas(typeof(GoToMarketController))) rotas.Add(rota);

        Assert.Contains("api/developer/overview", rotas);
        Assert.Contains("api/developer/api-keys", rotas);
        Assert.Contains("api/developer/api-keys/{id:guid}/revogar", rotas);
        Assert.Contains("api/contratos/{id:guid}/sla", rotas);
        Assert.Contains("api/sla/incidentes/{id:guid}/resolver", rotas);
        Assert.Contains("api/suporte/chamados/{id:guid}/escalar", rotas);
        Assert.Contains("api/beta/feedbacks/{id:guid}/classificar", rotas);
        Assert.Contains("api/monitoramento/alertas/{id:guid}/resolver", rotas);
        Assert.Contains("api/gotomarket/materiais", rotas);
    }

    [Fact]
    public void MigracaoB2BLaunch_DeveConterTabelasWhiteLabelRevendaSlaTelemetriaELgpd()
    {
        var raiz = EncontrarRaizRepositorio();
        var sql = File.ReadAllText(Path.Combine(raiz, "database", "migrations", "2026_plantao_pro_white_label_b2b_launch.sql"));

        foreach (var tabela in new[] { "tenant_white_label", "white_label_publicacoes", "api_chaves", "api_rate_limits", "parceiros", "parceiro_repasses", "contratos", "contrato_slas", "suporte_chamados", "beta_feedbacks", "telemetria_alertas", "lgpd_solicitacoes_titular", "medico_disponibilidades", "plantao_substituicoes" })
        {
            Assert.Contains(tabela, sql, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("DO $$", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("create index if not exists ix_api_chaves_hash", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ADD CONSTRAINT IF NOT EXISTS", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ServicosB2B_DeveTerHashApiKeyAuditoriaEValidadorIsolamento()
    {
        var raiz = EncontrarRaizRepositorio();
        var service = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Api", "B2BLaunchServices.cs"));

        Assert.Contains("SHA256.Create", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ChaveExibicaoUnica", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TenantIsolationValidatorService", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ACESSO_CROSS_TENANT_BLOQUEADO", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BeginTransactionAsync", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RegistrarAsync", service, StringComparison.OrdinalIgnoreCase);
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
