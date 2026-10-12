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
/// R8-B5 — Write path produtivo de <c>medico_especialidades</c> (matriz r8a0 linha "Especialidades na
/// operação" / varredura vinculos-unidades A2): o M:N nasceu em v2341 para sustentar a elegibilidade por
/// especialidade (<c>MedicoElegibilidadeService</c>, motivo <c>ESPECIALIDADE</c>) e o score de
/// recomendação (80×20), mas nenhum writer produtivo o populava — a tabela nasce vazia em banco real e
/// todo médico com especialidade ficava inelegível por ela.
/// Correção: <c>MedicoService</c> espelha a especialidade do formulário com semântica replace-all
/// (desativa o não selecionado; reativa a linha histórica quando existe; insere só se o par nunca teve
/// registro — histórico via <c>reg_status</c>, único parcial <c>ux_medico_especialidade_ativa</c> garante
/// no máximo um vínculo ativo) + migration v2348 faz backfill idempotente dos médicos já existentes a
/// partir de <c>medicos.especialidade_id</c> ("poplar/migrar" da matriz).
/// </summary>
public sealed class R8B5EspecialidadesMedicoTests
{
    private static string Root => RepositoryPathResolver.ResolveRoot();

    private static IConfiguration Cfg() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
        .Build();

    private static MedicoService Servico() => new(Cfg(), new FakeAuditService(), NullLogger<MedicoService>.Instance);

    private static MedicoElegibilidadeService Elegibilidade() => new(Cfg(), new ConflitoHorarioService(Cfg()));

    private static string Sufixo() => Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

    private static string CpfUnico() => "9" + Guid.NewGuid().ToString("N")[..13];

    private sealed record MedCtx(Guid Id, string Cpf);

    // ---------- seeds (padrão R7-A1: tenant próprio por teste, cleanup escopado) ----------

    private static async Task SeedTenantAsync(NpgsqlConnection cn, Guid t) =>
        await cn.ExecuteAsync("insert into plantaopro.clientes(id,tenant_id,nome,status,reg_status) values(@t,@t,'R8B5 Clinica','ATIVO','A')", new { t });

    private static async Task<Guid> SeedEspAsync(NpgsqlConnection cn, Guid t, string nome) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.especialidades(tenant_id,cliente_id,nome,status) values(@t,@t,@n,'ATIVO') returning id", new { t, n = nome });

    private static async Task LimparAsync(Guid t)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync(@"
 delete from plantaopro.medico_especialidades where (especialidade_id in (select id from plantaopro.especialidades where tenant_id=@t or cliente_id=@t)) or (medico_id in (select id from plantaopro.medicos where tenant_id=@t or cliente_id=@t));
 delete from plantaopro.medicos where tenant_id=@t or cliente_id=@t;
 delete from plantaopro.especialidades where tenant_id=@t or cliente_id=@t;
 delete from plantaopro.clientes where tenant_id=@t or id=@t;", new { t });
    }

    // ---------- escrita via serviço produtivo ----------

    private static async Task<MedCtx> CriarMedicoAsync(string cs, MedicoService svc, Guid esp, string nome)
    {
        var cpf = CpfUnico();
        var r = await svc.CriarAsync(new CreateMedicoRequest(nome, cpf, "123456", "SP", $"r8b5-{Guid.NewGuid():N}@exemplo.com", "(11) 90000-0000", "Cidade", "SP", esp), Guid.NewGuid(), null, null);
        Assert.True(r.Success, r.Message);
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        return new MedCtx(await cn.ExecuteScalarAsync<Guid>("select id from plantaopro.medicos where cpf=@c", new { c = cpf }), cpf);
    }

    private static async Task AtualizarEspecialidadeAsync(MedicoService svc, MedCtx ctx, Guid esp)
    {
        var r = await svc.AtualizarAsync(ctx.Id, new UpdateMedicoRequest("Dr R8B5 Teste", ctx.Cpf, "123456", "SP", $"r8b5-{Guid.NewGuid():N}@exemplo.com", "(11) 90000-0000", "Cidade", "SP", esp, "A"), Guid.NewGuid(), null, null);
        Assert.True(r.Success, r.Message);
    }

