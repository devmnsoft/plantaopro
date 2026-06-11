set search_path to plantaopro, public;

create schema if not exists plantaopro;
create extension if not exists pgcrypto;
create sequence if not exists plantaopro.seq_painel_senhas start 1;

create table if not exists plantaopro.pacientes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    nome_completo text not null default '',
    nome text not null default '',
    nome_social text not null default '',
    data_nascimento date null,
    sexo text not null default '',
    sexo_genero text not null default '',
    cpf text not null default '',
    cns text not null default '',
    documento text not null default '',
    documento_alternativo text not null default '',
    telefone text not null default '',
    email text not null default '',
    endereco text not null default '',
    responsavel_nome text not null default '',
    observacoes text not null default '',
    consentimento_lgpd boolean not null default false,
    consentimento_lgpd_em timestamp without time zone null,
    consentimento_lgpd_canal text not null default '',
    finalidade_tratamento text not null default 'ASSISTENCIA_SAUDE',
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamp without time zone not null default now(),
    updated_at timestamp without time zone null,
    reg_date timestamp without time zone not null default now(),
    reg_update timestamp without time zone null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.pacientes
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists nome_completo text not null default '',
    add column if not exists nome text not null default '',
    add column if not exists nome_social text not null default '',
    add column if not exists data_nascimento date null,
    add column if not exists sexo text not null default '',
    add column if not exists sexo_genero text not null default '',
    add column if not exists cpf text not null default '',
    add column if not exists cns text not null default '',
    add column if not exists documento text not null default '',
    add column if not exists documento_alternativo text not null default '',
    add column if not exists telefone text not null default '',
    add column if not exists email text not null default '',
    add column if not exists endereco text not null default '',
    add column if not exists responsavel_nome text not null default '',
    add column if not exists observacoes text not null default '',
    add column if not exists consentimento_lgpd boolean not null default false,
    add column if not exists consentimento_lgpd_em timestamp without time zone null,
    add column if not exists consentimento_lgpd_canal text not null default '',
    add column if not exists finalidade_tratamento text not null default 'ASSISTENCIA_SAUDE',
    add column if not exists status text not null default 'ATIVO',
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists created_at timestamp without time zone not null default now(),
    add column if not exists updated_at timestamp without time zone null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_update timestamp without time zone null,
    add column if not exists reg_status char(1) not null default 'A';

update plantaopro.pacientes set nome_completo = nome where nome_completo = '' and coalesce(nome, '') <> '';
update plantaopro.pacientes set nome = nome_completo where nome = '' and coalesce(nome_completo, '') <> '';

