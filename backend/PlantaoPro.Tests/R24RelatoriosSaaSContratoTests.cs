using Dapper;
using Npgsql;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R24-B1 (F-R14-B, fila r14z): o wrapper de paginação dos relatórios SaaS devolvia
/// total = itens da página (LongCount) na listagem E na exportação — a UI sempre mostrava 1 página.
/// Decisão D-R24-2 (r24a0): contagem real via subquery de count compartilhada (ExecutarPaginadoAsync)
/// nos dois caminhos, com a query de itens e o sufixo "limit @s offset @offset" INALTERADOS
/// (contrato já versionado em R14FaturasCompetenciaNullContratoTests, que permanece verde).
/// Parte 2 do F-R14-B (scoping de tenant do RelatoriosSaasController): D-R24-3 mantém GLOBAL por design —
/// backoffice da operadora SaaS restrito a papéis de plataforma/operacional; o atributo [Authorize] fica
/// travado aqui. A visibilidade de tenant mais fina é tema do epic E2 da diretriz r24a1 (R38/R39).
/// </summary>
public class R24RelatoriosSaaSContratoTests
{
    private static string ControllerSource() => File.ReadAllText(
        Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Api", "Controllers", "RelatoriosSaasController.cs"));

    [Fact]
    public void WrapperPaginado_RelatoriosSaaS_UsaContagemRealENaoContagemDaPagina()
    {
        // Regressão exata do F-R14-B: total volta a ser o tamanho da página em memoria.
        var src = ControllerSource();
        Assert.Contains("count(*)::bigint", src, StringComparison.Ordinal);
        Assert.Contains(") as __contagem", src, StringComparison.Ordinal);
        Assert.DoesNotContain("LongCount", src, StringComparison.Ordinal);
        // Sufixo de paginação preservado (contrato R14).
        Assert.Contains("limit @s offset @offset", src, StringComparison.Ordinal);
    }

    [Fact]
    public void RelatoriosSaaS_AutorizacaoDoBackofficeGlobal_FicaTravada()
    {
        // D-R24-3: endpoint global por design, restrito a papéis de plataforma/operacional —
        // nenhum papel genérico de tenant (ex.: AdministradorCliente, Medico) pode entrar.
        var linha = ControllerSource().Split('\n').First(l => l.Contains("[Authorize(Roles", StringComparison.Ordinal));
        Assert.Contains("AdministradorGlobal", linha, StringComparison.Ordinal);
        Assert.Contains("Financeiro", linha, StringComparison.Ordinal);
        Assert.DoesNotContain("AdministradorCliente", linha, StringComparison.Ordinal);
        Assert.DoesNotContain("Medico", linha, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RelatorioSaaSDeFaturamento_ContagemRealEPaginas_SeedDeterministicoDe125Faturas()
    {
        // Prova de comportamento contra o banco real no formato EXATO do wrapper pós-D-R24-2:
        // count subquery para o total + query original limit/offset para os itens.
        // Antes do fix, o total real (125) era inalcançável pelo wrapper (contagem <= pageSize).
        // A partial index ux_faturas_saas_cliente_competencia_ativa exige 1 fatura ativa por
        // cliente/competencia: o seed usa 125 clientes de teste, cada um com 1 fatura na
        // competencia exclusiva 2099-01-15 (fora de qualquer dado de homologo/desenvolvimento).
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await cn.ExecuteAsync("delete from plantaopro.faturas_saas where competencia='2099-01-15'");
            await cn.ExecuteAsync("delete from plantaopro.clientes where codigo like 'R24B1L-%'");
            await cn.ExecuteAsync(@"insert into plantaopro.clientes (codigo, nome, status, razao_social, nome_fantasia, cnpj, reg_status)
select 'R24B1L-'||lpad(i::text,3,'0'),'CLIENTE R24B1 PAGINACAO '||i,'ATIVO','CLIENTE R24B1 '||i,'CLIENTE R24B1 '||i,'00.0'||lpad(i::text,7,'0')||'/0001-00','A' from generate_series(1,125) i");
            await cn.ExecuteAsync(@"insert into plantaopro.faturas_saas (cliente_id, codigo, nome, status, valor_total, valor, vencimento, competencia, reg_status)
select c.id,'F-R24B1-'||c.codigo,'FATURA R24B1 '||c.codigo,'VENCIDA',10,10,'2099-02-01','2099-01-15','A' from plantaopro.clientes c where c.codigo like 'R24B1L-%'");

            // SQL exata da ação Faturamento do controller (mesma que o contrato R14 executa).
            var sql = @"select f.cliente_id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"",
       coalesce(f.status,'') as ""Status"", 'FATURA' as ""Classificacao"", 1::bigint as ""Quantidade"", f.valor as ""Valor"", f.competencia as ""Competencia""
from plantaopro.faturas_saas f
join plantaopro.clientes c on c.id=f.cliente_id
where f.reg_status='A' and (@status is null or upper(f.status)=upper(@status)) and (@competencia::date is null or f.competencia=@competencia::date)
order by f.competencia desc, f.vencimento desc";

            // @competencia como string de proposito: o cast '@competencia::date' da SQL ja converte e o
            // handler DateOnlyDapperTypeHandler so esta registrado no projeto Api (convencao da casa).
            var total = await cn.ExecuteScalarAsync<long>("select count(*)::bigint from (" + sql + ") as __contagem",
                new { competencia = "2099-01-15", status = (string?)null });
            Assert.Equal(125, total);

            var p1 = new DynamicParameters(new { s = 50, offset = 0 });
            p1.AddDynamicParams(new { competencia = "2099-01-15", status = (string?)null });
            var pagina1 = (await cn.QueryAsync<LinhaRelatorio>(sql + "\nlimit @s offset @offset", p1)).ToList();
            Assert.Equal(50, pagina1.Count);

            var p3 = new DynamicParameters(new { s = 50, offset = 100 });
            p3.AddDynamicParams(new { competencia = "2099-01-15", status = (string?)null });
            var pagina3 = (await cn.QueryAsync<LinhaRelatorio>(sql + "\nlimit @s offset @offset", p3)).ToList();
            Assert.Equal(25, pagina3.Count);
        }
        finally
        {
            await cn.ExecuteAsync("delete from plantaopro.faturas_saas where competencia='2099-01-15'");
            await cn.ExecuteAsync("delete from plantaopro.clientes where codigo like 'R24B1L-%'");
        }
    }

    private sealed class LinhaRelatorio
    {
        public Guid ClienteId { get; set; }
        public string ClienteNome { get; set; } = "";
        public string Status { get; set; } = "";
        public string Classificacao { get; set; } = "";
        public long Quantidade { get; set; }
        public decimal Valor { get; set; }
        public DateOnly? Competencia { get; set; }
    }
}
