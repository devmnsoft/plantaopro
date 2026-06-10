using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR")]
[Route("api/piloto")]
[Tags("Piloto e Beta")]
public sealed class PilotoBetaController : ControllerBase
{
    private readonly B2BCommercialOpsService _service;
    private readonly ILogger<PilotoBetaController> _logger;
    public PilotoBetaController(B2BCommercialOpsService service, ILogger<PilotoBetaController> logger) { _service = service; _logger = logger; }

    [HttpGet("programas")] public async Task<IActionResult> Programas() => Ok(await _service.ListarProgramasAsync());
    [HttpGet("programas/{id:guid}")] public async Task<IActionResult> Programa(Guid id) { var r = await _service.ObterProgramaAsync(id); return StatusCode(r.StatusCode, r); }
    [HttpPost("programas")] public async Task<IActionResult> CriarPrograma([FromBody] PilotoProgramaRequest request) { try { var r = await _service.SalvarProgramaAsync(request, null, Ip()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { _logger.LogError(ex, "Erro no POST programa piloto"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível criar programa piloto.", 500)); } }
    [HttpPut("programas/{id:guid}")] public async Task<IActionResult> AtualizarPrograma(Guid id, [FromBody] PilotoProgramaRequest request) { try { var r = await _service.SalvarProgramaAsync(request, id, Ip()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { _logger.LogError(ex, "Erro no PUT programa piloto {Id}", id); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível atualizar programa piloto.", 500)); } }
    [HttpGet("clientes")] public async Task<IActionResult> Clientes() => Ok(await _service.ListarClientesPilotoAsync());
    [HttpGet("clientes/{id:guid}")] public async Task<IActionResult> Cliente(Guid id) { var r = await _service.ObterClientePilotoAsync(id); return StatusCode(r.StatusCode, r); }
    [HttpPost("clientes")] public async Task<IActionResult> CriarCliente([FromBody] PilotoClienteRequest request) { try { var r = await _service.CriarClientePilotoAsync(request, Ip()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { _logger.LogError(ex, "Erro no POST cliente piloto"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível criar cliente piloto.", 500)); } }
    [HttpPost("clientes/{id:guid}/iniciar")] public async Task<IActionResult> Iniciar(Guid id) { var r = await _service.AlterarClientePilotoStatusAsync(id, "EM_ANDAMENTO", Ip()); return StatusCode(r.StatusCode, r); }
    [HttpPost("clientes/{id:guid}/pausar")] public async Task<IActionResult> Pausar(Guid id) { var r = await _service.AlterarClientePilotoStatusAsync(id, "PAUSADO", Ip()); return StatusCode(r.StatusCode, r); }
    [HttpPost("clientes/{id:guid}/concluir")] public async Task<IActionResult> Concluir(Guid id) { var r = await _service.AlterarClientePilotoStatusAsync(id, "CONCLUIDO", Ip()); return StatusCode(r.StatusCode, r); }
    [HttpPost("clientes/{id:guid}/converter")] public async Task<IActionResult> Converter(Guid id) { var r = await _service.AlterarClientePilotoStatusAsync(id, "CONVERTIDO", Ip()); return StatusCode(r.StatusCode, r); }
    [HttpGet("feedbacks")] public async Task<IActionResult> Feedbacks() => Ok(await _service.ListarFeedbacksAsync());
    [HttpPost("feedbacks")] public async Task<IActionResult> CriarFeedback([FromBody] PilotoFeedbackRequest request) { try { var r = await _service.CriarFeedbackAsync(request, Ip()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { _logger.LogError(ex, "Erro no POST feedback piloto"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível criar feedback.", 500)); } }
    [HttpPost("feedbacks/{id:guid}/classificar")] public async Task<IActionResult> Classificar(Guid id, [FromBody] FeedbackClassificacaoRequest request) { var r = await _service.ClassificarFeedbackAsync(id, request, Ip()); return StatusCode(r.StatusCode, r); }
    [HttpPost("feedbacks/{id:guid}/resolver")] public async Task<IActionResult> Resolver(Guid id, [FromBody] ResolverRequest request) { var r = await _service.ResolverFeedbackAsync(id, request, Ip()); return StatusCode(r.StatusCode, r); }
    [HttpGet("indicadores")] public async Task<IActionResult> Indicadores() => Ok(await _service.IndicadoresPilotoAsync());
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}

[ApiController]
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR")]
[Route("api/customer-success")]
[Tags("Customer Success")]
public sealed class CustomerSuccessApiController : ControllerBase
{
    private readonly B2BCommercialOpsService _service;
    private readonly ILogger<CustomerSuccessApiController> _logger;
    public CustomerSuccessApiController(B2BCommercialOpsService service, ILogger<CustomerSuccessApiController> logger) { _service = service; _logger = logger; }
    [HttpGet("contas")] public async Task<IActionResult> Contas() => Ok(await _service.ListarCsContasAsync());
    [HttpGet("contas/{tenantId:guid}")] public async Task<IActionResult> Conta(Guid tenantId) => Ok(await _service.ObterCsContaAsync(tenantId));
    [HttpGet("contas/{tenantId:guid}/health")] public async Task<IActionResult> Health(Guid tenantId) => Ok(await _service.CalcularHealthAsync(tenantId));
    [HttpPost("contas/{tenantId:guid}/interacoes")] public async Task<IActionResult> Interacao(Guid tenantId, [FromBody] CsInteracaoRequest request) { try { var r = await _service.RegistrarCsInteracaoAsync(tenantId, request, Ip()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { _logger.LogError(ex, "Erro ao registrar interação CS"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível registrar interação.", 500)); } }
    [HttpPost("contas/{tenantId:guid}/plano-acao")] public async Task<IActionResult> PlanoAcao(Guid tenantId, [FromBody] CsInteracaoRequest request) => await Interacao(tenantId, request);
    [HttpPost("contas/{tenantId:guid}/tarefas")] public async Task<IActionResult> Tarefa(Guid tenantId, [FromBody] CsInteracaoRequest request) => await Interacao(tenantId, request);
    [HttpPost("contas/{tenantId:guid}/nps")] public async Task<IActionResult> Nps(Guid tenantId, [FromBody] CsNpsRequest request) { try { var r = await _service.RegistrarNpsAsync(tenantId, request, Ip()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { _logger.LogError(ex, "Erro ao registrar NPS"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível registrar NPS.", 500)); } }
    [HttpGet("riscos")] public async Task<IActionResult> Riscos() => Ok(await _service.ListarRiscosAsync());
    [HttpGet("oportunidades")] public async Task<IActionResult> Oportunidades() => Ok(await _service.ListarOportunidadesAsync());
    [HttpGet("playbooks")] public async Task<IActionResult> Playbooks() => Ok(await _service.PlaybooksAsync());
    [HttpPost("playbooks")] public async Task<IActionResult> CriarPlaybook([FromBody] B2BResumoRequest request) { var r = await _service.CriarPlaybookAsync(request, Ip()); return StatusCode(r.StatusCode, r); }
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}

[ApiController]
[Authorize(Roles = "ADMINISTRADOR_GLOBAL")]
[Route("api/executivo")]
[Tags("Dashboard Executivo B2B")]
public sealed class ExecutivoController : ControllerBase
{
    private readonly B2BCommercialOpsService _service;
    public ExecutivoController(B2BCommercialOpsService service) { _service = service; }
    [HttpGet("resumo")] public async Task<IActionResult> Resumo() => Ok(await _service.ExecutivoAsync("resumo"));
    [HttpGet("receita")] public async Task<IActionResult> Receita() => Ok(await _service.ExecutivoAsync("receita"));
    [HttpGet("clientes")] public async Task<IActionResult> Clientes() => Ok(await _service.ExecutivoAsync("clientes"));
    [HttpGet("operacao")] public async Task<IActionResult> Operacao() => Ok(await _service.ExecutivoAsync("operacao"));
    [HttpGet("comercial")] public async Task<IActionResult> Comercial() => Ok(await _service.ExecutivoAsync("comercial"));
    [HttpGet("produto")] public async Task<IActionResult> Produto() => Ok(await _service.ExecutivoAsync("produto"));
    [HttpGet("alertas")] public async Task<IActionResult> Alertas() => Ok(await _service.ExecutivoAsync("alertas"));
    [HttpGet("metas")] public async Task<IActionResult> Metas() => Ok(await _service.ExecutivoAsync("metas"));
}

[ApiController]
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR")]
[Route("api/white-label/templates")]
[Tags("Templates White Label")]
public sealed class WhiteLabelTemplatesApiController : ControllerBase
{
    private readonly B2BCommercialOpsService _service;
    private readonly ILogger<WhiteLabelTemplatesApiController> _logger;
    public WhiteLabelTemplatesApiController(B2BCommercialOpsService service, ILogger<WhiteLabelTemplatesApiController> logger) { _service = service; _logger = logger; }
    [HttpGet] public async Task<IActionResult> Listar() => Ok(await _service.ListarTemplatesAsync());
    [HttpGet("{id:guid}")] public async Task<IActionResult> Obter(Guid id) { var r = await _service.ObterTemplateAsync(id); return StatusCode(r.StatusCode, r); }
    [HttpPost] public async Task<IActionResult> Criar([FromBody] WhiteLabelTemplateRequest request) { try { var r = await _service.SalvarTemplateAsync(request, null, Ip()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { _logger.LogError(ex, "Erro ao criar template"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível criar template.", 500)); } }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Atualizar(Guid id, [FromBody] WhiteLabelTemplateRequest request) { try { var r = await _service.SalvarTemplateAsync(request, id, Ip()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { _logger.LogError(ex, "Erro ao atualizar template"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível atualizar template.", 500)); } }
    [HttpPost("{id:guid}/aplicar/{tenantId:guid}")] public async Task<IActionResult> Aplicar(Guid id, Guid tenantId) { var r = await _service.AplicarTemplateAsync(id, tenantId, Ip()); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/duplicar")] public async Task<IActionResult> Duplicar(Guid id) { var r = await _service.DuplicarTemplateAsync(id, Ip()); return StatusCode(r.StatusCode, r); }
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}

[ApiController]
[Authorize]
[Route("api/treinamento")]
[Tags("Treinamento B2B")]
public sealed class TreinamentoController : ControllerBase
{
    private readonly B2BCommercialOpsService _service;
    public TreinamentoController(B2BCommercialOpsService service) { _service = service; }
    [HttpGet("trilhas")] public async Task<IActionResult> Trilhas() => Ok(await _service.ListarTrilhasAsync());
    [HttpGet("trilhas/{id:guid}")] public async Task<IActionResult> Trilha(Guid id) { var r = await _service.ObterTrilhaAsync(id); return StatusCode(r.StatusCode, r); }
    [HttpPost("trilhas/{id:guid}/concluir")] public async Task<IActionResult> Concluir(Guid id) { var r = await _service.ConcluirTrilhaAsync(id, Ip()); return StatusCode(r.StatusCode, r); }
    [HttpGet("artigos")] public async Task<IActionResult> Artigos([FromQuery] string? termo) => Ok(await _service.ListarArtigosAsync(termo));
    [HttpGet("buscar")] public async Task<IActionResult> Buscar([FromQuery] string? q) => Ok(await _service.ListarArtigosAsync(q));
    [HttpPost("artigos/{id:guid}/feedback")] public async Task<IActionResult> Feedback(Guid id, [FromBody] TreinamentoFeedbackRequest request) { var r = await _service.FeedbackArtigoAsync(id, request, Ip()); return StatusCode(r.StatusCode, r); }
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}

[ApiController]
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR")]
[Route("api/operacao-assistida")]
[Tags("Operação Assistida")]
public sealed class OperacaoAssistidaPlanosController : ControllerBase
{
    private readonly B2BCommercialOpsService _service;
    private readonly ILogger<OperacaoAssistidaPlanosController> _logger;
    public OperacaoAssistidaPlanosController(B2BCommercialOpsService service, ILogger<OperacaoAssistidaPlanosController> logger) { _service = service; _logger = logger; }
    [HttpGet("planos")] public async Task<IActionResult> Planos() => Ok(await _service.ListarPlanosOperacaoAsync());
    [HttpGet("planos/{id:guid}")] public async Task<IActionResult> Plano(Guid id) { var r = await _service.ObterPlanoOperacaoAsync(id); return StatusCode(r.StatusCode, r); }
    [HttpPost("planos")] public async Task<IActionResult> CriarPlano([FromBody] OperacaoAssistidaPlanoRequest request) { try { var r = await _service.CriarPlanoOperacaoAsync(request, Ip()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { _logger.LogError(ex, "Erro ao criar operação assistida"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível criar plano de operação assistida.", 500)); } }
    [HttpPost("planos/{id:guid}/iniciar")] public async Task<IActionResult> Iniciar(Guid id) { var r = await _service.AlterarPlanoOperacaoAsync(id, "EM_ANDAMENTO", Ip()); return StatusCode(r.StatusCode, r); }
    [HttpPost("etapas/{id:guid}/concluir")] public async Task<IActionResult> ConcluirEtapa(Guid id, [FromBody] ResolverRequest request) { var r = await _service.ConcluirEtapaAsync(id, false, request, Ip()); return StatusCode(r.StatusCode, r); }
    [HttpPost("etapas/{id:guid}/pular")] public async Task<IActionResult> PularEtapa(Guid id, [FromBody] ResolverRequest request) { var r = await _service.ConcluirEtapaAsync(id, true, request, Ip()); return StatusCode(r.StatusCode, r); }
    [HttpPost("pendencias")] public async Task<IActionResult> Pendencia([FromBody] CsInteracaoRequest request) { var r = await _service.RegistrarCsInteracaoAsync(Guid.Empty, request, Ip()); return StatusCode(r.StatusCode, r); }
    [HttpPost("evidencias")] public async Task<IActionResult> Evidencia([FromBody] CsInteracaoRequest request) { var r = await _service.RegistrarCsInteracaoAsync(Guid.Empty, request, Ip()); return StatusCode(r.StatusCode, r); }
    [HttpGet("atrasadas")] public async Task<IActionResult> Atrasadas() => Ok(await _service.EtapasAtrasadasAsync());
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}

[ApiController]
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR")]
[Route("api/central-escala")]
[Tags("Central de Escala Evoluída")]
public sealed class CentralEscalaEvoluidaController : ControllerBase
{
    private readonly B2BCommercialOpsService _service;
    public CentralEscalaEvoluidaController(B2BCommercialOpsService service) { _service = service; }
    [HttpGet("plantao-descoberto")] public async Task<IActionResult> Descobertos() => Ok(await _service.PlantaoDescobertoAsync());
    [HttpGet("risco")] public async Task<IActionResult> Risco() => Ok(await _service.RiscoEscalaAsync());
    [HttpGet("medicos-disponiveis")] public async Task<IActionResult> MedicosDisponiveis() => Ok(await _service.MedicosDisponiveisAsync());
    [HttpPost("convidar")] public async Task<IActionResult> Convidar([FromBody] CentralEscalaAcaoRequest request) { var r = await _service.AcaoCentralEscalaAsync("CONVIDAR", request, Ip()); return StatusCode(r.StatusCode, r); }
    [HttpPost("substituir")] public async Task<IActionResult> Substituir([FromBody] CentralEscalaAcaoRequest request) { var r = await _service.AcaoCentralEscalaAsync("SUBSTITUIR", request, Ip()); return StatusCode(r.StatusCode, r); }
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}

[ApiController]
[Authorize(Roles = "MEDICO,ADMINISTRADOR_GLOBAL")]
[Route("api/medicos/me")]
[Tags("Agenda Médica")]
public sealed class MedicoAgendaMeController : ControllerBase
{
    private readonly B2BCommercialOpsService _service;
    public MedicoAgendaMeController(B2BCommercialOpsService service) { _service = service; }
    [HttpGet("agenda")] public async Task<IActionResult> Agenda() => Ok(await _service.AgendaMedicaAsync(Uid()));
    private Guid Uid() { var uid = User.FindFirst("uid")?.Value; return Guid.TryParse(uid, out var parsed) ? parsed : Guid.Empty; }
}

[ApiController]
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR")]
[Route("api/renovacoes")]
[Tags("Renovações")]
public sealed class RenovacoesController : ControllerBase
{
    private readonly B2BCommercialOpsService _service;
    public RenovacoesController(B2BCommercialOpsService service) { _service = service; }
    [HttpGet] public async Task<IActionResult> Listar() => Ok(await _service.ListarRenovacoesAsync());
    [HttpGet("{id:guid}")] public async Task<IActionResult> Obter(Guid id) { var r = await _service.ObterRenovacaoAsync(id); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/iniciar")] public async Task<IActionResult> Iniciar(Guid id, [FromBody] ResolverRequest request) { var r = await _service.StatusRenovacaoAsync(id, "INICIADA", request, Ip()); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/registrar-contato")] public async Task<IActionResult> Contato(Guid id, [FromBody] CsInteracaoRequest request) { var r = await _service.RegistrarContatoRenovacaoAsync(id, request, Ip()); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/renovar")] public async Task<IActionResult> Renovar(Guid id, [FromBody] ResolverRequest request) { var r = await _service.StatusRenovacaoAsync(id, "RENOVADA", request, Ip()); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/marcar-risco")] public async Task<IActionResult> Risco(Guid id, [FromBody] ResolverRequest request) { var r = await _service.StatusRenovacaoAsync(id, "RISCO", request, Ip()); return StatusCode(r.StatusCode, r); }
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}

[ApiController]
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR")]
[Route("api/expansoes")]
[Tags("Expansões")]
public sealed class ExpansoesController : ControllerBase
{
    private readonly B2BCommercialOpsService _service;
    public ExpansoesController(B2BCommercialOpsService service) { _service = service; }
    [HttpGet] public async Task<IActionResult> Listar() => Ok(await _service.ListarExpansoesAsync());
    [HttpPost] public async Task<IActionResult> Criar([FromBody] ExpansaoRequest request) { var r = await _service.CriarExpansaoAsync(request, Ip()); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/ganhar")] public async Task<IActionResult> Ganhar(Guid id, [FromBody] ResolverRequest request) { var r = await _service.StatusExpansaoAsync(id, "GANHA", request, Ip()); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/perder")] public async Task<IActionResult> Perder(Guid id, [FromBody] ResolverRequest request) { var r = await _service.StatusExpansaoAsync(id, "PERDIDA", request, Ip()); return StatusCode(r.StatusCode, r); }
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
