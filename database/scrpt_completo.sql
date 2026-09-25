-- PlantãoPro - schema SQL puro para banco de destino já existente
-- Versão do schema: v2.19.1
-- PostgreSQL suportado: 16
-- Data de geração: 2026-09-24
-- Execução oficial:
--   psql \
--     -v ON_ERROR_STOP=1 \
--     -h localhost \
--     -p 5432 \
--     -U postgres \
--     -d plantaopro \
--     -f database/scrpt_completo.sql
-- Para criar roles e banco automaticamente, execute database/instalar_plantaopro.psql.
-- Este arquivo não contém credenciais reais, senhas administrativas, tokens ou connection strings.
-- Não use scripts de demonstração em produção.

CREATE EXTENSION IF NOT EXISTS pgcrypto;
DO $$
DECLARE
    v_schema text;
    v_relocatable boolean;
BEGIN
    SELECT n.nspname, e.extrelocatable
      INTO v_schema, v_relocatable
      FROM pg_extension e
      JOIN pg_namespace n ON n.oid = e.extnamespace
     WHERE e.extname = 'unaccent';

    IF v_schema IS NULL THEN
        CREATE EXTENSION IF NOT EXISTS unaccent WITH SCHEMA public;
    ELSIF v_schema <> 'public' AND coalesce(v_relocatable, false) THEN
        ALTER EXTENSION unaccent SET SCHEMA public;
    END IF;
END $$;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE SCHEMA IF NOT EXISTS plantaopro;
SET search_path TO plantaopro, public;

-- ============================================================
-- Seção 03 — Schema canônico de instalação limpa v1.18.8
-- ============================================================

-- SOURCE: database/schema/000_extensions_schema.sql
-- SOURCE-SHA256: c101f4eb90ed73d2ad5aefb9406408a44fd98eb03dc0a4c4b77aa27269be5776
-- v1.18.7 extensões e schema canônico
CREATE EXTENSION IF NOT EXISTS pgcrypto;
DO $$
DECLARE
    v_schema text;
    v_relocatable boolean;
BEGIN
    SELECT n.nspname, e.extrelocatable
      INTO v_schema, v_relocatable
      FROM pg_extension e
      JOIN pg_namespace n ON n.oid = e.extnamespace
     WHERE e.extname = 'unaccent';

    IF v_schema IS NULL THEN
        CREATE EXTENSION IF NOT EXISTS unaccent WITH SCHEMA public;
    ELSIF v_schema <> 'public' AND coalesce(v_relocatable, false) THEN
        ALTER EXTENSION unaccent SET SCHEMA public;
    END IF;
END $$;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE SCHEMA IF NOT EXISTS plantaopro;
SET search_path TO plantaopro, public;

-- SOURCE: database/schema/000_schema_canonico_base.sql
-- SOURCE-SHA256: f64b7628592c32c42c12bbdbed02c8549cd131bb2a82c6af3e4736cd39e5b57e
-- v1.18.6 schema canonico base: permissões/perfis/acessos
SET search_path TO plantaopro, public;

CREATE TABLE IF NOT EXISTS plantaopro.modulos_sistema (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), codigo text NOT NULL, nome text NOT NULL, descricao text NOT NULL DEFAULT '', ordem int NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'ATIVO', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, created_by uuid NULL, updated_by uuid NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.acoes_sistema (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), codigo text NOT NULL, nome text NOT NULL, descricao text NOT NULL DEFAULT '', ordem int NOT NULL DEFAULT 0, sensivel boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'ATIVO', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, created_by uuid NULL, updated_by uuid NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.permissoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), nome text NOT NULL, descricao text NULL, modulo text NULL, acao text NULL, modulo_id uuid NULL, acao_id uuid NULL, codigo text NULL, sensivel boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'ATIVO', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, created_by uuid NULL, updated_by uuid NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.perfil_permissoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), perfil_id uuid NOT NULL, permissao_id uuid NOT NULL, permitido boolean NOT NULL DEFAULT true, bloqueado_por_plano boolean NOT NULL DEFAULT false, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, created_by uuid NULL, updated_by uuid NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.usuarios_perfis (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NOT NULL, perfil_id uuid NOT NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, created_by uuid NULL, updated_by uuid NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.usuario_permissoes_especiais (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NOT NULL, permissao_id uuid NOT NULL, permitido boolean NOT NULL DEFAULT true, justificativa text NOT NULL DEFAULT '', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, created_by uuid NULL, updated_by uuid NULL
);

-- compat: ALTER TABLE bloco obrigatório para bases parciais/legadas
ALTER TABLE plantaopro.modulos_sistema ADD COLUMN IF NOT EXISTS codigo text, ADD COLUMN IF NOT EXISTS nome text, ADD COLUMN IF NOT EXISTS descricao text DEFAULT '', ADD COLUMN IF NOT EXISTS ordem int DEFAULT 0, ADD COLUMN IF NOT EXISTS status text DEFAULT 'ATIVO', ADD COLUMN IF NOT EXISTS reg_status char(1) DEFAULT 'A', ADD COLUMN IF NOT EXISTS reg_date timestamptz DEFAULT now(), ADD COLUMN IF NOT EXISTS reg_update timestamptz, ADD COLUMN IF NOT EXISTS created_by uuid, ADD COLUMN IF NOT EXISTS updated_by uuid;
ALTER TABLE plantaopro.acoes_sistema ADD COLUMN IF NOT EXISTS codigo text, ADD COLUMN IF NOT EXISTS nome text, ADD COLUMN IF NOT EXISTS descricao text DEFAULT '', ADD COLUMN IF NOT EXISTS ordem int DEFAULT 0, ADD COLUMN IF NOT EXISTS sensivel boolean DEFAULT false, ADD COLUMN IF NOT EXISTS status text DEFAULT 'ATIVO', ADD COLUMN IF NOT EXISTS reg_status char(1) DEFAULT 'A', ADD COLUMN IF NOT EXISTS reg_date timestamptz DEFAULT now(), ADD COLUMN IF NOT EXISTS reg_update timestamptz, ADD COLUMN IF NOT EXISTS created_by uuid, ADD COLUMN IF NOT EXISTS updated_by uuid;
ALTER TABLE plantaopro.permissoes ADD COLUMN IF NOT EXISTS nome text, ADD COLUMN IF NOT EXISTS descricao text, ADD COLUMN IF NOT EXISTS modulo text, ADD COLUMN IF NOT EXISTS acao text, ADD COLUMN IF NOT EXISTS modulo_id uuid, ADD COLUMN IF NOT EXISTS acao_id uuid, ADD COLUMN IF NOT EXISTS codigo text, ADD COLUMN IF NOT EXISTS sensivel boolean DEFAULT false, ADD COLUMN IF NOT EXISTS status text DEFAULT 'ATIVO', ADD COLUMN IF NOT EXISTS reg_status char(1) DEFAULT 'A', ADD COLUMN IF NOT EXISTS reg_date timestamptz DEFAULT now(), ADD COLUMN IF NOT EXISTS reg_update timestamptz, ADD COLUMN IF NOT EXISTS created_by uuid, ADD COLUMN IF NOT EXISTS updated_by uuid;
ALTER TABLE plantaopro.perfil_permissoes ADD COLUMN IF NOT EXISTS perfil_id uuid, ADD COLUMN IF NOT EXISTS permissao_id uuid, ADD COLUMN IF NOT EXISTS permitido boolean DEFAULT true, ADD COLUMN IF NOT EXISTS bloqueado_por_plano boolean DEFAULT false, ADD COLUMN IF NOT EXISTS reg_status char(1) DEFAULT 'A', ADD COLUMN IF NOT EXISTS reg_date timestamptz DEFAULT now(), ADD COLUMN IF NOT EXISTS reg_update timestamptz, ADD COLUMN IF NOT EXISTS created_by uuid, ADD COLUMN IF NOT EXISTS updated_by uuid;
ALTER TABLE plantaopro.usuarios_perfis ADD COLUMN IF NOT EXISTS tenant_id uuid, ADD COLUMN IF NOT EXISTS cliente_id uuid, ADD COLUMN IF NOT EXISTS usuario_id uuid, ADD COLUMN IF NOT EXISTS perfil_id uuid, ADD COLUMN IF NOT EXISTS reg_status char(1) DEFAULT 'A', ADD COLUMN IF NOT EXISTS reg_date timestamptz DEFAULT now(), ADD COLUMN IF NOT EXISTS reg_update timestamptz, ADD COLUMN IF NOT EXISTS created_by uuid, ADD COLUMN IF NOT EXISTS updated_by uuid;
ALTER TABLE plantaopro.usuario_permissoes_especiais ADD COLUMN IF NOT EXISTS tenant_id uuid, ADD COLUMN IF NOT EXISTS cliente_id uuid, ADD COLUMN IF NOT EXISTS usuario_id uuid, ADD COLUMN IF NOT EXISTS permissao_id uuid, ADD COLUMN IF NOT EXISTS permitido boolean DEFAULT true, ADD COLUMN IF NOT EXISTS justificativa text DEFAULT '', ADD COLUMN IF NOT EXISTS reg_status char(1) DEFAULT 'A', ADD COLUMN IF NOT EXISTS reg_date timestamptz DEFAULT now(), ADD COLUMN IF NOT EXISTS reg_update timestamptz, ADD COLUMN IF NOT EXISTS created_by uuid, ADD COLUMN IF NOT EXISTS updated_by uuid;

UPDATE plantaopro.permissoes SET codigo = upper(regexp_replace(unaccent(coalesce(nullif(codigo,''), nullif(nome,''), id::text)::text), '[^A-Za-z0-9]+', '_', 'g')) WHERE codigo IS NULL OR btrim(codigo)='';
UPDATE plantaopro.permissoes SET modulo = coalesce(nullif(modulo,''), split_part(codigo,'_',1), 'GERAL'), acao = coalesce(nullif(acao,''), nullif(array_to_string((regexp_split_to_array(codigo,'_'))[2:array_length(regexp_split_to_array(codigo,'_'),1)], '_'), ''), 'ACESSAR'), nome = coalesce(nullif(nome,''), codigo), descricao = coalesce(descricao,''), sensivel = coalesce(sensivel,false), status = coalesce(nullif(status,''),'ATIVO'), reg_status = coalesce(nullif(reg_status,''),'A'), reg_date = coalesce(reg_date, now());
WITH dup AS (SELECT id, row_number() OVER (PARTITION BY lower(codigo), reg_status ORDER BY reg_date, id) rn FROM plantaopro.permissoes WHERE reg_status='A') UPDATE plantaopro.permissoes p SET codigo = p.codigo || '_' || left(p.id::text,8), reg_update=now() FROM dup WHERE dup.id=p.id AND dup.rn>1;
INSERT INTO plantaopro.modulos_sistema(codigo,nome) SELECT DISTINCT upper(regexp_replace(unaccent(modulo::text), '[^A-Za-z0-9]+', '_', 'g')), modulo FROM plantaopro.permissoes p WHERE p.modulo IS NOT NULL AND NOT EXISTS (SELECT 1 FROM plantaopro.modulos_sistema m WHERE lower(m.codigo)=lower(upper(regexp_replace(unaccent(p.modulo::text), '[^A-Za-z0-9]+', '_', 'g'))) AND m.reg_status='A');
INSERT INTO plantaopro.acoes_sistema(codigo,nome) SELECT DISTINCT upper(regexp_replace(unaccent(acao::text), '[^A-Za-z0-9]+', '_', 'g')), acao FROM plantaopro.permissoes p WHERE p.acao IS NOT NULL AND NOT EXISTS (SELECT 1 FROM plantaopro.acoes_sistema a WHERE lower(a.codigo)=lower(upper(regexp_replace(unaccent(p.acao::text), '[^A-Za-z0-9]+', '_', 'g'))) AND a.reg_status='A');
UPDATE plantaopro.permissoes p SET modulo_id=m.id FROM plantaopro.modulos_sistema m WHERE p.modulo_id IS NULL AND lower(m.codigo)=lower(upper(regexp_replace(unaccent(p.modulo::text), '[^A-Za-z0-9]+', '_', 'g'))) AND m.reg_status='A';
UPDATE plantaopro.permissoes p SET acao_id=a.id FROM plantaopro.acoes_sistema a WHERE p.acao_id IS NULL AND lower(a.codigo)=lower(upper(regexp_replace(unaccent(p.acao::text), '[^A-Za-z0-9]+', '_', 'g'))) AND a.reg_status='A';
DO $$ BEGIN IF EXISTS (SELECT 1 FROM plantaopro.permissoes WHERE codigo IS NULL OR modulo_id IS NULL OR acao_id IS NULL) THEN RAISE EXCEPTION 'Permissões canônicas inválidas: codigo/modulo_id/acao_id nulos'; END IF; END $$;
ALTER TABLE plantaopro.permissoes ALTER COLUMN codigo SET NOT NULL, ALTER COLUMN modulo_id SET NOT NULL, ALTER COLUMN acao_id SET NOT NULL, ALTER COLUMN nome SET NOT NULL, ALTER COLUMN descricao SET DEFAULT '', ALTER COLUMN sensivel SET DEFAULT false, ALTER COLUMN sensivel SET NOT NULL, ALTER COLUMN status SET DEFAULT 'ATIVO', ALTER COLUMN status SET NOT NULL, ALTER COLUMN reg_status SET DEFAULT 'A', ALTER COLUMN reg_status SET NOT NULL, ALTER COLUMN reg_date SET DEFAULT now(), ALTER COLUMN reg_date SET NOT NULL;
ALTER TABLE plantaopro.modulos_sistema ALTER COLUMN codigo SET NOT NULL, ALTER COLUMN nome SET NOT NULL, ALTER COLUMN descricao SET DEFAULT '', ALTER COLUMN descricao SET NOT NULL, ALTER COLUMN status SET DEFAULT 'ATIVO', ALTER COLUMN status SET NOT NULL, ALTER COLUMN reg_status SET DEFAULT 'A', ALTER COLUMN reg_status SET NOT NULL;
ALTER TABLE plantaopro.acoes_sistema ALTER COLUMN codigo SET NOT NULL, ALTER COLUMN nome SET NOT NULL, ALTER COLUMN descricao SET DEFAULT '', ALTER COLUMN descricao SET NOT NULL, ALTER COLUMN sensivel SET DEFAULT false, ALTER COLUMN sensivel SET NOT NULL, ALTER COLUMN status SET DEFAULT 'ATIVO', ALTER COLUMN status SET NOT NULL, ALTER COLUMN reg_status SET DEFAULT 'A', ALTER COLUMN reg_status SET NOT NULL;
DO $$
DECLARE
    v_has_pp_reg_status boolean;
    v_has_pp_reg_date boolean;
    v_has_pp_permitido boolean;
    v_has_pp_bloqueado_por_plano boolean;
    v_has_up_tenant_id boolean;
    v_has_up_cliente_id boolean;
    v_has_up_reg_status boolean;
    v_has_up_reg_date boolean;
    v_sql text;
BEGIN
    IF to_regclass('plantaopro.perfis_permissoes') IS NOT NULL THEN
        SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'plantaopro' AND table_name = 'perfis_permissoes' AND column_name = 'reg_status') INTO v_has_pp_reg_status;
        SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'plantaopro' AND table_name = 'perfis_permissoes' AND column_name = 'reg_date') INTO v_has_pp_reg_date;
        SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'plantaopro' AND table_name = 'perfis_permissoes' AND column_name = 'permitido') INTO v_has_pp_permitido;
        SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'plantaopro' AND table_name = 'perfis_permissoes' AND column_name = 'bloqueado_por_plano') INTO v_has_pp_bloqueado_por_plano;

        v_sql := format($sql$
            INSERT INTO plantaopro.perfil_permissoes(
                perfil_id,
                permissao_id,
                permitido,
                bloqueado_por_plano,
                reg_status,
                reg_date
            )
            SELECT
                pp.perfil_id,
                pp.permissao_id,
                %s,
                %s,
                %s,
                %s
            FROM plantaopro.perfis_permissoes pp
            WHERE NOT EXISTS (
                SELECT 1
                FROM plantaopro.perfil_permissoes x
                WHERE x.perfil_id = pp.perfil_id
                  AND x.permissao_id = pp.permissao_id
                  AND x.reg_status = 'A'
            )
        $sql$,
            CASE WHEN v_has_pp_permitido THEN 'coalesce(pp.permitido, true)' ELSE 'true' END,
            CASE WHEN v_has_pp_bloqueado_por_plano THEN 'coalesce(pp.bloqueado_por_plano, false)' ELSE 'false' END,
            CASE WHEN v_has_pp_reg_status THEN 'coalesce(pp.reg_status, ''A'')' ELSE '''A''' END,
            CASE WHEN v_has_pp_reg_date THEN 'coalesce(pp.reg_date, now())' ELSE 'now()' END
        );
        EXECUTE v_sql;
    END IF;

    IF to_regclass('plantaopro.usuario_perfis') IS NOT NULL THEN
        SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'plantaopro' AND table_name = 'usuario_perfis' AND column_name = 'tenant_id') INTO v_has_up_tenant_id;
        SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'plantaopro' AND table_name = 'usuario_perfis' AND column_name = 'cliente_id') INTO v_has_up_cliente_id;
        SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'plantaopro' AND table_name = 'usuario_perfis' AND column_name = 'reg_status') INTO v_has_up_reg_status;
        SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'plantaopro' AND table_name = 'usuario_perfis' AND column_name = 'reg_date') INTO v_has_up_reg_date;

        v_sql := format($sql$
            INSERT INTO plantaopro.usuarios_perfis(
                usuario_id,
                perfil_id,
                tenant_id,
                cliente_id,
                reg_status,
                reg_date
            )
            SELECT
                up.usuario_id,
                up.perfil_id,
                %s,
                %s,
                %s,
                %s
            FROM plantaopro.usuario_perfis up
            WHERE NOT EXISTS (
                SELECT 1
                FROM plantaopro.usuarios_perfis x
                WHERE x.usuario_id = up.usuario_id
                  AND x.perfil_id = up.perfil_id
                  AND x.reg_status = 'A'
            )
        $sql$,
            CASE WHEN v_has_up_tenant_id THEN 'up.tenant_id' ELSE 'NULL::uuid' END,
            CASE WHEN v_has_up_cliente_id THEN 'up.cliente_id' ELSE 'NULL::uuid' END,
            CASE WHEN v_has_up_reg_status THEN 'coalesce(up.reg_status, ''A'')' ELSE '''A''' END,
            CASE WHEN v_has_up_reg_date THEN 'coalesce(up.reg_date, now())' ELSE 'now()' END
        );
        EXECUTE v_sql;
    END IF;
END $$;
CREATE UNIQUE INDEX IF NOT EXISTS ux_modulos_sistema_codigo ON plantaopro.modulos_sistema(lower(codigo)) WHERE reg_status='A';
CREATE UNIQUE INDEX IF NOT EXISTS ux_acoes_sistema_codigo ON plantaopro.acoes_sistema(lower(codigo)) WHERE reg_status='A';
CREATE UNIQUE INDEX IF NOT EXISTS ux_permissoes_codigo ON plantaopro.permissoes(lower(codigo)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_permissoes_modulo_status_regdate ON plantaopro.permissoes(modulo_id,status,reg_date);
CREATE UNIQUE INDEX IF NOT EXISTS ux_perfil_permissoes_perfil_permissao ON plantaopro.perfil_permissoes(perfil_id,permissao_id) WHERE reg_status='A';
CREATE UNIQUE INDEX IF NOT EXISTS ux_usuarios_perfis_usuario_perfil_ativo ON plantaopro.usuarios_perfis(usuario_id,perfil_id) WHERE reg_status='A';
CREATE UNIQUE INDEX IF NOT EXISTS ux_usuario_permissoes_especiais_usuario_permissao ON plantaopro.usuario_permissoes_especiais(usuario_id,permissao_id) WHERE reg_status='A';
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_permissoes_modulo_id') THEN ALTER TABLE plantaopro.permissoes ADD CONSTRAINT fk_permissoes_modulo_id FOREIGN KEY (modulo_id) REFERENCES plantaopro.modulos_sistema(id); END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_permissoes_acao_id') THEN ALTER TABLE plantaopro.permissoes ADD CONSTRAINT fk_permissoes_acao_id FOREIGN KEY (acao_id) REFERENCES plantaopro.acoes_sistema(id); END IF;
END $$;

CREATE TABLE IF NOT EXISTS plantaopro.login_tentativas(
    id uuid primary key default gen_random_uuid(), usuario_id uuid null, email text not null, ip text null,
    user_agent text null, sucesso boolean not null, motivo text not null, bloqueado_ate timestamp null,
    reg_date timestamp not null default now(), reg_update timestamp null, reg_status char(1) not null default 'A'
);
CREATE INDEX IF NOT EXISTS ix_login_tentativas_usuario_data ON plantaopro.login_tentativas(usuario_id, reg_date desc);
CREATE TABLE IF NOT EXISTS plantaopro.recuperacao_senha(
    id uuid primary key default gen_random_uuid(), usuario_id uuid not null, token_hash text not null,
    expiracao timestamp not null, utilizado boolean not null default false, reg_date timestamp not null default now(),
    reg_update timestamp null, reg_status char(1) not null default 'A'
);
CREATE INDEX IF NOT EXISTS ix_recuperacao_senha_usuario_token ON plantaopro.recuperacao_senha(usuario_id, token_hash);

CREATE TABLE IF NOT EXISTS plantaopro.schema_migrations (
    id text PRIMARY KEY,
    script_path text NOT NULL,
    checksum text NOT NULL,
    applied_at timestamptz NOT NULL DEFAULT now()
);

-- SOURCE: database/schema/010_identity_access.sql
-- SOURCE-SHA256: 9ba007cec03cf0e623f884026c6534f4219d009e33a30b57478d676e69e8fa4b
-- v1.18.6 schema canonico base: permissões/perfis/acessos
SET search_path TO plantaopro, public;


