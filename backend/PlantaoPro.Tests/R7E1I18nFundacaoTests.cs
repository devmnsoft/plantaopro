using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Xml;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using PlantaoPro.CrossCutting.Localization;
using PlantaoPro.Api.Security;
using PlantaoPro.Tests.Infrastructure;
using PlantaoPro.Web.Services.Localization;
using Xunit;

namespace PlantaoPro.Tests;

// ============================================================================
// R7-E1 — Fundação i18n da interface (pt-BR padrão / en / es / fr).
// 1) Catálogo canônico de idiomas + precedência (usuário > organização > navegador > pt-BR)
//    em UiLanguageCatalog — função pura, testada sem host.
// 2) Provider de cultura: CurrentCulture SEMPRE pt-BR (moeda/data/fiscal independentes do
//    idioma); só a UICulture varia (recursos localizados).
// 3) Recursos localizados: paridade das 4 linguagens (nenhuma chave vazia → nunca expor chave);
//    <html lang> resolvido por request, sem hardcode restante nas views.
// 4) Provas comportamentais Web (stub determinístico): anônimo via Accept-Language; claims de
//    sessão (usuário/organização) vencem o navegador; login com payload de idioma emite as
//    claims e renderiza; POST /Usuario/SalvarIdioma persiste via API, reemite a sessão no
//    caminho canônico e o efeito vale no PRÓXIMO REQUEST sem re-login.
// 5) Persistência real (API + PostgreSQL de teste): column v2344 gravada pelo endpoint,
//    lida de volta no login e no refresh-context.
// ============================================================================

/// <summary>Unidades puras: catálogo, precedência e provider de cultura (sem host).</summary>
public sealed class R7E1I18nCatalogoEProviderTests
{
    [Theory]
    // (usuario, organizacao, accept-language) -> esperado
    [InlineData("fr", "en", "es", "fr")]                // usuário vence tudo
    [InlineData("FR-FR", null, "de", "fr")]             // variante regional do usuário normaliza
    [InlineData(null, "ES-MX", "fr", "es")]             // organização vence navegador
    [InlineData(null, null, "es;q=0.8, fr;q=0.9, en;q=0.7", "fr")] // navegador por q-value
    [InlineData(null, null, "de;q=0.5, en;q=0.5", "en")]           // empate q preserva ordem do header
    [InlineData(null, "zz", "de-DE,de", "pt-BR")]       // fonte inválida cai para a próxima; nada → pt-BR
    [InlineData("", null, null, "pt-BR")]               // tudo ausente → pt-BR
    [InlineData(null, null, "es;q=0", "pt-BR")]         // q=0 descarta o item
    public void Resolve_AplicaPrecedenciaCanonica(string? usuario, string? org, string? accept, string esperado)
        => Assert.Equal(esperado, UiLanguageCatalog.Resolve(usuario, org, accept));

    [Theory]
    [InlineData("EN-us", "en")]
    [InlineData("PT", "pt-BR")]
    [InlineData("pt-br", "pt-BR")]
    [InlineData("french-like", null)]
    [InlineData(null, null)]
    [InlineData("   ", null)]
    public void Normalize_AceitaVariantesRegionaisERejeitaDesconhecidas(string? valor, string? esperado)
        => Assert.Equal(esperado, UiLanguageCatalog.Normalize(valor));

    [Fact]
    public void Catalogo_TemExatamenteQuatroIdiomasComPadraoEPtBrDeFormatacao()
    {
        Assert.Equal(new[] { "pt-BR", "en", "es", "fr" }, UiLanguageCatalog.Supported.ToArray());
        Assert.Equal("pt-BR", UiLanguageCatalog.Default);
        Assert.Equal("pt-BR", UiLanguageCatalog.FormatCultureName);
    }

