-- ============================================================================
-- PlantaoPro | Evidência WP-A4 — imutabilidade v2305 por papel
-- Passo 3/4: cenário do MEMBRO de plantaopro_maintenance (conexão real como gate_maint)
--
-- E. GUC on  → fn_adm360_bypass_habilitado() = TRUE
-- F. GUC off → fn_adm360_bypass_habilitado() = FALSE (o papel sozinho não habilita)
-- G. GUC on  → UPDATE permitido (1 linha)
-- H. GUC on  → DELETE permitido (1 linha; remove a linha-base da evidência)
-- ============================================================================
\set ON_ERROR_STOP 1

SELECT 'gate_maint' AS conexao_real, current_user AS current_user_efetivo, session_user AS session_user;

-- [E] GUC on → função restrita = true -------------------------------------------
DO $$
DECLARE r boolean;
BEGIN
    PERFORM set_config('plantao.bypass_imutabilidade_adm360', 'on', false);
    r := plantaopro.fn_adm360_bypass_habilitado();
    PERFORM set_config('plantao.bypass_imutabilidade_adm360', 'off', false);
    IF NOT r THEN RAISE EXCEPTION 'E FALHOU: funcao deveria ser TRUE para membro de plantaopro_maintenance com GUC on'; END IF;
    RAISE NOTICE 'E OK: membro de plantaopro_maintenance com GUC on -> fn_adm360_bypass_habilitado() = true';
END $$;

-- [F] GUC off → função restrita = false ------------------------------------------
DO $$
DECLARE r boolean;
BEGIN
    PERFORM set_config('plantao.bypass_imutabilidade_adm360', 'off', false);
    r := plantaopro.fn_adm360_bypass_habilitado();
    IF r THEN RAISE EXCEPTION 'F FALHOU: funcao retornou TRUE sem GUC on'; END IF;
    RAISE NOTICE 'F OK: membro de plantaopro_maintenance sem GUC on -> fn_adm360_bypass_habilitado() = false';
END $$;

-- [G] GUC on → UPDATE permitido ---------------------------------------------------
DO $$
DECLARE n int;
BEGIN
    PERFORM set_config('plantao.bypass_imutabilidade_adm360', 'on', false);
    UPDATE plantaopro.adm360_eventos SET descricao = 'Evidencia WPA4 v2305 (update com bypass)' WHERE id = 'e0a1b2c3-d4e5-4f60-a1b2-c3d4e5f60789';
    GET DIAGNOSTICS n = ROW_COUNT;
    PERFORM set_config('plantao.bypass_imutabilidade_adm360', 'off', false);
    IF n <> 1 THEN RAISE EXCEPTION 'G FALHOU: update do membro nao afetou exatamente 1 linha (n=%)', n; END IF;
    RAISE NOTICE 'G OK: membro de plantaopro_maintenance com GUC on atualizou o evento (1 linha)';
END $$;

-- [H] GUC on → DELETE permitido (limpa a linha-base) -------------------------------
DO $$
DECLARE n int;
BEGIN
    PERFORM set_config('plantao.bypass_imutabilidade_adm360', 'on', false);
    DELETE FROM plantaopro.adm360_eventos WHERE id = 'e0a1b2c3-d4e5-4f60-a1b2-c3d4e5f60789';
    GET DIAGNOSTICS n = ROW_COUNT;
    PERFORM set_config('plantao.bypass_imutabilidade_adm360', 'off', false);
    IF n <> 1 THEN RAISE EXCEPTION 'H FALHOU: delete do membro nao removeu exatamente 1 linha (n=%)', n; END IF;
    RAISE NOTICE 'H OK: membro de plantaopro_maintenance com GUC on removeu o evento (1 linha)';
END $$;
