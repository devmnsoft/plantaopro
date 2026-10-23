using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using PlantaoPro.Api.Productivity;

namespace PlantaoPro.Api.Ai;

/// <summary>
/// R98 — jornada do assistente Drael em linguagem simples: resumo do pendente +
/// orientacao ao usuario final (dono da organizacao, nao um perfil tecnico).
/// O contexto e montado NO SERVIDOR apos autorizacao (a UI envia apenas o comando):
/// tres areas minimizadas — contrato/teste (status real da assinatura),
/// configuracao inicial (etapas pendentes do onboarding reavaliadas contra os
/// dados) e operacao (produtividade com prazo na janela, atrasadas incluidas).
/// Minimizacao: somente rotulos/estados curtos de catalogo; descricao livre,
/// evidencia e conteudo clinico NUNCA entram nas linhas. Sem pendente relevante
/// e contrato ATIVA → null (a rota responde VAZIO sem gastar chamada de provedor).
/// </summary>
public sealed class AiJornadaResumoPendente
{
    /// <summary>Janela considerada para a area de operacao: hoje até 7 dias à frente.</summary>
    public const int JanelaDiasOperacao = 7;

    /// <summary>Teto de etapas de configuração listadas no contexto.</summary>
    public const int MaxLinhasOnboarding = 12;

    /// <summary>Teto de pendências operacionais listadas no contexto.</summary>
    public const int MaxLinhasOperacao = 15;

    /// <summary>Contrato canônico de linguagem simples imposto ao modelo
    /// (travado pelo contrato R98): português simples, sem jargão/siglas,
    /// agrupado por área, ordem prática numerada, teto de palavras.</summary>
    public const string InstrucaoLinguagemSimples =
        "Explique em português simples, com frases curtas, sem jargão técnico e sem siglas, o que o dono desta organização precisa saber agora. "
        + "Use somente os itens listados, agrupados por área (contrato e teste, configuração inicial, operação diária): "
        + "primeiro diga como está o contrato ou o teste grátis e quanto tempo falta, se houver; depois diga o que ainda falta configurar; "
        + "por fim, o que precisa de atenção na operação. Em seguida, dê a ordem prática do que fazer primeiro, em passos curtos numerados. "
        + "Não invente itens que não estão listados. Máximo de 160 palavras. "
        + "Termine lembrando que cada passo é feito na tela indicada e que o assistente não aplica nada sozinho.";

    private readonly IProductivityActionService _productivity;
    private readonly OnboardingJornadaService _onboarding;
    private readonly IConfiguration _cfg;
    private readonly ICurrentUserService _current;

    public AiJornadaResumoPendente(
        IProductivityActionService productivity,
        OnboardingJornadaService onboarding,
        IConfiguration cfg,
        ICurrentUserService current)
    {
        _productivity = productivity;
        _onboarding = onboarding;
        _cfg = cfg;
        _current = current;
    }

    private sealed class AssinaturaAtivaRow
    {
        public string Status { get; set; } = string.Empty;
        public string PlanoNome { get; set; } = string.Empty;
        public string Periodicidade { get; set; } = string.Empty;
        public DateOnly? DataFim { get; set; }
        public DateOnly? DataTrialFim { get; set; }
    }

