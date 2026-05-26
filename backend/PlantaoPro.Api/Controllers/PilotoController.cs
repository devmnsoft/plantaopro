using System.Collections.Concurrent;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR")]
[Route("api/piloto")]
public sealed class PilotoController : ControllerBase
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<PilotoController> _logger;
    private static readonly ConcurrentDictionary<int, bool> ChecklistStatus = new();
    private static readonly ConcurrentDictionary<Guid, PilotoOcorrenciaDto> Ocorrencias = new();

    public PilotoController(IConfiguration cfg, ILogger<PilotoController> logger)
    {
        _cfg = cfg;
        _logger = logger;
    }

    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo()
    {
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var sql = @"select
                (select coalesce(max(nome_fantasia),'Cliente piloto') from plantaopro.clientes where reg_status='A') as clientePiloto,
                (select count(1) from plantaopro.usuarios where reg_status='A') as usuariosAtivos,
                (select count(1) from plantaopro.plantoes where reg_status='A') as plantoesCriados,
                (select count(1) from plantaopro.plantoes where reg_status='A' and status='publicado') as plantoesPublicados,
                (select count(1) from plantaopro.escalas where reg_status='A' and status='confirmada') as escalasConfirmadas,
                (select count(1) from plantaopro.pagamentos where reg_status='A' and status='confirmado') as pagamentosConfirmados,
                (select count(1) from plantaopro.notificacoes where reg_status='A') as notificacoesEnviadas";
            var row = await cn.QueryFirstAsync(sql);
            var abertas = Ocorrencias.Values.Count(x => x.Status == "ABERTA" || x.Status == "EM_ANALISE");
            var resolvidas = Ocorrencias.Values.Count(x => x.Status == "RESOLVIDA");
            var pendencias = ChecklistBase().Count(x => !ChecklistStatus.GetValueOrDefault(x.Id));
            return Ok(ApiResponse<object>.Ok(new
            {
                row.clientePiloto,
                row.usuariosAtivos,
                row.plantoesCriados,
                row.plantoesPublicados,
                row.escalasConfirmadas,
                row.pagamentosConfirmados,
                row.notificacoesEnviadas,
                ocorrenciasAbertas = abertas,
                ocorrenciasResolvidas = resolvidas,
                pendenciasImplantacao = pendencias
            }, "Resumo do piloto carregado com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar resumo do piloto");
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível carregar o resumo do piloto.", 500));
        }
    }

    [HttpGet("checklist")]
    public IActionResult Checklist()
    {
        var data = ChecklistBase().Select(x => new { x.Id, x.Titulo, Concluido = ChecklistStatus.GetValueOrDefault(x.Id) });
        return Ok(ApiResponse<object>.Ok(data, "Checklist carregado com sucesso."));
    }

    [HttpPost("checklist/{id:int}/concluir")]
    public IActionResult ConcluirChecklist(int id)
    {
        if (!ChecklistBase().Any(x => x.Id == id)) return NotFound(ApiResponse<object>.Fail("Item de checklist não encontrado.", 404));
        ChecklistStatus[id] = true;
        return Ok(ApiResponse<object>.Ok(new { id, concluido = true }, "Item concluído com sucesso."));
    }

    [HttpGet("ocorrencias")]
    public IActionResult GetOcorrencias() => Ok(ApiResponse<object>.Ok(Ocorrencias.Values.OrderByDescending(x => x.DataAbertura), "Ocorrências carregadas com sucesso."));

    [HttpPost("ocorrencias")]
    public IActionResult CriarOcorrencia([FromBody] PilotoNovaOcorrenciaRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Tipo) || string.IsNullOrWhiteSpace(request.Prioridade) || string.IsNullOrWhiteSpace(request.Descricao))
                return BadRequest(ApiResponse<object>.Fail("Verifique os campos obrigatórios.", 400));

            var id = Guid.NewGuid();
            var dto = new PilotoOcorrenciaDto(id, request.Tipo.Trim(), request.Prioridade.Trim(), request.Descricao.Trim(), "ABERTA", request.Responsavel?.Trim() ?? "Não definido", DateTime.UtcNow, null);
            Ocorrencias[id] = dto;
            return Ok(ApiResponse<object>.Ok(dto, "Ocorrência registrada com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar ocorrência de piloto");
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível registrar ocorrência.", 500));
        }
    }

    private static List<(int Id, string Titulo)> ChecklistBase() =>
    [
        (1, "Cliente cadastrado"), (2, "Usuários criados"), (3, "Hospitais cadastrados"), (4, "Médicos cadastrados"), (5, "Especialidades cadastradas"),
        (6, "Primeiro plantão criado"), (7, "Primeiro plantão publicado"), (8, "Primeiro médico escalado"), (9, "Primeiro pagamento confirmado"),
        (10, "Usuários treinados"), (11, "Homologação aprovada")
    ];

    public sealed record PilotoNovaOcorrenciaRequest(string Tipo, string Prioridade, string Descricao, string? Responsavel);
    public sealed record PilotoOcorrenciaDto(Guid Id, string Tipo, string Prioridade, string Descricao, string Status, string Responsavel, DateTime DataAbertura, DateTime? DataResolucao);
}
