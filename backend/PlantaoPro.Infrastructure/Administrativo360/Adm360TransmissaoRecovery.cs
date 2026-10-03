using Dapper;
using Npgsql;

namespace PlantaoPro.Infrastructure.Administrativo360;

/// <summary>
/// Recuperação de respostas presas em ENVIANDO por queda/interrompimento do processo.
/// B5: a transmissão conclui em duas transações curtas (marcação -> rede fora de transação ->
/// finalização); um crash entre elas pode deixar linhas ENVIANDO antigas e a tentativa
/// persistida presa em INICIADA. Elas são conciliadas para RESULTADO_DESCONHECIDO (estado
/// honesto: sem protocolo externo confirmado) e a tentativa é marcada INTERRUPTA —
/// nunca apagadas nem inventadas. Retorna o número de respostas recuperadas.
/// </summary>
public static class Adm360TransmissaoRecovery
{
    /// <summary>Concilia linhas ENVIANDO com atualizar data anterior ao limiar. Retorna o número de linhas recuperadas.</summary>
    public static async Task<int> ReconciliarEnviandoAncoradosAsync(string connectionString, int olderThanMinutes = 10, CancellationToken ct = default)
    {
        await using var cn = new NpgsqlConnection(connectionString);
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);
        try
        {
            var recuperadas = await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_cotacao_respostas
                SET status_transmissao = 'RESULTADO_DESCONHECIDO',
                    mensagem_retorno = coalesce(mensagem_retorno, '') || ' | Recuperacao oficial: transmissao ficou em ENVIANDO sem resultado registrado (processo interrompido).',
                    updated_at = now()
                WHERE status_transmissao = 'ENVIANDO'
                  AND updated_at <= now() - (@minutos || ' minutes')::interval",
                new { minutos = olderThanMinutes }, tx, cancellationToken: ct));

            // B5: fecha as tentativas persistidas que ficaram INICIADA por processo interrompido.
            // Só atingem tentativas cuja resposta JÁ NÃO está em ENVIANDO (ou conciliada acima
            // ou finalizada fora do padrão), então nunca tocam em uma transmissão real em curso.
            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_cotacao_envios e
                SET status_transmissao = 'INTERRUPTA',
                    mensagem = coalesce(e.mensagem, '') || ' | Recuperacao oficial: tentativa de transmissao interrompida sem registro de resultado.',
                    finalizado_em = now()
                FROM plantaopro.adm360_cotacao_respostas r
                WHERE e.resposta_id = r.id
                  AND e.tenant_id = r.tenant_id
                  AND e.status_transmissao = 'INICIADA'
                  AND r.status_transmissao <> 'ENVIANDO'",
                transaction: tx, cancellationToken: ct));

            await tx.CommitAsync(ct);
            return recuperadas;
        }
        catch
        {
            try { await tx.RollbackAsync(ct); } catch { /* rollback best-effort */ }
            throw;
        }
    }
}
