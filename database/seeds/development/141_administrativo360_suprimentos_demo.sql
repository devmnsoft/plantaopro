\set ON_ERROR_STOP on
-- Massa opt-in de Suprimentos, Estoque, Orçamentos Cirúrgicos e Reservas do Administrativo 360.
-- Reaplicável; não altera senhas de usuários existentes.
DO $$
DECLARE 
    t1 uuid; 
    t2 uuid;
    usr1 uuid;
    usr2 uuid;
    
    fornecedor uuid:='a3610000-0000-4000-8000-000000000001'; 
    hospital_p uuid:='a3610000-0000-4000-8000-000000000030';
    produto uuid:='a3610000-0000-4000-8000-000000000002'; 
    
    local_cd uuid:='a3610000-0000-4000-8000-000000000003'; 
    local_hosp uuid:='a3610000-0000-4000-8000-000000000004'; 
    local_inv uuid:='a3610000-0000-4000-8000-000000000020';
    
    lote_livre uuid:='a3610000-0000-4000-8000-000000000005'; 
    lote_q uuid:='a3610000-0000-4000-8000-000000000006'; 
    lote_v uuid:='a3610000-0000-4000-8000-000000000007'; 
    lote_vence_antes uuid:='a3610000-0000-4000-8000-000000000025';

    pedido uuid:='a3610000-0000-4000-8000-000000000008'; 
    item uuid:='a3610000-0000-4000-8000-000000000009'; 
    receb uuid:='a3610000-0000-4000-8000-000000000010'; 
    ri uuid:='a3610000-0000-4000-8000-000000000011'; 

    orc_rascunho uuid:='a3610000-0000-4000-8000-000000000040';
    orc_aprovado uuid:='a3610000-0000-4000-8000-000000000041';
    orc_item_1 uuid:='a3610000-0000-4000-8000-000000000042';
    orc_item_2 uuid:='a3610000-0000-4000-8000-000000000043';
    reserva_1 uuid:='a3610000-0000-4000-8000-000000000044';
