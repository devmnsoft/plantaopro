-- ============================================================================
-- PlantaoPro | Evidência WP-A4 — imutabilidade v2305 por papel
-- Passo 2/4: cenário do USUÁRIO COMUM (conexão real como gate_usr)
--
-- B. fn_adm360_bypass_habilitado() = FALSE com GUC on
--    (demonstra o gotcha PG18 documentado no cabeçalho do v2305: o não-superuser
--     CONSEGUE SETar o GUC placeholder de 2 segmentos — a proteção é a função restrita)
-- C. UPDATE bloqueado pelo trigger (append-only)
-- D. DELETE bloqueado pelo trigger (append-only)
-- ============================================================================
\set ON_ERROR_STOP 1

SELECT 'gate_usr' AS conexao_real, current_user AS current_user_efetivo, session_user AS session_user;

-- [B] GUC on → função restrita deve ser false ----------------------------------
DO $$
DECLARE r boolean;
BEGIN
    PERFORM set_config('plantao.bypass_imutabilidade_adm360', 'on', false);
    r := plantaopro.fn_adm360_bypass_habilitado();
    PERFORM set_config('plantao.bypass_imutabilidade_adm360', 'off', false);
    IF r THEN RAISE EXCEPTION 'B FALHOU: funcao retornou TRUE para usuario comum com GUC on'; END IF;
    RAISE NOTICE 'B OK: usuario comum SETou o GUC livremente (gotcha PG18), mas fn_adm360_bypass_habilitado() = false';
END $$;

-- [C] GUC on → UPDATE bloqueado -------------------------------------------------
DO $$
BEGIN
    PERFORM set_config('plantao.bypass_imutabilidade_adm360', 'on', false);
    UPDATE plantaopro.adm360_eventos SET descricao = descricao WHERE id = 'e0a1b2c3-d4e5-4f60-a1b2-c3d4e5f60789';
    RAISE EXCEPTION 'C FALHOU: UPDATE do usuario comum nao foi bloqueado';
EXCEPTION WHEN raise_exception THEN
    BEGIN
        IF sqlerrm LIKE '%imutavel%' THEN
            RAISE NOTICE 'C OK: UPDATE bloqueado para usuario comum (GUC on): %', sqlerrm;
        ELSE
            RAISE;
        END IF;
    END;
END $$;

-- [D] GUC on → DELETE bloqueado -------------------------------------------------
DO $$
BEGIN
    PERFORM set_config('plantao.bypass_imutabilidade_adm360', 'on', false);
    DELETE FROM plantaopro.adm360_eventos WHERE id = 'e0a1b2c3-d4e5-4f60-a1b2-c3d4e5f60789';
    RAISE EXCEPTION 'D FALHOU: DELETE do usuario comum nao foi bloqueado';
EXCEPTION WHEN raise_exception THEN
    BEGIN
        IF sqlerrm LIKE '%imutavel%' THEN
            RAISE NOTICE 'D OK: DELETE bloqueado para usuario comum (GUC on): %', sqlerrm;
        ELSE
            RAISE;
        END IF;
    END;
END $$;
