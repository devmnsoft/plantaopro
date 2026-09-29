-- Migration 2026_09_v2200_administrativo360_exportacao_proposta_imutavel.sql
-- Administrativo 360 — Bloco 7 (Exportação e Integrações):
-- Arquivo real da proposta aprovada para o canal IMPORTACAO_MANUAL (EXPORTADA_MANUALMENTE).
-- Regras de negócio:
-- 1. O arquivo é gerado UMA única vez por resposta transmitida (UNIQUE tenant_id + resposta_id);
--    retransmissões preservam a primeira geração (ON CONFLICT DO NOTHING na aplicação).
-- 2. O registro é IMUTÁVEL após a geração: UPDATE/DELETE bloqueados por trigger.
--    (TRUNCATE não dispara row triggers — usado apenas em limpeza de testes/homologação.)
-- 3. Escopo multi-tenant: UNIQUE(tenant_id, id) e FOREIGN KEY em tenants.

CREATE TABLE IF NOT EXISTS plantaopro.adm360_cotacao_exportacoes (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    cotacao_id uuid NOT NULL REFERENCES plantaopro.adm360_cotacoes(id),
    resposta_id uuid NOT NULL REFERENCES plantaopro.adm360_cotacao_respostas(id),
    nome_arquivo varchar(255) NOT NULL,
    tamanho_bytes integer NOT NULL CHECK(tamanho_bytes > 0),
    content_type varchar(64) NOT NULL,
    sha256_hash char(64) NOT NULL,
    conteudo bytea NOT NULL,
    gerado_em timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, resposta_id)
);

CREATE INDEX IF NOT EXISTS ix_adm360_export_tenant_cotacao
    ON plantaopro.adm360_cotacao_exportacoes(tenant_id, cotacao_id);

-- Imutabilidade: a proposta exportada não pode ser alterada nem apagada por UPDATE/DELETE.
CREATE OR REPLACE FUNCTION plantaopro.fn_adm360_cotacao_exportacao_imutavel()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Arquivo de exportação da cotação é imutável após a geração (registro %).', OLD.id;
END;
$$;

DROP TRIGGER IF EXISTS trg_adm360_cotacao_exportacao_imutavel ON plantaopro.adm360_cotacao_exportacoes;
CREATE TRIGGER trg_adm360_cotacao_exportacao_imutavel
    BEFORE UPDATE OR DELETE ON plantaopro.adm360_cotacao_exportacoes
    FOR EACH ROW EXECUTE FUNCTION plantaopro.fn_adm360_cotacao_exportacao_imutavel();
