-- Administrativo 360 bloco 3: Idempotência de operações, inventário por condição e orçamentos cirúrgicos com reservas.

CREATE SEQUENCE IF NOT EXISTS plantaopro.adm360_orcamento_numero;

CREATE TABLE IF NOT EXISTS plantaopro.adm360_operacoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    tipo varchar(50) NOT NULL,
    idempotency_key varchar(120) NOT NULL,
    payload_hash varchar(64) NOT NULL,
    resultado text,
    created_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, idempotency_key)
);

CREATE INDEX IF NOT EXISTS ix_adm360_operacoes_tenant_tipo ON plantaopro.adm360_operacoes(tenant_id, tipo, created_at);

-- Ajuste de granularidade de inventário por condição
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema='plantaopro' AND table_name='adm360_inventario_itens' AND column_name='condicao'
    ) THEN
        ALTER TABLE plantaopro.adm360_inventario_itens ADD COLUMN condicao varchar(15) NOT NULL DEFAULT 'LIBERADO' CHECK(condicao IN ('QUARENTENA','LIBERADO','BLOQUEADO','REPROVADO','VENCIDO'));
    END IF;
END $$;

ALTER TABLE plantaopro.adm360_inventario_itens DROP CONSTRAINT IF EXISTS adm360_inventario_itens_tenant_id_inventario_id_produto_id_l_key;
CREATE UNIQUE INDEX IF NOT EXISTS ux_adm360_inv_itens_condicao ON plantaopro.adm360_inventario_itens(tenant_id, inventario_id, produto_id, lote_id, condicao);

-- Orçamentos Cirúrgicos
CREATE TABLE IF NOT EXISTS plantaopro.adm360_orcamentos (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    numero varchar(30) NOT NULL,
    revisao integer NOT NULL DEFAULT 1,
    hospital_id uuid NOT NULL,
    medico_id uuid,
    procedimento varchar(180) NOT NULL,
    responsavel_financeiro_id uuid NOT NULL,
    vendedor_id uuid,
    data_prevista date NOT NULL,
    validade date NOT NULL,
    situacao varchar(20) NOT NULL DEFAULT 'RASCUNHO' CHECK(situacao IN ('RASCUNHO','ENVIADO','APROVADO','REJEITADO','EXPIRADO','CANCELADO')),
    total_produtos numeric(18,4) NOT NULL DEFAULT 0 CHECK(total_produtos >= 0),
    desconto_geral numeric(18,4) NOT NULL DEFAULT 0 CHECK(desconto_geral >= 0),
    total_geral numeric(18,4) NOT NULL DEFAULT 0 CHECK(total_geral >= 0),
    observacoes text,
    aprovado_em timestamptz,
    aprovado_por uuid,
    idempotency_key varchar(120),
    versao bigint NOT NULL DEFAULT 1,
    created_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, numero, revisao),
    UNIQUE(tenant_id, idempotency_key),
    FOREIGN KEY(tenant_id, hospital_id) REFERENCES plantaopro.adm360_parceiros(tenant_id, id),
    FOREIGN KEY(tenant_id, responsavel_financeiro_id) REFERENCES plantaopro.adm360_parceiros(tenant_id, id)
);

CREATE INDEX IF NOT EXISTS ix_adm360_orcamentos_tenant_sit ON plantaopro.adm360_orcamentos(tenant_id, situacao, data_prevista);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_orcamento_itens (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    orcamento_id uuid NOT NULL,
    produto_id uuid NOT NULL,
    quantidade numeric(18,4) NOT NULL CHECK(quantidade > 0),
    preco_unitario numeric(18,4) NOT NULL CHECK(preco_unitario >= 0),
    desconto numeric(18,4) NOT NULL DEFAULT 0 CHECK(desconto >= 0),
    total numeric(18,4) NOT NULL CHECK(total >= 0),
    UNIQUE(tenant_id, id),
    FOREIGN KEY(tenant_id, orcamento_id) REFERENCES plantaopro.adm360_orcamentos(tenant_id, id),
    FOREIGN KEY(tenant_id, produto_id) REFERENCES plantaopro.adm360_produtos(tenant_id, id)
);

CREATE TABLE IF NOT EXISTS plantaopro.adm360_orcamento_revisoes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL,
    orcamento_id uuid NOT NULL,
    revisao integer NOT NULL,
    motivo text NOT NULL,
    snapshot_json text NOT NULL,
    criado_por uuid,
    criado_em timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY(tenant_id, orcamento_id) REFERENCES plantaopro.adm360_orcamentos(tenant_id, id)
);

CREATE INDEX IF NOT EXISTS ix_adm360_orcamento_revisoes_orc ON plantaopro.adm360_orcamento_revisoes(tenant_id, orcamento_id, revisao);
