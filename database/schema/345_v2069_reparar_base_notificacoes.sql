-- Compatibilidade para bases legadas que registraram v1.31.0 como baseline
-- sem possuir o agregado completo de notificacoes. As definicoes reproduzem
-- integralmente o contrato original antes de v2.07.0; nenhum dado existente e
-- removido ou reescrito.
create table if not exists plantaopro.notifications (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null,
    cliente_id uuid,
    unidade_id uuid,
    categoria varchar(20) not null check (categoria in ('OPERACAO','ESCALA','CLINICA','FINANCEIRO','SEGURANCA','SISTEMA')),
    titulo varchar(160) not null,
    descricao text not null,
    url text,
    criado_em timestamptz not null default now(),
    expira_em timestamptz,
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.notification_recipients (
    id uuid primary key default gen_random_uuid(),
    notification_id uuid not null references plantaopro.notifications(id) on delete cascade,
    usuario_id uuid not null,
    unique (notification_id, usuario_id)
);

create table if not exists plantaopro.notification_read_states (
    id uuid primary key default gen_random_uuid(),
    notification_id uuid not null references plantaopro.notifications(id) on delete cascade,
    usuario_id uuid not null,
    lida_em timestamptz not null default now(),
    unique (notification_id, usuario_id)
);

create table if not exists plantaopro.notification_preferences (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null,
    usuario_id uuid not null,
    categoria varchar(20) not null,
    in_app boolean not null default true,
    email boolean not null default false,
    push boolean not null default false,
    atualizado_em timestamptz not null default now(),
    unique (tenant_id, usuario_id, categoria)
);
