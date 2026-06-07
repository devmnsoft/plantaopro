using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using System.Security.Claims;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IAuditService _auditService;
    private readonly UserService _userService;
    private readonly ILogger<UsuariosController> _logger;

    public UsuariosController(IConfiguration configuration, IAuditService auditService, UserService userService, ILogger<UsuariosController> logger)
    {
        _configuration = configuration;
        _auditService = auditService;
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = RolesConstants.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserListVM>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUsers()
    {
        var users = await _userService.ListAsync();
        return Ok(ApiResponse<IEnumerable<UserListVM>>.Ok(users));
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UsuarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe()
    {
        var uid = GetUserId();
        if (uid == Guid.Empty) return Unauthorized(ApiResponse<object>.Fail("Usuário não autenticado.", 401));

        var requestDateUtc = DateTime.UtcNow;
        var requestIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var requestEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        await using var cn = new NpgsqlConnection(_configuration.GetConnectionString("Default"));
        var user = await cn.QueryFirstOrDefaultAsync<UsuarioDto>(@"
            select
                u.id,
                u.email,
                u.nome,
                u.telefone,
                coalesce(string_agg(distinct p.nome, ','), '') as Perfil,
                coalesce(u.preferencias_notificacao, '{}') as PreferenciasNotificacao
            from plantaopro.usuarios u
            left join plantaopro.usuarios_perfis up on up.usuario_id = u.id and up.reg_status = 'A'
            left join plantaopro.perfis p on p.id = up.perfil_id and p.reg_status = 'A'
            where u.id = @id and u.reg_status = 'A'
            group by u.id, u.email, u.nome, u.telefone, u.preferencias_notificacao", new { id = uid });

        if (user is null)
        {
            _logger.LogWarning("GET /api/usuarios/me não encontrado UsuarioId:{UsuarioId} Email:{Email} IP:{Ip} Status:{Status} DataHoraUtc:{DataHoraUtc}", uid, requestEmail, requestIp, 404, requestDateUtc);
            return NotFound(ApiResponse<object>.Fail("Usuário não encontrado.", 404));
        }

        var dto = user with
        {
            Perfil = string.IsNullOrWhiteSpace(user.Perfil) ? "SEM_PERFIL" : user.Perfil,
            PreferenciasNotificacao = string.IsNullOrWhiteSpace(user.PreferenciasNotificacao) ? "{}" : user.PreferenciasNotificacao
        };

        _logger.LogInformation("GET /api/usuarios/me sucesso UsuarioId:{UsuarioId} Email:{Email} Perfil:{Perfil} IP:{Ip} Status:{Status} DataHoraUtc:{DataHoraUtc}", dto.Id, dto.Email, dto.Perfil, requestIp, 200, requestDateUtc);
        return Ok(ApiResponse<UsuarioDto>.Ok(dto));
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserSettingsRequest req)
    {
        var uid = GetUserId();
        await using var cn = new NpgsqlConnection(_configuration.GetConnectionString("Default"));
        var affected = await cn.ExecuteAsync("update plantaopro.usuarios set nome=@Nome,email=@Email,telefone=@Telefone,preferencias_notificacao=@PreferenciasNotificacao,reg_update=now() where id=@id and reg_status='A'", new { id = uid, req.Nome, req.Email, req.Telefone, req.PreferenciasNotificacao });
        if (affected == 0) return NotFound(ApiResponse<object>.Fail("Usuário não encontrado.", 404));
        await _auditService.LogAsync(uid, "USUARIO_UPDATE", "usuarios", uid, "Atualização de dados do usuário", ip: HttpContext.Connection.RemoteIpAddress?.ToString(), userAgent: Request.Headers.UserAgent.ToString());
        return Ok(ApiResponse<object>.Ok(new { }, "Dados atualizados com sucesso."));
    }

    [HttpPost("me/alterar-senha")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaRequest req)
    {
        var uid = GetUserId();
        await using var cn = new NpgsqlConnection(_configuration.GetConnectionString("Default"));
        var user = await cn.QueryFirstOrDefaultAsync<(Guid Id, string SenhaHash)>("select id,senha_hash as SenhaHash from plantaopro.usuarios where id=@id and reg_status='A'", new { id = uid });
        if (user.Id == Guid.Empty) return NotFound(ApiResponse<object>.Fail("Usuário não encontrado.", 404));
        if (!BCrypt.Net.BCrypt.Verify(req.SenhaAtual, user.SenhaHash)) return BadRequest(ApiResponse<object>.Fail("Senha atual inválida."));

        var newHash = BCrypt.Net.BCrypt.HashPassword(req.NovaSenha);
        await cn.ExecuteAsync("update plantaopro.usuarios set senha_hash=@h,reg_update=now() where id=@id", new { h = newHash, id = uid });
        await _auditService.LogAsync(uid, "USUARIO_ALTERAR_SENHA", "usuarios", uid, "Alteração de senha do usuário", ip: HttpContext.Connection.RemoteIpAddress?.ToString(), userAgent: Request.Headers.UserAgent.ToString());
        return Ok(ApiResponse<object>.Ok(new { }, "Senha alterada com sucesso."));
    }

    [HttpPost("unlock/{id:guid}")]
    [Authorize(Roles = RolesConstants.Administrador + ",ADMINISTRATOR")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unlock(Guid id)
    {
        var adminId = GetUserId();
        if (adminId == Guid.Empty) return Unauthorized(ApiResponse<object>.Fail("Usuário não autenticado.", 401));

        var isAdministrator =
            User.IsInRole(RolesConstants.Administrador) ||
            User.IsInRole("ADMINISTRATOR");

        if (!isAdministrator) throw new UnauthorizedAccessException("Somente administradores podem desbloquear usuários.");

        var ok = await _userService.UnlockUserAsync(id, adminId, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
        if (!ok) return NotFound(ApiResponse<object>.Fail("Usuário não encontrado.", 404));
        return Ok(ApiResponse<object>.Ok(new { message = "Usuário desbloqueado com sucesso" }, "Usuário desbloqueado com sucesso"));
    }

    private Guid GetUserId()
    {
        if (Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid)) return uid;
        if (Guid.TryParse(User.FindFirstValue("uid"), out uid)) return uid;
        if (Guid.TryParse(User.FindFirstValue(ClaimTypes.Name), out uid)) return uid;
        return Guid.Empty;
    }
}

// Alteração: DTO atualizado com Perfil e PreferenciasNotificacao null-safe para o endpoint /api/usuarios/me.
public record UsuarioDto(Guid Id, string Email, string Nome, string? Telefone, string Perfil = "SEM_PERFIL", string PreferenciasNotificacao = "{}");
public record UpdateUserSettingsRequest(string Nome, string Email, string? Telefone, string PreferenciasNotificacao);
public record AlterarSenhaRequest(string SenhaAtual, string NovaSenha);
