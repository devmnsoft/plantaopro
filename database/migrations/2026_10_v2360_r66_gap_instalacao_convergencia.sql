-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2360_r66_gap_instalacao_convergencia
-- R66 (plano 50 sprints, bloco B1): convergencia de colunas para a importacao unica.
-- As secoes curadas criam tabelas com menos colunas que as definicoes legadas que
-- passam a compor o mesmo script; esta secao alinha as colunas ao estado sabio do
-- ambiente em operacao (gerado da diferenca entre o banco local em uso e a importacao
-- limpa do script original). Idempotente: ADD COLUMN IF NOT EXISTS.
-- ============================================================================
-- clinica_unidades / clinica_salas: referenciadas pelo codigo (CentralAtendimentoService)
-- sem criador em nenhum arquivo do repo; definicao minima para a instalacao unica fechar.
create table if not exists plantaopro.clinica_unidades (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    nome varchar(200) not null,
    reg_date timestamptz not null default now(),
    reg_update timestamptz,
    reg_status char(1) not null default 'A'
);
create table if not exists plantaopro.clinica_salas (
    id uuid primary key default gen_random_uuid(),
    unidade_id uuid null,
    tenant_id uuid null,
    nome varchar(200) not null,
    reg_date timestamptz not null default now(),
    reg_update timestamptz,
    reg_status char(1) not null default 'A'
);
-- fim convergencia r66 (colunas adicionadas: 0)
