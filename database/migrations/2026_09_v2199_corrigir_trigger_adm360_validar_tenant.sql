-- ============================================================================
-- Migration: 2026_09_v2199_corrigir_trigger_adm360_validar_tenant.sql
-- Objetivo: Corrigir a funcao de validacao de tenant criada em v2190.
--           A funcao v2190 avaliava os tres ramos em expressoes separadas, cada
--           uma contendo subconsulta que referencia NEW.<coluna> existente em
--           apenas UMA das tabelas cobertas (adm_cargos.departamento_id,
--           adm_colaboradores.cargo_id, adm_contratos_trabalho.colaborador_id).
--           O PL/pgSQL resolve o campo do registro NEW dentro das subconsultas
--           contra o rowtype da tabela onde o gatilho disparou, mesmo quando a
--           condicao anterior do AND deveria curto-circuitar, causando falha em
--           QUALQUER INSERT/UPDATE nas tres tabelas:
--             ERRO: registro "new" nao tem campo "cargo_id"
--           Este script reorganiza a funcao em IF/ELSIF por tabela: cada ramo so
--           referencia colunas da propria tabela e so e planejado/executado quando
--           o gatilho dispara naquela tabela. Gatilhos, nomes e mensagens de erro
--           sao mantidos identicos (semantica preservada); nenhuma alteracao de
--           dados ou de estrutura.
-- Idempotencia: CREATE OR REPLACE FUNCTION. Nao altera triggers existentes
--               (trg_adm_cargos_tenant, trg_adm_colaboradores_tenant,
--                trg_adm_contratos_tenant continuam apontando para esta funcao).
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
