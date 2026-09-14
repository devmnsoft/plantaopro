-- PlantãoPro v2.16.8 — evolução aditiva da conferência sem reabrir decisões.
set search_path to plantaopro, public;

alter table medico_presenca_correcoes
 add column if not exists versao_presenca_base bigint not null default 1;

-- Apenas propostas ainda pendentes precisam ser compatibilizadas com a versão
-- já incrementada pela criação da correção na v2.16.7.
update medico_presenca_correcoes x set versao_presenca_base=c.versao
from medico_checkins c
where c.id=x.presenca_id
  and x.status='PENDENTE'
  and x.versao_presenca_base=1
  and c.versao<>1;

-- Nunca recalcula status: completa somente metadados técnicos ausentes.
update medico_checkins set
 checkin_recebido_em=coalesce(checkin_recebido_em,checkin_em),
 checkout_recebido_em=coalesce(checkout_recebido_em,checkout_em)
where checkin_recebido_em is null or (checkout_em is not null and checkout_recebido_em is null);
