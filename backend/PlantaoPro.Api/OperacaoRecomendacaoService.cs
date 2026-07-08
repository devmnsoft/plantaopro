using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed class OperacaoRecomendacaoService
{
    private readonly IConfiguration cfg;
    private readonly ILogger<OperacaoRecomendacaoService> logger;

    public OperacaoRecomendacaoService(IConfiguration cfg, ILogger<OperacaoRecomendacaoService> logger)
    {
        this.cfg = cfg;
        this.logger = logger;
    }

    public async Task<OperacaoInteligenteResumoDto> GerarResumoAsync(Guid? tenantId, Guid? clienteId, string? perfil)
    {
        var pendencias = new List<OperacaoPendenciaDto>();
        var recomendacoes = new List<OperacaoRecomendacaoDto>();
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.OpenAsync();

        await AdicionarPendenciaAsync(cn, pendencias, recomendacoes, tenantId, clienteId, "Plantões sem médico", "Plantões publicados sem escala confirmada para as próximas 24h.", "CRITICA", "Coordenacao", "Convidar recomendados", "/Plantoes/Index", "ABERTO", "Plantões", "Plantao", "select p.id::text as EntidadeId, coalesce(h.nome,'Hospital não informado') as Contexto, p.data_inicio as Prazo from plantaopro.plantoes p left join plantaopro.hospitais h on h.id=p.hospital_id where p.reg_status='A' and (@clienteId is null or p.cliente_id=@clienteId) and p.data_inicio between now() and now()+interval '24 hours' and not exists (select 1 from plantaopro.escalas e where e.plantao_id=p.id and e.reg_status='A' and e.status in ('CONFIRMADA','ACEITA')) order by p.data_inicio limit 5");
        await AdicionarPendenciaAsync(cn, pendencias, recomendacoes, tenantId, clienteId, "Escalas aguardando confirmação", "Escalas solicitadas aguardam confirmação antes do início.", "ALTA", "Coordenacao", "Confirmar escalas", "/Escalas/Index", "PENDENTE", "Escalas", "Escala", "select e.id::text as EntidadeId, coalesce(m.nome,'Médico não informado') as Contexto, p.data_inicio as Prazo from plantaopro.escalas e left join plantaopro.medicos m on m.id=e.medico_id left join plantaopro.plantoes p on p.id=e.plantao_id where e.reg_status='A' and (@clienteId is null or e.cliente_id=@clienteId) and e.status in ('SOLICITADA','PENDENTE','AGUARDANDO_CONFIRMACAO') order by p.data_inicio nulls last limit 5");
        await AdicionarPendenciaAsync(cn, pendencias, recomendacoes, tenantId, clienteId, "Convites sem resposta", "Convites enviados ainda não foram aceitos ou recusados.", "MEDIA", "Coordenacao", "Reenviar convite", "/CentralEscala/Index", "AGUARDANDO", "Convites", "Convite", "select c.id::text as EntidadeId, coalesce(m.nome,'Médico convidado') as Contexto, c.reg_date + interval '12 hours' as Prazo from plantaopro.convites c left join plantaopro.medicos m on m.id=c.medico_id where c.reg_status='A' and (@clienteId is null or c.cliente_id=@clienteId) and c.status in ('ENVIADO','PENDENTE','AGUARDANDO') order by c.reg_date limit 5");
        await AdicionarPendenciaAsync(cn, pendencias, recomendacoes, tenantId, clienteId, "Pagamentos pendentes", "Pagamentos médicos aguardam conferência financeira.", "ALTA", "Financeiro", "Conferir pagamentos", "/Financeiro/Index", "PENDENTE", "Financeiro", "Pagamento", "select f.id::text as EntidadeId, coalesce(m.nome,'Médico') as Contexto, f.data_prevista::timestamp as Prazo from plantaopro.pagamentos f left join plantaopro.medicos m on m.id=f.medico_id where f.reg_status='A' and (@clienteId is null or f.cliente_id=@clienteId) and f.status in ('PENDENTE','EM_CONFERENCIA') order by f.data_prevista nulls first limit 5");
        await AdicionarPendenciaAsync(cn, pendencias, recomendacoes, tenantId, clienteId, "Consultas aguardando atendimento", "Pacientes com check-in aguardam atendimento médico.", "ALTA", "Medico", "Abrir agenda do dia", "/Agendamentos/AgendaDia", "AGUARDANDO_CONSULTA", "Saúde 360", "Consulta", "select a.id::text as EntidadeId, 'Agendamento ' || a.status as Contexto, a.data_inicio as Prazo from plantaopro.agendamentos a where a.reg_status='A' and (@clienteId is null or a.cliente_id=@clienteId) and a.status in ('AGUARDANDO_CONSULTA','CHECKIN_REALIZADO') order by a.data_inicio limit 5");
        await AdicionarPendenciaAsync(cn, pendencias, recomendacoes, tenantId, clienteId, "Pacientes aguardando triagem", "Fila de triagem com pacientes aguardando classificação.", "ALTA", "Triagem", "Abrir fila", "/Triagem/Fila", "AGUARDANDO_TRIAGEM", "Saúde 360", "Triagem", "select t.id::text as EntidadeId, 'Triagem pendente' as Contexto, t.reg_date as Prazo from plantaopro.triagens t where t.reg_status='A' and (@clienteId is null or t.cliente_id=@clienteId) and t.status in ('AGUARDANDO','AGUARDANDO_TRIAGEM') order by t.reg_date limit 5");
        await AdicionarPendenciaAsync(cn, pendencias, recomendacoes, tenantId, clienteId, "Contas a receber vencidas", "Recebíveis vencidos impactam o caixa do dia.", "MEDIA", "Financeiro", "Cobrar recebíveis", "/ClinicaFinanceiro/ContasReceber", "VENCIDO", "Financeiro Clínica", "ContaReceber", "select cr.id::text as EntidadeId, 'Conta vencida' as Contexto, cr.vencimento::timestamp as Prazo from plantaopro.clinica_contas_receber cr where cr.reg_status='A' and (@clienteId is null or cr.cliente_id=@clienteId) and cr.status in ('VENCIDO','ABERTO') and cr.vencimento < current_date order by cr.vencimento limit 5");
        await AdicionarPendenciaAsync(cn, pendencias, recomendacoes, tenantId, clienteId, "Autorizações de convênio pendentes", "Autorizações pendentes antes do faturamento.", "MEDIA", "Recepcao", "Regularizar convênio", "/Convenios/Autorizacoes", "PENDENTE", "Convênios", "Autorizacao", "select a.id::text as EntidadeId, 'Autorização pendente' as Contexto, a.reg_date as Prazo from plantaopro.convenio_autorizacoes a where a.reg_status='A' and (@clienteId is null or a.cliente_id=@clienteId) and a.status in ('PENDENTE','SOLICITADA') order by a.reg_date limit 5");

        if (pendencias.Count == 0)
        {
            pendencias.Add(new OperacaoPendenciaDto("Sem pendências críticas", "Nenhum registro real pendente foi encontrado para o contexto atual.", "BAIXA", perfil ?? "Operação", "Atualizar", "/OperacaoInteligente/Index", null, "OK", "Operação", tenantId?.ToString("N") ?? string.Empty, clienteId?.ToString("N") ?? string.Empty));
        }

        var serie = pendencias.GroupBy(p => p.Prioridade).Select(g => new DashboardChartItem(g.Key, g.Count())).OrderByDescending(i => i.Valor).ToList();
        return new OperacaoInteligenteResumoDto(pendencias, recomendacoes, serie, "Dados carregados da API e do PostgreSQL, respeitando tenant/cliente quando informado.", pendencias.Any(p => p.Prioridade == "CRITICA") ? "Priorize pendências críticas." : "Acompanhe a agenda e próximos plantões.");
    }

    private async Task AdicionarPendenciaAsync(NpgsqlConnection cn, IList<OperacaoPendenciaDto> pendencias, IList<OperacaoRecomendacaoDto> recomendacoes, Guid? tenantId, Guid? clienteId, string titulo, string motivo, string prioridade, string perfil, string cta, string url, string status, string modulo, string entidade, string sql)
    {
        try
        {
            var rows = (await cn.QueryAsync<OperacaoRow>(sql, new { tenantId, clienteId })).ToList();
            foreach (var row in rows)
            {
                pendencias.Add(new OperacaoPendenciaDto(titulo, string.IsNullOrWhiteSpace(row.Contexto) ? motivo : motivo + " " + row.Contexto, prioridade, perfil, cta, url, row.Prazo, status, modulo, tenantId?.ToString("N") ?? string.Empty, clienteId?.ToString("N") ?? string.Empty));
                recomendacoes.Add(new OperacaoRecomendacaoDto(prioridade, perfil, modulo, entidade, row.EntidadeId, motivo, cta, url, DateTime.UtcNow, tenantId?.ToString("N") ?? string.Empty, clienteId?.ToString("N") ?? string.Empty));
            }
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01" || ex.SqlState == "42703")
        {
            logger.LogWarning(ex, "Consulta de operação inteligente ignorada por tabela/coluna ausente. Modulo:{Modulo}", modulo);
            pendencias.Add(new OperacaoPendenciaDto(titulo, "Dados reais indisponíveis neste ambiente: estrutura de banco pendente para " + modulo + ".", "BAIXA", perfil, "Ver pendência", url, null, "BLOQUEADO_AMBIENTE", modulo, tenantId?.ToString("N") ?? string.Empty, clienteId?.ToString("N") ?? string.Empty));
        }
    }

    private sealed class OperacaoRow
    {
        public string EntidadeId { get; set; } = string.Empty;
        public string Contexto { get; set; } = string.Empty;
        public DateTime? Prazo { get; set; }
    }
}
