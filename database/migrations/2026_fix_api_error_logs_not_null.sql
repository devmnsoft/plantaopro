set search_path to plantaopro, public;

create schema if not exists plantaopro;
create extension if not exists pgcrypto;

create table if not exists plantaopro.api_error_logs (
    id uuid primary key default gen_random_uuid(),
    endpoint text not null default '',
    method text not null default '',
    status_code integer not null default 0,
    success boolean not null default false,
    error_message text not null default '',
    stack_trace text not null default '',
    user_id uuid null,
    email text not null default '',
    perfil text not null default '',
    ip text not null default '',
    query_string text not null default '',
    duration_ms bigint not null default 0,
    reg_date timestamp without time zone not null default now()
);

alter table if exists plantaopro.api_error_logs
    add column if not exists endpoint text not null default '',
    add column if not exists method text not null default '',
    add column if not exists metodo text not null default '',
    add column if not exists status_code integer not null default 0,
    add column if not exists success boolean not null default false,
    add column if not exists error_message text not null default '',
    add column if not exists mensagem text not null default '',
    add column if not exists stack_trace text not null default '',
    add column if not exists user_id uuid null,
    add column if not exists usuario_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists email text not null default '',
    add column if not exists perfil text not null default '',
    add column if not exists ip text not null default '',
    add column if not exists ip_origem text not null default '',
    add column if not exists user_agent text not null default '',
    add column if not exists query_string text not null default '',
    add column if not exists duration_ms bigint not null default 0,
    add column if not exists exception_type text not null default '',
    add column if not exists reg_status char(1) not null default 'A',
    add column if not exists reg_date timestamp without time zone not null default now();

alter table if exists plantaopro.api_error_logs
    alter column error_message set default '',
    alter column mensagem set default '',
    alter column stack_trace set default '',
    alter column endpoint set default '',
    alter column method set default '',
    alter column metodo set default '',
    alter column email set default '',
    alter column perfil set default '',
    alter column ip set default '',
    alter column ip_origem set default '',
    alter column user_agent set default '',
    alter column query_string set default '',
    alter column success set default false,
    alter column duration_ms set default 0;

update plantaopro.api_error_logs
set error_message = ''
where error_message is null;

update plantaopro.api_error_logs
set mensagem = ''
where mensagem is null;

update plantaopro.api_error_logs
set stack_trace = ''
where stack_trace is null;

update plantaopro.api_error_logs
set query_string = ''
where query_string is null;

update plantaopro.api_error_logs
set duration_ms = 0
where duration_ms is null;

create index if not exists ix_api_error_logs_reg_date
on plantaopro.api_error_logs(reg_date desc);

create index if not exists ix_api_error_logs_endpoint
on plantaopro.api_error_logs(endpoint);

create index if not exists ix_api_error_logs_status_code
on plantaopro.api_error_logs(status_code);
