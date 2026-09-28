using System.Data;
using System.Globalization;
using Dapper;

namespace PlantaoPro.Api;

/// <summary>
/// O Dapper usado neste projeto não mapeia <see cref="DateOnly"/> em parâmetros
/// (SqlMapper.LookupDbType lança NotSupportedException). O handler cobre as duas direções:
/// escrita (SetValue envia DateTime à meia-noite, sem perda em coluna <c>date</c>) e
/// leitura (Parse converte o DateTime retornado pelo Npgsql de volta para DateOnly).
/// </summary>
public sealed class DateOnlyDapperTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        // DbType.Date faz o Npgsql enviar o parâmetro tipado como `date` (e não `timestamp`),
        // o que é necessário para funções como daterange() e consistente com as colunas da tabela.
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
        parameter.DbType = DbType.Date;
    }

    public override DateOnly Parse(object value) => value switch
    {
        DateOnly d => d,
        DateTime dt => DateOnly.FromDateTime(dt),
        string s => DateOnly.Parse(s, CultureInfo.InvariantCulture),
        _ => throw new InvalidCastException($"Não é possível converter {value?.GetType().Name ?? "null"} para DateOnly no Dapper.")
    };
}

/// <summary>
/// Registro global e idempotente dos handlers Dapper do processo.
/// Idempotência é necessária porque testes hospedam mais de um host (WebApplicationFactory)
/// no mesmo processo; registrar o mesmo tipo duas vezes lançaria ArgumentException.
/// </summary>
public static class DapperTypeHandlerRegistrar
{
    private static readonly object Portao = new();
    private static bool _pronto;

    public static void RegistrarTodos()
    {
        lock (Portao)
        {
            if (_pronto) return;
            SqlMapper.AddTypeHandler(new DateOnlyDapperTypeHandler());
            _pronto = true;
        }
    }
}
