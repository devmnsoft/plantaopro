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
    add column if not exists metodo text not null default '',
    add column if not exists method text not null default '',
    add column if not exists status_code integer not null default 0,
    add column if not exists success boolean not null default false,
    add column if not exists mensagem text not null default '',
    add column if not exists error_message text not null default '',
    add column if not exists exception_type text not null default '',
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
    add column if not exists reg_status char(1) not null default 'A',
    add column if not exists reg_date timestamp without time zone not null default now();

alter table if exists plantaopro.api_error_logs
    alter column endpoint set default '';

alter table if exists plantaopro.api_error_logs
    alter column metodo set default '';

alter table if exists plantaopro.api_error_logs
    alter column method set default '';

alter table if exists plantaopro.api_error_logs
    alter column error_message set default '';

alter table if exists plantaopro.api_error_logs
    alter column mensagem set default '';

alter table if exists plantaopro.api_error_logs
    alter column exception_type set default '';

alter table if exists plantaopro.api_error_logs
    alter column stack_trace set default '';

alter table if exists plantaopro.api_error_logs
    alter column email set default '';

alter table if exists plantaopro.api_error_logs
    alter column perfil set default '';

alter table if exists plantaopro.api_error_logs
    alter column ip set default '';

alter table if exists plantaopro.api_error_logs
    alter column ip_origem set default '';

alter table if exists plantaopro.api_error_logs
    alter column user_agent set default '';

alter table if exists plantaopro.api_error_logs
    alter column query_string set default '';

alter table if exists plantaopro.api_error_logs
    alter column status_code set default 0;

alter table if exists plantaopro.api_error_logs
    alter column success set default false;

alter table if exists plantaopro.api_error_logs
    alter column duration_ms set default 0;

update plantaopro.api_error_logs
set endpoint = ''
where endpoint is null;

update plantaopro.api_error_logs
set metodo = ''
where metodo is null;

update plantaopro.api_error_logs
set method = ''
where method is null;

update plantaopro.api_error_logs
set error_message = ''
where error_message is null;

update plantaopro.api_error_logs
set mensagem = ''
where mensagem is null;

update plantaopro.api_error_logs
set exception_type = ''
where exception_type is null;

update plantaopro.api_error_logs
set stack_trace = ''
where stack_trace is null;

update plantaopro.api_error_logs
set email = ''
where email is null;

update plantaopro.api_error_logs
set perfil = ''
where perfil is null;

update plantaopro.api_error_logs
set ip = ''
where ip is null;

update plantaopro.api_error_logs
set ip_origem = ''
where ip_origem is null;

update plantaopro.api_error_logs
set user_agent = ''
where user_agent is null;

update plantaopro.api_error_logs
set query_string = ''
where query_string is null;

update plantaopro.api_error_logs
set status_code = 0
where status_code is null;

update plantaopro.api_error_logs
set success = false
where success is null;

update plantaopro.api_error_logs
set duration_ms = 0
where duration_ms is null;

alter table if exists plantaopro.api_error_logs
    alter column endpoint set not null,
    alter column method set not null,
    alter column error_message set not null,
    alter column stack_trace set not null,
    alter column email set not null,
    alter column perfil set not null,
    alter column ip set not null,
    alter column query_string set not null,
    alter column status_code set not null,
    alter column success set not null,
    alter column duration_ms set not null;

create index if not exists ix_api_error_logs_reg_date
on plantaopro.api_error_logs(reg_date desc);

create index if not exists ix_api_error_logs_endpoint
on plantaopro.api_error_logs(endpoint);

create index if not exists ix_api_error_logs_status_code
on plantaopro.api_error_logs(status_code);

create index if not exists ix_api_error_logs_success
on plantaopro.api_error_logs(success);
