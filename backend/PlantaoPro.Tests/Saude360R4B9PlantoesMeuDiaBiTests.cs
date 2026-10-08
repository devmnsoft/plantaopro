using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using PlantaoPro.Api.Productivity;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R4-B9 Saude 360 — Plantoes (jornada ate encerrado + contestacao), Meu Dia real e explicavel,
/// acoes derivadas (fechamento divergente, pagamento pendente, contestacao aberta),
/// BI por competencia/fuso/escopo, dashboards e relatorios com competencia correta.
/// </summary>
[Collection("saas-operacao-serial")] // KPIs premium comparam delta GLOBAL (g0..g1): churn concorrente em escalas/agendamentos invalida a contagem
public sealed class Saude360R4B9PlantoesMeuDiaBiTests
{
    static Saude360R4B9PlantoesMeuDiaBiTests() => DapperTypeHandlerRegistrar.RegistrarTodos();

    // ---------- infra ----------

    private sealed class B9FakeUser : ICurrentUserService
    {
        private readonly Guid? _tenant;
        public B9FakeUser(Guid? tenant, Guid userId) { _tenant = tenant; UserId = userId; }
        public Guid? UserId { get; }
        public Guid? TenantId => _tenant;
        public Guid? ClienteId => _tenant;
        public Guid? SessionId => null;
        public IReadOnlyCollection<string> Roles { get; } = Array.Empty<string>();
        public bool IsAuthenticated() => true;
        public bool IsGlobalAdmin() => false;
        public bool IsTenantAdmin() => _tenant.HasValue;
        public bool IsPartner() => false;
        public bool IsDoctor() => false;
        public bool HasRole(string role) => false;
    }

    private sealed class B9FakeAudit : IAuditService
    {
        public Task RegistrarAsync(Guid? usuarioId, Guid? clienteId, string entidade, Guid? entidadeId, string acao, object? detalhes, bool sucesso, string? ipOrigem, string? perfil, CancellationToken ct = default) => Task.CompletedTask;
        public Task LogAsync(Guid? userId, string acao, string entidade, Guid? registroId, string descricao, string? valorAnterior = null, string? valorNovo = null, string? ip = null, string? userAgent = null) => Task.CompletedTask;
    }

    private static IConfiguration Cfg() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
        .Build();

    private static PlantaoService PlantaoSvc() => new(Cfg(), new B9FakeAudit(), NullLogger<PlantaoService>.Instance, new PlantaoRegraService(), new PlantaoHistoricoService(), new PlantaoTransicaoService());
    private static FinanceiroService FinSvc(Guid? t, Guid u) => new(Cfg(), new B9FakeAudit(), new NotificacaoService(Cfg(), new B9FakeAudit(), NullLogger<NotificacaoService>.Instance), new B9FakeUser(t, u), NullLogger<FinanceiroService>.Instance);
    private static BiService BiSvc() => new(Cfg(), NullLogger<BiService>.Instance);
    private static ReportQueryService RelSvc() => new(Cfg());
    private static DashboardService DashSvc() => new(Cfg());
    private static DashboardPremiumService PremSvc() => new(Cfg(), NullLogger<DashboardPremiumService>.Instance);
    private static ProductivityActionRepository ProdRepo() => new(Cfg());

    // ---------- seeds ----------

    private static async Task<Guid> SeedHospAsync(NpgsqlConnection cn, Guid t) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.hospitais(tenant_id,cliente_id,nome,nome_fantasia,status) values(@t,@t,'B9 Hospedagem','B9 Hosp','ATIVO') returning id", new { t });

    private static async Task<Guid> SeedEspAsync(NpgsqlConnection cn, Guid t) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.especialidades(tenant_id,cliente_id,nome,status) values(@t,@t,'B9 Especialidade','ATIVO') returning id", new { t });

    private static async Task<Guid> SeedMedAsync(NpgsqlConnection cn, Guid t, Guid? usuarioId) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.medicos(tenant_id,cliente_id,nome,status,usuario_id) values(@t,@t,'B9 Medico','ATIVO',@u) returning id", new { t, u = usuarioId });

    private static async Task<Guid> SeedPltAsync(NpgsqlConnection cn, Guid t, Guid h, Guid e, DateTime i, DateTime f, decimal v, int vagas, int disp, string s) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.plantoes(tenant_id,cliente_id,hospital_id,especialidade_id,data_inicio,data_fim,valor,vagas,vagas_disponiveis,status,reg_status) values(@t,@t,@h,@e,@i,@f,@v,@vag,@disp,@s,'A') returning id", new { t, h, e, i, f, v, vag = vagas, disp, s });

    private static async Task<Guid> SeedEscAsync(NpgsqlConnection cn, Guid t, Guid p, Guid m, string s) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.escalas(tenant_id,cliente_id,plantao_id,medico_id,status) values(@t,@t,@p,@m,@s) returning id", new { t, p, m, s });

    private static async Task<Guid> SeedPgAsync(NpgsqlConnection cn, Guid t, Guid p, Guid es, Guid m, string s, decimal previsto, decimal? aprovado = null, decimal? pago = null, DateTime? dp = null, string? forma = null, DateTime? prev = null, decimal? apurado = null) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.pagamentos(tenant_id,cliente_id,plantao_id,escala_id,medico_id,status,valor_previsto,valor_aprovado,valor_pago,data_pagamento,forma_pagamento,data_prevista,valor_apurado,origem_pagamento,parametros_apuracao) values(@t,@t,@p,@es,@m,@s,@vp,@va,@pago,@dp::date,@f,@prev::date,@apur,'MANUAL','{}'::jsonb) returning id", new { t, p, es, m, s, vp = previsto, va = aprovado, pago, dp = dp?.ToString("yyyy-MM-dd"), f = forma, prev = prev?.ToString("yyyy-MM-dd"), apur = apurado });

    private static async Task<Guid> SeedFechAsync(NpgsqlConnection cn, Guid t, Guid p, string s, Guid u) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.fechamento_plantao(tenant_id,cliente_id,plantao_id,status,iniciado_por) values(@t,@t,@p,@s,@u) returning id", new { t, p, s, u });

    private static async Task SeedCheckinAsync(NpgsqlConnection cn, Guid t, Guid m, Guid es, DateTime ci, DateTime? co) =>
        await cn.ExecuteAsync("insert into plantaopro.medico_checkins(tenant_id,medico_id,escala_id,checkin_em,checkout_em) values(@t,@m,@e,@ci,@co)", new { t, m, e = es, ci, co });

    private static async Task SeedClienteAsync(NpgsqlConnection cn, Guid t, string nome) =>
        await cn.ExecuteAsync("insert into plantaopro.clientes(id,tenant_id,nome,status) values(@t,@t,@n,'ATIVO')", new { t, n = nome });

    private static async Task SeedAssAsync(NpgsqlConnection cn, Guid t, decimal valorContratado) =>
        await cn.ExecuteAsync("insert into plantaopro.assinaturas(tenant_id,cliente_id,status,valor_contratado,valor_mensal) values(@t,@t,'ATIVA',@vc,@vc)", new { t, vc = valorContratado });

    private static async Task SeedAgendAsync(NpgsqlConnection cn, Guid t) =>
        await cn.ExecuteAsync("insert into plantaopro.agendamentos(tenant_id,cliente_id,status) values(@t,@t,'AGENDADO')", new { t });

    private static async Task SeedContestacaoLegadaAsync(NpgsqlConnection cn, Guid t, Guid pg, decimal valorOriginal, Guid abertoPor) =>
        await cn.ExecuteAsync("insert into plantaopro.pagamento_contestacoes(tenant_id,cliente_id,pagamento_id,motivo,status,valor_original,aberto_por) values(@t,@t,@pg,'Legado sem situacao anterior','ABERTA',@v,@u)", new { t, pg, v = valorOriginal, u = abertoPor });

