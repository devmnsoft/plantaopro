using System.IO;
using System.Text;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R54 (fila pós-diretriz r24a1 — item 1 do r53b1 §7): paginação no servidor das listas densas.
/// Scanner de fonte que trava: (1) LIMIT/OFFSET dos Orçamentos + clamp da Api, (2) LIMIT/OFFSET e
/// agregados do Estoque + clamp da Api, (3) assinatura pública dos envelopes concretos em Contracts,
/// (4) pager das views preservando filtros, (5) consumidores secundários adaptados ao envelope.
/// Sem paginação, a linha de base R52 era 17,2 MB / ~12–20 s (Orçamentos) e 8,75 MB / ~9–19 s
/// (Estoque) para 9.330 e ~5,7 mil registros respectivamente.
/// Plano: docs/evidencias/2026-10-18-rodada-54/r54a0-paginacao-listas-densas-plano.md.
/// </summary>
public sealed class R54PaginacaoListasDensasContratoTests
{
    private static string P(params string[] partes)
    {
        var caminho = RepositoryPathResolver.RepoRoot;
        foreach (var parte in partes) caminho = Path.Combine(caminho, parte);
        return caminho;
    }

    private static string Ler(params string[] partes) => File.ReadAllText(P(partes), Encoding.UTF8);

    [Fact]
    public void Orcamentos_SqlPaginadaEClampNaApi()
    {
        // D-R54-1/D-R54-3: a variante paginada existe e limita o volume no banco, não na view.
        var repo = Ler("backend", "PlantaoPro.Infrastructure", "Administrativo360", "OrcamentoCirurgicoRepository.cs");
        Assert.Contains("LIMIT @porPagina OFFSET @offset", repo);
        Assert.Contains("SELECT count(*)::bigint", repo);
        Assert.Contains("ListarPaginadoAsync(", repo);
        // D-R54-4: a base é compartilhada entre as variantes integral e paginada (critérios idênticos).
        Assert.Contains("BaseListagemSql", repo);

        var ctl = Ler("backend", "PlantaoPro.Api", "Controllers", "Adm360OrcamentosController.cs");
        Assert.Contains("[FromQuery] int? pagina,", ctl);
        Assert.Contains("[FromQuery] int? porPagina,", ctl);
        Assert.Contains("Math.Max(1, pagina ?? 1)", ctl);
        Assert.Contains("Math.Clamp(porPagina ?? 100, 1, 500)", ctl);
        Assert.Contains("ListarPaginadoAsync(", ctl);
    }

    [Fact]
    public void Estoque_SqlPaginadaAgregadosEClampNaApi()
    {
        // D-R54-1/D-R54-2/D-R54-3: paginação + agregados sobre o conjunto filtrado completo.
        var repo = Ler("backend", "PlantaoPro.Infrastructure", "Administrativo360", "EstoqueRepository.cs");
        Assert.Contains("LIMIT @porPagina OFFSET @offset", repo);
        Assert.Contains("SELECT count(*)::bigint", repo);
        Assert.Contains("COALESCE(SUM(fisico),0) AS TotalFisico", repo);
        Assert.Contains("COALESCE(SUM(reservado),0) AS TotalReservado", repo);
        Assert.Contains("COALESCE(SUM(disponivel),0) AS TotalDisponivel", repo);
        Assert.Contains("ConsultarPaginadoAsync(", repo);
        Assert.Contains("BasePosicaoSql", repo);

        var ctl = Ler("backend", "PlantaoPro.Api", "Controllers", "Adm360SuprimentosControllers.cs");
        Assert.Contains("Math.Max(1,pagina??1)", ctl);
        Assert.Contains("Math.Clamp(porPagina??100,1,500)", ctl);
        Assert.Contains("ConsultarPaginadoAsync(", ctl);
    }

