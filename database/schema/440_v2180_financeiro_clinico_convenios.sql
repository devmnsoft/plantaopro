-- v2.18.0 - base comercial do Saúde 360 (sem gateway, TISS ou integração externa).
SET search_path TO plantaopro, public;

-- Declarações de compatibilidade tornam a migration segura também quando aplicada
-- isoladamente; em upgrades, IF NOT EXISTS preserva integralmente as tabelas atuais.
CREATE TABLE IF NOT EXISTS plantaopro.clinica_contas_receber (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid, cliente_id uuid, paciente_id uuid,
    agendamento_id uuid, consulta_id uuid, descricao text NOT NULL DEFAULT '', valor_total numeric(14,2) NOT NULL DEFAULT 0,
    valor_pendente numeric(14,2) NOT NULL DEFAULT 0, vencimento date, status text NOT NULL DEFAULT 'ABERTO',
    created_by uuid, updated_by uuid, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz,
    reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz, reg_status char(1) NOT NULL DEFAULT 'A'
);
CREATE TABLE IF NOT EXISTS plantaopro.clinica_recebimentos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid, cliente_id uuid, conta_receber_id uuid,
    valor numeric(14,2) NOT NULL DEFAULT 0, forma_pagamento text NOT NULL DEFAULT '', status text NOT NULL DEFAULT 'CONFIRMADO',
    created_by uuid, updated_by uuid, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz,
    reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz, reg_status char(1) NOT NULL DEFAULT 'A'
);
CREATE TABLE IF NOT EXISTS plantaopro.clinica_caixa (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid, cliente_id uuid, saldo_inicial numeric(14,2) NOT NULL DEFAULT 0,
    status text NOT NULL DEFAULT 'ABERTO', created_by uuid, updated_by uuid, created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz, reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz, reg_status char(1) NOT NULL DEFAULT 'A'
);
CREATE TABLE IF NOT EXISTS plantaopro.plano_saude_pacientes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid, cliente_id uuid, paciente_id uuid, plano_saude_id uuid,
    numero_carteirinha text NOT NULL DEFAULT '', principal boolean NOT NULL DEFAULT false, validade date,
    status text NOT NULL DEFAULT 'ATIVO', created_by uuid, updated_by uuid, created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz, reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz, reg_status char(1) NOT NULL DEFAULT 'A'
);

ALTER TABLE IF EXISTS plantaopro.clinica_contas_receber
    ADD COLUMN IF NOT EXISTS convenio_id uuid,
    ADD COLUMN IF NOT EXISTS plano_saude_id uuid,
    ADD COLUMN IF NOT EXISTS forma_cobranca varchar(30),
    ADD COLUMN IF NOT EXISTS valor_desconto numeric(14,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS valor_coparticipacao numeric(14,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS cancelada_em timestamptz,
    ADD COLUMN IF NOT EXISTS cancelada_por uuid,
    ADD COLUMN IF NOT EXISTS motivo_cancelamento text;

ALTER TABLE IF EXISTS plantaopro.clinica_recebimentos
    ADD COLUMN IF NOT EXISTS caixa_id uuid,
    ADD COLUMN IF NOT EXISTS data_recebimento timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS estornado_em timestamptz,
    ADD COLUMN IF NOT EXISTS estornado_por uuid,
    ADD COLUMN IF NOT EXISTS justificativa_estorno text;

ALTER TABLE IF EXISTS plantaopro.clinica_caixa
    ADD COLUMN IF NOT EXISTS unidade_id uuid,
    ADD COLUMN IF NOT EXISTS aberto_em timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS fechado_em timestamptz,
    ADD COLUMN IF NOT EXISTS saldo_informado numeric(14,2),
    ADD COLUMN IF NOT EXISTS diferenca numeric(14,2),
    ADD COLUMN IF NOT EXISTS conferencia_observacao text;

CREATE TABLE IF NOT EXISTS plantaopro.fechamento_caixa (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL,
    caixa_id uuid NOT NULL, saldo_sistema numeric(14,2) NOT NULL,
    saldo_informado numeric(14,2) NOT NULL, diferenca numeric(14,2) NOT NULL,
    conferencia_observacao text NOT NULL, fechado_por uuid NOT NULL,
    fechado_em timestamptz NOT NULL DEFAULT now(), status varchar(20) NOT NULL DEFAULT 'FECHADO',
    created_by uuid, reg_date timestamptz NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A'
);

CREATE TABLE IF NOT EXISTS plantaopro.clinica_caixa_movimentos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL,
    caixa_id uuid NOT NULL, recebimento_id uuid, tipo varchar(30) NOT NULL,
    valor numeric(14,2) NOT NULL, forma_pagamento varchar(30), justificativa text,
    ajuste_pos_fechamento boolean NOT NULL DEFAULT false, created_by uuid,
    reg_date timestamptz NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A'
);

CREATE TABLE IF NOT EXISTS plantaopro.convenio_contratos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, convenio_id uuid NOT NULL,
    numero varchar(80) NOT NULL, inicio_vigencia date NOT NULL, fim_vigencia date NOT NULL,
    exige_autorizacao boolean NOT NULL DEFAULT false, regra_coparticipacao jsonb NOT NULL DEFAULT '{}'::jsonb,
    status varchar(20) NOT NULL DEFAULT 'ATIVO', created_by uuid, updated_by uuid,
    reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz, reg_status char(1) NOT NULL DEFAULT 'A'
);

