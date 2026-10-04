-- ============================================================================
-- Migration: 2026_10_v2310_adm360_eventos_tipo_conferencia.sql
-- Objetivo: WP-A3/G5 — habilitar o evento de conferencia autorizada de
--      documentos fiscais importados pelo Centro de XML (Administrativo 360).
--
--   O repositorio de documentos registra o evento 'CONFIRMACAO_CONFERENCIA'
--      em plantaopro.adm360_eventos quando um documento e confirmado pela
--      operacao (ConferirDocumentoAsync), habilitando o uso do documento no
--      gate de recebimento de estoque (ComprasRepository.ReceberAsync). O
--      CHECK atual da coluna tipo_evento apenas aceita os quatro tipos
--      operacionais legados; sem este ajuste a conferencia falha com a
--      violacao 23514 e o documento nunca chega ao estado CONFERIDO.
--
--   Nao altera a natureza append-only da tabela nem os demais tipos: o novo
--      valor so amplia o conjunto aceito.
--
-- Idempotencia: DO $migration$ — remove o CHECK legado da coluna (definicao
--      contendo 'APROVACAO', exceto a nova constraint nomeada) e so recria
--      se nenhuma versao ja contiver CONFIRMACAO_CONFERENCIA.
-- ============================================================================

DO $migration$
DECLARE
  r record;
  tem_novo boolean;
BEGIN
  -- Remove qualquer CHECK antigo da coluna (a definicao varia entre
  -- instalacoes por ter sido criada sem nome explicito).
  FOR r IN
    SELECT con.conname
    FROM pg_constraint con
    JOIN pg_class rel ON rel.oid = con.conrelid
    JOIN pg_attribute at ON at.attrelid = con.conrelid AND at.attname = 'tipo_evento'
    WHERE rel.relnamespace = 'plantaopro'::regnamespace
      AND rel.relname = 'adm360_eventos'
      AND con.contype = 'c'
      AND con.conkey @> ARRAY[at.attnum]
      AND pg_get_constraintdef(con.oid) LIKE '%''APROVACAO''%'
      AND pg_get_constraintdef(con.oid) NOT LIKE '%''CONFIRMACAO_CONFERENCIA''%'
      AND con.conname <> 'ck_adm360_eventos_tipo_evento'
  LOOP
    EXECUTE format('ALTER TABLE plantaopro.adm360_eventos DROP CONSTRAINT %I', r.conname);
  END LOOP;

  -- So recria se nenhuma versao ja contiver o novo valor (upgrade em 2 passadas).
  SELECT count(*) > 0 INTO tem_novo
  FROM pg_constraint con
  JOIN pg_class rel ON rel.oid = con.conrelid
  WHERE rel.relnamespace = 'plantaopro'::regnamespace
    AND rel.relname = 'adm360_eventos'
    AND con.contype = 'c'
    AND pg_get_constraintdef(con.oid) LIKE '%''CONFIRMACAO_CONFERENCIA''%';

  IF NOT tem_novo THEN
    ALTER TABLE plantaopro.adm360_eventos
      ADD CONSTRAINT ck_adm360_eventos_tipo_evento
      CHECK (tipo_evento IN ('APROVACAO', 'ARQUIVO', 'DECLARACAO_MANUAL', 'RETORNO_EXTERNO', 'CONFIRMACAO_CONFERENCIA'));
  END IF;
END $migration$;
