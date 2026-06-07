-- PlantaoPro SaaS Premium - colunas, auditoria e notificações avançadas

ALTER TABLE IF EXISTS usuarios
    ADD COLUMN IF NOT EXISTS telefone varchar(20),
    ADD COLUMN IF NOT EXISTS receber_notificacao_email boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS receber_notificacao_inapp boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS alerta_escala boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS alerta_plantao boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS alerta_pagamento boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS limite_horas_semanais numeric(6,2) NOT NULL DEFAULT 60;

ALTER TABLE IF EXISTS escalas
    ADD COLUMN IF NOT EXISTS horas_previstas numeric(6,2),
    ADD COLUMN IF NOT EXISTS conflito_detectado boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS score_prioridade integer NOT NULL DEFAULT 0;

UPDATE escalas
SET horas_previstas = EXTRACT(EPOCH FROM (data_fim - data_inicio)) / 3600.0
WHERE horas_previstas IS NULL;

CREATE TABLE IF NOT EXISTS notificacoes_envio_log (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    notificacao_id uuid NULL,
    usuario_id uuid NULL,
    canal varchar(20) NOT NULL,
    sucesso boolean NOT NULL,
    erro text NULL,
    ip_origem varchar(64) NULL,
    perfil varchar(60) NULL,
    payload jsonb NULL,
    reg_date timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS auditoria_acoes_criticas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id uuid NULL,
    entidade varchar(100) NOT NULL,
    entidade_id uuid NULL,
    acao varchar(80) NOT NULL,
    detalhes jsonb NULL,
    sucesso boolean NOT NULL DEFAULT true,
    ip_origem varchar(64) NULL,
    perfil varchar(60) NULL,
    reg_date timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS historico_alteracoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tabela varchar(100) NOT NULL,
    registro_id uuid NOT NULL,
    campo varchar(100) NOT NULL,
    valor_anterior text NULL,
    valor_novo text NULL,
    alterado_por uuid NULL,
    reg_date timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_notificacoes_envio_log_reg_date ON notificacoes_envio_log(reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_auditoria_acoes_criticas_entidade ON auditoria_acoes_criticas(entidade, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_historico_alteracoes_registro ON historico_alteracoes(registro_id, reg_date DESC);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_pagamentos_valores_positivos'
          AND conrelid = 'plantaopro.pagamentos'::regclass
    ) THEN
        ALTER TABLE plantaopro.pagamentos
        ADD CONSTRAINT ck_pagamentos_valores_positivos CHECK (valor_previsto >= 0 AND (valor_pago IS NULL OR valor_pago >= 0));
    END IF;
END $$;


-- Regras premium SaaS: score de prioridade, limites semanais, notificacoes e constraints de pagamentos
alter table if exists plantaopro.escalas
    add column if not exists escala_prioridade_score numeric(10,2) not null default 0,
    add column if not exists horas_previstas numeric(10,2) not null default 0;

alter table if exists plantaopro.medicos
    add column if not exists limite_horas_semanais numeric(10,2) not null default 60;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_pagamentos_valores_validos'
          AND conrelid = 'plantaopro.pagamentos'::regclass
    ) THEN
        ALTER TABLE plantaopro.pagamentos
        ADD CONSTRAINT ck_pagamentos_valores_validos CHECK (coalesce(valor_pago,0) >= 0 and valor_previsto >= 0);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_pagamentos_status_validos'
          AND conrelid = 'plantaopro.pagamentos'::regclass
    ) THEN
        ALTER TABLE plantaopro.pagamentos
        ADD CONSTRAINT ck_pagamentos_status_validos CHECK (status in ('PENDENTE','PAGO','CANCELADO'));
    END IF;
END $$;

create index if not exists idx_escalas_medico_periodo on plantaopro.escalas(medico_id, data_inicio, data_fim);
create index if not exists idx_pagamentos_status_data on plantaopro.pagamentos(status, data_prevista);
create index if not exists idx_notificacoes_usuario_lida on plantaopro.notificacoes(usuario_id, lida);