CREATE TABLE IF NOT EXISTS plantaopro.convenio_tabelas_procedimentos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, convenio_id uuid NOT NULL,
    contrato_id uuid, procedimento_id uuid, codigo varchar(80) NOT NULL, descricao text NOT NULL,
    valor numeric(14,2) NOT NULL, exige_autorizacao boolean NOT NULL DEFAULT false,
    status varchar(20) NOT NULL DEFAULT 'ATIVO', created_by uuid,
    reg_date timestamptz NOT NULL DEFAULT now(), reg_status char(1) NOT NULL DEFAULT 'A'
);

CREATE TABLE IF NOT EXISTS plantaopro.convenio_glosas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, convenio_id uuid NOT NULL,
    conta_receber_id uuid NOT NULL, autorizacao_id uuid, motivo text NOT NULL,
    valor_glosado numeric(14,2) NOT NULL, status varchar(20) NOT NULL DEFAULT 'ABERTA',
    justificativa_recurso text, recurso_em timestamptz, resolvida_em timestamptz,
    created_by uuid, updated_by uuid, reg_date timestamptz NOT NULL DEFAULT now(),
    reg_update timestamptz, reg_status char(1) NOT NULL DEFAULT 'A'
);

ALTER TABLE IF EXISTS plantaopro.plano_saude_pacientes
    ADD COLUMN IF NOT EXISTS titularidade varchar(20) NOT NULL DEFAULT 'TITULAR',
    ADD COLUMN IF NOT EXISTS titular_nome varchar(160),
    ADD COLUMN IF NOT EXISTS cobertura_resumo text,
    ADD COLUMN IF NOT EXISTS inativado_em timestamptz;

CREATE TABLE IF NOT EXISTS plantaopro.regras_repasse_medico (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, medico_id uuid NOT NULL,
    procedimento_id uuid, tipo_regra varchar(20) NOT NULL, percentual numeric(7,4), valor_fixo numeric(14,2),
    vigencia_inicio date NOT NULL, vigencia_fim date, status varchar(20) NOT NULL DEFAULT 'ATIVO',
    created_by uuid, updated_by uuid, reg_date timestamptz NOT NULL DEFAULT now(),
    reg_update timestamptz, reg_status char(1) NOT NULL DEFAULT 'A'
);

CREATE TABLE IF NOT EXISTS plantaopro.repasses_medicos_clinicos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, medico_id uuid NOT NULL,
    consulta_id uuid NOT NULL, recebimento_id uuid, regra_repasse_id uuid,
    valor_base numeric(14,2) NOT NULL, valor_repasse numeric(14,2) NOT NULL,
    status varchar(20) NOT NULL DEFAULT 'PENDENTE', motivo_contestacao text,
    justificativa_cancelamento text, pago_em timestamptz, created_by uuid, updated_by uuid,
    reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz, reg_status char(1) NOT NULL DEFAULT 'A'
);

CREATE TABLE IF NOT EXISTS plantaopro.auditoria_financeira_clinica (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), cliente_id uuid NOT NULL, usuario_id uuid,
    entidade varchar(80) NOT NULL, entidade_id uuid, acao varchar(80) NOT NULL,
    detalhes jsonb NOT NULL DEFAULT '{}'::jsonb, ocorrido_em timestamptz NOT NULL DEFAULT now(),
    reg_status char(1) NOT NULL DEFAULT 'A'
);

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_repasse_valores_nao_negativos') THEN
        ALTER TABLE plantaopro.repasses_medicos_clinicos ADD CONSTRAINT ck_repasse_valores_nao_negativos CHECK (valor_base >= 0 AND valor_repasse >= 0);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_glosa_valor_positivo') THEN
        ALTER TABLE plantaopro.convenio_glosas ADD CONSTRAINT ck_glosa_valor_positivo CHECK (valor_glosado > 0);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_contrato_vigencia') THEN
        ALTER TABLE plantaopro.convenio_contratos ADD CONSTRAINT ck_contrato_vigencia CHECK (fim_vigencia >= inicio_vigencia);
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_repasse_consulta_ativo ON plantaopro.repasses_medicos_clinicos(cliente_id,consulta_id) WHERE reg_status='A' AND status<>'CANCELADO';
CREATE UNIQUE INDEX IF NOT EXISTS ux_recebimento_conta_confirmado ON plantaopro.clinica_recebimentos(cliente_id,conta_receber_id) WHERE reg_status='A' AND status='CONFIRMADO';
CREATE INDEX IF NOT EXISTS ix_conta_clinica_tenant_status_vencimento ON plantaopro.clinica_contas_receber(cliente_id,status,vencimento);
CREATE INDEX IF NOT EXISTS ix_caixa_tenant_status_data ON plantaopro.clinica_caixa(cliente_id,status,aberto_em);
CREATE INDEX IF NOT EXISTS ix_contrato_tenant_convenio_vigencia ON plantaopro.convenio_contratos(cliente_id,convenio_id,fim_vigencia);
CREATE INDEX IF NOT EXISTS ix_glosa_tenant_status_convenio ON plantaopro.convenio_glosas(cliente_id,status,convenio_id);
CREATE INDEX IF NOT EXISTS ix_plano_paciente_tenant_paciente_status ON plantaopro.plano_saude_pacientes(cliente_id,paciente_id,status);
CREATE INDEX IF NOT EXISTS ix_repasse_tenant_medico_status ON plantaopro.repasses_medicos_clinicos(cliente_id,medico_id,status);
CREATE INDEX IF NOT EXISTS ix_auditoria_financeira_tenant_data ON plantaopro.auditoria_financeira_clinica(cliente_id,ocorrido_em DESC);
