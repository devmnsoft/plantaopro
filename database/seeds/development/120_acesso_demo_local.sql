-- Seed OPT-IN de desenvolvimento: cria superadministrador e gestor de cliente teste.
-- NÃO faz parte da instalação canônica. Execute somente em banco local/descartável.
-- Uso (pgAdmin Query Tool no banco plantaopro, ou psql):
--   \i database/seeds/development/120_acesso_demo_local.sql
--
-- Contas (apenas desenvolvimento):
--   superadmin@mnsoft.example          / MnSoft!Demo2026#Admin
--   gestor@santacasa-demo.example      / SantaCasa!Demo2026#Gestor
--
-- O login da API consulta plantaopro.usuarios por lower(email) e valida senha_hash com BCrypt.
-- O instalador oficial NÃO cria usuário com senha conhecida; por isso o sistema recusa login
-- em instalação limpa até este script (ou --provision-demo) ser executado.

SET search_path TO plantaopro, public;

DO $seed$
DECLARE
    v_db text := current_database();
    v_client_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647501';
    v_tenant_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647502';
    v_plan_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647503';
    v_unit_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647504';
    v_subscription_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647505';
    v_super_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647510';
    v_manager_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647511';
    v_global_profile uuid;
    v_client_profile uuid;
    v_other_global text;
    v_has_col boolean;