    private static DefaultHttpContext Ctx(string? idioma = null, string? orgIdioma = null, string? acceptLanguage = null)
    {
        var ctx = new DefaultHttpContext();
        var claims = new List<Claim>();
        if (idioma != null) claims.Add(new Claim("idioma", idioma));
        if (orgIdioma != null) claims.Add(new Claim("org_idioma", orgIdioma));
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims.Count > 0 ? claims : null!, "test"));
        if (acceptLanguage != null) ctx.Request.Headers.AcceptLanguage = acceptLanguage;
        return ctx;
    }

    [Fact]
    public async Task Provider_FormatacaoSemprePtBrMesmoComOutraUICulture()
    {
        var r = await new UiLanguageCultureProvider().DetermineProviderCultureResult(Ctx(idioma: "fr", acceptLanguage: "de"));
        Assert.NotNull(r);
        Assert.Equal("pt-BR", r!.Cultures[0].ToString());     // moeda/data/fiscal inalteradas
        Assert.Equal("fr", r.UICultures[0].ToString());       // só a UI muda
    }

    [Fact]
    public async Task Provider_AnonimoSemNenhumaFonte_PtBrPorPadrao()
    {
        var r = await new UiLanguageCultureProvider().DetermineProviderCultureResult(Ctx());
        Assert.NotNull(r);
        Assert.Equal("pt-BR", r!.Cultures[0].ToString());
        Assert.Equal("pt-BR", r.UICultures[0].ToString());
    }
}

/// <summary>Contratos de fonte: paridade dos 4 arquivos .resx e ausência de hardcode de idioma.</summary>
public sealed class R7E1I18nContratoFontesTests
{
    private static string Root => RepositoryPathResolver.ResolveRoot();
    private static string ViewsDir => Path.Combine(Root, "backend", "PlantaoPro.Web", "Views");

    private static Dictionary<string, string> LerChavesResx(string arquivo)
    {
        var doc = new XmlDocument();
        doc.Load(Path.Combine(Root, "backend", "PlantaoPro.Web", "Localization", arquivo));
        var resultado = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (XmlNode node in doc.SelectNodes("//data"))
        {
            var nome = node.Attributes?["name"]?.Value ?? throw new Xunit.Sdk.XunitException($"<data> sem name em {arquivo}");
            var valor = node.SelectSingleNode("value")?.InnerText ?? string.Empty;
            resultado[nome] = valor;
        }
        return resultado;
    }

    [Fact]
    public void RecursosParidadeTotalEntreOsQuatroIdiomas()
    {
        var master = LerChavesResx("UiTexts.resx");
        Assert.True(master.Count >= 50, $"Esperava o conjunto-base de chaves (>=50); achei {master.Count}.");

        foreach (var satellite in new[] { "UiTexts.en.resx", "UiTexts.es.resx", "UiTexts.fr.resx" })
        {
            var trad = LerChavesResx(satellite);
            var faltando = master.Keys.Except(trad.Keys).ToArray();
            var sobrando = trad.Keys.Except(master.Keys).ToArray();
            Assert.True(faltando.Length == 0, $"{satellite} FALTA(m): {string.Join(",", faltando)}");
            Assert.True(sobrando.Length == 0, $"{satellite} TEM A MAIS: {string.Join(",", sobrando)}");
            foreach (var chave in master.Keys)
            {
                Assert.False(string.IsNullOrWhiteSpace(trad[chave]), $"{satellite} valor VAZIO para a chave {chave} — exporia a chave ao usuário.");
            }
        }
    }

    [Fact]
    public void ViewsSemHtmlLangHardcodado_E_UsoDoResolverCanônico()
    {
        var arquivos = Directory.GetFiles(ViewsDir, "*.cshtml", SearchOption.AllDirectories);
        Assert.True(arquivos.Length > 0, "Diretório de views não encontrado.");
        var comHtmlLang = 0;
        foreach (var arquivo in arquivos)
        {
            var conteudo = File.ReadAllText(arquivo);
            var relativo = Path.GetRelativePath(Root, arquivo).Replace('\\', '/');
            Assert.DoesNotContain("lang=\"pt-BR\"", conteudo, StringComparison.Ordinal);
            Assert.DoesNotContain("lang=\"pt-br\"", conteudo, StringComparison.Ordinal);
            if (conteudo.Contains("<html lang=", StringComparison.Ordinal))
            {
                comHtmlLang++;
                Assert.True(conteudo.Contains("UiText.HtmlLang(Context)", StringComparison.Ordinal),
                    $"{relativo} tem <html lang> mas não usa o resolver canônico UiText.HtmlLang(Context).");
            }
        }
        // 4 páginas com <html> próprio: _Layout, _AuthLayout, OrcamentoImprimir, ValeImprimir.
        Assert.Equal(4, comHtmlLang);
    }

