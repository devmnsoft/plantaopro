create table if not exists plantaopro.saved_views (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null,
    user_id uuid not null,
    module varchar(50) not null,
    name varchar(80) not null,
    normalized_name varchar(80) not null,
    filters_json jsonb not null default '{}'::jsonb,
    sort_json jsonb null,
    is_default boolean not null default false,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ck_saved_views_filters_object check (jsonb_typeof(filters_json) = 'object' and pg_column_size(filters_json) <= 16384),
    constraint ck_saved_views_sort_object check (sort_json is null or (jsonb_typeof(sort_json) = 'object' and pg_column_size(sort_json) <= 16384)),
    constraint ck_saved_views_module check (module in ('PLANTOES','ESCALAS','PAGAMENTOS','PRODUTIVIDADE','PACIENTES','AGENDA')),
    constraint uq_saved_views_name unique (tenant_id,user_id,module,normalized_name)
);

create index if not exists ix_saved_views_tenant_user on plantaopro.saved_views(tenant_id,user_id);
create index if not exists ix_saved_views_tenant_module on plantaopro.saved_views(tenant_id,module);
create unique index if not exists ux_saved_views_default on plantaopro.saved_views(tenant_id,user_id,module) where is_default;

-- Business state stays in its source entity; only per-user presentation state is persisted.
create table if not exists plantaopro.productivity_item_user_state (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, user_id uuid not null,
    item_key varchar(300) not null, snoozed_until timestamptz null, dismissed_at timestamptz null,
    last_seen_at timestamptz null, created_at timestamptz not null default now(), updated_at timestamptz not null default now(),
    constraint uq_productivity_user_item unique (tenant_id,user_id,item_key)
);
create index if not exists ix_productivity_user_state_scope on plantaopro.productivity_item_user_state(tenant_id,user_id);
create index if not exists ix_productivity_user_state_snooze on plantaopro.productivity_item_user_state(snoozed_until) where snoozed_until is not null;

do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'fk_saved_views_tenant') then
        alter table plantaopro.saved_views add constraint fk_saved_views_tenant foreign key (tenant_id) references plantaopro.clientes(id) on delete cascade;
    end if;
    if not exists (select 1 from pg_constraint where conname = 'fk_saved_views_user') then
        alter table plantaopro.saved_views add constraint fk_saved_views_user foreign key (user_id) references plantaopro.usuarios(id) on delete cascade;
    end if;
    if not exists (select 1 from pg_constraint where conname = 'fk_productivity_state_tenant') then
        alter table plantaopro.productivity_item_user_state add constraint fk_productivity_state_tenant foreign key (tenant_id) references plantaopro.clientes(id) on delete cascade;
    end if;
    if not exists (select 1 from pg_constraint where conname = 'fk_productivity_state_user') then
        alter table plantaopro.productivity_item_user_state add constraint fk_productivity_state_user foreign key (user_id) references plantaopro.usuarios(id) on delete cascade;
    end if;
end $$;
