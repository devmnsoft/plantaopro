using Dapper;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class Adm360FinanceiroRelatoriosRepository : Adm360Repository, IAdm360FinanceiroRelatoriosRepository
{
    public Adm360FinanceiroRelatoriosRepository(string connectionString) : base(connectionString) { }

    public async Task<IReadOnlyList<RelatorioVendasItem>> RelatorioVendasAsync(
        Guid tenantId, DateOnly? inicio, DateOnly? fim, Guid? hospitalId, Guid? vendedorId, CancellationToken ct)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT v.numero AS VendaNumero,
                   v.competencia AS Competencia,
                   COALESCE(c.nome, hosp.nome, 'Hospital') AS Hospital,
                   COALESCE(p.nome, c.nome, hosp.nome, 'Pagador') AS Pagador,
                   COALESCE(u.nome, 'Não informado') AS Vendedor,
                   v.total_liquido AS TotalLiquido,
                   v.total_custo AS TotalCusto,
                   v.comissao_prevista AS ComissaoPrevista,
                   v.situacao AS Situacao
            FROM plantaopro.adm360_vendas v
            LEFT JOIN plantaopro.adm360_parceiros c ON c.id = v.cliente_id AND c.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = v.cliente_id AND hosp.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros p ON p.id = v.pagador_id AND p.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.usuarios u ON u.id = v.vendedor_id AND u.tenant_id = v.tenant_id
            WHERE v.tenant_id = @tenantId
              AND (@inicio::date IS NULL OR v.competencia >= @inicio::date)
              AND (@fim::date IS NULL OR v.competencia <= @fim::date)
              AND (@hospitalId::uuid IS NULL OR v.cliente_id = @hospitalId::uuid)
              AND (@vendedorId::uuid IS NULL OR v.vendedor_id = @vendedorId::uuid)
            ORDER BY v.competencia DESC, v.numero DESC",
            new { tenantId, inicio, fim, hospitalId, vendedorId }, cancellationToken: ct));

        return rows.Select(r => new RelatorioVendasItem(
            (string)r.vendanumero,
            ToDateOnly(r.competencia),
            (string)r.hospital,
            (string)r.pagador,
            (string)r.vendedor,
            (decimal)r.totalliquido,
            (decimal)r.totalcusto,
            (decimal)r.comissaoprevista,
            (string)r.situacao
        )).ToList();
    }

    public async Task<IReadOnlyList<RelatorioComissaoItem>> RelatorioComissoesAsync(
        Guid tenantId, Guid? vendedorId, DateOnly? inicio, DateOnly? fim, CancellationToken ct)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT COALESCE(u.nome, 'Vendedor') AS Vendedor,
                   v.numero AS VendaNumero,
                   b.data_recebimento AS DataRecebimento,
                   c.base_calculo AS BaseCalculo,
                   c.percentual AS Percentual,
                   c.valor_comissao AS ComissaoApropriada,
                   c.situacao AS Situacao
            FROM plantaopro.adm360_comissoes_apropriadas c
            JOIN plantaopro.adm360_vendas v ON v.id = c.venda_id AND v.tenant_id = c.tenant_id
            LEFT JOIN plantaopro.usuarios u ON u.id = c.vendedor_id AND u.tenant_id = c.tenant_id
            LEFT JOIN plantaopro.adm360_titulo_baixas b ON b.id = c.baixa_id AND b.tenant_id = c.tenant_id
            WHERE c.tenant_id = @tenantId
              AND (@vendedorId::uuid IS NULL OR c.vendedor_id = @vendedorId::uuid)
              AND (@inicio::date IS NULL OR b.data_recebimento >= @inicio::date)
              AND (@fim::date IS NULL OR b.data_recebimento <= @fim::date)
            ORDER BY b.data_recebimento DESC, u.nome",
            new { tenantId, vendedorId, inicio, fim }, cancellationToken: ct));

        return rows.Select(r => new RelatorioComissaoItem(
            (string)r.vendedor,
            (string)r.vendanumero,
            r.datarecebimento is not null ? ToDateOnly(r.datarecebimento) : DateOnly.FromDateTime(DateTime.UtcNow),
            (decimal)r.basecalculo,
            (decimal)r.percentual,
            (decimal)r.comissaoapropriada,
            (string)r.situacao
        )).ToList();
    }

    public async Task<IReadOnlyList<RelatorioMargemItem>> RelatorioMargemAsync(
        Guid tenantId, DateOnly? inicio, DateOnly? fim, CancellationToken ct)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT v.numero AS VendaNumero,
                   COALESCE(vale.numero, 'N/A') AS ValeNumero,
                   COALESCE(c.nome, hosp.nome, 'Hospital') AS Hospital,
                   v.total_liquido AS ReceitaLiquida,
                   v.total_custo AS CustoConsumido,
                   v.comissao_prevista AS ComissaoPrevista,
                   COALESCE((SELECT SUM(com.valor_comissao) FROM plantaopro.adm360_comissoes_apropriadas com WHERE com.venda_id = v.id AND com.tenant_id = v.tenant_id AND com.situacao = 'APROPRIADA'), 0) AS ComissaoApropriada
            FROM plantaopro.adm360_vendas v
            LEFT JOIN plantaopro.adm360_valorizacoes val ON val.id = v.valorizacao_id AND val.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_vales vale ON vale.id = val.vale_id AND vale.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros c ON c.id = v.cliente_id AND c.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = v.cliente_id AND hosp.tenant_id = v.tenant_id
            WHERE v.tenant_id = @tenantId
              AND v.situacao = 'CONFIRMADA'
              AND (@inicio::date IS NULL OR v.competencia >= @inicio::date)
              AND (@fim::date IS NULL OR v.competencia <= @fim::date)
            ORDER BY v.competencia DESC, v.numero DESC",
            new { tenantId, inicio, fim }, cancellationToken: ct));

        return rows.Select(r =>
        {
            decimal receita = (decimal)r.receitaliquida;
            decimal custo = (decimal)r.custoconsumido;
            decimal comissaoPrevista = (decimal)r.comissaoprevista;
            decimal comissaoApropriada = (decimal)r.comissaoapropriada;
            decimal margem = receita - custo - comissaoPrevista;
            decimal margemPct = receita > 0 ? Math.Round((margem / receita) * 100m, 2) : 0m;

            return new RelatorioMargemItem(
                (string)r.vendanumero,
                (string)r.valenumero,
                (string)r.hospital,
                receita,
                custo,
                comissaoPrevista,
                comissaoApropriada,
                margem,
                margemPct
            );
        }).ToList();
    }
}
