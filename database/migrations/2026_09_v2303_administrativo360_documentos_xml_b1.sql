-- ============================================================================
-- Migration: 2026_09_v2303_administrativo360_documentos_xml_b1.sql
-- Objetivo: B1 do Administrativo 360 - Central de documentos XML.
--   1. Coluna xml_bytes bytea NOT NULL com os bytes originais do arquivo
--      recebido (fonte da verdade para hash SHA-256 e download identico ao
--      enviado). Backfill: convert_to(xml_conteudo, 'UTF8') sobre o texto ja
--      gravado.
--   2. chave_acesso ampliada para varchar(60) para chaves sinteticas NFS-e
--      (NFSE + 36 hex) e identificadores longos em quarentena.
--   3. CHECK de tipo_documento estendido para os tipos fiscais suportados
--      por conteudo: NFE_COMPLETA (mod 55), NFC_E (mod 65), NFS_E (mod 67)
--      e RESUMO (legado). A DTD continua proibida no parse da aplicacao
--      (DtdProcessing.Ignore + XmlResolver=null): aqui o banco apenas
--      restringe o conjunto de tipos persistidos.
-- Idempotencia: DO $$ em 3 etapas (ADD nullable -> backfill -> SET NOT NULL);
--               DO $$ que remove o CHECK antigo localizando-o pela definicao
--               (o nome do constraint varia entre instalacoes) e recria com
--               nome fixo apenas quando falta; DO $$ com IF p/ varchar(60).
-- ============================================================================

-- 1. Bytes originais preservados (fonte da verdade para hash/download).
--    Padrão em 3 etapas (ADD nullable -> backfill -> SET NOT NULL) porque o
--    PostgreSQL desta instancia nao aceita referencia de coluna no DEFAULT.
DO $migration$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'plantaopro'
      AND table_name = 'adm360_documentos_recebidos'
      AND column_name = 'xml_bytes'
  ) THEN
    ALTER TABLE plantaopro.adm360_documentos_recebidos ADD COLUMN xml_bytes bytea;
  END IF;

  -- Backfill idempotente: bytes = UTF-8 do texto ja gravado (rows herdadas)
  UPDATE plantaopro.adm360_documentos_recebidos
     SET xml_bytes = convert_to(xml_conteudo, 'UTF8')
   WHERE xml_bytes IS NULL;

  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'plantaopro'
      AND table_name = 'adm360_documentos_recebidos'
      AND column_name = 'xml_bytes'
      AND is_nullable = 'YES'
  ) THEN
    ALTER TABLE plantaopro.adm360_documentos_recebidos ALTER COLUMN xml_bytes SET NOT NULL;
  END IF;
END $migration$;

COMMENT ON COLUMN plantaopro.adm360_documentos_recebidos.xml_bytes IS
  'B1: bytes originais do arquivo XML recebido; fonte da verdade para xml_hash (SHA-256) e download.';

-- 2. chave_acesso: varchar(44) -> varchar(60)
DO $migration$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'plantaopro'
      AND table_name = 'adm360_documentos_recebidos'
      AND column_name = 'chave_acesso'
      AND character_maximum_length IS NOT NULL
      AND character_maximum_length < 60
  ) THEN
    ALTER TABLE plantaopro.adm360_documentos_recebidos
      ALTER COLUMN chave_acesso TYPE varchar(60);
  END IF;
END $migration$;

-- 3. CHECK tipo_documento estendido (NFE_COMPLETA | NFC_E | NFS_E | RESUMO)
DO $migration$
DECLARE
  r record;
  tem_novo boolean;
BEGIN
  -- Remove qualquer CHECK antigo da coluna (definicao contendo NFE_COMPLETA),
  -- pois o nome do constraint varia entre instalacoes.
  FOR r IN
    SELECT con.conname
    FROM pg_constraint con
    JOIN pg_class rel ON rel.oid = con.conrelid
    JOIN pg_attribute at ON at.attrelid = con.conrelid AND at.attname = 'tipo_documento'
    WHERE rel.relnamespace = 'plantaopro'::regnamespace
      AND rel.relname = 'adm360_documentos_recebidos'
      AND con.contype = 'c'
      AND con.conkey @> ARRAY[at.attnum]
      AND pg_get_constraintdef(con.oid) LIKE '%NFE_COMPLETA%'
  LOOP
    EXECUTE format('ALTER TABLE plantaopro.adm360_documentos_recebidos DROP CONSTRAINT %I', r.conname);
  END LOOP;

  SELECT count(*) > 0 INTO tem_novo
  FROM pg_constraint con
  JOIN pg_class rel ON rel.oid = con.conrelid
  WHERE rel.relnamespace = 'plantaopro'::regnamespace
    AND rel.relname = 'adm360_documentos_recebidos'
    AND con.contype = 'c'
    AND pg_get_constraintdef(con.oid) LIKE '%''NFS_E''%';

  IF NOT tem_novo THEN
    ALTER TABLE plantaopro.adm360_documentos_recebidos
      ADD CONSTRAINT ck_adm360_docrec_tipo_documento
      CHECK (tipo_documento IN ('NFE_COMPLETA', 'NFC_E', 'NFS_E', 'RESUMO'));
  END IF;
END $migration$;
