set search_path to plantaopro, public;

create table if not exists plantaopro.cid_tabela (
    id uuid primary key default gen_random_uuid(),
    codigo text not null default '',
    descricao text not null default '',
    categoria text not null default '',
    capitulo text not null default '',
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamp without time zone not null default now(),
    reg_update timestamp without time zone null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.cid_tabela add column if not exists tenant_id uuid null;
alter table if exists plantaopro.cid_tabela add column if not exists cliente_id uuid null;
alter table if exists plantaopro.cid_tabela add column if not exists codigo text not null default '';
alter table if exists plantaopro.cid_tabela add column if not exists descricao text not null default '';
alter table if exists plantaopro.cid_tabela add column if not exists categoria text not null default '';
alter table if exists plantaopro.cid_tabela add column if not exists capitulo text not null default '';
alter table if exists plantaopro.cid_tabela add column if not exists status text not null default 'ATIVO';
alter table if exists plantaopro.cid_tabela add column if not exists created_by uuid null;
alter table if exists plantaopro.cid_tabela add column if not exists updated_by uuid null;
alter table if exists plantaopro.cid_tabela add column if not exists reg_date timestamp without time zone not null default now();
alter table if exists plantaopro.cid_tabela add column if not exists reg_update timestamp without time zone null;
alter table if exists plantaopro.cid_tabela add column if not exists reg_status char(1) not null default 'A';

create index if not exists ix_cid_tabela_codigo on plantaopro.cid_tabela(codigo);
create index if not exists ix_cid_tabela_descricao on plantaopro.cid_tabela(descricao);
create index if not exists ix_cid_tabela_status on plantaopro.cid_tabela(status);
create unique index if not exists ux_cid_tabela_codigo_ativo on plantaopro.cid_tabela(upper(codigo)) where reg_status = 'A' and codigo <> '';

insert into plantaopro.cid_tabela(codigo, descricao, categoria, capitulo, status)
select v.codigo, v.descricao, v.categoria, v.capitulo, 'ATIVO'
from (values
    ('I10','Hipertensão essencial','DEMO','Doenças do aparelho circulatório'),
    ('E11','Diabetes mellitus tipo 2','DEMO','Doenças endócrinas, nutricionais e metabólicas'),
    ('J06','Infecção aguda das vias aéreas superiores','DEMO','Doenças do aparelho respiratório'),
    ('R51','Cefaleia','DEMO','Sintomas, sinais e achados anormais'),
    ('M54','Dorsalgia','DEMO','Doenças do sistema osteomuscular'),
    ('A09','Diarreia e gastroenterite de origem infecciosa presumível','DEMO','Algumas doenças infecciosas e parasitárias'),
    ('F41','Transtornos ansiosos','DEMO','Transtornos mentais e comportamentais'),
    ('J45','Asma','DEMO','Doenças do aparelho respiratório'),
    ('N39','Transtornos do trato urinário','DEMO','Doenças do aparelho geniturinário'),
    ('Z00','Exame geral','DEMO','Fatores que influenciam o estado de saúde')
) as v(codigo, descricao, categoria, capitulo)
where not exists (select 1 from plantaopro.cid_tabela c where upper(c.codigo) = upper(v.codigo) and c.reg_status = 'A');

-- Compatibilidade com serviços que atualizam updated_at além de reg_update.
alter table if exists plantaopro.cid_tabela add column if not exists updated_at timestamp without time zone null;
