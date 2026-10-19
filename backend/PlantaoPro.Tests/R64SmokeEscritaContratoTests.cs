using System.IO;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R64 (fila r53b1 §7 item 9): smoke de escrita de compras (criar → aprovar →
/// receber) só existe porque há um seed demo idempotente em plantaopro_test.
/// Este contrato trava o contrato do fixture: três linhas fixas (UUIDs
/// determinísticos) do tenant do demo Santa Casa, upsert puro por chave primária,
/// reaplicável sem duplicar, sem apagar nada e sem tocar em outra linha.
/// Sem isso o smoke de escrita deixaria o demo inconsistente a cada execução.
/// </summary>
public sealed class R64SmokeEscritaContratoTests
{
    private const string TenantDemo = "d3f6584c-2c64-4e5a-9ea9-4e1428647502";
    private const string FornecedorUuid = "7c8f0a01-6d4a-4a63-9e11-a1b2c3d4e5f1";
    private const string ProdutoUuid = "7c8f0a02-6d4a-4a63-9e11-a1b2c3d4e5f2";
    private const string LocalUuid = "7c8f0a03-6d4a-4a63-9e11-a1b2c3d4e5f3";

    [Fact]
    public void Seed_Fixture_SmokeEscrita_IdempotenteEPuro()
    {
        var path = Path.Combine(RepositoryPathResolver.RepoRoot,
            "database", "seeds", "2026_r64_smoke_escrita_compras_demo.sql");
        Assert.True(File.Exists(path), "seed 2026_r64_smoke_escrita_compras_demo.sql ausente");
        var sql = File.ReadAllText(path);

        // Três linhas fixas do tenant do demo, com UUIDs determinísticos.
        Assert.Contains(TenantDemo, sql);
        Assert.Contains(FornecedorUuid, sql);
        Assert.Contains(ProdutoUuid, sql);
        Assert.Contains(LocalUuid, sql);

        // Idempotência: upsert por chave primária nas três inserções — reaplicável.
        var upserts = 0;
        var p = sql.IndexOf("ON CONFLICT (id) DO UPDATE");
        while (p >= 0) { upserts++; p = sql.IndexOf("ON CONFLICT (id) DO UPDATE", p + 1); }
        Assert.True(upserts == 3, "exatamente um ON CONFLICT (id) DO UPDATE por linha fixa (encontrados: " + upserts + ")");
        Assert.Contains("INSERT INTO plantaopro.adm360_parceiros", sql);
        Assert.Contains("INSERT INTO plantaopro.adm360_produtos", sql);
        Assert.Contains("INSERT INTO plantaopro.adm360_locais", sql);

        // Pureza: só upsert — nunca apaga nem reescreve esquema de ninguém.
        var low = sql.ToLowerInvariant();
        Assert.False(low.Contains("delete from"), "seed não pode apagar dados");
        Assert.False(low.Contains("truncate"), "seed não pode truncar tabelas");
        Assert.False(low.Contains("drop "), "seed não pode remover objetos");
        Assert.False(low.Contains("update plantaopro"), "seed não pode atualizar fora dos upserts");
        Assert.False(low.Contains("alter table"), "seed não pode alterar esquema");

        // Marcadores que o smoke e a UI reconhecem (não podem virar aleatórios).
        Assert.True(sql.Contains("fornecedor = true"), "o parceiro do smoke deve ser fornecedor");
        Assert.True(sql.Contains("'SMK64'"), "SKU estável do produto do smoke");
        Assert.True(sql.Contains("exige_inspecao = false"), "recebimento direto ao estoque, sem quarentena");
        Assert.True(sql.Contains("'SMK64ALM'"), "código estável do local interno");
        Assert.True(sql.Contains("'INTERNO'"), "local de destino precisa ser INTERNO");
    }
}
