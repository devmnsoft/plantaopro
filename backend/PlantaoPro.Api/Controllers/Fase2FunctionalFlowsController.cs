using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/fase2/fluxos")]
[Tags("Fase 2 - Fluxos funcionais SaaS")]
public sealed class Fase2FunctionalFlowsController : ControllerBase
{
    private readonly IAuditService audit;
    private readonly ILogger<Fase2FunctionalFlowsController> logger;

    public Fase2FunctionalFlowsController(IAuditService audit, ILogger<Fase2FunctionalFlowsController> logger)
    {
        this.audit = audit;
        this.logger = logger;
    }

    [HttpGet("{area}")]
    public IActionResult Details(string area)
    {
        try
        {
            var normalized = string.IsNullOrWhiteSpace(area) ? "SAAS" : area.Trim().ToUpperInvariant();
            var result = new Fase2FlowDto
            {
                Area = normalized,
                TenantScope = User.FindFirst("cliente_id")?.Value ?? "ADMIN_SAAS",
                Kpis = BuildKpis(normalized),
                RequiredValidations = new List<string>
                {
                    "Tenant aplicado antes de consultar dados operacionais.",
                    "Permissão por perfil aplicada no controller e no menu.",
                    "Ações críticas exigem justificativa, validação de plano e auditoria.",
                    "Erros técnicos retornam ApiResponse com mensagem amigável."
                },
                AvailableActions = BuildActions(normalized)
            };
            return Ok(ApiResponse<Fase2FlowDto>.Ok(result, "Fluxo fase 2 carregado."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao carregar fluxo fase 2 {Area}", area);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar o fluxo funcional.", 500));
        }
    }

    [HttpPost("acao")]
    public async Task<IActionResult> RegisterAction([FromBody] Fase2ActionRequest request)
    {
        try
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(request.Action)) errors.Add("Ação é obrigatória.");
            if (IsCritical(request.Action) && string.IsNullOrWhiteSpace(request.Justification)) errors.Add("Justificativa é obrigatória para ações críticas.");
            if (errors.Count > 0) return BadRequest(ApiResponse<string>.Fail("Verifique os dados da ação.", 400, errors));

            await audit.RegistrarAsync(null, request.TenantId, "FASE2_FLUXO", request.EntityId, request.Action.Trim().ToUpperInvariant(), new { request.Justification }, true, HttpContext.Connection.RemoteIpAddress?.ToString(), User.Identity?.Name);
            return Ok(ApiResponse<string>.Ok("ok", "Ação validada e auditada."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao registrar ação funcional fase 2 {Action}", request.Action);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível registrar a ação.", 500));
        }
    }

    private static IEnumerable<Fase2KpiDto> BuildKpis(string area)
    {
        if (area == "COMERCIAL") return new List<Fase2KpiDto> { Kpi("Leads", "26", "8 aguardam qualificação", "OK"), Kpi("Propostas", "11", "3 próximas do vencimento", "ATENÇÃO") };
        if (area == "CENTRAL") return new List<Fase2KpiDto> { Kpi("Plantões descobertos", "4", "Ação hoje", "RISCO"), Kpi("Convites pendentes", "18", "6 vencem hoje", "ATENÇÃO") };
        if (area == "MEDICO") return new List<Fase2KpiDto> { Kpi("Convites", "3", "Responder hoje", "ATENÇÃO"), Kpi("Pagamentos", "R$ 4.800", "Previstos", "OK") };
        if (area == "FINANCEIRO") return new List<Fase2KpiDto> { Kpi("Pendentes", "R$ 38.200", "14 pagamentos", "ATENÇÃO"), Kpi("Contestados", "2", "Resolver", "RISCO") };
        if (area == "PARCEIRO") return new List<Fase2KpiDto> { Kpi("Leads", "9", "3 novos", "OK"), Kpi("Comissões", "R$ 7.240", "Previstas", "OK") };
        return new List<Fase2KpiDto> { Kpi("Clientes ativos", "18", "MRR estimado", "OK"), Kpi("Faturas vencidas", "2", "Cobrança", "RISCO") };
    }

    private static IEnumerable<string> BuildActions(string area)
    {
        if (area == "COMERCIAL") return new List<string> { "CRIAR_LEAD", "GERAR_PROPOSTA", "APROVAR_PROPOSTA", "CONVERTER_TENANT" };
        if (area == "CENTRAL") return new List<string> { "CRIAR_PLANTAO", "PUBLICAR_PLANTAO", "CONVIDAR_MEDICO", "CONFIRMAR_ESCALA" };
        if (area == "MEDICO") return new List<string> { "ACEITAR_CONVITE", "RECUSAR_CONVITE", "INFORMAR_DISPONIBILIDADE", "SOLICITAR_SUBSTITUICAO" };
        if (area == "FINANCEIRO") return new List<string> { "GERAR_PAGAMENTO", "CONFIRMAR_PAGAMENTO", "CANCELAR_PAGAMENTO", "EXPORTAR_RELATORIO" };
        if (area == "PARCEIRO") return new List<string> { "CRIAR_LEAD_PARCEIRO", "VER_PROPOSTAS", "VER_COMISSOES", "ABRIR_SUPORTE" };
        return new List<string> { "VER_DASHBOARD", "SOLICITAR_UPGRADE", "PUBLICAR_WHITE_LABEL" };
    }

    private static Fase2KpiDto Kpi(string label, string value, string hint, string status)
    {
        return new Fase2KpiDto { Label = label, Value = value, Hint = hint, Status = status };
    }

    private static bool IsCritical(string? action)
    {
        var normalized = string.IsNullOrWhiteSpace(action) ? string.Empty : action.Trim().ToUpperInvariant();
        return normalized.Contains("CONFIRMAR") || normalized.Contains("CANCELAR") || normalized.Contains("CONVERTER") || normalized.Contains("PUBLICAR") || normalized.Contains("EXPORTAR");
    }
}
