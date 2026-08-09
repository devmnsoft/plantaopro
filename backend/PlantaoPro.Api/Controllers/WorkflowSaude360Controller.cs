using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/workflow-saude360")]
public sealed class WorkflowSaude360Controller : ControllerBase
{
    private readonly IWorkflowSaude360Service service;

    public WorkflowSaude360Controller(IWorkflowSaude360Service service) => this.service = service;

    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo([FromQuery] WorkflowSaude360Filtro filtro, CancellationToken ct) =>
        Ok(ApiResponse<WorkflowResumoDto>.Ok(await service.ResumoAsync(filtro, ct), "Resumo do workflow carregado."));

    [HttpGet("proxima-acao")]
    public async Task<IActionResult> ProximaAcao([FromQuery] WorkflowSaude360Filtro filtro, CancellationToken ct) =>
        Ok(ApiResponse<WorkflowProximaAcaoDto>.Ok(await service.ProximaAcaoAsync(filtro, ct), "Próxima ação carregada."));

    [HttpGet("etapas")]
    public async Task<IActionResult> Etapas([FromQuery] WorkflowSaude360Filtro filtro, CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyList<WorkflowEtapaDto>>.Ok(await service.EtapasAsync(filtro, ct), "Etapas do workflow carregadas."));

    [HttpGet("pendencias")]
    public async Task<IActionResult> Pendencias([FromQuery] WorkflowSaude360Filtro filtro, CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyList<WorkflowPendenciaDto>>.Ok(await service.PendenciasAsync(filtro, ct), "Pendências do workflow carregadas."));
}

public sealed class WorkflowSaude360Filtro
{
    public DateTimeOffset? Inicio { get; set; }
    public DateTimeOffset? Fim { get; set; }
    public Guid? UnidadeId { get; set; }
    public Guid? ProfissionalId { get; set; }
    public string? Status { get; set; }
}

public sealed class WorkflowResumoDto
{
    public int TotalPacientes { get; set; }
    public int Agendados { get; set; }
    public int Confirmados { get; set; }
    public int Checkins { get; set; }
    public int AguardandoTriagem { get; set; }
    public int EmTriagem { get; set; }
    public int AguardandoConsulta { get; set; }
    public int EmAtendimento { get; set; }
    public int Finalizados { get; set; }
    public int ContasPendentes { get; set; }
    public int PagamentosRecebidos { get; set; }
    public int PendenciasCriticas { get; set; }
    public string ProximaAcao { get; set; } = string.Empty;
}

public sealed class WorkflowProximaAcaoDto { public string Titulo { get; set; } = string.Empty; public string Descricao { get; set; } = string.Empty; public string Link { get; set; } = string.Empty; public string PerfilResponsavel { get; set; } = string.Empty; public string Prioridade { get; set; } = string.Empty; }
public sealed class WorkflowEtapaDto { public string Codigo { get; set; } = string.Empty; public string Nome { get; set; } = string.Empty; public string Descricao { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; public int Quantidade { get; set; } public int Pendencias { get; set; } public string Link { get; set; } = string.Empty; public string PerfilResponsavel { get; set; } = string.Empty; public string ProximaAcao { get; set; } = string.Empty; }
public sealed class WorkflowPendenciaDto { public Guid Id { get; set; } public string Titulo { get; set; } = string.Empty; public string Descricao { get; set; } = string.Empty; public string Prioridade { get; set; } = string.Empty; public string LinkResolucao { get; set; } = string.Empty; public string PerfilResponsavel { get; set; } = string.Empty; public DateTimeOffset? Prazo { get; set; } }
