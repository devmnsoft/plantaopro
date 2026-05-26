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

            var payload = new MobileLoginResponseDto(response.Data.Token, null, response.Data.ExpiresAt, response.Data.Roles ?? Array.Empty<string>());
            _logger.LogInformation("Mobile login sucesso uid:{Uid} perfil:{Perfil} ip:{Ip} duracaoMs:{Duracao}", response.Data.UsuarioId, string.Join(',', payload.Roles), GetIp(), sw.ElapsedMilliseconds);
            return Ok(ApiResponse<MobileLoginResponseDto>.Ok(payload, "Login realizado com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile login erro ip:{Ip} duracaoMs:{Duracao}", GetIp(), sw.ElapsedMilliseconds);
            return StatusCode(500, ApiResponse<MobileLoginResponseDto>.Fail("Não foi possível autenticar no momento.", 500));
        }
    }



    [HttpGet("me")]
    public IActionResult Me()
    {
        var uid = GetUserId();
        if (uid == Guid.Empty) return Unauthorized(ApiResponse<object>.Fail("Sessão inválida.", 401));
        var nome = User.FindFirst("nome")?.Value ?? User.Identity?.Name ?? "Usuário";
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
        return Ok(ApiResponse<object>.Ok(new { id = uid, nome, email, perfil = GetPerfil() }, "Usuário autenticado."));
    }

    [HttpPost("auth/logout")]
    public IActionResult Logout() => Ok(ApiResponse<object>.Ok(new { revoked = false }, "Logout concluído no cliente mobile."));


    [HttpGet("perfil")]
    public IActionResult Perfil()
    {
        var uid = GetUserId();
        if (uid == Guid.Empty) return Unauthorized(ApiResponse<object>.Fail("Sessão inválida.", 401));
        var nome = User.FindFirst("nome")?.Value ?? User.Identity?.Name ?? "Usuário";
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
        return Ok(ApiResponse<object>.Ok(new { nome, email, perfil = GetPerfil() }, "Perfil carregado com sucesso."));
    }

    [HttpPut("perfil")]
    public IActionResult AtualizarPerfil([FromBody] MobilePerfilUpdateRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Nome)) return BadRequest(ApiResponse<object>.Fail("Verifique os campos obrigatórios.", 400));
            return Ok(ApiResponse<object>.Ok(new { request.Nome, request.Telefone }, "Alterações salvas com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile atualizar perfil erro uid:{Uid}", GetUserId());
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível atualizar perfil.", 500));
        }
    }

    [HttpPut("notificacoes/{id:guid}/lida")]
    public async Task<IActionResult> NotificacaoLida(Guid id)
    {
        var uid = GetUserId();
        try { var response = await _notificacao.MarcarLidaAsync(uid, id, GetIp(), Request.Headers.UserAgent.ToString()); return StatusCode(response.StatusCode, response); }
        catch (Exception ex) { _logger.LogError(ex, "Mobile notificacao lida erro uid:{Uid} id:{Id}", uid, id); return StatusCode(500, ApiResponse<object>.Fail("Não foi possível atualizar notificação.", 500)); }
    }

    [HttpPut("notificacoes/lidas")]
    public async Task<IActionResult> NotificacoesLidas()
    {
        var uid = GetUserId();
        try { var response = await _notificacao.MarcarTodasLidasAsync(uid, GetIp(), Request.Headers.UserAgent.ToString()); return StatusCode(response.StatusCode, response); }
        catch (Exception ex) { _logger.LogError(ex, "Mobile notificacoes lidas erro uid:{Uid}", uid); return StatusCode(500, ApiResponse<object>.Fail("Não foi possível atualizar notificações.", 500)); }
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

    [HttpGet("convites")]
    public async Task<IActionResult> Convites([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return await PlantoesDisponiveis(page, pageSize);
    }

    [HttpGet("convites/{id:guid}")]
    public async Task<IActionResult> Convite(Guid id)
    {
        return await Plantao(id);
    }

    [HttpPost("convites/{id:guid}/aceitar")]
    public async Task<IActionResult> AceitarConvite(Guid id)
    {
        return await SolicitarPlantao(id);
    }

    [HttpPost("convites/{id:guid}/recusar")]
    public IActionResult RecusarConvite(Guid id)
    {
        return Ok(ApiResponse<object>.Ok(new { id, status = "recusado" }, "Convite recusado."));
    }

    [HttpGet("plantoes/{id:guid}")]
    public async Task<IActionResult> Plantao(Guid id)
    {
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var row = await cn.QueryFirstOrDefaultAsync(@"select p.id, h.nome_fantasia as hospitalNome, e.nome as especialidadeNome, p.data_inicio as dataInicio, p.data_fim as dataFim, p.valor, p.vagas_disponiveis as vagasDisponiveis, p.status
from plantaopro.plantoes p
join plantaopro.hospitais h on h.id=p.hospital_id
join plantaopro.especialidades e on e.id=p.especialidade_id
where p.id=@id and p.reg_status='A'", new { id });
            if (row is null) return NotFound(ApiResponse<object>.Fail("Plantão não encontrado.", 404));
            return Ok(ApiResponse<object>.Ok(row, "Plantão carregado."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile plantao detalhe erro id:{Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível carregar o plantão.", 500));
        }
    }

    [HttpPost("plantoes/{id:guid}/solicitar")]
    public async Task<IActionResult> SolicitarPlantao(Guid id)
    {
        var uid = GetUserId();
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var medicoId = await cn.ExecuteScalarAsync<Guid?>("select id from plantaopro.medicos where lower(email)=lower((select email from plantaopro.usuarios where id=@uid)) and reg_status='A' limit 1", new { uid });
            if (medicoId is null) return NotFound(ApiResponse<object>.Fail("Médico não encontrado.", 404));
            var exists = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.escalas where plantao_id=@id and medico_id=@medico and reg_status='A'", new { id, medico = medicoId });
            if (exists > 0) return BadRequest(ApiResponse<object>.Fail("Solicitação já existe para este plantão.", 400));
            await cn.ExecuteAsync("insert into plantaopro.escalas(id,plantao_id,medico_id,status,reg_status,reg_date) values(gen_random_uuid(),@id,@medico,'solicitado','A',now())", new { id, medico = medicoId });
            return Ok(ApiResponse<object>.Ok(new { id }, "Solicitação enviada com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile solicitar plantao erro uid:{Uid} id:{Id}", uid, id);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível solicitar plantão.", 500));
        }
    }


    public sealed record MobilePerfilUpdateRequest(string Nome, string? Telefone);
}