    /// <summary>Medico + plantao + escala + pagamento num unico contexto isolado.</summary>
    private static async Task<(Guid t, Guid h, Guid e, Guid m, Guid p, Guid es, Guid pg)> SeedPgContextAsync(
        NpgsqlConnection cn, Guid t, Guid u, string pgStatus, decimal previsto,
        decimal? aprovado = null, decimal? pago = null, DateTime? dp = null, string? forma = null, DateTime? prev = null, decimal? apurado = null)
    {
        var h = await SeedHospAsync(cn, t);
        var e = await SeedEspAsync(cn, t);
        var m = await SeedMedAsync(cn, t, u);
        var p = await SeedPltAsync(cn, t, h, e, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(5), previsto, 1, 0, "aberto");
        var es = await SeedEscAsync(cn, t, p, m, "confirmado");
        var pg = await SeedPgAsync(cn, t, p, es, m, pgStatus, previsto, aprovado, pago, dp, forma, prev, apurado);
        return (t, h, e, m, p, es, pg);
    }

    // ---------- helpers ----------

    private static long L(object o) => Convert.ToInt64(o);
    private static decimal M(object o) => Convert.ToDecimal(o);

    private static async Task LimparAsync(Guid t, params Guid[] userIds)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync(@"
delete from plantaopro.plantao_historico where plantao_id in (select id from plantaopro.plantoes where tenant_id=@t or cliente_id=@t);
delete from plantaopro.historico_pagamento where pagamento_id in (select id from plantaopro.pagamentos where tenant_id=@t or cliente_id=@t);
delete from plantaopro.pagamento_contestacoes where tenant_id=@t or cliente_id=@t;
delete from plantaopro.medico_checkins where tenant_id=@t;
delete from plantaopro.pagamentos where tenant_id=@t or cliente_id=@t;
delete from plantaopro.fechamento_plantao where tenant_id=@t or cliente_id=@t;
delete from plantaopro.plantao_convites where plantao_id in (select id from plantaopro.plantoes where tenant_id=@t or cliente_id=@t);
delete from plantaopro.escalas where tenant_id=@t or cliente_id=@t;
delete from plantaopro.agendamentos where tenant_id=@t or cliente_id=@t;
delete from plantaopro.plantoes where tenant_id=@t or cliente_id=@t;
delete from plantaopro.medicos where tenant_id=@t or cliente_id=@t;
delete from plantaopro.hospitais where tenant_id=@t or cliente_id=@t;
delete from plantaopro.especialidades where tenant_id=@t or cliente_id=@t;
delete from plantaopro.assinaturas where tenant_id=@t or cliente_id=@t;
delete from plantaopro.clientes where tenant_id=@t or id=@t;", new { t });
        if (userIds != null && userIds.Length > 0)
            await cn.ExecuteAsync("delete from plantaopro.notificacoes where usuario_id=any(@uids)", new { uids = userIds });
    }

    [Fact]
    public async Task Encerrar_PlantaoInexistente_Retorna404SemPontoFinal()
    {
        var svc = PlantaoSvc();
        var r = await svc.EncerrarAsync(Guid.NewGuid(), "Encerrando plantao do teste.", Guid.NewGuid(), null, null);
        Assert.False(r.Success);
        Assert.Equal(404, r.StatusCode);
        Assert.Equal("Plantão não encontrado", r.Message); // B9: ancora exata, SEM ponto final
    }

    [Fact]
    public async Task Encerrar_PlantaoNaoRealizado_Retorna409ComMensagemExata()
    {
        var t = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var h = await SeedHospAsync(cn, t);
        var e = await SeedEspAsync(cn, t);
        var p = await SeedPltAsync(cn, t, h, e, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(5), 100m, 1, 1, "aberto");
        try
        {
            var r = await PlantaoSvc().EncerrarAsync(p, null, Guid.NewGuid(), null, null);
            Assert.False(r.Success);
            Assert.Equal(409, r.StatusCode);
            Assert.Equal("Somente plantão realizado pode ser encerrado.", r.Message);
        }
        finally { await LimparAsync(t); }
    }

