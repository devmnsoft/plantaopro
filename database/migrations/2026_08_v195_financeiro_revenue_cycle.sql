-- PlantaoPro v1.95: evolução incremental do financeiro clínico; não altera pagamentos de plantão nem faturamento SaaS.
begin;

alter table plantaopro.v113_faturas
  add column if not exists origem_tipo varchar(40),
  add column if not exists regra_id uuid,
  add column if not exists regra_codigo varchar(80),
  add column if not exists valor_base_snapshot numeric(14,2) not null default 0,
  add column if not exists descontos numeric(14,2) not null default 0,
  add column if not exists acrescimos numeric(14,2) not null default 0,
  add column if not exists glosa_reconhecida numeric(14,2) not null default 0;

alter table plantaopro.v115_recebimentos
  add column if not exists tipo varchar(20) not null default 'RECEBIMENTO',
  add column if not exists recebimento_origem_id uuid,
  add column if not exists observacao text,
  add column if not exists recebido_em timestamptz not null default now();

do $$ begin
  alter table plantaopro.v115_recebimentos add constraint ck_v195_recebimento_positivo check (valor_recebido > 0);
exception when duplicate_object then null; end $$;
do $$ begin
  alter table plantaopro.v115_recebimentos add constraint ck_v195_recebimento_tipo check (tipo in ('RECEBIMENTO','ESTORNO'));
exception when duplicate_object then null; end $$;

create unique index if not exists ux_v195_conta_origem_ativa on plantaopro.v113_faturas(tenant_id,origem_tipo,pedido_id) where reg_status='A' and origem_tipo is not null and pedido_id is not null;
create index if not exists ix_v195_conta_tenant_status on plantaopro.v113_faturas(tenant_id,status) where reg_status='A';
create index if not exists ix_v195_recebimento_conta_tipo on plantaopro.v115_recebimentos(tenant_id,conta_receber_id,tipo) where reg_status='A';

alter table plantaopro.v115_regras_glosa add column if not exists valor_recuperado numeric(14,2) not null default 0;
do $$ begin alter table plantaopro.v115_regras_glosa add constraint ck_v195_glosa_positiva check(valor_glosado>0); exception when duplicate_object then null; end $$;
create index if not exists ix_v195_glosa_tenant_status_prazo on plantaopro.v115_regras_glosa(tenant_id,status,prazo_recurso) where reg_status='A';

alter table plantaopro.v115_regras_repasse
 add column if not exists regra_id uuid,
 add column if not exists evento_gerador varchar(40) not null default 'CONTA_EMITIDA';
do $$ begin alter table plantaopro.v115_regras_repasse add constraint ck_v195_repasse_percentual check(tipo_regra<>'PERCENTUAL' or percentual between 0 and 100); exception when duplicate_object then null; end $$;
create unique index if not exists ux_v195_repasse_lancamento on plantaopro.v115_regras_repasse(tenant_id,referencia_id,medico_id,regra_id) where reg_status='A' and referencia_id is not null and status not in ('CANCELADO','REGRA_ATIVA');
create index if not exists ix_v195_repasse_tenant_status on plantaopro.v115_regras_repasse(tenant_id,status) where reg_status='A';

create table if not exists plantaopro.v115_financeiro_historico(
 id uuid primary key default gen_random_uuid(), tenant_id uuid not null, entidade_tipo varchar(30) not null,
 entidade_id uuid not null, evento varchar(60) not null, valor_anterior numeric(14,2), valor_novo numeric(14,2),
 detalhes jsonb not null default '{}'::jsonb, created_at timestamptz not null default now(), created_by uuid
);
create index if not exists ix_v195_financeiro_historico_entidade on plantaopro.v115_financeiro_historico(tenant_id,entidade_tipo,entidade_id,created_at desc);
commit;
