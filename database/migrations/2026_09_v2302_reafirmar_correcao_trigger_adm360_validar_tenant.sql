-- ============================================================================
-- Migration: 2026_09_v2302_reafirmar_correcao_trigger_adm360_validar_tenant.sql
-- Objetivo: Reafirmar a correcao da funcao plantaopro.adm360_validar_tenant()
--           introduzida em v2199. O migrador oficial pode re-executar o v2190
--           em um upgrade quando a linha de tracking do v2190 nao estava em
--           schema_migrations enquanto a do v2199 ja existia: nesse cenario o
--           CREATE OR REPLACE FUNCTION do v2190 (corpo com avaliacao dos tres
--           ramos em expressoes separadas) foi aplicado APOS o v2199 e
--           restabeleceu a falha de resolugao do rowtype NEW dentro das
--           subconsultas:
--             ERRO: registro "new" nao tem campo "cargo_id"
--           quebrando INSERT/UPDATE em adm_cargos, adm_colaboradores e
--           adm_contratos_trabalho. Este script reaplica, em ordem cronologica
--           oficial e apos o v2190/v2199, o corpo corrigido em IF/ELSIF por
--           tabela de v2199, de modo que instalacao limpa e upgrade convergem
--           para a mesma funcao. Nomes, triggers, mensagens de erro e dados
--           sao mantidos identicos; nenhuma alteracao de estrutura.
-- Idempotencia: CREATE OR REPLACE FUNCTION. Os gatilhos existentes
--               (trg_adm_cargos_tenant, trg_adm_colaboradores_tenant,
--                trg_adm_contratos_tenant) continuam apontando para esta
--               funcao, sem recriacao.
-- ============================================================================

create or replace function plantaopro.adm360_validar_tenant()
returns trigger
language plpgsql
as $$
begin
  if tg_table_name = 'adm_cargos' then
    if new.departamento_id is not null
       and not exists(select 1 from plantaopro.adm_departamentos d where d.id = new.departamento_id and d.tenant_id = new.tenant_id) then
      raise exception 'Departamento pertence a outro tenant';
    end if;
  elsif tg_table_name = 'adm_colaboradores' then
    if not exists(select 1 from plantaopro.adm_cargos c where c.id = new.cargo_id and c.tenant_id = new.tenant_id) then
      raise exception 'Cargo pertence a outro tenant';
    end if;
  elsif tg_table_name = 'adm_contratos_trabalho' then
    if not exists(select 1 from plantaopro.adm_colaboradores p where p.id = new.colaborador_id and p.tenant_id = new.tenant_id) then
      raise exception 'Colaborador pertence a outro tenant';
    end if;
  end if;
  return new;
end;
$$;
