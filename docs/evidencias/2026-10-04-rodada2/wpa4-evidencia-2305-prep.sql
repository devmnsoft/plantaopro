-- ============================================================================
-- PlantaoPro | Evidência WP-A4 (rodada 2, 2026-10-04) — imutabilidade v2305 por papel
-- Passo 1/4: preparação (executor: superuser postgres @ plantaopro_test)
--
-- Cria os papéis temporários de simulação COM login (a função restrita consulta
-- session_user, que só muda com conexão real — SET ROLE não basta) e insere a
-- linha-base append-only em plantaopro.adm360_eventos.
-- Senha temporária dos papéis de simulação: gate-evidencia (descartável, local).
-- ============================================================================
\set ON_ERROR_STOP 1

-- Limpa estado de execuções anteriores (revokes + drop; a linha residual da evidência
-- é removida com o bypass GUC dentro da mesma transação abaixo — o trigger append-only
-- bloquearia o DELETE do superuser sem o bypass).
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'gate_usr') THEN
        EXECUTE 'REVOKE USAGE ON SCHEMA plantaopro FROM gate_usr';
        EXECUTE 'REVOKE SELECT, INSERT, UPDATE, DELETE ON plantaopro.adm360_eventos FROM gate_usr';
        EXECUTE 'DROP ROLE gate_usr';
    END IF;
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'gate_maint') THEN
        EXECUTE 'REVOKE plantaopro_maintenance FROM gate_maint';
        EXECUTE 'REVOKE USAGE ON SCHEMA plantaopro FROM gate_maint';
        EXECUTE 'REVOKE SELECT, INSERT, UPDATE, DELETE ON plantaopro.adm360_eventos FROM gate_maint';
        EXECUTE 'DROP ROLE gate_maint';
    END IF;
END $$;

CREATE ROLE gate_usr   LOGIN NOSUPERUSER NOINHERIT NOCREATEDB NOCREATEROLE NOREPLICATION PASSWORD 'gate-evidencia';
CREATE ROLE gate_maint LOGIN NOSUPERUSER NOINHERIT NOCREATEDB NOCREATEROLE NOREPLICATION PASSWORD 'gate-evidencia';
GRANT plantaopro_maintenance TO gate_maint;
GRANT USAGE ON SCHEMA plantaopro TO gate_usr, gate_maint;
GRANT SELECT, INSERT, UPDATE, DELETE ON plantaopro.adm360_eventos TO gate_usr, gate_maint;

DO $$
DECLARE t uuid;
BEGIN
    PERFORM set_config('plantao.bypass_imutabilidade_adm360', 'on', true); -- local à transação do bloco
    SELECT id INTO t FROM plantaopro.tenants LIMIT 1;
    IF t IS NULL THEN
        RAISE EXCEPTION 'precondicao ausente: nao ha tenants em plantaopro.tenants';
    END IF;
    DELETE FROM plantaopro.adm360_eventos WHERE id = 'e0a1b2c3-d4e5-4f60-a1b2-c3d4e5f60789';
    INSERT INTO plantaopro.adm360_eventos
        (id, tenant_id, tipo_evento, entidade, entidade_id, descricao, sha256_hash, idempotency_key)
    VALUES
        ('e0a1b2c3-d4e5-4f60-a1b2-c3d4e5f60789', t, 'DECLARACAO_MANUAL', 'GATE_WPA4_2305',
         'e0a1b2c3-d4e5-4f60-a1b2-c3d4e5f60789', 'Evidencia WPA4 v2305 (linha-base)',
         '0a1b2c3d4e5f60718293a4b5c6d7e8f90a1b2c3d4e5f60718293a4b5c6d7e8f9', 'GATE-WPA4-2305');
    RAISE NOTICE 'PREP OK: papeis criados (gate_usr comum; gate_maint membro de plantaopro_maintenance) e linha-base inserida';
END $$;
