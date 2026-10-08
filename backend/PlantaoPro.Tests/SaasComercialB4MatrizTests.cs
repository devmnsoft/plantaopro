using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Controllers;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R5-B4 (matriz comercial canônica + estados explícitos), contra o PostgreSQL
/// local (plantaopro_test), no template B6: GUIDs fixos com prefixo b4, semente
/// idempotente em uma única transação (purga + insert) para reruns previsíveis.
///
/// B1  Matriz canônica do plano (plano → módulos → limites → recursos) em um
///     payload, lida das tabelas; 404 honesto para plano inexistente.
/// B2  Aprovar upgrade troca o plano com histórico ALTERAR_PLANO; decisão
///     repetida é idempotente; destino inativo recusa com 409.
/// B3  Recusar exige justificativa (400 sem); cancelamento aprovado cancela a
///     assinatura com motivo/data; tipo×status incoerente recusa com 409.
/// B4  Downgrade com uso acima do destino recusa com 409; dentro do limite aprova.
/// B5  Ativar-agendados: só AGENDADO vencido vira ATIVO (+habilitado, +trilha);
///     segunda chamada não reativa nada.
/// </summary>
[Collection("saas-operacao-serial")]
public sealed class SaasComercialB4MatrizTests : IDisposable
{
    private static readonly Guid B4PlanoA = Guid.Parse("b4000001-0000-4000-9000-000000000001");
    private static readonly Guid B4PlanoB = Guid.Parse("b4000001-0000-4000-9000-000000000002");
    private static readonly Guid B4PlanoInativo = Guid.Parse("b4000001-0000-4000-9000-000000000003");
    private static readonly Guid B4Modulo = Guid.Parse("b4000001-0000-4000-9000-000000000010");
    private static readonly Guid B4Tenant = Guid.Parse("b4000001-0000-4000-9000-000000000020");
    private static readonly Guid B4Cliente1 = Guid.Parse("b4000001-0000-4000-9000-000000000031");
    private static readonly Guid B4Cliente2 = Guid.Parse("b4000001-0000-4000-9000-000000000032");
    private static readonly Guid B4Cliente3 = Guid.Parse("b4000001-0000-4000-9000-000000000033");
    private static readonly Guid B4Cliente4 = Guid.Parse("b4000001-0000-4000-9000-000000000034");
    private static readonly Guid B4Cliente5 = Guid.Parse("b4000001-0000-4000-9000-000000000035");
    private static readonly Guid B4Ass1 = Guid.Parse("b4000001-0000-4000-9000-000000000041");
    private static readonly Guid B4Ass2 = Guid.Parse("b4000001-0000-4000-9000-000000000042");
    private static readonly Guid B4Ass3 = Guid.Parse("b4000001-0000-4000-9000-000000000043");
    private static readonly Guid B4Ass4 = Guid.Parse("b4000001-0000-4000-9000-000000000044");
    private static readonly Guid B4Ass5 = Guid.Parse("b4000001-0000-4000-9000-000000000045");
    private static readonly Guid B4Up1 = Guid.Parse("b4000001-0000-4000-9000-000000000051");
    private static readonly Guid B4Down2 = Guid.Parse("b4000001-0000-4000-9000-000000000052");
    private static readonly Guid B4Down3 = Guid.Parse("b4000001-0000-4000-9000-000000000053");
    private static readonly Guid B4Cancel4 = Guid.Parse("b4000001-0000-4000-9000-000000000054");
    private static readonly Guid B4Up5 = Guid.Parse("b4000001-0000-4000-9000-000000000055");
    private static readonly Guid B4Usuario = Guid.Parse("b4000001-0000-4000-9000-000000000060");

    private readonly Task SementePronta = SementarAsync();

    public void Dispose()
    {
        SementePronta.GetAwaiter().GetResult();
        PurgarAsync().GetAwaiter().GetResult();
    }