CREATE TABLE IF NOT EXISTS plantaopro.perfis (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, codigo text NULL,
    nome text NOT NULL, descricao text NULL, base_sistema boolean NOT NULL DEFAULT false, customizado boolean NOT NULL DEFAULT false,
    status text NOT NULL DEFAULT 'ATIVO', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(),
    reg_update timestamptz NULL, created_by uuid NULL, updated_by uuid NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.usuarios (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, nome text NOT NULL,
    email text NOT NULL, email_normalizado text NULL, senha_hash text NOT NULL, telefone text NULL, status text NOT NULL DEFAULT 'ATIVO',
    reg_status char(1) NOT NULL DEFAULT 'A', bloqueado_ate timestamptz NULL, senha_alteracao_obrigatoria boolean NOT NULL DEFAULT false,
    ultimo_login timestamptz NULL, preferencias_notificacao jsonb NOT NULL DEFAULT '{}'::jsonb, reg_date timestamptz NOT NULL DEFAULT now(),
    reg_update timestamptz NULL, created_by uuid NULL, updated_by uuid NULL
);
ALTER TABLE plantaopro.perfis
    ADD COLUMN IF NOT EXISTS tenant_id uuid,
    ADD COLUMN IF NOT EXISTS cliente_id uuid,
    ADD COLUMN IF NOT EXISTS codigo text,
    ADD COLUMN IF NOT EXISTS descricao text,
    ADD COLUMN IF NOT EXISTS base_sistema boolean DEFAULT false,
    ADD COLUMN IF NOT EXISTS customizado boolean DEFAULT false,
    ADD COLUMN IF NOT EXISTS status text DEFAULT 'ATIVO',
    ADD COLUMN IF NOT EXISTS reg_date timestamptz DEFAULT now(),
    ADD COLUMN IF NOT EXISTS reg_update timestamptz,
    ADD COLUMN IF NOT EXISTS created_by uuid,
    ADD COLUMN IF NOT EXISTS updated_by uuid;
ALTER TABLE plantaopro.usuarios
    ADD COLUMN IF NOT EXISTS tenant_id uuid,
    ADD COLUMN IF NOT EXISTS cliente_id uuid,
    ADD COLUMN IF NOT EXISTS email_normalizado text,
    ADD COLUMN IF NOT EXISTS telefone text,
    ADD COLUMN IF NOT EXISTS status text DEFAULT 'ATIVO',
    ADD COLUMN IF NOT EXISTS bloqueado_ate timestamptz,
    ADD COLUMN IF NOT EXISTS senha_alteracao_obrigatoria boolean DEFAULT false,
    ADD COLUMN IF NOT EXISTS ultimo_login timestamptz,
    ADD COLUMN IF NOT EXISTS preferencias_notificacao jsonb DEFAULT '{}'::jsonb,
    ADD COLUMN IF NOT EXISTS reg_date timestamptz DEFAULT now(),
    ADD COLUMN IF NOT EXISTS reg_update timestamptz,
    ADD COLUMN IF NOT EXISTS created_by uuid,
    ADD COLUMN IF NOT EXISTS updated_by uuid;
UPDATE plantaopro.perfis SET codigo = upper(regexp_replace(unaccent(coalesce(nullif(codigo,''), nome, id::text)), '[^A-Za-z0-9]+', '_', 'g')) WHERE codigo IS NULL OR btrim(codigo)='';
UPDATE plantaopro.usuarios SET email_normalizado = upper(email) WHERE email_normalizado IS NULL OR btrim(email_normalizado)='';
ALTER TABLE plantaopro.perfis ALTER COLUMN codigo SET NOT NULL, ALTER COLUMN base_sistema SET DEFAULT false, ALTER COLUMN customizado SET DEFAULT false, ALTER COLUMN status SET DEFAULT 'ATIVO';
UPDATE plantaopro.usuarios SET status = coalesce(nullif(status,''),'ATIVO'), senha_alteracao_obrigatoria = coalesce(senha_alteracao_obrigatoria,false), preferencias_notificacao = coalesce(preferencias_notificacao,'{}'::jsonb);
ALTER TABLE plantaopro.usuarios ALTER COLUMN email_normalizado SET NOT NULL, ALTER COLUMN status SET DEFAULT 'ATIVO', ALTER COLUMN status SET NOT NULL, ALTER COLUMN senha_alteracao_obrigatoria SET DEFAULT false, ALTER COLUMN senha_alteracao_obrigatoria SET NOT NULL, ALTER COLUMN preferencias_notificacao SET DEFAULT '{}'::jsonb, ALTER COLUMN preferencias_notificacao SET NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_usuarios_email_normalizado ON plantaopro.usuarios(lower(email_normalizado)) WHERE reg_status='A';

CREATE TABLE IF NOT EXISTS plantaopro.modulos_sistema (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), codigo text NOT NULL, nome text NOT NULL, descricao text NOT NULL DEFAULT '', ordem int NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'ATIVO', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, created_by uuid NULL, updated_by uuid NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.acoes_sistema (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), codigo text NOT NULL, nome text NOT NULL, descricao text NOT NULL DEFAULT '', ordem int NOT NULL DEFAULT 0, sensivel boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'ATIVO', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, created_by uuid NULL, updated_by uuid NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.permissoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), nome text NOT NULL, descricao text NULL, modulo text NULL, acao text NULL, modulo_id uuid NULL, acao_id uuid NULL, codigo text NULL, sensivel boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'ATIVO', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, created_by uuid NULL, updated_by uuid NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.perfil_permissoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), perfil_id uuid NOT NULL, permissao_id uuid NOT NULL, permitido boolean NOT NULL DEFAULT true, bloqueado_por_plano boolean NOT NULL DEFAULT false, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, created_by uuid NULL, updated_by uuid NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.usuarios_perfis (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NOT NULL, perfil_id uuid NOT NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, created_by uuid NULL, updated_by uuid NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.usuario_permissoes_especiais (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NOT NULL, permissao_id uuid NOT NULL, permitido boolean NOT NULL DEFAULT true, justificativa text NOT NULL DEFAULT '', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, created_by uuid NULL, updated_by uuid NULL
);

-- compat: ALTER TABLE bloco obrigatório para bases parciais/legadas
ALTER TABLE plantaopro.modulos_sistema ADD COLUMN IF NOT EXISTS codigo text, ADD COLUMN IF NOT EXISTS nome text, ADD COLUMN IF NOT EXISTS descricao text DEFAULT '', ADD COLUMN IF NOT EXISTS ordem int DEFAULT 0, ADD COLUMN IF NOT EXISTS status text DEFAULT 'ATIVO', ADD COLUMN IF NOT EXISTS reg_status char(1) DEFAULT 'A', ADD COLUMN IF NOT EXISTS reg_date timestamptz DEFAULT now(), ADD COLUMN IF NOT EXISTS reg_update timestamptz, ADD COLUMN IF NOT EXISTS created_by uuid, ADD COLUMN IF NOT EXISTS updated_by uuid;
ALTER TABLE plantaopro.acoes_sistema ADD COLUMN IF NOT EXISTS codigo text, ADD COLUMN IF NOT EXISTS nome text, ADD COLUMN IF NOT EXISTS descricao text DEFAULT '', ADD COLUMN IF NOT EXISTS ordem int DEFAULT 0, ADD COLUMN IF NOT EXISTS sensivel boolean DEFAULT false, ADD COLUMN IF NOT EXISTS status text DEFAULT 'ATIVO', ADD COLUMN IF NOT EXISTS reg_status char(1) DEFAULT 'A', ADD COLUMN IF NOT EXISTS reg_date timestamptz DEFAULT now(), ADD COLUMN IF NOT EXISTS reg_update timestamptz, ADD COLUMN IF NOT EXISTS created_by uuid, ADD COLUMN IF NOT EXISTS updated_by uuid;
ALTER TABLE plantaopro.permissoes ADD COLUMN IF NOT EXISTS nome text, ADD COLUMN IF NOT EXISTS descricao text, ADD COLUMN IF NOT EXISTS modulo text, ADD COLUMN IF NOT EXISTS acao text, ADD COLUMN IF NOT EXISTS modulo_id uuid, ADD COLUMN IF NOT EXISTS acao_id uuid, ADD COLUMN IF NOT EXISTS codigo text, ADD COLUMN IF NOT EXISTS sensivel boolean DEFAULT false, ADD COLUMN IF NOT EXISTS status text DEFAULT 'ATIVO', ADD COLUMN IF NOT EXISTS reg_status char(1) DEFAULT 'A', ADD COLUMN IF NOT EXISTS reg_date timestamptz DEFAULT now(), ADD COLUMN IF NOT EXISTS reg_update timestamptz, ADD COLUMN IF NOT EXISTS created_by uuid, ADD COLUMN IF NOT EXISTS updated_by uuid;
ALTER TABLE plantaopro.perfil_permissoes ADD COLUMN IF NOT EXISTS perfil_id uuid, ADD COLUMN IF NOT EXISTS permissao_id uuid, ADD COLUMN IF NOT EXISTS permitido boolean DEFAULT true, ADD COLUMN IF NOT EXISTS bloqueado_por_plano boolean DEFAULT false, ADD COLUMN IF NOT EXISTS reg_status char(1) DEFAULT 'A', ADD COLUMN IF NOT EXISTS reg_date timestamptz DEFAULT now(), ADD COLUMN IF NOT EXISTS reg_update timestamptz, ADD COLUMN IF NOT EXISTS created_by uuid, ADD COLUMN IF NOT EXISTS updated_by uuid;
ALTER TABLE plantaopro.usuarios_perfis ADD COLUMN IF NOT EXISTS tenant_id uuid, ADD COLUMN IF NOT EXISTS cliente_id uuid, ADD COLUMN IF NOT EXISTS usuario_id uuid, ADD COLUMN IF NOT EXISTS perfil_id uuid, ADD COLUMN IF NOT EXISTS reg_status char(1) DEFAULT 'A', ADD COLUMN IF NOT EXISTS reg_date timestamptz DEFAULT now(), ADD COLUMN IF NOT EXISTS reg_update timestamptz, ADD COLUMN IF NOT EXISTS created_by uuid, ADD COLUMN IF NOT EXISTS updated_by uuid;
ALTER TABLE plantaopro.usuario_permissoes_especiais ADD COLUMN IF NOT EXISTS tenant_id uuid, ADD COLUMN IF NOT EXISTS cliente_id uuid, ADD COLUMN IF NOT EXISTS usuario_id uuid, ADD COLUMN IF NOT EXISTS permissao_id uuid, ADD COLUMN IF NOT EXISTS permitido boolean DEFAULT true, ADD COLUMN IF NOT EXISTS justificativa text DEFAULT '', ADD COLUMN IF NOT EXISTS reg_status char(1) DEFAULT 'A', ADD COLUMN IF NOT EXISTS reg_date timestamptz DEFAULT now(), ADD COLUMN IF NOT EXISTS reg_update timestamptz, ADD COLUMN IF NOT EXISTS created_by uuid, ADD COLUMN IF NOT EXISTS updated_by uuid;

UPDATE plantaopro.permissoes SET codigo = upper(regexp_replace(unaccent(coalesce(nullif(codigo,''), nullif(nome,''), id::text)::text), '[^A-Za-z0-9]+', '_', 'g')) WHERE codigo IS NULL OR btrim(codigo)='';
UPDATE plantaopro.permissoes SET modulo = coalesce(nullif(modulo,''), split_part(codigo,'_',1), 'GERAL'), acao = coalesce(nullif(acao,''), nullif(array_to_string((regexp_split_to_array(codigo,'_'))[2:array_length(regexp_split_to_array(codigo,'_'),1)], '_'), ''), 'ACESSAR'), nome = coalesce(nullif(nome,''), codigo), descricao = coalesce(descricao,''), sensivel = coalesce(sensivel,false), status = coalesce(nullif(status,''),'ATIVO'), reg_status = coalesce(nullif(reg_status,''),'A'), reg_date = coalesce(reg_date, now());
WITH dup AS (SELECT id, row_number() OVER (PARTITION BY lower(codigo), reg_status ORDER BY reg_date, id) rn FROM plantaopro.permissoes WHERE reg_status='A') UPDATE plantaopro.permissoes p SET codigo = p.codigo || '_' || left(p.id::text,8), reg_update=now() FROM dup WHERE dup.id=p.id AND dup.rn>1;
INSERT INTO plantaopro.modulos_sistema(codigo,nome) SELECT DISTINCT upper(regexp_replace(unaccent(modulo::text), '[^A-Za-z0-9]+', '_', 'g')), modulo FROM plantaopro.permissoes p WHERE p.modulo IS NOT NULL AND NOT EXISTS (SELECT 1 FROM plantaopro.modulos_sistema m WHERE lower(m.codigo)=lower(upper(regexp_replace(unaccent(p.modulo::text), '[^A-Za-z0-9]+', '_', 'g'))) AND m.reg_status='A');
INSERT INTO plantaopro.acoes_sistema(codigo,nome) SELECT DISTINCT upper(regexp_replace(unaccent(acao::text), '[^A-Za-z0-9]+', '_', 'g')), acao FROM plantaopro.permissoes p WHERE p.acao IS NOT NULL AND NOT EXISTS (SELECT 1 FROM plantaopro.acoes_sistema a WHERE lower(a.codigo)=lower(upper(regexp_replace(unaccent(p.acao::text), '[^A-Za-z0-9]+', '_', 'g'))) AND a.reg_status='A');
UPDATE plantaopro.permissoes p SET modulo_id=m.id FROM plantaopro.modulos_sistema m WHERE p.modulo_id IS NULL AND lower(m.codigo)=lower(upper(regexp_replace(unaccent(p.modulo::text), '[^A-Za-z0-9]+', '_', 'g'))) AND m.reg_status='A';
UPDATE plantaopro.permissoes p SET acao_id=a.id FROM plantaopro.acoes_sistema a WHERE p.acao_id IS NULL AND lower(a.codigo)=lower(upper(regexp_replace(unaccent(p.acao::text), '[^A-Za-z0-9]+', '_', 'g'))) AND a.reg_status='A';
DO $$ BEGIN IF EXISTS (SELECT 1 FROM plantaopro.permissoes WHERE codigo IS NULL OR modulo_id IS NULL OR acao_id IS NULL) THEN RAISE EXCEPTION 'Permissões canônicas inválidas: codigo/modulo_id/acao_id nulos'; END IF; END $$;
ALTER TABLE plantaopro.permissoes ALTER COLUMN codigo SET NOT NULL, ALTER COLUMN modulo_id SET NOT NULL, ALTER COLUMN acao_id SET NOT NULL, ALTER COLUMN nome SET NOT NULL, ALTER COLUMN descricao SET DEFAULT '', ALTER COLUMN sensivel SET DEFAULT false, ALTER COLUMN sensivel SET NOT NULL, ALTER COLUMN status SET DEFAULT 'ATIVO', ALTER COLUMN status SET NOT NULL, ALTER COLUMN reg_status SET DEFAULT 'A', ALTER COLUMN reg_status SET NOT NULL, ALTER COLUMN reg_date SET DEFAULT now(), ALTER COLUMN reg_date SET NOT NULL;
ALTER TABLE plantaopro.modulos_sistema ALTER COLUMN codigo SET NOT NULL, ALTER COLUMN nome SET NOT NULL, ALTER COLUMN descricao SET DEFAULT '', ALTER COLUMN descricao SET NOT NULL, ALTER COLUMN status SET DEFAULT 'ATIVO', ALTER COLUMN status SET NOT NULL, ALTER COLUMN reg_status SET DEFAULT 'A', ALTER COLUMN reg_status SET NOT NULL;
ALTER TABLE plantaopro.acoes_sistema ALTER COLUMN codigo SET NOT NULL, ALTER COLUMN nome SET NOT NULL, ALTER COLUMN descricao SET DEFAULT '', ALTER COLUMN descricao SET NOT NULL, ALTER COLUMN sensivel SET DEFAULT false, ALTER COLUMN sensivel SET NOT NULL, ALTER COLUMN status SET DEFAULT 'ATIVO', ALTER COLUMN status SET NOT NULL, ALTER COLUMN reg_status SET DEFAULT 'A', ALTER COLUMN reg_status SET NOT NULL;
DO $$ BEGIN
    IF to_regclass('plantaopro.perfis_permissoes') IS NOT NULL THEN
        EXECUTE 'ALTER TABLE plantaopro.perfis_permissoes ADD COLUMN IF NOT EXISTS reg_date timestamptz DEFAULT now()';
        INSERT INTO plantaopro.perfil_permissoes(perfil_id,permissao_id,permitido,reg_status,reg_date)
        SELECT perfil_id,permissao_id,true,coalesce(reg_status,'A'),coalesce(reg_date,now()) FROM plantaopro.perfis_permissoes pp
        WHERE NOT EXISTS (SELECT 1 FROM plantaopro.perfil_permissoes x WHERE x.perfil_id=pp.perfil_id AND x.permissao_id=pp.permissao_id AND x.reg_status='A');
    END IF;
    IF to_regclass('plantaopro.usuario_perfis') IS NOT NULL THEN
        EXECUTE 'ALTER TABLE plantaopro.usuario_perfis ADD COLUMN IF NOT EXISTS reg_date timestamptz DEFAULT now()';
        INSERT INTO plantaopro.usuarios_perfis(usuario_id,perfil_id,reg_status,reg_date)
        SELECT usuario_id,perfil_id,coalesce(reg_status,'A'),coalesce(reg_date,now()) FROM plantaopro.usuario_perfis up
        WHERE NOT EXISTS (SELECT 1 FROM plantaopro.usuarios_perfis x WHERE x.usuario_id=up.usuario_id AND x.perfil_id=up.perfil_id AND x.reg_status='A');
    END IF;
END $$;
-- v1.36.0: reconciliação sem perda de perfis semanticamente duplicados.
CREATE TABLE IF NOT EXISTS plantaopro.perfil_consolidacao_historico (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    perfil_origem_id uuid NOT NULL,
    perfil_destino_id uuid NOT NULL,
    codigo text NOT NULL,
    motivo text NOT NULL,
    usuarios_transferidos integer NOT NULL DEFAULT 0,
    permissoes_transferidas integer NOT NULL DEFAULT 0,
    referencias_transferidas integer NOT NULL DEFAULT 0,
    executado_em timestamptz NOT NULL DEFAULT now(),
    executado_por text NOT NULL DEFAULT current_user,
    detalhes jsonb NOT NULL DEFAULT '{}'::jsonb,
    CONSTRAINT ck_perfil_consolidacao_origem_destino CHECK (perfil_origem_id <> perfil_destino_id),
    CONSTRAINT ux_perfil_consolidacao_origem UNIQUE (perfil_origem_id)
);

DO $reconcile$
DECLARE
    duplicate_record record;
    canonical_id uuid;
    moved_users integer;
    moved_permissions integer;
    moved_references integer;
    reference_record record;
    affected integer;
BEGIN
    FOR duplicate_record IN
        WITH ranked AS (
            SELECT p.*,
                   row_number() OVER (
                       PARTITION BY coalesce(p.tenant_id, '00000000-0000-0000-0000-000000000000'::uuid), lower(btrim(p.codigo))
                       ORDER BY p.base_sistema DESC, p.customizado ASC, (p.reg_status = 'A') DESC,
                           ((SELECT count(*) FROM plantaopro.usuarios_perfis up WHERE up.perfil_id = p.id AND up.reg_status = 'A') +
                            (SELECT count(*) FROM plantaopro.perfil_permissoes pp WHERE pp.perfil_id = p.id AND pp.reg_status = 'A')) DESC,
                           p.reg_date ASC, p.id ASC) AS position,
                   first_value(p.id) OVER (
                       PARTITION BY coalesce(p.tenant_id, '00000000-0000-0000-0000-000000000000'::uuid), lower(btrim(p.codigo))
                       ORDER BY p.base_sistema DESC, p.customizado ASC, (p.reg_status = 'A') DESC,
                           ((SELECT count(*) FROM plantaopro.usuarios_perfis up WHERE up.perfil_id = p.id AND up.reg_status = 'A') +
                            (SELECT count(*) FROM plantaopro.perfil_permissoes pp WHERE pp.perfil_id = p.id AND pp.reg_status = 'A')) DESC,
                           p.reg_date ASC, p.id ASC) AS winner_id
            FROM plantaopro.perfis p
            WHERE p.reg_status = 'A'
        )
        SELECT * FROM ranked WHERE position > 1
    LOOP
        canonical_id := duplicate_record.winner_id;
        moved_users := 0;
        moved_permissions := 0;
        moved_references := 0;

        INSERT INTO plantaopro.usuarios_perfis
            (tenant_id, cliente_id, usuario_id, perfil_id, reg_status, reg_date, reg_update, created_by, updated_by)
        SELECT up.tenant_id, up.cliente_id, up.usuario_id, canonical_id, 'A', up.reg_date, now(), up.created_by, up.updated_by
        FROM plantaopro.usuarios_perfis up
        WHERE up.perfil_id = duplicate_record.id AND up.reg_status = 'A'
          AND NOT EXISTS (
              SELECT 1 FROM plantaopro.usuarios_perfis existing
              WHERE existing.usuario_id = up.usuario_id AND existing.perfil_id = canonical_id AND existing.reg_status = 'A');
        GET DIAGNOSTICS moved_users = ROW_COUNT;

        UPDATE plantaopro.usuarios_perfis
        SET reg_status = 'I', reg_update = now()
        WHERE perfil_id = duplicate_record.id AND reg_status = 'A';

        UPDATE plantaopro.perfil_permissoes target
        SET permitido = target.permitido AND source.permitido,
            bloqueado_por_plano = target.bloqueado_por_plano OR source.bloqueado_por_plano,
            reg_update = now()
        FROM plantaopro.perfil_permissoes source
        WHERE source.perfil_id = duplicate_record.id AND source.reg_status = 'A'
          AND target.perfil_id = canonical_id AND target.permissao_id = source.permissao_id AND target.reg_status = 'A';

        INSERT INTO plantaopro.perfil_permissoes
            (perfil_id, permissao_id, permitido, bloqueado_por_plano, reg_status, reg_date, reg_update, created_by, updated_by)
        SELECT canonical_id, source.permissao_id, source.permitido, source.bloqueado_por_plano,
               'A', source.reg_date, now(), source.created_by, source.updated_by
        FROM plantaopro.perfil_permissoes source
        WHERE source.perfil_id = duplicate_record.id AND source.reg_status = 'A'
          AND NOT EXISTS (
              SELECT 1 FROM plantaopro.perfil_permissoes target
              WHERE target.perfil_id = canonical_id AND target.permissao_id = source.permissao_id AND target.reg_status = 'A');
        GET DIAGNOSTICS moved_permissions = ROW_COUNT;

        UPDATE plantaopro.perfil_permissoes
        SET reg_status = 'I', reg_update = now()
        WHERE perfil_id = duplicate_record.id AND reg_status = 'A';

        -- Atualiza toda FK não canônica descoberta no catálogo; tabelas canônicas foram tratadas acima.
        FOR reference_record IN
            SELECT ns.nspname AS schema_name, cls.relname AS table_name, att.attname AS column_name
            FROM pg_constraint con
            JOIN pg_class cls ON cls.oid = con.conrelid
            JOIN pg_namespace ns ON ns.oid = cls.relnamespace
            JOIN pg_attribute att ON att.attrelid = con.conrelid AND att.attnum = con.conkey[1]
            WHERE con.contype = 'f' AND con.confrelid = 'plantaopro.perfis'::regclass
              AND cardinality(con.conkey) = 1
              AND NOT (ns.nspname = 'plantaopro' AND cls.relname IN ('usuarios_perfis', 'perfil_permissoes'))
        LOOP
            EXECUTE format('UPDATE %I.%I SET %I = $1 WHERE %I = $2', reference_record.schema_name,
                           reference_record.table_name, reference_record.column_name, reference_record.column_name)
            USING canonical_id, duplicate_record.id;
            GET DIAGNOSTICS affected = ROW_COUNT;
            moved_references := moved_references + affected;
        END LOOP;

        UPDATE plantaopro.perfis
        SET reg_status = 'I', status = 'INATIVO', reg_update = now()
        WHERE id = duplicate_record.id;

        INSERT INTO plantaopro.perfil_consolidacao_historico
            (tenant_id, perfil_origem_id, perfil_destino_id, codigo, motivo,
             usuarios_transferidos, permissoes_transferidas, referencias_transferidas, detalhes)
        VALUES
            (duplicate_record.tenant_id, duplicate_record.id, canonical_id, duplicate_record.codigo,
             'Duplicidade semântica por tenant e código normalizado', moved_users, moved_permissions,
             moved_references, jsonb_build_object('codigo_normalizado', lower(btrim(duplicate_record.codigo)),
                                                   'criterio', 'base_sistema, customizado, vinculos, reg_date, id'))
        ON CONFLICT (perfil_origem_id) DO NOTHING;
    END LOOP;
END
$reconcile$;

DO $validation$
BEGIN
    IF EXISTS (
        SELECT 1 FROM plantaopro.perfis
        WHERE reg_status = 'A'
        GROUP BY coalesce(tenant_id, '00000000-0000-0000-0000-000000000000'::uuid), lower(btrim(codigo))
        HAVING count(*) > 1
    ) THEN
        RAISE EXCEPTION 'Ainda existem perfis ativos duplicados; o índice único não pode ser criado.';
    END IF;
END
$validation$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_perfis_tenant_codigo
ON plantaopro.perfis (
    coalesce(tenant_id, '00000000-0000-0000-0000-000000000000'::uuid),
    lower(btrim(codigo))
)
WHERE reg_status = 'A';


CREATE UNIQUE INDEX IF NOT EXISTS ux_modulos_sistema_codigo ON plantaopro.modulos_sistema(lower(codigo)) WHERE reg_status='A';
CREATE UNIQUE INDEX IF NOT EXISTS ux_acoes_sistema_codigo ON plantaopro.acoes_sistema(lower(codigo)) WHERE reg_status='A';
CREATE UNIQUE INDEX IF NOT EXISTS ux_permissoes_codigo ON plantaopro.permissoes(lower(codigo)) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_permissoes_modulo_status_regdate ON plantaopro.permissoes(modulo_id,status,reg_date);
CREATE UNIQUE INDEX IF NOT EXISTS ux_perfil_permissoes_perfil_permissao ON plantaopro.perfil_permissoes(perfil_id,permissao_id) WHERE reg_status='A';
CREATE UNIQUE INDEX IF NOT EXISTS ux_usuarios_perfis_usuario_perfil_ativo ON plantaopro.usuarios_perfis(usuario_id,perfil_id) WHERE reg_status='A';
CREATE UNIQUE INDEX IF NOT EXISTS ux_usuario_permissoes_especiais_usuario_permissao ON plantaopro.usuario_permissoes_especiais(usuario_id,permissao_id) WHERE reg_status='A';
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_permissoes_modulo_id') THEN ALTER TABLE plantaopro.permissoes ADD CONSTRAINT fk_permissoes_modulo_id FOREIGN KEY (modulo_id) REFERENCES plantaopro.modulos_sistema(id); END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_permissoes_acao_id') THEN ALTER TABLE plantaopro.permissoes ADD CONSTRAINT fk_permissoes_acao_id FOREIGN KEY (acao_id) REFERENCES plantaopro.acoes_sistema(id); END IF;
END $$;

CREATE TABLE IF NOT EXISTS plantaopro.login_tentativas(
    id uuid primary key default gen_random_uuid(), usuario_id uuid null, email text not null, ip text null,
    user_agent text null, sucesso boolean not null, motivo text not null, bloqueado_ate timestamp null,
    reg_date timestamp not null default now(), reg_update timestamp null, reg_status char(1) not null default 'A'
);
CREATE INDEX IF NOT EXISTS ix_login_tentativas_usuario_data ON plantaopro.login_tentativas(usuario_id, reg_date desc);
INSERT INTO plantaopro.perfis(tenant_id,cliente_id,codigo,nome,descricao,base_sistema,customizado,status,reg_status) SELECT NULL,NULL,'ADMINISTRADOR_GLOBAL','Administrador Global','Acesso administrativo global do sistema',true,false,'ATIVO','A' WHERE NOT EXISTS (SELECT 1 FROM plantaopro.perfis WHERE tenant_id IS NULL AND codigo='ADMINISTRADOR_GLOBAL' AND reg_status='A');

CREATE TABLE IF NOT EXISTS plantaopro.recuperacao_senha(
    id uuid primary key default gen_random_uuid(), usuario_id uuid not null, token_hash text not null,
    expiracao timestamp not null, utilizado boolean not null default false, reg_date timestamp not null default now(),
    reg_update timestamp null, reg_status char(1) not null default 'A'
);
CREATE INDEX IF NOT EXISTS ix_recuperacao_senha_usuario_token ON plantaopro.recuperacao_senha(usuario_id, token_hash);

