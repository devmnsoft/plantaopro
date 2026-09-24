-- Migration 2026_09_v2194_administrativo360_valorizacao_vendas_financeiro.sql
-- Jornada: Vale Reconciliado -> Valorização -> Venda Interna -> Contas a Receber -> Recebimento Manual -> Estorno -> Comissão e Fluxo de Caixa

-- 1. Suporte a inspeção de retorno de consignação na Qualidade
DO $$
BEGIN
    ALTER TABLE plantaopro.adm360_inspecoes ALTER COLUMN recebimento_item_id DROP NOT NULL;
    ALTER TABLE plantaopro.adm360_inspecoes ADD COLUMN IF NOT EXISTS retorno_evento_id uuid REFERENCES plantaopro.adm360_vale_eventos(id);
    ALTER TABLE plantaopro.adm360_inspecoes ADD COLUMN IF NOT EXISTS origem_tipo varchar(30) NOT NULL DEFAULT 'RECEBIMENTO';
    ALTER TABLE plantaopro.adm360_vale_eventos ADD COLUMN IF NOT EXISTS quantidade_decidida numeric(18,4) NOT NULL DEFAULT 0;
EXCEPTION
    WHEN OTHERS THEN NULL;
END $$;

-- 2. Custo histórico em produtos e lotes
DO $$
BEGIN
    ALTER TABLE plantaopro.adm360_produtos ADD COLUMN IF NOT EXISTS preco_custo numeric(18,4) NOT NULL DEFAULT 0;
    ALTER TABLE plantaopro.adm360_lotes ADD COLUMN IF NOT EXISTS custo_unitario numeric(18,4);
EXCEPTION
    WHEN OTHERS THEN NULL;
END $$;

-- 3. Sequência de número da venda interna
CREATE SEQUENCE IF NOT EXISTS plantaopro.adm360_venda_numero START 1;

-- 4. Valorização do Vale de Consignação
CREATE TABLE IF NOT EXISTS plantaopro.adm360_valorizacoes (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    vale_id uuid NOT NULL REFERENCES plantaopro.adm360_vales(id),
    cirurgia_id uuid REFERENCES plantaopro.adm360_cirurgias(id),
    orcamento_id uuid REFERENCES plantaopro.adm360_orcamentos(id),
    orcamento_revisao integer,
    hospital_id uuid NOT NULL,
    pagador_id uuid NOT NULL,
    vendedor_id uuid,
    total_bruto numeric(18,4) NOT NULL DEFAULT 0 CHECK(total_bruto >= 0),
    desconto numeric(18,4) NOT NULL DEFAULT 0 CHECK(desconto >= 0),
    total_liquido numeric(18,4) NOT NULL DEFAULT 0 CHECK(total_liquido >= 0),
    total_custo numeric(18,4) NOT NULL DEFAULT 0 CHECK(total_custo >= 0),
    comissao_percentual numeric(18,4) NOT NULL DEFAULT 0 CHECK(comissao_percentual >= 0 AND comissao_percentual <= 100),
    comissao_prevista numeric(18,4) NOT NULL DEFAULT 0 CHECK(comissao_prevista >= 0),
    situacao varchar(20) NOT NULL DEFAULT 'CONFIRMADA' CHECK(situacao IN ('CONFIRMADA', 'CANCELADA')),
    versao bigint NOT NULL DEFAULT 1,
    created_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, vale_id)
);

CREATE INDEX IF NOT EXISTS ix_adm360_val_tenant_vale ON plantaopro.adm360_valorizacoes(tenant_id, vale_id);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_valorizacao_itens (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    valorizacao_id uuid NOT NULL REFERENCES plantaopro.adm360_valorizacoes(id) ON DELETE CASCADE,
    vale_item_id uuid NOT NULL REFERENCES plantaopro.adm360_vale_itens(id),
    produto_id uuid NOT NULL REFERENCES plantaopro.adm360_produtos(id),
    lote_id uuid NOT NULL REFERENCES plantaopro.adm360_lotes(id),
    quantidade_consumida numeric(18,4) NOT NULL CHECK(quantidade_consumida > 0),
    preco_unitario numeric(18,4) NOT NULL CHECK(preco_unitario >= 0),
    desconto numeric(18,4) NOT NULL DEFAULT 0 CHECK(desconto >= 0),
    subtotal numeric(18,4) NOT NULL CHECK(subtotal >= 0),
    custo_unitario numeric(18,4) NOT NULL DEFAULT 0 CHECK(custo_unitario >= 0),
    custo_total numeric(18,4) NOT NULL DEFAULT 0 CHECK(custo_total >= 0),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, valorizacao_id, vale_item_id)
);

