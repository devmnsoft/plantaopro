using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
/// R5-C7/C8 (onboarding adaptado ao contrato + conclusao derivada de dados), contra o
/// PostgreSQL local (plantaopro_test), no molde do B5: GUIDs fixos com prefixo b7, semente
/// idempotente em uma unica transacao (purga + insert) para reruns previsiveis.
///
/// C1  Iniciar so materializa o catalogo valido: nucleo geral sempre; etapas de modulo
///     apenas com contrato efetivo (predicado de vigencia B4/B6). Nunca mais 11 fixas.
/// C2  Sincronizar adiciona etapas quando o contrato entra em vigor e remove (soft) as
///     etapas cujo contrato deixou de valer, preservando o historico.
/// C3  Conclusao derivada: etapa com criterio so vira CONCLUIDO quando o dado persistido
///     existe (origem AUTOMATICO); se o dado sumir, a etapa volta a pendente.
/// C4  Concluir por clique sem criterio atendido devolve 409 honesto (nada de sucesso ficticio).
/// C5  Pular so vale para etapa opcional e fica persistido (justificativa no banco); obrigatoria
///     nao pula (409); restaurar devolve a etapa ao checklist.
/// C6  Finalizacao derivada: master vira FINALIZADO 100% quando todas as obrigatorias do escopo
///     estao atendidas em dados; opcional pendente nao trava; remover dado reverte o quadro.
/// </summary>
[Collection("saas-operacao-serial")]
public sealed class OnboardingC7Tests : IDisposable
{
    private static readonly Guid C7Tenant = Guid.Parse("b7000001-0000-4000-9000-000000000010");
    private static readonly Guid C7Cliente = Guid.Parse("b7000001-0000-4000-9000-000000000011");
    private static readonly Guid C7Autor = Guid.Parse("b7000001-0000-4000-9000-000000000012");
    private static readonly Guid C7Perfil = Guid.Parse("b7000001-0000-4000-9000-000000000013");

    private readonly Task SementePronta = SementarAsync();

    public void Dispose()
    {
        SementePronta.GetAwaiter().GetResult();
        PurgarAsync().GetAwaiter().GetResult();
    }

    // ------------------------------------------------------------------
    // C1 - materializacao fiel ao contrato (nada de lista generica fixa).
    // ------------------------------------------------------------------
    [Fact]
    public async Task C1_Iniciar_SemContrato_MaterializaSoNucleoDoCatalogo()
    {
        await SementePronta;
        var svc = NovoJornada();

        var r = await svc.IniciarAsync(C7Tenant, C7Cliente);
        Assert.True(r.Success, r.Message);
        var codigos = r.Data!.Select(d => d.Codigo).ToList();

        var nucleo = await ConsultarListaAsync(
            "select codigo from plantaopro.onboarding_etapas_catalogo where reg_status='A' and modulo_codigo is null");
        Assert.True(nucleo.Count >= 4, "catalogo geral esperado no banco");
        foreach (var c in nucleo) Assert.Contains(c, codigos);

        // Sem contrato de modulo, nenhuma etapa de modulo pode existir.
        Assert.DoesNotContain("ONB_PL_CRIAR", codigos);
        Assert.DoesNotContain("ONB_SD_UNIDADE", codigos);
        Assert.DoesNotContain("ONB_AD_OPERACAO", codigos);
        Assert.Equal(0, await ContarAsync(
            "select count(*) from plantaopro.tenant_onboarding_checklist where tenant_id=@t and reg_status='A' and modulo_codigo is not null",
            new { t = C7Tenant }));

        // Idempotencia: iniciar de novo nao duplica etapa alguma (indice unico arbitra).
        var r2 = await svc.IniciarAsync(C7Tenant, C7Cliente);
        Assert.True(r2.Success, r2.Message);
        Assert.Equal(codigos.OrderBy(x => x), r2.Data!.Select(d => d.Codigo).OrderBy(x => x));
    }

