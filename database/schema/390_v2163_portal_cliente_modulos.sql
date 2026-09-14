-- PlantãoPro v2.16.3 - solicitações comerciais de módulos, snapshots e ativação idempotente.
set search_path to plantaopro, public;

alter table plantaopro.modulos_sistema alter column preco_base drop not null;
alter table plantaopro.modulos_sistema add column if not exists periodicidade text null;
alter table plantaopro.modulos_sistema add column if not exists disponivel_comercialmente boolean not null default false;

create table if not exists plantaopro.solicitacoes_modulos (
 id uuid primary key, tenant_id uuid not null, cliente_id uuid null, protocolo text not null,
 status text not null, versao_condicoes text not null, total numeric(14,2) null, periodicidade text not null,
 inicio_previsto timestamptz not null, chave_idempotencia text not null, solicitado_por uuid null,
 decidido_por uuid null, decidido_em timestamptz null, justificativa text null,
 reg_date timestamptz not null default now(), reg_update timestamptz null, reg_status char(1) not null default 'A',
 constraint ck_solicitacoes_modulos_status check(status in ('PENDENTE','APROVADA','RECUSADA','CANCELADA')),
 constraint ck_solicitacoes_modulos_total check(total is null or total >= 0)
);
create table if not exists plantaopro.modulo_catalogo_dependencias (
 id uuid primary key default gen_random_uuid(), modulo_id uuid not null references plantaopro.modulos_sistema(id),
 modulo_dependencia_id uuid not null references plantaopro.modulos_sistema(id),
 reg_date timestamptz not null default now(), reg_status char(1) not null default 'A'
);
create table if not exists plantaopro.solicitacao_modulo_itens (
 id uuid primary key, solicitacao_id uuid not null references plantaopro.solicitacoes_modulos(id), modulo_id uuid not null,
 codigo text not null, nome text not null, descricao text not null, funcionalidades jsonb not null default '[]',
 dependencias jsonb not null default '[]', preco numeric(14,2) null, periodicidade text null,
 reg_date timestamptz not null default now(), reg_status char(1) not null default 'A'
);
create unique index if not exists ux_solicitacoes_modulos_idempotencia on plantaopro.solicitacoes_modulos(tenant_id,chave_idempotencia) where reg_status='A';
create unique index if not exists ux_modulo_catalogo_dependencia on plantaopro.modulo_catalogo_dependencias(modulo_id,modulo_dependencia_id) where reg_status='A';
create unique index if not exists ux_solicitacao_modulo_item on plantaopro.solicitacao_modulo_itens(solicitacao_id,modulo_id) where reg_status='A';
create index if not exists ix_solicitacoes_modulos_filtros on plantaopro.solicitacoes_modulos(status,tenant_id,reg_date desc) where reg_status='A';

-- Zero deixa de significar "gratuito": catálogo legado sem preço publicado passa a proposta.
update plantaopro.modulos_sistema set preco_base=null,periodicidade=null where preco_base=0 and reg_status='A';
