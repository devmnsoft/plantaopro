-- ============================================================================
-- PlantaoPro | WP-A4 (rodada 2) | Reconciliação do tracking em schema_migrations
-- Data: 2026-10-04 | Banco: plantaopro_test (dev, descartável) | Executado como: postgres
--
-- Diagnóstico (evidência em 2026-10-04):
--   As migrações v2303..v2308 possuem objetos aplicados neste banco desde as
--   rodadas de desenvolvimento (objetos existem e a suíte completa passa), mas
--   NUNCA foram registradas em plantaopro.schema_migrations. Consequências:
--     * o comando `upgrade` não as reconhece como aplicadas;
--     * o estado de v2305 ficou na revisão antiga: fn_adm360_evento_imutavel e
--       fn_adm360_vale_evento_progresso com corpo v2301 (GUC bruto) em vez da
--       função restrita fn_adm360_bypass_habilitado() — detectado pela
--       evidência por papel (cenário C falhou no primeiro run).
-- Correção:
--   1. Backfill das linhas v2303/v2304/v2306/v2307/v2308 com os checksums
--      canônicos (database/source-checksums.json), format idêntico ao usado
--      pelo PlantaoPro.Tools.Database (Program.cs, BuildMigrationInsert).
--   2. v2305 NÃO é backfilled aqui de propósito: o comando `upgrade` vai
--      reaplicar o arquivo canônico (idempotente: DO + CREATE OR REPLACE +
--      COMMENT ON) e registrá-lo sozinho, corrigindo as funções trigger
--      antigas com proveniência do próprio tool.
--   3. v2301: a linha registrada apontava o checksum 288e216b (rascunho de
--      desenvolvimento NUNCA commitado, aplicado em 2026-09-29); o arquivo
--      canônico atual é 77e3d861 (source-checksums.json). O estado DEPLOYED
--      foi verificado igual ao arquivo canônico: corpos de fn_adm360_evento_
--      imutavel / fn_adm360_vale_evento_progresso / fn_adm360_documento_
--      evento_preencher_hash idênticos, coluna sha256_hash NOT NULL, triggers
--      ativos; a única diferença textual entre revisões (limiar de 10 min na
--      recuperação única de linhas ENVIANDO) é bloco one-shot já consumido no
--      apply (0 linhas ENVIANDO antigas encontradas em 2026-10-04). => o
--      registro é realinhado ao checksum canônico; o manifest também foi
--      corrigido (migration-manifest.json L760).
-- Idempotência: linhas com success=false são removidas antes (mesma regra do
-- tool, Program.cs L255); INSERT usa ON CONFLICT (version) DO NOTHING.
-- ============================================================================

-- Pré-checagem auditável: estado atual do tracking para v23xx.
SELECT version, success, left(checksum, 16) AS checksum_16, source
  FROM plantaopro.schema_migrations
 WHERE version LIKE '2026_%v23%'
 ORDER BY version;

-- Mesma normalização que o tool aplica em cada upgrade (Program.cs L215).
CREATE UNIQUE INDEX IF NOT EXISTS ux_schema_migrations_version ON plantaopro.schema_migrations(version);

DELETE FROM plantaopro.schema_migrations
 WHERE version IN (
     '2026_09_v2303_administrativo360_documentos_xml_b1',
     '2026_09_v2304_administrativo360_transmissao_tentativas',
     '2026_10_v2306_saude360_plano_principal_unico',
     '2026_10_v2307_ai_camada_canonica',
     '2026_10_v2308_ai_governanca_modelos_orcamento'
   )
   AND success = false;

-- Idem do tool para tabela legada com PK id text (BuildMigrationInsert,
-- Programa.cs L437-439): id = gen_random_uuid()::text.
INSERT INTO plantaopro.schema_migrations(id, version, source, checksum, duration_ms, success) VALUES
  (gen_random_uuid()::text,
   '2026_09_v2303_administrativo360_documentos_xml_b1',
   'database/migrations/2026_09_v2303_administrativo360_documentos_xml_b1.sql',
   '50f78ce6e574d3fc7bf3863dc1fbad702ec73e29e999ae6d710ccf4943e36e6c', NULL, true),
  (gen_random_uuid()::text,
   '2026_09_v2304_administrativo360_transmissao_tentativas',
   'database/migrations/2026_09_v2304_administrativo360_transmissao_tentativas.sql',
   '37dcd938230badfecbd29f865fb56939966596a6d2f8d9f029bdef5f7c91729d', NULL, true),
  (gen_random_uuid()::text,
   '2026_10_v2306_saude360_plano_principal_unico',
   'database/migrations/2026_10_v2306_saude360_plano_principal_unico.sql',
   '8efd39faf5a9b2eb8e7f2322e6eddf8010d909903ff7f190ee5e608acf783b73', NULL, true),
  (gen_random_uuid()::text,
   '2026_10_v2307_ai_camada_canonica',
   'database/migrations/2026_10_v2307_ai_camada_canonica.sql',
   '47d5f9faaa95fa023673a5fe4484b8f874d44914ca9aed0bb12780f6e935eed0', NULL, true),
  (gen_random_uuid()::text,
   '2026_10_v2308_ai_governanca_modelos_orcamento',
   'database/migrations/2026_10_v2308_ai_governanca_modelos_orcamento.sql',
   '08c97f9e2d9957e0ead8055a4abcc5ce934fd76f5bf96d1c8d0570c818600554', NULL, true)
ON CONFLICT (version) DO NOTHING;

-- Realinha o registro de v2301 ao checksum canônico (ver comentário no cabeçalho).
UPDATE plantaopro.schema_migrations
   SET checksum = '77e3d861188b1984a8637940af7b73e8b00cea7ae86b189a9cd1c62e1d673f4d'
 WHERE version = '2026_09_v2301_administrativo360_eventos_imutabilidade'
   AND success = true;

-- Pós-checagem auditável: apenas v2305 deve permanecer ausente (será registrada
-- pelo comando `upgrade` imediatamente após este arquivo).
SELECT version, success, left(checksum, 16) AS checksum_16
  FROM plantaopro.schema_migrations
 WHERE version LIKE '2026_%v23%'
 ORDER BY version;

DO $$
DECLARE n int; n2301 int;
BEGIN
    SELECT count(*) INTO n FROM plantaopro.schema_migrations
     WHERE version IN (
         '2026_09_v2303_administrativo360_documentos_xml_b1',
         '2026_09_v2304_administrativo360_transmissao_tentativas',
         '2026_10_v2306_saude360_plano_principal_unico',
         '2026_10_v2307_ai_camada_canonica',
         '2026_10_v2308_ai_governanca_modelos_orcamento'
       ) AND success = true;
    IF n <> 5 THEN
        RAISE EXCEPTION 'RECONCILIACAO FALHOU: esperado 5 backfills com success=true, encontrado %', n;
    END IF;
    SELECT count(*) INTO n2301 FROM plantaopro.schema_migrations
     WHERE version = '2026_09_v2301_administrativo360_eventos_imutabilidade'
       AND success = true
       AND checksum = '77e3d861188b1984a8637940af7b73e8b00cea7ae86b189a9cd1c62e1d673f4d';
    IF n2301 <> 1 THEN
        RAISE EXCEPTION 'RECONCILIACAO FALHOU: v2301 nao realinhado ao checksum canonico (esperado 1, encontrado %)', n2301;
    END IF;
    RAISE NOTICE 'RECONCILIACAO OK: v2301 realinhado; v2303/v2304/v2306/v2307/v2308 registrados; v2305 pendente para o tool `upgrade`';
END $$;
