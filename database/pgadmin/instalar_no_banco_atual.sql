-- SQL puro para Query Tool do pgAdmin. Instala somente no banco selecionado.
DO $$ BEGIN
 IF current_database() = 'postgres' THEN
  RAISE EXCEPTION 'Este script instala as estruturas no banco atualmente selecionado. Para criar automaticamente o banco, utilize install-plantaopro.ps1 ou instalar_plantaopro.psql.';
 END IF;
END $$;
-- PlantãoPro - schema SQL puro para banco de destino já existente
-- Versão do schema: v1.95.1
-- PostgreSQL suportado: 16
-- Data de geração: 2026-08-20
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
