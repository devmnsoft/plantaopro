using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed class OperacaoRecomendacaoService
{
    public OperacaoInteligenteResumoDto GerarResumo(Guid? tenantId, Guid? clienteId, string? perfil)
    {
        var tenant = tenantId?.ToString("N") ?? "tenant-demo";
        var cliente = clienteId?.ToString("N") ?? "cliente-demo";
        var agora = DateTime.UtcNow;
        var pendencias = new List<OperacaoPendenciaDto>
        {
            new("Plantões sem médico", "Há vagas publicadas sem escala confirmada para as próximas 24h.", "CRITICA", "Coordenacao", "Convidar recomendados", "/Plantoes/Index", agora.AddHours(4), "ABERTO", "Plantões", tenant, cliente),
            new("Escalas aguardando confirmação", "Solicitações precisam de validação antes do início do plantão.", "ALTA", "Coordenacao", "Confirmar escalas", "/Escalas/Index", agora.AddHours(8), "PENDENTE", "Escalas", tenant, cliente),
            new("Convites sem resposta", "Convites enviados ainda não foram aceitos ou recusados.", "MEDIA", "Coordenacao", "Reenviar convite", "/CentralEscala/Index", agora.AddHours(12), "AGUARDANDO", "Convites", tenant, cliente),
            new("Conflitos de horário", "Existe sobreposição crítica em escala de médico.", "CRITICA", "Administrador Cliente", "Ver conflitos", "/OperacaoInteligente/Index", agora.AddHours(2), "RISCO", "Conflitos", tenant, cliente),
            new("Pagamentos pendentes", "Pagamentos médicos aguardam conferência financeira.", "ALTA", "Financeiro", "Conferir pagamentos", "/Financeiro/Index", agora.AddDays(1), "PENDENTE", "Financeiro", tenant, cliente),
            new("Consultas aguardando atendimento", "Pacientes já fizeram check-in e aguardam consulta.", "ALTA", "Medico", "Abrir agenda do dia", "/Agendamentos/AgendaDia", agora.AddMinutes(30), "AGUARDANDO_CONSULTA", "Saúde 360", tenant, cliente),
            new("Pacientes aguardando triagem", "Fila de triagem acima do tempo operacional esperado.", "ALTA", "Triagem", "Abrir fila", "/Triagem/Fila", agora.AddMinutes(20), "AGUARDANDO_TRIAGEM", "Saúde 360", tenant, cliente),
            new("Contas a receber vencidas", "Há recebíveis vencidos que impactam caixa do dia.", "MEDIA", "Financeiro", "Cobrar recebíveis", "/ClinicaFinanceiro/ContasReceber", agora.Date, "VENCIDO", "Financeiro Clínica", tenant, cliente),
            new("Autorizações de convênio pendentes", "Atendimentos dependem de autorização antes do faturamento.", "MEDIA", "Recepcao", "Regularizar convênio", "/Convenios/Index", agora.AddDays(1), "PENDENTE", "Convênios", tenant, cliente),
            new("Alertas de plano/limite", "Uso do plano está próximo do limite contratado.", "ALTA", "Administrador Cliente", "Revisar plano", "/Assinaturas/Uso", agora.AddDays(3), "ATENCAO", "SaaS", tenant, cliente)
        };
        var recomendacoes = new List<OperacaoRecomendacaoDto>
        {
            new("CRITICA", "Coordenacao", "Plantões", "Plantao", "demo-plantao-01", "Convide médicos recomendados para este plantão.", "Convidar recomendados", "/Plantoes/Details/demo-plantao-01", agora, tenant, cliente),
            new("ALTA", "Coordenacao", "Escalas", "Escala", "demo-escala-01", "Confirme escalas solicitadas antes de vencer.", "Confirmar escala", "/Escalas/Index", agora, tenant, cliente),
            new("CRITICA", "Administrador Cliente", "Conflitos", "Medico", "demo-medico-01", "Resolva conflito de horário do médico alocado.", "Ver conflito", "/OperacaoInteligente/Index", agora, tenant, cliente),
            new("ALTA", "Triagem", "Saúde 360", "Paciente", "demo-paciente-01", "Paciente está há muito tempo aguardando triagem.", "Iniciar triagem", "/Triagem/Fila", agora, tenant, cliente),
            new("MEDIA", "Medico", "Saúde 360", "Consulta", "demo-consulta-01", "Consulta finalizada sem prescrição, revisar se aplicável.", "Revisar consulta", "/Consultas/Index", agora, tenant, cliente),
            new("MEDIA", "Financeiro", "Financeiro Clínica", "ContaReceber", "demo-conta-01", "Conta a receber vencida há 3 dias.", "Registrar cobrança", "/ClinicaFinanceiro/ContasReceber", agora, tenant, cliente),
            new("MEDIA", "Recepcao", "Convênios", "Autorizacao", "demo-aut-01", "Convênio com autorização pendente.", "Regularizar autorização", "/Convenios/Index", agora, tenant, cliente),
            new("ALTA", "Administrador Cliente", "SaaS", "Plano", "demo-plano-01", "Plano próximo do limite de uso.", "Revisar assinatura", "/Assinaturas/Uso", agora, tenant, cliente)
        };
        var serie = new List<DashboardChartItem> { new("CRITICA", 2), new("ALTA", 5), new("MEDIA", 3), new("BAIXA", 0) };
        return new OperacaoInteligenteResumoDto(pendencias, recomendacoes, serie, "Centralize o que precisa de ação agora, com responsabilidade por perfil e links seguros.", "Comece pelas pendências críticas de escala e triagem.");
    }
}
