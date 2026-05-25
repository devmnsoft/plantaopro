-- Evolução SaaS Premium - PlantãoPro
create schema if not exists plantaopro;

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
create index if not exists idx_medico_score_medico on plantaopro.medico_score(medico_id);

create table if not exists plantaopro.plantao_avaliacoes (
    id uuid primary key default gen_random_uuid(),
    escala_id uuid not null,
    plantao_id uuid not null,
    medico_id uuid not null,
    hospital_id uuid null,
    cliente_id uuid null,
    avaliador_usuario_id uuid not null,
    nota smallint not null,
    pontualidade smallint not null,
    conduta smallint not null,
    comunicacao smallint not null,
    observacoes text null,
    status varchar(30) not null default 'ATIVA',
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.cliente_saude (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null,
    score_saude numeric(5,2) not null default 0,
    classificacao varchar(20) not null default 'ATENCAO',
    dias_sem_acesso int not null default 0,
    uso_plano_percentual numeric(5,2) not null default 0,
    faturas_vencidas int not null default 0,
    chamados_criticos int not null default 0,
    atualizado_em timestamp not null default now(),
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

create table if not exists plantaopro.documentos (
    id uuid primary key default gen_random_uuid(),
    nome varchar(255) not null,
    tipo varchar(60) not null,
    content_type varchar(120) not null,
    tamanho_bytes bigint not null,
    storage_path text not null,
    cliente_id uuid null,
    medico_id uuid null,
    hospital_id uuid null,
    criado_por_usuario_id uuid not null,
    reg_status char(1) not null default 'A',
    reg_date timestamp not null default now()
);

do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'uk_plantao_avaliacoes_escala') then
        alter table plantaopro.plantao_avaliacoes add constraint uk_plantao_avaliacoes_escala unique (escala_id);
    end if;
end $$;
