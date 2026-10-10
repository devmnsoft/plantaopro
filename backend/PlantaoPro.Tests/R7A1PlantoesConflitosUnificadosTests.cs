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
/// R7-A1 — Unificação de conflitos do profissional e provas de concorrência das jornadas de plantões/clínica.
/// 1) Política única de conflito (escala confirmada/solicitada + agendamento clínico ativo + indisponibilidade)
///    em ConflitoHorarioService, usada pelos dois lados: aceite/confirmacao de escala (EscalaService) e
///    criação/reagendamento de agendamento (Saude360ClinicalService). Intervalo [inicio, fim) — fim exclusivo.
/// 2) Prova de concorrência da última vaga (dois aceites simultâneos → exatamente uma confirmação).
/// 3) Idempotência/concorrência de check-in e reserva concorrente segura de item de fila.
/// 4) Migration v2341: medicos.bloqueado + medico_especialidades existiam só no código (500 latente no aceite).
/// </summary>
public sealed class R7A1PlantoesConflitosUnificadosTests
{
    private static string Root => RepositoryPathResolver.ResolveRoot();
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    private static IConfiguration Cfg() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
        .Build();

    private static Saude360ClinicalService Clinica(Guid tenant, Guid? userId = null) =>
        new(Cfg(), new FakeCurrentUser(tenant, userId), new FakeAuditService(), NullLogger<Saude360ClinicalService>.Instance, new ConflitoHorarioService(Cfg()));

    private static EscalaService Escala() => new(
        Cfg(),
        new FakeAuditService(),
        new NotificacaoService(Cfg(), new FakeAuditService(), NullLogger<NotificacaoService>.Instance),
        NullLogger<EscalaService>.Instance,
        new MedicoElegibilidadeService(Cfg(), new ConflitoHorarioService(Cfg())),
        new ConflitoHorarioService(Cfg()));

    // ---------- seeds ----------

    private static async Task<Guid> SeedTenantAsync(NpgsqlConnection cn, Guid t, string nome = "R7A1 Clinica") =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.clientes(id,tenant_id,nome,status,reg_status) values(@t,@t,@n,'ATIVO','A') returning id", new { t, n = nome });

    private static async Task<Guid> SeedHospAsync(NpgsqlConnection cn, Guid t) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.hospitais(tenant_id,cliente_id,nome,nome_fantasia,status) values(@t,@t,'R7A1 Hosp','R7A1 Hosp','ATIVO') returning id", new { t });

    private static async Task<Guid> SeedEspAsync(NpgsqlConnection cn, Guid t) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.especialidades(tenant_id,cliente_id,nome,status) values(@t,@t,'R7A1 Especialidade','ATIVO') returning id", new { t });

    private static async Task<Guid> SeedMedAsync(NpgsqlConnection cn, Guid t, Guid user, string nome, string crm, bool bloqueado = false) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.medicos(tenant_id,cliente_id,nome,status,usuario_id,crm,uf_crm,bloqueado) values(@t,@t,@n,'ATIVO',@u,@c,'SP',@b) returning id", new { t, n = nome, u = user, c = crm, b = bloqueado });

    private static async Task SeedMedEspAsync(NpgsqlConnection cn, Guid medico, Guid esp) =>
        await cn.ExecuteAsync("insert into plantaopro.medico_especialidades(medico_id,especialidade_id) values(@m,@e)", new { m = medico, e = esp });

    private static async Task<Guid> SeedPltAsync(NpgsqlConnection cn, Guid t, Guid h, Guid e, DateTime i, DateTime f, int vagas, int disp, string status) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.plantoes(tenant_id,cliente_id,hospital_id,especialidade_id,data_inicio,data_fim,valor,vagas,vagas_disponiveis,status,reg_status) values(@t,@t,@h,@e,@i,@f,100,@vag,@disp,@s,'A') returning id", new { t, h, e, i, f, vag = vagas, disp, s = status });

    private static async Task<Guid> SeedEscAsync(NpgsqlConnection cn, Guid t, Guid p, Guid m, string status) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.escalas(tenant_id,cliente_id,plantao_id,medico_id,status) values(@t,@t,@p,@m,@s) returning id", new { t, p, m, s = status });

