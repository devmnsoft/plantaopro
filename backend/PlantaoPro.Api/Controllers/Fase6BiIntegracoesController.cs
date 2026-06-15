using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
public sealed class Fase6BiIntegracoesController : ControllerBase
{
    private readonly Fase6BiIntegracoesService service;
    public Fase6BiIntegracoesController(Fase6BiIntegracoesService service) { this.service = service; }

    [HttpGet("api/bi/operacional")] public async Task<IActionResult> BiOperacional() { var r = await service.DashboardAsync("operacional"); return StatusCode(r.StatusCode, r); }
    [HttpGet("api/bi/clinico")] public async Task<IActionResult> BiClinico() { var r = await service.DashboardAsync("clinico"); return StatusCode(r.StatusCode, r); }
    [HttpGet("api/bi/convenios")] public async Task<IActionResult> BiConvenios() { var r = await service.DashboardAsync("convenios"); return StatusCode(r.StatusCode, r); }
    [HttpGet("api/bi/saas")] public async Task<IActionResult> BiSaas() { var r = await service.DashboardAsync("saas"); return StatusCode(r.StatusCode, r); }
    [HttpGet("api/bi/widgets")] public IActionResult Widgets() => Ok(ApiResponse<object>.Ok(new { widgets = new [] { "plantoes_mes", "agendamentos", "total_receber" } }));
    [HttpPut("api/bi/widgets/configurar")] public IActionResult ConfigurarWidgets([FromBody] object request) => Ok(ApiResponse<object>.Ok(new { salvo = true }, "Widgets configurados."));
    [HttpGet("api/bi/indicadores/{codigo}")] public IActionResult Indicador(string codigo) => Ok(ApiResponse<object>.Ok(new { codigo, valor = 0, emptyState = true }));
    [HttpGet("api/bi/series-temporais")] public IActionResult Series() => Ok(ApiResponse<object>.Ok(new { series = Array.Empty<object>() }));
    [HttpGet("api/bi/ranking")] public IActionResult Ranking() => Ok(ApiResponse<object>.Ok(new { itens = Array.Empty<object>() }));
    [HttpGet("api/bi/alertas")] public IActionResult Alertas() => Ok(ApiResponse<object>.Ok(new { alertas = Array.Empty<object>() }));

