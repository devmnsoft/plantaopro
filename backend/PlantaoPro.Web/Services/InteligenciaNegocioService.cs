using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Services;

public interface IInteligenciaNegocioService
{
    bool TemConflitoHorario(EscalaInteligenteRequestDto solicitacao, IEnumerable<EscalaHistoricoDto> historico);
    bool ExcedeLimiteSemanal(EscalaInteligenteRequestDto solicitacao, IEnumerable<EscalaHistoricoDto> historico, decimal limiteHorasSemana);
    IReadOnlyList<Guid> PriorizarMedicosComMenosEscalas(IEnumerable<EscalaHistoricoDto> historico, DateTime referenciaUtc);
    decimal CalcularPagamentoProporcional(decimal valorHora, DateTime inicioUtc, DateTime fimUtc);
    IEnumerable<AlertaFinanceiroViewModel> GerarAlertasPendencia(IEnumerable<(Guid MedicoId, string Medico, DateTime VencimentoUtc, decimal Valor)> pendencias, int diasLimite);
    InteligenciaDashboardViewModel ConstruirDashboard(IEnumerable<EscalaHistoricoDto> historico, IEnumerable<NotificacaoEventoDto> auditoria);
    KpiMedicoViewModel ConstruirKpiMedico(Guid medicoId, IEnumerable<EscalaHistoricoDto> historico, string especialidadePrincipal);
    IEnumerable<NotificacaoEventoDto> GerarNotificacoesEscala(string nomeMedico, string nomeHospital, DateTime inicioUtc, DateTime fimUtc, bool alteracao);
}

public class InteligenciaNegocioService : IInteligenciaNegocioService
{
    public bool TemConflitoHorario(EscalaInteligenteRequestDto solicitacao, IEnumerable<EscalaHistoricoDto> historico)
        => historico.Any(h => h.MedicoId == solicitacao.MedicoId && solicitacao.InicioUtc < h.FimUtc && solicitacao.FimUtc > h.InicioUtc);

    public bool ExcedeLimiteSemanal(EscalaInteligenteRequestDto solicitacao, IEnumerable<EscalaHistoricoDto> historico, decimal limiteHorasSemana)
    {
        var inicioSemana = solicitacao.InicioUtc.Date.AddDays(-(int)solicitacao.InicioUtc.DayOfWeek);
        var fimSemana = inicioSemana.AddDays(7);
        var horasHistorico = historico
            .Where(h => h.MedicoId == solicitacao.MedicoId && h.InicioUtc >= inicioSemana && h.FimUtc <= fimSemana)
            .Sum(h => (decimal)(h.FimUtc - h.InicioUtc).TotalHours);

        var horasSolicitadas = (decimal)(solicitacao.FimUtc - solicitacao.InicioUtc).TotalHours;
        return (horasHistorico + horasSolicitadas) > limiteHorasSemana;
    }

    public IReadOnlyList<Guid> PriorizarMedicosComMenosEscalas(IEnumerable<EscalaHistoricoDto> historico, DateTime referenciaUtc)
    {
        var seteDiasAtras = referenciaUtc.AddDays(-7);
        return historico
            .Where(h => h.InicioUtc >= seteDiasAtras)
            .GroupBy(h => h.MedicoId)
            .OrderBy(g => g.Count())
            .ThenBy(g => g.Max(x => x.FimUtc))
            .Select(g => g.Key)
            .ToList();
    }

    public decimal CalcularPagamentoProporcional(decimal valorHora, DateTime inicioUtc, DateTime fimUtc)
        => Math.Round((decimal)(fimUtc - inicioUtc).TotalHours * valorHora, 2);

    public IEnumerable<AlertaFinanceiroViewModel> GerarAlertasPendencia(IEnumerable<(Guid MedicoId, string Medico, DateTime VencimentoUtc, decimal Valor)> pendencias, int diasLimite)
    {
        var agora = DateTime.UtcNow.Date;
        return pendencias
            .Select(p => new AlertaFinanceiroViewModel(p.MedicoId, p.Medico, (agora - p.VencimentoUtc.Date).Days, p.Valor))
            .Where(a => a.DiasEmAtraso > diasLimite)
            .OrderByDescending(a => a.DiasEmAtraso)
            .ToList();
    }

    public InteligenciaDashboardViewModel ConstruirDashboard(IEnumerable<EscalaHistoricoDto> historico, IEnumerable<NotificacaoEventoDto> auditoria)
    {
        var lista = historico.ToList();
        var totalPorMedico = lista.GroupBy(x => x.MedicoId.ToString()).ToDictionary(g => g.Key, g => g.Sum(v => v.ValorPago));
        var totalPorHospital = lista.GroupBy(x => x.Hospital).ToDictionary(g => g.Key, g => g.Sum(v => v.ValorPago));
        var totalPorEspecialidade = lista.GroupBy(x => x.Especialidade).ToDictionary(g => g.Key, g => g.Sum(v => v.ValorPago));

        var executivo = new DashboardExecutivoViewModel(
            EscalasAtivas: lista.Count,
            EscalasComConflito: 0,
            TotalPagar: lista.Sum(x => x.ValorPago),
            NotificacoesPendentes: auditoria.Count(),
            AlertasFinanceiros: Array.Empty<AlertaFinanceiroViewModel>(),
            AlertasOperacionais: new[] { "Monitorar substituições e conflitos diariamente." });

        return new InteligenciaDashboardViewModel(executivo, totalPorMedico, totalPorHospital, totalPorEspecialidade, auditoria.OrderByDescending(a => a.DataHoraUtc).Take(20));
    }

    public KpiMedicoViewModel ConstruirKpiMedico(Guid medicoId, IEnumerable<EscalaHistoricoDto> historico, string especialidadePrincipal)
    {
        var semana = DateTime.UtcNow.AddDays(-7);
        var baseMedico = historico.Where(h => h.MedicoId == medicoId).ToList();
        var ultimosSete = baseMedico.Where(h => h.InicioUtc >= semana).ToList();

        var recomendacoes = historico
            .Where(h => h.Especialidade == especialidadePrincipal)
            .GroupBy(h => h.Hospital)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => $"Plantões recomendados no hospital {g.Key} ({g.Count()} ocorrências históricas)")
            .ToList();

        return new KpiMedicoViewModel(
            EscalasRealizadas: baseMedico.Count,
            HorasTrabalhadasSemana: Math.Round(ultimosSete.Sum(h => (decimal)(h.FimUtc - h.InicioUtc).TotalHours), 1),
            PagamentosReceber: baseMedico.Sum(h => h.ValorPago),
            Recomendacoes: recomendacoes);
    }

    public IEnumerable<NotificacaoEventoDto> GerarNotificacoesEscala(string nomeMedico, string nomeHospital, DateTime inicioUtc, DateTime fimUtc, bool alteracao)
    {
        var acao = alteracao ? "atualizada" : "criada";
        var mensagem = $"Escala {acao}: {nomeHospital} | {inicioUtc:dd/MM HH:mm} até {fimUtc:dd/MM HH:mm}.";
        return new[]
        {
            new NotificacaoEventoDto(nomeMedico, mensagem, DateTime.UtcNow, "InApp"),
            new NotificacaoEventoDto("coordenacao@plantaopro.com", $"Escala do médico {nomeMedico} {acao}.", DateTime.UtcNow, "Email")
        };
    }
}
