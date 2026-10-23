using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

/// <summary>
/// R97: conversão trial→pago sobre a assinatura existente (pendência herdada
/// da R96: alterar-plano/editar trocavam o plano e as datas, mas nunca
/// viravam o status nem limparam data_trial_fim — o cliente convertido ficava
/// bloqueado pelo gate do trial vencido mesmo pagando). Transação única com o
/// MESMO advisory lock por cliente que criação/reativação (o índice único de
/// ATIVA/TRIAL segue como árbitro final da corrida): status → ATIVA, trial_fim
/// limpa, datas e preço recalculados pela configuração do plano de destino
/// (fonte única PrecosPlano) e histórico CONVERSAO_TRIAL_PAGO materializado.
/// O trial nunca gerou fatura (gerar-mensal só cobra ATIVA); após a conversão
/// a assinatura entra no ciclo normal de cobrança com o valor contratado correto.
/// </summary>
public sealed class ConversaoTrialService
{
    private readonly IConfiguration _cfg;
    private readonly IAuditService _audit;
    private readonly ILogger<ConversaoTrialService> _logger;

    public ConversaoTrialService(IConfiguration cfg, IAuditService audit, ILogger<ConversaoTrialService> logger)
    {
        _cfg = cfg;
        _audit = audit;
        _logger = logger;
    }

