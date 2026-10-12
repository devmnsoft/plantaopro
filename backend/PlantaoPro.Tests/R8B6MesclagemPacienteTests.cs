using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Data;
using PlantaoPro.Tests.Infrastructure;
using System.Reflection;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B6 — Mesclagem controlada de pacientes (matriz §23: operação INTEIRA ausente — busca
/// merge/mescla/merged_into repo-wide → 0 matches antes desta fatia).
/// 1) Mescla move todos os vínculos das tabelas com paciente_id (39, conferidas em information_schema),
///    inativa a origem (status E reg_status — diferentemente da inativação comum, o CPF sai do índice
///    único) e deixa trilha MESCLAR no histórico dos dois + colunas mesclado_em/em/por (v2349).
/// 2) Gates: motivo obrigatório, destino distinto da origem, sem cross-tenant, origem não re-mesclada,
///    destino já mesclado não recebe nova mesclagem.
/// 3) Plano de saúde: principal da origem é despromovido — o principal do destino prevalece
///    (índice único parcial por paciente).
/// 4) Concorrência: duas mesclas simultâneas da mesma origem → exatamente uma ganha (FOR UPDATE ordenado).
/// 5) Contrato: migration v2349, array do serviço (reflexão) e information_schema viva coesos.
/// </summary>
public sealed class R8B6MesclagemPacienteTests
{
    private static string Root => RepositoryPathResolver.ResolveRoot();
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    // Espelho de Saude360ClinicalService.TabelasComPacienteId — o fato de contrato reconcilia esta
    // lista com o array do serviço (reflexão) e com o information_schema real do banco da suíte.
    private static readonly string[] Tabelas =
    {
        "agendamento_checkins", "agendamentos", "anexos_clinicos", "atendimentos_fila",
        "cid_uso_historico", "clinica_contas_receber", "clinica_recebimentos",
        "consulta_anamnese", "consulta_condutas", "consulta_diagnosticos", "consulta_encaminhamentos",
        "consulta_exame_fisico", "consulta_historico", "consultas", "convenio_autorizacoes",
        "documentos_clinicos", "encaminhamentos_clinicos", "paciente_alergias", "paciente_consentimentos",
        "paciente_contatos", "paciente_convenios", "paciente_documentos", "paciente_enderecos",
        "paciente_historico", "paciente_medicamentos_uso", "paciente_observacoes", "paciente_problemas",
        "painel_chamada_fila", "painel_chamada_historico", "plano_saude_autorizacoes",
        "plano_saude_historico", "plano_saude_pacientes", "prescricoes", "resultados_exames",
        "solicitacoes_exames", "triagem_fila", "triagem_historico", "triagem_sinais_vitais", "triagens"
    };

    private static IConfiguration Cfg() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
        .Build();

    private static Saude360ClinicalService Clinica(Guid tenant, Guid? userId = null) =>
        new(Cfg(), new FakeCurrentUser(tenant, userId), new FakeAuditService(), NullLogger<Saude360ClinicalService>.Instance, new ConflitoHorarioService(Cfg()));

    // ---------- schema (v2349) ----------

    /// <summary>Advisory lock serializa a aplicação entre classes paralelas; probe idempotente na coluna.</summary>
    private static async Task GarantirSchemaAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r8b6_mesclagem_pacientes_schema'));");
        try
        {
            var tem = await cn.ExecuteScalarAsync<bool>("select exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='pacientes' and column_name='mesclado_em')");
            if (!tem)
            {
                var sql = Read("database/migrations/2026_10_v2349_r8b6_mesclagem_pacientes.sql");
                for (var tentativa = 1; ; tentativa++)
                {
                    try { await cn.ExecuteAsync(sql); break; }
                    catch (PostgresException ex) when (ex.SqlState == "40P01" && tentativa < 5) { await Task.Delay(150 * tentativa); }
                }
            }
        }
        finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r8b6_mesclagem_pacientes_schema'));"); }
    }

    // ---------- seeds ----------

    private static async Task<Guid> SeedTenantAsync(NpgsqlConnection cn, Guid t) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.clientes(id,tenant_id,nome,status,reg_status) values(@t,@t,'R8B6 Clinica','ATIVO','A') returning id", new { t });

    private static async Task<Guid> SeedPacAsync(NpgsqlConnection cn, Guid t, string nome, string? cpf = null) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.pacientes(tenant_id,cliente_id,nome,nome_completo,cpf,status,reg_status) values(@t,@t,@n,@n,@c,'ATIVO','A') returning id", new { t, n = nome, c = cpf });

    private static async Task<Guid> SeedPlanoAsync(NpgsqlConnection cn, Guid t, Guid pac, Guid plano, bool principal) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.plano_saude_pacientes(id,cliente_id,tenant_id,plano_saude_id,paciente_id,principal,status) values(gen_random_uuid(),@t,@t,@pl,@p,@pr,'ATIVO') returning id", new { t, pl = plano, p = pac, pr = principal });

    private static async Task SeedVinculosAsync(NpgsqlConnection cn, Guid t, Guid pac, Guid uid)
    {
        var i = DateTime.UtcNow.Date.AddDays(2).AddHours(8);
        var f = i.AddHours(1);
        await cn.ExecuteAsync(@"
insert into plantaopro.agendamentos(tenant_id,cliente_id,paciente_id,medico_id,data_inicio,data_fim,tipo,status) values(@t,@t,@p,gen_random_uuid(),@i,@f,'CONSULTA','AGENDADO');
insert into plantaopro.consultas(tenant_id,cliente_id,paciente_id) values(@t,@t,@p);
insert into plantaopro.triagens(tenant_id,cliente_id,paciente_id) values(@t,@t,@p);
insert into plantaopro.paciente_contatos(tenant_id,cliente_id,paciente_id,nome,telefone) values(@t,@t,@p,'Contato Teste','11999990000');
insert into plantaopro.paciente_enderecos(tenant_id,cliente_id,paciente_id,cidade,estado) values(@t,@t,@p,'Cidade','SP');
insert into plantaopro.paciente_consentimentos(tenant_id,cliente_id,paciente_id,finalidade,usuario_id) values(@t,@t,@p,'ASSISTENCIA_SAUDE',@u);
insert into plantaopro.clinica_contas_receber(tenant_id,cliente_id,paciente_id) values(@t,@t,@p);
insert into plantaopro.clinica_recebimentos(tenant_id,cliente_id,paciente_id) values(@t,@t,@p);
insert into plantaopro.plano_saude_pacientes(cliente_id,tenant_id,plano_saude_id,paciente_id,principal,status) values(@t,@t,gen_random_uuid(),@p,true,'ATIVO');
insert into plantaopro.paciente_historico(id,cliente_id,tenant_id,paciente_id,acao,detalhes,usuario_id) values(gen_random_uuid(),@t,@t,@p,'TESTE_R8B6',cast('{}' as jsonb),@u);", new { t, p = pac, u = uid, i, f });
    }

    private static async Task LimparAsync(params Guid[] tenants)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        foreach (var t in tenants)
            await cn.ExecuteAsync(@"
delete from plantaopro.clinica_recebimentos where tenant_id=@t or cliente_id=@t;
delete from plantaopro.clinica_contas_receber where tenant_id=@t or cliente_id=@t;
delete from plantaopro.plano_saude_pacientes where tenant_id=@t or cliente_id=@t;
delete from plantaopro.paciente_historico where tenant_id=@t or cliente_id=@t;
delete from plantaopro.paciente_consentimentos where tenant_id=@t or cliente_id=@t;
delete from plantaopro.paciente_enderecos where tenant_id=@t or cliente_id=@t;
delete from plantaopro.paciente_contatos where tenant_id=@t or cliente_id=@t;
delete from plantaopro.triagens where tenant_id=@t or cliente_id=@t;
delete from plantaopro.consultas where tenant_id=@t or cliente_id=@t;
delete from plantaopro.agendamentos where tenant_id=@t or cliente_id=@t;
delete from plantaopro.pacientes where tenant_id=@t or cliente_id=@t;
delete from plantaopro.clientes where tenant_id=@t or id=@t;", new { t });
    }

    /// <summary>Linhas ainda apontando para a origem nas 39 tabelas (paciente_historico excluído — a origem
    /// retém por design sua própria trilha MESCLAR depois do re-pontejamento).</summary>
    private static async Task<int> RestantesNaOrigemAsync(NpgsqlConnection cn, Guid origem)
    {
        var total = 0;
        foreach (var t in Tabelas.Where(x => x != "paciente_historico"))
            total += await cn.ExecuteScalarAsync<int>($"select count(1) from plantaopro.{t} where paciente_id=@o", new { o = origem });
        return total;
    }

    // ---------- fatos ----------

    [Fact]
    public async Task Mescla_MoveTodosVinculos_InativaOrigem_ComTrilhaECPFLiberado()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, t);
            var alvo = await SeedPacAsync(cn, t, "R8B6 Alvo", "300.000.000-01");
            var origem = await SeedPacAsync(cn, t, "R8B6 Origem", "400.000.000-02");
            await SeedVinculosAsync(cn, t, origem, u);

            var r = await Clinica(t, u).MesclarPacientesAsync(alvo, origem, "duplicidade encontrada na busca por nome e CPF");
            Assert.Equal(200, r.StatusCode);

            Assert.Equal(0, await RestantesNaOrigemAsync(cn, origem));
            foreach (var tch in new[] { "agendamentos", "consultas", "triagens", "clinica_contas_receber", "clinica_recebimentos", "paciente_contatos", "paciente_enderecos", "paciente_consentimentos", "plano_saude_pacientes" })
            {
                var n = await cn.ExecuteScalarAsync<int>($"select count(1) from plantaopro.{tch} where paciente_id=@a", new { a = alvo });
                Assert.True(n == 1, $"tabela {tch}: esperado 1 vinculo no alvo, encontrado {n}");
            }

            var o = await cn.QuerySingleAsync("select status, reg_status, mesclado_em, mesclado_por from plantaopro.pacientes where id=@o", new { o = origem });
            Assert.Equal("INATIVO", (string)o.status);
            Assert.Equal("I", (string)o.reg_status);
            Assert.Equal(alvo, (Guid)o.mesclado_em);
            Assert.Equal(u, (Guid)o.mesclado_por);

            // Trilha MESCLAR nos dois lados + histórico antigo da origem movido para o destino.
            Assert.Equal(1, await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.paciente_historico where paciente_id=@a and acao='mesclar'", new { a = alvo }));
            Assert.Equal(1, await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.paciente_historico where paciente_id=@o and acao='mesclar'", new { o = origem }));
            Assert.Equal(1, await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.paciente_historico where paciente_id=@a and acao='TESTE_R8B6'", new { a = alvo }));

            // CPF da origem liberado do índice único: novo paciente com o mesmo CPF pode ser criado.
            var novo = await SeedPacAsync(cn, t, "R8B6 Novo MesmoCPF", "400.000.000-02");
            Assert.NotEqual(Guid.Empty, novo);
        }
        finally { await LimparAsync(t); }
    }

    [Fact]
    public async Task Mescla_SemMotivo_400SemEscrita()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, t);
            var alvo = await SeedPacAsync(cn, t, "R8B6 SemMotivo A");
            var origem = await SeedPacAsync(cn, t, "R8B6 SemMotivo B");
            var r = await Clinica(t, u).MesclarPacientesAsync(alvo, origem, "  ");
            Assert.Equal(400, r.StatusCode);
            Assert.Null(await cn.ExecuteScalarAsync<Guid?>("select mesclado_em from plantaopro.pacientes where id=@o", new { o = origem }));
            var reg = await cn.QuerySingleAsync("select reg_status from plantaopro.pacientes where id=@o", new { o = origem });
            Assert.Equal("A", (string)reg.reg_status);
        }
        finally { await LimparAsync(t); }
    }

    [Fact]
    public async Task Mescla_AlvoIgualOrigem_400()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        var id = Guid.NewGuid();
        var r = await Clinica(t, u).MesclarPacientesAsync(id, id, "motivo qualquer");
        Assert.Equal(400, r.StatusCode);
    }

    [Fact]
    public async Task Mescla_CrossTenant_409NadaMovido()
    {
        var t1 = Guid.NewGuid(); var t2 = Guid.NewGuid(); var u = Guid.NewGuid();
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, t1);
            await SeedTenantAsync(cn, t2);
            var alvo = await SeedPacAsync(cn, t1, "R8B6 Cross Alvo T1");
            var origem = await SeedPacAsync(cn, t2, "R8B6 Cross Origem T2");
            await SeedVinculosAsync(cn, t2, origem, u);

            var r = await Clinica(t1, u).MesclarPacientesAsync(alvo, origem, "tentativa cross-tenant");
            Assert.Equal(409, r.StatusCode);

            Assert.Equal(1, await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.agendamentos where paciente_id=@o", new { o = origem }));
            Assert.Null(await cn.ExecuteScalarAsync<Guid?>("select mesclado_em from plantaopro.pacientes where id=@o", new { o = origem }));
        }
        finally { await LimparAsync(t1, t2); }
    }

    [Fact]
    public async Task Mescla_OrigemJaMesclada_409()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, t);
            var alvo = await SeedPacAsync(cn, t, "R8B6 Reuso Alvo");
            var origem = await SeedPacAsync(cn, t, "R8B6 Reuso Origem");
            await SeedVinculosAsync(cn, t, origem, u);

            var ok = await Clinica(t, u).MesclarPacientesAsync(alvo, origem, "primeira mescla");
            Assert.Equal(200, ok.StatusCode);

            var deNovo = await Clinica(t, u).MesclarPacientesAsync(alvo, origem, "segunda tentativa");
            Assert.Equal(409, deNovo.StatusCode);
            var ponteiro = await cn.QuerySingleAsync("select mesclado_em from plantaopro.pacientes where id=@o", new { o = origem });
            Assert.Equal(alvo, (Guid)ponteiro.mesclado_em);
        }
        finally { await LimparAsync(t); }
    }

    [Fact]
    public async Task Mescla_AlvoJaMesclado_409()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, t);
            var alvo1 = await SeedPacAsync(cn, t, "R8B6 Destino1");
            var jaMesclado = await SeedPacAsync(cn, t, "R8B6 JaAbsorvido");
            var outro = await SeedPacAsync(cn, t, "R8B6 OutroPaciente");

            var ok = await Clinica(t, u).MesclarPacientesAsync(alvo1, jaMesclado, "absorve o primeiro");
            Assert.Equal(200, ok.StatusCode);

            var r = await Clinica(t, u).MesclarPacientesAsync(jaMesclado, outro, "usar ja-mesclado como destino");
            Assert.Equal(409, r.StatusCode);
            Assert.Null(await cn.ExecuteScalarAsync<Guid?>("select mesclado_em from plantaopro.pacientes where id=@o", new { o = outro }));
        }
        finally { await LimparAsync(t); }
    }

    [Fact]
    public async Task Mescla_PlanoPrincipalDaOrigem_Despromovido()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, t);
            var alvo = await SeedPacAsync(cn, t, "R8B6 Plano Alvo");
            var origem = await SeedPacAsync(cn, t, "R8B6 Plano Origem");
            var linhaAlvo = await SeedPlanoAsync(cn, t, alvo, Guid.NewGuid(), true);
            var linhaOrigem = await SeedPlanoAsync(cn, t, origem, Guid.NewGuid(), true);

            var r = await Clinica(t, u).MesclarPacientesAsync(alvo, origem, "dois planos principais em pacientes distintos");
            Assert.Equal(200, r.StatusCode);

            var ra = await cn.QuerySingleAsync("select principal, paciente_id from plantaopro.plano_saude_pacientes where id=@id", new { id = linhaAlvo });
            Assert.True((bool)ra.principal);
            Assert.Equal(alvo, (Guid)ra.paciente_id);
            var ro = await cn.QuerySingleAsync("select principal, paciente_id from plantaopro.plano_saude_pacientes where id=@id", new { id = linhaOrigem });
            Assert.False((bool)ro.principal);
            Assert.Equal(alvo, (Guid)ro.paciente_id);
            Assert.Equal(1, await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.plano_saude_pacientes where paciente_id=@a and principal=true and reg_status='A'", new { a = alvo }));
        }
        finally { await LimparAsync(t); }
    }

    [Fact]
    public async Task Mescla_Concorrencia_MesmaOrigemDoisAlvos_ExatamenteUmGanha()
    {
        var t = Guid.NewGuid(); var u = Guid.NewGuid();
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, t);
            var alvo1 = await SeedPacAsync(cn, t, "R8B6 Conc A1");
            var alvo2 = await SeedPacAsync(cn, t, "R8B6 Conc A2");
            var origem = await SeedPacAsync(cn, t, "R8B6 Conc O");
            await SeedVinculosAsync(cn, t, origem, u);

            var s1 = Clinica(t, u);
            var s2 = Clinica(t, u);
            var resultados = await Task.WhenAll(
                s1.MesclarPacientesAsync(alvo1, origem, "corrida 1"),
                s2.MesclarPacientesAsync(alvo2, origem, "corrida 2"));
            var r1 = resultados[0];
            var r2 = resultados[1];

            var status = new[] { r1.StatusCode, r2.StatusCode };
            Assert.Contains(200, status);
            Assert.Contains(409, status);

            var vencedor = await cn.ExecuteScalarAsync<Guid>("select mesclado_em from plantaopro.pacientes where id=@o", new { o = origem });
            Assert.True(vencedor == alvo1 || vencedor == alvo2);
            Assert.Equal(1, await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.agendamentos where paciente_id=@v", new { v = vencedor }));
            Assert.Equal(0, await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.agendamentos where paciente_id in (@a1,@a2) and paciente_id<>@v", new { a1 = alvo1, a2 = alvo2, v = vencedor }));
        }
        finally { await LimparAsync(t); }
    }

    [Fact]
    public async Task Contrato_MigrationEServicoEEndpointCoerentesComSchemaVivo()
    {
        var mig = Read("database/migrations/2026_10_v2349_r8b6_mesclagem_pacientes.sql");
        Assert.Contains("mesclado_em", mig);
        Assert.Contains("mesclado_em_em", mig);
        Assert.Contains("mesclado_por", mig);
        Assert.Contains("ix_pacientes_mesclado_em", mig);

        var svc = Read("backend/PlantaoPro.Api/Saude360ClinicalService.cs");
        Assert.Contains("MesclarPacientesAsync", svc);
        Assert.Contains("TabelasComPacienteId", svc);
        Assert.Contains("mesclado_em=", svc);

        var ctl = Read("backend/PlantaoPro.Api/Controllers/Saude360ClinicalControllers.cs");
        Assert.Contains("{id:guid}/mesclar", ctl);

        // Array do serviço (reflexão) reconciliado com o espelho do teste e com o information_schema vivo.
        await GarantirSchemaAsync();
        var campo = typeof(Saude360ClinicalService).GetField("TabelasComPacienteId", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(campo);
        var arr = (string[])campo.GetValue(null)!;
        Assert.Equal(Tabelas.Length, arr.Length);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var vivo = (await cn.QueryAsync<string>("select table_name from information_schema.columns where table_schema='plantaopro' and column_name='paciente_id' order by 1")).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(vivo, new HashSet<string>(arr, StringComparer.Ordinal));
        Assert.Equal(vivo, new HashSet<string>(Tabelas, StringComparer.Ordinal));
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
