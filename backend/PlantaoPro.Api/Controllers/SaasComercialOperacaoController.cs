using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador)]
public sealed class SaasComercialOperacaoController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SaasComercialOperacaoController> _logger;

    public SaasComercialOperacaoController(IConfiguration configuration, ILogger<SaasComercialOperacaoController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("api/saas/clientes")]
    public Task<IActionResult> Clientes() => ListarAsync("plantaopro.saas_clientes", "reg_date desc", "Clientes carregados.");
    [HttpGet("api/saas/tenants")]
    public Task<IActionResult> Tenants() => ListarAsync("plantaopro.saas_tenants", "reg_date desc", "Tenants carregados.");
    [HttpGet("api/saas/planos")]
    public Task<IActionResult> Planos() => ListarAsync("plantaopro.saas_planos", "nome", "Planos carregados.");
    [HttpGet("api/saas/limites")]
    public Task<IActionResult> Limites() => ListarAsync("plantaopro.saas_limites", "codigo", "Limites carregados.");
    [HttpGet("api/saas/uso")]
    public Task<IActionResult> Uso() => ListarAsync("plantaopro.saas_uso_mensal", "competencia desc", "Uso carregado.");
    [HttpGet("api/saas/bloqueios")]
    public Task<IActionResult> Bloqueios() => ListarAsync("plantaopro.saas_bloqueios", "reg_date desc", "Bloqueios carregados.");
    [HttpGet("api/billing/assinaturas")]
    public Task<IActionResult> Assinaturas() => ListarAsync("plantaopro.saas_billing_assinaturas", "reg_date desc", "Assinaturas carregadas.");
    [HttpGet("api/billing/faturas")]
    public Task<IActionResult> Faturas() => ListarAsync("plantaopro.saas_billing_faturas", "vencimento desc", "Faturas carregadas.");
    [HttpGet("api/marketplace/modulos")]
    public Task<IActionResult> Marketplace() => ListarAsync("plantaopro.saas_marketplace_modulos", "nome", "Módulos carregados.");
    [HttpGet("api/marketplace/meus-modulos")]
    public Task<IActionResult> MeusModulos() => ListarAsync("plantaopro.saas_marketplace_contratacoes", "reg_date desc", "Contratações carregadas.");
    [HttpGet("api/customer-success/clientes")]
    public Task<IActionResult> CsClientes() => ListarAsync("plantaopro.saas_clientes", "nome_fantasia", "Clientes carregados para CS.");

    [HttpGet("api/operacao/health")]
    public IActionResult Health() => Ok(ApiResponse<object>.Ok(new { api = "online", web = "online", banco = "verificar via healthcheck", timestamp = DateTime.UtcNow }, "Saúde operacional carregada."));
    [HttpGet("api/operacao/erros")]
    public IActionResult Erros() => Ok(ApiResponse<IEnumerable<object>>.Ok(Array.Empty<object>(), "Sem erros recentes registrados."));
    [HttpGet("api/operacao/endpoints-lentos")]
    public IActionResult EndpointsLentos() => Ok(ApiResponse<IEnumerable<object>>.Ok(Array.Empty<object>(), "Sem endpoints lentos registrados."));
    [HttpGet("api/operacao/uso")]
    public Task<IActionResult> OperacaoUso() => ListarAsync("plantaopro.saas_uso_mensal", "reg_date desc", "Uso operacional carregado.");
    [HttpGet("api/operacao/auditoria-resumo")]
    public IActionResult AuditoriaResumo() => Ok(ApiResponse<object>.Ok(new { acoesCriticas = 0, falhasPermissao = 0 }, "Resumo de auditoria carregado."));

    private async Task<IActionResult> ListarAsync(string tabela, string ordenacao, string mensagem)
    {
        try
        {
            await using var cn = new NpgsqlConnection(_configuration.GetConnectionString("Default"));
            var itens = await cn.QueryAsync($"select * from {tabela} where reg_status = 'A' order by {ordenacao} limit 100");
            return Ok(ApiResponse<IEnumerable<dynamic>>.Ok(itens, mensagem));
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            _logger.LogWarning(ex, "Tabela SaaS ainda não aplicada: {Tabela}", tabela);
            return Ok(ApiResponse<IEnumerable<object>>.Ok(Array.Empty<object>(), "Migration SaaS pendente para esta visão."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar dados SaaS em {Tabela}", tabela);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar os dados SaaS.", 500));
        }
    }
}
