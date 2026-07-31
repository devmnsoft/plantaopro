-- v1.27.0: normalização de permissões e prontuário operacional (idempotente)
CREATE TABLE IF NOT EXISTS plantaopro.perfil_permissoes (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), perfil_id uuid NOT NULL, permissao_id uuid NOT NULL,
 permitido boolean NOT NULL DEFAULT true, bloqueado_por_plano boolean NOT NULL DEFAULT false,
 tenant_id uuid, cliente_id uuid, created_by uuid, updated_by uuid,
 reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz, reg_status char(1) NOT NULL DEFAULT 'A');
DO $$
DECLARE tem_status boolean; tem_permitido boolean; tem_bloqueio boolean;
BEGIN
 IF to_regclass('plantaopro.perfis_permissoes') IS NOT NULL THEN
  SELECT EXISTS(SELECT 1 FROM information_schema.columns WHERE table_schema='plantaopro' AND table_name='perfis_permissoes' AND column_name='reg_status'),
         EXISTS(SELECT 1 FROM information_schema.columns WHERE table_schema='plantaopro' AND table_name='perfis_permissoes' AND column_name='permitido'),
         EXISTS(SELECT 1 FROM information_schema.columns WHERE table_schema='plantaopro' AND table_name='perfis_permissoes' AND column_name='bloqueado_por_plano')
    INTO tem_status,tem_permitido,tem_bloqueio;
  EXECUTE format('INSERT INTO plantaopro.perfil_permissoes(perfil_id,permissao_id,permitido,bloqueado_por_plano,reg_status,reg_date)
    SELECT perfil_id,permissao_id,%s,%s,%s,now() FROM plantaopro.perfis_permissoes l
    WHERE perfil_id IS NOT NULL AND permissao_id IS NOT NULL AND NOT EXISTS
      (SELECT 1 FROM plantaopro.perfil_permissoes c WHERE c.perfil_id=l.perfil_id AND c.permissao_id=l.permissao_id AND c.reg_status=''A'')',
      CASE WHEN tem_permitido THEN 'coalesce(permitido,true)' ELSE 'true' END,
      CASE WHEN tem_bloqueio THEN 'coalesce(bloqueado_por_plano,false)' ELSE 'false' END,
      CASE WHEN tem_status THEN 'coalesce(reg_status,''A'')' ELSE '''A''' END);
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE contype='f' AND confrelid='plantaopro.perfis_permissoes'::regclass) THEN
    DROP TABLE plantaopro.perfis_permissoes;
  END IF;
 END IF;
END $$;
CREATE UNIQUE INDEX IF NOT EXISTS ux_perfil_permissoes_ativo ON plantaopro.perfil_permissoes(perfil_id,permissao_id) WHERE reg_status='A';
CREATE INDEX IF NOT EXISTS ix_perfil_permissoes_cliente ON plantaopro.perfil_permissoes(cliente_id,perfil_id) WHERE reg_status='A';

ALTER TABLE plantaopro.consultas ADD COLUMN IF NOT EXISTS unidade_id uuid;
ALTER TABLE plantaopro.consultas ADD COLUMN IF NOT EXISTS atendimento_id uuid;
ALTER TABLE plantaopro.consultas ADD COLUMN IF NOT EXISTS triagem_id uuid;
ALTER TABLE plantaopro.consultas ADD COLUMN IF NOT EXISTS hipotese_diagnostica text;
ALTER TABLE plantaopro.consultas ADD COLUMN IF NOT EXISTS orientacoes text;
ALTER TABLE plantaopro.consultas ADD COLUMN IF NOT EXISTS inicio_em timestamptz;
ALTER TABLE plantaopro.consultas ADD COLUMN IF NOT EXISTS finalizada_em timestamptz;
ALTER TABLE plantaopro.consultas ADD COLUMN IF NOT EXISTS cancelada_em timestamptz;
ALTER TABLE plantaopro.consultas ADD COLUMN IF NOT EXISTS motivo_cancelamento text;
ALTER TABLE plantaopro.consultas ADD COLUMN IF NOT EXISTS versao integer NOT NULL DEFAULT 1;

CREATE TABLE IF NOT EXISTS plantaopro.consulta_cids (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, consulta_id uuid NOT NULL,
 cid_id uuid NOT NULL, tipo varchar(20) NOT NULL DEFAULT 'SECUNDARIO', principal boolean NOT NULL DEFAULT false,
 ordem integer NOT NULL DEFAULT 1, created_by uuid, updated_by uuid, reg_date timestamptz NOT NULL DEFAULT now(),
 reg_update timestamptz, reg_status char(1) NOT NULL DEFAULT 'A',
 CONSTRAINT fk_consulta_cids_consulta FOREIGN KEY(consulta_id) REFERENCES plantaopro.consultas(id),
 CONSTRAINT fk_consulta_cids_cid FOREIGN KEY(cid_id) REFERENCES plantaopro.cid_tabela(id));
CREATE UNIQUE INDEX IF NOT EXISTS ux_consulta_cids_cid_ativo ON plantaopro.consulta_cids(cliente_id,consulta_id,cid_id) WHERE reg_status='A';
CREATE UNIQUE INDEX IF NOT EXISTS ux_consulta_cids_principal ON plantaopro.consulta_cids(cliente_id,consulta_id) WHERE principal AND reg_status='A';
CREATE INDEX IF NOT EXISTS ix_consulta_cids_consulta ON plantaopro.consulta_cids(cliente_id,consulta_id,ordem) WHERE reg_status='A';

CREATE TABLE IF NOT EXISTS plantaopro.prescricao_itens (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, prescricao_id uuid NOT NULL,
 medicamento_nome varchar(200) NOT NULL, principio_ativo varchar(200), apresentacao varchar(120), concentracao varchar(80),
 dose varchar(80) NOT NULL, unidade_dose varchar(40), via_administracao varchar(60) NOT NULL, frequencia varchar(100) NOT NULL,
 duracao varchar(80), quantidade numeric(12,3), instrucoes text, uso_continuo boolean NOT NULL DEFAULT false,
 ordem integer NOT NULL DEFAULT 1, created_by uuid, updated_by uuid, reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz, reg_status char(1) NOT NULL DEFAULT 'A',
 CONSTRAINT fk_prescricao_itens_prescricao FOREIGN KEY(prescricao_id) REFERENCES plantaopro.prescricoes(id));
CREATE INDEX IF NOT EXISTS ix_prescricao_itens_prescricao ON plantaopro.prescricao_itens(cliente_id,prescricao_id,ordem) WHERE reg_status='A';

CREATE TABLE IF NOT EXISTS plantaopro.migrations_aplicadas(nome text PRIMARY KEY, aplicada_em timestamptz NOT NULL DEFAULT now());
INSERT INTO plantaopro.migrations_aplicadas(nome,aplicada_em)
SELECT '2026_v1270_normalizar_permissoes_e_prontuario',now()
WHERE NOT EXISTS(SELECT 1 FROM plantaopro.migrations_aplicadas WHERE nome='2026_v1270_normalizar_permissoes_e_prontuario');