    public async Task<ApiResponse<AssinaturaAtualDto>> ConverterAsync(Guid assinaturaId, Guid? novoPlanoId, string? periodicidade, string? justificativa, Guid? atorId, string? ip)
    {
        if (string.IsNullOrWhiteSpace(justificativa))
            return ApiResponse<AssinaturaAtualDto>.Fail("Justificativa obrigatória para converter o teste em plano pago.", 400);

        var periodo = string.IsNullOrWhiteSpace(periodicidade) ? null : periodicidade.Trim().ToUpperInvariant();
        if (periodo is not (null or "MENSAL" or "ANUAL"))
            return ApiResponse<AssinaturaAtualDto>.Fail("Periodicidade deve ser MENSAL ou ANUAL.", 400);

        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();

            var atual = await cn.QueryFirstOrDefaultAsync<(Guid ClienteId, Guid PlanoId, string Status, string? Periodicidade)>(
                "select cliente_id, plano_id, coalesce(status,'') as Status, periodicidade as Periodicidade from plantaopro.assinaturas where id=@id and reg_status='A' for update",
                new { id = assinaturaId }, tx);
            if (atual == default)
            {
                await tx.RollbackAsync();
                return ApiResponse<AssinaturaAtualDto>.Fail("Assinatura não encontrada.", 404);
            }
            if (!string.Equals(atual.Status, "TRIAL", StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync();
                return ApiResponse<AssinaturaAtualDto>.Fail("Apenas assinaturas em teste (TRIAL) podem ser convertidas para paga.", 400);
            }

            // B6/R97: mesmo lock por cliente da criação/reativação de assinatura;
            // duas assinaturas ativas nunca coexistem (23505 -> 409).
            await cn.ExecuteAsync("select pg_advisory_xact_lock(hashtextextended('assinaturas:' || @clienteId::text, 0))", new { clienteId = atual.ClienteId }, tx);

            var planoId = novoPlanoId ?? atual.PlanoId;
            var plano = await cn.QueryFirstOrDefaultAsync<(decimal ValorMensal, decimal ValorAnual, string? Nome)>(
                "select valor_mensal as ValorMensal, coalesce(valor_anual,0) as ValorAnual, coalesce(nome,'') as Nome from plantaopro.planos where id=@id and reg_status='A' and upper(status)='ATIVO'",
                new { id = planoId }, tx);
            // Tupla de valor: sem linha, Dapper devolve default (Nome nulo); com linha, Nome vem coalescido.
            if (plano.Nome is null)
            {
                await tx.RollbackAsync();
                return ApiResponse<AssinaturaAtualDto>.Fail("Plano escolhido não está ativo.", 400);
            }

            var periodoFinal = periodo ?? atual.Periodicidade ?? "MENSAL";
            var valor = PrecosPlano.ValorContratado(plano.ValorMensal, plano.ValorAnual, periodoFinal);
            // Calendário local (regra da R96: coluna `date` + sessão America/Sao_Paulo).
            var hoje = DateTime.Today;
            var fim = periodoFinal == "ANUAL" ? hoje.AddYears(1) : hoje.AddMonths(1);
            var novoTexto = "Conversão de teste para pago: " + justificativa.Trim();

            await cn.ExecuteAsync(@"update plantaopro.assinaturas
set plano_id=@planoId, status='ATIVA', data_inicio=@hoje, data_fim=@fim, data_trial_fim=null,
    valor_contratado=@valor, periodicidade=@periodoFinal,
    observacoes=case when observacoes is null or btrim(observacoes)='' then @novoTexto else observacoes || ' | ' || @novoTexto end,
    reg_update=now()
where id=@id", new { id = assinaturaId, planoId, hoje, fim, valor, periodoFinal, novoTexto }, tx);

            await cn.ExecuteAsync(@"insert into plantaopro.assinatura_historico(id, assinatura_id, cliente_id, plano_id_anterior, plano_id_novo, acao, justificativa, status_novo, reg_status, reg_date)
values(gen_random_uuid(), @id, @ClienteId, @PlanoAnterior, @PlanoNovo, 'CONVERSAO_TRIAL_PAGO', @Justificativa, 'ATIVA', 'A', now())",
                new { id = assinaturaId, atual.ClienteId, PlanoAnterior = atual.PlanoId, PlanoNovo = planoId, Justificativa = justificativa.Trim() }, tx);

            try
            {
                await tx.CommitAsync();
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                await tx.RollbackAsync();
                _logger.LogWarning(ex, "Corrida na conversão de trial para paga da assinatura {AssinaturaId}: cliente {ClienteId} já possui ativa/trial", assinaturaId, atual.ClienteId);
                return ApiResponse<AssinaturaAtualDto>.Fail("Conflito: o cliente já possui outra assinatura ativa ou trial.", 409);
            }

            await _audit.RegistrarAsync(atorId, atual.ClienteId, AuditoriaConstants.Entidades.Assinatura, assinaturaId, AuditoriaConstants.Acoes.Editar,
                new { acao = "CONVERSAO_TRIAL_PAGO", planoId, periodoFinal, valor, justificativa = justificativa.Trim() }, true, ip, "ADMINISTRADOR_GLOBAL");

            var dto = await cn.QueryFirstOrDefaultAsync<AssinaturaAtualDto>(@"select a.id as ""Id"", a.cliente_id as ""ClienteId"", a.plano_id as ""PlanoId"", coalesce(p.nome,'') as ""PlanoNome"",
        coalesce(a.status,'') as ""Status"", a.data_inicio::timestamp as ""DataInicio"", a.data_fim::timestamp as ""DataFim"", a.data_trial_fim::timestamp as ""DataTrialFim"",
        a.valor_contratado as ""ValorContratado"", a.dia_vencimento as ""DiaVencimento"", coalesce(a.periodicidade,'MENSAL') as ""Periodicidade""
from plantaopro.assinaturas a
join plantaopro.planos p on p.id=a.plano_id
where a.id=@id and a.reg_status='A'", new { id = assinaturaId });
            return dto is null
                ? ApiResponse<AssinaturaAtualDto>.Fail("Assinatura não encontrada.", 404)
                : ApiResponse<AssinaturaAtualDto>.Ok(dto, "Assinatura convertida para paga.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao converter trial para paga. assinatura:{AssinaturaId}", assinaturaId);
            return ApiResponse<AssinaturaAtualDto>.Fail("Não foi possível converter o teste em plano pago.", 500);
        }
    }
}
