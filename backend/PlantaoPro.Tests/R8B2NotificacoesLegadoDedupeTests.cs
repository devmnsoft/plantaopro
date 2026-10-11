using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Data;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B2 — Notificações legado sem dedupe em retries (achado E3 da varredura
/// r8a0-varredura-financas-pacientes-imports-falhas-notificacoes): o caminho legado
/// (<see cref="NotificacaoService.CriarNotificacaoAsync"/> -> <c>plantaopro.notificacoes</c>)
/// inseria SEM dedupe/unique — cada chamada repetida do mesmo evento criava uma
/// notificação duplicada para o usuário.
/// Correção adota a convenção já estabelecida pelo in-app operacional (Operation360
/// NotificationService: origem_tipo/origem_id + on conflict do nothing + inserted==0):
/// chave única parcial (usuario_id, origem_tipo, origem_id, titulo) apenas para linhas
/// COM origem declarada (migration v2346); chamadas sem origem mantêm o comportamento
/// histórico. A chave inclui o titulo para preservar a sequência natural de eventos
/// distintos do MESMO registro (confirmada -> cancelada); repetir o MESMO evento do
/// mesmo registro para o mesmo usuário vira no-op (retorna false, zero linhas novas).
/// </summary>
public sealed class R8B2NotificacoesLegadoDedupeTests
{
    // Users/registros fixos e distintos por fato para rodar em paralelo sem interferir.
    private static readonly Guid UserRetry = Guid.Parse("e1b2c3d4-0000-4000-8000-0000000000b1");
    private static readonly Guid EscalaRetry = Guid.Parse("e1b2c3d4-0000-4000-8000-0000000000c1");
    private static readonly Guid UserSequencia = Guid.Parse("e1b2c3d4-0000-4000-8000-0000000000b2");
    private static readonly Guid EscalaSequencia = Guid.Parse("e1b2c3d4-0000-4000-8000-0000000000c2");
    private static readonly Guid UserMultiOrigem = Guid.Parse("e1b2c3d4-0000-4000-8000-0000000000b3");
    private static readonly Guid EscalaAlpha = Guid.Parse("e1b2c3d4-0000-4000-8000-0000000000d3");
    private static readonly Guid EscalaBeta = Guid.Parse("e1b2c3d4-0000-4000-8000-0000000000d4");
    private static readonly Guid UserSemOrigem = Guid.Parse("e1b2c3d4-0000-4000-8000-0000000000b4");

    private static async Task GarantirSchemaAsync(string cs)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        // Lock de conselheiro: serializa a garantia de schema entre classes paralelas
        // (DDL com lock exclusivo de catálogo pode gerar impasse 40P01 em corrida).
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r8b2_notificacoes_legado_dedupe'));");
        try
        {
            var temColuna = await cn.ExecuteScalarAsync<bool>(
                "select exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='notificacoes' and column_name='origem_tipo')");
            if (!temColuna)
            {
                var migrationPath = Path.Combine(RepositoryPathResolver.RepoRoot,
                    "database/migrations/2026_10_v2346_r8b2_notificacoes_legado_dedupe.sql");
                Assert.True(File.Exists(migrationPath), "Migration v2346 (dedupe notificações legado) não encontrada.");
                await cn.ExecuteAsync(await File.ReadAllTextAsync(migrationPath));
            }
        }
        finally
        {
            await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r8b2_notificacoes_legado_dedupe'));");
        }
    }

    private static NotificacaoService Servico()
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
            .Build();
        return new NotificacaoService(cfg, new AuditService(cfg, NullLogger<AuditService>.Instance), NullLogger<NotificacaoService>.Instance);
    }

    private static async Task<int> ContarAsync(NpgsqlConnection cn, NpgsqlTransaction tx, Guid user) =>
        await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.notificacoes where usuario_id=@u and reg_status='A'", new { u = user }, tx);

    [Fact]
    public async Task retry_do_mesmo_evento_para_o_mesmo_usuario_e_idempotente()
    {
        await GarantirSchemaAsync(TestDatabase.ConnectionString);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await cn.ExecuteAsync("delete from plantaopro.notificacoes where usuario_id=@u", new { u = UserRetry });
            await using var tx = await cn.BeginTransactionAsync();
            var servico = Servico();
            var primeira = await servico.CriarNotificacaoAsync(UserRetry, "Escala confirmada", "Sua escala foi confirmada", "escala", tx, "escala", EscalaRetry);
            var repetida = await servico.CriarNotificacaoAsync(UserRetry, "Escala confirmada", "Sua escala foi confirmada", "escala", tx, "escala", EscalaRetry);
            Assert.True(primeira);
            Assert.False(repetida, "repetição do mesmo evento deve ser no-op");
            Assert.Equal(1, await ContarAsync(cn, tx, UserRetry));
            await tx.CommitAsync();
        }
        finally
        {
            await cn.ExecuteAsync("delete from plantaopro.notificacoes where usuario_id=@u", new { u = UserRetry });
        }
    }

    [Fact]
    public async Task sequencia_de_eventos_distintos_do_mesmo_registro_coexistem()
    {
        await GarantirSchemaAsync(TestDatabase.ConnectionString);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await cn.ExecuteAsync("delete from plantaopro.notificacoes where usuario_id=@u", new { u = UserSequencia });
            await using var tx = await cn.BeginTransactionAsync();
            var servico = Servico();
            var confirmada = await servico.CriarNotificacaoAsync(UserSequencia, "Escala confirmada", "Sua escala foi confirmada", "escala", tx, "escala", EscalaSequencia);
            var cancelada = await servico.CriarNotificacaoAsync(UserSequencia, "Escala cancelada", "cancelamento por motivo X", "escala", tx, "escala", EscalaSequencia);
            Assert.True(confirmada);
            Assert.True(cancelada, "evento posterior distinto do mesmo registro não pode ser dedupado");
            Assert.Equal(2, await ContarAsync(cn, tx, UserSequencia));
            await tx.CommitAsync();
        }
        finally
        {
            await cn.ExecuteAsync("delete from plantaopro.notificacoes where usuario_id=@u", new { u = UserSequencia });
        }
    }

    [Fact]
    public async Task mesmo_evento_para_registros_diferentes_coexistem()
    {
        await GarantirSchemaAsync(TestDatabase.ConnectionString);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await cn.ExecuteAsync("delete from plantaopro.notificacoes where usuario_id=@u", new { u = UserMultiOrigem });
            await using var tx = await cn.BeginTransactionAsync();
            var servico = Servico();
            var alpha = await servico.CriarNotificacaoAsync(UserMultiOrigem, "Escala cancelada", "motivo A", "escala", tx, "escala", EscalaAlpha);
            var beta = await servico.CriarNotificacaoAsync(UserMultiOrigem, "Escala cancelada", "motivo B", "escala", tx, "escala", EscalaBeta);
            Assert.True(alpha);
            Assert.True(beta, "o mesmo evento em registros diferentes não pode ser dedupado");
            Assert.Equal(2, await ContarAsync(cn, tx, UserMultiOrigem));
            await tx.CommitAsync();
        }
        finally
        {
            await cn.ExecuteAsync("delete from plantaopro.notificacoes where usuario_id=@u", new { u = UserMultiOrigem });
        }
    }

    [Fact]
    public async Task chamada_sem_origem_mantem_comportamento_historico_uma_por_chamada()
    {
        await GarantirSchemaAsync(TestDatabase.ConnectionString);
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await cn.ExecuteAsync("delete from plantaopro.notificacoes where usuario_id=@u", new { u = UserSemOrigem });
            await using var tx = await cn.BeginTransactionAsync();
            var servico = Servico();
            var primeira = await servico.CriarNotificacaoAsync(UserSemOrigem, "Aviso genérico", "mensagem", "alerta", tx);
            var segunda = await servico.CriarNotificacaoAsync(UserSemOrigem, "Aviso genérico", "mensagem", "alerta", tx);
            Assert.True(primeira);
            Assert.True(segunda, "sem origem declarada não há dedupe (comportamento histórico preservado)");
            Assert.Equal(2, await ContarAsync(cn, tx, UserSemOrigem));
            await tx.CommitAsync();
        }
        finally
        {
            await cn.ExecuteAsync("delete from plantaopro.notificacoes where usuario_id=@u", new { u = UserSemOrigem });
        }
    }

    [Fact]
    public void migration_v2346_definicao_dedupe_contraido_pelo_arquivo()
    {
        var sql = File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot,
            "database/migrations/2026_10_v2346_r8b2_notificacoes_legado_dedupe.sql"));
        Assert.Contains("add column if not exists origem_tipo text", sql);
        Assert.Contains("add column if not exists origem_id uuid", sql);
        Assert.Contains("create unique index if not exists ux_notificacao_legado_origem", sql);
        Assert.Contains("where reg_status='A' and origem_id is not null", sql);
    }
}