-- v1.18.7 Central de Segurança: sessões, refresh tokens, políticas e auditoria.
CREATE TABLE IF NOT EXISTS plantaopro.auth_sessoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NOT NULL,
    dispositivo_nome text NOT NULL DEFAULT 'Dispositivo não identificado', ip_mascarado text NULL, user_agent_sanitizado text NULL,
    iniciado_em timestamptz NOT NULL DEFAULT now(), ultimo_uso_em timestamptz NULL, expira_em timestamptz NULL,
    revogada_em timestamptz NULL, motivo_revogacao text NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.auth_refresh_tokens (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), sessao_id uuid NOT NULL, usuario_id uuid NOT NULL, token_hash text NOT NULL,
    emitido_em timestamptz NOT NULL DEFAULT now(), expira_em timestamptz NOT NULL, usado_em timestamptz NULL, substituido_por_id uuid NULL,
    revogado_em timestamptz NULL, motivo_revogacao text NULL, reg_status char(1) NOT NULL DEFAULT 'A'
);
CREATE TABLE IF NOT EXISTS plantaopro.auth_revogacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), sessao_id uuid NULL, usuario_id uuid NOT NULL, motivo text NOT NULL,
    revogado_por uuid NULL, ip_mascarado text NULL, reg_date timestamptz NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A'
);
CREATE TABLE IF NOT EXISTS plantaopro.senha_historico (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid NOT NULL, senha_hash text NOT NULL,
    origem text NOT NULL DEFAULT 'ALTERACAO_SENHA', reg_date timestamptz NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A'
);
CREATE TABLE IF NOT EXISTS plantaopro.politicas_senha (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, tamanho_minimo int NOT NULL DEFAULT 10,
    exige_maiuscula boolean NOT NULL DEFAULT true, exige_minuscula boolean NOT NULL DEFAULT true, exige_numero boolean NOT NULL DEFAULT true,
    exige_especial boolean NOT NULL DEFAULT true, historico_quantidade int NOT NULL DEFAULT 5, expiracao_dias int NOT NULL DEFAULT 90,
    tentativas_permitidas int NOT NULL DEFAULT 5, bloqueio_minutos int NOT NULL DEFAULT 30, troca_obrigatoria boolean NOT NULL DEFAULT false,
    proibir_senhas_comuns boolean NOT NULL DEFAULT true, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL
);
CREATE INDEX IF NOT EXISTS ix_auth_sessoes_usuario_status ON plantaopro.auth_sessoes(usuario_id, reg_status, ultimo_uso_em DESC);
CREATE INDEX IF NOT EXISTS ix_auth_refresh_tokens_sessao ON plantaopro.auth_refresh_tokens(sessao_id, expira_em DESC);
CREATE INDEX IF NOT EXISTS ix_auth_revogacoes_usuario ON plantaopro.auth_revogacoes(usuario_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_senha_historico_usuario ON plantaopro.senha_historico(usuario_id, reg_date DESC);
CREATE UNIQUE INDEX IF NOT EXISTS ux_politicas_senha_tenant ON plantaopro.politicas_senha(coalesce(tenant_id, '00000000-0000-0000-0000-000000000000'::uuid)) WHERE reg_status='A';
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_auth_refresh_tokens_sessao') THEN ALTER TABLE plantaopro.auth_refresh_tokens ADD CONSTRAINT fk_auth_refresh_tokens_sessao FOREIGN KEY (sessao_id) REFERENCES plantaopro.auth_sessoes(id); END IF;
END $$;

-- SOURCE: database/schema/020_saas_tenants.sql
-- SOURCE-SHA256: 3e1a57ad9a97798a78ffa3baab8cbad0b5196604240db2dede04f8d472cdf9ef
-- SaaS tenants canônicos mínimos definidos no manifesto para preservar compatibilidade com legados.
SET search_path TO plantaopro, public;

-- DDL canônico idempotente v1.18.9
CREATE TABLE IF NOT EXISTS plantaopro.planos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.clientes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.tenants (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.assinaturas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_historico (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_uso (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_modulos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.assinatura_bloqueios (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_modulos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_parametros (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_configuracoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_white_label (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_onboarding (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.tenant_onboarding_checklist (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.upgrade_solicitacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.downgrade_solicitacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.faturas_saas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.pagamentos_saas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);

-- SOURCE: database/schema/030_operacao_plantoes.sql
-- SOURCE-SHA256: b7e7ba760953e8ed6bd0c3a65b8a828c5866f3ec15901d06d989cb57bfce6881
-- Operação de plantões preservada a partir das origens históricas normalizadas pelo gerador.
SET search_path TO plantaopro, public;

-- DDL canônico idempotente v1.18.9
CREATE TABLE IF NOT EXISTS plantaopro.especialidades (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.hospitais (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.medicos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.plantoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.plantao_historico (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.plantao_convites (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.convites (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.escalas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.historico_escala (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.substituicoes_plantao (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.substituicao_candidatos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.substituicao_aprovacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.substituicao_historico (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.medico_disponibilidades (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.medico_indisponibilidades (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.medico_preferencias_plantao (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.notificacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.conversas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.mensagens (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);

-- SOURCE: database/schema/040_saude360.sql
-- SOURCE-SHA256: 90eaa5768e2391b2636c62d7b4b3e637a33dc306f2c0d7a841d14b1d2951a6fe
-- Saúde 360 preservado a partir das origens históricas normalizadas pelo gerador.
SET search_path TO plantaopro, public;

-- DDL canônico idempotente v1.18.9
CREATE TABLE IF NOT EXISTS plantaopro.pacientes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.agendamentos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.checkins (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.painel_chamadas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.triagens (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.consultas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.cid (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.prescricoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.convenios (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.planos_saude (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.financeiro_clinico (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.unidades (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.salas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.consentimentos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.historico_clinico (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.auditoria_clinica (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);

-- SOURCE: database/schema/050_financeiro.sql
-- SOURCE-SHA256: 43237f61fcffb8cb41244672fe447d9ac00e15fd22f5a40303b4100ab1c94ba6
-- Financeiro preservado a partir das origens históricas normalizadas pelo gerador.
SET search_path TO plantaopro, public;

-- DDL canônico idempotente v1.18.9
CREATE TABLE IF NOT EXISTS plantaopro.pagamentos_medicos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.pagamento_medico_historico (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.regras_faturamento (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.regras_repasse (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.regras_glosa (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.contas_receber (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.recebimentos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.caixa (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.lotes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.faturas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.itens_faturaveis (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.eventos_financeiros (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);

-- SOURCE: database/schema/060_auditoria_observabilidade.sql
-- SOURCE-SHA256: 34f02c49d3d5bce2beca60a6d16701a3fdf6fb964afffbca1618c1dcc2e07a65
-- Auditoria e observabilidade preservadas a partir das origens históricas normalizadas pelo gerador.
SET search_path TO plantaopro, public;

-- DDL canônico idempotente v1.18.9
CREATE TABLE IF NOT EXISTS plantaopro.auditoria (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.auditoria_acoes_criticas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.auditoria_eventos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.api_request_logs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.api_error_logs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.background_job_logs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.logs_operacionais (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.eventos_sistema (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.acessos_negados_log (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.permissao_logs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);

-- SOURCE: database/schema/070_relatorios.sql
-- SOURCE-SHA256: c54ed035de3e7ba1b8f2d94dc64e793c7c0002c7d2492418352ae14d44b2f5ff
-- Relatórios preservados a partir das origens históricas normalizadas pelo gerador.
SET search_path TO plantaopro, public;

-- DDL canônico idempotente v1.18.9
CREATE TABLE IF NOT EXISTS plantaopro.relatorio_exportacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.relatorios_exportacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.relatorios_filtros_salvos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);

-- SOURCE: database/schema/080_constraints.sql
-- SOURCE-SHA256: 72e593f065c706c8e02d08284445e3d90dbbf63556e87487c6c103eefdb6ff46
-- Constraints canônicas complementares são mantidas idempotentes nas respectivas seções.
SET search_path TO plantaopro, public;

-- SOURCE: database/schema/090_indexes.sql
-- SOURCE-SHA256: 584210538344133b1bc98359e3db7a64574bb47ddd0acf77ebb960c43f58880f
-- Índices canônicos complementares são mantidos idempotentes nas respectivas seções.
SET search_path TO plantaopro, public;

-- SOURCE: database/schema/100_reference_data.sql
-- SOURCE-SHA256: 1a6a6637aea031658d332fc131267e111f567c3ad3dc509b7db3115e9cb10b7e
-- Dados referenciais mínimos sem credenciais fixas.
INSERT INTO plantaopro.politicas_senha(tenant_id)
SELECT NULL WHERE NOT EXISTS (SELECT 1 FROM plantaopro.politicas_senha WHERE tenant_id IS NULL AND reg_status='A');

-- SOURCE: database/schema/110_implantacao_go_live.sql
-- SOURCE-SHA256: 219c010bc98a32552392603bd63578dab21132b30366a58dc11acd6599b99d6c
-- v1.18.8 Central de Implantação, Diagnóstico e Go-Live
SET search_path TO plantaopro, public;
CREATE TABLE IF NOT EXISTS plantaopro.implantacao_status (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, classificacao text NOT NULL DEFAULT 'NÃO_CONFIGURADO', prontidao_percentual numeric(5,2) NOT NULL DEFAULT 0, versao text NOT NULL DEFAULT 'v1.18.8', ambiente text NOT NULL DEFAULT 'NAO_INFORMADO', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL);
CREATE TABLE IF NOT EXISTS plantaopro.implantacao_etapas (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, codigo text NOT NULL, ordem int NOT NULL, nome text NOT NULL, descricao text NOT NULL DEFAULT '', status text NOT NULL DEFAULT 'PENDENTE', responsavel text NULL, link_seguro text NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL);
CREATE TABLE IF NOT EXISTS plantaopro.implantacao_validacoes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), etapa_id uuid NULL, codigo text NOT NULL, descricao text NOT NULL DEFAULT '', status text NOT NULL DEFAULT 'PENDENTE', detalhes_sanitizados jsonb NOT NULL DEFAULT '{}'::jsonb, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.implantacao_pendencias (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), etapa_id uuid NULL, descricao text NOT NULL, acao_sugerida text NOT NULL DEFAULT '', criticidade text NOT NULL DEFAULT 'ATENÇÃO', responsavel text NULL, prazo date NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.implantacao_execucoes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, comando text NOT NULL, resultado text NOT NULL, detalhes_sanitizados jsonb NOT NULL DEFAULT '{}'::jsonb, executado_por uuid NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.implantacao_evidencias (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), etapa_id uuid NULL, tipo text NOT NULL, referencia text NOT NULL, hash_conteudo text NULL, criado_por uuid NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plantaopro.go_live_checklists (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, decisao_final text NOT NULL DEFAULT 'PENDENTE', relatorio jsonb NOT NULL DEFAULT '{}'::jsonb, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL);
CREATE TABLE IF NOT EXISTS plantaopro.go_live_aprovacoes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), checklist_id uuid NULL, aprovador_nome text NOT NULL, papel text NOT NULL, decisao text NOT NULL, observacao text NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
CREATE UNIQUE INDEX IF NOT EXISTS ux_implantacao_etapas_tenant_codigo ON plantaopro.implantacao_etapas(coalesce(tenant_id,'00000000-0000-0000-0000-000000000000'::uuid), lower(codigo)) WHERE reg_status='A';

-- SOURCE: database/schema/120_operacoes_continuidade.sql
-- SOURCE-SHA256: 01909376c31af7016d3cddc4ba045523febc132dc9333c8e98befcf31ec4eb8e
-- Operações e continuidade v1.18.9
SET search_path TO plantaopro, public;

-- DDL canônico idempotente v1.18.9
CREATE TABLE IF NOT EXISTS plantaopro.operacao_incidentes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.operacao_incidente_eventos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.operacao_incidente_responsaveis (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.operacao_incidente_comentarios (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.operacao_alertas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.operacao_outbox (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.backup_politicas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.backup_execucoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.backup_arquivos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.backup_verificacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.restore_testes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.dr_execucoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.job_definicoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.job_execucoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.job_tentativas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.job_bloqueios (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.release_versoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.release_implantacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.release_evidencias (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.release_aprovacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.release_rollbacks (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.runbooks (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.runbook_passos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.manutencao_janelas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.manutencao_tarefas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.manutencao_comunicacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.manutencao_aprovacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    codigo text NULL,
    nome text NULL,
    status text NOT NULL DEFAULT 'ATIVO',
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NULL
);

-- SOURCE: database/schema/130_contexto_multiempresa.sql
-- SOURCE-SHA256: f08349be6f286abc56f13ab14ccfb3934acb98b12c3b3c72294743e421f0bd6d
-- v1.19.0 - Contexto multiempresa e suporte assistido
CREATE TABLE IF NOT EXISTS plantaopro.usuario_tenant_acessos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid NOT NULL, tenant_id uuid NULL, cliente_id uuid NULL, perfil_id uuid NULL,
    origem text NOT NULL DEFAULT 'LEGADO', acesso_inicio timestamptz NOT NULL DEFAULT now(), acesso_fim timestamptz NULL,
    status text NOT NULL DEFAULT 'ATIVO', reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, created_by uuid NULL, updated_by uuid NULL
);
CREATE TABLE IF NOT EXISTS plantaopro.usuario_contextos_recentes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid NOT NULL, tenant_id uuid NOT NULL, cliente_id uuid NULL, ultimo_acesso_em timestamptz NOT NULL DEFAULT now(), total_acessos int NOT NULL DEFAULT 1, reg_status char(1) NOT NULL DEFAULT 'A'
);
CREATE TABLE IF NOT EXISTS plantaopro.contexto_sessoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), sessao_id text NOT NULL, usuario_id uuid NOT NULL, tenant_id uuid NULL, cliente_id uuid NULL,
    modo text NOT NULL DEFAULT 'GLOBAL', perfil_efetivo text NOT NULL, iniciado_em timestamptz NOT NULL DEFAULT now(), ultimo_uso_em timestamptz NOT NULL DEFAULT now(), encerrado_em timestamptz NULL,
    ip_mascarado text NULL, user_agent_sanitizado text NULL, reg_status char(1) NOT NULL DEFAULT 'A'
);
CREATE TABLE IF NOT EXISTS plantaopro.contexto_trocas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), contexto_sessao_id uuid NULL, usuario_id uuid NOT NULL, tenant_origem_id uuid NULL, tenant_destino_id uuid NULL, modo_origem text NULL, modo_destino text NOT NULL, motivo text NULL, ip_mascarado text NULL, reg_date timestamptz NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A'
);
CREATE TABLE IF NOT EXISTS plantaopro.impersonacao_sessoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_origem_id uuid NOT NULL, usuario_alvo_id uuid NOT NULL, tenant_id uuid NOT NULL, cliente_id uuid NULL,
    motivo text NOT NULL, ticket_referencia text NOT NULL, iniciado_em timestamptz NOT NULL DEFAULT now(), expira_em timestamptz NOT NULL, encerrado_em timestamptz NULL, encerrado_por uuid NULL,
    status text NOT NULL DEFAULT 'ATIVA', ip_mascarado text NULL, reg_status char(1) NOT NULL DEFAULT 'A'
);
CREATE TABLE IF NOT EXISTS plantaopro.impersonacao_eventos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), impersonacao_sessao_id uuid NOT NULL, usuario_origem_id uuid NOT NULL, usuario_alvo_id uuid NOT NULL, evento text NOT NULL, detalhes jsonb NOT NULL DEFAULT '{}'::jsonb, reg_date timestamptz NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A'
);
CREATE INDEX IF NOT EXISTS ix_usuario_tenant_acessos_usuario ON plantaopro.usuario_tenant_acessos(usuario_id, reg_status, status);
CREATE UNIQUE INDEX IF NOT EXISTS ux_usuario_contextos_recentes_usuario_tenant ON plantaopro.usuario_contextos_recentes(usuario_id, tenant_id);
CREATE INDEX IF NOT EXISTS ix_contexto_sessoes_usuario ON plantaopro.contexto_sessoes(usuario_id, reg_status, encerrado_em);
CREATE INDEX IF NOT EXISTS ix_impersonacao_sessoes_origem ON plantaopro.impersonacao_sessoes(usuario_origem_id, status, reg_status);
INSERT INTO plantaopro.usuario_tenant_acessos(usuario_id, tenant_id, cliente_id, perfil_id, origem, created_by)
SELECT up.usuario_id, up.tenant_id, up.cliente_id, up.perfil_id, 'LEGADO_USUARIOS_PERFIS', up.created_by
FROM plantaopro.usuarios_perfis up
JOIN plantaopro.perfis p ON p.id = up.perfil_id
WHERE up.reg_status='A' AND p.reg_status='A' AND up.tenant_id IS NOT NULL
  AND coalesce(p.codigo,p.nome) <> 'ADMINISTRADOR_GLOBAL'
  AND NOT EXISTS (SELECT 1 FROM plantaopro.usuario_tenant_acessos uta WHERE uta.usuario_id=up.usuario_id AND uta.perfil_id=up.perfil_id AND uta.tenant_id=up.tenant_id AND uta.reg_status='A');

-- SOURCE: database/schema/140_experiencia_premium_meu_dia.sql
-- SOURCE-SHA256: c64c5c9f4865eb89e2243bd2c4f033e2eb7a144abf87fb7c8fe31296fdd66178
-- PlantãoPro v1.20.1 - Experiência premium Meu Dia
CREATE TABLE IF NOT EXISTS plantaopro.usuario_preferencias_interface (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id uuid NOT NULL,
    chave text NOT NULL,
    valor jsonb NOT NULL DEFAULT '{}'::jsonb,
    reg_date timestamptz NOT NULL DEFAULT now(),
    reg_status char(1) NOT NULL DEFAULT 'A',
    UNIQUE (usuario_id, chave)
);

CREATE TABLE IF NOT EXISTS plantaopro.meu_dia_item_estados (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id uuid NOT NULL,
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    item_origem_tipo text NOT NULL,
    item_origem_id uuid NOT NULL,
    status text NOT NULL DEFAULT 'ABERTO',
    adiado_ate timestamptz NULL,
    reg_date timestamptz NOT NULL DEFAULT now(),
    reg_status char(1) NOT NULL DEFAULT 'A',
    UNIQUE (usuario_id, item_origem_tipo, item_origem_id)
);

CREATE TABLE IF NOT EXISTS plantaopro.meu_dia_historico (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id uuid NOT NULL,
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    item_estado_id uuid NULL,
    evento text NOT NULL,
    detalhes jsonb NOT NULL DEFAULT '{}'::jsonb,
    reg_date timestamptz NOT NULL DEFAULT now(),
    reg_status char(1) NOT NULL DEFAULT 'A'
);

CREATE INDEX IF NOT EXISTS ix_meu_dia_item_estados_usuario ON plantaopro.meu_dia_item_estados(usuario_id, status, reg_status);
CREATE INDEX IF NOT EXISTS ix_meu_dia_historico_usuario ON plantaopro.meu_dia_historico(usuario_id, reg_date DESC);

-- ============================================================
-- Seção 12 — Operacional real v1.13
-- ============================================================

-- SOURCE: database/migrations/2026_v113_operacional_real.sql
-- SOURCE-SHA256: 5a3ecb94d9cdad2e508dc8c3183a0d2c3ff2f7bc9f67c6247cbd226c0fc68599
create schema if not exists plantaopro;
create extension if not exists pgcrypto;
create table if not exists plantaopro.v113_clientes (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, nome varchar(160), documento varchar(40), email varchar(160), status varchar(40));
alter table if exists plantaopro.v113_clientes add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_clientes add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_clientes add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_clientes add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_clientes add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_clientes add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_clientes add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_clientes add column if not exists nome varchar(160);
alter table if exists plantaopro.v113_clientes add column if not exists documento varchar(40);
alter table if exists plantaopro.v113_clientes add column if not exists email varchar(160);
alter table if exists plantaopro.v113_clientes add column if not exists status varchar(40);
create index if not exists ix_v113_clientes_cliente_id on plantaopro.v113_clientes (cliente_id);
create index if not exists ix_v113_clientes_status on plantaopro.v113_clientes (status);
create index if not exists ix_v113_clientes_created_at on plantaopro.v113_clientes (created_at);
create index if not exists ix_v113_clientes_tenant_id on plantaopro.v113_clientes (tenant_id);
create table if not exists plantaopro.v113_produtos (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, codigo varchar(60), nome varchar(160), preco numeric(14,2) default 0, estoque_minimo numeric(14,2) default 0, status varchar(40));
alter table if exists plantaopro.v113_produtos add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_produtos add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_produtos add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_produtos add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_produtos add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_produtos add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_produtos add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_produtos add column if not exists codigo varchar(60);
alter table if exists plantaopro.v113_produtos add column if not exists nome varchar(160);
alter table if exists plantaopro.v113_produtos add column if not exists preco numeric(14,2) default 0;
alter table if exists plantaopro.v113_produtos add column if not exists estoque_minimo numeric(14,2) default 0;
alter table if exists plantaopro.v113_produtos add column if not exists status varchar(40);
create index if not exists ix_v113_produtos_cliente_id on plantaopro.v113_produtos (cliente_id);
create index if not exists ix_v113_produtos_status on plantaopro.v113_produtos (status);
create index if not exists ix_v113_produtos_created_at on plantaopro.v113_produtos (created_at);
create index if not exists ix_v113_produtos_tenant_id on plantaopro.v113_produtos (tenant_id);
create table if not exists plantaopro.v113_estoque_movimentos (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, produto_id uuid, pedido_id uuid, quantidade numeric(14,2) default 0, tipo varchar(60), observacao text, status varchar(40));
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists produto_id uuid;
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists pedido_id uuid;
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists quantidade numeric(14,2) default 0;
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists tipo varchar(60);
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists observacao text;
alter table if exists plantaopro.v113_estoque_movimentos add column if not exists status varchar(40);
create index if not exists ix_v113_estoque_movimentos_cliente_id on plantaopro.v113_estoque_movimentos (cliente_id);
create index if not exists ix_v113_estoque_movimentos_status on plantaopro.v113_estoque_movimentos (status);
create index if not exists ix_v113_estoque_movimentos_created_at on plantaopro.v113_estoque_movimentos (created_at);
create index if not exists ix_v113_estoque_movimentos_tenant_id on plantaopro.v113_estoque_movimentos (tenant_id);
create table if not exists plantaopro.v113_pedidos (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, cliente_operacional_id uuid, status varchar(40), total numeric(14,2) default 0);
alter table if exists plantaopro.v113_pedidos add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_pedidos add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_pedidos add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_pedidos add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_pedidos add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_pedidos add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_pedidos add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_pedidos add column if not exists cliente_operacional_id uuid;
alter table if exists plantaopro.v113_pedidos add column if not exists status varchar(40);
alter table if exists plantaopro.v113_pedidos add column if not exists total numeric(14,2) default 0;
create index if not exists ix_v113_pedidos_cliente_id on plantaopro.v113_pedidos (cliente_id);
create index if not exists ix_v113_pedidos_status on plantaopro.v113_pedidos (status);
create index if not exists ix_v113_pedidos_created_at on plantaopro.v113_pedidos (created_at);
create index if not exists ix_v113_pedidos_tenant_id on plantaopro.v113_pedidos (tenant_id);
create table if not exists plantaopro.v113_pedido_itens (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, pedido_id uuid, produto_id uuid, quantidade numeric(14,2) default 0, valor_unitario numeric(14,2) default 0, status varchar(40));
alter table if exists plantaopro.v113_pedido_itens add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_pedido_itens add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_pedido_itens add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_pedido_itens add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_pedido_itens add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_pedido_itens add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_pedido_itens add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_pedido_itens add column if not exists pedido_id uuid;
alter table if exists plantaopro.v113_pedido_itens add column if not exists produto_id uuid;
alter table if exists plantaopro.v113_pedido_itens add column if not exists quantidade numeric(14,2) default 0;
alter table if exists plantaopro.v113_pedido_itens add column if not exists valor_unitario numeric(14,2) default 0;
alter table if exists plantaopro.v113_pedido_itens add column if not exists status varchar(40);
create index if not exists ix_v113_pedido_itens_cliente_id on plantaopro.v113_pedido_itens (cliente_id);
create index if not exists ix_v113_pedido_itens_status on plantaopro.v113_pedido_itens (status);
create index if not exists ix_v113_pedido_itens_created_at on plantaopro.v113_pedido_itens (created_at);
create index if not exists ix_v113_pedido_itens_tenant_id on plantaopro.v113_pedido_itens (tenant_id);
create table if not exists plantaopro.v113_tarefas (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, pedido_id uuid, titulo varchar(180), status varchar(40), responsavel varchar(120), comentarios jsonb default '[]'::jsonb);
alter table if exists plantaopro.v113_tarefas add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_tarefas add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_tarefas add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_tarefas add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_tarefas add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_tarefas add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_tarefas add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_tarefas add column if not exists pedido_id uuid;
alter table if exists plantaopro.v113_tarefas add column if not exists titulo varchar(180);
alter table if exists plantaopro.v113_tarefas add column if not exists status varchar(40);
alter table if exists plantaopro.v113_tarefas add column if not exists responsavel varchar(120);
alter table if exists plantaopro.v113_tarefas add column if not exists comentarios jsonb default '[]'::jsonb;
create index if not exists ix_v113_tarefas_cliente_id on plantaopro.v113_tarefas (cliente_id);
create index if not exists ix_v113_tarefas_status on plantaopro.v113_tarefas (status);
create index if not exists ix_v113_tarefas_created_at on plantaopro.v113_tarefas (created_at);
create index if not exists ix_v113_tarefas_tenant_id on plantaopro.v113_tarefas (tenant_id);
create table if not exists plantaopro.v113_faturas (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, pedido_id uuid, valor numeric(14,2) default 0, status varchar(40));
alter table if exists plantaopro.v113_faturas add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_faturas add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_faturas add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_faturas add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_faturas add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_faturas add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_faturas add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_faturas add column if not exists pedido_id uuid;
alter table if exists plantaopro.v113_faturas add column if not exists valor numeric(14,2) default 0;
alter table if exists plantaopro.v113_faturas add column if not exists status varchar(40);
create index if not exists ix_v113_faturas_cliente_id on plantaopro.v113_faturas (cliente_id);
create index if not exists ix_v113_faturas_status on plantaopro.v113_faturas (status);
create index if not exists ix_v113_faturas_created_at on plantaopro.v113_faturas (created_at);
create index if not exists ix_v113_faturas_tenant_id on plantaopro.v113_faturas (tenant_id);
create table if not exists plantaopro.v113_titulos (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, fatura_id uuid, valor numeric(14,2) default 0, status varchar(40), demo_boleto boolean default false, vencimento timestamptz);
alter table if exists plantaopro.v113_titulos add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_titulos add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_titulos add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_titulos add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_titulos add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_titulos add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_titulos add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_titulos add column if not exists fatura_id uuid;
alter table if exists plantaopro.v113_titulos add column if not exists valor numeric(14,2) default 0;
alter table if exists plantaopro.v113_titulos add column if not exists status varchar(40);
alter table if exists plantaopro.v113_titulos add column if not exists demo_boleto boolean default false;
alter table if exists plantaopro.v113_titulos add column if not exists vencimento timestamptz;
create index if not exists ix_v113_titulos_cliente_id on plantaopro.v113_titulos (cliente_id);
create index if not exists ix_v113_titulos_status on plantaopro.v113_titulos (status);
create index if not exists ix_v113_titulos_created_at on plantaopro.v113_titulos (created_at);
create index if not exists ix_v113_titulos_tenant_id on plantaopro.v113_titulos (tenant_id);
create table if not exists plantaopro.v113_outbox_eventos (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, tipo varchar(80), payload_ref varchar(120), payload jsonb default '{}'::jsonb, status varchar(40), erro text);
alter table if exists plantaopro.v113_outbox_eventos add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_outbox_eventos add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_outbox_eventos add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_outbox_eventos add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_outbox_eventos add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_outbox_eventos add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_outbox_eventos add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_outbox_eventos add column if not exists tipo varchar(80);
alter table if exists plantaopro.v113_outbox_eventos add column if not exists payload_ref varchar(120);
alter table if exists plantaopro.v113_outbox_eventos add column if not exists payload jsonb default '{}'::jsonb;
alter table if exists plantaopro.v113_outbox_eventos add column if not exists status varchar(40);
alter table if exists plantaopro.v113_outbox_eventos add column if not exists erro text;
create index if not exists ix_v113_outbox_eventos_cliente_id on plantaopro.v113_outbox_eventos (cliente_id);
create index if not exists ix_v113_outbox_eventos_status on plantaopro.v113_outbox_eventos (status);
create index if not exists ix_v113_outbox_eventos_created_at on plantaopro.v113_outbox_eventos (created_at);
create index if not exists ix_v113_outbox_eventos_tenant_id on plantaopro.v113_outbox_eventos (tenant_id);
create table if not exists plantaopro.v113_outbox_logs (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, outbox_evento_id uuid, status varchar(40), detalhe text);
alter table if exists plantaopro.v113_outbox_logs add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_outbox_logs add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_outbox_logs add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_outbox_logs add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_outbox_logs add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_outbox_logs add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_outbox_logs add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_outbox_logs add column if not exists outbox_evento_id uuid;
alter table if exists plantaopro.v113_outbox_logs add column if not exists status varchar(40);
alter table if exists plantaopro.v113_outbox_logs add column if not exists detalhe text;
create index if not exists ix_v113_outbox_logs_cliente_id on plantaopro.v113_outbox_logs (cliente_id);
create index if not exists ix_v113_outbox_logs_status on plantaopro.v113_outbox_logs (status);
create index if not exists ix_v113_outbox_logs_created_at on plantaopro.v113_outbox_logs (created_at);
create index if not exists ix_v113_outbox_logs_tenant_id on plantaopro.v113_outbox_logs (tenant_id);
create table if not exists plantaopro.v113_templates (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, codigo varchar(80) unique, nome varchar(160), descricao text, status varchar(40));
alter table if exists plantaopro.v113_templates add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_templates add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_templates add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_templates add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_templates add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_templates add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_templates add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_templates add column if not exists codigo varchar(80) unique;
alter table if exists plantaopro.v113_templates add column if not exists nome varchar(160);
alter table if exists plantaopro.v113_templates add column if not exists descricao text;
alter table if exists plantaopro.v113_templates add column if not exists status varchar(40);
create index if not exists ix_v113_templates_cliente_id on plantaopro.v113_templates (cliente_id);
create index if not exists ix_v113_templates_status on plantaopro.v113_templates (status);
create index if not exists ix_v113_templates_created_at on plantaopro.v113_templates (created_at);
create index if not exists ix_v113_templates_tenant_id on plantaopro.v113_templates (tenant_id);
create table if not exists plantaopro.v113_template_instalacoes (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, template_id uuid, status varchar(40));
alter table if exists plantaopro.v113_template_instalacoes add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_template_instalacoes add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_template_instalacoes add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_template_instalacoes add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_template_instalacoes add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_template_instalacoes add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_template_instalacoes add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_template_instalacoes add column if not exists template_id uuid;
alter table if exists plantaopro.v113_template_instalacoes add column if not exists status varchar(40);
create index if not exists ix_v113_template_instalacoes_cliente_id on plantaopro.v113_template_instalacoes (cliente_id);
create index if not exists ix_v113_template_instalacoes_status on plantaopro.v113_template_instalacoes (status);
create index if not exists ix_v113_template_instalacoes_created_at on plantaopro.v113_template_instalacoes (created_at);
create index if not exists ix_v113_template_instalacoes_tenant_id on plantaopro.v113_template_instalacoes (tenant_id);
create table if not exists plantaopro.v113_jornada_acoes (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, codigo varchar(80), status varchar(40), detalhe text, erro text, open_url varchar(180), ordem int default 0);
alter table if exists plantaopro.v113_jornada_acoes add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_jornada_acoes add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_jornada_acoes add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_jornada_acoes add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_jornada_acoes add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_jornada_acoes add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_jornada_acoes add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_jornada_acoes add column if not exists codigo varchar(80);
alter table if exists plantaopro.v113_jornada_acoes add column if not exists status varchar(40);
alter table if exists plantaopro.v113_jornada_acoes add column if not exists detalhe text;
alter table if exists plantaopro.v113_jornada_acoes add column if not exists erro text;
alter table if exists plantaopro.v113_jornada_acoes add column if not exists open_url varchar(180);
alter table if exists plantaopro.v113_jornada_acoes add column if not exists ordem int default 0;
create index if not exists ix_v113_jornada_acoes_cliente_id on plantaopro.v113_jornada_acoes (cliente_id);
create index if not exists ix_v113_jornada_acoes_status on plantaopro.v113_jornada_acoes (status);
create index if not exists ix_v113_jornada_acoes_created_at on plantaopro.v113_jornada_acoes (created_at);
create index if not exists ix_v113_jornada_acoes_tenant_id on plantaopro.v113_jornada_acoes (tenant_id);
create table if not exists plantaopro.v113_atividades (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, tipo varchar(80), descricao text, status varchar(40));
alter table if exists plantaopro.v113_atividades add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_atividades add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_atividades add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_atividades add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_atividades add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_atividades add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_atividades add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_atividades add column if not exists tipo varchar(80);
alter table if exists plantaopro.v113_atividades add column if not exists descricao text;
alter table if exists plantaopro.v113_atividades add column if not exists status varchar(40);
create index if not exists ix_v113_atividades_cliente_id on plantaopro.v113_atividades (cliente_id);
create index if not exists ix_v113_atividades_status on plantaopro.v113_atividades (status);
create index if not exists ix_v113_atividades_created_at on plantaopro.v113_atividades (created_at);
create index if not exists ix_v113_atividades_tenant_id on plantaopro.v113_atividades (tenant_id);
create table if not exists plantaopro.v113_auditoria (id uuid primary key, cliente_id uuid null, tenant_id uuid null, reg_status varchar(20) default 'A', created_at timestamptz default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null, usuario_id uuid, entidade varchar(120), entidade_id uuid, acao varchar(120), detalhes jsonb default '{}'::jsonb, sucesso boolean default true, ip_origem varchar(80), perfil varchar(120), status varchar(40));
alter table if exists plantaopro.v113_auditoria add column if not exists cliente_id uuid null;
alter table if exists plantaopro.v113_auditoria add column if not exists tenant_id uuid null;
alter table if exists plantaopro.v113_auditoria add column if not exists reg_status varchar(20) default 'A';
alter table if exists plantaopro.v113_auditoria add column if not exists created_at timestamptz default now();
alter table if exists plantaopro.v113_auditoria add column if not exists created_by uuid null;
alter table if exists plantaopro.v113_auditoria add column if not exists updated_at timestamptz null;
alter table if exists plantaopro.v113_auditoria add column if not exists updated_by uuid null;
alter table if exists plantaopro.v113_auditoria add column if not exists usuario_id uuid;
alter table if exists plantaopro.v113_auditoria add column if not exists entidade varchar(120);
alter table if exists plantaopro.v113_auditoria add column if not exists entidade_id uuid;
alter table if exists plantaopro.v113_auditoria add column if not exists acao varchar(120);
alter table if exists plantaopro.v113_auditoria add column if not exists detalhes jsonb default '{}'::jsonb;
alter table if exists plantaopro.v113_auditoria add column if not exists sucesso boolean default true;
alter table if exists plantaopro.v113_auditoria add column if not exists ip_origem varchar(80);
alter table if exists plantaopro.v113_auditoria add column if not exists perfil varchar(120);
alter table if exists plantaopro.v113_auditoria add column if not exists status varchar(40);
create index if not exists ix_v113_auditoria_cliente_id on plantaopro.v113_auditoria (cliente_id);
create index if not exists ix_v113_auditoria_status on plantaopro.v113_auditoria (status);
create index if not exists ix_v113_auditoria_created_at on plantaopro.v113_auditoria (created_at);
create index if not exists ix_v113_auditoria_tenant_id on plantaopro.v113_auditoria (tenant_id);

-- ============================================================
-- Seção 13 — Consolidação de produto v1.14
-- ============================================================

-- SOURCE: database/migrations/2026_v114_consolidacao_produto.sql
-- SOURCE-SHA256: a120e44846606bb5a714f36c0c3064e985df2231b5768821f09e7a594fdda337
-- v1.14 consolida o domínio PlantãoPro sobre as tabelas persistidas da v1.13.
-- Não cria módulo paralelo; adiciona estruturas pequenas para favoritos, filtros, atalhos e timelines.
create schema if not exists plantaopro;
create table if not exists plantaopro.v114_favoritos_usuario(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, usuario_id uuid null, titulo text not null, rota text not null, modulo text not null, reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null);
create table if not exists plantaopro.v114_filtros_salvos(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, usuario_id uuid null, nome text not null, rota text not null, filtros jsonb not null default '{}'::jsonb, reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null);
create table if not exists plantaopro.v114_timelines(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, entidade text not null, entidade_id uuid null, evento text not null, resumo text not null, perfil text null, dados_minimos jsonb not null default '{}'::jsonb, reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null);
create table if not exists plantaopro.v114_checklist_implantacao(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, titulo text not null, perfil_responsavel text not null, status text not null default 'PENDENTE', ordem int not null default 0, reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null);

-- ============================================================
-- Seção 14 — Regras de faturamento e repasses v1.15
-- ============================================================

-- SOURCE: database/migrations/2026_v115_regras_faturamento_repasses.sql
-- SOURCE-SHA256: a2830c43932f4f5c64bdc8835076d18d97f76b6a3312b0b1d2e4b10b60670cb8
create schema if not exists plantaopro;
create extension if not exists pgcrypto;

create table if not exists plantaopro.v115_regras_faturamento(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, codigo text not null, nome text not null, tipo_faturamento text not null, item_faturavel_id uuid null, convenio_id uuid null, valor_base numeric(12,2) not null default 0, percentual_desconto numeric(6,2) not null default 0, percentual_acrescimo numeric(6,2) not null default 0, status text not null default 'ATIVA', reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null);
create table if not exists plantaopro.v115_regras_repasse(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, referencia_id uuid null, medico_id uuid null, convenio_id uuid null, tipo_regra text not null default 'PERCENTUAL', percentual numeric(6,2) not null default 0, valor_fixo numeric(12,2) not null default 0, valor_base numeric(12,2) not null default 0, valor_repasse numeric(12,2) not null default 0, contestacao text null, status text not null default 'REGRA_ATIVA', reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null);
create table if not exists plantaopro.v115_regras_glosa(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, conta_receber_id uuid null, titulo_id uuid null, convenio_id uuid null, motivo text not null default 'REGRA_CONVENIO', valor_glosado numeric(12,2) not null default 0, percentual_glosa numeric(6,2) not null default 0, prazo_recurso timestamptz null, resolucao text null, status text not null default 'ABERTA', reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null);
create table if not exists plantaopro.v115_convenio_regras(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, convenio_id uuid null, nome text not null, prazo_recebimento_dias int not null default 30, exige_autorizacao boolean not null default false, status text not null default 'ATIVA', reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null);
create table if not exists plantaopro.v115_faturamento_eventos(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, tipo text not null, entidade_id uuid null, payload jsonb not null default '{}'::jsonb, status text not null default 'PENDENTE', reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null);
create table if not exists plantaopro.v115_recebimentos(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, conta_receber_id uuid null, valor_recebido numeric(12,2) not null default 0, forma text not null default 'MANUAL_AUDITADO', status text not null default 'RECEBIDO', reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null);
create table if not exists plantaopro.v115_configuracoes_financeiras(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, chave text not null, valor text not null, escopo text not null default 'TENANT', status text not null default 'ATIVA', reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null);
create table if not exists plantaopro.v115_jornada_perfil_progresso(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, perfil text not null, passo text not null, rota text not null, cta text not null, pendencia_relacionada text null, status text not null default 'PENDENTE', reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null);
create table if not exists plantaopro.v115_alertas_operacionais(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, tipo text not null, prioridade text not null, perfil_responsavel text not null, modulo text not null, entidade_id uuid null, cta text not null, rota text not null, prazo timestamptz null, status text not null default 'ABERTA', origem_regra text not null, reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null);
create table if not exists plantaopro.v115_configuracoes_mobile(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, perfil text not null, chave text not null, valor jsonb not null default '{}'::jsonb, status text not null default 'ATIVA', reg_status char(1) not null default 'A', created_at timestamptz not null default now(), created_by uuid null, updated_at timestamptz null, updated_by uuid null);

alter table if exists plantaopro.v115_regras_faturamento add column if not exists updated_by uuid null;
alter table if exists plantaopro.v115_regras_repasse add column if not exists contestacao text null;
alter table if exists plantaopro.v115_regras_glosa add column if not exists resolucao text null;
alter table if exists plantaopro.v115_alertas_operacionais add column if not exists origem_regra text not null default 'v115';

create index if not exists ix_v115_regras_faturamento_tenant_status_data on plantaopro.v115_regras_faturamento(tenant_id,reg_status,created_at);
create index if not exists ix_v115_regras_repasse_tenant_status_data on plantaopro.v115_regras_repasse(tenant_id,reg_status,created_at);
create index if not exists ix_v115_regras_glosa_tenant_status_data on plantaopro.v115_regras_glosa(tenant_id,reg_status,created_at);
create index if not exists ix_v115_convenio_regras_tenant_status_data on plantaopro.v115_convenio_regras(tenant_id,reg_status,created_at);
create index if not exists ix_v115_faturamento_eventos_tenant_status_data on plantaopro.v115_faturamento_eventos(tenant_id,reg_status,created_at);
create index if not exists ix_v115_recebimentos_tenant_status_data on plantaopro.v115_recebimentos(tenant_id,reg_status,created_at);
create index if not exists ix_v115_config_fin_tenant_status_data on plantaopro.v115_configuracoes_financeiras(tenant_id,reg_status,created_at);
create index if not exists ix_v115_jornada_tenant_status_data on plantaopro.v115_jornada_perfil_progresso(tenant_id,reg_status,created_at);
create index if not exists ix_v115_alertas_tenant_status_data on plantaopro.v115_alertas_operacionais(tenant_id,reg_status,created_at);
create index if not exists ix_v115_mobile_tenant_status_data on plantaopro.v115_configuracoes_mobile(tenant_id,reg_status,created_at);

-- ============================================================
-- Seção 15 — Painel público seguro v1.24.3
-- ============================================================

-- SOURCE: database/schema/150_v1243_painel_publico_seguro.sql
-- SOURCE-SHA256: 70a98b8b035155c391136e4b76af84b20145f82f570c7d68b35489eb7aa89ff9
set search_path to plantaopro, public;

create table if not exists plantaopro.paineis_publicos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null references plantaopro.clientes(id),
    unidade_id uuid not null,
    nome varchar(120) not null,
    logotipo_url varchar(500),
    cor_primaria varchar(9) not null default '#155EEF',
    ativo boolean not null default true,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz
);

create table if not exists plantaopro.painel_publico_tokens (
    id uuid primary key default gen_random_uuid(),
    painel_id uuid not null references plantaopro.paineis_publicos(id) on delete cascade,
    cliente_id uuid not null references plantaopro.clientes(id),
    token_hash char(64) not null,
    expira_em timestamptz not null,
    revogado_em timestamptz,
    ultima_utilizacao_em timestamptz,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    constraint ck_painel_token_hash_sha256 check (token_hash ~ '^[0-9a-f]{64}$'),
    constraint uq_painel_token_hash unique (token_hash)
);

create index if not exists ix_paineis_publicos_escopo on plantaopro.paineis_publicos(cliente_id, unidade_id) where reg_status='A' and ativo;
create index if not exists ix_painel_tokens_validade on plantaopro.painel_publico_tokens(painel_id, expira_em) where reg_status='A' and revogado_em is null;

-- ============================================================
-- Seção 15 — Produto vendável, agenda e operação mobile v1.44.0
-- ============================================================

-- SOURCE: database/schema/300_v1440_produto_vendavel_design_mobile_operacao.sql
-- SOURCE-SHA256: a91ac25f68d57fdefde5b37fa4831a12c8a347c86616dd61060d01146581a0ed
-- PlantãoPro v1.44.0 - produto vendável, agenda e operação mobile
CREATE TABLE IF NOT EXISTS agenda_eventos_operacionais (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, unidade_id uuid NULL,
 tipo varchar(40) NOT NULL, titulo varchar(180) NOT NULL, descricao text NULL,
 inicio_em timestamptz NOT NULL, fim_em timestamptz NOT NULL, status varchar(30) NOT NULL DEFAULT 'AGENDADO',
 origem_tipo varchar(50) NULL, origem_id uuid NULL, responsavel_usuario_id uuid NULL,
 metadados jsonb NOT NULL DEFAULT '{}'::jsonb, criado_em timestamptz NOT NULL DEFAULT now(), atualizado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT ck_agenda_evento_periodo CHECK (fim_em > inicio_em)
);
CREATE INDEX IF NOT EXISTS idx_agenda_eventos_tenant_periodo ON agenda_eventos_operacionais(tenant_id,inicio_em,fim_em);
CREATE INDEX IF NOT EXISTS idx_agenda_eventos_responsavel ON agenda_eventos_operacionais(responsavel_usuario_id,inicio_em) WHERE responsavel_usuario_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS medico_registros_jornada (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, medico_id uuid NOT NULL, escala_id uuid NOT NULL,
 tipo varchar(20) NOT NULL, registrado_em timestamptz NOT NULL DEFAULT now(), latitude numeric(9,6) NULL, longitude numeric(9,6) NULL,
 observacao varchar(1000) NULL, dispositivo jsonb NOT NULL DEFAULT '{}'::jsonb, criado_por uuid NOT NULL,
 CONSTRAINT ck_medico_jornada_tipo CHECK (tipo IN ('CHECKIN','CHECKOUT')),
 CONSTRAINT uq_medico_jornada_escala_tipo UNIQUE(escala_id,tipo)
);
CREATE INDEX IF NOT EXISTS idx_medico_jornada_medico_data ON medico_registros_jornada(medico_id,registrado_em DESC);

CREATE TABLE IF NOT EXISTS onboarding_progresso (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, etapa varchar(50) NOT NULL, status varchar(20) NOT NULL DEFAULT 'PENDENTE',
 dados jsonb NOT NULL DEFAULT '{}'::jsonb, concluido_por uuid NULL, concluido_em timestamptz NULL, atualizado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT uq_onboarding_tenant_etapa UNIQUE(tenant_id,etapa),
 CONSTRAINT ck_onboarding_status CHECK(status IN ('PENDENTE','EM_ANDAMENTO','CONCLUIDO','BLOQUEADO'))
);
CREATE TABLE IF NOT EXISTS relatorios_salvos_v144 (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, usuario_id uuid NOT NULL, nome varchar(140) NOT NULL,
 tipo varchar(40) NOT NULL, filtros jsonb NOT NULL DEFAULT '{}'::jsonb, formato_padrao varchar(10) NOT NULL DEFAULT 'CSV', criado_em timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS exportacoes_gerenciais (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, usuario_id uuid NOT NULL, relatorio_id uuid NULL,
 tipo varchar(40) NOT NULL, formato varchar(10) NOT NULL, status varchar(20) NOT NULL DEFAULT 'SOLICITADA', arquivo_chave varchar(500) NULL,
 expira_em timestamptz NULL, erro text NULL, solicitado_em timestamptz NOT NULL DEFAULT now(), concluido_em timestamptz NULL
);
CREATE INDEX IF NOT EXISTS idx_exportacoes_tenant_usuario ON exportacoes_gerenciais(tenant_id,usuario_id,solicitado_em DESC);

CREATE TABLE IF NOT EXISTS notificacoes_mobile (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, usuario_id uuid NOT NULL, categoria varchar(30) NOT NULL,
 severidade varchar(20) NOT NULL DEFAULT 'INFORMATIVA', titulo varchar(160) NOT NULL, mensagem text NOT NULL, destino_seguro varchar(500) NULL,
 agrupamento_chave varchar(160) NULL, lida_em timestamptz NULL, expira_em timestamptz NULL, criada_em timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_notificacoes_mobile_caixa ON notificacoes_mobile(tenant_id,usuario_id,lida_em,criada_em DESC);

CREATE TABLE IF NOT EXISTS white_label_previews (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, usuario_id uuid NOT NULL,
 tema jsonb NOT NULL, contraste_minimo numeric(4,2) NOT NULL, contraste_valido boolean NOT NULL,
 criado_em timestamptz NOT NULL DEFAULT now(), aplicado_em timestamptz NULL,
 CONSTRAINT ck_white_label_contraste CHECK (contraste_minimo >= 1 AND contraste_minimo <= 21),
 CONSTRAINT ck_white_label_aplicacao_segura CHECK (aplicado_em IS NULL OR contraste_valido)
);
CREATE TABLE IF NOT EXISTS acoes_rapidas_auditoria (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, usuario_id uuid NOT NULL, acao varchar(80) NOT NULL,
 entidade varchar(80) NULL, entidade_id uuid NULL, resultado varchar(20) NOT NULL, contexto jsonb NOT NULL DEFAULT '{}'::jsonb,
 ip inet NULL, criada_em timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_acoes_rapidas_auditoria_tenant ON acoes_rapidas_auditoria(tenant_id,criada_em DESC);

-- ============================================================
-- Seção 16 — Operação assistida v1.31.0
-- ============================================================

-- SOURCE: database/schema/210_v1310_consolidacao_operacao_assistida.sql
-- SOURCE-SHA256: 67b91b2ca48bdba2158be6c33971eac190178ea7b5f8fab17e1f389467feacde
-- PlantãoPro v1.31.0: operação assistida, persistência e concorrência otimista.
create schema if not exists plantaopro;
create table if not exists plantaopro.work_items (
 id uuid primary key default gen_random_uuid(), tenant_id uuid not null, cliente_id uuid, unidade_id uuid,
 tipo varchar(40) not null check (tipo in ('CONVITE_PENDENTE','ESCALA_SEM_COBERTURA','AGENDAMENTO_NAO_CONFIRMADO','PACIENTE_AGUARDANDO','TRIAGEM_PENDENTE','CONSULTA_EM_RASCUNHO','PRESCRICAO_PENDENTE','CONTA_VENCIDA','REPASSE_PENDENTE','OCORRENCIA_ABERTA','DOCUMENTO_PENDENTE','ALERTA_DE_SLA')),
 titulo varchar(160) not null, descricao text not null default '', status varchar(24) not null default 'ENTRADA', prioridade varchar(16) not null default 'NORMAL', responsavel_id uuid,
 posicao integer not null default 0, vence_em timestamptz, versao integer not null default 1, criado_por uuid not null, criado_em timestamptz not null default now(), atualizado_em timestamptz not null default now(), reg_status char(1) not null default 'A');
create index if not exists ix_work_items_contexto on plantaopro.work_items(tenant_id,unidade_id,status,posicao) where reg_status='A';
create table if not exists plantaopro.work_item_assignments (id uuid primary key default gen_random_uuid(), work_item_id uuid not null references plantaopro.work_items(id) on delete cascade, usuario_id uuid not null, atribuido_por uuid not null, atribuido_em timestamptz not null default now(), encerrado_em timestamptz);
create table if not exists plantaopro.work_item_comments (id uuid primary key default gen_random_uuid(), work_item_id uuid not null references plantaopro.work_items(id) on delete cascade, autor_id uuid not null, comentario text not null check(length(btrim(comentario))>0), criado_em timestamptz not null default now());
create table if not exists plantaopro.work_item_history (id uuid primary key default gen_random_uuid(), work_item_id uuid not null references plantaopro.work_items(id) on delete cascade, usuario_id uuid not null, acao varchar(40) not null, origem varchar(24), destino varchar(24), detalhes jsonb not null default '{}'::jsonb, criado_em timestamptz not null default now());
create table if not exists plantaopro.notifications (id uuid primary key default gen_random_uuid(), tenant_id uuid not null, cliente_id uuid, unidade_id uuid, categoria varchar(20) not null check(categoria in ('OPERACAO','ESCALA','CLINICA','FINANCEIRO','SEGURANCA','SISTEMA')), titulo varchar(160) not null, descricao text not null, url text, criado_em timestamptz not null default now(), expira_em timestamptz, reg_status char(1) not null default 'A');
create table if not exists plantaopro.notification_recipients (id uuid primary key default gen_random_uuid(), notification_id uuid not null references plantaopro.notifications(id) on delete cascade, usuario_id uuid not null, unique(notification_id,usuario_id));
create table if not exists plantaopro.notification_read_states (id uuid primary key default gen_random_uuid(), notification_id uuid not null references plantaopro.notifications(id) on delete cascade, usuario_id uuid not null, lida_em timestamptz not null default now(), unique(notification_id,usuario_id));
create table if not exists plantaopro.notification_preferences (id uuid primary key default gen_random_uuid(), tenant_id uuid not null, usuario_id uuid not null, categoria varchar(20) not null, in_app boolean not null default true, email boolean not null default false, push boolean not null default false, atualizado_em timestamptz not null default now(), unique(tenant_id,usuario_id,categoria));
create table if not exists plantaopro.user_favorites (id uuid primary key default gen_random_uuid(), tenant_id uuid not null, usuario_id uuid not null, tipo varchar(40) not null, referencia_id uuid, titulo varchar(160) not null, url text not null, criado_em timestamptz not null default now(), unique(tenant_id,usuario_id,tipo,referencia_id));
create table if not exists plantaopro.recent_items (id uuid primary key default gen_random_uuid(), tenant_id uuid not null, usuario_id uuid not null, tipo varchar(40) not null, referencia_id uuid, titulo varchar(160) not null, url text not null, acessado_em timestamptz not null default now());
create index if not exists ix_recent_items_user on plantaopro.recent_items(tenant_id,usuario_id,acessado_em desc);
create table if not exists plantaopro.saved_views (id uuid primary key default gen_random_uuid(), tenant_id uuid not null, usuario_id uuid not null, setor_id uuid, modulo varchar(40) not null, nome varchar(100) not null, configuracao jsonb not null default '{}'::jsonb, padrao boolean not null default false, compartilhada boolean not null default false, criado_em timestamptz not null default now(), atualizado_em timestamptz not null default now());
create table if not exists plantaopro.saved_filters (id uuid primary key default gen_random_uuid(), saved_view_id uuid not null references plantaopro.saved_views(id) on delete cascade, campo varchar(80) not null, operador varchar(24) not null, valor jsonb not null);
create table if not exists plantaopro.medico_disponibilidade (id uuid primary key default gen_random_uuid(), tenant_id uuid not null, medico_id uuid not null, plantao_noturno boolean not null default false, finais_semana boolean not null default false, antecedencia_minima_horas integer not null default 0 check(antecedencia_minima_horas>=0), limite_semanal_horas integer check(limite_semanal_horas between 1 and 168), observacao text, versao integer not null default 1, atualizado_por uuid not null, atualizado_em timestamptz not null default now(), unique(tenant_id,medico_id));
create table if not exists plantaopro.medico_disponibilidade_periodos (id uuid primary key default gen_random_uuid(), disponibilidade_id uuid not null references plantaopro.medico_disponibilidade(id) on delete cascade, dia_semana smallint not null check(dia_semana between 0 and 6), horario_inicio time not null, horario_fim time not null, unidade_id uuid, especialidade_id uuid, check(horario_inicio < horario_fim));
create table if not exists plantaopro.medico_indisponibilidades (id uuid primary key default gen_random_uuid(), disponibilidade_id uuid not null references plantaopro.medico_disponibilidade(id) on delete cascade, inicio timestamptz not null, fim timestamptz not null, motivo varchar(300), check(inicio < fim));
create table if not exists plantaopro.agenda_change_history (id uuid primary key default gen_random_uuid(), tenant_id uuid not null, agendamento_id uuid not null, usuario_id uuid not null, origem jsonb not null, destino jsonb not null, idempotency_key uuid not null, criado_em timestamptz not null default now(), unique(tenant_id,idempotency_key));
create table if not exists plantaopro.operational_transition_history (id uuid primary key default gen_random_uuid(), tenant_id uuid not null, unidade_id uuid, entidade varchar(40) not null, entidade_id uuid not null, origem varchar(40) not null, destino varchar(40) not null, usuario_id uuid not null, versao integer not null, idempotency_key uuid not null, criado_em timestamptz not null default now(), unique(tenant_id,idempotency_key));
-- Consolida a tabela histórica sem mascará-la no comparador de schemas.
do $migration$ declare cols text; begin
 if to_regclass('plantaopro.perfis_permissoes') is not null then
  select string_agg(column_name,',') into cols from information_schema.columns where table_schema='plantaopro' and table_name='perfis_permissoes';
  if position('perfil_id' in coalesce(cols,''))>0 and position('permissao_id' in coalesce(cols,''))>0 then
   execute 'insert into plantaopro.perfil_permissoes(perfil_id,permissao_id,permitido,bloqueado_por_plano,reg_status,reg_date) select perfil_id,permissao_id,' || case when position('permitido' in cols)>0 then 'coalesce(permitido,true)' else 'true' end || ',' || case when position('bloqueado_por_plano' in cols)>0 then 'coalesce(bloqueado_por_plano,false)' else 'false' end || ',' || case when position('reg_status' in cols)>0 then 'coalesce(reg_status,''A'')' else '''A''' end || ',' || case when position('reg_date' in cols)>0 then 'coalesce(reg_date,now())' else 'now()' end || ' from plantaopro.perfis_permissoes legacy where not exists(select 1 from plantaopro.perfil_permissoes canonical where canonical.perfil_id=legacy.perfil_id and canonical.permissao_id=legacy.permissao_id)';
  end if;
  if not exists(select 1 from pg_constraint where confrelid='plantaopro.perfis_permissoes'::regclass) then drop table plantaopro.perfis_permissoes; end if;
 end if;
end $migration$;
insert into plantaopro.schema_migrations(id,script_path,checksum,applied_at) select 'v1.31.0','database/migrations/2026_v1310_consolidacao_operacao_assistida.sql','runtime-managed',now() where not exists(select 1 from plantaopro.schema_migrations where id='v1.31.0');

-- ============================================================
-- Seção 17 — Bootstrap seguro e catálogo canônico v1.37.0
-- ============================================================

-- SOURCE: database/schema/250_v1370_bootstrap_superadmin.sql
-- SOURCE-SHA256: 6f5469f6e5fb49e8eb5026c2b22f76c1cf02edcc8b2f0c6f7f5fbc154b91457f
-- v1.37.0: catálogo mínimo determinístico e infraestrutura do bootstrap.
SET search_path TO plantaopro, public;
SELECT pg_advisory_lock(hashtext('plantaopro.install.v1370'));

ALTER TABLE plantaopro.schema_migrations
    ADD COLUMN IF NOT EXISTS versao text,
    ADD COLUMN IF NOT EXISTS nome text,
    ADD COLUMN IF NOT EXISTS iniciado_em timestamptz,
    ADD COLUMN IF NOT EXISTS aplicado_em timestamptz,
    ADD COLUMN IF NOT EXISTS duracao_ms bigint,
    ADD COLUMN IF NOT EXISTS status text,
    ADD COLUMN IF NOT EXISTS erro_resumido text,
    ADD COLUMN IF NOT EXISTS executado_por text,
    ADD COLUMN IF NOT EXISTS ambiente text;
UPDATE plantaopro.schema_migrations
SET versao=coalesce(versao,id), nome=coalesce(nome,script_path), iniciado_em=coalesce(iniciado_em,applied_at), aplicado_em=coalesce(aplicado_em,applied_at),
    duracao_ms=coalesce(duracao_ms,0), status=coalesce(status,'APLICADA'), executado_por=coalesce(executado_por,current_user),
    ambiente=coalesce(ambiente,'UNKNOWN');
ALTER TABLE plantaopro.schema_migrations
    ALTER COLUMN versao SET DEFAULT '', ALTER COLUMN versao SET NOT NULL,
    ALTER COLUMN nome SET DEFAULT '', ALTER COLUMN nome SET NOT NULL,
    ALTER COLUMN duracao_ms SET DEFAULT 0, ALTER COLUMN duracao_ms SET NOT NULL,
    ALTER COLUMN status SET DEFAULT 'APLICADA', ALTER COLUMN status SET NOT NULL;


WITH catalog(codigo,nome,ordem) AS (VALUES
 ('ADMIN_SAAS','Administração SaaS',10),('TENANTS','Tenants',20),('CLIENTES','Clientes',30),('PLANOS','Planos',40),
 ('ASSINATURAS','Assinaturas',50),('USUARIOS','Usuários',60),('PERFIS','Perfis',70),('PERMISSOES','Permissões',80),
 ('AUDITORIA','Auditoria',90),('SEGURANCA','Segurança',100),('CONFIGURACOES','Configurações',110),
 ('FEATURE_FLAGS','Feature flags',120),('OBSERVABILIDADE','Observabilidade',130),('SUPORTE','Suporte',140),
 ('OPERACAO360','Operação 360',150),('SAUDE360','Saúde 360',160),('FINANCEIRO','Financeiro',170),('RELATORIOS','Relatórios',180)
)
INSERT INTO plantaopro.modulos_sistema(id,codigo,nome,descricao,ordem,status,reg_status)
SELECT md5('module:'||c.codigo)::uuid,c.codigo,c.nome,'Módulo canônico PlantãoPro',c.ordem,'ATIVO','A' FROM catalog c
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.modulos_sistema m WHERE upper(btrim(m.codigo))=c.codigo AND m.reg_status='A');

WITH catalog(codigo,nome,ordem,sensivel) AS (VALUES
 ('VER','Ver',10,false),('LISTAR','Listar',20,false),('CRIAR','Criar',30,false),('EDITAR','Editar',40,false),
 ('INATIVAR','Inativar',50,true),('REATIVAR','Reativar',60,true),('APROVAR','Aprovar',70,true),('CANCELAR','Cancelar',80,true),
 ('EXCLUIR','Excluir',90,true),('EXPORTAR','Exportar',100,true),('CONFIGURAR','Configurar',110,true),
 ('GERENCIAR','Gerenciar',120,true),('IMPERSONAR','Impersonar',130,true),('AUDITAR','Auditar',140,true),
 ('EXECUTAR','Executar',150,true),('REPROCESSAR','Reprocessar',160,true),('VER_DADOS_SENSIVEIS','Ver dados sensíveis',170,true)
)
INSERT INTO plantaopro.acoes_sistema(id,codigo,nome,descricao,ordem,sensivel,status,reg_status)
SELECT md5('action:'||c.codigo)::uuid,c.codigo,c.nome,'Ação canônica PlantãoPro',c.ordem,c.sensivel,'ATIVO','A' FROM catalog c
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.acoes_sistema a WHERE upper(btrim(a.codigo))=c.codigo AND a.reg_status='A');

WITH combinations(modulo,acao) AS (VALUES
 ('ADMIN_SAAS','VER'),('ADMIN_SAAS','GERENCIAR'),('TENANTS','LISTAR'),('TENANTS','CRIAR'),('TENANTS','EDITAR'),('TENANTS','INATIVAR'),('TENANTS','REATIVAR'),
 ('TENANTS','IMPERSONAR'),('CLIENTES','LISTAR'),('CLIENTES','GERENCIAR'),('PLANOS','LISTAR'),('PLANOS','GERENCIAR'),('ASSINATURAS','LISTAR'),
 ('ASSINATURAS','CANCELAR'),('USUARIOS','LISTAR'),('USUARIOS','GERENCIAR'),('PERFIS','LISTAR'),('PERFIS','GERENCIAR'),
 ('PERMISSOES','LISTAR'),('PERMISSOES','GERENCIAR'),('AUDITORIA','AUDITAR'),('AUDITORIA','EXPORTAR'),('SEGURANCA','VER'),
 ('SEGURANCA','GERENCIAR'),('CONFIGURACOES','CONFIGURAR'),('FEATURE_FLAGS','CONFIGURAR'),('OBSERVABILIDADE','VER'),
 ('SUPORTE','GERENCIAR'),('OPERACAO360','VER'),('OPERACAO360','EXECUTAR'),('SAUDE360','VER'),('SAUDE360','VER_DADOS_SENSIVEIS'),
 ('FINANCEIRO','VER'),('FINANCEIRO','EXPORTAR'),('RELATORIOS','VER'),('RELATORIOS','EXPORTAR')
)
INSERT INTO plantaopro.permissoes(id,codigo,nome,descricao,modulo,acao,modulo_id,acao_id,sensivel,status,reg_status)
SELECT md5('permission:'||c.modulo||':'||c.acao)::uuid,c.modulo||'.'||c.acao,c.modulo||' '||c.acao,
       'Permissão canônica PlantãoPro',c.modulo,c.acao,m.id,a.id,a.sensivel,'ATIVO','A'
FROM combinations c
JOIN plantaopro.modulos_sistema m ON upper(btrim(m.codigo))=c.modulo AND m.reg_status='A'
JOIN plantaopro.acoes_sistema a ON upper(btrim(a.codigo))=c.acao AND a.reg_status='A'
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.permissoes p WHERE upper(btrim(p.codigo))=c.modulo||'.'||c.acao AND p.reg_status='A');

-- ============================================================
-- Seção 18 — Seeds obrigatórios de sistema v1.95.1
-- ============================================================

-- SOURCE: database/seeds/system/010_modulos.sql
-- SOURCE-SHA256: ee27f14e0966c5aef1ba6e7669182dc3bec4382de80dabd99dc49506fc2f0bf3
-- Catálogo real de módulos consumidos pelo runtime. IDs determinísticos tornam o replay seguro.
WITH catalog(codigo,nome,ordem) AS (VALUES
 ('ADMIN_SAAS','Administração SaaS',10),('TENANTS','Tenants',20),('CLIENTES','Clientes',30),('PLANOS','Planos',40),
 ('ASSINATURAS','Assinaturas',50),('USUARIOS','Usuários',60),('PERFIS','Perfis',70),('PERMISSOES','Permissões',80),
 ('PLANTOES','Plantões',90),('ESCALAS','Escalas',100),('PACIENTES','Pacientes',110),('CONSULTAS','Consultas',120),
 ('FINANCEIRO','Financeiro',130),('RELATORIOS','Relatórios',140),('AUDITORIA','Auditoria',150),('SEGURANCA','Segurança',160),
 ('COBERTURA','Cobertura inteligente',170),('FECHAMENTO','Fechamento operacional',180)
)
INSERT INTO plantaopro.modulos_sistema(id,codigo,nome,descricao,ordem,status,reg_status)
SELECT md5('module:'||codigo)::uuid,codigo,nome,'Módulo canônico PlantãoPro',ordem,'ATIVO','A' FROM catalog
ON CONFLICT DO NOTHING;

-- SOURCE: database/seeds/system/020_acoes.sql
-- SOURCE-SHA256: 4b192295da67ad56935ed39879bc12eb73825756e21ad7e07e7a45bc95a54238
WITH catalog(codigo,nome,ordem,sensivel) AS (VALUES
 ('VER','Ver',10,false),('LISTAR','Listar',20,false),('CRIAR','Criar',30,false),('EDITAR','Editar',40,false),
 ('GERENCIAR','Gerenciar',50,true),('SUSPENDER','Suspender',60,true),('IMPERSONAR','Impersonar',70,true),
 ('PUBLICAR','Publicar',80,true),('CANCELAR','Cancelar',90,true),('CONFIRMAR','Confirmar',100,false),
 ('RECUSAR','Recusar',110,false),('SUBSTITUIR','Substituir',120,true),('INICIAR','Iniciar',130,false),
 ('FINALIZAR','Finalizar',140,true),('EXPORTAR','Exportar',150,true),('CONVIDAR','Convidar',160,true),
 ('REALIZAR','Realizar',170,true),('CONFERIR','Conferir',180,true),('APROVAR','Aprovar',190,true),
 ('REABRIR','Reabrir',200,true),('PAGAR','Pagar',210,true)
)
INSERT INTO plantaopro.acoes_sistema(id,codigo,nome,descricao,ordem,sensivel,status,reg_status)
SELECT md5('action:'||codigo)::uuid,codigo,nome,'Ação canônica PlantãoPro',ordem,sensivel,'ATIVO','A' FROM catalog
ON CONFLICT DO NOTHING;

-- SOURCE: database/seeds/system/030_permissoes.sql
-- SOURCE-SHA256: af8f4a93a329103f764da681fd3b3b15c72c33724c447d1efff4b5db24416833
WITH catalog(modulo,acao) AS (VALUES
 ('ADMIN_SAAS','VER'),('ADMIN_SAAS','GERENCIAR'),('TENANTS','LISTAR'),('TENANTS','CRIAR'),('TENANTS','EDITAR'),('TENANTS','SUSPENDER'),('TENANTS','IMPERSONAR'),
 ('USUARIOS','LISTAR'),('USUARIOS','CRIAR'),('USUARIOS','EDITAR'),('PERFIS','LISTAR'),('PERFIS','GERENCIAR'),
 ('PLANTOES','LISTAR'),('PLANTOES','CRIAR'),('PLANTOES','EDITAR'),('PLANTOES','PUBLICAR'),('PLANTOES','CANCELAR'),
 ('ESCALAS','LISTAR'),('ESCALAS','CONFIRMAR'),('ESCALAS','RECUSAR'),('ESCALAS','SUBSTITUIR'),
 ('PACIENTES','LISTAR'),('PACIENTES','CRIAR'),('CONSULTAS','INICIAR'),('CONSULTAS','EDITAR'),('CONSULTAS','FINALIZAR'),
 ('FINANCEIRO','VER'),('FINANCEIRO','GERENCIAR'),('RELATORIOS','VER'),('RELATORIOS','EXPORTAR'),('AUDITORIA','VER'),('SEGURANCA','GERENCIAR'),
 ('COBERTURA','VER'),('COBERTURA','GERENCIAR'),('COBERTURA','CONVIDAR'),
 ('ESCALAS','REALIZAR'),('FECHAMENTO','VER'),('FECHAMENTO','CONFERIR'),('FECHAMENTO','APROVAR'),('FECHAMENTO','REABRIR'),
 ('FINANCEIRO','APROVAR'),('FINANCEIRO','PAGAR'),('FINANCEIRO','CANCELAR'),('FINANCEIRO','EXPORTAR')
)
INSERT INTO plantaopro.permissoes(id,codigo,nome,descricao,modulo,acao,modulo_id,acao_id,sensivel,status,reg_status)
SELECT md5('permission:'||c.modulo||':'||c.acao)::uuid,c.modulo||'.'||c.acao,c.modulo||' '||c.acao,'Permissão canônica',c.modulo,c.acao,m.id,a.id,a.sensivel,'ATIVO','A'
FROM catalog c JOIN plantaopro.modulos_sistema m ON m.codigo=c.modulo AND m.reg_status='A'
JOIN plantaopro.acoes_sistema a ON a.codigo=c.acao AND a.reg_status='A'
ON CONFLICT DO NOTHING;

-- SOURCE: database/seeds/system/040_perfis.sql
-- SOURCE-SHA256: c3c0e38970a10bafcd2fbcbee38ac6e930ef68fd69cb9b0b02d4f9a7a115f7f5
WITH catalog(codigo,nome) AS (VALUES
 ('ADMINISTRADOR_GLOBAL','Administrador global'),('ADMINISTRADOR_CLIENTE','Administrador do cliente'),('ADMINISTRADOR_CLINICA','Administrador da clínica'),
 ('COORDENACAO','Coordenação'),('OPERADOR','Operador'),('MEDICO','Médico'),('HOSPITAL','Hospital'),('RECEPCAO','Recepção'),('TRIAGEM','Triagem'),
 ('ENFERMAGEM','Enfermagem'),('FINANCEIRO','Financeiro'),('FATURAMENTO_CONVENIO','Faturamento de convênio'),('AUDITOR','Auditor'),
 ('AUDITOR_CLINICO','Auditor clínico'),('SUPORTE','Suporte')
)
INSERT INTO plantaopro.perfis(id,tenant_id,cliente_id,codigo,nome,descricao,base_sistema,customizado,status,reg_status)
SELECT md5('profile:'||codigo)::uuid,NULL,NULL,codigo,nome,'Perfil canônico de sistema',true,false,'ATIVO','A' FROM catalog
ON CONFLICT DO NOTHING;

-- SOURCE: database/seeds/system/050_perfil_permissoes.sql
-- SOURCE-SHA256: aa6efe9df7c40694b6bbc862a2de123f6a96e16f273b33205a722c4e609992dd
-- Global recebe o catálogo; demais perfis recebem somente famílias necessárias ao trabalho.
INSERT INTO plantaopro.perfil_permissoes(id,perfil_id,permissao_id,permitido,bloqueado_por_plano,reg_status)
SELECT md5('profile-permission:'||p.codigo||':'||x.codigo)::uuid,p.id,x.id,true,false,'A'
FROM plantaopro.perfis p CROSS JOIN plantaopro.permissoes x
WHERE p.codigo='ADMINISTRADOR_GLOBAL' AND p.reg_status='A' AND x.reg_status='A'
ON CONFLICT DO NOTHING;
WITH matrix(perfil,modulo) AS (VALUES
 ('ADMINISTRADOR_CLIENTE','USUARIOS'),('ADMINISTRADOR_CLIENTE','PERFIS'),('ADMINISTRADOR_CLIENTE','PLANTOES'),('ADMINISTRADOR_CLIENTE','ESCALAS'),
 ('ADMINISTRADOR_CLINICA','USUARIOS'),('ADMINISTRADOR_CLINICA','PLANTOES'),('ADMINISTRADOR_CLINICA','ESCALAS'),('COORDENACAO','PLANTOES'),('COORDENACAO','ESCALAS'),
 ('OPERADOR','PLANTOES'),('OPERADOR','ESCALAS'),('MEDICO','PLANTOES'),('MEDICO','ESCALAS'),('HOSPITAL','PLANTOES'),('HOSPITAL','ESCALAS'),
 ('RECEPCAO','PACIENTES'),('TRIAGEM','PACIENTES'),('TRIAGEM','CONSULTAS'),('ENFERMAGEM','PACIENTES'),('ENFERMAGEM','CONSULTAS'),
 ('FINANCEIRO','FINANCEIRO'),('FATURAMENTO_CONVENIO','FINANCEIRO'),('AUDITOR','AUDITORIA'),('AUDITOR_CLINICO','AUDITORIA'),('SUPORTE','USUARIOS')
)
INSERT INTO plantaopro.perfil_permissoes(id,perfil_id,permissao_id,permitido,bloqueado_por_plano,reg_status)
SELECT md5('profile-permission:'||p.codigo||':'||x.codigo)::uuid,p.id,x.id,true,false,'A'
FROM matrix m JOIN plantaopro.perfis p ON p.codigo=m.perfil AND p.reg_status='A'
JOIN plantaopro.permissoes x ON x.modulo=m.modulo AND x.reg_status='A'
ON CONFLICT DO NOTHING;

-- SOURCE: database/seeds/system/060_politica_senha.sql
-- SOURCE-SHA256: 3d5ffaab64fdddd3c882f522383d18dc5f06d7bc24802d826bb759aeb6cf873e
INSERT INTO plantaopro.politicas_senha(id,tenant_id,tamanho_minimo,exige_maiuscula,exige_minuscula,exige_numero,exige_especial,expiracao_dias,tentativas_permitidas,bloqueio_minutos,reg_status)
SELECT md5('password-policy:global')::uuid,NULL,12,true,true,true,true,90,5,15,'A'
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.politicas_senha WHERE tenant_id IS NULL AND reg_status='A');

-- SOURCE: database/seeds/system/070_planos_recursos.sql
-- SOURCE-SHA256: 078253b958cb25cffef4f040f6e612845d89dcaf3da3683b0c98e14e468bda3d
-- O runtime aceita catálogo comercial vazio; nenhum cliente ou assinatura fictícia é criado.
SELECT 1 AS catalogo_comercial_opcional;

-- SOURCE: database/seeds/system/080_parametros_globais.sql
-- SOURCE-SHA256: 55c6012d5e6d782d95715932118f4f1b458d61f90693316caa6f2343cae28a45
CREATE TABLE IF NOT EXISTS plantaopro.parametros_sistema (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, codigo text NOT NULL, categoria text NOT NULL, nome text NOT NULL,
 descricao text NOT NULL DEFAULT '', tipo text NOT NULL, valor text NULL, valor_padrao text NULL, sensivel boolean NOT NULL DEFAULT false,
 editavel boolean NOT NULL DEFAULT true, status text NOT NULL DEFAULT 'ATIVO', reg_status char(1) NOT NULL DEFAULT 'A',
 reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_parametros_sistema_global_codigo ON plantaopro.parametros_sistema(lower(codigo)) WHERE tenant_id IS NULL AND reg_status='A';
WITH catalog(codigo,categoria,nome,tipo,valor) AS (VALUES
 ('SISTEMA.LOCALE','SISTEMA','Localidade','TEXTO','pt-BR'),('SISTEMA.TIMEZONE','SISTEMA','Fuso horário','TEXTO','America/Belem'),
 ('SISTEMA.CURRENCY','SISTEMA','Moeda','TEXTO','BRL'),('SISTEMA.DATE_FORMAT','SISTEMA','Formato de data','TEXTO','dd/MM/yyyy'),('SISTEMA.TIME_FORMAT','SISTEMA','Formato de hora','TEXTO','HH:mm'),
 ('SEGURANCA.LOGIN_MAX_TENTATIVAS','SEGURANCA','Máximo de tentativas','INTEIRO','5'),('SEGURANCA.LOGIN_BLOQUEIO_MINUTOS','SEGURANCA','Bloqueio do login','INTEIRO','15'),
 ('SEGURANCA.SENHA_TAMANHO_MINIMO','SEGURANCA','Tamanho mínimo da senha','INTEIRO','12'),('SEGURANCA.SENHA_EXPIRACAO_DIAS','SEGURANCA','Expiração da senha','INTEIRO','90'),
 ('SEGURANCA.SESSAO_MINUTOS','SEGURANCA','Duração da sessão','INTEIRO','60'),('OPERACAO.PLANTAO_DURACAO_MAXIMA_HORAS','OPERACAO','Duração máxima','INTEIRO','168'),
 ('OPERACAO.CONFLITO_INTERVALO_MINUTOS','OPERACAO','Intervalo de conflito','INTEIRO','0'),('OPERACAO.CANCELAMENTO_ANTECEDENCIA_HORAS','OPERACAO','Antecedência de cancelamento','INTEIRO','24'),
 ('NOTIFICACOES.EMAIL_ATIVO','NOTIFICACOES','E-mail ativo','BOOLEANO','false'),('NOTIFICACOES.PUSH_ATIVO','NOTIFICACOES','Push ativo','BOOLEANO','false'),
 ('NOTIFICACOES.WHATSAPP_ATIVO','NOTIFICACOES','WhatsApp ativo','BOOLEANO','false'),('FINANCEIRO.MOEDA','FINANCEIRO','Moeda','TEXTO','BRL'),
 ('FINANCEIRO.CASAS_DECIMAIS','FINANCEIRO','Casas decimais','INTEIRO','2'),('ARQUIVOS.TAMANHO_MAXIMO_MB','ARQUIVOS','Tamanho máximo','INTEIRO','25'),
 ('LGPD.RETENCAO_LOGS_DIAS','LGPD','Retenção de logs','INTEIRO','365')
)
INSERT INTO plantaopro.parametros_sistema(id,codigo,categoria,nome,tipo,valor,valor_padrao)
SELECT md5('parameter:'||codigo)::uuid,codigo,categoria,nome,tipo,valor,valor FROM catalog ON CONFLICT DO NOTHING;

-- SOURCE: database/seeds/system/090_notificacoes.sql
-- SOURCE-SHA256: d0227e5cfa331213531e0465c30fd822aa6baae84173766fc231657f380d6bb6
-- Catálogo notificacoes: estruturas canônicas aceitam estado vazio e configuração posterior.
SELECT 1 AS seed_090_notificacoes;

-- SOURCE: database/seeds/system/100_status_operacionais.sql
-- SOURCE-SHA256: ef836ab01b0bfd1061176118379ba57f9d3def83c88445ee68917d57973945cb
-- Catálogo status_operacionais: estruturas canônicas aceitam estado vazio e configuração posterior.
SELECT 1 AS seed_100_status_operacionais;

-- SOURCE: database/seeds/system/110_configuracoes_runtime.sql
-- SOURCE-SHA256: 076be98b9c8f146b3c6166ee6d9caa0d01fc9dc1d5eb24c90f62fe0cbfe932db
-- Checkpoint de schema concluído somente após todas as estruturas e seeds anteriores.
INSERT INTO plantaopro.schema_migrations(id,versao,nome,script_path,checksum,iniciado_em,applied_at,aplicado_em,duracao_ms,status,executado_por,ambiente)
SELECT 'v1.95.1','v1.95.1','One-click database runtime-ready','database/install-manifest.json','manifest-managed',now(),now(),now(),0,'APLICADA',current_user,'INSTALL'
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.schema_migrations WHERE id='v1.95.1');

-- ============================================================
-- Seção 19 — Produto operacional premium v1.40.0
-- ============================================================

-- SOURCE: database/schema/260_v1400_produto_operacional_premium.sql
-- SOURCE-SHA256: 5ac8669035bf9cff19eb0c3d576c0a8e6b9e7d49451d35ada413648f6b525174
-- PlantãoPro v1.40.0 — trilha operacional, cobertura e fechamento.
-- Estruturas aditivas, idempotentes e isoladas por tenant.

alter table if exists plantaopro.saved_views
    add column if not exists filtros jsonb not null default '{}'::jsonb;
alter table if exists plantaopro.saved_views
    add column if not exists visualizacao varchar(24) not null default 'TABELA';

create table if not exists plantaopro.operational_action_history (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null,
    usuario_id uuid,
    entidade varchar(40) not null,
    entidade_id uuid not null,
    acao varchar(60) not null,
    status_anterior varchar(40),
    status_novo varchar(40),
    motivo text,
    comentario text,
    metadata jsonb not null default '{}'::jsonb,
    ocorrido_em timestamptz not null default now()
);
create index if not exists ix_operational_action_history_entity
    on plantaopro.operational_action_history(tenant_id, entidade, entidade_id, ocorrido_em desc);

create table if not exists plantaopro.cobertura_auditoria (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null,
    plantao_id uuid not null,
    medico_id uuid,
    convite_id uuid,
    usuario_id uuid,
    acao varchar(60) not null,
    motivo text,
    criterios_ranking jsonb not null default '{}'::jsonb,
    ocorrido_em timestamptz not null default now()
);
create index if not exists ix_cobertura_auditoria_plantao
    on plantaopro.cobertura_auditoria(tenant_id, plantao_id, ocorrido_em desc);

create table if not exists plantaopro.fechamento_auditoria (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null,
    plantao_id uuid not null,
    escala_id uuid,
    pagamento_id uuid,
    usuario_id uuid,
    acao varchar(60) not null,
    valor_anterior numeric(14,2),
    valor_novo numeric(14,2),
    justificativa text,
    metadata jsonb not null default '{}'::jsonb,
    ocorrido_em timestamptz not null default now()
);
create index if not exists ix_fechamento_auditoria_plantao
    on plantaopro.fechamento_auditoria(tenant_id, plantao_id, ocorrido_em desc);

-- ============================================================
-- Seção 20 — Ciclo operacional v1.41.0
-- ============================================================

-- SOURCE: database/schema/270_v1410_cobertura_escalas_fechamento_financeiro.sql
-- SOURCE-SHA256: bfc383c6236d37f7863246636f12d111a57a67b8d168ed9b00beacaaa554c721
-- PlantãoPro v1.41.0 — cobertura, execução, fechamento e origem financeira.
-- Modelo aditivo e idempotente; todas as entidades operacionais carregam o tenant.
set search_path to plantaopro, public;

create table if not exists cobertura_sugestoes (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, plantao_id uuid not null,
    medico_id uuid not null, score smallint not null check (score between 0 and 100),
    criterios jsonb not null default '{}'::jsonb, elegivel boolean not null,
    impedimentos jsonb not null default '[]'::jsonb, calculado_em timestamptz not null default now(),
    unique (tenant_id, plantao_id, medico_id)
);
create index if not exists ix_cobertura_sugestoes_ranking on cobertura_sugestoes(tenant_id, plantao_id, elegivel, score desc);

create table if not exists cobertura_convites (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, plantao_id uuid not null,
    medico_id uuid not null, status varchar(20) not null default 'PENDENTE'
        check (status in ('PENDENTE','ACEITO','RECUSADO','CANCELADO','EXPIRADO')),
    mensagem text, criado_por uuid not null, criado_em timestamptz not null default now(),
    reenviado_em timestamptz, respondido_em timestamptz, cancelado_em timestamptz, motivo text
);
create unique index if not exists ux_cobertura_convite_pendente
    on cobertura_convites(tenant_id, plantao_id, medico_id) where status = 'PENDENTE';

create table if not exists escala_transicoes (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, escala_id uuid not null,
    estado_anterior varchar(24), estado_novo varchar(24) not null
        check (estado_novo in ('SOLICITADA','CONFIRMADA','RECUSADA','CANCELADA','SUBSTITUIDA','REALIZADA','AUSENTE','EM_FECHAMENTO','FECHADA')),
    motivo text, novo_medico_id uuid, executado_por uuid not null, executado_em timestamptz not null default now(), metadata jsonb not null default '{}'::jsonb
);
create index if not exists ix_escala_transicoes_timeline on escala_transicoes(tenant_id, escala_id, executado_em desc);

create table if not exists fechamento_plantao (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, plantao_id uuid not null,
    status varchar(24) not null default 'EM_CONFERENCIA' check (status in ('EM_CONFERENCIA','COM_DIVERGENCIA','APROVADO','FECHADO','REABERTO')),
    iniciado_por uuid not null, iniciado_em timestamptz not null default now(), aprovado_por uuid, aprovado_em timestamptz,
    fechado_em timestamptz, reaberto_em timestamptz, motivo_reabertura text, versao integer not null default 1
);
create unique index if not exists ux_fechamento_plantao_ativo on fechamento_plantao(tenant_id, plantao_id) where status <> 'REABERTO';

create table if not exists fechamento_plantao_escalas (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, fechamento_id uuid not null references fechamento_plantao(id),
    escala_id uuid not null, presenca boolean not null, horas_previstas numeric(6,2) not null default 0,
    horas_realizadas numeric(6,2) not null default 0 check (horas_realizadas >= 0), valor_previsto numeric(14,2) not null default 0,
    valor_calculado numeric(14,2) not null default 0, conferido_por uuid, conferido_em timestamptz, unique(tenant_id, fechamento_id, escala_id)
);
create table if not exists fechamento_divergencias (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, fechamento_id uuid not null references fechamento_plantao(id),
    escala_id uuid, tipo varchar(40) not null, descricao text not null check (length(trim(descricao)) >= 3),
    status varchar(20) not null default 'ABERTA' check (status in ('ABERTA','RESOLVIDA','CANCELADA')),
    criada_por uuid not null, criada_em timestamptz not null default now(), resolucao text, resolvida_por uuid, resolvida_em timestamptz
);
create table if not exists fechamento_aprovacoes (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, fechamento_id uuid not null references fechamento_plantao(id),
    aprovado_por uuid not null, decisao varchar(16) not null check (decisao in ('APROVADO','REJEITADO','REABERTO')),
    justificativa text, criado_em timestamptz not null default now()
);
create table if not exists financeiro_pagamento_origem (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, pagamento_id uuid not null,
    fechamento_id uuid not null references fechamento_plantao(id), escala_id uuid not null,
    criado_em timestamptz not null default now(), unique(tenant_id, pagamento_id), unique(tenant_id, escala_id)
);
create table if not exists work_item_contextos (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, work_item_id uuid not null,
    tipo varchar(40) not null, entidade_id uuid not null, rota_segura text not null, dados jsonb not null default '{}'::jsonb,
    criado_em timestamptz not null default now(), unique(tenant_id, work_item_id, tipo, entidade_id)
);

-- ============================================================
-- Seção 21 — Design executivo, operação inteligente e comercial v1.45.0
-- ============================================================

-- SOURCE: database/schema/310_v1450_design_system_executivo_operacao_comercial.sql
-- SOURCE-SHA256: 58f2affeb86244c30184eaf27922e5562439dbc4afe5f39d7218d09cd9b4856a
-- PlantãoPro v1.45.0 - operação inteligente, comercial B2B e experiência premium
-- Estruturas são tenant-aware e preservam histórico auditável das decisões operacionais.

CREATE TABLE IF NOT EXISTS agenda_evento_participantes (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, evento_id uuid NOT NULL,
 usuario_id uuid NULL, medico_id uuid NULL, papel varchar(30) NOT NULL DEFAULT 'PARTICIPANTE',
 status varchar(30) NOT NULL DEFAULT 'PENDENTE', respondido_em timestamptz NULL,
 criado_em timestamptz NOT NULL DEFAULT now(), atualizado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT ck_agenda_participante_identidade CHECK (usuario_id IS NOT NULL OR medico_id IS NOT NULL),
 CONSTRAINT uq_agenda_participante UNIQUE NULLS NOT DISTINCT (tenant_id,evento_id,usuario_id,medico_id)
);
CREATE INDEX IF NOT EXISTS idx_agenda_participantes_evento ON agenda_evento_participantes(tenant_id,evento_id,status);

CREATE TABLE IF NOT EXISTS agenda_evento_conflitos (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, evento_id uuid NOT NULL,
 evento_conflitante_id uuid NULL, tipo varchar(40) NOT NULL, severidade varchar(20) NOT NULL DEFAULT 'ALTA',
 descricao varchar(1000) NOT NULL, status varchar(20) NOT NULL DEFAULT 'ABERTO',
 resolucao varchar(1000) NULL, resolvido_por uuid NULL, resolvido_em timestamptz NULL,
 criado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT ck_agenda_conflito_status CHECK (status IN ('ABERTO','IGNORADO','RESOLVIDO'))
);
CREATE INDEX IF NOT EXISTS idx_agenda_conflitos_abertos ON agenda_evento_conflitos(tenant_id,evento_id,severidade) WHERE status = 'ABERTO';

CREATE TABLE IF NOT EXISTS medico_checkins (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, medico_id uuid NOT NULL, escala_id uuid NOT NULL,
 checkin_em timestamptz NOT NULL DEFAULT now(), checkout_em timestamptz NULL,
 checkin_latitude numeric(9,6) NULL, checkin_longitude numeric(9,6) NULL,
 checkout_latitude numeric(9,6) NULL, checkout_longitude numeric(9,6) NULL,
 origem varchar(20) NOT NULL DEFAULT 'MOBILE', dispositivo jsonb NOT NULL DEFAULT '{}'::jsonb,
 criado_em timestamptz NOT NULL DEFAULT now(), atualizado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT uq_medico_checkin_escala UNIQUE (tenant_id,escala_id),
 CONSTRAINT ck_medico_checkout_ordem CHECK (checkout_em IS NULL OR checkout_em >= checkin_em)
);
CREATE INDEX IF NOT EXISTS idx_medico_checkins_medico ON medico_checkins(tenant_id,medico_id,checkin_em DESC);

CREATE TABLE IF NOT EXISTS medico_disponibilidade_regras (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, medico_id uuid NOT NULL,
 tipo varchar(20) NOT NULL, dia_semana smallint NULL, inicio_hora time NULL, fim_hora time NULL,
 inicio_em timestamptz NULL, fim_em timestamptz NULL, especialidade_id uuid NULL, unidade_id uuid NULL,
 ativa boolean NOT NULL DEFAULT true, observacao varchar(500) NULL, criado_em timestamptz NOT NULL DEFAULT now(),
 atualizado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT ck_disponibilidade_tipo CHECK (tipo IN ('DISPONIVEL','INDISPONIVEL')),
 CONSTRAINT ck_disponibilidade_dia CHECK (dia_semana IS NULL OR dia_semana BETWEEN 0 AND 6),
 CONSTRAINT ck_disponibilidade_periodo CHECK (fim_em IS NULL OR inicio_em IS NULL OR fim_em > inicio_em)
);
CREATE INDEX IF NOT EXISTS idx_disponibilidade_regras_medico ON medico_disponibilidade_regras(tenant_id,medico_id,ativa);

CREATE TABLE IF NOT EXISTS onboarding_etapas_execucao (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, etapa varchar(60) NOT NULL,
 status varchar(20) NOT NULL DEFAULT 'PENDENTE', progresso smallint NOT NULL DEFAULT 0,
 dados_rascunho jsonb NOT NULL DEFAULT '{}'::jsonb, pendencias jsonb NOT NULL DEFAULT '[]'::jsonb,
 iniciado_por uuid NULL, concluido_por uuid NULL, iniciado_em timestamptz NULL, concluido_em timestamptz NULL,
 atualizado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT uq_onboarding_execucao_etapa UNIQUE (tenant_id,etapa),
 CONSTRAINT ck_onboarding_execucao_progresso CHECK (progresso BETWEEN 0 AND 100),
 CONSTRAINT ck_onboarding_execucao_status CHECK (status IN ('PENDENTE','EM_ANDAMENTO','CONCLUIDA','BLOQUEADA'))
);

CREATE TABLE IF NOT EXISTS relatorio_modelos (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, codigo varchar(60) NOT NULL,
 nome varchar(140) NOT NULL, categoria varchar(40) NOT NULL, descricao varchar(500) NULL,
 definicao jsonb NOT NULL DEFAULT '{}'::jsonb, formatos varchar(10)[] NOT NULL DEFAULT ARRAY['CSV']::varchar[],
 ativo boolean NOT NULL DEFAULT true, criado_por uuid NULL, criado_em timestamptz NOT NULL DEFAULT now(), atualizado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT uq_relatorio_modelo_codigo UNIQUE NULLS NOT DISTINCT (tenant_id,codigo)
);
CREATE TABLE IF NOT EXISTS relatorio_execucoes (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, modelo_id uuid NOT NULL,
 solicitado_por uuid NOT NULL, formato varchar(10) NOT NULL, filtros jsonb NOT NULL DEFAULT '{}'::jsonb,
 status varchar(20) NOT NULL DEFAULT 'SOLICITADA', progresso smallint NOT NULL DEFAULT 0,
 arquivo_chave varchar(500) NULL, erro text NULL, solicitado_em timestamptz NOT NULL DEFAULT now(),
 iniciado_em timestamptz NULL, concluido_em timestamptz NULL, expira_em timestamptz NULL,
 CONSTRAINT ck_relatorio_execucao_formato CHECK (formato IN ('CSV','XLSX','PDF')),
 CONSTRAINT ck_relatorio_execucao_progresso CHECK (progresso BETWEEN 0 AND 100)
);
CREATE INDEX IF NOT EXISTS idx_relatorio_execucoes_historico ON relatorio_execucoes(tenant_id,solicitado_por,solicitado_em DESC);

CREATE TABLE IF NOT EXISTS superadmin_cliente_riscos (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, tipo varchar(40) NOT NULL,
 severidade varchar(20) NOT NULL, score numeric(5,2) NOT NULL, evidencias jsonb NOT NULL DEFAULT '[]'::jsonb,
 status varchar(20) NOT NULL DEFAULT 'ABERTO', responsavel_id uuid NULL, proxima_acao varchar(500) NULL,
 detectado_em timestamptz NOT NULL DEFAULT now(), resolvido_em timestamptz NULL, atualizado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT ck_cliente_risco_score CHECK (score BETWEEN 0 AND 100)
);
CREATE INDEX IF NOT EXISTS idx_cliente_riscos_abertos ON superadmin_cliente_riscos(tenant_id,severidade,score DESC) WHERE status = 'ABERTO';

CREATE TABLE IF NOT EXISTS white_label_temas (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, nome varchar(100) NOT NULL,
 logo_url varchar(500) NULL, cor_primaria varchar(9) NOT NULL, cor_secundaria varchar(9) NOT NULL,
 cor_fundo varchar(9) NOT NULL, cor_texto varchar(9) NOT NULL, contraste_minimo numeric(4,2) NOT NULL,
 contraste_aa boolean NOT NULL, tokens jsonb NOT NULL DEFAULT '{}'::jsonb, ativo boolean NOT NULL DEFAULT false,
 criado_por uuid NOT NULL, criado_em timestamptz NOT NULL DEFAULT now(), atualizado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT ck_white_label_tema_contraste CHECK (contraste_minimo BETWEEN 1 AND 21),
 CONSTRAINT ck_white_label_tema_ativo_legivel CHECK (NOT ativo OR contraste_aa)
);
CREATE UNIQUE INDEX IF NOT EXISTS uq_white_label_tema_ativo ON white_label_temas(tenant_id) WHERE ativo;
CREATE TABLE IF NOT EXISTS white_label_historico (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, tema_id uuid NOT NULL,
 acao varchar(30) NOT NULL, antes jsonb NULL, depois jsonb NOT NULL, alterado_por uuid NOT NULL,
 alterado_em timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_white_label_historico ON white_label_historico(tenant_id,alterado_em DESC);

CREATE TABLE IF NOT EXISTS ajuda_contextual_topicos (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, rota varchar(180) NOT NULL,
 perfil varchar(50) NULL, titulo varchar(140) NOT NULL, resumo varchar(500) NOT NULL,
 acao_texto varchar(80) NULL, acao_url varchar(500) NULL, prioridade smallint NOT NULL DEFAULT 0,
 ativo boolean NOT NULL DEFAULT true, criado_em timestamptz NOT NULL DEFAULT now(), atualizado_em timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_ajuda_contextual_rota ON ajuda_contextual_topicos(rota,perfil,ativo,prioridade DESC);

CREATE TABLE IF NOT EXISTS operacao_assistida_runbooks (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, codigo varchar(60) NOT NULL,
 titulo varchar(140) NOT NULL, modulo varchar(40) NOT NULL, gatilho jsonb NOT NULL DEFAULT '{}'::jsonb,
 passos jsonb NOT NULL DEFAULT '[]'::jsonb, versao integer NOT NULL DEFAULT 1, ativo boolean NOT NULL DEFAULT true,
 criado_em timestamptz NOT NULL DEFAULT now(), atualizado_em timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT uq_runbook_codigo_versao UNIQUE NULLS NOT DISTINCT (tenant_id,codigo,versao)
);

CREATE TABLE IF NOT EXISTS notificacao_agrupamentos_v145 (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, usuario_id uuid NOT NULL,
 chave varchar(160) NOT NULL, categoria varchar(40) NOT NULL, severidade varchar(20) NOT NULL DEFAULT 'INFORMATIVA',
 titulo varchar(160) NOT NULL, quantidade integer NOT NULL DEFAULT 1, ultima_notificacao_em timestamptz NOT NULL DEFAULT now(),
 lido_em timestamptz NULL, expira_em timestamptz NULL, acao_url varchar(500) NULL,
 CONSTRAINT uq_notificacao_agrupamento UNIQUE (tenant_id,usuario_id,chave),
 CONSTRAINT ck_notificacao_quantidade CHECK (quantidade > 0)
);
CREATE INDEX IF NOT EXISTS idx_notificacao_agrupamentos_caixa ON notificacao_agrupamentos_v145(tenant_id,usuario_id,lido_em,ultima_notificacao_em DESC);

CREATE TABLE IF NOT EXISTS user_saved_dashboards (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, usuario_id uuid NOT NULL,
 nome varchar(120) NOT NULL, perfil varchar(50) NOT NULL, configuracao jsonb NOT NULL DEFAULT '{}'::jsonb,
 padrao boolean NOT NULL DEFAULT false, criado_em timestamptz NOT NULL DEFAULT now(), atualizado_em timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX IF NOT EXISTS uq_saved_dashboard_padrao ON user_saved_dashboards(tenant_id,usuario_id,perfil) WHERE padrao;
CREATE INDEX IF NOT EXISTS idx_saved_dashboards_usuario ON user_saved_dashboards(tenant_id,usuario_id,perfil);

-- ============================================================
-- Seção 38 — Pagamentos de plantões v1.95.1
-- ============================================================

-- SOURCE: database/schema/305_v1951_pagamentos_plantoes.sql
-- SOURCE-SHA256: fcfb9d72d6f208a95433e9357a772aadcd2aa802dd5c1cb1bb69395aaa773da0
-- PlantãoPro v1.95.1 — contrato canônico de pagamentos de plantões.
-- Mantém este domínio separado de pagamentos_medicos e pagamentos_saas.
set search_path to plantaopro, public;

create table if not exists plantaopro.pagamentos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    escala_id uuid not null,
    medico_id uuid not null,
    plantao_id uuid not null,
    valor_previsto numeric(14,2) not null default 0,
    valor_pago numeric(14,2),
    valor_hora numeric(14,2) not null default 0,
    horas_referencia numeric(8,2) not null default 0,
    status varchar(24) not null default 'pendente',
    data_prevista date,
    data_vencimento date,
    data_pagamento date,
    forma_pagamento varchar(50),
    chave_pix varchar(180),
    observacoes text,
    processado_automaticamente boolean not null default false,
    created_by uuid,
    updated_by uuid,
    reg_date timestamptz not null default now(),
    reg_update timestamptz,
    reg_status char(1) not null default 'A',
    constraint ck_pagamentos_valores check (
        valor_previsto >= 0 and coalesce(valor_pago, 0) >= 0 and
        valor_hora >= 0 and horas_referencia >= 0),
    constraint ck_pagamentos_reg_status check (reg_status in ('A','I'))
);

create unique index if not exists ux_pagamentos_escala_ativo
    on plantaopro.pagamentos(escala_id) where reg_status = 'A';
create index if not exists ix_pagamentos_tenant_status
    on plantaopro.pagamentos(tenant_id, status, data_prevista) where reg_status = 'A';
create index if not exists ix_pagamentos_medico
    on plantaopro.pagamentos(medico_id, reg_date desc) where reg_status = 'A';

create table if not exists plantaopro.historico_pagamento (
    id uuid primary key default gen_random_uuid(),
    pagamento_id uuid not null references plantaopro.pagamentos(id),
    status_anterior varchar(24),
    status_novo varchar(24) not null,
    justificativa text,
    usuario_id uuid,
    reg_date timestamptz not null default now()
);
create index if not exists ix_historico_pagamento_timeline
    on plantaopro.historico_pagamento(pagamento_id, reg_date desc);

-- ============================================================
-- Seção 39 — plantaopro.fechamento_operacional_v187
-- ============================================================

-- SOURCE: database/schema/320_v187_fechamento_operacional_financeiro.sql
-- SOURCE-SHA256: 5f64684c9f494a9023d31df3334adba89652ef8138edaa0e6ba7a015525a2f13
-- PlantaoPro v1.87.0 - fechamento operacional e contestacao financeira reais.
-- Evolui as estruturas de v1.41 sem criar um dominio concorrente.
set search_path to plantaopro, public;

alter table plantaopro.fechamento_plantao
    add column if not exists cliente_id uuid,
    add column if not exists unidade_id uuid,
    add column if not exists hospital_id uuid,
    add column if not exists data_referencia date,
    add column if not exists valor_previsto numeric(14,2) not null default 0,
    add column if not exists valor_apurado numeric(14,2) not null default 0,
    add column if not exists horas_previstas numeric(8,2) not null default 0,
    add column if not exists horas_realizadas numeric(8,2) not null default 0,
    add column if not exists conferido_por uuid,
    add column if not exists conferido_em timestamptz,
    add column if not exists devolvido_por uuid,
    add column if not exists devolvido_em timestamptz,
    add column if not exists motivo_devolucao varchar(500),
    add column if not exists financeiro_gerado_por uuid,
    add column if not exists financeiro_gerado_em timestamptz,
    add column if not exists concluido_em timestamptz,
    add column if not exists atualizado_por uuid,
    add column if not exists atualizado_em timestamptz not null default now();

alter table plantaopro.fechamento_plantao drop constraint if exists fechamento_plantao_status_check;
alter table plantaopro.fechamento_plantao add constraint fechamento_plantao_status_check
 check (status in ('ABERTO','EM_CONFERENCIA','COM_DIVERGENCIA','AGUARDANDO_APROVACAO','APROVADO','DEVOLVIDO','FINANCEIRO_GERADO','CONCLUIDO','CANCELADO','FECHADO','REABERTO'));

alter table plantaopro.fechamento_plantao_escalas
    add column if not exists medico_id uuid,
    add column if not exists plantao_id uuid,
    add column if not exists status_escala varchar(24),
    add column if not exists inicio_previsto timestamptz,
    add column if not exists fim_previsto timestamptz,
    add column if not exists inicio_realizado timestamptz,
    add column if not exists fim_realizado timestamptz,
    add column if not exists possui_divergencia boolean not null default false,
    add column if not exists observacao varchar(500),
    add column if not exists criado_em timestamptz not null default now(),
    add column if not exists atualizado_em timestamptz not null default now();

alter table plantaopro.fechamento_divergencias
    add column if not exists fechamento_item_id uuid,
    add column if not exists valor_anterior numeric(14,2),
    add column if not exists valor_proposto numeric(14,2),
    add column if not exists motivo varchar(500);

create table if not exists plantaopro.fechamento_historico (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, cliente_id uuid not null,
    fechamento_id uuid not null references plantaopro.fechamento_plantao(id), evento varchar(50) not null,
    status_anterior varchar(24), status_novo varchar(24), descricao varchar(500), dados jsonb not null default '{}'::jsonb,
    executado_por uuid not null, executado_em timestamptz not null default now()
);

create table if not exists plantaopro.pagamento_contestacoes (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, cliente_id uuid not null,
    pagamento_id uuid not null references plantaopro.pagamentos(id), motivo varchar(500) not null,
    status varchar(20) not null default 'ABERTA', valor_original numeric(14,2) not null,
    valor_proposto numeric(14,2), aberto_por uuid not null, aberto_em timestamptz not null default now(),
    decisao varchar(30), justificativa_resolucao varchar(1000), valor_resolvido numeric(14,2),
    resolvido_por uuid, resolvido_em timestamptz, created_at timestamptz not null default now(), updated_at timestamptz not null default now(),
    constraint ck_pagamento_contestacao_status check (status in ('ABERTA','RESOLVIDA','CANCELADA')),
    constraint ck_pagamento_contestacao_decisao check (decisao is null or decisao in ('MANTER_VALOR','AJUSTAR_VALOR','CANCELAR_PAGAMENTO'))
);

create unique index if not exists ux_pagamento_contestacao_aberta on plantaopro.pagamento_contestacoes(tenant_id,pagamento_id) where status='ABERTA';
create index if not exists ix_fechamento_status on plantaopro.fechamento_plantao(tenant_id,status,iniciado_em desc);
create index if not exists ix_fechamento_historico_timeline on plantaopro.fechamento_historico(tenant_id,fechamento_id,executado_em desc);
create index if not exists ix_fechamento_divergencias_abertas on plantaopro.fechamento_divergencias(tenant_id,fechamento_id,status);
create index if not exists ix_contestacoes_status on plantaopro.pagamento_contestacoes(tenant_id,status,aberto_em desc);

-- ============================================================
-- Seção 40 — plantaopro.prontuario_longitudinal_v188
-- ============================================================

-- SOURCE: database/schema/330_v188_prontuario_longitudinal.sql
-- SOURCE-SHA256: a6d32c3f466bca2dd209dc9b91163700755da2e721aa57079c8e86d223de9e66
-- PlantaoPro v1.88.0 - camada clinica longitudinal, tenant-safe e auditavel.
CREATE SCHEMA IF NOT EXISTS plantaopro;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS plantaopro.paciente_problemas(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, cliente_id uuid NOT NULL, paciente_id uuid NOT NULL, cid_id uuid,
 descricao varchar(500) NOT NULL, status varchar(20) NOT NULL DEFAULT 'ATIVO', data_inicio date NOT NULL DEFAULT current_date, data_resolucao date,
 observacao text, origem_consulta_id uuid, versao integer NOT NULL DEFAULT 1, criado_por uuid, criado_em timestamptz NOT NULL DEFAULT now(),
 atualizado_por uuid, atualizado_em timestamptz, reg_status char(1) NOT NULL DEFAULT 'A',
 CONSTRAINT ck_paciente_problemas_status CHECK(status IN ('ATIVO','RESOLVIDO','INATIVO')));
CREATE INDEX IF NOT EXISTS ix_paciente_problemas_tenant_paciente ON plantaopro.paciente_problemas(tenant_id,paciente_id,status) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_paciente_problemas_consulta ON plantaopro.paciente_problemas(tenant_id,origem_consulta_id) WHERE origem_consulta_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS plantaopro.paciente_alergias(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),tenant_id uuid NOT NULL,paciente_id uuid NOT NULL,tipo varchar(20) NOT NULL,substancia varchar(250) NOT NULL,
 descricao text,gravidade varchar(20) NOT NULL DEFAULT 'NAO_INFORMADA',reacao text,status varchar(20) NOT NULL DEFAULT 'ATIVA',confirmada boolean NOT NULL DEFAULT false,
 origem_consulta_id uuid,registrado_por uuid,registrado_em timestamptz NOT NULL DEFAULT now(),atualizado_por uuid,atualizado_em timestamptz,
 versao integer NOT NULL DEFAULT 1,reg_status char(1) NOT NULL DEFAULT 'A',
 CONSTRAINT ck_paciente_alergias_tipo CHECK(tipo IN ('MEDICAMENTO','ALIMENTO','SUBSTANCIA','OUTRA')),
 CONSTRAINT ck_paciente_alergias_gravidade CHECK(gravidade IN ('LEVE','MODERADA','GRAVE','NAO_INFORMADA')),
 CONSTRAINT ck_paciente_alergias_status CHECK(status IN ('ATIVA','INATIVA')));
CREATE INDEX IF NOT EXISTS ix_paciente_alergias_tenant_paciente ON plantaopro.paciente_alergias(tenant_id,paciente_id,status) WHERE reg_status='A';

CREATE TABLE IF NOT EXISTS plantaopro.paciente_medicamentos_uso(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),tenant_id uuid NOT NULL,paciente_id uuid NOT NULL,medicamento_id uuid,medicamento_descricao varchar(300) NOT NULL,
 dose varchar(100),frequencia varchar(150),via varchar(80),inicio_em date,fim_em date,status varchar(20) NOT NULL DEFAULT 'EM_USO',origem varchar(50) NOT NULL,
 consulta_id uuid,prescricao_id uuid,observacao text,versao integer NOT NULL DEFAULT 1,created_by uuid,created_at timestamptz NOT NULL DEFAULT now(),
 updated_by uuid,updated_at timestamptz,reg_status char(1) NOT NULL DEFAULT 'A',CONSTRAINT ck_medicamentos_uso_status CHECK(status IN ('EM_USO','SUSPENSO','FINALIZADO')));
CREATE INDEX IF NOT EXISTS ix_medicamentos_uso_tenant_paciente ON plantaopro.paciente_medicamentos_uso(tenant_id,paciente_id,status) WHERE reg_status='A';

CREATE TABLE IF NOT EXISTS plantaopro.solicitacoes_exames(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),tenant_id uuid NOT NULL,paciente_id uuid NOT NULL,consulta_id uuid,medico_id uuid,unidade_id uuid,
 status varchar(30) NOT NULL DEFAULT 'SOLICITADO',prioridade varchar(20) NOT NULL DEFAULT 'ROTINA',indicacao_clinica text NOT NULL,observacoes text,
 solicitado_em timestamptz NOT NULL DEFAULT now(),realizado_em timestamptz,cancelado_em timestamptz,created_by uuid,created_at timestamptz NOT NULL DEFAULT now(),updated_by uuid,updated_at timestamptz,
 CONSTRAINT ck_solicitacoes_exames_status CHECK(status IN ('SOLICITADO','AUTORIZACAO_PENDENTE','AUTORIZADO','AGENDADO','REALIZADO','RESULTADO_DISPONIVEL','CANCELADO')));
CREATE INDEX IF NOT EXISTS ix_solicitacoes_exames_tenant_paciente ON plantaopro.solicitacoes_exames(tenant_id,paciente_id,status,solicitado_em DESC);
CREATE INDEX IF NOT EXISTS ix_solicitacoes_exames_consulta ON plantaopro.solicitacoes_exames(tenant_id,consulta_id) WHERE consulta_id IS NOT NULL;
CREATE TABLE IF NOT EXISTS plantaopro.solicitacao_exame_itens(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),solicitacao_id uuid NOT NULL,codigo varchar(80),nome varchar(250) NOT NULL,tipo varchar(80) NOT NULL,observacao text,status varchar(30) NOT NULL DEFAULT 'SOLICITADO',created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_solicitacao_exame_itens_solicitacao ON plantaopro.solicitacao_exame_itens(solicitacao_id,status);

CREATE TABLE IF NOT EXISTS plantaopro.resultados_exames(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),tenant_id uuid NOT NULL,solicitacao_id uuid NOT NULL,item_id uuid,paciente_id uuid NOT NULL,tipo varchar(80) NOT NULL,
 resumo varchar(500) NOT NULL,resultado_textual text NOT NULL,realizado_em timestamptz NOT NULL,liberado_em timestamptz,profissional_responsavel varchar(250),
 documento_id uuid,created_by uuid,created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_resultados_exames_tenant_paciente ON plantaopro.resultados_exames(tenant_id,paciente_id,created_at DESC);
CREATE INDEX IF NOT EXISTS ix_resultados_exames_solicitacao ON plantaopro.resultados_exames(tenant_id,solicitacao_id);

CREATE TABLE IF NOT EXISTS plantaopro.encaminhamentos_clinicos(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),tenant_id uuid NOT NULL,paciente_id uuid NOT NULL,consulta_id uuid NOT NULL,medico_origem_id uuid,
 especialidade_destino_id uuid,profissional_destino_id uuid,unidade_destino_id uuid,motivo text NOT NULL,resumo_clinico text NOT NULL,prioridade varchar(20) NOT NULL DEFAULT 'ROTINA',
 status varchar(30) NOT NULL DEFAULT 'CRIADO',criado_em timestamptz NOT NULL DEFAULT now(),agendado_em timestamptz,concluido_em timestamptz,created_by uuid,updated_by uuid,updated_at timestamptz,
 CONSTRAINT ck_encaminhamentos_status CHECK(status IN ('CRIADO','AGUARDANDO_AGENDAMENTO','AGENDADO','CONCLUIDO','CANCELADO')));
CREATE INDEX IF NOT EXISTS ix_encaminhamentos_tenant_paciente ON plantaopro.encaminhamentos_clinicos(tenant_id,paciente_id,status,criado_em DESC);
CREATE INDEX IF NOT EXISTS ix_encaminhamentos_consulta ON plantaopro.encaminhamentos_clinicos(tenant_id,consulta_id);

CREATE TABLE IF NOT EXISTS plantaopro.documentos_clinicos(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),tenant_id uuid NOT NULL,paciente_id uuid NOT NULL,consulta_id uuid,tipo varchar(40) NOT NULL,titulo varchar(250) NOT NULL,
 conteudo text NOT NULL,status varchar(20) NOT NULL DEFAULT 'RASCUNHO',versao integer NOT NULL DEFAULT 1,emitido_por uuid,emitido_em timestamptz,
 cancelado_por uuid,cancelado_em timestamptz,motivo_cancelamento text,hash_documento varchar(64),assinatura_status varchar(20) NOT NULL DEFAULT 'NAO_ASSINADO',
 cid_exibido boolean NOT NULL DEFAULT false,quantidade_dias integer,inicio_afastamento date,created_by uuid,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz,
 CONSTRAINT ck_documentos_tipo CHECK(tipo IN ('ATESTADO','DECLARACAO','ENCAMINHAMENTO','RESUMO_ATENDIMENTO','RELATORIO_CLINICO')),
 CONSTRAINT ck_documentos_assinatura CHECK(assinatura_status IN ('NAO_ASSINADO','PENDENTE','ASSINADO','FALHOU','CANCELADO')));
CREATE INDEX IF NOT EXISTS ix_documentos_clinicos_tenant_paciente ON plantaopro.documentos_clinicos(tenant_id,paciente_id,status,created_at DESC);
CREATE INDEX IF NOT EXISTS ix_documentos_clinicos_consulta ON plantaopro.documentos_clinicos(tenant_id,consulta_id) WHERE consulta_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS plantaopro.anexos_clinicos(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),tenant_id uuid NOT NULL,paciente_id uuid NOT NULL,entidade_tipo varchar(30) NOT NULL,entidade_id uuid NOT NULL,
 nome_original varchar(255) NOT NULL,nome_armazenado varchar(100) NOT NULL,mime_type varchar(100) NOT NULL,tamanho bigint NOT NULL,hash varchar(64) NOT NULL,
 storage_provider varchar(30) NOT NULL,storage_key varchar(500) NOT NULL,created_by uuid,created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_anexos_clinicos_entidade ON plantaopro.anexos_clinicos(tenant_id,entidade_tipo,entidade_id);
CREATE INDEX IF NOT EXISTS ix_anexos_clinicos_paciente ON plantaopro.anexos_clinicos(tenant_id,paciente_id,created_at DESC);

-- ============================================================
-- Seção 41 — plantaopro.clinical_hardening_v189
-- ============================================================

-- SOURCE: database/schema/340_v189_clinical_operational_hardening.sql
-- SOURCE-SHA256: abfa8ca830797ca5589a55292307a021d4741179c0baa552521fd390642941a5
-- PlantaoPro v1.89.0 - hardening operacional clínico, incremental e idempotente.
BEGIN;
ALTER TABLE plantaopro.solicitacoes_exames DROP CONSTRAINT IF EXISTS ck_solicitacoes_exames_status;
ALTER TABLE plantaopro.solicitacoes_exames ADD CONSTRAINT ck_solicitacoes_exames_status CHECK(status IN ('SOLICITADO','AUTORIZACAO_PENDENTE','AUTORIZADO','NEGADO','AGENDADO','REALIZADO','PARCIALMENTE_RESULTADO','RESULTADO_DISPONIVEL','CANCELADO'));
ALTER TABLE plantaopro.solicitacao_exame_itens DROP CONSTRAINT IF EXISTS ck_solicitacao_exame_itens_status;
ALTER TABLE plantaopro.solicitacao_exame_itens ADD CONSTRAINT ck_solicitacao_exame_itens_status CHECK(status IN ('SOLICITADO','REALIZADO','RESULTADO_DISPONIVEL','CANCELADO'));
ALTER TABLE plantaopro.anexos_clinicos ADD COLUMN IF NOT EXISTS removed_at timestamptz;
ALTER TABLE plantaopro.anexos_clinicos ADD COLUMN IF NOT EXISTS removed_by uuid;
ALTER TABLE plantaopro.anexos_clinicos ADD COLUMN IF NOT EXISTS removal_reason varchar(500);
ALTER TABLE plantaopro.anexos_clinicos ADD COLUMN IF NOT EXISTS reg_status char(1) NOT NULL DEFAULT 'A';
ALTER TABLE plantaopro.anexos_clinicos DROP CONSTRAINT IF EXISTS ck_anexos_clinicos_reg_status;
ALTER TABLE plantaopro.anexos_clinicos ADD CONSTRAINT ck_anexos_clinicos_reg_status CHECK(reg_status IN ('A','I'));
DO $$ BEGIN IF NOT EXISTS(SELECT 1 FROM plantaopro.resultados_exames WHERE item_id IS NOT NULL GROUP BY tenant_id,solicitacao_id,item_id HAVING count(*)>1) THEN CREATE UNIQUE INDEX IF NOT EXISTS ux_resultados_exames_item ON plantaopro.resultados_exames(tenant_id,solicitacao_id,item_id) WHERE item_id IS NOT NULL; END IF; END $$;
DO $$ BEGIN IF NOT EXISTS(SELECT 1 FROM plantaopro.anexos_clinicos WHERE reg_status='A' GROUP BY tenant_id,paciente_id,entidade_tipo,entidade_id,hash HAVING count(*)>1) THEN CREATE UNIQUE INDEX IF NOT EXISTS ux_anexos_clinicos_hash_ativo ON plantaopro.anexos_clinicos(tenant_id,paciente_id,entidade_tipo,entidade_id,hash) WHERE reg_status='A'; END IF; END $$;
CREATE INDEX IF NOT EXISTS ix_timeline_exames ON plantaopro.solicitacoes_exames(tenant_id,paciente_id,solicitado_em DESC);
CREATE INDEX IF NOT EXISTS ix_timeline_documentos ON plantaopro.documentos_clinicos(tenant_id,paciente_id,created_at DESC);
DO $$ BEGIN
 IF NOT EXISTS(SELECT 1 FROM pg_constraint WHERE conname='fk_exame_itens_solicitacao') THEN ALTER TABLE plantaopro.solicitacao_exame_itens ADD CONSTRAINT fk_exame_itens_solicitacao FOREIGN KEY(solicitacao_id) REFERENCES plantaopro.solicitacoes_exames(id) ON DELETE RESTRICT NOT VALID; END IF;
 IF NOT EXISTS(SELECT 1 FROM pg_constraint WHERE conname='fk_resultados_solicitacao') THEN ALTER TABLE plantaopro.resultados_exames ADD CONSTRAINT fk_resultados_solicitacao FOREIGN KEY(solicitacao_id) REFERENCES plantaopro.solicitacoes_exames(id) ON DELETE RESTRICT NOT VALID; END IF;
 IF NOT EXISTS(SELECT 1 FROM pg_constraint WHERE conname='fk_resultados_item') THEN ALTER TABLE plantaopro.resultados_exames ADD CONSTRAINT fk_resultados_item FOREIGN KEY(item_id) REFERENCES plantaopro.solicitacao_exame_itens(id) ON DELETE RESTRICT NOT VALID; END IF;
END $$;
COMMIT;

-- ============================================================
-- Seção 42 — plantaopro.saved_views_v192
-- ============================================================

-- SOURCE: database/migrations/2026_v192_saved_views.sql
-- SOURCE-SHA256: df55fc3e7c192bc4d6d7d6c8064d38638980e782215fea5a33e5c2ace2bef7b6
-- Canonical Saved Views contract. This migration also upgrades the Portuguese
-- v1.31/v1.42 contract in place; IDs and rows are never recreated or deleted.
create table if not exists plantaopro.saved_views (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null,
    user_id uuid, module varchar(50), name varchar(100), normalized_name varchar(100),
    filters_json jsonb, sort_json jsonb, is_default boolean,
    created_at timestamptz, updated_at timestamptz
);

do $saved_views_upgrade$
declare has_old_config boolean; has_old_sector boolean; has_old_shared boolean;
begin
    select exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='configuracao') into has_old_config;
    select exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='setor_id') into has_old_sector;
    select exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='compartilhada') into has_old_shared;

    -- Rename when only the legacy spelling exists. Mixed/partially upgraded
    -- databases are handled by the backfills below.
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='usuario_id') and not exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='user_id') then alter table plantaopro.saved_views rename column usuario_id to user_id; end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='modulo') and not exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='module') then alter table plantaopro.saved_views rename column modulo to module; end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='nome') and not exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='name') then alter table plantaopro.saved_views rename column nome to name; end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='padrao') and not exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='is_default') then alter table plantaopro.saved_views rename column padrao to is_default; end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='criado_em') and not exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='created_at') then alter table plantaopro.saved_views rename column criado_em to created_at; end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='atualizado_em') and not exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='updated_at') then alter table plantaopro.saved_views rename column atualizado_em to updated_at; end if;

    alter table plantaopro.saved_views add column if not exists user_id uuid, add column if not exists module varchar(50), add column if not exists name varchar(100), add column if not exists normalized_name varchar(100), add column if not exists filters_json jsonb, add column if not exists sort_json jsonb, add column if not exists is_default boolean, add column if not exists created_at timestamptz, add column if not exists updated_at timestamptz;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='usuario_id') then execute 'update plantaopro.saved_views set user_id=coalesce(user_id,usuario_id)'; end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='modulo') then execute 'update plantaopro.saved_views set module=coalesce(module,modulo)'; end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='nome') then execute 'update plantaopro.saved_views set name=coalesce(name,nome)'; end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='padrao') then execute 'update plantaopro.saved_views set is_default=coalesce(is_default,padrao)'; end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='criado_em') then execute 'update plantaopro.saved_views set created_at=coalesce(created_at,criado_em)'; end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='atualizado_em') then execute 'update plantaopro.saved_views set updated_at=coalesce(updated_at,atualizado_em)'; end if;
    if has_old_config then execute 'update plantaopro.saved_views set filters_json=coalesce(filters_json,configuracao)'; end if;
    update plantaopro.saved_views set filters_json=coalesce(filters_json,'{}'::jsonb), is_default=coalesce(is_default,false), created_at=coalesce(created_at,now()), updated_at=coalesce(updated_at,created_at,now()), normalized_name=coalesce(nullif(normalized_name,''),lower(regexp_replace(btrim(name),'\s+',' ','g')));
    -- Preserve legacy-only sharing/sector state inside the JSON contract before
    -- removing competing presentation columns.
    if has_old_sector then execute 'update plantaopro.saved_views set filters_json=filters_json || jsonb_build_object(''_legacy_setor_id'',setor_id) where setor_id is not null'; end if;
    if has_old_shared then execute 'update plantaopro.saved_views set filters_json=filters_json || jsonb_build_object(''_legacy_compartilhada'',compartilhada) where compartilhada'; end if;
end $saved_views_upgrade$;

alter table plantaopro.saved_views alter column user_id set not null, alter column module set not null, alter column name set not null, alter column normalized_name set not null, alter column filters_json set default '{}'::jsonb, alter column filters_json set not null, alter column is_default set default false, alter column is_default set not null, alter column created_at set default now(), alter column created_at set not null, alter column updated_at set default now(), alter column updated_at set not null;
create index if not exists ix_saved_views_tenant_user on plantaopro.saved_views(tenant_id,user_id);
create index if not exists ix_saved_views_tenant_module on plantaopro.saved_views(tenant_id,module);
create unique index if not exists ux_saved_views_name on plantaopro.saved_views(tenant_id,user_id,module,normalized_name);
create unique index if not exists ux_saved_views_default on plantaopro.saved_views(tenant_id,user_id,module) where is_default;

create table if not exists plantaopro.productivity_item_user_state (id uuid primary key default gen_random_uuid(), tenant_id uuid not null, user_id uuid not null, item_key varchar(300) not null, snoozed_until timestamptz null, dismissed_at timestamptz null, last_seen_at timestamptz null, created_at timestamptz not null default now(), updated_at timestamptz not null default now(), constraint uq_productivity_user_item unique (tenant_id,user_id,item_key));
create index if not exists ix_productivity_user_state_scope on plantaopro.productivity_item_user_state(tenant_id,user_id);
create index if not exists ix_productivity_user_state_snooze on plantaopro.productivity_item_user_state(snoozed_until) where snoozed_until is not null;

do $$ begin
 if not exists(select 1 from pg_constraint where conname='fk_saved_views_tenant') then alter table plantaopro.saved_views add constraint fk_saved_views_tenant foreign key(tenant_id) references plantaopro.clientes(id) on delete cascade; end if;
 if not exists(select 1 from pg_constraint where conname='fk_saved_views_user') then alter table plantaopro.saved_views add constraint fk_saved_views_user foreign key(user_id) references plantaopro.usuarios(id) on delete cascade; end if;
 if not exists(select 1 from pg_constraint where conname='fk_productivity_state_tenant') then alter table plantaopro.productivity_item_user_state add constraint fk_productivity_state_tenant foreign key(tenant_id) references plantaopro.clientes(id) on delete cascade; end if;
 if not exists(select 1 from pg_constraint where conname='fk_productivity_state_user') then alter table plantaopro.productivity_item_user_state add constraint fk_productivity_state_user foreign key(user_id) references plantaopro.usuarios(id) on delete cascade; end if;
end $$;

-- ============================================================
-- Seção 43 — plantaopro.revenue_cycle_v195
-- ============================================================

-- SOURCE: database/migrations/2026_08_v195_financeiro_revenue_cycle.sql
-- SOURCE-SHA256: 2fc8ef99593ac024377f398873b0bdaefbffe9bb9a139812479f7de87aaecd72
-- PlantaoPro v1.95: evolução incremental do financeiro clínico; não altera pagamentos de plantão nem faturamento SaaS.
begin;

alter table plantaopro.v113_faturas
  add column if not exists origem_tipo varchar(40),
  add column if not exists regra_id uuid,
  add column if not exists regra_codigo varchar(80),
  add column if not exists valor_base_snapshot numeric(14,2) not null default 0,
  add column if not exists descontos numeric(14,2) not null default 0,
  add column if not exists acrescimos numeric(14,2) not null default 0,
  add column if not exists glosa_reconhecida numeric(14,2) not null default 0;

alter table plantaopro.v115_recebimentos
  add column if not exists tipo varchar(20) not null default 'RECEBIMENTO',
  add column if not exists recebimento_origem_id uuid,
  add column if not exists observacao text,
  add column if not exists recebido_em timestamptz not null default now();

do $$ begin
  alter table plantaopro.v115_recebimentos add constraint ck_v195_recebimento_positivo check (valor_recebido > 0);
exception when duplicate_object then null; end $$;
do $$ begin
  alter table plantaopro.v115_recebimentos add constraint ck_v195_recebimento_tipo check (tipo in ('RECEBIMENTO','ESTORNO'));
exception when duplicate_object then null; end $$;

create unique index if not exists ux_v195_conta_origem_ativa on plantaopro.v113_faturas(tenant_id,origem_tipo,pedido_id) where reg_status='A' and origem_tipo is not null and pedido_id is not null;
create index if not exists ix_v195_conta_tenant_status on plantaopro.v113_faturas(tenant_id,status) where reg_status='A';
create index if not exists ix_v195_recebimento_conta_tipo on plantaopro.v115_recebimentos(tenant_id,conta_receber_id,tipo) where reg_status='A';

alter table plantaopro.v115_regras_glosa add column if not exists valor_recuperado numeric(14,2) not null default 0;
do $$ begin alter table plantaopro.v115_regras_glosa add constraint ck_v195_glosa_positiva check(valor_glosado>0); exception when duplicate_object then null; end $$;
create index if not exists ix_v195_glosa_tenant_status_prazo on plantaopro.v115_regras_glosa(tenant_id,status,prazo_recurso) where reg_status='A';

alter table plantaopro.v115_regras_repasse
 add column if not exists regra_id uuid,
 add column if not exists evento_gerador varchar(40) not null default 'CONTA_EMITIDA';
do $$ begin alter table plantaopro.v115_regras_repasse add constraint ck_v195_repasse_percentual check(tipo_regra<>'PERCENTUAL' or percentual between 0 and 100); exception when duplicate_object then null; end $$;
create unique index if not exists ux_v195_repasse_lancamento on plantaopro.v115_regras_repasse(tenant_id,referencia_id,medico_id,regra_id) where reg_status='A' and referencia_id is not null and status not in ('CANCELADO','REGRA_ATIVA');
create index if not exists ix_v195_repasse_tenant_status on plantaopro.v115_regras_repasse(tenant_id,status) where reg_status='A';

create table if not exists plantaopro.v115_financeiro_historico(
 id uuid primary key default gen_random_uuid(), tenant_id uuid not null, entidade_tipo varchar(30) not null,
 entidade_id uuid not null, evento varchar(60) not null, valor_anterior numeric(14,2), valor_novo numeric(14,2),
 detalhes jsonb not null default '{}'::jsonb, created_at timestamptz not null default now(), created_by uuid
);
create index if not exists ix_v195_financeiro_historico_entidade on plantaopro.v115_financeiro_historico(tenant_id,entidade_tipo,entidade_id,created_at desc);
commit;

-- ============================================================
-- Seção 44 — plantaopro.notificacoes_v2070
-- ============================================================

-- SOURCE: database/schema/345_v2069_reparar_base_notificacoes.sql
-- SOURCE-SHA256: f0fdf1e80d44c76e160e87707631f2fda7420833d7a691d2e77f4e2c533be304
-- Compatibilidade para bases legadas que registraram v1.31.0 como baseline
-- sem possuir o agregado completo de notificacoes. As definicoes reproduzem
-- integralmente o contrato original antes de v2.07.0; nenhum dado existente e
-- removido ou reescrito.
create table if not exists plantaopro.notifications (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null,
    cliente_id uuid,
    unidade_id uuid,
    categoria varchar(20) not null check (categoria in ('OPERACAO','ESCALA','CLINICA','FINANCEIRO','SEGURANCA','SISTEMA')),
    titulo varchar(160) not null,
    descricao text not null,
    url text,
    criado_em timestamptz not null default now(),
    expira_em timestamptz,
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.notification_recipients (
    id uuid primary key default gen_random_uuid(),
    notification_id uuid not null references plantaopro.notifications(id) on delete cascade,
    usuario_id uuid not null,
    unique (notification_id, usuario_id)
);

create table if not exists plantaopro.notification_read_states (
    id uuid primary key default gen_random_uuid(),
    notification_id uuid not null references plantaopro.notifications(id) on delete cascade,
    usuario_id uuid not null,
    lida_em timestamptz not null default now(),
    unique (notification_id, usuario_id)
);

create table if not exists plantaopro.notification_preferences (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null,
    usuario_id uuid not null,
    categoria varchar(20) not null,
    in_app boolean not null default true,
    email boolean not null default false,
    push boolean not null default false,
    atualizado_em timestamptz not null default now(),
    unique (tenant_id, usuario_id, categoria)
);

-- SOURCE: database/schema/350_v2070_notificacoes_alertas_operacionais.sql
-- SOURCE-SHA256: e7b73da88edb97aa0a99c07edf360a8dd2177603d79642168a39e8766076bac2
-- PlantãoPro v2.07.0: central tenant-safe, rastreabilidade e outbox de notificações.
alter table plantaopro.notifications add column if not exists tipo_evento varchar(60) not null default 'SISTEMA';
alter table plantaopro.notifications add column if not exists prioridade varchar(16) not null default 'BAIXA';
alter table plantaopro.notifications add column if not exists origem_tipo varchar(60);
alter table plantaopro.notifications add column if not exists origem_id uuid;
alter table plantaopro.notifications add column if not exists usuario_id_dedupe uuid;

create unique index if not exists ux_notifications_alerta_idempotente
    on plantaopro.notifications(tenant_id, usuario_id_dedupe, origem_tipo, origem_id, tipo_evento)
    where origem_id is not null and usuario_id_dedupe is not null and reg_status='A';
create index if not exists ix_notifications_central
    on plantaopro.notifications(tenant_id, prioridade, criado_em desc) where reg_status='A';

alter table plantaopro.notification_preferences add column if not exists tipo_evento varchar(60) not null default 'GERAL';
alter table plantaopro.notification_preferences add column if not exists whatsapp boolean not null default false;
alter table plantaopro.notification_preferences add column if not exists ativo boolean not null default true;
alter table plantaopro.notification_preferences drop constraint if exists notification_preferences_tenant_id_usuario_id_categoria_key;
create unique index if not exists ux_notification_preferences_evento
    on plantaopro.notification_preferences(tenant_id, usuario_id, categoria, tipo_evento);

create table if not exists plantaopro.notification_actions (
    id uuid primary key default gen_random_uuid(), notification_id uuid not null references plantaopro.notifications(id) on delete cascade,
    tenant_id uuid not null, usuario_id uuid not null, status varchar(16) not null check(status in ('ARQUIVADA','RESOLVIDA')),
    criado_em timestamptz not null default now()
);
create index if not exists ix_notification_actions_user
    on plantaopro.notification_actions(tenant_id, usuario_id, notification_id, criado_em desc);

-- Preparação de domínio: nenhum provedor externo é presumido. O worker futuro processará apenas canais configurados.
create table if not exists plantaopro.notification_outbox (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, notification_id uuid not null references plantaopro.notifications(id) on delete cascade,
    canal varchar(16) not null check(canal in ('EMAIL','PUSH')), payload jsonb not null default '{}'::jsonb,
    status varchar(16) not null default 'PENDENTE', tentativas integer not null default 0,
    proxima_tentativa_em timestamptz, criado_em timestamptz not null default now(), processado_em timestamptz,
    unique(notification_id, canal)
);
create index if not exists ix_notification_outbox_pending
    on plantaopro.notification_outbox(tenant_id, status, proxima_tentativa_em) where status='PENDENTE';

-- ============================================================
-- Seção 45 — plantaopro.central_operacional_escalas_v2157
-- ============================================================

-- SOURCE: database/schema/360_v2157_central_operacional_escalas.sql
-- SOURCE-SHA256: f39bf4995f91ce8f4fd444ab217bf69271e2404e567dedee6f439f441057897d
-- PlantãoPro v2.15.7: invariantes transacionais do ciclo plantão -> escala -> pagamento.
-- Os índices parciais preservam o histórico e impedem efeitos duplicados apenas nos estados ativos.

alter table plantaopro.plantoes
    add column if not exists data_inicio timestamptz,
    add column if not exists data_fim timestamptz,
    add column if not exists valor numeric(12,2) default 0,
    add column if not exists vagas integer default 1,
    add column if not exists vagas_disponiveis integer default 1;

alter table plantaopro.plantoes
    drop constraint if exists ck_plantoes_vagas_consistentes;
alter table plantaopro.plantoes
    add constraint ck_plantoes_vagas_consistentes
    check (vagas > 0 and vagas_disponiveis between 0 and vagas and valor >= 0 and data_fim > data_inicio)
    not valid;

alter table plantaopro.plantao_convites
    add column if not exists plantao_id uuid,
    add column if not exists medico_id uuid,
    add column if not exists reg_status char(1) default 'A',
    add column if not exists expira_em timestamptz;

create unique index if not exists ux_plantao_convite_pendente
    on plantaopro.plantao_convites(plantao_id, medico_id)
    where reg_status='A' and upper(status) in ('ENVIADO', 'PENDENTE', 'PROCESSANDO');

alter table plantaopro.escalas
    add column if not exists plantao_id uuid,
    add column if not exists medico_id uuid,
    add column if not exists reg_status char(1) default 'A';

create unique index if not exists ux_escala_ocupacao_ativa
    on plantaopro.escalas(plantao_id, medico_id)
    where reg_status='A' and lower(status) in ('solicitado', 'solicitada', 'confirmado', 'confirmada', 'realizado', 'realizada');

create unique index if not exists ux_pagamento_origem_escala
    on plantaopro.pagamentos(escala_id)
    where reg_status='A';

-- A política de conflito é global por profissional: tenants distintos não podem reservar
-- o mesmo CRM no mesmo intervalo. A API retorna somente o bloqueio, nunca dados do outro tenant.

-- ============================================================
-- Seção 46 — plantaopro.financeiro_medico_v2158
-- ============================================================

-- SOURCE: database/schema/360_v2158_financeiro_medico_conferencia.sql
-- SOURCE-SHA256: d9df5b1c3e0113e1fdc7e172a7f46d449d2391eefde3ab819edafe2e2509b057
-- PlantãoPro v2.15.8 — integridade da obrigação e do pagamento médico.
-- Evolui o domínio canônico plantaopro.pagamentos; não se confunde com cobrança SaaS ou financeiro clínico.
set search_path to plantaopro, public;

alter table plantaopro.pagamentos
  add column if not exists valor_apurado numeric(14,2),
  add column if not exists valor_aprovado numeric(14,2),
  add column if not exists parametros_apuracao jsonb not null default '{}'::jsonb,
  add column if not exists versao bigint not null default 1,
  add column if not exists origem_pagamento varchar(20),
  add column if not exists referencia_pagamento varchar(120),
  add column if not exists aprovado_por uuid,
  add column if not exists aprovado_em timestamptz;

update plantaopro.pagamentos
set valor_apurado=coalesce(valor_apurado,valor_previsto),
    valor_aprovado=case when status='pago' then coalesce(valor_aprovado,valor_pago,valor_previsto) else valor_aprovado end,
    origem_pagamento=case when status='pago' then coalesce(origem_pagamento,'MANUAL') else origem_pagamento end
where valor_apurado is null or (status='pago' and valor_aprovado is null);

do $$ begin
 alter table plantaopro.pagamentos add constraint ck_v2158_pagamento_valores
 check (coalesce(valor_apurado,0)>=0 and coalesce(valor_aprovado,0)>=0 and coalesce(valor_pago,0)>=0
        and (status<>'pago' or (valor_pago=valor_aprovado and valor_pago>0)));
exception when duplicate_object then null; end $$;

do $$ begin
 alter table plantaopro.pagamentos add constraint ck_v2158_pagamento_origem
 check (origem_pagamento is null or origem_pagamento in ('MANUAL','IMPORTADO','INTEGRACAO_CONFIRMADA'));
exception when duplicate_object then null; end $$;

create unique index if not exists ux_v2158_pagamento_referencia_manual
 on plantaopro.pagamentos(tenant_id,cliente_id,referencia_pagamento)
 where referencia_pagamento is not null and status='pago' and reg_status='A';
create index if not exists ix_v2158_pagamento_conferencia
 on plantaopro.pagamentos(tenant_id,cliente_id,status,reg_date desc) where reg_status='A';

-- ============================================================
-- Seção 47 — plantaopro.saude360_v2159
-- ============================================================

-- SOURCE: database/schema/370_v2159_saude360_agenda_recepcao.sql
-- SOURCE-SHA256: cec52f65dc1477179d1e90453c2262e7d4a4b9d5fcc8e250fbafd922145b0211
-- v2.15.9 - invariantes concorrentes da jornada Saúde 360.
-- Incremental e idempotente: não altera migrations já aplicadas.
set search_path to plantaopro, public;

create extension if not exists btree_gist;

alter table plantaopro.pacientes
    add column if not exists cliente_id uuid,
    add column if not exists cpf text,
    add column if not exists reg_status char(1) not null default 'A',
    add column if not exists reg_date timestamptz not null default now();

alter table plantaopro.agendamentos
    add column if not exists cliente_id uuid,
    add column if not exists paciente_id uuid,
    add column if not exists medico_id uuid,
    add column if not exists unidade_id uuid,
    add column if not exists data_inicio timestamptz,
    add column if not exists data_fim timestamptz,
    add column if not exists reg_status char(1) not null default 'A',
    add column if not exists reg_date timestamptz not null default now();

create table if not exists plantaopro.agendamento_checkins (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    tenant_id uuid null,
    agendamento_id uuid not null,
    paciente_id uuid null,
    usuario_id uuid null,
    observacoes text null,
    status text not null default 'REALIZADO',
    created_by uuid null,
    updated_by uuid null,
    reg_update timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.painel_chamada_fila (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    tenant_id uuid null,
    painel_id uuid null,
    paciente_id uuid null,
    agendamento_id uuid null,
    triagem_id uuid null,
    atendimento_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    senha text null,
    paciente_nome text null,
    status text not null default 'AGUARDANDO',
    prioridade int not null default 0,
    chamada_em timestamptz null,
    created_by uuid null,
    updated_by uuid null,
    reg_update timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.triagem_fila (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    tenant_id uuid null,
    paciente_id uuid null,
    agendamento_id uuid null,
    atendimento_id uuid null,
    status text not null default 'AGUARDANDO',
    prioridade int not null default 0,
    created_by uuid null,
    updated_by uuid null,
    reg_update timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);

-- CPF é opcional, porém único por cliente quando informado e normalizado.
create unique index if not exists ux_v2159_paciente_cpf_cliente
    on plantaopro.pacientes (cliente_id, regexp_replace(cpf, '[^0-9]', '', 'g'))
    where reg_status = 'A' and cpf is not null and regexp_replace(cpf, '[^0-9]', '', 'g') <> '';

-- O banco, e não somente a validação prévia da API, arbitra reservas concorrentes.
do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'ck_v2159_agendamento_periodo' and conrelid = 'plantaopro.agendamentos'::regclass) then
        alter table plantaopro.agendamentos add constraint ck_v2159_agendamento_periodo check (data_fim > data_inicio) not valid;
    end if;
    if not exists (select 1 from pg_constraint where conname = 'ex_v2159_agendamento_medico' and conrelid = 'plantaopro.agendamentos'::regclass) then
        alter table plantaopro.agendamentos add constraint ex_v2159_agendamento_medico
            exclude using gist (cliente_id with =, medico_id with =, tstzrange(data_inicio, data_fim, '[)') with &&)
            where (reg_status = 'A' and status not in ('CANCELADO','REAGENDADO','FALTOU'));
    end if;
end $$;

-- Repetições/retries não podem gerar duas chegadas ou duas posições ativas.
create unique index if not exists ux_v2159_checkin_ativo
    on plantaopro.agendamento_checkins (cliente_id, agendamento_id) where reg_status = 'A';
create unique index if not exists ux_v2159_fila_painel_ativa
    on plantaopro.painel_chamada_fila (cliente_id, agendamento_id) where reg_status = 'A' and agendamento_id is not null;
create unique index if not exists ux_v2159_fila_triagem_ativa
    on plantaopro.triagem_fila (cliente_id, agendamento_id) where reg_status = 'A' and agendamento_id is not null;

create index if not exists ix_v2159_recepcao_dia
    on plantaopro.agendamentos (cliente_id, unidade_id, data_inicio, id)
    where reg_status = 'A';
create index if not exists ix_v2159_fila_ordenacao
    on plantaopro.painel_chamada_fila (cliente_id, status, prioridade desc, reg_date, id)
    where reg_status = 'A';

-- ============================================================
-- Seção 48 — plantaopro.jornada_clinica_v2160
-- ============================================================

-- SOURCE: database/schema/380_v2160_triagem_consulta_jornada.sql
-- SOURCE-SHA256: 313d92fe8c3319ad979d9e1af2808ac748e932110204d0b321cddaed62141403
-- PlantãoPro v2.16.0 — identidade, concorrência e evidência da jornada clínica.
-- Migration incremental: não altera artefatos já aplicados.
set search_path to plantaopro, public;

alter table plantaopro.triagens
    add column if not exists versao integer not null default 1,
    add column if not exists finalizada_em timestamptz,
    add column if not exists finalizada_por uuid,
    add column if not exists assumida_por uuid,
    add column if not exists assumida_em timestamptz,
    add column if not exists atendimento_id uuid,
    add column if not exists unidade_id uuid,
    add column if not exists cliente_id uuid,
    add column if not exists paciente_id uuid,
    add column if not exists agendamento_id uuid,
    add column if not exists classificacao_risco text,
    add column if not exists queixa_principal text,
    add column if not exists pressao_sistolica numeric(6,2),
    add column if not exists pressao_diastolica numeric(6,2),
    add column if not exists frequencia_cardiaca numeric(6,2),
    add column if not exists frequencia_respiratoria numeric(6,2),
    add column if not exists temperatura numeric(6,2),
    add column if not exists saturacao numeric(6,2),
    add column if not exists peso numeric(8,2),
    add column if not exists altura numeric(5,2),
    add column if not exists imc numeric(8,2),
    add column if not exists glicemia numeric(8,2),
    add column if not exists alergias_relatadas text,
    add column if not exists medicamentos_uso text,
    add column if not exists observacoes text,
    add column if not exists reg_status char(1) not null default 'A',
    add column if not exists reg_date timestamptz not null default now(),
    add column if not exists created_by uuid,
    add column if not exists updated_by uuid;

create table if not exists plantaopro.atendimentos_fila (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    unidade_id uuid null,
    agendamento_id uuid not null,
    paciente_id uuid null,
    senha text null,
    status text not null default 'AGUARDANDO',
    prioridade integer not null default 0,
    checkin_em timestamptz not null default now(),
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    reg_update timestamptz null,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now()
);

alter table plantaopro.consultas
    add column if not exists cliente_id uuid,
    add column if not exists paciente_id uuid,
    add column if not exists unidade_id uuid,
    add column if not exists triagem_id uuid,
    add column if not exists reg_status char(1) not null default 'A',
    add column if not exists reg_date timestamptz not null default now();

create table if not exists plantaopro.triagem_encaminhamentos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    triagem_id uuid not null,
    destino text not null default 'CONSULTA',
    status text not null default 'ATIVO',
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.consulta_historico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    paciente_id uuid null,
    consulta_id uuid null,
    evento varchar(50) not null,
    versao integer not null default 1,
    created_by uuid null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);

do $$
declare conflitos text;
begin
  select string_agg(format('tenant=%s agendamento=%s atendimentos=%s', cliente_id, agendamento_id, ids), '; ')
    into conflitos
    from (
      select cliente_id, agendamento_id, string_agg(id::text, ',' order by id) ids
        from plantaopro.atendimentos_fila
       where status not in ('FINALIZADO','CANCELADO')
       group by cliente_id, agendamento_id having count(*) > 1
    ) d;
  if conflitos is not null then
    raise exception 'V2160_ATENDIMENTOS_ATIVOS_DUPLICADOS: %. Corrija os estados com registro auditável antes de repetir a migration.', conflitos;
  end if;
end $$;

-- Recupera a identidade operacional sem apagar ou fundir registros legados.
update plantaopro.triagens t
   set atendimento_id = a.id,
       unidade_id = coalesce(t.unidade_id, a.unidade_id)
  from plantaopro.atendimentos_fila a
 where t.atendimento_id is null
   and t.agendamento_id = a.agendamento_id
   and t.cliente_id = a.cliente_id
   and a.status not in ('FINALIZADO','CANCELADO');

alter table plantaopro.consultas add column if not exists assumida_por uuid;
alter table plantaopro.consultas add column if not exists assumida_em timestamptz;
alter table plantaopro.consultas add column if not exists triagem_snapshot jsonb;
alter table plantaopro.consultas add column if not exists triagem_snapshot_em timestamptz;

-- Um agendamento só pode possuir um atendimento ainda ativo. Walk-ins continuam
-- identificados pelo próprio atendimento e jamais por nome/data do paciente.
create unique index if not exists ux_v2160_atendimento_agendamento_ativo
    on plantaopro.atendimentos_fila(cliente_id, agendamento_id)
    where status not in ('FINALIZADO','CANCELADO');
create unique index if not exists ux_v2160_encaminhamento_triagem_consulta
    on plantaopro.triagem_encaminhamentos(cliente_id, triagem_id, destino)
    where destino='CONSULTA' and reg_status='A';
create index if not exists ix_v2160_triagem_fila_estavel
    on plantaopro.triagens(cliente_id, status, reg_date, id)
    where reg_status='A';
create index if not exists ix_v2160_consulta_fila_estavel
    on plantaopro.consultas(cliente_id, unidade_id, status, reg_date, id)
    where reg_status='A';
create index if not exists ix_v2160_historico_paciente
    on plantaopro.consulta_historico(cliente_id, paciente_id, reg_date desc, id);

-- Congela a evidência de triagem usada quando a consulta é criada/vinculada.
create or replace function plantaopro.v2160_snapshot_triagem_consulta() returns trigger
language plpgsql as $$
declare deve_capturar boolean;
begin
  if tg_op = 'INSERT' then
    deve_capturar := true;
  else
    deve_capturar := new.triagem_id is distinct from old.triagem_id;
  end if;
  if new.triagem_id is not null and deve_capturar then
    select jsonb_build_object(
      'triagemId', t.id, 'versao', t.versao, 'classificacaoRisco', t.classificacao_risco,
      'queixaPrincipal', t.queixa_principal, 'pressaoSistolica', t.pressao_sistolica,
      'pressaoDiastolica', t.pressao_diastolica, 'frequenciaCardiaca', t.frequencia_cardiaca,
      'frequenciaRespiratoria', t.frequencia_respiratoria, 'temperatura', t.temperatura,
      'saturacao', t.saturacao, 'glicemia', t.glicemia, 'peso', t.peso, 'altura', t.altura,
      'imc', t.imc, 'alergiasRelatadas', t.alergias_relatadas,
      'medicamentosEmUso', t.medicamentos_uso, 'observacoes', t.observacoes,
      'autorId', coalesce(t.finalizada_por,t.updated_by,t.created_by),
      'finalizadaEm', t.finalizada_em)
      into new.triagem_snapshot
      from plantaopro.triagens t
      where t.id=new.triagem_id and t.cliente_id=new.cliente_id
        and t.paciente_id=new.paciente_id and t.reg_status='A';
    if new.triagem_snapshot is null then
      raise exception 'V2160_TRIAGEM_INCOMPATIVEL: triagem %, consulta %, tenant % e paciente % não possuem vínculo ativo correspondente.', new.triagem_id, new.id, new.cliente_id, new.paciente_id;
    end if;
    new.triagem_snapshot_em=now();
  end if;
  return new;
end $$;
drop trigger if exists tr_v2160_snapshot_triagem_insert on plantaopro.consultas;
drop trigger if exists tr_v2160_snapshot_triagem_update on plantaopro.consultas;
drop trigger if exists tr_v2160_snapshot_triagem on plantaopro.consultas;
create trigger tr_v2160_snapshot_triagem_insert before insert
on plantaopro.consultas for each row execute function plantaopro.v2160_snapshot_triagem_consulta();
create trigger tr_v2160_snapshot_triagem_update before update of triagem_id
on plantaopro.consultas for each row
when (new.triagem_id is distinct from old.triagem_id)
execute function plantaopro.v2160_snapshot_triagem_consulta();

-- ============================================================
-- Seção 49 — plantaopro.portal_cliente_modulos_v2163
-- ============================================================

-- SOURCE: database/schema/390_v2163_portal_cliente_modulos.sql
-- SOURCE-SHA256: 496b631938ba68ac14d8b582644a985eb3d5535337d020f10c313a74b2eeaab7
-- PlantãoPro v2.16.3 - solicitações comerciais de módulos, snapshots e ativação idempotente.
set search_path to plantaopro, public;

alter table plantaopro.modulos_sistema add column if not exists preco_base numeric(14,2) null;
alter table plantaopro.modulos_sistema alter column preco_base drop not null;
alter table plantaopro.modulos_sistema add column if not exists periodicidade text null;
alter table plantaopro.modulos_sistema add column if not exists disponivel_comercialmente boolean not null default false;

create table if not exists plantaopro.solicitacoes_modulos (
 id uuid primary key, tenant_id uuid not null, cliente_id uuid null, protocolo text not null,
 status text not null, versao_condicoes text not null, total numeric(14,2) null, periodicidade text not null,
 inicio_previsto timestamptz not null, chave_idempotencia text not null, solicitado_por uuid null,
 decidido_por uuid null, decidido_em timestamptz null, justificativa text null,
 reg_date timestamptz not null default now(), reg_update timestamptz null, reg_status char(1) not null default 'A',
 constraint ck_solicitacoes_modulos_status check(status in ('PENDENTE','APROVADA','RECUSADA','CANCELADA')),
 constraint ck_solicitacoes_modulos_total check(total is null or total >= 0)
);
create table if not exists plantaopro.modulo_catalogo_dependencias (
 id uuid primary key default gen_random_uuid(), modulo_id uuid not null references plantaopro.modulos_sistema(id),
 modulo_dependencia_id uuid not null references plantaopro.modulos_sistema(id),
 reg_date timestamptz not null default now(), reg_status char(1) not null default 'A'
);
create table if not exists plantaopro.solicitacao_modulo_itens (
 id uuid primary key, solicitacao_id uuid not null references plantaopro.solicitacoes_modulos(id), modulo_id uuid not null,
 codigo text not null, nome text not null, descricao text not null, funcionalidades jsonb not null default '[]',
 dependencias jsonb not null default '[]', preco numeric(14,2) null, periodicidade text null,
 reg_date timestamptz not null default now(), reg_status char(1) not null default 'A'
);
create unique index if not exists ux_solicitacoes_modulos_idempotencia on plantaopro.solicitacoes_modulos(tenant_id,chave_idempotencia) where reg_status='A';
create unique index if not exists ux_modulo_catalogo_dependencia on plantaopro.modulo_catalogo_dependencias(modulo_id,modulo_dependencia_id) where reg_status='A';
create unique index if not exists ux_solicitacao_modulo_item on plantaopro.solicitacao_modulo_itens(solicitacao_id,modulo_id) where reg_status='A';
create index if not exists ix_solicitacoes_modulos_filtros on plantaopro.solicitacoes_modulos(status,tenant_id,reg_date desc) where reg_status='A';

-- Zero deixa de significar "gratuito": catálogo legado sem preço publicado passa a proposta.
update plantaopro.modulos_sistema set preco_base=null,periodicidade=null where preco_base=0 and reg_status='A';

-- ============================================================
-- Seção 50 — plantaopro.execucao_conferencia_v2167
-- ============================================================

-- SOURCE: database/schema/400_v2167_execucao_conferencia.sql
-- SOURCE-SHA256: 1794e0cdb6f5785bcdff3c2463b3ec52922509ccd7077eae66e6eb39017cc97e
-- PlantãoPro v2.16.7 — execução e correção auditável de plantões.
set search_path to plantaopro, public;

alter table medico_checkins
  add column if not exists checkin_recebido_em timestamptz,
  add column if not exists checkout_recebido_em timestamptz,
  add column if not exists checkin_declarado_em timestamptz,
  add column if not exists checkout_declarado_em timestamptz,
  add column if not exists timezone_contexto varchar(80),
  add column if not exists status_conferencia varchar(30) not null default 'REGISTRO_INCOMPLETO',
  add column if not exists inicio_aprovado_em timestamptz,
  add column if not exists fim_aprovado_em timestamptz,
  add column if not exists versao bigint not null default 1;

do $$ begin alter table medico_checkins add constraint ck_v2167_presenca_aprovada_ordem
 check (fim_aprovado_em is null or (inicio_aprovado_em is not null and fim_aprovado_em>=inicio_aprovado_em));
exception when duplicate_object then null; end $$;
do $$ begin alter table medico_checkins add constraint ck_v2167_presenca_status
 check (status_conferencia in ('REGISTRO_INCOMPLETO','PENDENTE','CORRECAO_PENDENTE','APROVADA','AJUSTE_POS_APURACAO'));
exception when duplicate_object then null; end $$;

create table if not exists medico_presenca_correcoes (
 id uuid primary key default gen_random_uuid(), tenant_id uuid not null, cliente_id uuid not null,
 presenca_id uuid not null references medico_checkins(id), escala_id uuid not null, medico_id uuid not null,
 inicio_original_em timestamptz, fim_original_em timestamptz, inicio_proposto_em timestamptz, fim_proposto_em timestamptz,
 justificativa varchar(1000) not null, status varchar(20) not null default 'PENDENTE', versao bigint not null default 1,
 versao_presenca_base bigint not null default 1,
 solicitado_por uuid not null, solicitado_em timestamptz not null default now(), decidido_por uuid, decidido_em timestamptz,
 justificativa_decisao varchar(1000), inicio_aprovado_em timestamptz, fim_aprovado_em timestamptz,
 constraint ck_v2167_correcao_status check(status in ('PENDENTE','APROVADA','RECUSADA','CANCELADA')),
 constraint ck_v2167_correcao_intervalo check(fim_proposto_em is null or inicio_proposto_em is null or fim_proposto_em>=inicio_proposto_em)
);
create unique index if not exists ux_v2167_correcao_pendente on medico_presenca_correcoes(tenant_id,presenca_id) where status='PENDENTE';
create index if not exists ix_v2167_conferencia on medico_checkins(tenant_id,status_conferencia,checkin_em desc);
create index if not exists ix_v2167_correcao_cliente on medico_presenca_correcoes(tenant_id,cliente_id,status,solicitado_em desc);

create table if not exists medico_presenca_historico (
 id uuid primary key default gen_random_uuid(), tenant_id uuid not null, cliente_id uuid not null,
 presenca_id uuid not null, correcao_id uuid, evento varchar(40) not null,
 valores_anteriores jsonb, valores_posteriores jsonb, justificativa varchar(1000),
 executado_por uuid not null, executado_em timestamptz not null default now()
);
create index if not exists ix_v2167_presenca_historico on medico_presenca_historico(tenant_id,presenca_id,executado_em desc);

alter table medico_presenca_correcoes
 add column if not exists versao_presenca_base bigint not null default 1;
update medico_presenca_correcoes x set versao_presenca_base=c.versao
from medico_checkins c
where c.id=x.presenca_id and x.status='PENDENTE' and x.versao_presenca_base=1 and c.versao<>1;

-- Backfill only legacy rows which have never entered the conference workflow.
-- This predicate is deliberately replay-safe: decisions and corrections are evidence
-- that the default value no longer represents an uninitialised legacy row.
update medico_checkins c set
 checkin_recebido_em=coalesce(c.checkin_recebido_em,c.checkin_em),
 checkout_recebido_em=coalesce(c.checkout_recebido_em,c.checkout_em),
 status_conferencia=case when c.checkout_em is null then 'REGISTRO_INCOMPLETO' else 'PENDENTE' end
where c.status_conferencia='REGISTRO_INCOMPLETO'
  and c.inicio_aprovado_em is null
  and c.fim_aprovado_em is null
  and not exists(select 1 from medico_presenca_correcoes x where x.presenca_id=c.id)
  and not exists(select 1 from medico_presenca_historico hx where hx.presenca_id=c.id);

-- SOURCE: database/schema/410_v2168_conferencia_integridade.sql
-- SOURCE-SHA256: 4d4087ec92c261b8331729416e6cd3ea9620fb33d33b49992cda1e5462669138
-- PlantãoPro v2.16.8 — evolução aditiva da conferência sem reabrir decisões.
set search_path to plantaopro, public;

alter table medico_presenca_correcoes
 add column if not exists versao_presenca_base bigint not null default 1;

-- Apenas propostas ainda pendentes precisam ser compatibilizadas com a versão
-- já incrementada pela criação da correção na v2.16.7.
update medico_presenca_correcoes x set versao_presenca_base=c.versao
from medico_checkins c
where c.id=x.presenca_id
  and x.status='PENDENTE'
  and x.versao_presenca_base=1
  and c.versao<>1;

-- Nunca recalcula status: completa somente metadados técnicos ausentes.
update medico_checkins set
 checkin_recebido_em=coalesce(checkin_recebido_em,checkin_em),
 checkout_recebido_em=coalesce(checkout_recebido_em,checkout_em)
where checkin_recebido_em is null or (checkout_em is not null and checkout_recebido_em is null);

-- ============================================================
-- Seção 51 — plantaopro.cobertura_substituicoes_v2170
-- ============================================================

-- SOURCE: database/schema/420_v2170_cobertura_substituicoes.sql
-- SOURCE-SHA256: 7c7fd13bb91f61ad13f1c1da01903d7af52208aef5dc01345888bc15f591d30b
-- PlantãoPro v2.17.0: concorrência e rastreabilidade da cobertura/substituição.
alter table plantaopro.substituicoes_plantao
    add column if not exists versao bigint not null default 1,
    add column if not exists escala_id uuid,
    add column if not exists plantao_id uuid,
    add column if not exists cliente_id uuid,
    add column if not exists nova_escala_id uuid,
    add column if not exists cancelada_em timestamptz,
    add column if not exists reg_status char(1) not null default 'A',
    add column if not exists reg_date timestamptz not null default now();

-- Um pedido ativo por atribuição. Estados finais permanecem consultáveis e auditáveis.
create unique index if not exists ux_v2170_substituicao_ativa_por_escala
    on plantaopro.substituicoes_plantao(escala_id)
    where reg_status='A' and status in ('SOLICITADA','APROVADA','SUBSTITUTO_CONVIDADO','AGUARDANDO_APROVACAO');

-- A nova atribuição é exclusiva da efetivação e relaciona as duas pontas sem mover presença/financeiro.
create unique index if not exists ux_v2170_substituicao_nova_escala
    on plantaopro.substituicoes_plantao(nova_escala_id)
    where nova_escala_id is not null;

alter table plantaopro.substituicao_candidatos
    add column if not exists substituicao_id uuid,
    add column if not exists medico_id uuid,
    add column if not exists expira_em timestamptz,
    add column if not exists respondido_em timestamptz,
    add column if not exists notificacao_status varchar(24) not null default 'PENDENTE',
    add column if not exists reg_status char(1) not null default 'A',
    add column if not exists reg_date timestamptz not null default now();

create unique index if not exists ux_v2170_candidato_convite_ativo
    on plantaopro.substituicao_candidatos(substituicao_id,medico_id)
    where reg_status='A' and status in ('CONVIDADO','ACEITO');

-- ============================================================
-- Seção 52 — plantaopro.ocorrencias_operacionais_v2171
-- ============================================================

-- SOURCE: database/schema/430_ocorrencias_operacionais.sql
-- SOURCE-SHA256: 2689f6f0dcb0529fed9def6b88cca53417e305a12e28e06fa4d64bb618bd8850
CREATE SCHEMA IF NOT EXISTS plantaopro;

CREATE TABLE IF NOT EXISTS plantaopro.ocorrencias_operacionais (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, unidade_id uuid NOT NULL,
 plantao_id uuid NULL, titulo varchar(160) NOT NULL, descricao varchar(4000) NOT NULL,
 categoria varchar(30) NOT NULL, prioridade varchar(10) NOT NULL, situacao varchar(30) NOT NULL DEFAULT 'ABERTA',
 solicitante_id uuid NOT NULL, responsavel_id uuid NULL, prazo_resolucao timestamptz NULL,
 resolucao text NULL, cancelamento_motivo text NULL, versao integer NOT NULL DEFAULT 1,
 criado_em timestamptz NOT NULL DEFAULT now(), atualizado_em timestamptz NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A',
 CONSTRAINT ck_ocorrencia_categoria CHECK (categoria IN ('ATRASO','AUSENCIA','ACESSO','INDISPONIBILIDADE','COBERTURA','DIVERGENCIA_REGISTRO','OUTRA')),
 CONSTRAINT ck_ocorrencia_prioridade CHECK (prioridade IN ('BAIXA','MEDIA','ALTA','CRITICA')),
 CONSTRAINT ck_ocorrencia_situacao CHECK (situacao IN ('ABERTA','EM_ATENDIMENTO','AGUARDANDO_INFORMACAO','RESOLVIDA','CANCELADA'))
);
CREATE INDEX IF NOT EXISTS ix_ocorrencias_tenant_fila ON plantaopro.ocorrencias_operacionais(tenant_id,situacao,atualizado_em DESC,id);
CREATE INDEX IF NOT EXISTS ix_ocorrencias_unidade ON plantaopro.ocorrencias_operacionais(tenant_id,unidade_id,criado_em DESC);

CREATE TABLE IF NOT EXISTS plantaopro.ocorrencia_eventos (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, ocorrencia_id uuid NOT NULL REFERENCES plantaopro.ocorrencias_operacionais(id),
 tipo varchar(40) NOT NULL, autor_id uuid NOT NULL, descricao text NOT NULL, visivel_solicitante boolean NOT NULL DEFAULT true,
 dados jsonb NOT NULL DEFAULT '{}'::jsonb, criado_em timestamptz NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A'
);
CREATE INDEX IF NOT EXISTS ix_ocorrencia_eventos_historico ON plantaopro.ocorrencia_eventos(tenant_id,ocorrencia_id,criado_em,id);

CREATE TABLE IF NOT EXISTS plantaopro.ocorrencia_encaminhamentos (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, ocorrencia_id uuid NOT NULL REFERENCES plantaopro.ocorrencias_operacionais(id),
 tipo varchar(30) NOT NULL, referencia_id uuid NULL, estado varchar(20) NOT NULL, chave_idempotencia varchar(100) NOT NULL,
 criado_por uuid NOT NULL, criado_em timestamptz NOT NULL DEFAULT now(), atualizado_em timestamptz NULL, reg_status char(1) NOT NULL DEFAULT 'A'
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_ocorrencia_encaminhamento ON plantaopro.ocorrencia_encaminhamentos(tenant_id,ocorrencia_id,tipo,chave_idempotencia) WHERE reg_status='A';

-- ============================================================
-- Seção 53 — plantaopro.financeiro_clinico_convenios_v2180
-- ============================================================

-- SOURCE: database/schema/440_v2180_financeiro_clinico_convenios.sql
-- SOURCE-SHA256: f16c59da80b99719530ea7f2c6530b86b3f7abe7c6f129c55183ca9d2269daa9
-- v2.18.0 - base comercial do Saúde 360 (sem gateway, TISS ou integração externa).
SET search_path TO plantaopro, public;

-- Declarações de compatibilidade tornam a migration segura também quando aplicada
-- isoladamente; em upgrades, IF NOT EXISTS preserva integralmente as tabelas atuais.
CREATE TABLE IF NOT EXISTS plantaopro.clinica_contas_receber (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid, cliente_id uuid, paciente_id uuid,
    agendamento_id uuid, consulta_id uuid, descricao text NOT NULL DEFAULT '', valor_total numeric(14,2) NOT NULL DEFAULT 0,
    valor_pendente numeric(14,2) NOT NULL DEFAULT 0, vencimento date, status text NOT NULL DEFAULT 'ABERTO',
    created_by uuid, updated_by uuid, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz,
    reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz, reg_status char(1) NOT NULL DEFAULT 'A'
);
CREATE TABLE IF NOT EXISTS plantaopro.clinica_recebimentos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid, cliente_id uuid, conta_receber_id uuid,
    valor numeric(14,2) NOT NULL DEFAULT 0, forma_pagamento text NOT NULL DEFAULT '', status text NOT NULL DEFAULT 'CONFIRMADO',
    created_by uuid, updated_by uuid, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz,
    reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz, reg_status char(1) NOT NULL DEFAULT 'A'
);
CREATE TABLE IF NOT EXISTS plantaopro.clinica_caixa (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid, cliente_id uuid, saldo_inicial numeric(14,2) NOT NULL DEFAULT 0,
    status text NOT NULL DEFAULT 'ABERTO', created_by uuid, updated_by uuid, created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz, reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz, reg_status char(1) NOT NULL DEFAULT 'A'
);
CREATE TABLE IF NOT EXISTS plantaopro.plano_saude_pacientes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid, cliente_id uuid, paciente_id uuid, plano_saude_id uuid,
    numero_carteirinha text NOT NULL DEFAULT '', principal boolean NOT NULL DEFAULT false, validade date,
    status text NOT NULL DEFAULT 'ATIVO', created_by uuid, updated_by uuid, created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz, reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz, reg_status char(1) NOT NULL DEFAULT 'A'
);

ALTER TABLE IF EXISTS plantaopro.clinica_contas_receber
    ADD COLUMN IF NOT EXISTS convenio_id uuid,
    ADD COLUMN IF NOT EXISTS plano_saude_id uuid,
    ADD COLUMN IF NOT EXISTS forma_cobranca varchar(30),
    ADD COLUMN IF NOT EXISTS valor_desconto numeric(14,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS valor_coparticipacao numeric(14,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS cancelada_em timestamptz,
    ADD COLUMN IF NOT EXISTS cancelada_por uuid,
    ADD COLUMN IF NOT EXISTS motivo_cancelamento text;

ALTER TABLE IF EXISTS plantaopro.clinica_recebimentos
    ADD COLUMN IF NOT EXISTS caixa_id uuid,
    ADD COLUMN IF NOT EXISTS data_recebimento timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS estornado_em timestamptz,
    ADD COLUMN IF NOT EXISTS estornado_por uuid,
    ADD COLUMN IF NOT EXISTS justificativa_estorno text;

ALTER TABLE IF EXISTS plantaopro.clinica_caixa
    ADD COLUMN IF NOT EXISTS unidade_id uuid,
    ADD COLUMN IF NOT EXISTS aberto_em timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS fechado_em timestamptz,
    ADD COLUMN IF NOT EXISTS saldo_informado numeric(14,2),
    ADD COLUMN IF NOT EXISTS diferenca numeric(14,2),
    ADD COLUMN IF NOT EXISTS conferencia_observacao text;

CREATE TABLE IF NOT EXISTS plantaopro.fechamento_caixa (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL,
    caixa_id uuid NOT NULL, saldo_sistema numeric(14,2) NOT NULL,
    saldo_informado numeric(14,2) NOT NULL, diferenca numeric(14,2) NOT NULL,
    conferencia_observacao text NOT NULL, fechado_por uuid NOT NULL,
    fechado_em timestamptz NOT NULL DEFAULT now(), status varchar(20) NOT NULL DEFAULT 'FECHADO',
    created_by uuid, reg_date timestamptz NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A'
);

CREATE TABLE IF NOT EXISTS plantaopro.clinica_caixa_movimentos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL,
    caixa_id uuid NOT NULL, recebimento_id uuid, tipo varchar(30) NOT NULL,
    valor numeric(14,2) NOT NULL, forma_pagamento varchar(30), justificativa text,
    ajuste_pos_fechamento boolean NOT NULL DEFAULT false, created_by uuid,
    reg_date timestamptz NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A'
);

CREATE TABLE IF NOT EXISTS plantaopro.convenio_contratos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, convenio_id uuid NOT NULL,
    numero varchar(80) NOT NULL, inicio_vigencia date NOT NULL, fim_vigencia date NOT NULL,
    exige_autorizacao boolean NOT NULL DEFAULT false, regra_coparticipacao jsonb NOT NULL DEFAULT '{}'::jsonb,
    status varchar(20) NOT NULL DEFAULT 'ATIVO', created_by uuid, updated_by uuid,
    reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz, reg_status char(1) NOT NULL DEFAULT 'A'
);

CREATE TABLE IF NOT EXISTS plantaopro.convenio_tabelas_procedimentos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, convenio_id uuid NOT NULL,
    contrato_id uuid, procedimento_id uuid, codigo varchar(80) NOT NULL, descricao text NOT NULL,
    valor numeric(14,2) NOT NULL, exige_autorizacao boolean NOT NULL DEFAULT false,
    status varchar(20) NOT NULL DEFAULT 'ATIVO', created_by uuid,
    reg_date timestamptz NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A'
);

CREATE TABLE IF NOT EXISTS plantaopro.convenio_glosas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, convenio_id uuid NOT NULL,
    conta_receber_id uuid NOT NULL, autorizacao_id uuid, motivo text NOT NULL,
    valor_glosado numeric(14,2) NOT NULL, status varchar(20) NOT NULL DEFAULT 'ABERTA',
    justificativa_recurso text, recurso_em timestamptz, resolvida_em timestamptz,
    created_by uuid, updated_by uuid, reg_date timestamptz NOT NULL DEFAULT now(),
    reg_update timestamptz, reg_status char(1) NOT NULL DEFAULT 'A'
);

ALTER TABLE IF EXISTS plantaopro.plano_saude_pacientes
    ADD COLUMN IF NOT EXISTS titularidade varchar(20) NOT NULL DEFAULT 'TITULAR',
    ADD COLUMN IF NOT EXISTS titular_nome varchar(160),
    ADD COLUMN IF NOT EXISTS cobertura_resumo text,
    ADD COLUMN IF NOT EXISTS inativado_em timestamptz;

CREATE TABLE IF NOT EXISTS plantaopro.regras_repasse_medico (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, medico_id uuid NOT NULL,
    procedimento_id uuid, tipo_regra varchar(20) NOT NULL, percentual numeric(7,4), valor_fixo numeric(14,2),
    vigencia_inicio date NOT NULL, vigencia_fim date, status varchar(20) NOT NULL DEFAULT 'ATIVO',
    created_by uuid, updated_by uuid, reg_date timestamptz NOT NULL DEFAULT now(),
    reg_update timestamptz, reg_status char(1) NOT NULL DEFAULT 'A'
);

CREATE TABLE IF NOT EXISTS plantaopro.repasses_medicos_clinicos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, medico_id uuid NOT NULL,
    consulta_id uuid NOT NULL, recebimento_id uuid, regra_repasse_id uuid,
    valor_base numeric(14,2) NOT NULL, valor_repasse numeric(14,2) NOT NULL,
    status varchar(20) NOT NULL DEFAULT 'PENDENTE', motivo_contestacao text,
    justificativa_cancelamento text, pago_em timestamptz, created_by uuid, updated_by uuid,
    reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz, reg_status char(1) NOT NULL DEFAULT 'A'
);

CREATE TABLE IF NOT EXISTS plantaopro.auditoria_financeira_clinica (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, usuario_id uuid,
    entidade varchar(80) NOT NULL, entidade_id uuid, acao varchar(80) NOT NULL,
    detalhes jsonb NOT NULL DEFAULT '{}'::jsonb, ocorrido_em timestamptz NOT NULL DEFAULT now(),
    reg_status char(1) NOT NULL DEFAULT 'A'
);

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_repasse_valores_nao_negativos') THEN
        ALTER TABLE plantaopro.repasses_medicos_clinicos ADD CONSTRAINT ck_repasse_valores_nao_negativos CHECK (valor_base >= 0 AND valor_repasse >= 0);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_glosa_valor_positivo') THEN
        ALTER TABLE plantaopro.convenio_glosas ADD CONSTRAINT ck_glosa_valor_positivo CHECK (valor_glosado > 0);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_contrato_vigencia') THEN
        ALTER TABLE plantaopro.convenio_contratos ADD CONSTRAINT ck_contrato_vigencia CHECK (fim_vigencia >= inicio_vigencia);
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_repasse_consulta_ativo ON plantaopro.repasses_medicos_clinicos(cliente_id,consulta_id) WHERE reg_status='A' AND status<>'CANCELADO';
CREATE UNIQUE INDEX IF NOT EXISTS ux_recebimento_conta_confirmado ON plantaopro.clinica_recebimentos(cliente_id,conta_receber_id) WHERE reg_status='A' AND status='CONFIRMADO';
CREATE INDEX IF NOT EXISTS ix_conta_clinica_tenant_status_vencimento ON plantaopro.clinica_contas_receber(cliente_id,status,vencimento);
CREATE INDEX IF NOT EXISTS ix_caixa_tenant_status_data ON plantaopro.clinica_caixa(cliente_id,status,aberto_em);
CREATE INDEX IF NOT EXISTS ix_contrato_tenant_convenio_vigencia ON plantaopro.convenio_contratos(cliente_id,convenio_id,fim_vigencia);
CREATE INDEX IF NOT EXISTS ix_glosa_tenant_status_convenio ON plantaopro.convenio_glosas(cliente_id,status,convenio_id);
CREATE INDEX IF NOT EXISTS ix_plano_paciente_tenant_paciente_status ON plantaopro.plano_saude_pacientes(cliente_id,paciente_id,status);
CREATE INDEX IF NOT EXISTS ix_repasse_tenant_medico_status ON plantaopro.repasses_medicos_clinicos(cliente_id,medico_id,status);
CREATE INDEX IF NOT EXISTS ix_auditoria_financeira_tenant_data ON plantaopro.auditoria_financeira_clinica(cliente_id,ocorrido_em DESC);

-- ============================================================
-- Seção 54 — plantaopro.administrativo360_base
-- ============================================================

-- SOURCE: database/migrations/2026_09_v2190_administrativo360_base.sql
-- SOURCE-SHA256: 49da91626a61ed59b8566c0a4c504ccc11d7b0b4b9d438adfb1a913c3a632fb6
-- Administrativo360 / Prompt 1: base multiempresa, cadastros e contratação.
-- Idempotente e seguro para upgrade: todos os vínculos carregam tenant_id.
CREATE SCHEMA IF NOT EXISTS plantaopro;

CREATE TABLE IF NOT EXISTS plantaopro.adm_departamentos (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
 codigo varchar(30) NOT NULL, nome varchar(120) NOT NULL, ativo boolean NOT NULL DEFAULT true,
 created_by uuid NULL, reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, reg_status char(1) NOT NULL DEFAULT 'A',
 CONSTRAINT ck_adm_departamentos_reg_status CHECK(reg_status IN ('A','I'))
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_adm_departamentos_tenant_codigo ON plantaopro.adm_departamentos(tenant_id,lower(codigo)) WHERE reg_status='A';

CREATE TABLE IF NOT EXISTS plantaopro.adm_cargos (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
 departamento_id uuid NULL REFERENCES plantaopro.adm_departamentos(id), codigo varchar(30) NOT NULL, nome varchar(120) NOT NULL,
 ativo boolean NOT NULL DEFAULT true, created_by uuid NULL, reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, reg_status char(1) NOT NULL DEFAULT 'A',
 CONSTRAINT ck_adm_cargos_reg_status CHECK(reg_status IN ('A','I'))
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_adm_cargos_tenant_codigo ON plantaopro.adm_cargos(tenant_id,lower(codigo)) WHERE reg_status='A';

CREATE TABLE IF NOT EXISTS plantaopro.adm_colaboradores (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id), cargo_id uuid NOT NULL REFERENCES plantaopro.adm_cargos(id),
 matricula varchar(30) NOT NULL, nome varchar(160) NOT NULL, cpf char(11) NOT NULL, email varchar(180) NOT NULL, status varchar(20) NOT NULL DEFAULT 'ATIVO',
 created_by uuid NULL, reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, reg_status char(1) NOT NULL DEFAULT 'A',
 CONSTRAINT ck_adm_colaborador_cpf CHECK(cpf ~ '^[0-9]{11}$'), CONSTRAINT ck_adm_colaborador_status CHECK(status IN ('ATIVO','AFASTADO','DESLIGADO')),
 CONSTRAINT ck_adm_colaborador_reg_status CHECK(reg_status IN ('A','I'))
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_adm_colaboradores_tenant_matricula ON plantaopro.adm_colaboradores(tenant_id,lower(matricula)) WHERE reg_status='A';
CREATE UNIQUE INDEX IF NOT EXISTS ux_adm_colaboradores_tenant_cpf ON plantaopro.adm_colaboradores(tenant_id,cpf) WHERE reg_status='A';

CREATE TABLE IF NOT EXISTS plantaopro.adm_contratos_trabalho (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id), colaborador_id uuid NOT NULL REFERENCES plantaopro.adm_colaboradores(id),
 tipo varchar(20) NOT NULL, inicio date NOT NULL, fim date NULL, salario numeric(14,2) NOT NULL, carga_horaria_semanal smallint NOT NULL,
 status varchar(20) NOT NULL DEFAULT 'VIGENTE', created_by uuid NULL, reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, reg_status char(1) NOT NULL DEFAULT 'A',
 CONSTRAINT ck_adm_contrato_tipo CHECK(tipo IN ('CLT','PJ','ESTAGIO','TEMPORARIO')), CONSTRAINT ck_adm_contrato_vigencia CHECK(fim IS NULL OR fim>=inicio),
 CONSTRAINT ck_adm_contrato_valores CHECK(salario>0 AND carga_horaria_semanal BETWEEN 1 AND 60), CONSTRAINT ck_adm_contrato_status CHECK(status IN ('VIGENTE','ENCERRADO','CANCELADO')),
 CONSTRAINT ck_adm_contrato_reg_status CHECK(reg_status IN ('A','I'))
);
CREATE INDEX IF NOT EXISTS ix_adm_contratos_tenant_status ON plantaopro.adm_contratos_trabalho(tenant_id,status) WHERE reg_status='A';

-- Impede referências cruzadas entre tenants, inclusive por SQL direto.
CREATE OR REPLACE FUNCTION plantaopro.adm360_validar_tenant() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF TG_TABLE_NAME='adm_cargos' AND NEW.departamento_id IS NOT NULL AND NOT EXISTS(SELECT 1 FROM plantaopro.adm_departamentos d WHERE d.id=NEW.departamento_id AND d.tenant_id=NEW.tenant_id) THEN RAISE EXCEPTION 'Departamento pertence a outro tenant'; END IF;
 IF TG_TABLE_NAME='adm_colaboradores' AND NOT EXISTS(SELECT 1 FROM plantaopro.adm_cargos c WHERE c.id=NEW.cargo_id AND c.tenant_id=NEW.tenant_id) THEN RAISE EXCEPTION 'Cargo pertence a outro tenant'; END IF;
 IF TG_TABLE_NAME='adm_contratos_trabalho' AND NOT EXISTS(SELECT 1 FROM plantaopro.adm_colaboradores p WHERE p.id=NEW.colaborador_id AND p.tenant_id=NEW.tenant_id) THEN RAISE EXCEPTION 'Colaborador pertence a outro tenant'; END IF;
 RETURN NEW;
END $$;
DROP TRIGGER IF EXISTS trg_adm_cargos_tenant ON plantaopro.adm_cargos;
CREATE TRIGGER trg_adm_cargos_tenant BEFORE INSERT OR UPDATE ON plantaopro.adm_cargos FOR EACH ROW EXECUTE FUNCTION plantaopro.adm360_validar_tenant();
DROP TRIGGER IF EXISTS trg_adm_colaboradores_tenant ON plantaopro.adm_colaboradores;
CREATE TRIGGER trg_adm_colaboradores_tenant BEFORE INSERT OR UPDATE ON plantaopro.adm_colaboradores FOR EACH ROW EXECUTE FUNCTION plantaopro.adm360_validar_tenant();
DROP TRIGGER IF EXISTS trg_adm_contratos_tenant ON plantaopro.adm_contratos_trabalho;
CREATE TRIGGER trg_adm_contratos_tenant BEFORE INSERT OR UPDATE ON plantaopro.adm_contratos_trabalho FOR EACH ROW EXECUTE FUNCTION plantaopro.adm360_validar_tenant();

-- ============================================================
-- Seção 55 — plantaopro.administrativo360_suprimentos
-- ============================================================

-- SOURCE: database/migrations/2026_09_v2191_administrativo360_suprimentos.sql
-- SOURCE-SHA256: 6716a5d00d63e6693eb3f2f654b5c60ec9b0861daa978939ce788ee6fc2aaf11
-- Administrativo 360 bloco 2: suprimentos, qualidade, estoque e coleta.
CREATE SEQUENCE IF NOT EXISTS plantaopro.adm360_pedido_numero;
CREATE TABLE IF NOT EXISTS plantaopro.adm360_parceiros(id uuid primary key default gen_random_uuid(),tenant_id uuid not null references plantaopro.tenants(id),nome varchar(160) not null,documento varchar(20),fornecedor boolean not null default false,ativo boolean not null default true,created_at timestamptz not null default now(),unique(tenant_id,id));
CREATE UNIQUE INDEX ux_adm360_parceiro_documento ON plantaopro.adm360_parceiros(tenant_id,documento) WHERE documento IS NOT NULL;
CREATE TABLE IF NOT EXISTS plantaopro.adm360_produtos(id uuid primary key default gen_random_uuid(),tenant_id uuid not null references plantaopro.tenants(id),sku varchar(40) not null,nome varchar(180) not null,unidade varchar(12) not null,codigo_barras varchar(80),controla_lote boolean not null default false,controla_serie boolean not null default false,exige_inspecao boolean not null default true,ativo boolean not null default true,created_at timestamptz not null default now(),unique(tenant_id,id),unique(tenant_id,sku));
CREATE UNIQUE INDEX ux_adm360_produto_barcode ON plantaopro.adm360_produtos(tenant_id,codigo_barras) WHERE codigo_barras IS NOT NULL;
CREATE TABLE IF NOT EXISTS plantaopro.adm360_locais(id uuid primary key default gen_random_uuid(),tenant_id uuid not null references plantaopro.tenants(id),codigo varchar(30) not null,nome varchar(120) not null,tipo varchar(10) not null check(tipo in('INTERNO','EXTERNO')),ativo boolean not null default true,created_at timestamptz not null default now(),unique(tenant_id,id),unique(tenant_id,codigo));
CREATE TABLE IF NOT EXISTS plantaopro.adm360_pedidos(id uuid primary key,tenant_id uuid not null references plantaopro.tenants(id),numero varchar(30) not null,fornecedor_id uuid not null,situacao varchar(15) not null default 'RASCUNHO' check(situacao in('RASCUNHO','APROVADO','PARCIAL','RECEBIDO','CANCELADO')),previsao date,frete numeric(18,4) not null default 0 check(frete>=0),aprovado_em timestamptz,aprovado_por uuid,idempotency_key varchar(120),versao bigint not null default 1,created_by uuid,created_at timestamptz not null default now(),unique(tenant_id,id),unique(tenant_id,numero),unique(tenant_id,idempotency_key),foreign key(tenant_id,fornecedor_id) references plantaopro.adm360_parceiros(tenant_id,id));
CREATE TABLE IF NOT EXISTS plantaopro.adm360_pedido_itens(id uuid primary key,tenant_id uuid not null,pedido_id uuid not null,produto_id uuid not null,quantidade numeric(18,4) not null check(quantidade>0),quantidade_recebida numeric(18,4) not null default 0 check(quantidade_recebida>=0 and quantidade_recebida<=quantidade),preco_unitario numeric(18,4) not null check(preco_unitario>=0),desconto numeric(18,4) not null default 0 check(desconto>=0),unique(tenant_id,id),foreign key(tenant_id,pedido_id) references plantaopro.adm360_pedidos(tenant_id,id),foreign key(tenant_id,produto_id) references plantaopro.adm360_produtos(tenant_id,id));
CREATE TABLE IF NOT EXISTS plantaopro.adm360_lotes(id uuid primary key,tenant_id uuid not null,produto_id uuid not null,codigo varchar(80) not null,fabricacao date,validade date,versao bigint not null default 1,unique(tenant_id,id),unique(tenant_id,produto_id,codigo),foreign key(tenant_id,produto_id) references plantaopro.adm360_produtos(tenant_id,id),check(validade is null or fabricacao is null or validade>=fabricacao));
CREATE TABLE IF NOT EXISTS plantaopro.adm360_recebimentos(id uuid primary key,tenant_id uuid not null,pedido_id uuid not null,documento varchar(80) not null,idempotency_key varchar(120) not null,confirmado_em timestamptz not null,created_by uuid,created_at timestamptz not null default now(),unique(tenant_id,id),unique(tenant_id,idempotency_key),foreign key(tenant_id,pedido_id) references plantaopro.adm360_pedidos(tenant_id,id));
CREATE TABLE IF NOT EXISTS plantaopro.adm360_recebimento_itens(id uuid primary key,tenant_id uuid not null,recebimento_id uuid not null,pedido_item_id uuid not null,produto_id uuid not null,lote_id uuid not null,local_id uuid not null,quantidade numeric(18,4) not null check(quantidade>0),quantidade_decidida numeric(18,4) not null default 0 check(quantidade_decidida>=0 and quantidade_decidida<=quantidade),condicao varchar(15) not null check(condicao in('QUARENTENA','LIBERADO','VENCIDO')),created_at timestamptz not null default now(),unique(tenant_id,id),foreign key(tenant_id,recebimento_id) references plantaopro.adm360_recebimentos(tenant_id,id),foreign key(tenant_id,produto_id) references plantaopro.adm360_produtos(tenant_id,id),foreign key(tenant_id,lote_id) references plantaopro.adm360_lotes(tenant_id,id),foreign key(tenant_id,local_id) references plantaopro.adm360_locais(tenant_id,id));
CREATE TABLE IF NOT EXISTS plantaopro.adm360_inspecoes(id uuid primary key,tenant_id uuid not null,recebimento_item_id uuid not null,aprovada numeric(18,4) not null check(aprovada>=0),reprovada numeric(18,4) not null check(reprovada>=0),justificativa text,destino varchar(80),idempotency_key varchar(120) not null,decidido_por uuid,decidido_em timestamptz not null,unique(tenant_id,idempotency_key),foreign key(tenant_id,recebimento_item_id) references plantaopro.adm360_recebimento_itens(tenant_id,id),check(aprovada+reprovada>0),check(reprovada=0 or length(trim(justificativa))>0));
CREATE TABLE IF NOT EXISTS plantaopro.adm360_movimentos(id uuid primary key,tenant_id uuid not null,produto_id uuid not null,lote_id uuid not null,local_id uuid not null,tipo varchar(30) not null,condicao varchar(15) not null check(condicao in('QUARENTENA','LIBERADO','BLOQUEADO','REPROVADO','VENCIDO')),quantidade numeric(18,4) not null check(quantidade<>0),motivo text,origem_tipo varchar(30) not null,origem_id uuid not null,idempotency_key varchar(160) not null,created_by uuid,created_at timestamptz not null default now(),unique(tenant_id,idempotency_key),foreign key(tenant_id,produto_id) references plantaopro.adm360_produtos(tenant_id,id),foreign key(tenant_id,lote_id) references plantaopro.adm360_lotes(tenant_id,id),foreign key(tenant_id,local_id) references plantaopro.adm360_locais(tenant_id,id));
CREATE TABLE IF NOT EXISTS plantaopro.adm360_reservas(id uuid primary key,tenant_id uuid not null,produto_id uuid not null,lote_id uuid not null,local_id uuid not null,quantidade numeric(18,4) not null check(quantidade>0),situacao varchar(12) not null default 'ATIVA' check(situacao in('ATIVA','CONSUMIDA','CANCELADA')),origem_tipo varchar(30) not null,origem_id uuid not null,idempotency_key varchar(120) not null,created_by uuid,created_at timestamptz not null default now(),unique(tenant_id,idempotency_key),foreign key(tenant_id,produto_id) references plantaopro.adm360_produtos(tenant_id,id),foreign key(tenant_id,lote_id) references plantaopro.adm360_lotes(tenant_id,id),foreign key(tenant_id,local_id) references plantaopro.adm360_locais(tenant_id,id));
CREATE TABLE IF NOT EXISTS plantaopro.adm360_inventarios(id uuid primary key,tenant_id uuid not null,local_id uuid not null,situacao varchar(12) not null check(situacao in('ABERTO','CONTAGEM','REVISAO','APROVADO','CANCELADO')),escopo text not null,motivo_ajuste text,idempotency_aprovacao varchar(120),versao bigint not null default 1,created_by uuid,created_at timestamptz not null default now(),unique(tenant_id,id),unique(tenant_id,idempotency_aprovacao),foreign key(tenant_id,local_id) references plantaopro.adm360_locais(tenant_id,id));
CREATE TABLE IF NOT EXISTS plantaopro.adm360_inventario_itens(id uuid primary key,tenant_id uuid not null,inventario_id uuid not null,produto_id uuid not null,lote_id uuid not null,esperado numeric(18,4) not null,contado numeric(18,4),ajustado numeric(18,4),unique(tenant_id,inventario_id,produto_id,lote_id),foreign key(tenant_id,inventario_id) references plantaopro.adm360_inventarios(tenant_id,id));
CREATE TABLE IF NOT EXISTS plantaopro.adm360_ocorrencias(id uuid primary key,tenant_id uuid not null,recebimento_item_id uuid,lote_id uuid not null,produto_id uuid not null,local_id uuid not null,tipo varchar(40) not null,descricao text not null,quantidade numeric(18,4) not null check(quantidade>0),situacao varchar(15) not null default 'ABERTA',responsavel_id uuid,prazo date,destino text,justificativa_encerramento text,versao bigint not null default 1,created_at timestamptz not null default now(),unique(tenant_id,id));
CREATE TABLE IF NOT EXISTS plantaopro.adm360_tarefas_coleta(id uuid primary key,tenant_id uuid not null references plantaopro.tenants(id),tipo varchar(15) not null check(tipo in('RECEBIMENTO','INVENTARIO','SEPARACAO')),descricao varchar(180) not null,situacao varchar(12) not null default 'ABERTA',atribuida_a uuid,origem_id uuid not null,created_at timestamptz not null default now(),unique(tenant_id,id));
CREATE TABLE IF NOT EXISTS plantaopro.adm360_leituras(id uuid primary key,tenant_id uuid not null,tarefa_id uuid not null,scan_id uuid not null,codigo varchar(100) not null,lote varchar(80),quantidade numeric(18,4) not null check(quantidade>0),created_by uuid,created_at timestamptz not null default now(),unique(tenant_id,scan_id),foreign key(tenant_id,tarefa_id) references plantaopro.adm360_tarefas_coleta(tenant_id,id));
CREATE INDEX IF NOT EXISTS ix_adm360_movimentos_saldo ON plantaopro.adm360_movimentos(tenant_id,produto_id,lote_id,local_id,condicao,created_at);
CREATE INDEX IF NOT EXISTS ix_adm360_pedidos_filtro ON plantaopro.adm360_pedidos(tenant_id,situacao,created_at,fornecedor_id);
CREATE INDEX IF NOT EXISTS ix_adm360_lotes_validade ON plantaopro.adm360_lotes(tenant_id,validade);
CREATE OR REPLACE VIEW plantaopro.adm360_saldos AS SELECT m.tenant_id,m.produto_id,p.nome produto,m.lote_id,l.codigo lote,l.validade,m.local_id,o.nome local,m.condicao,sum(m.quantidade)::numeric(18,4) fisico,coalesce((select sum(r.quantidade) from plantaopro.adm360_reservas r where r.tenant_id=m.tenant_id and r.produto_id=m.produto_id and r.lote_id=m.lote_id and r.local_id=m.local_id and r.situacao='ATIVA'),0)::numeric(18,4) reservado,(case when m.condicao='LIBERADO' and (l.validade is null or l.validade>=current_date) then sum(m.quantidade)-coalesce((select sum(r.quantidade) from plantaopro.adm360_reservas r where r.tenant_id=m.tenant_id and r.produto_id=m.produto_id and r.lote_id=m.lote_id and r.local_id=m.local_id and r.situacao='ATIVA'),0) else 0 end)::numeric(18,4) disponivel from plantaopro.adm360_movimentos m join plantaopro.adm360_produtos p on p.id=m.produto_id and p.tenant_id=m.tenant_id join plantaopro.adm360_lotes l on l.id=m.lote_id and l.tenant_id=m.tenant_id join plantaopro.adm360_locais o on o.id=m.local_id and o.tenant_id=m.tenant_id group by m.tenant_id,m.produto_id,p.nome,m.lote_id,l.codigo,l.validade,m.local_id,o.nome,m.condicao;

-- ============================================================
-- Seção 56 — plantaopro.administrativo360_orcamentos_regras
-- ============================================================

-- SOURCE: database/migrations/2026_09_v2192_administrativo360_regras_estoque_orcamentos.sql
-- SOURCE-SHA256: b5cc310b22733357834e1c566fe61425f7007d74a9a542749aaaaa4e1fc2f612
CREATE SEQUENCE IF NOT EXISTS plantaopro.adm360_orcamento_numero;

CREATE TABLE IF NOT EXISTS plantaopro.adm360_operacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    tipo varchar(50) NOT NULL,
    idempotency_key varchar(120) NOT NULL,
    payload_hash varchar(64) NOT NULL,
    resultado text,
    created_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, idempotency_key)
);

CREATE INDEX IF NOT EXISTS ix_adm360_operacoes_tenant_tipo ON plantaopro.adm360_operacoes(tenant_id, tipo, created_at);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema='plantaopro' AND table_name='adm360_inventario_itens' AND column_name='condicao'
    ) THEN
        ALTER TABLE plantaopro.adm360_inventario_itens ADD COLUMN condicao varchar(15) NOT NULL DEFAULT 'LIBERADO' CHECK(condicao IN ('QUARENTENA','LIBERADO','BLOQUEADO','REPROVADO','VENCIDO'));
    END IF;
