using System.Security.Claims;
using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed class UsuarioContextService
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public UsuarioContextService(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetUsuarioId()
    {
        var claim = httpContextAccessor.HttpContext?.User.FindFirst("uid")?.Value
                    ?? httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid id;
        return Guid.TryParse(claim, out id) ? id : null;
    }

    public Guid? GetClienteId()
    {
        var claim = httpContextAccessor.HttpContext?.User.FindFirst("cliente_id")?.Value;
        Guid id;
        return Guid.TryParse(claim, out id) ? id : null;
    }

    public string[] GetRoles()
    {
        return httpContextAccessor.HttpContext?.User
            .FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();
    }

    public bool IsAdminGlobal()
    {
        return GetRoles().Any(r => string.Equals(r, "ADMINISTRADOR_GLOBAL", StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class TenantGuardService
{
    private readonly UsuarioContextService usuarioContextService;
    private readonly IAuditService auditService;

    public TenantGuardService(UsuarioContextService usuarioContextService, IAuditService auditService)
    {
        this.usuarioContextService = usuarioContextService;
        this.auditService = auditService;
    }

    public async Task<ApiResponse<bool>> ValidarAcessoClienteAsync(Guid clienteId)
    {
        if (usuarioContextService.IsAdminGlobal()) return ApiResponse<bool>.Ok(true, "Acesso autorizado.");

        var atual = usuarioContextService.GetClienteId();
        if (atual.HasValue && atual.Value == clienteId) return ApiResponse<bool>.Ok(true, "Acesso autorizado.");

        await auditService.LogAsync(usuarioContextService.GetUsuarioId(), "seguranca", "acesso_negado_cliente", "segurança", clienteId, "Bloqueio por isolamento de cliente.", null, null, null, null, null);
        return ApiResponse<bool>.Fail("Acesso negado ao cliente informado.", 403);
    }
}

public sealed class PermissionGuardService
{
    private readonly UsuarioContextService usuarioContextService;

    public PermissionGuardService(UsuarioContextService usuarioContextService)
    {
        this.usuarioContextService = usuarioContextService;
    }

    public bool HasAnyRole(params string[] roles)
    {
        var atuais = usuarioContextService.GetRoles();
        return roles.Any(role => atuais.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase)));
    }
}

public sealed class AssinaturaGuardService
{
    private readonly IConfiguration cfg;
    private readonly ILogger<AssinaturaGuardService> logger;

    public AssinaturaGuardService(IConfiguration cfg, ILogger<AssinaturaGuardService> logger)
    {
        this.cfg = cfg;
        this.logger = logger;
    }

    public async Task<ApiResponse<bool>> PodeUsarMobile(Guid clienteId)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var assinatura = await cn.QueryFirstOrDefaultAsync<(string Status, Guid PlanoId)>(@"select a.status as Status, a.plano_id as PlanoId
from plantaopro.assinaturas a
where a.cliente_id=@clienteId and a.reg_status='A'
order by a.reg_date desc
limit 1", new { clienteId });

            if (assinatura == default) return ApiResponse<bool>.Fail("Cliente sem assinatura ativa para uso mobile.", 403);

            if (string.Equals(assinatura.Status, "suspensa", StringComparison.OrdinalIgnoreCase)
                || string.Equals(assinatura.Status, "cancelada", StringComparison.OrdinalIgnoreCase)
                || string.Equals(assinatura.Status, "vencida", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<bool>.Fail("Assinatura sem permissão de operação mobile no momento.", 403);
            }

            var permiteMobile = await cn.ExecuteScalarAsync<bool?>("select permite_mobile from plantaopro.planos where id=@id and reg_status='A'", new { id = assinatura.PlanoId });
            if (permiteMobile != true) return ApiResponse<bool>.Fail("Seu plano atual não permite acesso mobile.", 403);

            return ApiResponse<bool>.Ok(true, "Acesso mobile permitido.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao validar acesso mobile por assinatura. cliente:{ClienteId}", clienteId);
            return ApiResponse<bool>.Fail("Não foi possível validar permissão mobile no momento.", 500);
        }
    }
}
