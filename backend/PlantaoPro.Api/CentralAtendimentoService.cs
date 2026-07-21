using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public interface ICentralAtendimentoService
{
    Task<ApiResponse<CentralAtendimentoResumoDto>> ListarAsync(CentralAtendimentoFiltro filtro);
}

public sealed class CentralAtendimentoService : ICentralAtendimentoService
{
    private readonly IConfiguration cfg;
    private readonly ICurrentUserService currentUser;
    private readonly ILogger<CentralAtendimentoService> logger;
    private static readonly string[] OrdemStatus = new[] { "AGENDADO", "CONFIRMADO", "CHECKIN_REALIZADO", "AGUARDANDO_CHAMADA", "CHAMADO", "AGUARDANDO_TRIAGEM", "EM_TRIAGEM", "AGUARDANDO_CONSULTA", "EM_ATENDIMENTO", "ATENDIDO", "FALTOU", "CANCELADO" };

    public CentralAtendimentoService(IConfiguration cfg, ICurrentUserService currentUser, ILogger<CentralAtendimentoService> logger)
    {
        this.cfg = cfg;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<ApiResponse<CentralAtendimentoResumoDto>> ListarAsync(CentralAtendimentoFiltro filtro)
    {
        var tenantId = currentUser.ClienteId ?? currentUser.TenantId;
        if (!tenantId.HasValue && !currentUser.IsGlobalAdmin()) return ApiResponse<CentralAtendimentoResumoDto>.Fail("Tenant obrigatório para consultar a Central de Atendimento.", 403);
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var data = filtro.Data.HasValue ? filtro.Data.Value.ToDateTime(TimeOnly.MinValue) : DateTime.UtcNow.Date;
            var sql = @"select a.id as AgendamentoId,
coalesce(nullif(p.nome,''),'Paciente') as Paciente,
coalesce(p.nome_social,'') as NomeSocial,
a.data_inicio as Horario,
greatest(0, floor(extract(epoch from (now() - a.data_inicio))/60))::int as TempoEsperaMinutos,
coalesce(m.nome,'Profissional não definido') as Medico,
coalesce(a.especialidade, e.nome, 'Especialidade não definida') as Especialidade,
coalesce(u.nome, h.nome_fantasia, h.razao_social, 'Unidade não definida') as Unidade,
coalesce(s.nome, '') as Sala,
coalesce(c.nome, ps.nome, '') as Convenio,
coalesce(a.status,'AGENDADO') as Status,
coalesce(a.prioridade, t.classificacao_risco, 'ROTINA') as Prioridade
from plantaopro.agendamentos a
left join plantaopro.pacientes p on p.id=a.paciente_id and p.reg_status='A'
left join plantaopro.medicos m on m.id=a.medico_id and m.reg_status='A'
left join plantaopro.especialidades e on e.id=a.especialidade_id and e.reg_status='A'
left join plantaopro.hospitais h on h.id=a.unidade_id and h.reg_status='A'
left join plantaopro.clinica_unidades u on u.id=a.unidade_id and u.reg_status='A'
left join plantaopro.clinica_salas s on s.id=a.sala_id and s.reg_status='A'
left join plantaopro.convenios c on c.id=a.convenio_id and c.reg_status='A'
left join plantaopro.planos_saude ps on ps.id=a.plano_saude_id and ps.reg_status='A'
left join plantaopro.triagens t on t.agendamento_id=a.id and t.reg_status='A'
where a.reg_status='A' and (@isGlobal or a.cliente_id=@tenantId or a.tenant_id=@tenantId)
and a.data_inicio >= @inicio and a.data_inicio < @fim
and (@unidadeId is null or a.unidade_id=@unidadeId)
and (@medicoId is null or a.medico_id=@medicoId)
and (@status is null or upper(a.status)=upper(@status))
and (@prioridade is null or upper(coalesce(a.prioridade,t.classificacao_risco,''))=upper(@prioridade))
and (@especialidade is null or lower(coalesce(a.especialidade,e.nome,'')) like lower(@especialidadeLike))
and (@paciente is null or lower(coalesce(p.nome,'') || ' ' || coalesce(p.nome_social,'')) like lower(@pacienteLike))";
            var rows = (await cn.QueryAsync<CentralAtendimentoItemDto>(sql, new { tenantId, isGlobal = currentUser.IsGlobalAdmin(), inicio = data, fim = data.AddDays(1), filtro.UnidadeId, filtro.MedicoId, filtro.Status, filtro.Prioridade, filtro.Especialidade, especialidadeLike = "%" + (filtro.Especialidade ?? string.Empty).Trim() + "%", filtro.Paciente, pacienteLike = "%" + (filtro.Paciente ?? string.Empty).Trim() + "%" })).ToList();
            foreach (var item in rows) item.ProximaAcaoPermitida = ProximaAcao(item.Status);
            var grupos = OrdemStatus.Select(s => new CentralAtendimentoGrupoDto { Status = s, Titulo = Titulo(s), Itens = rows.Where(r => string.Equals(r.Status, s, StringComparison.OrdinalIgnoreCase)).OrderBy(r => r.Horario).ToArray() }).ToArray();
            var dist = rows.GroupBy(r => r.Status).ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
            var kpis = new CentralAtendimentoKpiDto { TotalAgendado = rows.Count, Presentes = rows.Count(r => r.Status != "FALTOU" && r.Status != "CANCELADO"), Faltas = rows.Count(r => r.Status == "FALTOU"), PacientesAguardando = rows.Count(r => r.Status.Contains("AGUARDANDO") || r.Status == "CHECKIN_REALIZADO"), TempoMedioEsperaMinutos = rows.Count == 0 ? 0 : Math.Round((decimal)rows.Average(r => r.TempoEsperaMinutos), 2), TriagensPendentes = rows.Count(r => r.Status == "AGUARDANDO_TRIAGEM"), ConsultasEmAndamento = rows.Count(r => r.Status == "EM_ATENDIMENTO"), AtendimentosFinalizados = rows.Count(r => r.Status == "ATENDIDO"), AtrasosAcimaLimite = rows.Count(r => r.TempoEsperaMinutos > 30 && r.Status != "ATENDIDO"), DistribuicaoPorStatus = dist };
            return ApiResponse<CentralAtendimentoResumoDto>.Ok(new CentralAtendimentoResumoDto { Kpis = kpis, Grupos = grupos }, "Central de Atendimento carregada.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao carregar Central de Atendimento");
            return ApiResponse<CentralAtendimentoResumoDto>.Fail("Não foi possível carregar a Central de Atendimento.", 500);
        }
    }

    private static string ProximaAcao(string status)
    {
        if (status == "AGENDADO") return "confirmar";
        if (status == "CONFIRMADO") return "check-in";
        if (status == "CHECKIN_REALIZADO") return "chamar";
        if (status == "CHAMADO") return "iniciar triagem";
        if (status == "EM_TRIAGEM") return "finalizar triagem";
        if (status == "AGUARDANDO_CONSULTA") return "iniciar consulta";
        if (status == "EM_ATENDIMENTO") return "finalizar consulta";
        return "visualizar";
    }

    private static string Titulo(string status) => status.Replace("_", " ").ToLowerInvariant();
}
