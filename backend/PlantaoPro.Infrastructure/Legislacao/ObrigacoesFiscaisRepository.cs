using Dapper;
using Npgsql;

namespace PlantaoPro.Infrastructure.Legislacao;

/// <summary>
/// R84 (bloco B4): estado por tenant do calendario fiscal (v2367). Catalogo e datas oficiais
/// vivem na fonte unica (parametros_legais); aqui persiste somente a ativacao por periodo e a
/// marca de conclusao. Tabela sem FK de referencia de tenant (o tenant_id e o da sessao).
/// </summary>
public sealed class ObrigacoesFiscaisRepository
{
    private sealed record EstadoRow(bool Ativa, DateTime? ConcluidaEm);

    private readonly string connectionString;

    public ObrigacoesFiscaisRepository(string connectionString) => this.connectionString = connectionString;

    public async Task<(bool Ativa, DateTime? ConcluidaEm)?> ObterEstadoAsync(Guid tenantId, string codigo, int mesReferencia, int anoReferencia, CancellationToken ct = default)
    {
        await using var cn = new NpgsqlConnection(connectionString);
        await cn.OpenAsync();
        var def = new CommandDefinition(@"select ativa as Ativa, coalesce(concluida_em::timestamp, null) as ConcluidaEm
                                            from plantaopro.adm_obrigacoes_fiscais
                                           where tenant_id = @tenant and codigo_obrigacao = @codigo
                                             and mes_referencia = @mes and ano_referencia = @ano and reg_status = 'A'
                                         limit 1",
            new { tenant = tenantId, codigo, mes = mesReferencia, ano = anoReferencia }, cancellationToken: ct);
        var linha = await cn.QueryFirstOrDefaultAsync<EstadoRow>(def);
        return linha is null ? null : (linha.Ativa, linha.ConcluidaEm);
    }

    public async Task RegistrarAtivaAsync(Guid tenantId, string codigo, int mesReferencia, int anoReferencia, bool ativa, CancellationToken ct = default)
    {
        await using var cn = new NpgsqlConnection(connectionString);
        await cn.OpenAsync();
        var def = new CommandDefinition(@"insert into plantaopro.adm_obrigacoes_fiscais (tenant_id, codigo_obrigacao, mes_referencia, ano_referencia, ativa)
                                         values (@tenant, @codigo, @mes, @ano, @ativa)
                                        on conflict (tenant_id, codigo_obrigacao, mes_referencia, ano_referencia) where reg_status = 'A'
                                       do update set ativa = @ativa, updated_at = now()",
            new { tenant = tenantId, codigo, mes = mesReferencia, ano = anoReferencia, ativa }, cancellationToken: ct);
        await cn.ExecuteAsync(def);
    }

    public async Task AlternarConclusaoAsync(Guid tenantId, string codigo, int mesReferencia, int anoReferencia, string? usuario, CancellationToken ct = default)
    {
        await using var cn = new NpgsqlConnection(connectionString);
        await cn.OpenAsync();
        var def = new CommandDefinition(@"insert into plantaopro.adm_obrigacoes_fiscais (tenant_id, codigo_obrigacao, mes_referencia, ano_referencia, concluida_em, concluida_por)
                                         values (@tenant, @codigo, @mes, @ano, now(), coalesce(@usuario, ''))
                                        on conflict (tenant_id, codigo_obrigacao, mes_referencia, ano_referencia) where reg_status = 'A'
                                       do update set concluida_em = case when adm_obrigacoes_fiscais.concluida_em is null then now() else null end,
                                                       concluida_por = case when adm_obrigacoes_fiscais.concluida_em is null then coalesce(@usuario, '') else null end,
                                                       updated_at = now()",
            new { tenant = tenantId, codigo, mes = mesReferencia, ano = anoReferencia, usuario }, cancellationToken: ct);
        await cn.ExecuteAsync(def);
    }
}
