-- PlantaoPro Fase 4 - Automacao operacional, comunicacao, relatorios e recursos premium
create schema if not exists plantaopro;
create extension if not exists pgcrypto;

create table if not exists plantaopro.medico_disponibilidades (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    medico_id uuid not null,
    hospital_id uuid,
    especialidade_id uuid,
    dia_semana integer,
    data_inicio timestamp,
    data_fim timestamp,
    turno varchar(30),
    status varchar(30) default 'ATIVA',
    observacoes text,
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.medico_disponibilidades add column if not exists tenant_id uuid;
alter table plantaopro.medico_disponibilidades add column if not exists cliente_id uuid;
alter table plantaopro.medico_disponibilidades add column if not exists medico_id uuid;
alter table plantaopro.medico_disponibilidades add column if not exists hospital_id uuid;
alter table plantaopro.medico_disponibilidades add column if not exists especialidade_id uuid;
alter table plantaopro.medico_disponibilidades add column if not exists dia_semana integer;
alter table plantaopro.medico_disponibilidades add column if not exists data_inicio timestamp;
alter table plantaopro.medico_disponibilidades add column if not exists data_fim timestamp;
alter table plantaopro.medico_disponibilidades add column if not exists turno varchar(30);
alter table plantaopro.medico_disponibilidades add column if not exists status varchar(30) default 'ATIVA';
alter table plantaopro.medico_disponibilidades add column if not exists observacoes text;
alter table plantaopro.medico_disponibilidades add column if not exists created_by uuid;
alter table plantaopro.medico_disponibilidades add column if not exists updated_by uuid;
alter table plantaopro.medico_disponibilidades add column if not exists reg_status char(1) default 'A';
alter table plantaopro.medico_disponibilidades add column if not exists reg_date timestamp default now();
alter table plantaopro.medico_disponibilidades add column if not exists reg_update timestamp;
create index if not exists ix_medico_disponibilidades_tenant_id on plantaopro.medico_disponibilidades(tenant_id);
create index if not exists ix_medico_disponibilidades_cliente_id on plantaopro.medico_disponibilidades(cliente_id);
create index if not exists ix_medico_disponibilidades_medico_id on plantaopro.medico_disponibilidades(medico_id);
create index if not exists ix_medico_disponibilidades_hospital_id on plantaopro.medico_disponibilidades(hospital_id);
create index if not exists ix_medico_disponibilidades_status on plantaopro.medico_disponibilidades(status);
create index if not exists ix_medico_disponibilidades_reg_date on plantaopro.medico_disponibilidades(reg_date);

create table if not exists plantaopro.medico_indisponibilidades (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    medico_id uuid not null,
    data_inicio timestamp not null,
    data_fim timestamp not null,
    motivo text,
    status varchar(30) default 'ATIVA',
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.medico_indisponibilidades add column if not exists tenant_id uuid;
alter table plantaopro.medico_indisponibilidades add column if not exists cliente_id uuid;
alter table plantaopro.medico_indisponibilidades add column if not exists medico_id uuid;
alter table plantaopro.medico_indisponibilidades add column if not exists data_inicio timestamp;
alter table plantaopro.medico_indisponibilidades add column if not exists data_fim timestamp;
alter table plantaopro.medico_indisponibilidades add column if not exists motivo text;
alter table plantaopro.medico_indisponibilidades add column if not exists status varchar(30) default 'ATIVA';
alter table plantaopro.medico_indisponibilidades add column if not exists created_by uuid;
alter table plantaopro.medico_indisponibilidades add column if not exists updated_by uuid;
alter table plantaopro.medico_indisponibilidades add column if not exists reg_status char(1) default 'A';
alter table plantaopro.medico_indisponibilidades add column if not exists reg_date timestamp default now();
alter table plantaopro.medico_indisponibilidades add column if not exists reg_update timestamp;
create index if not exists ix_medico_indisponibilidades_tenant_id on plantaopro.medico_indisponibilidades(tenant_id);
create index if not exists ix_medico_indisponibilidades_cliente_id on plantaopro.medico_indisponibilidades(cliente_id);
create index if not exists ix_medico_indisponibilidades_medico_id on plantaopro.medico_indisponibilidades(medico_id);
create index if not exists ix_medico_indisponibilidades_status on plantaopro.medico_indisponibilidades(status);
create index if not exists ix_medico_indisponibilidades_reg_date on plantaopro.medico_indisponibilidades(reg_date);

create table if not exists plantaopro.medico_preferencias_plantao (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    medico_id uuid not null,
    hospitais_preferidos uuid[],
    especialidades_preferidas uuid[],
    turnos_preferidos text[],
    limite_plantoes_semana integer default 5,
    limite_plantoes_mes integer default 20,
    observacoes text,
    status varchar(30) default 'ATIVA',
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.medico_preferencias_plantao add column if not exists tenant_id uuid;
alter table plantaopro.medico_preferencias_plantao add column if not exists cliente_id uuid;
alter table plantaopro.medico_preferencias_plantao add column if not exists medico_id uuid;
alter table plantaopro.medico_preferencias_plantao add column if not exists hospitais_preferidos uuid[];
alter table plantaopro.medico_preferencias_plantao add column if not exists especialidades_preferidas uuid[];
alter table plantaopro.medico_preferencias_plantao add column if not exists turnos_preferidos text[];
alter table plantaopro.medico_preferencias_plantao add column if not exists limite_plantoes_semana integer default 5;
alter table plantaopro.medico_preferencias_plantao add column if not exists limite_plantoes_mes integer default 20;
alter table plantaopro.medico_preferencias_plantao add column if not exists observacoes text;
alter table plantaopro.medico_preferencias_plantao add column if not exists status varchar(30) default 'ATIVA';
alter table plantaopro.medico_preferencias_plantao add column if not exists created_by uuid;
alter table plantaopro.medico_preferencias_plantao add column if not exists updated_by uuid;
alter table plantaopro.medico_preferencias_plantao add column if not exists reg_status char(1) default 'A';
alter table plantaopro.medico_preferencias_plantao add column if not exists reg_date timestamp default now();
alter table plantaopro.medico_preferencias_plantao add column if not exists reg_update timestamp;
create index if not exists ix_medico_preferencias_plantao_tenant_id on plantaopro.medico_preferencias_plantao(tenant_id);
create index if not exists ix_medico_preferencias_plantao_cliente_id on plantaopro.medico_preferencias_plantao(cliente_id);
create index if not exists ix_medico_preferencias_plantao_medico_id on plantaopro.medico_preferencias_plantao(medico_id);
create index if not exists ix_medico_preferencias_plantao_status on plantaopro.medico_preferencias_plantao(status);
create index if not exists ix_medico_preferencias_plantao_reg_date on plantaopro.medico_preferencias_plantao(reg_date);

create table if not exists plantaopro.medico_bloqueios_agenda (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    medico_id uuid not null,
    plantao_id uuid,
    data_inicio timestamp,
    data_fim timestamp,
    motivo text,
    status varchar(30) default 'ATIVO',
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.medico_bloqueios_agenda add column if not exists tenant_id uuid;
alter table plantaopro.medico_bloqueios_agenda add column if not exists cliente_id uuid;
alter table plantaopro.medico_bloqueios_agenda add column if not exists medico_id uuid;
alter table plantaopro.medico_bloqueios_agenda add column if not exists plantao_id uuid;
alter table plantaopro.medico_bloqueios_agenda add column if not exists data_inicio timestamp;
alter table plantaopro.medico_bloqueios_agenda add column if not exists data_fim timestamp;
alter table plantaopro.medico_bloqueios_agenda add column if not exists motivo text;
alter table plantaopro.medico_bloqueios_agenda add column if not exists status varchar(30) default 'ATIVO';
alter table plantaopro.medico_bloqueios_agenda add column if not exists created_by uuid;
alter table plantaopro.medico_bloqueios_agenda add column if not exists updated_by uuid;
alter table plantaopro.medico_bloqueios_agenda add column if not exists reg_status char(1) default 'A';
alter table plantaopro.medico_bloqueios_agenda add column if not exists reg_date timestamp default now();
alter table plantaopro.medico_bloqueios_agenda add column if not exists reg_update timestamp;
create index if not exists ix_medico_bloqueios_agenda_tenant_id on plantaopro.medico_bloqueios_agenda(tenant_id);
create index if not exists ix_medico_bloqueios_agenda_cliente_id on plantaopro.medico_bloqueios_agenda(cliente_id);
create index if not exists ix_medico_bloqueios_agenda_medico_id on plantaopro.medico_bloqueios_agenda(medico_id);
create index if not exists ix_medico_bloqueios_agenda_plantao_id on plantaopro.medico_bloqueios_agenda(plantao_id);
create index if not exists ix_medico_bloqueios_agenda_status on plantaopro.medico_bloqueios_agenda(status);
create index if not exists ix_medico_bloqueios_agenda_reg_date on plantaopro.medico_bloqueios_agenda(reg_date);

create table if not exists plantaopro.medico_historico_disponibilidade (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    medico_id uuid not null,
    entidade varchar(80),
    entidade_id uuid,
    acao varchar(80),
    detalhes jsonb default '{}'::jsonb,
    usuario_id uuid,
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.medico_historico_disponibilidade add column if not exists tenant_id uuid;
alter table plantaopro.medico_historico_disponibilidade add column if not exists cliente_id uuid;
alter table plantaopro.medico_historico_disponibilidade add column if not exists medico_id uuid;
alter table plantaopro.medico_historico_disponibilidade add column if not exists entidade varchar(80);
alter table plantaopro.medico_historico_disponibilidade add column if not exists entidade_id uuid;
alter table plantaopro.medico_historico_disponibilidade add column if not exists acao varchar(80);
alter table plantaopro.medico_historico_disponibilidade add column if not exists detalhes jsonb default '{}'::jsonb;
alter table plantaopro.medico_historico_disponibilidade add column if not exists usuario_id uuid;
alter table plantaopro.medico_historico_disponibilidade add column if not exists created_by uuid;
alter table plantaopro.medico_historico_disponibilidade add column if not exists updated_by uuid;
alter table plantaopro.medico_historico_disponibilidade add column if not exists reg_status char(1) default 'A';
alter table plantaopro.medico_historico_disponibilidade add column if not exists reg_date timestamp default now();
alter table plantaopro.medico_historico_disponibilidade add column if not exists reg_update timestamp;
create index if not exists ix_medico_historico_disponibilidade_tenant_id on plantaopro.medico_historico_disponibilidade(tenant_id);
create index if not exists ix_medico_historico_disponibilidade_cliente_id on plantaopro.medico_historico_disponibilidade(cliente_id);
create index if not exists ix_medico_historico_disponibilidade_medico_id on plantaopro.medico_historico_disponibilidade(medico_id);
create index if not exists ix_medico_historico_disponibilidade_reg_date on plantaopro.medico_historico_disponibilidade(reg_date);

create table if not exists plantaopro.escala_sugestoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    plantao_id uuid not null,
    hospital_id uuid,
    especialidade_id uuid,
    status varchar(30) default 'GERADA',
    score_medio numeric(10,2) default 0,
    gerada_por uuid,
    criterios jsonb default '{}'::jsonb,
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.escala_sugestoes add column if not exists tenant_id uuid;
alter table plantaopro.escala_sugestoes add column if not exists cliente_id uuid;
alter table plantaopro.escala_sugestoes add column if not exists plantao_id uuid;
alter table plantaopro.escala_sugestoes add column if not exists hospital_id uuid;
alter table plantaopro.escala_sugestoes add column if not exists especialidade_id uuid;
alter table plantaopro.escala_sugestoes add column if not exists status varchar(30) default 'GERADA';
alter table plantaopro.escala_sugestoes add column if not exists score_medio numeric(10,2) default 0;
alter table plantaopro.escala_sugestoes add column if not exists gerada_por uuid;
alter table plantaopro.escala_sugestoes add column if not exists criterios jsonb default '{}'::jsonb;
alter table plantaopro.escala_sugestoes add column if not exists created_by uuid;
alter table plantaopro.escala_sugestoes add column if not exists updated_by uuid;
alter table plantaopro.escala_sugestoes add column if not exists reg_status char(1) default 'A';
alter table plantaopro.escala_sugestoes add column if not exists reg_date timestamp default now();
alter table plantaopro.escala_sugestoes add column if not exists reg_update timestamp;
create index if not exists ix_escala_sugestoes_tenant_id on plantaopro.escala_sugestoes(tenant_id);
create index if not exists ix_escala_sugestoes_cliente_id on plantaopro.escala_sugestoes(cliente_id);
create index if not exists ix_escala_sugestoes_hospital_id on plantaopro.escala_sugestoes(hospital_id);
create index if not exists ix_escala_sugestoes_plantao_id on plantaopro.escala_sugestoes(plantao_id);
create index if not exists ix_escala_sugestoes_status on plantaopro.escala_sugestoes(status);
create index if not exists ix_escala_sugestoes_reg_date on plantaopro.escala_sugestoes(reg_date);

create table if not exists plantaopro.escala_sugestao_medicos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    sugestao_id uuid not null,
    plantao_id uuid not null,
    medico_id uuid not null,
    score numeric(10,2) default 0,
    motivos text,
    alertas text,
    status varchar(30) default 'SUGERIDO',
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.escala_sugestao_medicos add column if not exists tenant_id uuid;
alter table plantaopro.escala_sugestao_medicos add column if not exists cliente_id uuid;
alter table plantaopro.escala_sugestao_medicos add column if not exists sugestao_id uuid;
alter table plantaopro.escala_sugestao_medicos add column if not exists plantao_id uuid;
alter table plantaopro.escala_sugestao_medicos add column if not exists medico_id uuid;
alter table plantaopro.escala_sugestao_medicos add column if not exists score numeric(10,2) default 0;
alter table plantaopro.escala_sugestao_medicos add column if not exists motivos text;
alter table plantaopro.escala_sugestao_medicos add column if not exists alertas text;
alter table plantaopro.escala_sugestao_medicos add column if not exists status varchar(30) default 'SUGERIDO';
alter table plantaopro.escala_sugestao_medicos add column if not exists created_by uuid;
alter table plantaopro.escala_sugestao_medicos add column if not exists updated_by uuid;
alter table plantaopro.escala_sugestao_medicos add column if not exists reg_status char(1) default 'A';
alter table plantaopro.escala_sugestao_medicos add column if not exists reg_date timestamp default now();
alter table plantaopro.escala_sugestao_medicos add column if not exists reg_update timestamp;
create index if not exists ix_escala_sugestao_medicos_tenant_id on plantaopro.escala_sugestao_medicos(tenant_id);
create index if not exists ix_escala_sugestao_medicos_cliente_id on plantaopro.escala_sugestao_medicos(cliente_id);
create index if not exists ix_escala_sugestao_medicos_medico_id on plantaopro.escala_sugestao_medicos(medico_id);
create index if not exists ix_escala_sugestao_medicos_plantao_id on plantaopro.escala_sugestao_medicos(plantao_id);
create index if not exists ix_escala_sugestao_medicos_status on plantaopro.escala_sugestao_medicos(status);
create index if not exists ix_escala_sugestao_medicos_reg_date on plantaopro.escala_sugestao_medicos(reg_date);

create table if not exists plantaopro.escala_sugestao_criterios (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    sugestao_id uuid,
    criterio varchar(80),
    peso numeric(10,2),
    descricao text,
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.escala_sugestao_criterios add column if not exists tenant_id uuid;
alter table plantaopro.escala_sugestao_criterios add column if not exists cliente_id uuid;
alter table plantaopro.escala_sugestao_criterios add column if not exists sugestao_id uuid;
alter table plantaopro.escala_sugestao_criterios add column if not exists criterio varchar(80);
alter table plantaopro.escala_sugestao_criterios add column if not exists peso numeric(10,2);
alter table plantaopro.escala_sugestao_criterios add column if not exists descricao text;
alter table plantaopro.escala_sugestao_criterios add column if not exists created_by uuid;
alter table plantaopro.escala_sugestao_criterios add column if not exists updated_by uuid;
alter table plantaopro.escala_sugestao_criterios add column if not exists reg_status char(1) default 'A';
alter table plantaopro.escala_sugestao_criterios add column if not exists reg_date timestamp default now();
alter table plantaopro.escala_sugestao_criterios add column if not exists reg_update timestamp;
create index if not exists ix_escala_sugestao_criterios_tenant_id on plantaopro.escala_sugestao_criterios(tenant_id);
create index if not exists ix_escala_sugestao_criterios_cliente_id on plantaopro.escala_sugestao_criterios(cliente_id);
create index if not exists ix_escala_sugestao_criterios_reg_date on plantaopro.escala_sugestao_criterios(reg_date);

create table if not exists plantaopro.escala_sugestao_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    sugestao_id uuid,
    plantao_id uuid,
    acao varchar(80),
    detalhes jsonb default '{}'::jsonb,
    usuario_id uuid,
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.escala_sugestao_historico add column if not exists tenant_id uuid;
alter table plantaopro.escala_sugestao_historico add column if not exists cliente_id uuid;
alter table plantaopro.escala_sugestao_historico add column if not exists sugestao_id uuid;
alter table plantaopro.escala_sugestao_historico add column if not exists plantao_id uuid;
alter table plantaopro.escala_sugestao_historico add column if not exists acao varchar(80);
alter table plantaopro.escala_sugestao_historico add column if not exists detalhes jsonb default '{}'::jsonb;
alter table plantaopro.escala_sugestao_historico add column if not exists usuario_id uuid;
alter table plantaopro.escala_sugestao_historico add column if not exists created_by uuid;
alter table plantaopro.escala_sugestao_historico add column if not exists updated_by uuid;
alter table plantaopro.escala_sugestao_historico add column if not exists reg_status char(1) default 'A';
alter table plantaopro.escala_sugestao_historico add column if not exists reg_date timestamp default now();
alter table plantaopro.escala_sugestao_historico add column if not exists reg_update timestamp;
create index if not exists ix_escala_sugestao_historico_tenant_id on plantaopro.escala_sugestao_historico(tenant_id);
create index if not exists ix_escala_sugestao_historico_cliente_id on plantaopro.escala_sugestao_historico(cliente_id);
create index if not exists ix_escala_sugestao_historico_plantao_id on plantaopro.escala_sugestao_historico(plantao_id);
create index if not exists ix_escala_sugestao_historico_reg_date on plantaopro.escala_sugestao_historico(reg_date);

create table if not exists plantaopro.escala_sugestao_feedback (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    sugestao_id uuid,
    medico_id uuid,
    feedback varchar(40),
    observacao text,
    usuario_id uuid,
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.escala_sugestao_feedback add column if not exists tenant_id uuid;
alter table plantaopro.escala_sugestao_feedback add column if not exists cliente_id uuid;
alter table plantaopro.escala_sugestao_feedback add column if not exists sugestao_id uuid;
alter table plantaopro.escala_sugestao_feedback add column if not exists medico_id uuid;
alter table plantaopro.escala_sugestao_feedback add column if not exists feedback varchar(40);
alter table plantaopro.escala_sugestao_feedback add column if not exists observacao text;
alter table plantaopro.escala_sugestao_feedback add column if not exists usuario_id uuid;
alter table plantaopro.escala_sugestao_feedback add column if not exists created_by uuid;
alter table plantaopro.escala_sugestao_feedback add column if not exists updated_by uuid;
alter table plantaopro.escala_sugestao_feedback add column if not exists reg_status char(1) default 'A';
alter table plantaopro.escala_sugestao_feedback add column if not exists reg_date timestamp default now();
alter table plantaopro.escala_sugestao_feedback add column if not exists reg_update timestamp;
create index if not exists ix_escala_sugestao_feedback_tenant_id on plantaopro.escala_sugestao_feedback(tenant_id);
create index if not exists ix_escala_sugestao_feedback_cliente_id on plantaopro.escala_sugestao_feedback(cliente_id);
create index if not exists ix_escala_sugestao_feedback_medico_id on plantaopro.escala_sugestao_feedback(medico_id);
create index if not exists ix_escala_sugestao_feedback_reg_date on plantaopro.escala_sugestao_feedback(reg_date);

create table if not exists plantaopro.substituicoes_plantao (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    plantao_id uuid not null,
    escala_id uuid,
    medico_solicitante_id uuid not null,
    medico_substituto_id uuid,
    motivo text not null,
    status varchar(40) default 'SOLICITADA',
    prazo_minimo_ok boolean default true,
    responsavel_usuario_id uuid,
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.substituicoes_plantao add column if not exists tenant_id uuid;
alter table plantaopro.substituicoes_plantao add column if not exists cliente_id uuid;
alter table plantaopro.substituicoes_plantao add column if not exists plantao_id uuid;
alter table plantaopro.substituicoes_plantao add column if not exists escala_id uuid;
alter table plantaopro.substituicoes_plantao add column if not exists medico_solicitante_id uuid;
alter table plantaopro.substituicoes_plantao add column if not exists medico_substituto_id uuid;
alter table plantaopro.substituicoes_plantao add column if not exists motivo text;
alter table plantaopro.substituicoes_plantao add column if not exists status varchar(40) default 'SOLICITADA';
alter table plantaopro.substituicoes_plantao add column if not exists prazo_minimo_ok boolean default true;
alter table plantaopro.substituicoes_plantao add column if not exists responsavel_usuario_id uuid;
alter table plantaopro.substituicoes_plantao add column if not exists created_by uuid;
alter table plantaopro.substituicoes_plantao add column if not exists updated_by uuid;
alter table plantaopro.substituicoes_plantao add column if not exists reg_status char(1) default 'A';
alter table plantaopro.substituicoes_plantao add column if not exists reg_date timestamp default now();
alter table plantaopro.substituicoes_plantao add column if not exists reg_update timestamp;
create index if not exists ix_substituicoes_plantao_tenant_id on plantaopro.substituicoes_plantao(tenant_id);
create index if not exists ix_substituicoes_plantao_cliente_id on plantaopro.substituicoes_plantao(cliente_id);
create index if not exists ix_substituicoes_plantao_plantao_id on plantaopro.substituicoes_plantao(plantao_id);
create index if not exists ix_substituicoes_plantao_status on plantaopro.substituicoes_plantao(status);
create index if not exists ix_substituicoes_plantao_reg_date on plantaopro.substituicoes_plantao(reg_date);

create table if not exists plantaopro.substituicao_candidatos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    substituicao_id uuid not null,
    medico_id uuid not null,
    score numeric(10,2) default 0,
    status varchar(40) default 'SUGERIDO',
    motivos text,
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.substituicao_candidatos add column if not exists tenant_id uuid;
alter table plantaopro.substituicao_candidatos add column if not exists cliente_id uuid;
alter table plantaopro.substituicao_candidatos add column if not exists substituicao_id uuid;
alter table plantaopro.substituicao_candidatos add column if not exists medico_id uuid;
alter table plantaopro.substituicao_candidatos add column if not exists score numeric(10,2) default 0;
alter table plantaopro.substituicao_candidatos add column if not exists status varchar(40) default 'SUGERIDO';
alter table plantaopro.substituicao_candidatos add column if not exists motivos text;
alter table plantaopro.substituicao_candidatos add column if not exists created_by uuid;
alter table plantaopro.substituicao_candidatos add column if not exists updated_by uuid;
alter table plantaopro.substituicao_candidatos add column if not exists reg_status char(1) default 'A';
alter table plantaopro.substituicao_candidatos add column if not exists reg_date timestamp default now();
alter table plantaopro.substituicao_candidatos add column if not exists reg_update timestamp;
create index if not exists ix_substituicao_candidatos_tenant_id on plantaopro.substituicao_candidatos(tenant_id);
create index if not exists ix_substituicao_candidatos_cliente_id on plantaopro.substituicao_candidatos(cliente_id);
create index if not exists ix_substituicao_candidatos_medico_id on plantaopro.substituicao_candidatos(medico_id);
create index if not exists ix_substituicao_candidatos_status on plantaopro.substituicao_candidatos(status);
create index if not exists ix_substituicao_candidatos_reg_date on plantaopro.substituicao_candidatos(reg_date);

create table if not exists plantaopro.substituicao_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    substituicao_id uuid not null,
    acao varchar(80),
    status_anterior varchar(40),
    status_novo varchar(40),
    observacao text,
    usuario_id uuid,
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.substituicao_historico add column if not exists tenant_id uuid;
alter table plantaopro.substituicao_historico add column if not exists cliente_id uuid;
alter table plantaopro.substituicao_historico add column if not exists substituicao_id uuid;
alter table plantaopro.substituicao_historico add column if not exists acao varchar(80);
alter table plantaopro.substituicao_historico add column if not exists status_anterior varchar(40);
alter table plantaopro.substituicao_historico add column if not exists status_novo varchar(40);
alter table plantaopro.substituicao_historico add column if not exists observacao text;
alter table plantaopro.substituicao_historico add column if not exists usuario_id uuid;
alter table plantaopro.substituicao_historico add column if not exists created_by uuid;
alter table plantaopro.substituicao_historico add column if not exists updated_by uuid;
alter table plantaopro.substituicao_historico add column if not exists reg_status char(1) default 'A';
alter table plantaopro.substituicao_historico add column if not exists reg_date timestamp default now();
alter table plantaopro.substituicao_historico add column if not exists reg_update timestamp;
create index if not exists ix_substituicao_historico_tenant_id on plantaopro.substituicao_historico(tenant_id);
create index if not exists ix_substituicao_historico_cliente_id on plantaopro.substituicao_historico(cliente_id);
create index if not exists ix_substituicao_historico_reg_date on plantaopro.substituicao_historico(reg_date);

create table if not exists plantaopro.substituicao_aprovacoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    substituicao_id uuid not null,
    aprovador_usuario_id uuid,
    decisao varchar(40),
    justificativa text,
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.substituicao_aprovacoes add column if not exists tenant_id uuid;
alter table plantaopro.substituicao_aprovacoes add column if not exists cliente_id uuid;
alter table plantaopro.substituicao_aprovacoes add column if not exists substituicao_id uuid;
alter table plantaopro.substituicao_aprovacoes add column if not exists aprovador_usuario_id uuid;
alter table plantaopro.substituicao_aprovacoes add column if not exists decisao varchar(40);
alter table plantaopro.substituicao_aprovacoes add column if not exists justificativa text;
alter table plantaopro.substituicao_aprovacoes add column if not exists created_by uuid;
alter table plantaopro.substituicao_aprovacoes add column if not exists updated_by uuid;
alter table plantaopro.substituicao_aprovacoes add column if not exists reg_status char(1) default 'A';
alter table plantaopro.substituicao_aprovacoes add column if not exists reg_date timestamp default now();
alter table plantaopro.substituicao_aprovacoes add column if not exists reg_update timestamp;
create index if not exists ix_substituicao_aprovacoes_tenant_id on plantaopro.substituicao_aprovacoes(tenant_id);
create index if not exists ix_substituicao_aprovacoes_cliente_id on plantaopro.substituicao_aprovacoes(cliente_id);
create index if not exists ix_substituicao_aprovacoes_reg_date on plantaopro.substituicao_aprovacoes(reg_date);

create table if not exists plantaopro.comunicacao_templates (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    nome varchar(120),
    tipo varchar(60),
    canal varchar(40) default 'INTERNO',
    assunto varchar(160),
    conteudo text,
    status varchar(30) default 'ATIVO',
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.comunicacao_templates add column if not exists tenant_id uuid;
alter table plantaopro.comunicacao_templates add column if not exists cliente_id uuid;
alter table plantaopro.comunicacao_templates add column if not exists nome varchar(120);
alter table plantaopro.comunicacao_templates add column if not exists tipo varchar(60);
alter table plantaopro.comunicacao_templates add column if not exists canal varchar(40) default 'INTERNO';
alter table plantaopro.comunicacao_templates add column if not exists assunto varchar(160);
alter table plantaopro.comunicacao_templates add column if not exists conteudo text;
alter table plantaopro.comunicacao_templates add column if not exists status varchar(30) default 'ATIVO';
alter table plantaopro.comunicacao_templates add column if not exists created_by uuid;
alter table plantaopro.comunicacao_templates add column if not exists updated_by uuid;
alter table plantaopro.comunicacao_templates add column if not exists reg_status char(1) default 'A';
alter table plantaopro.comunicacao_templates add column if not exists reg_date timestamp default now();
alter table plantaopro.comunicacao_templates add column if not exists reg_update timestamp;
create index if not exists ix_comunicacao_templates_tenant_id on plantaopro.comunicacao_templates(tenant_id);
create index if not exists ix_comunicacao_templates_cliente_id on plantaopro.comunicacao_templates(cliente_id);
create index if not exists ix_comunicacao_templates_status on plantaopro.comunicacao_templates(status);
create index if not exists ix_comunicacao_templates_reg_date on plantaopro.comunicacao_templates(reg_date);

create table if not exists plantaopro.comunicacao_eventos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    conversa_id uuid,
    plantao_id uuid,
    escala_id uuid,
    substituicao_id uuid,
    tipo varchar(80),
    descricao text,
    payload jsonb default '{}'::jsonb,
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.comunicacao_eventos add column if not exists tenant_id uuid;
alter table plantaopro.comunicacao_eventos add column if not exists cliente_id uuid;
alter table plantaopro.comunicacao_eventos add column if not exists conversa_id uuid;
alter table plantaopro.comunicacao_eventos add column if not exists plantao_id uuid;
alter table plantaopro.comunicacao_eventos add column if not exists escala_id uuid;
alter table plantaopro.comunicacao_eventos add column if not exists substituicao_id uuid;
alter table plantaopro.comunicacao_eventos add column if not exists tipo varchar(80);
alter table plantaopro.comunicacao_eventos add column if not exists descricao text;
alter table plantaopro.comunicacao_eventos add column if not exists payload jsonb default '{}'::jsonb;
alter table plantaopro.comunicacao_eventos add column if not exists created_by uuid;
alter table plantaopro.comunicacao_eventos add column if not exists updated_by uuid;
alter table plantaopro.comunicacao_eventos add column if not exists reg_status char(1) default 'A';
alter table plantaopro.comunicacao_eventos add column if not exists reg_date timestamp default now();
alter table plantaopro.comunicacao_eventos add column if not exists reg_update timestamp;
create index if not exists ix_comunicacao_eventos_tenant_id on plantaopro.comunicacao_eventos(tenant_id);
create index if not exists ix_comunicacao_eventos_cliente_id on plantaopro.comunicacao_eventos(cliente_id);
create index if not exists ix_comunicacao_eventos_plantao_id on plantaopro.comunicacao_eventos(plantao_id);
create index if not exists ix_comunicacao_eventos_reg_date on plantaopro.comunicacao_eventos(reg_date);

create table if not exists plantaopro.comunicacao_envios (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    evento_id uuid,
    template_id uuid,
    usuario_id uuid,
    canal varchar(40),
    destino varchar(180),
    status varchar(40) default 'PENDENTE',
    erro text,
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.comunicacao_envios add column if not exists tenant_id uuid;
alter table plantaopro.comunicacao_envios add column if not exists cliente_id uuid;
alter table plantaopro.comunicacao_envios add column if not exists evento_id uuid;
alter table plantaopro.comunicacao_envios add column if not exists template_id uuid;
alter table plantaopro.comunicacao_envios add column if not exists usuario_id uuid;
alter table plantaopro.comunicacao_envios add column if not exists canal varchar(40);
alter table plantaopro.comunicacao_envios add column if not exists destino varchar(180);
alter table plantaopro.comunicacao_envios add column if not exists status varchar(40) default 'PENDENTE';
alter table plantaopro.comunicacao_envios add column if not exists erro text;
alter table plantaopro.comunicacao_envios add column if not exists created_by uuid;
alter table plantaopro.comunicacao_envios add column if not exists updated_by uuid;
alter table plantaopro.comunicacao_envios add column if not exists reg_status char(1) default 'A';
alter table plantaopro.comunicacao_envios add column if not exists reg_date timestamp default now();
alter table plantaopro.comunicacao_envios add column if not exists reg_update timestamp;
create index if not exists ix_comunicacao_envios_tenant_id on plantaopro.comunicacao_envios(tenant_id);
create index if not exists ix_comunicacao_envios_cliente_id on plantaopro.comunicacao_envios(cliente_id);
create index if not exists ix_comunicacao_envios_status on plantaopro.comunicacao_envios(status);
create index if not exists ix_comunicacao_envios_reg_date on plantaopro.comunicacao_envios(reg_date);

create table if not exists plantaopro.comunicacao_leituras (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    envio_id uuid,
    usuario_id uuid,
    lida_em timestamp,
    status varchar(30) default 'LIDA',
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.comunicacao_leituras add column if not exists tenant_id uuid;
alter table plantaopro.comunicacao_leituras add column if not exists cliente_id uuid;
alter table plantaopro.comunicacao_leituras add column if not exists envio_id uuid;
alter table plantaopro.comunicacao_leituras add column if not exists usuario_id uuid;
alter table plantaopro.comunicacao_leituras add column if not exists lida_em timestamp;
alter table plantaopro.comunicacao_leituras add column if not exists status varchar(30) default 'LIDA';
alter table plantaopro.comunicacao_leituras add column if not exists created_by uuid;
alter table plantaopro.comunicacao_leituras add column if not exists updated_by uuid;
alter table plantaopro.comunicacao_leituras add column if not exists reg_status char(1) default 'A';
alter table plantaopro.comunicacao_leituras add column if not exists reg_date timestamp default now();
alter table plantaopro.comunicacao_leituras add column if not exists reg_update timestamp;
create index if not exists ix_comunicacao_leituras_tenant_id on plantaopro.comunicacao_leituras(tenant_id);
create index if not exists ix_comunicacao_leituras_cliente_id on plantaopro.comunicacao_leituras(cliente_id);
create index if not exists ix_comunicacao_leituras_status on plantaopro.comunicacao_leituras(status);
create index if not exists ix_comunicacao_leituras_reg_date on plantaopro.comunicacao_leituras(reg_date);

create table if not exists plantaopro.comunicacao_preferencias_usuario (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    usuario_id uuid not null,
    canal varchar(40),
    tipo varchar(80),
    habilitado boolean default true,
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.comunicacao_preferencias_usuario add column if not exists tenant_id uuid;
alter table plantaopro.comunicacao_preferencias_usuario add column if not exists cliente_id uuid;
alter table plantaopro.comunicacao_preferencias_usuario add column if not exists usuario_id uuid;
alter table plantaopro.comunicacao_preferencias_usuario add column if not exists canal varchar(40);
alter table plantaopro.comunicacao_preferencias_usuario add column if not exists tipo varchar(80);
alter table plantaopro.comunicacao_preferencias_usuario add column if not exists habilitado boolean default true;
alter table plantaopro.comunicacao_preferencias_usuario add column if not exists created_by uuid;
alter table plantaopro.comunicacao_preferencias_usuario add column if not exists updated_by uuid;
alter table plantaopro.comunicacao_preferencias_usuario add column if not exists reg_status char(1) default 'A';
alter table plantaopro.comunicacao_preferencias_usuario add column if not exists reg_date timestamp default now();
alter table plantaopro.comunicacao_preferencias_usuario add column if not exists reg_update timestamp;
create index if not exists ix_comunicacao_preferencias_usuario_tenant_id on plantaopro.comunicacao_preferencias_usuario(tenant_id);
create index if not exists ix_comunicacao_preferencias_usuario_cliente_id on plantaopro.comunicacao_preferencias_usuario(cliente_id);
create index if not exists ix_comunicacao_preferencias_usuario_reg_date on plantaopro.comunicacao_preferencias_usuario(reg_date);

create table if not exists plantaopro.notificacao_regras (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    tipo_evento varchar(80),
    perfil_destino varchar(80),
    canal varchar(40) default 'IN_APP',
    habilitada boolean default true,
    parametros jsonb default '{}'::jsonb,
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.notificacao_regras add column if not exists tenant_id uuid;
alter table plantaopro.notificacao_regras add column if not exists cliente_id uuid;
alter table plantaopro.notificacao_regras add column if not exists tipo_evento varchar(80);
alter table plantaopro.notificacao_regras add column if not exists perfil_destino varchar(80);
alter table plantaopro.notificacao_regras add column if not exists canal varchar(40) default 'IN_APP';
alter table plantaopro.notificacao_regras add column if not exists habilitada boolean default true;
alter table plantaopro.notificacao_regras add column if not exists parametros jsonb default '{}'::jsonb;
alter table plantaopro.notificacao_regras add column if not exists created_by uuid;
alter table plantaopro.notificacao_regras add column if not exists updated_by uuid;
alter table plantaopro.notificacao_regras add column if not exists reg_status char(1) default 'A';
alter table plantaopro.notificacao_regras add column if not exists reg_date timestamp default now();
alter table plantaopro.notificacao_regras add column if not exists reg_update timestamp;
create index if not exists ix_notificacao_regras_tenant_id on plantaopro.notificacao_regras(tenant_id);
create index if not exists ix_notificacao_regras_cliente_id on plantaopro.notificacao_regras(cliente_id);
create index if not exists ix_notificacao_regras_reg_date on plantaopro.notificacao_regras(reg_date);

create table if not exists plantaopro.notificacao_filas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    usuario_id uuid,
    tipo_evento varchar(80),
    titulo varchar(160),
    mensagem text,
    payload jsonb default '{}'::jsonb,
    status varchar(40) default 'PENDENTE',
    tentativas integer default 0,
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.notificacao_filas add column if not exists tenant_id uuid;
alter table plantaopro.notificacao_filas add column if not exists cliente_id uuid;
alter table plantaopro.notificacao_filas add column if not exists usuario_id uuid;
alter table plantaopro.notificacao_filas add column if not exists tipo_evento varchar(80);
alter table plantaopro.notificacao_filas add column if not exists titulo varchar(160);
alter table plantaopro.notificacao_filas add column if not exists mensagem text;
alter table plantaopro.notificacao_filas add column if not exists payload jsonb default '{}'::jsonb;
alter table plantaopro.notificacao_filas add column if not exists status varchar(40) default 'PENDENTE';
alter table plantaopro.notificacao_filas add column if not exists tentativas integer default 0;
alter table plantaopro.notificacao_filas add column if not exists created_by uuid;
alter table plantaopro.notificacao_filas add column if not exists updated_by uuid;
alter table plantaopro.notificacao_filas add column if not exists reg_status char(1) default 'A';
alter table plantaopro.notificacao_filas add column if not exists reg_date timestamp default now();
alter table plantaopro.notificacao_filas add column if not exists reg_update timestamp;
create index if not exists ix_notificacao_filas_tenant_id on plantaopro.notificacao_filas(tenant_id);
create index if not exists ix_notificacao_filas_cliente_id on plantaopro.notificacao_filas(cliente_id);
create index if not exists ix_notificacao_filas_status on plantaopro.notificacao_filas(status);
create index if not exists ix_notificacao_filas_reg_date on plantaopro.notificacao_filas(reg_date);

create table if not exists plantaopro.notificacao_eventos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    tipo_evento varchar(80),
    entidade varchar(80),
    entidade_id uuid,
    payload jsonb default '{}'::jsonb,
    status varchar(40) default 'PROCESSADO',
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.notificacao_eventos add column if not exists tenant_id uuid;
alter table plantaopro.notificacao_eventos add column if not exists cliente_id uuid;
alter table plantaopro.notificacao_eventos add column if not exists tipo_evento varchar(80);
alter table plantaopro.notificacao_eventos add column if not exists entidade varchar(80);
alter table plantaopro.notificacao_eventos add column if not exists entidade_id uuid;
alter table plantaopro.notificacao_eventos add column if not exists payload jsonb default '{}'::jsonb;
alter table plantaopro.notificacao_eventos add column if not exists status varchar(40) default 'PROCESSADO';
alter table plantaopro.notificacao_eventos add column if not exists created_by uuid;
alter table plantaopro.notificacao_eventos add column if not exists updated_by uuid;
alter table plantaopro.notificacao_eventos add column if not exists reg_status char(1) default 'A';
alter table plantaopro.notificacao_eventos add column if not exists reg_date timestamp default now();
alter table plantaopro.notificacao_eventos add column if not exists reg_update timestamp;
create index if not exists ix_notificacao_eventos_tenant_id on plantaopro.notificacao_eventos(tenant_id);
create index if not exists ix_notificacao_eventos_cliente_id on plantaopro.notificacao_eventos(cliente_id);
create index if not exists ix_notificacao_eventos_status on plantaopro.notificacao_eventos(status);
create index if not exists ix_notificacao_eventos_reg_date on plantaopro.notificacao_eventos(reg_date);

create table if not exists plantaopro.notificacao_destinatarios (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    evento_id uuid,
    usuario_id uuid,
    perfil varchar(80),
    status varchar(40) default 'PENDENTE',
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.notificacao_destinatarios add column if not exists tenant_id uuid;
alter table plantaopro.notificacao_destinatarios add column if not exists cliente_id uuid;
alter table plantaopro.notificacao_destinatarios add column if not exists evento_id uuid;
alter table plantaopro.notificacao_destinatarios add column if not exists usuario_id uuid;
alter table plantaopro.notificacao_destinatarios add column if not exists perfil varchar(80);
alter table plantaopro.notificacao_destinatarios add column if not exists status varchar(40) default 'PENDENTE';
alter table plantaopro.notificacao_destinatarios add column if not exists created_by uuid;
alter table plantaopro.notificacao_destinatarios add column if not exists updated_by uuid;
alter table plantaopro.notificacao_destinatarios add column if not exists reg_status char(1) default 'A';
alter table plantaopro.notificacao_destinatarios add column if not exists reg_date timestamp default now();
alter table plantaopro.notificacao_destinatarios add column if not exists reg_update timestamp;
create index if not exists ix_notificacao_destinatarios_tenant_id on plantaopro.notificacao_destinatarios(tenant_id);
create index if not exists ix_notificacao_destinatarios_cliente_id on plantaopro.notificacao_destinatarios(cliente_id);
create index if not exists ix_notificacao_destinatarios_status on plantaopro.notificacao_destinatarios(status);
create index if not exists ix_notificacao_destinatarios_reg_date on plantaopro.notificacao_destinatarios(reg_date);

create table if not exists plantaopro.notificacao_preferencias (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    usuario_id uuid not null,
    tipo_evento varchar(80),
    in_app boolean default true,
    email boolean default false,
    push boolean default false,
    whatsapp boolean default false,
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.notificacao_preferencias add column if not exists tenant_id uuid;
alter table plantaopro.notificacao_preferencias add column if not exists cliente_id uuid;
alter table plantaopro.notificacao_preferencias add column if not exists usuario_id uuid;
alter table plantaopro.notificacao_preferencias add column if not exists tipo_evento varchar(80);
alter table plantaopro.notificacao_preferencias add column if not exists in_app boolean default true;
alter table plantaopro.notificacao_preferencias add column if not exists email boolean default false;
alter table plantaopro.notificacao_preferencias add column if not exists push boolean default false;
alter table plantaopro.notificacao_preferencias add column if not exists whatsapp boolean default false;
alter table plantaopro.notificacao_preferencias add column if not exists created_by uuid;
alter table plantaopro.notificacao_preferencias add column if not exists updated_by uuid;
alter table plantaopro.notificacao_preferencias add column if not exists reg_status char(1) default 'A';
alter table plantaopro.notificacao_preferencias add column if not exists reg_date timestamp default now();
alter table plantaopro.notificacao_preferencias add column if not exists reg_update timestamp;
create index if not exists ix_notificacao_preferencias_tenant_id on plantaopro.notificacao_preferencias(tenant_id);
create index if not exists ix_notificacao_preferencias_cliente_id on plantaopro.notificacao_preferencias(cliente_id);
create index if not exists ix_notificacao_preferencias_reg_date on plantaopro.notificacao_preferencias(reg_date);

create table if not exists plantaopro.pendencias_operacionais (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    tipo varchar(80),
    titulo varchar(180),
    descricao text,
    prioridade varchar(30) default 'MEDIA',
    status varchar(40) default 'ABERTA',
    prazo timestamp,
    responsavel_usuario_id uuid,
    entidade varchar(80),
    entidade_id uuid,
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.pendencias_operacionais add column if not exists tenant_id uuid;
alter table plantaopro.pendencias_operacionais add column if not exists cliente_id uuid;
alter table plantaopro.pendencias_operacionais add column if not exists tipo varchar(80);
alter table plantaopro.pendencias_operacionais add column if not exists titulo varchar(180);
alter table plantaopro.pendencias_operacionais add column if not exists descricao text;
alter table plantaopro.pendencias_operacionais add column if not exists prioridade varchar(30) default 'MEDIA';
alter table plantaopro.pendencias_operacionais add column if not exists status varchar(40) default 'ABERTA';
alter table plantaopro.pendencias_operacionais add column if not exists prazo timestamp;
alter table plantaopro.pendencias_operacionais add column if not exists responsavel_usuario_id uuid;
alter table plantaopro.pendencias_operacionais add column if not exists entidade varchar(80);
alter table plantaopro.pendencias_operacionais add column if not exists entidade_id uuid;
alter table plantaopro.pendencias_operacionais add column if not exists created_by uuid;
alter table plantaopro.pendencias_operacionais add column if not exists updated_by uuid;
alter table plantaopro.pendencias_operacionais add column if not exists reg_status char(1) default 'A';
alter table plantaopro.pendencias_operacionais add column if not exists reg_date timestamp default now();
alter table plantaopro.pendencias_operacionais add column if not exists reg_update timestamp;
create index if not exists ix_pendencias_operacionais_tenant_id on plantaopro.pendencias_operacionais(tenant_id);
create index if not exists ix_pendencias_operacionais_cliente_id on plantaopro.pendencias_operacionais(cliente_id);
create index if not exists ix_pendencias_operacionais_status on plantaopro.pendencias_operacionais(status);
create index if not exists ix_pendencias_operacionais_reg_date on plantaopro.pendencias_operacionais(reg_date);

create table if not exists plantaopro.pendencia_responsaveis (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    pendencia_id uuid not null,
    usuario_id uuid not null,
    papel varchar(60),
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.pendencia_responsaveis add column if not exists tenant_id uuid;
alter table plantaopro.pendencia_responsaveis add column if not exists cliente_id uuid;
alter table plantaopro.pendencia_responsaveis add column if not exists pendencia_id uuid;
alter table plantaopro.pendencia_responsaveis add column if not exists usuario_id uuid;
alter table plantaopro.pendencia_responsaveis add column if not exists papel varchar(60);
alter table plantaopro.pendencia_responsaveis add column if not exists created_by uuid;
alter table plantaopro.pendencia_responsaveis add column if not exists updated_by uuid;
alter table plantaopro.pendencia_responsaveis add column if not exists reg_status char(1) default 'A';
alter table plantaopro.pendencia_responsaveis add column if not exists reg_date timestamp default now();
alter table plantaopro.pendencia_responsaveis add column if not exists reg_update timestamp;
create index if not exists ix_pendencia_responsaveis_tenant_id on plantaopro.pendencia_responsaveis(tenant_id);
create index if not exists ix_pendencia_responsaveis_cliente_id on plantaopro.pendencia_responsaveis(cliente_id);
create index if not exists ix_pendencia_responsaveis_reg_date on plantaopro.pendencia_responsaveis(reg_date);

create table if not exists plantaopro.pendencia_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    pendencia_id uuid not null,
    acao varchar(80),
    status_anterior varchar(40),
    status_novo varchar(40),
    observacao text,
    usuario_id uuid,
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.pendencia_historico add column if not exists tenant_id uuid;
alter table plantaopro.pendencia_historico add column if not exists cliente_id uuid;
alter table plantaopro.pendencia_historico add column if not exists pendencia_id uuid;
alter table plantaopro.pendencia_historico add column if not exists acao varchar(80);
alter table plantaopro.pendencia_historico add column if not exists status_anterior varchar(40);
alter table plantaopro.pendencia_historico add column if not exists status_novo varchar(40);
alter table plantaopro.pendencia_historico add column if not exists observacao text;
alter table plantaopro.pendencia_historico add column if not exists usuario_id uuid;
alter table plantaopro.pendencia_historico add column if not exists created_by uuid;
alter table plantaopro.pendencia_historico add column if not exists updated_by uuid;
alter table plantaopro.pendencia_historico add column if not exists reg_status char(1) default 'A';
alter table plantaopro.pendencia_historico add column if not exists reg_date timestamp default now();
alter table plantaopro.pendencia_historico add column if not exists reg_update timestamp;
create index if not exists ix_pendencia_historico_tenant_id on plantaopro.pendencia_historico(tenant_id);
create index if not exists ix_pendencia_historico_cliente_id on plantaopro.pendencia_historico(cliente_id);
create index if not exists ix_pendencia_historico_reg_date on plantaopro.pendencia_historico(reg_date);

create table if not exists plantaopro.pendencia_checklists (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    pendencia_id uuid not null,
    item varchar(180),
    concluido boolean default false,
    concluido_por uuid,
    concluido_em timestamp,
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.pendencia_checklists add column if not exists tenant_id uuid;
alter table plantaopro.pendencia_checklists add column if not exists cliente_id uuid;
alter table plantaopro.pendencia_checklists add column if not exists pendencia_id uuid;
alter table plantaopro.pendencia_checklists add column if not exists item varchar(180);
alter table plantaopro.pendencia_checklists add column if not exists concluido boolean default false;
alter table plantaopro.pendencia_checklists add column if not exists concluido_por uuid;
alter table plantaopro.pendencia_checklists add column if not exists concluido_em timestamp;
alter table plantaopro.pendencia_checklists add column if not exists created_by uuid;
alter table plantaopro.pendencia_checklists add column if not exists updated_by uuid;
alter table plantaopro.pendencia_checklists add column if not exists reg_status char(1) default 'A';
alter table plantaopro.pendencia_checklists add column if not exists reg_date timestamp default now();
alter table plantaopro.pendencia_checklists add column if not exists reg_update timestamp;
create index if not exists ix_pendencia_checklists_tenant_id on plantaopro.pendencia_checklists(tenant_id);
create index if not exists ix_pendencia_checklists_cliente_id on plantaopro.pendencia_checklists(cliente_id);
create index if not exists ix_pendencia_checklists_reg_date on plantaopro.pendencia_checklists(reg_date);

create table if not exists plantaopro.relatorios_modelos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    nome varchar(140),
    tipo varchar(60),
    descricao text,
    configuracao jsonb default '{}'::jsonb,
    status varchar(30) default 'ATIVO',
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.relatorios_modelos add column if not exists tenant_id uuid;
alter table plantaopro.relatorios_modelos add column if not exists cliente_id uuid;
alter table plantaopro.relatorios_modelos add column if not exists nome varchar(140);
alter table plantaopro.relatorios_modelos add column if not exists tipo varchar(60);
alter table plantaopro.relatorios_modelos add column if not exists descricao text;
alter table plantaopro.relatorios_modelos add column if not exists configuracao jsonb default '{}'::jsonb;
alter table plantaopro.relatorios_modelos add column if not exists status varchar(30) default 'ATIVO';
alter table plantaopro.relatorios_modelos add column if not exists created_by uuid;
alter table plantaopro.relatorios_modelos add column if not exists updated_by uuid;
alter table plantaopro.relatorios_modelos add column if not exists reg_status char(1) default 'A';
alter table plantaopro.relatorios_modelos add column if not exists reg_date timestamp default now();
alter table plantaopro.relatorios_modelos add column if not exists reg_update timestamp;
create index if not exists ix_relatorios_modelos_tenant_id on plantaopro.relatorios_modelos(tenant_id);
create index if not exists ix_relatorios_modelos_cliente_id on plantaopro.relatorios_modelos(cliente_id);
create index if not exists ix_relatorios_modelos_status on plantaopro.relatorios_modelos(status);
create index if not exists ix_relatorios_modelos_reg_date on plantaopro.relatorios_modelos(reg_date);

create table if not exists plantaopro.relatorios_execucoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    modelo_id uuid,
    tipo varchar(60),
    parametros jsonb default '{}'::jsonb,
    status varchar(40) default 'PROCESSADO',
    executado_por uuid,
    total_linhas integer default 0,
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.relatorios_execucoes add column if not exists tenant_id uuid;
alter table plantaopro.relatorios_execucoes add column if not exists cliente_id uuid;
alter table plantaopro.relatorios_execucoes add column if not exists modelo_id uuid;
alter table plantaopro.relatorios_execucoes add column if not exists tipo varchar(60);
alter table plantaopro.relatorios_execucoes add column if not exists parametros jsonb default '{}'::jsonb;
alter table plantaopro.relatorios_execucoes add column if not exists status varchar(40) default 'PROCESSADO';
alter table plantaopro.relatorios_execucoes add column if not exists executado_por uuid;
alter table plantaopro.relatorios_execucoes add column if not exists total_linhas integer default 0;
alter table plantaopro.relatorios_execucoes add column if not exists created_by uuid;
alter table plantaopro.relatorios_execucoes add column if not exists updated_by uuid;
alter table plantaopro.relatorios_execucoes add column if not exists reg_status char(1) default 'A';
alter table plantaopro.relatorios_execucoes add column if not exists reg_date timestamp default now();
alter table plantaopro.relatorios_execucoes add column if not exists reg_update timestamp;
create index if not exists ix_relatorios_execucoes_tenant_id on plantaopro.relatorios_execucoes(tenant_id);
create index if not exists ix_relatorios_execucoes_cliente_id on plantaopro.relatorios_execucoes(cliente_id);
create index if not exists ix_relatorios_execucoes_status on plantaopro.relatorios_execucoes(status);
create index if not exists ix_relatorios_execucoes_reg_date on plantaopro.relatorios_execucoes(reg_date);

create table if not exists plantaopro.relatorios_agendamentos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    modelo_id uuid,
    nome varchar(140),
    periodicidade varchar(40),
    proxima_execucao timestamp,
    status varchar(30) default 'ATIVO',
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.relatorios_agendamentos add column if not exists tenant_id uuid;
alter table plantaopro.relatorios_agendamentos add column if not exists cliente_id uuid;
alter table plantaopro.relatorios_agendamentos add column if not exists modelo_id uuid;
alter table plantaopro.relatorios_agendamentos add column if not exists nome varchar(140);
alter table plantaopro.relatorios_agendamentos add column if not exists periodicidade varchar(40);
alter table plantaopro.relatorios_agendamentos add column if not exists proxima_execucao timestamp;
alter table plantaopro.relatorios_agendamentos add column if not exists status varchar(30) default 'ATIVO';
alter table plantaopro.relatorios_agendamentos add column if not exists created_by uuid;
alter table plantaopro.relatorios_agendamentos add column if not exists updated_by uuid;
alter table plantaopro.relatorios_agendamentos add column if not exists reg_status char(1) default 'A';
alter table plantaopro.relatorios_agendamentos add column if not exists reg_date timestamp default now();
alter table plantaopro.relatorios_agendamentos add column if not exists reg_update timestamp;
create index if not exists ix_relatorios_agendamentos_tenant_id on plantaopro.relatorios_agendamentos(tenant_id);
create index if not exists ix_relatorios_agendamentos_cliente_id on plantaopro.relatorios_agendamentos(cliente_id);
create index if not exists ix_relatorios_agendamentos_status on plantaopro.relatorios_agendamentos(status);
create index if not exists ix_relatorios_agendamentos_reg_date on plantaopro.relatorios_agendamentos(reg_date);

create table if not exists plantaopro.relatorios_exportacoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    execucao_id uuid,
    tipo varchar(60),
    formato varchar(20) default 'CSV',
    arquivo_nome varchar(220),
    exportado_por uuid,
    total_linhas integer default 0,
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.relatorios_exportacoes add column if not exists tenant_id uuid;
alter table plantaopro.relatorios_exportacoes add column if not exists cliente_id uuid;
alter table plantaopro.relatorios_exportacoes add column if not exists execucao_id uuid;
alter table plantaopro.relatorios_exportacoes add column if not exists tipo varchar(60);
alter table plantaopro.relatorios_exportacoes add column if not exists formato varchar(20) default 'CSV';
alter table plantaopro.relatorios_exportacoes add column if not exists arquivo_nome varchar(220);
alter table plantaopro.relatorios_exportacoes add column if not exists exportado_por uuid;
alter table plantaopro.relatorios_exportacoes add column if not exists total_linhas integer default 0;
alter table plantaopro.relatorios_exportacoes add column if not exists created_by uuid;
alter table plantaopro.relatorios_exportacoes add column if not exists updated_by uuid;
alter table plantaopro.relatorios_exportacoes add column if not exists reg_status char(1) default 'A';
alter table plantaopro.relatorios_exportacoes add column if not exists reg_date timestamp default now();
alter table plantaopro.relatorios_exportacoes add column if not exists reg_update timestamp;
create index if not exists ix_relatorios_exportacoes_tenant_id on plantaopro.relatorios_exportacoes(tenant_id);
create index if not exists ix_relatorios_exportacoes_cliente_id on plantaopro.relatorios_exportacoes(cliente_id);
create index if not exists ix_relatorios_exportacoes_reg_date on plantaopro.relatorios_exportacoes(reg_date);

create table if not exists plantaopro.relatorios_filtros_salvos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    usuario_id uuid,
    nome varchar(120),
    tipo varchar(60),
    filtros jsonb default '{}'::jsonb,
    status varchar(30) default 'ATIVO',
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.relatorios_filtros_salvos add column if not exists tenant_id uuid;
alter table plantaopro.relatorios_filtros_salvos add column if not exists cliente_id uuid;
alter table plantaopro.relatorios_filtros_salvos add column if not exists usuario_id uuid;
alter table plantaopro.relatorios_filtros_salvos add column if not exists nome varchar(120);
alter table plantaopro.relatorios_filtros_salvos add column if not exists tipo varchar(60);
alter table plantaopro.relatorios_filtros_salvos add column if not exists filtros jsonb default '{}'::jsonb;
alter table plantaopro.relatorios_filtros_salvos add column if not exists status varchar(30) default 'ATIVO';
alter table plantaopro.relatorios_filtros_salvos add column if not exists created_by uuid;
alter table plantaopro.relatorios_filtros_salvos add column if not exists updated_by uuid;
alter table plantaopro.relatorios_filtros_salvos add column if not exists reg_status char(1) default 'A';
alter table plantaopro.relatorios_filtros_salvos add column if not exists reg_date timestamp default now();
alter table plantaopro.relatorios_filtros_salvos add column if not exists reg_update timestamp;
create index if not exists ix_relatorios_filtros_salvos_tenant_id on plantaopro.relatorios_filtros_salvos(tenant_id);
create index if not exists ix_relatorios_filtros_salvos_cliente_id on plantaopro.relatorios_filtros_salvos(cliente_id);
create index if not exists ix_relatorios_filtros_salvos_status on plantaopro.relatorios_filtros_salvos(status);
create index if not exists ix_relatorios_filtros_salvos_reg_date on plantaopro.relatorios_filtros_salvos(reg_date);

do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'uk_medico_preferencias_plantao_medico') then
        alter table plantaopro.medico_preferencias_plantao add constraint uk_medico_preferencias_plantao_medico unique (medico_id);
    end if;
end $$;
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'uk_notificacao_preferencias_usuario_tipo') then
        alter table plantaopro.notificacao_preferencias add constraint uk_notificacao_preferencias_usuario_tipo unique (usuario_id, tipo_evento);
    end if;
end $$;

create table if not exists plantaopro.recursos_premium_planos (
    id uuid primary key default gen_random_uuid(),
    plano_slug varchar(60) not null,
    recurso_codigo varchar(100) not null,
    nome varchar(160) not null,
    descricao text,
    habilitado boolean not null default true,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.recursos_premium_planos add column if not exists plano_slug varchar(60);
alter table plantaopro.recursos_premium_planos add column if not exists recurso_codigo varchar(100);
alter table plantaopro.recursos_premium_planos add column if not exists nome varchar(160);
alter table plantaopro.recursos_premium_planos add column if not exists descricao text;
alter table plantaopro.recursos_premium_planos add column if not exists habilitado boolean default true;
alter table plantaopro.recursos_premium_planos add column if not exists reg_status char(1) default 'A';
alter table plantaopro.recursos_premium_planos add column if not exists reg_date timestamp default now();
alter table plantaopro.recursos_premium_planos add column if not exists reg_update timestamp;
create index if not exists ix_recursos_premium_planos_plano_slug on plantaopro.recursos_premium_planos(plano_slug);
create index if not exists ix_recursos_premium_planos_recurso_codigo on plantaopro.recursos_premium_planos(recurso_codigo);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'uk_recursos_premium_planos_slug_recurso') then
        alter table plantaopro.recursos_premium_planos add constraint uk_recursos_premium_planos_slug_recurso unique (plano_slug, recurso_codigo);
    end if;
end $$;

insert into plantaopro.recursos_premium_planos(plano_slug,recurso_codigo,nome,descricao,habilitado)
values
('essencial','plantoes','Plantões','Plantões, escalas básicas, médicos, hospitais e convites básicos.',true),
('essencial','relatorios_basicos','Relatórios básicos','Relatórios operacionais básicos.',true),
('profissional','disponibilidade_medica','Disponibilidade médica','Agenda de disponibilidade, indisponibilidade e preferências.',true),
('profissional','sugestao_medicos','Sugestão de médicos','Motor determinístico de sugestão de médicos.',true),
('profissional','substituicao_plantao','Substituição de plantão','Solicitação, aprovação e confirmação de substitutos.',true),
('profissional','comunicacao_operacional','Comunicação operacional','Conversas internas e templates.',true),
('profissional','notificacoes_inteligentes','Notificações inteligentes','Eventos, filas e preferências.',true),
('enterprise','pendencias_operacionais','Pendências operacionais','Gestão priorizada de pendências e checklists.',true),
('enterprise','relatorios_executivos','Relatórios executivos','Relatórios avançados e exportações auditadas.',true),
('enterprise','automacao_escala','Automação de escala','Recursos avançados de automação operacional.',true),
('revendedor','portal_parceiro','Portal parceiro','Revenda, comissões e tenants vinculados.',true)
on conflict (plano_slug,recurso_codigo) do update set nome=excluded.nome, descricao=excluded.descricao, habilitado=excluded.habilitado, reg_update=now();

-- Consolidação das conversas internas usadas pelo módulo de comunicação operacional.
create table if not exists plantaopro.conversas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    titulo varchar(180) not null,
    tipo varchar(40) not null,
    entidade varchar(80),
    entidade_id uuid,
    status varchar(30) not null default 'ABERTA',
    created_by uuid,
    updated_by uuid,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp
);
alter table plantaopro.conversas add column if not exists tenant_id uuid;
alter table plantaopro.conversas add column if not exists cliente_id uuid;
alter table plantaopro.conversas add column if not exists created_by uuid;
alter table plantaopro.conversas add column if not exists updated_by uuid;
alter table plantaopro.conversas add column if not exists reg_update timestamp;
create index if not exists ix_conversas_tenant_id on plantaopro.conversas(tenant_id);
create index if not exists ix_conversas_cliente_id on plantaopro.conversas(cliente_id);
create index if not exists ix_conversas_status on plantaopro.conversas(status);
create index if not exists ix_conversas_reg_date on plantaopro.conversas(reg_date);

create table if not exists plantaopro.conversa_participantes (
    id uuid primary key default gen_random_uuid(),
    conversa_id uuid not null,
    usuario_id uuid not null,
    papel varchar(30),
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);
create index if not exists ix_conversa_participantes_conversa_id on plantaopro.conversa_participantes(conversa_id);
create index if not exists ix_conversa_participantes_usuario_id on plantaopro.conversa_participantes(usuario_id);

create table if not exists plantaopro.mensagens (
    id uuid primary key default gen_random_uuid(),
    conversa_id uuid not null,
    remetente_usuario_id uuid not null,
    mensagem text not null,
    tipo varchar(30) not null default 'TEXTO',
    anexo_url text,
    lida boolean not null default false,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);
create index if not exists ix_mensagens_conversa_id on plantaopro.mensagens(conversa_id);
create index if not exists ix_mensagens_remetente_usuario_id on plantaopro.mensagens(remetente_usuario_id);
create index if not exists ix_mensagens_reg_date on plantaopro.mensagens(reg_date);

create table if not exists plantaopro.mensagem_leituras (
    id uuid primary key default gen_random_uuid(),
    mensagem_id uuid not null,
    usuario_id uuid not null,
    lida_em timestamp not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_mensagem_leituras_mensagem_id on plantaopro.mensagem_leituras(mensagem_id);
create index if not exists ix_mensagem_leituras_usuario_id on plantaopro.mensagem_leituras(usuario_id);