END $$;

ALTER TABLE plantaopro.adm360_inventario_itens DROP CONSTRAINT IF EXISTS adm360_inventario_itens_tenant_id_inventario_id_produto_id_l_key;
CREATE UNIQUE INDEX IF NOT EXISTS ux_adm360_inv_itens_condicao ON plantaopro.adm360_inventario_itens(tenant_id, inventario_id, produto_id, lote_id, condicao);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_orcamentos (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    numero varchar(30) NOT NULL,
    revisao integer NOT NULL DEFAULT 1,
    hospital_id uuid NOT NULL,
    medico_id uuid,
    procedimento varchar(180) NOT NULL,
    responsavel_financeiro_id uuid NOT NULL,
    vendedor_id uuid,
    data_prevista date NOT NULL,
    validade date NOT NULL,
    situacao varchar(20) NOT NULL DEFAULT 'RASCUNHO' CHECK(situacao IN ('RASCUNHO','ENVIADO','APROVADO','REJEITADO','EXPIRADO','CANCELADO')),
    total_produtos numeric(18,4) NOT NULL DEFAULT 0 CHECK(total_produtos >= 0),
    desconto_geral numeric(18,4) NOT NULL DEFAULT 0 CHECK(desconto_geral >= 0),
    total_geral numeric(18,4) NOT NULL DEFAULT 0 CHECK(total_geral >= 0),
    observacoes text,
    aprovado_em timestamptz,
    aprovado_por uuid,
    idempotency_key varchar(120),
    versao bigint NOT NULL DEFAULT 1,
    created_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, numero, revisao),
    UNIQUE(tenant_id, idempotency_key),
    FOREIGN KEY(tenant_id, hospital_id) REFERENCES plantaopro.adm360_parceiros(tenant_id, id),
    FOREIGN KEY(tenant_id, responsavel_financeiro_id) REFERENCES plantaopro.adm360_parceiros(tenant_id, id)
);

