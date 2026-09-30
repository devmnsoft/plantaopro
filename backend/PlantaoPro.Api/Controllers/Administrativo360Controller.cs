using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Administrativo360;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Api.Controllers;

[ApiController, Authorize(Roles=RolesConstants.CadastrosCoordenacao), Route("api/administrativo360")]
public sealed class Administrativo360Controller : ControllerBase
{
    private readonly Administrativo360Service service;
    public Administrativo360Controller(Administrativo360Service service)=>this.service=service;

    // ---- Resumo organizacional ----
    [HttpGet("resumo")] public async Task<IActionResult> Resumo(CancellationToken ct)=>await LerAsync(() => service.ResumoAsync(ct));

    // ---- Departamentos ----
    [HttpGet("departamentos")] public async Task<IActionResult> Departamentos([FromQuery] int pagina = 1, [FromQuery] int tamanho = 50, [FromQuery] string? busca = null, [FromQuery] bool somenteAtivos = false, CancellationToken ct = default)=>await LerAsync(() => service.DepartamentosAsync(pagina, tamanho, busca, somenteAtivos, ct));
    [HttpPost("departamentos")] public async Task<IActionResult> Departamento(DepartamentoRequest request,CancellationToken ct)=>await LerAsync(() => service.CriarDepartamentoAsync(request,ct),System.Net.HttpStatusCode.Created);
    [HttpPut("departamentos/{id:guid}")] public async Task<IActionResult> AtualizarDepartamento(Guid id,DepartamentoRequest request,CancellationToken ct)=>await LerAsync(() => service.AtualizarDepartamentoAsync(id,request,ct));
    [HttpPost("departamentos/{id:guid}/inativar")] public async Task<IActionResult> InativarDepartamento(Guid id,CancellationToken ct)=>await ExecutarAsync(() => service.AlterarAtivacaoDepartamentoAsync(id,false,ct));
    [HttpPost("departamentos/{id:guid}/ativar")] public async Task<IActionResult> AtivarDepartamento(Guid id,CancellationToken ct)=>await ExecutarAsync(() => service.AlterarAtivacaoDepartamentoAsync(id,true,ct));

    // ---- Cargos ----
    [HttpGet("cargos")] public async Task<IActionResult> Cargos([FromQuery] int pagina = 1, [FromQuery] int tamanho = 50, [FromQuery] string? busca = null, [FromQuery] bool somenteAtivos = false, CancellationToken ct = default)=>await LerAsync(() => service.CargosAsync(pagina, tamanho, busca, somenteAtivos, ct));
    [HttpPost("cargos")] public async Task<IActionResult> Cargo(CargoRequest request,CancellationToken ct)=>await LerAsync(() => service.CriarCargoAsync(request,ct),System.Net.HttpStatusCode.Created);
    [HttpPut("cargos/{id:guid}")] public async Task<IActionResult> AtualizarCargo(Guid id,CargoRequest request,CancellationToken ct)=>await LerAsync(() => service.AtualizarCargoAsync(id,request,ct));
    [HttpPost("cargos/{id:guid}/inativar")] public async Task<IActionResult> InativarCargo(Guid id,CancellationToken ct)=>await ExecutarAsync(() => service.AlterarAtivacaoCargoAsync(id,false,ct));
    [HttpPost("cargos/{id:guid}/ativar")] public async Task<IActionResult> AtivarCargo(Guid id,CancellationToken ct)=>await ExecutarAsync(() => service.AlterarAtivacaoCargoAsync(id,true,ct));

    // ---- Colaboradores ----
    [HttpGet("colaboradores")] public async Task<IActionResult> Colaboradores([FromQuery] int pagina = 1, [FromQuery] int tamanho = 50, [FromQuery] string? busca = null, [FromQuery] string? status = null, CancellationToken ct = default)=>await LerAsync(() => service.ColaboradoresAsync(pagina, tamanho, busca, status, ct));
    [HttpPost("colaboradores")] public async Task<IActionResult> Colaborador(ColaboradorRequest request,CancellationToken ct)=>await LerAsync(() => service.CriarColaboradorAsync(request,ct),System.Net.HttpStatusCode.Created);
    [HttpPut("colaboradores/{id:guid}")] public async Task<IActionResult> AtualizarColaborador(Guid id,ColaboradorUpdateRequest request,CancellationToken ct)=>await LerAsync(() => service.AtualizarColaboradorAsync(id,request,ct));
    [HttpPost("colaboradores/{id:guid}/status")] public async Task<IActionResult> StatusColaborador(Guid id,[FromBody] ColaboradorStatusRequest request,CancellationToken ct)=>await ExecutarAsync(() => service.AlterarStatusColaboradorAsync(id,request.Status,ct));

    // ---- Contratos de trabalho ----
    [HttpGet("contratos")] public async Task<IActionResult> Contratos([FromQuery] int pagina = 1, [FromQuery] int tamanho = 50, [FromQuery] string? busca = null, [FromQuery] string? status = null, CancellationToken ct = default)=>await LerAsync(() => service.ContratosAsync(pagina, tamanho, busca, status, ct));
    [HttpGet("contratos/{id:guid}")] public async Task<IActionResult> ContratoDetalhe(Guid id,CancellationToken ct)=>await LerAsync(() => service.ContratoDetalheAsync(id,ct));
    [HttpPost("contratos")] public async Task<IActionResult> Contrato(ContratoTrabalhoRequest request,CancellationToken ct)=>await LerAsync(() => service.ContratarAsync(request,ct),System.Net.HttpStatusCode.Created);
    [HttpPost("contratos/{id:guid}/encerrar")] public async Task<IActionResult> EncerrarContrato(Guid id,[FromBody] EncerrarContratoRequest request,CancellationToken ct)=>await ExecutarAsync(() => service.EncerrarContratoAsync(id,request.Fim,ct));
    [HttpPost("contratos/{id:guid}/cancelar")] public async Task<IActionResult> CancelarContrato(Guid id,CancellationToken ct)=>await ExecutarAsync(() => service.CancelarContratoAsync(id,ct));

    // ============================================================
    // Erro de negócio -> HTTP com mensagem amigável ("message"):
    // 400 regra de negócio violada; 403 acesso negado no domínio.
    // A Web (BaseWebController) lê esse campo e exibe sem detalhes técnicos.
    // Falhas técnicas não passam por aqui: o filter de exceção/handler global
    // responde 404/500 com log estruturado e correlação.
    // ============================================================
    private static Task<IActionResult> LerAsync<T>(Func<Task<T>> acao,System.Net.HttpStatusCode ok=System.Net.HttpStatusCode.OK)=>ComNegocioAsync(async () =>
    {
        var valor=await acao();
        return new ObjectResult(valor){StatusCode=(int)ok};
    });
    private static Task<IActionResult> ExecutarAsync(Func<Task> acao)=>ComNegocioAsync(async () =>
    {
        await acao();
        return new OkObjectResult(new { ok=true });
    });
    private static async Task<IActionResult> ComNegocioAsync(Func<Task<IActionResult>> acao)
    {
        try { return await acao(); }
        catch(Administrativo360BusinessException ex) { return new BadRequestObjectResult(new { message=ex.Message }); }
        catch(UnauthorizedAccessException ex) { return new JsonResult(new { message=string.IsNullOrWhiteSpace(ex.Message)?"Acesso não autorizado.":ex.Message }) { StatusCode=StatusCodes.Status403Forbidden }; }
    }
}
