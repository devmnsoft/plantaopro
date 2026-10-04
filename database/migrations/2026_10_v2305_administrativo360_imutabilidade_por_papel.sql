-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2305_administrativo360_imutabilidade_por_papel
-- P0 Segurança: papel restrito para o bypass da imutabilidade do Administrativo 360.
--
-- Contexto (evidência empírica em PostgreSQL 18, testado em 2026-10-03 no banco
-- plantaopro_test com papel guc_probe NOLOGIN):
--   * O GUC plantao.bypass_imutabilidade_adm360 é parâmetro placeholder de 2
--     segmentos: qualquer não-superuser consegue SETá-lo livremente (SET_OK).
--   * GRANT/REVOKE ... SET ON PARAMETER plantao.bypass_imutabilidade_adm360 é
--     ACEITO na sintaxe PG18, mas REVOKE FROM PUBLIC não bloqueia o SET de um
--     não-superuser => o privilégio de parâmetro não protege o GUC.
--   * GRANT plantao ON DATABASE ... não existe ("tipo de privilégio desconhecido").
-- Solução (restrição dentro dos triggers, como manda o requisito P0):
--   1. Papel restrito plantaopro_maintenance (NOLOGIN NOINHERIT — marcador, não
--      concede privilégios em tabelas).
--   2. Função plantaopro.fn_adm360_bypass_habilitado(): bypass vale APENAS quando
--      GUC = 'on' E o session_user é superuser OU membro de plantaopro_maintenance.
--   3. As funções trigger v2301 (fn_adm360_evento_imutavel e
--      fn_adm360_vale_evento_progresso) passam a consultar a função restrita em
--      vez de ler o GUC diretamente — os triggers existentes permanecem os mesmos.
-- Uso: ferramentas oficiais de manutenção/migração continuam fazendo
-- SELECT set_config('plantao.bypass_imutabilidade_adm360','on',true) na MESMA
-- transação da escrita protegida; o usuário dessas ferramentas deve ser superuser
-- (dev/CI) ou membro de plantaopro_maintenance (produção).
-- Idempotência: DO bloqueando a criação do papel + CREATE OR REPLACE das funções.
-- ============================================================================

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'plantaopro_maintenance') THEN
        CREATE ROLE plantaopro_maintenance NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
    ELSE
        ALTER ROLE plantaopro_maintenance WITH NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE;
    END IF;
END $$;

COMMENT ON ROLE plantaopro_maintenance IS 'PlantaoPro: papel restrito do bypass de imutabilidade do Administrativo 360 (GUC plantao.bypass_imutabilidade_adm360). Marcador: não concede privilégios em tabelas.';

-- Bypass habilitado = GUC 'on' E session_user privilegiado (superuser ou membro do papel restrito).
CREATE OR REPLACE FUNCTION plantaopro.fn_adm360_bypass_habilitado()
RETURNS boolean
LANGUAGE sql
STABLE
AS $$
    SELECT coalesce(nullif(btrim(current_setting('plantao.bypass_imutabilidade_adm360', true)), ''), 'off') = 'on'
        AND (
            coalesce((SELECT rolsuper FROM pg_roles WHERE rolname = session_user), false)
            OR pg_has_role(session_user, 'plantaopro_maintenance', 'MEMBER')
        );
$$;

COMMENT ON FUNCTION plantaopro.fn_adm360_bypass_habilitado() IS 'P0: true somente quando GUC plantao.bypass_imutabilidade_adm360=''on'' E o session_user é superuser ou membro de plantaopro_maintenance (PG18 testado: privilégio de parâmetro não restringe GUCs placeholder de 2 segmentos).';

-- Recria a trigger de v2301 consultando a função restrita em vez do GUC bruto.
CREATE OR REPLACE FUNCTION plantaopro.fn_adm360_evento_imutavel()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF plantaopro.fn_adm360_bypass_habilitado() THEN
        RETURN COALESCE(NEW, OLD);
    END IF;
    RAISE EXCEPTION 'Evento do modulo Administrativo 360 e imutavel (append-only): UPDATE e DELETE nao permitidos (registro %).', COALESCE(NEW.id, OLD.id);
END;
$$;

-- Recria a trigger de v2301 consultando a função restrita em vez do GUC bruto.
CREATE OR REPLACE FUNCTION plantaopro.fn_adm360_vale_evento_progresso()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF plantaopro.fn_adm360_bypass_habilitado() THEN
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
