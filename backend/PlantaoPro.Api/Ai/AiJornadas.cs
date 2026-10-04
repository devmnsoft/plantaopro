using PlantaoPro.Api.Productivity;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Api.Ai;

/// <summary>
/// P2 IA — jornada "Resumo de pendências do Meu Dia".
/// O contexto é montado NO SERVIDOR após autorização: a UI envia apenas o comando,
/// nunca monta o prompt. Minimização de dados: linhas curtas, sem valores clínicos
/// livres, sem conteúdo binário. Sem pendências no período → null (a rota responde
/// VAZIO sem gastar chamada de provedor).
/// </summary>
public sealed class AiJornadaMeuDia
{
    private readonly IProductivityActionService _productivity;
    private readonly ICurrentUserService _current;

    public AiJornadaMeuDia(IProductivityActionService productivity, ICurrentUserService current)
    {
        _productivity = productivity;
        _current = current;
    }

    /// <summary>Período considerado pela resumo: hoje até 24h à frente.</summary>
    public const int JanelaDias = 1;

    public async Task<AiTaskExecution?> ConstruirAsync(CancellationToken ct)
    {
        var tenant = _current.TenantId ?? Guid.Empty;
        var agora = DateTimeOffset.UtcNow;
        var page = await _productivity.ListAsync(
            new ProductivityQuery(DueTo: agora.AddDays(JanelaDias), PageSize: 25), ct);

        var linhas = new List<string>(page.Items.Count);
        foreach (var item in page.Items)
        {
            var atrasada = item.DueAt is not null && item.DueAt < agora;
            var prazo = item.DueAt?.ToString("dd/MM/yyyy HH:mm") ?? "sem prazo";
            linhas.Add($"[{item.Priority}] {item.Module}/{item.Title} — {item.ContextLabel} "
                + $"(prazo: {prazo}{(atrasada ? ", ATARDA" : "")})");
        }
        if (linhas.Count == 0) return null;

        return new AiTaskExecution(
            tenant, _current.UserId ?? Guid.Empty, "PENDENCIAS_MEU_DIA", null,
            "Resuma estas pendências para o usuário: agrupe por urgência (atrasadas primeiro), "
                + "aponte os prazos mais próximos e sugira uma ordem prática de trabalho. "
                + "Responda em até 120 palavras, sem inventar itens que não estão listados.",
            linhas);
    }
}

/// <summary>
/// P2 IA — jornada "Análise de cotação" (Administrativo 360).
/// Busca a cotação SEMPRE pelo id do tenant autorizado (outro tenant → null → 404 na rota).
/// Minimização: iniciais do paciente NUNCA entram no contexto; anexos entram apenas pelo
/// contagem (conteúdo binário não é enviado ao provedor); itens limitados a 40.
/// </summary>
public sealed class AiJornadaCotacao
{
    private const int MaxItens = 40;

    private readonly ICotacoesRepository _cotacoes;
    private readonly ICurrentUserService _current;

    public AiJornadaCotacao(ICotacoesRepository cotacoes, ICurrentUserService current)
    {
        _cotacoes = cotacoes;
        _current = current;
    }

    public async Task<AiTaskExecution?> ConstruirAsync(Guid cotacaoId, CancellationToken ct)
    {
        var tenant = _current.TenantId ?? Guid.Empty;
        var cot = await _cotacoes.ObterCotacaoPorIdAsync(tenant, cotacaoId, ct);
        if (cot is null) return null; // inexistente ou de outro tenant

        var linhas = new List<string>(MaxItens + 4);
        linhas.Add($"Cotação {cot.Provedor} nº {cot.IdentificadorExterno}, revisão {cot.RevisaoExterna} | "
            + $"estabelecimento: {cot.EstabelecimentoNome} | hospital: {cot.HospitalNome} | "
            + $"procedimento: {cot.Procedimento} | data prevista: {cot.DataPrevista:dd/MM/yyyy} | "
            + $"prazo de resposta: {cot.PrazoResposta:dd/MM/yyyy HH:mm} ({cot.FusoHorario}) | "
            + $"status interno: {cot.StatusInterno} | status externo: {cot.StatusExterno} | origem: {cot.Origem}");

        var itens = cot.Itens.ToList();
        var exibidos = Math.Min(itens.Count, MaxItens);
        for (var i = 0; i < exibidos; i++)
        {
            var item = itens[i];
            linhas.Add($"item {item.NumeroItem}: {item.DescricaoExterna} ({item.FabricanteExterno} {item.ModeloExterno}) | "
                + $"quantidade solicitada: {item.QuantidadeSolicitada} {item.UnidadeSolicitada} | "
                + $"proposta unitária: {item.PrecoUnitarioOfertado:0.####} | proposta total: {item.PrecoTotalOfertado:0.####} | "
                + $"desconto: {item.Desconto:0.##}% | relação: {item.StatusRelacionamento}"
                + (item.MotivoNaoAtendimento is not null ? $" (motivo: {item.MotivoNaoAtendimento})" : ""));
        }
        if (itens.Count > MaxItens)
            linhas.Add($"… e mais {itens.Count - MaxItens} itens não listados");

        linhas.Add($"Anexos: {cot.Anexos.Count} arquivo(s) registrados (conteúdo binário NÃO enviado à IA)");

        return new AiTaskExecution(
            tenant, _current.UserId ?? Guid.Empty, "COTACAO", cot.Id,
            "Analise esta cotação recebida: aponte inconsistências entre descrição e proposta de preço, "
                + "riscos de atendimento (prazo, itens sem oferta ou com justificativa de não atendimento) e "
                + "sugira pontos práticos de negociação. Liste no máximo 5 achados, do mais importante para o menos. "
                + "Termine informando que a análise é apoio e não substitui a aprovação humana.",
            linhas);
    }
}
