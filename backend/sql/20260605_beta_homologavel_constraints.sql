-- PlantãoPro - Beta homologável: constraints e índices defensivos para fluxos críticos.
-- Seguro para reaplicação: cada constraint é adicionada somente quando não existe no schema plantaopro.
-- Observação: PostgreSQL não suporta ALTER TABLE com IF NOT EXISTS em constraints; por isso usamos blocos DO com consulta ao catálogo.

BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint c
        JOIN pg_namespace n ON n.oid = c.connamespace
        WHERE n.nspname = 'plantaopro' AND c.conname = 'ck_plantoes_periodo_valido'
    ) THEN
        ALTER TABLE plantaopro.plantoes
            ADD CONSTRAINT ck_plantoes_periodo_valido CHECK (data_fim > data_inicio) NOT VALID;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint c
        JOIN pg_namespace n ON n.oid = c.connamespace
        WHERE n.nspname = 'plantaopro' AND c.conname = 'ck_plantoes_vagas_validas'
    ) THEN
        ALTER TABLE plantaopro.plantoes
            ADD CONSTRAINT ck_plantoes_vagas_validas CHECK (vagas > 0 AND vagas_disponiveis >= 0 AND vagas_disponiveis <= vagas) NOT VALID;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint c
        JOIN pg_namespace n ON n.oid = c.connamespace
        WHERE n.nspname = 'plantaopro' AND c.conname = 'ck_plantoes_status_beta'
    ) THEN
        ALTER TABLE plantaopro.plantoes
            ADD CONSTRAINT ck_plantoes_status_beta CHECK (status IN ('rascunho', 'aberto', 'confirmado', 'realizado', 'cancelado')) NOT VALID;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint c
        JOIN pg_namespace n ON n.oid = c.connamespace
        WHERE n.nspname = 'plantaopro' AND c.conname = 'ck_escalas_status_beta'
    ) THEN
        ALTER TABLE plantaopro.escalas
            ADD CONSTRAINT ck_escalas_status_beta CHECK (status IN ('solicitada', 'confirmada', 'recusada', 'cancelada', 'substituida', 'realizada')) NOT VALID;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint c
        JOIN pg_namespace n ON n.oid = c.connamespace
        WHERE n.nspname = 'plantaopro' AND c.conname = 'ck_pagamentos_status_beta'
    ) THEN
        ALTER TABLE plantaopro.pagamentos
            ADD CONSTRAINT ck_pagamentos_status_beta CHECK (status IN ('pendente', 'gerado', 'confirmado', 'pago', 'cancelado', 'contestado')) NOT VALID;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint c
        JOIN pg_namespace n ON n.oid = c.connamespace
        WHERE n.nspname = 'plantaopro' AND c.conname = 'ck_convites_status_beta'
    ) THEN
        ALTER TABLE plantaopro.plantao_convites
            ADD CONSTRAINT ck_convites_status_beta CHECK (status IN ('enviado', 'aceito', 'recusado', 'expirado', 'cancelado')) NOT VALID;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_plantoes_beta_status_data
    ON plantaopro.plantoes (status, data_inicio)
    WHERE reg_status = 'A';

CREATE INDEX IF NOT EXISTS ix_escalas_beta_plantao_status
    ON plantaopro.escalas (plantao_id, status)
    WHERE reg_status = 'A';

CREATE INDEX IF NOT EXISTS ix_pagamentos_beta_status_previsao
    ON plantaopro.pagamentos (status, data_prevista)
    WHERE reg_status = 'A';

CREATE INDEX IF NOT EXISTS ix_convites_beta_plantao_status
    ON plantaopro.plantao_convites (plantao_id, status)
    WHERE reg_status = 'A';

COMMIT;
