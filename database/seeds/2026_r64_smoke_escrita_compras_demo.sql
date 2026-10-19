-- ============================================================================
-- PlantaoPro | Seed operacional: 2026_r64_smoke_escrita_compras_demo
-- R64 (r53b1 §7 item 9): fixture dedicado do smoke de escrita de compras
-- (criar -> aprovar -> receber) sobre o tenant do demo Santa Casa.
--
-- Três linhas fixas (UUIDs determinísticos) que o smoke usa como fornecedor,
-- produto e local interno de destino:
--   * adm360_parceiros : 'Fornecedor SMOKE R64 (teste)'       (fornecedor = true)
--   * adm360_produtos  : SKU 'SMK64' — sem controle de lote/inspeção obrigatória,
--                        para o recebimento ir direto a LIBERADO no estoque
--   * adm360_locais    : código 'SMK64ALM' (tipo INTERNO)
--
-- Idempotência (contrato da rodada): upsert por chave primária
-- (cláusula ON CONFLICT com DO UPDATE), reaplicável quantas vezes quiser sem duplicar,
-- sem apagar nada e sem tocar em dados que não sejam destas três linhas
-- deste tenant. Aplicar em qualquer banco que tenha o tenant do demo
-- (plantaopro_test nos gates; plantaopro/dev quando o app rodar apontando
-- para lá). Não faz parte do install obrigatório — é fixture de smoke.
-- ============================================================================

-- 1) Fornecedor exclusivo do smoke.
INSERT INTO plantaopro.adm360_parceiros (id, tenant_id, nome, documento, fornecedor, ativo)
VALUES (
    '7c8f0a01-6d4a-4a63-9e11-a1b2c3d4e5f1',
    'd3f6584c-2c64-4e5a-9ea9-4e1428647502',
    'Fornecedor SMOKE R64 (teste)',
    '99.999.999/0001-99',
    true,
    true
)
ON CONFLICT (id) DO UPDATE
SET nome = EXCLUDED.nome,
    documento = EXCLUDED.documento,
    fornecedor = true,
    ativo = true;

-- 2) Produto exclusivo do smoke (SKU estável p/ o smoke reconhecer na UI).
INSERT INTO plantaopro.adm360_produtos (id, tenant_id, sku, nome, unidade, controla_lote, exige_inspecao, preco_custo, ativo)
VALUES (
    '7c8f0a02-6d4a-4a63-9e11-a1b2c3d4e5f2',
    'd3f6584c-2c64-4e5a-9ea9-4e1428647502',
    'SMK64',
    'Insumo SMOKE R64 (teste de ponta a ponta)',
    'UN',
    false,
    false,
    8.40,
    true
)
ON CONFLICT (id) DO UPDATE
SET nome = EXCLUDED.nome,
    unidade = EXCLUDED.unidade,
    controla_lote = false,
    exige_inspecao = false,
    preco_custo = EXCLUDED.preco_custo,
    ativo = true;

-- 3) Local interno exclusivo do smoke (destino do recebimento).
INSERT INTO plantaopro.adm360_locais (id, tenant_id, codigo, nome, tipo, ativo)
VALUES (
    '7c8f0a03-6d4a-4a63-9e11-a1b2c3d4e5f3',
    'd3f6584c-2c64-4e5a-9ea9-4e1428647502',
    'SMK64ALM',
    'Almoxarifado SMOKE R64 (teste)',
    'INTERNO',
    true
)
ON CONFLICT (id) DO UPDATE
SET codigo = EXCLUDED.codigo,
    nome = EXCLUDED.nome,
    tipo = 'INTERNO',
    ativo = true;
