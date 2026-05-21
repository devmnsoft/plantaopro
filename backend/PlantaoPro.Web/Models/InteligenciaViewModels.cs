namespace PlantaoPro.Web.Models;

public record EscalaInteligenteRequestDto(
    Guid MedicoId,
    Guid PlantaoId,
    DateTime InicioUtc,
    DateTime FimUtc,
    string Especialidade,
    decimal ValorHora);

public record EscalaHistoricoDto(Guid MedicoId, DateTime InicioUtc, DateTime FimUtc, decimal ValorPago, string Especialidade, string Hospital);

public record NotificacaoEventoDto(string Destinatario, string Mensagem, DateTime DataHoraUtc, string Canal);

public record KpiMedicoViewModel(int EscalasRealizadas, decimal HorasTrabalhadasSemana, decimal PagamentosReceber, IEnumerable<string> Recomendacoes);

public record AlertaFinanceiroViewModel(Guid MedicoId, string Medico, int DiasEmAtraso, decimal ValorPendente);

public record DashboardFiltroViewModel(DateTime Inicio, DateTime Fim, string? Hospital, string? Especialidade, Guid? MedicoId);

public record MedicoPrioridadeViewModel(Guid MedicoId, int EscalasUltimos7Dias, decimal HorasUltimos7Dias, decimal ScorePrioridade);

public record SugestaoPlantaoViewModel(Guid MedicoId, string MedicoNome, string Hospital, string Especialidade, DateTime InicioUtc, DateTime FimUtc, decimal ScoreAderencia, string Justificativa);

public record IndicadorProdutividadeViewModel(string Chave, string Dimensao, int Escalas, decimal Horas, decimal Receita);

public record DashboardExecutivoViewModel(
    int EscalasAtivas,
    int EscalasComConflito,
    decimal TotalPagar,
    int NotificacoesPendentes,
    decimal MediaHorasSemanaPorMedico,
    int MedicosAcimaLimiteSemanal,
    IEnumerable<AlertaFinanceiroViewModel> AlertasFinanceiros,
    IEnumerable<string> AlertasOperacionais,
    IEnumerable<MedicoPrioridadeViewModel> RankingPrioridade,
    IEnumerable<SugestaoPlantaoViewModel> SugestoesPlantoes,
    IEnumerable<IndicadorProdutividadeViewModel> Produtividade);

public record InteligenciaDashboardViewModel(
    DashboardExecutivoViewModel Executivo,
    DashboardFiltroViewModel FiltroAplicado,
    IDictionary<string, decimal> TotalPorMedico,
    IDictionary<string, decimal> TotalPorHospital,
    IDictionary<string, decimal> TotalPorEspecialidade,
    IEnumerable<NotificacaoEventoDto> AuditoriaRecente);
