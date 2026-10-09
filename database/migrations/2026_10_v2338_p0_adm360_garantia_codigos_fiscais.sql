-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2338_p0_adm360_garantia_codigos_fiscais
-- Guarda de compatibilidade P0 (ensaio rodada 5): a sequencia canonica de
-- migrations cria em permissoes apenas o conjunto historico de codigos ADM360
-- (2026_09_v2198); os 7 codigos fiscaus novos (CRIAR, EDITAR, CONFIGURAR,
-- REABRIR, CONFIRMAR, CANCELAR, CONFERIR) eram criados somente pelo catalogo
-- de permissoes do runtime da API. Isso fazia a sanidade da
-- 2026_10_v2322_adm360_grants_adm_cliente_r5_a2 falhar em bancos novos ou
-- legados onde a API nova ainda nao subiu antes do upgrade de banco.
-- Esta guarda cadastra os codigos faltantes com o MESMO padrao da v2198
-- (acao EDITAR para acoes de escrita, modulo ADM360 quando existir) e e
-- integralmente idempotente: nao duplica onde o runtime ja criou e nao
-- altera codigos existentes.
-- ============================================================================

DO $migration$
DECLARE
    v_acao_editar_id uuid;
    v_modulo_adm_id uuid;
    v_perm_code text;
BEGIN
    select id into v_acao_editar_id
      from plantaopro.acoes_sistema
     where codigo in ('EDITAR', 'SALVAR') order by id limit 1;
    if v_acao_editar_id is null then
        v_acao_editar_id := gen_random_uuid();
        insert into plantaopro.acoes_sistema(id, codigo, nome, status, reg_status, reg_date)
        values (v_acao_editar_id, 'EDITAR', 'Editar', 'ATIVO', 'A', now());
    end if;

    select id into v_modulo_adm_id
      from plantaopro.modulos_sistema
     where upper(btrim(codigo)) = 'ADM360' and reg_status = 'A' limit 1;

    foreach v_perm_code in array array[
        'ADM360:CRIAR',
        'ADM360:EDITAR',
        'ADM360:CONFIGURAR',
        'ADM360:REABRIR',
        'ADM360:CONFIRMAR',
        'ADM360:CANCELAR',
        'ADM360:CONFERIR'
    ] loop
        if not exists (
            select 1 from plantaopro.permissoes
             where upper(btrim(codigo)) = upper(btrim(v_perm_code))
               and reg_status = 'A'
        ) then
            insert into plantaopro.permissoes (id, acao_id, modulo_id, codigo, nome, status, reg_status, reg_date)
            values (gen_random_uuid(), v_acao_editar_id, v_modulo_adm_id, v_perm_code,
                    replace(v_perm_code, ':', ' - '), 'ATIVO', 'A', now());
        end if;
    end loop;
END
$migration$;
