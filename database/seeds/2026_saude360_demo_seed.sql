-- Massa demo idempotente PlantãoPro Saúde 360.
-- Dados fictícios; não usar em produção sem revisão.
-- Shim legado: clientes/pacientes/convenios/planos_saude adotaram o shape atual;
-- campos auxiliares (telefone/email/data_nascimento, operadora/registro_ans)
-- preservados no jsonb "dados"; deduplicação por cpf/nome mantém a semântica original.
create extension if not exists pgcrypto;
create schema if not exists plantaopro;

do $$
declare
    v_cliente uuid;
    v_user uuid;
begin
    select id into v_cliente from plantaopro.clientes where reg_status = 'A' order by criado_em nulls last, id limit 1;
    select id into v_user from plantaopro.usuarios where reg_status = 'A' order by reg_date nulls last, id limit 1;

    create table if not exists plantaopro.cid_tabela(id uuid primary key default gen_random_uuid(), cliente_id uuid null, tenant_id uuid null, codigo text not null, descricao text not null, categoria text not null default '', status text not null default 'ATIVO', created_by uuid null, reg_date timestamptz not null default now(), reg_status char(1) not null default 'A');

    insert into plantaopro.cid_tabela(id, cliente_id, tenant_id, codigo, descricao, categoria, status, created_by)
    select gen_random_uuid(), v_cliente, v_cliente, x.codigo, x.descricao, 'DEMO', 'ATIVO', v_user
    from (values
        ('I10','Hipertensão essencial'),('E11','Diabetes mellitus tipo 2'),('J06','Infecção aguda das vias aéreas superiores'),('R51','Cefaleia'),('M54','Dor dorsalgia'),('A09','Diarreia e gastroenterite de origem infecciosa presumível'),('F41','Transtornos ansiosos'),('J45','Asma'),('N39','Transtornos do trato urinário'),('Z00','Exame geral')
    ) as x(codigo, descricao)
    where not exists (select 1 from plantaopro.cid_tabela c where c.codigo = x.codigo and c.reg_status = 'A');

    if to_regclass('plantaopro.pacientes') is not null then
        insert into plantaopro.pacientes(id, cliente_id, nome, cpf, status, dados)
        select gen_random_uuid(), v_cliente, 'Paciente Demo ' || gs, '900000000' || lpad(gs::text, 2, '0'), case when gs <= 10 then 'ATIVO' else 'INATIVO' end,
               jsonb_build_object('telefone', '(11) 90000-00' || lpad(gs::text, 2, '0'), 'email', 'paciente.demo.' || gs || '@example.invalid', 'dataNascimento', (current_date - (interval '30 years') - (gs || ' months')::interval)::text)
        from generate_series(1,12) gs
        where not exists (select 1 from plantaopro.pacientes p where p.cpf = '900000000' || lpad(gs::text, 2, '0'));
    end if;

    if to_regclass('plantaopro.convenios') is not null then
        insert into plantaopro.convenios(codigo, nome, status, dados)
        select x.codigo, x.nome, 'ATIVO', jsonb_build_object('codigoAns', x.codigo, 'clienteId', v_cliente)
        from (values ('Convênio Demo Vida','ANS001'),('Convênio Demo Saúde','ANS002'),('Particular','PART')) x(nome,codigo)
        where not exists (select 1 from plantaopro.convenios c where upper(c.nome) = upper(x.nome));
    end if;

    if to_regclass('plantaopro.planos_saude') is not null then
        insert into plantaopro.planos_saude(codigo, nome, status, dados)
        select x.codigo, x.nome, 'ATIVO', jsonb_build_object('operadora', 'Operadora Demo', 'registroAns', x.codigo, 'clienteId', v_cliente)
        from (values ('Ambulatorial Demo','P001'),('Hospitalar Demo','P002'),('Executivo Demo','P003'),('Empresarial Demo','P004'),('Odonto Demo','P005')) x(nome,codigo)
        where not exists (select 1 from plantaopro.planos_saude p where upper(p.nome) = upper(x.nome));
    end if;
end $$;
