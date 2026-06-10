create schema if not exists plantaopro;
create extension if not exists pgcrypto;
create sequence if not exists plantaopro.seq_painel_senhas start 1;

-- PlantãoPro Saúde 360 - base clínica inicial idempotente.
-- LGPD: tabelas técnicas e de auditoria guardam metadados; dados clínicos sensíveis ficam restritos às tabelas clínicas por tenant.

create table if not exists plantaopro.pacientes (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    tenant_id uuid null,
    nome text not null,
    nome_social text null,
    data_nascimento date null,
    sexo_genero text null,
    cpf text null,
    cns text null,
    documento_alternativo text null,
    telefone text null,
    email text null,
    endereco text null,
    responsavel_nome text null,
    observacoes text null,
    consentimento_lgpd boolean not null default false,
    consentimento_lgpd_em timestamptz null,
    consentimento_lgpd_canal text null,
    finalidade_tratamento text not null default 'ASSISTENCIA_SAUDE',
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_update timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);

alter table plantaopro.pacientes add column if not exists tenant_id uuid null;
alter table plantaopro.pacientes add column if not exists nome_social text null;
alter table plantaopro.pacientes add column if not exists data_nascimento date null;
alter table plantaopro.pacientes add column if not exists sexo_genero text null;
alter table plantaopro.pacientes add column if not exists cpf text null;
alter table plantaopro.pacientes add column if not exists cns text null;
alter table plantaopro.pacientes add column if not exists documento_alternativo text null;
alter table plantaopro.pacientes add column if not exists telefone text null;
alter table plantaopro.pacientes add column if not exists email text null;
alter table plantaopro.pacientes add column if not exists endereco text null;
alter table plantaopro.pacientes add column if not exists responsavel_nome text null;
alter table plantaopro.pacientes add column if not exists observacoes text null;
alter table plantaopro.pacientes add column if not exists consentimento_lgpd boolean not null default false;
alter table plantaopro.pacientes add column if not exists consentimento_lgpd_em timestamptz null;
alter table plantaopro.pacientes add column if not exists consentimento_lgpd_canal text null;
alter table plantaopro.pacientes add column if not exists finalidade_tratamento text not null default 'ASSISTENCIA_SAUDE';
alter table plantaopro.pacientes add column if not exists status text not null default 'ATIVO';
alter table plantaopro.pacientes add column if not exists created_by uuid null;
alter table plantaopro.pacientes add column if not exists updated_by uuid null;
alter table plantaopro.pacientes add column if not exists created_at timestamptz not null default now();
alter table plantaopro.pacientes add column if not exists updated_at timestamptz null;
alter table plantaopro.pacientes add column if not exists reg_update timestamptz null;
alter table plantaopro.pacientes add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.pacientes add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.paciente_contatos (
    id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, paciente_id uuid not null,
    nome text null, tipo text not null default 'PRINCIPAL', telefone text null, email text null,
    status text not null default 'ATIVO', created_by uuid null, updated_by uuid null,
    created_at timestamptz not null default now(), updated_at timestamptz null, reg_update timestamptz null,
    reg_date timestamptz not null default now(), reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.paciente_enderecos (
    id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, paciente_id uuid not null,
    logradouro text null, numero text null, complemento text null, bairro text null, cidade text null, estado text null, cep text null,
    status text not null default 'ATIVO', created_by uuid null, updated_by uuid null,
    created_at timestamptz not null default now(), updated_at timestamptz null, reg_update timestamptz null,
    reg_date timestamptz not null default now(), reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.paciente_documentos (
    id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, paciente_id uuid not null,
    tipo text not null, numero text not null, emissor text null, validade date null,
    status text not null default 'ATIVO', created_by uuid null, updated_by uuid null,
    created_at timestamptz not null default now(), updated_at timestamptz null, reg_update timestamptz null,
    reg_date timestamptz not null default now(), reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.paciente_historico (
    id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, paciente_id uuid not null,
    acao text not null, detalhes jsonb not null default '{}'::jsonb, usuario_id uuid null,
    status text not null default 'REGISTRADO', created_by uuid null, updated_by uuid null,
    created_at timestamptz not null default now(), updated_at timestamptz null, reg_update timestamptz null,
    reg_date timestamptz not null default now(), reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.paciente_consentimentos (
    id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, paciente_id uuid not null,
    finalidade text not null, concedido boolean not null default true, canal text null, ip text null, usuario_id uuid null,
    status text not null default 'VIGENTE', created_by uuid null, updated_by uuid null,
    created_at timestamptz not null default now(), updated_at timestamptz null, reg_update timestamptz null,
    reg_date timestamptz not null default now(), reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.agendamentos (
    id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null,
    paciente_id uuid not null, medico_id uuid not null, unidade_id uuid null, sala_id uuid null,
    data_inicio timestamptz not null, data_fim timestamptz not null,
    tipo text not null default 'CONSULTA', especialidade text null, observacoes text null, valor numeric(12,2) not null default 0,
    status text not null default 'AGENDADO', created_by uuid null, updated_by uuid null,
    created_at timestamptz not null default now(), updated_at timestamptz null, reg_update timestamptz null,
    reg_date timestamptz not null default now(), reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.agendamento_checkins (
    id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null,
    agendamento_id uuid not null, paciente_id uuid null, usuario_id uuid null, observacoes text null,
    status text not null default 'REALIZADO', created_by uuid null, updated_by uuid null,
    created_at timestamptz not null default now(), updated_at timestamptz null, reg_update timestamptz null,
    reg_date timestamptz not null default now(), reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.agendamento_historico (
    id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null,
    agendamento_id uuid not null, acao text not null, detalhes jsonb not null default '{}'::jsonb, usuario_id uuid null,
    status text not null default 'REGISTRADO', created_by uuid null, updated_by uuid null,
    created_at timestamptz not null default now(), updated_at timestamptz null, reg_update timestamptz null,
    reg_date timestamptz not null default now(), reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.painel_chamada (
    id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null,
    paciente_id uuid null, medico_id uuid null, agendamento_id uuid null, sala_id uuid null,
    senha text null, paciente_nome text null, status text not null default 'AGUARDANDO', created_by uuid null, updated_by uuid null,
    created_at timestamptz not null default now(), updated_at timestamptz null, reg_update timestamptz null,
    reg_date timestamptz not null default now(), reg_status char(1) not null default 'A'
);

-- Compatibilidade com serviços já existentes que usam painel_chamada_fila.
create table if not exists plantaopro.painel_chamada_fila (
    id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null,
    painel_id uuid null, paciente_id uuid null, agendamento_id uuid null, setor_id uuid null, sala_id uuid null, guiche_id uuid null,
    senha text null, paciente_nome text null, status text not null default 'AGUARDANDO', created_by uuid null, updated_by uuid null,
    created_at timestamptz not null default now(), updated_at timestamptz null, reg_update timestamptz null,
    reg_date timestamptz not null default now(), reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.triagens (
    id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null,
    paciente_id uuid not null, agendamento_id uuid null, enfermeiro_id uuid null,
    classificacao_risco text null, queixa_principal text null,
    pressao_sistolica integer null, pressao_diastolica integer null, frequencia_cardiaca integer null,
    frequencia_respiratoria integer null, temperatura numeric(4,1) null, saturacao integer null,
    peso numeric(6,2) null, altura numeric(4,2) null, imc numeric(6,2) null, glicemia numeric(6,2) null,
    alergias_relatadas text null, medicamentos_uso text null, observacoes text null,
    status text not null default 'AGUARDANDO', created_by uuid null, updated_by uuid null,
    created_at timestamptz not null default now(), updated_at timestamptz null, reg_update timestamptz null,
    reg_date timestamptz not null default now(), reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.triagem_fila (
    id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null,
    paciente_id uuid not null, agendamento_id uuid null, status text not null default 'AGUARDANDO', created_by uuid null, updated_by uuid null,
    created_at timestamptz not null default now(), updated_at timestamptz null, reg_update timestamptz null,
    reg_date timestamptz not null default now(), reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.triagem_historico (
    id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null,
    triagem_id uuid not null, acao text not null, detalhes jsonb not null default '{}'::jsonb, usuario_id uuid null,
    status text not null default 'REGISTRADO', created_by uuid null, updated_by uuid null,
    created_at timestamptz not null default now(), updated_at timestamptz null, reg_update timestamptz null,
    reg_date timestamptz not null default now(), reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.triagem_encaminhamentos (
    id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null,
    triagem_id uuid not null, paciente_id uuid null, agendamento_id uuid null, destino text not null default 'CONSULTA',
    status text not null default 'ENCAMINHADA', created_by uuid null, updated_by uuid null,
    created_at timestamptz not null default now(), updated_at timestamptz null, reg_update timestamptz null,
    reg_date timestamptz not null default now(), reg_status char(1) not null default 'A'
);

-- Compatibilidade observabilidade: error_message nunca nulo quando a coluna existir.
do $$
begin
    if to_regclass('plantaopro.api_request_logs') is not null then
        alter table plantaopro.api_request_logs add column if not exists error_message text not null default '';
        update plantaopro.api_request_logs set error_message = '' where error_message is null;
        alter table plantaopro.api_request_logs alter column error_message set not null, alter column error_message set default '';
    end if;

    if to_regclass('plantaopro.api_error_logs') is not null then
        alter table plantaopro.api_error_logs add column if not exists error_message text not null default '';
        update plantaopro.api_error_logs set error_message = '' where error_message is null;
        alter table plantaopro.api_error_logs alter column error_message set not null, alter column error_message set default '';
    end if;
end $$;

create unique index if not exists ux_pacientes_cliente_cpf_informado on plantaopro.pacientes (cliente_id, regexp_replace(coalesce(cpf,''), '[^0-9]', '', 'g')) where cpf is not null and btrim(cpf) <> '' and reg_status='A';
create index if not exists ix_pacientes_cliente_id on plantaopro.pacientes(cliente_id);
create index if not exists ix_pacientes_tenant_id on plantaopro.pacientes(tenant_id);
create index if not exists ix_pacientes_status on plantaopro.pacientes(status);
create index if not exists ix_paciente_contatos_tenant_id on plantaopro.paciente_contatos(tenant_id);
create index if not exists ix_paciente_contatos_paciente_id on plantaopro.paciente_contatos(paciente_id);
create index if not exists ix_paciente_contatos_status on plantaopro.paciente_contatos(status);
create index if not exists ix_paciente_enderecos_tenant_id on plantaopro.paciente_enderecos(tenant_id);
create index if not exists ix_paciente_enderecos_paciente_id on plantaopro.paciente_enderecos(paciente_id);
create index if not exists ix_paciente_enderecos_status on plantaopro.paciente_enderecos(status);
create index if not exists ix_paciente_documentos_tenant_id on plantaopro.paciente_documentos(tenant_id);
create index if not exists ix_paciente_documentos_paciente_id on plantaopro.paciente_documentos(paciente_id);
create index if not exists ix_paciente_documentos_status on plantaopro.paciente_documentos(status);
create index if not exists ix_paciente_historico_tenant_id on plantaopro.paciente_historico(tenant_id);
create index if not exists ix_paciente_historico_paciente_id on plantaopro.paciente_historico(paciente_id);
create index if not exists ix_agendamentos_cliente_id on plantaopro.agendamentos(cliente_id);
create index if not exists ix_agendamentos_tenant_id on plantaopro.agendamentos(tenant_id);
create index if not exists ix_agendamentos_paciente_id on plantaopro.agendamentos(paciente_id);
create index if not exists ix_agendamentos_medico_id on plantaopro.agendamentos(medico_id);
create index if not exists ix_agendamentos_status on plantaopro.agendamentos(status);
create index if not exists ix_agendamentos_periodo on plantaopro.agendamentos(cliente_id, medico_id, data_inicio, data_fim);
create index if not exists ix_agendamento_checkins_tenant_id on plantaopro.agendamento_checkins(tenant_id);
create index if not exists ix_agendamento_checkins_agendamento_id on plantaopro.agendamento_checkins(agendamento_id);
create index if not exists ix_agendamento_checkins_paciente_id on plantaopro.agendamento_checkins(paciente_id);
create index if not exists ix_agendamento_checkins_status on plantaopro.agendamento_checkins(status);
create index if not exists ix_painel_chamada_tenant_id on plantaopro.painel_chamada(tenant_id);
create index if not exists ix_painel_chamada_paciente_id on plantaopro.painel_chamada(paciente_id);
create index if not exists ix_painel_chamada_medico_id on plantaopro.painel_chamada(medico_id);
create index if not exists ix_painel_chamada_agendamento_id on plantaopro.painel_chamada(agendamento_id);
create index if not exists ix_painel_chamada_status on plantaopro.painel_chamada(status);
create index if not exists ix_painel_fila_tenant_id on plantaopro.painel_chamada_fila(tenant_id);
create index if not exists ix_painel_fila_paciente_id on plantaopro.painel_chamada_fila(paciente_id);
create index if not exists ix_painel_fila_agendamento_id on plantaopro.painel_chamada_fila(agendamento_id);
create index if not exists ix_painel_fila_status on plantaopro.painel_chamada_fila(status);
create index if not exists ix_triagens_cliente_id on plantaopro.triagens(cliente_id);
create index if not exists ix_triagens_tenant_id on plantaopro.triagens(tenant_id);
create index if not exists ix_triagens_paciente_id on plantaopro.triagens(paciente_id);
create index if not exists ix_triagens_agendamento_id on plantaopro.triagens(agendamento_id);
create index if not exists ix_triagens_status on plantaopro.triagens(status);
create index if not exists ix_triagem_fila_agendamento_id on plantaopro.triagem_fila(agendamento_id);
create index if not exists ix_triagem_fila_paciente_id on plantaopro.triagem_fila(paciente_id);
create index if not exists ix_triagem_fila_status on plantaopro.triagem_fila(status);