CREATE INDEX IF NOT EXISTS ix_adm360_orcamentos_tenant_sit ON plantaopro.adm360_orcamentos(tenant_id, situacao, data_prevista);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_orcamento_itens (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    orcamento_id uuid NOT NULL,
    produto_id uuid NOT NULL,
    quantidade numeric(18,4) NOT NULL CHECK(quantidade > 0),
    preco_unitario numeric(18,4) NOT NULL CHECK(preco_unitario >= 0),
    desconto numeric(18,4) NOT NULL DEFAULT 0 CHECK(desconto >= 0),
    total numeric(18,4) NOT NULL CHECK(total >= 0),
    UNIQUE(tenant_id, id),
    FOREIGN KEY(tenant_id, orcamento_id) REFERENCES plantaopro.adm360_orcamentos(tenant_id, id),
    FOREIGN KEY(tenant_id, produto_id) REFERENCES plantaopro.adm360_produtos(tenant_id, id)
);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_orcamento_revisoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL,
    orcamento_id uuid NOT NULL,
    revisao integer NOT NULL,
    motivo text NOT NULL,
    snapshot_json text NOT NULL,
    criado_por uuid,
    criado_em timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY(tenant_id, orcamento_id) REFERENCES plantaopro.adm360_orcamentos(tenant_id, id)
);

