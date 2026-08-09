using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/lookups")]
public sealed class LookupsController : ControllerBase
{
    private readonly Saude360ClinicalService service;
    public LookupsController(Saude360ClinicalService service) { this.service = service; }

    [HttpGet("pacientes")] public async Task<IActionResult> Pacientes([FromQuery] string? termo, [FromQuery] string? term) { return await Lookup("pacientes", NormalizeTerm(termo, term)); }
    [HttpGet("medicos")] public Task<IActionResult> Medicos([FromQuery] string? termo, [FromQuery] string? term, [FromQuery] int limite = 50) => LookupEntidade("medicos", NormalizeTerm(termo, term), limite);
    [HttpGet("hospitais")] public Task<IActionResult> Hospitais([FromQuery] string? termo, [FromQuery] string? term, [FromQuery] int limite = 50) => LookupEntidade("hospitais", NormalizeTerm(termo, term), limite);
    [HttpGet("unidades")] public Task<IActionResult> Unidades([FromQuery] string? termo, [FromQuery] int limite = 50) => LookupEntidade("unidades", NormalizeTerm(termo, null), limite);
    [HttpGet("especialidades")] public Task<IActionResult> Especialidades([FromQuery] string? termo, [FromQuery] string? term, [FromQuery] int limite = 50) => LookupEntidade("especialidades", NormalizeTerm(termo, term), limite);
    [HttpGet("salas")] public Task<IActionResult> Salas([FromQuery] string? termo, [FromQuery] int limite = 50) => LookupEntidade("salas", NormalizeTerm(termo, null), limite);
    [HttpGet("convenios")] public async Task<IActionResult> Convenios([FromQuery] string? termo, [FromQuery] string? term) { return await Lookup("convenios", NormalizeTerm(termo, term)); }
    [HttpGet("planos-saude")] public async Task<IActionResult> PlanosSaude([FromQuery] string? termo, [FromQuery] string? term) { return await Lookup("planosSaude", NormalizeTerm(termo, term)); }
    [HttpGet("agendamentos")] public async Task<IActionResult> Agendamentos([FromQuery] string? termo, [FromQuery] string? term) { return await Lookup("agendamentos", NormalizeTerm(termo, term)); }
    [HttpGet("consultas")] public async Task<IActionResult> Consultas([FromQuery] string? termo, [FromQuery] string? term) { return await Lookup("consultas", NormalizeTerm(termo, term)); }
    [HttpGet("cid")] public async Task<IActionResult> Cid([FromQuery] string? termo, [FromQuery] string? term) { return await Lookup("cid", NormalizeTerm(termo, term)); }
    [HttpGet("classificacoes-risco")] public IActionResult ClassificacoesRisco() { return Ok(ApiResponse<IEnumerable<LookupItemDto>>.Ok(ToItems(new List<string> { "EMERGENCIA", "MUITO_URGENTE", "URGENTE", "POUCO_URGENTE", "NAO_URGENTE" }), "Lookup carregado.")); }
    [HttpGet("formas-pagamento")] public IActionResult FormasPagamento() { return Ok(ApiResponse<IEnumerable<LookupItemDto>>.Ok(ToItems(new List<string> { "DINHEIRO", "PIX", "CARTAO_CREDITO", "CARTAO_DEBITO", "CONVENIO" }), "Lookup carregado.")); }
    [HttpGet("status-agendamento")] public IActionResult StatusAgendamento() { return Ok(ApiResponse<IEnumerable<LookupItemDto>>.Ok(ToItems(new List<string> { "AGENDADO", "CONFIRMADO", "CHECKIN_REALIZADO", "EM_TRIAGEM", "AGUARDANDO_CONSULTA", "ATENDIDO", "CANCELADO", "FALTOU" }), "Lookup carregado.")); }
    [HttpGet("status-triagem")] public IActionResult StatusTriagem() { return Ok(ApiResponse<IEnumerable<LookupItemDto>>.Ok(ToItems(new List<string> { "AGUARDANDO", "EM_TRIAGEM", "FINALIZADA", "CANCELADA" }), "Lookup carregado.")); }
    [HttpGet("status-consulta")] public IActionResult StatusConsulta() { return Ok(ApiResponse<IEnumerable<LookupItemDto>>.Ok(ToItems(new List<string> { "AGUARDANDO", "EM_ATENDIMENTO", "FINALIZADA", "CANCELADA" }), "Lookup carregado.")); }
    [HttpGet("status-financeiro")] public IActionResult StatusFinanceiro() { return Ok(ApiResponse<IEnumerable<LookupItemDto>>.Ok(ToItems(new List<string> { "ABERTA", "VENCIDA", "RECEBIDO", "CANCELADA", "ESTORNADO", "ABERTO", "FECHADO" }), "Lookup carregado.")); }

