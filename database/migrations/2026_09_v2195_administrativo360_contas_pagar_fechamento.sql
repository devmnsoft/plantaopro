-- Migration 2026_09_v2195_administrativo360_contas_pagar_fechamento.sql
-- Jornada: Compra/Recebimento -> Contas a Pagar -> Pagamento Manual -> Estorno -> Caixa Consolidado -> Fechamento

DO $$
BEGIN
    ALTER TABLE plantaopro.adm360_contas_financeiras
    ADD COLUMN IF NOT EXISTS data_saldo_inicial date NOT NULL DEFAULT '2000-01-01';
EXCEPTION
    WHEN OTHERS THEN NULL;
END $$;

-- 1. Sequência para títulos a pagar
CREATE SEQUENCE IF NOT EXISTS plantaopro.adm360_titulo_pagar_numero START 1;

-- 2. Tabela de Contas a Pagar (Obrigações)
CREATE TABLE IF NOT EXISTS plantaopro.adm360_titulos_pagar (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    numero varchar(40) NOT NULL,
    origem_tipo varchar(30) NOT NULL CHECK(origem_tipo IN ('RECEBIMENTO_COMPRA', 'DESPESA_MANUAL', 'COMISSAO_VENDEDOR')),
    origem_id uuid,
    fornecedor_id uuid NOT NULL REFERENCES plantaopro.adm360_parceiros(id),
    documento varchar(80),
    competencia date NOT NULL,
    data_emissao date NOT NULL,
    data_vencimento date NOT NULL,
    parcela integer NOT NULL DEFAULT 1 CHECK(parcela >= 1),
    total_parcelas integer NOT NULL DEFAULT 1 CHECK(total_parcelas >= 1),
    valor_principal numeric(18,4) NOT NULL CHECK(valor_principal > 0),
    valor_desconto numeric(18,4) NOT NULL DEFAULT 0 CHECK(valor_desconto >= 0),
    valor_juros numeric(18,4) NOT NULL DEFAULT 0 CHECK(valor_juros >= 0),
    valor_pago numeric(18,4) NOT NULL DEFAULT 0 CHECK(valor_pago >= 0),
    saldo_aberto numeric(18,4) NOT NULL CHECK(saldo_aberto >= 0),
    centro_custo varchar(80),
    situacao varchar(25) NOT NULL DEFAULT 'PENDENTE_APROVACAO' CHECK(situacao IN ('PENDENTE_APROVACAO', 'APROVADO', 'PARCIAL', 'PAGO', 'CANCELADO')),
    observacoes text,
    aprovado_por uuid,
    aprovado_em timestamptz,
    idempotency_key varchar(120),
    versao bigint NOT NULL DEFAULT 1,
    created_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, numero),
    UNIQUE(tenant_id, idempotency_key)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_adm360_titulos_pagar_origem ON plantaopro.adm360_titulos_pagar(tenant_id, origem_tipo, origem_id, parcela) WHERE origem_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_adm360_titulos_pagar_fornecedor ON plantaopro.adm360_titulos_pagar(tenant_id, fornecedor_id, situacao);
CREATE INDEX IF NOT EXISTS ix_adm360_titulos_pagar_venc ON plantaopro.adm360_titulos_pagar(tenant_id, data_vencimento, situacao);

-- 3. Tabela de Pagamentos Manuais (Baixas de AP)
CREATE TABLE IF NOT EXISTS plantaopro.adm360_titulo_pagamentos (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    titulo_id uuid NOT NULL REFERENCES plantaopro.adm360_titulos_pagar(id),
    conta_id uuid NOT NULL REFERENCES plantaopro.adm360_contas_financeiras(id),
    data_pagamento date NOT NULL,
    valor_pago numeric(18,4) NOT NULL CHECK(valor_pago > 0),
    meio_pagamento varchar(30) NOT NULL,
    referencia varchar(120),
    estornado boolean NOT NULL DEFAULT false,
    movimento_financeiro_id uuid REFERENCES plantaopro.adm360_movimentos_financeiros(id),
    idempotency_key varchar(120) NOT NULL,
    pago_por uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, idempotency_key)
);

