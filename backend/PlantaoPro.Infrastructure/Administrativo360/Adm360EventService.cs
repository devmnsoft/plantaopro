using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Npgsql;

namespace PlantaoPro.Infrastructure.Administrativo360;

/// <summary>Tipos oficiais de evento do módulo Administrativo 360 (requisito P4).</summary>
public static class Adm360TipoEvento
{
    public const string Aprovacao = "APROVACAO";
    public const string Arquivo = "ARQUIVO";
    public const string DeclaracaoManual = "DECLARACAO_MANUAL";
    public const string RetornoExterno = "RETORNO_EXTERNO";

    // A3: conferência autorizada de documento fiscal recebido (gátes do estoque).
    public const string ConfirmacaoConferencia = "CONFIRMACAO_CONFERENCIA";
}

/// <summary>
/// Registro unificado e imutável (append-only) de eventos de negócio do Administrativo 360.
/// Cada evento armazena o hash SHA-256 do conteúdo canônico; a tabela
/// <c>plantaopro.adm360_eventos</c> é protegida por trigger contra UPDATE/DELETE.
/// Quando uma conexão/transação do chamador é informada, o registro participa dela
/// (atomicidade com a operação de negócio); sem contexto, abre conexão e transação próprias.
/// Com chave de idempotência, a primeira gravação vence e as repetições não duplicam linhas.
/// </summary>
public sealed class Adm360EventService
{
    private readonly string connectionString;

    public Adm360EventService(string connectionString) => this.connectionString = connectionString;

    /// <summary>
    /// Calcula o hash canônico gravado na coluna sha256_hash.
    /// Determinístico: mesmo conteúdo (tenant, tipo, entidade, id, descrição, dados) => mesmo hash.
    /// </summary>
    public static string ComputarHash(Guid tenantId, string tipoEvento, string entidade, Guid entidadeId, string descricao, object? dados)
    {
        var dadosJson = JsonSerializer.Serialize(dados ?? new { });
        var canonica = $"adm360|{tenantId:N}|{tipoEvento}|{entidade}|{entidadeId:N}|{descricao}|{dadosJson}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonica))).ToLowerInvariant();
    }

    /// <summary>
    /// Registra o evento participando da conexão/transação do chamador.
    /// Retorna true quando a linha foi gravada; false quando a chave de idempotência já existia.
    /// </summary>
    public async Task<bool> RegistrarAsync(
        NpgsqlConnection cn,
        NpgsqlTransaction? tx,
        Guid tenantId,
        string tipoEvento,
        string entidade,
        Guid entidadeId,
        Guid? usuarioId,
        string descricao,
        object? dados,
        string? idempotencyKey = null,
        CancellationToken ct = default)
    {
        var hash = ComputarHash(tenantId, tipoEvento, entidade, entidadeId, descricao, dados);
        var id = Guid.NewGuid();
        var gravou = await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.adm360_eventos(
                id, tenant_id, tipo_evento, entidade, entidade_id, usuario_id, descricao, dados, sha256_hash, idempotency_key
            ) VALUES (
                @id, @tenantId, @tipoEvento, @entidade, @entidadeId, @usuarioId, @descricao, cast(@dados as jsonb), @hash, @idempotencyKey
            )
            ON CONFLICT (tenant_id, idempotency_key) DO NOTHING",
            new
            {
                id,
                tenantId,
                tipoEvento,
                entidade,
                entidadeId,
                usuarioId,
                descricao,
                dados = JsonSerializer.Serialize(dados ?? new { }),
                hash,
                idempotencyKey
            }, tx, cancellationToken: ct));
        return gravou > 0;
    }

    /// <summary>Registra o evento em conexão e transação próprias (quando não há contexto de negócio).</summary>
    public async Task<bool> RegistrarAsync(
        Guid tenantId,
        string tipoEvento,
        string entidade,
        Guid entidadeId,
        Guid? usuarioId,
        string descricao,
        object? dados,
        string? idempotencyKey = null,
        CancellationToken ct = default)
    {
        await using var cn = new NpgsqlConnection(connectionString);
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);
        try
        {
            var gravou = await RegistrarAsync(cn, tx, tenantId, tipoEvento, entidade, entidadeId, usuarioId, descricao, dados, idempotencyKey, ct);
            await tx.CommitAsync(ct);
            return gravou;
        }
        catch
        {
            try { await tx.RollbackAsync(ct); } catch { /* rollback best-effort */ }
            throw;
        }
    }
}
