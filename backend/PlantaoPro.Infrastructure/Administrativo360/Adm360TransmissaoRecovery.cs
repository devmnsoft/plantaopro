using Dapper;
using Npgsql;

namespace PlantaoPro.Infrastructure.Administrativo360;

/// <summary>
/// Recuperação de respostas presas em ENVIANDO por queda/interrompimento do processo.
/// A transmissão conclui marca + resultado em uma única transação; mesmo assim, um crash
/// pode deixar linhas ENVIANDO antigas. Elas são conciliadas para RESULTADO_DESCONHECIDO
/// (estado honesto: sem protocolo externo confirmado), nunca apagadas nem inventadas.
/// </summary>
public static class Adm360TransmissaoRecovery
{
    /// <summary>Concilia linhas ENVIANDO com atualizar data anterior ao limiar. Retorna o número de linhas recuperadas.</summary>
    public static async Task<int> ReconciliarEnviandoAncoradosAsync(string connectionString, int olderThanMinutes = 10, CancellationToken ct = default)
    {
        await using var cn = new NpgsqlConnection(connectionString);
        await cn.OpenAsync(ct);
        return await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_cotacao_respostas
            SET status_transmissao = 'RESULTADO_DESCONHECIDO',
                mensagem_retorno = coalesce(mensagem_retorno, '') || ' | Recuperacao oficial: transmissao ficou em ENVIANDO sem resultado registrado (processo interrompido).',
                updated_at = now()
            WHERE status_transmissao = 'ENVIANDO'
              AND updated_at <= now() - (@minutos || ' minutes')::interval",
            new { minutos = olderThanMinutes }, cancellationToken: ct));
    }
}
