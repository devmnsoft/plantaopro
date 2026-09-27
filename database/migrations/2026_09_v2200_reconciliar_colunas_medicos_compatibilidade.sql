-- ============================================================================
-- Migration: 2026_09_v2200_reconciliar_colunas_medicos_compatibilidade.sql
-- Objetivo: Reconciliar bases legadas cuja tabela plantaopro.medicos foi criada
--           por instaladores antigos (sem as colunas de CRM, contato, vinculo
--           clinico e identidade multitenant). Idempotente em bases novas ou ja
--           migradas: somente adiciona colunas ausentes, com os tipos exatos do
--           DDL canônico (PlantaoPro_PostgreSQL_Completo.sql + evolucao vivo).
-- Causa: ObterLookupsAsync (GET /api/administrativo360/cadastros/lookups)
--        consulta m.crm, m.codigo, m.cpf e o seed demo completo insere
--        crm/uf_crm/reg_date; bases legadas sem essas colunas produziam
--        Npgsql 42703 "coluna m.crm nao existe" nos endpoints ADM360.
-- Regras: nenhuma coluna existente é alterada nem removida; sem chaves
--        estrangeiras (compatibilidade com bases legadas); histórico preservado.
-- ============================================================================

alter table plantaopro.medicos add column if not exists especialidade_id uuid;
alter table plantaopro.medicos add column if not exists crm varchar(20);
alter table plantaopro.medicos add column if not exists uf_crm char(2);
alter table plantaopro.medicos add column if not exists telefone varchar(20);
alter table plantaopro.medicos add column if not exists email varchar(120);
alter table plantaopro.medicos add column if not exists cidade varchar(80);
alter table plantaopro.medicos add column if not exists estado char(2);
alter table plantaopro.medicos add column if not exists pix_chave varchar(120);
alter table plantaopro.medicos add column if not exists dados_bancarios jsonb;
alter table plantaopro.medicos add column if not exists observacoes text;
alter table plantaopro.medicos add column if not exists reg_date timestamp default now();
alter table plantaopro.medicos add column if not exists reg_update timestamp;
alter table plantaopro.medicos add column if not exists created_by uuid;
alter table plantaopro.medicos add column if not exists updated_by uuid;
alter table plantaopro.medicos add column if not exists cliente_id uuid;
alter table plantaopro.medicos add column if not exists plano_id bigint;
alter table plantaopro.medicos add column if not exists parceiro_id bigint;
alter table plantaopro.medicos add column if not exists dominio varchar(250);
alter table plantaopro.medicos add column if not exists subdominio varchar(120);
alter table plantaopro.medicos add column if not exists api_key_hash varchar(128);
alter table plantaopro.medicos add column if not exists created_at timestamptz not null default now();
alter table plantaopro.medicos add column if not exists updated_at timestamptz;
