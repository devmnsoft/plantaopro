-- schema plantaopro
create table if not exists plantaopro.medico_score (
 id uuid primary key default gen_random_uuid(),
 medico_id uuid not null,
 cliente_id uuid null,
 score_geral numeric(5,2) not null default 0,
 taxa_aceite numeric(5,2) not null default 0,
 taxa_cancelamento numeric(5,2) not null default 0,
 pontualidade numeric(5,2) not null default 0,
 total_horas_mes numeric(10,2) not null default 0,
 total_plantoes_mes int not null default 0,
 total_realizados int not null default 0,
 total_recusados int not null default 0,
 total_cancelados int not null default 0,
 media_avaliacao numeric(3,2) not null default 0,
 atualizado_em timestamp not null default now(),
 reg_status char(1) not null default 'A',
 reg_date timestamp not null default now()
);
create index if not exists ix_medico_score_medico on plantaopro.medico_score(medico_id);

create table if not exists plantaopro.plantao_avaliacoes (
 id uuid primary key default gen_random_uuid(),
 escala_id uuid not null,
 plantao_id uuid not null,
 medico_id uuid not null,
 hospital_id uuid not null,
 cliente_id uuid null,
 avaliador_usuario_id uuid not null,
 nota int not null,
 pontualidade int not null,
 conduta int not null,
 comunicacao int not null,
 observacoes text null,
 status varchar(20) not null default 'ATIVA',
 reg_status char(1) not null default 'A',
 reg_date timestamp not null default now()
);

do $$ begin
if not exists (select 1 from pg_constraint where conname = 'ck_plantao_avaliacoes_nota') then
alter table plantaopro.plantao_avaliacoes add constraint ck_plantao_avaliacoes_nota check (nota between 1 and 5);
end if;
end $$;
