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
/// R8-B3 — Política de conflito de SALA (recurso compartilhado) nos agendamentos clínicos.
/// Achado (matriz r8a0 linha 118 / varredura vínculos-unidades A8): tabelas prontas
/// (agendamentos.sala_id) mas regra de conflito existia SOMENTE por profissional — dois
/// agendamentos simultâneos na mesma sala eram possíveis.
/// Regras fixadas nesta fatia:
/// 1) Dois agendamentos ATIVOS não podem se sobrepor quando declaram a MESMA sala
///    (intervalo [inicio, fim) — fim exclusivo; adjacência não é conflito).
/// 2) Sem sala declarada (null) não há conflito de sala — nenhuma direção.
/// 3) Predicado de ativo espelha o ramo AGENDAMENTO da política unificada (reg_status='A'
///    e status fora de CANCELADO/FINALIZADO).
/// 4) Revalidação ocorre na criação e no reagendamento, incluindo mudança APENAS de sala
///    (período inalterado) — sala efetiva pós-update = coalesce(request.SalaId, sala atual).
/// Escopo: agendamentos (encontro documentado); outros reservantes de sala ficam como dívida
/// registrada no doc r8b3.
/// </summary>
public sealed class R8B3AgendamentoConflitoSalaTests
{
    private static IConfiguration Cfg() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
        .Build();

    private static Saude360ClinicalService Clinica(Guid tenant, Guid? userId = null) =>
        new(Cfg(), new FakeCurrentUser(tenant, userId), new FakeAuditService(), NullLogger<Saude360ClinicalService>.Instance, new ConflitoHorarioService(Cfg()));

    // ---------- seeds ----------

    private static async Task<Guid> SeedTenantAsync(NpgsqlConnection cn, Guid t, string nome = "R8B3 Clinica") =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.clientes(id,tenant_id,nome,status,reg_status) values(@t,@t,@n,'ATIVO','A') returning id", new { t, n = nome });

    private static async Task<Guid> SeedMedAsync(NpgsqlConnection cn, Guid t, Guid user, string nome, string crm) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.medicos(tenant_id,cliente_id,nome,status,usuario_id,crm,uf_crm) values(@t,@t,@n,'ATIVO',@u,@c,'SP') returning id", new { t, n = nome, u = user, c = crm });

    private static async Task<Guid> SeedPacAsync(NpgsqlConnection cn, Guid t, string nome) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.pacientes(tenant_id,cliente_id,nome,nome_completo) values(@t,@t,@n,@n) returning id", new { t, n = nome });

    private static async Task<Guid> SeedSalaAsync(NpgsqlConnection cn, Guid t, string nome) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.clinica_salas_atendimento(cliente_id,tenant_id,nome,status) values(@t,@t,@n,'ATIVO') returning id", new { t, n = nome });

    private static async Task<Guid> SeedAgendAsync(NpgsqlConnection cn, Guid t, Guid pac, Guid m, DateTime i, DateTime f, Guid? sala = null, string status = "AGENDADO") =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.agendamentos(tenant_id,cliente_id,paciente_id,medico_id,sala_id,data_inicio,data_fim,tipo,status) values(@t,@t,@p,@m,@s,@i,@f,'CONSULTA',@st) returning id", new { t, p = pac, m, s = sala, i, f, st = status });

