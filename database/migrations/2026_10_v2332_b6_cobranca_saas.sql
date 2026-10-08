-- R5-B6: cobranca SaaS de verdade (provider sandbox + webhook assinado) e reconciliacao
-- dos shapes congelados de faturas_saas/pagamentos_saas.
-- Contexto: criacoes concorrentes com "if not exists" congelaram as tabelas no formato
-- placeholder em bancos reais. O BFF canônico (api/faturamento-saas) ja escreve em
-- data_pagamento/forma_pagamento/motivo_*/resposta_contestacao/observacoes e essas
-- colunas nunca existiram nesses bancos (42703 escondido por testes de contrato).
-- Esta migration e aditiva e idempotente: ADD COLUMN IF NOT EXISTS preserva quem ja tem.
alter table plantaopro.faturas_saas add column if not exists data_pagamento date null;
alter table plantaopro.faturas_saas add column if not exists forma_pagamento varchar(60) null;
alter table plantaopro.faturas_saas add column if not exists motivo_cancelamento text null;
alter table plantaopro.faturas_saas add column if not exists motivo_contestacao text null;
alter table plantaopro.faturas_saas add column if not exists resposta_contestacao text null;
alter table plantaopro.faturas_saas add column if not exists valor numeric(14,2) not null default 0;
alter table plantaopro.faturas_saas add column if not exists valor_pago numeric(14,2) null;
alter table plantaopro.faturas_saas add column if not exists assinatura_id uuid null;

alter table plantaopro.pagamentos_saas add column if not exists data_pagamento date null;
alter table plantaopro.pagamentos_saas add column if not exists forma_pagamento varchar(60) null;
alter table plantaopro.pagamentos_saas add column if not exists observacoes text null;
alter table plantaopro.pagamentos_saas add column if not exists criado_em timestamp null;

-- Provider de cobranca: registro administrativo do meio de pagamento.
-- Nenhum dado de cartao ou bancario sensivel e armazenado aqui.
create table if not exists plantaopro.cobranca_providers (
    codigo varchar(40) primary key,
    nome varchar(120) not null,
    modo varchar(20) not null default 'SANDBOX' constraint ck_cobranca_providers_modo check (modo in ('SANDBOX','PRODUCAO')),
    status varchar(20) not null default 'INATIVO' constraint ck_cobranca_providers_status check (status in ('ATIVO','INATIVO')),
    descricao text null,
    criado_em timestamp not null default now(),
    atualizado_em timestamp not null default now(),
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

-- Cobranca = tentativa de pagamento de uma fatura em um provider.
-- Referencia e opaca e unica; no maximo uma cobranca ativa por fatura.
create table if not exists plantaopro.cobranca_cobrancas (
    id uuid primary key default gen_random_uuid(),
    fatura_id uuid not null,
    cliente_id uuid not null,
    provider_codigo varchar(40) not null,
    referencia varchar(80) not null,
    checkout_url text null,
    valor numeric(14,2) not null constraint ck_cobranca_cobrancas_valor check (valor > 0),
    status varchar(20) not null default 'PENDENTE' constraint ck_cobranca_cobrancas_status check (status in ('PENDENTE','INICIADA','PAGA','FALHA','CANCELADA','ESTORNADA')),
    expira_em timestamp not null default now() + interval '7 days',
    evento_final_id varchar(120) null,
    criado_em timestamp not null default now(),
    atualizado_em timestamp not null default now(),
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now(),
    reg_update timestamp null
);

create unique index if not exists ux_cobranca_cobrancas_referencia on plantaopro.cobranca_cobrancas(referencia);
create unique index if not exists ux_cobranca_cobrancas_fatura_ativa on plantaopro.cobranca_cobrancas(fatura_id) where status in ('PENDENTE','INICIADA') and reg_status='A';
create index if not exists ix_cobranca_cobrancas_fatura on plantaopro.cobranca_cobrancas(fatura_id);
create index if not exists ix_cobranca_cobrancas_cliente on plantaopro.cobranca_cobrancas(cliente_id);

-- Eventos de webhook recebidos. O indice unico (provider, evento) e o arbitro do dedupe:
-- violacao significa evento repetido e a transacao inteira de aplicacao e descartada.
create table if not exists plantaopro.cobranca_webhook_eventos (
    id uuid primary key default gen_random_uuid(),
    provider_codigo varchar(40) not null,
    evento_id varchar(120) not null,
    tipo_evento varchar(40) not null,
    referencia varchar(80) null,
    cobranca_id uuid null,
    fatura_id uuid null,
    resultado varchar(20) not null constraint ck_cobranca_webhook_resultado check (resultado in ('APLICADO','IGNORADO','RECUSADO')),
    mensagem text null,
    payload jsonb null,
    processado_em timestamp not null default now(),
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create unique index if not exists ux_cobranca_webhook_evento on plantaopro.cobranca_webhook_eventos(provider_codigo, evento_id);
create index if not exists ix_cobranca_webhook_fatura on plantaopro.cobranca_webhook_eventos(fatura_id);

-- Provider sandbox deterministico (nenhum provedor externo existe ainda no produto).
insert into plantaopro.cobranca_providers(codigo,nome,modo,status,descricao)
values('SANDBOX','Cobranca sandbox PLANTAOPro','SANDBOX','ATIVO','Checkout simulado sem dados de cartao; webhooks assinados com HMAC local.')
on conflict (codigo) do update set nome=excluded.nome, modo=excluded.modo, descricao=excluded.descricao, atualizado_em=now();
