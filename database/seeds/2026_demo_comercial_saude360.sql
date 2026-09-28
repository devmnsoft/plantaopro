-- Seed idempotente de demonstração comercial PlantãoPro Saúde 360.
-- Dados 100% fictícios; CPFs usam sequências inválidas para evitar dados reais.
-- Shim legado: cria cid_tabela quando ausente (DDL idêntico ao de 2026_saude360_demo_seed.sql)
-- e preserva telefone/email no jsonb "dados" de pacientes (shape atual da tabela).
create schema if not exists plantaopro;

do $$
begin
    if to_regclass('plantaopro.cid_tabela') is null then
        create table if not exists plantaopro.cid_tabela(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, codigo text not null, descricao text not null, categoria text not null default '', status text not null default 'ATIVO', created_by uuid null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');
    end if;

    insert into plantaopro.cid_tabela(codigo, descricao, categoria, status)
    select v.codigo, v.descricao, 'DEMO', 'ATIVO'
    from (values
    ('A09','Gastroenterite infecciosa demo'),('J00','Nasofaringite aguda demo'),('J45','Asma demo'),('I10','Hipertensão essencial demo'),('E11','Diabetes mellitus tipo 2 demo'),('R50','Febre demo'),('M54','Dor lombar demo'),('N39','Infecção urinária demo'),('F41','Ansiedade demo'),('Z00','Exame geral demo')) v(codigo,descricao)
    where not exists (select 1 from plantaopro.cid_tabela c where c.codigo=v.codigo and c.reg_status='A');
end $$;

insert into plantaopro.pacientes(id, nome, cpf, status, dados)
select gen_random_uuid(), v.nome, v.cpf, 'ATIVO', jsonb_build_object('telefone', v.telefone, 'email', v.email)
from (values
('Ana Demo Saúde','00000000000','11900000001','ana.demo@example.invalid'),('Bruno Demo Saúde','00000000001','11900000002','bruno.demo@example.invalid'),('Carla Demo Saúde','00000000002','11900000003','carla.demo@example.invalid'),('Diego Demo Saúde','00000000003','11900000004','diego.demo@example.invalid'),('Elisa Demo Saúde','00000000004','11900000005','elisa.demo@example.invalid')) v(nome,cpf,telefone,email)
where not exists (select 1 from plantaopro.pacientes p where p.cpf=v.cpf and p.reg_status='A');
