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
    v_user_consulta_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647515';
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
    v_perfil_consulta_id uuid;
    v_perm_ver_id uuid;
    v_perm_code text;
    v_perm_id uuid;
    v_now timestamp with time zone := clock_timestamp();
    v_perms text[] := ARRAY[
        'ADM360:VER',
        'ADM360:COMPRAS',
        'ADM360:ESTOQUE',
        'ADM360:LIBERAR_QUALIDADE',
        'ADM360:INVENTARIO_APROVAR',
        'ADM360:COMERCIAL',
        'ADM360:CIRURGIAS',
        'ADM360:SEPARAR',
        'ADM360:EXPEDIR',
        'ADM360:RECONCILIAR',
        'ADM360:VALORIZAR',
        'ADM360:VENDAS',
        'ADM360:RECEBER',
        'ADM360:ESTORNAR',
        'ADM360:FINANCEIRO',
        'ADM360:PAGAR',
        'ADM360:APROVAR_DESPESA',
        'ADM360:CRIAR_DESPESA',
        'ADM360:FECHAR_CAIXA',
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
    v_mod_code text;
    v_mod_id uuid;
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

    -- Contratação canônica dos módulos destinados ao cliente Santa Casa Demonstração:
    -- ADM360, ESCALAS, CONFERENCIA, EXECUCAO, RELATORIOS
    FOREACH v_mod_code IN ARRAY ARRAY['ADM360', 'ESCALAS', 'CONFERENCIA', 'EXECUCAO', 'RELATORIOS'] LOOP
        SELECT id INTO v_mod_id FROM plantaopro.modulos_sistema WHERE upper(btrim(codigo)) = v_mod_code AND reg_status = 'A' LIMIT 1;
        IF v_mod_id IS NOT NULL THEN
            INSERT INTO plantaopro.tenant_modulos
                (id, tenant_id, modulo_id, codigo, codigo_modulo, habilitado, status, origem, ativado_em, reg_date, reg_status)
            VALUES
                (gen_random_uuid(), v_tenant_id, v_mod_id, v_mod_code, v_mod_code, true, 'ATIVO', 'CONTRATO_DEMO', v_now, v_now, 'A')
            ON CONFLICT (tenant_id, modulo_id) WHERE reg_status = 'A' AND modulo_id IS NOT NULL
            DO UPDATE SET habilitado = true, status = 'ATIVO', reg_update = v_now;
        END IF;
    END LOOP;

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

    -- Cadastrar Permissões ADM360 e Vincular ao Perfil do Gestor (32 permissões operacionais completas)
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
            id, tenant_id, cliente_id, nome, email, email_normalizado, senha_hash, status, reg_status, senha_alteracao_obrigatoria, reg_date
        ) VALUES (
            v_user_consulta_id, v_tenant_id, 'd3f6584c-2c64-4e5a-9ea9-4e1428647501', 'Auditor Restrito — Demonstração',
            'consulta@santacasa-demo.example', 'consulta@santacasa-demo.example',
            '$2a$11$mNmgw83PBauw.5XsFVB5TuMB1.8OgFZ0SpfujQSuGzUgzwvs7d8S.',
            'ATIVO', 'A', false, v_now
        ) ON CONFLICT (id) DO UPDATE SET cliente_id = 'd3f6584c-2c64-4e5a-9ea9-4e1428647501', tenant_id = v_tenant_id;
    ELSE
        UPDATE plantaopro.usuarios
        SET cliente_id = 'd3f6584c-2c64-4e5a-9ea9-4e1428647501', tenant_id = v_tenant_id, status = 'ATIVO', reg_status = 'A'
        WHERE lower(email) = 'consulta@santacasa-demo.example';
    END IF;

    -- Perfil de Consulta/Auditoria (Apenas leitura sem escrita)
    SELECT id INTO v_perfil_consulta_id 
    FROM plantaopro.perfis 
    WHERE (tenant_id = v_tenant_id OR tenant_id IS NULL) 
      AND (codigo = 'CONSULTA_CLIENTE' OR codigo = 'AUDITOR')
      AND reg_status = 'A'
    ORDER BY (tenant_id = v_tenant_id) DESC LIMIT 1;

    IF v_perfil_consulta_id IS NULL THEN
        v_perfil_consulta_id := gen_random_uuid();
        INSERT INTO plantaopro.perfis (id, tenant_id, codigo, nome, descricao, base_sistema, customizado, status, reg_status, reg_date)
        VALUES (v_perfil_consulta_id, v_tenant_id, 'CONSULTA_CLIENTE', 'Consulta e Auditoria do Cliente', 'Perfil de consulta restrita', true, false, 'ATIVO', 'A', v_now);
    END IF;

    -- Atribuir exclusivamente permissões de leitura ao perfil de consulta
    FOREACH v_perm_code IN ARRAY ARRAY['ADM360:VER', 'ADM360:COTACAO_CONSULTAR', 'ADM360:CONSULTAR_ANEXOS', 'ADM360:AUDITAR'] LOOP
        SELECT id INTO v_perm_id FROM plantaopro.permissoes WHERE upper(codigo) = upper(v_perm_code) AND reg_status = 'A' LIMIT 1;
        IF v_perm_id IS NOT NULL THEN
            INSERT INTO plantaopro.perfil_permissoes (id, perfil_id, permissao_id, permitido, reg_status, reg_date)
            VALUES (gen_random_uuid(), v_perfil_consulta_id, v_perm_id, true, 'A', v_now)
            ON CONFLICT (perfil_id, permissao_id) WHERE reg_status = 'A' DO UPDATE SET permitido = true;
        END IF;
    END LOOP;

    INSERT INTO plantaopro.usuarios_perfis (id, tenant_id, usuario_id, perfil_id, reg_status, reg_date)
    VALUES (gen_random_uuid(), v_tenant_id, v_user_consulta_id, v_perfil_consulta_id, 'A', v_now)
    ON CONFLICT (usuario_id, perfil_id) WHERE reg_status = 'A' DO NOTHING;

    -- Garantir médico canônico ativo vinculado diretamente ao tenant Santa Casa Demonstração
    INSERT INTO plantaopro.medicos (id, tenant_id, nome, crm, crm_uf, especialidade, status, reg_status, reg_date)
    VALUES ('d3f6584c-2c64-4e5a-9ea9-4e1428647530', v_tenant_id, 'Dra. Ana Souza — Demonstração', '123456', 'SP', 'Cardiologia e Cirurgia Geral', 'ATIVO', 'A', v_now)
    ON CONFLICT (id) DO UPDATE SET tenant_id = v_tenant_id, status = 'ATIVO', reg_status = 'A';

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

    -- 5. PARCEIROS (HOSPITAL, PAGADOR E FORNECEDOR COM PAPÉIS EXPLÍCITOS)
    -- Garante colunas caso o seed seja executado antes da migration
    ALTER TABLE plantaopro.adm360_parceiros ADD COLUMN IF NOT EXISTS eh_hospital boolean NOT NULL DEFAULT false;
    ALTER TABLE plantaopro.adm360_parceiros ADD COLUMN IF NOT EXISTS eh_pagador boolean NOT NULL DEFAULT false;
    ALTER TABLE plantaopro.adm360_parceiros ADD COLUMN IF NOT EXISTS eh_cliente boolean NOT NULL DEFAULT false;

    INSERT INTO plantaopro.adm360_parceiros (id, tenant_id, nome, documento, fornecedor, eh_hospital, eh_pagador, eh_cliente, ativo, created_at)
    VALUES 
        (v_hospital_parceiro_id, v_tenant_id, 'Hospital Regional Parceiro', '12345678000199', false, true, false, false, true, v_now),
        (v_pagador_parceiro_id, v_tenant_id, 'Unimed Seguros Saúde', '98765432000188', false, false, true, false, true, v_now),
        (v_fornecedor_parceiro_id, v_tenant_id, 'Ortopedia & Cirurgia Distribuidora Ltda', '11222333000144', true, false, false, false, true, v_now)
    ON CONFLICT (id) DO UPDATE 
        SET nome = excluded.nome,
            fornecedor = excluded.fornecedor,
            eh_hospital = excluded.eh_hospital,
            eh_pagador = excluded.eh_pagador,
            eh_cliente = excluded.eh_cliente,
            ativo = true;

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

    -- 13. SUPRIMENTOS, ESTOQUE, CIRURGIAS E VALES DE CONSIGNAÇÃO
    DECLARE
        v_sup_fornecedor uuid := 'a3610000-0000-4000-8000-000000000001'; 
        v_sup_hospital uuid := 'a3610000-0000-4000-8000-000000000030';
        v_sup_produto uuid := 'a3610000-0000-4000-8000-000000000002'; 
        v_sup_local_cd uuid := 'a3610000-0000-4000-8000-000000000003'; 
        v_sup_local_hosp uuid := 'a3610000-0000-4000-8000-000000000004'; 
        v_sup_local_inv uuid := 'a3610000-0000-4000-8000-000000000020';
        v_sup_lote_livre uuid := 'a3610000-0000-4000-8000-000000000005'; 
        v_sup_lote_q uuid := 'a3610000-0000-4000-8000-000000000006'; 
        v_sup_lote_v uuid := 'a3610000-0000-4000-8000-000000000007'; 
        v_sup_lote_vence_antes uuid := 'a3610000-0000-4000-8000-000000000025';
        v_sup_pedido uuid := 'a3610000-0000-4000-8000-000000000008'; 
        v_sup_item uuid := 'a3610000-0000-4000-8000-000000000009'; 
        v_sup_receb uuid := 'a3610000-0000-4000-8000-000000000010'; 
        v_sup_ri uuid := 'a3610000-0000-4000-8000-000000000011';
        v_sup_orc_rascunho uuid := 'a3610000-0000-4000-8000-000000000040';
        v_sup_orc_aprovado uuid := 'a3610000-0000-4000-8000-000000000041';
        v_sup_orc_item_1 uuid := 'a3610000-0000-4000-8000-000000000042';
        v_sup_orc_item_2 uuid := 'a3610000-0000-4000-8000-000000000043';
        v_sup_reserva_1 uuid := 'a3610000-0000-4000-8000-000000000044';
        v_sup_lote_10 uuid := 'a3610000-0000-4000-8000-000000000050';
        v_sup_orc_demo_10 uuid := 'a3610000-0000-4000-8000-000000000051';
        v_sup_item_demo_10 uuid := 'a3610000-0000-4000-8000-000000000052';
        v_sup_reserva_6 uuid := 'a3610000-0000-4000-8000-000000000053';
        v_sup_cirurgia_1 uuid := 'a3610000-0000-4000-8000-000000000054';
        v_sup_vale_1 uuid := 'a3610000-0000-4000-8000-000000000055';
        v_sup_vale_item_1 uuid := 'a3610000-0000-4000-8000-000000000056';
        v_sup_cirurgia_rec uuid := 'a3610000-0000-4000-8000-000000000060';
        v_sup_vale_rec uuid := 'a3610000-0000-4000-8000-000000000061';
        v_sup_vale_rec_item uuid := 'a3610000-0000-4000-8000-000000000062';
    BEGIN
        -- Parceiros e Hospital
        INSERT INTO plantaopro.adm360_parceiros(id,tenant_id,nome,documento,fornecedor,ativo) 
        VALUES
            (v_sup_fornecedor,v_tenant_id,'Orto Demo Fornecimentos','11222333000181',true,true),
            (v_sup_hospital,v_tenant_id,'Hospital São Lucas Demonstração','44555666000199',false,true)
        ON CONFLICT(id) DO UPDATE SET ativo=true;

        -- Produtos
        INSERT INTO plantaopro.adm360_produtos(id,tenant_id,sku,nome,unidade,codigo_barras,controla_lote,exige_inspecao,preco_custo,ativo) 
        VALUES(v_sup_produto,v_tenant_id,'IMP-DEMO','Implante controlado demonstrativo','UN','7890000003602',true,true,600.00,true) 
        ON CONFLICT(id) DO UPDATE SET ativo=true, preco_custo=600.00;

        -- Locais
        INSERT INTO plantaopro.adm360_locais(id,tenant_id,codigo,nome,tipo,ativo) 
        VALUES
            (v_sup_local_cd,v_tenant_id,'CD-DEMO','Centro de distribuição','INTERNO',true),
            (v_sup_local_hosp,v_tenant_id,'HOSP-DEMO','Hospital Demo (custódia)','EXTERNO',true),
            (v_sup_local_inv,v_tenant_id,'ALMOX-INV','Almoxarifado em Contagem de Inventário','INTERNO',true) 
        ON CONFLICT(id) DO UPDATE SET ativo=true;

        -- Lotes
        INSERT INTO plantaopro.adm360_lotes(id,tenant_id,produto_id,codigo,validade) 
        VALUES
            (v_sup_lote_livre,v_tenant_id,v_sup_produto,'DEMO-LIVRE',date '2027-09-24'),
            (v_sup_lote_q,v_tenant_id,v_sup_produto,'DEMO-QUARENTENA',date '2027-06-30'),
            (v_sup_lote_v,v_tenant_id,v_sup_produto,'DEMO-VENCIDO',date '2025-01-01'),
            (v_sup_lote_vence_antes,v_tenant_id,v_sup_produto,'DEMO-VENCE-ANTES',date '2026-10-15'),
            (v_sup_lote_10,v_tenant_id,v_sup_produto,'LOTE-DEMO-10UN',date '2027-12-31')
        ON CONFLICT(id) DO NOTHING;

        -- Pedido de Compra e Recebimento
        INSERT INTO plantaopro.adm360_pedidos(id,tenant_id,numero,fornecedor_id,situacao,previsao,frete,aprovado_em,aprovado_por,created_by) 
        VALUES(v_sup_pedido,v_tenant_id,'PC-DEMO-001',v_sup_fornecedor,'PARCIAL',date '2026-09-25',0,now(),v_user_id,v_user_id) 
        ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_pedido_itens(id,tenant_id,pedido_id,produto_id,quantidade,quantidade_recebida,preco_unitario,desconto) 
        VALUES(v_sup_item,v_tenant_id,v_sup_pedido,v_sup_produto,20,10,100,0) 
        ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_recebimentos(id,tenant_id,pedido_id,documento,idempotency_key,confirmado_em,created_by) 
        VALUES(v_sup_receb,v_tenant_id,v_sup_pedido,'NF-DEMO-PARCIAL','seed:recebimento:1',now(),v_user_id) 
        ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_recebimento_itens(id,tenant_id,recebimento_id,pedido_item_id,produto_id,lote_id,local_id,quantidade,condicao) 
        VALUES(v_sup_ri,v_tenant_id,v_sup_receb,v_sup_item,v_sup_produto,v_sup_lote_q,v_sup_local_cd,10,'QUARENTENA') 
        ON CONFLICT(id) DO NOTHING;

        -- Estoques no mesmo local (CD-DEMO): LIBERADO, QUARENTENA, VENCIDO e LOTE QUE VENCE ANTES
        INSERT INTO plantaopro.adm360_movimentos(id,tenant_id,produto_id,lote_id,local_id,tipo,condicao,quantidade,origem_tipo,origem_id,idempotency_key,created_by) 
        VALUES
            ('a3610000-0000-4000-8000-000000000012',v_tenant_id,v_sup_produto,v_sup_lote_q,v_sup_local_cd,'ENTRADA','QUARENTENA',10,'RECEBIMENTO',v_sup_ri,'seed:movimento:q',v_user_id),
            ('a3610000-0000-4000-8000-000000000013',v_tenant_id,v_sup_produto,v_sup_lote_livre,v_sup_local_cd,'ENTRADA','LIBERADO',15,'SEED',v_sup_lote_livre,'seed:movimento:livre',v_user_id),
            ('a3610000-0000-4000-8000-000000000014',v_tenant_id,v_sup_produto,v_sup_lote_v,v_sup_local_cd,'ENTRADA','VENCIDO',2,'SEED',v_sup_lote_v,'seed:movimento:vencido',v_user_id),
            ('a3610000-0000-4000-8000-000000000026',v_tenant_id,v_sup_produto,v_sup_lote_vence_antes,v_sup_local_cd,'ENTRADA','LIBERADO',5,'SEED',v_sup_lote_vence_antes,'seed:movimento:vence_antes',v_user_id),
            ('a3610000-0000-4000-8000-000000000057',v_tenant_id,v_sup_produto,v_sup_lote_10,v_sup_local_cd,'ENTRADA','LIBERADO',10,'SEED',v_sup_lote_10,'seed:movimento:lote10',v_user_id)
        ON CONFLICT(id) DO NOTHING;

        -- Ocorrência
        INSERT INTO plantaopro.adm360_ocorrencias(id,tenant_id,recebimento_item_id,lote_id,produto_id,local_id,tipo,descricao,quantidade,situacao,responsavel_id,prazo) 
        VALUES('a3610000-0000-4000-8000-000000000015',v_tenant_id,v_sup_ri,v_sup_lote_q,v_sup_produto,v_sup_local_cd,'DOCUMENTACAO','Divergência documental demonstrativa',2,'ABERTA',v_user_id,date '2026-09-30') 
        ON CONFLICT(id) DO NOTHING;

        -- Inventário ativo somente no local_inv
        DELETE FROM plantaopro.adm360_inventarios WHERE id='a3610000-0000-4000-8000-000000000016';
        INSERT INTO plantaopro.adm360_inventarios(id,tenant_id,local_id,situacao,escopo,created_by) 
        VALUES('a3610000-0000-4000-8000-000000000016',v_tenant_id,v_sup_local_inv,'CONTAGEM','Implantes controlados em conferência',v_user_id) 
        ON CONFLICT(id) DO UPDATE SET local_id=v_sup_local_inv, situacao='CONTAGEM';

        -- Tarefas de coleta
        INSERT INTO plantaopro.adm360_tarefas_coleta(id,tenant_id,tipo,descricao,situacao,atribuida_a,origem_id) 
        VALUES
            ('a3610000-0000-4000-8000-000000000017',v_tenant_id,'INVENTARIO','Contar almoxarifado em inventário','ABERTA',v_user_id,'a3610000-0000-4000-8000-000000000016'),
            ('a3610000-0000-4000-8000-000000000018',v_tenant_id,'SEPARACAO','Separação interna demonstrativa','ABERTA',v_user_id,v_sup_lote_livre) 
        ON CONFLICT(id) DO NOTHING;

        -- Orçamentos Cirúrgicos e Reservas
        INSERT INTO plantaopro.adm360_orcamentos(
            id, tenant_id, numero, revisao, hospital_id, procedimento, responsavel_financeiro_id,
            data_prevista, validade, situacao, total_produtos, desconto_geral, total_geral, observacoes, created_by
        ) VALUES (
            v_sup_orc_rascunho, v_tenant_id, 'ORC-DEMO-001', 1, v_sup_hospital, 'Artroplastia Total de Quadril Direita', v_sup_hospital,
            date '2026-11-20', date '2026-11-05', 'RASCUNHO', 1200.00, 0, 1200.00, 'Orçamento em fase de elaboração e cotação', v_user_id
        ) ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_orcamento_itens(
            id, tenant_id, orcamento_id, produto_id, quantidade, preco_unitario, desconto, total
        ) VALUES (
            v_sup_orc_item_1, v_tenant_id, v_sup_orc_rascunho, v_sup_produto, 2, 600.00, 0, 1200.00
        ) ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_orcamentos(
            id, tenant_id, numero, revisao, hospital_id, procedimento, responsavel_financeiro_id,
            data_prevista, validade, situacao, total_produtos, desconto_geral, total_geral, observacoes, aprovado_em, aprovado_por, created_by
        ) VALUES (
            v_sup_orc_aprovado, v_tenant_id, 'ORC-DEMO-002', 1, v_sup_hospital, 'Reconstrução Ligamentar Joelho Esquerdo', v_sup_hospital,
            date '2026-11-20', date '2026-11-10', 'APROVADO', 2500.00, 100.00, 2400.00, 'Orçamento aprovado pelo convênio / hospital', now(), v_user_id, v_user_id
        ) ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_orcamento_itens(
            id, tenant_id, orcamento_id, produto_id, quantidade, preco_unitario, desconto, total
        ) VALUES (
            v_sup_orc_item_2, v_tenant_id, v_sup_orc_aprovado, v_sup_produto, 5, 500.00, 100.00, 2400.00
        ) ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_reservas(
            id, tenant_id, produto_id, lote_id, local_id, quantidade, situacao, origem_tipo, origem_id, orcamento_item_id, idempotency_key, created_by
        ) VALUES (
            v_sup_reserva_1, v_tenant_id, v_sup_produto, v_sup_lote_livre, v_sup_local_cd, 2, 'ATIVA', 'ORCAMENTO_CIRURGICO', v_sup_orc_aprovado, v_sup_orc_item_2, 'seed:reserva:orc2:item2', v_user_id
        ) ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_orcamentos(
            id, tenant_id, numero, revisao, hospital_id, procedimento, responsavel_financeiro_id,
            data_prevista, validade, situacao, total_produtos, desconto_geral, total_geral, observacoes, aprovado_em, aprovado_por, created_by
        ) VALUES (
            v_sup_orc_demo_10, v_tenant_id, 'ORC-DEMO-010', 1, v_sup_hospital, 'Artroplastia de Joelho Bilateral', v_sup_hospital,
            date '2026-11-25', date '2026-11-15', 'APROVADO', 6000.00, 0, 6000.00, 'Orçamento aprovado para jornada de consignação', now(), v_user_id, v_user_id
        ) ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_orcamento_itens(id, tenant_id, orcamento_id, produto_id, quantidade, preco_unitario, desconto, total)
        VALUES(v_sup_item_demo_10, v_tenant_id, v_sup_orc_demo_10, v_sup_produto, 10, 600.00, 0, 6000.00)
        ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_reservas(
            id, tenant_id, produto_id, lote_id, local_id, quantidade, situacao, origem_tipo, origem_id, orcamento_item_id, idempotency_key, created_by
        ) VALUES (
            v_sup_reserva_6, v_tenant_id, v_sup_produto, v_sup_lote_10, v_sup_local_cd, 6, 'ATIVA', 'ORCAMENTO_CIRURGICO', v_sup_orc_demo_10, v_sup_item_demo_10, 'seed:reserva:demo:6', v_user_id
        ) ON CONFLICT(id) DO NOTHING;

        -- Cirurgias e Vales
        INSERT INTO plantaopro.adm360_cirurgias(
            id, tenant_id, numero, hospital_id, procedimento, data_prevista, hora_prevista,
            orcamento_id, orcamento_revisao, local_destino_id, situacao, observacoes, created_by
        ) VALUES (
            v_sup_cirurgia_1, v_tenant_id, 'CIR-DEMO-001', v_sup_hospital, 'Artroplastia de Joelho Bilateral', date '2026-11-25', time '08:00',
            v_sup_orc_demo_10, 1, v_sup_local_hosp, 'AGENDADA', 'Cirurgia demonstrativa com vale em preparação', v_user_id
        ) ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_vales(
            id, tenant_id, numero, cirurgia_id, orcamento_id, orcamento_revisao, hospital_id,
            local_origem_id, local_destino_id, data_saida_prevista, data_retorno_prevista,
            situacao, situacao_financeira, observacoes, created_by
        ) VALUES (
            v_sup_vale_1, v_tenant_id, 'VAL-DEMO-001', v_sup_cirurgia_1, v_sup_orc_demo_10, 1, v_sup_hospital,
            v_sup_local_cd, v_sup_local_hosp, date '2026-11-24', date '2026-11-28',
            'EM_SEPARACAO', 'PENDENTE_VALORIZACAO', 'Vale em fase de separação no almoxarifado', v_user_id
        ) ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_vale_itens(
            id, tenant_id, vale_id, produto_id, lote_id, reserva_id, quantidade_solicitada, quantidade_separada, preco_unitario
        ) VALUES (
            v_sup_vale_item_1, v_tenant_id, v_sup_vale_1, v_sup_produto, v_sup_lote_10, v_sup_reserva_6, 6, 3, 600.00
        ) ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_cirurgias(
            id, tenant_id, numero, hospital_id, procedimento, data_prevista, hora_prevista,
            local_destino_id, situacao, observacoes, created_by
        ) VALUES (
            v_sup_cirurgia_rec, v_tenant_id, 'CIR-DEMO-REC', v_sup_hospital, 'Cirurgia Concluída Reconciliada', date '2026-09-10', time '09:00',
            v_sup_local_hosp, 'REALIZADA', 'Cirurgia concluída com sucesso e vale reconciliado', v_user_id
        ) ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_vales(
            id, tenant_id, numero, cirurgia_id, hospital_id, local_origem_id, local_destino_id,
            data_saida_prevista, data_saida_efetiva, situacao, situacao_financeira, observacoes, created_by
        ) VALUES (
            v_sup_vale_rec, v_tenant_id, 'VAL-DEMO-REC', v_sup_cirurgia_rec, v_sup_hospital,
            v_sup_local_cd, v_sup_local_hosp, date '2026-09-09', date '2026-09-09 07:30:00+00',
            'RECONCILIADO', 'VALORIZADO', 'Vale reconciliado com devolução e consumo faturado', v_user_id
        ) ON CONFLICT(id) DO NOTHING;

        INSERT INTO plantaopro.adm360_vale_itens(
            id, tenant_id, vale_id, produto_id, lote_id, quantidade_solicitada, quantidade_separada,
            quantidade_expedida, quantidade_consumida, quantidade_devolvida, preco_unitario
        ) VALUES (
            v_sup_vale_rec_item, v_tenant_id, v_sup_vale_rec, v_sup_produto, v_sup_lote_10,
            6, 6, 6, 4, 2, 600.00
        ) ON CONFLICT(id) DO NOTHING;
    END;

    RAISE NOTICE 'Carga demonstrativa do Administrativo 360 concluída com sucesso para o tenant % (Santa Casa).', v_tenant_id;
END $DEMO_BLOCK$;
