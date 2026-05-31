using System.Security.Claims;
using Dapper;
using System.Collections.Generic;
using Npgsql;
using PlantaoPro.Api.Models;
using PlantaoPro.Api.Data;

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
    private readonly IConfiguration cfg;
    private readonly UsuarioContextService usuarioContextService;
    private readonly IAuditService auditService;
    private readonly ILogger<TenantGuardService> logger;

    public TenantGuardService(
        IConfiguration cfg,
        UsuarioContextService usuarioContextService,
        IAuditService auditService,
        ILogger<TenantGuardService> logger)
    {
        this.cfg = cfg;
        this.usuarioContextService = usuarioContextService;
        this.auditService = auditService;
        this.logger = logger;
    }

    public async Task<ApiResponse<bool>> ValidarAcessoClienteAsync(Guid clienteId)
    {
        var usuarioId = usuarioContextService.GetUsuarioId();
        if (usuarioId.HasValue && await PodeAcessarClienteAsync(usuarioId.Value, clienteId)) return ApiResponse<bool>.Ok(true, "Acesso autorizado.");
        await RegistrarAcessoNegadoAsync(AuditoriaConstants.Entidades.Cliente, clienteId, AuditoriaConstants.Acoes.BloqueioTenant, "Bloqueio por isolamento de cliente.");
        return ApiResponse<bool>.Fail("Acesso negado ao cliente informado.", 403);
    }

    public Task<bool> PodeAcessarClienteAsync(Guid usuarioId, Guid clienteId)
    {
        if (usuarioContextService.IsAdminGlobal()) return Task.FromResult(true);
        var atual = usuarioContextService.GetClienteId();
        return Task.FromResult(atual.HasValue && atual.Value == clienteId);
    }

    public Task<bool> PodeAcessarMedicoAsync(Guid usuarioId, Guid medicoId)
    {
        return PodeAcessarEntidadePorClienteAsync(usuarioId, medicoId, "medicos", "id");
    }

    public Task<bool> PodeAcessarHospitalAsync(Guid usuarioId, Guid hospitalId)
    {
        return PodeAcessarEntidadePorClienteAsync(usuarioId, hospitalId, "hospitais", "id");
    }

    public Task<bool> PodeAcessarPlantaoAsync(Guid usuarioId, Guid plantaoId)
    {
        return PodeAcessarEntidadePorClienteAsync(usuarioId, plantaoId, "plantoes", "id");
    }

    public Task<bool> PodeAcessarEscalaAsync(Guid usuarioId, Guid escalaId)
    {
        return PodeAcessarEntidadePorClienteAsync(usuarioId, escalaId, "escalas", "id");
    }

    public Task<bool> PodeAcessarPagamentoAsync(Guid usuarioId, Guid pagamentoId)
    {
        return PodeAcessarEntidadePorClienteAsync(usuarioId, pagamentoId, "pagamentos", "id");
    }

    public async Task RegistrarAcessoNegadoAsync(string entidade, Guid? entidadeId, string acao, string motivo)
    {
        var usuarioId = usuarioContextService.GetUsuarioId();
        var clienteId = usuarioContextService.GetClienteId();
        var perfil = string.Join(',', usuarioContextService.GetRoles());
        logger.LogWarning("Acesso negado UsuarioId={UsuarioId} ClienteId={ClienteId} Perfil={Perfil} Entidade={Entidade} EntidadeId={EntidadeId} Motivo={Motivo}", usuarioId, clienteId, perfil, entidade, entidadeId, motivo);
        await auditService.RegistrarAsync(usuarioId, clienteId, entidade, entidadeId, acao, new { motivo }, false, null, string.IsNullOrWhiteSpace(perfil) ? "sem-perfil" : perfil);
    }

    private async Task<bool> PodeAcessarEntidadePorClienteAsync(Guid usuarioId, Guid entidadeId, string tabela, string colunaId)
    {
        if (usuarioContextService.IsAdminGlobal()) return true;
        var clienteId = usuarioContextService.GetClienteId();
        if (!clienteId.HasValue) return false;

        try
        {
            using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var sql = "select count(1) from plantaopro." + tabela + " where " + colunaId + "=@entidadeId and cliente_id=@clienteId and coalesce(reg_status,'A') <> 'E'";
            var total = await cn.ExecuteScalarAsync<int>(sql, new { entidadeId, clienteId = clienteId.Value });
            return total > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao validar tenant Tabela={Tabela} EntidadeId={EntidadeId} UsuarioId={UsuarioId}", tabela, entidadeId, usuarioId);
            return false;
        }
    }
}

