-- v1.18.9: contrato definitivo de identidade/autenticacao para upgrade legado.
SET search_path TO plantaopro, public;
CREATE SCHEMA IF NOT EXISTS plantaopro;
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS unaccent;

CREATE TABLE IF NOT EXISTS plantaopro.perfis (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nome text NOT NULL,
    descricao text NULL,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS plantaopro.usuarios (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nome text NOT NULL,
    email text NOT NULL,
    senha_hash text NOT NULL,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now()
);

ALTER TABLE plantaopro.usuarios
    ADD COLUMN IF NOT EXISTS tenant_id uuid,
    ADD COLUMN IF NOT EXISTS cliente_id uuid,
    ADD COLUMN IF NOT EXISTS email_normalizado text,
    ADD COLUMN IF NOT EXISTS status text DEFAULT 'ATIVO',
    ADD COLUMN IF NOT EXISTS bloqueado_ate timestamptz,
    ADD COLUMN IF NOT EXISTS senha_alteracao_obrigatoria boolean DEFAULT false,
    ADD COLUMN IF NOT EXISTS ultimo_login timestamptz,
    ADD COLUMN IF NOT EXISTS preferencias_notificacao jsonb DEFAULT '{}'::jsonb,
    ADD COLUMN IF NOT EXISTS reg_update timestamptz,
    ADD COLUMN IF NOT EXISTS created_by uuid,
    ADD COLUMN IF NOT EXISTS updated_by uuid;

UPDATE plantaopro.usuarios
SET email_normalizado = upper(btrim(email))
WHERE email_normalizado IS NULL OR btrim(email_normalizado) = '';
UPDATE plantaopro.usuarios
SET status = CASE WHEN coalesce(reg_status,'A') = 'A' THEN 'ATIVO' ELSE 'INATIVO' END
WHERE status IS NULL OR btrim(status) = '';
UPDATE plantaopro.usuarios SET senha_alteracao_obrigatoria = false WHERE senha_alteracao_obrigatoria IS NULL;
UPDATE plantaopro.usuarios SET preferencias_notificacao = '{}'::jsonb WHERE preferencias_notificacao IS NULL;

ALTER TABLE plantaopro.usuarios
    ALTER COLUMN email_normalizado SET NOT NULL,
    ALTER COLUMN status SET DEFAULT 'ATIVO',
    ALTER COLUMN status SET NOT NULL,
    ALTER COLUMN senha_alteracao_obrigatoria SET DEFAULT false,
    ALTER COLUMN senha_alteracao_obrigatoria SET NOT NULL,
    ALTER COLUMN preferencias_notificacao SET DEFAULT '{}'::jsonb,
    ALTER COLUMN preferencias_notificacao SET NOT NULL;

ALTER TABLE plantaopro.perfis
    ADD COLUMN IF NOT EXISTS tenant_id uuid,
    ADD COLUMN IF NOT EXISTS cliente_id uuid,
    ADD COLUMN IF NOT EXISTS codigo text,
    ADD COLUMN IF NOT EXISTS base_sistema boolean DEFAULT false,
    ADD COLUMN IF NOT EXISTS customizado boolean DEFAULT false,
    ADD COLUMN IF NOT EXISTS status text DEFAULT 'ATIVO',
    ADD COLUMN IF NOT EXISTS reg_update timestamptz,
    ADD COLUMN IF NOT EXISTS created_by uuid,
    ADD COLUMN IF NOT EXISTS updated_by uuid;

UPDATE plantaopro.perfis
SET codigo = upper(regexp_replace(public.unaccent(coalesce(nullif(codigo,''), nome, id::text)), '[^A-Za-z0-9]+', '_', 'g'))
WHERE codigo IS NULL OR btrim(codigo) = '';
UPDATE plantaopro.perfis SET base_sistema = false WHERE base_sistema IS NULL;
UPDATE plantaopro.perfis SET customizado = false WHERE customizado IS NULL;
UPDATE plantaopro.perfis SET status = CASE WHEN coalesce(reg_status,'A') = 'A' THEN 'ATIVO' ELSE 'INATIVO' END WHERE status IS NULL OR btrim(status) = '';

WITH dup AS (
  SELECT id, row_number() OVER (PARTITION BY coalesce(tenant_id,'00000000-0000-0000-0000-000000000000'::uuid), lower(codigo) ORDER BY reg_date, id) rn
  FROM plantaopro.perfis WHERE reg_status='A'
)
UPDATE plantaopro.perfis p SET codigo = p.codigo || '_' || left(p.id::text,8), reg_update=now() FROM dup WHERE dup.id=p.id AND dup.rn>1;

ALTER TABLE plantaopro.perfis
    ALTER COLUMN codigo SET NOT NULL,
    ALTER COLUMN base_sistema SET DEFAULT false,
    ALTER COLUMN base_sistema SET NOT NULL,
    ALTER COLUMN customizado SET DEFAULT false,
    ALTER COLUMN customizado SET NOT NULL,
    ALTER COLUMN status SET DEFAULT 'ATIVO',
    ALTER COLUMN status SET NOT NULL;

CREATE TABLE IF NOT EXISTS plantaopro.usuarios_perfis (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NOT NULL, perfil_id uuid NOT NULL,
    reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL, created_by uuid NULL, updated_by uuid NULL
);
ALTER TABLE plantaopro.usuarios_perfis ADD COLUMN IF NOT EXISTS tenant_id uuid, ADD COLUMN IF NOT EXISTS cliente_id uuid, ADD COLUMN IF NOT EXISTS usuario_id uuid, ADD COLUMN IF NOT EXISTS perfil_id uuid, ADD COLUMN IF NOT EXISTS reg_status char(1) DEFAULT 'A', ADD COLUMN IF NOT EXISTS reg_date timestamptz DEFAULT now(), ADD COLUMN IF NOT EXISTS reg_update timestamptz, ADD COLUMN IF NOT EXISTS created_by uuid, ADD COLUMN IF NOT EXISTS updated_by uuid;

CREATE TABLE IF NOT EXISTS plantaopro.login_tentativas(id uuid primary key default gen_random_uuid(), usuario_id uuid null, email text not null, ip text null, user_agent text null, sucesso boolean not null, motivo text not null, bloqueado_ate timestamptz null, reg_date timestamptz not null default now(), reg_update timestamptz null, reg_status char(1) not null default 'A');
CREATE TABLE IF NOT EXISTS plantaopro.recuperacao_senha(id uuid primary key default gen_random_uuid(), usuario_id uuid not null, token_hash text not null, expiracao timestamptz not null, utilizado boolean not null default false, reg_date timestamptz not null default now(), reg_update timestamptz null, reg_status char(1) not null default 'A');
CREATE TABLE IF NOT EXISTS plantaopro.auth_sessoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, cliente_id uuid NULL, usuario_id uuid NOT NULL, dispositivo_nome text NOT NULL DEFAULT 'Dispositivo não identificado', ip_mascarado text NULL, user_agent_sanitizado text NULL, iniciado_em timestamptz NOT NULL DEFAULT now(), ultimo_uso_em timestamptz NULL, expira_em timestamptz NULL, revogada_em timestamptz NULL, motivo_revogacao text NULL, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL);
CREATE TABLE IF NOT EXISTS plantaopro.auth_refresh_tokens(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), sessao_id uuid NOT NULL, usuario_id uuid NOT NULL, token_hash text NOT NULL, emitido_em timestamptz NOT NULL DEFAULT now(), expira_em timestamptz NOT NULL, usado_em timestamptz NULL, substituido_por_id uuid NULL, revogado_em timestamptz NULL, motivo_revogacao text NULL, reg_status char(1) NOT NULL DEFAULT 'A');
CREATE TABLE IF NOT EXISTS plantaopro.auth_revogacoes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), sessao_id uuid NULL, usuario_id uuid NOT NULL, motivo text NOT NULL, revogado_por uuid NULL, ip_mascarado text NULL, reg_date timestamptz NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A');
CREATE TABLE IF NOT EXISTS plantaopro.senha_historico(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), usuario_id uuid NOT NULL, senha_hash text NOT NULL, origem text NOT NULL DEFAULT 'ALTERACAO_SENHA', reg_date timestamptz NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A');
CREATE TABLE IF NOT EXISTS plantaopro.politicas_senha(id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, tamanho_minimo int NOT NULL DEFAULT 10, exige_maiuscula boolean NOT NULL DEFAULT true, exige_minuscula boolean NOT NULL DEFAULT true, exige_numero boolean NOT NULL DEFAULT true, exige_especial boolean NOT NULL DEFAULT true, historico_quantidade int NOT NULL DEFAULT 5, expiracao_dias int NOT NULL DEFAULT 90, tentativas_permitidas int NOT NULL DEFAULT 5, bloqueio_minutos int NOT NULL DEFAULT 30, troca_obrigatoria boolean NOT NULL DEFAULT false, proibir_senhas_comuns boolean NOT NULL DEFAULT true, reg_status char(1) NOT NULL DEFAULT 'A', reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL);

