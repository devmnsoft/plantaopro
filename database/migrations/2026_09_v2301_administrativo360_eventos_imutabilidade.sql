-- ============================================================================
-- PlantaoPro | Migration: 2026_09_v2301_administrativo360_eventos_imutabilidade
-- Administrativo 360 - Eventos unificados imutaveis com hash (requisito P4)
--
-- Alteracoes oficiais:
--   1. Cria plantaopro.adm360_eventos: log append-only de eventos de negocio que
--      classifica APROVACAO, ARQUIVO, DECLARACAO_MANUAL e RETORNO_EXTERNO, com hash
--      SHA-256 do conteudo canonico e chave de idempotencia opcional
--      UNIQUE(tenant_id, idempotency_key).
--   2. Protege adm360_eventos e adm360_documento_eventos (apos backfill de
--      sha256_hash) contra UPDATE/DELETE: append-only com bypass GUC controlado.
--   3. Protege adm360_vale_eventos: DELETE bloqueado; apenas o progresso da
--      decisao (quantidade_decidida) pode ser atualizado.
--   4. Conciliacao unica: respostas presas em ENVIANDO por queda do processo viram
--      RESULTADO_DESCONHECIDO (estado honesto, sem protocolo externo confirmado).
--   5. Trigger de preenchimento: todo novo evento de documento fiscal recebe
--      sha256_hash calculado sobre as colunas armazenadas (mesma formula do backfill).
--
-- Idempotencia: CREATE ... IF NOT EXISTS / CREATE OR REPLACE / ADD COLUMN IF NOT EXISTS;
-- o backfill atualiza apenas linhas sem hash; a conciliacao zera linhas no re-executar.
-- O bypass SELECT set_config('plantao.bypass_imutabilidade_adm360', 'on', true) deve ser
-- feito na MESMA transacao da escrita protegida e esta reservado a ferramentas oficiais
-- de manutencao. TRUNCATE nao dispara row triggers (mesmo padrao de v2200).
-- ============================================================================

-- 1. Log unico de eventos do Administrativo 360 (append-only)
CREATE TABLE IF NOT EXISTS plantaopro.adm360_eventos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL REFERENCES plantaopro.tenants(id),
    tipo_evento varchar(30) NOT NULL CHECK(tipo_evento IN ('APROVACAO', 'ARQUIVO', 'DECLARACAO_MANUAL', 'RETORNO_EXTERNO')),
    entidade varchar(40) NOT NULL,
    entidade_id uuid NOT NULL,
    usuario_id uuid,
    descricao text NOT NULL,
    dados jsonb NOT NULL DEFAULT '{}'::jsonb,
    sha256_hash char(64) NOT NULL CHECK(sha256_hash ~ '^[0-9a-f]{64}$'),
    idempotency_key varchar(160),
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(tenant_id, id),
    UNIQUE(tenant_id, idempotency_key)
);