    private static async Task<(DateTime Di, DateTime Df, Guid? Sala)> LerAgendAsync(NpgsqlConnection cn, Guid id) =>
        await cn.QueryFirstAsync<(DateTime, DateTime, Guid?)>("select data_inicio, data_fim, sala_id from plantaopro.agendamentos where id=@id", new { id });

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
delete from plantaopro.agendamentos where tenant_id=@t or cliente_id=@t;
delete from plantaopro.clinica_salas_atendimento where tenant_id=@t or cliente_id=@t;
delete from plantaopro.pacientes where tenant_id=@t or cliente_id=@t;
delete from plantaopro.medicos where tenant_id=@t or cliente_id=@t;
delete from plantaopro.clientes where tenant_id=@t or id=@t;", new { t });
        if (userIds.Length > 0)
            await cn.ExecuteAsync("delete from plantaopro.notificacoes where usuario_id=any(@uids)", new { uids = userIds });
    }

    // ---------- 1) criação: dimensão de sala independente do profissional ----------

    [Fact]
    public async Task CriarAgendamento_MesmaSalaSobreposta_MedicosDiferentes_409MensagemDeSala()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        var baseH = DateTime.UtcNow.Date.AddDays(4).AddHours(10);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var m1 = await SeedMedAsync(cn, t, u, "R8B3 Med Um", "R8B3M01");
        var m2 = await SeedMedAsync(cn, t, u, "R8B3 Med Dois", "R8B3M02");
        var pac = await SeedPacAsync(cn, t, "R8B3 Pac Sala");
        var sala = await SeedSalaAsync(cn, t, "R8B3 Sala 1");
        await SeedAgendAsync(cn, t, pac, m1, baseH, baseH.AddHours(1), sala);
        try
        {
            var r = await Clinica(t, u).CriarAsync("agendamentos", new Saude360CreateRequest { PacienteId = pac, MedicoId = m2, SalaId = sala, DataInicio = baseH.AddMinutes(30), DataFim = baseH.AddMinutes(90) });
            Assert.False(r.Success);
            Assert.Equal(409, r.StatusCode);
            Assert.Contains("sala", r.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task CriarAgendamento_MesmaSala_AdacenteSemSobreposicao_Ok()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        var baseH = DateTime.UtcNow.Date.AddDays(4).AddHours(10);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var m1 = await SeedMedAsync(cn, t, u, "R8B3 Med Adj1", "R8B3M03");
        var m2 = await SeedMedAsync(cn, t, u, "R8B3 Med Adj2", "R8B3M04");
        var pac = await SeedPacAsync(cn, t, "R8B3 Pac Adj");
        var sala = await SeedSalaAsync(cn, t, "R8B3 Sala Adj");
        await SeedAgendAsync(cn, t, pac, m1, baseH, baseH.AddHours(1), sala);
        try
        {
            // fim do primeiro == início do segundo → NÃO é conflito (fim exclusivo).
            var r = await Clinica(t, u).CriarAsync("agendamentos", new Saude360CreateRequest { PacienteId = pac, MedicoId = m2, SalaId = sala, DataInicio = baseH.AddHours(1), DataFim = baseH.AddHours(2) });
            Assert.True(r.Success, r.Message ?? "");
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task CriarAgendamento_SalasDiferentes_MesmoHorario_Ok()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        var baseH = DateTime.UtcNow.Date.AddDays(4).AddHours(10);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var m1 = await SeedMedAsync(cn, t, u, "R8B3 Med Dif1", "R8B3M05");
        var m2 = await SeedMedAsync(cn, t, u, "R8B3 Med Dif2", "R8B3M06");
        var pac = await SeedPacAsync(cn, t, "R8B3 Pac Dif");
        var s1 = await SeedSalaAsync(cn, t, "R8B3 Sala A");
        var s2 = await SeedSalaAsync(cn, t, "R8B3 Sala B");
        await SeedAgendAsync(cn, t, pac, m1, baseH, baseH.AddHours(1), s1);
        try
        {
            var r = await Clinica(t, u).CriarAsync("agendamentos", new Saude360CreateRequest { PacienteId = pac, MedicoId = m2, SalaId = s2, DataInicio = baseH, DataFim = baseH.AddHours(1) });
            Assert.True(r.Success, r.Message ?? "");
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task CriarAgendamento_SemSalaDeclarada_NaoConflitaComSalaDeclarada_Ok()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        var baseH = DateTime.UtcNow.Date.AddDays(4).AddHours(10);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var m1 = await SeedMedAsync(cn, t, u, "R8B3 Med NS1", "R8B3M07");
        var m2 = await SeedMedAsync(cn, t, u, "R8B3 Med NS2", "R8B3M08");
        var pac = await SeedPacAsync(cn, t, "R8B3 Pac NS");
        var sala = await SeedSalaAsync(cn, t, "R8B3 Sala NS");
        await SeedAgendAsync(cn, t, pac, m1, baseH, baseH.AddHours(1), sala);
        try
        {
            var r = await Clinica(t, u).CriarAsync("agendamentos", new Saude360CreateRequest { PacienteId = pac, MedicoId = m2, DataInicio = baseH, DataFim = baseH.AddHours(1) });
            Assert.True(r.Success, r.Message ?? "");
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task CriarAgendamento_SalaDeclarada_NaoConflitaComAgendamentoSemSala_Ok()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        var baseH = DateTime.UtcNow.Date.AddDays(4).AddHours(10);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var m1 = await SeedMedAsync(cn, t, u, "R8B3 Med SN1", "R8B3M09");
        var m2 = await SeedMedAsync(cn, t, u, "R8B3 Med SN2", "R8B3M10");
        var pac = await SeedPacAsync(cn, t, "R8B3 Pac SN");
        var sala = await SeedSalaAsync(cn, t, "R8B3 Sala SN");
        await SeedAgendAsync(cn, t, pac, m1, baseH, baseH.AddHours(1));
        try
        {
            var r = await Clinica(t, u).CriarAsync("agendamentos", new Saude360CreateRequest { PacienteId = pac, MedicoId = m2, SalaId = sala, DataInicio = baseH, DataFim = baseH.AddHours(1) });
            Assert.True(r.Success, r.Message ?? "");
        }
        finally { await LimparAsync(t, u); }
    }

    // ---------- 2) reagendamento ----------

    [Fact]
    public async Task Reagendar_MoverApenasSalaParaOcupadaNoMesmoPeriodo_409RegistroPreservado()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        var baseH = DateTime.UtcNow.Date.AddDays(4).AddHours(10);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var m1 = await SeedMedAsync(cn, t, u, "R8B3 Med RA1", "R8B3M11");
        var m2 = await SeedMedAsync(cn, t, u, "R8B3 Med RA2", "R8B3M12");
        var pac = await SeedPacAsync(cn, t, "R8B3 Pac RA");
        var sala = await SeedSalaAsync(cn, t, "R8B3 Sala RA");
        await SeedAgendAsync(cn, t, pac, m2, baseH, baseH.AddHours(1), sala);
        var criado = await Clinica(t, u).CriarAsync("agendamentos", new Saude360CreateRequest { PacienteId = pac, MedicoId = m1, DataInicio = baseH, DataFim = baseH.AddHours(1) });
        Assert.True(criado.Success, criado.Message ?? "");
        try
        {
            // mudança APENAS de sala (período/profissional inalterados) exige revalidação de sala.
            var r = await Clinica(t, u).AtualizarAsync("agendamentos", criado.Data.Id, new Saude360CreateRequest { SalaId = sala });
            Assert.False(r.Success);
            Assert.Equal(409, r.StatusCode);
            Assert.Contains("sala", r.Message, StringComparison.OrdinalIgnoreCase);
            var atual = await LerAgendAsync(cn, criado.Data.Id);
            Assert.Null(atual.Sala);
            Assert.Equal(baseH, atual.Di);
            Assert.Equal(baseH.AddHours(1), atual.Df);
        }
        finally { await LimparAsync(t, u); }
    }

    [Fact]
    public async Task Reagendar_MoverPeriodoNaMesmaSalaParaHorarioOcupado_409MensagemDeSala()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        var baseH = DateTime.UtcNow.Date.AddDays(4).AddHours(10);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var m1 = await SeedMedAsync(cn, t, u, "R8B3 Med RP1", "R8B3M13");
        var m2 = await SeedMedAsync(cn, t, u, "R8B3 Med RP2", "R8B3M14");
        var pac = await SeedPacAsync(cn, t, "R8B3 Pac RP");
        var sala = await SeedSalaAsync(cn, t, "R8B3 Sala RP");
        await SeedAgendAsync(cn, t, pac, m2, baseH, baseH.AddHours(1), sala);
        var criado = await Clinica(t, u).CriarAsync("agendamentos", new Saude360CreateRequest { PacienteId = pac, MedicoId = m1, SalaId = sala, DataInicio = baseH.AddHours(2), DataFim = baseH.AddHours(3) });
        Assert.True(criado.Success, criado.Message ?? "");
        try
        {
            // mesmo profissional livre no novo período, mas a própria sala já está ocupada.
            var r = await Clinica(t, u).AtualizarAsync("agendamentos", criado.Data.Id, new Saude360CreateRequest { DataInicio = baseH.AddMinutes(30), DataFim = baseH.AddMinutes(90) });
            Assert.False(r.Success);
            Assert.Equal(409, r.StatusCode);
            Assert.Contains("sala", r.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally { await LimparAsync(t, u); }
    }

    // ---------- 3) predicato de ativo espelha a política unificada ----------

    [Fact]
    public async Task CriarAgendamento_OcupanteDaSalaCancelado_NaoBloqueia_Ok()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        var baseH = DateTime.UtcNow.Date.AddDays(4).AddHours(10);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await SeedTenantAsync(cn, t);
        var m1 = await SeedMedAsync(cn, t, u, "R8B3 Med CA1", "R8B3M15");
        var m2 = await SeedMedAsync(cn, t, u, "R8B3 Med CA2", "R8B3M16");
        var pac = await SeedPacAsync(cn, t, "R8B3 Pac CA");
        var sala = await SeedSalaAsync(cn, t, "R8B3 Sala CA");
        await SeedAgendAsync(cn, t, pac, m1, baseH, baseH.AddHours(1), sala, status: "CANCELADO");
        try
        {
            var r = await Clinica(t, u).CriarAsync("agendamentos", new Saude360CreateRequest { PacienteId = pac, MedicoId = m2, SalaId = sala, DataInicio = baseH, DataFim = baseH.AddHours(1) });
            Assert.True(r.Success, r.Message ?? "");
        }
        finally { await LimparAsync(t, u); }
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
