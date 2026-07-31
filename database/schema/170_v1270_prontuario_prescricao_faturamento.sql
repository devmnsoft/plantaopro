-- Fonte canônica v1.27.0 para instalação limpa.
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
