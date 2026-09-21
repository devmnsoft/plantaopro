using Dapper;
using Npgsql;
using PlantaoPro.Api.Operation360.Realtime;

namespace PlantaoPro.Api.Operation360.Notifications;

public interface INotificationDispatcher
{
    Task<bool> DispatchAsync(DispatchNotification notification, CancellationToken ct);
}

public sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly INotificationRepository repository;
    private readonly IOperationRealtimePublisher realtime;
    private readonly ILogger<NotificationDispatcher> logger;

    public NotificationDispatcher(INotificationRepository repository, IOperationRealtimePublisher realtime, ILogger<NotificationDispatcher> logger)
    {
        this.repository = repository;
        this.realtime = realtime;
        this.logger = logger;
    }

    public async Task<bool> DispatchAsync(DispatchNotification notification, CancellationToken ct)
    {
        try
        {
            var created = await repository.DispatchAsync(notification, ct);
            if (created)
            {
                logger.LogInformation("Alerta {Rule} criado para {UserId} no tenant {TenantId}", notification.TipoEvento, notification.UsuarioId, notification.TenantId);
                await realtime.PublishNotificationAsync(notification.TenantId, notification.UsuarioId, "NotificationReceived", new
                {
                    notification.OrigemId,
                    notification.TipoEvento,
                    notification.Titulo,
                    notification.Mensagem,
                    notification.Prioridade,
                    notification.DestinoUrl
                }, ct);
            }
            return created;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha crítica ao despachar alerta {Rule} no tenant {TenantId}", notification.TipoEvento, notification.TenantId);
            throw;
        }
    }
}

public interface IAlertRuleService
{
    Task<int> EvaluateAsync(Guid tenantId, CancellationToken ct);
    Task<int> EvaluatePlatformAsync(Guid globalAdminUserId, CancellationToken ct);
}

public sealed class AlertRuleService : IAlertRuleService
{
    private readonly string connectionString;
    private readonly INotificationDispatcher dispatcher;
    private readonly ILogger<AlertRuleService> logger;

    private static readonly IReadOnlyDictionary<string, (string Event, string Title, string Priority, string Module)> Rules =
        new Dictionary<string, (string, string, string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["CONVITE_PENDENTE"] = ("PLANTAO_AGUARDANDO_CONFIRMACAO", "Plantão aguardando confirmação", "ALTA", "ESCALA"),
            ["ESCALA_SEM_COBERTURA"] = ("PLANTAO_SEM_PROFISSIONAL", "Plantão sem profissional definido", "CRITICA", "ESCALA"),
            ["AGENDAMENTO_NAO_CONFIRMADO"] = ("CHECKIN_PENDENTE", "Check-in pendente ou atrasado", "ALTA", "OPERACAO"),
            ["OCORRENCIA_ABERTA"] = ("OCORRENCIA_PLANTAO", "Ocorrência registrada em plantão", "ALTA", "OPERACAO"),
            ["REPASSE_PENDENTE"] = ("PAGAMENTO_PENDENTE_APROVACAO", "Pagamento pendente de aprovação", "MEDIA", "FINANCEIRO"),
            ["CONTA_VENCIDA"] = ("FECHAMENTO_FINANCEIRO_PENDENTE", "Fechamento financeiro pendente", "ALTA", "FINANCEIRO"),
            ["ALERTA_DE_SLA"] = ("RISCO_COBERTURA", "Plantão com risco de cobertura", "CRITICA", "OPERACAO")
        };

    public AlertRuleService(IConfiguration cfg, INotificationDispatcher dispatcher, ILogger<AlertRuleService> logger)
    {
        connectionString = cfg.GetConnectionString("Default") ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada.");
        this.dispatcher = dispatcher;
        this.logger = logger;
    }

