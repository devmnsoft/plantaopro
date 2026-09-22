using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using PlantaoPro.Api.Security;

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
        public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken cancellationToken)
        {
            var suppliedCorrelationId = Request.Headers["X-Correlation-ID"].FirstOrDefault();
            var correlationId = !string.IsNullOrWhiteSpace(suppliedCorrelationId) && suppliedCorrelationId.Length <= 128
                ? suppliedCorrelationId
                : HttpContext.TraceIdentifier;
            Response.Headers["X-Correlation-ID"] = correlationId;
            using var loginScope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
            var identifierKind = LoginIdentifierNormalizer.Classify(req.Email);
            var auditIdentifier = LoginIdentifierNormalizer.AuditValue(req.Email, identifierKind);
            try
            {
                var r = await _service.LoginAsync(req, ip, Request.Headers.UserAgent.ToString(), cancellationToken);
                var perfil = r.Data?.Roles is { Length: > 0 } ? string.Join(',', r.Data.Roles) : "sem-perfil";
                await _auditService.RegistrarAsync(
                    r.Data?.UsuarioId,
                    r.Data?.ClienteId,
                    AuditoriaConstants.Entidades.Usuario,
                    r.Data?.UsuarioId,
                    r.Success ? AuditoriaConstants.Acoes.LoginSucesso : AuditoriaConstants.Acoes.LoginFalha,
                    new { identifier = auditIdentifier, identifierKind, statusCode = r.StatusCode },
                    r.Success,
                    ip,
                    perfil,
                    cancellationToken);
                _logger.LogInformation("Login processado Identificador:{Identificador} Tipo:{Tipo} IP:{Ip} Status:{Status} Perfil:{Perfil} DataHoraUtc:{DataHoraUtc}", auditIdentifier, identifierKind, ip, r.StatusCode, perfil, DateTime.UtcNow);
                return StatusCode(r.StatusCode, r);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Login API cancelado pelo solicitante. Identificador:{Identificador}", auditIdentifier);
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha inesperada no login Identificador:{Identificador} Tipo:{Tipo} IP:{Ip}", auditIdentifier, identifierKind, ip);
                await _auditService.RegistrarAsync(null, null, AuditoriaConstants.Entidades.Usuario, null, AuditoriaConstants.Acoes.LoginFalha, new { identifier = auditIdentifier, identifierKind, motivo = "erro_interno" }, false, ip, "sem-perfil");
                return StatusCode(500, ApiResponse<object>.Fail("Não foi possível processar o login no momento.", 500));
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
        {
            await using var cn = new NpgsqlConnection(_configuration.GetConnectionString("Default"));
            var normalizedEmail = (req.Email ?? string.Empty).Trim().ToLowerInvariant();
            var usuario = await cn.QueryFirstOrDefaultAsync<(Guid Id, string Nome)>("select id,nome from plantaopro.usuarios where lower(email)=@email and reg_status='A'", new { email = normalizedEmail });

            if (usuario.Id != Guid.Empty)
            {
                var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
                await cn.ExecuteAsync("insert into plantaopro.recuperacao_senha(id,usuario_id,token_hash,expiracao,utilizado,reg_date) values(gen_random_uuid(),@u,@h,now()+interval '30 minutes',false,now())", new { u = usuario.Id, h = tokenHash });
                await _auditService.LogAsync(usuario.Id, "PASSWORD_FORGOT", "usuarios", usuario.Id, "Solicitação de recuperação de senha", ip: HttpContext.Connection.RemoteIpAddress?.ToString(), userAgent: Request.Headers.UserAgent.ToString());
                _logger.LogInformation("Recuperação de senha aceita e token protegido persistido. UsuarioId:{UsuarioId} Entrega:PENDENTE_SEM_PROVEDOR", usuario.Id);
            }
            else
                _logger.LogInformation("Recuperação de senha aceita sem conta elegível. Identificador:{Identificador}", LoginIdentifierNormalizer.AuditValue(normalizedEmail, LoginIdentifierKind.Email));

            return Ok(ApiResponse<object>.Ok(new { }, "Se o e-mail estiver cadastrado, enviaremos instruções para recuperação."));
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Token) ||
                string.IsNullOrWhiteSpace(req.NovaSenha) || req.NovaSenha.Length < 12)
                return BadRequest(ApiResponse<object>.Fail("Solicitação de redefinição inválida. A nova senha deve ter pelo menos 12 caracteres."));

            await using var cn = new NpgsqlConnection(_configuration.GetConnectionString("Default"));
            var normalizedEmail = req.Email.Trim().ToLowerInvariant();
            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(req.Token.Trim())));
            var row = await cn.QueryFirstOrDefaultAsync<(Guid UsuarioId, bool Utilizado, DateTime Expiracao)>(@"select rs.usuario_id,rs.utilizado,rs.expiracao
                from plantaopro.recuperacao_senha rs
                join plantaopro.usuarios u on u.id=rs.usuario_id
                where lower(u.email)=@email and rs.token_hash=@tokenHash and u.reg_status='A'
                order by rs.reg_date desc limit 1", new { email = normalizedEmail, tokenHash });

            if (row.UsuarioId == Guid.Empty || row.Utilizado || row.Expiracao < DateTime.UtcNow)
            {
                _logger.LogWarning("Redefinição de senha recusada. Identificador:{Identificador}", LoginIdentifierNormalizer.AuditValue(normalizedEmail, LoginIdentifierKind.Email));
                return BadRequest(ApiResponse<object>.Fail("Token inválido ou expirado."));
            }

            var hash = PasswordHashService.Hash(req.NovaSenha);
            await cn.OpenAsync();
            await using var transaction = await cn.BeginTransactionAsync();
            await cn.ExecuteAsync("update plantaopro.usuarios set senha_hash=@h,reg_update=now() where id=@id", new { h = hash, id = row.UsuarioId }, transaction);
            await cn.ExecuteAsync("update plantaopro.recuperacao_senha set utilizado=true where usuario_id=@id and utilizado=false", new { id = row.UsuarioId }, transaction);
            await cn.ExecuteAsync("update plantaopro.auth_sessoes set revogada_em=now(),motivo_revogacao='PASSWORD_RESET',reg_update=now() where usuario_id=@id and revogada_em is null", new { id = row.UsuarioId }, transaction);
            await transaction.CommitAsync();
            await _auditService.LogAsync(row.UsuarioId, "PASSWORD_RESET", "usuarios", row.UsuarioId, "Senha redefinida", ip: HttpContext.Connection.RemoteIpAddress?.ToString(), userAgent: Request.Headers.UserAgent.ToString());
            _logger.LogInformation("Senha redefinida e sessões anteriores revogadas. UsuarioId:{UsuarioId}", row.UsuarioId);

            return Ok(ApiResponse<object>.Ok(new { }, "Senha redefinida com sucesso."));
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromServices] IAuthenticationSessionService sessions, CancellationToken ct)
        {
            var revoked = await sessions.RevokeAsync(User, "LOGOUT", ct);
            if (!revoked)
                return Unauthorized(ApiResponse<object>.Fail("Sessão inválida ou já encerrada.", 401));

            return Ok(ApiResponse<object>.Ok(new { revoked = true }, "Sessão encerrada com sucesso."));
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("refresh-context")]
        public async Task<IActionResult> RefreshContext(CancellationToken ct)
        {
            var uidClaim = User.FindFirst("uid")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(uidClaim, out var uid))
                return Unauthorized(ApiResponse<object>.Fail("Sessão inválida.", 401));

            var r = await _service.RefreshContextAsync(uid, ct);
            return StatusCode(r.StatusCode, r);
        }
    }

    public record ForgotPasswordRequest(string Email);
    public record ResetPasswordRequest(string Email, string Token, string NovaSenha);
}
