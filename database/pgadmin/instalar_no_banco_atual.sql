-- SQL puro para Query Tool do pgAdmin. Instala somente no banco selecionado.
DO $$ BEGIN
 IF current_database() = 'postgres' THEN
  RAISE EXCEPTION 'Este script instala as estruturas no banco atualmente selecionado. Para criar automaticamente o banco, utilize install-plantaopro.ps1 ou instalar_plantaopro.psql.';
 END IF;
END $$;
-- PlantãoPro - schema SQL puro para banco de destino já existente
-- Versão do schema: v2.21.9
-- PostgreSQL suportado: 16
-- Data de geração: 2026-10-15
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
-- SOURCE-SHA256: 99e4d2708231b7d2cea2569e1b40e1d7150bac865020871035a1ed2885bec7a5
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
-- SOURCE-SHA256: ef9a527339f5c4e65102f959113ab6df36eccb77740f7cd74fcbdc5434e89b84
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
-- SOURCE-SHA256: 73d5cf654c476093e56caf57ab42e9bbaed323ba299acc94cf7ebcc2fd21c51b
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
-- SOURCE-SHA256: 9a44e35d7a5fa32b1fea553c3c5a8be7e8e96b26d0b77c3ee70fc02b714ce931
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
-- SOURCE-SHA256: 1b8f162e309f6f7d618975610a30497ffed6314c2267669bf07af2d560523762
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
-- SOURCE-SHA256: e5f8c671337109984b3533657fd6336d829bd52d4a77a19ff21dc39cbee5a3d2
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
-- SOURCE-SHA256: 90357b7c1c8d3093091e3a8ec89b2a167f6281695a0e8568435b14c10ae0e255
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
-- SOURCE-SHA256: 577d21a163e63faa52beb0553caeca09fdae0c59c808dfa464c6037637da7747
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
    usuario_id uuid NULL,
    cliente_id uuid NULL,
    entidade varchar(100) NOT NULL DEFAULT 'SISTEMA',
    entidade_id uuid NULL,
    acao varchar(100) NOT NULL DEFAULT 'ACAO',
    detalhes jsonb NULL,
    sucesso boolean NOT NULL DEFAULT true,
    ip_origem varchar(64) NULL,
    perfil varchar(80) NULL,
    user_agent text NULL,
    reg_date timestamptz NOT NULL DEFAULT now(),
    reg_status char(1) NOT NULL DEFAULT 'A'
);

-- Compatibilidade com instalações existentes que criaram a tabela com shape genérico:
-- adiciona as colunas de auditoria usadas pelo código em execução e preserva as
-- colunas genéricas legadas (MESMO shape final em nova instalação e em upgrade).
ALTER TABLE IF EXISTS plantaopro.auditoria_acoes_criticas
    ADD COLUMN IF NOT EXISTS usuario_id uuid NULL,
    ADD COLUMN IF NOT EXISTS cliente_id uuid NULL,
    ADD COLUMN IF NOT EXISTS entidade varchar(100) NOT NULL DEFAULT 'SISTEMA',
    ADD COLUMN IF NOT EXISTS entidade_id uuid NULL,
    ADD COLUMN IF NOT EXISTS acao varchar(100) NOT NULL DEFAULT 'ACAO',
    ADD COLUMN IF NOT EXISTS detalhes jsonb NULL,
    ADD COLUMN IF NOT EXISTS sucesso boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS ip_origem varchar(64) NULL,
    ADD COLUMN IF NOT EXISTS perfil varchar(80) NULL,
    ADD COLUMN IF NOT EXISTS user_agent text NULL,
    ADD COLUMN IF NOT EXISTS reg_date timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS reg_status char(1) NOT NULL DEFAULT 'A',
    ADD COLUMN IF NOT EXISTS tenant_id uuid NULL,
    ADD COLUMN IF NOT EXISTS codigo text NULL,
    ADD COLUMN IF NOT EXISTS nome text NULL,
    ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO',
    ADD COLUMN IF NOT EXISTS dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    ADD COLUMN IF NOT EXISTS criado_em timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS atualizado_em timestamptz NULL;

ALTER TABLE IF EXISTS plantaopro.auditoria_acoes_criticas
    ALTER COLUMN id SET DEFAULT gen_random_uuid();

CREATE INDEX IF NOT EXISTS ix_auditoria_acoes_criticas_cliente_data ON plantaopro.auditoria_acoes_criticas(cliente_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_auditoria_acoes_criticas_usuario_data ON plantaopro.auditoria_acoes_criticas(usuario_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_auditoria_acoes_criticas_entidade ON plantaopro.auditoria_acoes_criticas(entidade, entidade_id);
CREATE INDEX IF NOT EXISTS ix_auditoria_acoes_criticas_acao_data ON plantaopro.auditoria_acoes_criticas(acao, reg_date DESC);
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
    endpoint text NOT NULL DEFAULT '',
    metodo varchar(10) NOT NULL DEFAULT 'GET',
    method varchar(12) NOT NULL DEFAULT 'GET',
    status_code integer NOT NULL DEFAULT 0,
    sucesso boolean NOT NULL DEFAULT true,
    duracao_ms bigint NOT NULL DEFAULT 0,
    duration_ms bigint NOT NULL DEFAULT 0,
    usuario_id uuid NULL,
    cliente_id uuid NULL,
    email varchar(255) NULL,
    perfil varchar(80) NULL,
    ip_origem varchar(64) NULL,
    ip varchar(80) NULL,
    user_agent text NULL,
    query_string text NULL,
    erro text NULL,
    error_message text NULL,
    reg_date timestamptz NOT NULL DEFAULT now(),
    reg_status char(1) NOT NULL DEFAULT 'A'
);

-- Compatibilidade com instalações existentes que criaram a tabela com shape genérico:
-- adiciona as colunas de observabilidade usadas pelo middleware e pelos controllers
-- e preserva as colunas genéricas legadas (MESMO shape final em nova instalação e em upgrade).
ALTER TABLE IF EXISTS plantaopro.api_request_logs
    ADD COLUMN IF NOT EXISTS endpoint text NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS metodo varchar(10) NOT NULL DEFAULT 'GET',
    ADD COLUMN IF NOT EXISTS method varchar(12) NOT NULL DEFAULT 'GET',
    ADD COLUMN IF NOT EXISTS status_code integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS sucesso boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS duracao_ms bigint NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS duration_ms bigint NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS usuario_id uuid NULL,
    ADD COLUMN IF NOT EXISTS cliente_id uuid NULL,
    ADD COLUMN IF NOT EXISTS email varchar(255) NULL,
    ADD COLUMN IF NOT EXISTS perfil varchar(80) NULL,
    ADD COLUMN IF NOT EXISTS ip_origem varchar(64) NULL,
    ADD COLUMN IF NOT EXISTS ip varchar(80) NULL,
    ADD COLUMN IF NOT EXISTS user_agent text NULL,
    ADD COLUMN IF NOT EXISTS query_string text NULL,
    ADD COLUMN IF NOT EXISTS erro text NULL,
    ADD COLUMN IF NOT EXISTS error_message text NULL,
    ADD COLUMN IF NOT EXISTS reg_date timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS reg_status char(1) NOT NULL DEFAULT 'A',
    ADD COLUMN IF NOT EXISTS tenant_id uuid NULL,
    ADD COLUMN IF NOT EXISTS codigo text NULL,
    ADD COLUMN IF NOT EXISTS nome text NULL,
    ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO',
    ADD COLUMN IF NOT EXISTS dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    ADD COLUMN IF NOT EXISTS criado_em timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS atualizado_em timestamptz NULL;

ALTER TABLE IF EXISTS plantaopro.api_request_logs
    ALTER COLUMN id SET DEFAULT gen_random_uuid();

CREATE INDEX IF NOT EXISTS ix_api_request_logs_reg_date ON plantaopro.api_request_logs(reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_endpoint_data ON plantaopro.api_request_logs(endpoint, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_status_data ON plantaopro.api_request_logs(status_code, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_usuario_data ON plantaopro.api_request_logs(usuario_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_cliente_data ON plantaopro.api_request_logs(cliente_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_perfil_data ON plantaopro.api_request_logs(perfil, reg_date DESC);
CREATE TABLE IF NOT EXISTS plantaopro.api_error_logs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    endpoint text NULL,
    metodo varchar(10) NULL,
    method varchar(12) NOT NULL DEFAULT 'GET',
    status_code integer NULL,
    success boolean NOT NULL DEFAULT true,
    usuario_id uuid NULL,
    cliente_id uuid NULL,
    email varchar(255) NULL,
    perfil varchar(80) NULL,
    ip_origem varchar(64) NULL,
    ip varchar(80) NULL,
    user_agent text NULL,
    query_string text NULL,
    duration_ms bigint NOT NULL DEFAULT 0,
    mensagem text NOT NULL DEFAULT '',
    error_message text NULL,
    exception_type varchar(255) NULL,
    stack_trace text NULL,
    reg_date timestamptz NOT NULL DEFAULT now(),
    reg_status char(1) NOT NULL DEFAULT 'A'
);

-- Compatibilidade com instalações existentes que criaram a tabela com shape genérico:
-- adiciona as colunas de observabilidade usadas pelo middleware e pelos controllers
-- e preserva as colunas genéricas legadas (MESMO shape final em nova instalação e em upgrade).
ALTER TABLE IF EXISTS plantaopro.api_error_logs
    ADD COLUMN IF NOT EXISTS endpoint text NULL,
    ADD COLUMN IF NOT EXISTS metodo varchar(10) NULL,
    ADD COLUMN IF NOT EXISTS method varchar(12) NOT NULL DEFAULT 'GET',
    ADD COLUMN IF NOT EXISTS status_code integer NULL,
    ADD COLUMN IF NOT EXISTS success boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS usuario_id uuid NULL,
    ADD COLUMN IF NOT EXISTS cliente_id uuid NULL,
    ADD COLUMN IF NOT EXISTS email varchar(255) NULL,
    ADD COLUMN IF NOT EXISTS perfil varchar(80) NULL,
    ADD COLUMN IF NOT EXISTS ip_origem varchar(64) NULL,
    ADD COLUMN IF NOT EXISTS ip varchar(80) NULL,
    ADD COLUMN IF NOT EXISTS user_agent text NULL,
    ADD COLUMN IF NOT EXISTS query_string text NULL,
    ADD COLUMN IF NOT EXISTS duration_ms bigint NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS mensagem text NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS error_message text NULL,
    ADD COLUMN IF NOT EXISTS exception_type varchar(255) NULL,
    ADD COLUMN IF NOT EXISTS stack_trace text NULL,
    ADD COLUMN IF NOT EXISTS reg_date timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS reg_status char(1) NOT NULL DEFAULT 'A',
    ADD COLUMN IF NOT EXISTS tenant_id uuid NULL,
    ADD COLUMN IF NOT EXISTS codigo text NULL,
    ADD COLUMN IF NOT EXISTS nome text NULL,
    ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO',
    ADD COLUMN IF NOT EXISTS dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    ADD COLUMN IF NOT EXISTS criado_em timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS atualizado_em timestamptz NULL;

ALTER TABLE IF EXISTS plantaopro.api_error_logs
    ALTER COLUMN id SET DEFAULT gen_random_uuid();

CREATE INDEX IF NOT EXISTS ix_api_error_logs_reg_date ON plantaopro.api_error_logs(reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_error_logs_status_data ON plantaopro.api_error_logs(status_code, reg_date DESC);
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
-- SOURCE-SHA256: bbaad4c4aa3eb5cb5befb0ac425c6addafc7ee4280dd3d025a1abe6c5a62cd9d
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
-- SOURCE-SHA256: ba92ad304072b85e769d690d5d1189bf89f6b94759f05f0fa4b1c7fcf8c4eeb0
-- Constraints canônicas complementares são mantidas idempotentes nas respectivas seções.
SET search_path TO plantaopro, public;

-- SOURCE: database/schema/090_indexes.sql
-- SOURCE-SHA256: 4c5a26dce844262569b5a5962f7bdd68b7fa763cccb0786583c441cc7c78e613
-- Índices canônicos complementares são mantidos idempotentes nas respectivas seções.
SET search_path TO plantaopro, public;

-- SOURCE: database/schema/100_reference_data.sql
-- SOURCE-SHA256: f8f03c725fcde4386371191121cb8eff14986e521ed914fe1df0c293d75d46ef
-- Dados referenciais mínimos sem credenciais fixas.
INSERT INTO plantaopro.politicas_senha(tenant_id)
SELECT NULL WHERE NOT EXISTS (SELECT 1 FROM plantaopro.politicas_senha WHERE tenant_id IS NULL AND reg_status='A');

-- SOURCE: database/schema/110_implantacao_go_live.sql
-- SOURCE-SHA256: 5b7b17163859dcec4a3692f0cc74fd93ad483a6b18fa121de17ffdfe5888c980
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
-- SOURCE-SHA256: 2ed1d710551e69ff287ce2377d2ebdced8333b9236f969da6ccd03d687fbd18c
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
-- SOURCE-SHA256: 1fd35e5019083382e4d11038006685b47cd898bd18f359f8b9a3b7608c00fe5e
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
-- SOURCE-SHA256: da8e47e86a7cfc2f7607ea2dcea98f2ab6992f3619d69c9beaecd95019aa60b6
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
-- SOURCE-SHA256: 5b07ca155fe5d588f683c49f6682b2eb860d9d6c2dababc872dfdbb726bb8f32
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
-- SOURCE-SHA256: f96f73e12ff6e9c528de10d15ec461b5ebde658472f8b473701ce5c86e588e53
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
-- SOURCE-SHA256: ca01dae4c1382100894795ceb3d4b08816a6b5301c21d20b0a82f53965c39a4f
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
-- SOURCE-SHA256: 428ca98b9ccfe372db5a02cdffea70d95310e48e8b5f76e000bc60bc1a3680ad
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
-- SOURCE-SHA256: 5d5bca4c43e49330dbc76a917e5f171ea6133ea6c9fa171956295831d94383cd
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
-- SOURCE-SHA256: e4599069e2f876b26f0ca87d13540e6317fbd3c0675ba698e801a21999f74e11
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
-- SOURCE-SHA256: 538ebe8b1aae7d858a8fc889b2821dcdb89f8164aea4ec4835f1c285211e90ee
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
-- SOURCE-SHA256: af80130879cdf55d6092666251163d89bc42329d20f00679c558358cb0ba8924
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
-- SOURCE-SHA256: 2210cfdbbb7e5d9921f4592508c034579f396aab098c25a5b0365a5bdfb67e3e
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
-- SOURCE-SHA256: 770d7f5090616789e9865b480c4a17ff7825bc7bd0818038236935dbc92a0fa0
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
-- SOURCE-SHA256: d0e0c306aaa29558f8e79c387b508ee914e4e168e10830f5d51ef028c76824ed
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
-- SOURCE-SHA256: 2d3b02e7d018c0f5678d04adf1fa472cf43b9db3ea32d6b31257dbcbc98bdec4
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
-- SOURCE-SHA256: fec7f151cf00d47f6b72009773d8bd211d36eec68a39d18daecc9be333bcbfa7
INSERT INTO plantaopro.politicas_senha(id,tenant_id,tamanho_minimo,exige_maiuscula,exige_minuscula,exige_numero,exige_especial,expiracao_dias,tentativas_permitidas,bloqueio_minutos,reg_status)
SELECT md5('password-policy:global')::uuid,NULL,12,true,true,true,true,90,5,15,'A'
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.politicas_senha WHERE tenant_id IS NULL AND reg_status='A');

-- SOURCE: database/seeds/system/070_planos_recursos.sql
-- SOURCE-SHA256: db79ae7ca1184b4e968acf6bc8b94326de9391769f88281ea753c137407cdb1d
-- O runtime aceita catálogo comercial vazio; nenhum cliente ou assinatura fictícia é criado.
SELECT 1 AS catalogo_comercial_opcional;

-- SOURCE: database/seeds/system/080_parametros_globais.sql
-- SOURCE-SHA256: 1e1ffe5d74b6f9fa2a6a210d0ef93c4853103447ad929ff568a1ef0cd511492d
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
-- SOURCE-SHA256: 8131520e5a8419ec144e4341f6e4d502d2c08fc600e3a73022352fe6d6a09c0c
-- Catálogo notificacoes: estruturas canônicas aceitam estado vazio e configuração posterior.
SELECT 1 AS seed_090_notificacoes;

-- SOURCE: database/seeds/system/100_status_operacionais.sql
-- SOURCE-SHA256: 80db8de1ece47e4e865653963f6c06dea96a25eb124dc200f356d4a08510fdf8
-- Catálogo status_operacionais: estruturas canônicas aceitam estado vazio e configuração posterior.
SELECT 1 AS seed_100_status_operacionais;

-- SOURCE: database/seeds/system/110_configuracoes_runtime.sql
-- SOURCE-SHA256: b5cf4e3cbe7ca237a50a4da5ff5312f7f34f7bf8d499ee94f0cf0a068e608728
-- Checkpoint de schema concluído somente após todas as estruturas e seeds anteriores.
INSERT INTO plantaopro.schema_migrations(id,versao,nome,script_path,checksum,iniciado_em,applied_at,aplicado_em,duracao_ms,status,executado_por,ambiente)
SELECT 'v1.95.1','v1.95.1','One-click database runtime-ready','database/install-manifest.json','manifest-managed',now(),now(),now(),0,'APLICADA',current_user,'INSTALL'
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.schema_migrations WHERE id='v1.95.1');

-- ============================================================
-- Seção 19 — Produto operacional premium v1.40.0
-- ============================================================

-- SOURCE: database/schema/260_v1400_produto_operacional_premium.sql
-- SOURCE-SHA256: 9c3753a41daa3dc35fefba19136a6570fc03737f11923e37bebeb2af796096d9
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
-- SOURCE-SHA256: 2a1ada757d85915453fcb087e9cee34d51e9c23246ff97013b7336567fb256c7
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
-- SOURCE-SHA256: 393eafa115f81ea8c194246160181bc7ab2380358102bf09d231ad6e13ccf4c1
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
-- SOURCE-SHA256: c2d261f333bb2c851b2f6ff7c3389cbc6e380d661af4e178d0d63848761a6f22
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
-- SOURCE-SHA256: 123060b679befe444a53d310ac7826b5ad52e4235f523d2c467b642ef0332995
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
-- SOURCE-SHA256: da82a5dae658af21bb823287f0083e543c5b34b633978e8eb8e71836288dc8d1
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
-- SOURCE-SHA256: 54ae03b820c6c72ab033826a5802013f031867aa41b5cdf757460178b594113c
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
-- SOURCE-SHA256: 735c05760d06f694028ba5c4cac48ecbd3e5e4e043d0d9fb086a2e6e524e484e
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
-- SOURCE-SHA256: 63d2785de452db67bac40f632703e2587153ec66872b5a00c9803dacd58e5c33
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
-- SOURCE-SHA256: 47538fda9eb0bd85a8acaacfd49451fe9823e50ec4e382b2ec810eeda054b168
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
-- SOURCE-SHA256: d99c9b76b7dcebaf09280d776e60bdba02806ef98442c06268b034156d23052a
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
-- SOURCE-SHA256: a9c2502b6503e1e6ff266a448f47ab6c56fb919998cd259a0e675c3732678fad
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
-- SOURCE-SHA256: dee352f4f088fa6c32711141bd764e4b21307cfd708af05e8568b7cdd60d9a9f
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
-- SOURCE-SHA256: 1a2abecd0e695800e42a15e9edf75ee414b0f42c32eef9d8ee1e96a8ec7f56ce
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
-- SOURCE-SHA256: f1e993b5938d07adda4d50828c931e4983e5a014350039f8d6a058d97f6671d1
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
-- SOURCE-SHA256: 4b7297bd3463183d090841541753447b283688faca2c9b015e8426a35e3e3ced
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
-- SOURCE-SHA256: 1a141a2b72a52295bdde7184b2f3d72a0d909101cfbf29b56edee9169c6ca29e
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
-- SOURCE-SHA256: 5001f77ff9a495c39781aa96208e34b7feed96795dc588aa67a09a526eaa67b2
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
-- SOURCE-SHA256: d430e8c077ca15afeb28241e901a5a2e3a78e5b7a7cef47c11a8d7ec467893ce
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
-- SOURCE-SHA256: f4b2a2e42a269d66e225d1abe4844147ffeab1252662c8a3f3ace42964d76f2f
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
-- SOURCE-SHA256: 0db11d50370573b628551e33fa9843fca7ded84bcec2f642e6ca1452e432678b
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
-- SOURCE-SHA256: 36b30cbaf3701d0cac4676be17df8907c3d8bf3bfb1e2553bee0a268973dc574
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
-- SOURCE-SHA256: b68c28e1e0629ad7f1429e11ae58195049d62bd07f2b54ef038a5b820cd86057
-- Administrativo 360 bloco 2: suprimentos, qualidade, estoque e coleta.
CREATE SEQUENCE IF NOT EXISTS plantaopro.adm360_pedido_numero;
CREATE TABLE IF NOT EXISTS plantaopro.adm360_parceiros(id uuid primary key default gen_random_uuid(),tenant_id uuid not null references plantaopro.tenants(id),nome varchar(160) not null,documento varchar(20),fornecedor boolean not null default false,ativo boolean not null default true,created_at timestamptz not null default now(),unique(tenant_id,id));
CREATE UNIQUE INDEX IF NOT EXISTS ux_adm360_parceiro_documento ON plantaopro.adm360_parceiros(tenant_id,documento) WHERE documento IS NOT NULL;
CREATE TABLE IF NOT EXISTS plantaopro.adm360_produtos(id uuid primary key default gen_random_uuid(),tenant_id uuid not null references plantaopro.tenants(id),sku varchar(40) not null,nome varchar(180) not null,unidade varchar(12) not null,codigo_barras varchar(80),controla_lote boolean not null default false,controla_serie boolean not null default false,exige_inspecao boolean not null default true,ativo boolean not null default true,created_at timestamptz not null default now(),unique(tenant_id,id),unique(tenant_id,sku));
CREATE UNIQUE INDEX IF NOT EXISTS ux_adm360_produto_barcode ON plantaopro.adm360_produtos(tenant_id,codigo_barras) WHERE codigo_barras IS NOT NULL;
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
-- Seção 56 — Administrativo360 Regras Estoque Orcamentos
-- ============================================================

-- SOURCE: database/migrations/2026_09_v2192_administrativo360_regras_estoque_orcamentos.sql
-- SOURCE-SHA256: b5cc310b22733357834e1c566fe61425f7007d74a9a542749aaaaa4e1fc2f612
-- Administrativo 360 bloco 3: Idempotência de operações, inventário por condição e orçamentos cirúrgicos com reservas.

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

-- Ajuste de granularidade de inventário por condição
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

-- Orçamentos Cirúrgicos
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
-- Seção 57 — Administrativo360 Cirurgia Vales Reconciliacao
-- ============================================================

-- SOURCE: database/migrations/2026_09_v2193_administrativo360_cirurgia_vales_reconciliacao.sql
-- SOURCE-SHA256: 6442e40d9cf12b1bd48a6c0d75a48da889a688d424a3fac328e4c7c50abd695e
-- Administrativo 360 bloco 4: Cirurgias operacionais, Vales de consignação, Expedição, Consumo/Retorno e Reconciliação.

CREATE SEQUENCE IF NOT EXISTS plantaopro.adm360_cirurgia_numero;
CREATE SEQUENCE IF NOT EXISTS plantaopro.adm360_vale_numero;

-- Vinculação de item de orçamento e situação em reservas
ALTER TABLE plantaopro.adm360_reservas ADD COLUMN IF NOT EXISTS orcamento_item_id uuid;

DO $$
BEGIN
    ALTER TABLE plantaopro.adm360_reservas DROP CONSTRAINT IF EXISTS adm360_reservas_situacao_check;
    ALTER TABLE plantaopro.adm360_reservas DROP CONSTRAINT IF EXISTS chk_adm360_reservas_situacao;
    ALTER TABLE plantaopro.adm360_reservas ADD CONSTRAINT chk_adm360_reservas_situacao CHECK(situacao IN ('ATIVA', 'CONSUMIDA', 'ATENDIDA', 'CANCELADA'));
EXCEPTION
    WHEN OTHERS THEN NULL;
END $$;

-- Cirurgias Operacionais
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

-- Vales de Consignação
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

-- Itens do Vale de Consignação
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

-- Eventos do Vale: Consumo, Retorno, Perda, Avaria
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

-- ============================================================
-- Seção 58 — Administrativo360 Valorizacao Vendas Financeiro
-- ============================================================

-- SOURCE: database/migrations/2026_09_v2194_administrativo360_valorizacao_vendas_financeiro.sql
-- SOURCE-SHA256: 26243e62271f5b1ecab4d9fc1b95e80224d2149d70c029272cfbfc2b088b6d2d
-- Migration 2026_09_v2194_administrativo360_valorizacao_vendas_financeiro.sql
-- Jornada: Vale Reconciliado -> Valorização -> Venda Interna -> Contas a Receber -> Recebimento Manual -> Estorno -> Comissão e Fluxo de Caixa

-- 1. Suporte a inspeção de retorno de consignação na Qualidade
DO $$
BEGIN
    ALTER TABLE plantaopro.adm360_inspecoes ALTER COLUMN recebimento_item_id DROP NOT NULL;
    ALTER TABLE plantaopro.adm360_inspecoes ADD COLUMN IF NOT EXISTS retorno_evento_id uuid REFERENCES plantaopro.adm360_vale_eventos(id);
    ALTER TABLE plantaopro.adm360_inspecoes ADD COLUMN IF NOT EXISTS origem_tipo varchar(30) NOT NULL DEFAULT 'RECEBIMENTO';
    ALTER TABLE plantaopro.adm360_vale_eventos ADD COLUMN IF NOT EXISTS quantidade_decidida numeric(18,4) NOT NULL DEFAULT 0;
EXCEPTION
    WHEN OTHERS THEN NULL;
END $$;

-- 2. Custo histórico em produtos e lotes
DO $$
BEGIN
    ALTER TABLE plantaopro.adm360_produtos ADD COLUMN IF NOT EXISTS preco_custo numeric(18,4) NOT NULL DEFAULT 0;
    ALTER TABLE plantaopro.adm360_lotes ADD COLUMN IF NOT EXISTS custo_unitario numeric(18,4);
EXCEPTION
    WHEN OTHERS THEN NULL;
END $$;

-- 3. Sequência de número da venda interna
CREATE SEQUENCE IF NOT EXISTS plantaopro.adm360_venda_numero START 1;

-- 4. Valorização do Vale de Consignação
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

-- 5. Vendas Internas
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

-- 6. Contas Financeiras e Caixa
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

-- 7. Contas a Receber (Títulos, Baixas, Estornos)
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

-- 8. Comissões Apropriadas
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

-- ============================================================
-- Seção 59 — Administrativo360 Contas Pagar Fechamento
-- ============================================================

-- SOURCE: database/migrations/2026_09_v2195_administrativo360_contas_pagar_fechamento.sql
-- SOURCE-SHA256: 008797d3f103fe9b2cf867e89ff6c2fd01ba22665b73df953c9a65543455d81e
-- Migration 2026_09_v2195_administrativo360_contas_pagar_fechamento.sql
-- Jornada: Compra/Recebimento -> Contas a Pagar -> Pagamento Manual -> Estorno -> Caixa Consolidado -> Fechamento

DO $$
BEGIN
    ALTER TABLE plantaopro.adm360_contas_financeiras
    ADD COLUMN IF NOT EXISTS data_saldo_inicial date NOT NULL DEFAULT '2000-01-01';
EXCEPTION
    WHEN OTHERS THEN NULL;
END $$;

-- 1. Sequência para títulos a pagar
CREATE SEQUENCE IF NOT EXISTS plantaopro.adm360_titulo_pagar_numero START 1;

-- 2. Tabela de Contas a Pagar (Obrigações)
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

-- 3. Tabela de Pagamentos Manuais (Baixas de AP)
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

-- 4. Tabela de Estornos de Pagamento
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

-- 5. Fechamento de Caixa por Conta e Período/Data
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

-- 6. Suporte a comissão vinculada a título a pagar
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

-- ============================================================
-- Seção 60 — Administrativo360 Cotacoes Xml Dashboard
-- ============================================================

-- SOURCE: database/migrations/2026_09_v2196_administrativo360_cotacoes_xml_dashboard.sql
-- SOURCE-SHA256: 1a61b839c29de84ccfbf0db263698a668eaee55033d8929f4d4eaaa3f6744885
-- Migration 2026_09_v2196_administrativo360_cotacoes_xml_dashboard.sql
-- Ampliação do Administrativo 360 do PlantãoPro:
-- 1. Estabelecimentos e Capacidades Contratadas (Portais de Cotação, XML Recebidos, Dashboard Gerencial)
-- 2. Conexões de Portais de Cotação (OPMENEXO, INPART Saúde, Importação Manual)
-- 3. Captação de Cotações, Itens e Anexos
-- 4. Relacionamento de Dados (De/Para de Hospitais, Médicos, Unidades, Produtos)
-- 5. Outbox de Respostas de Cotação e Orçamento Vinculado
-- 6. Sincronização DF-e e Documentos XML Recebidos (NF-e modelo 55), Itens e Eventos

-- 1. Estabelecimentos (Unidades / CNPJs autorizados no Tenant)
CREATE TABLE IF NOT EXISTS plantaopro.adm360_estabelecimentos (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    cnpj varchar(14) NOT NULL,
    razao_social varchar(160) NOT NULL,
    nome_fantasia varchar(160),
    inscricao_estadual varchar(30),
    cnae varchar(20),
    ambiente varchar(20) NOT NULL DEFAULT 'HOMOLOGACAO' CHECK(ambiente IN ('HOMOLOGACAO', 'PRODUCAO')),
    ativo boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, cnpj)
);

CREATE INDEX IF NOT EXISTS ix_adm360_estab_tenant_cnpj ON plantaopro.adm360_estabelecimentos(tenant_id, cnpj);

-- 2. Capacidades Contratadas por Tenant
CREATE TABLE IF NOT EXISTS plantaopro.adm360_capacidades_contratadas (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    capacidade varchar(40) NOT NULL CHECK(capacidade IN ('PORTAIS_COTACAO', 'XML_RECEBIDOS', 'DASHBOARD_GERENCIAL')),
    habilitado boolean NOT NULL DEFAULT true,
    ativado_em timestamptz NOT NULL DEFAULT now(),
    configuracoes jsonb NOT NULL DEFAULT '{}'::jsonb,
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, capacidade)
);

-- 3. Contas de Conexão com Portais Externos (OPMENEXO, INPART, Importação Manual)
CREATE TABLE IF NOT EXISTS plantaopro.adm360_portal_contas (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    estabelecimento_id uuid NOT NULL REFERENCES plantaopro.adm360_estabelecimentos(id),
    provedor varchar(30) NOT NULL CHECK(provedor IN ('OPMENEXO', 'INPART', 'IMPORTACAO_MANUAL')),
    nome_conta varchar(100) NOT NULL,
    identificador_externo varchar(100),
    usuario_acesso varchar(100),
    segredo_referencia text,
    ambiente varchar(20) NOT NULL DEFAULT 'HOMOLOGACAO' CHECK(ambiente IN ('HOMOLOGACAO', 'PRODUCAO')),
    status_integracao varchar(30) NOT NULL DEFAULT 'NAO_CONFIGURADA' CHECK(status_integracao IN ('NAO_CONFIGURADA', 'CONFIGURADA', 'BLOQUEADA', 'ERRO_AUTENTICACAO')),
    motivo_bloqueio text,
    ultima_sincronizacao timestamptz,
    ativo boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, provedor, identificador_externo)
);

CREATE INDEX IF NOT EXISTS ix_adm360_pcontas_tenant_prov ON plantaopro.adm360_portal_contas(tenant_id, provedor, status_integracao);

-- 4. De/Para de Dados e Cadastros (Hospitais, Médicos, Unidades, Produtos)
CREATE TABLE IF NOT EXISTS plantaopro.adm360_mapeamentos_de_para (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    portal_conta_id uuid REFERENCES plantaopro.adm360_portal_contas(id),
    provedor varchar(30) NOT NULL,
    tipo_entidade varchar(30) NOT NULL CHECK(tipo_entidade IN ('HOSPITAL', 'SOLICITANTE', 'MEDICO', 'OPERADORA', 'PAGADOR', 'UNIDADE_MEDIDA', 'PRODUTO')),
    codigo_externo varchar(100) NOT NULL,
    descricao_externa varchar(250) NOT NULL,
    entidade_interna_id uuid,
    entidade_interna_descricao varchar(250) NOT NULL,
    fator_conversao numeric(18,4) NOT NULL DEFAULT 1.0000,
    situacao varchar(20) NOT NULL DEFAULT 'ATIVO' CHECK(situacao IN ('ATIVO', 'INATIVO')),
    criado_por uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, provedor, tipo_entidade, codigo_externo)
);

CREATE INDEX IF NOT EXISTS ix_adm360_depara_tenant_tipo ON plantaopro.adm360_mapeamentos_de_para(tenant_id, provedor, tipo_entidade, codigo_externo);

-- 5. Cotações Pré-Cirúrgicas Captadas
CREATE TABLE IF NOT EXISTS plantaopro.adm360_cotacoes (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    estabelecimento_id uuid NOT NULL REFERENCES plantaopro.adm360_estabelecimentos(id),
    portal_conta_id uuid NOT NULL REFERENCES plantaopro.adm360_portal_contas(id),
    provedor varchar(30) NOT NULL,
    identificador_externo varchar(100) NOT NULL,
    revisao_externa integer NOT NULL DEFAULT 1,
    hospital_id uuid REFERENCES plantaopro.adm360_parceiros(id),
    hospital_solicitante_externo varchar(200),
    paciente_iniciais varchar(50),
    procedimento varchar(250),
    data_prevista date,
    prazo_resposta timestamptz NOT NULL,
    fuso_horario varchar(40) NOT NULL DEFAULT 'America/Sao_Paulo',
    status_interno varchar(30) NOT NULL DEFAULT 'RECEBIDA' CHECK(status_interno IN ('RECEBIDA', 'EM_RELACIONAMENTO', 'EM_ORCAMENTO', 'AGUARDANDO_APROVACAO', 'PRONTA_PARA_ENVIO', 'RESPONDIDA', 'CANCELADA', 'EXPIRADA')),
    status_externo varchar(40) NOT NULL DEFAULT 'ABERTA',
    origem varchar(30) NOT NULL DEFAULT 'PORTAL_OFICIAL' CHECK(origem IN ('PORTAL_OFICIAL', 'IMPORTACAO_MANUAL', 'DADOS_DE_TESTE')),
    orcamento_id uuid REFERENCES plantaopro.adm360_orcamentos(id),
    payload_original text NOT NULL DEFAULT '{}',
    idempotency_key varchar(120),
    versao bigint NOT NULL DEFAULT 1,
    capturada_em timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, portal_conta_id, identificador_externo, revisao_externa)
);

CREATE INDEX IF NOT EXISTS ix_adm360_cot_tenant_prazo ON plantaopro.adm360_cotacoes(tenant_id, prazo_resposta, status_interno);
CREATE INDEX IF NOT EXISTS ix_adm360_cot_tenant_ext ON plantaopro.adm360_cotacoes(tenant_id, provedor, identificador_externo);

-- 6. Itens da Cotação
CREATE TABLE IF NOT EXISTS plantaopro.adm360_cotacao_itens (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    cotacao_id uuid NOT NULL REFERENCES plantaopro.adm360_cotacoes(id) ON DELETE CASCADE,
    numero_item integer NOT NULL,
    codigo_externo varchar(80),
    descricao_externa varchar(250) NOT NULL,
    fabricante_externo varchar(120),
    modelo_externo varchar(120),
    quantidade_solicitada numeric(18,4) NOT NULL CHECK(quantidade_solicitada > 0),
    unidade_solicitada varchar(30) NOT NULL,
    produto_id uuid REFERENCES plantaopro.adm360_produtos(id),
    unidade_interna varchar(30),
    fator_conversao numeric(18,4) NOT NULL DEFAULT 1.0000 CHECK(fator_conversao > 0),
    quantidade_convertida numeric(18,4) NOT NULL DEFAULT 0,
    preco_unitario_ofertado numeric(18,4) NOT NULL DEFAULT 0 CHECK(preco_unitario_ofertado >= 0),
    desconto numeric(18,4) NOT NULL DEFAULT 0 CHECK(desconto >= 0),
    preco_total_ofertado numeric(18,4) NOT NULL DEFAULT 0 CHECK(preco_total_ofertado >= 0),
    material_ofertado varchar(250),
    justificativa_substituicao text,
    status_relacionamento varchar(30) NOT NULL DEFAULT 'PENDENTE' CHECK(status_relacionamento IN ('PENDENTE', 'RELACIONADO', 'NAO_ATENDIDO')),
    motivo_nao_atendimento text,
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, cotacao_id, numero_item)
);

-- 7. Anexos de Cotação
CREATE TABLE IF NOT EXISTS plantaopro.adm360_cotacao_anexos (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    cotacao_id uuid NOT NULL REFERENCES plantaopro.adm360_cotacoes(id) ON DELETE CASCADE,
    nome_arquivo varchar(250) NOT NULL,
    tamanho_bytes bigint NOT NULL CHECK(tamanho_bytes >= 0),
    content_type varchar(100) NOT NULL,
    sha256_hash varchar(64) NOT NULL,
    conteudo bytea,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id)
);

-- 8. Fila Outbox de Respostas de Cotação
CREATE TABLE IF NOT EXISTS plantaopro.adm360_cotacao_respostas (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    cotacao_id uuid NOT NULL REFERENCES plantaopro.adm360_cotacoes(id),
    orcamento_id uuid NOT NULL REFERENCES plantaopro.adm360_orcamentos(id),
    revisao integer NOT NULL DEFAULT 1,
    snapshot_proposta jsonb NOT NULL,
    status_transmissao varchar(30) NOT NULL DEFAULT 'NA_FILA' CHECK(status_transmissao IN ('NA_FILA', 'ENVIANDO', 'ACEITA_PELO_PORTAL', 'REJEITADA_PELO_PORTAL', 'RESULTADO_DESCONHECIDO')),
    status_comercial_externo varchar(40) NOT NULL DEFAULT 'AGUARDANDO_DECISAO' CHECK(status_comercial_externo IN ('AGUARDANDO_DECISAO', 'VENCEDORA', 'PERDIDA', 'CANCELADA_PORTAL')),
    tentativas integer NOT NULL DEFAULT 0,
    max_tentativas integer NOT NULL DEFAULT 3,
    proxima_tentativa timestamptz NOT NULL DEFAULT now(),
    protocolo_externo varchar(120),
    mensagem_retorno text,
    aprovado_por uuid,
    aprovado_em timestamptz,
    enviado_em timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id)
);

CREATE INDEX IF NOT EXISTS ix_adm360_respcot_tenant_status ON plantaopro.adm360_cotacao_respostas(tenant_id, status_transmissao, proxima_tentativa);

-- 9. Sincronização DF-e (NFeDistribuicaoDFe)
CREATE TABLE IF NOT EXISTS plantaopro.adm360_dfe_sincronizacoes (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    estabelecimento_id uuid NOT NULL REFERENCES plantaopro.adm360_estabelecimentos(id),
    cnpj varchar(14) NOT NULL,
    ambiente varchar(20) NOT NULL DEFAULT 'HOMOLOGACAO' CHECK(ambiente IN ('HOMOLOGACAO', 'PRODUCAO')),
    provedor varchar(30) NOT NULL DEFAULT 'SEFAZ_DISTRIBUICAO_DFE',
    ultimo_nsu varchar(20) NOT NULL DEFAULT '0',
    max_nsu varchar(20) NOT NULL DEFAULT '0',
    data_consulta timestamptz,
    proxima_consulta_permitida timestamptz NOT NULL DEFAULT now(),
    status varchar(30) NOT NULL DEFAULT 'PRONTO' CHECK(status IN ('PRONTO', 'SINCRONIZANDO', 'ERRO', 'AGUARDANDO_INTERVALO')),
    mensagem_erro text,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, estabelecimento_id, ambiente)
);

-- 10. Documentos Fiscais Recebidos (NF-e modelo 55)
CREATE TABLE IF NOT EXISTS plantaopro.adm360_documentos_recebidos (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    estabelecimento_id uuid NOT NULL REFERENCES plantaopro.adm360_estabelecimentos(id),
    chave_acesso varchar(44) NOT NULL,
    numero varchar(20) NOT NULL,
    serie varchar(10) NOT NULL,
    modelo varchar(5) NOT NULL DEFAULT '55',
    data_emissao timestamptz NOT NULL,
    emitente_cnpj varchar(14) NOT NULL,
    emitente_nome varchar(160) NOT NULL,
    destinatario_cnpj varchar(14) NOT NULL,
    destinatario_nome varchar(160) NOT NULL,
    valor_total numeric(18,4) NOT NULL DEFAULT 0 CHECK(valor_total >= 0),
    valor_produtos numeric(18,4) NOT NULL DEFAULT 0 CHECK(valor_produtos >= 0),
    tipo_documento varchar(20) NOT NULL DEFAULT 'NFE_COMPLETA' CHECK(tipo_documento IN ('NFE_COMPLETA', 'RESUMO')),
    status_manifestacao varchar(35) NOT NULL DEFAULT 'SEM_MANIFESTACAO' CHECK(status_manifestacao IN ('SEM_MANIFESTACAO', 'CIENCIA_DA_OPERACAO', 'CONFIRMACAO_DA_OPERACAO', 'DESCONHECIMENTO', 'OPERACAO_NAO_REALIZADA')),
    status_conferencia varchar(25) NOT NULL DEFAULT 'PENDENTE' CHECK(status_conferencia IN ('PENDENTE', 'CONFERIDO', 'VINCULADO', 'DIVERGENTE')),
    pedido_id uuid REFERENCES plantaopro.adm360_pedidos(id),
    recebimento_id uuid REFERENCES plantaopro.adm360_recebimentos(id),
    titulo_pagar_id uuid REFERENCES plantaopro.adm360_titulos_pagar(id),
    xml_conteudo text NOT NULL,
    xml_hash varchar(64) NOT NULL,
    nsu varchar(20),
    quarentena boolean NOT NULL DEFAULT false,
    motivo_quarentena text,
    origem varchar(30) NOT NULL DEFAULT 'IMPORTACAO_MANUAL' CHECK(origem IN ('IMPORTACAO_MANUAL', 'DFE_SINCRONIZACAO', 'DADOS_DE_TESTE')),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, chave_acesso)
);

CREATE INDEX IF NOT EXISTS ix_adm360_docrec_tenant_chave ON plantaopro.adm360_documentos_recebidos(tenant_id, chave_acesso);
CREATE INDEX IF NOT EXISTS ix_adm360_docrec_tenant_status ON plantaopro.adm360_documentos_recebidos(tenant_id, status_conferencia, quarentena);

-- 11. Itens do Documento Fiscal Recebido
CREATE TABLE IF NOT EXISTS plantaopro.adm360_documento_itens (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    documento_id uuid NOT NULL REFERENCES plantaopro.adm360_documentos_recebidos(id) ON DELETE CASCADE,
    numero_item integer NOT NULL,
    codigo_produto_emitente varchar(60) NOT NULL,
    descricao_produto_emitente varchar(250) NOT NULL,
    ncm varchar(10),
    cfop varchar(10),
    unidade_comercial varchar(20) NOT NULL,
    quantidade_comercial numeric(18,4) NOT NULL CHECK(quantidade_comercial > 0),
    valor_unitario numeric(18,4) NOT NULL CHECK(valor_unitario >= 0),
    valor_total numeric(18,4) NOT NULL CHECK(valor_total >= 0),
    produto_id uuid REFERENCES plantaopro.adm360_produtos(id),
    unidade_interna varchar(20),
    fator_conversao numeric(18,4) NOT NULL DEFAULT 1.0000,
    quantidade_convertida numeric(18,4) NOT NULL DEFAULT 0,
    conferido boolean NOT NULL DEFAULT false,
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, documento_id, numero_item)
);

-- 12. Eventos e Histórico do Documento Fiscal
CREATE TABLE IF NOT EXISTS plantaopro.adm360_documento_eventos (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    documento_id uuid NOT NULL REFERENCES plantaopro.adm360_documentos_recebidos(id) ON DELETE CASCADE,
    tipo_evento varchar(40) NOT NULL,
    sequencia_evento integer NOT NULL DEFAULT 1,
    descricao_evento varchar(250) NOT NULL,
    data_evento timestamptz NOT NULL DEFAULT now(),
    protocolo varchar(60),
    detalhes text,
    registrado_por uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id)
);

-- ============================================================
-- Seção 61 — Reconciliação canônica de contratos de módulos v2.19.7
-- ============================================================

-- SOURCE: database/migrations/2026_09_v2197_reconciliar_contratos_modulos.sql
-- SOURCE-SHA256: 16432ef2198eb03d5b2c216da1cd97ad82af501d6d52f96035c7b7c7050df7ba
-- PlantãoPro v2.19.7 - contrato canônico e reconciliação segura de tenant_modulos.
-- Não remove dados: códigos sem correspondência ou ambíguos permanecem intactos e
-- são registrados em tenant_modulos_reconciliacao para correção administrativa.
set search_path to plantaopro, public;

do $migration$
begin
    if to_regclass('plantaopro.tenant_modulos') is null
       or to_regclass('plantaopro.modulos_sistema') is null then
        raise exception 'Pré-requisitos ausentes: tenant_modulos e modulos_sistema devem existir';
    end if;
end $migration$;

-- Acrescentar primeiro como nullable evita que o DEFAULT transforme contratos
-- cancelados/bloqueados legados em contratos habilitados durante o backfill.
alter table plantaopro.tenant_modulos add column if not exists modulo_id uuid null;
alter table plantaopro.tenant_modulos add column if not exists codigo text null;
alter table plantaopro.tenant_modulos add column if not exists codigo_modulo text null;
alter table plantaopro.tenant_modulos add column if not exists habilitado boolean null;
alter table plantaopro.tenant_modulos add column if not exists origem text null;
alter table plantaopro.tenant_modulos add column if not exists status text null;
alter table plantaopro.tenant_modulos add column if not exists reg_status char(1) null;
alter table plantaopro.tenant_modulos add column if not exists reg_date timestamptz null;
alter table plantaopro.tenant_modulos add column if not exists reg_update timestamptz null;
alter table plantaopro.tenant_modulos add column if not exists ativado_em timestamptz null;
alter table plantaopro.tenant_modulos add column if not exists desativado_em timestamptz null;
alter table plantaopro.tenant_modulos add column if not exists limite_contratado integer null;
alter table plantaopro.tenant_modulos add column if not exists preco_contratado numeric(14,2) null;
alter table plantaopro.tenant_modulos add column if not exists created_by uuid null;
alter table plantaopro.tenant_modulos add column if not exists updated_by uuid null;

-- Datas legadas são copiadas quando essas colunas existem; SQL dinâmico mantém a
-- migration compatível com as duas famílias de schema encontradas no repositório.
do $dates$
begin
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='tenant_modulos' and column_name='criado_em') then
        execute 'update plantaopro.tenant_modulos set reg_date=coalesce(reg_date,criado_em) where reg_date is null';
    end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='tenant_modulos' and column_name='atualizado_em') then
        execute 'update plantaopro.tenant_modulos set reg_update=coalesce(reg_update,atualizado_em) where reg_update is null';
    end if;
end $dates$;

update plantaopro.tenant_modulos
set codigo_modulo = nullif(btrim(coalesce(codigo_modulo,codigo)),''),
    codigo = nullif(btrim(coalesce(codigo,codigo_modulo)),''),
    status = upper(coalesce(nullif(btrim(status),''),'ATIVO')),
    reg_status = upper(coalesce(nullif(btrim(reg_status),''),'A')),
    reg_date = coalesce(reg_date,now()),
    origem = coalesce(nullif(btrim(origem),''),'LEGADO')
where codigo_modulo is distinct from nullif(btrim(coalesce(codigo_modulo,codigo)),'')
   or codigo is distinct from nullif(btrim(coalesce(codigo,codigo_modulo)),'')
   or status is null or reg_status is null or reg_date is null or origem is null;

-- A ausência do flag só herda acesso para um contrato realmente ATIVO. Estados
-- AGENDADO, SUSPENSO, BLOQUEADO, CANCELADO e INATIVO continuam sem acesso.
update plantaopro.tenant_modulos
set habilitado = (upper(coalesce(status,''))='ATIVO' and reg_status='A')
where habilitado is null;

create table if not exists plantaopro.tenant_modulos_reconciliacao (
    tenant_modulo_id uuid primary key,
    tenant_id uuid null,
    codigo_legado text null,
    motivo text not null,
    candidatos uuid[] not null default '{}',
    detectado_em timestamptz not null default now(),
    resolvido_em timestamptz null
);

-- O Administrativo 360 é um produto independente: entra no catálogo sem criar
-- dependências implícitas com Saúde 360 ou Plantões.
insert into plantaopro.modulos_sistema(id,codigo,nome,descricao,ordem,status,reg_status,reg_date)
select 'a3600000-0000-4000-8000-000000000099','ADM360','Administrativo 360',
       'Gestão administrativa independente',2190,'ATIVO','A',now()
where not exists(select 1 from plantaopro.modulos_sistema where upper(btrim(codigo))='ADM360' and reg_status='A');

-- Relaciona somente códigos com exatamente um módulo ativo no catálogo.
with matches as (
    select tm.id tenant_modulo_id, (array_agg(ms.id order by ms.id))[1] modulo_id, count(*) quantidade
    from plantaopro.tenant_modulos tm
    join plantaopro.modulos_sistema ms
      on upper(btrim(ms.codigo))=upper(btrim(coalesce(tm.codigo_modulo,tm.codigo)))
     and ms.reg_status='A'
    where tm.modulo_id is null and nullif(btrim(coalesce(tm.codigo_modulo,tm.codigo)),'') is not null
    group by tm.id
)
update plantaopro.tenant_modulos tm
set modulo_id=m.modulo_id, reg_update=coalesce(tm.reg_update,now())
from matches m where tm.id=m.tenant_modulo_id and m.quantidade=1;

update plantaopro.tenant_modulos tm
set codigo_modulo=ms.codigo, codigo=coalesce(nullif(tm.codigo,''),ms.codigo)
from plantaopro.modulos_sistema ms
where tm.modulo_id=ms.id and ms.reg_status='A'
  and (tm.codigo_modulo is null or upper(btrim(tm.codigo_modulo))<>upper(btrim(ms.codigo)));

insert into plantaopro.tenant_modulos_reconciliacao(tenant_modulo_id,tenant_id,codigo_legado,motivo,candidatos,detectado_em,resolvido_em)
select tm.id,tm.tenant_id,coalesce(tm.codigo_modulo,tm.codigo),
       case when count(ms.id)=0 then 'CATALOGO_SEM_CORRESPONDENCIA' else 'CATALOGO_AMBIGUO' end,
       coalesce(array_agg(ms.id order by ms.id) filter(where ms.id is not null),'{}'),now(),null
from plantaopro.tenant_modulos tm
left join plantaopro.modulos_sistema ms
  on upper(btrim(ms.codigo))=upper(btrim(coalesce(tm.codigo_modulo,tm.codigo))) and ms.reg_status='A'
where tm.modulo_id is null and tm.reg_status='A'
group by tm.id,tm.tenant_id,tm.codigo_modulo,tm.codigo
on conflict(tenant_modulo_id) do update set codigo_legado=excluded.codigo_legado,motivo=excluded.motivo,
 candidatos=excluded.candidatos,detectado_em=excluded.detectado_em,resolvido_em=null;

update plantaopro.tenant_modulos_reconciliacao r set resolvido_em=now()
where resolvido_em is null and exists(select 1 from plantaopro.tenant_modulos tm where tm.id=r.tenant_modulo_id and tm.modulo_id is not null);

do $validate$
begin
    if exists(select 1 from plantaopro.tenant_modulos tm left join plantaopro.modulos_sistema ms on ms.id=tm.modulo_id
              where tm.modulo_id is not null and ms.id is null) then
        raise exception 'tenant_modulos contém modulo_id órfão; consulte tenant_modulos antes de prosseguir';
    end if;
    if exists(select 1 from plantaopro.tenant_modulos where tenant_id is null and reg_status='A') then
        raise exception 'tenant_modulos contém contrato ativo sem tenant_id';
    end if;
    if exists(select 1 from plantaopro.tenant_modulos where reg_status='A' and modulo_id is not null
              group by tenant_id,modulo_id having count(*)>1) then
        raise exception 'tenant_modulos contém contratos ativos duplicados para tenant/módulo';
    end if;
end $validate$;

alter table plantaopro.tenant_modulos alter column habilitado set default true;
alter table plantaopro.tenant_modulos alter column habilitado set not null;
alter table plantaopro.tenant_modulos alter column origem set default 'CONTRATO';
alter table plantaopro.tenant_modulos alter column origem set not null;
alter table plantaopro.tenant_modulos alter column status set default 'ATIVO';
alter table plantaopro.tenant_modulos alter column status set not null;
alter table plantaopro.tenant_modulos alter column reg_status set default 'A';
alter table plantaopro.tenant_modulos alter column reg_status set not null;
alter table plantaopro.tenant_modulos alter column reg_date set default now();
alter table plantaopro.tenant_modulos alter column reg_date set not null;

create unique index if not exists ux_tenant_modulos_contrato_ativo
 on plantaopro.tenant_modulos(tenant_id,modulo_id) where reg_status='A' and modulo_id is not null;
create index if not exists ix_tenant_modulos_acesso
 on plantaopro.tenant_modulos(tenant_id,habilitado,status) where reg_status='A';

do $constraints$
begin
    if not exists(select 1 from pg_constraint where conname='fk_tenant_modulos_modulo_id' and conrelid='plantaopro.tenant_modulos'::regclass) then
        alter table plantaopro.tenant_modulos add constraint fk_tenant_modulos_modulo_id
          foreign key(modulo_id) references plantaopro.modulos_sistema(id) not valid;
        alter table plantaopro.tenant_modulos validate constraint fk_tenant_modulos_modulo_id;
    end if;
end $constraints$;

-- ============================================================
-- Seção 62 — Reconciliação e permissões granulares v2.19.8
-- ============================================================

-- SOURCE: database/migrations/2026_09_v2198_reconciliar_duplicidades_contratos_modulos.sql
-- SOURCE-SHA256: 0d4b352f984115ab6a956b72fc2c1f5b3a398ad2aa5b02a3d3e083229b931cf5
-- ============================================================================
-- Migration: 2026_09_v2198_reconciliar_duplicidades_contratos_modulos.sql
-- Objetivo: Reconciliação incremental segura de duplicidades em contratos de
--           módulos (tenant_modulos), preservando histórico, contratos inativos,
--           garantindo catálogo completo de permissões operacionais do ADM360,
--           papéis explícitos de parceiros (hospital, pagador, fornecedor, cliente)
--           e garantindo idempotência em bases novas ou migradas.
-- ============================================================================

-- 1. Garantir tabela de reconciliação para auditoria e histórico
create table if not exists plantaopro.tenant_modulos_reconciliacao (
    tenant_modulo_id uuid primary key,
    tenant_id uuid null,
    codigo_legado text null,
    motivo text not null,
    candidatos uuid[] not null default '{}',
    detectado_em timestamptz not null default now(),
    resolvido_em timestamptz null
);

-- 2. Identificar e reconciliar contratos ativos duplicados para o mesmo (tenant_id, modulo_id)
-- Regra de negócio:
-- (a) Se houver apenas um contrato efetivamente ATIVO (habilitado = true e status = 'ATIVO')
--     e contratos legados desabilitados com reg_status = 'A', preserva o ativo e inativa os legados.
-- (b) Se houver conflito de múltiplos contratos ATIVOS:
--     - Se termos forem idênticos (mesmo preço e limite), preserva um e inativa o duplicado com DUPLICIDADE_IDENTICA_RESOLVIDA.
--     - Se houver divergência comercial/ambiguidade, marca o conflitante como INATIVO_AMBIGUIDADE ('I'),
--       mantém o provisional como PENDENTE_RECONCILIACAO ('A'), e registra em tenant_modulos_reconciliacao
--       com motivo 'CONTRATO_DUPLICADO_AMBIGUO' e resolvido_em = NULL para reconciliação administrativa explícita.
do $reconciliar$
declare
    v_rec record;
    v_ativo_id uuid;
    v_outro_id uuid;
    v_qtd_ativos integer;
    v_termos_iguais boolean;
begin
    for v_rec in
        select tenant_id, modulo_id, count(*) as total
        from plantaopro.tenant_modulos
        where reg_status = 'A' and modulo_id is not null
        group by tenant_id, modulo_id
        having count(*) > 1
    loop
        -- Contar quantos estão efetivamente habilitados e ativos
        select count(*) into v_qtd_ativos
        from plantaopro.tenant_modulos
        where tenant_id = v_rec.tenant_id
          and modulo_id = v_rec.modulo_id
          and reg_status = 'A'
          and habilitado = true
          and upper(status) = 'ATIVO';

        if v_qtd_ativos = 1 then
            -- Exatamente um ativo: escolher este como o canônico
            select id into v_ativo_id
            from plantaopro.tenant_modulos
            where tenant_id = v_rec.tenant_id
              and modulo_id = v_rec.modulo_id
              and reg_status = 'A'
              and habilitado = true
              and upper(status) = 'ATIVO'
            limit 1;

            -- Inativar os outros registros não habilitados que estavam como reg_status = 'A'
            for v_outro_id in
                select id from plantaopro.tenant_modulos
                where tenant_id = v_rec.tenant_id
                  and modulo_id = v_rec.modulo_id
                  and reg_status = 'A'
                  and id <> v_ativo_id
            loop
                update plantaopro.tenant_modulos
                set reg_status = 'I', habilitado = false, status = 'INATIVO', reg_update = now()
                where id = v_outro_id;

                insert into plantaopro.tenant_modulos_reconciliacao
                    (tenant_modulo_id, tenant_id, codigo_legado, motivo, candidatos, detectado_em, resolvido_em)
                values
                    (v_outro_id, v_rec.tenant_id, 'ADM360', 'DUPLICIDADE_RESOLVIDA_PRESERVADO_ATIVO', array[v_ativo_id], now(), now())
                on conflict (tenant_modulo_id) do update
                set motivo = excluded.motivo, resolvido_em = now();
            end loop;

        else
            -- Múltiplos ativos ou nenhum habilitado: verificar se termos são idênticos
            select (count(distinct coalesce(preco_contratado, 0)) <= 1 and count(distinct coalesce(limite_contratado, 0)) <= 1)
            into v_termos_iguais
            from plantaopro.tenant_modulos
            where tenant_id = v_rec.tenant_id
              and modulo_id = v_rec.modulo_id
              and reg_status = 'A';

            -- Selecionar o primeiro registro canônico
            select id into v_ativo_id
            from plantaopro.tenant_modulos
            where tenant_id = v_rec.tenant_id
              and modulo_id = v_rec.modulo_id
              and reg_status = 'A'
            order by (case when habilitado = true and upper(status) = 'ATIVO' then 0 else 1 end),
                     reg_date asc, id asc
            limit 1;

            if v_termos_iguais then
                for v_outro_id in
                    select id from plantaopro.tenant_modulos
                    where tenant_id = v_rec.tenant_id
                      and modulo_id = v_rec.modulo_id
                      and reg_status = 'A'
                      and id <> v_ativo_id
                loop
                    update plantaopro.tenant_modulos
                    set reg_status = 'I', habilitado = false, status = 'INATIVO_DUPLICADO', reg_update = now()
                    where id = v_outro_id;

                    insert into plantaopro.tenant_modulos_reconciliacao
                        (tenant_modulo_id, tenant_id, codigo_legado, motivo, candidatos, detectado_em, resolvido_em)
                    values
                        (v_outro_id, v_rec.tenant_id, 'ADM360', 'DUPLICIDADE_IDENTICA_RESOLVIDA', array[v_ativo_id], now(), now())
                    on conflict (tenant_modulo_id) do update
                    set motivo = excluded.motivo, resolvido_em = now();
                end loop;
            else
                -- Divergência comercial real: manter o contrato primário como PENDENTE_RECONCILIACAO para preservar continuidade
                -- e marcar ambiguidade explícita na reconciliação para auditoria humana sem exclusão de histórico
                update plantaopro.tenant_modulos
                set status = 'PENDENTE_RECONCILIACAO', reg_update = now()
                where id = v_ativo_id;

                for v_outro_id in
                    select id from plantaopro.tenant_modulos
                    where tenant_id = v_rec.tenant_id
                      and modulo_id = v_rec.modulo_id
                      and reg_status = 'A'
                      and id <> v_ativo_id
                loop
                    update plantaopro.tenant_modulos
                    set reg_status = 'I', habilitado = false, status = 'INATIVO_AMBIGUIDADE', reg_update = now()
                    where id = v_outro_id;

                    insert into plantaopro.tenant_modulos_reconciliacao
                        (tenant_modulo_id, tenant_id, codigo_legado, motivo, candidatos, detectado_em, resolvido_em)
                    values
                        (v_outro_id, v_rec.tenant_id, 'ADM360', 'CONTRATO_DUPLICADO_AMBIGUO', array[v_ativo_id], now(), null)
                    on conflict (tenant_modulo_id) do update
                    set motivo = excluded.motivo, resolvido_em = null;
                end loop;
            end if;
        end if;
    end loop;
end $reconciliar$;

-- 3. Garantir índice único parcial de contrato ativo
create unique index if not exists ux_tenant_modulos_contrato_ativo
    on plantaopro.tenant_modulos(tenant_id, modulo_id)
    where reg_status = 'A' and modulo_id is not null;

-- 4. Papéis explícitos de parceiros (Hospital, Pagador/Convênio, Fornecedor, Cliente Comercial)
alter table plantaopro.adm360_parceiros add column if not exists eh_hospital boolean not null default false;
alter table plantaopro.adm360_parceiros add column if not exists eh_pagador boolean not null default false;
alter table plantaopro.adm360_parceiros add column if not exists eh_cliente boolean not null default false;

-- Backfill coerente: parceiros que não eram fornecedores assumem papéis operacionais
update plantaopro.adm360_parceiros
set eh_hospital = true, eh_pagador = true, eh_cliente = true
where fornecedor = false and eh_hospital = false and eh_pagador = false and eh_cliente = false;

-- 5. Catálogo Canônico Completo de Permissões ADM360 (32 Permissões)
do $permissoes$
declare
    v_acao_acessar_id uuid;
    v_acao_editar_id uuid;
    v_modulo_adm_id uuid;
    v_perm_code text;
    v_perm_id uuid;
    v_perfil_rec record;
    v_all_perms text[] := array[
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
    v_readonly_perms text[] := array[
        'ADM360:VER',
        'ADM360:COTACAO_CONSULTAR',
        'ADM360:CONSULTAR_ANEXOS',
        'ADM360:AUDITAR'
    ];
begin
    -- Garantir ações canônicas
    select id into v_acao_acessar_id from plantaopro.acoes_sistema where codigo in ('ACESSAR', 'LISTAR') order by id limit 1;
    if v_acao_acessar_id is null then
        v_acao_acessar_id := gen_random_uuid();
        insert into plantaopro.acoes_sistema(id, codigo, nome, status, reg_status, reg_date)
        values (v_acao_acessar_id, 'ACESSAR', 'Acessar', 'ATIVO', 'A', now());
    end if;

    select id into v_acao_editar_id from plantaopro.acoes_sistema where codigo in ('EDITAR', 'SALVAR') order by id limit 1;
    if v_acao_editar_id is null then
        v_acao_editar_id := gen_random_uuid();
        insert into plantaopro.acoes_sistema(id, codigo, nome, status, reg_status, reg_date)
        values (v_acao_editar_id, 'EDITAR', 'Editar', 'ATIVO', 'A', now());
    end if;

    -- Obter módulo ADM360
    select id into v_modulo_adm_id from plantaopro.modulos_sistema where upper(btrim(codigo)) = 'ADM360' and reg_status = 'A' limit 1;

    -- Cadastrar todas as permissões do Administrativo 360
    foreach v_perm_code in array v_all_perms loop
        select id into v_perm_id from plantaopro.permissoes where upper(btrim(codigo)) = upper(btrim(v_perm_code)) and reg_status = 'A' limit 1;
        if v_perm_id is null then
            v_perm_id := gen_random_uuid();
            insert into plantaopro.permissoes (id, acao_id, modulo_id, codigo, nome, status, reg_status, reg_date)
            values (v_perm_id, case when v_perm_code in ('ADM360:VER', 'ADM360:COTACAO_CONSULTAR', 'ADM360:CONSULTAR_ANEXOS', 'ADM360:AUDITAR') then v_acao_acessar_id else v_acao_editar_id end,
                    v_modulo_adm_id, v_perm_code, replace(v_perm_code, ':', ' - '), 'ATIVO', 'A', now());
        end if;

        -- Atribuir a perfis de gestão (ADMINISTRADOR_CLIENTE, ADMIN_CLIENTE, GESTOR_OPERACIONAL)
        for v_perfil_rec in
            select id from plantaopro.perfis
            where upper(btrim(codigo)) in ('ADMINISTRADOR_CLIENTE', 'ADMIN_CLIENTE', 'GESTOR_OPERACIONAL')
              and reg_status = 'A'
        loop
            insert into plantaopro.perfil_permissoes (id, perfil_id, permissao_id, permitido, reg_status, reg_date)
            values (gen_random_uuid(), v_perfil_rec.id, v_perm_id, true, 'A', now())
            on conflict (perfil_id, permissao_id) where reg_status = 'A' do update set permitido = true;
        end loop;

        -- Atribuir apenas permissões de leitura a perfis de auditoria/consulta
        if v_perm_code = any(v_readonly_perms) then
            for v_perfil_rec in
                select id from plantaopro.perfis
                where upper(btrim(codigo)) in ('CONSULTA_CLIENTE', 'AUDITOR')
                  and reg_status = 'A'
            loop
                insert into plantaopro.perfil_permissoes (id, perfil_id, permissao_id, permitido, reg_status, reg_date)
                values (gen_random_uuid(), v_perfil_rec.id, v_perm_id, true, 'A', now())
                on conflict (perfil_id, permissao_id) where reg_status = 'A' do update set permitido = true;
            end loop;
        end if;
    end loop;
end $permissoes$;

-- 6. Constraints de integridade e consistência dos cadastros
do $constraints$
begin
    if not exists (select 1 from pg_constraint where conname = 'ck_adm360_produtos_preco_custo_nao_negativo') then
        alter table plantaopro.adm360_produtos add constraint ck_adm360_produtos_preco_custo_nao_negativo check (coalesce(preco_custo, 0) >= 0);
    end if;
end $constraints$;

create unique index if not exists ux_adm360_locais_tenant_codigo
    on plantaopro.adm360_locais(tenant_id, upper(codigo)) where ativo = true;

-- ============================================================
-- Seção 63 — Correção do gatilho de validação de tenant ADM360 v2.19.9
-- ============================================================

-- SOURCE: database/migrations/2026_09_v2199_corrigir_trigger_adm360_validar_tenant.sql
-- SOURCE-SHA256: 06c4dc1ea9c82cc242357bd6a11d5d691dc768f8b114ecdbcae7892c1df73a65
-- ============================================================================
-- Migration: 2026_09_v2199_corrigir_trigger_adm360_validar_tenant.sql
-- Objetivo: Corrigir a funcao de validacao de tenant criada em v2190.
--           A funcao v2190 avaliava os tres ramos em expressoes separadas, cada
--           uma contendo subconsulta que referencia NEW.<coluna> existente em
--           apenas UMA das tabelas cobertas (adm_cargos.departamento_id,
--           adm_colaboradores.cargo_id, adm_contratos_trabalho.colaborador_id).
--           O PL/pgSQL resolve o campo do registro NEW dentro das subconsultas
--           contra o rowtype da tabela onde o gatilho disparou, mesmo quando a
--           condicao anterior do AND deveria curto-circuitar, causando falha em
--           QUALQUER INSERT/UPDATE nas tres tabelas:
--             ERRO: registro "new" nao tem campo "cargo_id"
--           Este script reorganiza a funcao em IF/ELSIF por tabela: cada ramo so
--           referencia colunas da propria tabela e so e planejado/executado quando
--           o gatilho dispara naquela tabela. Gatilhos, nomes e mensagens de erro
--           sao mantidos identicos (semantica preservada); nenhuma alteracao de
--           dados ou de estrutura.
-- Idempotencia: CREATE OR REPLACE FUNCTION. Nao altera triggers existentes
--               (trg_adm_cargos_tenant, trg_adm_colaboradores_tenant,
--                trg_adm_contratos_tenant continuam apontando para esta funcao).
-- ============================================================================

create or replace function plantaopro.adm360_validar_tenant()
returns trigger
language plpgsql
as $$
begin
  if tg_table_name = 'adm_cargos' then
    if new.departamento_id is not null
       and not exists(select 1 from plantaopro.adm_departamentos d where d.id = new.departamento_id and d.tenant_id = new.tenant_id) then
      raise exception 'Departamento pertence a outro tenant';
    end if;
  elsif tg_table_name = 'adm_colaboradores' then
    if not exists(select 1 from plantaopro.adm_cargos c where c.id = new.cargo_id and c.tenant_id = new.tenant_id) then
      raise exception 'Cargo pertence a outro tenant';
    end if;
  elsif tg_table_name = 'adm_contratos_trabalho' then
    if not exists(select 1 from plantaopro.adm_colaboradores p where p.id = new.colaborador_id and p.tenant_id = new.tenant_id) then
      raise exception 'Colaborador pertence a outro tenant';
    end if;
  end if;
  return new;
end;
$$;

-- ============================================================
-- Seção 64 — Reconciliação de colunas de médicos compatibilidade v2.20.0
-- ============================================================

-- SOURCE: database/migrations/2026_09_v2200_reconciliar_colunas_medicos_compatibilidade.sql
-- SOURCE-SHA256: 926b11c2d6da671365a22e590491196857f2d6cc1a4edc3b8dd59fa5c0261ae6
-- ============================================================================
-- Migration: 2026_09_v2200_reconciliar_colunas_medicos_compatibilidade.sql
-- Objetivo: Reconciliar bases legadas cuja tabela plantaopro.medicos foi criada
--           por instaladores antigos (sem as colunas de CRM, contato, vinculo
--           clinico e identidade multitenant). Idempotente em bases novas ou ja
--           migradas: somente adiciona colunas ausentes, com os tipos exatos do
--           DDL canônico (PlantaoPro_PostgreSQL_Completo.sql + evolucao vivo).
-- Causa: ObterLookupsAsync (GET /api/administrativo360/cadastros/lookups)
--        consulta m.crm, m.codigo, m.cpf e o seed demo completo insere
--        crm/uf_crm/reg_date; bases legadas sem essas colunas produziam
--        Npgsql 42703 "coluna m.crm nao existe" nos endpoints ADM360.
-- Regras: nenhuma coluna existente é alterada nem removida; sem chaves
--        estrangeiras (compatibilidade com bases legadas); histórico preservado.
-- ============================================================================

alter table plantaopro.medicos add column if not exists especialidade_id uuid;
alter table plantaopro.medicos add column if not exists crm varchar(20);
alter table plantaopro.medicos add column if not exists uf_crm char(2);
alter table plantaopro.medicos add column if not exists telefone varchar(20);
alter table plantaopro.medicos add column if not exists email varchar(120);
alter table plantaopro.medicos add column if not exists cidade varchar(80);
alter table plantaopro.medicos add column if not exists estado char(2);
alter table plantaopro.medicos add column if not exists pix_chave varchar(120);
alter table plantaopro.medicos add column if not exists dados_bancarios jsonb;
alter table plantaopro.medicos add column if not exists observacoes text;
alter table plantaopro.medicos add column if not exists reg_date timestamp default now();
alter table plantaopro.medicos add column if not exists reg_update timestamp;
alter table plantaopro.medicos add column if not exists created_by uuid;
alter table plantaopro.medicos add column if not exists updated_by uuid;
alter table plantaopro.medicos add column if not exists cliente_id uuid;
alter table plantaopro.medicos add column if not exists plano_id bigint;
alter table plantaopro.medicos add column if not exists parceiro_id bigint;
alter table plantaopro.medicos add column if not exists dominio varchar(250);
alter table plantaopro.medicos add column if not exists subdominio varchar(120);
alter table plantaopro.medicos add column if not exists api_key_hash varchar(128);
alter table plantaopro.medicos add column if not exists created_at timestamptz not null default now();
alter table plantaopro.medicos add column if not exists updated_at timestamptz;

-- ============================================================
-- Seção 65 — Reconciliação de colunas comerciais de módulos v2.20.1
-- ============================================================

-- SOURCE: database/migrations/2026_09_v2201_reconciliar_colunas_modulos_sistema_comercial.sql
-- SOURCE-SHA256: 8a24bfa9b0b5209748b201f60080ad57bcc705b06cd395ed23439aec689fa4ff
-- ============================================================================
-- Migration: 2026_09_v2201_reconciliar_colunas_modulos_sistema_comercial.sql
-- Objetivo: Reconciliar bases instaladas pela cadeia oficial que nao receberam
--           as colunas comerciais de plantaopro.modulos_sistema definidas em
--           2026_v2149_saas_core_modulos_perfis_menus.sql (migracao isolada,
--           sem secao nos manifests de instalacao). Idempotente: somente
--           adiciona as colunas ausentes, com os tipos exatos do DDL original.
-- Causa: GET /api/portal-cliente/modulos/catalogo e o upsert de modulos do
--        AdminSaas consultam m.funcionalidades, m.categoria, m.essencial e
--        m.limite_padrao; bases sem elas produzem Npgsql 42703
--        "coluna m.funcionalidades nao existe".
-- Regras: preco_base permanece sob responsabilidade do v2.16.3 (nullable);
--        nenhuma coluna existente e alterada nem removida; historico preservado;
--        tambem reconcile a tabela plantaopro.tenant_modulos_historico
--        (criada somente pelo v2149): ToggleTenantAsync do SaaS insere
--        auditoria de habilitar/desabilitar-tenant e falha com 42P01 sem ela.
-- ============================================================================

alter table plantaopro.modulos_sistema add column if not exists categoria text not null default 'OPERACAO';
alter table plantaopro.modulos_sistema add column if not exists essencial boolean not null default false;
alter table plantaopro.modulos_sistema add column if not exists funcionalidades jsonb not null default '[]'::jsonb;
alter table plantaopro.modulos_sistema add column if not exists limite_padrao integer null;

create table if not exists plantaopro.tenant_modulos_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_modulo_id uuid not null,
    tenant_id uuid not null,
    modulo_id uuid not null,
    acao text not null,
    antes jsonb null,
    depois jsonb null,
    usuario_id uuid null,
    ip_origem text null,
    reg_date timestamptz not null default now()
);

create index if not exists ix_tenant_modulos_historico_tenant
    on plantaopro.tenant_modulos_historico(tenant_id, reg_date desc);

do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'fk_tenant_modulos_historico_modulo') then
        alter table plantaopro.tenant_modulos_historico add constraint fk_tenant_modulos_historico_modulo foreign key (modulo_id) references plantaopro.modulos_sistema(id);
    end if;
end $$;

-- ============================================================
-- Seção 66 — Administrativo360 Exportacao Proposta Imutavel v2.21.0
-- ============================================================

-- SOURCE: database/migrations/2026_09_v2200_administrativo360_exportacao_proposta_imutavel.sql
-- SOURCE-SHA256: 56aa38e963c82fded0e437e0f2ee1e2777a61feb7468e8cc6a2d31e5216ea727
-- Migration 2026_09_v2200_administrativo360_exportacao_proposta_imutavel.sql
-- Administrativo 360 — Bloco 7 (Exportação e Integrações):
-- Arquivo real da proposta aprovada para o canal IMPORTACAO_MANUAL (EXPORTADA_MANUALMENTE).
-- Regras de negócio:
-- 1. O arquivo é gerado UMA única vez por resposta transmitida (UNIQUE tenant_id + resposta_id);
--    retransmissões preservam a primeira geração (ON CONFLICT DO NOTHING na aplicação).
-- 2. O registro é IMUTÁVEL após a geração: UPDATE/DELETE bloqueados por trigger.
--    (TRUNCATE não dispara row triggers — usado apenas em limpeza de testes/homologação.)
-- 3. Escopo multi-tenant: UNIQUE(tenant_id, id) e FOREIGN KEY em tenants.

CREATE TABLE IF NOT EXISTS plantaopro.adm360_cotacao_exportacoes (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    cotacao_id uuid NOT NULL REFERENCES plantaopro.adm360_cotacoes(id),
    resposta_id uuid NOT NULL REFERENCES plantaopro.adm360_cotacao_respostas(id),
    nome_arquivo varchar(255) NOT NULL,
    tamanho_bytes integer NOT NULL CHECK(tamanho_bytes > 0),
    content_type varchar(64) NOT NULL,
    sha256_hash char(64) NOT NULL,
    conteudo bytea NOT NULL,
    gerado_em timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, resposta_id)
);

CREATE INDEX IF NOT EXISTS ix_adm360_export_tenant_cotacao
    ON plantaopro.adm360_cotacao_exportacoes(tenant_id, cotacao_id);

-- Imutabilidade: a proposta exportada não pode ser alterada nem apagada por UPDATE/DELETE.
CREATE OR REPLACE FUNCTION plantaopro.fn_adm360_cotacao_exportacao_imutavel()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Arquivo de exportação da cotação é imutável após a geração (registro %).', OLD.id;
END;
$$;

DROP TRIGGER IF EXISTS trg_adm360_cotacao_exportacao_imutavel ON plantaopro.adm360_cotacao_exportacoes;
CREATE TRIGGER trg_adm360_cotacao_exportacao_imutavel
    BEFORE UPDATE OR DELETE ON plantaopro.adm360_cotacao_exportacoes
    FOR EACH ROW EXECUTE FUNCTION plantaopro.fn_adm360_cotacao_exportacao_imutavel();

-- ============================================================
-- Seção 67 — Reconciliação de Auditoria e Observabilidade v2.21.0
-- ============================================================

-- SOURCE: database/migrations/2026_09_v2202_reconciliar_auditoria_observabilidade.sql
-- SOURCE-SHA256: 6a564a4d23efcbfcc05f84066ed55672163c1f3ae2ca6296f374480c22181d23
-- ============================================================================
-- PlantaPro — Administrativo 360 | Migration v2202
-- Reconciliação de auditoria e observabilidade (delta do 060 em 8c183e2)
-- Instalações existentes criaram auditoria_acoes_criticas, api_request_logs e
-- api_error_logs com o shape genérico histórico (tenant_id/codigo/nome/status/
-- dados/criado_em/atualizado_em). O commit 8c183e2 reformulou
-- database/schema/060_auditoria_observabilidade.sql para o shape novo usado pelo
-- RequestLoggingMiddleware e pelo auditor central, incluindo os índices de
-- consulta. Esta migration aplica o MESMO delta às bases existentes, de forma
-- idempotente, convergindo fresh-install e upgrade ao mesmo shape final:
--   * ADD COLUMN IF NOT EXISTS das colunas novas e das colunas genéricas legadas
--     (qualquer shape intermediário converge ao shape final canônico);
--   * DEFAULT gen_random_uuid() preservado na coluna id;
--   * CREATE INDEX IF NOT EXISTS dos índices de consulta.
-- Nenhuma coluna existente é alterada nem removida; histórico preservado.
-- ============================================================================

-- auditoria_acoes_criticas
ALTER TABLE IF EXISTS plantaopro.auditoria_acoes_criticas
    ADD COLUMN IF NOT EXISTS usuario_id uuid NULL,
    ADD COLUMN IF NOT EXISTS cliente_id uuid NULL,
    ADD COLUMN IF NOT EXISTS entidade varchar(100) NOT NULL DEFAULT 'SISTEMA',
    ADD COLUMN IF NOT EXISTS entidade_id uuid NULL,
    ADD COLUMN IF NOT EXISTS acao varchar(100) NOT NULL DEFAULT 'ACAO',
    ADD COLUMN IF NOT EXISTS detalhes jsonb NULL,
    ADD COLUMN IF NOT EXISTS sucesso boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS ip_origem varchar(64) NULL,
    ADD COLUMN IF NOT EXISTS perfil varchar(80) NULL,
    ADD COLUMN IF NOT EXISTS user_agent text NULL,
    ADD COLUMN IF NOT EXISTS reg_date timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS reg_status char(1) NOT NULL DEFAULT 'A',
    ADD COLUMN IF NOT EXISTS tenant_id uuid NULL,
    ADD COLUMN IF NOT EXISTS codigo text NULL,
    ADD COLUMN IF NOT EXISTS nome text NULL,
    ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO',
    ADD COLUMN IF NOT EXISTS dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    ADD COLUMN IF NOT EXISTS criado_em timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS atualizado_em timestamptz NULL;

ALTER TABLE IF EXISTS plantaopro.auditoria_acoes_criticas
    ALTER COLUMN id SET DEFAULT gen_random_uuid();

CREATE INDEX IF NOT EXISTS ix_auditoria_acoes_criticas_cliente_data ON plantaopro.auditoria_acoes_criticas(cliente_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_auditoria_acoes_criticas_usuario_data ON plantaopro.auditoria_acoes_criticas(usuario_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_auditoria_acoes_criticas_entidade ON plantaopro.auditoria_acoes_criticas(entidade, entidade_id);
CREATE INDEX IF NOT EXISTS ix_auditoria_acoes_criticas_acao_data ON plantaopro.auditoria_acoes_criticas(acao, reg_date DESC);

-- api_request_logs
ALTER TABLE IF EXISTS plantaopro.api_request_logs
    ADD COLUMN IF NOT EXISTS endpoint text NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS metodo varchar(10) NOT NULL DEFAULT 'GET',
    ADD COLUMN IF NOT EXISTS method varchar(12) NOT NULL DEFAULT 'GET',
    ADD COLUMN IF NOT EXISTS status_code integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS sucesso boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS duracao_ms bigint NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS duration_ms bigint NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS usuario_id uuid NULL,
    ADD COLUMN IF NOT EXISTS cliente_id uuid NULL,
    ADD COLUMN IF NOT EXISTS email varchar(255) NULL,
    ADD COLUMN IF NOT EXISTS perfil varchar(80) NULL,
    ADD COLUMN IF NOT EXISTS ip_origem varchar(64) NULL,
    ADD COLUMN IF NOT EXISTS ip varchar(80) NULL,
    ADD COLUMN IF NOT EXISTS user_agent text NULL,
    ADD COLUMN IF NOT EXISTS query_string text NULL,
    ADD COLUMN IF NOT EXISTS erro text NULL,
    ADD COLUMN IF NOT EXISTS error_message text NULL,
    ADD COLUMN IF NOT EXISTS reg_date timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS reg_status char(1) NOT NULL DEFAULT 'A',
    ADD COLUMN IF NOT EXISTS tenant_id uuid NULL,
    ADD COLUMN IF NOT EXISTS codigo text NULL,
    ADD COLUMN IF NOT EXISTS nome text NULL,
    ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO',
    ADD COLUMN IF NOT EXISTS dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    ADD COLUMN IF NOT EXISTS criado_em timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS atualizado_em timestamptz NULL;

ALTER TABLE IF EXISTS plantaopro.api_request_logs
    ALTER COLUMN id SET DEFAULT gen_random_uuid();

CREATE INDEX IF NOT EXISTS ix_api_request_logs_reg_date ON plantaopro.api_request_logs(reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_endpoint_data ON plantaopro.api_request_logs(endpoint, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_status_data ON plantaopro.api_request_logs(status_code, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_usuario_data ON plantaopro.api_request_logs(usuario_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_cliente_data ON plantaopro.api_request_logs(cliente_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_perfil_data ON plantaopro.api_request_logs(perfil, reg_date DESC);

-- api_error_logs
ALTER TABLE IF EXISTS plantaopro.api_error_logs
    ADD COLUMN IF NOT EXISTS endpoint text NULL,
    ADD COLUMN IF NOT EXISTS metodo varchar(10) NULL,
    ADD COLUMN IF NOT EXISTS method varchar(12) NOT NULL DEFAULT 'GET',
    ADD COLUMN IF NOT EXISTS status_code integer NULL,
    ADD COLUMN IF NOT EXISTS success boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS usuario_id uuid NULL,
    ADD COLUMN IF NOT EXISTS cliente_id uuid NULL,
    ADD COLUMN IF NOT EXISTS email varchar(255) NULL,
    ADD COLUMN IF NOT EXISTS perfil varchar(80) NULL,
    ADD COLUMN IF NOT EXISTS ip_origem varchar(64) NULL,
    ADD COLUMN IF NOT EXISTS ip varchar(80) NULL,
    ADD COLUMN IF NOT EXISTS user_agent text NULL,
    ADD COLUMN IF NOT EXISTS query_string text NULL,
    ADD COLUMN IF NOT EXISTS duration_ms bigint NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS mensagem text NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS error_message text NULL,
    ADD COLUMN IF NOT EXISTS exception_type varchar(255) NULL,
    ADD COLUMN IF NOT EXISTS stack_trace text NULL,
    ADD COLUMN IF NOT EXISTS reg_date timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS reg_status char(1) NOT NULL DEFAULT 'A',
    ADD COLUMN IF NOT EXISTS tenant_id uuid NULL,
    ADD COLUMN IF NOT EXISTS codigo text NULL,
    ADD COLUMN IF NOT EXISTS nome text NULL,
    ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO',
    ADD COLUMN IF NOT EXISTS dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    ADD COLUMN IF NOT EXISTS criado_em timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS atualizado_em timestamptz NULL;

ALTER TABLE IF EXISTS plantaopro.api_error_logs
    ALTER COLUMN id SET DEFAULT gen_random_uuid();

CREATE INDEX IF NOT EXISTS ix_api_error_logs_reg_date ON plantaopro.api_error_logs(reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_api_error_logs_status_data ON plantaopro.api_error_logs(status_code, reg_date DESC);

-- ============================================================
-- Seção 68 — Administrativo360 Estados de Transmissão da Exportação v2.21.0
-- ============================================================

-- SOURCE: database/migrations/2026_09_v2300_administrativo360_status_transmissao_exportacao.sql
-- SOURCE-SHA256: e220614f3a1161b38ccd84cad2d27211e5cf9e6f2c2dc4bf64887452296c4d41
-- ============================================================================
-- PlantaPro — Administrativo 360 | Migration v2300
-- Bloco 7 — Exportação e Integrações
-- Estende o constraint de verificação de status_transmissao nas respostas de
-- cotação para incluir os estados introduzidos pelo canal de exportação manual
-- (IMPORTACAO_MANUAL) e pelos provedores ainda não integrados:
--   * EXPORTADA_MANUALMENTE  — proposta aprovada exportada como arquivo imutável
--                              pelo operador (canal manual); sem protocolo externo.
--   * CONFIGURACAO_PENDENTE  — tentativa de transmissão em provedor oficial com
--                              integração pendente (OPMENEXO/INPART): sem protocolo,
--                              a cotação segue PRONTA_PARA_ENVIO.
-- Mantém os estados existentes: NA_FILA, ENVIANDO, ACEITA_PELO_PORTAL,
-- REJEITADA_PELO_PORTAL, RESULTADO_DESCONHECIDO.
-- Idempotente: DROP CONSTRAINT IF EXISTS + ADD CONSTRAINT.
-- ============================================================================

ALTER TABLE plantaopro.adm360_cotacao_respostas
    DROP CONSTRAINT IF EXISTS adm360_cotacao_respostas_status_transmissao_check;

ALTER TABLE plantaopro.adm360_cotacao_respostas
    ADD CONSTRAINT adm360_cotacao_respostas_status_transmissao_check
    CHECK (status_transmissao IN (
        'NA_FILA',
        'ENVIANDO',
        'EXPORTADA_MANUALMENTE',
        'CONFIGURACAO_PENDENTE',
        'ACEITA_PELO_PORTAL',
        'REJEITADA_PELO_PORTAL',
        'RESULTADO_DESCONHECIDO'
    ));

-- ============================================================
-- Seção 69 — Administrativo360 Eventos Imutáveis v2.21.0
-- ============================================================

-- SOURCE: database/migrations/2026_09_v2301_administrativo360_eventos_imutabilidade.sql
-- SOURCE-SHA256: 77e3d861188b1984a8637940af7b73e8b00cea7ae86b189a9cd1c62e1d673f4d
-- ============================================================================
-- PlantaoPro | Migration: 2026_09_v2301_administrativo360_eventos_imutabilidade
-- Administrativo 360 - Eventos unificados imutaveis com hash (requisito P4)
--
-- Alteracoes oficiais:
--   1. Cria plantaopro.adm360_eventos: log append-only de eventos de negocio que
--      classifica APROVACAO, ARQUIVO, DECLARACAO_MANUAL e RETORNO_EXTERNO, com hash
--      SHA-256 do conteudo canonico e chave de idempotencia opcional
--      UNIQUE(tenant_id, idempotency_key).
--   2. Protege adm360_eventos e adm360_documento_eventos (apos backfill de
--      sha256_hash) contra UPDATE/DELETE: append-only com bypass GUC controlado.
--   3. Protege adm360_vale_eventos: DELETE bloqueado; apenas o progresso da
--      decisao (quantidade_decidida) pode ser atualizado.
--   4. Conciliacao unica: respostas presas em ENVIANDO por queda do processo viram
--      RESULTADO_DESCONHECIDO (estado honesto, sem protocolo externo confirmado).
--   5. Trigger de preenchimento: todo novo evento de documento fiscal recebe
--      sha256_hash calculado sobre as colunas armazenadas (mesma formula do backfill).
--
-- Idempotencia: CREATE ... IF NOT EXISTS / CREATE OR REPLACE / ADD COLUMN IF NOT EXISTS;
-- o backfill atualiza apenas linhas sem hash; a conciliacao zera linhas no re-executar.
-- O bypass SELECT set_config('plantao.bypass_imutabilidade_adm360', 'on', true) deve ser
-- feito na MESMA transacao da escrita protegida e esta reservado a ferramentas oficiais
-- de manutencao. TRUNCATE nao dispara row triggers (mesmo padrao de v2200).
-- ============================================================================

-- 1. Log unico de eventos do Administrativo 360 (append-only)
CREATE TABLE IF NOT EXISTS plantaopro.adm360_eventos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    tipo_evento varchar(30) NOT NULL CHECK(tipo_evento IN ('APROVACAO', 'ARQUIVO', 'DECLARACAO_MANUAL', 'RETORNO_EXTERNO')),
    entidade varchar(40) NOT NULL,
    entidade_id uuid NOT NULL,
    usuario_id uuid,
    descricao text NOT NULL,
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    sha256_hash char(64) NOT NULL CHECK(sha256_hash ~ '^[0-9a-f]{64}$'),
    idempotency_key varchar(160),
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, idempotency_key)
);

CREATE INDEX IF NOT EXISTS ix_adm360_eventos_tenant_tipo
    ON plantaopro.adm360_eventos(tenant_id, tipo_evento, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_adm360_eventos_entidade
    ON plantaopro.adm360_eventos(tenant_id, entidade, entidade_id);

COMMENT ON TABLE plantaopro.adm360_eventos IS 'Administrativo 360: log unificado de eventos imutaveis (APROVACAO/ARQUIVO/DECLARACAO_MANUAL/RETORNO_EXTERNO) com hash SHA-256 do conteudo.';

-- 2. Imutabilidade append-only com bypass GUC controlado
CREATE OR REPLACE FUNCTION plantaopro.fn_adm360_evento_imutavel()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF current_setting('plantao.bypass_imutabilidade_adm360', true) = 'on' THEN
        RETURN COALESCE(NEW, OLD);
    END IF;
    RAISE EXCEPTION 'Evento do modulo Administrativo 360 e imutavel (append-only): UPDATE e DELETE nao permitidos (registro %).', COALESCE(NEW.id, OLD.id);
END;
$$;

DROP TRIGGER IF EXISTS trg_adm360_eventos_imutavel ON plantaopro.adm360_eventos;
CREATE TRIGGER trg_adm360_eventos_imutavel
    BEFORE UPDATE OR DELETE ON plantaopro.adm360_eventos
    FOR EACH ROW EXECUTE FUNCTION plantaopro.fn_adm360_evento_imutavel();

-- 3. Eventos de documento fiscal (NF-e): hash SHA-256 + imutabilidade append-only
ALTER TABLE plantaopro.adm360_documento_eventos ADD COLUMN IF NOT EXISTS sha256_hash char(64);

UPDATE plantaopro.adm360_documento_eventos e
SET sha256_hash = encode(digest(
        coalesce(e.id::text, '') || '|' || coalesce(e.tenant_id::text, '') || '|' ||
        coalesce(e.tipo_evento, '') || '|' || coalesce(e.sequencia_evento::text, '') || '|' ||
        coalesce(e.descricao_evento, '') || '|' || coalesce(e.data_evento::text, '') || '|' ||
        coalesce(e.protocolo, '') || '|' || coalesce(e.detalhes, ''),
        'sha256'), 'hex')
WHERE e.sha256_hash IS NULL;

ALTER TABLE plantaopro.adm360_documento_eventos ALTER COLUMN sha256_hash SET NOT NULL;

-- Fonte unica da formula: todo novo evento (qualquer tipo) recebe o hash calculado
-- sobre as colunas que serão armazenadas (consistente com o backfill acima).
CREATE OR REPLACE FUNCTION plantaopro.fn_adm360_documento_evento_preencher_hash()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.sha256_hash IS NULL THEN
        NEW.sha256_hash := encode(digest(
            coalesce(NEW.id::text, '') || '|' || coalesce(NEW.tenant_id::text, '') || '|' ||
            coalesce(NEW.tipo_evento, '') || '|' || coalesce(NEW.sequencia_evento::text, '') || '|' ||
            coalesce(NEW.descricao_evento, '') || '|' || coalesce(NEW.data_evento::text, '') || '|' ||
            coalesce(NEW.protocolo, '') || '|' || coalesce(NEW.detalhes, ''),
            'sha256'), 'hex')::character(64);
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_adm360_documento_eventos_hash ON plantaopro.adm360_documento_eventos;
CREATE TRIGGER trg_adm360_documento_eventos_hash
    BEFORE INSERT ON plantaopro.adm360_documento_eventos
    FOR EACH ROW EXECUTE FUNCTION plantaopro.fn_adm360_documento_evento_preencher_hash();

DROP TRIGGER IF EXISTS trg_adm360_documento_eventos_imutavel ON plantaopro.adm360_documento_eventos;
CREATE TRIGGER trg_adm360_documento_eventos_imutavel
    BEFORE UPDATE OR DELETE ON plantaopro.adm360_documento_eventos
    FOR EACH ROW EXECUTE FUNCTION plantaopro.fn_adm360_evento_imutavel();

-- 4. Eventos de vale: identidade imutavel; so o progresso da decisao pode mudar
CREATE OR REPLACE FUNCTION plantaopro.fn_adm360_vale_evento_progresso()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF current_setting('plantao.bypass_imutabilidade_adm360', true) = 'on' THEN
        RETURN COALESCE(NEW, OLD);
    END IF;
    IF TG_OP = 'DELETE' THEN
        RAISE EXCEPTION 'Evento de vale e imutavel: DELETE nao permitido (registro %).', OLD.id;
    END IF;
    IF NEW.tenant_id IS DISTINCT FROM OLD.tenant_id
       OR NEW.vale_id IS DISTINCT FROM OLD.vale_id
       OR NEW.vale_item_id IS DISTINCT FROM OLD.vale_item_id
       OR NEW.tipo IS DISTINCT FROM OLD.tipo
       OR NEW.quantidade IS DISTINCT FROM OLD.quantidade
       OR NEW.data_evento IS DISTINCT FROM OLD.data_evento
       OR NEW.motivo IS DISTINCT FROM OLD.motivo
       OR NEW.movimento_id IS DISTINCT FROM OLD.movimento_id
       OR NEW.idempotency_key IS DISTINCT FROM OLD.idempotency_key
       OR NEW.registrado_por IS DISTINCT FROM OLD.registrado_por
       OR NEW.created_at IS DISTINCT FROM OLD.created_at
    THEN
        RAISE EXCEPTION 'Evento de vale e imutavel: apenas o progresso da decisao (quantidade_decidida) pode ser atualizado (registro %).', NEW.id;
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_adm360_vale_eventos_progresso ON plantaopro.adm360_vale_eventos;
CREATE TRIGGER trg_adm360_vale_eventos_progresso
    BEFORE UPDATE OR DELETE ON plantaopro.adm360_vale_eventos
    FOR EACH ROW EXECUTE FUNCTION plantaopro.fn_adm360_vale_evento_progresso();

-- 5. Recuperacao unica: ENVIANDO ancorado por queda do processo.
-- Limiar de 10 min (mesmo criterio da recuperacao de boot): a re-execucao sob carga
-- concorrente nunca toca uma transmissao em curso (ENVIANDO recente pertence ao conector ativo).
UPDATE plantaopro.adm360_cotacao_respostas
SET status_transmissao = 'RESULTADO_DESCONHECIDO',
    mensagem_retorno = coalesce(mensagem_retorno, '') || ' | Recuperacao oficial: transmissao ficou em ENVIANDO sem resultado registrado (processo interrompido).',
    updated_at = now()
WHERE status_transmissao = 'ENVIANDO'
  AND updated_at <= now() - interval '10 minutes';

-- ============================================================
-- Seção 70 — Administrativo360 Reafirmação Trigger Validar Tenant v2.21.1
-- ============================================================

-- SOURCE: database/migrations/2026_09_v2302_reafirmar_correcao_trigger_adm360_validar_tenant.sql
-- SOURCE-SHA256: bc6baaff554451db2b2b50d37c432460ddee9b01d00d10cf18ebe0105670172a
-- ============================================================================
-- Migration: 2026_09_v2302_reafirmar_correcao_trigger_adm360_validar_tenant.sql
-- Objetivo: Reafirmar a correcao da funcao plantaopro.adm360_validar_tenant()
--           introduzida em v2199. O migrador oficial pode re-executar o v2190
--           em um upgrade quando a linha de tracking do v2190 nao estava em
--           schema_migrations enquanto a do v2199 ja existia: nesse cenario o
--           CREATE OR REPLACE FUNCTION do v2190 (corpo com avaliacao dos tres
--           ramos em expressoes separadas) foi aplicado APOS o v2199 e
--           restabeleceu a falha de resolugao do rowtype NEW dentro das
--           subconsultas:
--             ERRO: registro "new" nao tem campo "cargo_id"
--           quebrando INSERT/UPDATE em adm_cargos, adm_colaboradores e
--           adm_contratos_trabalho. Este script reaplica, em ordem cronologica
--           oficial e apos o v2190/v2199, o corpo corrigido em IF/ELSIF por
--           tabela de v2199, de modo que instalacao limpa e upgrade convergem
--           para a mesma funcao. Nomes, triggers, mensagens de erro e dados
--           sao mantidos identicos; nenhuma alteracao de estrutura.
-- Idempotencia: CREATE OR REPLACE FUNCTION. Os gatilhos existentes
--               (trg_adm_cargos_tenant, trg_adm_colaboradores_tenant,
--                trg_adm_contratos_tenant) continuam apontando para esta
--               funcao, sem recriacao.
-- ============================================================================

create or replace function plantaopro.adm360_validar_tenant()
returns trigger
language plpgsql
as $$
begin
  if tg_table_name = 'adm_cargos' then
    if new.departamento_id is not null
       and not exists(select 1 from plantaopro.adm_departamentos d where d.id = new.departamento_id and d.tenant_id = new.tenant_id) then
      raise exception 'Departamento pertence a outro tenant';
    end if;
  elsif tg_table_name = 'adm_colaboradores' then
    if not exists(select 1 from plantaopro.adm_cargos c where c.id = new.cargo_id and c.tenant_id = new.tenant_id) then
      raise exception 'Cargo pertence a outro tenant';
    end if;
  elsif tg_table_name = 'adm_contratos_trabalho' then
    if not exists(select 1 from plantaopro.adm_colaboradores p where p.id = new.colaborador_id and p.tenant_id = new.tenant_id) then
      raise exception 'Colaborador pertence a outro tenant';
    end if;
  end if;
  return new;
end;
$$;

-- ============================================================
-- Seção 71 — Saúde 360 Prontuário e Permissões v1.27.0
-- ============================================================

-- SOURCE: database/migrations/2026_v1270_normalizar_permissoes_e_prontuario.sql
-- SOURCE-SHA256: 74819da1eb03011a10ec4ac7d2d3c5ff0fb70080fbf683e271e95807f9215b50
BEGIN;
CREATE SCHEMA IF NOT EXISTS plantaopro;

CREATE TABLE IF NOT EXISTS plantaopro.perfil_permissoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), perfil_id uuid NOT NULL, permissao_id uuid NOT NULL, permitido boolean NOT NULL DEFAULT true, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now());
DO $migration$
DECLARE cols_ok boolean;
BEGIN
  IF to_regclass('plantaopro.perfis_permissoes') IS NOT NULL THEN
    SELECT count(*)=2 INTO cols_ok FROM information_schema.columns WHERE table_schema='plantaopro' AND table_name='perfis_permissoes' AND column_name IN ('perfil_id','permissao_id');
    IF cols_ok THEN
      EXECUTE 'insert into plantaopro.perfil_permissoes(perfil_id,permissao_id,permitido,reg_status,reg_date) select perfil_id,permissao_id,true,''A'',now() from plantaopro.perfis_permissoes l where perfil_id is not null and permissao_id is not null and not exists(select 1 from plantaopro.perfil_permissoes c where c.perfil_id=l.perfil_id and c.permissao_id=l.permissao_id and c.reg_status=''A'')';
      IF NOT EXISTS(select 1 from pg_constraint where confrelid='plantaopro.perfis_permissoes'::regclass) THEN DROP TABLE plantaopro.perfis_permissoes; END IF;
    END IF;
  END IF;
END $migration$;
CREATE UNIQUE INDEX IF NOT EXISTS ux_perfil_permissoes_ativo ON plantaopro.perfil_permissoes(perfil_id,permissao_id) WHERE reg_status='A';

ALTER TABLE plantaopro.consultas ADD COLUMN IF NOT EXISTS atendimento_id uuid, ADD COLUMN IF NOT EXISTS unidade_id uuid, ADD COLUMN IF NOT EXISTS triagem_id uuid, ADD COLUMN IF NOT EXISTS anamnese text, ADD COLUMN IF NOT EXISTS exame_fisico text, ADD COLUMN IF NOT EXISTS hipotese_diagnostica text, ADD COLUMN IF NOT EXISTS diagnostico text, ADD COLUMN IF NOT EXISTS conduta text, ADD COLUMN IF NOT EXISTS orientacoes text, ADD COLUMN IF NOT EXISTS observacoes text, ADD COLUMN IF NOT EXISTS inicio_em timestamptz, ADD COLUMN IF NOT EXISTS versao integer NOT NULL DEFAULT 1;
CREATE INDEX IF NOT EXISTS ix_consultas_fila_medica ON plantaopro.consultas(cliente_id,unidade_id,status,reg_date) WHERE reg_status='A';

CREATE TABLE IF NOT EXISTS plantaopro.consulta_cids(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),cliente_id uuid NOT NULL,consulta_id uuid NOT NULL,cid_id uuid NOT NULL,tipo varchar(20) NOT NULL DEFAULT 'SECUNDARIO',principal boolean NOT NULL DEFAULT false,ordem integer NOT NULL DEFAULT 1,created_by uuid,removed_by uuid,removed_at timestamptz,reg_date timestamptz NOT NULL DEFAULT now(),reg_status char(1) NOT NULL DEFAULT 'A');
CREATE UNIQUE INDEX IF NOT EXISTS ux_consulta_cid_ativo ON plantaopro.consulta_cids(cliente_id,consulta_id,cid_id) WHERE reg_status='A';
CREATE UNIQUE INDEX IF NOT EXISTS ux_consulta_cid_principal ON plantaopro.consulta_cids(cliente_id,consulta_id) WHERE principal AND reg_status='A';
CREATE INDEX IF NOT EXISTS ix_consulta_cids_consulta ON plantaopro.consulta_cids(cliente_id,consulta_id,ordem) WHERE reg_status='A';

CREATE TABLE IF NOT EXISTS plantaopro.consulta_historico(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),cliente_id uuid NOT NULL,consulta_id uuid NOT NULL,evento varchar(50) NOT NULL,versao integer NOT NULL,created_by uuid,reg_date timestamptz NOT NULL DEFAULT now(),reg_status char(1) NOT NULL DEFAULT 'A');
CREATE TABLE IF NOT EXISTS plantaopro.consulta_solicitacoes_exames(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),cliente_id uuid NOT NULL,consulta_id uuid NOT NULL,exame varchar(250) NOT NULL,indicacao_clinica text,prioridade varchar(20) NOT NULL DEFAULT 'ROTINA',created_by uuid,reg_date timestamptz NOT NULL DEFAULT now(),reg_status char(1) NOT NULL DEFAULT 'A');
CREATE TABLE IF NOT EXISTS plantaopro.consulta_encaminhamentos(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),cliente_id uuid NOT NULL,consulta_id uuid NOT NULL,especialidade varchar(150) NOT NULL,motivo text NOT NULL,created_by uuid,reg_date timestamptz NOT NULL DEFAULT now(),reg_status char(1) NOT NULL DEFAULT 'A');

ALTER TABLE plantaopro.clinica_contas_receber ADD COLUMN IF NOT EXISTS unidade_id uuid,ADD COLUMN IF NOT EXISTS paciente_id uuid,ADD COLUMN IF NOT EXISTS atendimento_id uuid,ADD COLUMN IF NOT EXISTS consulta_id uuid,ADD COLUMN IF NOT EXISTS medico_id uuid,ADD COLUMN IF NOT EXISTS procedimento_id uuid,ADD COLUMN IF NOT EXISTS valor_bruto numeric(14,2) NOT NULL DEFAULT 0,ADD COLUMN IF NOT EXISTS desconto numeric(14,2) NOT NULL DEFAULT 0,ADD COLUMN IF NOT EXISTS coparticipacao numeric(14,2) NOT NULL DEFAULT 0,ADD COLUMN IF NOT EXISTS valor_liquido numeric(14,2) NOT NULL DEFAULT 0,ADD COLUMN IF NOT EXISTS valor_pago numeric(14,2) NOT NULL DEFAULT 0,ADD COLUMN IF NOT EXISTS vencimento date,ADD COLUMN IF NOT EXISTS origem varchar(30),ADD COLUMN IF NOT EXISTS justificativa text,ADD COLUMN IF NOT EXISTS created_by uuid,ADD COLUMN IF NOT EXISTS reg_date timestamptz NOT NULL DEFAULT now();
CREATE UNIQUE INDEX IF NOT EXISTS ux_conta_consulta_ativa ON plantaopro.clinica_contas_receber(cliente_id,consulta_id) WHERE consulta_id IS NOT NULL AND reg_status='A';

INSERT INTO plantaopro.modulos_sistema(id,codigo,nome,descricao,reg_status,reg_date) SELECT gen_random_uuid(),'PRONTUARIO','Prontuário','Acesso clínico sensível','A',now() WHERE NOT EXISTS(SELECT 1 FROM plantaopro.modulos_sistema WHERE codigo='PRONTUARIO' AND reg_status='A');
INSERT INTO plantaopro.acoes_sistema(id,codigo,nome,descricao,reg_status,reg_date) SELECT gen_random_uuid(),'OPERAR','Operar','Ação clínica granular','A',now() WHERE NOT EXISTS(SELECT 1 FROM plantaopro.acoes_sistema WHERE codigo='OPERAR' AND reg_status='A');
INSERT INTO plantaopro.permissoes(id,codigo,nome,descricao,modulo,acao,modulo_id,acao_id,sensivel,status,reg_status,reg_date)
SELECT gen_random_uuid(),p.codigo,p.nome,p.nome,'PRONTUARIO','OPERAR',m.id,a.id,true,'ATIVO','A',now() FROM (VALUES
('CONSULTA_VISUALIZAR','Visualizar consulta'),('CONSULTA_INICIAR','Iniciar consulta'),('CONSULTA_EDITAR','Editar consulta'),('CONSULTA_FINALIZAR','Finalizar consulta'),('CONSULTA_CANCELAR','Cancelar consulta'),('CONSULTA_REABRIR','Reabrir consulta'),('CONSULTA_VER_HISTORICO','Ver histórico clínico'),('CID_VINCULAR','Vincular CID'),('CID_REMOVER','Remover CID'),('PRESCRICAO_CRIAR','Criar prescrição'),('PRESCRICAO_EDITAR','Editar prescrição'),('PRESCRICAO_FINALIZAR','Finalizar prescrição'),('PRESCRICAO_CANCELAR','Cancelar prescrição'),('PRESCRICAO_IMPRIMIR','Imprimir prescrição'),('PRESCRICAO_GERENCIAR_MODELOS','Gerenciar modelos'),('PRONTUARIO_VER_DADOS_SENSIVEIS','Ver dados clínicos sensíveis'),('PRONTUARIO_EXPORTAR','Exportar prontuário')) p(codigo,nome) CROSS JOIN plantaopro.modulos_sistema m CROSS JOIN plantaopro.acoes_sistema a
WHERE m.codigo='PRONTUARIO' AND m.reg_status='A' AND a.codigo='OPERAR' AND a.reg_status='A' AND NOT EXISTS(SELECT 1 FROM plantaopro.permissoes x WHERE x.codigo=p.codigo);

INSERT INTO plantaopro.schema_migrations(id,script_path,checksum,applied_at) SELECT 'v1.27.0','database/migrations/2026_v1270_normalizar_permissoes_e_prontuario.sql','runtime-managed',now() WHERE NOT EXISTS(SELECT 1 FROM plantaopro.schema_migrations WHERE id='v1.27.0');
COMMIT;

-- ============================================================
-- Seção 72 — Administrativo360 Documentos XML B1 v2.21.2
-- ============================================================

-- SOURCE: database/migrations/2026_09_v2303_administrativo360_documentos_xml_b1.sql
-- SOURCE-SHA256: 50f78ce6e574d3fc7bf3863dc1fbad702ec73e29e999ae6d710ccf4943e36e6c
-- ============================================================================
-- Migration: 2026_09_v2303_administrativo360_documentos_xml_b1.sql
-- Objetivo: B1 do Administrativo 360 - Central de documentos XML.
--   1. Coluna xml_bytes bytea NOT NULL com os bytes originais do arquivo
--      recebido (fonte da verdade para hash SHA-256 e download identico ao
--      enviado). Backfill: convert_to(xml_conteudo, 'UTF8') sobre o texto ja
--      gravado.
--   2. chave_acesso ampliada para varchar(60) para chaves sinteticas NFS-e
--      (NFSE + 36 hex) e identificadores longos em quarentena.
--   3. CHECK de tipo_documento estendido para os tipos fiscais suportados
--      por conteudo: NFE_COMPLETA (mod 55), NFC_E (mod 65), NFS_E (mod 67)
--      e RESUMO (legado). A DTD continua proibida no parse da aplicacao
--      (DtdProcessing.Ignore + XmlResolver=null): aqui o banco apenas
--      restringe o conjunto de tipos persistidos.
-- Idempotencia: DO $$ em 3 etapas (ADD nullable -> backfill -> SET NOT NULL);
--               DO $$ que remove o CHECK antigo localizando-o pela definicao
--               (o nome do constraint varia entre instalacoes) e recria com
--               nome fixo apenas quando falta; DO $$ com IF p/ varchar(60).
-- ============================================================================

-- 1. Bytes originais preservados (fonte da verdade para hash/download).
--    Padrão em 3 etapas (ADD nullable -> backfill -> SET NOT NULL) porque o
--    PostgreSQL desta instancia nao aceita referencia de coluna no DEFAULT.
DO $migration$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'plantaopro'
      AND table_name = 'adm360_documentos_recebidos'
      AND column_name = 'xml_bytes'
  ) THEN
    ALTER TABLE plantaopro.adm360_documentos_recebidos ADD COLUMN xml_bytes bytea;
  END IF;

  -- Backfill idempotente: bytes = UTF-8 do texto ja gravado (rows herdadas)
  UPDATE plantaopro.adm360_documentos_recebidos
     SET xml_bytes = convert_to(xml_conteudo, 'UTF8')
   WHERE xml_bytes IS NULL;

  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'plantaopro'
      AND table_name = 'adm360_documentos_recebidos'
      AND column_name = 'xml_bytes'
      AND is_nullable = 'YES'
  ) THEN
    ALTER TABLE plantaopro.adm360_documentos_recebidos ALTER COLUMN xml_bytes SET NOT NULL;
  END IF;
END $migration$;

COMMENT ON COLUMN plantaopro.adm360_documentos_recebidos.xml_bytes IS
  'B1: bytes originais do arquivo XML recebido; fonte da verdade para xml_hash (SHA-256) e download.';

-- 2. chave_acesso: varchar(44) -> varchar(60)
DO $migration$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'plantaopro'
      AND table_name = 'adm360_documentos_recebidos'
      AND column_name = 'chave_acesso'
      AND character_maximum_length IS NOT NULL
      AND character_maximum_length < 60
  ) THEN
    ALTER TABLE plantaopro.adm360_documentos_recebidos
      ALTER COLUMN chave_acesso TYPE varchar(60);
  END IF;
END $migration$;

-- 3. CHECK tipo_documento estendido (NFE_COMPLETA | NFC_E | NFS_E | RESUMO)
DO $migration$
DECLARE
  r record;
  tem_novo boolean;
BEGIN
  -- Remove qualquer CHECK antigo da coluna (definicao contendo NFE_COMPLETA),
  -- pois o nome do constraint varia entre instalacoes.
  FOR r IN
    SELECT con.conname
    FROM pg_constraint con
    JOIN pg_class rel ON rel.oid = con.conrelid
    JOIN pg_attribute at ON at.attrelid = con.conrelid AND at.attname = 'tipo_documento'
    WHERE rel.relnamespace = 'plantaopro'::regnamespace
      AND rel.relname = 'adm360_documentos_recebidos'
      AND con.contype = 'c'
      AND con.conkey @> ARRAY[at.attnum]
      AND pg_get_constraintdef(con.oid) LIKE '%NFE_COMPLETA%'
  LOOP
    EXECUTE format('ALTER TABLE plantaopro.adm360_documentos_recebidos DROP CONSTRAINT %I', r.conname);
  END LOOP;

  SELECT count(*) > 0 INTO tem_novo
  FROM pg_constraint con
  JOIN pg_class rel ON rel.oid = con.conrelid
  WHERE rel.relnamespace = 'plantaopro'::regnamespace
    AND rel.relname = 'adm360_documentos_recebidos'
    AND con.contype = 'c'
    AND pg_get_constraintdef(con.oid) LIKE '%''NFS_E''%';

  IF NOT tem_novo THEN
    ALTER TABLE plantaopro.adm360_documentos_recebidos
      ADD CONSTRAINT ck_adm360_docrec_tipo_documento
      CHECK (tipo_documento IN ('NFE_COMPLETA', 'NFC_E', 'NFS_E', 'RESUMO'));
  END IF;
END $migration$;

-- ============================================================
-- Seção 73 — Administrativo360 Transmissão Tentativas B5 v2.21.3
-- ============================================================

-- SOURCE: database/migrations/2026_09_v2304_administrativo360_transmissao_tentativas.sql
-- SOURCE-SHA256: 37dcd938230badfecbd29f865fb56939966596a6d2f8d9f029bdef5f7c91729d
-- ============================================================================
-- Migration: 2026_09_v2304_administrativo360_transmissao_tentativas.sql
-- Objetivo: B5 do Administrativo 360 - Cotações e Transmissão.
--   1. Tabela adm360_cotacao_envios: tentativa PERSISTIDA de transmissão de
--      resposta de cotação (outbox de evidência). Cada envio gera exatamente
--      uma linha (UNIQUE tenant_id + resposta_id + tentativa) com:
--        * canal (provedor no momento da tentativa);
--        * status_transmissao: INICIADA enquanto a chamada externa ocorre e,
--          ao final, o mesmo vocabulário da resposta (EXPORTADA_MANUALMENTE,
--          CONFIGURACAO_PENDENTE, ACEITA_PELO_PORTAL, REJEITADA_PELO_PORTAL,
--          RESULTADO_DESCONHECIDO) ou INTERRUPTA quando a conciliação de
--          envios interrompidos (Adm360TransmissaoRecovery) fecha uma linha
--          INICIADA cuja resposta já saiu de ENVIANDO sem registro de
--          resultado (processo caiu entre a marcação e a finalização).
--      A linha é a evidência que impede retransmissão cega: o operador vê
--      quantas tentativas houve, por qual canal e com qual retorno antes de
--      confirmar nova transmissão a partir de RESULTADO_DESCONHECIDO.
--   2. A chamada externa do conector foi retirada da transação longa de
--      banco: marcação (INICIADA + ENVIANDO) e finalização (estado final +
--      arquivo imutável + evento) agora são duas transações curtas, com a
--      rede entre elas. A marcação usa SELECT ... FOR UPDATE para controle
--      de concorrência (duas transmissões simultâneas nunca ambas avançam).
-- Idempotencia: CREATE TABLE IF NOT EXISTS + CREATE INDEX IF NOT EXISTS;
--               pode ser reaplicada em qualquer ordem/replicação do banco.
-- ============================================================================

CREATE TABLE IF NOT EXISTS plantaopro.adm360_cotacao_envios (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL,
    resposta_id uuid NOT NULL REFERENCES plantaopro.adm360_cotacao_respostas(id),
    tentativa int NOT NULL CHECK (tentativa >= 1),
    canal varchar(40) NOT NULL,
    status_transmissao varchar(40) NOT NULL CHECK (status_transmissao IN (
        'INICIADA',
        'ENVIANDO',
        'EXPORTADA_MANUALMENTE',
        'CONFIGURACAO_PENDENTE',
        'ACEITA_PELO_PORTAL',
        'REJEITADA_PELO_PORTAL',
        'RESULTADO_DESCONHECIDO',
        'INTERRUPTA'
    )),
    protocolo_externo varchar(120),
    mensagem varchar(2000),
    iniciado_em timestamptz NOT NULL DEFAULT now(),
    finalizado_em timestamptz,
    created_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_adm360_cotacao_envios PRIMARY KEY (id),
    CONSTRAINT uq_adm360_cotacao_envios_tentativa UNIQUE (tenant_id, resposta_id, tentativa)
);

CREATE INDEX IF NOT EXISTS ix_adm360_envios_resposta ON plantaopro.adm360_cotacao_envios(resposta_id, tenant_id);
CREATE INDEX IF NOT EXISTS ix_adm360_envios_tenant_status ON plantaopro.adm360_cotacao_envios(tenant_id, status_transmissao);

COMMENT ON TABLE plantaopro.adm360_cotacao_envios IS
  'B5: tentativas persistidas de transmissão de respostas de cotação (evidência para idempotência, reconciliação e bloqueio de retransmissão cega de resultado desconhecido).';
COMMENT ON COLUMN plantaopro.adm360_cotacao_envios.tentativa IS
  'Nº sequencial da tentativa para a resposta (1 = primeira). UNIQUE com tenant+resposta garante uma linha por tentativa.';
COMMENT ON COLUMN plantaopro.adm360_cotacao_envios.canal IS
  'Provedor/canal utilizado na tentativa (IMPORTACAO_MANUAL, OPMENEXO, INPART ou conta não encontrada = IMPORTACAO_MANUAL).';
COMMENT ON COLUMN plantaopro.adm360_cotacao_envios.status_transmissao IS
  'INICIADA durante a chamada externa; estado final espelha o da resposta ou INTERRUPTA quando a conciliação fecha envio interrompido.';

-- ============================================================
-- Seção 74 — Administrativo360 Imutabilidade por Papel P0 v2.21.4
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2305_administrativo360_imutabilidade_por_papel.sql
-- SOURCE-SHA256: 4b4e31a9a37edd2670d2cb566b39a49cb278ff721efc5b1d47616693cc017ee4
-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2305_administrativo360_imutabilidade_por_papel
-- P0 Segurança: papel restrito para o bypass da imutabilidade do Administrativo 360.
--
-- Contexto (evidência empírica em PostgreSQL 18, testado em 2026-10-03 no banco
-- plantaopro_test com papel guc_probe NOLOGIN):
--   * O GUC plantao.bypass_imutabilidade_adm360 é parâmetro placeholder de 2
--     segmentos: qualquer não-superuser consegue SETá-lo livremente (SET_OK).
--   * GRANT/REVOKE ... SET ON PARAMETER plantao.bypass_imutabilidade_adm360 é
--     ACEITO na sintaxe PG18, mas REVOKE FROM PUBLIC não bloqueia o SET de um
--     não-superuser => o privilégio de parâmetro não protege o GUC.
--   * GRANT plantao ON DATABASE ... não existe ("tipo de privilégio desconhecido").
-- Solução (restrição dentro dos triggers, como manda o requisito P0):
--   1. Papel restrito plantaopro_maintenance (NOLOGIN NOINHERIT — marcador, não
--      concede privilégios em tabelas).
--   2. Função plantaopro.fn_adm360_bypass_habilitado(): bypass vale APENAS quando
--      GUC = 'on' E o session_user é superuser OU membro de plantaopro_maintenance.
--   3. As funções trigger v2301 (fn_adm360_evento_imutavel e
--      fn_adm360_vale_evento_progresso) passam a consultar a função restrita em
--      vez de ler o GUC diretamente — os triggers existentes permanecem os mesmos.
-- Uso: ferramentas oficiais de manutenção/migração continuam fazendo
-- SELECT set_config('plantao.bypass_imutabilidade_adm360','on',true) na MESMA
-- transação da escrita protegida; o usuário dessas ferramentas deve ser superuser
-- (dev/CI) ou membro de plantaopro_maintenance (produção).
-- Idempotência: DO bloqueando a criação do papel + CREATE OR REPLACE das funções.
-- ============================================================================

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'plantaopro_maintenance') THEN
        CREATE ROLE plantaopro_maintenance NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
    ELSE
        ALTER ROLE plantaopro_maintenance WITH NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE;
    END IF;
END $$;

COMMENT ON ROLE plantaopro_maintenance IS 'PlantaoPro: papel restrito do bypass de imutabilidade do Administrativo 360 (GUC plantao.bypass_imutabilidade_adm360). Marcador: não concede privilégios em tabelas.';

-- Bypass habilitado = GUC 'on' E session_user privilegiado (superuser ou membro do papel restrito).
CREATE OR REPLACE FUNCTION plantaopro.fn_adm360_bypass_habilitado()
RETURNS boolean
LANGUAGE sql
STABLE
AS $$
    SELECT coalesce(nullif(btrim(current_setting('plantao.bypass_imutabilidade_adm360', true)), ''), 'off') = 'on'
        AND (
            coalesce((SELECT rolsuper FROM pg_roles WHERE rolname = session_user), false)
            OR pg_has_role(session_user, 'plantaopro_maintenance', 'MEMBER')
        );
$$;

COMMENT ON FUNCTION plantaopro.fn_adm360_bypass_habilitado() IS 'P0: true somente quando GUC plantao.bypass_imutabilidade_adm360=''on'' E o session_user é superuser ou membro de plantaopro_maintenance (PG18 testado: privilégio de parâmetro não restringe GUCs placeholder de 2 segmentos).';

-- Recria a trigger de v2301 consultando a função restrita em vez do GUC bruto.
CREATE OR REPLACE FUNCTION plantaopro.fn_adm360_evento_imutavel()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF plantaopro.fn_adm360_bypass_habilitado() THEN
        RETURN COALESCE(NEW, OLD);
    END IF;
    RAISE EXCEPTION 'Evento do modulo Administrativo 360 e imutavel (append-only): UPDATE e DELETE nao permitidos (registro %).', COALESCE(NEW.id, OLD.id);
END;
$$;

-- Recria a trigger de v2301 consultando a função restrita em vez do GUC bruto.
CREATE OR REPLACE FUNCTION plantaopro.fn_adm360_vale_evento_progresso()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF plantaopro.fn_adm360_bypass_habilitado() THEN
        RETURN COALESCE(NEW, OLD);
    END IF;
    IF TG_OP = 'DELETE' THEN
        RAISE EXCEPTION 'Evento de vale e imutavel: DELETE nao permitido (registro %).', OLD.id;
    END IF;
    IF NEW.tenant_id IS DISTINCT FROM OLD.tenant_id
       OR NEW.vale_id IS DISTINCT FROM OLD.vale_id
       OR NEW.vale_item_id IS DISTINCT FROM OLD.vale_item_id
       OR NEW.tipo IS DISTINCT FROM OLD.tipo
       OR NEW.quantidade IS DISTINCT FROM OLD.quantidade
       OR NEW.data_evento IS DISTINCT FROM OLD.data_evento
       OR NEW.motivo IS DISTINCT FROM OLD.motivo
       OR NEW.movimento_id IS DISTINCT FROM OLD.movimento_id
       OR NEW.idempotency_key IS DISTINCT FROM OLD.idempotency_key
       OR NEW.registrado_por IS DISTINCT FROM OLD.registrado_por
       OR NEW.created_at IS DISTINCT FROM OLD.created_at
    THEN
        RAISE EXCEPTION 'Evento de vale e imutavel: apenas o progresso da decisao (quantidade_decidida) pode ser atualizado (registro %).', NEW.id;
    END IF;
    RETURN NEW;
END;
$$;

-- ============================================================
-- Seção 75 — Saúde 360 Plano Principal Único P1 v2.21.5
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2306_saude360_plano_principal_unico.sql
-- SOURCE-SHA256: 8efd39faf5a9b2eb8e7f2322e6eddf8010d909903ff7f190ee5e608acf783b73
-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2306_saude360_plano_principal_unico
-- P1 Saúde 360: garantia de banco para a troca ATÔMICA do plano principal.
--
--   1. Deduplicação: pacientes com mais de uma linha ativa marcada principal=true
--      mantêm apenas a mais recentemente atualizada (empate resolvido por id desc)
--      e as demais são despromovidas.
--   2. Índice parcial único garante no máximo 1 plano principal ativo por
--      paciente em nível de banco — a corrida de escrita residual (ex.: paciente
--      sem linhas anteriores) vira violação de constraint que o serviço converte
--      em 409.
-- Idempotência: o UPDATE só afeta linhas duplicadas (reaplicação sem dados novos
--      não altera nada); CREATE UNIQUE INDEX IF NOT EXISTS.
-- ============================================================================

WITH ranking AS (
    SELECT id,
           row_number() OVER (
               PARTITION BY cliente_id, paciente_id
               ORDER BY coalesce(updated_at, reg_date) DESC, id DESC
           ) AS rn
    FROM plantaopro.plano_saude_pacientes
    WHERE reg_status = 'A' AND principal = TRUE
)
UPDATE plantaopro.plano_saude_pacientes p
SET principal = FALSE, updated_at = now()
FROM ranking r
WHERE p.id = r.id AND r.rn > 1;

CREATE UNIQUE INDEX IF NOT EXISTS ux_plano_saude_pacientes_principal
    ON plantaopro.plano_saude_pacientes (paciente_id)
    WHERE principal = TRUE AND reg_status = 'A' AND paciente_id IS NOT NULL;

-- ============================================================
-- Seção 76 — IA Camada Canônica P2 v2.21.6
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2307_ai_camada_canonica.sql
-- SOURCE-SHA256: 47d5f9faaa95fa023673a5fe4484b8f874d44914ca9aed0bb12780f6e935eed0
-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2307_ai_camada_canonica
-- P2 IA: camada canônica de assistentes — configuração por tenant/tarefa
--        e auditoria de usos.
--
--   1. ai_config  : 1 linha por (tenant, tarefa). Habilitação por tenant,
--      provedor/modelo por tarefa, fallback explícito (destino aprovado),
--      chave do tenant cifrada no servidor (AES-GCM; o texto puro nunca é
--      gravado), limites de tokens/timeout/cota mensal e orçamento mensal.
--   2. ai_usos    : auditoria imutável de cada geração (sucesso ou falha):
--      quem, qual tarefa, qual provedor/modelo, tokens, duração, classe de
--      erro. Não guarda o texto do prompt nem da resposta (minimização:
--      apenas o identificador do contexto, nunca o conteúdo).
--
-- Idempotência: CREATE TABLE/INDEX IF NOT EXISTS; sem alteração de dados.
-- Sem FK de tenant: segue a convenção das demais tabelas operacionais
-- (ex.: tenant_modulos) que referenciam o tenant por uuid sem constraint.
-- ============================================================================

CREATE TABLE IF NOT EXISTS plantaopro.ai_config (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL,
    task_code text NOT NULL CHECK (task_code IN ('MEU_DIA_RESUMO', 'COTACAO_ANALISE')),
    habilitada boolean NOT NULL DEFAULT FALSE,
    provedor text NOT NULL CHECK (provedor IN ('groq', 'gemini', 'deepseek')),
    modelo text NOT NULL CHECK (char_length(modelo) BETWEEN 1 AND 64),
    fallback_provedor text CHECK (fallback_provedor IN ('groq', 'gemini', 'deepseek')),
    api_key_cifrada bytea,
    chave_mascara text,
    limite_tokens_entrada integer NOT NULL DEFAULT 4000 CHECK (limite_tokens_entrada BETWEEN 256 AND 16000),
    limite_tokens_saida integer NOT NULL DEFAULT 1200 CHECK (limite_tokens_saida BETWEEN 64 AND 8000),
    timeout_s integer NOT NULL DEFAULT 30 CHECK (timeout_s BETWEEN 5 AND 120),
    cota_mensal_usos integer NOT NULL DEFAULT 100 CHECK (cota_mensal_usos BETWEEN 1 AND 100000),
    orcamento_mensal numeric(14, 2) CHECK (orcamento_mensal IS NULL OR orcamento_mensal >= 0),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (tenant_id, task_code)
);

CREATE TABLE IF NOT EXISTS plantaopro.ai_usos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL,
    user_id uuid,
    task_code text NOT NULL CHECK (task_code IN ('MEU_DIA_RESUMO', 'COTACAO_ANALISE')),
    contexto_tipo text,
    contexto_id uuid,
    provedor text,
    modelo text,
    status text NOT NULL CHECK (status IN ('SUCESSO', 'FALHA')),
    erro_classe text,
    tokens_entrada integer,
    tokens_saida integer,
    duracao_ms integer,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_ai_config_tenant ON plantaopro.ai_config (tenant_id);
CREATE INDEX IF NOT EXISTS ix_ai_usos_tenant_task_date ON plantaopro.ai_usos (tenant_id, task_code, created_at);
CREATE INDEX IF NOT EXISTS ix_ai_usos_tenant_date ON plantaopro.ai_usos (tenant_id, created_at);

-- ============================================================
-- Seção 77 — IA Governança de Modelos e Orçamento P2 v2.21.7
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2308_ai_governanca_modelos_orcamento.sql
-- SOURCE-SHA256: 08c97f9e2d9957e0ead8055a4abcc5ce934fd76f5bf96d1c8d0570c818600554
-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2308_ai_governanca_modelos_orcamento
-- Rodada 2 IA (Brief A.2) — governança dos provedores de IA:
--
--   1. ai_config: modelo próprio do fallback (não reusa o modelo do
--      provedor principal); orçamento mensal com moeda ISO-4217 explícita;
--      contadores mensais ATÔMICOS com rollover preguiçoso via
--      mes_referencia (ano*100+mês calculado no fuso America/Sao_Paulo):
--      usos_mes_atual (reserva de cota) e orcamento_usado_mes (custo).
--      A reserva e o reset de mês acontecem num único UPDATE condicional —
--      sem corrida entre SELECT/INSERT e sem perda de contagem em upgrade.
--   2. ai_usos: custo estimado/confirmado por chamada, flag de custo incerto
--      (ex.: timeout — o provedor pode ter processado), moeda e versão do
--      preço usado, carimbo de reconciliação; task_code estendido para
--      TESTAR_CONEXAO (testes de conexão também ficam auditados, fora da
--      cota das tarefas — a cota continua contando só SUCESSO das tarefas).
--   3. ai_precos_modelos: tabela versionada de preços por provedor/modelo
--      (por milhão de tokens), com seed a partir de documentação oficial
--      consultada em 2026-10-04. Modelo sem preço vigente → custo incerto
--      (a aplicação registra estimativa; o seed NÃO inventa preço).
--   4. ai_chamadas_ativas: slots distribuídos de concorrência (valem para
--      TODAS as instâncias do servidor, não apenas o processo): limpeza por
--      idade (>6 min) + inserção condicional; release explícito ao final de
--      cada chamada (finally).
--
-- Idempotência (contrato de reaplicação): ADD COLUMN IF NOT EXISTS,
-- DROP/ADD CONSTRAINT com nome fixo, CREATE ... IF NOT EXISTS, INSERT ...
-- ON CONFLICT DO NOTHING. Reaplicar sobre o schema atual não altera dados.
-- ============================================================================

-- 1) ai_config ---------------------------------------------------------------
ALTER TABLE plantaopro.ai_config
    ADD COLUMN IF NOT EXISTS fallback_modelo text,
    ADD COLUMN IF NOT EXISTS orcamento_mensal_moeda char(3) NOT NULL DEFAULT 'USD',
    ADD COLUMN IF NOT EXISTS mes_referencia integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS usos_mes_atual integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS orcamento_usado_mes numeric(18, 4) NOT NULL DEFAULT 0;

ALTER TABLE plantaopro.ai_config
    DROP CONSTRAINT IF EXISTS ai_config_fallback_modelo_check;
ALTER TABLE plantaopro.ai_config
    ADD CONSTRAINT ai_config_fallback_modelo_check
    CHECK (fallback_modelo IS NULL OR char_length(fallback_modelo) BETWEEN 1 AND 64);

ALTER TABLE plantaopro.ai_config
    DROP CONSTRAINT IF EXISTS ai_config_orcamento_mensal_moeda_check;
ALTER TABLE plantaopro.ai_config
    ADD CONSTRAINT ai_config_orcamento_mensal_moeda_check
    CHECK (orcamento_mensal_moeda ~ '^[A-Z]{3}$');

ALTER TABLE plantaopro.ai_config
    DROP CONSTRAINT IF EXISTS ai_config_usos_mes_atual_check;
ALTER TABLE plantaopro.ai_config
    ADD CONSTRAINT ai_config_usos_mes_atual_check CHECK (usos_mes_atual >= 0);

ALTER TABLE plantaopro.ai_config
    DROP CONSTRAINT IF EXISTS ai_config_orcamento_usado_mes_check;
ALTER TABLE plantaopro.ai_config
    ADD CONSTRAINT ai_config_orcamento_usado_mes_check CHECK (orcamento_usado_mes >= 0);

-- 2) ai_usos ------------------------------------------------------------------
ALTER TABLE plantaopro.ai_usos
    ADD COLUMN IF NOT EXISTS custo_estimado numeric(14, 4),
    ADD COLUMN IF NOT EXISTS custo_confirmado numeric(14, 4),
    ADD COLUMN IF NOT EXISTS custo_incerto boolean NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS moeda char(3) NOT NULL DEFAULT 'USD',
    ADD COLUMN IF NOT EXISTS preco_versao date,
    ADD COLUMN IF NOT EXISTS reconciliado_em timestamptz;

ALTER TABLE plantaopro.ai_usos
    DROP CONSTRAINT IF EXISTS ai_usos_custo_estimado_check;
ALTER TABLE plantaopro.ai_usos
    ADD CONSTRAINT ai_usos_custo_estimado_check
    CHECK (custo_estimado IS NULL OR custo_estimado >= 0);

ALTER TABLE plantaopro.ai_usos
    DROP CONSTRAINT IF EXISTS ai_usos_custo_confirmado_check;
ALTER TABLE plantaopro.ai_usos
    ADD CONSTRAINT ai_usos_custo_confirmado_check
    CHECK (custo_confirmado IS NULL OR custo_confirmado >= 0);

ALTER TABLE plantaopro.ai_usos
    DROP CONSTRAINT IF EXISTS ai_usos_task_code_check;
ALTER TABLE plantaopro.ai_usos
    ADD CONSTRAINT ai_usos_task_code_check
    CHECK (task_code IN ('MEU_DIA_RESUMO', 'COTACAO_ANALISE', 'TESTAR_CONEXAO'));

CREATE INDEX IF NOT EXISTS ix_ai_usos_incertos
    ON plantaopro.ai_usos (tenant_id, created_at)
    WHERE status = 'FALHA' AND custo_incerto AND reconciliado_em IS NULL;

-- 3) ai_precos_modelos ----------------------------------------------------------
CREATE TABLE IF NOT EXISTS plantaopro.ai_precos_modelos (
    provedor text NOT NULL CHECK (provedor IN ('groq', 'gemini', 'deepseek')),
    modelo text NOT NULL CHECK (char_length(modelo) BETWEEN 1 AND 64),
    moeda char(3) NOT NULL DEFAULT 'USD',
    preco_entrada_milhao numeric(14, 6) NOT NULL CHECK (preco_entrada_milhao >= 0),
    preco_saida_milhao numeric(14, 6) NOT NULL CHECK (preco_saida_milhao >= 0),
    versao_de date NOT NULL,
    versao_ate date,
    atualizado_em timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (provedor, modelo, versao_de)
);

CREATE INDEX IF NOT EXISTS ix_ai_precos_vigencia
    ON plantaopro.ai_precos_modelos (provedor, modelo, versao_de DESC);

-- Seed de preços por milhão de tokens (moeda USD), consultado em 2026-10-04:
--   Groq     : docs.groq.com (catálogo self-service; llama-* enterprise-only desde
--              2026-08-26 — última tarifa pública registrada tem versao_ate).
--   Gemini   : ai.google.dev/gemini-api/docs/pricing
--   DeepSeek : api-docs.deepseek.com (DeepSeek usa peak/off-peak; o seed registra
--              o PEAK — conservador — como preço vigente permanente).
INSERT INTO plantaopro.ai_precos_modelos
    (provedor, modelo, moeda, preco_entrada_milhao, preco_saida_milhao, versao_de, versao_ate)
VALUES
    -- Groq (self-service desde 2026-08-26):
    ('groq', 'gpt-oss-20b', 'USD', 0.075, 0.30, '2026-08-26', NULL),
    ('groq', 'gpt-oss-120b', 'USD', 0.15, 0.60, '2026-08-26', NULL),
    -- Última tarifa pública registrada (enterprise-only desde 2026-08-26 → sem preço vigente):
    ('groq', 'llama-3.3-70b-versatile', 'USD', 0.59, 0.79, '2026-01-01', '2026-08-25'),
    -- Gemini (tier base ≤ 200k tokens de entrada):
    ('gemini', 'gemini-2.5-flash', 'USD', 0.30, 2.50, '2025-06-17', NULL),
    ('gemini', 'gemini-2.5-flash-lite', 'USD', 0.10, 0.40, '2025-06-17', NULL),
    ('gemini', 'gemini-2.5-pro', 'USD', 1.25, 10.00, '2025-06-17', NULL),
    -- DeepSeek (preço peak; deepseek-chat/deepseek-reasoner aposentados em 2026-07-24):
    ('deepseek', 'deepseek-flash', 'USD', 0.30, 1.20, '2026-09-01', NULL),
    ('deepseek', 'deepseek-v4-pro', 'USD', 1.32, 3.96, '2026-09-01', NULL)
ON CONFLICT (provedor, modelo, versao_de) DO NOTHING;

-- 4) ai_chamadas_ativas ---------------------------------------------------------
CREATE TABLE IF NOT EXISTS plantaopro.ai_chamadas_ativas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL,
    task_code text NOT NULL,
    criado_em timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_ai_chamadas_ativas_criado
    ON plantaopro.ai_chamadas_ativas (criado_em);

-- ============================================================
-- Seção 78 — Administrativo360 Documentos XML A3 v2.21.8
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2309_administrativo360_documentos_xml_a3.sql
-- SOURCE-SHA256: 9516a4d9446a930284d7284781a649cfbf6691b038aef4191f5d6f99b9e4d957
-- ============================================================================
-- Migration: 2026_10_v2309_administrativo360_documentos_xml_a3.sql
-- Objetivo: WP-A3 — Centro de XML de documentos fiscais (Administrativo 360)
--
--   1. nome_arquivo varchar(255): persistir o nome do arquivo original no
--      import. O arquivo passa a ser conceito explícito, separado do
--      documento fiscal (chave de acesso) e do id interno (id). O conteúdo
--      segue guardado em xml_bytes/xml_hash (canônico p/ dedup e download);
--      esta coluna apenas identifica o arquivo recebido (linhas legadas
--      permanecem NULL).
--   2. CHECK tipo_documento estendido com 'ABRASF' (prescrição eletrônica):
--      a família ABRASF é reconhecida como família não-fiscal aceita por
--      este módulo (o destinatário NÃO é validado para essa família).
--
-- Idempotência: DO $$ com checagem em information_schema/pg_constraint.
-- ============================================================================

-- 1. Nome do arquivo original.
DO $migration$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'plantaopro'
      AND table_name = 'adm360_documentos_recebidos'
      AND column_name = 'nome_arquivo'
  ) THEN
    ALTER TABLE plantaopro.adm360_documentos_recebidos
      ADD COLUMN nome_arquivo varchar(255);
  END IF;
END $migration$;

COMMENT ON COLUMN plantaopro.adm360_documentos_recebidos.nome_arquivo IS
  'WP-A3: nome do arquivo XML original no import (opcional; linhas legadas NULL). O arquivo e o documento fiscal (chave_acesso) são conceitos distintos: o hash/dedup/download usam os bytes exatos, esta coluna só identifica o arquivo recebido.';

-- 2. CHECK tipo_documento estendido com ABRASF.
DO $migration$
DECLARE
  r record;
  tem_novo boolean;
BEGIN
  -- Remove qualquer CHECK antigo da coluna (definicao contendo NFE_COMPLETA),
  -- pois o nome do constraint varia entre instalacoes.
  FOR r IN
    SELECT con.conname
    FROM pg_constraint con
    JOIN pg_class rel ON rel.oid = con.conrelid
    JOIN pg_attribute at ON at.attrelid = con.conrelid AND at.attname = 'tipo_documento'
    WHERE rel.relnamespace = 'plantaopro'::regnamespace
      AND rel.relname = 'adm360_documentos_recebidos'
      AND con.contype = 'c'
      AND con.conkey @> ARRAY[at.attnum]
      AND pg_get_constraintdef(con.oid) LIKE '%NFE_COMPLETA%'
  LOOP
    EXECUTE format('ALTER TABLE plantaopro.adm360_documentos_recebidos DROP CONSTRAINT %I', r.conname);
  END LOOP;

  -- Só recria se nenhuma versao ja contiver ABRASF (upgrade em 2 passadas).
  SELECT count(*) > 0 INTO tem_novo
  FROM pg_constraint con
  JOIN pg_class rel ON rel.oid = con.conrelid
  WHERE rel.relnamespace = 'plantaopro'::regnamespace
    AND rel.relname = 'adm360_documentos_recebidos'
    AND con.contype = 'c'
    AND pg_get_constraintdef(con.oid) LIKE '%''ABRASF''%';

  IF NOT tem_novo THEN
    ALTER TABLE plantaopro.adm360_documentos_recebidos
      ADD CONSTRAINT ck_adm360_docrec_tipo_documento
      CHECK (tipo_documento IN ('NFE_COMPLETA', 'NFC_E', 'NFS_E', 'ABRASF', 'RESUMO'));
  END IF;
END $migration$;

-- ============================================================
-- Seção 79 — Administrativo360 Eventos Conferencia A3 v2.21.8
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2310_adm360_eventos_tipo_conferencia.sql
-- SOURCE-SHA256: 9177b33b502201c3f54c5d2483a354da5ce27f76c8da1a61407bfb8debd795f1
-- ============================================================================
-- Migration: 2026_10_v2310_adm360_eventos_tipo_conferencia.sql
-- Objetivo: WP-A3/G5 — habilitar o evento de conferencia autorizada de
--      documentos fiscais importados pelo Centro de XML (Administrativo 360).
--
--   O repositorio de documentos registra o evento 'CONFIRMACAO_CONFERENCIA'
--      em plantaopro.adm360_eventos quando um documento e confirmado pela
--      operacao (ConferirDocumentoAsync), habilitando o uso do documento no
--      gate de recebimento de estoque (ComprasRepository.ReceberAsync). O
--      CHECK atual da coluna tipo_evento apenas aceita os quatro tipos
--      operacionais legados; sem este ajuste a conferencia falha com a
--      violacao 23514 e o documento nunca chega ao estado CONFERIDO.
--
--   Nao altera a natureza append-only da tabela nem os demais tipos: o novo
--      valor so amplia o conjunto aceito.
--
-- Idempotencia: DO $migration$ — remove o CHECK legado da coluna (definicao
--      contendo 'APROVACAO', exceto a nova constraint nomeada) e so recria
--      se nenhuma versao ja contiver CONFIRMACAO_CONFERENCIA.
-- ============================================================================

DO $migration$
DECLARE
  r record;
  tem_novo boolean;
BEGIN
  -- Remove qualquer CHECK antigo da coluna (a definicao varia entre
  -- instalacoes por ter sido criada sem nome explicito).
  FOR r IN
    SELECT con.conname
    FROM pg_constraint con
    JOIN pg_class rel ON rel.oid = con.conrelid
    JOIN pg_attribute at ON at.attrelid = con.conrelid AND at.attname = 'tipo_evento'
    WHERE rel.relnamespace = 'plantaopro'::regnamespace
      AND rel.relname = 'adm360_eventos'
      AND con.contype = 'c'
      AND con.conkey @> ARRAY[at.attnum]
      AND pg_get_constraintdef(con.oid) LIKE '%''APROVACAO''%'
      AND pg_get_constraintdef(con.oid) NOT LIKE '%''CONFIRMACAO_CONFERENCIA''%'
      AND con.conname <> 'ck_adm360_eventos_tipo_evento'
  LOOP
    EXECUTE format('ALTER TABLE plantaopro.adm360_eventos DROP CONSTRAINT %I', r.conname);
  END LOOP;

  -- So recria se nenhuma versao ja contiver o novo valor (upgrade em 2 passadas).
  SELECT count(*) > 0 INTO tem_novo
  FROM pg_constraint con
  JOIN pg_class rel ON rel.oid = con.conrelid
  WHERE rel.relnamespace = 'plantaopro'::regnamespace
    AND rel.relname = 'adm360_eventos'
    AND con.contype = 'c'
    AND pg_get_constraintdef(con.oid) LIKE '%''CONFIRMACAO_CONFERENCIA''%';

  IF NOT tem_novo THEN
    ALTER TABLE plantaopro.adm360_eventos
      ADD CONSTRAINT ck_adm360_eventos_tipo_evento
      CHECK (tipo_evento IN ('APROVACAO', 'ARQUIVO', 'DECLARACAO_MANUAL', 'RETORNO_EXTERNO', 'CONFIRMACAO_CONFERENCIA'));
  END IF;
END $migration$;

-- ============================================================
-- Seção 80 — Administrativo360 Identidade Documental e Sequencia de Eventos WS-A3 v2.21.8
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2311_adm360_documentos_identidade_eventos.sql
-- SOURCE-SHA256: 5075bfe673fcf1d093b36eda354eb5f93287e2675af402da23599c2c4b85b477
-- ============================================================================
-- Migration: 2026_10_v2311_adm360_documentos_identidade_eventos.sql
-- WS-A3: identidade documental por familia fiscal + unicidade de sequencia_evento.
--
-- Alteracoes oficiais:
--   1) Reconciliacao EXPLICITA de duplicatas fiscais FORA de quarentena:
--      - agrupa pela identidade (tenant + emitente + modelo + numero + serie);
--      - apaga apenas copias BYTE-IDENTICAS (mesmo xml_hash) sem vinculos
--        financeiros/operacionais e sem eventos alheios a importacao;
--      - qualquer restante divergente levanta RAISE com diagnostico completo
--        (nao decide silenciosamente qual e o documento verdadeiro).
--   2) Unicidade fiscal parcial: UNIQUE(tenant, emitente, modelo, numero, serie)
--      WHERE quarentena = false. Documentos em quarentena (ex.: XML_MALFORMADO)
--      trazem identificadores extraidos sem fiabilidade e nao bloqueiam novas
--      importacoes; quando reentram ao ciclo sao validados pelo indice/codigo.
--   3) Renumera sequencia_evento por documento em 1..N (ROW_NUMBER sobre
--      data_evento, id), normaliza sha256_hash quando divergente da formula
--      canonica (a sequencia participa do conteudo hash — mesma formula do
--      trigger de v2301) e adiciona UNIQUE(tenant_id, documento_id, sequencia_evento).
--
-- O bypass de imutabilidade (GUC v2301) e ativado na MESMA transacao.
-- A migration deve ser executada dentro de uma transacao unica (padrao do
-- runner canonico: transactional=true; falha em qualquer pos-condicao = rollback).
-- Idempotente: IF NOT EXISTS/guardas por nome; reconciliacao converge;
-- renumeracao e no-op para sequencias ja contiguas.
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Pre-condicoes: tabelas e colunas esperadas devem existir.
-- ----------------------------------------------------------------------------
DO $v2311$
DECLARE
    n integer;
BEGIN
    IF to_regclass('plantaopro.adm360_documentos_recebidos') IS NULL
       OR to_regclass('plantaopro.adm360_documento_eventos') IS NULL THEN
        RAISE EXCEPTION 'v2311 pre-condicao falhou: faltam as tabelas adm360_documentos_recebidos e/ou adm360_documento_eventos';
    END IF;

    SELECT count(*) INTO n FROM information_schema.columns
    WHERE table_schema = 'plantaopro' AND table_name = 'adm360_documentos_recebidos'
      AND column_name IN ('tenant_id','emitente_cnpj','modelo','numero','serie','quarentena',
                          'xml_hash','chave_acesso','recebimento_id','pedido_id','titulo_pagar_id','updated_at');
    IF n <> 12 THEN
        RAISE EXCEPTION 'v2311 pre-condicao falhou: colunas esperadas em adm360_documentos_recebidos nao encontradas (%/12)', n;
    END IF;

    SELECT count(*) INTO n FROM information_schema.columns
    WHERE table_schema = 'plantaopro' AND table_name = 'adm360_documento_eventos'
      AND column_name IN ('tenant_id','documento_id','tipo_evento','sequencia_evento','sha256_hash','data_evento');
    IF n <> 6 THEN
        RAISE EXCEPTION 'v2311 pre-condicao falhou: colunas esperadas em adm360_documento_eventos nao encontradas (%/6)', n;
    END IF;
END
$v2311$;

-- Bypass de imutabilidade (padrao v2301), escopo da transacao atual: libera o
-- DELETE/UPDATE dos documentos/eventos durante reconciliacao e renumeracao.
SELECT set_config('plantao.bypass_imutabilidade_adm360', 'on', true);

-- ----------------------------------------------------------------------------
-- 1. Reconciliacao: remove copias duplicadas byte-identicas (a mais antiga
--    permanece como ancora) somente quando sem vinculos e sem historia alheia
--    a importacao. Copias divergentes NAO sao tocadas (passo 2 as diagnostica).
-- ----------------------------------------------------------------------------
DO $v2311$
DECLARE
    g record;
    n integer;
BEGIN
    FOR g IN
        SELECT tenant_id, emitente_cnpj, modelo, numero, serie
        FROM plantaopro.adm360_documentos_recebidos
        WHERE quarentena = false
        GROUP BY tenant_id, emitente_cnpj, modelo, numero, serie
        HAVING count(*) > 1
        ORDER BY tenant_id::text, emitente_cnpj, numero, serie
    LOOP
        DELETE FROM plantaopro.adm360_documentos_recebidos d
        USING (
            SELECT id, xml_hash
            FROM plantaopro.adm360_documentos_recebidos
            WHERE tenant_id = g.tenant_id
              AND emitente_cnpj = g.emitente_cnpj
              AND modelo = g.modelo AND numero = g.numero AND serie = g.serie
              AND quarentena = false
            ORDER BY created_at, id
            LIMIT 1
        ) ancora
        WHERE d.tenant_id = g.tenant_id
          AND d.emitente_cnpj = g.emitente_cnpj
          AND d.modelo = g.modelo AND d.numero = g.numero AND d.serie = g.serie
          AND d.quarentena = false
          AND d.id <> ancora.id
          AND d.xml_hash = ancora.xml_hash
          AND d.recebimento_id IS NULL
          AND d.pedido_id IS NULL
          AND d.titulo_pagar_id IS NULL
          AND NOT EXISTS (
                SELECT 1 FROM plantaopro.adm360_documento_eventos e
                WHERE e.documento_id = d.id AND e.tipo_evento <> 'IMPORTACAO_MANUAL');
        GET DIAGNOSTICS n = ROW_COUNT;
        IF n > 0 THEN
            RAISE NOTICE 'WS-A3/v2311: % copia(s) byte-identica(s) reconciliada(s) (emitente %, modelo %, numero %, serie %)',
                n, g.emitente_cnpj, g.modelo, g.numero, g.serie;
        END IF;
    END LOOP;
END
$v2311$;

-- ----------------------------------------------------------------------------
-- 2. Diagnostico: duplicatas divergentes restantes FORA de quarentena bloqueiam
--    o upgrade com laudo completo (executado ANTES da criacao do indice unico).
-- ----------------------------------------------------------------------------
DO $v2311$
DECLARE
    g record;
    n_dup integer;
    n_hashes integer;
    chaves text;
    tmin timestamptz;
    tmax timestamptz;
BEGIN
    FOR g IN
        SELECT tenant_id, emitente_cnpj, modelo, numero, serie
        FROM plantaopro.adm360_documentos_recebidos
        WHERE quarentena = false
        GROUP BY tenant_id, emitente_cnpj, modelo, numero, serie
        HAVING count(*) > 1
        ORDER BY tenant_id::text, emitente_cnpj, numero, serie
    LOOP
        SELECT count(*), count(DISTINCT xml_hash),
               string_agg(DISTINCT left(chave_acesso, 16), ', '),
               min(created_at), max(created_at)
        INTO n_dup, n_hashes, chaves, tmin, tmax
        FROM plantaopro.adm360_documentos_recebidos
        WHERE tenant_id = g.tenant_id
          AND emitente_cnpj = g.emitente_cnpj
          AND modelo = g.modelo AND numero = g.numero AND serie = g.serie
          AND quarentena = false;

        RAISE EXCEPTION 'WS-A3/v2311 conflito de identidade fiscal pendente de reconciliacao manual (emitente %, modelo %, numero %, serie %, tenant %): % registro(s), % hash(es) distinto(s), chaves: %, periodo % .. %. Quarentene as duplicadas ou corrija a extracao antes de reexecutar a migration.',
            g.emitente_cnpj, g.modelo, g.numero, g.serie, g.tenant_id,
            n_dup, n_hashes, chaves,
            tmin AT TIME ZONE 'UTC', tmax AT TIME ZONE 'UTC';
    END LOOP;
END
$v2311$;

-- ----------------------------------------------------------------------------
-- 3. Unicidade parcial de identidade fiscal (documentos em quarentena fora).
-- ----------------------------------------------------------------------------
CREATE UNIQUE INDEX IF NOT EXISTS ux_adm360_docrec_tenant_fiscal
    ON plantaopro.adm360_documentos_recebidos (tenant_id, emitente_cnpj, modelo, numero, serie)
    WHERE quarentena = false;

-- ----------------------------------------------------------------------------
-- 4. Renumera sequencias de eventos 1..N por documento (ordem canonica:
--    data_evento, id) e normaliza sha256_hash com a MESMA formula do trigger
--    de v2301 (a sequencia participa do conteudo hash).
-- ----------------------------------------------------------------------------
DO $v2311$
DECLARE
    alterados integer := 0;
    normalizados integer := 0;
BEGIN
    WITH alvo AS (
        SELECT e.id,
               row_number() OVER (PARTITION BY e.tenant_id, e.documento_id
                                  ORDER BY e.data_evento, e.id) AS nova_seq
        FROM plantaopro.adm360_documento_eventos e
    )
    UPDATE plantaopro.adm360_documento_eventos e
    SET sequencia_evento = a.nova_seq
    FROM alvo a
    WHERE e.id = a.id
      AND e.sequencia_evento <> a.nova_seq;
    GET DIAGNOSTICS alterados = ROW_COUNT;

    UPDATE plantaopro.adm360_documento_eventos e
    SET sha256_hash = encode(digest(
            coalesce(e.id::text, '') || '|' || coalesce(e.tenant_id::text, '') || '|' ||
            coalesce(e.tipo_evento, '') || '|' || coalesce(e.sequencia_evento::text, '') || '|' ||
            coalesce(e.descricao_evento, '') || '|' || coalesce(e.data_evento::text, '') || '|' ||
            coalesce(e.protocolo, '') || '|' || coalesce(e.detalhes, ''),
            'sha256'), 'hex')
    WHERE e.sha256_hash IS DISTINCT FROM encode(digest(
            coalesce(e.id::text, '') || '|' || coalesce(e.tenant_id::text, '') || '|' ||
            coalesce(e.tipo_evento, '') || '|' || coalesce(e.sequencia_evento::text, '') || '|' ||
            coalesce(e.descricao_evento, '') || '|' || coalesce(e.data_evento::text, '') || '|' ||
            coalesce(e.protocolo, '') || '|' || coalesce(e.detalhes, ''),
            'sha256'), 'hex');
    GET DIAGNOSTICS normalizados = ROW_COUNT;

    IF alterados > 0 OR normalizados > 0 THEN
        RAISE NOTICE 'WS-A3/v2311: % evento(s) renumerado(s), % hash(es) normalizado(s)', alterados, normalizados;
    END IF;
END
$v2311$;

-- ----------------------------------------------------------------------------
-- 5. Constraint de unicidade concorrente da sequencia (guarda por nome para
--    re-execucao idempotente; dados ja estao 1..N apos o passo 4).
-- ----------------------------------------------------------------------------
DO $v2311$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace ns ON ns.oid = rel.relnamespace
        WHERE ns.nspname = 'plantaopro'
          AND rel.relname = 'adm360_documento_eventos'
          AND con.conname = 'ux_adm360_doc_eventos_tenant_doc_seq'
    ) THEN
        ALTER TABLE plantaopro.adm360_documento_eventos
            ADD CONSTRAINT ux_adm360_doc_eventos_tenant_doc_seq
            UNIQUE (tenant_id, documento_id, sequencia_evento);
    END IF;
END
$v2311$;

-- ----------------------------------------------------------------------------
-- Pos-condicoes verificaveis (falha aborta a transacao inteira).
-- ----------------------------------------------------------------------------
DO $v2311$
DECLARE
    n integer;
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes
        WHERE schemaname = 'plantaopro' AND indexname = 'ux_adm360_docrec_tenant_fiscal'
    ) THEN
        RAISE EXCEPTION 'v2311 pos-condicao falhou: indice unico ux_adm360_docrec_tenant_fiscal ausente';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace ns ON ns.oid = rel.relnamespace
        WHERE ns.nspname = 'plantaopro'
          AND rel.relname = 'adm360_documento_eventos'
          AND con.conname = 'ux_adm360_doc_eventos_tenant_doc_seq'
    ) THEN
        RAISE EXCEPTION 'v2311 pos-condicao falhou: constraint ux_adm360_doc_eventos_tenant_doc_seq ausente';
    END IF;

    SELECT count(*) INTO n FROM (
        SELECT 1 FROM plantaopro.adm360_documentos_recebidos
        WHERE quarentena = false
        GROUP BY tenant_id, emitente_cnpj, modelo, numero, serie
        HAVING count(*) > 1
    ) x;
    IF n > 0 THEN
        RAISE EXCEPTION 'v2311 pos-condicao falhou: % identidade(s) fiscal(is) duplicada(s) fora de quarentena', n;
    END IF;

    SELECT count(*) INTO n FROM (
        SELECT 1 FROM plantaopro.adm360_documento_eventos
        GROUP BY tenant_id, documento_id, sequencia_evento
        HAVING count(*) > 1
    ) x;
    IF n > 0 THEN
        RAISE EXCEPTION 'v2311 pos-condicao falhou: sequencias de eventos de documento duplicadas';
    END IF;

    -- Continuidade 1..N por documento (ROW_NUMBER garante min=1 e max=count).
    SELECT count(*) INTO n FROM (
        SELECT 1 FROM plantaopro.adm360_documento_eventos e
        GROUP BY e.tenant_id, e.documento_id
        HAVING count(*) <> max(e.sequencia_evento)
    ) x;
    IF n > 0 THEN
        RAISE EXCEPTION 'v2311 pos-condicao falhou: sequencias de eventos de documento nao contiguas 1..N';
    END IF;

    -- Coerencia dos hashes com a formula canonica (v2301).
    SELECT count(*) INTO n
    FROM plantaopro.adm360_documento_eventos e
    WHERE e.sha256_hash IS DISTINCT FROM encode(digest(
            coalesce(e.id::text, '') || '|' || coalesce(e.tenant_id::text, '') || '|' ||
            coalesce(e.tipo_evento, '') || '|' || coalesce(e.sequencia_evento::text, '') || '|' ||
            coalesce(e.descricao_evento, '') || '|' || coalesce(e.data_evento::text, '') || '|' ||
            coalesce(e.protocolo, '') || '|' || coalesce(e.detalhes, ''),
            'sha256'), 'hex');
    IF n > 0 THEN
        RAISE EXCEPTION 'v2311 pos-condicao falhou: % evento(s) com sha256_hash divergente da formula canonica', n;
    END IF;

    RAISE NOTICE 'WS-A3/v2311: pos-condicoes verificadas (identidade fiscal unica fora de quarentena, sequencias 1..N, hashes coerentes)';
END
$v2311$;

-- ============================================================
-- Seção 81 — Tabelas Legado Gap Instalacao v2.21.9
-- ============================================================

-- SOURCE: database/schema/450_v2219_tabelas_legado_gap_instalacao.sql
-- SOURCE-SHA256: 9606092c662e98fd3c4d30df1cc7c1426e4ff9b2e6ce421c69dfbdd784b2a3fc
-- ============================================================================
-- PlantaoPro | Schema canonico de instalacao limpa: 450_v2219_tabelas_legado_gap_instalacao
-- Rodada 9 (r9i) - fechamento do debito de instalacao consolidada (auditoria 3.10).
--
-- Contexto: oito tabelas criadas por migrations legadas (antes da curadoria do
-- install-manifest) nunca foram incorporadas ao caminho de instalacao limpa,
-- quebrando as migrations subsequentes v2312-v2353 em banco novo:
--   plano_modulos              (2026_plantao_pro_self_service_white_label e similares)
--   plano_recursos             (2026_plantao_pro_saas_comercial_lgpd_jornada e similares)
--   lgpd_consentimentos        (20260607_saas_lgpd_jornada_comercial_ajuda e similares)
--   v116_faturamento_lote_itens (2026_v116_consolidacao_operacional_final)
--   cid_tabela                 (2026_fix_cid_tabela_schema / saude360_cid_*)
--   clinica_lancamentos        (2026_plantao_pro_saude360_modulos_clinicos / 2026_saude360_financeiro_clinica)
--   clinica_estornos           (2026_saude360_financeiro_clinica)
--   convenio_autorizacoes      (2026_plantao_pro_saude360_modulos_clinicos / 2026_saude360_convenios_planos_saude)
--
-- Conteudo: snapshot de schema (colunas, constraints, indices e sequencias)
-- gerado do catalogo do banco de homologacao em 2026-10-15 (pg_attribute,
-- pg_constraint/pg_get_constraintdef, pg_index, pg_depend). As migrations
-- v2315/v2324/v2325/v2331/v2350/v2351 sao aditivas e idempotentes (ADD COLUMN IF
-- NOT EXISTS / DROP CONSTRAINT IF EXISTS), portanto convergem sobre este
-- snapshot sem conflito em banco novo nem legado.
--
-- Idempotencia: CREATE TABLE/SEQUENCE/INDEX IF NOT EXISTS; seguro dentro da
-- transacao unica do comando install. Inclui bloco final de garantia dos
-- codigos ADM360/USUARIOS exigidos pelos v2322/v2329 ("garantia codigos").
-- ============================================================================

CREATE TABLE IF NOT EXISTS plantaopro.plano_modulos (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    plano_id uuid NOT NULL,
    modulo_id uuid,
    codigo_modulo text NOT NULL,
    habilitado boolean NOT NULL DEFAULT true,
    status text NOT NULL DEFAULT 'ATIVO'::text,
    reg_date timestamp without time zone NOT NULL DEFAULT now(),
    reg_update timestamp without time zone,
    reg_status character(1) NOT NULL DEFAULT 'A'::bpchar,
    tenant_id uuid,
    cliente_id uuid,
    parceiro_id uuid,
    metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
    incluido boolean NOT NULL DEFAULT true,
    limite integer,
    preco_adicional numeric(14,2),
    PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_plano_modulos_cliente_id ON plantaopro.plano_modulos USING btree (cliente_id);

CREATE INDEX IF NOT EXISTS ix_plano_modulos_plano_status_regdate ON plantaopro.plano_modulos USING btree (plano_id, status, reg_date);

CREATE INDEX IF NOT EXISTS ix_plano_modulos_reg_date ON plantaopro.plano_modulos USING btree (reg_date DESC);

CREATE INDEX IF NOT EXISTS ix_plano_modulos_status ON plantaopro.plano_modulos USING btree (status);

CREATE INDEX IF NOT EXISTS ix_plano_modulos_tenant_id ON plantaopro.plano_modulos USING btree (tenant_id);

CREATE UNIQUE INDEX IF NOT EXISTS ux_plano_modulos_ativo ON plantaopro.plano_modulos USING btree (plano_id, modulo_id) WHERE ((reg_status = 'A'::bpchar) AND (modulo_id IS NOT NULL));

CREATE TABLE IF NOT EXISTS plantaopro.plano_recursos (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    plano_id uuid NOT NULL,
    recurso character varying(80) NOT NULL,
    habilitado boolean NOT NULL DEFAULT true,
    limite integer,
    reg_status character(1) NOT NULL DEFAULT 'A'::bpchar,
    reg_date timestamp without time zone NOT NULL DEFAULT now(),
    reg_update timestamp without time zone,
    codigo character varying(80),
    tenant_id uuid,
    cliente_id uuid,
    parceiro_id uuid,
    status character varying(40) NOT NULL DEFAULT 'ATIVO'::character varying,
    metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
    nome character varying(160),
    descricao text,
    FOREIGN KEY (plano_id) REFERENCES plantaopro.planos(id),
    PRIMARY KEY (id),
    UNIQUE (plano_id, codigo)
);

CREATE INDEX IF NOT EXISTS ix_plano_recursos_cliente_id ON plantaopro.plano_recursos USING btree (cliente_id);

CREATE INDEX IF NOT EXISTS ix_plano_recursos_plano_codigo ON plantaopro.plano_recursos USING btree (plano_id, codigo, reg_status);

CREATE INDEX IF NOT EXISTS ix_plano_recursos_reg_date ON plantaopro.plano_recursos USING btree (reg_date DESC);

CREATE INDEX IF NOT EXISTS ix_plano_recursos_status ON plantaopro.plano_recursos USING btree (status);

CREATE INDEX IF NOT EXISTS ix_plano_recursos_tenant_id ON plantaopro.plano_recursos USING btree (tenant_id);

CREATE TABLE IF NOT EXISTS plantaopro.lgpd_consentimentos (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    usuario_id uuid,
    finalidade character varying(160) NOT NULL,
    base_legal character varying(120) NOT NULL,
    versao_politica character varying(30) NOT NULL,
    consentido boolean NOT NULL DEFAULT true,
    ip character varying(80),
    user_agent text,
    reg_status character(1) NOT NULL DEFAULT 'A'::bpchar,
    reg_date timestamp without time zone NOT NULL DEFAULT now(),
    reg_update timestamp without time zone,
    cliente_id uuid,
    tenant_id uuid,
    parceiro_id uuid,
    plano_id uuid,
    status character varying(40) NOT NULL DEFAULT 'ATIVO'::character varying,
    metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
    titular_email text,
    politica_id uuid,
    aceito boolean,
    origem text,
    ip_origem text,
    PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_lgpd_consentimentos_cliente_id ON plantaopro.lgpd_consentimentos USING btree (cliente_id);

CREATE INDEX IF NOT EXISTS ix_lgpd_consentimentos_reg_date ON plantaopro.lgpd_consentimentos USING btree (reg_date DESC);

CREATE INDEX IF NOT EXISTS ix_lgpd_consentimentos_status ON plantaopro.lgpd_consentimentos USING btree (status);

CREATE INDEX IF NOT EXISTS ix_lgpd_consentimentos_tenant_cliente_status_regdate ON plantaopro.lgpd_consentimentos USING btree (tenant_id, cliente_id, reg_status, reg_date);

CREATE INDEX IF NOT EXISTS ix_lgpd_consentimentos_tenant_id ON plantaopro.lgpd_consentimentos USING btree (tenant_id);

CREATE INDEX IF NOT EXISTS ix_lgpd_consentimentos_usuario ON plantaopro.lgpd_consentimentos USING btree (usuario_id, finalidade);

CREATE INDEX IF NOT EXISTS ix_lgpd_consentimentos_usuario_finalidade ON plantaopro.lgpd_consentimentos USING btree (usuario_id, finalidade, reg_date DESC);

CREATE INDEX IF NOT EXISTS ix_lgpd_consentimentos_usuario_reg_date ON plantaopro.lgpd_consentimentos USING btree (usuario_id, reg_date DESC);

CREATE TABLE IF NOT EXISTS plantaopro.v116_faturamento_lote_itens (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status character varying(40) NOT NULL DEFAULT 'ATIVO'::character varying,
    descricao text,
    status_operacional character varying(60),
    valor numeric(14,2),
    data_referencia timestamp with time zone,
    payload_demo jsonb,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    created_by character varying(120),
    updated_at timestamp with time zone,
    updated_by character varying(120),
    lote_id uuid,
    PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_v116_faturamento_lote_itens_tenant_status_data ON plantaopro.v116_faturamento_lote_itens USING btree (tenant_id, reg_status, created_at);

CREATE INDEX IF NOT EXISTS ix_v116_lote_itens_tenant_lote ON plantaopro.v116_faturamento_lote_itens USING btree (tenant_id, lote_id);

CREATE TABLE IF NOT EXISTS plantaopro.cid_tabela (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    cliente_id uuid,
    tenant_id uuid,
    codigo text NOT NULL,
    descricao text NOT NULL,
    categoria text NOT NULL DEFAULT ''::text,
    status text NOT NULL DEFAULT 'ATIVO'::text,
    created_by uuid,
    reg_date timestamp with time zone NOT NULL DEFAULT now(),
    reg_status character(1) NOT NULL DEFAULT 'A'::bpchar,
    capitulo text NOT NULL DEFAULT ''::text,
    updated_by uuid,
    reg_update timestamp without time zone,
    updated_at timestamp without time zone,
    data_fim timestamp with time zone,
    data_inicio timestamp with time zone,
    versao text NOT NULL DEFAULT 'CID-10'::text,
    descricao_normalizada text NOT NULL DEFAULT ''::text,
    capitulo_codigo text NOT NULL DEFAULT ''::text,
    capitulo_nome text NOT NULL DEFAULT ''::text,
    grupo_codigo text NOT NULL DEFAULT ''::text,
    grupo_nome text NOT NULL DEFAULT ''::text,
    fonte text NOT NULL DEFAULT ''::text,
    fonte_url text NOT NULL DEFAULT ''::text,
    data_importacao timestamp without time zone NOT NULL DEFAULT now(),
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_cid_tabela_busca ON plantaopro.cid_tabela USING btree (codigo, descricao);

CREATE INDEX IF NOT EXISTS ix_cid_tabela_capitulo_codigo ON plantaopro.cid_tabela USING btree (capitulo_codigo);

CREATE INDEX IF NOT EXISTS ix_cid_tabela_cliente_id ON plantaopro.cid_tabela USING btree (cliente_id);

CREATE INDEX IF NOT EXISTS ix_cid_tabela_codigo ON plantaopro.cid_tabela USING btree (codigo);

CREATE INDEX IF NOT EXISTS ix_cid_tabela_descricao ON plantaopro.cid_tabela USING btree (descricao);

CREATE INDEX IF NOT EXISTS ix_cid_tabela_descricao_normalizada ON plantaopro.cid_tabela USING btree (descricao_normalizada);

CREATE INDEX IF NOT EXISTS ix_cid_tabela_grupo_codigo ON plantaopro.cid_tabela USING btree (grupo_codigo);

CREATE INDEX IF NOT EXISTS ix_cid_tabela_reg_date ON plantaopro.cid_tabela USING btree (reg_date);

CREATE INDEX IF NOT EXISTS ix_cid_tabela_reg_status ON plantaopro.cid_tabela USING btree (reg_status);

CREATE INDEX IF NOT EXISTS ix_cid_tabela_status ON plantaopro.cid_tabela USING btree (status);

CREATE INDEX IF NOT EXISTS ix_cid_tabela_versao ON plantaopro.cid_tabela USING btree (versao);

CREATE INDEX IF NOT EXISTS ix_saude360_cid_codigo ON plantaopro.cid_tabela USING btree (codigo);

CREATE INDEX IF NOT EXISTS ix_saude360_cid_descricao ON plantaopro.cid_tabela USING btree (descricao);

CREATE UNIQUE INDEX IF NOT EXISTS ux_cid_tabela_versao_codigo_ativo ON plantaopro.cid_tabela USING btree (versao, upper(codigo)) WHERE ((reg_status = 'A'::bpchar) AND (codigo <> ''::text));

CREATE TABLE IF NOT EXISTS plantaopro.clinica_lancamentos (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    cliente_id uuid,
    caixa_id uuid,
    tipo text NOT NULL,
    descricao text NOT NULL,
    valor numeric(12,2) NOT NULL,
    status text NOT NULL DEFAULT 'REGISTRADO'::text,
    created_by uuid,
    updated_by uuid,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    updated_at timestamp with time zone,
    reg_date timestamp with time zone NOT NULL DEFAULT now(),
    reg_status character(1) NOT NULL DEFAULT 'A'::bpchar,
    PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_clinica_lancamentos_cliente_caixa ON plantaopro.clinica_lancamentos USING btree (cliente_id, caixa_id);

CREATE INDEX IF NOT EXISTS ix_clinica_lancamentos_cliente_id ON plantaopro.clinica_lancamentos USING btree (cliente_id);

CREATE INDEX IF NOT EXISTS ix_clinica_lancamentos_reg_date ON plantaopro.clinica_lancamentos USING btree (reg_date);

CREATE INDEX IF NOT EXISTS ix_clinica_lancamentos_reg_status ON plantaopro.clinica_lancamentos USING btree (reg_status);

CREATE INDEX IF NOT EXISTS ix_clinica_lancamentos_status ON plantaopro.clinica_lancamentos USING btree (status);

CREATE TABLE IF NOT EXISTS plantaopro.clinica_estornos (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    recebimento_id uuid,
    motivo text NOT NULL DEFAULT ''::text,
    valor numeric(14,2) NOT NULL DEFAULT 0,
    status text NOT NULL DEFAULT 'ESTORNADO'::text,
    reg_date timestamp without time zone NOT NULL DEFAULT now(),
    reg_status character(1) NOT NULL DEFAULT 'A'::bpchar,
    PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_clinica_estornos_recebimento ON plantaopro.clinica_estornos USING btree (recebimento_id);

CREATE TABLE IF NOT EXISTS plantaopro.convenio_autorizacoes (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    paciente_id uuid,
    convenio_id uuid,
    plano_saude_id uuid,
    agendamento_id uuid,
    consulta_id uuid,
    numero_guia text NOT NULL DEFAULT ''::text,
    senha_autorizacao text NOT NULL DEFAULT ''::text,
    procedimento text NOT NULL DEFAULT ''::text,
    procedimento_id uuid,
    motivo text NOT NULL DEFAULT ''::text,
    valor_autorizado numeric(14,2) NOT NULL DEFAULT 0,
    status text NOT NULL DEFAULT 'PENDENTE'::text,
    motivo_negativa text NOT NULL DEFAULT ''::text,
    validade date,
    created_by uuid,
    updated_by uuid,
    created_at timestamp without time zone NOT NULL DEFAULT now(),
    updated_at timestamp without time zone,
    reg_date timestamp without time zone NOT NULL DEFAULT now(),
    reg_update timestamp without time zone,
    reg_status character(1) NOT NULL DEFAULT 'A'::bpchar,
    PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_convenio_autorizacoes_agendamento_id ON plantaopro.convenio_autorizacoes USING btree (agendamento_id);

CREATE INDEX IF NOT EXISTS ix_convenio_autorizacoes_cliente_consulta ON plantaopro.convenio_autorizacoes USING btree (cliente_id, consulta_id);

CREATE INDEX IF NOT EXISTS ix_convenio_autorizacoes_cliente_id ON plantaopro.convenio_autorizacoes USING btree (cliente_id);

CREATE INDEX IF NOT EXISTS ix_convenio_autorizacoes_consulta_id ON plantaopro.convenio_autorizacoes USING btree (consulta_id);

CREATE INDEX IF NOT EXISTS ix_convenio_autorizacoes_convenio_id ON plantaopro.convenio_autorizacoes USING btree (convenio_id);

CREATE INDEX IF NOT EXISTS ix_convenio_autorizacoes_paciente_id ON plantaopro.convenio_autorizacoes USING btree (paciente_id);

CREATE INDEX IF NOT EXISTS ix_convenio_autorizacoes_reg_date ON plantaopro.convenio_autorizacoes USING btree (reg_date);

CREATE INDEX IF NOT EXISTS ix_convenio_autorizacoes_reg_status ON plantaopro.convenio_autorizacoes USING btree (reg_status);

CREATE INDEX IF NOT EXISTS ix_convenio_autorizacoes_status ON plantaopro.convenio_autorizacoes USING btree (status);

-- ============================================================================
-- Garantia dos codigos de permissao de instalacao limpa. Este bloco
-- garante os codigos exigidos pelos sanity-checks do v2322 (grants
-- ADMINISTRADOR_CLIENTE ADM360) e do v2329 (grants ADMINISTRADOR_CLIENTE
-- USUARIOS). Em instalacao limpa o seeder canonico (v2198 / seeds/system)
-- cobre apenas parte deles e o restante (v2338) so existe depois do v2322 na
-- ordem de versao; nos bancos legados esses codigos vieram dos scripts
-- consolidados pre-manifest. Bloco idempotente: insere somente o que falta
-- (dedupe por codigo normalizado ':' -> '.', mesmo padrao dos sanity-checks),
-- usando o formato canonico com dois-pontos, o modulo ADM360 garantido pelo
-- v2197 e as acoes/modulo USUARIOS canonicos dos seeds/system.
-- ============================================================================
DO $garantia_codigos$
DECLARE
    v_acao_acessar_id uuid;
    v_acao_editar_id uuid;
    v_acao_usr_id uuid;
    v_modulo_adm_id uuid;
    v_modulo_usr_id uuid;
    v_suffix text;
BEGIN
    select id into v_acao_acessar_id from plantaopro.acoes_sistema where codigo in ('ACESSAR', 'LISTAR') order by id limit 1;
    if v_acao_acessar_id is null then
        v_acao_acessar_id := gen_random_uuid();
        insert into plantaopro.acoes_sistema(id, codigo, nome, status, reg_status, reg_date)
        values (v_acao_acessar_id, 'ACESSAR', 'Acessar', 'ATIVO', 'A', now());
    end if;

    select id into v_acao_editar_id from plantaopro.acoes_sistema where codigo in ('EDITAR', 'SALVAR') order by id limit 1;
    if v_acao_editar_id is null then
        v_acao_editar_id := gen_random_uuid();
        insert into plantaopro.acoes_sistema(id, codigo, nome, status, reg_status, reg_date)
        values (v_acao_editar_id, 'EDITAR', 'Editar', 'ATIVO', 'A', now());
    end if;

    select id into v_modulo_adm_id from plantaopro.modulos_sistema where upper(btrim(codigo)) = 'ADM360' and reg_status = 'A' limit 1;

    foreach v_suffix in array array[
        'VER',
        'EXPORTAR',
        'IMPORTAR_XML',
        'CRIAR',
        'EDITAR',
        'CONFIGURAR',
        'REABRIR',
        'CONFIRMAR',
        'CANCELAR',
        'CONFERIR',
        'MANIFESTAR_DFE',
        'VINCULAR_DOCUMENTOS',
        'TRANSMITIR_RESPOSTA'
    ] loop
        if not exists (
            select 1 from plantaopro.permissoes
             where replace(upper(btrim(codigo)), ':', '.') = ('ADM360.' || v_suffix)
               and reg_status = 'A'
        ) then
            insert into plantaopro.permissoes (id, acao_id, modulo_id, codigo, nome, status, reg_status, reg_date)
            values (gen_random_uuid(),
                    case when v_suffix = 'VER' then v_acao_acessar_id else v_acao_editar_id end,
                    v_modulo_adm_id, 'ADM360:' || v_suffix, replace('ADM360:' || v_suffix, ':', ' - '), 'ATIVO', 'A', now());
        end if;
    end loop;

    select id into v_modulo_usr_id from plantaopro.modulos_sistema where upper(btrim(codigo)) = 'USUARIOS' and reg_status = 'A' limit 1;
    foreach v_suffix in array array['VER', 'CRIAR', 'EDITAR', 'CONVIDAR'] loop
        if not exists (
            select 1 from plantaopro.permissoes
             where replace(upper(btrim(codigo)), ':', '.') = ('USUARIOS.' || v_suffix)
               and reg_status = 'A'
        ) then
            select id into v_acao_usr_id from plantaopro.acoes_sistema where codigo = v_suffix and reg_status = 'A' order by id limit 1;
            if v_acao_usr_id is null then
                v_acao_usr_id := gen_random_uuid();
                insert into plantaopro.acoes_sistema(id, codigo, nome, status, reg_status, reg_date)
                values (v_acao_usr_id, v_suffix, v_suffix, 'ATIVO', 'A', now());
            end if;
            insert into plantaopro.permissoes (id, acao_id, modulo_id, codigo, nome, status, reg_status, reg_date)
            values (gen_random_uuid(), v_acao_usr_id, v_modulo_usr_id, 'USUARIOS:' || v_suffix, 'USUARIOS ' || v_suffix, 'ATIVO', 'A', now());
        end if;
    end loop;
END
$garantia_codigos$;

-- ============================================================
-- Seção 82 — Colunas Convergencia Instalacao v2.21.9
-- ============================================================

-- SOURCE: database/schema/460_v2219_colunas_convergencia_instalacao.sql
-- SOURCE-SHA256: 92e665682e06e1d4bcdcddd0b66da475d10a6be1581a6944892cf74f0551abc9
-- ============================================================================
-- PlantaoPro | Convergencia de colunas de instalacao limpa: 460_v2219_colunas_convergencia_instalacao
-- Rodada 9 (r9i) - fechamento do debito de instalacao consolidada (auditoria 3.10).
--
-- Contexto: a base canonica de instalacao foi curada para uma forma enxuta,
-- enquanto o banco homologado (que percorreu todas as migrations legadas ate
-- v2353) carrega, em 66 tabelas, as colunas de escopo/auditoria
-- (tenant_id/cliente_id/parceiro_id/plano_id/metadata/reg_date/reg_update/
-- reg_status/created_at/updated_at/created_by/updated_by) e campos de dominio
-- que o codigo e as migrations v2312-v2353 consomem. Sem essa convergencia, a
-- instalacao limpa quebra em v2333 (a.reg_status) e o runtime perderia colunas.
--
-- Conteudo: ADD COLUMN IF NOT EXISTS idempotente para cada coluna presente no
-- homologado e ausente na instalacao limpa (fora das que o lote novo ja
-- adiciona). Regra de seguranca: NOT NULL e mantido somente quando ha DEFAULT
-- (linhas existentes herdariam o valor); colunas NOT NULL sem default sao
-- adicionadas como NULLABLE para nunca falhar em tabela ja populada.
-- Gerado do catalogo do banco homologado em 2026-10-15 (pg_attribute).
-- ============================================================================

alter table if exists plantaopro.acoes_sistema add column if not exists cliente_id uuid;
alter table if exists plantaopro.acoes_sistema add column if not exists metadata jsonb default '{}'::jsonb not null;
alter table if exists plantaopro.acoes_sistema add column if not exists parceiro_id uuid;
alter table if exists plantaopro.acoes_sistema add column if not exists plano_id uuid;
alter table if exists plantaopro.acoes_sistema add column if not exists tenant_id uuid;
alter table if exists plantaopro.agendamento_checkins add column if not exists created_at timestamp with time zone default now() not null;
alter table if exists plantaopro.agendamento_checkins add column if not exists updated_at timestamp with time zone;
alter table if exists plantaopro.agendamentos add column if not exists convenio_id uuid;
alter table if exists plantaopro.agendamentos add column if not exists created_at timestamp with time zone default now() not null;
alter table if exists plantaopro.agendamentos add column if not exists created_by uuid;
alter table if exists plantaopro.agendamentos add column if not exists especialidade text;
alter table if exists plantaopro.agendamentos add column if not exists especialidade_id uuid;
alter table if exists plantaopro.agendamentos add column if not exists hospital_id uuid;
alter table if exists plantaopro.agendamentos add column if not exists motivo_cancelamento text default ''::text not null;
alter table if exists plantaopro.agendamentos add column if not exists observacoes text default ''::text not null;
alter table if exists plantaopro.agendamentos add column if not exists plano_saude_id uuid;
alter table if exists plantaopro.agendamentos add column if not exists reg_update timestamp without time zone;
alter table if exists plantaopro.agendamentos add column if not exists sala_id uuid;
alter table if exists plantaopro.agendamentos add column if not exists tipo text default 'CONSULTA'::text not null;
alter table if exists plantaopro.agendamentos add column if not exists updated_at timestamp with time zone;
alter table if exists plantaopro.agendamentos add column if not exists updated_by uuid;
alter table if exists plantaopro.agendamentos add column if not exists valor numeric(12,2) default 0 not null;
alter table if exists plantaopro.api_error_logs add column if not exists user_id uuid;
alter table if exists plantaopro.assinatura_bloqueios add column if not exists cliente_id uuid;
alter table if exists plantaopro.assinatura_bloqueios add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.assinatura_bloqueios add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.assinatura_historico add column if not exists assinatura_id uuid;
alter table if exists plantaopro.assinatura_historico add column if not exists cliente_id uuid;
alter table if exists plantaopro.assinatura_historico add column if not exists reg_date timestamp with time zone default now() not null;
alter table if exists plantaopro.assinatura_historico add column if not exists status_novo text;
alter table if exists plantaopro.assinatura_modulos add column if not exists cliente_id uuid;
alter table if exists plantaopro.assinatura_modulos add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.assinatura_modulos add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.assinatura_uso add column if not exists cliente_id uuid;
alter table if exists plantaopro.assinatura_uso add column if not exists competencia date;
alter table if exists plantaopro.assinatura_uso add column if not exists quantidade integer default 0 not null;
alter table if exists plantaopro.assinatura_uso add column if not exists recurso character varying(80);
alter table if exists plantaopro.assinatura_uso add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.assinatura_uso add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.assinaturas add column if not exists cliente_id uuid;
alter table if exists plantaopro.assinaturas add column if not exists data_fim date default (CURRENT_DATE + '1 mon'::interval) not null;
alter table if exists plantaopro.assinaturas add column if not exists data_suspensao timestamp with time zone;
alter table if exists plantaopro.assinaturas add column if not exists data_trial_fim date;
alter table if exists plantaopro.assinaturas add column if not exists dia_vencimento integer default 10 not null;
alter table if exists plantaopro.assinaturas add column if not exists motivo_suspensao text;
alter table if exists plantaopro.assinaturas add column if not exists periodicidade character varying(30) default 'MENSAL'::character varying not null;
alter table if exists plantaopro.assinaturas add column if not exists plano_id uuid;
alter table if exists plantaopro.assinaturas add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.assinaturas add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.assinaturas add column if not exists reg_update timestamp without time zone;
alter table if exists plantaopro.assinaturas add column if not exists renovacao_automatica boolean default true not null;
alter table if exists plantaopro.assinaturas add column if not exists valor_contratado numeric(12,2) default 0 not null;
alter table if exists plantaopro.assinaturas add column if not exists valor_mensal numeric(12,2) default 0 not null;
alter table if exists plantaopro.auditoria_eventos add column if not exists acao character varying(120);
alter table if exists plantaopro.auditoria_eventos add column if not exists cliente_id uuid;
alter table if exists plantaopro.auditoria_eventos add column if not exists reg_date timestamp with time zone default now() not null;
alter table if exists plantaopro.clientes add column if not exists cnpj character varying(30);
alter table if exists plantaopro.clientes add column if not exists data_cancelamento timestamp with time zone;
alter table if exists plantaopro.clientes add column if not exists data_inicio_contrato date;
alter table if exists plantaopro.clientes add column if not exists data_suspensao timestamp with time zone;
alter table if exists plantaopro.clientes add column if not exists motivo_cancelamento text;
alter table if exists plantaopro.clientes add column if not exists motivo_suspensao text;
alter table if exists plantaopro.clientes add column if not exists nome_fantasia character varying(200);
alter table if exists plantaopro.clientes add column if not exists observacoes text;
alter table if exists plantaopro.clientes add column if not exists razao_social character varying(200);
alter table if exists plantaopro.clientes add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.clientes add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.clientes add column if not exists reg_update timestamp without time zone;
alter table if exists plantaopro.clientes add column if not exists responsavel_email character varying(180);
alter table if exists plantaopro.clientes add column if not exists responsavel_nome character varying(180);
alter table if exists plantaopro.clientes add column if not exists responsavel_telefone character varying(40);
alter table if exists plantaopro.clientes add column if not exists saude_score integer default 50 not null;
alter table if exists plantaopro.clientes add column if not exists saude_status character varying(20) default 'ATENCAO'::character varying not null;
alter table if exists plantaopro.clientes add column if not exists ultimo_recalculo_saude timestamp with time zone;
alter table if exists plantaopro.clinica_caixa add column if not exists data_abertura timestamp without time zone default now() not null;
alter table if exists plantaopro.clinica_caixa add column if not exists data_fechamento timestamp without time zone;
alter table if exists plantaopro.clinica_caixa add column if not exists observacoes text default ''::text not null;
alter table if exists plantaopro.clinica_caixa add column if not exists saldo_final numeric(14,2) default 0 not null;
alter table if exists plantaopro.clinica_caixa add column if not exists total_entradas numeric(14,2) default 0 not null;
alter table if exists plantaopro.clinica_caixa add column if not exists total_saidas numeric(14,2) default 0 not null;
alter table if exists plantaopro.clinica_caixa add column if not exists usuario_abertura_id uuid;
alter table if exists plantaopro.clinica_caixa add column if not exists usuario_fechamento_id uuid;
alter table if exists plantaopro.clinica_contas_receber add column if not exists observacoes text default ''::text not null;
alter table if exists plantaopro.clinica_contas_receber add column if not exists tipo_recebimento text default 'PARTICULAR'::text not null;
alter table if exists plantaopro.clinica_recebimentos add column if not exists comprovante text default ''::text not null;
alter table if exists plantaopro.clinica_recebimentos add column if not exists observacoes text default ''::text not null;
alter table if exists plantaopro.clinica_recebimentos add column if not exists paciente_id uuid;
alter table if exists plantaopro.consulta_encaminhamentos add column if not exists created_at timestamp with time zone default now() not null;
alter table if exists plantaopro.consulta_encaminhamentos add column if not exists descricao text default ''::text not null;
alter table if exists plantaopro.consulta_encaminhamentos add column if not exists destino text;
alter table if exists plantaopro.consulta_encaminhamentos add column if not exists especialidade_destino_id uuid;
alter table if exists plantaopro.consulta_encaminhamentos add column if not exists observacoes text default ''::text not null;
alter table if exists plantaopro.consulta_encaminhamentos add column if not exists paciente_id uuid;
alter table if exists plantaopro.consulta_encaminhamentos add column if not exists prioridade text default ''::text not null;
alter table if exists plantaopro.consulta_encaminhamentos add column if not exists status text default 'ABERTO'::text not null;
alter table if exists plantaopro.consulta_encaminhamentos add column if not exists tenant_id uuid;
alter table if exists plantaopro.consulta_encaminhamentos add column if not exists updated_at timestamp with time zone;
alter table if exists plantaopro.consulta_encaminhamentos add column if not exists updated_by uuid;
alter table if exists plantaopro.consulta_historico add column if not exists acao text default ''::text not null;
alter table if exists plantaopro.consulta_historico add column if not exists detalhe text default ''::text not null;
alter table if exists plantaopro.consulta_historico add column if not exists status text default 'REGISTRADO'::text not null;
alter table if exists plantaopro.consulta_historico add column if not exists tenant_id uuid;
alter table if exists plantaopro.consulta_historico add column if not exists usuario_id uuid;
alter table if exists plantaopro.consultas add column if not exists agendamento_id uuid;
alter table if exists plantaopro.consultas add column if not exists cancelada_em timestamp with time zone;
alter table if exists plantaopro.consultas add column if not exists cid_id uuid;
alter table if exists plantaopro.consultas add column if not exists codigo_cid text default ''::text not null;
alter table if exists plantaopro.consultas add column if not exists created_by uuid;
alter table if exists plantaopro.consultas add column if not exists data_fim timestamp without time zone;
alter table if exists plantaopro.consultas add column if not exists data_inicio timestamp without time zone;
alter table if exists plantaopro.consultas add column if not exists especialidade_id uuid;
alter table if exists plantaopro.consultas add column if not exists finalizada_em timestamp with time zone;
alter table if exists plantaopro.consultas add column if not exists hospital_id uuid;
alter table if exists plantaopro.consultas add column if not exists medico_id uuid;
alter table if exists plantaopro.consultas add column if not exists motivo_atendimento text default ''::text not null;
alter table if exists plantaopro.consultas add column if not exists motivo_cancelamento text default ''::text not null;
alter table if exists plantaopro.consultas add column if not exists profissional_id uuid;
alter table if exists plantaopro.consultas add column if not exists reg_update timestamp without time zone;
alter table if exists plantaopro.consultas add column if not exists resumo text default ''::text not null;
alter table if exists plantaopro.consultas add column if not exists tipo text default 'CONSULTA'::text not null;
alter table if exists plantaopro.consultas add column if not exists updated_by uuid;
alter table if exists plantaopro.convenios add column if not exists cliente_id uuid;
alter table if exists plantaopro.convenios add column if not exists cnpj text default ''::text not null;
alter table if exists plantaopro.convenios add column if not exists codigo_ans text default ''::text not null;
alter table if exists plantaopro.convenios add column if not exists created_by uuid;
alter table if exists plantaopro.convenios add column if not exists reg_date timestamp with time zone default now() not null;
alter table if exists plantaopro.convenios add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.convenios add column if not exists updated_at timestamp with time zone;
alter table if exists plantaopro.convenios add column if not exists updated_by uuid;
alter table if exists plantaopro.conversas add column if not exists cliente_id uuid;
alter table if exists plantaopro.conversas add column if not exists created_by uuid;
alter table if exists plantaopro.conversas add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.conversas add column if not exists reg_update timestamp without time zone;
alter table if exists plantaopro.conversas add column if not exists updated_by uuid;
alter table if exists plantaopro.downgrade_solicitacoes add column if not exists cliente_id uuid;
alter table if exists plantaopro.downgrade_solicitacoes add column if not exists metadata jsonb default '{}'::jsonb not null;
alter table if exists plantaopro.downgrade_solicitacoes add column if not exists parceiro_id uuid;
alter table if exists plantaopro.downgrade_solicitacoes add column if not exists plano_id uuid;
alter table if exists plantaopro.downgrade_solicitacoes add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.downgrade_solicitacoes add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.downgrade_solicitacoes add column if not exists reg_update timestamp with time zone;
alter table if exists plantaopro.escalas add column if not exists cliente_id uuid;
alter table if exists plantaopro.escalas add column if not exists created_by uuid;
alter table if exists plantaopro.escalas add column if not exists data_fim timestamp with time zone;
alter table if exists plantaopro.escalas add column if not exists data_inicio timestamp with time zone;
alter table if exists plantaopro.escalas add column if not exists justificativa text;
alter table if exists plantaopro.escalas add column if not exists reg_date timestamp with time zone default now() not null;
alter table if exists plantaopro.especialidades add column if not exists cliente_id uuid;
alter table if exists plantaopro.especialidades add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.eventos_sistema add column if not exists cliente_id uuid;
alter table if exists plantaopro.eventos_sistema add column if not exists reg_date timestamp with time zone default now() not null;
alter table if exists plantaopro.eventos_sistema add column if not exists tipo character varying(120);
alter table if exists plantaopro.faturas_saas add column if not exists cliente_id uuid;
alter table if exists plantaopro.faturas_saas add column if not exists competencia date;
alter table if exists plantaopro.faturas_saas add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.faturas_saas add column if not exists valor_total numeric(12,2) default 0 not null;
alter table if exists plantaopro.faturas_saas add column if not exists vencimento date;
alter table if exists plantaopro.hospitais add column if not exists cidade character varying(80);
alter table if exists plantaopro.hospitais add column if not exists cliente_id uuid;
alter table if exists plantaopro.hospitais add column if not exists cnpj character varying(18);
alter table if exists plantaopro.hospitais add column if not exists estado character(2);
alter table if exists plantaopro.hospitais add column if not exists nome_fantasia character varying(200);
alter table if exists plantaopro.hospitais add column if not exists razao_social character varying(200);
alter table if exists plantaopro.hospitais add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.logs_operacionais add column if not exists acao character varying(120);
alter table if exists plantaopro.logs_operacionais add column if not exists cliente_id uuid;
alter table if exists plantaopro.logs_operacionais add column if not exists reg_date timestamp with time zone default now() not null;
alter table if exists plantaopro.medico_disponibilidades add column if not exists cliente_id uuid;
alter table if exists plantaopro.medico_disponibilidades add column if not exists created_by uuid;
alter table if exists plantaopro.medico_disponibilidades add column if not exists data_fim timestamp without time zone;
alter table if exists plantaopro.medico_disponibilidades add column if not exists data_inicio timestamp without time zone;
alter table if exists plantaopro.medico_disponibilidades add column if not exists dia_semana integer;
alter table if exists plantaopro.medico_disponibilidades add column if not exists especialidade_id uuid;
alter table if exists plantaopro.medico_disponibilidades add column if not exists hospital_id uuid;
alter table if exists plantaopro.medico_disponibilidades add column if not exists medico_id uuid;
alter table if exists plantaopro.medico_disponibilidades add column if not exists metadata jsonb default '{}'::jsonb not null;
alter table if exists plantaopro.medico_disponibilidades add column if not exists observacoes text;
alter table if exists plantaopro.medico_disponibilidades add column if not exists parceiro_id uuid;
alter table if exists plantaopro.medico_disponibilidades add column if not exists plano_id uuid;
alter table if exists plantaopro.medico_disponibilidades add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.medico_disponibilidades add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.medico_disponibilidades add column if not exists reg_update timestamp without time zone;
alter table if exists plantaopro.medico_disponibilidades add column if not exists turno character varying(30);
alter table if exists plantaopro.medico_disponibilidades add column if not exists updated_by uuid;
alter table if exists plantaopro.medico_indisponibilidades add column if not exists cliente_id uuid;
alter table if exists plantaopro.medico_indisponibilidades add column if not exists created_by uuid;
alter table if exists plantaopro.medico_indisponibilidades add column if not exists data_fim timestamp without time zone;
alter table if exists plantaopro.medico_indisponibilidades add column if not exists data_inicio timestamp without time zone;
alter table if exists plantaopro.medico_indisponibilidades add column if not exists medico_id uuid;
alter table if exists plantaopro.medico_indisponibilidades add column if not exists motivo text;
alter table if exists plantaopro.medico_indisponibilidades add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.medico_indisponibilidades add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.medico_indisponibilidades add column if not exists reg_update timestamp without time zone;
alter table if exists plantaopro.medico_indisponibilidades add column if not exists updated_by uuid;
alter table if exists plantaopro.medico_preferencias_plantao add column if not exists cliente_id uuid;
alter table if exists plantaopro.medico_preferencias_plantao add column if not exists created_by uuid;
alter table if exists plantaopro.medico_preferencias_plantao add column if not exists especialidades_preferidas uuid[];
alter table if exists plantaopro.medico_preferencias_plantao add column if not exists hospitais_preferidos uuid[];
alter table if exists plantaopro.medico_preferencias_plantao add column if not exists limite_plantoes_mes integer default 20 not null;
alter table if exists plantaopro.medico_preferencias_plantao add column if not exists limite_plantoes_semana integer default 5 not null;
alter table if exists plantaopro.medico_preferencias_plantao add column if not exists medico_id uuid;
alter table if exists plantaopro.medico_preferencias_plantao add column if not exists observacoes text;
alter table if exists plantaopro.medico_preferencias_plantao add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.medico_preferencias_plantao add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.medico_preferencias_plantao add column if not exists reg_update timestamp without time zone;
alter table if exists plantaopro.medico_preferencias_plantao add column if not exists turnos_preferidos text[];
alter table if exists plantaopro.medico_preferencias_plantao add column if not exists updated_by uuid;
alter table if exists plantaopro.medicos add column if not exists cpf text;
alter table if exists plantaopro.medicos add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.medicos add column if not exists usuario_id uuid;
alter table if exists plantaopro.mensagens add column if not exists conversa_id uuid;
alter table if exists plantaopro.mensagens add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.mensagens add column if not exists remetente_usuario_id uuid;
alter table if exists plantaopro.modulos_sistema add column if not exists cliente_id uuid;
alter table if exists plantaopro.modulos_sistema add column if not exists is_demo boolean default false not null;
alter table if exists plantaopro.modulos_sistema add column if not exists metadata jsonb default '{}'::jsonb not null;
alter table if exists plantaopro.modulos_sistema add column if not exists parceiro_id uuid;
alter table if exists plantaopro.modulos_sistema add column if not exists plano_id uuid;
alter table if exists plantaopro.modulos_sistema add column if not exists tenant_id uuid;
alter table if exists plantaopro.pacientes add column if not exists cns text;
alter table if exists plantaopro.pacientes add column if not exists consentimento_lgpd boolean default false not null;
alter table if exists plantaopro.pacientes add column if not exists consentimento_lgpd_canal text default ''::text not null;
alter table if exists plantaopro.pacientes add column if not exists consentimento_lgpd_em timestamp without time zone;
alter table if exists plantaopro.pacientes add column if not exists created_at timestamp with time zone default now() not null;
alter table if exists plantaopro.pacientes add column if not exists created_by uuid;
alter table if exists plantaopro.pacientes add column if not exists data_nascimento date;
alter table if exists plantaopro.pacientes add column if not exists documento text default ''::text not null;
alter table if exists plantaopro.pacientes add column if not exists documento_alternativo text;
alter table if exists plantaopro.pacientes add column if not exists email text default ''::text not null;
alter table if exists plantaopro.pacientes add column if not exists endereco text;
alter table if exists plantaopro.pacientes add column if not exists finalidade_tratamento text default 'ASSISTENCIA_SAUDE'::text not null;
alter table if exists plantaopro.pacientes add column if not exists nome_completo text default ''::text not null;
alter table if exists plantaopro.pacientes add column if not exists nome_social text;
alter table if exists plantaopro.pacientes add column if not exists observacoes text;
alter table if exists plantaopro.pacientes add column if not exists reg_update timestamp without time zone;
alter table if exists plantaopro.pacientes add column if not exists responsavel_nome text;
alter table if exists plantaopro.pacientes add column if not exists sexo text default ''::text not null;
alter table if exists plantaopro.pacientes add column if not exists sexo_genero text;
alter table if exists plantaopro.pacientes add column if not exists telefone text default ''::text not null;
alter table if exists plantaopro.pacientes add column if not exists updated_at timestamp with time zone;
alter table if exists plantaopro.pacientes add column if not exists updated_by uuid;
alter table if exists plantaopro.pacientes add column if not exists ver_dados_sensiveis boolean default false not null;
alter table if exists plantaopro.pagamentos_saas add column if not exists cliente_id uuid;
alter table if exists plantaopro.pagamentos_saas add column if not exists fatura_id uuid;
alter table if exists plantaopro.pagamentos_saas add column if not exists reg_date timestamp with time zone default now() not null;
alter table if exists plantaopro.pagamentos_saas add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.pagamentos_saas add column if not exists valor_pago numeric(12,2) default 0 not null;
alter table if exists plantaopro.painel_chamada_fila add column if not exists chamado_em timestamp without time zone;
alter table if exists plantaopro.painel_chamada_fila add column if not exists created_at timestamp with time zone default now() not null;
alter table if exists plantaopro.painel_chamada_fila add column if not exists finalizado_em timestamp without time zone;
alter table if exists plantaopro.painel_chamada_fila add column if not exists guiche text default ''::text not null;
alter table if exists plantaopro.painel_chamada_fila add column if not exists sala text default ''::text not null;
alter table if exists plantaopro.painel_chamada_fila add column if not exists setor text default ''::text not null;
alter table if exists plantaopro.painel_chamada_fila add column if not exists updated_at timestamp with time zone;
alter table if exists plantaopro.perfil_permissoes add column if not exists cliente_id uuid;
alter table if exists plantaopro.perfil_permissoes add column if not exists metadata jsonb default '{}'::jsonb not null;
alter table if exists plantaopro.perfil_permissoes add column if not exists parceiro_id uuid;
alter table if exists plantaopro.perfil_permissoes add column if not exists plano_id uuid;
alter table if exists plantaopro.perfil_permissoes add column if not exists status character varying(40) default 'ATIVO'::character varying not null;
alter table if exists plantaopro.perfil_permissoes add column if not exists tenant_id uuid;
alter table if exists plantaopro.perfis add column if not exists metadata jsonb default '{}'::jsonb not null;
alter table if exists plantaopro.perfis add column if not exists parceiro_id uuid;
alter table if exists plantaopro.perfis add column if not exists plano_id uuid;
alter table if exists plantaopro.permissoes add column if not exists cliente_id uuid;
alter table if exists plantaopro.permissoes add column if not exists metadata jsonb default '{}'::jsonb not null;
alter table if exists plantaopro.permissoes add column if not exists parceiro_id uuid;
alter table if exists plantaopro.permissoes add column if not exists plano_id uuid;
alter table if exists plantaopro.permissoes add column if not exists tenant_id uuid;
alter table if exists plantaopro.planos add column if not exists cliente_id uuid;
alter table if exists plantaopro.planos add column if not exists descricao text;
alter table if exists plantaopro.planos add column if not exists destaque boolean;
alter table if exists plantaopro.planos add column if not exists dias_trial integer default 14 not null;
alter table if exists plantaopro.planos add column if not exists limite_convites_mes integer default 0 not null;
alter table if exists plantaopro.planos add column if not exists limite_hospitais integer default 0 not null;
alter table if exists plantaopro.planos add column if not exists limite_medicos integer default 0 not null;
alter table if exists plantaopro.planos add column if not exists limite_plantoes_mes integer default 0 not null;
alter table if exists plantaopro.planos add column if not exists limite_usuarios integer default 0 not null;
alter table if exists plantaopro.planos add column if not exists metadata jsonb default '{}'::jsonb not null;
alter table if exists plantaopro.planos add column if not exists ordem integer;
alter table if exists plantaopro.planos add column if not exists ordem_exibicao integer default 0 not null;
alter table if exists plantaopro.planos add column if not exists parceiro_id uuid;
alter table if exists plantaopro.planos add column if not exists permite_api boolean default false not null;
alter table if exists plantaopro.planos add column if not exists permite_bi boolean default false not null;
alter table if exists plantaopro.planos add column if not exists permite_integracoes boolean default false not null;
alter table if exists plantaopro.planos add column if not exists permite_mobile boolean default false not null;
alter table if exists plantaopro.planos add column if not exists permite_operacao_assistida boolean default false not null;
alter table if exists plantaopro.planos add column if not exists permite_perfis_customizados text;
alter table if exists plantaopro.planos add column if not exists permite_relatorios_avancados boolean default false not null;
alter table if exists plantaopro.planos add column if not exists permite_suporte_prioritario boolean default false not null;
alter table if exists plantaopro.planos add column if not exists permite_trial boolean default true not null;
alter table if exists plantaopro.planos add column if not exists permite_white_label boolean;
alter table if exists plantaopro.planos add column if not exists plano_id uuid;
alter table if exists plantaopro.planos add column if not exists possui_bi boolean default false not null;
alter table if exists plantaopro.planos add column if not exists possui_mobile boolean default false not null;
alter table if exists plantaopro.planos add column if not exists possui_relatorios_avancados boolean default false not null;
alter table if exists plantaopro.planos add column if not exists publico boolean;
alter table if exists plantaopro.planos add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.planos add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.planos add column if not exists reg_update timestamp without time zone;
alter table if exists plantaopro.planos add column if not exists slug text;
alter table if exists plantaopro.planos add column if not exists valor_anual numeric(14,2) default 0 not null;
alter table if exists plantaopro.planos add column if not exists valor_mensal numeric(12,2) default 0 not null;
alter table if exists plantaopro.planos_saude add column if not exists cliente_id uuid;
alter table if exists plantaopro.planos_saude add column if not exists convenio_id uuid;
alter table if exists plantaopro.planos_saude add column if not exists created_by uuid;
alter table if exists plantaopro.planos_saude add column if not exists operadora text default ''::text not null;
alter table if exists plantaopro.planos_saude add column if not exists reg_date timestamp with time zone default now() not null;
alter table if exists plantaopro.planos_saude add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.planos_saude add column if not exists registro_ans text default ''::text not null;
alter table if exists plantaopro.planos_saude add column if not exists updated_at timestamp with time zone;
alter table if exists plantaopro.planos_saude add column if not exists updated_by uuid;
alter table if exists plantaopro.plantao_convites add column if not exists data_envio timestamp with time zone;
alter table if exists plantaopro.plantao_convites add column if not exists mensagem text;
alter table if exists plantaopro.plantao_convites add column if not exists usuario_id uuid;
alter table if exists plantaopro.plantoes add column if not exists cliente_id uuid;
alter table if exists plantaopro.plantoes add column if not exists created_by uuid;
alter table if exists plantaopro.plantoes add column if not exists especialidade_id uuid;
alter table if exists plantaopro.plantoes add column if not exists hospital_id uuid;
alter table if exists plantaopro.plantoes add column if not exists observacoes text;
alter table if exists plantaopro.plantoes add column if not exists reg_date timestamp with time zone default now() not null;
alter table if exists plantaopro.plantoes add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.plantoes add column if not exists setor text;
alter table if exists plantaopro.plantoes add column if not exists setor_id uuid;
alter table if exists plantaopro.plantoes add column if not exists tipo text default 'PRESENCIAL'::text not null;
alter table if exists plantaopro.prescricoes add column if not exists cancelada_em timestamp with time zone;
alter table if exists plantaopro.prescricoes add column if not exists cliente_id uuid;
alter table if exists plantaopro.prescricoes add column if not exists consulta_id uuid;
alter table if exists plantaopro.prescricoes add column if not exists created_at timestamp with time zone default now() not null;
alter table if exists plantaopro.prescricoes add column if not exists created_by uuid;
alter table if exists plantaopro.prescricoes add column if not exists finalizada_em timestamp with time zone;
alter table if exists plantaopro.prescricoes add column if not exists medico_id uuid;
alter table if exists plantaopro.prescricoes add column if not exists modelo_id uuid;
alter table if exists plantaopro.prescricoes add column if not exists orientacoes text default ''::text not null;
alter table if exists plantaopro.prescricoes add column if not exists paciente_id uuid;
alter table if exists plantaopro.prescricoes add column if not exists reg_date timestamp with time zone default now() not null;
alter table if exists plantaopro.prescricoes add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.prescricoes add column if not exists reg_update timestamp with time zone;
alter table if exists plantaopro.prescricoes add column if not exists updated_at timestamp with time zone;
alter table if exists plantaopro.prescricoes add column if not exists updated_by uuid;
alter table if exists plantaopro.relatorio_exportacoes add column if not exists cliente_id uuid;
alter table if exists plantaopro.relatorio_exportacoes add column if not exists metadata jsonb default '{}'::jsonb not null;
alter table if exists plantaopro.relatorio_exportacoes add column if not exists parceiro_id uuid;
alter table if exists plantaopro.relatorio_exportacoes add column if not exists plano_id uuid;
alter table if exists plantaopro.relatorio_exportacoes add column if not exists reg_date timestamp with time zone default now() not null;
alter table if exists plantaopro.relatorio_exportacoes add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.relatorio_exportacoes add column if not exists reg_update timestamp with time zone;
alter table if exists plantaopro.relatorios_exportacoes add column if not exists arquivo_nome character varying(220);
alter table if exists plantaopro.relatorios_exportacoes add column if not exists cliente_id uuid;
alter table if exists plantaopro.relatorios_exportacoes add column if not exists created_by uuid;
alter table if exists plantaopro.relatorios_exportacoes add column if not exists execucao_id uuid;
alter table if exists plantaopro.relatorios_exportacoes add column if not exists exportado_por uuid;
alter table if exists plantaopro.relatorios_exportacoes add column if not exists formato character varying(20) default 'CSV'::character varying not null;
alter table if exists plantaopro.relatorios_exportacoes add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.relatorios_exportacoes add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.relatorios_exportacoes add column if not exists reg_update timestamp without time zone;
alter table if exists plantaopro.relatorios_exportacoes add column if not exists tipo character varying(60);
alter table if exists plantaopro.relatorios_exportacoes add column if not exists total_linhas integer default 0 not null;
alter table if exists plantaopro.relatorios_exportacoes add column if not exists updated_by uuid;
alter table if exists plantaopro.relatorios_filtros_salvos add column if not exists cliente_id uuid;
alter table if exists plantaopro.relatorios_filtros_salvos add column if not exists created_by uuid;
alter table if exists plantaopro.relatorios_filtros_salvos add column if not exists filtros jsonb default '{}'::jsonb not null;
alter table if exists plantaopro.relatorios_filtros_salvos add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.relatorios_filtros_salvos add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.relatorios_filtros_salvos add column if not exists reg_update timestamp without time zone;
alter table if exists plantaopro.relatorios_filtros_salvos add column if not exists tipo character varying(60);
alter table if exists plantaopro.relatorios_filtros_salvos add column if not exists updated_by uuid;
alter table if exists plantaopro.relatorios_filtros_salvos add column if not exists usuario_id uuid;
alter table if exists plantaopro.schema_migrations add column if not exists duration_ms integer;
alter table if exists plantaopro.schema_migrations add column if not exists error_code text;
alter table if exists plantaopro.schema_migrations add column if not exists error_message_sanitized text;
alter table if exists plantaopro.schema_migrations add column if not exists executor_version text default 'PlantaoPro.Tools.Database v1.91.0'::text not null;
alter table if exists plantaopro.schema_migrations add column if not exists source text;
alter table if exists plantaopro.schema_migrations add column if not exists success boolean default true not null;
alter table if exists plantaopro.schema_migrations add column if not exists version text;
alter table if exists plantaopro.substituicao_aprovacoes add column if not exists aprovador_usuario_id uuid;
alter table if exists plantaopro.substituicao_aprovacoes add column if not exists cliente_id uuid;
alter table if exists plantaopro.substituicao_aprovacoes add column if not exists created_by uuid;
alter table if exists plantaopro.substituicao_aprovacoes add column if not exists decisao character varying(40);
alter table if exists plantaopro.substituicao_aprovacoes add column if not exists justificativa text;
alter table if exists plantaopro.substituicao_aprovacoes add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.substituicao_aprovacoes add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.substituicao_aprovacoes add column if not exists reg_update timestamp without time zone;
alter table if exists plantaopro.substituicao_aprovacoes add column if not exists substituicao_id uuid;
alter table if exists plantaopro.substituicao_aprovacoes add column if not exists updated_by uuid;
alter table if exists plantaopro.substituicao_candidatos add column if not exists cliente_id uuid;
alter table if exists plantaopro.substituicao_candidatos add column if not exists created_by uuid;
alter table if exists plantaopro.substituicao_candidatos add column if not exists motivos text;
alter table if exists plantaopro.substituicao_candidatos add column if not exists reg_update timestamp without time zone;
alter table if exists plantaopro.substituicao_candidatos add column if not exists score numeric(10,2) default 0 not null;
alter table if exists plantaopro.substituicao_candidatos add column if not exists updated_by uuid;
alter table if exists plantaopro.substituicao_historico add column if not exists acao character varying(80);
alter table if exists plantaopro.substituicao_historico add column if not exists cliente_id uuid;
alter table if exists plantaopro.substituicao_historico add column if not exists created_by uuid;
alter table if exists plantaopro.substituicao_historico add column if not exists observacao text;
alter table if exists plantaopro.substituicao_historico add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.substituicao_historico add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.substituicao_historico add column if not exists reg_update timestamp without time zone;
alter table if exists plantaopro.substituicao_historico add column if not exists status_anterior character varying(40);
alter table if exists plantaopro.substituicao_historico add column if not exists status_novo character varying(40);
alter table if exists plantaopro.substituicao_historico add column if not exists substituicao_id uuid;
alter table if exists plantaopro.substituicao_historico add column if not exists updated_by uuid;
alter table if exists plantaopro.substituicao_historico add column if not exists usuario_id uuid;
alter table if exists plantaopro.substituicoes_plantao add column if not exists created_by uuid;
alter table if exists plantaopro.substituicoes_plantao add column if not exists medico_solicitante_id uuid;
alter table if exists plantaopro.substituicoes_plantao add column if not exists medico_substituto_id uuid;
alter table if exists plantaopro.substituicoes_plantao add column if not exists motivo text;
alter table if exists plantaopro.substituicoes_plantao add column if not exists prazo_minimo_ok boolean default true not null;
alter table if exists plantaopro.substituicoes_plantao add column if not exists reg_update timestamp without time zone;
alter table if exists plantaopro.substituicoes_plantao add column if not exists responsavel_usuario_id uuid;
alter table if exists plantaopro.substituicoes_plantao add column if not exists updated_by uuid;
alter table if exists plantaopro.tenant_configuracoes add column if not exists categoria text default 'GERAL'::text not null;
alter table if exists plantaopro.tenant_configuracoes add column if not exists chave text;
alter table if exists plantaopro.tenant_configuracoes add column if not exists cliente_id uuid;
alter table if exists plantaopro.tenant_configuracoes add column if not exists metadata jsonb default '{}'::jsonb not null;
alter table if exists plantaopro.tenant_configuracoes add column if not exists parceiro_id uuid;
alter table if exists plantaopro.tenant_configuracoes add column if not exists plano_id uuid;
alter table if exists plantaopro.tenant_configuracoes add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.tenant_configuracoes add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.tenant_configuracoes add column if not exists reg_update timestamp with time zone;
alter table if exists plantaopro.tenant_modulos add column if not exists cliente_id uuid;
alter table if exists plantaopro.tenant_modulos add column if not exists metadata jsonb default '{}'::jsonb not null;
alter table if exists plantaopro.tenant_modulos add column if not exists parceiro_id uuid;
alter table if exists plantaopro.tenant_modulos add column if not exists plano_id uuid;
alter table if exists plantaopro.tenant_onboarding add column if not exists cliente_id uuid;
alter table if exists plantaopro.tenant_onboarding add column if not exists metadata jsonb default '{}'::jsonb not null;
alter table if exists plantaopro.tenant_onboarding add column if not exists parceiro_id uuid;
alter table if exists plantaopro.tenant_onboarding add column if not exists plano_id uuid;
alter table if exists plantaopro.tenant_onboarding add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.tenant_onboarding add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.tenant_onboarding add column if not exists reg_update timestamp with time zone;
alter table if exists plantaopro.tenant_onboarding_checklist add column if not exists cliente_id uuid;
alter table if exists plantaopro.tenant_onboarding_checklist add column if not exists metadata jsonb default '{}'::jsonb not null;
alter table if exists plantaopro.tenant_onboarding_checklist add column if not exists parceiro_id uuid;
alter table if exists plantaopro.tenant_onboarding_checklist add column if not exists plano_id uuid;
alter table if exists plantaopro.tenant_onboarding_checklist add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.tenant_onboarding_checklist add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.tenant_onboarding_checklist add column if not exists reg_update timestamp with time zone;
alter table if exists plantaopro.tenant_parametros add column if not exists categoria text;
alter table if exists plantaopro.tenant_parametros add column if not exists chave text;
alter table if exists plantaopro.tenant_parametros add column if not exists cliente_id uuid;
alter table if exists plantaopro.tenant_parametros add column if not exists metadata jsonb default '{}'::jsonb not null;
alter table if exists plantaopro.tenant_parametros add column if not exists parceiro_id uuid;
alter table if exists plantaopro.tenant_parametros add column if not exists plano_id uuid;
alter table if exists plantaopro.tenant_parametros add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.tenant_parametros add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.tenant_parametros add column if not exists reg_update timestamp with time zone;
alter table if exists plantaopro.tenant_white_label add column if not exists cliente_id uuid;
alter table if exists plantaopro.tenant_white_label add column if not exists metadata jsonb default '{}'::jsonb not null;
alter table if exists plantaopro.tenant_white_label add column if not exists parceiro_id uuid;
alter table if exists plantaopro.tenant_white_label add column if not exists plano_id uuid;
alter table if exists plantaopro.tenant_white_label add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.tenant_white_label add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.tenant_white_label add column if not exists reg_update timestamp with time zone;
alter table if exists plantaopro.tenants add column if not exists cliente_id uuid;
alter table if exists plantaopro.tenants add column if not exists dominio_customizado text;
alter table if exists plantaopro.tenants add column if not exists metadata jsonb default '{}'::jsonb not null;
alter table if exists plantaopro.tenants add column if not exists motivo_suspensao text;
alter table if exists plantaopro.tenants add column if not exists parceiro_id uuid;
alter table if exists plantaopro.tenants add column if not exists plano_id uuid;
alter table if exists plantaopro.tenants add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.tenants add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.tenants add column if not exists reg_update timestamp with time zone;
alter table if exists plantaopro.tenants add column if not exists slug text;
alter table if exists plantaopro.tenants add column if not exists subdominio text;
alter table if exists plantaopro.triagem_encaminhamentos add column if not exists created_at timestamp with time zone default now() not null;
alter table if exists plantaopro.triagem_encaminhamentos add column if not exists updated_at timestamp with time zone;
alter table if exists plantaopro.triagem_fila add column if not exists created_at timestamp with time zone default now() not null;
alter table if exists plantaopro.triagem_fila add column if not exists updated_at timestamp with time zone;
alter table if exists plantaopro.triagens add column if not exists created_at timestamp with time zone default now() not null;
alter table if exists plantaopro.triagens add column if not exists enfermeiro_id uuid;
alter table if exists plantaopro.triagens add column if not exists iniciada_em timestamp without time zone;
alter table if exists plantaopro.triagens add column if not exists medico_id uuid;
alter table if exists plantaopro.triagens add column if not exists motivo_cancelamento text default ''::text not null;
alter table if exists plantaopro.triagens add column if not exists profissional_id uuid;
alter table if exists plantaopro.triagens add column if not exists reg_update timestamp without time zone;
alter table if exists plantaopro.triagens add column if not exists updated_at timestamp with time zone;
alter table if exists plantaopro.unidades add column if not exists cliente_id uuid;
alter table if exists plantaopro.unidades add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.upgrade_solicitacoes add column if not exists cliente_id uuid;
alter table if exists plantaopro.upgrade_solicitacoes add column if not exists metadata jsonb default '{}'::jsonb not null;
alter table if exists plantaopro.upgrade_solicitacoes add column if not exists parceiro_id uuid;
alter table if exists plantaopro.upgrade_solicitacoes add column if not exists plano_id uuid;
alter table if exists plantaopro.upgrade_solicitacoes add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.upgrade_solicitacoes add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.upgrade_solicitacoes add column if not exists reg_update timestamp with time zone;
alter table if exists plantaopro.usuario_permissoes_especiais add column if not exists metadata jsonb default '{}'::jsonb not null;
alter table if exists plantaopro.usuario_permissoes_especiais add column if not exists parceiro_id uuid;
alter table if exists plantaopro.usuario_permissoes_especiais add column if not exists plano_id uuid;
alter table if exists plantaopro.usuario_permissoes_especiais add column if not exists status character varying(40) default 'ATIVO'::character varying not null;
alter table if exists plantaopro.usuarios_perfis add column if not exists metadata jsonb default '{}'::jsonb not null;
alter table if exists plantaopro.usuarios_perfis add column if not exists parceiro_id uuid;
alter table if exists plantaopro.usuarios_perfis add column if not exists plano_id uuid;
alter table if exists plantaopro.usuarios_perfis add column if not exists status character varying(40) default 'ATIVO'::character varying not null;
alter table if exists plantaopro.white_label_temas add column if not exists cliente_id uuid;
alter table if exists plantaopro.white_label_temas add column if not exists metadata jsonb default '{}'::jsonb not null;
alter table if exists plantaopro.white_label_temas add column if not exists parceiro_id uuid;
alter table if exists plantaopro.white_label_temas add column if not exists plano_id uuid;
alter table if exists plantaopro.white_label_temas add column if not exists reg_date timestamp without time zone default now() not null;
alter table if exists plantaopro.white_label_temas add column if not exists reg_status character(1) default 'A'::bpchar not null;
alter table if exists plantaopro.white_label_temas add column if not exists reg_update timestamp with time zone;
alter table if exists plantaopro.white_label_temas add column if not exists status text default 'ATIVO'::text not null;

-- ============================================================
-- Seção 83 — Saas Assinaturas Colunas Comerciais B6 v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2312_saas_assinaturas_colunas_comerciais_b6.sql
-- SOURCE-SHA256: 67149e1d4e2c6c3585da34c4cdef92a246c83354a6e258456b2d7db2f903ad11
-- R4-B6 (rodada 4, item B6 governanca SaaS): colunas comerciais ausentes em plantaopro.assinaturas.
-- Os endpoints /api/assinaturas (Criar/AlterarStatus) e o SELECT de ObterAssinaturaAtual
-- escrevem/lemem data_inicio, observacoes, motivo_cancelamento e data_cancelamento;
-- nenhuma migracao anterior as criou, o que causava erro 42703 em tempo de execucao
-- sem cobertura de teste viva (defeito preexistente, fechado em B6).
-- Adicao aditiva e idempotente: todas as colunas aceitam null e nao afetam linhas existentes.

alter table plantaopro.assinaturas add column if not exists data_inicio date null;
alter table plantaopro.assinaturas add column if not exists observacoes text null;
alter table plantaopro.assinaturas add column if not exists motivo_cancelamento text null;
alter table plantaopro.assinaturas add column if not exists data_cancelamento timestamptz null;

-- ============================================================
-- Seção 84 — Saas Planos Permite Relatorios B6 v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2313_saas_planos_permite_relatorios_b6.sql
-- SOURCE-SHA256: fd2b80cd31711c5d727da1b76cb805de59c5deaa7baf9947f041608ff38478ea
-- R4-B6 (rodada 4, item B6 governanca SaaS): coluna permite_relatorios ausente em plantaopro.planos.
-- Os endpoints comerciais (SaasCommercialController: listar/criar/editar plano) e o
-- SELECT de ObterUsoPlano (TenantServices) leem/escrevem permite_relatorios; nenhuma
-- migracao da linhagem de upgrade a criou, o que causava erro 42703 em tempo de
-- execucao em plantaopro_test e no banco local plantaopro (verificado em 2026-10-06).
-- A linhagem nova (2026_plantao_pro_saas_jornada_lgpd_inteligencia.sql) ja nasce com
-- a coluna; bancos criados pela linhagem antiga precisam desta adicao.
-- Adicao aditiva e idempotente; default false mantem o feature flag fechado em bancos
-- existentes, no mesmo padrao das colunas vizinhas (permite_api, permite_integracoes).

alter table plantaopro.planos add column if not exists permite_relatorios boolean not null default false;

-- ============================================================
-- Seção 85 — Administrativo360 Cotacao Cancelamento Estorno Triagem B7 v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2314_adm360_b7_cotacao_cancelamento_estorno_triagem.sql
-- SOURCE-SHA256: 4b56d6d862e403482d9473d75f1502abe730d613ac00f4635ec3158252210409
-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2314_adm360_b7_cotacao_cancelamento_estorno_triagem
-- Administrativo 360 - B7: cancelamento de cotacao, estorno de resposta e
-- triagem responsavel por documento em quarentena
--
-- Alteracoes oficiais:
--   1. adm360_cotacoes: colunas de cancelamento auditavel (cancelado_em e
--      motivo_cancelamento). O estado CANCELADA ja existia na maquina de
--      estados do modulo, mas nao havia registro do momento nem do motivo.
--   2. adm360_cotacao_respostas: indice unico PARCIAL garante no maximo uma
--      resposta NAO RESOLVIDA por cotacao (estados operacionais NA_FILA,
--      ENVIANDO, RESULTADO_DESCONHECIDO, CONFIGURACAO_PENDENTE,
--      REJEITADA_PELO_PORTAL e EXPORTADA_MANUALMENTE). Estados resolvidos
--      (ACEITA_PELO_PORTAL) ficam fora do indice: preserva o historico e
--      permite estorno + re-aprovacao sem duplicidade (a aplicacao reutiliza
--      a linha existente em vez de inserir nova).
--      Backstop de banco complementa a guarda serializada da aplicacao
--      (lock de cotacao -> lock de resposta na mesma ordem em todos os fluxos).
--   3. adm360_documentos_recebidos: responsabilidade e prazo da triagem de
--      quarentena (triagem_responsavel_id, triagem_aberta_em, triagem_prazo,
--      triagem_observacao e triagem_resolvida_em) + indice parcial para a
--      fila de triagens abertas. Resolver a triagem apenas sai do quarentena:
--      a conferencia (status_conferencia) continua uma decisao separada.
--   4. adm360_eventos: ampliacao idempotente do CHECK de tipo_evento com
--      ESTORNO, CANCELAMENTO e TRIAGEM (mesmo padrao nomeado do v2310).
--
-- Idempotencia: ADD COLUMN IF NOT EXISTS / CREATE INDEX IF NOT EXISTS; a
-- ampliacao do CHECK remove qualquer versao anterior sem os novos valores e
-- recria a constraint nomeada apenas quando nao existe versao atualizada.
-- ============================================================================

ALTER TABLE plantaopro.adm360_cotacoes
    ADD COLUMN IF NOT EXISTS cancelado_em timestamptz;

ALTER TABLE plantaopro.adm360_cotacoes
    ADD COLUMN IF NOT EXISTS motivo_cancelamento text;

COMMENT ON COLUMN plantaopro.adm360_cotacoes.cancelado_em IS 'B7: momento do cancelamento interno (NULL quando nao cancelada).';
COMMENT ON COLUMN plantaopro.adm360_cotacoes.motivo_cancelamento IS 'B7: motivo registrado pelo operador no cancelamento.';

CREATE UNIQUE INDEX IF NOT EXISTS ux_adm360_resposta_cotacao_nao_resolvida
    ON plantaopro.adm360_cotacao_respostas (tenant_id, cotacao_id)
    WHERE status_transmissao IN ('NA_FILA', 'ENVIANDO', 'RESULTADO_DESCONHECIDO',
                                 'CONFIGURACAO_PENDENTE', 'REJEITADA_PELO_PORTAL',
                                 'EXPORTADA_MANUALMENTE');

CREATE INDEX IF NOT EXISTS ix_adm360_respostas_cotacao_ordem
    ON plantaopro.adm360_cotacao_respostas (tenant_id, cotacao_id, created_at);

ALTER TABLE plantaopro.adm360_documentos_recebidos
    ADD COLUMN IF NOT EXISTS triagem_responsavel_id uuid REFERENCES plantaopro.usuarios(id);

ALTER TABLE plantaopro.adm360_documentos_recebidos
    ADD COLUMN IF NOT EXISTS triagem_aberta_em timestamptz;

ALTER TABLE plantaopro.adm360_documentos_recebidos
    ADD COLUMN IF NOT EXISTS triagem_prazo timestamptz;

ALTER TABLE plantaopro.adm360_documentos_recebidos
    ADD COLUMN IF NOT EXISTS triagem_observacao text;

ALTER TABLE plantaopro.adm360_documentos_recebidos
    ADD COLUMN IF NOT EXISTS triagem_resolvida_em timestamptz;

COMMENT ON COLUMN plantaopro.adm360_documentos_recebidos.triagem_responsavel_id IS 'B7: usuario do tenant responsavel pela analise da quarentena.';
COMMENT ON COLUMN plantaopro.adm360_documentos_recebidos.triagem_aberta_em IS 'B7: momento em que a triagem foi aberta (reabrindo reinicia).';
COMMENT ON COLUMN plantaopro.adm360_documentos_recebidos.triagem_prazo IS 'B7: prazo UTC definido no ato da atribuicao.';
COMMENT ON COLUMN plantaopro.adm360_documentos_recebidos.triagem_observacao IS 'B7: observacao registrada ao abrir a triagem.';
COMMENT ON COLUMN plantaopro.adm360_documentos_recebidos.triagem_resolvida_em IS 'B7: momento da resolucao (saida da quarentena); NULL enquanto aberta.';

CREATE INDEX IF NOT EXISTS ix_adm360_docrec_triagem
    ON plantaopro.adm360_documentos_recebidos (tenant_id, quarentena, triagem_prazo)
    WHERE quarentena;

-- 4. Ampliacao do CHECK de tipo_evento (padrao nomeado do v2310, agora com
-- ESTORNO, CANCELAMENTO e TRIAGEM). Remove a constraint nomeada anterior e
-- quaisquer checks legados contendo APROVACAO sem TRIAGEM, e so recria quando
-- nenhuma versao atualizada existe.
DO $migration$
DECLARE
  r record;
  tem_novo boolean;
BEGIN
  -- Remove primeiro a constraint nomeada anterior (podem existir instalacoes
  -- ja criadas pelo v2310 com apenas os cinco tipos originais ampliados).
  ALTER TABLE plantaopro.adm360_eventos DROP CONSTRAINT IF EXISTS ck_adm360_eventos_tipo_evento;

  -- Remove qualquer CHECK legado restante da coluna (definicao varia entre
  -- instalacoes por ter sido criada sem nome explicito no v2301).
  FOR r IN
    SELECT con.conname
    FROM pg_constraint con
    JOIN pg_class rel ON rel.oid = con.conrelid
    JOIN pg_attribute at ON at.attrelid = con.conrelid AND at.attname = 'tipo_evento'
    WHERE rel.relnamespace = 'plantaopro'::regnamespace
      AND rel.relname = 'adm360_eventos'
      AND con.contype = 'c'
      AND con.conkey @> ARRAY[at.attnum]
      AND pg_get_constraintdef(con.oid) LIKE '%''APROVACAO''%'
      AND pg_get_constraintdef(con.oid) NOT LIKE '%''TRIAGEM'''
  LOOP
    EXECUTE format('ALTER TABLE plantaopro.adm360_eventos DROP CONSTRAINT %I', r.conname);
  END LOOP;

  -- So recria se nenhuma versao ja contiver o novo conjunto completo.
  SELECT count(*) > 0 INTO tem_novo
  FROM pg_constraint con
  JOIN pg_class rel ON rel.oid = con.conrelid
  WHERE rel.relnamespace = 'plantaopro'::regnamespace
    AND rel.relname = 'adm360_eventos'
    AND con.contype = 'c'
    AND pg_get_constraintdef(con.oid) LIKE '%''TRIAGEM''%';

  IF NOT tem_novo THEN
    ALTER TABLE plantaopro.adm360_eventos
      ADD CONSTRAINT ck_adm360_eventos_tipo_evento
      CHECK (tipo_evento IN ('APROVACAO', 'ARQUIVO', 'DECLARACAO_MANUAL', 'RETORNO_EXTERNO',
                             'CONFIRMACAO_CONFERENCIA', 'ESTORNO', 'CANCELAMENTO', 'TRIAGEM'));
  END IF;
END $migration$;

-- ============================================================
-- Seção 86 — Saude360 Financeiro Canonico B8 v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2315_saude360_b8_financeiro_canonico.sql
-- SOURCE-SHA256: dd5a5e4d32b8ab1d9b23b9b4845dfbfd1e0772f2e6634db433f118c0f1f155d2
-- ============================================================================
-- v2315 | B8 Saúde 360 | Financeiro canônico: baixa, estorno e fechamento de caixa
-- ----------------------------------------------------------------------------
-- Decisão (R4-B8): para a jornada clínica (consulta -> conta a receber ->
-- recebimento -> caixa/lançamentos/fechamento -> estorno), clinica_* é a fonte
-- canônica de origem do financeiro. As camadas v115/v116 permanecem módulos de
-- consolidação com origem própria (faturamento em lote, relatórios, integração)
-- e NÃO recebem escritas cruzadas desta jornada. Não há nova tabela paralela:
-- esta migração apenas normaliza dados existentes e libera pagamentos parciais.
--
-- Alterações:
--   1) Normaliza contas geradas pela finalização de consulta (que gravava
--      somente valor_bruto/desconto/coparticipacao/valor_liquido): o par
--      operacional canônico passa a ser valor_total/valor_pendente.
--   2) Baixa parcial: remove a unicidade "um recebimento ativo por conta"
--      (ux_recebimento_conta_confirmado). A proteção contra pagamento em
--      excesso passa a ser transacional: lock da conta (FOR UPDATE) +
--      validação valor <= saldo pendente + predicado de valor esperado na
--      atualização da conta (conflicto 409 em corrida).
--   3) Índices de apoio à baixa/estorno/estornos/histórico/autorizações.
-- ============================================================================
set search_path to plantaopro, public;

update plantaopro.clinica_contas_receber
set    valor_total = coalesce(valor_liquido, 0)
where  reg_status = 'A'
  and  coalesce(valor_total, 0) = 0
  and  coalesce(valor_liquido, 0) > 0;

update plantaopro.clinica_contas_receber
set    valor_pendente = greatest(coalesce(valor_liquido, 0) - coalesce(valor_pago, 0), 0)
where  reg_status = 'A'
  and  coalesce(valor_pendente, 0) = 0
  and  coalesce(valor_liquido, 0) > coalesce(valor_pago, 0);

drop index if exists plantaopro.ux_recebimento_conta_confirmado;

-- Índices de apoio: nomes sem qualificação de schema (CREATE INDEX não admite;
-- o search_path acima já posiciona em plantaopro). Tabelas continuam qualificadas.
create index if not exists ix_clinica_recebimentos_cliente_conta_status
    on plantaopro.clinica_recebimentos (cliente_id, conta_receber_id, status);

create index if not exists ix_clinica_lancamentos_cliente_caixa
    on plantaopro.clinica_lancamentos (cliente_id, caixa_id);

create index if not exists ix_clinica_estornos_recebimento
    on plantaopro.clinica_estornos (recebimento_id);

create index if not exists ix_convenio_autorizacoes_cliente_consulta
    on plantaopro.convenio_autorizacoes (cliente_id, consulta_id);

-- ============================================================
-- Seção 87 — Saude360 Fila Atendimento Legado B8 v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2316_saude360_b8_fila_atendimento_legado.sql
-- SOURCE-SHA256: cc22f08306c85a294395a021f31f1e26b9eae600b501b9eb7c8fe569a01d51e8
-- ============================================================================
-- v2316 | B8 Saúde 360 | Tabela legada fila_atendimento (DDL ausente no repo)
-- ----------------------------------------------------------------------------
-- Root cause (R4-B8): FinalizarAsync (ConsultaApplicationService.cs, linha do
-- UPDATE em plantaopro.fila_atendimento) foi introduzido no commit 5406e514
-- (prontuário médico e faturamento v1.27.0) e faz parte da transação de
-- finalização de consulta, mas NENHUMA DDL do repositório cria a tabela.
-- Sem ela, a transação de finalização falha com 42P01 (relation does not
-- exist) em todo ambiente onde ela nunca existiu manualmente — incluindo os
-- bancos locais de homologação (plantaopro/plantaopro_test), o que impedia a
-- jornada consulta -> conta -> recebimento -> caixa exigida pelo B8.
--
-- A documentação de auditoria (mapa-saude360.md, rodada 3) lista
-- fila_atendimento entre as tabelas principais do módulo; esta migração
-- materializa esse contrato de forma idempotente (IF NOT EXISTS), preservando
-- o código existente sem módulos paralelas. O UPDATE da finalização só marca
-- status='FINALIZADO' onde já houver linhas; nenhum escritor insere nesta
-- versão (projeção legada de fila), então a criação é suficiente para a
-- jornada operar.
-- ============================================================================
set search_path to plantaopro, public;

create table if not exists plantaopro.fila_atendimento(
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid not null,
    atendimento_id uuid not null,
    status text not null default 'AGUARDANDO',
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);

-- Índice de apoio ao UPDATE da finalização (predicado cliente + atendimento):
-- nome sem qualificação de schema (CREATE INDEX não admite; o search_path
-- acima posiciona em plantaopro). Tabela continua qualificada.
create index if not exists ix_fila_atendimento_cliente_atendimento
    on plantaopro.fila_atendimento(cliente_id, atendimento_id);

-- ============================================================
-- Seção 88 — Saude360 Contestacao Situacao Anterior B9 v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2317_saude360_b9_contestacao_situacao_anterior.sql
-- SOURCE-SHA256: be46976f4bc70a02bc84b16cfeb4b457235cb36cade2d27403bdb16c21fd76aa
-- ============================================================================
-- v2317 | B9 Saude 360 | Contestacao financeira: situacao_anterior
-- ----------------------------------------------------------------------------
-- Decisao (R4-B9, opcao B): contestar um pagamento pendente ou aprovado
-- passa a marca-lo como 'contestado', preservando a situacao de origem.
-- Para que a resolucao (MANTER_VALOR / AJUSTAR_VALOR / CANCELAR_PAGAMENTO)
-- restaure o estado anterior de forma deterministica e explicavel, a situacao
-- do pagamento imediatamente antes da contestacao e gravada na propria
-- contestacao. Linhas historicas permanecem com situacao_anterior NULL; a
-- resolucao as trata explicitamente (409 orientando manutencao de dados) em
-- vez de inferir o estado anterior.
-- O indice unico parcial ux_pagamento_contestacao_aberta (tenant_id,
-- pagamento_id) WHERE status='ABERTA' ja existe no schema (regra de uma
-- unica contestacao aberta por pagamento); nenhum novo indice nesta migracao.
-- ============================================================================
set search_path to plantaopro, public;

alter table plantaopro.pagamento_contestacoes
    add column if not exists situacao_anterior varchar(32);

comment on column plantaopro.pagamento_contestacoes.situacao_anterior is
    'Situacao do pagamento imediatamente antes da contestacao (pendente/aprovado/pago). NULL apenas em linhas historicas criadas antes da v2317.';

-- ============================================================
-- Seção 89 — Saude360 Plantao Historico Contrato B9 v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2318_saude360_b9_plantao_historico_contrato.sql
-- SOURCE-SHA256: 8a0f882bf14e8c6684805c1eaaac1840633a780a214729500e631c780ddd1074
-- ============================================================================
-- v2318 | B9 Saude 360 | plantao_historico alinhado ao contrato de codigo
-- ----------------------------------------------------------------------------
-- Problema: PlantaoHistoricoService.RegistrarAsync e PlantaoService.
-- ListarHistoricoAsync leem/gravam as colunas plantao_id, status_anterior,
-- status_novo, justificativa, usuario_id, reg_date e reg_status, mas a tabela
-- criada no schema base (030_operacao_plantoes.sql) conserva o layout generico
-- (id/tenant_id/codigo/nome/status/dados/criado_em/atualizado_em) — nenhuma
-- migracao anterior adicionou as colunas usadas pelo codigo. Resultado: toda
-- mudanca de status de plantao (publicar/cancelar/realizar/encerrar) falhava
-- com "column ... does not exist" em ambos os bancos (plantaopro e
-- plantaopro_test), tornando o estado terminal 'encerrado' inalcançavel pela
-- API — requisito central do bloco B9.
-- Decisao: acrescentar as colunas faltantes (idempotente; as colunas
-- genericas permanecem por compatibilidade). Nenhum dado historico dependia
-- do layout antigo (a tabela esta vazia em ambos os bancos nesta data).
-- Nota: plantaopro.historico_escala apresenta a mesma defasacao e segue em
-- backlog (fora da jornada critica B9: nenhuma acao testada aqui grava nela).
-- ============================================================================
set search_path to plantaopro, public;

alter table plantaopro.plantao_historico add column if not exists plantao_id uuid;
alter table plantaopro.plantao_historico add column if not exists status_anterior varchar(32);
alter table plantaopro.plantao_historico add column if not exists status_novo varchar(32);
alter table plantaopro.plantao_historico add column if not exists justificativa text;
alter table plantaopro.plantao_historico add column if not exists usuario_id uuid;
alter table plantaopro.plantao_historico add column if not exists reg_date timestamptz;
alter table plantaopro.plantao_historico add column if not exists reg_status char(1) not null default 'A';

create index if not exists ix_plantao_historico_plantao_regdate
    on plantaopro.plantao_historico (plantao_id, reg_date);

comment on column plantaopro.plantao_historico.plantao_id is
    'Plantao cuja mudanca de status esta sendo registrada (PlantaoHistoricoService).';
comment on column plantaopro.plantao_historico.status_anterior is
    'Status do plantao imediatamente antes da transicao.';
comment on column plantaopro.plantao_historico.status_novo is
    'Status do plantao apos a transicao.';

-- ============================================================
-- Seção 90 — Saude360 Notificacoes Contrato B9 v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2319_saude360_b9_notificacoes_contrato.sql
-- SOURCE-SHA256: baca5925613bcd26ac0568892c7e159492d542f4cd2000c8abc537f76db9706a
-- ============================================================================
-- v2319 | B9 Saude 360 | notificacoes com contrato de instancia do codigo
-- ----------------------------------------------------------------------------
-- Problema: NotificacaoService.CriarNotificacaoAsync/ListarNotificacoesAsync/
-- MarcarLidaAsync e DashboardService (NotificacoesNaoLidas) leem/gravam em
-- plantaopro.notificacoes as colunas usuario_id, titulo, mensagem, tipo, lida,
-- created_by, updated_by, reg_date, reg_update e reg_status, mas a tabela
-- criada no schema base (030_operacao_plantoes.sql) conserva o layout generico
-- de catalogo (id/tenant_id/codigo/nome/status/dados/criado_em/atualizado_em).
-- Nenhuma migracao anterior adicionou essas colunas (v2069/v2070 repararam
-- apenas o agregado central novo plantaopro.notifications). Resultado: todo
-- fluxo que cria notificacao (escala aceita/confirmada/recusada/substituida/
-- realizada, pagamento gerado/confirmado/registrado/contestado/resolvido/
-- estornado/cancelado) falha com "column usuario_id does not exist" em bases
-- instaladas pelo object-catalog, e os contadores de dashboard tambem quebram.
-- Decisao: acrescentar as colunas faltantes de forma idempotente (mesmo
-- padrao de 345_v2069_reparar_base_notificacoes) e preservar as colunas
-- genericas por compatibilidade. Linhas de catalogo existentes (status
-- 'ATIVO', codigo/nome preenchidos) permanecem intactas: os caminhos de
-- notificacao filtram por usuario_id e reg_status='A' e usuario_id delas e
-- nulo, logo nao aparecem em listagens nem contagens.
-- ============================================================================
set search_path to plantaopro, public;

alter table plantaopro.notificacoes add column if not exists usuario_id uuid;
alter table plantaopro.notificacoes add column if not exists titulo varchar(160);
alter table plantaopro.notificacoes add column if not exists mensagem text;
alter table plantaopro.notificacoes add column if not exists tipo varchar(40);
alter table plantaopro.notificacoes add column if not exists lida boolean not null default false;
alter table plantaopro.notificacoes add column if not exists created_by uuid;
alter table plantaopro.notificacoes add column if not exists updated_by uuid;
alter table plantaopro.notificacoes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.notificacoes add column if not exists reg_update timestamptz;
alter table plantaopro.notificacoes add column if not exists reg_status char(1) not null default 'A' check (reg_status in ('A','I'));

create index if not exists idx_notificacoes_usuario_lida
    on plantaopro.notificacoes (usuario_id, lida);

comment on column plantaopro.notificacoes.usuario_id is
    'Destinatario da notificacao (NotificacaoService); linhas de catalogo permanecem com valor nulo.';
comment on column plantaopro.notificacoes.lida is
    'Estado de leitura da notificacao para o destinatario.';

-- ============================================================
-- Seção 91 — Saude360 Plantoes Auditoria B9 v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2320_saude360_b9_plantoes_auditoria.sql
-- SOURCE-SHA256: d9eec352ba21c23e6d76ded2718c70f52cf089ffa59405436d580692045503f7
-- ============================================================================
-- v2320 | B9 Saude 360 | plantoes com colunas de auditoria usadas pelo codigo
-- ----------------------------------------------------------------------------
-- Problema: PlantaoService.ChangeStatusAsync (L1223), a edicao de plantao
-- (L1135), a deteccao de conflito (L1385) e o aceite de convite operacional
-- (L1487) executam update em plantaopro.plantoes usando reg_update,
-- updated_by e conflito_detectado, mas a tabela criada no schema base
-- (030_operacao_plantoes.sql) termina as colunas em reg_status/reg_date/
-- created_by — nenhuma migracao anterior adicionou essas tres colunas.
-- Resultado: qualquer transicao de status de plantao (publicar/cancelar/
-- realizar/encerrar), edicao, deteccao de conflito ou aceite de convite
-- falha com "column reg_update does not exist" em bases instaladas pelo
-- object-catalog — mesma classe da defasacao corrigida em v2318 para
-- plantao_historico.
-- Decisao: acrescentar as colunas faltantes de forma idempotente e preservar
-- as colunas genericas por compatibilidade (padrao de 345_v2069_reparar_base_
-- notificacoes / v2318). Dados existentes permanecem intactos; linhas ja
-- existentes recebem updated_by null e reg_update null, o que e consistente
-- com o restante do esquema (colunas de auditoria nullable).
-- ============================================================================
set search_path to plantaopro, public;

alter table plantaopro.plantoes add column if not exists updated_by uuid;
alter table plantaopro.plantoes add column if not exists reg_update timestamptz;
alter table plantaopro.plantoes add column if not exists conflito_detectado boolean not null default false;

comment on column plantaopro.plantoes.reg_update is
    'Data/hora da ultima alteracao do plantao (PlantaoService).';
comment on column plantaopro.plantoes.updated_by is
    'Usuario que fez a ultima alteracao do plantao.';
comment on column plantaopro.plantoes.conflito_detectado is
    'Flag de conflito operacional detectada no aceite de escala (PlantaoService).';

-- ============================================================
-- Seção 92 — Administrativo360 Fiscal Parametros Pre Emitidas v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2321_adm360_fiscal_parametros_pre_emitidas.sql
-- SOURCE-SHA256: 724de9952b3aff65d429d94b09673a318657970e76d3d66602a535fec13c3411
-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2321_adm360_fiscal_parametros_pre_emitidas
-- Administrativo 360 - Fiscal (item 9 do plano d1 / requisito A29 do MVP):
-- parametros de emissao, pre-nota interna e seus itens.
--
-- Escopo oficial do bloco: decidir PRIMEIRO operacao/UF/provedor (decisao
-- comercial registrada como PENDENCIA quando ausente) e avancar o que e
-- independente dela: cadastro, seguranca (credenciais fora do banco, A33) e
-- modelagem de estados. Homologacao externa BLOQUEADA ate haver credenciais;
-- portanto esta migration cria APENAS o mundo interno: nenhum status externo
-- pode surgir sem evidencia valida (chave de acesso 44 digitos + momento).
--
-- Alteracoes oficiais:
--   1. adm360_parametros_fiscais: UMA linha por tenant com os parametros da
--      emissao (UF, municipio, regime, operacao, CFOPs por operacao em jsonb
--      - sem CFOP universal hardcodado -, responsavel, ambiente, provedor e
--      certificado_referencia = NOME do segredo no user-secrets/ambiente,
--      nunca o valor — A33). A decisao comercial ausente fica explicita:
--      qualquer campo obrigatorio NULL => status PENDENTE_DE_CONFIGURACAO,
--      garantida por CHECK (status "CONFIGURADO"/"BLOQUEADO" so com o
--      conjunto completo + ambiente escolhido). O operador ve "PENDENTE DE
--      CONFIGURACAO" na tela exatamente enquanto houver pendencia (P2),
--      sem sucesso ficticio de emissao.
--   2. adm360_notas_pre_emitidas: pre-nota interna (A29 MVP: pre-documento +
--      conferencia de referencias; SEM emissao autorizada — isso e P1).
--      Maquina de estados: RASCUNHO -> PRONTA_PARA_EMISSAO -> ENVIANDO ->
--      AUTORIZADA | REJEITADA (+ PENDENTE_CONFIRMACAO para resultado duvidoso)
--      e CANCELADA. INVARIANTE DE EVIDENCIA no proprio banco (H19: emitir
--      bloqueado != sucesso falso):
--        * AUTORIZADA exige chave_acesso_externa (44 digitos) + emitida_em;
--        * REJEITADA exige emitida_em;
--        * CANCELADA exige cancelada_em + motivo_cancelamento.
--      Numero interno sequencial ('NPE-' + 8 digitos) via sequencia, mesmo
--      padrao dos pedidos ADM360 ('PC-').
--   3. adm360_nota_pre_emitida_itens: itens da pre-nota (descricao,
--      quantidade, preco unitario, total). Sem calculo automatico de
--      tributos (A30): o total e registro interno declarado pelo operador.
--   4. adm360_eventos: ampliacao idempotente do CHECK de tipo_evento com
--      EMISSAO_FISCAL (mesmo padrao nomeado do v2314), permitindo a trilha
--      imutavel de transicoes de estado da pre-nota (rastreabilidade B7).
--
-- Idempotencia: CREATE SEQUENCE/TABLE/INDEX IF NOT EXISTS + bloco DO que so
-- recria a constraint quando nenhuma versao contendo EMISSAO_FISCAL existe;
-- pode ser reaplicada em qualquer ordem/replicacao do banco.
-- ============================================================================

-- 1. Parametros fiscais de emissao (uma linha por tenant) -------------------
CREATE TABLE IF NOT EXISTS plantaopro.adm360_parametros_fiscais(
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null references plantaopro.tenants(id),
    uf char(2) check(uf is null or length(btrim(uf)) = 2),
    municipio varchar(120),
    regime_fiscal varchar(20) check(regime_fiscal is null or regime_fiscal in ('SIMPLES_NACIONAL','LUCRO_PRESUMIDO','LUCRO_REAL')),
    operacao_fiscal varchar(12) check(operacao_fiscal is null or operacao_fiscal in ('VENDA','REMESSA','RETORNO')),
    cfops jsonb not null default '{}'::jsonb,
    responsavel_id uuid,
    responsavel_nome varchar(160),
    ambiente varchar(12) not null default 'PENDENTE' check(ambiente in ('PENDENTE','HOMOLOGACAO','PRODUCAO')),
    provedor varchar(12) check(provedor is null or provedor in ('SEFAZ_DIRETO','OPMENEXO','INPART','OUTRO')),
    certificado_referencia varchar(120),
    status varchar(32) not null default 'PENDENTE_DE_CONFIGURACAO'
        check(status in ('PENDENTE_DE_CONFIGURACAO','CONFIGURADO','BLOQUEADO')),
    observacao text,
    created_by uuid,
    updated_by uuid,
    created_at timestamptz not null default now(),
    updated_at timestamptz,
    unique(tenant_id),
    unique(tenant_id,id),
    -- Pendencia P2 formalizada: status "decidido" (CONFIGURADO/BLOQUEADO)
    -- somente com o conjunto comercial completo e ambiente escolhido;
    -- enquanto faltar qualquer campo, a situacao e PENDENTE_DE_CONFIGURACAO.
    check((uf is not null and municipio is not null and regime_fiscal is not null
           and operacao_fiscal is not null and provedor is not null
           and ambiente in ('HOMOLOGACAO','PRODUCAO'))
          = (status in ('CONFIGURADO','BLOQUEADO')))
);

comment on table plantaopro.adm360_parametros_fiscais is
    'ADM360 Fiscal: parametros de emissao por tenant. Campos nulos registram a pendencia de decisao comercial (operacao/UF/regime/provedor) — status PENDENTE_DE_CONFIGURACAO ate completar.';
comment on column plantaopro.adm360_parametros_fiscais.cfops is
    'CFOP por operacao (jsonb: {"VENDA":"5101","REMESSA":"5405",...}). Sem CFOP universal hardcodado; ausente/{} significa pendencia de parametrizacao.';
comment on column plantaopro.adm360_parametros_fiscais.certificado_referencia is
    'NOME da referencia do segredo (user-secrets/variavel de ambiente) que guarda senha/token/certificado A1 — nunca o valor em si (A33).';
comment on column plantaopro.adm360_parametros_fiscais.status is
    'PENDENTE_DE_CONFIGURACAO: falta decisao comercial (P2). CONFIGURADO: conjunto completo + ambiente. BLOQUEADO: completo, mas emissao externa bloqueada (ex.: credencial ainda indisponivel).';

-- 2. Pre-nota de emissao (documento interno — A29 MVP) -----------------------
CREATE SEQUENCE IF NOT EXISTS plantaopro.adm360_nota_pre_numero;

CREATE TABLE IF NOT EXISTS plantaopro.adm360_notas_pre_emitidas(
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null references plantaopro.tenants(id),
    numero varchar(24) not null,
    origem_tipo varchar(12) not null check(origem_tipo in ('MANUAL','VENDA','ORCAMENTO','COTACAO')),
    origem_id uuid,
    situacao varchar(24) not null default 'RASCUNHO'
        check(situacao in ('RASCUNHO','PRONTA_PARA_EMISSAO','ENVIANDO','AUTORIZADA','REJEITADA','PENDENTE_CONFIRMACAO','CANCELADA')),
    destinatario_nome varchar(180) not null,
    destinatario_documento varchar(20),
    itens_total numeric(18,4) not null default 0 check(itens_total >= 0),
    valor_total numeric(18,4) not null default 0 check(valor_total >= 0),
    chave_acesso_externa varchar(44) check(chave_acesso_externa is null or chave_acesso_externa ~ '^[0-9]{44}$'),
    protocolo_externo varchar(80),
    emitida_em timestamptz,
    rejeicao_mensagem text,
    cancelada_em timestamptz,
    motivo_cancelamento text,
    idempotency_key varchar(160),
    versao bigint not null default 1,
    created_by uuid,
    created_at timestamptz not null default now(),
    updated_at timestamptz,
    updated_by uuid,
    unique(tenant_id,id),
    unique(tenant_id,numero),
    unique(tenant_id,idempotency_key),
    -- Invariante de evidencia (A29/H19): status EXTERNO somente com evidencia
    -- valida gravada — o banco impede "NF-e autorizada" sem chave + momento.
    check(situacao <> 'AUTORIZADA' or (chave_acesso_externa is not null and emitida_em is not null)),
    check(situacao <> 'REJEITADA' or emitida_em is not null),
    check((situacao = 'CANCELADA') = (cancelada_em is not null)),
    check(situacao <> 'CANCELADA' or motivo_cancelamento is not null)
);

create index if not exists ix_adm360_notas_pre_filtro
    on plantaopro.adm360_notas_pre_emitidas(tenant_id,situacao,created_at desc);

comment on table plantaopro.adm360_notas_pre_emitidas is
    'ADM360 Fiscal: pre-nota interna (MVP A29). Sem emissao autorizada externa no MVP (isso e P1); o documento vive nos estados internos e apenas AUTORIZADA/REJEITADA exigem evidencia externa valida (CHECK).';
comment on column plantaopro.adm360_notas_pre_emitidas.numero is
    'Numero interno sequencial por tenant ("NPE-" + 8 digitos, sequencia adm360_nota_pre_numero) — identificacao interna, nao numero fiscal.';
comment on column plantaopro.adm360_notas_pre_emitidas.chave_acesso_externa is
    'Chave de acesso NF-e (44 digitos) devolvida pela autorizacao externa — evidencia obrigatoria para situacao AUTORIZADA.';

-- 3. Itens da pre-nota ---------------------------------------------------------
CREATE TABLE IF NOT EXISTS plantaopro.adm360_nota_pre_emitida_itens(
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null,
    nota_id uuid not null,
    descricao varchar(180) not null,
    quantidade numeric(18,4) not null check(quantidade > 0),
    preco_unitario numeric(18,4) not null default 0 check(preco_unitario >= 0),
    total numeric(18,4) not null default 0 check(total >= 0),
    created_at timestamptz not null default now(),
    unique(tenant_id,id),
    foreign key(tenant_id,nota_id) references plantaopro.adm360_notas_pre_emitidas(tenant_id,id)
);

create index if not exists ix_adm360_notas_pre_itens_nota
    on plantaopro.adm360_nota_pre_emitida_itens(tenant_id,nota_id);

comment on table plantaopro.adm360_nota_pre_emitida_itens is
    'ADM360 Fiscal: itens da pre-nota (sem calculo automatico de tributos — A30; total e valor interno declarado).';

-- 4. Ampliacao do CHECK de tipo_evento com EMISSAO_FISCAL --------------------
DO $migration$
DECLARE
  r record;
  tem_novo boolean;
BEGIN
  ALTER TABLE plantaopro.adm360_eventos DROP CONSTRAINT IF EXISTS ck_adm360_eventos_tipo_evento;

  -- Remove checks legados anteriores (definicao varia entre instalacoes) que
  -- ja contenham TRIAGEM (v2314) mas ainda nao contenham EMISSAO_FISCAL.
  FOR r IN
    SELECT con.conname
    FROM pg_constraint con
    JOIN pg_class rel ON rel.oid = con.conrelid
    JOIN pg_attribute at ON at.attrelid = con.conrelid AND at.attname = 'tipo_evento'
    WHERE rel.relnamespace = 'plantaopro'::regnamespace
      AND rel.relname = 'adm360_eventos'
      AND con.contype = 'c'
      AND con.conkey @> ARRAY[at.attnum]
      AND pg_get_constraintdef(con.oid) LIKE '%''TRIAGEM'''
      AND pg_get_constraintdef(con.oid) NOT LIKE '%''EMISSAO_FISCAL'''
  LOOP
    EXECUTE format('ALTER TABLE plantaopro.adm360_eventos DROP CONSTRAINT %I', r.conname);
  END LOOP;

  SELECT count(*) > 0 INTO tem_novo
  FROM pg_constraint con
  JOIN pg_class rel ON rel.oid = con.conrelid
  WHERE rel.relnamespace = 'plantaopro'::regnamespace
    AND rel.relname = 'adm360_eventos'
    AND con.contype = 'c'
    AND pg_get_constraintdef(con.oid) LIKE '%''EMISSAO_FISCAL''%';

  IF NOT tem_novo THEN
    ALTER TABLE plantaopro.adm360_eventos
      ADD CONSTRAINT ck_adm360_eventos_tipo_evento
      CHECK (tipo_evento IN ('APROVACAO', 'ARQUIVO', 'DECLARACAO_MANUAL', 'RETORNO_EXTERNO',
                             'CONFIRMACAO_CONFERENCIA', 'ESTORNO', 'CANCELAMENTO', 'TRIAGEM',
                             'EMISSAO_FISCAL'));
  END IF;
END $migration$;

-- ============================================================
-- Seção 93 — Administrativo360 Grants Adm Cliente R5 A2 v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2322_adm360_grants_adm_cliente_r5_a2.sql
-- SOURCE-SHA256: 1994d6ba58125896ecb57efb792cad804dde0cd2676364138bde7135560e2191
-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2322_adm360_grants_adm_cliente_r5_a2
-- Administrativo 360 - Fiscal (R5-A2): grants ADM360 para ADMINISTRADOR_CLIENTE.
--
-- Contexto: a API fiscal (api/administrativo360/fiscal) autoriza POR ACAO
-- (Adm360.Ver/Configurar/Criar/Editar/Reabrir/Confirmar/Cancelar + as acoes de
-- XML ja existentes). Perfis ADMINISTRADOR_CLIENTE criados antes desta rodada
-- (seed antigo ou provisionamento manual) nao possuem esses grants, entao as
-- telas fiscais negariam tudo com PERMISSAO_NEGADA mesmo para o gestor do
-- cliente. Esta migration concede os 13 codigos ADM360 aos perfis ativos
-- codigo='ADMINISTRADOR_CLIENTE'.
--
-- Dedupe: permissoes.codigo mistura ponto e dois-pontos (linhas historicas
-- duplicadas, ex.: ADM360.VER e ADM360:VER). A escolha e deterministica
-- (distinct on do codigo normalizado ':' -> '.', menor id) e a insercao so
-- acontece quando o perfil ainda nao tem NENHUM grant com o mesmo codigo
-- normalizado — reaplicacao nao duplica nada (idempotente).
-- ============================================================================

DO $migration$
BEGIN
    -- Sanity: os 13 codigos precisam existir (forma ponto ou dois-pontos).
    IF EXISTS (
        SELECT 1
          FROM (VALUES ('ADM360.VER'),('ADM360.EXPORTAR'),('ADM360.IMPORTAR_XML'),
                       ('ADM360.CRIAR'),('ADM360.EDITAR'),('ADM360.CONFIGURAR'),
                       ('ADM360.REABRIR'),('ADM360.CONFIRMAR'),('ADM360.CANCELAR'),
                       ('ADM360.CONFERIR'),('ADM360.MANIFESTAR_DFE'),
                       ('ADM360.VINCULAR_DOCUMENTOS'),('ADM360.TRANSMITIR_RESPOSTA')) AS alvo(codigo)
         WHERE NOT EXISTS (
             SELECT 1 FROM plantaopro.permissoes pe
              WHERE replace(pe.codigo, ':', '.') = alvo.codigo)
    ) THEN
        RAISE EXCEPTION 'v2322: codigo ADM360 esperado ausente em plantaopro.permissoes';
    END IF;

    INSERT INTO plantaopro.perfil_permissoes(id, perfil_id, permissao_id, permitido, reg_date, reg_status)
    SELECT gen_random_uuid(), pf.id, esc.id, true, now(), 'A'
      FROM plantaopro.perfis pf
      CROSS JOIN (VALUES ('ADM360.VER'),('ADM360.EXPORTAR'),('ADM360.IMPORTAR_XML'),
                         ('ADM360.CRIAR'),('ADM360.EDITAR'),('ADM360.CONFIGURAR'),
                         ('ADM360.REABRIR'),('ADM360.CONFIRMAR'),('ADM360.CANCELAR'),
                         ('ADM360.CONFERIR'),('ADM360.MANIFESTAR_DFE'),
                         ('ADM360.VINCULAR_DOCUMENTOS'),('ADM360.TRANSMITIR_RESPOSTA')) AS alvo(codigo)
      JOIN LATERAL (
          SELECT pe.id
            FROM plantaopro.permissoes pe
           WHERE replace(pe.codigo, ':', '.') = alvo.codigo
           ORDER BY pe.id
           LIMIT 1
      ) AS esc ON true
     WHERE pf.codigo = 'ADMINISTRADOR_CLIENTE'
       AND pf.reg_status = 'A'
       AND NOT EXISTS (
           SELECT 1
             FROM plantaopro.perfil_permissoes pp
             JOIN plantaopro.permissoes pe2 ON pe2.id = pp.permissao_id
            WHERE pp.perfil_id = pf.id
              AND pp.reg_status = 'A'
              AND replace(pe2.codigo, ':', '.') = alvo.codigo
       );
END $migration$;

-- ============================================================
-- Seção 94 — B4 Matriz Comercial Solicitacoes v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2323_b4_matriz_comercial_solicitacoes.sql
-- SOURCE-SHA256: 5b02855bf3374750252687bda7f2efc3162af36772dfebc253e1cd3531b4ade0
-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2323_b4_matriz_comercial_solicitacoes
-- B4 (matriz comercial canonica): reconcilia o shape das tabelas comerciais que
-- duas migrations concorrentes criaram com tipos diferentes (o "IF NOT EXISTS"
-- congela o shape de quem rodou primeiro):
--
-- 1. plantaopro.planos: publico/destaque/permite_white_label nasceram TEXT
--    ('true'/'false'/NULL) mas o codigo le como boolean (ListarPlanosPublicos
--    quebrava com 42804 e o /planos/publicos devolvia 500 para todo mundo);
--    ordem nasceu TEXT mas o codigo ordena como numero. Conversao com USING
--    explicito: NULL vira false/999 (negacao por padrao em exposicao publica;
--    decisao registrada em r5b4-matriz-comercial.md).
-- 2. plantaopro.upgrade_solicitacoes / downgrade_solicitacoes: nasceram com
--    colunas genericas (codigo/nome/dados), mas o self-service insere colunas
--    ricas (assinatura_id/plano_destino_id/motivo/...) -> 500 em
--    solicitar-upgrade/downgrade/cancelamento. Adiciona as colunas que faltam
--    + colunas de decisao (decidido_por/decidido_em/justificativa_decisao) para
--    a aprovacao B2B das solicitacoes (estados explicitos).
--
-- Idempotencia: cada bloco verifica o catalogo antes de alterar; reaplicacao
-- nao muda nada. Nao ha UPDATE de dados alem da conversao de tipo (USING).
-- ============================================================================

DO $migration$
BEGIN
    -- 1a. planos.publico TEXT -> boolean (NULL vira false: plano interno/rascunho
    -- nao aparece no catalogo publico ate decisao comercial explicita).
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
         WHERE table_schema = 'plantaopro' AND table_name = 'planos'
           AND column_name = 'publico' AND data_type <> 'boolean'
    ) THEN
        ALTER TABLE plantaopro.planos ALTER COLUMN publico TYPE boolean
            USING (coalesce(lower(nullif(publico, '')), 'false') IN ('true', 't', '1', 's', 'sim', 'y', 'yes'));
    END IF;

    -- 1b. planos.destaque TEXT -> boolean (NULL vira false).
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
         WHERE table_schema = 'plantaopro' AND table_name = 'planos'
           AND column_name = 'destaque' AND data_type <> 'boolean'
    ) THEN
        ALTER TABLE plantaopro.planos ALTER COLUMN destaque TYPE boolean
            USING (coalesce(lower(nullif(destaque, '')), 'false') IN ('true', 't', '1', 's', 'sim', 'y', 'yes'));
    END IF;

    -- 1c. planos.permite_white_label TEXT -> boolean (NULL vira false).
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
         WHERE table_schema = 'plantaopro' AND table_name = 'planos'
           AND column_name = 'permite_white_label' AND data_type <> 'boolean'
    ) THEN
        ALTER TABLE plantaopro.planos ALTER COLUMN permite_white_label TYPE boolean
            USING (coalesce(lower(nullif(permite_white_label, '')), 'false') IN ('true', 't', '1', 's', 'sim', 'y', 'yes'));
    END IF;

    -- 1d. planos.ordem TEXT -> integer (nao-numerico/NULL vira 999 = fim da lista,
    -- mesmo fallback do ORDER BY do catalogo publico).
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
         WHERE table_schema = 'plantaopro' AND table_name = 'planos'
           AND column_name = 'ordem' AND data_type <> 'integer'
    ) THEN
        ALTER TABLE plantaopro.planos ALTER COLUMN ordem TYPE integer
            USING (CASE WHEN ordem ~ '^[0-9]+$' THEN ordem::integer ELSE 999 END);
    END IF;

    -- 2a. upgrade_solicitacoes: colunas ricas do self-service + decisao B2B.
    ALTER TABLE plantaopro.upgrade_solicitacoes
        ADD COLUMN IF NOT EXISTS assinatura_id uuid NULL,
        ADD COLUMN IF NOT EXISTS plano_atual_id uuid NULL,
        ADD COLUMN IF NOT EXISTS plano_destino_id uuid NULL,
        ADD COLUMN IF NOT EXISTS motivo text NULL,
        ADD COLUMN IF NOT EXISTS solicitado_por uuid NULL,
        ADD COLUMN IF NOT EXISTS decidido_por uuid NULL,
        ADD COLUMN IF NOT EXISTS decidido_em timestamptz NULL,
        ADD COLUMN IF NOT EXISTS justificativa_decisao text NULL;
    CREATE INDEX IF NOT EXISTS ix_upgrade_solicitacoes_assinatura_id
        ON plantaopro.upgrade_solicitacoes(assinatura_id);

    -- 2b. downgrade_solicitacoes: idem + flags de impacto (cancelamento usa esta
    -- tabela com status='CANCELAMENTO_SOLICITADO' e plano_destino = plano atual).
    ALTER TABLE plantaopro.downgrade_solicitacoes
        ADD COLUMN IF NOT EXISTS assinatura_id uuid NULL,
        ADD COLUMN IF NOT EXISTS plano_atual_id uuid NULL,
        ADD COLUMN IF NOT EXISTS plano_destino_id uuid NULL,
        ADD COLUMN IF NOT EXISTS motivo text NULL,
        ADD COLUMN IF NOT EXISTS impacto_validado boolean NOT NULL DEFAULT false,
        ADD COLUMN IF NOT EXISTS bloqueado boolean NOT NULL DEFAULT false,
        ADD COLUMN IF NOT EXISTS solicitado_por uuid NULL,
        ADD COLUMN IF NOT EXISTS decidido_por uuid NULL,
        ADD COLUMN IF NOT EXISTS decidido_em timestamptz NULL,
        ADD COLUMN IF NOT EXISTS justificativa_decisao text NULL;
    CREATE INDEX IF NOT EXISTS ix_downgrade_solicitacoes_assinatura_id
        ON plantaopro.downgrade_solicitacoes(assinatura_id);
END $migration$;

-- ============================================================
-- Seção 95 — B4 Plano Modulos Canonicos v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2324_b4_plano_modulos_canonicos.sql
-- SOURCE-SHA256: 63b0e1f517295cc9aa27c55cd5bd1477479b348e46a1b4263d61b81007fa9371
-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2324_b4_plano_modulos_canonicos
-- B4 (matriz comercial canonica): reconcilia plantaopro.plano_modulos.
-- Tres migrations concorrentes criaram a tabela com shapes diferentes
-- (generico / habilitado-status / incluido-limite-preco_adicional); o codigo
-- canonico (LinkPlan + GET matriz) usa o terceiro. Como a tabela esta vazia
-- nos bancos conhecidos, a reconciliacao e aditiva e sem risco de dados:
--
--   1. garante as colunas canonicas (modulo_id, codigo_modulo, incluido,
--      limite, preco_adicional);
--   2. retroage incluido=false onde habilitado=false (bancos que ja tenham
--      linhas no shape antigo);
--   3. cria o indice unico ux_plano_modulos_ativo exigido pelo ON CONFLICT
--      do vincular-plano (sem ele, o upsert quebra).
--
-- Idempotencia: ADD COLUMN / CREATE INDEX IF NOT EXISTS + blocos
-- condicionais ao catalogo; reaplicacao nao muda nada.
-- ============================================================================

DO $migration$
BEGIN
    ALTER TABLE plantaopro.plano_modulos
        ADD COLUMN IF NOT EXISTS modulo_id uuid NULL,
        ADD COLUMN IF NOT EXISTS codigo_modulo text NULL,
        ADD COLUMN IF NOT EXISTS incluido boolean NOT NULL DEFAULT true,
        ADD COLUMN IF NOT EXISTS limite integer NULL,
        ADD COLUMN IF NOT EXISTS preco_adicional numeric(14, 2) NULL;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
         WHERE table_schema = 'plantaopro' AND table_name = 'plano_modulos'
           AND column_name = 'habilitado'
    ) THEN
        UPDATE plantaopro.plano_modulos
           SET incluido = false
         WHERE coalesce(habilitado, true) = false
           AND incluido = true;
    END IF;

    CREATE UNIQUE INDEX IF NOT EXISTS ux_plano_modulos_ativo
        ON plantaopro.plano_modulos(plano_id, modulo_id)
        WHERE reg_status = 'A' AND modulo_id IS NOT NULL;
END $migration$;

-- ============================================================
-- Seção 96 — B4 Plano Recursos Colunas v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2325_b4_plano_recursos_colunas.sql
-- SOURCE-SHA256: 797e597937f13830b53997581a8e41bafe17e014034bee04bcc0320a467db73a
-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2325_b4_plano_recursos_colunas
-- B4 (matriz comercial canonica): reconcilia plantaopro.plano_recursos.
-- O shape canonico (saas_inteligente_auditavel) tem recurso/habilitado/
-- limite, mas o contrato vigente da API (GET/PUT planos/{id}/recursos e a
-- matriz B4) usa codigo/nome/descricao — colunas que faltam nos bancos onde
-- outra migration criou a tabela primeiro (42703 no GET/PUT). Adiciona as
-- colunas que faltam sem tocar nas existentes.
--
-- Idempotencia: ADD COLUMN IF NOT EXISTS; reaplicacao nao muda nada.
-- ============================================================================

DO $migration$
BEGIN
    ALTER TABLE plantaopro.plano_recursos
        ADD COLUMN IF NOT EXISTS codigo varchar(80) NULL,
        ADD COLUMN IF NOT EXISTS nome varchar(160) NULL,
        ADD COLUMN IF NOT EXISTS descricao text NULL;
END $migration$;

-- ============================================================
-- Seção 97 — B4 Assinatura Historico Colunas v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2326_b4_assinatura_historico_colunas.sql
-- SOURCE-SHA256: 52688af05632eeec06ded37ca238c361df6d25e61d94ef3aae8973b98680c5b1
-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2326_b4_assinatura_historico_colunas
-- B4 (estados explicitos): reconcilia plantaopro.assinatura_historico.
-- O alterar-plano manual B2B (e a aprovacao de upgrade/downgrade B4) grava
-- (plano_id_anterior, plano_id_novo, acao, justificativa) — colunas que
-- nenhuma migration criou nos bancos vigentes (42703 no ALTERAR_PLANO real).
-- Adiciona as colunas que faltam sem tocar nas existentes.
--
-- Idempotencia: ADD COLUMN IF NOT EXISTS; reaplicacao nao muda nada.
-- ============================================================================

DO $migration$
BEGIN
    ALTER TABLE plantaopro.assinatura_historico
        ADD COLUMN IF NOT EXISTS plano_id_anterior uuid NULL,
        ADD COLUMN IF NOT EXISTS plano_id_novo uuid NULL,
        ADD COLUMN IF NOT EXISTS acao varchar(40) NULL,
        ADD COLUMN IF NOT EXISTS justificativa text NULL,
        ADD COLUMN IF NOT EXISTS reg_update timestamptz NULL;
END $migration$;

-- ============================================================
-- Seção 98 — B4 Historico Reg Status v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2327_b4_historico_reg_status.sql
-- SOURCE-SHA256: f4523c5ca99a5ee31361400b5a467d669f64eaa7153f725f17ae1249348394de
-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2327_b4_historico_reg_status
-- B4: complementa a v2326 — plantaopro.assinatura_historico tambem nao tem
-- reg_status nos bancos vigentes (o shape canonico auditavel tem). Sem ela,
-- o INSERT do alterar-plano/aprovacao quebra com 42703.
--
-- Idempotencia: ADD COLUMN IF NOT EXISTS; reaplicacao nao muda nada.
-- ============================================================================

DO $migration$
BEGIN
    ALTER TABLE plantaopro.assinatura_historico
        ADD COLUMN IF NOT EXISTS reg_status char(1) NOT NULL DEFAULT 'A';
END $migration$;

-- ============================================================
-- Seção 99 — B5 Provisionamento Convites v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2328_b5_provisionamento_convites.sql
-- SOURCE-SHA256: 7ad01e18a79b7884bb53ef7b37644d82a75416cc676a1e2e1a48f3d97691b1cf
-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2328_b5_provisionamento_convites
-- B5 (provisionar cliente novo de verdade):
--
-- 1. plantaopro.clientes: o finalizar self-service grava email/telefone/
--    cidade/estado/plano_id — colunas que faltam nos bancos vigentes (42703
--    no finalizar real). Adiciona sem tocar nas existentes.
-- 2. plantaopro.usuario_convites (NOVA): convite de equipe com expiracao e
--    uso unico. Guarda SÓ o hash SHA-256 do token (nunca o token); estado
--    derivado (PENDENTE = sem uso/revogacao e dentro da validade). Indice
--    unico parcial impede dois convites pendentes para o mesmo e-mail no
--    tenant (re-convite idempotente = 409 honesto, não duplicata).
--
-- Idempotencia: CREATE/ADD/INDEX IF NOT EXISTS; reaplicacao nao muda nada.
-- ============================================================================

DO $migration$
BEGIN
    -- 1. clientes: colunas do contrato de provisionamento.
    ALTER TABLE plantaopro.clientes
        ADD COLUMN IF NOT EXISTS email varchar(254) NULL,
        ADD COLUMN IF NOT EXISTS telefone varchar(40) NULL,
        ADD COLUMN IF NOT EXISTS cidade varchar(120) NULL,
        ADD COLUMN IF NOT EXISTS estado varchar(60) NULL,
        ADD COLUMN IF NOT EXISTS plano_id uuid NULL;

    -- 2. convites de equipe (token com expiracao + uso unico).
    CREATE TABLE IF NOT EXISTS plantaopro.usuario_convites (
        id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
        tenant_id uuid NOT NULL,
        cliente_id uuid NOT NULL,
        email varchar(254) NOT NULL,
        perfil_ids uuid[] NOT NULL,
        token_hash char(64) NOT NULL,
        expira_em timestamptz NOT NULL,
        usado_em timestamptz NULL,
        usado_por uuid NULL,
        revogado_em timestamptz NULL,
        criado_por uuid NULL,
        reg_date timestamptz NOT NULL DEFAULT now(),
        reg_update timestamptz NULL,
        reg_status char(1) NOT NULL DEFAULT 'A'
    );
    CREATE UNIQUE INDEX IF NOT EXISTS ux_usuario_convites_token
        ON plantaopro.usuario_convites(token_hash)
        WHERE reg_status = 'A';
    CREATE UNIQUE INDEX IF NOT EXISTS ux_usuario_convites_pendente
        ON plantaopro.usuario_convites(tenant_id, lower(email))
        WHERE reg_status = 'A' AND usado_em IS NULL AND revogado_em IS NULL;
    CREATE INDEX IF NOT EXISTS ix_usuario_convites_tenant
        ON plantaopro.usuario_convites(tenant_id, reg_date DESC);
END $migration$;

-- ============================================================
-- Seção 100 — B5 Grants Usuarios Adm Cliente v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2329_b5_grants_usuarios_adm_cliente.sql
-- SOURCE-SHA256: 505f10f5f1e06704b7e53bce3a55a44bc9fc7b0a638a0dca4033be1799e57ce0
-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2329_b5_grants_usuarios_adm_cliente
-- B5 (convidar equipe): grants USUARIOS para ADMINISTRADOR_CLIENTE.
-- As telas de equipe/convites autorizam POR ACAO (USUARIOS.VER/CRIAR/EDITAR/
-- CONVIDAR); perfis de cliente criados antes desta rodada nao tem esses
-- grants e negariam tudo com PERMISSAO_NEGADA. Mesmo padrao dedupe da v2322
-- (distinct por codigo normalizado + not-exists = idempotente).
-- ============================================================================

DO $migration$
BEGIN
    IF EXISTS (
        SELECT 1
          FROM (VALUES ('USUARIOS.VER'),('USUARIOS.CRIAR'),('USUARIOS.EDITAR'),('USUARIOS.CONVIDAR')) AS alvo(codigo)
         WHERE NOT EXISTS (
             SELECT 1 FROM plantaopro.permissoes pe
              WHERE replace(pe.codigo, ':', '.') = alvo.codigo)
    ) THEN
        RAISE EXCEPTION 'v2329: codigo USUARIOS esperado ausente em plantaopro.permissoes';
    END IF;

    INSERT INTO plantaopro.perfil_permissoes(id, perfil_id, permissao_id, permitido, reg_date, reg_status)
    SELECT gen_random_uuid(), pf.id, esc.id, true, now(), 'A'
      FROM plantaopro.perfis pf
      CROSS JOIN (VALUES ('USUARIOS.VER'),('USUARIOS.CRIAR'),('USUARIOS.EDITAR'),('USUARIOS.CONVIDAR')) AS alvo(codigo)
      JOIN LATERAL (
          SELECT pe.id
            FROM plantaopro.permissoes pe
           WHERE replace(pe.codigo, ':', '.') = alvo.codigo
           ORDER BY pe.id
           LIMIT 1
      ) AS esc ON true
     WHERE pf.codigo = 'ADMINISTRADOR_CLIENTE'
       AND pf.reg_status = 'A'
       AND NOT EXISTS (
           SELECT 1
             FROM plantaopro.perfil_permissoes pp
             JOIN plantaopro.permissoes pe2 ON pe2.id = pp.permissao_id
            WHERE pp.perfil_id = pf.id
              AND pp.reg_status = 'A'
              AND replace(pe2.codigo, ':', '.') = alvo.codigo
       );
END $migration$;

-- ============================================================
-- Seção 101 — B5 Onboarding Whitelabel Colunas v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2330_b5_onboarding_whitelabel_colunas.sql
-- SOURCE-SHA256: 0aa20335a227cd35786c8ed33cce31a16eb98022a5246c6b890dce1c661badc4
-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2330_b5_onboarding_whitelabel_colunas
-- B5 (provisionar cliente novo): reconcilia tenant_white_label,
-- tenant_onboarding e tenant_onboarding_checklist. O provisionamento
-- (CriarWhiteLabelPadrao/CriarOnboarding) e as leituras usam o shape
-- canonico (self_service_white_label), mas os bancos vigentes tem o shape
-- generico (42703 no finalizar real). Adiciona as colunas que faltam.
--
-- Idempotencia: ADD COLUMN IF NOT EXISTS; reaplicacao nao muda nada.
-- ============================================================================

DO $migration$
BEGIN
    ALTER TABLE plantaopro.tenant_white_label
        ADD COLUMN IF NOT EXISTS nome_plataforma text NULL,
        ADD COLUMN IF NOT EXISTS cliente_nome text NULL,
        ADD COLUMN IF NOT EXISTS slogan text NULL,
        ADD COLUMN IF NOT EXISTS logo_url text NULL,
        ADD COLUMN IF NOT EXISTS logo_reduzida_url text NULL,
        ADD COLUMN IF NOT EXISTS favicon_url text NULL,
        ADD COLUMN IF NOT EXISTS cor_primaria text NULL,
        ADD COLUMN IF NOT EXISTS cor_secundaria text NULL,
        ADD COLUMN IF NOT EXISTS cor_fundo text NULL,
        ADD COLUMN IF NOT EXISTS cor_menu text NULL,
        ADD COLUMN IF NOT EXISTS tema text NULL,
        ADD COLUMN IF NOT EXISTS email_remetente text NULL,
        ADD COLUMN IF NOT EXISTS texto_boas_vindas text NULL,
        ADD COLUMN IF NOT EXISTS texto_rodape text NULL,
        ADD COLUMN IF NOT EXISTS login_banner_url text NULL;

    ALTER TABLE plantaopro.tenant_onboarding
        ADD COLUMN IF NOT EXISTS progresso int NULL,
        ADD COLUMN IF NOT EXISTS proxima_acao text NULL,
        ADD COLUMN IF NOT EXISTS iniciado_em timestamptz NULL,
        ADD COLUMN IF NOT EXISTS finalizado_em timestamptz NULL;

    ALTER TABLE plantaopro.tenant_onboarding_checklist
        ADD COLUMN IF NOT EXISTS onboarding_id uuid NULL,
        ADD COLUMN IF NOT EXISTS titulo text NULL,
        ADD COLUMN IF NOT EXISTS descricao text NULL,
        ADD COLUMN IF NOT EXISTS ordem int NULL,
        ADD COLUMN IF NOT EXISTS obrigatorio boolean NULL,
        ADD COLUMN IF NOT EXISTS concluido boolean NOT NULL DEFAULT false,
        ADD COLUMN IF NOT EXISTS concluido_em timestamptz NULL,
        ADD COLUMN IF NOT EXISTS link_acao text NULL;
END $migration$;

-- ============================================================
-- Seção 102 — B5 Lgpd Consentimentos Colunas v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2331_b5_lgpd_consentimentos_colunas.sql
-- SOURCE-SHA256: 7744c1349b4946f2b677897bea42dd5e9600b2dbb53b4807cf2c14082bc8e646
-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2331_b5_lgpd_consentimentos_colunas
-- B5 (provisionar cliente novo): reconcilia plantaopro.lgpd_consentimentos.
-- O provisionamento grava (titular_email, aceito, origem, ip_origem) do shape
-- canonico (self_service_white_label), mas os bancos vigentes tem outro
-- shape (42703 no finalizar real). Adiciona as colunas que faltam; o codigo
-- passa a gravar tambem consentido/ip exigidos pelo shape vigente.
--
-- Idempotencia: ADD COLUMN IF NOT EXISTS; reaplicacao nao muda nada.
-- ============================================================================

DO $migration$
BEGIN
    ALTER TABLE plantaopro.lgpd_consentimentos
        ADD COLUMN IF NOT EXISTS titular_email text NULL,
        ADD COLUMN IF NOT EXISTS politica_id uuid NULL,
        ADD COLUMN IF NOT EXISTS aceito boolean NULL,
        ADD COLUMN IF NOT EXISTS origem text NULL,
        ADD COLUMN IF NOT EXISTS ip_origem text NULL;
END $migration$;

-- ============================================================
-- Seção 103 — B6 Cobranca Saas v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2332_b6_cobranca_saas.sql
-- SOURCE-SHA256: b6aee89b3a44863c0a4399aac0f971065e46d6fd555d5baf16415b13120b74ab
-- R5-B6: cobranca SaaS de verdade (provider sandbox + webhook assinado) e reconciliacao
-- dos shapes congelados de faturas_saas/pagamentos_saas.
-- Contexto: criacoes concorrentes com "if not exists" congelaram as tabelas no formato
-- placeholder em bancos reais. O BFF canônico (api/faturamento-saas) ja escreve em
-- data_pagamento/forma_pagamento/motivo_*/resposta_contestacao/observacoes e essas
-- colunas nunca existiram nesses bancos (42703 escondido por testes de contrato).
-- Esta migration e aditiva e idempotente: ADD COLUMN IF NOT EXISTS preserva quem ja tem.
alter table plantaopro.faturas_saas add column if not exists data_pagamento date null;
alter table plantaopro.faturas_saas add column if not exists forma_pagamento varchar(60) null;
alter table plantaopro.faturas_saas add column if not exists motivo_cancelamento text null;
alter table plantaopro.faturas_saas add column if not exists motivo_contestacao text null;
alter table plantaopro.faturas_saas add column if not exists resposta_contestacao text null;
alter table plantaopro.faturas_saas add column if not exists valor numeric(14,2) not null default 0;
alter table plantaopro.faturas_saas add column if not exists valor_pago numeric(14,2) null;
alter table plantaopro.faturas_saas add column if not exists assinatura_id uuid null;

alter table plantaopro.pagamentos_saas add column if not exists data_pagamento date null;
alter table plantaopro.pagamentos_saas add column if not exists forma_pagamento varchar(60) null;
alter table plantaopro.pagamentos_saas add column if not exists observacoes text null;
alter table plantaopro.pagamentos_saas add column if not exists criado_em timestamp null;

-- Provider de cobranca: registro administrativo do meio de pagamento.
-- Nenhum dado de cartao ou bancario sensivel e armazenado aqui.
create table if not exists plantaopro.cobranca_providers (
    codigo varchar(40) primary key,
    nome varchar(120) not null,
    modo varchar(20) not null default 'SANDBOX' constraint ck_cobranca_providers_modo check (modo in ('SANDBOX','PRODUCAO')),
    status varchar(20) not null default 'INATIVO' constraint ck_cobranca_providers_status check (status in ('ATIVO','INATIVO')),
    descricao text null,
    criado_em timestamp not null default now(),
    atualizado_em timestamp not null default now(),
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

-- Cobranca = tentativa de pagamento de uma fatura em um provider.
-- Referencia e opaca e unica; no maximo uma cobranca ativa por fatura.
create table if not exists plantaopro.cobranca_cobrancas (
    id uuid primary key default gen_random_uuid(),
    fatura_id uuid not null,
    cliente_id uuid not null,
    provider_codigo varchar(40) not null,
    referencia varchar(80) not null,
    checkout_url text null,
    valor numeric(14,2) not null constraint ck_cobranca_cobrancas_valor check (valor > 0),
    status varchar(20) not null default 'PENDENTE' constraint ck_cobranca_cobrancas_status check (status in ('PENDENTE','INICIADA','PAGA','FALHA','CANCELADA','ESTORNADA')),
    expira_em timestamp not null default now() + interval '7 days',
    evento_final_id varchar(120) null,
    criado_em timestamp not null default now(),
    atualizado_em timestamp not null default now(),
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create unique index if not exists ux_cobranca_cobrancas_referencia on plantaopro.cobranca_cobrancas(referencia);
create unique index if not exists ux_cobranca_cobrancas_fatura_ativa on plantaopro.cobranca_cobrancas(fatura_id) where status in ('PENDENTE','INICIADA') and reg_status='A';
create index if not exists ix_cobranca_cobrancas_fatura on plantaopro.cobranca_cobrancas(fatura_id);
create index if not exists ix_cobranca_cobrancas_cliente on plantaopro.cobranca_cobrancas(cliente_id);

-- Eventos de webhook recebidos. O indice unico (provider, evento) e o arbitro do dedupe:
-- violacao significa evento repetido e a transacao inteira de aplicacao e descartada.
create table if not exists plantaopro.cobranca_webhook_eventos (
    id uuid primary key default gen_random_uuid(),
    provider_codigo varchar(40) not null,
    evento_id varchar(120) not null,
    tipo_evento varchar(40) not null,
    referencia varchar(80) null,
    cobranca_id uuid null,
    fatura_id uuid null,
    resultado varchar(20) not null constraint ck_cobranca_webhook_resultado check (resultado in ('APLICADO','IGNORADO','RECUSADO')),
    mensagem text null,
    payload jsonb null,
    processado_em timestamp not null default now(),
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create unique index if not exists ux_cobranca_webhook_evento on plantaopro.cobranca_webhook_eventos(provider_codigo, evento_id);
create index if not exists ix_cobranca_webhook_fatura on plantaopro.cobranca_webhook_eventos(fatura_id);

-- Provider sandbox deterministico (nenhum provedor externo existe ainda no produto).
insert into plantaopro.cobranca_providers(codigo,nome,modo,status,descricao)
values('SANDBOX','Cobranca sandbox PLANTAOPro','SANDBOX','ATIVO','Checkout simulado sem dados de cartao; webhooks assinados com HMAC local.')
on conflict (codigo) do update set nome=excluded.nome, modo=excluded.modo, descricao=excluded.descricao, atualizado_em=now();

-- ============================================================
-- Seção 104 — C7 Onboarding Contrato v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2333_c7_onboarding_contrato.sql
-- SOURCE-SHA256: 8057680ca36bcba306753c6f89c58a4ab1d27761e0fa3dd9bb7fb146a4c2f5d7
-- R5-C7: onboarding adaptado ao contrato (catalogo canonicos de etapas com jornada e criterio verificavel).
-- Contexto: o kernel B5 materializava 11 etapas genericas fixas (codigo ETAPA_00..11), iguais para
-- qualquer plano, sem objetivo/responsavel/pre-requisito e concluidas por clique (sucesso ficticio).
-- Esta migration e aditiva e idempotente: o catalogo vira a unica fonte das etapas; cada passo da
-- jornada do cliente passa a ter objetivo, responsavel, pre-requisito, destino e um CRITERIO que o
-- servidor verifica em dados persistidos (avaliador no OnboardingJornadaService). Etapas de modulo
-- so existem para o tenant quando o contrato do modulo esta efetivo (predicado de vigencia B4/B6).
create table if not exists plantaopro.onboarding_etapas_catalogo (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(60) not null constraint ux_onboarding_etapas_catalogo_codigo unique,
    titulo varchar(160) not null,
    objetivo text not null,
    descricao text null,
    responsavel varchar(20) not null default 'CLIENTE' constraint ck_onboarding_etapas_catalogo_resp check (responsavel in ('CLIENTE','MNSOFT')),
    pre_requisito_codigo varchar(60) null,
    modulo_codigo varchar(40) null,
    criterio_codigo varchar(60) null,
    link_acao text not null,
    ordem integer not null,
    obrigatorio boolean not null default true,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);
comment on column plantaopro.onboarding_etapas_catalogo.modulo_codigo is 'nulo = nucleo geral do onboarding; senao codigo operacional em modulos_sistema (PLANTOES/SAUDE360/ADM360). A etapa so e materializada com contrato efetivo do modulo.';
comment on column plantaopro.onboarding_etapas_catalogo.criterio_codigo is 'nulo = conclusao humana explicita (origem MANUAL); senao o servidor deriva a conclusao verificando dados persistidos do tenant.';

-- Colunas que materializam a jornada no checklist do tenant (preserva linhas legadas ETAPA_xx).
alter table plantaopro.tenant_onboarding_checklist add column if not exists objetivo text null;
alter table plantaopro.tenant_onboarding_checklist add column if not exists responsavel varchar(20) null;
alter table plantaopro.tenant_onboarding_checklist add column if not exists pre_requisito_codigo varchar(60) null;
alter table plantaopro.tenant_onboarding_checklist add column if not exists modulo_codigo varchar(40) null;
alter table plantaopro.tenant_onboarding_checklist add column if not exists criterio_codigo varchar(60) null;
alter table plantaopro.tenant_onboarding_checklist add column if not exists origem_conclusao varchar(20) null;
alter table plantaopro.tenant_onboarding_checklist add column if not exists evidencia text null;
alter table plantaopro.tenant_onboarding_checklist add column if not exists avaliada_em timestamptz null;
alter table plantaopro.tenant_onboarding_checklist add column if not exists pulada boolean not null default false;
alter table plantaopro.tenant_onboarding_checklist add column if not exists motivo_pular text null;

-- Arbitro anti-duplicacao de etapa por tenant (materializacao idempotente por codigo).
-- Deducacao previa defensiva: linhas antigas simultaneas nao bloqueiam o indice.
delete from plantaopro.tenant_onboarding_checklist a
 using plantaopro.tenant_onboarding_checklist b
 where a.tenant_id = b.tenant_id and a.codigo = b.codigo and a.reg_status = 'A' and b.reg_status = 'A'
   and a.ctid < b.ctid;
create unique index if not exists ux_tenant_onboarding_checklist_tenant_codigo
 on plantaopro.tenant_onboarding_checklist(tenant_id, codigo) where reg_status = 'A';

-- Catalogo canonicos (fonte unica das etapas; rerun atualiza a jornada sem tocar nos tenants).
insert into plantaopro.onboarding_etapas_catalogo
(codigo,titulo,objetivo,descricao,responsavel,pre_requisito_codigo,modulo_codigo,criterio_codigo,link_acao,ordem,obrigatorio)
values
('ONB_EMPRESA_DADOS','Completar os dados da empresa','Manter o cadastro do cliente integro para contato, faturamento e suporte.','Verificamos razao social, nome fantasia, CNPJ, e-mail, telefone, cidade e estado no cadastro persistido.','CLIENTE',null,null,'EMPRESA_DADOS_COMPLETOS','/ClientePortal/Index',10,true),
('ONB_EQUIPE_CONVIDAR','Convidar o primeiro integrante da equipe','Distribuir acesso nominal desde o inicio; nenhuma conta compartilhada.','O criterio le a tabela de convites de equipe (token com expiracao e uso unico, B5).','CLIENTE',null,null,'CONVITE_EQUIPE_ENVIADO','/ConvitesEquipe/Index',20,true),
('ONB_EQUIPE_ACESSO','Primeiro convite aceito pela equipe','Provar que uma pessoa real entrou no ambiente com login proprio.','O criterio le o aceite do convite (uso do token registrado no banco).','CLIENTE','ONB_EQUIPE_CONVIDAR',null,'MEMBRO_EQUIPE_ATIVO','/ConvitesEquipe/Index',30,true),
('ONB_IDENTIDADE','Personalizar a identidade visual','Entregar o produto com a marca do cliente (white label).','Etapa opcional: pode ser pulada sem travar o onboarding. O criterio le logo/nome customizados na configuracao salva.','CLIENTE',null,null,'WHITE_LABEL_PERSONALIZADO','/WhiteLabel',40,false),
('ONB_PL_CRIAR','Criar o primeiro plantao','Registrar uma vaga real na grade de plantoes.','Exige contrato efetivo do modulo Plantoes. O criterio le a tabela de plantoes do tenant.','CLIENTE',null,'PLANTOES','PRIMEIRO_PLANTAO_CRIADO','/Plantoes/Create',110,true),
('ONB_PL_PUBLICAR','Publicar o plantao na grade','Abrir a vaga publicada para que a operacao comecou de verdade.','Pre-requisito: plantao criado. O criterio le plantoes em status publicado (aberto/confirmado/realizado).','CLIENTE','ONB_PL_CRIAR','PLANTOES','PRIMEIRO_PLANTAO_PUBLICADO','/Plantoes/Index',120,true),
('ONB_PL_ESCALA','Fechar a primeira escala com medico','Completar o ciclo convite -> resposta -> escala confirmada.','Pre-requisito: plantao publicado. O criterio le escalas confirmadas/realizadas vinculadas aos plantoes do tenant.','CLIENTE','ONB_PL_PUBLICAR','PLANTOES','ESCALA_PREENCHIDA','/CentralEscala/Index',130,true),
('ONB_SD_UNIDADE','Cadastrar a unidade de atendimento','Estruturar o local fisico onde o Saude 360 vai operar.','Exige contrato efetivo do modulo Saude 360. O criterio le unidades de atendimento cadastradas.','CLIENTE',null,'SAUDE360','PRIMEIRA_UNIDADE_SAUDE','/ClinicaDashboard/Index',210,true),
('ONB_SD_PACIENTE','Cadastrar o primeiro paciente','Formar a base cadastral do atendimento em saude.','Pre-requisito: unidade cadastrada. O criterio le pacientes do tenant.','CLIENTE','ONB_SD_UNIDADE','SAUDE360','PRIMEIRO_PACIENTE_CADASTRADO','/Pacientes/Create',220,true),
('ONB_SD_TRIAGEM','Realizar a primeira triagem','Exercitar o fluxo assistido completo na operacao real.','Pre-requisito: paciente cadastrado. O criterio le triagens registradas do tenant.','CLIENTE','ONB_SD_PACIENTE','SAUDE360','PRIMEIRA_TRIAGEM_REALIZADA','/Triagem/Create',230,true),
('ONB_AD_OPERACAO','Registrar a primeira operacao administrativa','Movimentar o Administrativo 360 com um lancamento real.','Exige contrato efetivo do modulo Administrativo 360. O criterio le operacoes do tenant.','CLIENTE',null,'ADM360','PRIMEIRA_OPERACAO_ADM360','/Administrativo360/Index',310,true),
('ONB_REVISAO','Revisao final e liberacao','Conferir as pendencias restantes e declarar a operacao pronta.','Conclusao derivada: o servidor so marca esta etapa quando TODAS as obrigatorias contratadas estao atendidas em dados.','CLIENTE',null,null,'REVISAO_GERAL','/Onboarding/Index',900,true)
on conflict (codigo) do update set
    titulo = excluded.titulo,
    objetivo = excluded.objetivo,
    descricao = excluded.descricao,
    responsavel = excluded.responsavel,
    pre_requisito_codigo = excluded.pre_requisito_codigo,
    modulo_codigo = excluded.modulo_codigo,
    criterio_codigo = excluded.criterio_codigo,
    link_acao = excluded.link_acao,
    ordem = excluded.ordem,
    obrigatorio = excluded.obrigatorio,
    reg_update = now();

-- ============================================================
-- Seção 105 — D9 Perfil Admin Modulos Jornada v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2334_d9_perfil_admin_modulos_jornada.sql
-- SOURCE-SHA256: 69f87da1d81ffb94de63f153b2952c21c958fc22f65acc2f103b3e0ae112c87f
-- R5-D9: completude de permissoes canonicas do perfil ADMINISTRADOR_CLIENTE para modulos de jornada.
-- Contexto: a homologacao D9 mostrou que habilitar um modulo no contrato (B5 habilitar-tenant) ativa o
-- claim de modulo, mas o PERFIL do administrador do cliente so tinha grants parciais semeados:
-- PLANTOES com 5 de 31 acoes (sem PLANTOES.VER -> pagina /Plantoes caia em PERMISSAO_NEGADA apesar de
-- contrato ATIVO) e SAUDE360 sem nenhuma acao. O precedente do catalogo e o suite ADM360, que foi
-- semeado completo (39 acoes) para este mesmo perfil. Esta migration segue o mesmo contrato:
-- quem contrata um modulo administra o modulo contratado; o gate de contrato (tenant_modulos +
-- vigencia B4/B6) continua obrigatorio - aqui só se completa o catalogo de capacidades do perfil,
-- que o proprio cliente pode revogar por override depois (usuario_permissoes_especiais).
-- Aditiva e idempotente: so insere o que ainda nao existe; nunca remove ou reduz grants.
insert into plantaopro.perfil_permissoes (
    id, perfil_id, permissao_id, permitido, bloqueado_por_plano, reg_status, reg_date, status, metadata
)
select
    gen_random_uuid(), pf.id, p.id, true, false, 'A', now(), 'ATIVO', '{}'::jsonb
from plantaopro.perfis pf
join plantaopro.permissoes p on p.reg_status = 'A'
join plantaopro.modulos_sistema ms on ms.id = p.modulo_id and upper(ms.codigo) in ('PLANTOES','SAUDE360')
where upper(pf.codigo) = 'ADMINISTRADOR_CLIENTE'
  and pf.tenant_id is null
  and pf.reg_status = 'A'
  and not exists (
      select 1 from plantaopro.perfil_permissoes pp
      where pp.perfil_id = pf.id and pp.permissao_id = p.id
  );

-- ============================================================
-- Seção 106 — D11 Perfil Admin Modulos Finos Saude v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2335_d11_perfil_admin_modulos_finos_saude.sql
-- SOURCE-SHA256: 2a551c70881105e5f7527b33c2f61e2126e8d722eeb19c307af47b89de1fcc0d
-- R5-D11: completude do catalogo de permissoes canonicas do ADMINISTRADOR_CLIENTE para os modulos
-- finos de jornada Saude 360. Contexto: a homologacao D11 mostrou que o contrato SAUDE360 habilitado
-- (B5) e os grants do suite generico SAUDE360.* (v2334) ainda deixam o gestor sem os codigos finos
-- que o guard Web cobra por controller (SaasRouteGuardFilter: Pacientes->PACIENTES, Consultas->
-- CONSULTAS, Triagem->TRIAGEM, Agendamentos->AGENDAMENTOS, ClinicaDashboard->CLINICA_DASHBOARD).
-- Sem PACIENTES.* no perfil, /Pacientes caia em PERMISSAO_NEGADA mesmo com modulo PACIENTES ATIVO no
-- contrato - o mesmo defeito que v2334 corrigiu para PLANTOES/SAUDE360, agora na camada fina.
-- Mesmo contrato de v2334: quem contrata um modulo administra o modulo contratado; o gate de
-- vigencia (tenant_modulos + B4/B6) continua obrigatorio e codigos inexistentes em modulos_sistema
-- (ex.: TRIAGEM, ate decisao de catalogo E13) viram no-op natural do join. Desta vez cobre tambem os
-- perfis base por tenant criados pelo kernel B5 (ba7faac0-... nao recebia nada dos suites).
-- Aditiva e idempotente: so insere o que falta; nunca remove ou reduz grants existentes.
insert into plantaopro.perfil_permissoes (
    id, perfil_id, permissao_id, permitido, bloqueado_por_plano, reg_status, reg_date, status, metadata
)
select
    gen_random_uuid(), pf.id, p.id, true, false, 'A', now(), 'ATIVO', '{}'::jsonb
from plantaopro.perfis pf
join plantaopro.permissoes p on p.reg_status = 'A'
join plantaopro.modulos_sistema ms on ms.id = p.modulo_id
    and upper(ms.codigo) in ('PACIENTES','CONSULTAS','AGENDAMENTOS','TRIAGEM','UNIDADES','CLINICA_DASHBOARD')
where upper(pf.codigo) = 'ADMINISTRADOR_CLIENTE'
  and pf.reg_status = 'A'
  and not exists (
      select 1 from plantaopro.perfil_permissoes pp
      where pp.perfil_id = pf.id and pp.permissao_id = p.id
  );

-- ============================================================
-- Seção 107 — E13 P2 Backfill Tenant Id Nucleo Clinico v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2336_e13_p2_backfill_tenant_id_nucleo_clinico.sql
-- SOURCE-SHA256: 72651255f8ebe56ae8b2f5037d313055ea7ae38c6c09c7b2d24509cfb8af621f
-- R5-E13/P2: padronizacao do escopo canonico do nucleo clinico em tenant_id.
-- Contexto (divida medida na evidencia D11): o kernel Saude360 (Saude360ClinicalService) gravava
-- somente cliente_id e deixava tenant_id NULL em 100% das linhas clinicas criadas por rota; dois
-- writers pontuais (favoritar CID e usar modelo de prescricao) ainda escreviam a chave de escopo
-- (cliente_id) dentro da coluna tenant_id, contaminando a semantica. A rota de escrita agora
-- resolve o tenant de origem pelo mapeamento persistido (clientes.tenant_id, com fallback
-- tenants.cliente_id ou o proprio tenant quando o escopo ja e um tenant) e esta migracao retroa a
-- MESMA regra as linhas existentes.
-- Regra de backfill: tenant_id := clientes.tenant_id do cliente do registro, somente onde o valor
-- atual e NULL ou esta contaminado (= cliente_id do proprio registro). Valores distintos
-- intencionais nao sao tocados. Tabelas sem a coluna tenant_id sao ignoradas pelo guard.
-- Aditiva e idempotente: reexecucao nao altera nada (a condicao so casa com NULL/contaminado).
do $$
declare
    t text;
begin
    foreach t in array array[
        'pacientes','paciente_historico','painel_chamada_fila','painel_chamada_historico',
        'agendamentos','triagens','consultas','consulta_historico','cid_tabela','cid_favoritos',
        'cid_uso_historico','prescricoes','prescricao_modelos','prescricao_historico',
        'clinica_contas_receber','clinica_recebimentos','clinica_caixa','convenios',
        'convenio_planos','convenio_autorizacoes','planos_saude','plano_saude_pacientes',
        'convenio_glosas','repasses_medicos_clinicos','clinica_unidades_atendimento'
    ] loop
        if exists (
            select 1 from information_schema.columns
            where table_schema = 'plantaopro' and table_name = t and column_name = 'tenant_id'
        ) then
            execute format(
                'update plantaopro.%I x set tenant_id = m.tenant_id
                 from plantaopro.clientes m
                 where m.id = x.cliente_id and m.tenant_id is not null
                   and (x.tenant_id is null or x.tenant_id = x.cliente_id)', t);
        end if;
    end loop;
end $$;

insert into plantaopro.schema_migrations(id,script_path,checksum,applied_at)
select 'v2336','database/migrations/2026_10_v2336_e13_p2_backfill_tenant_id_nucleo_clinico.sql','runtime-managed',now()
where not exists(select 1 from plantaopro.schema_migrations where id='v2336');

-- ============================================================
-- Seção 108 — P0 Convergencia Tenant Modulos Reg Status v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2337_p0_convergencia_tenant_modulos_reg_status.sql
-- SOURCE-SHA256: 464e02c21b47f3e8fc129c0ad6db3e76b3deef4326beab6023f96a6a74c1bc2a
-- R5-P0: Convergencia de shape do tenant_modulos para linhagens legadas de instalacao.
-- Linhagens criadas antes do catalogo SaaS de modulos possuem plantaopro.tenant_modulos com o
-- shape antigo (codigo/dados/criado_em) sem as colunas canonicas; como as migrations subsequentes
-- usam create table if not exists, o indice unico parcial de
-- 2026_plantao_pro_self_service_white_label (tenant_id, lower(codigo_modulo) WHERE reg_status='A')
-- falha com 42703 ao referenciar colunas ausentes em bancos legados (medido no ensaio P0 sobre
-- clone do banco principal). Esta guarda replica exatamente o bloco de colunas da reconciliacao
-- canonica 2026_09_v2197 (mesmas colunas, mesmas nulabilidades), permitindo que a ordem canonica
-- seja aplicada por PlantaoPro.Tools.Database upgrade em qualquer linhagem sem reordenar o
-- manifesto nem remendo ad-hoc fora das migrations; a propria v2197 continua sendo a reconciliacao
-- de dados e constraints ao chegar sua vez no manifesto.
-- Aditivo e idempotente: add column if not exists e no-op onde a coluna ja existe.
alter table if exists plantaopro.tenant_modulos add column if not exists modulo_id uuid null;
alter table if exists plantaopro.tenant_modulos add column if not exists codigo text null;
alter table if exists plantaopro.tenant_modulos add column if not exists codigo_modulo text null;
alter table if exists plantaopro.tenant_modulos add column if not exists habilitado boolean null;
alter table if exists plantaopro.tenant_modulos add column if not exists origem text null;
alter table if exists plantaopro.tenant_modulos add column if not exists status text null;
alter table if exists plantaopro.tenant_modulos add column if not exists reg_status char(1) null;
alter table if exists plantaopro.tenant_modulos add column if not exists reg_date timestamptz null;
alter table if exists plantaopro.tenant_modulos add column if not exists reg_update timestamptz null;
alter table if exists plantaopro.tenant_modulos add column if not exists ativado_em timestamptz null;
alter table if exists plantaopro.tenant_modulos add column if not exists desativado_em timestamptz null;
alter table if exists plantaopro.tenant_modulos add column if not exists limite_contratado integer null;
alter table if exists plantaopro.tenant_modulos add column if not exists preco_contratado numeric(14,2) null;
alter table if exists plantaopro.tenant_modulos add column if not exists created_by uuid null;
alter table if exists plantaopro.tenant_modulos add column if not exists updated_by uuid null;

-- ============================================================
-- Seção 109 — P0 Administrativo360 Garantia Codigos Fiscais v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2338_p0_adm360_garantia_codigos_fiscais.sql
-- SOURCE-SHA256: 1ef1dda3e29ba1e9bca510ac239de65055cd8389d10b61134a9e1ca5fef24427
-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2338_p0_adm360_garantia_codigos_fiscais
-- Guarda de compatibilidade P0 (ensaio rodada 5): a sequencia canonica de
-- migrations cria em permissoes apenas o conjunto historico de codigos ADM360
-- (2026_09_v2198); os 7 codigos fiscaus novos (CRIAR, EDITAR, CONFIGURAR,
-- REABRIR, CONFIRMAR, CANCELAR, CONFERIR) eram criados somente pelo catalogo
-- de permissoes do runtime da API. Isso fazia a sanidade da
-- 2026_10_v2322_adm360_grants_adm_cliente_r5_a2 falhar em bancos novos ou
-- legados onde a API nova ainda nao subiu antes do upgrade de banco.
-- Esta guarda cadastra os codigos faltantes com o MESMO padrao da v2198
-- (acao EDITAR para acoes de escrita, modulo ADM360 quando existir) e e
-- integralmente idempotente: nao duplica onde o runtime ja criou e nao
-- altera codigos existentes.
-- ============================================================================

DO $migration$
DECLARE
    v_acao_editar_id uuid;
    v_modulo_adm_id uuid;
    v_perm_code text;
BEGIN
    select id into v_acao_editar_id
      from plantaopro.acoes_sistema
     where codigo in ('EDITAR', 'SALVAR') order by id limit 1;
    if v_acao_editar_id is null then
        v_acao_editar_id := gen_random_uuid();
        insert into plantaopro.acoes_sistema(id, codigo, nome, status, reg_status, reg_date)
        values (v_acao_editar_id, 'EDITAR', 'Editar', 'ATIVO', 'A', now());
    end if;

    select id into v_modulo_adm_id
      from plantaopro.modulos_sistema
     where upper(btrim(codigo)) = 'ADM360' and reg_status = 'A' limit 1;

    foreach v_perm_code in array array[
        'ADM360:CRIAR',
        'ADM360:EDITAR',
        'ADM360:CONFIGURAR',
        'ADM360:REABRIR',
        'ADM360:CONFIRMAR',
        'ADM360:CANCELAR',
        'ADM360:CONFERIR'
    ] loop
        if not exists (
            select 1 from plantaopro.permissoes
             where upper(btrim(codigo)) = upper(btrim(v_perm_code))
               and reg_status = 'A'
        ) then
            insert into plantaopro.permissoes (id, acao_id, modulo_id, codigo, nome, status, reg_status, reg_date)
            values (gen_random_uuid(), v_acao_editar_id, v_modulo_adm_id, v_perm_code,
                    replace(v_perm_code, ':', ' - '), 'ATIVO', 'A', now());
        end if;
    end loop;
END
$migration$;

-- ============================================================
-- Seção 110 — R6A Granularidade Saude360 Pacote Capacidades v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2339_r6a_granularidade_saude360_pacote_capacidades.sql
-- SOURCE-SHA256: f29fa8e6f083d1035352827a9184ef2a1771a2029759d224eef13bd9a306e038
-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2339_r6a_granularidade_saude360_pacote_capacidades
-- R6-BlocoA item 2: granularidade real do Saude 360. O catalogo canônico só
-- registrava PACIENTES e CONSULTAS como modulos finos; os demais codigos que o
-- guard Web cobra por controller (AGENDAMENTOS, TRIAGEM, UNIDADES,
-- CLINICA_DASHBOARD, PAINEL_CHAMADA, CID, PRESCRICOES, CLINICA_FINANCEIRO,
-- CONVENIOS, PLANOS_SAUDE, PENDENCIAS_CLINICAS) nao existiam em
-- modulos_sistema, entao um contrato SAUDE360 valido ainda derrubava paginas
-- clinicas legitimas em MODULO_NAO_CONTRATADO (defeito medido no inventario
-- R6 secao D.2/D.3).
--
-- Modelo decidido (sem preco, sem cobranca extra): SAUDE360 permanece o
-- pacote comercial de topo; as capacidades viram linhas proprias no catalogo
-- com disponivel_comercialmente=false e pacote_codigo='SAUDE360' (relacao
-- pacote->capacidade no proprio catalogo canunico). Quem contrata o pacote
-- passa a ter as capacidades efetivas automaticamente - nenhum insert novo em
-- tenant_modulos, nenhuma machine de estado paralela. A vigencia continua
-- avaliada 1:1 pelo predicado canonical de B6 (ModuleContractVigencia), agora
-- materializado na funcao plantaopro.modulos_efetivos()/modulo_efetivo(), que
-- todo o runtime passa a consultar (claims de login, politica por request,
-- gate clinico da API, autosservico).
--
-- Permissoes por acao preservadas: cada capacidade recebe a MESMA suíte de
-- 31 permissoes X.ACAO do modulo PACIENTES (v2322/v2335), e os grants
-- operacionais translating os conjuntos hardcoded do guard legado
-- (AccessServices pre-v2149) para dados de catalogo: administradores recebem a
-- suíte completa das capacidades (mesmo contrato de v2334/v2335), perfis
-- operacional recebem apenas as acoes que o guard sabe cobrar (VER/LISTAR/
-- VISUALIZAR/CRIAR/EDITAR/EXCLUIR/EXPORTAR/CONFIRMAR) e auditoria recebe
-- somente leitura. Aditiva e idempotente: nao remove nem reduz nada.
-- ============================================================================

DO $migration$
DECLARE
    v_acoes uuid[];
BEGIN
    -- 1) Relacao pacote->capacidade no catalogo canônico -----------------------
    alter table plantaopro.modulos_sistema add column if not exists pacote_codigo text;
    comment on column plantaopro.modulos_sistema.pacote_codigo is
      'R6: codigo do modulo-pacote que inclui esta capacidade (ex.: SAUDE360). Null = modulo de topo, contratado e cobrado individualmente.';
    create index if not exists ix_modulos_sistema_pacote_codigo
        on plantaopro.modulos_sistema (lower(pacote_codigo)) where reg_status = 'A';

    if not exists (select 1 from pg_constraint where conname = 'ck_modulos_sistema_pacote_distinto') then
        alter table plantaopro.modulos_sistema
            add constraint ck_modulos_sistema_pacote_distinto
            check (pacote_codigo is null or lower(pacote_codigo) <> lower(codigo));
    end if;

    if not exists (select 1 from plantaopro.modulos_sistema
                   where lower(codigo) = 'saude360' and reg_status = 'A') then
        raise exception 'v2339: modulo-pacote SAUDE360 ausente do catalogo canônico';
    end if;

    -- 2) Capacidades-filhas faltantes (somente as que o guard/regras ja cobram)
    insert into plantaopro.modulos_sistema
        (id, codigo, nome, descricao, ordem, status, reg_status, reg_date,
         categoria, essencial, disponivel_comercialmente, funcionalidades, metadata)
    select md5('module:' || f.codigo)::uuid, f.codigo, f.nome, f.descricao,
           111 + row_number() over () * 10, 'ATIVO', 'A', now(),
           'SAUDE360', false, false, '[]'::jsonb, '{}'::jsonb
    from (values
        ('AGENDAMENTOS',      'Agendamentos',       'Capacidade inclusa no pacote SAUDE360: agenda, check-in e chegada de pacientes.'),
        ('TRIAGEM',           'Triagem',            'Capacidade inclusa no pacote SAUDE360: priorizacao e encaminhamento de pacientes.'),
        ('UNIDADES',          'Unidades de Atendimento', 'Capacidade inclusa no pacote SAUDE360: unidades clinicas do tenant (R5-E13).'),
        ('CLINICA_DASHBOARD', 'Dashboard Clinico',  'Capacidade inclusa no pacote SAUDE360: visao operacional da clinica.'),
        ('PAINEL_CHAMADA',    'Painel de Chamada',  'Capacidade inclusa no pacote SAUDE360: chamada de pacientes sem expor dados sensiveis.'),
        ('CID',               'CID',                'Capacidade inclusa no pacote SAUDE360: catalogo e uso de cid na consulta.'),
        ('PRESCRICOES',       'Prescricoes',        'Capacidade inclusa no pacote SAUDE360: prescricoes e receitarios.'),
        ('CLINICA_FINANCEIRO','Financeiro Clinico', 'Capacidade inclusa no pacote SAUDE360: contas a receber e faturamento da clinica.'),
        ('CONVENIOS',         'Convenios',          'Capacidade inclusa no pacote SAUDE360: credenciamento e guias de convenios.'),
        ('PLANOS_SAUDE',      'Planos de Saude',    'Capacidade inclusa no pacote SAUDE360: planos vinculados aos convenios.'),
        ('PENDENCIAS_CLINICAS','Pendencias Clinicas','Capacidade inclusa no pacote SAUDE360: pendencias assistenciais por paciente.')
    ) as f(codigo, nome, descricao)
    on conflict (lower(codigo)) where reg_status = 'A' do nothing;

    -- 3) Mapeamento de pacote em todas as capacidades da familia ---------------
    update plantaopro.modulos_sistema
       set pacote_codigo = 'SAUDE360', reg_update = now()
     where reg_status = 'A'
       and lower(codigo) in ('pacientes', 'consultas', 'agendamentos', 'triagem', 'unidades',
                             'clinica_dashboard', 'painel_chamada', 'cid', 'prescricoes',
                             'clinica_financeiro', 'convenios', 'planos_saude', 'pendencias_clinicas')
       and pacote_codigo is distinct from 'SAUDE360';

    -- 4) Suíte de permissoes por capacidade ====================================
    -- Espelha exatamente o conjunto de acoes das permissoes existentes do modulo
    -- PACIENTES (mesma suíte generica canônica); em bancos onde ela ainda não
    -- existe cai no conjunto completo de acoes_sistema ativas.
    select array_agg(distinct px.acao_id) into v_acoes
    from plantaopro.permissoes px
    join plantaopro.modulos_sistema mp on mp.id = px.modulo_id and lower(mp.codigo) = 'pacientes'
    where px.reg_status = 'A' and px.acao_id is not null;

    if v_acoes is null or cardinality(v_acoes) = 0 then
        select array_agg(id) into v_acoes from plantaopro.acoes_sistema where reg_status = 'A';
    end if;

    insert into plantaopro.permissoes (id, codigo, nome, status, reg_status, reg_date, modulo_id, acao_id)
    select gen_random_uuid(), upper(m.codigo || '.' || ac.codigo), m.nome || ' - ' || lower(ac.codigo),
           'ATIVO', 'A', now(), m.id, ac.id
    from plantaopro.modulos_sistema m
    join plantaopro.acoes_sistema ac on ac.id = any (v_acoes) and ac.reg_status = 'A'
    where m.reg_status = 'A'
      and lower(m.codigo) in ('pacientes', 'consultas', 'agendamentos', 'triagem', 'unidades',
                              'clinica_dashboard', 'painel_chamada', 'cid', 'prescricoes',
                              'clinica_financeiro', 'convenios', 'planos_saude', 'pendencias_clinicas')
    on conflict (lower(codigo)) where reg_status = 'A' do nothing;

    -- 5) Grants - administrados/operacao/auditoria =============================
    -- 5a) Administradores do tenant e da clinica administram cada capacidade
    --     contratada via pacote (mesmo contrato de v2334/v2335).
    insert into plantaopro.perfil_permissoes (
        id, perfil_id, permissao_id, permitido, bloqueado_por_plano, reg_status, reg_date, status, metadata
    )
    select gen_random_uuid(), pf.id, p.id, true, false, 'A', now(), 'ATIVO', '{}'::jsonb
    from plantaopro.perfis pf
    join plantaopro.permissoes p on p.reg_status = 'A'
    join plantaopro.modulos_sistema ms on ms.id = p.modulo_id
        and upper(ms.codigo) in ('PACIENTES', 'CONSULTAS', 'AGENDAMENTOS', 'TRIAGEM', 'UNIDADES',
                                 'CLINICA_DASHBOARD', 'PAINEL_CHAMADA', 'CID', 'PRESCRICOES',
                                 'CLINICA_FINANCEIRO', 'CONVENIOS', 'PLANOS_SAUDE', 'PENDENCIAS_CLINICAS')
    where upper(pf.codigo) in ('ADMINISTRADOR_CLIENTE', 'ADMINISTRADOR_CLINICA')
      and pf.reg_status = 'A'
      and not exists (
          select 1 from plantaopro.perfil_permissoes pp
          where pp.perfil_id = pf.id and pp.permissao_id = p.id
      );

    -- 5b) Subconjuntos operacionais: traducao dos conjuntos hardcoded do guard
    --     legado (saude360Recepcao/Triagem/Medico/Financeiro/Convenios em
    --     AccessServices) para grants de catalogo. So usa acoes que o resolver
    --     generico do guard sabe exigir e o tipo de leitura definida por perfil.
    insert into plantaopro.perfil_permissoes (
        id, perfil_id, permissao_id, permitido, bloqueado_por_plano, reg_status, reg_date, status, metadata
    )
    select gen_random_uuid(), pf.id, p.id, true, false, 'A', now(), 'ATIVO', '{}'::jsonb
    from plantaopro.perfis pf
    join (values
        ('RECEPCAO',             'PACIENTES',          'op'),
        ('RECEPCAO',             'AGENDAMENTOS',       'op'),
        ('RECEPCAO',             'PAINEL_CHAMADA',     'op'),
        ('RECEPCAO',             'CLINICA_DASHBOARD',  'leitura'),
        ('TRIAGEM',              'TRIAGEM',            'op'),
        ('TRIAGEM',              'CLINICA_DASHBOARD',  'leitura'),
        ('ENFERMAGEM',           'TRIAGEM',            'op'),
        ('ENFERMAGEM',           'CLINICA_DASHBOARD',  'leitura'),
        ('COORDENADOR_CLINICO',  'TRIAGEM',            'op'),
        ('COORDENADOR_CLINICO',  'CLINICA_DASHBOARD',  'leitura'),
        ('MEDICO',               'CONSULTAS',          'op'),
        ('MEDICO',               'PRESCRICOES',        'op'),
        ('MEDICO',               'CID',                'op'),
        ('MEDICO',               'CLINICA_DASHBOARD',  'leitura'),
        ('MEDICO',               'AGENDAMENTOS',       'leitura'),
        ('MEDICO',               'TRIAGEM',            'leitura'),
        ('FINANCEIRO_CLINICA',   'CLINICA_FINANCEIRO', 'op'),
        ('FINANCEIRO',           'CLINICA_FINANCEIRO', 'relatorio'),
        ('FATURAMENTO_CONVENIO', 'CONVENIOS',          'op'),
        ('FATURAMENTO_CONVENIO', 'PLANOS_SAUDE',       'op')
    ) as r(perfil, modulo, tipo) on upper(pf.codigo) = r.perfil
    join plantaopro.permissoes p on p.reg_status = 'A'
    join plantaopro.modulos_sistema ms on ms.id = p.modulo_id and upper(ms.codigo) = r.modulo
    join plantaopro.acoes_sistema ac on ac.id = p.acao_id
        and ac.codigo = any (case r.tipo
                                when 'op'       then array['VER', 'LISTAR', 'VISUALIZAR', 'CRIAR', 'EDITAR', 'EXCLUIR', 'EXPORTAR', 'CONFIRMAR']
                                when 'relatorio' then array['VER', 'LISTAR', 'VISUALIZAR', 'EXPORTAR', 'CONFIRMAR']
                                else array['VER', 'LISTAR', 'VISUALIZAR']
                             end)
    where pf.reg_status = 'A'
      and not exists (
          select 1 from plantaopro.perfil_permissoes pp
          where pp.perfil_id = pf.id and pp.permissao_id = p.id
      );

    -- 5c) Auditor clinico: somente leitura em toda a familia (guard legado
    --     liberava qualquer SAUDE360_* para leitura).
    insert into plantaopro.perfil_permissoes (
        id, perfil_id, permissao_id, permitido, bloqueado_por_plano, reg_status, reg_date, status, metadata
    )
    select gen_random_uuid(), pf.id, p.id, true, false, 'A', now(), 'ATIVO', '{}'::jsonb
    from plantaopro.perfis pf
    join plantaopro.permissoes p on p.reg_status = 'A'
    join plantaopro.modulos_sistema ms on ms.id = p.modulo_id
        and upper(ms.codigo) in ('PACIENTES', 'CONSULTAS', 'AGENDAMENTOS', 'TRIAGEM', 'UNIDADES',
                                 'CLINICA_DASHBOARD', 'PAINEL_CHAMADA', 'CID', 'PRESCRICOES',
                                 'CLINICA_FINANCEIRO', 'CONVENIOS', 'PLANOS_SAUDE', 'PENDENCIAS_CLINICAS')
    join plantaopro.acoes_sistema ac on ac.id = p.acao_id
        and ac.codigo in ('VER', 'LISTAR', 'VISUALIZAR')
    where upper(pf.codigo) = 'AUDITOR_CLINICO'
      and pf.reg_status = 'A'
      and not exists (
          select 1 from plantaopro.perfil_permissoes pp
          where pp.perfil_id = pf.id and pp.permissao_id = p.id
      );

    -- 6) Kernel canonico de contratacao efetiva =================================
    -- MESMA semantica de ModuleContractVigencia.EffectivePredicate (B6) +
    -- heranca de capacidades pelo pacote (pacote_codigo). O texto do predicado e
    -- verificado por teste de sincronia contra o C# (R6GranularidadeSaude360Tests).
    create or replace function plantaopro.modulos_efetivos(p_tenant_id uuid)
    returns table (codigo text, modulo_id uuid, pacote_codigo text)
    language sql stable
    as $fn$
        select distinct upper(ms.codigo), ms.id, nullif(ms.pacote_codigo, '')
        from plantaopro.tenant_modulos tm
        join plantaopro.modulos_sistema ms on ms.id = tm.modulo_id and ms.reg_status = 'A' and upper(ms.status) = 'ATIVO'
        where tm.tenant_id = p_tenant_id
          and tm.reg_status='A' and tm.desativado_em is null
          and ((upper(coalesce(tm.status,'ATIVO'))='ATIVO' and tm.habilitado=true and (tm.ativado_em is null or tm.ativado_em<=now()))
            or (upper(coalesce(tm.status,''))='AGENDADO' and coalesce(tm.ativado_em,tm.reg_date)<=now()))
        union
        select distinct upper(ch.codigo), ch.id, nullif(ch.pacote_codigo, '')
        from plantaopro.tenant_modulos tm
        join plantaopro.modulos_sistema pkg on pkg.id = tm.modulo_id and pkg.reg_status = 'A' and upper(pkg.status) = 'ATIVO'
        join plantaopro.modulos_sistema ch on ch.reg_status = 'A' and upper(ch.status) = 'ATIVO'
            and lower(ch.pacote_codigo) = lower(pkg.codigo)
        where tm.tenant_id = p_tenant_id
          and tm.reg_status='A' and tm.desativado_em is null
          and ((upper(coalesce(tm.status,'ATIVO'))='ATIVO' and tm.habilitado=true and (tm.ativado_em is null or tm.ativado_em<=now()))
            or (upper(coalesce(tm.status,''))='AGENDADO' and coalesce(tm.ativado_em,tm.reg_date)<=now()))
        union
        select distinct upper(ms.codigo), ms.id, nullif(ms.pacote_codigo, '')
        from plantaopro.tenant_modulos tm
        join plantaopro.modulos_sistema ms on lower(ms.codigo) = lower(nullif(tm.codigo_modulo, '')) and ms.reg_status = 'A' and upper(ms.status) = 'ATIVO'
        where tm.tenant_id = p_tenant_id
          and tm.reg_status='A' and tm.desativado_em is null
          and ((upper(coalesce(tm.status,'ATIVO'))='ATIVO' and tm.habilitado=true and (tm.ativado_em is null or tm.ativado_em<=now()))
            or (upper(coalesce(tm.status,''))='AGENDADO' and coalesce(tm.ativado_em,tm.reg_date)<=now()))
        union
        select distinct upper(ch.codigo), ch.id, nullif(ch.pacote_codigo, '')
        from plantaopro.tenant_modulos tm
        join plantaopro.modulos_sistema pkg on lower(pkg.codigo) = lower(nullif(tm.codigo_modulo, '')) and pkg.reg_status = 'A' and upper(pkg.status) = 'ATIVO'
        join plantaopro.modulos_sistema ch on ch.reg_status = 'A' and upper(ch.status) = 'ATIVO'
            and lower(ch.pacote_codigo) = lower(pkg.codigo)
        where tm.tenant_id = p_tenant_id
          and tm.reg_status='A' and tm.desativado_em is null
          and ((upper(coalesce(tm.status,'ATIVO'))='ATIVO' and tm.habilitado=true and (tm.ativado_em is null or tm.ativado_em<=now()))
            or (upper(coalesce(tm.status,''))='AGENDADO' and coalesce(tm.ativado_em,tm.reg_date)<=now()));
    $fn$;

    create or replace function plantaopro.modulo_efetivo(p_tenant_id uuid, p_codigo text)
    returns boolean
    language sql stable
    as $fn$
        select exists(
            select 1 from plantaopro.modulos_efetivos(p_tenant_id) e
            where e.codigo = upper(btrim(p_codigo)));
    $fn$;

    comment on function plantaopro.modulos_efetivos(uuid) is
      'R6: unicos modulos efetivamente contratados do tenant (predicado B6 + capacidades herdadas do pacote). Fonte unica do kernel de acesso.';
    comment on function plantaopro.modulo_efetivo(uuid, text) is
      'R6: consulta pontual de contratacao efetiva (inclui heranca de pacote). Usado por politicas por request e gates de modulo.';
END
$migration$;

-- ============================================================
-- Seção 111 — R6A1 Acesso Canonico Conflito Pacote Capacidade v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2340_r6a1_acesso_canonico_conflito_pacote_capacidade.sql
-- SOURCE-SHA256: e8715962ccc1376740f6db90021a49c55725253c9ede0acc6d6c26cf3c5337b3
-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2340_r6a1_acesso_canonico_conflito_pacote_capacidade
-- R6-BlocoA item 1 (acesso canônico): trata EXPLICITAMENTE o conflito entre o
-- contrato de um pacote e a restrição de uma de suas capacidades-filhas.
--
-- O que mudou em relação à v2339:
--   Na v2339, um pacote contratado (ex.: SAUDE360) expandia TODAS as capacidades
--   cujo pacote_codigo = código do pacote, independentemente de o tenant ter uma
--   linha própria de contrato para aquela capacidade. Consequência: se um
--   ADMINISTRADOR desabilitava uma capacidade individualmente (tenant_modulos
--   com habilitado=false / status inativo para PACIENTES, por exemplo), a função
--   ainda devolvia PACIENTES como efetivo via herança do pacote -- a restrição
--   era ignorada e a página continuava acessível com contrato "contraditório".
--
-- Regra canônica agora (documentada no kernel):
--   Um módulo M é efetivo para o tenant T quando
--     (a) T possui linha direta efetiva para M (predicado B6), OU
--     (b) M é capacidade de um pacote P contratado por T (linha efetiva para P)
--         E T NÃO possui linha própria de contrato para M (nenhum override).
--   Ou seja: abrir uma linha própria de contrato para uma capacidade é uma
--   DECISÃO PER-CAPACIDADE que prevalece sobre a herança do pacote. Capacidade
--   sem linha própria herda o estado do pacote; capacidade com linha própria
--   segue o estado daquela linha. Aditivo e idempotente; reusa o predicado B6
--   idêntico (mesmo texto verificado pelo teste de sincronia contra o C#).
-- ============================================================================

DO $migration$
BEGIN
    create or replace function plantaopro.modulos_efetivos(p_tenant_id uuid)
    returns table (codigo text, modulo_id uuid, pacote_codigo text)
    language sql stable
    as $fn$
        -- (a) Contratação direta por modulo_id, avaliada 1:1 pelo predicado B6.
        select distinct upper(ms.codigo), ms.id, nullif(ms.pacote_codigo, '')
        from plantaopro.tenant_modulos tm
        join plantaopro.modulos_sistema ms on ms.id = tm.modulo_id and ms.reg_status = 'A' and upper(ms.status) = 'ATIVO'
        where tm.tenant_id = p_tenant_id
          and tm.reg_status='A' and tm.desativado_em is null
          and ((upper(coalesce(tm.status,'ATIVO'))='ATIVO' and tm.habilitado=true and (tm.ativado_em is null or tm.ativado_em<=now()))
            or (upper(coalesce(tm.status,''))='AGENDADO' and coalesce(tm.ativado_em,tm.reg_date)<=now()))
        union
        -- (b1) Herança de capacidades do pacote contratado por modulo_id,
        --      SEMPRE QUE o tenant NÃO tenha aberto linha própria para aquela
        --      capacidade (override per-capacidade prevalece sobre o pacote).
        select distinct upper(ch.codigo), ch.id, nullif(ch.pacote_codigo, '')
        from plantaopro.tenant_modulos tm
        join plantaopro.modulos_sistema pkg on pkg.id = tm.modulo_id and pkg.reg_status = 'A' and upper(pkg.status) = 'ATIVO'
        join plantaopro.modulos_sistema ch on ch.reg_status = 'A' and upper(ch.status) = 'ATIVO'
            and lower(ch.pacote_codigo) = lower(pkg.codigo)
        where tm.tenant_id = p_tenant_id
          and tm.reg_status='A' and tm.desativado_em is null
          and ((upper(coalesce(tm.status,'ATIVO'))='ATIVO' and tm.habilitado=true and (tm.ativado_em is null or tm.ativado_em<=now()))
            or (upper(coalesce(tm.status,''))='AGENDADO' and coalesce(tm.ativado_em,tm.reg_date)<=now()))
          and not exists (
              select 1 from plantaopro.tenant_modulos ovr
              where ovr.tenant_id = p_tenant_id and ovr.reg_status = 'A'
                and (ovr.modulo_id = ch.id
                     or (ovr.modulo_id is null and lower(nullif(ovr.codigo_modulo,'')) = lower(ch.codigo)))
          )
        union
        -- (a') Contratação direta por codigo_modulo (linhas sem modulo_id).
        select distinct upper(ms.codigo), ms.id, nullif(ms.pacote_codigo, '')
        from plantaopro.tenant_modulos tm
        join plantaopro.modulos_sistema ms on lower(ms.codigo) = lower(nullif(tm.codigo_modulo, '')) and ms.reg_status = 'A' and upper(ms.status) = 'ATIVO'
        where tm.tenant_id = p_tenant_id
          and tm.reg_status='A' and tm.desativado_em is null
          and ((upper(coalesce(tm.status,'ATIVO'))='ATIVO' and tm.habilitado=true and (tm.ativado_em is null or tm.ativado_em<=now()))
            or (upper(coalesce(tm.status,''))='AGENDADO' and coalesce(tm.ativado_em,tm.reg_date)<=now()))
        union
        -- (b2) Herança de capacidades por codigo_modulo do pacote contratado,
        --      com a mesma regra de override per-capacidade.
        select distinct upper(ch.codigo), ch.id, nullif(ch.pacote_codigo, '')
        from plantaopro.tenant_modulos tm
        join plantaopro.modulos_sistema pkg on lower(pkg.codigo) = lower(nullif(tm.codigo_modulo, '')) and pkg.reg_status = 'A' and upper(pkg.status) = 'ATIVO'
        join plantaopro.modulos_sistema ch on ch.reg_status = 'A' and upper(ch.status) = 'ATIVO'
            and lower(ch.pacote_codigo) = lower(pkg.codigo)
        where tm.tenant_id = p_tenant_id
          and tm.reg_status='A' and tm.desativado_em is null
          and ((upper(coalesce(tm.status,'ATIVO'))='ATIVO' and tm.habilitado=true and (tm.ativado_em is null or tm.ativado_em<=now()))
            or (upper(coalesce(tm.status,''))='AGENDADO' and coalesce(tm.ativado_em,tm.reg_date)<=now()))
          and not exists (
              select 1 from plantaopro.tenant_modulos ovr
              where ovr.tenant_id = p_tenant_id and ovr.reg_status = 'A'
                and (ovr.modulo_id = ch.id
                     or (ovr.modulo_id is null and lower(nullif(ovr.codigo_modulo,'')) = lower(ch.codigo)))
          );
    $fn$;

    comment on function plantaopro.modulos_efetivos(uuid) is
      'R6: modulos efetivamente contratados do tenant (predicado B6 + capacidades herdadas do pacote). Override per-capacidade (linha propria em tenant_modulos) prevalece sobre a heranca do pacote. Fonte unica do kernel de acesso.';
END
$migration$;

-- ============================================================
-- Seção 112 — R7A1 Conflitos Unificados Profissional v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2341_r7a1_conflitos_unificados_profissional.sql
-- SOURCE-SHA256: b2c44ea9be12e49029afbabf1bae25182e0b00ce5e1dee9306a957551d5c819f
-- PlantãoPro v2.34.1 (R7-A1): invariantes das verificações de conflito/elegibilidade unificadas do profissional.
-- O fluxo canônico de aceite/confirmacao de escala (EscalaService/MedicoElegibilidadeService) consulta
-- medicos.bloqueado e medico_especialidades; ambas as estruturas eram referenciadas pelo código mas
-- ausentes do banco (falha 500 latente em todo aceite de convite operacional). Esta migration materializa
-- a estrutura mínima exigida pela regra, sem alterar histórico.

alter table plantaopro.medicos
    add column if not exists bloqueado boolean not null default false;

create table if not exists plantaopro.medico_especialidades (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    medico_id uuid not null references plantaopro.medicos(id),
    especialidade_id uuid not null references plantaopro.especialidades(id),
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now()
);

-- Vínculo ativo único por médico/especialidade; o histórico é preservado via reg_status.
create unique index if not exists ux_medico_especialidade_ativa
    on plantaopro.medico_especialidades(medico_id, especialidade_id)
    where reg_status='A';

-- ============================================================
-- Seção 113 — R7A1 Historico Escala Colunas v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2342_r7a1_historico_escala_colunas.sql
-- SOURCE-SHA256: 2c5800b64701f8a27feaf6878720419de0582543c92f8445858b313f769a5f47
-- PlantãoPro v2.34.2 (R7-A1): colunas do histórico de escalas referenciadas pelo código canônico.
-- As transições de escala (EscalaService.AddHistoricoAsync) gravam escala_id/status_anterior/status_novo/
-- justificativa/usuario_id/reg_date e o Portal Profissional lê escala_id/status_novo/reg_date para
-- derivar "confirmado em" e "pode confirmar"; todas essas colunas estavam ausentes da tabela
-- (falha 500 latente no aceite de convite e na listagem do portal).

alter table plantaopro.historico_escala
    add column if not exists escala_id uuid,
    add column if not exists status_anterior text,
    add column if not exists status_novo text,
    add column if not exists justificativa text,
    add column if not exists usuario_id uuid,
    add column if not exists reg_date timestamptz;

create index if not exists ix_historico_escala_escala
    on plantaopro.historico_escala(escala_id);

-- ============================================================
-- Seção 114 — R7A1 Convite Ciclo de Vida v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2343_r7a1_convite_ciclo_vida.sql
-- SOURCE-SHA256: d3c57ceda6a8fafc585f77c101b77b5e78c98911dff3311254eb1486d879eb8e
-- PlantãoPro v2.34.3 (R7-A1): colunas de ciclo de vida de convites operacionais ausentes da estrutura.
-- O código canônico grava/lê data_resposta e motivo_recusa (aceite, recusa, cancelamento e listagens no
-- portal profissional/móvel) e reg_update e reg_date (aceite de convite e envio de convite); nenhuma
-- dessas colunas existia em plantao_convites (falha 500 latente no aceite/recusa e 500 latente nos
-- seletores de convites do portal e do app).

alter table plantaopro.plantao_convites
    add column if not exists data_resposta timestamptz,
    add column if not exists motivo_recusa text,
    add column if not exists reg_update timestamptz,
    add column if not exists reg_date timestamptz not null default now();

-- ============================================================
-- Seção 115 — R7E1 Usuario Idioma Preferido v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2344_r7e1_usuario_idioma_preferido.sql
-- SOURCE-SHA256: 623f6f52c4f1ee4ad0ba1e850094b5cdd4100c03023698da12671ae170302226
-- PlantãoPro v2.34.4 (R7-E1): preferência de idioma da interface do usuário.
-- A coluna persiste a escolha do USUÁRIO (precedência canônica: usuário > organização >
-- navegador > pt-BR; ver PlantaoPro.CrossCutting.Localization.UiLanguageCatalog). O padrão
-- da ORGANIZAÇÃO vive em tenants.metadata->>'idiomaPadrao' (jsonb existente, sem estrutura nova).
-- O idioma é INDEPENDENTE de moeda/fuso/fiscal: apenas rótulos da interface variam; formatos
-- permanecem pt-BR (CurrentCulture fixada) e conteúdo clínico/XML fiscal nunca é traduzido.

alter table plantaopro.usuarios
    add column if not exists idioma_preferido varchar(8);

alter table plantaopro.usuarios drop constraint if exists ck_usuarios_idioma_preferido;
alter table plantaopro.usuarios
    add constraint ck_usuarios_idioma_preferido
    check (idioma_preferido is null or idioma_preferido in ('pt-BR','en','es','fr'));

comment on column plantaopro.usuarios.idioma_preferido is
    'Idioma da interface escolhido pelo usuario (pt-BR/en/es/fr). Nulo = resolver por organizacao/navegador/pt-BR. R7-E1.';

-- ============================================================
-- Seção 116 — R7OB Onboarding Preferencias Idioma v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2345_r7ob_onboarding_preferencias_idioma.sql
-- SOURCE-SHA256: 15d6bdb179322525d976bc5a039cd6b707d288cc6aacf59fcd5b601628314029
-- R7-OB: jornada de onboarding — etapa "Definir o idioma da interface" (opcional, núcleo).
-- R7-E1 (v2344) criou a preferência persistida (usuarios.idioma_preferido) e o padrão de
-- organização (tenants.metadata->>'idiomaPadrao'). Esta migração expõe a configuração na
-- jornada por contrato com critério DERIVADO DE DADOS (mesma filosofia C7: a verdade segue
-- os dados, não o botão): concluída quando o padrão de idioma da organização está registrado
-- OU ao menos um usuário ativo do tenant tem preferência de idioma salva.
-- Opcional (obrigatorio=false): se nada for definido, pt-BR continua sendo o padrão silencioso;
-- a etapa pode ser pulada com justificativa persistida sem travar o onboarding.
-- Rerun idempotente: upsert pelo codigo único + MaterializarAsync só cria para tenants novos;
-- tenants existentes recebem a etapa via POST /api/onboarding/sincronizar (ou iniciar).

insert into plantaopro.onboarding_etapas_catalogo
(codigo,titulo,objetivo,descricao,responsavel,pre_requisito_codigo,modulo_codigo,criterio_codigo,link_acao,ordem,obrigatorio)
values
('ONB_PREFERENCIAS','Definir o idioma da interface','Garantir que a operação escolha conscientemente o idioma da interface (padrão da organização ou preferência individual).','Etapa opcional do núcleo: pode ser pulada com justificativa sem travar o onboarding. O critério deriva dos dados persistidos: padrão de idioma da organização registrado OU ao menos um usuário ativo com preferência de idioma salva (coluna v2344).','CLIENTE',null,null,'IDIOMA_PREFERENCIA_DEFINIDA','/Usuario/Index',50,false)
on conflict (codigo) do update set
    titulo = excluded.titulo,
    objetivo = excluded.objetivo,
    descricao = excluded.descricao,
    responsavel = excluded.responsavel,
    pre_requisito_codigo = excluded.pre_requisito_codigo,
    modulo_codigo = excluded.modulo_codigo,
    criterio_codigo = excluded.criterio_codigo,
    link_acao = excluded.link_acao,
    ordem = excluded.ordem,
    obrigatorio = excluded.obrigatorio,
    reg_update = now();

-- ============================================================
-- Seção 117 — R8B2 Notificacoes Legado Dedupe v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2346_r8b2_notificacoes_legado_dedupe.sql
-- SOURCE-SHA256: 97f2f638e79d002022a7528a0b810b0a48db9a4dce268b57986aa1701400ee65
-- PlantãoPro v2.34.6 (R8-B2): idempotência do caminho legado de notificações em retries.
-- Achado E3 (r8a0-varredura-financas-pacientes-imports-falhas-notificacoes.md): o caminho legado
-- (NotificacaoService.CriarNotificacaoAsync -> plantaopro.notificacoes) inseria sem dedupe/unique;
-- cada chamada repetida do mesmo evento criava uma notificação duplicada para o usuário.
-- Adota a convenção já estabelecida pelo in-app operacional (Operation360 NotificationService:
-- origem_tipo/origem_id + on conflict do nothing + inserted==0 -> false).
-- Dedupe PARCIAL e intencional: só notificações COM origem declarada (origem_id não nulo) são
-- deduplicadas; chamadas sem origem mantêm o comportamento histórico (uma por chamada).
-- A chave inclui titulo para permitir a sequência natural de eventos distintos do MESMO registro
-- (ex.: escala confirmada -> cancelada); repetir o mesmo evento do mesmo registro para o mesmo
-- usuário vira no-op. O predicato reg_status='A' preserva o padrão de vínculo ativo da base.
-- Rerun idempotente: add column if not exists / create unique index if not exists.

alter table plantaopro.notificacoes
    add column if not exists origem_tipo text,
    add column if not exists origem_id uuid;

create unique index if not exists ux_notificacao_legado_origem
    on plantaopro.notificacoes(usuario_id, origem_tipo, origem_id, titulo)
    where reg_status='A' and origem_id is not null;

-- ============================================================
-- Seção 118 — R8B4 Cancelamento Compras v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2347_r8b4_cancelamento_compras.sql
-- SOURCE-SHA256: 2e8ba13f929badb6c3a51961e0557611cec8532b319f58e7a383067094a6da8c
-- R8-B4: trilha de auditoria do cancelamento de pedido de compra (ADM360 suprimentos).
-- O estado CANCELADO ja existe no check de situacao (v2191); este corte so registra
-- quem/quando/por que cancelou, no mesmo padrao de auditoria dos demais cancels do modulo.
alter table plantaopro.adm360_pedidos add column if not exists motivo_cancelamento text;
alter table plantaopro.adm360_pedidos add column if not exists cancelado_em timestamptz;
alter table plantaopro.adm360_pedidos add column if not exists cancelado_por uuid;

-- ============================================================
-- Seção 119 — R8B5 Backfill Medico Especialidades v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2348_r8b5_backfill_medico_especialidades.sql
-- SOURCE-SHA256: 5317df66afb7a42f141e1846d230a8caa584cc5cce92c5a133e9c7e57c40ee64
-- PlantãoPro v2.34.8 (R8-B5): backfill de medico_especialidades a partir de medicos.especialidade_id.
-- O M:N nasceu em v2341 para sustentar a elegibilidade por especialidade e o score de recomendação
-- (BusinessRulesServices), mas nenhum writer produtivo o populava — a tabela nasce vazia em banco real
-- (matriz r8a0 linha "Especialidades na operação" / varredura A2). A partir daqui o MedicoService
-- espelha a especialidade do formulário (replace-all, histórico via reg_status); este backfill aplica a
-- mesma regra aos médicos já existentes: reativa a linha histórica quando existe, senão insere.
-- Idempotente: repetir não cria duplicatas nem desativa vínculos.
-- Defensivo: médicos com especialidade_id órfã (especialidade já inexistente) são pulados no insert.

update plantaopro.medico_especialidades me set reg_status='A'
from plantaopro.medicos m
where me.medico_id = m.id
  and me.especialidade_id = m.especialidade_id
  and me.reg_status <> 'A'
  and m.reg_status = 'A'
  and m.especialidade_id is not null
  and m.especialidade_id <> '00000000-0000-0000-0000-000000000000';

insert into plantaopro.medico_especialidades(medico_id, especialidade_id, reg_status)
select m.id, m.especialidade_id, 'A'
from plantaopro.medicos m
where m.reg_status = 'A'
  and m.especialidade_id is not null
  and m.especialidade_id <> '00000000-0000-0000-0000-000000000000'
  and not exists(select 1 from plantaopro.medico_especialidades me where me.medico_id = m.id and me.especialidade_id = m.especialidade_id)
  and exists(select 1 from plantaopro.especialidades e where e.id = m.especialidade_id);

-- ============================================================
-- Seção 120 — R8B6 Mesclagem Pacientes v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2349_r8b6_mesclagem_pacientes.sql
-- SOURCE-SHA256: 0b226ee6cb5d739ae84ba9aab3c68cde1c7f229cb00c624df2febb84e3fae1cd
-- PlantãoPro v2.34.9 (R8-B6): trilha de mesclagem de pacientes.
-- A operação (mesclagem controlada) re-aponta os vínculos do paciente de origem para o alvo e
-- inativa a origem; estas colunas tornam a absorção auditável e idempotente:
--   mesclado_em   -> paciente alvo da absorção (não nula = já mesclado)
--   mesclado_em_em-> instante da mesclagem
--   mesclado_por  -> usuário que executou
-- Idempotente: add column if not exists + índice condicional.

alter table plantaopro.pacientes add column if not exists mesclado_em uuid null;
alter table plantaopro.pacientes add column if not exists mesclado_em_em timestamptz null;
alter table plantaopro.pacientes add column if not exists mesclado_por uuid null;

create index if not exists ix_pacientes_mesclado_em on plantaopro.pacientes (mesclado_em) where mesclado_em is not null;

-- ============================================================
-- Seção 121 — R8B7 Glosa Item Recurso Documentos v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2350_r8b7_glosa_item_recurso_documentos.sql
-- SOURCE-SHA256: f363c5d37285179edfa51a9ce8c36d0095cd57b2d0e16296a384b303289beef6
-- PlantãoPro v2.35.0 (R8-B7): glosa por item de lote + recurso com documentos (matriz §22).
-- Antes: a glosa v115 era registrada apenas por conta/título — não havia referência ao item do
-- lote (v116_faturamento_lote_itens), o recurso não tinha trilha própria (nem justificativa nem
-- documentos persistidos) e a coluna v113_faturas.glosa_reconhecida era lida na fórmula do saldo
-- sem writer (a glosa não afetava o saldo). Esta migração torna o ciclo
-- lote -> item -> glosa -> recurso -> resolucao reconciliável no saldo da conta.
-- Idempotente: add column if not exists + create table/index if not exists.

alter table plantaopro.v115_regras_glosa add column if not exists lote_item_id uuid null;
alter table plantaopro.v115_regras_glosa add column if not exists recurso_justificativa text null;
alter table plantaopro.v115_regras_glosa add column if not exists recurso_em timestamptz null;

create index if not exists ix_v115_glosa_lote_item on plantaopro.v115_regras_glosa (lote_item_id) where reg_status = 'A';

alter table plantaopro.v116_faturamento_lote_itens add column if not exists lote_id uuid null;

create index if not exists ix_v116_lote_itens_tenant_lote on plantaopro.v116_faturamento_lote_itens (tenant_id, lote_id);

create table if not exists plantaopro.v115_recurso_documentos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    glosa_id uuid null,
    nome_arquivo text not null default '',
    tipo_mime text null,
    conteudo text null,
    tamanho int not null default 0,
    created_at timestamptz not null default now(),
    created_by uuid null,
    reg_status char(1) not null default 'A'
);

create index if not exists ix_v115_recurso_documentos_glosa on plantaopro.v115_recurso_documentos (glosa_id) where reg_status = 'A';

-- ============================================================
-- Seção 122 — R8B10 CID Tabela Unicao Versionada v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2351_r8b10_cid_tabela_unicao_versionada.sql
-- SOURCE-SHA256: 25a9c25f89b1e6aeefee695a4080df3be259386e6f78faed6bac3f29c793cb19
-- PlantãoPro v2.35.1 (R8-B10): unicidade canônica VERSIONADA de cid_tabela (§29 item 2 — drift de schema).
-- Antecedente: cid_tabela é um vocabulário de CID VERSIONADO (CID-10/CID-11). O importador
-- (Saude360ClinicalService, upsert ~L580) grava via ON CONFLICT (versao, upper(codigo)) WHERE reg_status='A'
-- AND codigo<>'', ou seja, o MESMO código pode existir em várias versões ao mesmo tempo.
-- Drift: 6 DDLs criaram cid_tabela com formas divergentes; sobraram TRÊS índices únicos sobre codigo:
--   ux_cid_tabela_codigo              UNIQUE (codigo)                                                    -> bloq. mesmo código em 2 versões
--   ux_cid_tabela_codigo_ativo        UNIQUE (upper(codigo)) WHERE reg_status='A' AND codigo<>''         -> idem (só ativos)
--   ux_cid_tabela_versao_codigo_ativo UNIQUE (versao, upper(codigo)) WHERE reg_status='A' AND codigo<>'' -> CANÔNICO (alvo do upsert)
-- Os dois primeiros contradizem o modelo versionado: importar o mesmo código sob duas versões falhava
-- com 23505 no índice plano (bug latente — o import nunca cruzou versões nos testes até aqui).
-- Correção (relaxação monotônica, sem perda de dados; 0 duplicidades existentes no base): remove as duas
-- unicidades NÃO-versionadas e reafirma a versionada (canônica). Idempotente. Sem alteração de colunas —
-- o superset já converge via alter-add idempotente dos DDLs anteriores na ordem de aplicação; a
-- deduplicação dos 6 blocos CREATE segue o rito §29 (nenhuma deleção de tabela/DDL nesta rodada).
-- Robustez: em alguns ambientes cada unicidade é um índice puro (CREATE UNIQUE INDEX); em outros,
-- uma RESTRIÇÃO de tabela (ADD CONSTRAINT ... UNIQUE) — e DROP INDEX não remove índice que sustenta
-- restrição. Por isso, para cada nome: tenta DROP CONSTRAINT (se for restrição) e depois DROP INDEX
-- (se for índice puro, ou vira no-op após a restrição). Idempotente nos dois sentidos.
alter table if exists plantaopro.cid_tabela drop constraint if exists ux_cid_tabela_codigo;
drop index if exists plantaopro.ux_cid_tabela_codigo;
alter table if exists plantaopro.cid_tabela drop constraint if exists ux_cid_tabela_codigo_ativo;
drop index if exists plantaopro.ux_cid_tabela_codigo_ativo;
-- Reafirma a unicidade versionada (canônica; é o alvo do ON CONFLICT do importador).
-- OBS: o NOME do índice não leva o esquema (Postgres: CREATE INDEX [nome] ON esquema.tabela).
create unique index if not exists ux_cid_tabela_versao_codigo_ativo
    on plantaopro.cid_tabela (versao, upper(codigo)) where reg_status = 'A' and codigo <> '';

-- ============================================================
-- Seção 123 — R9C Cadastro ANVISA Produtos v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2352_r9c_cadastro_anvisa_produtos.sql
-- SOURCE-SHA256: 455574771556eff4a86cd33522e56e1acbf9e4470c62f2e1461412a8a7d027eb
-- PlantãoPro v2.35.2 (R9-C / §6): cadastro regulatório ANVISA para produtos do Administrativo 360.
-- Requisitos da rodada: número como TEXTO (preserva zeros à esquerda — nunca numérico),
-- aplicabilidade tri-estável (INFORMADO / NAO_APLICAVEL / PENDENTE), flag de entrada manual
-- (não há busca simulada: toda entrada atual é manual), vigência REGULATÓRIA independente da
-- validade do lote, e justificação obrigatória quando o produto é marcado como não aplicável.
-- Rerun idempotente: add column if not exists / drop-add constraint.

alter table plantaopro.adm360_produtos
    add column if not exists anvisa_numero varchar(60),
    add column if not exists anvisa_aplicabilidade varchar(15) not null default 'PENDENTE',
    add column if not exists anvisa_entrada_manual boolean not null default true,
    add column if not exists anvisa_vigencia date,
    add column if not exists anvisa_justificacao text;

alter table plantaopro.adm360_produtos
    drop constraint if exists ck_adm360_produtos_anvisa_aplicabilidade,
    add constraint ck_adm360_produtos_anvisa_aplicabilidade
        check (anvisa_aplicabilidade in ('INFORMADO', 'NAO_APLICAVEL', 'PENDENTE'));

-- ============================================================
-- Seção 124 — R9D Precificacao Preco Venda v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2353_r9d_precificacao_preco_venda.sql
-- SOURCE-SHA256: 4580ee72997a604d765b78cdefd05d40e268679c738275e0ca0786c802be7283
-- PlantãoPro v2.35.3 (R9-D / §7): preço de venda padrão dos produtos do Administrativo 360 (precificação).
-- preco_venda numeric(18,4) NULL — NULL = preço de venda padrão NÃO DEFINIDO (estado "ausente"),
-- nunca substituído por zero. O custo (preco_custo) permanece nullable: CUSTO AUSENTE ≠ ZERO.
-- Os campos derivados (lucro bruto unitário, margem sobre venda %, markup sobre custo %) são
-- recalculados NO SERVIDOR (Application/Administrativo360/Contracts.cs - Produto360) com
-- arredondamento de 2 casas; denominador zero => "não calculável"; margem negativa não é
-- truncada (evidência de margem negativa). Esta DDL não reescreve cotações/vendas existentes.
-- Rerun idempotente: add column if not exists / comment on (sobrescreve).

alter table plantaopro.adm360_produtos
    add column if not exists preco_venda numeric(18,4);

-- Custo AUSENTE ≠ ZERO: a coluna histórica é NOT NULL DEFAULT 0, o que tornava impossível
-- representar "custo não informado" (o default 0 virava margem 100% nas derivadas).
-- Passa a admitir NULL; linhas existentes não são modificadas (sem backfill).
-- Rerun idempotente: drop not null em coluna já nullable é no-op.
alter table plantaopro.adm360_produtos
    alter column preco_custo drop not null;

comment on column plantaopro.adm360_produtos.preco_venda is
    'Preco de venda padrao (s7 rodada 9). NULL = nao definido; derivadas calculadas no servidor; custo ausente != zero.';

-- ============================================================
-- Seção 125 — R55 Indice Reservas Saldo v2.21.9
-- ============================================================

-- SOURCE: database/migrations/2026_10_v2354_r55_indice_reservas_saldo.sql
-- SOURCE-SHA256: 31ab4e426a5f88fe7d249c1ee4ed59f627e894e7b795cca8907a486e37259ea5
-- R55: índice de apoio à view adm360_saldos — cobre o padrão da subquery correlacionada das
-- reservas ativas (sem ele cada grupo de saldo refaz scan completo de adm360_reservas duas vezes).
CREATE INDEX IF NOT EXISTS ix_adm360_reservas_saldo ON plantaopro.adm360_reservas(tenant_id,produto_id,lote_id,local_id) INCLUDE(quantidade) WHERE situacao='ATIVA';
