-- PlantaoPro Premium SaaS: automação, KPIs e reforço de integridade
set search_path to plantaopro, public;

alter table if exists plantaopro.escalas
    add column if not exists horas_previstas numeric(8,2) not null default 0,
    add column if not exists score_prioridade numeric(8,2) not null default 0,
    add column if not exists conflito_detectado boolean not null default false;

alter table if exists plantaopro.escalas
    add constraint if not exists ck_escalas_horas_previstas_non_negative check (horas_previstas >= 0),
    add constraint if not exists ck_escalas_score_prioridade_non_negative check (score_prioridade >= 0);

alter table if exists plantaopro.pagamentos
    add column if not exists valor_hora numeric(10,2) not null default 0,
    add column if not exists horas_referencia numeric(8,2) not null default 0;

alter table if exists plantaopro.pagamentos
    add constraint if not exists ck_pagamentos_valor_hora_non_negative check (valor_hora >= 0),
    add constraint if not exists ck_pagamentos_horas_ref_non_negative check (horas_referencia >= 0);

create index if not exists idx_escalas_medico_status_regdate on plantaopro.escalas(medico_id, status, reg_date desc);
create index if not exists idx_plantoes_periodo_status on plantaopro.plantoes(data_inicio, data_fim, status);
create index if not exists idx_pagamentos_status_data_prevista on plantaopro.pagamentos(status, data_prevista);

-- atualização dos registros existentes com cálculo proporcional
update plantaopro.pagamentos pg
set horas_referencia = coalesce(horas_referencia, extract(epoch from (pl.data_fim-pl.data_inicio))/3600.0),
    valor_hora = case
        when coalesce(extract(epoch from (pl.data_fim-pl.data_inicio))/3600.0, 0) > 0
            then round(pg.valor_previsto / (extract(epoch from (pl.data_fim-pl.data_inicio))/3600.0), 2)
        else 0
    end
from plantaopro.plantoes pl
where pl.id = pg.plantao_id
  and pg.reg_status = 'A';
