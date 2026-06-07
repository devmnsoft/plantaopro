-- ============================================================================
-- PlantãoPro SaaS Inteligente - regras comerciais, limites, faturamento e CS
-- Script incremental idempotente para PostgreSQL
-- ============================================================================

CREATE SCHEMA IF NOT EXISTS plantaopro;

-- Clientes SaaS: complementos comerciais sem quebrar seeds existentes.
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS responsavel_nome varchar(180);
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS responsavel_email varchar(180);
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS responsavel_telefone varchar(40);
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS observacoes text;
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS data_inicio_contrato date;
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS data_cancelamento timestamptz;
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS motivo_cancelamento text;
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS data_suspensao timestamptz;
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS motivo_suspensao text;
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS saude_status varchar(20) NOT NULL DEFAULT 'ATENCAO';
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS saude_score int NOT NULL DEFAULT 50;
ALTER TABLE plantaopro.clientes ADD COLUMN IF NOT EXISTS ultimo_recalculo_saude timestamptz;

-- Planos e recursos.
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS valor_anual numeric(14,2) NOT NULL DEFAULT 0;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS limite_usuarios int;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS limite_convites_mes int;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_mobile boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_bi boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_api boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_integracoes boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_relatorios_avancados boolean NOT NULL DEFAULT true;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_suporte_prioritario boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS permite_operacao_assistida boolean NOT NULL DEFAULT false;
ALTER TABLE plantaopro.planos ADD COLUMN IF NOT EXISTS ordem_exibicao int NOT NULL DEFAULT 0;

CREATE TABLE IF NOT EXISTS plantaopro.plano_recursos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    plano_id uuid NOT NULL,
    codigo varchar(80) NOT NULL,
    nome varchar(140) NOT NULL,
    descricao text,
    habilitado boolean NOT NULL DEFAULT true,
    limite int,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now(),
    reg_update timestamptz
);

ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS data_trial_fim timestamptz;
ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS periodicidade varchar(20) NOT NULL DEFAULT 'MENSAL';
ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS motivo_suspensao text;
ALTER TABLE plantaopro.assinaturas ADD COLUMN IF NOT EXISTS data_suspensao timestamptz;

CREATE TABLE IF NOT EXISTS plantaopro.assinatura_historico (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    assinatura_id uuid NOT NULL,
    cliente_id uuid NOT NULL,
    plano_id_anterior uuid,
    plano_id_novo uuid,
    status_anterior varchar(30),
    status_novo varchar(30),
    acao varchar(60) NOT NULL,
    justificativa text,
    usuario_id uuid,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.assinatura_uso (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    assinatura_id uuid,
    cliente_id uuid NOT NULL,
    recurso varchar(80) NOT NULL,
    quantidade int NOT NULL DEFAULT 0,
    competencia date NOT NULL DEFAULT date_trunc('month', now())::date,
    origem varchar(80),
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now()
);

-- Faturamento SaaS.
CREATE TABLE IF NOT EXISTS plantaopro.faturas_saas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    assinatura_id uuid NOT NULL,
    competencia date NOT NULL,
    valor numeric(14,2) NOT NULL DEFAULT 0,
    vencimento date NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'ABERTA',
    valor_pago numeric(14,2),
    data_pagamento date,
    forma_pagamento varchar(60),
    motivo_cancelamento text,
    motivo_contestacao text,
    resposta_contestacao text,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now(),
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
    reg_date timestamptz NOT NULL DEFAULT now(),
    criado_em timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.pagamentos_saas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    fatura_id uuid NOT NULL,
    cliente_id uuid,
    valor_pago numeric(14,2) NOT NULL,
    data_pagamento date NOT NULL,
    forma_pagamento varchar(60) NOT NULL,
    observacoes text,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now(),
    criado_em timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.cobranca_eventos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    fatura_id uuid NOT NULL,
    cliente_id uuid NOT NULL,
    tipo varchar(60) NOT NULL,
    canal varchar(40),
    mensagem text,
    sucesso boolean NOT NULL DEFAULT true,
    usuario_id uuid,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now()
);

