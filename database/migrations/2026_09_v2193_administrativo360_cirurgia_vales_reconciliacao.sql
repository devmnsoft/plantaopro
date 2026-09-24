-- Administrativo 360 bloco 4: Cirurgias operacionais, Vales de consignação, Expedição, Consumo/Retorno e Reconciliação.

CREATE SEQUENCE IF NOT EXISTS plantaopro.adm360_cirurgia_numero;
CREATE SEQUENCE IF NOT EXISTS plantaopro.adm360_vale_numero;

-- Vinculação de item de orçamento e situação em reservas
ALTER TABLE plantaopro.adm360_reservas ADD COLUMN IF NOT EXISTS orcamento_item_id uuid;

DO $$
BEGIN
    ALTER TABLE plantaopro.adm360_reservas DROP CONSTRAINT IF EXISTS adm360_reservas_situacao_check;
    ALTER TABLE plantaopro.adm360_reservas DROP CONSTRAINT IF EXISTS chk_adm360_reservas_situacao;
    ALTER TABLE plantaopro.adm360_reservas ADD CONSTRAINT chk_adm360_reservas_situacao CHECK(situacao IN ('ATIVA', 'CONSUMIDA', 'ATENDIDA', 'CANCELADA'));
EXCEPTION
    WHEN OTHERS THEN NULL;
END $$;

-- Cirurgias Operacionais
CREATE TABLE IF NOT EXISTS plantaopro.adm360_cirurgias (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    numero varchar(30) NOT NULL,
    hospital_id uuid NOT NULL,
    medico_id uuid,
    procedimento varchar(180) NOT NULL,
    data_prevista date NOT NULL,
    hora_prevista time,
    orcamento_id uuid REFERENCES plantaopro.adm360_orcamentos(id),
    orcamento_revisao integer DEFAULT 1,
    responsavel_id uuid,
    local_destino_id uuid NOT NULL REFERENCES plantaopro.adm360_locais(id),
    situacao varchar(20) NOT NULL DEFAULT 'AGENDADA' CHECK(situacao IN ('AGENDADA', 'EM_ANDAMENTO', 'REALIZADA', 'CANCELADA')),
    observacoes text,
    versao bigint NOT NULL DEFAULT 1,
    created_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, numero)
);

CREATE INDEX IF NOT EXISTS ix_adm360_cirurgias_tenant_data ON plantaopro.adm360_cirurgias(tenant_id, data_prevista, situacao);
CREATE INDEX IF NOT EXISTS ix_adm360_cirurgias_orcamento ON plantaopro.adm360_cirurgias(tenant_id, orcamento_id);

-- Vales de Consignação
CREATE TABLE IF NOT EXISTS plantaopro.adm360_vales (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    numero varchar(30) NOT NULL,
    cirurgia_id uuid REFERENCES plantaopro.adm360_cirurgias(id),
    orcamento_id uuid REFERENCES plantaopro.adm360_orcamentos(id),
    orcamento_revisao integer DEFAULT 1,
    hospital_id uuid NOT NULL,
    custodiante_id uuid,
    local_origem_id uuid NOT NULL REFERENCES plantaopro.adm360_locais(id),
    local_destino_id uuid NOT NULL REFERENCES plantaopro.adm360_locais(id),
    data_saida_prevista date NOT NULL,
    data_saida_efetiva timestamptz,
    data_retorno_prevista date,
    data_reconciliacao timestamptz,
    situacao varchar(30) NOT NULL DEFAULT 'RASCUNHO' CHECK(situacao IN ('RASCUNHO', 'EM_SEPARACAO', 'PRONTO_PARA_EXPEDICAO', 'EXPEDIDO', 'RETORNO_PARCIAL', 'RECONCILIADO', 'CANCELADO')),
    situacao_financeira varchar(30) NOT NULL DEFAULT 'PENDENTE_VALORIZACAO' CHECK(situacao_financeira IN ('PENDENTE_VALORIZACAO', 'VALORIZADO', 'FATURADO')),
    separado_por uuid,
    separado_em timestamptz,
    expedido_por uuid,
    expedido_em timestamptz,
    reconciliado_por uuid,
    observacoes text,
    idempotency_key varchar(120),
    versao bigint NOT NULL DEFAULT 1,
    created_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, numero),
    UNIQUE(tenant_id, idempotency_key)
);

