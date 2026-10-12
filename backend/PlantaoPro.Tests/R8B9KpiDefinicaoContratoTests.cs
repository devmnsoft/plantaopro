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
/// R8-B9 (§27 — estender padrão Operacional / contrato de definição).
///
/// Achado r8a0 §29 item 1 lista 5 calculadores sobrepostos de dashboard/KPI
/// (BiService, DashboardPremiumService, DashboardService/api-dashboard,
/// Fase6BiIntegracoesService, v114/operacional). Apenas Relatórios Operacionais
/// expunha definição por indicador (<see cref="OperationalReportIndicator.Definition"/>
/// + <see cref="OperationalReportResult.TimeCriterion"/>). Esta fatia ESTENDE esse
/// padrão aos demais, de modo que cada indicador de cada calculador expose
/// fórmula/fonte/período/fuso/filtros — tornando os calculadores sobrepostos
/// auto-descritivos e comparáveis. A consolidação propriamente dita (canônico →
/// migrar → regressões) segue o rito §29, sem deleção nesta rodada.
///
/// Contrato (prova por comportamento contra o test DB + referência por reflexão):
///   • BI resumo executivo: Success + Periodo/Fuso/TimeCriterion não vazios +
///     Definicoes cobre os códigos-chave (nenhum indicador com definição vazia);
///   • api/dashboard: Success + Indicadores.TimeCriterion + Indicadores.Definicoes
///     cobrem os códigos (nenhum indicador com definição vazia);
///   • Dashboard Premium: todo KPI retornado carrega Fonte=="real" + Definicao não vazia;
///   • Fase6 BI: todo indicador retornado carrega Explicacao não vazia;
///   • Operacional (referência estendida): o padrão de origem permanece íntegro.
/// </summary>
public sealed class R8B9KpiDefinicaoContratoTests
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

    private static Fase6BiIntegracoesService ServicoFase6(Guid tenant) => new(
        Cfg(),
        NullLogger<Fase6BiIntegracoesService>.Instance,
        Context(tenant),
        new AuditService(Cfg(), NullLogger<AuditService>.Instance));

    [Fact]
    public async Task bi_resumo_executivo_exposicao_definicao_por_indicador()
    {
        var bi = new BiService(Cfg(), NullLogger<BiService>.Instance);
        var resp = await bi.GetResumoExecutivoAsync(null, null, null);
        Assert.True(resp.Success, resp.Message);
        Assert.NotNull(resp.Data);
        Assert.False(string.IsNullOrWhiteSpace(resp.Data.Periodo), "Periodo vazio");
        Assert.False(string.IsNullOrWhiteSpace(resp.Data.Fuso), "Fuso vazio");
        Assert.False(string.IsNullOrWhiteSpace(resp.Data.TimeCriterion), "TimeCriterion vazio");
        Assert.True(resp.Data.Definicoes.Count > 0, "Definicoes vazias no resumo executivo");

        foreach (var codigo in new[] { "TotalClientesAtivos", "ReceitaMensalEstimada", "PlantoesPublicadosMes", "PercentualCobertura", "TempoMedioPreenchimentoHoras", "EscalasConfirmadasPeriodo", "ValorPagoPeriodo" })
        {
            var d = resp.Data.Definicoes.FirstOrDefault(x => x.Codigo == codigo);
            Assert.True(d != null, $"Definição ausente para o código {codigo}");
            Assert.False(string.IsNullOrWhiteSpace(d!.Label), $"Label vazio p/ {codigo}");
            Assert.False(string.IsNullOrWhiteSpace(d.Definicao), $"Definicao vazia p/ {codigo}");
        }
        foreach (var d in resp.Data.Definicoes)
            Assert.False(string.IsNullOrWhiteSpace(d.Definicao), $"KPI {d.Codigo} com Definicao vazia");
    }

    [Fact]
    public async Task dashboard_api_exposicao_definicao_por_indicador_e_criterio_de_tempo()
    {
        var svc = new DashboardService(Cfg());
        var resp = await svc.GetAsync(Guid.NewGuid(), null);
        Assert.True(resp.Success, resp.Message);
        Assert.NotNull(resp.Data);
        Assert.False(string.IsNullOrWhiteSpace(resp.Data.Indicadores.TimeCriterion), "Indicadores.TimeCriterion vazio");
        Assert.True(resp.Data.Indicadores.Definicoes.Count > 0, "Indicadores.Definicoes vazias");

        foreach (var codigo in new[] { "TotalMedicos", "TotalPlantoes", "PlantoesAbertos", "PagamentosPendentes", "ValorPendente", "ValorPagoMes", "NotificacoesNaoLidas" })
        {
            var d = resp.Data.Indicadores.Definicoes.FirstOrDefault(x => x.Codigo == codigo);
            Assert.True(d != null, $"Definição ausente para o código {codigo}");
            Assert.False(string.IsNullOrWhiteSpace(d!.Label), $"Label vazio p/ {codigo}");
            Assert.False(string.IsNullOrWhiteSpace(d.Definicao), $"Definicao vazia p/ {codigo}");
        }
        foreach (var d in resp.Data.Indicadores.Definicoes)
            Assert.False(string.IsNullOrWhiteSpace(d.Definicao), $"KPI {d.Codigo} com Definicao vazia");
    }

    [Fact]
    public async Task dashboard_premium_todo_kpi_com_definicao_e_fonte_real()
    {
        var svc = new DashboardPremiumService(Cfg(), NullLogger<DashboardPremiumService>.Instance);
        var tenant = Guid.NewGuid();
        var dto = await svc.ObterAsync("admin", tenant);
        Assert.NotNull(dto);
        var kpis = dto.Kpis.ToList();
        Assert.True(kpis.Count > 0, "Nenhum KPI retornado no perfil premium");
        foreach (var k in kpis)
        {
            Assert.Equal("real", k.Fonte);
            Assert.False(string.IsNullOrWhiteSpace(k.Definicao), $"KPI {k.Titulo} sem Definicao");
        }
    }

    [Fact]
    public async Task fase6_bi_todo_indicador_com_explicacao()
    {
        var tenant = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await cn.ExecuteAsync("insert into plantaopro.clientes(id,tenant_id,nome,status,reg_status) values(@t,@t,'R8B9 Ten','ATIVO','A')", new { t = tenant });
            var resp = await ServicoFase6(tenant).DashboardAsync("operacional");
            Assert.True(resp.Success, resp.Message);
            Assert.NotNull(resp.Data);
            var inds = resp.Data.Indicadores.ToList();
            Assert.True(inds.Count > 0, "Fase6 não retornou indicadores para o tenant");
            foreach (var k in inds)
                Assert.False(string.IsNullOrWhiteSpace(k.Explicacao), $"Fase6 KPI {k.Codigo} sem Explicacao");
        }
        finally
        {
            await cn.ExecuteAsync("delete from plantaopro.clientes where id=@id", new { id = tenant });
        }
    }

    [Fact]
    public void operacional_padrao_de_referencia_integro()
    {
        var t = typeof(OperationalReportIndicator);
        var def = t.GetProperty("Definition");
        Assert.True(def != null, "OperationalReportIndicator perdeu o campo Definition (padrão de referência estendido por R8-B9)");
        var r = typeof(OperationalReportResult);
        var tc = r.GetProperty("TimeCriterion");
        Assert.True(tc != null, "OperationalReportResult perdeu o campo TimeCriterion (padrão de referência estendido por R8-B9)");
    }
}
