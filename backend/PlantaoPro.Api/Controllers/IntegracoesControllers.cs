using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Security;

namespace PlantaoPro.Api.Controllers;

// R103 (integracoes - WhatsApp opt-in + e-mail transacional): gestao do opt-in por paciente e
// envio manual com registro na caixa de saida. Mesma audiencia clinica dos modulos assistenciais
// (roles + gate de modulo SAUDE360 contratado).

[ApiController]
[Authorize(Roles = RolesConstants.Saude360Assistencial)]
[Saude360Module]
[Route("api/mensageria")]
[Tags("Mensageria")]
public sealed class MensageriaController : ControllerBase
{
    private readonly MensageriaService _service;

    public MensageriaController(MensageriaService service) => _service = service;

    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpPost("opt-in")]
    public async Task<IActionResult> OptIn([FromBody] OptInPacienteRequest request)
    {
        var result = await _service.OptInAsync(request, Ip());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("enviar")]
    public async Task<IActionResult> Enviar([FromBody] MensagemEnviarRequest request)
    {
        var result = await _service.EnviarAsync(request, Ip());
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("outbox")]
    public async Task<IActionResult> Outbox([FromQuery] int limite = 50) => Ok(await _service.ListarOutboxAsync(limite));

    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo() => Ok(await _service.ResumoAsync());
}

// R103 (integracoes - API publica com chaves): leitura autenticada exclusivamente por X-Api-Key
// (esquema ApiChave), com escopo e rate limit por rota via EscopoChave. Nunca aceita JWT.

[ApiController]
[Authorize(AuthenticationSchemes = Security.ApiKeyAuthenticationHandler.SchemeName)]
[Route("api/publico")]
[Tags("API pública")]
public sealed class PublicoController : ControllerBase
{
    private readonly IntegracoesPublicaService _service;

    public PublicoController(IntegracoesPublicaService service) => _service = service;

    [HttpGet("identidade")]
    [EscopoChave("")]
    public IActionResult Identidade() => Ok(_service.IdentidadeAsync(User));

    [HttpGet("agendamentos/{id:guid}/situacao")]
    [EscopoChave("agendamentos:read")]
    public async Task<IActionResult> SituacaoAgendamento(Guid id)
    {
        var result = await _service.SituacaoAgendamentoAsync(User, id);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("pacientes/{id:guid}")]
    [EscopoChave("pacientes:read")]
    public async Task<IActionResult> Paciente(Guid id)
    {
        var result = await _service.PacienteAsync(User, id);
        return StatusCode(result.StatusCode, result);
    }
}
