using System.Diagnostics;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/mobile")]
[Tags("Mobile")]
public class MobileController : ControllerBase
{
    private readonly IConfiguration _cfg;
    private readonly AuthService _auth;
    private readonly MedicoAreaService _medicoArea;
    private readonly NotificacaoService _notificacao;
    private readonly ILogger<MobileController> _logger;

    public MobileController(IConfiguration cfg, AuthService auth, MedicoAreaService medicoArea, NotificacaoService notificacao, ILogger<MobileController> logger)
    {
        _cfg = cfg;
        _auth = auth;
        _medicoArea = medicoArea;
        _notificacao = notificacao;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var uidClaim = User.FindFirst("uid")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(uidClaim, out var uid) ? uid : Guid.Empty;
    }

    private string GetPerfil() => User.FindFirst("perfil")?.Value ?? "desconhecido";
    private string GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";

    [AllowAnonymous]
    [HttpPost("auth/login")]
    public async Task<IActionResult> Login([FromBody] MobileLoginRequestDto request)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await _auth.LoginAsync(new LoginRequest(request.Email, request.Senha), GetIp(), Request.Headers.UserAgent.ToString());
            if (!response.Success || response.Data is null)
            {
                _logger.LogWarning("Mobile login bloqueado email:{Email} ip:{Ip} duracaoMs:{Duracao}", request.Email, GetIp(), sw.ElapsedMilliseconds);
                return StatusCode(response.StatusCode, ApiResponse<MobileLoginResponseDto>.Fail(response.Message, response.StatusCode));
            }

            var payload = new MobileLoginResponseDto(response.Data.Token, response.Data.RefreshToken, response.Data.ExpiresAtUtc, response.Data.Roles ?? Array.Empty<string>());
            _logger.LogInformation("Mobile login sucesso uid:{Uid} perfil:{Perfil} ip:{Ip} duracaoMs:{Duracao}", response.Data.UserId, string.Join(',', payload.Roles), GetIp(), sw.ElapsedMilliseconds);
            return Ok(ApiResponse<MobileLoginResponseDto>.Ok(payload, "Login realizado com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile login erro ip:{Ip} duracaoMs:{Duracao}", GetIp(), sw.ElapsedMilliseconds);
            return StatusCode(500, ApiResponse<MobileLoginResponseDto>.Fail("Não foi possível autenticar no momento.", 500));
        }
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var sw = Stopwatch.StartNew();
        var uid = GetUserId();
        try
        {
            var response = await _medicoArea.ResumoAsync(uid);
            _logger.LogInformation("Mobile dashboard sucesso uid:{Uid} perfil:{Perfil} ip:{Ip} duracaoMs:{Duracao}", uid, GetPerfil(), GetIp(), sw.ElapsedMilliseconds);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile dashboard erro uid:{Uid} ip:{Ip} duracaoMs:{Duracao}", uid, GetIp(), sw.ElapsedMilliseconds);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível carregar dashboard.", 500));
        }
    }

    [HttpGet("plantoes-disponiveis")]
    public async Task<IActionResult> PlantoesDisponiveis([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var sw = Stopwatch.StartNew();
        var uid = GetUserId();
        try
        {
            var response = await _medicoArea.PlantoesDisponiveisAsync(uid, page, pageSize);
            _logger.LogInformation("Mobile plantoes disponiveis uid:{Uid} perfil:{Perfil} ip:{Ip} page:{Page} size:{Size} duracaoMs:{Duracao}", uid, GetPerfil(), GetIp(), page, pageSize, sw.ElapsedMilliseconds);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile plantoes disponiveis erro uid:{Uid} ip:{Ip} duracaoMs:{Duracao}", uid, GetIp(), sw.ElapsedMilliseconds);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível listar plantões disponíveis.", 500));
        }
    }

    [HttpGet("minhas-escalas")]
    public async Task<IActionResult> MinhasEscalas([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var uid = GetUserId();
        try { var response = await _medicoArea.MinhasEscalasAsync(uid, page, pageSize); return StatusCode(response.StatusCode, response); }
        catch (Exception ex) { _logger.LogError(ex, "Mobile escalas erro uid:{Uid}", uid); return StatusCode(500, ApiResponse<object>.Fail("Não foi possível listar escalas.", 500)); }
    }

    [HttpGet("meus-pagamentos")]
    public async Task<IActionResult> MeusPagamentos([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var uid = GetUserId();
        try { var response = await _medicoArea.MeusPagamentosAsync(uid, page, pageSize); return StatusCode(response.StatusCode, response); }
        catch (Exception ex) { _logger.LogError(ex, "Mobile pagamentos erro uid:{Uid}", uid); return StatusCode(500, ApiResponse<object>.Fail("Não foi possível listar pagamentos.", 500)); }
    }

    [HttpGet("notificacoes")]
    public async Task<IActionResult> Notificacoes([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var uid = GetUserId();
        try { var response = await _notificacao.ListarAsync(uid, new NotificationFilterRequest(null, null, null, null, page, pageSize)); return StatusCode(response.StatusCode, response); }
        catch (Exception ex) { _logger.LogError(ex, "Mobile notificacoes erro uid:{Uid}", uid); return StatusCode(500, ApiResponse<object>.Fail("Não foi possível listar notificações.", 500)); }
    }

    [HttpGet("notificacoes/contador")]
    public async Task<IActionResult> ContadorNotificacoes()
    {
        var uid = GetUserId();
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var total = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.notificacoes where usuario_id=@uid and reg_status='A' and coalesce(lida,false)=false", new { uid });
            return Ok(ApiResponse<object>.Ok(new { total }, "Contador carregado."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile contador notificacoes erro uid:{Uid}", uid);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível carregar contador de notificações.", 500));
        }
    }
}
