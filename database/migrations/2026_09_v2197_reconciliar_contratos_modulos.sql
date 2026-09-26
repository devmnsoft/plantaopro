-- PlantãoPro v2.19.7 - contrato canônico e reconciliação segura de tenant_modulos.
-- Não remove dados: códigos sem correspondência ou ambíguos permanecem intactos e
-- são registrados em tenant_modulos_reconciliacao para correção administrativa.
set search_path to plantaopro, public;

do $migration$
begin
    if to_regclass('plantaopro.tenant_modulos') is null
       or to_regclass('plantaopro.modulos_sistema') is null then
        raise exception 'Pré-requisitos ausentes: tenant_modulos e modulos_sistema devem existir';
    end if;
end $migration$;

-- Acrescentar primeiro como nullable evita que o DEFAULT transforme contratos
-- cancelados/bloqueados legados em contratos habilitados durante o backfill.
alter table plantaopro.tenant_modulos add column if not exists modulo_id uuid null;
alter table plantaopro.tenant_modulos add column if not exists codigo text null;
alter table plantaopro.tenant_modulos add column if not exists codigo_modulo text null;
alter table plantaopro.tenant_modulos add column if not exists habilitado boolean null;
alter table plantaopro.tenant_modulos add column if not exists origem text null;
alter table plantaopro.tenant_modulos add column if not exists status text null;
alter table plantaopro.tenant_modulos add column if not exists reg_status char(1) null;
alter table plantaopro.tenant_modulos add column if not exists reg_date timestamptz null;
alter table plantaopro.tenant_modulos add column if not exists reg_update timestamptz null;
alter table plantaopro.tenant_modulos add column if not exists ativado_em timestamptz null;
alter table plantaopro.tenant_modulos add column if not exists desativado_em timestamptz null;
alter table plantaopro.tenant_modulos add column if not exists limite_contratado integer null;
alter table plantaopro.tenant_modulos add column if not exists preco_contratado numeric(14,2) null;
alter table plantaopro.tenant_modulos add column if not exists created_by uuid null;
alter table plantaopro.tenant_modulos add column if not exists updated_by uuid null;

-- Datas legadas são copiadas quando essas colunas existem; SQL dinâmico mantém a
-- migration compatível com as duas famílias de schema encontradas no repositório.
do $dates$
begin
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='tenant_modulos' and column_name='criado_em') then
        execute 'update plantaopro.tenant_modulos set reg_date=coalesce(reg_date,criado_em) where reg_date is null';
    end if;
    if exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='tenant_modulos' and column_name='atualizado_em') then
        execute 'update plantaopro.tenant_modulos set reg_update=coalesce(reg_update,atualizado_em) where reg_update is null';
    end if;
end $dates$;

update plantaopro.tenant_modulos
set codigo_modulo = nullif(btrim(coalesce(codigo_modulo,codigo)),''),
    codigo = nullif(btrim(coalesce(codigo,codigo_modulo)),''),
    status = upper(coalesce(nullif(btrim(status),''),'ATIVO')),
    reg_status = upper(coalesce(nullif(btrim(reg_status),''),'A')),
    reg_date = coalesce(reg_date,now()),
    origem = coalesce(nullif(btrim(origem),''),'LEGADO')
where codigo_modulo is distinct from nullif(btrim(coalesce(codigo_modulo,codigo)),'')
   or codigo is distinct from nullif(btrim(coalesce(codigo,codigo_modulo)),'')
   or status is null or reg_status is null or reg_date is null or origem is null;

-- A ausência do flag só herda acesso para um contrato realmente ATIVO. Estados
-- AGENDADO, SUSPENSO, BLOQUEADO, CANCELADO e INATIVO continuam sem acesso.
update plantaopro.tenant_modulos
set habilitado = (upper(coalesce(status,''))='ATIVO' and reg_status='A')
where habilitado is null;

create table if not exists plantaopro.tenant_modulos_reconciliacao (
    tenant_modulo_id uuid primary key,
    tenant_id uuid null,
    codigo_legado text null,
    motivo text not null,
    candidatos uuid[] not null default '{}',
    detectado_em timestamptz not null default now(),
    resolvido_em timestamptz null
);

-- O Administrativo 360 é um produto independente: entra no catálogo sem criar
-- dependências implícitas com Saúde 360 ou Plantões.
insert into plantaopro.modulos_sistema(id,codigo,nome,descricao,ordem,status,reg_status,reg_date)
select 'a3600000-0000-4000-8000-000000000099','ADM360','Administrativo 360',
       'Gestão administrativa independente',2190,'ATIVO','A',now()
where not exists(select 1 from plantaopro.modulos_sistema where upper(btrim(codigo))='ADM360' and reg_status='A');

