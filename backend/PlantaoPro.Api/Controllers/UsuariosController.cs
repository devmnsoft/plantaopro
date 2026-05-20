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

    public UsuariosController(IConfiguration configuration, IAuditService auditService)
    {
        _configuration = configuration;
        _auditService = auditService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var uid = GetUserId();
        if (uid == Guid.Empty) return Unauthorized(ApiResponse<object>.Fail("Usuário não autenticado.", 401));

        await using var cn = new NpgsqlConnection(_configuration.GetConnectionString("Default"));
        var user = await cn.QueryFirstOrDefaultAsync<UserSettingsDto>("select id,nome,email,telefone,coalesce(preferencias_notificacao,'Email') as PreferenciasNotificacao from plantaopro.usuarios where id=@id and reg_status='A'", new { id = uid });
        return user is null
            ? NotFound(ApiResponse<object>.Fail("Usuário não encontrado.", 404))
            : Ok(ApiResponse<UserSettingsDto>.Ok(user));
    }

    [HttpPut("me")]
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

    private Guid GetUserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;
}

public record UserSettingsDto(Guid Id, string Nome, string Email, string? Telefone, string PreferenciasNotificacao);
public record UpdateUserSettingsRequest(string Nome, string Email, string? Telefone, string PreferenciasNotificacao);
public record AlterarSenhaRequest(string SenhaAtual, string NovaSenha);
