-- PlantãoPro v2.15.8 — integridade da obrigação e do pagamento médico.
-- Evolui o domínio canônico plantaopro.pagamentos; não se confunde com cobrança SaaS ou financeiro clínico.
set search_path to plantaopro, public;

alter table plantaopro.pagamentos
  add column if not exists valor_apurado numeric(14,2),
  add column if not exists valor_aprovado numeric(14,2),
  add column if not exists parametros_apuracao jsonb not null default '{}'::jsonb,
  add column if not exists versao bigint not null default 1,
  add column if not exists origem_pagamento varchar(20),
  add column if not exists referencia_pagamento varchar(120),
  add column if not exists aprovado_por uuid,
  add column if not exists aprovado_em timestamptz;

update plantaopro.pagamentos
set valor_apurado=coalesce(valor_apurado,valor_previsto),
    valor_aprovado=case when status='pago' then coalesce(valor_aprovado,valor_pago,valor_previsto) else valor_aprovado end,
    origem_pagamento=case when status='pago' then coalesce(origem_pagamento,'MANUAL') else origem_pagamento end
where valor_apurado is null or (status='pago' and valor_aprovado is null);

do $$ begin
 alter table plantaopro.pagamentos add constraint ck_v2158_pagamento_valores
 check (coalesce(valor_apurado,0)>=0 and coalesce(valor_aprovado,0)>=0 and coalesce(valor_pago,0)>=0
        and (status<>'pago' or (valor_pago=valor_aprovado and valor_pago>0)));
exception when duplicate_object then null; end $$;

do $$ begin
 alter table plantaopro.pagamentos add constraint ck_v2158_pagamento_origem
 check (origem_pagamento is null or origem_pagamento in ('MANUAL','IMPORTADO','INTEGRACAO_CONFIRMADA'));
exception when duplicate_object then null; end $$;

create unique index if not exists ux_v2158_pagamento_referencia_manual
 on plantaopro.pagamentos(tenant_id,cliente_id,referencia_pagamento)
 where referencia_pagamento is not null and status='pago' and reg_status='A';
create index if not exists ix_v2158_pagamento_conferencia
 on plantaopro.pagamentos(tenant_id,cliente_id,status,reg_date desc) where reg_status='A';
