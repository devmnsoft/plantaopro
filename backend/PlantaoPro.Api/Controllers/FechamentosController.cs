using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Fechamentos;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController, Route("api/fechamentos")]
[Authorize(Roles = RolesConstants.EscalasGestao + "," + RolesConstants.FinanceiroGestao)]
public sealed class FechamentosController : ControllerBase
{
    private readonly FechamentoOperacionalService service; private readonly ILogger<FechamentosController> logger;
    public FechamentosController(FechamentoOperacionalService service, ILogger<FechamentosController> logger) { this.service=service; this.logger=logger; }

    [HttpGet] public Task<IActionResult> Listar(CancellationToken ct)=>Execute(()=>service.ListarAsync(false,ct));
    [HttpGet("pendentes")] public Task<IActionResult> Pendentes(CancellationToken ct)=>Execute(()=>service.ListarAsync(true,ct));
    [HttpGet("{id:guid}")] public Task<IActionResult> Obter(Guid id,CancellationToken ct)=>Execute(()=>service.ObterAsync(id,ct));
    [HttpGet("{id:guid}/timeline")] public Task<IActionResult> Timeline(Guid id,CancellationToken ct)=>Execute(()=>service.TimelineAsync(id,ct));

    [HttpPost("plantao/{plantaoId:guid}/gerar"), Authorize(Roles=RolesConstants.EscalasGestao)]
    public Task<IActionResult> Gerar(Guid plantaoId,CancellationToken ct)=>Execute(()=>service.GerarAsync(plantaoId,Ip(),Request.Headers.UserAgent,ct),201);
    [HttpPost("{id:guid}/iniciar-conferencia"), Authorize(Roles=RolesConstants.EscalasGestao)]
    public Task<IActionResult> Iniciar(Guid id,CancellationToken ct)=>Execute(()=>service.IniciarConferenciaAsync(id,ct));
    [HttpPost("{id:guid}/concluir-conferencia"), Authorize(Roles=RolesConstants.EscalasGestao)]
    public Task<IActionResult> Concluir(Guid id,CancellationToken ct)=>Execute(()=>service.ConcluirConferenciaAsync(id,ct));
    [HttpPost("{id:guid}/divergencias"), Authorize(Roles=RolesConstants.EscalasGestao)]
    public Task<IActionResult> Divergencia(Guid id,[FromBody]CriarDivergenciaRequest request,CancellationToken ct)=>Execute(()=>service.CriarDivergenciaAsync(id,request,ct),201);
    [HttpPost("{id:guid}/divergencias/{divergenciaId:guid}/resolver"), Authorize(Roles=RolesConstants.EscalasGestao+","+RolesConstants.FinanceiroGestao)]
    public Task<IActionResult> Resolver(Guid id,Guid divergenciaId,[FromBody]ResolverDivergenciaRequest request,CancellationToken ct)=>Execute(()=>service.ResolverDivergenciaAsync(id,divergenciaId,request.Resolucao,ct));
    [HttpPost("{id:guid}/aprovar"), Authorize(Roles=RolesConstants.Administrador+","+RolesConstants.AdministradorGlobal+","+RolesConstants.AdministradorCliente)]
    public Task<IActionResult> Aprovar(Guid id,CancellationToken ct)=>Execute(()=>service.AprovarAsync(id,ct));
    [HttpPost("{id:guid}/devolver"), Authorize(Roles=RolesConstants.Administrador+","+RolesConstants.AdministradorGlobal+","+RolesConstants.AdministradorCliente)]
    public Task<IActionResult> Devolver(Guid id,[FromBody]DevolverFechamentoRequest request,CancellationToken ct)=>Execute(()=>service.DevolverAsync(id,request.Motivo,ct));
    [HttpPost("{id:guid}/gerar-financeiro"), Authorize(Roles=RolesConstants.FinanceiroGestao)]
    public Task<IActionResult> Financeiro(Guid id,CancellationToken ct)=>Execute(()=>service.GerarFinanceiroAsync(id,ct));

    private string? Ip()=>HttpContext.Connection.RemoteIpAddress?.ToString();
    private async Task<IActionResult> Execute<T>(Func<Task<ApiResponse<T>>> action,int success=200)
    { try { var result=await action(); return StatusCode(result.Success?success:result.StatusCode,result); } catch(UnauthorizedAccessException){return Unauthorized(ApiResponse<object>.Fail("Contexto de tenant inválido.",401));} catch(Exception ex){logger.LogError(ex,"Falha não tratada em fechamento operacional");return StatusCode(500,ApiResponse<object>.Fail("Não foi possível processar o fechamento.",500));} }
}