    // ------------------------------------------------------------------
    // C2 - sincronizar acompanha os contratos efetivos (entra/sai com historico).
    // ------------------------------------------------------------------
    [Fact]
    public async Task C2_Sincronizar_ContratoEntraESai_PreservandoHistorico()
    {
        await SementePronta;
        var svc = NovoJornada();
        await svc.IniciarAsync(C7Tenant, C7Cliente);

        var plantoes = await IdDoModuloAsync("PLANTOES");
        var saude = await IdDoModuloAsync("SAUDE360");

        // Contrato futuro (AGENDADO com inicio depois de agora) NAO vale: nada entra.
        await InserirContratoAsync(saude, "SAUDE360", "AGENDADO", "now() + interval '30 days'");
        await svc.SincronizarAsync(C7Tenant, C7Cliente);
        var aposFuturo = await ListaCodigosAsync(svc);
        Assert.DoesNotContain("ONB_SD_UNIDADE", aposFuturo);

        // Contrato ATIVO entra em vigor e o sincronizar adiciona as etapas do modulo.
        await InserirContratoAsync(plantoes, "PLANTOES", "ATIVO", "now() - interval '1 minute'");
        await svc.SincronizarAsync(C7Tenant, C7Cliente);
        var aposAtivo = await ListaCodigosAsync(svc);
        Assert.Contains("ONB_PL_CRIAR", aposAtivo);
        Assert.Contains("ONB_PL_ESCALA", aposAtivo);

        // Etapa ja atendida por dados permanece concluida nos ciclos de sincronizar (historico preservado).
        await CompletarDadosEmpresaAsync();
        await svc.ReavaliarAsync(C7Tenant, C7Cliente);

        // Contrato sai de vigor (desativado_em) e o sincronizar remove (soft) as etapas dele.
        await ExecutarAsync("update plantaopro.tenant_modulos set desativado_em=now() where tenant_id=@t and codigo_modulo='PLANTOES'", new { t = C7Tenant });
        await svc.SincronizarAsync(C7Tenant, C7Cliente);
        var aposSaida = await ListaCodigosAsync(svc);
        Assert.DoesNotContain("ONB_PL_CRIAR", aposSaida);
        Assert.True(await ContarAsync(
            "select count(*) from plantaopro.tenant_onboarding_checklist where tenant_id=@t and codigo='ONB_PL_CRIAR' and reg_status='I'",
            new { t = C7Tenant }) >= 1, "saida deve ser soft (historico mantido)");
        Assert.True(await ConsultarAsync<bool>(
            "select coalesce(concluido,false) from plantaopro.tenant_onboarding_checklist where tenant_id=@t and codigo='ONB_EMPRESA_DADOS' and reg_status='A'",
            new { t = C7Tenant }), "etapa concluida por dados nao pode sumir com a saida de outro modulo");
    }

    // ------------------------------------------------------------------
    // C3 - conclusao derivada dos dados e reversao quando o dado some.
    // ------------------------------------------------------------------
    [Fact]
    public async Task C3_ConclusaoDerivada_DeDados_E_ReverteQuandoDadoSumiu()
    {
        await SementePronta;
        var svc = NovoJornada();
        var inicio = await svc.IniciarAsync(C7Tenant, C7Cliente);
        var empresa = inicio.Data!.First(d => d.Codigo == "ONB_EMPRESA_DADOS");
        Assert.False(empresa.Atendida);

        await ExecutarAsync(@"update plantaopro.clientes set email='c7@exemplo.test', telefone='81999990000', cidade='Recife', estado='PE' where id=@c", new { c = C7Cliente });
        var apos = (await svc.ReavaliarAsync(C7Tenant, C7Cliente)).Data!;
        var empresaOk = apos.First(d => d.Codigo == "ONB_EMPRESA_DADOS");
        Assert.True(empresaOk.Atendida, empresaOk.Evidencia);

        var origem = await ConsultarAsync<string>(
            "select coalesce(origem_conclusao,'') from plantaopro.tenant_onboarding_checklist where tenant_id=@t and codigo='ONB_EMPRESA_DADOS' and reg_status='A'",
            new { t = C7Tenant });
        Assert.Equal("AUTOMATICO", origem);

        // O dado sumiu: a verdade segue os dados, nao o botao.
        await ExecutarAsync("update plantaopro.clientes set email=null where id=@c", new { c = C7Cliente });
        var revertido = (await svc.ReavaliarAsync(C7Tenant, C7Cliente)).Data!;
        Assert.False(revertido.First(d => d.Codigo == "ONB_EMPRESA_DADOS").Atendida);
        Assert.False(await ConsultarAsync<bool>(
            "select coalesce(concluido,false) from plantaopro.tenant_onboarding_checklist where tenant_id=@t and codigo='ONB_EMPRESA_DADOS' and reg_status='A'",
            new { t = C7Tenant }));
    }

