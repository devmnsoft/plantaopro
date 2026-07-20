CREATE SCHEMA IF NOT EXISTS plantaopro;
ALTER TABLE IF EXISTS plantaopro.v116_timelines ADD COLUMN IF NOT EXISTS entidade varchar(120) NULL;
ALTER TABLE IF EXISTS plantaopro.v116_timelines ADD COLUMN IF NOT EXISTS entidade_id uuid NULL;
ALTER TABLE IF EXISTS plantaopro.v116_notificacoes_operacionais ADD COLUMN IF NOT EXISTS prioridade varchar(30) NOT NULL DEFAULT 'MEDIA';
ALTER TABLE IF EXISTS plantaopro.v116_notificacoes_operacionais ADD COLUMN IF NOT EXISTS perfil_responsavel varchar(80) NOT NULL DEFAULT 'OPERACAO';
CREATE INDEX IF NOT EXISTS ix_v117_v116_timelines_entidade ON plantaopro.v116_timelines(tenant_id, entidade, entidade_id, created_at);
CREATE INDEX IF NOT EXISTS ix_v117_v116_notificacoes_prioridade ON plantaopro.v116_notificacoes_operacionais(tenant_id, prioridade, status_operacional, created_at);
