using System.Data;
using Dapper;
using Npgsql;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value)
    {
        if (value is DateTime dt) return DateOnly.FromDateTime(dt);
        if (value is DateOnly d) return d;
        return DateOnly.Parse(value.ToString()!);
    }
}

public abstract class Adm360Repository
{
    static Adm360Repository()
    {
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
    }

    protected static DateOnly ToDateOnly(object? value)
    {
        if (value is null) return default;
        if (value is DateOnly d) return d;
        if (value is DateTime dt) return DateOnly.FromDateTime(dt);
        return DateOnly.Parse(value.ToString()!);
    }

    private readonly string connectionString;
    protected Adm360Repository(string connectionString) => this.connectionString = connectionString;
    protected NpgsqlConnection Connection() => new(connectionString);

    protected static async Task ValidarBloqueioInventarioAsync(NpgsqlConnection cn, NpgsqlTransaction tx, Guid tenantId, Guid localId, CancellationToken ct)
    {
        var emInventario = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(
            "select exists(select 1 from plantaopro.adm360_inventarios where tenant_id=@tenantId and local_id=@localId and situacao in ('ABERTO','CONTAGEM','REVISAO'))",
            new { tenantId, localId },
            tx,
            cancellationToken: ct));
        if (emInventario)
            throw new InvalidOperationException("Movimentação bloqueada: o local está com inventário ativo em contagem ou revisão.");
    }

    protected static async Task BloquearChavesDeterministasAsync(NpgsqlConnection cn, NpgsqlTransaction tx, IEnumerable<string> lockKeys, CancellationToken ct)
    {
        var sorted = lockKeys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().OrderBy(k => k, StringComparer.Ordinal);
        foreach (var lockKey in sorted)
        {
            await cn.ExecuteAsync(new CommandDefinition(
                "select pg_advisory_xact_lock(hashtextextended(@lockKey, 0))",
                new { lockKey },
                tx,
                cancellationToken: ct));
        }
    }

    protected async Task ExecutarComRetrySerializableAsync(Func<NpgsqlConnection, NpgsqlTransaction, Task> acao, CancellationToken ct, int maxTentativas = 4)
    {
        for (var tentativa = 1; ; tentativa++)
        {
            await using var cn = Connection();
            await cn.OpenAsync(ct);
            await using var tx = await cn.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            try
            {
                await acao(cn, tx);
                await tx.CommitAsync(ct);
                return;
            }
            catch (NpgsqlException ex) when ((ex.SqlState is "40001" or "40P01") && tentativa < maxTentativas)
            {
                try { await tx.RollbackAsync(ct); } catch { }
                await Task.Delay(25 * tentativa, ct);
            }
            catch
            {
                try { await tx.RollbackAsync(ct); } catch { }
                throw;
            }
        }
    }
}
