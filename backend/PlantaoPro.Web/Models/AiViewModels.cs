namespace PlantaoPro.Web.Models;

/// <summary>
/// P2 IA — resultado canônico para a UI. O Texto já nasce sanitizado
/// (HTML-escape) no servidor; a renderização usa apenas textContent.
/// Nenhum segredo (chave, prompt) aparece nas mensagens.
/// </summary>
public sealed class AiOutcomeViewModel
{
    public bool Success { get; set; }
    public string StatusKind { get; set; } = string.Empty;
    public string? Mensagem { get; set; }
    public string? Texto { get; set; }
    public string? Provedor { get; set; }
    public string? Modelo { get; set; }
    public int? TokensIn { get; set; }
    public int? TokensOut { get; set; }
    public int? DuracaoMs { get; set; }
    public bool FallbackUsado { get; set; }
}

/// <summary>
/// P2 IA — visão de configuração de uma tarefa. A chave do provedor do
/// tenant aparece SOMENTE mascarada; a flag de chave global informa se o
/// servidor tem fallback disponível.
/// </summary>
public sealed class AiConfiguracaoViewModel
{
    public string TaskCode { get; set; } = string.Empty;
    public bool Habilitada { get; set; }
    public string Provedor { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string? FallbackProvedor { get; set; }
    public bool ChaveDoTenantConfigurada { get; set; }
    public string? ChaveMascara { get; set; }
    public bool ChaveGlobalDisponivel { get; set; }
    public string? ModeloSugerido { get; set; }
    public int LimiteTokensEntrada { get; set; }
    public int LimiteTokensSaida { get; set; }
    public int TimeoutS { get; set; }
    public int CotaMensalUsos { get; set; }
    public decimal? OrcamentoMensal { get; set; }
    public int UsosNoMesAtual { get; set; }
}

/// <summary>P2 IA — página de configuração do assistente (admin do tenant).</summary>
public sealed class AiConfigPageViewModel
{
    public IReadOnlyList<AiConfiguracaoViewModel> Configuracoes { get; set; } = Array.Empty<AiConfiguracaoViewModel>();
    public bool ChaveMestraDoServidorConfigurada { get; set; }
    public string? Erro { get; set; }
}

/// <summary>
/// P2 IA — formulário de salvamento por tarefa. ApiKey vazia preserva a
/// chave atual (o gateway só cifra quando um novo valor chega).
/// </summary>
public sealed class AiConfigFormModel
{
    public bool Habilitada { get; set; }
    public string Provedor { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string? FallbackProvedor { get; set; }
    public string? ApiKey { get; set; }
    public int LimiteTokensEntrada { get; set; }
    public int LimiteTokensSaida { get; set; }
    public int TimeoutS { get; set; }
    public int CotaMensalUsos { get; set; }
    public decimal? OrcamentoMensal { get; set; }
}
