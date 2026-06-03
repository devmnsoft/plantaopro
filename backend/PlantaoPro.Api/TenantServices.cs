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

    public string? GetIpOrigem()
    {
        return httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
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
        var perfilSeguro = string.IsNullOrWhiteSpace(perfil) ? "sem-perfil" : perfil;
        var ip = usuarioContextService.GetIpOrigem();
        await auditService.RegistrarAsync(usuarioId, clienteId, entidade, entidadeId, acao, new { motivo }, false, ip, perfilSeguro);

        try
        {
            using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync(@"insert into plantaopro.acessos_negados_log(id, usuario_id, cliente_id, entidade, entidade_id, motivo, ip, perfil, reg_date, reg_status)
values (gen_random_uuid(), @usuarioId, @clienteId, @entidade, @entidadeId, @motivo, @ip, @perfil, now(), 'A')", new
            {
                usuarioId,
                clienteId,
                entidade,
                entidadeId,
                motivo = MascararMotivo(motivo),
                ip,
                perfil = perfilSeguro
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao registrar acesso negado em tabela operacional UsuarioId={UsuarioId} Entidade={Entidade} EntidadeId={EntidadeId}", usuarioId, entidade, entidadeId);
        }
    }

    private static string MascararMotivo(string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo)) return "Acesso negado.";
        var termosSensiveis = new[] { "senha", "password", "token", "hash", "secret", "segredo", "authorization" };
        foreach (var termo in termosSensiveis)
        {
            if (motivo.IndexOf(termo, StringComparison.OrdinalIgnoreCase) >= 0) return "[DADO_SENSIVEL_MASCARADO]";
        }
        return motivo.Length > 500 ? motivo.Substring(0, 500) : motivo;
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
        ["ADMINISTRADOR"] = new[] { PermissionConstants.MedicosVer, PermissionConstants.MedicosCriar, PermissionConstants.MedicosEditar, PermissionConstants.MedicosInativar, PermissionConstants.HospitaisVer, PermissionConstants.HospitaisCriar, PermissionConstants.HospitaisEditar, PermissionConstants.PlantoesVer, PermissionConstants.PlantoesCriar, PermissionConstants.PlantoesEditar, PermissionConstants.PlantoesPublicar, PermissionConstants.PlantoesCancelar, PermissionConstants.EscalasVer, PermissionConstants.EscalasConfirmar, PermissionConstants.EscalasRecusar, PermissionConstants.EscalasCancelar, PermissionConstants.FinanceiroVer, PermissionConstants.FinanceiroConfirmar, PermissionConstants.FinanceiroCancelar, PermissionConstants.UsuariosGerenciar, PermissionConstants.ClientesGerenciar, PermissionConstants.RelatoriosVer, PermissionConstants.AuditoriaVer, PermissionConstants.ConfiguracoesEditar, PermissionConstants.SuporteVer },
        ["COORDENACAO"] = new[] { PermissionConstants.MedicosVer, PermissionConstants.HospitaisVer, PermissionConstants.PlantoesVer, PermissionConstants.PlantoesCriar, PermissionConstants.PlantoesEditar, PermissionConstants.PlantoesPublicar, PermissionConstants.PlantoesCancelar, PermissionConstants.EscalasVer, PermissionConstants.EscalasConfirmar, PermissionConstants.EscalasRecusar, PermissionConstants.EscalasCancelar, PermissionConstants.RelatoriosVer },
        ["OPERADOR"] = new[] { PermissionConstants.MedicosVer, PermissionConstants.HospitaisVer, PermissionConstants.PlantoesVer, PermissionConstants.EscalasVer, PermissionConstants.EscalasConfirmar, PermissionConstants.EscalasRecusar },
        ["FINANCEIRO"] = new[] { PermissionConstants.FinanceiroVer, PermissionConstants.FinanceiroConfirmar, PermissionConstants.FinanceiroCancelar, PermissionConstants.RelatoriosVer },
        ["MEDICO"] = new[] { PermissionConstants.PlantoesVer, PermissionConstants.EscalasVer, PermissionConstants.FinanceiroVer },
        ["HOSPITAL"] = new[] { PermissionConstants.PlantoesVer, PermissionConstants.EscalasVer }
    };

    private readonly UsuarioContextService usuarioContextService;
    private readonly TenantGuardService tenantGuardService;
    private readonly IConfiguration cfg;
    private readonly ILogger<PermissionGuardService> logger;

    public PermissionGuardService(UsuarioContextService usuarioContextService, TenantGuardService tenantGuardService, IConfiguration cfg, ILogger<PermissionGuardService> logger)
    {
        this.usuarioContextService = usuarioContextService;
        this.tenantGuardService = tenantGuardService;
        this.cfg = cfg;
        this.logger = logger;
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
        if (TemPermissao(permissao))
        {
            await RegistrarPermissaoLogAsync(permissao, true, null);
            return ApiResponse<bool>.Ok(true, "Permissão autorizada.");
        }

        await RegistrarPermissaoLogAsync(permissao, false, "Permissão insuficiente.");
        await tenantGuardService.RegistrarAcessoNegadoAsync(AuditoriaConstants.Entidades.Permissao, null, AuditoriaConstants.Acoes.BloqueioPermissao, "Permissão requerida: " + permissao);
        return ApiResponse<bool>.Fail("Você não possui permissão para executar esta ação.", 403);
    }

    private async Task RegistrarPermissaoLogAsync(string permissao, bool autorizado, string? motivo)
    {
        try
        {
            using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync(@"insert into plantaopro.permissao_logs(id, usuario_id, cliente_id, permissao, autorizado, motivo, reg_date, reg_status)
values (gen_random_uuid(), @usuarioId, @clienteId, @permissao, @autorizado, @motivo, now(), 'A')", new
            {
                usuarioId = usuarioContextService.GetUsuarioId(),
                clienteId = usuarioContextService.GetClienteId(),
                permissao,
                autorizado,
                motivo
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao registrar log de permissão Permissao={Permissao} Autorizado={Autorizado}", permissao, autorizado);
        }
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

    public Task<ApiResponse<bool>> PodeCadastrarMedico(Guid clienteId) => ValidarLimiteAsync(clienteId, "medicos", "Limite de médicos do plano atingido.");

    public Task<ApiResponse<bool>> PodeCadastrarHospital(Guid clienteId) => ValidarLimiteAsync(clienteId, "hospitais", "Limite de hospitais do plano atingido.");

    public Task<ApiResponse<bool>> PodePublicarPlantao(Guid clienteId) => ValidarLimiteAsync(clienteId, "plantoes", "Limite mensal de plantões do plano atingido.");

    public Task<ApiResponse<bool>> PodeUsarMobile(Guid clienteId) => ValidarFuncionalidadeAsync(clienteId, "mobile", "Seu plano atual não permite acesso mobile.");

    public Task<ApiResponse<bool>> PodeUsarBi(Guid clienteId) => ValidarFuncionalidadeAsync(clienteId, "bi", "Seu plano atual não permite BI.");

    public Task<ApiResponse<bool>> PodeUsarRelatoriosAvancados(Guid clienteId) => ValidarFuncionalidadeAsync(clienteId, "relatorios", "Seu plano atual não permite relatórios avançados.");

    public Task<ApiResponse<bool>> PodeUsarIntegracoes(Guid clienteId) => ValidarFuncionalidadeAsync(clienteId, "integracoes", "Seu plano atual não permite integrações.");

    public async Task<ApiResponse<UsoPlanoDto>> ObterUsoPlano(Guid clienteId)
    {
        try
        {
            using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var clienteStatus = await cn.ExecuteScalarAsync<string?>("select upper(coalesce(status,'')) from plantaopro.clientes where id=@clienteId and reg_status='A'", new { clienteId });
            if (string.Equals(clienteStatus, "SUSPENSO", StringComparison.OrdinalIgnoreCase)) return ApiResponse<UsoPlanoDto>.Fail("Cliente suspenso não pode operar até regularização.", 403);
            if (string.Equals(clienteStatus, "CANCELADO", StringComparison.OrdinalIgnoreCase)) return ApiResponse<UsoPlanoDto>.Fail("Cliente cancelado não pode operar.", 403);

            var uso = await cn.QueryFirstOrDefaultAsync<UsoPlanoDto>(@"select a.cliente_id as ""ClienteId"", a.id as ""AssinaturaId"", p.id as ""PlanoId"", coalesce(p.nome,'') as ""PlanoNome"", coalesce(a.status,'') as ""AssinaturaStatus"",
       (select count(1)::int from plantaopro.medicos m where m.cliente_id=a.cliente_id and m.reg_status='A') as ""MedicosUsados"",
       coalesce(p.limite_medicos,0) as ""MedicosLimite"",
       (select count(1)::int from plantaopro.hospitais h where h.cliente_id=a.cliente_id and h.reg_status='A') as ""HospitaisUsados"",
       coalesce(p.limite_hospitais,0) as ""HospitaisLimite"",
       (select count(1)::int from plantaopro.plantoes pl where pl.cliente_id=a.cliente_id and pl.reg_status='A' and date_trunc('month',pl.data_inicio)=date_trunc('month',now())) as ""PlantoesMesUsados"",
       coalesce(p.limite_plantoes_mes,0) as ""PlantoesMesLimite"",
       coalesce(p.permite_mobile,p.permite_api,false) as ""PermiteMobile"",
       coalesce(p.permite_bi,p.permite_relatorios,false) as ""PermiteBi"",
       coalesce(p.permite_relatorios_avancados,p.permite_relatorios,false) as ""PermiteRelatoriosAvancados"",
       coalesce(p.permite_integracoes,p.permite_api,false) as ""PermiteIntegracoes""
from plantaopro.assinaturas a
join plantaopro.planos p on p.id=a.plano_id
where a.cliente_id=@clienteId and a.reg_status='A'
order by case when upper(a.status)='ATIVA' then 0 else 1 end, a.reg_date desc
limit 1", new { clienteId });

            return uso is null
                ? ApiResponse<UsoPlanoDto>.Fail("Cliente sem assinatura cadastrada.", 403)
                : ApiResponse<UsoPlanoDto>.Ok(uso, "Uso do plano carregado.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter uso do plano. cliente:{ClienteId}", clienteId);
            return ApiResponse<UsoPlanoDto>.Fail("Não foi possível validar uso do plano no momento.", 500);
        }
    }

    private async Task<ApiResponse<bool>> ValidarFuncionalidadeAsync(Guid clienteId, string funcionalidade, string mensagemBloqueio)
    {
        var usoResponse = await ObterUsoPlano(clienteId);
        if (!usoResponse.Success || usoResponse.Data is null) return ApiResponse<bool>.Fail(usoResponse.Message, usoResponse.StatusCode);
        var uso = usoResponse.Data;
        var statusOk = string.Equals(uso.AssinaturaStatus, "ATIVA", StringComparison.OrdinalIgnoreCase);
        if (!statusOk) return ApiResponse<bool>.Fail("Assinatura sem permissão de operação no momento.", 403);

        var permitido = funcionalidade switch
        {
            "mobile" => uso.PermiteMobile,
            "bi" => uso.PermiteBi,
            "relatorios" => uso.PermiteRelatoriosAvancados,
            "integracoes" => uso.PermiteIntegracoes,
            _ => false
        };

        return permitido ? ApiResponse<bool>.Ok(true, "Funcionalidade permitida.") : ApiResponse<bool>.Fail(mensagemBloqueio, 403);
    }

    private async Task<ApiResponse<bool>> ValidarLimiteAsync(Guid clienteId, string limite, string mensagemBloqueio)
    {
        var usoResponse = await ObterUsoPlano(clienteId);
        if (!usoResponse.Success || usoResponse.Data is null) return ApiResponse<bool>.Fail(usoResponse.Message, usoResponse.StatusCode);
        var uso = usoResponse.Data;
        if (!string.Equals(uso.AssinaturaStatus, "ATIVA", StringComparison.OrdinalIgnoreCase)) return ApiResponse<bool>.Fail("Cliente sem assinatura ativa para operar.", 403);

        var permitido = limite switch
        {
            "medicos" => uso.MedicosLimite <= 0 || uso.MedicosUsados < uso.MedicosLimite,
            "hospitais" => uso.HospitaisLimite <= 0 || uso.HospitaisUsados < uso.HospitaisLimite,
            "plantoes" => uso.PlantoesMesLimite <= 0 || uso.PlantoesMesUsados < uso.PlantoesMesLimite,
            _ => false
        };

        return permitido ? ApiResponse<bool>.Ok(true, "Limite disponível.") : ApiResponse<bool>.Fail(mensagemBloqueio, 403);
    }
}
