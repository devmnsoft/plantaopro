-- v2.15.9 - invariantes concorrentes da jornada Saúde 360.
-- Incremental e idempotente: não altera migrations já aplicadas.
set search_path to plantaopro, public;

create extension if not exists btree_gist;

alter table plantaopro.pacientes
    add column if not exists cliente_id uuid,
    add column if not exists cpf text,
    add column if not exists reg_status char(1) not null default 'A',
    add column if not exists reg_date timestamptz not null default now();

alter table plantaopro.agendamentos
    add column if not exists cliente_id uuid,
    add column if not exists paciente_id uuid,
    add column if not exists medico_id uuid,
    add column if not exists unidade_id uuid,
    add column if not exists data_inicio timestamptz,
    add column if not exists data_fim timestamptz,
    add column if not exists reg_status char(1) not null default 'A',
    add column if not exists reg_date timestamptz not null default now();

create table if not exists plantaopro.agendamento_checkins (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    tenant_id uuid null,
    agendamento_id uuid not null,
    paciente_id uuid null,
    usuario_id uuid null,
    observacoes text null,
    status text not null default 'REALIZADO',
    created_by uuid null,
    updated_by uuid null,
    reg_update timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.painel_chamada_fila (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    tenant_id uuid null,
    painel_id uuid null,
    paciente_id uuid null,
    agendamento_id uuid null,
    triagem_id uuid null,
    atendimento_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    senha text null,
    paciente_nome text null,
    status text not null default 'AGUARDANDO',
    prioridade int not null default 0,
    chamada_em timestamptz null,
    created_by uuid null,
    updated_by uuid null,
    reg_update timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.triagem_fila (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    tenant_id uuid null,
    paciente_id uuid null,
    agendamento_id uuid null,
    atendimento_id uuid null,
    status text not null default 'AGUARDANDO',
    prioridade int not null default 0,
    created_by uuid null,
    updated_by uuid null,
    reg_update timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);

-- CPF é opcional, porém único por cliente quando informado e normalizado.
create unique index if not exists ux_v2159_paciente_cpf_cliente
    on plantaopro.pacientes (cliente_id, regexp_replace(cpf, '[^0-9]', '', 'g'))
    where reg_status = 'A' and cpf is not null and regexp_replace(cpf, '[^0-9]', '', 'g') <> '';

-- O banco, e não somente a validação prévia da API, arbitra reservas concorrentes.
do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'ck_v2159_agendamento_periodo' and conrelid = 'plantaopro.agendamentos'::regclass) then
        alter table plantaopro.agendamentos add constraint ck_v2159_agendamento_periodo check (data_fim > data_inicio) not valid;
    end if;
    if not exists (select 1 from pg_constraint where conname = 'ex_v2159_agendamento_medico' and conrelid = 'plantaopro.agendamentos'::regclass) then
        alter table plantaopro.agendamentos add constraint ex_v2159_agendamento_medico
            exclude using gist (cliente_id with =, medico_id with =, tstzrange(data_inicio, data_fim, '[)') with &&)
            where (reg_status = 'A' and status not in ('CANCELADO','REAGENDADO','FALTOU'));
    end if;
end $$;

-- Repetições/retries não podem gerar duas chegadas ou duas posições ativas.
create unique index if not exists ux_v2159_checkin_ativo
    on plantaopro.agendamento_checkins (cliente_id, agendamento_id) where reg_status = 'A';
create unique index if not exists ux_v2159_fila_painel_ativa
    on plantaopro.painel_chamada_fila (cliente_id, agendamento_id) where reg_status = 'A' and agendamento_id is not null;
create unique index if not exists ux_v2159_fila_triagem_ativa
    on plantaopro.triagem_fila (cliente_id, agendamento_id) where reg_status = 'A' and agendamento_id is not null;

create index if not exists ix_v2159_recepcao_dia
    on plantaopro.agendamentos (cliente_id, unidade_id, data_inicio, id)
    where reg_status = 'A';
create index if not exists ix_v2159_fila_ordenacao
    on plantaopro.painel_chamada_fila (cliente_id, status, prioridade desc, reg_date, id)
    where reg_status = 'A';
