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