public sealed class PermissionGuardService
{
    private static readonly Dictionary<string, string[]> FallbackPermissoes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["ADMINISTRADOR_GLOBAL"] = new[] { "*" },
        ["ADMINISTRADOR"] = new[] { PermissionConstants.MedicosVer, PermissionConstants.MedicosCriar, PermissionConstants.MedicosEditar, PermissionConstants.MedicosInativar, PermissionConstants.HospitaisVer, PermissionConstants.HospitaisCriar, PermissionConstants.HospitaisEditar, PermissionConstants.PlantoesVer, PermissionConstants.PlantoesCriar, PermissionConstants.PlantoesEditar, PermissionConstants.PlantoesPublicar, PermissionConstants.PlantoesCancelar, PermissionConstants.EscalasVer, PermissionConstants.EscalasConfirmar, PermissionConstants.EscalasRecusar, PermissionConstants.EscalasCancelar, PermissionConstants.FinanceiroVer, PermissionConstants.FinanceiroConfirmar, PermissionConstants.FinanceiroCancelar, PermissionConstants.UsuariosGerenciar, PermissionConstants.RelatoriosVer, PermissionConstants.AuditoriaVer, PermissionConstants.ConfiguracoesEditar, PermissionConstants.SuporteVer },
        ["COORDENACAO"] = new[] { PermissionConstants.MedicosVer, PermissionConstants.HospitaisVer, PermissionConstants.PlantoesVer, PermissionConstants.PlantoesCriar, PermissionConstants.PlantoesEditar, PermissionConstants.PlantoesPublicar, PermissionConstants.PlantoesCancelar, PermissionConstants.EscalasVer, PermissionConstants.EscalasConfirmar, PermissionConstants.EscalasRecusar, PermissionConstants.EscalasCancelar, PermissionConstants.RelatoriosVer },
        ["OPERADOR"] = new[] { PermissionConstants.MedicosVer, PermissionConstants.HospitaisVer, PermissionConstants.PlantoesVer, PermissionConstants.EscalasVer, PermissionConstants.EscalasConfirmar, PermissionConstants.EscalasRecusar },
        ["FINANCEIRO"] = new[] { PermissionConstants.FinanceiroVer, PermissionConstants.FinanceiroConfirmar, PermissionConstants.FinanceiroCancelar, PermissionConstants.RelatoriosVer },
        ["MEDICO"] = new[] { PermissionConstants.PlantoesVer, PermissionConstants.EscalasVer, PermissionConstants.FinanceiroVer },
        ["HOSPITAL"] = new[] { PermissionConstants.PlantoesVer, PermissionConstants.EscalasVer }
    };

    private readonly UsuarioContextService usuarioContextService;
    private readonly TenantGuardService tenantGuardService;

    public PermissionGuardService(UsuarioContextService usuarioContextService, TenantGuardService tenantGuardService)
    {
        this.usuarioContextService = usuarioContextService;
        this.tenantGuardService = tenantGuardService;
    }

    public bool HasAnyRole(params string[] roles)
    {
        var atuais = usuarioContextService.GetRoles();
        return roles.Any(role => atuais.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase)));
    }

    public bool TemPermissao(string permissao)
    {
        var roles = usuarioContextService.GetRoles();
        foreach (var role in roles)
        {
            if (FallbackPermissoes.TryGetValue(role, out var permissoes) && (permissoes.Contains("*") || permissoes.Contains(permissao, StringComparer.OrdinalIgnoreCase))) return true;
        }
        return false;
    }

    public async Task<ApiResponse<bool>> ValidarPermissaoAsync(string permissao)
    {
        if (TemPermissao(permissao)) return ApiResponse<bool>.Ok(true, "Permissão autorizada.");
        await tenantGuardService.RegistrarAcessoNegadoAsync(AuditoriaConstants.Entidades.Permissao, null, AuditoriaConstants.Acoes.BloqueioPermissao, "Permissão requerida: " + permissao);
        return ApiResponse<bool>.Fail("Você não possui permissão para executar esta ação.", 403);
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
