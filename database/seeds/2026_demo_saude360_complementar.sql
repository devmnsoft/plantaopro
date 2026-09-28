set search_path to plantaopro, public;

-- Shim legado: convenios/planos_saude adotaram o shape atual (sem cliente_id/
-- operadora/codigo_ans/reg_date/reg_status como colunas); valores auxiliares
-- preservados no jsonb "dados". Deduplicação por nome mantém a semântica original
-- (os seeds demo compartilhavam o mesmo primeiro cliente).
do $$
declare
    v_cliente uuid;
begin
    if to_regclass('plantaopro.clientes') is not null then
        select id into v_cliente from plantaopro.clientes order by criado_em nulls last, id limit 1;
    end if;

    if to_regclass('plantaopro.convenios') is not null then
        insert into plantaopro.convenios(codigo, nome, status, dados)
        select v.codigo, v.nome, 'ATIVO', jsonb_build_object('codigoAns', v.codigo, 'clienteId', v_cliente)
        from (values ('Convênio Demo Vida','ANSDEMO01'),('Convênio Demo Saúde','ANSDEMO02'),('Particular','PART')) v(nome,codigo)
        where not exists (select 1 from plantaopro.convenios x where upper(x.nome) = upper(v.nome));
    end if;

    if to_regclass('plantaopro.planos_saude') is not null then
        insert into plantaopro.planos_saude(codigo, nome, status, dados)
        select v.codigo, v.nome, 'ATIVO', jsonb_build_object('operadora', 'Operadora Demo', 'registroAns', v.codigo, 'clienteId', v_cliente)
        from (values ('Ambulatorial Demo','PDEMO01'),('Hospitalar Demo','PDEMO02'),('Executivo Demo','PDEMO03')) v(nome,codigo)
        where not exists (select 1 from plantaopro.planos_saude x where upper(x.nome) = upper(v.nome));
    end if;

    if to_regclass('plantaopro.plano_saude_pacientes') is not null and to_regclass('plantaopro.pacientes') is not null and to_regclass('plantaopro.planos_saude') is not null then
        insert into plantaopro.plano_saude_pacientes(plano_saude_id, paciente_id, numero_carteirinha, principal, status)
        select ps.id, fp.paciente_id, 'DEMO-' || substr(fp.paciente_id::text, 1, 8), true, 'ATIVO'
        from (select id as paciente_id from plantaopro.pacientes where reg_status = 'A' order by reg_date nulls last, id limit 1) fp
        cross join (select id from plantaopro.planos_saude where nome in ('Ambulatorial Demo','Hospitalar Demo','Executivo Demo')) ps
        where not exists (select 1 from plantaopro.plano_saude_pacientes v where v.paciente_id = fp.paciente_id and v.plano_saude_id = ps.id and v.reg_status = 'A');
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
