using System.Collections.Generic;

namespace PlantaoPro.Api.Ai;

/// <summary>
/// P2 IA — contratos canônicos da camada de assistentes.
/// Tarefas da entrega inicial: MEU_DIA_RESUMO e COTACAO_ANALISE
/// (novas tarefas entram no backlog após homologação).
/// </summary>
public static class AiTaskCodes
{
    public const string MeuDiaResumo = "MEU_DIA_RESUMO";
    public const string CotacaoAnalise = "COTACAO_ANALISE";

    /// <summary>Registro de auditoria apenas (sem configuração por tenant):
    /// teste de conexão do admin. Não consome a cota mensal das tarefas.</summary>
    public const string TestarConexao = "TESTAR_CONEXAO";

    public static bool TryParse(string? value, out string taskCode)
    {
        taskCode = string.Empty;
        if (value is null) return false;
        var v = value.Trim().ToUpperInvariant();
        if (v != MeuDiaResumo && v != CotacaoAnalise) return false;
        taskCode = v;
        return true;
    }
}

public static class AiProviderCodes
{
    public const string Groq = "groq";
    public const string Gemini = "gemini";
    public const string DeepSeek = "deepseek";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase) { Groq, Gemini, DeepSeek };
}

/// <summary>
/// Classes de erro canônicas devolvidas à UI. As mensagens são texto puro
/// (sem causa raiz técnica); nenhum segredo (chave, prompt) aparece nelas.
/// </summary>
public static class AiErrorKinds
{
    public const string Ok = "OK";
    public const string Vazio = "VAZIO";
    public const string NaoHabilitado = "NAO_HABILITADO";
    public const string NaoConfigurado = "NAO_CONFIGURADO";
    public const string CotaExcedida = "COTA_EXCEDIDA";
    public const string Timeout = "TIMEOUT";
    public const string RespostaInvalida = "RESPOSTA_INVALIDA";
    public const string Transporte = "TRANSPORTE";

    /// <summary>O provedor respondeu HTTP 429 (limitação de requisições do lado
    /// dele). Não afirma nada sobre autenticação nem disponibilidade da chave.</summary>
    public const string ProvedorLimitado = "PROVEDOR_LIMITADO";
    /// <summary>Muitas chamadas simultâneas no servidor (slots distribuídos esgotados).</summary>
    public const string Concorrencia = "CONCORRENCIA";
    /// <summary>Orçamento mensal da tarefa atingido antes de nova chamada.</summary>
    public const string OrcamentoExcedido = "ORCAMENTO_EXCEDIDO";
    /// <summary>Limite de testes de conexão por tenant/provedor em 24h atingido.</summary>
    public const string TesteLimite = "TESTE_LIMITE";
}

/// <summary>Requisição de geração para um adaptador de provedor.</summary>
public sealed record AiCompletionRequest(
    string Model, string SystemPrompt, string UserPrompt, int MaxTokens, double Temperature);

/// <summary>Resultado bruto interpretado a partir do wire format do provedor.</summary>
public sealed record AiCompletionResult(
    string Text, string Model, int TokensIn, int TokensOut);

/// <summary>
/// Falha classificada de um provedor. A <see cref="ErrorKind"/> usa as classes
/// canônicas (<see cref="AiErrorKinds"/>) e a mensagem é segura para auditoria
/// (nome do provedor + classe; sem corpo, chave ou URL com segredo).
/// </summary>
public sealed class AiProviderException : Exception
{
    public string ErrorKind { get; }
    public AiProviderException(string errorKind, string message) : base(message) => ErrorKind = errorKind;
    public AiProviderException(string errorKind, string message, Exception inner) : base(message, inner) => ErrorKind = errorKind;
}

/// <summary>
/// Contexto da tarefa montado NO SERVIDOR após autorização (minimização:
/// linhas já truncadas e sem conteúdo binário; a UI nunca monta o prompt).
/// </summary>
public sealed record AiTaskExecution(
    Guid TenantId, Guid UserId, string ContextoTipo, Guid? ContextoId,
    string InstrucaoTarefa, IReadOnlyList<string> ContextoLinhas,
    string? Escopo = null);

/// <summary>
/// Saída canônica para controllers/WEB: sucesso ou classe de erro + mensagem
/// amigável; quando há geração, o Texto já nasce sanitizado (HTML-escape).
/// </summary>
public sealed record AiOutcome(
    bool Success,
    string StatusKind,
    string Mensagem,
    string? Texto = null,
    string? Provedor = null,
    string? Modelo = null,
    int? TokensIn = null,
    int? TokensOut = null,
    int? DuracaoMs = null,
    bool FallbackUsado = false,
    /// <summary>Escopo explícito da análise (período/fuso/quantidade), exibido junto do texto.</summary>
    string? Escopo = null);

/// <summary>Comando de atualização da configuração de uma tarefa (admin do tenant).</summary>
public sealed record AiConfigUpdate(
    bool Habilitada,
    string Provedor,
    string Modelo,
    string? FallbackProvedor,
    string? ApiKey,
    int LimiteTokensEntrada,
    int LimiteTokensSaida,
    int TimeoutS,
    int CotaMensalUsos,
    decimal? OrcamentoMensal,
    /// <summary>Modelo próprio do fallback (válido para o provedor de fallback);
    /// nulo/vazio → o padrão vigente do próprio provedor de fallback.</summary>
    string? FallbackModelo = null,
    /// <summary>Código ISO-4217 de 3 letras (ex.: USD, BRL); nulo → USD.</summary>
    string? OrcamentoMensalMoeda = null);

/// <summary>
/// Visão de configuração exposta ao admin: a chave do tenant aparece SOMENTE
/// mascarada ("…7890"); a flag de chave global informa se o servidor tem fallback.
/// </summary>
public sealed record AiConfigView(
    string TaskCode,
    bool Habilitada,
    string Provedor,
    string Modelo,
    string? FallbackProvedor,
    bool ChaveDoTenantConfigurada,
    string? ChaveMascara,
    bool ChaveGlobalDisponivel,
    string? ModeloSugerido,
    int LimiteTokensEntrada,
    int LimiteTokensSaida,
    int TimeoutS,
    int CotaMensalUsos,
    decimal? OrcamentoMensal,
    int UsosNoMesAtual,
    string? FallbackModelo = null,
    string OrcamentoMensalMoeda = "USD",
    decimal OrcamentoUsadoMes = 0m,
    /// <summary>Aviso não bloqueante de compatibilidade de modelo (vazio = tudo certo).</summary>
    string? AvisoModelo = null);

/// <summary>Registro de auditoria com custo incerto pendente de reconciliação
/// (ex.: timeout — o provedor pode ter processado a chamada).</summary>
public sealed record AiUsoIncerto(
    Guid UsoId, Guid TenantId, string TaskCode, string? Provedor, string? Modelo,
    DateTime CriadoEmUtc, decimal? CustoEstimado, string Moeda, string? ErroClasse);

/// <summary>Comando de reconciliação de custo: valor confirmado em moeda do uso;
/// nulo → zera o custo creditado (chamada confirmada sem custo).</summary>
public sealed record AiReconciliacaoRequest(Guid UsoId, decimal? ValorConfirmado);

/// <summary>Erro de validação/configuração mapeável para HTTP 400.</summary>
public sealed class AiConfigException : Exception
{
    public AiConfigException(string message) : base(message) { }
}
