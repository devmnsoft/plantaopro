using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = RolesConstants.CadastrosOperacao + ",RECEPCAO,ADMINISTRADOR_CLINICA")]
[Route("api/pacientes")]
public sealed class PacientesController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public PacientesController(Saude360ClinicalService service) { this.service = service; }
    [HttpGet] public async Task<IActionResult> Listar() { var r = await service.ListarAsync("pacientes"); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> Obter(Guid id) { var r = await service.ObterAsync("pacientes", id); return StatusCode(r.StatusCode, r); }
    [HttpPost] public async Task<IActionResult> Criar([FromBody] PacienteRequest request) { var r = await service.CriarPacienteAsync(request); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize(Roles = RolesConstants.CadastrosOperacao + ",RECEPCAO,ADMINISTRADOR_CLINICA")]
[Route("api/painel-chamada")]
public sealed class PainelChamadaController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public PainelChamadaController(Saude360ClinicalService service) { this.service = service; }
    [HttpGet] public async Task<IActionResult> Index() { var r = await service.ListarAsync("painel_chamada_fila"); return StatusCode(r.StatusCode, r); }
    [AllowAnonymous]
    [HttpGet("tv/{painelId:guid}")] public async Task<IActionResult> Tv(Guid painelId, [FromQuery] string token) { var r = await service.TvPainelAsync(painelId, token); return StatusCode(r.StatusCode, r); }
    [HttpPost("chamar")] public async Task<IActionResult> Chamar([FromBody] PainelChamadaAcaoRequest request) { var r = await service.AcaoPainelAsync("CHAMAR", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("rechamar")] public async Task<IActionResult> Rechamar([FromBody] PainelChamadaAcaoRequest request) { var r = await service.AcaoPainelAsync("RECHAMAR", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("finalizar")] public async Task<IActionResult> Finalizar([FromBody] PainelChamadaAcaoRequest request) { var r = await service.AcaoPainelAsync("FINALIZAR", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("ausente")] public async Task<IActionResult> Ausente([FromBody] PainelChamadaAcaoRequest request) { var r = await service.AcaoPainelAsync("AUSENTE", request); return StatusCode(r.StatusCode, r); }
    [HttpGet("historico")] public async Task<IActionResult> Historico() { var r = await service.ListarAsync("painel_chamada_historico"); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize(Roles = RolesConstants.CadastrosOperacao + ",RECEPCAO,ADMINISTRADOR_CLINICA")]
[Route("api/agendamentos")]
public sealed class AgendamentosClinicosController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public AgendamentosClinicosController(Saude360ClinicalService service) { this.service = service; }
    [HttpGet] public async Task<IActionResult> Listar() { var r = await service.ListarAsync("agendamentos"); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> Obter(Guid id) { var r = await service.ObterAsync("agendamentos", id); return StatusCode(r.StatusCode, r); }
    [HttpPost] public async Task<IActionResult> Criar([FromBody] AgendamentoRequest request) { var r = await service.CriarAgendamentoAsync(request); return StatusCode(r.StatusCode, r); }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Atualizar(Guid id, [FromBody] AgendamentoRequest request) { var r = await service.AtualizarAgendamentoAsync(id, request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/confirmar")] public async Task<IActionResult> Confirmar(Guid id) { var r = await service.ConfirmarAgendamentoAsync(id); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/checkin")] public async Task<IActionResult> Checkin(Guid id) { var r = await service.CheckinAgendamentoAsync(id); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/cancelar")] public async Task<IActionResult> Cancelar(Guid id, [FromBody] AgendamentoCancelamentoRequest request) { var r = await service.CancelarAgendamentoAsync(id, request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/reagendar")] public async Task<IActionResult> Reagendar(Guid id, [FromBody] AgendamentoReagendamentoRequest request) { var r = await service.ReagendarAsync(id, request); return StatusCode(r.StatusCode, r); }
    [HttpGet("calendario")] public async Task<IActionResult> Calendario() { var r = await service.ListarAsync("agendamentos"); return StatusCode(r.StatusCode, r); }
    [HttpGet("medico/{medicoId:guid}")] public async Task<IActionResult> AgendaMedico(Guid medicoId) { var r = await service.ListarAsync("agendamentos", filtroId: medicoId, filtroColuna: "medico_id"); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize(Roles = RolesConstants.CadastrosOperacao + ",TRIAGEM,ADMINISTRADOR_CLINICA")]
[Route("api/triagens")]
public sealed class TriagensController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public TriagensController(Saude360ClinicalService service) { this.service = service; }
    [HttpGet] public async Task<IActionResult> Listar() { var r = await service.ListarAsync("triagens"); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> Obter(Guid id) { var r = await service.ObterAsync("triagens", id); return StatusCode(r.StatusCode, r); }
    [HttpPost] public async Task<IActionResult> Criar([FromBody] TriagemRequest request) { var r = await service.CriarTriagemAsync(request); return StatusCode(r.StatusCode, r); }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Atualizar(Guid id, [FromBody] TriagemRequest request) { var r = await service.AtualizarTriagemAsync(id, request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/finalizar")] public async Task<IActionResult> Finalizar(Guid id) { var r = await service.FinalizarTriagemAsync(id); return StatusCode(r.StatusCode, r); }
    [HttpGet("fila")] public async Task<IActionResult> Fila() { var r = await service.ListarAsync("triagens", "EM_TRIAGEM"); return StatusCode(r.StatusCode, r); }
    [HttpGet("paciente/{pacienteId:guid}")] public async Task<IActionResult> Paciente(Guid pacienteId) { var r = await service.ListarAsync("triagens", filtroId: pacienteId, filtroColuna: "paciente_id"); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize(Roles = RolesConstants.CadastrosOperacao + "," + RolesConstants.Medico + ",ADMINISTRADOR_CLINICA")]
[Route("api/consultas")]
public sealed class ConsultasClinicasController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public ConsultasClinicasController(Saude360ClinicalService service) { this.service = service; }
    [HttpGet] public async Task<IActionResult> Listar() { var r = await service.ListarAsync("consultas"); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> Obter(Guid id) { var r = await service.ObterAsync("consultas", id); return StatusCode(r.StatusCode, r); }
    [HttpPost] public async Task<IActionResult> Criar([FromBody] ConsultaRequest request) { var r = await service.CriarConsultaAsync(request); return StatusCode(r.StatusCode, r); }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Atualizar(Guid id, [FromBody] ConsultaRequest request) { var r = await service.AtualizarConsultaAsync(id, request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/iniciar")] public async Task<IActionResult> Iniciar(Guid id) { var r = await service.IniciarConsultaAsync(id); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/finalizar")] public async Task<IActionResult> Finalizar(Guid id) { var r = await service.FinalizarConsultaAsync(id); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/cancelar")] public async Task<IActionResult> Cancelar(Guid id, [FromBody] ConsultaRequest request) { var r = await service.CancelarConsultaAsync(id, request); return StatusCode(r.StatusCode, r); }
    [HttpGet("paciente/{pacienteId:guid}")] public async Task<IActionResult> Paciente(Guid pacienteId) { var r = await service.ListarAsync("consultas", filtroId: pacienteId, filtroColuna: "paciente_id"); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize(Roles = RolesConstants.CadastrosOperacao + "," + RolesConstants.Medico + ",ADMINISTRADOR_CLINICA")]
[Route("api/cid")]
public sealed class CidController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public CidController(Saude360ClinicalService service) { this.service = service; }
    [HttpGet] public async Task<IActionResult> Listar() { var r = await service.ListarAsync("cid_tabela"); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> Obter(Guid id) { var r = await service.ObterAsync("cid_tabela", id); return StatusCode(r.StatusCode, r); }
    [HttpGet("buscar")] public async Task<IActionResult> Buscar([FromQuery] string termo) { var r = await service.BuscarCidAsync(termo); return StatusCode(r.StatusCode, r); }
    [HttpPost] public async Task<IActionResult> Criar([FromBody] CidRequest request) { var r = await service.CriarCidAsync(request); return StatusCode(r.StatusCode, r); }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Atualizar(Guid id, [FromBody] CidRequest request) { var r = await service.AtualizarCidAsync(id, request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/inativar")] public async Task<IActionResult> Inativar(Guid id) { var r = await service.InativarCidAsync(id); return StatusCode(r.StatusCode, r); }
    [HttpPost("importar")] public async Task<IActionResult> Importar([FromBody] IEnumerable<CidRequest> requests) { var r = await service.ImportarCidAsync(requests); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/favoritar")] public async Task<IActionResult> Favoritar(Guid id) { var r = await service.FavoritarCidAsync(id); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize(Roles = RolesConstants.Medico + ",ADMINISTRADOR_CLINICA")]
[Route("api/prescricoes")]
public sealed class PrescricoesController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public PrescricoesController(Saude360ClinicalService service) { this.service = service; }
    [HttpGet] public async Task<IActionResult> Listar() { var r = await service.ListarAsync("prescricoes"); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> Obter(Guid id) { var r = await service.ObterAsync("prescricoes", id); return StatusCode(r.StatusCode, r); }
    [HttpPost] public async Task<IActionResult> Criar([FromBody] PrescricaoRequest request) { var r = await service.CriarPrescricaoAsync(request); return StatusCode(r.StatusCode, r); }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Atualizar(Guid id, [FromBody] PrescricaoRequest request) { var r = await service.AtualizarPrescricaoAsync(id, request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/finalizar")] public async Task<IActionResult> Finalizar(Guid id) { var r = await service.FinalizarPrescricaoAsync(id); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/cancelar")] public async Task<IActionResult> Cancelar(Guid id, [FromBody] PrescricaoRequest request) { var r = await service.CancelarPrescricaoAsync(id, request); return StatusCode(r.StatusCode, r); }
    [HttpGet("paciente/{pacienteId:guid}")] public async Task<IActionResult> Paciente(Guid pacienteId) { var r = await service.ListarAsync("prescricoes", filtroId: pacienteId, filtroColuna: "paciente_id"); return StatusCode(r.StatusCode, r); }
    [HttpGet("modelos")] public async Task<IActionResult> Modelos() { var r = await service.ListarAsync("prescricao_modelos"); return StatusCode(r.StatusCode, r); }
    [HttpPost("modelos")] public async Task<IActionResult> CriarModelo([FromBody] PrescricaoModeloRequest request) { var r = await service.CriarModeloPrescricaoAsync(request); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize(Roles = RolesConstants.FinanceiroGestao + ",FINANCEIRO_CLINICA,ADMINISTRADOR_CLINICA")]
[Route("api/clinica-financeiro")]
public sealed class ClinicaFinanceiroController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public ClinicaFinanceiroController(Saude360ClinicalService service) { this.service = service; }
    [HttpGet("resumo")] public async Task<IActionResult> Resumo() { var r = await service.ListarAsync("clinica_recebimentos"); return StatusCode(r.StatusCode, r); }
    [HttpGet("contas-receber")] public async Task<IActionResult> Contas() { var r = await service.ListarAsync("clinica_contas_receber"); return StatusCode(r.StatusCode, r); }
    [HttpPost("receber")] public async Task<IActionResult> Receber([FromBody] ClinicaRecebimentoRequest request) { var r = await service.ReceberAsync(request); return StatusCode(r.StatusCode, r); }
    [HttpPost("cancelar")] public async Task<IActionResult> Cancelar([FromBody] ClinicaRecebimentoRequest request) { var r = await service.EstornarFinanceiroAsync(request.ContaReceberId, request); return StatusCode(r.StatusCode, r); }
    [HttpPost("estornar")] public async Task<IActionResult> Estornar([FromBody] ClinicaRecebimentoRequest request) { var r = await service.EstornarFinanceiroAsync(request.ContaReceberId, request); return StatusCode(r.StatusCode, r); }
    [HttpGet("caixa")] public async Task<IActionResult> Caixa() { var r = await service.ListarAsync("clinica_caixa"); return StatusCode(r.StatusCode, r); }
    [HttpPost("fechar-caixa")] public async Task<IActionResult> FecharCaixa([FromBody] ClinicaRecebimentoRequest request) { var r = await service.FecharCaixaAsync(request); return StatusCode(r.StatusCode, r); }
    [HttpGet("relatorios")] public async Task<IActionResult> Relatorios() { var r = await service.ListarAsync("clinica_lancamentos"); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize(Roles = RolesConstants.CadastrosOperacao + ",FATURAMENTO_CONVENIO,ADMINISTRADOR_CLINICA")]
[Route("api/convenios")]
public sealed class ConveniosController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public ConveniosController(Saude360ClinicalService service) { this.service = service; }
    [HttpGet] public async Task<IActionResult> Listar() { var r = await service.ListarAsync("convenios"); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> Obter(Guid id) { var r = await service.ObterAsync("convenios", id); return StatusCode(r.StatusCode, r); }
    [HttpPost] public async Task<IActionResult> Criar([FromBody] ConvenioRequest request) { var r = await service.CriarConvenioAsync(request); return StatusCode(r.StatusCode, r); }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Atualizar(Guid id, [FromBody] ConvenioRequest request) { var r = await service.AtualizarConvenioAsync(id, request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/inativar")] public async Task<IActionResult> Inativar(Guid id, [FromBody] ConvenioRequest request) { var r = await service.InativarConvenioAsync(id, request); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}/planos")] public async Task<IActionResult> Planos(Guid id) { var r = await service.ListarAsync("convenio_planos"); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/planos")] public async Task<IActionResult> CriarPlano(Guid id, [FromBody] PlanoSaudeRequest request) { var r = await service.CriarPlanoConvenioAsync(id, request); return StatusCode(r.StatusCode, r); }
    [HttpGet("autorizacoes")] public async Task<IActionResult> Autorizacoes() { var r = await service.ListarAsync("convenio_autorizacoes"); return StatusCode(r.StatusCode, r); }
    [HttpPost("autorizacoes")] public async Task<IActionResult> CriarAutorizacao([FromBody] ConvenioAutorizacaoRequest request) { var r = await service.CriarAutorizacaoConvenioAsync(request); return StatusCode(r.StatusCode, r); }
    [HttpPost("autorizacoes/{id:guid}/aprovar")] public async Task<IActionResult> Aprovar(Guid id, [FromBody] ConvenioAutorizacaoRequest request) { var r = await service.DecidirAutorizacaoAsync(id, "APROVADA", request); return StatusCode(r.StatusCode, r); }
    [HttpPost("autorizacoes/{id:guid}/negar")] public async Task<IActionResult> Negar(Guid id, [FromBody] ConvenioAutorizacaoRequest request) { var r = await service.DecidirAutorizacaoAsync(id, "NEGADA", request); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize(Roles = RolesConstants.CadastrosOperacao + ",FATURAMENTO_CONVENIO,ADMINISTRADOR_CLINICA")]
[Route("api/planos-saude")]
public sealed class PlanosSaudeController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public PlanosSaudeController(Saude360ClinicalService service) { this.service = service; }
    [HttpGet] public async Task<IActionResult> Listar() { var r = await service.ListarAsync("planos_saude"); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> Obter(Guid id) { var r = await service.ObterAsync("planos_saude", id); return StatusCode(r.StatusCode, r); }
    [HttpPost] public async Task<IActionResult> Criar([FromBody] PlanoSaudeRequest request) { var r = await service.CriarPlanoSaudeAsync(request); return StatusCode(r.StatusCode, r); }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Atualizar(Guid id, [FromBody] PlanoSaudeRequest request) { var r = await service.AtualizarPlanoSaudeAsync(id, request); return StatusCode(r.StatusCode, r); }
    [HttpPost("{id:guid}/inativar")] public async Task<IActionResult> Inativar(Guid id, [FromBody] PlanoSaudeRequest request) { var r = await service.InativarPlanoSaudeAsync(id, request); return StatusCode(r.StatusCode, r); }
    [HttpGet("paciente/{pacienteId:guid}")] public async Task<IActionResult> Paciente(Guid pacienteId) { var r = await service.ListarAsync("plano_saude_pacientes", filtroId: pacienteId, filtroColuna: "paciente_id"); return StatusCode(r.StatusCode, r); }
    [HttpPost("paciente/{pacienteId:guid}")] public async Task<IActionResult> Vincular(Guid pacienteId, [FromBody] PlanoSaudePacienteRequest request) { var r = await service.VincularPlanoPacienteAsync(pacienteId, request); return StatusCode(r.StatusCode, r); }
}
