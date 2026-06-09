using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/permissoes")]
[Authorize]
public sealed class PermissoesController : ControllerBase
{
    private static readonly string[] Perfis = new[]
    {
        "ADMINISTRADOR_GLOBAL", "ADMINISTRADOR", "ADMINISTRADOR_CLIENTE", "DIRETOR", "COORDENADOR", "OPERADOR", "FINANCEIRO", "MEDICO", "HOSPITAL", "PARCEIRO", "SUPORTE", "AUDITOR", "COMERCIAL", "CUSTOMER_SUCCESS"
    };

    private static readonly Dictionary<string, string[]> Matriz = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["ADMINISTRADOR_GLOBAL"] = new[] { "*" },
        ["ADMINISTRADOR"] = new[] { "CLIENTE_PORTAL:GERENCIAR", "USUARIOS:GERENCIAR", "WHITE_LABEL:EDITAR", "PLANTOES:GERENCIAR", "ESCALAS:GERENCIAR", "FINANCEIRO:VER" },
        ["ADMINISTRADOR_CLIENTE"] = new[] { "CLIENTE_PORTAL:GERENCIAR", "USUARIOS:GERENCIAR", "WHITE_LABEL:EDITAR", "PLANTOES:GERENCIAR", "ESCALAS:GERENCIAR", "FINANCEIRO:VER" },
        ["COORDENADOR"] = new[] { "PLANTOES:GERENCIAR", "ESCALAS:GERENCIAR", "CONVITES:GERENCIAR", "CENTRAL_ESCALA:VER", "MEDICOS:VER" },
        ["OPERADOR"] = new[] { "PLANTOES:VER", "ESCALAS:VER", "CONVITES:VER" },
        ["FINANCEIRO"] = new[] { "FINANCEIRO:GERENCIAR", "RELATORIOS:VER", "FATURAS:VER" },
        ["MEDICO"] = new[] { "MEDICO_AREA:VER", "CONVITES:VER", "AGENDA:VER", "PAGAMENTOS:VER" },
        ["HOSPITAL"] = new[] { "PLANTOES:VER", "ESCALAS:VER" },
        ["PARCEIRO"] = new[] { "PARCEIRO:VER", "PROPOSTAS:VER", "COMISSOES:VER" },
        ["SUPORTE"] = new[] { "SUPORTE:GERENCIAR", "CHAMADOS:GERENCIAR" },
        ["AUDITOR"] = new[] { "AUDITORIA:VER", "RELATORIOS:VER" },
        ["COMERCIAL"] = new[] { "COMERCIAL:GERENCIAR", "PROPOSTAS:GERENCIAR", "PLANOS:VER" },
        ["CUSTOMER_SUCCESS"] = new[] { "CUSTOMER_SUCCESS:GERENCIAR", "ONBOARDING:GERENCIAR", "CLIENTES:VER" }
    };

    [HttpGet("matriz")]
    public IActionResult GetMatriz()
    {
        return Ok(ApiResponse<object>.Ok(new { perfis = Perfis, matriz = Matriz }, "Matriz de permissões carregada."));
    }

    [HttpGet("perfil/{perfil}")]
    public IActionResult GetPerfil(string perfil)
    {
        if (!Matriz.TryGetValue(perfil, out var permissoes)) return NotFound(ApiResponse<object>.Fail("Perfil não encontrado.", 404));
        return Ok(ApiResponse<object>.Ok(new { perfil, permissoes }, "Permissões do perfil carregadas."));
    }

    [HttpPost("testar-acesso")]
    public IActionResult TestarAcesso([FromBody] TestarAcessoRequest request)
    {
        var perfil = request.Perfil ?? string.Empty;
        var modulo = request.Modulo ?? string.Empty;
        var acao = request.Acao ?? "VER";
        var chave = modulo.ToUpperInvariant() + ":" + acao.ToUpperInvariant();
        var permitido = Matriz.TryGetValue(perfil, out var permissoes) && (permissoes.Contains("*") || permissoes.Contains(chave, StringComparer.OrdinalIgnoreCase) || permissoes.Any(p => p.StartsWith(modulo + ":", StringComparison.OrdinalIgnoreCase) && string.Equals(acao, "VER", StringComparison.OrdinalIgnoreCase)));
        var motivo = permitido ? "Permitido por perfil e módulo." : "Bloqueado por perfil, plano, módulo ou tenant.";
        return Ok(ApiResponse<object>.Ok(new { permitido, motivo, perfil, modulo, acao }, motivo));
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
