BEGIN;

CREATE TABLE IF NOT EXISTS plantaopro.consulta_adendos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    consulta_id uuid NOT NULL,
    autor_id uuid NOT NULL,
    medico_id uuid NULL,
    motivo varchar(500) NOT NULL CHECK (length(btrim(motivo)) >= 10),
    conteudo text NOT NULL CHECK (length(btrim(conteudo)) >= 10),
    created_at timestamptz NOT NULL DEFAULT now(),
    hash char(64) NOT NULL CHECK (hash ~ '^[0-9a-f]{64}$'),
    CONSTRAINT fk_consulta_adendos_consulta
        FOREIGN KEY (consulta_id) REFERENCES plantaopro.consultas(id) ON DELETE RESTRICT
);

COMMENT ON TABLE plantaopro.consulta_adendos IS
    'Adendos imutáveis de consultas finalizadas; correções são feitas por novo adendo.';

CREATE INDEX IF NOT EXISTS ix_consulta_adendos_tenant_consulta_data
    ON plantaopro.consulta_adendos (cliente_id, consulta_id, created_at, id);

CREATE UNIQUE INDEX IF NOT EXISTS ux_consulta_adendos_tenant_id
    ON plantaopro.consulta_adendos (cliente_id, id);

CREATE OR REPLACE FUNCTION plantaopro.bloquear_mutacao_consulta_adendo()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION 'consulta_adendos são imutáveis; registre um novo adendo corretivo';
END;
$$;

DROP TRIGGER IF EXISTS trg_consulta_adendos_imutavel ON plantaopro.consulta_adendos;
CREATE TRIGGER trg_consulta_adendos_imutavel
BEFORE UPDATE OR DELETE ON plantaopro.consulta_adendos
FOR EACH ROW EXECUTE FUNCTION plantaopro.bloquear_mutacao_consulta_adendo();

COMMIT;
