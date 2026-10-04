using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Ai;

namespace PlantaoPro.Api.Controllers;

/// <summary>
/// P2 IA — rotas da camada canônica de assistentes (api/ai).
/// Autorização no servidor em toda leitura/escrita:
///   - configuração: somente administradores (global / tenant) com contexto de cliente;
///   - tarefas: autenticados; análise de cotação exige a mesma política do módulo
///     Administrativo 360 (Adm360.CotacaoConsultar) — o contexto é sempre montado
///     no servidor após autorização, e o tenant vem da sessão (nunca do corpo).
/// </summary>
[ApiController]
[Route("api/ai")]
[Authorize]
public sealed class AiController : ControllerBase
{
    private readonly IAiGateway _gateway;
    private readonly AiJornadaMeuDia _meuDia;
    private readonly AiJornadaCotacao _cotacao;
    private readonly ICurrentUserService _current;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IAiGateway gateway,
        AiJornadaMeuDia meuDia,
        AiJornadaCotacao cotacao,
        ICurrentUserService current,
        ILogger<AiController> logger)
    {
        _gateway = gateway;
        _meuDia = meuDia;
        _cotacao = cotacao;
        _current = current;
        _logger = logger;
    }

    // ------------------------------------------------------------------
    // Configuração (admin global / admin do tenant)
    // ------------------------------------------------------------------

    /// <summary>Listagem por tarefa: chave do tenant apenas mascarada; sugestão de modelo por provedor.</summary>
    [HttpGet("config")]
    [Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE")]
    public async Task<IActionResult> Config(CancellationToken ct)
    {
        var views = await _gateway.ObterConfiguracoesAsync(ct);
        return Ok(new
        {
            configuracoes = views,
            chaveMestraDoServidorConfigurada = _gateway.MasterKeyConfigurada
        });
    }

    [HttpPut("config/{task}")]
    [Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE")]
    public async Task<IActionResult> SalvarConfig(string task, [FromBody] AiConfigUpdate update, CancellationToken ct)
    {
        if (update is null)
            return BadRequest(new { mensagem = "Corpo da requisição ausente." });
        try
        {
            var view = await _gateway.SalvarConfiguracaoAsync(task, update, ct);
            return Ok(view);
        }
        catch (AiConfigException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpPost("test-conexao")]
    [Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE")]
    public async Task<IActionResult> TestarConexao([FromBody] AiTestarConexaoRequest req, CancellationToken ct)
        => Ok(await _gateway.TestarConexaoAsync(req?.Provedor ?? string.Empty, ct));

    // ------------------------------------------------------------------
    // Tarefas (jornadas da entrega inicial)
    // ------------------------------------------------------------------

    /// <summary>Resumo das pendências do Meu Dia (autenticado, com tenant na sessão).</summary>
    [HttpPost("tarefas/meu-dia-resumo")]
    public async Task<IActionResult> ResumoMeuDia(CancellationToken ct)
    {
        if (!_TemTenant())
            return Ok(NaoHabilitado("Selecione um cliente/instituição para usar o assistente."));
        AiTaskExecution? exec;
        try
        {
            exec = await _meuDia.ConstruirAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "IA: falha ao montar o contexto do Meu Dia.");
            return Ok(new AiOutcome(false, AiErrorKinds.RespostaInvalida,
                "Não foi possível carregar as pendências para montar o resumo."));
        }
        if (exec is null)
            return Ok(new AiOutcome(true, AiErrorKinds.Vazio,
                "Não há pendências no período. Nada para resumir."));
        return Ok(await _gateway.ExecutarTarefaAsync(AiTaskCodes.MeuDiaResumo, exec, ct));
    }

    /// <summary>Análise de cotação (Administrativo 360) — mesma política do módulo.</summary>
    [HttpPost("tarefas/analise-cotacao")]
    [Authorize(Policy = "Adm360.CotacaoConsultar")]
    public async Task<IActionResult> AnaliseCotacao([FromBody] AiAnaliseCotacaoRequest req, CancellationToken ct)
    {
        if (req is null || !_TemTenant())
            return Ok(NaoHabilitado("Selecione um cliente/instituição para usar o assistente."));
        AiTaskExecution? exec;
        try
        {
            exec = await _cotacao.ConstruirAsync(req.CotacaoId, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "IA: falha ao montar o contexto da cotação {CotacaoId}.", req.CotacaoId);
            return Ok(new AiOutcome(false, AiErrorKinds.RespostaInvalida,
                "Não foi possível carregar a cotação para montar a análise."));
        }
        if (exec is null)
            return NotFound(new { mensagem = "Cotação não encontrada neste cliente." });
        return Ok(await _gateway.ExecutarTarefaAsync(AiTaskCodes.CotacaoAnalise, exec, ct));
    }

    private bool _TemTenant()
    {
        var t = _current.TenantId;
        return t is not null && t != Guid.Empty;
    }

    private static AiOutcome NaoHabilitado(string mensagem)
        => new(false, AiErrorKinds.NaoHabilitado, mensagem);
}

public sealed record AiTestarConexaoRequest(string? Provedor);
public sealed record AiAnaliseCotacaoRequest(Guid CotacaoId);
