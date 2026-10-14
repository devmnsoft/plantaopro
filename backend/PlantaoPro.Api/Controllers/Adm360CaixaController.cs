using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Api.Controllers;

/// <summary>R8-B27 (§27): resposta do Fluxo de Caixa que carrega as definições fiéis ao SQL dos indicadores.
/// Vive na camada Api porque KpiDefinicao está no projeto Api (o DTO de Aplicação não o referencia).
/// Mantém os mesmos campos de topo de FluxoCaixaDto para a BFF Web mapear 1:1 por nome (STJ).</summary>
public sealed record FluxoCaixaResposta(
    DateOnly DataBase,
    decimal SaldoAtualContas,
    decimal SaldoAberturaPeriodo,
    decimal SaldoFechamentoPeriodo,
    decimal TotalEntradasPrevistas,
    decimal TotalSaidasPrevistas,
    decimal TotalEntradasRealizadas,
    decimal TotalSaidasRealizadas,
    decimal TotalVencidosReceber,
    decimal TotalVencidosPagar,
    IReadOnlyList<FluxoCaixaItemDto> Itens,
    IReadOnlyList<KpiDefinicao> Definicoes,
    string TimeCriterion);

[ApiController, Route("api/administrativo360/caixa"), Authorize(Policy = "Adm360.Financeiro")]
public sealed class Adm360CaixaController : ControllerBase
{
    private readonly ICaixaRepository repository;
    private readonly ICurrentUserService current;
    private readonly ILogger<Adm360CaixaController> logger;

    public Adm360CaixaController(
        ICaixaRepository repository,
        ICurrentUserService current,
        ILogger<Adm360CaixaController> logger)
    {
        this.repository = repository;
        this.current = current;
        this.logger = logger;
    }

    private (Guid Tenant, Guid User) Context() =>
        (current.TenantId ?? throw new UnauthorizedAccessException("Tenant não identificado."),
         current.UserId ?? throw new UnauthorizedAccessException("Usuário não autenticado."));

    [HttpGet("contas")]
    public async Task<IActionResult> ListarContas(CancellationToken ct)
    {
        var (tenant, _) = Context();
        var lista = await repository.ListarContasAsync(tenant, ct);
        return Ok(lista);
    }

    [HttpGet("contas/{id:guid}")]
    public async Task<IActionResult> ObterConta(Guid id, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var conta = await repository.ObterContaPorIdAsync(tenant, id, ct);
        if (conta is null) return NotFound(new { error = "Conta financeira não encontrada." });
        return Ok(conta);
    }

    [HttpPost("contas")]
    public async Task<IActionResult> CriarConta([FromBody] CriarContaFinanceiraCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var id = await repository.CriarContaAsync(tenant, user, command, ct);
        logger.LogInformation("Conta financeira {ContaId} criada para tenant {TenantId}.", id, tenant);
        return CreatedAtAction(nameof(ObterConta), new { id }, new { id });
    }

    [HttpPut("contas/{id:guid}")]
    public async Task<IActionResult> AtualizarConta(Guid id, [FromBody] AtualizarContaFinanceiraCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        if (id != command.ContaId)
            command = command with { ContaId = id };

        await repository.AtualizarContaAsync(tenant, user, command, ct);
        logger.LogInformation("Conta financeira {ContaId} atualizada.", id);
        return Ok(new { success = true });
    }

    [HttpDelete("contas/{id:guid}")]
    public async Task<IActionResult> InativarConta(Guid id, CancellationToken ct)
    {
        var (tenant, user) = Context();
        await repository.InativarContaAsync(tenant, user, id, ct);
        logger.LogInformation("Conta financeira {ContaId} inativada.", id);
        return Ok(new { success = true });
    }