    // ---------------------------------------------------------------------
    // B1 - matriz canônica.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task B1_Matriz_PlanoExistente_RetornaPlanoModulosRecursosERegras()
    {
        await SementePronta;
        var controller = NovoPlanosController();

        var resultado = await controller.Matriz(B4PlanoA);
        Assert.Equal(200, StatusDe(resultado));
        var matriz = ((OkObjectResult)resultado).Value as ApiResponse<PlanoMatrizDto>;
        Assert.NotNull(matriz);
        Assert.True(matriz!.Success);
        Assert.Equal("B4 Plano A", matriz.Data!.Plano.Nome);
        var modulo = Assert.Single(matriz.Data.Modulos);
        Assert.Equal("B4MOD", modulo.Codigo);
        Assert.True(modulo.Incluido);
        var recurso = Assert.Single(matriz.Data.Recursos);
        Assert.Equal("SUPORTE", recurso.Codigo);
        Assert.NotEmpty(matriz.Data.Regras);
        Assert.Contains("EffectiveAccess", matriz.Data.FonteAcesso);
    }

    [Fact]
    public async Task B1_Matriz_PlanoInexistente_404Honesto()
    {
        await SementePronta;
        var controller = NovoPlanosController();

        var resultado = await controller.Matriz(Guid.NewGuid());
        Assert.Equal(404, StatusDe(resultado));
    }

