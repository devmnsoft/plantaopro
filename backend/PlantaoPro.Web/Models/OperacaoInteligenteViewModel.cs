namespace PlantaoPro.Web.Models;

public sealed class OperacaoInteligenteViewModel
{
    public string Titulo { get; set; } = "Operação Inteligente";
    public string Subtitulo { get; set; } = "Central do que precisa ser resolvido agora por perfil, módulo e prioridade.";
    public IList<OperacaoPendenciaViewModel> Pendencias { get; set; } = new List<OperacaoPendenciaViewModel>();
    public IList<OperacaoRecomendacaoViewModel> Recomendacoes { get; set; } = new List<OperacaoRecomendacaoViewModel>();

    public static OperacaoInteligenteViewModel Empty() => new OperacaoInteligenteViewModel();

    public static OperacaoInteligenteViewModel Demo()
    {
        var model = new OperacaoInteligenteViewModel();
        model.Pendencias.Add(new OperacaoPendenciaViewModel("Plantões sem médico", "Vagas críticas nas próximas 24h", "CRITICA", "Coordenação", "Convidar recomendados", "/Plantoes/Index", "ABERTO"));
        model.Pendencias.Add(new OperacaoPendenciaViewModel("Escalas aguardando confirmação", "Solicitações vencem hoje", "ALTA", "Coordenação", "Confirmar escalas", "/Escalas/Index", "PENDENTE"));
        model.Pendencias.Add(new OperacaoPendenciaViewModel("Convites sem resposta", "Médicos ainda não responderam", "MEDIA", "Coordenação", "Reenviar convite", "/CentralEscala/Index", "AGUARDANDO"));
        model.Pendencias.Add(new OperacaoPendenciaViewModel("Conflitos de horário", "Sobreposição em escala médica", "CRITICA", "Admin Cliente", "Ver conflitos", "/OperacaoInteligente/Index", "RISCO"));
        model.Pendencias.Add(new OperacaoPendenciaViewModel("Pagamentos pendentes", "Conferência financeira necessária", "ALTA", "Financeiro", "Conferir", "/Financeiro/Index", "PENDENTE"));
        model.Pendencias.Add(new OperacaoPendenciaViewModel("Consultas aguardando atendimento", "Pacientes em sala de espera", "ALTA", "Médico", "Abrir agenda", "/Agendamentos/AgendaDia", "AGUARDANDO_CONSULTA"));
        model.Pendencias.Add(new OperacaoPendenciaViewModel("Pacientes aguardando triagem", "Fila acima do tempo esperado", "ALTA", "Triagem", "Abrir fila", "/Triagem/Fila", "AGUARDANDO_TRIAGEM"));
        model.Pendencias.Add(new OperacaoPendenciaViewModel("Contas a receber vencidas", "Recebíveis impactam caixa", "MEDIA", "Financeiro", "Cobrar", "/ClinicaFinanceiro/ContasReceber", "VENCIDO"));
        model.Pendencias.Add(new OperacaoPendenciaViewModel("Autorizações de convênio pendentes", "Regularização antes do faturamento", "MEDIA", "Recepção", "Regularizar", "/Convenios/Index", "PENDENTE"));
        model.Pendencias.Add(new OperacaoPendenciaViewModel("Alertas de plano/limite", "Uso próximo ao contratado", "ALTA", "Admin Cliente", "Revisar plano", "/Assinaturas/Uso", "ATENCAO"));
        model.Recomendacoes.Add(new OperacaoRecomendacaoViewModel("CRITICA", "Coordenação", "Plantões", "Convide médicos recomendados para este plantão.", "Convidar recomendados", "/Plantoes/Index"));
        model.Recomendacoes.Add(new OperacaoRecomendacaoViewModel("ALTA", "Triagem", "Saúde 360", "Paciente está há muito tempo aguardando triagem.", "Iniciar triagem", "/Triagem/Fila"));
        model.Recomendacoes.Add(new OperacaoRecomendacaoViewModel("MEDIA", "Financeiro", "Financeiro Clínica", "Conta a receber vencida há 3 dias.", "Registrar cobrança", "/ClinicaFinanceiro/ContasReceber"));
        return model;
    }
}

public record OperacaoPendenciaViewModel(string Titulo,string Motivo,string Prioridade,string PerfilResponsavel,string Cta,string UrlOrigem,string Status);
public record OperacaoRecomendacaoViewModel(string Severidade,string PerfilAlvo,string Modulo,string Mensagem,string AcaoSugerida,string UrlAcao);

public sealed class OperacaoInteligenteResumoApiDto
{
    public IEnumerable<OperacaoPendenciaApiDto> Pendencias { get; set; } = Array.Empty<OperacaoPendenciaApiDto>();
    public IEnumerable<OperacaoRecomendacaoApiDto> Recomendacoes { get; set; } = Array.Empty<OperacaoRecomendacaoApiDto>();
    public string MensagemGuia { get; set; } = string.Empty;
    public string ProximoPasso { get; set; } = string.Empty;
}

public sealed class OperacaoPendenciaApiDto
{
    public string Titulo { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
    public string PerfilResponsavel { get; set; } = string.Empty;
    public string Cta { get; set; } = string.Empty;
    public string UrlOrigem { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class OperacaoRecomendacaoApiDto
{
    public string Severidade { get; set; } = string.Empty;
    public string PerfilAlvo { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string AcaoSugerida { get; set; } = string.Empty;
    public string UrlAcao { get; set; } = string.Empty;
}
