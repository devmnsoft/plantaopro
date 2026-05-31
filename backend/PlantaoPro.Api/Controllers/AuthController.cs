using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _service;
        private readonly IConfiguration _configuration;
        private readonly IAuditService _auditService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService service, IConfiguration configuration, IAuditService auditService, ILogger<AuthController> logger)
        {
            _service = service;
            _configuration = configuration;
            _auditService = auditService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
            try
            {
                var r = await _service.LoginAsync(req, ip, Request.Headers.UserAgent.ToString());
                var perfil = r.Data?.Roles is { Length: > 0 } ? string.Join(',', r.Data.Roles) : "sem-perfil";
                await _auditService.RegistrarAsync(
                    r.Data?.UsuarioId,
                    null,
                    AuditoriaConstants.Entidades.Usuario,
                    r.Data?.UsuarioId,
                    r.Success ? AuditoriaConstants.Acoes.LoginSucesso : AuditoriaConstants.Acoes.LoginFalha,
                    new { email = req.Email, statusCode = r.StatusCode },
                    r.Success,
                    ip,
                    perfil);
                _logger.LogInformation("Login processado Email:{Email} IP:{Ip} Status:{Status} Perfil:{Perfil} DataHoraUtc:{DataHoraUtc}", req.Email, ip, r.StatusCode, perfil, DateTime.UtcNow);
                return StatusCode(r.StatusCode, r);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha inesperada no login Email:{Email} IP:{Ip}", req.Email, ip);
                await _auditService.RegistrarAsync(null, null, AuditoriaConstants.Entidades.Usuario, null, AuditoriaConstants.Acoes.LoginFalha, new { email = req.Email, motivo = "erro_interno" }, false, ip, "sem-perfil");
                return StatusCode(500, ApiResponse<object>.Fail("Não foi possível processar o login no momento.", 500));
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
        {
            await using var cn = new NpgsqlConnection(_configuration.GetConnectionString("Default"));
            var usuario = await cn.QueryFirstOrDefaultAsync<(Guid Id, string Nome)>("select id,nome from plantaopro.usuarios where email=@email and reg_status='A'", new { email = req.Email });
            var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

            if (usuario.Id != Guid.Empty)
            {
                await cn.ExecuteAsync(@"create table if not exists plantaopro.recuperacao_senha(
                    id uuid primary key,
                    usuario_id uuid not null,
                    token_hash text not null,
                    expiracao timestamp not null,
                    utilizado boolean not null default false,
                    reg_date timestamp not null default now())");

                await cn.ExecuteAsync("insert into plantaopro.recuperacao_senha(id,usuario_id,token_hash,expiracao,utilizado,reg_date) values(gen_random_uuid(),@u,@h,now()+interval '30 minutes',false,now())", new { u = usuario.Id, h = tokenHash });
                await _auditService.LogAsync(usuario.Id, "PASSWORD_FORGOT", "usuarios", usuario.Id, "Solicitação de recuperação de senha", ip: HttpContext.Connection.RemoteIpAddress?.ToString(), userAgent: Request.Headers.UserAgent.ToString());
            }

            _logger.LogInformation("Solicitação de recuperação de senha para {Email}", req.Email);
            return Ok(ApiResponse<object>.Ok(new { TokenDev = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment() ? rawToken : null }, "Se o e-mail estiver cadastrado, enviaremos instruções para recuperação."));
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            await using var cn = new NpgsqlConnection(_configuration.GetConnectionString("Default"));
            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(req.Token)));
            var row = await cn.QueryFirstOrDefaultAsync<(Guid UsuarioId, bool Utilizado, DateTime Expiracao)>(@"select rs.usuario_id,rs.utilizado,rs.expiracao
                from plantaopro.recuperacao_senha rs
                join plantaopro.usuarios u on u.id=rs.usuario_id
                where u.email=@email and rs.token_hash=@tokenHash
                order by rs.reg_date desc limit 1", new { email = req.Email, tokenHash });

            if (row.UsuarioId == Guid.Empty || row.Utilizado || row.Expiracao < DateTime.UtcNow)
            {
                _logger.LogWarning("Token inválido/expirado para reset de senha {Email}", req.Email);
                return BadRequest(ApiResponse<object>.Fail("Token inválido ou expirado."));
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(req.NovaSenha);
            await cn.ExecuteAsync("update plantaopro.usuarios set senha_hash=@h,reg_update=now() where id=@id", new { h = hash, id = row.UsuarioId });
            await cn.ExecuteAsync("update plantaopro.recuperacao_senha set utilizado=true where usuario_id=@id and token_hash=@tokenHash", new { id = row.UsuarioId, tokenHash });
            await _auditService.LogAsync(row.UsuarioId, "PASSWORD_RESET", "usuarios", row.UsuarioId, "Senha redefinida", ip: HttpContext.Connection.RemoteIpAddress?.ToString(), userAgent: Request.Headers.UserAgent.ToString());
            _logger.LogInformation("Senha redefinida para {Email}", req.Email);

            return Ok(ApiResponse<object>.Ok(new { }, "Senha redefinida com sucesso."));
        }
    }

    public record ForgotPasswordRequest(string Email);
    public record ResetPasswordRequest(string Email, string Token, string NovaSenha);
}
