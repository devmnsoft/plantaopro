using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R93 (B5 observabilidade): erros de negócio centralizados + digest diário.
/// Antes da rodada, as exceções de negócio conhecidas do módulo Administrativo 360 viravam
/// 400/403/404 amigáveis no Adm360BusinessExceptionFilter e paravam aí — nenhum destino as
/// guardava (apenas falhas técnicas 500 caíam em plantaopro.api_error_logs). A rodada cria
/// plantaopro.erros_negocio (migration v2374), passa a persistir cada erro mapeado pelo filtro
/// (best-effort, timeout curto, sem alterar a resposta) e expõe o digest diário em
/// GET /api/observabilidade/digest-diario (+ TotalErrosNegocioHoje no /resumo). Contrato travado:
/// migration com a DDL exata, manifest com hash real e dependência em v2373, tabela/índices no
/// banco de teste (auto-cura pela reaplicação idempotente sob advisory lock + retry de impasse
/// 40P01, padrão R55/R89/R92), persistência real via HTTP 404 de negócio no fiscal (rota sem
/// catch no controller → passa pelo filtro) com tenant/usuário das claims, e agregação do
/// digest por dia/categoria com top de mensagens e filtro de tenant.
/// </summary>
public sealed class R93ObservabilidadeErrosNegocioContratoTests : IClassFixture<PlantaoProApiFactory>
{
    private readonly PlantaoProApiFactory _factory;

    private const string MigrationFile = "database/migrations/2026_10_v2374_r93_erros_negocio.sql";
    private const string MigrationVersion = "2026_10_v2374_r93_erros_negocio";

    private const string GestorEmail = "gestor@santacasa-demo.example";
    private const string GestorSenha = "SantaCasa!Demo2026#Gestor";

    /// <summary>Tenant dedicado deste contrato: o digest global enxerga todos os tenants, então o
    /// determinismo da agregação exige um tenant que só este teste escreve (limpo ao final).</summary>
    private static readonly Guid TenantR93 = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f6cfa93");

    public R93ObservabilidadeErrosNegocioContratoTests(PlantaoProApiFactory factory) => _factory = factory;

    // Fato 1 - migration v2374 com a DDL exata da tabela e dos dois índices.
    [Fact]
    public void Migration_v2374_cria_erros_negocio_com_DDL_exata()
    {
        var root = RepositoryPathResolver.ResolveRoot();
        var sql = File.ReadAllText(Path.Combine(root, MigrationFile));
        Assert.StartsWith("-- R93", sql.TrimStart());
        foreach (var fragmento in new[]
        {
            "create table if not exists plantaopro.erros_negocio (",
            "id uuid primary key default gen_random_uuid()",
            "tenant_id uuid",
            "usuario_id uuid",
            "modulo varchar(80) not null",
            "categoria varchar(20) not null check (categoria in ('REGRA_NEGOCIO','ACESSO_NEGADO','NAO_ENCONTRADO'))",
            "mensagem text not null",
            "endpoint text",
            "reg_date timestamptz not null default now()",
            "create index if not exists ix_erros_negocio_dia on plantaopro.erros_negocio(reg_date desc);",
            "create index if not exists ix_erros_negocio_tenant_dia on plantaopro.erros_negocio(tenant_id, reg_date desc);"
        })
        {
            Assert.Contains(fragmento, Normalizar(sql));
        }
    }

    // Fato 2 - manifesto com a entrada v2374 real (hash LF, dependência em v2373, status ativo).
    [Fact]
    public void Manifesto_tem_entrada_v2374_com_hash_real_dependencia_v2373_e_status()
    {
        var root = RepositoryPathResolver.ResolveRoot();
        var sql = File.ReadAllText(Path.Combine(root, MigrationFile));
        var manifest = File.ReadAllText(Path.Combine(root, "database", "migration-manifest.json"));

        using var doc = JsonDocument.Parse(manifest);
        var entrada = doc.RootElement.GetProperty("migrations").EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("version").GetString() == MigrationVersion);
        Assert.NotEqual(JsonValueKind.Undefined, entrada.ValueKind);