-- 5. Vendas Internas
CREATE TABLE IF NOT EXISTS plantaopro.adm360_vendas (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    numero varchar(30) NOT NULL,
    origem_tipo varchar(30) NOT NULL DEFAULT 'VALE_CONSIGNACAO',
    origem_id uuid NOT NULL,
    valorizacao_id uuid REFERENCES plantaopro.adm360_valorizacoes(id),
    cliente_id uuid NOT NULL,
    pagador_id uuid NOT NULL,
    vendedor_id uuid,
    competencia date NOT NULL,
    total_bruto numeric(18,4) NOT NULL DEFAULT 0 CHECK(total_bruto >= 0),
    desconto numeric(18,4) NOT NULL DEFAULT 0 CHECK(desconto >= 0),
    total_liquido numeric(18,4) NOT NULL DEFAULT 0 CHECK(total_liquido >= 0),
    total_custo numeric(18,4) NOT NULL DEFAULT 0 CHECK(total_custo >= 0),
    comissao_percentual numeric(18,4) NOT NULL DEFAULT 0 CHECK(comissao_percentual >= 0 AND comissao_percentual <= 100),
    comissao_prevista numeric(18,4) NOT NULL DEFAULT 0 CHECK(comissao_prevista >= 0),
    condicao_pagamento varchar(50) NOT NULL DEFAULT 'A_VISTA',
    quantidade_parcelas integer NOT NULL DEFAULT 1 CHECK(quantidade_parcelas >= 1),
    situacao varchar(20) NOT NULL DEFAULT 'CONFIRMADA' CHECK(situacao IN ('RASCUNHO', 'CONFIRMADA', 'CANCELADA')),
    observacoes text,
    idempotency_key varchar(120),
    versao bigint NOT NULL DEFAULT 1,
    created_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, numero),
    UNIQUE(tenant_id, valorizacao_id),
    UNIQUE(tenant_id, idempotency_key)
);

CREATE INDEX IF NOT EXISTS ix_adm360_vendas_tenant_comp ON plantaopro.adm360_vendas(tenant_id, competencia, situacao);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_venda_itens (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    venda_id uuid NOT NULL REFERENCES plantaopro.adm360_vendas(id) ON DELETE CASCADE,
    produto_id uuid NOT NULL REFERENCES plantaopro.adm360_produtos(id),
    lote_id uuid NOT NULL REFERENCES plantaopro.adm360_lotes(id),
    quantidade numeric(18,4) NOT NULL CHECK(quantidade > 0),
    preco_unitario numeric(18,4) NOT NULL CHECK(preco_unitario >= 0),
    desconto numeric(18,4) NOT NULL DEFAULT 0 CHECK(desconto >= 0),
    subtotal numeric(18,4) NOT NULL CHECK(subtotal >= 0),
    custo_unitario numeric(18,4) NOT NULL DEFAULT 0 CHECK(custo_unitario >= 0),
    custo_total numeric(18,4) NOT NULL DEFAULT 0 CHECK(custo_total >= 0),
    UNIQUE(tenant_id, id)
);

-- 6. Contas Financeiras e Caixa
CREATE TABLE IF NOT EXISTS plantaopro.adm360_contas_financeiras (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    nome varchar(120) NOT NULL,
    tipo varchar(20) NOT NULL CHECK(tipo IN ('CAIXA', 'BANCO', 'APLICACAO')),
    banco varchar(40),
    agencia varchar(20),
    conta varchar(30),
    saldo_inicial numeric(18,4) NOT NULL DEFAULT 0,
    ativo boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id)
);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_movimentos_financeiros (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    conta_id uuid NOT NULL REFERENCES plantaopro.adm360_contas_financeiras(id),
    tipo varchar(10) NOT NULL CHECK(tipo IN ('ENTRADA', 'SAIDA')),
    valor numeric(18,4) NOT NULL CHECK(valor > 0),
    data_movimento date NOT NULL,
    descricao varchar(200) NOT NULL,
    origem_tipo varchar(30) NOT NULL,
    origem_id uuid NOT NULL,
    idempotency_key varchar(120),
    created_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, idempotency_key)
);

