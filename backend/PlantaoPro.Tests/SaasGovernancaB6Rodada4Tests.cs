using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Controllers;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using PlantaoPro.CrossCutting.Security;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R4-B6 (rodada 4 - governanca SaaS). Cobre os pontos de estabilizacao S1-S5 contra o
/// PostgreSQL local (plantaopro_test), no template Q7: GUIDs fixos com prefixo b6,
/// semente idempotente em uma unica transacao (purga + insert) para reruns previsiveis.
///
/// G1  Perfis globais (S1): fonte unica GlobalAdminProfiles + scan de codigo.
/// G2  Vigencia de contrato por modulo (S2): predicado canonico (ATIVO/AGENDADO/vigencia/desativacao).
/// G3  TRIAL consistente (S3): trial vale enquanto nao vence; vencida bloqueia com registro de bloqueio.
/// G4  Corrida de assinatura ativa/trial (S4): lock advisory por cliente + indice unico como arbitro.
/// G5  Revogacao administrativa de sessao aberta: valida antes, invalida depois, motivo auditavel.
/// G6  Downgrade imediato (S5): desativacao de contrato revoga sessoes abertas na mesma transacao.
/// </summary>
[Collection("saas-operacao-serial")]
public sealed class SaasGovernancaB6Rodada4Tests : IDisposable
{
    // Chave/módulo da cadeia de autorização (G2 e G6 compartilham o módulo).
    private static readonly Guid B6Modulo = Guid.Parse("b6000001-0000-4000-9000-000000000001");
    private static readonly Guid B6Acao = Guid.Parse("b6000001-0000-4000-9000-000000000002");
    private static readonly Guid B6Permissao = Guid.Parse("b6000001-0000-4000-9000-000000000003");

    // Usuários e sessões.
    private static readonly Guid B6Usuario = Guid.Parse("b6000001-0000-4000-9000-000000000004");
    private static readonly Guid B6UsuarioDown = Guid.Parse("b6000001-0000-4000-9000-000000000005");
    private static readonly Guid B6SessaoG5 = Guid.Parse("b6000001-0000-4000-9000-000000000010");
    private static readonly Guid B6SessaoG6 = Guid.Parse("b6000001-0000-4000-9000-000000000011");

    // Plano e clientes comerciais (G3 e G4).
    private static readonly Guid B6Plano = Guid.Parse("b6000001-0000-4000-9000-000000000020");
    private static readonly Guid B6ClienteTrialOk = Guid.Parse("b6000001-0000-4000-9000-000000000051");
    private static readonly Guid B6ClienteTrialVencida = Guid.Parse("b6000001-0000-4000-9000-000000000052");
    private static readonly Guid B6ClienteAtivaVencida = Guid.Parse("b6000001-0000-4000-9000-000000000053");
    private static readonly Guid B6ClienteCorrida = Guid.Parse("b6000001-0000-4000-9000-000000000054");
    private static readonly Guid B6ClienteReativar = Guid.Parse("b6000001-0000-4000-9000-000000000055");

    // Assinaturas fixas (G4b).
    private static readonly Guid B6AssAtivaReativar = Guid.Parse("b6000001-0000-4000-9000-000000000064");
    private static readonly Guid B6AssSuspensa = Guid.Parse("b6000001-0000-4000-9000-000000000065");

    // Tenants da matriz de vigência (G2) e do downgrade (G6).
    private static readonly Guid B6TenantA = Guid.Parse("b6000001-0000-4000-9000-000000000031");
    private static readonly Guid B6TenantB = Guid.Parse("b6000001-0000-4000-9000-000000000032");
    private static readonly Guid B6TenantC = Guid.Parse("b6000001-0000-4000-9000-000000000033");
    private static readonly Guid B6TenantD = Guid.Parse("b6000001-0000-4000-9000-000000000034");
    private static readonly Guid B6TenantE = Guid.Parse("b6000001-0000-4000-9000-000000000035");
    private static readonly Guid B6TenantDown = Guid.Parse("b6000001-0000-4000-9000-000000000036");

