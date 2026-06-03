-- PlantaoPro MVP Comercial Avançado - faturamento SaaS, limites e observabilidade.
-- Script incremental seguro para PostgreSQL.

CREATE SCHEMA IF NOT EXISTS plantaopro;

ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_mobile boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_bi boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_relatorios_avancados boolean NOT NULL DEFAULT true;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_integracoes boolean NOT NULL DEFAULT false;

CREATE TABLE IF NOT EXISTS plantaopro.faturas_saas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    assinatura_id uuid NOT NULL,
    competencia date NOT NULL,
    valor numeric(14,2) NOT NULL DEFAULT 0,
    vencimento date NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'ABERTA',
    valor_pago numeric(14,2) NULL,
    data_pagamento date NULL,
    forma_pagamento varchar(60) NULL,
    motivo_cancelamento text NULL,
    motivo_contestacao text NULL,
    reg_status char(1) NOT NULL DEFAULT 'A',
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.fatura_itens (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    fatura_id uuid NOT NULL,
    descricao varchar(180) NOT NULL,
    quantidade numeric(12,2) NOT NULL DEFAULT 1,
    valor_unitario numeric(14,2) NOT NULL DEFAULT 0,
    valor_total numeric(14,2) NOT NULL DEFAULT 0,
    reg_status char(1) NOT NULL DEFAULT 'A',
    criado_em timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.pagamentos_saas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    fatura_id uuid NOT NULL,
    valor_pago numeric(14,2) NOT NULL,
    data_pagamento date NOT NULL,
    forma_pagamento varchar(60) NOT NULL,
    observacoes text NULL,
    reg_status char(1) NOT NULL DEFAULT 'A',
    criado_em timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.customer_success_interacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    usuario_id uuid NULL,
    tipo varchar(40) NOT NULL DEFAULT 'CONTATO',
    resumo varchar(220) NOT NULL,
    descricao text NULL,
    proxima_acao text NULL,
    data_interacao timestamptz NOT NULL DEFAULT now(),
    reg_status char(1) NOT NULL DEFAULT 'A'
);

CREATE TABLE IF NOT EXISTS plantaopro.customer_success_planos_acao (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    titulo varchar(180) NOT NULL,
    descricao text NULL,
    responsavel varchar(120) NULL,
    status varchar(30) NOT NULL DEFAULT 'ABERTO',
    prazo date NULL,
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NOT NULL DEFAULT now(),
    reg_status char(1) NOT NULL DEFAULT 'A'
);

CREATE INDEX IF NOT EXISTS ix_faturas_saas_cliente_status ON plantaopro.faturas_saas(cliente_id, status, reg_status);
CREATE INDEX IF NOT EXISTS ix_faturas_saas_competencia ON plantaopro.faturas_saas(competencia, reg_status);
CREATE INDEX IF NOT EXISTS ix_faturas_saas_vencimento ON plantaopro.faturas_saas(vencimento, status);
CREATE INDEX IF NOT EXISTS ix_fatura_itens_fatura ON plantaopro.fatura_itens(fatura_id, reg_status);
CREATE INDEX IF NOT EXISTS ix_pagamentos_saas_fatura ON plantaopro.pagamentos_saas(fatura_id, reg_status);
CREATE INDEX IF NOT EXISTS ix_customer_success_interacoes_cliente ON plantaopro.customer_success_interacoes(cliente_id, data_interacao DESC);
CREATE INDEX IF NOT EXISTS ix_customer_success_planos_cliente_status ON plantaopro.customer_success_planos_acao(cliente_id, status, reg_status);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_faturas_saas_cliente'
          AND conrelid = 'plantaopro.faturas_saas'::regclass
    ) THEN
        ALTER TABLE plantaopro.faturas_saas
        ADD CONSTRAINT fk_faturas_saas_cliente
        FOREIGN KEY (cliente_id) REFERENCES plantaopro.clientes(id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_faturas_saas_assinatura'
          AND conrelid = 'plantaopro.faturas_saas'::regclass
    ) THEN
        ALTER TABLE plantaopro.faturas_saas
        ADD CONSTRAINT fk_faturas_saas_assinatura
        FOREIGN KEY (assinatura_id) REFERENCES plantaopro.assinaturas(id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'uq_faturas_saas_assinatura_competencia'
          AND conrelid = 'plantaopro.faturas_saas'::regclass
    ) THEN
        ALTER TABLE plantaopro.faturas_saas
        ADD CONSTRAINT uq_faturas_saas_assinatura_competencia
        UNIQUE (assinatura_id, competencia);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_faturas_saas_status'
          AND conrelid = 'plantaopro.faturas_saas'::regclass
    ) THEN
        ALTER TABLE plantaopro.faturas_saas
        ADD CONSTRAINT ck_faturas_saas_status
        CHECK (status IN ('ABERTA','PAGA','VENCIDA','CANCELADA','EM_CONTESTACAO'));
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_faturas_saas_valores'
          AND conrelid = 'plantaopro.faturas_saas'::regclass
    ) THEN
        ALTER TABLE plantaopro.faturas_saas
        ADD CONSTRAINT ck_faturas_saas_valores
        CHECK (valor >= 0 AND (valor_pago IS NULL OR valor_pago >= 0));
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_fatura_itens_fatura'
          AND conrelid = 'plantaopro.fatura_itens'::regclass
    ) THEN
        ALTER TABLE plantaopro.fatura_itens
        ADD CONSTRAINT fk_fatura_itens_fatura
        FOREIGN KEY (fatura_id) REFERENCES plantaopro.faturas_saas(id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_pagamentos_saas_fatura'
          AND conrelid = 'plantaopro.pagamentos_saas'::regclass
    ) THEN
        ALTER TABLE plantaopro.pagamentos_saas
        ADD CONSTRAINT fk_pagamentos_saas_fatura
        FOREIGN KEY (fatura_id) REFERENCES plantaopro.faturas_saas(id);
    END IF;
END $$;
