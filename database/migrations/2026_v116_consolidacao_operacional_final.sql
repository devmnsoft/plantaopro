CREATE SCHEMA IF NOT EXISTS plantaopro;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS plantaopro.v116_convenio_autorizacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_convenio_autorizacoes ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_convenio_autorizacoes ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_convenio_autorizacoes ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_convenio_autorizacoes ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_convenio_autorizacoes ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_convenio_autorizacoes ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_convenio_autorizacoes ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_convenio_autorizacoes_tenant_status_data ON plantaopro.v116_convenio_autorizacoes(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_convenio_guias (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_convenio_guias ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_convenio_guias ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_convenio_guias ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_convenio_guias ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_convenio_guias ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_convenio_guias ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_convenio_guias ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_convenio_guias_tenant_status_data ON plantaopro.v116_convenio_guias(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_faturamento_lotes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lotes ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lotes ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lotes ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lotes ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lotes ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lotes ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lotes ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_faturamento_lotes_tenant_status_data ON plantaopro.v116_faturamento_lotes(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_faturamento_lote_itens (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lote_itens ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lote_itens ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lote_itens ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lote_itens ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lote_itens ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lote_itens ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_faturamento_lote_itens ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_faturamento_lote_itens_tenant_status_data ON plantaopro.v116_faturamento_lote_itens(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_caixas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_caixas ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_caixas ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_caixas ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_caixas ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_caixas ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_caixas ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_caixas ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_caixas_tenant_status_data ON plantaopro.v116_caixas(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_caixa_movimentos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_caixa_movimentos ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_caixa_movimentos ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_caixa_movimentos ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_caixa_movimentos ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_caixa_movimentos ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_caixa_movimentos ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_caixa_movimentos ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_caixa_movimentos_tenant_status_data ON plantaopro.v116_caixa_movimentos(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_recebimentos_parciais (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_recebimentos_parciais ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_recebimentos_parciais ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_recebimentos_parciais ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_recebimentos_parciais ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_recebimentos_parciais ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_recebimentos_parciais ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_recebimentos_parciais ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_recebimentos_parciais_tenant_status_data ON plantaopro.v116_recebimentos_parciais(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_estornos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_estornos ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_estornos ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_estornos ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_estornos ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_estornos ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_estornos ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_estornos ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_estornos_tenant_status_data ON plantaopro.v116_estornos(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_timelines (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_timelines ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_timelines ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_timelines ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_timelines ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_timelines ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_timelines ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_timelines ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_timelines_tenant_status_data ON plantaopro.v116_timelines(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_notificacoes_operacionais (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_notificacoes_operacionais ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_notificacoes_operacionais ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_notificacoes_operacionais ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_notificacoes_operacionais ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_notificacoes_operacionais ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_notificacoes_operacionais ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_notificacoes_operacionais ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_notificacoes_operacionais_tenant_status_data ON plantaopro.v116_notificacoes_operacionais(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_relatorios_execucoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_relatorios_execucoes ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_relatorios_execucoes ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_relatorios_execucoes ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_relatorios_execucoes ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_relatorios_execucoes ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_relatorios_execucoes ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_relatorios_execucoes ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_relatorios_execucoes_tenant_status_data ON plantaopro.v116_relatorios_execucoes(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_auditoria_consultas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_auditoria_consultas ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_auditoria_consultas ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_auditoria_consultas ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_auditoria_consultas ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_auditoria_consultas ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_auditoria_consultas ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_auditoria_consultas ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_auditoria_consultas_tenant_status_data ON plantaopro.v116_auditoria_consultas(tenant_id, reg_status, created_at);

CREATE TABLE IF NOT EXISTS plantaopro.v116_integracao_provedores (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    reg_status varchar(40) NOT NULL DEFAULT 'ATIVO',
    descricao text NULL,
    status_operacional varchar(60) NULL,
    valor numeric(14,2) NULL,
    data_referencia timestamptz NULL,
    payload_demo jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NULL,
    updated_at timestamptz NULL,
    updated_by varchar(120) NULL
);

ALTER TABLE IF EXISTS plantaopro.v116_integracao_provedores ADD COLUMN IF NOT EXISTS cliente_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_integracao_provedores ADD COLUMN IF NOT EXISTS tenant_id uuid;

ALTER TABLE IF EXISTS plantaopro.v116_integracao_provedores ADD COLUMN IF NOT EXISTS reg_status varchar(40);

ALTER TABLE IF EXISTS plantaopro.v116_integracao_provedores ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_integracao_provedores ADD COLUMN IF NOT EXISTS created_by varchar(120);

ALTER TABLE IF EXISTS plantaopro.v116_integracao_provedores ADD COLUMN IF NOT EXISTS updated_at timestamptz;

ALTER TABLE IF EXISTS plantaopro.v116_integracao_provedores ADD COLUMN IF NOT EXISTS updated_by varchar(120);

CREATE INDEX IF NOT EXISTS ix_v116_integracao_provedores_tenant_status_data ON plantaopro.v116_integracao_provedores(tenant_id, reg_status, created_at);
