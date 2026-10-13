using Dapper;
using Npgsql;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B10 — cid_tabela: unicidade canônica VERSIONADA + contrato anti-drift (§29 item 2).
/// Contexto: cid_tabela é um vocabulário de CID versionado (CID-10/CID-11). O importador grava via
/// ON CONFLICT (versao, upper(codigo)) WHERE reg_status='A' AND codigo&lt;&gt;'' — a MESMA código pode
/// existir em várias versões simultaneamente. Havia drift: três índices únicos sobre codigo coexistiam,
/// e os dois NÃO-versionados (ux_cid_tabela_codigo, ux_cid_tabela_codigo_ativo) bloqueavam o mesmo
/// código sob duas versões (23505), contradizendo o modelo versionado. A v2351 remove essas duas
/// unicidades (relaxação monotônica, sem perda de dados) e reafirma a versionada (canônica).
/// Contrato (anti-drift): o índice versionado existe com a definição correta; os não-versionados NÃO
/// existem; e o superset de colunas usadas pelo consumidor está presente. Jornada: o mesmo código em
/// duas versões coexiste (antes falhava) e o upsert EXATO do import atualiza sem conflito.
/// </summary>
public sealed class R8B10CidTabelaUnicaoVersionadaTests
{
    private const string VersaoA = "R8B10-V1";
    private const string VersaoB = "R8B10-V2";
    private const string VersaoUpsert = "R8B10-V3";
    private const string CodCross = "ZTEST-R8B10-CROSS";
    private const string CodUpsert = "ZTEST-R8B10-UPSERT";

    private static string Cs => TestDatabase.ConnectionString;

    // Mesma ordem de colunas do insert real do importador (Saude360ClinicalService ~L578): satisfaz os
    // NOT NULL sem depender de default para as colunas que a produção já insere explicitamente.
    private static readonly string Colunas =
        "versao,codigo,descricao,descricao_normalizada,capitulo_codigo,capitulo_nome,grupo_codigo,grupo_nome,categoria,fonte,fonte_url,created_by";

    private static readonly string InsertSql =
        $@"insert into plantaopro.cid_tabela({Colunas})
           values(@versao,@codigo,'descricao teste R8B10','descricao teste r8b10','','','','','','fonte-teste','x',@uid)";

    // Espelha EXATAMENTE a cláusula on conflict do importador (~L580) — prova que o alvo do upsert
    // (o índice versionado canônico) continua resolvido após a correção.
    private static readonly string UpsertSql =
        $@"insert into plantaopro.cid_tabela({Colunas})
           values(@versao,@codigo,@descricao,lower(@descricao),'','','','','','fonte-teste','x',@uid)
           on conflict (versao, upper(codigo)) where reg_status='A' and codigo <> '' do update set
              descricao=excluded.descricao, descricao_normalizada=excluded.descricao_normalizada,
              capitulo_codigo=excluded.capitulo_codigo, capitulo_nome=excluded.capitulo_nome,
              grupo_codigo=excluded.grupo_codigo, grupo_nome=excluded.grupo_nome,
              categoria=excluded.categoria, fonte=excluded.fonte, fonte_url=excluded.fonte_url,
              reg_update=now(), updated_by=@uid";

    private static async Task<NpgsqlConnection> OpenAsync()
    {
        try { var cn = new NpgsqlConnection(Cs); await cn.OpenAsync(); return cn; }
        catch (Exception ex) { throw new Exception($"PostgreSQL obrigatório para a suíte R8-B10 não está acessível: {ex.Message}", ex); }
    }

    /// <summary>Purge escopado pelas versões de teste (não afeta as 12 linhas de seed CID-10).</summary>
    private static async Task LimparAsync()
    {
        await using var cn = await OpenAsync();
        await cn.ExecuteAsync(
            "delete from plantaopro.cid_tabela where versao in (@a,@b,@u)",
            new { a = VersaoA, b = VersaoB, u = VersaoUpsert });
    }