    // Uma instancia por metodo de teste (xunit recria a classe em cada metodo): a
    // semente corre por metodo e o Dispose abaixo purga logo apos, mantendo o banco
    // de teste compartilhado sem acúmulo de estado entre execucoes da suite.
    private readonly Task SementePronta = SementarAsync();

    public void Dispose()
    {
        SementePronta.GetAwaiter().GetResult();
        PurgarAsync().GetAwaiter().GetResult();
    }

    // ---------------------------------------------------------------------
    // G1a - fonte unica dos perfis globais: codigos distintos e classificacao.
    // ---------------------------------------------------------------------
    [Fact]
    public void G1a_PrefisGlobais_FonteUnica_CodigosDistinctos_E_ClassificacaoConsistente()
    {
        Assert.Equal(4, GlobalAdminProfiles.ProfileCodes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(GlobalAdminProfiles.ProfileCodes, code =>
            Assert.Contains("'" + code + "'", GlobalAdminProfiles.SqlInList, StringComparison.Ordinal));

        Assert.True(GlobalAdminProfiles.IsGlobalProfile("SUPER_ADMINISTRADOR"));
        Assert.True(GlobalAdminProfiles.IsGlobalProfile("super_admin"));
        Assert.True(GlobalAdminProfiles.IsGlobalProfile(" Admin_Global "));
        Assert.True(GlobalAdminProfiles.IsGlobalProfile("ADMINISTRADOR_GLOBAL"));

        Assert.False(GlobalAdminProfiles.IsGlobalProfile(null));
        Assert.False(GlobalAdminProfiles.IsGlobalProfile(""));
        Assert.False(GlobalAdminProfiles.IsGlobalProfile("   "));
        Assert.False(GlobalAdminProfiles.IsGlobalProfile("MEDICO"));
        Assert.False(GlobalAdminProfiles.IsGlobalProfile("SUPERADMIN"));
    }

    // ---------------------------------------------------------------------
    // G1b - nenhum .cs fora da fonte unica carrega o literal SUPER_ADMINISTRADOR
    //      e nenhuma view .cshtml carrega qualquer alias SUPER_ADMIN.
    // ---------------------------------------------------------------------
    [Fact]
    public void G1b_PrefisGlobais_SomenteFonteUnicaCarregaLiteralEmCodigo()
    {
        var arquivosCs = RepositoryPathResolver.EnumerateSourceFiles(RepositoryPathResolver.BackendRoot)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Replace('\\', '/').Contains("/PlantaoPro.Tests/", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.EndsWith("GlobalAdminProfiles.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.NotEmpty(arquivosCs);

        var infratores = arquivosCs
            .Where(path => File.ReadAllText(path).Contains("'SUPER_ADMINISTRADOR'", StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(infratores);

        var views = RepositoryPathResolver.EnumerateSourceFiles(RepositoryPathResolver.WebRoot)
            .Where(path => path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.NotEmpty(views);
        var viewsComAlias = views
            .Where(path => File.ReadAllText(path).Contains("SUPER_ADMIN", StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(viewsComAlias);

        // As duas views migradas na rodada usam a fonte unica qualificada.
        Assert.Contains("IsGlobalProfile", RepositoryPathResolver.ReadRepositoryFile(
            "backend", "PlantaoPro.Web", "Views", "Ajuda", "Index.cshtml"), StringComparison.Ordinal);
        Assert.Contains("IsGlobalProfile", RepositoryPathResolver.ReadRepositoryFile(
            "backend", "PlantaoPro.Web", "Views", "Ajuda", "PrimeirosPassos.cshtml"), StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------
    // G2 - vigencia de contrato por modulo: ATIVO vigente, AGENDADO dentro do
    //      prazo, desativado e AGENDADO futuro nao valem; sem contrato nem vale.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task G2_VigenciaContrato_ModuloNaoCore_DecidePorContratoEfetivo()
    {
        await SementePronta;
        var svc = new EffectivePermissionService(BuildCfg());

        var semContrato = await svc.TestarAsync(B6Usuario, B6TenantA, "B6GOV", "TESTE");
        Assert.False(semContrato.Permitido);
        Assert.Equal("MODULE_NOT_CONTRACTED", semContrato.Codigo);

        var desativado = await svc.TestarAsync(B6Usuario, B6TenantB, "B6GOV", "TESTE");
        Assert.False(desativado.Permitido);
        Assert.Equal("MODULE_NOT_CONTRACTED", desativado.Codigo);

        var agendadoNoPrazo = await svc.TestarAsync(B6Usuario, B6TenantC, "B6GOV", "TESTE");
        Assert.True(agendadoNoPrazo.Permitido);
        Assert.Equal("ALLOWED", agendadoNoPrazo.Codigo);

        var agendadoFuturo = await svc.TestarAsync(B6Usuario, B6TenantD, "B6GOV", "TESTE");
        Assert.False(agendadoFuturo.Permitido);
        Assert.Equal("MODULE_NOT_CONTRACTED", agendadoFuturo.Codigo);

        var ativoControle = await svc.TestarAsync(B6Usuario, B6TenantE, "B6GOV", "TESTE");
        Assert.True(ativoControle.Permitido);
        Assert.Equal("ALLOWED", ativoControle.Codigo);
    }

    // ---------------------------------------------------------------------
    // G3 - TRIAL: valido opera (uso/limites), vencido bloqueia 403 com registro
    //      de bloqueio ASSINATURA_TRIAL_VENCIDA; ATIVA vencida continua ASSINATURA_VENCIDA.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task G3_Trial_ValidoOpera_E_VencidoBloqueiaComRegistro()
    {
        await SementePronta;
        var guard = new AssinaturaGuardService(BuildCfg(), new B6FakeAuditService(), NullLogger<AssinaturaGuardService>.Instance);

        var usoOk = await guard.ObterUsoPlanoAsync(B6ClienteTrialOk);
        Assert.True(usoOk.Success, usoOk.Message);
        Assert.Equal("TRIAL", usoOk.Data!.AssinaturaStatus);

        var podeMedico = await guard.PodeCadastrarMedicoAsync(B6ClienteTrialOk);
        Assert.True(podeMedico.Success, podeMedico.Message);
        Assert.True(podeMedico.Data);

        var usoVencido = await guard.ObterUsoPlanoAsync(B6ClienteTrialVencida);
        Assert.False(usoVencido.Success);
        Assert.Equal(403, usoVencido.StatusCode);
        Assert.Contains("experimental", usoVencido.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(await BloqueioRegistradoAsync(B6ClienteTrialVencida, "ASSINATURA_TRIAL_VENCIDA"),
            "Trial vencida deve registrar bloqueio ASSINATURA_TRIAL_VENCIDA.");

        var ativaVencida = await guard.ObterUsoPlanoAsync(B6ClienteAtivaVencida);
        Assert.False(ativaVencida.Success);
        Assert.Equal(403, ativaVencida.StatusCode);
        Assert.True(await BloqueioRegistradoAsync(B6ClienteAtivaVencida, "ASSINATURA_VENCIDA"),
            "Assinatura ativa vencida deve continuar registrando ASSINATURA_VENCIDA.");
    }

    // ---------------------------------------------------------------------
    // G4a - corrida de criacao: duas chamadas concorrentes nunca deixam duas
    //      assinaturas ativas; exatamente uma vence (200) e a outra e 400/409.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task G4a_CorridaCriacao_ExatamenteUmaAssinaturaAtivaPorCliente()
    {
        await SementePronta;
        var cfg = BuildCfg();
        var controllerA = NovaAssinaturasController(cfg);
        var controllerB = NovaAssinaturasController(cfg);
        var request = new AssinaturaComercialRequest(B6ClienteCorrida, B6Plano,
            DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(60), 100m, 10, "B6 corrida");

        var tarefaA = controllerA.Criar(request);
        var tarefaB = controllerB.Criar(request);
        await Task.WhenAll(tarefaA, tarefaB);
        var statusA = StatusDe(await tarefaA);
        var statusB = StatusDe(await tarefaB);

        Assert.Equal(1, new[] { statusA, statusB }.Count(status => status == 200));
        Assert.All(new[] { statusA, statusB }, status =>
            Assert.True(status == 200 || status == 400 || status == 409, $"Status inesperado na corrida: {status}"));

        var linhas = await ConsultarAsync<int>("select count(1)::int from plantaopro.assinaturas where cliente_id=@clienteId and reg_status='A'", new { clienteId = B6ClienteCorrida });
        Assert.Equal(1, linhas);
    }

    // ---------------------------------------------------------------------
    // G4b - reativar suspensa quando ja existe ATIVA: 409 e a ATIVA original intacta.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task G4b_ReativarComConflito_Retorna409_E_AtivaOriginalIntacta()
    {
        await SementePronta;
        var controller = NovaAssinaturasController(BuildCfg());

        var resultado = await controller.Reativar(B6AssSuspensa, new JustificativaRequest("B6 reativar com conflito"));
        Assert.Equal(409, StatusDe(resultado));

        var ativas = await ConsultarAsync<int>(
            "select count(1)::int from plantaopro.assinaturas where cliente_id=@clienteId and upper(status)='ATIVA' and reg_status='A'",
            new { clienteId = B6ClienteReativar });
        Assert.Equal(1, ativas);

        var suspensaStatus = await ConsultarAsync<string>(
            "select coalesce(status,'') from plantaopro.assinaturas where id=@id",
            new { id = B6AssSuspensa });
        Assert.Equal("SUSPENSA", suspensaStatus);
    }

    // ---------------------------------------------------------------------
    // G5 - revogacao administrativa: sessao aberta valida para o proprio usuario
    //      fica invalida apos a revogacao, com motivo REVOGACAO_ADMINISTRATIVA.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task G5_RevogacaoAdministrativa_SessaoAbertaViraInvalidaComMotivo()
    {
        await SementePronta;
        var cfg = BuildCfg();
        var sessoes = new AuthenticationSessionService(cfg);
        var admin = new SecurityAdministrationService(cfg,
            new B6FakeCurrentUser(B6Usuario, global: true),
            new EffectivePermissionService(cfg),
            new B6FakeAuditService());
        var principal = NovoPrincipal(B6Usuario, B6SessaoG5);

        Assert.True(await sessoes.ValidateAsync(principal),
            "Sessao aberta do proprio usuario deve ser valida antes da revogacao.");

        var revogacao = await admin.RevogarSessoesAdministrativamenteAsync(B6Usuario, "127.0.0.1", CancellationToken.None);
        Assert.True(revogacao.Success, revogacao.Message);

        Assert.False(await sessoes.ValidateAsync(principal),
            "Sessao revogada administrativamente deve deixar de ser valida.");

        var motivo = await ConsultarAsync<string>("select coalesce(motivo_revogacao,'') from plantaopro.auth_sessoes where id=@id", new { id = B6SessaoG5 });
        Assert.Equal("REVOGACAO_ADMINISTRATIVA", motivo);
    }

    // ---------------------------------------------------------------------
    // G6 - downgrade imediato: desativar contrato do modulo revoga a sessao
    //      aberta do tenant na mesma operacao; re-habilitar restaura o contrato.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task G6_DowngradeImediato_DesativaContrato_E_RevogaSessaoAberta()
    {
        await SementePronta;
        var cfg = BuildCfg();
        var catalog = new SaasModuleCatalogService(cfg, new B6FakeCurrentUser(B6UsuarioDown, global: true), new B6FakeAuditService());
        var request = new TenantModuleContractRequest { TenantId = B6TenantDown, Motivo = "B6 downgrade imediato", PrecoContratado = 10m, LimiteContratado = 1 };

        var desativar = await catalog.ToggleTenantAsync(B6Modulo, request, enabled: false, "127.0.0.1", CancellationToken.None);
        Assert.True(desativar.Success, desativar.Message);

        var contrato = await ConsultarAsync<(string Status, bool DesativadoEmPreenchido, bool Habilitado)>(
            @"select coalesce(tm.status,'') as Status, tm.desativado_em is not null as DesativadoEmPreenchido, tm.habilitado as Habilitado
from plantaopro.tenant_modulos tm where tm.tenant_id=@tenantId and tm.modulo_id=@moduleId and tm.reg_status='A'",
            new { tenantId = B6TenantDown, moduleId = B6Modulo });
        Assert.Equal("BLOQUEADO", contrato.Status);
        Assert.True(contrato.DesativadoEmPreenchido);
        Assert.False(contrato.Habilitado);

        var sessao = await ConsultarAsync<(bool Revogada, string Motivo)>(
            "select s.revogada_em is not null as Revogada, coalesce(s.motivo_revogacao,'') as Motivo from plantaopro.auth_sessoes s where s.id=@id",
            new { id = B6SessaoG6 });
        Assert.True(sessao.Revogada);
        Assert.Equal("MODULO_DESATIVADO", sessao.Motivo);

        var historico = await ConsultarAsync<int>(
            "select count(1)::int from plantaopro.tenant_modulos_historico where tenant_id=@tenantId and modulo_id=@moduleId",
            new { tenantId = B6TenantDown, moduleId = B6Modulo });
        Assert.True(historico >= 1, "Downgrade deve auditar historico do contrato.");

        var reativar = await catalog.ToggleTenantAsync(B6Modulo, request, enabled: true, "127.0.0.1", CancellationToken.None);
        Assert.True(reativar.Success, reativar.Message);

        var contratoRestaurado = await ConsultarAsync<(string Status, bool DesativadoEmPreenchido, bool Habilitado)>(
            @"select coalesce(tm.status,'') as Status, tm.desativado_em is not null as DesativadoEmPreenchido, tm.habilitado as Habilitado
from plantaopro.tenant_modulos tm where tm.tenant_id=@tenantId and tm.modulo_id=@moduleId and tm.reg_status='A'",
            new { tenantId = B6TenantDown, moduleId = B6Modulo });
        Assert.Equal("ATIVO", contratoRestaurado.Status);
        Assert.False(contratoRestaurado.DesativadoEmPreenchido);
        Assert.True(contratoRestaurado.Habilitado);
    }

    // ---------------------------------------------------------------------
    // Infraestrutura local (semente, fakes, helpers).
    // ---------------------------------------------------------------------
    private static IConfiguration BuildCfg() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
        .Build();

    private static ClaimsPrincipal NovoPrincipal(Guid userId, Guid sessionId) =>
        new(new ClaimsIdentity(new[]
        {
            new Claim("uid", userId.ToString()),
            new Claim("session_id", sessionId.ToString())
        }, "b6-teste"));

    private static AssinaturasController NovaAssinaturasController(IConfiguration cfg) =>
        new(cfg,
            new AssinaturaGuardService(cfg, new B6FakeAuditService(), NullLogger<AssinaturaGuardService>.Instance),
            new B6FakeAuditService(),
            NullLogger<AssinaturasController>.Instance)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

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

    private static Task<bool> BloqueioRegistradoAsync(Guid clienteId, string tipo) =>
        ConsultarAsync<bool>("select exists(select 1 from plantaopro.cliente_bloqueios where cliente_id=@clienteId and tipo=@tipo)", new { clienteId, tipo });

    private const string PurgeSql = @"
delete from plantaopro.cliente_bloqueios where cliente_id in (@cTrialOk,@cTrialVencida,@cAtivaVencida,@cCorrida,@cReativar);
delete from plantaopro.cliente_alertas where cliente_id in (@cTrialOk,@cTrialVencida,@cAtivaVencida,@cCorrida,@cReativar);
delete from plantaopro.tenant_modulos_historico where modulo_id=@modulo;
delete from plantaopro.tenant_modulos where modulo_id=@modulo;
delete from plantaopro.auth_sessoes where id in (@sessaoG5,@sessaoG6) or tenant_id=@tDown;
delete from plantaopro.usuario_tenant_acessos where usuario_id in (@usuario,@usuarioDown);
delete from plantaopro.usuario_permissoes_especiais where usuario_id in (@usuario,@usuarioDown);
delete from plantaopro.assinaturas where cliente_id in (@cTrialOk,@cTrialVencida,@cAtivaVencida,@cCorrida,@cReativar);
delete from plantaopro.permissoes where id=@permissao;
delete from plantaopro.acoes_sistema where id=@acao;
delete from plantaopro.modulos_sistema where id=@modulo;
delete from plantaopro.usuarios where id in (@usuario,@usuarioDown);
delete from plantaopro.clientes where id in (@cTrialOk,@cTrialVencida,@cAtivaVencida,@cCorrida,@cReativar);
delete from plantaopro.planos where id=@plano;
delete from plantaopro.tenants where id in (@tA,@tB,@tC,@tD,@tE,@tDown);";

    private const string InsertSql = @"
insert into plantaopro.tenants (id, tenant_id, codigo, nome, status, reg_status) values
  (@tA,   @tA,   'B6TENTA','B6 Tenant A - sem contrato',     'ATIVO','A'),
  (@tB,   @tB,   'B6TENTB','B6 Tenant B - contrato desativado','ATIVO','A'),
  (@tC,   @tC,   'B6TENTC','B6 Tenant C - agendado no prazo','ATIVO','A'),
  (@tD,   @tD,   'B6TENTD','B6 Tenant D - agendado futuro',  'ATIVO','A'),
  (@tE,   @tE,   'B6TENTE','B6 Tenant E - ativo controle',   'ATIVO','A'),
  (@tDown,@tDown,'B6TENTDOWN','B6 Tenant Downgrade',         'ATIVO','A');

insert into plantaopro.modulos_sistema (id, codigo, nome, status, reg_status) values
  (@modulo, 'B6GOV', 'B6 Governanca Teste', 'ATIVO', 'A');
insert into plantaopro.acoes_sistema (id, codigo, nome) values
  (@acao, 'TESTE', 'Acao de teste B6');
insert into plantaopro.permissoes (id, nome, modulo_id, acao_id, codigo) values
  (@permissao, 'B6 permissao de teste', @modulo, @acao, 'B6GOV.TESTE');

insert into plantaopro.usuarios (id, nome, email, email_normalizado, senha_hash, status, reg_status) values
  (@usuario,     'B6 Usuario Teste',     'b6-usuario@exemplo.test',     'b6-usuario@exemplo.test',     '{b6}', 'ATIVO', 'A'),
  (@usuarioDown,'B6 Usuario Downgrade', 'b6-usuario-down@exemplo.test','b6-usuario-down@exemplo.test','{b6}', 'ATIVO', 'A');

insert into plantaopro.usuario_tenant_acessos (usuario_id, tenant_id, status) values
  (@usuario, @tA, 'ATIVO'),
  (@usuario, @tB, 'ATIVO'),
  (@usuario, @tC, 'ATIVO'),
  (@usuario, @tD, 'ATIVO'),
  (@usuario, @tE, 'ATIVO');

insert into plantaopro.usuario_permissoes_especiais (usuario_id, permissao_id, permitido, justificativa) values
  (@usuario, @permissao, true, 'B6 teste');

insert into plantaopro.tenant_modulos (id, tenant_id, modulo_id, codigo_modulo, habilitado, status, origem, ativado_em, desativado_em, reg_status) values
  (gen_random_uuid(), @tB,  @modulo, 'B6GOV', true, 'ATIVO',    'CONTRATO', now()-interval '1 day',  now()-interval '1 hour', 'A'),
  (gen_random_uuid(), @tC,  @modulo, 'B6GOV', true, 'AGENDADO', 'CONTRATO', now()-interval '1 day',  null,                       'A'),
  (gen_random_uuid(), @tD,  @modulo, 'B6GOV', true, 'AGENDADO', 'CONTRATO', now()+interval '7 days', null,                       'A'),
  (gen_random_uuid(), @tE,  @modulo, 'B6GOV', true, 'ATIVO',    'CONTRATO', now()-interval '1 day',  null,                       'A'),
  (gen_random_uuid(), @tDown, @modulo, 'B6GOV', true, 'ATIVO',  'CONTRATO', now()-interval '1 day',  null,                       'A');

insert into plantaopro.auth_sessoes (id, tenant_id, cliente_id, usuario_id, dispositivo_nome, ip_mascarado, user_agent_sanitizado, iniciado_em, ultimo_uso_em, expira_em, reg_status, reg_date) values
  (@sessaoG5,  null,    null, @usuario,     'B6 device', 'b6-teste', 'b6-agent', now(), now(), now()+interval '8 hours', 'A', now()),
  (@sessaoG6,  @tDown,  null, @usuarioDown,'B6 device', 'b6-teste', 'b6-agent', now(), now(), now()+interval '8 hours', 'A', now());

insert into plantaopro.planos (id, nome, valor_mensal, limite_medicos, status, reg_status) values
  (@plano, 'B6 Plano Teste', 100, 0, 'ATIVO', 'A');

insert into plantaopro.clientes (id, razao_social, status, reg_status) values
  (@cTrialOk,      'B6 Cliente Trial Ok',        'ATIVO', 'A'),
  (@cTrialVencida, 'B6 Cliente Trial Vencida',   'ATIVO', 'A'),
  (@cAtivaVencida, 'B6 Cliente Ativa Vencida',   'ATIVO', 'A'),
  (@cCorrida,      'B6 Cliente Corrida',         'ATIVO', 'A'),
  (@cReativar,     'B6 Cliente Reativar',        'ATIVO', 'A');

insert into plantaopro.assinaturas (id, tenant_id, cliente_id, plano_id, data_inicio, data_fim, status, valor_contratado, dia_vencimento, periodicidade, data_trial_fim, observacoes, reg_status, reg_date) values
  (gen_random_uuid(), null, @cTrialOk,      @plano, now()::date, current_date + interval '30 days', 'TRIAL',  0,   10, 'MENSAL', current_date + interval '3 days', 'B6 trial ok',       'A', now()),
  (gen_random_uuid(), null, @cTrialVencida, @plano, now()::date, current_date + interval '30 days', 'TRIAL',  0,   10, 'MENSAL', current_date - interval '1 day',  'B6 trial vencida',  'A', now()),
  (gen_random_uuid(), null, @cAtivaVencida, @plano, now()::date, current_date - interval '2 days', 'ATIVA',  100, 10, 'MENSAL', null,                           'B6 ativa vencida',  'A', now()),
  (@assAtiva, null, @cReativar, @plano, now()::date, current_date + interval '60 days', 'ATIVA', 100, 10, 'MENSAL', null, 'B6 ativa original',   'A', now()),
  (@assSuspensa, null, @cReativar, @plano, now()::date, current_date + interval '60 days', 'SUSPENSA', 100, 10, 'MENSAL', null, 'B6 suspensa', 'A', now());";

    private static object SeedParams() => new
    {
        modulo = B6Modulo,
        acao = B6Acao,
        permissao = B6Permissao,
        usuario = B6Usuario,
        usuarioDown = B6UsuarioDown,
        sessaoG5 = B6SessaoG5,
        sessaoG6 = B6SessaoG6,
        plano = B6Plano,
        tA = B6TenantA,
        tB = B6TenantB,
        tC = B6TenantC,
        tD = B6TenantD,
        tE = B6TenantE,
        tDown = B6TenantDown,
        cTrialOk = B6ClienteTrialOk,
        cTrialVencida = B6ClienteTrialVencida,
        cAtivaVencida = B6ClienteAtivaVencida,
        cCorrida = B6ClienteCorrida,
        cReativar = B6ClienteReativar,
        assAtiva = B6AssAtivaReativar,
        assSuspensa = B6AssSuspensa
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

    private sealed class B6FakeCurrentUser : ICurrentUserService
    {
        public Guid? UserId { get; }
        public Guid? TenantId => null;
        public Guid? ClienteId => null;
        public Guid? SessionId => null;
        public IReadOnlyCollection<string> Roles { get; }

        public B6FakeCurrentUser(Guid? userId, bool global)
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

    private sealed class B6FakeAuditService : IAuditService
    {
        public Task RegistrarAsync(Guid? usuarioId, Guid? clienteId, string entidade, Guid? entidadeId, string acao,
            object? detalhes, bool sucesso, string? ipOrigem, string? perfil, CancellationToken ct = default) => Task.CompletedTask;

        public Task LogAsync(Guid? userId, string acao, string entidade, Guid? registroId, string descricao,
            string? valorAnterior = null, string? valorNovo = null, string? ip = null, string? userAgent = null) => Task.CompletedTask;
    }
}
