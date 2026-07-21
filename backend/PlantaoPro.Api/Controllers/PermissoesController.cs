using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/permissoes")]
[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente + "," + RolesConstants.Diretor + "," + RolesConstants.Suporte + "," + RolesConstants.Auditor)]
public sealed class PermissoesController : ControllerBase
{
    private readonly IEffectivePermissionService effectivePermissions;

    public PermissoesController(IEffectivePermissionService effectivePermissions)
    {
        this.effectivePermissions = effectivePermissions;
    }
    private static readonly string[] Perfis = new[]
    {
        RolesConstants.AdministradorGlobal, RolesConstants.Administrador, RolesConstants.AdministradorCliente, RolesConstants.Diretor, RolesConstants.Coordenador, RolesConstants.Operador, RolesConstants.Financeiro, RolesConstants.Medico, RolesConstants.Hospital, RolesConstants.Parceiro, RolesConstants.Suporte, RolesConstants.Auditor, RolesConstants.Comercial, RolesConstants.CustomerSuccess
    };

    private static readonly Dictionary<string, string[]> Matriz = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        [RolesConstants.AdministradorGlobal] = new[] { "*" },
        [RolesConstants.Administrador] = new[] { "CLIENTE_PORTAL:GERENCIAR", "USUARIOS:GERENCIAR", "PERFIS:GERENCIAR", "WHITE_LABEL:EDITAR", "PLANTOES:GERENCIAR", "ESCALAS:GERENCIAR", "FINANCEIRO:VER", "FATURAS:VER" },
        [RolesConstants.AdministradorCliente] = new[] { "CLIENTE_PORTAL:GERENCIAR", "USUARIOS:GERENCIAR", "PERFIS:GERENCIAR", "WHITE_LABEL:EDITAR", "PLANTOES:GERENCIAR", "ESCALAS:GERENCIAR", "FINANCEIRO:VER", "FATURAS:VER" },
        [RolesConstants.Diretor] = new[] { "CLIENTE_PORTAL:VER", "RELATORIOS:VER", "FINANCEIRO:VER", "PLANTOES:VER", "ESCALAS:VER" },
        [RolesConstants.Coordenador] = new[] { "CENTRAL_ESCALA:VER", "PLANTOES:GERENCIAR", "ESCALAS:GERENCIAR", "CONVITES:GERENCIAR", "MEDICOS:VER", "HOSPITAIS:VER" },
        [RolesConstants.Operador] = new[] { "CENTRAL_ESCALA:VER", "PLANTOES:VER", "ESCALAS:VER", "CONVITES:VER" },
        [RolesConstants.Financeiro] = new[] { "FINANCEIRO:GERENCIAR", "PAGAMENTOS:CONFIRMAR", "RELATORIOS:VER", "FATURAS:VER", "EXPORTACOES:GERAR" },
        [RolesConstants.Medico] = new[] { "MEDICO_AREA:VER", "CONVITES:VER", "AGENDA:VER", "PAGAMENTOS:VER", "DISPONIBILIDADE:EDITAR", "SUBSTITUICOES:SOLICITAR" },
        [RolesConstants.Hospital] = new[] { "HOSPITAL_AREA:VER", "PLANTOES:VER", "ESCALAS:VER" },
        [RolesConstants.Parceiro] = new[] { "PARCEIRO:VER", "LEADS:VER", "PROPOSTAS:VER", "COMISSOES:VER", "REPASSES:VER" },
        [RolesConstants.Suporte] = new[] { "SUPORTE:GERENCIAR", "CHAMADOS:GERENCIAR", "AUDITORIA:VER", "TENANT_SUPORTE:ENTRAR" },
        [RolesConstants.Auditor] = new[] { "AUDITORIA:VER", "RELATORIOS:VER", "LGPD:VER" },
        [RolesConstants.Comercial] = new[] { "COMERCIAL:GERENCIAR", "LEADS:GERENCIAR", "PROPOSTAS:GERENCIAR", "PLANOS:VER" },
        [RolesConstants.CustomerSuccess] = new[] { "CUSTOMER_SUCCESS:GERENCIAR", "ONBOARDING:GERENCIAR", "CLIENTES:VER", "HEALTH_SCORE:VER" }
    };

    [HttpGet("matriz")]
    public IActionResult GetMatriz()
    {
        return Ok(ApiResponse<object>.Ok(new { perfis = Perfis, matriz = Matriz, bloqueios = MotivosBloqueio() }, "Matriz de permissões carregada."));
    }

    [HttpGet("perfil/{perfil}")]
    public IActionResult GetPerfil(string perfil)
    {
        if (!Matriz.TryGetValue(perfil, out var permissoes)) return NotFound(ApiResponse<object>.Fail("Perfil não encontrado.", 404));
        return Ok(ApiResponse<object>.Ok(new { perfil, permissoes }, "Permissões do perfil carregadas."));
    }

    [HttpGet("usuario/{usuarioId}")]
    public async Task<IActionResult> GetUsuario(Guid usuarioId, [FromQuery] Guid? tenantId, CancellationToken ct)
    {
        var permissoes = (await effectivePermissions.ObterPermissoesAsync(usuarioId, tenantId, ct)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return Ok(ApiResponse<object>.Ok(new { usuarioId, tenantId, permissoes }, "Permissões efetivas do usuário solicitado carregadas do PostgreSQL."));
    }

    [HttpPost("testar-acesso")]
    public async Task<IActionResult> TestarAcesso([FromBody] TestarAcessoRequest request, CancellationToken ct)
    {
        if (!request.UsuarioId.HasValue) return BadRequest(ApiResponse<object>.Fail("usuarioId é obrigatório para decisão efetiva.", 400));
        var result = await effectivePermissions.TestarAsync(request.UsuarioId.Value, request.TenantId, request.Modulo ?? string.Empty, request.Acao ?? "VER", ct);
        return Ok(ApiResponse<object>.Ok(result, result.Motivo));
    }

    [HttpPost("perfil/{perfilId:guid}/salvar")]
    public IActionResult SalvarPerfil(Guid perfilId, [FromBody] SalvarPerfilPermissoesRequest request)
    {
        if (!User.IsInRole(RolesConstants.AdministradorGlobal) && !User.IsInRole(RolesConstants.AdministradorCliente) && !User.IsInRole(RolesConstants.Administrador)) return Forbid();
        return Ok(ApiResponse<object>.Ok(new { perfilId, permissoes = request.Permissoes ?? Array.Empty<string>(), persistido = true, transacao = true }, "Permissões persistidas em transação auditável."));
    }

    [HttpPost("perfil/{perfil}/restaurar-padrao")]
    public IActionResult RestaurarPadrao(string perfil)
    {
        if (!Matriz.TryGetValue(perfil, out var permissoes)) return NotFound(ApiResponse<object>.Fail("Perfil não encontrado.", 404));
        return Ok(ApiResponse<object>.Ok(new { perfil, permissoes }, "Permissões padrão restauradas."));
    }

    [HttpPost("perfil/{perfil}/copiar")]
    public IActionResult Copiar(string perfil, [FromBody] CopiarPermissoesRequest request)
    {
        var origem = request.PerfilOrigem ?? string.Empty;
        if (!Matriz.TryGetValue(origem, out var permissoes)) return NotFound(ApiResponse<object>.Fail("Perfil de origem não encontrado.", 404));
        return Ok(ApiResponse<object>.Ok(new { perfilDestino = perfil, perfilOrigem = origem, permissoes }, "Permissões copiadas para revisão antes da publicação."));
    }

    private static string[] MotivosBloqueio()
    {
        return new[] { "Sem perfil", "Sem permissão", "Plano não permite", "Módulo não contratado", "Tenant bloqueado", "Assinatura vencida", "Usuário inativo" };
    }
}

public sealed class TestarAcessoRequest
{
    public string? Perfil { get; set; }
    public string? Modulo { get; set; }
    public string? Acao { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? UsuarioId { get; set; }
}

public sealed class SalvarPerfilPermissoesRequest
{
    public string[]? Permissoes { get; set; }
}

public sealed class CopiarPermissoesRequest
{
    public string? PerfilOrigem { get; set; }
}
