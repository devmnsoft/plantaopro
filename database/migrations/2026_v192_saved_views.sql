-- Canonical Saved Views contract. This migration also upgrades the Portuguese
-- v1.31/v1.42 contract in place; IDs and rows are never recreated or deleted.
create table if not exists plantaopro.saved_views (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null,
    user_id uuid, module varchar(50), name varchar(100), normalized_name varchar(100),
    filters_json jsonb, sort_json jsonb, is_default boolean,
    created_at timestamptz, updated_at timestamptz
);

do $saved_views_upgrade$
declare has_old_config boolean; has_old_sector boolean; has_old_shared boolean;
begin
    select exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='configuracao') into has_old_config;
    select exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='setor_id') into has_old_sector;
    select exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='compartilhada') into has_old_shared;

    -- Rename when only the legacy spelling exists. Mixed/partially upgraded
    -- databases are handled by the backfills below.
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='usuario_id') and not exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='user_id') then alter table plantaopro.saved_views rename column usuario_id to user_id; end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='modulo') and not exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='module') then alter table plantaopro.saved_views rename column modulo to module; end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='nome') and not exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='name') then alter table plantaopro.saved_views rename column nome to name; end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='padrao') and not exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='is_default') then alter table plantaopro.saved_views rename column padrao to is_default; end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='criado_em') and not exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='created_at') then alter table plantaopro.saved_views rename column criado_em to created_at; end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='atualizado_em') and not exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='updated_at') then alter table plantaopro.saved_views rename column atualizado_em to updated_at; end if;

    alter table plantaopro.saved_views add column if not exists user_id uuid, add column if not exists module varchar(50), add column if not exists name varchar(100), add column if not exists normalized_name varchar(100), add column if not exists filters_json jsonb, add column if not exists sort_json jsonb, add column if not exists is_default boolean, add column if not exists created_at timestamptz, add column if not exists updated_at timestamptz;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='usuario_id') then execute 'update plantaopro.saved_views set user_id=coalesce(user_id,usuario_id)'; end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='modulo') then execute 'update plantaopro.saved_views set module=coalesce(module,modulo)'; end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='nome') then execute 'update plantaopro.saved_views set name=coalesce(name,nome)'; end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='padrao') then execute 'update plantaopro.saved_views set is_default=coalesce(is_default,padrao)'; end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='criado_em') then execute 'update plantaopro.saved_views set created_at=coalesce(created_at,criado_em)'; end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='saved_views' and column_name='atualizado_em') then execute 'update plantaopro.saved_views set updated_at=coalesce(updated_at,atualizado_em)'; end if;
    if has_old_config then execute 'update plantaopro.saved_views set filters_json=coalesce(filters_json,configuracao)'; end if;
    update plantaopro.saved_views set filters_json=coalesce(filters_json,'{}'::jsonb), is_default=coalesce(is_default,false), created_at=coalesce(created_at,now()), updated_at=coalesce(updated_at,created_at,now()), normalized_name=coalesce(nullif(normalized_name,''),lower(regexp_replace(btrim(name),'\s+',' ','g')));
    -- Preserve legacy-only sharing/sector state inside the JSON contract before
    -- removing competing presentation columns.
    if has_old_sector then execute 'update plantaopro.saved_views set filters_json=filters_json || jsonb_build_object(''_legacy_setor_id'',setor_id) where setor_id is not null'; end if;
    if has_old_shared then execute 'update plantaopro.saved_views set filters_json=filters_json || jsonb_build_object(''_legacy_compartilhada'',compartilhada) where compartilhada'; end if;
end $saved_views_upgrade$;

alter table plantaopro.saved_views alter column user_id set not null, alter column module set not null, alter column name set not null, alter column normalized_name set not null, alter column filters_json set default '{}'::jsonb, alter column filters_json set not null, alter column is_default set default false, alter column is_default set not null, alter column created_at set default now(), alter column created_at set not null, alter column updated_at set default now(), alter column updated_at set not null;
create index if not exists ix_saved_views_tenant_user on plantaopro.saved_views(tenant_id,user_id);
create index if not exists ix_saved_views_tenant_module on plantaopro.saved_views(tenant_id,module);
create unique index if not exists ux_saved_views_name on plantaopro.saved_views(tenant_id,user_id,module,normalized_name);
create unique index if not exists ux_saved_views_default on plantaopro.saved_views(tenant_id,user_id,module) where is_default;

create table if not exists plantaopro.productivity_item_user_state (id uuid primary key default gen_random_uuid(), tenant_id uuid not null, user_id uuid not null, item_key varchar(300) not null, snoozed_until timestamptz null, dismissed_at timestamptz null, last_seen_at timestamptz null, created_at timestamptz not null default now(), updated_at timestamptz not null default now(), constraint uq_productivity_user_item unique (tenant_id,user_id,item_key));
create index if not exists ix_productivity_user_state_scope on plantaopro.productivity_item_user_state(tenant_id,user_id);
create index if not exists ix_productivity_user_state_snooze on plantaopro.productivity_item_user_state(snoozed_until) where snoozed_until is not null;

do $$ begin
 if not exists(select 1 from pg_constraint where conname='fk_saved_views_tenant') then alter table plantaopro.saved_views add constraint fk_saved_views_tenant foreign key(tenant_id) references plantaopro.clientes(id) on delete cascade; end if;
 if not exists(select 1 from pg_constraint where conname='fk_saved_views_user') then alter table plantaopro.saved_views add constraint fk_saved_views_user foreign key(user_id) references plantaopro.usuarios(id) on delete cascade; end if;
 if not exists(select 1 from pg_constraint where conname='fk_productivity_state_tenant') then alter table plantaopro.productivity_item_user_state add constraint fk_productivity_state_tenant foreign key(tenant_id) references plantaopro.clientes(id) on delete cascade; end if;
 if not exists(select 1 from pg_constraint where conname='fk_productivity_state_user') then alter table plantaopro.productivity_item_user_state add constraint fk_productivity_state_user foreign key(user_id) references plantaopro.usuarios(id) on delete cascade; end if;
end $$;
