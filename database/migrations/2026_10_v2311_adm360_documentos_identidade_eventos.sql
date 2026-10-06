-- ============================================================================
-- Migration: 2026_10_v2311_adm360_documentos_identidade_eventos.sql
-- WS-A3: identidade documental por familia fiscal + unicidade de sequencia_evento.
--
-- Alteracoes oficiais:
--   1) Reconciliacao EXPLICITA de duplicatas fiscais FORA de quarentena:
--      - agrupa pela identidade (tenant + emitente + modelo + numero + serie);
--      - apaga apenas copias BYTE-IDENTICAS (mesmo xml_hash) sem vinculos
--        financeiros/operacionais e sem eventos alheios a importacao;
--      - qualquer restante divergente levanta RAISE com diagnostico completo
--        (nao decide silenciosamente qual e o documento verdadeiro).
--   2) Unicidade fiscal parcial: UNIQUE(tenant, emitente, modelo, numero, serie)
--      WHERE quarentena = false. Documentos em quarentena (ex.: XML_MALFORMADO)
--      trazem identificadores extraidos sem fiabilidade e nao bloqueiam novas
--      importacoes; quando reentram ao ciclo sao validados pelo indice/codigo.
--   3) Renumera sequencia_evento por documento em 1..N (ROW_NUMBER sobre
--      data_evento, id), normaliza sha256_hash quando divergente da formula
--      canonica (a sequencia participa do conteudo hash — mesma formula do
--      trigger de v2301) e adiciona UNIQUE(tenant_id, documento_id, sequencia_evento).
--
-- O bypass de imutabilidade (GUC v2301) e ativado na MESMA transacao.
-- A migration deve ser executada dentro de uma transacao unica (padrao do
-- runner canonico: transactional=true; falha em qualquer pos-condicao = rollback).
-- Idempotente: IF NOT EXISTS/guardas por nome; reconciliacao converge;
-- renumeracao e no-op para sequencias ja contiguas.
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Pre-condicoes: tabelas e colunas esperadas devem existir.
-- ----------------------------------------------------------------------------
DO $v2311$
DECLARE
    n integer;
BEGIN
    IF to_regclass('plantaopro.adm360_documentos_recebidos') IS NULL
       OR to_regclass('plantaopro.adm360_documento_eventos') IS NULL THEN
        RAISE EXCEPTION 'v2311 pre-condicao falhou: faltam as tabelas adm360_documentos_recebidos e/ou adm360_documento_eventos';
    END IF;

    SELECT count(*) INTO n FROM information_schema.columns
    WHERE table_schema = 'plantaopro' AND table_name = 'adm360_documentos_recebidos'
      AND column_name IN ('tenant_id','emitente_cnpj','modelo','numero','serie','quarentena',
                          'xml_hash','chave_acesso','recebimento_id','pedido_id','titulo_pagar_id','updated_at');
    IF n <> 12 THEN
        RAISE EXCEPTION 'v2311 pre-condicao falhou: colunas esperadas em adm360_documentos_recebidos nao encontradas (%/12)', n;
    END IF;

    SELECT count(*) INTO n FROM information_schema.columns
    WHERE table_schema = 'plantaopro' AND table_name = 'adm360_documento_eventos'
      AND column_name IN ('tenant_id','documento_id','tipo_evento','sequencia_evento','sha256_hash','data_evento');
    IF n <> 6 THEN
        RAISE EXCEPTION 'v2311 pre-condicao falhou: colunas esperadas em adm360_documento_eventos nao encontradas (%/6)', n;
    END IF;
END
$v2311$;

-- Bypass de imutabilidade (padrao v2301), escopo da transacao atual: libera o
-- DELETE/UPDATE dos documentos/eventos durante reconciliacao e renumeracao.
SELECT set_config('plantao.bypass_imutabilidade_adm360', 'on', true);

-- ----------------------------------------------------------------------------
-- 1. Reconciliacao: remove copias duplicadas byte-identicas (a mais antiga
--    permanece como ancora) somente quando sem vinculos e sem historia alheia
--    a importacao. Copias divergentes NAO sao tocadas (passo 2 as diagnostica).
-- ----------------------------------------------------------------------------
DO $v2311$
DECLARE
    g record;
    n integer;
