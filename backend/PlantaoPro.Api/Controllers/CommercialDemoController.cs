using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public")]
[Tags("Comercial público")]
public sealed class PublicCommercialController : ControllerBase
{
    private readonly CommercialDemoService _service;
    private readonly ILogger<PublicCommercialController> _logger;
    public PublicCommercialController(CommercialDemoService service, ILogger<PublicCommercialController> logger) { _service = service; _logger = logger; }

    [HttpGet("landing")] public async Task<IActionResult> Landing() { var r = await _service.LandingAsync(); return StatusCode(r.StatusCode, r); }
    [HttpGet("faq")] public async Task<IActionResult> Faq() { var r = await _service.FaqAsync(); return StatusCode(r.StatusCode, r); }
    [HttpGet("casos-uso")] public async Task<IActionResult> CasosUso() { var r = await _service.UseCasesAsync(); return StatusCode(r.StatusCode, r); }
    [HttpPost("lead")] public async Task<IActionResult> Lead([FromBody] PublicLeadRequest request) { try { var r = await _service.RegisterLeadAsync(request, Ip()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { _logger.LogError(ex, "Erro no POST lead público"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível registrar o lead.", 500)); } }
    [HttpPost("agendar-demo")] public async Task<IActionResult> AgendarDemo([FromBody] DemoScheduleRequest request) { try { var r = await _service.ScheduleDemoAsync(request, Ip()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { _logger.LogError(ex, "Erro no POST agendar demo"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível agendar demonstração.", 500)); } }
    [HttpGet("simulador/perguntas")] public async Task<IActionResult> Perguntas() { var r = await _service.SimulatorQuestionsAsync(); return StatusCode(r.StatusCode, r); }
    [HttpPost("simulador/calcular")] public async Task<IActionResult> Calcular([FromBody] PlanSimulatorRequest request) { try { var r = await _service.CalculatePlanAsync(request, Ip()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { _logger.LogError(ex, "Erro no POST simulador"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível calcular o plano ideal.", 500)); } }
    [HttpPost("simulador/gerar-lead")] public async Task<IActionResult> GerarLead([FromBody] PublicLeadRequest request) { try { var r = await _service.LeadFromSimulationAsync(request, Ip()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { _logger.LogError(ex, "Erro no POST lead do simulador"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível gerar lead.", 500)); } }
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}

[ApiController]
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,PARCEIRO")]
[Route("api/propostas-comerciais")]
[Tags("Propostas comerciais")]
public sealed class PropostasComerciaisController : ControllerBase
{
    private readonly CommercialDemoService _service;
    private readonly ILogger<PropostasComerciaisController> _logger;
    public PropostasComerciaisController(CommercialDemoService service, ILogger<PropostasComerciaisController> logger) { _service = service; _logger = logger; }
    [HttpGet] public async Task<IActionResult> Listar() { var r = await _service.ListProposalsAsync(); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> Obter(Guid id) { var r = await _service.GetProposalAsync(id); return StatusCode(r.StatusCode, r); }
    [HttpPost] public async Task<IActionResult> Criar([FromBody] CommercialProposalRequest request) { try { var r = await _service.SaveProposalAsync(null, request, IsGlobalAdmin(), Ip()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { _logger.LogError(ex, "Erro no POST proposta"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível criar proposta.", 500)); } }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Atualizar(Guid id, [FromBody] CommercialProposalRequest request) { try { var r = await _service.SaveProposalAsync(id, request, IsGlobalAdmin(), Ip()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { _logger.LogError(ex, "Erro no PUT proposta {Id}", id); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível atualizar proposta.", 500)); } }
    [HttpPost("{id:guid}/gerar-itens")] public async Task<IActionResult> GerarItens(Guid id) { var r = await _service.GenerateItemsAsync(id, Ip()); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/enviar")] public async Task<IActionResult> Enviar(Guid id) { var r = await _service.ChangeProposalStatusAsync(id, "ENVIADA", null, Ip()); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/aprovar")] public async Task<IActionResult> Aprovar(Guid id) { var r = await _service.ChangeProposalStatusAsync(id, "APROVADA", null, Ip()); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/recusar")] public async Task<IActionResult> Recusar(Guid id, [FromBody] RejectProposalRequest request) { var r = await _service.ChangeProposalStatusAsync(id, "RECUSADA", request.Motivo, Ip()); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/converter-em-cliente")] public async Task<IActionResult> Converter(Guid id) { var r = await _service.ConvertProposalAsync(id, Ip()); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}/preview")] public async Task<IActionResult> Preview(Guid id) { var r = await _service.PreviewProposalAsync(id); return StatusCode(r.StatusCode, r); }
    private bool IsGlobalAdmin() => User.IsInRole("ADMINISTRADOR_GLOBAL");
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}

[ApiController]
[Authorize(Roles = "ADMINISTRADOR_GLOBAL")]
[Route("api/admin-saas")]
[Tags("Admin SaaS")]
public sealed class AdminSaasApiController : ControllerBase
{
    private readonly CommercialDemoService _service;
    public AdminSaasApiController(CommercialDemoService service) { _service = service; }
    [HttpGet("resumo")] public async Task<IActionResult> Resumo() { var r = await _service.AdminSaasResumoAsync(); return StatusCode(r.StatusCode, r); }
    [HttpGet("clientes")] public async Task<IActionResult> Clientes() => await Resumo();
    [HttpGet("receita")] public async Task<IActionResult> Receita() => await Resumo();
    [HttpGet("vendas")] public async Task<IActionResult> Vendas() => await Resumo();
    [HttpGet("implantacoes")] public async Task<IActionResult> Implantacoes() => await Resumo();
    [HttpGet("alertas")] public async Task<IActionResult> Alertas() => await Resumo();
}

[ApiController]
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR")]
[Route("api/cliente-portal")]
[Tags("Portal cliente")]
public sealed class ClientePortalApiController : ControllerBase
{
    private readonly CommercialDemoService _service;
    public ClientePortalApiController(CommercialDemoService service) { _service = service; }
    [HttpGet("resumo")] public async Task<IActionResult> Resumo() { var r = await _service.ClientePortalResumoAsync(null); return StatusCode(r.StatusCode, r); }
    [HttpGet("meu-plano")] public async Task<IActionResult> MeuPlano() => await Resumo();
    [HttpGet("uso")] public async Task<IActionResult> Uso() => await Resumo();
    [HttpGet("faturas")] public async Task<IActionResult> Faturas() => await Resumo();
    [HttpGet("onboarding")] public async Task<IActionResult> Onboarding() => await Resumo();
    [HttpGet("alertas")] public async Task<IActionResult> Alertas() => await Resumo();
}

[ApiController]
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,PARCEIRO")]
[Route("api/parceiro-portal")]
[Tags("Portal parceiro")]
public sealed class ParceiroPortalApiController : ControllerBase
{
    private readonly CommercialDemoService _service;
    public ParceiroPortalApiController(CommercialDemoService service) { _service = service; }
    [HttpGet("resumo")] public async Task<IActionResult> Resumo() { var r = await _service.ParceiroPortalResumoAsync(); return StatusCode(r.StatusCode, r); }
    [HttpGet("leads")] public async Task<IActionResult> Leads() => await Resumo();
    [HttpGet("clientes")] public async Task<IActionResult> Clientes() => await Resumo();
    [HttpGet("comissoes")] public async Task<IActionResult> Comissoes() => await Resumo();
    [HttpGet("repasses")] public async Task<IActionResult> Repasses() => await Resumo();
    [HttpGet("materiais")] public async Task<IActionResult> Materiais() => await Resumo();
}

[ApiController]
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR")]
[Route("api/modulos")]
[Tags("Governança de módulos")]
public sealed class ModulosApiController : ControllerBase
{
    private readonly CommercialDemoService _service;
    private readonly ILogger<ModulosApiController> _logger;
    public ModulosApiController(CommercialDemoService service, ILogger<ModulosApiController> logger) { _service = service; _logger = logger; }
    [HttpGet] public async Task<IActionResult> Listar() { var r = await _service.ListModulesAsync(); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> Obter(Guid id) { var r = await _service.GetModuleAsync(id); return StatusCode(r.StatusCode, r); }
    [HttpPost] public async Task<IActionResult> Criar([FromBody] UpsertModuleRequest request) { try { var r = await _service.SaveModuleAsync(null, request, Ip()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { _logger.LogError(ex, "Erro ao criar módulo"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível criar módulo.", 500)); } }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Atualizar(Guid id, [FromBody] UpsertModuleRequest request) { try { var r = await _service.SaveModuleAsync(id, request, Ip()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { _logger.LogError(ex, "Erro ao atualizar módulo {Id}", id); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível atualizar módulo.", 500)); } }
    [HttpPost("{id:guid}/habilitar-tenant")] public async Task<IActionResult> Habilitar(Guid id, [FromBody] TenantModuleRequest request) { var r = await _service.ToggleModuleForTenantAsync(id, request, true, Ip()); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/desabilitar-tenant")] public async Task<IActionResult> Desabilitar(Guid id, [FromBody] TenantModuleRequest request) { var r = await _service.ToggleModuleForTenantAsync(id, request, false, Ip()); return StatusCode(r.StatusCode, r); }
    [HttpGet("tenant/{tenantId:guid}")] public async Task<IActionResult> Tenant(Guid tenantId) { var r = await _service.ListModulesAsync(); return StatusCode(r.StatusCode, r); }
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}

[ApiController]
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR")]
[Route("api/feature-flags")]
[Tags("Feature flags")]
public sealed class FeatureFlagsApiController : ControllerBase
{
    private readonly CommercialDemoService _service;
    public FeatureFlagsApiController(CommercialDemoService service) { _service = service; }
    [HttpGet] public async Task<IActionResult> Listar() { var r = await _service.ListFeatureFlagsAsync(); return StatusCode(r.StatusCode, r); }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Atualizar(Guid id, [FromBody] UpdateFeatureFlagRequest request) { var r = await _service.UpdateFeatureFlagAsync(id, request, HttpContext.Connection.RemoteIpAddress?.ToString()); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize(Roles = "ADMINISTRADOR_GLOBAL")]
[Route("api/demo")]
[Tags("Demo comercial")]
public sealed class DemoApiController : ControllerBase
{
    private readonly CommercialDemoService _service;
    public DemoApiController(CommercialDemoService service) { _service = service; }
    [HttpPost("gerar-dados")] public async Task<IActionResult> Gerar() { var r = await _service.GenerateDemoDataAsync(Ip()); return StatusCode(r.StatusCode, r); }
    [HttpPost("limpar-dados")] public async Task<IActionResult> Limpar() { var r = await _service.ClearDemoDataAsync(Ip()); return StatusCode(r.StatusCode, r); }
    [HttpGet("status")] public async Task<IActionResult> Status() { var r = await _service.DemoStatusAsync(); return StatusCode(r.StatusCode, r); }
    [HttpGet("roteiros")] public async Task<IActionResult> Roteiros() { var r = await _service.DemoRoutesAsync(); return StatusCode(r.StatusCode, r); }
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
