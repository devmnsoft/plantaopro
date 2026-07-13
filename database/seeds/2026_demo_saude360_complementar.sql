set search_path to plantaopro, public;

do $$
begin
    if to_regclass('plantaopro.convenios') is not null and to_regclass('plantaopro.clientes') is not null then
        insert into plantaopro.convenios(id, cliente_id, nome, codigo, codigo_ans, status, reg_date, reg_status)
        select gen_random_uuid(), c.id, v.nome, v.codigo, v.codigo, 'ATIVO', now(), 'A'
        from (select id from plantaopro.clientes where reg_status = 'A' order by reg_date limit 1) c
        cross join (values ('Convênio Demo Vida','ANSDEMO01'),('Convênio Demo Saúde','ANSDEMO02'),('Particular','PART')) v(nome,codigo)
        where not exists (select 1 from plantaopro.convenios x where x.cliente_id = c.id and x.nome = v.nome and x.reg_status = 'A');
    end if;

    if to_regclass('plantaopro.planos_saude') is not null and to_regclass('plantaopro.clientes') is not null then
        insert into plantaopro.planos_saude(id, cliente_id, nome, operadora, codigo, registro_ans, status, reg_date, reg_status)
        select gen_random_uuid(), c.id, v.nome, 'Operadora Demo', v.codigo, v.codigo, 'ATIVO', now(), 'A'
        from (select id from plantaopro.clientes where reg_status = 'A' order by reg_date limit 1) c
        cross join (values ('Ambulatorial Demo','PDEMO01'),('Hospitalar Demo','PDEMO02'),('Executivo Demo','PDEMO03')) v(nome,codigo)
        where not exists (select 1 from plantaopro.planos_saude x where x.cliente_id = c.id and x.nome = v.nome and x.reg_status = 'A');
    end if;

    if to_regclass('plantaopro.plano_saude_pacientes') is not null and to_regclass('plantaopro.pacientes') is not null and to_regclass('plantaopro.planos_saude') is not null then
        insert into plantaopro.plano_saude_pacientes(id, cliente_id, plano_saude_id, paciente_id, numero_carteirinha, principal, status, reg_date, reg_status)
        select gen_random_uuid(), p.cliente_id, ps.id, p.id, 'DEMO-' || substr(p.id::text, 1, 8), true, 'ATIVO', now(), 'A'
        from plantaopro.pacientes p join plantaopro.planos_saude ps on ps.cliente_id = p.cliente_id
        where p.reg_status = 'A' and ps.reg_status = 'A'
          and not exists (select 1 from plantaopro.plano_saude_pacientes v where v.paciente_id = p.id and v.plano_saude_id = ps.id and v.reg_status = 'A')
        limit 3;
    end if;

    if to_regclass('plantaopro.convenio_autorizacoes') is not null and to_regclass('plantaopro.pacientes') is not null and to_regclass('plantaopro.convenios') is not null then
        insert into plantaopro.convenio_autorizacoes(id, cliente_id, convenio_id, paciente_id, motivo, procedimento, status, reg_date, reg_status)
        select gen_random_uuid(), p.cliente_id, c.id, p.id, 'Autorização demo para consulta ambulatorial', 'Consulta ambulatorial', 'AUTORIZADA', now(), 'A'
        from plantaopro.pacientes p join plantaopro.convenios c on c.cliente_id = p.cliente_id
        where p.reg_status = 'A' and c.reg_status = 'A'
          and not exists (select 1 from plantaopro.convenio_autorizacoes a where a.paciente_id = p.id and a.convenio_id = c.id and a.reg_status = 'A')
        limit 3;
    end if;
end $$;
