-- ============================================================================
-- v2317 | B9 Saude 360 | Contestacao financeira: situacao_anterior
-- ----------------------------------------------------------------------------
-- Decisao (R4-B9, opcao B): contestar um pagamento pendente ou aprovado
-- passa a marca-lo como 'contestado', preservando a situacao de origem.
-- Para que a resolucao (MANTER_VALOR / AJUSTAR_VALOR / CANCELAR_PAGAMENTO)
-- restaure o estado anterior de forma deterministica e explicavel, a situacao
-- do pagamento imediatamente antes da contestacao e gravada na propria
-- contestacao. Linhas historicas permanecem com situacao_anterior NULL; a
-- resolucao as trata explicitamente (409 orientando manutencao de dados) em
-- vez de inferir o estado anterior.
-- O indice unico parcial ux_pagamento_contestacao_aberta (tenant_id,
-- pagamento_id) WHERE status='ABERTA' ja existe no schema (regra de uma
-- unica contestacao aberta por pagamento); nenhum novo indice nesta migracao.
-- ============================================================================
set search_path to plantaopro, public;

alter table plantaopro.pagamento_contestacoes
    add column if not exists situacao_anterior varchar(32);

comment on column plantaopro.pagamento_contestacoes.situacao_anterior is
    'Situacao do pagamento imediatamente antes da contestacao (pendente/aprovado/pago). NULL apenas em linhas historicas criadas antes da v2317.';