    private static async Task<Guid> SeedConviteAsync(NpgsqlConnection cn, Guid p, Guid m, DateTime expira) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.plantao_convites(plantao_id,medico_id,status,expira_em,reg_status) values(@p,@m,'ENVIADO',@x,'A') returning id", new { p, m, x = expira });

    private static async Task<Guid> SeedPacAsync(NpgsqlConnection cn, Guid t, string nome) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.pacientes(tenant_id,cliente_id,nome,nome_completo) values(@t,@t,@n,@n) returning id", new { t, n = nome });

    private static async Task<Guid> SeedAgendAsync(NpgsqlConnection cn, Guid t, Guid pac, Guid m, DateTime i, DateTime f, string status = "AGENDADO") =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.agendamentos(tenant_id,cliente_id,paciente_id,medico_id,data_inicio,data_fim,tipo,status) values(@t,@t,@p,@m,@i,@f,'CONSULTA',@s) returning id", new { t, p = pac, m, i, f, s = status });

    private static async Task SeedIndispAsync(NpgsqlConnection cn, Guid t, Guid m, DateTime i, DateTime f) =>
        await cn.ExecuteAsync("insert into plantaopro.medico_indisponibilidades(cliente_id,medico_id,data_inicio,data_fim,motivo,status,created_by,reg_status) values(@t,@m,@i,@f,'Teste R7A1','ATIVA',gen_random_uuid(),'A')", new { t, m, i, f });

    private static async Task<Guid> SeedFilaAsync(NpgsqlConnection cn, Guid t, Guid pac, Guid agend) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.painel_chamada_fila(cliente_id,paciente_id,agendamento_id,senha,paciente_nome,status) values(@t,@p,@a,'P0001','R7A1 Paciente','AGUARDANDO') returning id", new { t, p = pac, a = agend });

    private static async Task LimparAsync(Guid t, params Guid[] userIds)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync(@"
delete from plantaopro.agendamento_checkins where cliente_id=@t or tenant_id=@t;
delete from plantaopro.painel_chamada_historico where cliente_id=@t or tenant_id=@t;
delete from plantaopro.painel_chamada_fila where cliente_id=@t or tenant_id=@t;
delete from plantaopro.triagem_fila where cliente_id=@t or tenant_id=@t;
delete from plantaopro.agendamento_historico where cliente_id=@t or tenant_id=@t;
delete from plantaopro.medico_indisponibilidades where cliente_id=@t or tenant_id=@t;
delete from plantaopro.medico_disponibilidades where cliente_id=@t or tenant_id=@t;
delete from plantaopro.agendamentos where tenant_id=@t or cliente_id=@t;
delete from plantaopro.pacientes where tenant_id=@t or cliente_id=@t;
delete from plantaopro.plantao_convites where plantao_id in (select id from plantaopro.plantoes where tenant_id=@t or cliente_id=@t);
delete from plantaopro.historico_escala where escala_id in (select id from plantaopro.escalas where tenant_id=@t or cliente_id=@t);
delete from plantaopro.escalas where tenant_id=@t or cliente_id=@t;
delete from plantaopro.plantoes where tenant_id=@t or cliente_id=@t;
delete from plantaopro.medico_especialidades where medico_id in (select id from plantaopro.medicos where tenant_id=@t or cliente_id=@t);
delete from plantaopro.medicos where tenant_id=@t or cliente_id=@t;
delete from plantaopro.hospitais where tenant_id=@t or cliente_id=@t;
delete from plantaopro.especialidades where tenant_id=@t or cliente_id=@t;
delete from plantaopro.clientes where tenant_id=@t or id=@t;", new { t });
        if (userIds.Length > 0)
            await cn.ExecuteAsync("delete from plantaopro.notificacoes where usuario_id=any(@uids)", new { uids = userIds });
    }

    // ---------- 1) política unificada: agenda clínica x escalas ----------

