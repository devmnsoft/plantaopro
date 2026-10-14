-- PlantãoPro v2.35.3 (R9-D / §7): preço de venda padrão dos produtos do Administrativo 360 (precificação).
-- preco_venda numeric(18,4) NULL — NULL = preço de venda padrão NÃO DEFINIDO (estado "ausente"),
-- nunca substituído por zero. O custo (preco_custo) permanece nullable: CUSTO AUSENTE ≠ ZERO.
-- Os campos derivados (lucro bruto unitário, margem sobre venda %, markup sobre custo %) são
-- recalculados NO SERVIDOR (Application/Administrativo360/Contracts.cs - Produto360) com
-- arredondamento de 2 casas; denominador zero => "não calculável"; margem negativa não é
-- truncada (evidência de margem negativa). Esta DDL não reescreve cotações/vendas existentes.
-- Rerun idempotente: add column if not exists / comment on (sobrescreve).

alter table plantaopro.adm360_produtos
    add column if not exists preco_venda numeric(18,4);

-- Custo AUSENTE ≠ ZERO: a coluna histórica é NOT NULL DEFAULT 0, o que tornava impossível
-- representar "custo não informado" (o default 0 virava margem 100% nas derivadas).
-- Passa a admitir NULL; linhas existentes não são modificadas (sem backfill).
-- Rerun idempotente: drop not null em coluna já nullable é no-op.
alter table plantaopro.adm360_produtos
    alter column preco_custo drop not null;

comment on column plantaopro.adm360_produtos.preco_venda is
    'Preco de venda padrao (s7 rodada 9). NULL = nao definido; derivadas calculadas no servidor; custo ausente != zero.';
