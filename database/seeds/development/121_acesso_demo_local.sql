-- Complemento OPT-IN: garante superadmin, gestor e médico fictício no banco local.
-- Pode rodar no banco plantaopro OU em postgres legado se o schema plantaopro.usuarios existir.
-- Contas (somente Development):
--   superadmin@mnsoft.example           / MnSoft!Demo2026#Admin
--   gestor@santacasa-demo.example       / SantaCasa!Demo2026#Gestor
--   medico@santacasa-demo.example       / Medico!Demo2026#Acesso

SET search_path TO plantaopro, public;

DO $seed$
DECLARE
    v_db text := current_database();
    v_client_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647501';
    v_tenant_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647502';
    v_super_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647510';
    v_manager_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647511';
    v_physician_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647512';
    v_global_profile uuid;
    v_client_profile uuid;
    v_medico_profile uuid;
BEGIN
    IF to_regclass('plantaopro.usuarios') IS NULL OR to_regclass('plantaopro.perfis') IS NULL THEN
        RAISE EXCEPTION 'Schema de identidade ausente em %. Instale o banco antes.', v_db;
    END IF;

    IF v_db IN ('template0', 'template1') THEN
        RAISE EXCEPTION 'Recusado: não execute seed em %.', v_db;
    END IF;

    PERFORM pg_advisory_xact_lock(7065262026);

    IF to_regclass('plantaopro.clientes') IS NOT NULL THEN
        ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS tenant_id uuid;
        ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS codigo text;
        ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS nome text;
        ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS razao_social varchar(200);
        ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS nome_fantasia varchar(200);
        ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS cnpj varchar(30);
        ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS status text DEFAULT 'ATIVO';
        ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS dados jsonb DEFAULT '{}'::jsonb;
        ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS reg_status char(1) DEFAULT 'A';
    END IF;

    IF to_regclass('plantaopro.medicos') IS NULL THEN
        CREATE TABLE plantaopro.medicos (
            id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
            usuario_id uuid NULL,
            cpf text NULL,
            status text NOT NULL DEFAULT 'ATIVO',
            reg_status char(1) NOT NULL DEFAULT 'A',
            reg_date timestamptz NOT NULL DEFAULT now()
        );
    ELSE
        ALTER TABLE plantaopro.medicos ADD COLUMN IF NOT EXISTS usuario_id uuid;
        ALTER TABLE plantaopro.medicos ADD COLUMN IF NOT EXISTS cpf text;
        ALTER TABLE plantaopro.medicos ADD COLUMN IF NOT EXISTS reg_status char(1) DEFAULT 'A';
    END IF;

    INSERT INTO plantaopro.perfis(tenant_id, cliente_id, codigo, nome, descricao, base_sistema, customizado, status, reg_status)
    SELECT NULL, NULL, 'ADMINISTRADOR_GLOBAL', 'Administrador global', 'Perfil canônico para demonstração local', true, false, 'ATIVO', 'A'
    WHERE NOT EXISTS (SELECT 1 FROM plantaopro.perfis WHERE codigo = 'ADMINISTRADOR_GLOBAL' AND tenant_id IS NULL AND reg_status = 'A');

    INSERT INTO plantaopro.perfis(tenant_id, cliente_id, codigo, nome, descricao, base_sistema, customizado, status, reg_status)
    SELECT v_tenant_id, v_client_id, 'ADMINISTRADOR_CLIENTE', 'Administrador do cliente', 'Perfil canônico para demonstração local', true, false, 'ATIVO', 'A'
    WHERE NOT EXISTS (
        SELECT 1 FROM plantaopro.perfis
        WHERE codigo = 'ADMINISTRADOR_CLIENTE' AND tenant_id IS NOT DISTINCT FROM v_tenant_id AND reg_status = 'A'
    );

    INSERT INTO plantaopro.perfis(tenant_id, cliente_id, codigo, nome, descricao, base_sistema, customizado, status, reg_status)
    SELECT v_tenant_id, v_client_id, 'MEDICO', 'Médico', 'Perfil canônico para demonstração local', true, false, 'ATIVO', 'A'
    WHERE NOT EXISTS (
        SELECT 1 FROM plantaopro.perfis
        WHERE codigo = 'MEDICO' AND tenant_id IS NOT DISTINCT FROM v_tenant_id AND reg_status = 'A'
    );

    SELECT id INTO v_global_profile FROM plantaopro.perfis WHERE codigo = 'ADMINISTRADOR_GLOBAL' AND tenant_id IS NULL AND reg_status = 'A' LIMIT 1;
    SELECT id INTO v_client_profile FROM plantaopro.perfis WHERE codigo = 'ADMINISTRADOR_CLIENTE' AND tenant_id IS NOT DISTINCT FROM v_tenant_id AND reg_status = 'A' LIMIT 1;
    SELECT id INTO v_medico_profile FROM plantaopro.perfis WHERE codigo = 'MEDICO' AND tenant_id IS NOT DISTINCT FROM v_tenant_id AND reg_status = 'A' LIMIT 1;

    IF to_regclass('plantaopro.clientes') IS NOT NULL THEN
        INSERT INTO plantaopro.clientes(id) VALUES (v_client_id) ON CONFLICT (id) DO NOTHING;
        UPDATE plantaopro.clientes SET
            tenant_id = COALESCE(tenant_id, v_tenant_id),
            codigo = COALESCE(codigo, 'SANTA_CASA_DEMONSTRACAO'),
            nome = COALESCE(nome, 'Santa Casa Demonstração'),
            razao_social = COALESCE(NULLIF(razao_social,''), 'Santa Casa Demonstração'),
            nome_fantasia = COALESCE(NULLIF(nome_fantasia,''), 'Santa Casa Demonstração'),
            status = COALESCE(NULLIF(status,''), 'ATIVO'),
            reg_status = COALESCE(NULLIF(reg_status,''), 'A')
        WHERE id = v_client_id;
    END IF;

    IF EXISTS (SELECT 1 FROM plantaopro.usuarios WHERE id = v_super_id OR lower(email) = lower('superadmin@mnsoft.example')) THEN
        UPDATE plantaopro.usuarios SET
            nome = 'Administrador MNSOFT — Demonstração',
            email = 'superadmin@mnsoft.example',
            email_normalizado = 'superadmin@mnsoft.example',
            senha_hash = '$2a$11$ZqHfNeWgejMB0D8rMXjJdeIT5LAomItKhK2f184rYL6M8gFIlkMsC',
            status = 'ATIVO', reg_status = 'A', senha_alteracao_obrigatoria = false,
            bloqueado_ate = NULL, tenant_id = NULL, cliente_id = NULL, reg_update = now()
        WHERE id = v_super_id OR lower(email) = lower('superadmin@mnsoft.example');
    ELSE
        INSERT INTO plantaopro.usuarios(id, tenant_id, cliente_id, nome, email, email_normalizado, senha_hash, status, reg_status, senha_alteracao_obrigatoria)
        VALUES (v_super_id, NULL, NULL, 'Administrador MNSOFT — Demonstração', 'superadmin@mnsoft.example', 'superadmin@mnsoft.example',
                '$2a$11$ZqHfNeWgejMB0D8rMXjJdeIT5LAomItKhK2f184rYL6M8gFIlkMsC', 'ATIVO', 'A', false);
    END IF;

    INSERT INTO plantaopro.usuarios_perfis(tenant_id, cliente_id, usuario_id, perfil_id, reg_status)
    SELECT NULL, NULL, u.id, v_global_profile, 'A'
    FROM plantaopro.usuarios u
    WHERE lower(u.email) = lower('superadmin@mnsoft.example')
      AND v_global_profile IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM plantaopro.usuarios_perfis up WHERE up.usuario_id = u.id AND up.perfil_id = v_global_profile AND up.reg_status = 'A');

    IF EXISTS (SELECT 1 FROM plantaopro.usuarios WHERE id = v_manager_id OR lower(email) = lower('gestor@santacasa-demo.example')) THEN
        UPDATE plantaopro.usuarios SET
            nome = 'Gestor Santa Casa — Demonstração',
            email = 'gestor@santacasa-demo.example',
            email_normalizado = 'gestor@santacasa-demo.example',
            senha_hash = '$2a$11$mNmgw83PBauw.5XsFVB5TuMB1.8OgFZ0SpfujQSuGzUgzwvs7d8S.',
            status = 'ATIVO', reg_status = 'A', senha_alteracao_obrigatoria = false,
            bloqueado_ate = NULL, tenant_id = v_tenant_id, cliente_id = v_client_id, reg_update = now()
        WHERE id = v_manager_id OR lower(email) = lower('gestor@santacasa-demo.example');
    ELSE
        INSERT INTO plantaopro.usuarios(id, tenant_id, cliente_id, nome, email, email_normalizado, senha_hash, status, reg_status, senha_alteracao_obrigatoria)
        VALUES (v_manager_id, v_tenant_id, v_client_id, 'Gestor Santa Casa — Demonstração', 'gestor@santacasa-demo.example', 'gestor@santacasa-demo.example',
                '$2a$11$mNmgw83PBauw.5XsFVB5TuMB1.8OgFZ0SpfujQSuGzUgzwvs7d8S.', 'ATIVO', 'A', false);
    END IF;

    INSERT INTO plantaopro.usuarios_perfis(tenant_id, cliente_id, usuario_id, perfil_id, reg_status)
    SELECT v_tenant_id, v_client_id, u.id, v_client_profile, 'A'
    FROM plantaopro.usuarios u
    WHERE lower(u.email) = lower('gestor@santacasa-demo.example')
      AND v_client_profile IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM plantaopro.usuarios_perfis up WHERE up.usuario_id = u.id AND up.perfil_id = v_client_profile AND up.reg_status = 'A');

    IF EXISTS (SELECT 1 FROM plantaopro.usuarios WHERE id = v_physician_id OR lower(email) = lower('medico@santacasa-demo.example')) THEN
        UPDATE plantaopro.usuarios SET
            nome = 'Dra. Ana Souza — Demonstração',
            email = 'medico@santacasa-demo.example',
            email_normalizado = 'medico@santacasa-demo.example',
            senha_hash = '$2b$11$IgKzuHNrOEWgc/ExL.InS.cP3WajR8y0RY9hW2uKtKpbydU57i82m',
            status = 'ATIVO', reg_status = 'A', senha_alteracao_obrigatoria = false,
            bloqueado_ate = NULL, tenant_id = v_tenant_id, cliente_id = v_client_id, reg_update = now()
        WHERE id = v_physician_id OR lower(email) = lower('medico@santacasa-demo.example');
    ELSE
        INSERT INTO plantaopro.usuarios(id, tenant_id, cliente_id, nome, email, email_normalizado, senha_hash, status, reg_status, senha_alteracao_obrigatoria)
        VALUES (v_physician_id, v_tenant_id, v_client_id, 'Dra. Ana Souza — Demonstração', 'medico@santacasa-demo.example', 'medico@santacasa-demo.example',
                '$2b$11$IgKzuHNrOEWgc/ExL.InS.cP3WajR8y0RY9hW2uKtKpbydU57i82m', 'ATIVO', 'A', false);
    END IF;

    INSERT INTO plantaopro.usuarios_perfis(tenant_id, cliente_id, usuario_id, perfil_id, reg_status)
    SELECT v_tenant_id, v_client_id, u.id, v_medico_profile, 'A'
    FROM plantaopro.usuarios u
    WHERE lower(u.email) = lower('medico@santacasa-demo.example')
      AND v_medico_profile IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM plantaopro.usuarios_perfis up WHERE up.usuario_id = u.id AND up.perfil_id = v_medico_profile AND up.reg_status = 'A');

    INSERT INTO plantaopro.medicos(id, usuario_id, cpf, status, reg_status)
    SELECT 'd3f6584c-2c64-4e5a-9ea9-4e1428647530', v_physician_id, '52998224725', 'ATIVO', 'A'
    WHERE NOT EXISTS (SELECT 1 FROM plantaopro.medicos WHERE usuario_id = v_physician_id AND coalesce(reg_status,'A') = 'A');

    RAISE NOTICE 'Acesso local confirmado no banco %.', v_db;
END
$seed$;

SELECT u.email, u.status, u.reg_status, left(u.senha_hash, 7) AS hash_prefix,
       string_agg(DISTINCT p.codigo, ', ' ORDER BY p.codigo) AS perfis
FROM plantaopro.usuarios u
LEFT JOIN plantaopro.usuarios_perfis up ON up.usuario_id = u.id AND up.reg_status = 'A'
LEFT JOIN plantaopro.perfis p ON p.id = up.perfil_id AND p.reg_status = 'A'
WHERE lower(u.email) IN ('superadmin@mnsoft.example','gestor@santacasa-demo.example','medico@santacasa-demo.example')
GROUP BY u.email, u.status, u.reg_status, u.senha_hash
ORDER BY u.email;