    public async Task<AiTaskExecution?> ConstruirAsync(CancellationToken ct)
    {
        var tenant = _current.TenantId ?? Guid.Empty;
        var agora = DateTimeOffset.UtcNow;

        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        await cn.OpenAsync(ct);

        // --- Area 1: contrato / teste -------------------------------------------
        var assinatura = await cn.QueryFirstOrDefaultAsync<AssinaturaAtivaRow>(new CommandDefinition(
            @"select a.status as ""Status"", coalesce(p.nome,'') as ""PlanoNome"", coalesce(a.periodicidade,'') as ""Periodicidade"",
                    a.data_fim::date as ""DataFim"", a.data_trial_fim::date as ""DataTrialFim""
             from plantaopro.assinaturas a
             join plantaopro.planos p on p.id=a.plano_id
             where a.tenant_id=@tenant and a.reg_status='A' and a.status in ('TRIAL','ATIVA','SUSPENSA','VENCIDA')
             order by case a.status when 'TRIAL' then 0 when 'ATIVA' then 1 when 'SUSPENSA' then 2 else 3 end, a.data_inicio desc
             limit 1",
            new { tenant }));
        var statusContrato = assinatura?.Status ?? "NENHUM";
        var linhas = new List<string> { LinhaContrato(assinatura) };

        // --- Area 2: configuracao inicial (onboarding reavaliado contra dados) --
        var etapaPendentes = new List<OnboardingChecklistItemDto>();
        var clienteId = await cn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
            @"select coalesce(t.cliente_id,(select t2.cliente_id from plantaopro.tenants t2 where t2.id=@tenant))
              from plantaopro.tenants t where t.id=@tenant",
            new { tenant }));
        if (clienteId.HasValue && clienteId.Value != Guid.Empty)
        {
            var quadro = await _onboarding.ReavaliarAsync(tenant, clienteId.Value);
            if (quadro.Success && quadro.Data is not null)
                etapaPendentes = quadro.Data.Where(e => e.Status == "PENDENTE" || e.Status == "BLOQUEADO").ToList();
        }
        var etapasExibidas = Math.Min(etapaPendentes.Count, MaxLinhasOnboarding);
        for (var i = 0; i < etapasExibidas; i++)
        {
            var e = etapaPendentes[i];
            var rotuloEtapa = e.Status == "BLOQUEADO" ? "Etapa aguardando etapa anterior:" : "Etapa pendente:";
            var naturezaEtapa = e.Obrigatorio ? "obrigatória" : "opcional";
            linhas.Add($"[configuracao] {rotuloEtapa} {e.Titulo} — módulo {e.ModuloNome}, {naturezaEtapa}.");
        }
        if (etapaPendentes.Count > MaxLinhasOnboarding)
            linhas.Add($"[configuracao] … e mais {etapaPendentes.Count - MaxLinhasOnboarding} etapa(s) de configuração pendente(s) não listada(s).");

        // --- Area 3: operacao (janela de 7 dias, atrasadas primeiro pela origem) -
        var page = await _productivity.ListAsync(
            new ProductivityQuery(DueTo: agora.AddDays(JanelaDiasOperacao), PageSize: 25), ct);
        var itens = page.Items.ToList();
        var itensExibidos = Math.Min(itens.Count, MaxLinhasOperacao);
        for (var i = 0; i < itensExibidos; i++)
        {
            var item = itens[i];
            var atrasada = item.DueAt is not null && item.DueAt < agora;
            var prazo = item.DueAt?.ToString("dd/MM/yyyy HH:mm") ?? "sem prazo";
            var sufixoAtraso = atrasada ? ", ATARDA" : string.Empty;
            linhas.Add($"[operacao] [{item.Priority}] {item.Module}/{item.Title} — {item.ContextLabel} "
                + $"(prazo: {prazo}{sufixoAtraso})");
        }
        if (itens.Count > MaxLinhasOperacao)
            linhas.Add($"[operacao] … e mais {itens.Count - MaxLinhasOperacao} pendência(s) operacional(is) não listada(s).");

        // Regra de VAZIO: nada pendente nas duas areas de acao e contrato ATIVA →
        // a rota responde VAZIO sem gastar chamada/cota de provedor. Contrato em
        // outro estado (teste, suspenso, vencido ou ausente) SEMPRE gera, porque a
        // orientação sobre ele é exatamente o propósito da tarefa.
        if (statusContrato == "ATIVA" && etapaPendentes.Count == 0 && itens.Count == 0) return null;

        // Escopo explícito exibido na UI junto do texto (o usuário sabe o que foi analisado).
        var sp = TimeSpan.FromHours(-3); // America/Sao_Paulo (sem horário de verão desde 2019)
        var inicio = agora.ToOffset(sp).ToString("dd/MM/yyyy HH:mm");
        var fim = agora.AddDays(JanelaDiasOperacao).ToOffset(sp).ToString("dd/MM/yyyy HH:mm");
        var escopo = $"Resumo do pendente em linguagem simples (America/Sao_Paulo); período operacional de {inicio} até {fim}; "
            + $"contrato: {statusContrato}; configuração inicial: {etapaPendentes.Count} etapa(s) pendente(s), até {MaxLinhasOnboarding} listada(s); "
            + $"operação: {itens.Count} item(ns) carregado(s) de {page.Total} no total, até {MaxLinhasOperacao} listados";

        return new AiTaskExecution(
            tenant, _current.UserId ?? Guid.Empty, "RESUMO_PENDENTE", null,
            InstrucaoLinguagemSimples, linhas,
            Escopo: escopo);
    }

    private static string LinhaContrato(AssinaturaAtivaRow? a)
    {
        if (a is null)
            return "[contrato] Nenhum contrato vinculado a esta organização no momento: é necessário escolher um plano para operar.";
        var cobranca = string.Equals(a.Periodicidade, "ANUAL", StringComparison.OrdinalIgnoreCase) ? "anual" : "mensal";
        switch (a.Status)
        {
            case "TRIAL":
                if (a.DataTrialFim.HasValue)
                {
                    var hoje = DateOnly.FromDateTime(DateTime.Today);
                    var faltam = (a.DataTrialFim.Value.ToDateTime(TimeOnly.MinValue) - hoje.ToDateTime(TimeOnly.MinValue)).Days;
                    if (faltam >= 0)
                        return $"[contrato] Teste gratuito em andamento no plano {a.PlanoNome} (depois do teste, cobrança {cobranca}): o teste termina em {a.DataTrialFim.Value:dd/MM/yyyy} (faltam {faltam} dia(s)).";
                    var janela = a.DataFim.HasValue ? $"; há uma janela até {a.DataFim.Value:dd/MM/yyyy} para regularizar" : string.Empty;
                    return $"[contrato] Teste gratuito encerrado no plano {a.PlanoNome} (cobrança {cobranca} após o teste): o teste terminou em {a.DataTrialFim.Value:dd/MM/yyyy}{janela}.";
                }
                return $"[contrato] Teste gratuito em andamento no plano {a.PlanoNome} (cobrança {cobranca} após o teste); fim do teste não informado.";
            case "ATIVA":
                return a.DataFim.HasValue
                    ? $"[contrato] Contrato ativo no plano {a.PlanoNome}, cobrança {cobranca}; próximo vencimento em {a.DataFim.Value:dd/MM/yyyy}."
                    : $"[contrato] Contrato ativo no plano {a.PlanoNome}, cobrança {cobranca}; data de vencimento não informada.";
            case "SUSPENSA":
                return $"[contrato] Contrato suspenso no plano {a.PlanoNome}: a operação pode estar limitada até a regularização.";
            case "VENCIDA":
                return a.DataFim.HasValue
                    ? $"[contrato] Contrato vencido no plano {a.PlanoNome} (vencimento registrado em {a.DataFim.Value:dd/MM/yyyy}): regularização pendente para retomar a operação plena."
                    : $"[contrato] Contrato vencido no plano {a.PlanoNome}: regularização pendente para retomar a operação plena.";
            default:
                return $"[contrato] Contrato em situação não comum ({a.Status}) no plano {a.PlanoNome}.";
        }
    }
}
