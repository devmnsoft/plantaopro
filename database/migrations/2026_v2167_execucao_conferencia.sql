-- PlantãoPro v2.16.7 — execução e correção auditável de plantões.
set search_path to plantaopro, public;

alter table medico_checkins
  add column if not exists checkin_recebido_em timestamptz,
  add column if not exists checkout_recebido_em timestamptz,
  add column if not exists checkin_declarado_em timestamptz,
  add column if not exists checkout_declarado_em timestamptz,
  add column if not exists timezone_contexto varchar(80),
  add column if not exists status_conferencia varchar(30) not null default 'REGISTRO_INCOMPLETO',
  add column if not exists inicio_aprovado_em timestamptz,
  add column if not exists fim_aprovado_em timestamptz,
  add column if not exists versao bigint not null default 1;

update medico_checkins set checkin_recebido_em=coalesce(checkin_recebido_em,checkin_em),
 checkout_recebido_em=coalesce(checkout_recebido_em,checkout_em),
 status_conferencia=case when checkout_em is null then 'REGISTRO_INCOMPLETO' else 'PENDENTE' end;

do $$ begin alter table medico_checkins add constraint ck_v2167_presenca_aprovada_ordem
 check (fim_aprovado_em is null or (inicio_aprovado_em is not null and fim_aprovado_em>=inicio_aprovado_em));
exception when duplicate_object then null; end $$;
do $$ begin alter table medico_checkins add constraint ck_v2167_presenca_status
 check (status_conferencia in ('REGISTRO_INCOMPLETO','PENDENTE','CORRECAO_PENDENTE','APROVADA','AJUSTE_POS_APURACAO'));
exception when duplicate_object then null; end $$;

create table if not exists medico_presenca_correcoes (
 id uuid primary key default gen_random_uuid(), tenant_id uuid not null, cliente_id uuid not null,
 presenca_id uuid not null references medico_checkins(id), escala_id uuid not null, medico_id uuid not null,
 inicio_original_em timestamptz, fim_original_em timestamptz, inicio_proposto_em timestamptz, fim_proposto_em timestamptz,
 justificativa varchar(1000) not null, status varchar(20) not null default 'PENDENTE', versao bigint not null default 1,
 solicitado_por uuid not null, solicitado_em timestamptz not null default now(), decidido_por uuid, decidido_em timestamptz,
 justificativa_decisao varchar(1000), inicio_aprovado_em timestamptz, fim_aprovado_em timestamptz,
 constraint ck_v2167_correcao_status check(status in ('PENDENTE','APROVADA','RECUSADA','CANCELADA')),
 constraint ck_v2167_correcao_intervalo check(fim_proposto_em is null or inicio_proposto_em is null or fim_proposto_em>=inicio_proposto_em)
);
create unique index if not exists ux_v2167_correcao_pendente on medico_presenca_correcoes(tenant_id,presenca_id) where status='PENDENTE';
create index if not exists ix_v2167_conferencia on medico_checkins(tenant_id,status_conferencia,checkin_em desc);
create index if not exists ix_v2167_correcao_cliente on medico_presenca_correcoes(tenant_id,cliente_id,status,solicitado_em desc);

create table if not exists medico_presenca_historico (
 id uuid primary key default gen_random_uuid(), tenant_id uuid not null, cliente_id uuid not null,
 presenca_id uuid not null, correcao_id uuid, evento varchar(40) not null,
 valores_anteriores jsonb, valores_posteriores jsonb, justificativa varchar(1000),
 executado_por uuid not null, executado_em timestamptz not null default now()
);
create index if not exists ix_v2167_presenca_historico on medico_presenca_historico(tenant_id,presenca_id,executado_em desc);
