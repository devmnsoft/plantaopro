-- PlantãoPro v2.07.0: central tenant-safe, rastreabilidade e outbox de notificações.
alter table plantaopro.notifications add column if not exists tipo_evento varchar(60) not null default 'SISTEMA';
alter table plantaopro.notifications add column if not exists prioridade varchar(16) not null default 'BAIXA';
alter table plantaopro.notifications add column if not exists origem_tipo varchar(60);
alter table plantaopro.notifications add column if not exists origem_id uuid;
alter table plantaopro.notifications add column if not exists usuario_id_dedupe uuid;

create unique index if not exists ux_notifications_alerta_idempotente
    on plantaopro.notifications(tenant_id, usuario_id_dedupe, origem_tipo, origem_id, tipo_evento)
    where origem_id is not null and usuario_id_dedupe is not null and reg_status='A';
create index if not exists ix_notifications_central
    on plantaopro.notifications(tenant_id, prioridade, criado_em desc) where reg_status='A';

alter table plantaopro.notification_preferences add column if not exists tipo_evento varchar(60) not null default 'GERAL';
alter table plantaopro.notification_preferences add column if not exists whatsapp boolean not null default false;
alter table plantaopro.notification_preferences add column if not exists ativo boolean not null default true;
alter table plantaopro.notification_preferences drop constraint if exists notification_preferences_tenant_id_usuario_id_categoria_key;
create unique index if not exists ux_notification_preferences_evento
    on plantaopro.notification_preferences(tenant_id, usuario_id, categoria, tipo_evento);

create table if not exists plantaopro.notification_actions (
    id uuid primary key default gen_random_uuid(), notification_id uuid not null references plantaopro.notifications(id) on delete cascade,
    tenant_id uuid not null, usuario_id uuid not null, status varchar(16) not null check(status in ('ARQUIVADA','RESOLVIDA')),
    criado_em timestamptz not null default now()
);
create index if not exists ix_notification_actions_user
    on plantaopro.notification_actions(tenant_id, usuario_id, notification_id, criado_em desc);

-- Preparação de domínio: nenhum provedor externo é presumido. O worker futuro processará apenas canais configurados.
create table if not exists plantaopro.notification_outbox (
    id uuid primary key default gen_random_uuid(), tenant_id uuid not null, notification_id uuid not null references plantaopro.notifications(id) on delete cascade,
    canal varchar(16) not null check(canal in ('EMAIL','PUSH')), payload jsonb not null default '{}'::jsonb,
    status varchar(16) not null default 'PENDENTE', tentativas integer not null default 0,
    proxima_tentativa_em timestamptz, criado_em timestamptz not null default now(), processado_em timestamptz,
    unique(notification_id, canal)
);
create index if not exists ix_notification_outbox_pending
    on plantaopro.notification_outbox(tenant_id, status, proxima_tentativa_em) where status='PENDENTE';
