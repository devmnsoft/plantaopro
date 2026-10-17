using System.Net;
using Dapper;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R14-B1 (topo da fila R13, achados r13b2 F13-A/F13-B): com competencia nula (DateOnly?), o
/// Dapper envia o parametro sem tipo (OID 0) e o PostgreSQL falha no parse com 42P08 — regra
/// documentada na propria casa (DateOnlyDapperTypeHandler: "a protecao contra 42P08 vem dos casts
/// nas consultas"). A varredura sistematica (r14a0 F4) encontrou exatamente duas ocorrencias sem
/// cast no repo, ambas em filtros de competencia de faturas_saas: a listagem comercial (500 na
/// tela /FaturamentoSaas/Index com degradacao silenciosa + vazio falso) e o relatorio SaaS de
/// faturamento (chamado pelo Web sempre sem competencia => 500 para todo mundo). Fix validado
/// empiricamente contra o banco real: o cast precisa cobrir as DUAS ocorrencias da clausula
/// ("@p::date is null or col=@p::date") — o walker de resolucao do PG exige tipo ja na ocorrencia
/// IS NULL e um cast so na comparacao posterior nao salva (evidence r14b1 precheck). Sem DDL, sem
/// mudanca no handler, v2149 intacto.
/// </summary>
public sealed class R14FaturasCompetenciaCastFonteContractTests
{
    private static readonly string Root = RepositoryPathResolver.RepoRoot;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void ListagemComercial_TipaOParametroDeCompetenciaNaConvencaoDaCasa_EExtintaAFormaSemCast()
    {
        var src = Read("backend/PlantaoPro.Api/Controllers/SaasCommercialController.cs");

        Assert.Contains("(@competencia::date is null or f.competencia=@competencia::date)", src);
        // A forma sem cast revivia o 42P08 no parse quando competencia era nulo.
        Assert.DoesNotContain("f.competencia=@competencia)", src);
    }

    [Fact]
    public void RelatorioSaaSDeFaturamento_TipaOParametroDeCompetenciaNaConvencaoDaCasa_EExtintaAFormaSemCast()
    {
        var src = Read("backend/PlantaoPro.Api/Controllers/RelatoriosSaasController.cs");

        Assert.Contains("(@competencia::date is null or f.competencia=@competencia::date)", src);
        Assert.DoesNotContain("f.competencia=@competencia)", src);
    }
}

/// <summary>
/// R14-B1 (prova de raiz contra banco real): executa o SQL exato das duas ocorrencias corrigidas
/// com a configuracao que quebrou em producao (escopo nao-global, competencia/status nulos). A
/// forma antiga (sem nenhum cast) continua reproduzindo o 42P08 — se o PostgreSQL resolver esse
/// caso sozinho ou se o cast sumir do SQL, um dos lados do contrato falha de proposito.
/// </summary>
[Collection("saas-operacao-serial")]
public sealed class R14FaturasCompetenciaNullBancoContratoTests
{
    // O processo de testes nao passa pelo Program.cs da Api — registra os type handlers (idempotente)
    // para espelhar o binding de producao: sem o handler, ate o null de DateOnly? vira NotSupportedException.
    static R14FaturasCompetenciaNullBancoContratoTests() => DapperTypeHandlerRegistrar.RegistrarTodos();

    // Espelho exato do where da producao apos o fix (o pareamento com a fonte e coberto pelo scan acima).
    private const string WhereCorrigida = "f.reg_status='A' and (@global or f.cliente_id=@scopedClienteId) and (@requestedClienteId is null or f.cliente_id=@requestedClienteId) and (@status is null or f.status=@status) and (@competencia::date is null or f.competencia=@competencia::date)";
    private const string WhereAntiga = "f.reg_status='A' and (@global or f.cliente_id=@scopedClienteId) and (@requestedClienteId is null or f.cliente_id=@requestedClienteId) and (@status is null or f.status=@status) and (@competencia is null or f.competencia=@competencia)";

    // Mesma forma do args object da acao Faturas (SaasCommercialController) com escopo nao-global e filtros nulos.
    private static object ArgsEscopoNaoGlobal() => new
    {
        global = false,
        scopedClienteId = Guid.NewGuid(),
        requestedClienteId = (Guid?)null,
        status = (string?)null,
        competencia = (DateOnly?)null,
        s = 20,
        offset = 0
    };

