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
    decimal? OrcamentoMensal,
    string? FallbackModelo = null,
    string OrcamentoMensalMoeda = "USD",
    /// <summary>YYYYMM em America/Sao_Paulo do último rollover; 0 = nunca rolou.</summary>
    int MesReferencia = 0,
    /// <summary>Contador atômico de usos SUCESSO do mês de <see cref="MesReferencia"/>.</summary>
    int UsosMesAtual = 0,
    /// <summary>Custo creditado ao orçamento no mês de <see cref="MesReferencia"/>.</summary>
    decimal OrcamentoUsadoMes = 0m);

/// <summary>Preço vigente de um modelo na data corrente (por milhão de tokens).</summary>
public sealed record AiPrecoVigente(
    string Moeda, decimal PrecoEntradaMilhao, decimal PrecoSaidaMilhao, DateTime VersaoDe);

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

    /// <summary>
    /// Reserva atômica de 1 uso da cota mensal. Antes da reserva faz o rollover
    /// preguiçoso do mês (reset dos contadores quando <paramref name="mesReferencia"/>
    /// diverge da linha) e backfill do contador a partir de ai_usos quando vazio.
    /// Retorna false quando a cota já está esgotada.
    /// </summary>
    Task<bool> ReservarUsoAsync(Guid tenantId, string taskCode, int mesReferencia, CancellationToken ct);
    /// <summary>Liberar a reserva em caso de falha/cancelamento (guarda por mês e &gt;0).</summary>
    Task LiberarUsoAsync(Guid tenantId, string taskCode, int mesReferencia, CancellationToken ct);
    /// <summary>Credita (delta positivo) ou ajusta (delta negativo) o custo do mês.</summary>
    Task RegistrarCustoMensalAsync(Guid tenantId, string taskCode, int mesReferencia, decimal delta, CancellationToken ct);

    /// <summary>Abre um slot distribuído de concorrência (multi-instância).
    /// Retorna null quando todos os slots estão ocupados.</summary>
    Task<Guid?> AbrirSlotAsync(Guid tenantId, string taskCode, int maxSlots, CancellationToken ct);
    Task FecharSlotAsync(Guid slotId, CancellationToken ct);

    /// <summary>Último preço vigente (versao_de mais recente, sem versao_ate expirada) para o modelo;
    /// null = modelo sem preço registrado (custo incerto honesto).</summary>
    Task<AiPrecoVigente?> ObterPrecoVigenteAsync(string provedor, string modelo, CancellationToken ct);

    /// <summary>Quantos testes de conexão (auditados em ai_usos) este tenant fez para o provedor nas últimas 24h.</summary>
    Task<int> ContarTestesConexao24hAsync(Guid tenantId, string provedor, CancellationToken ct);

    Task<IReadOnlyList<AiUsoIncerto>> ListarUsosIncertosAsync(CancellationToken ct);
    /// <summary>Reconcilia o custo de um uso incerto (mesmo mês corrente).
    /// <paramref name="tenantEscopo"/> = Guid.Empty libera o escopo (admin global).
    /// Retorna false se o uso não existir/for de outro escopo/já reconciliado/fora do mês.</summary>
    Task<bool> ReconciliarUsoAsync(Guid tenantEscopo, Guid usoId, decimal? valorConfirmado, int mesReferenciaAtual, CancellationToken ct);

    /// <summary>Auditoria completa com campos de custo (rodada 2 IA).</summary>
    Task RegistrarUsoComCustoAsync(
        Guid tenantId, Guid? userId, string taskCode, string? contextoTipo, Guid? contextoId,
        string? provedor, string? modelo, bool sucesso, string? erroClasse,
        int? tokensEntrada, int? tokensSaida, int? duracaoMs,
        decimal? custoEstimado, decimal? custoConfirmado, bool custoIncerto,
        string moeda, DateTime? precoVersao, CancellationToken ct);
}

