-- ============================================================================
-- Migration: 2026_10_v2309_administrativo360_documentos_xml_a3.sql
-- Objetivo: WP-A3 — Centro de XML de documentos fiscais (Administrativo 360)
--
--   1. nome_arquivo varchar(255): persistir o nome do arquivo original no
--      import. O arquivo passa a ser conceito explícito, separado do
--      documento fiscal (chave de acesso) e do id interno (id). O conteúdo
--      segue guardado em xml_bytes/xml_hash (canônico p/ dedup e download);
--      esta coluna apenas identifica o arquivo recebido (linhas legadas
--      permanecem NULL).
--   2. CHECK tipo_documento estendido com 'ABRASF' (prescrição eletrônica):
--      a família ABRASF é reconhecida como família não-fiscal aceita por
--      este módulo (o destinatário NÃO é validado para essa família).
--
-- Idempotência: DO $$ com checagem em information_schema/pg_constraint.
-- ============================================================================

-- 1. Nome do arquivo original.
DO $migration$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'plantaopro'
      AND table_name = 'adm360_documentos_recebidos'
      AND column_name = 'nome_arquivo'
  ) THEN
    ALTER TABLE plantaopro.adm360_documentos_recebidos
      ADD COLUMN nome_arquivo varchar(255);
  END IF;
END $migration$;

COMMENT ON COLUMN plantaopro.adm360_documentos_recebidos.nome_arquivo IS
  'WP-A3: nome do arquivo XML original no import (opcional; linhas legadas NULL). O arquivo e o documento fiscal (chave_acesso) são conceitos distintos: o hash/dedup/download usam os bytes exatos, esta coluna só identifica o arquivo recebido.';

-- 2. CHECK tipo_documento estendido com ABRASF.
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

  -- Só recria se nenhuma versao ja contiver ABRASF (upgrade em 2 passadas).
  SELECT count(*) > 0 INTO tem_novo
  FROM pg_constraint con
  JOIN pg_class rel ON rel.oid = con.conrelid
  WHERE rel.relnamespace = 'plantaopro'::regnamespace
    AND rel.relname = 'adm360_documentos_recebidos'
    AND con.contype = 'c'
    AND pg_get_constraintdef(con.oid) LIKE '%''ABRASF''%';

  IF NOT tem_novo THEN
    ALTER TABLE plantaopro.adm360_documentos_recebidos
      ADD CONSTRAINT ck_adm360_docrec_tipo_documento
      CHECK (tipo_documento IN ('NFE_COMPLETA', 'NFC_E', 'NFS_E', 'ABRASF', 'RESUMO'));
  END IF;
END $migration$;
