using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R7-OB (v2345): jornada de onboarding — etapa "Definir o idioma da interface"
/// (ONB_PREFERENCIAS), núcleo, opcional, com critério DERIVADO DE DADOS — mesma filosofia C7.
///
/// R7-E1 (v2344) persistiu a preferência (usuarios.idioma_preferido) e o padrão da organização
/// (tenants.metadata->>'idiomaPadrao'); esta etapa expõe a configuração na jornada do contrato:
/// concluída quando o padrão organizacional está registrado OU ao menos um usuário ativo tem
/// preferência salva. Opcional porque nada definido = pt-BR por omissão (não trava o onboarding).
///
/// Molde das C7: GUIDs fixos (prefixo 7ob), semente idempotente por teste em transação única
/// (purga + insert), coleção serial do banco de operação.
///
/// O1  Materializacao fiel ao catalogo: sem contrato de modulo a etapa entra no nucleo, opcional,
///     com criterio/codigo/ordem/link corretos; rerun do iniciar nao duplica a linha.
/// O2  Conclusao derivada pela preferencia do usuario (v2344) e reversao quando a preferencia some.
/// O3  Alternativa pelo padrao da organizacao (metadata) nomeando o idioma na evidencia; limpar reverte.
/// O4  Etapa opcional: pulavel com justificativa persistida; restaurar volta ao estado sem duplicar.
/// O5  Clique sem dado nao conclui etapa com criterio (409, sem sucesso ficticio).
/// </summary>
[Collection("saas-operacao-serial")]
public sealed class R7OBOnboardingPreferenciasIdiomaTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7a0b0001-0000-4000-9000-000000000010");
    private static readonly Guid Cliente = Guid.Parse("7a0b0001-0000-4000-9000-000000000011");
    private static readonly Guid Admin = Guid.Parse("7a0b0001-0000-4000-9000-000000000012");

    private readonly Task SementePronta = SementarAsync();

    public void Dispose()
    {
        SementePronta.GetAwaiter().GetResult();
        PurgarAsync().GetAwaiter().GetResult();
    }

    // ------------------------------------------------------------------
    // O1 - materializacao fiel ao catalogo (sem contrato de modulo) + idempotencia.
    // ------------------------------------------------------------------
    [Fact]
    public async Task O1_Iniciar_MaterializaEtapaDeIdioma_OpcionalNoNucleo_Idempotente()
    {
        Assert.Equal(1, await ContarAsync(
            "select count(*) from plantaopro.onboarding_etapas_catalogo where codigo='ONB_PREFERENCIAS' and reg_status='A'", new { }));

        await SementePronta;
        var svc = NovoJornada();

        var r = await svc.IniciarAsync(Tenant, Cliente);
        Assert.True(r.Success, r.Message);
        var etapa = r.Data!.Single(d => d.Codigo == "ONB_PREFERENCIAS");
        Assert.False(etapa.Obrigatorio);
        Assert.Equal("PENDENTE", etapa.Status);
        Assert.False(etapa.Atendida);
        Assert.Equal("IDIOMA_PREFERENCIA_DEFINIDA", etapa.CriterioCodigo);
        Assert.Contains("idioma", etapa.CriterioDescricao, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("/Usuario/Index", etapa.LinkAcao);
        Assert.Equal(50, etapa.Ordem);

        // Rerun do inicio nao duplica a linha do checklist (materializacao idempotente por codigo).
        var r2 = await svc.IniciarAsync(Tenant, Cliente);
        Assert.True(r2.Success, r2.Message);
        Assert.Equal(1, await ContarAsync(
            "select count(*) from plantaopro.tenant_onboarding_checklist where tenant_id=@t and codigo='ONB_PREFERENCIAS' and reg_status='A'",
            new { t = Tenant }));
    }

    // ------------------------------------------------------------------
    // O2 - conclusao derivada pela preferencia do usuario; reverte quando o dado some.
    // ------------------------------------------------------------------
    [Fact]
    public async Task O2_PreferenciaDoUsuario_ConcluiPorDados_E_ReverteQuandoSome()
    {
        await SementePronta;
        var svc = NovoJornada();
        var inicio = await svc.IniciarAsync(Tenant, Cliente);
        Assert.True(inicio.Success, inicio.Message);
        Assert.False(inicio.Data!.Single(d => d.Codigo == "ONB_PREFERENCIAS").Atendida);

        await ExecutarAsync("update plantaopro.usuarios set idioma_preferido='es' where id=@u", new { u = Admin });
        var apos = await svc.ReavaliarAsync(Tenant, Cliente);
        Assert.True(apos.Success, apos.Message);
        var concluida = apos.Data!.Single(d => d.Codigo == "ONB_PREFERENCIAS");
        Assert.True(concluida.Atendida);
        Assert.True(concluida.Concluido);
        Assert.Contains("preferência de idioma salva", concluida.Evidencia, StringComparison.Ordinal);

        // A verdade segue os dados: apagar a preferencia devolve a etapa ao pendente.
        await ExecutarAsync("update plantaopro.usuarios set idioma_preferido=null where id=@u", new { u = Admin });
        var deNovo = await svc.ReavaliarAsync(Tenant, Cliente);
        Assert.True(deNovo.Success, deNovo.Message);
        var revertida = deNovo.Data!.Single(d => d.Codigo == "ONB_PREFERENCIAS");
        Assert.False(revertida.Atendida);
        Assert.False(revertida.Concluido);
    }

    // ------------------------------------------------------------------
    // O3 - alternativa pelo padrao da organizacao (mesmo predicado do login, R7-E1).
    // ------------------------------------------------------------------
    [Fact]
    public async Task O3_PadraoDaOrganizacao_TambemConclui_NomeandoOIdioma_E_ReverteAoLimpar()
    {
        await SementePronta;
        var svc = NovoJornada();
        var inicio = await svc.IniciarAsync(Tenant, Cliente);
        Assert.True(inicio.Success, inicio.Message);

        await ExecutarAsync(
            @"update plantaopro.tenants set metadata=coalesce(metadata,'{}'::jsonb)||'{""idiomaPadrao"":""en""}'::jsonb where id=@t",
            new { t = Tenant });
        var apos = await svc.ReavaliarAsync(Tenant, Cliente);
        Assert.True(apos.Success, apos.Message);
        var concluida = apos.Data!.Single(d => d.Codigo == "ONB_PREFERENCIAS");
        Assert.True(concluida.Atendida);
        Assert.True(concluida.Concluido);
        Assert.Contains("(en)", concluida.Evidencia, StringComparison.Ordinal);

        await ExecutarAsync("update plantaopro.tenants set metadata='{}'::jsonb where id=@t", new { t = Tenant });
        var deNovo = await svc.ReavaliarAsync(Tenant, Cliente);
        Assert.True(deNovo.Success, deNovo.Message);
        var revertida = deNovo.Data!.Single(d => d.Codigo == "ONB_PREFERENCIAS");
        Assert.False(revertida.Atendida);
    }

    // ------------------------------------------------------------------
    // O4 - opcional: pulavel com justificativa persistida; restaurar devolve o estado.
    // ------------------------------------------------------------------
    [Fact]
    public async Task O4_Opcional_PulavelComJustificativaPersistida_E_RestauraSemDuplicar()
    {
        await SementePronta;
        var svc = NovoJornada();
        var inicio = await svc.IniciarAsync(Tenant, Cliente);
        Assert.True(inicio.Success, inicio.Message);
        var etapa = inicio.Data!.Single(d => d.Codigo == "ONB_PREFERENCIAS");

        var ok = await svc.PularEtapaAsync(Tenant, Cliente, etapa.Id, "Operação 100% em português.");
        Assert.True(ok.Success, ok.Message);
        var motivo = await ConsultarAsync<string?>(
            "select motivo_pular from plantaopro.tenant_onboarding_checklist where id=@i", new { i = etapa.Id });
        Assert.Equal("Operação 100% em português.", motivo);

        var aposPular = await svc.ReavaliarAsync(Tenant, Cliente);
        Assert.True(aposPular.Success, aposPular.Message);
        var pulada = aposPular.Data!.Single(d => d.Codigo == "ONB_PREFERENCIAS");
        Assert.True(pulada.Pulada);
        Assert.True(pulada.Atendida);
        Assert.Contains("pulada", pulada.Evidencia, StringComparison.OrdinalIgnoreCase);

        var restaurada = await svc.RestaurarEtapaAsync(Tenant, Cliente, etapa.Id);
        Assert.True(restaurada.Success, restaurada.Message);
        var deNovo = await svc.ReavaliarAsync(Tenant, Cliente);
        Assert.True(deNovo.Success, deNovo.Message);
        var restaura = deNovo.Data!.Single(d => d.Codigo == "ONB_PREFERENCIAS");
        Assert.False(restaura.Pulada);
        Assert.False(restaura.Atendida);
        Assert.Equal(1, await ContarAsync(
            "select count(*) from plantaopro.tenant_onboarding_checklist where tenant_id=@t and codigo='ONB_PREFERENCIAS' and reg_status='A'",
            new { t = Tenant }));
    }

    // ------------------------------------------------------------------
    // O5 - clique sem dados nao conclui etapa com criterio (409, sem sucesso ficticio).
    // ------------------------------------------------------------------
    [Fact]
    public async Task O5_CliqueSemDado_NaoConcluiEtapaComCriterio_409()
    {
        await SementePronta;
        var svc = NovoJornada();
        var inicio = await svc.IniciarAsync(Tenant, Cliente);
        Assert.True(inicio.Success, inicio.Message);
        var etapa = inicio.Data!.Single(d => d.Codigo == "ONB_PREFERENCIAS");

        var r = await svc.ConcluirEtapaAsync(Tenant, Cliente, etapa.Id);
        Assert.False(r.Success);
        Assert.Equal(409, r.StatusCode);
        Assert.False(await ConsultarAsync<bool>(
            "select coalesce(concluido,false) from plantaopro.tenant_onboarding_checklist where id=@i", new { i = etapa.Id }));
    }

    // ------------------------------------------------------------------
    // Infra de teste (molde B5/C7).
    // ------------------------------------------------------------------
    private static IConfiguration BuildCfg() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
        .Build();

    private static OnboardingJornadaService NovoJornada() =>
        new(BuildCfg(), NullLogger<OnboardingJornadaService>.Instance);

    private static async Task<T> ConsultarAsync<T>(string sql, object parametros)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        return await cn.QuerySingleAsync<T>(sql, parametros);
    }

    private static async Task<int> ContarAsync(string sql, object parametros)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        return await cn.QuerySingleAsync<int>(sql, parametros);
    }

    private static async Task ExecutarAsync(string sql, object parametros)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.ExecuteAsync(sql, parametros);
    }

    private const string PurgeSql = @"
