using System.Text;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R29 (diretriz r24a1, epic E1 — "linguagem que qualquer usuário entenda", demais módulos):
/// 19 pontos em 9 views fora de portal/Adm360 (AdminSaas, Lgpd, Escalas, Configuracoes, Financeiro,
/// Pagamentos, Observabilidade, MinhaAssinatura, Comercial). Padrão "X pela API", endpoints crus em
/// texto, nome de tabela, e o jargão inglês "billing" em exibição → linguagem de usuário. Este fato
/// trava anti-regressão dos literais antigos E presença dos novos (contra remoção acidental).
/// Whitelist (código/comentário, não renderiza): comentários Razor/HTML de rodada, atributo
/// data-lookup-endpoint dos componentes de busca, IDs/atributos #billing e data-billing-* em outras
/// views, landing CommercialDemoWeb (zona de marketing comercial, whitelist desde R25). Inventário
/// file:line em docs/evidencias/2026-10-18-rodada-29/r29a0-linguagem-simples-demais-modulos-plano.md.
/// </summary>
public class R29LinguagemSimplesDemaisModulosContratoTests
{
    private static string ReadRelative(params string[] partes) => File.ReadAllText(
        Path.Combine(new[] { RepositoryPathResolver.RepoRoot }.Concat(partes).ToArray()), Encoding.UTF8);

    [Fact]
    public void DemaisModulos_JargaoTecnicoRemovidoDasViews()
    {
        var adminSaas = ReadRelative("backend", "PlantaoPro.Web", "Views", "AdminSaas", "Index.cshtml");
        Assert.DoesNotContain("pela API", adminSaas, StringComparison.Ordinal);
        Assert.DoesNotContain("billing e contexto", adminSaas, StringComparison.Ordinal);

        var lgpd = ReadRelative("backend", "PlantaoPro.Web", "Views", "Lgpd", "Eventos.cshtml");
        Assert.DoesNotContain("lgpd_eventos_privacidade</code>", lgpd, StringComparison.Ordinal);
        Assert.DoesNotContain("pela API", lgpd, StringComparison.Ordinal);

        var escalas = ReadRelative("backend", "PlantaoPro.Web", "Views", "Escalas", "Index.cshtml");
        Assert.DoesNotContain("pela API", escalas, StringComparison.Ordinal);

        var configuracoes = ReadRelative("backend", "PlantaoPro.Web", "Views", "Configuracoes", "Index.cshtml");
        Assert.DoesNotContain("retornado pela API", configuracoes, StringComparison.Ordinal);

        var financeiro = ReadRelative("backend", "PlantaoPro.Web", "Views", "Financeiro", "Index.cshtml");
        Assert.DoesNotContain("pela API", financeiro, StringComparison.Ordinal);

        var pagamentos = ReadRelative("backend", "PlantaoPro.Web", "Views", "Pagamentos", "Index.cshtml");
        Assert.DoesNotContain("pela API", pagamentos, StringComparison.Ordinal);

        var erros = ReadRelative("backend", "PlantaoPro.Web", "Views", "Observabilidade", "Erros.cshtml");
        Assert.DoesNotContain("pela API/Web", erros, StringComparison.Ordinal);

        // "Billing" com B maiúsculo era exibição (eyebrow); os atributos id="billing-title"/pp-billing-timeline
        // são identificadores de código e seguem whitelist.
        var assinatura = ReadRelative("backend", "PlantaoPro.Web", "Views", "MinhaAssinatura", "Index.cshtml");
        Assert.DoesNotContain("pela API", assinatura, StringComparison.Ordinal);
        Assert.DoesNotContain("Billing", assinatura, StringComparison.Ordinal);

        var previsao = ReadRelative("backend", "PlantaoPro.Web", "Views", "Comercial", "PrevisaoReceita.cshtml");
        Assert.DoesNotContain("pela API", previsao, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/comercial/previsao-receita", previsao, StringComparison.Ordinal);
    }

    [Fact]
    public void DemaisModulos_TextosNovosEmLinguagemSimples_Existem()
    {
        var adminSaas = ReadRelative("backend", "PlantaoPro.Web", "Views", "AdminSaas", "Index.cshtml");
        Assert.Contains("Nenhum módulo encontrado.", adminSaas, StringComparison.Ordinal);
        Assert.Contains("bloqueios, cobranças e contexto assistido.", adminSaas, StringComparison.Ordinal);

        var lgpd = ReadRelative("backend", "PlantaoPro.Web", "Views", "Lgpd", "Eventos.cshtml");
        Assert.Contains("Eventos de privacidade ficam registrados no sistema e podem ser consultados nesta área.", lgpd, StringComparison.Ordinal);

        var escalas = ReadRelative("backend", "PlantaoPro.Web", "Views", "Escalas", "Index.cshtml");
        Assert.Contains("<dd>Não informada</dd>", escalas, StringComparison.Ordinal);

        var financeiro = ReadRelative("backend", "PlantaoPro.Web", "Views", "Financeiro", "Index.cshtml");
        Assert.Contains("Somente valores realizados</small>", financeiro, StringComparison.Ordinal);

        var pagamentos = ReadRelative("backend", "PlantaoPro.Web", "Views", "Pagamentos", "Index.cshtml");
        Assert.Contains("também só aparecem quando existem registros.", pagamentos, StringComparison.Ordinal);

        var erros = ReadRelative("backend", "PlantaoPro.Web", "Views", "Observabilidade", "Erros.cshtml");
        Assert.Contains("Últimas falhas registradas pelo sistema.", erros, StringComparison.Ordinal);

        var assinatura = ReadRelative("backend", "PlantaoPro.Web", "Views", "MinhaAssinatura", "Index.cshtml");
        Assert.Contains("Contrato e cobranças", assinatura, StringComparison.Ordinal);
        Assert.Contains("Consulte somente as informações do contrato da sua organização.", assinatura, StringComparison.Ordinal);
        Assert.Contains("Situação atual", assinatura, StringComparison.Ordinal);
        Assert.Contains("Nenhum módulo habilitado no momento.", assinatura, StringComparison.Ordinal);
        Assert.Contains("Somente eventos registrados na cobrança.", assinatura, StringComparison.Ordinal);

        var previsao = ReadRelative("backend", "PlantaoPro.Web", "Views", "Comercial", "PrevisaoReceita.cshtml");
        Assert.Contains("do seu funil comercial.", previsao, StringComparison.Ordinal);
    }
}
