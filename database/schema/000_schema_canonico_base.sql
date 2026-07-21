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

UPDATE plantaopro.permissoes SET codigo = upper(regexp_replace(public.unaccent(coalesce(nullif(codigo,''), nullif(nome,''), id::text)::text), '[^A-Za-z0-9]+', '_', 'g')) WHERE codigo IS NULL OR btrim(codigo)='';
UPDATE plantaopro.permissoes SET modulo = coalesce(nullif(modulo,''), split_part(codigo,'_',1), 'GERAL'), acao = coalesce(nullif(acao,''), nullif(array_to_string((regexp_split_to_array(codigo,'_'))[2:array_length(regexp_split_to_array(codigo,'_'),1)], '_'), ''), 'ACESSAR'), nome = coalesce(nullif(nome,''), codigo), descricao = coalesce(descricao,''), sensivel = coalesce(sensivel,false), status = coalesce(nullif(status,''),'ATIVO'), reg_status = coalesce(nullif(reg_status,''),'A'), reg_date = coalesce(reg_date, now());
WITH dup AS (SELECT id, row_number() OVER (PARTITION BY lower(codigo), reg_status ORDER BY reg_date, id) rn FROM plantaopro.permissoes WHERE reg_status='A') UPDATE plantaopro.permissoes p SET codigo = p.codigo || '_' || left(p.id::text,8), reg_update=now() FROM dup WHERE dup.id=p.id AND dup.rn>1;
INSERT INTO plantaopro.modulos_sistema(codigo,nome) SELECT DISTINCT upper(regexp_replace(public.unaccent(modulo::text), '[^A-Za-z0-9]+', '_', 'g')), modulo FROM plantaopro.permissoes p WHERE p.modulo IS NOT NULL AND NOT EXISTS (SELECT 1 FROM plantaopro.modulos_sistema m WHERE lower(m.codigo)=lower(upper(regexp_replace(public.unaccent(p.modulo::text), '[^A-Za-z0-9]+', '_', 'g'))) AND m.reg_status='A');
INSERT INTO plantaopro.acoes_sistema(codigo,nome) SELECT DISTINCT upper(regexp_replace(public.unaccent(acao::text), '[^A-Za-z0-9]+', '_', 'g')), acao FROM plantaopro.permissoes p WHERE p.acao IS NOT NULL AND NOT EXISTS (SELECT 1 FROM plantaopro.acoes_sistema a WHERE lower(a.codigo)=lower(upper(regexp_replace(public.unaccent(p.acao::text), '[^A-Za-z0-9]+', '_', 'g'))) AND a.reg_status='A');
UPDATE plantaopro.permissoes p SET modulo_id=m.id FROM plantaopro.modulos_sistema m WHERE p.modulo_id IS NULL AND lower(m.codigo)=lower(upper(regexp_replace(public.unaccent(p.modulo::text), '[^A-Za-z0-9]+', '_', 'g'))) AND m.reg_status='A';
UPDATE plantaopro.permissoes p SET acao_id=a.id FROM plantaopro.acoes_sistema a WHERE p.acao_id IS NULL AND lower(a.codigo)=lower(upper(regexp_replace(public.unaccent(p.acao::text), '[^A-Za-z0-9]+', '_', 'g'))) AND a.reg_status='A';
DO $$ BEGIN IF EXISTS (SELECT 1 FROM plantaopro.permissoes WHERE codigo IS NULL OR modulo_id IS NULL OR acao_id IS NULL) THEN RAISE EXCEPTION 'Permissões canônicas inválidas: codigo/modulo_id/acao_id nulos'; END IF; END $$;
ALTER TABLE plantaopro.permissoes ALTER COLUMN codigo SET NOT NULL, ALTER COLUMN modulo_id SET NOT NULL, ALTER COLUMN acao_id SET NOT NULL, ALTER COLUMN nome SET NOT NULL, ALTER COLUMN descricao SET DEFAULT '', ALTER COLUMN sensivel SET DEFAULT false, ALTER COLUMN sensivel SET NOT NULL, ALTER COLUMN status SET DEFAULT 'ATIVO', ALTER COLUMN status SET NOT NULL, ALTER COLUMN reg_status SET DEFAULT 'A', ALTER COLUMN reg_status SET NOT NULL, ALTER COLUMN reg_date SET DEFAULT now(), ALTER COLUMN reg_date SET NOT NULL;
ALTER TABLE plantaopro.modulos_sistema ALTER COLUMN codigo SET NOT NULL, ALTER COLUMN nome SET NOT NULL, ALTER COLUMN descricao SET DEFAULT '', ALTER COLUMN descricao SET NOT NULL, ALTER COLUMN status SET DEFAULT 'ATIVO', ALTER COLUMN status SET NOT NULL, ALTER COLUMN reg_status SET DEFAULT 'A', ALTER COLUMN reg_status SET NOT NULL;
ALTER TABLE plantaopro.acoes_sistema ALTER COLUMN codigo SET NOT NULL, ALTER COLUMN nome SET NOT NULL, ALTER COLUMN descricao SET DEFAULT '', ALTER COLUMN descricao SET NOT NULL, ALTER COLUMN sensivel SET DEFAULT false, ALTER COLUMN sensivel SET NOT NULL, ALTER COLUMN status SET DEFAULT 'ATIVO', ALTER COLUMN status SET NOT NULL, ALTER COLUMN reg_status SET DEFAULT 'A', ALTER COLUMN reg_status SET NOT NULL;
DO $$ BEGIN IF to_regclass('plantaopro.perfis_permissoes') IS NOT NULL THEN INSERT INTO plantaopro.perfil_permissoes(perfil_id,permissao_id,permitido,reg_status,reg_date) SELECT perfil_id,permissao_id,true,coalesce(reg_status,'A'),coalesce(reg_date,now()) FROM plantaopro.perfis_permissoes pp WHERE NOT EXISTS (SELECT 1 FROM plantaopro.perfil_permissoes x WHERE x.perfil_id=pp.perfil_id AND x.permissao_id=pp.permissao_id AND x.reg_status='A'); END IF; IF to_regclass('plantaopro.usuario_perfis') IS NOT NULL THEN INSERT INTO plantaopro.usuarios_perfis(usuario_id,perfil_id,reg_status,reg_date) SELECT usuario_id,perfil_id,coalesce(reg_status,'A'),coalesce(reg_date,now()) FROM plantaopro.usuario_perfis up WHERE NOT EXISTS (SELECT 1 FROM plantaopro.usuarios_perfis x WHERE x.usuario_id=up.usuario_id AND x.perfil_id=up.perfil_id AND x.reg_status='A'); END IF; END $$;
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
