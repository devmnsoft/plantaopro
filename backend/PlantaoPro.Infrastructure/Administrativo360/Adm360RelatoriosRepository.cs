using Dapper;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class Adm360RelatoriosRepository : Adm360Repository, IAdm360RelatoriosRepository
{
    public Adm360RelatoriosRepository(string connectionString) : base(connectionString) { }

    public async Task<IReadOnlyList<RelatorioValesPendentesItem>> ValesPendentesAsync(Guid tenantId, CancellationToken ct)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT v.id AS ValeId,
                   v.numero AS Numero,
                   COALESCE(h.nome, hosp.nome, 'Hospital') AS Hospital,
                   c.numero AS CirurgiaNumero,
                   v.data_saida_efetiva AS DataSaida,
                   v.data_retorno_prevista AS DataRetornoPrevista,
                   COALESCE(u.nome, 'Não informado') AS Responsavel,
                   COALESCE(SUM(vi.quantidade_expedida - vi.quantidade_consumida - vi.quantidade_devolvida - vi.quantidade_perda), 0) AS QuantidadePendente,
                   CASE 
                       WHEN v.data_retorno_prevista IS NOT NULL AND v.data_retorno_prevista < CURRENT_DATE
                       THEN (CURRENT_DATE - v.data_retorno_prevista)
                       ELSE 0
                   END AS DiasAtraso
            FROM plantaopro.adm360_vales v
            LEFT JOIN plantaopro.adm360_cirurgias c ON c.id = v.cirurgia_id AND c.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros h ON h.id = v.hospital_id AND h.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = v.hospital_id AND hosp.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.usuarios u ON u.id = v.created_by AND u.tenant_id = v.tenant_id
            JOIN plantaopro.adm360_vale_itens vi ON vi.vale_id = v.id AND vi.tenant_id = v.tenant_id
            WHERE v.tenant_id = @tenantId
              AND v.situacao IN ('EXPEDIDO', 'RETORNO_PARCIAL')
            GROUP BY v.id, v.numero, h.nome, hosp.nome, c.numero, v.data_saida_efetiva, v.data_retorno_prevista, u.nome
            HAVING SUM(vi.quantidade_expedida - vi.quantidade_consumida - vi.quantidade_devolvida - vi.quantidade_perda) > 0
            ORDER BY v.data_retorno_prevista ASC NULLS LAST, v.data_saida_efetiva ASC",
            new { tenantId }, cancellationToken: ct));

        return rows.Select(r => new RelatorioValesPendentesItem(
            (Guid)r.valeid,
            (string)r.numero,
            (string)r.hospital,
            (string?)r.cirurgianumero,
            (DateTimeOffset?)r.datasaida,
            r.dataretornoprevista is not null ? DateOnly.FromDateTime((DateTime)r.dataretornoprevista) : null,
            (decimal)r.quantidadependente,
            (string)r.responsavel,
            (int)r.diasatraso)).ToList();
    }

    public async Task<IReadOnlyList<RelatorioCustodiaExternaItem>> CustodiaExternaAsync(Guid tenantId, CancellationToken ct)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT dest.id AS LocalId,
                   dest.nome AS Local,
                   p.id AS ProdutoId,
                   p.sku AS Sku,
                   p.nome AS Produto,
                   l.id AS LoteId,
                   l.codigo AS Lote,
                   l.validade AS Validade,
                   COALESCE(h.nome, hosp.nome, dest.nome) AS Hospital,
                   v.id AS ValeId,
                   v.numero AS ValeNumero,
                   (vi.quantidade_expedida - vi.quantidade_consumida - vi.quantidade_devolvida - vi.quantidade_perda) AS Quantidade
            FROM plantaopro.adm360_vale_itens vi
            JOIN plantaopro.adm360_vales v ON v.id = vi.vale_id AND v.tenant_id = vi.tenant_id
            JOIN plantaopro.adm360_produtos p ON p.id = vi.produto_id AND p.tenant_id = vi.tenant_id
            JOIN plantaopro.adm360_lotes l ON l.id = vi.lote_id AND l.tenant_id = vi.tenant_id
            JOIN plantaopro.adm360_locais dest ON dest.id = v.local_destino_id AND dest.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros h ON h.id = v.hospital_id AND h.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = v.hospital_id AND hosp.tenant_id = v.tenant_id
            WHERE vi.tenant_id = @tenantId
              AND v.situacao IN ('EXPEDIDO', 'RETORNO_PARCIAL')
              AND (vi.quantidade_expedida - vi.quantidade_consumida - vi.quantidade_devolvida - vi.quantidade_perda) > 0
            ORDER BY dest.nome, p.nome, l.codigo",
            new { tenantId }, cancellationToken: ct));

        return rows.Select(r => new RelatorioCustodiaExternaItem(
            (Guid)r.localid,
            (string)r.local,
            (Guid)r.produtoid,
            (string)r.sku,
            (string)r.produto,
            (Guid)r.loteid,
            (string)r.lote,
            r.validade is not null ? DateOnly.FromDateTime((DateTime)r.validade) : null,
            (string)r.hospital,
            (Guid)r.valeid,
            (string)r.valenumero,
            (decimal)r.quantidade)).ToList();
    }

    public async Task<IReadOnlyList<RelatorioReconciliacaoItem>> ReconciliacaoAsync(Guid tenantId, CancellationToken ct)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT v.id AS ValeId,
                   v.numero AS Numero,
                   COALESCE(h.nome, hosp.nome, 'Hospital') AS Hospital,
                   c.numero AS Cirurgia,
                   COALESCE(SUM(vi.quantidade_expedida), 0) AS TotalExpedido,
                   COALESCE(SUM(vi.quantidade_consumida), 0) AS TotalConsumido,
                   COALESCE(SUM(vi.quantidade_devolvida), 0) AS TotalDevolvido,
                   COALESCE(SUM(vi.quantidade_perda), 0) AS TotalPerda,
                   COALESCE(SUM(vi.quantidade_expedida - vi.quantidade_consumida - vi.quantidade_devolvida - vi.quantidade_perda), 0) AS PendenteCustodia,
                   v.situacao AS Situacao
            FROM plantaopro.adm360_vales v
            LEFT JOIN plantaopro.adm360_cirurgias c ON c.id = v.cirurgia_id AND c.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros h ON h.id = v.hospital_id AND h.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = v.hospital_id AND hosp.tenant_id = v.tenant_id
            JOIN plantaopro.adm360_vale_itens vi ON vi.vale_id = v.id AND vi.tenant_id = v.tenant_id
            WHERE v.tenant_id = @tenantId
            GROUP BY v.id, v.numero, h.nome, hosp.nome, c.numero, v.situacao
            ORDER BY v.created_at DESC",
            new { tenantId }, cancellationToken: ct));

        return rows.Select(r => new RelatorioReconciliacaoItem(
            (Guid)r.valeid,
            (string)r.numero,
            (string)r.hospital,
            (string?)r.cirurgia,
            (decimal)r.totalexpedido,
            (decimal)r.totalconsumido,
            (decimal)r.totaldevolvido,
            (decimal)r.totalperda,
            (decimal)r.pendentecustodia,
            (string)r.situacao)).ToList();
    }

    public async Task<IReadOnlyList<RelatorioRastreabilidadeItem>> RastreabilidadeAsync(
        Guid tenantId, string? produtoOuLote, CancellationToken ct)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT p.nome AS Produto,
                   l.codigo AS Lote,
                   l.validade AS Validade,
                   m.documento_tipo AS OrigemTipo,
                   m.documento_id::text AS DocumentoOrigem,
                   v.numero AS ValeNumero,
                   COALESCE(h.nome, hosp.nome, '') AS Hospital,
                   loc.nome AS LocalAtual,
                   m.condicao AS Condicao,
                   m.quantidade AS Quantidade,
                   m.created_at AS DataMovimento
            FROM plantaopro.adm360_movimentos m
            JOIN plantaopro.adm360_produtos p ON p.id = m.produto_id AND p.tenant_id = m.tenant_id
            JOIN plantaopro.adm360_lotes l ON l.id = m.lote_id AND l.tenant_id = m.tenant_id
            JOIN plantaopro.adm360_locais loc ON loc.id = m.local_id AND loc.tenant_id = m.tenant_id
            LEFT JOIN plantaopro.adm360_vales v ON v.id = m.documento_id AND v.tenant_id = m.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros h ON h.id = v.hospital_id AND h.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = v.hospital_id AND hosp.tenant_id = v.tenant_id
            WHERE m.tenant_id = @tenantId
              AND (@produtoOuLote IS NULL OR p.nome ILIKE '%' || @produtoOuLote || '%' OR l.codigo ILIKE '%' || @produtoOuLote || '%')
            ORDER BY m.created_at DESC",
            new { tenantId, produtoOuLote }, cancellationToken: ct));

        return rows.Select(r => new RelatorioRastreabilidadeItem(
            (string)r.produto,
            (string)r.lote,
            r.validade is not null ? DateOnly.FromDateTime((DateTime)r.validade) : null,
            (string)r.origemtipo,
            (string?)r.documentoorigem,
            (string?)r.valenumero,
            (string?)r.hospital,
            (string)r.localatual,
            (string)r.condicao,
            (decimal)r.quantidade,
            (DateTimeOffset)r.datamovimento)).ToList();
    }
}
