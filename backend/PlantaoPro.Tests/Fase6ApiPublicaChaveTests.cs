using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Data;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// P1 (homologação): a API pública (/api/public/v1/*) passa a exigir credencial própria —
/// header X-Api-Key validado contra o hash persistido em plantaopro.api_keys.
/// Regras validadas aqui (mesmo mecanismo usado pelo filtro ChavePublicaValida do controller):
///   1. chave ausente/em branco/desconhecida  => não autoriza (null);
///   2. chave ATIVA e dentro da validade       => autoriza e devolve o tenant dono da chave;
///   3. validação bem-sucedida registra ultimo_uso_em;
///   4. chave REVOGADA                          => não autoriza mais;
///   5. chave ATIVA mas expirada                => não autoriza.
/// O vínculo de tenant vem da própria chave (rota anônima, sem sessão do aplicativo).
/// </summary>
public sealed class Fase6ApiPublicaChaveTests
{
    private static readonly Guid TenantSantaCasa = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
    private static readonly Guid UsuarioSeed = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647511");

    private static async Task<Fase6BiIntegracoesService> CriarServicoAsync()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = TestDatabase.ConnectionString
        }).Build();
        return new Fase6BiIntegracoesService(
            cfg,
            NullLogger<Fase6BiIntegracoesService>.Instance,
            new UsuarioContextService(new HttpContextAccessor()),
            new AuditService(cfg, NullLogger<AuditService>.Instance));
    }

    private static string HashDe(string chave) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(chave))).ToLowerInvariant();

    private async Task<Guid> InserirChaveAsync(NpgsqlConnection cn, string raw, string status = "ATIVA", DateTime? expiraEm = null)
    {
        var id = Guid.NewGuid();
        await cn.ExecuteAsync(@"
            insert into plantaopro.api_keys(id, cliente_id, nome, prefixo, chave_hash, criado_por, status, expira_em)
            values(@id, @tenant, 'wps1-teste-publica', 'pp_wps1test', @hash, @uid, @status, @expira)",
            new { id, tenant = TenantSantaCasa, hash = HashDe(raw), uid = UsuarioSeed, status, expira = expiraEm });
        return id;
    }

    [Fact]
    public async Task chave_ativa_autoriza_devolve_tenant_e_registra_uso_enquanto_invalidas_nao_autorizam()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var svc = await CriarServicoAsync();
        var raw = "pp_" + Guid.NewGuid().ToString("N");
        Guid? chaveId = null;
        try
        {
            // 1. ausente / em branco / desconhecida => null (401 no controller)
            Assert.Null(await svc.ValidarChavePublicaAsync(null));
            Assert.Null(await svc.ValidarChavePublicaAsync("   "));
            Assert.Null(await svc.ValidarChavePublicaAsync("pp_desconhecida_sem_persistencia"));

            // 2. ATIVA e dentro da validade => tenant dono
            chaveId = await InserirChaveAsync(cn, raw);
            Assert.Equal(TenantSantaCasa, await svc.ValidarChavePublicaAsync(raw));

            // 3. uso registrado
            var uso = await cn.ExecuteScalarAsync<DateTime?>("select ultimo_uso_em from plantaopro.api_keys where id=@id", new { id = chaveId.Value });
            Assert.NotNull(uso);

            // 4. revogada => para de autorizar (mesmo hash, mesmo raw)
            await cn.ExecuteAsync("update plantaopro.api_keys set status='REVOGADA' where id=@id", new { id = chaveId.Value });
            Assert.Null(await svc.ValidarChavePublicaAsync(raw));
        }
        finally
        {
            if (chaveId.HasValue) await cn.ExecuteAsync("delete from plantaopro.api_keys where id=@id", new { id = chaveId.Value });
        }
    }

    [Fact]
    public async Task chave_expirada_nao_autoriza_apesar_de_status_ativa()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var svc = await CriarServicoAsync();
        var raw = "pp_" + Guid.NewGuid().ToString("N");
        var id = await InserirChaveAsync(cn, raw, "ATIVA", DateTime.UtcNow.AddMinutes(-5));
        try
        {
            Assert.Null(await svc.ValidarChavePublicaAsync(raw));
        }
        finally
        {
            await cn.ExecuteAsync("delete from plantaopro.api_keys where id=@id", new { id });
        }
    }
}
