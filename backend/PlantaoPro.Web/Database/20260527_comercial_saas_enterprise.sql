-- Incremental PlantaoPro SaaS Enterprise
CREATE SCHEMA IF NOT EXISTS plantaopro;

CREATE TABLE IF NOT EXISTS plantaopro.leads (
    id uuid PRIMARY KEY,
    cliente_id uuid NULL,
    nome text NOT NULL,
    email text NULL,
    cnpj text NULL,
    telefone text NULL,
    status text NOT NULL DEFAULT 'NOVO',
    responsavel_id uuid NULL,
    criado_em timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.oportunidades (
    id uuid PRIMARY KEY,
    lead_id uuid NULL,
    cliente_id uuid NULL,
    titulo text NOT NULL,
    status text NOT NULL DEFAULT 'ABERTA',
    valor_potencial numeric(14,2) NOT NULL DEFAULT 0,
    motivo_perda text NULL,
    criado_em timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.propostas_comerciais (
    id uuid PRIMARY KEY,
    oportunidade_id uuid NULL,
    cliente_id uuid NULL,
    numero text NOT NULL,
    status text NOT NULL DEFAULT 'RASCUNHO',
    valor_total numeric(14,2) NOT NULL DEFAULT 0,
    motivo_recusa text NULL,
    criado_em timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.contratos_saas (
    id uuid PRIMARY KEY,
    cliente_id uuid NOT NULL,
    assinatura_id uuid NULL,
    numero text NOT NULL,
    objeto text NULL,
    data_inicio date NOT NULL,
    data_fim date NULL,
    valor_mensal numeric(14,2) NOT NULL DEFAULT 0,
    status text NOT NULL DEFAULT 'RASCUNHO',
    observacoes text NULL,
    criado_em timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_leads_email ON plantaopro.leads(email);
CREATE INDEX IF NOT EXISTS ix_leads_cnpj ON plantaopro.leads(cnpj);
CREATE INDEX IF NOT EXISTS ix_oportunidades_status ON plantaopro.oportunidades(status);
CREATE INDEX IF NOT EXISTS ix_propostas_status ON plantaopro.propostas_comerciais(status);
CREATE INDEX IF NOT EXISTS ix_contratos_saas_status ON plantaopro.contratos_saas(status);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_leads_status'
    ) THEN
        ALTER TABLE plantaopro.leads
            ADD CONSTRAINT ck_leads_status
            CHECK (status IN ('NOVO','CONTATADO','QUALIFICADO','DESCARTADO','CONVERTIDO'));
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_oportunidades_status'
    ) THEN
        ALTER TABLE plantaopro.oportunidades
            ADD CONSTRAINT ck_oportunidades_status
            CHECK (status IN ('ABERTA','EM_NEGOCIACAO','PROPOSTA_ENVIADA','GANHA','PERDIDA','CANCELADA'));
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_propostas_status'
    ) THEN
        ALTER TABLE plantaopro.propostas_comerciais
            ADD CONSTRAINT ck_propostas_status
            CHECK (status IN ('RASCUNHO','ENVIADA','APROVADA','RECUSADA','CANCELADA'));
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_contratos_saas_status'
    ) THEN
        ALTER TABLE plantaopro.contratos_saas
            ADD CONSTRAINT ck_contratos_saas_status
            CHECK (status IN ('RASCUNHO','ATIVO','SUSPENSO','ENCERRADO','CANCELADO'));
    END IF;
END $$;
