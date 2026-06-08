-- PLANTÃOPRO — VERSÃO COMERCIAL DEMONSTRÁVEL, WHITE LABEL, SELF-SERVICE E FUNCIONAL
-- Script incremental idempotente. Não remove dados existentes.
create schema if not exists plantaopro;

create table if not exists plantaopro.public_landing_secoes (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.public_landing_secoes add column if not exists codigo varchar(120);
alter table plantaopro.public_landing_secoes add column if not exists nome varchar(220);
alter table plantaopro.public_landing_secoes add column if not exists descricao text;
alter table plantaopro.public_landing_secoes add column if not exists status varchar(40);
alter table plantaopro.public_landing_secoes add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.public_landing_secoes add column if not exists is_demo boolean default false;
alter table plantaopro.public_landing_secoes add column if not exists reg_status char(1) default 'A';
alter table plantaopro.public_landing_secoes add column if not exists reg_date timestamptz default now();
alter table plantaopro.public_landing_secoes add column if not exists reg_update timestamptz;
create index if not exists ix_public_landing_secoes_status on plantaopro.public_landing_secoes (status);
create index if not exists ix_public_landing_secoes_reg_date on plantaopro.public_landing_secoes (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_public_landing_secoes_reg_status') then
        alter table plantaopro.public_landing_secoes add constraint ck_public_landing_secoes_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.public_landing_depoimentos (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.public_landing_depoimentos add column if not exists codigo varchar(120);
alter table plantaopro.public_landing_depoimentos add column if not exists nome varchar(220);
alter table plantaopro.public_landing_depoimentos add column if not exists descricao text;
alter table plantaopro.public_landing_depoimentos add column if not exists status varchar(40);
alter table plantaopro.public_landing_depoimentos add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.public_landing_depoimentos add column if not exists is_demo boolean default false;
alter table plantaopro.public_landing_depoimentos add column if not exists reg_status char(1) default 'A';
alter table plantaopro.public_landing_depoimentos add column if not exists reg_date timestamptz default now();
alter table plantaopro.public_landing_depoimentos add column if not exists reg_update timestamptz;
create index if not exists ix_public_landing_depoimentos_status on plantaopro.public_landing_depoimentos (status);
create index if not exists ix_public_landing_depoimentos_reg_date on plantaopro.public_landing_depoimentos (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_public_landing_depoimentos_reg_status') then
        alter table plantaopro.public_landing_depoimentos add constraint ck_public_landing_depoimentos_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.public_landing_faq (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.public_landing_faq add column if not exists codigo varchar(120);
alter table plantaopro.public_landing_faq add column if not exists nome varchar(220);
alter table plantaopro.public_landing_faq add column if not exists descricao text;
alter table plantaopro.public_landing_faq add column if not exists status varchar(40);
alter table plantaopro.public_landing_faq add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.public_landing_faq add column if not exists is_demo boolean default false;
alter table plantaopro.public_landing_faq add column if not exists reg_status char(1) default 'A';
alter table plantaopro.public_landing_faq add column if not exists reg_date timestamptz default now();
alter table plantaopro.public_landing_faq add column if not exists reg_update timestamptz;
create index if not exists ix_public_landing_faq_status on plantaopro.public_landing_faq (status);
create index if not exists ix_public_landing_faq_reg_date on plantaopro.public_landing_faq (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_public_landing_faq_reg_status') then
        alter table plantaopro.public_landing_faq add constraint ck_public_landing_faq_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.public_landing_ctas (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.public_landing_ctas add column if not exists codigo varchar(120);
alter table plantaopro.public_landing_ctas add column if not exists nome varchar(220);
alter table plantaopro.public_landing_ctas add column if not exists descricao text;
alter table plantaopro.public_landing_ctas add column if not exists status varchar(40);
alter table plantaopro.public_landing_ctas add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.public_landing_ctas add column if not exists is_demo boolean default false;
alter table plantaopro.public_landing_ctas add column if not exists reg_status char(1) default 'A';
alter table plantaopro.public_landing_ctas add column if not exists reg_date timestamptz default now();
alter table plantaopro.public_landing_ctas add column if not exists reg_update timestamptz;
create index if not exists ix_public_landing_ctas_status on plantaopro.public_landing_ctas (status);
create index if not exists ix_public_landing_ctas_reg_date on plantaopro.public_landing_ctas (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_public_landing_ctas_reg_status') then
        alter table plantaopro.public_landing_ctas add constraint ck_public_landing_ctas_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.public_landing_casos_uso (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.public_landing_casos_uso add column if not exists codigo varchar(120);
alter table plantaopro.public_landing_casos_uso add column if not exists nome varchar(220);
alter table plantaopro.public_landing_casos_uso add column if not exists descricao text;
alter table plantaopro.public_landing_casos_uso add column if not exists status varchar(40);
alter table plantaopro.public_landing_casos_uso add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.public_landing_casos_uso add column if not exists is_demo boolean default false;
alter table plantaopro.public_landing_casos_uso add column if not exists reg_status char(1) default 'A';
alter table plantaopro.public_landing_casos_uso add column if not exists reg_date timestamptz default now();
alter table plantaopro.public_landing_casos_uso add column if not exists reg_update timestamptz;
create index if not exists ix_public_landing_casos_uso_status on plantaopro.public_landing_casos_uso (status);
create index if not exists ix_public_landing_casos_uso_reg_date on plantaopro.public_landing_casos_uso (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_public_landing_casos_uso_reg_status') then
        alter table plantaopro.public_landing_casos_uso add constraint ck_public_landing_casos_uso_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.public_landing_materiais (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.public_landing_materiais add column if not exists codigo varchar(120);
alter table plantaopro.public_landing_materiais add column if not exists nome varchar(220);
alter table plantaopro.public_landing_materiais add column if not exists descricao text;
alter table plantaopro.public_landing_materiais add column if not exists status varchar(40);
alter table plantaopro.public_landing_materiais add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.public_landing_materiais add column if not exists is_demo boolean default false;
alter table plantaopro.public_landing_materiais add column if not exists reg_status char(1) default 'A';
alter table plantaopro.public_landing_materiais add column if not exists reg_date timestamptz default now();
alter table plantaopro.public_landing_materiais add column if not exists reg_update timestamptz;
create index if not exists ix_public_landing_materiais_status on plantaopro.public_landing_materiais (status);
create index if not exists ix_public_landing_materiais_reg_date on plantaopro.public_landing_materiais (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_public_landing_materiais_reg_status') then
        alter table plantaopro.public_landing_materiais add constraint ck_public_landing_materiais_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.public_landing_leads (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.public_landing_leads add column if not exists codigo varchar(120);
alter table plantaopro.public_landing_leads add column if not exists nome varchar(220);
alter table plantaopro.public_landing_leads add column if not exists descricao text;
alter table plantaopro.public_landing_leads add column if not exists status varchar(40);
alter table plantaopro.public_landing_leads add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.public_landing_leads add column if not exists is_demo boolean default false;
alter table plantaopro.public_landing_leads add column if not exists reg_status char(1) default 'A';
alter table plantaopro.public_landing_leads add column if not exists reg_date timestamptz default now();
alter table plantaopro.public_landing_leads add column if not exists reg_update timestamptz;
create index if not exists ix_public_landing_leads_status on plantaopro.public_landing_leads (status);
create index if not exists ix_public_landing_leads_reg_date on plantaopro.public_landing_leads (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_public_landing_leads_reg_status') then
        alter table plantaopro.public_landing_leads add constraint ck_public_landing_leads_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.simulador_planos_perguntas (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.simulador_planos_perguntas add column if not exists codigo varchar(120);
alter table plantaopro.simulador_planos_perguntas add column if not exists nome varchar(220);
alter table plantaopro.simulador_planos_perguntas add column if not exists descricao text;
alter table plantaopro.simulador_planos_perguntas add column if not exists status varchar(40);
alter table plantaopro.simulador_planos_perguntas add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.simulador_planos_perguntas add column if not exists is_demo boolean default false;
alter table plantaopro.simulador_planos_perguntas add column if not exists reg_status char(1) default 'A';
alter table plantaopro.simulador_planos_perguntas add column if not exists reg_date timestamptz default now();
alter table plantaopro.simulador_planos_perguntas add column if not exists reg_update timestamptz;
create index if not exists ix_simulador_planos_perguntas_status on plantaopro.simulador_planos_perguntas (status);
create index if not exists ix_simulador_planos_perguntas_reg_date on plantaopro.simulador_planos_perguntas (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_simulador_planos_perguntas_reg_status') then
        alter table plantaopro.simulador_planos_perguntas add constraint ck_simulador_planos_perguntas_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.simulador_planos_respostas (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.simulador_planos_respostas add column if not exists codigo varchar(120);
alter table plantaopro.simulador_planos_respostas add column if not exists nome varchar(220);
alter table plantaopro.simulador_planos_respostas add column if not exists descricao text;
alter table plantaopro.simulador_planos_respostas add column if not exists status varchar(40);
alter table plantaopro.simulador_planos_respostas add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.simulador_planos_respostas add column if not exists is_demo boolean default false;
alter table plantaopro.simulador_planos_respostas add column if not exists reg_status char(1) default 'A';
alter table plantaopro.simulador_planos_respostas add column if not exists reg_date timestamptz default now();
alter table plantaopro.simulador_planos_respostas add column if not exists reg_update timestamptz;
create index if not exists ix_simulador_planos_respostas_status on plantaopro.simulador_planos_respostas (status);
create index if not exists ix_simulador_planos_respostas_reg_date on plantaopro.simulador_planos_respostas (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_simulador_planos_respostas_reg_status') then
        alter table plantaopro.simulador_planos_respostas add constraint ck_simulador_planos_respostas_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.simulador_planos_resultados (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.simulador_planos_resultados add column if not exists codigo varchar(120);
alter table plantaopro.simulador_planos_resultados add column if not exists nome varchar(220);
alter table plantaopro.simulador_planos_resultados add column if not exists descricao text;
alter table plantaopro.simulador_planos_resultados add column if not exists status varchar(40);
alter table plantaopro.simulador_planos_resultados add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.simulador_planos_resultados add column if not exists is_demo boolean default false;
alter table plantaopro.simulador_planos_resultados add column if not exists reg_status char(1) default 'A';
alter table plantaopro.simulador_planos_resultados add column if not exists reg_date timestamptz default now();
alter table plantaopro.simulador_planos_resultados add column if not exists reg_update timestamptz;
create index if not exists ix_simulador_planos_resultados_status on plantaopro.simulador_planos_resultados (status);
create index if not exists ix_simulador_planos_resultados_reg_date on plantaopro.simulador_planos_resultados (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_simulador_planos_resultados_reg_status') then
        alter table plantaopro.simulador_planos_resultados add constraint ck_simulador_planos_resultados_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.simulador_planos_historico (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.simulador_planos_historico add column if not exists codigo varchar(120);
alter table plantaopro.simulador_planos_historico add column if not exists nome varchar(220);
alter table plantaopro.simulador_planos_historico add column if not exists descricao text;
alter table plantaopro.simulador_planos_historico add column if not exists status varchar(40);
alter table plantaopro.simulador_planos_historico add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.simulador_planos_historico add column if not exists is_demo boolean default false;
alter table plantaopro.simulador_planos_historico add column if not exists reg_status char(1) default 'A';
alter table plantaopro.simulador_planos_historico add column if not exists reg_date timestamptz default now();
alter table plantaopro.simulador_planos_historico add column if not exists reg_update timestamptz;
create index if not exists ix_simulador_planos_historico_status on plantaopro.simulador_planos_historico (status);
create index if not exists ix_simulador_planos_historico_reg_date on plantaopro.simulador_planos_historico (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_simulador_planos_historico_reg_status') then
        alter table plantaopro.simulador_planos_historico add constraint ck_simulador_planos_historico_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.proposta_comercial_templates (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.proposta_comercial_templates add column if not exists codigo varchar(120);
alter table plantaopro.proposta_comercial_templates add column if not exists nome varchar(220);
alter table plantaopro.proposta_comercial_templates add column if not exists descricao text;
alter table plantaopro.proposta_comercial_templates add column if not exists status varchar(40);
alter table plantaopro.proposta_comercial_templates add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.proposta_comercial_templates add column if not exists is_demo boolean default false;
alter table plantaopro.proposta_comercial_templates add column if not exists reg_status char(1) default 'A';
alter table plantaopro.proposta_comercial_templates add column if not exists reg_date timestamptz default now();
alter table plantaopro.proposta_comercial_templates add column if not exists reg_update timestamptz;
create index if not exists ix_proposta_comercial_templates_status on plantaopro.proposta_comercial_templates (status);
create index if not exists ix_proposta_comercial_templates_reg_date on plantaopro.proposta_comercial_templates (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_proposta_comercial_templates_reg_status') then
        alter table plantaopro.proposta_comercial_templates add constraint ck_proposta_comercial_templates_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.demo_roteiros (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.demo_roteiros add column if not exists codigo varchar(120);
alter table plantaopro.demo_roteiros add column if not exists nome varchar(220);
alter table plantaopro.demo_roteiros add column if not exists descricao text;
alter table plantaopro.demo_roteiros add column if not exists status varchar(40);
alter table plantaopro.demo_roteiros add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.demo_roteiros add column if not exists is_demo boolean default false;
alter table plantaopro.demo_roteiros add column if not exists reg_status char(1) default 'A';
alter table plantaopro.demo_roteiros add column if not exists reg_date timestamptz default now();
alter table plantaopro.demo_roteiros add column if not exists reg_update timestamptz;
create index if not exists ix_demo_roteiros_status on plantaopro.demo_roteiros (status);
create index if not exists ix_demo_roteiros_reg_date on plantaopro.demo_roteiros (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_demo_roteiros_reg_status') then
        alter table plantaopro.demo_roteiros add constraint ck_demo_roteiros_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.demo_checklists (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.demo_checklists add column if not exists codigo varchar(120);
alter table plantaopro.demo_checklists add column if not exists nome varchar(220);
alter table plantaopro.demo_checklists add column if not exists descricao text;
alter table plantaopro.demo_checklists add column if not exists status varchar(40);
alter table plantaopro.demo_checklists add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.demo_checklists add column if not exists is_demo boolean default false;
alter table plantaopro.demo_checklists add column if not exists reg_status char(1) default 'A';
alter table plantaopro.demo_checklists add column if not exists reg_date timestamptz default now();
alter table plantaopro.demo_checklists add column if not exists reg_update timestamptz;
create index if not exists ix_demo_checklists_status on plantaopro.demo_checklists (status);
create index if not exists ix_demo_checklists_reg_date on plantaopro.demo_checklists (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_demo_checklists_reg_status') then
        alter table plantaopro.demo_checklists add constraint ck_demo_checklists_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.modulos_sistema (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.modulos_sistema add column if not exists codigo varchar(120);
alter table plantaopro.modulos_sistema add column if not exists nome varchar(220);
alter table plantaopro.modulos_sistema add column if not exists descricao text;
alter table plantaopro.modulos_sistema add column if not exists status varchar(40);
alter table plantaopro.modulos_sistema add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.modulos_sistema add column if not exists is_demo boolean default false;
alter table plantaopro.modulos_sistema add column if not exists reg_status char(1) default 'A';
alter table plantaopro.modulos_sistema add column if not exists reg_date timestamptz default now();
alter table plantaopro.modulos_sistema add column if not exists reg_update timestamptz;
create index if not exists ix_modulos_sistema_status on plantaopro.modulos_sistema (status);
create index if not exists ix_modulos_sistema_reg_date on plantaopro.modulos_sistema (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_modulos_sistema_reg_status') then
        alter table plantaopro.modulos_sistema add constraint ck_modulos_sistema_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.proposta_templates (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.proposta_templates add column if not exists codigo varchar(120);
alter table plantaopro.proposta_templates add column if not exists nome varchar(220);
alter table plantaopro.proposta_templates add column if not exists descricao text;
alter table plantaopro.proposta_templates add column if not exists status varchar(40);
alter table plantaopro.proposta_templates add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.proposta_templates add column if not exists is_demo boolean default false;
alter table plantaopro.proposta_templates add column if not exists reg_status char(1) default 'A';
alter table plantaopro.proposta_templates add column if not exists reg_date timestamptz default now();
alter table plantaopro.proposta_templates add column if not exists reg_update timestamptz;
create index if not exists ix_proposta_templates_status on plantaopro.proposta_templates (status);
create index if not exists ix_proposta_templates_reg_date on plantaopro.proposta_templates (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_proposta_templates_reg_status') then
        alter table plantaopro.proposta_templates add constraint ck_proposta_templates_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.proposta_template_secoes (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.proposta_template_secoes add column if not exists codigo varchar(120);
alter table plantaopro.proposta_template_secoes add column if not exists nome varchar(220);
alter table plantaopro.proposta_template_secoes add column if not exists descricao text;
alter table plantaopro.proposta_template_secoes add column if not exists status varchar(40);
alter table plantaopro.proposta_template_secoes add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.proposta_template_secoes add column if not exists is_demo boolean default false;
alter table plantaopro.proposta_template_secoes add column if not exists reg_status char(1) default 'A';
alter table plantaopro.proposta_template_secoes add column if not exists reg_date timestamptz default now();
alter table plantaopro.proposta_template_secoes add column if not exists reg_update timestamptz;
create index if not exists ix_proposta_template_secoes_status on plantaopro.proposta_template_secoes (status);
create index if not exists ix_proposta_template_secoes_reg_date on plantaopro.proposta_template_secoes (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_proposta_template_secoes_reg_status') then
        alter table plantaopro.proposta_template_secoes add constraint ck_proposta_template_secoes_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.proposta_template_variaveis (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.proposta_template_variaveis add column if not exists codigo varchar(120);
alter table plantaopro.proposta_template_variaveis add column if not exists nome varchar(220);
alter table plantaopro.proposta_template_variaveis add column if not exists descricao text;
alter table plantaopro.proposta_template_variaveis add column if not exists status varchar(40);
alter table plantaopro.proposta_template_variaveis add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.proposta_template_variaveis add column if not exists is_demo boolean default false;
alter table plantaopro.proposta_template_variaveis add column if not exists reg_status char(1) default 'A';
alter table plantaopro.proposta_template_variaveis add column if not exists reg_date timestamptz default now();
alter table plantaopro.proposta_template_variaveis add column if not exists reg_update timestamptz;
create index if not exists ix_proposta_template_variaveis_status on plantaopro.proposta_template_variaveis (status);
create index if not exists ix_proposta_template_variaveis_reg_date on plantaopro.proposta_template_variaveis (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_proposta_template_variaveis_reg_status') then
        alter table plantaopro.proposta_template_variaveis add constraint ck_proposta_template_variaveis_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.proposta_template_itens (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.proposta_template_itens add column if not exists codigo varchar(120);
alter table plantaopro.proposta_template_itens add column if not exists nome varchar(220);
alter table plantaopro.proposta_template_itens add column if not exists descricao text;
alter table plantaopro.proposta_template_itens add column if not exists status varchar(40);
alter table plantaopro.proposta_template_itens add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.proposta_template_itens add column if not exists is_demo boolean default false;
alter table plantaopro.proposta_template_itens add column if not exists reg_status char(1) default 'A';
alter table plantaopro.proposta_template_itens add column if not exists reg_date timestamptz default now();
alter table plantaopro.proposta_template_itens add column if not exists reg_update timestamptz;
create index if not exists ix_proposta_template_itens_status on plantaopro.proposta_template_itens (status);
create index if not exists ix_proposta_template_itens_reg_date on plantaopro.proposta_template_itens (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_proposta_template_itens_reg_status') then
        alter table plantaopro.proposta_template_itens add constraint ck_proposta_template_itens_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.comercial_playbooks (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.comercial_playbooks add column if not exists codigo varchar(120);
alter table plantaopro.comercial_playbooks add column if not exists nome varchar(220);
alter table plantaopro.comercial_playbooks add column if not exists descricao text;
alter table plantaopro.comercial_playbooks add column if not exists status varchar(40);
alter table plantaopro.comercial_playbooks add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.comercial_playbooks add column if not exists is_demo boolean default false;
alter table plantaopro.comercial_playbooks add column if not exists reg_status char(1) default 'A';
alter table plantaopro.comercial_playbooks add column if not exists reg_date timestamptz default now();
alter table plantaopro.comercial_playbooks add column if not exists reg_update timestamptz;
create index if not exists ix_comercial_playbooks_status on plantaopro.comercial_playbooks (status);
create index if not exists ix_comercial_playbooks_reg_date on plantaopro.comercial_playbooks (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_comercial_playbooks_reg_status') then
        alter table plantaopro.comercial_playbooks add constraint ck_comercial_playbooks_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro."comercial_objeções" (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro."comercial_objeções" add column if not exists codigo varchar(120);
alter table plantaopro."comercial_objeções" add column if not exists nome varchar(220);
alter table plantaopro."comercial_objeções" add column if not exists descricao text;
alter table plantaopro."comercial_objeções" add column if not exists status varchar(40);
alter table plantaopro."comercial_objeções" add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro."comercial_objeções" add column if not exists is_demo boolean default false;
alter table plantaopro."comercial_objeções" add column if not exists reg_status char(1) default 'A';
alter table plantaopro."comercial_objeções" add column if not exists reg_date timestamptz default now();
alter table plantaopro."comercial_objeções" add column if not exists reg_update timestamptz;
create index if not exists ix_comercial_objeções_status on plantaopro."comercial_objeções" (status);
create index if not exists ix_comercial_objeções_reg_date on plantaopro."comercial_objeções" (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_comercial_objeções_reg_status') then
        alter table plantaopro."comercial_objeções" add constraint ck_comercial_objeções_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.comercial_scripts (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.comercial_scripts add column if not exists codigo varchar(120);
alter table plantaopro.comercial_scripts add column if not exists nome varchar(220);
alter table plantaopro.comercial_scripts add column if not exists descricao text;
alter table plantaopro.comercial_scripts add column if not exists status varchar(40);
alter table plantaopro.comercial_scripts add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.comercial_scripts add column if not exists is_demo boolean default false;
alter table plantaopro.comercial_scripts add column if not exists reg_status char(1) default 'A';
alter table plantaopro.comercial_scripts add column if not exists reg_date timestamptz default now();
alter table plantaopro.comercial_scripts add column if not exists reg_update timestamptz;
create index if not exists ix_comercial_scripts_status on plantaopro.comercial_scripts (status);
create index if not exists ix_comercial_scripts_reg_date on plantaopro.comercial_scripts (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_comercial_scripts_reg_status') then
        alter table plantaopro.comercial_scripts add constraint ck_comercial_scripts_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.comercial_cadencias (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.comercial_cadencias add column if not exists codigo varchar(120);
alter table plantaopro.comercial_cadencias add column if not exists nome varchar(220);
alter table plantaopro.comercial_cadencias add column if not exists descricao text;
alter table plantaopro.comercial_cadencias add column if not exists status varchar(40);
alter table plantaopro.comercial_cadencias add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.comercial_cadencias add column if not exists is_demo boolean default false;
alter table plantaopro.comercial_cadencias add column if not exists reg_status char(1) default 'A';
alter table plantaopro.comercial_cadencias add column if not exists reg_date timestamptz default now();
alter table plantaopro.comercial_cadencias add column if not exists reg_update timestamptz;
create index if not exists ix_comercial_cadencias_status on plantaopro.comercial_cadencias (status);
create index if not exists ix_comercial_cadencias_reg_date on plantaopro.comercial_cadencias (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_comercial_cadencias_reg_status') then
        alter table plantaopro.comercial_cadencias add constraint ck_comercial_cadencias_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.comercial_metas (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.comercial_metas add column if not exists codigo varchar(120);
alter table plantaopro.comercial_metas add column if not exists nome varchar(220);
alter table plantaopro.comercial_metas add column if not exists descricao text;
alter table plantaopro.comercial_metas add column if not exists status varchar(40);
alter table plantaopro.comercial_metas add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.comercial_metas add column if not exists is_demo boolean default false;
alter table plantaopro.comercial_metas add column if not exists reg_status char(1) default 'A';
alter table plantaopro.comercial_metas add column if not exists reg_date timestamptz default now();
alter table plantaopro.comercial_metas add column if not exists reg_update timestamptz;
create index if not exists ix_comercial_metas_status on plantaopro.comercial_metas (status);
create index if not exists ix_comercial_metas_reg_date on plantaopro.comercial_metas (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_comercial_metas_reg_status') then
        alter table plantaopro.comercial_metas add constraint ck_comercial_metas_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.propostas_comerciais (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.propostas_comerciais add column if not exists codigo varchar(120);
alter table plantaopro.propostas_comerciais add column if not exists nome varchar(220);
alter table plantaopro.propostas_comerciais add column if not exists descricao text;
alter table plantaopro.propostas_comerciais add column if not exists status varchar(40);
alter table plantaopro.propostas_comerciais add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.propostas_comerciais add column if not exists is_demo boolean default false;
alter table plantaopro.propostas_comerciais add column if not exists reg_status char(1) default 'A';
alter table plantaopro.propostas_comerciais add column if not exists reg_date timestamptz default now();
alter table plantaopro.propostas_comerciais add column if not exists reg_update timestamptz;
alter table plantaopro.propostas_comerciais add column if not exists tenant_id uuid null;
alter table plantaopro.propostas_comerciais add column if not exists cliente_id uuid null;
create index if not exists ix_propostas_comerciais_tenant_id on plantaopro.propostas_comerciais (tenant_id);
create index if not exists ix_propostas_comerciais_cliente_id on plantaopro.propostas_comerciais (cliente_id);
create index if not exists ix_propostas_comerciais_status on plantaopro.propostas_comerciais (status);
create index if not exists ix_propostas_comerciais_reg_date on plantaopro.propostas_comerciais (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_propostas_comerciais_reg_status') then
        alter table plantaopro.propostas_comerciais add constraint ck_propostas_comerciais_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.proposta_comercial_itens (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.proposta_comercial_itens add column if not exists codigo varchar(120);
alter table plantaopro.proposta_comercial_itens add column if not exists nome varchar(220);
alter table plantaopro.proposta_comercial_itens add column if not exists descricao text;
alter table plantaopro.proposta_comercial_itens add column if not exists status varchar(40);
alter table plantaopro.proposta_comercial_itens add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.proposta_comercial_itens add column if not exists is_demo boolean default false;
alter table plantaopro.proposta_comercial_itens add column if not exists reg_status char(1) default 'A';
alter table plantaopro.proposta_comercial_itens add column if not exists reg_date timestamptz default now();
alter table plantaopro.proposta_comercial_itens add column if not exists reg_update timestamptz;
alter table plantaopro.proposta_comercial_itens add column if not exists tenant_id uuid null;
alter table plantaopro.proposta_comercial_itens add column if not exists cliente_id uuid null;
create index if not exists ix_proposta_comercial_itens_tenant_id on plantaopro.proposta_comercial_itens (tenant_id);
create index if not exists ix_proposta_comercial_itens_cliente_id on plantaopro.proposta_comercial_itens (cliente_id);
create index if not exists ix_proposta_comercial_itens_status on plantaopro.proposta_comercial_itens (status);
create index if not exists ix_proposta_comercial_itens_reg_date on plantaopro.proposta_comercial_itens (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_proposta_comercial_itens_reg_status') then
        alter table plantaopro.proposta_comercial_itens add constraint ck_proposta_comercial_itens_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.proposta_comercial_planos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.proposta_comercial_planos add column if not exists codigo varchar(120);
alter table plantaopro.proposta_comercial_planos add column if not exists nome varchar(220);
alter table plantaopro.proposta_comercial_planos add column if not exists descricao text;
alter table plantaopro.proposta_comercial_planos add column if not exists status varchar(40);
alter table plantaopro.proposta_comercial_planos add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.proposta_comercial_planos add column if not exists is_demo boolean default false;
alter table plantaopro.proposta_comercial_planos add column if not exists reg_status char(1) default 'A';
alter table plantaopro.proposta_comercial_planos add column if not exists reg_date timestamptz default now();
alter table plantaopro.proposta_comercial_planos add column if not exists reg_update timestamptz;
alter table plantaopro.proposta_comercial_planos add column if not exists tenant_id uuid null;
alter table plantaopro.proposta_comercial_planos add column if not exists cliente_id uuid null;
create index if not exists ix_proposta_comercial_planos_tenant_id on plantaopro.proposta_comercial_planos (tenant_id);
create index if not exists ix_proposta_comercial_planos_cliente_id on plantaopro.proposta_comercial_planos (cliente_id);
create index if not exists ix_proposta_comercial_planos_status on plantaopro.proposta_comercial_planos (status);
create index if not exists ix_proposta_comercial_planos_reg_date on plantaopro.proposta_comercial_planos (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_proposta_comercial_planos_reg_status') then
        alter table plantaopro.proposta_comercial_planos add constraint ck_proposta_comercial_planos_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.proposta_comercial_modulos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.proposta_comercial_modulos add column if not exists codigo varchar(120);
alter table plantaopro.proposta_comercial_modulos add column if not exists nome varchar(220);
alter table plantaopro.proposta_comercial_modulos add column if not exists descricao text;
alter table plantaopro.proposta_comercial_modulos add column if not exists status varchar(40);
alter table plantaopro.proposta_comercial_modulos add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.proposta_comercial_modulos add column if not exists is_demo boolean default false;
alter table plantaopro.proposta_comercial_modulos add column if not exists reg_status char(1) default 'A';
alter table plantaopro.proposta_comercial_modulos add column if not exists reg_date timestamptz default now();
alter table plantaopro.proposta_comercial_modulos add column if not exists reg_update timestamptz;
alter table plantaopro.proposta_comercial_modulos add column if not exists tenant_id uuid null;
alter table plantaopro.proposta_comercial_modulos add column if not exists cliente_id uuid null;
create index if not exists ix_proposta_comercial_modulos_tenant_id on plantaopro.proposta_comercial_modulos (tenant_id);
create index if not exists ix_proposta_comercial_modulos_cliente_id on plantaopro.proposta_comercial_modulos (cliente_id);
create index if not exists ix_proposta_comercial_modulos_status on plantaopro.proposta_comercial_modulos (status);
create index if not exists ix_proposta_comercial_modulos_reg_date on plantaopro.proposta_comercial_modulos (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_proposta_comercial_modulos_reg_status') then
        alter table plantaopro.proposta_comercial_modulos add constraint ck_proposta_comercial_modulos_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.proposta_comercial_condicoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.proposta_comercial_condicoes add column if not exists codigo varchar(120);
alter table plantaopro.proposta_comercial_condicoes add column if not exists nome varchar(220);
alter table plantaopro.proposta_comercial_condicoes add column if not exists descricao text;
alter table plantaopro.proposta_comercial_condicoes add column if not exists status varchar(40);
alter table plantaopro.proposta_comercial_condicoes add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.proposta_comercial_condicoes add column if not exists is_demo boolean default false;
alter table plantaopro.proposta_comercial_condicoes add column if not exists reg_status char(1) default 'A';
alter table plantaopro.proposta_comercial_condicoes add column if not exists reg_date timestamptz default now();
alter table plantaopro.proposta_comercial_condicoes add column if not exists reg_update timestamptz;
alter table plantaopro.proposta_comercial_condicoes add column if not exists tenant_id uuid null;
alter table plantaopro.proposta_comercial_condicoes add column if not exists cliente_id uuid null;
create index if not exists ix_proposta_comercial_condicoes_tenant_id on plantaopro.proposta_comercial_condicoes (tenant_id);
create index if not exists ix_proposta_comercial_condicoes_cliente_id on plantaopro.proposta_comercial_condicoes (cliente_id);
create index if not exists ix_proposta_comercial_condicoes_status on plantaopro.proposta_comercial_condicoes (status);
create index if not exists ix_proposta_comercial_condicoes_reg_date on plantaopro.proposta_comercial_condicoes (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_proposta_comercial_condicoes_reg_status') then
        alter table plantaopro.proposta_comercial_condicoes add constraint ck_proposta_comercial_condicoes_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.proposta_comercial_aceites (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.proposta_comercial_aceites add column if not exists codigo varchar(120);
alter table plantaopro.proposta_comercial_aceites add column if not exists nome varchar(220);
alter table plantaopro.proposta_comercial_aceites add column if not exists descricao text;
alter table plantaopro.proposta_comercial_aceites add column if not exists status varchar(40);
alter table plantaopro.proposta_comercial_aceites add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.proposta_comercial_aceites add column if not exists is_demo boolean default false;
alter table plantaopro.proposta_comercial_aceites add column if not exists reg_status char(1) default 'A';
alter table plantaopro.proposta_comercial_aceites add column if not exists reg_date timestamptz default now();
alter table plantaopro.proposta_comercial_aceites add column if not exists reg_update timestamptz;
alter table plantaopro.proposta_comercial_aceites add column if not exists tenant_id uuid null;
alter table plantaopro.proposta_comercial_aceites add column if not exists cliente_id uuid null;
create index if not exists ix_proposta_comercial_aceites_tenant_id on plantaopro.proposta_comercial_aceites (tenant_id);
create index if not exists ix_proposta_comercial_aceites_cliente_id on plantaopro.proposta_comercial_aceites (cliente_id);
create index if not exists ix_proposta_comercial_aceites_status on plantaopro.proposta_comercial_aceites (status);
create index if not exists ix_proposta_comercial_aceites_reg_date on plantaopro.proposta_comercial_aceites (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_proposta_comercial_aceites_reg_status') then
        alter table plantaopro.proposta_comercial_aceites add constraint ck_proposta_comercial_aceites_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.proposta_comercial_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.proposta_comercial_historico add column if not exists codigo varchar(120);
alter table plantaopro.proposta_comercial_historico add column if not exists nome varchar(220);
alter table plantaopro.proposta_comercial_historico add column if not exists descricao text;
alter table plantaopro.proposta_comercial_historico add column if not exists status varchar(40);
alter table plantaopro.proposta_comercial_historico add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.proposta_comercial_historico add column if not exists is_demo boolean default false;
alter table plantaopro.proposta_comercial_historico add column if not exists reg_status char(1) default 'A';
alter table plantaopro.proposta_comercial_historico add column if not exists reg_date timestamptz default now();
alter table plantaopro.proposta_comercial_historico add column if not exists reg_update timestamptz;
alter table plantaopro.proposta_comercial_historico add column if not exists tenant_id uuid null;
alter table plantaopro.proposta_comercial_historico add column if not exists cliente_id uuid null;
create index if not exists ix_proposta_comercial_historico_tenant_id on plantaopro.proposta_comercial_historico (tenant_id);
create index if not exists ix_proposta_comercial_historico_cliente_id on plantaopro.proposta_comercial_historico (cliente_id);
create index if not exists ix_proposta_comercial_historico_status on plantaopro.proposta_comercial_historico (status);
create index if not exists ix_proposta_comercial_historico_reg_date on plantaopro.proposta_comercial_historico (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_proposta_comercial_historico_reg_status') then
        alter table plantaopro.proposta_comercial_historico add constraint ck_proposta_comercial_historico_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.demo_ambientes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.demo_ambientes add column if not exists codigo varchar(120);
alter table plantaopro.demo_ambientes add column if not exists nome varchar(220);
alter table plantaopro.demo_ambientes add column if not exists descricao text;
alter table plantaopro.demo_ambientes add column if not exists status varchar(40);
alter table plantaopro.demo_ambientes add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.demo_ambientes add column if not exists is_demo boolean default false;
alter table plantaopro.demo_ambientes add column if not exists reg_status char(1) default 'A';
alter table plantaopro.demo_ambientes add column if not exists reg_date timestamptz default now();
alter table plantaopro.demo_ambientes add column if not exists reg_update timestamptz;
alter table plantaopro.demo_ambientes add column if not exists tenant_id uuid null;
alter table plantaopro.demo_ambientes add column if not exists cliente_id uuid null;
create index if not exists ix_demo_ambientes_tenant_id on plantaopro.demo_ambientes (tenant_id);
create index if not exists ix_demo_ambientes_cliente_id on plantaopro.demo_ambientes (cliente_id);
create index if not exists ix_demo_ambientes_status on plantaopro.demo_ambientes (status);
create index if not exists ix_demo_ambientes_reg_date on plantaopro.demo_ambientes (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_demo_ambientes_reg_status') then
        alter table plantaopro.demo_ambientes add constraint ck_demo_ambientes_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.demo_usuarios (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.demo_usuarios add column if not exists codigo varchar(120);
alter table plantaopro.demo_usuarios add column if not exists nome varchar(220);
alter table plantaopro.demo_usuarios add column if not exists descricao text;
alter table plantaopro.demo_usuarios add column if not exists status varchar(40);
alter table plantaopro.demo_usuarios add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.demo_usuarios add column if not exists is_demo boolean default false;
alter table plantaopro.demo_usuarios add column if not exists reg_status char(1) default 'A';
alter table plantaopro.demo_usuarios add column if not exists reg_date timestamptz default now();
alter table plantaopro.demo_usuarios add column if not exists reg_update timestamptz;
alter table plantaopro.demo_usuarios add column if not exists tenant_id uuid null;
alter table plantaopro.demo_usuarios add column if not exists cliente_id uuid null;
create index if not exists ix_demo_usuarios_tenant_id on plantaopro.demo_usuarios (tenant_id);
create index if not exists ix_demo_usuarios_cliente_id on plantaopro.demo_usuarios (cliente_id);
create index if not exists ix_demo_usuarios_status on plantaopro.demo_usuarios (status);
create index if not exists ix_demo_usuarios_reg_date on plantaopro.demo_usuarios (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_demo_usuarios_reg_status') then
        alter table plantaopro.demo_usuarios add constraint ck_demo_usuarios_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.demo_execucoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.demo_execucoes add column if not exists codigo varchar(120);
alter table plantaopro.demo_execucoes add column if not exists nome varchar(220);
alter table plantaopro.demo_execucoes add column if not exists descricao text;
alter table plantaopro.demo_execucoes add column if not exists status varchar(40);
alter table plantaopro.demo_execucoes add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.demo_execucoes add column if not exists is_demo boolean default false;
alter table plantaopro.demo_execucoes add column if not exists reg_status char(1) default 'A';
alter table plantaopro.demo_execucoes add column if not exists reg_date timestamptz default now();
alter table plantaopro.demo_execucoes add column if not exists reg_update timestamptz;
alter table plantaopro.demo_execucoes add column if not exists tenant_id uuid null;
alter table plantaopro.demo_execucoes add column if not exists cliente_id uuid null;
create index if not exists ix_demo_execucoes_tenant_id on plantaopro.demo_execucoes (tenant_id);
create index if not exists ix_demo_execucoes_cliente_id on plantaopro.demo_execucoes (cliente_id);
create index if not exists ix_demo_execucoes_status on plantaopro.demo_execucoes (status);
create index if not exists ix_demo_execucoes_reg_date on plantaopro.demo_execucoes (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_demo_execucoes_reg_status') then
        alter table plantaopro.demo_execucoes add constraint ck_demo_execucoes_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.demo_feedbacks (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.demo_feedbacks add column if not exists codigo varchar(120);
alter table plantaopro.demo_feedbacks add column if not exists nome varchar(220);
alter table plantaopro.demo_feedbacks add column if not exists descricao text;
alter table plantaopro.demo_feedbacks add column if not exists status varchar(40);
alter table plantaopro.demo_feedbacks add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.demo_feedbacks add column if not exists is_demo boolean default false;
alter table plantaopro.demo_feedbacks add column if not exists reg_status char(1) default 'A';
alter table plantaopro.demo_feedbacks add column if not exists reg_date timestamptz default now();
alter table plantaopro.demo_feedbacks add column if not exists reg_update timestamptz;
alter table plantaopro.demo_feedbacks add column if not exists tenant_id uuid null;
alter table plantaopro.demo_feedbacks add column if not exists cliente_id uuid null;
create index if not exists ix_demo_feedbacks_tenant_id on plantaopro.demo_feedbacks (tenant_id);
create index if not exists ix_demo_feedbacks_cliente_id on plantaopro.demo_feedbacks (cliente_id);
create index if not exists ix_demo_feedbacks_status on plantaopro.demo_feedbacks (status);
create index if not exists ix_demo_feedbacks_reg_date on plantaopro.demo_feedbacks (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_demo_feedbacks_reg_status') then
        alter table plantaopro.demo_feedbacks add constraint ck_demo_feedbacks_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.modulo_dependencias (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.modulo_dependencias add column if not exists codigo varchar(120);
alter table plantaopro.modulo_dependencias add column if not exists nome varchar(220);
alter table plantaopro.modulo_dependencias add column if not exists descricao text;
alter table plantaopro.modulo_dependencias add column if not exists status varchar(40);
alter table plantaopro.modulo_dependencias add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.modulo_dependencias add column if not exists is_demo boolean default false;
alter table plantaopro.modulo_dependencias add column if not exists reg_status char(1) default 'A';
alter table plantaopro.modulo_dependencias add column if not exists reg_date timestamptz default now();
alter table plantaopro.modulo_dependencias add column if not exists reg_update timestamptz;
alter table plantaopro.modulo_dependencias add column if not exists tenant_id uuid null;
alter table plantaopro.modulo_dependencias add column if not exists cliente_id uuid null;
create index if not exists ix_modulo_dependencias_tenant_id on plantaopro.modulo_dependencias (tenant_id);
create index if not exists ix_modulo_dependencias_cliente_id on plantaopro.modulo_dependencias (cliente_id);
create index if not exists ix_modulo_dependencias_status on plantaopro.modulo_dependencias (status);
create index if not exists ix_modulo_dependencias_reg_date on plantaopro.modulo_dependencias (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_modulo_dependencias_reg_status') then
        alter table plantaopro.modulo_dependencias add constraint ck_modulo_dependencias_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.modulo_planos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.modulo_planos add column if not exists codigo varchar(120);
alter table plantaopro.modulo_planos add column if not exists nome varchar(220);
alter table plantaopro.modulo_planos add column if not exists descricao text;
alter table plantaopro.modulo_planos add column if not exists status varchar(40);
alter table plantaopro.modulo_planos add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.modulo_planos add column if not exists is_demo boolean default false;
alter table plantaopro.modulo_planos add column if not exists reg_status char(1) default 'A';
alter table plantaopro.modulo_planos add column if not exists reg_date timestamptz default now();
alter table plantaopro.modulo_planos add column if not exists reg_update timestamptz;
alter table plantaopro.modulo_planos add column if not exists tenant_id uuid null;
alter table plantaopro.modulo_planos add column if not exists cliente_id uuid null;
create index if not exists ix_modulo_planos_tenant_id on plantaopro.modulo_planos (tenant_id);
create index if not exists ix_modulo_planos_cliente_id on plantaopro.modulo_planos (cliente_id);
create index if not exists ix_modulo_planos_status on plantaopro.modulo_planos (status);
create index if not exists ix_modulo_planos_reg_date on plantaopro.modulo_planos (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_modulo_planos_reg_status') then
        alter table plantaopro.modulo_planos add constraint ck_modulo_planos_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.modulo_flags (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.modulo_flags add column if not exists codigo varchar(120);
alter table plantaopro.modulo_flags add column if not exists nome varchar(220);
alter table plantaopro.modulo_flags add column if not exists descricao text;
alter table plantaopro.modulo_flags add column if not exists status varchar(40);
alter table plantaopro.modulo_flags add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.modulo_flags add column if not exists is_demo boolean default false;
alter table plantaopro.modulo_flags add column if not exists reg_status char(1) default 'A';
alter table plantaopro.modulo_flags add column if not exists reg_date timestamptz default now();
alter table plantaopro.modulo_flags add column if not exists reg_update timestamptz;
alter table plantaopro.modulo_flags add column if not exists tenant_id uuid null;
alter table plantaopro.modulo_flags add column if not exists cliente_id uuid null;
create index if not exists ix_modulo_flags_tenant_id on plantaopro.modulo_flags (tenant_id);
create index if not exists ix_modulo_flags_cliente_id on plantaopro.modulo_flags (cliente_id);
create index if not exists ix_modulo_flags_status on plantaopro.modulo_flags (status);
create index if not exists ix_modulo_flags_reg_date on plantaopro.modulo_flags (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_modulo_flags_reg_status') then
        alter table plantaopro.modulo_flags add constraint ck_modulo_flags_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.modulo_habilitacoes_tenant (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.modulo_habilitacoes_tenant add column if not exists codigo varchar(120);
alter table plantaopro.modulo_habilitacoes_tenant add column if not exists nome varchar(220);
alter table plantaopro.modulo_habilitacoes_tenant add column if not exists descricao text;
alter table plantaopro.modulo_habilitacoes_tenant add column if not exists status varchar(40);
alter table plantaopro.modulo_habilitacoes_tenant add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.modulo_habilitacoes_tenant add column if not exists is_demo boolean default false;
alter table plantaopro.modulo_habilitacoes_tenant add column if not exists reg_status char(1) default 'A';
alter table plantaopro.modulo_habilitacoes_tenant add column if not exists reg_date timestamptz default now();
alter table plantaopro.modulo_habilitacoes_tenant add column if not exists reg_update timestamptz;
alter table plantaopro.modulo_habilitacoes_tenant add column if not exists tenant_id uuid null;
alter table plantaopro.modulo_habilitacoes_tenant add column if not exists cliente_id uuid null;
create index if not exists ix_modulo_habilitacoes_tenant_tenant_id on plantaopro.modulo_habilitacoes_tenant (tenant_id);
create index if not exists ix_modulo_habilitacoes_tenant_cliente_id on plantaopro.modulo_habilitacoes_tenant (cliente_id);
create index if not exists ix_modulo_habilitacoes_tenant_status on plantaopro.modulo_habilitacoes_tenant (status);
create index if not exists ix_modulo_habilitacoes_tenant_reg_date on plantaopro.modulo_habilitacoes_tenant (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_modulo_habilitacoes_tenant_reg_status') then
        alter table plantaopro.modulo_habilitacoes_tenant add constraint ck_modulo_habilitacoes_tenant_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.modulo_historico_alteracoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.modulo_historico_alteracoes add column if not exists codigo varchar(120);
alter table plantaopro.modulo_historico_alteracoes add column if not exists nome varchar(220);
alter table plantaopro.modulo_historico_alteracoes add column if not exists descricao text;
alter table plantaopro.modulo_historico_alteracoes add column if not exists status varchar(40);
alter table plantaopro.modulo_historico_alteracoes add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.modulo_historico_alteracoes add column if not exists is_demo boolean default false;
alter table plantaopro.modulo_historico_alteracoes add column if not exists reg_status char(1) default 'A';
alter table plantaopro.modulo_historico_alteracoes add column if not exists reg_date timestamptz default now();
alter table plantaopro.modulo_historico_alteracoes add column if not exists reg_update timestamptz;
alter table plantaopro.modulo_historico_alteracoes add column if not exists tenant_id uuid null;
alter table plantaopro.modulo_historico_alteracoes add column if not exists cliente_id uuid null;
create index if not exists ix_modulo_historico_alteracoes_tenant_id on plantaopro.modulo_historico_alteracoes (tenant_id);
create index if not exists ix_modulo_historico_alteracoes_cliente_id on plantaopro.modulo_historico_alteracoes (cliente_id);
create index if not exists ix_modulo_historico_alteracoes_status on plantaopro.modulo_historico_alteracoes (status);
create index if not exists ix_modulo_historico_alteracoes_reg_date on plantaopro.modulo_historico_alteracoes (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_modulo_historico_alteracoes_reg_status') then
        alter table plantaopro.modulo_historico_alteracoes add constraint ck_modulo_historico_alteracoes_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.onboarding_templates (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.onboarding_templates add column if not exists codigo varchar(120);
alter table plantaopro.onboarding_templates add column if not exists nome varchar(220);
alter table plantaopro.onboarding_templates add column if not exists descricao text;
alter table plantaopro.onboarding_templates add column if not exists status varchar(40);
alter table plantaopro.onboarding_templates add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.onboarding_templates add column if not exists is_demo boolean default false;
alter table plantaopro.onboarding_templates add column if not exists reg_status char(1) default 'A';
alter table plantaopro.onboarding_templates add column if not exists reg_date timestamptz default now();
alter table plantaopro.onboarding_templates add column if not exists reg_update timestamptz;
alter table plantaopro.onboarding_templates add column if not exists tenant_id uuid null;
alter table plantaopro.onboarding_templates add column if not exists cliente_id uuid null;
create index if not exists ix_onboarding_templates_tenant_id on plantaopro.onboarding_templates (tenant_id);
create index if not exists ix_onboarding_templates_cliente_id on plantaopro.onboarding_templates (cliente_id);
create index if not exists ix_onboarding_templates_status on plantaopro.onboarding_templates (status);
create index if not exists ix_onboarding_templates_reg_date on plantaopro.onboarding_templates (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_onboarding_templates_reg_status') then
        alter table plantaopro.onboarding_templates add constraint ck_onboarding_templates_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.onboarding_template_etapas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.onboarding_template_etapas add column if not exists codigo varchar(120);
alter table plantaopro.onboarding_template_etapas add column if not exists nome varchar(220);
alter table plantaopro.onboarding_template_etapas add column if not exists descricao text;
alter table plantaopro.onboarding_template_etapas add column if not exists status varchar(40);
alter table plantaopro.onboarding_template_etapas add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.onboarding_template_etapas add column if not exists is_demo boolean default false;
alter table plantaopro.onboarding_template_etapas add column if not exists reg_status char(1) default 'A';
alter table plantaopro.onboarding_template_etapas add column if not exists reg_date timestamptz default now();
alter table plantaopro.onboarding_template_etapas add column if not exists reg_update timestamptz;
alter table plantaopro.onboarding_template_etapas add column if not exists tenant_id uuid null;
alter table plantaopro.onboarding_template_etapas add column if not exists cliente_id uuid null;
create index if not exists ix_onboarding_template_etapas_tenant_id on plantaopro.onboarding_template_etapas (tenant_id);
create index if not exists ix_onboarding_template_etapas_cliente_id on plantaopro.onboarding_template_etapas (cliente_id);
create index if not exists ix_onboarding_template_etapas_status on plantaopro.onboarding_template_etapas (status);
create index if not exists ix_onboarding_template_etapas_reg_date on plantaopro.onboarding_template_etapas (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_onboarding_template_etapas_reg_status') then
        alter table plantaopro.onboarding_template_etapas add constraint ck_onboarding_template_etapas_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.onboarding_template_tarefas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.onboarding_template_tarefas add column if not exists codigo varchar(120);
alter table plantaopro.onboarding_template_tarefas add column if not exists nome varchar(220);
alter table plantaopro.onboarding_template_tarefas add column if not exists descricao text;
alter table plantaopro.onboarding_template_tarefas add column if not exists status varchar(40);
alter table plantaopro.onboarding_template_tarefas add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.onboarding_template_tarefas add column if not exists is_demo boolean default false;
alter table plantaopro.onboarding_template_tarefas add column if not exists reg_status char(1) default 'A';
alter table plantaopro.onboarding_template_tarefas add column if not exists reg_date timestamptz default now();
alter table plantaopro.onboarding_template_tarefas add column if not exists reg_update timestamptz;
alter table plantaopro.onboarding_template_tarefas add column if not exists tenant_id uuid null;
alter table plantaopro.onboarding_template_tarefas add column if not exists cliente_id uuid null;
create index if not exists ix_onboarding_template_tarefas_tenant_id on plantaopro.onboarding_template_tarefas (tenant_id);
create index if not exists ix_onboarding_template_tarefas_cliente_id on plantaopro.onboarding_template_tarefas (cliente_id);
create index if not exists ix_onboarding_template_tarefas_status on plantaopro.onboarding_template_tarefas (status);
create index if not exists ix_onboarding_template_tarefas_reg_date on plantaopro.onboarding_template_tarefas (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_onboarding_template_tarefas_reg_status') then
        alter table plantaopro.onboarding_template_tarefas add constraint ck_onboarding_template_tarefas_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.onboarding_template_perfis (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.onboarding_template_perfis add column if not exists codigo varchar(120);
alter table plantaopro.onboarding_template_perfis add column if not exists nome varchar(220);
alter table plantaopro.onboarding_template_perfis add column if not exists descricao text;
alter table plantaopro.onboarding_template_perfis add column if not exists status varchar(40);
alter table plantaopro.onboarding_template_perfis add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.onboarding_template_perfis add column if not exists is_demo boolean default false;
alter table plantaopro.onboarding_template_perfis add column if not exists reg_status char(1) default 'A';
alter table plantaopro.onboarding_template_perfis add column if not exists reg_date timestamptz default now();
alter table plantaopro.onboarding_template_perfis add column if not exists reg_update timestamptz;
alter table plantaopro.onboarding_template_perfis add column if not exists tenant_id uuid null;
alter table plantaopro.onboarding_template_perfis add column if not exists cliente_id uuid null;
create index if not exists ix_onboarding_template_perfis_tenant_id on plantaopro.onboarding_template_perfis (tenant_id);
create index if not exists ix_onboarding_template_perfis_cliente_id on plantaopro.onboarding_template_perfis (cliente_id);
create index if not exists ix_onboarding_template_perfis_status on plantaopro.onboarding_template_perfis (status);
create index if not exists ix_onboarding_template_perfis_reg_date on plantaopro.onboarding_template_perfis (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_onboarding_template_perfis_reg_status') then
        alter table plantaopro.onboarding_template_perfis add constraint ck_onboarding_template_perfis_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.onboarding_template_aplicacoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.onboarding_template_aplicacoes add column if not exists codigo varchar(120);
alter table plantaopro.onboarding_template_aplicacoes add column if not exists nome varchar(220);
alter table plantaopro.onboarding_template_aplicacoes add column if not exists descricao text;
alter table plantaopro.onboarding_template_aplicacoes add column if not exists status varchar(40);
alter table plantaopro.onboarding_template_aplicacoes add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.onboarding_template_aplicacoes add column if not exists is_demo boolean default false;
alter table plantaopro.onboarding_template_aplicacoes add column if not exists reg_status char(1) default 'A';
alter table plantaopro.onboarding_template_aplicacoes add column if not exists reg_date timestamptz default now();
alter table plantaopro.onboarding_template_aplicacoes add column if not exists reg_update timestamptz;
alter table plantaopro.onboarding_template_aplicacoes add column if not exists tenant_id uuid null;
alter table plantaopro.onboarding_template_aplicacoes add column if not exists cliente_id uuid null;
create index if not exists ix_onboarding_template_aplicacoes_tenant_id on plantaopro.onboarding_template_aplicacoes (tenant_id);
create index if not exists ix_onboarding_template_aplicacoes_cliente_id on plantaopro.onboarding_template_aplicacoes (cliente_id);
create index if not exists ix_onboarding_template_aplicacoes_status on plantaopro.onboarding_template_aplicacoes (status);
create index if not exists ix_onboarding_template_aplicacoes_reg_date on plantaopro.onboarding_template_aplicacoes (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_onboarding_template_aplicacoes_reg_status') then
        alter table plantaopro.onboarding_template_aplicacoes add constraint ck_onboarding_template_aplicacoes_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.comercial_followups (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.comercial_followups add column if not exists codigo varchar(120);
alter table plantaopro.comercial_followups add column if not exists nome varchar(220);
alter table plantaopro.comercial_followups add column if not exists descricao text;
alter table plantaopro.comercial_followups add column if not exists status varchar(40);
alter table plantaopro.comercial_followups add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.comercial_followups add column if not exists is_demo boolean default false;
alter table plantaopro.comercial_followups add column if not exists reg_status char(1) default 'A';
alter table plantaopro.comercial_followups add column if not exists reg_date timestamptz default now();
alter table plantaopro.comercial_followups add column if not exists reg_update timestamptz;
alter table plantaopro.comercial_followups add column if not exists tenant_id uuid null;
alter table plantaopro.comercial_followups add column if not exists cliente_id uuid null;
create index if not exists ix_comercial_followups_tenant_id on plantaopro.comercial_followups (tenant_id);
create index if not exists ix_comercial_followups_cliente_id on plantaopro.comercial_followups (cliente_id);
create index if not exists ix_comercial_followups_status on plantaopro.comercial_followups (status);
create index if not exists ix_comercial_followups_reg_date on plantaopro.comercial_followups (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_comercial_followups_reg_status') then
        alter table plantaopro.comercial_followups add constraint ck_comercial_followups_reg_status check (reg_status in ('A','I'));
    end if;
end $$;

create table if not exists plantaopro.comercial_resultados (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo varchar(120) null,
    nome varchar(220) not null default '',
    descricao text not null default '',
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    is_demo boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);
alter table plantaopro.comercial_resultados add column if not exists codigo varchar(120);
alter table plantaopro.comercial_resultados add column if not exists nome varchar(220);
alter table plantaopro.comercial_resultados add column if not exists descricao text;
alter table plantaopro.comercial_resultados add column if not exists status varchar(40);
alter table plantaopro.comercial_resultados add column if not exists metadata jsonb default '{}'::jsonb;
alter table plantaopro.comercial_resultados add column if not exists is_demo boolean default false;
alter table plantaopro.comercial_resultados add column if not exists reg_status char(1) default 'A';
alter table plantaopro.comercial_resultados add column if not exists reg_date timestamptz default now();
alter table plantaopro.comercial_resultados add column if not exists reg_update timestamptz;
alter table plantaopro.comercial_resultados add column if not exists tenant_id uuid null;
alter table plantaopro.comercial_resultados add column if not exists cliente_id uuid null;
create index if not exists ix_comercial_resultados_tenant_id on plantaopro.comercial_resultados (tenant_id);
create index if not exists ix_comercial_resultados_cliente_id on plantaopro.comercial_resultados (cliente_id);
create index if not exists ix_comercial_resultados_status on plantaopro.comercial_resultados (status);
create index if not exists ix_comercial_resultados_reg_date on plantaopro.comercial_resultados (reg_date);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'ck_comercial_resultados_reg_status') then
        alter table plantaopro.comercial_resultados add constraint ck_comercial_resultados_reg_status check (reg_status in ('A','I'));
    end if;
end $$;


-- Colunas específicas para propostas, simulador, leads e governança.
alter table plantaopro.public_landing_leads add column if not exists email varchar(220) null;
alter table plantaopro.public_landing_leads add column if not exists telefone varchar(60) null;
alter table plantaopro.public_landing_leads add column if not exists empresa varchar(220) null;
alter table plantaopro.public_landing_leads add column if not exists origem varchar(80) null;
create index if not exists ix_public_landing_leads_email on plantaopro.public_landing_leads (email);
create index if not exists ix_public_landing_leads_origem on plantaopro.public_landing_leads (origem);

alter table plantaopro.propostas_comerciais add column if not exists lead_id uuid null;
alter table plantaopro.propostas_comerciais add column if not exists plano_id uuid null;
alter table plantaopro.propostas_comerciais add column if not exists valor_setup numeric(14,2) not null default 0;
alter table plantaopro.propostas_comerciais add column if not exists valor_mensal numeric(14,2) not null default 0;
alter table plantaopro.propostas_comerciais add column if not exists desconto_percentual numeric(7,2) not null default 0;
alter table plantaopro.propostas_comerciais add column if not exists validade date null;
alter table plantaopro.propostas_comerciais add column if not exists sla text not null default '';
create index if not exists ix_propostas_comerciais_plano_id on plantaopro.propostas_comerciais (plano_id);
create index if not exists ix_propostas_comerciais_validade on plantaopro.propostas_comerciais (validade);

alter table plantaopro.modulo_habilitacoes_tenant add column if not exists modulo_id uuid null;
alter table plantaopro.modulo_habilitacoes_tenant add column if not exists habilitado boolean not null default true;
create index if not exists ix_modulo_habilitacoes_tenant_modulo_id on plantaopro.modulo_habilitacoes_tenant (modulo_id);

alter table plantaopro.modulo_flags add column if not exists modulo_id uuid null;
alter table plantaopro.modulo_flags add column if not exists chave varchar(160) null;
alter table plantaopro.modulo_flags add column if not exists habilitada boolean not null default false;
create index if not exists ix_modulo_flags_modulo_id on plantaopro.modulo_flags (modulo_id);
create index if not exists ix_modulo_flags_chave on plantaopro.modulo_flags (chave);