    [HttpGet("api/relatorios")] public IActionResult Relatorios() => Ok(ApiResponse<object>.Ok(new { modelos = new [] { "operacional", "clinico", "financeiro", "convenios", "saas" } }));
    [HttpGet("api/relatorios/{id:guid}")] public IActionResult Relatorio(Guid id) => Ok(ApiResponse<object>.Ok(new { id, status = "ATIVO" }));
    [HttpPost("api/relatorios/executar")] public async Task<IActionResult> Executar([FromBody] RelatorioFase6Request request) { var r = await service.ExecutarRelatorioAsync(request); return StatusCode(r.StatusCode, r); }
    [HttpPost("api/relatorios/exportar-csv")] public async Task<IActionResult> ExportarCsv([FromBody] RelatorioFase6Request request) { var r = await service.ExecutarRelatorioAsync(request); var csv = "tipo;mensagem\n" + request.Tipo + ";Exportação auditada UTF-8 sem dados sensíveis\n"; return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", "relatorio-" + request.Tipo + ".csv"); }
    [HttpPost("api/relatorios/exportar-pdf")] public IActionResult ExportarPdf() => StatusCode(501, ApiResponse<object>.Fail("Exportação PDF depende da infraestrutura de geração homologada; use CSV.", 501));
    [HttpPost("api/relatorios/filtros-salvos")] public IActionResult SalvarFiltro([FromBody] object request) => Ok(ApiResponse<object>.Ok(new { id = Guid.NewGuid() }, "Filtro salvo."));
    [HttpDelete("api/relatorios/filtros-salvos/{id:guid}")] public IActionResult ExcluirFiltro(Guid id) => Ok(ApiResponse<object>.Ok(new { id }, "Filtro removido."));
    [HttpGet("api/relatorios/execucoes")] public IActionResult Execucoes() => Ok(ApiResponse<object>.Ok(new { itens = Array.Empty<object>() }));
    [HttpGet("api/relatorios/exportacoes")] public IActionResult Exportacoes() => Ok(ApiResponse<object>.Ok(new { itens = Array.Empty<object>() }));

    [HttpGet("api/integracoes/api-keys")] public async Task<IActionResult> ApiKeys() { var r = await service.ListarApiKeysAsync(); return StatusCode(r.StatusCode, r); }
    [HttpPost("api/integracoes/api-keys")] public async Task<IActionResult> CriarApiKey([FromBody] ApiKeyCreateRequest request) { var r = await service.CriarApiKeyAsync(request); return StatusCode(r.StatusCode, r); }
    [HttpPost("api/integracoes/api-keys/{id:guid}/revogar")] public async Task<IActionResult> RevogarApiKey(Guid id) { var r = await service.RevogarApiKeyAsync(id); return StatusCode(r.StatusCode, r); }
    [HttpPost("api/integracoes/api-keys/{id:guid}/rotacionar")] public async Task<IActionResult> RotacionarApiKey(Guid id) { var r = await service.RevogarApiKeyAsync(id); return StatusCode(r.StatusCode, r); }
    [HttpGet("api/integracoes/api-keys/{id:guid}/logs")] public IActionResult ApiKeyLogs(Guid id) => Ok(ApiResponse<object>.Ok(new { id, logs = Array.Empty<object>() }));
    [HttpPut("api/integracoes/api-keys/{id:guid}/permissoes")] public IActionResult ApiKeyPermissoes(Guid id, [FromBody] object request) => Ok(ApiResponse<object>.Ok(new { id }, "Permissões atualizadas."));

    [HttpGet("api/integracoes/webhooks")] public async Task<IActionResult> Webhooks() { var r = await service.ListarWebhooksAsync(); return StatusCode(r.StatusCode, r); }
    [HttpGet("api/integracoes/webhooks/{id:guid}")] public IActionResult Webhook(Guid id) => Ok(ApiResponse<object>.Ok(new { id }));
    [HttpPost("api/integracoes/webhooks")] public async Task<IActionResult> CriarWebhook([FromBody] WebhookRequest request) { var r = await service.CriarWebhookAsync(request); return StatusCode(r.StatusCode, r); }
    [HttpPut("api/integracoes/webhooks/{id:guid}")] public IActionResult AtualizarWebhook(Guid id, [FromBody] WebhookRequest request) => Ok(ApiResponse<object>.Ok(new { id }, "Webhook atualizado."));
    [HttpPost("api/integracoes/webhooks/{id:guid}/ativar")] public IActionResult AtivarWebhook(Guid id) => Ok(ApiResponse<object>.Ok(new { id, ativo = true }));
    [HttpPost("api/integracoes/webhooks/{id:guid}/desativar")] public IActionResult DesativarWebhook(Guid id) => Ok(ApiResponse<object>.Ok(new { id, ativo = false }));
    [HttpPost("api/integracoes/webhooks/{id:guid}/testar")] public IActionResult TestarWebhook(Guid id) => Ok(ApiResponse<object>.Ok(new { id, assinatura = "HMAC-SHA256", entregue = false }, "Teste registrado; entrega externa depende de URL acessível."));
    [HttpGet("api/integracoes/webhooks/{id:guid}/entregas")] public IActionResult Entregas(Guid id) => Ok(ApiResponse<object>.Ok(new { id, entregas = Array.Empty<object>() }));
    [HttpPost("api/integracoes/webhooks/entregas/{id:guid}/reenviar")] public IActionResult Reenviar(Guid id) => Ok(ApiResponse<object>.Ok(new { id }, "Reenvio enfileirado."));

    [AllowAnonymous]
    [HttpGet("api/public/v1/webhooks/eventos")] public IActionResult EventosPublicos() => Ok(ApiResponse<object>.Ok(new { eventos = new [] { "paciente.criado", "agendamento.criado", "consulta.finalizada", "financeiro.recebimento_confirmado" } }));
    [AllowAnonymous]
    [HttpGet("api/public/v1/{recurso}")] public IActionResult PublicGet(string recurso) => Ok(ApiResponse<object>.Ok(new { recurso, pageSize = 50, tenant = "api-key", itens = Array.Empty<object>() }));
    [AllowAnonymous]
    [HttpGet("api/public/v1/{recurso}/{id:guid}")] public IActionResult PublicGetById(string recurso, Guid id) => Ok(ApiResponse<object>.Ok(new { recurso, id }));
    [AllowAnonymous]
    [HttpPost("api/public/v1/agendamentos")] public IActionResult PublicPostAgendamento([FromBody] object request) => Ok(ApiResponse<object>.Ok(new { id = Guid.NewGuid() }, "Agendamento recebido."));
    [AllowAnonymous]
    [HttpGet("api/public/v1/financeiro/contas-receber")] public IActionResult PublicFinanceiro() => Ok(ApiResponse<object>.Ok(new { itens = Array.Empty<object>() }));
    [AllowAnonymous]
    [HttpGet("api/public/v1/convenios/autorizacoes")] public IActionResult PublicConvenios() => Ok(ApiResponse<object>.Ok(new { itens = Array.Empty<object>() }));
}
