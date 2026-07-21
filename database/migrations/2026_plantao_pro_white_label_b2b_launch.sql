-- PlantãoPro White Label B2B Launch - migração incremental idempotente
create schema if not exists plantaopro;
create extension if not exists pgcrypto;

create table if not exists plantaopro.tenants (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A',
    slug varchar(160) null,
    nome varchar(200) null,
    dominio_customizado varchar(255) null,
    subdominio varchar(160) null
);
alter table plantaopro.tenants add column if not exists tenant_id uuid null;
alter table plantaopro.tenants add column if not exists cliente_id uuid null;
alter table plantaopro.tenants add column if not exists parceiro_id uuid null;
alter table plantaopro.tenants add column if not exists plano_id uuid null;
alter table plantaopro.tenants add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.tenants add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.tenants add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.tenants add column if not exists reg_update timestamptz null;
alter table plantaopro.tenants add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_tenants_tenant_id on plantaopro.tenants(tenant_id);
create index if not exists ix_tenants_cliente_id on plantaopro.tenants(cliente_id);
create index if not exists ix_tenants_status on plantaopro.tenants(status);
create index if not exists ix_tenants_reg_date on plantaopro.tenants(reg_date desc);

create table if not exists plantaopro.tenant_configuracoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.tenant_configuracoes add column if not exists tenant_id uuid null;
alter table plantaopro.tenant_configuracoes add column if not exists cliente_id uuid null;
alter table plantaopro.tenant_configuracoes add column if not exists parceiro_id uuid null;
alter table plantaopro.tenant_configuracoes add column if not exists plano_id uuid null;
alter table plantaopro.tenant_configuracoes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.tenant_configuracoes add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.tenant_configuracoes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.tenant_configuracoes add column if not exists reg_update timestamptz null;
alter table plantaopro.tenant_configuracoes add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_tenant_configuracoes_tenant_id on plantaopro.tenant_configuracoes(tenant_id);
create index if not exists ix_tenant_configuracoes_cliente_id on plantaopro.tenant_configuracoes(cliente_id);
create index if not exists ix_tenant_configuracoes_status on plantaopro.tenant_configuracoes(status);
create index if not exists ix_tenant_configuracoes_reg_date on plantaopro.tenant_configuracoes(reg_date desc);

create table if not exists plantaopro.tenant_modulos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.tenant_modulos add column if not exists tenant_id uuid null;
alter table plantaopro.tenant_modulos add column if not exists cliente_id uuid null;
alter table plantaopro.tenant_modulos add column if not exists parceiro_id uuid null;
alter table plantaopro.tenant_modulos add column if not exists plano_id uuid null;
alter table plantaopro.tenant_modulos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.tenant_modulos add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.tenant_modulos add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.tenant_modulos add column if not exists reg_update timestamptz null;
alter table plantaopro.tenant_modulos add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_tenant_modulos_tenant_id on plantaopro.tenant_modulos(tenant_id);
create index if not exists ix_tenant_modulos_cliente_id on plantaopro.tenant_modulos(cliente_id);
create index if not exists ix_tenant_modulos_status on plantaopro.tenant_modulos(status);
create index if not exists ix_tenant_modulos_reg_date on plantaopro.tenant_modulos(reg_date desc);