    [HttpGet("contas/{id:guid}/extrato")]
    public async Task<IActionResult> Extrato(
        Guid id, [FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim, CancellationToken ct)
    {
        var (tenant, _) = Context();
        try
        {
            var extrato = await repository.ExtratoContaAsync(tenant, id, inicio, fim, ct);
            return Ok(extrato);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("fluxo")]
    public async Task<IActionResult> Fluxo(
        [FromQuery] DateOnly inicio, [FromQuery] DateOnly fim, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var fluxo = await repository.FluxoCaixaAsync(tenant, inicio, fim, ct);
        var resposta = new FluxoCaixaResposta(
            fluxo.DataBase,
            fluxo.SaldoAtualContas,
            fluxo.SaldoAberturaPeriodo,
            fluxo.SaldoFechamentoPeriodo,
            fluxo.TotalEntradasPrevistas,
            fluxo.TotalSaidasPrevistas,
            fluxo.TotalEntradasRealizadas,
            fluxo.TotalSaidasRealizadas,
            fluxo.TotalVencidosReceber,
            fluxo.TotalVencidosPagar,
            fluxo.Itens,
            Definicoes(),
            TempoCritico);
        return Ok(resposta);
    }

    private static string TempoCritico =>
        "Período [início, fim] (inclusivo, data do filtro) para Entradas/Saídas Realizadas e Previsões (AR/AP); " +
        "estado atual em now() (UTC, fuso do servidor) para Saldo Atual das Contas e Vencidos em Aberto; " +
        "Abertura/Fechamento derivados no servidor. Sem fuso do usuário.";

    private static IReadOnlyList<KpiDefinicao> Definicoes() => new[]
    {
        new KpiDefinicao("SaldoAtualContas", "Saldo Atual das Contas", "Soma do saldo atual derivado de todas as contas financeiras ativas do tenant; o saldo por conta é o saldo inicial documentado + soma(movimentos ENTRADA - SAIDA) em adm360_movimentos_financeiros; não há saldo gravado, logo nunca diverge dos movimentos; estado atual (data-base hoje)."),
        new KpiDefinicao("SaldoAberturaPeriodo", "Saldo Abertura Período", "Soma dos saldos iniciais das contas com DataSaldoInicial <= início + soma dos movimentos (ENTRADA - SAIDA) com data_movimento < @inicio; posição antes do período filtrado."),
        new KpiDefinicao("TotalEntradasRealizadas", "Entradas Realizadas", "Soma dos valores dos movimentos tipo 'ENTRADA' em adm360_movimentos_financeiros com data_movimento dentro de [início, fim] (inclusivo)."),
        new KpiDefinicao("TotalSaidasRealizadas", "Saídas Realizadas", "Soma dos valores dos movimentos tipo 'SAIDA' em adm360_movimentos_financeiros com data_movimento dentro de [início, fim] (inclusivo)."),
        new KpiDefinicao("SaldoFechamentoPeriodo", "Saldo Fechamento Período", "Calculado no servidor (fórmula derivada, não gravada): Abertura do período + Entradas Realizadas - Saídas Realizadas."),
        new KpiDefinicao("TotalEntradasPrevistas", "Entradas Previstas (AR)", "Soma de saldo_aberto de títulos a receber (adm360_titulos_receber) com situacao IN ('ABERTO','PARCIAL') e data_vencimento dentro de [início, fim]; são projeções e NÃO entram no Saldo Acumulado das linhas."),
        new KpiDefinicao("TotalSaidasPrevistas", "Saídas Previstas (AP)", "Soma de saldo_aberto de títulos a pagar (adm360_titulos_pagar) com situacao IN ('PENDENTE_APROVACAO','APROVADO','PARCIAL') e data_vencimento dentro de [início, fim]; projeções e NÃO entram no Saldo Acumulado."),
        new KpiDefinicao("TotalVencidos", "Vencidos em Aberto (AR+AP)", "Títulos com data_vencimento < hoje (independente do período): AR = soma de saldo_aberto em ABERTO/PARCIAL (adm360_titulos_receber); AP = soma de saldo_aberto em PENDENTE_APROVACAO/APROVADO/PARCIAL (adm360_titulos_pagar)."),
        new KpiDefinicao("Estorno", "Tratamento de estorno", "O estorno gera movimento compensatório tipo SAIDA datado na data do estorno (origem ESTORNO_RECEBIMENTO), reduzindo o saldo derivado da conta onde o recebimento caiu; o título volta a ABERTO/PARCIAL e as comissões da baixa viram ESTORNADA. Estorno é bloqueado dentro de fechamento de caixa fechado."),
    };

    [HttpGet("fechamentos")]
    public async Task<IActionResult> ListarFechamentos([FromQuery] Guid? contaId, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var lista = await repository.ListarFechamentosAsync(tenant, contaId, ct);
        return Ok(lista);
    }

    [HttpPost("fechar"), Authorize(Policy = "Adm360.FecharCaixa")]
    public async Task<IActionResult> FecharCaixa([FromBody] FecharCaixaCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var fechamentoId = await repository.FecharCaixaAsync(tenant, user, command, ct);
        logger.LogInformation("Fechamento de caixa {FechamentoId} registrado para a conta {ContaId}.", fechamentoId, command.ContaId);
        return Ok(new { id = fechamentoId });
    }

    [HttpPost("fechamentos/{id:guid}/reabrir"), Authorize(Policy = "Adm360.FecharCaixa")]
    public async Task<IActionResult> ReabrirCaixa(Guid id, [FromBody] ReabrirCaixaCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        if (id != command.FechamentoId)
            command = command with { FechamentoId = id };

        await repository.ReabrirCaixaAsync(tenant, user, command, ct);
        logger.LogInformation("Fechamento de caixa {FechamentoId} reaberto.", id);
        return Ok(new { success = true });
    }
}