CREATE INDEX IF NOT EXISTS ix_adm360_titpag_tenant_tit ON plantaopro.adm360_titulo_pagamentos(tenant_id, titulo_id);
CREATE INDEX IF NOT EXISTS ix_adm360_titpag_tenant_data ON plantaopro.adm360_titulo_pagamentos(tenant_id, data_pagamento);

-- 4. Tabela de Estornos de Pagamento
CREATE TABLE IF NOT EXISTS plantaopro.adm360_pagamento_estornos (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    pagamento_id uuid NOT NULL REFERENCES plantaopro.adm360_titulo_pagamentos(id),
    titulo_id uuid NOT NULL REFERENCES plantaopro.adm360_titulos_pagar(id),
    valor_estornado numeric(18,4) NOT NULL CHECK(valor_estornado > 0),
    motivo text NOT NULL,
    movimento_financeiro_id uuid REFERENCES plantaopro.adm360_movimentos_financeiros(id),
    estornado_por uuid,
    idempotency_key varchar(120) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, idempotency_key)
);

-- 5. Fechamento de Caixa por Conta e Período/Data
CREATE TABLE IF NOT EXISTS plantaopro.adm360_caixa_fechamentos (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    conta_id uuid NOT NULL REFERENCES plantaopro.adm360_contas_financeiras(id),
    data_inicio date,
    data_fim date,
    data_fechamento date,
    saldo_abertura numeric(18,4) NOT NULL,
    total_entradas numeric(18,4) NOT NULL DEFAULT 0,
    total_saidas numeric(18,4) NOT NULL DEFAULT 0,
    entradas numeric(18,4) NOT NULL DEFAULT 0,
    saidas numeric(18,4) NOT NULL DEFAULT 0,
    saldo_calculado numeric(18,4) NOT NULL,
    saldo_conferido numeric(18,4) NOT NULL,
    diferenca numeric(18,4) NOT NULL,
    justificativa text,
    situacao varchar(20) NOT NULL DEFAULT 'FECHADO' CHECK(situacao IN ('FECHADO', 'REABERTO')),
    motivo_reabertura text,
    fechado_por uuid,
    reaberto_por uuid,
    fechado_em timestamptz NOT NULL DEFAULT now(),
    reaberto_em timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id)
);

DO $$
BEGIN
    ALTER TABLE plantaopro.adm360_titulo_pagamentos
        ADD COLUMN IF NOT EXISTS movimento_financeiro_id uuid REFERENCES plantaopro.adm360_movimentos_financeiros(id);

    ALTER TABLE plantaopro.adm360_pagamento_estornos
        ADD COLUMN IF NOT EXISTS movimento_financeiro_id uuid REFERENCES plantaopro.adm360_movimentos_financeiros(id);

    ALTER TABLE plantaopro.adm360_caixa_fechamentos
        ADD COLUMN IF NOT EXISTS data_inicio date,
        ADD COLUMN IF NOT EXISTS data_fim date,
        ADD COLUMN IF NOT EXISTS total_entradas numeric(18,4) NOT NULL DEFAULT 0,
        ADD COLUMN IF NOT EXISTS total_saidas numeric(18,4) NOT NULL DEFAULT 0,
        ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now();
EXCEPTION
    WHEN OTHERS THEN NULL;
END $$;

CREATE INDEX IF NOT EXISTS ix_adm360_caixa_fech_tenant_conta ON plantaopro.adm360_caixa_fechamentos(tenant_id, conta_id, data_fim);

-- 6. Suporte a comissão vinculada a título a pagar
DO $$
BEGIN
    ALTER TABLE plantaopro.adm360_comissoes_apropriadas
    ADD COLUMN IF NOT EXISTS titulo_pagar_id uuid REFERENCES plantaopro.adm360_titulos_pagar(id);

    ALTER TABLE plantaopro.adm360_comissoes_apropriadas
    DROP CONSTRAINT IF EXISTS adm360_comissoes_apropriadas_situacao_check;

    ALTER TABLE plantaopro.adm360_comissoes_apropriadas
    ADD CONSTRAINT adm360_comissoes_apropriadas_situacao_check
    CHECK(situacao IN ('APROPRIADA', 'A_PAGAR', 'PAGA', 'ESTORNADA', 'AJUSTE_PENDENTE'));
EXCEPTION
    WHEN OTHERS THEN NULL;
END $$;
