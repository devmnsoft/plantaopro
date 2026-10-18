using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Operation360.WorkItems;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R18-B4 (achados F7b/F7c do smoke vivo r18b3 — os logs do Api expuseram dois endpoints 500 que o
/// guard recém-liberado deixava visível pela primeira vez):
///   F7b: GET /api/minha-central 500 para todo papel nao-global — WorkItemDto (record) declarava
///        DateTimeOffset onde o reader do Npgsql devolve DateTime para timestamptz; o Dapper nao
///        converte e a materializacao falhava. So o superadmin passava por short-circuit GlobalView
///        (sem ListAsync) e as suítes verdes porque o banco de teste tinha zero work_items — o
///        deserializer e gerado antes da primeira linha.
///   F7c: GET /api/modulos 500 para todo papel — a SRF modulos_efetivos() dentro de any() em WHERE
///        e rejeitada pelo PG18 (0A000); o BFF mostrava o catalogo vazio sem erro visivel. A forma
///        ANY->EXISTS resolvia o parse mas custava ~4min23s (acima do Command Timeout de 60s); a
///        forma final e uma unica passagem por tenant com group by codigo (equivalente count
///        distinct, validado 12/12 contra a versao lenta, ~180ms).
/// </summary>
public sealed class R18MinhaCentralModulosQueryTests
{
    private static readonly string Root = RepositoryPathResolver.RepoRoot;
    private static readonly string Modelos = Path.Combine(Root, "backend", "PlantaoPro.Api", "Operation360", "WorkItems", "WorkItemModels.cs");
    private static readonly string SaasCore = Path.Combine(Root, "backend", "PlantaoPro.Api", "SaasCoreServices.cs");

    [Fact]
    public void Fonte_WorkItemDto_DeclaraDatasComoDateTimeENaoDateTimeOffset()
    {
        var src = File.ReadAllText(Modelos);
        var s = src.IndexOf("record WorkItemDto(", StringComparison.Ordinal);
        var e = src.IndexOf(");", s, StringComparison.Ordinal);
        Assert.True(s >= 0 && e > s, "registro WorkItemDto nao localizado na fonte");
        var registro = src.Substring(s, e - s + 1);

        // O reader devolve DateTime para timestamptz; record com parametro DateTimeOffset quebra a binding.
        Assert.DoesNotContain("DateTimeOffset", registro);
        Assert.Contains("DateTime? VenceEm", registro);
        Assert.Contains("DateTime CriadoEm, DateTime AtualizadoEm", registro);
    }

    [Fact]
    public async Task Db_WorkItemRepositoryListAsync_MaterializaLinhaRealSemFalhaDeBinding()
    {
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid(); // tenant aleatorio: nao conflita com dados de teste existentes

        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync(
            "insert into plantaopro.work_items(id,tenant_id,tipo,titulo,responsavel_id,posicao,vence_em,criado_por) values(@id,@tenantId,'CONVITE_PENDENTE','Item anti-regressao R18',@responsavel,1,now() + interval '1 day',@criadoPor)",
            new { id, tenantId, responsavel = Guid.NewGuid(), criadoPor = Guid.NewGuid() });

        try
        {
            var cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
                .Build();
            var repo = new WorkItemRepository(cfg);

            var rows = await repo.ListAsync(tenantId, null, CancellationToken.None);

            Assert.Single(rows);
            Assert.Equal(id, rows[0].Id); // scoping por tenant_id retornou exatamente a linha semeada
            Assert.Equal("CONVITE_PENDENTE", rows[0].Tipo);
            Assert.Equal("Item anti-regressao R18", rows[0].Titulo);
            Assert.NotNull(rows[0].VenceEm);
            Assert.InRange(rows[0].CriadoEm, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
        }
        finally
        {
            await cn.ExecuteAsync("delete from plantaopro.work_items where id=@id", new { id });
        }
    }

    [Fact]
    public void Fonte_ConsultaDoCatalogo_SemSrfEmWhereEEComPasseUnicaPorTenant()
    {
        var src = File.ReadAllText(SaasCore);

        // Raiz do 0A000: SRF dentro de any() em WHERE (rejeitada pelo PG18).
        Assert.DoesNotContain("any(plantaopro.", src);
        // Forma final: conjunto efetivo calculado uma vez por tenant e agrupado por codigo.
        Assert.Contains("cross join lateral plantaopro.modulos_efetivos(t2.id) e where t2.reg_status='A' group by e.codigo", src);
        Assert.Contains("coalesce(ca.cnt,0)::bigint as \"\"ClientesAtivos\"\"", src);
    }

    [Fact]
    public async Task Db_ConsultaCompletaDoCatalogoExtraidaDaFonteExecutaContraOSchemaReal()
    {
        // Extrai o SQL EXATO do arquivo (nao copia): qualquer derivação da consulta entra no fato.
        var src = File.ReadAllText(SaasCore);
        const string inicio = "select m.id as \"\"Id\"\"";
        const string fim = "order by m.essencial desc, m.ordem, m.nome";
        var s = src.IndexOf(inicio, StringComparison.Ordinal);
        var e = src.IndexOf(fim, s, StringComparison.Ordinal);
        Assert.True(s >= 0 && e > s, "consulta do catálogo nao localizada na fonte");
        // O trecho vive dentro de um string literal @"" na fonte: desempacota as aspas duplas escapadas.
        var sql = src.Substring(s, e - s + fim.Length).Replace("\"\"", "\"");

        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();

        // Mesmo caminho do endpoint: Dapper + binding em SaasModuleDto com @tenantId nulo (superadmin).
        var rows = (await cn.QueryAsync<SaasModuleDto>(new CommandDefinition(sql, new { tenantId = (Guid?)null }))).ToList();

        Assert.NotEmpty(rows);
        // Com contratos de teste ativos no banco, algum modulo deve reportar clientes > 0.
        Assert.Contains(rows, r => r.ClientesAtivos >= 1);
    }
}
