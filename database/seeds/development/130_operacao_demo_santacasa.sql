-- Seed OPT-IN de desenvolvimento: operação demonstrável Santa Casa.
-- Não entra no instalador canônico.
-- Uso:
--   psql -d <banco> -f database/seeds/development/130_operacao_demo_santacasa.sql
--
-- Pré-requisito: schema plantaopro e acesso demo provisionado (120/121 ou --provision-demo).
-- Cobre a jornada: Escalas -> Execução -> Conferência com isolamento multi-tenant.

SET search_path TO plantaopro, public;

DO $seed$
DECLARE
    v_db text := current_database();
    v_client_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647501';
    v_tenant_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647502';
    v_unit_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647504';
    v_manager_user_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647511';
    v_physician_user_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647512';
    v_physician_medico_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647530';

    v_specialty_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647540';
    v_sector_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647545';

    -- 4 plantões na janela [ontem, hoje, amanhã, +3 dias]
    v_shift1_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647551'; -- ontem (elegível a conferência)
    v_shift2_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647552'; -- hoje (em execução / check-in ativo)
    v_shift3_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647553'; -- amanhã (atribuído à médica + convite pendente)
    v_shift4_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647554'; -- +3 dias (descoberto sem médico)

    v_scale1_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647561';
    v_scale2_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647562';
    v_scale3_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647563';

    v_checkin1_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647571';
    v_checkin2_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647572';

    v_closing1_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647581';
    v_invite3_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647582';
    v_incident2_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647591';

    v_incompatible record;
