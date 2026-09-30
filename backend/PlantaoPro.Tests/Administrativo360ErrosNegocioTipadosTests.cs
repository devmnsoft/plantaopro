using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Administrativo360;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using PlantaoPro.Tests.Infrastructure;
using Xunit;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Tests;

/// <summary>
/// Suíte do WP3 — erros de negócio tipados, logs estruturados com correlação e 400 ≠ falha técnica.
///
/// Escopo validado:
///   1. Mapeamento do filtro Adm360BusinessExceptionFilter (unidade, sem banco):
///      negócio/entrada inválida → 400, acesso negado no domínio → 403, registro inexistente → 404,
///      falhas técnicas (inclusive InvalidOperationException genérica) NÃO são convertidas e seguem
///      para o handler global (HTTP 500 + log estruturado com correlação).
///   2. HTTP real via WebApplicationFactory contra o PostgreSQL de teste:
///      - 400 amigável em violação de unicidade (duplicata de departamento);
///      - X-Correlation-ID gerado quando ausente e ecoado quando enviado pelo cliente;
///      - 401 sem token / 403 por papel fora do escopo no resumo;
///      - autorização persistida em plantaopro.permissao_logs (schema 060, payload jsonb) — fechamento
///        do achado A1 (INSERT antigo referenciava colunas inexistentes → PG 42703 silencioso);
///      - negação persistida em permissao_logs E acessos_negados_log (mesmo formato 060).
/// </summary>
public sealed class Administrativo360ErrosNegocioTipadosTests : IClassFixture<PlantaoProApiFactory>
{
    private readonly PlantaoProApiFactory _factory;

    private static readonly Guid TenantSantaCasa = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
    private const string GestorEmail = "gestor@santacasa-demo.example";
    private const string GestorSenha = "SantaCasa!Demo2026#Gestor";
    private const string AuditorEmail = "consulta@santacasa-demo.example";
    private const string AuditorSenha = "SantaCasa!Demo2026#Gestor";

    public Administrativo360ErrosNegocioTipadosTests(PlantaoProApiFactory factory) => _factory = factory;

    // =========================================================================
    // Testes de unidade do filtro (sem banco, sem host)
    // =========================================================================

    private static ExceptionContext Contexto(Exception ex, string path)
    {
        var http = new DefaultHttpContext();
        http.Request.Path = new PathString(path);
        // .NET 10: o ctor de ExceptionContext copia via ActionContext e exige RouteData não nulo
        // (ThrowIfNull 'routeData'). RouteValueDictionary deixou de herdar de RouteData no .NET 10,
        // então usamos new RouteData() + o ctor de 3 args (sem ModelStateDictionary).
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new ExceptionContext(action, Array.Empty<IFilterMetadata>()) { Exception = ex };
    }

    private static (int? Status, ApiResponse<string>? Resposta, bool Tratado) AplicarFiltro(Exception ex, string path)
    {
        var ctx = Contexto(ex, path);
        new Adm360BusinessExceptionFilter().OnException(ctx);
        if (!ctx.ExceptionHandled || ctx.Result is not JsonResult jr || jr.Value is not ApiResponse<string> resp)
            return (null, null, ctx.ExceptionHandled);
        return (jr.StatusCode, resp, true);
    }

    [Fact]
    public void Excecao_de_negocio_do_modulo_vira_400_com_a_mensagem_amigavel()
    {
        var (status, resp, tratado) = AplicarFiltro(
            new Administrativo360BusinessException("Já existe um cargo ativo com este código."),
            "/api/administrativo360/cargos");
        Assert.True(tratado, "Exceção de negócio deve ser tratada pelo filtro.");
        Assert.Equal(400, status);
        Assert.False(resp!.Success);
        Assert.Equal("Já existe um cargo ativo com este código.", resp.Message);
    }

