using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Controllers;

namespace PlantaoPro.Tests;

public class PilotoComercialOperacaoB2BContractTests
{
    [Fact]
    public void Api_DeveExporRotasDoPilotoCsExecutivoOperacaoWhiteLabelTreinamentoEscalaRenovacaoExpansao()
    {
        var rotas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tipo in new[]
        {
            typeof(PilotoBetaController), typeof(CustomerSuccessApiController), typeof(ExecutivoController), typeof(WhiteLabelTemplatesApiController),
            typeof(TreinamentoController), typeof(OperacaoAssistidaPlanosController), typeof(CentralEscalaEvoluidaController), typeof(MedicoAgendaMeController),
            typeof(RenovacoesController), typeof(ExpansoesController)
        })
        {
            foreach (var rota in ObterRotas(tipo)) rotas.Add(rota);
        }

        foreach (var rota in new[]
        {
            "api/piloto/programas", "api/piloto/clientes/{id:guid}/converter", "api/piloto/feedbacks/{id:guid}/resolver", "api/piloto/indicadores",
            "api/customer-success/contas/{tenantId:guid}/health", "api/customer-success/contas/{tenantId:guid}/nps", "api/customer-success/riscos", "api/customer-success/oportunidades",
            "api/executivo/resumo", "api/executivo/receita", "api/executivo/produto", "api/executivo/metas",
            "api/operacao-assistida/planos", "api/operacao-assistida/etapas/{id:guid}/concluir", "api/operacao-assistida/atrasadas",
            "api/white-label/templates/{id:guid}/aplicar/{tenantId:guid}", "api/white-label/templates/{id:guid}/duplicar",
            "api/treinamento/trilhas/{id:guid}/concluir", "api/treinamento/artigos/{id:guid}/feedback",
            "api/central-escala/plantao-descoberto", "api/central-escala/medicos-disponiveis", "api/central-escala/substituir",
            "api/medicos/me/agenda", "api/medicos/me/disponibilidade", "api/medicos/me/substituicao",
            "api/renovacoes/{id:guid}/renovar", "api/expansoes/{id:guid}/ganhar", "api/expansoes/{id:guid}/perder"
        })
        {
            Assert.Contains(rota, rotas);
        }
    }

    [Fact]
    public void Migracao_DeveConterTabelasObrigatoriasEIdempotencia()
    {
        var raiz = EncontrarRaizRepositorio();
        var sql = File.ReadAllText(Path.Combine(raiz, "database", "migrations", "2026_plantao_pro_piloto_comercial_operacao_b2b.sql"));
        foreach (var tabela in new[]
        {
            "piloto_programas", "piloto_clientes", "piloto_feedbacks", "cs_contas", "cs_health_score", "cs_nps", "adocao_eventos", "churn_riscos",
            "operacao_assistida_planos", "white_label_templates", "white_label_template_aplicacoes", "suporte_chamados", "contas_b2b_renovacoes", "contas_b2b_expansoes", "executivo_kpis_snapshots"
        })
        {
            Assert.Contains("CREATE TABLE IF NOT EXISTS plantaopro." + tabela, sql, StringComparison.OrdinalIgnoreCase);
        }
        Assert.Contains("CREATE INDEX IF NOT EXISTS", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DO $$", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ADD CONSTRAINT IF NOT EXISTS", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Servico_DeveConterRegrasDeterministicasAuditoriaEValidacoesCriticas()
    {
        var raiz = EncontrarRaizRepositorio();
        var service = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Api", "B2BCommercialOpsServices.cs"));
        Assert.Contains("NPS baixo", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Uso acima de 80%", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RegistrarAsync", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Template não liberado", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Justificativa é obrigatória", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EtapasImplantacao", service, StringComparison.OrdinalIgnoreCase);
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
