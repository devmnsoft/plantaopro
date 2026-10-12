using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Data;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B7 — Glosa por item de lote + recurso rastreável com documentos (matriz §22, fatia §22).
/// Antes: a glosa v115 era registrada apenas por conta/título (sem referência ao item de lote),
/// o recurso não tinha trilha própria (justificativa nem documentos) e
/// v113_faturas.glosa_reconhecida era lida na fórmula do saldo SEM writer — a glosa não afetava o saldo.
/// 1) Glosa agora referencia v116_faturamento_lote_itens.id (validação de tenant) e abate glosa_reconhecida.
/// 2) AdicionarItem persiste lote_id (antes o parâmetro era descartado) e há listagem por lote.
/// 3) Recurso (ABERTA -> EM_RECURSO) persiste justificativa + documentos anexos (v115_recurso_documentos).
/// 4) Resolução aceita valor_recuperado (devolve ao saldo, piso em zero) e grava valor_recuperado.
/// 5) Aceite §30: lote parcial+glosado reconciliável no saldo da conta.
/// </summary>
public sealed class R8B7GlosaItemRecursoTests
{
    private static string Root => RepositoryPathResolver.ResolveRoot();
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    private static IConfiguration Cfg() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
        .Build();

    private static V115GlosaService Glosas(Guid tenant, Guid? userId = null)
    {
        var user = new FakeCurrentUser(tenant, userId);
        return new V115GlosaService(Cfg(), user, new FinancialTenantContext(user), new FakeAuditService(), NullLogger<V115GlosaService>.Instance);
    }

    private static V115FaturamentoRegraService Faturamento(Guid tenant, Guid? userId = null)
    {
        var user = new FakeCurrentUser(tenant, userId);
        return new V115FaturamentoRegraService(Cfg(), new FinancialTenantContext(user), new FakeAuditService(), NullLogger<V115FaturamentoRegraService>.Instance);
    }

    private static V116LoteFaturamentoService Lotes(Guid tenant, Guid? userId = null) =>
        new(Cfg(), new FakeCurrentUser(tenant, userId), new FakeAuditService(), NullLogger<V116LoteFaturamentoService>.Instance);

    // ---------- schema (v2350) ----------