BEGIN
    -- Identifica tenants de demonstração e homologação
    SELECT id INTO t1 FROM plantaopro.tenants WHERE id='d3f6584c-2c64-4e5a-9ea9-4e1428647502'; 
    IF t1 IS NULL THEN RAISE EXCEPTION 'Tenant demo Santa Casa ausente'; END IF;
    
    SELECT id INTO t2 FROM plantaopro.tenants WHERE id='8b0c8e74-a81b-4ea2-b499-94755a1ca001';

    SELECT id INTO usr1 FROM plantaopro.usuarios WHERE tenant_id=t1 AND reg_status='A' ORDER BY reg_date LIMIT 1;
    IF t2 IS NOT NULL THEN
        SELECT id INTO usr2 FROM plantaopro.usuarios WHERE tenant_id=t2 AND reg_status='A' ORDER BY reg_date LIMIT 1;
    END IF;

    -- =========================================================================
    -- TENANT 1: SANTA CASA DEMONSTRAÇÃO
    -- =========================================================================

    -- Parceiros e Hospital
    INSERT INTO plantaopro.adm360_parceiros(id,tenant_id,nome,documento,fornecedor,ativo) 
    VALUES
        (fornecedor,t1,'Orto Demo Fornecimentos','11222333000181',true,true),
        (hospital_p,t1,'Hospital São Lucas Demonstração','44555666000199',false,true)
    ON CONFLICT(id) DO UPDATE SET ativo=true;

    -- Produtos
    INSERT INTO plantaopro.adm360_produtos(id,tenant_id,sku,nome,unidade,codigo_barras,controla_lote,exige_inspecao) 
    VALUES(produto,t1,'IMP-DEMO','Implante controlado demonstrativo','UN','7890000003602',true,true) 
    ON CONFLICT(id) DO NOTHING;

    -- Locais
    INSERT INTO plantaopro.adm360_locais(id,tenant_id,codigo,nome,tipo) 
    VALUES
        (local_cd,t1,'CD-DEMO','Centro de distribuição','INTERNO'),
        (local_hosp,t1,'HOSP-DEMO','Hospital Demo (custódia)','EXTERNO'),
        (local_inv,t1,'ALMOX-INV','Almoxarifado em Contagem de Inventário','INTERNO') 
    ON CONFLICT(id) DO NOTHING;

    -- Lotes: livre, quarentena, vencido e que vence antes da cirurgia (cirurgia prevista para 2026-11-20)
    INSERT INTO plantaopro.adm360_lotes(id,tenant_id,produto_id,codigo,validade) 
    VALUES
        (lote_livre,t1,produto,'DEMO-LIVRE',date '2027-09-24'),
        (lote_q,t1,produto,'DEMO-QUARENTENA',date '2027-06-30'),
        (lote_v,t1,produto,'DEMO-VENCIDO',date '2025-01-01'),
        (lote_vence_antes,t1,produto,'DEMO-VENCE-ANTES',date '2026-10-15')
    ON CONFLICT(id) DO NOTHING;

    -- Pedido de Compra e Recebimento
    INSERT INTO plantaopro.adm360_pedidos(id,tenant_id,numero,fornecedor_id,situacao,previsao,frete,aprovado_em,aprovado_por,created_by) 
    VALUES(pedido,t1,'PC-DEMO-001',fornecedor,'PARCIAL',date '2026-09-25',0,now(),usr1,usr1) 
    ON CONFLICT(id) DO NOTHING;

    INSERT INTO plantaopro.adm360_pedido_itens(id,tenant_id,pedido_id,produto_id,quantidade,quantidade_recebida,preco_unitario,desconto) 
    VALUES(item,t1,pedido,produto,20,10,100,0) 
    ON CONFLICT(id) DO NOTHING;

    INSERT INTO plantaopro.adm360_recebimentos(id,tenant_id,pedido_id,documento,idempotency_key,confirmado_em,created_by) 
    VALUES(receb,t1,pedido,'NF-DEMO-PARCIAL','seed:recebimento:1',now(),usr1) 
    ON CONFLICT(id) DO NOTHING;

    INSERT INTO plantaopro.adm360_recebimento_itens(id,tenant_id,recebimento_id,pedido_item_id,produto_id,lote_id,local_id,quantidade,condicao) 
    VALUES(ri,t1,receb,item,produto,lote_q,local_cd,10,'QUARENTENA') 
    ON CONFLICT(id) DO NOTHING;

    -- Estoques no mesmo local (CD-DEMO): LIBERADO, QUARENTENA, VENCIDO e LOTE QUE VENCE ANTES
    INSERT INTO plantaopro.adm360_movimentos(id,tenant_id,produto_id,lote_id,local_id,tipo,condicao,quantidade,origem_tipo,origem_id,idempotency_key,created_by) 
    VALUES
        ('a3610000-0000-4000-8000-000000000012',t1,produto,lote_q,local_cd,'ENTRADA','QUARENTENA',10,'RECEBIMENTO',ri,'seed:movimento:q',usr1),
        ('a3610000-0000-4000-8000-000000000013',t1,produto,lote_livre,local_cd,'ENTRADA','LIBERADO',15,'SEED',lote_livre,'seed:movimento:livre',usr1),
        ('a3610000-0000-4000-8000-000000000014',t1,produto,lote_v,local_cd,'ENTRADA','VENCIDO',2,'SEED',lote_v,'seed:movimento:vencido',usr1),
        ('a3610000-0000-4000-8000-000000000026',t1,produto,lote_vence_antes,local_cd,'ENTRADA','LIBERADO',5,'SEED',lote_vence_antes,'seed:movimento:vence_antes',usr1)
    ON CONFLICT(id) DO NOTHING;

    -- Ocorrência
    INSERT INTO plantaopro.adm360_ocorrencias(id,tenant_id,recebimento_item_id,lote_id,produto_id,local_id,tipo,descricao,quantidade,situacao,responsavel_id,prazo) 
    VALUES('a3610000-0000-4000-8000-000000000015',t1,ri,lote_q,produto,local_cd,'DOCUMENTACAO','Divergência documental demonstrativa',2,'ABERTA',usr1,date '2026-09-30') 
    ON CONFLICT(id) DO NOTHING;

    -- Inventário ativo somente no local_inv (garante que local_cd não fica bloqueado acidentalmente)
    DELETE FROM plantaopro.adm360_inventarios WHERE id='a3610000-0000-4000-8000-000000000016';
    INSERT INTO plantaopro.adm360_inventarios(id,tenant_id,local_id,situacao,escopo,created_by) 
    VALUES('a3610000-0000-4000-8000-000000000016',t1,local_inv,'CONTAGEM','Implantes controlados em conferência',usr1) 
    ON CONFLICT(id) DO UPDATE SET local_id=local_inv, situacao='CONTAGEM';

    -- Tarefas de coleta
    INSERT INTO plantaopro.adm360_tarefas_coleta(id,tenant_id,tipo,descricao,situacao,atribuida_a,origem_id) 
    VALUES
        ('a3610000-0000-4000-8000-000000000017',t1,'INVENTARIO','Contar almoxarifado em inventário','ABERTA',usr1,'a3610000-0000-4000-8000-000000000016'),
        ('a3610000-0000-4000-8000-000000000018',t1,'SEPARACAO','Separação interna demonstrativa','ABERTA',usr1,lote_livre) 
    ON CONFLICT(id) DO NOTHING;

    -- Orçamento Cirúrgico 1: RASCUNHO (cirurgia em 2026-11-20)
    INSERT INTO plantaopro.adm360_orcamentos(
        id, tenant_id, numero, revisao, hospital_id, procedimento, responsavel_financeiro_id,
        data_prevista, validade, situacao, total_produtos, desconto_geral, total_geral, observacoes, created_by
    ) VALUES (
        orc_rascunho, t1, 'ORC-DEMO-001', 1, hospital_p, 'Artroplastia Total de Quadril Direita', hospital_p,
        date '2026-11-20', date '2026-11-05', 'RASCUNHO', 1200.00, 0, 1200.00, 'Orçamento em fase de elaboração e cotação', usr1
    ) ON CONFLICT(id) DO NOTHING;

    INSERT INTO plantaopro.adm360_orcamento_itens(
        id, tenant_id, orcamento_id, produto_id, quantidade, preco_unitario, desconto, total
    ) VALUES (
        orc_item_1, t1, orc_rascunho, produto, 2, 600.00, 0, 1200.00
    ) ON CONFLICT(id) DO NOTHING;

    -- Orçamento Cirúrgico 2: APROVADO com Reserva Parcial
    INSERT INTO plantaopro.adm360_orcamentos(
        id, tenant_id, numero, revisao, hospital_id, procedimento, responsavel_financeiro_id,
        data_prevista, validade, situacao, total_produtos, desconto_geral, total_geral, observacoes, aprovado_em, aprovado_por, created_by
    ) VALUES (
        orc_aprovado, t1, 'ORC-DEMO-002', 1, hospital_p, 'Reconstrução Ligamentar Joelho Esquerdo', hospital_p,
        date '2026-11-20', date '2026-11-10', 'APROVADO', 2500.00, 100.00, 2400.00, 'Orçamento aprovado pelo convênio / hospital', now(), usr1, usr1
    ) ON CONFLICT(id) DO NOTHING;

    INSERT INTO plantaopro.adm360_orcamento_itens(
        id, tenant_id, orcamento_id, produto_id, quantidade, preco_unitario, desconto, total
    ) VALUES (
        orc_item_2, t1, orc_aprovado, produto, 5, 500.00, 100.00, 2400.00
    ) ON CONFLICT(id) DO NOTHING;

    -- Reserva parcial: solicitados 5, reservados 2 de lote_livre no local_cd (restam 3 a atender)
    INSERT INTO plantaopro.adm360_reservas(
        id, tenant_id, produto_id, lote_id, local_id, quantidade, situacao, origem_tipo, origem_id, orcamento_item_id, idempotency_key, created_by
    ) VALUES (
        reserva_1, t1, produto, lote_livre, local_cd, 2, 'ATIVA', 'ORCAMENTO_CIRURGICO', orc_aprovado, orc_item_2, 'seed:reserva:orc2:item2', usr1
    ) ON CONFLICT(id) DO NOTHING;

    -- =========================================================================
    -- CENÁRIO DEMONSTRATIVO SEÇÃO 15:
    -- 1) Uma cirurgia, Orçamento aprovado, Lote com 10 liberadas, Reserva de 6, Vale em preparação.
    -- 2) Cenário concluído e reconciliado para consulta de relatórios.
    -- =========================================================================
    DECLARE
        lote_10 uuid := 'a3610000-0000-4000-8000-000000000050';
        orc_demo_10 uuid := 'a3610000-0000-4000-8000-000000000051';
        item_demo_10 uuid := 'a3610000-0000-4000-8000-000000000052';
        reserva_6 uuid := 'a3610000-0000-4000-8000-000000000053';
        cirurgia_1 uuid := 'a3610000-0000-4000-8000-000000000054';
        vale_1 uuid := 'a3610000-0000-4000-8000-000000000055';
        vale_item_1 uuid := 'a3610000-0000-4000-8000-000000000056';

        -- Cenário 2 (Reconciliado)
        cirurgia_rec uuid := 'a3610000-0000-4000-8000-000000000060';
        vale_rec uuid := 'a3610000-0000-4000-8000-000000000061';
        vale_rec_item uuid := 'a3610000-0000-4000-8000-000000000062';
        op_rec uuid := 'a3610000-0000-4000-8000-000000000063';

        -- Cenário 3 (Pendente com Atraso para Relatórios)
        cirurgia_pend uuid := 'a3610000-0000-4000-8000-000000000070';
        vale_pend uuid := 'a3610000-0000-4000-8000-000000000071';
        vale_pend_item uuid := 'a3610000-0000-4000-8000-000000000072';
    BEGIN
        -- Lote com 10 liberadas
        INSERT INTO plantaopro.adm360_lotes(id, tenant_id, produto_id, codigo, validade)
        VALUES(lote_10, t1, produto, 'LOTE-DEMO-10UN', date '2027-12-31')
        ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_movimentos(id, tenant_id, produto_id, lote_id, local_id, tipo, condicao, quantidade, origem_tipo, origem_id, idempotency_key, created_by)
        VALUES('a3610000-0000-4000-8000-000000000057', t1, produto, lote_10, local_cd, 'ENTRADA', 'LIBERADO', 10, 'SEED', lote_10, 'seed:movimento:lote10', usr1)
        ON CONFLICT(id) DO NOTHING;

        -- Orçamento aprovado de 10 unidades
        INSERT INTO plantaopro.adm360_orcamentos(
            id, tenant_id, numero, revisao, hospital_id, procedimento, responsavel_financeiro_id,
            data_prevista, validade, situacao, total_produtos, desconto_geral, total_geral, observacoes, aprovado_em, aprovado_por, created_by
        ) VALUES (
            orc_demo_10, t1, 'ORC-DEMO-010', 1, hospital_p, 'Artroplastia de Joelho Bilateral', hospital_p,
            date '2026-11-25', date '2026-11-15', 'APROVADO', 6000.00, 0, 6000.00, 'Orçamento aprovado para jornada de consignação', now(), usr1, usr1
        ) ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_orcamento_itens(id, tenant_id, orcamento_id, produto_id, quantidade, preco_unitario, desconto, total)
        VALUES(item_demo_10, t1, orc_demo_10, produto, 10, 600.00, 0, 6000.00)
        ON CONFLICT(id) DO NOTHING;

        -- Reserva de 6 unidades
        INSERT INTO plantaopro.adm360_reservas(
            id, tenant_id, produto_id, lote_id, local_id, quantidade, situacao, origem_tipo, origem_id, orcamento_item_id, idempotency_key, created_by
        ) VALUES (
            reserva_6, t1, produto, lote_10, local_cd, 6, 'ATIVA', 'ORCAMENTO_CIRURGICO', orc_demo_10, item_demo_10, 'seed:reserva:demo:6', usr1
        ) ON CONFLICT(id) DO NOTHING;

        -- Cirurgia 1
        INSERT INTO plantaopro.adm360_cirurgias(
            id, tenant_id, numero, hospital_id, procedimento, data_prevista, hora_prevista,
            orcamento_id, orcamento_revisao, local_destino_id, situacao, observacoes, created_by
        ) VALUES (
            cirurgia_1, t1, 'CIR-DEMO-001', hospital_p, 'Artroplastia de Joelho Bilateral', date '2026-11-25', time '08:00',
            orc_demo_10, 1, local_hosp, 'AGENDADA', 'Cirurgia demonstrativa com vale em preparação', usr1
        ) ON CONFLICT(id) DO NOTHING;

        -- Vale 1 em preparação (EM_SEPARACAO)
        INSERT INTO plantaopro.adm360_vales(
            id, tenant_id, numero, cirurgia_id, orcamento_id, orcamento_revisao, hospital_id,
            local_origem_id, local_destino_id, data_saida_prevista, data_retorno_prevista,
            situacao, situacao_financeira, observacoes, created_by
        ) VALUES (
            vale_1, t1, 'VAL-DEMO-001', cirurgia_1, orc_demo_10, 1, hospital_p,
            local_cd, local_hosp, date '2026-11-24', date '2026-11-28',
            'EM_SEPARACAO', 'PENDENTE_VALORIZACAO', 'Vale em fase de separação no almoxarifado', usr1
        ) ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_vale_itens(
            id, tenant_id, vale_id, produto_id, lote_id, reserva_id, quantidade_solicitada, quantidade_separada, preco_unitario
        ) VALUES (
            vale_item_1, t1, vale_1, produto, lote_10, reserva_6, 6, 3, 600.00
        ) ON CONFLICT(id) DO NOTHING;

        -- Cenário 2: Vale Reconciliado (Expedido 6 = Consumido 4 + Devolvido 2, pendente 0)
        INSERT INTO plantaopro.adm360_cirurgias(
            id, tenant_id, numero, hospital_id, procedimento, data_prevista, hora_prevista,
            local_destino_id, situacao, observacoes, created_by
        ) VALUES (
            cirurgia_rec, t1, 'CIR-DEMO-REC', hospital_p, 'Cirurgia Concluída Reconciliada', date '2026-09-10', time '09:00',
            local_hosp, 'REALIZADA', 'Cirurgia concluída com sucesso e vale reconciliado', usr1
        ) ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_vales(
            id, tenant_id, numero, cirurgia_id, hospital_id, local_origem_id, local_destino_id,
            data_saida_prevista, data_saida_efetiva, data_retorno_prevista, data_reconciliacao,
            situacao, situacao_financeira, observacoes, created_by
        ) VALUES (
            vale_rec, t1, 'VAL-DEMO-REC', cirurgia_rec, hospital_p, local_cd, local_hosp,
            date '2026-09-09', now() - interval '14 days', date '2026-09-15', now() - interval '10 days',
            'RECONCILIADO', 'PENDENTE_VALORIZACAO', 'Vale reconciliado: 4 consumidos, 2 devolvidos', usr1
        ) ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_vale_itens(
            id, tenant_id, vale_id, produto_id, lote_id, quantidade_solicitada, quantidade_separada,
            quantidade_expedida, quantidade_consumida, quantidade_devolvida, quantidade_perda, preco_unitario
        ) VALUES (
            vale_rec_item, t1, vale_rec, produto, lote_livre, 6, 6, 6, 4, 2, 0, 500.00
        ) ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_vale_eventos(id, tenant_id, vale_id, vale_item_id, tipo, quantidade, data_evento, motivo, registrado_por)
        VALUES
            ('a3610000-0000-4000-8000-000000000064', t1, vale_rec, vale_rec_item, 'CONSUMO', 4, now() - interval '12 days', 'Implantes fixados no paciente', usr1),
            ('a3610000-0000-4000-8000-000000000065', t1, vale_rec, vale_rec_item, 'RETORNO', 2, now() - interval '11 days', 'Sobra cirúrgica íntegra devolvida para quarentena', usr1)
        ON CONFLICT(id) DO NOTHING;

        -- Cenário 3: Vale com atraso no retorno previsto (para relatórios de pendência)
        INSERT INTO plantaopro.adm360_cirurgias(
            id, tenant_id, numero, hospital_id, procedimento, data_prevista, hora_prevista,
            local_destino_id, situacao, observacoes, created_by
        ) VALUES (
            cirurgia_pend, t1, 'CIR-DEMO-ATRASO', hospital_p, 'Cirurgia Emergencial Ortopédica', date '2026-09-01', time '14:00',
            local_hosp, 'REALIZADA', 'Cirurgia realizada, aguardando devolução de materiais', usr1
        ) ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_vales(
            id, tenant_id, numero, cirurgia_id, hospital_id, local_origem_id, local_destino_id,
            data_saida_prevista, data_saida_efetiva, data_retorno_prevista,
            situacao, situacao_financeira, observacoes, created_by
        ) VALUES (
            vale_pend, t1, 'VAL-DEMO-ATRASO', cirurgia_pend, hospital_p, local_cd, local_hosp,
            date '2026-08-31', now() - interval '20 days', date '2026-09-05',
            'EXPEDIDO', 'PENDENTE_VALORIZACAO', 'Materiais pendentes de recolhimento no hospital', usr1
        ) ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_vale_itens(
            id, tenant_id, vale_id, produto_id, lote_id, quantidade_solicitada, quantidade_separada,
            quantidade_expedida, quantidade_consumida, quantidade_devolvida, quantidade_perda, preco_unitario
        ) VALUES (
            vale_pend_item, t1, vale_pend, produto, lote_livre, 5, 5, 5, 2, 0, 0, 500.00
        ) ON CONFLICT(id) DO NOTHING;

        -- =========================================================================
        -- CONTAS FINANCEIRAS DEMONSTRATIVAS
        -- =========================================================================
        INSERT INTO plantaopro.adm360_contas_financeiras(
            id, tenant_id, nome, tipo, banco, agencia, conta, saldo_inicial, ativo, created_at
        ) VALUES
            ('a3610000-0000-4000-8000-000000000080', t1, 'Banco do Brasil - Conta Movimento', 'BANCO', '001 - BB', '1234-5', '98765-4', 10000.00, true, now()),
            ('a3610000-0000-4000-8000-000000000081', t1, 'Caixa Físico Tesouraria', 'CAIXA', NULL, NULL, NULL, 500.00, true, now())
        ON CONFLICT(id) DO NOTHING;

        -- Movimento financeiro demonstrativo
        INSERT INTO plantaopro.adm360_movimentos_financeiros(
            id, tenant_id, conta_id, tipo, valor, data_movimento, descricao, origem_tipo, origem_id, idempotency_key
        ) VALUES
            ('a3610000-0000-4000-8000-000000000082', t1, 'a3610000-0000-4000-8000-000000000080', 'ENTRADA', 300.00, date '2026-09-20', 'Recebimento demonstrativo', 'RECEBIMENTO_TITULO', 'a3610000-0000-4000-8000-000000000080', 'seed:movimento:1')
        ON CONFLICT(id) DO NOTHING;
    END;

    -- =========================================================================
    -- TENANT 2: TESTE DE ISOLAMENTO MULTI-TENANT
    -- =========================================================================
    IF t2 IS NOT NULL THEN
        -- Parceiro e produto do Tenant 2
        INSERT INTO plantaopro.adm360_parceiros(id,tenant_id,nome,documento,fornecedor,ativo) 
        VALUES('a3620000-0000-4000-8000-000000000001', t2, 'Fornecedor Tenant 2 Isolado', '99888777000155', true, true) 
        ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_produtos(id,tenant_id,sku,nome,unidade,codigo_barras,controla_lote,exige_inspecao) 
        VALUES('a3620000-0000-4000-8000-000000000002', t2, 'PROD-T2-ISOLADO', 'Material Específico do Tenant 2', 'UN', '7899999990022', true, false) 
        ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_locais(id,tenant_id,codigo,nome,tipo) 
        VALUES('a3620000-0000-4000-8000-000000000003', t2, 'CD-TENANT2', 'Depósito Central Tenant 2', 'INTERNO') 
        ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_orcamentos(
            id, tenant_id, numero, revisao, hospital_id, procedimento, responsavel_financeiro_id,
            data_prevista, validade, situacao, total_produtos, desconto_geral, total_geral, observacoes, created_by
        ) VALUES (
            'a3620000-0000-4000-8000-000000000040', t2, 'ORC-T2-001', 1, 'a3620000-0000-4000-8000-000000000001',
            'Cirurgia Privada Tenant 2', 'a3620000-0000-4000-8000-000000000001', date '2026-12-01', date '2026-11-25',
            'RASCUNHO', 800.00, 0, 800.00, 'Orçamento isolado do Tenant 2', usr2
        ) ON CONFLICT(id) DO NOTHING;
    END IF;

END $$;
