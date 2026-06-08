using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public")]
[Tags("Self-service público")]
public sealed class PublicSelfServiceController : ControllerBase
{
    private readonly SelfServiceSaasService _service;
    private readonly ILogger<PublicSelfServiceController> _logger;

    public PublicSelfServiceController(SelfServiceSaasService service, ILogger<PublicSelfServiceController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("planos")]
    public async Task<IActionResult> Planos()
    {
        var result = await _service.ListarPlanosPublicosAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("planos/comparativo")]
    public async Task<IActionResult> Comparativo()
    {
        var result = await _service.ComparativoAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("planos/faq")]
    public async Task<IActionResult> Faq()
    {
        var result = await _service.FaqPlanosAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("cadastro/iniciar")]
    public IActionResult Iniciar()
    {
        return Ok(ApiResponse<object>.Ok(new { etapa = "empresa" }, "Cadastro iniciado."));
    }

    [HttpPost("cadastro/empresa")]
    public IActionResult Empresa([FromBody] CadastroEmpresaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RazaoSocial) || string.IsNullOrWhiteSpace(request.Cnpj)) return BadRequest(ApiResponse<string>.Fail("Razão social e CNPJ são obrigatórios.", 400));
        return Ok(ApiResponse<CadastroEmpresaRequest>.Ok(request, "Dados da empresa validados."));
    }

    [HttpPost("cadastro/plano")]
    public IActionResult Plano([FromBody] CadastroPlanoRequest request)
    {
        if (request.PlanoId == Guid.Empty) return BadRequest(ApiResponse<string>.Fail("Plano é obrigatório.", 400));
        if (!request.AceiteTermos || !request.AceitePrivacidade) return BadRequest(ApiResponse<string>.Fail("Aceite os termos e a política de privacidade.", 400));
        return Ok(ApiResponse<CadastroPlanoRequest>.Ok(request, "Plano validado."));
    }

    [HttpPost("cadastro/usuario-admin")]
    public IActionResult UsuarioAdmin([FromBody] CadastroUsuarioAdminRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Senha)) return BadRequest(ApiResponse<string>.Fail("E-mail e senha são obrigatórios.", 400));
        return Ok(ApiResponse<object>.Ok(new { email = request.Email }, "Usuário administrador validado."));
    }

    [HttpPost("cadastro/finalizar")]
    public async Task<IActionResult> Finalizar([FromBody] CadastroSelfServiceRequest request)
    {
        try
        {
            var result = await _service.FinalizarCadastroAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no cadastro self-service público");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível finalizar o cadastro.", 500));
        }
    }
}

[ApiController]
[AllowAnonymous]
[Route("api/planos")]
[Tags("Planos públicos")]
public sealed class PlanosPublicosApiController : ControllerBase
{
    private readonly SelfServiceSaasService _service;
    public PlanosPublicosApiController(SelfServiceSaasService service) => _service = service;

    [HttpGet("publicos")]
    public async Task<IActionResult> Publicos()
    {
        var result = await _service.ListarPlanosPublicosAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("comparativo")]
    public async Task<IActionResult> Comparativo()
    {
        var result = await _service.ComparativoAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("faq")]
    public async Task<IActionResult> Faq()
    {
        var result = await _service.FaqPlanosAsync();
        return StatusCode(result.StatusCode, result);
    }
}

[ApiController]
[Authorize]
[Route("api/white-label")]
[Tags("White label")]
public sealed class WhiteLabelController : ControllerBase
{
    private readonly SelfServiceSaasService _service;
    private readonly ILogger<WhiteLabelController> _logger;

