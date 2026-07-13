using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/developer")]
[Tags("Developer Portal")]
public sealed class DeveloperController : ControllerBase
{
    private readonly B2BLaunchService _service;
    public DeveloperController(B2BLaunchService service) => _service = service;

    [HttpGet("overview")]
    public async Task<IActionResult> Overview() => Ok(await _service.DeveloperOverviewAsync());

    [HttpGet("auth")]
    public async Task<IActionResult> Auth() => Ok(await _service.ListarAsync("developer-auth"));

    [HttpGet("endpoints")]
    public async Task<IActionResult> Endpoints() => Ok(await _service.ListarAsync("developer-endpoints"));

    [HttpGet("rate-limits")]
    public async Task<IActionResult> RateLimits() => Ok(await _service.ListarAsync("developer-rate-limits"));

    [HttpGet("webhooks")]
    public async Task<IActionResult> Webhooks() => Ok(await _service.ListarAsync("developer-webhooks"));

    [HttpGet("uso")]
    public async Task<IActionResult> Uso() => Ok(await _service.ListarAsync("developer-uso"));

    [HttpGet("exemplos")]
    public async Task<IActionResult> Exemplos() => Ok(await _service.ListarAsync("developer-exemplos"));

    [HttpGet("escopos")]
    public async Task<IActionResult> Escopos() => Ok(await _service.ListarEscoposAsync());

    [HttpGet("api-keys")]
    public async Task<IActionResult> ApiKeys() => Ok(await _service.ListarApiKeysAsync());