BEGIN
    IF v_db IN ('postgres', 'template0', 'template1') THEN
        RAISE EXCEPTION 'Recusado: execute este seed no banco da aplicação (ex.: plantaopro), não em %.', v_db;
    END IF;

    IF to_regclass('plantaopro.usuarios') IS NULL OR to_regclass('plantaopro.perfis') IS NULL THEN
        RAISE EXCEPTION 'Schema de identidade ausente. Instale o banco antes (database/scrpt_completo.sql ou Tools.Database install).';
    END IF;

    PERFORM pg_advisory_xact_lock(7065262026);

    -- Compatibilidade com o SQL de login (AuthService): colunas esperadas em clientes/medicos.
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
        UPDATE plantaopro.clientes SET reg_status = coalesce(nullif(reg_status,''),'A') WHERE reg_status IS NULL OR btrim(reg_status)='';
        UPDATE plantaopro.clientes SET razao_social = coalesce(nullif(razao_social,''), nullif(nome,''), 'Cliente') WHERE razao_social IS NULL OR btrim(razao_social)='';
        UPDATE plantaopro.clientes SET nome_fantasia = coalesce(nullif(nome_fantasia,''), nullif(nome,''), razao_social) WHERE nome_fantasia IS NULL OR btrim(nome_fantasia)='';
    END IF;

    IF to_regclass('plantaopro.medicos') IS NOT NULL THEN
        ALTER TABLE plantaopro.medicos ADD COLUMN IF NOT EXISTS usuario_id uuid;
        ALTER TABLE plantaopro.medicos ADD COLUMN IF NOT EXISTS cpf text;
        ALTER TABLE plantaopro.medicos ADD COLUMN IF NOT EXISTS reg_status char(1) DEFAULT 'A';
    ELSE
        CREATE TABLE plantaopro.medicos (
            id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
            usuario_id uuid NULL,
            cpf text NULL,
            status text NOT NULL DEFAULT 'ATIVO',
            reg_status char(1) NOT NULL DEFAULT 'A',
            reg_date timestamptz NOT NULL DEFAULT now()
        );
    END IF;

    SELECT u.email INTO v_other_global
    FROM plantaopro.usuarios u
    JOIN plantaopro.usuarios_perfis up ON up.usuario_id = u.id AND up.reg_status = 'A'
    JOIN plantaopro.perfis p ON p.id = up.perfil_id AND p.reg_status = 'A'
    WHERE p.codigo = 'ADMINISTRADOR_GLOBAL'
      AND u.reg_status = 'A'
      AND lower(u.email) <> lower('superadmin@mnsoft.example')
    LIMIT 1;

    IF v_other_global IS NOT NULL THEN
        RAISE NOTICE 'Já existe administrador global (%). O superadmin de demonstração não será criado; o gestor demo ainda pode ser provisionado.', v_other_global;
    END IF;

    INSERT INTO plantaopro.perfis(tenant_id, cliente_id, codigo, nome, descricao, base_sistema, customizado, status, reg_status)
    SELECT NULL, NULL, 'ADMINISTRADOR_GLOBAL', 'Administrador global', 'Perfil canônico para demonstração local', true, false, 'ATIVO', 'A'
    WHERE NOT EXISTS (
        SELECT 1 FROM plantaopro.perfis
        WHERE codigo = 'ADMINISTRADOR_GLOBAL' AND tenant_id IS NULL AND reg_status = 'A'
    );

    INSERT INTO plantaopro.perfis(tenant_id, cliente_id, codigo, nome, descricao, base_sistema, customizado, status, reg_status)
    SELECT v_tenant_id, v_client_id, 'ADMINISTRADOR_CLIENTE', 'Administrador do cliente', 'Perfil canônico para demonstração local', true, false, 'ATIVO', 'A'
    WHERE NOT EXISTS (
        SELECT 1 FROM plantaopro.perfis
        WHERE codigo = 'ADMINISTRADOR_CLIENTE'
          AND tenant_id IS NOT DISTINCT FROM v_tenant_id
          AND reg_status = 'A'
    );

    SELECT id INTO v_global_profile
    FROM plantaopro.perfis
    WHERE codigo = 'ADMINISTRADOR_GLOBAL' AND tenant_id IS NULL AND reg_status = 'A'
    LIMIT 1;

    SELECT id INTO v_client_profile
    FROM plantaopro.perfis
    WHERE codigo = 'ADMINISTRADOR_CLIENTE'
      AND tenant_id IS NOT DISTINCT FROM v_tenant_id
      AND reg_status = 'A'
    LIMIT 1;

    IF v_client_profile IS NULL THEN
        SELECT id INTO v_client_profile
        FROM plantaopro.perfis
        WHERE codigo = 'ADMINISTRADOR_CLIENTE' AND reg_status = 'A'
        ORDER BY CASE WHEN tenant_id IS NULL THEN 1 ELSE 0 END, reg_date
        LIMIT 1;
    END IF;

    IF to_regclass('plantaopro.planos') IS NOT NULL THEN
        INSERT INTO plantaopro.planos(id)
        VALUES (v_plan_id)
        ON CONFLICT (id) DO NOTHING;
        UPDATE plantaopro.planos SET
            codigo = COALESCE(codigo, 'DEMO_LOCAL'),
            nome = COALESCE(nome, 'Plano demonstração local'),
            status = COALESCE(NULLIF(status,''), 'ATIVO')
        WHERE id = v_plan_id;
    END IF;

    IF to_regclass('plantaopro.tenants') IS NOT NULL THEN
        INSERT INTO plantaopro.tenants(id)
        VALUES (v_tenant_id)
        ON CONFLICT (id) DO NOTHING;
        UPDATE plantaopro.tenants SET
            tenant_id = COALESCE(tenant_id, v_tenant_id),
            codigo = COALESCE(codigo, 'santa-casa-demonstracao'),
            nome = COALESCE(nome, 'Santa Casa Demonstração'),
            status = COALESCE(NULLIF(status,''), 'ATIVO')
        WHERE id = v_tenant_id;
    END IF;

    IF to_regclass('plantaopro.clientes') IS NOT NULL THEN
        INSERT INTO plantaopro.clientes(id)
        VALUES (v_client_id)
        ON CONFLICT (id) DO NOTHING;
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

    IF to_regclass('plantaopro.assinaturas') IS NOT NULL THEN
        INSERT INTO plantaopro.assinaturas(id)
        VALUES (v_subscription_id)
        ON CONFLICT (id) DO NOTHING;
        UPDATE plantaopro.assinaturas SET
            tenant_id = COALESCE(tenant_id, v_tenant_id),
            codigo = COALESCE(codigo, 'CONTRATO_DEMO'),
            nome = COALESCE(nome, 'Contrato demonstrativo'),
            status = COALESCE(NULLIF(status,''), 'ATIVO')
        WHERE id = v_subscription_id;
    END IF;

    IF to_regclass('plantaopro.unidades') IS NOT NULL THEN
        INSERT INTO plantaopro.unidades(id)
        VALUES (v_unit_id)
        ON CONFLICT (id) DO NOTHING;
        UPDATE plantaopro.unidades SET
            tenant_id = COALESCE(tenant_id, v_tenant_id),
            codigo = COALESCE(codigo, 'UNIDADE_DEMO'),
            nome = COALESCE(nome, 'Unidade Central — Demonstração'),
            status = COALESCE(NULLIF(status,''), 'ATIVO')
        WHERE id = v_unit_id;
    END IF;

    IF to_regclass('plantaopro.tenant_modulos') IS NOT NULL THEN
        INSERT INTO plantaopro.tenant_modulos(id, tenant_id, codigo, nome, status)
        VALUES
            ('d3f6584c-2c64-4e5a-9ea9-4e1428647521', v_tenant_id, 'ESCALAS', 'Escalas', 'ATIVO'),
            ('d3f6584c-2c64-4e5a-9ea9-4e1428647522', v_tenant_id, 'EXECUCAO', 'Execução', 'ATIVO'),
            ('d3f6584c-2c64-4e5a-9ea9-4e1428647523', v_tenant_id, 'CONFERENCIA', 'Conferência', 'ATIVO')
        ON CONFLICT (id) DO NOTHING;
    END IF;

    IF v_other_global IS NULL THEN
        IF EXISTS (SELECT 1 FROM plantaopro.usuarios WHERE id = v_super_id OR lower(email) = lower('superadmin@mnsoft.example')) THEN
            UPDATE plantaopro.usuarios SET
                nome = 'Administrador MNSOFT — Demonstração',
                email = 'superadmin@mnsoft.example',
                email_normalizado = 'superadmin@mnsoft.example',
                senha_hash = '$2a$11$ZqHfNeWgejMB0D8rMXjJdeIT5LAomItKhK2f184rYL6M8gFIlkMsC',
                status = 'ATIVO',
                reg_status = 'A',
                senha_alteracao_obrigatoria = false,
                bloqueado_ate = NULL,
                tenant_id = NULL,
                cliente_id = NULL,
                reg_update = now()
            WHERE id = v_super_id OR lower(email) = lower('superadmin@mnsoft.example');
        ELSE
            INSERT INTO plantaopro.usuarios(
                id, tenant_id, cliente_id, nome, email, email_normalizado, senha_hash,
                status, reg_status, senha_alteracao_obrigatoria
            ) VALUES (
                v_super_id, NULL, NULL,
                'Administrador MNSOFT — Demonstração',
                'superadmin@mnsoft.example',
                'superadmin@mnsoft.example',
                '$2a$11$ZqHfNeWgejMB0D8rMXjJdeIT5LAomItKhK2f184rYL6M8gFIlkMsC',
                'ATIVO', 'A', false
            );
        END IF;

        INSERT INTO plantaopro.usuarios_perfis(tenant_id, cliente_id, usuario_id, perfil_id, reg_status)
        SELECT NULL, NULL, u.id, v_global_profile, 'A'
        FROM plantaopro.usuarios u
        WHERE lower(u.email) = lower('superadmin@mnsoft.example')
          AND NOT EXISTS (
              SELECT 1 FROM plantaopro.usuarios_perfis up
              WHERE up.usuario_id = u.id AND up.perfil_id = v_global_profile AND up.reg_status = 'A'
          );
    END IF;

    IF EXISTS (SELECT 1 FROM plantaopro.usuarios WHERE id = v_manager_id OR lower(email) = lower('gestor@santacasa-demo.example')) THEN
        UPDATE plantaopro.usuarios SET
            nome = 'Gestor Santa Casa — Demonstração',
            email = 'gestor@santacasa-demo.example',
            email_normalizado = 'gestor@santacasa-demo.example',
            senha_hash = '$2a$11$mNmgw83PBauw.5XsFVB5TuMB1.8OgFZ0SpfujQSuGzUgzwvs7d8S.',
            status = 'ATIVO',
            reg_status = 'A',
            senha_alteracao_obrigatoria = false,
            bloqueado_ate = NULL,
            tenant_id = v_tenant_id,
            cliente_id = v_client_id,
            reg_update = now()
        WHERE id = v_manager_id OR lower(email) = lower('gestor@santacasa-demo.example');
    ELSE
        INSERT INTO plantaopro.usuarios(
            id, tenant_id, cliente_id, nome, email, email_normalizado, senha_hash,
            status, reg_status, senha_alteracao_obrigatoria
        ) VALUES (
            v_manager_id, v_tenant_id, v_client_id,
            'Gestor Santa Casa — Demonstração',
            'gestor@santacasa-demo.example',
            'gestor@santacasa-demo.example',
            '$2a$11$mNmgw83PBauw.5XsFVB5TuMB1.8OgFZ0SpfujQSuGzUgzwvs7d8S.',
            'ATIVO', 'A', false
        );
    END IF;

    INSERT INTO plantaopro.usuarios_perfis(tenant_id, cliente_id, usuario_id, perfil_id, reg_status)
    SELECT v_tenant_id, v_client_id, u.id, v_client_profile, 'A'
    FROM plantaopro.usuarios u
    WHERE lower(u.email) = lower('gestor@santacasa-demo.example')
      AND v_client_profile IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM plantaopro.usuarios_perfis up
          WHERE up.usuario_id = u.id AND up.perfil_id = v_client_profile AND up.reg_status = 'A'
      );

    IF to_regclass('plantaopro.login_tentativas') IS NOT NULL THEN
        UPDATE plantaopro.login_tentativas
        SET bloqueado_ate = NULL, reg_update = now()
        WHERE usuario_id IN (v_super_id, v_manager_id)
          AND bloqueado_ate IS NOT NULL
          AND bloqueado_ate > now();
    END IF;

    RAISE NOTICE 'Seed de acesso local concluído no banco %.', v_db;
END
$seed$;

SELECT u.email,
       u.status,
       u.reg_status,
       left(u.senha_hash, 7) AS hash_prefix,
       string_agg(DISTINCT p.codigo, ', ' ORDER BY p.codigo) AS perfis
FROM plantaopro.usuarios u
LEFT JOIN plantaopro.usuarios_perfis up ON up.usuario_id = u.id AND up.reg_status = 'A'
LEFT JOIN plantaopro.perfis p ON p.id = up.perfil_id AND p.reg_status = 'A'
WHERE lower(u.email) IN ('superadmin@mnsoft.example', 'gestor@santacasa-demo.example')
GROUP BY u.email, u.status, u.reg_status, u.senha_hash
ORDER BY u.email;
