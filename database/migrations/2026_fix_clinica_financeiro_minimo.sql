set search_path to plantaopro, public;

create table if not exists plantaopro.clinica_contas_receber (
    id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, paciente_id uuid null,
    agendamento_id uuid null, consulta_id uuid null, convenio_id uuid null, plano_saude_id uuid null, medico_id uuid null,
    descricao text not null default '', origem text not null default 'MANUAL', tipo_recebimento text not null default 'PARTICULAR',
    valor_total numeric(14,2) not null default 0, valor_pago numeric(14,2) not null default 0, valor_desconto numeric(14,2) not null default 0,
    valor_pendente numeric(14,2) not null default 0, vencimento date null, status text not null default 'ABERTO', observacoes text not null default '',
    created_by uuid null, updated_by uuid null, reg_date timestamp without time zone not null default now(), reg_update timestamp without time zone null, reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.clinica_contas_receber add column if not exists tenant_id uuid null;
alter table if exists plantaopro.clinica_contas_receber add column if not exists cliente_id uuid null;
alter table if exists plantaopro.clinica_contas_receber add column if not exists paciente_id uuid null;
alter table if exists plantaopro.clinica_contas_receber add column if not exists agendamento_id uuid null;
alter table if exists plantaopro.clinica_contas_receber add column if not exists consulta_id uuid null;
alter table if exists plantaopro.clinica_contas_receber add column if not exists convenio_id uuid null;
alter table if exists plantaopro.clinica_contas_receber add column if not exists plano_saude_id uuid null;
alter table if exists plantaopro.clinica_contas_receber add column if not exists medico_id uuid null;
alter table if exists plantaopro.clinica_contas_receber add column if not exists descricao text not null default '';
alter table if exists plantaopro.clinica_contas_receber add column if not exists origem text not null default 'MANUAL';
alter table if exists plantaopro.clinica_contas_receber add column if not exists tipo_recebimento text not null default 'PARTICULAR';
alter table if exists plantaopro.clinica_contas_receber add column if not exists valor_total numeric(14,2) not null default 0;
alter table if exists plantaopro.clinica_contas_receber add column if not exists valor_pago numeric(14,2) not null default 0;
alter table if exists plantaopro.clinica_contas_receber add column if not exists valor_desconto numeric(14,2) not null default 0;
alter table if exists plantaopro.clinica_contas_receber add column if not exists valor_pendente numeric(14,2) not null default 0;
alter table if exists plantaopro.clinica_contas_receber add column if not exists vencimento date null;
alter table if exists plantaopro.clinica_contas_receber add column if not exists status text not null default 'ABERTO';
alter table if exists plantaopro.clinica_contas_receber add column if not exists observacoes text not null default '';
alter table if exists plantaopro.clinica_contas_receber add column if not exists created_by uuid null;
alter table if exists plantaopro.clinica_contas_receber add column if not exists updated_by uuid null;
alter table if exists plantaopro.clinica_contas_receber add column if not exists reg_date timestamp without time zone not null default now();
alter table if exists plantaopro.clinica_contas_receber add column if not exists reg_update timestamp without time zone null;
alter table if exists plantaopro.clinica_contas_receber add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.clinica_recebimentos (
    id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, conta_receber_id uuid null, paciente_id uuid null,
    valor numeric(14,2) not null default 0, forma_pagamento text not null default '', data_recebimento timestamp without time zone not null default now(),
    comprovante text not null default '', observacoes text not null default '', status text not null default 'CONFIRMADO', created_by uuid null,
    reg_date timestamp without time zone not null default now(), reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.clinica_caixa (
    id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, unidade_id uuid null, usuario_abertura_id uuid null, usuario_fechamento_id uuid null,
    data_abertura timestamp without time zone not null default now(), data_fechamento timestamp without time zone null,
    saldo_inicial numeric(14,2) not null default 0, total_entradas numeric(14,2) not null default 0, total_saidas numeric(14,2) not null default 0,
    saldo_final numeric(14,2) not null default 0, status text not null default 'ABERTO', observacoes text not null default '', created_by uuid null, updated_by uuid null,
    reg_date timestamp without time zone not null default now(), reg_update timestamp without time zone null, reg_status char(1) not null default 'A'
);


alter table if exists plantaopro.clinica_recebimentos add column if not exists tenant_id uuid null;
alter table if exists plantaopro.clinica_recebimentos add column if not exists cliente_id uuid null;
alter table if exists plantaopro.clinica_recebimentos add column if not exists conta_receber_id uuid null;
alter table if exists plantaopro.clinica_recebimentos add column if not exists paciente_id uuid null;
alter table if exists plantaopro.clinica_recebimentos add column if not exists valor numeric(14,2) not null default 0;
alter table if exists plantaopro.clinica_recebimentos add column if not exists forma_pagamento text not null default '';
alter table if exists plantaopro.clinica_recebimentos add column if not exists data_recebimento timestamp without time zone not null default now();
alter table if exists plantaopro.clinica_recebimentos add column if not exists comprovante text not null default '';
alter table if exists plantaopro.clinica_recebimentos add column if not exists observacoes text not null default '';
alter table if exists plantaopro.clinica_recebimentos add column if not exists status text not null default 'CONFIRMADO';
alter table if exists plantaopro.clinica_recebimentos add column if not exists created_by uuid null;
alter table if exists plantaopro.clinica_recebimentos add column if not exists reg_date timestamp without time zone not null default now();
alter table if exists plantaopro.clinica_recebimentos add column if not exists reg_status char(1) not null default 'A';

alter table if exists plantaopro.clinica_caixa add column if not exists tenant_id uuid null;
alter table if exists plantaopro.clinica_caixa add column if not exists cliente_id uuid null;
alter table if exists plantaopro.clinica_caixa add column if not exists status text not null default 'ABERTO';
alter table if exists plantaopro.clinica_caixa add column if not exists reg_date timestamp without time zone not null default now();
alter table if exists plantaopro.clinica_caixa add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.clinica_fechamentos_caixa (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, caixa_id uuid null, valor_informado numeric(14,2) not null default 0, diferenca numeric(14,2) not null default 0, status text not null default 'FECHADO', observacoes text not null default '', created_by uuid null, reg_date timestamp without time zone not null default now(), reg_status char(1) not null default 'A');
create table if not exists plantaopro.clinica_financeiro_historico (id uuid primary key default gen_random_uuid(), tenant_id uuid null, cliente_id uuid null, entidade text not null default '', entidade_id uuid null, acao text not null default '', detalhes jsonb not null default '{}'::jsonb, usuario_id uuid null, reg_date timestamp without time zone not null default now(), reg_status char(1) not null default 'A');

create index if not exists ix_clinica_contas_receber_cliente_id on plantaopro.clinica_contas_receber(cliente_id);
create index if not exists ix_clinica_contas_receber_paciente_id on plantaopro.clinica_contas_receber(paciente_id);
create index if not exists ix_clinica_contas_receber_consulta_id on plantaopro.clinica_contas_receber(consulta_id);
create index if not exists ix_clinica_contas_receber_agendamento_id on plantaopro.clinica_contas_receber(agendamento_id);
create index if not exists ix_clinica_contas_receber_status on plantaopro.clinica_contas_receber(status);
create index if not exists ix_clinica_contas_receber_reg_date on plantaopro.clinica_contas_receber(reg_date);
create index if not exists ix_clinica_recebimentos_cliente_id on plantaopro.clinica_recebimentos(cliente_id);
create index if not exists ix_clinica_recebimentos_paciente_id on plantaopro.clinica_recebimentos(paciente_id);
create index if not exists ix_clinica_recebimentos_status on plantaopro.clinica_recebimentos(status);
create index if not exists ix_clinica_caixa_cliente_id on plantaopro.clinica_caixa(cliente_id);
create index if not exists ix_clinica_caixa_status on plantaopro.clinica_caixa(status);
create index if not exists ix_clinica_caixa_reg_date on plantaopro.clinica_caixa(reg_date);

insert into plantaopro.clinica_caixa(saldo_inicial, total_entradas, saldo_final, status, observacoes)
select 100, 380, 480, 'ABERTO', 'Caixa demo aberto' where not exists (select 1 from plantaopro.clinica_caixa where observacoes = 'Caixa demo aberto' and reg_status = 'A');

insert into plantaopro.clinica_contas_receber(descricao, origem, tipo_recebimento, valor_total, valor_pago, valor_pendente, vencimento, status, observacoes)
select v.descricao, 'DEMO', 'PARTICULAR', v.valor, v.pago, v.valor - v.pago, current_date + v.dias, v.status, 'Massa demo financeiro Saúde 360'
from (values ('Consulta clínica demo',180.00,180.00,0,'RECEBIDO'),('Retorno particular demo',120.00,0.00,7,'ABERTO'),('Procedimento ambulatorial demo',260.00,0.00,-5,'VENCIDA')) as v(descricao, valor, pago, dias, status)
where not exists (select 1 from plantaopro.clinica_contas_receber c where c.descricao = v.descricao and c.origem = 'DEMO' and c.reg_status = 'A');

insert into plantaopro.clinica_recebimentos(conta_receber_id, valor, forma_pagamento, observacoes, status)
select c.id, c.valor_pago, case when c.descricao like 'Consulta%' then 'PIX' else 'CARTAO_DEBITO' end, 'Recebimento demo Saúde 360', 'CONFIRMADO'
from plantaopro.clinica_contas_receber c
where c.origem = 'DEMO' and c.valor_pago > 0 and not exists (select 1 from plantaopro.clinica_recebimentos r where r.conta_receber_id = c.id and r.reg_status = 'A');

-- Compatibilidade com serviços legados que ainda gravam updated_at/reg_update em ações genéricas.
alter table if exists plantaopro.clinica_contas_receber add column if not exists updated_at timestamp without time zone null;
alter table if exists plantaopro.clinica_recebimentos add column if not exists updated_at timestamp without time zone null;
alter table if exists plantaopro.clinica_recebimentos add column if not exists updated_by uuid null;
alter table if exists plantaopro.clinica_recebimentos add column if not exists reg_update timestamp without time zone null;
alter table if exists plantaopro.clinica_caixa add column if not exists updated_at timestamp without time zone null;
