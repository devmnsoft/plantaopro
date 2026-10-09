using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R5-E13/P2: padronizacao do escopo canonico do nucleo clinico em tenant_id (divida medida na
/// evidencia D11). Todo ramo do BuildInsert do kernel Saude360 passa a persistir tenant_id junto
/// de cliente_id, e o tenant de origem SEMPRE vem do mapeamento persistido (clientes.tenant_id,
/// fallback tenants.cliente_id ou o proprio tenant no escopo) — nunca da chave de escopo nem de
/// claim cru. Os dois writers que contaminavam tenant_id com cliente_id (favoritar CID e usar
/// modelo de prescricao) estao travados por assercao. O backfill do legado fica na migracao
/// v2336, com a mesma regra e idempotencia condicional (so toca NULL/contaminado); sem
/// mapeamento o estado persistido e NULL honesto, provado ao vivo contra o banco de teste.
/// </summary>
public sealed class Saude360TenantScopeP2ContractTests
{
    private static readonly string Root = RepositoryPathResolver.RepoRoot;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        public FakeCurrentUser(Guid? escopo = null)
        {
            TenantId = escopo;
        }

        public bool GlobalAdmin => false;
        public Guid? UserId => Guid.Empty;
        public Guid? TenantId { get; }
        public Guid? ClienteId => TenantId;
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

    private static IConfiguration BuildCfg(string connectionString) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = connectionString })
        .Build();

    private static Saude360ClinicalService BuildService(Guid? escopo) =>
        new(BuildCfg(TestDatabase.ConnectionString), new FakeCurrentUser(escopo), new FakeAuditService(), NullLogger<Saude360ClinicalService>.Instance);

    [Fact]
    public void BuildInsert_GravaTenantIdEmTodoNucleoClinico_SemContaminacaoPelaChaveDeEscopo()
    {
        var service = Read("backend/PlantaoPro.Api/Saude360ClinicalService.cs");
        var inicio = service.IndexOf("private (string Sql, object Args) BuildInsert", StringComparison.Ordinal);
        var fim = service.IndexOf("private static string BuildUpdate", StringComparison.Ordinal);
        Assert.True(inicio > 0 && fim > inicio, "BuildInsert nao localizado entre os metodos-ancora.");

        var corpo = service.Substring(inicio, fim - inicio);
        var linhasInsert = corpo.Split('\n').Where(l => l.Contains("insert into plantaopro.")).ToList();
        Assert.Equal(18, linhasInsert.Count);
        Assert.All(linhasInsert, l => Assert.Contains("tenant_id", l, StringComparison.Ordinal));
        // Nenhum writer do arquivo pode voltar a escrever a chave de escopo dentro de tenant_id.
        Assert.DoesNotContain(",@tenantId,@tenantId,", service);
    }

    [Fact]
    public void Kernel_ResolveTenantOrigemPeloMapeamentoPersistido_TresChamadasCanonicas()
    {
        var service = Read("backend/PlantaoPro.Api/Saude360ClinicalService.cs");

        Assert.Contains("private async Task<Guid?> ResolverTenantOrigemAsync(IDbConnection cn)", service);
        Assert.Contains("plantaopro.clientes c where c.id=@escopo", service);
        Assert.Contains("plantaopro.tenants t where t.cliente_id=@escopo", service);
        Assert.Contains("plantaopro.tenants tt where tt.id=@escopo", service);
        // Criacao generica (CriarAsync) + os dois writers que contaminavam (favoritar CID, usar modelo).
        Assert.Equal(3, service.Split("ResolverTenantOrigemAsync(cn);", StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void MigracaoV2336_BackfillCondicionalIdempotente_ComRegistroEmSchemaMigrations()
    {
        var sql = Read("database/migrations/2026_10_v2336_e13_p2_backfill_tenant_id_nucleo_clinico.sql");

        Assert.Contains("foreach t in array array[", sql);
        Assert.Contains("column_name = 'tenant_id'", sql);
        // So toca NULL ou contaminado (= cliente_id do registro): valores distintos intencionais ficam intactos.
        Assert.Contains("(x.tenant_id is null or x.tenant_id = x.cliente_id)", sql);
        foreach (var tabela in new[] { "'pacientes'", "'consultas'", "'triagens'", "'agendamentos'", "'cid_favoritos'", "'prescricoes'", "'clinica_unidades_atendimento'" })
            Assert.Contains(tabela, sql);
        Assert.Contains("'v2336'", sql);
    }

    [Fact]
    public async Task Criar_GravaTenantOrigemResolvidoDoMapeamentoClientes()
    {
        var cs = TestDatabase.ConnectionString;
        var tenant = Guid.NewGuid();
        var cliente = Guid.NewGuid();
        await using (var seed = new NpgsqlConnection(cs))
        {
            await seed.OpenAsync();
            await seed.ExecuteAsync(@"insert into plantaopro.tenants(id, tenant_id, codigo, nome, status, reg_status)
values(@id, @id, @cod, 'Tenant P2 tenant_id', 'ATIVO', 'A')", new { id = tenant, cod = "p2-" + tenant.ToString("N")[..12] });
            await seed.ExecuteAsync(@"insert into plantaopro.clientes(id, razao_social, tenant_id, status, reg_status)
values(@id, 'Cliente P2 mapeado', @t, 'ATIVO', 'A')", new { id = cliente, t = tenant });
        }

        var nome = "Conv P2 " + Guid.NewGuid().ToString("N")[..10];
        try
        {
            var svc = BuildService(cliente);
            var criar = await svc.CriarAsync("convenios", new Saude360CreateRequest { Nome = nome, Codigo = Guid.NewGuid().ToString("N")[..8] });
            Assert.True(criar.Success, "Criar convenio deveria ter sucesso: " + criar.Message);

            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            Assert.Equal(cliente, await cn.ExecuteScalarAsync<Guid>("select cliente_id from plantaopro.convenios where nome=@nome", new { nome }));
            Assert.Equal(tenant, await cn.ExecuteScalarAsync<Guid?>("select tenant_id from plantaopro.convenios where nome=@nome", new { nome }));
        }
        finally
        {
            await using var limpa = new NpgsqlConnection(cs);
            await limpa.OpenAsync();
            await limpa.ExecuteAsync("delete from plantaopro.convenios where nome=@nome", new { nome });
            await limpa.ExecuteAsync("delete from plantaopro.clientes where id=@id", new { id = cliente });
            await limpa.ExecuteAsync("delete from plantaopro.tenants where id=@id", new { id = tenant });
        }
    }

    [Fact]
    public async Task Criar_ClienteSemMapeamentoTenant_PersisteTenantIdNulo_Honesto()
    {
        var cs = TestDatabase.ConnectionString;
        var cliente = Guid.NewGuid();
        await using (var seed = new NpgsqlConnection(cs))
        {
            await seed.OpenAsync();
            await seed.ExecuteAsync(@"insert into plantaopro.clientes(id, razao_social, status, reg_status)
values(@id, 'Cliente P2 sem tenant', 'ATIVO', 'A')", new { id = cliente });
        }

        var nome = "Conv P2-nulo " + Guid.NewGuid().ToString("N")[..10];
        try
        {
            var svc = BuildService(cliente);
            var criar = await svc.CriarAsync("convenios", new Saude360CreateRequest { Nome = nome, Codigo = Guid.NewGuid().ToString("N")[..8] });
            Assert.True(criar.Success, "Criar convenio deveria ter sucesso: " + criar.Message);

            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            Assert.Equal(cliente, await cn.ExecuteScalarAsync<Guid>("select cliente_id from plantaopro.convenios where nome=@nome", new { nome }));
            Assert.Null(await cn.ExecuteScalarAsync<Guid?>("select tenant_id from plantaopro.convenios where nome=@nome", new { nome }));
        }
        finally
        {
            await using var limpa = new NpgsqlConnection(cs);
            await limpa.OpenAsync();
            await limpa.ExecuteAsync("delete from plantaopro.convenios where nome=@nome", new { nome });
            await limpa.ExecuteAsync("delete from plantaopro.clientes where id=@id", new { id = cliente });
        }
    }
}