CREATE INDEX IF NOT EXISTS ix_adm360_eventos_tenant_tipo
    ON plantaopro.adm360_eventos(tenant_id, tipo_evento, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_adm360_eventos_entidade
    ON plantaopro.adm360_eventos(tenant_id, entidade, entidade_id);

COMMENT ON TABLE plantaopro.adm360_eventos IS 'Administrativo 360: log unificado de eventos imutaveis (APROVACAO/ARQUIVO/DECLARACAO_MANUAL/RETORNO_EXTERNO) com hash SHA-256 do conteudo.';

-- 2. Imutabilidade append-only com bypass GUC controlado
CREATE OR REPLACE FUNCTION plantaopro.fn_adm360_evento_imutavel()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF current_setting('plantao.bypass_imutabilidade_adm360', true) = 'on' THEN
        RETURN COALESCE(NEW, OLD);
    END IF;
    RAISE EXCEPTION 'Evento do modulo Administrativo 360 e imutavel (append-only): UPDATE e DELETE nao permitidos (registro %).', COALESCE(NEW.id, OLD.id);
END;
$$;

DROP TRIGGER IF EXISTS trg_adm360_eventos_imutavel ON plantaopro.adm360_eventos;
CREATE TRIGGER trg_adm360_eventos_imutavel
    BEFORE UPDATE OR DELETE ON plantaopro.adm360_eventos
    FOR EACH ROW EXECUTE FUNCTION plantaopro.fn_adm360_evento_imutavel();

-- 3. Eventos de documento fiscal (NF-e): hash SHA-256 + imutabilidade append-only
ALTER TABLE plantaopro.adm360_documento_eventos ADD COLUMN IF NOT EXISTS sha256_hash char(64);

UPDATE plantaopro.adm360_documento_eventos e
SET sha256_hash = encode(digest(
        coalesce(e.id::text, '') || '|' || coalesce(e.tenant_id::text, '') || '|' ||
        coalesce(e.tipo_evento, '') || '|' || coalesce(e.sequencia_evento::text, '') || '|' ||
        coalesce(e.descricao_evento, '') || '|' || coalesce(e.data_evento::text, '') || '|' ||
        coalesce(e.protocolo, '') || '|' || coalesce(e.detalhes, ''),
        'sha256'), 'hex')
WHERE e.sha256_hash IS NULL;

ALTER TABLE plantaopro.adm360_documento_eventos ALTER COLUMN sha256_hash SET NOT NULL;

-- Fonte unica da formula: todo novo evento (qualquer tipo) recebe o hash calculado
-- sobre as colunas que serão armazenadas (consistente com o backfill acima).
CREATE OR REPLACE FUNCTION plantaopro.fn_adm360_documento_evento_preencher_hash()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.sha256_hash IS NULL THEN
        NEW.sha256_hash := encode(digest(
            coalesce(NEW.id::text, '') || '|' || coalesce(NEW.tenant_id::text, '') || '|' ||
            coalesce(NEW.tipo_evento, '') || '|' || coalesce(NEW.sequencia_evento::text, '') || '|' ||
            coalesce(NEW.descricao_evento, '') || '|' || coalesce(NEW.data_evento::text, '') || '|' ||
            coalesce(NEW.protocolo, '') || '|' || coalesce(NEW.detalhes, ''),
            'sha256'), 'hex')::character(64);
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_adm360_documento_eventos_hash ON plantaopro.adm360_documento_eventos;
CREATE TRIGGER trg_adm360_documento_eventos_hash
    BEFORE INSERT ON plantaopro.adm360_documento_eventos
    FOR EACH ROW EXECUTE FUNCTION plantaopro.fn_adm360_documento_evento_preencher_hash();

DROP TRIGGER IF EXISTS trg_adm360_documento_eventos_imutavel ON plantaopro.adm360_documento_eventos;
CREATE TRIGGER trg_adm360_documento_eventos_imutavel
    BEFORE UPDATE OR DELETE ON plantaopro.adm360_documento_eventos
    FOR EACH ROW EXECUTE FUNCTION plantaopro.fn_adm360_evento_imutavel();

-- 4. Eventos de vale: identidade imutavel; so o progresso da decisao pode mudar
CREATE OR REPLACE FUNCTION plantaopro.fn_adm360_vale_evento_progresso()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF current_setting('plantao.bypass_imutabilidade_adm360', true) = 'on' THEN
        RETURN COALESCE(NEW, OLD);
    END IF;
    IF TG_OP = 'DELETE' THEN
        RAISE EXCEPTION 'Evento de vale e imutavel: DELETE nao permitido (registro %).', OLD.id;
    END IF;
    IF NEW.tenant_id IS DISTINCT FROM OLD.tenant_id
       OR NEW.vale_id IS DISTINCT FROM OLD.vale_id
       OR NEW.vale_item_id IS DISTINCT FROM OLD.vale_item_id
       OR NEW.tipo IS DISTINCT FROM OLD.tipo
       OR NEW.quantidade IS DISTINCT FROM OLD.quantidade
       OR NEW.data_evento IS DISTINCT FROM OLD.data_evento
       OR NEW.motivo IS DISTINCT FROM OLD.motivo
       OR NEW.movimento_id IS DISTINCT FROM OLD.movimento_id
       OR NEW.idempotency_key IS DISTINCT FROM OLD.idempotency_key
       OR NEW.registrado_por IS DISTINCT FROM OLD.registrado_por
       OR NEW.created_at IS DISTINCT FROM OLD.created_at
    THEN
        RAISE EXCEPTION 'Evento de vale e imutavel: apenas o progresso da decisao (quantidade_decidida) pode ser atualizado (registro %).', NEW.id;
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_adm360_vale_eventos_progresso ON plantaopro.adm360_vale_eventos;
CREATE TRIGGER trg_adm360_vale_eventos_progresso
    BEFORE UPDATE OR DELETE ON plantaopro.adm360_vale_eventos
    FOR EACH ROW EXECUTE FUNCTION plantaopro.fn_adm360_vale_evento_progresso();

-- 5. Recuperacao unica: ENVIANDO ancorado por queda do processo (0 linhas no re-executar)
UPDATE plantaopro.adm360_cotacao_respostas
SET status_transmissao = 'RESULTADO_DESCONHECIDO',
    mensagem_retorno = coalesce(mensagem_retorno, '') || ' | Recuperacao oficial: transmissao ficou em ENVIANDO sem resultado registrado (processo interrompido).',
    updated_at = now()
WHERE status_transmissao = 'ENVIANDO';