create table if not exists plantaopro.tenant_parametros (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.tenant_parametros add column if not exists tenant_id uuid null;
alter table plantaopro.tenant_parametros add column if not exists cliente_id uuid null;
alter table plantaopro.tenant_parametros add column if not exists parceiro_id uuid null;
alter table plantaopro.tenant_parametros add column if not exists plano_id uuid null;
alter table plantaopro.tenant_parametros add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.tenant_parametros add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.tenant_parametros add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.tenant_parametros add column if not exists reg_update timestamptz null;
alter table plantaopro.tenant_parametros add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_tenant_parametros_tenant_id on plantaopro.tenant_parametros(tenant_id);
create index if not exists ix_tenant_parametros_cliente_id on plantaopro.tenant_parametros(cliente_id);
create index if not exists ix_tenant_parametros_status on plantaopro.tenant_parametros(status);
create index if not exists ix_tenant_parametros_reg_date on plantaopro.tenant_parametros(reg_date desc);

create table if not exists plantaopro.tenant_dominios (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.tenant_dominios add column if not exists tenant_id uuid null;
alter table plantaopro.tenant_dominios add column if not exists cliente_id uuid null;
alter table plantaopro.tenant_dominios add column if not exists parceiro_id uuid null;
alter table plantaopro.tenant_dominios add column if not exists plano_id uuid null;
alter table plantaopro.tenant_dominios add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.tenant_dominios add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.tenant_dominios add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.tenant_dominios add column if not exists reg_update timestamptz null;
alter table plantaopro.tenant_dominios add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_tenant_dominios_tenant_id on plantaopro.tenant_dominios(tenant_id);
create index if not exists ix_tenant_dominios_cliente_id on plantaopro.tenant_dominios(cliente_id);
create index if not exists ix_tenant_dominios_status on plantaopro.tenant_dominios(status);
create index if not exists ix_tenant_dominios_reg_date on plantaopro.tenant_dominios(reg_date desc);

create table if not exists plantaopro.tenant_onboarding (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.tenant_onboarding add column if not exists tenant_id uuid null;
alter table plantaopro.tenant_onboarding add column if not exists cliente_id uuid null;
alter table plantaopro.tenant_onboarding add column if not exists parceiro_id uuid null;
alter table plantaopro.tenant_onboarding add column if not exists plano_id uuid null;
alter table plantaopro.tenant_onboarding add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.tenant_onboarding add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.tenant_onboarding add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.tenant_onboarding add column if not exists reg_update timestamptz null;
alter table plantaopro.tenant_onboarding add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_tenant_onboarding_tenant_id on plantaopro.tenant_onboarding(tenant_id);
create index if not exists ix_tenant_onboarding_cliente_id on plantaopro.tenant_onboarding(cliente_id);
create index if not exists ix_tenant_onboarding_status on plantaopro.tenant_onboarding(status);
create index if not exists ix_tenant_onboarding_reg_date on plantaopro.tenant_onboarding(reg_date desc);

create table if not exists plantaopro.tenant_onboarding_checklist (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.tenant_onboarding_checklist add column if not exists tenant_id uuid null;
alter table plantaopro.tenant_onboarding_checklist add column if not exists cliente_id uuid null;
alter table plantaopro.tenant_onboarding_checklist add column if not exists parceiro_id uuid null;
alter table plantaopro.tenant_onboarding_checklist add column if not exists plano_id uuid null;
alter table plantaopro.tenant_onboarding_checklist add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.tenant_onboarding_checklist add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.tenant_onboarding_checklist add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.tenant_onboarding_checklist add column if not exists reg_update timestamptz null;
alter table plantaopro.tenant_onboarding_checklist add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_tenant_onboarding_checklist_tenant_id on plantaopro.tenant_onboarding_checklist(tenant_id);
create index if not exists ix_tenant_onboarding_checklist_cliente_id on plantaopro.tenant_onboarding_checklist(cliente_id);
create index if not exists ix_tenant_onboarding_checklist_status on plantaopro.tenant_onboarding_checklist(status);
create index if not exists ix_tenant_onboarding_checklist_reg_date on plantaopro.tenant_onboarding_checklist(reg_date desc);

create table if not exists plantaopro.tenant_auditoria_configuracoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.tenant_auditoria_configuracoes add column if not exists tenant_id uuid null;
alter table plantaopro.tenant_auditoria_configuracoes add column if not exists cliente_id uuid null;
alter table plantaopro.tenant_auditoria_configuracoes add column if not exists parceiro_id uuid null;
alter table plantaopro.tenant_auditoria_configuracoes add column if not exists plano_id uuid null;
alter table plantaopro.tenant_auditoria_configuracoes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.tenant_auditoria_configuracoes add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.tenant_auditoria_configuracoes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.tenant_auditoria_configuracoes add column if not exists reg_update timestamptz null;
alter table plantaopro.tenant_auditoria_configuracoes add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_tenant_auditoria_configuracoes_tenant_id on plantaopro.tenant_auditoria_configuracoes(tenant_id);
create index if not exists ix_tenant_auditoria_configuracoes_cliente_id on plantaopro.tenant_auditoria_configuracoes(cliente_id);
create index if not exists ix_tenant_auditoria_configuracoes_status on plantaopro.tenant_auditoria_configuracoes(status);
create index if not exists ix_tenant_auditoria_configuracoes_reg_date on plantaopro.tenant_auditoria_configuracoes(reg_date desc);

create table if not exists plantaopro.tenant_ambientes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.tenant_ambientes add column if not exists tenant_id uuid null;
alter table plantaopro.tenant_ambientes add column if not exists cliente_id uuid null;
alter table plantaopro.tenant_ambientes add column if not exists parceiro_id uuid null;
alter table plantaopro.tenant_ambientes add column if not exists plano_id uuid null;
alter table plantaopro.tenant_ambientes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.tenant_ambientes add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.tenant_ambientes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.tenant_ambientes add column if not exists reg_update timestamptz null;
alter table plantaopro.tenant_ambientes add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_tenant_ambientes_tenant_id on plantaopro.tenant_ambientes(tenant_id);
create index if not exists ix_tenant_ambientes_cliente_id on plantaopro.tenant_ambientes(cliente_id);
create index if not exists ix_tenant_ambientes_status on plantaopro.tenant_ambientes(status);
create index if not exists ix_tenant_ambientes_reg_date on plantaopro.tenant_ambientes(reg_date desc);

create table if not exists plantaopro.tenant_status_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.tenant_status_historico add column if not exists tenant_id uuid null;
alter table plantaopro.tenant_status_historico add column if not exists cliente_id uuid null;
alter table plantaopro.tenant_status_historico add column if not exists parceiro_id uuid null;
alter table plantaopro.tenant_status_historico add column if not exists plano_id uuid null;
alter table plantaopro.tenant_status_historico add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.tenant_status_historico add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.tenant_status_historico add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.tenant_status_historico add column if not exists reg_update timestamptz null;
alter table plantaopro.tenant_status_historico add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_tenant_status_historico_tenant_id on plantaopro.tenant_status_historico(tenant_id);
create index if not exists ix_tenant_status_historico_cliente_id on plantaopro.tenant_status_historico(cliente_id);
create index if not exists ix_tenant_status_historico_status on plantaopro.tenant_status_historico(status);
create index if not exists ix_tenant_status_historico_reg_date on plantaopro.tenant_status_historico(reg_date desc);

create table if not exists plantaopro.tenant_white_label (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.tenant_white_label add column if not exists tenant_id uuid null;
alter table plantaopro.tenant_white_label add column if not exists cliente_id uuid null;
alter table plantaopro.tenant_white_label add column if not exists parceiro_id uuid null;
alter table plantaopro.tenant_white_label add column if not exists plano_id uuid null;
alter table plantaopro.tenant_white_label add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.tenant_white_label add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.tenant_white_label add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.tenant_white_label add column if not exists reg_update timestamptz null;
alter table plantaopro.tenant_white_label add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_tenant_white_label_tenant_id on plantaopro.tenant_white_label(tenant_id);
create index if not exists ix_tenant_white_label_cliente_id on plantaopro.tenant_white_label(cliente_id);
create index if not exists ix_tenant_white_label_status on plantaopro.tenant_white_label(status);
create index if not exists ix_tenant_white_label_reg_date on plantaopro.tenant_white_label(reg_date desc);

create table if not exists plantaopro.white_label_temas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.white_label_temas add column if not exists tenant_id uuid null;
alter table plantaopro.white_label_temas add column if not exists cliente_id uuid null;
alter table plantaopro.white_label_temas add column if not exists parceiro_id uuid null;
alter table plantaopro.white_label_temas add column if not exists plano_id uuid null;
alter table plantaopro.white_label_temas add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.white_label_temas add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.white_label_temas add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.white_label_temas add column if not exists reg_update timestamptz null;
alter table plantaopro.white_label_temas add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_white_label_temas_tenant_id on plantaopro.white_label_temas(tenant_id);
create index if not exists ix_white_label_temas_cliente_id on plantaopro.white_label_temas(cliente_id);
create index if not exists ix_white_label_temas_status on plantaopro.white_label_temas(status);
create index if not exists ix_white_label_temas_reg_date on plantaopro.white_label_temas(reg_date desc);

create table if not exists plantaopro.white_label_assets (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.white_label_assets add column if not exists tenant_id uuid null;
alter table plantaopro.white_label_assets add column if not exists cliente_id uuid null;
alter table plantaopro.white_label_assets add column if not exists parceiro_id uuid null;
alter table plantaopro.white_label_assets add column if not exists plano_id uuid null;
alter table plantaopro.white_label_assets add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.white_label_assets add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.white_label_assets add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.white_label_assets add column if not exists reg_update timestamptz null;
alter table plantaopro.white_label_assets add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_white_label_assets_tenant_id on plantaopro.white_label_assets(tenant_id);
create index if not exists ix_white_label_assets_cliente_id on plantaopro.white_label_assets(cliente_id);
create index if not exists ix_white_label_assets_status on plantaopro.white_label_assets(status);
create index if not exists ix_white_label_assets_reg_date on plantaopro.white_label_assets(reg_date desc);

create table if not exists plantaopro.white_label_textos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.white_label_textos add column if not exists tenant_id uuid null;
alter table plantaopro.white_label_textos add column if not exists cliente_id uuid null;
alter table plantaopro.white_label_textos add column if not exists parceiro_id uuid null;
alter table plantaopro.white_label_textos add column if not exists plano_id uuid null;
alter table plantaopro.white_label_textos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.white_label_textos add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.white_label_textos add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.white_label_textos add column if not exists reg_update timestamptz null;
alter table plantaopro.white_label_textos add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_white_label_textos_tenant_id on plantaopro.white_label_textos(tenant_id);
create index if not exists ix_white_label_textos_cliente_id on plantaopro.white_label_textos(cliente_id);
create index if not exists ix_white_label_textos_status on plantaopro.white_label_textos(status);
create index if not exists ix_white_label_textos_reg_date on plantaopro.white_label_textos(reg_date desc);

create table if not exists plantaopro.white_label_emails (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.white_label_emails add column if not exists tenant_id uuid null;
alter table plantaopro.white_label_emails add column if not exists cliente_id uuid null;
alter table plantaopro.white_label_emails add column if not exists parceiro_id uuid null;
alter table plantaopro.white_label_emails add column if not exists plano_id uuid null;
alter table plantaopro.white_label_emails add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.white_label_emails add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.white_label_emails add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.white_label_emails add column if not exists reg_update timestamptz null;
alter table plantaopro.white_label_emails add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_white_label_emails_tenant_id on plantaopro.white_label_emails(tenant_id);
create index if not exists ix_white_label_emails_cliente_id on plantaopro.white_label_emails(cliente_id);
create index if not exists ix_white_label_emails_status on plantaopro.white_label_emails(status);
create index if not exists ix_white_label_emails_reg_date on plantaopro.white_label_emails(reg_date desc);

create table if not exists plantaopro.white_label_parametros_mobile (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.white_label_parametros_mobile add column if not exists tenant_id uuid null;
alter table plantaopro.white_label_parametros_mobile add column if not exists cliente_id uuid null;
alter table plantaopro.white_label_parametros_mobile add column if not exists parceiro_id uuid null;
alter table plantaopro.white_label_parametros_mobile add column if not exists plano_id uuid null;
alter table plantaopro.white_label_parametros_mobile add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.white_label_parametros_mobile add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.white_label_parametros_mobile add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.white_label_parametros_mobile add column if not exists reg_update timestamptz null;
alter table plantaopro.white_label_parametros_mobile add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_white_label_parametros_mobile_tenant_id on plantaopro.white_label_parametros_mobile(tenant_id);
create index if not exists ix_white_label_parametros_mobile_cliente_id on plantaopro.white_label_parametros_mobile(cliente_id);
create index if not exists ix_white_label_parametros_mobile_status on plantaopro.white_label_parametros_mobile(status);
create index if not exists ix_white_label_parametros_mobile_reg_date on plantaopro.white_label_parametros_mobile(reg_date desc);

create table if not exists plantaopro.white_label_dominios (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.white_label_dominios add column if not exists tenant_id uuid null;
alter table plantaopro.white_label_dominios add column if not exists cliente_id uuid null;
alter table plantaopro.white_label_dominios add column if not exists parceiro_id uuid null;
alter table plantaopro.white_label_dominios add column if not exists plano_id uuid null;
alter table plantaopro.white_label_dominios add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.white_label_dominios add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.white_label_dominios add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.white_label_dominios add column if not exists reg_update timestamptz null;
alter table plantaopro.white_label_dominios add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_white_label_dominios_tenant_id on plantaopro.white_label_dominios(tenant_id);
create index if not exists ix_white_label_dominios_cliente_id on plantaopro.white_label_dominios(cliente_id);
create index if not exists ix_white_label_dominios_status on plantaopro.white_label_dominios(status);
create index if not exists ix_white_label_dominios_reg_date on plantaopro.white_label_dominios(reg_date desc);

create table if not exists plantaopro.white_label_publicacoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.white_label_publicacoes add column if not exists tenant_id uuid null;
alter table plantaopro.white_label_publicacoes add column if not exists cliente_id uuid null;
alter table plantaopro.white_label_publicacoes add column if not exists parceiro_id uuid null;
alter table plantaopro.white_label_publicacoes add column if not exists plano_id uuid null;
alter table plantaopro.white_label_publicacoes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.white_label_publicacoes add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.white_label_publicacoes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.white_label_publicacoes add column if not exists reg_update timestamptz null;
alter table plantaopro.white_label_publicacoes add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_white_label_publicacoes_tenant_id on plantaopro.white_label_publicacoes(tenant_id);
create index if not exists ix_white_label_publicacoes_cliente_id on plantaopro.white_label_publicacoes(cliente_id);
create index if not exists ix_white_label_publicacoes_status on plantaopro.white_label_publicacoes(status);
create index if not exists ix_white_label_publicacoes_reg_date on plantaopro.white_label_publicacoes(reg_date desc);

create table if not exists plantaopro.white_label_historico_alteracoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.white_label_historico_alteracoes add column if not exists tenant_id uuid null;
alter table plantaopro.white_label_historico_alteracoes add column if not exists cliente_id uuid null;
alter table plantaopro.white_label_historico_alteracoes add column if not exists parceiro_id uuid null;
alter table plantaopro.white_label_historico_alteracoes add column if not exists plano_id uuid null;
alter table plantaopro.white_label_historico_alteracoes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.white_label_historico_alteracoes add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.white_label_historico_alteracoes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.white_label_historico_alteracoes add column if not exists reg_update timestamptz null;
alter table plantaopro.white_label_historico_alteracoes add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_white_label_historico_alteracoes_tenant_id on plantaopro.white_label_historico_alteracoes(tenant_id);
create index if not exists ix_white_label_historico_alteracoes_cliente_id on plantaopro.white_label_historico_alteracoes(cliente_id);
create index if not exists ix_white_label_historico_alteracoes_status on plantaopro.white_label_historico_alteracoes(status);
create index if not exists ix_white_label_historico_alteracoes_reg_date on plantaopro.white_label_historico_alteracoes(reg_date desc);

create table if not exists plantaopro.cadastro_cliente_solicitacoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.cadastro_cliente_solicitacoes add column if not exists tenant_id uuid null;
alter table plantaopro.cadastro_cliente_solicitacoes add column if not exists cliente_id uuid null;
alter table plantaopro.cadastro_cliente_solicitacoes add column if not exists parceiro_id uuid null;
alter table plantaopro.cadastro_cliente_solicitacoes add column if not exists plano_id uuid null;
alter table plantaopro.cadastro_cliente_solicitacoes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.cadastro_cliente_solicitacoes add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.cadastro_cliente_solicitacoes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.cadastro_cliente_solicitacoes add column if not exists reg_update timestamptz null;
alter table plantaopro.cadastro_cliente_solicitacoes add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_cadastro_cliente_solicitacoes_tenant_id on plantaopro.cadastro_cliente_solicitacoes(tenant_id);
create index if not exists ix_cadastro_cliente_solicitacoes_cliente_id on plantaopro.cadastro_cliente_solicitacoes(cliente_id);
create index if not exists ix_cadastro_cliente_solicitacoes_status on plantaopro.cadastro_cliente_solicitacoes(status);
create index if not exists ix_cadastro_cliente_solicitacoes_reg_date on plantaopro.cadastro_cliente_solicitacoes(reg_date desc);

create table if not exists plantaopro.cadastro_cliente_etapas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.cadastro_cliente_etapas add column if not exists tenant_id uuid null;
alter table plantaopro.cadastro_cliente_etapas add column if not exists cliente_id uuid null;
alter table plantaopro.cadastro_cliente_etapas add column if not exists parceiro_id uuid null;
alter table plantaopro.cadastro_cliente_etapas add column if not exists plano_id uuid null;
alter table plantaopro.cadastro_cliente_etapas add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.cadastro_cliente_etapas add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.cadastro_cliente_etapas add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.cadastro_cliente_etapas add column if not exists reg_update timestamptz null;
alter table plantaopro.cadastro_cliente_etapas add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_cadastro_cliente_etapas_tenant_id on plantaopro.cadastro_cliente_etapas(tenant_id);
create index if not exists ix_cadastro_cliente_etapas_cliente_id on plantaopro.cadastro_cliente_etapas(cliente_id);
create index if not exists ix_cadastro_cliente_etapas_status on plantaopro.cadastro_cliente_etapas(status);
create index if not exists ix_cadastro_cliente_etapas_reg_date on plantaopro.cadastro_cliente_etapas(reg_date desc);

create table if not exists plantaopro.cadastro_cliente_validacoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.cadastro_cliente_validacoes add column if not exists tenant_id uuid null;
alter table plantaopro.cadastro_cliente_validacoes add column if not exists cliente_id uuid null;
alter table plantaopro.cadastro_cliente_validacoes add column if not exists parceiro_id uuid null;
alter table plantaopro.cadastro_cliente_validacoes add column if not exists plano_id uuid null;
alter table plantaopro.cadastro_cliente_validacoes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.cadastro_cliente_validacoes add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.cadastro_cliente_validacoes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.cadastro_cliente_validacoes add column if not exists reg_update timestamptz null;
alter table plantaopro.cadastro_cliente_validacoes add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_cadastro_cliente_validacoes_tenant_id on plantaopro.cadastro_cliente_validacoes(tenant_id);
create index if not exists ix_cadastro_cliente_validacoes_cliente_id on plantaopro.cadastro_cliente_validacoes(cliente_id);
create index if not exists ix_cadastro_cliente_validacoes_status on plantaopro.cadastro_cliente_validacoes(status);
create index if not exists ix_cadastro_cliente_validacoes_reg_date on plantaopro.cadastro_cliente_validacoes(reg_date desc);

create table if not exists plantaopro.cadastro_cliente_convites (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.cadastro_cliente_convites add column if not exists tenant_id uuid null;
alter table plantaopro.cadastro_cliente_convites add column if not exists cliente_id uuid null;
alter table plantaopro.cadastro_cliente_convites add column if not exists parceiro_id uuid null;
alter table plantaopro.cadastro_cliente_convites add column if not exists plano_id uuid null;
alter table plantaopro.cadastro_cliente_convites add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.cadastro_cliente_convites add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.cadastro_cliente_convites add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.cadastro_cliente_convites add column if not exists reg_update timestamptz null;
alter table plantaopro.cadastro_cliente_convites add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_cadastro_cliente_convites_tenant_id on plantaopro.cadastro_cliente_convites(tenant_id);
create index if not exists ix_cadastro_cliente_convites_cliente_id on plantaopro.cadastro_cliente_convites(cliente_id);
create index if not exists ix_cadastro_cliente_convites_status on plantaopro.cadastro_cliente_convites(status);
create index if not exists ix_cadastro_cliente_convites_reg_date on plantaopro.cadastro_cliente_convites(reg_date desc);

create table if not exists plantaopro.cadastro_cliente_pagamentos_iniciais (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.cadastro_cliente_pagamentos_iniciais add column if not exists tenant_id uuid null;
alter table plantaopro.cadastro_cliente_pagamentos_iniciais add column if not exists cliente_id uuid null;
alter table plantaopro.cadastro_cliente_pagamentos_iniciais add column if not exists parceiro_id uuid null;
alter table plantaopro.cadastro_cliente_pagamentos_iniciais add column if not exists plano_id uuid null;
alter table plantaopro.cadastro_cliente_pagamentos_iniciais add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.cadastro_cliente_pagamentos_iniciais add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.cadastro_cliente_pagamentos_iniciais add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.cadastro_cliente_pagamentos_iniciais add column if not exists reg_update timestamptz null;
alter table plantaopro.cadastro_cliente_pagamentos_iniciais add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_cadastro_cliente_pagamentos_iniciais_tenant_id on plantaopro.cadastro_cliente_pagamentos_iniciais(tenant_id);
create index if not exists ix_cadastro_cliente_pagamentos_iniciais_cliente_id on plantaopro.cadastro_cliente_pagamentos_iniciais(cliente_id);
create index if not exists ix_cadastro_cliente_pagamentos_iniciais_status on plantaopro.cadastro_cliente_pagamentos_iniciais(status);
create index if not exists ix_cadastro_cliente_pagamentos_iniciais_reg_date on plantaopro.cadastro_cliente_pagamentos_iniciais(reg_date desc);

create table if not exists plantaopro.cadastro_cliente_aceites (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.cadastro_cliente_aceites add column if not exists tenant_id uuid null;
alter table plantaopro.cadastro_cliente_aceites add column if not exists cliente_id uuid null;
alter table plantaopro.cadastro_cliente_aceites add column if not exists parceiro_id uuid null;
alter table plantaopro.cadastro_cliente_aceites add column if not exists plano_id uuid null;
alter table plantaopro.cadastro_cliente_aceites add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.cadastro_cliente_aceites add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.cadastro_cliente_aceites add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.cadastro_cliente_aceites add column if not exists reg_update timestamptz null;
alter table plantaopro.cadastro_cliente_aceites add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_cadastro_cliente_aceites_tenant_id on plantaopro.cadastro_cliente_aceites(tenant_id);
create index if not exists ix_cadastro_cliente_aceites_cliente_id on plantaopro.cadastro_cliente_aceites(cliente_id);
create index if not exists ix_cadastro_cliente_aceites_status on plantaopro.cadastro_cliente_aceites(status);
create index if not exists ix_cadastro_cliente_aceites_reg_date on plantaopro.cadastro_cliente_aceites(reg_date desc);

create table if not exists plantaopro.perfis (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.perfis add column if not exists tenant_id uuid null;
alter table plantaopro.perfis add column if not exists cliente_id uuid null;
alter table plantaopro.perfis add column if not exists parceiro_id uuid null;
alter table plantaopro.perfis add column if not exists plano_id uuid null;
alter table plantaopro.perfis add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.perfis add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.perfis add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.perfis add column if not exists reg_update timestamptz null;
alter table plantaopro.perfis add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_perfis_tenant_id on plantaopro.perfis(tenant_id);
create index if not exists ix_perfis_cliente_id on plantaopro.perfis(cliente_id);
create index if not exists ix_perfis_status on plantaopro.perfis(status);
create index if not exists ix_perfis_reg_date on plantaopro.perfis(reg_date desc);

create table if not exists plantaopro.permissoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.permissoes add column if not exists tenant_id uuid null;
alter table plantaopro.permissoes add column if not exists cliente_id uuid null;
alter table plantaopro.permissoes add column if not exists parceiro_id uuid null;
alter table plantaopro.permissoes add column if not exists plano_id uuid null;
alter table plantaopro.permissoes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.permissoes add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.permissoes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.permissoes add column if not exists reg_update timestamptz null;
alter table plantaopro.permissoes add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_permissoes_tenant_id on plantaopro.permissoes(tenant_id);
create index if not exists ix_permissoes_cliente_id on plantaopro.permissoes(cliente_id);
create index if not exists ix_permissoes_status on plantaopro.permissoes(status);
create index if not exists ix_permissoes_reg_date on plantaopro.permissoes(reg_date desc);

create table if not exists plantaopro.modulos_sistema (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.modulos_sistema add column if not exists tenant_id uuid null;
alter table plantaopro.modulos_sistema add column if not exists cliente_id uuid null;
alter table plantaopro.modulos_sistema add column if not exists parceiro_id uuid null;
alter table plantaopro.modulos_sistema add column if not exists plano_id uuid null;
alter table plantaopro.modulos_sistema add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.modulos_sistema add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.modulos_sistema add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.modulos_sistema add column if not exists reg_update timestamptz null;
alter table plantaopro.modulos_sistema add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_modulos_sistema_tenant_id on plantaopro.modulos_sistema(tenant_id);
create index if not exists ix_modulos_sistema_cliente_id on plantaopro.modulos_sistema(cliente_id);
create index if not exists ix_modulos_sistema_status on plantaopro.modulos_sistema(status);
create index if not exists ix_modulos_sistema_reg_date on plantaopro.modulos_sistema(reg_date desc);

create table if not exists plantaopro.acoes_sistema (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.acoes_sistema add column if not exists tenant_id uuid null;
alter table plantaopro.acoes_sistema add column if not exists cliente_id uuid null;
alter table plantaopro.acoes_sistema add column if not exists parceiro_id uuid null;
alter table plantaopro.acoes_sistema add column if not exists plano_id uuid null;
alter table plantaopro.acoes_sistema add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.acoes_sistema add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.acoes_sistema add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.acoes_sistema add column if not exists reg_update timestamptz null;
alter table plantaopro.acoes_sistema add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_acoes_sistema_tenant_id on plantaopro.acoes_sistema(tenant_id);
create index if not exists ix_acoes_sistema_cliente_id on plantaopro.acoes_sistema(cliente_id);
create index if not exists ix_acoes_sistema_status on plantaopro.acoes_sistema(status);
create index if not exists ix_acoes_sistema_reg_date on plantaopro.acoes_sistema(reg_date desc);

create table if not exists plantaopro.perfil_permissoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.perfil_permissoes add column if not exists tenant_id uuid null;
alter table plantaopro.perfil_permissoes add column if not exists cliente_id uuid null;
alter table plantaopro.perfil_permissoes add column if not exists parceiro_id uuid null;
alter table plantaopro.perfil_permissoes add column if not exists plano_id uuid null;
alter table plantaopro.perfil_permissoes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.perfil_permissoes add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.perfil_permissoes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.perfil_permissoes add column if not exists reg_update timestamptz null;
alter table plantaopro.perfil_permissoes add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_perfil_permissoes_tenant_id on plantaopro.perfil_permissoes(tenant_id);
create index if not exists ix_perfil_permissoes_cliente_id on plantaopro.perfil_permissoes(cliente_id);
create index if not exists ix_perfil_permissoes_status on plantaopro.perfil_permissoes(status);
create index if not exists ix_perfil_permissoes_reg_date on plantaopro.perfil_permissoes(reg_date desc);

create table if not exists plantaopro.perfil_modulos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.perfil_modulos add column if not exists tenant_id uuid null;
alter table plantaopro.perfil_modulos add column if not exists cliente_id uuid null;
alter table plantaopro.perfil_modulos add column if not exists parceiro_id uuid null;
alter table plantaopro.perfil_modulos add column if not exists plano_id uuid null;
alter table plantaopro.perfil_modulos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.perfil_modulos add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.perfil_modulos add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.perfil_modulos add column if not exists reg_update timestamptz null;
alter table plantaopro.perfil_modulos add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_perfil_modulos_tenant_id on plantaopro.perfil_modulos(tenant_id);
create index if not exists ix_perfil_modulos_cliente_id on plantaopro.perfil_modulos(cliente_id);
create index if not exists ix_perfil_modulos_status on plantaopro.perfil_modulos(status);
create index if not exists ix_perfil_modulos_reg_date on plantaopro.perfil_modulos(reg_date desc);

create table if not exists plantaopro.usuarios_perfis (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.usuarios_perfis add column if not exists tenant_id uuid null;
alter table plantaopro.usuarios_perfis add column if not exists cliente_id uuid null;
alter table plantaopro.usuarios_perfis add column if not exists parceiro_id uuid null;
alter table plantaopro.usuarios_perfis add column if not exists plano_id uuid null;
alter table plantaopro.usuarios_perfis add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.usuarios_perfis add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.usuarios_perfis add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.usuarios_perfis add column if not exists reg_update timestamptz null;
alter table plantaopro.usuarios_perfis add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_usuarios_perfis_tenant_id on plantaopro.usuarios_perfis(tenant_id);
create index if not exists ix_usuarios_perfis_cliente_id on plantaopro.usuarios_perfis(cliente_id);
create index if not exists ix_usuarios_perfis_status on plantaopro.usuarios_perfis(status);
create index if not exists ix_usuarios_perfis_reg_date on plantaopro.usuarios_perfis(reg_date desc);

create table if not exists plantaopro.usuario_permissoes_especiais (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.usuario_permissoes_especiais add column if not exists tenant_id uuid null;
alter table plantaopro.usuario_permissoes_especiais add column if not exists cliente_id uuid null;
alter table plantaopro.usuario_permissoes_especiais add column if not exists parceiro_id uuid null;
alter table plantaopro.usuario_permissoes_especiais add column if not exists plano_id uuid null;
alter table plantaopro.usuario_permissoes_especiais add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.usuario_permissoes_especiais add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.usuario_permissoes_especiais add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.usuario_permissoes_especiais add column if not exists reg_update timestamptz null;
alter table plantaopro.usuario_permissoes_especiais add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_usuario_permissoes_especiais_tenant_id on plantaopro.usuario_permissoes_especiais(tenant_id);
create index if not exists ix_usuario_permissoes_especiais_cliente_id on plantaopro.usuario_permissoes_especiais(cliente_id);
create index if not exists ix_usuario_permissoes_especiais_status on plantaopro.usuario_permissoes_especiais(status);
create index if not exists ix_usuario_permissoes_especiais_reg_date on plantaopro.usuario_permissoes_especiais(reg_date desc);

create table if not exists plantaopro.planos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A',
    slug varchar(160) null,
    nome varchar(200) null,
    permite_white_label boolean not null default false,
    permite_api boolean not null default false,
    valor_mensal numeric(12,2) not null default 0
);
alter table plantaopro.planos add column if not exists tenant_id uuid null;
alter table plantaopro.planos add column if not exists cliente_id uuid null;
alter table plantaopro.planos add column if not exists parceiro_id uuid null;
alter table plantaopro.planos add column if not exists plano_id uuid null;
alter table plantaopro.planos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.planos add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.planos add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.planos add column if not exists reg_update timestamptz null;
alter table plantaopro.planos add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_planos_tenant_id on plantaopro.planos(tenant_id);
create index if not exists ix_planos_cliente_id on plantaopro.planos(cliente_id);
create index if not exists ix_planos_status on plantaopro.planos(status);
create index if not exists ix_planos_reg_date on plantaopro.planos(reg_date desc);

create table if not exists plantaopro.plano_recursos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.plano_recursos add column if not exists tenant_id uuid null;
alter table plantaopro.plano_recursos add column if not exists cliente_id uuid null;
alter table plantaopro.plano_recursos add column if not exists parceiro_id uuid null;
alter table plantaopro.plano_recursos add column if not exists plano_id uuid null;
alter table plantaopro.plano_recursos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.plano_recursos add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.plano_recursos add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.plano_recursos add column if not exists reg_update timestamptz null;
alter table plantaopro.plano_recursos add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_plano_recursos_tenant_id on plantaopro.plano_recursos(tenant_id);
create index if not exists ix_plano_recursos_cliente_id on plantaopro.plano_recursos(cliente_id);
create index if not exists ix_plano_recursos_status on plantaopro.plano_recursos(status);
create index if not exists ix_plano_recursos_reg_date on plantaopro.plano_recursos(reg_date desc);

create table if not exists plantaopro.plano_modulos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.plano_modulos add column if not exists tenant_id uuid null;
alter table plantaopro.plano_modulos add column if not exists cliente_id uuid null;
alter table plantaopro.plano_modulos add column if not exists parceiro_id uuid null;
alter table plantaopro.plano_modulos add column if not exists plano_id uuid null;
alter table plantaopro.plano_modulos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.plano_modulos add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.plano_modulos add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.plano_modulos add column if not exists reg_update timestamptz null;
alter table plantaopro.plano_modulos add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_plano_modulos_tenant_id on plantaopro.plano_modulos(tenant_id);
create index if not exists ix_plano_modulos_cliente_id on plantaopro.plano_modulos(cliente_id);
create index if not exists ix_plano_modulos_status on plantaopro.plano_modulos(status);
create index if not exists ix_plano_modulos_reg_date on plantaopro.plano_modulos(reg_date desc);

create table if not exists plantaopro.plano_precos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.plano_precos add column if not exists tenant_id uuid null;
alter table plantaopro.plano_precos add column if not exists cliente_id uuid null;
alter table plantaopro.plano_precos add column if not exists parceiro_id uuid null;
alter table plantaopro.plano_precos add column if not exists plano_id uuid null;
alter table plantaopro.plano_precos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.plano_precos add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.plano_precos add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.plano_precos add column if not exists reg_update timestamptz null;
alter table plantaopro.plano_precos add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_plano_precos_tenant_id on plantaopro.plano_precos(tenant_id);
create index if not exists ix_plano_precos_cliente_id on plantaopro.plano_precos(cliente_id);
create index if not exists ix_plano_precos_status on plantaopro.plano_precos(status);
create index if not exists ix_plano_precos_reg_date on plantaopro.plano_precos(reg_date desc);

create table if not exists plantaopro.plano_limites (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.plano_limites add column if not exists tenant_id uuid null;
alter table plantaopro.plano_limites add column if not exists cliente_id uuid null;
alter table plantaopro.plano_limites add column if not exists parceiro_id uuid null;
alter table plantaopro.plano_limites add column if not exists plano_id uuid null;
alter table plantaopro.plano_limites add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.plano_limites add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.plano_limites add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.plano_limites add column if not exists reg_update timestamptz null;
alter table plantaopro.plano_limites add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_plano_limites_tenant_id on plantaopro.plano_limites(tenant_id);
create index if not exists ix_plano_limites_cliente_id on plantaopro.plano_limites(cliente_id);
create index if not exists ix_plano_limites_status on plantaopro.plano_limites(status);
create index if not exists ix_plano_limites_reg_date on plantaopro.plano_limites(reg_date desc);

create table if not exists plantaopro.plano_comparativo (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.plano_comparativo add column if not exists tenant_id uuid null;
alter table plantaopro.plano_comparativo add column if not exists cliente_id uuid null;
alter table plantaopro.plano_comparativo add column if not exists parceiro_id uuid null;
alter table plantaopro.plano_comparativo add column if not exists plano_id uuid null;
alter table plantaopro.plano_comparativo add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.plano_comparativo add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.plano_comparativo add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.plano_comparativo add column if not exists reg_update timestamptz null;
alter table plantaopro.plano_comparativo add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_plano_comparativo_tenant_id on plantaopro.plano_comparativo(tenant_id);
create index if not exists ix_plano_comparativo_cliente_id on plantaopro.plano_comparativo(cliente_id);
create index if not exists ix_plano_comparativo_status on plantaopro.plano_comparativo(status);
create index if not exists ix_plano_comparativo_reg_date on plantaopro.plano_comparativo(reg_date desc);

create table if not exists plantaopro.plano_faq (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.plano_faq add column if not exists tenant_id uuid null;
alter table plantaopro.plano_faq add column if not exists cliente_id uuid null;
alter table plantaopro.plano_faq add column if not exists parceiro_id uuid null;
alter table plantaopro.plano_faq add column if not exists plano_id uuid null;
alter table plantaopro.plano_faq add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.plano_faq add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.plano_faq add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.plano_faq add column if not exists reg_update timestamptz null;
alter table plantaopro.plano_faq add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_plano_faq_tenant_id on plantaopro.plano_faq(tenant_id);
create index if not exists ix_plano_faq_cliente_id on plantaopro.plano_faq(cliente_id);
create index if not exists ix_plano_faq_status on plantaopro.plano_faq(status);
create index if not exists ix_plano_faq_reg_date on plantaopro.plano_faq(reg_date desc);

create table if not exists plantaopro.plano_setup_taxas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.plano_setup_taxas add column if not exists tenant_id uuid null;
alter table plantaopro.plano_setup_taxas add column if not exists cliente_id uuid null;
alter table plantaopro.plano_setup_taxas add column if not exists parceiro_id uuid null;
alter table plantaopro.plano_setup_taxas add column if not exists plano_id uuid null;
alter table plantaopro.plano_setup_taxas add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.plano_setup_taxas add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.plano_setup_taxas add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.plano_setup_taxas add column if not exists reg_update timestamptz null;
alter table plantaopro.plano_setup_taxas add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_plano_setup_taxas_tenant_id on plantaopro.plano_setup_taxas(tenant_id);
create index if not exists ix_plano_setup_taxas_cliente_id on plantaopro.plano_setup_taxas(cliente_id);
create index if not exists ix_plano_setup_taxas_status on plantaopro.plano_setup_taxas(status);
create index if not exists ix_plano_setup_taxas_reg_date on plantaopro.plano_setup_taxas(reg_date desc);

create table if not exists plantaopro.plano_sla (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.plano_sla add column if not exists tenant_id uuid null;
alter table plantaopro.plano_sla add column if not exists cliente_id uuid null;
alter table plantaopro.plano_sla add column if not exists parceiro_id uuid null;
alter table plantaopro.plano_sla add column if not exists plano_id uuid null;
alter table plantaopro.plano_sla add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.plano_sla add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.plano_sla add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.plano_sla add column if not exists reg_update timestamptz null;
alter table plantaopro.plano_sla add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_plano_sla_tenant_id on plantaopro.plano_sla(tenant_id);
create index if not exists ix_plano_sla_cliente_id on plantaopro.plano_sla(cliente_id);
create index if not exists ix_plano_sla_status on plantaopro.plano_sla(status);
create index if not exists ix_plano_sla_reg_date on plantaopro.plano_sla(reg_date desc);

create table if not exists plantaopro.plano_api_limites (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.plano_api_limites add column if not exists tenant_id uuid null;
alter table plantaopro.plano_api_limites add column if not exists cliente_id uuid null;
alter table plantaopro.plano_api_limites add column if not exists parceiro_id uuid null;
alter table plantaopro.plano_api_limites add column if not exists plano_id uuid null;
alter table plantaopro.plano_api_limites add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.plano_api_limites add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.plano_api_limites add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.plano_api_limites add column if not exists reg_update timestamptz null;
alter table plantaopro.plano_api_limites add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_plano_api_limites_tenant_id on plantaopro.plano_api_limites(tenant_id);
create index if not exists ix_plano_api_limites_cliente_id on plantaopro.plano_api_limites(cliente_id);
create index if not exists ix_plano_api_limites_status on plantaopro.plano_api_limites(status);
create index if not exists ix_plano_api_limites_reg_date on plantaopro.plano_api_limites(reg_date desc);

create table if not exists plantaopro.plano_armazenamento_limites (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.plano_armazenamento_limites add column if not exists tenant_id uuid null;
alter table plantaopro.plano_armazenamento_limites add column if not exists cliente_id uuid null;
alter table plantaopro.plano_armazenamento_limites add column if not exists parceiro_id uuid null;
alter table plantaopro.plano_armazenamento_limites add column if not exists plano_id uuid null;
alter table plantaopro.plano_armazenamento_limites add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.plano_armazenamento_limites add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.plano_armazenamento_limites add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.plano_armazenamento_limites add column if not exists reg_update timestamptz null;
alter table plantaopro.plano_armazenamento_limites add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_plano_armazenamento_limites_tenant_id on plantaopro.plano_armazenamento_limites(tenant_id);
create index if not exists ix_plano_armazenamento_limites_cliente_id on plantaopro.plano_armazenamento_limites(cliente_id);
create index if not exists ix_plano_armazenamento_limites_status on plantaopro.plano_armazenamento_limites(status);
create index if not exists ix_plano_armazenamento_limites_reg_date on plantaopro.plano_armazenamento_limites(reg_date desc);

create table if not exists plantaopro.upgrade_solicitacoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.upgrade_solicitacoes add column if not exists tenant_id uuid null;
alter table plantaopro.upgrade_solicitacoes add column if not exists cliente_id uuid null;
alter table plantaopro.upgrade_solicitacoes add column if not exists parceiro_id uuid null;
alter table plantaopro.upgrade_solicitacoes add column if not exists plano_id uuid null;
alter table plantaopro.upgrade_solicitacoes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.upgrade_solicitacoes add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.upgrade_solicitacoes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.upgrade_solicitacoes add column if not exists reg_update timestamptz null;
alter table plantaopro.upgrade_solicitacoes add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_upgrade_solicitacoes_tenant_id on plantaopro.upgrade_solicitacoes(tenant_id);
create index if not exists ix_upgrade_solicitacoes_cliente_id on plantaopro.upgrade_solicitacoes(cliente_id);
create index if not exists ix_upgrade_solicitacoes_status on plantaopro.upgrade_solicitacoes(status);
create index if not exists ix_upgrade_solicitacoes_reg_date on plantaopro.upgrade_solicitacoes(reg_date desc);

create table if not exists plantaopro.downgrade_solicitacoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.downgrade_solicitacoes add column if not exists tenant_id uuid null;
alter table plantaopro.downgrade_solicitacoes add column if not exists cliente_id uuid null;
alter table plantaopro.downgrade_solicitacoes add column if not exists parceiro_id uuid null;
alter table plantaopro.downgrade_solicitacoes add column if not exists plano_id uuid null;
alter table plantaopro.downgrade_solicitacoes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.downgrade_solicitacoes add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.downgrade_solicitacoes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.downgrade_solicitacoes add column if not exists reg_update timestamptz null;
alter table plantaopro.downgrade_solicitacoes add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_downgrade_solicitacoes_tenant_id on plantaopro.downgrade_solicitacoes(tenant_id);
create index if not exists ix_downgrade_solicitacoes_cliente_id on plantaopro.downgrade_solicitacoes(cliente_id);
create index if not exists ix_downgrade_solicitacoes_status on plantaopro.downgrade_solicitacoes(status);
create index if not exists ix_downgrade_solicitacoes_reg_date on plantaopro.downgrade_solicitacoes(reg_date desc);

create table if not exists plantaopro.parceiros (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.parceiros add column if not exists tenant_id uuid null;
alter table plantaopro.parceiros add column if not exists cliente_id uuid null;
alter table plantaopro.parceiros add column if not exists parceiro_id uuid null;
alter table plantaopro.parceiros add column if not exists plano_id uuid null;
alter table plantaopro.parceiros add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.parceiros add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.parceiros add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.parceiros add column if not exists reg_update timestamptz null;
alter table plantaopro.parceiros add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_parceiros_tenant_id on plantaopro.parceiros(tenant_id);
create index if not exists ix_parceiros_cliente_id on plantaopro.parceiros(cliente_id);
create index if not exists ix_parceiros_status on plantaopro.parceiros(status);
create index if not exists ix_parceiros_reg_date on plantaopro.parceiros(reg_date desc);

create table if not exists plantaopro.parceiro_tenants (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.parceiro_tenants add column if not exists tenant_id uuid null;
alter table plantaopro.parceiro_tenants add column if not exists cliente_id uuid null;
alter table plantaopro.parceiro_tenants add column if not exists parceiro_id uuid null;
alter table plantaopro.parceiro_tenants add column if not exists plano_id uuid null;
alter table plantaopro.parceiro_tenants add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.parceiro_tenants add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.parceiro_tenants add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.parceiro_tenants add column if not exists reg_update timestamptz null;
alter table plantaopro.parceiro_tenants add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_parceiro_tenants_tenant_id on plantaopro.parceiro_tenants(tenant_id);
create index if not exists ix_parceiro_tenants_cliente_id on plantaopro.parceiro_tenants(cliente_id);
create index if not exists ix_parceiro_tenants_status on plantaopro.parceiro_tenants(status);
create index if not exists ix_parceiro_tenants_reg_date on plantaopro.parceiro_tenants(reg_date desc);

create table if not exists plantaopro.parceiro_planos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.parceiro_planos add column if not exists tenant_id uuid null;
alter table plantaopro.parceiro_planos add column if not exists cliente_id uuid null;
alter table plantaopro.parceiro_planos add column if not exists parceiro_id uuid null;
alter table plantaopro.parceiro_planos add column if not exists plano_id uuid null;
alter table plantaopro.parceiro_planos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.parceiro_planos add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.parceiro_planos add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.parceiro_planos add column if not exists reg_update timestamptz null;
alter table plantaopro.parceiro_planos add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_parceiro_planos_tenant_id on plantaopro.parceiro_planos(tenant_id);
create index if not exists ix_parceiro_planos_cliente_id on plantaopro.parceiro_planos(cliente_id);
create index if not exists ix_parceiro_planos_status on plantaopro.parceiro_planos(status);
create index if not exists ix_parceiro_planos_reg_date on plantaopro.parceiro_planos(reg_date desc);

create table if not exists plantaopro.parceiro_comissoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.parceiro_comissoes add column if not exists tenant_id uuid null;
alter table plantaopro.parceiro_comissoes add column if not exists cliente_id uuid null;
alter table plantaopro.parceiro_comissoes add column if not exists parceiro_id uuid null;
alter table plantaopro.parceiro_comissoes add column if not exists plano_id uuid null;
alter table plantaopro.parceiro_comissoes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.parceiro_comissoes add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.parceiro_comissoes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.parceiro_comissoes add column if not exists reg_update timestamptz null;
alter table plantaopro.parceiro_comissoes add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_parceiro_comissoes_tenant_id on plantaopro.parceiro_comissoes(tenant_id);
create index if not exists ix_parceiro_comissoes_cliente_id on plantaopro.parceiro_comissoes(cliente_id);
create index if not exists ix_parceiro_comissoes_status on plantaopro.parceiro_comissoes(status);
create index if not exists ix_parceiro_comissoes_reg_date on plantaopro.parceiro_comissoes(reg_date desc);

create table if not exists plantaopro.parceiro_repasses (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.parceiro_repasses add column if not exists tenant_id uuid null;
alter table plantaopro.parceiro_repasses add column if not exists cliente_id uuid null;
alter table plantaopro.parceiro_repasses add column if not exists parceiro_id uuid null;
alter table plantaopro.parceiro_repasses add column if not exists plano_id uuid null;
alter table plantaopro.parceiro_repasses add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.parceiro_repasses add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.parceiro_repasses add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.parceiro_repasses add column if not exists reg_update timestamptz null;
alter table plantaopro.parceiro_repasses add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_parceiro_repasses_tenant_id on plantaopro.parceiro_repasses(tenant_id);
create index if not exists ix_parceiro_repasses_cliente_id on plantaopro.parceiro_repasses(cliente_id);
create index if not exists ix_parceiro_repasses_status on plantaopro.parceiro_repasses(status);
create index if not exists ix_parceiro_repasses_reg_date on plantaopro.parceiro_repasses(reg_date desc);

create table if not exists plantaopro.parceiro_leads (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.parceiro_leads add column if not exists tenant_id uuid null;
alter table plantaopro.parceiro_leads add column if not exists cliente_id uuid null;
alter table plantaopro.parceiro_leads add column if not exists parceiro_id uuid null;
alter table plantaopro.parceiro_leads add column if not exists plano_id uuid null;
alter table plantaopro.parceiro_leads add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.parceiro_leads add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.parceiro_leads add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.parceiro_leads add column if not exists reg_update timestamptz null;
alter table plantaopro.parceiro_leads add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_parceiro_leads_tenant_id on plantaopro.parceiro_leads(tenant_id);
create index if not exists ix_parceiro_leads_cliente_id on plantaopro.parceiro_leads(cliente_id);
create index if not exists ix_parceiro_leads_status on plantaopro.parceiro_leads(status);
create index if not exists ix_parceiro_leads_reg_date on plantaopro.parceiro_leads(reg_date desc);

create table if not exists plantaopro.parceiro_oportunidades (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.parceiro_oportunidades add column if not exists tenant_id uuid null;
alter table plantaopro.parceiro_oportunidades add column if not exists cliente_id uuid null;
alter table plantaopro.parceiro_oportunidades add column if not exists parceiro_id uuid null;
alter table plantaopro.parceiro_oportunidades add column if not exists plano_id uuid null;
alter table plantaopro.parceiro_oportunidades add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.parceiro_oportunidades add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.parceiro_oportunidades add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.parceiro_oportunidades add column if not exists reg_update timestamptz null;
alter table plantaopro.parceiro_oportunidades add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_parceiro_oportunidades_tenant_id on plantaopro.parceiro_oportunidades(tenant_id);
create index if not exists ix_parceiro_oportunidades_cliente_id on plantaopro.parceiro_oportunidades(cliente_id);
create index if not exists ix_parceiro_oportunidades_status on plantaopro.parceiro_oportunidades(status);
create index if not exists ix_parceiro_oportunidades_reg_date on plantaopro.parceiro_oportunidades(reg_date desc);

create table if not exists plantaopro.parceiro_margens (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.parceiro_margens add column if not exists tenant_id uuid null;
alter table plantaopro.parceiro_margens add column if not exists cliente_id uuid null;
alter table plantaopro.parceiro_margens add column if not exists parceiro_id uuid null;
alter table plantaopro.parceiro_margens add column if not exists plano_id uuid null;
alter table plantaopro.parceiro_margens add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.parceiro_margens add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.parceiro_margens add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.parceiro_margens add column if not exists reg_update timestamptz null;
alter table plantaopro.parceiro_margens add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_parceiro_margens_tenant_id on plantaopro.parceiro_margens(tenant_id);
create index if not exists ix_parceiro_margens_cliente_id on plantaopro.parceiro_margens(cliente_id);
create index if not exists ix_parceiro_margens_status on plantaopro.parceiro_margens(status);
create index if not exists ix_parceiro_margens_reg_date on plantaopro.parceiro_margens(reg_date desc);

create table if not exists plantaopro.parceiro_contratos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.parceiro_contratos add column if not exists tenant_id uuid null;
alter table plantaopro.parceiro_contratos add column if not exists cliente_id uuid null;
alter table plantaopro.parceiro_contratos add column if not exists parceiro_id uuid null;
alter table plantaopro.parceiro_contratos add column if not exists plano_id uuid null;
alter table plantaopro.parceiro_contratos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.parceiro_contratos add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.parceiro_contratos add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.parceiro_contratos add column if not exists reg_update timestamptz null;
alter table plantaopro.parceiro_contratos add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_parceiro_contratos_tenant_id on plantaopro.parceiro_contratos(tenant_id);
create index if not exists ix_parceiro_contratos_cliente_id on plantaopro.parceiro_contratos(cliente_id);
create index if not exists ix_parceiro_contratos_status on plantaopro.parceiro_contratos(status);
create index if not exists ix_parceiro_contratos_reg_date on plantaopro.parceiro_contratos(reg_date desc);

create table if not exists plantaopro.contratos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A',
    numero varchar(80) null,
    propriedade_dados text null,
    taxa_setup numeric(12,2) not null default 0
);
alter table plantaopro.contratos add column if not exists tenant_id uuid null;
alter table plantaopro.contratos add column if not exists cliente_id uuid null;
alter table plantaopro.contratos add column if not exists parceiro_id uuid null;
alter table plantaopro.contratos add column if not exists plano_id uuid null;
alter table plantaopro.contratos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.contratos add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.contratos add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.contratos add column if not exists reg_update timestamptz null;
alter table plantaopro.contratos add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_contratos_tenant_id on plantaopro.contratos(tenant_id);
create index if not exists ix_contratos_cliente_id on plantaopro.contratos(cliente_id);
create index if not exists ix_contratos_status on plantaopro.contratos(status);
create index if not exists ix_contratos_reg_date on plantaopro.contratos(reg_date desc);

create table if not exists plantaopro.contrato_itens (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.contrato_itens add column if not exists tenant_id uuid null;
alter table plantaopro.contrato_itens add column if not exists cliente_id uuid null;
alter table plantaopro.contrato_itens add column if not exists parceiro_id uuid null;
alter table plantaopro.contrato_itens add column if not exists plano_id uuid null;
alter table plantaopro.contrato_itens add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.contrato_itens add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.contrato_itens add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.contrato_itens add column if not exists reg_update timestamptz null;
alter table plantaopro.contrato_itens add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_contrato_itens_tenant_id on plantaopro.contrato_itens(tenant_id);
create index if not exists ix_contrato_itens_cliente_id on plantaopro.contrato_itens(cliente_id);
create index if not exists ix_contrato_itens_status on plantaopro.contrato_itens(status);
create index if not exists ix_contrato_itens_reg_date on plantaopro.contrato_itens(reg_date desc);

create table if not exists plantaopro.contrato_slas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.contrato_slas add column if not exists tenant_id uuid null;
alter table plantaopro.contrato_slas add column if not exists cliente_id uuid null;
alter table plantaopro.contrato_slas add column if not exists parceiro_id uuid null;
alter table plantaopro.contrato_slas add column if not exists plano_id uuid null;
alter table plantaopro.contrato_slas add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.contrato_slas add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.contrato_slas add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.contrato_slas add column if not exists reg_update timestamptz null;
alter table plantaopro.contrato_slas add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_contrato_slas_tenant_id on plantaopro.contrato_slas(tenant_id);
create index if not exists ix_contrato_slas_cliente_id on plantaopro.contrato_slas(cliente_id);
create index if not exists ix_contrato_slas_status on plantaopro.contrato_slas(status);
create index if not exists ix_contrato_slas_reg_date on plantaopro.contrato_slas(reg_date desc);

create table if not exists plantaopro.contrato_aceites (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.contrato_aceites add column if not exists tenant_id uuid null;
alter table plantaopro.contrato_aceites add column if not exists cliente_id uuid null;
alter table plantaopro.contrato_aceites add column if not exists parceiro_id uuid null;
alter table plantaopro.contrato_aceites add column if not exists plano_id uuid null;
alter table plantaopro.contrato_aceites add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.contrato_aceites add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.contrato_aceites add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.contrato_aceites add column if not exists reg_update timestamptz null;
alter table plantaopro.contrato_aceites add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_contrato_aceites_tenant_id on plantaopro.contrato_aceites(tenant_id);
create index if not exists ix_contrato_aceites_cliente_id on plantaopro.contrato_aceites(cliente_id);
create index if not exists ix_contrato_aceites_status on plantaopro.contrato_aceites(status);
create index if not exists ix_contrato_aceites_reg_date on plantaopro.contrato_aceites(reg_date desc);

create table if not exists plantaopro.contrato_renovacoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.contrato_renovacoes add column if not exists tenant_id uuid null;
alter table plantaopro.contrato_renovacoes add column if not exists cliente_id uuid null;
alter table plantaopro.contrato_renovacoes add column if not exists parceiro_id uuid null;
alter table plantaopro.contrato_renovacoes add column if not exists plano_id uuid null;
alter table plantaopro.contrato_renovacoes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.contrato_renovacoes add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.contrato_renovacoes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.contrato_renovacoes add column if not exists reg_update timestamptz null;
alter table plantaopro.contrato_renovacoes add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_contrato_renovacoes_tenant_id on plantaopro.contrato_renovacoes(tenant_id);
create index if not exists ix_contrato_renovacoes_cliente_id on plantaopro.contrato_renovacoes(cliente_id);
create index if not exists ix_contrato_renovacoes_status on plantaopro.contrato_renovacoes(status);
create index if not exists ix_contrato_renovacoes_reg_date on plantaopro.contrato_renovacoes(reg_date desc);

create table if not exists plantaopro.contrato_anexos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.contrato_anexos add column if not exists tenant_id uuid null;
alter table plantaopro.contrato_anexos add column if not exists cliente_id uuid null;
alter table plantaopro.contrato_anexos add column if not exists parceiro_id uuid null;
alter table plantaopro.contrato_anexos add column if not exists plano_id uuid null;
alter table plantaopro.contrato_anexos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.contrato_anexos add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.contrato_anexos add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.contrato_anexos add column if not exists reg_update timestamptz null;
alter table plantaopro.contrato_anexos add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_contrato_anexos_tenant_id on plantaopro.contrato_anexos(tenant_id);
create index if not exists ix_contrato_anexos_cliente_id on plantaopro.contrato_anexos(cliente_id);
create index if not exists ix_contrato_anexos_status on plantaopro.contrato_anexos(status);
create index if not exists ix_contrato_anexos_reg_date on plantaopro.contrato_anexos(reg_date desc);

create table if not exists plantaopro.sla_eventos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.sla_eventos add column if not exists tenant_id uuid null;
alter table plantaopro.sla_eventos add column if not exists cliente_id uuid null;
alter table plantaopro.sla_eventos add column if not exists parceiro_id uuid null;
alter table plantaopro.sla_eventos add column if not exists plano_id uuid null;
alter table plantaopro.sla_eventos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.sla_eventos add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.sla_eventos add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.sla_eventos add column if not exists reg_update timestamptz null;
alter table plantaopro.sla_eventos add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_sla_eventos_tenant_id on plantaopro.sla_eventos(tenant_id);
create index if not exists ix_sla_eventos_cliente_id on plantaopro.sla_eventos(cliente_id);
create index if not exists ix_sla_eventos_status on plantaopro.sla_eventos(status);
create index if not exists ix_sla_eventos_reg_date on plantaopro.sla_eventos(reg_date desc);

create table if not exists plantaopro.sla_incidentes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.sla_incidentes add column if not exists tenant_id uuid null;
alter table plantaopro.sla_incidentes add column if not exists cliente_id uuid null;
alter table plantaopro.sla_incidentes add column if not exists parceiro_id uuid null;
alter table plantaopro.sla_incidentes add column if not exists plano_id uuid null;
alter table plantaopro.sla_incidentes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.sla_incidentes add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.sla_incidentes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.sla_incidentes add column if not exists reg_update timestamptz null;
alter table plantaopro.sla_incidentes add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_sla_incidentes_tenant_id on plantaopro.sla_incidentes(tenant_id);
create index if not exists ix_sla_incidentes_cliente_id on plantaopro.sla_incidentes(cliente_id);
create index if not exists ix_sla_incidentes_status on plantaopro.sla_incidentes(status);
create index if not exists ix_sla_incidentes_reg_date on plantaopro.sla_incidentes(reg_date desc);

create table if not exists plantaopro.sla_indicadores (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.sla_indicadores add column if not exists tenant_id uuid null;
alter table plantaopro.sla_indicadores add column if not exists cliente_id uuid null;
alter table plantaopro.sla_indicadores add column if not exists parceiro_id uuid null;
alter table plantaopro.sla_indicadores add column if not exists plano_id uuid null;
alter table plantaopro.sla_indicadores add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.sla_indicadores add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.sla_indicadores add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.sla_indicadores add column if not exists reg_update timestamptz null;
alter table plantaopro.sla_indicadores add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_sla_indicadores_tenant_id on plantaopro.sla_indicadores(tenant_id);
create index if not exists ix_sla_indicadores_cliente_id on plantaopro.sla_indicadores(cliente_id);
create index if not exists ix_sla_indicadores_status on plantaopro.sla_indicadores(status);
create index if not exists ix_sla_indicadores_reg_date on plantaopro.sla_indicadores(reg_date desc);

create table if not exists plantaopro.api_clientes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.api_clientes add column if not exists tenant_id uuid null;
alter table plantaopro.api_clientes add column if not exists cliente_id uuid null;
alter table plantaopro.api_clientes add column if not exists parceiro_id uuid null;
alter table plantaopro.api_clientes add column if not exists plano_id uuid null;
alter table plantaopro.api_clientes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.api_clientes add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.api_clientes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.api_clientes add column if not exists reg_update timestamptz null;
alter table plantaopro.api_clientes add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_api_clientes_tenant_id on plantaopro.api_clientes(tenant_id);
create index if not exists ix_api_clientes_cliente_id on plantaopro.api_clientes(cliente_id);
create index if not exists ix_api_clientes_status on plantaopro.api_clientes(status);
create index if not exists ix_api_clientes_reg_date on plantaopro.api_clientes(reg_date desc);

create table if not exists plantaopro.api_chaves (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A',
    nome varchar(180) null,
    api_key_hash varchar(128) null,
    escopos text null,
    revogada_em timestamptz null
);
alter table plantaopro.api_chaves add column if not exists tenant_id uuid null;
alter table plantaopro.api_chaves add column if not exists cliente_id uuid null;
alter table plantaopro.api_chaves add column if not exists parceiro_id uuid null;
alter table plantaopro.api_chaves add column if not exists plano_id uuid null;
alter table plantaopro.api_chaves add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.api_chaves add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.api_chaves add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.api_chaves add column if not exists reg_update timestamptz null;
alter table plantaopro.api_chaves add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_api_chaves_tenant_id on plantaopro.api_chaves(tenant_id);
create index if not exists ix_api_chaves_cliente_id on plantaopro.api_chaves(cliente_id);
create index if not exists ix_api_chaves_status on plantaopro.api_chaves(status);
create index if not exists ix_api_chaves_reg_date on plantaopro.api_chaves(reg_date desc);

create table if not exists plantaopro.api_escopos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.api_escopos add column if not exists tenant_id uuid null;
alter table plantaopro.api_escopos add column if not exists cliente_id uuid null;
alter table plantaopro.api_escopos add column if not exists parceiro_id uuid null;
alter table plantaopro.api_escopos add column if not exists plano_id uuid null;
alter table plantaopro.api_escopos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.api_escopos add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.api_escopos add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.api_escopos add column if not exists reg_update timestamptz null;
alter table plantaopro.api_escopos add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_api_escopos_tenant_id on plantaopro.api_escopos(tenant_id);
create index if not exists ix_api_escopos_cliente_id on plantaopro.api_escopos(cliente_id);
create index if not exists ix_api_escopos_status on plantaopro.api_escopos(status);
create index if not exists ix_api_escopos_reg_date on plantaopro.api_escopos(reg_date desc);

create table if not exists plantaopro.api_rate_limits (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.api_rate_limits add column if not exists tenant_id uuid null;
alter table plantaopro.api_rate_limits add column if not exists cliente_id uuid null;
alter table plantaopro.api_rate_limits add column if not exists parceiro_id uuid null;
alter table plantaopro.api_rate_limits add column if not exists plano_id uuid null;
alter table plantaopro.api_rate_limits add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.api_rate_limits add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.api_rate_limits add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.api_rate_limits add column if not exists reg_update timestamptz null;
alter table plantaopro.api_rate_limits add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_api_rate_limits_tenant_id on plantaopro.api_rate_limits(tenant_id);
create index if not exists ix_api_rate_limits_cliente_id on plantaopro.api_rate_limits(cliente_id);
create index if not exists ix_api_rate_limits_status on plantaopro.api_rate_limits(status);
create index if not exists ix_api_rate_limits_reg_date on plantaopro.api_rate_limits(reg_date desc);

create table if not exists plantaopro.api_uso (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.api_uso add column if not exists tenant_id uuid null;
alter table plantaopro.api_uso add column if not exists cliente_id uuid null;
alter table plantaopro.api_uso add column if not exists parceiro_id uuid null;
alter table plantaopro.api_uso add column if not exists plano_id uuid null;
alter table plantaopro.api_uso add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.api_uso add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.api_uso add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.api_uso add column if not exists reg_update timestamptz null;
alter table plantaopro.api_uso add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_api_uso_tenant_id on plantaopro.api_uso(tenant_id);
create index if not exists ix_api_uso_cliente_id on plantaopro.api_uso(cliente_id);
create index if not exists ix_api_uso_status on plantaopro.api_uso(status);
create index if not exists ix_api_uso_reg_date on plantaopro.api_uso(reg_date desc);

create table if not exists plantaopro.api_webhooks (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.api_webhooks add column if not exists tenant_id uuid null;
alter table plantaopro.api_webhooks add column if not exists cliente_id uuid null;
alter table plantaopro.api_webhooks add column if not exists parceiro_id uuid null;
alter table plantaopro.api_webhooks add column if not exists plano_id uuid null;
alter table plantaopro.api_webhooks add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.api_webhooks add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.api_webhooks add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.api_webhooks add column if not exists reg_update timestamptz null;
alter table plantaopro.api_webhooks add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_api_webhooks_tenant_id on plantaopro.api_webhooks(tenant_id);
create index if not exists ix_api_webhooks_cliente_id on plantaopro.api_webhooks(cliente_id);
create index if not exists ix_api_webhooks_status on plantaopro.api_webhooks(status);
create index if not exists ix_api_webhooks_reg_date on plantaopro.api_webhooks(reg_date desc);

create table if not exists plantaopro.api_webhook_eventos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.api_webhook_eventos add column if not exists tenant_id uuid null;
alter table plantaopro.api_webhook_eventos add column if not exists cliente_id uuid null;
alter table plantaopro.api_webhook_eventos add column if not exists parceiro_id uuid null;
alter table plantaopro.api_webhook_eventos add column if not exists plano_id uuid null;
alter table plantaopro.api_webhook_eventos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.api_webhook_eventos add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.api_webhook_eventos add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.api_webhook_eventos add column if not exists reg_update timestamptz null;
alter table plantaopro.api_webhook_eventos add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_api_webhook_eventos_tenant_id on plantaopro.api_webhook_eventos(tenant_id);
create index if not exists ix_api_webhook_eventos_cliente_id on plantaopro.api_webhook_eventos(cliente_id);
create index if not exists ix_api_webhook_eventos_status on plantaopro.api_webhook_eventos(status);
create index if not exists ix_api_webhook_eventos_reg_date on plantaopro.api_webhook_eventos(reg_date desc);

create table if not exists plantaopro.api_documentacao_topicos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.api_documentacao_topicos add column if not exists tenant_id uuid null;
alter table plantaopro.api_documentacao_topicos add column if not exists cliente_id uuid null;
alter table plantaopro.api_documentacao_topicos add column if not exists parceiro_id uuid null;
alter table plantaopro.api_documentacao_topicos add column if not exists plano_id uuid null;
alter table plantaopro.api_documentacao_topicos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.api_documentacao_topicos add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.api_documentacao_topicos add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.api_documentacao_topicos add column if not exists reg_update timestamptz null;
alter table plantaopro.api_documentacao_topicos add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_api_documentacao_topicos_tenant_id on plantaopro.api_documentacao_topicos(tenant_id);
create index if not exists ix_api_documentacao_topicos_cliente_id on plantaopro.api_documentacao_topicos(cliente_id);
create index if not exists ix_api_documentacao_topicos_status on plantaopro.api_documentacao_topicos(status);
create index if not exists ix_api_documentacao_topicos_reg_date on plantaopro.api_documentacao_topicos(reg_date desc);

create table if not exists plantaopro.api_documentacao_exemplos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.api_documentacao_exemplos add column if not exists tenant_id uuid null;
alter table plantaopro.api_documentacao_exemplos add column if not exists cliente_id uuid null;
alter table plantaopro.api_documentacao_exemplos add column if not exists parceiro_id uuid null;
alter table plantaopro.api_documentacao_exemplos add column if not exists plano_id uuid null;
alter table plantaopro.api_documentacao_exemplos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.api_documentacao_exemplos add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.api_documentacao_exemplos add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.api_documentacao_exemplos add column if not exists reg_update timestamptz null;
alter table plantaopro.api_documentacao_exemplos add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_api_documentacao_exemplos_tenant_id on plantaopro.api_documentacao_exemplos(tenant_id);
create index if not exists ix_api_documentacao_exemplos_cliente_id on plantaopro.api_documentacao_exemplos(cliente_id);
create index if not exists ix_api_documentacao_exemplos_status on plantaopro.api_documentacao_exemplos(status);
create index if not exists ix_api_documentacao_exemplos_reg_date on plantaopro.api_documentacao_exemplos(reg_date desc);

create table if not exists plantaopro.suporte_canais (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.suporte_canais add column if not exists tenant_id uuid null;
alter table plantaopro.suporte_canais add column if not exists cliente_id uuid null;
alter table plantaopro.suporte_canais add column if not exists parceiro_id uuid null;
alter table plantaopro.suporte_canais add column if not exists plano_id uuid null;
alter table plantaopro.suporte_canais add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.suporte_canais add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.suporte_canais add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.suporte_canais add column if not exists reg_update timestamptz null;
alter table plantaopro.suporte_canais add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_suporte_canais_tenant_id on plantaopro.suporte_canais(tenant_id);
create index if not exists ix_suporte_canais_cliente_id on plantaopro.suporte_canais(cliente_id);
create index if not exists ix_suporte_canais_status on plantaopro.suporte_canais(status);
create index if not exists ix_suporte_canais_reg_date on plantaopro.suporte_canais(reg_date desc);

create table if not exists plantaopro.suporte_chamados (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A',
    assunto varchar(220) null,
    prioridade varchar(40) null,
    resolvido_em timestamptz null
);
alter table plantaopro.suporte_chamados add column if not exists tenant_id uuid null;
alter table plantaopro.suporte_chamados add column if not exists cliente_id uuid null;
alter table plantaopro.suporte_chamados add column if not exists parceiro_id uuid null;
alter table plantaopro.suporte_chamados add column if not exists plano_id uuid null;
alter table plantaopro.suporte_chamados add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.suporte_chamados add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.suporte_chamados add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.suporte_chamados add column if not exists reg_update timestamptz null;
alter table plantaopro.suporte_chamados add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_suporte_chamados_tenant_id on plantaopro.suporte_chamados(tenant_id);
create index if not exists ix_suporte_chamados_cliente_id on plantaopro.suporte_chamados(cliente_id);
create index if not exists ix_suporte_chamados_status on plantaopro.suporte_chamados(status);
create index if not exists ix_suporte_chamados_reg_date on plantaopro.suporte_chamados(reg_date desc);

create table if not exists plantaopro.suporte_chamado_eventos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.suporte_chamado_eventos add column if not exists tenant_id uuid null;
alter table plantaopro.suporte_chamado_eventos add column if not exists cliente_id uuid null;
alter table plantaopro.suporte_chamado_eventos add column if not exists parceiro_id uuid null;
alter table plantaopro.suporte_chamado_eventos add column if not exists plano_id uuid null;
alter table plantaopro.suporte_chamado_eventos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.suporte_chamado_eventos add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.suporte_chamado_eventos add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.suporte_chamado_eventos add column if not exists reg_update timestamptz null;
alter table plantaopro.suporte_chamado_eventos add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_suporte_chamado_eventos_tenant_id on plantaopro.suporte_chamado_eventos(tenant_id);
create index if not exists ix_suporte_chamado_eventos_cliente_id on plantaopro.suporte_chamado_eventos(cliente_id);
create index if not exists ix_suporte_chamado_eventos_status on plantaopro.suporte_chamado_eventos(status);
create index if not exists ix_suporte_chamado_eventos_reg_date on plantaopro.suporte_chamado_eventos(reg_date desc);

create table if not exists plantaopro.suporte_sla (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.suporte_sla add column if not exists tenant_id uuid null;
alter table plantaopro.suporte_sla add column if not exists cliente_id uuid null;
alter table plantaopro.suporte_sla add column if not exists parceiro_id uuid null;
alter table plantaopro.suporte_sla add column if not exists plano_id uuid null;
alter table plantaopro.suporte_sla add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.suporte_sla add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.suporte_sla add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.suporte_sla add column if not exists reg_update timestamptz null;
alter table plantaopro.suporte_sla add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_suporte_sla_tenant_id on plantaopro.suporte_sla(tenant_id);
create index if not exists ix_suporte_sla_cliente_id on plantaopro.suporte_sla(cliente_id);
create index if not exists ix_suporte_sla_status on plantaopro.suporte_sla(status);
create index if not exists ix_suporte_sla_reg_date on plantaopro.suporte_sla(reg_date desc);

create table if not exists plantaopro.suporte_base_conhecimento (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.suporte_base_conhecimento add column if not exists tenant_id uuid null;
alter table plantaopro.suporte_base_conhecimento add column if not exists cliente_id uuid null;
alter table plantaopro.suporte_base_conhecimento add column if not exists parceiro_id uuid null;
alter table plantaopro.suporte_base_conhecimento add column if not exists plano_id uuid null;
alter table plantaopro.suporte_base_conhecimento add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.suporte_base_conhecimento add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.suporte_base_conhecimento add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.suporte_base_conhecimento add column if not exists reg_update timestamptz null;
alter table plantaopro.suporte_base_conhecimento add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_suporte_base_conhecimento_tenant_id on plantaopro.suporte_base_conhecimento(tenant_id);
create index if not exists ix_suporte_base_conhecimento_cliente_id on plantaopro.suporte_base_conhecimento(cliente_id);
create index if not exists ix_suporte_base_conhecimento_status on plantaopro.suporte_base_conhecimento(status);
create index if not exists ix_suporte_base_conhecimento_reg_date on plantaopro.suporte_base_conhecimento(reg_date desc);

create table if not exists plantaopro.suporte_feedbacks (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.suporte_feedbacks add column if not exists tenant_id uuid null;
alter table plantaopro.suporte_feedbacks add column if not exists cliente_id uuid null;
alter table plantaopro.suporte_feedbacks add column if not exists parceiro_id uuid null;
alter table plantaopro.suporte_feedbacks add column if not exists plano_id uuid null;
alter table plantaopro.suporte_feedbacks add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.suporte_feedbacks add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.suporte_feedbacks add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.suporte_feedbacks add column if not exists reg_update timestamptz null;
alter table plantaopro.suporte_feedbacks add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_suporte_feedbacks_tenant_id on plantaopro.suporte_feedbacks(tenant_id);
create index if not exists ix_suporte_feedbacks_cliente_id on plantaopro.suporte_feedbacks(cliente_id);
create index if not exists ix_suporte_feedbacks_status on plantaopro.suporte_feedbacks(status);
create index if not exists ix_suporte_feedbacks_reg_date on plantaopro.suporte_feedbacks(reg_date desc);

create table if not exists plantaopro.beta_programas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.beta_programas add column if not exists tenant_id uuid null;
alter table plantaopro.beta_programas add column if not exists cliente_id uuid null;
alter table plantaopro.beta_programas add column if not exists parceiro_id uuid null;
alter table plantaopro.beta_programas add column if not exists plano_id uuid null;
alter table plantaopro.beta_programas add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.beta_programas add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.beta_programas add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.beta_programas add column if not exists reg_update timestamptz null;
alter table plantaopro.beta_programas add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_beta_programas_tenant_id on plantaopro.beta_programas(tenant_id);
create index if not exists ix_beta_programas_cliente_id on plantaopro.beta_programas(cliente_id);
create index if not exists ix_beta_programas_status on plantaopro.beta_programas(status);
create index if not exists ix_beta_programas_reg_date on plantaopro.beta_programas(reg_date desc);

create table if not exists plantaopro.beta_clientes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.beta_clientes add column if not exists tenant_id uuid null;
alter table plantaopro.beta_clientes add column if not exists cliente_id uuid null;
alter table plantaopro.beta_clientes add column if not exists parceiro_id uuid null;
alter table plantaopro.beta_clientes add column if not exists plano_id uuid null;
alter table plantaopro.beta_clientes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.beta_clientes add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.beta_clientes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.beta_clientes add column if not exists reg_update timestamptz null;
alter table plantaopro.beta_clientes add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_beta_clientes_tenant_id on plantaopro.beta_clientes(tenant_id);
create index if not exists ix_beta_clientes_cliente_id on plantaopro.beta_clientes(cliente_id);
create index if not exists ix_beta_clientes_status on plantaopro.beta_clientes(status);
create index if not exists ix_beta_clientes_reg_date on plantaopro.beta_clientes(reg_date desc);

create table if not exists plantaopro.beta_feedbacks (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.beta_feedbacks add column if not exists tenant_id uuid null;
alter table plantaopro.beta_feedbacks add column if not exists cliente_id uuid null;
alter table plantaopro.beta_feedbacks add column if not exists parceiro_id uuid null;
alter table plantaopro.beta_feedbacks add column if not exists plano_id uuid null;
alter table plantaopro.beta_feedbacks add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.beta_feedbacks add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.beta_feedbacks add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.beta_feedbacks add column if not exists reg_update timestamptz null;
alter table plantaopro.beta_feedbacks add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_beta_feedbacks_tenant_id on plantaopro.beta_feedbacks(tenant_id);
create index if not exists ix_beta_feedbacks_cliente_id on plantaopro.beta_feedbacks(cliente_id);
create index if not exists ix_beta_feedbacks_status on plantaopro.beta_feedbacks(status);
create index if not exists ix_beta_feedbacks_reg_date on plantaopro.beta_feedbacks(reg_date desc);

create table if not exists plantaopro.beta_incidentes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.beta_incidentes add column if not exists tenant_id uuid null;
alter table plantaopro.beta_incidentes add column if not exists cliente_id uuid null;
alter table plantaopro.beta_incidentes add column if not exists parceiro_id uuid null;
alter table plantaopro.beta_incidentes add column if not exists plano_id uuid null;
alter table plantaopro.beta_incidentes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.beta_incidentes add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.beta_incidentes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.beta_incidentes add column if not exists reg_update timestamptz null;
alter table plantaopro.beta_incidentes add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_beta_incidentes_tenant_id on plantaopro.beta_incidentes(tenant_id);
create index if not exists ix_beta_incidentes_cliente_id on plantaopro.beta_incidentes(cliente_id);
create index if not exists ix_beta_incidentes_status on plantaopro.beta_incidentes(status);
create index if not exists ix_beta_incidentes_reg_date on plantaopro.beta_incidentes(reg_date desc);

create table if not exists plantaopro.marketing_casos_uso (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.marketing_casos_uso add column if not exists tenant_id uuid null;
alter table plantaopro.marketing_casos_uso add column if not exists cliente_id uuid null;
alter table plantaopro.marketing_casos_uso add column if not exists parceiro_id uuid null;
alter table plantaopro.marketing_casos_uso add column if not exists plano_id uuid null;
alter table plantaopro.marketing_casos_uso add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.marketing_casos_uso add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.marketing_casos_uso add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.marketing_casos_uso add column if not exists reg_update timestamptz null;
alter table plantaopro.marketing_casos_uso add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_marketing_casos_uso_tenant_id on plantaopro.marketing_casos_uso(tenant_id);
create index if not exists ix_marketing_casos_uso_cliente_id on plantaopro.marketing_casos_uso(cliente_id);
create index if not exists ix_marketing_casos_uso_status on plantaopro.marketing_casos_uso(status);
create index if not exists ix_marketing_casos_uso_reg_date on plantaopro.marketing_casos_uso(reg_date desc);

create table if not exists plantaopro.marketing_materiais (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.marketing_materiais add column if not exists tenant_id uuid null;
alter table plantaopro.marketing_materiais add column if not exists cliente_id uuid null;
alter table plantaopro.marketing_materiais add column if not exists parceiro_id uuid null;
alter table plantaopro.marketing_materiais add column if not exists plano_id uuid null;
alter table plantaopro.marketing_materiais add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.marketing_materiais add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.marketing_materiais add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.marketing_materiais add column if not exists reg_update timestamptz null;
alter table plantaopro.marketing_materiais add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_marketing_materiais_tenant_id on plantaopro.marketing_materiais(tenant_id);
create index if not exists ix_marketing_materiais_cliente_id on plantaopro.marketing_materiais(cliente_id);
create index if not exists ix_marketing_materiais_status on plantaopro.marketing_materiais(status);
create index if not exists ix_marketing_materiais_reg_date on plantaopro.marketing_materiais(reg_date desc);

create table if not exists plantaopro.campanhas_b2b (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.campanhas_b2b add column if not exists tenant_id uuid null;
alter table plantaopro.campanhas_b2b add column if not exists cliente_id uuid null;
alter table plantaopro.campanhas_b2b add column if not exists parceiro_id uuid null;
alter table plantaopro.campanhas_b2b add column if not exists plano_id uuid null;
alter table plantaopro.campanhas_b2b add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.campanhas_b2b add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.campanhas_b2b add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.campanhas_b2b add column if not exists reg_update timestamptz null;
alter table plantaopro.campanhas_b2b add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_campanhas_b2b_tenant_id on plantaopro.campanhas_b2b(tenant_id);
create index if not exists ix_campanhas_b2b_cliente_id on plantaopro.campanhas_b2b(cliente_id);
create index if not exists ix_campanhas_b2b_status on plantaopro.campanhas_b2b(status);
create index if not exists ix_campanhas_b2b_reg_date on plantaopro.campanhas_b2b(reg_date desc);

create table if not exists plantaopro.contatos_decisores (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.contatos_decisores add column if not exists tenant_id uuid null;
alter table plantaopro.contatos_decisores add column if not exists cliente_id uuid null;
alter table plantaopro.contatos_decisores add column if not exists parceiro_id uuid null;
alter table plantaopro.contatos_decisores add column if not exists plano_id uuid null;
alter table plantaopro.contatos_decisores add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.contatos_decisores add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.contatos_decisores add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.contatos_decisores add column if not exists reg_update timestamptz null;
alter table plantaopro.contatos_decisores add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_contatos_decisores_tenant_id on plantaopro.contatos_decisores(tenant_id);
create index if not exists ix_contatos_decisores_cliente_id on plantaopro.contatos_decisores(cliente_id);
create index if not exists ix_contatos_decisores_status on plantaopro.contatos_decisores(status);
create index if not exists ix_contatos_decisores_reg_date on plantaopro.contatos_decisores(reg_date desc);

create table if not exists plantaopro.telemetria_eventos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.telemetria_eventos add column if not exists tenant_id uuid null;
alter table plantaopro.telemetria_eventos add column if not exists cliente_id uuid null;
alter table plantaopro.telemetria_eventos add column if not exists parceiro_id uuid null;
alter table plantaopro.telemetria_eventos add column if not exists plano_id uuid null;
alter table plantaopro.telemetria_eventos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.telemetria_eventos add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.telemetria_eventos add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.telemetria_eventos add column if not exists reg_update timestamptz null;
alter table plantaopro.telemetria_eventos add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_telemetria_eventos_tenant_id on plantaopro.telemetria_eventos(tenant_id);
create index if not exists ix_telemetria_eventos_cliente_id on plantaopro.telemetria_eventos(cliente_id);
create index if not exists ix_telemetria_eventos_status on plantaopro.telemetria_eventos(status);
create index if not exists ix_telemetria_eventos_reg_date on plantaopro.telemetria_eventos(reg_date desc);

create table if not exists plantaopro.telemetria_metricas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.telemetria_metricas add column if not exists tenant_id uuid null;
alter table plantaopro.telemetria_metricas add column if not exists cliente_id uuid null;
alter table plantaopro.telemetria_metricas add column if not exists parceiro_id uuid null;
alter table plantaopro.telemetria_metricas add column if not exists plano_id uuid null;
alter table plantaopro.telemetria_metricas add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.telemetria_metricas add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.telemetria_metricas add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.telemetria_metricas add column if not exists reg_update timestamptz null;
alter table plantaopro.telemetria_metricas add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_telemetria_metricas_tenant_id on plantaopro.telemetria_metricas(tenant_id);
create index if not exists ix_telemetria_metricas_cliente_id on plantaopro.telemetria_metricas(cliente_id);
create index if not exists ix_telemetria_metricas_status on plantaopro.telemetria_metricas(status);
create index if not exists ix_telemetria_metricas_reg_date on plantaopro.telemetria_metricas(reg_date desc);

create table if not exists plantaopro.telemetria_alertas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.telemetria_alertas add column if not exists tenant_id uuid null;
alter table plantaopro.telemetria_alertas add column if not exists cliente_id uuid null;
alter table plantaopro.telemetria_alertas add column if not exists parceiro_id uuid null;
alter table plantaopro.telemetria_alertas add column if not exists plano_id uuid null;
alter table plantaopro.telemetria_alertas add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.telemetria_alertas add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.telemetria_alertas add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.telemetria_alertas add column if not exists reg_update timestamptz null;
alter table plantaopro.telemetria_alertas add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_telemetria_alertas_tenant_id on plantaopro.telemetria_alertas(tenant_id);
create index if not exists ix_telemetria_alertas_cliente_id on plantaopro.telemetria_alertas(cliente_id);
create index if not exists ix_telemetria_alertas_status on plantaopro.telemetria_alertas(status);
create index if not exists ix_telemetria_alertas_reg_date on plantaopro.telemetria_alertas(reg_date desc);

create table if not exists plantaopro.telemetria_healthchecks (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.telemetria_healthchecks add column if not exists tenant_id uuid null;
alter table plantaopro.telemetria_healthchecks add column if not exists cliente_id uuid null;
alter table plantaopro.telemetria_healthchecks add column if not exists parceiro_id uuid null;
alter table plantaopro.telemetria_healthchecks add column if not exists plano_id uuid null;
alter table plantaopro.telemetria_healthchecks add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.telemetria_healthchecks add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.telemetria_healthchecks add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.telemetria_healthchecks add column if not exists reg_update timestamptz null;
alter table plantaopro.telemetria_healthchecks add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_telemetria_healthchecks_tenant_id on plantaopro.telemetria_healthchecks(tenant_id);
create index if not exists ix_telemetria_healthchecks_cliente_id on plantaopro.telemetria_healthchecks(cliente_id);
create index if not exists ix_telemetria_healthchecks_status on plantaopro.telemetria_healthchecks(status);
create index if not exists ix_telemetria_healthchecks_reg_date on plantaopro.telemetria_healthchecks(reg_date desc);

create table if not exists plantaopro.telemetria_endpoint_performance (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.telemetria_endpoint_performance add column if not exists tenant_id uuid null;
alter table plantaopro.telemetria_endpoint_performance add column if not exists cliente_id uuid null;
alter table plantaopro.telemetria_endpoint_performance add column if not exists parceiro_id uuid null;
alter table plantaopro.telemetria_endpoint_performance add column if not exists plano_id uuid null;
alter table plantaopro.telemetria_endpoint_performance add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.telemetria_endpoint_performance add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.telemetria_endpoint_performance add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.telemetria_endpoint_performance add column if not exists reg_update timestamptz null;
alter table plantaopro.telemetria_endpoint_performance add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_telemetria_endpoint_performance_tenant_id on plantaopro.telemetria_endpoint_performance(tenant_id);
create index if not exists ix_telemetria_endpoint_performance_cliente_id on plantaopro.telemetria_endpoint_performance(cliente_id);
create index if not exists ix_telemetria_endpoint_performance_status on plantaopro.telemetria_endpoint_performance(status);
create index if not exists ix_telemetria_endpoint_performance_reg_date on plantaopro.telemetria_endpoint_performance(reg_date desc);

create table if not exists plantaopro.telemetria_tenant_uso (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.telemetria_tenant_uso add column if not exists tenant_id uuid null;
alter table plantaopro.telemetria_tenant_uso add column if not exists cliente_id uuid null;
alter table plantaopro.telemetria_tenant_uso add column if not exists parceiro_id uuid null;
alter table plantaopro.telemetria_tenant_uso add column if not exists plano_id uuid null;
alter table plantaopro.telemetria_tenant_uso add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.telemetria_tenant_uso add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.telemetria_tenant_uso add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.telemetria_tenant_uso add column if not exists reg_update timestamptz null;
alter table plantaopro.telemetria_tenant_uso add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_telemetria_tenant_uso_tenant_id on plantaopro.telemetria_tenant_uso(tenant_id);
create index if not exists ix_telemetria_tenant_uso_cliente_id on plantaopro.telemetria_tenant_uso(cliente_id);
create index if not exists ix_telemetria_tenant_uso_status on plantaopro.telemetria_tenant_uso(status);
create index if not exists ix_telemetria_tenant_uso_reg_date on plantaopro.telemetria_tenant_uso(reg_date desc);

create table if not exists plantaopro.telemetria_erros_criticos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.telemetria_erros_criticos add column if not exists tenant_id uuid null;
alter table plantaopro.telemetria_erros_criticos add column if not exists cliente_id uuid null;
alter table plantaopro.telemetria_erros_criticos add column if not exists parceiro_id uuid null;
alter table plantaopro.telemetria_erros_criticos add column if not exists plano_id uuid null;
alter table plantaopro.telemetria_erros_criticos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.telemetria_erros_criticos add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.telemetria_erros_criticos add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.telemetria_erros_criticos add column if not exists reg_update timestamptz null;
alter table plantaopro.telemetria_erros_criticos add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_telemetria_erros_criticos_tenant_id on plantaopro.telemetria_erros_criticos(tenant_id);
create index if not exists ix_telemetria_erros_criticos_cliente_id on plantaopro.telemetria_erros_criticos(cliente_id);
create index if not exists ix_telemetria_erros_criticos_status on plantaopro.telemetria_erros_criticos(status);
create index if not exists ix_telemetria_erros_criticos_reg_date on plantaopro.telemetria_erros_criticos(reg_date desc);

create table if not exists plantaopro.tenant_parametros_operacionais (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.tenant_parametros_operacionais add column if not exists tenant_id uuid null;
alter table plantaopro.tenant_parametros_operacionais add column if not exists cliente_id uuid null;
alter table plantaopro.tenant_parametros_operacionais add column if not exists parceiro_id uuid null;
alter table plantaopro.tenant_parametros_operacionais add column if not exists plano_id uuid null;
alter table plantaopro.tenant_parametros_operacionais add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.tenant_parametros_operacionais add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.tenant_parametros_operacionais add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.tenant_parametros_operacionais add column if not exists reg_update timestamptz null;
alter table plantaopro.tenant_parametros_operacionais add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_tenant_parametros_operacionais_tenant_id on plantaopro.tenant_parametros_operacionais(tenant_id);
create index if not exists ix_tenant_parametros_operacionais_cliente_id on plantaopro.tenant_parametros_operacionais(cliente_id);
create index if not exists ix_tenant_parametros_operacionais_status on plantaopro.tenant_parametros_operacionais(status);
create index if not exists ix_tenant_parametros_operacionais_reg_date on plantaopro.tenant_parametros_operacionais(reg_date desc);

create table if not exists plantaopro.tenant_parametros_financeiros (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.tenant_parametros_financeiros add column if not exists tenant_id uuid null;
alter table plantaopro.tenant_parametros_financeiros add column if not exists cliente_id uuid null;
alter table plantaopro.tenant_parametros_financeiros add column if not exists parceiro_id uuid null;
alter table plantaopro.tenant_parametros_financeiros add column if not exists plano_id uuid null;
alter table plantaopro.tenant_parametros_financeiros add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.tenant_parametros_financeiros add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.tenant_parametros_financeiros add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.tenant_parametros_financeiros add column if not exists reg_update timestamptz null;
alter table plantaopro.tenant_parametros_financeiros add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_tenant_parametros_financeiros_tenant_id on plantaopro.tenant_parametros_financeiros(tenant_id);
create index if not exists ix_tenant_parametros_financeiros_cliente_id on plantaopro.tenant_parametros_financeiros(cliente_id);
create index if not exists ix_tenant_parametros_financeiros_status on plantaopro.tenant_parametros_financeiros(status);
create index if not exists ix_tenant_parametros_financeiros_reg_date on plantaopro.tenant_parametros_financeiros(reg_date desc);

create table if not exists plantaopro.tenant_parametros_notificacoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.tenant_parametros_notificacoes add column if not exists tenant_id uuid null;
alter table plantaopro.tenant_parametros_notificacoes add column if not exists cliente_id uuid null;
alter table plantaopro.tenant_parametros_notificacoes add column if not exists parceiro_id uuid null;
alter table plantaopro.tenant_parametros_notificacoes add column if not exists plano_id uuid null;
alter table plantaopro.tenant_parametros_notificacoes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.tenant_parametros_notificacoes add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.tenant_parametros_notificacoes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.tenant_parametros_notificacoes add column if not exists reg_update timestamptz null;
alter table plantaopro.tenant_parametros_notificacoes add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_tenant_parametros_notificacoes_tenant_id on plantaopro.tenant_parametros_notificacoes(tenant_id);
create index if not exists ix_tenant_parametros_notificacoes_cliente_id on plantaopro.tenant_parametros_notificacoes(cliente_id);
create index if not exists ix_tenant_parametros_notificacoes_status on plantaopro.tenant_parametros_notificacoes(status);
create index if not exists ix_tenant_parametros_notificacoes_reg_date on plantaopro.tenant_parametros_notificacoes(reg_date desc);

create table if not exists plantaopro.tenant_parametros_lgpd (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.tenant_parametros_lgpd add column if not exists tenant_id uuid null;
alter table plantaopro.tenant_parametros_lgpd add column if not exists cliente_id uuid null;
alter table plantaopro.tenant_parametros_lgpd add column if not exists parceiro_id uuid null;
alter table plantaopro.tenant_parametros_lgpd add column if not exists plano_id uuid null;
alter table plantaopro.tenant_parametros_lgpd add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.tenant_parametros_lgpd add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.tenant_parametros_lgpd add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.tenant_parametros_lgpd add column if not exists reg_update timestamptz null;
alter table plantaopro.tenant_parametros_lgpd add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_tenant_parametros_lgpd_tenant_id on plantaopro.tenant_parametros_lgpd(tenant_id);
create index if not exists ix_tenant_parametros_lgpd_cliente_id on plantaopro.tenant_parametros_lgpd(cliente_id);
create index if not exists ix_tenant_parametros_lgpd_status on plantaopro.tenant_parametros_lgpd(status);
create index if not exists ix_tenant_parametros_lgpd_reg_date on plantaopro.tenant_parametros_lgpd(reg_date desc);

create table if not exists plantaopro.tenant_parametros_api (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.tenant_parametros_api add column if not exists tenant_id uuid null;
alter table plantaopro.tenant_parametros_api add column if not exists cliente_id uuid null;
alter table plantaopro.tenant_parametros_api add column if not exists parceiro_id uuid null;
alter table plantaopro.tenant_parametros_api add column if not exists plano_id uuid null;
alter table plantaopro.tenant_parametros_api add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.tenant_parametros_api add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.tenant_parametros_api add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.tenant_parametros_api add column if not exists reg_update timestamptz null;
alter table plantaopro.tenant_parametros_api add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_tenant_parametros_api_tenant_id on plantaopro.tenant_parametros_api(tenant_id);
create index if not exists ix_tenant_parametros_api_cliente_id on plantaopro.tenant_parametros_api(cliente_id);
create index if not exists ix_tenant_parametros_api_status on plantaopro.tenant_parametros_api(status);
create index if not exists ix_tenant_parametros_api_reg_date on plantaopro.tenant_parametros_api(reg_date desc);

create table if not exists plantaopro.tenant_parametros_suporte (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.tenant_parametros_suporte add column if not exists tenant_id uuid null;
alter table plantaopro.tenant_parametros_suporte add column if not exists cliente_id uuid null;
alter table plantaopro.tenant_parametros_suporte add column if not exists parceiro_id uuid null;
alter table plantaopro.tenant_parametros_suporte add column if not exists plano_id uuid null;
alter table plantaopro.tenant_parametros_suporte add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.tenant_parametros_suporte add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.tenant_parametros_suporte add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.tenant_parametros_suporte add column if not exists reg_update timestamptz null;
alter table plantaopro.tenant_parametros_suporte add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_tenant_parametros_suporte_tenant_id on plantaopro.tenant_parametros_suporte(tenant_id);
create index if not exists ix_tenant_parametros_suporte_cliente_id on plantaopro.tenant_parametros_suporte(cliente_id);
create index if not exists ix_tenant_parametros_suporte_status on plantaopro.tenant_parametros_suporte(status);
create index if not exists ix_tenant_parametros_suporte_reg_date on plantaopro.tenant_parametros_suporte(reg_date desc);

create table if not exists plantaopro.lgpd_consentimentos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.lgpd_consentimentos add column if not exists tenant_id uuid null;
alter table plantaopro.lgpd_consentimentos add column if not exists cliente_id uuid null;
alter table plantaopro.lgpd_consentimentos add column if not exists parceiro_id uuid null;
alter table plantaopro.lgpd_consentimentos add column if not exists plano_id uuid null;
alter table plantaopro.lgpd_consentimentos add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.lgpd_consentimentos add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.lgpd_consentimentos add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.lgpd_consentimentos add column if not exists reg_update timestamptz null;
alter table plantaopro.lgpd_consentimentos add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_lgpd_consentimentos_tenant_id on plantaopro.lgpd_consentimentos(tenant_id);
create index if not exists ix_lgpd_consentimentos_cliente_id on plantaopro.lgpd_consentimentos(cliente_id);
create index if not exists ix_lgpd_consentimentos_status on plantaopro.lgpd_consentimentos(status);
create index if not exists ix_lgpd_consentimentos_reg_date on plantaopro.lgpd_consentimentos(reg_date desc);

create table if not exists plantaopro.lgpd_politicas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.lgpd_politicas add column if not exists tenant_id uuid null;
alter table plantaopro.lgpd_politicas add column if not exists cliente_id uuid null;
alter table plantaopro.lgpd_politicas add column if not exists parceiro_id uuid null;
alter table plantaopro.lgpd_politicas add column if not exists plano_id uuid null;
alter table plantaopro.lgpd_politicas add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.lgpd_politicas add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.lgpd_politicas add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.lgpd_politicas add column if not exists reg_update timestamptz null;
alter table plantaopro.lgpd_politicas add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_lgpd_politicas_tenant_id on plantaopro.lgpd_politicas(tenant_id);
create index if not exists ix_lgpd_politicas_cliente_id on plantaopro.lgpd_politicas(cliente_id);
create index if not exists ix_lgpd_politicas_status on plantaopro.lgpd_politicas(status);
create index if not exists ix_lgpd_politicas_reg_date on plantaopro.lgpd_politicas(reg_date desc);

create table if not exists plantaopro.lgpd_solicitacoes_titular (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.lgpd_solicitacoes_titular add column if not exists tenant_id uuid null;
alter table plantaopro.lgpd_solicitacoes_titular add column if not exists cliente_id uuid null;
alter table plantaopro.lgpd_solicitacoes_titular add column if not exists parceiro_id uuid null;
alter table plantaopro.lgpd_solicitacoes_titular add column if not exists plano_id uuid null;
alter table plantaopro.lgpd_solicitacoes_titular add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.lgpd_solicitacoes_titular add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.lgpd_solicitacoes_titular add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.lgpd_solicitacoes_titular add column if not exists reg_update timestamptz null;
alter table plantaopro.lgpd_solicitacoes_titular add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_lgpd_solicitacoes_titular_tenant_id on plantaopro.lgpd_solicitacoes_titular(tenant_id);
create index if not exists ix_lgpd_solicitacoes_titular_cliente_id on plantaopro.lgpd_solicitacoes_titular(cliente_id);
create index if not exists ix_lgpd_solicitacoes_titular_status on plantaopro.lgpd_solicitacoes_titular(status);
create index if not exists ix_lgpd_solicitacoes_titular_reg_date on plantaopro.lgpd_solicitacoes_titular(reg_date desc);

create table if not exists plantaopro.lgpd_eventos_privacidade (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.lgpd_eventos_privacidade add column if not exists tenant_id uuid null;
alter table plantaopro.lgpd_eventos_privacidade add column if not exists cliente_id uuid null;
alter table plantaopro.lgpd_eventos_privacidade add column if not exists parceiro_id uuid null;
alter table plantaopro.lgpd_eventos_privacidade add column if not exists plano_id uuid null;
alter table plantaopro.lgpd_eventos_privacidade add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.lgpd_eventos_privacidade add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.lgpd_eventos_privacidade add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.lgpd_eventos_privacidade add column if not exists reg_update timestamptz null;
alter table plantaopro.lgpd_eventos_privacidade add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_lgpd_eventos_privacidade_tenant_id on plantaopro.lgpd_eventos_privacidade(tenant_id);
create index if not exists ix_lgpd_eventos_privacidade_cliente_id on plantaopro.lgpd_eventos_privacidade(cliente_id);
create index if not exists ix_lgpd_eventos_privacidade_status on plantaopro.lgpd_eventos_privacidade(status);
create index if not exists ix_lgpd_eventos_privacidade_reg_date on plantaopro.lgpd_eventos_privacidade(reg_date desc);

create table if not exists plantaopro.medico_disponibilidades (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.medico_disponibilidades add column if not exists tenant_id uuid null;
alter table plantaopro.medico_disponibilidades add column if not exists cliente_id uuid null;
alter table plantaopro.medico_disponibilidades add column if not exists parceiro_id uuid null;
alter table plantaopro.medico_disponibilidades add column if not exists plano_id uuid null;
alter table plantaopro.medico_disponibilidades add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.medico_disponibilidades add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.medico_disponibilidades add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.medico_disponibilidades add column if not exists reg_update timestamptz null;
alter table plantaopro.medico_disponibilidades add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_medico_disponibilidades_tenant_id on plantaopro.medico_disponibilidades(tenant_id);
create index if not exists ix_medico_disponibilidades_cliente_id on plantaopro.medico_disponibilidades(cliente_id);
create index if not exists ix_medico_disponibilidades_status on plantaopro.medico_disponibilidades(status);
create index if not exists ix_medico_disponibilidades_reg_date on plantaopro.medico_disponibilidades(reg_date desc);

create table if not exists plantaopro.plantao_substituicoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.plantao_substituicoes add column if not exists tenant_id uuid null;
alter table plantaopro.plantao_substituicoes add column if not exists cliente_id uuid null;
alter table plantaopro.plantao_substituicoes add column if not exists parceiro_id uuid null;
alter table plantaopro.plantao_substituicoes add column if not exists plano_id uuid null;
alter table plantaopro.plantao_substituicoes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.plantao_substituicoes add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.plantao_substituicoes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.plantao_substituicoes add column if not exists reg_update timestamptz null;
alter table plantaopro.plantao_substituicoes add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_plantao_substituicoes_tenant_id on plantaopro.plantao_substituicoes(tenant_id);
create index if not exists ix_plantao_substituicoes_cliente_id on plantaopro.plantao_substituicoes(cliente_id);
create index if not exists ix_plantao_substituicoes_status on plantaopro.plantao_substituicoes(status);
create index if not exists ix_plantao_substituicoes_reg_date on plantaopro.plantao_substituicoes(reg_date desc);

create table if not exists plantaopro.relatorio_exportacoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.relatorio_exportacoes add column if not exists tenant_id uuid null;
alter table plantaopro.relatorio_exportacoes add column if not exists cliente_id uuid null;
alter table plantaopro.relatorio_exportacoes add column if not exists parceiro_id uuid null;
alter table plantaopro.relatorio_exportacoes add column if not exists plano_id uuid null;
alter table plantaopro.relatorio_exportacoes add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.relatorio_exportacoes add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.relatorio_exportacoes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.relatorio_exportacoes add column if not exists reg_update timestamptz null;
alter table plantaopro.relatorio_exportacoes add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_relatorio_exportacoes_tenant_id on plantaopro.relatorio_exportacoes(tenant_id);
create index if not exists ix_relatorio_exportacoes_cliente_id on plantaopro.relatorio_exportacoes(cliente_id);
create index if not exists ix_relatorio_exportacoes_status on plantaopro.relatorio_exportacoes(status);
create index if not exists ix_relatorio_exportacoes_reg_date on plantaopro.relatorio_exportacoes(reg_date desc);

create table if not exists plantaopro.notificacoes_avancadas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    parceiro_id uuid null,
    plano_id uuid null,
    codigo varchar(120) null,
    titulo varchar(220) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    metadata jsonb not null default '{}'::jsonb,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);
alter table plantaopro.notificacoes_avancadas add column if not exists tenant_id uuid null;
alter table plantaopro.notificacoes_avancadas add column if not exists cliente_id uuid null;
alter table plantaopro.notificacoes_avancadas add column if not exists parceiro_id uuid null;
alter table plantaopro.notificacoes_avancadas add column if not exists plano_id uuid null;
alter table plantaopro.notificacoes_avancadas add column if not exists status varchar(40) not null default 'ATIVO';
alter table plantaopro.notificacoes_avancadas add column if not exists metadata jsonb not null default '{}'::jsonb;
alter table plantaopro.notificacoes_avancadas add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.notificacoes_avancadas add column if not exists reg_update timestamptz null;
alter table plantaopro.notificacoes_avancadas add column if not exists reg_status char(1) not null default 'A';
create index if not exists ix_notificacoes_avancadas_tenant_id on plantaopro.notificacoes_avancadas(tenant_id);
create index if not exists ix_notificacoes_avancadas_cliente_id on plantaopro.notificacoes_avancadas(cliente_id);
create index if not exists ix_notificacoes_avancadas_status on plantaopro.notificacoes_avancadas(status);
create index if not exists ix_notificacoes_avancadas_reg_date on plantaopro.notificacoes_avancadas(reg_date desc);

alter table plantaopro.tenants add column if not exists dominio_customizado varchar(255) null;
alter table plantaopro.tenants add column if not exists subdominio varchar(160) null;
alter table plantaopro.tenant_dominios add column if not exists dominio_customizado varchar(255) null;
alter table plantaopro.tenant_dominios add column if not exists subdominio varchar(160) null;
alter table plantaopro.white_label_dominios add column if not exists dominio_customizado varchar(255) null;
alter table plantaopro.white_label_dominios add column if not exists subdominio varchar(160) null;
create index if not exists ix_tenants_dominio on plantaopro.tenants(dominio_customizado) where dominio_customizado is not null;
create index if not exists ix_tenants_subdominio on plantaopro.tenants(subdominio) where subdominio is not null;
create index if not exists ix_tenant_dominios_dominio on plantaopro.tenant_dominios(dominio_customizado) where dominio_customizado is not null;
create index if not exists ix_tenant_dominios_subdominio on plantaopro.tenant_dominios(subdominio) where subdominio is not null;
create index if not exists ix_white_label_dominios_dominio on plantaopro.white_label_dominios(dominio_customizado) where dominio_customizado is not null;
create index if not exists ix_white_label_dominios_subdominio on plantaopro.white_label_dominios(subdominio) where subdominio is not null;
create index if not exists ix_api_chaves_hash on plantaopro.api_chaves(api_key_hash) where api_key_hash is not null;
create index if not exists ix_planos_slug on plantaopro.planos(slug) where slug is not null;
create index if not exists ix_parceiro_tenants_parceiro on plantaopro.parceiro_tenants(parceiro_id);

do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'ck_planos_valor_mensal_nao_negativo') then
        alter table plantaopro.planos add constraint ck_planos_valor_mensal_nao_negativo check (valor_mensal >= 0);
    end if;
    if not exists (select 1 from pg_constraint where conname = 'ck_api_rate_limits_limites_positivos') then
        alter table plantaopro.api_rate_limits add constraint ck_api_rate_limits_limites_positivos check ((metadata ? 'limite') = false or true);
    end if;
end $$;


insert into plantaopro.planos(id, slug, nome, descricao, permite_white_label, permite_api, valor_mensal, status, reg_date, reg_status)
values
    (gen_random_uuid(), 'essencial', 'Essencial', 'Até 20 médicos, 2 unidades e 100 plantões/mês.', false, false, 299, 'ATIVO', now(), 'A'),
    (gen_random_uuid(), 'profissional', 'Profissional', 'API limitada, relatórios avançados e white label básico.', true, true, 899, 'ATIVO', now(), 'A'),
    (gen_random_uuid(), 'enterprise-white-label', 'Enterprise White Label', 'White label completo, domínio customizado, API completa, BI e SLA.', true, true, 2490, 'ATIVO', now(), 'A'),
    (gen_random_uuid(), 'revendedor', 'Revendedor', 'Console do parceiro, múltiplos tenants, comissão e repasses.', true, true, 3990, 'ATIVO', now(), 'A'),
    (gen_random_uuid(), 'custom', 'Custom', 'Contrato sob proposta com integrações e SLA customizado.', true, true, 0, 'ATIVO', now(), 'A')
on conflict do nothing;

alter table plantaopro.tenant_dominios add column if not exists dominio_customizado varchar(255) null;
alter table plantaopro.tenant_dominios add column if not exists subdominio varchar(160) null;
alter table plantaopro.white_label_dominios add column if not exists dominio_customizado varchar(255) null;
alter table plantaopro.white_label_dominios add column if not exists subdominio varchar(160) null;
create index if not exists ix_tenant_dominios_dominio_customizado on plantaopro.tenant_dominios(dominio_customizado) where dominio_customizado is not null;
create index if not exists ix_tenant_dominios_subdominio_valor on plantaopro.tenant_dominios(subdominio) where subdominio is not null;
create index if not exists ix_white_label_dominios_dominio_customizado on plantaopro.white_label_dominios(dominio_customizado) where dominio_customizado is not null;
create index if not exists ix_white_label_dominios_subdominio_valor on plantaopro.white_label_dominios(subdominio) where subdominio is not null;
