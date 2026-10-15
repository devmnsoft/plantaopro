-- r9g (s11) — dados de homologacao do DRE gerencial no tenant Sistema Vertical (idempotente, ids fixos)
-- Complementa o seed r9f (VEN-SV-R9F-01 / Clinica Horizonte Azul). Adiciona:
--   * venda ORFA (cliente_id que nao resolve em parceiro/hospital) -> bucket "Nao atribu"
--   * 2 titulos pagar: 1 APROVADO na competencia (contabilizado) + 1 CANCELADO (fora)
DO $$
BEGIN
  INSERT INTO plantaopro.adm360_vendas(
    id, tenant_id, numero, origem_tipo, origem_id, cliente_id, pagador_id, competencia,
    total_bruto, desconto, total_liquido, total_custo, comissao_percentual, comissao_prevista,
    condicao_pagamento, quantidade_parcelas, situacao, idempotency_key, created_by
  ) VALUES (
    'e9a00003-9f0f-4a11-8f0f-000000000003',
    '8b0c8e74-a81b-4ea2-b499-94755a1ca301',
    'VEN-SV-R9G-ORFA', 'DIRETA', 'e9a00003-9f0f-4a11-8f0f-000000000003',
    'e9a0aaaa-9f0f-4a11-8f0f-0000000000aa',   -- sem parceiro/hospital -> Nao atribuida
    'e9f00002-9f0f-4a11-8f0f-000000000002',   -- pagador "Cofre Clinica Azul" (seed r9f)
    CURRENT_DATE - 5,
    300.00, 50.00, 250.00, 100.00, 1, 2.50, 'A_PRAZO', 1, 'CONFIRMADA', 'key-ven-sv-r9g-orfa', NULL
  )
  ON CONFLICT (id) DO NOTHING;

  INSERT INTO plantaopro.adm360_titulos_pagar(
    id, tenant_id, numero, origem_tipo, fornecedor_id, competencia, data_emissao, data_vencimento,
    valor_principal, situacao, saldo_aberto
  ) VALUES
    ('e9a00011-9f0f-4a11-8f0f-000000000011', '8b0c8e74-a81b-4ea2-b499-94755a1ca301',
     'TP-SV-R9G-01', 'DESPESA_MANUAL', 'e9f00002-9f0f-4a11-8f0f-000000000002',
     CURRENT_DATE - 5, CURRENT_DATE - 5, CURRENT_DATE - 5, 40.00, 'APROVADO', 40.00),
    ('e9a00012-9f0f-4a11-8f0f-000000000012', '8b0c8e74-a81b-4ea2-b499-94755a1ca301',
     'TP-SV-R9G-02', 'DESPESA_MANUAL', 'e9f00002-9f0f-4a11-8f0f-000000000002',
     CURRENT_DATE - 5, CURRENT_DATE - 5, CURRENT_DATE - 5, 99.00, 'CANCELADO', 99.00)
  ON CONFLICT (id) DO NOTHING;
END $$;
