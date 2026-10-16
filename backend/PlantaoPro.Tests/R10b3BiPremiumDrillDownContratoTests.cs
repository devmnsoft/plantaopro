using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// Rodada 10 r10b3 — §27-L208 ("Abrir registros que compõem o total" nos cards Bi/Premium).
///
/// Regra da fatia (plano r10b3 / r8a0 L208): estender o padrão existente de drill-down
/// (Operacional "Ver registros", FaturamentoClinico "Abrir origem") APENAS para KPIs cujo
/// conjunto de registros tem uma ROTA DE LISTAGEM EXISTENTE filtrável pela dimensão do KPI.
/// KPI sem rota alvo existente fica SEM link — zero link para rota inventada.
///
/// Alvos validados por fonte canônica (queries dos endpoints, não suposição):
///   • Bi Clientes Ativos        -> /Clientes?status=ATIVO        (api/clientes/central: reg_status='A' AND upper(status)='ATIVO' — predicado idêntico ao do KPI)
///   • Bi Receita Estimada       -> /Assinaturas?status=ATIVA     (api/assinaturas: reg_status='A' AND upper(status)=upper(@status))
///   • Bi Receita Vencida        -> /FaturamentoSaas?status=VENCIDA (api/faturamento-saas/faturas: f.status=@status, escopo automático por contexto de usuário)
///   • Bi Escalas no período     -> /Escalas?status=confirmado&dataInicio=&dataFim= (JS monta a partir de data.periodo)
///   • Bi Cobertura              -> /Plantoes?dataInicio=&dataFim= (JS; caveats: lista inclui rascunho/cancelado — superset documentado)
///   • Bi Valor pago no período  -> SEM LINK (api/financeiro/pagamentos filtra por pl.data_inicio, o KPI usa data_pagamento — dimensões diferentes)
///   • Premium Plantões          -> /Plantoes ; Premium Escalas -> /Escalas (estado atual reg_status='A', mesmo escopo)
///   • Premium Médicos           -> SEM LINK (api/medicos é diretório global, sem escopo de tenant; KPI é tenant-scoped)
///   • Premium Agendamentos      -> SEM LINK (/agenda é por período com default semana; sem rota de "todos os ativos")
/// </summary>
public sealed class R10b3BiPremiumDrillDownContratoTests
{
    private static string W(string p) => Path.Combine(RepositoryPathResolver.BackendRoot, "PlantaoPro.Web", p);

    [Fact]
    public void BiView_CardsComRotaAlvoExistente_TemDrillDown()
    {
        var view = File.ReadAllText(W("Views/Bi/Index.cshtml"));
        // Estado atual — alvos estáticos Razor (predicado exato do KPI):
        Assert.Contains("asp-controller=\"Clientes\"", view);
        Assert.Contains("asp-route-status=\"ATIVO\"", view);
        Assert.Contains("asp-controller=\"Assinaturas\"", view);
        Assert.Contains("asp-route-status=\"ATIVA\"", view);
        Assert.Contains("asp-controller=\"FaturamentoSaas\"", view);
        Assert.Contains("asp-route-status=\"VENCIDA\"", view);
        // Período — alvos montados via JS a partir de data.periodo (invisíveis até o fetch dar ok):
        Assert.Contains("data-drill=\"escalas\"", view);
        Assert.Contains("data-drill=\"cobertura\"", view);
        Assert.Contains("status=confirmado", view);
        Assert.Contains("/Plantoes?dataInicio=", view);
        Assert.DoesNotContain("href=\"#\"", view);
        // Os cinco cards com alvo usam o mesmo rótulo do padrão Operacional:
        Assert.True(view.Split("Ver registros").Length - 1 >= 5, "Esperava >= 5 links 'Ver registros' na view Bi.");
    }

    [Fact]
    public void BiView_ValorPagoNoPeriodo_SemRotaHonestaNaoTemLink()
    {
        var view = File.ReadAllText(W("Views/Bi/Index.cshtml"));
        Assert.DoesNotContain("data-drill=\"pagamentos\"", view);
        // O card 'Valor pago no período' não pode ganhar drill: a listagem api/financeiro/pagamentos
        // filtra por pl.data_inicio (data do plantão) e o KPI conta por data_pagamento — conjuntos diferentes.
        int i = view.IndexOf("id=\"kpi-pago-periodo\"", StringComparison.Ordinal);
        Assert.True(i >= 0, "Card 'Valor pago no período' sumiu da view Bi.");
        int fim = view.IndexOf("</div>", i, StringComparison.Ordinal);
        var seg = view.Substring(i, fim - i);
        Assert.DoesNotContain("Ver registros", seg);
        Assert.DoesNotContain("data-drill", seg);
    }

    [Fact]
    public void PremiumView_MapeiaSomenteTitulosComRotaDeListagemExistente()
    {
        var view = File.ReadAllText(W("Views/DashboardsPremium/Perfil.cshtml"));
        // Mapeamento por título (o set de KPIs é fixo e definido em DashboardPremiumService):
        Assert.Contains("kpi.Titulo == \"Plantões\"", view);
        Assert.Contains("kpi.Titulo == \"Escalas\"", view);
        Assert.Contains("asp-controller=\"Plantoes\"", view);
        Assert.Contains("asp-controller=\"Escalas\"", view);
        Assert.Contains("asp-action=\"Index\"", view);
        // Sem rota honesta => sem link (lista médica global sem escopo de tenant; agenda por período):
        Assert.DoesNotContain("asp-controller=\"Medicos\"", view);
        Assert.DoesNotContain("asp-controller=\"Agenda\"", view);
    }

    [Fact]
    public void RotasReferenciadasPelosDrills_ExistemNoProjetoWeb()
    {
        foreach (var ctl in new[]
        {
            "Controllers/ClientesController.cs",
            "Controllers/AssinaturasController.cs",
            "Controllers/FaturamentoSaasController.cs",
            "Controllers/EscalasController.cs",
            "Controllers/PlantoesController.cs",
        })
        {
            var src = File.ReadAllText(W(ctl));
            Assert.Contains("<IActionResult> Index(", src);
        }
    }
}
