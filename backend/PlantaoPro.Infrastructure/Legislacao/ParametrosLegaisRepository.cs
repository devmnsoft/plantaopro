using Dapper;
using Npgsql;
using PlantaoPro.Application.Legislacao;

namespace PlantaoPro.Infrastructure.Legislacao;

/// <summary>
/// R80 (bloco B4): leitura da fonte única de parâmetros legais
/// (v2363_r80_parametros_legais). Tabela global (sem tenant) e somente leitura
/// nesta rodada: gravações futuras passam por migração versionada com fonte citável.
/// </summary>
public sealed class ParametrosLegaisRepository : IParametrosLegaisRepository
{
    private readonly string connectionString;

    public ParametrosLegaisRepository(string connectionString) => this.connectionString = connectionString;

    public async Task<List<ParametroLegalDto>> ListarAtivosAsync(int? ano, CancellationToken ct = default)
    {
        await using var cn = new NpgsqlConnection(connectionString);
        await cn.OpenAsync();
        // Dapper exige tipo exato no construtor do DTO: smallint nao casa com int,
        // por isso o cast ::int (mesma razao de created_at::timestamp no padrao R72).
        var sql = @"select codigo as Codigo, descricao as Descricao, valor as Valor,
                   fonte_oficial as FonteOficial, ano::int as Ano,
                   coalesce(vigencia_inicio::text, '') as VigenciaInicio,
                   coalesce(vigencia_fim::text, '') as VigenciaFim
                     from plantaopro.parametros_legais
                    where reg_status = 'A'";
        if (ano is not null) sql += " and ano = @ano";
        sql += " order by codigo, ano desc";
        var def = new CommandDefinition(sql, new { ano }, cancellationToken: ct);
        return (await cn.QueryAsync<ParametroLegalDto>(def)).ToList();
    }

    public async Task<ParametroLegalDto?> ObterPorCodigoAsync(string codigo, int? ano, CancellationToken ct = default)
    {
        await using var cn = new NpgsqlConnection(connectionString);
        await cn.OpenAsync();
        var sql = @"select codigo as Codigo, descricao as Descricao, valor as Valor,
                   fonte_oficial as FonteOficial, ano::int as Ano,
                   coalesce(vigencia_inicio::text, '') as VigenciaInicio,
                   coalesce(vigencia_fim::text, '') as VigenciaFim
                     from plantaopro.parametros_legais
                    where reg_status = 'A' and codigo = @codigo
                      and (@ano::smallint is null or ano = @ano)
                 order by ano desc
                    limit 1";
        var def = new CommandDefinition(sql, new { codigo, ano }, cancellationToken: ct);
        return await cn.QueryFirstOrDefaultAsync<ParametroLegalDto>(def);
    }
}
