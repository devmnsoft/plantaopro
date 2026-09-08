-- PlantãoPro v2.14.9 - núcleo comercial de módulos SaaS.
-- Idempotente e compatível com instalações que possuem versões anteriores das tabelas.
set search_path to plantaopro, public;

alter table plantaopro.modulos_sistema add column if not exists categoria text not null default 'OPERACAO';
alter table plantaopro.modulos_sistema add column if not exists preco_base numeric(14,2) not null default 0;
alter table plantaopro.modulos_sistema add column if not exists essencial boolean not null default false;
alter table plantaopro.modulos_sistema add column if not exists funcionalidades jsonb not null default '[]'::jsonb;
alter table plantaopro.modulos_sistema add column if not exists limite_padrao integer null;

-- Compatibilidade de identificação individual/institucional em bases antigas.
alter table plantaopro.medicos add column if not exists usuario_id uuid null;
alter table plantaopro.medicos add column if not exists cpf text null;
alter table plantaopro.medicos add column if not exists reg_status char(1) not null default 'A';
alter table plantaopro.clientes add column if not exists cnpj text null;
alter table plantaopro.clientes add column if not exists nome_fantasia text null;
alter table plantaopro.clientes add column if not exists razao_social text null;
alter table plantaopro.clientes add column if not exists reg_status char(1) not null default 'A';

alter table plantaopro.tenant_modulos add column if not exists modulo_id uuid null;
alter table plantaopro.tenant_modulos add column if not exists codigo text null;
alter table plantaopro.tenant_modulos add column if not exists codigo_modulo text null;
alter table plantaopro.tenant_modulos add column if not exists habilitado boolean not null default true;
alter table plantaopro.tenant_modulos add column if not exists origem text not null default 'CONTRATO';
alter table plantaopro.tenant_modulos add column if not exists limite_contratado integer null;
alter table plantaopro.tenant_modulos add column if not exists preco_contratado numeric(14,2) null;
alter table plantaopro.tenant_modulos add column if not exists ativado_em timestamptz null;
alter table plantaopro.tenant_modulos add column if not exists desativado_em timestamptz null;
alter table plantaopro.tenant_modulos add column if not exists created_by uuid null;
alter table plantaopro.tenant_modulos add column if not exists updated_by uuid null;
alter table plantaopro.tenant_modulos add column if not exists reg_status char(1) not null default 'A';
alter table plantaopro.tenant_modulos add column if not exists reg_date timestamptz not null default now();
alter table plantaopro.tenant_modulos add column if not exists reg_update timestamptz null;

update plantaopro.tenant_modulos tm
set modulo_id = ms.id,
    codigo_modulo = ms.codigo
from plantaopro.modulos_sistema ms
where tm.modulo_id is null
  and upper(coalesce(nullif(tm.codigo_modulo, ''), nullif(tm.codigo, ''))) = upper(ms.codigo)
  and ms.reg_status = 'A';

update plantaopro.tenant_modulos tm
set codigo_modulo = coalesce(nullif(tm.codigo_modulo, ''), nullif(tm.codigo, ''), ms.codigo)
from plantaopro.modulos_sistema ms
where tm.modulo_id = ms.id and nullif(tm.codigo_modulo, '') is null;

create table if not exists plantaopro.plano_modulos (
    id uuid primary key default gen_random_uuid(),
    plano_id uuid not null,
    modulo_id uuid not null,
    incluido boolean not null default true,
    limite integer null,
    preco_adicional numeric(14,2) null,
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    created_by uuid null,
    updated_by uuid null
);

create table if not exists plantaopro.tenant_modulos_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_modulo_id uuid not null,
    tenant_id uuid not null,
    modulo_id uuid not null,
    acao text not null,
    antes jsonb null,
    depois jsonb null,
    usuario_id uuid null,
    ip_origem text null,
    reg_date timestamptz not null default now()
);

create unique index if not exists ux_plano_modulos_ativo
    on plantaopro.plano_modulos(plano_id, modulo_id) where reg_status = 'A';
create unique index if not exists ux_tenant_modulos_contrato_ativo
    on plantaopro.tenant_modulos(tenant_id, modulo_id) where reg_status = 'A' and modulo_id is not null;
create index if not exists ix_tenant_modulos_acesso
    on plantaopro.tenant_modulos(tenant_id, habilitado, status) where reg_status = 'A';
create index if not exists ix_tenant_modulos_historico_tenant
    on plantaopro.tenant_modulos_historico(tenant_id, reg_date desc);
create index if not exists ix_medicos_cpf_normalizado
    on plantaopro.medicos ((regexp_replace(coalesce(cpf, ''), '[^0-9]', '', 'g'))) where reg_status = 'A';
create index if not exists ix_medicos_usuario_ativo
    on plantaopro.medicos (usuario_id) where reg_status = 'A' and usuario_id is not null;
create index if not exists ix_clientes_cnpj_normalizado
    on plantaopro.clientes ((regexp_replace(coalesce(cnpj, ''), '[^0-9]', '', 'g'))) where reg_status = 'A';

do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'ck_modulos_sistema_preco_base') then
        alter table plantaopro.modulos_sistema add constraint ck_modulos_sistema_preco_base check (preco_base >= 0);
    end if;
    if not exists (select 1 from pg_constraint where conname = 'ck_tenant_modulos_preco_contratado') then
        alter table plantaopro.tenant_modulos add constraint ck_tenant_modulos_preco_contratado check (preco_contratado is null or preco_contratado >= 0);
    end if;
    if not exists (select 1 from pg_constraint where conname = 'fk_plano_modulos_plano') then
        alter table plantaopro.plano_modulos add constraint fk_plano_modulos_plano foreign key (plano_id) references plantaopro.planos(id);
    end if;
    if not exists (select 1 from pg_constraint where conname = 'fk_plano_modulos_modulo') then
        alter table plantaopro.plano_modulos add constraint fk_plano_modulos_modulo foreign key (modulo_id) references plantaopro.modulos_sistema(id);
    end if;
    if not exists (select 1 from pg_constraint where conname = 'fk_tenant_modulos_historico_modulo') then
        alter table plantaopro.tenant_modulos_historico add constraint fk_tenant_modulos_historico_modulo foreign key (modulo_id) references plantaopro.modulos_sistema(id);
    end if;
end $$;
