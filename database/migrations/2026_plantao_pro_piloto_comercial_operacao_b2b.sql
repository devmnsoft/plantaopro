-- PlantãoPro — Piloto comercial, Customer Success, adoção, operação B2B e evolução funcional
-- Script incremental idempotente. Não remove dados existentes.
CREATE SCHEMA IF NOT EXISTS plantaopro;
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE TABLE IF NOT EXISTS plantaopro.piloto_programas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.piloto_programas ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.piloto_programas ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.piloto_programas ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.piloto_programas ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_piloto_programas_tenant_id ON plantaopro.piloto_programas(tenant_id);
CREATE INDEX IF NOT EXISTS ix_piloto_programas_cliente_id ON plantaopro.piloto_programas(cliente_id);
CREATE INDEX IF NOT EXISTS ix_piloto_programas_status ON plantaopro.piloto_programas(status);
CREATE INDEX IF NOT EXISTS ix_piloto_programas_reg_date ON plantaopro.piloto_programas(reg_date);
CREATE INDEX IF NOT EXISTS ix_piloto_programas_programa_id ON plantaopro.piloto_programas(programa_id);
CREATE INDEX IF NOT EXISTS ix_piloto_programas_conta_id ON plantaopro.piloto_programas(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_piloto_programas_reg_status' AND conrelid = 'plantaopro.piloto_programas'::regclass) THEN
        ALTER TABLE plantaopro.piloto_programas ADD CONSTRAINT ck_piloto_programas_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.piloto_clientes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.piloto_clientes ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.piloto_clientes ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.piloto_clientes ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.piloto_clientes ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_piloto_clientes_tenant_id ON plantaopro.piloto_clientes(tenant_id);
CREATE INDEX IF NOT EXISTS ix_piloto_clientes_cliente_id ON plantaopro.piloto_clientes(cliente_id);
CREATE INDEX IF NOT EXISTS ix_piloto_clientes_status ON plantaopro.piloto_clientes(status);
CREATE INDEX IF NOT EXISTS ix_piloto_clientes_reg_date ON plantaopro.piloto_clientes(reg_date);
CREATE INDEX IF NOT EXISTS ix_piloto_clientes_programa_id ON plantaopro.piloto_clientes(programa_id);
CREATE INDEX IF NOT EXISTS ix_piloto_clientes_conta_id ON plantaopro.piloto_clientes(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_piloto_clientes_reg_status' AND conrelid = 'plantaopro.piloto_clientes'::regclass) THEN
        ALTER TABLE plantaopro.piloto_clientes ADD CONSTRAINT ck_piloto_clientes_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.piloto_etapas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.piloto_etapas ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.piloto_etapas ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.piloto_etapas ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.piloto_etapas ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_piloto_etapas_tenant_id ON plantaopro.piloto_etapas(tenant_id);
CREATE INDEX IF NOT EXISTS ix_piloto_etapas_cliente_id ON plantaopro.piloto_etapas(cliente_id);
CREATE INDEX IF NOT EXISTS ix_piloto_etapas_status ON plantaopro.piloto_etapas(status);
CREATE INDEX IF NOT EXISTS ix_piloto_etapas_reg_date ON plantaopro.piloto_etapas(reg_date);
CREATE INDEX IF NOT EXISTS ix_piloto_etapas_programa_id ON plantaopro.piloto_etapas(programa_id);
CREATE INDEX IF NOT EXISTS ix_piloto_etapas_conta_id ON plantaopro.piloto_etapas(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_piloto_etapas_reg_status' AND conrelid = 'plantaopro.piloto_etapas'::regclass) THEN
        ALTER TABLE plantaopro.piloto_etapas ADD CONSTRAINT ck_piloto_etapas_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.piloto_feedbacks (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.piloto_feedbacks ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.piloto_feedbacks ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.piloto_feedbacks ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.piloto_feedbacks ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_piloto_feedbacks_tenant_id ON plantaopro.piloto_feedbacks(tenant_id);
CREATE INDEX IF NOT EXISTS ix_piloto_feedbacks_cliente_id ON plantaopro.piloto_feedbacks(cliente_id);
CREATE INDEX IF NOT EXISTS ix_piloto_feedbacks_status ON plantaopro.piloto_feedbacks(status);
CREATE INDEX IF NOT EXISTS ix_piloto_feedbacks_reg_date ON plantaopro.piloto_feedbacks(reg_date);
CREATE INDEX IF NOT EXISTS ix_piloto_feedbacks_programa_id ON plantaopro.piloto_feedbacks(programa_id);
CREATE INDEX IF NOT EXISTS ix_piloto_feedbacks_conta_id ON plantaopro.piloto_feedbacks(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_piloto_feedbacks_reg_status' AND conrelid = 'plantaopro.piloto_feedbacks'::regclass) THEN
        ALTER TABLE plantaopro.piloto_feedbacks ADD CONSTRAINT ck_piloto_feedbacks_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.piloto_bugs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.piloto_bugs ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.piloto_bugs ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.piloto_bugs ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.piloto_bugs ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_piloto_bugs_tenant_id ON plantaopro.piloto_bugs(tenant_id);
CREATE INDEX IF NOT EXISTS ix_piloto_bugs_cliente_id ON plantaopro.piloto_bugs(cliente_id);
CREATE INDEX IF NOT EXISTS ix_piloto_bugs_status ON plantaopro.piloto_bugs(status);
CREATE INDEX IF NOT EXISTS ix_piloto_bugs_reg_date ON plantaopro.piloto_bugs(reg_date);
CREATE INDEX IF NOT EXISTS ix_piloto_bugs_programa_id ON plantaopro.piloto_bugs(programa_id);
CREATE INDEX IF NOT EXISTS ix_piloto_bugs_conta_id ON plantaopro.piloto_bugs(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_piloto_bugs_reg_status' AND conrelid = 'plantaopro.piloto_bugs'::regclass) THEN
        ALTER TABLE plantaopro.piloto_bugs ADD CONSTRAINT ck_piloto_bugs_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.piloto_metricas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.piloto_metricas ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.piloto_metricas ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.piloto_metricas ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.piloto_metricas ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_piloto_metricas_tenant_id ON plantaopro.piloto_metricas(tenant_id);
CREATE INDEX IF NOT EXISTS ix_piloto_metricas_cliente_id ON plantaopro.piloto_metricas(cliente_id);
CREATE INDEX IF NOT EXISTS ix_piloto_metricas_status ON plantaopro.piloto_metricas(status);
CREATE INDEX IF NOT EXISTS ix_piloto_metricas_reg_date ON plantaopro.piloto_metricas(reg_date);
CREATE INDEX IF NOT EXISTS ix_piloto_metricas_programa_id ON plantaopro.piloto_metricas(programa_id);
CREATE INDEX IF NOT EXISTS ix_piloto_metricas_conta_id ON plantaopro.piloto_metricas(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_piloto_metricas_reg_status' AND conrelid = 'plantaopro.piloto_metricas'::regclass) THEN
        ALTER TABLE plantaopro.piloto_metricas ADD CONSTRAINT ck_piloto_metricas_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.piloto_reunioes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.piloto_reunioes ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.piloto_reunioes ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.piloto_reunioes ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.piloto_reunioes ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_piloto_reunioes_tenant_id ON plantaopro.piloto_reunioes(tenant_id);
CREATE INDEX IF NOT EXISTS ix_piloto_reunioes_cliente_id ON plantaopro.piloto_reunioes(cliente_id);
CREATE INDEX IF NOT EXISTS ix_piloto_reunioes_status ON plantaopro.piloto_reunioes(status);
CREATE INDEX IF NOT EXISTS ix_piloto_reunioes_reg_date ON plantaopro.piloto_reunioes(reg_date);
CREATE INDEX IF NOT EXISTS ix_piloto_reunioes_programa_id ON plantaopro.piloto_reunioes(programa_id);
CREATE INDEX IF NOT EXISTS ix_piloto_reunioes_conta_id ON plantaopro.piloto_reunioes(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_piloto_reunioes_reg_status' AND conrelid = 'plantaopro.piloto_reunioes'::regclass) THEN
        ALTER TABLE plantaopro.piloto_reunioes ADD CONSTRAINT ck_piloto_reunioes_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.piloto_decisoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.piloto_decisoes ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.piloto_decisoes ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.piloto_decisoes ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.piloto_decisoes ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_piloto_decisoes_tenant_id ON plantaopro.piloto_decisoes(tenant_id);
CREATE INDEX IF NOT EXISTS ix_piloto_decisoes_cliente_id ON plantaopro.piloto_decisoes(cliente_id);
CREATE INDEX IF NOT EXISTS ix_piloto_decisoes_status ON plantaopro.piloto_decisoes(status);
CREATE INDEX IF NOT EXISTS ix_piloto_decisoes_reg_date ON plantaopro.piloto_decisoes(reg_date);
CREATE INDEX IF NOT EXISTS ix_piloto_decisoes_programa_id ON plantaopro.piloto_decisoes(programa_id);
CREATE INDEX IF NOT EXISTS ix_piloto_decisoes_conta_id ON plantaopro.piloto_decisoes(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_piloto_decisoes_reg_status' AND conrelid = 'plantaopro.piloto_decisoes'::regclass) THEN
        ALTER TABLE plantaopro.piloto_decisoes ADD CONSTRAINT ck_piloto_decisoes_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.piloto_checklists (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.piloto_checklists ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.piloto_checklists ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.piloto_checklists ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.piloto_checklists ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_piloto_checklists_tenant_id ON plantaopro.piloto_checklists(tenant_id);
CREATE INDEX IF NOT EXISTS ix_piloto_checklists_cliente_id ON plantaopro.piloto_checklists(cliente_id);
CREATE INDEX IF NOT EXISTS ix_piloto_checklists_status ON plantaopro.piloto_checklists(status);
CREATE INDEX IF NOT EXISTS ix_piloto_checklists_reg_date ON plantaopro.piloto_checklists(reg_date);
CREATE INDEX IF NOT EXISTS ix_piloto_checklists_programa_id ON plantaopro.piloto_checklists(programa_id);
CREATE INDEX IF NOT EXISTS ix_piloto_checklists_conta_id ON plantaopro.piloto_checklists(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_piloto_checklists_reg_status' AND conrelid = 'plantaopro.piloto_checklists'::regclass) THEN
        ALTER TABLE plantaopro.piloto_checklists ADD CONSTRAINT ck_piloto_checklists_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.piloto_criterios_sucesso (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.piloto_criterios_sucesso ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.piloto_criterios_sucesso ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.piloto_criterios_sucesso ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.piloto_criterios_sucesso ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_piloto_criterios_sucesso_tenant_id ON plantaopro.piloto_criterios_sucesso(tenant_id);
CREATE INDEX IF NOT EXISTS ix_piloto_criterios_sucesso_cliente_id ON plantaopro.piloto_criterios_sucesso(cliente_id);
CREATE INDEX IF NOT EXISTS ix_piloto_criterios_sucesso_status ON plantaopro.piloto_criterios_sucesso(status);
CREATE INDEX IF NOT EXISTS ix_piloto_criterios_sucesso_reg_date ON plantaopro.piloto_criterios_sucesso(reg_date);
CREATE INDEX IF NOT EXISTS ix_piloto_criterios_sucesso_programa_id ON plantaopro.piloto_criterios_sucesso(programa_id);
CREATE INDEX IF NOT EXISTS ix_piloto_criterios_sucesso_conta_id ON plantaopro.piloto_criterios_sucesso(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_piloto_criterios_sucesso_reg_status' AND conrelid = 'plantaopro.piloto_criterios_sucesso'::regclass) THEN
        ALTER TABLE plantaopro.piloto_criterios_sucesso ADD CONSTRAINT ck_piloto_criterios_sucesso_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.cs_carteiras (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.cs_carteiras ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.cs_carteiras ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.cs_carteiras ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.cs_carteiras ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_cs_carteiras_tenant_id ON plantaopro.cs_carteiras(tenant_id);
CREATE INDEX IF NOT EXISTS ix_cs_carteiras_cliente_id ON plantaopro.cs_carteiras(cliente_id);
CREATE INDEX IF NOT EXISTS ix_cs_carteiras_status ON plantaopro.cs_carteiras(status);
CREATE INDEX IF NOT EXISTS ix_cs_carteiras_reg_date ON plantaopro.cs_carteiras(reg_date);
CREATE INDEX IF NOT EXISTS ix_cs_carteiras_programa_id ON plantaopro.cs_carteiras(programa_id);
CREATE INDEX IF NOT EXISTS ix_cs_carteiras_conta_id ON plantaopro.cs_carteiras(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_cs_carteiras_reg_status' AND conrelid = 'plantaopro.cs_carteiras'::regclass) THEN
        ALTER TABLE plantaopro.cs_carteiras ADD CONSTRAINT ck_cs_carteiras_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.cs_contas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.cs_contas ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.cs_contas ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.cs_contas ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.cs_contas ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_cs_contas_tenant_id ON plantaopro.cs_contas(tenant_id);
CREATE INDEX IF NOT EXISTS ix_cs_contas_cliente_id ON plantaopro.cs_contas(cliente_id);
CREATE INDEX IF NOT EXISTS ix_cs_contas_status ON plantaopro.cs_contas(status);
CREATE INDEX IF NOT EXISTS ix_cs_contas_reg_date ON plantaopro.cs_contas(reg_date);
CREATE INDEX IF NOT EXISTS ix_cs_contas_programa_id ON plantaopro.cs_contas(programa_id);
CREATE INDEX IF NOT EXISTS ix_cs_contas_conta_id ON plantaopro.cs_contas(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_cs_contas_reg_status' AND conrelid = 'plantaopro.cs_contas'::regclass) THEN
        ALTER TABLE plantaopro.cs_contas ADD CONSTRAINT ck_cs_contas_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.cs_interacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.cs_interacoes ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.cs_interacoes ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.cs_interacoes ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.cs_interacoes ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_cs_interacoes_tenant_id ON plantaopro.cs_interacoes(tenant_id);
CREATE INDEX IF NOT EXISTS ix_cs_interacoes_cliente_id ON plantaopro.cs_interacoes(cliente_id);
CREATE INDEX IF NOT EXISTS ix_cs_interacoes_status ON plantaopro.cs_interacoes(status);
CREATE INDEX IF NOT EXISTS ix_cs_interacoes_reg_date ON plantaopro.cs_interacoes(reg_date);
CREATE INDEX IF NOT EXISTS ix_cs_interacoes_programa_id ON plantaopro.cs_interacoes(programa_id);
CREATE INDEX IF NOT EXISTS ix_cs_interacoes_conta_id ON plantaopro.cs_interacoes(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_cs_interacoes_reg_status' AND conrelid = 'plantaopro.cs_interacoes'::regclass) THEN
        ALTER TABLE plantaopro.cs_interacoes ADD CONSTRAINT ck_cs_interacoes_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.cs_planos_acao (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.cs_planos_acao ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.cs_planos_acao ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.cs_planos_acao ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.cs_planos_acao ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_cs_planos_acao_tenant_id ON plantaopro.cs_planos_acao(tenant_id);
CREATE INDEX IF NOT EXISTS ix_cs_planos_acao_cliente_id ON plantaopro.cs_planos_acao(cliente_id);
CREATE INDEX IF NOT EXISTS ix_cs_planos_acao_status ON plantaopro.cs_planos_acao(status);
CREATE INDEX IF NOT EXISTS ix_cs_planos_acao_reg_date ON plantaopro.cs_planos_acao(reg_date);
CREATE INDEX IF NOT EXISTS ix_cs_planos_acao_programa_id ON plantaopro.cs_planos_acao(programa_id);
CREATE INDEX IF NOT EXISTS ix_cs_planos_acao_conta_id ON plantaopro.cs_planos_acao(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_cs_planos_acao_reg_status' AND conrelid = 'plantaopro.cs_planos_acao'::regclass) THEN
        ALTER TABLE plantaopro.cs_planos_acao ADD CONSTRAINT ck_cs_planos_acao_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.cs_tarefas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.cs_tarefas ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.cs_tarefas ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.cs_tarefas ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.cs_tarefas ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_cs_tarefas_tenant_id ON plantaopro.cs_tarefas(tenant_id);
CREATE INDEX IF NOT EXISTS ix_cs_tarefas_cliente_id ON plantaopro.cs_tarefas(cliente_id);
CREATE INDEX IF NOT EXISTS ix_cs_tarefas_status ON plantaopro.cs_tarefas(status);
CREATE INDEX IF NOT EXISTS ix_cs_tarefas_reg_date ON plantaopro.cs_tarefas(reg_date);
CREATE INDEX IF NOT EXISTS ix_cs_tarefas_programa_id ON plantaopro.cs_tarefas(programa_id);
CREATE INDEX IF NOT EXISTS ix_cs_tarefas_conta_id ON plantaopro.cs_tarefas(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_cs_tarefas_reg_status' AND conrelid = 'plantaopro.cs_tarefas'::regclass) THEN
        ALTER TABLE plantaopro.cs_tarefas ADD CONSTRAINT ck_cs_tarefas_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.cs_riscos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.cs_riscos ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.cs_riscos ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.cs_riscos ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.cs_riscos ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_cs_riscos_tenant_id ON plantaopro.cs_riscos(tenant_id);
CREATE INDEX IF NOT EXISTS ix_cs_riscos_cliente_id ON plantaopro.cs_riscos(cliente_id);
CREATE INDEX IF NOT EXISTS ix_cs_riscos_status ON plantaopro.cs_riscos(status);
CREATE INDEX IF NOT EXISTS ix_cs_riscos_reg_date ON plantaopro.cs_riscos(reg_date);
CREATE INDEX IF NOT EXISTS ix_cs_riscos_programa_id ON plantaopro.cs_riscos(programa_id);
CREATE INDEX IF NOT EXISTS ix_cs_riscos_conta_id ON plantaopro.cs_riscos(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_cs_riscos_reg_status' AND conrelid = 'plantaopro.cs_riscos'::regclass) THEN
        ALTER TABLE plantaopro.cs_riscos ADD CONSTRAINT ck_cs_riscos_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.cs_oportunidades (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.cs_oportunidades ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.cs_oportunidades ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.cs_oportunidades ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.cs_oportunidades ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_cs_oportunidades_tenant_id ON plantaopro.cs_oportunidades(tenant_id);
CREATE INDEX IF NOT EXISTS ix_cs_oportunidades_cliente_id ON plantaopro.cs_oportunidades(cliente_id);
CREATE INDEX IF NOT EXISTS ix_cs_oportunidades_status ON plantaopro.cs_oportunidades(status);
CREATE INDEX IF NOT EXISTS ix_cs_oportunidades_reg_date ON plantaopro.cs_oportunidades(reg_date);
CREATE INDEX IF NOT EXISTS ix_cs_oportunidades_programa_id ON plantaopro.cs_oportunidades(programa_id);
CREATE INDEX IF NOT EXISTS ix_cs_oportunidades_conta_id ON plantaopro.cs_oportunidades(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_cs_oportunidades_reg_status' AND conrelid = 'plantaopro.cs_oportunidades'::regclass) THEN
        ALTER TABLE plantaopro.cs_oportunidades ADD CONSTRAINT ck_cs_oportunidades_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.cs_health_score (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.cs_health_score ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.cs_health_score ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.cs_health_score ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.cs_health_score ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_cs_health_score_tenant_id ON plantaopro.cs_health_score(tenant_id);
CREATE INDEX IF NOT EXISTS ix_cs_health_score_cliente_id ON plantaopro.cs_health_score(cliente_id);
CREATE INDEX IF NOT EXISTS ix_cs_health_score_status ON plantaopro.cs_health_score(status);
CREATE INDEX IF NOT EXISTS ix_cs_health_score_reg_date ON plantaopro.cs_health_score(reg_date);
CREATE INDEX IF NOT EXISTS ix_cs_health_score_programa_id ON plantaopro.cs_health_score(programa_id);
CREATE INDEX IF NOT EXISTS ix_cs_health_score_conta_id ON plantaopro.cs_health_score(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_cs_health_score_reg_status' AND conrelid = 'plantaopro.cs_health_score'::regclass) THEN
        ALTER TABLE plantaopro.cs_health_score ADD CONSTRAINT ck_cs_health_score_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.cs_nps (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.cs_nps ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.cs_nps ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.cs_nps ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.cs_nps ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_cs_nps_tenant_id ON plantaopro.cs_nps(tenant_id);
CREATE INDEX IF NOT EXISTS ix_cs_nps_cliente_id ON plantaopro.cs_nps(cliente_id);
CREATE INDEX IF NOT EXISTS ix_cs_nps_status ON plantaopro.cs_nps(status);
CREATE INDEX IF NOT EXISTS ix_cs_nps_reg_date ON plantaopro.cs_nps(reg_date);
CREATE INDEX IF NOT EXISTS ix_cs_nps_programa_id ON plantaopro.cs_nps(programa_id);
CREATE INDEX IF NOT EXISTS ix_cs_nps_conta_id ON plantaopro.cs_nps(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_cs_nps_reg_status' AND conrelid = 'plantaopro.cs_nps'::regclass) THEN
        ALTER TABLE plantaopro.cs_nps ADD CONSTRAINT ck_cs_nps_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.cs_qbr (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.cs_qbr ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.cs_qbr ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.cs_qbr ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.cs_qbr ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_cs_qbr_tenant_id ON plantaopro.cs_qbr(tenant_id);
CREATE INDEX IF NOT EXISTS ix_cs_qbr_cliente_id ON plantaopro.cs_qbr(cliente_id);
CREATE INDEX IF NOT EXISTS ix_cs_qbr_status ON plantaopro.cs_qbr(status);
CREATE INDEX IF NOT EXISTS ix_cs_qbr_reg_date ON plantaopro.cs_qbr(reg_date);
CREATE INDEX IF NOT EXISTS ix_cs_qbr_programa_id ON plantaopro.cs_qbr(programa_id);
CREATE INDEX IF NOT EXISTS ix_cs_qbr_conta_id ON plantaopro.cs_qbr(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_cs_qbr_reg_status' AND conrelid = 'plantaopro.cs_qbr'::regclass) THEN
        ALTER TABLE plantaopro.cs_qbr ADD CONSTRAINT ck_cs_qbr_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.cs_reunioes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.cs_reunioes ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.cs_reunioes ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.cs_reunioes ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.cs_reunioes ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_cs_reunioes_tenant_id ON plantaopro.cs_reunioes(tenant_id);
CREATE INDEX IF NOT EXISTS ix_cs_reunioes_cliente_id ON plantaopro.cs_reunioes(cliente_id);
CREATE INDEX IF NOT EXISTS ix_cs_reunioes_status ON plantaopro.cs_reunioes(status);
CREATE INDEX IF NOT EXISTS ix_cs_reunioes_reg_date ON plantaopro.cs_reunioes(reg_date);
CREATE INDEX IF NOT EXISTS ix_cs_reunioes_programa_id ON plantaopro.cs_reunioes(programa_id);
CREATE INDEX IF NOT EXISTS ix_cs_reunioes_conta_id ON plantaopro.cs_reunioes(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_cs_reunioes_reg_status' AND conrelid = 'plantaopro.cs_reunioes'::regclass) THEN
        ALTER TABLE plantaopro.cs_reunioes ADD CONSTRAINT ck_cs_reunioes_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.cs_playbooks (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.cs_playbooks ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.cs_playbooks ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.cs_playbooks ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.cs_playbooks ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_cs_playbooks_tenant_id ON plantaopro.cs_playbooks(tenant_id);
CREATE INDEX IF NOT EXISTS ix_cs_playbooks_cliente_id ON plantaopro.cs_playbooks(cliente_id);
CREATE INDEX IF NOT EXISTS ix_cs_playbooks_status ON plantaopro.cs_playbooks(status);
CREATE INDEX IF NOT EXISTS ix_cs_playbooks_reg_date ON plantaopro.cs_playbooks(reg_date);
CREATE INDEX IF NOT EXISTS ix_cs_playbooks_programa_id ON plantaopro.cs_playbooks(programa_id);
CREATE INDEX IF NOT EXISTS ix_cs_playbooks_conta_id ON plantaopro.cs_playbooks(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_cs_playbooks_reg_status' AND conrelid = 'plantaopro.cs_playbooks'::regclass) THEN
        ALTER TABLE plantaopro.cs_playbooks ADD CONSTRAINT ck_cs_playbooks_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.cs_marcos_sucesso (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.cs_marcos_sucesso ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.cs_marcos_sucesso ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.cs_marcos_sucesso ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.cs_marcos_sucesso ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_cs_marcos_sucesso_tenant_id ON plantaopro.cs_marcos_sucesso(tenant_id);
CREATE INDEX IF NOT EXISTS ix_cs_marcos_sucesso_cliente_id ON plantaopro.cs_marcos_sucesso(cliente_id);
CREATE INDEX IF NOT EXISTS ix_cs_marcos_sucesso_status ON plantaopro.cs_marcos_sucesso(status);
CREATE INDEX IF NOT EXISTS ix_cs_marcos_sucesso_reg_date ON plantaopro.cs_marcos_sucesso(reg_date);
CREATE INDEX IF NOT EXISTS ix_cs_marcos_sucesso_programa_id ON plantaopro.cs_marcos_sucesso(programa_id);
CREATE INDEX IF NOT EXISTS ix_cs_marcos_sucesso_conta_id ON plantaopro.cs_marcos_sucesso(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_cs_marcos_sucesso_reg_status' AND conrelid = 'plantaopro.cs_marcos_sucesso'::regclass) THEN
        ALTER TABLE plantaopro.cs_marcos_sucesso ADD CONSTRAINT ck_cs_marcos_sucesso_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.adocao_eventos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.adocao_eventos ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.adocao_eventos ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.adocao_eventos ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.adocao_eventos ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_adocao_eventos_tenant_id ON plantaopro.adocao_eventos(tenant_id);
CREATE INDEX IF NOT EXISTS ix_adocao_eventos_cliente_id ON plantaopro.adocao_eventos(cliente_id);
CREATE INDEX IF NOT EXISTS ix_adocao_eventos_status ON plantaopro.adocao_eventos(status);
CREATE INDEX IF NOT EXISTS ix_adocao_eventos_reg_date ON plantaopro.adocao_eventos(reg_date);
CREATE INDEX IF NOT EXISTS ix_adocao_eventos_programa_id ON plantaopro.adocao_eventos(programa_id);
CREATE INDEX IF NOT EXISTS ix_adocao_eventos_conta_id ON plantaopro.adocao_eventos(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_adocao_eventos_reg_status' AND conrelid = 'plantaopro.adocao_eventos'::regclass) THEN
        ALTER TABLE plantaopro.adocao_eventos ADD CONSTRAINT ck_adocao_eventos_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.adocao_metricas_tenant (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.adocao_metricas_tenant ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.adocao_metricas_tenant ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.adocao_metricas_tenant ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.adocao_metricas_tenant ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_adocao_metricas_tenant_tenant_id ON plantaopro.adocao_metricas_tenant(tenant_id);
CREATE INDEX IF NOT EXISTS ix_adocao_metricas_tenant_cliente_id ON plantaopro.adocao_metricas_tenant(cliente_id);
CREATE INDEX IF NOT EXISTS ix_adocao_metricas_tenant_status ON plantaopro.adocao_metricas_tenant(status);
CREATE INDEX IF NOT EXISTS ix_adocao_metricas_tenant_reg_date ON plantaopro.adocao_metricas_tenant(reg_date);
CREATE INDEX IF NOT EXISTS ix_adocao_metricas_tenant_programa_id ON plantaopro.adocao_metricas_tenant(programa_id);
CREATE INDEX IF NOT EXISTS ix_adocao_metricas_tenant_conta_id ON plantaopro.adocao_metricas_tenant(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_adocao_metricas_tenant_reg_status' AND conrelid = 'plantaopro.adocao_metricas_tenant'::regclass) THEN
        ALTER TABLE plantaopro.adocao_metricas_tenant ADD CONSTRAINT ck_adocao_metricas_tenant_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.adocao_metricas_usuario (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.adocao_metricas_usuario ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.adocao_metricas_usuario ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.adocao_metricas_usuario ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.adocao_metricas_usuario ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_adocao_metricas_usuario_tenant_id ON plantaopro.adocao_metricas_usuario(tenant_id);
CREATE INDEX IF NOT EXISTS ix_adocao_metricas_usuario_cliente_id ON plantaopro.adocao_metricas_usuario(cliente_id);
CREATE INDEX IF NOT EXISTS ix_adocao_metricas_usuario_status ON plantaopro.adocao_metricas_usuario(status);
CREATE INDEX IF NOT EXISTS ix_adocao_metricas_usuario_reg_date ON plantaopro.adocao_metricas_usuario(reg_date);
CREATE INDEX IF NOT EXISTS ix_adocao_metricas_usuario_programa_id ON plantaopro.adocao_metricas_usuario(programa_id);
CREATE INDEX IF NOT EXISTS ix_adocao_metricas_usuario_conta_id ON plantaopro.adocao_metricas_usuario(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_adocao_metricas_usuario_reg_status' AND conrelid = 'plantaopro.adocao_metricas_usuario'::regclass) THEN
        ALTER TABLE plantaopro.adocao_metricas_usuario ADD CONSTRAINT ck_adocao_metricas_usuario_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.adocao_funcionalidades (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.adocao_funcionalidades ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.adocao_funcionalidades ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.adocao_funcionalidades ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.adocao_funcionalidades ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_adocao_funcionalidades_tenant_id ON plantaopro.adocao_funcionalidades(tenant_id);
CREATE INDEX IF NOT EXISTS ix_adocao_funcionalidades_cliente_id ON plantaopro.adocao_funcionalidades(cliente_id);
CREATE INDEX IF NOT EXISTS ix_adocao_funcionalidades_status ON plantaopro.adocao_funcionalidades(status);
CREATE INDEX IF NOT EXISTS ix_adocao_funcionalidades_reg_date ON plantaopro.adocao_funcionalidades(reg_date);
CREATE INDEX IF NOT EXISTS ix_adocao_funcionalidades_programa_id ON plantaopro.adocao_funcionalidades(programa_id);
CREATE INDEX IF NOT EXISTS ix_adocao_funcionalidades_conta_id ON plantaopro.adocao_funcionalidades(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_adocao_funcionalidades_reg_status' AND conrelid = 'plantaopro.adocao_funcionalidades'::regclass) THEN
        ALTER TABLE plantaopro.adocao_funcionalidades ADD CONSTRAINT ck_adocao_funcionalidades_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.adocao_funil_onboarding (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.adocao_funil_onboarding ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.adocao_funil_onboarding ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.adocao_funil_onboarding ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.adocao_funil_onboarding ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_adocao_funil_onboarding_tenant_id ON plantaopro.adocao_funil_onboarding(tenant_id);
CREATE INDEX IF NOT EXISTS ix_adocao_funil_onboarding_cliente_id ON plantaopro.adocao_funil_onboarding(cliente_id);
CREATE INDEX IF NOT EXISTS ix_adocao_funil_onboarding_status ON plantaopro.adocao_funil_onboarding(status);
CREATE INDEX IF NOT EXISTS ix_adocao_funil_onboarding_reg_date ON plantaopro.adocao_funil_onboarding(reg_date);
CREATE INDEX IF NOT EXISTS ix_adocao_funil_onboarding_programa_id ON plantaopro.adocao_funil_onboarding(programa_id);
CREATE INDEX IF NOT EXISTS ix_adocao_funil_onboarding_conta_id ON plantaopro.adocao_funil_onboarding(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_adocao_funil_onboarding_reg_status' AND conrelid = 'plantaopro.adocao_funil_onboarding'::regclass) THEN
        ALTER TABLE plantaopro.adocao_funil_onboarding ADD CONSTRAINT ck_adocao_funil_onboarding_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.adocao_alertas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.adocao_alertas ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.adocao_alertas ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.adocao_alertas ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.adocao_alertas ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_adocao_alertas_tenant_id ON plantaopro.adocao_alertas(tenant_id);
CREATE INDEX IF NOT EXISTS ix_adocao_alertas_cliente_id ON plantaopro.adocao_alertas(cliente_id);
CREATE INDEX IF NOT EXISTS ix_adocao_alertas_status ON plantaopro.adocao_alertas(status);
CREATE INDEX IF NOT EXISTS ix_adocao_alertas_reg_date ON plantaopro.adocao_alertas(reg_date);
CREATE INDEX IF NOT EXISTS ix_adocao_alertas_programa_id ON plantaopro.adocao_alertas(programa_id);
CREATE INDEX IF NOT EXISTS ix_adocao_alertas_conta_id ON plantaopro.adocao_alertas(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_adocao_alertas_reg_status' AND conrelid = 'plantaopro.adocao_alertas'::regclass) THEN
        ALTER TABLE plantaopro.adocao_alertas ADD CONSTRAINT ck_adocao_alertas_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.adocao_recomendacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.adocao_recomendacoes ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.adocao_recomendacoes ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.adocao_recomendacoes ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.adocao_recomendacoes ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_adocao_recomendacoes_tenant_id ON plantaopro.adocao_recomendacoes(tenant_id);
CREATE INDEX IF NOT EXISTS ix_adocao_recomendacoes_cliente_id ON plantaopro.adocao_recomendacoes(cliente_id);
CREATE INDEX IF NOT EXISTS ix_adocao_recomendacoes_status ON plantaopro.adocao_recomendacoes(status);
CREATE INDEX IF NOT EXISTS ix_adocao_recomendacoes_reg_date ON plantaopro.adocao_recomendacoes(reg_date);
CREATE INDEX IF NOT EXISTS ix_adocao_recomendacoes_programa_id ON plantaopro.adocao_recomendacoes(programa_id);
CREATE INDEX IF NOT EXISTS ix_adocao_recomendacoes_conta_id ON plantaopro.adocao_recomendacoes(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_adocao_recomendacoes_reg_status' AND conrelid = 'plantaopro.adocao_recomendacoes'::regclass) THEN
        ALTER TABLE plantaopro.adocao_recomendacoes ADD CONSTRAINT ck_adocao_recomendacoes_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.churn_riscos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.churn_riscos ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.churn_riscos ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.churn_riscos ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.churn_riscos ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_churn_riscos_tenant_id ON plantaopro.churn_riscos(tenant_id);
CREATE INDEX IF NOT EXISTS ix_churn_riscos_cliente_id ON plantaopro.churn_riscos(cliente_id);
CREATE INDEX IF NOT EXISTS ix_churn_riscos_status ON plantaopro.churn_riscos(status);
CREATE INDEX IF NOT EXISTS ix_churn_riscos_reg_date ON plantaopro.churn_riscos(reg_date);
CREATE INDEX IF NOT EXISTS ix_churn_riscos_programa_id ON plantaopro.churn_riscos(programa_id);
CREATE INDEX IF NOT EXISTS ix_churn_riscos_conta_id ON plantaopro.churn_riscos(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_churn_riscos_reg_status' AND conrelid = 'plantaopro.churn_riscos'::regclass) THEN
        ALTER TABLE plantaopro.churn_riscos ADD CONSTRAINT ck_churn_riscos_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.churn_motivos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.churn_motivos ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.churn_motivos ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.churn_motivos ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.churn_motivos ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_churn_motivos_tenant_id ON plantaopro.churn_motivos(tenant_id);
CREATE INDEX IF NOT EXISTS ix_churn_motivos_cliente_id ON plantaopro.churn_motivos(cliente_id);
CREATE INDEX IF NOT EXISTS ix_churn_motivos_status ON plantaopro.churn_motivos(status);
CREATE INDEX IF NOT EXISTS ix_churn_motivos_reg_date ON plantaopro.churn_motivos(reg_date);
CREATE INDEX IF NOT EXISTS ix_churn_motivos_programa_id ON plantaopro.churn_motivos(programa_id);
CREATE INDEX IF NOT EXISTS ix_churn_motivos_conta_id ON plantaopro.churn_motivos(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_churn_motivos_reg_status' AND conrelid = 'plantaopro.churn_motivos'::regclass) THEN
        ALTER TABLE plantaopro.churn_motivos ADD CONSTRAINT ck_churn_motivos_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.churn_sinais (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.churn_sinais ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.churn_sinais ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.churn_sinais ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.churn_sinais ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_churn_sinais_tenant_id ON plantaopro.churn_sinais(tenant_id);
CREATE INDEX IF NOT EXISTS ix_churn_sinais_cliente_id ON plantaopro.churn_sinais(cliente_id);
CREATE INDEX IF NOT EXISTS ix_churn_sinais_status ON plantaopro.churn_sinais(status);
CREATE INDEX IF NOT EXISTS ix_churn_sinais_reg_date ON plantaopro.churn_sinais(reg_date);
CREATE INDEX IF NOT EXISTS ix_churn_sinais_programa_id ON plantaopro.churn_sinais(programa_id);
CREATE INDEX IF NOT EXISTS ix_churn_sinais_conta_id ON plantaopro.churn_sinais(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_churn_sinais_reg_status' AND conrelid = 'plantaopro.churn_sinais'::regclass) THEN
        ALTER TABLE plantaopro.churn_sinais ADD CONSTRAINT ck_churn_sinais_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.churn_planos_retencao (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.churn_planos_retencao ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.churn_planos_retencao ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.churn_planos_retencao ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.churn_planos_retencao ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_churn_planos_retencao_tenant_id ON plantaopro.churn_planos_retencao(tenant_id);
CREATE INDEX IF NOT EXISTS ix_churn_planos_retencao_cliente_id ON plantaopro.churn_planos_retencao(cliente_id);
CREATE INDEX IF NOT EXISTS ix_churn_planos_retencao_status ON plantaopro.churn_planos_retencao(status);
CREATE INDEX IF NOT EXISTS ix_churn_planos_retencao_reg_date ON plantaopro.churn_planos_retencao(reg_date);
CREATE INDEX IF NOT EXISTS ix_churn_planos_retencao_programa_id ON plantaopro.churn_planos_retencao(programa_id);
CREATE INDEX IF NOT EXISTS ix_churn_planos_retencao_conta_id ON plantaopro.churn_planos_retencao(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_churn_planos_retencao_reg_status' AND conrelid = 'plantaopro.churn_planos_retencao'::regclass) THEN
        ALTER TABLE plantaopro.churn_planos_retencao ADD CONSTRAINT ck_churn_planos_retencao_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.churn_ofertas_retencao (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.churn_ofertas_retencao ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.churn_ofertas_retencao ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.churn_ofertas_retencao ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.churn_ofertas_retencao ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_churn_ofertas_retencao_tenant_id ON plantaopro.churn_ofertas_retencao(tenant_id);
CREATE INDEX IF NOT EXISTS ix_churn_ofertas_retencao_cliente_id ON plantaopro.churn_ofertas_retencao(cliente_id);
CREATE INDEX IF NOT EXISTS ix_churn_ofertas_retencao_status ON plantaopro.churn_ofertas_retencao(status);
CREATE INDEX IF NOT EXISTS ix_churn_ofertas_retencao_reg_date ON plantaopro.churn_ofertas_retencao(reg_date);
CREATE INDEX IF NOT EXISTS ix_churn_ofertas_retencao_programa_id ON plantaopro.churn_ofertas_retencao(programa_id);
CREATE INDEX IF NOT EXISTS ix_churn_ofertas_retencao_conta_id ON plantaopro.churn_ofertas_retencao(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_churn_ofertas_retencao_reg_status' AND conrelid = 'plantaopro.churn_ofertas_retencao'::regclass) THEN
        ALTER TABLE plantaopro.churn_ofertas_retencao ADD CONSTRAINT ck_churn_ofertas_retencao_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.churn_cancelamentos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.churn_cancelamentos ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.churn_cancelamentos ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.churn_cancelamentos ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.churn_cancelamentos ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_churn_cancelamentos_tenant_id ON plantaopro.churn_cancelamentos(tenant_id);
CREATE INDEX IF NOT EXISTS ix_churn_cancelamentos_cliente_id ON plantaopro.churn_cancelamentos(cliente_id);
CREATE INDEX IF NOT EXISTS ix_churn_cancelamentos_status ON plantaopro.churn_cancelamentos(status);
CREATE INDEX IF NOT EXISTS ix_churn_cancelamentos_reg_date ON plantaopro.churn_cancelamentos(reg_date);
CREATE INDEX IF NOT EXISTS ix_churn_cancelamentos_programa_id ON plantaopro.churn_cancelamentos(programa_id);
CREATE INDEX IF NOT EXISTS ix_churn_cancelamentos_conta_id ON plantaopro.churn_cancelamentos(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_churn_cancelamentos_reg_status' AND conrelid = 'plantaopro.churn_cancelamentos'::regclass) THEN
        ALTER TABLE plantaopro.churn_cancelamentos ADD CONSTRAINT ck_churn_cancelamentos_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.churn_winback (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.churn_winback ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.churn_winback ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.churn_winback ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.churn_winback ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_churn_winback_tenant_id ON plantaopro.churn_winback(tenant_id);
CREATE INDEX IF NOT EXISTS ix_churn_winback_cliente_id ON plantaopro.churn_winback(cliente_id);
CREATE INDEX IF NOT EXISTS ix_churn_winback_status ON plantaopro.churn_winback(status);
CREATE INDEX IF NOT EXISTS ix_churn_winback_reg_date ON plantaopro.churn_winback(reg_date);
CREATE INDEX IF NOT EXISTS ix_churn_winback_programa_id ON plantaopro.churn_winback(programa_id);
CREATE INDEX IF NOT EXISTS ix_churn_winback_conta_id ON plantaopro.churn_winback(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_churn_winback_reg_status' AND conrelid = 'plantaopro.churn_winback'::regclass) THEN
        ALTER TABLE plantaopro.churn_winback ADD CONSTRAINT ck_churn_winback_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.operacao_assistida_planos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.operacao_assistida_planos ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.operacao_assistida_planos ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.operacao_assistida_planos ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.operacao_assistida_planos ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_planos_tenant_id ON plantaopro.operacao_assistida_planos(tenant_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_planos_cliente_id ON plantaopro.operacao_assistida_planos(cliente_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_planos_status ON plantaopro.operacao_assistida_planos(status);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_planos_reg_date ON plantaopro.operacao_assistida_planos(reg_date);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_planos_programa_id ON plantaopro.operacao_assistida_planos(programa_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_planos_conta_id ON plantaopro.operacao_assistida_planos(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_operacao_assistida_planos_reg_status' AND conrelid = 'plantaopro.operacao_assistida_planos'::regclass) THEN
        ALTER TABLE plantaopro.operacao_assistida_planos ADD CONSTRAINT ck_operacao_assistida_planos_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.operacao_assistida_etapas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.operacao_assistida_etapas ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.operacao_assistida_etapas ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.operacao_assistida_etapas ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.operacao_assistida_etapas ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_etapas_tenant_id ON plantaopro.operacao_assistida_etapas(tenant_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_etapas_cliente_id ON plantaopro.operacao_assistida_etapas(cliente_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_etapas_status ON plantaopro.operacao_assistida_etapas(status);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_etapas_reg_date ON plantaopro.operacao_assistida_etapas(reg_date);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_etapas_programa_id ON plantaopro.operacao_assistida_etapas(programa_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_etapas_conta_id ON plantaopro.operacao_assistida_etapas(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_operacao_assistida_etapas_reg_status' AND conrelid = 'plantaopro.operacao_assistida_etapas'::regclass) THEN
        ALTER TABLE plantaopro.operacao_assistida_etapas ADD CONSTRAINT ck_operacao_assistida_etapas_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.operacao_assistida_tarefas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.operacao_assistida_tarefas ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.operacao_assistida_tarefas ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.operacao_assistida_tarefas ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.operacao_assistida_tarefas ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_tarefas_tenant_id ON plantaopro.operacao_assistida_tarefas(tenant_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_tarefas_cliente_id ON plantaopro.operacao_assistida_tarefas(cliente_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_tarefas_status ON plantaopro.operacao_assistida_tarefas(status);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_tarefas_reg_date ON plantaopro.operacao_assistida_tarefas(reg_date);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_tarefas_programa_id ON plantaopro.operacao_assistida_tarefas(programa_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_tarefas_conta_id ON plantaopro.operacao_assistida_tarefas(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_operacao_assistida_tarefas_reg_status' AND conrelid = 'plantaopro.operacao_assistida_tarefas'::regclass) THEN
        ALTER TABLE plantaopro.operacao_assistida_tarefas ADD CONSTRAINT ck_operacao_assistida_tarefas_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.operacao_assistida_reunioes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.operacao_assistida_reunioes ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.operacao_assistida_reunioes ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.operacao_assistida_reunioes ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.operacao_assistida_reunioes ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_reunioes_tenant_id ON plantaopro.operacao_assistida_reunioes(tenant_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_reunioes_cliente_id ON plantaopro.operacao_assistida_reunioes(cliente_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_reunioes_status ON plantaopro.operacao_assistida_reunioes(status);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_reunioes_reg_date ON plantaopro.operacao_assistida_reunioes(reg_date);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_reunioes_programa_id ON plantaopro.operacao_assistida_reunioes(programa_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_reunioes_conta_id ON plantaopro.operacao_assistida_reunioes(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_operacao_assistida_reunioes_reg_status' AND conrelid = 'plantaopro.operacao_assistida_reunioes'::regclass) THEN
        ALTER TABLE plantaopro.operacao_assistida_reunioes ADD CONSTRAINT ck_operacao_assistida_reunioes_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.operacao_assistida_pendencias (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.operacao_assistida_pendencias ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.operacao_assistida_pendencias ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.operacao_assistida_pendencias ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.operacao_assistida_pendencias ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_pendencias_tenant_id ON plantaopro.operacao_assistida_pendencias(tenant_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_pendencias_cliente_id ON plantaopro.operacao_assistida_pendencias(cliente_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_pendencias_status ON plantaopro.operacao_assistida_pendencias(status);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_pendencias_reg_date ON plantaopro.operacao_assistida_pendencias(reg_date);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_pendencias_programa_id ON plantaopro.operacao_assistida_pendencias(programa_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_pendencias_conta_id ON plantaopro.operacao_assistida_pendencias(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_operacao_assistida_pendencias_reg_status' AND conrelid = 'plantaopro.operacao_assistida_pendencias'::regclass) THEN
        ALTER TABLE plantaopro.operacao_assistida_pendencias ADD CONSTRAINT ck_operacao_assistida_pendencias_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.operacao_assistida_evidencias (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.operacao_assistida_evidencias ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.operacao_assistida_evidencias ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.operacao_assistida_evidencias ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.operacao_assistida_evidencias ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_evidencias_tenant_id ON plantaopro.operacao_assistida_evidencias(tenant_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_evidencias_cliente_id ON plantaopro.operacao_assistida_evidencias(cliente_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_evidencias_status ON plantaopro.operacao_assistida_evidencias(status);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_evidencias_reg_date ON plantaopro.operacao_assistida_evidencias(reg_date);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_evidencias_programa_id ON plantaopro.operacao_assistida_evidencias(programa_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_evidencias_conta_id ON plantaopro.operacao_assistida_evidencias(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_operacao_assistida_evidencias_reg_status' AND conrelid = 'plantaopro.operacao_assistida_evidencias'::regclass) THEN
        ALTER TABLE plantaopro.operacao_assistida_evidencias ADD CONSTRAINT ck_operacao_assistida_evidencias_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.operacao_assistida_marcos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.operacao_assistida_marcos ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.operacao_assistida_marcos ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.operacao_assistida_marcos ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.operacao_assistida_marcos ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_marcos_tenant_id ON plantaopro.operacao_assistida_marcos(tenant_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_marcos_cliente_id ON plantaopro.operacao_assistida_marcos(cliente_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_marcos_status ON plantaopro.operacao_assistida_marcos(status);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_marcos_reg_date ON plantaopro.operacao_assistida_marcos(reg_date);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_marcos_programa_id ON plantaopro.operacao_assistida_marcos(programa_id);
CREATE INDEX IF NOT EXISTS ix_operacao_assistida_marcos_conta_id ON plantaopro.operacao_assistida_marcos(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_operacao_assistida_marcos_reg_status' AND conrelid = 'plantaopro.operacao_assistida_marcos'::regclass) THEN
        ALTER TABLE plantaopro.operacao_assistida_marcos ADD CONSTRAINT ck_operacao_assistida_marcos_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.white_label_templates (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.white_label_templates ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.white_label_templates ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.white_label_templates ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.white_label_templates ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_white_label_templates_tenant_id ON plantaopro.white_label_templates(tenant_id);
CREATE INDEX IF NOT EXISTS ix_white_label_templates_cliente_id ON plantaopro.white_label_templates(cliente_id);
CREATE INDEX IF NOT EXISTS ix_white_label_templates_status ON plantaopro.white_label_templates(status);
CREATE INDEX IF NOT EXISTS ix_white_label_templates_reg_date ON plantaopro.white_label_templates(reg_date);
CREATE INDEX IF NOT EXISTS ix_white_label_templates_programa_id ON plantaopro.white_label_templates(programa_id);
CREATE INDEX IF NOT EXISTS ix_white_label_templates_conta_id ON plantaopro.white_label_templates(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_white_label_templates_reg_status' AND conrelid = 'plantaopro.white_label_templates'::regclass) THEN
        ALTER TABLE plantaopro.white_label_templates ADD CONSTRAINT ck_white_label_templates_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.white_label_template_assets (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.white_label_template_assets ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.white_label_template_assets ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.white_label_template_assets ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.white_label_template_assets ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_white_label_template_assets_tenant_id ON plantaopro.white_label_template_assets(tenant_id);
CREATE INDEX IF NOT EXISTS ix_white_label_template_assets_cliente_id ON plantaopro.white_label_template_assets(cliente_id);
CREATE INDEX IF NOT EXISTS ix_white_label_template_assets_status ON plantaopro.white_label_template_assets(status);
CREATE INDEX IF NOT EXISTS ix_white_label_template_assets_reg_date ON plantaopro.white_label_template_assets(reg_date);
CREATE INDEX IF NOT EXISTS ix_white_label_template_assets_programa_id ON plantaopro.white_label_template_assets(programa_id);
CREATE INDEX IF NOT EXISTS ix_white_label_template_assets_conta_id ON plantaopro.white_label_template_assets(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_white_label_template_assets_reg_status' AND conrelid = 'plantaopro.white_label_template_assets'::regclass) THEN
        ALTER TABLE plantaopro.white_label_template_assets ADD CONSTRAINT ck_white_label_template_assets_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.white_label_template_cores (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.white_label_template_cores ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.white_label_template_cores ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.white_label_template_cores ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.white_label_template_cores ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_white_label_template_cores_tenant_id ON plantaopro.white_label_template_cores(tenant_id);
CREATE INDEX IF NOT EXISTS ix_white_label_template_cores_cliente_id ON plantaopro.white_label_template_cores(cliente_id);
CREATE INDEX IF NOT EXISTS ix_white_label_template_cores_status ON plantaopro.white_label_template_cores(status);
CREATE INDEX IF NOT EXISTS ix_white_label_template_cores_reg_date ON plantaopro.white_label_template_cores(reg_date);
CREATE INDEX IF NOT EXISTS ix_white_label_template_cores_programa_id ON plantaopro.white_label_template_cores(programa_id);
CREATE INDEX IF NOT EXISTS ix_white_label_template_cores_conta_id ON plantaopro.white_label_template_cores(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_white_label_template_cores_reg_status' AND conrelid = 'plantaopro.white_label_template_cores'::regclass) THEN
        ALTER TABLE plantaopro.white_label_template_cores ADD CONSTRAINT ck_white_label_template_cores_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.white_label_template_textos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.white_label_template_textos ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.white_label_template_textos ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.white_label_template_textos ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.white_label_template_textos ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_white_label_template_textos_tenant_id ON plantaopro.white_label_template_textos(tenant_id);
CREATE INDEX IF NOT EXISTS ix_white_label_template_textos_cliente_id ON plantaopro.white_label_template_textos(cliente_id);
CREATE INDEX IF NOT EXISTS ix_white_label_template_textos_status ON plantaopro.white_label_template_textos(status);
CREATE INDEX IF NOT EXISTS ix_white_label_template_textos_reg_date ON plantaopro.white_label_template_textos(reg_date);
CREATE INDEX IF NOT EXISTS ix_white_label_template_textos_programa_id ON plantaopro.white_label_template_textos(programa_id);
CREATE INDEX IF NOT EXISTS ix_white_label_template_textos_conta_id ON plantaopro.white_label_template_textos(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_white_label_template_textos_reg_status' AND conrelid = 'plantaopro.white_label_template_textos'::regclass) THEN
        ALTER TABLE plantaopro.white_label_template_textos ADD CONSTRAINT ck_white_label_template_textos_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.white_label_template_emails (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.white_label_template_emails ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.white_label_template_emails ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.white_label_template_emails ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.white_label_template_emails ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_white_label_template_emails_tenant_id ON plantaopro.white_label_template_emails(tenant_id);
CREATE INDEX IF NOT EXISTS ix_white_label_template_emails_cliente_id ON plantaopro.white_label_template_emails(cliente_id);
CREATE INDEX IF NOT EXISTS ix_white_label_template_emails_status ON plantaopro.white_label_template_emails(status);
CREATE INDEX IF NOT EXISTS ix_white_label_template_emails_reg_date ON plantaopro.white_label_template_emails(reg_date);
CREATE INDEX IF NOT EXISTS ix_white_label_template_emails_programa_id ON plantaopro.white_label_template_emails(programa_id);
CREATE INDEX IF NOT EXISTS ix_white_label_template_emails_conta_id ON plantaopro.white_label_template_emails(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_white_label_template_emails_reg_status' AND conrelid = 'plantaopro.white_label_template_emails'::regclass) THEN
        ALTER TABLE plantaopro.white_label_template_emails ADD CONSTRAINT ck_white_label_template_emails_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.white_label_template_aplicacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.white_label_template_aplicacoes ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.white_label_template_aplicacoes ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.white_label_template_aplicacoes ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.white_label_template_aplicacoes ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_white_label_template_aplicacoes_tenant_id ON plantaopro.white_label_template_aplicacoes(tenant_id);
CREATE INDEX IF NOT EXISTS ix_white_label_template_aplicacoes_cliente_id ON plantaopro.white_label_template_aplicacoes(cliente_id);
CREATE INDEX IF NOT EXISTS ix_white_label_template_aplicacoes_status ON plantaopro.white_label_template_aplicacoes(status);
CREATE INDEX IF NOT EXISTS ix_white_label_template_aplicacoes_reg_date ON plantaopro.white_label_template_aplicacoes(reg_date);
CREATE INDEX IF NOT EXISTS ix_white_label_template_aplicacoes_programa_id ON plantaopro.white_label_template_aplicacoes(programa_id);
CREATE INDEX IF NOT EXISTS ix_white_label_template_aplicacoes_conta_id ON plantaopro.white_label_template_aplicacoes(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_white_label_template_aplicacoes_reg_status' AND conrelid = 'plantaopro.white_label_template_aplicacoes'::regclass) THEN
        ALTER TABLE plantaopro.white_label_template_aplicacoes ADD CONSTRAINT ck_white_label_template_aplicacoes_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.suporte_chamados (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.suporte_chamados ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.suporte_chamados ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.suporte_chamados ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.suporte_chamados ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_suporte_chamados_tenant_id ON plantaopro.suporte_chamados(tenant_id);
CREATE INDEX IF NOT EXISTS ix_suporte_chamados_cliente_id ON plantaopro.suporte_chamados(cliente_id);
CREATE INDEX IF NOT EXISTS ix_suporte_chamados_status ON plantaopro.suporte_chamados(status);
CREATE INDEX IF NOT EXISTS ix_suporte_chamados_reg_date ON plantaopro.suporte_chamados(reg_date);
CREATE INDEX IF NOT EXISTS ix_suporte_chamados_programa_id ON plantaopro.suporte_chamados(programa_id);
CREATE INDEX IF NOT EXISTS ix_suporte_chamados_conta_id ON plantaopro.suporte_chamados(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_suporte_chamados_reg_status' AND conrelid = 'plantaopro.suporte_chamados'::regclass) THEN
        ALTER TABLE plantaopro.suporte_chamados ADD CONSTRAINT ck_suporte_chamados_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.suporte_chamado_eventos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.suporte_chamado_eventos ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.suporte_chamado_eventos ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.suporte_chamado_eventos ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.suporte_chamado_eventos ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_suporte_chamado_eventos_tenant_id ON plantaopro.suporte_chamado_eventos(tenant_id);
CREATE INDEX IF NOT EXISTS ix_suporte_chamado_eventos_cliente_id ON plantaopro.suporte_chamado_eventos(cliente_id);
CREATE INDEX IF NOT EXISTS ix_suporte_chamado_eventos_status ON plantaopro.suporte_chamado_eventos(status);
CREATE INDEX IF NOT EXISTS ix_suporte_chamado_eventos_reg_date ON plantaopro.suporte_chamado_eventos(reg_date);
CREATE INDEX IF NOT EXISTS ix_suporte_chamado_eventos_programa_id ON plantaopro.suporte_chamado_eventos(programa_id);
CREATE INDEX IF NOT EXISTS ix_suporte_chamado_eventos_conta_id ON plantaopro.suporte_chamado_eventos(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_suporte_chamado_eventos_reg_status' AND conrelid = 'plantaopro.suporte_chamado_eventos'::regclass) THEN
        ALTER TABLE plantaopro.suporte_chamado_eventos ADD CONSTRAINT ck_suporte_chamado_eventos_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.suporte_chamado_anexos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.suporte_chamado_anexos ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.suporte_chamado_anexos ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.suporte_chamado_anexos ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.suporte_chamado_anexos ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_suporte_chamado_anexos_tenant_id ON plantaopro.suporte_chamado_anexos(tenant_id);
CREATE INDEX IF NOT EXISTS ix_suporte_chamado_anexos_cliente_id ON plantaopro.suporte_chamado_anexos(cliente_id);
CREATE INDEX IF NOT EXISTS ix_suporte_chamado_anexos_status ON plantaopro.suporte_chamado_anexos(status);
CREATE INDEX IF NOT EXISTS ix_suporte_chamado_anexos_reg_date ON plantaopro.suporte_chamado_anexos(reg_date);
CREATE INDEX IF NOT EXISTS ix_suporte_chamado_anexos_programa_id ON plantaopro.suporte_chamado_anexos(programa_id);
CREATE INDEX IF NOT EXISTS ix_suporte_chamado_anexos_conta_id ON plantaopro.suporte_chamado_anexos(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_suporte_chamado_anexos_reg_status' AND conrelid = 'plantaopro.suporte_chamado_anexos'::regclass) THEN
        ALTER TABLE plantaopro.suporte_chamado_anexos ADD CONSTRAINT ck_suporte_chamado_anexos_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.suporte_sla_regras (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.suporte_sla_regras ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.suporte_sla_regras ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.suporte_sla_regras ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.suporte_sla_regras ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_suporte_sla_regras_tenant_id ON plantaopro.suporte_sla_regras(tenant_id);
CREATE INDEX IF NOT EXISTS ix_suporte_sla_regras_cliente_id ON plantaopro.suporte_sla_regras(cliente_id);
CREATE INDEX IF NOT EXISTS ix_suporte_sla_regras_status ON plantaopro.suporte_sla_regras(status);
CREATE INDEX IF NOT EXISTS ix_suporte_sla_regras_reg_date ON plantaopro.suporte_sla_regras(reg_date);
CREATE INDEX IF NOT EXISTS ix_suporte_sla_regras_programa_id ON plantaopro.suporte_sla_regras(programa_id);
CREATE INDEX IF NOT EXISTS ix_suporte_sla_regras_conta_id ON plantaopro.suporte_sla_regras(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_suporte_sla_regras_reg_status' AND conrelid = 'plantaopro.suporte_sla_regras'::regclass) THEN
        ALTER TABLE plantaopro.suporte_sla_regras ADD CONSTRAINT ck_suporte_sla_regras_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.suporte_sla_eventos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.suporte_sla_eventos ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.suporte_sla_eventos ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.suporte_sla_eventos ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.suporte_sla_eventos ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_suporte_sla_eventos_tenant_id ON plantaopro.suporte_sla_eventos(tenant_id);
CREATE INDEX IF NOT EXISTS ix_suporte_sla_eventos_cliente_id ON plantaopro.suporte_sla_eventos(cliente_id);
CREATE INDEX IF NOT EXISTS ix_suporte_sla_eventos_status ON plantaopro.suporte_sla_eventos(status);
CREATE INDEX IF NOT EXISTS ix_suporte_sla_eventos_reg_date ON plantaopro.suporte_sla_eventos(reg_date);
CREATE INDEX IF NOT EXISTS ix_suporte_sla_eventos_programa_id ON plantaopro.suporte_sla_eventos(programa_id);
CREATE INDEX IF NOT EXISTS ix_suporte_sla_eventos_conta_id ON plantaopro.suporte_sla_eventos(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_suporte_sla_eventos_reg_status' AND conrelid = 'plantaopro.suporte_sla_eventos'::regclass) THEN
        ALTER TABLE plantaopro.suporte_sla_eventos ADD CONSTRAINT ck_suporte_sla_eventos_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.suporte_canais (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.suporte_canais ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.suporte_canais ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.suporte_canais ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.suporte_canais ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_suporte_canais_tenant_id ON plantaopro.suporte_canais(tenant_id);
CREATE INDEX IF NOT EXISTS ix_suporte_canais_cliente_id ON plantaopro.suporte_canais(cliente_id);
CREATE INDEX IF NOT EXISTS ix_suporte_canais_status ON plantaopro.suporte_canais(status);
CREATE INDEX IF NOT EXISTS ix_suporte_canais_reg_date ON plantaopro.suporte_canais(reg_date);
CREATE INDEX IF NOT EXISTS ix_suporte_canais_programa_id ON plantaopro.suporte_canais(programa_id);
CREATE INDEX IF NOT EXISTS ix_suporte_canais_conta_id ON plantaopro.suporte_canais(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_suporte_canais_reg_status' AND conrelid = 'plantaopro.suporte_canais'::regclass) THEN
        ALTER TABLE plantaopro.suporte_canais ADD CONSTRAINT ck_suporte_canais_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.suporte_base_conhecimento (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.suporte_base_conhecimento ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.suporte_base_conhecimento ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.suporte_base_conhecimento ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.suporte_base_conhecimento ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_suporte_base_conhecimento_tenant_id ON plantaopro.suporte_base_conhecimento(tenant_id);
CREATE INDEX IF NOT EXISTS ix_suporte_base_conhecimento_cliente_id ON plantaopro.suporte_base_conhecimento(cliente_id);
CREATE INDEX IF NOT EXISTS ix_suporte_base_conhecimento_status ON plantaopro.suporte_base_conhecimento(status);
CREATE INDEX IF NOT EXISTS ix_suporte_base_conhecimento_reg_date ON plantaopro.suporte_base_conhecimento(reg_date);
CREATE INDEX IF NOT EXISTS ix_suporte_base_conhecimento_programa_id ON plantaopro.suporte_base_conhecimento(programa_id);
CREATE INDEX IF NOT EXISTS ix_suporte_base_conhecimento_conta_id ON plantaopro.suporte_base_conhecimento(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_suporte_base_conhecimento_reg_status' AND conrelid = 'plantaopro.suporte_base_conhecimento'::regclass) THEN
        ALTER TABLE plantaopro.suporte_base_conhecimento ADD CONSTRAINT ck_suporte_base_conhecimento_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.contas_b2b (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.contas_b2b ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.contas_b2b ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.contas_b2b ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.contas_b2b ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_contas_b2b_tenant_id ON plantaopro.contas_b2b(tenant_id);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_cliente_id ON plantaopro.contas_b2b(cliente_id);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_status ON plantaopro.contas_b2b(status);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_reg_date ON plantaopro.contas_b2b(reg_date);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_programa_id ON plantaopro.contas_b2b(programa_id);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_conta_id ON plantaopro.contas_b2b(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_contas_b2b_reg_status' AND conrelid = 'plantaopro.contas_b2b'::regclass) THEN
        ALTER TABLE plantaopro.contas_b2b ADD CONSTRAINT ck_contas_b2b_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.contas_b2b_contatos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.contas_b2b_contatos ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.contas_b2b_contatos ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.contas_b2b_contatos ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.contas_b2b_contatos ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_contas_b2b_contatos_tenant_id ON plantaopro.contas_b2b_contatos(tenant_id);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_contatos_cliente_id ON plantaopro.contas_b2b_contatos(cliente_id);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_contatos_status ON plantaopro.contas_b2b_contatos(status);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_contatos_reg_date ON plantaopro.contas_b2b_contatos(reg_date);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_contatos_programa_id ON plantaopro.contas_b2b_contatos(programa_id);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_contatos_conta_id ON plantaopro.contas_b2b_contatos(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_contas_b2b_contatos_reg_status' AND conrelid = 'plantaopro.contas_b2b_contatos'::regclass) THEN
        ALTER TABLE plantaopro.contas_b2b_contatos ADD CONSTRAINT ck_contas_b2b_contatos_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.contas_b2b_decisores (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.contas_b2b_decisores ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.contas_b2b_decisores ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.contas_b2b_decisores ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.contas_b2b_decisores ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_contas_b2b_decisores_tenant_id ON plantaopro.contas_b2b_decisores(tenant_id);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_decisores_cliente_id ON plantaopro.contas_b2b_decisores(cliente_id);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_decisores_status ON plantaopro.contas_b2b_decisores(status);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_decisores_reg_date ON plantaopro.contas_b2b_decisores(reg_date);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_decisores_programa_id ON plantaopro.contas_b2b_decisores(programa_id);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_decisores_conta_id ON plantaopro.contas_b2b_decisores(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_contas_b2b_decisores_reg_status' AND conrelid = 'plantaopro.contas_b2b_decisores'::regclass) THEN
        ALTER TABLE plantaopro.contas_b2b_decisores ADD CONSTRAINT ck_contas_b2b_decisores_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.contas_b2b_reunioes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.contas_b2b_reunioes ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.contas_b2b_reunioes ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.contas_b2b_reunioes ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.contas_b2b_reunioes ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_contas_b2b_reunioes_tenant_id ON plantaopro.contas_b2b_reunioes(tenant_id);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_reunioes_cliente_id ON plantaopro.contas_b2b_reunioes(cliente_id);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_reunioes_status ON plantaopro.contas_b2b_reunioes(status);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_reunioes_reg_date ON plantaopro.contas_b2b_reunioes(reg_date);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_reunioes_programa_id ON plantaopro.contas_b2b_reunioes(programa_id);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_reunioes_conta_id ON plantaopro.contas_b2b_reunioes(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_contas_b2b_reunioes_reg_status' AND conrelid = 'plantaopro.contas_b2b_reunioes'::regclass) THEN
        ALTER TABLE plantaopro.contas_b2b_reunioes ADD CONSTRAINT ck_contas_b2b_reunioes_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.contas_b2b_oportunidades (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.contas_b2b_oportunidades ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.contas_b2b_oportunidades ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.contas_b2b_oportunidades ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.contas_b2b_oportunidades ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_contas_b2b_oportunidades_tenant_id ON plantaopro.contas_b2b_oportunidades(tenant_id);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_oportunidades_cliente_id ON plantaopro.contas_b2b_oportunidades(cliente_id);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_oportunidades_status ON plantaopro.contas_b2b_oportunidades(status);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_oportunidades_reg_date ON plantaopro.contas_b2b_oportunidades(reg_date);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_oportunidades_programa_id ON plantaopro.contas_b2b_oportunidades(programa_id);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_oportunidades_conta_id ON plantaopro.contas_b2b_oportunidades(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_contas_b2b_oportunidades_reg_status' AND conrelid = 'plantaopro.contas_b2b_oportunidades'::regclass) THEN
        ALTER TABLE plantaopro.contas_b2b_oportunidades ADD CONSTRAINT ck_contas_b2b_oportunidades_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.contas_b2b_renovacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.contas_b2b_renovacoes ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.contas_b2b_renovacoes ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.contas_b2b_renovacoes ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.contas_b2b_renovacoes ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_contas_b2b_renovacoes_tenant_id ON plantaopro.contas_b2b_renovacoes(tenant_id);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_renovacoes_cliente_id ON plantaopro.contas_b2b_renovacoes(cliente_id);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_renovacoes_status ON plantaopro.contas_b2b_renovacoes(status);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_renovacoes_reg_date ON plantaopro.contas_b2b_renovacoes(reg_date);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_renovacoes_programa_id ON plantaopro.contas_b2b_renovacoes(programa_id);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_renovacoes_conta_id ON plantaopro.contas_b2b_renovacoes(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_contas_b2b_renovacoes_reg_status' AND conrelid = 'plantaopro.contas_b2b_renovacoes'::regclass) THEN
        ALTER TABLE plantaopro.contas_b2b_renovacoes ADD CONSTRAINT ck_contas_b2b_renovacoes_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.contas_b2b_expansoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.contas_b2b_expansoes ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.contas_b2b_expansoes ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.contas_b2b_expansoes ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.contas_b2b_expansoes ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_contas_b2b_expansoes_tenant_id ON plantaopro.contas_b2b_expansoes(tenant_id);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_expansoes_cliente_id ON plantaopro.contas_b2b_expansoes(cliente_id);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_expansoes_status ON plantaopro.contas_b2b_expansoes(status);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_expansoes_reg_date ON plantaopro.contas_b2b_expansoes(reg_date);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_expansoes_programa_id ON plantaopro.contas_b2b_expansoes(programa_id);
CREATE INDEX IF NOT EXISTS ix_contas_b2b_expansoes_conta_id ON plantaopro.contas_b2b_expansoes(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_contas_b2b_expansoes_reg_status' AND conrelid = 'plantaopro.contas_b2b_expansoes'::regclass) THEN
        ALTER TABLE plantaopro.contas_b2b_expansoes ADD CONSTRAINT ck_contas_b2b_expansoes_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.executivo_kpis_snapshots (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.executivo_kpis_snapshots ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.executivo_kpis_snapshots ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.executivo_kpis_snapshots ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.executivo_kpis_snapshots ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_executivo_kpis_snapshots_tenant_id ON plantaopro.executivo_kpis_snapshots(tenant_id);
CREATE INDEX IF NOT EXISTS ix_executivo_kpis_snapshots_cliente_id ON plantaopro.executivo_kpis_snapshots(cliente_id);
CREATE INDEX IF NOT EXISTS ix_executivo_kpis_snapshots_status ON plantaopro.executivo_kpis_snapshots(status);
CREATE INDEX IF NOT EXISTS ix_executivo_kpis_snapshots_reg_date ON plantaopro.executivo_kpis_snapshots(reg_date);
CREATE INDEX IF NOT EXISTS ix_executivo_kpis_snapshots_programa_id ON plantaopro.executivo_kpis_snapshots(programa_id);
CREATE INDEX IF NOT EXISTS ix_executivo_kpis_snapshots_conta_id ON plantaopro.executivo_kpis_snapshots(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_executivo_kpis_snapshots_reg_status' AND conrelid = 'plantaopro.executivo_kpis_snapshots'::regclass) THEN
        ALTER TABLE plantaopro.executivo_kpis_snapshots ADD CONSTRAINT ck_executivo_kpis_snapshots_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.executivo_alertas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.executivo_alertas ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.executivo_alertas ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.executivo_alertas ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.executivo_alertas ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_executivo_alertas_tenant_id ON plantaopro.executivo_alertas(tenant_id);
CREATE INDEX IF NOT EXISTS ix_executivo_alertas_cliente_id ON plantaopro.executivo_alertas(cliente_id);
CREATE INDEX IF NOT EXISTS ix_executivo_alertas_status ON plantaopro.executivo_alertas(status);
CREATE INDEX IF NOT EXISTS ix_executivo_alertas_reg_date ON plantaopro.executivo_alertas(reg_date);
CREATE INDEX IF NOT EXISTS ix_executivo_alertas_programa_id ON plantaopro.executivo_alertas(programa_id);
CREATE INDEX IF NOT EXISTS ix_executivo_alertas_conta_id ON plantaopro.executivo_alertas(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_executivo_alertas_reg_status' AND conrelid = 'plantaopro.executivo_alertas'::regclass) THEN
        ALTER TABLE plantaopro.executivo_alertas ADD CONSTRAINT ck_executivo_alertas_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.executivo_eventos_negocio (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.executivo_eventos_negocio ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.executivo_eventos_negocio ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.executivo_eventos_negocio ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.executivo_eventos_negocio ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_executivo_eventos_negocio_tenant_id ON plantaopro.executivo_eventos_negocio(tenant_id);
CREATE INDEX IF NOT EXISTS ix_executivo_eventos_negocio_cliente_id ON plantaopro.executivo_eventos_negocio(cliente_id);
CREATE INDEX IF NOT EXISTS ix_executivo_eventos_negocio_status ON plantaopro.executivo_eventos_negocio(status);
CREATE INDEX IF NOT EXISTS ix_executivo_eventos_negocio_reg_date ON plantaopro.executivo_eventos_negocio(reg_date);
CREATE INDEX IF NOT EXISTS ix_executivo_eventos_negocio_programa_id ON plantaopro.executivo_eventos_negocio(programa_id);
CREATE INDEX IF NOT EXISTS ix_executivo_eventos_negocio_conta_id ON plantaopro.executivo_eventos_negocio(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_executivo_eventos_negocio_reg_status' AND conrelid = 'plantaopro.executivo_eventos_negocio'::regclass) THEN
        ALTER TABLE plantaopro.executivo_eventos_negocio ADD CONSTRAINT ck_executivo_eventos_negocio_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.executivo_metas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.executivo_metas ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.executivo_metas ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.executivo_metas ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.executivo_metas ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_executivo_metas_tenant_id ON plantaopro.executivo_metas(tenant_id);
CREATE INDEX IF NOT EXISTS ix_executivo_metas_cliente_id ON plantaopro.executivo_metas(cliente_id);
CREATE INDEX IF NOT EXISTS ix_executivo_metas_status ON plantaopro.executivo_metas(status);
CREATE INDEX IF NOT EXISTS ix_executivo_metas_reg_date ON plantaopro.executivo_metas(reg_date);
CREATE INDEX IF NOT EXISTS ix_executivo_metas_programa_id ON plantaopro.executivo_metas(programa_id);
CREATE INDEX IF NOT EXISTS ix_executivo_metas_conta_id ON plantaopro.executivo_metas(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_executivo_metas_reg_status' AND conrelid = 'plantaopro.executivo_metas'::regclass) THEN
        ALTER TABLE plantaopro.executivo_metas ADD CONSTRAINT ck_executivo_metas_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
CREATE TABLE IF NOT EXISTS plantaopro.executivo_resultados (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    parceiro_id uuid NULL,
    programa_id uuid NULL,
    conta_id uuid NULL,
    usuario_id uuid NULL,
    codigo text NOT NULL DEFAULT '',
    nome text NOT NULL DEFAULT '',
    titulo text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    categoria text NOT NULL DEFAULT 'GERAL',
    severidade text NOT NULL DEFAULT 'MEDIA',
    prioridade text NOT NULL DEFAULT 'MEDIA',
    status text NOT NULL DEFAULT 'ATIVO',
    valor numeric(18,2) NOT NULL DEFAULT 0,
    percentual numeric(9,2) NOT NULL DEFAULT 0,
    score integer NOT NULL DEFAULT 0,
    data_inicio timestamp NULL,
    data_fim timestamp NULL,
    data_limite timestamp NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    observacoes text NOT NULL DEFAULT '',
    reg_date timestamp NOT NULL DEFAULT now(),
    reg_update timestamp NULL,
    reg_status char(1) NOT NULL DEFAULT 'A'
);
ALTER TABLE plantaopro.executivo_resultados ADD COLUMN IF NOT EXISTS tenant_id uuid NULL;
ALTER TABLE plantaopro.executivo_resultados ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.executivo_resultados ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'ATIVO';
ALTER TABLE plantaopro.executivo_resultados ADD COLUMN IF NOT EXISTS payload jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_executivo_resultados_tenant_id ON plantaopro.executivo_resultados(tenant_id);
CREATE INDEX IF NOT EXISTS ix_executivo_resultados_cliente_id ON plantaopro.executivo_resultados(cliente_id);
CREATE INDEX IF NOT EXISTS ix_executivo_resultados_status ON plantaopro.executivo_resultados(status);
CREATE INDEX IF NOT EXISTS ix_executivo_resultados_reg_date ON plantaopro.executivo_resultados(reg_date);
CREATE INDEX IF NOT EXISTS ix_executivo_resultados_programa_id ON plantaopro.executivo_resultados(programa_id);
CREATE INDEX IF NOT EXISTS ix_executivo_resultados_conta_id ON plantaopro.executivo_resultados(conta_id);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_executivo_resultados_reg_status' AND conrelid = 'plantaopro.executivo_resultados'::regclass) THEN
        ALTER TABLE plantaopro.executivo_resultados ADD CONSTRAINT ck_executivo_resultados_reg_status CHECK (reg_status IN ('A','I'));
    END IF;
END $$;
ALTER TABLE plantaopro.piloto_programas ADD COLUMN IF NOT EXISTS objetivo text NOT NULL DEFAULT '';
ALTER TABLE plantaopro.piloto_programas ADD COLUMN IF NOT EXISTS criterios_sucesso text NOT NULL DEFAULT '';
ALTER TABLE plantaopro.piloto_programas ADD COLUMN IF NOT EXISTS responsavel text NOT NULL DEFAULT '';
ALTER TABLE plantaopro.piloto_clientes ADD COLUMN IF NOT EXISTS plano_trial text NOT NULL DEFAULT 'PILOTO_TRIAL';
ALTER TABLE plantaopro.piloto_clientes ADD COLUMN IF NOT EXISTS desconto_percentual numeric(9,2) NOT NULL DEFAULT 0;
ALTER TABLE plantaopro.piloto_clientes ADD COLUMN IF NOT EXISTS convertido_em timestamp NULL;
ALTER TABLE plantaopro.piloto_feedbacks ADD COLUMN IF NOT EXISTS resolucao text NOT NULL DEFAULT '';
ALTER TABLE plantaopro.piloto_feedbacks ADD COLUMN IF NOT EXISTS acao_produto boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.cs_contas ADD COLUMN IF NOT EXISTS health_score integer NOT NULL DEFAULT 0;
ALTER TABLE plantaopro.cs_contas ADD COLUMN IF NOT EXISTS health_categoria text NOT NULL DEFAULT 'ATENCAO';
ALTER TABLE plantaopro.cs_contas ADD COLUMN IF NOT EXISTS nps integer NOT NULL DEFAULT 0;
ALTER TABLE plantaopro.cs_contas ADD COLUMN IF NOT EXISTS uso_percentual integer NOT NULL DEFAULT 0;
ALTER TABLE plantaopro.cs_nps ADD COLUMN IF NOT EXISTS nota integer NOT NULL DEFAULT 0;
ALTER TABLE plantaopro.cs_nps ADD COLUMN IF NOT EXISTS periodo text NOT NULL DEFAULT '';
ALTER TABLE plantaopro.white_label_templates ADD COLUMN IF NOT EXISTS cor_primaria text NOT NULL DEFAULT '#0d6efd';
ALTER TABLE plantaopro.white_label_templates ADD COLUMN IF NOT EXISTS cor_secundaria text NOT NULL DEFAULT '#198754';
ALTER TABLE plantaopro.white_label_templates ADD COLUMN IF NOT EXISTS fonte text NOT NULL DEFAULT 'Inter';
ALTER TABLE plantaopro.white_label_templates ADD COLUMN IF NOT EXISTS liberado_admin_cliente boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.operacao_assistida_etapas ADD COLUMN IF NOT EXISTS ordem integer NOT NULL DEFAULT 0;
ALTER TABLE plantaopro.operacao_assistida_etapas ADD COLUMN IF NOT EXISTS responsavel text NOT NULL DEFAULT '';
ALTER TABLE plantaopro.operacao_assistida_etapas ADD COLUMN IF NOT EXISTS justificativa text NOT NULL DEFAULT '';
ALTER TABLE plantaopro.contas_b2b_renovacoes ADD COLUMN IF NOT EXISTS vencimento_contrato date NULL;
ALTER TABLE plantaopro.contas_b2b_renovacoes ADD COLUMN IF NOT EXISTS valor_atual numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE plantaopro.contas_b2b_expansoes ADD COLUMN IF NOT EXISTS valor_estimado numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE plantaopro.contas_b2b_expansoes ADD COLUMN IF NOT EXISTS motivo_perda text NOT NULL DEFAULT '';
ALTER TABLE plantaopro.suporte_chamados ADD COLUMN IF NOT EXISTS sla_limite timestamp NULL;
ALTER TABLE plantaopro.suporte_chamados ADD COLUMN IF NOT EXISTS resolvido_em timestamp NULL;
ALTER TABLE plantaopro.executivo_kpis_snapshots ADD COLUMN IF NOT EXISTS visao text NOT NULL DEFAULT 'resumo';
ALTER TABLE plantaopro.executivo_kpis_snapshots ADD COLUMN IF NOT EXISTS mrr_estimado numeric(18,2) NOT NULL DEFAULT 0;
INSERT INTO plantaopro.white_label_templates(nome, titulo, descricao, categoria, status, payload)
SELECT nome, nome, descricao, 'TEMPLATE', 'ATIVO', payload::jsonb
FROM (VALUES
('Saúde Azul Profissional','Azul profissional para operações médicas','{"corPrimaria":"#0d6efd","modulos":["PLANTOES","ESCALAS","FINANCEIRO"]}'),
('Hospital Premium','Identidade premium para hospitais','{"corPrimaria":"#12263f","modulos":["WHITE_LABEL","SLA"]}'),
('Clínica Moderna','Visual claro para clínicas','{"corPrimaria":"#20c997","modulos":["PLANTOES"]}'),
('Rede Médica Enterprise','Template para redes multiunidade','{"corPrimaria":"#6610f2","modulos":["MULTI_TENANT","RELATORIOS"]}'),
('Operação Simples','Configuração inicial enxuta','{"corPrimaria":"#198754","modulos":["PLANTOES"]}'),
('White Label Neutro','Template neutro e reutilizável','{"corPrimaria":"#6c757d","modulos":["BASE"]}'),
('Parceiro Revendedor','Template para canais e revenda','{"corPrimaria":"#fd7e14","modulos":["PARCEIROS"]}')
) AS v(nome, descricao, payload)
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.white_label_templates t WHERE lower(t.nome)=lower(v.nome) AND t.reg_status='A');