    [Fact]
    public async Task CriarAgendamento_SobrepostoAEscalaConfirmada_409ComMensagemDeEscala()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        var inicio = DateTime.UtcNow.Date.AddDays(3).AddHours(8);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var h = await SeedHospAsync(cn, t); var e = await SeedEspAsync(cn, t);
        var m = await SeedMedAsync(cn, t, u, "R7A1 Med Escala", "R7A1M01");
        await SeedMedEspAsync(cn, m, e);
        var p = await SeedPltAsync(cn, t, h, e, inicio, inicio.AddHours(4), 1, 1, "aberto");
        await SeedEscAsync(cn, t, p, m, "confirmado");
        var pac = await SeedPacAsync(cn, t, "R7A1 Pac Escala");
        try
        {
            var r = await Clinica(t).CriarAsync("agendamentos", new Saude360CreateRequest { PacienteId = pac, MedicoId = m, DataInicio = inicio.AddMinutes(30), DataFim = inicio.AddHours(2) });
            Assert.False(r.Success);
            Assert.Equal(409, r.StatusCode);
            Assert.Contains("escala", r.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task CriarAgendamento_SobrepostoAAgendamentoExistente_409PreservaAncoraDeMensagem()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        var inicio = DateTime.UtcNow.Date.AddDays(3).AddHours(8);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        await SeedHospAsync(cn, t);
        var e = await SeedEspAsync(cn, t);
        var m = await SeedMedAsync(cn, t, u, "R7A1 Med Agenda", "R7A1M02");
        await SeedMedEspAsync(cn, m, e);
        var pac1 = await SeedPacAsync(cn, t, "R7A1 Pac Um");
        var pac2 = await SeedPacAsync(cn, t, "R7A1 Pac Dois");
        await SeedAgendAsync(cn, t, pac1, m, inicio, inicio.AddHours(2));
        try
        {
            var r = await Clinica(t).CriarAsync("agendamentos", new Saude360CreateRequest { PacienteId = pac2, MedicoId = m, DataInicio = inicio.AddMinutes(30), DataFim = inicio.AddHours(1) });
            Assert.False(r.Success);
            Assert.Equal(409, r.StatusCode);
            Assert.Contains("Já existe agendamento para o médico", r.Message);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task CriarAgendamento_SobrepostoAIndisponibilidadeAtiva_409()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        var inicio = DateTime.UtcNow.Date.AddDays(3).AddHours(8);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        await SeedHospAsync(cn, t);
        var e = await SeedEspAsync(cn, t);
        var m = await SeedMedAsync(cn, t, u, "R7A1 Med Indisp", "R7A1M03");
        await SeedMedEspAsync(cn, m, e);
        await SeedIndispAsync(cn, t, m, inicio, inicio.AddHours(3));
        var pac = await SeedPacAsync(cn, t, "R7A1 Pac Indisp");
        try
        {
            var r = await Clinica(t).CriarAsync("agendamentos", new Saude360CreateRequest { PacienteId = pac, MedicoId = m, DataInicio = inicio.AddMinutes(30), DataFim = inicio.AddHours(1) });
            Assert.False(r.Success);
            Assert.Equal(409, r.StatusCode);
            Assert.Contains("indisponibilidade", r.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task CriarAgendamento_MedicoBloqueado_409()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        var inicio = DateTime.UtcNow.Date.AddDays(3).AddHours(8);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        await SeedHospAsync(cn, t);
        var e = await SeedEspAsync(cn, t);
        var m = await SeedMedAsync(cn, t, u, "R7A1 Med Bloq", "R7A1M04", bloqueado: true);
        await SeedMedEspAsync(cn, m, e);
        var pac = await SeedPacAsync(cn, t, "R7A1 Pac Bloq");
        try
        {
            var r = await Clinica(t).CriarAsync("agendamentos", new Saude360CreateRequest { PacienteId = pac, MedicoId = m, DataInicio = inicio, DataFim = inicio.AddHours(1) });
            Assert.False(r.Success);
            Assert.Equal(409, r.StatusCode);
            Assert.Contains("bloqueado", r.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task CriarAgendamento_SemConflito_PersisteComStatusAgendado()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        var inicio = DateTime.UtcNow.Date.AddDays(3).AddHours(8);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        await SeedHospAsync(cn, t);
        var e = await SeedEspAsync(cn, t);
        var m = await SeedMedAsync(cn, t, u, "R7A1 Med Livre", "R7A1M05");
        await SeedMedEspAsync(cn, m, e);
        var pac = await SeedPacAsync(cn, t, "R7A1 Pac Livre");
        try
        {
            var r = await Clinica(t).CriarAsync("agendamentos", new Saude360CreateRequest { PacienteId = pac, MedicoId = m, DataInicio = inicio, DataFim = inicio.AddHours(1) });
            Assert.True(r.Success, r.Message);
            var total = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.agendamentos where medico_id=@m and coalesce(tenant_id,cliente_id)=@t", new { m, t });
            Assert.Equal(1, total);
            var dbStatus = await cn.ExecuteScalarAsync<string>("select status from plantaopro.agendamentos where medico_id=@m and coalesce(tenant_id,cliente_id)=@t", new { m, t });
            Assert.Equal("AGENDADO", dbStatus);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task AtualizarAgendamento_MoverParaSobreEscalaConfirmada_409EPreservaHorarioOriginal()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        var dia = DateTime.UtcNow.Date.AddDays(3).AddHours(8);
        var alvo = DateTime.UtcNow.Date.AddDays(4).AddHours(8);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var h = await SeedHospAsync(cn, t);
        var e = await SeedEspAsync(cn, t);
        var m = await SeedMedAsync(cn, t, u, "R7A1 Med Reag", "R7A1M06");
        await SeedMedEspAsync(cn, m, e);
        var p = await SeedPltAsync(cn, t, h, e, alvo, alvo.AddHours(4), 1, 1, "aberto");
        await SeedEscAsync(cn, t, p, m, "confirmado");
        var pac = await SeedPacAsync(cn, t, "R7A1 Pac Reag");
        var agend = await SeedAgendAsync(cn, t, pac, m, dia, dia.AddHours(1));
        try
        {
            var r = await Clinica(t).AtualizarAsync("agendamentos", agend, new Saude360CreateRequest { DataInicio = alvo.AddMinutes(30), DataFim = alvo.AddHours(2) });
            Assert.False(r.Success);
            Assert.Equal(409, r.StatusCode);
            var original = await cn.QueryFirstAsync<(DateTime Di, DateTime Df)>("select data_inicio as Di, data_fim as Df from plantaopro.agendamentos where id=@id", new { id = agend });
            Assert.Equal(dia, original.Di);
            Assert.Equal(dia.AddHours(1), original.Df);
        }
        finally { await LimparAsync(t, u); }
    }

    // ---------- 2) aceite de escala: prova de concorrência + unificação reversa ----------

    [Fact]
    public async Task AceitarConvite_UltimaVaga_DoisConcorrentes_SomenteUmaConfirmacao()
    {
        var t = Guid.NewGuid();
        var u1 = Guid.NewGuid(); var u2 = Guid.NewGuid();
        var inicio = DateTime.UtcNow.Date.AddDays(5).AddHours(8);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var h = await SeedHospAsync(cn, t);
        var e = await SeedEspAsync(cn, t);
        var m1 = await SeedMedAsync(cn, t, u1, "R7A1 Contendor A", "R7A1M07");
        var m2 = await SeedMedAsync(cn, t, u2, "R7A1 Contendor B", "R7A1M08");
        await SeedMedEspAsync(cn, m1, e);
        await SeedMedEspAsync(cn, m2, e);
        var p = await SeedPltAsync(cn, t, h, e, inicio, inicio.AddHours(4), 1, 1, "aberto");
        var c1 = await SeedConviteAsync(cn, p, m1, inicio.Date.AddDays(30));
        var c2 = await SeedConviteAsync(cn, p, m2, inicio.Date.AddDays(30));
        try
        {
            var svc = Escala();
            var resultadoCorrida = await Task.WhenAll(
                svc.AceitarConviteAsync(c1, m1, t, u1, null, null),
                svc.AceitarConviteAsync(c2, m2, t, u2, null, null));
            var (r1, r2) = (resultadoCorrida[0], resultadoCorrida[1]);
            Assert.True(r1.Success ^ r2.Success, $"Esperado exatamente um aceite vencido; r1={(r1.Success ? r1.Message : r1.Message + "/" + r1.StatusCode)}; r2={r2.Message}/{r2.StatusCode}");
            var perdedor = r1.Success ? r2 : r1;
            Assert.False(perdedor.Success);
            Assert.Equal(409, perdedor.StatusCode);

            var estado = await cn.QueryFirstAsync<(int Disp, int Confirmadas)>(
                "select coalesce(vagas_disponiveis,0) as Disp, (select count(1) from plantaopro.escalas es where es.plantao_id=@p and lower(es.status) in ('confirmado','confirmada') and es.reg_status='A') as Confirmadas from plantaopro.plantoes where id=@p", new { p });
            Assert.Equal(0, estado.Disp);
            Assert.Equal(1, estado.Confirmadas);
        }
        finally { await LimparAsync(t, u1, u2); }
    }

    [Fact]
    public async Task AceitarConvite_MedicoComAgendamentoClinicoSobreposto_409PelaPoliticaUnificada()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        var inicio = DateTime.UtcNow.Date.AddDays(5).AddHours(8);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var h = await SeedHospAsync(cn, t);
        var e = await SeedEspAsync(cn, t);
        var m = await SeedMedAsync(cn, t, u, "R7A1 Med Cruzado", "R7A1M09");
        await SeedMedEspAsync(cn, m, e);
        var p = await SeedPltAsync(cn, t, h, e, inicio, inicio.AddHours(4), 1, 1, "aberto");
        var pac = await SeedPacAsync(cn, t, "R7A1 Pac Cruzado");
        await SeedAgendAsync(cn, t, pac, m, inicio.AddMinutes(30), inicio.AddHours(2));
        var c = await SeedConviteAsync(cn, p, m, inicio.Date.AddDays(30));
        try
        {
            var r = await Escala().AceitarConviteAsync(c, m, t, u, null, null);
            Assert.False(r.Success);
            Assert.Equal(409, r.StatusCode);
            Assert.Contains("compromisso", r.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally { await LimparAsync(t, u); }
    }

    // ---------- 3) check-in: idempotência sequencial e concorrência ----------

    [Fact]
    public async Task Checkin_Repetido_SegundaTentativaRetorna409EMantemUmRegistro()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        var inicio = DateTime.UtcNow.Date.AddDays(3).AddHours(9);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var pac = await SeedPacAsync(cn, t, "R7A1 Pac Checkin");
        var m = await SeedMedAsync(cn, t, u, "R7A1 Med Checkin", "R7A1M10");
        var agend = await SeedAgendAsync(cn, t, pac, m, inicio, inicio.AddHours(1), status: "CONFIRMADO");
        try
        {
            var svc = Clinica(t, u);
            var r1 = await svc.AcaoAsync("agendamentos", agend, "checkin", new Saude360ActionRequest());
            Assert.True(r1.Success, r1.Message);
            var r2 = await svc.AcaoAsync("agendamentos", agend, "checkin", new Saude360ActionRequest());
            Assert.False(r2.Success);
            Assert.Equal(409, r2.StatusCode);

            var checkins = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.agendamento_checkins where agendamento_id=@id and reg_status='A'", new { id = agend });
            var fila = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.painel_chamada_fila where agendamento_id=@id and reg_status='A'", new { id = agend });
            Assert.Equal(1, checkins);
            Assert.InRange(fila, 0, 1);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Checkin_Concorrente_DoisOperadores_SomenteUmEfetivadoESemDuplicarFila()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        var inicio = DateTime.UtcNow.Date.AddDays(3).AddHours(9);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var pac = await SeedPacAsync(cn, t, "R7A1 Pac CheckinCorr");
        var m = await SeedMedAsync(cn, t, u, "R7A1 Med CheckinCorr", "R7A1M11");
        var agend = await SeedAgendAsync(cn, t, pac, m, inicio, inicio.AddHours(1), status: "CONFIRMADO");
        try
        {
            var resultadoCorrida = await Task.WhenAll(
                Clinica(t, u).AcaoAsync("agendamentos", agend, "checkin", new Saude360ActionRequest()),
                Clinica(t, u).AcaoAsync("agendamentos", agend, "checkin", new Saude360ActionRequest()));
            var (r1, r2) = (resultadoCorrida[0], resultadoCorrida[1]);
            Assert.True(r1.Success ^ r2.Success, $"Esperado exatamente um check-in efetivado; r1={r1.Message}; r2={r2.Message}/{r2.StatusCode}");
            var checkins = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.agendamento_checkins where agendamento_id=@id and reg_status='A'", new { id = agend });
            var fila = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.painel_chamada_fila where agendamento_id=@id and reg_status='A'", new { id = agend });
            Assert.Equal(1, checkins);
            Assert.InRange(fila, 0, 1);
        }
        finally { await LimparAsync(t, u); }
    }

    // ---------- 4) fila de chamada: reserva concorrente segura ----------

    [Fact]
    public async Task Chamar_FilaConcorrente_DoisOperadores_SomenteUmaReservaESemHistoricoDuplicado()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        var inicio = DateTime.UtcNow.Date.AddDays(3).AddHours(9);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var pac = await SeedPacAsync(cn, t, "R7A1 Pac Fila");
        var m = await SeedMedAsync(cn, t, u, "R7A1 Med Fila", "R7A1M12");
        var agend = await SeedAgendAsync(cn, t, pac, m, inicio, inicio.AddHours(1), status: "CONFIRMADO");
        var fila = await SeedFilaAsync(cn, t, pac, agend);
        try
        {
            var resultadoCorrida = await Task.WhenAll(
                Clinica(t, u).AcaoAsync("painel", fila, "chamar", new Saude360ActionRequest()),
                Clinica(t, u).AcaoAsync("painel", fila, "chamar", new Saude360ActionRequest()));
            var (r1, r2) = (resultadoCorrida[0], resultadoCorrida[1]);
            Assert.True(r1.Success ^ r2.Success, $"Esperado exatamente uma chamada efetivada; r1={r1.Message}; r2={r2.Message}/{r2.StatusCode}");
            var status = await cn.ExecuteScalarAsync<string>("select status from plantaopro.painel_chamada_fila where id=@id", new { id = fila });
            Assert.Equal("CHAMADO", status);
            var historico = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.painel_chamada_historico where fila_id=@id and upper(coalesce(acao,'')) like 'CHAMAR%'", new { id = fila });
            Assert.Equal(1, historico);
        }
        finally { await LimparAsync(t, u); }
    }

    // ---------- 5) contratos de fonte (regra única, sem implementação paralela) ----------

    [Fact]
    public void MigracaoV2341_MaterializaBloqueadoEVinculoEspecialidadeReferenciadosPeloCodigo()
    {
        var sql = Read("database/migrations/2026_10_v2341_r7a1_conflitos_unificados_profissional.sql");
        Assert.Contains("add column if not exists bloqueado boolean not null default false", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("create table if not exists plantaopro.medico_especialidades", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ux_medico_especialidade_ativa", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MigracaoV2342_MaterializaColunasDoHistoricoDeEscalaUsadasPorCodigoEPortal()
    {
        var sql = Read("database/migrations/2026_10_v2342_r7a1_historico_escala_colunas.sql");
        foreach (var col in new[] { "escala_id", "status_anterior", "status_novo", "justificativa", "usuario_id", "reg_date" })
            Assert.Contains($"add column if not exists {col}", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ix_historico_escala_escala", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MigracaoV2343_MaterializaColunasDoCicloDeVidaDoConviteOperacional()
    {
        var sql = Read("database/migrations/2026_10_v2343_r7a1_convite_ciclo_vida.sql");
        foreach (var col in new[] { "data_resposta", "motivo_recusa", "reg_update", "reg_date" })
            Assert.Contains($"add column if not exists {col}", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConflitoCanônico_UnificaEscalasAgendaEIndisponibilidadeSemImplementacaoParalela()
    {
        var src = Read("backend/PlantaoPro.Api/BusinessRulesServices.cs");
        Assert.Contains("union all", src, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("plantaopro.agendamentos", src);
        Assert.Contains("plantaopro.medico_indisponibilidades", src);

        var serv = Read("backend/PlantaoPro.Api/Saude360ClinicalService.cs");
        Assert.Contains("ListarFontesConflitoAsync", serv);
        Assert.DoesNotContain("plantaopro.escalas", serv);

        var data = Read("backend/PlantaoPro.Api/Data.cs");
        Assert.Contains("conflitoService.ExisteConflitoAsync(medicoId, plantao.Inicio", data);
    }

    // ---------- fakes ----------

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        private readonly Guid _userId;
        private readonly Guid? _tenantId;
        public FakeCurrentUser(Guid? tenantId = null, Guid? userId = null)
        {
            _tenantId = tenantId;
            _userId = userId ?? Guid.Empty;
        }
        public bool GlobalAdmin => false;
        public Guid? UserId => _userId == Guid.Empty ? null : _userId;
        public Guid? TenantId => _tenantId;
        public Guid? ClienteId => _tenantId;
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
}
