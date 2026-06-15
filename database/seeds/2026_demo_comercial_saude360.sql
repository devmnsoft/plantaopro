-- Seed idempotente de demonstração comercial PlantãoPro Saúde 360.
-- Dados 100% fictícios; CPFs usam sequências inválidas para evitar dados reais.
create schema if not exists plantaopro;

insert into plantaopro.cid_tabela(id, codigo, descricao, categoria, status)
select gen_random_uuid(), v.codigo, v.descricao, 'DEMO', 'ATIVO'
from (values
('A09','Gastroenterite infecciosa demo'),('J00','Nasofaringite aguda demo'),('J45','Asma demo'),('I10','Hipertensão essencial demo'),('E11','Diabetes mellitus tipo 2 demo'),('R50','Febre demo'),('M54','Dor lombar demo'),('N39','Infecção urinária demo'),('F41','Ansiedade demo'),('Z00','Exame geral demo')) v(codigo,descricao)
where not exists (select 1 from plantaopro.cid_tabela c where c.codigo=v.codigo and c.reg_status='A');

insert into plantaopro.pacientes(id, nome, cpf, telefone, email, status)
select gen_random_uuid(), v.nome, v.cpf, v.telefone, v.email, 'ATIVO'
from (values
('Ana Demo Saúde','00000000000','11900000001','ana.demo@example.invalid'),('Bruno Demo Saúde','00000000001','11900000002','bruno.demo@example.invalid'),('Carla Demo Saúde','00000000002','11900000003','carla.demo@example.invalid'),('Diego Demo Saúde','00000000003','11900000004','diego.demo@example.invalid'),('Elisa Demo Saúde','00000000004','11900000005','elisa.demo@example.invalid')) v(nome,cpf,telefone,email)
where not exists (select 1 from plantaopro.pacientes p where p.cpf=v.cpf and p.reg_status='A');
