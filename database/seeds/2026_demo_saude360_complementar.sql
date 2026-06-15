set search_path to plantaopro, public;

insert into plantaopro.convenios(id, cliente_id, nome, codigo_ans, status, reg_date, reg_status)
select gen_random_uuid(), c.id, v.nome, v.codigo, 'ATIVO', now(), 'A'
from (select id from plantaopro.clientes where reg_status = 'A' order by reg_date limit 1) c
cross join (values ('Convênio Demo Vida','ANSDEMO01'),('Convênio Demo Saúde','ANSDEMO02'),('Particular','PART')) v(nome,codigo)
where not exists (select 1 from plantaopro.convenios x where x.cliente_id = c.id and x.nome = v.nome and x.reg_status = 'A');

insert into plantaopro.planos_saude(id, cliente_id, nome, operadora, registro_ans, status, reg_date, reg_status)
select gen_random_uuid(), c.id, v.nome, 'Operadora Demo', v.codigo, 'ATIVO', now(), 'A'
from (select id from plantaopro.clientes where reg_status = 'A' order by reg_date limit 1) c
cross join (values ('Ambulatorial Demo','PDEMO01'),('Hospitalar Demo','PDEMO02'),('Executivo Demo','PDEMO03')) v(nome,codigo)
where not exists (select 1 from plantaopro.planos_saude x where x.cliente_id = c.id and x.nome = v.nome and x.reg_status = 'A');

insert into plantaopro.plano_saude_pacientes(id, cliente_id, plano_saude_id, paciente_id, numero_carteirinha, principal, status, reg_date, reg_status)
select gen_random_uuid(), p.cliente_id, ps.id, p.id, 'DEMO-' || substr(p.id::text, 1, 8), true, 'ATIVO', now(), 'A'
from plantaopro.pacientes p join plantaopro.planos_saude ps on ps.cliente_id = p.cliente_id
where p.reg_status = 'A' and ps.reg_status = 'A'
  and not exists (select 1 from plantaopro.plano_saude_pacientes v where v.paciente_id = p.id and v.plano_saude_id = ps.id and v.reg_status = 'A')
limit 3;

insert into plantaopro.convenio_autorizacoes(id, cliente_id, convenio_id, paciente_id, motivo, status, reg_date, reg_status)
select gen_random_uuid(), p.cliente_id, c.id, p.id, 'Autorização demo para consulta ambulatorial', 'APROVADA', now(), 'A'
from plantaopro.pacientes p join plantaopro.convenios c on c.cliente_id = p.cliente_id
where p.reg_status = 'A' and c.reg_status = 'A'
  and not exists (select 1 from plantaopro.convenio_autorizacoes a where a.paciente_id = p.id and a.convenio_id = c.id and a.reg_status = 'A')
limit 3;
