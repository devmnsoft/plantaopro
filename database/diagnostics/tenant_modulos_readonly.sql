-- Diagnóstico somente leitura. Pode ser executado no pgAdmin antes da v2197.
begin transaction read only;
select column_name,data_type,is_nullable,column_default
from information_schema.columns
where table_schema='plantaopro' and table_name in ('tenant_modulos','modulos_sistema')
order by table_name,ordinal_position;

select upper(btrim(codigo)) codigo,count(*) quantidade,array_agg(id order by id) ids
from plantaopro.modulos_sistema where reg_status='A'
group by upper(btrim(codigo)) having count(*)>1;

-- Esta consulta usa somente as colunas presentes no schema legado canônico.
select tenant_id,upper(btrim(codigo)) codigo,count(*) quantidade,array_agg(id order by id) ids
from plantaopro.tenant_modulos
group by tenant_id,upper(btrim(codigo)) having count(*)>1;
rollback;