CREATE INDEX IF NOT EXISTS ix_adm360_vales_tenant_situacao ON plantaopro.adm360_vales(tenant_id, situacao, data_saida_prevista);
CREATE INDEX IF NOT EXISTS ix_adm360_vales_tenant_hospital ON plantaopro.adm360_vales(tenant_id, hospital_id);
CREATE INDEX IF NOT EXISTS ix_adm360_vales_cirurgia ON plantaopro.adm360_vales(tenant_id, cirurgia_id);

-- Itens do Vale de Consignação
CREATE TABLE IF NOT EXISTS plantaopro.adm360_vale_itens (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    vale_id uuid NOT NULL REFERENCES plantaopro.adm360_vales(id) ON DELETE CASCADE,
    produto_id uuid NOT NULL REFERENCES plantaopro.adm360_produtos(id),
    lote_id uuid NOT NULL REFERENCES plantaopro.adm360_lotes(id),
    reserva_id uuid REFERENCES plantaopro.adm360_reservas(id),
    quantidade_solicitada numeric(18,4) NOT NULL CHECK(quantidade_solicitada > 0),
    quantidade_separada numeric(18,4) NOT NULL DEFAULT 0 CHECK(quantidade_separada >= 0),
    quantidade_expedida numeric(18,4) NOT NULL DEFAULT 0 CHECK(quantidade_expedida >= 0),
    quantidade_consumida numeric(18,4) NOT NULL DEFAULT 0 CHECK(quantidade_consumida >= 0),
    quantidade_devolvida numeric(18,4) NOT NULL DEFAULT 0 CHECK(quantidade_devolvida >= 0),
    quantidade_perda numeric(18,4) NOT NULL DEFAULT 0 CHECK(quantidade_perda >= 0),
    preco_unitario numeric(18,4) NOT NULL DEFAULT 0 CHECK(preco_unitario >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, vale_id, produto_id, lote_id)
);

CREATE INDEX IF NOT EXISTS ix_adm360_vale_itens_tenant_vale ON plantaopro.adm360_vale_itens(tenant_id, vale_id);
CREATE INDEX IF NOT EXISTS ix_adm360_vale_itens_reserva ON plantaopro.adm360_vale_itens(tenant_id, reserva_id);

-- Eventos do Vale: Consumo, Retorno, Perda, Avaria
CREATE TABLE IF NOT EXISTS plantaopro.adm360_vale_eventos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    vale_id uuid NOT NULL REFERENCES plantaopro.adm360_vales(id),
    vale_item_id uuid NOT NULL REFERENCES plantaopro.adm360_vale_itens(id),
    tipo varchar(30) NOT NULL CHECK(tipo IN ('CONSUMO', 'RETORNO', 'PERDA', 'AVARIA')),
    quantidade numeric(18,4) NOT NULL CHECK(quantidade > 0),
    data_evento timestamptz NOT NULL DEFAULT now(),
    motivo text,
    movimento_id uuid REFERENCES plantaopro.adm360_movimentos(id),
    idempotency_key varchar(120),
    registrado_por uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, idempotency_key)
);

CREATE INDEX IF NOT EXISTS ix_adm360_vale_eventos_tenant_vale ON plantaopro.adm360_vale_eventos(tenant_id, vale_id);
CREATE INDEX IF NOT EXISTS ix_adm360_vale_eventos_item ON plantaopro.adm360_vale_eventos(tenant_id, vale_item_id);
