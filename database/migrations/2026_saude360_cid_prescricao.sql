create schema if not exists plantaopro;
create extension if not exists pgcrypto;

create table if not exists plantaopro.cid_tabela (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    codigo text not null,
    descricao text not null,
    categoria text not null default '',
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.cid_tabela
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists codigo text not null default '',
    add column if not exists descricao text not null default '',
    add column if not exists categoria text not null default '',
    add column if not exists status text not null default 'ATIVO',
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists created_at timestamptz not null default now(),
    add column if not exists updated_at timestamptz null,
    add column if not exists reg_date timestamptz not null default now(),
    add column if not exists reg_update timestamptz null,
    add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.cid_favoritos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    cid_id uuid not null,
    medico_id uuid null,
    usuario_id uuid null,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.cid_favoritos
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists cid_id uuid null,
    add column if not exists medico_id uuid null,
    add column if not exists usuario_id uuid null,
    add column if not exists status text not null default 'ATIVO',
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists created_at timestamptz not null default now(),
    add column if not exists updated_at timestamptz null,
    add column if not exists reg_date timestamptz not null default now(),
    add column if not exists reg_update timestamptz null,
    add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.cid_uso_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    cid_id uuid not null,
    consulta_id uuid null,
    paciente_id uuid null,
    medico_id uuid null,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    created_at timestamptz not null default now(),
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.cid_uso_historico
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists cid_id uuid null,
    add column if not exists consulta_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists medico_id uuid null,
    add column if not exists usuario_id uuid null,
    add column if not exists status text not null default 'REGISTRADO',
    add column if not exists created_by uuid null,
    add column if not exists created_at timestamptz not null default now(),
    add column if not exists reg_date timestamptz not null default now(),
    add column if not exists reg_status char(1) not null default 'A';

alter table if exists plantaopro.consultas
    add column if not exists anamnese text not null default '',
    add column if not exists exame_fisico text not null default '',
    add column if not exists diagnostico text not null default '',
    add column if not exists cid_id uuid null,
    add column if not exists codigo_cid text not null default '',
    add column if not exists conduta text not null default '',
    add column if not exists orientacoes text not null default '',
    add column if not exists finalizada_em timestamptz null,
    add column if not exists cancelada_em timestamptz null;

create table if not exists plantaopro.consulta_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    consulta_id uuid not null,
    paciente_id uuid null,
    acao text not null,
    detalhe text not null default '',
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    created_at timestamptz not null default now(),
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.prescricoes (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    paciente_id uuid not null,
    consulta_id uuid not null,
    medico_id uuid not null,
    modelo_id uuid null,
    orientacoes text not null default '',
    status text not null default 'RASCUNHO',
    finalizada_em timestamptz null,
    cancelada_em timestamptz null,
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.prescricoes
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists paciente_id uuid null,
    add column if not exists consulta_id uuid null,
    add column if not exists medico_id uuid null,
    add column if not exists modelo_id uuid null,
    add column if not exists orientacoes text not null default '',
    add column if not exists status text not null default 'RASCUNHO',
    add column if not exists finalizada_em timestamptz null,
    add column if not exists cancelada_em timestamptz null,
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists created_at timestamptz not null default now(),
    add column if not exists updated_at timestamptz null,
    add column if not exists reg_date timestamptz not null default now(),
    add column if not exists reg_update timestamptz null,
    add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.prescricao_itens (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    prescricao_id uuid not null,
    medicamento text not null default '',
    posologia text not null default '',
    frequencia text not null default '',
    duracao text not null default '',
    orientacoes text not null default '',
    ordem integer not null default 1,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.prescricao_modelos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    nome text not null,
    medico_id uuid null,
    descricao text not null default '',
    conteudo jsonb not null default '{}'::jsonb,
    status text not null default 'ATIVO',
    created_by uuid null,
    updated_by uuid null,
    created_at timestamptz not null default now(),
    updated_at timestamptz null,
    reg_date timestamptz not null default now(),
    reg_update timestamptz null,
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.prescricao_modelos
    add column if not exists tenant_id uuid null,
    add column if not exists cliente_id uuid null,
    add column if not exists nome text not null default '',
    add column if not exists medico_id uuid null,
    add column if not exists descricao text not null default '',
    add column if not exists conteudo jsonb not null default '{}'::jsonb,
    add column if not exists status text not null default 'ATIVO',
    add column if not exists created_by uuid null,
    add column if not exists updated_by uuid null,
    add column if not exists created_at timestamptz not null default now(),
    add column if not exists updated_at timestamptz null,
    add column if not exists reg_date timestamptz not null default now(),
    add column if not exists reg_update timestamptz null,
    add column if not exists reg_status char(1) not null default 'A';

create table if not exists plantaopro.prescricao_historico (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    prescricao_id uuid not null,
    acao text not null,
    detalhes jsonb not null default '{}'::jsonb,
    usuario_id uuid null,
    status text not null default 'REGISTRADO',
    created_by uuid null,
    created_at timestamptz not null default now(),
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.prescricao_cancelamentos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid null,
    cliente_id uuid null,
    prescricao_id uuid not null,
    justificativa text not null,
    usuario_id uuid null,
    status text not null default 'CANCELADO',
    created_by uuid null,
    created_at timestamptz not null default now(),
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);

create index if not exists ix_cid_tabela_codigo on plantaopro.cid_tabela (codigo);
create index if not exists ix_cid_tabela_busca on plantaopro.cid_tabela (codigo, descricao);
create index if not exists ix_cid_favoritos_medico on plantaopro.cid_favoritos (cliente_id, medico_id, cid_id);
create index if not exists ix_cid_uso_historico_cid on plantaopro.cid_uso_historico (cliente_id, cid_id);
create index if not exists ix_consultas_cid_id on plantaopro.consultas (cid_id);
create index if not exists ix_consulta_historico_consulta_id on plantaopro.consulta_historico (consulta_id);
create index if not exists ix_prescricoes_consulta_id on plantaopro.prescricoes (consulta_id);
create index if not exists ix_prescricoes_paciente_id on plantaopro.prescricoes (paciente_id);
create index if not exists ix_prescricoes_medico_id on plantaopro.prescricoes (medico_id);
create index if not exists ix_prescricao_itens_prescricao_id on plantaopro.prescricao_itens (prescricao_id);
create index if not exists ix_prescricao_modelos_medico on plantaopro.prescricao_modelos (cliente_id, medico_id);
create index if not exists ix_prescricao_historico_prescricao_id on plantaopro.prescricao_historico (prescricao_id);
create index if not exists ix_prescricao_cancelamentos_prescricao_id on plantaopro.prescricao_cancelamentos (prescricao_id);

do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'ux_cid_tabela_codigo') then
        alter table plantaopro.cid_tabela add constraint ux_cid_tabela_codigo unique (codigo);
    end if;
    if not exists (select 1 from pg_constraint where conname = 'ux_cid_favoritos_medico_cid') then
        alter table plantaopro.cid_favoritos add constraint ux_cid_favoritos_medico_cid unique (cliente_id, medico_id, cid_id);
    end if;
    if not exists (select 1 from pg_constraint where conname = 'fk_prescricao_itens_prescricao') then
        alter table plantaopro.prescricao_itens add constraint fk_prescricao_itens_prescricao foreign key (prescricao_id) references plantaopro.prescricoes(id);
    end if;
end $$;
