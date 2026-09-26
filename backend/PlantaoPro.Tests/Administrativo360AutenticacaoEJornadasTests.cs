using System.Security.Cryptography;
using Dapper;
using Npgsql;
using PlantaoPro.CrossCutting.Security;
using Xunit;

namespace PlantaoPro.Tests;

public sealed class Administrativo360AutenticacaoEJornadasTests
{
    private static readonly Guid TenantSantaCasa = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
    private static readonly Guid TenantIsolado = Guid.Parse("e0e0e0e0-e0e0-e0e0-e0e0-e0e0e0e0e0e0");

    private static string ObterConnectionString()
    {
        return Environment.GetEnvironmentVariable("PLANTAOPRO_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=127.0.0.1;Port=5432;Database=plantaopro_test;Username=postgres;Password=123456;Pooling=true;Maximum Pool Size=50;Minimum Pool Size=0;Timeout=30;Command Timeout=60;Search Path=PlantaoPro,public;Application Name=PlantaoPro.tests";
    }

    [Fact]
    public async Task Etapa1_MigrationV2197_ReconciliouColunaModuloIdEPreservouInativos()
    {
        var cs = ObterConnectionString();
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();

        // 1. Validar que a coluna modulo_id existe e é foreign key
        var hasModuloId = await cn.ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1 FROM information_schema.columns 
                WHERE table_schema = 'plantaopro' AND table_name = 'tenant_modulos' AND column_name = 'modulo_id'
            );");
        Assert.True(hasModuloId, "A coluna tm.modulo_id deve existir após a migração v2197.");

        // 2. Validar que o módulo ADM360 está ativo para o tenant Santa Casa
        var activeModules = (await cn.QueryAsync<string>(@"
            SELECT DISTINCT upper(ms.codigo)
            FROM plantaopro.tenant_modulos tm
            JOIN plantaopro.modulos_sistema ms ON ms.id = tm.modulo_id AND ms.reg_status = 'A' AND upper(ms.status) = 'ATIVO'
            WHERE tm.tenant_id = @TenantSantaCasa AND tm.reg_status = 'A' AND tm.habilitado = true AND upper(tm.status) = 'ATIVO'
            ORDER BY 1;", new { TenantSantaCasa })).ToList();

        Assert.Contains("ADM360", activeModules);

        // 3. Validar que contratos inativos ou bloqueados são preservados na tabela
        var inativoExiste = await cn.ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1 FROM plantaopro.tenant_modulos tm
                WHERE tm.tenant_id = @TenantSantaCasa AND (tm.habilitado = false OR upper(tm.status) = 'INATIVO')
            );", new { TenantSantaCasa });
        Assert.True(inativoExiste, "Contratos inativos devem ser preservados para auditoria e histórico.");
    }

    [Fact]
    public async Task Etapa1_AutenticacaoReal_GestorPossuiCredenciaisPerfilEModuloAdm360()
    {
        var cs = ObterConnectionString();
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();

        // Buscar usuário gestor Santa Casa
        var user = await cn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT u.id, u.nome, u.email, u.senha_hash, u.tenant_id, u.status
            FROM plantaopro.usuarios u
            WHERE lower(u.email) = 'gestor@santacasa-demo.example' AND u.reg_status = 'A';");

        Assert.NotNull(user);
        Assert.Equal(TenantSantaCasa, (Guid)user.tenant_id);
        Assert.Equal("ATIVO", (string)user.status);

        // Validar hash BCrypt com a senha documentada
        string senhaCorreta = "SantaCasa!Demo2026#Gestor";
        bool senhaValida = PlantaoPro.Api.Security.PasswordHashService.Verify(senhaCorreta, (string)user.senha_hash);
        Assert.True(senhaValida, "A senha documentada deve bater com o hash BCrypt no banco.");

        // Validar perfis atribuídos
        var perfis = (await cn.QueryAsync<string>(@"
            SELECT pf.codigo
            FROM plantaopro.usuarios_perfis up
            JOIN plantaopro.perfis pf ON pf.id = up.perfil_id AND pf.reg_status = 'A'
            WHERE up.usuario_id = @UsuarioId AND up.reg_status = 'A';",
            new { UsuarioId = (Guid)user.id })).ToList();

        Assert.Contains("ADMINISTRADOR_CLIENTE", perfis);

        // Validar módulos carregados canonicamente
        var modulos = (await cn.QueryAsync<string>(@"
            SELECT DISTINCT upper(ms.codigo)
            FROM plantaopro.tenant_modulos tm
            JOIN plantaopro.modulos_sistema ms ON ms.id = tm.modulo_id AND ms.reg_status = 'A' AND upper(ms.status) = 'ATIVO'
            WHERE tm.tenant_id = @TenantId AND tm.reg_status = 'A' AND tm.habilitado = true AND upper(tm.status) = 'ATIVO';",
            new { TenantId = (Guid)user.tenant_id })).ToList();

        Assert.Contains("ADM360", modulos);
    }

    [Fact]
    public async Task Etapa1_IsolamentoMultiTenant_DadosSantaCasaNaoVazamParaOutroTenant()
    {
        var cs = ObterConnectionString();
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();

        // Contar orçamentos do tenant Santa Casa
        var countSantaCasa = await cn.ExecuteScalarAsync<int>(@"
            SELECT count(*) FROM plantaopro.adm360_orcamentos WHERE tenant_id = @TenantSantaCasa;",
            new { TenantSantaCasa });

        Assert.True(countSantaCasa > 0, "Tenant Santa Casa deve ter orçamentos inseridos pelo seed.");

        // Consultar orçamentos do outro tenant
        var countOutroTenant = await cn.ExecuteScalarAsync<int>(@"
            SELECT count(*) FROM plantaopro.adm360_orcamentos WHERE tenant_id = @TenantIsolado;",
            new { TenantIsolado });

        Assert.Equal(0, countOutroTenant);

        // Mesma verificação para produtos
        var produtosOutroTenant = await cn.ExecuteScalarAsync<int>(@"
            SELECT count(*) FROM plantaopro.adm360_produtos WHERE tenant_id = @TenantIsolado;",
            new { TenantIsolado });
        Assert.Equal(0, produtosOutroTenant);

        var cotsOutroTenant = await cn.ExecuteScalarAsync<int>(@"
            SELECT count(*) FROM plantaopro.adm360_cotacoes WHERE tenant_id = @TenantIsolado;",
            new { TenantIsolado });
        Assert.Equal(0, cotsOutroTenant);
    }

    [Fact]
    public async Task Etapa2_CadastrosLookup_RetornaEntidadesRelacionadasComSucesso()
    {
        var cs = ObterConnectionString();
        var repo = new PlantaoPro.Infrastructure.Administrativo360.CadastrosRepository(cs);

        var lookups = await repo.ObterLookupsAsync(TenantSantaCasa, default);
        Assert.NotNull(lookups);
        Assert.NotEmpty(lookups.Parceiros);
        Assert.NotEmpty(lookups.Produtos);
        Assert.NotEmpty(lookups.Locais);
    }

    [Fact]
    public void Etapa5_ViewsDeCadastro_NaoExigemGuidDigitado()
    {
        var root = RepositoryPathResolver.RepoRoot;
        var orcamentoNovo = File.ReadAllText(Path.Combine(root, "backend/PlantaoPro.Web/Views/Administrativo360/OrcamentoNovo.cshtml"));
        var cirurgiaNova = File.ReadAllText(Path.Combine(root, "backend/PlantaoPro.Web/Views/Administrativo360/CirurgiaNova.cshtml"));
        var valeNovo = File.ReadAllText(Path.Combine(root, "backend/PlantaoPro.Web/Views/Administrativo360/ValeNovo.cshtml"));

        // Nenhum input deve pedir GUID text
        Assert.DoesNotContain("placeholder=\"00000000-0000", orcamentoNovo);
        Assert.DoesNotContain("placeholder=\"00000000-0000", cirurgiaNova);
        Assert.DoesNotContain("placeholder=\"00000000-0000", valeNovo);

        // Todos devem usar selects populados
        Assert.Contains("HospitalId", orcamentoNovo);
        Assert.Contains("<select", orcamentoNovo);
        Assert.Contains("HospitalId", cirurgiaNova);
        Assert.Contains("<select", cirurgiaNova);
        Assert.Contains("HospitalId", valeNovo);
        Assert.Contains("<select", valeNovo);
    }
}
