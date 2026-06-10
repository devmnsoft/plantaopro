create schema if not exists plantaopro;
create extension if not exists pgcrypto;

create table if not exists plantaopro.pacientes (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    nome text not null,
    data_nascimento date null,
    cpf text null,
    email text null,
    telefone text null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_pacientes_cliente_id on plantaopro.pacientes (cliente_id);
create index if not exists ix_pacientes_status on plantaopro.pacientes (status);
create index if not exists ix_pacientes_reg_date on plantaopro.pacientes (reg_date);
create index if not exists ix_pacientes_reg_status on plantaopro.pacientes (reg_status);

create table if not exists plantaopro.paciente_contatos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    paciente_id uuid not null,
    nome text not null,
    tipo text not null default 'CONTATO',
    telefone text null,
    email text null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_paciente_contatos_cliente_id on plantaopro.paciente_contatos (cliente_id);
create index if not exists ix_paciente_contatos_paciente_id on plantaopro.paciente_contatos (paciente_id);
create index if not exists ix_paciente_contatos_status on plantaopro.paciente_contatos (status);
create index if not exists ix_paciente_contatos_reg_date on plantaopro.paciente_contatos (reg_date);
create index if not exists ix_paciente_contatos_reg_status on plantaopro.paciente_contatos (reg_status);

create table if not exists plantaopro.paciente_enderecos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    paciente_id uuid not null,
    logradouro text null,
    numero text null,
    bairro text null,
    cidade text null,
    estado text null,
    cep text null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_paciente_enderecos_cliente_id on plantaopro.paciente_enderecos (cliente_id);
create index if not exists ix_paciente_enderecos_paciente_id on plantaopro.paciente_enderecos (paciente_id);
create index if not exists ix_paciente_enderecos_status on plantaopro.paciente_enderecos (status);
create index if not exists ix_paciente_enderecos_reg_date on plantaopro.paciente_enderecos (reg_date);
create index if not exists ix_paciente_enderecos_reg_status on plantaopro.paciente_enderecos (reg_status);

create table if not exists plantaopro.paciente_documentos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    paciente_id uuid not null,
    tipo text not null,
    numero text not null,
    emissor text null,
    validade date null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_paciente_documentos_cliente_id on plantaopro.paciente_documentos (cliente_id);
create index if not exists ix_paciente_documentos_paciente_id on plantaopro.paciente_documentos (paciente_id);
create index if not exists ix_paciente_documentos_status on plantaopro.paciente_documentos (status);
create index if not exists ix_paciente_documentos_reg_date on plantaopro.paciente_documentos (reg_date);
create index if not exists ix_paciente_documentos_reg_status on plantaopro.paciente_documentos (reg_status);

create table if not exists plantaopro.paciente_convenios (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    paciente_id uuid not null,
    convenio_id uuid null,
    plano_saude_id uuid null,
    numero_carteirinha text null,
    principal boolean not null default false,
    validade date null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_paciente_convenios_cliente_id on plantaopro.paciente_convenios (cliente_id);
create index if not exists ix_paciente_convenios_paciente_id on plantaopro.paciente_convenios (paciente_id);
create index if not exists ix_paciente_convenios_status on plantaopro.paciente_convenios (status);
create index if not exists ix_paciente_convenios_reg_date on plantaopro.paciente_convenios (reg_date);
create index if not exists ix_paciente_convenios_reg_status on plantaopro.paciente_convenios (reg_status);

create table if not exists plantaopro.paciente_historico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    paciente_id uuid not null,
    acao text not null,
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_paciente_historico_cliente_id on plantaopro.paciente_historico (cliente_id);
create index if not exists ix_paciente_historico_paciente_id on plantaopro.paciente_historico (paciente_id);
create index if not exists ix_paciente_historico_status on plantaopro.paciente_historico (status);
create index if not exists ix_paciente_historico_reg_date on plantaopro.paciente_historico (reg_date);
create index if not exists ix_paciente_historico_reg_status on plantaopro.paciente_historico (reg_status);

create table if not exists plantaopro.paineis_chamada (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    nome text not null,
    unidade_id uuid null,
    token_publico text null,
    permite_tv_publica boolean not null default false,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_paineis_chamada_cliente_id on plantaopro.paineis_chamada (cliente_id);
create index if not exists ix_paineis_chamada_status on plantaopro.paineis_chamada (status);
create index if not exists ix_paineis_chamada_reg_date on plantaopro.paineis_chamada (reg_date);
create index if not exists ix_paineis_chamada_reg_status on plantaopro.paineis_chamada (reg_status);

create table if not exists plantaopro.painel_chamada_configuracoes (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    painel_id uuid not null,
    tema text not null default 'WHITE_LABEL',
    exibir_nome_completo boolean not null default false,
    tempo_exibicao_segundos int not null default 20,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_painel_chamada_configuracoes_cliente_id on plantaopro.painel_chamada_configuracoes (cliente_id);
create index if not exists ix_painel_chamada_configuracoes_status on plantaopro.painel_chamada_configuracoes (status);
create index if not exists ix_painel_chamada_configuracoes_reg_date on plantaopro.painel_chamada_configuracoes (reg_date);
create index if not exists ix_painel_chamada_configuracoes_reg_status on plantaopro.painel_chamada_configuracoes (reg_status);

create table if not exists plantaopro.painel_chamada_setores (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    painel_id uuid null,
    nome text not null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_painel_chamada_setores_cliente_id on plantaopro.painel_chamada_setores (cliente_id);
create index if not exists ix_painel_chamada_setores_status on plantaopro.painel_chamada_setores (status);
create index if not exists ix_painel_chamada_setores_reg_date on plantaopro.painel_chamada_setores (reg_date);
create index if not exists ix_painel_chamada_setores_reg_status on plantaopro.painel_chamada_setores (reg_status);

create table if not exists plantaopro.painel_chamada_salas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    setor_id uuid null,
    nome text not null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_painel_chamada_salas_cliente_id on plantaopro.painel_chamada_salas (cliente_id);
create index if not exists ix_painel_chamada_salas_status on plantaopro.painel_chamada_salas (status);
create index if not exists ix_painel_chamada_salas_reg_date on plantaopro.painel_chamada_salas (reg_date);
create index if not exists ix_painel_chamada_salas_reg_status on plantaopro.painel_chamada_salas (reg_status);

create table if not exists plantaopro.painel_chamada_guiches (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    setor_id uuid null,
    nome text not null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_painel_chamada_guiches_cliente_id on plantaopro.painel_chamada_guiches (cliente_id);
create index if not exists ix_painel_chamada_guiches_status on plantaopro.painel_chamada_guiches (status);
create index if not exists ix_painel_chamada_guiches_reg_date on plantaopro.painel_chamada_guiches (reg_date);
create index if not exists ix_painel_chamada_guiches_reg_status on plantaopro.painel_chamada_guiches (reg_status);

create table if not exists plantaopro.painel_chamada_fila (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    painel_id uuid null,
    paciente_id uuid null,
    agendamento_id uuid null,
    setor_id uuid null,
    sala_id uuid null,
    guiche_id uuid null,
    senha text null,
    paciente_nome text null,
    prioridade int not null default 0,
    status text not null default 'AGUARDANDO',
    chamado_em timestamptz null,
    finalizado_em timestamptz null,
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_painel_chamada_fila_cliente_id on plantaopro.painel_chamada_fila (cliente_id);
create index if not exists ix_painel_chamada_fila_paciente_id on plantaopro.painel_chamada_fila (paciente_id);
create index if not exists ix_painel_chamada_fila_agendamento_id on plantaopro.painel_chamada_fila (agendamento_id);
create index if not exists ix_painel_chamada_fila_status on plantaopro.painel_chamada_fila (status);
create index if not exists ix_painel_chamada_fila_reg_date on plantaopro.painel_chamada_fila (reg_date);
create index if not exists ix_painel_chamada_fila_reg_status on plantaopro.painel_chamada_fila (reg_status);

create table if not exists plantaopro.painel_chamada_historico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    painel_id uuid null,
    fila_id uuid null,
    paciente_id uuid null,
    acao text not null,
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_painel_chamada_historico_cliente_id on plantaopro.painel_chamada_historico (cliente_id);
create index if not exists ix_painel_chamada_historico_paciente_id on plantaopro.painel_chamada_historico (paciente_id);
create index if not exists ix_painel_chamada_historico_status on plantaopro.painel_chamada_historico (status);
create index if not exists ix_painel_chamada_historico_reg_date on plantaopro.painel_chamada_historico (reg_date);
create index if not exists ix_painel_chamada_historico_reg_status on plantaopro.painel_chamada_historico (reg_status);

create table if not exists plantaopro.agendamentos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    paciente_id uuid not null,
    medico_id uuid null,
    unidade_id uuid null,
    especialidade_id uuid null,
    data_inicio timestamptz not null,
    data_fim timestamptz not null,
    tipo text not null default 'CONSULTA',
    status text not null default 'AGENDADO',
    observacoes text null,
    valor numeric(12,2) not null default 0,
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_agendamentos_cliente_id on plantaopro.agendamentos (cliente_id);
create index if not exists ix_agendamentos_paciente_id on plantaopro.agendamentos (paciente_id);
create index if not exists ix_agendamentos_medico_id on plantaopro.agendamentos (medico_id);
create index if not exists ix_agendamentos_status on plantaopro.agendamentos (status);
create index if not exists ix_agendamentos_reg_date on plantaopro.agendamentos (reg_date);
create index if not exists ix_agendamentos_reg_status on plantaopro.agendamentos (reg_status);

create table if not exists plantaopro.agendamento_historico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    agendamento_id uuid not null,
    acao text not null,
    status_anterior text null,
    status_novo text null,
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_agendamento_historico_cliente_id on plantaopro.agendamento_historico (cliente_id);
create index if not exists ix_agendamento_historico_agendamento_id on plantaopro.agendamento_historico (agendamento_id);
create index if not exists ix_agendamento_historico_status on plantaopro.agendamento_historico (status);
create index if not exists ix_agendamento_historico_reg_date on plantaopro.agendamento_historico (reg_date);
create index if not exists ix_agendamento_historico_reg_status on plantaopro.agendamento_historico (reg_status);

create table if not exists plantaopro.agendamento_bloqueios (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    medico_id uuid null,
    unidade_id uuid null,
    data_inicio timestamptz not null,
    data_fim timestamptz not null,
    motivo text not null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_agendamento_bloqueios_cliente_id on plantaopro.agendamento_bloqueios (cliente_id);
create index if not exists ix_agendamento_bloqueios_medico_id on plantaopro.agendamento_bloqueios (medico_id);
create index if not exists ix_agendamento_bloqueios_status on plantaopro.agendamento_bloqueios (status);
create index if not exists ix_agendamento_bloqueios_reg_date on plantaopro.agendamento_bloqueios (reg_date);
create index if not exists ix_agendamento_bloqueios_reg_status on plantaopro.agendamento_bloqueios (reg_status);

create table if not exists plantaopro.agendamento_cancelamentos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    agendamento_id uuid not null,
    motivo text not null,
    usuario_id uuid null,
    status text not null default 'CANCELADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_agendamento_cancelamentos_cliente_id on plantaopro.agendamento_cancelamentos (cliente_id);
create index if not exists ix_agendamento_cancelamentos_agendamento_id on plantaopro.agendamento_cancelamentos (agendamento_id);
create index if not exists ix_agendamento_cancelamentos_status on plantaopro.agendamento_cancelamentos (status);
create index if not exists ix_agendamento_cancelamentos_reg_date on plantaopro.agendamento_cancelamentos (reg_date);
create index if not exists ix_agendamento_cancelamentos_reg_status on plantaopro.agendamento_cancelamentos (reg_status);

create table if not exists plantaopro.agendamento_checkins (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    agendamento_id uuid not null,
    paciente_id uuid null,
    realizado_em timestamptz not null default now(),
    origem text not null default 'RECEPCAO',
    status text not null default 'REALIZADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_agendamento_checkins_cliente_id on plantaopro.agendamento_checkins (cliente_id);
create index if not exists ix_agendamento_checkins_paciente_id on plantaopro.agendamento_checkins (paciente_id);
create index if not exists ix_agendamento_checkins_agendamento_id on plantaopro.agendamento_checkins (agendamento_id);
create index if not exists ix_agendamento_checkins_status on plantaopro.agendamento_checkins (status);
create index if not exists ix_agendamento_checkins_reg_date on plantaopro.agendamento_checkins (reg_date);
create index if not exists ix_agendamento_checkins_reg_status on plantaopro.agendamento_checkins (reg_status);

create table if not exists plantaopro.triagens (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    paciente_id uuid not null,
    agendamento_id uuid null,
    enfermeiro_id uuid null,
    classificacao_risco text null,
    queixa_principal text null,
    status text not null default 'ABERTA',
    finalizada_em timestamptz null,
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_triagens_cliente_id on plantaopro.triagens (cliente_id);
create index if not exists ix_triagens_paciente_id on plantaopro.triagens (paciente_id);
create index if not exists ix_triagens_agendamento_id on plantaopro.triagens (agendamento_id);
create index if not exists ix_triagens_status on plantaopro.triagens (status);
create index if not exists ix_triagens_reg_date on plantaopro.triagens (reg_date);
create index if not exists ix_triagens_reg_status on plantaopro.triagens (reg_status);

create table if not exists plantaopro.triagem_sinais_vitais (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    triagem_id uuid not null,
    pressao_arterial text null,
    frequencia_cardiaca int null,
    frequencia_respiratoria int null,
    temperatura numeric(4,1) null,
    saturacao int null,
    glicemia int null,
    dor_escala int null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_triagem_sinais_vitais_cliente_id on plantaopro.triagem_sinais_vitais (cliente_id);
create index if not exists ix_triagem_sinais_vitais_status on plantaopro.triagem_sinais_vitais (status);
create index if not exists ix_triagem_sinais_vitais_reg_date on plantaopro.triagem_sinais_vitais (reg_date);
create index if not exists ix_triagem_sinais_vitais_reg_status on plantaopro.triagem_sinais_vitais (reg_status);

create table if not exists plantaopro.triagem_classificacoes_risco (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    nome text not null,
    cor text not null,
    prioridade int not null,
    tempo_alvo_minutos int not null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_triagem_classificacoes_risco_cliente_id on plantaopro.triagem_classificacoes_risco (cliente_id);
create index if not exists ix_triagem_classificacoes_risco_status on plantaopro.triagem_classificacoes_risco (status);
create index if not exists ix_triagem_classificacoes_risco_reg_date on plantaopro.triagem_classificacoes_risco (reg_date);
create index if not exists ix_triagem_classificacoes_risco_reg_status on plantaopro.triagem_classificacoes_risco (reg_status);

create table if not exists plantaopro.triagem_historico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    triagem_id uuid not null,
    paciente_id uuid null,
    acao text not null,
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_triagem_historico_cliente_id on plantaopro.triagem_historico (cliente_id);
create index if not exists ix_triagem_historico_paciente_id on plantaopro.triagem_historico (paciente_id);
create index if not exists ix_triagem_historico_status on plantaopro.triagem_historico (status);
create index if not exists ix_triagem_historico_reg_date on plantaopro.triagem_historico (reg_date);
create index if not exists ix_triagem_historico_reg_status on plantaopro.triagem_historico (reg_status);

create table if not exists plantaopro.consultas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    paciente_id uuid not null,
    agendamento_id uuid null,
    triagem_id uuid null,
    medico_id uuid not null,
    data_inicio timestamptz null,
    data_fim timestamptz null,
    status text not null default 'ABERTA',
    motivo_cancelamento text null,
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_consultas_cliente_id on plantaopro.consultas (cliente_id);
create index if not exists ix_consultas_paciente_id on plantaopro.consultas (paciente_id);
create index if not exists ix_consultas_medico_id on plantaopro.consultas (medico_id);
create index if not exists ix_consultas_agendamento_id on plantaopro.consultas (agendamento_id);
create index if not exists ix_consultas_status on plantaopro.consultas (status);
create index if not exists ix_consultas_reg_date on plantaopro.consultas (reg_date);
create index if not exists ix_consultas_reg_status on plantaopro.consultas (reg_status);

create table if not exists plantaopro.consulta_anamnese (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    consulta_id uuid not null,
    queixa_principal text null,
    historia_doenca_atual text null,
    antecedentes text null,
    alergias text null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_consulta_anamnese_cliente_id on plantaopro.consulta_anamnese (cliente_id);
create index if not exists ix_consulta_anamnese_consulta_id on plantaopro.consulta_anamnese (consulta_id);
create index if not exists ix_consulta_anamnese_status on plantaopro.consulta_anamnese (status);
create index if not exists ix_consulta_anamnese_reg_date on plantaopro.consulta_anamnese (reg_date);
create index if not exists ix_consulta_anamnese_reg_status on plantaopro.consulta_anamnese (reg_status);

create table if not exists plantaopro.consulta_exame_fisico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    consulta_id uuid not null,
    descricao text null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_consulta_exame_fisico_cliente_id on plantaopro.consulta_exame_fisico (cliente_id);
create index if not exists ix_consulta_exame_fisico_consulta_id on plantaopro.consulta_exame_fisico (consulta_id);
create index if not exists ix_consulta_exame_fisico_status on plantaopro.consulta_exame_fisico (status);
create index if not exists ix_consulta_exame_fisico_reg_date on plantaopro.consulta_exame_fisico (reg_date);
create index if not exists ix_consulta_exame_fisico_reg_status on plantaopro.consulta_exame_fisico (reg_status);

create table if not exists plantaopro.consulta_diagnosticos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    consulta_id uuid not null,
    cid_id uuid null,
    codigo_cid text null,
    descricao text null,
    principal boolean not null default false,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_consulta_diagnosticos_cliente_id on plantaopro.consulta_diagnosticos (cliente_id);
create index if not exists ix_consulta_diagnosticos_consulta_id on plantaopro.consulta_diagnosticos (consulta_id);
create index if not exists ix_consulta_diagnosticos_status on plantaopro.consulta_diagnosticos (status);
create index if not exists ix_consulta_diagnosticos_reg_date on plantaopro.consulta_diagnosticos (reg_date);
create index if not exists ix_consulta_diagnosticos_reg_status on plantaopro.consulta_diagnosticos (reg_status);

create table if not exists plantaopro.consulta_condutas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    consulta_id uuid not null,
    descricao text not null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_consulta_condutas_cliente_id on plantaopro.consulta_condutas (cliente_id);
create index if not exists ix_consulta_condutas_consulta_id on plantaopro.consulta_condutas (consulta_id);
create index if not exists ix_consulta_condutas_status on plantaopro.consulta_condutas (status);
create index if not exists ix_consulta_condutas_reg_date on plantaopro.consulta_condutas (reg_date);
create index if not exists ix_consulta_condutas_reg_status on plantaopro.consulta_condutas (reg_status);

create table if not exists plantaopro.consulta_encaminhamentos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    consulta_id uuid not null,
    destino text not null,
    motivo text null,
    status text not null default 'ABERTO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_consulta_encaminhamentos_cliente_id on plantaopro.consulta_encaminhamentos (cliente_id);
create index if not exists ix_consulta_encaminhamentos_consulta_id on plantaopro.consulta_encaminhamentos (consulta_id);
create index if not exists ix_consulta_encaminhamentos_status on plantaopro.consulta_encaminhamentos (status);
create index if not exists ix_consulta_encaminhamentos_reg_date on plantaopro.consulta_encaminhamentos (reg_date);
create index if not exists ix_consulta_encaminhamentos_reg_status on plantaopro.consulta_encaminhamentos (reg_status);

create table if not exists plantaopro.consulta_historico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    consulta_id uuid not null,
    paciente_id uuid null,
    acao text not null,
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_consulta_historico_cliente_id on plantaopro.consulta_historico (cliente_id);
create index if not exists ix_consulta_historico_paciente_id on plantaopro.consulta_historico (paciente_id);
create index if not exists ix_consulta_historico_consulta_id on plantaopro.consulta_historico (consulta_id);
create index if not exists ix_consulta_historico_status on plantaopro.consulta_historico (status);
create index if not exists ix_consulta_historico_reg_date on plantaopro.consulta_historico (reg_date);
create index if not exists ix_consulta_historico_reg_status on plantaopro.consulta_historico (reg_status);

create table if not exists plantaopro.cid_tabela (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    codigo text not null,
    descricao text not null,
    grupo text null,
    ativo boolean not null default true,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_cid_tabela_cliente_id on plantaopro.cid_tabela (cliente_id);
create index if not exists ix_cid_tabela_status on plantaopro.cid_tabela (status);
create index if not exists ix_cid_tabela_reg_date on plantaopro.cid_tabela (reg_date);
create index if not exists ix_cid_tabela_reg_status on plantaopro.cid_tabela (reg_status);

create table if not exists plantaopro.cid_favoritos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    cid_id uuid not null,
    medico_id uuid null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_cid_favoritos_cliente_id on plantaopro.cid_favoritos (cliente_id);
create index if not exists ix_cid_favoritos_medico_id on plantaopro.cid_favoritos (medico_id);
create index if not exists ix_cid_favoritos_status on plantaopro.cid_favoritos (status);
create index if not exists ix_cid_favoritos_reg_date on plantaopro.cid_favoritos (reg_date);
create index if not exists ix_cid_favoritos_reg_status on plantaopro.cid_favoritos (reg_status);

create table if not exists plantaopro.cid_uso_historico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    cid_id uuid null,
    consulta_id uuid null,
    medico_id uuid null,
    paciente_id uuid null,
    acao text not null default 'USO',
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_cid_uso_historico_cliente_id on plantaopro.cid_uso_historico (cliente_id);
create index if not exists ix_cid_uso_historico_paciente_id on plantaopro.cid_uso_historico (paciente_id);
create index if not exists ix_cid_uso_historico_medico_id on plantaopro.cid_uso_historico (medico_id);
create index if not exists ix_cid_uso_historico_consulta_id on plantaopro.cid_uso_historico (consulta_id);
create index if not exists ix_cid_uso_historico_status on plantaopro.cid_uso_historico (status);
create index if not exists ix_cid_uso_historico_reg_date on plantaopro.cid_uso_historico (reg_date);
create index if not exists ix_cid_uso_historico_reg_status on plantaopro.cid_uso_historico (reg_status);

create table if not exists plantaopro.prescricoes (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    paciente_id uuid not null,
    consulta_id uuid null,
    medico_id uuid not null,
    modelo_id uuid null,
    status text not null default 'RASCUNHO',
    finalizada_em timestamptz null,
    cancelada_em timestamptz null,
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_prescricoes_cliente_id on plantaopro.prescricoes (cliente_id);
create index if not exists ix_prescricoes_paciente_id on plantaopro.prescricoes (paciente_id);
create index if not exists ix_prescricoes_medico_id on plantaopro.prescricoes (medico_id);
create index if not exists ix_prescricoes_consulta_id on plantaopro.prescricoes (consulta_id);
create index if not exists ix_prescricoes_status on plantaopro.prescricoes (status);
create index if not exists ix_prescricoes_reg_date on plantaopro.prescricoes (reg_date);
create index if not exists ix_prescricoes_reg_status on plantaopro.prescricoes (reg_status);

create table if not exists plantaopro.prescricao_itens (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    prescricao_id uuid not null,
    medicamento text not null,
    posologia text not null,
    quantidade text null,
    duracao text null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_prescricao_itens_cliente_id on plantaopro.prescricao_itens (cliente_id);
create index if not exists ix_prescricao_itens_status on plantaopro.prescricao_itens (status);
create index if not exists ix_prescricao_itens_reg_date on plantaopro.prescricao_itens (reg_date);
create index if not exists ix_prescricao_itens_reg_status on plantaopro.prescricao_itens (reg_status);

create table if not exists plantaopro.prescricao_modelos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    nome text not null,
    medico_id uuid null,
    descricao text null,
    itens jsonb not null default '[]'::jsonb,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_prescricao_modelos_cliente_id on plantaopro.prescricao_modelos (cliente_id);
create index if not exists ix_prescricao_modelos_medico_id on plantaopro.prescricao_modelos (medico_id);
create index if not exists ix_prescricao_modelos_status on plantaopro.prescricao_modelos (status);
create index if not exists ix_prescricao_modelos_reg_date on plantaopro.prescricao_modelos (reg_date);
create index if not exists ix_prescricao_modelos_reg_status on plantaopro.prescricao_modelos (reg_status);

create table if not exists plantaopro.prescricao_historico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    prescricao_id uuid not null,
    acao text not null,
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_prescricao_historico_cliente_id on plantaopro.prescricao_historico (cliente_id);
create index if not exists ix_prescricao_historico_status on plantaopro.prescricao_historico (status);
create index if not exists ix_prescricao_historico_reg_date on plantaopro.prescricao_historico (reg_date);
create index if not exists ix_prescricao_historico_reg_status on plantaopro.prescricao_historico (reg_status);

create table if not exists plantaopro.prescricao_cancelamentos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    prescricao_id uuid not null,
    justificativa text not null,
    usuario_id uuid null,
    status text not null default 'CANCELADA',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_prescricao_cancelamentos_cliente_id on plantaopro.prescricao_cancelamentos (cliente_id);
create index if not exists ix_prescricao_cancelamentos_status on plantaopro.prescricao_cancelamentos (status);
create index if not exists ix_prescricao_cancelamentos_reg_date on plantaopro.prescricao_cancelamentos (reg_date);
create index if not exists ix_prescricao_cancelamentos_reg_status on plantaopro.prescricao_cancelamentos (reg_status);

create table if not exists plantaopro.clinica_contas_receber (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    paciente_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    descricao text not null,
    valor numeric(12,2) not null,
    vencimento date null,
    status text not null default 'ABERTA',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_clinica_contas_receber_cliente_id on plantaopro.clinica_contas_receber (cliente_id);
create index if not exists ix_clinica_contas_receber_paciente_id on plantaopro.clinica_contas_receber (paciente_id);
create index if not exists ix_clinica_contas_receber_agendamento_id on plantaopro.clinica_contas_receber (agendamento_id);
create index if not exists ix_clinica_contas_receber_consulta_id on plantaopro.clinica_contas_receber (consulta_id);
create index if not exists ix_clinica_contas_receber_status on plantaopro.clinica_contas_receber (status);
create index if not exists ix_clinica_contas_receber_reg_date on plantaopro.clinica_contas_receber (reg_date);
create index if not exists ix_clinica_contas_receber_reg_status on plantaopro.clinica_contas_receber (reg_status);

create table if not exists plantaopro.clinica_recebimentos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    conta_receber_id uuid null,
    valor numeric(12,2) not null,
    forma_pagamento text not null,
    recebido_em timestamptz not null default now(),
    status text not null default 'RECEBIDO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_clinica_recebimentos_cliente_id on plantaopro.clinica_recebimentos (cliente_id);
create index if not exists ix_clinica_recebimentos_status on plantaopro.clinica_recebimentos (status);
create index if not exists ix_clinica_recebimentos_reg_date on plantaopro.clinica_recebimentos (reg_date);
create index if not exists ix_clinica_recebimentos_reg_status on plantaopro.clinica_recebimentos (reg_status);

create table if not exists plantaopro.clinica_caixa (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    data_abertura timestamptz not null default now(),
    data_fechamento timestamptz null,
    saldo_inicial numeric(12,2) not null default 0,
    saldo_final numeric(12,2) null,
    status text not null default 'ABERTO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_clinica_caixa_cliente_id on plantaopro.clinica_caixa (cliente_id);
create index if not exists ix_clinica_caixa_status on plantaopro.clinica_caixa (status);
create index if not exists ix_clinica_caixa_reg_date on plantaopro.clinica_caixa (reg_date);
create index if not exists ix_clinica_caixa_reg_status on plantaopro.clinica_caixa (reg_status);

create table if not exists plantaopro.clinica_fechamentos_caixa (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    caixa_id uuid not null,
    valor_informado numeric(12,2) not null,
    divergencia numeric(12,2) not null default 0,
    observacoes text null,
    status text not null default 'FECHADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_clinica_fechamentos_caixa_cliente_id on plantaopro.clinica_fechamentos_caixa (cliente_id);
create index if not exists ix_clinica_fechamentos_caixa_status on plantaopro.clinica_fechamentos_caixa (status);
create index if not exists ix_clinica_fechamentos_caixa_reg_date on plantaopro.clinica_fechamentos_caixa (reg_date);
create index if not exists ix_clinica_fechamentos_caixa_reg_status on plantaopro.clinica_fechamentos_caixa (reg_status);

create table if not exists plantaopro.clinica_repasses (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    medico_id uuid null,
    valor numeric(12,2) not null,
    competencia text null,
    status text not null default 'PENDENTE',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_clinica_repasses_cliente_id on plantaopro.clinica_repasses (cliente_id);
create index if not exists ix_clinica_repasses_medico_id on plantaopro.clinica_repasses (medico_id);
create index if not exists ix_clinica_repasses_status on plantaopro.clinica_repasses (status);
create index if not exists ix_clinica_repasses_reg_date on plantaopro.clinica_repasses (reg_date);
create index if not exists ix_clinica_repasses_reg_status on plantaopro.clinica_repasses (reg_status);

create table if not exists plantaopro.clinica_lancamentos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    caixa_id uuid null,
    tipo text not null,
    descricao text not null,
    valor numeric(12,2) not null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_clinica_lancamentos_cliente_id on plantaopro.clinica_lancamentos (cliente_id);
create index if not exists ix_clinica_lancamentos_status on plantaopro.clinica_lancamentos (status);
create index if not exists ix_clinica_lancamentos_reg_date on plantaopro.clinica_lancamentos (reg_date);
create index if not exists ix_clinica_lancamentos_reg_status on plantaopro.clinica_lancamentos (reg_status);

create table if not exists plantaopro.clinica_glosas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    convenio_id uuid null,
    faturamento_id uuid null,
    valor numeric(12,2) not null,
    motivo text not null,
    status text not null default 'ABERTA',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_clinica_glosas_cliente_id on plantaopro.clinica_glosas (cliente_id);
create index if not exists ix_clinica_glosas_status on plantaopro.clinica_glosas (status);
create index if not exists ix_clinica_glosas_reg_date on plantaopro.clinica_glosas (reg_date);
create index if not exists ix_clinica_glosas_reg_status on plantaopro.clinica_glosas (reg_status);

create table if not exists plantaopro.convenios (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    nome text not null,
    codigo_ans text null,
    cnpj text null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenios_cliente_id on plantaopro.convenios (cliente_id);
create index if not exists ix_convenios_status on plantaopro.convenios (status);
create index if not exists ix_convenios_reg_date on plantaopro.convenios (reg_date);
create index if not exists ix_convenios_reg_status on plantaopro.convenios (reg_status);

create table if not exists plantaopro.convenio_contratos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    convenio_id uuid not null,
    numero text null,
    vigencia_inicio date null,
    vigencia_fim date null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenio_contratos_cliente_id on plantaopro.convenio_contratos (cliente_id);
create index if not exists ix_convenio_contratos_status on plantaopro.convenio_contratos (status);
create index if not exists ix_convenio_contratos_reg_date on plantaopro.convenio_contratos (reg_date);
create index if not exists ix_convenio_contratos_reg_status on plantaopro.convenio_contratos (reg_status);

create table if not exists plantaopro.convenio_planos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    convenio_id uuid not null,
    nome text not null,
    registro_ans text null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenio_planos_cliente_id on plantaopro.convenio_planos (cliente_id);
create index if not exists ix_convenio_planos_status on plantaopro.convenio_planos (status);
create index if not exists ix_convenio_planos_reg_date on plantaopro.convenio_planos (reg_date);
create index if not exists ix_convenio_planos_reg_status on plantaopro.convenio_planos (reg_status);

create table if not exists plantaopro.convenio_tabelas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    convenio_id uuid not null,
    nome text not null,
    vigencia_inicio date null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenio_tabelas_cliente_id on plantaopro.convenio_tabelas (cliente_id);
create index if not exists ix_convenio_tabelas_status on plantaopro.convenio_tabelas (status);
create index if not exists ix_convenio_tabelas_reg_date on plantaopro.convenio_tabelas (reg_date);
create index if not exists ix_convenio_tabelas_reg_status on plantaopro.convenio_tabelas (reg_status);

create table if not exists plantaopro.convenio_procedimentos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    convenio_id uuid not null,
    tabela_id uuid null,
    codigo text not null,
    descricao text not null,
    valor numeric(12,2) not null default 0,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenio_procedimentos_cliente_id on plantaopro.convenio_procedimentos (cliente_id);
create index if not exists ix_convenio_procedimentos_status on plantaopro.convenio_procedimentos (status);
create index if not exists ix_convenio_procedimentos_reg_date on plantaopro.convenio_procedimentos (reg_date);
create index if not exists ix_convenio_procedimentos_reg_status on plantaopro.convenio_procedimentos (reg_status);

create table if not exists plantaopro.convenio_autorizacoes (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    convenio_id uuid null,
    paciente_id uuid null,
    agendamento_id uuid null,
    consulta_id uuid null,
    procedimento_id uuid null,
    numero_guia text null,
    motivo text null,
    status text not null default 'SOLICITADA',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenio_autorizacoes_cliente_id on plantaopro.convenio_autorizacoes (cliente_id);
create index if not exists ix_convenio_autorizacoes_paciente_id on plantaopro.convenio_autorizacoes (paciente_id);
create index if not exists ix_convenio_autorizacoes_agendamento_id on plantaopro.convenio_autorizacoes (agendamento_id);
create index if not exists ix_convenio_autorizacoes_consulta_id on plantaopro.convenio_autorizacoes (consulta_id);
create index if not exists ix_convenio_autorizacoes_status on plantaopro.convenio_autorizacoes (status);
create index if not exists ix_convenio_autorizacoes_reg_date on plantaopro.convenio_autorizacoes (reg_date);
create index if not exists ix_convenio_autorizacoes_reg_status on plantaopro.convenio_autorizacoes (reg_status);

create table if not exists plantaopro.convenio_glosas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    convenio_id uuid null,
    autorizacao_id uuid null,
    valor numeric(12,2) not null default 0,
    motivo text not null,
    status text not null default 'ABERTA',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenio_glosas_cliente_id on plantaopro.convenio_glosas (cliente_id);
create index if not exists ix_convenio_glosas_status on plantaopro.convenio_glosas (status);
create index if not exists ix_convenio_glosas_reg_date on plantaopro.convenio_glosas (reg_date);
create index if not exists ix_convenio_glosas_reg_status on plantaopro.convenio_glosas (reg_status);

create table if not exists plantaopro.convenio_faturamentos (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    convenio_id uuid not null,
    competencia text not null,
    valor_total numeric(12,2) not null default 0,
    status text not null default 'ABERTO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_convenio_faturamentos_cliente_id on plantaopro.convenio_faturamentos (cliente_id);
create index if not exists ix_convenio_faturamentos_status on plantaopro.convenio_faturamentos (status);
create index if not exists ix_convenio_faturamentos_reg_date on plantaopro.convenio_faturamentos (reg_date);
create index if not exists ix_convenio_faturamentos_reg_status on plantaopro.convenio_faturamentos (reg_status);

create table if not exists plantaopro.planos_saude (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    nome text not null,
    operadora text null,
    registro_ans text null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_planos_saude_cliente_id on plantaopro.planos_saude (cliente_id);
create index if not exists ix_planos_saude_status on plantaopro.planos_saude (status);
create index if not exists ix_planos_saude_reg_date on plantaopro.planos_saude (reg_date);
create index if not exists ix_planos_saude_reg_status on plantaopro.planos_saude (reg_status);

create table if not exists plantaopro.plano_saude_coberturas (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    plano_saude_id uuid not null,
    descricao text not null,
    limite text null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_plano_saude_coberturas_cliente_id on plantaopro.plano_saude_coberturas (cliente_id);
create index if not exists ix_plano_saude_coberturas_status on plantaopro.plano_saude_coberturas (status);
create index if not exists ix_plano_saude_coberturas_reg_date on plantaopro.plano_saude_coberturas (reg_date);
create index if not exists ix_plano_saude_coberturas_reg_status on plantaopro.plano_saude_coberturas (reg_status);

create table if not exists plantaopro.plano_saude_pacientes (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    plano_saude_id uuid not null,
    paciente_id uuid not null,
    numero_carteirinha text null,
    principal boolean not null default false,
    validade date null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_plano_saude_pacientes_cliente_id on plantaopro.plano_saude_pacientes (cliente_id);
create index if not exists ix_plano_saude_pacientes_paciente_id on plantaopro.plano_saude_pacientes (paciente_id);
create index if not exists ix_plano_saude_pacientes_status on plantaopro.plano_saude_pacientes (status);
create index if not exists ix_plano_saude_pacientes_reg_date on plantaopro.plano_saude_pacientes (reg_date);
create index if not exists ix_plano_saude_pacientes_reg_status on plantaopro.plano_saude_pacientes (reg_status);

create table if not exists plantaopro.plano_saude_autorizacoes (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    plano_saude_id uuid null,
    paciente_id uuid null,
    procedimento text null,
    motivo text null,
    status text not null default 'SOLICITADA',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_plano_saude_autorizacoes_cliente_id on plantaopro.plano_saude_autorizacoes (cliente_id);
create index if not exists ix_plano_saude_autorizacoes_paciente_id on plantaopro.plano_saude_autorizacoes (paciente_id);
create index if not exists ix_plano_saude_autorizacoes_status on plantaopro.plano_saude_autorizacoes (status);
create index if not exists ix_plano_saude_autorizacoes_reg_date on plantaopro.plano_saude_autorizacoes (reg_date);
create index if not exists ix_plano_saude_autorizacoes_reg_status on plantaopro.plano_saude_autorizacoes (reg_status);

create table if not exists plantaopro.plano_saude_historico (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid null,
    plano_saude_id uuid null,
    paciente_id uuid null,
    acao text not null,
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);
create index if not exists ix_plano_saude_historico_cliente_id on plantaopro.plano_saude_historico (cliente_id);
create index if not exists ix_plano_saude_historico_paciente_id on plantaopro.plano_saude_historico (paciente_id);
create index if not exists ix_plano_saude_historico_status on plantaopro.plano_saude_historico (status);
create index if not exists ix_plano_saude_historico_reg_date on plantaopro.plano_saude_historico (reg_date);
create index if not exists ix_plano_saude_historico_reg_status on plantaopro.plano_saude_historico (reg_status);

do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'ux_cid_tabela_codigo') then
        alter table plantaopro.cid_tabela add constraint ux_cid_tabela_codigo unique (codigo);
    end if;
end $$;

do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'ck_agendamentos_periodo') then
        alter table plantaopro.agendamentos add constraint ck_agendamentos_periodo check (data_fim > data_inicio);
    end if;
end $$;

do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'ck_plano_saude_pacientes_validade') then
        alter table plantaopro.plano_saude_pacientes add constraint ck_plano_saude_pacientes_validade check (validade is null or validade >= date '1900-01-01');
    end if;
end $$;
