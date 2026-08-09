using Dapper;
using Npgsql;
using PlantaoPro.Api.Controllers;

namespace PlantaoPro.Api;

public interface IWorkflowSaude360Service
{
    Task<WorkflowResumoDto> ResumoAsync(WorkflowSaude360Filtro filtro, CancellationToken ct);
    Task<WorkflowProximaAcaoDto> ProximaAcaoAsync(WorkflowSaude360Filtro filtro, CancellationToken ct);
    Task<IReadOnlyList<WorkflowEtapaDto>> EtapasAsync(WorkflowSaude360Filtro filtro, CancellationToken ct);
    Task<IReadOnlyList<WorkflowPendenciaDto>> PendenciasAsync(WorkflowSaude360Filtro filtro, CancellationToken ct);
}

public sealed class WorkflowSaude360Service : IWorkflowSaude360Service
{
    private readonly string connectionString;
    private readonly ICurrentUserService current;

    public WorkflowSaude360Service(IConfiguration configuration, ICurrentUserService current)
    {
        connectionString = configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada.");
        this.current = current;
    }

    public async Task<WorkflowResumoDto> ResumoAsync(WorkflowSaude360Filtro filtro, CancellationToken ct)
    {
        var tenantId = current.TenantId ?? throw new UnauthorizedAccessException("Contexto do tenant não encontrado.");
        var inicio = filtro.Inicio ?? new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var fim = filtro.Fim ?? inicio.AddDays(1);
        if (fim <= inicio) throw new ArgumentException("O fim do período deve ser posterior ao início.");

        const string sql = @"
select
 count(distinct a.paciente_id)::int as TotalPacientes,
 count(*) filter(where upper(a.status)='AGENDADO')::int as Agendados,
 count(*) filter(where upper(a.status)='CONFIRMADO')::int as Confirmados,
 (select count(*)::int from plantaopro.agendamento_checkins c where c.tenant_id=@tenantId and c.reg_status='A' and c.reg_date>=@inicio and c.reg_date<@fim) as Checkins,
 count(*) filter(where upper(a.status) in ('CHECKIN_REALIZADO','AGUARDANDO_TRIAGEM'))::int as AguardandoTriagem,
 (select count(*)::int from plantaopro.triagens t where t.tenant_id=@tenantId and t.reg_status='A' and upper(t.status)='EM_TRIAGEM' and t.reg_date>=@inicio and t.reg_date<@fim) as EmTriagem,
 count(*) filter(where upper(a.status)='AGUARDANDO_CONSULTA')::int as AguardandoConsulta,
 (select count(*)::int from plantaopro.consultas c where c.tenant_id=@tenantId and c.reg_status='A' and upper(c.status)='EM_ATENDIMENTO' and c.reg_date>=@inicio and c.reg_date<@fim) as EmAtendimento,
 (select count(*)::int from plantaopro.consultas c where c.tenant_id=@tenantId and c.reg_status='A' and upper(c.status) in ('FINALIZADA','ATENDIDO') and c.reg_date>=@inicio and c.reg_date<@fim) as Finalizados,
 (select count(*)::int from plantaopro.clinica_contas_receber c where c.tenant_id=@tenantId and c.reg_status='A' and upper(c.status) in ('ABERTO','VENCIDA')) as ContasPendentes,
 (select count(*)::int from plantaopro.clinica_recebimentos r where r.tenant_id=@tenantId and r.reg_status='A' and upper(r.status)='CONFIRMADO' and r.data_recebimento>=@inicio and r.data_recebimento<@fim) as PagamentosRecebidos,
 (select count(*)::int from plantaopro.work_items w where w.tenant_id=@tenantId and w.reg_status='A' and upper(w.prioridade)='CRITICA' and w.status not in ('CONCLUIDO','CANCELADO')) as PendenciasCriticas
from plantaopro.agendamentos a
where a.tenant_id=@tenantId and a.reg_status='A' and a.data_inicio>=@inicio and a.data_inicio<@fim
 and (@unidadeId is null or a.unidade_id=@unidadeId)
 and (@profissionalId is null or a.medico_id=@profissionalId)
 and (@status is null or upper(a.status)=upper(@status));";
        await using var cn = new NpgsqlConnection(connectionString);
        var resumo = await cn.QuerySingleAsync<WorkflowResumoDto>(new CommandDefinition(sql, new { tenantId, inicio, fim, filtro.UnidadeId, filtro.ProfissionalId, filtro.Status }, cancellationToken: ct));
        resumo.ProximaAcao = (await ProximaAcaoAsync(resumo, ct)).Titulo;
        return resumo;
    }

    public async Task<WorkflowProximaAcaoDto> ProximaAcaoAsync(WorkflowSaude360Filtro filtro, CancellationToken ct) =>
        await ProximaAcaoAsync(await ResumoAsync(filtro, ct), ct);