    [HttpPost("api-keys")]
    public async Task<IActionResult> CriarApiKey([FromBody] ApiKeyCreateRequest request)
    {
        var result = await _service.CriarApiKeyAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("api-keys/{id:guid}/revogar")]
    public async Task<IActionResult> Revogar(Guid id)
    {
        var result = await _service.RevogarApiKeyAsync(id, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("api-keys/{id:guid}/rotacionar")]
    public async Task<IActionResult> Rotacionar(Guid id)
    {
        var result = await _service.RotacionarApiKeyAsync(id, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("api-keys/{id:guid}/logs")]
    public async Task<IActionResult> Logs(Guid id) => Ok(await _service.ListarApiKeyLogsAsync(id));
}

[ApiController]
[Authorize]
[Route("api/contratos")]
[Tags("Contratos e SLA")]
public sealed class ContratosController : ControllerBase
{
    private readonly B2BLaunchService _service;
    public ContratosController(B2BLaunchService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await _service.ListarAsync("contratos"));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detalhar(Guid id) => Ok(await _service.ListarAsync("contratos"));

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] ContratoRequest request)
    {
        var result = await _service.CriarItemAuditavelAsync("CONTRATO", request.Numero, request.SlaResumo, "NORMAL", HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, [FromBody] ContratoRequest request)
    {
        if (id == Guid.Empty) return BadRequest(ApiResponse<string>.Fail("Contrato inválido.", 400));
        var result = await _service.CriarItemAuditavelAsync("CONTRATO", request.Numero, request.SlaResumo, "NORMAL", HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{id:guid}/ativar")]
    public Task<IActionResult> Ativar(Guid id) => Status(id, "ATIVAR");

    [HttpPost("{id:guid}/encerrar")]
    public Task<IActionResult> Encerrar(Guid id) => Status(id, "ENCERRAR");

    [HttpPost("{id:guid}/renovar")]
    public Task<IActionResult> Renovar(Guid id) => Status(id, "RENOVAR");

    [HttpGet("{id:guid}/sla")]
    public async Task<IActionResult> Sla(Guid id) => Ok(await _service.ListarAsync("contratos"));

    private async Task<IActionResult> Status(Guid id, string acao)
    {
        var result = await _service.AlterarStatusAuditavelAsync("CONTRATO", id, acao, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }
}

[ApiController]
[Authorize]
[Route("api/sla")]
[Tags("SLA")]
public sealed class SlaController : ControllerBase
{
    private readonly B2BLaunchService _service;
    public SlaController(B2BLaunchService service) => _service = service;

    [HttpGet("indicadores")]
    public async Task<IActionResult> Indicadores() => Ok(await _service.ListarAsync("sla-indicadores"));

    [HttpGet("incidentes")]
    public async Task<IActionResult> Incidentes() => Ok(await _service.ListarAsync("sla-incidentes"));

    [HttpPost("incidentes")]
    public async Task<IActionResult> CriarIncidente([FromBody] SlaIncidenteRequest request)
    {
        var result = await _service.CriarItemAuditavelAsync("SLA_INCIDENTE", request.Titulo, request.Descricao, request.Severidade, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("incidentes/{id:guid}/resolver")]
    public async Task<IActionResult> Resolver(Guid id)
    {
        var result = await _service.AlterarStatusAuditavelAsync("SLA_INCIDENTE", id, "RESOLVER", HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }
}

[ApiController]
[Authorize]
[Route("api/suporte")]
[Tags("Suporte B2B")]
public sealed class SuporteController : ControllerBase
{
    private readonly B2BLaunchService _service;
    public SuporteController(B2BLaunchService service) => _service = service;

    [HttpGet("chamados")]
    public async Task<IActionResult> Chamados() => Ok(await _service.ListarAsync("suporte"));

    [HttpGet("chamados/{id:guid}")]
    public async Task<IActionResult> Detalhar(Guid id) => Ok(await _service.ListarAsync("suporte"));

    [HttpPost("chamados")]
    public async Task<IActionResult> Criar([FromBody] SuporteChamadoRequest request)
    {
        var result = await _service.CriarItemAuditavelAsync("SUPORTE_CHAMADO", request.Assunto, request.Descricao, request.Prioridade, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("chamados/{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, [FromBody] SuporteChamadoRequest request)
    {
        var result = await _service.CriarItemAuditavelAsync("SUPORTE_CHAMADO", request.Assunto, request.Descricao, request.Prioridade, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("chamados/{id:guid}/responder")]
    public Task<IActionResult> Responder(Guid id, [FromBody] SuporteInteracaoRequest request) => Status(id, "RESPONDER");

    [HttpPost("chamados/{id:guid}/escalar")]
    public Task<IActionResult> Escalar(Guid id) => Status(id, "ESCALAR");

    [HttpPost("chamados/{id:guid}/resolver")]
    public Task<IActionResult> Resolver(Guid id, [FromBody] SuporteInteracaoRequest request) => Status(id, "RESOLVER");

    [HttpGet("base-conhecimento")]
    public async Task<IActionResult> BaseConhecimento() => Ok(await _service.ListarAsync("suporte-base"));

    [HttpGet("sla")]
    public async Task<IActionResult> Sla() => Ok(await _service.ListarAsync("suporte-sla"));

    private async Task<IActionResult> Status(Guid id, string acao)
    {
        var result = await _service.AlterarStatusAuditavelAsync("SUPORTE_CHAMADO", id, acao, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }
}

[ApiController]
[Authorize]
[Route("api/beta")]
[Tags("Soft launch beta")]
public sealed class BetaController : ControllerBase
{
    private readonly B2BLaunchService _service;
    public BetaController(B2BLaunchService service) => _service = service;

    [HttpGet("programas")]
    public async Task<IActionResult> Programas() => Ok(await _service.ListarAsync("beta-programas"));

    [HttpPost("programas")]
    public async Task<IActionResult> CriarPrograma([FromBody] GoToMarketMaterialRequest request)
    {
        var result = await _service.CriarItemAuditavelAsync("BETA_PROGRAMA", request.Nome, request.Conteudo, request.Tipo, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("clientes")]
    public async Task<IActionResult> Clientes() => Ok(await _service.ListarAsync("beta-clientes"));

    [HttpPost("clientes")]
    public async Task<IActionResult> CriarCliente([FromBody] GoToMarketMaterialRequest request)
    {
        var result = await _service.CriarItemAuditavelAsync("BETA_CLIENTE", request.Nome, request.Conteudo, request.Tipo, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("feedbacks")]
    public async Task<IActionResult> Feedbacks() => Ok(await _service.ListarAsync("beta-feedbacks"));

    [HttpPost("feedbacks")]
    public async Task<IActionResult> CriarFeedback([FromBody] BetaFeedbackRequest request)
    {
        var result = await _service.CriarItemAuditavelAsync("BETA_FEEDBACK", request.Titulo, request.Descricao, request.Severidade, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("feedbacks/{id:guid}/classificar")]
    public Task<IActionResult> Classificar(Guid id) => Status(id, "CLASSIFICAR");

    [HttpPost("feedbacks/{id:guid}/resolver")]
    public Task<IActionResult> Resolver(Guid id) => Status(id, "RESOLVER");

    [HttpGet("indicadores")]
    public async Task<IActionResult> Indicadores() => Ok(await _service.ListarAsync("beta-indicadores"));

    private async Task<IActionResult> Status(Guid id, string acao)
    {
        var result = await _service.AlterarStatusAuditavelAsync("BETA_FEEDBACK", id, acao, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }
}

[ApiController]
[Authorize]
[Route("api/monitoramento")]
[Tags("Monitoramento ativo")]
public sealed class MonitoramentoController : ControllerBase
{
    private readonly B2BLaunchService _service;
    public MonitoramentoController(B2BLaunchService service) => _service = service;

    [HttpGet("health")]
    public IActionResult Health() => Ok(ApiResponse<object>.Ok(new { status = "online", timestampUtc = DateTime.UtcNow }));

    [HttpGet("tenants")]
    public async Task<IActionResult> Tenants() => Ok(await _service.ListarAsync("monitoramento-tenants"));

    [HttpGet("performance")]
    public async Task<IActionResult> Performance() => Ok(await _service.ListarAsync("monitoramento-performance"));

    [HttpGet("erros")]
    public async Task<IActionResult> Erros() => Ok(await _service.ListarAsync("monitoramento-erros"));

    [HttpGet("alertas")]
    public async Task<IActionResult> Alertas() => Ok(await _service.ListarAsync("monitoramento"));

    [HttpPost("alertas/{id:guid}/reconhecer")]
    public async Task<IActionResult> Reconhecer(Guid id)
    {
        var result = await _service.AlterarStatusAuditavelAsync("TELEMETRIA_ALERTA", id, "RECONHECER", HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("alertas/{id:guid}/resolver")]
    public async Task<IActionResult> Resolver(Guid id)
    {
        var result = await _service.AlterarStatusAuditavelAsync("TELEMETRIA_ALERTA", id, "RESOLVER", HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }
}

[ApiController]
[Authorize]
[Route("api/gotomarket")]
[Tags("Go-to-market B2B")]
public sealed class GoToMarketController : ControllerBase
{
    private readonly B2BLaunchService _service;
    public GoToMarketController(B2BLaunchService service) => _service = service;

    [HttpGet("casos-uso")]
    public async Task<IActionResult> CasosUso() => Ok(await _service.ListarAsync("gotomarket"));

    [HttpPost("casos-uso")]
    public Task<IActionResult> CriarCaso([FromBody] GoToMarketMaterialRequest request) => Criar("GTM_CASO_USO", request);

    [HttpGet("materiais")]
    public async Task<IActionResult> Materiais() => Ok(await _service.ListarAsync("gotomarket"));

    [HttpPost("materiais")]
    public Task<IActionResult> CriarMaterial([FromBody] GoToMarketMaterialRequest request) => Criar("GTM_MATERIAL", request);

    [HttpGet("campanhas")]
    public async Task<IActionResult> Campanhas() => Ok(await _service.ListarAsync("gotomarket"));

    [HttpPost("campanhas")]
    public Task<IActionResult> CriarCampanha([FromBody] GoToMarketMaterialRequest request) => Criar("GTM_CAMPANHA", request);

    [HttpGet("decisores")]
    public async Task<IActionResult> Decisores() => Ok(await _service.ListarAsync("gotomarket"));

    [HttpPost("decisores")]
    public Task<IActionResult> CriarDecisor([FromBody] GoToMarketMaterialRequest request) => Criar("GTM_DECISOR", request);

    private async Task<IActionResult> Criar(string entidade, GoToMarketMaterialRequest request)
    {
        var result = await _service.CriarItemAuditavelAsync(entidade, request.Nome, request.Conteudo, request.Tipo, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }
}

[ApiController]
[Authorize]
[Route("api/operacional-launch")]
[Tags("Evolução operacional")]
public sealed class OperacionalLaunchController : ControllerBase
{
    private readonly B2BLaunchService _service;
    public OperacionalLaunchController(B2BLaunchService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Index() => Ok(await _service.OperacionalAsync());
}
