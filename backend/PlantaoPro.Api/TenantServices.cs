using System.Security.Claims;
using Dapper;
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

    public string? GetPerfilPrincipal()
    {
        return GetRoles().FirstOrDefault();
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

    public string? GetIp()
    {
        return httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }

    public bool IsAdminGlobal()
    {
        return GetRoles().Any(r => string.Equals(r, RolesConstants.AdministradorGlobal, StringComparison.OrdinalIgnoreCase));
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
        if (await PodeAcessarClienteAsync(usuarioId ?? Guid.Empty, clienteId)) return ApiResponse<bool>.Ok(true, "Acesso autorizado.");

        await RegistrarAcessoNegadoAsync(usuarioId, clienteId, AuditoriaConstants.Entidades.Cliente, clienteId, AuditoriaConstants.Acoes.BloqueioTenant, "Bloqueio por isolamento de cliente.");
        return ApiResponse<bool>.Fail("Acesso negado ao cliente informado.", 403);
    }

    public Task<bool> PodeAcessarClienteAsync(Guid usuarioId, Guid clienteId)
    {
        if (usuarioContextService.IsAdminGlobal()) return Task.FromResult(true);

        var atual = usuarioContextService.GetClienteId();
        return Task.FromResult(atual.HasValue && atual.Value == clienteId);
    }

    public async Task<bool> PodeAcessarMedicoAsync(Guid usuarioId, Guid medicoId)
    {
        if (usuarioContextService.IsAdminGlobal()) return true;
        var clienteId = await BuscarClienteIdAsync("plantaopro.medicos", medicoId);
        if (clienteId.HasValue && !await PodeAcessarClienteAsync(usuarioId, clienteId.Value)) return false;

        if (TemPerfil(RolesConstants.Medico))
        {
            using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var vinculado = await cn.ExecuteScalarAsync<bool>("select exists(select 1 from plantaopro.medicos where id=@medicoId and usuario_id=@usuarioId)", new { medicoId, usuarioId });
            return vinculado;
        }

        return clienteId.HasValue;
    }

    public async Task<bool> PodeAcessarHospitalAsync(Guid usuarioId, Guid hospitalId)
    {
        if (usuarioContextService.IsAdminGlobal()) return true;
        var clienteId = await BuscarClienteIdAsync("plantaopro.hospitais", hospitalId);
        if (!clienteId.HasValue || !await PodeAcessarClienteAsync(usuarioId, clienteId.Value)) return false;
        if (!TemPerfil(RolesConstants.Hospital)) return true;

        using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        return await cn.ExecuteScalarAsync<bool>("select exists(select 1 from plantaopro.hospitais where id=@hospitalId and usuario_id=@usuarioId)", new { hospitalId, usuarioId });
    }

    public Task<bool> PodeAcessarPlantaoAsync(Guid usuarioId, Guid plantaoId) => PodeAcessarEntidadePorClienteAsync(usuarioId, "plantaopro.plantoes", plantaoId);
    public Task<bool> PodeAcessarEscalaAsync(Guid usuarioId, Guid escalaId) => PodeAcessarEntidadePorClienteAsync(usuarioId, "plantaopro.escalas", escalaId);
    public Task<bool> PodeAcessarPagamentoAsync(Guid usuarioId, Guid pagamentoId) => PodeAcessarEntidadePorClienteAsync(usuarioId, "plantaopro.pagamentos", pagamentoId);

    public async Task RegistrarAcessoNegadoAsync(Guid? usuarioId, Guid? clienteId, string entidade, Guid? entidadeId, string acao, string motivo)
    {
        logger.LogWarning("Acesso negado UsuarioId={UsuarioId} ClienteId={ClienteId} Entidade={Entidade} EntidadeId={EntidadeId} Motivo={Motivo}", usuarioId, clienteId, entidade, entidadeId, motivo);
        await auditService.RegistrarAsync(usuarioId, clienteId, entidade, entidadeId, acao, new { motivo }, false, usuarioContextService.GetIp(), usuarioContextService.GetPerfilPrincipal());
    }

    private async Task<bool> PodeAcessarEntidadePorClienteAsync(Guid usuarioId, string tabela, Guid id)
    {
        if (usuarioContextService.IsAdminGlobal()) return true;
        var clienteId = await BuscarClienteIdAsync(tabela, id);
        return clienteId.HasValue && await PodeAcessarClienteAsync(usuarioId, clienteId.Value);
    }

    private async Task<Guid?> BuscarClienteIdAsync(string tabela, Guid id)
    {
        using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        return await cn.ExecuteScalarAsync<Guid?>($"select cliente_id from {tabela} where id=@id limit 1", new { id });
    }

    private bool TemPerfil(string perfil)
    {
        return usuarioContextService.GetRoles().Any(x => string.Equals(x, perfil, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class PermissionGuardService
{
    private readonly UsuarioContextService usuarioContextService;
    private static readonly Dictionary<string, string[]> FallbackPermissoes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        [RolesConstants.AdministradorGlobal] = Permissoes.Todas,
        [RolesConstants.Administrador] = Permissoes.AdminCliente,
        [RolesConstants.Coordenacao] = Permissoes.Coordenacao,
        [RolesConstants.Operador] = Permissoes.Operador,
        [RolesConstants.Financeiro] = Permissoes.Financeiro,
        [RolesConstants.Medico] = Permissoes.Medico,
        [RolesConstants.Hospital] = Permissoes.Hospital
    };

    public PermissionGuardService(UsuarioContextService usuarioContextService)
    {
        this.usuarioContextService = usuarioContextService;
    }

    public bool HasAnyRole(params string[] roles)
    {
        var atuais = usuarioContextService.GetRoles();
        return roles.Any(role => atuais.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase)));
    }

    public bool HasPermission(string permissao)
    {
        var roles = usuarioContextService.GetRoles();
        return roles.Any(role => FallbackPermissoes.TryGetValue(role, out var permissoes)
            && permissoes.Any(x => string.Equals(x, permissao, StringComparison.OrdinalIgnoreCase)));
    }
}

public static class Permissoes
{
    public const string MedicosVer = "MEDICOS_VER";
    public const string MedicosCriar = "MEDICOS_CRIAR";
    public const string MedicosEditar = "MEDICOS_EDITAR";
    public const string MedicosInativar = "MEDICOS_INATIVAR";
    public const string HospitaisVer = "HOSPITAIS_VER";
    public const string HospitaisCriar = "HOSPITAIS_CRIAR";
    public const string HospitaisEditar = "HOSPITAIS_EDITAR";
    public const string PlantoesVer = "PLANTOES_VER";
    public const string PlantoesCriar = "PLANTOES_CRIAR";
    public const string PlantoesEditar = "PLANTOES_EDITAR";
    public const string PlantoesPublicar = "PLANTOES_PUBLICAR";
    public const string PlantoesCancelar = "PLANTOES_CANCELAR";
    public const string EscalasVer = "ESCALAS_VER";
    public const string EscalasConfirmar = "ESCALAS_CONFIRMAR";
    public const string EscalasRecusar = "ESCALAS_RECUSAR";
    public const string EscalasCancelar = "ESCALAS_CANCELAR";
    public const string FinanceiroVer = "FINANCEIRO_VER";
    public const string FinanceiroConfirmar = "FINANCEIRO_CONFIRMAR";
    public const string FinanceiroCancelar = "FINANCEIRO_CANCELAR";
    public const string UsuariosGerenciar = "USUARIOS_GERENCIAR";
    public const string ClientesGerenciar = "CLIENTES_GERENCIAR";
    public const string PlanosGerenciar = "PLANOS_GERENCIAR";
    public const string AssinaturasGerenciar = "ASSINATURAS_GERENCIAR";
    public const string RelatoriosVer = "RELATORIOS_VER";
    public const string AuditoriaVer = "AUDITORIA_VER";
    public const string ObservabilidadeVer = "OBSERVABILIDADE_VER";
    public const string ConfiguracoesEditar = "CONFIGURACOES_EDITAR";
    public const string SuporteVer = "SUPORTE_VER";

    public static readonly string[] Todas = new[] { MedicosVer, MedicosCriar, MedicosEditar, MedicosInativar, HospitaisVer, HospitaisCriar, HospitaisEditar, PlantoesVer, PlantoesCriar, PlantoesEditar, PlantoesPublicar, PlantoesCancelar, EscalasVer, EscalasConfirmar, EscalasRecusar, EscalasCancelar, FinanceiroVer, FinanceiroConfirmar, FinanceiroCancelar, UsuariosGerenciar, ClientesGerenciar, PlanosGerenciar, AssinaturasGerenciar, RelatoriosVer, AuditoriaVer, ObservabilidadeVer, ConfiguracoesEditar, SuporteVer };
    public static readonly string[] AdminCliente = new[] { MedicosVer, MedicosCriar, MedicosEditar, MedicosInativar, HospitaisVer, HospitaisCriar, HospitaisEditar, PlantoesVer, PlantoesCriar, PlantoesEditar, PlantoesPublicar, PlantoesCancelar, EscalasVer, EscalasConfirmar, EscalasRecusar, EscalasCancelar, FinanceiroVer, FinanceiroConfirmar, FinanceiroCancelar, UsuariosGerenciar, RelatoriosVer, AuditoriaVer, ConfiguracoesEditar, SuporteVer };
    public static readonly string[] Coordenacao = new[] { MedicosVer, HospitaisVer, PlantoesVer, PlantoesCriar, PlantoesEditar, PlantoesPublicar, PlantoesCancelar, EscalasVer, EscalasConfirmar, EscalasRecusar, EscalasCancelar, RelatoriosVer, SuporteVer };
    public static readonly string[] Operador = new[] { MedicosVer, HospitaisVer, PlantoesVer, PlantoesCriar, PlantoesEditar, EscalasVer, EscalasConfirmar, EscalasRecusar, SuporteVer };
    public static readonly string[] Financeiro = new[] { FinanceiroVer, FinanceiroConfirmar, FinanceiroCancelar, RelatoriosVer };
    public static readonly string[] Medico = new[] { PlantoesVer, EscalasVer, EscalasConfirmar, EscalasRecusar, FinanceiroVer };
    public static readonly string[] Hospital = new[] { PlantoesVer, EscalasVer, RelatoriosVer };
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
            using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var assinatura = await cn.QueryFirstOrDefaultAsync<AssinaturaResumo>(@"select a.status as Status, a.plano_id as PlanoId
from plantaopro.assinaturas a
where a.cliente_id=@clienteId and a.reg_status='A'
order by a.reg_date desc
limit 1", new { clienteId });

            if (assinatura is null) return ApiResponse<bool>.Fail("Cliente sem assinatura ativa para uso mobile.", 403);

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

    public sealed class AssinaturaResumo
    {
        public string Status { get; set; } = string.Empty;
        public Guid PlanoId { get; set; }
    }
}
