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
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// Saúde 360 — Regressão da jornada de homologação S1–S11 (probe OK=79/FAIL=0,
/// evidência wpS-jornada-f3.log). Cobre os corretores aplicados sobre a4bf05c:
///
/// F1/F1b — "iniciar" é ação de uma única vez no motor canônico
///          Saude360ClinicalService.AcaoAsync: corrida simultânea de iniciar
///          (triagens e consultas) produz EXATAMENTE um 200 e o perdedor 409
///          "Conflito: o registro foi alterado por outra ação simultânea.";
///          repetir iniciar em sequência também é 409; o histórico clínica
///          registra a ação uma única vez; finalizar de triagem continua
///          retry-safe (sem duplicar histórico); finalizar duplicado de
///          consulta é 409; ações em ID inexistente dão 404 (rollback, sem
///          histórico); tenant B não enxerga o registro do tenant A (404).
/// F4      — Saude360ModuleFilter: tenant sem contrato SAUDE360 ativo recebe
///          403 com a mensagem exata de contrato; contratado passa; global
///          admin faz bypass sem consultar banco; anônimo delega para a
///          camada de autenticação; falha de banco é fail-open.
/// F3      — Correlação Web&lt;-&gt;API: o BFF envia X-Correlation-ID e loga
///          DuracaoTotalMs sem expor token; a API loga CorrelationId em toda
///          requisição concluída (contract tests de leitura de source).
/// </summary>
public sealed class Saude360JornadaSFixesRegressionTests
{
    private const string MsgConflito = "Conflito: o registro foi alterado por outra ação simultânea.";
    private const string MsgNaoEncontrado = "Registro não encontrado para ação.";
    private const string MsgModulo = "Módulo Saúde 360 não contratado para este cliente.";

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        public FakeCurrentUser(Guid? tenantId = null, bool globalAdmin = false)
        {
            TenantId = tenantId;
            GlobalAdmin = globalAdmin;
        }

        public bool GlobalAdmin { get; }
        public Guid? UserId => Guid.Empty;
        public Guid? TenantId { get; }
        public Guid? ClienteId => TenantId;
        public Guid? SessionId => null;
        public IReadOnlyCollection<string> Roles => Array.Empty<string>();
        public bool IsAuthenticated() => true;
        public bool IsGlobalAdmin() => GlobalAdmin;
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

    private static Saude360ClinicalService BuildService(Guid? tenantId, bool globalAdmin = false) =>
        new(BuildCfg(TestDatabase.ConnectionString), new FakeCurrentUser(tenantId, globalAdmin), new FakeAuditService(), NullLogger<Saude360ClinicalService>.Instance);

    #region F1b — corrida/repetição de iniciar no motor canônico