    // ---------------------------------------------------------------------
    // B2 - aprovar upgrade.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task B2_AprovarUpgrade_TrocaPlanoComHistorico_E_Idempotente()
    {
        await SementePronta;
        var svc = NovoServico();

        var r1 = await svc.DecidirAsync("UPGRADE", B4Up1, true, "aprovado comercial", null, default);
        Assert.True(r1.Success, r1.Message);
        Assert.Equal("APROVADA", await StatusSolicitacaoAsync("plantaopro.upgrade_solicitacoes", B4Up1));
        Assert.Equal(B4PlanoB, await PlanoDaAssinaturaAsync(B4Ass1));
        Assert.True(await HistoricoExisteAsync(B4Ass1, "ALTERAR_PLANO"), "Aprovar upgrade deve gravar historico ALTERAR_PLANO.");

        var r2 = await svc.DecidirAsync("UPGRADE", B4Up1, true, "repeticao", null, default);
        Assert.True(r2.Success);
        Assert.Contains("já estava", r2.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task B2_AprovarUpgrade_DestinoInativo_409SemMudarNada()
    {
        await SementePronta;
        var svc = NovoServico();

        var r = await svc.DecidirAsync("UPGRADE", B4Up5, true, "tentativa", null, default);
        Assert.False(r.Success);
        Assert.Equal(409, r.StatusCode);
        Assert.Equal("SOLICITADO", await StatusSolicitacaoAsync("plantaopro.upgrade_solicitacoes", B4Up5));
        Assert.Equal(B4PlanoA, await PlanoDaAssinaturaAsync(B4Ass5));
    }

    // ---------------------------------------------------------------------
    // B3 - recusar / cancelamento / coerência.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task B3_RecusarSemJustificativa_400_E_ComJustificativa_Recusa()
    {
        await SementePronta;
        var svc = NovoServico();

        var semMotivo = await svc.DecidirAsync("UPGRADE", B4Up5, false, "  ", null, default);
        Assert.False(semMotivo.Success);
        Assert.Equal(400, semMotivo.StatusCode);

        var comMotivo = await svc.DecidirAsync("UPGRADE", B4Up5, false, "sem budget", null, default);
        Assert.True(comMotivo.Success, comMotivo.Message);
        Assert.Equal("RECUSADA", await StatusSolicitacaoAsync("plantaopro.upgrade_solicitacoes", B4Up5));
        Assert.Equal(B4PlanoA, await PlanoDaAssinaturaAsync(B4Ass5));
    }

    [Fact]
    public async Task B3_AprovarCancelamento_CancelaAssinaturaComMotivoEData()
    {
        await SementePronta;
        var svc = NovoServico();

        var r = await svc.DecidirAsync("CANCELAMENTO", B4Cancel4, true, "churn aceito", null, default);
        Assert.True(r.Success, r.Message);
        Assert.Equal("APROVADA", await StatusSolicitacaoAsync("plantaopro.downgrade_solicitacoes", B4Cancel4));
        var (status, motivo, data) = await AssinaturaCanceladaAsync(B4Ass4);
        Assert.Equal("CANCELADA", status);
        Assert.Contains("B4 motivo cancel", motivo, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(data);
    }

    [Fact]
    public async Task B3_DecidirDowngrade_EmSolicitacaoDeCancelamento_409()
    {
        await SementePronta;
        var svc = NovoServico();

        var r = await svc.DecidirAsync("DOWNGRADE", B4Cancel4, true, "tipo errado", null, default);
        Assert.False(r.Success);
        Assert.Equal(409, r.StatusCode);
    }

    [Fact]
    public async Task B3_Decidir_Inexistente_404()
    {
        await SementePronta;
        var svc = NovoServico();

        var r = await svc.DecidirAsync("UPGRADE", Guid.NewGuid(), true, "x", null, default);
        Assert.False(r.Success);
        Assert.Equal(404, r.StatusCode);
    }

    // ---------------------------------------------------------------------
    // B4 - downgrade com/sem excesso de uso.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task B4_Downgrade_UsoAcimaDoDestino_409()
    {
        await SementePronta;
        var svc = NovoServico();

        var r = await svc.DecidirAsync("DOWNGRADE", B4Down2, true, "tentativa", null, default);
        Assert.False(r.Success);
        Assert.Equal(409, r.StatusCode);
        Assert.Contains("excede", r.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("SOLICITADO", await StatusSolicitacaoAsync("plantaopro.downgrade_solicitacoes", B4Down2));
    }

    [Fact]
    public async Task B4_Downgrade_UsoDentroDoLimite_Aprova()
    {
        await SementePronta;
        var svc = NovoServico();

        var r = await svc.DecidirAsync("DOWNGRADE", B4Down3, true, "ok", null, default);
        Assert.True(r.Success, r.Message);
        Assert.Equal(B4PlanoB, await PlanoDaAssinaturaAsync(B4Ass3));
    }

    // ---------------------------------------------------------------------
    // B5 - ativar agendados.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task B5_AtivarAgendados_SoVencidoViraAtivo_ComTrilha_E_Idempotente()
    {
        await SementePronta;
        var catalog = new SaasModuleCatalogService(BuildCfg(), new B4FakeCurrentUser(B4Usuario, true), new B4FakeAuditService());

        var r1 = await catalog.AtivarAgendadosAsync(null, default);
        Assert.True(r1.Success, r1.Message);
        Assert.Equal(1, r1.Data);
        Assert.Equal("ATIVO", await StatusContratoAsync());
        Assert.True(await TrilhaExisteAsync(), "Ativacao deve gravar trilha ATIVACAO_AGENDADA.");

        var r2 = await catalog.AtivarAgendadosAsync(null, default);
        Assert.True(r2.Success);
        Assert.Equal(0, r2.Data);
    }

    [Fact]
    public async Task B5_Listar_FiltraTipoEStatus()
    {
        await SementePronta;
        var svc = NovoServico();

        var todas = await svc.ListarAsync(null, null, default);
        Assert.True(todas.Success);
        Assert.Equal(5, todas.Data!.Count());

        var upgrades = await svc.ListarAsync("UPGRADE", null, default);
        Assert.Equal(2, upgrades.Data!.Count());

        var solicitados = await svc.ListarAsync(null, "SOLICITADO", default);
        Assert.Equal(4, solicitados.Data!.Count());
    }

    // ---------------------------------------------------------------------
    // Infraestrutura local (semente, fakes, helpers).
    // ---------------------------------------------------------------------
    private static IConfiguration BuildCfg() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
        .Build();

    private static SolicitacoesPlanosService NovoServico()
    {
        var cfg = BuildCfg();
        var audit = new B4FakeAuditService();
        return new SolicitacoesPlanosService(cfg, new B4FakeCurrentUser(B4Usuario, true), audit,
            new AssinaturaGuardService(cfg, audit, NullLogger<AssinaturaGuardService>.Instance),
            NullLogger<SolicitacoesPlanosService>.Instance);
    }

    private static PlanosController NovoPlanosController() =>
        new(BuildCfg(), new B4FakeAuditService(), new B4FakeCurrentUser(B4Usuario, true), NullLogger<PlanosController>.Instance);

    private static int StatusDe(IActionResult resultado) => resultado switch
    {
        OkObjectResult => 200,
        BadRequestObjectResult => 400,
        NotFoundObjectResult => 404,
        ConflictObjectResult => 409,
        ObjectResult objectResult => objectResult.StatusCode ?? 500,
        _ => -1
    };

    private static async Task<T> ConsultarAsync<T>(string sql, object parametros)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        return await cn.QuerySingleAsync<T>(sql, parametros);
    }

    private static Task<string> StatusSolicitacaoAsync(string tabela, Guid id) =>
        ConsultarAsync<string>($"select status from {tabela} where id=@id", new { id });

    private static Task<Guid> PlanoDaAssinaturaAsync(Guid assinatura) =>
        ConsultarAsync<Guid>("select plano_id from plantaopro.assinaturas where id=@assinatura", new { assinatura });

    private static Task<bool> HistoricoExisteAsync(Guid assinatura, string acao) =>
        ConsultarAsync<bool>("select exists(select 1 from plantaopro.assinatura_historico where assinatura_id=@assinatura and acao=@acao)", new { assinatura, acao });

    private static Task<(string Status, string Motivo, DateTime? Data)> AssinaturaCanceladaAsync(Guid assinatura) =>
        ConsultarAsync<(string, string, DateTime?)>("select status, coalesce(motivo_cancelamento,''), data_cancelamento from plantaopro.assinaturas where id=@assinatura", new { assinatura });

    private static Task<string> StatusContratoAsync() =>
        ConsultarAsync<string>("select status from plantaopro.tenant_modulos where tenant_id=@t and modulo_id=@m", new { t = B4Tenant, m = B4Modulo });

    private static Task<bool> TrilhaExisteAsync() =>
        ConsultarAsync<bool>("select exists(select 1 from plantaopro.tenant_modulos_historico where tenant_id=@t and modulo_id=@m and acao='ATIVACAO_AGENDADA')", new { t = B4Tenant, m = B4Modulo });

    private const string PurgeSql = @"
delete from plantaopro.upgrade_solicitacoes where id in (@up1,@up5);
delete from plantaopro.downgrade_solicitacoes where id in (@down2,@down3,@cancel4);
delete from plantaopro.assinatura_historico where assinatura_id in (@ass1,@ass2,@ass3,@ass4,@ass5);
delete from plantaopro.cliente_bloqueios where cliente_id in (@c1,@c2,@c3,@c4,@c5);
delete from plantaopro.cliente_alertas where cliente_id in (@c1,@c2,@c3,@c4,@c5);
delete from plantaopro.tenant_modulos_historico where tenant_id=@tenant and modulo_id=@modulo;
delete from plantaopro.tenant_modulos where tenant_id=@tenant and modulo_id=@modulo;
delete from plantaopro.assinatura_uso where cliente_id in (@c1,@c2,@c3,@c4,@c5);
delete from plantaopro.assinaturas where id in (@ass1,@ass2,@ass3,@ass4,@ass5);
delete from plantaopro.medicos where cliente_id in (@c1,@c2,@c3,@c4,@c5);
delete from plantaopro.plano_modulos where plano_id in (@planoA,@planoB,@planoOff);
delete from plantaopro.plano_recursos where plano_id in (@planoA,@planoB,@planoOff);
delete from plantaopro.modulos_sistema where id=@modulo;
delete from plantaopro.usuarios where id=@usuario;
delete from plantaopro.clientes where id in (@c1,@c2,@c3,@c4,@c5);
delete from plantaopro.planos where id in (@planoA,@planoB,@planoOff);
delete from plantaopro.tenants where id=@tenant;";

    private const string InsertSql = @"
insert into plantaopro.tenants (id, tenant_id, codigo, nome, status, reg_status) values
  (@tenant, @tenant, 'B4TENT', 'B4 Tenant Agendado', 'ATIVO', 'A');

insert into plantaopro.modulos_sistema (id, codigo, nome, status, reg_status) values
  (@modulo, 'B4MOD', 'B4 Modulo Teste', 'ATIVO', 'A');

insert into plantaopro.tenant_modulos (id, tenant_id, modulo_id, codigo_modulo, habilitado, status, origem, ativado_em, desativado_em, reg_status) values
  (gen_random_uuid(), @tenant, @modulo, 'B4MOD', false, 'AGENDADO', 'SOLICITACAO', now()-interval '1 day', null, 'A');

insert into plantaopro.planos (id, nome, valor_mensal, limite_medicos, limite_hospitais, limite_plantoes_mes, limite_usuarios, status, reg_status) values
  (@planoA, 'B4 Plano A', 399, 0, 0, 0, 0, 'ATIVO', 'A'),
  (@planoB, 'B4 Plano B', 899, 1, 0, 0, 0, 'ATIVO', 'A'),
  (@planoOff, 'B4 Plano Off', 0, 0, 0, 0, 0, 'INATIVO', 'A');

insert into plantaopro.plano_modulos (id, plano_id, modulo_id, codigo_modulo, incluido, limite, preco_adicional, reg_status, reg_date) values
  (gen_random_uuid(), @planoA, @modulo, 'B4MOD', true, null, 0, 'A', now());

insert into plantaopro.plano_recursos (id, plano_id, codigo, nome, descricao, recurso, habilitado, limite, reg_status, reg_date) values
  (gen_random_uuid(), @planoA, 'SUPORTE', 'Suporte padrao', 'B4 recurso', 'SUPORTE', true, null, 'A', now());

insert into plantaopro.usuarios (id, nome, email, email_normalizado, senha_hash, status, reg_status) values
  (@usuario, 'B4 Usuario Teste', 'b4-usuario@exemplo.test', 'b4-usuario@exemplo.test', '{b4}', 'ATIVO', 'A');

insert into plantaopro.clientes (id, razao_social, status, reg_status) values
  (@c1, 'B4 Cliente 1', 'ATIVO', 'A'),
  (@c2, 'B4 Cliente 2', 'ATIVO', 'A'),
  (@c3, 'B4 Cliente 3', 'ATIVO', 'A'),
  (@c4, 'B4 Cliente 4', 'ATIVO', 'A'),
  (@c5, 'B4 Cliente 5', 'ATIVO', 'A');

insert into plantaopro.assinaturas (id, cliente_id, plano_id, data_inicio, data_fim, status, valor_contratado, dia_vencimento, periodicidade, observacoes, reg_status, reg_date) values
  (@ass1, @c1, @planoA, now()::date, current_date + interval '60 days', 'ATIVA', 399, 10, 'MENSAL', 'B4 ass1', 'A', now()),
  (@ass2, @c2, @planoA, now()::date, current_date + interval '60 days', 'ATIVA', 399, 10, 'MENSAL', 'B4 ass2', 'A', now()),
  (@ass3, @c3, @planoA, now()::date, current_date + interval '60 days', 'ATIVA', 399, 10, 'MENSAL', 'B4 ass3', 'A', now()),
  (@ass4, @c4, @planoA, now()::date, current_date + interval '60 days', 'ATIVA', 399, 10, 'MENSAL', 'B4 ass4', 'A', now()),
  (@ass5, @c5, @planoA, now()::date, current_date + interval '60 days', 'ATIVA', 399, 10, 'MENSAL', 'B4 ass5', 'A', now());

insert into plantaopro.medicos (id, cliente_id, nome, status, dados, criado_em, reg_status) values
  (gen_random_uuid(), @c2, 'B4 Medico 1', 'ATIVO', '{}', now(), 'A'),
  (gen_random_uuid(), @c2, 'B4 Medico 2', 'ATIVO', '{}', now(), 'A');

insert into plantaopro.upgrade_solicitacoes (id, tenant_id, cliente_id, assinatura_id, plano_atual_id, plano_destino_id, motivo, status, solicitado_por, reg_date, reg_status) values
  (@up1, @tenant, @c1, @ass1, @planoA, @planoB, 'B4 upgrade', 'SOLICITADO', @usuario, now(), 'A'),
  (@up5, @tenant, @c5, @ass5, @planoA, @planoOff, 'B4 upgrade inativo', 'SOLICITADO', @usuario, now(), 'A');

insert into plantaopro.downgrade_solicitacoes (id, tenant_id, cliente_id, assinatura_id, plano_atual_id, plano_destino_id, motivo, impacto_validado, status, solicitado_por, reg_date, reg_status) values
  (@down2, @tenant, @c2, @ass2, @planoA, @planoB, 'B4 downgrade excesso', false, 'SOLICITADO', @usuario, now(), 'A'),
  (@down3, @tenant, @c3, @ass3, @planoA, @planoB, 'B4 downgrade ok', false, 'SOLICITADO', @usuario, now(), 'A'),
  (@cancel4, @tenant, @c4, @ass4, @planoA, @planoA, 'B4 motivo cancel', false, 'CANCELAMENTO_SOLICITADO', @usuario, now(), 'A');";

    private static object SeedParams() => new
    {
        planoA = B4PlanoA,
        planoB = B4PlanoB,
        planoOff = B4PlanoInativo,
        modulo = B4Modulo,
        tenant = B4Tenant,
        c1 = B4Cliente1,
        c2 = B4Cliente2,
        c3 = B4Cliente3,
        c4 = B4Cliente4,
        c5 = B4Cliente5,
        ass1 = B4Ass1,
        ass2 = B4Ass2,
        ass3 = B4Ass3,
        ass4 = B4Ass4,
        ass5 = B4Ass5,
        up1 = B4Up1,
        up5 = B4Up5,
        down2 = B4Down2,
        down3 = B4Down3,
        cancel4 = B4Cancel4,
        usuario = B4Usuario
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

    private sealed class B4FakeCurrentUser : ICurrentUserService
    {
        public Guid? UserId { get; }
        public Guid? TenantId => null;
        public Guid? ClienteId => null;
        public Guid? SessionId => null;
        public IReadOnlyCollection<string> Roles { get; }

        public B4FakeCurrentUser(Guid? userId, bool global)
        {
            UserId = userId;
            Roles = global ? new[] { "ADMINISTRADOR_GLOBAL" } : Array.Empty<string>();
        }

        public bool IsAuthenticated() => UserId.HasValue;
        public bool IsGlobalAdmin() => Roles.Any(role => role.Equals("ADMINISTRADOR_GLOBAL", StringComparison.OrdinalIgnoreCase));
        public bool IsTenantAdmin() => false;
        public bool IsPartner() => false;
        public bool IsDoctor() => false;
        public bool HasRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class B4FakeAuditService : IAuditService
    {
        public Task RegistrarAsync(Guid? usuarioId, Guid? clienteId, string entidade, Guid? entidadeId, string acao,
            object? detalhes, bool sucesso, string? ipOrigem, string? perfil, CancellationToken ct = default) => Task.CompletedTask;

        public Task LogAsync(Guid? userId, string acao, string entidade, Guid? registroId, string descricao,
            string? valorAnterior = null, string? valorNovo = null, string? ip = null, string? userAgent = null) => Task.CompletedTask;
    }
}