    public WhiteLabelController(SelfServiceSaasService service, ILogger<WhiteLabelController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("configuracao")]
    [AllowAnonymous]
    public async Task<IActionResult> Configuracao()
    {
        var result = await _service.ObterWhiteLabelAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("tenant/{tenantId:guid}")]
    public async Task<IActionResult> Tenant(Guid tenantId)
    {
        var result = await _service.ObterWhiteLabelAsync(tenantId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("tenant/{tenantId:guid}")]
    public async Task<IActionResult> Salvar(Guid tenantId, [FromBody] WhiteLabelConfiguracaoDto request)
    {
        try
        {
            var result = await _service.SalvarWhiteLabelAsync(tenantId, request, HttpContext.Connection.RemoteIpAddress?.ToString());
            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar white label");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível salvar white label.", 500));
        }
    }

    [HttpPost("tenant/{tenantId:guid}/logo")]
    public async Task<IActionResult> Logo(Guid tenantId, [FromBody] AssetUploadRequest request)
    {
        var validacao = ValidarAsset(request);
        if (!validacao.Success) return BadRequest(validacao);
        var cfg = (await _service.ObterWhiteLabelAsync(tenantId)).Data ?? new WhiteLabelConfiguracaoDto { TenantId = tenantId };
        cfg.LogoUrl = request.Url;
        var result = await _service.SalvarWhiteLabelAsync(tenantId, cfg, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("tenant/{tenantId:guid}/favicon")]
    public async Task<IActionResult> Favicon(Guid tenantId, [FromBody] AssetUploadRequest request)
    {
        var validacao = ValidarAsset(request);
        if (!validacao.Success) return BadRequest(validacao);
        var cfg = (await _service.ObterWhiteLabelAsync(tenantId)).Data ?? new WhiteLabelConfiguracaoDto { TenantId = tenantId };
        cfg.FaviconUrl = request.Url;
        var result = await _service.SalvarWhiteLabelAsync(tenantId, cfg, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("tenant/{tenantId:guid}/restaurar-padrao")]
    public async Task<IActionResult> Restaurar(Guid tenantId)
    {
        var result = await _service.SalvarWhiteLabelAsync(tenantId, new WhiteLabelConfiguracaoDto { TenantId = tenantId }, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("preview")]
    [AllowAnonymous]
    public async Task<IActionResult> Preview()
    {
        var result = await _service.ObterWhiteLabelAsync();
        return StatusCode(result.StatusCode, result);
    }

    private static ApiResponse<string> ValidarAsset(AssetUploadRequest request)
    {
        if (request.TamanhoBytes <= 0 || request.TamanhoBytes > 2 * 1024 * 1024) return ApiResponse<string>.Fail("Arquivo deve ter até 2MB.", 400);
        var contentType = request.ContentType ?? string.Empty;
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return ApiResponse<string>.Fail("Apenas imagens são permitidas.", 400);
        if (string.IsNullOrWhiteSpace(request.Url)) return ApiResponse<string>.Fail("URL do asset é obrigatória.", 400);
        return ApiResponse<string>.Ok("ok");
    }
}

[ApiController]
[Authorize]
[Route("api/perfis")]
[Tags("Perfis e permissões")]
public sealed class PerfisController : ControllerBase
{
    private readonly SelfServiceSaasService _service;
    private readonly IConfiguration _cfg;
    private readonly ILogger<PerfisController> _logger;

    public PerfisController(SelfServiceSaasService service, IConfiguration cfg, ILogger<PerfisController> logger)
    {
        _service = service;
        _cfg = cfg;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var result = await _service.ListarPerfisAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detalhar(Guid id)
    {
        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        var dto = await cn.QueryFirstOrDefaultAsync<PerfilDto>("select id as \"Id\", tenant_id as \"TenantId\", cliente_id as \"ClienteId\", coalesce(codigo,'') as \"Codigo\", coalesce(nome,'') as \"Nome\", coalesce(descricao,'') as \"Descricao\", base_sistema as \"BaseSistema\", customizado as \"Customizado\", coalesce(status,'') as \"Status\" from plantaopro.perfis where id=@id and reg_status='A'", new { id });
        return dto is null ? NotFound(ApiResponse<string>.Fail("Perfil não encontrado.", 404)) : Ok(ApiResponse<PerfilDto>.Ok(dto));
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] PerfilRequest request)
    {
        try
        {
            var result = await _service.SalvarPerfilAsync(null, request, HttpContext.Connection.RemoteIpAddress?.ToString());
            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar perfil");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível criar perfil.", 500));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, [FromBody] PerfilRequest request)
    {
        try
        {
            var result = await _service.SalvarPerfilAsync(id, request, HttpContext.Connection.RemoteIpAddress?.ToString());
            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao editar perfil");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível editar perfil.", 500));
        }
    }

    [HttpPost("{id:guid}/inativar")]
    public async Task<IActionResult> Inativar(Guid id)
    {
        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        var baseSistema = await cn.ExecuteScalarAsync<bool>("select coalesce(base_sistema,false) from plantaopro.perfis where id=@id and reg_status='A'", new { id });
        if (baseSistema) return BadRequest(ApiResponse<string>.Fail("Perfis base não podem ser inativados.", 400));
        await cn.ExecuteAsync("update plantaopro.perfis set status='INATIVO', reg_status='I', reg_update=now() where id=@id", new { id });
        return Ok(ApiResponse<string>.Ok("ok", "Perfil inativado."));
    }

    [HttpPost("{id:guid}/permissoes")]
    public async Task<IActionResult> Permissoes(Guid id, [FromBody] PerfilPermissoesRequest request)
    {
        var result = await _service.AtualizarPermissoesPerfilAsync(id, request, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }
}

