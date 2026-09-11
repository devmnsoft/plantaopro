-- v2.15.9 - invariantes concorrentes da jornada Saúde 360.
-- Incremental e idempotente: não altera migrations já aplicadas.
set search_path to plantaopro, public;

create extension if not exists btree_gist;

-- CPF é opcional, porém único por cliente quando informado e normalizado.
create unique index if not exists ux_v2159_paciente_cpf_cliente
    on plantaopro.pacientes (cliente_id, regexp_replace(cpf, '[^0-9]', '', 'g'))
    where reg_status = 'A' and cpf is not null and regexp_replace(cpf, '[^0-9]', '', 'g') <> '';

-- O banco, e não somente a validação prévia da API, arbitra reservas concorrentes.
do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'ck_v2159_agendamento_periodo') then
        alter table plantaopro.agendamentos add constraint ck_v2159_agendamento_periodo check (data_fim > data_inicio) not valid;
    end if;
    if not exists (select 1 from pg_constraint where conname = 'ex_v2159_agendamento_medico') then
        alter table plantaopro.agendamentos add constraint ex_v2159_agendamento_medico
            exclude using gist (cliente_id with =, medico_id with =, tstzrange(data_inicio, data_fim, '[)') with &&)
            where (reg_status = 'A' and status not in ('CANCELADO','REAGENDADO','FALTOU'));
    end if;
end $$;

-- Repetições/retries não podem gerar duas chegadas ou duas posições ativas.
create unique index if not exists ux_v2159_checkin_ativo
    on plantaopro.agendamento_checkins (cliente_id, agendamento_id) where reg_status = 'A';
create unique index if not exists ux_v2159_fila_painel_ativa
    on plantaopro.painel_chamada_fila (cliente_id, agendamento_id) where reg_status = 'A' and agendamento_id is not null;
create unique index if not exists ux_v2159_fila_triagem_ativa
    on plantaopro.triagem_fila (cliente_id, agendamento_id) where reg_status = 'A' and agendamento_id is not null;

create index if not exists ix_v2159_recepcao_dia
    on plantaopro.agendamentos (cliente_id, unidade_id, data_inicio, id)
    where reg_status = 'A';
create index if not exists ix_v2159_fila_ordenacao
    on plantaopro.painel_chamada_fila (cliente_id, status, prioridade desc, reg_date, id)
    where reg_status = 'A';
