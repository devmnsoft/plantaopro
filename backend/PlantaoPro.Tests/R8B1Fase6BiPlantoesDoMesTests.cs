using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Data;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B1 — Bug "Plantões do mês" (achado r8a0 §27 / r8a0-varredura-indicadores): o KPI
/// <c>plantoes_mes</c> de <see cref="Fase6BiIntegracoesService.DashboardAsync"/> era rotulado
/// "Plantões do mês", mas o SQL contava TODOS os plantões ativos do tenant, sem filtro de mês.
/// Correção (decisão registrada no doc r8b1): o rótulo e o código já declaravam a intenção
/// mensal — faltava só o filtro. Agora conta plantões com <c>data_inicio</c> no mês corrente.
/// Bloqueadores adicionais corrigidos na mesma statement (necessários para a jornada
/// DashboardAsync funcionar, provados por este teste):
///   • <c>total_receber</c> referenciava a tabela inexistente <c>financeiro_contas_receber</c>
///     (500 em todo perfil tenant); agora usa a canônica <c>clinica_contas_receber.valor_pendente</c>;
///   • leitura do trio de KPIs via tupla tipada (antes via dynamic/DapperRow, que resolvia nulo
///     e zera-via o valor mesmo com a linha presente).
/// Prova: planta-se 1 plantão do tenant com início no mês corrente + 3 distrações
/// (mês anterior / outro tenant / inativo) e o KPI deve valer exatamente 1.
/// Os limites da janela usam <c>date_trunc('month', now()) ± …</c> calculados no banco,
/// para o teste não quebrar em transição de mês.
/// </summary>
public sealed class R8B1Fase6BiPlantoesDoMesTests
{
    private static readonly Guid UsuarioSeed = Guid.Parse("e1b1c2d3-0000-4000-8000-0000000000a9");

    private static IConfiguration Cfg() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
        .Build();

    private static UsuarioContextService Context(Guid tenant)
    {
        var accessor = new HttpContextAccessor();
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("cliente_id", tenant.ToString()),
            new Claim("uid", UsuarioSeed.ToString())
        }, "TestAuth"));
        accessor.HttpContext = ctx;
        return new UsuarioContextService(accessor);
    }

    private static Fase6BiIntegracoesService Servico(Guid tenant) => new(
        Cfg(),
        NullLogger<Fase6BiIntegracoesService>.Instance,
        Context(tenant),
        new AuditService(Cfg(), NullLogger<AuditService>.Instance));

    private static async Task<Guid> SeedPltAsync(NpgsqlConnection cn, Guid t, Guid h, Guid e, DateTime inicio, string regStatus) =>
        await cn.ExecuteScalarAsync<Guid>(
            @"insert into plantaopro.plantoes(tenant_id,cliente_id,hospital_id,especialidade_id,data_inicio,data_fim,valor,vagas,vagas_disponiveis,status,reg_status)
              values(@t,@t,@h,@e,@i,@f,100,1,1,'aberto',@rs) returning id",
            new { t, h, e, i = inicio, f = inicio.AddHours(8), rs = regStatus });

    [Fact]
    public async Task kpi_plantoes_do_mes_soma_somente_inicio_no_mes_corrente_do_proprio_tenant_ativo()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        Guid hospA = Guid.Empty, espA = Guid.Empty, hospB = Guid.Empty, espB = Guid.Empty;
        var plantoes = new List<Guid>();
        try
        {
            await cn.ExecuteAsync("insert into plantaopro.clientes(id,tenant_id,nome,status,reg_status) values(@t,@t,@n,'ATIVO','A')", new { t = tenantA, n = "R8B1 Tenante A" });
            await cn.ExecuteAsync("insert into plantaopro.clientes(id,tenant_id,nome,status,reg_status) values(@t,@t,@n,'ATIVO','A')", new { t = tenantB, n = "R8B1 Tenante B" });
            hospA = await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.hospitais(tenant_id,cliente_id,nome,nome_fantasia,status) values(@t,@t,'R8B1 Hosp A','R8B1 Hosp A','ATIVO') returning id", new { t = tenantA });
            espA = await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.especialidades(tenant_id,cliente_id,nome,status) values(@t,@t,'R8B1 Esp A','ATIVO') returning id", new { t = tenantA });
            hospB = await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.hospitais(tenant_id,cliente_id,nome,nome_fantasia,status) values(@t,@t,'R8B1 Hosp B','R8B1 Hosp B','ATIVO') returning id", new { t = tenantB });
            espB = await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.especialidades(tenant_id,cliente_id,nome,status) values(@t,@t,'R8B1 Esp B','ATIVO') returning id", new { t = tenantB });

            // limites robustos a transição de mês (calculados no banco): 2º dia do mês corrente / fim do mês anterior
            var noMesCorrente = await cn.ExecuteScalarAsync<DateTime>("select date_trunc('month', now()) + interval '1 day'");
            var mesPassado = await cn.ExecuteScalarAsync<DateTime>("select date_trunc('month', now()) - interval '1 hour'");

            plantoes.Add(await SeedPltAsync(cn, tenantA, hospA, espA, noMesCorrente, "A"));   // o único que deve contar
            plantoes.Add(await SeedPltAsync(cn, tenantA, hospA, espA, mesPassado, "A"));      // início em mês anterior → fora
            plantoes.Add(await SeedPltAsync(cn, tenantB, hospB, espB, noMesCorrente, "A"));   // outro tenant → fora
            plantoes.Add(await SeedPltAsync(cn, tenantA, hospA, espA, noMesCorrente, "I"));   // registro inativo → fora

            var resp = await Servico(tenantA).DashboardAsync("operacional");

            Assert.True(resp.Success, resp.Message);
            Assert.NotNull(resp.Data);
            var kpi = Assert.Single(resp.Data.Indicadores, x => x.Codigo == "plantoes_mes");
            Assert.Equal("Plantões do mês", kpi.Nome);
            Assert.Equal(1m, kpi.Valor);
            // os demais KPIs do tenant seguem existindo (escopo da correção = a janela do mês + bloqueadores)
            Assert.Contains(resp.Data.Indicadores, x => x.Codigo == "agendamentos");
            Assert.Contains(resp.Data.Indicadores, x => x.Codigo == "total_receber");
        }
        finally
        {
            if (plantoes.Count > 0) await cn.ExecuteAsync("delete from plantaopro.plantoes where id = any(@ids)", new { ids = plantoes.ToArray() });
            if (hospA != Guid.Empty) await cn.ExecuteAsync("delete from plantaopro.hospitais where id=@id", new { id = hospA });
            if (hospB != Guid.Empty) await cn.ExecuteAsync("delete from plantaopro.hospitais where id=@id", new { id = hospB });
            if (espA != Guid.Empty) await cn.ExecuteAsync("delete from plantaopro.especialidades where id=@id", new { id = espA });
            if (espB != Guid.Empty) await cn.ExecuteAsync("delete from plantaopro.especialidades where id=@id", new { id = espB });
            await cn.ExecuteAsync("delete from plantaopro.clientes where id=@id", new { id = tenantA });
            await cn.ExecuteAsync("delete from plantaopro.clientes where id=@id", new { id = tenantB });
        }
    }
}