BEGIN
    IF v_db IN ('template0', 'template1') THEN
        RAISE EXCEPTION 'Recusado: execute o seed no banco de desenvolvimento (ex: plantaopro ou postgres), não em %.', v_db;
    END IF;

    IF to_regclass('plantaopro.plantoes') IS NULL THEN
        RAISE EXCEPTION 'Schema plantaopro ou tabela plantoes ausente. Execute as migrações/scripts base antes deste seed.';
    END IF;

    PERFORM pg_advisory_xact_lock(7065262027);

    -- 1. O seed só opera sobre o schema canônico UUID. Recusar schemas legados
    -- evita converter referências existentes para NULL ou remover dados de tenants.
    FOR v_incompatible IN
        SELECT c.table_name, c.column_name
        FROM information_schema.columns c
        JOIN (VALUES
            ('especialidades', 'tenant_id'), ('especialidades', 'cliente_id'),
            ('hospitais', 'tenant_id'), ('plantoes', 'tenant_id'),
            ('escalas', 'tenant_id'), ('clientes', 'tenant_id'),
            ('assinaturas', 'tenant_id'), ('unidades', 'tenant_id'),
            ('medicos', 'tenant_id'), ('plantao_convites', 'tenant_id'),
            ('plantao_convites', 'cliente_id'), ('tenants', 'id'),
            ('tenant_modulos', 'id')
        ) AS expected(table_name, column_name)
          ON expected.table_name = c.table_name AND expected.column_name = c.column_name
        WHERE c.table_schema = 'plantaopro' AND c.data_type = 'bigint'
    LOOP
        RAISE EXCEPTION
            'Schema legado incompatível: plantaopro.%.% usa bigint. Migre o banco para UUID antes de executar este seed; nenhum dado foi alterado.',
            v_incompatible.table_name, v_incompatible.column_name;
    END LOOP;

    -- 2. Compatibilidade idempotente de colunas essenciais
    IF to_regclass('plantaopro.especialidades') IS NOT NULL THEN
        ALTER TABLE plantaopro.especialidades ADD COLUMN IF NOT EXISTS tenant_id uuid;
        ALTER TABLE plantaopro.especialidades ADD COLUMN IF NOT EXISTS cliente_id uuid;
        ALTER TABLE plantaopro.especialidades ADD COLUMN IF NOT EXISTS codigo text;
        ALTER TABLE plantaopro.especialidades ADD COLUMN IF NOT EXISTS nome text;
        ALTER TABLE plantaopro.especialidades ADD COLUMN IF NOT EXISTS status text DEFAULT 'ATIVO';
        ALTER TABLE plantaopro.especialidades ADD COLUMN IF NOT EXISTS reg_status char(1) DEFAULT 'A';
    END IF;

    IF to_regclass('plantaopro.hospitais') IS NOT NULL THEN
        ALTER TABLE plantaopro.hospitais ADD COLUMN IF NOT EXISTS tenant_id uuid;
        ALTER TABLE plantaopro.hospitais ADD COLUMN IF NOT EXISTS cliente_id uuid;
        ALTER TABLE plantaopro.hospitais ADD COLUMN IF NOT EXISTS codigo text;
        ALTER TABLE plantaopro.hospitais ADD COLUMN IF NOT EXISTS nome text;
        ALTER TABLE plantaopro.hospitais ADD COLUMN IF NOT EXISTS nome_fantasia varchar(200);
        ALTER TABLE plantaopro.hospitais ADD COLUMN IF NOT EXISTS razao_social varchar(200);
        ALTER TABLE plantaopro.hospitais ADD COLUMN IF NOT EXISTS cnpj varchar(18);
        ALTER TABLE plantaopro.hospitais ADD COLUMN IF NOT EXISTS cidade varchar(80);
        ALTER TABLE plantaopro.hospitais ADD COLUMN IF NOT EXISTS estado char(2);
        ALTER TABLE plantaopro.hospitais ADD COLUMN IF NOT EXISTS status text DEFAULT 'ATIVO';
        ALTER TABLE plantaopro.hospitais ADD COLUMN IF NOT EXISTS reg_status char(1) DEFAULT 'A';
    END IF;

    IF to_regclass('plantaopro.setores') IS NOT NULL THEN
        ALTER TABLE plantaopro.setores ADD COLUMN IF NOT EXISTS tenant_id uuid;
        ALTER TABLE plantaopro.setores ADD COLUMN IF NOT EXISTS cliente_id uuid;
        ALTER TABLE plantaopro.setores ADD COLUMN IF NOT EXISTS unidade_id uuid;
        ALTER TABLE plantaopro.setores ADD COLUMN IF NOT EXISTS codigo text;
        ALTER TABLE plantaopro.setores ADD COLUMN IF NOT EXISTS nome text;
        ALTER TABLE plantaopro.setores ADD COLUMN IF NOT EXISTS status text DEFAULT 'ATIVO';
        ALTER TABLE plantaopro.setores ADD COLUMN IF NOT EXISTS reg_status char(1) DEFAULT 'A';
    ELSE
        CREATE TABLE plantaopro.setores (
            id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id uuid NULL,
            cliente_id uuid NULL,
            unidade_id uuid NULL,
            codigo text NULL,
            nome text NULL,
            status text NOT NULL DEFAULT 'ATIVO',
            dados jsonb NOT NULL DEFAULT '{}'::jsonb,
            reg_status char(1) NOT NULL DEFAULT 'A',
            criado_em timestamptz NOT NULL DEFAULT now(),
            atualizado_em timestamptz NULL
        );
    END IF;

    IF to_regclass('plantaopro.plantoes') IS NOT NULL THEN
        ALTER TABLE plantaopro.plantoes ADD COLUMN IF NOT EXISTS tenant_id uuid;
        ALTER TABLE plantaopro.plantoes ADD COLUMN IF NOT EXISTS cliente_id uuid;
        ALTER TABLE plantaopro.plantoes ADD COLUMN IF NOT EXISTS hospital_id uuid;
        ALTER TABLE plantaopro.plantoes ADD COLUMN IF NOT EXISTS especialidade_id uuid;
        ALTER TABLE plantaopro.plantoes ADD COLUMN IF NOT EXISTS setor_id uuid;
        ALTER TABLE plantaopro.plantoes ADD COLUMN IF NOT EXISTS setor text;
        ALTER TABLE plantaopro.plantoes ADD COLUMN IF NOT EXISTS data_inicio timestamptz;
        ALTER TABLE plantaopro.plantoes ADD COLUMN IF NOT EXISTS data_fim timestamptz;
        ALTER TABLE plantaopro.plantoes ADD COLUMN IF NOT EXISTS valor numeric(12,2) DEFAULT 0;
        ALTER TABLE plantaopro.plantoes ADD COLUMN IF NOT EXISTS vagas integer DEFAULT 1;
        ALTER TABLE plantaopro.plantoes ADD COLUMN IF NOT EXISTS vagas_disponiveis integer DEFAULT 1;
        ALTER TABLE plantaopro.plantoes ADD COLUMN IF NOT EXISTS tipo text DEFAULT 'PRESENCIAL';
        ALTER TABLE plantaopro.plantoes ADD COLUMN IF NOT EXISTS status text DEFAULT 'aberto';
        ALTER TABLE plantaopro.plantoes ADD COLUMN IF NOT EXISTS observacoes text;
        ALTER TABLE plantaopro.plantoes ADD COLUMN IF NOT EXISTS reg_status char(1) DEFAULT 'A';
        ALTER TABLE plantaopro.plantoes ADD COLUMN IF NOT EXISTS reg_date timestamptz DEFAULT now();
    END IF;

    IF to_regclass('plantaopro.escalas') IS NOT NULL THEN
        ALTER TABLE plantaopro.escalas ADD COLUMN IF NOT EXISTS tenant_id uuid;
        ALTER TABLE plantaopro.escalas ADD COLUMN IF NOT EXISTS cliente_id uuid;
        ALTER TABLE plantaopro.escalas ADD COLUMN IF NOT EXISTS plantao_id uuid;
        ALTER TABLE plantaopro.escalas ADD COLUMN IF NOT EXISTS medico_id uuid;
        ALTER TABLE plantaopro.escalas ADD COLUMN IF NOT EXISTS status text DEFAULT 'solicitada';
        ALTER TABLE plantaopro.escalas ADD COLUMN IF NOT EXISTS justificativa text;
        ALTER TABLE plantaopro.escalas ADD COLUMN IF NOT EXISTS created_by uuid;
        ALTER TABLE plantaopro.escalas ADD COLUMN IF NOT EXISTS reg_status char(1) DEFAULT 'A';
        ALTER TABLE plantaopro.escalas ADD COLUMN IF NOT EXISTS reg_date timestamptz DEFAULT now();
    END IF;

    -- 3. Garantir dados básicos do tenant demo se não existirem
    IF to_regclass('plantaopro.clientes') IS NOT NULL THEN
        INSERT INTO plantaopro.clientes(id, tenant_id, codigo, nome, razao_social, nome_fantasia, cnpj, status, reg_status)
        VALUES (v_client_id, v_tenant_id, 'SANTA_CASA_DEMONSTRACAO', 'Santa Casa Demonstração', 'Santa Casa Demonstração', 'Santa Casa Demonstração', '00.000.000/0001-91', 'ATIVO', 'A')
        ON CONFLICT (id) DO UPDATE SET
            tenant_id = EXCLUDED.tenant_id,
            codigo = EXCLUDED.codigo,
            nome = EXCLUDED.nome,
            status = 'ATIVO',
            reg_status = 'A';
    END IF;

    IF to_regclass('plantaopro.tenants') IS NOT NULL THEN
        INSERT INTO plantaopro.tenants(id, tenant_id, codigo, nome, status)
        VALUES (v_tenant_id, v_tenant_id, 'santa-casa-demonstracao', 'Santa Casa Demonstração', 'ATIVO')
        ON CONFLICT (id) DO UPDATE SET
            tenant_id = EXCLUDED.tenant_id,
            codigo = EXCLUDED.codigo,
            nome = EXCLUDED.nome,
            status = 'ATIVO';
    END IF;

    IF to_regclass('plantaopro.unidades') IS NOT NULL THEN
        INSERT INTO plantaopro.unidades(id, cliente_id, tenant_id, codigo, nome, status, reg_status)
        VALUES (v_unit_id, v_client_id, v_tenant_id, 'UNIDADE_DEMO', 'Unidade Central — Demonstração', 'ATIVA', 'A')
        ON CONFLICT (id) DO UPDATE SET
            cliente_id = EXCLUDED.cliente_id,
            tenant_id = EXCLUDED.tenant_id,
            codigo = EXCLUDED.codigo,
            nome = EXCLUDED.nome,
            status = 'ATIVA',
            reg_status = 'A';
    END IF;

    IF to_regclass('plantaopro.tenant_modulos') IS NOT NULL THEN
        INSERT INTO plantaopro.tenant_modulos(id, tenant_id, codigo, nome, status)
        VALUES
            ('d3f6584c-2c64-4e5a-9ea9-4e1428647521', v_tenant_id, 'ESCALAS', 'Escalas', 'ATIVO'),
            ('d3f6584c-2c64-4e5a-9ea9-4e1428647522', v_tenant_id, 'EXECUCAO', 'Execução', 'ATIVO'),
            ('d3f6584c-2c64-4e5a-9ea9-4e1428647523', v_tenant_id, 'CONFERENCIA', 'Conferência', 'ATIVO')
        ON CONFLICT (id) DO NOTHING;
    END IF;

    -- 4. Garantir especialidade e hospital/unidade demo
    SELECT id INTO v_specialty_id FROM plantaopro.especialidades WHERE nome = 'Clínica Médica' LIMIT 1;
    IF v_specialty_id IS NULL THEN
        v_specialty_id := 'd3f6584c-2c64-4e5a-9ea9-4e1428647540';
        INSERT INTO plantaopro.especialidades(id, tenant_id, cliente_id, codigo, nome, status, reg_status)
        VALUES (v_specialty_id, v_tenant_id, v_client_id, 'CLINICA_GERAL', 'Clínica Médica', 'ATIVO', 'A')
        ON CONFLICT (id) DO UPDATE SET
            tenant_id = EXCLUDED.tenant_id,
            cliente_id = EXCLUDED.cliente_id,
            codigo = EXCLUDED.codigo,
            status = 'ATIVO',
            reg_status = 'A';
    ELSE
        UPDATE plantaopro.especialidades
        SET tenant_id = v_tenant_id,
            cliente_id = v_client_id,
            codigo = COALESCE(codigo, 'CLINICA_GERAL'),
            status = 'ATIVO',
            reg_status = 'A'
        WHERE id = v_specialty_id;
    END IF;

    INSERT INTO plantaopro.hospitais(id, tenant_id, cliente_id, codigo, nome, nome_fantasia, razao_social, cnpj, cidade, estado, status, reg_status)
    VALUES (v_unit_id, v_tenant_id, v_client_id, 'UNIDADE_DEMO', 'Santa Casa Demonstração', 'Santa Casa Central — Demonstração', 'Santa Casa de Misericórdia Demonstração', '00.000.000/0001-91', 'São Paulo', 'SP', 'ATIVO', 'A')
    ON CONFLICT (id) DO UPDATE SET
        tenant_id = EXCLUDED.tenant_id,
        cliente_id = EXCLUDED.cliente_id,
        codigo = EXCLUDED.codigo,
        nome_fantasia = EXCLUDED.nome_fantasia,
        razao_social = EXCLUDED.razao_social,
        status = 'ATIVO',
        reg_status = 'A';

    INSERT INTO plantaopro.setores(id, tenant_id, cliente_id, unidade_id, codigo, nome, status, reg_status)
    VALUES (v_sector_id, v_tenant_id, v_client_id, v_unit_id, 'PRONTO_SOCORRO', 'Pronto Socorro Adulto', 'ATIVO', 'A')
    ON CONFLICT (id) DO UPDATE SET
        tenant_id = EXCLUDED.tenant_id,
        cliente_id = EXCLUDED.cliente_id,
        unidade_id = EXCLUDED.unidade_id,
        codigo = EXCLUDED.codigo,
        nome = EXCLUDED.nome,
        status = 'ATIVO',
        reg_status = 'A';

    -- 5. Garantir vínculo do médico demo
    IF to_regclass('plantaopro.medicos') IS NOT NULL THEN
        INSERT INTO plantaopro.medicos(id, usuario_id, cpf, status, reg_status)
        VALUES (v_physician_medico_id, v_physician_user_id, '52998224725', 'ATIVO', 'A')
        ON CONFLICT (id) DO NOTHING;

        UPDATE plantaopro.medicos
        SET tenant_id = v_tenant_id,
            cliente_id = v_client_id,
            nome = COALESCE(nome, 'Dra. Ana Souza — Demonstração'),
            status = 'ATIVO',
            reg_status = 'A'
        WHERE id = v_physician_medico_id OR usuario_id = v_physician_user_id;
    END IF;

    -- 6. Inserir / Atualizar os 4 Plantões
    -- Plantão 1: Ontem (elegível a conferência)
    INSERT INTO plantaopro.plantoes(
        id, tenant_id, cliente_id, hospital_id, especialidade_id, setor_id, setor,
        data_inicio, data_fim, valor, vagas, vagas_disponiveis, tipo, status, observacoes, reg_status, created_by
    ) VALUES (
        v_shift1_id, v_tenant_id, v_client_id, v_unit_id, v_specialty_id, v_sector_id, 'Pronto Socorro Adulto',
        (current_date - interval '1 day') + time '07:00',
        (current_date - interval '1 day') + time '19:00',
        1500.00, 1, 0, 'PRESENCIAL', 'realizado', 'Plantão diurno concluído ontem — aguardando conferência final.', 'A', v_manager_user_id
    ) ON CONFLICT (id) DO UPDATE SET
        tenant_id = EXCLUDED.tenant_id,
        cliente_id = EXCLUDED.cliente_id,
        hospital_id = EXCLUDED.hospital_id,
        especialidade_id = EXCLUDED.especialidade_id,
        data_inicio = EXCLUDED.data_inicio,
        data_fim = EXCLUDED.data_fim,
        valor = EXCLUDED.valor,
        vagas = EXCLUDED.vagas,
        vagas_disponiveis = EXCLUDED.vagas_disponiveis,
        status = EXCLUDED.status,
        observacoes = EXCLUDED.observacoes,
        reg_status = 'A';

    -- Plantão 2: Hoje (em execução pela médica demo)
    INSERT INTO plantaopro.plantoes(
        id, tenant_id, cliente_id, hospital_id, especialidade_id, setor_id, setor,
        data_inicio, data_fim, valor, vagas, vagas_disponiveis, tipo, status, observacoes, reg_status, created_by
    ) VALUES (
        v_shift2_id, v_tenant_id, v_client_id, v_unit_id, v_specialty_id, v_sector_id, 'Pronto Socorro Adulto',
        current_date + time '07:00',
        current_date + time '19:00',
        1500.00, 1, 0, 'PRESENCIAL', 'confirmado', 'Plantão em andamento com check-in da médica demo realizado.', 'A', v_manager_user_id
    ) ON CONFLICT (id) DO UPDATE SET
        tenant_id = EXCLUDED.tenant_id,
        cliente_id = EXCLUDED.cliente_id,
        hospital_id = EXCLUDED.hospital_id,
        especialidade_id = EXCLUDED.especialidade_id,
        data_inicio = EXCLUDED.data_inicio,
        data_fim = EXCLUDED.data_fim,
        valor = EXCLUDED.valor,
        vagas = EXCLUDED.vagas,
        vagas_disponiveis = EXCLUDED.vagas_disponiveis,
        status = EXCLUDED.status,
        observacoes = EXCLUDED.observacoes,
        reg_status = 'A';

    -- Plantão 3: Amanhã (atribuído à médica demo + 1 vaga com convite pendente)
    INSERT INTO plantaopro.plantoes(
        id, tenant_id, cliente_id, hospital_id, especialidade_id, setor_id, setor,
        data_inicio, data_fim, valor, vagas, vagas_disponiveis, tipo, status, observacoes, reg_status, created_by
    ) VALUES (
        v_shift3_id, v_tenant_id, v_client_id, v_unit_id, v_specialty_id, v_sector_id, 'Pronto Socorro Adulto',
        (current_date + interval '1 day') + time '19:00',
        (current_date + interval '2 days') + time '07:00',
        1650.00, 2, 1, 'PRESENCIAL', 'aberto', 'Plantão noturno com 1 vaga atribuída e 1 vaga em convite de cobertura.', 'A', v_manager_user_id
    ) ON CONFLICT (id) DO UPDATE SET
        tenant_id = EXCLUDED.tenant_id,
        cliente_id = EXCLUDED.cliente_id,
        hospital_id = EXCLUDED.hospital_id,
        especialidade_id = EXCLUDED.especialidade_id,
        data_inicio = EXCLUDED.data_inicio,
        data_fim = EXCLUDED.data_fim,
        valor = EXCLUDED.valor,
        vagas = EXCLUDED.vagas,
        vagas_disponiveis = EXCLUDED.vagas_disponiveis,
        status = EXCLUDED.status,
        observacoes = EXCLUDED.observacoes,
        reg_status = 'A';

    -- Plantão 4: +3 dias (DESCOBERTO para o gestor agir)
    INSERT INTO plantaopro.plantoes(
        id, tenant_id, cliente_id, hospital_id, especialidade_id, setor_id, setor,
        data_inicio, data_fim, valor, vagas, vagas_disponiveis, tipo, status, observacoes, reg_status, created_by
    ) VALUES (
        v_shift4_id, v_tenant_id, v_client_id, v_unit_id, v_specialty_id, v_sector_id, 'Pronto Socorro Adulto',
        (current_date + interval '3 days') + time '07:00',
        (current_date + interval '3 days') + time '19:00',
        1500.00, 1, 1, 'PRESENCIAL', 'aberto', 'Plantão sem médico escalado — vaga descoberta requer ação do gestor.', 'A', v_manager_user_id
    ) ON CONFLICT (id) DO UPDATE SET
        tenant_id = EXCLUDED.tenant_id,
        cliente_id = EXCLUDED.cliente_id,
        hospital_id = EXCLUDED.hospital_id,
        especialidade_id = EXCLUDED.especialidade_id,
        data_inicio = EXCLUDED.data_inicio,
        data_fim = EXCLUDED.data_fim,
        valor = EXCLUDED.valor,
        vagas = EXCLUDED.vagas,
        vagas_disponiveis = EXCLUDED.vagas_disponiveis,
        status = EXCLUDED.status,
        observacoes = EXCLUDED.observacoes,
        reg_status = 'A';

    -- 7. Escalas correspondentes
    -- Escala 1 (Ontem): Realizada pela médica demo
    INSERT INTO plantaopro.escalas(id, tenant_id, cliente_id, plantao_id, medico_id, status, justificativa, created_by, reg_status, data_inicio, data_fim)
    VALUES (v_scale1_id, v_tenant_id, v_client_id, v_shift1_id, v_physician_medico_id, 'realizada', 'Plantão cumprido normalmente', v_manager_user_id, 'A', (current_date - interval '1 day') + time '07:00', (current_date - interval '1 day') + time '19:00')
    ON CONFLICT (id) DO UPDATE SET
        tenant_id = EXCLUDED.tenant_id,
        cliente_id = EXCLUDED.cliente_id,
        plantao_id = EXCLUDED.plantao_id,
        medico_id = EXCLUDED.medico_id,
        status = EXCLUDED.status,
        reg_status = 'A';

    -- Escala 2 (Hoje): Confirmada / Em execução pela médica demo
    INSERT INTO plantaopro.escalas(id, tenant_id, cliente_id, plantao_id, medico_id, status, justificativa, created_by, reg_status, data_inicio, data_fim)
    VALUES (v_scale2_id, v_tenant_id, v_client_id, v_shift2_id, v_physician_medico_id, 'confirmada', 'Plantão do dia confirmado', v_manager_user_id, 'A', current_date + time '07:00', current_date + time '19:00')
    ON CONFLICT (id) DO UPDATE SET
        tenant_id = EXCLUDED.tenant_id,
        cliente_id = EXCLUDED.cliente_id,
        plantao_id = EXCLUDED.plantao_id,
        medico_id = EXCLUDED.medico_id,
        status = EXCLUDED.status,
        reg_status = 'A';

    -- Escala 3 (Amanhã): Atribuída / Solicitada à médica demo
    INSERT INTO plantaopro.escalas(id, tenant_id, cliente_id, plantao_id, medico_id, status, justificativa, created_by, reg_status, data_inicio, data_fim)
    VALUES (v_scale3_id, v_tenant_id, v_client_id, v_shift3_id, v_physician_medico_id, 'solicitada', 'Atribuído à médica demo para confirmação', v_manager_user_id, 'A', (current_date + interval '1 day') + time '19:00', (current_date + interval '2 days') + time '07:00')
    ON CONFLICT (id) DO UPDATE SET
        tenant_id = EXCLUDED.tenant_id,
        cliente_id = EXCLUDED.cliente_id,
        plantao_id = EXCLUDED.plantao_id,
        medico_id = EXCLUDED.medico_id,
        status = EXCLUDED.status,
        reg_status = 'A';

    -- 8. Check-ins de Presença (Execução e Conferência)
    IF to_regclass('plantaopro.medico_checkins') IS NOT NULL THEN
        -- Check-in 1: Ontem (completo com check-out, pendente de conferência)
        INSERT INTO plantaopro.medico_checkins(
            id, tenant_id, medico_id, escala_id, origem,
            checkin_em, checkin_recebido_em, checkout_em, checkout_recebido_em,
            status_conferencia, timezone_contexto, versao
        ) VALUES (
            v_checkin1_id, v_tenant_id, v_physician_medico_id, v_scale1_id, 'WEB',
            (current_date - interval '1 day') + time '07:02',
            (current_date - interval '1 day') + time '07:02',
            (current_date - interval '1 day') + time '19:04',
            (current_date - interval '1 day') + time '19:04',
            'PENDENTE', 'America/Sao_Paulo', 2
        ) ON CONFLICT (id) DO UPDATE SET
            tenant_id = EXCLUDED.tenant_id,
            medico_id = EXCLUDED.medico_id,
            escala_id = EXCLUDED.escala_id,
            checkout_em = EXCLUDED.checkout_em,
            status_conferencia = 'PENDENTE';

        -- Check-in 2: Hoje (em execução: checkin feito, sem checkout)
        INSERT INTO plantaopro.medico_checkins(
            id, tenant_id, medico_id, escala_id, origem,
            checkin_em, checkin_recebido_em, checkout_em, checkout_recebido_em,
            status_conferencia, timezone_contexto, versao
        ) VALUES (
            v_checkin2_id, v_tenant_id, v_physician_medico_id, v_scale2_id, 'WEB',
            current_date + time '07:01',
            current_date + time '07:01',
            NULL, NULL,
            'REGISTRO_INCOMPLETO', 'America/Sao_Paulo', 1
        ) ON CONFLICT (id) DO UPDATE SET
            tenant_id = EXCLUDED.tenant_id,
            medico_id = EXCLUDED.medico_id,
            escala_id = EXCLUDED.escala_id,
            checkout_em = NULL,
            status_conferencia = 'REGISTRO_INCOMPLETO';
    END IF;

    -- 9. Fechamento de Plantão (Conferência)
    IF to_regclass('plantaopro.fechamento_plantao') IS NOT NULL THEN
        INSERT INTO plantaopro.fechamento_plantao(
            id, tenant_id, plantao_id, status, iniciado_por, iniciado_em, versao
        ) VALUES (
            v_closing1_id, v_tenant_id, v_shift1_id, 'EM_CONFERENCIA', v_manager_user_id, (current_date - interval '1 day') + time '19:15', 1
        ) ON CONFLICT (id) DO UPDATE SET
            tenant_id = EXCLUDED.tenant_id,
            plantao_id = EXCLUDED.plantao_id,
            status = 'EM_CONFERENCIA';

        IF to_regclass('plantaopro.fechamento_plantao_escalas') IS NOT NULL THEN
            INSERT INTO plantaopro.fechamento_plantao_escalas(
                id, tenant_id, fechamento_id, escala_id, presenca, horas_previstas, horas_realizadas,
                valor_previsto, valor_calculado, medico_id, plantao_id, status_escala
            ) VALUES (
                'd3f6584c-2c64-4e5a-9ea9-4e1428647583', v_tenant_id, v_closing1_id, v_scale1_id, true, 12, 12,
                1500.00, 1500.00, v_physician_medico_id, v_shift1_id, 'REALIZADA'
            ) ON CONFLICT (tenant_id, fechamento_id, escala_id) DO NOTHING;
        END IF;
    END IF;

    -- 10. Ocorrência Operacional aberta ligada a um plantão
    IF to_regclass('plantaopro.ocorrencias_operacionais') IS NOT NULL THEN
        INSERT INTO plantaopro.ocorrencias_operacionais(
            id, tenant_id, unidade_id, plantao_id, titulo, descricao,
            categoria, prioridade, situacao, solicitante_id, responsavel_id, criado_em, reg_status
        ) VALUES (
            v_incident2_id, v_tenant_id, v_unit_id, v_shift2_id,
            'Atraso na transição do plantão diurno',
            'Médica demo comunicou alta demanda no atendimento de emergência; transição e rendição de turno em acompanhamento.',
            'ATRASO', 'ALTA', 'ABERTA', v_manager_user_id, v_manager_user_id, current_date + time '08:30', 'A'
        ) ON CONFLICT (id) DO UPDATE SET
            tenant_id = EXCLUDED.tenant_id,
            unidade_id = EXCLUDED.unidade_id,
            plantao_id = EXCLUDED.plantao_id,
            titulo = EXCLUDED.titulo,
            descricao = EXCLUDED.descricao,
            categoria = EXCLUDED.categoria,
            prioridade = EXCLUDED.prioridade,
            situacao = 'ABERTA',
            reg_status = 'A';
    END IF;

    -- 11. Convite / Pendência de Cobertura
    IF to_regclass('plantaopro.cobertura_convites') IS NOT NULL THEN
        INSERT INTO plantaopro.cobertura_convites(
            id, tenant_id, plantao_id, medico_id, status, mensagem, criado_por, criado_em
        ) VALUES (
            v_invite3_id, v_tenant_id, v_shift3_id, v_physician_medico_id, 'PENDENTE',
            'Convite para plantão de amanhã à noite na Santa Casa Central (Pronto Socorro). Favor confirmar.',
            v_manager_user_id, now()
        ) ON CONFLICT (id) DO UPDATE SET
            tenant_id = EXCLUDED.tenant_id,
            plantao_id = EXCLUDED.plantao_id,
            medico_id = EXCLUDED.medico_id,
            status = 'PENDENTE';
    END IF;

    IF to_regclass('plantaopro.plantao_convites') IS NOT NULL THEN
        INSERT INTO plantaopro.plantao_convites(
            id, tenant_id, plantao_id, medico_id, usuario_id, status, mensagem, data_envio, reg_status
        ) VALUES (
            v_invite3_id, v_tenant_id, v_shift3_id, v_physician_medico_id, v_physician_user_id, 'enviado',
            'Convite para plantão de amanhã à noite na Santa Casa Central (Pronto Socorro).',
            now(), 'A'
        ) ON CONFLICT (id) DO UPDATE SET
            tenant_id = EXCLUDED.tenant_id,
            plantao_id = EXCLUDED.plantao_id,
            medico_id = EXCLUDED.medico_id,
            usuario_id = EXCLUDED.usuario_id,
            status = 'enviado',
            reg_status = 'A';
    END IF;

    RAISE NOTICE 'Seed de operação demonstrável concluído com sucesso no banco %.', v_db;
END
$seed$;

-- Relatório de validação do seed
SELECT p.id AS plantao_id,
       p.data_inicio::date AS data,
       p.data_inicio::time AS inicio,
       p.data_fim::time AS fim,
       p.vagas,
       p.vagas_disponiveis,
       p.status,
       COALESCE(m.nome, 'SEM MÉDICO (DESCOBERTO)') AS medico_escalado,
       COALESCE(c.status_conferencia, 'SEM CHECK-IN') AS conferencia
FROM plantaopro.plantoes p
LEFT JOIN plantaopro.escalas e ON e.plantao_id = p.id AND e.reg_status = 'A'
LEFT JOIN plantaopro.medicos m ON m.id = e.medico_id AND m.reg_status = 'A'
LEFT JOIN plantaopro.medico_checkins c ON c.escala_id = e.id
WHERE p.cliente_id = 'd3f6584c-2c64-4e5a-9ea9-4e1428647501'
  AND p.reg_status = 'A'
ORDER BY p.data_inicio;