    [Fact]
    public void PipelineAtivaUseRequestLocalization_E_BuilderEmiteClaimsDeIdioma()
    {
        var program = File.ReadAllText(Path.Combine(Root, "backend", "PlantaoPro.Web", "Program.cs"));
        Assert.Contains("app.UseRequestLocalization()", program, StringComparison.Ordinal);
        Assert.Contains("new UiLanguageCultureProvider()", program, StringComparison.Ordinal);

        var builder = File.ReadAllText(Path.Combine(Root, "backend", "PlantaoPro.Web", "Security", "SessionClaimsBuilder.cs"));
        Assert.Contains("\"idioma\"", builder, StringComparison.Ordinal);
        Assert.Contains("\"org_idioma\"", builder, StringComparison.Ordinal);
    }
}

/// <summary>Comportamental Web (stub determinístico, coleção web-bff).</summary>
[Collection("web-bff")]
public sealed class R7E1I18nWebStubTests : IDisposable
{
    private readonly PlantaoProWebFactory _factory;

    public R7E1I18nWebStubTests(PlantaoProWebFactory factory) => _factory = factory;

    public void Dispose() => _factory.ApiStub.Reset();

    private static async Task<string> GetAsync(HttpClient client, string url, string? acceptLanguage = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        if (acceptLanguage != null) req.Headers.AcceptLanguage.ParseAdd(acceptLanguage);
        using var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        return await resp.Content.ReadAsStringAsync();
    }

    // O Razor aplica HtmlEncoder por padrão: acentos chegam como entidades numéricas
    // (é -> &#xE9;). As asserts com texto acentuado comparam o HTML DECODIFICADO.
    private static string Dec(string html) => System.Net.WebUtility.HtmlDecode(html);

    [Fact]
    public async Task Anonimo_AcceptLanguageFrancês_RenderizaFrancesNoLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await GetAsync(client, "/Account/Login", "fr-FR,fr;q=0.9");

        Assert.Contains("<html lang=\"fr\">", html, StringComparison.Ordinal);
        Assert.Contains("Accédez à votre compte", Dec(html), StringComparison.Ordinal);
        Assert.DoesNotContain("Acesse sua conta", Dec(html), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Anonimo_IdiomaIndisponível_DegradaParaPtBr()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await GetAsync(client, "/Account/Login", "de-DE,de;q=0.9");

        Assert.Contains("<html lang=\"pt-BR\">", html, StringComparison.Ordinal);
        Assert.Contains("Acesse sua conta", Dec(html), StringComparison.Ordinal);
        Assert.DoesNotContain("Accédez à votre compte", Dec(html), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Anonimo_SemNenhumaFonte_PtBrPorPadrao()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await GetAsync(client, "/Account/Login");

        Assert.Contains("<html lang=\"pt-BR\">", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClaimUsuarioVenceOrganizacaoENavegador()
    {
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "MEDICO"), ("modules", "MEU_DIA"), ("permissions", "MEU_DIA.*"),
                ("claim", "idioma=es"), ("claim", "org_idioma=fr")));

        var html = await GetAsync(client, "/Account/Login", "fr");

        Assert.Contains("<html lang=\"es\">", html, StringComparison.Ordinal);
        Assert.Contains("Acceda a su cuenta", Dec(html), StringComparison.Ordinal);
        Assert.DoesNotContain("Accédez à votre compte", Dec(html), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClaimOrganizacaoVenceNavegadorQuandoUsuarioNaoDefiniu()
    {
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "MEDICO"), ("modules", "MEU_DIA"), ("permissions", "MEU_DIA.*"),
                ("claim", "org_idioma=fr")));

