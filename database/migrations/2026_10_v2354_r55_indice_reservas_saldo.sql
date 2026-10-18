-- R55: índice de apoio à view adm360_saldos — cobre o padrão da subquery correlacionada das
-- reservas ativas (sem ele cada grupo de saldo refaz scan completo de adm360_reservas duas vezes).
CREATE INDEX IF NOT EXISTS ix_adm360_reservas_saldo ON plantaopro.adm360_reservas(tenant_id,produto_id,lote_id,local_id) INCLUDE(quantidade) WHERE situacao='ATIVA';