BEGIN
    FOR g IN
        SELECT tenant_id, emitente_cnpj, modelo, numero, serie
        FROM plantaopro.adm360_documentos_recebidos
        WHERE quarentena = false
        GROUP BY tenant_id, emitente_cnpj, modelo, numero, serie
        HAVING count(*) > 1
        ORDER BY tenant_id::text, emitente_cnpj, numero, serie
    LOOP
        DELETE FROM plantaopro.adm360_documentos_recebidos d
        USING (
            SELECT id, xml_hash
            FROM plantaopro.adm360_documentos_recebidos
            WHERE tenant_id = g.tenant_id
              AND emitente_cnpj = g.emitente_cnpj
              AND modelo = g.modelo AND numero = g.numero AND serie = g.serie
              AND quarentena = false
            ORDER BY created_at, id
            LIMIT 1
        ) ancora
        WHERE d.tenant_id = g.tenant_id
          AND d.emitente_cnpj = g.emitente_cnpj
          AND d.modelo = g.modelo AND d.numero = g.numero AND d.serie = g.serie
          AND d.quarentena = false
          AND d.id <> ancora.id
          AND d.xml_hash = ancora.xml_hash
          AND d.recebimento_id IS NULL
          AND d.pedido_id IS NULL
          AND d.titulo_pagar_id IS NULL
          AND NOT EXISTS (
                SELECT 1 FROM plantaopro.adm360_documento_eventos e
                WHERE e.documento_id = d.id AND e.tipo_evento <> 'IMPORTACAO_MANUAL');
        GET DIAGNOSTICS n = ROW_COUNT;
        IF n > 0 THEN
            RAISE NOTICE 'WS-A3/v2311: % copia(s) byte-identica(s) reconciliada(s) (emitente %, modelo %, numero %, serie %)',
                n, g.emitente_cnpj, g.modelo, g.numero, g.serie;
        END IF;
    END LOOP;
END
$v2311$;

-- ----------------------------------------------------------------------------
-- 2. Diagnostico: duplicatas divergentes restantes FORA de quarentena bloqueiam
--    o upgrade com laudo completo (executado ANTES da criacao do indice unico).
-- ----------------------------------------------------------------------------
DO $v2311$
DECLARE
    g record;
    n_dup integer;
    n_hashes integer;
    chaves text;
    tmin timestamptz;
    tmax timestamptz;
BEGIN
    FOR g IN
        SELECT tenant_id, emitente_cnpj, modelo, numero, serie
        FROM plantaopro.adm360_documentos_recebidos
        WHERE quarentena = false
        GROUP BY tenant_id, emitente_cnpj, modelo, numero, serie
        HAVING count(*) > 1
        ORDER BY tenant_id::text, emitente_cnpj, numero, serie
    LOOP
        SELECT count(*), count(DISTINCT xml_hash),
               string_agg(DISTINCT left(chave_acesso, 16), ', '),
               min(created_at), max(created_at)
        INTO n_dup, n_hashes, chaves, tmin, tmax
        FROM plantaopro.adm360_documentos_recebidos
        WHERE tenant_id = g.tenant_id
          AND emitente_cnpj = g.emitente_cnpj
          AND modelo = g.modelo AND numero = g.numero AND serie = g.serie
          AND quarentena = false;

        RAISE EXCEPTION 'WS-A3/v2311 conflito de identidade fiscal pendente de reconciliacao manual (emitente %, modelo %, numero %, serie %, tenant %): % registro(s), % hash(es) distinto(s), chaves: %, periodo % .. %. Quarentene as duplicadas ou corrija a extracao antes de reexecutar a migration.',
            g.emitente_cnpj, g.modelo, g.numero, g.serie, g.tenant_id,
            n_dup, n_hashes, chaves,
            tmin AT TIME ZONE 'UTC', tmax AT TIME ZONE 'UTC';
    END LOOP;
END
$v2311$;

-- ----------------------------------------------------------------------------
-- 3. Unicidade parcial de identidade fiscal (documentos em quarentena fora).
-- ----------------------------------------------------------------------------
CREATE UNIQUE INDEX IF NOT EXISTS ux_adm360_docrec_tenant_fiscal
    ON plantaopro.adm360_documentos_recebidos (tenant_id, emitente_cnpj, modelo, numero, serie)
    WHERE quarentena = false;

-- ----------------------------------------------------------------------------
-- 4. Renumera sequencias de eventos 1..N por documento (ordem canonica:
--    data_evento, id) e normaliza sha256_hash com a MESMA formula do trigger
--    de v2301 (a sequencia participa do conteudo hash).
-- ----------------------------------------------------------------------------
DO $v2311$
DECLARE
    alterados integer := 0;
    normalizados integer := 0;
BEGIN
    WITH alvo AS (
        SELECT e.id,
               row_number() OVER (PARTITION BY e.tenant_id, e.documento_id
                                  ORDER BY e.data_evento, e.id) AS nova_seq
        FROM plantaopro.adm360_documento_eventos e
    )
    UPDATE plantaopro.adm360_documento_eventos e
    SET sequencia_evento = a.nova_seq
    FROM alvo a
    WHERE e.id = a.id
      AND e.sequencia_evento <> a.nova_seq;
    GET DIAGNOSTICS alterados = ROW_COUNT;

    UPDATE plantaopro.adm360_documento_eventos e
    SET sha256_hash = encode(digest(
            coalesce(e.id::text, '') || '|' || coalesce(e.tenant_id::text, '') || '|' ||
            coalesce(e.tipo_evento, '') || '|' || coalesce(e.sequencia_evento::text, '') || '|' ||
            coalesce(e.descricao_evento, '') || '|' || coalesce(e.data_evento::text, '') || '|' ||
            coalesce(e.protocolo, '') || '|' || coalesce(e.detalhes, ''),
            'sha256'), 'hex')
    WHERE e.sha256_hash IS DISTINCT FROM encode(digest(
            coalesce(e.id::text, '') || '|' || coalesce(e.tenant_id::text, '') || '|' ||
            coalesce(e.tipo_evento, '') || '|' || coalesce(e.sequencia_evento::text, '') || '|' ||
            coalesce(e.descricao_evento, '') || '|' || coalesce(e.data_evento::text, '') || '|' ||
            coalesce(e.protocolo, '') || '|' || coalesce(e.detalhes, ''),
            'sha256'), 'hex');
    GET DIAGNOSTICS normalizados = ROW_COUNT;

    IF alterados > 0 OR normalizados > 0 THEN
        RAISE NOTICE 'WS-A3/v2311: % evento(s) renumerado(s), % hash(es) normalizado(s)', alterados, normalizados;
    END IF;