        var html = await GetAsync(client, "/Account/Login", "de");

        Assert.Contains("<html lang=\"fr\">", html, StringComparison.Ordinal);
    }

    private static string LoginPayload(string idiomaUsuario, string? idiomaOrg)
    {
        var extra = $",\n            \"idiomaPreferido\": \"{idiomaUsuario}\",\n            \"idiomaOrganizacao\": {(idiomaOrg is null ? "null" : "\"" + idiomaOrg + "\"")}";
        return WpA1LandingMedicoTests.PayloadTemplate
            .Replace("__MODULES__", "[\"MEU_DIA\"]")
            .Replace("\"permissions\": [\"MEU_DIA.*\"]", "\"permissions\": [\"MEU_DIA.*\"]" + extra);
    }

    private async Task<HttpClient> FazerLoginComPayloadAsync(string idiomaUsuario, string? idiomaOrg)
    {
        _factory.ApiStub.Respond(req =>
            req.RequestUri?.AbsolutePath == "/api/auth/login"
                ? StubApiHandler.Json(HttpStatusCode.OK, LoginPayload(idiomaUsuario, idiomaOrg))
                : StubApiHandler.Json(HttpStatusCode.OK, "{\"ok\":true}"));

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var pagina = await client.GetAsync("/Account/Login");
        var html = await pagina.Content.ReadAsStringAsync();
        var match = System.Text.RegularExpressions.Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "Token antiforgery nao encontrado na pagina de login.");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "medico@clinica.teste",
            ["senha"] = "senha-valida-123",
            ["__RequestVerificationToken"] = match.Groups[1].Value
        });
        using var resp = await client.PostAsync("/Account/Login", form);
        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        return client;
    }

    [Fact]
    public async Task Login_PayloadComIdiomaDoUsuario_EmiteClaimERenderizaSemMudarBrowser()
    {
        var client = await FazerLoginComPayloadAsync("es", null);
        var html = await GetAsync(client, "/Account/Login", "de");

        Assert.Contains("<html lang=\"es\">", html, StringComparison.Ordinal);
        Assert.Contains("Acceda a su cuenta", html, StringComparison.Ordinal);
    }

    private const string EnvelopeOkIdioma = "{\"success\":true,\"message\":\"Idioma atualizado com sucesso.\",\"data\":{\"idioma\":\"es\"},\"errors\":null,\"statusCode\":200,\"timestamp\":\"2026-10-10T00:00:00Z\"}";

    [Fact]
    public async Task SalvarIdioma_PostPersisteViaApiReemiteSessaoEEfeitoImediatoSemRelogin()
    {
        _factory.ApiStub.Reset();
        var corpoSalvamento = new System.Text.StringBuilder();
        _factory.ApiStub.Respond(req =>
        {
            var uri = req.RequestUri?.ToString() ?? string.Empty;
            if (uri.Contains("/api/usuarios/me/idioma") && req.Method == HttpMethod.Post)
            {
                corpoSalvamento.Append(req.Content is null ? string.Empty : req.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                return StubApiHandler.Json(HttpStatusCode.OK, EnvelopeOkIdioma);
            }
            if (uri.Contains("/api/auth/refresh-context") && req.Method == HttpMethod.Post)
                return StubApiHandler.Json(HttpStatusCode.OK, LoginPayload("es", null));
            return StubApiHandler.Json(HttpStatusCode.OK, "{\"ok\":true}");
        });

        var (client, token) = await WebBffTestKit.ClienteComAntiforgeryAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "MEDICO"), ("modules", "MEU_DIA"), ("permissions", "MEU_DIA.*")));

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["idioma"] = "es",
            ["__RequestVerificationToken"] = token
        });
        using var resp = await client.PostAsync("/Usuario/SalvarIdioma", form);

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        Assert.EndsWith("/Usuario/Index", resp.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"idioma\":\"es\"", corpoSalvamento.ToString(), StringComparison.Ordinal);
        Assert.Contains("POST http://api.test.local/api/usuarios/me/idioma", _factory.ApiStub.Requests);
        Assert.Contains("POST http://api.test.local/api/auth/refresh-context", _factory.ApiStub.Requests);

        // Efeito imediato: MESMO cookie, browser alemão, e a interface já está em espanhol.
        var html = await GetAsync(client, "/Usuario/Index", "de");
        Assert.Contains("<html lang=\"es\">", html, StringComparison.Ordinal);
        Assert.Contains("Mi Perfil", Dec(html), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SalvarIdioma_ValorForaDoCatalogo_NaoChamaAPINemMudaNada()
    {
        _factory.ApiStub.Reset();
        _factory.ApiStub.Respond(_ => StubApiHandler.Json(HttpStatusCode.OK, "{\"ok\":true}"));
        var (client, token) = await WebBffTestKit.ClienteComAntiforgeryAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "MEDICO"), ("modules", "MEU_DIA"), ("permissions", "MEU_DIA.*")));

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["idioma"] = "zz",
            ["__RequestVerificationToken"] = token
        });
        using var resp = await client.PostAsync("/Usuario/SalvarIdioma", form);

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        Assert.EndsWith("/Usuario/Index", resp.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(_factory.ApiStub.Requests, r => r.Contains("/api/usuarios/me/idioma", StringComparison.OrdinalIgnoreCase));

        // Nada mudou: navegador francês continua valendo.
        var html = await GetAsync(client, "/Account/Login", "fr");
        Assert.Contains("<html lang=\"fr\">", html, StringComparison.Ordinal);
    }
}