    [Fact]
    public async Task ListagemComercial_FormaAntigaSemCast_FalhaCom42P08_EnquantoACorrigidaExecuta()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();

        var falha = await Record.ExceptionAsync(() => cn.ExecuteScalarAsync<long>(
            "select count(1) from plantaopro.faturas_saas f where " + WhereAntiga, ArgsEscopoNaoGlobal()));
        var postgres = Assert.IsType<PostgresException>(falha);
        Assert.Equal("42P08", postgres.SqlState);

        var total = await cn.ExecuteScalarAsync<long>(
            "select count(1) from plantaopro.faturas_saas f where " + WhereCorrigida, ArgsEscopoNaoGlobal());
        Assert.True(total >= 0);
    }

    [Fact]
    public async Task RelatorioSaaSDeFaturamento_ExecutaComCompetenciaNula_NoExatoFormatoDoEndpoint()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();

        // SQL exata da acao Faturamento (pos-fix) + sufixo do wrapper ExecutarRelatorioAsync.
        var sql = @"select f.cliente_id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"",
       coalesce(f.status,'') as ""Status"", 'FATURA' as ""Classificacao"", 1::bigint as ""Quantidade"", f.valor as ""Valor"", f.competencia as ""Competencia""
from plantaopro.faturas_saas f
join plantaopro.clientes c on c.id=f.cliente_id
where f.reg_status='A' and (@status is null or upper(f.status)=upper(@status)) and (@competencia::date is null or f.competencia=@competencia::date)
order by f.competencia desc, f.vencimento desc";

        var parametros = new DynamicParameters(new { competencia = (DateOnly?)null, status = (string?)null });
        parametros.AddDynamicParams(new { s = 50, offset = 0 });

        var linhas = (await cn.QueryAsync(sql + "\nlimit @s offset @offset", parametros)).ToList();
        Assert.True(linhas.Count >= 0);
    }
}

/// <summary>
/// R14-B1/B2 (degradacao honesta, F13-B): com a API paginada em 500, as views exibem o banner do
/// padrao de casa (alert role=alert com a mensagem do envelope) e nao mostram o vazio falso
/// ("Nenhuma fatura encontrada" / "Nenhum dado encontrado.") — lista vazia por falha != vazia
/// real. Os controles positivos garantem que o vazio verdadeiro continua anunciando o estado real
/// (o fix nao suprime o empty-state legitimo). Achado r14b2 (homologacao viva): o caminho de
/// SUCESSO do ReadApiResponse* expoe a mensagem do envelope ("Sucesso") como Error — os stubs de
/// resumo usam o envelope real (com "message":"Sucesso") e o contrato cobra que o controller
/// atribua o erro pelo status da chamada, nunca anunciando sucesso como falha. Colecao web-bff:
/// live-check desligado, decisao pelos claims da sessao (deterministico).
/// </summary>
[Collection("web-bff")]
public sealed class R14DegradacaoHonestaFaturamentoSaasWebTests : IDisposable
{
    private readonly PlantaoProWebFactory _factory;

    public R14DegradacaoHonestaFaturamentoSaasWebTests(PlantaoProWebFactory factory) => _factory = factory;

    public void Dispose() => _factory.ApiStub.Reset();