[ApiController]
[Authorize]
[Route("api")]
[Tags("Permissões")]
public sealed class PermissoesSistemaController : ControllerBase
{
    private readonly IConfiguration _cfg;
    public PermissoesSistemaController(IConfiguration cfg) => _cfg = cfg;

    [HttpGet("permissoes")]
    public async Task<IActionResult> Permissoes()
    {
        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<PermissaoDto>(@"select p.id as ""Id"", coalesce(m.nome,'') as ""Modulo"", coalesce(a.nome,'') as ""Acao"", coalesce(p.codigo,'') as ""Codigo"", coalesce(p.nome,'') as ""Nome"", p.sensivel as ""Sensivel"" from plantaopro.permissoes p join plantaopro.modulos_sistema m on m.id=p.modulo_id join plantaopro.acoes_sistema a on a.id=p.acao_id where p.reg_status='A' order by m.ordem,a.ordem limit 500");
        return Ok(ApiResponse<IEnumerable<PermissaoDto>>.Ok(rows));
    }

    [HttpGet("modulos-sistema")]
    public async Task<IActionResult> Modulos()
    {
        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<ModuloSistemaDto>("select id as \"Id\", coalesce(codigo,'') as \"Codigo\", coalesce(nome,'') as \"Nome\", coalesce(descricao,'') as \"Descricao\" from plantaopro.modulos_sistema where reg_status='A' order by ordem limit 200");
        return Ok(ApiResponse<IEnumerable<ModuloSistemaDto>>.Ok(rows));
    }
}

[ApiController]
[Authorize]
[Route("api/parametrizacoes")]
[Tags("Parametrizações do cliente")]
public sealed class ParametrizacoesController : ControllerBase
{
    private readonly SelfServiceSaasService _service;
    private readonly ILogger<ParametrizacoesController> _logger;
    public ParametrizacoesController(SelfServiceSaasService service, ILogger<ParametrizacoesController> logger) { _service = service; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _service.ObterParametrizacoesAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("operacionais")]
    public Task<IActionResult> Operacionais([FromBody] ParametrosCategoriaRequest request) => Salvar("OPERACIONAL", request);
    [HttpPut("financeiras")]
    public Task<IActionResult> Financeiras([FromBody] ParametrosCategoriaRequest request) => Salvar("FINANCEIRA", request);
    [HttpPut("notificacoes")]
    public Task<IActionResult> Notificacoes([FromBody] ParametrosCategoriaRequest request) => Salvar("NOTIFICACOES", request);
    [HttpPut("lgpd")]
    public Task<IActionResult> Lgpd([FromBody] ParametrosCategoriaRequest request) => Salvar("LGPD", request);
    [HttpPut("white-label")]
    public Task<IActionResult> WhiteLabel([FromBody] ParametrosCategoriaRequest request) => Salvar("WHITE_LABEL", request);

    private async Task<IActionResult> Salvar(string categoria, ParametrosCategoriaRequest request)
    {
        try
        {
            var result = await _service.SalvarParametrosAsync(categoria, request, HttpContext.Connection.RemoteIpAddress?.ToString());
            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar parametrizações");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível salvar parametrizações.", 500));
        }
    }
}

[ApiController]
[Authorize]
[Route("api/minha-assinatura")]
[Tags("Minha assinatura")]
public sealed class MinhaAssinaturaController : ControllerBase
{
    private readonly SelfServiceSaasService _service;
    public MinhaAssinaturaController(SelfServiceSaasService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _service.MinhaAssinaturaAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("uso")]
    public async Task<IActionResult> Uso()
    {
        var result = await _service.ObterUsoPlanoAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("faturas")]
    public async Task<IActionResult> Faturas()
    {
        var result = await _service.FaturasMinhaAssinaturaAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("solicitar-upgrade")]
    public async Task<IActionResult> Upgrade([FromBody] SolicitacaoMudancaPlanoRequest request)
    {
        var result = await _service.SolicitarMudancaPlanoAsync("UPGRADE", request, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("solicitar-downgrade")]
    public async Task<IActionResult> Downgrade([FromBody] SolicitacaoMudancaPlanoRequest request)
    {
        var result = await _service.SolicitarMudancaPlanoAsync("DOWNGRADE", request, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("solicitar-cancelamento")]
    public async Task<IActionResult> Cancelamento([FromBody] SolicitacaoCancelamentoAssinaturaRequest request)
    {
        var result = await _service.SolicitarCancelamentoAssinaturaAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result);
    }
}
