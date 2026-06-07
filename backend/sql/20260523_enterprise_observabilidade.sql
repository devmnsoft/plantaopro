create table if not exists plantaopro.api_request_logs (
    id uuid primary key default gen_random_uuid(),
    endpoint text not null,
    method varchar(16) not null,
    status_code int not null,
    duration_ms numeric(12,2) not null,
    cliente_id uuid null,
    usuario_id uuid null,
    reg_date timestamp not null default now()
);

create index if not exists idx_api_request_logs_reg_date on plantaopro.api_request_logs(reg_date desc);
create index if not exists idx_api_request_logs_endpoint on plantaopro.api_request_logs(endpoint);

create table if not exists plantaopro.api_error_logs (
    id uuid primary key default gen_random_uuid(),
    endpoint text not null,
    method varchar(16) not null,
    status_code int not null,
    error_message text not null,
    cliente_id uuid null,
    usuario_id uuid null,
    reg_date timestamp not null default now()
);

create index if not exists idx_api_error_logs_reg_date on plantaopro.api_error_logs(reg_date desc);

create table if not exists plantaopro.background_job_logs (
    id uuid primary key default gen_random_uuid(),
    job_name varchar(120) not null,
    status varchar(20) not null,
    message text null,
    duration_ms numeric(12,2) null,
    reg_date timestamp not null default now()
);
