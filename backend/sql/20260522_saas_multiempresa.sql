create schema if not exists plantaopro;

create table if not exists plantaopro.planos (
    id uuid primary key default gen_random_uuid(),
    nome varchar(120) not null,
    descricao text null,
    valor_mensal numeric(12,2) not null default 0,
    limite_medicos int not null default 0,
    limite_hospitais int not null default 0,
    limite_plantoes_mes int not null default 0,
    permite_relatorios boolean not null default true,
    permite_api boolean not null default false,
    permite_notificacao_email boolean not null default true,
    status varchar(20) not null default 'ATIVO',
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now()
);

create table if not exists plantaopro.clientes (
    id uuid primary key default gen_random_uuid(),
    razao_social varchar(180) not null,
    nome_fantasia varchar(180) not null,
    cnpj varchar(18) not null,
    email varchar(180) null,
    telefone varchar(20) null,
    cidade varchar(120) null,
    estado varchar(2) null,
    plano_id uuid null,
    status varchar(20) not null default 'ATIVO',
    reg_status char(1) not null default 'A',
    reg_date timestamptz not null default now(),
    reg_update timestamptz null
);

do $$ begin
if not exists (select 1 from pg_constraint where conname='fk_clientes_planos') then
alter table plantaopro.clientes add constraint fk_clientes_planos foreign key (plano_id) references plantaopro.planos(id);
end if;
end $$;

alter table plantaopro.usuarios add column if not exists cliente_id uuid;
alter table plantaopro.hospitais add column if not exists cliente_id uuid;
alter table plantaopro.medicos add column if not exists cliente_id uuid;
alter table plantaopro.plantoes add column if not exists cliente_id uuid;
alter table plantaopro.escalas add column if not exists cliente_id uuid;
alter table plantaopro.pagamentos add column if not exists cliente_id uuid;
alter table plantaopro.notificacoes add column if not exists cliente_id uuid;
alter table plantaopro.auditoria add column if not exists cliente_id uuid;

create index if not exists ix_clientes_status on plantaopro.clientes(status, reg_status);
create index if not exists ix_hospitais_cliente_id on plantaopro.hospitais(cliente_id);

insert into plantaopro.planos(nome,descricao,valor_mensal,limite_medicos,limite_hospitais,limite_plantoes_mes,permite_api,status)
select 'Starter','Plano inicial para pequenas operações.',299,20,5,150,false,'ATIVO'
where not exists(select 1 from plantaopro.planos where nome='Starter');

insert into plantaopro.clientes(razao_social,nome_fantasia,cnpj,email,telefone,cidade,estado,plano_id,status)
select 'Cliente Demonstração PlantãoPro LTDA','Cliente Demonstração PlantãoPro','12.345.678/0001-90','demo@plantaopro.com','11999999999','São Paulo','SP',(select id from plantaopro.planos where nome='Starter' limit 1),'ATIVO'
where not exists(select 1 from plantaopro.clientes where nome_fantasia='Cliente Demonstração PlantãoPro');

update plantaopro.usuarios set cliente_id=(select id from plantaopro.clientes where nome_fantasia='Cliente Demonstração PlantãoPro' limit 1) where cliente_id is null;