    [Fact]
    public async Task Encerrar_RealizadoSemFechamentoOuFechamentoEmConferencia_Retorna409Financeiro()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var h = await SeedHospAsync(cn, t);
        var e = await SeedEspAsync(cn, t);
        var p = await SeedPltAsync(cn, t, h, e, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 100m, 1, 0, "realizado");
        try
        {
            var svc = PlantaoSvc();
            var r1 = await svc.EncerrarAsync(p, "Fechamento pendente.", u, null, null);
            Assert.False(r1.Success);
            Assert.Equal(409, r1.StatusCode);
            Assert.Equal("Conclua o fechamento financeiro antes de encerrar o plantão.", r1.Message);

            await SeedFechAsync(cn, t, p, "EM_CONFERENCIA", u);
            var r2 = await svc.EncerrarAsync(p, "Fechamento pendente.", u, null, null);
            Assert.False(r2.Success);
            Assert.Equal(409, r2.StatusCode);
            Assert.Equal("Conclua o fechamento financeiro antes de encerrar o plantão.", r2.Message);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Encerrar_JornadaRascunhoAteEncerrado_RegistraHistoricoCompleto()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var h = await SeedHospAsync(cn, t);
        var e = await SeedEspAsync(cn, t);
        var i = DateTime.UtcNow.Date.AddDays(-30).AddHours(8);
        var p = await SeedPltAsync(cn, t, h, e, i, i.AddHours(10), 60m, 1, 1, "rascunho");
        try
        {
            var svc = PlantaoSvc();
            var pub = await svc.PublicarAsync(p, "Publicacao para teste B9", u, null, null);
            Assert.True(pub.Success);
            Assert.Equal("aberto", await cn.ExecuteScalarAsync<string>("select status from plantaopro.plantoes where id=@id", new { id = p }));

            // fixture de homologacao: avanca direto para realizado (RealizarAsync exige presencas/escalas reconciliadas)
            await cn.ExecuteAsync("update plantaopro.plantoes set status='realizado' where id=@id", new { id = p });
            await SeedFechAsync(cn, t, p, "FINANCEIRO_GERADO", u);

            var enc = await svc.EncerrarAsync(p, "Jornada financeira concluida.", u, null, null);
            Assert.True(enc.Success);
            Assert.Equal("encerrado", await cn.ExecuteScalarAsync<string>("select status from plantaopro.plantoes where id=@id", new { id = p }));

            var hist = await svc.ListarHistoricoAsync(p);
            Assert.True(hist.Success);
            var itens = hist.Data!.ToList();
            Assert.Equal(2, itens.Count); // publicar + encerrar (v2318 corrigiu a defasagem da tabela)
            Assert.Equal("realizado", itens[0].StatusAnterior);
            Assert.Equal("encerrado", itens[0].StatusNovo);
            Assert.Equal("rascunho", itens[1].StatusAnterior);
            Assert.Equal("aberto", itens[1].StatusNovo);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Encerrar_JaEncerrado_EhIdempotenteSemNovaLinhaDeHistorico()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var h = await SeedHospAsync(cn, t);
        var e = await SeedEspAsync(cn, t);
        var p = await SeedPltAsync(cn, t, h, e, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 80m, 1, 0, "realizado");
        await SeedFechAsync(cn, t, p, "FINANCEIRO_GERADO", u);
        try
        {
            var svc = PlantaoSvc();
            var r1 = await svc.EncerrarAsync(p, "Primeiro encerramento.", u, null, null);
            Assert.True(r1.Success);
            var r2 = await svc.EncerrarAsync(p, "Segundo encerramento.", u, null, null);
            Assert.True(r2.Success);
            Assert.Equal(200, r2.StatusCode);
            Assert.Equal("O plantão já estava encerrado.", r2.Message);
            var total = await cn.ExecuteScalarAsync<int>("select count(1)::int from plantaopro.plantao_historico where plantao_id=@id", new { id = p });
            Assert.Equal(1, total);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Contestar_Pendente_MudaParaContestadoRegistraBasePrevistoESituacaoAnterior()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var (_, _, _, _, _, _, pg) = await SeedPgContextAsync(cn, t, u, "pendente", 100m);
        try
        {
            var fin = FinSvc(t, u);
            var r = await fin.ContestarAsync(pg, new ContestarPagamentoRequest("Divergencia de horas informadas no turno."), u, false, null, null);
            Assert.True(r.Success);
            Assert.Equal("Contestação registrada.", r.Message);
            Assert.NotNull(r.Data);
            Assert.Equal("contestado", r.Data!.Status);
            Assert.Equal(100m, r.Data.Valor);
            Assert.Equal("aguardar-resolucao", r.Data.ProximaAcao);

            Assert.Equal("contestado", await cn.ExecuteScalarAsync<string>("select status from plantaopro.pagamentos where id=@id", new { id = pg }));
            Assert.Equal(2L, await cn.ExecuteScalarAsync<long>("select versao::bigint from plantaopro.pagamentos where id=@id", new { id = pg }));
            Assert.Equal("ABERTA", await cn.ExecuteScalarAsync<string>("select status from plantaopro.pagamento_contestacoes where pagamento_id=@id", new { id = pg }));
            Assert.Equal("pendente", await cn.ExecuteScalarAsync<string>("select situacao_anterior from plantaopro.pagamento_contestacoes where pagamento_id=@id", new { id = pg }));
            Assert.Equal(100m, await cn.ExecuteScalarAsync<decimal>("select valor_original from plantaopro.pagamento_contestacoes where pagamento_id=@id", new { id = pg }));
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Contestar_ObrigacaoAprovadaComApurado_UseApuradoComoValorBase()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var (_, _, _, _, _, _, pg) = await SeedPgContextAsync(cn, t, u, "aprovado", 100m, aprovado: 100m, apurado: 77.5m);
        try
        {
            var fin = FinSvc(t, u);
            var r = await fin.ContestarAsync(pg, new ContestarPagamentoRequest("Valor apurado difere do previsto."), u, false, null, null);
            Assert.True(r.Success);
            Assert.Equal(77.5m, r.Data!.Valor);
            Assert.Equal(77.5m, await cn.ExecuteScalarAsync<decimal>("select valor_original from plantaopro.pagamento_contestacoes where pagamento_id=@id", new { id = pg }));
            Assert.Equal("aprovado", await cn.ExecuteScalarAsync<string>("select situacao_anterior from plantaopro.pagamento_contestacoes where pagamento_id=@id", new { id = pg }));
            Assert.Equal("contestado", await cn.ExecuteScalarAsync<string>("select status from plantaopro.pagamentos where id=@id", new { id = pg }));
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Contestar_PagamentoPago_PermanecePagoESemIncrementoDeVersao()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var (_, _, _, _, _, _, pg) = await SeedPgContextAsync(cn, t, u, "pago", 60m, aprovado: 60m, pago: 60m, dp: DateTime.UtcNow, forma: "PIX");
        try
        {
            var fin = FinSvc(t, u);
            var r = await fin.ContestarAsync(pg, new ContestarPagamentoRequest("Pagamento registrado com duvida."), u, false, null, null);
            Assert.True(r.Success);
            Assert.Equal("pago", r.Data!.Status);
            Assert.Equal(60m, r.Data.Valor);
            Assert.Equal("pago", await cn.ExecuteScalarAsync<string>("select status from plantaopro.pagamentos where id=@id", new { id = pg }));
            Assert.Equal(1L, await cn.ExecuteScalarAsync<long>("select versao::bigint from plantaopro.pagamentos where id=@id", new { id = pg }));
            Assert.Equal("pago", await cn.ExecuteScalarAsync<string>("select situacao_anterior from plantaopro.pagamento_contestacoes where pagamento_id=@id", new { id = pg }));
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Contestar_DuplicadaAberta_Retorna409()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var (_, _, _, _, _, _, pg) = await SeedPgContextAsync(cn, t, u, "pendente", 100m);
        try
        {
            // A guarda de status so verifica 'pendente'/'aprovado'/'pago'; a unicidade de uma
            // contestacao ABERTA por pagamento e garantida pela constraint do banco (23505 -> 409).
            // O pagamento segue 'pendente' com a contestacao ABERTA ja aberta fora da transacao.
            await SeedContestacaoLegadaAsync(cn, t, pg, 100m, u);
            var fin = FinSvc(t, u);
            var r = await fin.ContestarAsync(pg, new ContestarPagamentoRequest("Nova contestacao com aberta existente."), u, false, null, null);
            Assert.False(r.Success);
            Assert.Equal(409, r.StatusCode);
            Assert.Equal("Já existe contestação aberta para este pagamento.", r.Message);
            Assert.Equal("pendente", await cn.ExecuteScalarAsync<string>("select status from plantaopro.pagamentos where id=@id", new { id = pg }));
            Assert.Equal(1L, await cn.ExecuteScalarAsync<long>("select versao::bigint from plantaopro.pagamentos where id=@id", new { id = pg }));
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Contestar_ValorBaseNaoPositivo400_EStatusIncompativel409()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var fin = FinSvc(t, u);
        try
        {
            var c1 = await SeedPgContextAsync(cn, t, u, "pendente", 0m);
            var r1 = await fin.ContestarAsync(c1.pg, new ContestarPagamentoRequest("Previsto zero nao contesta."), u, false, null, null);
            Assert.False(r1.Success);
            Assert.Equal(400, r1.StatusCode);
            Assert.Equal("Valor previsto inválido para contestação.", r1.Message);

            var c2 = await SeedPgContextAsync(cn, t, u, "cancelado", 50m);
            var r2 = await fin.ContestarAsync(c2.pg, new ContestarPagamentoRequest("Obrigacao ja cancelada."), u, false, null, null);
            Assert.False(r2.Success);
            Assert.Equal(409, r2.StatusCode);
            Assert.Equal("Somente pagamento pendente pode ser contestado ou obrigação aprovada.", r2.Message);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Contestar_GuardasDeEntrada_MotivoObrigatorioETenantInvalido()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var (_, _, _, _, _, _, pg) = await SeedPgContextAsync(cn, t, u, "pendente", 100m);
        try
        {
            var r1 = await FinSvc(t, u).ContestarAsync(pg, new ContestarPagamentoRequest(""), u, false, null, null);
            Assert.False(r1.Success);
            Assert.Equal(400, r1.StatusCode);
            Assert.Equal("Motivo obrigatório.", r1.Message);

            var r2 = await FinSvc(null, u).ContestarAsync(pg, new ContestarPagamentoRequest("Tenant invalido na contestacao."), Guid.NewGuid(), false, null, null);
            Assert.False(r2.Success);
            Assert.Equal(401, r2.StatusCode);
            Assert.Equal("Contexto de tenant inválido.", r2.Message);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Resolver_ManterValor_ReverteSituacaoAnteriorEValorPrevisto()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var (_, _, _, _, _, _, pg) = await SeedPgContextAsync(cn, t, u, "pendente", 100m);
        var fin = FinSvc(t, u);
        try
        {
            Assert.True((await fin.ContestarAsync(pg, new ContestarPagamentoRequest("Manter apos analise das horas."), u, false, null, null)).Success);
            var r = await fin.ResolverContestacaoAsync(pg, new ResolverContestacaoPagamentoRequest("MANTER_VALOR", "Conferido; mantemos o valor conforme contrato.", null), u, null, null);
            Assert.True(r.Success);
            Assert.Equal("Contestação resolvida.", r.Message);
            Assert.Equal("pendente", r.Data!.Status);
            Assert.Equal(100m, r.Data.Valor);
            Assert.Equal("nenhuma", r.Data.ProximaAcao);
            Assert.Equal("pendente", await cn.ExecuteScalarAsync<string>("select status from plantaopro.pagamentos where id=@id", new { id = pg }));
            Assert.Equal(100m, await cn.ExecuteScalarAsync<decimal>("select valor_aprovado from plantaopro.pagamentos where id=@id", new { id = pg }));
            Assert.Equal("RESOLVIDA", await cn.ExecuteScalarAsync<string>("select status from plantaopro.pagamento_contestacoes where pagamento_id=@id", new { id = pg }));
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Resolver_AjustarValor_AtualizaPagamentoERegistraValorResolvido()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var (_, _, _, _, _, _, pg) = await SeedPgContextAsync(cn, t, u, "aprovado", 100m, aprovado: 100m);
        var fin = FinSvc(t, u);
        try
        {
            Assert.True((await fin.ContestarAsync(pg, new ContestarPagamentoRequest("Ajuste por horas extras nao pagas."), u, false, null, null)).Success);
            var r = await fin.ResolverContestacaoAsync(pg, new ResolverContestacaoPagamentoRequest("AJUSTAR_VALOR", "Ajustamos o valor apurado do medico.", 80m), u, null, null);
            Assert.True(r.Success);
            Assert.Equal("aprovado", r.Data!.Status);
            Assert.Equal(80m, r.Data.Valor);
            Assert.Equal(80m, await cn.ExecuteScalarAsync<decimal>("select valor_aprovado from plantaopro.pagamentos where id=@id", new { id = pg }));
            Assert.Equal("aprovado", await cn.ExecuteScalarAsync<string>("select status from plantaopro.pagamentos where id=@id", new { id = pg }));
            Assert.Equal(80m, await cn.ExecuteScalarAsync<decimal>("select valor_resolvido from plantaopro.pagamento_contestacoes where pagamento_id=@id", new { id = pg }));
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Resolver_CancelarPagamento_MarcaStatusCancelado()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var (_, _, _, _, _, _, pg) = await SeedPgContextAsync(cn, t, u, "pendente", 90m);
        var fin = FinSvc(t, u);
        try
        {
            Assert.True((await fin.ContestarAsync(pg, new ContestarPagamentoRequest("Obrigacao nao se aplica ao turno."), u, false, null, null)).Success);
            var r = await fin.ResolverContestacaoAsync(pg, new ResolverContestacaoPagamentoRequest("CANCELAR_PAGAMENTO", "Cancelamos a obrigacao financeira.", null), u, null, null);
            Assert.True(r.Success);
            Assert.Equal("cancelado", r.Data!.Status);
            Assert.Equal("cancelado", await cn.ExecuteScalarAsync<string>("select status from plantaopro.pagamentos where id=@id", new { id = pg }));
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Resolver_PagamentoPago_AjustarExigeEstornoANteDoManterOk()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var (_, _, _, _, _, _, pg) = await SeedPgContextAsync(cn, t, u, "pago", 60m, aprovado: 60m, pago: 60m, dp: DateTime.UtcNow, forma: "PIX");
        var fin = FinSvc(t, u);
        try
        {
            Assert.True((await fin.ContestarAsync(pg, new ContestarPagamentoRequest("Duvida sobre pagamento registrado."), u, false, null, null)).Success);
            var ajuste = await fin.ResolverContestacaoAsync(pg, new ResolverContestacaoPagamentoRequest("AJUSTAR_VALOR", "Ajuste direto sobre pago exigira estorno.", 30m), u, null, null);
            Assert.False(ajuste.Success);
            Assert.Equal(409, ajuste.StatusCode);
            Assert.Equal("Pagamento registrado exige estorno antes de ajuste ou cancelamento.", ajuste.Message);

            var manter = await fin.ResolverContestacaoAsync(pg, new ResolverContestacaoPagamentoRequest("MANTER_VALOR", "Mantemos o valor conforme registrado.", null), u, null, null);
            Assert.True(manter.Success);
            Assert.Equal("pago", manter.Data!.Status);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Resolver_LegadoSemSituacaoAnterior409_E404SemAberta_E422DecisaoInvalida()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var fin = FinSvc(t, u);
        try
        {
            var leg = await SeedPgContextAsync(cn, t, u, "contestado", 70m);
            await SeedContestacaoLegadaAsync(cn, t, leg.pg, 70m, u);
            var r1 = await fin.ResolverContestacaoAsync(leg.pg, new ResolverContestacaoPagamentoRequest("MANTER_VALOR", "Resolver legado sem situacao anterior.", null), u, null, null);
            Assert.False(r1.Success);
            Assert.Equal(409, r1.StatusCode);
            Assert.Equal("Contestação sem situação anterior registrada; resolva por manutenção de dados.", r1.Message);

            var sem = await SeedPgContextAsync(cn, t, u, "aprovado", 50m, aprovado: 50m);
            var r2 = await fin.ResolverContestacaoAsync(sem.pg, new ResolverContestacaoPagamentoRequest("MANTER_VALOR", "Resolver sem contestacao aberta existe.", null), u, null, null);
            Assert.False(r2.Success);
            Assert.Equal(404, r2.StatusCode);
            Assert.Equal("Contestação aberta não encontrada.", r2.Message);

            var r3 = await fin.ResolverContestacaoAsync(sem.pg, new ResolverContestacaoPagamentoRequest("TROCAR_COR", "Decisao fora do conjunto esperado.", null), u, null, null);
            Assert.False(r3.Success);
            Assert.Equal(422, r3.StatusCode);
            Assert.Equal("Decisão inválida.", r3.Message);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Estornar_Pago_ReverteParaAprovadoEZeraDadosDoRecebimento()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var (_, _, _, _, _, _, pg) = await SeedPgContextAsync(cn, t, u, "pago", 60m, aprovado: 60m, pago: 60m, dp: DateTime.UtcNow, forma: "PIX");
        try
        {
            var r = await FinSvc(t, u).EstornarPagamentoAsync(pg, new EstornarPagamentoRequest("Erro de digitação na conferência de caixa."), u, null, null);
            Assert.True(r.Success);
            Assert.Equal("Pagamento estornado.", r.Message);
            Assert.Equal("aprovado", r.Data!.Status);
            Assert.Equal(0m, r.Data.Valor);
            Assert.Equal("estorno", r.Data.ProximaAcao);
            Assert.Null(r.Data.DataPagamento);

            Assert.Equal("aprovado", await cn.ExecuteScalarAsync<string>("select status from plantaopro.pagamentos where id=@id", new { id = pg }));
            Assert.Null(await cn.ExecuteScalarAsync<decimal?>("select valor_pago from plantaopro.pagamentos where id=@id", new { id = pg }));
            Assert.Null(await cn.ExecuteScalarAsync<string?>("select forma_pagamento from plantaopro.pagamentos where id=@id", new { id = pg }));
            Assert.Null(await cn.ExecuteScalarAsync<DateOnly?>("select data_pagamento from plantaopro.pagamentos where id=@id", new { id = pg }));
            Assert.Equal(60m, await cn.ExecuteScalarAsync<decimal>("select valor_aprovado from plantaopro.pagamentos where id=@id", new { id = pg }));
            Assert.Equal(2L, await cn.ExecuteScalarAsync<long>("select versao::bigint from plantaopro.pagamentos where id=@id", new { id = pg }));
            var obs = await cn.ExecuteScalarAsync<string?>("select observacoes from plantaopro.pagamentos where id=@id", new { id = pg });
            Assert.Contains("Erro de digitação na conferência de caixa.", obs ?? "");
            Assert.True(await cn.ExecuteScalarAsync<int>("select count(1)::int from plantaopro.historico_pagamento where pagamento_id=@id", new { id = pg }) >= 1);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Estornar_SemRegistroDePagamento409_EEstornoDuplicado409()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var fin = FinSvc(t, u);
        try
        {
            var pend = await SeedPgContextAsync(cn, t, u, "pendente", 100m);
            var r1 = await fin.EstornarPagamentoAsync(pend.pg, new EstornarPagamentoRequest("Pendente ainda nao foi pago aqui."), u, null, null);
            Assert.False(r1.Success);
            Assert.Equal(409, r1.StatusCode);
            Assert.Equal("Somente pagamento registrado pode ser estornado.", r1.Message);

            var pago = await SeedPgContextAsync(cn, t, u, "pago", 60m, aprovado: 60m, pago: 60m, dp: DateTime.UtcNow, forma: "PIX");
            Assert.True((await fin.EstornarPagamentoAsync(pago.pg, new EstornarPagamentoRequest("Primeiro estorno valido e ok."), u, null, null)).Success);
            var r3 = await fin.EstornarPagamentoAsync(pago.pg, new EstornarPagamentoRequest("Segundo estorno sobre aprovado."), u, null, null);
            Assert.False(r3.Success);
            Assert.Equal(409, r3.StatusCode);
            Assert.Equal("Somente pagamento registrado pode ser estornado.", r3.Message);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Estornar_ComContestacaoAberta409()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var (_, _, _, _, _, _, pg) = await SeedPgContextAsync(cn, t, u, "pago", 60m, aprovado: 60m, pago: 60m, dp: DateTime.UtcNow, forma: "PIX");
        var fin = FinSvc(t, u);
        try
        {
            Assert.True((await fin.ContestarAsync(pg, new ContestarPagamentoRequest("Contestacao antes do estorno."), u, false, null, null)).Success);
            var r = await fin.EstornarPagamentoAsync(pg, new EstornarPagamentoRequest("Tentar estorno com aberta pendente."), u, null, null);
            Assert.False(r.Success);
            Assert.Equal(409, r.StatusCode);
            Assert.Equal("Resolva a contestação aberta antes de estornar o pagamento.", r.Message);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Estornar_GuardasDeEntrada_MotivoMinimoETenantInvalido()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var (_, _, _, _, _, _, pg) = await SeedPgContextAsync(cn, t, u, "pago", 60m, aprovado: 60m, pago: 60m, dp: DateTime.UtcNow, forma: "PIX");
        try
        {
            var r1 = await FinSvc(t, u).EstornarPagamentoAsync(pg, new EstornarPagamentoRequest("abc"), u, null, null);
            Assert.False(r1.Success);
            Assert.Equal(400, r1.StatusCode);
            Assert.Equal("Motivo obrigatório (mínimo 10 caracteres).", r1.Message);

            var r2 = await FinSvc(null, u).EstornarPagamentoAsync(pg, new EstornarPagamentoRequest("Tenant invalido no estorno."), Guid.NewGuid(), null, null);
            Assert.False(r2.Success);
            Assert.Equal(401, r2.StatusCode);
            Assert.Equal("Contexto de tenant inválido.", r2.Message);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task JornadaCompleta_ContestarBloqueiaEstornoResolveEPermiteEstorno()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var (_, _, _, _, _, _, pg) = await SeedPgContextAsync(cn, t, u, "pago", 50m, aprovado: 50m, pago: 50m, dp: DateTime.UtcNow, forma: "DINHEIRO");
        var fin = FinSvc(t, u);
        try
        {
            Assert.True((await fin.ContestarAsync(pg, new ContestarPagamentoRequest("Inicio da jornada completa B9."), u, false, null, null)).Success);
            var bloqueado = await fin.EstornarPagamentoAsync(pg, new EstornarPagamentoRequest("Estorno bloqueado pela aberta."), u, null, null);
            Assert.Equal(409, bloqueado.StatusCode);
            Assert.True((await fin.ResolverContestacaoAsync(pg, new ResolverContestacaoPagamentoRequest("MANTER_VALOR", "Resolvido mantendo valor pago.", null), u, null, null)).Success);
            var ok = await fin.EstornarPagamentoAsync(pg, new EstornarPagamentoRequest("Estorno liberado apos resolucao."), u, null, null);
            Assert.True(ok.Success);
            Assert.Equal("aprovado", ok.Data!.Status);
            var semAberta = await fin.ResolverContestacaoAsync(pg, new ResolverContestacaoPagamentoRequest("MANTER_VALOR", "Tentar resolver de novo sem aberta.", null), u, null, null);
            Assert.Equal(404, semAberta.StatusCode);
            Assert.Equal("Contestação aberta não encontrada.", semAberta.Message);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task MeuDiaMedico_SecoesLabelsTurnoComCheckinEIsolamentoDeTenant()
    {
        var t = Guid.NewGuid(); var tb = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            var h = await SeedHospAsync(cn, t);
            var e = await SeedEspAsync(cn, t);
            var m = await SeedMedAsync(cn, t, u);

            var i3 = DateTime.UtcNow.Date.AddDays(-1).AddHours(8);
            var p3 = await SeedPltAsync(cn, t, h, e, i3, i3.AddHours(10), 80m, 1, 0, "realizado");
            await SeedEscAsync(cn, t, p3, m, "confirmado");

            var i2 = DateTime.UtcNow.AddHours(-2);
            var p2 = await SeedPltAsync(cn, t, h, e, i2, i2.AddHours(4), 80m, 1, 0, "aberto");
            var e2 = await SeedEscAsync(cn, t, p2, m, "confirmado");
            await SeedCheckinAsync(cn, t, m, e2, DateTime.UtcNow.AddHours(-1), null);

            var i1 = DateTime.UtcNow.AddHours(10);
            var p1 = await SeedPltAsync(cn, t, h, e, i1, i1.AddHours(4), 80m, 1, 0, "aberto");
            await SeedEscAsync(cn, t, p1, m, "confirmado");

            // outro tenant com medico ligando ao MESMO usuario -> precisa ficar fora
            var hb = await SeedHospAsync(cn, tb);
            var eb = await SeedEspAsync(cn, tb);
            var mb = await SeedMedAsync(cn, tb, u);
            var ib = DateTime.UtcNow.AddHours(20);
            var pb = await SeedPltAsync(cn, tb, hb, eb, ib, ib.AddHours(4), 80m, 1, 0, "aberto");
            await SeedEscAsync(cn, tb, pb, mb, "confirmado");

            var itens = await ProdRepo().GetAgendaAsync(t, u, true, false, CancellationToken.None);
            Assert.Equal(3, itens.Count);
            Assert.Equal("CONFERÊNCIA", itens[0].Section);
            Assert.Equal("Turno finalizado · aguardando conferência e fechamento", itens[0].ContextLabel);
            Assert.StartsWith("Plantão", itens[0].Title);
            Assert.Equal("EM ANDAMENTO", itens[1].Section);
            Assert.Equal("Turno em execução · check-in registrado", itens[1].ContextLabel);
            Assert.StartsWith("Plantão", itens[1].Title);
            Assert.Equal("PRÓXIMO", itens[2].Section);
            Assert.Contains(itens[2].ContextLabel, new[] { "Turno confirmado · check-in pendente", "Plantão confirmado · aguardando início do turno" });
            Assert.StartsWith("Plantão", itens[2].Title);
        }
        finally { await LimparAsync(t, u); await LimparAsync(tb, u); }
    }

    [Fact]
    public async Task MeuDiaAdmin_CoberturaExecucaoEscalasEmOrdemDeInicioComExclusoes()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            var h = await SeedHospAsync(cn, t);
            var e = await SeedEspAsync(cn, t);

            var a3i = DateTime.UtcNow.AddHours(-1);
            var pa3 = await SeedPltAsync(cn, t, h, e, a3i, a3i.AddHours(4), 90m, 2, 2, "em_andamento");
            var a1i = DateTime.UtcNow.AddHours(2);
            var pa1 = await SeedPltAsync(cn, t, h, e, a1i, a1i.AddHours(4), 70m, 1, 1, "aberto");
            var a2i = DateTime.UtcNow.AddHours(3);
            var pa2 = await SeedPltAsync(cn, t, h, e, a2i, a2i.AddHours(4), 50m, 3, 0, "confirmado");
            var xi = DateTime.UtcNow.AddHours(4);
            await SeedPltAsync(cn, t, h, e, xi, xi.AddHours(4), 10m, 1, 1, "rascunho");
            var ci = DateTime.UtcNow.AddHours(5);
            await SeedPltAsync(cn, t, h, e, ci, ci.AddHours(4), 10m, 1, 1, "cancelado");

            var itens = await ProdRepo().GetAgendaAsync(t, u, false, true, CancellationToken.None);
            Assert.Equal(3, itens.Count);
            Assert.Equal("EXECUÇÃO", itens[0].Section);
            Assert.Equal("Em execução · acompanhe presenças e check-ins", itens[0].ContextLabel);
            Assert.StartsWith("Plantão", itens[0].Title);
            Assert.Equal("COBERTURA", itens[1].Section);
            Assert.Equal("1 vaga(s) ainda sem médico", itens[1].ContextLabel);
            Assert.StartsWith("Plantão", itens[1].Title);
            Assert.Equal("ESCALAS", itens[2].Section);
            Assert.Equal("3 de 3 vagas preenchidas", itens[2].ContextLabel);
            Assert.StartsWith("Plantão", itens[2].Title);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task AcoesDerivadas_FechamentoDivergentePagamentoPendenteContestacaoAbertaExplicaveis()
    {
        var t = Guid.NewGuid(); var uAdmin = Guid.NewGuid(); var uDoc = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            var h = await SeedHospAsync(cn, t);
            var e = await SeedEspAsync(cn, t);
            var m1 = await SeedMedAsync(cn, t, Guid.NewGuid());
            var m2 = await SeedMedAsync(cn, t, uDoc);
            var p = await SeedPltAsync(cn, t, h, e, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(5), 100m, 1, 1, "aberto");
            var e1 = await SeedEscAsync(cn, t, p, m1, "confirmado");
            var e2 = await SeedEscAsync(cn, t, p, m2, "confirmado");
            await SeedPgAsync(cn, t, p, e1, m1, "pendente", 100m);
            var pgHost = await SeedPgAsync(cn, t, p, e2, m2, "pago", 50m, 50m, 50m, DateTime.UtcNow, "PIX");
            await SeedFechAsync(cn, t, p, "COM_DIVERGENCIA", uAdmin);

            var contestar = await FinSvc(t, uDoc).ContestarAsync(pgHost, new ContestarPagamentoRequest("Revisar horas do plantão pago."), uDoc, false, null, null);
            Assert.True(contestar.Success);

            var page = await ProdRepo().ListAsync(t, uAdmin, new ProductivityQuery(), true, false, true, false, CancellationToken.None);
            Assert.Equal(3, page.Total);
            Assert.Equal(3, page.Items.Count);

            var fech = page.Items.Single(x => x.EntityType == "FECHAMENTO");
            Assert.Equal(ProductivityPriority.Alta, fech.Priority);
            Assert.Equal("Fechamento com divergência aberta; resolva antes de prosseguir.", fech.PriorityReason);

            var con = page.Items.Single(x => x.EntityType == "CONTESTACAO");
            Assert.Equal(ProductivityPriority.Alta, con.Priority);
            Assert.Equal("Contestação aberta; analise o motivo e decida (manter valor, ajustar ou cancelar).", con.PriorityReason);

            var pag = page.Items.Single(x => x.EntityType == "PAGAMENTO");
            Assert.Equal(ProductivityPriority.Normal, pag.Priority);
            Assert.Equal("Pagamento pendente; confira valores e horas antes de liberar.", pag.PriorityReason);
        }
        finally { await LimparAsync(t, uAdmin, uDoc); }
    }

    [Fact]
    public async Task Bi_ResumoExecutivoPorEscopoPeriodoEFuso_EDeterministico()
    {
        var tA = Guid.NewGuid(); var tB = Guid.NewGuid();
        var u1 = Guid.NewGuid(); var u2 = Guid.NewGuid(); var u3 = Guid.NewGuid(); var u4 = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedClienteAsync(cn, tA, "B9 Cliente A");
            await SeedAssAsync(cn, tA, 250m);
            var hA = await SeedHospAsync(cn, tA);
            var eA = await SeedEspAsync(cn, tA);
            var mA1 = await SeedMedAsync(cn, tA, u1);
            var mA2 = await SeedMedAsync(cn, tA, u2);
            var mA3 = await SeedMedAsync(cn, tA, u3);
            var mA4 = await SeedMedAsync(cn, tA, u4);

            var pubA = await SeedPltAsync(cn, tA, hA, eA, new DateTime(2026, 9, 10, 8, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 10, 20, 0, 0, DateTimeKind.Utc), 120m, 2, 1, "aberto");
            var rasA = await SeedPltAsync(cn, tA, hA, eA, new DateTime(2026, 9, 15, 8, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 15, 20, 0, 0, DateTimeKind.Utc), 10m, 1, 1, "rascunho");
            var octA = await SeedPltAsync(cn, tA, hA, eA, new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 10, 20, 0, 0, DateTimeKind.Utc), 10m, 1, 1, "aberto");

            var e1 = await SeedEscAsync(cn, tA, pubA, mA1, "confirmado");
            var e2 = await SeedEscAsync(cn, tA, pubA, mA2, "confirmado");
            var e3 = await SeedEscAsync(cn, tA, pubA, mA3, "confirmado");
            await SeedPgAsync(cn, tA, pubA, e1, mA1, "pendente", 100m);
            await SeedPgAsync(cn, tA, pubA, e2, mA2, "pago", 300m, 300m, 300m, new DateTime(2026, 9, 5), "PIX");
            await SeedPgAsync(cn, tA, pubA, e3, mA3, "pago", 120m, 120m, 120m, new DateTime(2026, 10, 15), "DINHEIRO");
            await SeedEscAsync(cn, tA, octA, mA4, "cancelado");

            // tenant B fora do escopo: plantao publicado no mes e escala confirmada
            await SeedClienteAsync(cn, tB, "B9 Cliente B");
            await SeedAssAsync(cn, tB, 999m);
            var hB = await SeedHospAsync(cn, tB);
            var eB = await SeedEspAsync(cn, tB);
            var mB = await SeedMedAsync(cn, tB, Guid.NewGuid());
            var pB = await SeedPltAsync(cn, tB, hB, eB, new DateTime(2026, 9, 12, 8, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 12, 20, 0, 0, DateTimeKind.Utc), 10m, 1, 1, "aberto");
            await SeedEscAsync(cn, tB, pB, mB, "confirmado");

            var r = await BiSvc().GetResumoExecutivoAsync(tA, "2026-09", "America/Sao_Paulo");
            Assert.True(r.Success);
            var d = r.Data!;
            Assert.Equal(1L, L(d.TotalClientesAtivos));
            Assert.Equal(250m, M(d.ReceitaMensalEstimada));
            Assert.Equal(0m, M(d.ReceitaVencida));
            Assert.Equal(1L, L(d.PlantoesPublicadosMes));
            Assert.Equal(50m, M(d.PercentualCobertura));
            Assert.Equal(3L, L(d.EscalasConfirmadas));       // acumulado (janela irrelevante)
            Assert.Equal(1L, L(d.EscalasCanceladas));
            Assert.Equal(1L, L(d.PagamentosPendentes));       // 'contestado' nao entra nos buckets
            Assert.Equal(2L, L(d.PagamentosConfirmados));
            Assert.Equal(3L, L(d.EscalasConfirmadasPeriodo)); // periodo != acumulado quando existe historico
            Assert.Equal(300m, M(d.ValorPagoPeriodo));        // pagamento de 15/10 fica fora da competencia 09
            Assert.InRange(M(d.TempoMedioPreenchimentoHoras), 0m, 1m);
            Assert.Equal("2026-09", d.Periodo);
            Assert.Equal("America/Sao_Paulo", d.Fuso);
        }
        finally { await LimparAsync(tA, u1, u2, u3, u4); await LimparAsync(tB); }
    }

    [Fact]
    public async Task Bi_Global_DeltaEntreSnapshotsEDefaultFusoSegueServidor()
    {
        var bi = BiSvc();
        var g0 = await bi.GetResumoExecutivoAsync(null, null, null);
        Assert.True(g0.Success, g0.Message ?? "fallo sem mensagem");
        var b0 = g0.Data!;

        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedClienteAsync(cn, t, "B9 Global");
            await SeedAssAsync(cn, t, 400m);
            var h = await SeedHospAsync(cn, t);
            var e = await SeedEspAsync(cn, t);
            var m1 = await SeedMedAsync(cn, t, u);
            var m2 = await SeedMedAsync(cn, t, Guid.NewGuid());
            var dia15 = DateTime.UtcNow.Date.AddDays(-(DateTime.UtcNow.Day - 1)).AddDays(15);
            var p = await SeedPltAsync(cn, t, h, e, dia15.AddHours(8), dia15.AddHours(16), 100m, 1, 1, "aberto");
            var e1 = await SeedEscAsync(cn, t, p, m1, "confirmado");
            var e2 = await SeedEscAsync(cn, t, p, m2, "confirmado");
            await SeedPgAsync(cn, t, p, e1, m1, "pendente", 100m);
            await SeedPgAsync(cn, t, p, e2, m2, "pago", 75m, 75m, 75m, DateTime.UtcNow, "DINHEIRO");

            var g1 = await bi.GetResumoExecutivoAsync(null, null, null);
            Assert.True(g1.Success, g1.Message ?? "fallo sem mensagem");
            var b1 = g1.Data!;
            Assert.Equal(1L, L(b1.TotalClientesAtivos) - L(b0.TotalClientesAtivos));
            Assert.Equal(400m, M(b1.ReceitaMensalEstimada) - M(b0.ReceitaMensalEstimada));
            Assert.Equal(1L, L(b1.PlantoesPublicadosMes) - L(b0.PlantoesPublicadosMes));
            Assert.Equal(2L, L(b1.EscalasConfirmadas) - L(b0.EscalasConfirmadas));
            Assert.Equal(2L, L(b1.EscalasConfirmadasPeriodo) - L(b0.EscalasConfirmadasPeriodo));
            Assert.Equal(1L, L(b1.PagamentosPendentes) - L(b0.PagamentosPendentes));
            Assert.Equal(1L, L(b1.PagamentosConfirmados) - L(b0.PagamentosConfirmados));
            Assert.Equal(75m, M(b1.ValorPagoPeriodo) - M(b0.ValorPagoPeriodo));

            // fuso/periodo default seguem o servidor, medidos de forma independente
            var fz = await cn.ExecuteScalarAsync<string>("select current_setting('TimeZone')");
            Assert.Equal(fz, b1.Fuso);
            var expPeriodo = await cn.ExecuteScalarAsync<string>("select to_char((now() at time zone @fz)::date,'YYYY-MM')", new { fz });
            Assert.Equal(expPeriodo, b1.Periodo);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Bi_PeriodoEFusoInvalidos_Retornam400ComMensagensExatas()
    {
        var bi = BiSvc();
        var r1 = await bi.GetResumoExecutivoAsync(null, "2026-13", null);
        Assert.False(r1.Success);
        Assert.Equal(400, r1.StatusCode);
        Assert.Equal("Período deve estar no formato yyyy-MM.", r1.Message);

        var r2 = await bi.GetResumoExecutivoAsync(null, "2026-9", null);
        Assert.False(r2.Success);
        Assert.Equal(400, r2.StatusCode);
        Assert.Equal("Período deve estar no formato yyyy-MM.", r2.Message);

        var r3 = await bi.GetResumoExecutivoAsync(null, "2026-09", "Marte/Olympus");
        Assert.False(r3.Success);
        Assert.Equal(400, r3.StatusCode);
        Assert.Equal("Fuso horário inválido.", r3.Message);
    }

    [Fact]
    public async Task Relatorio_PlantoesOperacional_CompeticenciaPorDataNegocioComEscopo()
    {
        var cat = new ReportCatalogService();
        var rep = cat.Obter("PLANTOES_OPERACIONAL");
        Assert.NotNull(rep);
        var svc = RelSvc();
        var d0 = DateTime.UtcNow.Date.AddDays(-5);
        var d1 = DateTime.UtcNow.Date.AddDays(-1);
        var tA = Guid.NewGuid(); var tB = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            var hA = await SeedHospAsync(cn, tA);
            var eA = await SeedEspAsync(cn, tA);
            await SeedPltAsync(cn, tA, hA, eA, d0.AddHours(10), d0.AddHours(12), 10m, 1, 1, "aberto");
            await SeedPltAsync(cn, tA, hA, eA, d1.AddHours(14), d1.AddHours(16), 10m, 1, 1, "confirmado");
            await SeedPltAsync(cn, tA, hA, eA, d0.AddDays(-1).AddHours(10), d0.AddDays(-1).AddHours(12), 10m, 1, 1, "aberto");

            var rowsA = await svc.ConsultarAsync(rep!, new ReportFilterRequest { Inicio = d0, Fim = d1 }, tA, CancellationToken.None);
            Assert.Equal(2, rowsA.Count);
            var porDia = rowsA.ToDictionary(x => Convert.ToString(x["periodo"])!, x => L(x["valor"]));
            Assert.Equal(1, porDia[d0.ToString("yyyy-MM-dd")]);
            Assert.Equal(1, porDia[d1.ToString("yyyy-MM-dd")]);
            Assert.All(rowsA, x => Assert.Equal("PLANTOES_OPERACIONAL", Convert.ToString(x["indicador"])));

            var hB = await SeedHospAsync(cn, tB);
            var eB = await SeedEspAsync(cn, tB);
            await SeedPltAsync(cn, tB, hB, eB, d0.AddHours(3), d0.AddHours(5), 10m, 1, 1, "aberto");
            var rowsB = await svc.ConsultarAsync(rep, new ReportFilterRequest { Inicio = d0, Fim = d1 }, tB, CancellationToken.None);
            Assert.Single(rowsB);
            Assert.Equal(d0.ToString("yyyy-MM-dd"), Convert.ToString(rowsB.First()["periodo"]));
        }
        finally { await LimparAsync(tA); await LimparAsync(tB); }
    }

    [Fact]
    public async Task Relatorio_PagamentosMedicos_FiltrosDeDimensaoAusenteEFormaCaseInsensitive()
    {
        var rep = new ReportCatalogService().Obter("PAGAMENTOS_MEDICOS");
        Assert.NotNull(rep);
        var svc = RelSvc();
        var d0 = DateTime.UtcNow.Date.AddDays(-5);
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            var h = await SeedHospAsync(cn, t);
            var e = await SeedEspAsync(cn, t);
            var m = await SeedMedAsync(cn, t, u);
            var p = await SeedPltAsync(cn, t, h, e, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(5), 100m, 1, 0, "aberto");
            var es = await SeedEscAsync(cn, t, p, m, "confirmado");
            await SeedPgAsync(cn, t, p, es, m, "pendente", 100m, prev: d0, forma: "DINHEIRO");

            var baseFiltros = () => new ReportFilterRequest { Inicio = d0, Fim = d0 };

            var r0 = await svc.ConsultarAsync(rep!, baseFiltros(), t, CancellationToken.None);
            Assert.Single(r0);
            Assert.Equal(d0.ToString("yyyy-MM-dd"), Convert.ToString(r0.First()["periodo"]));
            Assert.Equal(1L, L(r0.First()["valor"]));

            // dimensoes ausentes na tabela pagamentos -> sempre vazio (comportamento documentado)
            Assert.Empty(await svc.ConsultarAsync(rep, new ReportFilterRequest { Inicio = d0, Fim = d0, HospitalId = h }, t, CancellationToken.None));
            Assert.Empty(await svc.ConsultarAsync(rep, new ReportFilterRequest { Inicio = d0, Fim = d0, HospitalId = Guid.NewGuid() }, t, CancellationToken.None));
            Assert.Empty(await svc.ConsultarAsync(rep, new ReportFilterRequest { Inicio = d0, Fim = d0, ConvenioId = Guid.NewGuid() }, t, CancellationToken.None));
            Assert.Empty(await svc.ConsultarAsync(rep, new ReportFilterRequest { Inicio = d0, Fim = d0, EspecialidadeId = e }, t, CancellationToken.None));

            // forma pagamentoe case-insensitive via upper()
            Assert.Single(await svc.ConsultarAsync(rep, new ReportFilterRequest { Inicio = d0, Fim = d0, FormaPagamento = "dinheiro" }, t, CancellationToken.None));
            Assert.Empty(await svc.ConsultarAsync(rep, new ReportFilterRequest { Inicio = d0, Fim = d0, FormaPagamento = "PIX" }, t, CancellationToken.None));

            // medico ativo na dimensao
            Assert.Single(await svc.ConsultarAsync(rep, new ReportFilterRequest { Inicio = d0, Fim = d0, MedicoId = m }, t, CancellationToken.None));
            Assert.Empty(await svc.ConsultarAsync(rep, new ReportFilterRequest { Inicio = d0, Fim = d0, MedicoId = Guid.NewGuid() }, t, CancellationToken.None));
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task DashboardApi_AbsolutoPorTenantEDeltaGlobal()
    {
        var svc = DashSvc();
        var uDash = Guid.NewGuid();
        var g0 = await svc.GetAsync(uDash, null);
        Assert.True(g0.Success, g0.Message ?? "fallo sem mensagem");
        var b0 = g0.Data!.Indicadores;

        var tT = Guid.NewGuid(); var uT = Guid.NewGuid();
        var tU = Guid.NewGuid(); var uU = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            var hT = await SeedHospAsync(cn, tT);
            var eT = await SeedEspAsync(cn, tT);
            var mT = await SeedMedAsync(cn, tT, uT);
            var pt1 = await SeedPltAsync(cn, tT, hT, eT, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(5), 100m, 1, 1, "aberto");
            var pt2 = await SeedPltAsync(cn, tT, hT, eT, DateTime.UtcNow.AddHours(-5), DateTime.UtcNow.AddHours(-1), 100m, 1, 0, "cancelado");
            var eT1 = await SeedEscAsync(cn, tT, pt1, mT, "confirmado");
            var eT2 = await SeedEscAsync(cn, tT, pt2, mT, "confirmado");
            await SeedPgAsync(cn, tT, pt1, eT1, mT, "pendente", 100m);
            await SeedPgAsync(cn, tT, pt2, eT2, mT, "pago", 50m, 50m, 50m, DateTime.UtcNow, "DINHEIRO");

            var hU = await SeedHospAsync(cn, tU);
            var eU = await SeedEspAsync(cn, tU);
            var mU = await SeedMedAsync(cn, tU, uU);
            var pu1 = await SeedPltAsync(cn, tU, hU, eU, DateTime.UtcNow.AddHours(-6), DateTime.UtcNow.AddHours(-2), 10m, 1, 0, "cancelado");
            var eU1 = await SeedEscAsync(cn, tU, pu1, mU, "confirmado");
            await SeedPgAsync(cn, tU, pu1, eU1, mU, "pendente", 999m);

            var rT = await svc.GetAsync(uT, tT);
            Assert.True(rT.Success);
            var a = rT.Data!.Indicadores;
            Assert.Equal(1L, a.TotalMedicos);
            Assert.Equal(1L, a.TotalHospitais);
            Assert.Equal(1L, a.TotalEspecialidades);
            Assert.Equal(2L, a.TotalPlantoes);
            Assert.Equal(1L, a.PlantoesAbertos);
            Assert.Equal(0L, a.PlantoesConfirmados);
            Assert.Equal(0L, a.PlantoesRealizados);
            Assert.Equal(1L, a.PlantoesCancelados);
            Assert.Equal(1L, a.PagamentosPendentes);
            Assert.Equal(1L, a.PagamentosPagos);
            Assert.Equal(100m, a.ValorPendente);
            Assert.Equal(50m, a.ValorPagoMes);
            Assert.Equal(0L, a.NotificacoesNaoLidas);

            var g1 = await svc.GetAsync(uDash, null);
            Assert.True(g1.Success, g1.Message ?? "fallo sem mensagem");
            var b1 = g1.Data!.Indicadores;
            Assert.Equal(2L, b1.TotalMedicos - b0.TotalMedicos);
            Assert.Equal(2L, b1.TotalHospitais - b0.TotalHospitais);
            Assert.Equal(2L, b1.TotalEspecialidades - b0.TotalEspecialidades);
            Assert.Equal(3L, b1.TotalPlantoes - b0.TotalPlantoes);
            Assert.Equal(1L, b1.PlantoesAbertos - b0.PlantoesAbertos);
            Assert.Equal(2L, b1.PlantoesCancelados - b0.PlantoesCancelados);
            Assert.Equal(2L, b1.PagamentosPendentes - b0.PagamentosPendentes);
            Assert.Equal(1L, b1.PagamentosPagos - b0.PagamentosPagos);
            Assert.Equal(1099m, b1.ValorPendente - b0.ValorPendente);
            Assert.Equal(50m, b1.ValorPagoMes - b0.ValorPagoMes);
        }
        finally { await LimparAsync(tT, uT); await LimparAsync(tU, uU); }
    }

    [Fact]
    public async Task DashboardPremium_KpisReaisPorContextoEDeltaGlobal()
    {
        var svc = PremSvc();
        var g0 = await svc.ObterAsync("admin", null);

        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            var h = await SeedHospAsync(cn, t);
            var e = await SeedEspAsync(cn, t);
            var m = await SeedMedAsync(cn, t, u);
            var p1 = await SeedPltAsync(cn, t, h, e, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(5), 10m, 1, 1, "aberto");
            var p2 = await SeedPltAsync(cn, t, h, e, DateTime.UtcNow.AddHours(6), DateTime.UtcNow.AddHours(10), 10m, 1, 1, "confirmado");
            await SeedEscAsync(cn, t, p1, m, "confirmado");
            await SeedEscAsync(cn, t, p2, m, "confirmado");
            await SeedAgendAsync(cn, t);

            var r = await svc.ObterAsync("admin", t);
            var k = r.Kpis.ToList();
            Assert.Equal(2L, Kpi(k, "Plantões"));
            Assert.Equal(2L, Kpi(k, "Escalas"));
            Assert.Equal(1L, Kpi(k, "Médicos"));
            Assert.Equal(1L, Kpi(k, "Agendamentos"));
            Assert.All(k, x => Assert.Equal("real", x.Fonte));

            var g1 = await svc.ObterAsync("admin", null);
            Assert.Equal(2L, Kpi(g1.Kpis.ToList(), "Plantões") - Kpi(g0.Kpis.ToList(), "Plantões"));
            Assert.Equal(2L, Kpi(g1.Kpis.ToList(), "Escalas") - Kpi(g0.Kpis.ToList(), "Escalas"));
            Assert.Equal(1L, Kpi(g1.Kpis.ToList(), "Médicos") - Kpi(g0.Kpis.ToList(), "Médicos"));
            Assert.Equal(1L, Kpi(g1.Kpis.ToList(), "Agendamentos") - Kpi(g0.Kpis.ToList(), "Agendamentos"));
        }
        finally { await LimparAsync(t, u); }
    }

    private static long Kpi(IReadOnlyList<DashboardKpiDto> kpis, string titulo) => kpis.First(x => x.Titulo == titulo).Valor;

    [Fact]
    public void AncorasFonteB9_FixamRotasMigracaoManifestEEtiquetasDeUi()
    {
        var plantoesCtl = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Api", "Controllers", "PlantoesController.cs");
        Assert.Contains("[HttpPost(\"{id:guid}/encerrar\")]", plantoesCtl);
        Assert.Contains("[HttpPost(\"{id:guid}/realizar\")]", plantoesCtl);
        Assert.Contains("EncerrarAsync", plantoesCtl);

        var pgCtl = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Api", "Controllers", "PagamentosController.cs");
        Assert.Contains("[HttpPost(\"{id:guid}/estornar\")]", pgCtl);
        Assert.Contains("EstornarPagamentoAsync", pgCtl);

        var data = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Api", "Data.cs");
        Assert.Contains("O plantão já estava encerrado.", data);
        Assert.Contains("Conclua o fechamento financeiro antes de encerrar o plantão.", data);
        Assert.Contains("Somente plantão realizado pode ser encerrado.", data);
        Assert.Contains("situacao_anterior", data);
        Assert.Contains("Resolva a contestação aberta antes de estornar o pagamento.", data);

        var bi = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Api", "BiServices.cs");
        Assert.Contains("pg_timezone_names", bi);
        Assert.Contains("Período deve estar no formato yyyy-MM.", bi);
        Assert.Contains("valor_contratado", bi);

        var rel = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Api", "ReportServices.cs");
        Assert.Contains("@inicioDia::date", rel);
        Assert.Contains("forma_pagamento", rel);
        Assert.Contains("[0-9]{4}-[0-9]{2}-[0-9]{2}", rel);

        var prod = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Api", "ProductivityActionServices.cs");
        Assert.Contains("PriorityReason", prod);
        Assert.Contains("Fechamento com divergência aberta; resolva antes de prosseguir.", prod);
        Assert.Contains("GetAgendaAsync", prod);

        var meud = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Web", "Views", "MeuDia", "Index.cshtml");
        Assert.Contains("day-item-reason", meud);

        var biv = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Web", "Views", "Bi", "Index.cshtml");
        Assert.Contains("bi-periodo-fuso", biv);
        Assert.Contains("escalasConfirmadasPeriodo", biv);

        var mig = Directory.GetFiles(Path.Combine(RepositoryPathResolver.DatabaseRoot, "migrations"), "2026_10_v23*.sql");
        Assert.Contains(mig, f => f.Contains("v2317"));
        Assert.Contains(mig, f => f.Contains("v2318"));
        Assert.Contains(mig, f => f.Contains("v2319"));
        Assert.Contains(mig, f => f.Contains("v2320"));

        var manifest = RepositoryPathResolver.ReadRepositoryFile("database", "migration-manifest.json");
        Assert.Contains("2026_10_v2318", manifest);
        Assert.Contains("8a0f882bf14e8c6684805c1eaaac1840633a780a214729500e631c780ddd1074", manifest);
        Assert.Contains("2026_10_v2319", manifest);
        Assert.Contains("baca5925613bcd26ac0568892c7e159492d542f4cd2000c8abc537f76db9706a", manifest);
        Assert.Contains("2026_10_v2320", manifest);
        Assert.Contains("d9eec352ba21c23e6d76ded2718c70f52cf089ffa59405436d580692045503f7", manifest);
    }
}