CREATE INDEX IF NOT EXISTS ix_adm360_movfin_tenant_conta ON plantaopro.adm360_movimentos_financeiros(tenant_id, conta_id, data_movimento);

-- 7. Contas a Receber (Títulos, Baixas, Estornos)
CREATE TABLE IF NOT EXISTS plantaopro.adm360_titulos_receber (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    venda_id uuid NOT NULL REFERENCES plantaopro.adm360_vendas(id),
    numero varchar(40) NOT NULL,
    pagador_id uuid NOT NULL,
    parcela integer NOT NULL DEFAULT 1,
    total_parcelas integer NOT NULL DEFAULT 1,
    data_emissao date NOT NULL,
    data_vencimento date NOT NULL,
    valor_principal numeric(18,4) NOT NULL CHECK(valor_principal > 0),
    valor_desconto numeric(18,4) NOT NULL DEFAULT 0 CHECK(valor_desconto >= 0),
    valor_juros numeric(18,4) NOT NULL DEFAULT 0 CHECK(valor_juros >= 0),
    valor_recebido numeric(18,4) NOT NULL DEFAULT 0 CHECK(valor_recebido >= 0),
    saldo_aberto numeric(18,4) NOT NULL CHECK(saldo_aberto >= 0),
    situacao varchar(20) NOT NULL DEFAULT 'ABERTO' CHECK(situacao IN ('ABERTO', 'PARCIAL', 'QUITADO', 'CANCELADO')),
    versao bigint NOT NULL DEFAULT 1,
    created_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, venda_id, parcela)
);

CREATE INDEX IF NOT EXISTS ix_adm360_titulos_tenant_venc ON plantaopro.adm360_titulos_receber(tenant_id, data_vencimento, situacao);
CREATE INDEX IF NOT EXISTS ix_adm360_titulos_pagador ON plantaopro.adm360_titulos_receber(tenant_id, pagador_id);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_titulo_baixas (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    titulo_id uuid NOT NULL REFERENCES plantaopro.adm360_titulos_receber(id),
    conta_id uuid NOT NULL REFERENCES plantaopro.adm360_contas_financeiras(id),
    data_recebimento date NOT NULL,
    valor_recebido numeric(18,4) NOT NULL CHECK(valor_recebido > 0),
    meio_pagamento varchar(30) NOT NULL,
    referencia text,
    movimento_financeiro_id uuid REFERENCES plantaopro.adm360_movimentos_financeiros(id),
    estornado boolean NOT NULL DEFAULT false,
    idempotency_key varchar(120),
    recebido_por uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, idempotency_key)
);

CREATE INDEX IF NOT EXISTS ix_adm360_baixas_tenant_tit ON plantaopro.adm360_titulo_baixas(tenant_id, titulo_id);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_titulo_estornos (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    baixa_id uuid NOT NULL REFERENCES plantaopro.adm360_titulo_baixas(id),
    titulo_id uuid NOT NULL REFERENCES plantaopro.adm360_titulos_receber(id),
    valor_estornado numeric(18,4) NOT NULL CHECK(valor_estornado > 0),
    motivo text NOT NULL,
    movimento_financeiro_id uuid REFERENCES plantaopro.adm360_movimentos_financeiros(id),
    idempotency_key varchar(120),
    estornado_por uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, baixa_id),
    UNIQUE(tenant_id, idempotency_key)
);

-- 8. Comissões Apropriadas
CREATE TABLE IF NOT EXISTS plantaopro.adm360_comissoes_apropriadas (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    venda_id uuid NOT NULL REFERENCES plantaopro.adm360_vendas(id),
    baixa_id uuid REFERENCES plantaopro.adm360_titulo_baixas(id),
    vendedor_id uuid NOT NULL,
    base_calculo numeric(18,4) NOT NULL,
    percentual numeric(18,4) NOT NULL,
    valor_comissao numeric(18,4) NOT NULL,
    situacao varchar(20) NOT NULL DEFAULT 'APROPRIADA' CHECK(situacao IN ('PREVISTA', 'APROPRIADA', 'PAGA', 'ESTORNADA')),
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, baixa_id)
);

CREATE INDEX IF NOT EXISTS ix_adm360_comissoes_vendedor ON plantaopro.adm360_comissoes_apropriadas(tenant_id, vendedor_id, situacao);
