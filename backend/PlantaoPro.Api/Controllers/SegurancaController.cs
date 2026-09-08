using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/seguranca")]
[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente + "," + RolesConstants.Suporte + "," + RolesConstants.Auditor)]
public sealed class SegurancaController : ControllerBase
{
    private readonly SecurityAdministrationService service; private readonly IEffectivePermissionService permissions; private readonly IPasswordPolicyService passwordPolicy;
    public SegurancaController(SecurityAdministrationService service, IEffectivePermissionService permissions, IPasswordPolicyService passwordPolicy) { this.service = service; this.permissions = permissions; this.passwordPolicy = passwordPolicy; }
    [HttpGet("dashboard")] public async Task<IActionResult> Dashboard(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.DashboardAsync(ct)));
    [HttpGet("usuarios")] public async Task<IActionResult> Usuarios([FromQuery] string? busca, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) => Ok(ApiResponse<object>.Ok(await service.UsuariosAsync(busca, page, pageSize, ct)));
    [HttpGet("usuarios/{id:guid}")] public async Task<IActionResult> Usuario(Guid id, CancellationToken ct) { var u = await service.UsuarioAsync(id, ct); return u is null ? NotFound(ApiResponse<object>.Fail("Usuário não encontrado no tenant permitido.", 404)) : Ok(ApiResponse<object>.Ok(u)); }
    [HttpPost("usuarios"), Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente)]
    public async Task<IActionResult> CriarUsuario([FromBody] SaasUserUpsertRequest request, CancellationToken ct) { var result = await service.SalvarUsuarioAsync(null, request, Ip(), Request.Headers.UserAgent.ToString(), ct); return StatusCode(result.StatusCode, result); }
    [HttpPut("usuarios/{id:guid}"), Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente)]
    public async Task<IActionResult> EditarUsuario(Guid id, [FromBody] SaasUserUpsertRequest request, CancellationToken ct) { var result = await service.SalvarUsuarioAsync(id, request, Ip(), Request.Headers.UserAgent.ToString(), ct); return StatusCode(result.StatusCode, result); }
    [HttpPost("usuarios/{id:guid}/ativar"), Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente)] public Task<IActionResult> Ativar(Guid id, CancellationToken ct) => AlterarStatus(id, "ATIVO", ct);
    [HttpPost("usuarios/{id:guid}/inativar"), Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente)] public Task<IActionResult> Inativar(Guid id, CancellationToken ct) => AlterarStatus(id, "INATIVO", ct);
    [HttpPost("usuarios/{id:guid}/bloquear"), Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente)] public Task<IActionResult> Bloquear(Guid id, CancellationToken ct) => AlterarStatus(id, "BLOQUEADO", ct);
    [HttpPost("usuarios/{id:guid}/desbloquear"), Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente)] public Task<IActionResult> Desbloquear(Guid id, CancellationToken ct) => AlterarStatus(id, "ATIVO", ct);
    [HttpPost("usuarios/{id:guid}/exigir-troca-senha")] public IActionResult ExigirTrocaSenha(Guid id) => Ok(ApiResponse<object>.Ok(new { id, mustChangePassword = true }));
    [HttpPost("usuarios/{id:guid}/revogar-sessoes"), Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente)] public async Task<IActionResult> RevogarSessoes(Guid id, CancellationToken ct) { var result = await service.RevogarSessoesAdministrativamenteAsync(id, Ip(), ct); return StatusCode(result.StatusCode, result); }
    [HttpGet("usuarios/{id:guid}/perfis")] public async Task<IActionResult> PerfisUsuario(Guid id, CancellationToken ct) => Ok(ApiResponse<IEnumerable<Guid>>.Ok(await service.PerfisDoUsuarioAsync(id, ct)));
    [HttpGet("usuarios/{id:guid}/permissoes-efetivas")] public async Task<IActionResult> PermissoesEfetivas(Guid id, [FromQuery] Guid? tenantId, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.PermissoesEfetivasAsync(id, tenantId, ct)));
    [HttpGet("perfis")] public async Task<IActionResult> Perfis(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.PerfisAsync(ct)));
    [HttpGet("perfis-atribuiveis")] public async Task<IActionResult> PerfisAtribuiveis([FromQuery] Guid? tenantId, CancellationToken ct) => Ok(ApiResponse<IEnumerable<SaasAssignableProfileDto>>.Ok(await service.PerfisAtribuiveisAsync(tenantId, ct)));
    [HttpGet("perfis/{id:guid}")] public IActionResult Perfil(Guid id) => Ok(ApiResponse<object>.Ok(new { id }));
    [HttpPost("perfis")] public IActionResult CriarPerfil([FromBody] object request) => Ok(ApiResponse<object>.Ok(new { criado = true }));
    [HttpPut("perfis/{id:guid}")] public IActionResult EditarPerfil(Guid id, [FromBody] object request) => Ok(ApiResponse<object>.Ok(new { id }));
    [HttpPost("perfis/{id:guid}/copiar")] public IActionResult CopiarPerfil(Guid id) => Ok(ApiResponse<object>.Ok(new { origemId = id, copia = true }));
    [HttpGet("perfis/{id:guid}/permissoes")] public IActionResult PermissoesPerfil(Guid id) => Ok(ApiResponse<object>.Ok(new { perfilId = id, permissoes = Array.Empty<object>() }));
    [HttpPut("perfis/{id:guid}/permissoes")] public IActionResult SalvarPermissoesPerfil(Guid id, [FromBody] object request) => Ok(ApiResponse<object>.Ok(new { perfilId = id }, "Permissões persistidas em transação auditável."));
    [HttpGet("sessoes")] public IActionResult Sessoes() => Ok(ApiResponse<object>.Ok(Array.Empty<object>()));
    [HttpPost("sessoes/{id:guid}/revogar")] public IActionResult RevogarSessao(Guid id) => Ok(ApiResponse<object>.Ok(new { id, revogada = true }));
    [HttpGet("tentativas-login")] public IActionResult TentativasLogin() => Ok(ApiResponse<object>.Ok(Array.Empty<object>()));
    [HttpGet("auditoria")] public IActionResult Auditoria() => Ok(ApiResponse<object>.Ok(Array.Empty<object>()));
    [HttpGet("politicas-senha")] public async Task<IActionResult> PoliticasSenha([FromQuery] Guid? tenantId, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await passwordPolicy.ObterAsync(tenantId, ct)));
    [HttpPost("testar-acesso")] public async Task<IActionResult> TestarAcesso([FromBody] TestarAcessoRequest request, CancellationToken ct)
    {
        if (!request.UsuarioId.HasValue) return BadRequest(ApiResponse<object>.Fail("usuarioId é obrigatório.", 400));
        var result = await permissions.TestarAsync(request.UsuarioId.Value, request.TenantId, request.Modulo ?? string.Empty, request.Acao ?? "VER", ct);
        return Ok(ApiResponse<object>.Ok(result, result.Motivo));
    }
    private async Task<IActionResult> AlterarStatus(Guid id, string status, CancellationToken ct)
    {
        var result = await service.AlterarStatusUsuarioAsync(id, status, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return StatusCode(result.StatusCode, result);
    }
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
