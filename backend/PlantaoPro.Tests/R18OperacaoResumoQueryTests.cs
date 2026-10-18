using Npgsql;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R18-B2 (P2 da fila r17z §5): GET /api/central-escala/resumo caia em 500 (Npgsql 42703:
/// "coluna n.cliente_id não existe") para todo tenant atingível — o contador de notificações do
/// resumo operacional referenciava uma coluna que nunca existiu em plantaopro.notificacoes
/// (schema real: scoping por tenant_id/usuario_id; writer único nao popula tenant_id ainda —
/// r18a0 F6). Fix D5: trocar o scoping quebrado pelo scoping real, mantendo a tolerância a
/// broadcast (is null) e a isenção global-admin. Fatos: (i) fonte — a referência quebrada saiu
/// e o scoping real entrou, com comentário de casa; (ii) comportamento — a subquery EXATAMENTE
/// como está no arquivo executa contra o schema real (revert para n.cliente_id => 42703 de novo).
/// </summary>
public sealed class R18OperacaoResumoQueryTests
{
    private static readonly string Root = RepositoryPathResolver.RepoRoot;
    private static readonly string Servico = Path.Combine(Root, "backend", "PlantaoPro.Api", "OperacaoServices.cs");

    [Fact]
    public void Fonte_ColunaInexistenteDeNotificacoesForaDoResumoOperacional_EComentarioDeCasa()
    {
        var src = File.ReadAllText(Servico);

        // Raiz do 42703: cliente_id nunca foi coluna de notificacoes (coluna real de scoping: tenant_id).
        Assert.DoesNotContain("n.cliente_id", src);
        Assert.Contains("n.tenant_id=@clienteId or n.tenant_id is null", src);
        Assert.Contains("R18 (r18a0 D5)", src);
    }

    [Fact]
    public async Task ContadorDeNotificacoesExtraidoDaFonteExecutaContraOSchemaReal()
    {
        // Extrai a subquery do ARQUIVO (nao copia): qualquer derivação do scoping entra no fato.
        var src = File.ReadAllText(Servico);
        const string inicio = "(select count(1) from plantaopro.notificacoes n";
        const string fim = ") as NotificacoesNaoLidas";
        var s = src.IndexOf(inicio, StringComparison.Ordinal);
        var e = src.IndexOf(fim, s, StringComparison.Ordinal);
        Assert.True(s >= 0 && e > s, "subquery do contador de notificações nao localizada na fonte");
        var subquery = src.Substring(s, e - s + 1); // inclui o parentese final antes de ' as'

        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();

        // Parametrização idêntica a GetResumoAsync (tenantParams: userId/clienteId/isAdminGlobal).
        using var cmd = new NpgsqlCommand("select " + subquery, cn);
        cmd.Parameters.AddWithValue("@isAdminGlobal", false);
        cmd.Parameters.AddWithValue("@clienteId", Guid.NewGuid());
        cmd.Parameters.AddWithValue("@userId", Guid.NewGuid());

        var valor = await cmd.ExecuteScalarAsync();

        Assert.NotNull(valor);
        Assert.True(Convert.ToInt64(valor) >= 0, "contador deve retornar um inteiro não negativo");
    }
}
