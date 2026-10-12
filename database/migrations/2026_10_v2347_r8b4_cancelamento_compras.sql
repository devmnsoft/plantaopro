-- R8-B4: trilha de auditoria do cancelamento de pedido de compra (ADM360 suprimentos).
-- O estado CANCELADO ja existe no check de situacao (v2191); este corte so registra
-- quem/quando/por que cancelou, no mesmo padrao de auditoria dos demais cancels do modulo.
alter table plantaopro.adm360_pedidos add column if not exists motivo_cancelamento text;
alter table plantaopro.adm360_pedidos add column if not exists cancelado_em timestamptz;
alter table plantaopro.adm360_pedidos add column if not exists cancelado_por uuid;
