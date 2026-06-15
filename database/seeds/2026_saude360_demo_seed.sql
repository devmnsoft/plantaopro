-- Massa demo idempotente PlantãoPro Saúde 360.
-- Dados fictícios; não usar em produção sem revisão.
create extension if not exists pgcrypto;
create schema if not exists plantaopro;

do $$
declare
    v_cliente uuid;
    v_user uuid;
begin
    select id into v_cliente from plantaopro.clientes where reg_status = 'A' order by reg_date nulls last limit 1;
    select id into v_user from plantaopro.usuarios where reg_status = 'A' order by reg_date nulls last limit 1;

    create table if not exists plantaopro.cid_tabela(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, codigo text not null, descricao text not null, categoria text not null default '', status text not null default 'ATIVO', created_by uuid null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');

    insert into plantaopro.cid_tabela(id, cliente_id, tenant_id, codigo, descricao, categoria, status, created_by)
    select gen_random_uuid(), v_cliente, v_cliente, x.codigo, x.descricao, 'DEMO', 'ATIVO', v_user
    from (values
        ('I10','Hipertensão essencial'),('E11','Diabetes mellitus tipo 2'),('J06','Infecção aguda das vias aéreas superiores'),('R51','Cefaleia'),('M54','Dor dorsalgia'),('A09','Diarreia e gastroenterite de origem infecciosa presumível'),('F41','Transtornos ansiosos'),('J45','Asma'),('N39','Transtornos do trato urinário'),('Z00','Exame geral')
    ) as x(codigo, descricao)
    where not exists (select 1 from plantaopro.cid_tabela c where c.codigo = x.codigo and c.reg_status = 'A');

    if to_regclass('plantaopro.pacientes') is not null then
        insert into plantaopro.pacientes(id, cliente_id, nome, cpf, telefone, email, data_nascimento, status, created_by)
        select gen_random_uuid(), v_cliente, 'Paciente Demo ' || gs, '900000000' || lpad(gs::text, 2, '0'), '(11) 90000-00' || lpad(gs::text, 2, '0'), 'paciente.demo.' || gs || '@example.invalid', current_date - (interval '30 years') - (gs || ' months')::interval, case when gs <= 10 then 'ATIVO' else 'INATIVO' end, v_user
        from generate_series(1,12) gs
        where not exists (select 1 from plantaopro.pacientes p where p.email = 'paciente.demo.' || gs || '@example.invalid');
    end if;

    if to_regclass('plantaopro.convenios') is not null then
        insert into plantaopro.convenios(id, cliente_id, nome, codigo_ans, status, created_by)
        select gen_random_uuid(), v_cliente, nome, codigo, 'ATIVO', v_user
        from (values ('Convênio Demo Vida','ANS001'),('Convênio Demo Saúde','ANS002'),('Particular','PART')) x(nome,codigo)
        where not exists (select 1 from plantaopro.convenios c where c.nome = x.nome and (c.cliente_id = v_cliente or v_cliente is null));
    end if;

    if to_regclass('plantaopro.planos_saude') is not null then
        insert into plantaopro.planos_saude(id, cliente_id, nome, operadora, registro_ans, status, created_by)
        select gen_random_uuid(), v_cliente, nome, 'Operadora Demo', codigo, 'ATIVO', v_user
        from (values ('Ambulatorial Demo','P001'),('Hospitalar Demo','P002'),('Executivo Demo','P003'),('Empresarial Demo','P004'),('Odonto Demo','P005')) x(nome,codigo)
        where not exists (select 1 from plantaopro.planos_saude p where p.nome = x.nome and (p.cliente_id = v_cliente or v_cliente is null));
    end if;
end $$;
