-- PlantãoPro v1.42.0 - interface e fluxos operacionais
CREATE TABLE IF NOT EXISTS saved_views (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    usuario_id uuid NOT NULL,
    nome varchar(120) NOT NULL,
    modulo varchar(80) NOT NULL,
    filtros jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS user_favorites (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    usuario_id uuid NOT NULL,
    titulo varchar(160) NOT NULL,
    url varchar(500) NOT NULL,
    criado_em timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS recent_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    usuario_id uuid NOT NULL,
    entidade varchar(80) NOT NULL,
    entidade_id uuid NOT NULL,
    titulo varchar(160) NOT NULL,
    acessado_em timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS work_item_contextos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    work_item_id uuid NOT NULL,
    contexto jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS notificacao_agrupamentos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    chave varchar(160) NOT NULL,
    titulo varchar(160) NOT NULL,
    prioridade varchar(30) NOT NULL DEFAULT 'normal',
    expira_em timestamptz NULL,
    criado_em timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS user_dashboard_preferences (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    usuario_id uuid NOT NULL,
    perfil varchar(80) NOT NULL,
    preferencias jsonb NOT NULL DEFAULT '{}'::jsonb,
    atualizado_em timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS operational_timeline_events (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    entidade varchar(80) NOT NULL,
    entidade_id uuid NOT NULL,
    tipo varchar(80) NOT NULL,
    titulo varchar(160) NOT NULL,
    descricao text NULL,
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_por uuid NULL,
    criado_em timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS ui_audit_events (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    usuario_id uuid NULL,
    acao varchar(120) NOT NULL,
    rota varchar(300) NOT NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    criado_em timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_saved_views_usuario_modulo ON saved_views(usuario_id, modulo);
CREATE INDEX IF NOT EXISTS idx_recent_items_usuario ON recent_items(usuario_id, acessado_em DESC);
CREATE INDEX IF NOT EXISTS idx_operational_timeline_entidade ON operational_timeline_events(entidade, entidade_id, criado_em DESC);
