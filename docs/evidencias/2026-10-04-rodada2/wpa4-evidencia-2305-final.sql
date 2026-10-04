-- ============================================================================
-- PlantaoPro | Evidência WP-A4 — imutabilidade v2305 por papel
-- Passo 4/4: estado final + limpeza (executor: superuser postgres)
--
-- I.  nenhuma linha residual da evidência;
-- II. o papel marcador plantaopro_maintenance NÃO tem CREATEROLE/superuser/login
--     (o CREATE ROLE do próprio v2305 ocorreu no tempo de INSTALAÇÃO/MIGRAÇÃO, com a
--     conta de provisionamento da ferramenta Tools.Database — fora da aplicação operacional,
--     que não executa CREATE ROLE em tempo de execução; ver relatório WPA4).
-- ============================================================================
\set ON_ERROR_STOP 1

SELECT count(*) AS linhas_residuais_da_evidencia
FROM plantaopro.adm360_eventos WHERE id = 'e0a1b2c3-d4e5-4f60-a1b2-c3d4e5f60789';

SELECT rolname, rolsuper, rolcreaterole, rolcanlogin, rolinherit
FROM pg_roles
WHERE rolname IN ('plantaopro_maintenance', 'gate_usr', 'gate_maint')
ORDER BY 1;

REVOKE USAGE ON SCHEMA plantaopro FROM gate_usr, gate_maint;
REVOKE SELECT, INSERT, UPDATE, DELETE ON plantaopro.adm360_eventos FROM gate_usr, gate_maint;
DROP ROLE gate_usr;
DROP ROLE gate_maint;