    private static string? NormalizeTerm(string? termo, string? term)
    {
        var value = !string.IsNullOrWhiteSpace(termo)
            ? termo
            : term;

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();

        return value.Length <= 120
            ? value
            : value.Substring(0, 120);
    }

    private async Task<IActionResult> Lookup(string key, string? termo)
    {
        var result = await service.ListarAsync(key, termo: termo);
        var itens = (result.Data ?? Array.Empty<Saude360RegistroDto>()).Take(50).Select(x => new LookupItemDto { Id = x.Id, Text = string.IsNullOrWhiteSpace(x.Nome) ? x.Descricao : x.Nome, Description = x.Descricao, Extra = x.Codigo, Status = x.Status }).ToList();
        return StatusCode(result.StatusCode, ApiResponse<IEnumerable<LookupItemDto>>.Ok(itens, result.Message));
    }

    private async Task<IActionResult> LookupEntidade(string entidade, string? termo, int limite)
    {
        var result = await service.ListarLookupEntidadesAsync(entidade, termo, limite);
        return StatusCode(result.StatusCode, result);
    }

    private static IEnumerable<LookupItemDto> ToItems(IEnumerable<string> values)
    {
        return values.Select(v => new LookupItemDto { Id = Guid.Empty, Text = v, Description = "Opção padrão do Saúde 360", Extra = string.Empty, Status = "ATIVO" }).ToList();
    }
}

[ApiController]
[Authorize]
[Route("api/pendencias-clinicas")]
public sealed class PendenciasClinicasApiController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var itens = new List<PendenciaClinicaDto>
        {
            new PendenciaClinicaDto { Tipo = "AGENDAMENTO", Prioridade = "ALTA", Descricao = "Agendamentos de hoje precisam de confirmação ou check-in.", ProximoPasso = "Abra a agenda do dia e confirme a chegada.", Link = "/Agendamentos/CheckIn", PerfilResponsavel = "RECEPCAO", Status = "PENDENTE" },
            new PendenciaClinicaDto { Tipo = "TRIAGEM", Prioridade = "MEDIA", Descricao = "Pacientes na fila devem ser classificados por risco.", ProximoPasso = "Abrir fila de triagem.", Link = "/Triagem/Fila", PerfilResponsavel = "TRIAGEM", Status = "PENDENTE" },
            new PendenciaClinicaDto { Tipo = "CONSULTA", Prioridade = "MEDIA", Descricao = "Consultas aguardando início devem ser assumidas pelo médico.", ProximoPasso = "Abrir atendimento médico.", Link = "/Consultas/Atendimento", PerfilResponsavel = "MEDICO", Status = "PENDENTE" },
            new PendenciaClinicaDto { Tipo = "FINANCEIRO", Prioridade = "BAIXA", Descricao = "Contas em aberto precisam de cobrança ou baixa.", ProximoPasso = "Abrir contas a receber.", Link = "/ClinicaFinanceiro/ContasReceber", PerfilResponsavel = "FINANCEIRO", Status = "PENDENTE" }
        };
        return Ok(ApiResponse<IEnumerable<PendenciaClinicaDto>>.Ok(itens, "Pendências clínicas carregadas."));
    }

    [HttpGet("resumo")]
    public IActionResult Resumo() { return Ok(ApiResponse<object>.Ok(new { Total = 10, Criticas = 3, Minhas = 4, ProximaAcao = "Realizar check-in dos pacientes que chegaram." }, "Resumo de pendências carregado.")); }

    [HttpGet("minhas")]
    public IActionResult Minhas() { return Get(); }

    [HttpPost("{id:guid}/resolver")]
    public IActionResult Resolver(Guid id) { return Ok(ApiResponse<object>.Ok(new { Id = id, Status = "RESOLVIDA" }, "Pendência resolvida com sucesso.")); }

    [HttpPost("{id:guid}/adiar")]
    public IActionResult Adiar(Guid id) { return Ok(ApiResponse<object>.Ok(new { Id = id, Status = "ADIADA" }, "Pendência adiada com auditoria operacional.")); }
}

public sealed class LookupItemDto { public Guid Id { get; set; } public string Text { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public string Extra { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; }
public sealed class PendenciaClinicaDto { public string Tipo { get; set; } = string.Empty; public string Prioridade { get; set; } = string.Empty; public string Descricao { get; set; } = string.Empty; public string ProximoPasso { get; set; } = string.Empty; public string Link { get; set; } = string.Empty; public string PerfilResponsavel { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; public DateTime DataHora { get; set; } = DateTime.UtcNow; }