create table if not exists plantaopro.agendamentos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid not null,
    medico_id uuid null,
    hospital_id uuid null,
    unidade_id uuid null,
    especialidade_id uuid null,
    especialidade text not null default '',
    sala_id uuid null,
    tipo text not null default 'CONSULTA',
    data_inicio timestamp without time zone not null,
    data_fim timestamp without time zone not null,
    status text not null default 'AGENDADO',
    observacoes text not null default '',
    motivo_cancelamento text not null default '',
    valor numeric(12,2) not null default 0,
    created_by uuid null,
    updated_by uuid null,
    created_at timestamp without time zone not null default now(),
    updated_at timestamp without time zone null,
    reg_date timestamp without time zone not null default now(),
    reg_update timestamp without time zone null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.agendamentos
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists medico_id uuid null,
    add column if not exists hospital_id uuid null,
    add column if not exists unidade_id uuid null,
    add column if not exists especialidade_id uuid null,
    add column if not exists especialidade text not null default '',
    add column if not exists sala_id uuid null,
    add column if not exists tipo text not null default 'CONSULTA',
    add column if not exists data_inicio timestamp without time zone null,
    add column if not exists data_fim timestamp without time zone null,
    add column if not exists status text not null default 'AGENDADO',
    add column if not exists observacoes text not null default '',
    add column if not exists motivo_cancelamento text not null default '',
    add column if not exists valor numeric(12,2) not null default 0,
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists created_at timestamp without time zone not null default now(),
    add column if not exists updated_at timestamp without time zone null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_update timestamp without time zone null,
    add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.painel_chamada_fila (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    painel_id uuid null,
    agendamento_id uuid null,
    paciente_id uuid null,
    triagem_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    senha text not null default '',
    paciente_nome text not null default '',
    setor text not null default '',
    sala text not null default '',
    guiche text not null default '',
    status text not null default 'AGUARDANDO',
    prioridade integer not null default 0,
    chamado_em timestamp without time zone null,
    finalizado_em timestamp without time zone null,
    created_by uuid null,
    updated_by uuid null,
    created_at timestamp without time zone not null default now(),
    updated_at timestamp without time zone null,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.painel_chamada_fila
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists painel_id uuid null,
    add column if not exists agendamento_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists triagem_id uuid null,
    add column if not exists setor_id uuid null,
    add column if not exists sala_id uuid null,
    add column if not exists guiche_id uuid null,
    add column if not exists senha text not null default '',
    add column if not exists paciente_nome text not null default '',
    add column if not exists setor text not null default '',
    add column if not exists sala text not null default '',
    add column if not exists guiche text not null default '',
    add column if not exists status text not null default 'AGUARDANDO',
    add column if not exists prioridade integer not null default 0,
    add column if not exists chamado_em timestamp without time zone null,
    add column if not exists finalizado_em timestamp without time zone null,
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists created_at timestamp without time zone not null default now(),
    add column if not exists updated_at timestamp without time zone null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.painel_chamada_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    fila_id uuid null,
    agendamento_id uuid null,
    paciente_id uuid null,
    acao text not null default '',
    detalhe text not null default '',
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_at timestamp without time zone not null default now(),
    updated_at timestamp without time zone null,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.painel_chamada_historico
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists fila_id uuid null,
    add column if not exists agendamento_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists acao text not null default '',
    add column if not exists detalhe text not null default '',
    add column if not exists detalhes jsonb not null default '{}'::jsonb,
    add column if not exists usuario_id uuid null,
    add column if not exists status text not null default 'REGISTRADO',
    add column if not exists created_at timestamp without time zone not null default now(),
    add column if not exists updated_at timestamp without time zone null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.triagens (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid not null,
    agendamento_id uuid null,
    medico_id uuid null,
    profissional_id uuid null,
    enfermeiro_id uuid null,
    queixa_principal text not null default '',
    classificacao_risco text not null default 'NAO_URGENTE',
    alergias_relatadas text not null default '',
    medicamentos_uso text not null default '',
    status text not null default 'AGUARDANDO',
    observacoes text not null default '',
    motivo_cancelamento text not null default '',
    iniciada_em timestamp without time zone null,
    finalizada_em timestamp without time zone null,
    created_by uuid null,
    updated_by uuid null,
    created_at timestamp without time zone not null default now(),
    updated_at timestamp without time zone null,
    reg_date timestamp without time zone not null default now(),
    reg_update timestamp without time zone null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.triagens
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists agendamento_id uuid null,
    add column if not exists medico_id uuid null,
    add column if not exists profissional_id uuid null,
    add column if not exists enfermeiro_id uuid null,
    add column if not exists queixa_principal text not null default '',
    add column if not exists classificacao_risco text not null default 'NAO_URGENTE',
    add column if not exists alergias_relatadas text not null default '',
    add column if not exists medicamentos_uso text not null default '',
    add column if not exists status text not null default 'AGUARDANDO',
    add column if not exists observacoes text not null default '',
    add column if not exists motivo_cancelamento text not null default '',
    add column if not exists iniciada_em timestamp without time zone null,
    add column if not exists finalizada_em timestamp without time zone null,
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists created_at timestamp without time zone not null default now(),
    add column if not exists updated_at timestamp without time zone null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_update timestamp without time zone null,
    add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.triagem_sinais_vitais (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    triagem_id uuid not null,
    paciente_id uuid not null,
    pressao_arterial text not null default '',
    frequencia_cardiaca numeric(10,2) null,
    frequencia_respiratoria numeric(10,2) null,
    temperatura numeric(10,2) null,
    saturacao numeric(10,2) null,
    peso numeric(10,2) null,
    altura numeric(10,2) null,
    imc numeric(10,2) null,
    glicemia numeric(10,2) null,
    alergias text not null default '',
    medicamentos_uso text not null default '',
    observacoes text not null default '',
    status text not null default 'ATIVO',
    created_by uuid null,
    created_at timestamp without time zone not null default now(),
    updated_at timestamp without time zone null,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.triagem_sinais_vitais
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists triagem_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists pressao_arterial text not null default '',
    add column if not exists frequencia_cardiaca numeric(10,2) null,
    add column if not exists frequencia_respiratoria numeric(10,2) null,
    add column if not exists temperatura numeric(10,2) null,
    add column if not exists saturacao numeric(10,2) null,
    add column if not exists peso numeric(10,2) null,
    add column if not exists altura numeric(10,2) null,
    add column if not exists imc numeric(10,2) null,
    add column if not exists glicemia numeric(10,2) null,
    add column if not exists alergias text not null default '',
    add column if not exists medicamentos_uso text not null default '',
    add column if not exists observacoes text not null default '',
    add column if not exists status text not null default 'ATIVO',
    add column if not exists created_by uuid null,
    add column if not exists created_at timestamp without time zone not null default now(),
    add column if not exists updated_at timestamp without time zone null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.triagem_classificacoes_risco (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo text not null,
    nome text not null,
    cor text not null default '',
    prioridade integer not null default 0,
    tempo_alvo_minutos integer null,
    status text not null default 'ATIVO',
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.triagem_classificacoes_risco
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists codigo text not null default '',
    add column if not exists nome text not null default '',
    add column if not exists cor text not null default '',
    add column if not exists prioridade integer not null default 0,
    add column if not exists tempo_alvo_minutos integer null,
    add column if not exists status text not null default 'ATIVO',
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.triagem_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    triagem_id uuid not null,
    paciente_id uuid null,
    acao text not null default '',
    detalhe text not null default '',
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_at timestamp without time zone not null default now(),
    updated_at timestamp without time zone null,
    reg_date timestamp without time zone not null default now(),
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.triagem_historico
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists triagem_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists acao text not null default '',
    add column if not exists detalhe text not null default '',
    add column if not exists detalhes jsonb not null default '{}'::jsonb,
    add column if not exists usuario_id uuid null,
    add column if not exists status text not null default 'REGISTRADO',
    add column if not exists created_at timestamp without time zone not null default now(),
    add column if not exists updated_at timestamp without time zone null,
    add column if not exists reg_date timestamp without time zone not null default now(),
    add column if not exists reg_status char(1) not null default 'A';

create index if not exists ix_pacientes_tenant_id on plantaopro.pacientes(tenant_id);
create index if not exists ix_pacientes_cliente_id on plantaopro.pacientes(cliente_id);
create index if not exists ix_pacientes_cpf on plantaopro.pacientes(cpf);
create index if not exists ix_pacientes_nome_completo on plantaopro.pacientes(nome_completo);
create index if not exists ix_pacientes_nome on plantaopro.pacientes(nome);
create index if not exists ix_pacientes_status on plantaopro.pacientes(status);
create index if not exists ix_pacientes_reg_date on plantaopro.pacientes(reg_date desc);
create unique index if not exists ux_pacientes_cliente_cpf on plantaopro.pacientes(cliente_id, cpf) where cpf <> '' and reg_status = 'A';

create index if not exists ix_agendamentos_cliente_id on plantaopro.agendamentos(cliente_id);
create index if not exists ix_agendamentos_paciente_id on plantaopro.agendamentos(paciente_id);
create index if not exists ix_agendamentos_medico_id on plantaopro.agendamentos(medico_id);
create index if not exists ix_agendamentos_hospital_id on plantaopro.agendamentos(hospital_id);
create index if not exists ix_agendamentos_especialidade_id on plantaopro.agendamentos(especialidade_id);
create index if not exists ix_agendamentos_data_inicio on plantaopro.agendamentos(data_inicio);
create index if not exists ix_agendamentos_status on plantaopro.agendamentos(status);
create index if not exists ix_agendamentos_reg_date on plantaopro.agendamentos(reg_date desc);

create index if not exists ix_painel_chamada_fila_cliente_id on plantaopro.painel_chamada_fila(cliente_id);
create index if not exists ix_painel_chamada_fila_agendamento_id on plantaopro.painel_chamada_fila(agendamento_id);
create index if not exists ix_painel_chamada_fila_paciente_id on plantaopro.painel_chamada_fila(paciente_id);
create index if not exists ix_painel_chamada_fila_status on plantaopro.painel_chamada_fila(status);
create index if not exists ix_painel_chamada_fila_prioridade on plantaopro.painel_chamada_fila(prioridade);
create index if not exists ix_painel_chamada_fila_reg_date on plantaopro.painel_chamada_fila(reg_date desc);

create index if not exists ix_painel_chamada_historico_cliente_id on plantaopro.painel_chamada_historico(cliente_id);
create index if not exists ix_painel_chamada_historico_fila_id on plantaopro.painel_chamada_historico(fila_id);
create index if not exists ix_painel_chamada_historico_paciente_id on plantaopro.painel_chamada_historico(paciente_id);
create index if not exists ix_painel_chamada_historico_acao on plantaopro.painel_chamada_historico(acao);
create index if not exists ix_painel_chamada_historico_reg_date on plantaopro.painel_chamada_historico(reg_date desc);

create index if not exists ix_triagens_cliente_id on plantaopro.triagens(cliente_id);
create index if not exists ix_triagens_paciente_id on plantaopro.triagens(paciente_id);
create index if not exists ix_triagens_agendamento_id on plantaopro.triagens(agendamento_id);
create index if not exists ix_triagens_classificacao_risco on plantaopro.triagens(classificacao_risco);
create index if not exists ix_triagens_status on plantaopro.triagens(status);
create index if not exists ix_triagens_reg_date on plantaopro.triagens(reg_date desc);

create index if not exists ix_triagem_sinais_vitais_cliente_id on plantaopro.triagem_sinais_vitais(cliente_id);
create index if not exists ix_triagem_sinais_vitais_triagem_id on plantaopro.triagem_sinais_vitais(triagem_id);
create index if not exists ix_triagem_sinais_vitais_paciente_id on plantaopro.triagem_sinais_vitais(paciente_id);
create index if not exists ix_triagem_sinais_vitais_reg_date on plantaopro.triagem_sinais_vitais(reg_date desc);

create index if not exists ix_triagem_classificacoes_risco_codigo on plantaopro.triagem_classificacoes_risco(codigo);
create index if not exists ix_triagem_classificacoes_risco_prioridade on plantaopro.triagem_classificacoes_risco(prioridade);

create index if not exists ix_triagem_historico_cliente_id on plantaopro.triagem_historico(cliente_id);
create index if not exists ix_triagem_historico_triagem_id on plantaopro.triagem_historico(triagem_id);
create index if not exists ix_triagem_historico_paciente_id on plantaopro.triagem_historico(paciente_id);
create index if not exists ix_triagem_historico_acao on plantaopro.triagem_historico(acao);
create index if not exists ix_triagem_historico_reg_date on plantaopro.triagem_historico(reg_date desc);

do $$
begin
    if not exists (
        select 1 from pg_constraint
        where conname = 'ck_agendamentos_data_fim_maior_inicio'
          and conrelid = 'plantaopro.agendamentos'::regclass
    ) then
        alter table plantaopro.agendamentos
        add constraint ck_agendamentos_data_fim_maior_inicio
        check (data_fim is null or data_inicio is null or data_fim > data_inicio);
    end if;
end $$;

insert into plantaopro.triagem_classificacoes_risco (codigo, nome, cor, prioridade, tempo_alvo_minutos)
select 'EMERGENCIA', 'Emergência', 'VERMELHO', 1, 0
where not exists (select 1 from plantaopro.triagem_classificacoes_risco where codigo = 'EMERGENCIA' and cliente_id is null);

insert into plantaopro.triagem_classificacoes_risco (codigo, nome, cor, prioridade, tempo_alvo_minutos)
select 'MUITO_URGENTE', 'Muito urgente', 'LARANJA', 2, 10
where not exists (select 1 from plantaopro.triagem_classificacoes_risco where codigo = 'MUITO_URGENTE' and cliente_id is null);

insert into plantaopro.triagem_classificacoes_risco (codigo, nome, cor, prioridade, tempo_alvo_minutos)
select 'URGENTE', 'Urgente', 'AMARELO', 3, 60
where not exists (select 1 from plantaopro.triagem_classificacoes_risco where codigo = 'URGENTE' and cliente_id is null);

insert into plantaopro.triagem_classificacoes_risco (codigo, nome, cor, prioridade, tempo_alvo_minutos)
select 'POUCO_URGENTE', 'Pouco urgente', 'VERDE', 4, 120
where not exists (select 1 from plantaopro.triagem_classificacoes_risco where codigo = 'POUCO_URGENTE' and cliente_id is null);

insert into plantaopro.triagem_classificacoes_risco (codigo, nome, cor, prioridade, tempo_alvo_minutos)
select 'NAO_URGENTE', 'Não urgente', 'AZUL', 5, 240
where not exists (select 1 from plantaopro.triagem_classificacoes_risco where codigo = 'NAO_URGENTE' and cliente_id is null);
