-- v2.10.0 - estruturas idempotentes; aplicadas somente pelo pipeline de migração.
create table if not exists plantaopro.solicitacoes_plantao(
 id uuid primary key default gen_random_uuid(), tenant_id uuid not null, unidade_id uuid not null,
 especialidade_id uuid not null, setor text not null, data date not null, horario_inicio time not null,
 horario_fim time not null, quantidade_profissionais integer not null check(quantidade_profissionais>0),
 observacoes text, prioridade text not null default 'normal', justificativa text not null,
 status text not null default 'rascunho' check(status in('rascunho','enviada','em_analise','aprovada','recusada','convertida')),
 motivo_recusa text, plantao_id uuid, criado_por uuid not null, criado_em timestamptz not null default now(), atualizado_em timestamptz,
 constraint ck_solicitacao_horario check(horario_inicio<horario_fim));
create index if not exists ix_solicitacoes_plantao_escopo on plantaopro.solicitacoes_plantao(tenant_id,unidade_id,status,data);

create table if not exists plantaopro.contratos_operacionais(
 id uuid primary key default gen_random_uuid(), tenant_id uuid not null, unidade_id uuid not null,
 vigencia_inicio date not null, vigencia_fim date not null, status text not null,
 especialidades uuid[] not null default '{}', valor_base numeric(14,2) not null default 0,
 cobertura_minima integer not null default 0, sla_cobertura_minutos integer not null default 0,
 regras_cancelamento text, regras_substituicao text, observacoes_comerciais text, criado_em timestamptz not null default now(),
 constraint ck_contrato_vigencia check(vigencia_inicio<=vigencia_fim));
create index if not exists ix_contratos_operacionais_vigencia on plantaopro.contratos_operacionais(tenant_id,unidade_id,status,vigencia_inicio,vigencia_fim);

create table if not exists plantaopro.regras_preco_contrato(
 id uuid primary key default gen_random_uuid(), contrato_id uuid not null references plantaopro.contratos_operacionais(id),
 tenant_id uuid not null, especialidade_id uuid, tipo_plantao text not null, dia_semana smallint,
 periodo text, feriado boolean, horario_inicio time, horario_fim time, valor_base numeric(14,2) not null,
 acrescimo_percentual numeric(7,2) not null default 0, desconto_percentual numeric(7,2) not null default 0,
 vigencia_inicio date not null, vigencia_fim date not null);
create index if not exists ix_regras_preco_vigencia on plantaopro.regras_preco_contrato(tenant_id,contrato_id,vigencia_inicio,vigencia_fim);
