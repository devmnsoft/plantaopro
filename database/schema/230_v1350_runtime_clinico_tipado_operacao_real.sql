-- PlantãoPro v1.35.0: consolidação segura das preferências de notificação.
create table if not exists plantaopro.notification_preferences (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null,
    usuario_id uuid not null,
    categoria varchar(40) not null,
    tipo_evento varchar(80) not null default 'GERAL',
    in_app boolean not null default true,
    email boolean not null default false,
    push boolean not null default false,
    whatsapp boolean not null default false,
    ativo boolean not null default true,
    atualizado_em timestamptz not null default now()
);

alter table plantaopro.notification_preferences add column if not exists tipo_evento varchar(80) not null default 'GERAL';
alter table plantaopro.notification_preferences add column if not exists whatsapp boolean not null default false;
alter table plantaopro.notification_preferences add column if not exists ativo boolean not null default true;

-- Preserva a origem legada; somente copia linhas com tenant resolvido e nunca a apaga.
do $migration$
begin
    if to_regclass('plantaopro.notificacao_preferencias') is not null then
        execute $copy$
            insert into plantaopro.notification_preferences
                (id, tenant_id, usuario_id, categoria, tipo_evento, in_app, email, push, whatsapp, ativo, atualizado_em)
            select gen_random_uuid(), coalesce(tenant_id, cliente_id), usuario_id,
                   'GERAL', upper(trim(tipo_evento)), coalesce(in_app, true), coalesce(email, false),
                   coalesce(push, false), coalesce(whatsapp, false), reg_status = 'A', coalesce(reg_update, reg_date, now())
              from plantaopro.notificacao_preferencias legacy
             where coalesce(tenant_id, cliente_id) is not null and usuario_id is not null and tipo_evento is not null
            on conflict do nothing
        $copy$;
    end if;
end
$migration$;

-- Substitui a unicidade antiga por categoria pela chave completa do contrato canônico.
do $constraints$
declare constraint_name text;
begin
    select conname into constraint_name
      from pg_constraint
     where conrelid = 'plantaopro.notification_preferences'::regclass
       and contype = 'u'
       and pg_get_constraintdef(oid) = 'UNIQUE (tenant_id, usuario_id, categoria)'
     limit 1;
    if constraint_name is not null then
        execute format('alter table plantaopro.notification_preferences drop constraint %I', constraint_name);
    end if;
end
$constraints$;

create unique index if not exists ux_notification_preferences_scope_event
    on plantaopro.notification_preferences(tenant_id, usuario_id, categoria, tipo_evento);
create index if not exists ix_notification_preferences_tenant_user_active
    on plantaopro.notification_preferences(tenant_id, usuario_id) where ativo;
