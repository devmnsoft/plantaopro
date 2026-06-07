using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = RolesConstants.PlantoesGestao + "," + RolesConstants.EscalasGestao + "," + RolesConstants.Financeiro)]
[Route("api/central-escala")]
public sealed class CentralEscalaController : ControllerBase
{
    private readonly OperacaoService _operacaoService;
    private readonly PlantaoService _plantaoService;
    private readonly ILogger<CentralEscalaController> _logger;

    public CentralEscalaController(OperacaoService operacaoService, PlantaoService plantaoService, ILogger<CentralEscalaController> logger)
    {
        _operacaoService = operacaoService;
        _plantaoService = plantaoService;
        _logger = logger;
    }

    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo()
    {
        try
        {
            var response = await _operacaoService.GetResumoAsync(Uid(), HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar resumo da central de escala");
            return StatusCode(500, ApiResponse<string>.Fail("Erro ao carregar resumo da central de escala.", 500));
        }
    }

    [HttpGet("plantoes")]
    public async Task<IActionResult> Plantoes([FromQuery] PlantaoFilterRequest filter)
    {
        try
        {
            var status = string.IsNullOrWhiteSpace(filter.Status) ? "aberto" : filter.Status;
            var response = await _plantaoService.GetAllAsync(filter with { Status = status, PageSize = Math.Clamp(filter.PageSize, 1, 50) });
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar plantões da central de escala");
            return StatusCode(500, ApiResponse<string>.Fail("Erro ao listar plantões da central de escala.", 500));
        }
    }

    [HttpGet("pendencias")]
    public async Task<IActionResult> Pendencias()
    {
        try
        {
            var resumo = await _operacaoService.GetResumoAsync(Uid(), HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
            if (!resumo.Success || resumo.Data is null)
            {
                return StatusCode(resumo.StatusCode, ApiResponse<IEnumerable<CentralEscalaPendenciaDto>>.Fail(resumo.Message, resumo.StatusCode));
            }

            var pendencias = new List<CentralEscalaPendenciaDto>();
            foreach (var plantao in resumo.Data.PlantoesCriticos)
            {
                pendencias.Add(new CentralEscalaPendenciaDto { Tipo = "PLANTAO_SEM_COBERTURA", Prioridade = "ALTA", Titulo = "Plantão aberto exige cobertura", Descricao = plantao.HospitalNome + " - " + plantao.EspecialidadeNome, ReferenciaId = plantao.Id, DataReferencia = plantao.DataInicio });
            }
            foreach (var escala in resumo.Data.EscalasPendentes)
            {
                pendencias.Add(new CentralEscalaPendenciaDto { Tipo = "ESCALA_SOLICITADA", Prioridade = "MEDIA", Titulo = "Escala aguardando decisão", Descricao = escala.MedicoNome + " em " + escala.HospitalNome, ReferenciaId = escala.Id, DataReferencia = escala.DataInicio });
            }
            foreach (var pagamento in resumo.Data.PagamentosPendentes)
            {
                pendencias.Add(new CentralEscalaPendenciaDto { Tipo = "PAGAMENTO_PENDENTE", Prioridade = "MEDIA", Titulo = "Pagamento pendente", Descricao = pagamento.MedicoNome + " - " + pagamento.ValorPrevisto.ToString("C"), ReferenciaId = pagamento.Id, DataReferencia = pagamento.DataPrevista?.ToDateTime(TimeOnly.MinValue) });
            }

            return Ok(ApiResponse<IEnumerable<CentralEscalaPendenciaDto>>.Ok(pendencias));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar pendências da central de escala");
            return StatusCode(500, ApiResponse<string>.Fail("Erro ao carregar pendências da central de escala.", 500));
        }
    }

    [HttpGet("acoes-recomendadas")]
    public async Task<IActionResult> AcoesRecomendadas()
    {
        try
        {
            var resumo = await _operacaoService.GetResumoAsync(Uid(), HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
            if (!resumo.Success || resumo.Data is null)
            {
                return StatusCode(resumo.StatusCode, ApiResponse<IEnumerable<CentralEscalaAcaoRecomendadaDto>>.Fail(resumo.Message, resumo.StatusCode));
            }

            var acoes = new List<CentralEscalaAcaoRecomendadaDto>();
            if (resumo.Data.TotalPlantoesAbertos > 0)
            {
                acoes.Add(new CentralEscalaAcaoRecomendadaDto { Codigo = "RECOMENDAR_MEDICOS", Titulo = "Gerar recomendações", Descricao = "Existem plantões abertos que podem receber recomendação automática de médicos.", Severidade = "ALTA", UrlSugerida = "/Plantoes" });
            }
            if (resumo.Data.TotalEscalasSolicitadas > 0)
            {
                acoes.Add(new CentralEscalaAcaoRecomendadaDto { Codigo = "CONFIRMAR_ESCALAS", Titulo = "Analisar solicitações", Descricao = "Escalas solicitadas aguardam confirmação ou recusa da coordenação.", Severidade = "MEDIA", UrlSugerida = "/Escalas" });
            }
            if (resumo.Data.TotalPagamentosPendentes > 0)
            {
                acoes.Add(new CentralEscalaAcaoRecomendadaDto { Codigo = "BAIXAR_PAGAMENTOS", Titulo = "Regularizar financeiro", Descricao = "Pagamentos pendentes/atrasados impactam experiência médica.", Severidade = "MEDIA", UrlSugerida = "/Financeiro" });
            }

            return Ok(ApiResponse<IEnumerable<CentralEscalaAcaoRecomendadaDto>>.Ok(acoes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar ações recomendadas da central de escala");
            return StatusCode(500, ApiResponse<string>.Fail("Erro ao carregar ações recomendadas.", 500));
        }
    }

    private Guid Uid()
    {
        var uid = User.FindFirst("uid")?.Value;
        return Guid.TryParse(uid, out var parsed) ? parsed : Guid.Empty;
    }
}
