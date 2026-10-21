using System.Threading;

namespace PlantaoPro.Application.Legislacao;

/// <summary>
/// R84 (bloco B4): item do calendario fiscal/obrigacoes legais para o periodo exibido. A
/// competencia (mes/ano de referencia) gera o vencimento conforme o catalogo oficial da fonte
/// unica; DataVencimento e a data calculada. Estado: NAO_SE_APLICA (tenant desmarcou a
/// obrigacao para o periodo), CONCLUIDA, ATRASADA, VENCE_HOJE, VENCE_EM_CURTO_PRAZO (dentro da
/// janela oficial da fonte unica) ou AGENDADA. DiasRestantes e negativo apos o vencimento.
/// </summary>
public sealed record ItemObrigacaoFiscalDto(
    string Codigo,
    string Nome,
    string Descricao,
    string Periodicidade,
    string RegimeAplicavel,
    int MesReferencia,
    int AnoReferencia,
    int DiaVencimento,
    DateOnly DataVencimento,
    bool Ativa,
    bool Concluida,
    DateTime? ConcluidaEm,
    string Estado,
    int DiasRestantes,
    string FonteOficial);

/// <summary>Calendario do periodo exibido: itens ordenados por vencimento + janela de alerta oficial.</summary>
public sealed record CalendarioFiscalDto(
    int MesExibicao,
    int AnoExibicao,
    int JanelaAlertaDias,
    IReadOnlyList<ItemObrigacaoFiscalDto> Itens);

/// <summary>
/// Comando de ativar/desativar obrigacao para o periodo exibido (mes/ano da tela); o servico
/// converte para o periodo de referencia usando o offset do catalogo.
/// </summary>
public sealed record AlterarAtivaObrigacaoCommand(string Codigo, int MesPeriodo, int AnoPeriodo, bool Ativa);

/// <summary>Comando de alternar a marca de conclusao (conclui se pendente, desmarca se concluida).</summary>
public sealed record AlternarConclusaoObrigacaoCommand(string Codigo, int MesPeriodo, int AnoPeriodo);

/// <summary>
/// R84 (bloco B4): motor de obrigacoes/reminders fiscais por tenant. Catalogo e datas oficiais
/// sao lidos da fonte unica (parametros_legais); o estado (ativa/concluida por periodo) e
/// persistido por tenant em adm_obrigacoes_fiscais.
/// </summary>
public interface ICalendarioFiscalService
{
    Task<CalendarioFiscalDto> ObterCalendarioAsync(Guid tenantId, int mesExibicao, int anoExibicao, DateOnly hoje, CancellationToken ct = default);

    Task<ItemObrigacaoFiscalDto> AlterarAtivaAsync(AlterarAtivaObrigacaoCommand comando, Guid tenantId, DateOnly hoje, CancellationToken ct = default);

    Task<ItemObrigacaoFiscalDto> AlternarConclusaoAsync(AlternarConclusaoObrigacaoCommand comando, Guid tenantId, string? usuario, DateOnly hoje, CancellationToken ct = default);
}
