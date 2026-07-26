-- PlantãoPro v1.20.1 - Experiência premium Meu Dia
CREATE TABLE IF NOT EXISTS plantaopro.usuario_preferencias_interface (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id uuid NOT NULL,
    chave text NOT NULL,
    valor jsonb NOT NULL DEFAULT '{}'::jsonb,
    reg_date timestamptz NOT NULL DEFAULT now(),
    reg_status char(1) NOT NULL DEFAULT 'A',
    UNIQUE (usuario_id, chave)
);

CREATE TABLE IF NOT EXISTS plantaopro.meu_dia_item_estados (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id uuid NOT NULL,
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    item_origem_tipo text NOT NULL,
    item_origem_id uuid NOT NULL,
    status text NOT NULL DEFAULT 'ABERTO',
    adiado_ate timestamptz NULL,
    reg_date timestamptz NOT NULL DEFAULT now(),
    reg_status char(1) NOT NULL DEFAULT 'A',
    UNIQUE (usuario_id, item_origem_tipo, item_origem_id)
);

CREATE TABLE IF NOT EXISTS plantaopro.meu_dia_historico (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id uuid NOT NULL,
    tenant_id uuid NULL,
    cliente_id uuid NULL,
    item_estado_id uuid NULL,
    evento text NOT NULL,
    detalhes jsonb NOT NULL DEFAULT '{}'::jsonb,
    reg_date timestamptz NOT NULL DEFAULT now(),
    reg_status char(1) NOT NULL DEFAULT 'A'
);

CREATE INDEX IF NOT EXISTS ix_meu_dia_item_estados_usuario ON plantaopro.meu_dia_item_estados(usuario_id, status, reg_status);
CREATE INDEX IF NOT EXISTS ix_meu_dia_historico_usuario ON plantaopro.meu_dia_historico(usuario_id, reg_date DESC);
