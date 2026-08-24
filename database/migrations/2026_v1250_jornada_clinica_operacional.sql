-- v1.25.0: invariantes concorrentes e histórico imutável da jornada assistencial.
CREATE SCHEMA IF NOT EXISTS plantaopro;

CREATE TABLE IF NOT EXISTS plantaopro.agendamento_status_historico (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL,
    agendamento_id uuid NOT NULL, status_anterior text NULL, status_novo text NOT NULL,
    motivo text NULL, usuario_id uuid NULL, reg_date timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_agendamento_status_historico_tenant_agendamento
    ON plantaopro.agendamento_status_historico(cliente_id, agendamento_id, reg_date DESC);

CREATE TABLE IF NOT EXISTS plantaopro.atendimentos_fila (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, unidade_id uuid NOT NULL,
    agendamento_id uuid NOT NULL, paciente_id uuid NOT NULL, senha text NOT NULL, status text NOT NULL,
    prioridade integer NOT NULL DEFAULT 0, checkin_em timestamptz NOT NULL DEFAULT now(),
    chamado_em timestamptz NULL, finalizado_em timestamptz NULL, reg_update timestamptz NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_atendimentos_fila_checkin_ativo
    ON plantaopro.atendimentos_fila(cliente_id, agendamento_id)
    WHERE status NOT IN ('FINALIZADO','CANCELADO');
CREATE INDEX IF NOT EXISTS ix_atendimentos_fila_operacao
    ON plantaopro.atendimentos_fila(cliente_id, unidade_id, status, prioridade DESC, checkin_em);

CREATE TABLE IF NOT EXISTS plantaopro.triagem_historico (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, triagem_id uuid NOT NULL,
    evento text NOT NULL, usuario_id uuid NULL, reg_date timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_triagem_historico_tenant_triagem
    ON plantaopro.triagem_historico(cliente_id, triagem_id, reg_date DESC);

CREATE TABLE IF NOT EXISTS plantaopro.idempotencia_operacional (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, escopo text NOT NULL,
    chave_hash text NOT NULL, recurso_id uuid NULL, criado_em timestamptz NOT NULL DEFAULT now(),
    expira_em timestamptz NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_idempotencia_operacional
    ON plantaopro.idempotencia_operacional(cliente_id, escopo, chave_hash);

DO $$
DECLARE
    permitido_expr text := 'true';
    reg_status_expr text := '''A''';
    reg_date_expr text := 'now()';
BEGIN
    IF to_regclass('plantaopro.perfis_permissoes') IS NOT NULL THEN
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='plantaopro' AND table_name='perfis_permissoes' AND column_name='permitido') THEN
            permitido_expr := 'coalesce(permitido, true)';
        END IF;
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='plantaopro' AND table_name='perfis_permissoes' AND column_name='reg_status') THEN
            reg_status_expr := 'coalesce(reg_status, ''A'')';
        END IF;
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='plantaopro' AND table_name='perfis_permissoes' AND column_name='reg_date') THEN
            reg_date_expr := 'coalesce(reg_date, now())';
        END IF;
        EXECUTE format(
            'INSERT INTO plantaopro.perfil_permissoes(perfil_id, permissao_id, permitido, reg_status, reg_date) '
            'SELECT perfil_id, permissao_id, %s, %s, %s FROM plantaopro.perfis_permissoes '
            'ON CONFLICT (perfil_id, permissao_id) DO NOTHING',
            permitido_expr, reg_status_expr, reg_date_expr);
    END IF;
END $$;
