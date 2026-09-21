-- PlantãoPro v2.15.7: invariantes transacionais do ciclo plantão -> escala -> pagamento.
-- Os índices parciais preservam o histórico e impedem efeitos duplicados apenas nos estados ativos.

alter table plantaopro.plantoes
    add column if not exists data_inicio timestamptz,
    add column if not exists data_fim timestamptz,
    add column if not exists valor numeric(12,2) default 0,
    add column if not exists vagas integer default 1,
    add column if not exists vagas_disponiveis integer default 1;

alter table plantaopro.plantoes
    drop constraint if exists ck_plantoes_vagas_consistentes;
alter table plantaopro.plantoes
    add constraint ck_plantoes_vagas_consistentes
    check (vagas > 0 and vagas_disponiveis between 0 and vagas and valor >= 0 and data_fim > data_inicio)
    not valid;

alter table plantaopro.plantao_convites
    add column if not exists plantao_id uuid,
    add column if not exists medico_id uuid,
    add column if not exists reg_status char(1) default 'A',
    add column if not exists expira_em timestamptz;

create unique index if not exists ux_plantao_convite_pendente
    on plantaopro.plantao_convites(plantao_id, medico_id)
    where reg_status='A' and upper(status) in ('ENVIADO', 'PENDENTE', 'PROCESSANDO');

alter table plantaopro.escalas
    add column if not exists plantao_id uuid,
    add column if not exists medico_id uuid,
    add column if not exists reg_status char(1) default 'A';

create unique index if not exists ux_escala_ocupacao_ativa
    on plantaopro.escalas(plantao_id, medico_id)
    where reg_status='A' and lower(status) in ('solicitado', 'solicitada', 'confirmado', 'confirmada', 'realizado', 'realizada');

create unique index if not exists ux_pagamento_origem_escala
    on plantaopro.pagamentos(escala_id)
    where reg_status='A';

-- A política de conflito é global por profissional: tenants distintos não podem reservar
-- o mesmo CRM no mesmo intervalo. A API retorna somente o bloqueio, nunca dados do outro tenant.
