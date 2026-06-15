using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/clinica-dashboard")]
public sealed class ClinicaDashboardController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public ClinicaDashboardController(Saude360ClinicalService service) { this.service = service; }
    [HttpGet("resumo")] public async Task<IActionResult> Resumo() { var r = await service.DashboardResumoAsync(); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize]
[Route("api/painel-chamada")]
public sealed class PainelChamadaController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public PainelChamadaController(Saude360ClinicalService service) { this.service = service; }
    [HttpGet] public async Task<IActionResult> Get([FromQuery] string? status) { var r = await service.ListarAsync("painel", status); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> GetById(Guid id) { var r = await service.ObterAsync("painel", id); return StatusCode(r.StatusCode, r); }
    [HttpPost] public async Task<IActionResult> Post([FromBody] Saude360CreateRequest request) { var r = await service.CriarAsync("painel", request); return StatusCode(r.StatusCode, r); }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Put(Guid id, [FromBody] Saude360CreateRequest request) { var r = await service.AtualizarAsync("painel", id, request); return StatusCode(r.StatusCode, r); }
    [AllowAnonymous, HttpGet("tv/{painelId:guid}")] public async Task<IActionResult> Tv(Guid painelId, [FromQuery] string? token) { var r = await service.ListarAsync("painel", "CHAMADO"); return StatusCode(r.StatusCode, r); }
    [HttpPost("chamar")] public async Task<IActionResult> Chamar([FromBody] Saude360ActionRequest request) { var id = request.FilaId ?? request.Id ?? Guid.Empty; var r = await service.AcaoAsync("painel", id, "chamar", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("rechamar")] public async Task<IActionResult> Rechamar([FromBody] Saude360ActionRequest request) { var id = request.FilaId ?? request.Id ?? Guid.Empty; var r = await service.AcaoAsync("painel", id, "rechamar", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("cancelar")] public async Task<IActionResult> Cancelar([FromBody] Saude360ActionRequest request) { var id = request.FilaId ?? request.Id ?? Guid.Empty; var r = await service.AcaoAsync("painel", id, "cancelar", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("finalizar")] public async Task<IActionResult> Finalizar([FromBody] Saude360ActionRequest request) { var id = request.FilaId ?? request.Id ?? Guid.Empty; var r = await service.AcaoAsync("painel", id, "finalizar", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("ausente")] public async Task<IActionResult> Ausente([FromBody] Saude360ActionRequest request) { var id = request.FilaId ?? request.Id ?? Guid.Empty; var r = await service.AcaoAsync("painel", id, "ausente", request); return StatusCode(r.StatusCode, r); }
    [HttpGet("historico")] public async Task<IActionResult> Historico() { var r = await service.ListarAsync("painelHistorico"); return StatusCode(r.StatusCode, r); }
    [HttpGet("fila")] public async Task<IActionResult> Fila() { var r = await service.ListarAsync("painel"); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize]
[Route("api/agendamentos")]
public sealed class AgendamentosController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public AgendamentosController(Saude360ClinicalService service) { this.service = service; }
    [HttpGet] public async Task<IActionResult> Get([FromQuery] string? status, [FromQuery] Guid? pacienteId) { var r = await service.ListarAsync("agendamentos", status, pacienteId); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> GetById(Guid id) { var r = await service.ObterAsync("agendamentos", id); return StatusCode(r.StatusCode, r); }
    [HttpPost] public async Task<IActionResult> Post([FromBody] Saude360CreateRequest request) { var r = await service.CriarAsync("agendamentos", request); return StatusCode(r.StatusCode, r); }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Put(Guid id, [FromBody] Saude360CreateRequest request) { var r = await service.AtualizarAsync("agendamentos", id, request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/confirmar")] public async Task<IActionResult> Confirmar(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("agendamentos", id, "confirmar", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/checkin")] public async Task<IActionResult> Checkin(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("agendamentos", id, "checkin", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/cancelar")] public async Task<IActionResult> Cancelar(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("agendamentos", id, "cancelar", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/reagendar")] public async Task<IActionResult> Reagendar(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("agendamentos", id, "reagendar", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/marcar-falta")] public async Task<IActionResult> MarcarFalta(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("agendamentos", id, "marcar-falta", request); return StatusCode(r.StatusCode, r); }
    [HttpGet("agenda-dia")] public async Task<IActionResult> AgendaDia() { var r = await service.ListarAsync("agendamentos"); return StatusCode(r.StatusCode, r); }
    [HttpGet("calendario")] public async Task<IActionResult> Calendario() { var r = await service.ListarAsync("agendamentos"); return StatusCode(r.StatusCode, r); }
    [HttpGet("medico/{medicoId:guid}")] public async Task<IActionResult> Medico(Guid medicoId) { var r = await service.ListarAsync("agendamentos", medicoId: medicoId); return StatusCode(r.StatusCode, r); }
    [HttpGet("paciente/{pacienteId:guid}")] public async Task<IActionResult> Paciente(Guid pacienteId) { var r = await service.ListarAsync("agendamentos", pacienteId: pacienteId); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize]
[Route("api/triagens")]
public sealed class TriagensController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public TriagensController(Saude360ClinicalService service) { this.service = service; }
    [HttpGet] public async Task<IActionResult> Get([FromQuery] string? status) { var r = await service.ListarAsync("triagens", status); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> GetById(Guid id) { var r = await service.ObterAsync("triagens", id); return StatusCode(r.StatusCode, r); }
    [HttpPost] public async Task<IActionResult> Post([FromBody] Saude360CreateRequest request) { var r = await service.CriarAsync("triagens", request); return StatusCode(r.StatusCode, r); }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Put(Guid id, [FromBody] Saude360CreateRequest request) { var r = await service.AtualizarAsync("triagens", id, request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/iniciar")] public async Task<IActionResult> Iniciar(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("triagens", id, "iniciar", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/finalizar")] public async Task<IActionResult> Finalizar(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("triagens", id, "finalizar", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/cancelar")] public async Task<IActionResult> Cancelar(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("triagens", id, "cancelar", request); return StatusCode(r.StatusCode, r); }
    [HttpGet("fila")] public async Task<IActionResult> Fila() { var r = await service.ListarAsync("triagens", "AGUARDANDO"); return StatusCode(r.StatusCode, r); }
    [HttpGet("paciente/{pacienteId:guid}")] public async Task<IActionResult> Paciente(Guid pacienteId) { var r = await service.ListarAsync("triagens", pacienteId: pacienteId); return StatusCode(r.StatusCode, r); }
    [HttpGet("classificacoes-risco")] public IActionResult ClassificacoesRisco() { return Ok(ApiResponse<object>.Ok(new[] { "EMERGENCIA", "MUITO_URGENTE", "URGENTE", "POUCO_URGENTE", "NAO_URGENTE" }, "Classificações carregadas.")); }
    [HttpPut("classificacoes-risco")] public IActionResult AtualizarClassificacoesRisco([FromBody] object request) { return Ok(ApiResponse<object>.Ok(request, "Classificações recebidas para configuração por tenant.")); }
}

[ApiController]
[Authorize]
[Route("api/consultas")]
public sealed class ConsultasController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public ConsultasController(Saude360ClinicalService service) { this.service = service; }
    [HttpGet] public async Task<IActionResult> Get([FromQuery] string? status) { var r = await service.ListarAsync("consultas", status); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> GetById(Guid id) { var r = await service.ObterAsync("consultas", id); return StatusCode(r.StatusCode, r); }
    [HttpPost] public async Task<IActionResult> Post([FromBody] Saude360CreateRequest request) { var r = await service.CriarAsync("consultas", request); return StatusCode(r.StatusCode, r); }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Put(Guid id, [FromBody] Saude360CreateRequest request) { var r = await service.AtualizarAsync("consultas", id, request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/iniciar")] public async Task<IActionResult> Iniciar(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("consultas", id, "iniciar", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/finalizar")] public async Task<IActionResult> Finalizar(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("consultas", id, "finalizar", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/cancelar")] public async Task<IActionResult> Cancelar(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("consultas", id, "cancelar", request); return StatusCode(r.StatusCode, r); }
    [HttpGet("paciente/{pacienteId:guid}")] public async Task<IActionResult> Paciente(Guid pacienteId) { var r = await service.ListarAsync("consultas", pacienteId: pacienteId); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}/historico")] public async Task<IActionResult> Historico(Guid id) { var r = await service.HistoricoConsultaAsync(id); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}/resumo")] public async Task<IActionResult> Resumo(Guid id) { var r = await service.ResumoConsultaAsync(id); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize]
[Route("api/cid")]
public sealed class CidController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public CidController(Saude360ClinicalService service) { this.service = service; }
    [HttpGet("capitulos")] public async Task<IActionResult> Capitulos() { var r = await service.CidCapitulosAsync(); return StatusCode(r.StatusCode, r); }
    [HttpGet("grupos")] public async Task<IActionResult> Grupos([FromQuery] string? capitulo) { var r = await service.CidGruposAsync(capitulo); return StatusCode(r.StatusCode, r); }
    [HttpGet("importacoes")] public async Task<IActionResult> Importacoes() { var r = await service.CidImportacoesAsync(); return StatusCode(r.StatusCode, r); }
    [HttpPost("importar-csv")] public async Task<IActionResult> ImportarCsv([FromBody] CidImportacaoRequest request) { var r = await service.ImportarCidCsvAsync(request); return StatusCode(r.StatusCode, r); }
    [HttpPost("importar-url")] public async Task<IActionResult> ImportarUrl([FromBody] CidImportacaoUrlRequest request) { var r = await service.ImportarCidUrlAsync(request); return StatusCode(r.StatusCode, r); }
    [HttpGet] public async Task<IActionResult> Get([FromQuery] string? termo) { var r = await service.ListarAsync("cid", termo: termo); return StatusCode(r.StatusCode, r); }
    [HttpGet("buscar")] public async Task<IActionResult> Buscar([FromQuery] string termo) { var r = await service.ListarAsync("cid", termo: termo); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> GetById(Guid id) { var r = await service.ObterAsync("cid", id); return StatusCode(r.StatusCode, r); }
    [HttpPost] public async Task<IActionResult> Post([FromBody] Saude360CreateRequest request) { var r = await service.CriarAsync("cid", request); return StatusCode(r.StatusCode, r); }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Put(Guid id, [FromBody] Saude360CreateRequest request) { var r = await service.AtualizarAsync("cid", id, request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/inativar")] public async Task<IActionResult> Inativar(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("cid", id, "inativar", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("importar")] public async Task<IActionResult> Importar([FromBody] Saude360CreateRequest request) { var r = await service.CriarAsync("cid", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/favoritar")] public async Task<IActionResult> Favoritar(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.FavoritarCidAsync(id, request); return StatusCode(r.StatusCode, r); }
    [HttpGet("favoritos")] public async Task<IActionResult> Favoritos() { var r = await service.CidFavoritosAsync(); return StatusCode(r.StatusCode, r); }
    [HttpGet("mais-usados")] public async Task<IActionResult> MaisUsados() { var r = await service.CidMaisUsadosAsync(); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize]
[Route("api/prescricoes")]
public sealed class PrescricoesController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public PrescricoesController(Saude360ClinicalService service) { this.service = service; }
    [HttpGet] public async Task<IActionResult> Get([FromQuery] Guid? consultaId, [FromQuery] Guid? pacienteId) { var r = await service.ListarAsync("prescricoes", consultaId: consultaId, pacienteId: pacienteId); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> GetById(Guid id) { var r = await service.ObterAsync("prescricoes", id); return StatusCode(r.StatusCode, r); }
    [HttpPost] public async Task<IActionResult> Post([FromBody] Saude360CreateRequest request) { var r = await service.CriarAsync("prescricoes", request); return StatusCode(r.StatusCode, r); }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Put(Guid id, [FromBody] Saude360CreateRequest request) { var r = await service.AtualizarAsync("prescricoes", id, request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/finalizar")] public async Task<IActionResult> Finalizar(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("prescricoes", id, "finalizar", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/cancelar")] public async Task<IActionResult> Cancelar(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("prescricoes", id, "cancelar", request); return StatusCode(r.StatusCode, r); }
    [HttpGet("paciente/{pacienteId:guid}")] public async Task<IActionResult> Paciente(Guid pacienteId) { var r = await service.ListarAsync("prescricoes", pacienteId: pacienteId); return StatusCode(r.StatusCode, r); }
    [HttpGet("consulta/{consultaId:guid}")] public async Task<IActionResult> Consulta(Guid consultaId) { var r = await service.ListarAsync("prescricoes", consultaId: consultaId); return StatusCode(r.StatusCode, r); }
    [HttpGet("modelos")] public async Task<IActionResult> Modelos() { var r = await service.ListarAsync("prescricaoModelos"); return StatusCode(r.StatusCode, r); }
    [HttpPost("modelos")] public async Task<IActionResult> CriarModelo([FromBody] Saude360CreateRequest request) { var r = await service.CriarAsync("prescricaoModelos", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("modelos/{id:guid}/usar")] public async Task<IActionResult> UsarModelo(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.UsarModeloPrescricaoAsync(id, request); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize]
[Route("api/clinica-financeiro")]
public sealed class ClinicaFinanceiroController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public ClinicaFinanceiroController(Saude360ClinicalService service) { this.service = service; }
    [HttpGet("resumo")] public async Task<IActionResult> Resumo() { var r = await service.ResumoFinanceiroAsync(); return StatusCode(r.StatusCode, r); }
    [HttpGet("contas-receber")] public async Task<IActionResult> ContasReceber() { var r = await service.ListarAsync("contasReceber"); return StatusCode(r.StatusCode, r); }
    [HttpPost("contas-receber")] public async Task<IActionResult> CriarContaReceber([FromBody] Saude360CreateRequest request) { var r = await service.CriarAsync("contasReceber", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("receber")] public async Task<IActionResult> Receber([FromBody] Saude360CreateRequest request) { var r = await service.CriarAsync("recebimentos", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("cancelar")] public async Task<IActionResult> Cancelar([FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("contasReceber", request.Id ?? Guid.Empty, "cancelar", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("estornar")] public async Task<IActionResult> Estornar([FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("recebimentos", request.Id ?? Guid.Empty, "estornar", request); return StatusCode(r.StatusCode, r); }
    [HttpGet("caixa")] public async Task<IActionResult> Caixa() { var r = await service.ListarAsync("caixa"); return StatusCode(r.StatusCode, r); }
    [HttpPost("fechar-caixa")] public async Task<IActionResult> FecharCaixa([FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("caixa", request.CaixaId ?? request.Id ?? Guid.Empty, "fechar-caixa", request); return StatusCode(r.StatusCode, r); }
    [HttpGet("relatorios")] public async Task<IActionResult> Relatorios() { var r = await service.ResumoFinanceiroAsync(); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize]
[Route("api/convenios")]
public sealed class ConveniosController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public ConveniosController(Saude360ClinicalService service) { this.service = service; }
    [HttpGet] public async Task<IActionResult> Get() { var r = await service.ListarAsync("convenios"); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> GetById(Guid id) { var r = await service.ObterAsync("convenios", id); return StatusCode(r.StatusCode, r); }
    [HttpPost] public async Task<IActionResult> Post([FromBody] Saude360CreateRequest request) { var r = await service.CriarAsync("convenios", request); return StatusCode(r.StatusCode, r); }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Put(Guid id, [FromBody] Saude360CreateRequest request) { var r = await service.AtualizarAsync("convenios", id, request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/inativar")] public async Task<IActionResult> Inativar(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("convenios", id, "inativar", request); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}/planos")] public async Task<IActionResult> Planos(Guid id) { var r = await service.ListarAsync("convenioPlanos"); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/planos")] public async Task<IActionResult> CriarPlano(Guid id, [FromBody] Saude360CreateRequest request) { request.ConvenioId = id; var r = await service.CriarAsync("convenioPlanos", request); return StatusCode(r.StatusCode, r); }
    [HttpGet("autorizacoes")] public async Task<IActionResult> Autorizacoes() { var r = await service.ListarAsync("convenioAutorizacoes"); return StatusCode(r.StatusCode, r); }
    [HttpPost("autorizacoes")] public async Task<IActionResult> CriarAutorizacao([FromBody] Saude360CreateRequest request) { var r = await service.CriarAsync("convenioAutorizacoes", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("autorizacoes/{id:guid}/aprovar")] public async Task<IActionResult> Aprovar(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("convenioAutorizacoes", id, "aprovar", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("autorizacoes/{id:guid}/negar")] public async Task<IActionResult> Negar(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("convenioAutorizacoes", id, "negar", request); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize]
[Route("api/planos-saude")]
public sealed class PlanosSaudeController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public PlanosSaudeController(Saude360ClinicalService service) { this.service = service; }
    [HttpGet] public async Task<IActionResult> Get() { var r = await service.ListarAsync("planosSaude"); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> GetById(Guid id) { var r = await service.ObterAsync("planosSaude", id); return StatusCode(r.StatusCode, r); }
    [HttpPost] public async Task<IActionResult> Post([FromBody] Saude360CreateRequest request) { var r = await service.CriarAsync("planosSaude", request); return StatusCode(r.StatusCode, r); }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Put(Guid id, [FromBody] Saude360CreateRequest request) { var r = await service.AtualizarAsync("planosSaude", id, request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/inativar")] public async Task<IActionResult> Inativar(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("planosSaude", id, "inativar", request); return StatusCode(r.StatusCode, r); }
    [HttpGet("paciente/{pacienteId:guid}")] public async Task<IActionResult> Paciente(Guid pacienteId) { var r = await service.ListarAsync("planoSaudePacientes", pacienteId: pacienteId); return StatusCode(r.StatusCode, r); }
    [HttpPost("paciente/{pacienteId:guid}")] public async Task<IActionResult> VincularPaciente(Guid pacienteId, [FromBody] Saude360CreateRequest request) { request.PacienteId = pacienteId; var r = await service.CriarAsync("planoSaudePacientes", request); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize]
[Route("api/pacientes")]
public sealed class PacientesController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public PacientesController(Saude360ClinicalService service) { this.service = service; }
    [HttpGet] public async Task<IActionResult> Get([FromQuery] string? status) { var r = await service.ListarAsync("pacientes", status); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> GetById(Guid id) { var r = await service.ObterAsync("pacientes", id); return StatusCode(r.StatusCode, r); }
    [HttpPost] public async Task<IActionResult> Post([FromBody] Saude360CreateRequest request) { var r = await service.CriarAsync("pacientes", request); return StatusCode(r.StatusCode, r); }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Put(Guid id, [FromBody] Saude360CreateRequest request) { var r = await service.AtualizarAsync("pacientes", id, request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/inativar")] public async Task<IActionResult> Inativar(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("pacientes", id, "inativar", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/reativar")] public async Task<IActionResult> Reativar(Guid id, [FromBody] Saude360ActionRequest request) { var r = await service.AcaoAsync("pacientes", id, "reativar", request); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}/historico")] public async Task<IActionResult> Historico(Guid id) { var r = await service.ListarAsync("pacienteHistorico", pacienteId: id); return StatusCode(r.StatusCode, r); }
    [HttpGet("buscar")] public async Task<IActionResult> Buscar([FromQuery] string? termo) { var r = await service.ListarAsync("pacientes", termo: termo); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}/resumo-clinico")] public async Task<IActionResult> ResumoClinico(Guid id) { var r = await service.ObterAsync("pacientes", id); return StatusCode(r.StatusCode, r); }
}

public sealed class CidImportacaoRequest { public string Csv { get; set; } = string.Empty; public string ArquivoNome { get; set; } = string.Empty; public string Fonte { get; set; } = string.Empty; public string FonteUrl { get; set; } = string.Empty; public string Versao { get; set; } = "CID-10"; }
public sealed class CidImportacaoUrlRequest { public string Url { get; set; } = string.Empty; public string Fonte { get; set; } = string.Empty; public string Versao { get; set; } = "CID-10"; }
