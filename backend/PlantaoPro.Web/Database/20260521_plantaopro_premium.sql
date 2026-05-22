-- PlantaoPro Premium SaaS - Evolução de schema
-- Compatível com PostgreSQL 13+

CREATE SCHEMA IF NOT EXISTS plantaopro;

ALTER TABLE IF EXISTS plantaopro.escalas
    ADD COLUMN IF NOT EXISTS data_inicio timestamptz,
    ADD COLUMN IF NOT EXISTS data_fim timestamptz,
    ADD COLUMN IF NOT EXISTS horas_previstas numeric(6,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS conflito_detectado boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS score_prioridade numeric(8,2) NOT NULL DEFAULT 0;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_escalas_periodo_valido') THEN
        ALTER TABLE plantaopro.escalas ADD CONSTRAINT ck_escalas_periodo_valido CHECK (data_fim IS NULL OR data_inicio IS NULL OR data_fim > data_inicio);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_escalas_horas_previstas_non_negative') THEN
        ALTER TABLE plantaopro.escalas ADD CONSTRAINT ck_escalas_horas_previstas_non_negative CHECK (horas_previstas >= 0);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_escalas_score_prioridade_non_negative') THEN
        ALTER TABLE plantaopro.escalas ADD CONSTRAINT ck_escalas_score_prioridade_non_negative CHECK (score_prioridade >= 0);
    END IF;
END $$;

ALTER TABLE IF EXISTS plantaopro.pagamentos
    ADD COLUMN IF NOT EXISTS valor_hora numeric(12,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS horas_referencia numeric(6,2) NOT NULL DEFAULT 0;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_pagamentos_valor_hora_non_negative') THEN
        ALTER TABLE plantaopro.pagamentos ADD CONSTRAINT ck_pagamentos_valor_hora_non_negative CHECK (valor_hora >= 0);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_pagamentos_horas_referencia_non_negative') THEN
        ALTER TABLE plantaopro.pagamentos ADD CONSTRAINT ck_pagamentos_horas_referencia_non_negative CHECK (horas_referencia >= 0);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_pagamentos_valor_pago_non_negative') THEN
        ALTER TABLE plantaopro.pagamentos ADD CONSTRAINT ck_pagamentos_valor_pago_non_negative CHECK (valor_pago >= 0);
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS plantaopro.auditoria_eventos (
    id uuid PRIMARY KEY,
    entidade varchar(80) NOT NULL,
    entidade_id uuid NULL,
    usuario varchar(120) NOT NULL,
    perfil varchar(60) NOT NULL,
    endpoint varchar(255) NOT NULL,
    metodo varchar(10) NOT NULL,
    status_code int NOT NULL,
    ip varchar(64) NOT NULL,
    payload jsonb NULL,
    ocorrido_em timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.notificacoes (
    id uuid PRIMARY KEY,
    usuario_id uuid NOT NULL,
    titulo varchar(120) NOT NULL,
    mensagem varchar(700) NOT NULL,
    nivel varchar(20) NOT NULL,
    lida boolean NOT NULL DEFAULT false,
    criado_em timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_escalas_medico_periodo ON plantaopro.escalas (medico_id, data_inicio, data_fim);
CREATE INDEX IF NOT EXISTS ix_escalas_conflito_detectado ON plantaopro.escalas (conflito_detectado);
CREATE INDEX IF NOT EXISTS ix_pagamentos_status_vencimento ON plantaopro.pagamentos (status, data_vencimento);
CREATE INDEX IF NOT EXISTS ix_auditoria_ocorrido_em ON plantaopro.auditoria_eventos (ocorrido_em DESC);
CREATE INDEX IF NOT EXISTS ix_auditoria_endpoint_status ON plantaopro.auditoria_eventos (endpoint, status_code);
CREATE INDEX IF NOT EXISTS ix_notificacoes_usuario_lida ON plantaopro.notificacoes (usuario_id, lida, criado_em DESC);
