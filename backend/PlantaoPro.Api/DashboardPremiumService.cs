using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed class DashboardPremiumService
{
    private readonly IConfiguration cfg;
    private readonly ILogger<DashboardPremiumService> logger;
    public DashboardPremiumService(IConfiguration cfg, ILogger<DashboardPremiumService> logger) { this.cfg = cfg; this.logger = logger; }
    public async Task<DashboardPremiumDto> ObterAsync(string perfil, Guid? clienteId)
    {
        var dto = new DashboardPremiumDto(perfil, new List<DashboardKpiDto>(), new List<string>(), new List<string>(), new List<DashboardAtalhoDto>(), new List<DashboardChartItem>(), "Plano validado conforme contexto do usuário.");
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            var kpis = (List<DashboardKpiDto>)dto.Kpis;
            await AddCount(cn, kpis, "Plantões", "plantaopro.plantoes", clienteId);
            await AddCount(cn, kpis, "Escalas", "plantaopro.escalas", clienteId);
            await AddCount(cn, kpis, "Médicos", "plantaopro.medicos", clienteId);
            await AddCount(cn, kpis, "Agendamentos", "plantaopro.agendamentos", clienteId);
            ((List<string>)dto.ProximosPassos).Add("Revisar pendências reais e filtros do perfil " + perfil + ".");
            ((List<DashboardAtalhoDto>)dto.Atalhos).Add(new DashboardAtalhoDto("Operação Inteligente", "/OperacaoInteligente/Index"));
            ((List<DashboardChartItem>)dto.Series).AddRange(kpis.Select(k => new DashboardChartItem(k.Titulo, k.Valor)));
            if (!kpis.Any()) ((List<string>)dto.Alertas).Add("Sem dados reais para exibir neste contexto.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Dashboard premium indisponível para perfil {Perfil}", perfil);
            ((List<string>)dto.Alertas).Add("Dados reais indisponíveis neste ambiente. Valide PostgreSQL/migrations.");
        }
        return dto;
    }
    private static async Task AddCount(NpgsqlConnection cn, IList<DashboardKpiDto> kpis, string titulo, string tabela, Guid? clienteId)
    {
        try
        {
            var total = await cn.ExecuteScalarAsync<long>("select count(1)::bigint from " + tabela + " where reg_status='A' and (@clienteId is null or cliente_id=@clienteId)", new { clienteId });
            kpis.Add(new DashboardKpiDto(titulo, total, "real"));
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01" || ex.SqlState == "42703") { }
    }
}

public record DashboardPremiumDto(string Perfil,IEnumerable<DashboardKpiDto> Kpis,IEnumerable<string> Alertas,IEnumerable<string> ProximosPassos,IEnumerable<DashboardAtalhoDto> Atalhos,IEnumerable<DashboardChartItem> Series,string StatusPlanoModulo);
public record DashboardKpiDto(string Titulo,long Valor,string Fonte);
public record DashboardAtalhoDto(string Titulo,string Url);
