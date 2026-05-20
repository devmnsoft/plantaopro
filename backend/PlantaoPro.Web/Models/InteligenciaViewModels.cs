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

public record DashboardExecutivoViewModel(
    int EscalasAtivas,
    int EscalasComConflito,
    decimal TotalPagar,
    int NotificacoesPendentes,
    IEnumerable<AlertaFinanceiroViewModel> AlertasFinanceiros,
    IEnumerable<string> AlertasOperacionais);

public record InteligenciaDashboardViewModel(
    DashboardExecutivoViewModel Executivo,
    IDictionary<string, decimal> TotalPorMedico,
    IDictionary<string, decimal> TotalPorHospital,
    IDictionary<string, decimal> TotalPorEspecialidade,
    IEnumerable<NotificacaoEventoDto> AuditoriaRecente);