    // ------------------------------------------------------------------
    // C4 - concluir por clique sem criterio atendido e 409 honesto.
    // ------------------------------------------------------------------
    [Fact]
    public async Task C4_Concluir_SemCriterioAtendido_409_SemSucessoFicticio()
    {
        await SementePronta;
        var svc = NovoJornada();
        var inicio = await svc.IniciarAsync(C7Tenant, C7Cliente);
        var convite = inicio.Data!.First(d => d.Codigo == "ONB_EQUIPE_CONVIDAR");
        Assert.False(convite.Atendida);

        var r = await svc.ConcluirEtapaAsync(C7Tenant, C7Cliente, convite.Id);
        Assert.False(r.Success);
        Assert.Equal(409, r.StatusCode);
        Assert.False(await ConsultarAsync<bool>(
            "select coalesce(concluido,false) from plantaopro.tenant_onboarding_checklist where id=@i", new { i = convite.Id }));
    }

    // ------------------------------------------------------------------
    // C5 - pular so etapa opcional, persistido; obrigatoria nao pula; restaurar funciona.
    // ------------------------------------------------------------------
    [Fact]
    public async Task C5_Pular_SoOpcional_Persistida_E_Obrigatoria409()
    {
        await SementePronta;
        var svc = NovoJornada();
        var inicio = await svc.IniciarAsync(C7Tenant, C7Cliente);
        var identidade = inicio.Data!.First(d => d.Codigo == "ONB_IDENTIDADE");
        var empresa = inicio.Data!.First(d => d.Codigo == "ONB_EMPRESA_DADOS");

        var ruim = await svc.PularEtapaAsync(C7Tenant, C7Cliente, empresa.Id, "nao deveria deixar");
        Assert.False(ruim.Success);
        Assert.Equal(409, ruim.StatusCode);

        var ok = await svc.PularEtapaAsync(C7Tenant, C7Cliente, identidade.Id, "identidade fica para a fase 2");
        Assert.True(ok.Success, ok.Message);
        var pulada = await ConsultarAsync<(bool, string?)>(
            "select coalesce(pulada,false), motivo_pular from plantaopro.tenant_onboarding_checklist where id=@i", new { i = identidade.Id });
        Assert.True(pulada.Item1);
        Assert.Equal("identidade fica para a fase 2", pulada.Item2);

        // Reavaliar nao desfaz o pulo nem trata opcional pulada como pendente.
        var apos = (await svc.ReavaliarAsync(C7Tenant, C7Cliente)).Data!;
        Assert.True(apos.First(d => d.Codigo == "ONB_IDENTIDADE").Pulada);

        var restaurada = await svc.RestaurarEtapaAsync(C7Tenant, C7Cliente, identidade.Id);
        Assert.True(restaurada.Success, restaurada.Message);
        Assert.False(await ConsultarAsync<bool>(
            "select coalesce(pulada,false) from plantaopro.tenant_onboarding_checklist where id=@i", new { i = identidade.Id }));
    }

    // ------------------------------------------------------------------
    // C6 - finalizacao derivada de dados reais (e reversao do master ao perder dado).
    // ------------------------------------------------------------------
    [Fact]
    public async Task C6_Finalizado_QuandoTodasObrigatoriasAtendidas_EmDados()
    {
        await SementePronta;
        var svc = NovoJornada();
        await svc.IniciarAsync(C7Tenant, C7Cliente);

        await CompletarDadosEmpresaAsync();
        await ExecutarAsync(@"insert into plantaopro.usuario_convites(id,tenant_id,cliente_id,email,perfil_ids,token_hash,expira_em,criado_por,reg_date,reg_status)
values(gen_random_uuid(),@t,@c,'c7-convite@exemplo.test',array[@p]::uuid[],'c7-hash-token-unico',now() + interval '7 days',@a,now(),'A')",
            new { t = C7Tenant, c = C7Cliente, p = C7Perfil, a = C7Autor });

        // Falta o aceite do convite: membro pendente => ainda nao finalizado.
        var meio = await svc.ReavaliarAsync(C7Tenant, C7Cliente);
        Assert.False(meio.Data!.First(d => d.Codigo == "ONB_EQUIPE_ACESSO").Atendida);
        Assert.NotEqual("FINALIZADO", await StatusMasterAsync());

        await ExecutarAsync("update plantaopro.usuario_convites set usado_em=now(), usado_por=@a where tenant_id=@t", new { a = C7Autor, t = C7Tenant });
        var fim = await svc.ReavaliarAsync(C7Tenant, C7Cliente);

        foreach (var obrigatoria in fim.Data!.Where(d => d.Obrigatorio))
            Assert.True(obrigatoria.Atendida, obrigatoria.Codigo + ": " + obrigatoria.Evidencia);
        Assert.Equal("FINALIZADO", await StatusMasterAsync());
        Assert.Equal(100, await ContarAsync("select progresso from plantaopro.tenant_onboarding where tenant_id=@t and reg_status='A'", new { t = C7Tenant }));
        Assert.True(await ConsultarAsync<bool>("select finalizado_em is not null from plantaopro.tenant_onboarding where tenant_id=@t and reg_status='A'", new { t = C7Tenant }));

        // Identidade (opcional) seguiu pendente sem travar nada.
        Assert.False(fim.Data!.First(d => d.Codigo == "ONB_IDENTIDADE").Obrigatorio);

        // Perder dado obrigatorio reverte o master honestamente.
        await ExecutarAsync("update plantaopro.clientes set telefone=null where id=@c", new { c = C7Cliente });
        await svc.ReavaliarAsync(C7Tenant, C7Cliente);
        Assert.NotEqual("FINALIZADO", await StatusMasterAsync());
    }

