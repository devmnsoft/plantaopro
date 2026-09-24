using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Administrativo360;

namespace PlantaoPro.Api.Controllers;

[ApiController, Authorize(Roles=RolesConstants.CadastrosCoordenacao), Route("api/administrativo360")]
public sealed class Administrativo360Controller : ControllerBase
{
    private readonly Administrativo360Service service;
    public Administrativo360Controller(Administrativo360Service service)=>this.service=service;
    [HttpGet("resumo")] public async Task<IActionResult> Resumo(CancellationToken ct)=>Ok(await service.ResumoAsync(ct));
    [HttpGet("departamentos")] public async Task<IActionResult> Departamentos(CancellationToken ct)=>Ok(await service.DepartamentosAsync(ct));
    [HttpPost("departamentos")] public async Task<IActionResult> Departamento(DepartamentoRequest request,CancellationToken ct)=>Created("api/administrativo360/departamentos",await service.CriarDepartamentoAsync(request,ct));
    [HttpGet("cargos")] public async Task<IActionResult> Cargos(CancellationToken ct)=>Ok(await service.CargosAsync(ct));
    [HttpPost("cargos")] public async Task<IActionResult> Cargo(CargoRequest request,CancellationToken ct)=>Created("api/administrativo360/cargos",await service.CriarCargoAsync(request,ct));
    [HttpGet("colaboradores")] public async Task<IActionResult> Colaboradores(CancellationToken ct)=>Ok(await service.ColaboradoresAsync(ct));
    [HttpPost("colaboradores")] public async Task<IActionResult> Colaborador(ColaboradorRequest request,CancellationToken ct)=>Created("api/administrativo360/colaboradores",await service.CriarColaboradorAsync(request,ct));
    [HttpGet("contratos")] public async Task<IActionResult> Contratos(CancellationToken ct)=>Ok(await service.ContratosAsync(ct));
    [HttpPost("contratos")] public async Task<IActionResult> Contratar(ContratoTrabalhoRequest request,CancellationToken ct)=>Created("api/administrativo360/contratos",await service.ContratarAsync(request,ct));
}