-- Relaciona somente códigos com exatamente um módulo ativo no catálogo.
with matches as (
    select tm.id tenant_modulo_id, (array_agg(ms.id order by ms.id))[1] modulo_id, count(*) quantidade
    from plantaopro.tenant_modulos tm
    join plantaopro.modulos_sistema ms
      on upper(btrim(ms.codigo))=upper(btrim(coalesce(tm.codigo_modulo,tm.codigo)))
     and ms.reg_status='A'
    where tm.modulo_id is null and nullif(btrim(coalesce(tm.codigo_modulo,tm.codigo)),'') is not null
    group by tm.id
)
update plantaopro.tenant_modulos tm
set modulo_id=m.modulo_id, reg_update=coalesce(tm.reg_update,now())
from matches m where tm.id=m.tenant_modulo_id and m.quantidade=1;

update plantaopro.tenant_modulos tm
set codigo_modulo=ms.codigo, codigo=coalesce(nullif(tm.codigo,''),ms.codigo)
from plantaopro.modulos_sistema ms
where tm.modulo_id=ms.id and ms.reg_status='A'
  and (tm.codigo_modulo is null or upper(btrim(tm.codigo_modulo))<>upper(btrim(ms.codigo)));

insert into plantaopro.tenant_modulos_reconciliacao(tenant_modulo_id,tenant_id,codigo_legado,motivo,candidatos,detectado_em,resolvido_em)
select tm.id,tm.tenant_id,coalesce(tm.codigo_modulo,tm.codigo),
       case when count(ms.id)=0 then 'CATALOGO_SEM_CORRESPONDENCIA' else 'CATALOGO_AMBIGUO' end,
       coalesce(array_agg(ms.id order by ms.id) filter(where ms.id is not null),'{}'),now(),null
from plantaopro.tenant_modulos tm
left join plantaopro.modulos_sistema ms
  on upper(btrim(ms.codigo))=upper(btrim(coalesce(tm.codigo_modulo,tm.codigo))) and ms.reg_status='A'
where tm.modulo_id is null and tm.reg_status='A'
group by tm.id,tm.tenant_id,tm.codigo_modulo,tm.codigo
on conflict(tenant_modulo_id) do update set codigo_legado=excluded.codigo_legado,motivo=excluded.motivo,
 candidatos=excluded.candidatos,detectado_em=excluded.detectado_em,resolvido_em=null;

update plantaopro.tenant_modulos_reconciliacao r set resolvido_em=now()
where resolvido_em is null and exists(select 1 from plantaopro.tenant_modulos tm where tm.id=r.tenant_modulo_id and tm.modulo_id is not null);

do $validate$
begin
    if exists(select 1 from plantaopro.tenant_modulos tm left join plantaopro.modulos_sistema ms on ms.id=tm.modulo_id
              where tm.modulo_id is not null and ms.id is null) then
        raise exception 'tenant_modulos contém modulo_id órfão; consulte tenant_modulos antes de prosseguir';
    end if;
    if exists(select 1 from plantaopro.tenant_modulos where tenant_id is null and reg_status='A') then
        raise exception 'tenant_modulos contém contrato ativo sem tenant_id';
    end if;
    if exists(select 1 from plantaopro.tenant_modulos where reg_status='A' and modulo_id is not null
              group by tenant_id,modulo_id having count(*)>1) then
        raise exception 'tenant_modulos contém contratos ativos duplicados para tenant/módulo';
    end if;
end $validate$;

alter table plantaopro.tenant_modulos alter column habilitado set default true;
alter table plantaopro.tenant_modulos alter column habilitado set not null;
alter table plantaopro.tenant_modulos alter column origem set default 'CONTRATO';
alter table plantaopro.tenant_modulos alter column origem set not null;
alter table plantaopro.tenant_modulos alter column status set default 'ATIVO';
alter table plantaopro.tenant_modulos alter column status set not null;
alter table plantaopro.tenant_modulos alter column reg_status set default 'A';
alter table plantaopro.tenant_modulos alter column reg_status set not null;
alter table plantaopro.tenant_modulos alter column reg_date set default now();
alter table plantaopro.tenant_modulos alter column reg_date set not null;

create unique index if not exists ux_tenant_modulos_contrato_ativo
 on plantaopro.tenant_modulos(tenant_id,modulo_id) where reg_status='A' and modulo_id is not null;
create index if not exists ix_tenant_modulos_acesso
 on plantaopro.tenant_modulos(tenant_id,habilitado,status) where reg_status='A';

do $constraints$
begin
    if not exists(select 1 from pg_constraint where conname='fk_tenant_modulos_modulo_id' and conrelid='plantaopro.tenant_modulos'::regclass) then
        alter table plantaopro.tenant_modulos add constraint fk_tenant_modulos_modulo_id
          foreign key(modulo_id) references plantaopro.modulos_sistema(id) not valid;
        alter table plantaopro.tenant_modulos validate constraint fk_tenant_modulos_modulo_id;
    end if;
end $constraints$;