    // ------------------------------------------------------------------
    // Infra de teste (molde B5).
    // ------------------------------------------------------------------
    private static IConfiguration BuildCfg() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
        .Build();

    private static OnboardingJornadaService NovoJornada() =>
        new(BuildCfg(), NullLogger<OnboardingJornadaService>.Instance);

    private async Task<List<string>> ListaCodigosAsync(OnboardingJornadaService svc)
    {
        var r = await svc.ReavaliarAsync(C7Tenant, C7Cliente);
        Assert.True(r.Success, r.Message);
        return r.Data!.Select(d => d.Codigo).ToList();
    }

    private Task<string> StatusMasterAsync() => ConsultarAsync<string>(
        "select status from plantaopro.tenant_onboarding where tenant_id=@t and reg_status='A'", new { t = C7Tenant });

    private async Task CompletarDadosEmpresaAsync() => await ExecutarAsync(
        "update plantaopro.clientes set email='c7@exemplo.test', telefone='81999990000', cidade='Recife', estado='PE' where id=@c", new { c = C7Cliente });

    private static async Task<Guid> IdDoModuloAsync(string codigo) => await ConsultarAsync<Guid>(
        "select id from plantaopro.modulos_sistema where upper(codigo)=@c and reg_status='A'", new { c = codigo });

    private static async Task InserirContratoAsync(Guid moduloId, string codigo, string status, string ativadoEm)
    {
        await ExecutarAsync($@"insert into plantaopro.tenant_modulos (id, tenant_id, modulo_id, codigo_modulo, habilitado, status, origem, ativado_em, desativado_em, reg_status)
values (gen_random_uuid(), @t, @m, @c, true, @s, 'ASSINATURA', {ativadoEm}, null, 'A')",
            new { t = C7Tenant, m = moduloId, c = codigo, s = status });
    }

    private static async Task<T> ConsultarAsync<T>(string sql, object parametros)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        return await cn.QuerySingleAsync<T>(sql, parametros);
    }

    private static async Task<List<string>> ConsultarListaAsync(string sql)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        return (await cn.QueryAsync<string>(sql)).ToList();
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
delete from plantaopro.tenant_white_label where tenant_id=@tenant;
delete from plantaopro.usuario_convites where tenant_id=@tenant;
delete from plantaopro.tenant_modulos where tenant_id=@tenant;
delete from plantaopro.perfis where id=@perfil;
delete from plantaopro.usuarios where id=@autor;
delete from plantaopro.clientes where id=@cliente;
delete from plantaopro.tenants where id=@tenant;";

    private const string InsertSql = @"
insert into plantaopro.tenants (id, tenant_id, codigo, nome, status, reg_status) values
  (@tenant, @tenant, 'C7TENT', 'C7 Tenant Onboarding', 'ATIVO', 'A');
insert into plantaopro.clientes (id, razao_social, cnpj, status, reg_status) values
  (@cliente, 'C7 Cliente LTDA', '33221144000199', 'ATIVO', 'A');
insert into plantaopro.perfis (id, tenant_id, codigo, nome, status, reg_status) values
  (@perfil, @tenant, 'C7PERFIL', 'C7 Perfil Equipe', 'ATIVO', 'A');
insert into plantaopro.usuarios (id, tenant_id, cliente_id, nome, email, email_normalizado, senha_hash, status, reg_status, senha_alteracao_obrigatoria, preferencias_notificacao, reg_date) values
  (@autor, @tenant, @cliente, 'C7 Autor', 'c7-autor@exemplo.test', 'C7-AUTOR@EXEMPLO.TEST', '{c7}', 'ATIVO', 'A', false, '{}', now());";

    private static object SeedParams() => new
    {
        tenant = C7Tenant,
        cliente = C7Cliente,
        autor = C7Autor,
        perfil = C7Perfil
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
