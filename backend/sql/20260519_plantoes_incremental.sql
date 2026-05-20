-- Incremental ajuste seguro para plantaopro.plantoes
ALTER TABLE IF EXISTS plantaopro.plantoes
    ADD COLUMN IF NOT EXISTS vagas_disponiveis integer,
    ADD COLUMN IF NOT EXISTS tipo text,
    ADD COLUMN IF NOT EXISTS observacoes text,
    ADD COLUMN IF NOT EXISTS reg_update timestamp without time zone;