END
$v2311$;

-- ----------------------------------------------------------------------------
-- 5. Constraint de unicidade concorrente da sequencia (guarda por nome para
--    re-execucao idempotente; dados ja estao 1..N apos o passo 4).
-- ----------------------------------------------------------------------------
DO $v2311$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace ns ON ns.oid = rel.relnamespace
        WHERE ns.nspname = 'plantaopro'
          AND rel.relname = 'adm360_documento_eventos'
          AND con.conname = 'ux_adm360_doc_eventos_tenant_doc_seq'
    ) THEN
        ALTER TABLE plantaopro.adm360_documento_eventos
            ADD CONSTRAINT ux_adm360_doc_eventos_tenant_doc_seq
            UNIQUE (tenant_id, documento_id, sequencia_evento);
    END IF;
END
$v2311$;

-- ----------------------------------------------------------------------------
-- Pos-condicoes verificaveis (falha aborta a transacao inteira).
-- ----------------------------------------------------------------------------
DO $v2311$
DECLARE
    n integer;
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes
        WHERE schemaname = 'plantaopro' AND indexname = 'ux_adm360_docrec_tenant_fiscal'
    ) THEN
        RAISE EXCEPTION 'v2311 pos-condicao falhou: indice unico ux_adm360_docrec_tenant_fiscal ausente';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace ns ON ns.oid = rel.relnamespace
        WHERE ns.nspname = 'plantaopro'
          AND rel.relname = 'adm360_documento_eventos'
          AND con.conname = 'ux_adm360_doc_eventos_tenant_doc_seq'
    ) THEN
        RAISE EXCEPTION 'v2311 pos-condicao falhou: constraint ux_adm360_doc_eventos_tenant_doc_seq ausente';
    END IF;

    SELECT count(*) INTO n FROM (
        SELECT 1 FROM plantaopro.adm360_documentos_recebidos
        WHERE quarentena = false
        GROUP BY tenant_id, emitente_cnpj, modelo, numero, serie
        HAVING count(*) > 1
    ) x;
    IF n > 0 THEN
        RAISE EXCEPTION 'v2311 pos-condicao falhou: % identidade(s) fiscal(is) duplicada(s) fora de quarentena', n;
    END IF;

    SELECT count(*) INTO n FROM (
        SELECT 1 FROM plantaopro.adm360_documento_eventos
        GROUP BY tenant_id, documento_id, sequencia_evento
        HAVING count(*) > 1
    ) x;
    IF n > 0 THEN
        RAISE EXCEPTION 'v2311 pos-condicao falhou: sequencias de eventos de documento duplicadas';
    END IF;

    -- Continuidade 1..N por documento (ROW_NUMBER garante min=1 e max=count).
    SELECT count(*) INTO n FROM (
        SELECT 1 FROM plantaopro.adm360_documento_eventos e
        GROUP BY e.tenant_id, e.documento_id
        HAVING count(*) <> max(e.sequencia_evento)
    ) x;
    IF n > 0 THEN
        RAISE EXCEPTION 'v2311 pos-condicao falhou: sequencias de eventos de documento nao contiguas 1..N';
    END IF;

    -- Coerencia dos hashes com a formula canonica (v2301).
    SELECT count(*) INTO n
    FROM plantaopro.adm360_documento_eventos e
    WHERE e.sha256_hash IS DISTINCT FROM encode(digest(
            coalesce(e.id::text, '') || '|' || coalesce(e.tenant_id::text, '') || '|' ||
            coalesce(e.tipo_evento, '') || '|' || coalesce(e.sequencia_evento::text, '') || '|' ||
            coalesce(e.descricao_evento, '') || '|' || coalesce(e.data_evento::text, '') || '|' ||
            coalesce(e.protocolo, '') || '|' || coalesce(e.detalhes, ''),
            'sha256'), 'hex');
    IF n > 0 THEN
        RAISE EXCEPTION 'v2311 pos-condicao falhou: % evento(s) com sha256_hash divergente da formula canonica', n;
    END IF;

    RAISE NOTICE 'WS-A3/v2311: pos-condicoes verificadas (identidade fiscal unica fora de quarentena, sequencias 1..N, hashes coerentes)';
END
$v2311$;
