-- PlantaoPro v1.95.1+: normaliza a propriedade dos objetos do schema plantaopro
-- para a role dona do banco e reemite os privilegios da role da aplicacao.
-- Seeds de demonstracao (ou execucoes manuais fora do instalador) podem criar
-- objetos com a role que os aplicou; sem esta normalizacao, a proxima aplicacao
-- falharia no passo de GRANT com "permissao negada para tabela <nome>"
-- (identificado na rodada 68 na segunda aplicacao do perfil demo, tabela setores).
-- Idempotente: nada faz quando todos os objetos ja pertencem a database_owner.
-- Uso pelo instalador:
--   psql -v ON_ERROR_STOP=1 -v database_owner=<role> -v application_role=<role> -f <este arquivo>
\set ON_ERROR_STOP on
RESET ROLE;
SELECT format('ALTER TABLE %I.%I OWNER TO %I', n.nspname, c.relname, :'database_owner')
FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname = 'plantaopro' AND c.relkind IN ('r', 'v', 'm')
  AND c.relowner::regrole::text <> :'database_owner' \gexec
SELECT format('ALTER SEQUENCE %I.%I OWNER TO %I', n.nspname, c.relname, :'database_owner')
FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname = 'plantaopro' AND c.relkind = 'S'
  AND c.relowner::regrole::text <> :'database_owner' \gexec
SELECT format('ALTER FUNCTION %s OWNER TO %I', p.oid::regproc::text, :'database_owner')
FROM pg_proc p JOIN pg_namespace n ON n.oid = p.pronamespace
WHERE n.nspname = 'plantaopro' AND p.prokind = 'f'
  AND p.proowner::regrole::text <> :'database_owner' \gexec
SELECT format('ALTER PROCEDURE %s OWNER TO %I', p.oid::regproc::text, :'database_owner')
FROM pg_proc p JOIN pg_namespace n ON n.oid = p.pronamespace
WHERE n.nspname = 'plantaopro' AND p.prokind = 'p'
  AND p.proowner::regrole::text <> :'database_owner' \gexec
SELECT format('REVOKE ALL ON SCHEMA plantaopro FROM %I', :'application_role') \gexec
SELECT format('GRANT USAGE ON SCHEMA plantaopro TO %I', :'application_role') \gexec
SELECT format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA plantaopro TO %I', :'application_role') \gexec
SELECT format('GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA plantaopro TO %I', :'application_role') \gexec
SELECT format('GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA plantaopro TO %I', :'application_role') \gexec
