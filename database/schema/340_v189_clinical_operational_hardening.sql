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
