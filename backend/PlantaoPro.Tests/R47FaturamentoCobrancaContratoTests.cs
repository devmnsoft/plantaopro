using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R47 (diretriz r24a1, epic E9 — faturamento/cobrança fluxo completo): o cliente consegue
/// pagar a própria fatura dentro da conta, sem depender de ninguém: a listagem une as duas
/// fontes da assinatura (fatura mensal + pagamentos de cadastro), só a fatura mensal pagável
/// oferece ação, o novo endpoint escopa por dono ANTES de tocar no motor de cobrança (que é
/// idempotente e continua dono único das regras de criação/provedor/eventos) e a tela fala
/// a língua do cliente (status humano, sem código cru). Plano:
/// docs/evidencias/2026-10-18-rodada-47/r47a0-e9-faturamento-cobranca-plano.md.
/// </summary>
public sealed class R47FaturamentoCobrancaContratoTests
{
    private static string Ler(string relativo) => System.IO.File.ReadAllText(System.IO.Path.Combine(RepositoryPathResolver.RepoRoot, relativo), System.Text.Encoding.UTF8);

    [Fact]
    public void Api_PagamentoDoCliente_EscopaPorDonoEDelegaAoMotorDeCobranca()
    {
        var apiController = Ler("backend/PlantaoPro.Api/Controllers/SelfServiceSaasController.cs");
        Assert.Contains("[HttpPost(\"faturas/{faturaId:guid}/cobrar\")]", apiController);

        var servico = Ler("backend/PlantaoPro.Api/SelfServiceServices.cs");
        // Escopo de dono na própria query: a fatura precisa pertencer ao cliente autenticado.
        Assert.Contains("from plantaopro.faturas_saas where id=@faturaId and cliente_id=@clienteId", servico);
        // Delegação ao motor existente (idempotente) — sem cópia da regra de criação.
        Assert.Contains("await _cobranca.CobrarAsync(faturaId, null, ip", servico);
    }

    [Fact]
    public void Api_ListagemDoCliente_UnificaAsDuasFontesDaAssinatura()
    {
        var servico = Ler("backend/PlantaoPro.Api/SelfServiceServices.cs");
        var inicio = servico.IndexOf("FaturasMinhaAssinaturaAsync", StringComparison.Ordinal);
        Assert.True(inicio >= 0, "método de listagem de faturas ausente");
        var regiao = servico.Substring(inicio, 2600);
        // A fatura mensal (cobrável pelo motor) e os pagamentos de cadastro saem juntos,
        // cada um marcado com a própria origem.
        Assert.Contains("union all", regiao, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("from plantaopro.faturas_saas", regiao);
        Assert.Contains("from plantaopro.cadastro_cliente_pagamentos_iniciais", regiao);
        Assert.Contains("'SAAS' as \"\"Origem\"\"", regiao);
        Assert.Contains("'CADASTRO' as \"\"Origem\"\"", regiao);
    }

    [Fact]
    public void Web_RotaDePagamento_FicaSobGuardEAntiforgery()
    {
        var controller = Ler("backend/PlantaoPro.Web/Controllers/MinhaAssinaturaController.cs");
        var inicio = controller.IndexOf("[HttpPost(\"PagarFatura/{faturaId:guid}\")]", StringComparison.Ordinal);
        Assert.True(inicio >= 0, "ação PagarFatura ausente no controller Web");
        var regiao = controller.Substring(inicio, Math.Min(900, controller.Length - inicio));
        Assert.Contains("[ValidateAntiForgeryToken]", regiao);

        var guard = Ler("backend/PlantaoPro.Web/Services/Security/SaasRouteGuardFilter.cs");
        Assert.Contains("[\"MinhaAssinatura/PagarFatura\"] = \"CRIAR\"", guard);
    }

    [Fact]
    public void TelaDeFaturas_LinguagemSimplesEAcaoDePagamento()
    {
        var view = Ler("backend/PlantaoPro.Web/Views/MinhaAssinatura/Faturas.cshtml");
        Assert.Contains("Pagar agora", view);
        Assert.Contains("asp-action=\"PagarFatura\"", view);
        Assert.Contains("Abrir página de pagamento", view);
        // Status humano (o código cru nunca é exibido diretamente).
        Assert.Contains("@f.StatusExibicao", view);
        Assert.DoesNotContain("@f.Status<", view);
    }
}
