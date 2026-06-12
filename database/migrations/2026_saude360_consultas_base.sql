create schema if not exists plantaopro;
create extension if not exists pgcrypto;

create table if not exists plantaopro.consultas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid not null,
    agendamento_id uuid null,
    triagem_id uuid null,
    medico_id uuid null,
    profissional_id uuid null,
    hospital_id uuid null,
    unidade_id uuid null,
    especialidade_id uuid null,
    data_inicio timestamp without time zone null,
    data_fim timestamp without time zone null,
    status text not null default 'AGUARDANDO',
    tipo text not null default 'CONSULTA',
    motivo_atendimento text not null default '',
    resumo text not null default '',
    observacoes text not null default '',
    motivo_cancelamento text not null default '',
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamp without time zone not null default now(),
    reg_update timestamp without time zone null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.consultas
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists agendamento_id uuid null,
    add column if not exists triagem_id uuid null,
    add column if not exists medico_id uuid null,
    add column if not exists profissional_id uuid null,
    add column if not exists hospital_id uuid null,
    add column if not exists unidade_id uuid null,
    add column if not exists especialidade_id uuid null,
    add column if not exists data_inicio timestamp without time zone null,
    add column if not exists data_fim timestamp without time zone null,
    add column if not exists status text not null default 'AGUARDANDO',
    add column if not exists tipo text not null default 'CONSULTA',
    add column if not exists motivo_atendimento text not null default '',
    add column if not exists resumo text not null default '',
    add column if not exists observacoes text not null default '',
    add column if not exists motivo_cancelamento text not null default '',
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_update timestamp without time zone null,
    add column if not exists reg_status char(1) not null default 'A';

alter table if exists plantaopro.consultas
    alter column status set default 'AGUARDANDO',
    alter column tipo set default 'CONSULTA',
    alter column motivo_atendimento set default '',
    alter column resumo set default '',
    alter column observacoes set default '',
    alter column motivo_cancelamento set default '',
    alter column reg_date set default now(),
    alter column reg_status set default 'A';

create index if not exists ix_consultas_cliente_id on plantaopro.consultas (cliente_id);
create index if not exists ix_consultas_paciente_id on plantaopro.consultas (paciente_id);
create index if not exists ix_consultas_agendamento_id on plantaopro.consultas (agendamento_id);
create index if not exists ix_consultas_triagem_id on plantaopro.consultas (triagem_id);
create index if not exists ix_consultas_medico_id on plantaopro.consultas (medico_id);
create index if not exists ix_consultas_status on plantaopro.consultas (status);
create index if not exists ix_consultas_data_inicio on plantaopro.consultas (data_inicio);
create index if not exists ix_consultas_reg_date on plantaopro.consultas (reg_date);

