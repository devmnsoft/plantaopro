using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Productivity;

namespace PlantaoPro.Api.Controllers;

// Rodada 10 r10b2 (B17): consolidação da fachada de compatibilidade de R8-B17.
// Meu Dia agora depende direto do serviço canônico IProductivityActionService — a mesma
// fonte única da Central de Ações. A antiga fachada de compatibilidade (interfaces e classes
// Meu Dia que viviam em MeuDiaServices.cs) não tinha fonte própria nem SQL própria
// (R8-B17 §1): era delegação pura + padrão de janela, que foi relocado para
// JanelaHoje()/JanelaAgendaClinica() abaixo.
// Caveat mantido e documentado: a janela "hoje" segue em UTC (DateTimeOffset.UtcNow.AddDays(1)) —
// mudar para o fuso do tenant/usuário é mudança de comportamento registrada como dívida (r8b17 §5).
[ApiController, Route("api/meu-dia"), Authorize]
public sealed class MeuDiaController : ControllerBase
{
    private readonly IProductivityActionService productivity;
    public MeuDiaController(IProductivityActionService productivity) => this.productivity = productivity;

    // Janela fixa do Meu Dia: vence até amanhã em UTC (mantém exatamente o comportamento R8-B17).
    private static ProductivityQuery JanelaHoje() => new(DueTo: DateTimeOffset.UtcNow.AddDays(1), PageSize: 25);
    // Agenda: módulo clínico na mesma janela UTC.
    private static ProductivityQuery JanelaAgendaClinica() => new(Module: "CLINICO", DueTo: DateTimeOffset.UtcNow.AddDays(1));

    [HttpGet] public async Task<IActionResult> Get(CancellationToken ct) => Ok(await productivity.ListAsync(JanelaHoje(), ct));
    [HttpGet("indicadores")] public async Task<IActionResult> Indicators(CancellationToken ct) => Ok(await productivity.SummaryAsync(ct));
    [HttpGet("pendencias")] public async Task<IActionResult> Actions(CancellationToken ct) => Ok((await productivity.ListAsync(JanelaHoje(), ct)).Items);
    [HttpGet("agenda")] public async Task<IActionResult> Agenda(CancellationToken ct) => Ok((await productivity.ListAsync(JanelaAgendaClinica(), ct)).Items);
    [HttpGet("alertas")] public IActionResult Alerts() => Ok(Array.Empty<object>());
    [HttpGet("acoes-rapidas")] public IActionResult QuickActions() => Ok(productivity.QuickActions());

    [HttpPost("itens/{id:guid}/concluir"), HttpPost("itens/{id:guid}/reabrir")]
    public IActionResult UnsupportedMutation(Guid id) => StatusCode(StatusCodes.Status410Gone, new
    {
        error = "A ação deve ser concluída na entidade operacional de origem."
    });
}