    // ---------- leitura/estado ----------

    private static async Task<int> TotalAtivosAsync(string cs, Guid medico)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        return await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.medico_especialidades where medico_id=@m and reg_status='A'", new { m = medico });
    }

    private static async Task<Guid?> EspAtivaAsync(string cs, Guid medico)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        return await cn.QuerySingleOrDefaultAsync<Guid?>("select especialidade_id from plantaopro.medico_especialidades where medico_id=@m and reg_status='A' limit 1", new { m = medico });
    }

    private static async Task<int> LinhasParAsync(string cs, Guid medico, Guid esp)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        return await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.medico_especialidades where medico_id=@m and especialidade_id=@e", new { m = medico, e = esp });
    }

    private static async Task<string?> StatusParAsync(string cs, Guid medico, Guid esp)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        return await cn.QuerySingleOrDefaultAsync<string?>("select reg_status from plantaopro.medico_especialidades where medico_id=@m and especialidade_id=@e limit 1", new { m = medico, e = esp });
    }

    private static async Task AplicarV2348Async(string cs)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        await cn.ExecuteAsync("select pg_advisory_lock(hashtext('r8b5_medico_especialidades'));");
        try
        {
            var sql = File.ReadAllText(Path.Combine(Root, "database/migrations/2026_10_v2348_r8b5_backfill_medico_especialidades.sql"));
            await cn.ExecuteAsync(sql);
        }
        finally
        {
            await cn.ExecuteAsync("select pg_advisory_unlock(hashtext('r8b5_medico_especialidades'));");
        }
    }

    [Fact(DisplayName = "R8-B5: criar médico com especialidade espelha vínculo ativo e o consumidor de elegibilidade enxerga")]
    public async Task criar_com_especialidade_espelha_vinculo_ativo_e_consumidor_ve()
    {
        var cs = TestDatabase.ConnectionString;
        var t = Guid.NewGuid();
        try
        {
            Guid espA, espB;
            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                await SeedTenantAsync(cn, t);
                espA = await SeedEspAsync(cn, t, "R8B5 Esp A " + Sufixo());
                espB = await SeedEspAsync(cn, t, "R8B5 Esp B " + Sufixo());
            }

            var ctx = await CriarMedicoAsync(cs, Servico(), espA, "Dr R8B5 " + Sufixo());

            Assert.Equal(1, await TotalAtivosAsync(cs, ctx.Id));
            Assert.Equal(espA, await EspAtivaAsync(cs, ctx.Id));
            Assert.True(await Elegibilidade().VerificarEspecialidadeCompatívelAsync(ctx.Id, espA));
            Assert.False(await Elegibilidade().VerificarEspecialidadeCompatívelAsync(ctx.Id, espB));
        }
        finally
        {
            await LimparAsync(t);
        }
    }

    [Fact(DisplayName = "R8-B5: trocar a especialidade mantém um único vínculo ativo e preserva o histórico")]
    public async Task trocar_especialidade_mantem_unico_ativo_e_historico_preservado()
    {
        var cs = TestDatabase.ConnectionString;
        var t = Guid.NewGuid();
        try
        {
            Guid espA, espB;
            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                await SeedTenantAsync(cn, t);
                espA = await SeedEspAsync(cn, t, "R8B5 Esp A " + Sufixo());
                espB = await SeedEspAsync(cn, t, "R8B5 Esp B " + Sufixo());
            }

            var svc = Servico();
            var ctx = await CriarMedicoAsync(cs, svc, espA, "Dr R8B5 " + Sufixo());
            await AtualizarEspecialidadeAsync(svc, ctx, espB);

            Assert.Equal(1, await TotalAtivosAsync(cs, ctx.Id));
            Assert.Equal(espB, await EspAtivaAsync(cs, ctx.Id));
            Assert.True(await Elegibilidade().VerificarEspecialidadeCompatívelAsync(ctx.Id, espB));
            Assert.False(await Elegibilidade().VerificarEspecialidadeCompatívelAsync(ctx.Id, espA));
            // histórico preservado via reg_status — sem exclusão física
            Assert.Equal("I", await StatusParAsync(cs, ctx.Id, espA));
        }
        finally
        {
            await LimparAsync(t);
        }
    }

    [Fact(DisplayName = "R8-B5: voltar à especialidade histórica reativa a mesma linha sem duplicar")]
    public async Task voltar_a_especialidade_historica_reativa_sem_duplicar_linha()
    {
        var cs = TestDatabase.ConnectionString;
        var t = Guid.NewGuid();
        try
        {
            Guid espA, espB;
            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                await SeedTenantAsync(cn, t);
                espA = await SeedEspAsync(cn, t, "R8B5 Esp A " + Sufixo());
                espB = await SeedEspAsync(cn, t, "R8B5 Esp B " + Sufixo());
            }

            var svc = Servico();
            var ctx = await CriarMedicoAsync(cs, svc, espA, "Dr R8B5 " + Sufixo());
            await AtualizarEspecialidadeAsync(svc, ctx, espB);
            await AtualizarEspecialidadeAsync(svc, ctx, espA);

            Assert.Equal(1, await LinhasParAsync(cs, ctx.Id, espA));
            Assert.Equal("A", await StatusParAsync(cs, ctx.Id, espA));
            Assert.Equal(1, await TotalAtivosAsync(cs, ctx.Id));
            Assert.True(await Elegibilidade().VerificarEspecialidadeCompatívelAsync(ctx.Id, espA));
        }
        finally
        {
            await LimparAsync(t);
        }
    }

    [Fact(DisplayName = "R8-B5: remover a especialidade desativa todos os vínculos (sem exclusão física)")]
    public async Task remover_especialidade_desativa_todos_os_vinculos()
    {
        var cs = TestDatabase.ConnectionString;
        var t = Guid.NewGuid();
        try
        {
            Guid espA;
            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                await SeedTenantAsync(cn, t);
                espA = await SeedEspAsync(cn, t, "R8B5 Esp A " + Sufixo());
            }

            var svc = Servico();
            var ctx = await CriarMedicoAsync(cs, svc, espA, "Dr R8B5 " + Sufixo());
            await AtualizarEspecialidadeAsync(svc, ctx, Guid.Empty);

            Assert.Equal(0, await TotalAtivosAsync(cs, ctx.Id));
            Assert.Null(await EspAtivaAsync(cs, ctx.Id));
            Assert.False(await Elegibilidade().VerificarEspecialidadeCompatívelAsync(ctx.Id, espA));
            Assert.Equal("I", await StatusParAsync(cs, ctx.Id, espA));
        }
        finally
        {
            await LimparAsync(t);
        }
    }

    [Fact(DisplayName = "R8-B5: criar médico sem especialidade não gera vínculo")]
    public async Task criar_sem_especialidade_nao_gera_vinculo()
    {
        var cs = TestDatabase.ConnectionString;
        var t = Guid.NewGuid();
        try
        {
            Guid espA;
            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                await SeedTenantAsync(cn, t);
                espA = await SeedEspAsync(cn, t, "R8B5 Esp A " + Sufixo());
            }

            var ctx = await CriarMedicoAsync(cs, Servico(), Guid.Empty, "Dr R8B5 Sem Esp " + Sufixo());

            Assert.Equal(0, await LinhasParAsync(cs, ctx.Id, espA));
            Assert.Equal(0, await TotalAtivosAsync(cs, ctx.Id));
            Assert.False(await Elegibilidade().VerificarEspecialidadeCompatívelAsync(ctx.Id, espA));
        }
        finally
        {
            await LimparAsync(t);
        }
    }

    [Fact(DisplayName = "R8-B5: backfill v2348 popula médico legado e é idempotente (sem duplicar o já espelhado)")]
    public async Task backfill_v2348_popula_medico_legado_e_e_idempotente()
    {
        var cs = TestDatabase.ConnectionString;
        var t = Guid.NewGuid();
        try
        {
            var espA = Guid.Empty;
            string cpfLegado;
            Guid legado;
            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                await SeedTenantAsync(cn, t);
                espA = await SeedEspAsync(cn, t, "R8B5 Esp A " + Sufixo());
                // Médico "legado": writer produtivo pré-R8-B5 gravava só medicos.especialidade_id (Data.cs antigo),
                // nunca o M:N — é exatamente esse estado que o backfill precisa cobrir.
                cpfLegado = CpfUnico();
                legado = await cn.ExecuteScalarAsync<Guid>(@"
                    insert into plantaopro.medicos(id,nome,cpf,crm,uf_crm,especialidade_id,reg_date,reg_status,created_by)
                    values(gen_random_uuid(),'Dr Legado R8B5 ' || @suf,@cpf,'777777','SP',@esp,now(),'A',gen_random_uuid())
                    returning id", new { suf = Sufixo(), cpf = cpfLegado, esp = espA });
                Assert.Equal(0, await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.medico_especialidades where medico_id=@m", new { m = legado }));
            }

            var ctx = await CriarMedicoAsync(cs, Servico(), espA, "Dr R8B5 " + Sufixo());

            await AplicarV2348Async(cs);
            await AplicarV2348Async(cs); // idempotente

            // legado ganhou exatamente um vínculo ativo para a especialidade registrada
            Assert.Equal(1, await TotalAtivosAsync(cs, legado));
            Assert.Equal(espA, await EspAtivaAsync(cs, legado));
            Assert.Equal(1, await LinhasParAsync(cs, legado, espA));
            Assert.True(await Elegibilidade().VerificarEspecialidadeCompatívelAsync(legado, espA));

            // médico criado pelo writer novo não foi duplicado pelo backfill
            Assert.Equal(1, await LinhasParAsync(cs, ctx.Id, espA));
            Assert.Equal(1, await TotalAtivosAsync(cs, ctx.Id));
        }
        finally
        {
            await LimparAsync(t);
        }
    }

    [Fact(DisplayName = "R8-B5 contrato: espelho no MedicoService (criar+atualizar), form alimenta e backfill existe na fonte")]
    public void contrato_write_path_presente_na_fonte()
    {
        var data = File.ReadAllText(Path.Combine(Root, "backend/PlantaoPro.Api/Data.cs"));
        Assert.Contains("private static async Task EspelharEspecialidadeAsync(NpgsqlConnection cn, Guid medicoId, Guid especialidadeId)", data);
        Assert.Contains("update plantaopro.medico_especialidades set reg_status='I' where medico_id=@id and reg_status='A'", data);
        Assert.Contains("insert into plantaopro.medico_especialidades(medico_id,especialidade_id,reg_status) select @id,@esp,'A'", data);
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(data, "EspelharEspecialidadeAsync\\(cn, id, req\\.EspecialidadeId\\)").Count);

        var form = File.ReadAllText(Path.Combine(Root, "backend/PlantaoPro.Web/Views/Medicos/_MedicoForm.cshtml"));
        Assert.Contains("asp-for=\"EspecialidadeId\"", form);

        var mig = File.ReadAllText(Path.Combine(Root, "database/migrations/2026_10_v2348_r8b5_backfill_medico_especialidades.sql"));
        Assert.Contains("not exists(select 1 from plantaopro.medico_especialidades", mig);
    }

    private sealed class FakeAuditService : IAuditService
    {
        public Task RegistrarAsync(Guid? usuarioId, Guid? clienteId, string entidade, Guid? entidadeId, string acao, object? detalhes, bool sucesso, string? ipOrigem, string? perfil, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task LogAsync(Guid? userId, string acao, string entidade, Guid? registroId, string descricao, string? valorAnterior = null, string? valorNovo = null, string? ip = null, string? userAgent = null)
            => Task.CompletedTask;
    }
}
