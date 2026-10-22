-- R92: índices de apoio aos caminhos mais quentes do bloco ADM360 (EXPLAIN ANALYZE medido na R92,
-- banco de teste, tenant mais denso com 10.172 orçamentos / 17.886 movimentos / 2.623 reservas).
-- 1) adm360_reservas por origem: o detalhe do orçamento busca reservas por item (N+1, uma consulta
--    por linha do item) e o join de cirurgias filtra por (tenant_id, origem_tipo, origem_id); sem
--    índice líder em origem, cada acesso fazia seq scan completo da tabela de reservas. O INCLUDE
--    (produto_id, quantidade) torna o scan index-only para a soma por item (a consulta filtra o
--    produto e agrega a quantidade sem tocar na heap).
CREATE INDEX IF NOT EXISTS ix_adm360_reservas_origem ON plantaopro.adm360_reservas(tenant_id, origem_tipo, origem_id) INCLUDE (produto_id, quantidade) WHERE situacao = 'ATIVA';
-- 2) adm360_orcamentos na ordem da listagem padrão (created_at DESC): sem índice alinhado ao
--    ORDER BY, cada página da listagem paginada refazia seq scan + top-N sort de todas as linhas
--    do tenant (4,4 ms na página 1 com 10k linhas; custo cresce linearmente com o volume e com a
--    profundidade do OFFSET). Com o índice, cada página é proporcional apenas ao tamanho da página.
CREATE INDEX IF NOT EXISTS ix_adm360_orcamentos_tenant_criado ON plantaopro.adm360_orcamentos(tenant_id, created_at DESC);