/// <summary>Persistência REAL (API + PostgreSQL de teste): coluna v2344 escrita e lida de volta.</summary>
public sealed class R7E1I18nPersistenciaRealTests
{
    private const string Senha = "R7E1!senha-valida-2026";

    private static async Task<Guid> SeedInfraAsync(NpgsqlConnection cn, Guid t)
    {
        await cn.ExecuteAsync("insert into plantaopro.clientes(id,tenant_id,nome,status,reg_status) values(@t,@t,'R7E1 Clinica','ATIVO','A')", new { t });
        await cn.ExecuteAsync(@"insert into plantaopro.tenants(id,tenant_id,codigo,nome,status,dados,criado_em,reg_status,reg_date,metadata)
            values(@t,@t,'R7E1T','R7E1 Tenant','ATIVO','{}',now(),'A',now(),'{""idiomaPadrao"":""es""}'::jsonb)", new { t });
        var perfil = await cn.ExecuteScalarAsync<Guid>(@"insert into plantaopro.perfis(tenant_id,codigo,nome,base_sistema,customizado,status,reg_status,reg_date,metadata)
            values(@t,'MEDICO','Medico',false,true,'ATIVO','A',now(),'{}'::jsonb) returning id", new { t });
        return perfil;
    }

    private static async Task<Guid> SeedUserAsync(NpgsqlConnection cn, Guid t, Guid perfil, Guid u, string email)
    {
        var hash = PasswordHashService.Hash(Senha);
        await cn.ExecuteAsync(@"insert into plantaopro.usuarios(id,tenant_id,cliente_id,nome,email,email_normalizado,senha_hash,status,reg_status,senha_alteracao_obrigatoria,preferencias_notificacao,reg_date)
            values(@u,@t,@t,'R7E1 Medico',@email,lower(@email),@h,'ATIVO','A',false,'{}',now())", new { u, t, email, h = hash });
        await cn.ExecuteAsync(@"insert into plantaopro.usuarios_perfis(usuario_id,perfil_id,status,metadata,reg_status,reg_date)
            values(@u,@p,'ATIVO','{}'::jsonb,'A',now())", new { u, p = perfil });
        return u;
    }

    private static async Task LimparAsync(NpgsqlConnection cn, Guid t, params Guid[] userIds)
    {
        await cn.ExecuteAsync("delete from plantaopro.usuarios_perfis where usuario_id = any(@ids)", new { ids = userIds });
        await cn.ExecuteAsync("delete from plantaopro.usuarios where id = any(@ids)", new { ids = userIds });
        await cn.ExecuteAsync("delete from plantaopro.perfis where tenant_id=@t and codigo='MEDICO'", new { t });
        await cn.ExecuteAsync("delete from plantaopro.tenants where id=@t", new { t });
        await cn.ExecuteAsync("delete from plantaopro.clientes where id=@t", new { t });
    }

    private static async Task<JsonElement> LogarAsync(HttpClient api, string email)
    {
        using var resp = await api.PostAsJsonAsync("api/auth/login", new { email, senha = Senha });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.Clone();
    }

    private static string? Text(JsonElement root, string prop) =>
        root.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static string Bearer(JsonElement login) => "Bearer " + Text(login, "token");

    [Fact]
    public async Task ApiReal_SalvaColunaV2344_E_RefleteNoRefreshENoNovoLogin()
    {
        var tenant = Guid.NewGuid();
        var userA = Guid.NewGuid();
        var emailA = $"r7e1-a-{tenant:N}@plantaopro.test";

        await using (var cn = new NpgsqlConnection(TestDatabase.ConnectionString))
        {
            await cn.OpenAsync();
            var perfil = await SeedInfraAsync(cn, tenant);
            await SeedUserAsync(cn, tenant, perfil, userA, emailA);
        }

        try
        {
            await using var api = new PlantaoProApiFactory();
            var client = api.CreateClient();

            // 1) Primeiro login: sem preferência de usuário; padrão da ORGANIZAÇÃO vem do banco.
            var primeiro = await LogarAsync(client, emailA);
            Assert.Null(Text(primoProp(primeiro), "idiomaPreferido"));
            Assert.Equal("es", Text(primoProp(primeiro), "idiomaOrganizacao"));

            // 2) Salvar preferência via endpoint real → coluna v2344 atualizada no PostgreSQL.
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Text(primoProp(primeiro), "token")!);
            var save = await client.PostAsJsonAsync("api/usuarios/me/idioma",
                new { idioma = "fr" });
            Assert.Equal(HttpStatusCode.OK, save.StatusCode);
            await using (var cn = new NpgsqlConnection(TestDatabase.ConnectionString))
            {
                await cn.OpenAsync();
                var salvo = await cn.ExecuteScalarAsync<string?>("select idioma_preferido from plantaopro.usuarios where id=@u", new { u = userA });
                Assert.Equal("fr", salvo);
            }

            // 3) Refresh-context: mesmo token/sessão já traz a nova preferência.
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Text(primoProp(primeiro), "token")!);
            using var refreshResp = await client.PostAsync("api/auth/refresh-context", null);
            Assert.Equal(HttpStatusCode.OK, refreshResp.StatusCode);
            using var refreshDoc = JsonDocument.Parse(await refreshResp.Content.ReadAsStringAsync());
            var refreshData = refreshDoc.RootElement.GetProperty("data");
            Assert.Equal("fr", Text(refreshData, "idiomaPreferido"));
            Assert.Equal("es", Text(refreshData, "idiomaOrganizacao"));

            // 4) Novo login: preferências persistidas continuam valendo.
            var segundo = primoProp(await LogarAsync(client, emailA));
            Assert.Equal("fr", Text(segundo, "idiomaPreferido"));
            Assert.Equal("es", Text(segundo, "idiomaOrganizacao"));
        }
        finally
        {
            await using (var cn = new NpgsqlConnection(TestDatabase.ConnectionString))
            {
                await cn.OpenAsync();
                await LimparAsync(cn, tenant, userA);
            }
        }
    }

    private static JsonElement primoProp(JsonElement login) => login.GetProperty("data");
}