    public async Task<int> EvaluateAsync(Guid tenantId, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(connectionString);
        var generated = 0;

        // 1. Work Items existentes
        var rows = await cn.QueryAsync<RuleCandidate>(new CommandDefinition(@"select w.id,w.tipo,w.descricao,w.responsavel_id as UsuarioId
from plantaopro.work_items w where w.tenant_id=@tenantId and w.reg_status='A' and w.status not in ('CONCLUIDO','CANCELADO')
and w.responsavel_id is not null", new { tenantId }, cancellationToken: ct));

        foreach (var row in rows)
        {
            if (!Rules.TryGetValue(row.Tipo, out var rule)) continue;
            if (await dispatcher.DispatchAsync(new(tenantId, row.UsuarioId, rule.Module, rule.Event, rule.Title, row.Descricao, rule.Priority, "WORK_ITEM", row.Id, "/MinhaCentral"), ct))
                generated++;
        }

        // Recuperar gestores do tenant (Admins/Coordenação/Diretoria)
        var tenantManagers = (await cn.QueryAsync<Guid>(new CommandDefinition(@"
select distinct u.id
from plantaopro.usuarios u
join plantaopro.usuario_perfis up on up.usuario_id = u.id
join plantaopro.perfis pf on pf.id = up.perfil_id
where (u.tenant_id = @tenantId or u.cliente_id = @tenantId)
  and u.reg_status = 'A'
  and pf.codigo in ('ADMINISTRADOR', 'ADMINISTRADOR_CLIENTE', 'COORDENACAO', 'DIRETOR')", new { tenantId }, cancellationToken: ct))).AsList();

        if (tenantManagers.Count == 0)
        {
            tenantManagers = (await cn.QueryAsync<Guid>(new CommandDefinition(@"select id from plantaopro.usuarios where (tenant_id=@tenantId or cliente_id=@tenantId) and reg_status='A' limit 1", new { tenantId }, cancellationToken: ct))).AsList();
        }

        // 2. Plantões com vagas em aberto nas próximas 48h (ESCALA_SEM_COBERTURA)
        var uncoveredShifts = await cn.QueryAsync<(Guid Id, string HospitalNome, DateTime DataInicio, int Vagas)>(new CommandDefinition(@"
select p.id as Id, coalesce(h.nome_fantasia, 'Sua Unidade') as HospitalNome, p.data_inicio as DataInicio, p.vagas_disponiveis as Vagas
from plantaopro.plantoes p
join plantaopro.hospitais h on h.id = p.hospital_id
where (p.tenant_id = @tenantId or p.cliente_id = @tenantId)
  and p.reg_status = 'A'
  and p.status in ('publicado', 'aberto')
  and p.vagas_disponiveis > 0
  and p.data_inicio >= now()
  and p.data_inicio <= now() + interval '48 hours'", new { tenantId }, cancellationToken: ct));

        foreach (var shift in uncoveredShifts)
        {
            foreach (var managerId in tenantManagers)
            {
                var msg = $"Plantão em {shift.HospitalNome} ({shift.DataInicio:dd/MM HH:mm}) possui {shift.Vagas} vaga(s) sem cobertura nas próximas 48h.";
                if (await dispatcher.DispatchAsync(new(tenantId, managerId, "ESCALA", "PLANTAO_SEM_PROFISSIONAL", "Plantão sem cobertura", msg, "CRITICA", "PLANTAO", shift.Id, $"/Escalas/Details/{shift.Id}"), ct))
                    generated++;
            }
        }

        // 3. Check-ins pendentes (plantão já iniciado há mais de 15m sem check-in)
        var pendingCheckins = await cn.QueryAsync<(Guid EscalaId, Guid PlantaoId, Guid UsuarioId, string HospitalNome, DateTime DataInicio)>(new CommandDefinition(@"
select e.id as EscalaId, p.id as PlantaoId, coalesce(m.usuario_id, e.created_by) as UsuarioId, coalesce(h.nome_fantasia, 'Sua Unidade') as HospitalNome, p.data_inicio as DataInicio
from plantaopro.escalas e
join plantaopro.plantoes p on p.id = e.plantao_id
join plantaopro.hospitais h on h.id = p.hospital_id
join plantaopro.medicos m on m.id = e.medico_id
where (e.tenant_id = @tenantId or e.cliente_id = @tenantId or p.tenant_id = @tenantId or p.cliente_id = @tenantId)
  and e.reg_status = 'A'
  and e.status in ('confirmada', 'agendada')
  and p.data_inicio <= now() - interval '15 minutes'
  and p.data_fim >= now()
  and not exists (
      select 1 from plantaopro.presencas pr 
      where pr.escala_id = e.id and pr.tipo = 'checkin' and pr.reg_status = 'A'
  )", new { tenantId }, cancellationToken: ct));

        foreach (var item in pendingCheckins)
        {
            if (item.UsuarioId == Guid.Empty) continue;
            var msg = $"Seu plantão em {item.HospitalNome} iniciou às {item.DataInicio:HH:mm} e o check-in ainda não foi registrado.";
            if (await dispatcher.DispatchAsync(new(tenantId, item.UsuarioId, "OPERACAO", "CHECKIN_PENDENTE", "Check-in pendente", msg, "ALTA", "ESCALA", item.EscalaId, "/MinhaAgenda/Presencas"), ct))
                generated++;
        }

        // 4. Convites de plantão pendentes (para médicos)
        var pendingInvites = await cn.QueryAsync<(Guid ConviteId, Guid PlantaoId, Guid UsuarioId, string HospitalNome, DateTime DataInicio)>(new CommandDefinition(@"
select c.id as ConviteId, p.id as PlantaoId, m.usuario_id as UsuarioId, coalesce(h.nome_fantasia, 'Sua Unidade') as HospitalNome, p.data_inicio as DataInicio
from plantaopro.plantao_convites c
join plantaopro.plantoes p on p.id = c.plantao_id
join plantaopro.hospitais h on h.id = p.hospital_id
join plantaopro.medicos m on m.id = c.medico_id
where (p.tenant_id = @tenantId or p.cliente_id = @tenantId)
  and c.reg_status = 'A'
  and c.status = 'enviado'
  and p.data_inicio >= now()
  and m.usuario_id is not null", new { tenantId }, cancellationToken: ct));

        foreach (var inv in pendingInvites)
        {
            var msg = $"Você tem um convite pendente para plantão em {inv.HospitalNome} em {inv.DataInicio:dd/MM HH:mm}.";
            if (await dispatcher.DispatchAsync(new(tenantId, inv.UsuarioId, "ESCALA", "PLANTAO_AGUARDANDO_CONFIRMACAO", "Convite de plantão pendente", msg, "ALTA", "CONVITE", inv.ConviteId, "/MinhaAgenda/Index"), ct))
                generated++;
        }

        // 5. Ocorrências abertas
        var openOccurrences = await cn.QueryAsync<(Guid Id, string Titulo, string Gravidade)>(new CommandDefinition(@"
select o.id as Id, coalesce(o.titulo, 'Ocorrência operacional') as Titulo, coalesce(o.severidade, 'ALTA') as Gravidade
from plantaopro.ocorrencias o
where (o.tenant_id = @tenantId or o.cliente_id = @tenantId)
  and o.reg_status = 'A'
  and o.status in ('aberta', 'em_analise', 'ABERTA')", new { tenantId }, cancellationToken: ct));

        foreach (var occ in openOccurrences)
        {
            foreach (var managerId in tenantManagers)
            {
                var msg = $"Ocorrência aberta: {occ.Titulo} aguardando tratativa.";
                if (await dispatcher.DispatchAsync(new(tenantId, managerId, "OPERACAO", "OCORRENCIA_PLANTAO", "Ocorrência aberta", msg, "ALTA", "OCORRENCIA", occ.Id, $"/Ocorrencias/Detalhes/{occ.Id}"), ct))
                    generated++;
            }
        }

        // 6. Conferência de plantão pendente
        var pendingConferences = await cn.QueryAsync<(Guid Id, Guid EscalaId, string HospitalNome)>(new CommandDefinition(@"
select pr.id as Id, pr.escala_id as EscalaId, coalesce(h.nome_fantasia, 'Sua Unidade') as HospitalNome
from plantaopro.presencas pr
join plantaopro.escalas e on e.id = pr.escala_id
join plantaopro.plantoes p on p.id = e.plantao_id
join plantaopro.hospitais h on h.id = p.hospital_id
where (p.tenant_id = @tenantId or p.cliente_id = @tenantId)
  and pr.reg_status = 'A'
  and pr.tipo = 'checkout'
  and coalesce(pr.status_conferencia, 'pendente') in ('pendente', 'em_revisao')", new { tenantId }, cancellationToken: ct));

        foreach (var conf in pendingConferences)
        {
            foreach (var managerId in tenantManagers)
            {
                var msg = $"Check-out realizado em {conf.HospitalNome} aguardando conferência de execução.";
                if (await dispatcher.DispatchAsync(new(tenantId, managerId, "OPERACAO", "FECHAMENTO_FINANCEIRO_PENDENTE", "Conferência de plantão pendente", msg, "MEDIA", "PRESENCA", conf.Id, "/ConferenciaExecucao/Index"), ct))
                    generated++;
            }
        }

        logger.LogInformation("Avaliação operacional gerou {Count} alertas no tenant {TenantId}", generated, tenantId);
        return generated;
    }

    public async Task<int> EvaluatePlatformAsync(Guid globalAdminUserId, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(connectionString);
        var generated = 0;
        if (globalAdminUserId == Guid.Empty) return 0;

        var issueClients = await cn.QueryAsync<(Guid Id, Guid? TenantId, string Nome, string Motivo)>(new CommandDefinition(@"
select c.id as Id, c.tenant_id as TenantId, c.nome_fantasia as Nome, 'Cliente inativo' as Motivo
from plantaopro.clientes c
where c.reg_status = 'I'
union all
select c.id as Id, c.tenant_id as TenantId, c.nome_fantasia as Nome, 'Sem módulos contratados ativos' as Motivo
from plantaopro.clientes c
where c.reg_status = 'A'
  and not exists (
      select 1 from plantaopro.tenant_modulos tm
      where (tm.tenant_id = c.tenant_id or tm.tenant_id = c.id)
        and coalesce(tm.habilitado, true) = true
  )", cancellationToken: ct));

        foreach (var item in issueClients)
        {
            var tid = item.TenantId ?? item.Id;
            var msg = $"Cliente '{item.Nome}' requer atenção de governança: {item.Motivo}.";
            if (await dispatcher.DispatchAsync(new(tid, globalAdminUserId, "SISTEMA", "PLATAFORMA_ALERTA", "Atenção requerida no cliente", msg, "ALTA", "CLIENTE", item.Id, $"/CentralAtendimento/Detalhes/{item.Id}"), ct))
                generated++;
        }

        logger.LogInformation("Avaliação de plataforma gerou {Count} alertas para admin global {AdminId}", generated, globalAdminUserId);
        return generated;
    }

    private sealed record RuleCandidate(Guid Id, string Tipo, string Descricao, Guid UsuarioId);
}
