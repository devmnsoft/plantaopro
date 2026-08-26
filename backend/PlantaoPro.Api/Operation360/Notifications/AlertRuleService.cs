using Dapper;
using Npgsql;

namespace PlantaoPro.Api.Operation360.Notifications;

public interface INotificationDispatcher
{
    Task<bool> DispatchAsync(DispatchNotification notification, CancellationToken ct);
}

public sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly INotificationRepository repository;
    private readonly ILogger<NotificationDispatcher> logger;
    public NotificationDispatcher(INotificationRepository repository, ILogger<NotificationDispatcher> logger) { this.repository=repository; this.logger=logger; }
    public async Task<bool> DispatchAsync(DispatchNotification notification, CancellationToken ct)
    {
        try
        {
            var created = await repository.DispatchAsync(notification, ct);
            if (created) logger.LogInformation("Alerta {Rule} criado para {UserId} no tenant {TenantId}", notification.TipoEvento, notification.UsuarioId, notification.TenantId);
            return created;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha crítica ao despachar alerta {Rule} no tenant {TenantId}", notification.TipoEvento, notification.TenantId);
            throw;
        }
    }
}

public interface IAlertRuleService { Task<int> EvaluateAsync(Guid tenantId, CancellationToken ct); }

public sealed class AlertRuleService : IAlertRuleService
{
    private readonly string connectionString; private readonly INotificationDispatcher dispatcher; private readonly ILogger<AlertRuleService> logger;
    private static readonly IReadOnlyDictionary<string,(string Event,string Title,string Priority,string Module)> Rules =
        new Dictionary<string,(string,string,string,string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["CONVITE_PENDENTE"]=("PLANTAO_AGUARDANDO_CONFIRMACAO","Plantão aguardando confirmação","ALTA","ESCALA"),
            ["ESCALA_SEM_COBERTURA"]=("PLANTAO_SEM_PROFISSIONAL","Plantão sem profissional definido","CRITICA","ESCALA"),
            ["AGENDAMENTO_NAO_CONFIRMADO"]=("CHECKIN_PENDENTE","Check-in pendente ou atrasado","ALTA","OPERACAO"),
            ["OCORRENCIA_ABERTA"]=("OCORRENCIA_PLANTAO","Ocorrência registrada em plantão","ALTA","OPERACAO"),
            ["REPASSE_PENDENTE"]=("PAGAMENTO_PENDENTE_APROVACAO","Pagamento pendente de aprovação","MEDIA","FINANCEIRO"),
            ["CONTA_VENCIDA"]=("FECHAMENTO_FINANCEIRO_PENDENTE","Fechamento financeiro pendente","ALTA","FINANCEIRO"),
            ["ALERTA_DE_SLA"]=("RISCO_COBERTURA","Plantão com risco de cobertura","CRITICA","OPERACAO")
        };

    public AlertRuleService(IConfiguration cfg, INotificationDispatcher dispatcher, ILogger<AlertRuleService> logger) { connectionString=cfg.GetConnectionString("Default") ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada."); this.dispatcher=dispatcher; this.logger=logger; }
    public async Task<int> EvaluateAsync(Guid tenantId, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(connectionString);
        var rows = await cn.QueryAsync<RuleCandidate>(new CommandDefinition(@"select w.id,w.tipo,w.descricao,w.responsavel_id as UsuarioId
from plantaopro.work_items w where w.tenant_id=@tenantId and w.reg_status='A' and w.status not in ('CONCLUIDO','CANCELADO')
and w.responsavel_id is not null", new { tenantId }, cancellationToken: ct));
        var generated=0;
        foreach(var row in rows)
        {
            if(!Rules.TryGetValue(row.Tipo,out var rule)) continue;
            if(await dispatcher.DispatchAsync(new(tenantId,row.UsuarioId,rule.Module,rule.Event,rule.Title,row.Descricao,rule.Priority,"WORK_ITEM",row.Id,"/MinhaCentral"),ct)) generated++;
        }
        logger.LogInformation("Avaliação operacional gerou {Count} alertas no tenant {TenantId}",generated,tenantId);
        return generated;
    }
    private sealed record RuleCandidate(Guid Id,string Tipo,string Descricao,Guid UsuarioId);
}
