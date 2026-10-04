using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace PlantaoPro.Api.Ai;

/// <summary>Linha crua de configuração de IA (chave já cifrada).</summary>
public sealed record AiConfigRow(
    Guid TenantId,
    string TaskCode,
    bool Habilitada,
    string Provedor,
    string Modelo,
    string? FallbackProvedor,
    byte[]? ApiKeyCifrada,
    string? ChaveMascara,
    int LimiteTokensEntrada,
    int LimiteTokensSaida,
    int TimeoutS,
    int CotaMensalUsos,
    decimal? OrcamentoMensal);

public interface IAiConfigRepository
{
    Task<AiConfigRow?> ObterAsync(Guid tenantId, string taskCode, CancellationToken ct);
    Task<IReadOnlyList<AiConfigRow>> ObterTodasAsync(Guid tenantId, CancellationToken ct);
    Task UpsertAsync(AiConfigRow row, CancellationToken ct);
    Task<int> ContarUsosMesAtualAsync(Guid tenantId, string taskCode, CancellationToken ct);
    Task RegistrarUsoAsync(
        Guid tenantId, Guid? userId, string taskCode, string? contextoTipo, Guid? contextoId,
        string? provedor, string? modelo, bool sucesso, string? erroClasse,
        int? tokensEntrada, int? tokensSaida, int? duracaoMs, CancellationToken ct);
}

/// <summary>
/// Persistência da camada de IA (ai_config / ai_usos).
/// Escopo sempre por tenant_id: nenhuma consulta cruza tenant.
/// </summary>
public sealed class AiConfigRepository : IAiConfigRepository
{
    private readonly string _connectionString;

    public AiConfigRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada.");
    }

    private static readonly string SelectColunas = @"
        SELECT tenant_id AS TenantId, task_code AS TaskCode, habilitada AS Habilitada, provedor AS Provedor,
               modelo AS Modelo, fallback_provedor AS FallbackProvedor,
               api_key_cifrada AS ApiKeyCifrada, chave_mascara AS ChaveMascara,
               limite_tokens_entrada AS LimiteTokensEntrada, limite_tokens_saida AS LimiteTokensSaida,
               timeout_s AS TimeoutS, cota_mensal_usos AS CotaMensalUsos, orcamento_mensal AS OrcamentoMensal
        FROM plantaopro.ai_config";

    public async Task<AiConfigRow?> ObterAsync(Guid tenantId, string taskCode, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        return (await cn.QueryAsync<AiConfigRow>(new CommandDefinition($@"
            {SelectColunas}
            WHERE tenant_id=@TenantId AND task_code=@TaskCode",
            new { TenantId = tenantId, TaskCode = taskCode }, cancellationToken: ct)))
            .FirstOrDefault();
    }

    public async Task<IReadOnlyList<AiConfigRow>> ObterTodasAsync(Guid tenantId, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        return (await cn.QueryAsync<AiConfigRow>(new CommandDefinition($@"
            {SelectColunas}
            WHERE tenant_id=@TenantId
            ORDER BY task_code",
            new { TenantId = tenantId }, cancellationToken: ct))).ToList();
    }

    public async Task UpsertAsync(AiConfigRow row, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.ai_config (
                tenant_id, task_code, habilitada, provedor, modelo, fallback_provedor,
                api_key_cifrada, chave_mascara, limite_tokens_entrada, limite_tokens_saida,
                timeout_s, cota_mensal_usos, orcamento_mensal, updated_at)
            VALUES (
                @TenantId, @TaskCode, @Habilitada, @Provedor, @Modelo, @FallbackProvedor,
                @ApiKeyCifrada, @ChaveMascara, @LimiteTokensEntrada, @LimiteTokensSaida,
                @TimeoutS, @CotaMensalUsos, @OrcamentoMensal, now())
            ON CONFLICT (tenant_id, task_code) DO UPDATE SET
                habilitada = EXCLUDED.habilitada,
                provedor = EXCLUDED.provedor,
                modelo = EXCLUDED.modelo,
                fallback_provedor = EXCLUDED.fallback_provedor,
                api_key_cifrada = EXCLUDED.api_key_cifrada,
                chave_mascara = EXCLUDED.chave_mascara,
                limite_tokens_entrada = EXCLUDED.limite_tokens_entrada,
                limite_tokens_saida = EXCLUDED.limite_tokens_saida,
                timeout_s = EXCLUDED.timeout_s,
                cota_mensal_usos = EXCLUDED.cota_mensal_usos,
                orcamento_mensal = EXCLUDED.orcamento_mensal,
                updated_at = now()",
            row, cancellationToken: ct));
    }

    /// <summary>
    /// Contagem do mês corrente para a cota mensal: contam apenas os usos
    /// SUCESSO (falhas não consomem cota) — data truncada no fuso da sessão.
    /// </summary>
    public async Task<int> ContarUsosMesAtualAsync(Guid tenantId, string taskCode, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        return await cn.ExecuteScalarAsync<int>(new CommandDefinition(@"
            SELECT count(*)
            FROM plantaopro.ai_usos
            WHERE tenant_id=@TenantId AND task_code=@TaskCode AND status='SUCESSO'
              AND date_trunc('month', created_at) = date_trunc('month', now())",
            new { TenantId = tenantId, TaskCode = taskCode }, cancellationToken: ct));
    }

    public async Task RegistrarUsoAsync(
        Guid tenantId, Guid? userId, string taskCode, string? contextoTipo, Guid? contextoId,
        string? provedor, string? modelo, bool sucesso, string? erroClasse,
        int? tokensEntrada, int? tokensSaida, int? duracaoMs, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.ai_usos (
                tenant_id, user_id, task_code, contexto_tipo, contexto_id,
                provedor, modelo, status, erro_classe, tokens_entrada, tokens_saida, duracao_ms, created_at)
            VALUES (
                @TenantId, @UserId, @TaskCode, @ContextoTipo, @ContextoId,
                @Provedor, @Modelo, @Status, @ErroClasse, @TokensEntrada, @TokensSaida, @DuracaoMs, now())",
            new
            {
                TenantId = tenantId,
                UserId = userId,
                TaskCode = taskCode,
                ContextoTipo = contextoTipo,
                ContextoId = contextoId,
                Provedor = provedor,
                Modelo = modelo,
                Status = sucesso ? "SUCESSO" : "FALHA",
                ErroClasse = erroClasse,
                TokensEntrada = tokensEntrada,
                TokensSaida = tokensSaida,
                DuracaoMs = duracaoMs
            }, cancellationToken: ct));
    }
}