INSERT INTO plantaopro.perfis(tenant_id,cliente_id,codigo,nome,descricao,base_sistema,customizado,status,reg_status)
SELECT NULL,NULL,'ADMINISTRADOR_GLOBAL','Administrador Global','Acesso administrativo global do sistema',true,false,'ATIVO','A'
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.perfis WHERE tenant_id IS NULL AND codigo='ADMINISTRADOR_GLOBAL' AND reg_status='A');

CREATE UNIQUE INDEX IF NOT EXISTS ux_perfis_tenant_codigo ON plantaopro.perfis(coalesce(tenant_id, '00000000-0000-0000-0000-000000000000'::uuid), lower(codigo)) WHERE reg_status='A';
CREATE UNIQUE INDEX IF NOT EXISTS ux_usuarios_email_normalizado ON plantaopro.usuarios(lower(email_normalizado)) WHERE reg_status='A';
CREATE UNIQUE INDEX IF NOT EXISTS ux_usuarios_perfis_usuario_perfil_ativo ON plantaopro.usuarios_perfis(usuario_id,perfil_id) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_login_tentativas_usuario_data ON plantaopro.login_tentativas(usuario_id, reg_date desc);
CREATE INDEX IF NOT EXISTS ix_recuperacao_senha_usuario_token ON plantaopro.recuperacao_senha(usuario_id, token_hash);
CREATE INDEX IF NOT EXISTS ix_auth_sessoes_usuario_status ON plantaopro.auth_sessoes(usuario_id, reg_status, ultimo_uso_em DESC);
CREATE INDEX IF NOT EXISTS ix_auth_refresh_tokens_sessao ON plantaopro.auth_refresh_tokens(sessao_id, expira_em DESC);
CREATE INDEX IF NOT EXISTS ix_auth_revogacoes_usuario ON plantaopro.auth_revogacoes(usuario_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_senha_historico_usuario ON plantaopro.senha_historico(usuario_id, reg_date DESC);
CREATE UNIQUE INDEX IF NOT EXISTS ux_politicas_senha_tenant ON plantaopro.politicas_senha(coalesce(tenant_id, '00000000-0000-0000-0000-000000000000'::uuid)) WHERE reg_status='A';
