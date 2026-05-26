create schema if not exists plantaopro;

create table if not exists plantaopro.medico_elegibilidade_log (
    id uuid primary key default gen_random_uuid(),
    medico_id uuid not null,
    plantao_id uuid null,
    elegivel boolean not null,
    bloqueado boolean not null,
    score_elegibilidade numeric(5,2) not null default 0,
    recomendacao_texto text null,
    motivos_json jsonb null,
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.escala_historico (
    id uuid primary key default gen_random_uuid(),
    escala_id uuid not null,
    status_anterior varchar(40) null,
    status_novo varchar(40) not null,
    justificativa text null,
    usuario_id uuid null,
    reg_date timestamp not null default now()
);

create index if not exists idx_medico_elegibilidade_log_medico_data on plantaopro.medico_elegibilidade_log (medico_id, reg_date desc);
create index if not exists idx_escala_historico_escala_data on plantaopro.escala_historico (escala_id, reg_date desc);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_medico_elegibilidade_log_medico_id'
    ) THEN
        ALTER TABLE plantaopro.medico_elegibilidade_log
            ADD CONSTRAINT fk_medico_elegibilidade_log_medico_id
            FOREIGN KEY (medico_id) REFERENCES plantaopro.medicos(id);
    END IF;
END $$;