    [Fact]
    public async Task Contrato_IndiceCanoneVersionado_SemUnicasNaoVersionadas_EColunasPresentes()
    {
        await using var cn = await OpenAsync();

        var indices = (await cn.QueryAsync<string>(
            "select indexname from pg_indexes where schemaname='plantaopro' and tablename='cid_tabela'")).ToList();

        Assert.Contains("ux_cid_tabela_versao_codigo_ativo", indices);
        Assert.DoesNotContain("ux_cid_tabela_codigo", indices);
        Assert.DoesNotContain("ux_cid_tabela_codigo_ativo", indices);

        // Definição canônica: (versao, upper(codigo)) restrito aos ativos com código não vazio.
        var defs = (await cn.QueryAsync<string>(
            "select indexdef from pg_indexes where schemaname='plantaopro' and tablename='cid_tabela' and indexname='ux_cid_tabela_versao_codigo_ativo'")).ToList();
        Assert.True(defs.Count > 0, "índice ux_cid_tabela_versao_codigo_ativo ausente no base de teste");
        Assert.Contains("versao", defs[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("upper(codigo)", defs[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reg_status", defs[0], StringComparison.OrdinalIgnoreCase);

        // Superset de colunas usadas pelo consumidor (o "primeiro if not exists define o schema" já convergiu).
        var cols = (await cn.QueryAsync<string>(
            "select column_name from information_schema.columns where table_schema='plantaopro' and table_name='cid_tabela'"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var c in new[] { "versao", "codigo", "descricao", "descricao_normalizada", "capitulo_codigo",
            "capitulo_nome", "grupo_codigo", "grupo_nome", "categoria", "fonte", "fonte_url" })
            Assert.True(cols.Contains(c), $"coluna canônica ausente de cid_tabela: {c}");
    }

    [Fact]
    public async Task Jornada_MesmoCodigoDuasVersoes_Coexiste_SemConflitoGlobal()
    {
        await LimparAsync();
        try
        {
            await using var cn = await OpenAsync();
            await cn.ExecuteAsync(InsertSql, new { versao = VersaoA, codigo = CodCross, uid = Guid.NewGuid() });
            // Antes da v2351, esta segunda insert falharia com 23505 em ux_cid_tabela_codigo.
            await cn.ExecuteAsync(InsertSql, new { versao = VersaoB, codigo = CodCross, uid = Guid.NewGuid() });

            var n = await cn.ExecuteScalarAsync<int>(
                "select count(1) from plantaopro.cid_tabela where upper(codigo)=upper(@c) and reg_status='A'", new { c = CodCross });
            Assert.Equal(2, n);

            var vs = (await cn.QueryAsync<string>(
                "select distinct versao from plantaopro.cid_tabela where upper(codigo)=upper(@c)", new { c = CodCross }))
                .OrderBy(x => x, StringComparer.Ordinal).ToArray();
            Assert.Equal(new[] { VersaoA, VersaoB }, vs);
        }
        finally { await LimparAsync(); }
    }

    [Fact]
    public async Task Jornada_UpsertImport_MesmaVersaoCodigo_AtualizaSemConflito()
    {
        await LimparAsync();
        try
        {
            await using var cn = await OpenAsync();
            var uid = Guid.NewGuid();
            await cn.ExecuteAsync(UpsertSql, new { versao = VersaoUpsert, codigo = CodUpsert, descricao = "primeira", uid });
            await cn.ExecuteAsync(UpsertSql, new { versao = VersaoUpsert, codigo = CodUpsert, descricao = "segunda-atualizada", uid });

            var n = await cn.ExecuteScalarAsync<int>(
                "select count(1) from plantaopro.cid_tabela where versao=@v and upper(codigo)=upper(@c)", new { v = VersaoUpsert, c = CodUpsert });
            var d = await cn.ExecuteScalarAsync<string>(
                "select descricao from plantaopro.cid_tabela where versao=@v and upper(codigo)=upper(@c)", new { v = VersaoUpsert, c = CodUpsert });

            Assert.Equal(1, n);                       // on conflict não duplicou
            Assert.Equal("segunda-atualizada", d);    // on conflict atualizou via índice versionado
        }
        finally { await LimparAsync(); }
    }
}
