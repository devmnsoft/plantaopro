using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/meu-dia")]
[Authorize]
public sealed class MeuDiaController : ControllerBase
{
    private readonly IMeuDiaService _service;
    public MeuDiaController(IMeuDiaService service) => _service = service;

    [HttpGet]
    public IActionResult Get() => Ok(ApiResponse<MeuDiaDto>.Ok(_service.ObterResumo(User)));
    [HttpGet("indicadores")]
    public IActionResult Indicadores() => Ok(ApiResponse<IEnumerable<MeuDiaIndicadorDto>>.Ok(_service.Indicadores(User)));
    [HttpGet("pendencias")]
    public IActionResult Pendencias() => Ok(ApiResponse<IEnumerable<MeuDiaItemDto>>.Ok(_service.Pendencias(User)));
    [HttpGet("agenda")]
    public IActionResult Agenda() => Ok(ApiResponse<IEnumerable<MeuDiaItemDto>>.Ok(_service.Agenda(User)));
    [HttpGet("alertas")]
    public IActionResult Alertas() => Ok(ApiResponse<IEnumerable<MeuDiaItemDto>>.Ok(_service.Alertas(User)));
    [HttpGet("acoes-rapidas")]
    public IActionResult AcoesRapidas() => Ok(ApiResponse<IEnumerable<MeuDiaItemDto>>.Ok(_service.AcoesRapidas(User)));
    [HttpPost("itens/{id:guid}/concluir")]
    public IActionResult Concluir(Guid id, [FromBody] MeuDiaEstadoRequest request) => Ok(ApiResponse<MeuDiaItemDto>.Ok(_service.AlterarEstado(id, "concluido", request)));
    [HttpPost("itens/{id:guid}/adiar")]
    public IActionResult Adiar(Guid id, [FromBody] MeuDiaEstadoRequest request) => Ok(ApiResponse<MeuDiaItemDto>.Ok(_service.AlterarEstado(id, "adiado", request)));
    [HttpPost("itens/{id:guid}/reabrir")]
    public IActionResult Reabrir(Guid id, [FromBody] MeuDiaEstadoRequest request) => Ok(ApiResponse<MeuDiaItemDto>.Ok(_service.AlterarEstado(id, "aberto", request)));
}
