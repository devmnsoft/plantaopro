-- Shim legado: colunas realinhadas ao shape atual das tabelas. Campos auxiliares que
-- sumiram do schema (telefone/email, cnpj/observacoes, total_entradas/saidas, origem/
-- valor_pago, versao/fonte) foram preservados no jsonb "dados". Order-by usam id como
-- desempate para seleções determinísticas entre execuções.
do $$
declare
    v_cliente uuid;
    v_paciente uuid;
    v_convenio uuid;
    v_plano uuid;
    v_conta uuid;
begin
    if to_regclass('plantaopro.clientes') is not null then
        select id into v_cliente from plantaopro.clientes where reg_status='A' order by criado_em nulls last, id limit 1;
    end if;

    if to_regclass('plantaopro.pacientes') is not null then
        select id into v_paciente from plantaopro.pacientes where reg_status='A' and (v_cliente is null or cliente_id=v_cliente or cliente_id is null) order by reg_date nulls last, id limit 1;
        if v_paciente is null then
            insert into plantaopro.pacientes(cliente_id,nome,status,dados)
            values(v_cliente,'Paciente Demo Saúde 360','ATIVO',jsonb_build_object('telefone','(11) 90000-0000','email','paciente.demo@exemplo.local','observacoes','Paciente fictício para demonstração')) returning id into v_paciente;
        end if;
    end if;

    if to_regclass('plantaopro.convenios') is not null then
        insert into plantaopro.convenios(codigo,nome,status,dados)
        select x.codigo, x.nome, 'ATIVO', jsonb_build_object('cnpj','','observacoes','Convênio fictício para demo Saúde 360','clienteId',v_cliente)
        from (values ('Particular','PARTICULAR'),('Unimed Demo','UNIMED-DEMO'),('Bradesco Saúde Demo','BRADESCO-DEMO'),('Amil Demo','AMIL-DEMO'),('SulAmérica Demo','SULAMERICA-DEMO')) x(nome,codigo)
        where not exists (select 1 from plantaopro.convenios c where upper(c.nome)=upper(x.nome) or upper(c.codigo)=upper(x.codigo));
        select id into v_convenio from plantaopro.convenios order by criado_em nulls last, id limit 1;
    end if;

    if to_regclass('plantaopro.convenio_planos') is not null then
        insert into plantaopro.convenio_planos(cliente_id,convenio_id,nome,codigo,tipo_acomodacao,coparticipacao_percentual,status)
        select v_cliente, v_convenio, x.nome, upper(replace(x.nome,' ','-')), x.acomodacao, x.copart, 'ATIVO'
        from (values ('Básico','ENFERMARIA',10),('Especial','APARTAMENTO',5),('Executivo','APARTAMENTO',0),('Empresarial','MISTO',15)) x(nome,acomodacao,copart)
        where v_convenio is not null and not exists (select 1 from plantaopro.convenio_planos p where p.convenio_id=v_convenio and p.nome=x.nome and p.reg_status='A');
    end if;

    if to_regclass('plantaopro.planos_saude') is not null then
        insert into plantaopro.planos_saude(codigo,nome,status,dados)
        select x.codigo, x.nome, 'ATIVO', jsonb_build_object('tipo',x.tipo,'observacoes','Plano fictício para demo','convenioId',v_convenio,'clienteId',v_cliente)
        from (values ('Plano Ambulatorial Demo','AMB-DEMO','AMBULATORIAL'),('Plano Hospitalar Demo','HOSP-DEMO','HOSPITALAR'),('Plano Empresarial Demo','EMP-DEMO','EMPRESARIAL')) x(nome,codigo,tipo)
        where not exists (select 1 from plantaopro.planos_saude p where upper(p.codigo)=upper(x.codigo));
        select id into v_plano from plantaopro.planos_saude order by criado_em nulls last, id limit 1;
    end if;

    if to_regclass('plantaopro.plano_saude_pacientes') is not null and v_paciente is not null and v_plano is not null then
        insert into plantaopro.plano_saude_pacientes(cliente_id,paciente_id,plano_saude_id,numero_carteirinha,validade,titular_nome,principal,status)
        select v_cliente, v_paciente, v_plano, 'DEMO-0001', current_date + interval '1 year', 'Titular Demo', true, 'ATIVO'
        where not exists (select 1 from plantaopro.plano_saude_pacientes p where p.paciente_id=v_paciente and p.plano_saude_id=v_plano and p.reg_status='A');
    end if;

    if to_regclass('plantaopro.convenio_autorizacoes') is not null then
        insert into plantaopro.convenio_autorizacoes(cliente_id,paciente_id,convenio_id,plano_saude_id,numero_guia,procedimento,valor_autorizado,status,motivo_negativa,validade)
        select v_cliente, v_paciente, v_convenio, v_plano, 'GUIA-DEMO-' || x.n, x.proc, x.valor, x.status, x.motivo, current_date + interval '30 days'
        from (values (1,'Consulta clínica',180,'PENDENTE',''),(2,'Retorno',120,'PENDENTE',''),(3,'Exame laboratorial',90,'PENDENTE',''),(4,'Consulta cardiologia',260,'AUTORIZADA',''),(5,'Ultrassom',300,'AUTORIZADA',''),(6,'Procedimento não coberto',500,'NEGADA','Cobertura não prevista no plano demo')) x(n,proc,valor,status,motivo)
        where not exists (select 1 from plantaopro.convenio_autorizacoes a where a.numero_guia='GUIA-DEMO-' || x.n and a.reg_status='A');
    end if;

    if to_regclass('plantaopro.clinica_caixa') is not null then
        -- Tabela legado rica (sem jsonb dados): usa coluna de observação existente.
        insert into plantaopro.clinica_caixa(cliente_id,saldo_inicial,aberto_em,status,conferencia_observacao)
        select v_cliente,100,now(),'ABERTO','Caixa demo aberto Saúde 360'
        where not exists (select 1 from plantaopro.clinica_caixa c where c.conferencia_observacao='Caixa demo aberto Saúde 360' and c.reg_status='A');
    end if;
    if to_regclass('plantaopro.clinica_contas_receber') is not null then
        -- tipo_recebimento -> forma_cobranca; "valor pago" derivado de valor_total - valor_pendente.
        insert into plantaopro.clinica_contas_receber(cliente_id,paciente_id,convenio_id,plano_saude_id,descricao,forma_cobranca,valor_total,valor_pendente,vencimento,status)
        select v_cliente,v_paciente,v_convenio,v_plano,x.desc_texto,x.tipo,x.total,x.total-x.pago,current_date+x.dias,x.status
        from (values ('Consulta particular demo','PARTICULAR',180,180,-2,'RECEBIDO'),('Consulta convênio demo','CONVENIO',220,0,5,'ABERTO'),('Retorno demo','PARTICULAR',120,120,1,'RECEBIDO'),('Exame demo','CONVENIO',90,0,10,'ABERTO'),('Conta vencida demo','PARTICULAR',150,0,-10,'VENCIDA')) x(desc_texto,tipo,total,pago,dias,status)
        where not exists (select 1 from plantaopro.clinica_contas_receber c where c.descricao=x.desc_texto and c.reg_status='A');
        select id into v_conta from plantaopro.clinica_contas_receber where (valor_total-valor_pendente) > 0 and reg_status='A' and descricao in ('Consulta particular demo','Retorno demo','Consulta convênio demo','Exame demo','Conta vencida demo') order by reg_date nulls last, descricao limit 1;
    end if;
    if to_regclass('plantaopro.clinica_recebimentos') is not null then
        insert into plantaopro.clinica_recebimentos(cliente_id,conta_receber_id,valor,forma_pagamento,status)
        select v_cliente, c.id, c.valor_total - c.valor_pendente, case when row_number() over(order by c.reg_date nulls last, c.id) = 1 then 'PIX' else 'CARTAO_CREDITO' end, 'CONFIRMADO'
        from plantaopro.clinica_contas_receber c where (c.valor_total-c.valor_pendente) > 0 and c.reg_status='A' and c.descricao in ('Consulta particular demo','Retorno demo','Consulta convênio demo','Exame demo','Conta vencida demo') and not exists (select 1 from plantaopro.clinica_recebimentos r where r.conta_receber_id=c.id and r.reg_status='A') limit 3;
    end if;
    if to_regclass('plantaopro.clinica_estornos') is not null and v_conta is not null then
        insert into plantaopro.clinica_estornos(cliente_id,recebimento_id,motivo,valor,status)
        select v_cliente, null, 'Estorno fictício de demonstração', 10, 'ESTORNADO'
        where not exists (select 1 from plantaopro.clinica_estornos e where e.motivo='Estorno fictício de demonstração' and e.reg_status='A');
    end if;

    if to_regclass('plantaopro.cid_tabela') is not null then
        insert into plantaopro.cid_tabela(codigo,descricao,categoria,status)
        select x.codigo, x.descricao, 'DEMO', 'ATIVO'
        from (values ('I10','Hipertensão essencial'),('E11','Diabetes mellitus tipo 2'),('J06','Infecção aguda das vias aéreas superiores'),('R51','Cefaleia'),('M54','Dorsalgia'),('A09','Diarreia e gastroenterite'),('F41','Transtornos ansiosos'),('J45','Asma'),('N39','Transtornos do trato urinário'),('Z00','Exame geral')) x(codigo,descricao)
        where not exists (select 1 from plantaopro.cid_tabela c where upper(c.codigo)=upper(x.codigo) and c.reg_status='A');
    end if;
end $$;
