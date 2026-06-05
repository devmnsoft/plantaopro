using System.Diagnostics;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantaoPro.Api;
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
    private readonly FinanceiroService _financeiro;
    private readonly MedicoRecomendacaoService _recomendacaoService;
    private readonly NotificacaoService _notificacao;
    private readonly EscalaService _escala;
    private readonly ILogger<MobileController> _logger;
    private readonly UsuarioContextService _usuarioContext;
    private readonly AssinaturaGuardService _assinaturaGuard;
    private readonly IAuditService _audit;

    public MobileController(IConfiguration cfg, AuthService auth, MedicoAreaService medicoArea, FinanceiroService financeiro, MedicoRecomendacaoService recomendacaoService, NotificacaoService notificacao, EscalaService escala, UsuarioContextService usuarioContext, AssinaturaGuardService assinaturaGuard, IAuditService audit, ILogger<MobileController> logger)
    {
        _cfg = cfg;
        _auth = auth;
        _medicoArea = medicoArea;
        _financeiro = financeiro;
        _recomendacaoService = recomendacaoService;
        _notificacao = notificacao;
        _escala = escala;
        _logger = logger;
        _usuarioContext = usuarioContext;
        _assinaturaGuard = assinaturaGuard;
        _audit = audit;
    }

    private Guid GetUserId()
    {
        var uidClaim = User.FindFirst("uid")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(uidClaim, out var uid) ? uid : Guid.Empty;
    }

    private string GetPerfil()
    {
        var roles = string.Join(',', User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(x => x.Value));
        return string.IsNullOrWhiteSpace(roles) ? "sem-perfil" : roles;
    }
    private string GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
    private static int NormalizarPage(int page) => Math.Max(1, page);
    private static int NormalizarPageSize(int pageSize) => Math.Clamp(pageSize, 1, 50);
    private static int NormalizarLimite(int limite) => Math.Clamp(limite, 1, 25);



    private async Task<Guid?> GetMedicoIdAutenticadoAsync(NpgsqlConnection cn, Guid uid, Guid? clienteId)
    {
        return await cn.ExecuteScalarAsync<Guid?>(@"select id
from plantaopro.medicos
where reg_status='A'
  and (@clienteId is null or cliente_id=@clienteId)
  and (usuario_id=@uid or lower(email)=lower((select email from plantaopro.usuarios where id=@uid)))
limit 1", new { uid, clienteId });
    }

    private async Task<MobileConviteDto?> BuscarConviteDoMedicoAsync(NpgsqlConnection cn, Guid conviteId, Guid medicoId, Guid? clienteId)
    {
        return await cn.QueryFirstOrDefaultAsync<MobileConviteDto>(@"select c.id as ""Id"",
       c.plantao_id as ""PlantaoId"",
       coalesce(h.nome_fantasia,'') as ""HospitalNome"",
       coalesce(h.cidade,'') as ""HospitalCidade"",
       coalesce(h.estado,'') as ""HospitalEstado"",
       coalesce(e.nome,'') as ""EspecialidadeNome"",
       p.data_inicio as ""DataInicio"",
       p.data_fim as ""DataFim"",
       p.valor as ""Valor"",
       coalesce(c.status,'') as ""Status"",
       coalesce(c.mensagem,'') as ""Mensagem"",
       c.data_envio as ""DataEnvio"",
       c.data_resposta as ""DataResposta"",
       coalesce(c.motivo_recusa,'') as ""MotivoRecusa""
from plantaopro.plantao_convites c
join plantaopro.plantoes p on p.id=c.plantao_id
join plantaopro.hospitais h on h.id=p.hospital_id
join plantaopro.especialidades e on e.id=p.especialidade_id
where c.id=@conviteId
  and c.medico_id=@medicoId
  and c.reg_status='A'
  and p.reg_status='A'
  and (@clienteId is null or p.cliente_id=@clienteId)", new { conviteId, medicoId, clienteId });
    }

    private async Task<ApiResponse<bool>?> ValidarPlanoMobileAsync()
    {
        var clienteId = _usuarioContext.GetClienteId();
        if (!clienteId.HasValue)
        {
            return ApiResponse<bool>.Fail("Cliente do usuário não identificado para acesso mobile.", 403);
        }

        var permissao = await _assinaturaGuard.PodeUsarMobile(clienteId.Value);
        if (!permissao.Success)
        {
            _logger.LogWarning("Acesso mobile negado por plano cliente:{ClienteId} uid:{Uid}", clienteId, GetUserId());
            await _audit.RegistrarAsync(GetUserId(), clienteId, AuditoriaConstants.Entidades.ApiMobile, null, AuditoriaConstants.Acoes.AcessoNegado, new { motivo = permissao.Message }, false, GetIp(), GetPerfil());
            return permissao;
        }

        return null;
    }
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
                await _audit.RegistrarAsync(null, null, AuditoriaConstants.Entidades.ApiMobile, null, AuditoriaConstants.Acoes.LoginFalha, new { email = request.Email, statusCode = response.StatusCode }, false, GetIp(), "sem-perfil");
                return StatusCode(response.StatusCode, ApiResponse<MobileLoginResponseDto>.Fail(response.Message, response.StatusCode));
            }

            var payload = new MobileLoginResponseDto(response.Data.Token, null, response.Data.ExpiresAt, response.Data.Roles ?? Array.Empty<string>());
            var perfil = string.Join(',', payload.Roles);
            await _audit.RegistrarAsync(response.Data.UsuarioId, response.Data.ClienteId, AuditoriaConstants.Entidades.ApiMobile, response.Data.UsuarioId, AuditoriaConstants.Acoes.LoginSucesso, new { email = request.Email }, true, GetIp(), string.IsNullOrWhiteSpace(perfil) ? "sem-perfil" : perfil);
            _logger.LogInformation("Mobile login sucesso uid:{Uid} perfil:{Perfil} ip:{Ip} duracaoMs:{Duracao}", response.Data.UsuarioId, perfil, GetIp(), sw.ElapsedMilliseconds);
            return Ok(ApiResponse<MobileLoginResponseDto>.Ok(payload, "Login realizado com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile login erro ip:{Ip} duracaoMs:{Duracao}", GetIp(), sw.ElapsedMilliseconds);
            await _audit.RegistrarAsync(null, null, AuditoriaConstants.Entidades.ApiMobile, null, AuditoriaConstants.Acoes.LoginFalha, new { email = request.Email, motivo = "erro_interno" }, false, GetIp(), "sem-perfil");
            return StatusCode(500, ApiResponse<MobileLoginResponseDto>.Fail("Não foi possível autenticar no momento.", 500));
        }
    }



    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        var uid = GetUserId();
        if (uid == Guid.Empty) return Unauthorized(ApiResponse<object>.Fail("Sessão inválida.", 401));
        var nome = User.FindFirst("nome")?.Value ?? User.Identity?.Name ?? "Usuário";
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
        return Ok(ApiResponse<object>.Ok(new { id = uid, nome, email, perfil = GetPerfil() }, "Usuário autenticado."));
    }

    [HttpPost("auth/logout")]
    public IActionResult Logout() => Ok(ApiResponse<object>.Ok(new { revoked = false }, "Logout concluído no cliente mobile."));


    [HttpGet("perfil")]
    public async Task<IActionResult> Perfil()
    {
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        var uid = GetUserId();
        if (uid == Guid.Empty) return Unauthorized(ApiResponse<object>.Fail("Sessão inválida.", 401));
        var nome = User.FindFirst("nome")?.Value ?? User.Identity?.Name ?? "Usuário";
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
        return Ok(ApiResponse<object>.Ok(new { nome, email, perfil = GetPerfil() }, "Perfil carregado com sucesso."));
    }

    [HttpPut("perfil")]
    public async Task<IActionResult> AtualizarPerfil([FromBody] MobilePerfilUpdateRequest request)
    {
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
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
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        var uid = GetUserId();
        try { var response = await _notificacao.MarcarLidaAsync(uid, id, GetIp(), Request.Headers.UserAgent.ToString()); return StatusCode(response.StatusCode, response); }
        catch (Exception ex) { _logger.LogError(ex, "Mobile notificacao lida erro uid:{Uid} id:{Id}", uid, id); return StatusCode(500, ApiResponse<object>.Fail("Não foi possível atualizar notificação.", 500)); }
    }

    [HttpPut("notificacoes/lidas")]
    public async Task<IActionResult> NotificacoesLidas()
    {
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        var uid = GetUserId();
        try { var response = await _notificacao.MarcarTodasLidasAsync(uid, GetIp(), Request.Headers.UserAgent.ToString()); return StatusCode(response.StatusCode, response); }
        catch (Exception ex) { _logger.LogError(ex, "Mobile notificacoes lidas erro uid:{Uid}", uid); return StatusCode(500, ApiResponse<object>.Fail("Não foi possível atualizar notificações.", 500)); }
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
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
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        var sw = Stopwatch.StartNew();
        var uid = GetUserId();
        try
        {
            var response = await _medicoArea.PlantoesDisponiveisAsync(uid, NormalizarPage(page), NormalizarPageSize(pageSize));
            _logger.LogInformation("Mobile plantoes disponiveis uid:{Uid} perfil:{Perfil} ip:{Ip} page:{Page} size:{Size} duracaoMs:{Duracao}", uid, GetPerfil(), GetIp(), NormalizarPage(page), NormalizarPageSize(pageSize), sw.ElapsedMilliseconds);
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
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        var uid = GetUserId();
        try { var response = await _medicoArea.MinhasEscalasAsync(uid, NormalizarPage(page), NormalizarPageSize(pageSize)); return StatusCode(response.StatusCode, response); }
        catch (Exception ex) { _logger.LogError(ex, "Mobile escalas erro uid:{Uid}", uid); return StatusCode(500, ApiResponse<object>.Fail("Não foi possível listar escalas.", 500)); }
    }

    [HttpGet("meus-pagamentos")]
    public async Task<IActionResult> MeusPagamentos([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        var uid = GetUserId();
        try { var response = await _medicoArea.MeusPagamentosAsync(uid, NormalizarPage(page), NormalizarPageSize(pageSize)); return StatusCode(response.StatusCode, response); }
        catch (Exception ex) { _logger.LogError(ex, "Mobile pagamentos erro uid:{Uid}", uid); return StatusCode(500, ApiResponse<object>.Fail("Não foi possível listar pagamentos.", 500)); }
    }

    [HttpGet("meus-pagamentos/{id:guid}")]
    public async Task<IActionResult> MeuPagamento(Guid id)
    {
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        var uid = GetUserId();
        try
        {
            var response = await _financeiro.MeuByIdAsync(uid, id);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile detalhe pagamento erro uid:{Uid} pagamento:{PagamentoId}", uid, id);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível carregar pagamento.", 500));
        }
    }

    [HttpGet("notificacoes")]
    public async Task<IActionResult> Notificacoes([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        var uid = GetUserId();
        try { var response = await _notificacao.ListarAsync(uid, new NotificationFilterRequest(null, null, null, null, NormalizarPage(page), NormalizarPageSize(pageSize))); return StatusCode(response.StatusCode, response); }
        catch (Exception ex) { _logger.LogError(ex, "Mobile notificacoes erro uid:{Uid}", uid); return StatusCode(500, ApiResponse<object>.Fail("Não foi possível listar notificações.", 500)); }
    }

    [HttpGet("notificacoes/contador")]
    public async Task<IActionResult> ContadorNotificacoes()
    {
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        var uid = GetUserId();
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var total = await cn.ExecuteScalarAsync<long>("select count(1) from plantaopro.notificacoes where usuario_id=@uid and reg_status='A' and coalesce(lida,false)=false", new { uid });
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
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        var uid = GetUserId();
        var pg = NormalizarPage(page);
        var ps = NormalizarPageSize(pageSize);
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var clienteId = _usuarioContext.GetClienteId();
            var medicoId = await GetMedicoIdAutenticadoAsync(cn, uid, clienteId);
            if (medicoId is null) return NotFound(ApiResponse<object>.Fail("Médico não encontrado para o usuário autenticado.", 404));

            var total = await cn.ExecuteScalarAsync<long>(@"select count(1)
from plantaopro.plantao_convites c
join plantaopro.plantoes p on p.id=c.plantao_id
where c.medico_id=@medicoId
  and c.reg_status='A'
  and p.reg_status='A'
  and (@clienteId is null or p.cliente_id=@clienteId)", new { medicoId, clienteId });
            var items = await cn.QueryAsync<MobileConviteDto>(@"select c.id as ""Id"",
       c.plantao_id as ""PlantaoId"",
       coalesce(h.nome_fantasia,'') as ""HospitalNome"",
       coalesce(h.cidade,'') as ""HospitalCidade"",
       coalesce(h.estado,'') as ""HospitalEstado"",
       coalesce(e.nome,'') as ""EspecialidadeNome"",
       p.data_inicio as ""DataInicio"",
       p.data_fim as ""DataFim"",
       p.valor as ""Valor"",
       coalesce(c.status,'') as ""Status"",
       coalesce(c.mensagem,'') as ""Mensagem"",
       c.data_envio as ""DataEnvio"",
       c.data_resposta as ""DataResposta"",
       coalesce(c.motivo_recusa,'') as ""MotivoRecusa""
from plantaopro.plantao_convites c
join plantaopro.plantoes p on p.id=c.plantao_id
join plantaopro.hospitais h on h.id=p.hospital_id
join plantaopro.especialidades e on e.id=p.especialidade_id
where c.medico_id=@medicoId
  and c.reg_status='A'
  and p.reg_status='A'
  and (@clienteId is null or p.cliente_id=@clienteId)
order by c.data_envio desc
limit @lim offset @off", new { medicoId, clienteId, lim = ps, off = (pg - 1) * ps });
            return Ok(ApiResponse<PagedResult<MobileConviteDto>>.Ok(new PagedResult<MobileConviteDto>(items, pg, ps, total), "Convites carregados."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile convites erro uid:{Uid}", uid);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível listar convites.", 500));
        }
    }

    [HttpGet("convites/{id:guid}")]
    public async Task<IActionResult> Convite(Guid id)
    {
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        var uid = GetUserId();
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var clienteId = _usuarioContext.GetClienteId();
            var medicoId = await GetMedicoIdAutenticadoAsync(cn, uid, clienteId);
            if (medicoId is null) return NotFound(ApiResponse<object>.Fail("Médico não encontrado para o usuário autenticado.", 404));
            var convite = await BuscarConviteDoMedicoAsync(cn, id, medicoId.Value, clienteId);
            if (convite is null) return NotFound(ApiResponse<object>.Fail("Convite não encontrado.", 404));
            return Ok(ApiResponse<MobileConviteDto>.Ok(convite, "Convite carregado."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile convite detalhe erro uid:{Uid} convite:{ConviteId}", uid, id);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível carregar convite.", 500));
        }
    }

    [HttpPost("convites/{id:guid}/aceitar")]
    public async Task<IActionResult> AceitarConvite(Guid id)
    {
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        var uid = GetUserId();
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var clienteId = _usuarioContext.GetClienteId();
            var medicoId = await GetMedicoIdAutenticadoAsync(cn, uid, clienteId);
            if (medicoId is null) return NotFound(ApiResponse<object>.Fail("Médico não encontrado para o usuário autenticado.", 404));
            var convite = await BuscarConviteDoMedicoAsync(cn, id, medicoId.Value, clienteId);
            if (convite is null) return NotFound(ApiResponse<object>.Fail("Convite não encontrado.", 404));
            if (!string.Equals(convite.Status, "ENVIADO", StringComparison.OrdinalIgnoreCase) && !string.Equals(convite.Status, "PENDENTE", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<object>.Fail("Convite não está pendente para aceite.", 400));
            }

            var response = await _escala.AceitarAsync(convite.PlantaoId, medicoId.Value, uid, GetIp(), Request.Headers.UserAgent.ToString());
            if (response.Success)
            {
                await cn.ExecuteAsync("update plantaopro.plantao_convites set status='ACEITO', data_resposta=now() where id=@id and medico_id=@medicoId", new { id, medicoId });
                await _audit.RegistrarAsync(uid, clienteId, AuditoriaConstants.Entidades.Convite, id, AuditoriaConstants.Acoes.AceitarConvite, new { conviteId = id, convite.PlantaoId }, true, GetIp(), GetPerfil());
            }

            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile aceitar convite erro uid:{Uid} convite:{ConviteId}", uid, id);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível aceitar convite.", 500));
        }
    }

    [HttpPost("convites/{id:guid}/recusar")]
    public async Task<IActionResult> RecusarConvite(Guid id, [FromBody] MobileRecusarConviteRequest? request)
    {
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        var uid = GetUserId();
        if (request is null || string.IsNullOrWhiteSpace(request.Motivo)) return BadRequest(ApiResponse<object>.Fail("Informe o motivo da recusa.", 400));
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var clienteId = _usuarioContext.GetClienteId();
            var medicoId = await GetMedicoIdAutenticadoAsync(cn, uid, clienteId);
            if (medicoId is null) return NotFound(ApiResponse<object>.Fail("Médico não encontrado para o usuário autenticado.", 404));
            var convite = await BuscarConviteDoMedicoAsync(cn, id, medicoId.Value, clienteId);
            if (convite is null) return NotFound(ApiResponse<object>.Fail("Convite não encontrado.", 404));
            if (!string.Equals(convite.Status, "ENVIADO", StringComparison.OrdinalIgnoreCase) && !string.Equals(convite.Status, "PENDENTE", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<object>.Fail("Convite não está pendente para recusa.", 400));
            }

            await cn.ExecuteAsync("update plantaopro.plantao_convites set status='RECUSADO', data_resposta=now(), motivo_recusa=@motivo where id=@id and medico_id=@medicoId", new { id, medicoId, motivo = request.Motivo.Trim() });
            await _audit.RegistrarAsync(uid, clienteId, AuditoriaConstants.Entidades.Convite, id, AuditoriaConstants.Acoes.RecusarConvite, new { conviteId = id, motivo = "informado" }, true, GetIp(), GetPerfil());
            return Ok(ApiResponse<object>.Ok(new { id, status = "RECUSADO" }, "Convite recusado."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile recusar convite erro uid:{Uid} convite:{ConviteId}", uid, id);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível recusar convite.", 500));
        }
    }

    [HttpGet("recomendacoes")]
    public async Task<IActionResult> Recomendacoes([FromQuery] int limite = 10)
    {
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        var uid = GetUserId();
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var clienteId = _usuarioContext.GetClienteId();
            var medicoId = await cn.ExecuteScalarAsync<Guid?>("select id from plantaopro.medicos where lower(email)=lower((select email from plantaopro.usuarios where id=@uid)) and reg_status='A' and (@clienteId is null or cliente_id=@clienteId) limit 1", new { uid, clienteId });
            if (medicoId is null) return NotFound(ApiResponse<object>.Fail("Médico não encontrado.", 404));
            var lista = await _recomendacaoService.RecomendarPlantoesAsync(medicoId.Value, NormalizarLimite(limite));
            return Ok(ApiResponse<IEnumerable<MedicoPlantaoRecomendacaoDto>>.Ok(lista, "Recomendações carregadas com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile recomendacoes erro uid:{Uid}", uid);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível carregar recomendações.", 500));
        }
    }

    [HttpGet("disponibilidade")]
    public async Task<IActionResult> Disponibilidade()
    {
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        return Ok(ApiResponse<object>.Ok(new { }, "Disponibilidade carregada com sucesso."));
    }

    [HttpGet("preferencias")]
    public async Task<IActionResult> Preferencias()
    {
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        return Ok(ApiResponse<object>.Ok(new { notificacoesPush = true, lembretePlantaoHoras = 12 }, "Preferências carregadas com sucesso."));
    }



    [HttpPut("disponibilidade")]
    public async Task<IActionResult> AtualizarDisponibilidade([FromBody] object request)
    {
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        try
        {
            await _audit.RegistrarAsync(GetUserId(), _usuarioContext.GetClienteId(), AuditoriaConstants.Entidades.ApiMobile, null, AuditoriaConstants.Acoes.Editar, new { area = "disponibilidade_mobile" }, true, GetIp(), GetPerfil());
            return Ok(ApiResponse<object>.Ok(new { atualizado = true }, "Disponibilidade atualizada com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile atualizar disponibilidade erro uid:{Uid}", GetUserId());
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível atualizar disponibilidade.", 500));
        }
    }

    [HttpPut("preferencias")]
    public async Task<IActionResult> AtualizarPreferencias([FromBody] object request)
    {
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        try
        {
            await _audit.RegistrarAsync(GetUserId(), _usuarioContext.GetClienteId(), AuditoriaConstants.Entidades.ApiMobile, null, AuditoriaConstants.Acoes.Editar, new { area = "preferencias_mobile" }, true, GetIp(), GetPerfil());
            return Ok(ApiResponse<object>.Ok(new { atualizado = true }, "Preferências atualizadas com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile atualizar preferencias erro uid:{Uid}", GetUserId());
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível atualizar preferências.", 500));
        }
    }

    [HttpGet("plantoes/{id:guid}")]
    public async Task<IActionResult> Plantao(Guid id)
    {
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var clienteId = _usuarioContext.GetClienteId();
            var plantaoExiste = await cn.ExecuteScalarAsync<long>("select count(1) from plantaopro.plantoes where id=@id and reg_status='A'", new { id });
            var row = await cn.QueryFirstOrDefaultAsync(@"select p.id, h.nome_fantasia as hospitalNome, e.nome as especialidadeNome, p.data_inicio as dataInicio, p.data_fim as dataFim, p.valor, p.vagas_disponiveis as vagasDisponiveis, p.status
from plantaopro.plantoes p
join plantaopro.hospitais h on h.id=p.hospital_id
join plantaopro.especialidades e on e.id=p.especialidade_id
where p.id=@id and p.reg_status='A' and (@clienteId is null or p.cliente_id=@clienteId)", new { id, clienteId });
            if (row is null && plantaoExiste > 0)
            {
                await _audit.RegistrarAsync(GetUserId(), clienteId, AuditoriaConstants.Entidades.ApiMobile, id, AuditoriaConstants.Acoes.AcessoNegado, new { motivo = "plantao_fora_do_cliente" }, false, GetIp(), GetPerfil());
                return StatusCode(403, ApiResponse<object>.Fail("Você não possui permissão para acessar este plantão.", 403));
            }
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
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        var uid = GetUserId();
        var sw = Stopwatch.StartNew();
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var clienteId = _usuarioContext.GetClienteId();
            var medicoId = await cn.ExecuteScalarAsync<Guid?>(@"select id
from plantaopro.medicos
where reg_status='A'
  and (@clienteId is null or cliente_id=@clienteId)
  and (usuario_id=@uid or lower(email)=lower((select email from plantaopro.usuarios where id=@uid)))
limit 1", new { uid, clienteId });
            if (medicoId is null) return NotFound(ApiResponse<object>.Fail("Médico não encontrado para o usuário autenticado.", 404));

            var plantaoPermitido = await cn.ExecuteScalarAsync<long>(@"select count(1)
from plantaopro.plantoes
where id=@id and reg_status='A' and (@clienteId is null or cliente_id=@clienteId)", new { id, clienteId });
            if (plantaoPermitido == 0)
            {
                await _audit.RegistrarAsync(uid, clienteId, AuditoriaConstants.Entidades.ApiMobile, id, AuditoriaConstants.Acoes.AcessoNegado, new { motivo = "plantao_fora_do_cliente" }, false, GetIp(), GetPerfil());
                return StatusCode(403, ApiResponse<object>.Fail("Você não possui permissão para acessar este plantão.", 403));
            }

            var response = await _escala.AceitarAsync(id, medicoId.Value, uid, GetIp(), Request.Headers.UserAgent.ToString());
            _logger.LogInformation("Mobile solicitação de plantão uid:{Uid} medico:{MedicoId} plantao:{PlantaoId} status:{StatusCode} duracaoMs:{Duracao}", uid, medicoId, id, response.StatusCode, sw.ElapsedMilliseconds);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile solicitar plantao erro uid:{Uid} id:{Id} duracaoMs:{Duracao}", uid, id, sw.ElapsedMilliseconds);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível solicitar plantão.", 500));
        }
    }


    [HttpGet("suporte/chamados")]
    public async Task<IActionResult> ChamadosSuporte([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        var uid = GetUserId();
        var clienteId = _usuarioContext.GetClienteId();
        var pagina = NormalizarPage(page);
        var tamanho = NormalizarPageSize(pageSize);
        var offset = (pagina - 1) * tamanho;
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var total = await cn.ExecuteScalarAsync<long>(@"select count(1)
from plantaopro.chamados_suporte
where reg_status='A'
  and usuario_id=@uid
  and (@clienteId is null or cliente_id=@clienteId)", new { uid, clienteId });

            var itens = await cn.QueryAsync<MobileChamadoSuporteDto>(@"select id as ""Id"",
       coalesce(protocolo,'') as ""Protocolo"",
       coalesce(titulo,'') as ""Titulo"",
       coalesce(categoria,'GERAL') as ""Categoria"",
       coalesce(prioridade,'NORMAL') as ""Prioridade"",
       coalesce(status,'ABERTO') as ""Status"",
       criado_em as ""CriadoEm"",
       atualizado_em as ""AtualizadoEm""
from plantaopro.chamados_suporte
where reg_status='A'
  and usuario_id=@uid
  and (@clienteId is null or cliente_id=@clienteId)
order by criado_em desc
limit @tamanho offset @offset", new { uid, clienteId, tamanho, offset });

            return Ok(ApiResponse<object>.Ok(new { page = pagina, pageSize = tamanho, total, items = itens }, "Chamados carregados com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile listar chamados suporte erro uid:{Uid}", uid);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível listar chamados de suporte.", 500));
        }
    }

    [HttpGet("suporte/chamados/{id:guid}")]
    public async Task<IActionResult> ChamadoSuporte(Guid id)
    {
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        var uid = GetUserId();
        var clienteId = _usuarioContext.GetClienteId();
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var chamado = await cn.QueryFirstOrDefaultAsync<MobileChamadoSuporteDetalheDto>(@"select id as ""Id"",
       coalesce(protocolo,'') as ""Protocolo"",
       coalesce(titulo,'') as ""Titulo"",
       coalesce(descricao,'') as ""Descricao"",
       coalesce(categoria,'GERAL') as ""Categoria"",
       coalesce(prioridade,'NORMAL') as ""Prioridade"",
       coalesce(status,'ABERTO') as ""Status"",
       criado_em as ""CriadoEm"",
       atualizado_em as ""AtualizadoEm""
from plantaopro.chamados_suporte
where id=@id
  and reg_status='A'
  and usuario_id=@uid
  and (@clienteId is null or cliente_id=@clienteId)", new { id, uid, clienteId });
            if (chamado is null)
            {
                await _audit.RegistrarAsync(uid, clienteId, AuditoriaConstants.Entidades.Suporte, id, AuditoriaConstants.Acoes.AcessoNegado, new { motivo = "chamado_mobile_indisponivel" }, false, GetIp(), GetPerfil());
                return NotFound(ApiResponse<object>.Fail("Chamado não encontrado para o usuário autenticado.", 404));
            }

            var mensagens = await cn.QueryAsync<MobileChamadoMensagemDto>(@"select id as ""Id"",
       coalesce(tipo_autor,'USUARIO') as ""TipoAutor"",
       coalesce(mensagem,'') as ""Mensagem"",
       criado_em as ""CriadoEm""
from plantaopro.chamado_mensagens
where chamado_id=@id
  and reg_status='A'
order by criado_em asc
limit 50", new { id });

            return Ok(ApiResponse<MobileChamadoSuporteDetalheResponseDto>.Ok(new MobileChamadoSuporteDetalheResponseDto(chamado, mensagens), "Chamado carregado com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile detalhe chamado suporte erro uid:{Uid} chamado:{ChamadoId}", uid, id);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível carregar o chamado de suporte.", 500));
        }
    }

    [HttpPost("suporte/chamados")]
    public async Task<IActionResult> CriarChamadoSuporte([FromBody] MobileCriarChamadoSuporteRequest? request)
    {
        var bloqueio = await ValidarPlanoMobileAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);
        var uid = GetUserId();
        var clienteId = _usuarioContext.GetClienteId();
        if (request is null || string.IsNullOrWhiteSpace(request.Titulo) || string.IsNullOrWhiteSpace(request.Descricao))
        {
            return BadRequest(ApiResponse<object>.Fail("Informe título e descrição do chamado.", 400));
        }

        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var id = Guid.NewGuid();
            var protocolo = "SUP-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            await cn.ExecuteAsync(@"insert into plantaopro.chamados_suporte
(id, cliente_id, usuario_id, protocolo, titulo, descricao, categoria, prioridade, status, origem, reg_status, criado_em, atualizado_em)
values
(@id, @clienteId, @uid, @protocolo, @titulo, @descricao, @categoria, @prioridade, 'ABERTO', 'MOBILE', 'A', now(), now())", new
            {
                id,
                clienteId,
                uid,
                protocolo,
                titulo = request.Titulo.Trim(),
                descricao = request.Descricao.Trim(),
                categoria = string.IsNullOrWhiteSpace(request.Categoria) ? "GERAL" : request.Categoria.Trim().ToUpperInvariant(),
                prioridade = string.IsNullOrWhiteSpace(request.Prioridade) ? "NORMAL" : request.Prioridade.Trim().ToUpperInvariant()
            });
            await _audit.RegistrarAsync(uid, clienteId, AuditoriaConstants.Entidades.Suporte, id, AuditoriaConstants.Acoes.Criar, new { origem = "mobile", protocolo }, true, GetIp(), GetPerfil());
            return StatusCode(201, new ApiResponse<object>(true, "Chamado criado com sucesso.", new { id, protocolo, status = "ABERTO" }, null, 201, DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile criar chamado suporte erro uid:{Uid}", uid);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível criar chamado de suporte.", 500));
        }
    }


    public sealed record MobilePerfilUpdateRequest(string Nome, string? Telefone);
    public sealed record MobileCriarChamadoSuporteRequest(string Titulo, string Descricao, string? Categoria, string? Prioridade);
    public sealed record MobileChamadoSuporteDto(Guid Id, string Protocolo, string Titulo, string Categoria, string Prioridade, string Status, DateTime CriadoEm, DateTime AtualizadoEm);
    public sealed record MobileChamadoSuporteDetalheDto(Guid Id, string Protocolo, string Titulo, string Descricao, string Categoria, string Prioridade, string Status, DateTime CriadoEm, DateTime AtualizadoEm);
    public sealed record MobileChamadoMensagemDto(Guid Id, string TipoAutor, string Mensagem, DateTime CriadoEm);
    public sealed record MobileChamadoSuporteDetalheResponseDto(MobileChamadoSuporteDetalheDto Chamado, IEnumerable<MobileChamadoMensagemDto> Mensagens);
    public sealed record MobileRecusarConviteRequest(string Motivo);
    public sealed record MobileConviteDto(Guid Id, Guid PlantaoId, string HospitalNome, string HospitalCidade, string HospitalEstado, string EspecialidadeNome, DateTime DataInicio, DateTime DataFim, decimal Valor, string Status, string Mensagem, DateTime DataEnvio, DateTime? DataResposta, string MotivoRecusa);
}
