-- ============================================================================
-- PlantãoPro - Script Único de Demonstração e Homologação
-- Módulo: Administrativo 360 (Central de Cotações, XML Recebidos, Dashboard)
-- Compatibilidade: PostgreSQL 14+ / Ferramenta pgAdmin Query Tool
-- NOTA: Este script é estritamente transacional, idempotente e não utiliza
--       comandos específicos do cliente psql (como \i ou \set).
-- ============================================================================

DO $DEMO_BLOCK$
DECLARE
    v_tenant_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647502';
    v_user_id   uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647511';
    v_user_consulta_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647512';
    v_tenant2_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647599';
    v_estab_matriz_id uuid := 'a3600000-0000-4000-8000-000000000010';
    v_estab_filial_id uuid := 'a3600000-0000-4000-8000-000000000011';
    v_conta_opme_id uuid := 'a3600000-0000-4000-8000-000000000020';
    v_conta_inpart_id uuid := 'a3600000-0000-4000-8000-000000000021';
    v_prod1_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647520';
    v_prod2_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647521';
    v_hospital_parceiro_id uuid := 'a3600000-0000-4000-8000-000000000025';
    v_pagador_parceiro_id uuid := 'a3600000-0000-4000-8000-000000000026';
    v_fornecedor_parceiro_id uuid := 'a3600000-0000-4000-8000-000000000027';
    v_cotacao1_id uuid := 'a3600000-0000-4000-8000-000000000030';
    v_cotacao2_id uuid := 'a3600000-0000-4000-8000-000000000031';
    v_cotacao3_id uuid := 'a3600000-0000-4000-8000-000000000032';
    v_cotacao4_id uuid := 'a3600000-0000-4000-8000-000000000033';
    v_orcamento1_id uuid := 'a3600000-0000-4000-8000-000000000040';
    v_doc1_id uuid := 'a3600000-0000-4000-8000-000000000050';
    v_doc_quarentena_id uuid := 'a3600000-0000-4000-8000-000000000051';
    v_modulo_adm360_id uuid := 'a3600000-0000-4000-8000-000000000099';
    v_acao_acessar_id uuid;
    v_perfil_adm_id uuid;
    v_perm_code text;
    v_perm_id uuid;
    v_now timestamp with time zone := clock_timestamp();
    v_perms text[] := ARRAY[
        'ADM360:VER',
        'ADM360:CONFIGURAR_INTEGRACAO',
        'ADM360:COTACAO_CONSULTAR',
        'ADM360:MAPEAR_CADASTROS',
        'ADM360:MAPEAR_PRODUTOS',
        'ADM360:ELABORAR_ORCAMENTO',
        'ADM360:APROVAR_RESPOSTA',
        'ADM360:TRANSMITIR_RESPOSTA',
        'ADM360:CONSULTAR_ANEXOS',
        'ADM360:IMPORTAR_XML',
        'ADM360:MANIFESTAR_DFE',
        'ADM360:VINCULAR_DOCUMENTOS',
        'ADM360:EXPORTAR',
        'ADM360:AUDITAR'
    ];