CREATE INDEX IF NOT EXISTS ix_adm360_orcamento_revisoes_orc ON plantaopro.adm360_orcamento_revisoes(tenant_id, orcamento_id, revisao);

-- ============================================================
-- Seção 57 — Administrativo 360: Cirurgias, Vales, Expedição e Reconciliação
-- ============================================================

CREATE SEQUENCE IF NOT EXISTS plantaopro.adm360_cirurgia_numero;
CREATE SEQUENCE IF NOT EXISTS plantaopro.adm360_vale_numero;

ALTER TABLE plantaopro.adm360_reservas ADD COLUMN IF NOT EXISTS orcamento_item_id uuid;

DO $$
BEGIN
    ALTER TABLE plantaopro.adm360_reservas DROP CONSTRAINT IF EXISTS adm360_reservas_situacao_check;
    ALTER TABLE plantaopro.adm360_reservas DROP CONSTRAINT IF EXISTS chk_adm360_reservas_situacao;
    ALTER TABLE plantaopro.adm360_reservas ADD CONSTRAINT chk_adm360_reservas_situacao CHECK(situacao IN ('ATIVA', 'CONSUMIDA', 'ATENDIDA', 'CANCELADA'));
EXCEPTION
    WHEN OTHERS THEN NULL;
END $$;