    [Fact]
    public void Argumento_invalido_com_sufixo_de_parâmetro_vira_400_limpo()
    {
        var (status, resp, tratado) = AplicarFiltro(
            new ArgumentException("Valor fora da faixa permitida (Parameter 'quantidade')"),
            "/api/administrativo360/reservas");
        Assert.True(tratado);
        Assert.Equal(400, status);
        Assert.Equal("Valor fora da faixa permitida", resp!.Message);
    }

    [Fact]
    public void Registro_inexistente_sem_mensagem_vira_404_com_mensagem_padrao()
    {
        var (status, resp, tratado) = AplicarFiltro(new KeyNotFoundException(), "/api/administrativo360/departamentos/{id}");
        Assert.True(tratado);
        Assert.Equal(404, status);
        Assert.Equal("Registro não encontrado.", resp!.Message);
    }

    [Fact]
    public void Registro_inexistente_com_mensagem_vira_404_com_a_mensagem()
    {
        var (status, resp, tratado) = AplicarFiltro(
            new KeyNotFoundException("Colaborador não encontrado ou inativo."),
            "/api/administrativo360/colaboradores/{id}");
        Assert.True(tratado);
        Assert.Equal(404, status);
        Assert.Equal("Colaborador não encontrado ou inativo.", resp!.Message);
    }

    [Fact]
    public void Acesso_negado_no_dominio_vira_403()
    {
        var (status, resp, tratado) = AplicarFiltro(new UnauthorizedAccessException(), "/api/administrativo360/departamentos/{id}");
        Assert.True(tratado);
        Assert.Equal(403, status);
        Assert.Equal("Acesso não autorizado.", resp!.Message);
    }

    [Fact]
    public void InvalidOperationException_genérica_não_é_erro_de_negocio_e_passa_para_o_handler_global()
    {
        var (_, _, tratado) = AplicarFiltro(new InvalidOperationException("Falha técnica simulada"), "/api/administrativo360/resumo");
        Assert.False(tratado, "Falha técnica genérica não pode virar 400: segue para o handler global (500 + log estruturado).");
    }

    [Fact]
    public void Exceção_técnica_inesperada_não_é_interceptada_pelo_filtro()
    {
        var (_, _, tratado) = AplicarFiltro(new NullReferenceException(), "/api/administrativo360/resumo");
        Assert.False(tratado);
    }

    [Theory]
    [InlineData("/api/outro-modulo/coisa")]
    [InlineData("/api/administrativo-outra/coisa")]
    public void Rota_fora_do_administrativo360_não_é_interceptada(string path)
    {
        var (_, _, tratadoNegocio) = AplicarFiltro(new Administrativo360BusinessException("regra"), path);
        Assert.False(tratadoNegocio, $"Filtro deveria ignorar a rota {path} para exceção de negócio.");

        var (_, _, tratadoArg) = AplicarFiltro(new ArgumentException("entrada inválida"), path);
        Assert.False(tratadoArg, $"Filtro deveria ignorar a rota {path} para ArgumentException.");
    }

    // =========================================================================
    // Testes de integração (API real via WebApplicationFactory + PostgreSQL)
    // =========================================================================

    private static string Sufixo() => Guid.NewGuid().ToString("N")[..8];