BEGIN
    -- 1. VALIDAÇÃO DE SCHEMA E PRÉ-REQUISITOS
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_schema = 'plantaopro' AND table_name = 'adm360_cotacoes'
    ) THEN
        RAISE EXCEPTION 'A migration v2196 (adm360_cotacoes) não foi aplicada. Execute as migrations antes deste script.';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_schema = 'plantaopro' AND table_name = 'adm360_documentos_recebidos'
    ) THEN
        RAISE EXCEPTION 'A migration v2196 (adm360_documentos_recebidos) não foi aplicada.';
    END IF;

    -- 2. GARANTIR TENANT E USUÁRIO DEMONSTRATIVO (SANTA CASA DEMONSTRAÇÃO)
    INSERT INTO plantaopro.tenants (id, codigo, nome, status, dados, criado_em)
    VALUES (v_tenant_id, 'SANTACASA-DEMO', 'Santa Casa Demonstração', 'ATIVO', '{}'::jsonb, v_now)
    ON CONFLICT (id) DO UPDATE SET nome = excluded.nome, status = 'ATIVO';

    -- Segundo tenant isolado para testes de não-vazamento / isolamento multi-tenant
    INSERT INTO plantaopro.tenants (id, codigo, nome, status, dados, criado_em)
    VALUES (v_tenant2_id, 'SAOLUCAS-NEGATIVO', 'Hospital São Lucas (Tenant Negativo)', 'ATIVO', '{}'::jsonb, v_now)
    ON CONFLICT (id) DO UPDATE SET nome = excluded.nome, status = 'ATIVO';

    -- Módulo Administrativo 360 no catálogo do sistema
    INSERT INTO plantaopro.modulos_sistema (id, codigo, nome, status, reg_status, reg_date)
    VALUES (v_modulo_adm360_id, 'ADM360', 'Administrativo 360', 'ATIVO', 'A', v_now)
    ON CONFLICT (id) DO NOTHING;
    SELECT id INTO v_modulo_adm360_id FROM plantaopro.modulos_sistema
    WHERE upper(btrim(codigo))='ADM360' AND reg_status='A' ORDER BY id LIMIT 1;

    -- Contratação pelo contrato canônico (v2197), compartilhado por login,
    -- autorização, menu e serviço comercial.
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='plantaopro' AND table_name='tenant_modulos' AND column_name='modulo_id') THEN
        RAISE EXCEPTION 'Contrato canônico de módulos ausente. Aplique a migration v2197 antes deste script.';
    END IF;
    INSERT INTO plantaopro.tenant_modulos
        (id,tenant_id,modulo_id,codigo,codigo_modulo,habilitado,status,origem,ativado_em,reg_date,reg_status)
    VALUES(gen_random_uuid(),v_tenant_id,v_modulo_adm360_id,'ADM360','ADM360',true,'ATIVO','SEED_DEMO',v_now,v_now,'A')
    ON CONFLICT (tenant_id,modulo_id) WHERE reg_status='A' AND modulo_id IS NOT NULL
    DO UPDATE SET codigo='ADM360',codigo_modulo='ADM360',habilitado=true,status='ATIVO',reg_update=v_now;

    -- Obter ou criar ação padrão para permissões
    SELECT id INTO v_acao_acessar_id FROM plantaopro.acoes_sistema WHERE codigo = 'ACESSAR' OR codigo = 'LISTAR' LIMIT 1;
    IF v_acao_acessar_id IS NULL THEN
        v_acao_acessar_id := gen_random_uuid();
        INSERT INTO plantaopro.acoes_sistema (id, codigo, nome, status, reg_status, reg_date)
        VALUES (v_acao_acessar_id, 'ACESSAR', 'Acessar', 'ATIVO', 'A', v_now);
    END IF;

    -- Garantir Perfil de Administrador do Cliente no Tenant
    SELECT id INTO v_perfil_adm_id 
    FROM plantaopro.perfis 
    WHERE (tenant_id = v_tenant_id OR tenant_id IS NULL) 
      AND (codigo = 'ADMINISTRADOR_CLIENTE' OR codigo = 'ADMIN_CLIENTE')
      AND reg_status = 'A'
    ORDER BY (tenant_id = v_tenant_id) DESC LIMIT 1;

    IF v_perfil_adm_id IS NULL THEN
        v_perfil_adm_id := gen_random_uuid();
        INSERT INTO plantaopro.perfis (id, tenant_id, codigo, nome, descricao, base_sistema, customizado, status, reg_status, reg_date)
        VALUES (v_perfil_adm_id, v_tenant_id, 'ADMINISTRADOR_CLIENTE', 'Administrador do Cliente', 'Perfil do Gestor Santa Casa', true, false, 'ATIVO', 'A', v_now);
    END IF;

    -- Cadastrar Permissões ADM360 e Vincular ao Perfil
    FOREACH v_perm_code IN ARRAY v_perms LOOP
        SELECT id INTO v_perm_id FROM plantaopro.permissoes WHERE lower(codigo) = lower(v_perm_code) AND reg_status = 'A';
        IF v_perm_id IS NULL THEN
            v_perm_id := gen_random_uuid();
            INSERT INTO plantaopro.permissoes (id, nome, modulo_id, acao_id, codigo, status, reg_status, reg_date)
            VALUES (v_perm_id, v_perm_code, v_modulo_adm360_id, v_acao_acessar_id, v_perm_code, 'ATIVO', 'A', v_now);
        END IF;

        INSERT INTO plantaopro.perfil_permissoes (id, perfil_id, permissao_id, permitido, reg_status, reg_date)
        VALUES (gen_random_uuid(), v_perfil_adm_id, v_perm_id, true, 'A', v_now)
        ON CONFLICT (perfil_id, permissao_id) WHERE reg_status = 'A' DO UPDATE SET permitido = true;
    END LOOP;

    -- Garantir usuário gestor demonstrativo
    -- Senha inicial: SantaCasa!Demo2026#Gestor ($2a$11$mNmgw83PBauw.5XsFVB5TuMB1.8OgFZ0SpfujQSuGzUgzwvs7d8S.)
    IF NOT EXISTS (SELECT 1 FROM plantaopro.usuarios WHERE lower(email) = 'gestor@santacasa-demo.example') THEN
        INSERT INTO plantaopro.usuarios (
            id, tenant_id, nome, email, email_normalizado, senha_hash, status, reg_status, senha_alteracao_obrigatoria, reg_date
        ) VALUES (
            v_user_id, v_tenant_id, 'Gestor Santa Casa — Demonstração',
            'gestor@santacasa-demo.example', 'gestor@santacasa-demo.example',
            '$2a$11$mNmgw83PBauw.5XsFVB5TuMB1.8OgFZ0SpfujQSuGzUgzwvs7d8S.',
            'ATIVO', 'A', false, v_now
        );
    ELSE
        -- Preserva senha se já foi alterada, atualiza vínculo e status
        UPDATE plantaopro.usuarios 
        SET tenant_id = v_tenant_id, status = 'ATIVO', reg_status = 'A'
        WHERE lower(email) = 'gestor@santacasa-demo.example';
    END IF;

    -- Vincular Gestor ao Perfil ADMINISTRADOR_CLIENTE
    INSERT INTO plantaopro.usuarios_perfis (id, tenant_id, usuario_id, perfil_id, reg_status, reg_date)
    VALUES (gen_random_uuid(), v_tenant_id, v_user_id, v_perfil_adm_id, 'A', v_now)
    ON CONFLICT (usuario_id, perfil_id) WHERE reg_status = 'A' DO NOTHING;

    -- Usuário de consulta restrita para testes de autorização
    IF NOT EXISTS (SELECT 1 FROM plantaopro.usuarios WHERE lower(email) = 'consulta@santacasa-demo.example') THEN
        INSERT INTO plantaopro.usuarios (
            id, tenant_id, nome, email, email_normalizado, senha_hash, status, reg_status, senha_alteracao_obrigatoria, reg_date
        ) VALUES (
            v_user_consulta_id, v_tenant_id, 'Auditor Restrito — Demonstração',
            'consulta@santacasa-demo.example', 'consulta@santacasa-demo.example',
            '$2a$11$mNmgw83PBauw.5XsFVB5TuMB1.8OgFZ0SpfujQSuGzUgzwvs7d8S.',
            'ATIVO', 'A', false, v_now
        );
    END IF;

    -- 3. HABILITAR CAPACIDADES CONTRATADAS NO ADMINISTRATIVO 360
    INSERT INTO plantaopro.adm360_capacidades_contratadas (id, tenant_id, capacidade, habilitado, ativado_em, configuracoes)
    VALUES 
        (gen_random_uuid(), v_tenant_id, 'PORTAIS_COTACAO', true, v_now, '{"provedores":["OPMENEXO","INPART"]}'::jsonb),
        (gen_random_uuid(), v_tenant_id, 'XML_RECEBIDOS', true, v_now, '{"modelos":["55"]}'::jsonb),
        (gen_random_uuid(), v_tenant_id, 'DASHBOARD_GERENCIAL', true, v_now, '{"atualizacao":"TEMPO_REAL"}'::jsonb)
    ON CONFLICT (tenant_id, capacidade) DO UPDATE 
        SET habilitado = true;

    -- 4. ESTABELECIMENTOS AUTORIZADOS (CNPJs DA SANTA CASA)
    INSERT INTO plantaopro.adm360_estabelecimentos (
        id, tenant_id, cnpj, razao_social, nome_fantasia, inscricao_estadual, cnae, ambiente, ativo, created_at
    ) VALUES 
        (v_estab_matriz_id, v_tenant_id, '46389044000130', 'Irmandade da Santa Casa de Misericórdia de Demonstração', 'Santa Casa Matriz', '123456789', '8610101', 'HOMOLOGACAO', true, v_now),
        (v_estab_filial_id, v_tenant_id, '46389044000213', 'Irmandade da Santa Casa - Unidade Cirúrgica Avançada', 'Santa Casa Filial', '987654321', '8610101', 'HOMOLOGACAO', true, v_now)
    ON CONFLICT (tenant_id, cnpj) DO UPDATE 
        SET razao_social = excluded.razao_social, ativo = true;

    -- 5. PARCEIROS (HOSPITAL, PAGADOR E FORNECEDOR)
    INSERT INTO plantaopro.adm360_parceiros (id, tenant_id, nome, documento, fornecedor, ativo, created_at)
    VALUES 
        (v_hospital_parceiro_id, v_tenant_id, 'Hospital Regional Parceiro', '12345678000199', false, true, v_now),
        (v_pagador_parceiro_id, v_tenant_id, 'Unimed Seguros Saúde', '98765432000188', false, true, v_now),
        (v_fornecedor_parceiro_id, v_tenant_id, 'Ortopedia & Cirurgia Distribuidora Ltda', '11222333000144', true, true, v_now)
    ON CONFLICT (id) DO UPDATE SET nome = excluded.nome;

    -- 6. CONTAS DE PORTAIS DE COTAÇÃO (OPMENEXO E INPART SAÚDE)
    INSERT INTO plantaopro.adm360_portal_contas (
        id, tenant_id, estabelecimento_id, provedor, nome_conta, identificador_externo, usuario_acesso, ambiente, status_integracao, motivo_bloqueio, ativo, created_at
    ) VALUES 
        (v_conta_opme_id, v_tenant_id, v_estab_matriz_id, 'OPMENEXO', 'Opmenexo Matriz Cirúrgica', 'OPME-CLI-84920', 'integracao@santacasa-demo.example', 'HOMOLOGACAO', 'CONFIGURADA', NULL, true, v_now),
        (v_conta_inpart_id, v_tenant_id, v_estab_matriz_id, 'INPART', 'INPART Saúde - Santa Casa', 'INPART-8831', 'api.inpart@santacasa-demo.example', 'HOMOLOGACAO', 'BLOQUEADA', 'Aguardando liberação de credenciais oficiais e endpoint de homologação da INPART Saúde', true, v_now)
    ON CONFLICT (tenant_id, provedor, identificador_externo) DO UPDATE 
        SET status_integracao = excluded.status_integracao, motivo_bloqueio = excluded.motivo_bloqueio;

    -- 7. CADASTRO DE PRODUTOS INTERNOS DEMONSTRATIVOS
    INSERT INTO plantaopro.adm360_produtos (
        id, tenant_id, sku, nome, unidade, preco_custo, ativo, created_at
    ) VALUES 
        (v_prod1_id, v_tenant_id, 'STENT-DES-001', 'Stent Coronário Farmacológico de Sirolimus', 'UN', 2500.00, true, v_now),
        (v_prod2_id, v_tenant_id, 'GUIA-ANGIO-002', 'Fio Guia Hidrofílico Angioplastia 0.014"', 'UN', 450.00, true, v_now)
    ON CONFLICT (id) DO UPDATE SET nome = excluded.nome;

    -- 8. MAPEAMENTOS DE/PARA PERSISTIDOS
    INSERT INTO plantaopro.adm360_mapeamentos_de_para (
        id, tenant_id, portal_conta_id, provedor, tipo_entidade, codigo_externo, descricao_externa, entidade_interna_id, entidade_interna_descricao, fator_conversao, situacao, created_at
    ) VALUES 
        (gen_random_uuid(), v_tenant_id, v_conta_opme_id, 'OPMENEXO', 'PRODUTO', 'OPME-MAT-991', 'Stent Sirolimus 3.0x18mm', v_prod1_id, 'Stent Coronário Farmacológico de Sirolimus', 1.0000, 'ATIVO', v_now),
        (gen_random_uuid(), v_tenant_id, v_conta_opme_id, 'OPMENEXO', 'UNIDADE_MEDIDA', 'CX-10', 'Caixa com 10 unidades', NULL, 'UN', 10.0000, 'ATIVO', v_now),
        (gen_random_uuid(), v_tenant_id, v_conta_opme_id, 'OPMENEXO', 'HOSPITAL', 'HOSP-EXT-01', 'Hospital Geral Regional Solicitante', v_hospital_parceiro_id, 'Hospital Regional Parceiro', 1.0000, 'ATIVO', v_now)
    ON CONFLICT (tenant_id, provedor, tipo_entidade, codigo_externo) DO NOTHING;

    -- 9. ORÇAMENTO CIRÚRGICO VINCULADO EXISTENTE (CANÔNICO)
    INSERT INTO plantaopro.adm360_orcamentos (
        id, tenant_id, numero, revisao, hospital_id, procedimento, responsavel_financeiro_id, data_prevista, validade, situacao, total_produtos, total_geral, created_at, created_by
    ) VALUES (
        v_orcamento1_id, v_tenant_id, 'ORC-2026-0001', 1, v_hospital_parceiro_id, 'Angioplastia Coronária com Implante de Stent', v_pagador_parceiro_id, CURRENT_DATE + 5, CURRENT_DATE + 30, 'APROVADO', 5350.00, 5350.00, v_now, v_user_id
    ) ON CONFLICT (tenant_id, numero, revisao) DO NOTHING;

    -- 10. COTAÇÕES DEMONSTRATIVAS COM ORIGEM 'DADOS_DE_TESTE'
    -- Cotação 1: Recebida via OPMENEXO, com itens mapeados e orçamento vinculado
    INSERT INTO plantaopro.adm360_cotacoes (
        id, tenant_id, estabelecimento_id, portal_conta_id, provedor, identificador_externo, revisao_externa, hospital_solicitante_externo, paciente_iniciais, procedimento, data_prevista, prazo_resposta, fuso_horario, status_interno, status_externo, origem, orcamento_id, capturada_em, payload_original
    ) VALUES (
        v_cotacao1_id, v_tenant_id, v_estab_matriz_id, v_conta_opme_id, 'OPMENEXO', 'COT-2026-OPME-001', 1, 'Hospital Geral Regional', 'J.S.M.', 'Angioplastia Coronária', CURRENT_DATE + 5, v_now + interval '48 hours', 'America/Sao_Paulo', 'PRONTA_PARA_ENVIO', 'RECEBIDA', 'DADOS_DE_TESTE', v_orcamento1_id, v_now, '{"simulado":true,"cotacao":"COT-2026-OPME-001"}'
    ) ON CONFLICT (tenant_id, portal_conta_id, identificador_externo, revisao_externa) DO NOTHING;

    INSERT INTO plantaopro.adm360_cotacao_itens (
        id, tenant_id, cotacao_id, numero_item, codigo_externo, descricao_externa, fabricante_externo, modelo_externo, quantidade_solicitada, unidade_solicitada, produto_id, unidade_interna, fator_conversao, quantidade_convertida, preco_unitario_ofertado, desconto, preco_total_ofertado, status_relacionamento
    ) VALUES 
        (gen_random_uuid(), v_tenant_id, v_cotacao1_id, 1, 'OPME-MAT-991', 'Stent Sirolimus 3.0x18mm', 'Biotronik', 'Orsiro', 1, 'UN', v_prod1_id, 'UN', 1.0000, 1, 4500.00, 0.00, 4500.00, 'RELACIONADO'),
        (gen_random_uuid(), v_tenant_id, v_cotacao1_id, 2, 'OPME-MAT-992', 'Fio Guia Hidrofilico 0.014', 'Asahi', 'Sion', 1, 'UN', v_prod2_id, 'UN', 1.0000, 1, 850.00, 0.00, 850.00, 'RELACIONADO')
    ON CONFLICT (tenant_id, cotacao_id, numero_item) DO NOTHING;

    -- Resposta Outbox da Cotação 1
    INSERT INTO plantaopro.adm360_cotacao_respostas (
        id, tenant_id, cotacao_id, orcamento_id, revisao, snapshot_proposta, status_transmissao, status_comercial_externo, tentativas, proxima_tentativa, protocolo_externo, mensagem_retorno, aprovado_por, aprovado_em, created_at
    ) VALUES (
        gen_random_uuid(), v_tenant_id, v_cotacao1_id, v_orcamento1_id, 1, '{"total":5350.00,"itens":2,"aprovado_por":"gestor"}'::jsonb, 'ACEITA_PELO_PORTAL', 'VENCEDORA', 1, v_now, 'PROT-OPME-849281', 'Proposta recebida e aceita tecnicamente pelo portal OPMENEXO.', v_user_id, v_now, v_now
    ) ON CONFLICT DO NOTHING;

    -- Cotação 2: Recebida via INPART, com item PENDENTE de De/Para (bloqueia aprovação/envio)
    INSERT INTO plantaopro.adm360_cotacoes (
        id, tenant_id, estabelecimento_id, portal_conta_id, provedor, identificador_externo, revisao_externa, hospital_solicitante_externo, paciente_iniciais, procedimento, data_prevista, prazo_resposta, fuso_horario, status_interno, status_externo, origem, orcamento_id, capturada_em, payload_original
    ) VALUES (
        v_cotacao2_id, v_tenant_id, v_estab_matriz_id, v_conta_inpart_id, 'INPART', 'COT-2026-INPART-002', 1, 'Hospital Unimed Central', 'M.A.C.', 'Artroplastia Total de Quadril', CURRENT_DATE + 10, v_now + interval '72 hours', 'America/Sao_Paulo', 'EM_RELACIONAMENTO', 'RECEBIDA', 'DADOS_DE_TESTE', NULL, v_now, '{"simulado":true}'
    ) ON CONFLICT (tenant_id, portal_conta_id, identificador_externo, revisao_externa) DO NOTHING;

    INSERT INTO plantaopro.adm360_cotacao_itens (
        id, tenant_id, cotacao_id, numero_item, codigo_externo, descricao_externa, fabricante_externo, modelo_externo, quantidade_solicitada, unidade_solicitada, status_relacionamento
    ) VALUES (
        gen_random_uuid(), v_tenant_id, v_cotacao2_id, 1, 'INP-PROT-01', 'Prótese de Quadril Cimentada Haste Femoral', 'Zimmer', 'CPT', 1, 'UN', 'PENDENTE'
    ) ON CONFLICT (tenant_id, cotacao_id, numero_item) DO NOTHING;

    -- Cotação 3: Cotação Expirada (Prazo vencido há 24h)
    INSERT INTO plantaopro.adm360_cotacoes (
        id, tenant_id, estabelecimento_id, portal_conta_id, provedor, identificador_externo, revisao_externa, hospital_solicitante_externo, paciente_iniciais, procedimento, data_prevista, prazo_resposta, fuso_horario, status_interno, status_externo, origem, orcamento_id, capturada_em, payload_original
    ) VALUES (
        v_cotacao3_id, v_tenant_id, v_estab_matriz_id, v_conta_opme_id, 'OPMENEXO', 'COT-2026-EXPIRADA-003', 1, 'Hospital Samaritano', 'R.P.S.', 'Cirurgia Neurológica', CURRENT_DATE + 1, v_now - interval '24 hours', 'America/Sao_Paulo', 'EXPIRADA', 'EXPIRADA', 'DADOS_DE_TESTE', NULL, v_now - interval '72 hours', '{"simulado":true}'
    ) ON CONFLICT (tenant_id, portal_conta_id, identificador_externo, revisao_externa) DO NOTHING;

    -- Cotação 4: Cotação com Nova Revisão (v1 preservada, v2 ativa)
    INSERT INTO plantaopro.adm360_cotacoes (
        id, tenant_id, estabelecimento_id, portal_conta_id, provedor, identificador_externo, revisao_externa, hospital_solicitante_externo, paciente_iniciais, procedimento, data_prevista, prazo_resposta, fuso_horario, status_interno, status_externo, origem, orcamento_id, capturada_em, payload_original
    ) VALUES 
        (gen_random_uuid(), v_tenant_id, v_estab_matriz_id, v_conta_opme_id, 'OPMENEXO', 'COT-2026-REV-004', 1, 'Hospital São Lucas', 'A.F.T.', 'Laminectomia Lombar', CURRENT_DATE + 7, v_now + interval '24 hours', 'America/Sao_Paulo', 'RECEBIDA', 'SUBSTITUIDA', 'DADOS_DE_TESTE', NULL, v_now - interval '12 hours', '{"v":1}'),
        (v_cotacao4_id, v_tenant_id, v_estab_matriz_id, v_conta_opme_id, 'OPMENEXO', 'COT-2026-REV-004', 2, 'Hospital São Lucas', 'A.F.T.', 'Laminectomia Lombar com Artrodese', CURRENT_DATE + 7, v_now + interval '36 hours', 'America/Sao_Paulo', 'EM_RELACIONAMENTO', 'RECEBIDA', 'DADOS_DE_TESTE', NULL, v_now, '{"v":2}')
    ON CONFLICT (tenant_id, portal_conta_id, identificador_externo, revisao_externa) DO NOTHING;

    -- Anexo demonstrativo sintético para a Cotação 1
    INSERT INTO plantaopro.adm360_cotacao_anexos (
        id, tenant_id, cotacao_id, nome_arquivo, tamanho_bytes, content_type, sha256_hash, conteudo, created_at
    ) VALUES (
        gen_random_uuid(), v_tenant_id, v_cotacao1_id, 'solicitacao_cirurgica_001.pdf', 1024, 'application/pdf', 'e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855', '\x255044462d312e340a25d0d4c5d80a312030206f626a0a3c3c2f547970652f436174616c6f672f50616765732032203020523e3e0a656e646f626a0a', v_now
    ) ON CONFLICT DO NOTHING;

    -- 11. SINCRONIZAÇÃO DF-e SEFAZ
    INSERT INTO plantaopro.adm360_dfe_sincronizacoes (
        id, tenant_id, estabelecimento_id, cnpj, ambiente, provedor, ultimo_nsu, max_nsu, data_consulta, proxima_consulta_permitida, status, created_at
    ) VALUES (
        gen_random_uuid(), v_tenant_id, v_estab_matriz_id, '46389044000130', 'HOMOLOGACAO', 'SEFAZ_DISTRIBUICAO_DFE', '000000000001200', '000000000001250', v_now - interval '30 minutes', v_now + interval '30 minutes', 'PRONTO', v_now
    ) ON CONFLICT (tenant_id, estabelecimento_id, ambiente) DO UPDATE 
        SET ultimo_nsu = excluded.ultimo_nsu, max_nsu = excluded.max_nsu;

    -- 12. CENTRAL DE XML RECEBIDOS (NF-e MODELO 55)
    -- Documento 1: NF-e 55 válida e conferida
    INSERT INTO plantaopro.adm360_documentos_recebidos (
        id, tenant_id, estabelecimento_id, chave_acesso, numero, serie, modelo, data_emissao, emitente_cnpj, emitente_nome, destinatario_cnpj, destinatario_nome, valor_total, valor_produtos, tipo_documento, status_manifestacao, status_conferencia, xml_conteudo, xml_hash, nsu, quarentena, origem, created_at
    ) VALUES (
        v_doc1_id, v_tenant_id, v_estab_matriz_id,
        '35260900000000000000550010000000011000000010', '1', '1', '55',
        v_now - interval '2 days', '11222333000144', 'Ortopedia & Cirurgia Distribuidora Ltda',
        '46389044000130', 'Santa Casa Matriz', 5350.00, 5350.00,
        'NFE_COMPLETA', 'CONFIRMACAO_DA_OPERACAO', 'CONFERIDO',
        '<nfeProc xmlns="http://www.portalfiscal.inf.br/nfe" versao="4.00"><NFe><infNFe Id="NFe35260900000000000000550010000000011000000010" versao="4.00"><ide><cUF>35</cUF><cNF>00000001</cNF><natOp>VENDA</natOp><mod>55</mod><serie>1</serie><nNF>1</nNF><dhEmi>2026-09-23T10:00:00-03:00</dhEmi><tpNF>1</tpNF><idDest>1</idDest><cMunFG>3550308</cMunFG><tpImp>1</tpImp><tpEmis>1</tpEmis><cDV>0</cDV><tpAmb>2</tpAmb><finNFe>1</finNFe><indFinal>0</indFinal><indPres>9</indPres><procEmi>0</procEmi><verProc>1.0</verProc></ide><emit><CNPJ>11222333000144</CNPJ><xNome>Ortopedia e Cirurgia Distribuidora Ltda</xNome></emit><dest><CNPJ>46389044000130</CNPJ><xNome>Santa Casa Matriz</xNome></dest><total><ICMSTot><vProd>5350.00</vProd><vNF>5350.00</vNF></ICMSTot></total></infNFe></NFe></nfeProc>',
        'hash-sha256-nfe-001', '000000000001201', false, 'DADOS_DE_TESTE', v_now
    ) ON CONFLICT (tenant_id, chave_acesso) DO NOTHING;

    INSERT INTO plantaopro.adm360_documento_itens (
        id, tenant_id, documento_id, numero_item, codigo_produto_emitente, descricao_produto_emitente, ncm, cfop, unidade_comercial, quantidade_comercial, valor_unitario, valor_total, produto_id, unidade_interna, fator_conversao, quantidade_convertida, conferido
    ) VALUES (
        gen_random_uuid(), v_tenant_id, v_doc1_id, 1, 'FORN-STENT-01', 'Stent Coronario Sirolimus Orsiro', '90219080', '5102', 'UN', 1, 4500.00, 4500.00, v_prod1_id, 'UN', 1.0000, 1, true
    ) ON CONFLICT (tenant_id, documento_id, numero_item) DO NOTHING;

    INSERT INTO plantaopro.adm360_documento_eventos (
        id, tenant_id, documento_id, tipo_evento, sequencia_evento, descricao_evento, data_evento, protocolo, detalhes, registrado_por, created_at
    ) VALUES (
        gen_random_uuid(), v_tenant_id, v_doc1_id, 'CONFIRMACAO_DA_OPERACAO', 1, 'Confirmação da Operação pelo Destinatário', v_now, 'PROT-SEFAZ-994821', 'Evento transmitido e registrado no ambiente de homologação', v_user_id, v_now
    ) ON CONFLICT DO NOTHING;

    -- Documento 2: XML sintético rejeitado/em quarentena por CNPJ de destinatário não autorizado no tenant
    INSERT INTO plantaopro.adm360_documentos_recebidos (
        id, tenant_id, estabelecimento_id, chave_acesso, numero, serie, modelo, data_emissao, emitente_cnpj, emitente_nome, destinatario_cnpj, destinatario_nome, valor_total, valor_produtos, tipo_documento, status_manifestacao, status_conferencia, xml_conteudo, xml_hash, quarentena, motivo_quarentena, origem, created_at
    ) VALUES (
        v_doc_quarentena_id, v_tenant_id, v_estab_matriz_id,
        '35260999999999999999550010000000021000000020', '2', '1', '55',
        v_now, '99888777000100', 'Fornecedor Terceiro Desconhecido',
        '00000000000000', 'Empresa Não Pertencente ao Tenant', 1200.00, 1200.00,
        'NFE_COMPLETA', 'SEM_MANIFESTACAO', 'PENDENTE',
        '<nfeProc><NFe><infNFe Id="NFe35260999999999999999550010000000021000000020"><ide><mod>55</mod><nNF>2</nNF></ide><emit><CNPJ>99888777000100</CNPJ></emit><dest><CNPJ>00000000000000</CNPJ></dest><total><ICMSTot><vNF>1200.00</vNF></ICMSTot></total></infNFe></NFe></nfeProc>',
        'hash-sha256-nfe-quarentena', true, 'DESTINATARIO_NAO_AUTORIZADO', 'DADOS_DE_TESTE', v_now
    ) ON CONFLICT (tenant_id, chave_acesso) DO NOTHING;

    RAISE NOTICE 'Carga demonstrativa do Administrativo 360 concluída com sucesso para o tenant % (Santa Casa).', v_tenant_id;
END $DEMO_BLOCK$;