        Assert.Equal(MigrationFile.Replace('\\', '/'), entrada.GetProperty("source").GetString());
        Assert.Equal(Sha256Lf(sql), entrada.GetProperty("checksum").GetString());
        Assert.Equal("active", entrada.GetProperty("status").GetString());
        Assert.True(entrada.GetProperty("transactional").GetBoolean());
        var deps = entrada.GetProperty("dependsOn").EnumerateArray().Select(d => d.GetString()).ToArray();
        Assert.Equal(new[] { "2026_10_v2373_r92_indizes_caminhos_quentes" }, deps);
    }

    // Fato 3 - tabela e índices presentes no banco de teste com definição exata (auto-cura idempotente).
    [Fact]
    public async Task TabelaEIndices_existemNoBancoDeTestesComDefinicaoExata()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();

        await GarantirTabelaAsync(cn);

        var tabela = await cn.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM information_schema.tables WHERE table_schema='plantaopro' AND table_name='erros_negocio';");
        Assert.Equal(1, tabela);

        foreach (var nome in new[] { "ix_erros_negocio_dia", "ix_erros_negocio_tenant_dia" })
        {
            var ocorrencias = await cn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM pg_indexes WHERE schemaname='plantaopro' AND indexname=@nome;",
                new { nome });
            Assert.Equal(1, ocorrencias);
        }

        var defDia = await cn.ExecuteScalarAsync<string>(
            "SELECT pg_get_indexdef('plantaopro.ix_erros_negocio_dia'::regclass);");
        Assert.Contains("USING btree (reg_date DESC)", defDia);

        var defTenant = await cn.ExecuteScalarAsync<string>(
            "SELECT pg_get_indexdef('plantaopro.ix_erros_negocio_tenant_dia'::regclass);");
        Assert.Contains("USING btree (tenant_id, reg_date DESC)", defTenant);
    }

    // Fato 4 - erro de negócio REAL via HTTP (404 de pré-nota inexistente no fiscal, rota sem
    // catch no controller → passa pelo filtro) persiste com tenant/usuário das claims.
    [Fact]
    public async Task Erro_de_negocio_via_API_persiste_em_erros_negocio_com_tenant_e_usuario()
    {
        await using var setup = new NpgsqlConnection(TestDatabase.ConnectionString);
        await setup.OpenAsync();
        await GarantirTabelaAsync(setup);

        var (uid, clienteId) = await setup.QuerySingleAsync<(Guid Uid, Guid? ClienteId)>(
            "select id, cliente_id from plantaopro.usuarios where lower(email)='gestor@santacasa-demo.example'");
        var antes = await ContarErrosDoCenarioAsync(setup, clienteId);

        using var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("api/auth/login", new { Email = GestorEmail, Senha = GestorSenha });
        Assert.True(login.IsSuccessStatusCode, $"Login do gestor: {(int)login.StatusCode} {await login.Content.ReadAsStringAsync()}");
        var jsonLogin = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jsonLogin.GetProperty("data").GetProperty("token").GetString());

        var notaInexistente = Guid.NewGuid();
        var r404 = await client.GetAsync($"api/administrativo360/fiscal/notas/{notaInexistente}");
        Assert.Equal(HttpStatusCode.NotFound, r404.StatusCode);
        var corpo404 = await r404.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Pré-nota não encontrada.", corpo404.GetProperty("message").GetString());

        var depois = await AguardarMinimoAsync(() => ContarErrosDoCenarioAsync(setup, clienteId), antes + 1);
        Assert.True(depois >= antes + 1,
            $"erros_negocio não registrou o 404 de negócio (antes={antes}, depois={depois}, tenant={clienteId}).");

        var linha = await setup.QueryFirstOrDefaultAsync<(string? Categoria, string? Endpoint, string? Mensagem)>(
            @"select categoria, endpoint, mensagem from plantaopro.erros_negocio
              where tenant_id=@t and usuario_id=@u
                and mensagem='Pré-nota não encontrada.'
              order by reg_date desc limit 1",
            new { t = clienteId, u = uid });
        Assert.Equal("NAO_ENCONTRADO", linha.Categoria);
        Assert.Contains("fiscal/notas", linha.Endpoint ?? string.Empty);
        Assert.Equal("Pré-nota não encontrada.", linha.Mensagem);
    }

    // Fato 5 - digest agrega por dia/categoria, ordena top de mensagens e respeita o filtro de tenant.
    [Fact]
    public async Task Digest_diario_agrega_por_dia_e_categoria_com_top_mensagens_e_filtro_de_tenant()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await GarantirTabelaAsync(cn);
        await cn.ExecuteAsync("delete from plantaopro.erros_negocio where tenant_id=@t", new { t = TenantR93 });
        try
        {
            await cn.ExecuteAsync(@"insert into plantaopro.erros_negocio (tenant_id, usuario_id, modulo, categoria, mensagem, endpoint, reg_date) values
                (@t, null, 'ADMINISTRATIVO360', 'REGRA_NEGOCIO', 'Saldo insuficiente para reservar o lote.', '/api/administrativo360/reservas', now()),
                (@t, null, 'ADMINISTRATIVO360', 'REGRA_NEGOCIO', 'Saldo insuficiente para reservar o lote.', '/api/administrativo360/reservas', now()),
                (@t, null, 'ADMINISTRATIVO360', 'NAO_ENCONTRADO', 'Orçamento não encontrado ou inativo.', '/api/administrativo360/orcamentos/{id}', now()),
                (@t, null, 'ADMINISTRATIVO360', 'REGRA_NEGOCIO', 'Estoque abaixo do mínimo para lançar o item.', '/api/administrativo360/estoque', now() - interval '1 day')",
                new { t = TenantR93 });

            var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = TestDatabase.ConnectionString
            }).Build();
            var ctrl = new PlantaoPro.Api.Controllers.ObservabilidadeController(cfg, NullLogger<PlantaoPro.Api.Controllers.ObservabilidadeController>.Instance);

            var r1 = await ctrl.DigestDiario(dias: 1, tenantId: TenantR93);
            var j1 = JsonSerializer.SerializeToElement(((ObjectResult)r1!).Value!)!.GetProperty("Data");
            Assert.True(j1.TryGetProperty("PeriodoDias", out _), $"resposta inesperada do digest: {j1.GetRawText()}");
            Assert.Equal(1, j1.GetProperty("PeriodoDias").GetInt32());
            Assert.Equal(3, j1.GetProperty("TotalErrosNegocio").GetInt64());
            var porDia = j1.GetProperty("PorDia").EnumerateArray().ToArray();
            Assert.Single(porDia);
            Assert.Equal(3, porDia[0].GetProperty("Total").GetInt64());
            var cats = porDia[0].GetProperty("Categorias").EnumerateArray().ToList();
            var regra = cats.First(c => c.GetProperty("Categoria").GetString() == "REGRA_NEGOCIO");
            var naoEncontrado = cats.First(c => c.GetProperty("Categoria").GetString() == "NAO_ENCONTRADO");
            Assert.Equal(2, regra.GetProperty("Total").GetInt64());
            Assert.Equal(1, naoEncontrado.GetProperty("Total").GetInt64());
            var top = j1.GetProperty("TopMensagens").EnumerateArray().First();
            Assert.Equal("Saldo insuficiente para reservar o lote.", top.GetProperty("Mensagem").GetString());
            Assert.Equal(2, top.GetProperty("Total").GetInt64());

            var r2 = await ctrl.DigestDiario(dias: 7, tenantId: TenantR93);
            var j2 = JsonSerializer.SerializeToElement(((ObjectResult)r2!).Value!)!.GetProperty("Data");
            Assert.Equal(4, j2.GetProperty("TotalErrosNegocio").GetInt64());
            Assert.Equal(2, j2.GetProperty("PorDia").EnumerateArray().Count());
        }
        finally
        {
            await cn.ExecuteAsync("delete from plantaopro.erros_negocio where tenant_id=@t", new { t = TenantR93 });
        }
    }

    // Fato 6 - resumo da observabilidade passa a expor o total de erros de negócio do dia.
    [Fact]
    public void Resumo_da_observabilidade_inclui_total_de_erros_negocio_do_dia()
    {
        var controller = File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "Controllers", "ObservabilidadeController.cs"));
        Assert.Contains("TotalErrosNegocioHoje", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("from plantaopro.erros_negocio where reg_date::date = current_date", controller, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Reaplica a migration v2374 idempotente (auto-cura padrão da era das rodadas), serializada por
    /// advisory lock sobre o próprio processo. Após a primeira aplicação bem-sucedida as chamadas
    /// seguem ignorando a DDL — menos catálogo disputado com as outras classes que rodam em paralelo.
    /// </summary>
    private static readonly SemaphoreSlim GarantirTabelaSema = new(1, 1);
    private static volatile bool _tabelaGarantida;

    private static async Task GarantirTabelaAsync(NpgsqlConnection cn)
    {
        if (_tabelaGarantida) return;
        await GarantirTabelaSema.WaitAsync();
        try
        {
            if (_tabelaGarantida) return;
            await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r93_erros_negocio'));");
            try
            {
                var sql = File.ReadAllText(Path.Combine(RepositoryPathResolver.ResolveRoot(), MigrationFile));
                for (var tentativa = 1; ; tentativa++)
                {
                    try
                    {
                        await cn.ExecuteAsync(sql);
                        break;
                    }
                    catch (PostgresException ex) when (ex.SqlState == "40P01" && tentativa < 5)
                    {
                        await Task.Delay(150 * tentativa);
                    }
                }
            }
            finally
            {
                await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r93_erros_negocio'));");
            }
            _tabelaGarantida = true;
        }
        finally
        {
            GarantirTabelaSema.Release();
        }
    }

    private static async Task<long> ContarErrosDoCenarioAsync(NpgsqlConnection cn, Guid? tenant)
    {
        return await cn.ExecuteScalarAsync<long>(
            @"select count(1) from plantaopro.erros_negocio
              where tenant_id=@t and categoria='NAO_ENCONTRADO'
                and endpoint like '%fiscal/notas%'
                and mensagem='Pré-nota não encontrada.'",
            new { t = tenant });
    }

    /// <summary>Aguarda (janela curta) até a consulta atingir o mínimo — absorve jitter sem esconder falha real.</summary>
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

    private static string Sha256Lf(string conteudo) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(conteudo.Replace("\r\n", "\n")))).ToLowerInvariant();

    private static string Normalizar(string sql) =>
        string.Join(" ", sql.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
}
