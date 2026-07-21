using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/implantacao")]
public sealed class ImplantacaoController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    public ImplantacaoController(IConfiguration configuration, IWebHostEnvironment environment){ _configuration = configuration; _environment = environment; }

    [HttpGet("diagnostico")]
    public async Task<IActionResult> Diagnostico(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await BuildReport(ct), "Diagnóstico sanitizado de implantação."));

    [HttpGet("prontidao")]
    public async Task<IActionResult> Prontidao(CancellationToken ct)
    {
        var report = await BuildReport(ct);
        return Ok(ApiResponse<object>.Ok(new { classificacao = report.Classificacao, prontidaoPercentual = report.ProntidaoPercentual }, "Prontidão calculada."));
    }

    [HttpGet("etapas")]
    public IActionResult Etapas() => Ok(ApiResponse<object>.Ok(DefaultSteps(), "Etapas da implantação."));

    [HttpPost("etapas/{codigo}/validar")]
    public IActionResult ValidarEtapa(string codigo) => Ok(ApiResponse<object>.Ok(new { codigo, status = "PENDENTE", auditoria = "registrar_em_implantacao_execucoes" }, "Validação solicitada."));

    [HttpPost("validar-tudo")]
    public async Task<IActionResult> ValidarTudo(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await BuildReport(ct), "Validação completa executada."));

    [HttpGet("relatorio")]
    public async Task<IActionResult> Relatorio(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await BuildReport(ct), "Relatório de Go-Live gerado."));

    private async Task<GoLiveReport> BuildReport(CancellationToken ct)
    {
        var checks = new Dictionary<string, string>();
        try
        {
            await using var cn = new NpgsqlConnection(_configuration.GetConnectionString("Default"));
            await cn.OpenAsync(ct);
            checks["postgresql"] = "OK";
            foreach (var table in new[] { "usuarios", "perfis", "implantacao_status", "go_live_checklists", "api_request_logs" })
            {
                await using var cmd = new NpgsqlCommand("select count(*) from information_schema.tables where table_schema='plantaopro' and table_name=@table", cn);
                cmd.Parameters.AddWithValue("table", table);
                checks[table] = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) == 1 ? "OK" : "PENDENTE";
            }
        }
        catch { checks["postgresql"] = "INDISPONIVEL"; }
        var ok = checks.Count(x => x.Value == "OK");
        var pct = checks.Count == 0 ? 0 : decimal.Round(ok * 100m / checks.Count, 2);
        var cls = pct >= 95 ? "PRONTO_PARA_GO_LIVE" : pct >= 75 ? "PRONTO_PARA_HOMOLOGAÇÃO" : pct >= 40 ? "ATENÇÃO" : "PENDENTE";
        return new GoLiveReport(DateTime.UtcNow, "v1.18.8", _environment.EnvironmentName, checks, pct, cls, DefaultSteps());
    }

    private static object[] DefaultSteps() => new object[]
    {
        new { codigo="BANCO", ordem=1, nome="Banco", status="PENDENTE", descricao="PostgreSQL, banco e schema", acaoSugerida="Executar create-database/install", responsavel="DevOps/DBA", linkSeguro="/Implantacao" },
        new { codigo="ADMIN", ordem=2, nome="Administrador", status="PENDENTE", descricao="Bootstrap administrativo", acaoSugerida="Executar bootstrap-admin", responsavel="Suporte", linkSeguro="/Implantacao" },
        new { codigo="GO_LIVE", ordem=17, nome="Go-Live", status="PENDENTE", descricao="Decisão final", acaoSugerida="Gerar relatório", responsavel="PM/Sponsor", linkSeguro="/Implantacao" }
    };
}

public sealed record GoLiveReport(DateTime Data, string Versao, string Ambiente, IDictionary<string,string> Health, decimal ProntidaoPercentual, string Classificacao, object[] Etapas);