    [Fact]
    public async Task IndexDeFaturamento_FaturasEm500_MostraBannerEEscondeOVazioFalso()
    {
        _factory.ApiStub.Respond(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("/api/faturamento-saas/resumo"))
                return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"message\":\"Sucesso\",\"data\":{}}");
            if (path.EndsWith("/api/faturamento-saas/faturas"))
                return StubApiHandler.Json(HttpStatusCode.InternalServerError, "{\"message\":\"Não foi possível carregar faturas.\"}");
            return StubApiHandler.Json(HttpStatusCode.OK, "{\"ok\":true}");
        });

        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR"), ("modules", "MEU_DIA,FATURAMENTO_SAAS"), ("permissions", "FATURAMENTO_SAAS.*")));

        var resp = await client.GetAsync("/FaturamentoSaas/Index");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Falha ao carregar.", html);
        // Sufixo sem acentos: o Razor codifica caracteres acentuados como entidades numericas (&#227;) no HTML.
        Assert.Contains("carregar faturas.", html);
        // Vazio falso extinto: a consulta falhou — a pagina nao pode afirmar que nao ha faturas.
        Assert.DoesNotContain("Nenhuma fatura encontrada", html);
    }

    [Fact]
    public async Task IndexDeFaturamento_ListaVaziaReal_MantemOVazioHonestoESemBanner()
    {
        _factory.ApiStub.Respond(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            // Envelope real do resumo em sucesso: traz "message":"Sucesso". Antes do fix r14b2 essa
            // mensagem vazava para o banner ("Falha ao carregar. Sucesso") e suprimia o vazio legitimo.
            if (path.EndsWith("/api/faturamento-saas/resumo"))
                return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"message\":\"Sucesso\",\"data\":{}}");
            if (path.EndsWith("/api/faturamento-saas/faturas"))
                return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":{\"items\":[]}}");
            return StubApiHandler.Json(HttpStatusCode.OK, "{\"ok\":true}");
        });

        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR"), ("modules", "MEU_DIA,FATURAMENTO_SAAS"), ("permissions", "FATURAMENTO_SAAS.*")));

        var resp = await client.GetAsync("/FaturamentoSaas/Index");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await resp.Content.ReadAsStringAsync();
        // Vazio verdadeiro continua anunciando o estado real (sem over-supressao do fix).
        Assert.Contains("Nenhuma fatura encontrada", html);
        Assert.DoesNotContain("Falha ao carregar.", html);
    }

    [Fact]
    public async Task IndexDeFaturamento_ResumoEmFalha_MostraBannerDaInstabilidadeESuprimeOVazio()
    {
        _factory.ApiStub.Respond(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("/api/faturamento-saas/resumo"))
                return StubApiHandler.Json(HttpStatusCode.InternalServerError, "{\"message\":\"Nao foi possivel carregar o resumo comercial.\"}");
            if (path.EndsWith("/api/faturamento-saas/faturas"))
                return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"message\":\"Sucesso\",\"data\":{\"items\":[]}}");
            return StubApiHandler.Json(HttpStatusCode.OK, "{\"ok\":true}");
        });

        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR"), ("modules", "MEU_DIA,FATURAMENTO_SAAS"), ("permissions", "FATURAMENTO_SAAS.*")));

        var resp = await client.GetAsync("/FaturamentoSaas/Index");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Falha ao carregar.", html);
        // O banner carrega a mensagem atribuida ao endpoint que falhou: para 500 em chamada
        // simples, a casa mapeia para o literal fixo de instabilidade (BuildApiErrorMessage L523).
        Assert.Contains("instabilidade interna", html);
        Assert.DoesNotContain("Nenhuma fatura encontrada", html);
    }

    [Fact]
    public async Task RelatorioSaaSDeFaturamento_EndpointEm500_MostraBannerESuprimeOVazioFalso()
    {
        _factory.ApiStub.Respond(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("/api/relatorios/saas/faturamento"))
                return StubApiHandler.Json(HttpStatusCode.InternalServerError, "{\"message\":\"Não foi possível carregar relatório SaaS.\"}");
            return StubApiHandler.Json(HttpStatusCode.OK, "{\"ok\":true}");
        });

        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR"), ("modules", "MEU_DIA,RELATORIOS"), ("permissions", "MEU_DIA.*,RELATORIOS.*")));

        var resp = await client.GetAsync("/Relatorios/SaasFaturamento");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Falha ao carregar.", html);
        // Sufixo sem acentos: "relatório" chega ao HTML com acento codificado como entidade (&#233;).
        Assert.Contains("carregar relat", html);
        Assert.DoesNotContain("Nenhum dado encontrado.", html);
    }

    [Fact]
    public async Task RelatorioSaaSDeFaturamento_LinhasReais_NaoMostraBannerEExibeODado()
    {
        _factory.ApiStub.Respond(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("/api/relatorios/saas/faturamento"))
                return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":{\"items\":[{\"ClienteNome\":\"Cliente R14 Homolog\",\"Status\":\"VENCIDA\",\"Classificacao\":\"FATURA\",\"Quantidade\":1,\"Valor\":100}]}}");
            return StubApiHandler.Json(HttpStatusCode.OK, "{\"ok\":true}");
        });

        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR"), ("modules", "MEU_DIA,RELATORIOS"), ("permissions", "MEU_DIA.*,RELATORIOS.*")));

        var resp = await client.GetAsync("/Relatorios/SaasFaturamento");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Cliente R14 Homolog", html);
        Assert.DoesNotContain("Nenhum dado encontrado.", html);
        Assert.DoesNotContain("Falha ao carregar.", html);
    }
}