create table if not exists plantaopro.consulta_anamnese (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    consulta_id uuid not null,
    paciente_id uuid not null,
    queixa_principal text not null default '',
    historia_doenca_atual text not null default '',
    antecedentes_pessoais text not null default '',
    antecedentes_familiares text not null default '',
    alergias text not null default '',
    medicamentos_uso text not null default '',
    habitos_vida text not null default '',
    observacoes text not null default '',
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamp without time zone not null default now(),
    reg_update timestamp without time zone null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.consulta_anamnese
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists consulta_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists queixa_principal text not null default '',
    add column if not exists historia_doenca_atual text not null default '',
    add column if not exists antecedentes_pessoais text not null default '',
    add column if not exists antecedentes_familiares text not null default '',
    add column if not exists alergias text not null default '',
    add column if not exists medicamentos_uso text not null default '',
    add column if not exists habitos_vida text not null default '',
    add column if not exists observacoes text not null default '',
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_update timestamp without time zone null,
    add column if not exists reg_status char(1) not null default 'A';

create index if not exists ix_consulta_anamnese_consulta_id on plantaopro.consulta_anamnese (consulta_id);
create index if not exists ix_consulta_anamnese_paciente_id on plantaopro.consulta_anamnese (paciente_id);
create index if not exists ix_consulta_anamnese_cliente_id on plantaopro.consulta_anamnese (cliente_id);

create table if not exists plantaopro.consulta_exame_fisico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    consulta_id uuid not null,
    paciente_id uuid not null,
    descricao_geral text not null default '',
    aparelho_cardiovascular text not null default '',
    aparelho_respiratorio text not null default '',
    abdomen text not null default '',
    neurologico text not null default '',
    pele text not null default '',
    observacoes text not null default '',
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamp without time zone not null default now(),
    reg_update timestamp without time zone null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.consulta_exame_fisico
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists consulta_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists descricao_geral text not null default '',
    add column if not exists aparelho_cardiovascular text not null default '',
    add column if not exists aparelho_respiratorio text not null default '',
    add column if not exists abdomen text not null default '',
    add column if not exists neurologico text not null default '',
    add column if not exists pele text not null default '',
    add column if not exists observacoes text not null default '',
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_update timestamp without time zone null,
    add column if not exists reg_status char(1) not null default 'A';

create index if not exists ix_consulta_exame_fisico_consulta_id on plantaopro.consulta_exame_fisico (consulta_id);
create index if not exists ix_consulta_exame_fisico_paciente_id on plantaopro.consulta_exame_fisico (paciente_id);
create index if not exists ix_consulta_exame_fisico_cliente_id on plantaopro.consulta_exame_fisico (cliente_id);

create table if not exists plantaopro.consulta_diagnosticos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    consulta_id uuid not null,
    paciente_id uuid not null,
    cid_id uuid null,
    cid_codigo text not null default '',
    cid_descricao text not null default '',
    tipo text not null default 'PRINCIPAL',
    hipotese boolean not null default false,
    observacoes text not null default '',
    created_by uuid null,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.consulta_diagnosticos
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists consulta_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists cid_id uuid null,
    add column if not exists cid_codigo text not null default '',
    add column if not exists cid_descricao text not null default '',
    add column if not exists tipo text not null default 'PRINCIPAL',
    add column if not exists hipotese boolean not null default false,
    add column if not exists observacoes text not null default '',
    add column if not exists created_by uuid null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_status char(1) not null default 'A';

create index if not exists ix_consulta_diagnosticos_consulta_id on plantaopro.consulta_diagnosticos (consulta_id);
create index if not exists ix_consulta_diagnosticos_paciente_id on plantaopro.consulta_diagnosticos (paciente_id);
create index if not exists ix_consulta_diagnosticos_cid_codigo on plantaopro.consulta_diagnosticos (cid_codigo);
create index if not exists ix_consulta_diagnosticos_tipo on plantaopro.consulta_diagnosticos (tipo);
create index if not exists ix_consulta_diagnosticos_cliente_id on plantaopro.consulta_diagnosticos (cliente_id);

create table if not exists plantaopro.consulta_condutas (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    consulta_id uuid not null,
    paciente_id uuid not null,
    conduta text not null default '',
    solicitacao_exames text not null default '',
    orientacoes text not null default '',
    retorno_recomendado boolean not null default false,
    retorno_em_dias integer null,
    created_by uuid null,
    updated_by uuid null,
    reg_date timestamp without time zone not null default now(),
    reg_update timestamp without time zone null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.consulta_condutas
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists consulta_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists conduta text not null default '',
    add column if not exists solicitacao_exames text not null default '',
    add column if not exists orientacoes text not null default '',
    add column if not exists retorno_recomendado boolean not null default false,
    add column if not exists retorno_em_dias integer null,
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_update timestamp without time zone null,
    add column if not exists reg_status char(1) not null default 'A';

create index if not exists ix_consulta_condutas_consulta_id on plantaopro.consulta_condutas (consulta_id);
create index if not exists ix_consulta_condutas_paciente_id on plantaopro.consulta_condutas (paciente_id);
create index if not exists ix_consulta_condutas_cliente_id on plantaopro.consulta_condutas (cliente_id);

create table if not exists plantaopro.consulta_encaminhamentos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    consulta_id uuid not null,
    paciente_id uuid not null,
    especialidade_destino_id uuid null,
    descricao text not null default '',
    prioridade text not null default '',
    observacoes text not null default '',
    created_by uuid null,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.consulta_encaminhamentos
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists consulta_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists especialidade_destino_id uuid null,
    add column if not exists descricao text not null default '',
    add column if not exists prioridade text not null default '',
    add column if not exists observacoes text not null default '',
    add column if not exists created_by uuid null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_status char(1) not null default 'A';

create index if not exists ix_consulta_encaminhamentos_consulta_id on plantaopro.consulta_encaminhamentos (consulta_id);
create index if not exists ix_consulta_encaminhamentos_paciente_id on plantaopro.consulta_encaminhamentos (paciente_id);
create index if not exists ix_consulta_encaminhamentos_cliente_id on plantaopro.consulta_encaminhamentos (cliente_id);

create table if not exists plantaopro.consulta_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    consulta_id uuid not null,
    paciente_id uuid not null,
    acao text not null default '',
    detalhe text not null default '',
    usuario_id uuid null,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.consulta_historico
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists consulta_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists acao text not null default '',
    add column if not exists detalhe text not null default '',
    add column if not exists usuario_id uuid null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_status char(1) not null default 'A';

create index if not exists ix_consulta_historico_consulta_id on plantaopro.consulta_historico (consulta_id);
create index if not exists ix_consulta_historico_paciente_id on plantaopro.consulta_historico (paciente_id);
create index if not exists ix_consulta_historico_acao on plantaopro.consulta_historico (acao);
create index if not exists ix_consulta_historico_cliente_id on plantaopro.consulta_historico (cliente_id);
create index if not exists ix_consulta_historico_reg_date on plantaopro.consulta_historico (reg_date);

do $$
begin
    if not exists (
        select 1
        from pg_constraint
        where conname = 'ck_consultas_data_fim_maior_inicio'
          and conrelid = 'plantaopro.consultas'::regclass
    ) then
        alter table plantaopro.consultas
            add constraint ck_consultas_data_fim_maior_inicio
            check (data_inicio is null or data_fim is null or data_fim > data_inicio) not valid;
    end if;
end $$;

do $$
begin
    if not exists (
        select 1
        from pg_constraint
        where conname = 'ck_consultas_status_valido'
          and conrelid = 'plantaopro.consultas'::regclass
    ) then
        alter table plantaopro.consultas
            add constraint ck_consultas_status_valido
            check (status in ('AGUARDANDO', 'EM_ATENDIMENTO', 'EM_PREENCHIMENTO', 'FINALIZADA', 'CANCELADA', 'RETORNO_SOLICITADO')) not valid;
    end if;
end $$;