    private async Task<string> TokenAsync(string email, string senha)
    {
        using var client = _factory.CreateClient();
        var resposta = await client.PostAsJsonAsync("api/auth/login", new { Email = email, Senha = senha });
        resposta.EnsureSuccessStatusCode();
        var json = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var token = json.GetProperty("data").GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token), $"Login de {email} não retornou token.");
        return token!;
    }

    private async Task<HttpClient> ClienteAsync(string email, string senha)
    {
        var token = await TokenAsync(email, senha);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Departamento_duplicado_retorna_400_de_negocio_e_eca_correlacao_enviada()
    {
        using var client = await ClienteAsync(GestorEmail, GestorSenha);
        var codigo = "ERR" + Sufixo().ToUpperInvariant()[..7];
        string? idCriado = null;
        try
        {
            var r1 = await client.PostAsJsonAsync("api/administrativo360/departamentos", new { codigo, nome = "Depto Erros Tipados" });
            Assert.True(r1.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created, $"Criação inicial: {(int)r1.StatusCode} {await r1.Content.ReadAsStringAsync()}");
            var corpo1 = await r1.Content.ReadFromJsonAsync<JsonElement>();
            idCriado = corpo1.ValueKind == JsonValueKind.Object && corpo1.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;

            // Correlação gerada pela API quando o cliente não envia.
            Assert.True(r1.Headers.TryGetValues("X-Correlation-ID", out var gerada), "Resposta 201 deve carregar X-Correlation-ID gerado pela API.");
            Assert.False(string.IsNullOrWhiteSpace(gerada?.FirstOrDefault()));

            var correlacao = $"wp3-corr-{Sufixo()}";
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/administrativo360/departamentos")
            {
                Content = JsonContent.Create(new { codigo, nome = "Depto Erros Tipados Dup" })
            };
            req.Headers.Add("X-Correlation-ID", correlacao);
            var r2 = await client.SendAsync(req);

            Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
            var corpo = await r2.Content.ReadAsStringAsync();
            Assert.Contains("Já existe um departamento ativo com este código.", corpo);

            // Correlação enviada pelo cliente é ecoada (fonte única por requisição).
            Assert.Equal(correlacao, r2.Headers.GetValues("X-Correlation-ID").Single());
        }
        finally
        {
            if (!string.IsNullOrEmpty(idCriado))
            {
                try { await client.PostAsync($"api/administrativo360/departamentos/{idCriado}/inativar", null); }
                catch { /* limpeza best-effort */ }
            }
        }
    }

    [Fact]
    public async Task Correlacao_presente_em_respostas_sufito_autenticadas()
    {
        using var client = await ClienteAsync(GestorEmail, GestorSenha);
        var r = await client.GetAsync("api/administrativo360/resumo");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.True(r.Headers.TryGetValues("X-Correlation-ID", out var valores));
        Assert.False(string.IsNullOrWhiteSpace(valores?.FirstOrDefault()));
    }

    [Fact]
    public async Task Sem_token_401_e_auditor_fora_do_papel_403_no_resumo()
    {
        using var anonimo = _factory.CreateClient();
        var rAnonimo = await anonimo.GetAsync("api/administrativo360/resumo");
        Assert.Equal(HttpStatusCode.Unauthorized, rAnonimo.StatusCode);

        using var auditor = await ClienteAsync(AuditorEmail, AuditorSenha);
        var rAuditor = await auditor.GetAsync("api/administrativo360/resumo");
        Assert.Equal(HttpStatusCode.Forbidden, rAuditor.StatusCode);
    }

    [Fact]
    public async Task Autorizacao_persiste_em_permissao_logs_com_o_schema_oficial_060()
    {
        // O INSERT grava tenant_id = GetClienteId() (claim cliente_id do JWT = usuarios.cliente_id),
        // que no demo difere da constante TenantSantaCasa — por isso o tenant é derivado do próprio usuário.
        var (uid, clienteId) = await ObterAuditorAsync();
        var antes = await ContarPermissaoLogs(clienteId, uid, PermissionConstants.AuditoriaVer, "true");

        using var auditor = await ClienteAsync(AuditorEmail, AuditorSenha);
        var r = await auditor.GetAsync("api/auditoria?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);

        var depois = await AguardarMinimoAsync(() => ContarPermissaoLogs(clienteId, uid, PermissionConstants.AuditoriaVer, "true"), antes + 1);
        Assert.True(depois >= antes + 1, $"permissao_logs não registrou a autorização (antes={antes}, depois={depois}, tenant={clienteId}). Antes do WP3 o INSERT falhava (PG 42703 — colunas inexistentes).");
    }

    [Fact]
    public async Task Negacao_persiste_em_permissao_logs_e_acessos_negados_log()
    {
        var (uid, _) = await ObterAuditorAsync();
        var permissaoAntes = await ContarPermissaoLogs(TenantSantaCasa, uid, PermissionConstants.EscalasVer, "false");
        var negadosAntes = await ContarAcessosNegados(uid);

        var accessor = new HttpContextAccessor();
        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("uid", uid.ToString()),
                new Claim("cliente_id", TenantSantaCasa.ToString()),
                new Claim(ClaimTypes.Role, "AUDITOR")
            }, "teste"))
        };
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = TestDatabase.ConnectionString
        }).Build();
        var usuarioCtx = new UsuarioContextService(accessor);
        var audit = new AuditService(cfg, NullLogger<AuditService>.Instance);
        var tenantGuard = new TenantGuardService(cfg, usuarioCtx, audit, NullLogger<TenantGuardService>.Instance);
        var guard = new PermissionGuardService(usuarioCtx, tenantGuard, cfg, NullLogger<PermissionGuardService>.Instance);

        var resultado = await guard.ValidarPermissaoAsync(PermissionConstants.EscalasVer);
        Assert.False(resultado.Success, "Perfil AUDITOR não possui ESCALAS_VER: a validação deve negar.");
        Assert.Equal(403, resultado.StatusCode);

        var permissaoDepois = await AguardarMinimoAsync(() => ContarPermissaoLogs(TenantSantaCasa, uid, PermissionConstants.EscalasVer, "false"), permissaoAntes + 1);
        var negadosDepois = await AguardarMinimoAsync(() => ContarAcessosNegados(uid), negadosAntes + 1);
        Assert.True(permissaoDepois >= permissaoAntes + 1, $"permissao_logs não registrou a negação (antes={permissaoAntes}, depois={permissaoDepois}).");
        Assert.True(negadosDepois >= negadosAntes + 1, $"acessos_negados_log não registrou a negação (antes={negadosAntes}, depois={negadosDepois}). Antes do WP3 o INSERT falhava (PG 42703).");
    }

    /// <summary>Aguarda (janela curta) até a consulta atingir o mínimo — absorve jitter de pipeline sem esconder falha real.</summary>
    private static async Task<long> AguardarMinimoAsync(Func<Task<long>> consulta, long minimo, int tentativas = 20, int intervaloMs = 250)
    {
        long valor = 0;
        for (var i = 0; i < tentativas; i++)
        {
            valor = await consulta();
            if (valor >= minimo) return valor;
            await Task.Delay(intervaloMs);
        }
        return valor;
    }

    /// <summary>Retorna o id do auditor e o cliente_id (o valor que GetClienteId() grava em tenant_id).</summary>
    private static async Task<(Guid Uid, Guid? ClienteId)> ObterAuditorAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        cn.Open();
        return await cn.QuerySingleAsync<(Guid Uid, Guid? ClienteId)>(
            "select id, cliente_id from plantaopro.usuarios where lower(email)='consulta@santacasa-demo.example'");
    }

    private static async Task<long> ContarPermissaoLogs(Guid? tenant, Guid uid, string permissao, string autorizado)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        cn.Open();
        return await cn.ExecuteScalarAsync<long>(
            @"select count(1) from plantaopro.permissao_logs
              where (@t::uuid is null or tenant_id=@t)
                and nome=@p and dados->>'usuario_id'=@uid and dados->>'autorizado'=@a",
            new { t = tenant, p = permissao, uid = uid.ToString(), a = autorizado });
    }

    private static async Task<long> ContarAcessosNegados(Guid uid)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        cn.Open();
        return await cn.ExecuteScalarAsync<long>(
            @"select count(1) from plantaopro.acessos_negados_log
              where tenant_id=@t and codigo='ACESSO_NEGADO' and dados->>'usuario_id'=@uid and dados->>'entidade'='PERMISSAO'",
            new { t = TenantSantaCasa, uid = uid.ToString() });
    }
}
