-- PlantãoPro v1.95.1 — contrato canônico de pagamentos de plantões.
-- Mantém este domínio separado de pagamentos_medicos e pagamentos_saas.
set search_path to plantaopro, public;

create table if not exists plantaopro.pagamentos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid,
    cliente_id uuid,
    escala_id uuid not null,
    medico_id uuid not null,
    plantao_id uuid not null,
    valor_previsto numeric(14,2) not null default 0,
    valor_pago numeric(14,2),
    valor_hora numeric(14,2) not null default 0,
    horas_referencia numeric(8,2) not null default 0,
    status varchar(24) not null default 'pendente',
    data_prevista date,
    data_vencimento date,
    data_pagamento date,
    forma_pagamento varchar(50),
    chave_pix varchar(180),
    observacoes text,
    processado_automaticamente boolean not null default false,
    created_by uuid,
    updated_by uuid,
    reg_date timestamptz not null default now(),
    reg_update timestamptz,
    reg_status char(1) not null default 'A',
    constraint ck_pagamentos_valores check (
        valor_previsto >= 0 and coalesce(valor_pago, 0) >= 0 and
        valor_hora >= 0 and horas_referencia >= 0),
    constraint ck_pagamentos_reg_status check (reg_status in ('A','I'))
);

create unique index if not exists ux_pagamentos_escala_ativo
    on plantaopro.pagamentos(escala_id) where reg_status = 'A';
create index if not exists ix_pagamentos_tenant_status
    on plantaopro.pagamentos(tenant_id, status, data_prevista) where reg_status = 'A';
create index if not exists ix_pagamentos_medico
    on plantaopro.pagamentos(medico_id, reg_date desc) where reg_status = 'A';

create table if not exists plantaopro.historico_pagamento (
    id uuid primary key default gen_random_uuid(),
    pagamento_id uuid not null references plantaopro.pagamentos(id),
    status_anterior varchar(24),
    status_novo varchar(24) not null,
    justificativa text,
    usuario_id uuid,
    reg_date timestamptz not null default now()
);
create index if not exists ix_historico_pagamento_timeline
    on plantaopro.historico_pagamento(pagamento_id, reg_date desc);
