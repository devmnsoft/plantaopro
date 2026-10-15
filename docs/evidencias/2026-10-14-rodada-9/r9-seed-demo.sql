-- s9/s10 (rodada 9 r9f) — dados de homologação do tenant Sistema Vertical (ids fixos = reexecução idempotente):
-- cliente comercial + pagador + venda CONFIRMADA (a prazo, 3 parcelas) + títulos:
--   parcela 1: vencida há 15 dias, ABERTA   (vencida na data do relatório)
--   parcela 2: vencida há 7 dias,  QUITADA (histórico de liquidação, não é "vencida")
--   parcela 3: vence em 20 dias,   ABERTA  (futura)
-- Valores: bruto 1200.00, desconto 200.00, líquido 1000.00, custo 600.00, comissão 5% (50.00)
-- Margem esperada: 1000 - 600 - 50 = 350.00 (35.00%)
-- Saldo aberto total: 333.33 + 333.34 = 666.67; vencido: 333.33 (1 parcela)

INSERT INTO plantaopro.adm360_parceiros(id, tenant_id, nome, documento, fornecedor, ativo, eh_cliente, eh_pagador)
VALUES ('e9f00001-9f0f-4a11-8f0f-000000000001', '8b0c8e74-a81b-4ea2-b499-94755a1ca301', 'Clinica Horizonte Azul', '99999999000199', false, true, true, false)
ON CONFLICT (id) DO NOTHING;

INSERT INTO plantaopro.adm360_parceiros(id, tenant_id, nome, documento, fornecedor, ativo, eh_cliente, eh_pagador)
VALUES ('e9f00002-9f0f-4a11-8f0f-000000000002', '8b0c8e74-a81b-4ea2-b499-94755a1ca301', 'Cofre Clinica Azul', '99999999000299', false, true, false, true)
ON CONFLICT (id) DO NOTHING;

INSERT INTO plantaopro.adm360_vendas(
    id, tenant_id, numero, origem_tipo, origem_id, cliente_id, pagador_id, competencia,
    total_bruto, desconto, total_liquido, total_custo, comissao_percentual, comissao_prevista,
    condicao_pagamento, quantidade_parcelas, situacao, idempotency_key, created_by
) VALUES (
    'e9f00003-9f0f-4a11-8f0f-000000000003', '8b0c8e74-a81b-4ea2-b499-94755a1ca301', 'VEN-SV-R9F-01', 'DIRETA',
    'e9f00003-9f0f-4a11-8f0f-000000000003', 'e9f00001-9f0f-4a11-8f0f-000000000001', 'e9f00002-9f0f-4a11-8f0f-000000000002',
    (CURRENT_DATE - interval '5 days')::date,
    1200.00, 200.00, 1000.00, 600.00, 5, 50.00, 'A_PRAZO', 3, 'CONFIRMADA', 'key-ven-sv-r9f', NULL
)
ON CONFLICT (id) DO NOTHING;

INSERT INTO plantaopro.adm360_titulos_receber(
    id, tenant_id, venda_id, numero, pagador_id, parcela, total_parcelas,
    data_emissao, data_vencimento, valor_principal, valor_recebido, saldo_aberto, situacao, created_by
) VALUES
    ('e9f00011-9f0f-4a11-8f0f-000000000011', '8b0c8e74-a81b-4ea2-b499-94755a1ca301', 'e9f00003-9f0f-4a11-8f0f-000000000003', 'TIT-SV-R9F-01', 'e9f00002-9f0f-4a11-8f0f-000000000002', 1, 3,
     (CURRENT_DATE - interval '5 days')::date, (CURRENT_DATE - interval '15 days')::date, 333.33, 0.00, 333.33, 'ABERTO', NULL),
    ('e9f00012-9f0f-4a11-8f0f-000000000012', '8b0c8e74-a81b-4ea2-b499-94755a1ca301', 'e9f00003-9f0f-4a11-8f0f-000000000003', 'TIT-SV-R9F-02', 'e9f00002-9f0f-4a11-8f0f-000000000002', 2, 3,
     (CURRENT_DATE - interval '5 days')::date, (CURRENT_DATE - interval '7 days')::date, 333.33, 333.33, 0.00, 'QUITADO', NULL),
    ('e9f00013-9f0f-4a11-8f0f-000000000013', '8b0c8e74-a81b-4ea2-b499-94755a1ca301', 'e9f00003-9f0f-4a11-8f0f-000000000003', 'TIT-SV-R9F-03', 'e9f00002-9f0f-4a11-8f0f-000000000002', 3, 3,
     (CURRENT_DATE - interval '5 days')::date, (CURRENT_DATE + interval '20 days')::date, 333.34, 0.00, 333.34, 'ABERTO', NULL)
ON CONFLICT (id) DO NOTHING;

-- Parceiro de OUTRO tenant (para provar a rejeição cross-tenant do filtro clienteId).
INSERT INTO plantaopro.adm360_parceiros(id, tenant_id, nome, documento, fornecedor, ativo, eh_cliente)
SELECT 'e9f00099-9f0f-4a11-8f0f-000000000099', t.id, 'Parceiro Outro Tenant (r9f)', '99999999000999', false, true, false
FROM (SELECT id FROM plantaopro.tenants WHERE id <> '8b0c8e74-a81b-4ea2-b499-94755a1ca301' ORDER BY id LIMIT 1) t
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.adm360_parceiros WHERE id = 'e9f00099-9f0f-4a11-8f0f-000000000099');
