-- PlantãoPro Saúde 360 - módulos clínicos e assistenciais
create schema if not exists plantaopro;
create extension if not exists pgcrypto;
create extension if not exists unaccent;

create table if not exists plantaopro.pacientes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_pacientes_tenant_id on plantaopro.pacientes (tenant_id);
create index if not exists ix_pacientes_cliente_id on plantaopro.pacientes (cliente_id);
create index if not exists ix_pacientes_paciente_id on plantaopro.pacientes (paciente_id);
create index if not exists ix_pacientes_medico_id on plantaopro.pacientes (medico_id);
create index if not exists ix_pacientes_agendamento_id on plantaopro.pacientes (agendamento_id);
create index if not exists ix_pacientes_consulta_id on plantaopro.pacientes (consulta_id);
create index if not exists ix_pacientes_status on plantaopro.pacientes (status);
create index if not exists ix_pacientes_reg_date on plantaopro.pacientes (reg_date);
create index if not exists ix_pacientes_metadata_gin on plantaopro.pacientes using gin (metadata);

create table if not exists plantaopro.paciente_contatos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_paciente_contatos_tenant_id on plantaopro.paciente_contatos (tenant_id);
create index if not exists ix_paciente_contatos_cliente_id on plantaopro.paciente_contatos (cliente_id);
create index if not exists ix_paciente_contatos_paciente_id on plantaopro.paciente_contatos (paciente_id);
create index if not exists ix_paciente_contatos_medico_id on plantaopro.paciente_contatos (medico_id);
create index if not exists ix_paciente_contatos_agendamento_id on plantaopro.paciente_contatos (agendamento_id);
create index if not exists ix_paciente_contatos_consulta_id on plantaopro.paciente_contatos (consulta_id);
create index if not exists ix_paciente_contatos_status on plantaopro.paciente_contatos (status);
create index if not exists ix_paciente_contatos_reg_date on plantaopro.paciente_contatos (reg_date);
create index if not exists ix_paciente_contatos_metadata_gin on plantaopro.paciente_contatos using gin (metadata);

create table if not exists plantaopro.paciente_enderecos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_paciente_enderecos_tenant_id on plantaopro.paciente_enderecos (tenant_id);
create index if not exists ix_paciente_enderecos_cliente_id on plantaopro.paciente_enderecos (cliente_id);
create index if not exists ix_paciente_enderecos_paciente_id on plantaopro.paciente_enderecos (paciente_id);
create index if not exists ix_paciente_enderecos_medico_id on plantaopro.paciente_enderecos (medico_id);
create index if not exists ix_paciente_enderecos_agendamento_id on plantaopro.paciente_enderecos (agendamento_id);
create index if not exists ix_paciente_enderecos_consulta_id on plantaopro.paciente_enderecos (consulta_id);
create index if not exists ix_paciente_enderecos_status on plantaopro.paciente_enderecos (status);
create index if not exists ix_paciente_enderecos_reg_date on plantaopro.paciente_enderecos (reg_date);
create index if not exists ix_paciente_enderecos_metadata_gin on plantaopro.paciente_enderecos using gin (metadata);

create table if not exists plantaopro.paciente_documentos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_paciente_documentos_tenant_id on plantaopro.paciente_documentos (tenant_id);
create index if not exists ix_paciente_documentos_cliente_id on plantaopro.paciente_documentos (cliente_id);
create index if not exists ix_paciente_documentos_paciente_id on plantaopro.paciente_documentos (paciente_id);
create index if not exists ix_paciente_documentos_medico_id on plantaopro.paciente_documentos (medico_id);
create index if not exists ix_paciente_documentos_agendamento_id on plantaopro.paciente_documentos (agendamento_id);
create index if not exists ix_paciente_documentos_consulta_id on plantaopro.paciente_documentos (consulta_id);
create index if not exists ix_paciente_documentos_status on plantaopro.paciente_documentos (status);
create index if not exists ix_paciente_documentos_reg_date on plantaopro.paciente_documentos (reg_date);
create index if not exists ix_paciente_documentos_metadata_gin on plantaopro.paciente_documentos using gin (metadata);