CREATE TABLE IF NOT EXISTS plantaopro.adm360_cirurgias (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    numero varchar(30) NOT NULL,
    hospital_id uuid NOT NULL,
    medico_id uuid,
    procedimento varchar(180) NOT NULL,
    data_prevista date NOT NULL,
    hora_prevista time,
    orcamento_id uuid REFERENCES plantaopro.adm360_orcamentos(id),
    orcamento_revisao integer DEFAULT 1,
    responsavel_id uuid,
    local_destino_id uuid NOT NULL REFERENCES plantaopro.adm360_locais(id),
    situacao varchar(20) NOT NULL DEFAULT 'AGENDADA' CHECK(situacao IN ('AGENDADA', 'EM_ANDAMENTO', 'REALIZADA', 'CANCELADA')),
    observacoes text,
    versao bigint NOT NULL DEFAULT 1,
    created_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, numero)
);

CREATE INDEX IF NOT EXISTS ix_adm360_cirurgias_tenant_data ON plantaopro.adm360_cirurgias(tenant_id, data_prevista, situacao);
CREATE INDEX IF NOT EXISTS ix_adm360_cirurgias_orcamento ON plantaopro.adm360_cirurgias(tenant_id, orcamento_id);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_vales (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    numero varchar(30) NOT NULL,
    cirurgia_id uuid REFERENCES plantaopro.adm360_cirurgias(id),
    orcamento_id uuid REFERENCES plantaopro.adm360_orcamentos(id),
    orcamento_revisao integer DEFAULT 1,
    hospital_id uuid NOT NULL,
    custodiante_id uuid,
    local_origem_id uuid NOT NULL REFERENCES plantaopro.adm360_locais(id),
    local_destino_id uuid NOT NULL REFERENCES plantaopro.adm360_locais(id),
    data_saida_prevista date NOT NULL,
    data_saida_efetiva timestamptz,
    data_retorno_prevista date,
    data_reconciliacao timestamptz,
    situacao varchar(30) NOT NULL DEFAULT 'RASCUNHO' CHECK(situacao IN ('RASCUNHO', 'EM_SEPARACAO', 'PRONTO_PARA_EXPEDICAO', 'EXPEDIDO', 'RETORNO_PARCIAL', 'RECONCILIADO', 'CANCELADO')),
    situacao_financeira varchar(30) NOT NULL DEFAULT 'PENDENTE_VALORIZACAO' CHECK(situacao_financeira IN ('PENDENTE_VALORIZACAO', 'VALORIZADO', 'FATURADO')),
    separado_por uuid,
    separado_em timestamptz,
    expedido_por uuid,
    expedido_em timestamptz,
    reconciliado_por uuid,
    observacoes text,
    idempotency_key varchar(120),
    versao bigint NOT NULL DEFAULT 1,
    created_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, numero),
    UNIQUE(tenant_id, idempotency_key)
);

