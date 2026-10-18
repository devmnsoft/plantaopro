using System.Text;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R27 (diretriz r24a1, epic E1 — "linguagem que qualquer usuário entenda", zona portal):
/// os textos técnicos identificados no scan de jargão (r27a0, 18 pontos em 7 views) foram
/// reescritos para linguagem de usuário. Este fato trava anti-regressão dos literais antigos
/// E presença dos novos (contra remoção acidental). Whitelist (código, não texto renderizado):
/// expressões C# com Guid.Empty, propriedades Endpoint = "/api/..." em Razor, fetch() em JS,
/// atributos data-lookup-endpoint e comentários Razor @* *@. Inventário file:line em
/// docs/evidencias/2026-10-18-rodada-27/r27a0-linguagem-simples-portal-plano.md.
/// </summary>
public class R27LinguagemSimplesPortalContratoTests
{
    private static string ReadRelative(params string[] partes) => File.ReadAllText(
        Path.Combine(new[] { RepositoryPathResolver.RepoRoot }.Concat(partes).ToArray()), Encoding.UTF8);

    [Fact]
    public void ZonaPortal_JargaoTecnicoRemovidoDasViews()
    {
        var plantoesDetails = ReadRelative("backend", "PlantaoPro.Web", "Views", "Plantoes", "Details.cshtml");
        Assert.DoesNotContain("A API do plant", plantoesDetails, StringComparison.Ordinal);

        var plantaoForm = ReadRelative("backend", "PlantaoPro.Web", "Views", "Plantoes", "_PlantaoForm.cshtml");
        Assert.DoesNotContain("validadas pela API", plantaoForm, StringComparison.Ordinal);

        var agenda = ReadRelative("backend", "PlantaoPro.Web", "Views", "Agendamentos", "AgendaPremium.cshtml");
        Assert.DoesNotContain("pela API", agenda, StringComparison.Ordinal);
        Assert.DoesNotContain("Model.Endpoint", agenda, StringComparison.Ordinal);

        var fatura = ReadRelative("backend", "PlantaoPro.Web", "Views", "FaturamentoClinico", "Index.cshtml");
        Assert.DoesNotContain("pela API", fatura, StringComparison.Ordinal);
        Assert.DoesNotContain("por esta API", fatura, StringComparison.Ordinal);

        var perfil = ReadRelative("backend", "PlantaoPro.Web", "Views", "DashboardsPremium", "Perfil.cshtml");
        Assert.DoesNotContain("pela API", perfil, StringComparison.Ordinal);
        Assert.DoesNotContain("KPIs carregados da API", perfil, StringComparison.Ordinal);

        var cobertura = ReadRelative("backend", "PlantaoPro.Web", "Views", "OperacaoPremium", "Cobertura.cshtml");
        Assert.DoesNotContain("BFF", cobertura, StringComparison.Ordinal);

        // Template das páginas de produto (FaturamentoClinico/Títulos etc.): sem endpoint cru nem jargão tenant.
        var produto = ReadRelative("backend", "PlantaoPro.Web", "Views", "V114", "Produto.cshtml");
        Assert.DoesNotContain("Model.Endpoint", produto, StringComparison.Ordinal);
        Assert.DoesNotContain("tenant", produto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ZonaPortal_TextosNovosEmLinguagemSimples_Existem()
    {
        var plantoesDetails = ReadRelative("backend", "PlantaoPro.Web", "Views", "Plantoes", "Details.cshtml");
        Assert.Contains("O fechamento será vinculado quando for gerado no sistema.", plantoesDetails, StringComparison.Ordinal);

        var plantaoForm = ReadRelative("backend", "PlantaoPro.Web", "Views", "Plantoes", "_PlantaoForm.cshtml");
        Assert.Contains("As ações críticas são validadas e registradas para auditoria.", plantaoForm, StringComparison.Ordinal);

        var fatura = ReadRelative("backend", "PlantaoPro.Web", "Views", "FaturamentoClinico", "Index.cshtml");
        Assert.Contains("Você pode filtrar por status, competência e convênio.", fatura, StringComparison.Ordinal);
        Assert.Contains("detalhes completos (paciente, convênio, valores e histórico)", fatura, StringComparison.OrdinalIgnoreCase);

        var perfil = ReadRelative("backend", "PlantaoPro.Web", "Views", "DashboardsPremium", "Perfil.cshtml");
        Assert.Contains("Indicadores do seu perfil, plano e módulos contratados.", perfil, StringComparison.Ordinal);
        Assert.Contains("Nenhum indicador foi encontrado para este contexto.", perfil, StringComparison.Ordinal);

        var cobertura = ReadRelative("backend", "PlantaoPro.Web", "Views", "OperacaoPremium", "Cobertura.cshtml");
        Assert.Contains(">Atualizar cobertura</a>", cobertura, StringComparison.Ordinal);
    }
}