    [Fact]
    public async Task F1b_Triagem_IniciarConcorrente_ExatamenteUmSucesso_Perdedor409_HistoricoUnico()
    {
        var cs = TestDatabase.ConnectionString;
        var tenant = Guid.NewGuid();
        var triagemId = Guid.NewGuid();
        var pacienteId = Guid.NewGuid();

        await using (var cn = new NpgsqlConnection(cs))
        {
            await cn.OpenAsync();
            await cn.ExecuteAsync(@"insert into plantaopro.triagens(id, cliente_id, paciente_id, status, reg_status, reg_date)
values(@id, @tenant, @paciente, 'AGUARDANDO', 'A', now())", new { id = triagemId, tenant, paciente = pacienteId });
        }

        try
        {
            var svc = BuildService(tenant);
            // Barreira: as duas requisições chegam ao motor quase ao mesmo tempo,
            // como na jornada S4 (dois médicos clicando em "iniciar" juntos).
            using var gate = new ManualResetEventSlim(false);
            var t1 = Task.Run(async () => { gate.Wait(); return await svc.AcaoAsync("triagens", triagemId, "iniciar", new Saude360ActionRequest()); });
            var t2 = Task.Run(async () => { gate.Wait(); return await svc.AcaoAsync("triagens", triagemId, "iniciar", new Saude360ActionRequest()); });
            gate.Set();
            var par = await Task.WhenAll(t1, t2);
            var r1 = par[0];
            var r2 = par[1];

            Assert.Single(new[] { r1, r2 }, r => r.Success && r.StatusCode == 200);
            Assert.Single(new[] { r1, r2 }, r => !r.Success && r.StatusCode == 409 && r.Message == MsgConflito);

            await using var cn2 = new NpgsqlConnection(cs);
            await cn2.OpenAsync();
            Assert.Equal("EM_TRIAGEM", await cn2.ExecuteScalarAsync<string>("select status from plantaopro.triagens where id=@id", new { id = triagemId }));
            Assert.Equal(1, await cn2.ExecuteScalarAsync<int>("select count(1) from plantaopro.triagem_historico where triagem_id=@id and acao='iniciar'", new { id = triagemId }));

            // Repetição sequencial após o sucesso também é bloqueada (F1b guard).
            var repetida = await svc.AcaoAsync("triagens", triagemId, "iniciar", new Saude360ActionRequest());
            Assert.False(repetida.Success);
            Assert.Equal(409, repetida.StatusCode);
            Assert.Equal(MsgConflito, repetida.Message);
            Assert.Equal(1, await cn2.ExecuteScalarAsync<int>("select count(1) from plantaopro.triagem_historico where triagem_id=@id and acao='iniciar'", new { id = triagemId }));
        }
        finally
        {
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            await cn.ExecuteAsync(@"delete from plantaopro.triagem_historico where triagem_id=@id;
delete from plantaopro.triagem_encaminhamentos where triagem_id=@id;
delete from plantaopro.triagens where id=@id;", new { id = triagemId });
        }
    }

    [Fact]
    public async Task F1b_Triagem_Finalizada_NaoReinicia_E_FinalizarRetrying_NaoDuplicaHistorico()
    {
        var cs = TestDatabase.ConnectionString;
        var tenant = Guid.NewGuid();
        var triagemId = Guid.NewGuid();
        var pacienteId = Guid.NewGuid();

        await using (var cn = new NpgsqlConnection(cs))
        {
            await cn.OpenAsync();
            await cn.ExecuteAsync(@"insert into plantaopro.triagens(id, cliente_id, paciente_id, status, reg_status, reg_date)
values(@id, @tenant, @paciente, 'AGUARDANDO', 'A', now())", new { id = triagemId, tenant, paciente = pacienteId });
        }

        try
        {
            var svc = BuildService(tenant);
            var iniciar = await svc.AcaoAsync("triagens", triagemId, "iniciar", new Saude360ActionRequest());
            Assert.True(iniciar.Success, iniciar.Message);

            var finalizar1 = await svc.AcaoAsync("triagens", triagemId, "finalizar", new Saude360ActionRequest());
            Assert.True(finalizar1.Success, finalizar1.Message);

            // Retry seguro: segunda finalização devolve o registro sem repetir histórico/encaminhamento.
            var finalizar2 = await svc.AcaoAsync("triagens", triagemId, "finalizar", new Saude360ActionRequest());
            Assert.True(finalizar2.Success, finalizar2.Message);
            Assert.Equal(200, finalizar2.StatusCode);

            // Triagem finalizada não pode ser reiniciada.
            var reiniciar = await svc.AcaoAsync("triagens", triagemId, "iniciar", new Saude360ActionRequest());
            Assert.False(reiniciar.Success);
            Assert.Equal(409, reiniciar.StatusCode);
            Assert.Contains("não pode ser reiniciada", reiniciar.Message);

            await using var cn2 = new NpgsqlConnection(cs);
            await cn2.OpenAsync();
            Assert.Equal("FINALIZADA", await cn2.ExecuteScalarAsync<string>("select status from plantaopro.triagens where id=@id", new { id = triagemId }));
            Assert.Equal(1, await cn2.ExecuteScalarAsync<int>("select count(1) from plantaopro.triagem_historico where triagem_id=@id and acao='finalizar'", new { id = triagemId }));
        }
        finally
        {
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            await cn.ExecuteAsync(@"delete from plantaopro.triagem_historico where triagem_id=@id;
delete from plantaopro.triagem_encaminhamentos where triagem_id=@id;
delete from plantaopro.triagens where id=@id;", new { id = triagemId });
        }
    }

    [Fact]
    public async Task F1b_Consulta_IniciarConcorrente_ExatamenteUmSucesso_ViraEmAtendimento_HistoricoUnico()
    {
        var cs = TestDatabase.ConnectionString;
        var tenant = Guid.NewGuid();
        var consultaId = Guid.NewGuid();
        var pacienteId = Guid.NewGuid();
        var medicoId = Guid.NewGuid();

        await using (var cn = new NpgsqlConnection(cs))
        {
            await cn.OpenAsync();
            await cn.ExecuteAsync(@"insert into plantaopro.consultas(id, cliente_id, paciente_id, medico_id, status, reg_status, reg_date)
values(@id, @tenant, @paciente, @medico, 'AGUARDANDO', 'A', now())", new { id = consultaId, tenant, paciente = pacienteId, medico = medicoId });
        }

        try
        {
            var svc = BuildService(tenant);
            using var gate = new ManualResetEventSlim(false);
            var t1 = Task.Run(async () => { gate.Wait(); return await svc.AcaoAsync("consultas", consultaId, "iniciar", new Saude360ActionRequest()); });
            var t2 = Task.Run(async () => { gate.Wait(); return await svc.AcaoAsync("consultas", consultaId, "iniciar", new Saude360ActionRequest()); });
            gate.Set();
            var par = await Task.WhenAll(t1, t2);
            var r1 = par[0];
            var r2 = par[1];

            Assert.Single(new[] { r1, r2 }, r => r.Success && r.StatusCode == 200);
            Assert.Single(new[] { r1, r2 }, r => !r.Success && r.StatusCode == 409 && r.Message == MsgConflito);

            await using var cn2 = new NpgsqlConnection(cs);
            await cn2.OpenAsync();
            var status = await cn2.ExecuteScalarAsync<string>("select status from plantaopro.consultas where id=@id", new { id = consultaId });
            Assert.Equal("EM_ATENDIMENTO", status);
            Assert.NotNull(await cn2.ExecuteScalarAsync<DateTime?>("select data_inicio from plantaopro.consultas where id=@id", new { id = consultaId }));
            Assert.Equal(1, await cn2.ExecuteScalarAsync<int>("select count(1) from plantaopro.consulta_historico where consulta_id=@id and acao='iniciar'", new { id = consultaId }));

            // Terceiro clique (sequencial) após o atendimento iniciado => 409.
            var repetida = await svc.AcaoAsync("consultas", consultaId, "iniciar", new Saude360ActionRequest());
            Assert.False(repetida.Success);
            Assert.Equal(409, repetida.StatusCode);
            Assert.Equal(MsgConflito, repetida.Message);
        }
        finally
        {
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            await cn.ExecuteAsync(@"delete from plantaopro.consulta_historico where consulta_id=@id;
delete from plantaopro.consultas where id=@id;", new { id = consultaId });
        }
    }

    [Fact]
    public async Task F1b_Consulta_Finalizar_IdempotenciaContratada_HistoricoUnico_E_DuplicadoDa409()
    {
        var cs = TestDatabase.ConnectionString;
        var tenant = Guid.NewGuid();
        var consultaId = Guid.NewGuid();
        var pacienteId = Guid.NewGuid();
        var medicoId = Guid.NewGuid();

        await using (var cn = new NpgsqlConnection(cs))
        {
            await cn.OpenAsync();
            await cn.ExecuteAsync(@"insert into plantaopro.consultas(id, cliente_id, paciente_id, medico_id, status, reg_status, reg_date)
values(@id, @tenant, @paciente, @medico, 'AGUARDANDO', 'A', now())", new { id = consultaId, tenant, paciente = pacienteId, medico = medicoId });
        }

        try
        {
            var svc = BuildService(tenant);
            var iniciar = await svc.AcaoAsync("consultas", consultaId, "iniciar", new Saude360ActionRequest());
            Assert.True(iniciar.Success, iniciar.Message);

            var finalizar1 = await svc.AcaoAsync("consultas", consultaId, "finalizar", new Saude360ActionRequest());
            Assert.True(finalizar1.Success, finalizar1.Message);

            // Diferente de triagem: consulta finalizada é estado terminal —
            // qualquer nova ação (inclusive re-finalizar) é 409, sem duplicar histórico.
            var finalizar2 = await svc.AcaoAsync("consultas", consultaId, "finalizar", new Saude360ActionRequest());
            Assert.False(finalizar2.Success);
            Assert.Equal(409, finalizar2.StatusCode);
            Assert.Contains("não pode ser alterada", finalizar2.Message);

            var reiniciar = await svc.AcaoAsync("consultas", consultaId, "iniciar", new Saude360ActionRequest());
            Assert.False(reiniciar.Success);
            Assert.Equal(409, reiniciar.StatusCode);

            await using var cn2 = new NpgsqlConnection(cs);
            await cn2.OpenAsync();
            Assert.Equal("FINALIZADA", await cn2.ExecuteScalarAsync<string>("select status from plantaopro.consultas where id=@id", new { id = consultaId }));
            Assert.NotNull(await cn2.ExecuteScalarAsync<DateTime?>("select finalizada_em from plantaopro.consultas where id=@id", new { id = consultaId }));
            Assert.Equal(1, await cn2.ExecuteScalarAsync<int>("select count(1) from plantaopro.consulta_historico where consulta_id=@id and acao='finalizar'", new { id = consultaId }));
        }
        finally
        {
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            await cn.ExecuteAsync(@"delete from plantaopro.consulta_historico where consulta_id=@id;
delete from plantaopro.consultas where id=@id;", new { id = consultaId });
        }
    }

    [Fact]
    public async Task F1b_AcaoEmRegistroInexistente_Retorna404_SemGravarHistorico()
    {
        var cs = TestDatabase.ConnectionString;
        var tenant = Guid.NewGuid();
        var triagemId = Guid.NewGuid();
        var consultaId = Guid.NewGuid();

        var svc = BuildService(tenant);
        var rTriagem = await svc.AcaoAsync("triagens", triagemId, "iniciar", new Saude360ActionRequest());
        Assert.False(rTriagem.Success);
        Assert.Equal(404, rTriagem.StatusCode);
        Assert.Equal(MsgNaoEncontrado, rTriagem.Message);

        var rConsulta = await svc.AcaoAsync("consultas", consultaId, "iniciar", new Saude360ActionRequest());
        Assert.False(rConsulta.Success);
        Assert.Equal(404, rConsulta.StatusCode);
        Assert.Equal(MsgNaoEncontrado, rConsulta.Message);

        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        Assert.Equal(0, await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.triagem_historico where triagem_id=@id", new { id = triagemId }));
        Assert.Equal(0, await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.consulta_historico where consulta_id=@id", new { id = consultaId }));
    }

    [Fact]
    public async Task F1b_IsolamentoDeTenant_TenantB_NaoVêTriagemDeTenantA_E_Retorna404()
    {
        var cs = TestDatabase.ConnectionString;
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var triagemId = Guid.NewGuid();
        var pacienteId = Guid.NewGuid();

        await using (var cn = new NpgsqlConnection(cs))
        {
            await cn.OpenAsync();
            await cn.ExecuteAsync(@"insert into plantaopro.triagens(id, cliente_id, paciente_id, status, reg_status, reg_date)
values(@id, @tenant, @paciente, 'AGUARDANDO', 'A', now())", new { id = triagemId, tenant = tenantA, paciente = pacienteId });
        }

        try
        {
            var svcB = BuildService(tenantB);
            var r = await svcB.AcaoAsync("triagens", triagemId, "iniciar", new Saude360ActionRequest());
            Assert.False(r.Success);
            Assert.Equal(404, r.StatusCode);
            Assert.Equal(MsgNaoEncontrado, r.Message);

            await using var cn2 = new NpgsqlConnection(cs);
            await cn2.OpenAsync();
            // Registro do tenant A intocado (rollback/escopo server-side).
            Assert.Equal("AGUARDANDO", await cn2.ExecuteScalarAsync<string>("select status from plantaopro.triagens where id=@id", new { id = triagemId }));
            Assert.Equal(0, await cn2.ExecuteScalarAsync<int>("select count(1) from plantaopro.triagem_historico where triagem_id=@id", new { id = triagemId }));
        }
        finally
        {
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            await cn.ExecuteAsync(@"delete from plantaopro.triagem_historico where triagem_id=@id;
delete from plantaopro.triagens where id=@id;", new { id = triagemId });
        }
    }

    #endregion

    #region F4 — gate de módulo por contrato (Saude360ModuleFilter)

    private static AuthorizationFilterContext BuildFilterCtx(IConfiguration cfg, ICurrentUserService user, bool authenticated = true)
    {
        var http = new DefaultHttpContext();
        if (authenticated)
            http.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim("sub", Guid.NewGuid().ToString()),
            }, "TestAuth"));
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    private static async Task<(Guid moduloId, Guid assinaturaId)> SeedModuloS360Async(string cs, Guid tenant, string codigoModulo, bool habilitado, string status)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        var moduloId = Guid.NewGuid();
        var assinaturaId = Guid.NewGuid();
        // ux_modulos_sistema_codigo é única por código — usa código único por teste;
        // o gate do filtro lê tm.codigo_modulo (SAUDE360), não ms.codigo.
        await cn.ExecuteAsync(@"insert into plantaopro.modulos_sistema(id, codigo, nome, status, reg_status, reg_date)
values(@id, @codigo, @nome, 'ATIVO', 'A', now())", new { id = moduloId, codigo = "S360XT_" + tenant.ToString("N").Substring(0, 8), nome = "Saúde 360 (teste " + codigoModulo + ")" });
        await cn.ExecuteAsync(@"insert into plantaopro.tenant_modulos(id, tenant_id, cliente_id, modulo_id, codigo_modulo, codigo, nome, status, habilitado, origem, reg_status, reg_date)
values(@id, @tenant, @tenant, @moduloId, @codigoModulo, 'SAUDE360', 'Saúde 360', @status, @habilitado, 'TESTE_JORNADA_S', 'A', now())",
            new { id = assinaturaId, tenant, moduloId, codigoModulo, status, habilitado });
        return (moduloId, assinaturaId);
    }

    private static void AssertEnvelope403Objeto(ObjectResult obj)
    {
        Assert.Equal(403, obj.StatusCode);
        // Tipos anônimos não implementam IDictionary desde o .NET Core 3 — serializa e valida.
        var json = System.Text.Json.JsonSerializer.SerializeToElement(obj.Value!);
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.Equal(MsgModulo, json.GetProperty("message").GetString());
    }

    private static async Task CleanupModuloS360Async(string cs, Guid moduloId, Guid assinaturaId)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        await cn.ExecuteAsync("delete from plantaopro.tenant_modulos where id=@id", new { id = assinaturaId });
        await cn.ExecuteAsync("delete from plantaopro.modulos_sistema where id=@id", new { id = moduloId });
    }

    [Fact]
    public async Task F4_TenantSemContratoSaude360_BloqueadoCom403EMensagemDeContrato()
    {
        var cs = TestDatabase.ConnectionString;
        var tenant = Guid.NewGuid(); // nenhum contrato em tenant_modulos
        var filter = new Saude360ModuleFilter(new FakeCurrentUser(tenant), BuildCfg(cs));
        var ctx = BuildFilterCtx(BuildCfg(cs), new FakeCurrentUser(tenant));

        await filter.OnAuthorizationAsync(ctx);

        AssertEnvelope403Objeto(Assert.IsType<ObjectResult>(ctx.Result));
    }

    [Fact]
    public async Task F4_TenantComContratoAtivoHabilitado_PassaSemBloqueio()
    {
        var cs = TestDatabase.ConnectionString;
        var tenant = Guid.NewGuid();
        var (moduloId, assinaturaId) = await SeedModuloS360Async(cs, tenant, "SAUDE360", habilitado: true, status: "ATIVO");
        try
        {
            var user = new FakeCurrentUser(tenant);
            var ctx = BuildFilterCtx(BuildCfg(cs), user);
            var filter = new Saude360ModuleFilter(user, BuildCfg(cs));

            await filter.OnAuthorizationAsync(ctx);

            Assert.Null(ctx.Result);
        }
        finally
        {
            await CleanupModuloS360Async(cs, moduloId, assinaturaId);
        }
    }

    [Fact]
    public async Task F4_AssinaturaDesabilitada_BloqueiaTenant()
    {
        var cs = TestDatabase.ConnectionString;
        var tenant = Guid.NewGuid();
        var (moduloId, assinaturaId) = await SeedModuloS360Async(cs, tenant, "SAUDE360", habilitado: false, status: "ATIVO");
        try
        {
            var user = new FakeCurrentUser(tenant);
            var ctx = BuildFilterCtx(BuildCfg(cs), user);
            var filter = new Saude360ModuleFilter(user, BuildCfg(cs));

            await filter.OnAuthorizationAsync(ctx);

            AssertEnvelope403Objeto(Assert.IsType<ObjectResult>(ctx.Result));
        }
        finally
        {
            await CleanupModuloS360Async(cs, moduloId, assinaturaId);
        }
    }

    [Fact]
    public async Task F4_GlobalAdmin_FazBypassSemConsultarContrato()
    {
        var cs = TestDatabase.ConnectionString;
        var tenant = Guid.NewGuid(); // mesmo sem contrato, superadmin passa
        var user = new FakeCurrentUser(tenant, globalAdmin: true);
        var ctx = BuildFilterCtx(BuildCfg(cs), user);
        var filter = new Saude360ModuleFilter(user, BuildCfg(cs));

        await filter.OnAuthorizationAsync(ctx);

        Assert.Null(ctx.Result);
    }

    [Fact]
    public async Task F4_UsuarioAnonimo_DelegaParaCamadaDeAutenticacao()
    {
        var cs = TestDatabase.ConnectionString;
        var filter = new Saude360ModuleFilter(new FakeCurrentUser(Guid.NewGuid()), BuildCfg(cs));
        var ctx = BuildFilterCtx(BuildCfg(cs), new FakeCurrentUser(Guid.NewGuid()), authenticated: false);

        await filter.OnAuthorizationAsync(ctx);

        Assert.Null(ctx.Result);
    }

    [Fact]
    public async Task F4_AutenticadoSemTenantNoEscopo_BloqueadoCom403()
    {
        var cs = TestDatabase.ConnectionString;
        var filter = new Saude360ModuleFilter(new FakeCurrentUser(tenantId: null), BuildCfg(cs));
        var ctx = BuildFilterCtx(BuildCfg(cs), new FakeCurrentUser(tenantId: null));

        await filter.OnAuthorizationAsync(ctx);

        AssertEnvelope403Objeto(Assert.IsType<ObjectResult>(ctx.Result));
    }

    [Fact]
    public async Task F4_FalhaDoBanco_FailOpen_NaoBloqueiaTenant()
    {
        var unreachable = "Host=127.0.0.1;Port=59999;Database=plantaopro_test;Username=nobody;Password=nobody;Timeout=3;Command Timeout=3";
        var user = new FakeCurrentUser(Guid.NewGuid());
        var cfg = BuildCfg(unreachable);
        var filter = new Saude360ModuleFilter(user, cfg);
        var ctx = BuildFilterCtx(cfg, user);

        await filter.OnAuthorizationAsync(ctx);

        Assert.Null(ctx.Result); // verificação indisponível não derruba o módulo
    }

    #endregion

    #region F3 — correlação Web <-> API (contract tests de leitura de source)

    private static readonly string RepoRoot = RepositoryPathResolver.RepoRoot;
    private static string Read(string path) => File.ReadAllText(Path.Combine(RepoRoot, path));

    [Fact]
    public void F3_Bff_EnviaXCorrelationId_LogaDuracaoTotal_SemTokenNasMensagens()
    {
        var web = Read("backend/PlantaoPro.Web/Services/ProductivityWebService.cs");

        // Header de correlação via HttpRequestMessage (novo header por requisição).
        Assert.Contains("request.Headers.TryAddWithoutValidation(\"X-Correlation-ID\", correlationId)", web);

        // Log de sucesso com duração TOTAL (rede + leitura) e correlação.
        var successLine = web.Split('\n').FirstOrDefault(l => l.Contains("BFF->API GET") && l.Contains("LogInformation"));
        Assert.NotNull(successLine);
        Assert.Contains("DuracaoTotalMs={DuracaoTotalMs}", successLine!);
        Assert.Contains("CorrelationId={CorrelationId}", successLine!);
        // O token Bearer não pode vazar em nenhuma mensagem de log do BFF.
        Assert.DoesNotContain("token", successLine!, StringComparison.OrdinalIgnoreCase);

        // Stopwatch finalizado APÓS a leitura da resposta (duração total, não só headers):
        // o stopwatch.Stop() vem imediatamente antes do LogInformation de sucesso.
        var idx = web.IndexOf("BFF->API GET", StringComparison.Ordinal);
        Assert.True(idx > 0, "linha de log de sucesso BFF->API não encontrada");
        var antes = web.Substring(Math.Max(0, idx - 400), Math.Min(400, idx));
        Assert.Contains("_logger.LogInformation", antes);
        Assert.Contains("stopwatch.Stop()", antes);
        Assert.True(antes.LastIndexOf("stopwatch.Stop()", StringComparison.Ordinal) > antes.LastIndexOf("ReadFromJsonAsync", StringComparison.Ordinal),
            "o stopwatch deve parar após a leitura do corpo (duração total)");
    }

    [Fact]
    public void F3_ApiMiddleware_LogaCorrelationIdEmTodaRequisicaoConcluida()
    {
        var middleware = Read("backend/PlantaoPro.Api/RequestLoggingMiddleware.cs");
        var program = Read("backend/PlantaoPro.Api/Program.cs");

        // Correlação registrada em toda requisição (e não só nas lentas/erros).
        Assert.Contains("_logger.LogInformation(\"Requisição concluída CorrelationId={CorrelationId} Endpoint={Endpoint} Metodo={Metodo} Status={StatusCode} DuracaoMs={DuracaoMs}\"", middleware);
        // O middleware ecoa o CorrelationId em context.Items desde a entrada.
        Assert.Contains("ctx.Items[\"CorrelationId\"] = correlationId;", program);
    }

    #endregion
}