CREATE INDEX IF NOT EXISTS ix_adm360_vales_tenant_situacao ON plantaopro.adm360_vales(tenant_id, situacao, data_saida_prevista);
CREATE INDEX IF NOT EXISTS ix_adm360_vales_tenant_hospital ON plantaopro.adm360_vales(tenant_id, hospital_id);
CREATE INDEX IF NOT EXISTS ix_adm360_vales_cirurgia ON plantaopro.adm360_vales(tenant_id, cirurgia_id);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_vale_itens (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    vale_id uuid NOT NULL REFERENCES plantaopro.adm360_vales(id) ON DELETE CASCADE,
    produto_id uuid NOT NULL REFERENCES plantaopro.adm360_produtos(id),
    lote_id uuid NOT NULL REFERENCES plantaopro.adm360_lotes(id),
    reserva_id uuid REFERENCES plantaopro.adm360_reservas(id),
    quantidade_solicitada numeric(18,4) NOT NULL CHECK(quantidade_solicitada > 0),
    quantidade_separada numeric(18,4) NOT NULL DEFAULT 0 CHECK(quantidade_separada >= 0),
    quantidade_expedida numeric(18,4) NOT NULL DEFAULT 0 CHECK(quantidade_expedida >= 0),
    quantidade_consumida numeric(18,4) NOT NULL DEFAULT 0 CHECK(quantidade_consumida >= 0),
    quantidade_devolvida numeric(18,4) NOT NULL DEFAULT 0 CHECK(quantidade_devolvida >= 0),
    quantidade_perda numeric(18,4) NOT NULL DEFAULT 0 CHECK(quantidade_perda >= 0),
    preco_unitario numeric(18,4) NOT NULL DEFAULT 0 CHECK(preco_unitario >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, vale_id, produto_id, lote_id)
);

CREATE INDEX IF NOT EXISTS ix_adm360_vale_itens_tenant_vale ON plantaopro.adm360_vale_itens(tenant_id, vale_id);
CREATE INDEX IF NOT EXISTS ix_adm360_vale_itens_reserva ON plantaopro.adm360_vale_itens(tenant_id, reserva_id);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_vale_eventos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    vale_id uuid NOT NULL REFERENCES plantaopro.adm360_vales(id),
    vale_item_id uuid NOT NULL REFERENCES plantaopro.adm360_vale_itens(id),
    tipo varchar(30) NOT NULL CHECK(tipo IN ('CONSUMO', 'RETORNO', 'PERDA', 'AVARIA')),
    quantidade numeric(18,4) NOT NULL CHECK(quantidade > 0),
    data_evento timestamptz NOT NULL DEFAULT now(),
    motivo text,
    movimento_id uuid REFERENCES plantaopro.adm360_movimentos(id),
    idempotency_key varchar(120),
    registrado_por uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, idempotency_key)
);

CREATE INDEX IF NOT EXISTS ix_adm360_vale_eventos_tenant_vale ON plantaopro.adm360_vale_eventos(tenant_id, vale_id);
CREATE INDEX IF NOT EXISTS ix_adm360_vale_eventos_item ON plantaopro.adm360_vale_eventos(tenant_id, vale_item_id);

-- SECAO 58: ADMINISTRATIVO 360 - VALORIZACAO, VENDAS INTERNAS, CONTAS A RECEBER, CAIXA E COMISSOES (v2194)
DO $$
BEGIN
    ALTER TABLE plantaopro.adm360_inspecoes ALTER COLUMN recebimento_item_id DROP NOT NULL;
    ALTER TABLE plantaopro.adm360_inspecoes ADD COLUMN IF NOT EXISTS retorno_evento_id uuid REFERENCES plantaopro.adm360_vale_eventos(id);
    ALTER TABLE plantaopro.adm360_inspecoes ADD COLUMN IF NOT EXISTS origem_tipo varchar(30) NOT NULL DEFAULT 'RECEBIMENTO';
    ALTER TABLE plantaopro.adm360_vale_eventos ADD COLUMN IF NOT EXISTS quantidade_decidida numeric(18,4) NOT NULL DEFAULT 0;
EXCEPTION
    WHEN OTHERS THEN NULL;
END $$;

DO $$
BEGIN
    ALTER TABLE plantaopro.adm360_produtos ADD COLUMN IF NOT EXISTS preco_custo numeric(18,4) NOT NULL DEFAULT 0;
    ALTER TABLE plantaopro.adm360_lotes ADD COLUMN IF NOT EXISTS custo_unitario numeric(18,4);
EXCEPTION
    WHEN OTHERS THEN NULL;
END $$;

CREATE SEQUENCE IF NOT EXISTS plantaopro.adm360_venda_numero START 1;

CREATE TABLE IF NOT EXISTS plantaopro.adm360_valorizacoes (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    vale_id uuid NOT NULL REFERENCES plantaopro.adm360_vales(id),
    cirurgia_id uuid REFERENCES plantaopro.adm360_cirurgias(id),
    orcamento_id uuid REFERENCES plantaopro.adm360_orcamentos(id),
    orcamento_revisao integer,
    hospital_id uuid NOT NULL,
    pagador_id uuid NOT NULL,
    vendedor_id uuid,
    total_bruto numeric(18,4) NOT NULL DEFAULT 0 CHECK(total_bruto >= 0),
    desconto numeric(18,4) NOT NULL DEFAULT 0 CHECK(desconto >= 0),
    total_liquido numeric(18,4) NOT NULL DEFAULT 0 CHECK(total_liquido >= 0),
    total_custo numeric(18,4) NOT NULL DEFAULT 0 CHECK(total_custo >= 0),
    comissao_percentual numeric(18,4) NOT NULL DEFAULT 0 CHECK(comissao_percentual >= 0 AND comissao_percentual <= 100),
    comissao_prevista numeric(18,4) NOT NULL DEFAULT 0 CHECK(comissao_prevista >= 0),
    situacao varchar(20) NOT NULL DEFAULT 'CONFIRMADA' CHECK(situacao IN ('CONFIRMADA', 'CANCELADA')),
    versao bigint NOT NULL DEFAULT 1,
    created_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, vale_id)
);

CREATE INDEX IF NOT EXISTS ix_adm360_val_tenant_vale ON plantaopro.adm360_valorizacoes(tenant_id, vale_id);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_valorizacao_itens (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    valorizacao_id uuid NOT NULL REFERENCES plantaopro.adm360_valorizacoes(id) ON DELETE CASCADE,
    vale_item_id uuid NOT NULL REFERENCES plantaopro.adm360_vale_itens(id),
    produto_id uuid NOT NULL REFERENCES plantaopro.adm360_produtos(id),
    lote_id uuid NOT NULL REFERENCES plantaopro.adm360_lotes(id),
    quantidade_consumida numeric(18,4) NOT NULL CHECK(quantidade_consumida > 0),
    preco_unitario numeric(18,4) NOT NULL CHECK(preco_unitario >= 0),
    desconto numeric(18,4) NOT NULL DEFAULT 0 CHECK(desconto >= 0),
    subtotal numeric(18,4) NOT NULL CHECK(subtotal >= 0),
    custo_unitario numeric(18,4) NOT NULL DEFAULT 0 CHECK(custo_unitario >= 0),
    custo_total numeric(18,4) NOT NULL DEFAULT 0 CHECK(custo_total >= 0),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, valorizacao_id, vale_item_id)
);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_vendas (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    numero varchar(30) NOT NULL,
    origem_tipo varchar(30) NOT NULL DEFAULT 'VALE_CONSIGNACAO',
    origem_id uuid NOT NULL,
    valorizacao_id uuid REFERENCES plantaopro.adm360_valorizacoes(id),
    cliente_id uuid NOT NULL,
    pagador_id uuid NOT NULL,
    vendedor_id uuid,
    competencia date NOT NULL,
    total_bruto numeric(18,4) NOT NULL DEFAULT 0 CHECK(total_bruto >= 0),
    desconto numeric(18,4) NOT NULL DEFAULT 0 CHECK(desconto >= 0),
    total_liquido numeric(18,4) NOT NULL DEFAULT 0 CHECK(total_liquido >= 0),
    total_custo numeric(18,4) NOT NULL DEFAULT 0 CHECK(total_custo >= 0),
    comissao_percentual numeric(18,4) NOT NULL DEFAULT 0 CHECK(comissao_percentual >= 0 AND comissao_percentual <= 100),
    comissao_prevista numeric(18,4) NOT NULL DEFAULT 0 CHECK(comissao_prevista >= 0),
    condicao_pagamento varchar(50) NOT NULL DEFAULT 'A_VISTA',
    quantidade_parcelas integer NOT NULL DEFAULT 1 CHECK(quantidade_parcelas >= 1),
    situacao varchar(20) NOT NULL DEFAULT 'CONFIRMADA' CHECK(situacao IN ('RASCUNHO', 'CONFIRMADA', 'CANCELADA')),
    observacoes text,
    idempotency_key varchar(120),
    versao bigint NOT NULL DEFAULT 1,
    created_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, numero),
    UNIQUE(tenant_id, valorizacao_id),
    UNIQUE(tenant_id, idempotency_key)
);

CREATE INDEX IF NOT EXISTS ix_adm360_vendas_tenant_comp ON plantaopro.adm360_vendas(tenant_id, competencia, situacao);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_venda_itens (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    venda_id uuid NOT NULL REFERENCES plantaopro.adm360_vendas(id) ON DELETE CASCADE,
    produto_id uuid NOT NULL REFERENCES plantaopro.adm360_produtos(id),
    lote_id uuid NOT NULL REFERENCES plantaopro.adm360_lotes(id),
    quantidade numeric(18,4) NOT NULL CHECK(quantidade > 0),
    preco_unitario numeric(18,4) NOT NULL CHECK(preco_unitario >= 0),
    desconto numeric(18,4) NOT NULL DEFAULT 0 CHECK(desconto >= 0),
    subtotal numeric(18,4) NOT NULL CHECK(subtotal >= 0),
    custo_unitario numeric(18,4) NOT NULL DEFAULT 0 CHECK(custo_unitario >= 0),
    custo_total numeric(18,4) NOT NULL DEFAULT 0 CHECK(custo_total >= 0),
    UNIQUE(tenant_id, id)
);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_contas_financeiras (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    nome varchar(120) NOT NULL,
    tipo varchar(20) NOT NULL CHECK(tipo IN ('CAIXA', 'BANCO', 'APLICACAO')),
    banco varchar(40),
    agencia varchar(20),
    conta varchar(30),
    saldo_inicial numeric(18,4) NOT NULL DEFAULT 0,
    ativo boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id)
);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_movimentos_financeiros (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    conta_id uuid NOT NULL REFERENCES plantaopro.adm360_contas_financeiras(id),
    tipo varchar(10) NOT NULL CHECK(tipo IN ('ENTRADA', 'SAIDA')),
    valor numeric(18,4) NOT NULL CHECK(valor > 0),
    data_movimento date NOT NULL,
    descricao varchar(200) NOT NULL,
    origem_tipo varchar(30) NOT NULL,
    origem_id uuid NOT NULL,
    idempotency_key varchar(120),
    created_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, idempotency_key)
);

CREATE INDEX IF NOT EXISTS ix_adm360_movfin_tenant_conta ON plantaopro.adm360_movimentos_financeiros(tenant_id, conta_id, data_movimento);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_titulos_receber (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    venda_id uuid NOT NULL REFERENCES plantaopro.adm360_vendas(id),
    numero varchar(40) NOT NULL,
    pagador_id uuid NOT NULL,
    parcela integer NOT NULL DEFAULT 1,
    total_parcelas integer NOT NULL DEFAULT 1,
    data_emissao date NOT NULL,
    data_vencimento date NOT NULL,
    valor_principal numeric(18,4) NOT NULL CHECK(valor_principal > 0),
    valor_desconto numeric(18,4) NOT NULL DEFAULT 0 CHECK(valor_desconto >= 0),
    valor_juros numeric(18,4) NOT NULL DEFAULT 0 CHECK(valor_juros >= 0),
    valor_recebido numeric(18,4) NOT NULL DEFAULT 0 CHECK(valor_recebido >= 0),
    saldo_aberto numeric(18,4) NOT NULL CHECK(saldo_aberto >= 0),
    situacao varchar(20) NOT NULL DEFAULT 'ABERTO' CHECK(situacao IN ('ABERTO', 'PARCIAL', 'QUITADO', 'CANCELADO')),
    versao bigint NOT NULL DEFAULT 1,
    created_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, venda_id, parcela)
);

CREATE INDEX IF NOT EXISTS ix_adm360_titulos_tenant_venc ON plantaopro.adm360_titulos_receber(tenant_id, data_vencimento, situacao);
CREATE INDEX IF NOT EXISTS ix_adm360_titulos_pagador ON plantaopro.adm360_titulos_receber(tenant_id, pagador_id);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_titulo_baixas (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    titulo_id uuid NOT NULL REFERENCES plantaopro.adm360_titulos_receber(id),
    conta_id uuid NOT NULL REFERENCES plantaopro.adm360_contas_financeiras(id),
    data_recebimento date NOT NULL,
    valor_recebido numeric(18,4) NOT NULL CHECK(valor_recebido > 0),
    meio_pagamento varchar(30) NOT NULL,
    referencia text,
    movimento_financeiro_id uuid REFERENCES plantaopro.adm360_movimentos_financeiros(id),
    estornado boolean NOT NULL DEFAULT false,
    idempotency_key varchar(120),
    recebido_por uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, idempotency_key)
);

CREATE INDEX IF NOT EXISTS ix_adm360_baixas_tenant_tit ON plantaopro.adm360_titulo_baixas(tenant_id, titulo_id);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_titulo_estornos (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    baixa_id uuid NOT NULL REFERENCES plantaopro.adm360_titulo_baixas(id),
    titulo_id uuid NOT NULL REFERENCES plantaopro.adm360_titulos_receber(id),
    valor_estornado numeric(18,4) NOT NULL CHECK(valor_estornado > 0),
    motivo text NOT NULL,
    movimento_financeiro_id uuid REFERENCES plantaopro.adm360_movimentos_financeiros(id),
    idempotency_key varchar(120),
    estornado_por uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, baixa_id),
    UNIQUE(tenant_id, idempotency_key)
);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_comissoes_apropriadas (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    venda_id uuid NOT NULL REFERENCES plantaopro.adm360_vendas(id),
    baixa_id uuid REFERENCES plantaopro.adm360_titulo_baixas(id),
    vendedor_id uuid NOT NULL,
    base_calculo numeric(18,4) NOT NULL,
    percentual numeric(18,4) NOT NULL,
    valor_comissao numeric(18,4) NOT NULL,
    situacao varchar(20) NOT NULL DEFAULT 'APROPRIADA' CHECK(situacao IN ('PREVISTA', 'APROPRIADA', 'PAGA', 'ESTORNADA')),
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, baixa_id)
);

CREATE INDEX IF NOT EXISTS ix_adm360_comissoes_vendedor ON plantaopro.adm360_comissoes_apropriadas(tenant_id, vendedor_id, situacao);

-- Migration 2026_09_v2195_administrativo360_contas_pagar_fechamento.sql
DO $$
BEGIN
    ALTER TABLE plantaopro.adm360_contas_financeiras
    ADD COLUMN IF NOT EXISTS data_saldo_inicial date NOT NULL DEFAULT '2000-01-01';
EXCEPTION
    WHEN OTHERS THEN NULL;
END $$;

CREATE SEQUENCE IF NOT EXISTS plantaopro.adm360_titulo_pagar_numero START 1;

CREATE TABLE IF NOT EXISTS plantaopro.adm360_titulos_pagar (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    numero varchar(40) NOT NULL,
    origem_tipo varchar(30) NOT NULL CHECK(origem_tipo IN ('RECEBIMENTO_COMPRA', 'DESPESA_MANUAL', 'COMISSAO_VENDEDOR')),
    origem_id uuid,
    fornecedor_id uuid NOT NULL REFERENCES plantaopro.adm360_parceiros(id),
    documento varchar(80),
    competencia date NOT NULL,
    data_emissao date NOT NULL,
    data_vencimento date NOT NULL,
    parcela integer NOT NULL DEFAULT 1 CHECK(parcela >= 1),
    total_parcelas integer NOT NULL DEFAULT 1 CHECK(total_parcelas >= 1),
    valor_principal numeric(18,4) NOT NULL CHECK(valor_principal > 0),
    valor_desconto numeric(18,4) NOT NULL DEFAULT 0 CHECK(valor_desconto >= 0),
    valor_juros numeric(18,4) NOT NULL DEFAULT 0 CHECK(valor_juros >= 0),
    valor_pago numeric(18,4) NOT NULL DEFAULT 0 CHECK(valor_pago >= 0),
    saldo_aberto numeric(18,4) NOT NULL CHECK(saldo_aberto >= 0),
    centro_custo varchar(80),
    situacao varchar(25) NOT NULL DEFAULT 'PENDENTE_APROVACAO' CHECK(situacao IN ('PENDENTE_APROVACAO', 'APROVADO', 'PARCIAL', 'PAGO', 'CANCELADO')),
    observacoes text,
    aprovado_por uuid,
    aprovado_em timestamptz,
    idempotency_key varchar(120),
    versao bigint NOT NULL DEFAULT 1,
    created_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, numero),
    UNIQUE(tenant_id, idempotency_key)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_adm360_titulos_pagar_origem ON plantaopro.adm360_titulos_pagar(tenant_id, origem_tipo, origem_id, parcela) WHERE origem_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_adm360_titulos_pagar_fornecedor ON plantaopro.adm360_titulos_pagar(tenant_id, fornecedor_id, situacao);
CREATE INDEX IF NOT EXISTS ix_adm360_titulos_pagar_venc ON plantaopro.adm360_titulos_pagar(tenant_id, data_vencimento, situacao);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_titulo_pagamentos (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    titulo_id uuid NOT NULL REFERENCES plantaopro.adm360_titulos_pagar(id),
    conta_id uuid NOT NULL REFERENCES plantaopro.adm360_contas_financeiras(id),
    data_pagamento date NOT NULL,
    valor_pago numeric(18,4) NOT NULL CHECK(valor_pago > 0),
    meio_pagamento varchar(30) NOT NULL,
    referencia varchar(120),
    estornado boolean NOT NULL DEFAULT false,
    movimento_financeiro_id uuid REFERENCES plantaopro.adm360_movimentos_financeiros(id),
    idempotency_key varchar(120) NOT NULL,
    pago_por uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, idempotency_key)
);

CREATE INDEX IF NOT EXISTS ix_adm360_titpag_tenant_tit ON plantaopro.adm360_titulo_pagamentos(tenant_id, titulo_id);
CREATE INDEX IF NOT EXISTS ix_adm360_titpag_tenant_data ON plantaopro.adm360_titulo_pagamentos(tenant_id, data_pagamento);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_pagamento_estornos (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    pagamento_id uuid NOT NULL REFERENCES plantaopro.adm360_titulo_pagamentos(id),
    titulo_id uuid NOT NULL REFERENCES plantaopro.adm360_titulos_pagar(id),
    valor_estornado numeric(18,4) NOT NULL CHECK(valor_estornado > 0),
    motivo text NOT NULL,
    movimento_financeiro_id uuid REFERENCES plantaopro.adm360_movimentos_financeiros(id),
    estornado_por uuid,
    idempotency_key varchar(120) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, idempotency_key)
);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_caixa_fechamentos (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    conta_id uuid NOT NULL REFERENCES plantaopro.adm360_contas_financeiras(id),
    data_inicio date,
    data_fim date,
    data_fechamento date,
    saldo_abertura numeric(18,4) NOT NULL,
    total_entradas numeric(18,4) NOT NULL DEFAULT 0,
    total_saidas numeric(18,4) NOT NULL DEFAULT 0,
    entradas numeric(18,4) NOT NULL DEFAULT 0,
    saidas numeric(18,4) NOT NULL DEFAULT 0,
    saldo_calculado numeric(18,4) NOT NULL,
    saldo_conferido numeric(18,4) NOT NULL,
    diferenca numeric(18,4) NOT NULL,
    justificativa text,
    situacao varchar(20) NOT NULL DEFAULT 'FECHADO' CHECK(situacao IN ('FECHADO', 'REABERTO')),
    motivo_reabertura text,
    fechado_por uuid,
    reaberto_por uuid,
    fechado_em timestamptz NOT NULL DEFAULT now(),
    reaberto_em timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id)
);

DO $$
BEGIN
    ALTER TABLE plantaopro.adm360_titulo_pagamentos
        ADD COLUMN IF NOT EXISTS movimento_financeiro_id uuid REFERENCES plantaopro.adm360_movimentos_financeiros(id);

    ALTER TABLE plantaopro.adm360_pagamento_estornos
        ADD COLUMN IF NOT EXISTS movimento_financeiro_id uuid REFERENCES plantaopro.adm360_movimentos_financeiros(id);

    ALTER TABLE plantaopro.adm360_caixa_fechamentos
        ADD COLUMN IF NOT EXISTS data_inicio date,
        ADD COLUMN IF NOT EXISTS data_fim date,
        ADD COLUMN IF NOT EXISTS total_entradas numeric(18,4) NOT NULL DEFAULT 0,
        ADD COLUMN IF NOT EXISTS total_saidas numeric(18,4) NOT NULL DEFAULT 0,
        ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now();
EXCEPTION
    WHEN OTHERS THEN NULL;
END $$;

CREATE INDEX IF NOT EXISTS ix_adm360_caixa_fech_tenant_conta ON plantaopro.adm360_caixa_fechamentos(tenant_id, conta_id, data_fim);

DO $$
BEGIN
    ALTER TABLE plantaopro.adm360_comissoes_apropriadas
    ADD COLUMN IF NOT EXISTS titulo_pagar_id uuid REFERENCES plantaopro.adm360_titulos_pagar(id);

    ALTER TABLE plantaopro.adm360_comissoes_apropriadas
    DROP CONSTRAINT IF EXISTS adm360_comissoes_apropriadas_situacao_check;

    ALTER TABLE plantaopro.adm360_comissoes_apropriadas
    ADD CONSTRAINT adm360_comissoes_apropriadas_situacao_check
    CHECK(situacao IN ('APROPRIADA', 'A_PAGAR', 'PAGA', 'ESTORNADA', 'AJUSTE_PENDENTE'));
EXCEPTION
    WHEN OTHERS THEN NULL;
END $$;




