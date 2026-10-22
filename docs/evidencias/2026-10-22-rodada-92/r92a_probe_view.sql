-- R92 - probe (secao unica do psql, banco de teste): reescrita candidata da view adm360_saldos.
-- Forma atual: duas subqueries correlacionadas em adm360_reservas (uma por coluna reservado/disponivel),
-- executadas uma vez POR GRUPO (loops ~= 9.3k + 7.6k no recon r92a0). Candidata: LEFT JOIN unico numa
-- pre-agregacao das reservas ativas (no maximo 1 linha por chave do grupo => sem fan-out).
-- Verificacao: equivalencia exata dos conjuntos (EXCEPT nos dois sentidos deve ser 0) + EXPLAIN ANALYZE.
\set ON_ERROR_STOP on

CREATE TEMP VIEW saldos_r92 AS
SELECT m.tenant_id,
       m.produto_id,
       p.nome AS produto,
       m.lote_id,
       l.codigo AS lote,
       l.validade,
       m.local_id,
       o.nome AS local,
       m.condicao,
       (sum(m.quantidade))::numeric(18,4) AS fisico,
       (COALESCE(r.reservado_q, 0))::numeric(18,4) AS reservado,
       (CASE WHEN ((m.condicao)::text = 'LIBERADO'::text) AND (l.validade IS NULL OR l.validade >= CURRENT_DATE)
             THEN sum(m.quantidade) - COALESCE(r.reservado_q, 0)
             ELSE 0 END)::numeric(18,4) AS disponivel
FROM plantaopro.adm360_movimentos m
JOIN plantaopro.adm360_produtos p ON p.id = m.produto_id AND p.tenant_id = m.tenant_id
JOIN plantaopro.adm360_lotes l ON l.id = m.lote_id AND l.tenant_id = m.tenant_id
JOIN plantaopro.adm360_locais o ON o.id = m.local_id AND o.tenant_id = m.tenant_id
LEFT JOIN (SELECT tenant_id, produto_id, lote_id, local_id, sum(quantidade) AS reservado_q
           FROM plantaopro.adm360_reservas
           WHERE situacao = 'ATIVA'
           GROUP BY tenant_id, produto_id, lote_id, local_id) r
  ON r.tenant_id = m.tenant_id AND r.produto_id = m.produto_id AND r.lote_id = m.lote_id AND r.local_id = m.local_id
GROUP BY m.tenant_id, m.produto_id, p.nome, m.lote_id, l.codigo, l.validade, m.local_id, o.nome, m.condicao, r.reservado_q;

-- tenant mais denso no momento do probe (recon r92a0)
\timing on

SELECT '== EQUIVALENCIA (diferenca total entre view atual e candidata; esperado 0) ==' AS secao;
SELECT count(*) AS linhas_diferentes FROM (
  (SELECT tenant_id, produto_id, lote_id, local_id, condicao, fisico, reservado, disponivel FROM plantaopro.adm360_saldos
   EXCEPT
   SELECT tenant_id, produto_id, lote_id, local_id, condicao, fisico, reservado, disponivel FROM saldos_r92)
  UNION ALL
  (SELECT tenant_id, produto_id, lote_id, local_id, condicao, fisico, reservado, disponivel FROM saldos_r92
   EXCEPT
   SELECT tenant_id, produto_id, lote_id, local_id, condicao, fisico, reservado, disponivel FROM plantaopro.adm360_saldos)
) d;

SELECT '== EXPLAIN CANDIDATA - LINHAS paginadas p1 (baseline recon: 68.8 ms) ==' AS secao;
EXPLAIN (ANALYZE, BUFFERS) SELECT produto_id, produto, lote_id, lote, validade, local_id, local, condicao, fisico, reservado, disponivel
FROM saldos_r92 WHERE tenant_id = 'd3f6584c-2c64-4e5a-9ea9-4e1428647502'
ORDER BY produto, validade nulls last LIMIT 50 OFFSET 0;

SELECT '== EXPLAIN CANDIDATA - SOMAS agregadas (baseline recon: 63.3 ms) ==' AS secao;
EXPLAIN (ANALYZE, BUFFERS) SELECT coalesce(sum(fisico),0), coalesce(sum(reservado),0), coalesce(sum(disponivel),0)
FROM saldos_r92 WHERE tenant_id = 'd3f6584c-2c64-4e5a-9ea9-4e1428647502';

DROP VIEW saldos_r92;
