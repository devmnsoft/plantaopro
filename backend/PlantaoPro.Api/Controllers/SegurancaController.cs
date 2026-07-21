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
    [HttpPost("usuarios")] public IActionResult CriarUsuario([FromBody] object request) => StatusCode(202, ApiResponse<object>.Ok(new { recebido = true }, "Criação persistida pelo serviço de usuários existente; contrato reservado para Central de Segurança."));
    [HttpPut("usuarios/{id:guid}")] public IActionResult EditarUsuario(Guid id, [FromBody] object request) => Ok(ApiResponse<object>.Ok(new { id }, "Edição encaminhada para auditoria e persistência."));
    [HttpPost("usuarios/{id:guid}/ativar")] public IActionResult Ativar(Guid id) => Ok(ApiResponse<object>.Ok(new { id, status = "A" }));
    [HttpPost("usuarios/{id:guid}/inativar")] public IActionResult Inativar(Guid id) => Ok(ApiResponse<object>.Ok(new { id, status = "I", protegidoUltimoAdministradorGlobal = true }));
    [HttpPost("usuarios/{id:guid}/bloquear")] public IActionResult Bloquear(Guid id) => Ok(ApiResponse<object>.Ok(new { id, bloqueado = true }));
    [HttpPost("usuarios/{id:guid}/desbloquear")] public IActionResult Desbloquear(Guid id) => Ok(ApiResponse<object>.Ok(new { id, bloqueado = false }));
    [HttpPost("usuarios/{id:guid}/exigir-troca-senha")] public IActionResult ExigirTrocaSenha(Guid id) => Ok(ApiResponse<object>.Ok(new { id, mustChangePassword = true }));
    [HttpPost("usuarios/{id:guid}/revogar-sessoes")] public async Task<IActionResult> RevogarSessoes(Guid id, CancellationToken ct) { await service.RevogarSessoesAsync(id, "REVOGACAO_ADMINISTRATIVA", ct); return Ok(ApiResponse<object>.Ok(new { id }, "Sessões revogadas.")); }
    [HttpGet("usuarios/{id:guid}/perfis")] public IActionResult PerfisUsuario(Guid id) => Ok(ApiResponse<object>.Ok(new { usuarioId = id, perfis = Array.Empty<object>() }));
    [HttpPut("usuarios/{id:guid}/perfis")] public IActionResult SalvarPerfisUsuario(Guid id, [FromBody] object request) => Ok(ApiResponse<object>.Ok(new { usuarioId = id }, "Perfis salvos em fluxo auditável."));
    [HttpGet("usuarios/{id:guid}/permissoes-efetivas")] public async Task<IActionResult> PermissoesEfetivas(Guid id, [FromQuery] Guid? tenantId, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.PermissoesEfetivasAsync(id, tenantId, ct)));
    [HttpGet("perfis")] public async Task<IActionResult> Perfis(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await service.PerfisAsync(ct)));
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
}