    private static Task<WorkflowProximaAcaoDto> ProximaAcaoAsync(WorkflowResumoDto r, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var acao = r.PendenciasCriticas > 0 ? Acao("Tratar pendências críticas", "Há itens críticos vencidos ou próximos do prazo.", "/Pendencias", "COORDENACAO", "CRITICA")
            : r.Confirmados > 0 ? Acao("Realizar check-in", "Há pacientes confirmados aguardando chegada.", "/Agendamentos/CheckIn", "RECEPCAO", "ALTA")
            : r.AguardandoTriagem > 0 ? Acao("Chamar para triagem", "Há pacientes com check-in aguardando classificação.", "/Triagem/Fila", "TRIAGEM", "ALTA")
            : r.EmTriagem > 0 ? Acao("Concluir classificação", "Há triagens em andamento.", "/Triagem", "TRIAGEM", "ALTA")
            : r.AguardandoConsulta > 0 ? Acao("Iniciar atendimento", "Há consultas aguardando médico.", "/Consultas/Atendimento", "MEDICO", "ALTA")
            : r.EmAtendimento > 0 ? Acao("Concluir atendimento", "Há consultas em atendimento.", "/Consultas/Atendimento", "MEDICO", "MEDIA")
            : r.ContasPendentes > 0 ? Acao("Acompanhar recebimentos", "Há contas abertas ou vencidas.", "/ClinicaFinanceiro/ContasReceber", "FINANCEIRO", "MEDIA")
            : Acao("Operação em dia", "Não há ação operacional pendente para os filtros.", "/ClinicaDashboard", "GESTOR", "BAIXA");
        return Task.FromResult(acao);
    }

    public async Task<IReadOnlyList<WorkflowEtapaDto>> EtapasAsync(WorkflowSaude360Filtro filtro, CancellationToken ct)
    {
        var r = await ResumoAsync(filtro, ct);
        return new[]
        {
            Etapa("AGENDAMENTO", "Agendamento", r.Agendados + r.Confirmados, r.Confirmados, "/Agendamentos", "RECEPCAO", "Confirmar e fazer check-in"),
            Etapa("CHECKIN", "Check-in", r.Checkins, r.AguardandoTriagem, "/Agendamentos/CheckIn", "RECEPCAO", "Chamar paciente"),
            Etapa("TRIAGEM", "Triagem", r.AguardandoTriagem + r.EmTriagem, r.AguardandoTriagem, "/Triagem/Fila", "TRIAGEM", "Classificar risco"),
            Etapa("CONSULTA", "Consulta", r.AguardandoConsulta + r.EmAtendimento + r.Finalizados, r.AguardandoConsulta + r.EmAtendimento, "/Consultas", "MEDICO", "Concluir atendimento"),
            Etapa("FINANCEIRO", "Financeiro", r.PagamentosRecebidos + r.ContasPendentes, r.ContasPendentes, "/ClinicaFinanceiro/ContasReceber", "FINANCEIRO", "Receber ou cobrar")
        };
    }

    public async Task<IReadOnlyList<WorkflowPendenciaDto>> PendenciasAsync(WorkflowSaude360Filtro filtro, CancellationToken ct)
    {
        var tenantId = current.TenantId ?? throw new UnauthorizedAccessException("Contexto do tenant não encontrado.");
        await using var cn = new NpgsqlConnection(connectionString);
        var rows = await cn.QueryAsync<WorkflowPendenciaDto>(new CommandDefinition(@"select id as Id,titulo as Titulo,descricao as Descricao,prioridade as Prioridade,case when tipo like '%TRIAGEM%' then '/Triagem/Fila' when tipo like '%CONSULTA%' then '/Consultas' when tipo like '%CONTA%' then '/ClinicaFinanceiro/ContasReceber' else '/Pendencias' end as LinkResolucao,case when tipo like '%TRIAGEM%' then 'TRIAGEM' when tipo like '%CONSULTA%' then 'MEDICO' when tipo like '%CONTA%' then 'FINANCEIRO' else 'RECEPCAO' end as PerfilResponsavel,vence_em as Prazo from plantaopro.work_items where tenant_id=@tenantId and reg_status='A' and status not in ('CONCLUIDO','CANCELADO') and (@unidadeId is null or unidade_id=@unidadeId) order by case prioridade when 'CRITICA' then 0 when 'ALTA' then 1 else 2 end,vence_em nulls last limit 100", new { tenantId, filtro.UnidadeId }, cancellationToken: ct));
        return rows.AsList();
    }

    private static WorkflowProximaAcaoDto Acao(string titulo, string descricao, string link, string perfil, string prioridade) => new() { Titulo = titulo, Descricao = descricao, Link = link, PerfilResponsavel = perfil, Prioridade = prioridade };
    private static WorkflowEtapaDto Etapa(string codigo, string nome, int quantidade, int pendencias, string link, string perfil, string proximaAcao) => new() { Codigo = codigo, Nome = nome, Descricao = "Dados consolidados da operação no período selecionado.", Quantidade = quantidade, Pendencias = pendencias, Status = pendencias > 0 ? "ATENCAO" : "EM_DIA", Link = link, PerfilResponsavel = perfil, ProximaAcao = proximaAcao };
}