delete from plantaopro.tenant_onboarding_checklist where tenant_id=@tenant;
delete from plantaopro.tenant_onboarding where tenant_id=@tenant;
delete from plantaopro.usuarios where id=@admin;
delete from plantaopro.clientes where id=@cliente;
delete from plantaopro.tenants where id=@tenant;";

    private const string InsertSql = @"
insert into plantaopro.tenants (id, tenant_id, codigo, nome, status, reg_status) values
  (@tenant, @tenant, 'R7OB', 'R7OB Tenant Idioma', 'ATIVO', 'A');
insert into plantaopro.clientes (id, razao_social, cnpj, status, reg_status) values
  (@cliente, 'R7OB Cliente LTDA', '33221144000277', 'ATIVO', 'A');
insert into plantaopro.usuarios (id, tenant_id, cliente_id, nome, email, email_normalizado, senha_hash, status, reg_status, senha_alteracao_obrigatoria, preferencias_notificacao, reg_date) values
  (@admin, @tenant, @cliente, 'R7OB Admin', 'r7ob-admin@exemplo.test', 'R7OB-ADMIN@EXEMPLO.TEST', '{r7ob}', 'ATIVO', 'A', false, '{}', now());";

    private static object SeedParams() => new
    {
        tenant = Tenant,
        cliente = Cliente,
        admin = Admin
    };

    private static async Task SementarAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();
        await cn.ExecuteAsync(PurgeSql, SeedParams(), tx);
        await cn.ExecuteAsync(InsertSql, SeedParams(), tx);
        await tx.CommitAsync();
    }

    private static async Task PurgarAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync(PurgeSql, SeedParams());
    }
}