/// <summary>
/// Persistência da camada de IA (ai_config / ai_usos / ai_precos_modelos /
/// ai_chamadas_ativas). Escopo sempre por tenant_id: nenhuma consulta cruza tenant.
/// Os contadores mensais vivem em ai_config (rolagem preguiçosa por
/// mes_referencia em America/Sao_Paulo) e são atualizados apenas por UPDATEs
/// condicionais — reserva e cota são atômicas mesmo multi-instância.
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
               timeout_s AS TimeoutS, cota_mensal_usos AS CotaMensalUsos, orcamento_mensal AS OrcamentoMensal,
               fallback_modelo AS FallbackModelo,
               coalesce(orcamento_mensal_moeda, 'USD') AS OrcamentoMensalMoeda,
               mes_referencia AS MesReferencia, usos_mes_atual AS UsosMesAtual,
               orcamento_usado_mes AS OrcamentoUsadoMes
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
        // Os contadores mensais (mes_referencia/usos_mes_atual/orcamento_usado_mes)
        // NÃO entram no upsert: são mantidos pelo fluxo de reserva/crédito.
        await using var cn = new NpgsqlConnection(_connectionString);
        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.ai_config (
                tenant_id, task_code, habilitada, provedor, modelo, fallback_provedor, fallback_modelo,
                api_key_cifrada, chave_mascara, limite_tokens_entrada, limite_tokens_saida,
                timeout_s, cota_mensal_usos, orcamento_mensal, orcamento_mensal_moeda, updated_at)
            VALUES (
                @TenantId, @TaskCode, @Habilitada, @Provedor, @Modelo, @FallbackProvedor, @FallbackModelo,
                @ApiKeyCifrada, @ChaveMascara, @LimiteTokensEntrada, @LimiteTokensSaida,
                @TimeoutS, @CotaMensalUsos, @OrcamentoMensal, @OrcamentoMensalMoeda, now())
            ON CONFLICT (tenant_id, task_code) DO UPDATE SET
                habilitada = EXCLUDED.habilitada,
                provedor = EXCLUDED.provedor,
                modelo = EXCLUDED.modelo,
                fallback_provedor = EXCLUDED.fallback_provedor,
                fallback_modelo = EXCLUDED.fallback_modelo,
                api_key_cifrada = EXCLUDED.api_key_cifrada,
                chave_mascara = EXCLUDED.chave_mascara,
                limite_tokens_entrada = EXCLUDED.limite_tokens_entrada,
                limite_tokens_saida = EXCLUDED.limite_tokens_saida,
                timeout_s = EXCLUDED.timeout_s,
                cota_mensal_usos = EXCLUDED.cota_mensal_usos,
                orcamento_mensal = EXCLUDED.orcamento_mensal,
                orcamento_mensal_moeda = EXCLUDED.orcamento_mensal_moeda,
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
        await RegistrarUsoComCustoAsync(tenantId, userId, taskCode, contextoTipo, contextoId,
            provedor, modelo, sucesso, erroClasse, tokensEntrada, tokensSaida, duracaoMs,
            null, null, false, "USD", null, ct);
    }

    public async Task RegistrarUsoComCustoAsync(
        Guid tenantId, Guid? userId, string taskCode, string? contextoTipo, Guid? contextoId,
        string? provedor, string? modelo, bool sucesso, string? erroClasse,
        int? tokensEntrada, int? tokensSaida, int? duracaoMs,
        decimal? custoEstimado, decimal? custoConfirmado, bool custoIncerto,
        string moeda, DateTime? precoVersao, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.ai_usos (
                tenant_id, user_id, task_code, contexto_tipo, contexto_id,
                provedor, modelo, status, erro_classe, tokens_entrada, tokens_saida, duracao_ms,
                custo_estimado, custo_confirmado, custo_incerto, moeda, preco_versao, created_at)
            VALUES (
                @TenantId, @UserId, @TaskCode, @ContextoTipo, @ContextoId,
                @Provedor, @Modelo, @Status, @ErroClasse, @TokensEntrada, @TokensSaida, @DuracaoMs,
                @CustoEstimado, @CustoConfirmado, @CustoIncerto, @Moeda, @PrecoVersao, now())",
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
                DuracaoMs = duracaoMs,
                CustoEstimado = custoEstimado,
                CustoConfirmado = custoConfirmado,
                CustoIncerto = custoIncerto,
                Moeda = moeda,
                PrecoVersao = precoVersao
            }, cancellationToken: ct));
    }

    // ------------------------------------------------------------------
    // Cota mensal atômica + orçamento (contadores em ai_config)
    // ------------------------------------------------------------------

    public async Task<bool> ReservarUsoAsync(Guid tenantId, string taskCode, int mesReferencia, CancellationToken ct)
    {
        var p = new { TenantId = tenantId, TaskCode = taskCode, Mes = mesReferencia };
        await using var cn = new NpgsqlConnection(_connectionString);
        await cn.ExecuteAsync(new CommandDefinition(@"
            -- 1) rollover preguiçoso: mês novo reseta os contadores
            UPDATE plantaopro.ai_config
            SET mes_referencia = @Mes,
                usos_mes_atual = (SELECT count(*) FROM plantaopro.ai_usos
                                   WHERE tenant_id=@TenantId AND task_code=@TaskCode AND status='SUCESSO'
                                     AND date_trunc('month', created_at) = date_trunc('month', now())),
                orcamento_usado_mes = 0,
                updated_at = now()
            WHERE tenant_id=@TenantId AND task_code=@TaskCode AND mes_referencia <> @Mes;

            -- 2) backfill p/ upgrades/legado: contador vazio mas há sucessos no mês
            UPDATE plantaopro.ai_config
            SET usos_mes_atual = (SELECT count(*) FROM plantaopro.ai_usos
                                   WHERE tenant_id=@TenantId AND task_code=@TaskCode AND status='SUCESSO'
                                     AND date_trunc('month', created_at) = date_trunc('month', now())),
                updated_at = now()
            WHERE tenant_id=@TenantId AND task_code=@TaskCode AND mes_referencia = @Mes AND usos_mes_atual = 0;",
            p, cancellationToken: ct));

        var afetados = await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.ai_config
            SET usos_mes_atual = usos_mes_atual + 1, updated_at = now()
            WHERE tenant_id=@TenantId AND task_code=@TaskCode
              AND mes_referencia = @Mes
              AND usos_mes_atual < cota_mensal_usos",
            p, cancellationToken: ct));
        return afetados == 1;
    }

    public async Task LiberarUsoAsync(Guid tenantId, string taskCode, int mesReferencia, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.ai_config
            SET usos_mes_atual = usos_mes_atual - 1, updated_at = now()
            WHERE tenant_id=@TenantId AND task_code=@TaskCode
              AND mes_referencia = @Mes AND usos_mes_atual > 0",
            new { TenantId = tenantId, TaskCode = taskCode, Mes = mesReferencia }, cancellationToken: ct));
    }

    public async Task RegistrarCustoMensalAsync(Guid tenantId, string taskCode, int mesReferencia, decimal delta, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.ai_config
            SET orcamento_usado_mes = orcamento_usado_mes + @Delta, updated_at = now()
            WHERE tenant_id=@TenantId AND task_code=@TaskCode
              AND mes_referencia = @Mes
              AND orcamento_usado_mes + @Delta >= 0",
            new { TenantId = tenantId, TaskCode = taskCode, Mes = mesReferencia, Delta = delta },
            cancellationToken: ct));
    }

    // ------------------------------------------------------------------
    // Concorrência distribuída (ai_chamadas_ativas)
    // ------------------------------------------------------------------

    public async Task<Guid?> AbrirSlotAsync(Guid tenantId, string taskCode, int maxSlots, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        var id = (Guid?)null;
        await using var rd = await cn.ExecuteReaderAsync(new CommandDefinition(@"
            -- limpeza de slots órfãos (instância caiu sem fechar)
            DELETE FROM plantaopro.ai_chamadas_ativas
            WHERE criado_em < now() - interval '6 minutes';

            INSERT INTO plantaopro.ai_chamadas_ativas (id, tenant_id, task_code)
            SELECT gen_random_uuid(), @TenantId, @TaskCode
            WHERE (SELECT count(*) FROM plantaopro.ai_chamadas_ativas) < @Max
            RETURNING id;",
            new { TenantId = tenantId, TaskCode = taskCode, Max = maxSlots }, cancellationToken: ct));
        while (await rd.ReadAsync(ct))
        {
            if (rd.FieldCount > 0)
                id = rd.GetGuid(0);
        }
        return id;
    }

    public async Task FecharSlotAsync(Guid slotId, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        await cn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM plantaopro.ai_chamadas_ativas WHERE id = @Id",
            new { Id = slotId }, cancellationToken: ct));
    }

    // ------------------------------------------------------------------
    // Preços versionados + auditoria de testes de conexão + reconciliação
    // ------------------------------------------------------------------

    public async Task<AiPrecoVigente?> ObterPrecoVigenteAsync(string provedor, string modelo, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        return (await cn.QueryAsync<AiPrecoVigente>(new CommandDefinition(@"
            SELECT moeda AS Moeda, preco_entrada_milhao AS PrecoEntradaMilhao,
                   preco_saida_milhao AS PrecoSaidaMilhao,
                   -- ::timestamp (sem fuso) para mapear em DateTime sem conversão de fuso:
                   -- o valor volta à auditoria (ai_usos.preco_versao, coluna date) sem deriva de dia.
                   versao_de::timestamp AS VersaoDe
            FROM plantaopro.ai_precos_modelos
            WHERE provedor = lower(@Provedor) AND modelo = @Modelo
              AND versao_de <= CURRENT_DATE
              AND (versao_ate IS NULL OR versao_ate >= CURRENT_DATE)
            ORDER BY versao_de DESC
            LIMIT 1",
            new { Provedor = provedor, Modelo = modelo }, cancellationToken: ct)))
            .FirstOrDefault();
    }

    public async Task<int> ContarTestesConexao24hAsync(Guid tenantId, string provedor, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        return await cn.ExecuteScalarAsync<int>(new CommandDefinition(@"
            SELECT count(*)
            FROM plantaopro.ai_usos
            WHERE tenant_id=@TenantId AND task_code='TESTAR_CONEXAO'
              AND provedor = lower(@Provedor)
              AND created_at > now() - interval '24 hours'",
            new { TenantId = tenantId, Provedor = provedor }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<AiUsoIncerto>> ListarUsosIncertosAsync(CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        return (await cn.QueryAsync<AiUsoIncerto>(new CommandDefinition(@"
            SELECT id AS UsoId, tenant_id AS TenantId, task_code AS TaskCode,
                   provedor AS Provedor, modelo AS Modelo, created_at AS CriadoEmUtc,
                   custo_estimado AS CustoEstimado, moeda AS Moeda, erro_classe AS ErroClasse
            FROM plantaopro.ai_usos
            WHERE status = 'FALHA' AND custo_incerto AND reconciliado_em IS NULL
            ORDER BY created_at DESC
            LIMIT 200", cancellationToken: ct))).ToList();
    }

    public async Task<bool> ReconciliarUsoAsync(Guid tenantEscopo, Guid usoId, decimal? valorConfirmado, int mesReferenciaAtual, CancellationToken ct)
    {
        var escopoTodos = tenantEscopo == Guid.Empty;
        await using var cn = new NpgsqlConnection(_connectionString);

        var uso = (await cn.QueryAsync<(bool Encontrado, Guid TenantId, string TaskCode, decimal? CustoEstimado)>(
            new CommandDefinition(@"
                SELECT true AS Encontrado, tenant_id AS TenantId, task_code AS TaskCode,
                       custo_estimado AS CustoEstimado
                FROM plantaopro.ai_usos
                WHERE id=@UsoId
                  AND status='FALHA' AND custo_incerto AND reconciliado_em IS NULL
                  AND date_trunc('month', created_at) = date_trunc('month', now())
                  AND (@EscopoTodos = TRUE OR tenant_id=@Escopo)",
                new { UsoId = usoId, EscopoTodos = escopoTodos, Escopo = tenantEscopo },
                cancellationToken: ct))).FirstOrDefault();
        if (!uso.Encontrado) return false;

        var atualizado = await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.ai_usos
            SET custo_confirmado = @Valor, reconciliado_em = now()
            WHERE id=@UsoId AND reconciliado_em IS NULL",
            new { UsoId = usoId, Valor = valorConfirmado }, cancellationToken: ct));
        if (atualizado != 1) return false;

        // Ajusta o crédito mensal do TENANT DO USO: o que está creditado é o
        // estimado; o correto passa a ser o valor confirmado (nulo → zero).
        var delta = (valorConfirmado ?? 0m) - (uso.CustoEstimado ?? 0m);
        if (delta != 0m)
            await RegistrarCustoMensalAsync(uso.TenantId, uso.TaskCode, mesReferenciaAtual, delta, ct);
        return true;
    }
}
