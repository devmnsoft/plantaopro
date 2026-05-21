-- PlantaoPro Premium SaaS - Permissões, preferências de notificação e índices de auditoria
create table if not exists plantaopro.usuario_notificacao_preferencias (
    usuario_id uuid not null references plantaopro.usuarios(id),
    tipo varchar(50) not null,
    in_app boolean not null default true,
    email boolean not null default true,
    reg_date timestamp not null default now(),
    reg_update timestamp null,
    reg_status char(1) not null default 'A',
    constraint pk_usuario_notificacao_preferencias primary key (usuario_id, tipo)
);

create index if not exists ix_auditoria_entidade_registro on plantaopro.auditoria(entidade, registro_id, reg_date desc);
create index if not exists ix_auditoria_usuario_data on plantaopro.auditoria(usuario_id, reg_date desc);
create index if not exists ix_notificacao_pref_tipo on plantaopro.usuario_notificacao_preferencias(tipo);

alter table if exists plantaopro.pagamentos
    alter column status set default 'PENDENTE';

-- Segurança de integridade para pagamentos
alter table if exists plantaopro.pagamentos
    add constraint ck_pagamentos_status
    check (status in ('PENDENTE','DISPONIVEL','PAGO','ATRASADO','CANCELADO'));