create table if not exists plantaopro.paciente_convenios (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_paciente_convenios_tenant_id on plantaopro.paciente_convenios (tenant_id);
create index if not exists ix_paciente_convenios_cliente_id on plantaopro.paciente_convenios (cliente_id);
create index if not exists ix_paciente_convenios_paciente_id on plantaopro.paciente_convenios (paciente_id);
create index if not exists ix_paciente_convenios_medico_id on plantaopro.paciente_convenios (medico_id);
create index if not exists ix_paciente_convenios_agendamento_id on plantaopro.paciente_convenios (agendamento_id);
create index if not exists ix_paciente_convenios_consulta_id on plantaopro.paciente_convenios (consulta_id);
create index if not exists ix_paciente_convenios_status on plantaopro.paciente_convenios (status);
create index if not exists ix_paciente_convenios_reg_date on plantaopro.paciente_convenios (reg_date);
create index if not exists ix_paciente_convenios_metadata_gin on plantaopro.paciente_convenios using gin (metadata);

create table if not exists plantaopro.paciente_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_paciente_historico_tenant_id on plantaopro.paciente_historico (tenant_id);
create index if not exists ix_paciente_historico_cliente_id on plantaopro.paciente_historico (cliente_id);
create index if not exists ix_paciente_historico_paciente_id on plantaopro.paciente_historico (paciente_id);
create index if not exists ix_paciente_historico_medico_id on plantaopro.paciente_historico (medico_id);
create index if not exists ix_paciente_historico_agendamento_id on plantaopro.paciente_historico (agendamento_id);
create index if not exists ix_paciente_historico_consulta_id on plantaopro.paciente_historico (consulta_id);
create index if not exists ix_paciente_historico_status on plantaopro.paciente_historico (status);
create index if not exists ix_paciente_historico_reg_date on plantaopro.paciente_historico (reg_date);
create index if not exists ix_paciente_historico_metadata_gin on plantaopro.paciente_historico using gin (metadata);

create table if not exists plantaopro.paineis_chamada (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_paineis_chamada_tenant_id on plantaopro.paineis_chamada (tenant_id);
create index if not exists ix_paineis_chamada_cliente_id on plantaopro.paineis_chamada (cliente_id);
create index if not exists ix_paineis_chamada_paciente_id on plantaopro.paineis_chamada (paciente_id);
create index if not exists ix_paineis_chamada_medico_id on plantaopro.paineis_chamada (medico_id);
create index if not exists ix_paineis_chamada_agendamento_id on plantaopro.paineis_chamada (agendamento_id);
create index if not exists ix_paineis_chamada_consulta_id on plantaopro.paineis_chamada (consulta_id);
create index if not exists ix_paineis_chamada_status on plantaopro.paineis_chamada (status);
create index if not exists ix_paineis_chamada_reg_date on plantaopro.paineis_chamada (reg_date);
create index if not exists ix_paineis_chamada_metadata_gin on plantaopro.paineis_chamada using gin (metadata);

create table if not exists plantaopro.painel_chamada_configuracoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_painel_chamada_configuracoes_tenant_id on plantaopro.painel_chamada_configuracoes (tenant_id);
create index if not exists ix_painel_chamada_configuracoes_cliente_id on plantaopro.painel_chamada_configuracoes (cliente_id);
create index if not exists ix_painel_chamada_configuracoes_paciente_id on plantaopro.painel_chamada_configuracoes (paciente_id);
create index if not exists ix_painel_chamada_configuracoes_medico_id on plantaopro.painel_chamada_configuracoes (medico_id);
create index if not exists ix_painel_chamada_configuracoes_agendamento_id on plantaopro.painel_chamada_configuracoes (agendamento_id);
create index if not exists ix_painel_chamada_configuracoes_consulta_id on plantaopro.painel_chamada_configuracoes (consulta_id);
create index if not exists ix_painel_chamada_configuracoes_status on plantaopro.painel_chamada_configuracoes (status);
create index if not exists ix_painel_chamada_configuracoes_reg_date on plantaopro.painel_chamada_configuracoes (reg_date);
create index if not exists ix_painel_chamada_configuracoes_metadata_gin on plantaopro.painel_chamada_configuracoes using gin (metadata);

create table if not exists plantaopro.painel_chamada_setores (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_painel_chamada_setores_tenant_id on plantaopro.painel_chamada_setores (tenant_id);
create index if not exists ix_painel_chamada_setores_cliente_id on plantaopro.painel_chamada_setores (cliente_id);
create index if not exists ix_painel_chamada_setores_paciente_id on plantaopro.painel_chamada_setores (paciente_id);
create index if not exists ix_painel_chamada_setores_medico_id on plantaopro.painel_chamada_setores (medico_id);
create index if not exists ix_painel_chamada_setores_agendamento_id on plantaopro.painel_chamada_setores (agendamento_id);
create index if not exists ix_painel_chamada_setores_consulta_id on plantaopro.painel_chamada_setores (consulta_id);
create index if not exists ix_painel_chamada_setores_status on plantaopro.painel_chamada_setores (status);
create index if not exists ix_painel_chamada_setores_reg_date on plantaopro.painel_chamada_setores (reg_date);
create index if not exists ix_painel_chamada_setores_metadata_gin on plantaopro.painel_chamada_setores using gin (metadata);

create table if not exists plantaopro.painel_chamada_salas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_painel_chamada_salas_tenant_id on plantaopro.painel_chamada_salas (tenant_id);
create index if not exists ix_painel_chamada_salas_cliente_id on plantaopro.painel_chamada_salas (cliente_id);
create index if not exists ix_painel_chamada_salas_paciente_id on plantaopro.painel_chamada_salas (paciente_id);
create index if not exists ix_painel_chamada_salas_medico_id on plantaopro.painel_chamada_salas (medico_id);
create index if not exists ix_painel_chamada_salas_agendamento_id on plantaopro.painel_chamada_salas (agendamento_id);
create index if not exists ix_painel_chamada_salas_consulta_id on plantaopro.painel_chamada_salas (consulta_id);
create index if not exists ix_painel_chamada_salas_status on plantaopro.painel_chamada_salas (status);
create index if not exists ix_painel_chamada_salas_reg_date on plantaopro.painel_chamada_salas (reg_date);
create index if not exists ix_painel_chamada_salas_metadata_gin on plantaopro.painel_chamada_salas using gin (metadata);

create table if not exists plantaopro.painel_chamada_guiches (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_painel_chamada_guiches_tenant_id on plantaopro.painel_chamada_guiches (tenant_id);
create index if not exists ix_painel_chamada_guiches_cliente_id on plantaopro.painel_chamada_guiches (cliente_id);
create index if not exists ix_painel_chamada_guiches_paciente_id on plantaopro.painel_chamada_guiches (paciente_id);
create index if not exists ix_painel_chamada_guiches_medico_id on plantaopro.painel_chamada_guiches (medico_id);
create index if not exists ix_painel_chamada_guiches_agendamento_id on plantaopro.painel_chamada_guiches (agendamento_id);
create index if not exists ix_painel_chamada_guiches_consulta_id on plantaopro.painel_chamada_guiches (consulta_id);
create index if not exists ix_painel_chamada_guiches_status on plantaopro.painel_chamada_guiches (status);
create index if not exists ix_painel_chamada_guiches_reg_date on plantaopro.painel_chamada_guiches (reg_date);
create index if not exists ix_painel_chamada_guiches_metadata_gin on plantaopro.painel_chamada_guiches using gin (metadata);

create table if not exists plantaopro.painel_chamada_fila (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_painel_chamada_fila_tenant_id on plantaopro.painel_chamada_fila (tenant_id);
create index if not exists ix_painel_chamada_fila_cliente_id on plantaopro.painel_chamada_fila (cliente_id);
create index if not exists ix_painel_chamada_fila_paciente_id on plantaopro.painel_chamada_fila (paciente_id);
create index if not exists ix_painel_chamada_fila_medico_id on plantaopro.painel_chamada_fila (medico_id);
create index if not exists ix_painel_chamada_fila_agendamento_id on plantaopro.painel_chamada_fila (agendamento_id);
create index if not exists ix_painel_chamada_fila_consulta_id on plantaopro.painel_chamada_fila (consulta_id);
create index if not exists ix_painel_chamada_fila_status on plantaopro.painel_chamada_fila (status);
create index if not exists ix_painel_chamada_fila_reg_date on plantaopro.painel_chamada_fila (reg_date);
create index if not exists ix_painel_chamada_fila_metadata_gin on plantaopro.painel_chamada_fila using gin (metadata);

create table if not exists plantaopro.painel_chamada_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_painel_chamada_historico_tenant_id on plantaopro.painel_chamada_historico (tenant_id);
create index if not exists ix_painel_chamada_historico_cliente_id on plantaopro.painel_chamada_historico (cliente_id);
create index if not exists ix_painel_chamada_historico_paciente_id on plantaopro.painel_chamada_historico (paciente_id);
create index if not exists ix_painel_chamada_historico_medico_id on plantaopro.painel_chamada_historico (medico_id);
create index if not exists ix_painel_chamada_historico_agendamento_id on plantaopro.painel_chamada_historico (agendamento_id);
create index if not exists ix_painel_chamada_historico_consulta_id on plantaopro.painel_chamada_historico (consulta_id);
create index if not exists ix_painel_chamada_historico_status on plantaopro.painel_chamada_historico (status);
create index if not exists ix_painel_chamada_historico_reg_date on plantaopro.painel_chamada_historico (reg_date);
create index if not exists ix_painel_chamada_historico_metadata_gin on plantaopro.painel_chamada_historico using gin (metadata);

create table if not exists plantaopro.agendamentos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_agendamentos_tenant_id on plantaopro.agendamentos (tenant_id);
create index if not exists ix_agendamentos_cliente_id on plantaopro.agendamentos (cliente_id);
create index if not exists ix_agendamentos_paciente_id on plantaopro.agendamentos (paciente_id);
create index if not exists ix_agendamentos_medico_id on plantaopro.agendamentos (medico_id);
create index if not exists ix_agendamentos_agendamento_id on plantaopro.agendamentos (agendamento_id);
create index if not exists ix_agendamentos_consulta_id on plantaopro.agendamentos (consulta_id);
create index if not exists ix_agendamentos_status on plantaopro.agendamentos (status);
create index if not exists ix_agendamentos_reg_date on plantaopro.agendamentos (reg_date);
create index if not exists ix_agendamentos_metadata_gin on plantaopro.agendamentos using gin (metadata);

create table if not exists plantaopro.agendamento_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_agendamento_historico_tenant_id on plantaopro.agendamento_historico (tenant_id);
create index if not exists ix_agendamento_historico_cliente_id on plantaopro.agendamento_historico (cliente_id);
create index if not exists ix_agendamento_historico_paciente_id on plantaopro.agendamento_historico (paciente_id);
create index if not exists ix_agendamento_historico_medico_id on plantaopro.agendamento_historico (medico_id);
create index if not exists ix_agendamento_historico_agendamento_id on plantaopro.agendamento_historico (agendamento_id);
create index if not exists ix_agendamento_historico_consulta_id on plantaopro.agendamento_historico (consulta_id);
create index if not exists ix_agendamento_historico_status on plantaopro.agendamento_historico (status);
create index if not exists ix_agendamento_historico_reg_date on plantaopro.agendamento_historico (reg_date);
create index if not exists ix_agendamento_historico_metadata_gin on plantaopro.agendamento_historico using gin (metadata);

create table if not exists plantaopro.agendamento_bloqueios (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_agendamento_bloqueios_tenant_id on plantaopro.agendamento_bloqueios (tenant_id);
create index if not exists ix_agendamento_bloqueios_cliente_id on plantaopro.agendamento_bloqueios (cliente_id);
create index if not exists ix_agendamento_bloqueios_paciente_id on plantaopro.agendamento_bloqueios (paciente_id);
create index if not exists ix_agendamento_bloqueios_medico_id on plantaopro.agendamento_bloqueios (medico_id);
create index if not exists ix_agendamento_bloqueios_agendamento_id on plantaopro.agendamento_bloqueios (agendamento_id);
create index if not exists ix_agendamento_bloqueios_consulta_id on plantaopro.agendamento_bloqueios (consulta_id);
create index if not exists ix_agendamento_bloqueios_status on plantaopro.agendamento_bloqueios (status);
create index if not exists ix_agendamento_bloqueios_reg_date on plantaopro.agendamento_bloqueios (reg_date);
create index if not exists ix_agendamento_bloqueios_metadata_gin on plantaopro.agendamento_bloqueios using gin (metadata);

create table if not exists plantaopro.agendamento_cancelamentos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_agendamento_cancelamentos_tenant_id on plantaopro.agendamento_cancelamentos (tenant_id);
create index if not exists ix_agendamento_cancelamentos_cliente_id on plantaopro.agendamento_cancelamentos (cliente_id);
create index if not exists ix_agendamento_cancelamentos_paciente_id on plantaopro.agendamento_cancelamentos (paciente_id);
create index if not exists ix_agendamento_cancelamentos_medico_id on plantaopro.agendamento_cancelamentos (medico_id);
create index if not exists ix_agendamento_cancelamentos_agendamento_id on plantaopro.agendamento_cancelamentos (agendamento_id);
create index if not exists ix_agendamento_cancelamentos_consulta_id on plantaopro.agendamento_cancelamentos (consulta_id);
create index if not exists ix_agendamento_cancelamentos_status on plantaopro.agendamento_cancelamentos (status);
create index if not exists ix_agendamento_cancelamentos_reg_date on plantaopro.agendamento_cancelamentos (reg_date);
create index if not exists ix_agendamento_cancelamentos_metadata_gin on plantaopro.agendamento_cancelamentos using gin (metadata);

create table if not exists plantaopro.agendamento_checkins (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_agendamento_checkins_tenant_id on plantaopro.agendamento_checkins (tenant_id);
create index if not exists ix_agendamento_checkins_cliente_id on plantaopro.agendamento_checkins (cliente_id);
create index if not exists ix_agendamento_checkins_paciente_id on plantaopro.agendamento_checkins (paciente_id);
create index if not exists ix_agendamento_checkins_medico_id on plantaopro.agendamento_checkins (medico_id);
create index if not exists ix_agendamento_checkins_agendamento_id on plantaopro.agendamento_checkins (agendamento_id);
create index if not exists ix_agendamento_checkins_consulta_id on plantaopro.agendamento_checkins (consulta_id);
create index if not exists ix_agendamento_checkins_status on plantaopro.agendamento_checkins (status);
create index if not exists ix_agendamento_checkins_reg_date on plantaopro.agendamento_checkins (reg_date);
create index if not exists ix_agendamento_checkins_metadata_gin on plantaopro.agendamento_checkins using gin (metadata);

create table if not exists plantaopro.triagens (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_triagens_tenant_id on plantaopro.triagens (tenant_id);
create index if not exists ix_triagens_cliente_id on plantaopro.triagens (cliente_id);
create index if not exists ix_triagens_paciente_id on plantaopro.triagens (paciente_id);
create index if not exists ix_triagens_medico_id on plantaopro.triagens (medico_id);
create index if not exists ix_triagens_agendamento_id on plantaopro.triagens (agendamento_id);
create index if not exists ix_triagens_consulta_id on plantaopro.triagens (consulta_id);
create index if not exists ix_triagens_status on plantaopro.triagens (status);
create index if not exists ix_triagens_reg_date on plantaopro.triagens (reg_date);
create index if not exists ix_triagens_metadata_gin on plantaopro.triagens using gin (metadata);

create table if not exists plantaopro.triagem_sinais_vitais (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_triagem_sinais_vitais_tenant_id on plantaopro.triagem_sinais_vitais (tenant_id);
create index if not exists ix_triagem_sinais_vitais_cliente_id on plantaopro.triagem_sinais_vitais (cliente_id);
create index if not exists ix_triagem_sinais_vitais_paciente_id on plantaopro.triagem_sinais_vitais (paciente_id);
create index if not exists ix_triagem_sinais_vitais_medico_id on plantaopro.triagem_sinais_vitais (medico_id);
create index if not exists ix_triagem_sinais_vitais_agendamento_id on plantaopro.triagem_sinais_vitais (agendamento_id);
create index if not exists ix_triagem_sinais_vitais_consulta_id on plantaopro.triagem_sinais_vitais (consulta_id);
create index if not exists ix_triagem_sinais_vitais_status on plantaopro.triagem_sinais_vitais (status);
create index if not exists ix_triagem_sinais_vitais_reg_date on plantaopro.triagem_sinais_vitais (reg_date);
create index if not exists ix_triagem_sinais_vitais_metadata_gin on plantaopro.triagem_sinais_vitais using gin (metadata);

create table if not exists plantaopro.triagem_classificacoes_risco (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_triagem_classificacoes_risco_tenant_id on plantaopro.triagem_classificacoes_risco (tenant_id);
create index if not exists ix_triagem_classificacoes_risco_cliente_id on plantaopro.triagem_classificacoes_risco (cliente_id);
create index if not exists ix_triagem_classificacoes_risco_paciente_id on plantaopro.triagem_classificacoes_risco (paciente_id);
create index if not exists ix_triagem_classificacoes_risco_medico_id on plantaopro.triagem_classificacoes_risco (medico_id);
create index if not exists ix_triagem_classificacoes_risco_agendamento_id on plantaopro.triagem_classificacoes_risco (agendamento_id);
create index if not exists ix_triagem_classificacoes_risco_consulta_id on plantaopro.triagem_classificacoes_risco (consulta_id);
create index if not exists ix_triagem_classificacoes_risco_status on plantaopro.triagem_classificacoes_risco (status);
create index if not exists ix_triagem_classificacoes_risco_reg_date on plantaopro.triagem_classificacoes_risco (reg_date);
create index if not exists ix_triagem_classificacoes_risco_metadata_gin on plantaopro.triagem_classificacoes_risco using gin (metadata);

create table if not exists plantaopro.triagem_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_triagem_historico_tenant_id on plantaopro.triagem_historico (tenant_id);
create index if not exists ix_triagem_historico_cliente_id on plantaopro.triagem_historico (cliente_id);
create index if not exists ix_triagem_historico_paciente_id on plantaopro.triagem_historico (paciente_id);
create index if not exists ix_triagem_historico_medico_id on plantaopro.triagem_historico (medico_id);
create index if not exists ix_triagem_historico_agendamento_id on plantaopro.triagem_historico (agendamento_id);
create index if not exists ix_triagem_historico_consulta_id on plantaopro.triagem_historico (consulta_id);
create index if not exists ix_triagem_historico_status on plantaopro.triagem_historico (status);
create index if not exists ix_triagem_historico_reg_date on plantaopro.triagem_historico (reg_date);
create index if not exists ix_triagem_historico_metadata_gin on plantaopro.triagem_historico using gin (metadata);

create table if not exists plantaopro.consultas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_consultas_tenant_id on plantaopro.consultas (tenant_id);
create index if not exists ix_consultas_cliente_id on plantaopro.consultas (cliente_id);
create index if not exists ix_consultas_paciente_id on plantaopro.consultas (paciente_id);
create index if not exists ix_consultas_medico_id on plantaopro.consultas (medico_id);
create index if not exists ix_consultas_agendamento_id on plantaopro.consultas (agendamento_id);
create index if not exists ix_consultas_consulta_id on plantaopro.consultas (consulta_id);
create index if not exists ix_consultas_status on plantaopro.consultas (status);
create index if not exists ix_consultas_reg_date on plantaopro.consultas (reg_date);
create index if not exists ix_consultas_metadata_gin on plantaopro.consultas using gin (metadata);

create table if not exists plantaopro.consulta_anamnese (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_consulta_anamnese_tenant_id on plantaopro.consulta_anamnese (tenant_id);
create index if not exists ix_consulta_anamnese_cliente_id on plantaopro.consulta_anamnese (cliente_id);
create index if not exists ix_consulta_anamnese_paciente_id on plantaopro.consulta_anamnese (paciente_id);
create index if not exists ix_consulta_anamnese_medico_id on plantaopro.consulta_anamnese (medico_id);
create index if not exists ix_consulta_anamnese_agendamento_id on plantaopro.consulta_anamnese (agendamento_id);
create index if not exists ix_consulta_anamnese_consulta_id on plantaopro.consulta_anamnese (consulta_id);
create index if not exists ix_consulta_anamnese_status on plantaopro.consulta_anamnese (status);
create index if not exists ix_consulta_anamnese_reg_date on plantaopro.consulta_anamnese (reg_date);
create index if not exists ix_consulta_anamnese_metadata_gin on plantaopro.consulta_anamnese using gin (metadata);

create table if not exists plantaopro.consulta_exame_fisico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_consulta_exame_fisico_tenant_id on plantaopro.consulta_exame_fisico (tenant_id);
create index if not exists ix_consulta_exame_fisico_cliente_id on plantaopro.consulta_exame_fisico (cliente_id);
create index if not exists ix_consulta_exame_fisico_paciente_id on plantaopro.consulta_exame_fisico (paciente_id);
create index if not exists ix_consulta_exame_fisico_medico_id on plantaopro.consulta_exame_fisico (medico_id);
create index if not exists ix_consulta_exame_fisico_agendamento_id on plantaopro.consulta_exame_fisico (agendamento_id);
create index if not exists ix_consulta_exame_fisico_consulta_id on plantaopro.consulta_exame_fisico (consulta_id);
create index if not exists ix_consulta_exame_fisico_status on plantaopro.consulta_exame_fisico (status);
create index if not exists ix_consulta_exame_fisico_reg_date on plantaopro.consulta_exame_fisico (reg_date);
create index if not exists ix_consulta_exame_fisico_metadata_gin on plantaopro.consulta_exame_fisico using gin (metadata);

create table if not exists plantaopro.consulta_diagnosticos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_consulta_diagnosticos_tenant_id on plantaopro.consulta_diagnosticos (tenant_id);
create index if not exists ix_consulta_diagnosticos_cliente_id on plantaopro.consulta_diagnosticos (cliente_id);
create index if not exists ix_consulta_diagnosticos_paciente_id on plantaopro.consulta_diagnosticos (paciente_id);
create index if not exists ix_consulta_diagnosticos_medico_id on plantaopro.consulta_diagnosticos (medico_id);
create index if not exists ix_consulta_diagnosticos_agendamento_id on plantaopro.consulta_diagnosticos (agendamento_id);
create index if not exists ix_consulta_diagnosticos_consulta_id on plantaopro.consulta_diagnosticos (consulta_id);
create index if not exists ix_consulta_diagnosticos_status on plantaopro.consulta_diagnosticos (status);
create index if not exists ix_consulta_diagnosticos_reg_date on plantaopro.consulta_diagnosticos (reg_date);
create index if not exists ix_consulta_diagnosticos_metadata_gin on plantaopro.consulta_diagnosticos using gin (metadata);

create table if not exists plantaopro.consulta_condutas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_consulta_condutas_tenant_id on plantaopro.consulta_condutas (tenant_id);
create index if not exists ix_consulta_condutas_cliente_id on plantaopro.consulta_condutas (cliente_id);
create index if not exists ix_consulta_condutas_paciente_id on plantaopro.consulta_condutas (paciente_id);
create index if not exists ix_consulta_condutas_medico_id on plantaopro.consulta_condutas (medico_id);
create index if not exists ix_consulta_condutas_agendamento_id on plantaopro.consulta_condutas (agendamento_id);
create index if not exists ix_consulta_condutas_consulta_id on plantaopro.consulta_condutas (consulta_id);
create index if not exists ix_consulta_condutas_status on plantaopro.consulta_condutas (status);
create index if not exists ix_consulta_condutas_reg_date on plantaopro.consulta_condutas (reg_date);
create index if not exists ix_consulta_condutas_metadata_gin on plantaopro.consulta_condutas using gin (metadata);

create table if not exists plantaopro.consulta_encaminhamentos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_consulta_encaminhamentos_tenant_id on plantaopro.consulta_encaminhamentos (tenant_id);
create index if not exists ix_consulta_encaminhamentos_cliente_id on plantaopro.consulta_encaminhamentos (cliente_id);
create index if not exists ix_consulta_encaminhamentos_paciente_id on plantaopro.consulta_encaminhamentos (paciente_id);
create index if not exists ix_consulta_encaminhamentos_medico_id on plantaopro.consulta_encaminhamentos (medico_id);
create index if not exists ix_consulta_encaminhamentos_agendamento_id on plantaopro.consulta_encaminhamentos (agendamento_id);
create index if not exists ix_consulta_encaminhamentos_consulta_id on plantaopro.consulta_encaminhamentos (consulta_id);
create index if not exists ix_consulta_encaminhamentos_status on plantaopro.consulta_encaminhamentos (status);
create index if not exists ix_consulta_encaminhamentos_reg_date on plantaopro.consulta_encaminhamentos (reg_date);
create index if not exists ix_consulta_encaminhamentos_metadata_gin on plantaopro.consulta_encaminhamentos using gin (metadata);

create table if not exists plantaopro.consulta_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_consulta_historico_tenant_id on plantaopro.consulta_historico (tenant_id);
create index if not exists ix_consulta_historico_cliente_id on plantaopro.consulta_historico (cliente_id);
create index if not exists ix_consulta_historico_paciente_id on plantaopro.consulta_historico (paciente_id);
create index if not exists ix_consulta_historico_medico_id on plantaopro.consulta_historico (medico_id);
create index if not exists ix_consulta_historico_agendamento_id on plantaopro.consulta_historico (agendamento_id);
create index if not exists ix_consulta_historico_consulta_id on plantaopro.consulta_historico (consulta_id);
create index if not exists ix_consulta_historico_status on plantaopro.consulta_historico (status);
create index if not exists ix_consulta_historico_reg_date on plantaopro.consulta_historico (reg_date);
create index if not exists ix_consulta_historico_metadata_gin on plantaopro.consulta_historico using gin (metadata);

create table if not exists plantaopro.cid_tabela (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_cid_tabela_tenant_id on plantaopro.cid_tabela (tenant_id);
create index if not exists ix_cid_tabela_cliente_id on plantaopro.cid_tabela (cliente_id);
create index if not exists ix_cid_tabela_paciente_id on plantaopro.cid_tabela (paciente_id);
create index if not exists ix_cid_tabela_medico_id on plantaopro.cid_tabela (medico_id);
create index if not exists ix_cid_tabela_agendamento_id on plantaopro.cid_tabela (agendamento_id);
create index if not exists ix_cid_tabela_consulta_id on plantaopro.cid_tabela (consulta_id);
create index if not exists ix_cid_tabela_status on plantaopro.cid_tabela (status);
create index if not exists ix_cid_tabela_reg_date on plantaopro.cid_tabela (reg_date);
create index if not exists ix_cid_tabela_metadata_gin on plantaopro.cid_tabela using gin (metadata);

create table if not exists plantaopro.cid_favoritos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_cid_favoritos_tenant_id on plantaopro.cid_favoritos (tenant_id);
create index if not exists ix_cid_favoritos_cliente_id on plantaopro.cid_favoritos (cliente_id);
create index if not exists ix_cid_favoritos_paciente_id on plantaopro.cid_favoritos (paciente_id);
create index if not exists ix_cid_favoritos_medico_id on plantaopro.cid_favoritos (medico_id);
create index if not exists ix_cid_favoritos_agendamento_id on plantaopro.cid_favoritos (agendamento_id);
create index if not exists ix_cid_favoritos_consulta_id on plantaopro.cid_favoritos (consulta_id);
create index if not exists ix_cid_favoritos_status on plantaopro.cid_favoritos (status);
create index if not exists ix_cid_favoritos_reg_date on plantaopro.cid_favoritos (reg_date);
create index if not exists ix_cid_favoritos_metadata_gin on plantaopro.cid_favoritos using gin (metadata);

create table if not exists plantaopro.cid_uso_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_cid_uso_historico_tenant_id on plantaopro.cid_uso_historico (tenant_id);
create index if not exists ix_cid_uso_historico_cliente_id on plantaopro.cid_uso_historico (cliente_id);
create index if not exists ix_cid_uso_historico_paciente_id on plantaopro.cid_uso_historico (paciente_id);
create index if not exists ix_cid_uso_historico_medico_id on plantaopro.cid_uso_historico (medico_id);
create index if not exists ix_cid_uso_historico_agendamento_id on plantaopro.cid_uso_historico (agendamento_id);
create index if not exists ix_cid_uso_historico_consulta_id on plantaopro.cid_uso_historico (consulta_id);
create index if not exists ix_cid_uso_historico_status on plantaopro.cid_uso_historico (status);
create index if not exists ix_cid_uso_historico_reg_date on plantaopro.cid_uso_historico (reg_date);
create index if not exists ix_cid_uso_historico_metadata_gin on plantaopro.cid_uso_historico using gin (metadata);

create table if not exists plantaopro.prescricoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_prescricoes_tenant_id on plantaopro.prescricoes (tenant_id);
create index if not exists ix_prescricoes_cliente_id on plantaopro.prescricoes (cliente_id);
create index if not exists ix_prescricoes_paciente_id on plantaopro.prescricoes (paciente_id);
create index if not exists ix_prescricoes_medico_id on plantaopro.prescricoes (medico_id);
create index if not exists ix_prescricoes_agendamento_id on plantaopro.prescricoes (agendamento_id);
create index if not exists ix_prescricoes_consulta_id on plantaopro.prescricoes (consulta_id);
create index if not exists ix_prescricoes_status on plantaopro.prescricoes (status);
create index if not exists ix_prescricoes_reg_date on plantaopro.prescricoes (reg_date);
create index if not exists ix_prescricoes_metadata_gin on plantaopro.prescricoes using gin (metadata);

create table if not exists plantaopro.prescricao_itens (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_prescricao_itens_tenant_id on plantaopro.prescricao_itens (tenant_id);
create index if not exists ix_prescricao_itens_cliente_id on plantaopro.prescricao_itens (cliente_id);
create index if not exists ix_prescricao_itens_paciente_id on plantaopro.prescricao_itens (paciente_id);
create index if not exists ix_prescricao_itens_medico_id on plantaopro.prescricao_itens (medico_id);
create index if not exists ix_prescricao_itens_agendamento_id on plantaopro.prescricao_itens (agendamento_id);
create index if not exists ix_prescricao_itens_consulta_id on plantaopro.prescricao_itens (consulta_id);
create index if not exists ix_prescricao_itens_status on plantaopro.prescricao_itens (status);
create index if not exists ix_prescricao_itens_reg_date on plantaopro.prescricao_itens (reg_date);
create index if not exists ix_prescricao_itens_metadata_gin on plantaopro.prescricao_itens using gin (metadata);

create table if not exists plantaopro.prescricao_modelos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_prescricao_modelos_tenant_id on plantaopro.prescricao_modelos (tenant_id);
create index if not exists ix_prescricao_modelos_cliente_id on plantaopro.prescricao_modelos (cliente_id);
create index if not exists ix_prescricao_modelos_paciente_id on plantaopro.prescricao_modelos (paciente_id);
create index if not exists ix_prescricao_modelos_medico_id on plantaopro.prescricao_modelos (medico_id);
create index if not exists ix_prescricao_modelos_agendamento_id on plantaopro.prescricao_modelos (agendamento_id);
create index if not exists ix_prescricao_modelos_consulta_id on plantaopro.prescricao_modelos (consulta_id);
create index if not exists ix_prescricao_modelos_status on plantaopro.prescricao_modelos (status);
create index if not exists ix_prescricao_modelos_reg_date on plantaopro.prescricao_modelos (reg_date);
create index if not exists ix_prescricao_modelos_metadata_gin on plantaopro.prescricao_modelos using gin (metadata);

create table if not exists plantaopro.prescricao_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_prescricao_historico_tenant_id on plantaopro.prescricao_historico (tenant_id);
create index if not exists ix_prescricao_historico_cliente_id on plantaopro.prescricao_historico (cliente_id);
create index if not exists ix_prescricao_historico_paciente_id on plantaopro.prescricao_historico (paciente_id);
create index if not exists ix_prescricao_historico_medico_id on plantaopro.prescricao_historico (medico_id);
create index if not exists ix_prescricao_historico_agendamento_id on plantaopro.prescricao_historico (agendamento_id);
create index if not exists ix_prescricao_historico_consulta_id on plantaopro.prescricao_historico (consulta_id);
create index if not exists ix_prescricao_historico_status on plantaopro.prescricao_historico (status);
create index if not exists ix_prescricao_historico_reg_date on plantaopro.prescricao_historico (reg_date);
create index if not exists ix_prescricao_historico_metadata_gin on plantaopro.prescricao_historico using gin (metadata);

create table if not exists plantaopro.prescricao_cancelamentos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_prescricao_cancelamentos_tenant_id on plantaopro.prescricao_cancelamentos (tenant_id);
create index if not exists ix_prescricao_cancelamentos_cliente_id on plantaopro.prescricao_cancelamentos (cliente_id);
create index if not exists ix_prescricao_cancelamentos_paciente_id on plantaopro.prescricao_cancelamentos (paciente_id);
create index if not exists ix_prescricao_cancelamentos_medico_id on plantaopro.prescricao_cancelamentos (medico_id);
create index if not exists ix_prescricao_cancelamentos_agendamento_id on plantaopro.prescricao_cancelamentos (agendamento_id);
create index if not exists ix_prescricao_cancelamentos_consulta_id on plantaopro.prescricao_cancelamentos (consulta_id);
create index if not exists ix_prescricao_cancelamentos_status on plantaopro.prescricao_cancelamentos (status);
create index if not exists ix_prescricao_cancelamentos_reg_date on plantaopro.prescricao_cancelamentos (reg_date);
create index if not exists ix_prescricao_cancelamentos_metadata_gin on plantaopro.prescricao_cancelamentos using gin (metadata);

create table if not exists plantaopro.clinica_contas_receber (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_clinica_contas_receber_tenant_id on plantaopro.clinica_contas_receber (tenant_id);
create index if not exists ix_clinica_contas_receber_cliente_id on plantaopro.clinica_contas_receber (cliente_id);
create index if not exists ix_clinica_contas_receber_paciente_id on plantaopro.clinica_contas_receber (paciente_id);
create index if not exists ix_clinica_contas_receber_medico_id on plantaopro.clinica_contas_receber (medico_id);
create index if not exists ix_clinica_contas_receber_agendamento_id on plantaopro.clinica_contas_receber (agendamento_id);
create index if not exists ix_clinica_contas_receber_consulta_id on plantaopro.clinica_contas_receber (consulta_id);
create index if not exists ix_clinica_contas_receber_status on plantaopro.clinica_contas_receber (status);
create index if not exists ix_clinica_contas_receber_reg_date on plantaopro.clinica_contas_receber (reg_date);
create index if not exists ix_clinica_contas_receber_metadata_gin on plantaopro.clinica_contas_receber using gin (metadata);

create table if not exists plantaopro.clinica_recebimentos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_clinica_recebimentos_tenant_id on plantaopro.clinica_recebimentos (tenant_id);
create index if not exists ix_clinica_recebimentos_cliente_id on plantaopro.clinica_recebimentos (cliente_id);
create index if not exists ix_clinica_recebimentos_paciente_id on plantaopro.clinica_recebimentos (paciente_id);
create index if not exists ix_clinica_recebimentos_medico_id on plantaopro.clinica_recebimentos (medico_id);
create index if not exists ix_clinica_recebimentos_agendamento_id on plantaopro.clinica_recebimentos (agendamento_id);
create index if not exists ix_clinica_recebimentos_consulta_id on plantaopro.clinica_recebimentos (consulta_id);
create index if not exists ix_clinica_recebimentos_status on plantaopro.clinica_recebimentos (status);
create index if not exists ix_clinica_recebimentos_reg_date on plantaopro.clinica_recebimentos (reg_date);
create index if not exists ix_clinica_recebimentos_metadata_gin on plantaopro.clinica_recebimentos using gin (metadata);

create table if not exists plantaopro.clinica_caixa (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_clinica_caixa_tenant_id on plantaopro.clinica_caixa (tenant_id);
create index if not exists ix_clinica_caixa_cliente_id on plantaopro.clinica_caixa (cliente_id);
create index if not exists ix_clinica_caixa_paciente_id on plantaopro.clinica_caixa (paciente_id);
create index if not exists ix_clinica_caixa_medico_id on plantaopro.clinica_caixa (medico_id);
create index if not exists ix_clinica_caixa_agendamento_id on plantaopro.clinica_caixa (agendamento_id);
create index if not exists ix_clinica_caixa_consulta_id on plantaopro.clinica_caixa (consulta_id);
create index if not exists ix_clinica_caixa_status on plantaopro.clinica_caixa (status);
create index if not exists ix_clinica_caixa_reg_date on plantaopro.clinica_caixa (reg_date);
create index if not exists ix_clinica_caixa_metadata_gin on plantaopro.clinica_caixa using gin (metadata);

create table if not exists plantaopro.clinica_fechamentos_caixa (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_clinica_fechamentos_caixa_tenant_id on plantaopro.clinica_fechamentos_caixa (tenant_id);
create index if not exists ix_clinica_fechamentos_caixa_cliente_id on plantaopro.clinica_fechamentos_caixa (cliente_id);
create index if not exists ix_clinica_fechamentos_caixa_paciente_id on plantaopro.clinica_fechamentos_caixa (paciente_id);
create index if not exists ix_clinica_fechamentos_caixa_medico_id on plantaopro.clinica_fechamentos_caixa (medico_id);
create index if not exists ix_clinica_fechamentos_caixa_agendamento_id on plantaopro.clinica_fechamentos_caixa (agendamento_id);
create index if not exists ix_clinica_fechamentos_caixa_consulta_id on plantaopro.clinica_fechamentos_caixa (consulta_id);
create index if not exists ix_clinica_fechamentos_caixa_status on plantaopro.clinica_fechamentos_caixa (status);
create index if not exists ix_clinica_fechamentos_caixa_reg_date on plantaopro.clinica_fechamentos_caixa (reg_date);
create index if not exists ix_clinica_fechamentos_caixa_metadata_gin on plantaopro.clinica_fechamentos_caixa using gin (metadata);

create table if not exists plantaopro.clinica_repasses (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_clinica_repasses_tenant_id on plantaopro.clinica_repasses (tenant_id);
create index if not exists ix_clinica_repasses_cliente_id on plantaopro.clinica_repasses (cliente_id);
create index if not exists ix_clinica_repasses_paciente_id on plantaopro.clinica_repasses (paciente_id);
create index if not exists ix_clinica_repasses_medico_id on plantaopro.clinica_repasses (medico_id);
create index if not exists ix_clinica_repasses_agendamento_id on plantaopro.clinica_repasses (agendamento_id);
create index if not exists ix_clinica_repasses_consulta_id on plantaopro.clinica_repasses (consulta_id);
create index if not exists ix_clinica_repasses_status on plantaopro.clinica_repasses (status);
create index if not exists ix_clinica_repasses_reg_date on plantaopro.clinica_repasses (reg_date);
create index if not exists ix_clinica_repasses_metadata_gin on plantaopro.clinica_repasses using gin (metadata);

create table if not exists plantaopro.clinica_lancamentos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_clinica_lancamentos_tenant_id on plantaopro.clinica_lancamentos (tenant_id);
create index if not exists ix_clinica_lancamentos_cliente_id on plantaopro.clinica_lancamentos (cliente_id);
create index if not exists ix_clinica_lancamentos_paciente_id on plantaopro.clinica_lancamentos (paciente_id);
create index if not exists ix_clinica_lancamentos_medico_id on plantaopro.clinica_lancamentos (medico_id);
create index if not exists ix_clinica_lancamentos_agendamento_id on plantaopro.clinica_lancamentos (agendamento_id);
create index if not exists ix_clinica_lancamentos_consulta_id on plantaopro.clinica_lancamentos (consulta_id);
create index if not exists ix_clinica_lancamentos_status on plantaopro.clinica_lancamentos (status);
create index if not exists ix_clinica_lancamentos_reg_date on plantaopro.clinica_lancamentos (reg_date);
create index if not exists ix_clinica_lancamentos_metadata_gin on plantaopro.clinica_lancamentos using gin (metadata);

create table if not exists plantaopro.clinica_glosas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_clinica_glosas_tenant_id on plantaopro.clinica_glosas (tenant_id);
create index if not exists ix_clinica_glosas_cliente_id on plantaopro.clinica_glosas (cliente_id);
create index if not exists ix_clinica_glosas_paciente_id on plantaopro.clinica_glosas (paciente_id);
create index if not exists ix_clinica_glosas_medico_id on plantaopro.clinica_glosas (medico_id);
create index if not exists ix_clinica_glosas_agendamento_id on plantaopro.clinica_glosas (agendamento_id);
create index if not exists ix_clinica_glosas_consulta_id on plantaopro.clinica_glosas (consulta_id);
create index if not exists ix_clinica_glosas_status on plantaopro.clinica_glosas (status);
create index if not exists ix_clinica_glosas_reg_date on plantaopro.clinica_glosas (reg_date);
create index if not exists ix_clinica_glosas_metadata_gin on plantaopro.clinica_glosas using gin (metadata);

create table if not exists plantaopro.convenios (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenios_tenant_id on plantaopro.convenios (tenant_id);
create index if not exists ix_convenios_cliente_id on plantaopro.convenios (cliente_id);
create index if not exists ix_convenios_paciente_id on plantaopro.convenios (paciente_id);
create index if not exists ix_convenios_medico_id on plantaopro.convenios (medico_id);
create index if not exists ix_convenios_agendamento_id on plantaopro.convenios (agendamento_id);
create index if not exists ix_convenios_consulta_id on plantaopro.convenios (consulta_id);
create index if not exists ix_convenios_status on plantaopro.convenios (status);
create index if not exists ix_convenios_reg_date on plantaopro.convenios (reg_date);
create index if not exists ix_convenios_metadata_gin on plantaopro.convenios using gin (metadata);

create table if not exists plantaopro.convenio_contratos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenio_contratos_tenant_id on plantaopro.convenio_contratos (tenant_id);
create index if not exists ix_convenio_contratos_cliente_id on plantaopro.convenio_contratos (cliente_id);
create index if not exists ix_convenio_contratos_paciente_id on plantaopro.convenio_contratos (paciente_id);
create index if not exists ix_convenio_contratos_medico_id on plantaopro.convenio_contratos (medico_id);
create index if not exists ix_convenio_contratos_agendamento_id on plantaopro.convenio_contratos (agendamento_id);
create index if not exists ix_convenio_contratos_consulta_id on plantaopro.convenio_contratos (consulta_id);
create index if not exists ix_convenio_contratos_status on plantaopro.convenio_contratos (status);
create index if not exists ix_convenio_contratos_reg_date on plantaopro.convenio_contratos (reg_date);
create index if not exists ix_convenio_contratos_metadata_gin on plantaopro.convenio_contratos using gin (metadata);

create table if not exists plantaopro.convenio_planos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenio_planos_tenant_id on plantaopro.convenio_planos (tenant_id);
create index if not exists ix_convenio_planos_cliente_id on plantaopro.convenio_planos (cliente_id);
create index if not exists ix_convenio_planos_paciente_id on plantaopro.convenio_planos (paciente_id);
create index if not exists ix_convenio_planos_medico_id on plantaopro.convenio_planos (medico_id);
create index if not exists ix_convenio_planos_agendamento_id on plantaopro.convenio_planos (agendamento_id);
create index if not exists ix_convenio_planos_consulta_id on plantaopro.convenio_planos (consulta_id);
create index if not exists ix_convenio_planos_status on plantaopro.convenio_planos (status);
create index if not exists ix_convenio_planos_reg_date on plantaopro.convenio_planos (reg_date);
create index if not exists ix_convenio_planos_metadata_gin on plantaopro.convenio_planos using gin (metadata);

create table if not exists plantaopro.convenio_tabelas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenio_tabelas_tenant_id on plantaopro.convenio_tabelas (tenant_id);
create index if not exists ix_convenio_tabelas_cliente_id on plantaopro.convenio_tabelas (cliente_id);
create index if not exists ix_convenio_tabelas_paciente_id on plantaopro.convenio_tabelas (paciente_id);
create index if not exists ix_convenio_tabelas_medico_id on plantaopro.convenio_tabelas (medico_id);
create index if not exists ix_convenio_tabelas_agendamento_id on plantaopro.convenio_tabelas (agendamento_id);
create index if not exists ix_convenio_tabelas_consulta_id on plantaopro.convenio_tabelas (consulta_id);
create index if not exists ix_convenio_tabelas_status on plantaopro.convenio_tabelas (status);
create index if not exists ix_convenio_tabelas_reg_date on plantaopro.convenio_tabelas (reg_date);
create index if not exists ix_convenio_tabelas_metadata_gin on plantaopro.convenio_tabelas using gin (metadata);

create table if not exists plantaopro.convenio_procedimentos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenio_procedimentos_tenant_id on plantaopro.convenio_procedimentos (tenant_id);
create index if not exists ix_convenio_procedimentos_cliente_id on plantaopro.convenio_procedimentos (cliente_id);
create index if not exists ix_convenio_procedimentos_paciente_id on plantaopro.convenio_procedimentos (paciente_id);
create index if not exists ix_convenio_procedimentos_medico_id on plantaopro.convenio_procedimentos (medico_id);
create index if not exists ix_convenio_procedimentos_agendamento_id on plantaopro.convenio_procedimentos (agendamento_id);
create index if not exists ix_convenio_procedimentos_consulta_id on plantaopro.convenio_procedimentos (consulta_id);
create index if not exists ix_convenio_procedimentos_status on plantaopro.convenio_procedimentos (status);
create index if not exists ix_convenio_procedimentos_reg_date on plantaopro.convenio_procedimentos (reg_date);
create index if not exists ix_convenio_procedimentos_metadata_gin on plantaopro.convenio_procedimentos using gin (metadata);

create table if not exists plantaopro.convenio_autorizacoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenio_autorizacoes_tenant_id on plantaopro.convenio_autorizacoes (tenant_id);
create index if not exists ix_convenio_autorizacoes_cliente_id on plantaopro.convenio_autorizacoes (cliente_id);
create index if not exists ix_convenio_autorizacoes_paciente_id on plantaopro.convenio_autorizacoes (paciente_id);
create index if not exists ix_convenio_autorizacoes_medico_id on plantaopro.convenio_autorizacoes (medico_id);
create index if not exists ix_convenio_autorizacoes_agendamento_id on plantaopro.convenio_autorizacoes (agendamento_id);
create index if not exists ix_convenio_autorizacoes_consulta_id on plantaopro.convenio_autorizacoes (consulta_id);
create index if not exists ix_convenio_autorizacoes_status on plantaopro.convenio_autorizacoes (status);
create index if not exists ix_convenio_autorizacoes_reg_date on plantaopro.convenio_autorizacoes (reg_date);
create index if not exists ix_convenio_autorizacoes_metadata_gin on plantaopro.convenio_autorizacoes using gin (metadata);

create table if not exists plantaopro.convenio_glosas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenio_glosas_tenant_id on plantaopro.convenio_glosas (tenant_id);
create index if not exists ix_convenio_glosas_cliente_id on plantaopro.convenio_glosas (cliente_id);
create index if not exists ix_convenio_glosas_paciente_id on plantaopro.convenio_glosas (paciente_id);
create index if not exists ix_convenio_glosas_medico_id on plantaopro.convenio_glosas (medico_id);
create index if not exists ix_convenio_glosas_agendamento_id on plantaopro.convenio_glosas (agendamento_id);
create index if not exists ix_convenio_glosas_consulta_id on plantaopro.convenio_glosas (consulta_id);
create index if not exists ix_convenio_glosas_status on plantaopro.convenio_glosas (status);
create index if not exists ix_convenio_glosas_reg_date on plantaopro.convenio_glosas (reg_date);
create index if not exists ix_convenio_glosas_metadata_gin on plantaopro.convenio_glosas using gin (metadata);

create table if not exists plantaopro.convenio_faturamentos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenio_faturamentos_tenant_id on plantaopro.convenio_faturamentos (tenant_id);
create index if not exists ix_convenio_faturamentos_cliente_id on plantaopro.convenio_faturamentos (cliente_id);
create index if not exists ix_convenio_faturamentos_paciente_id on plantaopro.convenio_faturamentos (paciente_id);
create index if not exists ix_convenio_faturamentos_medico_id on plantaopro.convenio_faturamentos (medico_id);
create index if not exists ix_convenio_faturamentos_agendamento_id on plantaopro.convenio_faturamentos (agendamento_id);
create index if not exists ix_convenio_faturamentos_consulta_id on plantaopro.convenio_faturamentos (consulta_id);
create index if not exists ix_convenio_faturamentos_status on plantaopro.convenio_faturamentos (status);
create index if not exists ix_convenio_faturamentos_reg_date on plantaopro.convenio_faturamentos (reg_date);
create index if not exists ix_convenio_faturamentos_metadata_gin on plantaopro.convenio_faturamentos using gin (metadata);

create table if not exists plantaopro.planos_saude (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_planos_saude_tenant_id on plantaopro.planos_saude (tenant_id);
create index if not exists ix_planos_saude_cliente_id on plantaopro.planos_saude (cliente_id);
create index if not exists ix_planos_saude_paciente_id on plantaopro.planos_saude (paciente_id);
create index if not exists ix_planos_saude_medico_id on plantaopro.planos_saude (medico_id);
create index if not exists ix_planos_saude_agendamento_id on plantaopro.planos_saude (agendamento_id);
create index if not exists ix_planos_saude_consulta_id on plantaopro.planos_saude (consulta_id);
create index if not exists ix_planos_saude_status on plantaopro.planos_saude (status);
create index if not exists ix_planos_saude_reg_date on plantaopro.planos_saude (reg_date);
create index if not exists ix_planos_saude_metadata_gin on plantaopro.planos_saude using gin (metadata);

create table if not exists plantaopro.plano_saude_coberturas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_plano_saude_coberturas_tenant_id on plantaopro.plano_saude_coberturas (tenant_id);
create index if not exists ix_plano_saude_coberturas_cliente_id on plantaopro.plano_saude_coberturas (cliente_id);
create index if not exists ix_plano_saude_coberturas_paciente_id on plantaopro.plano_saude_coberturas (paciente_id);
create index if not exists ix_plano_saude_coberturas_medico_id on plantaopro.plano_saude_coberturas (medico_id);
create index if not exists ix_plano_saude_coberturas_agendamento_id on plantaopro.plano_saude_coberturas (agendamento_id);
create index if not exists ix_plano_saude_coberturas_consulta_id on plantaopro.plano_saude_coberturas (consulta_id);
create index if not exists ix_plano_saude_coberturas_status on plantaopro.plano_saude_coberturas (status);
create index if not exists ix_plano_saude_coberturas_reg_date on plantaopro.plano_saude_coberturas (reg_date);
create index if not exists ix_plano_saude_coberturas_metadata_gin on plantaopro.plano_saude_coberturas using gin (metadata);

create table if not exists plantaopro.plano_saude_pacientes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_plano_saude_pacientes_tenant_id on plantaopro.plano_saude_pacientes (tenant_id);
create index if not exists ix_plano_saude_pacientes_cliente_id on plantaopro.plano_saude_pacientes (cliente_id);
create index if not exists ix_plano_saude_pacientes_paciente_id on plantaopro.plano_saude_pacientes (paciente_id);
create index if not exists ix_plano_saude_pacientes_medico_id on plantaopro.plano_saude_pacientes (medico_id);
create index if not exists ix_plano_saude_pacientes_agendamento_id on plantaopro.plano_saude_pacientes (agendamento_id);
create index if not exists ix_plano_saude_pacientes_consulta_id on plantaopro.plano_saude_pacientes (consulta_id);
create index if not exists ix_plano_saude_pacientes_status on plantaopro.plano_saude_pacientes (status);
create index if not exists ix_plano_saude_pacientes_reg_date on plantaopro.plano_saude_pacientes (reg_date);
create index if not exists ix_plano_saude_pacientes_metadata_gin on plantaopro.plano_saude_pacientes using gin (metadata);

create table if not exists plantaopro.plano_saude_autorizacoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_plano_saude_autorizacoes_tenant_id on plantaopro.plano_saude_autorizacoes (tenant_id);
create index if not exists ix_plano_saude_autorizacoes_cliente_id on plantaopro.plano_saude_autorizacoes (cliente_id);
create index if not exists ix_plano_saude_autorizacoes_paciente_id on plantaopro.plano_saude_autorizacoes (paciente_id);
create index if not exists ix_plano_saude_autorizacoes_medico_id on plantaopro.plano_saude_autorizacoes (medico_id);
create index if not exists ix_plano_saude_autorizacoes_agendamento_id on plantaopro.plano_saude_autorizacoes (agendamento_id);
create index if not exists ix_plano_saude_autorizacoes_consulta_id on plantaopro.plano_saude_autorizacoes (consulta_id);
create index if not exists ix_plano_saude_autorizacoes_status on plantaopro.plano_saude_autorizacoes (status);
create index if not exists ix_plano_saude_autorizacoes_reg_date on plantaopro.plano_saude_autorizacoes (reg_date);
create index if not exists ix_plano_saude_autorizacoes_metadata_gin on plantaopro.plano_saude_autorizacoes using gin (metadata);

create table if not exists plantaopro.plano_saude_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    conta_receber_id uuid null,
    painel_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    codigo varchar(80) null,
    nome varchar(255) null,
    descricao text null,
    status varchar(40) not null default 'ATIVO',
    data_inicio timestamptz null,
    data_fim timestamptz null,
    vencimento date null,
    valor_total numeric(14,2) null,
    valor_pago numeric(14,2) null,
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    token_seguro varchar(160) null,
    metadata jsonb not null default '{}'::jsonb,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamptz not null default now(),
    updated_at timestamptz null,
    reg_status char(1) not null default 'A'
);
create index if not exists ix_plano_saude_historico_tenant_id on plantaopro.plano_saude_historico (tenant_id);
create index if not exists ix_plano_saude_historico_cliente_id on plantaopro.plano_saude_historico (cliente_id);
create index if not exists ix_plano_saude_historico_paciente_id on plantaopro.plano_saude_historico (paciente_id);
create index if not exists ix_plano_saude_historico_medico_id on plantaopro.plano_saude_historico (medico_id);
create index if not exists ix_plano_saude_historico_agendamento_id on plantaopro.plano_saude_historico (agendamento_id);
create index if not exists ix_plano_saude_historico_consulta_id on plantaopro.plano_saude_historico (consulta_id);
create index if not exists ix_plano_saude_historico_status on plantaopro.plano_saude_historico (status);
create index if not exists ix_plano_saude_historico_reg_date on plantaopro.plano_saude_historico (reg_date);
create index if not exists ix_plano_saude_historico_metadata_gin on plantaopro.plano_saude_historico using gin (metadata);

do $$
begin
    if not exists (
        select 1 from pg_constraint
        where conname = 'uq_cid_tabela_codigo'
          and conrelid = 'plantaopro.cid_tabela'::regclass
    ) then
        alter table plantaopro.cid_tabela add constraint uq_cid_tabela_codigo unique (codigo);
    end if;
end $$;

do $$
begin
    if not exists (
        select 1 from pg_constraint
        where conname = 'uq_paineis_chamada_token_seguro'
          and conrelid = 'plantaopro.paineis_chamada'::regclass
    ) then
        alter table plantaopro.paineis_chamada add constraint uq_paineis_chamada_token_seguro unique (token_seguro);
    end if;
end $$;


-- Catálogo funcional dos módulos por plano, sem alterar dados existentes.
create table if not exists plantaopro.saude360_modulos_planos (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(80) not null,
    plano varchar(40) not null,
    descricao text not null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create unique index if not exists ux_saude360_modulos_planos_codigo_plano on plantaopro.saude360_modulos_planos (codigo, plano);
insert into plantaopro.saude360_modulos_planos(codigo, plano, descricao)
select v.codigo, v.plano, v.descricao
from (values
    ('PAINEL_CHAMADA','ESSENCIAL','Painel básico'),
    ('AGENDAMENTO','ESSENCIAL','Agendamento básico'),
    ('CONSULTAS','ESSENCIAL','Consulta simples'),
    ('TRIAGEM','PROFISSIONAL','Triagem clínica'),
    ('PRESCRICAO','PROFISSIONAL','Prescrição médica'),
    ('CID','PROFISSIONAL','Tabela CID e favoritos'),
    ('FINANCEIRO_CLINICA','PROFISSIONAL','Financeiro da clínica'),
    ('CONVENIOS','ENTERPRISE','Convênios, glosas e faturamento'),
    ('PLANOS_SAUDE','ENTERPRISE','Planos de saúde e autorizações')
) as v(codigo, plano, descricao)
where not exists (
    select 1 from plantaopro.saude360_modulos_planos p where p.codigo=v.codigo and p.plano=v.plano
);
