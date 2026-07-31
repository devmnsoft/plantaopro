using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Clinical;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController, Authorize, Route("api/consultas")]
public sealed class ConsultasWorkspaceController : ControllerBase
{
    private readonly IConsultaApplicationService service; private readonly IConsultaRepository repository; private readonly ICurrentUserService user;
    public ConsultasWorkspaceController(IConsultaApplicationService service, IConsultaRepository repository, ICurrentUserService user) { this.service = service; this.repository = repository; this.user = user; }
    private Guid? Tenant => user.ClienteId ?? user.TenantId;
    private IActionResult Result<T>(ApiResponse<T> result) => StatusCode(result.StatusCode, result);
    [HttpGet("fila-medica"), Authorize(Policy = "Consulta.Iniciar")]
    public async Task<IActionResult> Fila([FromQuery] Guid? unidadeId, [FromQuery] Guid? medicoId, [FromQuery] int pagina = 1, [FromQuery] int tamanho = 30, CancellationToken ct = default) => Result(await service.FilaAsync(unidadeId, medicoId, pagina, tamanho, ct));
    [HttpPost("{id:guid}/iniciar"), Authorize(Policy = "Consulta.Iniciar")]
    public async Task<IActionResult> Iniciar(Guid id, IniciarConsultaRequest request, CancellationToken ct) => Result(await service.IniciarAsync(id, request, ct));
    [HttpGet("{id:guid}/workspace"), Authorize(Policy = "Consulta.VerDadosSensiveis")]
    public async Task<IActionResult> Workspace(Guid id, CancellationToken ct) => Result(await service.WorkspaceAsync(id, ct));
    [HttpPut("{id:guid}/rascunho"), Authorize(Policy = "Consulta.Editar")]
    public async Task<IActionResult> Rascunho(Guid id, SalvarConsultaRascunhoRequest request, CancellationToken ct) => Result(await service.SalvarRascunhoAsync(id, request, ct));
    [HttpGet("{id:guid}/pendencias-finalizacao"), Authorize(Policy = "Consulta.Finalizar")]
    public async Task<IActionResult> Pendencias(Guid id, CancellationToken ct) => Result(await service.PendenciasAsync(id, ct));
    [HttpPost("{id:guid}/finalizar"), Authorize(Policy = "Consulta.Finalizar")]
    public async Task<IActionResult> Finalizar(Guid id, FinalizarConsultaRequest request, CancellationToken ct) => Result(await service.FinalizarAsync(id, request, ct));
    [HttpGet("{id:guid}/cids"), Authorize(Policy = "Consulta.VerDadosSensiveis")]
    public async Task<IActionResult> Cids(Guid id, CancellationToken ct) => Tenant is not Guid t ? Result(ApiResponse<IReadOnlyList<ConsultaCid>>.Fail("Tenant obrigatório.", 403)) : Result(ApiResponse<IReadOnlyList<ConsultaCid>>.Ok(await repository.ListarCidsAsync(id, t, ct)));
    [HttpPost("{id:guid}/cids"), Authorize(Policy = "CID.Vincular")]
    public async Task<IActionResult> AdicionarCid(Guid id, AdicionarConsultaCidRequest request, CancellationToken ct) { if (Tenant is not Guid t) return Result(ApiResponse<ConsultaCid>.Fail("Tenant obrigatório.", 403)); var item = await repository.AdicionarCidAsync(id, t, request, user.UserId, ct); return item is null ? Result(ApiResponse<ConsultaCid>.Fail("CID inativo, duplicado ou consulta inválida.", 409)) : Result(ApiResponse<ConsultaCid>.Ok(item)); }
    [HttpDelete("{id:guid}/cids/{consultaCidId:guid}"), Authorize(Policy = "CID.Remover")]
    public async Task<IActionResult> RemoverCid(Guid id, Guid consultaCidId, CancellationToken ct) { if (Tenant is not Guid t) return Result(ApiResponse<bool>.Fail("Tenant obrigatório.", 403)); var ok = await repository.RemoverCidAsync(id, consultaCidId, t, user.UserId, ct); return Result(ok ? ApiResponse<bool>.Ok(true) : ApiResponse<bool>.Fail("CID vinculado não encontrado.", 404)); }
}
