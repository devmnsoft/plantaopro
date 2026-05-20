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

ALTER TABLE IF EXISTS pagamentos
    ADD CONSTRAINT IF NOT EXISTS ck_pagamentos_valores_positivos CHECK (valor_previsto >= 0 AND (valor_pago IS NULL OR valor_pago >= 0));