-- Customer Success e saúde do cliente.
CREATE TABLE IF NOT EXISTS plantaopro.customer_success_interacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    usuario_id uuid,
    tipo varchar(40) NOT NULL DEFAULT 'CONTATO',
    risco varchar(30),
    resumo varchar(220) NOT NULL,
    descricao text,
    proxima_acao text,
    data_interacao timestamptz NOT NULL DEFAULT now(),
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.customer_success_planos_acao (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    titulo varchar(180) NOT NULL,
    descricao text,
    responsavel varchar(120),
    status varchar(30) NOT NULL DEFAULT 'ABERTO',
    prioridade varchar(20) NOT NULL DEFAULT 'MEDIA',
    prazo date,
    concluido_em timestamptz,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now(),
    criado_em timestamptz NOT NULL DEFAULT now(),
    atualizado_em timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.cliente_saude_historico (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    score int NOT NULL DEFAULT 0,
    classificacao varchar(20) NOT NULL,
    riscos text,
    oportunidades text,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.cliente_alertas (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tipo varchar(60) NOT NULL,
    severidade varchar(20) NOT NULL DEFAULT 'MEDIA',
    titulo varchar(180) NOT NULL,
    mensagem text NOT NULL,
    resolvido boolean NOT NULL DEFAULT false,
    resolvido_em timestamptz,
    usuario_resolucao_id uuid,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.cliente_bloqueios (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    tipo varchar(80) NOT NULL,
    motivo text NOT NULL,
    origem varchar(80) NOT NULL DEFAULT 'SISTEMA',
    usuario_id uuid,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS plantaopro.cliente_limites_uso (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NOT NULL,
    assinatura_id uuid,
    recurso varchar(80) NOT NULL,
    usado int NOT NULL DEFAULT 0,
    limite int,
    percentual numeric(8,4) NOT NULL DEFAULT 0,
    competencia date NOT NULL DEFAULT date_trunc('month', now())::date,
    reg_status char(1) NOT NULL DEFAULT 'A',
    reg_date timestamptz NOT NULL DEFAULT now(),
    reg_update timestamptz
);

-- Índices.
CREATE UNIQUE INDEX IF NOT EXISTS ux_clientes_cnpj_ativo ON plantaopro.clientes(cnpj) WHERE reg_status='A' AND cnpj IS NOT NULL AND cnpj <> '';
CREATE INDEX IF NOT EXISTS ix_clientes_status_reg_date ON plantaopro.clientes(status, reg_date);
CREATE INDEX IF NOT EXISTS ix_clientes_saude_status ON plantaopro.clientes(saude_status, reg_status);
CREATE INDEX IF NOT EXISTS ix_planos_status_ordem ON plantaopro.planos(status, ordem_exibicao, reg_status);
CREATE INDEX IF NOT EXISTS ix_plano_recursos_plano_codigo ON plantaopro.plano_recursos(plano_id, codigo, reg_status);
CREATE INDEX IF NOT EXISTS ix_assinaturas_cliente_status_reg_date ON plantaopro.assinaturas(cliente_id, status, reg_date);
CREATE INDEX IF NOT EXISTS ix_assinaturas_plano_status ON plantaopro.assinaturas(plano_id, status, reg_status);
CREATE INDEX IF NOT EXISTS ix_assinatura_historico_cliente_reg_date ON plantaopro.assinatura_historico(cliente_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_assinatura_uso_cliente_competencia ON plantaopro.assinatura_uso(cliente_id, competencia, recurso);
CREATE INDEX IF NOT EXISTS ix_faturas_saas_cliente_status ON plantaopro.faturas_saas(cliente_id, status, reg_status);
CREATE INDEX IF NOT EXISTS ix_faturas_saas_competencia ON plantaopro.faturas_saas(competencia, reg_status);
CREATE INDEX IF NOT EXISTS ix_faturas_saas_vencimento_status ON plantaopro.faturas_saas(vencimento, status, reg_status);
CREATE INDEX IF NOT EXISTS ix_fatura_itens_fatura ON plantaopro.fatura_itens(fatura_id, reg_status);
CREATE INDEX IF NOT EXISTS ix_pagamentos_saas_fatura ON plantaopro.pagamentos_saas(fatura_id, reg_status);
CREATE INDEX IF NOT EXISTS ix_cobranca_eventos_cliente_reg_date ON plantaopro.cobranca_eventos(cliente_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_customer_success_interacoes_cliente ON plantaopro.customer_success_interacoes(cliente_id, data_interacao DESC);
CREATE INDEX IF NOT EXISTS ix_customer_success_planos_cliente_status ON plantaopro.customer_success_planos_acao(cliente_id, status, reg_status);
CREATE INDEX IF NOT EXISTS ix_cliente_saude_historico_cliente_reg_date ON plantaopro.cliente_saude_historico(cliente_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_cliente_saude_historico_classificacao ON plantaopro.cliente_saude_historico(classificacao, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_cliente_alertas_cliente_status ON plantaopro.cliente_alertas(cliente_id, resolvido, reg_status);
CREATE INDEX IF NOT EXISTS ix_cliente_alertas_tipo_reg_date ON plantaopro.cliente_alertas(tipo, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_cliente_bloqueios_cliente_reg_date ON plantaopro.cliente_bloqueios(cliente_id, reg_date DESC);
CREATE INDEX IF NOT EXISTS ix_cliente_limites_uso_cliente_competencia ON plantaopro.cliente_limites_uso(cliente_id, competencia, recurso);

-- Constraints idempotentes.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_clientes_status_saas' AND conrelid='plantaopro.clientes'::regclass) THEN
        ALTER TABLE plantaopro.clientes ADD CONSTRAINT ck_clientes_status_saas CHECK (status IN ('TESTE','ATIVO','SUSPENSO','CANCELADO','INATIVO'));
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_planos_status_saas' AND conrelid='plantaopro.planos'::regclass) THEN
        ALTER TABLE plantaopro.planos ADD CONSTRAINT ck_planos_status_saas CHECK (status IN ('ATIVO','INATIVO','ARQUIVADO'));
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_planos_valores_limites_saas' AND conrelid='plantaopro.planos'::regclass) THEN
        ALTER TABLE plantaopro.planos ADD CONSTRAINT ck_planos_valores_limites_saas CHECK (
            valor_mensal >= 0 AND valor_anual >= 0
            AND (limite_medicos IS NULL OR limite_medicos >= 0)
            AND (limite_hospitais IS NULL OR limite_hospitais >= 0)
            AND (limite_usuarios IS NULL OR limite_usuarios >= 0)
            AND (limite_plantoes_mes IS NULL OR limite_plantoes_mes >= 0)
            AND (limite_convites_mes IS NULL OR limite_convites_mes >= 0)
        );
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_plano_recursos_plano' AND conrelid='plantaopro.plano_recursos'::regclass) THEN
        ALTER TABLE plantaopro.plano_recursos ADD CONSTRAINT fk_plano_recursos_plano FOREIGN KEY (plano_id) REFERENCES plantaopro.planos(id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='uq_plano_recursos_codigo' AND conrelid='plantaopro.plano_recursos'::regclass) THEN
        ALTER TABLE plantaopro.plano_recursos ADD CONSTRAINT uq_plano_recursos_codigo UNIQUE (plano_id, codigo);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_assinaturas_status_saas' AND conrelid='plantaopro.assinaturas'::regclass) THEN
        ALTER TABLE plantaopro.assinaturas ADD CONSTRAINT ck_assinaturas_status_saas CHECK (status IN ('TRIAL','ATIVA','SUSPENSA','CANCELADA','VENCIDA'));
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_assinaturas_periodicidade_saas' AND conrelid='plantaopro.assinaturas'::regclass) THEN
        ALTER TABLE plantaopro.assinaturas ADD CONSTRAINT ck_assinaturas_periodicidade_saas CHECK (periodicidade IN ('MENSAL','ANUAL'));
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_assinaturas_cliente_ativa_trial ON plantaopro.assinaturas(cliente_id) WHERE reg_status='A' AND status IN ('ATIVA','TRIAL');

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_assinatura_historico_assinatura' AND conrelid='plantaopro.assinatura_historico'::regclass) THEN
        ALTER TABLE plantaopro.assinatura_historico ADD CONSTRAINT fk_assinatura_historico_assinatura FOREIGN KEY (assinatura_id) REFERENCES plantaopro.assinaturas(id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_assinatura_uso_cliente' AND conrelid='plantaopro.assinatura_uso'::regclass) THEN
        ALTER TABLE plantaopro.assinatura_uso ADD CONSTRAINT fk_assinatura_uso_cliente FOREIGN KEY (cliente_id) REFERENCES plantaopro.clientes(id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_faturas_saas_cliente' AND conrelid='plantaopro.faturas_saas'::regclass) THEN
        ALTER TABLE plantaopro.faturas_saas ADD CONSTRAINT fk_faturas_saas_cliente FOREIGN KEY (cliente_id) REFERENCES plantaopro.clientes(id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_faturas_saas_assinatura' AND conrelid='plantaopro.faturas_saas'::regclass) THEN
        ALTER TABLE plantaopro.faturas_saas ADD CONSTRAINT fk_faturas_saas_assinatura FOREIGN KEY (assinatura_id) REFERENCES plantaopro.assinaturas(id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='uq_faturas_saas_assinatura_competencia' AND conrelid='plantaopro.faturas_saas'::regclass) THEN
        ALTER TABLE plantaopro.faturas_saas ADD CONSTRAINT uq_faturas_saas_assinatura_competencia UNIQUE (assinatura_id, competencia);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_faturas_saas_status' AND conrelid='plantaopro.faturas_saas'::regclass) THEN
        ALTER TABLE plantaopro.faturas_saas ADD CONSTRAINT ck_faturas_saas_status CHECK (status IN ('ABERTA','PAGA','VENCIDA','CANCELADA','EM_CONTESTACAO'));
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_faturas_saas_valores' AND conrelid='plantaopro.faturas_saas'::regclass) THEN
        ALTER TABLE plantaopro.faturas_saas ADD CONSTRAINT ck_faturas_saas_valores CHECK (valor >= 0 AND (valor_pago IS NULL OR valor_pago >= 0));
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_fatura_itens_fatura' AND conrelid='plantaopro.fatura_itens'::regclass) THEN
        ALTER TABLE plantaopro.fatura_itens ADD CONSTRAINT fk_fatura_itens_fatura FOREIGN KEY (fatura_id) REFERENCES plantaopro.faturas_saas(id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_pagamentos_saas_fatura' AND conrelid='plantaopro.pagamentos_saas'::regclass) THEN
        ALTER TABLE plantaopro.pagamentos_saas ADD CONSTRAINT fk_pagamentos_saas_fatura FOREIGN KEY (fatura_id) REFERENCES plantaopro.faturas_saas(id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_cliente_alertas_cliente' AND conrelid='plantaopro.cliente_alertas'::regclass) THEN
        ALTER TABLE plantaopro.cliente_alertas ADD CONSTRAINT fk_cliente_alertas_cliente FOREIGN KEY (cliente_id) REFERENCES plantaopro.clientes(id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_cliente_bloqueios_cliente' AND conrelid='plantaopro.cliente_bloqueios'::regclass) THEN
        ALTER TABLE plantaopro.cliente_bloqueios ADD CONSTRAINT fk_cliente_bloqueios_cliente FOREIGN KEY (cliente_id) REFERENCES plantaopro.clientes(id);
    END IF;
END $$;