    /// <summary>Advisory lock serializa a aplicação entre classes paralelas; probe idempotente na coluna.</summary>
    private static async Task GarantirSchemaAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r8b7_glosa_item_recurso_schema'));");
        try
        {
            var tem = await cn.ExecuteScalarAsync<bool>("select exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='v115_regras_glosa' and column_name='lote_item_id')");
            if (!tem)
            {
                var sql = Read("database/migrations/2026_10_v2350_r8b7_glosa_item_recurso_documentos.sql");
                for (var tentativa = 1; ; tentativa++)
                {
                    try { await cn.ExecuteAsync(sql); break; }
                    catch (PostgresException ex) when (ex.SqlState == "40P01" && tentativa < 5) { await Task.Delay(150 * tentativa); }
                }
            }
        }
        finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r8b7_glosa_item_recurso_schema'));"); }
    }

    // ---------- seeds ----------

    private static async Task SeedTenantAsync(NpgsqlConnection cn, Guid t) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.clientes(id,tenant_id,nome,status,reg_status) values(@t,@t,'R8B7 Clinica','ATIVO','A') returning id", new { t });

    private static async Task<Guid> SeedContaAsync(NpgsqlConnection cn, Guid t, decimal valor)
    {
        var cid = Guid.NewGuid();
        await cn.ExecuteAsync("insert into plantaopro.v113_faturas(id,cliente_id,tenant_id,reg_status,valor,status) values(@id,@t,@t,'A',@v,'ABERTA')", new { id = cid, t, v = valor });
        return cid;
    }

    private static async Task<Guid> CriarLoteComItemAsync(Guid t, Guid conta, decimal valorItem)
    {
        var lotes = Lotes(t);
        var lote = await lotes.CriarAsync(new V116OperacaoRequest(Nome: "R8B7 Lote", EntidadeId: null, Valor: valorItem, Justificativa: null, Observacao: null));
        if (!lote.Success) throw new Exception("Falha ao criar lote: " + lote.Message);
        var loteId = (Guid)((dynamic)lote.Data!).Id;
        var item = await lotes.AdicionarItemAsync(loteId, new V116OperacaoRequest(Nome: null, EntidadeId: conta, Valor: valorItem, Justificativa: null, Observacao: "R8B7 item"));
        if (!item.Success) throw new Exception("Falha ao adicionar item: " + item.Message);
        return loteId;
    }

    private static async Task<Guid> ItemIdDoLoteAsync(NpgsqlConnection cn, Guid loteId, Guid t) =>
        await cn.ExecuteScalarAsync<Guid>("select id from plantaopro.v116_faturamento_lote_itens where lote_id=@l and tenant_id=@t order by created_at desc limit 1", new { l = loteId, t });

    private static async Task<Guid> GlosaIdDaContaAsync(NpgsqlConnection cn, Guid conta, Guid t) =>
        await cn.ExecuteScalarAsync<Guid>("select id from plantaopro.v115_regras_glosa where conta_receber_id=@c and tenant_id=@t order by created_at desc limit 1", new { c = conta, t });

    private static async Task LimparAsync(Guid t, params Guid[] userIds)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync(@"
delete from plantaopro.v115_recurso_documentos where tenant_id=@t or cliente_id=@t;
delete from plantaopro.v115_regras_glosa where tenant_id=@t or cliente_id=@t;
delete from plantaopro.v115_alertas_operacionais where tenant_id=@t or cliente_id=@t;
delete from plantaopro.v116_faturamento_lote_itens where tenant_id=@t or cliente_id=@t;
delete from plantaopro.v116_faturamento_lotes where tenant_id=@t or cliente_id=@t;
delete from plantaopro.v113_faturas where tenant_id=@t or cliente_id=@t;
delete from plantaopro.clientes where tenant_id=@t or id=@t;", new { t });
        if (userIds.Length > 0)
            await cn.ExecuteAsync("delete from plantaopro.notificacoes where usuario_id=any(@uids)", new { uids = userIds });
    }

    // ---------- 1) glosa por item de lote abate o saldo ----------

    [Fact]
    public async Task Registrar_GlosaComItem_PersisteReferenciaEAbateSaldo()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid(); var conv = Guid.NewGuid();
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var conta = await SeedContaAsync(cn, t, 100m);
        var loteId = await CriarLoteComItemAsync(t, conta, 100m);
        var itemId = await ItemIdDoLoteAsync(cn, loteId, t);
        try
        {
            var r = await Glosas(t, u).RegistrarAsync(new V115GlosaRequest(conta, null, conv, "REGRA_CONVENIO", 30m, null, itemId));
            Assert.True(r.Success, r.Message);
            var refDb = await cn.ExecuteScalarAsync<Guid?>("select lote_item_id from plantaopro.v115_regras_glosa where conta_receber_id=@c and tenant_id=@t", new { c = conta, t });
            Assert.Equal(itemId, refDb);
            var abatida = await cn.ExecuteScalarAsync<decimal>("select glosa_reconhecida from plantaopro.v113_faturas where id=@c", new { c = conta });
            Assert.Equal(30m, abatida);
            var contas = await Faturamento(t, u).ContasReceberAsync();
            var c = Assert.Single(contas.Data, x => x.Id == conta);
            Assert.Equal(70m, c.Saldo);
            Assert.Equal(30m, c.GlosaReconhecida);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Registrar_GlosaSemItem_MantemCompatibilidadeEAbateSaldo()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid(); var conv = Guid.NewGuid();
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var conta = await SeedContaAsync(cn, t, 100m);
        try
        {
            var r = await Glosas(t, u).RegistrarAsync(new V115GlosaRequest(conta, null, conv, "REGRA_CONVENIO", 20m, null, null));
            Assert.True(r.Success, r.Message);
            var refDb = await cn.ExecuteScalarAsync<Guid?>("select lote_item_id from plantaopro.v115_regras_glosa where conta_receber_id=@c and tenant_id=@t", new { c = conta, t });
            Assert.Null(refDb);
            var abatida = await cn.ExecuteScalarAsync<decimal>("select glosa_reconhecida from plantaopro.v113_faturas where id=@c", new { c = conta });
            Assert.Equal(20m, abatida);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Registrar_GlosaItemOutroTenant_404()
    {
        var tA = Guid.NewGuid(); var tB = Guid.NewGuid(); var u = Guid.NewGuid(); var conv = Guid.NewGuid();
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, tA);
        await SeedTenantAsync(cn, tB);
        var contaA = await SeedContaAsync(cn, tA, 100m);
        var loteB = await CriarLoteComItemAsync(tB, contaA, 50m); // item pertence ao tenant B
        var itemB = await ItemIdDoLoteAsync(cn, loteB, tB);
        try
        {
            var r = await Glosas(tA, u).RegistrarAsync(new V115GlosaRequest(contaA, null, conv, "REGRA_CONVENIO", 10m, null, itemB));
            Assert.False(r.Success);
            Assert.Equal(404, r.StatusCode);
            Assert.Contains("Item de lote", r.Message);
        }
        finally { await LimparAsync(tA, u); await LimparAsync(tB, u); }
    }

    [Fact]
    public async Task Registrar_GlosaExcedeValorElegivel_422()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid(); var conv = Guid.NewGuid();
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var conta = await SeedContaAsync(cn, t, 100m);
        try
        {
            var r = await Glosas(t, u).RegistrarAsync(new V115GlosaRequest(conta, null, conv, "REGRA_CONVENIO", 150m, null, null));
            Assert.False(r.Success);
            Assert.Equal(422, r.StatusCode);
            var abatida = await cn.ExecuteScalarAsync<decimal>("select coalesce(glosa_reconhecida,0) from plantaopro.v113_faturas where id=@c", new { c = conta });
            Assert.Equal(0m, abatida);
        }
        finally { await LimparAsync(t, u); }
    }

    // ---------- 2) recurso rastreável com documentos ----------

    [Fact]
    public async Task Recorrer_PersisteJustificativaEDocumentos_StatusEmRecurso()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid(); var conv = Guid.NewGuid();
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var conta = await SeedContaAsync(cn, t, 100m);
        var reg = await Glosas(t, u).RegistrarAsync(new V115GlosaRequest(conta, null, conv, "REGRA_CONVENIO", 30m, null, null));
        Assert.True(reg.Success, reg.Message);
        try
        {
            var glosaId = await GlosaIdDaContaAsync(cn, conta, t);
            var r = await Glosas(t, u).RecorrerAsync(glosaId, new V115RecursoRequest(
                "Documento comprovado; reenviar para análise.",
                new[]
                {
                    new V115RecursoDocumentoRequest("guia.pdf", "application/pdf", "QUJD"),
                    new V115RecursoDocumentoRequest("recibo.txt", "text/plain", "REVklERFA")
                }));
            Assert.True(r.Success, r.Message);

            var db = await cn.QueryFirstAsync<(string Status, string Just, DateTime Em)>(
                "select status as Status, recurso_justificativa as Just, recurso_em as Em from plantaopro.v115_regras_glosa where id=@id", new { id = glosaId });
            Assert.Equal("EM_RECURSO", db.Status);
            Assert.Equal("Documento comprovado; reenviar para análise.", db.Just);
            Assert.True(db.Em != DateTime.MinValue);

            var docs = await Glosas(t, u).DocumentosAsync(glosaId);
            Assert.True(docs.Success, docs.Message);
            Assert.Equal(2, docs.Data.Count());
            Assert.Contains("QUJD", docs.Data.Select(x => x.ConteudoBase64));
            Assert.Contains("guia.pdf", docs.Data.Select(x => x.NomeArquivo));
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Recorrer_SemJustificativa_400()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid(); var conv = Guid.NewGuid();
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var conta = await SeedContaAsync(cn, t, 100m);
        await Glosas(t, u).RegistrarAsync(new V115GlosaRequest(conta, null, conv, "REGRA_CONVENIO", 30m, null, null));
        try
        {
            var glosaId = await GlosaIdDaContaAsync(cn, conta, t);
            var r = await Glosas(t, u).RecorrerAsync(glosaId, new V115RecursoRequest("   ", Array.Empty<V115RecursoDocumentoRequest>()));
            Assert.False(r.Success);
            Assert.Equal(400, r.StatusCode);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Recorrer_GlosaNaoAberta_409()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid(); var conv = Guid.NewGuid();
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var conta = await SeedContaAsync(cn, t, 100m);
        await Glosas(t, u).RegistrarAsync(new V115GlosaRequest(conta, null, conv, "REGRA_CONVENIO", 30m, null, null));
        try
        {
            var glosaId = await GlosaIdDaContaAsync(cn, conta, t);
            var primeira = await Glosas(t, u).RecorrerAsync(glosaId, new V115RecursoRequest("Primeiro recurso.", null));
            Assert.True(primeira.Success, primeira.Message);
            var segunda = await Glosas(t, u).RecorrerAsync(glosaId, new V115RecursoRequest("Segundo recurso.", null));
            Assert.False(segunda.Success);
            Assert.Equal(409, segunda.StatusCode);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Recorrer_Concorrente_DoisOperadores_SomenteUmEfetivadoESemDocumentoDuplicado()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid(); var conv = Guid.NewGuid();
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var conta = await SeedContaAsync(cn, t, 100m);
        await Glosas(t, u).RegistrarAsync(new V115GlosaRequest(conta, null, conv, "REGRA_CONVENIO", 30m, null, null));
        try
        {
            var glosaId = await GlosaIdDaContaAsync(cn, conta, t);
            var corrida = await Task.WhenAll(
                Glosas(t, u).RecorrerAsync(glosaId, new V115RecursoRequest("recurso A.", new[] { new V115RecursoDocumentoRequest("a.pdf", "application/pdf", "QUJD") })),
                Glosas(t, u).RecorrerAsync(glosaId, new V115RecursoRequest("recurso B.", new[] { new V115RecursoDocumentoRequest("b.pdf", "application/pdf", "REVklERFA") })));
            var r1 = corrida[0]; var r2 = corrida[1];
            Assert.True(r1.Success ^ r2.Success, $"Esperado exatamente um recurso efetivado; r1={r1.Message}/{r1.StatusCode}; r2={r2.Message}/{r2.StatusCode}");

            var status = await cn.ExecuteScalarAsync<string>("select status from plantaopro.v115_regras_glosa where id=@id", new { id = glosaId });
            Assert.Equal("EM_RECURSO", status);
            var docs = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.v115_recurso_documentos where glosa_id=@id and reg_status='A'", new { id = glosaId });
            Assert.True(docs == 1, $"Esperado exatamente 1 documento persistido; obtido {docs}");
        }
        finally { await LimparAsync(t, u); }
    }

    // ---------- 3) resolucao com valor recuperado devolve ao saldo ----------

    [Fact]
    public async Task Resolver_ValorRecuperadoTotal_DevolveSaldoAoNormal()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid(); var conv = Guid.NewGuid();
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var conta = await SeedContaAsync(cn, t, 100m);
        await Glosas(t, u).RegistrarAsync(new V115GlosaRequest(conta, null, conv, "REGRA_CONVENIO", 30m, null, null));
        try
        {
            var glosaId = await GlosaIdDaContaAsync(cn, conta, t);
            var r = await Glosas(t, u).ResolverAsync(glosaId, new V115ResolucaoGlosaRequest("Convênio devolveu.", 30m));
            Assert.True(r.Success, r.Message);
            var restabelecida = await cn.ExecuteScalarAsync<decimal>("select glosa_reconhecida from plantaopro.v113_faturas where id=@c", new { c = conta });
            Assert.Equal(0m, restabelecida);
            var rec = await cn.ExecuteScalarAsync<decimal>("select valor_recuperado from plantaopro.v115_regras_glosa where id=@id", new { id = glosaId });
            Assert.Equal(30m, rec);
            var contas = await Faturamento(t, u).ContasReceberAsync();
            Assert.Equal(100m, Assert.Single(contas.Data, x => x.Id == conta).Saldo);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Resolver_ValorRecuperadoParcial_MantemGlosaParcialNoSaldo()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid(); var conv = Guid.NewGuid();
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var conta = await SeedContaAsync(cn, t, 100m);
        await Glosas(t, u).RegistrarAsync(new V115GlosaRequest(conta, null, conv, "REGRA_CONVENIO", 40m, null, null));
        try
        {
            var glosaId = await GlosaIdDaContaAsync(cn, conta, t);
            var r = await Glosas(t, u).ResolverAsync(glosaId, new V115ResolucaoGlosaRequest("Parcialmente reprovado.", 15m));
            Assert.True(r.Success, r.Message);
            var restante = await cn.ExecuteScalarAsync<decimal>("select glosa_reconhecida from plantaopro.v113_faturas where id=@c", new { c = conta });
            Assert.Equal(25m, restante);
            var contas = await Faturamento(t, u).ContasReceberAsync();
            Assert.Equal(75m, Assert.Single(contas.Data, x => x.Id == conta).Saldo);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Resolver_ValorRecuperadoAcimaDoGlosado_422()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid(); var conv = Guid.NewGuid();
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var conta = await SeedContaAsync(cn, t, 100m);
        await Glosas(t, u).RegistrarAsync(new V115GlosaRequest(conta, null, conv, "REGRA_CONVENIO", 30m, null, null));
        try
        {
            var glosaId = await GlosaIdDaContaAsync(cn, conta, t);
            var r = await Glosas(t, u).ResolverAsync(glosaId, new V115ResolucaoGlosaRequest("excesso.", 31m));
            Assert.False(r.Success);
            Assert.Equal(422, r.StatusCode);
            var status = await cn.ExecuteScalarAsync<string>("select status from plantaopro.v115_regras_glosa where id=@id", new { id = glosaId });
            Assert.Equal("ABERTA", status);
        }
        finally { await LimparAsync(t, u); }
    }

    // ---------- 4) aceite §30: lote parcial + glosado reconciliável ----------

    [Fact]
    public async Task Jornada_LoteParcialGlosado_ReconciliaSaldoDaConta()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid(); var conv = Guid.NewGuid();
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var conta = await SeedContaAsync(cn, t, 100m);
        var loteId = await CriarLoteComItemAsync(t, conta, 40m);
        var item = await ItemIdDoLoteAsync(cn, loteId, t);
        try
        {
            // glosa parcial sobre o item do lote (40 dos 100 faturados)
            var reg = await Glosas(t, u).RegistrarAsync(new V115GlosaRequest(conta, null, conv, "GLOSA_ITEM", 40m, null, item));
            Assert.True(reg.Success, reg.Message);

            var midia = await Faturamento(t, u).ContasReceberAsync();
            Assert.Equal(60m, Assert.Single(midia.Data, x => x.Id == conta).Saldo);

            var glosaId = await GlosaIdDaContaAsync(cn, conta, t);
            var rec = await Glosas(t, u).RecorrerAsync(glosaId, new V115RecursoRequest("Pedido de reanálise com anexos.", new[] { new V115RecursoDocumentoRequest("comprovante.pdf", "application/pdf", "QUJDREZFRA==") }));
            Assert.True(rec.Success, rec.Message);

            var resol = await Glosas(t, u).ResolverAsync(glosaId, new V115ResolucaoGlosaRequest("Recuperado parcial no recurso.", 25m));
            Assert.True(resol.Success, resol.Message);

            // reconciliação exata: valor - glosa + recuperado = 100 - 40 + 25 = 85
            var final = await Faturamento(t, u).ContasReceberAsync();
            var c = Assert.Single(final.Data, x => x.Id == conta);
            Assert.Equal(85m, c.Saldo);
            Assert.Equal(15m, c.GlosaReconhecida);

            // os itens do lote são rastreáveis por lote e o documento do recurso está disponível
            var itens = await Lotes(t, u).ListarItensAsync(loteId);
            Assert.True(itens.Success, itens.Message);
            Assert.Equal(1, itens.Data.Count());
            var docs = await Glosas(t, u).DocumentosAsync(glosaId);
            Assert.True(docs.Success, docs.Message);
            Assert.Equal("comprovante.pdf", Assert.Single(docs.Data).NomeArquivo);
        }
        finally { await LimparAsync(t, u); }
    }

    // ---------- 5) contrato: migration x serviço x endpoint coesos ----------

    [Fact]
    public void MigracaoV2350_MaterializaColunasETabelaDoCicloDeGlosaPorItem()
    {
        var sql = Read("database/migrations/2026_10_v2350_r8b7_glosa_item_recurso_documentos.sql");
        foreach (var col in new[] { "lote_item_id", "recurso_justificativa", "recurso_em" })
            Assert.Contains($"add column if not exists {col}", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("add column if not exists lote_id", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("create table if not exists plantaopro.v115_recurso_documentos", sql, StringComparison.OrdinalIgnoreCase);
        foreach (var ix in new[] { "ix_v115_glosa_lote_item", "ix_v116_lote_itens_tenant_lote", "ix_v115_recurso_documentos_glosa" })
            Assert.Contains(ix, sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ServicoEEndpointCoerentes_ComOscrituraDoSaldoEComARotaDoRecurso()
    {
        var serv = Read("backend/PlantaoPro.Api/V115GlosaService.cs");
        Assert.Contains("glosa_reconhecida=glosa_reconhecida+", serv, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("greatest(0,glosa_reconhecida-", serv, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EM_RECURSO", serv, StringComparison.Ordinal);
        Assert.Contains("lote_item_id", serv, StringComparison.OrdinalIgnoreCase);

        var ctrl = Read("backend/PlantaoPro.Api/Controllers/V115FaturamentoController.cs");
        Assert.Contains("glosas/{id:guid}/recorrer", ctrl);
        Assert.Contains("glosas/{id:guid}/documentos", ctrl);
        Assert.Contains("V115ResolucaoGlosaRequest", ctrl);
    }

    // ---------- fakes ----------

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        private readonly Guid _userId;
        private readonly Guid? _tenantId;
        public FakeCurrentUser(Guid? tenantId = null, Guid? userId = null)
        {
            _tenantId = tenantId;
            _userId = userId ?? Guid.Empty;
        }
        public bool GlobalAdmin => false;
        public Guid? UserId => _userId == Guid.Empty ? null : _userId;
        public Guid? TenantId => _tenantId;
        public Guid? ClienteId => _tenantId;
        public Guid? SessionId => null;
        public IReadOnlyCollection<string> Roles => Array.Empty<string>();
        public bool IsAuthenticated() => true;
        public bool IsGlobalAdmin() => false;
        public bool IsTenantAdmin() => true;
        public bool IsPartner() => false;
        public bool IsDoctor() => false;
        public bool HasRole(string role) => false;
    }

    private sealed class FakeAuditService : IAuditService
    {
        public Task RegistrarAsync(Guid? usuarioId, Guid? clienteId, string entidade, Guid? entidadeId, string acao, object? detalhes, bool sucesso, string? ipOrigem, string? perfil, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task LogAsync(Guid? userId, string acao, string entidade, Guid? registroId, string descricao, string? valorAnterior = null, string? valorNovo = null, string? ip = null, string? userAgent = null)
            => Task.CompletedTask;
    }
}