    [Fact]
    public void Contrato_EnvelopePublicoComAssinaturaTravada()
    {
        // D-R54-1: envelopes concretos (não genéricos — o scanner R20 não captura tipos genéricos)
        // com assinatura pública idêntica nos dois lados do wire.
        var contratos = Ler("backend", "PlantaoPro.Application", "Administrativo360", "Contracts.cs");
        Assert.Contains("public sealed record OrcamentoResumoPaginado(IReadOnlyList<OrcamentoResumo> Itens, int Total, int Pagina, int PorPagina);", contratos);
        Assert.Contains("public sealed record SaldoEstoquePaginado(IReadOnlyList<SaldoEstoque> Itens, int Total, int Pagina, int PorPagina, decimal TotalFisico, decimal TotalReservado, decimal TotalDisponivel);", contratos);
        Assert.Contains("Task<OrcamentoResumoPaginado> ListarPaginadoAsync(Guid tenantId, string? busca, string? situacao, DateOnly? inicio, DateOnly? fim, int pagina, int porPagina, CancellationToken ct);", contratos);
        Assert.Contains("Task<SaldoEstoquePaginado> ConsultarPaginadoAsync(Guid tenantId, string? busca, string? condicao, Guid? localId, int pagina, int porPagina, CancellationToken ct);", contratos);
        // Espelho Web com grafia idêntica (ADR-0003).
        var webModels = Ler("backend", "PlantaoPro.Web", "Models", "Administrativo360Models.cs");
        Assert.Contains("public sealed record OrcamentoResumoPaginadoViewModel(IReadOnlyList<OrcamentoResumoViewModel> Itens, int Total, int Pagina, int PorPagina);", webModels);
        Assert.Contains("public sealed record SaldoEstoquePaginadoViewModel(IReadOnlyList<SaldoEstoqueViewModel> Itens, int Total, int Pagina, int PorPagina, decimal TotalFisico, decimal TotalReservado, decimal TotalDisponivel);", webModels);
    }

    [Fact]
    public void Views_PagerPreservaFiltrosAtivos()
    {
        // D-R54-6: contagem "Mostrando A–B de N" + navegação que carrega pagina nos links.
        var orc = Ler("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Orcamentos.cshtml");
        Assert.Contains("<text>Mostrando @inicioExibicao", orc);
        Assert.Contains("asp-route-pagina=\"@(paginaAtual - 1)\"", orc);
        Assert.Contains("asp-route-pagina=\"@(paginaAtual + 1)\"", orc);
        Assert.Contains("aria-label=\"Navegação de páginas\"", orc);

        var est = Ler("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Estoque.cshtml");
        Assert.Contains("<text>Mostrando @inicioExibicao", est);
        Assert.Contains("asp-route-pagina=\"@(Model.Pagina - 1)\"", est);
        Assert.Contains("asp-route-pagina=\"@(Model.Pagina + 1)\"", est);
        Assert.Contains("aria-label=\"Navegação de páginas\"", est);
        // Cards de resumo seguem as somas do conjunto filtrado (não a fatia da página).
        Assert.Contains("@Model.TotalFisico.ToString(\"N0\")", est);
        Assert.Contains("@Model.TotalReservado.ToString(\"N0\")", est);
        Assert.Contains("@Model.TotalDisponivel.ToString(\"N0\")", est);

        // O BFF encaminha pagina/porPagina e publica os metadados para o pager.
        var bffOrc = Ler("backend", "PlantaoPro.Web", "Controllers", "Administrativo360Controller.Orcamentos.cs");
        Assert.Contains("&pagina={paginaEfetiva}&porPagina={porPaginaEfetiva}", bffOrc);
        Assert.Contains("ViewBag.Pagina = paginaEfetiva;", bffOrc);
        Assert.Contains("ViewBag.PorPagina = porPaginaEfetiva;", bffOrc);
    }

    [Fact]
    public void Bff_ConsomidoresSecundariosUsamEnvelope()
    {
        // D-R54-5: seletores de formulário leem .Itens (default da Api: 100 mais recentes);
        // o picker de transferências filtra LIBERADO e limita explicitamente no servidor.
        var lookups = Ler("backend", "PlantaoPro.Web", "Controllers", "Administrativo360Controller.cs");
        Assert.Contains("respOrc.Data?.Itens", lookups);
        var mov = Ler("backend", "PlantaoPro.Web", "Controllers", "Administrativo360Controller.EstoqueValorizacao.cs");
        Assert.Contains("\"api/administrativo360/estoque?condicao=LIBERADO&porPagina=500\"", mov);
    }

    [Fact]
    public void Bff_NaoUsaMaisAMalhaSemPaginacao()
    {
        // Regressão: nenhum call-site BFF pode voltar a ler as duas listas como lista crua.
        foreach (var f in Directory.GetFiles(P("backend", "PlantaoPro.Web", "Controllers"), "*.cs"))
        {
            var t = File.ReadAllText(f, Encoding.UTF8);
            Assert.DoesNotContain("ReadApiResponse<IReadOnlyList<OrcamentoResumoViewModel>>", t);
            Assert.DoesNotContain("ReadApiResponse<IReadOnlyList<SaldoEstoqueViewModel>>", t);
        }
    }
}
