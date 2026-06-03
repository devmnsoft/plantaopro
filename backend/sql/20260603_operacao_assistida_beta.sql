CREATE SCHEMA IF NOT EXISTS plantaopro;

CREATE TABLE IF NOT EXISTS plantaopro.operacao_assistida_clientes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    status varchar(40) NOT NULL DEFAULT 'EM_IMPLANTACAO',
    responsavel varchar(120),
    inicio_previsto timestamp,
    go_live_previsto timestamp,
    percentual integer NOT NULL DEFAULT 0,
    risco varchar(20) NOT NULL DEFAULT 'BAIXO',
    observacoes text,
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp,
    reg_status char(1) NOT NULL DEFAULT 'A'
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_operacao_assistida_clientes_cliente ON plantaopro.operacao_assistida_clientes(cliente_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_clientes_status ON plantaopro.operacao_assistida_clientes(status, reg_status);

CREATE TABLE IF NOT EXISTS plantaopro.operacao_assistida_checklist (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    ordem integer NOT NULL,
    titulo varchar(180) NOT NULL,
    descricao text,
    concluido boolean NOT NULL DEFAULT false,
    concluido_em timestamp,
    concluido_por varchar(120),
    justificativa text,
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp,
    reg_status char(1) NOT NULL DEFAULT 'A'
);

CREATE INDEX IF NOT EXISTS ix_operacao_assistida_checklist_cliente ON plantaopro.operacao_assistida_checklist(cliente_id, ordem, reg_status);

CREATE TABLE IF NOT EXISTS plantaopro.operacao_assistida_ocorrencias (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tipo varchar(30) NOT NULL,
    prioridade varchar(20) NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'ABERTA',
    responsavel varchar(120),
    descricao text NOT NULL,
    solucao text,
    data_abertura timestamp NOT NULL DEFAULT now(),
    data_resolucao timestamp,
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp,
    reg_status char(1) NOT NULL DEFAULT 'A'
);

CREATE INDEX IF NOT EXISTS ix_operacao_assistida_ocorrencias_cliente_status ON plantaopro.operacao_assistida_ocorrencias(cliente_id, status, prioridade, reg_status);

CREATE TABLE IF NOT EXISTS plantaopro.operacao_assistida_treinamentos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tema varchar(180) NOT NULL,
    perfil varchar(60),
    responsavel varchar(120),
    participantes text,
    realizado_em timestamp NOT NULL DEFAULT now(),
    observacoes text,
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_status char(1) NOT NULL DEFAULT 'A'
);

CREATE INDEX IF NOT EXISTS ix_operacao_assistida_treinamentos_cliente ON plantaopro.operacao_assistida_treinamentos(cliente_id, realizado_em desc, reg_status);

ALTER TABLE plantaopro.operacao_assistida_clientes ADD COLUMN IF NOT EXISTS risco varchar(20) NOT NULL DEFAULT 'BAIXO';
ALTER TABLE plantaopro.operacao_assistida_checklist ADD COLUMN IF NOT EXISTS justificativa text;
ALTER TABLE plantaopro.operacao_assistida_ocorrencias ADD COLUMN IF NOT EXISTS solucao text;
ALTER TABLE plantaopro.operacao_assistida_treinamentos ADD COLUMN IF NOT EXISTS observacoes text;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'ck_operacao_assistida_ocorrencias_prioridade'
    ) THEN
        ALTER TABLE plantaopro.operacao_assistida_ocorrencias
        ADD CONSTRAINT ck_operacao_assistida_ocorrencias_prioridade
        CHECK (prioridade IN ('BAIXA','MEDIA','ALTA','CRITICA'));
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'ck_operacao_assistida_ocorrencias_status'
    ) THEN
        ALTER TABLE plantaopro.operacao_assistida_ocorrencias
        ADD CONSTRAINT ck_operacao_assistida_ocorrencias_status
        CHECK (status IN ('ABERTA','EM_ANALISE','RESOLVIDA','CANCELADA'));
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_operacao_assistida_checklist_cliente_ordem ON plantaopro.operacao_assistida_checklist(cliente_id, ordem) WHERE reg_status = 'A';
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_checklist_id_status ON plantaopro.operacao_assistida_checklist(id, reg_status);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'ck_operacao_assistida_ocorrencias_tipo'
    ) THEN
        ALTER TABLE plantaopro.operacao_assistida_ocorrencias
        ADD CONSTRAINT ck_operacao_assistida_ocorrencias_tipo
        CHECK (tipo IN ('BUG','DUVIDA','MELHORIA','TREINAMENTO','CONFIGURACAO'));
    END IF;
END $$;
